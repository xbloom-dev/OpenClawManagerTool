using OpenClawManager.Models;

namespace OpenClawManager.Services;

internal sealed class TokenServiceAdapter : ITokenService
{
    public string GetDefaultVaultPath() => TokenService.GetDefaultVaultPath();
    public bool EnsureVaultExists(string path) => TokenService.EnsureVaultExists(path);
    public TokenVault LoadVault(string path) => TokenService.LoadVault(path);
    public void SaveVault(string path, TokenVault vault) => TokenService.SaveVault(path, vault);
    public Task ExportVaultAsync(string vaultPath, string filePath, string password) =>
        TokenService.ExportVaultAsync(vaultPath, filePath, password);
    public Task ImportVaultAsync(string vaultPath, string filePath, string password) =>
        TokenService.ImportVaultAsync(vaultPath, filePath, password);
}
