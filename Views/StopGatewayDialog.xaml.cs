using System.Windows;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

/// <summary>
/// Dialog for confirming Stop Gateway with optional TUI window cleanup.
///
/// Usage:
///   var dlg = new StopGatewayDialog { Owner = this };
///   if (dlg.ShowDialog() == true) {
///       if (dlg.CloseTui) App.GetService&lt;IGatewayService&gt;().StopAndCloseTui();
///       else App.GetService&lt;IGatewayService&gt;().Stop();
///   }
/// </summary>
public partial class StopGatewayDialog : Window
{
    /// <summary>
    /// True when the user chose to close TUI windows too.
    /// Read after ShowDialog() == true.
    /// </summary>
    public bool CloseTui => ChkCloseTui.IsChecked == true;

    public StopGatewayDialog()
    {
        InitializeComponent();
        ModernPaletteRuntimeStyles.ApplyIfModernPalette(this);

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
