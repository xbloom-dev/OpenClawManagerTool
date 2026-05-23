using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class TokenManagerWindow : Window
{
    private readonly ObservableCollection<TokenEntry> _tokens = new();
    private string? _gitCheckPath;
    private bool? _isTrackedByGit;

    private string VaultPath => OpenClawManager.App.GetService<ISettingsService>().Settings.TokenManagerSecretsPath;
    private static string S(string key) => L10n.Get(key);
    private static string F(string key, params object[] args) => L10n.Format(key, args);
    private static Brush ActionPositiveBrush =>
        ThemeService.GetBrush("Brush.ActionPositive", Color.FromRgb(0xD0, 0xFF, 0xD0));
    private static Brush ActionDangerBrush =>
        ThemeService.GetBrush("Brush.ActionDanger", Color.FromRgb(0xFF, 0xD0, 0xD0));
    private static Brush ActionUtilityBrush =>
        ThemeService.GetBrush("Brush.ActionUtility", Color.FromRgb(0xD0, 0xE8, 0xFF));

    public TokenManagerWindow()
    {
        InitializeComponent();
        ModernPaletteRuntimeStyles.ApplyIfModernPalette(this);

        TokenGrid.ItemsSource = _tokens;
        TokenGrid.SelectionChanged += (_, _) => UpdateTokenButtons();

        BtnInitVault.Click += (_, _) => InitVault();
        BtnRefresh.Click += (_, _) => LoadTokens(forceGitCheck: true);
        BtnOpenVaultFolder.Click += (_, _) => OpenVaultFolder();
        BtnAddGitIgnore.Click += (_, _) => AddVaultToGitIgnore();
        BtnBackupVault.Click += async (_, _) => await BackupVaultAsync();
        BtnRestoreVault.Click += async (_, _) => await RestoreVaultAsync();
        BtnAdd.Click += (_, _) => AddToken();
        BtnImport.Click += (_, _) => ImportTokenFromFile();
        BtnEdit.Click += (_, _) => EditSelectedToken();
        BtnRemove.Click += (_, _) => RemoveSelectedToken();
        BtnRotate.Click += (_, _) => RotateSelectedToken();
        BtnCopyPlaceholder.Click += (_, _) => CopySelectedPlaceholder();
        BtnBrowseTarget.Click += (_, _) => BrowseTargetFile();
        BtnRedactPreview.Click += (_, _) => PreviewRedactSelectedFile();
        BtnRedact.Click += (_, _) => RedactSelectedFile();
        BtnRestore.Click += (_, _) => RestoreSelectedFile();
        BtnVerify.Click += (_, _) => VerifySelectedFile();
        BtnClose.Click += (_, _) => Close();

        OpenClawManager.App.GetService<ISettingsService>().SettingsChanged += SettingsService_SettingsChanged;
        Closed += (_, _) => OpenClawManager.App.GetService<ISettingsService>().SettingsChanged -= SettingsService_SettingsChanged;

        ApplyLocalization();
        TxtVaultPath.Text = VaultPath;
        SetVerifyStatus(S("Str_Token_VerifyNotRun"), Brushes.Gray);
        AppendOutput(S("Str_Token_Ready"));
        LoadTokens();
    }

    private void ApplyLocalization()
    {
        Title = S("Str_Token_Title");
        GrpVault.Header = S("Str_Token_GroupVault");
        TxtVaultLabel.Text = S("Str_Token_VaultLabel");
        BtnInitVault.Content = S("Str_Token_InitVault");
        BtnInitVault.ToolTip = S("Str_Token_TipInitVault");
        BtnRefresh.Content = S("Str_Token_Refresh");
        BtnRefresh.ToolTip = S("Str_Token_TipRefresh");
        BtnOpenVaultFolder.Content = S("Str_Token_Folder");
        BtnOpenVaultFolder.ToolTip = S("Str_Token_TipOpenVaultFolder");
        BtnAddGitIgnore.ToolTip = S("Str_Token_TipAddGitIgnore");
        BtnBackupVault.Content = S("Str_Token_BackupVault");
        BtnBackupVault.ToolTip = S("Str_Token_TipBackupVault");
        BtnRestoreVault.Content = S("Str_Token_RestoreVault");
        BtnRestoreVault.ToolTip = S("Str_Token_TipRestoreVault");
        GrpTokens.Header = S("Str_Token_GroupTokens");
        ColDescription.Header = S("Str_Token_Description");
        ColCreated.Header = S("Str_Token_Created");
        ColPreview.Header = S("Str_Token_Preview");
        BtnAdd.Content = S("Str_Token_Add");
        BtnAdd.ToolTip = S("Str_Token_TipAdd");
        BtnImport.Content = S("Str_Token_Import");
        BtnImport.ToolTip = S("Str_Token_TipImport");
        BtnEdit.Content = S("Str_Token_Edit");
        BtnEdit.ToolTip = S("Str_Token_TipEdit");
        BtnRemove.Content = S("Str_Token_Delete");
        BtnRemove.ToolTip = S("Str_Token_TipDelete");
        BtnRotate.Content = S("Str_Token_Rotate");
        BtnRotate.ToolTip = S("Str_Token_TipRotate");
        BtnCopyPlaceholder.Content = S("Str_Token_CopyPlaceholder");
        BtnCopyPlaceholder.ToolTip = S("Str_Token_TipCopyPlaceholder");
        GrpFileOps.Header = S("Str_Token_GroupFileOps");
        TxtFileLabel.Text = S("Str_Token_File");
        BtnBrowseTarget.Content = S("Str_BtnBrowse");
        BtnBrowseTarget.Background = ActionUtilityBrush;
        BtnBrowseTarget.ToolTip = S("Str_Token_TipBrowseTarget");
        BtnRedactPreview.Content = S("Str_Token_RedactPreview");
        BtnRedactPreview.ToolTip = S("Str_Token_TipRedactPreview");
        BtnRedact.Content = S("Str_Token_Redact");
        BtnRedact.Background = ActionPositiveBrush;
        BtnRedact.FontWeight = FontWeights.Bold;
        BtnRedact.ToolTip = S("Str_Token_TipRedact");
        BtnRestore.Content = S("Str_Token_Restore");
        BtnRestore.Background = ActionDangerBrush;
        BtnRestore.ToolTip = S("Str_Token_TipRestore");
        BtnVerify.Content = S("Str_Token_Verify");
        BtnVerify.Background = ActionUtilityBrush;
        BtnVerify.FontWeight = FontWeights.Bold;
        BtnVerify.ToolTip = S("Str_Token_TipVerify");
        ChkRestoreInPlace.Content = S("Str_Token_RestoreInPlace");
        ChkRestoreInPlace.ToolTip = S("Str_Token_TipRestoreInPlace");
        BtnClose.Content = S("Str_BtnClose");
        BtnClose.Background = ActionDangerBrush;
        BtnClose.ToolTip = S("Str_Token_TipClose");
    }

    private void SettingsService_SettingsChanged(object? sender, EventArgs e)
    {
        _gitCheckPath = null;
        _isTrackedByGit = null;
        LoadTokens(forceGitCheck: true);
    }

    private void InitVault()
    {
        if (!ConfirmRiskyVaultLocation()) return;
        try
        {
            var created = TokenService.EnsureVaultExists(VaultPath);
            AppendOutput(created ? F("Str_Token_VaultCreated", VaultPath) : F("Str_Token_VaultExists", VaultPath));
            LoadTokens(forceGitCheck: true);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private bool ConfirmRiskyVaultLocation()
    {
        var safety = TokenService.AnalyzeVaultPath(VaultPath, OpenClawManager.App.GetService<ISettingsService>().Settings.OpenClawPath);
        if (safety.IsSafe) return true;
        var message = F("Str_Token_RiskyVaultMessage", string.Join("\n", safety.Warnings));
        return MessageBox.Show(message, S("Str_Token_RiskyVaultTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private void LoadTokens(bool forceGitCheck = false)
    {
        TxtVaultPath.Text = VaultPath;
        _tokens.Clear();
        UpdateVaultSafetyBanner();
        try
        {
            if (!File.Exists(VaultPath))
            {
                AppendOutput(F("Str_Token_VaultMissingInit", VaultPath));
                UpdateTokenButtons();
                return;
            }
            var vault = TokenService.LoadVault(VaultPath);
            foreach (var token in vault.Tokens.OrderBy(t => t.Id, StringComparer.Ordinal)) _tokens.Add(token);
            AppendOutput(F("Str_Token_LoadedCount", _tokens.Count));
            AppendOutput(TokenService.IsVaultEncryptedAtRest(VaultPath) ? S("Str_Token_VaultEncrypted") : S("Str_Token_VaultPlainWarning"));
            if (IsVaultTrackedByGitCached(forceGitCheck)) AppendOutput(S("Str_Token_VaultGitWarning"));
        }
        catch (Exception ex) { ShowError(ex.Message); }
        UpdateTokenButtons();
    }

    private bool IsVaultTrackedByGitCached(bool force)
    {
        if (!force && string.Equals(_gitCheckPath, VaultPath, StringComparison.OrdinalIgnoreCase) && _isTrackedByGit.HasValue) return _isTrackedByGit.Value;
        _gitCheckPath = VaultPath;
        _isTrackedByGit = TokenService.IsVaultTrackedByGit(VaultPath);
        return _isTrackedByGit.Value;
    }

    private void UpdateVaultSafetyBanner()
    {
        var safety = TokenService.AnalyzeVaultPath(VaultPath, OpenClawManager.App.GetService<ISettingsService>().Settings.OpenClawPath);
        VaultWarningBanner.Visibility = safety.IsSafe ? Visibility.Collapsed : Visibility.Visible;
        TxtVaultWarning.Text = safety.IsSafe ? "" : S("Str_Token_SecurityWarningPrefix") + string.Join(" ", safety.Warnings);
    }

    private void AddToken()
    {
        var dialog = new TokenEditWindow(S("Str_Token_AddTitle")) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try { TokenService.AddToken(VaultPath, dialog.TokenId, dialog.TokenValue, dialog.TokenDescription); AppendOutput(F("Str_Token_TokenAdded", dialog.TokenId)); LoadTokens(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ImportTokenFromFile()
    {
        var fileDialog = new OpenFileDialog { Title = S("Str_Token_FileDialogImportTitle"), Filter = S("Str_Token_FileDialogAllFiles"), CheckFileExists = true };
        if (fileDialog.ShowDialog() != true) return;
        var dialog = new TokenImportWindow(fileDialog.FileName) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try { TokenService.AddToken(VaultPath, dialog.TokenId, dialog.TokenValue, dialog.TokenDescription); AppendOutput(F("Str_Token_TokenImported", dialog.TokenId)); LoadTokens(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void EditSelectedToken()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token) return;
        var dialog = new TokenEditWindow(S("Str_Token_EditTitle"), token) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try { TokenService.UpdateToken(VaultPath, token.Id, dialog.TokenId, dialog.TokenValue, dialog.TokenDescription); AppendOutput(F("Str_Token_TokenUpdated", dialog.TokenId)); LoadTokens(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void RemoveSelectedToken()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token) return;
        if (MessageBox.Show(F("Str_Token_DeleteConfirm", token.Id), S("Str_Token_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { TokenService.RemoveToken(VaultPath, token.Id); AppendOutput(F("Str_Token_TokenDeleted", token.Id)); LoadTokens(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void RotateSelectedToken()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token) return;
        var dialog = new TokenEditWindow(S("Str_Token_RotateTitle"), token, idReadOnly: true, rotateOnly: true) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try { TokenService.RotateToken(VaultPath, token.Id, dialog.TokenValue); AppendOutput(F("Str_Token_TokenRotated", token.Id)); LoadTokens(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void CopySelectedPlaceholder()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token) return;
        Clipboard.SetText(token.Placeholder);
        AppendOutput(F("Str_Token_PlaceholderCopied", token.Placeholder));
    }

    private void OpenVaultFolder()
    {
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(Environment.ExpandEnvironmentVariables(VaultPath)));
            if (string.IsNullOrWhiteSpace(directory)) return;
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{directory}\"", UseShellExecute = true });
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private async Task BackupVaultAsync()
    {
        if (!File.Exists(VaultPath)) { ShowError(S("Str_Token_VaultMissingError")); return; }

        var saveDialog = new SaveFileDialog
        {
            Title = S("Str_Token_BackupDialogTitle"),
            DefaultExt = ".ocvault",
            Filter = S("Str_Token_BackupFileFilter"),
            FileName = "openclaw-token-vault.ocvault",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (saveDialog.ShowDialog(this) != true) return;

        var passwordDialog = new PasswordPromptWindow(
            S("Str_Token_BackupPasswordTitle"),
            S("Str_Token_BackupPasswordMessage"),
            requireConfirmation: true)
        {
            Owner = this
        };

        if (passwordDialog.ShowDialog() != true) return;

        try
        {
            await TokenService.ExportVaultAsync(VaultPath, saveDialog.FileName, passwordDialog.Password);
            AppendOutput(F("Str_Token_BackupExported", saveDialog.FileName));
            MessageBox.Show(S("Str_Token_BackupExportedMessage"), S("Str_Token_BackupVault"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private async Task RestoreVaultAsync()
    {
        var openDialog = new OpenFileDialog
        {
            Title = S("Str_Token_RestoreDialogTitle"),
            DefaultExt = ".ocvault",
            Filter = S("Str_Token_BackupFileFilter"),
            CheckFileExists = true
        };

        if (openDialog.ShowDialog(this) != true) return;

        var confirm = MessageBox.Show(
            S("Str_Token_RestoreVaultConfirm"),
            S("Str_Token_RestoreVault"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        var passwordDialog = new PasswordPromptWindow(
            S("Str_Token_RestorePasswordTitle"),
            S("Str_Token_RestorePasswordMessage"),
            requireConfirmation: false)
        {
            Owner = this
        };

        if (passwordDialog.ShowDialog() != true) return;

        try
        {
            await TokenService.ImportVaultAsync(VaultPath, openDialog.FileName, passwordDialog.Password);
            AppendOutput(F("Str_Token_BackupImported", openDialog.FileName));
            LoadTokens(forceGitCheck: true);
            MessageBox.Show(S("Str_Token_BackupImportedMessage"), S("Str_Token_RestoreVault"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void BrowseTargetFile()
    {
        var dialog = new OpenFileDialog { Title = S("Str_Token_FileDialogTargetTitle"), Filter = S("Str_Token_FileDialogAllFiles"), CheckFileExists = true };
        if (dialog.ShowDialog() == true) TxtTargetFile.Text = dialog.FileName;
    }

    private void PreviewRedactSelectedFile()
    {
        if (!EnsureVaultAndTarget()) return;
        try
        {
            var inputPath = TxtTargetFile.Text.Trim();
            var outputPath = TokenService.BuildDefaultOutputPath(inputPath, "redacted");
            var result = TokenService.RedactFile(VaultPath, inputPath, outputPath, overwrite: true, dryRun: true);
            AppendOutput(F("Str_Token_RedactPreviewResult", result.TotalCount, result.UniqueCount));
            AppendOutput(result.TokenIds.Count > 0 ? $"ID: {string.Join(", ", result.TokenIds)}" : S("Str_Token_NoTokensFound"));
            AppendOutput(F("Str_Token_OutputWouldBe", result.OutputPath));
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void RedactSelectedFile()
    {
        if (!EnsureVaultAndTarget()) return;
        var inputPath = TxtTargetFile.Text.Trim();
        var outputPath = TokenService.BuildDefaultOutputPath(inputPath, "redacted");
        var overwrite = false;
        if (File.Exists(outputPath))
        {
            var confirm = MessageBox.Show(F("Str_Token_OutputExistsConfirm", outputPath), S("Str_Token_OverwriteOutput"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;
            overwrite = true;
        }
        try { var result = TokenService.RedactFile(VaultPath, inputPath, outputPath, overwrite); AppendOutput(F("Str_Token_RedactedResult", result.TotalCount, result.UniqueCount)); AppendOutput(F("Str_Token_OutputPath", result.OutputPath)); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void RestoreSelectedFile()
    {
        if (!EnsureVaultAndTarget()) return;
        var inputPath = TxtTargetFile.Text.Trim();
        var inplace = ChkRestoreInPlace.IsChecked == true;
        var confirmText = inplace ? S("Str_Token_RestoreInPlaceConfirm") : S("Str_Token_RestoreCopyConfirm");
        if (MessageBox.Show(confirmText, S("Str_Token_RestoreFileTitle"), MessageBoxButton.YesNo, inplace ? MessageBoxImage.Warning : MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            TokenFileOperationResult result;
            if (inplace)
            {
                result = TokenService.RestoreFileInPlace(VaultPath, inputPath);
            }
            else
            {
                var outputPath = TokenService.BuildDefaultOutputPath(inputPath, "restored");
                var overwrite = false;
                if (File.Exists(outputPath))
                {
                    var overwriteConfirm = MessageBox.Show(F("Str_Token_OutputExistsConfirm", outputPath), S("Str_Token_OverwriteOutput"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (overwriteConfirm != MessageBoxResult.Yes) return;
                    overwrite = true;
                }
                result = TokenService.RestoreFile(VaultPath, inputPath, outputPath, overwrite);
            }
            AppendOutput(F("Str_Token_RestoredResult", result.TotalCount, result.UniqueCount));
            AppendOutput(F("Str_Token_OutputPath", result.OutputPath));
            if (result.BackupPath != null) AppendOutput(F("Str_Token_BackupPath", result.BackupPath));
            if (result.UnknownPlaceholders.Count > 0)
            {
                AppendOutput(F("Str_Token_UnknownPlaceholdersLog", string.Join(", ", result.UnknownPlaceholders)));
                MessageBox.Show(F("Str_Token_UnknownPlaceholdersMessage", string.Join("\n", result.UnknownPlaceholders)), S("Str_Token_UnknownPlaceholdersTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void VerifySelectedFile()
    {
        if (!EnsureVaultAndTarget()) return;
        try
        {
            var result = TokenService.VerifyFile(VaultPath, TxtTargetFile.Text.Trim());
            if (result.IsSafe)
            {
                AppendOutput(S("Str_Token_FileSafeLog"));
                SetVerifyStatus(S("Str_Token_VerifySafeStatus"), Brushes.DarkGreen);
                MessageBox.Show(S("Str_Token_FileSafeMessage"), S("Str_Token_VerifyTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (result.FoundTokenIds.Count > 0)
            {
                AppendOutput(F("Str_Token_DoNotShareLog", string.Join(", ", result.FoundTokenIds)));
                SetVerifyStatus(F("Str_Token_DoNotShareStatus", result.FoundTokenIds.Count), Brushes.DarkRed);
            }
            if (result.UnknownPlaceholders.Count > 0)
            {
                AppendOutput(F("Str_Token_StalePlaceholdersLog", string.Join(", ", result.UnknownPlaceholders)));
                if (result.FoundTokenIds.Count == 0) SetVerifyStatus(F("Str_Token_StalePlaceholdersStatus", result.UnknownPlaceholders.Count), Brushes.DarkOrange);
            }
            MessageBox.Show(S("Str_Token_FileUnsafeMessage"), S("Str_Token_VerifyTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private bool EnsureVaultAndTarget()
    {
        if (!File.Exists(VaultPath)) { ShowError(S("Str_Token_VaultMissingError")); return false; }
        if (string.IsNullOrWhiteSpace(TxtTargetFile.Text) || !File.Exists(TxtTargetFile.Text.Trim())) { ShowError(S("Str_Token_TargetMissingError")); return false; }
        return true;
    }

    private void UpdateTokenButtons()
    {
        var hasSelection = TokenGrid.SelectedItem is TokenEntry;
        BtnEdit.IsEnabled = hasSelection;
        BtnRemove.IsEnabled = hasSelection;
        BtnRotate.IsEnabled = hasSelection;
        BtnCopyPlaceholder.IsEnabled = hasSelection;
    }

    private void AddVaultToGitIgnore()
    {
        try
        {
            var result = TokenService.AddVaultToGitIgnore(VaultPath);
            AppendOutput(result.Added ? F("Str_Token_GitIgnoreAdded", result.Pattern) : F("Str_Token_GitIgnoreExists", result.Pattern));
            AppendOutput(F("Str_Token_RepoPath", result.RepoPath));
            _isTrackedByGit = null;
            LoadTokens(forceGitCheck: true);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ShowError(string message)
    {
        AppendOutput(F("Str_Token_Error", message));
        MessageBox.Show(message, S("Str_Token_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void SetVerifyStatus(string message, Brush color)
    {
        TxtVerifyStatus.Text = message;
        TxtVerifyStatus.Foreground = color;
    }

    private void AppendOutput(string message)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        TxtOutput.AppendText($"[{ts}] {message}\n");
        TxtOutput.ScrollToEnd();
    }
}
