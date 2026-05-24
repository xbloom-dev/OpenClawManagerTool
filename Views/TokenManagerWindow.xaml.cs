using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using OpenClawManager.Models;
using OpenClawManager.Services;
using OpenClawManager.ViewModels;

namespace OpenClawManager.Views;

public partial class TokenManagerWindow : Window
{
    private readonly TokenManagerViewModel _viewModel;

    public TokenManagerWindow()
        : this(OpenClawManager.App.GetService<ITokenService>(), OpenClawManager.App.GetService<ISettingsService>())
    {
    }

    public TokenManagerWindow(ITokenService tokenService, ISettingsService settingsService)
        : this(new TokenManagerViewModel(tokenService, settingsService))
    {
    }

    public TokenManagerWindow(TokenManagerViewModel viewModel)
    {
        InitializeComponent();
        ModernPaletteRuntimeStyles.ApplyIfModernPalette(this);

        _viewModel = viewModel;
        DataContext = _viewModel;

        TokenGrid.ItemsSource = _viewModel.Tokens;
        TokenGrid.SelectionChanged += (_, _) => _viewModel.IsTokenSelected = TokenGrid.SelectedItem is TokenEntry;

        _viewModel.OutputAppended += (_, _) => TxtOutput.ScrollToEnd();
        _viewModel.TokensReloaded += (_, _) => _viewModel.IsTokenSelected = TokenGrid.SelectedItem is TokenEntry;
        _viewModel.ErrorOccurred += message => MessageBox.Show(
            message,
            L10n.Get("Str_Token_Title"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        _viewModel.VerifyStatusChanged += SetVerifyStatus;

        BtnInitVault.Click += (_, _) => InitVault();
        BtnRefresh.Click += (_, _) => _viewModel.LoadTokens(forceGitCheck: true);
        BtnOpenVaultFolder.Click += (_, _) => OpenVaultFolder();
        BtnAddGitIgnore.Click += (_, _) => _viewModel.AddVaultToGitIgnore();
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

        Closed += (_, _) => _viewModel.Cleanup();

        SetVerifyStatus(_viewModel.VerifyNotRunText, Brushes.Gray);
        _viewModel.AppendOutput(_viewModel.ReadyText);
        _viewModel.LoadTokens();
    }

    private void InitVault()
    {
        if (!_viewModel.IsVaultLocationSafe())
        {
            var message = _viewModel.GetRiskyVaultMessage();
            if (MessageBox.Show(
                    message,
                    L10n.Get("Str_Token_RiskyVaultTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
        }

        _viewModel.InitVault();
    }

    private void AddToken()
    {
        var dialog = new TokenEditWindow(L10n.Get("Str_Token_AddTitle")) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        _viewModel.AddToken(dialog.TokenId, dialog.TokenValue, dialog.TokenDescription);
    }

    private void ImportTokenFromFile()
    {
        var fileDialog = new OpenFileDialog
        {
            Title = L10n.Get("Str_Token_FileDialogImportTitle"),
            Filter = L10n.Get("Str_Token_FileDialogAllFiles"),
            CheckFileExists = true
        };
        if (fileDialog.ShowDialog() != true)
            return;

        var dialog = new TokenImportWindow(fileDialog.FileName) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        _viewModel.AddToken(dialog.TokenId, dialog.TokenValue, dialog.TokenDescription, imported: true);
    }

    private void EditSelectedToken()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token)
            return;

        var dialog = new TokenEditWindow(L10n.Get("Str_Token_EditTitle"), token) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        _viewModel.EditToken(token.Id, dialog.TokenId, dialog.TokenValue, dialog.TokenDescription);
    }

    private void RemoveSelectedToken()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token)
            return;

        if (MessageBox.Show(
                L10n.Format("Str_Token_DeleteConfirm", token.Id),
                L10n.Get("Str_Token_DeleteTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        _viewModel.RemoveToken(token.Id);
    }

    private void RotateSelectedToken()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token)
            return;

        var dialog = new TokenEditWindow(
            L10n.Get("Str_Token_RotateTitle"),
            token,
            idReadOnly: true,
            rotateOnly: true)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true)
            return;

        _viewModel.RotateToken(token.Id, dialog.TokenValue);
    }

    private void CopySelectedPlaceholder()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token)
            return;

        Clipboard.SetText(token.Placeholder);
        _viewModel.CopyPlaceholder(token.Placeholder);
    }

    private void OpenVaultFolder()
    {
        try
        {
            var directory = Path.GetDirectoryName(
                Path.GetFullPath(Environment.ExpandEnvironmentVariables(_viewModel.VaultPath)));
            if (string.IsNullOrWhiteSpace(directory))
                return;

            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{directory}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _viewModel.ShowError(ex.Message);
        }
    }

    private async Task BackupVaultAsync()
    {
        if (!File.Exists(_viewModel.VaultPath))
        {
            _viewModel.ShowError(_viewModel.VaultMissingErrorText);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = L10n.Get("Str_Token_BackupDialogTitle"),
            DefaultExt = ".ocvault",
            Filter = L10n.Get("Str_Token_BackupFileFilter"),
            FileName = "openclaw-token-vault.ocvault",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (saveDialog.ShowDialog(this) != true)
            return;

        var passwordDialog = new PasswordPromptWindow(
            L10n.Get("Str_Token_BackupPasswordTitle"),
            L10n.Get("Str_Token_BackupPasswordMessage"),
            requireConfirmation: true)
        {
            Owner = this
        };
        if (passwordDialog.ShowDialog() != true)
            return;

        try
        {
            await _viewModel.BackupVaultAsync(saveDialog.FileName, passwordDialog.Password);
            MessageBox.Show(
                L10n.Get("Str_Token_BackupExportedMessage"),
                L10n.Get("Str_Token_BackupVault"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch
        {
        }
    }

    private async Task RestoreVaultAsync()
    {
        var openDialog = new OpenFileDialog
        {
            Title = L10n.Get("Str_Token_RestoreDialogTitle"),
            DefaultExt = ".ocvault",
            Filter = L10n.Get("Str_Token_BackupFileFilter"),
            CheckFileExists = true
        };
        if (openDialog.ShowDialog(this) != true)
            return;

        if (MessageBox.Show(
                L10n.Get("Str_Token_RestoreVaultConfirm"),
                L10n.Get("Str_Token_RestoreVault"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var passwordDialog = new PasswordPromptWindow(
            L10n.Get("Str_Token_RestorePasswordTitle"),
            L10n.Get("Str_Token_RestorePasswordMessage"),
            requireConfirmation: false)
        {
            Owner = this
        };
        if (passwordDialog.ShowDialog() != true)
            return;

        try
        {
            await _viewModel.RestoreVaultAsync(openDialog.FileName, passwordDialog.Password);
            MessageBox.Show(
                L10n.Get("Str_Token_BackupImportedMessage"),
                L10n.Get("Str_Token_RestoreVault"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch
        {
        }
    }

    private void BrowseTargetFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = L10n.Get("Str_Token_FileDialogTargetTitle"),
            Filter = L10n.Get("Str_Token_FileDialogAllFiles"),
            CheckFileExists = true
        };
        if (dialog.ShowDialog() == true)
            TxtTargetFile.Text = dialog.FileName;
    }

    private void PreviewRedactSelectedFile()
    {
        if (!EnsureVaultAndTarget())
            return;

        _viewModel.PreviewRedact(TxtTargetFile.Text.Trim());
    }

    private void RedactSelectedFile()
    {
        if (!EnsureVaultAndTarget())
            return;

        var inputPath = TxtTargetFile.Text.Trim();
        var outputPath = _viewModel.GetDefaultOutputPath(inputPath, "redacted");
        var overwrite = false;

        if (File.Exists(outputPath))
        {
            if (MessageBox.Show(
                    L10n.Format("Str_Token_OutputExistsConfirm", outputPath),
                    L10n.Get("Str_Token_OverwriteOutput"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            overwrite = true;
        }

        _viewModel.RedactFile(inputPath, outputPath, overwrite);
    }

    private void RestoreSelectedFile()
    {
        if (!EnsureVaultAndTarget())
            return;

        var inputPath = TxtTargetFile.Text.Trim();
        var inPlace = ChkRestoreInPlace.IsChecked == true;
        var confirmText = inPlace
            ? L10n.Get("Str_Token_RestoreInPlaceConfirm")
            : L10n.Get("Str_Token_RestoreCopyConfirm");

        if (MessageBox.Show(
                confirmText,
                L10n.Get("Str_Token_RestoreFileTitle"),
                MessageBoxButton.YesNo,
                inPlace ? MessageBoxImage.Warning : MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        string? outputPath = null;
        var overwrite = false;

        if (!inPlace)
        {
            outputPath = _viewModel.GetDefaultOutputPath(inputPath, "restored");
            if (File.Exists(outputPath))
            {
                if (MessageBox.Show(
                        L10n.Format("Str_Token_OutputExistsConfirm", outputPath),
                        L10n.Get("Str_Token_OverwriteOutput"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                overwrite = true;
            }
        }

        try
        {
            var result = _viewModel.RestoreFile(inputPath, inPlace, outputPath, overwrite);
            if (result.UnknownPlaceholders.Count > 0)
            {
                MessageBox.Show(
                    L10n.Format("Str_Token_UnknownPlaceholdersMessage", string.Join("\n", result.UnknownPlaceholders)),
                    L10n.Get("Str_Token_UnknownPlaceholdersTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            _viewModel.ShowError(ex.Message);
        }
    }

    private void VerifySelectedFile()
    {
        if (!EnsureVaultAndTarget())
            return;

        try
        {
            var result = _viewModel.VerifyFile(TxtTargetFile.Text.Trim());
            var message = result.IsSafe
                ? L10n.Get("Str_Token_FileSafeMessage")
                : L10n.Get("Str_Token_FileUnsafeMessage");
            var icon = result.IsSafe ? MessageBoxImage.Information : MessageBoxImage.Warning;

            MessageBox.Show(message, L10n.Get("Str_Token_VerifyTitle"), MessageBoxButton.OK, icon);
        }
        catch (Exception ex)
        {
            _viewModel.ShowError(ex.Message);
        }
    }

    private bool EnsureVaultAndTarget()
    {
        if (!File.Exists(_viewModel.VaultPath))
        {
            _viewModel.ShowError(_viewModel.VaultMissingErrorText);
            return false;
        }

        if (string.IsNullOrWhiteSpace(TxtTargetFile.Text) || !File.Exists(TxtTargetFile.Text.Trim()))
        {
            _viewModel.ShowError(_viewModel.TargetMissingErrorText);
            return false;
        }

        return true;
    }

    private void SetVerifyStatus(string message, Brush color)
    {
        TxtVerifyStatus.Text = message;
        TxtVerifyStatus.Foreground = color;
    }
}
