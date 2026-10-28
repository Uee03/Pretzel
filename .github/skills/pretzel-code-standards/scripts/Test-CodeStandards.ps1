<#
.SYNOPSIS
    Checks Pretzel C# sources against the NET-PROJECT-SETUP coding conventions, adapted for Unity.
.DESCRIPTION
    Static, line-based checks. Errors must be fixed; review items need a human decision (LINQ, structs).
    Exit code 1 when an error is found, so the script works as a pre-commit hook.
.EXAMPLE
    .\.github\skills\pretzel-code-standards\scripts\Test-CodeStandards.ps1
.EXAMPLE
    .\.github\skills\pretzel-code-standards\scripts\Test-CodeStandards.ps1 -Path Packages/com.uee03.pretzel.scenes -ShowReview
#>
[CmdletBinding()]
param(
    [string[]] $Path = @('Packages'),
    [switch] $ShowReview,
    [switch] $Staged
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path

$files = @()
if ($Staged) {
    $files = git -C $root diff --cached --name-only --diff-filter=ACMR |
    Where-Object { $_ -match '\.cs(\.txt)?$' -and $_ -like 'Packages/*' } |
    ForEach-Object { Join-Path $root $_ }
}
else {
    foreach ($p in $Path) {
        $full = if ([IO.Path]::IsPathRooted($p)) { $p } else { Join-Path $root $p }
        $files += Get-ChildItem -LiteralPath $full -Recurse -File -Force |
        Where-Object { $_.Name -like '*.cs' -or $_.Name -like '*.cs.txt' } |
        ForEach-Object FullName
    }
}

$errors = New-Object System.Collections.Generic.List[string]
$review = New-Object System.Collections.Generic.List[string]

function Add-Finding([System.Collections.Generic.List[string]] $list, [string] $file, [int] $line, [string] $rule, [string] $text) {
    $relative = $file.Substring($root.Length + 1).Replace('\', '/')
    $list.Add("${relative}:${line}: [$rule] $($text.Trim())")
}

# Removes string/char literal contents and line comments so code patterns are not matched inside them.
function Get-CodeOnly([string] $line) {
    $code = [regex]::Replace($line, '@"(?:[^"]|"")*"|\$?"(?:[^"\\]|\\.)*"', '""')
    $code = [regex]::Replace($code, "'(?:[^'\\]|\\.)'", "''")
    $comment = $code.IndexOf('//')
    if ($comment -ge 0) { $code = $code.Substring(0, $comment) }
    return $code
}

$controlHeader = '^\s*(?:else\s+)?(if|for|foreach|while|lock|fixed|using)\s*\('
$asyncReturn = '(?:Task|ValueTask|Awaitable|UniTask|UniTaskVoid)(?:<[^()]*>)?'
$unityMessages = '^(Awake|Start|OnEnable|OnDisable|OnDestroy|Update|FixedUpdate|LateUpdate|OnApplicationQuit|OnApplicationPause|OnApplicationFocus)$'

foreach ($file in $files) {
    $lines = [IO.File]::ReadAllLines($file)
    $code = @($lines | ForEach-Object { Get-CodeOnly $_ })
    $inBlockComment = $false
    $publicTypes = New-Object System.Collections.Generic.List[string]
    # Top-level types sit one indent inside a block namespace, or at column 0 without one.
    $topIndent = if (@($lines | Where-Object { $_ -match '^namespace\s' }).Count -gt 0) { ' {4}' } else { '' }

    for ($i = 0; $i -lt $lines.Length; $i++) {
        $raw = $lines[$i]
        $c = $code[$i]
        $n = $i + 1

        if ($inBlockComment) {
            if ($raw -match '\*/') { $inBlockComment = $false }
            continue
        }
        if ($c -match '^\s*/\*' -and $c -notmatch '\*/') { $inBlockComment = $true; continue }
        if ($c -match '^\s*(\*|#)') { continue }

        # 1.6 Explicit types over var.
        if ($c -match '(^|[\s(,])var\s+[A-Za-z_(]') {
            Add-Finding $errors $file $n 'explicit-type' $raw
        }

        # 1.6 Braces on all control blocks.
        if ($c -match $controlHeader -and $c -notmatch '^\s*using\s*\(\s*\w[\w.<>]*\s*=') {
            $depth = 0
            $j = $i
            $rest = $null
            while ($j -lt $lines.Length) {
                $segment = $code[$j]
                $start = if ($j -eq $i) { $segment.IndexOf('(') } else { 0 }
                for ($k = $start; $k -lt $segment.Length; $k++) {
                    if ($segment[$k] -eq '(') { $depth++ }
                    elseif ($segment[$k] -eq ')') {
                        $depth--
                        if ($depth -eq 0) { $rest = $segment.Substring($k + 1).Trim(); break }
                    }
                }
                if ($null -ne $rest) { break }
                $j++
            }
            if ($null -ne $rest) {
                $isBraceless = $false
                if ($rest -ne '' -and $rest -ne '{' -and -not $rest.StartsWith('{') -and $rest -ne ';') {
                    $isBraceless = $true
                }
                elseif ($rest -eq '') {
                    $next = $j + 1
                    while ($next -lt $lines.Length -and $code[$next].Trim() -eq '') { $next++ }
                    if ($next -lt $lines.Length) {
                        $nextCode = $code[$next].Trim()
                        $isStackedUsing = $Matches[1] -eq 'using' -and $nextCode -match '^using\s*\('
                        if (-not $nextCode.StartsWith('{') -and -not $isStackedUsing -and -not $nextCode.StartsWith('&&') -and -not $nextCode.StartsWith('||')) {
                            $isBraceless = $true
                        }
                    }
                }
                if ($isBraceless) { Add-Finding $errors $file $n 'braces' $raw }
            }
        }
        if ($c -match '^\s*else\s*$') {
            $next = $i + 1
            while ($next -lt $lines.Length -and $code[$next].Trim() -eq '') { $next++ }
            if ($next -lt $lines.Length -and -not $code[$next].Trim().StartsWith('{')) {
                Add-Finding $errors $file $n 'braces' $raw
            }
        }
        elseif ($c -match '^\s*(\}\s*)?else\s+(?!if\b)[^{\s]') {
            Add-Finding $errors $file $n 'braces' $raw
        }

        # 1.6 Explicit string comparison semantics.
        if ($raw -match '(==|!=)\s*@?\$?"' -or $raw -match '"\s*(==|!=)') {
            Add-Finding $errors $file $n 'string-comparison' $raw
        }
        if ($c -match '\.(StartsWith|EndsWith)\((?!\s*'')' -and $raw -notmatch 'StringComparison') {
            Add-Finding $errors $file $n 'string-comparison' $raw
        }
        if ($c -match '\b[Ss]tring\.Compare\(' -and $raw -notmatch 'StringComparison') {
            Add-Finding $errors $file $n 'string-comparison' $raw
        }
        if ($c -match 'new\s+(Dictionary|HashSet|SortedDictionary|ConcurrentDictionary)<string\b' -and $raw -notmatch 'StringComparer') {
            Add-Finding $errors $file $n 'string-comparer' $raw
        }
        if ($c -match '\.(ToLower|ToUpper)\(\)') {
            Add-Finding $errors $file $n 'string-comparison' $raw
        }

        # 1.6 Async suffix.
        if ($c -match "^\s*(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed|new|async|extern)\s+)*$asyncReturn\s+(\w+)\s*(?:<[^>]*>)?\s*\(") {
            $name = $Matches[1]
            if (-not $name.EndsWith('Async') -and $name -notmatch $unityMessages) {
                Add-Finding $errors $file $n 'async-suffix' $raw
            }
        }

        # 1.7 No Console output for diagnostics.
        if ($c -match '\bConsole\.(Write|WriteLine)\(') {
            Add-Finding $errors $file $n 'logging' $raw
        }

        # 1.6 Read-only collections on public APIs.
        if ($c -match '^\s*public\s+(?:(?:static|virtual|override|abstract|new)\s+)*(?:List|Dictionary|HashSet|Queue|Stack)<.*>\s+\w+\s*(\{|=>|\(|<\w)') {
            Add-Finding $errors $file $n 'readonly-collection' $raw
        }

        # 1.6 One public type per file, named like the file.
        if ($c -match "^$topIndent(?:\[[^\]]*\]\s*)*public\s+(?:(?:static|sealed|abstract|partial|readonly|ref|unsafe)\s+)*(class|struct|interface|enum|record)\s+(\w+)") {
            $publicTypes.Add($Matches[2])
        }
        elseif ($c -match "^$topIndent" + 'public\s+delegate\s+.+?\s(\w+)\s*(?:<[^>]*>)?\s*\(') {
            $publicTypes.Add($Matches[1])
        }

        # Naming: private and static fields _camelCase, constant fields PascalCase. Public serialized fields keep
        # their names, because Unity and JsonUtility use them as serialized keys.
        $fieldPattern = '^\s*(?:\[[^\]]*\]\s*)*((?:(?:private|protected|internal|public|static|readonly|volatile)\s+)+)[\w<>\[\],.?]+(?:<[^=;()]*>)?\s+(\w+)\s*(?:=(?!>)|;)'
        if ($c -notmatch '\bconst\b' -and $c -match $fieldPattern) {
            $modifiers = $Matches[1]
            $fieldName = $Matches[2]
            $isPrivate = $modifiers -match '\bprivate\b' -or ($modifiers -notmatch '\b(public|protected|internal)\b')
            if ($isPrivate -and $fieldName -cnotmatch '^_[a-z][A-Za-z0-9]*$') {
                Add-Finding $errors $file $n 'field-name' $raw
            }
        }
        $isConstField = $c -match '^\s*(?:(?:private|protected|internal|public|new)\s+)+const\s' -or $c -match '^\s{0,8}const\s'
        if ($isConstField -and $c -match '\bconst\s+[\w<>\[\],.?]+\s+(\w+)\s*=' -and $Matches[1] -cnotmatch '^[A-Z][A-Za-z0-9]*$') {
            Add-Finding $errors $file $n 'const-name' $raw
        }

        # 1.6 Classes by default. A struct needs a "// Value type: <reason>" line right above it.
        if ($c -match '\bstruct\s+\w+' -and $c -notmatch 'where\s+\w+\s*:\s*struct') {
            $justified = $false
            for ($k = [Math]::Max(0, $i - 3); $k -lt $i; $k++) {
                if ($lines[$k] -match '^\s*//\s*Value type:\s*\S') { $justified = $true }
            }
            if (-not $justified) { Add-Finding $errors $file $n 'struct' $raw }
        }

        # 1.6 LINQ only where it is clearer. Editor and test code is cold; runtime LINQ needs a look at the hot path.
        if ($c -match '^\s*using\s+System\.Linq\s*;' -and $file -notmatch '[\\/](Editor|Tests)[\\/]') {
            Add-Finding $review $file $n 'linq' $raw
        }
    }

    $baseName = [IO.Path]::GetFileName($file) -replace '\.cs(\.txt)?$', ''
    if ($publicTypes.Count -gt 1) {
        Add-Finding $errors $file 1 'one-type-per-file' ("public types: " + ($publicTypes -join ', '))
    }
    elseif ($publicTypes.Count -eq 1 -and $publicTypes[0] -ne $baseName -and -not $baseName.StartsWith($publicTypes[0] + '.')) {
        Add-Finding $errors $file 1 'file-name' ("type '$($publicTypes[0])' in file '$baseName'")
    }
}

foreach ($e in $errors) { Write-Output "error $e" }
if ($ShowReview) {
    foreach ($r in $review) { Write-Output "review $r" }
}
Write-Output ("{0} files, {1} errors, {2} review items" -f $files.Count, $errors.Count, $review.Count)
if ($errors.Count -gt 0) { exit 1 }
