// Views/WelcomeWindow.xaml.cs
// Onboarding welcome screen — zobrazí se při prvním spuštění aplikace.
// Scope: jen UI behavior; žádné změny v ThemeService, installeru ani CI.

using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class WelcomeWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IAppEnvironment  _environment;

    private AppTheme _selectedTheme;
    private bool     _videoLoaded;

    // ── Interní theme item pro ComboBox ──────────────────────────────────────

    private sealed record ThemeItem(AppTheme Theme, string DisplayName)
    {
        // ComboBox zobrazuje DisplayName přes ToString()
        public override string ToString() => DisplayName;
    }

    // ── Konstruktor ──────────────────────────────────────────────────────────

    public WelcomeWindow(ISettingsService settingsService, IAppEnvironment environment)
    {
        InitializeComponent();

        _settingsService = settingsService;
        _environment     = environment;
        _selectedTheme   = settingsService.Settings.Theme;

        Loaded   += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PopulateThemeComboBox();  // ComboBox.SelectionChanged spustí UpdateCtaGradient()
        TryStartVideo();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopVideo();
    }

    // ── ComboBox: výběr tématu ────────────────────────────────────────────────

    private void PopulateThemeComboBox()
    {
        var items = new[]
        {
            new ThemeItem(AppTheme.Dark,         "Moderní tmavé"),
            new ThemeItem(AppTheme.ModernLight,  "Moderní světlé"),
            new ThemeItem(AppTheme.Modern,       "Standardní"),
            new ThemeItem(AppTheme.StandardDark, "Standard tmavé"),
            new ThemeItem(AppTheme.CrabCute,     "CrabCute"),
            new ThemeItem(AppTheme.HighContrast, "Vysoký kontrast"),
            new ThemeItem(AppTheme.Legacy,       "Legacy"),
        };

        CmbTheme.ItemsSource   = items;
        // Preferuj aktuální téma ze settings; jinak první v seznamu
        CmbTheme.SelectedItem  = items.FirstOrDefault(t => t.Theme == _selectedTheme)
                                 ?? items[0];
    }

    private void CmbTheme_SelectionChanged(object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CmbTheme.SelectedItem is not ThemeItem item) return;

        _selectedTheme = item.Theme;

        // Live preview — ResourceDictionary swap; MainWindow ještě neexistuje,
        // takže ThemeChanged event nemá žádné handlery → bezpečné.
        ThemeService.Apply(_selectedTheme);

        // Aktualizuj gradient CTA tlačítka dle nových accent tokenů
        UpdateCtaGradient();

        // Aktualizuj caption karty náhledu
        TxtPreviewCaption.Text = item.DisplayName.ToUpperInvariant();
    }

    // ── CTA gradient ─────────────────────────────────────────────────────────
    // Gradient se nastavuje kódem, protože accent tokeny nejsou ve všech tématech
    // definovány — témata Legacy/Modern/Standard nemají Theme.Color.AccentTop/Bot.

    private void UpdateCtaGradient()
    {
        var topColor = Application.Current.TryFindResource("Theme.Color.AccentTop") is Color t
            ? t : FallbackAccentTop(_selectedTheme);
        var botColor = Application.Current.TryFindResource("Theme.Color.AccentBot") is Color b
            ? b : FallbackAccentBot(_selectedTheme);

        var grad = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint   = new System.Windows.Point(0, 1),
        };
        grad.GradientStops.Add(new GradientStop(topColor, 0));
        grad.GradientStops.Add(new GradientStop(botColor, 1));
        BtnLaunch.Background = grad;
    }

    private static Color FallbackAccentTop(AppTheme theme) => theme switch
    {
        AppTheme.CrabCute     => Color.FromRgb(0xFB, 0x71, 0x85),
        AppTheme.HighContrast => Color.FromRgb(0xFE, 0xF0, 0x8A),
        AppTheme.Legacy       => Color.FromRgb(0xFB, 0xBF, 0x24),
        _                     => Color.FromRgb(0x5A, 0xA1, 0xFF),
    };

    private static Color FallbackAccentBot(AppTheme theme) => theme switch
    {
        AppTheme.CrabCute     => Color.FromRgb(0xBE, 0x12, 0x3C),
        AppTheme.HighContrast => Color.FromRgb(0xCA, 0x8A, 0x04),
        AppTheme.Legacy       => Color.FromRgb(0x92, 0x40, 0x0E),
        _                     => Color.FromRgb(0x1D, 0x4E, 0xD8),
    };

    // ── Video splash ─────────────────────────────────────────────────────────

    private void TryStartVideo()
    {
        // MediaElement potřebuje file path — pack:// URI není podporované.
        var pathInRes  = Path.Combine(_environment.AppBaseDirectory, "Resources", "splash.mp4");
        var pathInRoot = Path.Combine(_environment.AppBaseDirectory, "splash.mp4");

        var mp4Path = File.Exists(pathInRes)  ? pathInRes
                    : File.Exists(pathInRoot) ? pathInRoot
                    : null;

        if (mp4Path == null) return;   // PNG fallback zůstane viditelný

        SplashVideo.Source = new Uri(mp4Path);
        SplashVideo.Play(); // Queued — spustí se po MediaOpened
    }

    private void SplashVideo_MediaOpened(object sender, RoutedEventArgs e)
    {
        _videoLoaded              = true;
        SplashVideo.Visibility    = Visibility.Visible;
        SplashImage.Visibility    = Visibility.Hidden;  // PNG schováno pod videem
    }

    private void SplashVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        // Video selhalo → PNG fallback
        SplashVideo.Visibility = Visibility.Collapsed;
        SplashImage.Visibility = Visibility.Visible;
    }

    private void SplashVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        // Smyčka videa
        SplashVideo.Position = TimeSpan.Zero;
        SplashVideo.Play();
    }

    private void SplashImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        // splash.png chybí → window background (#0B0C10) je finální fallback
        SplashImage.Visibility = Visibility.Collapsed;
    }

    private void StopVideo()
    {
        if (!_videoLoaded) return;
        try
        {
            SplashVideo.Stop();
            SplashVideo.Source = null;
        }
        catch { /* okno se uvolňuje — ignorujeme */ }
    }

    // ── Ovládání okna ────────────────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void BtnClose_Click(object sender, RoutedEventArgs e)
        => Close();

    // ── Spuštění aplikace ────────────────────────────────────────────────────

    private void BtnLaunch_Click(object sender, RoutedEventArgs e)
    {
        // Uložit vybrané téma
        var settings = _settingsService.Settings;
        settings.Theme = _selectedTheme;
        _settingsService.Save(settings);

        // Zajistit, že je správné téma aktivní
        ThemeService.Apply(_selectedTheme);

        // TODO: Codex wires first-run completion flag here (IsNewSettingsFile / CheckUpdatesOnStartup)

        // Otevřít MainWindow a uzavřít Welcome screen
        var mainWindow = App.GetService<MainWindow>();
        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();

        Close();
    }
}
