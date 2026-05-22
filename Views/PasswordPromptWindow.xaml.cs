using System.Windows;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class PasswordPromptWindow : Window
{
    private readonly bool _requireConfirmation;

    public string Password => PwdPassword.Password;

    private static string S(string key) => L10n.Get(key);

    public PasswordPromptWindow(string title, string message, bool requireConfirmation)
    {
        InitializeComponent();
        ModernPaletteRuntimeStyles.ApplyIfModernPalette(this);

        _requireConfirmation = requireConfirmation;
        Title = title;
        TxtMessage.Text = message;

        BtnOk.Content = S("Str_BtnOK");
        BtnCancel.Content = S("Str_BtnCancel");
        TxtPasswordLabel.Text = S("Str_Token_BackupPasswordLabel");
        TxtConfirmLabel.Text = S("Str_Token_BackupConfirmPasswordLabel");
        TxtConfirmLabel.Visibility = requireConfirmation ? Visibility.Visible : Visibility.Collapsed;
        PwdConfirm.Visibility = requireConfirmation ? Visibility.Visible : Visibility.Collapsed;

        BtnOk.Click += (_, _) => Confirm();
        BtnCancel.Click += (_, _) => { DialogResult = false; Close(); };
        Loaded += (_, _) => PwdPassword.Focus();
    }

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(PwdPassword.Password))
        {
            MessageBox.Show(S("Str_Token_BackupPasswordRequired"), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_requireConfirmation && !string.Equals(PwdPassword.Password, PwdConfirm.Password, StringComparison.Ordinal))
        {
            MessageBox.Show(S("Str_Token_BackupPasswordMismatch"), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
