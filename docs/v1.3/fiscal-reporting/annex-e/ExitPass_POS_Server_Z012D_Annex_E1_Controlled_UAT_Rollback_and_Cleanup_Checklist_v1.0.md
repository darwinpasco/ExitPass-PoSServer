# ExitPass POS Server Z-012D Annex E-1 Controlled UAT Rollback and Cleanup Checklist v1.0

## 1. Boundary

This checklist applies only to invocation-owned disposable resources created by a separately authorized run. It does not authorize deletion of governed fiscal records, retained evidence, shared infrastructure, standing UAT resources, archives, legal holds, or Production data.

## 2. Stop and containment

- [ ] Stop scenario invocation immediately on a mandatory failure.
- [ ] Record the safe failure classification and correlation reference.
- [ ] Prevent further artifact download or evidence promotion when integrity/privacy is uncertain.
- [ ] Stop only processes labeled with the exact run ID.
- [ ] Confirm no external delivery or submission occurred.
- [ ] Do not retry until the failure owner approves the bounded retry posture.

## 3. Non-destructive rollback

- [ ] Compare the pre-failure database/business-state manifest with expected mutation boundaries.
- [ ] Allow the database transaction to roll back through supported behavior; do not manually repair fiscal rows.
- [ ] Do not update/delete immutable Annex E workbooks, rows, facts, Z reports, BIR Summaries, or Electronic Journal events.
- [ ] Preserve safe metadata needed to prove the failure and rollback.
- [ ] Classify unknown outcome for explicit reconciliation; do not assume rollback or success.

## 4. Invocation-owned resource cleanup

- [ ] Stop and remove run-ID processes and containers.
- [ ] Remove only the exact run-ID database after evidence review authorizes disposable cleanup.
- [ ] Remove only run-ID volumes and networks after resolving absolute/registered targets.
- [ ] Remove run-ID temporary artifact files and proven unreferenced orphans.
- [ ] Remove run-ID test results, coverage, caches, and temporary directories outside the repository.
- [ ] Preserve governed evidence manifests and approved evidence artifacts according to the evidence decision.
- [ ] Never use wildcard, repository-wide clean, broad recursive target, or shared resource deletion.

## 5. Verification

- [ ] Capture post-cleanup process/container/database/volume/network/path manifests.
- [ ] Confirm zero invocation-owned runtime processes remain.
- [ ] Confirm zero invocation-owned containers, databases, volumes, and networks remain.
- [ ] Confirm zero generated XLSX or temporary artifacts remain in the repository.
- [ ] Confirm the Git worktree contains only intended documentation changes or is clean for an execution task.
- [ ] Confirm shared development, standing UAT, staging, and Production resources were untouched.
- [ ] Confirm no Production or real personal data was introduced.
- [ ] Confirm no external endpoint received an artifact or payload.
- [ ] Record cleanup executor and independent verifier roles, timestamp, and result.

## 6. Cleanup evidence template

```text
run_id:
cleanup_authorization_reference:
resources_created_manifest_hash:
resources_removed_manifest_hash:
remaining_run_owned_resources: NOT_EVALUATED
shared_resources_untouched: NOT_EVALUATED
production_data_absent: NOT_EVALUATED
external_delivery_absent: NOT_EVALUATED
cleanup_executor_role:
cleanup_verifier_role:
completed_at:
result: NOT_EVALUATED
failure_classification:
```

Any uncertainty results in `UAT_CLEANUP_FAILURE` and blocks run closure.

