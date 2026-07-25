# CI build script: publish the WPF app (framework-dependent, win-x64) and zip it.
# NO upload here -- release.yml creates/updates the GitHub Release and attaches the zip.
# Resolves the repo root from this script's location, so it can be invoked from anywhere.
$ErrorActionPreference = 'Stop'
# Quiet the .NET first-run experience/telemetry so freshly installed tools do not
# print a banner to stdout that would contaminate captured command output.
$env:DOTNET_NOLOGO = 'true'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $root

$proj       = Join-Path $root 'src\ProxyRouterWpf\ProxyRouterWpf.csproj'
$artifacts  = Join-Path $root 'artifacts'
$publishDir = Join-Path $artifacts 'publish\win-x64'

Remove-Item -Recurse -Force $artifacts -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $publishDir | Out-Null

# Framework-dependent build: a few MB, requires the .NET Desktop Runtime on the target machine.
dotnet publish $proj -c Release -r win-x64 --self-contained false -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# The version already came from GitVersion during the build (Major.Minor.<commits-since-tag>);
# read it back off the produced assembly instead of querying git a second time.
$dll = Join-Path $publishDir 'ProxyRouterWpf.dll'
if (-not (Test-Path $dll)) { throw "publish output not found: $dll" }
$ver = ("$([Diagnostics.FileVersionInfo]::GetVersionInfo($dll).ProductVersion)" -split '\+')[0]
if ($ver -notmatch '^\d+\.\d+\.\d+$') { throw "unexpected product version '$ver' (did GitVersion run?)" }
Write-Host "Version: $ver"

$zip = Join-Path $artifacts "ProxyRouterWpf-$ver-win-x64.zip"
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zip -Force
Write-Host "Packed: $([IO.Path]::GetFileName($zip))"
