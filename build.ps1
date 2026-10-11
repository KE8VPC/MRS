param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$destination = Join-Path $PSScriptRoot 'build\win-x64'
& $Dotnet publish (Join-Path $PSScriptRoot 'src\MRS.csproj') -c Release -r win-x64 --self-contained true -o $destination -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
$test = Start-Process (Join-Path $destination 'MRS.exe') -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -Wait
Get-Content (Join-Path $destination 'self-test.txt')
if ($test.ExitCode -ne 0) { throw 'Self-test failed.' }
Write-Output "Built and checked: $destination"
