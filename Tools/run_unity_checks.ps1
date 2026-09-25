param([string]$Unity = 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force -Path (Join-Path $project 'Logs') | Out-Null
function Invoke-UnityCheck([string[]]$ExtraArguments) {
    $arguments = @('-batchmode', '-nographics', '-projectPath', ('"{0}"' -f $project)) + $ExtraArguments
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -WorkingDirectory $project -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "Unity exited with code $($process.ExitCode). See Logs/." }
}
Invoke-UnityCheck @('-quit', '-executeMethod', 'Palsoul.Editor.PrototypeBuilder.Build', '-logFile', 'Logs/setup.log')
Invoke-UnityCheck @('-runTests', '-testPlatform', 'EditMode', '-testResults', 'Logs/test-results.xml', '-logFile', 'Logs/tests.log')
Write-Output 'Scene generation and Unity tests completed. Results: Logs/test-results.xml'
