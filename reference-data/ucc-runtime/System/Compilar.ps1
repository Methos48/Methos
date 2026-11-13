$ErrorActionPreference = 'Stop'
$systemDir = Join-Path $PSScriptRoot 'Interlude\System'
$output = Join-Path $systemDir 'Interface.u'
$exitCode = 1
Push-Location $systemDir
try {
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Force }
    $console = @('N' | & '.\ucc.exe' make -NoBind 2>&1)
    $compilerExit = $LASTEXITCODE
    $console | ForEach-Object { Write-Host $_ }
    if ($compilerExit -ne 0 -or !(Test-Path -LiteralPath $output) -or ($console -join "`n") -notmatch 'Success - 0 error\(s\)') {
        throw 'A compilacao falhou. A ultima versao em Testar foi preservada; veja o erro acima.'
    }
    & (Join-Path $PSScriptRoot 'ValidatePackage.ps1') -PackagePath $output
    $testDir = Join-Path $PSScriptRoot 'Testar\system'
    New-Item -ItemType Directory -Path $testDir -Force | Out-Null
    Copy-Item -LiteralPath $output -Destination (Join-Path $testDir 'Interface.u') -Force
    $layoutDir = Join-Path $PSScriptRoot 'Layout'
    if (Test-Path -LiteralPath $layoutDir) {
        Get-ChildItem -LiteralPath $layoutDir -File | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $testDir -Force
        }
    }
    Write-Host "SUCESSO: $(Join-Path $testDir 'Interface.u')" -ForegroundColor Green
    $exitCode = 0
} catch {
    Write-Host $_ -ForegroundColor Red
} finally {
    foreach ($file in @('ucc.log','UIScript.log','Interface.u')) {
        $temporary = Join-Path $systemDir $file
        if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
    }
    Pop-Location
}
exit $exitCode
