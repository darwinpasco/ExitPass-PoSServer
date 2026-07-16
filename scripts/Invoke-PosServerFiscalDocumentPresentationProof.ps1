param()

$ErrorActionPreference = "Stop"

function Assert-Condition {
    param(
        [bool] $Condition,
        [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$contractPath = Join-Path $repoRoot "contracts\pos-server\fiscal-document-presentation.v1.json"
$fixturePath = Join-Path $repoRoot "docs\v1.3\pos-server\digital-sales-invoice\fixtures\fiscal-document-presentation-contract-v1.json"
$apiTestProject = Join-Path $repoRoot "tests\ExitPass.PosServer.Api.Tests\ExitPass.PosServer.Api.Tests.csproj"

Assert-Condition (Test-Path $contractPath) "Contract artifact is missing."
Assert-Condition (Test-Path $fixturePath) "Presentation contract fixture is missing."

$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
$fixture = Get-Content -LiteralPath $fixturePath -Raw | ConvertFrom-Json

Assert-Condition ($contract.route -eq "/v1/fiscal-documents/{fiscalDocumentId}/digital-sales-invoice/presentation") "Unexpected authoritative presentation endpoint."
Assert-Condition ($contract.method -eq "GET") "Presentation endpoint must be GET."
Assert-Condition ($contract.contentType -eq "application/json") "Presentation endpoint content type must be application/json."
Assert-Condition ($contract.presentationVersion -eq "digital-sales-invoice-presentation-json-v1") "Presentation version is missing or unexpected."
Assert-Condition ($contract.templateVersion -eq "digital-sales-invoice-json-v1") "Template version is missing or unexpected."

$assigned = $fixture.cases | Where-Object { $_.caseId -eq "recorded-assigned-cash" } | Select-Object -First 1
$notAssigned = $fixture.cases | Where-Object { $_.caseId -eq "recorded-not-assigned-cash" } | Select-Object -First 1
$voided = $fixture.cases | Where-Object { $_.caseId -eq "voided-assigned-cash" } | Select-Object -First 1

Assert-Condition ($null -ne $assigned) "Assigned recorded fixture case is missing."
Assert-Condition ($null -ne $notAssigned) "Not-assigned recorded fixture case is missing."
Assert-Condition ($null -ne $voided) "Voided fixture case is missing."
Assert-Condition ($assigned.storedFiscalDocument.fiscalDocumentNumber -eq "SI-00000001-A") "Assigned fiscal number does not match fixture."
Assert-Condition ($assigned.storedFiscalDocument.tenderType -eq "cash") "Assigned fixture does not represent CASH tender."
Assert-Condition ($notAssigned.expectedResponse.fiscalNumberAssignmentState -eq "not_assigned") "Not-assigned fixture posture is unexpected."
Assert-Condition ($voided.expectedResponse.fiscalDocumentStatus -eq "voided") "Voided fixture status is unexpected."
Assert-Condition ($voided.expectedResponse.voidStatus -eq "recorded") "Voided fixture void status is unexpected."

$assignedRows = @($assigned.expectedPresentationRows)
Assert-Condition (@($assignedRows | Where-Object { $_.key -eq "lineItems[0000].description" -and $_.displayValue -eq "Parking fee" }).Count -eq 1) "Line presentation row is missing."
Assert-Condition (@($assignedRows | Where-Object { $_.key -eq "taxes[0000].taxAmount" -and $_.displayValue -eq "PHP 0.00" }).Count -eq 1) "Tax presentation row is missing."
Assert-Condition (@($assignedRows | Where-Object { $_.key -eq "totals[0000].amount" -and $_.displayValue -eq "PHP 125.00" }).Count -eq 1) "Total presentation row is missing."
Assert-Condition (@($assignedRows | Where-Object { $_.key -eq "tenders[0000].tenderTypeCodeKey" -and $_.displayValue -eq "cash" }).Count -eq 1) "CASH tender presentation row is missing."

Push-Location $repoRoot
try {
    dotnet test $apiTestProject --no-build --filter "FullyQualifiedName~FiscalDocumentPresentation"
    if ($LASTEXITCODE -ne 0) {
        throw "Focused FiscalDocumentPresentation tests failed."
    }
}
finally {
    Pop-Location
}

Write-Host "POS Server fiscal document presentation proof passed."
Write-Host "Authoritative presentation endpoint returned fixture-governed Digital Sales Invoice payload."
Write-Host "Fiscal number, references, line, tax, total, CASH tender, repeated-read, void posture, and no-mutation checks passed through focused tests."
