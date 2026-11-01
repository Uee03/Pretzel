$snd = 'C:\Program Files\SoundCraft\soundcraft-cli.exe'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $here '..\Assets\_Project\Audio\SFX'
$pack = Join-Path $here '..\..\..\Packages\com.uee03.pretzel.sfx\Samples~\SfxPack\Audio'
New-Item -ItemType Directory $target, (Join-Path $here 'source'), (Join-Path $here 'raw') -Force | Out-Null

# The sounds the game maps from the SFX pack.
foreach ($name in 'laser', 'hit_light', 'explosion', 'power_up', 'stinger_win', 'stinger_lose') {
    Copy-Item (Join-Path $pack "$name.wav") (Join-Path $target "$name.wav") -Force
    Write-Host "copied $name"
}

# The march notes are made here with SoundCraft.
node (Join-Path $here 'march.js')
foreach ($i in 1..4) {
    $name = "march_$i"
    $raw = Join-Path $here "raw\$name.wav"
    $wav = Join-Path $target "$name.wav"
    $bounce = & $snd script (Join-Path $here "scripts\$name.txt") --bounce $raw --start 0 --end 0.3 --save (Join-Path $here "source\$name.scraft") 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host "$name BOUNCE FAILED: $bounce"; continue }
    $convert = & $snd convert $raw $wav --bit-depth 16 --rate 44100 --normalize 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host "$name CONVERT FAILED: $convert"; continue }
    Write-Host ("{0}: {1}" -f $name, (& $snd info $wav --compact 2>&1 | Out-String).Trim())
}

Remove-Item (Join-Path $here 'raw') -Recurse -Force
