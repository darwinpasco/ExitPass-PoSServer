# ExitPass POS Server Z-012D Annex E-1 Controlled UAT Environment Readiness Checklist v1.0

## 1. Authorization and baseline

- [ ] A separate authorization explicitly permits data assignment and scenario execution.
- [ ] Repository commit, runtime version, profile, calculation-profile hash, template hash, and renderer version are recorded.
- [ ] The environment is classified `CONTROLLED_UAT_ISOLATED_SYNTHETIC`, never Production.
- [ ] The approved scenario and synthetic dataset identifiers are recorded.
- [ ] No unresolved external decision is represented as resolved.

## 2. Isolation and data

- [ ] PostgreSQL 16 uses a new invocation-owned database with a run-ID name.
- [ ] No shared development, standing UAT, staging, or Production database is configured or reachable.
- [ ] No shared volume, network, artifact directory, or evidence directory is reused.
- [ ] Fixtures are synthetic-only and approved before loading.
- [ ] No real customer, fiscal, statutory, payment, plate, ticket, or Production data is present.
- [ ] Outbound access to external delivery, email, BIR submission, and cloud transfer is denied.
- [ ] No external service credential is configured.

## 3. Application and authority

- [ ] The exact approved application build is identified by commit and binary/package hash.
- [ ] Required configuration keys are present, but evidence records names/status only, never values.
- [ ] Artifact root is invocation-owned, local, writable only by the test service identity, and outside the repository.
- [ ] Evidence root is separate from artifact storage and access controlled.
- [ ] Site POS Server, fiscal identity, and PHP scopes are synthetic and exact.
- [ ] Generation, read, download, fact-recording, zero-attestation, and correction permissions are assigned separately.
- [ ] Wrong-scope and missing-permission principals are synthetic and non-authoritative.
- [ ] Production hosting rejects fixture/development authority.
- [ ] No caller-supplied permission, role, or scope override is accepted.

## 4. Resource ownership and observability

- [ ] Every process, container, database, volume, network, temporary directory, and evidence location carries the run ID.
- [ ] Resource owner and cleanup verifier roles are assigned.
- [ ] Pre-run container, database, volume, network, filesystem, and Git manifests are captured safely.
- [ ] Logs are privacy-safe and exclude payloads, credentials, personal data, and stack traces from governed evidence.
- [ ] Time source and timezone behavior are recorded without changing governed timestamps.
- [ ] Available disk capacity is sufficient for deterministic artifacts and evidence.

## 5. Rollback and cleanup readiness

- [ ] Stop conditions are understood by executor and environment owner.
- [ ] Invocation-owned process/container stop commands are reviewed.
- [ ] Database and volume removal targets are exact and run-ID scoped.
- [ ] Artifact/evidence preservation boundaries are documented.
- [ ] Cleanup cannot target shared paths, wildcard resources, or governed retained evidence.
- [ ] A second role is assigned to verify cleanup.
- [ ] Post-cleanup checks prove no run-owned process, container, database, volume, network, or temporary artifact remains.

## 6. Readiness decision template

```text
run_id:
repository_commit:
environment_identifier:
synthetic_dataset_identifier:
readiness_result: NOT_EVALUATED
blocking_check_ids:
environment_owner_role:
security_reviewer_role:
cleanup_verifier_role:
review_timestamp:
```

Failure of any mandatory check results in `UAT_ENVIRONMENT_UNSAFE`; no data may be assigned and no scenario may execute.

