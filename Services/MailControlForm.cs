using System.Globalization;
using System.Text.RegularExpressions;
using OpenClawManager.Models;

namespace OpenClawManager.Services;

/// <summary>Typ ovládacího prvku, který se vykreslí pro jedno nastavení ze schématu.</summary>
public enum SettingControlKind
{
    Toggle,
    Integer,
    Text,
    Choice
}

/// <summary>
/// Popis jednoho pole formuláře odvozený výhradně ze schématu backendu
/// (nic o konkrétních klíčích nesmí být zadané natvrdo).
/// </summary>
public sealed record SettingFieldSpec(
    string Key,
    string Group,
    string Label,
    string Description,
    SettingControlKind Kind,
    long? Min,
    long? Max,
    int? MaxLength,
    string Unit,
    IReadOnlyList<string> Choices,
    string Pattern,
    bool RequiresConfirmation,
    bool HasSafeValue,
    object? SafeValue,
    object? Default);

public static class MailControlForm
{
    /// <summary>Skupiny v pořadí ze schématu; nastavení bez skupiny jdou do poslední.</summary>
    public static IReadOnlyList<(string Group, IReadOnlyList<SettingFieldSpec> Fields)> BuildGroups(
        IEnumerable<SettingDefinition> settings,
        IEnumerable<string>? groupOrder = null)
    {
        var specs = settings.Select(BuildSpec).ToList();
        var order = new List<string>(groupOrder ?? Array.Empty<string>());
        foreach (var g in specs.Select(s => s.Group))
        {
            if (!order.Contains(g))
                order.Add(g);
        }

        return order
            .Select(g => (g, (IReadOnlyList<SettingFieldSpec>)specs.Where(s => s.Group == g).ToList()))
            .Where(x => x.Item2.Count > 0)
            .ToList();
    }

    public static SettingFieldSpec BuildSpec(SettingDefinition s)
    {
        var kind = s.Type switch
        {
            "bool" => SettingControlKind.Toggle,
            "int" => SettingControlKind.Integer,
            "enum" when s.EnumValues.Count > 0 => SettingControlKind.Choice,
            _ => SettingControlKind.Text
        };

        long? min = kind == SettingControlKind.Integer && s.Min.HasValue ? (long)s.Min.Value : null;
        long? max = kind == SettingControlKind.Integer && s.Max.HasValue ? (long)s.Max.Value : null;
        // u textu je "max" nejvyšší délka (viz sender_name, signature)
        int? maxLength = kind == SettingControlKind.Text && s.Max.HasValue ? (int)s.Max.Value : null;

        return new SettingFieldSpec(
            s.Key,
            s.Group,
            s.Label,
            s.Description,
            kind,
            min,
            max,
            maxLength,
            s.Unit,
            s.EnumValues,
            s.Pattern,
            s.IsConfirmTier,
            s.HasSafeValue,
            s.SafeValue,
            s.Default);
    }

    /// <summary>
    /// Je cílová hodnota přesunem od bezpečné hodnoty? (Jen pro varování v UI před voláním;
    /// vlastní rozhodnutí o potvrzení dělá backend.)
    /// </summary>
    public static bool MovesAwayFromSafeValue(SettingFieldSpec spec, object? newValue) =>
        spec.RequiresConfirmation && spec.HasSafeValue && !ValuesEqual(spec.SafeValue, newValue);

    public static bool ValuesEqual(object? a, object? b)
    {
        if (a is null || b is null)
            return a is null && b is null;

        if (a is bool ba && b is bool bb)
            return ba == bb;

        if (IsNumber(a) && IsNumber(b))
            return Convert.ToDouble(a, CultureInfo.InvariantCulture) == Convert.ToDouble(b, CultureInfo.InvariantCulture);

        return string.Equals(Convert.ToString(a, CultureInfo.InvariantCulture), Convert.ToString(b, CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    private static bool IsNumber(object o) => o is long or int or double or decimal or float;

    /// <summary>
    /// Klientské ověření vstupu (jen pro okamžitou zpětnou vazbu; backend ověřuje znovu).
    /// Vrací null, když je vše v pořádku, jinak klíč lokalizované chyby.
    /// </summary>
    public static string? Validate(SettingFieldSpec spec, string? input, out object? value)
    {
        value = null;
        switch (spec.Kind)
        {
            case SettingControlKind.Toggle:
                if (!bool.TryParse(input, out var b))
                    return "Str_MailControl_Val_Invalid";
                value = b;
                return null;

            case SettingControlKind.Integer:
                if (!long.TryParse(input?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                    return "Str_MailControl_Val_NotInteger";
                if ((spec.Min.HasValue && n < spec.Min.Value) || (spec.Max.HasValue && n > spec.Max.Value))
                    return "Str_MailControl_Val_Range";
                value = n;
                return null;

            case SettingControlKind.Choice:
                if (input is null || !spec.Choices.Contains(input))
                    return "Str_MailControl_Val_Invalid";
                value = input;
                return null;

            default:
                var text = input ?? "";
                if (spec.MaxLength.HasValue && text.Length > spec.MaxLength.Value)
                    return "Str_MailControl_Val_TooLong";
                if (!string.IsNullOrEmpty(spec.Pattern))
                {
                    try
                    {
                        if (!Regex.IsMatch(text, spec.Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200)))
                            return "Str_MailControl_Val_Pattern";
                    }
                    catch (ArgumentException)
                    {
                        // neplatný vzor z backendu: ověření přenechat backendu
                    }
                    catch (RegexMatchTimeoutException)
                    {
                    }
                }

                value = text;
                return null;
        }
    }

    /// <summary>Hodnota pro zobrazení v buňce/popisku (prostý text).</summary>
    public static string Format(object? value) => value switch
    {
        null => "",
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => PlainText.Sanitize(value.ToString(), 400)
    };
}

/// <summary>Mapování chybových kódů backendu na klíče lokalizace.</summary>
public static class MailControlErrors
{
    private static readonly HashSet<string> Known = new(StringComparer.Ordinal)
    {
        "unknown_command", "bad_request", "invalid_value", "locked", "read_only", "needs_confirmation",
        "declined", "not_found", "conflict", "refused", "busy", "unauthorized", "forbidden", "too_large",
        "internal", "integrity",
        MailControlError.RunnerUnavailable, MailControlError.StartFailed, MailControlError.Timeout,
        MailControlError.Cancelled, MailControlError.BadOutput
    };

    /// <summary>Klíč lokalizovaného vysvětlení kódu; neznámý kód dostane obecný text.</summary>
    public static string KeyFor(string code) =>
        Known.Contains(code) ? "Str_MailControl_Err_" + code : "Str_MailControl_Err_unknown";

    /// <summary>Chyba, kvůli které se změna jen nezapsala a uživatel ji má vidět jako „zamítnuto“.</summary>
    public static bool IsDeclined(string code) => code is "declined" or "needs_confirmation";

    public static bool IsLocked(string code) => code is "locked" or "read_only";
}
