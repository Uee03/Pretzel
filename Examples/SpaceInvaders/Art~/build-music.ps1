$snd = 'C:\Program Files\SoundCraft\soundcraft-cli.exe'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $here '..\Assets\_Project\Audio\Music'
$tail = 2.5
New-Item -ItemType Directory $target, (Join-Path $here 'source'), (Join-Path $here 'raw') -Force | Out-Null

node (Join-Path $here 'music.js') | Out-Null
$tunes = Get-Content (Join-Path $here 'scripts\music.json') -Raw | ConvertFrom-Json

foreach ($tune in $tunes) {
    $name = $tune.name
    $raw = Join-Path $here "raw\$name.wav"
    $flat = Join-Path $here "raw\$name.16.wav"
    $end = $tune.loopSeconds + $tail
    $bounce = & $snd script (Join-Path $here "scripts\$name.txt") --bounce $raw --start 0 --end $end --save (Join-Path $here "source\$name.scraft") 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host "$name BOUNCE FAILED: $bounce"; continue }
    $convert = & $snd convert $raw $flat --bit-depth 16 --rate 44100 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host "$name CONVERT FAILED: $convert"; continue }
    node (Join-Path $here 'loopwrap.js') $flat $tune.loopSeconds (Join-Path $target "$name.wav")
    Write-Host ("{0}: {1}" -f $name, (& $snd info (Join-Path $target "$name.wav") --compact 2>&1 | Out-String).Trim())
}

Remove-Item (Join-Path $here 'raw') -Recurse -Force
