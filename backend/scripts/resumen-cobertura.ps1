$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$umbral = 60
Set-Location (Split-Path -Parent $PSScriptRoot)

dotnet tool restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$cobertura = Get-ChildItem -Path cobertura -Recurse -Filter coverage.cobertura.xml | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $cobertura) {
  Write-Host 'No hay informe de cobertura.'
  exit 1
}

dotnet tool run reportgenerator -- "-reports:$($cobertura.FullName)" "-targetdir:cobertura/informe" "-reporttypes:MarkdownSummaryGithub;JsonSummary" "-assemblyfilters:+Arquitectos.Application;+Arquitectos.Infrastructure"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$resumen = Get-Content -Raw -Encoding utf8 'cobertura/informe/Summary.json' | ConvertFrom-Json
$lineas = [double]$resumen.summary.linecoverage
$porcentaje = $lineas.ToString('0.0', [Globalization.CultureInfo]::InvariantCulture)
Write-Host "Cobertura de lineas (Application + Infrastructure): $porcentaje % (umbral $umbral %)."

if ($env:GITHUB_STEP_SUMMARY) {
  $encabezado = @"
### Cobertura backend

Lineas en Application e Infrastructure: **$porcentaje %** (umbral $umbral %).

"@
  $informe = Get-Content -Raw -Encoding utf8 'cobertura/informe/SummaryGithub.md'
  Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value ($encabezado + $informe) -Encoding utf8
}

if ($lineas -lt $umbral) {
  Write-Host "La cobertura $porcentaje % esta bajo el umbral de $umbral %."
  exit 1
}
