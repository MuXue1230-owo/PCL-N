#ifndef Payload
  #error Payload is required
#endif
[Setup]
AppId={{50AE2197-7C7B-43DA-BE98-1CDEBE86B273}
AppName=PCL Nexa
AppVersion={#ProductVersion}
VersionInfoVersion={#NumericVersion}.0
DefaultDirName={localappdata}\Programs\PCL Nexa
DefaultGroupName=PCL Nexa
PrivilegesRequired=lowest
ArchitecturesAllowed={#InstallArch}
ArchitecturesInstallIn64BitMode={#InstallArch}
OutputDir={#OutputDir}
OutputBaseFilename={#OutputName}
SetupIconFile={#IconPath}
UninstallDisplayIcon={app}\PCL.Desktop.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
[Files]
Source: "{#Payload}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\PCL Nexa"; Filename: "{app}\PCL.Desktop.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\PCL Nexa"; Filename: "{app}\PCL.Desktop.exe"; WorkingDir: "{app}"; Tasks: desktopicon
