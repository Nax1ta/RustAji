$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compiler = Join-Path ${env:ProgramFiles(x86)} 'NSIS\makensis.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw '未找到 NSIS。请先安装 NSIS.NSIS。'
}

Push-Location $projectRoot
try {
    dotnet publish TutorialLibrary/TutorialLibrary.csproj -c Release -o publish
    if ($LASTEXITCODE -ne 0) { throw 'RustAji 构建失败。' }
    & $compiler 'installer/RustAji.nsi'
    if ($LASTEXITCODE -ne 0) { throw '安装包构建失败。' }
}
finally {
    Pop-Location
}
