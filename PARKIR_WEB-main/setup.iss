#define MyAppName "ParkIRC Parking System"
#define MyAppVersion "1.0"
#define MyAppPublisher "Your Company Name"
#define MyAppExeName "ParkIRC.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
AppId={{2023E1D5-7F6A-4C3B-B70E-A83293A1F3E1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir=installer
OutputBaseFilename=ParkIRC_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\net6.0\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Create and configure start.bat during installation
procedure CreateStartBat();
var
  FilePath: String;
  Lines: TArrayOfString;
begin
  FilePath := ExpandConstant('{app}\start.bat');
  SetArrayLength(Lines, 7);
  Lines[0] := '@echo off';
  Lines[1] := 'echo Starting ParkIRC Parking System...';
  Lines[2] := 'echo.';
  Lines[3] := 'echo Please wait while the application starts...';
  Lines[4] := 'echo The application will open in your default browser automatically.';
  Lines[5] := 'start "" "http://localhost:5126"';
  Lines[6] := 'ParkIRC.exe';
  SaveStringsToFile(FilePath, Lines, False);
end;

// Add start.bat creation to the installation process
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    CreateStartBat();
  end;
end; 