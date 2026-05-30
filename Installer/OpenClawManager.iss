#define AppName "OpenClaw Manager Tool"
#ifndef AppVersion
#define AppVersion "2.0.2"
#endif
#ifndef FullSourceDir
#define FullSourceDir "..\artifacts\publish-full"
#endif
#ifndef LiteSourceDir
#define LiteSourceDir "..\artifacts\publish-lite"
#endif
#define Publisher "OpenClaw"
#define ExeName "OpenClawManager.exe"
#define WebView2Url "https://developer.microsoft.com/microsoft-edge/webview2/"

[Setup]
AppId={{8D70CC21-BE99-475D-A2C7-1BA6C0C0A92F}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
; Instalace do Program Files — vyžaduje administrátorská oprávnění (UAC)
DefaultDirName={autopf}\OpenClawManager
DefaultGroupName={#AppName}
OutputBaseFilename=OpenClawManagerTool-v{#AppVersion}-win-x64-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
DisableProgramGroupPage=yes
SetupIconFile=..\Resources\app-icon.ico
UninstallDisplayIcon={app}\{#ExeName}

[Types]
Name: "full"; Description: "Full Install (all themes + splash video)"
Name: "lite"; Description: "Lite Install (Legacy theme only, no splash video)"

[Components]
Name: "edition_full"; Description: "Full edition (includes Resources\splash.mp4)"; Types: full; Flags: fixed exclusive
Name: "edition_lite"; Description: "Lite edition (no splash video)";               Types: lite; Flags: fixed exclusive

[Files]
; Full edition — vše kromě settings.json a .pdb symbolů
Source: "{#FullSourceDir}\*"; DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Excludes: "settings.json,*.pdb"; \
  Components: edition_full

; Lite edition — vše kromě settings.json, .pdb a splash.mp4
Source: "{#LiteSourceDir}\*"; DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Excludes: "settings.json,*.pdb,Resources\splash.mp4"; \
  Components: edition_lite

[Tasks]
Name: "desktopicon"; \
  Description: "{cm:CreateDesktopIcon}"; \
  GroupDescription: "{cm:AdditionalIcons}"; \
  Flags: unchecked
Name: "webview2shortcut"; \
  Description: "Create WebView2 Runtime download shortcut in Start menu"; \
  Flags: unchecked

[Icons]
Name: "{group}\{#AppName}";                             Filename: "{app}\{#ExeName}"
Name: "{autodesktop}\{#AppName}";                       Filename: "{app}\{#ExeName}"; Tasks: desktopicon
Name: "{group}\Install Microsoft Edge WebView2 Runtime"; Filename: "{#WebView2Url}";  Tasks: webview2shortcut

[Run]
Filename: "{app}\{#ExeName}"; \
  Description: "Launch {#AppName}"; \
  Flags: nowait postinstall skipifsilent

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
  ClientKey:       String;
  ClientKeyWow64:  String;
begin
  ClientKey      := 'SOFTWARE\Microsoft\EdgeUpdate\Clients\' + WebView2ClientGuid;
  ClientKeyWow64 := 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\' + WebView2ClientGuid;

  Result :=
    HasVersionValue(HKCU,    ClientKey)       or
    HasVersionValue(HKLM,    ClientKeyWow64)  or
    HasVersionValue(HKLM64,  ClientKey)       or
    HasVersionValue(HKLM64,  ClientKeyWow64);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
begin
  if CurStep <> ssPostInstall then
    exit;

  if IsWebView2RuntimeInstalled() then
    exit;

  // WebView2 nenalezen — informovat uživatele, instalaci nezastavovat
  if MsgBox(
       'Microsoft Edge WebView2 Runtime nebyl nalezen.' + #13#10 +
       'OpenClaw Manager spustit lze, ale zabudovaný terminál WebView2 vyžaduje.' + #13#10#13#10 +
       'Otevřít stránku pro stažení WebView2 Runtime?',
       mbInformation,
       MB_YESNO) = IDYES then
  begin
    ShellExec('open', '{#WebView2Url}', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;
