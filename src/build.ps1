param([string]$Output = (Join-Path $PSScriptRoot 'publish'))
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
dotnet publish (Join-Path $PSScriptRoot 'KeyFlow.csproj') -c Release -o $Output
if ($LASTEXITCODE -ne 0) { throw '构建失败，请检查上方日志。需要 .NET 10 SDK 和联网下载 NuGet 依赖。' }
Write-Output "已生成到 $Output"
