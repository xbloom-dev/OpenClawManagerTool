using System.Text.Json;
using OpenClawManager.Models;

namespace OpenClawManager.Services;

/// <summary>
/// Typovaná fasáda nad <see cref="IMailControlRunner"/>. Žádná pravidla ani limity zde
/// nejsou: všechno vynucuje backend, tohle jen volá příkazy a mapuje odpovědi na DTO.
/// </summary>
public sealed class MailControlClient
{
    public static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Zápis může čekat na nativní potvrzovací dialog (backend ho po 2 minutách ukončí).</summary>
    public static readonly TimeSpan WriteTimeout = TimeSpan.FromSeconds(150);

    /// <summary>health a jobs volají OpenClaw CLI (několik sekund až desítek sekund).</summary>
    public static readonly TimeSpan SlowTimeout = TimeSpan.FromSeconds(100);

    private readonly IMailControlRunner _runner;

    public MailControlClient(IMailControlRunner runner)
    {
        _runner = runner;
    }

    public IMailControlRunner Runner => _runner;

    // ── pomocné ────────────────────────────────────────────────────────────

    private async Task<MailControlResult<T>> CallAsync<T>(
        string command,
        IReadOnlyDictionary<string, object?>? args,
        TimeSpan timeout,
        Func<JsonElement, T> map,
        CancellationToken ct)
    {
        var response = await _runner.RunAsync(command, args, timeout, ct).ConfigureAwait(false);
        if (!response.Ok)
            return MailControlResult<T>.Failure(response.Error ?? new MailControlError("internal", "", ""));

        try
        {
            return MailControlResult<T>.Success(map(response.Result));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return MailControlResult<T>.Failure(new MailControlError(MailControlError.BadOutput, "", ""));
        }
    }

    private static Dictionary<string, object?> Args(params (string Key, object? Value)[] pairs)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var (key, value) in pairs)
        {
            if (value is not null)
                dict[key] = value;
        }

        return dict;
    }

    private static List<T> MapList<T>(JsonElement e, string property, Func<JsonElement, T> map) =>
        e.Items(property).Select(map).ToList();

    // ── schéma, stav, nastavení ────────────────────────────────────────────

    public Task<MailControlResult<MailSchema>> GetSchemaAsync(CancellationToken ct = default) =>
        CallAsync("schema", null, ReadTimeout, e =>
        {
            var commands = new HashSet<string>(StringComparer.Ordinal);
            var cmdObj = e.Obj("commands");
            if (cmdObj.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in cmdObj.EnumerateObject())
                    commands.Add(p.Name);
            }

            return new MailSchema(
                (int)(e.Long("contract_version") ?? 0),
                MapList(e, "settings", SettingDefinition.FromJson),
                e.Strings("groups"),
                commands);
        }, ct);

    public Task<MailControlResult<MailState>> GetStateAsync(CancellationToken ct = default) =>
        CallAsync("state", null, ReadTimeout, MailState.FromJson, ct);

    public Task<MailControlResult<MailSettingsResult>> GetSettingsAsync(CancellationToken ct = default) =>
        CallAsync("settings", null, ReadTimeout,
            e => new MailSettingsResult(MapList(e, "settings", SettingDefinition.FromJson), e.Strings("groups")), ct);

    public Task<MailControlResult<SetSettingResult>> SetSettingAsync(string key, object value, CancellationToken ct = default) =>
        CallAsync("set", new Dictionary<string, object?> { ["key"] = key, ["value"] = value }, WriteTimeout,
            ParseSetResult, ct);

    public Task<MailControlResult<SetSettingResult>> RevertAsync(string historyId, bool force = false, CancellationToken ct = default) =>
        CallAsync("revert", Args(("id", historyId), ("force", force ? true : null)), WriteTimeout, ParseSetResult, ct);

    private static SetSettingResult ParseSetResult(JsonElement e) => new(
        e.Str("key"),
        e.Value("old"),
        e.Value("new"),
        e.Bool("changed"),
        e.Str("history_id"),
        e.Bool("confirmed_by_human"));

    public Task<MailControlResult<IReadOnlyList<TableRow>>> GetHistoryAsync(int n = 30, CancellationToken ct = default) =>
        CallAsync<IReadOnlyList<TableRow>>("history", Args(("n", n)), ReadTimeout,
            e => MapList(e, "entries", TableRow.FromJson), ct);

    public Task<MailControlResult<IReadOnlyList<ConfigFileInfo>>> GetConfigFilesAsync(CancellationToken ct = default) =>
        CallAsync<IReadOnlyList<ConfigFileInfo>>("config_files", null, ReadTimeout,
            e => MapList(e, "files", f => new ConfigFileInfo(
                f.Str("name"), f.Bool("exists"), f.Bool("locked"), f.Bool("editable_by_app"), PlainText.Sanitize(f.Str("purpose"), 300))), ct);

    // ── pravidla odesílatelů ───────────────────────────────────────────────

    public Task<MailControlResult<RulesResult>> GetRulesAsync(CancellationToken ct = default) =>
        CallAsync("rules", null, ReadTimeout, e =>
        {
            var categories = new Dictionary<string, string>();
            var cat = e.Obj("categories");
            if (cat.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in cat.EnumerateObject())
                    categories[p.Name] = p.Value.ToPlainCell(120);
            }

            return new RulesResult(
                e.Bool("shadow", true),
                MapList(e, "rules", SenderRule.FromJson),
                (int)(e.Long("max_rules") ?? 0),
                categories,
                e.Strings("routes"),
                e.Strings("autonomy"));
        }, ct);

    public Task<MailControlResult<SenderRule?>> SetRuleAsync(
        string? sender, string? domain, string? category, string route, string autonomy, string? note,
        CancellationToken ct = default) =>
        CallAsync<SenderRule?>("rule_set",
            Args(("sender", Blank(sender)), ("domain", Blank(domain)), ("category", Blank(category)),
                ("route", route), ("autonomy", autonomy), ("note", Blank(note))),
            WriteTimeout,
            e => e.Obj("rule").ValueKind == JsonValueKind.Object ? SenderRule.FromJson(e.Obj("rule")) : null, ct);

    public Task<MailControlResult<string>> RemoveRuleAsync(string id, CancellationToken ct = default) =>
        CallAsync("rule_remove", Args(("id", id)), ReadTimeout, e => e.Str("removed"), ct);

    // ── příjemci, důvěryhodní odesílatelé, šablony ────────────────────────

    public Task<MailControlResult<AllowlistResult>> GetAllowlistAsync(CancellationToken ct = default) =>
        CallAsync("allowlist", null, ReadTimeout,
            e => new AllowlistResult(
                MapList(e, "recipients", r => new Recipient(r.Str("alias"), r.Str("address"))),
                e.Bool("locked")), ct);

    public Task<MailControlResult<bool>> AddRecipientAsync(string address, string? alias, CancellationToken ct = default) =>
        CallAsync("allowlist_add", Args(("address", address), ("alias", Blank(alias))), WriteTimeout,
            e => e.Bool("changed", true), ct);

    public Task<MailControlResult<bool>> RemoveRecipientAsync(string aliasOrAddress, CancellationToken ct = default) =>
        CallAsync("allowlist_remove", Args(("alias", aliasOrAddress)), ReadTimeout, _ => true, ct);

    public Task<MailControlResult<TrustedSendersResult>> GetTrustedSendersAsync(CancellationToken ct = default) =>
        CallAsync("senders", null, ReadTimeout,
            e => new TrustedSendersResult(e.Strings("trusted"), e.Bool("locked")), ct);

    public Task<MailControlResult<bool>> AddTrustedSenderAsync(string address, CancellationToken ct = default) =>
        CallAsync("sender_add", Args(("address", address)), WriteTimeout, e => e.Bool("changed", true), ct);

    public Task<MailControlResult<bool>> RemoveTrustedSenderAsync(string address, CancellationToken ct = default) =>
        CallAsync("sender_remove", Args(("address", address)), ReadTimeout, _ => true, ct);

    public Task<MailControlResult<TemplatesResult>> GetTemplatesAsync(CancellationToken ct = default) =>
        CallAsync("templates", null, ReadTimeout,
            e => new TemplatesResult(
                MapList(e, "templates", t => new ReplyTemplate(t.Str("id"), t.Str("body"))),
                e.Strings("fields"),
                e.Bool("locked")), ct);

    public Task<MailControlResult<bool>> SetTemplateAsync(string id, string body, CancellationToken ct = default) =>
        CallAsync("template_set", Args(("id", id), ("body", body)), ReadTimeout, _ => true, ct);

    public Task<MailControlResult<bool>> RemoveTemplateAsync(string id, CancellationToken ct = default) =>
        CallAsync("template_remove", Args(("id", id)), ReadTimeout, _ => true, ct);

    // ── návrhy ─────────────────────────────────────────────────────────────

    public Task<MailControlResult<IReadOnlyList<ProposalInfo>>> GetProposalsAsync(CancellationToken ct = default) =>
        CallAsync<IReadOnlyList<ProposalInfo>>("proposals", null, ReadTimeout,
            e => MapList(e, "proposals", ProposalInfo.FromJson), ct);

    public Task<MailControlResult<string>> DecideProposalAsync(
        string id, bool yes, bool overrideFinding = false, CancellationToken ct = default) =>
        CallAsync("proposal_decide",
            Args(("id", id), ("choice", yes ? "yes" : "no"), ("override", overrideFinding ? true : null)),
            WriteTimeout,
            e => DescribeResult(e), ct);

    public Task<MailControlResult<string>> ReviseProposalAsync(string id, string text, CancellationToken ct = default) =>
        CallAsync("proposal_revise", Args(("id", id), ("text", text)), ReadTimeout, e => DescribeResult(e), ct);

    private static string DescribeResult(JsonElement e)
    {
        foreach (var name in new[] { "message", "summary", "result", "status", "outcome" })
        {
            var s = e.Str(name);
            if (!string.IsNullOrEmpty(s))
                return PlainText.Sanitize(s, 300);
        }

        return "";
    }

    // ── odchozí ────────────────────────────────────────────────────────────

    public Task<MailControlResult<IReadOnlyList<OutboxFile>>> GetOutboxAsync(CancellationToken ct = default) =>
        CallAsync<IReadOnlyList<OutboxFile>>("outbox", null, ReadTimeout,
            e => MapList(e, "files", OutboxFile.FromJson), ct);

    public Task<MailControlResult<OutboxPreview>> PreviewOutboxAsync(string name, CancellationToken ct = default) =>
        CallAsync("outbox_preview", Args(("name", name)), ReadTimeout, e =>
        {
            var headers = new Dictionary<string, string>();
            var h = e.Obj("headers");
            if (h.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in h.EnumerateObject())
                    headers[p.Name] = PlainText.Sanitize(p.Value.ToPlainCell(400), 400);
            }

            return new OutboxPreview(
                e.Str("name", name),
                headers,
                PlainText.Sanitize(e.Str("text"), 6000, keepNewLines: true),
                e.Items("attachments").Select(x => x.ToPlainCell(120)).ToList());
        }, ct);

    public Task<MailControlResult<bool>> DeleteOutboxAsync(string name, CancellationToken ct = default) =>
        CallAsync("outbox_delete", Args(("name", name)), ReadTimeout, _ => true, ct);

    // ── provoz ─────────────────────────────────────────────────────────────

    public Task<MailControlResult<IReadOnlyList<TableRow>>> GetSendLogAsync(int n = 30, CancellationToken ct = default) =>
        CallAsync<IReadOnlyList<TableRow>>("sendlog", Args(("n", n)), ReadTimeout,
            e => MapList(e, "entries", TableRow.FromJson), ct);

    public Task<MailControlResult<IReadOnlyList<TableRow>>> GetTriageAsync(int n = 10, CancellationToken ct = default) =>
        CallAsync<IReadOnlyList<TableRow>>("triage", Args(("n", n)), ReadTimeout,
            e => MapList(e, "mails", TableRow.FromJson), ct);

    public Task<MailControlResult<StatsResult>> GetStatsAsync(int days = 30, CancellationToken ct = default) =>
        CallAsync("stats", Args(("days", days)), ReadTimeout, e =>
        {
            var byCategory = new List<KeyValuePair<string, string>>();
            var cat = e.Obj("by_category");
            if (cat.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in cat.EnumerateObject())
                    byCategory.Add(new KeyValuePair<string, string>(p.Name, p.Value.ToPlainCell(40)));
            }

            return new StatsResult(
                (int)(e.Long("days") ?? days),
                (int)(e.Long("mails") ?? 0),
                byCategory,
                (int)(e.Long("needs_attention") ?? 0),
                e.Long("paused_runs") is { } paused ? (int)paused : null);
        }, ct);

    public Task<MailControlResult<IReadOnlyList<TableRow>>> GetAuditAsync(string source, int n = 50, CancellationToken ct = default) =>
        CallAsync<IReadOnlyList<TableRow>>("audit", Args(("source", source), ("n", n)), ReadTimeout,
            e => MapList(e, "entries", TableRow.FromJson), ct);

    public Task<MailControlResult<HealthResult>> GetHealthAsync(bool refresh = false, CancellationToken ct = default) =>
        CallAsync("health", Args(("refresh", refresh ? true : null)), SlowTimeout, e =>
        {
            var ollama = e.Obj("ollama");
            var plugins = e.Obj("plugins");
            var jobs = e.Obj("jobs");
            return new HealthResult(
                MapList(e, "integrity", i => new IntegrityItem(i.Str("file"), i.Bool("pinned"), i.Bool("ok"), i.Str("sha256"))),
                ollama.Bool("ok"),
                (int)(ollama.Long("models") ?? 0),
                ollama.Items("loaded").Select(m => m.Str("name")).Where(n => n.Length > 0).ToList(),
                plugins.ValueKind == JsonValueKind.Array
                    ? plugins.EnumerateArray().Select(p => new PluginItem(p.Str("id"), p.Bool("ok"), p.Str("status"))).ToList()
                    : new List<PluginItem>(),
                jobs.ValueKind == JsonValueKind.Array
                    ? jobs.EnumerateArray().Select(TableRow.FromJson).ToList()
                    : new List<TableRow>(),
                e.Strings("problems"),
                e.Str("checked"),
                plugins.ValueKind == JsonValueKind.Object ? plugins.Obj("error").Str("code", "error") : "",
                jobs.ValueKind == JsonValueKind.Object ? jobs.Obj("error").Str("code", "error") : "");
        }, ct);

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
