# ExitPass POS Server Docker deployment assets

These assets prepare a repeatable Windows 11 Pro, Docker Desktop, and WSL 2 deployment. They do not provision a production environment, generate credentials, create Google Cloud resources, change the host, or authorize BIR acceptance.

## Topology

| Service | Exposure | Persistence | Purpose |
| --- | --- | --- | --- |
| `api` | HTTPS host binding through `api_ingress`; database access through `pos_internal` | `pos_annex_e1`; external fiscal-report artifact directory | POS Server API and fiscal report artifact publication |
| `postgres` | Internal Docker network only | `pos_pgdata` | PostgreSQL 16 fiscal data |
| `backup` | No inbound ports; GCS egress only | status and failure-staging volumes; read-only Annex E-1 volume and fiscal-report artifact directory | Verified, encrypted database and artifact backup |

The default API binding is `127.0.0.1:8443`. A LAN binding and firewall rule require separate host/network approval. PostgreSQL has no host port mapping and joins only the internal network. The API, PostgreSQL, and backup services share the internal database network; only the API joins `api_ingress`, and only the backup service joins `backup_egress`.

## Asset inventory

- `compose.yaml`: three-service topology, health checks, restart policies, limits, logging, secrets, volumes, and networks.
- `.env.example`: non-secret configuration template.
- `config/appsettings.Production.example.json`: fail-closed application configuration shape.
- `postgres/init/`: empty-volume role creation, authoritative schema initialization, controlled-code load, and grants.
- `backup/`: backup image, scheduler, verification, encryption, GCS upload, health status, and isolated restore script.
- `windows/Test-DeploymentConfiguration.ps1`: non-mutating deployment preflight.
- `certificates/README.md` and `secrets/README.md`: external material requirements.
- `DESIGN-DECISIONS.md`: design choices, limitations, and unresolved approvals.
- `FOLLOW-UP-TASKS.md`: separately scoped .NET 10 upgrade requirement.

## External runtime directory

Create this outside the repository only after authorization. The paths are examples:

```text
C:\ExitPass\Runtime\
  config\
    appsettings.Production.json
    backup-age-recipient.txt
  tls\
    posserver.crt
    posserver.key
  secrets\
    postgres-bootstrap-password.txt
    postgres-admin-password.txt
    postgres-app-password.txt
    postgres-backup-password.txt
    gcs-backup-adc.json
  restore-secrets\
    gcs-restore-adc.json  # temporary; present only for an approved restore drill
  deployment.env
```

The backup credential path is exactly `C:\ExitPass\Runtime\secrets\gcs-backup-adc.json`. Compose mounts it read-only at `/run/secrets/gcp_backup_credentials`. The non-secret host path is visible in rendered Compose configuration, while credential bytes remain outside the image, environment, and rendered output. The restore credential uses the separate temporary host path `C:\ExitPass\Runtime\restore-secrets\gcs-restore-adc.json` and is never declared by the steady-state Compose file.

### Secure Windows credential installation

Run this procedure only in an approved elevated PowerShell session. Obtain the designated deployment operator's user SID from the authorized account record; do not use a shared group SID. The preflight permits explicit access only for that SID, Local System (`S-1-5-18`), and local Administrators (`S-1-5-32-544`). It rejects inheritance, any other allowed identity, an unauthorized owner, or missing explicit entries.

1. Before copying a credential, verify the target drive:

   ```powershell
   Get-BitLockerVolume -MountPoint C: |
     Select-Object MountPoint, VolumeStatus, ProtectionStatus, EncryptionPercentage
   ```

   Continue only when `VolumeStatus` is `FullyEncrypted`, `ProtectionStatus` is `On`, and `EncryptionPercentage` is `100`. An unavailable BitLocker cmdlet, suspended protection, or any other state is a deployment blocker.

2. Create the directory, remove inherited entries, set explicit access, and set an approved owner. Substitute the authorized user SID:

   ```powershell
   $OperatorSid = 'S-1-5-21-REPLACE-WITH-AUTHORIZED-USER-SID'
   $SecretDirectory = 'C:\ExitPass\Runtime\secrets'
   New-Item -ItemType Directory -Path $SecretDirectory -Force | Out-Null
   icacls $SecretDirectory /inheritance:r
   icacls $SecretDirectory /grant:r "*${OperatorSid}:(OI)(CI)(RX)" `
     '*S-1-5-18:(OI)(CI)(F)' '*S-1-5-32-544:(OI)(CI)(F)'
   icacls $SecretDirectory /setowner '*S-1-5-32-544'
   ```

3. Copy the authorized JSON directly from its protected transfer source to `C:\ExitPass\Runtime\secrets\gcs-backup-adc.json`. Do not paste it into a command, terminal log, environment file, repository, or Docker build context. Protect the file independently so it has no inherited access:

   ```powershell
   $CredentialFile = 'C:\ExitPass\Runtime\secrets\gcs-backup-adc.json'
   icacls $CredentialFile /inheritance:r
   icacls $CredentialFile /grant:r "*${OperatorSid}:(R)" `
     '*S-1-5-18:(R)' '*S-1-5-32-544:(F)'
   icacls $CredentialFile /setowner '*S-1-5-32-544'
   ```

4. Put the same operator SID in `POSSERVER_WINDOWS_DEPLOYMENT_OPERATOR_SID`, then run the full preflight. It reads only credential metadata needed to verify `type`, `project_id`, and `client_email`; it never prints private-key material.

Repeat the directory and file steps under `C:\ExitPass\Runtime\restore-secrets` when temporarily installing the restore credential. Run the preflight with `-RestoreCredentialFile`; remove the host file after the approved restore exercise and cloud-side revocation. Deleting a file is not a substitute for revoking its Google Cloud key.

## Application authentication configuration

Copy `config/appsettings.Production.example.json` outside the repository and replace the empty API-key map with reviewed registrations. The checked-in template is intentionally unusable. Each enabled registration needs:

- one externally generated key;
- `AuthorityClass` set to `PRODUCTION` for a Production host;
- least-privilege permissions;
- exact Site POS Server and fiscal identity UUID scopes where required;
- exact three-letter currency scopes where required.

Use separate principals for Central PMS, fiscal reporting, Accounting/Annex E-1, and privileged administration. The existing `deploy/authorization/central-pms.permissions.json` does not grant BIR Summary, Annex E-1, EJ integrity, or fiscal-document void authority.

## PostgreSQL initialization

Initialization is intentionally limited to a brand-new empty `pos_pgdata` volume:

1. The official PostgreSQL entrypoint initializes a PostgreSQL 16 cluster with data checksums and SCRAM host authentication.
2. `10-create-roles.sh` first rejects any user schema, public-schema object, large object, or pre-existing POS Server role. It then reads the three runtime-role password files and creates administration, application, and backup logins in one transaction. The official image entrypoint separately reads the bootstrap password file.
3. `20-apply-authoritative-state.sh` independently repeats the empty-database object check, reads `db/rebuild/pos_sql_apply_order.txt`, and applies every listed object as `posserver_admin`.
4. Generated controlled-code SQL is applied in filename order.
5. `30-grant-runtime-privileges.sql` grants the API `SELECT`, `INSERT`, and `UPDATE`; the backup role receives `SELECT`; public schema/object rights are revoked.

No script drops, truncates, resets, or recreates a database. The official PostgreSQL entrypoint runs these assets only when its data directory is empty, and the two independent database guards stop initialization if the target database already contains user state. A failed initialization leaves the new volume unusable until an operator diagnoses it; these assets provide no automated cleanup or retry against that volume. Do not reuse an existing, failed, unknown, or shared volume. Database upgrade procedures are outside this asset set.

## Google Cloud Storage backup design

The approved bucket must be private and should use uniform bucket-level access and public-access prevention. Bucket creation, IAM, retention, lifecycle, and location configuration are external infrastructure actions and are not performed here.

The approved staging configuration is:

| Setting | Approved value |
| --- | --- |
| Project | `exitpass-staging` |
| Bucket | `exitpass-staging-pos-backups` |
| Location | `asia-southeast1` |
| Backup service account | `exitpass-pos-backup@exitpass-staging.iam.gserviceaccount.com` |
| Restore service account | `exitpass-pos-restore@exitpass-staging.iam.gserviceaccount.com` |
| Interval | `3600` seconds |

`GCS_PREFIX` remains mandatory and site-specific. The operator must supply the approved prefix because these assets do not infer or define a canonical site identifier.

Grant the continuously running backup identity only:

```text
roles/storage.objectCreator
```

Grant it at the approved bucket or managed-folder boundary, never project-wide unless separately justified. Google documents that this role can create objects but cannot view, delete, or overwrite them. Do not grant Object User or Object Admin.

The restore identity receives bucket-scoped `roles/storage.objectViewer` only. It must not receive create, update, or delete permission. The preflight validates the two exact service-account emails and rejects either credential in the other role. Static validation cannot inspect live IAM bindings; an authorized live test must still prove the allow/deny matrix before backups are enabled.

Each scheduled run:

1. creates a PostgreSQL custom-format dump with ownership and ACL data excluded;
2. validates the dump with `pg_restore --list`;
3. archives the immutable Annex E-1 volume while excluding publication temporary files;
4. snapshots the fiscal-report artifact paths, verifies cataloged files against `pos.fiscal_report_artifacts`, and creates a deterministic artifact archive and per-file manifest containing relative path, byte length, and SHA-256;
5. verifies both artifact archives;
6. creates component SHA-256 checksums and a JSON package manifest;
7. verifies every checksum;
8. creates and validates a combined tar package;
9. encrypts the package to an externally managed age X25519 recipient;
10. verifies the encrypted checksum;
11. uploads the encrypted package and checksum to a unique GCS path with generation-match zero;
12. creates and validates `COMPLETE.json`, which binds the backup ID to the exact archive object, checksum object, encrypted SHA-256, and encrypted byte length;
13. uploads `COMPLETE.json` last with generation-match zero.

`COMPLETE.json` is the publication commit point. A directory containing only an archive, only a checksum, or both objects without a valid completion marker is an incomplete set and is never considered restorable. Local success status is written only after the completion-marker upload succeeds.

The service never lists, reads, overwrites, or deletes GCS objects. All three uploads use unique names and `--if-generation-match=0`. The continuously running identity remains create-only; it receives no permission to repair or remove incomplete sets. Successful temporary work files are removed. Failed work directories remain in `pos_backup_staging` for operator investigation and must not be removed without an approved incident procedure.

The backup container pins Google Cloud CLI package `588.0.0-0`. Compose sets `CLOUDSDK_CORE_DISABLE_PROMPTS=1`, both backup and restore scripts export it, and every storage command uses `--quiet`. A missing or invalid unattended credential therefore fails the operation instead of opening a prompt.

### Backup credential rotation and revocation

For an approved routine rotation:

1. A Google Cloud identity administrator creates a replacement key for the exact backup service account through the approved cloud process and transfers it over the approved protected channel. Do not grant a new role.
2. Install it under a new host filename on the BitLocker-protected volume and apply the explicit ACL procedure above.
3. Point a temporary copy of the external environment file at the new filename and run the full preflight. Do not print or inspect the private key in the terminal.
4. During the approved maintenance window, update the real external environment file and recreate only the backup service so its read-only credential mount uses the new file. Prove one create-only publication and the expected read/overwrite/delete denials.
5. Disable the old key in Google Cloud, confirm another scheduled backup succeeds with the replacement, then delete the old key after the approved observation period. Remove the old host file only after cloud-side disablement or deletion is confirmed.

For emergency revocation, the Google Cloud administrator first removes the bucket IAM binding or disables the service account/key according to the incident plan, because deleting the Windows file alone does not revoke issued access. Stop the backup service under incident authority, record its last success state, delete the revoked host credential, install and validate a replacement through the same procedure, restore only `roles/storage.objectCreator`, and re-enable the service after an authorized create-only test. BitLocker protects data at rest; key revocation is the control that invalidates a copied credential.

An authorized permission test must use a unique disposable prefix and must retain its evidence for review. With the backup credential, creating a new object with generation match zero must succeed; reading it, writing the same object name again, and deleting it must each fail with permission or precondition errors. With the restore credential, reading the published object must succeed, while creating a new object, overwriting the existing object, and deleting it must fail. The create-only identity cannot clean up test objects; a separately authorized retention administrator owns any later cleanup.

`BACKUP_INTERVAL_SECONDS` and `BACKUP_START_DELAY_SECONDS` control scheduling. The schedule is elapsed-time based and uses UTC timestamps. `GCS_BUCKET`, `GCS_PREFIX`, `GCS_EXPECTED_LOCATION`, and `GCS_STORAGE_CLASS` are configurable. `BACKUP_RETENTION_DAYS` is recorded in the manifest but causes no deletion; leave it `UNAPPROVED` until Records/Compliance approves bucket-side retention and legal-hold behavior.

### Database and fiscal artifact consistency boundary

The database dump is a transactionally consistent PostgreSQL snapshot. The Annex E-1 and fiscal-report artifact archives are captured afterward. These are sequential captures and are not a cross-resource atomic snapshot.

The current Annex E-1 and fiscal-report implementations publish and verify content-addressed artifact files before inserting and committing their database records. With the database-first backup order and append-only artifact behavior, a database row included in the dump should have its referenced artifact available to the later archive. The backup also checks every live `pos.fiscal_report_artifacts` catalog row against storage before publication. A concurrent generation can still add an unreferenced surplus artifact to an archive when its database transaction is outside the dump snapshot. This invariant depends on the current source behavior and must be re-reviewed if artifact publication or deletion behavior changes.

A quiet period is not required for routine hourly backups under that invariant, but those backups must be described as online sequential captures. A bounded application quiet period is required when an exact database/artifact set is needed for the controlled demonstration restore baseline. The operator must prevent new API writes before the backup begins and keep the system quiet until the completion marker is published. The backup service cannot enforce or attest to that quiet period and does not claim cross-resource atomicity.

### Provisional controlled-demonstration targets

- Backup interval: 3,600 seconds, or hourly.
- RPO target: one hour.
- Restore RTO target: four hours.
- Retention: unapproved; no destructive local or GCS pruning is implemented.
- Recovery acceptance: at least one isolated restore from a valid completion-marked GCS set, followed by database, controlled-code, fiscal-read, Electronic Journal, Annex E-1, and artifact-integrity validation.

These are demonstration planning values, not production acceptance criteria. Actual backup duration, GCS transfer time, and restore-drill evidence must show whether the targets are achievable.

### Backup status

After an authorized deployment, inspect status without reading credentials:

```powershell
docker compose --env-file C:\ExitPass\Runtime\deployment.env -f .\deploy\compose.yaml exec backup `
  jq . /var/lib/exitpass-backup/status/service-status.json

docker compose --env-file C:\ExitPass\Runtime\deployment.env -f .\deploy\compose.yaml exec backup `
  jq . /var/lib/exitpass-backup/status/last-success.json
```

A failed latest attempt makes the backup container unhealthy and writes a failure message to its structured container log. Docker does not automatically restart an unhealthy container, so external monitoring must alert an operator.

## HTTPS

The API listens only on HTTPS port 8443 inside the container. Supply an externally issued PEM certificate chain and private key through `POSSERVER_TLS_CERT_FILE` and `POSSERVER_TLS_KEY_FILE`. Both are mounted read-only. See `certificates/README.md`.

The internal health probe uses the same readiness endpoint with certificate verification disabled only for the loopback probe. Central PMS must validate the certificate chain and DNS name normally.

## Safe configuration validation

The following command reads configuration metadata and renders Compose. It does not build images, start containers, access a database, upload data, or print secret contents:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\deploy\windows\Test-DeploymentConfiguration.ps1 `
  -EnvFile C:\ExitPass\Runtime\deployment.env
```

The full preflight always requires immutable image digests, the pinned Google Cloud CLI package, exact staging values, the exact backup identity, protected NTFS ACLs, and active BitLocker. `-RequirePinnedImages` remains accepted for compatibility but is redundant in full mode. To validate the separately installed restore credential before an isolated drill:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\deploy\windows\Test-DeploymentConfiguration.ps1 `
  -EnvFile C:\ExitPass\Runtime\deployment.env `
  -RestoreCredentialFile C:\ExitPass\Runtime\restore-secrets\gcs-restore-adc.json
```

Before external material exists, run the structural preflight against the checked-in example:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\deploy\windows\Test-DeploymentConfiguration.ps1 `
  -EnvFile .\deploy\.env.example `
  -StaticOnly
```

Static mode checks Compose structure and script inputs without opening external files or asserting that placeholder values are deployable. It cannot be combined with `-RequirePinnedImages`, and the example must never be used to start the stack.

## Authorized deployment sequence

These commands are instructions for a later approved deployment. Do not run them against an existing/shared database or without reviewing the resolved Compose output.

1. Fetch and check out the exact approved revision. Confirm a clean worktree.
2. Prepare the external runtime directory, credentials, application configuration, age recipient, and certificate.
3. Record approved immutable PostgreSQL and .NET base-image digests and an exact Google Cloud CLI package version.
4. In an isolated build/review workflow, build the API and backup images, scan and test them, publish them to the approved registry, and record their immutable digests. This repository does not perform that release step.
5. Put all five approved digest references in the external environment file and run `Test-DeploymentConfiguration.ps1 -RequirePinnedImages`.
6. Review `docker compose ... config` without redirecting it to a shared location.
7. Confirm the intended named volume does not already contain a database. Never remove or reuse an unknown volume.
8. Use `docker compose ... up --detach --no-build postgres` and review initialization logs. Initialization must report successful manifest and controlled-code application.
9. Validate database inventory and role grants using the administration role without changing data.
10. Start `api` with `--no-build`, verify `/health/live` and `/health/ready` over trusted HTTPS, then perform permission-scoped API contract checks.
11. Start `backup` with `--no-build`, observe one successful verified upload, and record the backup ID and status.
12. Perform an isolated restore drill before enabling Central PMS traffic.
13. Only after explicit demonstration authorization, load the approved synthetic dataset and execute the approved scenarios.

## Isolated restoration from GCS

Restoration is never performed against the running POS database. It requires:

- a separate PostgreSQL 16 instance and network;
- a pre-created, empty database whose name contains `restore`, `isolated`, `validation`, `disposable`, or `test`;
- an empty artifact destination;
- a separate empty fiscal-report artifact destination for version 2 backup sets;
- a temporary GCS credential with bucket-scoped `roles/storage.objectViewer`;
- the separately held age private identity;
- a database administration password file for the isolated target.

Run the backup image with its entrypoint overridden to `/opt/exitpass-backup/restore-from-gcs.sh`, mounting those inputs read-only and an empty work/output directory read/write. Set:

```text
BACKUP_ID
GCS_BUCKET
GCS_PREFIX
CLOUDSDK_CORE_PROJECT=exitpass-staging
CLOUDSDK_CORE_DISABLE_PROMPTS=1
CLOUDSDK_AUTH_CREDENTIAL_FILE_OVERRIDE
GOOGLE_APPLICATION_CREDENTIALS
AGE_IDENTITY_FILE
RESTORE_PGHOST
RESTORE_PGPORT
RESTORE_PGDATABASE
RESTORE_PGUSER
RESTORE_PG_PASSWORD_FILE
RESTORE_ANNEX_E1_ROOT
RESTORE_FISCAL_REPORT_ARTIFACT_ROOT
RESTORE_WORK_ROOT
```

Mount `C:\ExitPass\Runtime\restore-secrets\gcs-restore-adc.json` read-only at `/run/secrets/gcp_restore_credentials`, and set both credential variables to that container path. Never mount `gcs-backup-adc.json` for restore. The restore image must use the same approved immutable backup-image digest that passed deployment preflight.

The script first requires and validates `COMPLETE.json`. It rejects a missing marker, a marker for another backup ID, unexpected object paths, invalid hashes, invalid sizes, or any marker that is not `complete`. Only then does it download the exact archive and checksum, require a single matching checksum record, verify encrypted size and SHA-256, decrypt, reject unsafe archive paths and entry types, verify all internal component checksums and the PostgreSQL archive, and check the isolated destinations. For version 2 sets, it verifies every fiscal-report artifact against the per-file manifest and the restored `pos.fiscal_report_artifacts` catalog before publishing the files into the isolated destination. It restores with `--no-owner --no-privileges` and writes `restore-result.json`. It does not create/drop a database or enable application traffic.

After restore, verify schema inventory, controlled codes, representative fiscal-document/X/Z/BIR/Annex reads, Electronic Journal integrity, counter continuity, artifact hashes, and byte-identical exports. A disaster-recovery promotion requires separately creating the runtime roles/grants and an explicit traffic-enable decision.

## Windows startup and recovery

Compose restart policies recover services only after Docker Desktop is running. The assessed host has Docker Desktop auto-start disabled and no automatic Windows logon. An unattended Windows design therefore still requires approval and testing for:

- Docker Desktop startup under the designated service/operator account;
- a delayed scheduled task that runs `docker compose up --detach` only after the engine responds;
- reliable NTP synchronization before PostgreSQL/API startup;
- graceful shutdown and cold-reboot exercises;
- BIOS power-loss recovery and UPS behavior;
- backup-failure, disk-space, certificate-expiry, and readiness alerts.

No host settings or scheduled tasks are changed by these assets.

## Acceptance boundary

Successful Compose rendering, image build, health checks, or restore does not establish production readiness, Controlled UAT authority, BIR acceptance, or authorization to submit Annex outputs. Those remain separate operational, security, accounting, records, and compliance decisions.
