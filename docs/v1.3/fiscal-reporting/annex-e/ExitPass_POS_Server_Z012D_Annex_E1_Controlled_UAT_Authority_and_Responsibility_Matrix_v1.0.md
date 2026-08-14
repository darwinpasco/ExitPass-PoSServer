# ExitPass POS Server Z-012D Annex E-1 Controlled UAT Authority and Responsibility Matrix v1.0

## 1. Roles

| Role | Boundary |
|---|---|
| Product/Fiscal Design Authority | Owns internal gate decisions; cannot claim external BIR approval |
| Controlled UAT Authorizer | May authorize exact data assignment/execution scope in a future record |
| Environment Owner | Builds and operates only the approved isolated environment |
| Synthetic Data Steward | Designs and attests synthetic-only datasets |
| Scenario Executor | Executes only authorized scenarios after all entry gates pass |
| Evidence Custodian | Records and protects governed evidence metadata |
| Technical Reviewer | Reviews implementation, determinism, security, scope, and cleanup evidence |
| Accounting Reviewer | Reviews approved equations and reconciliations; cannot resolve BIR-only questions |
| Security/Privacy Reviewer | Reviews authority, isolation, sensitive-data exclusion, and privacy incidents |
| Cleanup Verifier | Independently confirms removal of invocation-owned resources |
| Blocker Disposition Authority | Determines stop/remediation/review path within approved authority |
| BIR/Accreditation Examiner | External authority for examiner/BIR decisions; pending |
| Legal/Compliance/Records Authority | External/internal governance authority for retention/legal hold; pending |
| External Delivery Authorizer | Separate later authority; not granted by Z-012D or UAT |
| Production Authorizer | Separate later authority; not granted by Z-012D or UAT |

## 2. Responsibility matrix

`A` accountable, `R` responsible, `C` consulted, `I` informed, `PENDING` externally unassigned.

| Activity | Product/Fiscal | UAT Authorizer | Environment Owner | Data Steward | Executor | Evidence Custodian | Technical Reviewer | Accounting Reviewer | Security/Privacy | Cleanup Verifier | External authority |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Approve preparation package | A/R | C | I | I | I | C | C | C | C | I | I |
| Authorize data assignment/execution | C | A/R | C | C | I | I | C | C | C | I | I |
| Prepare isolated environment | I | A | R | I | I | I | C | I | C | C | I |
| Approve synthetic dataset | C | A | I | R | I | I | C | C | C | I | I |
| Execute authorized scenarios | I | A | C | C | R | C | I | I | I | I | I |
| Record evidence | I | A | I | I | C | R | C | C | C | C | I |
| Technical evidence review | I | A | C | I | I | C | R | C | C | C | I |
| Accounting reconciliation review | I | A | I | C | I | C | C | R | I | I | I |
| Privacy/security review | I | A | C | C | I | C | C | I | R | I | I |
| Blocker disposition | A | R | C | C | I | C | C | C | C | I | C/PENDING |
| Cleanup execution | I | A | R | I | C | C | C | I | C | C | I |
| Cleanup verification | I | A | C | I | I | C | C | I | C | R | I |
| Resolve AE-DR-002/004/010/011A/016/020A/024 | I | I | I | I | I | I | C | C | C | I | BIR/examiner `PENDING` |
| Resolve AE-DR-012/019 | I | I | I | I | I | I | C | Accounting R/C | I | I | BIR/examiner as applicable `PENDING` |
| Resolve AE-DR-016B | I | I | I | I | I | I | C | I | C | I | Legal/Records `PENDING` |
| Authorize external delivery | I | I | I | I | I | I | C | C | C | I | External Delivery Authorizer A/R |
| Authorize Production | C | I | C | I | I | C | C | C | C | C | Production Authorizer A/R |

## 3. Separation rules

1. The executor cannot be the sole evidence reviewer or cleanup verifier.
2. Environment ownership does not grant scenario execution authority.
3. Accounting review does not resolve examiner/BIR questions unless the governed decision expressly assigns Accounting sole authority.
4. Successful UAT does not grant external delivery, submission, or Production authority.
5. Personal names must not be embedded in this reusable matrix; assignment records use approved role-to-principal references in the future authorization evidence.

