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

        ApplyLocalization();
        LoadToUi();

        BtnSave.Click   += BtnSave_Click;
        BtnReset.Click  += BtnReset_Click;
        BtnCancel.Click += (_, _) => { DialogResult = false; Close(); };

        BtnBrowseOpenClaw.Click     += (_, _) => BrowseFolder(TxtOpenClawPath);
        BtnBrowseTemp.Click         += (_, _) => BrowseFolder(TxtTempPath);
        BtnBrowsePowerShell.Click   += (_, _) => BrowseFolder(TxtPowerShellWorkingDir);
        BtnBrowseTokenManagerSecrets.Click += (_, _) => BrowseTokenManagerSecrets();

        // v0.5: Theme přepínač řídí dostupnost video checkboxu
        RbThemeLegacy.Checked += (_, _) => UpdateSplashVideoEnabled();
        RbThemeModern.Checked += (_, _) => UpdateSplashVideoEnabled();

        TxtSettingsPath.Text = SettingsService.SettingsFilePath;
    }

    private bool Cs => L10n.IsCzech;
    private string T(string cs, string en) => Cs ? cs : en;

    private void ApplyLocalization()
    {
        Title = T("OpenClaw Manager - Nastaveni", "OpenClaw Manager - Settings");
        BtnSave.Content = T("Ulozit", "Save");
        BtnReset.Content = T("Reset na vychozi", "Reset to defaults");
        BtnCancel.Content = T("Zrusit", "Cancel");
        BtnSave.ToolTip = T("Ulozi nastaveni a zavre dialog.", "Saves settings and closes the dialog.");
        BtnReset.ToolTip = T("Obnovi vsechny hodnoty na vychozi.", "Restores all values to defaults.");
        BtnCancel.ToolTip = T("Zavre bez ulozeni.", "Closes without saving.");

        LblLanguage.Text = T("Jazyk / Language", "Language");
        RbLangCS.Content = "Cestina";
        RbLangEN.Content = "English";
        TxtLanguageHint.Text = T("Zmena jazyka se projevi po ulozeni nastaveni.", "Language changes after saving settings.");

        LblAppearance.Text = T("Vzhled", "Appearance");
        LblTheme.Text = T("Tema aplikace:", "Application theme:");
        ChkUseSplashVideo.Content = T("Prehrat splash screen video pri startu", "Play splash screen video on startup");
        TxtThemeHint.Text = T("Zmena tematu se projevi po restartu aplikace. Video je aktivni pouze v Modern tematu.", "Theme changes after app restart. Video is active only in Modern theme.");

        LblPaths.Text = T("Cesty", "Paths");
        LblOpenClawPath.Text = T("OpenClaw slozka:", "OpenClaw folder:");
        LblTempPath.Text = T("Temp slozka (Gateway logy):", "Temp folder (Gateway logs):");
        LblOpenClawCommand.Text = T("Cesta k openclaw prikazu:", "OpenClaw command path:");
        TxtOpenClawCommandHint.Text = T("Nech 'openclaw' pro PATH lookup, nebo zadej plnou cestu bez uvozovek a shell znaku.", "Keep 'openclaw' for PATH lookup, or enter a full path without quotes or shell characters.");
        LblPowerShellWorkingDir.Text = T("PowerShell pracovni adresar:", "PowerShell working directory:");
        TxtPowerShellHint.Text = T("Adresar, ve kterem se otevira PowerShell pres menu Otevrit.", "Directory used when opening PowerShell from the Open menu.");
        LblTokenManagerSecrets.Text = "Token Manager secrets.json:";
        TxtTokenManagerSecretsHint.Text = T("Lokalni DPAPI chraneny vault s API klici; nesdilet a neukladat do cloudu.", "Local DPAPI-protected API key vault; do not share or place in cloud sync.");
        LblSettingsPath.Text = T("Soubor s nastavenim:", "Settings file:");

        foreach (var button in new[] { BtnBrowseOpenClaw, BtnBrowseTemp, BtnBrowsePowerShell, BtnBrowseTokenManagerSecrets })
        {
            button.Content = T("Prochazet...", "Browse...");
            button.ToolTip = T("Vybere cestu ze systemu.", "Selects a path from the filesystem.");
        }
    }

    private void LoadToUi()
    {
        TxtOpenClawPath.Text         = _settings.OpenClawPath;
        TxtTempPath.Text             = _settings.TempPath;
        TxtOpenClawCommand.Text      = _settings.OpenClawCommand;
        TxtPowerShellWorkingDir.Text = _settings.PowerShellWorkingDir;
        TxtTokenManagerSecretsPath.Text = _settings.TokenManagerSecretsPath;

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
        _settings.TokenManagerSecretsPath = TxtTokenManagerSecretsPath.Text.Trim();

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

        if (!GatewayService.TryValidateOpenClawCommand(_settings.OpenClawCommand, out var commandError))
        {
            MessageBox.Show(
                T("OpenClaw prikaz neni bezpecny nebo platny:\n", "OpenClaw command is not safe or valid:\n") + commandError,
                T("Neplatny prikaz", "Invalid command"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var ok = SettingsService.Save(_settings);
        if (!ok)
        {
            MessageBox.Show(
                T("Ulozeni nastaveni selhalo.\nZkontroluj zda mas opravneni zapisovat do ", "Saving settings failed.\nCheck write permissions for ") + SettingsService.SettingsFilePath,
                T("Chyba", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        DialogResult = true;
        Close();

        // v0.5: okamžitě aplikovat téma (ThemeChanged event doručí do MainWindow)
        ThemeService.Apply(_settings.Theme);
    }

    private void BtnReset_Click(object? sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            T("Opravdu resetovat vsechna nastaveni na vychozi hodnoty?\nZmeny budou aktivni az po kliknuti na Ulozit.", "Reset all settings to defaults?\nChanges become active after clicking Save."),
            T("Reset na vychozi", "Reset to defaults"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        _settings = new AppSettings();
        ApplyLocalization();
        LoadToUi();
    }

    private void BrowseFolder(System.Windows.Controls.TextBox target)
    {
        var dialog = new OpenFolderDialog
        {
            Title = T("Vyber slozku", "Select folder"),
            InitialDirectory = string.IsNullOrEmpty(target.Text) ? "" : target.Text
        };

        if (dialog.ShowDialog() == true)
            target.Text = dialog.FolderName;
    }

    private void BrowseTokenManagerSecrets()
    {
        var dialog = new OpenFileDialog
        {
            Title = T("Vyber secrets.json", "Select secrets.json"),
            Filter = T("JSON soubory (*.json)|*.json|Vsechny soubory (*.*)|*.*", "JSON files (*.json)|*.json|All files (*.*)|*.*"),
            CheckFileExists = false,
            FileName = string.IsNullOrWhiteSpace(TxtTokenManagerSecretsPath.Text)
                ? "secrets.json"
                : TxtTokenManagerSecretsPath.Text
        };

        if (dialog.ShowDialog() == true)
            TxtTokenManagerSecretsPath.Text = dialog.FileName;
    }
}
