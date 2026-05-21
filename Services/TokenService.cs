using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenClawManager.Models;

namespace OpenClawManager.Services;

public class TokenServiceException : Exception
{
    public TokenServiceException(string message) : base(message) { }
}

public record TokenFileOperationResult(
    string InputPath,
    string OutputPath,
    int TotalCount,
    int UniqueCount,
    IReadOnlyList<string> TokenIds,
    IReadOnlyList<string> UnknownPlaceholders,
    string? BackupPath = null);

public record TokenVerifyResult(
    string FilePath,
    IReadOnlyList<string> FoundTokenIds,
    IReadOnlyList<string> UnknownPlaceholders)
{
    public bool IsSafe => FoundTokenIds.Count == 0 && UnknownPlaceholders.Count == 0;
}

public record GitIgnoreResult(string RepoPath, string GitIgnorePath, bool Added, string Pattern);

public record VaultSafetyResult(bool IsSafe, IReadOnlyList<string> Warnings);

public static class TokenService
{
    private static readonly Regex IdRegex = new("^[a-zA-Z0-9_]{1,64}$", RegexOptions.Compiled);
    private static readonly Regex PlaceholderRegex = new(@"\[REDACTED_([a-zA-Z0-9_]{1,64})\]", RegexOptions.Compiled);
    private static readonly byte[] DpapiEntropy = SHA256.HashData(Encoding.UTF8.GetBytes("OpenClawManager.TokenVault.v1"));

    /// <summary>
    /// Aktuální verze schématu vaultu.
    /// ValidateVault odmítne vault s verzí vyšší než tato (forward-only tolerance).
    /// Při přidání nového pole nebo změně struktury zvýšit tuto konstantu
    /// a přidat migrační logiku do LoadVault().
    /// </summary>
    private const int CurrentVaultVersion = 1;

    private static readonly JsonSerializerOptions ReadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string GetDefaultVaultPath()
    {
        var envPath = Environment.GetEnvironmentVariable("TOKEN_MANAGER_SECRETS");
        if (!string.IsNullOrWhiteSpace(envPath))
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(envPath));

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
            return Path.Combine(userProfile, ".token-manager", "secrets.json");

        return Path.Combine(AppContext.BaseDirectory, "secrets.json");
    }

    public static bool EnsureVaultExists(string path)
    {
        path = NormalizePath(path);
        if (File.Exists(path))
            return false;

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        SaveVault(path, new TokenVault());
        AppendAudit(path, "init", path, 0, 0, Array.Empty<string>());
        return true;
    }

    public static TokenVault LoadVault(string path)
    {
        path = NormalizePath(path);
        if (!File.Exists(path))
            throw new TokenServiceException($"Vault neexistuje: {path}. Nejdřív ho inicializuj.");

        TokenVault? vault;
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            vault = JsonSerializer.Deserialize<TokenVault>(json, ReadJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new TokenServiceException($"Vault není platný JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new TokenServiceException($"Vault nejde načíst: {ex.Message}");
        }

        if (vault == null)
            throw new TokenServiceException("Vault je prázdný nebo nečitelný.");

        vault.Tokens ??= new List<TokenEntry>();
        foreach (var token in vault.Tokens)
        {
            if (token.CreatedAt == default)
                token.CreatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(token.ProtectedValue))
                token.Value = UnprotectString(token.ProtectedValue);
        }

        ValidateVault(vault);
        return vault;
    }

    public static void SaveVault(string path, TokenVault vault)
    {
        path = NormalizePath(path);
        ValidateVault(vault);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var protectedVault = CreateProtectedVault(vault);
        var json = JsonSerializer.Serialize(protectedVault, WriteJsonOptions);
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var tmpPath = path + ".tmp";

        File.WriteAllText(tmpPath, json, encoding);

        if (File.Exists(path))
        {
            ReplaceExistingFile(tmpPath, path);
        }
        else
        {
            File.Move(tmpPath, path);
        }
    }

    private static void ReplaceExistingFile(string tmpPath, string targetPath)
    {
        var backupPath = targetPath + ".bak";

        try
        {
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            File.Replace(tmpPath, targetPath, backupPath);
        }
        catch (UnauthorizedAccessException)
        {
            File.Copy(targetPath, backupPath, overwrite: true);
            File.Move(tmpPath, targetPath, overwrite: true);
        }

        try { File.Delete(backupPath); } catch { }
    }

    public static TokenEntry AddToken(string vaultPath, string id, string value, string description)
    {
        var vault = LoadVault(vaultPath);
        ValidateToken(id, value);

        if (vault.Tokens.Any(t => string.Equals(t.Id, id, StringComparison.Ordinal)))
            throw new TokenServiceException($"Token s ID '{id}' už existuje.");

        var token = new TokenEntry
        {
            Id = id,
            Value = value,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        vault.Tokens.Add(token);
        SaveVault(vaultPath, vault);
        AppendAudit(vaultPath, "add", null, 1, 1, new[] { id });
        return token;
    }

    public static void UpdateToken(string vaultPath, string originalId, string id, string value, string description)
    {
        var vault = LoadVault(vaultPath);
        var token = vault.Tokens.FirstOrDefault(t => string.Equals(t.Id, originalId, StringComparison.Ordinal));
        if (token == null)
            throw new TokenServiceException($"Token '{originalId}' neexistuje.");

        var nextValue = string.IsNullOrEmpty(value) ? token.Value : value;
        ValidateToken(id, nextValue);

        if (!string.Equals(originalId, id, StringComparison.Ordinal) &&
            vault.Tokens.Any(t => string.Equals(t.Id, id, StringComparison.Ordinal)))
            throw new TokenServiceException($"Token s ID '{id}' už existuje.");

        token.Id = id;
        token.Value = nextValue;
        token.Description = description;
        SaveVault(vaultPath, vault);
        AppendAudit(vaultPath, "edit", null, 1, 1, new[] { id });
    }

    public static void RemoveToken(string vaultPath, string id)
    {
        var vault = LoadVault(vaultPath);
        var removed = vault.Tokens.RemoveAll(t => string.Equals(t.Id, id, StringComparison.Ordinal));
        if (removed == 0)
            throw new TokenServiceException($"Token '{id}' neexistuje.");

        SaveVault(vaultPath, vault);
        AppendAudit(vaultPath, "remove", null, removed, removed, new[] { id });
    }

    public static void RotateToken(string vaultPath, string id, string newValue)
    {
        var vault = LoadVault(vaultPath);
        var token = vault.Tokens.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.Ordinal));
        if (token == null)
            throw new TokenServiceException($"Token '{id}' neexistuje.");

        ValidateToken(id, newValue);
        token.Value = newValue;
        SaveVault(vaultPath, vault);
        AppendAudit(vaultPath, "rotate", null, 1, 1, new[] { id });
    }

    public static TokenFileOperationResult RedactFile(
        string vaultPath,
        string inputPath,
        string? outputPath = null,
        bool overwrite = false,
        bool dryRun = false)
    {
        var vault = LoadVault(vaultPath);
        inputPath = NormalizeExistingFile(inputPath);
        outputPath = NormalizePath(outputPath ?? BuildDefaultOutputPath(inputPath, "redacted"));

        var textFile = ReadUtf8TextFile(inputPath);
        var content = textFile.Text;

        var total = 0;
        var ids = new List<string>();

        foreach (var token in vault.Tokens.OrderByDescending(t => t.Value.Length))
        {
            var count = CountOccurrences(content, token.Value);
            if (count == 0)
                continue;

            content = content.Replace(token.Value, $"[REDACTED_{token.Id}]", StringComparison.Ordinal);
            total += count;
            ids.Add(token.Id);
        }

        if (!dryRun)
        {
            WriteOutputFile(outputPath, content, textFile.HadUtf8Bom, overwrite);
            AppendAudit(vaultPath, "redact", inputPath, total, ids.Distinct(StringComparer.Ordinal).Count(), ids);
        }

        return new TokenFileOperationResult(
            inputPath,
            outputPath,
            total,
            ids.Distinct(StringComparer.Ordinal).Count(),
            ids.Distinct(StringComparer.Ordinal).ToList(),
            Array.Empty<string>());
    }

    public static TokenFileOperationResult RestoreFile(
        string vaultPath,
        string inputPath,
        string? outputPath = null,
        bool overwrite = false,
        bool dryRun = false)
    {
        var vault = LoadVault(vaultPath);
        inputPath = NormalizeExistingFile(inputPath);
        outputPath = NormalizePath(outputPath ?? BuildDefaultOutputPath(inputPath, "restored"));

        var textFile = ReadUtf8TextFile(inputPath);
        var restore = BuildRestoredText(vault, textFile.Text);

        if (!dryRun)
        {
            WriteOutputFile(outputPath, restore.Text, textFile.HadUtf8Bom, overwrite);
            AppendAudit(vaultPath, "restore", inputPath, restore.Count, restore.UniqueIds.Count, restore.UniqueIds);
        }

        return new TokenFileOperationResult(
            inputPath,
            outputPath,
            restore.Count,
            restore.UniqueIds.Count,
            restore.UniqueIds,
            restore.UnknownPlaceholders);
    }

    public static TokenFileOperationResult RestoreFileInPlace(string vaultPath, string inputPath)
    {
        var vault = LoadVault(vaultPath);
        inputPath = NormalizeExistingFile(inputPath);

        var textFile = ReadUtf8TextFile(inputPath);
        var restore = BuildRestoredText(vault, textFile.Text);

        var backupPath = inputPath + ".bak";
        File.Copy(inputPath, backupPath, overwrite: true);
        WriteOutputFile(inputPath, restore.Text, textFile.HadUtf8Bom, overwrite: true);
        AppendAudit(vaultPath, "restore-inplace", inputPath, restore.Count, restore.UniqueIds.Count, restore.UniqueIds);

        return new TokenFileOperationResult(
            inputPath,
            inputPath,
            restore.Count,
            restore.UniqueIds.Count,
            restore.UniqueIds,
            restore.UnknownPlaceholders,
            backupPath);
    }

    private static RestoreBuildResult BuildRestoredText(TokenVault vault, string text)
    {
        var tokenById = vault.Tokens.ToDictionary(t => t.Id, StringComparer.Ordinal);
        var unknown = new SortedSet<string>(StringComparer.Ordinal);
        var restoredIds = new List<string>();
        var restoredCount = 0;

        var restored = PlaceholderRegex.Replace(text, match =>
        {
            var id = match.Groups[1].Value;
            if (!tokenById.TryGetValue(id, out var token))
            {
                unknown.Add(match.Value);
                return match.Value;
            }

            restoredCount++;
            restoredIds.Add(id);
            return token.Value;
        });

        return new RestoreBuildResult(
            restored,
            restoredCount,
            restoredIds.Distinct(StringComparer.Ordinal).ToList(),
            unknown.ToList());
    }

    public static TokenVerifyResult VerifyFile(string vaultPath, string inputPath)
    {
        var vault = LoadVault(vaultPath);
        inputPath = NormalizeExistingFile(inputPath);
        var text = ReadUtf8TextFile(inputPath).Text;

        var foundTokens = vault.Tokens
            .Where(t => CountOccurrences(text, t.Value) > 0)
            .Select(t => t.Id)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var knownIds = vault.Tokens.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var unknown = PlaceholderRegex.Matches(text)
            .Select(m => new { Placeholder = m.Value, Id = m.Groups[1].Value })
            .Where(x => !knownIds.Contains(x.Id))
            .Select(x => x.Placeholder)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        return new TokenVerifyResult(inputPath, foundTokens, unknown);
    }

    public static bool IsVaultTrackedByGit(string vaultPath)
    {
        try
        {
            vaultPath = NormalizePath(vaultPath);
            var directory = Path.GetDirectoryName(vaultPath);
            if (string.IsNullOrWhiteSpace(directory))
                return false;

            var current = new DirectoryInfo(directory);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")) ||
                    File.Exists(Path.Combine(current.FullName, ".git")))
                {
                    return IsVaultTrackedByGit(vaultPath, current.FullName);
                }

                current = current.Parent;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsPathInsideDirectory(string path, string directory)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(directory))
            return false;

        path = NormalizePath(path);
        directory = NormalizePath(directory);

        var relative = Path.GetRelativePath(directory, path);
        return relative != "." &&
               !relative.StartsWith("..", StringComparison.Ordinal) &&
               !Path.IsPathRooted(relative);
    }

    public static VaultSafetyResult AnalyzeVaultPath(string vaultPath, string openClawPath)
    {
        var warnings = new List<string>();

        try
        {
            vaultPath = NormalizePath(vaultPath);

            var repo = FindNearestGitRepository(vaultPath);
            if (repo != null && IsPathInsideDirectory(vaultPath, repo))
                warnings.Add("Vault is inside a Git repository. Move it outside the project or add it to .gitignore.");

            if (!string.IsNullOrWhiteSpace(openClawPath) && IsPathInsideDirectory(vaultPath, openClawPath))
                warnings.Add("Vault is inside the OpenClaw working folder. Store API keys outside runtime data.");

            var lower = vaultPath.ToLowerInvariant();
            string[] cloudMarkers = [@"\onedrive\", @"\dropbox\", @"\google drive\", @"\iclouddrive\", @"\syncthing\"];
            if (cloudMarkers.Any(lower.Contains))
                warnings.Add("Vault appears to be in a cloud-synced folder. Store it in a local non-synced folder.");
        }
        catch (Exception ex)
        {
            warnings.Add($"Vault path could not be fully validated: {ex.Message}");
        }

        return new VaultSafetyResult(warnings.Count == 0, warnings);
    }

    public static bool IsVaultEncryptedAtRest(string path)
    {
        path = NormalizePath(path);
        if (!File.Exists(path))
            return false;

        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var vault = JsonSerializer.Deserialize<TokenVault>(json, ReadJsonOptions);
            return vault?.Tokens != null && vault.Tokens.All(t =>
                string.IsNullOrEmpty(t.Value) && !string.IsNullOrWhiteSpace(t.ProtectedValue));
        }
        catch
        {
            return false;
        }
    }

    public static string? FindNearestGitRepository(string startPath)
    {
        try
        {
            startPath = NormalizePath(startPath);
            var directory = File.Exists(startPath)
                ? Path.GetDirectoryName(startPath)
                : startPath;

            if (string.IsNullOrWhiteSpace(directory))
                return null;

            var current = new DirectoryInfo(directory);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")) ||
                    File.Exists(Path.Combine(current.FullName, ".git")))
                    return current.FullName;

                current = current.Parent;
            }
        }
        catch { }

        return null;
    }

    public static GitIgnoreResult AddVaultToGitIgnore(string vaultPath)
    {
        vaultPath = NormalizePath(vaultPath);
        var repo = FindNearestGitRepository(vaultPath);
        if (repo == null)
            throw new TokenServiceException("Pro vault nebylo nalezeno žádné nadřazené Git repo.");

        var relative = Path.GetRelativePath(repo, vaultPath).Replace('\\', '/');
        var gitIgnorePath = Path.Combine(repo, ".gitignore");
        var existingLines = File.Exists(gitIgnorePath)
            ? File.ReadAllLines(gitIgnorePath).ToList()
            : new List<string>();

        if (existingLines.Any(line => string.Equals(line.Trim(), relative, StringComparison.Ordinal)))
            return new GitIgnoreResult(repo, gitIgnorePath, false, relative);

        existingLines.Add(relative);
        File.WriteAllLines(gitIgnorePath, existingLines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return new GitIgnoreResult(repo, gitIgnorePath, true, relative);
    }

    private static bool IsVaultTrackedByGit(string vaultPath, string repoPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repoPath) || !Directory.Exists(repoPath))
                return false;

            var relativePath = Path.GetRelativePath(repoPath, vaultPath).Replace('\\', '/');
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"ls-files --error-unmatch -- \"{relativePath}\"",
                WorkingDirectory = repoPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return false;

            process.WaitForExit(2000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }


    private static TokenVault CreateProtectedVault(TokenVault vault)
    {
        return new TokenVault
        {
            Version = vault.Version,
            Tokens = vault.Tokens.Select(token => new TokenEntry
            {
                Id = token.Id,
                Value = "",
                ProtectedValue = ProtectString(token.Value),
                Description = token.Description,
                CreatedAt = token.CreatedAt
            }).ToList()
        };
    }

    private static string ProtectString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(CryptProtect(bytes));
    }

    private static string UnprotectString(string protectedValue)
    {
        try
        {
            var bytes = Convert.FromBase64String(protectedValue);
            return Encoding.UTF8.GetString(CryptUnprotect(bytes));
        }
        catch (FormatException ex)
        {
            throw new TokenServiceException($"Vault obsahuje neplatnou DPAPI hodnotu: {ex.Message}");
        }
    }

    private static byte[] CryptProtect(byte[] data) => CryptData(data, protect: true);
    private static byte[] CryptUnprotect(byte[] data) => CryptData(data, protect: false);

    private static byte[] CryptData(byte[] data, bool protect)
    {
        var input = CreateBlob(data);
        var entropy = CreateBlob(DpapiEntropy);
        var output = new DATA_BLOB();
        var description = IntPtr.Zero;

        try
        {
            var ok = protect
                ? CryptProtectData(ref input, "OpenClaw Manager Token Vault", ref entropy, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, ref output)
                : CryptUnprotectData(ref input, out description, ref entropy, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, ref output);

            if (!ok)
                throw new TokenServiceException($"DPAPI operation failed (Win32 error {Marshal.GetLastWin32Error()}).");

            var result = new byte[output.cbData];
            Marshal.Copy(output.pbData, result, 0, result.Length);
            return result;
        }
        finally
        {
            FreeBlob(input);
            FreeBlob(entropy);
            if (output.pbData != IntPtr.Zero)
                LocalFree(output.pbData);
            if (description != IntPtr.Zero)
                LocalFree(description);
        }
    }

    private static DATA_BLOB CreateBlob(byte[] data)
    {
        if (data.Length == 0)
            return new DATA_BLOB();

        var ptr = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, ptr, data.Length);
        return new DATA_BLOB { cbData = data.Length, pbData = ptr };
    }

    private static void FreeBlob(DATA_BLOB blob)
    {
        if (blob.pbData != IntPtr.Zero)
            Marshal.FreeHGlobal(blob.pbData);
    }

    public static string BuildDefaultOutputPath(string inputPath, string marker)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? "";
        var fileName = Path.GetFileName(inputPath);
        var secondDot = fileName.IndexOf('.', 1);

        if (fileName.StartsWith('.') && secondDot < 0)
            return Path.Combine(directory, $"{fileName}.{marker}");

        var extension = Path.GetExtension(fileName);
        var stem = Path.GetFileNameWithoutExtension(fileName);
        return Path.Combine(directory, $"{stem}.{marker}{extension}");
    }

    private static void ValidateVault(TokenVault vault)
    {
        // Odmítnout vault z budoucí verze aplikace (neznámé schéma).
        // Akceptovat starší verze — backward-compatible načtení.
        if (vault.Version > CurrentVaultVersion)
            throw new TokenServiceException(
                $"Vault byl vytvořen novější verzí aplikace (verze schématu {vault.Version}, " +
                $"aktuální {CurrentVaultVersion}). Aktualizuj OpenClaw Manager.");

        foreach (var token in vault.Tokens)
            ValidateToken(token.Id, token.Value);

        var duplicate = vault.Tokens
            .GroupBy(t => t.Id, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
            throw new TokenServiceException($"Duplicitní token ID ve vaultu: {duplicate.Key}");
    }

    private static void ValidateToken(string id, string value)
    {
        if (!IdRegex.IsMatch(id))
            throw new TokenServiceException("ID musí obsahovat jen písmena, číslice a underscore, max. 64 znaků.");

        if (string.IsNullOrEmpty(value) || value.Length < 8)
            throw new TokenServiceException("Hodnota tokenu musí mít alespoň 8 znaků.");
    }

    private static string NormalizeExistingFile(string path)
    {
        path = NormalizePath(path);
        if (!File.Exists(path))
            throw new TokenServiceException($"Soubor neexistuje: {path}");
        return path;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new TokenServiceException("Cesta nesmí být prázdná.");

        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
    }

    private static int CountOccurrences(string text, string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static TextFile ReadUtf8TextFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var hadBom = bytes.Length >= 3 &&
                     bytes[0] == 0xEF &&
                     bytes[1] == 0xBB &&
                     bytes[2] == 0xBF;

        var offset = hadBom ? 3 : 0;
        var text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
            .GetString(bytes, offset, bytes.Length - offset);

        return new TextFile(text, hadBom);
    }

    private static void WriteOutputFile(string path, string text, bool hadBom, bool overwrite)
    {
        if (File.Exists(path) && !overwrite)
            throw new TokenServiceException($"Výstupní soubor už existuje: {path}");

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var body = utf8NoBom.GetBytes(text);

        if (!hadBom)
        {
            File.WriteAllBytes(path, body);
            return;
        }

        var bom = Encoding.UTF8.GetPreamble();
        var output = new byte[bom.Length + body.Length];
        Buffer.BlockCopy(bom, 0, output, 0, bom.Length);
        Buffer.BlockCopy(body, 0, output, bom.Length, body.Length);
        File.WriteAllBytes(path, output);
    }

    private static void AppendAudit(
        string vaultPath,
        string operation,
        string? targetPath,
        int totalCount,
        int uniqueCount,
        IEnumerable<string> tokenIds)
    {
        try
        {
            var directory = Path.GetDirectoryName(NormalizePath(vaultPath));
            if (string.IsNullOrWhiteSpace(directory))
                return;

            Directory.CreateDirectory(directory);
            var auditPath = Path.Combine(directory, "audit.log");
            var ids = string.Join(",", tokenIds.Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal));
            var line = $"{DateTimeOffset.Now:O}\toperation={operation}\ttarget={targetPath ?? "-"}\ttotal={totalCount}\tunique={uniqueCount}\tids={ids}";
            File.AppendAllText(auditPath, line + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch
        {
            // Audit log nesmí zablokovat bezpečnostní operaci nad tokeny.
        }
    }


    private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(
        ref DATA_BLOB pDataIn,
        string? szDataDescr,
        ref DATA_BLOB pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        ref DATA_BLOB pDataOut);

    [DllImport("crypt32.dll", SetLastError = true)]
    private static extern bool CryptUnprotectData(
        ref DATA_BLOB pDataIn,
        out IntPtr ppszDataDescr,
        ref DATA_BLOB pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        ref DATA_BLOB pDataOut);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr hMem);

    private record TextFile(string Text, bool HadUtf8Bom);
    private record RestoreBuildResult(string Text, int Count, IReadOnlyList<string> UniqueIds, IReadOnlyList<string> UnknownPlaceholders);
}
