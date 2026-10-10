using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class MailControlViewModel
{
    public ObservableCollection<OutboxFile> OutboxFiles { get; } = new();
    public ObservableCollection<KeyValuePair<string, string>> PreviewHeaders { get; } = new();

    [ObservableProperty] private OutboxFile? _selectedOutboxFile;
    [ObservableProperty] private string _previewText = "";
    [ObservableProperty] private string _previewAttachments = "";

    public bool CanDeleteSelectedOutbox => SelectedOutboxFile?.Deletable == true;

    partial void OnSelectedOutboxFileChanged(OutboxFile? value)
    {
        OnPropertyChanged(nameof(CanDeleteSelectedOutbox));
        PreviewHeaders.Clear();
        PreviewText = "";
        PreviewAttachments = "";
        if (value is not null)
            _ = SafeAsync(() => ShowOutboxPreviewAsync(value.Name));
    }

    private async Task LoadOutboxAsync()
    {
        var selected = SelectedOutboxFile?.Name;
        var result = await RunAsync(ct => _client.GetOutboxAsync(ct));
        if (result is null)
            return;

        OutboxFiles.Clear();
        foreach (var f in result)
            OutboxFiles.Add(f);

        SelectedOutboxFile = selected is null ? null : OutboxFiles.FirstOrDefault(f => f.Name == selected);
        SetBadge(MailSection.Outbox, OutboxFiles.Count);
    }

    private async Task ShowOutboxPreviewAsync(string name)
    {
        var preview = await RunAsync(ct => _client.PreviewOutboxAsync(name, ct));
        if (preview is null || SelectedOutboxFile?.Name != name)
            return;

        PreviewHeaders.Clear();
        foreach (var kv in preview.Headers)
            PreviewHeaders.Add(kv);
        PreviewText = preview.Text;
        PreviewAttachments = preview.Attachments.Count == 0 ? "" : string.Join(", ", preview.Attachments);
    }

    [RelayCommand]
    public async Task DeleteOutboxAsync()
    {
        var file = SelectedOutboxFile;
        if (file is null || !file.Deletable)
            return;

        if (!ConfirmAction(L10n.Get("Str_MailControl_Outbox_DeleteTitle"),
                L10n.Format("Str_MailControl_Outbox_DeleteConfirm", file.Name)))
            return;

        var deleted = await RunAsync(ct => _client.DeleteOutboxAsync(file.Name, ct));
        if (!deleted)
            return;

        ShowInfo(L10n.Get("Str_MailControl_Outbox_Deleted"));
        SelectedOutboxFile = null;
        await LoadOutboxAsync();
        await LoadStateAsync();
    }
}
