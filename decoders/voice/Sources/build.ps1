param([string]$Toolchain)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$objects=Join-Path $root 'objects'
New-Item -ItemType Directory -Force $objects | Out-Null
$env:PATH="$Toolchain;$env:PATH"
foreach($c in Get-ChildItem "$root/mbelib" -Filter '*.c'){
 & "$Toolchain/gcc.exe" -O2 -c $c.FullName -I "$root/mbelib" -o "$objects/$($c.BaseName).o"
 if($LASTEXITCODE){throw 'C compilation failed'}
}
& "$Toolchain/g++.exe" -O2 -std=c++11 -shared -static -static-libgcc -static-libstdc++ "$root/mrs_voice.cpp" "$root/dsd/src/p25p1_check_hdu.cpp" "$root/dsd/src/p25p1_check_ldu.cpp" "$root/dsd/src/Hamming.cpp" -I "$root/mbelib" -I "$root/dsd/include" @((Get-ChildItem $objects -Filter '*.o').FullName) -o "$root/../mrs_voice.dll"
if($LASTEXITCODE){throw 'Voice bridge link failed'}
