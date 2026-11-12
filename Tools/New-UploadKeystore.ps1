param(
    [string]$Directory,
    [string]$Alias = 'planetio-upload'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
if (-not $Directory) { $Directory = Join-Path $root 'Keystore' }
$validityDays = ([datetime]'9999-12-31' - (Get-Date).Date).Days

function Find-Keytool {
    $command = Get-Command keytool -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $candidates = @()
    if ($env:JAVA_HOME) { $candidates += Join-Path $env:JAVA_HOME 'bin\keytool.exe' }
    $candidates += Get-ChildItem 'C:\Program Files\Unity\Hub\Editor\*\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe' -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | ForEach-Object FullName
    $candidates += Get-ChildItem 'C:\Program Files\Microsoft\jdk-*\bin\keytool.exe' -ErrorAction SilentlyContinue |
        ForEach-Object FullName

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path $candidate)) { return $candidate }
    }

    throw 'keytool not found. Install a JDK or set JAVA_HOME.'
}

$keytool = Find-Keytool
New-Item -ItemType Directory -Force -Path $Directory | Out-Null
$keystore = Join-Path $Directory 'planetio-upload.keystore'

if (Test-Path $keystore) {
    throw "Keystore already exists: $keystore. Refusing to overwrite it."
}

Write-Host "keytool: $keytool"
Write-Host "Creating upload keystore: $keystore"
Write-Host 'keytool will ask for a keystore password, then your name/organization, then a key password (Enter = same as keystore).'
Write-Host 'Write the password down and back up the .keystore file. The default location is inside the repository, so keep the repository private.'
Write-Host ''

& $keytool -genkeypair -v -keystore $keystore -storetype PKCS12 -alias $Alias -keyalg RSA -keysize 4096 -validity $validityDays
if ($LASTEXITCODE -ne 0) { throw "keytool failed with exit code $LASTEXITCODE" }

Write-Host ''
Write-Host 'Keystore created. Set the password in the shell that launches Unity (or enter it in Player Settings > Publishing Settings):'
Write-Host "  `$env:PLANETIO_KEYSTORE_PASS = '<keystore password>'"
Write-Host 'Then run: Planet IO > Android > Build release AAB'
