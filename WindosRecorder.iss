#define MyAppName "X Recorder"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "long-X"
#define MyAppExeName "WindosRecorder.exe"

[Setup]
AppId={{B52A0B18-4E0E-4A56-8650-B17D5E0C7614}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\X Recorder
DefaultGroupName=X Recorder
AllowNoIcons=yes
Compression=lzma
SolidCompression=yes
WizardStyle=modern
OutputDir=dist\installer
OutputBaseFilename=X-Recorder-Setup
SetupIconFile=WindosRecorder\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
ShowLanguageDialog=yes
UsePreviousLanguage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "installer-assets\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "dist\WindosRecorder-portable-folder\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "installer-assets\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\X Recorder"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall X Recorder"; Filename: "{uninstallexe}"
Name: "{autodesktop}\X Recorder"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[CustomMessages]
english.LaunchApp=Launch X Recorder
chinesesimplified.LaunchApp=启动 X Recorder
english.InstallVCRedist=Install required Microsoft Visual C++ Runtime
chinesesimplified.InstallVCRedist=安装所需的 Microsoft Visual C++ 运行库

[Run]
Filename: "{tmp}\vc_redist.x64.exe"; Parameters: "/install /passive /norestart"; StatusMsg: "{cm:InstallVCRedist}"; Flags: waituntilterminated runhidden; Check: NeedsVCRedist
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[Code]
function NeedsVCRedist: Boolean;
var
  Installed: Cardinal;
begin
  Result := True;
  if RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Installed) then
    Result := Installed <> 1;
end;
