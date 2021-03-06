; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "ScincNet"
#define MyAppVersion "0.02"
#define MyAppPublisher "Phil Brooks"
#define MyAppURL "https://pbbwfc.github.io/ScincNet/"
#define MyAppExeName "ScincNet.exe"
#define MyAppIcoName "scinc.ico"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{3F75A24B-23D8-4039-A451-F19EF29FC78D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf64}\{#MyAppName}
DefaultGroupName={#MyAppName}
LicenseFile=D:\GitHub\ScincNet\License.txt
OutputDir=D:\GitHub\ScincNet\inno
OutputBaseFilename=setup
SetupIconFile=D:\GitHub\ScincNet\srcNet\ScincNet\Icons\scinc.ico
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "D:\GitHub\ScincNet\rel\netcoreapp3.1\ScincNet.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\GitHub\ScincNet\rel\netcoreapp3.1\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: "bases\*"; DestDir: "{userdocs}\ScincNet\bases"; Flags: ignoreversion
Source: "repertoire\*"; DestDir: "{userdocs}\ScincNet\repertoire"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}" ; IconFilename: "{app}\{#MyAppIcoName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppIcoName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
