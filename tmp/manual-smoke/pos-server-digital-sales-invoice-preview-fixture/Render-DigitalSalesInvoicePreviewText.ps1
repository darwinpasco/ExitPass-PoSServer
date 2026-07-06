param(
    [string] $FixturePath
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($FixturePath)) {
    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
    $FixturePath = Join-Path $repoRoot "docs\v1.3\pos-server\digital-sales-invoice\fixtures\digital-sales-invoice-presentation-assigned-sample-v1.json"
}

$resolvedFixturePath = Resolve-Path $FixturePath
$fixture = Get-Content -LiteralPath $resolvedFixturePath -Raw | ConvertFrom-Json
$presentation = $fixture.presentation

if ($null -eq $presentation) {
    throw "Fixture does not contain a presentation object: $resolvedFixturePath"
}

Write-Host "Digital Sales Invoice Preview Fixture"
Write-Host "Fixture: $resolvedFixturePath"
Write-Host "Presentation version: $($presentation.presentationVersion)"
Write-Host "Source contract: $($presentation.sourceTemplateContractVersion)"
Write-Host "Numbering state: $($presentation.numberingState)"
Write-Host ""

foreach ($notice in @($presentation.notices)) {
    Write-Host ("[{0}] {1}: {2}" -f $notice.severity, $notice.code, $notice.message)
}

if (@($presentation.notices).Count -gt 0) {
    Write-Host ""
}

foreach ($section in @($presentation.sections | Sort-Object sortOrder)) {
    Write-Host ("## {0} ({1})" -f $section.label, $section.posture)

    foreach ($row in @($section.rows)) {
        $value = $row.displayValue
        if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string] $value)) {
            $value = "<not available>"
        }

        Write-Host ("- {0}: {1} [{2}; {3}]" -f $row.label, $value, $row.valueKind, $row.posture)
    }

    Write-Host ""
}

Write-Host "Plain-text fixture preview only. No API call, PDF, HTML, QR, Digital SI URL, access event, or final statutory text was generated."
