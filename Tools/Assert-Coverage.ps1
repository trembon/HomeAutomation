param(
    [Parameter()]
    [string]$ResultsDirectory = "TestResults",

    [Parameter()]
    [ValidateRange(0, 100)]
    [double]$MinimumLineCoverage = 29
)

$coverageFile = Get-ChildItem -LiteralPath $ResultsDirectory -Filter "coverage.cobertura.xml" -Recurse |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $coverageFile) {
    throw "No coverage.cobertura.xml file was found under '$ResultsDirectory'."
}

[xml]$coverage = Get-Content -Raw -LiteralPath $coverageFile.FullName
$lineRate = [double]::Parse(
    $coverage.DocumentElement.GetAttribute("line-rate"),
    [System.Globalization.CultureInfo]::InvariantCulture)
$lineCoverage = $lineRate * 100

Write-Host ("Maintained-code line coverage: {0:N2}% (required: {1:N2}%)" -f $lineCoverage, $MinimumLineCoverage)

if ($lineCoverage -lt $MinimumLineCoverage) {
    throw ("Line coverage {0:N2}% is below the required {1:N2}%." -f $lineCoverage, $MinimumLineCoverage)
}
