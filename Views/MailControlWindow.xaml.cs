using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using OpenClawManager.Models;
using OpenClawManager.Services;
using OpenClawManager.ViewModels;

namespace OpenClawManager.Views;

/// <summary>Opak <see cref="BooleanToVisibilityConverter"/>: true → Collapsed.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Visibility.Collapsed;
}

/// <summary>Logická hodnota jako lokalizované „ano“ / „ne“ (pro buňky tabulek).</summary>
public sealed class BoolToYesNoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? L10n.Get(b ? "Str_MailControl_Yes" : "Str_MailControl_No") : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Okno „Pošta a kalendář“: ovládací panel komponenty automatického zpracování pošty
/// a kalendáře. Všechna pravidla vynucuje backend; okno jen zobrazuje a volá příkazy.
/// </summary>
public partial class MailControlWindow : Window
{
    private readonly MailControlViewModel _viewModel;
    private readonly FrameworkElement[] _sections;

    public MailControlWindow()
        : this(OpenClawManager.App.GetService<ISettingsService>())
    {
    }

    public MailControlWindow(ISettingsService settingsService)
        : this(CreateViewModel(settingsService))
    {
    }

    public MailControlWindow(MailControlViewModel viewModel)
    {
        InitializeComponent();
        ModernPaletteRuntimeStyles.ApplyIfModernPalette(this);

        _viewModel = viewModel;
        DataContext = _viewModel;

        // pořadí odpovídá MailSection
        _sections = new FrameworkElement[]
        {
            SecOverview, SecSettings, SecRules, SecProposals, SecOutbox, SecPeople, SecHealth, SecHistory
        };

        _viewModel.ConfirmAction = (title, text) =>
            MessageBox.Show(this, text, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No)
            == MessageBoxResult.Yes;

        LocalizeColumnHeaders(this);
        BindTableColumns(GridJobs, _viewModel.JobRows);
        BindTableColumns(GridHistory, _viewModel.HistoryRows);
        BindTableColumns(GridSendLog, _viewModel.SendLogRows);
        BindTableColumns(GridAudit, _viewModel.AuditRows);

        CmbAuditSource.ItemsSource = MailControlViewModel.AuditSources;
        CmbAuditSource.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty,
            new Binding(nameof(MailControlViewModel.AuditSource)) { Mode = BindingMode.TwoWay });

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ShowSection(_viewModel.SelectedSection);

        BtnChooseFolder.Click += async (_, _) => await GuardAsync(ChooseFolderAsync);
        BtnClose.Click += (_, _) => Close();
        Closed += (_, _) => _viewModel.Cleanup();
        Loaded += async (_, _) => await GuardAsync(_viewModel.InitializeAsync);
    }

    /// <summary>
    /// Zvolí implementaci kontraktu: simulátor (proměnná prostředí OPENCLAW_MAILCONTROL_FAKE),
    /// jinak proces <c>control_run.py</c> ve složce <see cref="AppSettings.MailRootPath"/>.
    /// </summary>
    public static MailControlViewModel CreateViewModel(ISettingsService settingsService)
    {
        if (FakeMailControlRunner.TryCreateFromEnvironment(out var fake) && fake is not null)
            return new MailControlViewModel(new MailControlClient(fake), settingsService, isSimulated: true);

        var runner = new ProcessMailControlRunner(() => settingsService.Settings.MailRootPath);
        return new MailControlViewModel(new MailControlClient(runner), settingsService);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MailControlViewModel.SelectedSection))
            ShowSection(_viewModel.SelectedSection);
    }

    private void ShowSection(MailSection section)
    {
        for (var i = 0; i < _sections.Length; i++)
            _sections[i].Visibility = i == (int)section ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Obsluha události typu async void nesmí shodit aplikaci.</summary>
    private async Task GuardAsync(Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception)
        {
            _viewModel.ReportUnexpectedError();
        }
    }

    private async Task ChooseFolderAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = L10n.Get("Str_MailControl_ChooseFolderTitle"),
            InitialDirectory = _viewModel.MailRootPath
        };
        if (dialog.ShowDialog(this) != true)
            return;

        if (!_viewModel.TrySetMailRoot(dialog.FolderName))
            return;

        await _viewModel.InitializeAsync();
    }

    // ── tabulky ────────────────────────────────────────────────────────────

    /// <summary>
    /// Záhlaví sloupců zapsaná v XAML jako klíč „Str_…“ se nahradí přeloženým textem
    /// (DataGridColumn není součástí vizuálního stromu, takže DynamicResource nefunguje).
    /// </summary>
    private static void LocalizeColumnHeaders(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DataGrid grid)
            {
                foreach (var column in grid.Columns)
                {
                    if (column.Header is string key && key.StartsWith("Str_", StringComparison.Ordinal))
                        column.Header = L10n.Get(key);
                }
            }

            if (child is DependencyObject d)
                LocalizeColumnHeaders(d);
        }
    }

    private static readonly string[] PreferredColumnOrder =
    {
        "ts", "at", "time", "id", "key", "command", "kind", "source", "to", "from", "subject", "result",
        "ok", "old", "new", "by", "via"
    };

    /// <summary>
    /// Obecné tabulky (historie, odeslané, audit, úlohy) nemají pevné sloupce: odvodí se z klíčů
    /// řádků, takže nová pole backendu se zobrazí bez změny aplikace. Hodnoty jsou prostý text.
    /// </summary>
    private static void BindTableColumns(DataGrid grid, ObservableCollection<TableRow> rows)
    {
        var current = new List<string>();

        void Rebuild()
        {
            var keys = rows.SelectMany(r => r.Cells.Keys).Distinct().ToList();
            keys.Sort((a, b) =>
            {
                var ia = Array.IndexOf(PreferredColumnOrder, a);
                var ib = Array.IndexOf(PreferredColumnOrder, b);
                if (ia < 0) ia = int.MaxValue;
                if (ib < 0) ib = int.MaxValue;
                return ia != ib ? ia.CompareTo(ib) : string.CompareOrdinal(a, b);
            });

            if (keys.SequenceEqual(current))
                return;

            current = keys;
            grid.Columns.Clear();
            foreach (var key in keys)
            {
                grid.Columns.Add(new DataGridTextColumn
                {
                    Header = HeaderFor(key),
                    Binding = new Binding($"[{key}]") { Mode = BindingMode.OneWay },
                    MaxWidth = 420
                });
            }
        }

        rows.CollectionChanged += (_, _) => Rebuild();
        Rebuild();
    }

    private static string HeaderFor(string key)
    {
        var resourceKey = "Str_MailControl_Col_" + key;
        return Application.Current?.TryFindResource(resourceKey) as string ?? key;
    }
}
