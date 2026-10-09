using System.Text.Json;

namespace OpenClawManager.Models;

// DTO a parsování kontraktu ovládací vrstvy pošty a kalendáře (E:\OpenClaw\control, viz
// Dokumentace\Kalendar-a-posta\API-ovladaci-aplikace.md). Parsování je záměrně tolerantní:
// neznámá pole se ignorují, chybějící dostanou výchozí hodnotu, nic nevyhazuje výjimku.

/// <summary>Chyba z backendu (obálka {"ok":false,"error":{...}}) nebo lokální chyba spuštění.</summary>
public sealed record MailControlError(string Code, string Message, string Hint)
{
    /// <summary>Kódy, které nevrací backend, ale vzniknou při spuštění procesu v aplikaci.</summary>
    public const string RunnerUnavailable = "runner_unavailable";
    public const string StartFailed = "start_failed";
    public const string Timeout = "timeout";
    public const string Cancelled = "cancelled";
    public const string BadOutput = "bad_output";
}

/// <summary>Obálka odpovědi: jeden JSON objekt na stdout.</summary>
public sealed class MailControlResponse
{
    public bool Ok { get; private init; }
    public JsonElement Result { get; private init; }
    public MailControlError? Error { get; private init; }

    public static MailControlResponse Success(JsonElement result) => new() { Ok = true, Result = result };

    public static MailControlResponse Failure(string code, string message = "", string hint = "") =>
        new() { Ok = false, Error = new MailControlError(code, message, hint) };

    public static MailControlResponse Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Failure(MailControlError.BadOutput);

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("ok", out var ok))
                return Failure(MailControlError.BadOutput);

            if (ok.ValueKind == JsonValueKind.True)
            {
                var result = root.TryGetProperty("result", out var r) ? r.Clone() : default;
                return Success(result);
            }

            if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.Object)
            {
                return Failure(
                    err.Str("code", "internal"),
                    err.Str("message"),
                    err.Str("hint"));
            }

            return Failure("internal");
        }
        catch (JsonException)
        {
            return Failure(MailControlError.BadOutput);
        }
    }
}

/// <summary>Výsledek typovaného volání: buď hodnota, nebo chyba.</summary>
public sealed record MailControlResult<T>(bool Ok, T? Value, MailControlError? Error)
{
    public static MailControlResult<T> Success(T value) => new(true, value, null);

    public static MailControlResult<T> Failure(MailControlError error) => new(false, default, error);
}

/// <summary>Pomocné čtení z JsonElement bez výjimek.</summary>
public static class MailControlJson
{
    public static string Str(this JsonElement e, string name, string fallback = "")
    {
        if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v))
            return fallback;

        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString() ?? fallback,
            JsonValueKind.Number => v.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => fallback
        };
    }

    public static bool Bool(this JsonElement e, string name, bool fallback = false) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => fallback
            }
            : fallback;

    public static long? Long(this JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object
        && e.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.Number
        && v.TryGetInt64(out var l)
            ? l
            : null;

    public static JsonElement Obj(this JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? v : default;

    public static IEnumerable<JsonElement> Items(this JsonElement e, string name)
    {
        if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Array)
            return Enumerable.Empty<JsonElement>();
        return v.EnumerateArray();
    }

    public static IReadOnlyList<string> Strings(this JsonElement e, string name) =>
        e.Items(name)
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString() ?? "")
            .ToList();

    /// <summary>Hodnota nastavení jako .NET objekt: bool, long, double, string nebo null.</summary>
    public static object? ToValue(this JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.TryGetInt64(out var l) ? (object)l : e.GetDouble(),
        _ => null
    };

    public static object? Value(this JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? v.ToValue() : null;

    public static bool Has(this JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out _);

    /// <summary>
    /// Zploští libovolnou hodnotu na jeden řádek prostého textu (nedůvěryhodný obsah: žádný
    /// markup, řídicí znaky se mění na mezery, délka je omezená).
    /// </summary>
    public static string ToPlainCell(this JsonElement e, int maxLength = 300)
    {
        var text = e.ValueKind switch
        {
            JsonValueKind.String => e.GetString() ?? "",
            JsonValueKind.Null or JsonValueKind.Undefined => "",
            JsonValueKind.Array => string.Join(", ", e.EnumerateArray().Select(x => x.ToPlainCell(maxLength))),
            _ => e.GetRawText()
        };
        return PlainText.Sanitize(text, maxLength);
    }
}

/// <summary>Úprava textu pocházejícího z e-mailů nebo modelu pro zobrazení v buňce či štítku.</summary>
public static class PlainText
{
    public static string Sanitize(string? text, int maxLength = 300, bool keepNewLines = false)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        var chars = new char[Math.Min(text.Length, maxLength)];
        var n = 0;
        foreach (var c in text)
        {
            if (n >= chars.Length)
                break;

            if (keepNewLines && c == '\n')
                chars[n++] = c;
            else if (char.IsControl(c))
                chars[n++] = ' ';
            else
                chars[n++] = c;
        }

        var result = new string(chars, 0, n);
        return text.Length > maxLength ? result + "…" : result;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Nastavení
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Položka registru nastavení (schema i settings); hodnota je vyplněná jen z příkazu settings.</summary>
public sealed record SettingDefinition(
    string Key,
    string Group,
    string Type,
    string Label,
    string Description,
    string Tier,
    object? Default,
    object? SafeValue,
    bool HasSafeValue,
    double? Min,
    double? Max,
    string Unit,
    IReadOnlyList<string> EnumValues,
    string Pattern,
    object? Value,
    bool IsDefault,
    string Source,
    bool Locked,
    bool Writable)
{
    public bool IsConfirmTier => string.Equals(Tier, "confirm", StringComparison.OrdinalIgnoreCase);

    public static SettingDefinition FromJson(JsonElement e)
    {
        double? Num(string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
                ? v.GetDouble()
                : null;

        var hasValue = e.Has("value");
        return new SettingDefinition(
            Key: e.Str("key"),
            Group: e.Str("group"),
            Type: e.Str("type", "text").ToLowerInvariant(),
            Label: e.Str("label", e.Str("key")),
            Description: e.Str("description"),
            Tier: e.Str("tier", "free").ToLowerInvariant(),
            Default: e.Value("default"),
            SafeValue: e.Value("safe_value"),
            HasSafeValue: e.Has("safe_value"),
            Min: Num("min"),
            Max: Num("max"),
            Unit: e.Str("unit"),
            EnumValues: e.Strings("enum"),
            Pattern: e.Str("pattern"),
            Value: hasValue ? e.Value("value") : e.Value("default"),
            IsDefault: e.Bool("is_default", !hasValue),
            Source: e.Str("source"),
            Locked: e.Bool("locked"),
            Writable: !e.Has("writable") || e.Bool("writable", true));
    }

    public SettingDefinition WithValue(object? value, bool isDefault = false) =>
        this with { Value = value, IsDefault = isDefault };
}

public sealed record MailSettingsResult(IReadOnlyList<SettingDefinition> Settings, IReadOnlyList<string> Groups);

public sealed record MailSchema(
    int ContractVersion,
    IReadOnlyList<SettingDefinition> Settings,
    IReadOnlyList<string> Groups,
    IReadOnlySet<string> Commands);

public sealed record SetSettingResult(
    string Key,
    object? Old,
    object? New,
    bool Changed,
    string HistoryId,
    bool ConfirmedByHuman);

// ─────────────────────────────────────────────────────────────────────────────
// Stav odesílání (stálý pruh)
// ─────────────────────────────────────────────────────────────────────────────

public enum SendingMode
{
    Unknown,
    /// <summary>Stínový režim: odpovědi se jen připravují jako soubory.</summary>
    Shadow,
    /// <summary>Odesílání vypnuté, stínový režim vypnutý: jen soubory .eml.</summary>
    Preview,
    /// <summary>Skutečné odesílání zapnuté.</summary>
    Live
}

public sealed record SendingStatus(
    SendingMode Mode,
    bool SendEnabled,
    bool Shadow,
    string Scope,
    int SendsLast24h)
{
    public static SendingStatus Unknown { get; } = new(SendingMode.Unknown, false, true, "", 0);

    /// <summary>Výrazné zobrazení: skutečné odesílání, nebo agent smí odpovídat komukoli.</summary>
    public bool IsAlert => Mode == SendingMode.Live || OpenScope;

    public bool OpenScope => string.Equals(Scope, "open", StringComparison.OrdinalIgnoreCase);

    public static SendingStatus FromState(JsonElement state)
    {
        var sending = state.Obj("sending");
        if (sending.ValueKind != JsonValueKind.Object)
            return Unknown;

        var send = sending.Bool("send_enabled");
        var shadow = sending.Bool("shadow", true);
        var mode = sending.Str("mode").ToLowerInvariant() switch
        {
            "live" => SendingMode.Live,
            "shadow" => SendingMode.Shadow,
            "preview" => SendingMode.Preview,
            // neznámá hodnota: spočítat z přepínačů, a to opatrně (neznámé = ne "bezpečné")
            _ => send && !shadow ? SendingMode.Live : shadow ? SendingMode.Shadow : SendingMode.Preview
        };

        return new SendingStatus(mode, send, shadow, sending.Str("scope"), (int)(sending.Long("sends_last_24h") ?? 0));
    }
}

public sealed record MailboxInfo(string Address, string Backend, bool PasswordSet, bool GmailToken);

public sealed record MailCounts(int Rules, int Proposals, int Outbox, int Allowlist, int TrustedSenders);

public sealed record MailState(
    int ContractVersion,
    SendingStatus Sending,
    bool ChatAgentRawMail,
    MailboxInfo Mailbox,
    MailCounts Counts,
    IReadOnlyDictionary<string, bool> LockedFiles,
    bool TokenExists,
    string Time)
{
    public static MailState FromJson(JsonElement e)
    {
        var mailbox = e.Obj("mailbox");
        var counts = e.Obj("counts");
        var locked = new Dictionary<string, bool>();
        var lockedObj = e.Obj("locked");
        if (lockedObj.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in lockedObj.EnumerateObject())
            {
                if (p.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    locked[p.Name] = p.Value.GetBoolean();
                else if (p.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var inner in p.Value.EnumerateObject())
                    {
                        if (inner.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            locked[inner.Name] = inner.Value.GetBoolean();
                    }
                }
            }
        }

        return new MailState(
            ContractVersion: (int)(e.Long("contract_version") ?? 0),
            Sending: SendingStatus.FromState(e),
            ChatAgentRawMail: e.Obj("privacy").Bool("chat_agent_raw_mail", true),
            // heslo aplikace backend nevrací; zobrazuje se jen příznak password_set
            Mailbox: new MailboxInfo(
                mailbox.Str("address"),
                mailbox.Str("backend"),
                mailbox.Bool("password_set"),
                mailbox.Bool("gmail_token")),
            Counts: new MailCounts(
                (int)(counts.Long("rules") ?? 0),
                (int)(counts.Long("proposals") ?? 0),
                (int)(counts.Long("outbox") ?? 0),
                (int)(counts.Long("allowlist") ?? 0),
                (int)(counts.Long("trusted_senders") ?? 0)),
            LockedFiles: locked,
            TokenExists: e.Bool("token_exists"),
            Time: e.Str("time"));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Pravidla, lidé, šablony
// ─────────────────────────────────────────────────────────────────────────────

public sealed record SenderRule(string Id, string Sender, string Domain, string Category, string Route, string Autonomy, string Note, string By)
{
    public string Target => !string.IsNullOrEmpty(Sender) ? Sender : Domain;

    public static SenderRule FromJson(JsonElement e) => new(
        e.Str("id"),
        e.Str("sender"),
        e.Str("domain"),
        e.Str("category"),
        e.Str("route"),
        e.Str("autonomy"),
        PlainText.Sanitize(e.Str("note"), 200),
        e.Str("by"));
}

public sealed record RulesResult(
    bool Shadow,
    IReadOnlyList<SenderRule> Rules,
    int MaxRules,
    IReadOnlyDictionary<string, string> Categories,
    IReadOnlyList<string> Routes,
    IReadOnlyList<string> Autonomies);

public sealed record Recipient(string Alias, string Address);

public sealed record AllowlistResult(IReadOnlyList<Recipient> Recipients, bool Locked);

public sealed record TrustedSendersResult(IReadOnlyList<string> Trusted, bool Locked);

public sealed record ReplyTemplate(string Id, string Body);

public sealed record TemplatesResult(IReadOnlyList<ReplyTemplate> Templates, IReadOnlyList<string> Fields, bool Locked);

// ─────────────────────────────────────────────────────────────────────────────
// Návrhy a odchozí
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Návrh čekající na rozhodnutí. Všechny texty jsou nedůvěryhodné: zobrazit jako prostý text.</summary>
public sealed record ProposalInfo(
    string Id,
    string Kind,
    string Created,
    string Sender,
    string Subject,
    string Question,
    bool NeedsExplicitOverride,
    string To,
    string Draft,
    string ReplySubject,
    string Body,
    string SendAfter,
    IReadOnlyList<string> Flags,
    string Details)
{
    public bool IsReplyDraft => Kind == "reply_draft";

    public static ProposalInfo FromJson(JsonElement e)
    {
        var question = e.Obj("question");
        var questionText = question.ValueKind == JsonValueKind.Array
            ? string.Join("\n", question.EnumerateArray().Select(x => x.ToPlainCell(500)))
            : PlainText.Sanitize(e.Str("question"), 1500, keepNewLines: true);

        var payment = e.Obj("payment");
        var flags = payment.Strings("flags");

        return new ProposalInfo(
            Id: e.Str("id"),
            Kind: e.Str("kind"),
            Created: e.Str("created"),
            Sender: PlainText.Sanitize(e.Str("sender"), 200),
            Subject: PlainText.Sanitize(e.Str("subject"), 300),
            Question: questionText,
            NeedsExplicitOverride: e.Bool("needs_explicit_override"),
            To: PlainText.Sanitize(e.Str("to"), 200),
            Draft: PlainText.Sanitize(e.Str("draft"), 3000, keepNewLines: true),
            ReplySubject: PlainText.Sanitize(e.Str("reply_subject"), 300),
            Body: PlainText.Sanitize(e.Str("body"), 3000, keepNewLines: true),
            SendAfter: e.Str("send_after"),
            Flags: flags,
            Details: DescribeDetails(e));
    }

    private static string DescribeDetails(JsonElement e)
    {
        var payment = e.Obj("payment");
        if (payment.ValueKind == JsonValueKind.Object)
        {
            return string.Join("; ", payment.EnumerateObject()
                .Where(p => p.Name != "flags")
                .Select(p => $"{p.Name}: {p.Value.ToPlainCell(120)}"));
        }

        var events = e.Items("events").ToList();
        if (events.Count > 0)
            return string.Join("\n", events.Select(x => x.ToPlainCell(200)));

        return "";
    }
}

public sealed record ProposalDecision(string Id, string Choice, string Summary);

public sealed record OutboxFile(string Name, long Size, string Modified, string Kind, bool Deletable)
{
    public static OutboxFile FromJson(JsonElement e) => new(
        e.Str("name"),
        e.Long("size") ?? 0,
        e.Str("modified"),
        e.Str("kind"),
        e.Bool("deletable"));
}

public sealed record OutboxPreview(
    string Name,
    IReadOnlyDictionary<string, string> Headers,
    string Text,
    IReadOnlyList<string> Attachments);

// ─────────────────────────────────────────────────────────────────────────────
// Provoz
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Obecný řádek tabulky (sendlog, audit, historie, úlohy): sloupce se zjistí z klíčů.</summary>
public sealed class TableRow
{
    public TableRow(IReadOnlyDictionary<string, string> cells) => Cells = cells;

    public IReadOnlyDictionary<string, string> Cells { get; }

    public string this[string key] => Cells.TryGetValue(key, out var v) ? v : "";

    public static TableRow FromJson(JsonElement e)
    {
        var cells = new Dictionary<string, string>();
        if (e.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in e.EnumerateObject())
                cells[p.Name] = p.Value.ToPlainCell();
        }
        else
        {
            cells["value"] = e.ToPlainCell();
        }

        return new TableRow(cells);
    }
}

public sealed record IntegrityItem(string File, bool Pinned, bool Ok, string Sha256);

public sealed record PluginItem(string Id, bool Ok, string Status);

public sealed record HealthResult(
    IReadOnlyList<IntegrityItem> Integrity,
    bool OllamaOk,
    int OllamaModels,
    IReadOnlyList<string> OllamaLoaded,
    IReadOnlyList<PluginItem> Plugins,
    IReadOnlyList<TableRow> Jobs,
    IReadOnlyList<string> Problems,
    string Checked,
    string PluginsError,
    string JobsError);

public sealed record ConfigFileInfo(string Name, bool Exists, bool Locked, bool EditableByApp, string Purpose);

public sealed record StatsResult(int Days, int Mails, IReadOnlyList<KeyValuePair<string, string>> ByCategory, int NeedsAttention);
