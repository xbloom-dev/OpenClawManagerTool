using System.IO;
using System.Windows;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class TokenImportWindow : Window
{
    public string TokenId => TxtId.Text.Trim();
    public string TokenValue => TxtContent.SelectedText;
    public string TokenDescription => TxtDescription.Text.Trim();

    private static string S(string key) => L10n.Get(key);
    private static string F(string key, params object[] args) => L10n.Format(key, args);

    public TokenImportWindow(string filePath)
    {
        InitializeComponent();

        Title = F("Str_Token_ImportTitle", Path.GetFileName(filePath));
        ApplyLocalization();
        TxtContent.Text = File.ReadAllText(filePath);

        BtnImport.Click += BtnImport_Click;
        BtnCancel.Click += (_, _) => { DialogResult = false; Close(); };
    }

    private void ApplyLocalization()
    {
        BtnImport.Content = S("Str_Token_ImportSelection");
        BtnImport.ToolTip = S("Str_Token_TipImportSelection");
        BtnCancel.Content = S("Str_BtnCancel");
        BtnCancel.ToolTip = S("Str_Token_TipImportCancel");
        TxtIdLabel.Text = "ID:";
        TxtDescriptionLabel.Text = S("Str_Token_Description") + ":";
        TxtHint.Text = S("Str_Token_ImportHint");
    }

    private void BtnImport_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TokenId))
        {
            MessageBox.Show(S("Str_Token_ImportMissingId"), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(TokenValue))
        {
            MessageBox.Show(S("Str_Token_ImportMissingValue"), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
