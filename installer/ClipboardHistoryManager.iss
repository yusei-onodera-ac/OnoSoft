; おのソフト - クリップボード履歴マネージャー インストーラー (Inno Setup)
;
; ビルド時に以下を必ず指定する:
;   iscc ClipboardHistoryManager.iss /DMyAppVersion=1.0.0 /DSourceDir=..\publish\ClipboardHistoryManager
;
; - 管理者権限は不要 (PrivilegesRequired=lowest, ユーザーのローカルフォルダにインストール)
; - Windowsの「アプリと機能」から通常どおりアンインストールできる
; - 同じ AppId のまま再インストールすると自動的に上書き更新される(アップデートが簡単)

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish\ClipboardHistoryManager"
#endif

#define MyAppName "クリップボード履歴マネージャー"
#define MyAppPublisher "おのソフト"
#define MyAppURL "https://github.com/yusei-onodera-ac/OnoSoft"
#define MyAppExeName "ClipboardHistoryManager.exe"

[Setup]
; 同じシリーズの別アプリと衝突しないよう、このアプリ専用の固定GUID
AppId={{6F2C9B1E-6D3A-4E7B-9C1D-2F8E4A6B7C10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
VersionInfoVersion={#MyAppVersion}

; 管理者権限なしでユーザーのローカルフォルダにインストールする
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\OnoSoft\ClipboardHistoryManager
DisableProgramGroupPage=yes
DefaultGroupName=おのソフト

OutputDir=.
OutputBaseFilename=ClipboardHistoryManager-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} (おのソフト)
WizardStyle=modern
SetupLogging=yes

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにアイコンを作成する"; GroupDescription: "追加のアイコン:"; Flags: unchecked
Name: "startup"; Description: "Windows起動時に自動的に起動する"; GroupDescription: "スタートアップ:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName}を起動する"; Flags: nowait postinstall skipifsilent

[Code]
procedure KillRunningApp;
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/IM {#MyAppExeName} /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function InitializeSetup(): Boolean;
begin
  KillRunningApp;
  Result := True;
end;

function InitializeUninstall(): Boolean;
begin
  KillRunningApp;
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
  SettingsDir: String;
  Response: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{userappdata}\ClipboardHistoryManager');
    SettingsDir := ExpandConstant('{userappdata}\OnoSoft\ClipboardHistoryManager');
    if DirExists(DataDir) or DirExists(SettingsDir) then
    begin
      Response := MsgBox('クリップボード履歴と設定のデータも削除しますか?' + #13#10 +
        '(「いいえ」を選ぶと、再インストール時に前の履歴を引き継げます)',
        mbConfirmation, MB_YESNO);
      if Response = IDYES then
      begin
        DelTree(DataDir, True, True, True);
        DelTree(SettingsDir, True, True, True);
      end;
    end;
  end;
end;
