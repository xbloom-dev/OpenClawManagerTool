using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

/// <summary>Jedno pole formuláře nastavení, vykreslené ze schématu backendu.</summary>
public sealed partial class SettingFieldViewModel : ObservableObject
{
    public SettingFieldViewModel(SettingDefinition definition)
    {
        Spec = MailControlForm.BuildSpec(definition);
        Choices = new ObservableCollection<string>(Spec.Choices);
        Definition = definition;
        Apply(definition);
    }

    public SettingFieldSpec Spec { get; }

    public ObservableCollection<string> Choices { get; }

    public string Key => Spec.Key;
    public string Label => Spec.Label;
    public string Description => Spec.Description;
    public string Unit => Spec.Unit;
    public SettingControlKind Kind => Spec.Kind;
    public bool IsToggle => Kind == SettingControlKind.Toggle;
    public bool IsInteger => Kind == SettingControlKind.Integer;
    public bool IsText => Kind == SettingControlKind.Text;
    public bool IsChoice => Kind == SettingControlKind.Choice;
    public bool HasUnit => Unit.Length > 0;
    public bool HasDescription => Description.Length > 0;
    public bool RequiresConfirmation => Spec.RequiresConfirmation;
    public bool HasSafeValue => Spec.HasSafeValue;

    /// <summary>Rozsah celého čísla nebo délka textu pro nápovědu (např. „1–200“).</summary>
    public string RangeText =>
        Kind == SettingControlKind.Integer && (Spec.Min.HasValue || Spec.Max.HasValue)
            ? $"{Spec.Min?.ToString() ?? "…"}–{Spec.Max?.ToString() ?? "…"}"
            : Kind == SettingControlKind.Text && Spec.MaxLength.HasValue
                ? $"≤ {Spec.MaxLength}"
                : "";

    public bool HasRange => RangeText.Length > 0;

    public SettingDefinition Definition { get; private set; }

    [ObservableProperty] private bool _editBool;
    [ObservableProperty] private string _editText = "";
    [ObservableProperty] private string? _editChoice;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private string _errorText = "";
    [ObservableProperty] private bool _isLocked;
    [ObservableProperty] private bool _movesAwayFromSafe;
    [ObservableProperty] private bool _isAwayFromSafe;
    [ObservableProperty] private string _sourceText = "";

    public bool HasError => ErrorText.Length > 0;

    public bool IsEditable => !IsLocked;
    public bool IsTextual => IsInteger || IsText;

    /// <summary>Uložit jde jen platná, změněná a nezamčená hodnota.</summary>
    public bool CanSave => IsDirty && !HasError && IsEditable;

    /// <summary>Tlačítko „bezpečná hodnota“: jen u položek s potvrzením, které jsou mimo bezpečnou hodnotu.</summary>
    public bool ShowSafeButton => HasSafeValue && RequiresConfirmation && IsAwayFromSafe && IsEditable;

    partial void OnIsLockedChanged(bool value) => NotifyDerived();

    partial void OnIsDirtyChanged(bool value) => NotifyDerived();

    partial void OnIsAwayFromSafeChanged(bool value) => NotifyDerived();

    private void NotifyDerived()
    {
        OnPropertyChanged(nameof(IsEditable));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(ShowSafeButton));
    }

    partial void OnErrorTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnEditBoolChanged(bool value) => Recompute();
    partial void OnEditTextChanged(string value) => Recompute();
    partial void OnEditChoiceChanged(string? value) => Recompute();

    /// <summary>Aktuální hodnota na backendu (jako text pro zobrazení).</summary>
    public string CurrentText => Kind == SettingControlKind.Toggle
        ? L10n.Get(Definition.Value is true ? "Str_MailControl_Value_On" : "Str_MailControl_Value_Off")
        : MailControlForm.Format(Definition.Value);

    /// <summary>Nastaví hodnotu z backendu a vymaže rozpracovanou úpravu.</summary>
    public void Apply(SettingDefinition definition)
    {
        Definition = definition;
        IsLocked = definition.Locked || !definition.Writable;
        SourceText = L10n.Get(definition.IsDefault ? "Str_MailControl_Src_Default" : "Str_MailControl_Src_File");
        IsAwayFromSafe = Spec.RequiresConfirmation && Spec.HasSafeValue
            && !MailControlForm.ValuesEqual(definition.Value, Spec.SafeValue);
        ResetEdit();
        OnPropertyChanged(nameof(CurrentText));
    }

    /// <summary>Zahodí rozpracovanou úpravu a vrátí pole na hodnotu z backendu.</summary>
    public void ResetEdit()
    {
        var v = Definition.Value;
        switch (Kind)
        {
            case SettingControlKind.Toggle:
                EditBool = v is true;
                break;
            case SettingControlKind.Choice:
                EditChoice = MailControlForm.Format(v);
                break;
            default:
                EditText = MailControlForm.Format(v);
                break;
        }

        Recompute();
    }

    /// <summary>Typovaná hodnota z pole; false a klíč chyby, když vstup neprošel klientským ověřením.</summary>
    public bool TryGetEditValue(out object? value, out string? errorKey)
    {
        errorKey = Kind switch
        {
            SettingControlKind.Toggle => MailControlForm.Validate(Spec, EditBool.ToString(), out value),
            SettingControlKind.Choice => MailControlForm.Validate(Spec, EditChoice, out value),
            _ => MailControlForm.Validate(Spec, EditText, out value)
        };
        return errorKey is null;
    }

    private void Recompute()
    {
        if (Definition is null)
            return;

        var valid = TryGetEditValue(out var value, out var errorKey);
        ErrorText = valid ? "" : L10n.Get(errorKey!);
        IsDirty = !valid || !MailControlForm.ValuesEqual(value, Definition.Value);
        MovesAwayFromSafe = valid && IsDirty && MailControlForm.MovesAwayFromSafeValue(Spec, value);
    }
}

/// <summary>Skupina nastavení (podle pole group ze schématu).</summary>
public sealed class SettingGroupViewModel
{
    public SettingGroupViewModel(string name, IEnumerable<SettingFieldViewModel> fields)
    {
        Name = name;
        Fields = new ObservableCollection<SettingFieldViewModel>(fields);
    }

    public string Name { get; }

    public ObservableCollection<SettingFieldViewModel> Fields { get; }
}
