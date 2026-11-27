; WinTaskStat installer (Inno Setup 6).
; Built by .github/workflows/release.yml, which first:
;   - publishes the app to ..\publish\
;   - downloads PawnIO_setup.exe to redist\ and verifies its SHA256
; Local build: ISCC.exe /DAppVersion=0.0.1 installer\TaskbarStatus.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

[Setup]
AppId={{6F3B2C1E-8A4D-4F7B-9C2E-5D1A0B7E3F42}
AppName=WinTaskStat
AppVersion={#AppVersion}
AppPublisher=Jackaopen
DefaultDirName={autopf}\WinTaskStat
DefaultGroupName=WinTaskStat
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\src\TaskbarStatus\Assets\tray.ico
UninstallDisplayIcon={app}\TaskbarStatus.exe
OutputDir=..\dist
OutputBaseFilename=WinTaskStat-{#AppVersion}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; dontcopy: only extracted to {tmp} by the [Code] section below, never installed to {app}.
Source: "redist\PawnIO_setup.exe"; Flags: dontcopy

[Icons]
Name: "{group}\WinTaskStat"; Filename: "{app}\TaskbarStatus.exe"
Name: "{group}\Uninstall WinTaskStat"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\TaskbarStatus.exe"; Description: "Launch WinTaskStat"; Flags: nowait postinstall skipifsilent

[Code]
var
  PawnIONeedsRestart: Boolean;

{ Install the PawnIO driver silently. Exit codes from its winget manifest:
  0 = installed, 3010 = installed but a restart is needed. }
procedure InstallPawnIO();
var
  ResultCode: Integer;
begin
  WizardForm.StatusLabel.Caption := 'Installing PawnIO driver (needed for CPU temperature)...';
  ExtractTemporaryFile('PawnIO_setup.exe');
  if not Exec(ExpandConstant('{tmp}\PawnIO_setup.exe'), '-install -silent', '',
              SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    SuppressibleMsgBox('Could not start the PawnIO driver setup: ' + SysErrorMessage(ResultCode) +
      #13#10 + 'WinTaskStat will still work, but CPU temperature may be unavailable.',
      mbError, MB_OK, IDOK);
  end
  else if ResultCode = 3010 then
    PawnIONeedsRestart := True
  else if ResultCode <> 0 then
    SuppressibleMsgBox('PawnIO driver setup failed (code ' + IntToStr(ResultCode) + ').' +
      #13#10 + 'WinTaskStat will still work, but CPU temperature may be unavailable.',
      mbError, MB_OK, IDOK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    InstallPawnIO();
end;

function NeedRestart(): Boolean;
begin
  Result := PawnIONeedsRestart;
end;

{ PawnIO is intentionally NOT removed on uninstall: other apps (e.g. FanControl)
  may share the same driver. }
