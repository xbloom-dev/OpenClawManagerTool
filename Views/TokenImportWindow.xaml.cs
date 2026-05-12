using System.IO;
using System.Windows;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class TokenImportWindow : Window
{
    public string TokenId => TxtId.Text.Trim();
    public string TokenValue => TxtContent.SelectedText;
    public string TokenDescription => TxtDescription.Text.Trim();

    private bool Cs => L10n.IsCzech;
    private string T(string cs, string en) => Cs ? cs : en;

    public TokenImportWindow(string filePath)
    {
        InitializeComponent();

        Title = T($"Import tokenu - {Path.GetFileName(filePath)}", $"Import token - {Path.GetFileName(filePath)}");
        ApplyLocalization();
        TxtContent.Text = File.ReadAllText(filePath);

        BtnImport.Click += BtnImport_Click;
        BtnCancel.Click += (_, _) => { DialogResult = false; Close(); };
    }

    private void ApplyLocalization()
    {
        BtnImport.Content = T("Importovat vyber", "Import selection");
        BtnCancel.Content = T("Zrusit", "Cancel");
        TxtIdLabel.Text = "ID:";
        TxtDescriptionLabel.Text = T("Popis:", "Description:");
        TxtHint.Text = T("Oznac v textu hodnotu tokenu a klikni Importovat vyber.",
            "Select the token value in the text and click Import selection.");
    }

    private void BtnImport_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TokenId))
        {
            MessageBox.Show(T("Zadej ID tokenu.", "Enter token ID."), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(TokenValue))
        {
            MessageBox.Show(T("Oznac v textu hodnotu tokenu.", "Select the token value in the text."), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
