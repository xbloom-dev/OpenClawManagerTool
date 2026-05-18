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
        DarkThemeRuntimeStyles.ApplyIfDark(this);

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
            UseButtonScanlineEffect = current.UseButtonScanlineEffect,
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
        RbThemeDark.Checked += (_, _) => UpdateSplashVideoEnabled();
        RbThemeHighContrast.Checked += (_, _) => UpdateSplashVideoEnabled();
        RbThemeCrabCute.Checked += (_, _) => UpdateSplashVideoEnabled();

        TxtSettingsPath.Text = SettingsService.SettingsFilePath;
    }

    private bool Cs => L10n.IsCzech;
    private string T(string cs, string en) => Cs ? cs : en;

    private void ApplyLocalization()
    {
        Title = T("OpenClaw Manager - Nastavení", "OpenClaw Manager - Settings");
        BtnSave.Content = T("Uložit", "Save");
        BtnReset.Content = T("Reset na výchozí", "Reset to defaults");
        BtnCancel.Content = T("Zrušit", "Cancel");
        BtnSave.ToolTip = T("Uloží nastavení a zavře dialog.", "Saves settings and closes the dialog.");
        BtnReset.ToolTip = T("Obnoví všechny hodnoty na výchozí.", "Restores all values to defaults.");
        BtnCancel.ToolTip = T("Zavře bez uložení.", "Closes without saving.");

        LblLanguage.Text = T("Jazyk / Language", "Language");
        RbLangCS.Content = "Čeština";
        RbLangEN.Content = "English";
        TxtLanguageHint.Text = T("Změna jazyka se projeví po uložení nastavení.", "Language changes after saving settings.");

        LblAppearance.Text = T("Vzhled", "Appearance");
        LblTheme.Text = T("Téma aplikace:", "Application theme:");
        RbThemeLegacy.Content = "Legacy";
        RbThemeModern.Content = "Modern";
        RbThemeDark.Content = "Dark";
        RbThemeHighContrast.Content = T("Vysok\u00FD kontrast", "High Contrast");
        RbThemeCrabCute.Content = "CrabCute";
        ChkUseSplashVideo.Content = T("SplashScreen animace p\u0159i startu", "SplashScreen startup animation");
        ChkUseButtonScanlineEffect.Content = T("Efekt \u0159\u00E1dkov\u00E1n\u00ED tla\u010D\u00EDtek", "Button scanline effect");
        TxtThemeHint.Text = T("Zm\u011Bna t\u00E9matu se projev\u00ED po ulo\u017Een\u00ED. Video je aktivn\u00ED ve v\u0161ech modern\u00EDch t\u00E9matech.", "Theme changes after saving. Video is active in all modern-style themes.");

        LblPaths.Text = T("Cesty", "Paths");
        LblOpenClawPath.Text = T("OpenClaw složka:", "OpenClaw folder:");
        LblTempPath.Text = T("Temp složka (Gateway logy):", "Temp folder (Gateway logs):");
        LblOpenClawCommand.Text = T("Cesta k openclaw příkazu:", "OpenClaw command path:");
        TxtOpenClawCommandHint.Text = T("Nech 'openclaw' pro PATH lookup, nebo zadej plnou cestu bez uvozovek a shell znaku.", "Keep 'openclaw' for PATH lookup, or enter a full path without quotes or shell characters.");
        LblPowerShellWorkingDir.Text = T("PowerShell pracovni adresar:", "PowerShell working directory:");
        TxtPowerShellHint.Text = T("Adresar, ve kterem se otevira PowerShell pres menu Otevrit.", "Directory used when opening PowerShell from the Open menu.");
        LblTokenManagerSecrets.Text = "Token Manager secrets.json:";
        TxtTokenManagerSecretsHint.Text = T("Lokální DPAPI chráněný vault s API klíči; nesdílet a neukládat do cloudu.", "Local DPAPI-protected API key vault; do not share or place in cloud sync.");
        LblSettingsPath.Text = T("Soubor s nastavením:", "Settings file:");

        foreach (var button in new[] { BtnBrowseOpenClaw, BtnBrowseTemp, BtnBrowsePowerShell, BtnBrowseTokenManagerSecrets })
        {
            button.Content = T("Procházet...", "Browse...");
            button.ToolTip = T("Vybere cestu ze systému.", "Selects a path from the filesystem.");
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
        switch (_settings.Theme)
        {
            case AppTheme.Modern:
                RbThemeModern.IsChecked = true;
                break;
            case AppTheme.Dark:
                RbThemeDark.IsChecked = true;
                break;
            case AppTheme.HighContrast:
                RbThemeHighContrast.IsChecked = true;
                break;
            case AppTheme.CrabCute:
                RbThemeCrabCute.IsChecked = true;
                break;
            case AppTheme.Legacy:
            default:
                RbThemeLegacy.IsChecked = true;
                break;
        }

        ChkUseSplashVideo.IsChecked = _settings.UseSplashVideo;
        ChkUseButtonScanlineEffect.IsChecked = _settings.UseButtonScanlineEffect;
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
        _settings.Theme = GetSelectedTheme();
        _settings.UseSplashVideo = ChkUseSplashVideo.IsChecked == true;
        _settings.UseButtonScanlineEffect = ChkUseButtonScanlineEffect.IsChecked == true;
    }

    private AppTheme GetSelectedTheme()
    {
        if (RbThemeModern.IsChecked == true) return AppTheme.Modern;
        if (RbThemeDark.IsChecked == true) return AppTheme.Dark;
        if (RbThemeHighContrast.IsChecked == true) return AppTheme.HighContrast;
        if (RbThemeCrabCute.IsChecked == true) return AppTheme.CrabCute;
        return AppTheme.Legacy;
    }

    /// <summary>
    /// v0.5: Checkbox pro splash video je aktivní pouze v Modern tématu.
    /// </summary>
    private void UpdateSplashVideoEnabled()
    {
        if (ChkUseSplashVideo == null) return;
        var isModernTheme = GetSelectedTheme() != AppTheme.Legacy;
        ChkUseSplashVideo.IsEnabled = isModernTheme;
        if (ChkUseButtonScanlineEffect != null)
            ChkUseButtonScanlineEffect.IsEnabled = isModernTheme;
    }

    private void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        ReadFromUi();

        if (!GatewayService.TryValidateOpenClawCommand(_settings.OpenClawCommand, out var commandError))
        {
            MessageBox.Show(
                T("OpenClaw příkaz není bezpečný nebo platný:\n", "OpenClaw command is not safe or valid:\n") + commandError,
                T("Neplatny prikaz", "Invalid command"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var ok = SettingsService.Save(_settings);
        if (!ok)
        {
            MessageBox.Show(
                T("Uložení nastavení selhalo.\nZkontroluj, zda máš oprávnění zapisovat do ", "Saving settings failed.\nCheck write permissions for ") + SettingsService.SettingsFilePath,
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
            T("Opravdu resetovat všechna nastavení na výchozí hodnoty?\nZměny budou aktivní až po kliknutí na Uložit.", "Reset all settings to defaults?\nChanges become active after clicking Save."),
            T("Reset na výchozí", "Reset to defaults"),
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
            Title = T("Vyber složku", "Select folder"),
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
