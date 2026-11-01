$aseprite = 'D:\Program Files (x86)\Steam\steamapps\common\Aseprite\Aseprite.exe'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$assets = Join-Path $here '..\Assets\_Project\Art'
$frames = Join-Path $assets 'Effects\Explosion'
$stars = Join-Path $assets 'Stars'
$source = Join-Path $here 'source'
foreach ($d in $frames, $stars, $source) { New-Item -ItemType Directory $d -Force | Out-Null }

function Invoke-Aseprite($script, [string[]]$params) {
    $arguments = @('-b')
    foreach ($p in $params) { $arguments += @('--script-param', $p) }
    $arguments += @('--script', (Join-Path $here $script))
    $output = & $aseprite @arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host "$script FAILED: $output" } else { Write-Host "$script ok $($output.Trim())" }
}

Invoke-Aseprite 'explosion.lua' @("out=$((Resolve-Path $frames).Path)", "source=$((Resolve-Path $source).Path)\explosion.aseprite")
Invoke-Aseprite 'stars.lua' @("out=$((Resolve-Path $stars).Path)", "source=$((Resolve-Path $source).Path)")
