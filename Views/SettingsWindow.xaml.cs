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

    public SettingsWindow()
    {
        InitializeComponent();

        // Klon aktuálních nastavení (uživatel pak může Cancel bez změny originálu)
        var current = SettingsService.Current;
        _settings = new AppSettings
        {
            OpenClawPath            = current.OpenClawPath,
            TempPath                = current.TempPath,
            OpenClawCommand         = current.OpenClawCommand,
            PowerShellWorkingDir    = current.PowerShellWorkingDir,
            Language                = current.Language,
            CleanupAgents           = current.CleanupAgents,
            TokenManagerSecretsPath = current.TokenManagerSecretsPath,
            // v0.5
            Theme                   = current.Theme,
            UseSplashVideo          = current.UseSplashVideo,
        };

        LoadToUi();

        BtnSave.Click   += BtnSave_Click;
        BtnReset.Click  += BtnReset_Click;
        BtnCancel.Click += (_, _) => { DialogResult = false; Close(); };

        BtnBrowseOpenClaw.Click     += (_, _) => BrowseFolder(TxtOpenClawPath);
        BtnBrowseTemp.Click         += (_, _) => BrowseFolder(TxtTempPath);
        BtnBrowsePowerShell.Click   += (_, _) => BrowseFolder(TxtPowerShellWorkingDir);

        // v0.5: Theme přepínač řídí dostupnost video checkboxu
        RbThemeLegacy.Checked += (_, _) => UpdateSplashVideoEnabled();
        RbThemeModern.Checked += (_, _) => UpdateSplashVideoEnabled();

        TxtSettingsPath.Text = SettingsService.SettingsFilePath;
    }

    private void LoadToUi()
    {
        TxtOpenClawPath.Text         = _settings.OpenClawPath;
        TxtTempPath.Text             = _settings.TempPath;
        TxtOpenClawCommand.Text      = _settings.OpenClawCommand;
        TxtPowerShellWorkingDir.Text = _settings.PowerShellWorkingDir;

        // Jazyk — přednačíst RadioButton
        if (_settings.Language == "EN")
            RbLangEN.IsChecked = true;
        else
            RbLangCS.IsChecked = true;

        // v0.5: Téma + splash video
        if (_settings.Theme == AppTheme.Modern)
            RbThemeModern.IsChecked = true;
        else
            RbThemeLegacy.IsChecked = true;

        ChkUseSplashVideo.IsChecked = _settings.UseSplashVideo;
        UpdateSplashVideoEnabled();
    }

    private void ReadFromUi()
    {
        _settings.OpenClawPath         = TxtOpenClawPath.Text.Trim();
        _settings.TempPath             = TxtTempPath.Text.Trim();
        _settings.OpenClawCommand      = TxtOpenClawCommand.Text.Trim();
        _settings.PowerShellWorkingDir = TxtPowerShellWorkingDir.Text.Trim();

        // Jazyk
        _settings.Language = RbLangEN.IsChecked == true ? "EN" : "CS";

        // v0.5: Téma + splash video
        _settings.Theme = RbThemeModern.IsChecked == true ? AppTheme.Modern : AppTheme.Legacy;
        _settings.UseSplashVideo = ChkUseSplashVideo.IsChecked == true;
    }

    /// <summary>
    /// v0.5: Checkbox pro splash video je aktivní pouze v Modern tématu.
    /// </summary>
    private void UpdateSplashVideoEnabled()
    {
        if (ChkUseSplashVideo == null) return;
        ChkUseSplashVideo.IsEnabled = RbThemeModern.IsChecked == true;
    }

    private void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        ReadFromUi();

        var ok = SettingsService.Save(_settings);
        if (!ok)
        {
            MessageBox.Show(
                "Uložení nastavení selhalo.\nZkontroluj zda máš oprávnění zapisovat do " + SettingsService.SettingsFilePath,
                "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

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

        _settings = new AppSettings();
        LoadToUi();
    }

    private void BrowseFolder(System.Windows.Controls.TextBox target)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Vyber složku",
            InitialDirectory = string.IsNullOrEmpty(target.Text) ? "" : target.Text
        };

        if (dialog.ShowDialog() == true)
            target.Text = dialog.FolderName;
    }
}
