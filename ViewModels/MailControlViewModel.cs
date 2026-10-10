using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public enum MailSection
{
    Overview,
    Settings,
    Rules,
    Proposals,
    Outbox,
    People,
    Health,
    History
}

/// <summary>Položka navigace okna (název se překládá při změně jazyka).</summary>
public sealed partial class MailNavItem : ObservableObject
{
    public MailNavItem(MailSection section, string titleKey, string requiredCommand)
    {
        Section = section;
        TitleKey = titleKey;
        RequiredCommand = requiredCommand;
        Title = L10n.Get(titleKey);
    }

    public MailSection Section { get; }
    public string TitleKey { get; }
    public string RequiredCommand { get; }

    [ObservableProperty] private string _title;
    [ObservableProperty] private string _badge = "";
    [ObservableProperty] private bool _isAvailable = true;
}

/// <summary>
/// Ovládací panel pošty a kalendáře. Žádná pravidla ani limity nevynucuje: všechno dělá
/// backend. ViewModel jen volá příkazy kontraktu a zobrazuje výsledky jako prostý text.
/// </summary>
public sealed partial class MailControlViewModel : ObservableObject
{
    public const int SupportedContractVersion = 1;

    private readonly MailControlClient _client;
    private readonly ISettingsService? _settingsService;
    private IReadOnlySet<string> _commands = new HashSet<string>();
    private CancellationTokenSource? _cts;
    private int _loadGeneration;

    public MailControlViewModel(MailControlClient client, ISettingsService? settingsService = null, bool isSimulated = false)
    {
        _client = client;
        _settingsService = settingsService;
        IsSimulated = isSimulated;

        NavItems = new ObservableCollection<MailNavItem>
        {
            new(MailSection.Overview, "Str_MailControl_Nav_Overview", "state"),
            new(MailSection.Settings, "Str_MailControl_Nav_Settings", "settings"),
            new(MailSection.Rules, "Str_MailControl_Nav_Rules", "rules"),
            new(MailSection.Proposals, "Str_MailControl_Nav_Proposals", "proposals"),
            new(MailSection.Outbox, "Str_MailControl_Nav_Outbox", "outbox"),
            new(MailSection.People, "Str_MailControl_Nav_People", "allowlist"),
            new(MailSection.Health, "Str_MailControl_Nav_Health", "health"),
            new(MailSection.History, "Str_MailControl_Nav_History", "audit")
        };

        RefreshLocalization();
        _cts = new CancellationTokenSource();
    }

    /// <summary>Potvrzení destruktivní akce v aplikaci (smazání souboru, pravidla…). V testech vždy ano.</summary>
    public Func<string, string, bool> ConfirmAction { get; set; } = (_, _) => true;

    /// <summary>Používá se simulátor (FakeMailControlRunner), ne skutečný backend.</summary>
    public bool IsSimulated { get; }

    public ObservableCollection<MailNavItem> NavItems { get; }

    private CancellationToken Token => _cts?.Token ?? CancellationToken.None;

    // ── stavový pruh (stálý) ───────────────────────────────────────────────

    [ObservableProperty] private SendingStatus _sending = SendingStatus.Unknown;
    [ObservableProperty] private string _statusHeadline = "";
    [ObservableProperty] private string _statusDetail = "";
    [ObservableProperty] private bool _statusIsAlert;

    // ── obecný stav okna ───────────────────────────────────────────────────

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _busyText = "";
    [ObservableProperty] private string _lastMessage = "";
    [ObservableProperty] private bool _lastMessageIsError;
    [ObservableProperty] private bool _backendUnavailable;
    [ObservableProperty] private string _connectionText = "";
    [ObservableProperty] private string _contractWarning = "";
    [ObservableProperty] private MailSection _selectedSection = MailSection.Overview;
    [ObservableProperty] private int _selectedNavIndex;

    public bool HasMessage => !string.IsNullOrEmpty(LastMessage);
    public bool HasInfoMessage => HasMessage && !LastMessageIsError;
    public bool HasErrorMessage => HasMessage && LastMessageIsError;

    partial void OnLastMessageChanged(string value) => NotifyMessageFlags();

    partial void OnLastMessageIsErrorChanged(bool value) => NotifyMessageFlags();

    private void NotifyMessageFlags()
    {
        OnPropertyChanged(nameof(HasMessage));
        OnPropertyChanged(nameof(HasInfoMessage));
        OnPropertyChanged(nameof(HasErrorMessage));
    }

    public bool HasContractWarning => !string.IsNullOrEmpty(ContractWarning);

    partial void OnContractWarningChanged(string value) => OnPropertyChanged(nameof(HasContractWarning));

    partial void OnSelectedNavIndexChanged(int value)
    {
        if (value >= 0 && value < NavItems.Count)
            SelectedSection = NavItems[value].Section;
    }

    // ── lokalizované texty rozhraní ────────────────────────────────────────

    [ObservableProperty] private string _windowTitle = "";
    [ObservableProperty] private string _simulatedBanner = "";
    [ObservableProperty] private string _btnRefreshContent = "";
    [ObservableProperty] private string _btnCloseContent = "";
    [ObservableProperty] private string _btnChooseFolderContent = "";

    public void RefreshLocalization()
    {
        WindowTitle = L10n.Get("Str_MailControl_Title");
        SimulatedBanner = IsSimulated ? L10n.Get("Str_MailControl_SimulatedBanner") : "";
        BtnRefreshContent = L10n.Get("Str_MailControl_BtnRefresh");
        BtnCloseContent = L10n.Get("Str_MailControl_BtnClose");
        BtnChooseFolderContent = L10n.Get("Str_MailControl_BtnChooseFolder");
        foreach (var item in NavItems)
            item.Title = L10n.Get(item.TitleKey);
        ApplySendingStatus(Sending);
    }

    // ── zprávy a chyby ─────────────────────────────────────────────────────

    private void ShowInfo(string text)
    {
        LastMessageIsError = false;
        LastMessage = text;
    }

    private void ShowError(MailControlError error, string? contextKey = null)
    {
        var parts = new List<string>();
        if (contextKey is not null)
            parts.Add(L10n.Get(contextKey));

        parts.Add(L10n.Get(MailControlErrors.KeyFor(error.Code)));
        // message a hint vytváří kód backendu (ne text e-mailu), přesto se čistí jako prostý text
        var message = PlainText.Sanitize(error.Message, 400);
        var hint = PlainText.Sanitize(error.Hint, 300);
        if (message.Length > 0 && error.Code is not ("declined" or "needs_confirmation" or "timeout" or "cancelled" or "bad_output"))
            parts.Add(message);
        if (hint.Length > 0)
            parts.Add(hint);

        LastMessageIsError = true;
        LastMessage = string.Join(" ", parts);

        if (error.Code is MailControlError.RunnerUnavailable or MailControlError.StartFailed or "integrity")
        {
            BackendUnavailable = true;
            ConnectionText = error.Code == "integrity"
                ? L10n.Get("Str_MailControl_Err_integrity")
                : L10n.Format("Str_MailControl_ConnectionMissing", MailRootPath);
        }
    }

    public string MailRootPath => _settingsService?.Settings.MailRootPath ?? MailControlPaths.FallbackRoot;

    private async Task<T?> RunAsync<T>(
        Func<CancellationToken, Task<MailControlResult<T>>> call,
        string? busyText = null,
        string? errorContextKey = null)
    {
        var generation = Interlocked.Increment(ref _loadGeneration);
        IsBusy = true;
        BusyText = busyText ?? L10n.Get("Str_MailControl_Busy");
        try
        {
            var result = await call(Token);
            if (!result.Ok)
            {
                ShowError(result.Error!, errorContextKey);
                return default;
            }

            BackendUnavailable = false;
            return result.Value;
        }
        finally
        {
            if (generation == _loadGeneration)
            {
                IsBusy = false;
                BusyText = "";
            }
        }
    }

    // ── stav, schéma, načítání ─────────────────────────────────────────────

    public MailState? State { get; private set; }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadStateAsync();
        await LoadSectionAsync(SelectedSection);
    }

    /// <summary>První načtení okna: schéma (seznam příkazů), stav a obsah přehledu.</summary>
    public async Task InitializeAsync()
    {
        var schema = await RunAsync(ct => _client.GetSchemaAsync(ct));
        if (schema is not null)
        {
            _commands = schema.Commands;
            ApplyCommandAvailability();
            if (schema.ContractVersion != 0 && schema.ContractVersion != SupportedContractVersion)
                ContractWarning = L10n.Format("Str_MailControl_ContractWarn", schema.ContractVersion, SupportedContractVersion);
        }

        await LoadStateAsync();
        await LoadSectionAsync(SelectedSection);
    }

    private void ApplyCommandAvailability()
    {
        foreach (var item in NavItems)
            item.IsAvailable = _commands.Count == 0 || _commands.Contains(item.RequiredCommand);
    }

    public async Task LoadStateAsync()
    {
        var state = await RunAsync(ct => _client.GetStateAsync(ct));
        if (state is null)
        {
            if (State is null)
                ApplySendingStatus(SendingStatus.Unknown);
            return;
        }

        State = state;
        ApplySendingStatus(state.Sending);
        if (state.ContractVersion != 0 && state.ContractVersion != SupportedContractVersion)
            ContractWarning = L10n.Format("Str_MailControl_ContractWarn", state.ContractVersion, SupportedContractVersion);

        SetBadge(MailSection.Proposals, state.Counts.Proposals);
        SetBadge(MailSection.Outbox, state.Counts.Outbox);
        RebuildOverview(state);
    }

    private void SetBadge(MailSection section, int count)
    {
        var item = NavItems.First(n => n.Section == section);
        item.Badge = count > 0 ? count.ToString() : "";
    }

    public async Task LoadSectionAsync(MailSection section)
    {
        switch (section)
        {
            case MailSection.Overview: await LoadOverviewAsync(); break;
            case MailSection.Settings: await LoadSettingsAsync(); break;
            case MailSection.Rules: await LoadRulesAsync(); break;
            case MailSection.Proposals: await LoadProposalsAsync(); break;
            case MailSection.Outbox: await LoadOutboxAsync(); break;
            case MailSection.People: await LoadPeopleAsync(); break;
            case MailSection.Health: await LoadHealthAsync(); break;
            case MailSection.History: await LoadHistoryAsync(); break;
        }
    }

    partial void OnSelectedSectionChanged(MailSection value)
    {
        var index = NavItems.ToList().FindIndex(n => n.Section == value);
        if (index >= 0 && index != SelectedNavIndex)
            SelectedNavIndex = index;

        _ = SafeAsync(() => LoadSectionAsync(value));
    }

    /// <summary>
    /// Spustí asynchronní práci bez čekání (z property changed) tak, aby případná neočekávaná výjimka
    /// neshodila aplikaci: ukáže se obecná chyba bez textu výjimky (mohl by obsahovat cizí text).
    /// </summary>
    private async Task SafeAsync(Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception)
        {
            IsBusy = false;
            BusyText = "";
            ShowError(new MailControlError("internal", "", ""));
        }
    }

    /// <summary>Pro obsluhu událostí okna: neočekávaná výjimka se ukáže jako obecná chyba.</summary>
    public void ReportUnexpectedError() => ShowError(new MailControlError("internal", "", ""));

    // ── stavový pruh ───────────────────────────────────────────────────────

    /// <summary>
    /// Klíče textů stavového pruhu pro daný stav odesílání (čistá funkce, pokrytá testem).
    /// </summary>
    public static (string HeadlineKey, string ScopeKey, bool IsAlert) DescribeSending(SendingStatus s)
    {
        var headline = s.Mode switch
        {
            SendingMode.Live => "Str_MailControl_Status_Live",
            SendingMode.Shadow => "Str_MailControl_Status_Shadow",
            SendingMode.Preview => "Str_MailControl_Status_Preview",
            _ => "Str_MailControl_Status_Unknown"
        };

        var scope = s.Scope.ToLowerInvariant() switch
        {
            "open" => "Str_MailControl_Status_ScopeOpen",
            "allowlist" => "Str_MailControl_Status_ScopeAllowlist",
            _ => ""
        };

        return (headline, scope, s.IsAlert);
    }

    private void ApplySendingStatus(SendingStatus status)
    {
        Sending = status;
        var (headlineKey, scopeKey, alert) = DescribeSending(status);
        StatusHeadline = L10n.Get(headlineKey);
        var detail = new List<string>();
        if (scopeKey.Length > 0)
            detail.Add(L10n.Get(scopeKey));
        if (status.Mode != SendingMode.Unknown)
            detail.Add(L10n.Format("Str_MailControl_Status_Sends24h", status.SendsLast24h));
        StatusDetail = string.Join("  ·  ", detail);
        StatusIsAlert = alert;
    }

    // ── změna složky backendu ──────────────────────────────────────────────

    /// <summary>Uloží novou cestu ke kořenu instalace a vrátí true, když je platná.</summary>
    public bool TrySetMailRoot(string path)
    {
        if (_settingsService is null || !MailControlPaths.IsValidRoot(path))
        {
            ShowError(new MailControlError(MailControlError.RunnerUnavailable, "", ""));
            return false;
        }

        var copy = _settingsService.Settings;
        copy.MailRootPath = path;
        return _settingsService.Save(copy);
    }

    public void Cleanup()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
