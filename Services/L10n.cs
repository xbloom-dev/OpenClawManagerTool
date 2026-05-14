using System.Windows;

namespace OpenClawManager.Services;

/// <summary>
/// Správa lokalizace — přepíná ResourceDictionary v App.Resources.
///
/// Použití:
///   L10n.Apply(Language.CS);  // přepne na češtinu
///   string s = L10n.Get("Str_BtnCleaningTool");  // "Vyčistit soubory"
///
/// V XAML není potřeba DynamicResource — texty se aplikují přes ApplyLocalization()
/// v code-behind při inicializaci a po přepnutí jazyka.
/// </summary>
public static class L10n
{
    public enum Language { EN, CS }

    public static Language Current { get; private set; } = Language.CS;
    public static bool IsCzech => Current == Language.CS;

    private const string EN_URI = "Resources/Lang/Strings.en.xaml";
    private const string CS_URI = "Resources/Lang/Strings.cs.xaml";

    /// <summary>
    /// Aplikuje vybraný jazyk — vymění ResourceDictionary v App.Resources.
    /// Volá se při startu a po změně jazyka v Settings.
    /// </summary>
    public static void Apply(Language lang)
    {
        Current = lang;
        var uri = new Uri(lang == Language.CS ? CS_URI : EN_URI, UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };

        // Najít a nahradit stávající jazykový dictionary
        var existing = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null &&
                (d.Source.OriginalString.Contains("Strings.en") ||
                 d.Source.OriginalString.Contains("Strings.cs")));

        if (existing != null)
            Application.Current.Resources.MergedDictionaries.Remove(existing);

        Application.Current.Resources.MergedDictionaries.Add(dict);
    }

    /// <summary>
    /// Získá přeložený string podle klíče.
    /// Pokud klíč neexistuje, vrátí klíč jako fallback (nikdy nevyhodí výjimku).
    /// </summary>
    public static string Get(string key)
    {
        try
        {
            return Application.Current.Resources[key] as string ?? key;
        }
        catch
        {
            return key;
        }
    }

    public static string Format(string key, params object[] args)
    {
        try
        {
            return string.Format(Get(key), args);
        }
        catch
        {
            return key;
        }
    }
}
