; Inno Setup script for the Batch Parameter Update Revit add-in.
; Installs per-user, so no administrator rights are required.

#define AppName        "Batch Parameter Update"
#define AppVersion     "1.0.0"
#define AppPublisher   "Sebastian Fuerte"
#define RevitVersion   "2026"
#define AddinFolder    "BatchParameterUpdate"

; Paths are relative to this script's folder (installer\).
#define BuildOutput    "..\src\BatchParameterUpdate\bin\Release\net8.0-windows"
#define AddinManifest  "..\src\BatchParameterUpdate\Resources\BatchParameterUpdate.addin"

[Setup]
AppId={{8F3C1A94-2D57-4E6B-9C08-5A71E3B4D260}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={userappdata}\Autodesk\Revit\Addins\{#RevitVersion}
DisableDirPage=yes
DisableProgramGroupPage=yes
CreateAppDir=yes
Uninstallable=yes
UninstallDisplayName={#AppName} for Revit {#RevitVersion}
OutputDir=Output
OutputBaseFilename=BatchParameterUpdate-{#AppVersion}-Revit{#RevitVersion}-Setup
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern

[Files]
; The manifest goes in the Addins root, where Revit scans for it.
Source: "{#AddinManifest}"; DestDir: "{app}"; Flags: ignoreversion

; Binaries go in their own subfolder to avoid DLL collisions with other add-ins.
Source: "{#BuildOutput}\BatchParameterUpdate.dll"; DestDir: "{app}\{#AddinFolder}"; Flags: ignoreversion

[Code]
function IsRevitInstalled: Boolean;
begin
  Result := RegKeyExists(HKEY_LOCAL_MACHINE,
    'SOFTWARE\Autodesk\Revit\Autodesk Revit {#RevitVersion}');
end;

function InitializeSetup: Boolean;
begin
  Result := True;

  if not IsRevitInstalled then
  begin
    Result := MsgBox(
      'Autodesk Revit {#RevitVersion} was not detected on this machine.' + #13#10#13#10 +
      'The add-in will still be installed, but it will only load once Revit {#RevitVersion} is present.' + #13#10#13#10 +
      'Continue anyway?',
      mbConfirmation, MB_YESNO) = IDYES;
  end;
end;