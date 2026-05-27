#define AppName "OpenClaw Manager Tool"
#ifndef AppVersion
#define AppVersion "2.0.2"
#endif
#ifndef SourceDir
#define SourceDir "..\dist\installer\full"
#endif
#define Publisher "OpenClaw"
#define ExeName "OpenClawManager.exe"
#define WebView2Url "https://developer.microsoft.com/microsoft-edge/webview2/"

[Setup]
AppId={{8D70CC21-BE99-475D-A2C7-1BA6C0C0A92F}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
DefaultDirName={localappdata}\Programs\OpenClawManager
DefaultGroupName={#AppName}
OutputBaseFilename=OpenClawManagerTool-v{#AppVersion}-win-x64-full-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
SetupIconFile=..\Resources\app-icon.ico
UninstallDisplayIcon={app}\{#ExeName}

[Types]
Name: "full"; Description: "Full installation"
Name: "compact"; Description: "Lite-style shortcuts only"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Components]
Name: "edition_full"; Description: "Full assets: themes, icons, splash video, and scripts"; Types: full custom; Flags: fixed
Name: "shortcut_startmenu"; Description: "Start menu shortcut"; Types: full compact custom; Flags: fixed
Name: "shortcut_desktop"; Description: "Desktop shortcut"; Types: full custom
Name: "help_webview2"; Description: "Add WebView2 Runtime download shortcut"; Types: custom

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "settings.json,*.pdb"; Components: edition_full

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#ExeName}"; Components: shortcut_startmenu
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#ExeName}"; Components: shortcut_desktop
Name: "{group}\Install Microsoft Edge WebView2 Runtime"; Filename: "{#WebView2Url}"; Components: help_webview2

[Run]
Filename: "{app}\{#ExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
const
  WebView2ClientGuid = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';

function HasVersionValue(RootKey: Integer; KeyName: String): Boolean;
var
  Version: String;
begin
  Result := RegQueryStringValue(RootKey, KeyName, 'pv', Version)
    and (Version <> '')
    and (Version <> '0.0.0.0');
end;

function IsWebView2RuntimeInstalled(): Boolean;
var
  ClientKey: String;
  ClientKeyWow64: String;
begin
  ClientKey := 'SOFTWARE\Microsoft\EdgeUpdate\Clients\' + WebView2ClientGuid;
  ClientKeyWow64 := 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\' + WebView2ClientGuid;

  Result :=
    HasVersionValue(HKCU, ClientKey) or
    HasVersionValue(HKLM, ClientKeyWow64) or
    HasVersionValue(HKLM64, ClientKey) or
    HasVersionValue(HKLM64, ClientKeyWow64);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
begin
  if CurStep <> ssPostInstall then
    exit;

  if IsWebView2RuntimeInstalled() then
    exit;

  if MsgBox(
       'Microsoft Edge WebView2 Runtime was not detected. OpenClaw Manager can run, but the embedded terminal needs WebView2.'#13#10#13#10 +
       'Open the official Microsoft download page now?',
       mbInformation,
       MB_YESNO) = IDYES then
  begin
    ShellExec('open', '{#WebView2Url}', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;
