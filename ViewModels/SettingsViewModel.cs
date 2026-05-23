using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IGatewayService _gatewayService;
    private AppTheme _theme;

    public SettingsViewModel(ISettingsService settingsService, IGatewayService gatewayService)
    {
        _settingsService = settingsService;
        _gatewayService = gatewayService;

        LoadDraft(settingsService.Settings);
        SettingsFilePath = settingsService.SettingsFilePath;
        RefreshLocalization();
    }

    public Action<bool>? CloseAction { get; set; }

    [ObservableProperty]
    private string _openClawPath = "";

    [ObservableProperty]
    private string _tempPath = "";

    [ObservableProperty]
    private string _openClawCommand = "";

    [ObservableProperty]
    private string _powerShellWorkingDir = "";

    [ObservableProperty]
    private string _tokenManagerSecretsPath = "";

    [ObservableProperty]
    private string _language = "CS";

    [ObservableProperty]
    private bool _useSplashVideo;

    [ObservableProperty]
    private bool _useButtonScanlineEffect;

    [ObservableProperty]
    private string _settingsFilePath = "";

    [ObservableProperty]
    private string _windowTitle = "";

    [ObservableProperty]
    private string _saveText = "";

    [ObservableProperty]
    private string _resetText = "";

    [ObservableProperty]
    private string _cancelText = "";

    [ObservableProperty]
    private string _saveToolTip = "";

    [ObservableProperty]
    private string _resetToolTip = "";

    [ObservableProperty]
    private string _cancelToolTip = "";

    [ObservableProperty]
    private string _languageTitle = "";

    [ObservableProperty]
    private string _czechLanguageText = "";

    [ObservableProperty]
    private string _languageHint = "";

    [ObservableProperty]
    private string _appearanceTitle = "";

    [ObservableProperty]
    private string _themeLabel = "";

    [ObservableProperty]
    private string _themeLegacyText = "";

    [ObservableProperty]
    private string _themeModernText = "";

    [ObservableProperty]
    private string _themeStandardDarkText = "";

    [ObservableProperty]
    private string _themeDarkText = "";

    [ObservableProperty]
    private string _themeModernLightText = "";

    [ObservableProperty]
    private string _themeHighContrastText = "";

    [ObservableProperty]
    private string _themeCrabCuteText = "";

    [ObservableProperty]
    private string _useSplashVideoText = "";

    [ObservableProperty]
    private string _useButtonScanlineEffectText = "";

    [ObservableProperty]
    private string _themeHint = "";

    [ObservableProperty]
    private string _pathsTitle = "";

    [ObservableProperty]
    private string _openClawPathLabel = "";

    [ObservableProperty]
    private string _tempPathLabel = "";

    [ObservableProperty]
    private string _openClawCommandLabel = "";

    [ObservableProperty]
    private string _openClawCommandHint = "";

    [ObservableProperty]
    private string _powerShellWorkingDirLabel = "";

    [ObservableProperty]
    private string _powerShellHint = "";

    [ObservableProperty]
    private string _tokenManagerSecretsLabel = "";

    [ObservableProperty]
    private string _tokenManagerSecretsHint = "";

    [ObservableProperty]
    private string _settingsPathLabel = "";

    [ObservableProperty]
    private string _browseText = "";

    [ObservableProperty]
    private string _browseToolTip = "";

    private bool Cs => L10n.IsCzech;
    private string T(string cs, string en) => Cs ? cs : en;

    public bool IsLanguageCzech
    {
        get => Language == "CS";
        set
        {
            if (value)
                Language = "CS";
        }
    }

    public bool IsLanguageEnglish
    {
        get => Language == "EN";
        set
        {
            if (value)
                Language = "EN";
        }
    }

    public AppTheme Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
                NotifyThemePropertiesChanged();
        }
    }

    public bool AreModernThemeOptionsEnabled => Theme != AppTheme.Legacy;
    public bool IsThemeLegacy { get => Theme == AppTheme.Legacy; set { if (value) Theme = AppTheme.Legacy; } }
    public bool IsThemeModern { get => Theme == AppTheme.Modern; set { if (value) Theme = AppTheme.Modern; } }
    public bool IsThemeStandardDark { get => Theme == AppTheme.StandardDark; set { if (value) Theme = AppTheme.StandardDark; } }
    public bool IsThemeDark { get => Theme == AppTheme.Dark; set { if (value) Theme = AppTheme.Dark; } }
    public bool IsThemeModernLight { get => Theme == AppTheme.ModernLight; set { if (value) Theme = AppTheme.ModernLight; } }
    public bool IsThemeHighContrast { get => Theme == AppTheme.HighContrast; set { if (value) Theme = AppTheme.HighContrast; } }
    public bool IsThemeCrabCute { get => Theme == AppTheme.CrabCute; set { if (value) Theme = AppTheme.CrabCute; } }

    partial void OnLanguageChanged(string value)
    {
        Language = string.Equals(value, "EN", StringComparison.OrdinalIgnoreCase) ? "EN" : "CS";
        OnPropertyChanged(nameof(IsLanguageCzech));
        OnPropertyChanged(nameof(IsLanguageEnglish));
    }

    public void RefreshLocalization()
    {
        WindowTitle = T("Nastavení", "Settings");
        SaveText = T("Uložit", "Save");
        ResetText = T("Reset na výchozí", "Reset to defaults");
        CancelText = T("Zrušit", "Cancel");
        SaveToolTip = T("Uloží nastavení a zavře dialog.", "Saves settings and closes the dialog.");
        ResetToolTip = T("Obnoví všechny hodnoty na výchozí.", "Restores all values to defaults.");
        CancelToolTip = T("Zavře bez uložení.", "Closes without saving.");

        LanguageTitle = T("Jazyk / Language", "Language");
        CzechLanguageText = "Čeština";
        LanguageHint = T("Změna jazyka se projeví po uložení nastavení.", "Language changes after saving settings.");
        AppearanceTitle = T("Vzhled", "Appearance");
        ThemeLabel = T("Téma aplikace:", "Application theme:");
        ThemeLegacyText = L10n.Get("Str_Theme_Legacy");
        ThemeModernText = L10n.Get("Str_Theme_Standard");
        ThemeStandardDarkText = L10n.Get("Str_Theme_StandardDark");
        ThemeDarkText = L10n.Get("Str_Theme_ModernDark");
        ThemeModernLightText = L10n.Get("Str_Theme_ModernLight");
        ThemeHighContrastText = L10n.Get("Str_Theme_HighContrast");
        ThemeCrabCuteText = L10n.Get("Str_Theme_CrabCute");
        UseSplashVideoText = T("SplashScreen animace při startu", "SplashScreen startup animation");
        UseButtonScanlineEffectText = T("Efekt řádkování tlačítek", "Button scanline effect");
        ThemeHint = T("Změna tématu se projeví po uložení. Video je aktivní ve všech moderních tématech.", "Theme changes after saving. Video is active in all modern-style themes.");

        PathsTitle = T("Cesty", "Paths");
        OpenClawPathLabel = T("OpenClaw složka:", "OpenClaw folder:");
        TempPathLabel = T("Temp složka (Gateway logy):", "Temp folder (Gateway logs):");
        OpenClawCommandLabel = T("Cesta k openclaw příkazu:", "OpenClaw command path:");
        OpenClawCommandHint = T("Nech 'openclaw' pro PATH lookup, nebo zadej plnou cestu bez uvozovek a shell znaku.", "Keep 'openclaw' for PATH lookup, or enter a full path without quotes or shell characters.");
        PowerShellWorkingDirLabel = T("PowerShell pracovní adresář:", "PowerShell working directory:");
        PowerShellHint = T("Adresář, ve kterém se otevírá PowerShell přes menu Otevřít.", "Directory used when opening PowerShell from the Open menu.");
        TokenManagerSecretsLabel = "Token Manager secrets.json:";
        TokenManagerSecretsHint = T("Lokální DPAPI chráněný vault s API klíči; nesdílet a neukládat do cloudu.", "Local DPAPI-protected API key vault; do not share or place in cloud sync.");
        SettingsPathLabel = T("Soubor s nastavením:", "Settings file:");
        BrowseText = T("Procházet...", "Browse...");
        BrowseToolTip = T("Vybere cestu ze systému.", "Selects a path from the filesystem.");
    }

    [RelayCommand]
    private void BrowseOpenClawPath() => BrowseFolder(OpenClawPath, value => OpenClawPath = value);

    [RelayCommand]
    private void BrowseTempPath() => BrowseFolder(TempPath, value => TempPath = value);

    [RelayCommand]
    private void BrowsePowerShellWorkingDir() => BrowseFolder(PowerShellWorkingDir, value => PowerShellWorkingDir = value);

    [RelayCommand]
    private void BrowseTokenManagerSecrets()
    {
        var dialog = new OpenFileDialog
        {
            Title = T("Vyber secrets.json", "Select secrets.json"),
            Filter = T("JSON soubory (*.json)|*.json|Všechny soubory (*.*)|*.*", "JSON files (*.json)|*.json|All files (*.*)|*.*"),
            CheckFileExists = false,
            FileName = string.IsNullOrWhiteSpace(TokenManagerSecretsPath)
                ? "secrets.json"
                : TokenManagerSecretsPath
        };

        if (dialog.ShowDialog() == true)
            TokenManagerSecretsPath = dialog.FileName;
    }

    [RelayCommand]
    private void Reset()
    {
        var result = MessageBox.Show(
            T("Opravdu resetovat všechna nastavení na výchozí hodnoty?\nZměny budou aktivní až po kliknutí na Uložit.", "Reset all settings to defaults?\nChanges become active after clicking Save."),
            T("Reset na výchozí", "Reset to defaults"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            LoadDraft(new AppSettings());
    }

    [RelayCommand]
    private void Save()
    {
        var draft = BuildSettings();
        if (!_gatewayService.TryValidateOpenClawCommand(draft.OpenClawCommand, out var commandError))
        {
            MessageBox.Show(
                T("OpenClaw příkaz není bezpečný nebo platný:\n", "OpenClaw command is not safe or valid:\n") + commandError,
                T("Neplatný příkaz", "Invalid command"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!_settingsService.Save(draft))
        {
            MessageBox.Show(
                T("Uložení nastavení selhalo.\nZkontroluj, zda máš oprávnění zapisovat do ", "Saving settings failed.\nCheck write permissions for ") + _settingsService.SettingsFilePath,
                T("Chyba", "Error"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        ThemeService.Apply(draft.Theme);
        CloseAction?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseAction?.Invoke(false);

    private void BrowseFolder(string currentPath, Action<string> setPath)
    {
        var dialog = new OpenFolderDialog
        {
            Title = T("Vyber složku", "Select folder"),
            InitialDirectory = string.IsNullOrWhiteSpace(currentPath) ? "" : currentPath
        };

        if (dialog.ShowDialog() == true)
            setPath(dialog.FolderName);
    }

    private void LoadDraft(AppSettings settings)
    {
        OpenClawPath = settings.OpenClawPath;
        TempPath = settings.TempPath;
        OpenClawCommand = settings.OpenClawCommand;
        PowerShellWorkingDir = settings.PowerShellWorkingDir;
        TokenManagerSecretsPath = settings.TokenManagerSecretsPath;
        Language = string.Equals(settings.Language, "EN", StringComparison.OrdinalIgnoreCase) ? "EN" : "CS";
        Theme = settings.Theme;
        UseSplashVideo = settings.UseSplashVideo;
        UseButtonScanlineEffect = settings.UseButtonScanlineEffect;
    }

    private AppSettings BuildSettings()
    {
        var current = _settingsService.Settings;
        return new AppSettings
        {
            SchemaVersion = current.SchemaVersion,
            OpenClawPath = OpenClawPath.Trim(),
            TempPath = TempPath.Trim(),
            OpenClawCommand = OpenClawCommand.Trim(),
            PowerShellWorkingDir = PowerShellWorkingDir.Trim(),
            CleanupAgents = new List<string>(current.CleanupAgents),
            TokenManagerSecretsPath = TokenManagerSecretsPath.Trim(),
            Language = IsLanguageEnglish ? "EN" : "CS",
            AutoScrollAppLog = current.AutoScrollAppLog,
            Theme = Theme,
            UseSplashVideo = UseSplashVideo,
            UseButtonScanlineEffect = UseButtonScanlineEffect
        };
    }

    private void NotifyThemePropertiesChanged()
    {
        OnPropertyChanged(nameof(AreModernThemeOptionsEnabled));
        OnPropertyChanged(nameof(IsThemeLegacy));
        OnPropertyChanged(nameof(IsThemeModern));
        OnPropertyChanged(nameof(IsThemeStandardDark));
        OnPropertyChanged(nameof(IsThemeDark));
        OnPropertyChanged(nameof(IsThemeModernLight));
        OnPropertyChanged(nameof(IsThemeHighContrast));
        OnPropertyChanged(nameof(IsThemeCrabCute));
    }
}
