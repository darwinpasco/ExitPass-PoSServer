# ExitPass POS Server deployment decisions

## Accepted design

1. The steady-state deployment contains exactly three services: `api`, `postgres`, and `backup`.
2. All three services share an internal database bridge. PostgreSQL publishes no host port and joins no external bridge.
3. The API alone joins the host-facing bridge. The backup service alone joins a separate bridge with outbound access for Google Cloud Storage.
4. Kestrel terminates HTTPS so a fourth reverse-proxy container is unnecessary.
5. PostgreSQL initialization runs only through the official image's empty-data-directory initialization lifecycle. Role and schema initialization independently reject user objects; role initialization also rejects pre-existing POS Server roles. There is no database drop, truncate, reset, or reuse path.
6. The repository manifest and generated controlled-code SQL are the initialization sources of truth.
7. PostgreSQL roles are fixed and separate: `posserver_admin`, `posserver_app`, `posserver_backup`, plus the image bootstrap superuser used only for initialization.
8. The API uses Npgsql's `Passfile` connection-string option. The role password is supplied by an external file and is not embedded in Compose or image metadata.
9. Annex E-1 artifacts use a named volume shared read/write with the API and read-only with the backup service. Fiscal report artifacts use an external durable Windows directory bind-mounted read/write into the API and read-only into the same backup service.
10. Backup packages combine a PostgreSQL custom archive, Annex E-1 and fiscal report artifact archives, a per-file fiscal artifact manifest, checksums, and the package manifest. Packages are encrypted with age before upload.
11. The running GCS identity has bucket-scoped `roles/storage.objectCreator` only. Every upload uses a unique object path and `if-generation-match=0` to prevent replacement.
12. A backup becomes publish-complete only when the encrypted archive and checksum have uploaded and a validated `COMPLETE.json` marker is uploaded last. Partial object sets remain incomplete and are rejected by restore. Fiscal report files are enumerated before archiving, hashed individually, and checked against restored database catalog rows during an isolated restore.
13. Restoration uses a different, temporarily mounted `roles/storage.objectViewer` identity and age private identity. Neither is available to the running backup service.
14. No destructive local or GCS retention pruning is implemented. The configured retention value is evidence for a future approved bucket-side policy.
15. The branch remains on .NET 8. Migration to .NET 10, dependency review, regression testing, and deployment qualification are a separate follow-up task.
16. Provisional demonstration planning uses hourly backups, a one-hour RPO target, a four-hour restore RTO target, no destructive pruning, and mandatory isolated restore validation.
17. Staging is fixed to project `exitpass-staging`, private bucket `exitpass-staging-pos-backups` in `asia-southeast1`, and a 3,600-second interval. The object prefix remains a required site-specific operator decision.
18. The Windows preflight validates exact, separate backup and restore service-account identities, protected NTFS ACLs, and active full-drive BitLocker before credentials may be used.
19. Base images use reviewed immutable digests, deployment application images must be supplied by immutable registry digest, and Google Cloud CLI is pinned to package version `588.0.0-0`.

## Deliberate limitations

- The backup design provides scheduled logical backups. It does not provide PostgreSQL point-in-time recovery or WAL archiving.
- The backup service verifies archive structure and checksums before upload. Its write-only GCS identity cannot download an object for post-upload readback.
- The database dump and Annex E-1 archive are captured sequentially and are not cross-resource atomic. Current source publishes an immutable artifact before committing its database row, so database-first capture should preserve referenced artifacts while allowing surplus files from concurrent work. An exact demonstration baseline requires an operator-enforced quiet period.
- The encrypted package, checksum, and completion marker are separate create-only objects. A failure before the marker upload can leave an incomplete set that restore rejects and the running identity cannot delete.
- `GCS_EXPECTED_LOCATION` is recorded in every manifest but the write-only identity cannot query bucket metadata. An infrastructure operator must verify the bucket location separately.
- The API readiness endpoint verifies database connectivity with `SELECT 1`; it does not prove schema completeness, credentials, Annex storage, fiscal configuration, or Electronic Journal integrity.
- Container-to-container PostgreSQL traffic is isolated to the internal Docker network but is not TLS-encrypted.
- Docker Desktop cold-boot startup, Windows firewall configuration, trusted time synchronization, UPS, and certificate issuance remain host/operator responsibilities. Credential use is blocked unless the preflight can verify active full-drive BitLocker.
- The example fixes reviewed base-image digests and the Google Cloud CLI package. API and backup release-image digests remain required outputs of the approved build and registry process.
- The application targets .NET 8, whose Microsoft support ends on 2026-11-10. This branch deliberately preserves .NET 8. A .NET 10 upgrade is a separate follow-up task and is required before production use beyond the .NET 8 support date.
- The repository has no `global.json` or NuGet lock file. Immutable SDK image digests constrain the toolchain, but a release build still needs dependency-locking and artifact-provenance evidence.
- Database upgrades for a non-empty database are not defined. They require a separately reviewed, backup-protected state-reconciliation procedure.
- Annex E-1 remains a bounded local implementation. These assets do not authorize Controlled UAT, external delivery, BIR submission, or production use.

## Decisions still required

- Approved API DNS name, certificate issuer, renewal owner, and trust distribution.
- Central PMS, reporting, accounting, and privileged-operation principals, permissions, and exact site/fiscal/currency scopes.
- Site-specific GCS object prefix, storage class, uniform-access policy, public-access prevention, and live IAM verification for the approved staging identities.
- Production backup RPO, restore RTO, schedule interval, local failure-staging handling, and approved retention/legal-hold policy. The one-hour/four-hour values are provisional demonstration targets only.
- Whether logical backups satisfy production recovery objectives or WAL-based recovery is required.
- Immutable registry digests for the built API and backup release images.
- Separate .NET 10 upgrade task covering compatibility, dependency locking, regression evidence, image updates, and deployment qualification.
- Production database upgrade and rollback governance.
- Windows unattended-start design or a production Linux host.
- Monitoring destination and operator notification channel for unhealthy or failed backups.
