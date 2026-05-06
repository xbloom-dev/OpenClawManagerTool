using System.Windows;
using Microsoft.Win32;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

/// <summary>
/// Dialog pro úpravu nastavení aplikace.
/// Otevírá se přes ShowDialog() z MainWindow (modální).
/// Po Save vrací DialogResult = true, jinak false.
/// </summary>
public partial class SettingsWindow : Window
{
    private AppSettings _settings;

    public SettingsWindow(AppSettings currentSettings)
    {
        InitializeComponent();

        // Klon aktuálních nastavení (uživatel pak může Cancel bez změny originálu)
        _settings = new AppSettings
        {
            OpenClawPath = currentSettings.OpenClawPath,
            TempPath = currentSettings.TempPath,
            OpenClawCommand = currentSettings.OpenClawCommand,
            PowerShellWorkingDir = currentSettings.PowerShellWorkingDir
        };

        LoadToUi();

        // Napojení tlačítek
        BtnSave.Click += BtnSave_Click;
        BtnReset.Click += BtnReset_Click;
        BtnCancel.Click += (_, _) => { DialogResult = false; Close(); };

        BtnBrowseOpenClaw.Click += (_, _) => BrowseFolder(TxtOpenClawPath);
        BtnBrowseTemp.Click += (_, _) => BrowseFolder(TxtTempPath);
        BtnBrowsePowerShell.Click += (_, _) => BrowseFolder(TxtPowerShellWorkingDir);

        // Cesta k settings souboru pro info
        TxtSettingsPath.Text = SettingsService.SettingsFilePath;
    }

    /// <summary>
    /// Načte hodnoty z _settings do textových polí.
    /// </summary>
    private void LoadToUi()
    {
        TxtOpenClawPath.Text = _settings.OpenClawPath;
        TxtTempPath.Text = _settings.TempPath;
        TxtOpenClawCommand.Text = _settings.OpenClawCommand;
        TxtPowerShellWorkingDir.Text = _settings.PowerShellWorkingDir;
    }

    /// <summary>
    /// Načte hodnoty z textových polí do _settings (validace).
    /// </summary>
    private void ReadFromUi()
    {
        _settings.OpenClawPath = TxtOpenClawPath.Text.Trim();
        _settings.TempPath = TxtTempPath.Text.Trim();
        _settings.OpenClawCommand = TxtOpenClawCommand.Text.Trim();
        _settings.PowerShellWorkingDir = TxtPowerShellWorkingDir.Text.Trim();
    }

    private void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        ReadFromUi();

        var ok = SettingsService.Save(_settings);
        if (!ok)
        {
            MessageBox.Show("Uložení nastavení selhalo.\nZkontroluj zda máš oprávnění zapisovat do " + SettingsService.SettingsFilePath,
                "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Vrátíme uložené nastavení do MainWindow přes property
        SavedSettings = _settings;
        DialogResult = true;
        Close();
    }

    private void BtnReset_Click(object? sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Opravdu resetovat všechna nastavení na výchozí hodnoty?\nZměny budou aktivní až po kliknutí na Uložit.",
            "Reset na výchozí",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        _settings = new AppSettings(); // čerstvá instance s defaulty
        LoadToUi();
    }

    /// <summary>
    /// Zobrazí Folder Picker dialog a vyplní zvolenou cestu do daného textového pole.
    /// </summary>
    private void BrowseFolder(System.Windows.Controls.TextBox target)
    {
        // OpenFolderDialog je dostupný od .NET 8
        var dialog = new OpenFolderDialog
        {
            Title = "Vyber složku",
            InitialDirectory = string.IsNullOrEmpty(target.Text) ? "" : target.Text
        };

        if (dialog.ShowDialog() == true)
        {
            target.Text = dialog.FolderName;
        }
    }

    /// <summary>
    /// Po Save je tady uložené nastavení (nullable — pokud uživatel zrušil, je null).
    /// MainWindow si po ShowDialog() přečte tuto property pokud DialogResult == true.
    /// </summary>
    public AppSettings? SavedSettings { get; private set; }
}
