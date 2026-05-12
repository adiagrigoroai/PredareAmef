; Inno Setup script — PredareAmef
; Compilare: ISCC.exe PredareAmef.iss
; Rezultat: Output\PredareAmef_Setup_<versiune>.exe

#define AppName        "PredareAmef"
#define AppVersion     "1.2.0"
#define AppPublisher   "Qbiz"
#define AppExeName     "PredareAmef.exe"
#define ReleaseDir     "..\PredareAmef\bin\Release"

[Setup]
AppId={{C7B3D2A1-9F4E-4F7B-A5C2-E1D8B0F3A2C9}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://qbiz.ro
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=PredareAmef_Setup_{#AppVersion}
SetupIconFile=
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#AppExeName}
LicenseFile=
ShowLanguageDialog=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#ReleaseDir}\PredareAmef.exe";        DestDir: "{app}"; Flags: ignoreversion
Source: "{#ReleaseDir}\PredareAmef.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ReleaseDir}\Interop.dude.dll";       DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}";  Filename: "{app}\{#AppExeName}"; Comment: "Utilitar predare memorie fiscala AMEF"
Name: "{autodesktop}\{#AppName}";   Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  Version: Cardinal;
begin
  Result := True;
  // Verifica .NET Framework 4.7.2 (release >= 461808)
  if not RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Version) then
  begin
    if MsgBox('PredareAmef necesita .NET Framework 4.7.2 sau mai nou.' + #13#10 +
              'Nu pare instalat. Continui oricum?',
              mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end
  else if Version < 461808 then
  begin
    if MsgBox('PredareAmef necesita .NET Framework 4.7.2 sau mai nou.' + #13#10 +
              'Versiunea instalata e mai veche (release ' + IntToStr(Version) + ').' + #13#10 +
              'Continui oricum?',
              mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;
