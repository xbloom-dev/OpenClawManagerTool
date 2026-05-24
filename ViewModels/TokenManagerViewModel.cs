using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class TokenManagerViewModel : ObservableObject
{
    private readonly ITokenService _tokenService;
    private readonly ISettingsService _settingsService;
    private string? _gitCheckPath;
    private bool? _isTrackedByGit;

    public TokenManagerViewModel(ITokenService tokenService, ISettingsService settingsService)
    {
        _tokenService = tokenService;
        _settingsService = settingsService;
        _settingsService.SettingsChanged += OnSettingsChanged;

        RefreshLocalization();
    }

    public event EventHandler? TokensReloaded;
    public event EventHandler? OutputAppended;
    public event Action<string>? ErrorOccurred;
    public event Action<string, Brush>? VerifyStatusChanged;

    public ObservableCollection<TokenEntry> Tokens { get; } = new();

    public string VaultPath => _settingsService.Settings.TokenManagerSecretsPath;

    [ObservableProperty]
    private string _vaultPathDisplay = "";

    [ObservableProperty]
    private string _outputLog = "";

    [ObservableProperty]
    private bool _isVaultSafetyWarningVisible;

    [ObservableProperty]
    private string _vaultSafetyWarningText = "";

    [ObservableProperty]
    private bool _isTokenSelected;

    [ObservableProperty] private string _windowTitle = "";
    [ObservableProperty] private string _grpVaultHeader = "";
    [ObservableProperty] private string _vaultLabel = "";
    [ObservableProperty] private string _btnInitVaultContent = "";
    [ObservableProperty] private string _btnInitVaultToolTip = "";
    [ObservableProperty] private string _btnRefreshContent = "";
    [ObservableProperty] private string _btnRefreshToolTip = "";
    [ObservableProperty] private string _btnOpenFolderContent = "";
    [ObservableProperty] private string _btnOpenFolderToolTip = "";
    [ObservableProperty] private string _btnAddGitIgnoreToolTip = "";
    [ObservableProperty] private string _btnBackupContent = "";
    [ObservableProperty] private string _btnBackupToolTip = "";
    [ObservableProperty] private string _btnRestoreVaultContent = "";
    [ObservableProperty] private string _btnRestoreVaultToolTip = "";
    [ObservableProperty] private string _grpTokensHeader = "";
    [ObservableProperty] private string _colDescriptionHeader = "";
    [ObservableProperty] private string _colCreatedHeader = "";
    [ObservableProperty] private string _colPreviewHeader = "";
    [ObservableProperty] private string _btnAddContent = "";
    [ObservableProperty] private string _btnAddToolTip = "";
    [ObservableProperty] private string _btnImportContent = "";
    [ObservableProperty] private string _btnImportToolTip = "";
    [ObservableProperty] private string _btnEditContent = "";
    [ObservableProperty] private string _btnEditToolTip = "";
    [ObservableProperty] private string _btnRemoveContent = "";
    [ObservableProperty] private string _btnRemoveToolTip = "";
    [ObservableProperty] private string _btnRotateContent = "";
    [ObservableProperty] private string _btnRotateToolTip = "";
    [ObservableProperty] private string _btnCopyPlaceholderContent = "";
    [ObservableProperty] private string _btnCopyPlaceholderToolTip = "";
    [ObservableProperty] private string _grpFileOpsHeader = "";
    [ObservableProperty] private string _fileLabel = "";
    [ObservableProperty] private string _btnBrowseContent = "";
    [ObservableProperty] private string _btnBrowseToolTip = "";
    [ObservableProperty] private string _btnRedactPreviewContent = "";
    [ObservableProperty] private string _btnRedactPreviewToolTip = "";
    [ObservableProperty] private string _btnRedactContent = "";
    [ObservableProperty] private string _btnRedactToolTip = "";
    [ObservableProperty] private string _btnRestoreFileContent = "";
    [ObservableProperty] private string _btnRestoreFileToolTip = "";
    [ObservableProperty] private string _btnVerifyContent = "";
    [ObservableProperty] private string _btnVerifyToolTip = "";
    [ObservableProperty] private string _chkRestoreInPlaceContent = "";
    [ObservableProperty] private string _chkRestoreInPlaceToolTip = "";
    [ObservableProperty] private string _btnCloseContent = "";
    [ObservableProperty] private string _btnCloseToolTip = "";
    [ObservableProperty] private string _verifyNotRunText = "";
    [ObservableProperty] private string _readyText = "";
    [ObservableProperty] private string _vaultMissingErrorText = "";
    [ObservableProperty] private string _targetMissingErrorText = "";

    public void RefreshLocalization()
    {
        WindowTitle = S("Str_Token_Title");
        GrpVaultHeader = S("Str_Token_GroupVault");
        VaultLabel = S("Str_Token_VaultLabel");
        BtnInitVaultContent = S("Str_Token_InitVault");
        BtnInitVaultToolTip = S("Str_Token_TipInitVault");
        BtnRefreshContent = S("Str_Token_Refresh");
        BtnRefreshToolTip = S("Str_Token_TipRefresh");
        BtnOpenFolderContent = S("Str_Token_Folder");
        BtnOpenFolderToolTip = S("Str_Token_TipOpenVaultFolder");
        BtnAddGitIgnoreToolTip = S("Str_Token_TipAddGitIgnore");
        BtnBackupContent = S("Str_Token_BackupVault");
        BtnBackupToolTip = S("Str_Token_TipBackupVault");
        BtnRestoreVaultContent = S("Str_Token_RestoreVault");
        BtnRestoreVaultToolTip = S("Str_Token_TipRestoreVault");
        GrpTokensHeader = S("Str_Token_GroupTokens");
        ColDescriptionHeader = S("Str_Token_Description");
        ColCreatedHeader = S("Str_Token_Created");
        ColPreviewHeader = S("Str_Token_Preview");
        BtnAddContent = S("Str_Token_Add");
        BtnAddToolTip = S("Str_Token_TipAdd");
        BtnImportContent = S("Str_Token_Import");
        BtnImportToolTip = S("Str_Token_TipImport");
        BtnEditContent = S("Str_Token_Edit");
        BtnEditToolTip = S("Str_Token_TipEdit");
        BtnRemoveContent = S("Str_Token_Delete");
        BtnRemoveToolTip = S("Str_Token_TipDelete");
        BtnRotateContent = S("Str_Token_Rotate");
        BtnRotateToolTip = S("Str_Token_TipRotate");
        BtnCopyPlaceholderContent = S("Str_Token_CopyPlaceholder");
        BtnCopyPlaceholderToolTip = S("Str_Token_TipCopyPlaceholder");
        GrpFileOpsHeader = S("Str_Token_GroupFileOps");
        FileLabel = S("Str_Token_File");
        BtnBrowseContent = S("Str_BtnBrowse");
        BtnBrowseToolTip = S("Str_Token_TipBrowseTarget");
        BtnRedactPreviewContent = S("Str_Token_RedactPreview");
        BtnRedactPreviewToolTip = S("Str_Token_TipRedactPreview");
        BtnRedactContent = S("Str_Token_Redact");
        BtnRedactToolTip = S("Str_Token_TipRedact");
        BtnRestoreFileContent = S("Str_Token_Restore");
        BtnRestoreFileToolTip = S("Str_Token_TipRestore");
        BtnVerifyContent = S("Str_Token_Verify");
        BtnVerifyToolTip = S("Str_Token_TipVerify");
        ChkRestoreInPlaceContent = S("Str_Token_RestoreInPlace");
        ChkRestoreInPlaceToolTip = S("Str_Token_TipRestoreInPlace");
        BtnCloseContent = S("Str_BtnClose");
        BtnCloseToolTip = S("Str_Token_TipClose");
        VerifyNotRunText = S("Str_Token_VerifyNotRun");
        ReadyText = S("Str_Token_Ready");
        VaultMissingErrorText = S("Str_Token_VaultMissingError");
        TargetMissingErrorText = S("Str_Token_TargetMissingError");
    }

    public void LoadTokens(bool forceGitCheck = false)
    {
        VaultPathDisplay = VaultPath;
        Tokens.Clear();
        UpdateVaultSafetyBanner();

        try
        {
            if (!File.Exists(VaultPath))
            {
                AppendOutput(F("Str_Token_VaultMissingInit", VaultPath));
                TokensReloaded?.Invoke(this, EventArgs.Empty);
                return;
            }

            var vault = _tokenService.LoadVault(VaultPath);
            foreach (var token in vault.Tokens.OrderBy(t => t.Id, StringComparer.Ordinal))
                Tokens.Add(token);

            AppendOutput(F("Str_Token_LoadedCount", Tokens.Count));
            AppendOutput(_tokenService.IsVaultEncryptedAtRest(VaultPath)
                ? S("Str_Token_VaultEncrypted")
                : S("Str_Token_VaultPlainWarning"));

            if (IsVaultTrackedByGitCached(forceGitCheck))
                AppendOutput(S("Str_Token_VaultGitWarning"));
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }

        TokensReloaded?.Invoke(this, EventArgs.Empty);
    }

    public bool IsVaultLocationSafe()
    {
        return _tokenService.AnalyzeVaultPath(VaultPath, _settingsService.Settings.OpenClawPath).IsSafe;
    }

    public string GetRiskyVaultMessage()
    {
        var safety = _tokenService.AnalyzeVaultPath(VaultPath, _settingsService.Settings.OpenClawPath);
        return F("Str_Token_RiskyVaultMessage", string.Join("\n", safety.Warnings));
    }

    public void InitVault()
    {
        try
        {
            var created = _tokenService.EnsureVaultExists(VaultPath);
            AppendOutput(created
                ? F("Str_Token_VaultCreated", VaultPath)
                : F("Str_Token_VaultExists", VaultPath));
            LoadTokens(forceGitCheck: true);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    public void AddToken(string id, string value, string description, bool imported = false)
    {
        try
        {
            _tokenService.AddToken(VaultPath, id, value, description);
            AppendOutput(F(imported ? "Str_Token_TokenImported" : "Str_Token_TokenAdded", id));
            LoadTokens();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    public void EditToken(string originalId, string id, string value, string description)
    {
        try
        {
            _tokenService.UpdateToken(VaultPath, originalId, id, value, description);
            AppendOutput(F("Str_Token_TokenUpdated", id));
            LoadTokens();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    public void RemoveToken(string id)
    {
        try
        {
            _tokenService.RemoveToken(VaultPath, id);
            AppendOutput(F("Str_Token_TokenDeleted", id));
            LoadTokens();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    public void RotateToken(string id, string newValue)
    {
        try
        {
            _tokenService.RotateToken(VaultPath, id, newValue);
            AppendOutput(F("Str_Token_TokenRotated", id));
            LoadTokens();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    public void CopyPlaceholder(string placeholder)
    {
        AppendOutput(F("Str_Token_PlaceholderCopied", placeholder));
    }

    public void AddVaultToGitIgnore()
    {
        try
        {
            var result = _tokenService.AddVaultToGitIgnore(VaultPath);
            AppendOutput(result.Added
                ? F("Str_Token_GitIgnoreAdded", result.Pattern)
                : F("Str_Token_GitIgnoreExists", result.Pattern));
            AppendOutput(F("Str_Token_RepoPath", result.RepoPath));
            _isTrackedByGit = null;
            LoadTokens(forceGitCheck: true);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    public async Task BackupVaultAsync(string outputPath, string password)
    {
        try
        {
            await _tokenService.ExportVaultAsync(VaultPath, outputPath, password);
            AppendOutput(F("Str_Token_BackupExported", outputPath));
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            throw;
        }
    }

    public async Task RestoreVaultAsync(string backupPath, string password)
    {
        try
        {
            await _tokenService.ImportVaultAsync(VaultPath, backupPath, password);
            AppendOutput(F("Str_Token_BackupImported", backupPath));
            LoadTokens(forceGitCheck: true);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            throw;
        }
    }

    public void PreviewRedact(string inputPath)
    {
        try
        {
            var outputPath = _tokenService.BuildDefaultOutputPath(inputPath, "redacted");
            var result = _tokenService.RedactFile(VaultPath, inputPath, outputPath, overwrite: true, dryRun: true);
            AppendOutput(F("Str_Token_RedactPreviewResult", result.TotalCount, result.UniqueCount));
            AppendOutput(result.TokenIds.Count > 0
                ? $"ID: {string.Join(", ", result.TokenIds)}"
                : S("Str_Token_NoTokensFound"));
            AppendOutput(F("Str_Token_OutputWouldBe", result.OutputPath));
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    public string GetDefaultOutputPath(string inputPath, string marker)
    {
        return _tokenService.BuildDefaultOutputPath(inputPath, marker);
    }

    public void RedactFile(string inputPath, string outputPath, bool overwrite)
    {
        try
        {
            var result = _tokenService.RedactFile(VaultPath, inputPath, outputPath, overwrite);
            AppendOutput(F("Str_Token_RedactedResult", result.TotalCount, result.UniqueCount));
            AppendOutput(F("Str_Token_OutputPath", result.OutputPath));
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    public TokenFileOperationResult RestoreFile(string inputPath, bool inPlace, string? outputPath = null, bool overwrite = false)
    {
        var result = inPlace
            ? _tokenService.RestoreFileInPlace(VaultPath, inputPath)
            : _tokenService.RestoreFile(VaultPath, inputPath, outputPath, overwrite);

        AppendOutput(F("Str_Token_RestoredResult", result.TotalCount, result.UniqueCount));
        AppendOutput(F("Str_Token_OutputPath", result.OutputPath));
        if (result.BackupPath != null)
            AppendOutput(F("Str_Token_BackupPath", result.BackupPath));
        if (result.UnknownPlaceholders.Count > 0)
            AppendOutput(F("Str_Token_UnknownPlaceholdersLog", string.Join(", ", result.UnknownPlaceholders)));

        return result;
    }

    public TokenVerifyResult VerifyFile(string inputPath)
    {
        var result = _tokenService.VerifyFile(VaultPath, inputPath);

        if (result.IsSafe)
        {
            AppendOutput(S("Str_Token_FileSafeLog"));
            VerifyStatusChanged?.Invoke(S("Str_Token_VerifySafeStatus"), Brushes.DarkGreen);
            return result;
        }

        if (result.FoundTokenIds.Count > 0)
        {
            AppendOutput(F("Str_Token_DoNotShareLog", string.Join(", ", result.FoundTokenIds)));
            VerifyStatusChanged?.Invoke(F("Str_Token_DoNotShareStatus", result.FoundTokenIds.Count), Brushes.DarkRed);
        }

        if (result.UnknownPlaceholders.Count > 0)
        {
            AppendOutput(F("Str_Token_StalePlaceholdersLog", string.Join(", ", result.UnknownPlaceholders)));
            if (result.FoundTokenIds.Count == 0)
                VerifyStatusChanged?.Invoke(F("Str_Token_StalePlaceholdersStatus", result.UnknownPlaceholders.Count), Brushes.DarkOrange);
        }

        return result;
    }

    public void AppendOutput(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        OutputLog += $"[{timestamp}] {message}\n";
        OutputAppended?.Invoke(this, EventArgs.Empty);
    }

    public void ShowError(string message)
    {
        AppendOutput(F("Str_Token_Error", message));
        ErrorOccurred?.Invoke(message);
    }

    public void Cleanup()
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;
    }

    private static string S(string key) => L10n.Get(key);
    private static string F(string key, params object[] args) => L10n.Format(key, args);

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        _gitCheckPath = null;
        _isTrackedByGit = null;
        RefreshLocalization();
        LoadTokens(forceGitCheck: true);
    }

    private bool IsVaultTrackedByGitCached(bool force)
    {
        if (!force &&
            string.Equals(_gitCheckPath, VaultPath, StringComparison.OrdinalIgnoreCase) &&
            _isTrackedByGit.HasValue)
            return _isTrackedByGit.Value;

        _gitCheckPath = VaultPath;
        _isTrackedByGit = _tokenService.IsVaultTrackedByGit(VaultPath);
        return _isTrackedByGit.Value;
    }

    private void UpdateVaultSafetyBanner()
    {
        var safety = _tokenService.AnalyzeVaultPath(VaultPath, _settingsService.Settings.OpenClawPath);
        IsVaultSafetyWarningVisible = !safety.IsSafe;
        VaultSafetyWarningText = safety.IsSafe
            ? ""
            : S("Str_Token_SecurityWarningPrefix") + string.Join(" ", safety.Warnings);
    }
}
