using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class MailControlViewModel
{
    public ObservableCollection<ProposalInfo> Proposals { get; } = new();

    [ObservableProperty] private ProposalInfo? _selectedProposal;

    /// <summary>Text konceptu odpovědi upravovaný v UI (pro reply_draft).</summary>
    [ObservableProperty] private string _reviseText = "";

    /// <summary>Druhý, výslovný krok u platby s nálezem: uživatel potvrdil, že rozumí nálezu.</summary>
    [ObservableProperty] private bool _overrideAcknowledged;

    public bool HasSelectedProposal => SelectedProposal is not null;
    public bool SelectedNeedsOverride => SelectedProposal?.NeedsExplicitOverride == true;
    public bool SelectedIsReplyDraft => SelectedProposal?.IsReplyDraft == true;
    public bool SelectedHasFlags => SelectedProposal is { Flags.Count: > 0 };
    public bool SelectedHasBody => SelectedProposal is { Body.Length: > 0 };

    /// <summary>Souhrn nálezu u platby pro zobrazení červeně (kódy nálezu + text napsaný kódem).</summary>
    public string SelectedFindingText =>
        SelectedProposal is null
            ? ""
            : string.Join(Environment.NewLine, new[] { SelectedProposal.Question }
                .Concat(SelectedProposal.Flags.Select(f => "• " + f))
                .Where(s => s.Length > 0));

    partial void OnSelectedProposalChanged(ProposalInfo? value)
    {
        ReviseText = value?.Draft ?? "";
        OverrideAcknowledged = false;
        OnPropertyChanged(nameof(HasSelectedProposal));
        OnPropertyChanged(nameof(SelectedNeedsOverride));
        OnPropertyChanged(nameof(SelectedIsReplyDraft));
        OnPropertyChanged(nameof(SelectedHasFlags));
        OnPropertyChanged(nameof(SelectedHasBody));
        OnPropertyChanged(nameof(SelectedFindingText));
    }

    private async Task LoadProposalsAsync()
    {
        var selectedId = SelectedProposal?.Id;
        var result = await RunAsync(ct => _client.GetProposalsAsync(ct));
        if (result is null)
            return;

        Proposals.Clear();
        foreach (var p in result)
            Proposals.Add(p);

        SelectedProposal = selectedId is null ? null : Proposals.FirstOrDefault(p => p.Id == selectedId);
        SetBadge(MailSection.Proposals, Proposals.Count);
    }

    /// <summary>
    /// Rozhodnutí o návrhu. U platby s nálezem se „ano“ posílá jen s override=true a jen po
    /// druhém výslovném kroku (<see cref="OverrideAcknowledged"/>).
    /// </summary>
    [RelayCommand]
    public async Task DecideProposalAsync(string? choice)
    {
        var proposal = SelectedProposal;
        if (proposal is null || choice is not ("yes" or "no"))
            return;

        var yes = choice == "yes";
        var useOverride = false;
        if (yes && proposal.NeedsExplicitOverride)
        {
            if (!OverrideAcknowledged)
            {
                ShowError(new MailControlError("refused", L10n.Get("Str_MailControl_Prop_NeedAck"), ""));
                return;
            }

            useOverride = true;
        }

        var busy = yes && (proposal.Kind is "scheduled_send" || useOverride)
            ? L10n.Get("Str_MailControl_WaitingConfirm")
            : null;

        var outcome = await RunAsync(ct => _client.DecideProposalAsync(proposal.Id, yes, useOverride, ct), busy);
        if (outcome is null)
            return;

        ShowInfo(L10n.Get(yes ? "Str_MailControl_Prop_DecidedYes" : "Str_MailControl_Prop_DecidedNo"));
        await LoadProposalsAsync();
        await LoadStateAsync();
    }

    [RelayCommand]
    public async Task ReviseProposalAsync()
    {
        var proposal = SelectedProposal;
        if (proposal is null || !proposal.IsReplyDraft)
            return;

        var outcome = await RunAsync(ct => _client.ReviseProposalAsync(proposal.Id, ReviseText, ct));
        if (outcome is null)
            return;

        ShowInfo(L10n.Get("Str_MailControl_Prop_Revised"));
        var id = proposal.Id;
        await LoadProposalsAsync();
        SelectedProposal = Proposals.FirstOrDefault(p => p.Id == id);
    }
}
