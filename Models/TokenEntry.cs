using System.Text.Json.Serialization;

namespace OpenClawManager.Models;

public class TokenEntry
{
    public string Id { get; set; } = "";
    public string Value { get; set; } = "";
    public string ProtectedValue { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public bool IsProtectedAtRest => !string.IsNullOrWhiteSpace(ProtectedValue);

    [JsonIgnore]
    public string Placeholder => $"[REDACTED_{Id}]";

    [JsonIgnore]
    public string ValuePreview
    {
        get
        {
            if (Value.Length <= 6)
                return "********";

            return $"{Value[..4]}...{Value[^2..]}";
        }
    }
}
