using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class MailControlViewModel
{
    public ObservableCollection<IntegrityItem> IntegrityItems { get; } = new();
    public ObservableCollection<PluginItem> PluginItems { get; } = new();
    public ObservableCollection<TableRow> JobRows { get; } = new();
    public ObservableCollection<ConfigFileInfo> ConfigFiles { get; } = new();
    public ObservableCollection<string> Problems { get; } = new();

    [ObservableProperty] private string _ollamaText = "";
    [ObservableProperty] private string _healthSummary = "";
    [ObservableProperty] private bool _healthHasProblems;
    [ObservableProperty] private string _healthCheckedAt = "";

    private async Task LoadHealthAsync(bool refresh = false)
    {
        var health = await RunAsync(ct => _client.GetHealthAsync(refresh, ct), L10n.Get("Str_MailControl_Health_Busy"));
        if (health is not null)
        {
            IntegrityItems.Clear();
            foreach (var i in health.Integrity)
                IntegrityItems.Add(i);

            PluginItems.Clear();
            foreach (var p in health.Plugins)
                PluginItems.Add(p);

            JobRows.Clear();
            foreach (var j in health.Jobs)
                JobRows.Add(j);

            Problems.Clear();
            foreach (var p in health.Problems)
                Problems.Add(PlainText.Sanitize(p, 200));

            OllamaText = health.OllamaOk
                ? L10n.Format("Str_MailControl_Health_OllamaOk", health.OllamaModels,
                    health.OllamaLoaded.Count == 0 ? "–" : string.Join(", ", health.OllamaLoaded))
                : L10n.Get("Str_MailControl_Health_OllamaDown");

            HealthHasProblems = health.Problems.Count > 0 || health.PluginsError.Length > 0 || health.JobsError.Length > 0;
            HealthSummary = HealthHasProblems
                ? L10n.Format("Str_MailControl_Health_Problems", health.Problems.Count)
                : L10n.Get("Str_MailControl_Health_AllOk");
            HealthCheckedAt = health.Checked;
        }

        var files = await RunAsync(ct => _client.GetConfigFilesAsync(ct));
        if (files is not null)
        {
            ConfigFiles.Clear();
            foreach (var f in files)
                ConfigFiles.Add(f);
        }
    }

    [RelayCommand]
    public Task RefreshHealthAsync() => LoadHealthAsync(refresh: true);
}
