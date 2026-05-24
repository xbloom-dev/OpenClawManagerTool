using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using OpenClawManager.Models;
using OpenClawManager.Services;

var root = Path.Combine(Path.GetTempPath(), "OpenClawManager.TokenService.Tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);

try
{
    RoundTripAndVerify();
    RestoreInPlaceCreatesBackup();
    EditKeepsValueWhenPasswordIsBlank();
    GitIgnoreHelperAddsVaultPath();
    AuditLogDoesNotContainSecretValues();
    VaultIsEncryptedAtRest();
    VaultBackupRoundTripsWithPassword();
    VaultSafetyDetectsRiskyPaths();
    RemoveTokenDeletesEntry();
    RemoveTokenNonexistentThrows();
    RotateTokenChangesValue();
    RotateTokenNonexistentThrows();
    RotateTokenAuditDoesNotContainNewValue();
    UpdateTokenChangesId();
    UpdateTokenWithRealValueChange();
    UpdateTokenNonexistentThrows();
    RedactFileOverwriteProtection();
    RedactFileNoTokensInInput();
    RestoreFileUnknownPlaceholdersReported();
    GitIgnoreIdempotent();
    VaultSafetyReportsSafePath();
    IsVaultTrackedByGitDetectsRepo();
    OpenClawCommandValidationRejectsShellCharacters();
    AppSettingsMigrationFillsMissingValues();

    Console.WriteLine("TokenService tests OK.");
    return 0;
}
finally
{
    if (Directory.Exists(root))
    {
        ResetAttributes(root);
        Directory.Delete(root, recursive: true);
    }
}

void RoundTripAndVerify()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");
    var inputPath = Path.Combine(caseDir, "openclaw.json");
    var redactedPath = TokenService.BuildDefaultOutputPath(inputPath, "redacted");

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "long_token", "abcTOKEN_1234567890XYZ", "Long token");
    TokenService.AddToken(vaultPath, "short_token", "TOKEN_1234567890", "Substring token");
    TokenService.AddToken(vaultPath, "regex_token", @"a.+?b\cTOKEN", "Regex chars");

    var original = "first=abcTOKEN_1234567890XYZ\r\nsecond=TOKEN_1234567890\r\nregex=a.+?b\\cTOKEN\r\n";
    File.WriteAllText(inputPath, original, new UTF8Encoding(false));
    var originalHash = Hash(inputPath);

    var dryRun = TokenService.RedactFile(vaultPath, inputPath, redactedPath, overwrite: true, dryRun: true);
    Assert(dryRun.TotalCount == 3, "Dry-run should count all token occurrences.");
    Assert(!File.Exists(redactedPath), "Dry-run must not write output file.");

    var redact = TokenService.RedactFile(vaultPath, inputPath, redactedPath, overwrite: true);
    Assert(redact.TotalCount == 3 && redact.UniqueCount == 3, "Redact counts should match all tokens.");

    var verifyRedacted = TokenService.VerifyFile(vaultPath, redactedPath);
    Assert(verifyRedacted.IsSafe, "Redacted file should be safe.");

    var verifyOriginal = TokenService.VerifyFile(vaultPath, inputPath);
    Assert(verifyOriginal.FoundTokenIds.Count == 3, "Original file should contain real token values.");

    var restoredPath = TokenService.BuildDefaultOutputPath(redactedPath, "restored");
    var restore = TokenService.RestoreFile(vaultPath, redactedPath, restoredPath, overwrite: true);
    Assert(restore.TotalCount == 3 && restore.UnknownPlaceholders.Count == 0, "Restore should replace all known placeholders.");
    Assert(originalHash == Hash(restoredPath), "Restore output should be byte-identical to original.");

    var stalePath = Path.Combine(caseDir, "stale.txt");
    File.WriteAllText(stalePath, "[REDACTED_missing_token]", new UTF8Encoding(false));
    var verifyStale = TokenService.VerifyFile(vaultPath, stalePath);
    Assert(verifyStale.UnknownPlaceholders.Count == 1, "Verify should detect stale placeholders.");
}

void RestoreInPlaceCreatesBackup()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");
    var filePath = Path.Combine(caseDir, "config.txt");

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "api_key", "secret-value-123", "");
    File.WriteAllText(filePath, "[REDACTED_api_key]\r\n", new UTF8Encoding(false));

    var result = TokenService.RestoreFileInPlace(vaultPath, filePath);
    Assert(File.Exists(filePath + ".bak"), "In-place restore must create .bak file.");
    Assert(result.BackupPath == filePath + ".bak", "Result should report backup path.");
    Assert(File.ReadAllText(filePath).Contains("secret-value-123", StringComparison.Ordinal), "In-place restore should write token value.");
}

void EditKeepsValueWhenPasswordIsBlank()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "api_key", "secret-value-123", "old");
    TokenService.UpdateToken(vaultPath, "api_key", "api_key", "", "new description");

    var token = TokenService.LoadVault(vaultPath).Tokens.Single(t => t.Id == "api_key");
    Assert(token.Value == "secret-value-123", "Blank edit password should keep the original value.");
    Assert(token.Description == "new description", "Edit should update description.");
}

void GitIgnoreHelperAddsVaultPath()
{
    var repo = Path.Combine(NewCase(), "repo");
    Directory.CreateDirectory(Path.Combine(repo, ".git"));
    var vaultPath = Path.Combine(repo, ".token-manager", "secrets.json");

    TokenService.EnsureVaultExists(vaultPath);
    var result = TokenService.AddVaultToGitIgnore(vaultPath);

    Assert(result.Added, "Gitignore helper should add missing vault path.");
    Assert(File.ReadAllText(Path.Combine(repo, ".gitignore")).Contains(".token-manager/secrets.json", StringComparison.Ordinal),
        ".gitignore should contain relative vault path.");
}

void AuditLogDoesNotContainSecretValues()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");
    var filePath = Path.Combine(caseDir, "config.txt");
    var secret = "secret-value-123";

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "api_key", secret, "");
    File.WriteAllText(filePath, secret, new UTF8Encoding(false));
    TokenService.RedactFile(vaultPath, filePath, overwrite: true);

    var audit = File.ReadAllText(Path.Combine(caseDir, "audit.log"));
    Assert(audit.Contains("operation=redact", StringComparison.Ordinal), "Audit log should record redact operation.");
    Assert(!audit.Contains(secret, StringComparison.Ordinal), "Audit log must never contain token values.");
}


void VaultIsEncryptedAtRest()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");
    var secret = "secret-value-123";

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "api_key", secret, "");

    var raw = File.ReadAllText(vaultPath, Encoding.UTF8);
    Assert(!raw.Contains(secret, StringComparison.Ordinal), "Vault file must not contain plaintext token values.");
    Assert(TokenService.IsVaultEncryptedAtRest(vaultPath), "Vault should report encrypted-at-rest after save.");
    Assert(TokenService.LoadVault(vaultPath).Tokens.Single().Value == secret, "Encrypted vault should decrypt transparently.");
}

void VaultBackupRoundTripsWithPassword()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");
    var backupPath = Path.Combine(caseDir, "backup.ocvault");
    var restoredVaultPath = Path.Combine(caseDir, "restored.json");
    var password = "correct horse battery staple";
    var secret = "secret-value-123";

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "api_key", secret, "test backup");

    TokenService.ExportVaultAsync(vaultPath, backupPath, password).GetAwaiter().GetResult();
    Assert(File.Exists(backupPath), "Vault backup file should be created.");
    Assert(!File.ReadAllText(backupPath, Encoding.UTF8).Contains(secret, StringComparison.Ordinal), "Vault backup must not contain plaintext token values.");

    TokenService.EnsureVaultExists(restoredVaultPath);
    TokenService.ImportVaultAsync(restoredVaultPath, backupPath, password).GetAwaiter().GetResult();

    var restored = TokenService.LoadVault(restoredVaultPath);
    var restoredToken = restored.Tokens.Single(t => t.Id == "api_key");
    Assert(restoredToken.Value == secret, "Vault backup import should restore token value.");
    Assert(restoredToken.Description == "test backup", "Vault backup import should restore token metadata.");
    Assert(TokenService.IsVaultEncryptedAtRest(restoredVaultPath), "Imported vault should be encrypted at rest through DPAPI.");

    AssertThrows(() =>
        TokenService.ImportVaultAsync(restoredVaultPath, backupPath, "wrong password").GetAwaiter().GetResult(),
        "Wrong vault backup password must fail.");
}

void VaultSafetyDetectsRiskyPaths()
{
    var repo = Path.Combine(NewCase(), "repo");
    Directory.CreateDirectory(Path.Combine(repo, ".git"));
    var vaultPath = Path.Combine(repo, ".token-manager", "secrets.json");

    var result = TokenService.AnalyzeVaultPath(vaultPath, repo);
    Assert(!result.IsSafe, "Vault safety should flag repo/OpenClaw locations.");
    Assert(result.Warnings.Count >= 2, "Vault safety should report all relevant warnings.");
}

void RemoveTokenDeletesEntry()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "to_remove", "secret-value-remove", "");
    TokenService.AddToken(vaultPath, "keep", "secret-value-keep", "");

    TokenService.RemoveToken(vaultPath, "to_remove");

    var vault = TokenService.LoadVault(vaultPath);
    Assert(vault.Tokens.All(t => t.Id != "to_remove"), "RemoveToken should delete the selected token.");
    Assert(vault.Tokens.Any(t => t.Id == "keep"), "RemoveToken should keep unrelated tokens.");
}

void RemoveTokenNonexistentThrows()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");

    TokenService.EnsureVaultExists(vaultPath);

    AssertThrows(
        () => TokenService.RemoveToken(vaultPath, "nonexistent_id"),
        "RemoveToken on nonexistent ID should throw.");
}

void RotateTokenChangesValue()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "api_key", "old-secret-value", "");
    TokenService.RotateToken(vaultPath, "api_key", "new-secret-value");

    var token = TokenService.LoadVault(vaultPath).Tokens.Single();
    Assert(token.Id == "api_key", "RotateToken should not change token ID.");
    Assert(token.Value == "new-secret-value", "RotateToken should change token value.");
}

void RotateTokenNonexistentThrows()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");

    TokenService.EnsureVaultExists(vaultPath);

    AssertThrows(
        () => TokenService.RotateToken(vaultPath, "missing_id", "new-secret-value"),
        "RotateToken on nonexistent ID should throw.");
}

void RotateTokenAuditDoesNotContainNewValue()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");
    var newSecret = "new-secret-value";

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "api_key", "old-secret-value", "");
    TokenService.RotateToken(vaultPath, "api_key", newSecret);

    var audit = File.ReadAllText(Path.Combine(caseDir, "audit.log"));
    Assert(audit.Contains("operation=rotate", StringComparison.Ordinal), "Audit log should record rotate operation.");
    Assert(!audit.Contains(newSecret, StringComparison.Ordinal), "Audit log must not contain rotated token values.");
}

void UpdateTokenChangesId()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "old_id", "secret-value-123", "desc");
    TokenService.UpdateToken(vaultPath, "old_id", "new_id", "", "desc");

    var vault = TokenService.LoadVault(vaultPath);
    Assert(vault.Tokens.All(t => t.Id != "old_id"), "UpdateToken should remove the old ID.");
    Assert(vault.Tokens.Single(t => t.Id == "new_id").Value == "secret-value-123", "Blank update value should keep the old value.");
}

void UpdateTokenWithRealValueChange()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "api_key", "original-secret", "");
    TokenService.UpdateToken(vaultPath, "api_key", "api_key", "updated-secret", "");

    Assert(TokenService.LoadVault(vaultPath).Tokens.Single().Value == "updated-secret", "UpdateToken should persist a new token value.");
}

void UpdateTokenNonexistentThrows()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");

    TokenService.EnsureVaultExists(vaultPath);

    AssertThrows(
        () => TokenService.UpdateToken(vaultPath, "missing_id", "missing_id", "", ""),
        "UpdateToken on nonexistent ID should throw.");
}

void RedactFileOverwriteProtection()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");
    var inputPath = Path.Combine(caseDir, "input.txt");
    var outputPath = Path.Combine(caseDir, "output.txt");

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "tok", "secret-value", "");
    File.WriteAllText(inputPath, "secret-value", new UTF8Encoding(false));
    File.WriteAllText(outputPath, "existing content", new UTF8Encoding(false));

    AssertThrows(
        () => TokenService.RedactFile(vaultPath, inputPath, outputPath, overwrite: false),
        "RedactFile without overwrite should throw when output exists.");
    Assert(File.ReadAllText(outputPath) == "existing content", "RedactFile should not change an existing output file without overwrite.");
}

void RedactFileNoTokensInInput()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");
    var inputPath = Path.Combine(caseDir, "input.txt");
    var outputPath = Path.Combine(caseDir, "output.txt");
    var content = "no tokens here at all";

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "tok", "secret-value", "");
    File.WriteAllText(inputPath, content, new UTF8Encoding(false));

    var result = TokenService.RedactFile(vaultPath, inputPath, outputPath, overwrite: true);

    Assert(result.TotalCount == 0, "RedactFile should report zero matches when no tokens are present.");
    Assert(File.ReadAllText(outputPath) == content, "RedactFile should preserve content when no tokens are present.");
}

void RestoreFileUnknownPlaceholdersReported()
{
    var caseDir = NewCase();
    var vaultPath = Path.Combine(caseDir, "secrets.json");
    var redactedPath = Path.Combine(caseDir, "redacted.txt");
    var restoredPath = Path.Combine(caseDir, "restored.txt");

    TokenService.EnsureVaultExists(vaultPath);
    TokenService.AddToken(vaultPath, "known", "real-value", "");
    File.WriteAllText(redactedPath, "[REDACTED_known]\r\n[REDACTED_unknown_token]\r\n", new UTF8Encoding(false));

    var result = TokenService.RestoreFile(vaultPath, redactedPath, restoredPath, overwrite: true);
    var restored = File.ReadAllText(restoredPath);

    Assert(result.UnknownPlaceholders.Count == 1, "RestoreFile should report unknown placeholders.");
    Assert(result.UnknownPlaceholders[0].Contains("unknown_token", StringComparison.Ordinal), "Unknown placeholder should identify the missing token.");
    Assert(restored.Contains("real-value", StringComparison.Ordinal), "RestoreFile should restore known placeholders.");
    Assert(restored.Contains("[REDACTED_unknown_token]", StringComparison.Ordinal), "RestoreFile should keep unknown placeholders unchanged.");
}

void GitIgnoreIdempotent()
{
    var repo = Path.Combine(NewCase(), "repo");
    Directory.CreateDirectory(Path.Combine(repo, ".git"));
    var vaultPath = Path.Combine(repo, ".token-manager", "secrets.json");

    TokenService.EnsureVaultExists(vaultPath);

    var first = TokenService.AddVaultToGitIgnore(vaultPath);
    var second = TokenService.AddVaultToGitIgnore(vaultPath);
    var content = File.ReadAllText(Path.Combine(repo, ".gitignore"));

    Assert(first.Added, "First AddVaultToGitIgnore call should add the vault path.");
    Assert(!second.Added, "Second AddVaultToGitIgnore call should be idempotent.");
    Assert(CountOccurrences(content, ".token-manager/secrets.json") == 1, "Gitignore helper should not add duplicate vault paths.");
}

void VaultSafetyReportsSafePath()
{
    var safeDir = NewCase();
    var vaultPath = Path.Combine(safeDir, "secrets.json");
    var openClawPath = Path.Combine(safeDir, "different_openclaw");

    var result = TokenService.AnalyzeVaultPath(vaultPath, openClawPath);

    Assert(result.IsSafe, "Vault safety should mark unrelated non-repo local paths as safe.");
    Assert(result.Warnings.Count == 0, "Safe vault paths should not report warnings.");
}

void IsVaultTrackedByGitDetectsRepo()
{
    var repo = Path.Combine(NewCase(), "repo");
    Directory.CreateDirectory(repo);
    RunGit(repo, "init");

    var trackedVaultPath = Path.Combine(repo, "secrets.json");
    File.WriteAllText(trackedVaultPath, "{}", new UTF8Encoding(false));
    RunGit(repo, "add secrets.json");

    Assert(TokenService.IsVaultTrackedByGit(trackedVaultPath), "IsVaultTrackedByGit should detect files tracked by Git.");

    var safeDir = NewCase();
    Assert(!TokenService.IsVaultTrackedByGit(Path.Combine(safeDir, "secrets.json")), "IsVaultTrackedByGit should be false outside Git repositories.");
}

void OpenClawCommandValidationRejectsShellCharacters()
{
    var gatewayService = new GatewayService(new SettingsService(), new ProcessDetector());

    Assert(gatewayService.TryValidateOpenClawCommand("openclaw", out _), "Plain openclaw command should be valid.");
    Assert(!gatewayService.TryValidateOpenClawCommand("openclaw; calc", out _), "Semicolon should be rejected.");
    Assert(!gatewayService.TryValidateOpenClawCommand("openclaw & calc", out _), "Ampersand should be rejected.");
    Assert(!gatewayService.TryValidateOpenClawCommand("openclaw | more", out _), "Pipe should be rejected.");
    Assert(!gatewayService.TryValidateOpenClawCommand("openclaw < input.txt", out _), "Input redirect should be rejected.");
    Assert(!gatewayService.TryValidateOpenClawCommand("openclaw > output.txt", out _), "Output redirect should be rejected.");
    Assert(!gatewayService.TryValidateOpenClawCommand("openclaw %TEMP%", out _), "Environment variable expansion should be rejected.");
    Assert(!gatewayService.TryValidateOpenClawCommand("openclaw ^& calc", out _), "Caret escaping should be rejected.");
    Assert(!gatewayService.TryValidateOpenClawCommand("C:\\Tools\\bad\"path.cmd", out _), "Quote should be rejected.");
}

void AppSettingsMigrationFillsMissingValues()
{
    var settings = new AppSettings
    {
        SchemaVersion = 0,
        OpenClawPath = "",
        TempPath = "",
        OpenClawCommand = "",
        PowerShellWorkingDir = null!,
        CleanupAgents = null!,
        TokenManagerSecretsPath = "",
        Language = "en"
    };

    var migrated = new SettingsService().MigrateSettings(settings, out var changed);

    Assert(changed, "Migration should report changes for legacy settings.");
    Assert(migrated.SchemaVersion == AppSettings.CurrentSchemaVersion, "Migration should set current schema version.");
    Assert(!string.IsNullOrWhiteSpace(migrated.OpenClawPath), "Migration should fill OpenClaw path.");
    Assert(!string.IsNullOrWhiteSpace(migrated.TempPath), "Migration should fill temp path.");
    Assert(!string.IsNullOrWhiteSpace(migrated.OpenClawCommand), "Migration should fill OpenClaw command.");
    Assert(migrated.PowerShellWorkingDir != null, "Migration should fill PowerShell working directory.");
    Assert(migrated.CleanupAgents.Count > 0, "Migration should fill cleanup agents.");
    Assert(!string.IsNullOrWhiteSpace(migrated.TokenManagerSecretsPath), "Migration should fill token vault path.");
    Assert(migrated.Language == "EN", "Migration should normalize language.");
}

string NewCase()
{
    var path = Path.Combine(root, Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(path);
    return path;
}

static string Hash(string path)
{
    using var sha = SHA256.Create();
    return Convert.ToHexString(sha.ComputeHash(File.ReadAllBytes(path)));
}

static int CountOccurrences(string text, string value)
{
    var count = 0;
    var index = 0;
    while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
    {
        count++;
        index += value.Length;
    }

    return count;
}

static void RunGit(string workingDirectory, string arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = "git",
        Arguments = arguments,
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardError = true,
        RedirectStandardOutput = true
    };

    using var process = Process.Start(psi) ?? throw new Exception("git process could not be started.");
    process.WaitForExit(5000);
    if (process.ExitCode != 0)
    {
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        throw new Exception($"git {arguments} failed. {output}{error}");
    }
}

static void ResetAttributes(string path)
{
    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        File.SetAttributes(file, FileAttributes.Normal);

    foreach (var directory in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
        File.SetAttributes(directory, FileAttributes.Normal);
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new Exception(message);
}

static void AssertThrows(Action action, string message)
{
    try
    {
        action();
    }
    catch
    {
        return;
    }

    throw new Exception(message);
}

