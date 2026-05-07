using System.Windows;

namespace OpenClawManager.Views;

/// <summary>
/// Dialog pro potvrzení Stop Gateway s volitelným zavřením TUI oken.
///
/// Použití:
///   var dlg = new StopGatewayDialog { Owner = this };
///   if (dlg.ShowDialog() == true) {
///       if (dlg.CloseTui) GatewayService.StopAndCloseTui();
///       else GatewayService.Stop();
///   }
/// </summary>
public partial class StopGatewayDialog : Window
{
    /// <summary>
    /// True pokud uživatel zaškrtl "Zavřít také TUI okna".
    /// Čte se po ShowDialog() == true.
    /// </summary>
    public bool CloseTui => ChkCloseTui.IsChecked == true;

    public StopGatewayDialog()
    {
        InitializeComponent();

        BtnYes.Click += (_, _) => { DialogResult = true; Close(); };
        BtnNo.Click += (_, _) => { DialogResult = false; Close(); };
    }
}
