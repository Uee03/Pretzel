<#
.SYNOPSIS
Regenerates the Setup window's module catalog from the first-party package manifests.

.DESCRIPTION
Reads every Packages/com.uee03.pretzel.*/package.json (except setup itself) and writes
Packages/com.uee03.pretzel.setup/Editor/Catalog/modules.json. Pretzel dependencies, required third-party
packages (Unity built-in modules are skipped) and descriptions come from package.json. Optional libraries come
from runtime asmdefs whose versionDefines symbol is also a defineConstraint, so the assembly only compiles when
the library is installed. Run it after changing a manifest, a dependency or an integration asmdef.

.PARAMETER Check
Fails when the committed catalog differs from the generated one, without writing it. Use before tagging a release.
#>
param(
    [switch]$Check
)

$ErrorActionPreference = "Stop"
$repository = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$packagesRoot = Join-Path $repository "Packages"
$prefix = "com.uee03.pretzel."
$setupName = $prefix + "setup"
$output = Join-Path $packagesRoot "$setupName\Editor\Catalog\modules.json"

# Order of the Addons tab: the base layer first, optional extras last. Unlisted modules follow alphabetically.
$order = @("core", "extensions", "data", "tween", "ui", "ui.ugui", "api", "events", "pooling", "scenes",
    "audio", "preferences", "devconsole", "templates", "integrations", "editor")

function ConvertTo-JsonString([string]$value) {
    $builder = New-Object System.Text.StringBuilder
    [void]$builder.Append('"')
    foreach ($c in $value.ToCharArray()) {
        switch ($c) {
            '"' { [void]$builder.Append('\"') }
            '\' { [void]$builder.Append('\\') }
            "`n" { [void]$builder.Append('\n') }
            "`r" { [void]$builder.Append('\r') }
            "`t" { [void]$builder.Append('\t') }
            default {
                if ([int]$c -lt 0x20) { [void]$builder.AppendFormat('\u{0:x4}', [int]$c) }
                else { [void]$builder.Append($c) }
            }
        }
    }
    [void]$builder.Append('"')
    return $builder.ToString()
}

function ConvertTo-JsonArray([string[]]$values) {
    if ($values.Count -eq 0) { return "[]" }
    return "[" + (($values | ForEach-Object { ConvertTo-JsonString $_ }) -join ", ") + "]"
}

$modules = @()
foreach ($directory in Get-ChildItem $packagesRoot -Directory -Filter "$prefix*") {
    if ($directory.Name -eq $setupName) { continue }

    $manifestPath = Join-Path $directory.FullName "package.json"
    $manifest = [System.IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
    if ($manifest.name -ne $directory.Name) {
        throw "$manifestPath declares '$($manifest.name)' but lives in '$($directory.Name)'."
    }

    $dependencies = @()
    $packages = @()
    if ($manifest.dependencies) {
        foreach ($dependency in $manifest.dependencies.PSObject.Properties) {
            if ($dependency.Name.StartsWith($prefix)) { $dependencies += $dependency.Name }
            elseif (-not $dependency.Name.StartsWith("com.unity.modules.")) { $packages += $dependency.Name }
        }
    }

    $optional = @()
    $runtime = Join-Path $directory.FullName "Runtime"
    if (Test-Path $runtime) {
        foreach ($asmdefFile in Get-ChildItem $runtime -Recurse -Filter "*.asmdef" | Sort-Object FullName) {
            $asmdef = [System.IO.File]::ReadAllText($asmdefFile.FullName) | ConvertFrom-Json
            foreach ($versionDefine in @($asmdef.versionDefines)) {
                if ($versionDefine -and @($asmdef.defineConstraints) -contains $versionDefine.define) {
                    $optional += [pscustomobject]@{ DisplayName = $asmdefFile.Directory.Name; PackageName = $versionDefine.name }
                }
            }
        }
    }

    $shortName = $directory.Name.Substring($prefix.Length)
    $rank = [Array]::IndexOf($order, $shortName)
    if ($rank -lt 0) {
        Write-Warning "$shortName is not in the display order list; it is listed after the known modules."
        $rank = 1000
    }

    $modules += [pscustomobject]@{
        Rank         = $rank
        Name         = $manifest.name
        DisplayName  = ($manifest.displayName -replace '^Pretzel ', '')
        Description  = $manifest.description
        Version      = $manifest.version
        Dependencies = $dependencies
        Packages     = $packages
        Optional     = $optional
    }
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("{")
$lines.Add('  "generatedBy": ".github/scripts/Update-ModuleCatalog.ps1",')
$lines.Add('  "modules": [')
$sorted = @($modules | Sort-Object Rank, Name)
for ($i = 0; $i -lt $sorted.Count; $i++) {
    $module = $sorted[$i]
    $optionalJson = "[]"
    if ($module.Optional.Count -gt 0) {
        $optionalJson = "[" + (($module.Optional | ForEach-Object {
                    '{ "displayName": ' + (ConvertTo-JsonString $_.DisplayName) + ', "packageName": ' + (ConvertTo-JsonString $_.PackageName) + ' }'
                }) -join ", ") + "]"
    }

    $lines.Add("    {")
    $lines.Add('      "name": ' + (ConvertTo-JsonString $module.Name) + ",")
    $lines.Add('      "displayName": ' + (ConvertTo-JsonString $module.DisplayName) + ",")
    $lines.Add('      "description": ' + (ConvertTo-JsonString $module.Description) + ",")
    $lines.Add('      "version": ' + (ConvertTo-JsonString $module.Version) + ",")
    $lines.Add('      "dependencies": ' + (ConvertTo-JsonArray $module.Dependencies) + ",")
    $lines.Add('      "packages": ' + (ConvertTo-JsonArray $module.Packages) + ",")
    $lines.Add('      "optionalLibraries": ' + $optionalJson)
    $lines.Add("    }" + $(if ($i -lt $sorted.Count - 1) { "," } else { "" }))
}
$lines.Add("  ]")
$lines.Add("}")
$generated = ($lines -join "`n") + "`n"

$existing = if (Test-Path $output) { [System.IO.File]::ReadAllText($output) } else { $null }
if ($Check) {
    if ($existing -ne $generated) {
        throw "modules.json is out of date. Run .github/scripts/Update-ModuleCatalog.ps1 and commit the result."
    }
    Write-Output "modules.json is up to date ($($sorted.Count) modules)."
    return
}

New-Item -ItemType Directory -Force -Path (Split-Path $output) | Out-Null
[System.IO.File]::WriteAllText($output, $generated, (New-Object System.Text.UTF8Encoding($false)))
Write-Output "Wrote $($sorted.Count) modules to $output"
