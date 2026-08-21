# おのソフト (OnoSoft)

Windows向けの小さなユーティリティアプリをシリーズとして量産・配信するための
モノレポです。全アプリで共通のトレイ常駐・ホットキー・ダークテーマ・
設定永続化・更新チェックの仕組みを `OnoSoft.Shared` にまとめ、
新しいアプリを素早く立ち上げられるようにしています。

## 構成

```
OnoSoft.sln
src/
  OnoSoft.Shared/            全アプリ共通のライブラリ
  ClipboardHistoryManager/   シリーズ第1弾: クリップボード履歴マネージャー
templates/
  NewAppTemplate/            新しいアプリを始めるための雛形
installer/
  ClipboardHistoryManager.iss  管理者権限不要のインストーラー(Inno Setup)
docs/
  BRANDING.md                シリーズの命名規則・配色・アイコン方針
  RELEASE_PROCESS.md         バージョニング・GitHub Releasesでの配布手順(インストーラー含む)
site/
  index.html                 配布サイト(GitHub Pages想定)の雛形
.github/workflows/release.yml  タグ push で自動ビルド・GitHub Release作成(インストーラーも)
```

## アプリ一覧

| アプリ | 概要 | ステータス |
|---|---|---|
| [クリップボード履歴マネージャー](src/ClipboardHistoryManager) | コピー履歴をタスクトレイから呼び出せる常駐アプリ。テーマ設定・インストーラー対応済み | 動作確認済み (v1.2.0) |

## ビルド

.NET 8 SDK が必要です(このマシンでは管理者権限が使えなかったため `C:\dotnet` に
ユーザースコープでインストール済み)。

```powershell
$env:DOTNET_ROOT = "C:\dotnet"; $env:PATH = "C:\dotnet;$env:PATH"
dotnet build
```

## 新しいアプリを追加する

[templates/NewAppTemplate/README.md](templates/NewAppTemplate/README.md) を参照してください。

## 公開の流れ

1. アプリを `src/` に追加し、動作確認する
2. [docs/BRANDING.md](docs/BRANDING.md) に沿って名前・アイコン・配色を決める
3. [docs/RELEASE_PROCESS.md](docs/RELEASE_PROCESS.md) の手順でタグを打ち、
   GitHub Actionsで自動ビルド・リリースする
4. `site/index.html` にダウンロードリンクを追記し、GitHub Pages等で公開する

## 未着手・今後決めること

- GitHub アカウント/組織の確定とリポジトリ作成(現時点ではローカルのみ)
- 配布サイトの実際のホスティング先(GitHub Pages / 独自ドメイン)
- コード署名の要否(現状は未署名 → SmartScreen警告あり、`docs/RELEASE_PROCESS.md` 参照)
