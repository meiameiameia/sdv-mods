param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("PowerGrid", "MetalKegs", "FishSmoker", "FarmTerminal")]
    [string[]]$Mods,

    [ValidateSet("none", "patch", "minor", "major")]
    [string]$Bump = "none",

    [string]$SetVersion,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$OutputDir = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function Parse-Version([string]$versionText) {
    if ($versionText -notmatch '^\d+\.\d+\.\d+$') {
        throw "Version '$versionText' must use semantic format MAJOR.MINOR.PATCH."
    }

    $parts = $versionText.Split(".")
    return @{
        Major = [int]$parts[0]
        Minor = [int]$parts[1]
        Patch = [int]$parts[2]
    }
}

function Bump-Version([string]$current, [string]$kind) {
    $v = Parse-Version $current
    switch ($kind) {
        "patch" { return "$($v.Major).$($v.Minor).$($v.Patch + 1)" }
        "minor" { return "$($v.Major).$($v.Minor + 1).0" }
        "major" { return "$($v.Major + 1).0.0" }
        default { return $current }
    }
}

function Resolve-ModDefinition([string]$repoRoot, [string]$modName) {
    switch ($modName) {
        "PowerGrid" {
            return @{
                Name = "PowerGrid"
                FolderName = "[SMAPI] PowerGrid"
                ModDir = Join-Path $repoRoot "[SMAPI] PowerGrid"
                ManifestPath = Join-Path $repoRoot "[SMAPI] PowerGrid\manifest.json"
                CsprojPath = Join-Path $repoRoot "[SMAPI] PowerGrid\PowerGrid.csproj"
            }
        }
        "MetalKegs" {
            return @{
                Name = "MetalKegs"
                FolderName = "[SMAPI] Metal Kegs"
                ModDir = Join-Path $repoRoot "[SMAPI] Metal Kegs"
                ManifestPath = Join-Path $repoRoot "[SMAPI] Metal Kegs\manifest.json"
                CsprojPath = Join-Path $repoRoot "[SMAPI] Metal Kegs\MetalKegs.csproj"
            }
        }
        "FishSmoker" {
            return @{
                Name = "FishSmoker"
                FolderName = "[CP] FishSmoker Recipe"
                ModDir = Join-Path $repoRoot "[CP] FishSmoker Recipe"
                ManifestPath = Join-Path $repoRoot "[CP] FishSmoker Recipe\manifest.json"
                CsprojPath = $null
            }
        }
        "FarmTerminal" {
            return @{
                Name = "FarmTerminal"
                FolderName = "[SMAPI] Farm Terminal"
                ModDir = Join-Path $repoRoot "[SMAPI] Farm Terminal"
                ManifestPath = Join-Path $repoRoot "[SMAPI] Farm Terminal\manifest.json"
                CsprojPath = Join-Path $repoRoot "[SMAPI] Farm Terminal\FarmTerminal.csproj"
            }
        }
        default {
            throw "Unknown mod '$modName'."
        }
    }
}

function Get-ManifestVersion([string]$manifestPath) {
    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    return [string]$manifest.Version
}

function Set-ManifestVersion([string]$manifestPath, [string]$newVersion) {
    $json = Get-Content -Raw -LiteralPath $manifestPath
    $updated = [System.Text.RegularExpressions.Regex]::Replace(
        $json,
        '"Version"\s*:\s*"[0-9]+\.[0-9]+\.[0-9]+"',
        "`"Version`": `"$newVersion`"",
        1
    )

    if ($updated -eq $json) {
        throw "Could not find a Version field in '$manifestPath'."
    }

    Set-Content -LiteralPath $manifestPath -Value $updated -NoNewline
}

function Sync-CsprojVersion([string]$csprojPath, [string]$version) {
    [xml]$xml = Get-Content -LiteralPath $csprojPath
    $projectNode = $xml.SelectSingleNode("/Project")
    if ($null -eq $projectNode) {
        throw "Invalid csproj XML in '$csprojPath'."
    }

    $propertyGroupNode = $xml.SelectSingleNode("/Project/PropertyGroup")
    if ($null -eq $propertyGroupNode) {
        throw "No PropertyGroup found in '$csprojPath'."
    }

    $assemblyVersion = "$version.0"

    $fieldMap = @{
        Version = $version
        AssemblyVersion = $assemblyVersion
        FileVersion = $assemblyVersion
        InformationalVersion = $version
    }

    foreach ($field in $fieldMap.Keys) {
        $node = $propertyGroupNode.SelectSingleNode($field)
        if ($null -eq $node) {
            $node = $xml.CreateElement($field)
            $null = $propertyGroupNode.AppendChild($node)
        }
        $node.InnerText = [string]$fieldMap[$field]
    }

    $xml.Save($csprojPath)
}

function Build-Mod([string]$csprojPath, [string]$configuration) {
    & dotnet build $csprojPath -c $configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for '$csprojPath'."
    }
}

function Copy-ModPayload([string]$sourceDir, [string]$destDir) {
    $files = Get-ChildItem -LiteralPath $sourceDir -Recurse -File
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($sourceDir.Length).TrimStart('\')
        if ($relative -match '^(bin|obj)\\') { continue }
        if ($file.Extension -ieq ".zip") { continue }

        $target = Join-Path $destDir $relative
        $targetParent = Split-Path -Parent $target
        if (-not (Test-Path -LiteralPath $targetParent)) {
            New-Item -ItemType Directory -Path $targetParent | Out-Null
        }
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
}

function Package-Mod([hashtable]$modDef, [string]$version, [string]$outputDir) {
    if (-not (Test-Path -LiteralPath $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir | Out-Null
    }

    $zipName = "$($modDef.FolderName)-$version.zip"
    $zipPath = Join-Path $outputDir $zipName
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    $stageRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("sdv-mod-release-" + [Guid]::NewGuid().ToString("N"))
    $stageFolder = Join-Path $stageRoot $modDef.FolderName
    New-Item -ItemType Directory -Path $stageFolder -Force | Out-Null

    try {
        Copy-ModPayload -sourceDir $modDef.ModDir -destDir $stageFolder
        Compress-Archive -Path (Join-Path $stageRoot '*') -DestinationPath $zipPath -CompressionLevel Optimal
    }
    finally {
        if (Test-Path -LiteralPath $stageRoot) {
            Remove-Item -LiteralPath $stageRoot -Recurse -Force
        }
    }

    return $zipPath
}

$repoRoot = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "artifacts\mod-zips"
}

if (-not [string]::IsNullOrWhiteSpace($SetVersion)) {
    $null = Parse-Version $SetVersion
}

$results = New-Object System.Collections.Generic.List[object]

foreach ($modName in $Mods) {
    $modDef = Resolve-ModDefinition -repoRoot $repoRoot -modName $modName
    $currentVersion = Get-ManifestVersion -manifestPath $modDef.ManifestPath
    $targetVersion = if (-not [string]::IsNullOrWhiteSpace($SetVersion)) {
        $SetVersion
    }
    else {
        Bump-Version -current $currentVersion -kind $Bump
    }

    if ($targetVersion -ne $currentVersion) {
        Set-ManifestVersion -manifestPath $modDef.ManifestPath -newVersion $targetVersion
        Write-Host "[$($modDef.Name)] Version bumped: $currentVersion -> $targetVersion"
    }
    else {
        Write-Host "[$($modDef.Name)] Version unchanged: $targetVersion"
    }

    if ($null -ne $modDef.CsprojPath) {
        Sync-CsprojVersion -csprojPath $modDef.CsprojPath -version $targetVersion
        Build-Mod -csprojPath $modDef.CsprojPath -configuration $Configuration
    }

    $zipPath = Package-Mod -modDef $modDef -version $targetVersion -outputDir $OutputDir
    Write-Host "[$($modDef.Name)] Packaged: $zipPath"

    $results.Add([pscustomobject]@{
        Mod = $modDef.Name
        Version = $targetVersion
        ZipPath = $zipPath
    }) | Out-Null
}

Write-Host ""
Write-Host "Release summary:"
$results | ForEach-Object {
    Write-Host "- $($_.Mod) $($_.Version) -> $($_.ZipPath)"
}
