#define AppVersion GetEnv("TM_VERSION")
#if AppVersion == ""
  #define AppVersion "0.0.0"
#endif
[Setup]
AppId={{6C3E9B2A-6E0B-4C59-9E0E-7A1B2C3D4E5F}
AppName=TibiaMirror
AppVersion={#AppVersion}
AppPublisher=TibiaMirror contributors
DefaultDirName={autopf}\TibiaMirror
DefaultGroupName=TibiaMirror
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\artifacts
OutputBaseFilename=TibiaMirror-Setup-{#AppVersion}
SetupIconFile=..\src\TibiaMirror\Assets\Icons\app.ico
UninstallDisplayIcon={app}\TibiaMirror.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
AppMutex=Local\TibiaMirror.SingleInstance
CloseApplications=yes
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "startup"; Description: "Start TibiaMirror when Windows starts"; Flags: unchecked
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
[Icons]
Name: "{group}\TibiaMirror"; Filename: "{app}\TibiaMirror.exe"
Name: "{autodesktop}\TibiaMirror"; Filename: "{app}\TibiaMirror.exe"; Tasks: desktopicon
Name: "{autostartup}\TibiaMirror"; Filename: "{app}\TibiaMirror.exe"; Tasks: startup
[Run]
Filename: "{app}\TibiaMirror.exe"; Description: "Launch TibiaMirror"; Flags: nowait postinstall skipifsilent
