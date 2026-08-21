# ClipboardHistoryManager を起動するための便利スクリプト。
# 開発環境の .NET SDK が C:\dotnet にユーザースコープでインストールされているため、
# 標準の `dotnet` コマンドが使えるようにパスを一時的に通してから実行/ビルドする。

$env:DOTNET_ROOT = "C:\dotnet"
$env:PATH = "C:\dotnet;$env:PATH"

Set-Location $PSScriptRoot

if ($args.Count -gt 0 -and $args[0] -eq "build") {
    & "C:\dotnet\dotnet.exe" build
}
elseif ($args.Count -gt 0 -and $args[0] -eq "run") {
    & "C:\dotnet\dotnet.exe" run
}
else {
    & "C:\dotnet\dotnet.exe" build -c Release
    Start-Process -FilePath ".\bin\Release\net8.0-windows\ClipboardHistoryManager.exe"
}
