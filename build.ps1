# Build script for DriversGet (C# 5 / .NET Framework 4.8 WinForms app).
# Uses the .NET Framework C# compiler (csc.exe) directly since no MSBuild /
# Visual Studio / dotnet CLI is available on this machine.

$ErrorActionPreference = "Stop"

$root    = Split-Path -Parent $MyInvocation.MyCommand.Definition
$srcDir  = Join-Path $root "src"
$outDir  = Join-Path $root "bin"
$exePath = Join-Path $outDir "DriversGet.exe"

# Locate csc.exe (prefer 64-bit framework compiler).
$cscCandidates = @(
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw "Could not find csc.exe (.NET Framework 4.x compiler)."
}
Write-Host "Using compiler: $csc"

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

# Gather all source files.
$sources = Get-ChildItem -Path $srcDir -Recurse -Filter *.cs | ForEach-Object { $_.FullName }
Write-Host "Compiling $($sources.Count) source file(s)..."

# Reference assemblies available in the .NET 4.8 reference/runtime set.
$refs = @(
    "System.dll",
    "System.Core.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    "System.Management.dll",
    "System.Xml.dll",
    "System.Configuration.dll"
)

$cscArgs = @(
    "/nologo",
    "/target:winexe",
    "/langversion:5",
    "/platform:anycpu",
    "/optimize+",
    "/out:`"$exePath`""
)
foreach ($r in $refs) { $cscArgs += "/reference:$r" }
foreach ($s in $sources) { $cscArgs += "`"$s`"" }

& $csc $cscArgs
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

# Copy app.config next to the exe so runtime config (TLS etc.) applies.
$appConfig = Join-Path $srcDir "App.config"
if (Test-Path $appConfig) {
    Copy-Item $appConfig (Join-Path $outDir "DriversGet.exe.config") -Force
}

Write-Host ""
Write-Host "Build succeeded: $exePath" -ForegroundColor Green
