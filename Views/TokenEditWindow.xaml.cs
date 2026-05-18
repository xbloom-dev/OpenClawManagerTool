using System.Windows;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class TokenEditWindow : Window
{
    public string TokenId => TxtId.Text.Trim();
    public string TokenValue => PwdValue.Password;
    public string TokenDescription => TxtDescription.Text.Trim();

    private static string S(string key) => L10n.Get(key);

    public TokenEditWindow(string title, TokenEntry? token = null, bool idReadOnly = false, bool rotateOnly = false)
    {
        InitializeComponent();
        DarkThemeRuntimeStyles.ApplyIfDark(this);

        Title = title;
        ApplyLocalization(token != null, rotateOnly);
        BtnSave.Click += BtnSave_Click;
        BtnCancel.Click += (_, _) => { DialogResult = false; Close(); };

        if (token != null)
        {
            TxtId.Text = token.Id;
            TxtDescription.Text = token.Description;
            PwdValue.ToolTip = rotateOnly
                ? S("Str_Token_EditTipNewValue")
                : S("Str_Token_EditTipKeepValue");
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
        BtnSave.Content = S("Str_BtnSave");
        BtnSave.ToolTip = S("Str_Token_TipEditSave");
        BtnCancel.Content = S("Str_BtnCancel");
        BtnCancel.ToolTip = S("Str_Token_TipEditCancel");
        TxtIdLabel.Text = "ID:";
        TxtValueLabel.Text = S("Str_Token_Value");
        TxtDescriptionLabel.Text = S("Str_Token_Description") + ":";
        TxtHint.Text = editingExisting && !rotateOnly
            ? S("Str_Token_EditHintKeep")
            : S("Str_Token_EditHintNew");
    }

    private void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
