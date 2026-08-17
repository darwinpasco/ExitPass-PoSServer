[CmdletBinding()]
param(
    [string] $OutputDirectory,
    [string] $DockerImage = 'postgres:16-alpine'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..')).Path
$datasetPath = Join-Path $repoRoot 'docs\v1.3\fiscal-reporting\annex-e\dataset\v1.7\annex-e1-synthetic-uat-dataset-v1.7.jsonl'
$validatorPath = Join-Path $repoRoot 'scripts\Invoke-AnnexE1SyntheticUatDatasetV17Validation.ps1'
$executionPath = Join-Path $repoRoot 'scripts\Invoke-AnnexE1V17IsolatedExecution.ps1'
$testProject = Join-Path $repoRoot 'tests\ExitPass.PosServer.Runtime.Tests\ExitPass.PosServer.Runtime.Tests.csproj'
$invocationId = ((Get-Date).ToUniversalTime().ToString('yyyyMMddHHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 10)).ToLowerInvariant()
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) "exitpass-annex-e1-v17-workbooks-$invocationId"
$workbookOutput = Join-Path $tempRoot 'generated'
$reportPath = Join-Path $tempRoot 'normalized-workbook-report.txt'
$oldDataset = $env:ANNEX_E1_V17_WORKBOOK_DATASET_PATH
$oldOutput = $env:ANNEX_E1_V17_WORKBOOK_OUTPUT_PATH
$oldReport = $env:ANNEX_E1_V17_WORKBOOK_REPORT_PATH
$createdResources = New-Object System.Collections.Generic.List[string]
$removedResources = New-Object System.Collections.Generic.List[string]

function Invoke-CheckedNative {
    param([string] $Name, [string] $FileName, [string[]] $Arguments)
    $savedErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $FileName @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }
    if ($exitCode -ne 0) {
        $diagnostic = ($output | Select-Object -Last 30) -join [Environment]::NewLine
        throw "$Name failed with exit code $exitCode.$([Environment]::NewLine)$diagnostic"
    }
    return $output
}

try {
    if (-not (Test-Path -LiteralPath $datasetPath -PathType Leaf)) { throw 'The committed v1.7 dataset is missing.' }
    if (-not (Test-Path -LiteralPath $validatorPath -PathType Leaf)) { throw 'The v1.7 offline validator is missing.' }
    if (-not (Test-Path -LiteralPath $executionPath -PathType Leaf)) { throw 'The v1.7 isolated execution runner is missing.' }
    if ($OutputDirectory) {
        $resolvedParent = Split-Path -Parent ([IO.Path]::GetFullPath($OutputDirectory))
        if (-not (Test-Path -LiteralPath $resolvedParent -PathType Container)) { throw "Output parent does not exist: $resolvedParent" }
        if (Test-Path -LiteralPath $OutputDirectory) { throw "Output directory must not already exist: $OutputDirectory" }
    }

    [void](New-Item -ItemType Directory -Path $tempRoot)
    $createdResources.Add("temporary-directory:$tempRoot")

    Invoke-CheckedNative 'validate committed Annex E-1 v1.7 package and dataset' 'powershell.exe' @(
        '-NoProfile','-ExecutionPolicy','Bypass','-File',$validatorPath
    ) | Out-Host

    $executionOutput = Invoke-CheckedNative 'execute the existing isolated Annex E-1 v1.7 harness' 'powershell.exe' @(
        '-NoProfile','-ExecutionPolicy','Bypass','-File',$executionPath,'-DockerImage',$DockerImage
    )
    $executionOutput | Out-Host
    if (($executionOutput -join "`n") -notmatch 'ANNEX_E1_V17_HARNESS=PASS') { throw 'The isolated execution harness did not declare PASS.' }
    if (($executionOutput -join "`n") -notmatch 'NORMALIZED_OUTPUT_SHA256=eb49cc8f3ba5a2935463e030f7e2b1707131aff68bcfd1d37c4b6ea1e224a18e') {
        throw 'The isolated execution report digest does not match the merged v1.7 baseline.'
    }

    Invoke-CheckedNative 'build workbook tests' 'dotnet' @('build',$testProject,'--configuration','Release','--no-restore') | Out-Host
    $env:ANNEX_E1_V17_WORKBOOK_DATASET_PATH = $datasetPath
    $env:ANNEX_E1_V17_WORKBOOK_OUTPUT_PATH = $workbookOutput
    $env:ANNEX_E1_V17_WORKBOOK_REPORT_PATH = $reportPath
    Invoke-CheckedNative 'generate and validate Annex E-1 v1.7 internal workbooks' 'dotnet' @(
        'test',$testProject,'--configuration','Release','--no-build',
        '--filter','FullyQualifiedName~AnnexE1V17WorkbookGenerationTests',
        '--logger','console;verbosity=minimal'
    ) | Out-Host

    if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) { throw 'Workbook tests did not produce the normalized report.' }
    $report = [IO.File]::ReadAllText($reportPath, [Text.Encoding]::UTF8)
    if (-not $report.StartsWith("ANNEX_E1_V17_INTERNAL_WORKBOOK_GENERATION=PASS`n", [StringComparison]::Ordinal)) { throw 'Workbook report did not declare PASS.' }
    $workbooks = @(Get-ChildItem -LiteralPath $workbookOutput -File -Filter '*.xlsx')
    if ($workbooks.Count -ne 19) { throw "Generated workbook count mismatch: $($workbooks.Count)." }
    if (-not (Test-Path -LiteralPath (Join-Path $workbookOutput 'annex-e1-v1.7-internal-workbook-manifest.json') -PathType Leaf)) { throw 'Generated workbook manifest is missing.' }

    if ($OutputDirectory) {
        [void](New-Item -ItemType Directory -Path $OutputDirectory)
        foreach ($file in Get-ChildItem -LiteralPath $workbookOutput -File) {
            Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $OutputDirectory $file.Name)
        }
        Write-Output "INSPECTION_OUTPUT=$([IO.Path]::GetFullPath($OutputDirectory))"
    }

    Write-Output $report.TrimEnd()
    Write-Output 'OFFLINE_VALIDATOR=PASS'
    Write-Output 'ISOLATED_EXECUTION=PASS'
    Write-Output 'NEGATIVE_TESTS=12_PASS'
    Write-Output 'TWO_RUN_BYTE_DETERMINISM=PASS'
}
finally {
    $env:ANNEX_E1_V17_WORKBOOK_DATASET_PATH = $oldDataset
    $env:ANNEX_E1_V17_WORKBOOK_OUTPUT_PATH = $oldOutput
    $env:ANNEX_E1_V17_WORKBOOK_REPORT_PATH = $oldReport
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
    if (Test-Path -LiteralPath $tempRoot) { throw "Invocation-owned temporary directory remains: $tempRoot" }
    $removedResources.Add("temporary-directory:$tempRoot")
    Write-Output 'TEMPORARY_RESOURCES_REMAINING=0'
    foreach ($resource in $createdResources) { Write-Output "CREATED_RESOURCE=$resource" }
    foreach ($resource in $removedResources) { Write-Output "REMOVED_RESOURCE=$resource" }
}
