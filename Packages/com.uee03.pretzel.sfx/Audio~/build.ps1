$snd = 'C:\Program Files\SoundCraft\soundcraft-cli.exe'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $here '..\Samples~\SfxPack\Audio'
node (Join-Path $here 'generate.js')
New-Item -ItemType Directory $target -Force | Out-Null
New-Item -ItemType Directory (Join-Path $here 'source') -Force | Out-Null
New-Item -ItemType Directory (Join-Path $here 'raw') -Force | Out-Null

$manifest = Get-Content (Join-Path $here 'scripts\manifest.json') -Raw | ConvertFrom-Json
foreach ($item in $manifest) {
    $name = $item.name
    $script = Join-Path $here "scripts\$name.txt"
    $raw = Join-Path $here "raw\$name.wav"
    $session = Join-Path $here "source\$name.scraft"
    $wav = Join-Path $target "$name.wav"
    $bounce = & $snd script $script --bounce $raw --start 0 --end $item.seconds --save $session 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host "$name BOUNCE FAILED: $bounce"; continue }
    $convert = & $snd convert $raw $wav --bit-depth 16 --rate 44100 --normalize 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host "$name CONVERT FAILED: $convert"; continue }
    $info = & $snd info $wav --compact 2>&1 | Out-String
    Write-Host ("{0}: {1}" -f $name, $info.Trim())
}
