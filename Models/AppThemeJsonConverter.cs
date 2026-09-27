using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenClawManager.Models;

/// <summary>
/// Backward-compatible JSON converter for AppTheme.
/// Accepts legacy values (Modern, Dark, Compact) and optional Theme.* prefix.
/// Writes only canonical enum names.
/// </summary>
public sealed class AppThemeJsonConverter : JsonConverter<AppTheme>
{
    public override AppTheme Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            if (TryMapTheme(raw, out var mapped))
                return mapped;
        }
        else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number))
        {
            if (Enum.IsDefined(typeof(AppTheme), number))
                return (AppTheme)number;
        }

        return AppTheme.Legacy;
    }

    public override void Write(Utf8JsonWriter writer, AppTheme value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());

    private static bool TryMapTheme(string? raw, out AppTheme theme)
    {
        theme = AppTheme.Legacy;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var value = raw.Trim();
        if (value.StartsWith("Theme.", StringComparison.OrdinalIgnoreCase))
            value = value.Substring("Theme.".Length);

        return value.ToLowerInvariant() switch
        {
            "legacy" => Set(AppTheme.Legacy, out theme),
            "standardlight" => Set(AppTheme.StandardLight, out theme),
            "standarddark" => Set(AppTheme.StandardDark, out theme),
            "moderndark" => Set(AppTheme.ModernDark, out theme),
            "modernlight" => Set(AppTheme.ModernLight, out theme),
            "highcontrast" => Set(AppTheme.HighContrast, out theme),
            "crabcute" => Set(AppTheme.CrabCute, out theme),

            // Legacy aliases (migration support).
            "modern" => Set(AppTheme.StandardLight, out theme),
            "dark" => Set(AppTheme.ModernDark, out theme),
            "compact" => Set(AppTheme.CrabCute, out theme),
            _ => false
        };
    }

    private static bool Set(AppTheme value, out AppTheme target)
    {
        target = value;
        return true;
    }
}
