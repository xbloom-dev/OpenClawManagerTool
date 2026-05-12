using System.Security.Cryptography;
using System.Text;
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
    VaultSafetyDetectsRiskyPaths();
    OpenClawCommandValidationRejectsShellCharacters();

    Console.WriteLine("TokenService tests OK.");
    return 0;
}
finally
{
    if (Directory.Exists(root))
        Directory.Delete(root, recursive: true);
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

void VaultSafetyDetectsRiskyPaths()
{
    var repo = Path.Combine(NewCase(), "repo");
    Directory.CreateDirectory(Path.Combine(repo, ".git"));
    var vaultPath = Path.Combine(repo, ".token-manager", "secrets.json");

    var result = TokenService.AnalyzeVaultPath(vaultPath, repo);
    Assert(!result.IsSafe, "Vault safety should flag repo/OpenClaw locations.");
    Assert(result.Warnings.Count >= 2, "Vault safety should report all relevant warnings.");
}

void OpenClawCommandValidationRejectsShellCharacters()
{
    Assert(GatewayService.TryValidateOpenClawCommand("openclaw", out _), "Plain openclaw command should be valid.");
    Assert(!GatewayService.TryValidateOpenClawCommand("openclaw; calc", out _), "Semicolon should be rejected.");
    Assert(!GatewayService.TryValidateOpenClawCommand("openclaw & calc", out _), "Ampersand should be rejected.");
    Assert(!GatewayService.TryValidateOpenClawCommand("C:\\Tools\\bad\"path.cmd", out _), "Quote should be rejected.");
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

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new Exception(message);
}

