$vec = 'C:\Program Files\VectorCraft\vectorcraft-cli.exe'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $here '..\Samples~\CameraExample\Art'
node (Join-Path $here 'generate.js')
New-Item -ItemType Directory $target -Force | Out-Null
New-Item -ItemType Directory (Join-Path $here 'source') -Force | Out-Null

foreach ($name in 'arena', 'orb') {
    $script = Join-Path $here "scripts\$name.json"
    $source = Join-Path $here "source\$name.vectorcraft"
    $png = Join-Path $target "$name.png"
    $output = & $vec run --commands $script --export $source --export $png 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        Write-Host "$name FAILED: $output"
    }
    else {
        Write-Host "$name ok"
    }
}
