param([string] $TemplateProject = '../UnityTemplates')

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$templateProjectRoot = if ([IO.Path]::IsPathRooted($TemplateProject)) {
    [IO.Path]::GetFullPath($TemplateProject)
} else {
    [IO.Path]::GetFullPath((Join-Path $projectRoot $TemplateProject))
}
$destinationAliases = @{
    'Modules/Core/SceneFlow' = 'Modules/Framework/SceneFlow'
    'Modules/Core/Settings' = 'Modules/Framework/Settings'
    'Modules/Core/Save' = 'Modules/Framework/Save'
    'Modules/Core/Startup' = 'Modules/Framework/Startup'
    'Modules/Core/Audio' = 'Modules/Presentation/Audio'
}
$units = @(
    'Foundation',
    'Modules/Core/SceneFlow',
    'Modules/Core/Settings',
    'Modules/Framework/Localization',
    'Modules/Framework/Addressables',
    'Modules/Core/Save',
    'Modules/Core/Startup',
    'Modules/Framework/Attributes',
    'Modules/Core/Audio',
    'Modules/Presentation/Haptics'
)

$templateRoot = Join-Path $templateProjectRoot 'Assets/UnityTemplates'
& (Join-Path $templateProjectRoot 'Tools/Link-UnityTemplates.ps1') -Project $projectRoot -Source $templateRoot -MountName TemplateSources -Folders $units -DestinationAliases $destinationAliases
