using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class WelcomeWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IAppEnvironment _environment;
    private IReadOnlyList<ThemeChoice> _themes;
    private bool _completed;

    public WelcomeWindow(ISettingsService settingsService, IAppEnvironment environment)
    {
        _settingsService = settingsService;
        _environment = environment;
        _themes = BuildThemeChoices();

        InitializeComponent();

        FooterVersionText.Text = $"{AppVersionInfo.Display} - build {DateTime.Now:yyyy.MM.dd} - DPAPI";
        Title = $"OpenClaw Manager Tool by Bloom {AppVersionInfo.Display}";

        ApplyWelcomeLocalization();
        RefreshThemeChoices(settingsService.Settings.Theme);
        AutoUpdateCheckBox.IsChecked = settingsService.Settings.CheckUpdatesOnStartup;

        TryStartSplashVideo();
    }

    private static IReadOnlyList<ThemeChoice> BuildThemeChoices() =>
        new ThemeChoice[]
        {
            new(AppTheme.Legacy, ThemeDisplayName(AppTheme.Legacy), "legacy"),
            new(AppTheme.StandardLight, ThemeDisplayName(AppTheme.StandardLight), "standard"),
            new(AppTheme.StandardDark, ThemeDisplayName(AppTheme.StandardDark), "standard-dark"),
            new(AppTheme.ModernDark, ThemeDisplayName(AppTheme.ModernDark), "modern-dark"),
            new(AppTheme.ModernLight, ThemeDisplayName(AppTheme.ModernLight), "modern"),
            new(AppTheme.HighContrast, ThemeDisplayName(AppTheme.HighContrast), "contrast"),
            new(AppTheme.CrabCute, ThemeDisplayName(AppTheme.CrabCute), "crab-cute"),
        }.Where(t => ThemeService.IsThemeAvailable(t.Theme)).ToList();

    private static string ThemeDisplayName(AppTheme theme)
    {
        if (L10n.IsCzech)
        {
            return theme switch
            {
                AppTheme.Legacy => "Legacy (v\u00fdchoz\u00ed)",
                AppTheme.StandardLight => "Standardn\u00ed",
                AppTheme.StandardDark => "Standardn\u00ed tmav\u00e9",
                AppTheme.ModernDark => "Modern\u00ed tmav\u00e9",
                AppTheme.ModernLight => "Modern\u00ed",
                AppTheme.HighContrast => "Vysok\u00fd kontrast",
                AppTheme.CrabCute => "Crab Cute",
                _ => theme.ToString()
            };
        }

        return theme switch
        {
            AppTheme.Legacy => "Legacy",
            AppTheme.StandardLight => "Standard",
            AppTheme.StandardDark => "Standard Dark",
            AppTheme.ModernDark => "Modern Dark",
            AppTheme.ModernLight => "Modern",
            AppTheme.HighContrast => "High Contrast",
            AppTheme.CrabCute => "Crab Cute",
            _ => theme.ToString()
        };
    }

    private void RefreshThemeChoices(AppTheme selectedTheme)
    {
        _themes = BuildThemeChoices();
        ThemeCombo.ItemsSource = _themes;
        ThemeCombo.SelectedItem = _themes.FirstOrDefault(t => t.Theme == selectedTheme) ?? _themes[0];

        if (ThemeCombo.SelectedItem is ThemeChoice choice)
        {
            UpdateThemePreview(choice);
            UpdateLaunchButtonPalette(choice.Theme);
        }
    }

    private void ApplyWelcomeLocalization()
    {
        WelcomeSubtitleText.Text = L10n.IsCzech ? "by Bloom - UV\u00cdTAC\u00cd OBRAZOVKA" : "by Bloom - WELCOME SCREEN";
        ThemeLabelText.Text = L10n.IsCzech ? "VYBERTE T\u00c9MA" : "SELECT THEME";
        AutoUpdateText.Text = L10n.IsCzech ? "Kontrolovat aktualizace p\u0159i startu" : "Check for updates on startup";
        ContinueButton.Content = L10n.IsCzech ? "Spustit aplikaci" : "Launch application";
        HelpText.Text = L10n.IsCzech ? "N\u00e1pov\u011bda" : "Help";
        DpapiWarningText.Text = L10n.IsCzech
            ? "Token vault je chr\u00e1n\u011bn\u00fd p\u0159es Windows DPAPI."
            : "The token vault is protected by Windows DPAPI.";

        LanguageCsButton.Opacity = L10n.IsCzech ? 1.0 : 0.55;
        LanguageEnButton.Opacity = L10n.IsCzech ? 0.55 : 1.0;
    }

    private void ApplyWelcomeLanguage(L10n.Language language)
    {
        var selectedTheme = ThemeCombo.SelectedItem is ThemeChoice choice
            ? choice.Theme
            : _settingsService.Settings.Theme;

        L10n.Apply(language);
        ApplyWelcomeLocalization();
        RefreshThemeChoices(selectedTheme);
    }

    private void ThemeCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is not ThemeChoice choice)
            return;

        ThemeService.Apply(choice.Theme);
        UpdateThemePreview(choice);
        UpdateLaunchButtonPalette(choice.Theme);
    }

    private async void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is not ThemeChoice choice)
            return;

        ContinueButton.IsEnabled = false;
        var draft = BuildSettings(choice.Theme, AutoUpdateCheckBox.IsChecked == true);
        if (!_settingsService.Save(draft))
        {
            ContinueButton.IsEnabled = true;
            MessageBox.Show(
                L10n.Format("Str_Error_SaveSettings", _settingsService.SettingsFilePath),
                "OpenClaw Manager Tool",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _completed = true;
        StopSplashVideo();
        await FadeAsync(this, 1, 0, TimeSpan.FromMilliseconds(300));

        var main = App.GetService<MainWindow>();
        Application.Current.MainWindow = main;
        main.Opacity = 0;
        main.Show();
        _ = FadeAsync(main, 0, 1, TimeSpan.FromMilliseconds(300));
        Close();
    }

    private AppSettings BuildSettings(AppTheme theme, bool checkUpdates)
    {
        var current = _settingsService.Settings;
        return new AppSettings
        {
            SchemaVersion = current.SchemaVersion,
            OpenClawPath = current.OpenClawPath,
            TempPath = current.TempPath,
            OpenClawCommand = current.OpenClawCommand,
            PowerShellWorkingDir = current.PowerShellWorkingDir,
            CleanupAgents = new List<string>(current.CleanupAgents),
            TokenManagerSecretsPath = current.TokenManagerSecretsPath,
            Language = L10n.Current.ToString(),
            AutoScrollAppLog = current.AutoScrollAppLog,
            CheckUpdatesOnStartup = checkUpdates,
            Theme = theme,
            UseSplashVideo = current.UseSplashVideo,
            UseButtonScanlineEffect = current.UseButtonScanlineEffect
        };
    }

    private string? ResolveSplashVideoPath()
    {
        var pathInResources = Path.Combine(_environment.AppBaseDirectory, "Resources", "splash.mp4");
        if (File.Exists(pathInResources))
            return pathInResources;

        var pathInRoot = Path.Combine(_environment.AppBaseDirectory, "splash.mp4");
        return File.Exists(pathInRoot) ? pathInRoot : null;
    }

    private void TryStartSplashVideo()
    {
        var videoPath = ResolveSplashVideoPath();
        if (videoPath == null)
            return;

        SplashVideo.Source = new Uri(videoPath);
        SplashVideo.Visibility = Visibility.Visible;
        SplashVideo.Volume = 0.55;
        SplashVideo.IsMuted = false;
        SplashVideo.Play();
    }

    private void ReplaySplash_Click(object sender, RoutedEventArgs e)
    {
        var videoPath = ResolveSplashVideoPath();
        if (videoPath == null)
            return;

        try
        {
            if (SplashVideo.Source == null || !Uri.TryCreate(videoPath, UriKind.Absolute, out var uri) || SplashVideo.Source != uri)
                SplashVideo.Source = new Uri(videoPath);

            SplashVideo.Visibility = Visibility.Visible;
            SplashVideo.Position = TimeSpan.Zero;
            SplashVideo.Volume = 0.55;
            SplashVideo.IsMuted = false;
            SplashVideo.Play();
        }
        catch
        {
        }
    }

    private void SplashVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (SplashVideo.NaturalDuration.HasTimeSpan)
            {
                var finalFrame = SplashVideo.NaturalDuration.TimeSpan - TimeSpan.FromMilliseconds(80);
                if (finalFrame > TimeSpan.Zero)
                    SplashVideo.Position = finalFrame;
            }

            SplashVideo.Pause();
        }
        catch
        {
        }
    }

    private void SplashVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        StopSplashVideo();
        SplashVideo.Visibility = Visibility.Collapsed;
    }

    private void SplashFallback_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        SplashFallback.Visibility = Visibility.Collapsed;
    }

    private void UpdateThemePreview(ThemeChoice choice)
    {
        ThemePreviewName.Text = choice.DisplayName;
        ThemePreviewCaption.Text = choice.Caption;
        ThemePreviewFooter.Text = choice.Theme.ToString().ToUpperInvariant();

        var previewPath = Path.Combine(_environment.AppBaseDirectory, "Resources", "ThemePreviews", choice.Theme + ".png");
        if (!File.Exists(previewPath))
        {
            ThemePreviewImage.Source = null;
            ThemePreviewImage.Visibility = Visibility.Collapsed;
            ThemePreviewPlaceholder.Visibility = Visibility.Visible;
            return;
        }

        ThemePreviewImage.Source = new BitmapImage(new Uri(previewPath));
        ThemePreviewImage.Visibility = Visibility.Visible;
        ThemePreviewPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void UpdateLaunchButtonPalette(AppTheme theme)
    {
        var (top, middle, bottom) = theme switch
        {
            AppTheme.Legacy => ("#34D399", "#16A34A", "#166534"),
            AppTheme.StandardLight => ("#93C5FD", "#3B82F6", "#1D4ED8"),
            AppTheme.StandardDark => ("#CBD5E1", "#64748B", "#334155"),
            AppTheme.ModernDark => ("#A78BFA", "#7C3AED", "#4C1D95"),
            AppTheme.ModernLight => ("#40E0D0", "#12B8A6", "#087F7A"),
            AppTheme.HighContrast => ("#FEF08A", "#EAB308", "#854D0E"),
            AppTheme.CrabCute => ("#FB7185", "#E11D48", "#9F1239"),
            _ => ("#78B8FF", "#2F72EA", "#1D55D3")
        };

        ContinueButton.Background = new LinearGradientBrush(
            new GradientStopCollection
            {
                new(ParseColor(top), 0),
                new(ParseColor(middle), 0.50),
                new(ParseColor(bottom), 1)
            },
            new Point(0, 0),
            new Point(0, 1));
    }

    private void StopSplashVideo()
    {
        try
        {
            SplashVideo.Stop();
            SplashVideo.Source = null;
        }
        catch
        {
        }
    }

    private static Color ParseColor(string hex) =>
        (Color)ColorConverter.ConvertFromString(hex);

    private static Task FadeAsync(UIElement element, double from, double to, TimeSpan duration)
    {
        var tcs = new TaskCompletionSource();
        var animation = new DoubleAnimation(from, to, duration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.HoldEnd
        };
        animation.Completed += (_, _) => tcs.SetResult();
        element.BeginAnimation(OpacityProperty, animation);
        return tcs.Task;
    }

    private void WindowChrome_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void LanguageCs_Click(object sender, RoutedEventArgs e) => ApplyWelcomeLanguage(L10n.Language.CS);

    private void LanguageEn_Click(object sender, RoutedEventArgs e) => ApplyWelcomeLanguage(L10n.Language.EN);

    private void HelpButton_Click(object sender, RoutedEventArgs e) => OpenUserManual();

    private void OpenUserManual()
    {
        var docs = L10n.IsCzech
            ? new[] { "USER_MANUAL.cs.html", "USER_MANUAL.cs.md", "USER_MANUAL.html", "USER_MANUAL.md" }
            : new[] { "USER_MANUAL.html", "USER_MANUAL.md", "USER_MANUAL.cs.html", "USER_MANUAL.cs.md" };

        foreach (var file in docs)
        {
            var candidate = Path.Combine(_environment.AppBaseDirectory, "Docs", file);
            if (!File.Exists(candidate))
                continue;

            try
            {
                if (candidate.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    candidate = EnsureLocalHtmlFromMarkdown(candidate, L10n.IsCzech);

                Process.Start(new ProcessStartInfo { FileName = candidate, UseShellExecute = true });
                return;
            }
            catch
            {
                // Try the next candidate.
            }
        }

        MessageBox.Show(
            L10n.IsCzech ? "Lokální uživatelský manuál nebyl nalezen." : "Local user manual was not found.",
            "OpenClaw Manager Tool",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static string EnsureLocalHtmlFromMarkdown(string markdownPath, bool isCzech)
    {
        var htmlPath = Path.ChangeExtension(markdownPath, ".local.html");
        if (File.Exists(htmlPath) && File.GetLastWriteTimeUtc(htmlPath) >= File.GetLastWriteTimeUtc(markdownPath))
            return htmlPath;

        var markdown = File.ReadAllText(markdownPath);
        var title = isCzech ? "Uživatelský manuál" : "User Manual";
        var htmlBody = MarkdownToHtml(markdown);

        var fullHtml = $@"<!doctype html>
<html lang=""{(isCzech ? "cs" : "en")}"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>{title}</title>
  <style>
    body {{ margin: 0; background: #101214; color: #e6e8ea; font-family: ""Segoe UI"", system-ui, sans-serif; }}
    main {{ max-width: 1100px; margin: 0 auto; padding: 24px; line-height: 1.55; }}
    h1, h2, h3, h4 {{ line-height: 1.25; margin-top: 1.4em; }}
    h1 {{ margin-top: 0; }}
    a {{ color: #7cc4ff; }}
    pre {{ background: #171a1f; border: 1px solid #2b3139; border-radius: 10px; padding: 12px; overflow: auto; }}
    code {{ background: #1b2027; border-radius: 6px; padding: 1px 5px; }}
    pre code {{ background: transparent; padding: 0; }}
    table {{ border-collapse: collapse; width: 100%; margin: 12px 0; }}
    th, td {{ border: 1px solid #2b3139; padding: 6px 8px; text-align: left; }}
    blockquote {{ border-left: 3px solid #3a4350; margin: 12px 0; padding: 4px 12px; color: #c6ccd3; }}
  </style>
</head>
<body>
  <main>
{htmlBody}
  </main>
</body>
</html>";

        File.WriteAllText(htmlPath, fullHtml);
        return htmlPath;
    }

    private static string MarkdownToHtml(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var html = new System.Text.StringBuilder();
        var paragraph = new System.Text.StringBuilder();
        var inCode = false;
        var inUl = false;
        var inOl = false;

        void FlushParagraph()
        {
            if (paragraph.Length == 0) return;
            html.Append("<p>").Append(ParseInline(paragraph.ToString().Trim())).AppendLine("</p>");
            paragraph.Clear();
        }

        void CloseLists()
        {
            if (inUl) { html.AppendLine("</ul>"); inUl = false; }
            if (inOl) { html.AppendLine("</ol>"); inOl = false; }
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine ?? string.Empty;

            if (line.TrimStart().StartsWith("```"))
            {
                FlushParagraph();
                CloseLists();
                html.AppendLine(inCode ? "</code></pre>" : "<pre><code>");
                inCode = !inCode;
                continue;
            }

            if (inCode)
            {
                html.AppendLine(System.Net.WebUtility.HtmlEncode(line));
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
                CloseLists();
                continue;
            }

            var headingMatch = System.Text.RegularExpressions.Regex.Match(line, @"^(#{1,6})\s+(.+)$");
            if (headingMatch.Success)
            {
                FlushParagraph();
                CloseLists();
                var level = headingMatch.Groups[1].Value.Length;
                html.Append('<').Append('h').Append(level).Append('>')
                    .Append(ParseInline(headingMatch.Groups[2].Value.Trim()))
                    .Append("</h").Append(level).AppendLine(">");
                continue;
            }

            var ulMatch = System.Text.RegularExpressions.Regex.Match(line, @"^\s*[-*]\s+(.+)$");
            if (ulMatch.Success)
            {
                FlushParagraph();
                if (inOl) { html.AppendLine("</ol>"); inOl = false; }
                if (!inUl) { html.AppendLine("<ul>"); inUl = true; }
                html.Append("<li>").Append(ParseInline(ulMatch.Groups[1].Value.Trim())).AppendLine("</li>");
                continue;
            }

            var olMatch = System.Text.RegularExpressions.Regex.Match(line, @"^\s*\d+\.\s+(.+)$");
            if (olMatch.Success)
            {
                FlushParagraph();
                if (inUl) { html.AppendLine("</ul>"); inUl = false; }
                if (!inOl) { html.AppendLine("<ol>"); inOl = true; }
                html.Append("<li>").Append(ParseInline(olMatch.Groups[1].Value.Trim())).AppendLine("</li>");
                continue;
            }

            if (paragraph.Length > 0) paragraph.Append(' ');
            paragraph.Append(line.Trim());
        }

        FlushParagraph();
        CloseLists();
        return html.ToString();
    }

    private static string ParseInline(string input)
    {
        var encoded = System.Net.WebUtility.HtmlEncode(input);
        encoded = System.Text.RegularExpressions.Regex.Replace(encoded, @"\[(.+?)\]\((.+?)\)", m =>
        {
            var text = m.Groups[1].Value;
            var href = m.Groups[2].Value.Trim();
            return $"<a href=\"{System.Net.WebUtility.HtmlEncode(href)}\">{text}</a>";
        });
        encoded = System.Text.RegularExpressions.Regex.Replace(encoded, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        encoded = System.Text.RegularExpressions.Regex.Replace(encoded, @"\*(.+?)\*", "<em>$1</em>");
        encoded = System.Text.RegularExpressions.Regex.Replace(encoded, @"`(.+?)`", "<code>$1</code>");
        return encoded;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopSplashVideo();
        if (!_completed)
            Application.Current.Shutdown();

        base.OnClosed(e);
    }

    private sealed record ThemeChoice(AppTheme Theme, string DisplayName, string Caption)
    {
        public override string ToString() => DisplayName;
    }
}
