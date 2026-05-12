// MainWindow.Splash.cs
// Partial class — splash screen logika (v0.5)

using System.IO;
using System.Windows;
using System.Windows.Threading;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager;

public partial class MainWindow
{
    private DispatcherTimer? _splashProgressTimer;
    private bool _splashVideoActive = false;

    private void InitSplash()
    {
        var settings = SettingsService.Current;

        if (settings.Theme == AppTheme.Legacy)
        {
            SplashOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        SplashOverlay.Visibility = Visibility.Visible;

        var pathInResources = Path.Combine(AppContext.BaseDirectory, "Resources", "splash.mp4");
        var pathInRoot      = Path.Combine(AppContext.BaseDirectory, "splash.mp4");

        string? splashMp4 = null;
        if (File.Exists(pathInResources)) splashMp4 = pathInResources;
        else if (File.Exists(pathInRoot)) splashMp4 = pathInRoot;

        Log($"[Splash] Theme=Modern, UseSplashVideo={settings.UseSplashVideo}, " +
            $"mp4={(splashMp4 ?? "(not found)")}");

        if (settings.UseSplashVideo && splashMp4 != null)
            StartSplashVideo(splashMp4);
        else
        {
            SplashMedia.Visibility    = Visibility.Collapsed;
            SplashProgress.Visibility = Visibility.Collapsed;
            Log("[Splash] Zobrazuje se statický splash.png (fallback).");
        }
    }

    private void StartSplashVideo(string mp4Path)
    {
        _splashVideoActive = true;
        SplashMedia.MediaOpened += SplashMedia_MediaOpened;
        SplashMedia.MediaFailed += SplashMedia_MediaFailed;
        SplashMedia.Visibility    = Visibility.Visible;
        SplashProgress.Visibility = Visibility.Visible;
        SplashMedia.Source = new Uri(mp4Path);
        SplashMedia.Play();
    }

    private void SplashMedia_MediaOpened(object? sender, RoutedEventArgs e)
    {
        _splashProgressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _splashProgressTimer.Tick += SplashProgressTimer_Tick;
        _splashProgressTimer.Start();

        var duration = SplashMedia.NaturalDuration.HasTimeSpan
            ? SplashMedia.NaturalDuration.TimeSpan.ToString(@"mm\:ss\.fff")
            : "(unknown)";
        Log($"[Splash] Video načteno, délka {duration}.");
    }

    private void SplashMedia_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        Log($"[Splash] CHYBA přehrávání videa: {e.ErrorException?.Message ?? "(unknown)"}");
        Log("[Splash] Přepínám na PNG fallback.");

        // Zastavit přehrávání bez ohledu na cokoli dalšího
        StopSplashVideo();

        SplashMedia.Visibility    = Visibility.Collapsed;
        SplashProgress.Visibility = Visibility.Collapsed;
        // SplashImage zůstane viditelný jako PNG fallback
    }

    private void SplashProgressTimer_Tick(object? sender, EventArgs e)
    {
        if (!SplashMedia.NaturalDuration.HasTimeSpan) return;
        if (SplashMedia.Position == TimeSpan.Zero) return;

        var total = SplashMedia.NaturalDuration.TimeSpan.TotalSeconds;
        if (total <= 0) return;

        SplashProgress.Value = Math.Clamp(
            SplashMedia.Position.TotalSeconds / total, 0.0, 1.0);
    }

    private void SplashMedia_MediaEnded(object sender, RoutedEventArgs e)
    {
        _splashProgressTimer?.Stop();
        SplashProgress.Value = 1.0;

        // Video doběhlo — zastavit okamžitě (zastaví i zvuk),
        // pak po 500ms skrýt overlay.
        StopSplashVideo();

        var endTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        endTimer.Tick += (_, _) =>
        {
            endTimer.Stop();
            SplashOverlay.Visibility = Visibility.Collapsed;
        };
        endTimer.Start();
    }

    /// <summary>
    /// Zastaví přehrávání videa a uvolní MediaElement.
    /// Bezpečné volat vícekrát — idempotentní.
    /// Odděleno od skrývání overlay aby zvuk vždy ustal okamžitě.
    /// </summary>
    private void StopSplashVideo()
    {
        if (!_splashVideoActive) return;

        _splashProgressTimer?.Stop();
        _splashProgressTimer = null;

        try
        {
            SplashMedia.Stop();
            SplashMedia.Source = null;
            SplashMedia.Close();
        }
        catch { }

        _splashVideoActive = false;
    }

    /// <summary>
    /// Dispose splash — voláno z BtnStartTui_Click nebo při přepnutí tématu.
    /// Vždy zastaví video (i zvuk), pak skryje overlay.
    /// </summary>
    private void DisposeSplash()
    {
        // Zastavit video vždy — nezávisle na Visibility.
        // Původní guard "if Visibility != Visible return" způsoboval
        // že video hrálo dál na pozadí pokud overlay byl již skrytý.
        StopSplashVideo();

        SplashOverlay.Visibility = Visibility.Collapsed;
    }
}
