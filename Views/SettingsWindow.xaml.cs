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
        DialogResult = result;
        Close();
    }

    private static SettingsViewModel CreateFallbackViewModel()
    {
        var settingsService = new SettingsService();
        return new SettingsViewModel(settingsService, new GatewayService(settingsService, new ProcessDetector()));
    }
}
