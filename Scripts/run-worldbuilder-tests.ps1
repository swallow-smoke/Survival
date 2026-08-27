# WorldBuilder CI helper: compile check + EditMode tests for the WorldBuilder package.
#
# Usage (from the project root):
#   powershell -File Scripts\run-worldbuilder-tests.ps1 [-UnityPath "D:\path\Unity.exe"] [-Filter "WorldBuilder.Tests"]
#
# Exit codes follow Unity's test runner: 0 = all green, 2 = test failures.

param(
    [string]$UnityPath = "D:\unityEditor\6000.4.2f1\Editor\Unity.exe",
    [string]$ProjectPath = $PSScriptRoot + "\..",
    [string]$Filter = "WorldBuilder.Tests",
    [switch]$CompileOnly
)

$ErrorActionPreference = "Stop"
$UnityPath = (Resolve-Path $UnityPath).Path
$ProjectPath = (Resolve-Path $ProjectPath).Path
$outDir = Join-Path $env:TEMP "worldbuilder-ci"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

if ($CompileOnly) {
    Write-Host "== Compile check =="
    & $UnityPath -batchmode -quit -nographics -projectPath $ProjectPath `
        -logFile "$outDir\compile.log" | Out-Null
    $code = $LASTEXITCODE
    if ($code -eq 0) { Write-Host "OK: compilation succeeded." }
    else {
        Write-Host "FAILED (exit $code). Errors:"
        Select-String -LiteralPath "$outDir\compile.log" -Pattern "error CS" |
            Select-Object -First 30 | ForEach-Object { $_.Line }
    }
    exit $code
}

Write-Host "== EditMode tests (filter: $Filter) =="
$results = Join-Path $outDir "results.xml"
& $UnityPath -batchmode -nographics -projectPath $ProjectPath `
    -runTests -testPlatform EditMode -testFilter $Filter `
    -testResults $results -logFile "$outDir\tests.log" | Out-Null
$code = $LASTEXITCODE

if (Test-Path $results) {
    [xml]$xml = Get-Content $results -Raw
    $run = $xml.'test-run'
    Write-Host ("total={0} passed={1} failed={2} skipped={3}" -f `
        $run.total, $run.passed, $run.failed, $run.skipped)
    if ([int]$run.failed -gt 0) {
        $xml.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
            Write-Host "  FAILED: $($_.fullname)"
        }
    }
}
else {
    Write-Warning "No results file produced. See $($outDir)\tests.log"
}

exit $code
