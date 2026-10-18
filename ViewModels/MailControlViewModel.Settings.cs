using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class MailControlViewModel
{
    public ObservableCollection<SettingGroupViewModel> SettingGroups { get; } = new();

    private readonly Dictionary<string, SettingFieldViewModel> _fieldsByKey = new();

    private async Task LoadSettingsAsync()
    {
        var result = await RunAsync(ct => _client.GetSettingsAsync(ct));
        if (result is null)
            return;

        ApplySettings(result);
    }

    /// <summary>
    /// Zobrazí nastavení. Má-li se rozložení formuláře (skupiny a klíče) oproti minulému
    /// načtení zachovat, pole se jen aktualizují; jinak se formulář postaví znovu ze schématu.
    /// </summary>
    public void ApplySettings(MailSettingsResult result)
    {
        var groups = MailControlForm.BuildGroups(result.Settings, result.Groups);
        var byKey = result.Settings.ToDictionary(s => s.Key);
        var sameLayout = groups.Count == SettingGroups.Count
            && groups.Zip(SettingGroups, (g, vm) => g.Group == vm.Name
                && g.Fields.Select(f => f.Key).SequenceEqual(vm.Fields.Select(f => f.Key))).All(x => x);

        if (sameLayout)
        {
            foreach (var field in _fieldsByKey.Values)
            {
                if (byKey.TryGetValue(field.Key, out var def))
                    field.Apply(def);
            }

            return;
        }

        SettingGroups.Clear();
        _fieldsByKey.Clear();
        foreach (var (group, fields) in groups)
        {
            var vms = fields.Select(f =>
            {
                var vm = new SettingFieldViewModel(byKey[f.Key]);
                _fieldsByKey[f.Key] = vm;
                return vm;
            }).ToList();
            SettingGroups.Add(new SettingGroupViewModel(group, vms));
        }
    }

    public SettingFieldViewModel? FindField(string key) =>
        _fieldsByKey.TryGetValue(key, out var f) ? f : null;

    /// <summary>
    /// Uloží jedno pole. Potvrzení rizikových změn dělá backend nativním dialogem Windows;
    /// aplikace jen čeká a ukáže výsledek (uloženo / zamítnuto / nelze potvrdit).
    /// </summary>
    [RelayCommand]
    public async Task SaveSettingAsync(SettingFieldViewModel? field)
    {
        if (field is null || field.IsLocked)
            return;

        if (!field.TryGetEditValue(out var value, out var errorKey))
        {
            field.ErrorText = L10n.Get(errorKey!);
            return;
        }

        var waitsForDialog = MailControlForm.MovesAwayFromSafeValue(field.Spec, value);
        var busy = waitsForDialog
            ? L10n.Get("Str_MailControl_WaitingConfirm")
            : L10n.Get("Str_MailControl_Saving");

        var result = await RunAsync(ct => _client.SetSettingAsync(field.Key, value!, ct), busy);
        if (result is null)
        {
            // zamítnuto, nelze potvrdit, zamčeno, mimo rozsah…: hodnota na backendu zůstala beze změny
            field.ResetEdit();
            await LoadStateAsync();
            return;
        }

        ShowInfo(result.Changed
            ? L10n.Format(result.ConfirmedByHuman ? "Str_MailControl_SavedConfirmed" : "Str_MailControl_Saved", field.Label)
            : L10n.Format("Str_MailControl_Unchanged", field.Label));

        await LoadSettingsAsync();
        await LoadStateAsync();
    }

    /// <summary>Vrátí pole na bezpečnou hodnotu (do bezpečného stavu se přechází bez potvrzení).</summary>
    [RelayCommand]
    public async Task ResetToSafeAsync(SettingFieldViewModel? field)
    {
        if (field is null || !field.Spec.HasSafeValue)
            return;

        switch (field.Kind)
        {
            case SettingControlKind.Toggle:
                field.EditBool = field.Spec.SafeValue is true;
                break;
            case SettingControlKind.Choice:
                field.EditChoice = MailControlForm.Format(field.Spec.SafeValue);
                break;
            default:
                field.EditText = MailControlForm.Format(field.Spec.SafeValue);
                break;
        }

        await SaveSettingAsync(field);
    }

    [RelayCommand]
    public void ResetSettingEdit(SettingFieldViewModel? field) => field?.ResetEdit();
}
