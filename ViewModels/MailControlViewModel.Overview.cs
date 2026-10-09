using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class MailControlViewModel
{
    [ObservableProperty] private string _overviewMailbox = "";
    [ObservableProperty] private string _overviewCounts = "";
    [ObservableProperty] private string _overviewPrivacy = "";
    [ObservableProperty] private string _overviewLocked = "";
    [ObservableProperty] private string _overviewStats = "";

    public ObservableCollection<TableRow> RecentMails { get; } = new();

    private void RebuildOverview(MailState state)
    {
        // heslo aplikace backend nevrací; zobrazuje se jen příznak, že je nastavené
        var mailbox = state.Mailbox;
        OverviewMailbox = string.Join(Environment.NewLine, new[]
        {
            L10n.Format("Str_MailControl_Ov_Address", PlainText.Sanitize(mailbox.Address, 120)),
            L10n.Format("Str_MailControl_Ov_Backend", PlainText.Sanitize(mailbox.Backend, 40)),
            L10n.Format("Str_MailControl_Ov_Password",
                L10n.Get(mailbox.PasswordSet ? "Str_MailControl_Yes" : "Str_MailControl_No")),
            L10n.Format("Str_MailControl_Ov_GmailToken",
                L10n.Get(mailbox.GmailToken ? "Str_MailControl_Yes" : "Str_MailControl_No"))
        });

        var c = state.Counts;
        OverviewCounts = L10n.Format("Str_MailControl_Ov_Counts",
            c.Proposals, c.Outbox, c.Rules, c.Allowlist, c.TrustedSenders);

        OverviewPrivacy = L10n.Get(state.ChatAgentRawMail
            ? "Str_MailControl_Ov_RawMailOn"
            : "Str_MailControl_Ov_RawMailOff");

        var locked = state.LockedFiles.Where(kv => kv.Value).Select(kv => kv.Key).OrderBy(x => x).ToList();
        OverviewLocked = locked.Count == 0
            ? L10n.Get("Str_MailControl_Ov_NoLocked")
            : L10n.Format("Str_MailControl_Ov_Locked", string.Join(", ", locked));
    }

    private async Task LoadOverviewAsync()
    {
        var triage = await RunAsync(ct => _client.GetTriageAsync(10, ct));
        if (triage is not null)
        {
            RecentMails.Clear();
            foreach (var row in triage)
                RecentMails.Add(row);
        }

        var stats = await RunAsync(ct => _client.GetStatsAsync(30, ct));
        if (stats is not null)
        {
            var byCategory = string.Join(", ", stats.ByCategory.Select(kv => $"{kv.Key}: {kv.Value}"));
            OverviewStats = L10n.Format("Str_MailControl_Ov_Stats", stats.Days, stats.Mails, stats.NeedsAttention, byCategory);
        }
    }
}
