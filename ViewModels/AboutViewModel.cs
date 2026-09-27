using CommunityToolkit.Mvvm.ComponentModel;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class AboutViewModel : ObservableObject
{
    public AboutViewModel()
    {
        AppVersion = AppVersionInfo.Display;
        RefreshLocalization();
    }

    public string AppName { get; } = "OpenClaw Manager Tool by Bloom";
    public string AppVersion { get; }
    public string RuntimeInfo { get; } = ".NET 8  |  WPF  |  WebView2 + ConPTY";

    [ObservableProperty]
    private string _windowTitle = "";

    [ObservableProperty]
    private string _closeText = "";

    [ObservableProperty]
    private string _closeToolTip = "";

    [ObservableProperty]
    private string _startTuiShortcut = "";

    [ObservableProperty]
    private string _gatewayShortcut = "";

    [ObservableProperty]
    private string _restartGatewayShortcut = "";

    [ObservableProperty]
    private string _cleaningToolShortcut = "";

    [ObservableProperty]
    private string _settingsShortcut = "";

    [ObservableProperty]
    private string _gatewayLiveLogShortcut = "";

    [ObservableProperty]
    private string _aboutShortcut = "";

    [ObservableProperty]
    private string _closeApplicationShortcut = "";

    public void RefreshLocalization()
    {
        var cs = L10n.Current == L10n.Language.CS;

        WindowTitle = cs ? "O aplikaci" : "About";
        CloseText = cs ? "Zavřít" : "Close";
        CloseToolTip = L10n.Get("Str_Tip_AboutClose");
        StartTuiShortcut = "Start/Stop OpenClaw TUI";
        GatewayShortcut = "Start/Stop Gateway";
        RestartGatewayShortcut = "Restart Gateway";
        CleaningToolShortcut = cs ? "Vyčistit soubory" : "Cleaning Tool";
        SettingsShortcut = cs ? "Nastavení" : "Settings";
        GatewayLiveLogShortcut = cs ? "Živá data Gateway logu" : "Gateway live log";
        AboutShortcut = cs ? "O aplikaci" : "About";
        CloseApplicationShortcut = cs ? "Zavřít aplikaci" : "Close application";
    }
}
