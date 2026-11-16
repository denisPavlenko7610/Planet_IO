param([string] $TemplateProject = '../UnityTemplates')

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$templateProjectRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot $TemplateProject))
$templateRoot = Join-Path $templateProjectRoot 'Assets/UnityTemplates'
$sourceUnits = @{
    'Modules/Framework/SceneFlow' = 'Modules/Core/SceneFlow'
    'Modules/Framework/Settings' = 'Modules/Core/Settings'
    'Modules/Framework/Save' = 'Modules/Core/Save'
    'Modules/Framework/Startup' = 'Modules/Core/Startup'
    'Modules/Presentation/Audio' = 'Modules/Core/Audio'
}
$linkRoot = Join-Path $projectRoot 'Assets/TemplateSources'
$units = @(
    'Foundation',
    'Modules/Framework/SceneFlow',
    'Modules/Framework/Settings',
    'Modules/Framework/Localization',
    'Modules/Framework/Addressables',
    'Modules/Framework/Save',
    'Modules/Framework/Startup',
    'Modules/Framework/Attributes',
    'Modules/Presentation/Audio',
    'Modules/Presentation/Haptics'
)

foreach ($unit in $units) {
    foreach ($part in @('Runtime', 'Editor')) {
        $relative = "$unit/$part"
        $sourceUnit = if ($sourceUnits.ContainsKey($unit)) { $sourceUnits[$unit] } else { $unit }
        $target = Join-Path $templateRoot "$sourceUnit/$part"
        if (-not (Test-Path -LiteralPath $target -PathType Container)) {
            if ($part -eq 'Runtime') { throw "Required template source is missing: $target" }
            continue
        }
        $link = Join-Path $linkRoot $relative
        if (Get-Item -LiteralPath $link -Force -ErrorAction SilentlyContinue) {
            $existing = Get-Item -LiteralPath $link -Force
            if (($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) -and
                ([IO.Path]::GetFullPath($existing.Target) -eq [IO.Path]::GetFullPath($target))) {
                continue
            }
            if (-not ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw "Destination already exists and is not a junction: $link"
            }
            $absoluteLink = [IO.Path]::GetFullPath($link)
            $allowedRoot = [IO.Path]::GetFullPath($linkRoot) + [IO.Path]::DirectorySeparatorChar
            if (-not $absoluteLink.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Link is outside the template link root: $absoluteLink"
            }
            [IO.Directory]::Delete($absoluteLink)
        }
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $link) | Out-Null
        if (-not (Test-Path -LiteralPath "$link.meta")) {
            Copy-Item -LiteralPath "$target.meta" -Destination "$link.meta"
        }
        New-Item -ItemType Junction -Path $link -Target $target | Out-Null
    }
}
Write-Host 'Planet_IO template sources linked. Demos and tests are excluded.'
