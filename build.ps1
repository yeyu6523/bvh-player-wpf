param(
    [switch]$Publish,
    [switch]$CopyToDesktop,
    [string]$SampleDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
Push-Location $projectRoot
try {
    dotnet build BVHPlayer.sln -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed. Close the running development executable and retry.' }

    $testArgs = @('samples/studio-walk.bvh')
    if ($SampleDirectory) { $testArgs += $SampleDirectory }
    dotnet run --project tests/BvhPlayer.Tests -c Release --no-build -- @testArgs
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }

    if ($Publish -or $CopyToDesktop) {
        $publishDir = Join-Path $projectRoot 'artifacts/win-x64'
        dotnet publish src/BvhPlayer.App -c Release -r win-x64 --self-contained true -o $publishDir --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
        $exePath = Join-Path $publishDir 'BVHPlayer.exe'
        Get-FileHash -LiteralPath $exePath -Algorithm SHA256
        if ($CopyToDesktop) {
            $desktopPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'BVHPlayer.exe'
            Copy-Item -LiteralPath $exePath -Destination $desktopPath
            Write-Host "Executable copied to: $desktopPath"
        }
    }
} finally {
    Pop-Location
}
