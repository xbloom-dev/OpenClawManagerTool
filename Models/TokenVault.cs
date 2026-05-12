namespace OpenClawManager.Models;

public class TokenVault
{
    public int Version { get; set; } = 1;
    public List<TokenEntry> Tokens { get; set; } = new();
}
