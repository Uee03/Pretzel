param(
    [Parameter(Mandatory = $true)]
    [string]$Destination,
    [string]$UnityVersion = "6000.6.5f1",
    [switch]$IncludeGeneratedExamples,
    [ValidateSet("PlainText", "Binary", "Encrypted")]
    [string]$SetupFormat = "Encrypted",
    [switch]$UseInstalledPackages
)

$ErrorActionPreference = "Stop"
$repository = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$destinationPath = [System.IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $destinationPath) {
    throw "Validation destination must not exist. This script never overwrites or deletes an existing project."
}

$packagesPath = Join-Path $destinationPath "Packages"
$assetsPath = Join-Path $destinationPath "Assets"
$settingsPath = Join-Path $destinationPath "ProjectSettings"
New-Item -ItemType Directory -Path $packagesPath, $assetsPath, $settingsPath | Out-Null

$packageNames = @()
foreach ($package in Get-ChildItem (Join-Path $repository "Packages") -Directory -Filter "com.uee03.pretzel.*") {
    Copy-Item -LiteralPath $package.FullName -Destination (Join-Path $packagesPath $package.Name) -Recurse
    $packageNames += $package.Name
}

$dependencies = [ordered]@{
    "com.unity.nuget.newtonsoft-json"   = "3.2.2"
    "com.unity.test-framework"          = "1.4.5"
    "com.unity.ugui"                    = "2.0.0"
    "com.unity.modules.audio"           = "1.0.0"
    "com.unity.modules.imgui"           = "1.0.0"
    "com.unity.modules.jsonserialize"   = "1.0.0"
    "com.unity.modules.physics"         = "1.0.0"
    "com.unity.modules.ui"              = "1.0.0"
    "com.unity.modules.uielements"      = "1.0.0"
    "com.unity.modules.unitywebrequest" = "1.0.0"
    "com.unity.modules.imageconversion" = "1.0.0"
}

if ($UseInstalledPackages) {
    $cache = Join-Path $repository "TestPretzel\Library\PackageCache"
    foreach ($name in @("com.unity.nuget.newtonsoft-json", "com.unity.test-framework", "com.unity.ext.nunit", "com.unity.ugui")) {
        $installed = @(Get-ChildItem $cache -Directory -Filter "$name@*")
        if ($installed.Count -ne 1) {
            throw "Expected exactly one installed $name package in $cache."
        }
        $dependencies[$name] = "file:" + $installed[0].FullName.Replace("\", "/")
    }
}

$manifest = [ordered]@{
    dependencies = $dependencies
    testables    = $packageNames
}
$utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Join-Path $packagesPath "manifest.json"), ($manifest | ConvertTo-Json -Depth 5), $utf8)
[System.IO.File]::WriteAllText((Join-Path $settingsPath "ProjectVersion.txt"), "m_EditorVersion: $UnityVersion`n", $utf8)

$dataPackage = Join-Path $packagesPath "com.uee03.pretzel.data"
Copy-Item -LiteralPath (Join-Path $dataPackage "Samples~\DataUsage") -Destination (Join-Path $assetsPath "DataUsage") -Recurse

if ($IncludeGeneratedExamples) {
    $examplesPath = Join-Path $assetsPath "GeneratedExamples"
    New-Item -ItemType Directory -Path $examplesPath | Out-Null
    foreach ($name in @("PretzelPlainTextExample", "PretzelBinaryExample", "PretzelAesExample", "ProjectSaveData", "ISaveService", "SaveService", "SaveController", "ProjectSaveManager", "PretzelEncryptionExample")) {
        Copy-Item -LiteralPath (Join-Path $dataPackage "Templates~\Examples\$name.cs.txt") -Destination (Join-Path $examplesPath "$name.cs")
    }
    $editorPath = Join-Path $examplesPath "Editor"
    New-Item -ItemType Directory -Path $editorPath | Out-Null
    Copy-Item -LiteralPath (Join-Path $dataPackage "Templates~\Examples\PretzelEncryptionExampleEditor.cs.txt") -Destination (Join-Path $editorPath "PretzelEncryptionExampleEditor.cs")
    if ($SetupFormat -eq "Encrypted") {
        $setup = [System.IO.File]::ReadAllText((Join-Path $dataPackage "Templates~\ProjectDataIO.Encrypted.cs.txt"))
        $setup = $setup.Replace("@@MODE_TEXT@@", "AES").Replace("@@MODE@@", "Aes").Replace("@@KEY_SOURCE@@", "Provider").Replace("@@USAGE@@", "Use DataIO.Save and check DataIO.Load results.")
    }
    else {
        $setup = [System.IO.File]::ReadAllText((Join-Path $dataPackage "Templates~\ProjectDataIO.Plain.cs.txt"))
        $setup = $setup.Replace("@@FORMAT@@", $SetupFormat)
    }
    if ($setup.Contains("@@")) {
        throw "Unresolved generated setup placeholders."
    }
    [System.IO.File]::WriteAllText((Join-Path $examplesPath "ProjectDataIO.cs"), $setup, $utf8)
}

Write-Output "Created isolated consumer host: $destinationPath"
Write-Output "Embedded $($packageNames.Count) first-party packages; Unity $UnityVersion."
