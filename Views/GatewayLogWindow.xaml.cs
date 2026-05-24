using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using OpenClawManager.ViewModels;

namespace OpenClawManager.Views;

public partial class GatewayLogWindow : Window
{
    private readonly GatewayLogViewModel _vm;
    private DispatcherTimer? _feedbackTimer;

    public GatewayLogWindow()
        : this(new GatewayLogViewModel("", 20))
    {
    }

    public GatewayLogWindow(string logPath, int defaultLines = 20)
        : this(new GatewayLogViewModel(logPath, defaultLines))
    {
    }

    public GatewayLogWindow(GatewayLogViewModel viewModel)
    {
        InitializeComponent();
        ModernPaletteRuntimeStyles.ApplyIfModernPalette(this);

        _vm = viewModel;
        DataContext = _vm;

        SelectLineCount(_vm.DefaultLines);

        BtnRefresh.Click += (_, _) => RefreshLog();
        BtnClose.Click += (_, _) => Close();
        BtnCopy.Click += BtnCopy_Click;
        BtnLiveLog.Click += BtnLiveLog_Click;
        CmbLineCount.SelectionChanged += (_, _) => RefreshLog();

        RefreshLog();
    }

    private void RefreshLog()
    {
        _vm.LoadLog(GetSelectedLineCount());
        Dispatcher.BeginInvoke(() => TxtLogContent.ScrollToEnd(), DispatcherPriority.Background);
    }

    private void SelectLineCount(int lines)
    {
        foreach (ComboBoxItem item in CmbLineCount.Items)
        {
            if (item.Tag is string tag && tag == lines.ToString())
            {
                CmbLineCount.SelectedItem = item;
                return;
            }
        }

        if (CmbLineCount.Items.Count > 1)
            CmbLineCount.SelectedIndex = 1;
    }

    private int GetSelectedLineCount()
    {
        if (CmbLineCount.SelectedItem is ComboBoxItem item &&
            item.Tag is string tag &&
            int.TryParse(tag, out var n))
        {
            return n;
        }

        return 20;
    }

    private void BtnCopy_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_vm.LogContent);
            ShowCopiedFeedback();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"{_vm.CopyFailedMessagePrefix}\n{ex.Message}",
                _vm.CopyFailedTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowCopiedFeedback()
    {
        _feedbackTimer?.Stop();
        TxtCopiedFeedback.BeginAnimation(OpacityProperty,
            new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(200) });
        _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1800) };
        _feedbackTimer.Tick += (_, _) =>
        {
            _feedbackTimer?.Stop();
            TxtCopiedFeedback.BeginAnimation(OpacityProperty,
                new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromMilliseconds(200) });
        };
        _feedbackTimer.Start();
    }

    private void BtnLiveLog_Click(object? sender, RoutedEventArgs e)
    {
        var live = new LiveLogWindow(_vm.LogPath, GetSelectedLineCount());
        live.Show();
        Close();
    }
}
