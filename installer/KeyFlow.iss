#ifndef AppSource
  #define AppSource "..\publish"
#endif
#ifndef ReleaseOutput
  #define ReleaseOutput "..\artifacts"
#endif
#define AppVersion "3.1.0-beta.1"

[Setup]
AppId={{1473F27C-7920-4C3D-961F-9C29D2287812}
AppName=KeyFlow
AppVersion={#AppVersion}
AppVerName=KeyFlow {#AppVersion}
AppPublisher=piwric77 (AI-built with Codex)
AppPublisherURL=https://github.com/piwric77/KeyFlow
AppSupportURL=https://github.com/piwric77/KeyFlow/issues
AppUpdatesURL=https://github.com/piwric77/KeyFlow/releases
DefaultDirName={localappdata}\Programs\KeyFlow
DefaultGroupName=KeyFlow
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
#ifdef WorkspaceValidation
CreateUninstallRegKey=no
#else
AppMutex=Local\KeyFlow.Copilot.v1
#endif
OutputDir={#ReleaseOutput}
OutputBaseFilename=KeyFlow-{#AppVersion}-Setup-x64
#ifdef WorkspaceValidation
Compression=none
SolidCompression=no
#else
Compression=lzma2
SolidCompression=yes
#endif
WizardStyle=modern
WizardSizePercent=120
InfoBeforeFile=AI-NOTICE.txt
UninstallDisplayIcon={app}\KeyFlow.exe
VersionInfoVersion=3.1.0.0
VersionInfoDescription=KeyFlow installer - AI-built compatibility beta
VersionInfoCopyright=piwric77 - Project built with AI
#ifdef WorkspaceValidation
CloseApplications=no
#else
CloseApplications=yes
#endif
RestartApplications=no
DisableProgramGroupPage=yes
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#AppSource}\*"; DestDir: "{app}"; Excludes: "*.log,*.pdb,settings.json,settings.json.bak,settings.json.tmp,*-test.txt,preview-*.png,startup-test.log"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "AI-NOTICE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\KeyFlow"; Filename: "{app}\KeyFlow.exe"
Name: "{group}\Uninstall KeyFlow"; Filename: "{uninstallexe}"
Name: "{autodesktop}\KeyFlow"; Filename: "{app}\KeyFlow.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\KeyFlow.exe"; Description: "Launch KeyFlow"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ShortcutPath, TargetPath: String;
  Shell, Link: Variant;
begin
  if CurUninstallStep = usUninstall then
  begin
    ShortcutPath := ExpandConstant('{userstartup}\KeyFlow.lnk');
    if FileExists(ShortcutPath) then
    begin
      try
        Shell := CreateOleObject('WScript.Shell');
        Link := Shell.CreateShortcut(ShortcutPath);
        TargetPath := Link.TargetPath;
        if CompareText(ExpandFileName(TargetPath), ExpandFileName(ExpandConstant('{app}\KeyFlow.exe'))) = 0 then
          DeleteFile(ShortcutPath);
      except
        Log('Could not inspect the startup shortcut; left it unchanged.');
      end;
    end;
  end;
end;
