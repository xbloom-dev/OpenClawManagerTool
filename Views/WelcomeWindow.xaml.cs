using System.IO;
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
    private readonly IReadOnlyList<ThemeChoice> _themes;
    private bool _completed;

    public WelcomeWindow(ISettingsService settingsService, IAppEnvironment environment)
    {
        _settingsService = settingsService;
        _environment = environment;
        _themes = new ThemeChoice[]
        {
            new(AppTheme.Legacy, L10n.Get("Str_Theme_Legacy"), "classic"),
            new(AppTheme.StandardLight, L10n.Get("Str_Theme_Standard"), "standard"),
            new(AppTheme.StandardDark, L10n.Get("Str_Theme_StandardDark"), "dark"),
            new(AppTheme.ModernDark, L10n.Get("Str_Theme_ModernDark"), "dark"),
            new(AppTheme.ModernLight, L10n.Get("Str_Theme_ModernLight"), "light"),
            new(AppTheme.HighContrast, L10n.Get("Str_Theme_HighContrast"), "contrast"),
            new(AppTheme.CrabCute, L10n.Get("Str_Theme_CrabCute"), "color"),
        }.Where(t => ThemeService.IsThemeAvailable(t.Theme)).ToList();

        InitializeComponent();

        ThemeCombo.ItemsSource = _themes;
        ThemeCombo.SelectedItem = _themes.FirstOrDefault(t => t.Theme == settingsService.Settings.Theme) ?? _themes[0];
        AutoUpdateCheckBox.IsChecked = settingsService.Settings.CheckUpdatesOnStartup;

        SettingsModeText.Text = settingsService.IsPortableMode ? "PORTABLE" : "APPDATA";
        DpapiWarningText.Text = L10n.IsCzech
            ? "Token vault je chraneny pres Windows DPAPI. Zaloha tokenu muze byt obnovitelna jen pod stejnym Windows uctem nebo po exportu s heslem."
            : "The token vault is protected by Windows DPAPI. Token backups may only be restorable under the same Windows account unless exported with a password.";

        TryStartSplashVideo();
    }

    private void ThemeCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is not ThemeChoice choice)
            return;

        ThemeService.Apply(choice.Theme);
        UpdateThemePreview(choice);
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
                "Ulozeni nastaveni selhalo. Zkontroluj opravneni pro zapis do: " + _settingsService.SettingsFilePath,
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
            Language = current.Language,
            AutoScrollAppLog = current.AutoScrollAppLog,
            CheckUpdatesOnStartup = checkUpdates,
            Theme = theme,
            UseSplashVideo = current.UseSplashVideo,
            UseButtonScanlineEffect = current.UseButtonScanlineEffect
        };
    }

    private void TryStartSplashVideo()
    {
        var pathInResources = Path.Combine(_environment.AppBaseDirectory, "Resources", "splash.mp4");
        var pathInRoot = Path.Combine(_environment.AppBaseDirectory, "splash.mp4");
        var videoPath = File.Exists(pathInResources)
            ? pathInResources
            : File.Exists(pathInRoot)
                ? pathInRoot
                : null;

        if (videoPath == null)
            return;

        SplashVideo.Source = new Uri(videoPath);
        SplashVideo.Visibility = Visibility.Visible;
        SplashVideo.Volume = 0;
        SplashVideo.IsMuted = true;
        SplashVideo.Play();
    }

    private void SplashVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        SplashVideo.Position = TimeSpan.Zero;
        SplashVideo.Play();
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

    protected override void OnClosed(EventArgs e)
    {
        StopSplashVideo();
        if (!_completed)
            Application.Current.Shutdown();

        base.OnClosed(e);
    }

    private sealed record ThemeChoice(AppTheme Theme, string DisplayName, string Caption);
}
