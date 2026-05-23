using System.Windows;
using OpenClawManager.Services;
using OpenClawManager.ViewModels;

namespace OpenClawManager.Views;

/// <summary>
/// Modal application settings dialog.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
        : this(CreateFallbackViewModel())
    {
    }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        viewModel.CloseAction = CloseWithDialogResult;

        ModernPaletteRuntimeStyles.ApplyIfModernPalette(this);
    }

    private void CloseWithDialogResult(bool result)
    {
 codex/2.0-settings-window-mvvm
        DialogResult = result;

        ReadFromUi();

        if (!App.GetService<IGatewayService>().TryValidateOpenClawCommand(_settings.OpenClawCommand, out var commandError))
        {
            MessageBox.Show(
                T("OpenClaw příkaz není bezpečný nebo platný:\n", "OpenClaw command is not safe or valid:\n") + commandError,
                T("Neplatny prikaz", "Invalid command"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var ok = OpenClawManager.App.GetService<ISettingsService>().Save(_settings);
        if (!ok)
        {
            MessageBox.Show(
                T("Uložení nastavení selhalo.\nZkontroluj, zda máš oprávnění zapisovat do ", "Saving settings failed.\nCheck write permissions for ") + OpenClawManager.App.GetService<ISettingsService>().SettingsFilePath,
                T("Chyba", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        DialogResult = true;
 develop
        Close();
    }

    private static SettingsViewModel CreateFallbackViewModel()
    {
        var settingsService = new SettingsService();
        return new SettingsViewModel(settingsService, new GatewayService(settingsService));
    }
}
