using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

/// <summary>Kategorie pro výběr v pravidle (popisek dodává backend).</summary>
public sealed record CategoryOption(string Key, string Label)
{
    public string Display => Key.Length == 0 ? Label : $"{Label} ({Key})";
}

public sealed partial class MailControlViewModel
{
    public ObservableCollection<SenderRule> Rules { get; } = new();
    public ObservableCollection<CategoryOption> CategoryOptions { get; } = new();
    public ObservableCollection<string> RouteOptions { get; } = new();
    public ObservableCollection<string> AutonomyOptions { get; } = new();

    [ObservableProperty] private SenderRule? _selectedRule;
    [ObservableProperty] private string _rulesSummary = "";
    [ObservableProperty] private string _newRuleSender = "";
    [ObservableProperty] private string _newRuleDomain = "";
    [ObservableProperty] private CategoryOption? _newRuleCategory;
    [ObservableProperty] private string? _newRuleRoute;
    [ObservableProperty] private string? _newRuleAutonomy;
    [ObservableProperty] private string _newRuleNote = "";

    /// <summary>Autonomie free vyžaduje potvrzení člověkem v dialogu backendu: upozornit předem.</summary>
    public bool NewRuleNeedsConfirmation => string.Equals(NewRuleAutonomy, "free", StringComparison.Ordinal);

    partial void OnNewRuleAutonomyChanged(string? value) => OnPropertyChanged(nameof(NewRuleNeedsConfirmation));

    private async Task LoadRulesAsync()
    {
        var result = await RunAsync(ct => _client.GetRulesAsync(ct));
        if (result is null)
            return;

        Rules.Clear();
        foreach (var rule in result.Rules)
            Rules.Add(rule);

        CategoryOptions.Clear();
        CategoryOptions.Add(new CategoryOption("", L10n.Get("Str_MailControl_Rules_AllCategories")));
        foreach (var (key, label) in result.Categories)
            CategoryOptions.Add(new CategoryOption(key, label));
        NewRuleCategory ??= CategoryOptions[0];

        RouteOptions.Clear();
        foreach (var r in result.Routes)
            RouteOptions.Add(r);
        NewRuleRoute ??= RouteOptions.FirstOrDefault();

        AutonomyOptions.Clear();
        foreach (var a in result.Autonomies)
            AutonomyOptions.Add(a);
        NewRuleAutonomy ??= AutonomyOptions.FirstOrDefault();

        RulesSummary = L10n.Format("Str_MailControl_Rules_Summary",
            result.Rules.Count,
            result.MaxRules,
            L10n.Get(result.Shadow ? "Str_MailControl_On" : "Str_MailControl_Off"));
    }

    [RelayCommand]
    public async Task AddRuleAsync()
    {
        var route = NewRuleRoute ?? "auto";
        var autonomy = NewRuleAutonomy ?? "none";
        var busy = autonomy == "free" ? L10n.Get("Str_MailControl_WaitingConfirm") : null;

        var rule = await RunAsync(ct => _client.SetRuleAsync(
            NewRuleSender, NewRuleDomain, NewRuleCategory?.Key, route, autonomy, NewRuleNote, ct), busy);
        if (rule is null)
            return;

        ShowInfo(L10n.Get("Str_MailControl_Rules_Added"));
        NewRuleSender = "";
        NewRuleDomain = "";
        NewRuleNote = "";
        await LoadRulesAsync();
        await LoadStateAsync();
    }

    [RelayCommand]
    public async Task RemoveRuleAsync(SenderRule? rule)
    {
        rule ??= SelectedRule;
        if (rule is null)
            return;

        if (!ConfirmAction(L10n.Get("Str_MailControl_Rules_RemoveTitle"),
                L10n.Format("Str_MailControl_Rules_RemoveConfirm", rule.Target.Length > 0 ? rule.Target : rule.Id)))
            return;

        var removed = await RunAsync(ct => _client.RemoveRuleAsync(rule.Id, ct));
        if (removed is null)
            return;

        ShowInfo(L10n.Get("Str_MailControl_Rules_Removed"));
        await LoadRulesAsync();
        await LoadStateAsync();
    }
}
