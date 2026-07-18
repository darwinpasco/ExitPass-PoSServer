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
$contractPath = Join-Path $repoRoot "contracts\pos-server\sales-invoice-header-profile-admin-api.v1.json"
$routeSourcePath = Join-Path $repoRoot "src\ExitPass.PosServer.Api\FiscalDocuments\FiscalDocumentEndpointRouteBuilderExtensions.cs"
$authSourcePath = Join-Path $repoRoot "src\ExitPass.PosServer.Api\FiscalDocuments\SalesInvoiceHeaderProfileAdminAuthorization.cs"
$endpointSourcePath = Join-Path $repoRoot "src\ExitPass.PosServer.Api\FiscalDocuments\SalesInvoiceHeaderProfileAdminEndpoint.cs"
$serviceSourcePath = Join-Path $repoRoot "src\ExitPass.PosServer.Runtime\FiscalDocuments\SalesInvoiceHeaderProfileAdminService.cs"
$runtimeTests = Join-Path $repoRoot "tests\ExitPass.PosServer.Runtime.Tests\ExitPass.PosServer.Runtime.Tests.csproj"
$apiTests = Join-Path $repoRoot "tests\ExitPass.PosServer.Api.Tests\ExitPass.PosServer.Api.Tests.csproj"
$persistenceTests = Join-Path $repoRoot "tests\ExitPass.PosServer.Persistence.Postgres.Tests\ExitPass.PosServer.Persistence.Postgres.Tests.csproj"
$dbChecks = Join-Path $repoRoot "db\scripts\Invoke-PosDbChecks.ps1"

Assert-Condition (Test-Path $contractPath) "Admin API contract artifact is missing."
Assert-Condition (Test-Path $routeSourcePath) "Admin route source is missing."
Assert-Condition (Test-Path $authSourcePath) "Admin authorization source is missing."
Assert-Condition (Test-Path $endpointSourcePath) "Admin endpoint source is missing."
Assert-Condition (Test-Path $serviceSourcePath) "Admin service source is missing."

$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
$routeSource = Get-Content -LiteralPath $routeSourcePath -Raw
$authSource = Get-Content -LiteralPath $authSourcePath -Raw
$endpointSource = Get-Content -LiteralPath $endpointSourcePath -Raw
$serviceSource = Get-Content -LiteralPath $serviceSourcePath -Raw

Assert-Condition ($contract.contractVersion -eq "sales-invoice-header-profile-admin-api.v1") "Unexpected admin API contract version."
Assert-Condition ($contract.authorization.policy -eq "SalesInvoiceHeaderProfileAdministration") "Unexpected admin authorization policy."
Assert-Condition ($contract.authorization.requiredPermission -eq "sales_invoice_header_profile.admin") "Unexpected admin required permission."
Assert-Condition ($contract.authorization.requiredHeaders -contains "X-Correlation-Id") "Correlation header is not governed."
Assert-Condition ($contract.authorization.requiredHeaders -contains "X-PosServer-Admin-Key") "Admin API key header is not governed."
Assert-Condition (-not ($contract.authorization.requiredHeaders -contains "X-PosServer-Admin-Permission")) "Permission header must not be required for authorization."
Assert-Condition ($contract.authorization.optionalHeaders -contains "X-PosServer-Admin-Permission") "Optional permission header posture is not governed."
Assert-Condition ($contract.authorization.apiKeyPosture.permissionSource -eq "server-derived") "Admin permission source must be server-derived."
Assert-Condition ($contract.authorization.permissionHeaderPosture.grantAuthority -eq $false) "Caller-supplied permission header must not grant authorization."
Assert-Condition ($routeSource -match "/v1/admin/fiscal-identities") "Fiscal Identity admin route is missing."
Assert-Condition ($routeSource -match "/v1/admin/sales-invoice-header-profiles") "Header Profile admin route is missing."
Assert-Condition ($routeSource -match "RequireAuthorization\(SalesInvoiceHeaderProfileAdminAuthorization.PolicyName\)") "Admin routes are not protected by the dedicated policy."
Assert-Condition ($authSource -match "AuthenticateResult.NoResult\(\)") "Unauthenticated admin request rejection path is missing."
Assert-Condition ($authSource -match "RequiredPermission") "Required permission mapping is missing."
Assert-Condition ($authSource -match "CryptographicOperations.FixedTimeEquals") "Admin API key comparison must use constant-time comparison."
Assert-Condition ($authSource -match "ResolveServerDerivedPermissions") "Server-derived permission resolution is missing."
Assert-Condition ($authSource -notmatch "suppliedPermission") "Permission claims must not be sourced from a supplied permission variable."
Assert-Condition ($authSource -notmatch "PermissionClaimType,\s*Request\.Headers") "Permission claims must not be sourced from request headers."
Assert-Condition ($endpointSource -match "missing_correlation_id") "Correlation validation is missing."
Assert-Condition ($endpointSource -notmatch "MapDelete") "Admin endpoint must not expose destructive delete."
Assert-Condition ($serviceSource -match "Only DRAFT profiles may be edited in place") "Draft-only mutation rule is missing."
Assert-Condition ($serviceSource -match "overlapping_approved_effective_window") "Overlap conflict rule is missing."
Assert-Condition ($serviceSource -match "RetireHeaderProfileAsync") "Retirement behavior is missing."
Assert-Condition ($serviceSource -match "GetEffectiveReadinessAsync") "Effective readiness behavior is missing."
Assert-Condition ($serviceSource -match "GetHeaderProfileUsageAsync") "Usage visibility behavior is missing."
Assert-Condition ($serviceSource -notmatch "Print") "Admin service must not introduce print behavior."
Assert-Condition ($serviceSource -notmatch "AssistedPaymentTerminal|AptPlaceholder") "Admin service must not introduce APT behavior."
Assert-Condition ($serviceSource -notmatch "CentralPms") "Admin service must not introduce Central PMS behavior."
Assert-Condition ($serviceSource -notmatch "ExitAuthorization") "Admin service must not introduce exit behavior."
Assert-Condition ($serviceSource -notmatch "GateExecution|OpenGate") "Admin service must not introduce gate behavior."

Push-Location $repoRoot
try {
    dotnet test $apiTests --no-build --filter "FullyQualifiedName~SalesInvoiceHeaderProfileAdmin"
    if ($LASTEXITCODE -ne 0) { throw "SalesInvoiceHeaderProfileAdmin API tests failed." }

    dotnet test $runtimeTests --no-build --filter "FullyQualifiedName~SalesInvoiceHeaderProfile"
    if ($LASTEXITCODE -ne 0) { throw "SalesInvoiceHeaderProfile runtime tests failed." }

    dotnet test $persistenceTests --no-build --filter "FullyQualifiedName~SalesInvoiceHeaderProfile"
    if ($LASTEXITCODE -ne 0) { throw "SalesInvoiceHeaderProfile persistence tests failed." }

    dotnet test $runtimeTests --no-build --filter "FullyQualifiedName~FiscalDocumentSemanticRequestHasher"
    if ($LASTEXITCODE -ne 0) { throw "FiscalDocumentSemanticRequestHasher tests failed." }

    powershell -ExecutionPolicy Bypass -File $dbChecks
    if ($LASTEXITCODE -ne 0) { throw "POS DB static validation failed." }
}
finally {
    Pop-Location
}

Write-Host "POS Server Sales Invoice header profile admin API proof passed."
Write-Host "Unauthorized/permission posture, forged permission-header rejection, authorized lifecycle operations, validation, approval, readiness, overlap conflict, usage visibility, retirement, semantic-hash protection, and no print/APT/Central PMS/exit/gate behavior were verified by contract/source checks and focused tests."
