param(
    [string]$CompilerPath,
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$projectFile = Join-Path $projectRoot 'Dayline.csproj'
$appVersion = (Select-Xml -LiteralPath $projectFile -XPath '/Project/PropertyGroup/Version').Node.InnerText
$publishDir = Join-Path $projectRoot 'artifacts\installer\win-x64'
if (-not $OutputDir) { $OutputDir = Join-Path $projectRoot 'artifacts\installer' }

if (-not $CompilerPath) {
    $compilerCandidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 7\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe"
    )
    $CompilerPath = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $CompilerPath -or -not (Test-Path -LiteralPath $CompilerPath)) {
    throw 'Install Inno Setup 6 or 7, or pass -CompilerPath with the full path to ISCC.exe.'
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
dotnet publish $projectFile -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o $publishDir
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $publishDir '使用说明.md')
Copy-Item -LiteralPath (Join-Path $projectRoot 'CHANGELOG.md') -Destination $publishDir

& $CompilerPath "/DAppVersion=$appVersion" "/DPublishDir=$publishDir" "/O$OutputDir" (Join-Path $PSScriptRoot 'Dayline.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
Get-Item -LiteralPath (Join-Path $OutputDir "Dayline-$appVersion-Setup-x64.exe")
