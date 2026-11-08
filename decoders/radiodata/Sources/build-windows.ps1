param([string]$ToolchainBin='C:\msys64\mingw64\bin',[string]$OutputDirectory='')
$ErrorActionPreference='Stop'
$prefix=Split-Path $ToolchainBin -Parent
$env:PATH=$ToolchainBin+';'+$env:PATH
$gcc=Join-Path $ToolchainBin 'gcc.exe'
$gpp=Join-Path $ToolchainBin 'g++.exe'
$out=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Split-Path $PSScriptRoot -Parent}
New-Item -ItemType Directory -Path $out -Force | Out-Null
$common=@('-O2','-static','-Wl,--stack,16777216')
function Compile([string]$Compiler,[string[]]$Arguments){& $Compiler @Arguments;if($LASTEXITCODE -ne 0){throw 'Native decoder compilation failed'}}
Compile $gcc ($common+@((Join-Path $PSScriptRoot 'mrs_ani.c'),(Join-Path $PSScriptRoot 'ani/mdc_decode.c'),(Join-Path $PSScriptRoot 'ani/fsync_decode.c'),'-lm','-o',(Join-Path $out 'mrs-ani.exe')))
$files=@((Join-Path $PSScriptRoot 'mrs_weak.c'))
$files+=@(Get-ChildItem (Join-Path $PSScriptRoot 'ft8/ft8') -Filter '*.c' | Sort-Object Name | ForEach-Object FullName)
$files+=Join-Path $PSScriptRoot 'ft8/common/monitor.c'
$files+=@(Get-ChildItem (Join-Path $PSScriptRoot 'ft8/fft') -Filter '*.c' | Sort-Object Name | ForEach-Object FullName)
foreach($name in @('wsprd.c','wsprsim_utils.c','wsprd_utils.c','tab.c','fano.c','nhash.c')){$files+=Join-Path $PSScriptRoot ('wspr/wsprd/'+$name)}
$includeFt='-I'+(Join-Path $PSScriptRoot 'ft8')
$includePrefix='-I'+(Join-Path $prefix 'include')
$libraryPrefix='-L'+(Join-Path $prefix 'lib')
Compile $gcc ($common+@($includeFt,$includePrefix)+$files+@($libraryPrefix,'-lfftw3f','-lm','-o',(Join-Path $out 'mrs-weak.exe')))
Compile $gpp ($common+@('-std=c++17','-DLORA_LITE_MAX_SF=12','-DLORA_LITE_MAX_PAYLOAD=255',(Join-Path $PSScriptRoot 'mrs_lora.cpp'),'-o',(Join-Path $out 'mrs-lora.exe')))
