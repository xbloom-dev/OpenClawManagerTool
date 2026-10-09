using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class MailControlViewModel
{
    public static IReadOnlyList<string> AuditSources { get; } = new[] { "control", "mail", "gcal" };

    public ObservableCollection<TableRow> HistoryRows { get; } = new();
    public ObservableCollection<TableRow> SendLogRows { get; } = new();
    public ObservableCollection<TableRow> AuditRows { get; } = new();

    [ObservableProperty] private TableRow? _selectedHistoryRow;
    [ObservableProperty] private string _auditSource = "control";

    /// <summary>Id záznamu, který vyžaduje druhý krok „vrátit i tak“ (hodnota se mezitím změnila).</summary>
    [ObservableProperty] private string? _pendingForceRevertId;

    public bool HasPendingForceRevert => !string.IsNullOrEmpty(PendingForceRevertId);

    partial void OnPendingForceRevertIdChanged(string? value) => OnPropertyChanged(nameof(HasPendingForceRevert));

    partial void OnAuditSourceChanged(string value) => _ = SafeAsync(LoadAuditAsync);

    private async Task LoadHistoryAsync()
    {
        var history = await RunAsync(ct => _client.GetHistoryAsync(30, ct));
        if (history is not null)
        {
            HistoryRows.Clear();
            foreach (var r in history)
                HistoryRows.Add(r);
        }

        var sendlog = await RunAsync(ct => _client.GetSendLogAsync(30, ct));
        if (sendlog is not null)
        {
            SendLogRows.Clear();
            foreach (var r in sendlog)
                SendLogRows.Add(r);
        }

        await LoadAuditAsync();
    }

    private async Task LoadAuditAsync()
    {
        var audit = await RunAsync(ct => _client.GetAuditAsync(AuditSource, 50, ct));
        if (audit is null)
            return;

        AuditRows.Clear();
        foreach (var r in audit)
            AuditRows.Add(r);
    }

    /// <summary>
    /// Vrátí nastavení na hodnotu před vybraným záznamem historie (stejná pravidla jako set:
    /// potvrzení rizikové změny dělá backend). Při konfliktu nabídne druhý krok „vrátit i tak“.
    /// </summary>
    [RelayCommand]
    public async Task RevertHistoryAsync(string? mode = null)
    {
        var force = mode == "force";
        var id = force ? PendingForceRevertId : SelectedHistoryRow?["id"];
        if (string.IsNullOrEmpty(id))
            return;

        PendingForceRevertId = null;
        var generation = _loadGeneration;
        IsBusy = true;
        BusyText = L10n.Get("Str_MailControl_WaitingConfirm");
        MailControlResult<SetSettingResult> result;
        try
        {
            result = await _client.RevertAsync(id, force, Token);
        }
        finally
        {
            if (generation == _loadGeneration)
            {
                IsBusy = false;
                BusyText = "";
            }
        }

        if (!result.Ok)
        {
            if (result.Error!.Code == "conflict")
                PendingForceRevertId = id;

            ShowError(result.Error!);
            return;
        }

        ShowInfo(L10n.Get("Str_MailControl_History_Reverted"));
        await LoadHistoryAsync();
        await LoadStateAsync();
    }
}
