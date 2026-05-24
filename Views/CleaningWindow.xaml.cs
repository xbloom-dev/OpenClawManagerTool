using System.Windows;
using OpenClawManager.Services;
using OpenClawManager.ViewModels;

namespace OpenClawManager.Views;

/// <summary>
/// Modal Cleaning Tool dialog. UI-only responsibilities stay here: log scrolling,
/// window closing, and embedded TUI cleanup delegation to MainWindow.
/// </summary>
public partial class CleaningWindow : Window
{
    private readonly CleaningViewModel _viewModel;

    public CleaningWindow()
        : this(CreateFallbackViewModel())
    {
    }

    public CleaningWindow(CleaningViewModel viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();
        DataContext = viewModel;

        viewModel.LogMessageAppended += OnLogMessageAppended;
        viewModel.CloseAction = Close;
        viewModel.StopEmbeddedTuiAction = StopEmbeddedTui;

        ModernPaletteRuntimeStyles.ApplyIfModernPalette(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.LogMessageAppended -= OnLogMessageAppended;
        _viewModel.CloseAction = null;
        _viewModel.StopEmbeddedTuiAction = null;
        base.OnClosed(e);
    }

    private void OnLogMessageAppended(object? sender, string message)
    {
        Dispatcher.Invoke(() =>
        {
            if (message == "__CLEAR__")
            {
                TxtLog.Clear();
                return;
            }

            TxtLog.AppendText(message + Environment.NewLine);
            TxtLog.ScrollToEnd();
        });
    }

    private void StopEmbeddedTui()
    {
        if (Owner is MainWindow main)
            main.Terminal.StopTui();
    }

    private static CleaningViewModel CreateFallbackViewModel()
    {
        var settingsService = new SettingsService();
        var processDetector = new ProcessDetector();
        return new CleaningViewModel(
            new GatewayService(settingsService, processDetector),
            settingsService,
            new CleanupServiceAdapter(),
            processDetector);
    }
}
