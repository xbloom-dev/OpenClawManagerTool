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

    private string VaultPath => SettingsService.Current.TokenManagerSecretsPath;
    private bool Cs => L10n.IsCzech;
    private string T(string cs, string en) => Cs ? cs : en;

    public TokenManagerWindow()
    {
        InitializeComponent();

        TokenGrid.ItemsSource = _tokens;
        TokenGrid.SelectionChanged += (_, _) => UpdateTokenButtons();

        BtnInitVault.Click += (_, _) => InitVault();
        BtnRefresh.Click += (_, _) => LoadTokens(forceGitCheck: true);
        BtnOpenVaultFolder.Click += (_, _) => OpenVaultFolder();
        BtnAddGitIgnore.Click += (_, _) => AddVaultToGitIgnore();
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

        SettingsService.SettingsChanged += SettingsService_SettingsChanged;
        Closed += (_, _) => SettingsService.SettingsChanged -= SettingsService_SettingsChanged;

        ApplyLocalization();
        TxtVaultPath.Text = VaultPath;
        SetVerifyStatus(T("Verify: zatím neprovedeno", "Verify: not run yet"), Brushes.Gray);
        AppendOutput(T("Token Manager připraven.", "Token Manager ready."));
        LoadTokens();
    }

    private void ApplyLocalization()
    {
        Title = T("OpenClaw Manager - Správce API klíčů", "OpenClaw Manager - Token Manager");
        GrpVault.Header = "Vault";
        TxtVaultLabel.Text = "secrets.json:";
        BtnInitVault.Content = T("Inicializovat", "Initialize");
        BtnRefresh.Content = T("Obnovit", "Refresh");
        BtnOpenVaultFolder.Content = T("Složka", "Folder");
        BtnOpenVaultFolder.ToolTip = T("Otevře složku vaultu.", "Opens the vault folder.");
        BtnAddGitIgnore.ToolTip = T("Přidá cestu k vaultu do nejbližšího .gitignore.", "Adds the vault path to the nearest .gitignore.");
        GrpTokens.Header = T("Tokeny", "Tokens");
        ColDescription.Header = T("Popis", "Description");
        ColCreated.Header = T("Vytvořeno", "Created");
        BtnAdd.Content = T("Přidat", "Add");
        BtnEdit.Content = T("Upravit", "Edit");
        BtnRemove.Content = T("Smazat", "Delete");
        BtnRotate.Content = T("Rotovat", "Rotate");
        BtnCopyPlaceholder.Content = "Placeholder";
        BtnCopyPlaceholder.ToolTip = T("Zkopíruje placeholder vybraného tokenu.", "Copies the selected token placeholder.");
        GrpFileOps.Header = T("Operace nad souborem", "File operations");
        TxtFileLabel.Text = T("Soubor:", "File:");
        BtnBrowseTarget.Content = T("Procházet...", "Browse...");
        BtnRedactPreview.Content = T("Náhled", "Preview");
        ChkRestoreInPlace.ToolTip = T("Když je vypnuto, Restore vytvoří výstupní .restored soubor.", "When off, Restore creates a .restored output file.");
        BtnClose.Content = T("Zavřít", "Close");
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
            AppendOutput(created ? T($"Vault vytvořen a chráněn DPAPI: {VaultPath}", $"Vault created and protected with DPAPI: {VaultPath}") : T($"Vault už existuje: {VaultPath}", $"Vault already exists: {VaultPath}"));
            LoadTokens(forceGitCheck: true);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private bool ConfirmRiskyVaultLocation()
    {
        var safety = TokenService.AnalyzeVaultPath(VaultPath, SettingsService.Current.OpenClawPath);
        if (safety.IsSafe) return true;
        var message = T("Umístění vaultu má bezpečnostní varování:\n\n", "The vault location has security warnings:\n\n") + string.Join("\n", safety.Warnings) + T("\n\nPokračovat i tak?", "\n\nContinue anyway?");
        return MessageBox.Show(message, T("Rizikové umístění vaultu", "Risky vault location"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
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
                AppendOutput(T($"Vault neexistuje. Klikni na Inicializovat: {VaultPath}", $"Vault does not exist. Click Initialize: {VaultPath}"));
                UpdateTokenButtons();
                return;
            }
            var vault = TokenService.LoadVault(VaultPath);
            foreach (var token in vault.Tokens.OrderBy(t => t.Id, StringComparer.Ordinal)) _tokens.Add(token);
            AppendOutput(T($"Načteno tokenů: {_tokens.Count}", $"Loaded tokens: {_tokens.Count}"));
            AppendOutput(TokenService.IsVaultEncryptedAtRest(VaultPath) ? T("Vault je uložen šifrovaně přes Windows DPAPI.", "Vault is stored encrypted with Windows DPAPI.") : T("[VAROVÁNÍ] Vault stále obsahuje plaintext hodnoty. Ulož token pro migraci na DPAPI.", "[WARNING] Vault still contains plaintext values. Save a token to migrate it to DPAPI."));
            if (IsVaultTrackedByGitCached(forceGitCheck)) AppendOutput(T("[VAROVÁNÍ] secrets.json je trackovaný Gitem. Odstraň ho z indexu a přidej do .gitignore.", "[WARNING] secrets.json is tracked by Git. Remove it from the index and add it to .gitignore."));
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
        var safety = TokenService.AnalyzeVaultPath(VaultPath, SettingsService.Current.OpenClawPath);
        VaultWarningBanner.Visibility = safety.IsSafe ? Visibility.Collapsed : Visibility.Visible;
        TxtVaultWarning.Text = safety.IsSafe ? "" : T("Bezpečnostní upozornění: ", "Security warning: ") + string.Join(" ", safety.Warnings);
    }

    private void AddToken()
    {
        var dialog = new TokenEditWindow(T("Přidat token", "Add token")) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try { TokenService.AddToken(VaultPath, dialog.TokenId, dialog.TokenValue, dialog.TokenDescription); AppendOutput(T($"Token přidán: {dialog.TokenId}", $"Token added: {dialog.TokenId}")); LoadTokens(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ImportTokenFromFile()
    {
        var fileDialog = new OpenFileDialog { Title = T("Vyber soubor, ze kterého chceš označit token", "Select a file to import a token from"), Filter = T("Všechny soubory (*.*)|*.*", "All files (*.*)|*.*"), CheckFileExists = true };
        if (fileDialog.ShowDialog() != true) return;
        var dialog = new TokenImportWindow(fileDialog.FileName) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try { TokenService.AddToken(VaultPath, dialog.TokenId, dialog.TokenValue, dialog.TokenDescription); AppendOutput(T($"Token importován: {dialog.TokenId}", $"Token imported: {dialog.TokenId}")); LoadTokens(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void EditSelectedToken()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token) return;
        var dialog = new TokenEditWindow(T("Upravit token", "Edit token"), token) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try { TokenService.UpdateToken(VaultPath, token.Id, dialog.TokenId, dialog.TokenValue, dialog.TokenDescription); AppendOutput(T($"Token upraven: {dialog.TokenId}", $"Token updated: {dialog.TokenId}")); LoadTokens(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void RemoveSelectedToken()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token) return;
        if (MessageBox.Show(T($"Opravdu smazat token '{token.Id}'?", $"Delete token '{token.Id}'?"), T("Smazat token", "Delete token"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { TokenService.RemoveToken(VaultPath, token.Id); AppendOutput(T($"Token smazán: {token.Id}", $"Token deleted: {token.Id}")); LoadTokens(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void RotateSelectedToken()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token) return;
        var dialog = new TokenEditWindow(T("Rotovat token", "Rotate token"), token, idReadOnly: true, rotateOnly: true) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try { TokenService.RotateToken(VaultPath, token.Id, dialog.TokenValue); AppendOutput(T($"Token rotován: {token.Id}", $"Token rotated: {token.Id}")); LoadTokens(); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void CopySelectedPlaceholder()
    {
        if (TokenGrid.SelectedItem is not TokenEntry token) return;
        Clipboard.SetText(token.Placeholder);
        AppendOutput(T($"Placeholder zkopírován: {token.Placeholder}", $"Placeholder copied: {token.Placeholder}"));
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

    private void BrowseTargetFile()
    {
        var dialog = new OpenFileDialog { Title = T("Vyber soubor pro Token Manager", "Select a file for Token Manager"), Filter = T("Všechny soubory (*.*)|*.*", "All files (*.*)|*.*"), CheckFileExists = true };
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
            AppendOutput(T($"NÁHLED: Redact by nahradil {result.TotalCount} výskytů ({result.UniqueCount} unique IDs)", $"PREVIEW: Redact would replace {result.TotalCount} occurrences ({result.UniqueCount} unique IDs)"));
            AppendOutput(result.TokenIds.Count > 0 ? $"ID: {string.Join(", ", result.TokenIds)}" : T("ID: žádné tokeny nenalezeny", "ID: no tokens found"));
            AppendOutput(T($"Výstup by byl: {result.OutputPath}", $"Output would be: {result.OutputPath}"));
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
            var confirm = MessageBox.Show(T($"Výstupní soubor už existuje:\n{outputPath}\n\nPřepsat?", $"Output file already exists:\n{outputPath}\n\nOverwrite?"), T("Přepsat výstup", "Overwrite output"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;
            overwrite = true;
        }
        try { var result = TokenService.RedactFile(VaultPath, inputPath, outputPath, overwrite); AppendOutput($"Redacted {result.TotalCount} tokens ({result.UniqueCount} unique IDs)"); AppendOutput(T($"Výstup: {result.OutputPath}", $"Output: {result.OutputPath}")); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void RestoreSelectedFile()
    {
        if (!EnsureVaultAndTarget()) return;
        var inputPath = TxtTargetFile.Text.Trim();
        var inplace = ChkRestoreInPlace.IsChecked == true;
        var confirmText = inplace ? T("Restore přepíše vybraný soubor a nejdřív vytvoří .bak zálohu.\n\nPokračovat?", "Restore will overwrite the selected file after creating a .bak backup.\n\nContinue?") : T("Restore vytvoří nový .restored soubor a původní soubor nechá beze změny.\n\nPokračovat?", "Restore will create a .restored file and leave the original unchanged.\n\nContinue?");
        if (MessageBox.Show(confirmText, T("Restore souboru", "Restore file"), MessageBoxButton.YesNo, inplace ? MessageBoxImage.Warning : MessageBoxImage.Question) != MessageBoxResult.Yes) return;
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
                    var overwriteConfirm = MessageBox.Show(T($"Výstupní soubor už existuje:\n{outputPath}\n\nPřepsat?", $"Output file already exists:\n{outputPath}\n\nOverwrite?"), T("Přepsat výstup", "Overwrite output"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (overwriteConfirm != MessageBoxResult.Yes) return;
                    overwrite = true;
                }
                result = TokenService.RestoreFile(VaultPath, inputPath, outputPath, overwrite);
            }
            AppendOutput($"Restored {result.TotalCount} placeholders ({result.UniqueCount} unique IDs)");
            AppendOutput(T($"Výstup: {result.OutputPath}", $"Output: {result.OutputPath}"));
            if (result.BackupPath != null) AppendOutput(T($"Záloha: {result.BackupPath}", $"Backup: {result.BackupPath}"));
            if (result.UnknownPlaceholders.Count > 0)
            {
                AppendOutput($"WARN: unknown placeholders skipped: {string.Join(", ", result.UnknownPlaceholders)}");
                MessageBox.Show(T("Některé placeholdery nemají odpovídající token ve vaultu:\n", "Some placeholders do not have a matching token in the vault:\n") + string.Join("\n", result.UnknownPlaceholders), T("Neznámé placeholdery", "Unknown placeholders"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                AppendOutput("OK: file is safe to share");
                SetVerifyStatus(T("Verify: soubor je bezpečný ke sdílení", "Verify: file is safe to share"), Brushes.DarkGreen);
                MessageBox.Show(T("Soubor je bezpečný ke sdílení.", "File is safe to share."), "Verify", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (result.FoundTokenIds.Count > 0)
            {
                AppendOutput(T($"NESDÍLET: soubor obsahuje reálné tokeny: {string.Join(", ", result.FoundTokenIds)}", $"DO NOT SHARE: file contains real tokens: {string.Join(", ", result.FoundTokenIds)}"));
                SetVerifyStatus(T($"Verify: NESDÍLET, nalezeny reálné tokeny ({result.FoundTokenIds.Count})", $"Verify: DO NOT SHARE, real tokens found ({result.FoundTokenIds.Count})"), Brushes.DarkRed);
            }
            if (result.UnknownPlaceholders.Count > 0)
            {
                AppendOutput($"WARN: stale placeholders: {string.Join(", ", result.UnknownPlaceholders)}");
                if (result.FoundTokenIds.Count == 0) SetVerifyStatus(T($"Verify: stale placeholdery ({result.UnknownPlaceholders.Count})", $"Verify: stale placeholders ({result.UnknownPlaceholders.Count})"), Brushes.DarkOrange);
            }
            MessageBox.Show(T("Soubor není bezpečný ke sdílení. Detaily jsou ve výstupu.", "File is not safe to share. Details are in the output."), "Verify", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private bool EnsureVaultAndTarget()
    {
        if (!File.Exists(VaultPath)) { ShowError(T("Vault neexistuje. Nejdřív klikni na Inicializovat.", "Vault does not exist. Click Initialize first.")); return false; }
        if (string.IsNullOrWhiteSpace(TxtTargetFile.Text) || !File.Exists(TxtTargetFile.Text.Trim())) { ShowError(T("Vyber existující soubor.", "Select an existing file.")); return false; }
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
            AppendOutput(result.Added ? T($"Přidáno do .gitignore: {result.Pattern}", $"Added to .gitignore: {result.Pattern}") : T($".gitignore už obsahuje: {result.Pattern}", $".gitignore already contains: {result.Pattern}"));
            AppendOutput($"Repo: {result.RepoPath}");
            _isTrackedByGit = null;
            LoadTokens(forceGitCheck: true);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ShowError(string message)
    {
        AppendOutput(T($"[CHYBA] {message}", $"[ERROR] {message}"));
        MessageBox.Show(message, "Token Manager", MessageBoxButton.OK, MessageBoxImage.Error);
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
