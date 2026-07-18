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
$contractPath = Join-Path $repoRoot "contracts\pos-server\sales-invoice-header-profile.v1.json"
$fixturePath = Join-Path $repoRoot "docs\v1.3\pos-server\sales-invoice-header-profile\fixtures\sales-invoice-header-profile-v1.json"
$runtimeTests = Join-Path $repoRoot "tests\ExitPass.PosServer.Runtime.Tests\ExitPass.PosServer.Runtime.Tests.csproj"
$persistenceTests = Join-Path $repoRoot "tests\ExitPass.PosServer.Persistence.Postgres.Tests\ExitPass.PosServer.Persistence.Postgres.Tests.csproj"
$apiTests = Join-Path $repoRoot "tests\ExitPass.PosServer.Api.Tests\ExitPass.PosServer.Api.Tests.csproj"
$dbChecks = Join-Path $repoRoot "db\scripts\Invoke-PosDbChecks.ps1"
$semanticFixturePath = Join-Path $repoRoot "docs\v1.3\pos-server\fiscal-numbering\fixtures\pos_server_semantic_hash_sha256_v1_representative_fixture.json"
$semanticTestPath = Join-Path $repoRoot "tests\ExitPass.PosServer.Runtime.Tests\FiscalDocumentSemanticRequestHasherTests.cs"
$profileRuntimeTestPath = Join-Path $repoRoot "tests\ExitPass.PosServer.Runtime.Tests\SalesInvoiceHeaderProfileServiceTests.cs"
$profilePersistenceTestPath = Join-Path $repoRoot "tests\ExitPass.PosServer.Persistence.Postgres.Tests\SalesInvoiceHeaderProfilePersistenceTests.cs"

Assert-Condition (Test-Path $contractPath) "Sales Invoice header profile contract artifact is missing."
Assert-Condition (Test-Path $fixturePath) "Sales Invoice header profile fixture is missing."
Assert-Condition (Test-Path $dbChecks) "POS DB checks script is missing."
Assert-Condition (Test-Path $semanticFixturePath) "Fiscal semantic hash fixture is missing."
Assert-Condition (Test-Path $semanticTestPath) "Fiscal semantic hash tests are missing."
Assert-Condition (Test-Path $profileRuntimeTestPath) "Sales Invoice header profile runtime tests are missing."
Assert-Condition (Test-Path $profilePersistenceTestPath) "Sales Invoice header profile persistence tests are missing."

$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
$fixture = Get-Content -LiteralPath $fixturePath -Raw | ConvertFrom-Json
$semanticFixture = Get-Content -LiteralPath $semanticFixturePath -Raw | ConvertFrom-Json
$semanticTestSource = Get-Content -LiteralPath $semanticTestPath -Raw
$profileRuntimeTestSource = Get-Content -LiteralPath $profileRuntimeTestPath -Raw
$profilePersistenceTestSource = Get-Content -LiteralPath $profilePersistenceTestPath -Raw

Assert-Condition ($contract.contractVersion -eq "sales-invoice-header-profile.v1") "Unexpected contract version."
Assert-Condition ($contract.supportedTemplateVersion -eq "digital-sales-invoice-json-v1") "Unexpected template version."
Assert-Condition ($contract.supportedPresentationVersion -eq "digital-sales-invoice-presentation-json-v1") "Unexpected presentation version."
Assert-Condition ($contract.enforcementSetting.name -eq "POS_REQUIRE_COMPLETE_SALES_INVOICE_HEADER_PROFILE") "Unexpected enforcement setting."
Assert-Condition ($contract.enforcementSetting.default -eq $false) "Header profile enforcement must default false."
Assert-Condition ($semanticFixture.canonical_source_version -eq "sha256:v1") "Semantic fixture source version changed."
Assert-Condition ($semanticFixture.hash_algorithm_version -eq "sha256:v1") "Semantic fixture hash version changed."
Assert-Condition ($semanticFixture.expected_sha256_hash -eq "6a490379e4275a57f0a0695ff9dbd1271c4480adaeeefb9b6bfbd11e4d1ed201") "Existing Central PMS representative sha256:v1 hash changed."
Assert-Condition ($semanticFixture.canonical_source_fact_count -eq 20) "Existing Central PMS representative sha256:v1 fact count changed."
Assert-Condition ($semanticFixture.canonical_source_text -notmatch "site_id") "Server profile resolution site_id leaked into sha256:v1 fixture."
Assert-Condition ($semanticFixture.canonical_source_text -notmatch "runtime_terminal_ref") "Runtime terminal ref leaked into sha256:v1 fixture."
Assert-Condition ($semanticFixture.canonical_source_text -notmatch "sales_invoice_header_profile") "Header profile data leaked into sha256:v1 fixture."
Assert-Condition ($semanticFixture.canonical_source_text -notmatch "bir_accreditation") "BIR profile data leaked into sha256:v1 fixture."
Assert-Condition ($semanticFixture.canonical_source_text -notmatch "ptu") "PTU profile data leaked into sha256:v1 fixture."
Assert-Condition ($semanticTestSource -match "ProfileResolutionInputsAndSnapshotFactsDoNotChangeSha256V1CanonicalSource") "Semantic profile-exclusion compatibility test is missing."
Assert-Condition ($semanticTestSource -match "ChangedStableFiscalFactsChangeSha256V1Hash") "Changed request semantic conflict coverage is missing."
Assert-Condition ($profileRuntimeTestSource -match "SnapshotIsImmutableWhenProfileIsRetiredOrSupersededLater") "Profile supersession snapshot immutability test is missing."
Assert-Condition ($profilePersistenceTestSource -match "IdempotentReplayReturnsBeforeProfileResolutionAndSnapshotCreation") "Replay-before-profile-resolution proof test is missing."

$complete = $fixture.profiles | Where-Object { $_.caseId -eq "complete-approved-effective-profile" } | Select-Object -First 1
$future = $fixture.profiles | Where-Object { $_.caseId -eq "future-dated-approved-profile" } | Select-Object -First 1
$expired = $fixture.profiles | Where-Object { $_.caseId -eq "expired-profile" } | Select-Object -First 1
$draft = $fixture.profiles | Where-Object { $_.caseId -eq "draft-profile" } | Select-Object -First 1
$incomplete = $fixture.profiles | Where-Object { $_.caseId -eq "incomplete-profile" } | Select-Object -First 1
$superseding = $fixture.profiles | Where-Object { $_.caseId -eq "superseding-profile-version" } | Select-Object -First 1

Assert-Condition ($null -ne $complete) "Complete approved effective profile fixture is missing."
Assert-Condition ($null -ne $future) "Future-dated approved profile fixture is missing."
Assert-Condition ($null -ne $expired) "Expired profile fixture is missing."
Assert-Condition ($null -ne $draft) "Draft profile fixture is missing."
Assert-Condition ($null -ne $incomplete) "Incomplete profile fixture is missing."
Assert-Condition ($null -ne $superseding) "Superseding profile version fixture is missing."
Assert-Condition ($complete.birAccreditationIssuedDate -ne $complete.birAccreditationValidUntil) "BIR issue and valid-until dates must be distinct in fixture."
Assert-Condition ($complete.birAccreditationIssuedDate -ne $complete.ptuIssuedDate) "BIR issued date and PTU issued date must be distinct in fixture."
Assert-Condition ($fixture.issuedFiscalDocumentWithImmutableSnapshot.snapshot.terminalId -eq "RUNTIME-TERMINAL-001") "Runtime terminal ID snapshot is missing."
Assert-Condition ($fixture.issuedFiscalDocumentWithImmutableSnapshot.snapshot.parkingLocationDisplay -eq "GOVERNED TEST PARKING LOCATION") "Original snapshot parking location changed unexpectedly."

Push-Location $repoRoot
try {
    dotnet test $runtimeTests --no-build --filter "FullyQualifiedName~FiscalDocumentSemanticRequestHasher"
    if ($LASTEXITCODE -ne 0) { throw "Runtime FiscalDocumentSemanticRequestHasher compatibility tests failed." }

    dotnet test $runtimeTests --no-build --filter "FullyQualifiedName~SalesInvoiceHeaderProfile"
    if ($LASTEXITCODE -ne 0) { throw "Runtime SalesInvoiceHeaderProfile tests failed." }

    dotnet test $persistenceTests --no-build --filter "FullyQualifiedName~SalesInvoiceHeaderProfile"
    if ($LASTEXITCODE -ne 0) { throw "Persistence SalesInvoiceHeaderProfile tests failed." }

    dotnet test $apiTests --no-build --filter "FullyQualifiedName~FiscalDocumentPresentation"
    if ($LASTEXITCODE -ne 0) { throw "Affected presentation contract tests failed." }

    powershell -ExecutionPolicy Bypass -File $dbChecks
    if ($LASTEXITCODE -ne 0) { throw "POS DB static validation failed." }
}
finally {
    Pop-Location
}

Write-Host "POS Server Sales Invoice header profile proof passed."
Write-Host "Complete profile, fixture-governed snapshot, distinct BIR/PTU dates, historical immutability, semantic hash compatibility, replay-before-profile-resolution posture, presentation integration, and no external workflow behavior were verified by focused tests/static DB checks."
