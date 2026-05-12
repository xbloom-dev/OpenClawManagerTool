using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace OpenClawManager.Services;

/// <summary>
/// Detekce admin práv a obsluha restartu aplikace s elevation.
///
/// Použití:
/// - IsAdmin() — true pokud aktuální proces běží jako Administrator
/// - PromptForElevation() — zobrazí dialog "Restartovat jako admin?"
///   a pokud uživatel souhlasí, znovuspustí aplikaci s UAC promptem.
/// </summary>
public static class AdminService
{
    /// <summary>
    /// True pokud aktuální proces má admin práva (běží jako Administrator).
    /// </summary>
    public static bool IsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Zobrazí dialog uživateli s nabídkou restartu jako admin.
    /// Pokud souhlasí, restartuje aplikaci s UAC promptem a vrátí true
    /// (caller by měl ihned ukončit aktuální proces).
    /// </summary>
    /// <param name="reason">Důvod proč jsou admin práva potřeba (zobrazí se v dialogu)</param>
    /// <returns>True pokud restart byl spuštěn (aplikace se má ihned ukončit)</returns>
    public static bool PromptForElevation(string reason)
    {
        var result = MessageBox.Show(
            $"{reason}\n\nAplikace potřebuje admin práva.\n\nRestartovat aplikaci jako administrátor?",
            "Vyžadována admin práva",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return false;

        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
                return false;

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"  // UAC prompt
            };

            Process.Start(psi);
            Application.Current.Shutdown();
            return true;
        }
        catch (Exception ex)
        {
            // UAC prompt zrušen nebo selhal
            MessageBox.Show(
                $"Restart jako admin selhal:\n{ex.Message}",
                "Chyba",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }
}
