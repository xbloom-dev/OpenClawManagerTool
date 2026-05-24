using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using OpenClawManager.Services;
using OpenClawManager.ViewModels;

namespace OpenClawManager.Views;

/// <summary>
/// Non-modal live Gateway log window. State and file watching live in the ViewModel;
/// code-behind keeps only WPF-specific clipboard, scrolling, and feedback animation.
/// </summary>
public partial class LiveLogWindow : Window
{
    private readonly LiveLogViewModel _viewModel;
    private DispatcherTimer? _highlightClearTimer;
    private DispatcherTimer? _feedbackTimer;

    public LiveLogWindow()
        : this(new LiveLogViewModel("", 20))
    {
    }

    public LiveLogWindow(string logPath, int windowSize = 20)
        : this(new LiveLogViewModel(logPath, windowSize))
    {
    }

    public LiveLogWindow(LiveLogViewModel viewModel)
    {
        InitializeComponent();
        ModernPaletteRuntimeStyles.ApplyIfModernPalette(this);

        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.LogChanged += OnViewModelLogChanged;
        _viewModel.NewLinesAppended += OnViewModelNewLinesAppended;
        _viewModel.HighlightClearRequested += OnViewModelHighlightClearRequested;

        BtnClose.Click += (_, _) => Close();
        BtnCopy.Click += BtnCopy_Click;

        Closed += (_, _) =>
        {
            _viewModel.LogChanged -= OnViewModelLogChanged;
            _viewModel.NewLinesAppended -= OnViewModelNewLinesAppended;
            _viewModel.HighlightClearRequested -= OnViewModelHighlightClearRequested;
            _viewModel.Dispose();
            _highlightClearTimer?.Stop();
            _feedbackTimer?.Stop();
        };
    }

    private void OnViewModelLogChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(_viewModel.ReadNewLines);
    }

    private void OnViewModelNewLinesAppended(object? sender, EventArgs e)
    {
        TxtLogContent.ScrollToEnd();
    }

    private void OnViewModelHighlightClearRequested(object? sender, EventArgs e)
    {
        _highlightClearTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _highlightClearTimer.Stop();
        _highlightClearTimer.Tick -= HighlightClearTimer_Tick;
        _highlightClearTimer.Tick += HighlightClearTimer_Tick;
        _highlightClearTimer.Start();
    }

    private void HighlightClearTimer_Tick(object? sender, EventArgs e)
    {
        _highlightClearTimer?.Stop();
        _viewModel.ClearHighlight();
    }

    private void BtnCopy_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_viewModel.CleanContent);
            ShowCopiedFeedback();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"{_viewModel.CopyFailedMessagePrefix}\n{ex.Message}",
                _viewModel.CopyFailedTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowCopiedFeedback()
    {
        _feedbackTimer?.Stop();
        TxtCopiedFeedback.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            });

        _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1800) };
        _feedbackTimer.Tick += (_, _) =>
        {
            _feedbackTimer?.Stop();
            TxtCopiedFeedback.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(200)
                });
        };
        _feedbackTimer.Start();
    }
}
