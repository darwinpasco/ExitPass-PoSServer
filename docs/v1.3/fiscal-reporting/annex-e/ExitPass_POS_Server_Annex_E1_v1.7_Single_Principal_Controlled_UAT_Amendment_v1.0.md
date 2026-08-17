# ExitPass POS Server Annex E-1 v1.7 Single-Principal Controlled UAT Amendment v1.0

## 1. Decision And Relationship

**Decision: `AUTHORIZED_FOR_ANNEX_E1_V1_7_SINGLE_PRINCIPAL_CONTROLLED_UAT`.**

This amendment supplements, but does not rewrite, the [Controlled UAT Execution Authorization v1.0](ExitPass_POS_Server_Annex_E1_v1.7_Controlled_UAT_Execution_Authorization_v1.0.md). It applies only to `pos-server-annex-e1-synthetic-uat-dataset-specification:v1.7`, authorized ancestor `e6c85e3f80ba2773104416a649ea164baa81a1aa`, and frozen package-root SHA-256 `5d60cb50d7f43b29f79486801a470e57128e2b571c4e50a37494e4a82131b829`.

The earlier `CONTROLLED_UAT_BLOCKED_ENVIRONMENT` result arose solely because separate role assignments were unavailable; no run ID was issued and no validator or runtime resource was started. This amendment permits a practical single-principal retry without claiming independent assurance. It does not execute UAT or create a role record.

## 2. Single-Principal Mode

Single-principal mode is permitted when the organization cannot reasonably staff separate roles for this internal technical UAT. One accountable principal may fill all eight required role slots:

1. Controlled UAT Executor
2. Evidence Recorder
3. Independent Reviewer
4. Authorization Approver
5. Environment Owner
6. Accounting Reviewer
7. Security/Privacy Reviewer
8. Cleanup Verifier

For compatibility with the original authorization, the role record may assign one opaque principal reference to all eight roles. In this mode, the `Independent Reviewer` role slot performs self-review and does not establish independence.

Before execution, the principal must provide one explicit acceptance statement covering all eight roles, every role responsibility, the complete scope boundary, and every mandatory stop condition. The principal may approve their own eight-role combination under this amendment. The original executor/reviewer, executor/evidence-recorder, and executor/cleanup-verifier separation requirements are waived only for this mode.

Every execution, finding, sign-off, cleanup verification, and final report under this mode must be labeled **`SELF-REVIEWED CONTROLLED UAT`**. It must never be described as independently reviewed, independently validated, independently accepted, or independently certified. Accounting, security/privacy, evidence, visual, and cleanup conclusions must each be labeled `SELF-REVIEWED`; cleanup may be self-verified only when the completion report states that fact explicitly.

## 3. Allowed Boundary

Single-principal mode is allowed only while every condition below remains true:

- synthetic data only;
- local and isolated environment;
- invocation-owned resources only;
- no shared development, standing UAT, staging, or Production environment;
- no `exitpass_v12_dev`, HikCentral, payment-provider, BIR system, or external business service access;
- no real customer, payment, vehicle, statutory-benefit, or fiscal data;
- all evidence remains internal technical evidence.

The principal retains full stop authority and must stop immediately if isolation cannot be proven, real data is encountered, a prohibited system or service may be accessed, the governed package has changed, expected evidence is missing, cleanup cannot be completed or verified, or an unexplained deviation or incident occurs. No stop condition may be waived through self-approval.

## 4. Result Taxonomy

A single-principal execution must issue exactly one result:

- `CONTROLLED_UAT_PASSED_SELF_REVIEWED`
- `CONTROLLED_UAT_FAILED_SELF_REVIEWED`
- `CONTROLLED_UAT_BLOCKED_ENVIRONMENT`
- `CONTROLLED_UAT_BLOCKED_AUTHORIZATION`
- `CONTROLLED_UAT_STOPPED_DEVIATION`
- `CONTROLLED_UAT_STOPPED_INCIDENT`

A successful self-reviewed result remains internal technical evidence only. It does not authorize BIR submission, signing or certification, external delivery, Production use, release approval based on independent assurance, Annex E-2 through E-5, or ARTS POSLog.

Independent review may be performed later when evidence will support an external submission, certification, Production decision, or another purpose requiring independent assurance. It is not required merely to complete this internal synthetic UAT.

## 5. Scope And Continuing Controls

This amendment applies only to the Annex E-1 v1.7 local synthetic Controlled UAT. It creates no general governance rule for Production, regulatory submission, or other ExitPass testing.

All baseline, package, environment, execution, evidence, acceptance, stop, cleanup, retention, and BIR-boundary provisions of the original authorization remain effective. Where the original authorization requires role separation, independent review, independent cleanup verification, or its original result taxonomy, this amendment controls only for an explicitly accepted and labeled single-principal execution.
