param([string] $TemplateProject = '../UnityTemplates')

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$templateProjectRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot $TemplateProject))
$templateRoot = Join-Path $templateProjectRoot 'Assets/UnityTemplates'
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
        $target = Join-Path $templateRoot $relative
        if (-not (Test-Path -LiteralPath $target -PathType Container)) {
            if ($part -eq 'Runtime') { throw "Required template source is missing: $target" }
            continue
        }
        $link = Join-Path $linkRoot $relative
        if (Test-Path -LiteralPath $link) {
            $existing = Get-Item -LiteralPath $link -Force
            if (($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) -and
                ([IO.Path]::GetFullPath($existing.Target) -eq [IO.Path]::GetFullPath($target))) {
                continue
            }
            throw "Destination already exists and is not the expected junction: $link"
        }
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $link) | Out-Null
        Copy-Item -LiteralPath "$target.meta" -Destination "$link.meta"
        New-Item -ItemType Junction -Path $link -Target $target | Out-Null
    }
}
Write-Host 'Planet_IO template sources linked. Demos and tests are excluded.'
