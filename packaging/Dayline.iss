#ifndef AppVersion
  #define AppVersion "0.6.1"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\installer\win-x64"
#endif

[Setup]
AppId=Dayline.Desktop
AppName=日迹 · Dayline
AppVersion={#AppVersion}
AppPublisher=mzz0928lmh-source
AppPublisherURL=https://github.com/mzz0928lmh-source/Dayline
AppSupportURL=https://github.com/mzz0928lmh-source/Dayline/issues
DefaultDirName={localappdata}\Programs\Dayline
DefaultGroupName=日迹 Dayline
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\artifacts\installer
OutputBaseFilename=Dayline-{#AppVersion}-Setup-x64
SetupIconFile=..\Assets\Dayline.ico
UninstallDisplayIcon={app}\Dayline.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut / 创建桌面快捷方式"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\日迹 Dayline"; Filename: "{app}\Dayline.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\日迹 Dayline"; Filename: "{app}\Dayline.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Dayline.exe"; Description: "Launch Dayline / 启动日迹"; Flags: nowait postinstall skipifsilent
