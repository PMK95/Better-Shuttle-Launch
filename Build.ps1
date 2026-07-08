$ErrorActionPreference = "Stop"

try {
    $repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    Set-Location $repoRoot

    $projectPath = Join-Path $repoRoot "Source\BetterShuttleLaunch\BetterShuttleLaunch.csproj"
    $outputDll = Join-Path $repoRoot "Better Shuttle Launch\1.6\Assemblies\BetterShuttleLaunch.dll"
    $assemblyDir = Split-Path -Parent $outputDll

    Write-Host "Starting Better Shuttle Launch Release build."
    Write-Host "Project: $projectPath"

    dotnet restore $projectPath
    dotnet build $projectPath -c Release --no-restore

    if (-not (Test-Path $outputDll)) {
        throw "Build output DLL was not found: $outputDll"
    }

    $extraFiles = @(Get-ChildItem -LiteralPath $assemblyDir -File | Where-Object { $_.Name -ne "BetterShuttleLaunch.dll" })
    if ($extraFiles.Count -gt 0) {
        Write-Warning "Assemblies contains files other than the release DLL."
        foreach ($file in $extraFiles) {
            Write-Warning " - $($file.Name)"
        }
    }

    Write-Host "Build completed: $outputDll"
    exit 0
}
catch {
    Write-Host "Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
