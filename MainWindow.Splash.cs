// MainWindow.Splash.cs
// Partial class — splash screen logika (v0.5)
// Umístění: OpenClawManager/ (vedle MainWindow.xaml.cs)

using System.IO;
using System.Windows;
using System.Windows.Threading;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager;

public partial class MainWindow
{
    // ── Splash fields ─────────────────────────────────────────────────────────
    private DispatcherTimer? _splashProgressTimer;
    private bool _splashVideoActive = false;

    // ── Inicializace (volá se na konci konstruktoru MainWindow) ───────────────
    private void InitSplash()
    {
        var settings = SettingsService.Current;

        // Legacy theme — splash overlay se nezobrazuje vůbec
        if (settings.Theme == AppTheme.Legacy)
        {
            SplashOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        // Modern theme — overlay vždy viditelný při startu
        SplashOverlay.Visibility = Visibility.Visible;

        // Hledáme splash.mp4 ve dvou možných umístěních:
        //   1) bin\...\Resources\splash.mp4  (CopyToOutputDirectory zachovává strukturu)
        //   2) bin\...\splash.mp4            (kdyby ho někdo dal přímo vedle EXE)
        var pathInResources = Path.Combine(AppContext.BaseDirectory, "Resources", "splash.mp4");
        var pathInRoot      = Path.Combine(AppContext.BaseDirectory, "splash.mp4");

        string? splashMp4 = null;
        if (File.Exists(pathInResources)) splashMp4 = pathInResources;
        else if (File.Exists(pathInRoot)) splashMp4 = pathInRoot;

        Log($"[Splash] Theme=Modern, UseSplashVideo={settings.UseSplashVideo}, " +
            $"mp4={(splashMp4 ?? "(not found)")}");

        if (settings.UseSplashVideo && splashMp4 != null)
        {
            StartSplashVideo(splashMp4);
        }
        else
        {
            // PNG fallback — SplashImage je viditelný defaultně
            SplashMedia.Visibility = Visibility.Collapsed;
            SplashProgress.Visibility = Visibility.Collapsed;
            Log("[Splash] Zobrazuje se statický splash.png (fallback).");
        }
    }

    // ── Video režim ───────────────────────────────────────────────────────────
    private void StartSplashVideo(string mp4Path)
    {
        _splashVideoActive = true;

        // Diagnostické handlery — pomohou pokud video selže
        SplashMedia.MediaOpened += SplashMedia_MediaOpened;
        SplashMedia.MediaFailed += SplashMedia_MediaFailed;

        SplashMedia.Visibility = Visibility.Visible;
        SplashProgress.Visibility = Visibility.Visible;

        SplashMedia.Source = new Uri(mp4Path);
        SplashMedia.Play();
    }

    // ── Video úspěšně načteno ─────────────────────────────────────────────────
    private void SplashMedia_MediaOpened(object? sender, RoutedEventArgs e)
    {
        // Progress timer startujeme až po MediaOpened — máme jistotu o NaturalDuration
        _splashProgressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _splashProgressTimer.Tick += SplashProgressTimer_Tick;
        _splashProgressTimer.Start();

        var duration = SplashMedia.NaturalDuration.HasTimeSpan
            ? SplashMedia.NaturalDuration.TimeSpan.ToString(@"mm\:ss\.fff")
            : "(unknown)";
        Log($"[Splash] Video načteno, délka {duration}.");
    }

    // ── Video selhalo (chybí codec, špatný formát atd.) ──────────────────────
    private void SplashMedia_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        Log($"[Splash] CHYBA přehrávání videa: {e.ErrorException?.Message ?? "(unknown)"}");
        Log("[Splash] Přepínám na PNG fallback.");

        _splashProgressTimer?.Stop();
        _splashProgressTimer = null;

        SplashMedia.Stop();
        SplashMedia.Source = null;
        SplashMedia.Visibility = Visibility.Collapsed;
        SplashProgress.Visibility = Visibility.Collapsed;
        _splashVideoActive = false;
        // SplashImage zůstane viditelný — to je náš PNG fallback
    }

    // ── Progress timer tick ───────────────────────────────────────────────────
    private void SplashProgressTimer_Tick(object? sender, EventArgs e)
    {
        if (!SplashMedia.NaturalDuration.HasTimeSpan) return;
        if (SplashMedia.Position == TimeSpan.Zero) return;

        var total = SplashMedia.NaturalDuration.TimeSpan.TotalSeconds;
        if (total <= 0) return;

        SplashProgress.Value = Math.Clamp(
            SplashMedia.Position.TotalSeconds / total, 0.0, 1.0);
    }

    // ── Video doběhlo — XAML event handler ────────────────────────────────────
    private void SplashMedia_MediaEnded(object sender, RoutedEventArgs e)
    {
        _splashProgressTimer?.Stop();
        SplashProgress.Value = 1.0;

        // Freeze na prvním snímku
        SplashMedia.Stop();
        SplashMedia.Position = TimeSpan.FromMilliseconds(1);
        SplashMedia.Pause();
    }

    // ── Dispose splash (první věc v BtnStartTui_Click) ────────────────────────
    private void DisposeSplash()
    {
        if (SplashOverlay.Visibility != Visibility.Visible) return;

        _splashProgressTimer?.Stop();
        _splashProgressTimer = null;

        if (_splashVideoActive)
        {
            SplashMedia.Stop();
            SplashMedia.Source = null;
            SplashMedia.Close();
            _splashVideoActive = false;
        }

        SplashOverlay.Visibility = Visibility.Collapsed;
    }
}
