using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class MailControlViewModel
{
    public ObservableCollection<Recipient> Recipients { get; } = new();
    public ObservableCollection<string> TrustedSenders { get; } = new();
    public ObservableCollection<ReplyTemplate> Templates { get; } = new();

    [ObservableProperty] private Recipient? _selectedRecipient;
    [ObservableProperty] private string? _selectedTrustedSender;
    [ObservableProperty] private ReplyTemplate? _selectedTemplate;
    [ObservableProperty] private string _newRecipientAddress = "";
    [ObservableProperty] private string _newRecipientAlias = "";
    [ObservableProperty] private string _newTrustedSender = "";
    [ObservableProperty] private string _templateId = "";
    [ObservableProperty] private string _templateBody = "";
    [ObservableProperty] private string _templateFieldsHint = "";
    [ObservableProperty] private bool _peopleLocked;

    partial void OnSelectedTemplateChanged(ReplyTemplate? value)
    {
        if (value is null)
            return;

        TemplateId = value.Id;
        TemplateBody = value.Body;
    }

    private async Task LoadPeopleAsync()
    {
        var allowlist = await RunAsync(ct => _client.GetAllowlistAsync(ct));
        if (allowlist is not null)
        {
            Recipients.Clear();
            foreach (var r in allowlist.Recipients)
                Recipients.Add(r);
            PeopleLocked = allowlist.Locked;
        }

        var senders = await RunAsync(ct => _client.GetTrustedSendersAsync(ct));
        if (senders is not null)
        {
            TrustedSenders.Clear();
            foreach (var s in senders.Trusted)
                TrustedSenders.Add(s);
            PeopleLocked |= senders.Locked;
        }

        var templates = await RunAsync(ct => _client.GetTemplatesAsync(ct));
        if (templates is not null)
        {
            var selectedId = SelectedTemplate?.Id;
            Templates.Clear();
            foreach (var t in templates.Templates)
                Templates.Add(t);
            SelectedTemplate = selectedId is null ? null : Templates.FirstOrDefault(t => t.Id == selectedId);
            TemplateFieldsHint = L10n.Format("Str_MailControl_Tpl_Fields",
                string.Join(" ", templates.Fields.Select(f => "{" + f + "}")));
            PeopleLocked |= templates.Locked;
        }
    }

    // Přidání příjemce nebo důvěryhodného odesílatele rozšiřuje, co agent smí: backend
    // vždy žádá potvrzení nativním dialogem, aplikace jen čeká a ukáže výsledek.
    [RelayCommand]
    public async Task AddRecipientAsync()
    {
        var changed = await RunAsync(
            ct => _client.AddRecipientAsync(NewRecipientAddress, NewRecipientAlias, ct),
            L10n.Get("Str_MailControl_WaitingConfirm"));
        if (!changed && LastMessageIsError)
            return;

        ShowInfo(L10n.Get(changed ? "Str_MailControl_People_RecipientAdded" : "Str_MailControl_People_NoChange"));
        NewRecipientAddress = "";
        NewRecipientAlias = "";
        await LoadPeopleAsync();
        await LoadStateAsync();
    }

    [RelayCommand]
    public async Task RemoveRecipientAsync()
    {
        var recipient = SelectedRecipient;
        if (recipient is null)
            return;

        if (!ConfirmAction(L10n.Get("Str_MailControl_People_RemoveTitle"),
                L10n.Format("Str_MailControl_People_RemoveConfirm", recipient.Address)))
            return;

        if (!await RunAsync(ct => _client.RemoveRecipientAsync(recipient.Alias.Length > 0 ? recipient.Alias : recipient.Address, ct)))
            return;

        ShowInfo(L10n.Get("Str_MailControl_People_Removed"));
        await LoadPeopleAsync();
        await LoadStateAsync();
    }

    [RelayCommand]
    public async Task AddTrustedSenderAsync()
    {
        var changed = await RunAsync(
            ct => _client.AddTrustedSenderAsync(NewTrustedSender, ct),
            L10n.Get("Str_MailControl_WaitingConfirm"));
        if (!changed && LastMessageIsError)
            return;

        ShowInfo(L10n.Get(changed ? "Str_MailControl_People_SenderAdded" : "Str_MailControl_People_NoChange"));
        NewTrustedSender = "";
        await LoadPeopleAsync();
        await LoadStateAsync();
    }

    [RelayCommand]
    public async Task RemoveTrustedSenderAsync()
    {
        var sender = SelectedTrustedSender;
        if (string.IsNullOrEmpty(sender))
            return;

        if (!ConfirmAction(L10n.Get("Str_MailControl_People_RemoveTitle"),
                L10n.Format("Str_MailControl_People_RemoveConfirm", sender)))
            return;

        if (!await RunAsync(ct => _client.RemoveTrustedSenderAsync(sender, ct)))
            return;

        ShowInfo(L10n.Get("Str_MailControl_People_Removed"));
        await LoadPeopleAsync();
        await LoadStateAsync();
    }

    [RelayCommand]
    public async Task SaveTemplateAsync()
    {
        if (!await RunAsync(ct => _client.SetTemplateAsync(TemplateId.Trim(), TemplateBody, ct)))
            return;

        ShowInfo(L10n.Get("Str_MailControl_Tpl_Saved"));
        await LoadPeopleAsync();
    }

    [RelayCommand]
    public async Task RemoveTemplateAsync()
    {
        var template = SelectedTemplate;
        if (template is null)
            return;

        if (!ConfirmAction(L10n.Get("Str_MailControl_Tpl_RemoveTitle"),
                L10n.Format("Str_MailControl_Tpl_RemoveConfirm", template.Id)))
            return;

        if (!await RunAsync(ct => _client.RemoveTemplateAsync(template.Id, ct)))
            return;

        ShowInfo(L10n.Get("Str_MailControl_Tpl_Removed"));
        SelectedTemplate = null;
        TemplateId = "";
        TemplateBody = "";
        await LoadPeopleAsync();
    }
}
