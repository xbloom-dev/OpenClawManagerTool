using System.Windows;
using OpenClawManager.Services;

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
        DarkThemeRuntimeStyles.ApplyIfDark(this);

        BtnYes.Click += (_, _) => { DialogResult = true; Close(); };
        BtnNo.Click += (_, _) => { DialogResult = false; Close(); };

        BtnYes.ToolTip = L10n.IsCzech
            ? "Zastaví Gateway podle vybraných voleb."
            : "Stops Gateway using the selected options.";
        BtnNo.ToolTip = L10n.IsCzech
            ? "Zruší akci a nechá Gateway běžet."
            : "Cancels the action and leaves Gateway running.";
    }
}
