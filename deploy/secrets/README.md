# External secret inputs

Do not store runtime secrets in this repository. The Compose configuration expects absolute paths to externally managed files containing:

- bootstrap, administration, application, and backup PostgreSQL passwords;
- Google Application Default Credentials for the write-only GCS backup identity;
- the production application configuration containing API-key registrations.

Each PostgreSQL password must be independently generated and stored in its own one-line file. The backup JSON must be installed exactly at `C:\ExitPass\Runtime\secrets\gcs-backup-adc.json`. It must identify project `exitpass-staging` and `exitpass-pos-backup@exitpass-staging.iam.gserviceaccount.com`. That identity receives bucket-scoped `roles/storage.objectCreator` only on `exitpass-staging-pos-backups`; it receives no object read, list, update, overwrite, or delete permission. Compose mounts the file read-only at `/run/secrets/gcp_backup_credentials` and never copies it into an image.

Isolated restoration uses a separate credential temporarily installed at `C:\ExitPass\Runtime\restore-secrets\gcs-restore-adc.json`. It must identify the same project and `exitpass-pos-restore@exitpass-staging.iam.gserviceaccount.com`, with bucket-scoped `roles/storage.objectViewer` only. Mount it read-only at `/run/secrets/gcp_restore_credentials` only for the isolated restore. Neither it nor the separately protected age private identity is mounted into the continuously running backup service.

Both credential directories and files must disable ACL inheritance and grant explicit access only to the designated operator SID, Local System, and local Administrators. The containing drive must report BitLocker `FullyEncrypted`, protection `On`, and 100 percent encryption. `windows/Test-DeploymentConfiguration.ps1` enforces these conditions, validates the exact identities, and rejects one credential in the other role. See `deploy/README.md` for installation, rotation, revocation, and permission-test procedures.
