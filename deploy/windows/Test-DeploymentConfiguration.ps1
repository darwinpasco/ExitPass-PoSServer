[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $EnvFile,

    [string] $RestoreCredentialFile,

    [switch] $RequirePinnedImages,

    [switch] $StaticOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$deployRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $deployRoot
$composeFile = Join-Path $deployRoot 'compose.yaml'
$resolvedEnvFile = (Resolve-Path -LiteralPath $EnvFile).Path
$approvedGcpProjectId = 'exitpass-staging'
$approvedGcsBucket = 'exitpass-staging-pos-backups'
$approvedGcsLocation = 'asia-southeast1'
$approvedBackupServiceAccount = 'exitpass-pos-backup@exitpass-staging.iam.gserviceaccount.com'
$approvedRestoreServiceAccount = 'exitpass-pos-restore@exitpass-staging.iam.gserviceaccount.com'
$approvedBackupIntervalSeconds = '3600'
$approvedGoogleCloudCliVersion = '588.0.0-0'
$approvedBackupCredentialPath = 'C:\ExitPass\Runtime\secrets\gcs-backup-adc.json'
$approvedRestoreCredentialPath = 'C:\ExitPass\Runtime\restore-secrets\gcs-restore-adc.json'

function Read-DotEnv {
    param([string] $Path)

    $result = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#')) {
            continue
        }
        $separator = $trimmed.IndexOf('=')
        if ($separator -lt 1) {
            throw "Invalid environment-file entry."
        }
        $name = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim()
        $result[$name] = $value
    }
    return $result
}

function Require-Setting {
    param([hashtable] $Settings, [string] $Name)

    if (-not $Settings.ContainsKey($Name) -or [string]::IsNullOrWhiteSpace($Settings[$Name])) {
        throw "Required setting '$Name' is missing."
    }
}

function Require-ExternalFile {
    param([hashtable] $Settings, [string] $Name)

    Require-Setting -Settings $Settings -Name $Name
    $path = $Settings[$Name]
    if (-not [System.IO.Path]::IsPathRooted($path)) {
        throw "Setting '$Name' must use an absolute external path."
    }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "The external file configured by '$Name' does not exist."
    }
    $resolvedPath = (Resolve-Path -LiteralPath $path).Path
    $repoPrefix = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    if ($resolvedPath.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "The file configured by '$Name' must be stored outside the repository."
    }
    if ((Get-Item -LiteralPath $resolvedPath).Length -eq 0) {
        throw "The external file configured by '$Name' is empty."
    }
}

function Require-ExternalDirectory {
    param([hashtable] $Settings, [string] $Name)

    Require-Setting -Settings $Settings -Name $Name
    $path = $Settings[$Name]
    if (-not [System.IO.Path]::IsPathRooted($path)) {
        throw "Setting '$Name' must use an absolute external path."
    }
    if (-not (Test-Path -LiteralPath $path -PathType Container)) {
        throw "The external directory configured by '$Name' does not exist."
    }
    $resolvedPath = (Resolve-Path -LiteralPath $path).Path
    $repoPrefix = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    if ($resolvedPath.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "The directory configured by '$Name' must be stored outside the repository."
    }
}

function Get-IgnorePolicyLines {
    param([string] $Path)

    return @(
        Get-Content -LiteralPath $Path |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_.Length -gt 0 -and -not $_.StartsWith('#') }
    )
}

function Test-GitPathIgnored {
    param(
        [string] $RepositoryRoot,
        [string] $RelativePath
    )

    $null = & git -C $RepositoryRoot check-ignore --quiet --no-index -- $RelativePath
    $exitCode = $LASTEXITCODE
    if ($exitCode -gt 1) {
        throw "Git ignore-policy validation failed for probe '$RelativePath'."
    }
    return $exitCode -eq 0
}

function Assert-DeploymentIgnorePolicies {
    param([string] $RepositoryRoot)

    $probeToken = 'ignore-policy-probe-' + [guid]::NewGuid().ToString('N')
    $ignoredGitProbes = @(
        'deployment.env',
        "deploy/$probeToken/deployment.env",
        "deploy/secrets/$probeToken.runtime-secret",
        "deploy/secrets/$probeToken/gcs-backup-adc.json",
        "deploy/secrets/$probeToken/private.key",
        "deploy/secrets/$probeToken/certificate.crt",
        "deploy/secrets/$probeToken/certificate.pem",
        "deploy/secrets/$probeToken/archive.pfx",
        "deploy/config/$probeToken-password.txt",
        "deploy/config/$probeToken-service-account.json"
    )
    foreach ($probe in $ignoredGitProbes) {
        if (-not (Test-GitPathIgnored -RepositoryRoot $RepositoryRoot -RelativePath $probe)) {
            throw "Git ignore-policy blocker: sensitive probe '$probe' is not ignored."
        }
    }

    $requiredGitVisiblePaths = @(
        'deploy/.env.example',
        'deploy/compose.yaml',
        'deploy/api/entrypoint.sh',
        'deploy/backup/.dockerignore',
        'deploy/backup/Dockerfile',
        'deploy/backup/backup-loop.sh',
        'deploy/backup/healthcheck.sh',
        'deploy/backup/restore-from-gcs.sh',
        'deploy/backup/run-backup.sh',
        'deploy/postgres/init/10-create-roles.sh',
        'deploy/postgres/init/20-apply-authoritative-state.sh',
        'deploy/postgres/init/30-grant-runtime-privileges.sql',
        'deploy/postgres/init/create-roles.psql',
        'deploy/secrets/README.md',
        'deploy/windows/Test-DeploymentConfiguration.ps1'
    )
    foreach ($path in $requiredGitVisiblePaths) {
        if (Test-GitPathIgnored -RepositoryRoot $RepositoryRoot -RelativePath $path) {
            throw "Git ignore-policy blocker: required deployment source '$path' is ignored."
        }
    }

    $gitIgnoreLines = Get-IgnorePolicyLines -Path (Join-Path $RepositoryRoot '.gitignore')
    $approvedSecretExceptions = @('!deploy/secrets/', '!deploy/secrets/README.md')
    $secretExceptions = @($gitIgnoreLines | Where-Object { $_ -like '!deploy/secrets*' })
    if (@(Compare-Object -ReferenceObject $approvedSecretExceptions -DifferenceObject $secretExceptions).Count -ne 0) {
        throw 'Git ignore-policy blocker: deploy/secrets contains an unapproved tracking exception.'
    }
    $environmentExceptions = @($gitIgnoreLines | Where-Object { $_.StartsWith('!') -and $_ -match '(?i)env' })
    if ($environmentExceptions.Count -ne 1 -or $environmentExceptions[0] -ne '!deploy/.env.example') {
        throw 'Git ignore-policy blocker: only deploy/.env.example may bypass environment-file ignores.'
    }

    $rootDockerIgnorePath = Join-Path $RepositoryRoot '.dockerignore'
    $rootDockerIgnoreLines = Get-IgnorePolicyLines -Path $rootDockerIgnorePath
    $requiredRootDockerIgnores = @(
        '**/.env',
        '**/.env.*',
        '**/*.env',
        '**/deployment.env',
        '**/secrets',
        '**/secrets/**',
        '**/*credential*.json',
        '**/*service-account*.json',
        '**/*adc*.json',
        '**/*password*',
        '**/*.crt',
        '**/*.cer',
        '**/*.der',
        '**/*.key',
        '**/*.pem',
        '**/*.pfx',
        '**/*.p12',
        '**/*.jks',
        '**/*.keystore',
        'deploy/config/appsettings.Production.json'
    )
    foreach ($pattern in $requiredRootDockerIgnores) {
        if ($rootDockerIgnoreLines -notcontains $pattern) {
            throw "Docker ignore-policy blocker: the API build context is missing '$pattern'."
        }
    }
    if (@($rootDockerIgnoreLines | Where-Object { $_.StartsWith('!') }).Count -ne 0) {
        throw 'Docker ignore-policy blocker: the API build context must not re-include ignored sensitive paths.'
    }
    foreach ($unsafeExclusion in @('*', '**', 'src', 'src/**', 'db', 'db/**', 'deploy/api', 'deploy/api/**', 'deploy/postgres', 'deploy/postgres/**')) {
        if ($rootDockerIgnoreLines -contains $unsafeExclusion) {
            throw "Docker ignore-policy blocker: required source is excluded by '$unsafeExclusion'."
        }
    }

    $backupDockerIgnorePath = Join-Path $deployRoot 'backup/.dockerignore'
    $backupDockerIgnoreLines = Get-IgnorePolicyLines -Path $backupDockerIgnorePath
    $approvedBackupContextPolicy = @(
        '**',
        '!.dockerignore',
        '!Dockerfile',
        '!backup-loop.sh',
        '!healthcheck.sh',
        '!restore-from-gcs.sh',
        '!run-backup.sh'
    )
    if (@(Compare-Object -ReferenceObject $approvedBackupContextPolicy -DifferenceObject $backupDockerIgnoreLines).Count -ne 0) {
        throw 'Docker ignore-policy blocker: the backup build context must contain only its reviewed Dockerfile and scripts.'
    }
}

function Convert-IdentityToSid {
    param([object] $Identity)

    if ($Identity -is [System.Security.Principal.SecurityIdentifier]) {
        return $Identity.Value
    }
    $account = [System.Security.Principal.NTAccount]::new([string]$Identity)
    return $account.Translate([System.Security.Principal.SecurityIdentifier]).Value
}

function Assert-SecureWindowsAcl {
    param(
        [string] $Path,
        [string] $OperatorSid
    )

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    $acl = Get-Acl -LiteralPath $resolvedPath
    if (-not $acl.AreAccessRulesProtected) {
        throw "Credential ACL blocker: inheritance is enabled on '$resolvedPath'."
    }

    $allowedSids = @(
        $OperatorSid,
        'S-1-5-18',       # NT AUTHORITY\SYSTEM
        'S-1-5-32-544'    # BUILTIN\Administrators
    )
    $rules = @($acl.GetAccessRules($true, $true, [System.Security.Principal.SecurityIdentifier]))
    foreach ($rule in $rules) {
        if ($rule.IsInherited) {
            throw "Credential ACL blocker: inherited access remains on '$resolvedPath'."
        }
        $sid = Convert-IdentityToSid -Identity $rule.IdentityReference
        if ($rule.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow -and
            $sid -notin $allowedSids) {
            throw "Credential ACL blocker: an unauthorized identity has allowed access to '$resolvedPath'."
        }
    }

    foreach ($requiredSid in $allowedSids) {
        if (-not ($rules | Where-Object {
            $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow -and
            (Convert-IdentityToSid -Identity $_.IdentityReference) -eq $requiredSid
        })) {
            throw "Credential ACL blocker: a required protected identity has no explicit allow entry on '$resolvedPath'."
        }
    }

    $ownerSid = Convert-IdentityToSid -Identity $acl.Owner
    if ($ownerSid -notin $allowedSids) {
        throw "Credential ACL blocker: '$resolvedPath' is owned by an unauthorized identity."
    }
}

function Assert-BitLockerProtection {
    param([string] $Path)

    $bitLockerCommand = Get-Command Get-BitLockerVolume -ErrorAction SilentlyContinue
    if ($null -eq $bitLockerCommand) {
        throw 'BitLocker blocker: Get-BitLockerVolume is unavailable on this host.'
    }

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    $mountPoint = [System.IO.Path]::GetPathRoot($resolvedPath).TrimEnd('\')
    try {
        $volume = Get-BitLockerVolume -MountPoint $mountPoint -ErrorAction Stop
    } catch {
        throw "BitLocker blocker: protection state could not be read for drive '$mountPoint'."
    }
    if ($null -eq $volume) {
        throw "BitLocker blocker: no BitLocker volume was found for drive '$mountPoint'."
    }
    if ([string]$volume.VolumeStatus -ne 'FullyEncrypted' -or
        [string]$volume.ProtectionStatus -ne 'On' -or
        [int]$volume.EncryptionPercentage -ne 100) {
        throw "BitLocker blocker: drive '$mountPoint' must be fully encrypted with protection on and not suspended."
    }
    return $mountPoint
}

function Read-ValidatedServiceAccountCredential {
    param(
        [string] $Path,
        [string] $ExpectedProjectId,
        [string] $ExpectedEmail,
        [string] $Purpose
    )

    try {
        $credential = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
    } catch {
        throw "The $Purpose credential file is not valid JSON."
    }
    if ([string]$credential.type -ne 'service_account') {
        throw "The $Purpose credential must be a dedicated service-account JSON credential."
    }
    if ([string]$credential.project_id -ne $ExpectedProjectId) {
        throw "The $Purpose credential does not belong to the approved Google Cloud project."
    }
    if ([string]$credential.client_email -ne $ExpectedEmail) {
        throw "The $Purpose credential does not belong to the approved service-account identity."
    }
    foreach ($property in @('private_key_id', 'private_key', 'client_id', 'token_uri')) {
        if ([string]::IsNullOrWhiteSpace([string]$credential.$property)) {
            throw "The $Purpose credential is missing required service-account material."
        }
    }
    return [pscustomobject]@{
        ProjectId = [string]$credential.project_id
        ClientEmail = [string]$credential.client_email
    }
}

Assert-DeploymentIgnorePolicies -RepositoryRoot $repoRoot

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker CLI is not available.'
}

$settings = Read-DotEnv -Path $resolvedEnvFile
if ($StaticOnly -and $RequirePinnedImages) {
    throw 'StaticOnly cannot be combined with RequirePinnedImages.'
}
if ($StaticOnly -and -not [string]::IsNullOrWhiteSpace($RestoreCredentialFile)) {
    throw 'StaticOnly cannot validate an external restore credential.'
}
$requiredValues = @(
    'ASPNETCORE_ENVIRONMENT',
    'POSSERVER_SOURCE_REVISION',
    'POSSERVER_DB_NAME',
    'GCP_PROJECT_ID',
    'GCP_BACKUP_SERVICE_ACCOUNT_EMAIL',
    'GCP_RESTORE_SERVICE_ACCOUNT_EMAIL',
    'GCS_BUCKET',
    'GCS_PREFIX',
    'GCS_EXPECTED_LOCATION',
    'BACKUP_INTERVAL_SECONDS',
    'BACKUP_RPO_TARGET_SECONDS',
    'RESTORE_RTO_TARGET_SECONDS',
    'BACKUP_RETENTION_DAYS',
    'GOOGLE_CLOUD_CLI_VERSION',
    'POSSERVER_FISCAL_REPORT_ARTIFACT_ROOT'
)
foreach ($name in $requiredValues) {
    Require-Setting -Settings $settings -Name $name
}

$requiredFiles = @(
    'POSSERVER_APP_CONFIG_FILE',
    'POSSERVER_TLS_CERT_FILE',
    'POSSERVER_TLS_KEY_FILE',
    'POSSERVER_POSTGRES_BOOTSTRAP_PASSWORD_FILE',
    'POSSERVER_POSTGRES_ADMIN_PASSWORD_FILE',
    'POSSERVER_POSTGRES_APP_PASSWORD_FILE',
    'POSSERVER_POSTGRES_BACKUP_PASSWORD_FILE',
    'POSSERVER_GCP_BACKUP_CREDENTIALS_FILE',
    'POSSERVER_BACKUP_AGE_RECIPIENT_FILE'
)
if ($settings['POSSERVER_SOURCE_REVISION'] -notmatch '^[0-9a-f]{40}$') {
    throw 'POSSERVER_SOURCE_REVISION must be a full Git commit hash.'
}
$headRevision = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'The checked-out Git revision could not be determined.'
}
if (-not $StaticOnly -and $headRevision -ne $settings['POSSERVER_SOURCE_REVISION']) {
    throw 'POSSERVER_SOURCE_REVISION does not match the checked-out revision.'
}

if ($settings['ASPNETCORE_ENVIRONMENT'] -ne 'Production') {
    throw 'The reviewed deployment configuration requires ASPNETCORE_ENVIRONMENT=Production.'
}

if ($settings['GCP_PROJECT_ID'] -ne $approvedGcpProjectId -or
    $settings['GCS_BUCKET'] -ne $approvedGcsBucket -or
    $settings['GCS_EXPECTED_LOCATION'] -ne $approvedGcsLocation -or
    $settings['GCP_BACKUP_SERVICE_ACCOUNT_EMAIL'] -ne $approvedBackupServiceAccount -or
    $settings['GCP_RESTORE_SERVICE_ACCOUNT_EMAIL'] -ne $approvedRestoreServiceAccount -or
    $settings['BACKUP_INTERVAL_SECONDS'] -ne $approvedBackupIntervalSeconds) {
    throw 'The Google Cloud staging project, bucket, location, service identities, or hourly interval differs from the approved values.'
}
if ($settings['GOOGLE_CLOUD_CLI_VERSION'] -ne $approvedGoogleCloudCliVersion) {
    throw 'GOOGLE_CLOUD_CLI_VERSION differs from the reviewed pinned package version.'
}

if (-not $StaticOnly) {
    foreach ($name in $requiredFiles) {
        Require-ExternalFile -Settings $settings -Name $name
    }
    Require-ExternalDirectory -Settings $settings -Name 'POSSERVER_FISCAL_REPORT_ARTIFACT_ROOT'

    $passwordPaths = @(
        $settings['POSSERVER_POSTGRES_BOOTSTRAP_PASSWORD_FILE'],
        $settings['POSSERVER_POSTGRES_ADMIN_PASSWORD_FILE'],
        $settings['POSSERVER_POSTGRES_APP_PASSWORD_FILE'],
        $settings['POSSERVER_POSTGRES_BACKUP_PASSWORD_FILE']
    ) | ForEach-Object { (Resolve-Path -LiteralPath $_).Path }
    if (($passwordPaths | Sort-Object -Unique).Count -ne $passwordPaths.Count) {
        throw 'Every PostgreSQL role must use a distinct password file.'
    }

    Require-Setting -Settings $settings -Name 'POSSERVER_WINDOWS_DEPLOYMENT_OPERATOR_SID'
    if ($settings['POSSERVER_WINDOWS_DEPLOYMENT_OPERATOR_SID'] -notmatch '^S-1-5-21-(?:\d+-){3}\d+$') {
        throw 'POSSERVER_WINDOWS_DEPLOYMENT_OPERATOR_SID must be the designated Windows user account SID.'
    }
    if ($settings['GCS_PREFIX'] -match '^replace-' -or
        $settings['GCS_PREFIX'] -notmatch '^[A-Za-z0-9](?:[A-Za-z0-9._/-]*[A-Za-z0-9])?$' -or
        $settings['GCS_PREFIX'] -match '(//|\.\.)') {
        throw 'GCS_PREFIX must be the approved site-specific object prefix and must not contain unsafe path segments.'
    }
    if ($settings['BACKUP_INTERVAL_SECONDS'] -notmatch '^\d+$' -or [int64]$settings['BACKUP_INTERVAL_SECONDS'] -lt 300) {
        throw 'BACKUP_INTERVAL_SECONDS must be an integer of at least 300.'
    }
    if ($settings['BACKUP_RPO_TARGET_SECONDS'] -notmatch '^\d+$' -or [int64]$settings['BACKUP_RPO_TARGET_SECONDS'] -lt 300) {
        throw 'BACKUP_RPO_TARGET_SECONDS must be an integer of at least 300.'
    }
    if ($settings['RESTORE_RTO_TARGET_SECONDS'] -notmatch '^\d+$' -or [int64]$settings['RESTORE_RTO_TARGET_SECONDS'] -lt 300) {
        throw 'RESTORE_RTO_TARGET_SECONDS must be an integer of at least 300.'
    }
    if ($settings['BACKUP_RETENTION_DAYS'] -ne 'UNAPPROVED' -and
        ($settings['BACKUP_RETENTION_DAYS'] -notmatch '^\d+$' -or [int64]$settings['BACKUP_RETENTION_DAYS'] -lt 1)) {
        throw 'BACKUP_RETENTION_DAYS must be UNAPPROVED or an approved positive number of days.'
    }

    $applicationConfig = Get-Content -Raw -LiteralPath $settings['POSSERVER_APP_CONFIG_FILE'] | ConvertFrom-Json
    $apiKeyProperties = @($applicationConfig.PosServer.Admin.ApiKeys.PSObject.Properties)
    if ($apiKeyProperties.Count -eq 0) {
        throw 'The production application configuration contains no API-key registrations.'
    }
    foreach ($registration in $apiKeyProperties) {
        $value = $registration.Value
        if ($value.Enabled -ne $true -or [string]::IsNullOrWhiteSpace([string]$value.Key)) {
            throw "API-key registration '$($registration.Name)' is incomplete or disabled."
        }
        if ([string]$value.AuthorityClass -ne 'PRODUCTION') {
            throw "API-key registration '$($registration.Name)' must use PRODUCTION authority."
        }
        if (@($value.Permissions).Count -eq 0) {
            throw "API-key registration '$($registration.Name)' has no permissions."
        }
    }

    $backupCredentialPath = (Resolve-Path -LiteralPath $settings['POSSERVER_GCP_BACKUP_CREDENTIALS_FILE']).Path
    if (-not $backupCredentialPath.Equals($approvedBackupCredentialPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'The backup credential is not installed at the approved Windows host path.'
    }
    $backupCredentialDirectory = Split-Path -Parent $backupCredentialPath
    Assert-SecureWindowsAcl -Path $backupCredentialDirectory -OperatorSid $settings['POSSERVER_WINDOWS_DEPLOYMENT_OPERATOR_SID']
    Assert-SecureWindowsAcl -Path $backupCredentialPath -OperatorSid $settings['POSSERVER_WINDOWS_DEPLOYMENT_OPERATOR_SID']
    $null = Assert-BitLockerProtection -Path $backupCredentialPath
    $backupCredential = Read-ValidatedServiceAccountCredential `
        -Path $backupCredentialPath `
        -ExpectedProjectId $approvedGcpProjectId `
        -ExpectedEmail $approvedBackupServiceAccount `
        -Purpose 'backup'

    if (-not [string]::IsNullOrWhiteSpace($RestoreCredentialFile)) {
        if (-not [System.IO.Path]::IsPathRooted($RestoreCredentialFile) -or
            -not (Test-Path -LiteralPath $RestoreCredentialFile -PathType Leaf)) {
            throw 'RestoreCredentialFile must be an existing absolute external file path.'
        }
        $restoreCredentialPath = (Resolve-Path -LiteralPath $RestoreCredentialFile).Path
        if (-not $restoreCredentialPath.Equals($approvedRestoreCredentialPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'The restore credential is not installed at the approved temporary Windows host path.'
        }
        if ($restoreCredentialPath.StartsWith(([System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'), [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'The restore credential must be stored outside the repository.'
        }
        if ($restoreCredentialPath.Equals($backupCredentialPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'Backup and restore credentials must be separate files and identities.'
        }
        $restoreCredentialDirectory = Split-Path -Parent $restoreCredentialPath
        Assert-SecureWindowsAcl -Path $restoreCredentialDirectory -OperatorSid $settings['POSSERVER_WINDOWS_DEPLOYMENT_OPERATOR_SID']
        Assert-SecureWindowsAcl -Path $restoreCredentialPath -OperatorSid $settings['POSSERVER_WINDOWS_DEPLOYMENT_OPERATOR_SID']
        $null = Assert-BitLockerProtection -Path $restoreCredentialPath
        $restoreCredential = Read-ValidatedServiceAccountCredential `
            -Path $restoreCredentialPath `
            -ExpectedProjectId $approvedGcpProjectId `
            -ExpectedEmail $approvedRestoreServiceAccount `
            -Purpose 'restore'
        if ($restoreCredential.ClientEmail -eq $backupCredential.ClientEmail) {
            throw 'Backup and restore service-account identities must not be interchangeable.'
        }
    }

    $ageRecipient = (Get-Content -Raw -LiteralPath $settings['POSSERVER_BACKUP_AGE_RECIPIENT_FILE']).Trim()
    if ($ageRecipient -notmatch '^age1[0-9a-z]+$') {
        throw 'The backup encryption recipient must be one age X25519 public recipient.'
    }
}

foreach ($imageSetting in @('POSTGRES_IMAGE', 'DOTNET_SDK_IMAGE', 'DOTNET_ASPNET_IMAGE')) {
    Require-Setting -Settings $settings -Name $imageSetting
    if ($settings[$imageSetting] -notmatch '@sha256:[0-9a-f]{64}$') {
        throw "Base image '$imageSetting' is not pinned to an immutable SHA-256 digest."
    }
}
if (-not $StaticOnly -or $RequirePinnedImages) {
    foreach ($imageSetting in @('POSSERVER_API_IMAGE', 'POSSERVER_BACKUP_IMAGE')) {
        Require-Setting -Settings $settings -Name $imageSetting
        if ($settings[$imageSetting] -notmatch '@sha256:[0-9a-f]{64}$') {
            throw "Image '$imageSetting' is not pinned to an immutable SHA-256 digest."
        }
    }
}

$composeJsonText = & docker compose --env-file $resolvedEnvFile --file $composeFile config --format json
if ($LASTEXITCODE -ne 0) {
    throw 'Docker Compose configuration rendering failed.'
}
$compose = $composeJsonText | ConvertFrom-Json
$serviceNames = @($compose.services.PSObject.Properties.Name | Sort-Object)
if (($serviceNames -join ',') -ne 'api,backup,postgres') {
    throw "The deployment must contain exactly api, backup, and postgres services."
}
$postgresPortsProperty = $compose.services.postgres.PSObject.Properties['ports']
if ($null -ne $postgresPortsProperty -and @($postgresPortsProperty.Value).Count -ne 0) {
    throw 'PostgreSQL must not publish a host port.'
}
if (-not $compose.networks.pos_internal.internal) {
    throw 'The POS database network must be internal.'
}
if (-not (@($compose.services.api.networks.PSObject.Properties.Name) -contains 'api_ingress')) {
    throw 'The API service must join the host-facing API network.'
}
if (@($compose.services.api.networks.PSObject.Properties.Name) -contains 'backup_egress') {
    throw 'The API service must not join the backup egress network.'
}
if (@($compose.services.postgres.networks.PSObject.Properties.Name) -contains 'backup_egress') {
    throw 'PostgreSQL must not join the backup egress network.'
}
if (@($compose.services.postgres.networks.PSObject.Properties.Name) -contains 'api_ingress') {
    throw 'PostgreSQL must not join the host-facing API network.'
}
if (@($compose.services.backup.networks.PSObject.Properties.Name) -contains 'api_ingress') {
    throw 'The backup service must not join the host-facing API network.'
}
if ([string]$compose.services.backup.environment.CLOUDSDK_CORE_DISABLE_PROMPTS -ne '1') {
    throw 'The backup service must disable all Google Cloud CLI prompts.'
}
if ([string]$compose.services.backup.environment.CLOUDSDK_CORE_PROJECT -ne $approvedGcpProjectId) {
    throw 'The backup service Google Cloud project does not match the approved staging project.'
}
$backupCredentialSecret = @($compose.services.backup.secrets | Where-Object {
    $_.source -eq 'gcp_backup_credentials' -and $_.target -eq '/run/secrets/gcp_backup_credentials'
})
if ($backupCredentialSecret.Count -ne 1 -or
    [string]$compose.services.backup.environment.CLOUDSDK_AUTH_CREDENTIAL_FILE_OVERRIDE -ne '/run/secrets/gcp_backup_credentials' -or
    [string]$compose.services.backup.environment.GOOGLE_APPLICATION_CREDENTIALS -ne '/run/secrets/gcp_backup_credentials') {
    throw 'The backup credential must be mounted once at the reviewed read-only Compose secret path.'
}
foreach ($serviceName in @('api', 'postgres')) {
    if (@($compose.services.$serviceName.secrets | Where-Object { $_.source -eq 'gcp_backup_credentials' }).Count -ne 0) {
        throw 'The GCP backup credential must be mounted only into the backup service.'
    }
}
$artifactHostRoot = [System.IO.Path]::GetFullPath($settings['POSSERVER_FISCAL_REPORT_ARTIFACT_ROOT']).TrimEnd('\')
$apiArtifactMounts = @($compose.services.api.volumes | Where-Object {
    $_.type -eq 'bind' -and
    [System.IO.Path]::GetFullPath([string]$_.source).TrimEnd('\').Equals($artifactHostRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
    $_.target -eq '/var/lib/exitpass/fiscal-report-artifacts' -and
    ($null -eq $_.PSObject.Properties['read_only'] -or $_.read_only -ne $true)
})
$backupArtifactMounts = @($compose.services.backup.volumes | Where-Object {
    $_.type -eq 'bind' -and
    [System.IO.Path]::GetFullPath([string]$_.source).TrimEnd('\').Equals($artifactHostRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
    $_.target -eq '/var/lib/exitpass/fiscal-report-artifacts' -and
    $_.read_only -eq $true
})
if ($apiArtifactMounts.Count -ne 1 -or $backupArtifactMounts.Count -ne 1 -or
    [string]$compose.services.api.environment.PosServer__FiscalReports__ArtifactRoot -ne '/var/lib/exitpass/fiscal-report-artifacts' -or
    [string]$compose.services.backup.environment.FISCAL_REPORT_ARTIFACT_ROOT -ne '/var/lib/exitpass/fiscal-report-artifacts') {
    throw 'The fiscal report artifact root must be mounted once read/write in API and once read-only in backup at the reviewed container path.'
}
$composeSource = Get-Content -Raw -LiteralPath $composeFile
if ($composeSource -match '(?m)^\s*gcp_restore_credentials:' -or
    $composeSource -match '(?m)^\s*-\s*gcp_restore_credentials\s*$') {
    throw 'The restore credential must not be defined or mounted by the steady-state Compose deployment.'
}

$roleInitializer = Get-Content -Raw -LiteralPath (Join-Path $deployRoot 'postgres/init/10-create-roles.sh')
$schemaInitializer = Get-Content -Raw -LiteralPath (Join-Path $deployRoot 'postgres/init/20-apply-authoritative-state.sh')
$grantInitializer = Get-Content -Raw -LiteralPath (Join-Path $deployRoot 'postgres/init/30-grant-runtime-privileges.sql')
$roleSql = Get-Content -Raw -LiteralPath (Join-Path $deployRoot 'postgres/init/create-roles.psql')
$initializationText = $roleInitializer + $schemaInitializer + $grantInitializer + $roleSql
if ($roleInitializer -notmatch 'database_object_count' -or $schemaInitializer -notmatch 'database_object_count') {
    throw 'Database initialization does not contain both required empty-database guards.'
}
if ($initializationText -match '(?im)^\s*(DROP\s+(DATABASE|SCHEMA)|TRUNCATE\s+)') {
    throw 'A destructive database statement was found in initialization assets.'
}

$backupPublisher = Get-Content -Raw -LiteralPath (Join-Path $deployRoot 'backup/run-backup.sh')
$restoreUtility = Get-Content -Raw -LiteralPath (Join-Path $deployRoot 'backup/restore-from-gcs.sh')
$backupDockerfile = Get-Content -Raw -LiteralPath (Join-Path $deployRoot 'backup/Dockerfile')
if ($backupDockerfile -notmatch [regex]::Escape("ARG GOOGLE_CLOUD_CLI_VERSION=$approvedGoogleCloudCliVersion") -or
    $backupDockerfile -match '(?s)apt-get install.*google-cloud-cli\s*(?:;|\\)') {
    throw 'The backup image must install only the reviewed exact Google Cloud CLI package version.'
}
if ($backupPublisher -notmatch 'CLOUDSDK_CORE_DISABLE_PROMPTS=1' -or
    $restoreUtility -notmatch 'CLOUDSDK_CORE_DISABLE_PROMPTS=1') {
    throw 'Backup and restore utilities must force Google Cloud CLI non-interactive mode.'
}
if ($backupPublisher -notmatch 'COMPLETE\.json' -or $backupPublisher -notmatch 'exitpass-posserver-complete-backup:v1') {
    throw 'The complete-backup publication marker is missing from the backup publisher.'
}
if ($restoreUtility -notmatch 'COMPLETE\.json' -or $restoreUtility -notmatch 'exitpass-posserver-complete-backup:v1') {
    throw 'The restore utility does not require the complete-backup publication marker.'
}
if ($backupPublisher -notmatch 'fiscal-report-artifacts\.tar' -or
    $backupPublisher -notmatch 'fiscal-report-artifacts-manifest\.json' -or
    $backupPublisher -notmatch 'exitpass-posserver-fiscal-report-artifact-manifest:v1' -or
    $restoreUtility -notmatch 'fiscal-report-artifacts\.tar' -or
    $restoreUtility -notmatch 'fiscal-report-artifacts-manifest\.json' -or
    $restoreUtility -notmatch 'RESTORE_FISCAL_REPORT_ARTIFACT_ROOT') {
    throw 'The backup and restore utilities do not contain the reviewed fiscal report artifact coverage.'
}
if (($backupPublisher + $restoreUtility) -match '(?i)gcloud\s+storage\s+(rm|objects\s+delete)') {
    throw 'A destructive GCS command was found in backup or restore assets.'
}
$archiveUploadIndex = $backupPublisher.IndexOf('--content-type=application/octet-stream', [System.StringComparison]::Ordinal)
$checksumUploadIndex = $backupPublisher.IndexOf('--content-type=text/plain', [System.StringComparison]::Ordinal)
$completionUploadIndex = $backupPublisher.IndexOf('--content-type=application/json', [System.StringComparison]::Ordinal)
$successStatusIndex = $backupPublisher.IndexOf('$status_root/last-success.json.tmp', [System.StringComparison]::Ordinal)
if ($archiveUploadIndex -lt 0 -or $checksumUploadIndex -le $archiveUploadIndex -or
    $completionUploadIndex -le $checksumUploadIndex -or $successStatusIndex -le $completionUploadIndex) {
    throw 'Backup publication order must be archive/checksum, completion marker, then local success status.'
}
$uploadCount = [regex]::Matches($backupPublisher, '(?m)^gcloud storage cp').Count
$createOnlyPreconditionCount = [regex]::Matches($backupPublisher, '(?m)^\s*--if-generation-match=0').Count
if ($uploadCount -ne 3 -or $createOnlyPreconditionCount -ne $uploadCount) {
    throw 'Every backup upload must use the create-only generation-match precondition.'
}
$markerDownloadIndex = $restoreUtility.IndexOf('gcloud storage cp "$completion_object"', [System.StringComparison]::Ordinal)
$archiveDownloadIndex = $restoreUtility.IndexOf('gcloud storage cp "$archive_object"', [System.StringComparison]::Ordinal)
if ($markerDownloadIndex -lt 0 -or $archiveDownloadIndex -le $markerDownloadIndex) {
    throw 'Restore must download and validate the completion marker before the encrypted archive.'
}
$restoreCopyCount = [regex]::Matches($restoreUtility, '(?m)^gcloud storage cp').Count
foreach ($requiredDownload in @(
    'gcloud storage cp "$completion_object" "$work_dir/$completion_name" --quiet',
    'gcloud storage cp "$archive_object" "$work_dir/$encrypted_name" --quiet',
    'gcloud storage cp "$checksum_object" "$work_dir/$checksum_name" --quiet'
)) {
    if ($restoreUtility.IndexOf($requiredDownload, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'The restore utility contains an unreviewed Google Cloud Storage transfer.'
    }
}
if ($restoreCopyCount -ne 3) {
    throw 'The restore utility must contain exactly the three reviewed read operations.'
}

$annexRepositoryPath = Join-Path $repoRoot 'src/ExitPass.PosServer.Persistence.Postgres/FiscalReports/PostgresAnnexE1Repository.cs'
$annexRepository = Get-Content -Raw -LiteralPath $annexRepositoryPath
$artifactPublishIndex = $annexRepository.IndexOf('artifactStore.PublishAsync', [System.StringComparison]::Ordinal)
$workbookInsertIndex = $annexRepository.IndexOf('InsertWorkbookAsync', [System.StringComparison]::Ordinal)
$transactionCommitIndex = if ($workbookInsertIndex -ge 0) {
    $annexRepository.IndexOf('transaction.CommitAsync', $workbookInsertIndex, [System.StringComparison]::Ordinal)
} else {
    -1
}
if ($artifactPublishIndex -lt 0 -or $workbookInsertIndex -le $artifactPublishIndex -or $transactionCommitIndex -le $workbookInsertIndex) {
    throw 'The documented Annex E-1 publish-before-database-commit consistency invariant is not present.'
}

[pscustomobject]@{
    Status = 'Valid'
    Revision = $headRevision
    Services = $serviceNames -join ', '
    DatabasePortPublished = $false
    InternalDatabaseNetwork = $true
    EmptyDatabaseGuards = $true
    CompleteBackupProtocol = $true
    DestructiveGcsCommands = $false
    RestoreGcsOperationsReadOnly = $true
    ApprovedGcpProject = $approvedGcpProjectId
    ApprovedGcsBucket = $approvedGcsBucket
    ApprovedGcsLocation = $approvedGcsLocation
    BackupIntervalSeconds = [int]$approvedBackupIntervalSeconds
    BackupCredentialIdentityValidated = (-not $StaticOnly)
    RestoreCredentialIdentityValidated = (-not $StaticOnly -and -not [string]::IsNullOrWhiteSpace($RestoreCredentialFile))
    CredentialAclValidated = (-not $StaticOnly)
    BitLockerValidated = (-not $StaticOnly)
    UnattendedGcloud = $true
    PinnedImagesValidated = (-not $StaticOnly)
    GoogleCloudCliVersion = $approvedGoogleCloudCliVersion
    AnnexPublishBeforeCommit = $true
    ExternalInputsValidated = (-not $StaticOnly)
    GitIgnorePolicyValidated = $true
    DockerIgnorePolicyValidated = $true
    SecretValuesPrinted = $false
    ContainersStarted = $false
}
