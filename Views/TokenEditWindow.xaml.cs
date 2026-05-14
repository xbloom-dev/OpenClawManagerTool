using System.Windows;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class TokenEditWindow : Window
{
    public string TokenId => TxtId.Text.Trim();
    public string TokenValue => PwdValue.Password;
    public string TokenDescription => TxtDescription.Text.Trim();

    private bool Cs => L10n.IsCzech;
    private string T(string cs, string en) => Cs ? cs : en;

    public TokenEditWindow(string title, TokenEntry? token = null, bool idReadOnly = false, bool rotateOnly = false)
    {
        InitializeComponent();

        Title = title;
        ApplyLocalization(token != null, rotateOnly);
        BtnSave.Click += BtnSave_Click;
        BtnCancel.Click += (_, _) => { DialogResult = false; Close(); };

        if (token != null)
        {
            TxtId.Text = token.Id;
            TxtDescription.Text = token.Description;
            PwdValue.ToolTip = rotateOnly
                ? T("Zadej novou hodnotu tokenu.", "Enter the new token value.")
                : T("Nech prázdné, pokud chceš zachovat aktuální hodnotu.", "Leave empty to keep the current value.");
        }

        TxtId.IsReadOnly = idReadOnly;
        if (rotateOnly)
        {
            TxtDescription.IsReadOnly = true;
            TxtDescription.Foreground = System.Windows.Media.Brushes.Gray;
        }
    }

    private void ApplyLocalization(bool editingExisting, bool rotateOnly)
    {
        BtnSave.Content = T("Uložit", "Save");
        BtnCancel.Content = T("Zrušit", "Cancel");
        TxtIdLabel.Text = "ID:";
        TxtValueLabel.Text = T("Hodnota:", "Value:");
        TxtDescriptionLabel.Text = T("Popis:", "Description:");
        TxtHint.Text = editingExisting && !rotateOnly
            ? T("ID smí obsahovat písmena, číslice a underscore. Nech hodnotu prázdnou pro zachování aktuálního tokenu.",
                "ID may contain letters, digits and underscore. Leave value empty to keep the current token.")
            : T("ID smí obsahovat písmena, číslice a underscore. Hodnota musí mít alespoň 8 znaků.",
                "ID may contain letters, digits and underscore. Value must be at least 8 characters.");
    }

    private void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
