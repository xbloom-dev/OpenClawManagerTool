// MainWindow.Splash.cs — splash screen logika (v0.5)

using System.IO;
using System.Windows;
using System.Windows.Threading;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager;

public partial class MainWindow
{
    private DispatcherTimer? _splashProgressTimer;
    private bool _splashVideoStarted;

    private void InitSplash()
    {
        var settings = SettingsService.Current;

        if (settings.Theme == AppTheme.Legacy)
        {
            SplashOverlay.Visibility = Visibility.Collapsed;
            // SplashBorder (ASCII art) v TerminalControl zůstane viditelný
            // dokud uživatel neklikne na OpenClaw TUI — DisposeSplash() ho skryje.
            return;
        }

        // Modern theme — SplashOverlay (video/PNG) překrývá terminál.
        // Schovat ASCII art SplashBorder v TerminalControl — ten patří Legacy.
        SplashOverlay.Visibility = Visibility.Visible;
        Terminal.HideSplashBorder();

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
            Log("[Splash] Zobrazuje se statický splash.png (fallback), pokud je dostupný.");
        }
    }

    private void StartSplashVideo(string mp4Path)
    {
        SplashMedia.Visibility    = Visibility.Visible;
        SplashProgress.Visibility = Visibility.Visible;
        SplashMedia.Volume        = 1.0;
        SplashMedia.IsMuted       = false;
        SplashMedia.Source        = new Uri(mp4Path);
        _splashVideoStarted       = true;
        SplashMedia.Play();
        Log("[Splash] SplashMedia.Play() spuštěno.");
    }

    private void SplashMedia_MediaOpened(object sender, RoutedEventArgs e)
    {
        _splashProgressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _splashProgressTimer.Tick += SplashProgressTimer_Tick;
        _splashProgressTimer.Start();

        var duration = SplashMedia.NaturalDuration.HasTimeSpan
            ? SplashMedia.NaturalDuration.TimeSpan.ToString(@"mm\:ss\.fff")
            : "(unknown)";
        Log($"[Splash] Video načteno, délka {duration}.");
    }

    private void SplashMedia_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        Log($"[Splash] CHYBA videa: {e.ErrorException?.Message ?? "(unknown)"}");
        StopSplashVideo();
        SplashMedia.Visibility    = Visibility.Collapsed;
        SplashProgress.Visibility = Visibility.Collapsed;
        // SplashImage (PNG) zůstane jako fallback
    }

    private void SplashImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        Log($"[Splash] CHYBA splash.png: {e.ErrorException?.Message ?? "(unknown)"}");
        ShowAsciiSplashFallback();
    }

    private void SplashMedia_MediaEnded(object sender, RoutedEventArgs e)
    {
        _splashProgressTimer?.Stop();
        SplashProgress.Value = 1.0;

        // Zastavit video a schovat ho.
        // SplashImage (PNG) pod ním zůstane viditelný jako freeze frame.
        // Overlay se neskryje automaticky — čeká na kliknutí uživatele
        // na tlačítko OpenClaw TUI (DisposeSplash() v BtnStartTui_Click).
        StopSplashVideo();
        SplashMedia.Visibility    = Visibility.Collapsed;
        SplashProgress.Visibility = Visibility.Collapsed;
    }

    private void SplashProgressTimer_Tick(object? sender, EventArgs e)
    {
        if (!SplashMedia.NaturalDuration.HasTimeSpan) return;
        var total = SplashMedia.NaturalDuration.TimeSpan.TotalSeconds;
        if (total <= 0) return;
        SplashProgress.Value = Math.Clamp(SplashMedia.Position.TotalSeconds / total, 0, 1);
    }

    private void StopSplashVideo()
    {
        if (!_splashVideoStarted && _splashProgressTimer == null && SplashMedia.Source == null)
            return;

        _splashProgressTimer?.Stop();
        _splashProgressTimer = null;
        try
        {
            SplashMedia.Volume  = 0;
            SplashMedia.IsMuted = true;
            SplashMedia.Stop();
            SplashMedia.Source  = null;
        }
        catch { }
        _splashVideoStarted = false;
        Log("[Splash] StopSplashVideo() dokončeno.");
    }

    private void DisposeSplash()
    {
        // Modern theme: zastavit video, skrýt overlay
        StopSplashVideo();
        SplashOverlay.Visibility = Visibility.Collapsed;

        // Obě témata: schovat ASCII art SplashBorder a zobrazit WebView
        Terminal.HideSplashBorder();
        Terminal.ShowWebView();
    }

    private void ShowAsciiSplashFallback()
    {
        StopSplashVideo();
        SplashMedia.Visibility    = Visibility.Collapsed;
        SplashProgress.Visibility = Visibility.Collapsed;
        SplashOverlay.Visibility  = Visibility.Collapsed;

        if (Terminal.IsTuiRunning)
        {
            Terminal.HideSplashBorder();
            Terminal.ShowWebView();
        }
        else
        {
            Terminal.ShowSplashBorder();
        }

        Log("[Splash] Chybí video i PNG fallback, zobrazuji ASCII ART.");
    }
}
