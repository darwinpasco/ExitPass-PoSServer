# ExitPass POS Server Physical Object Design Sequencing Plan

## 1. Recommended Sequence

| Step | Group | Why this order is safe |
| --- | --- | --- |
| 1 | Foundation and controlled codes | Establishes common status/reason/type value posture before dependent objects. |
| 2 | Fiscal identity and Site POS Server boundary | Defines fiscal authority boundary before channels and fiscal documents. |
| 3 | Channel/terminal registry | Defines child endpoints and capabilities before issuance records reference them. |
| 4 | Fiscal document core | Establishes canonical Sales Invoice and adjustment document headers. |
| 5 | Fiscal lines, tenders, tax, discount, and totals | Depends on fiscal document core and supports reports/exports. |
| 6 | Numbering and counter state | Depends on fiscal authority/document scope; needed before production issuance. |
| 7 | Idempotency and retry | Wraps side-effecting fiscal operations and duplicate prevention. |
| 8 | Digital SI URL and access audit | Depends on issued SI records and Security/Privacy posture. |
| 9 | Reprints and fiscal adjustments | Depend on original document/report references and audit/approval references. |
| 10 | Reports: X-read, Z-read, BIR Sales Summary, Annex E | Depend on canonical documents, lines, totals, and counters. |
| 11 | EJ, POSLog, JSON, and exports | Depend on canonical fiscal records and report/export profile posture. |
| 12 | Audit trail | Cross-cuts all prior groups; physical audit strategy should validate prior object names. |
| 13 | Recovery and tamper-evident continuity | Depends on counters, fiscal state, EJ hash, and audit chain planning. |
| 14 | Security/privacy references | Actor, approval, evidence, and privileged access references finalize after object scope is known. |
| 15 | Optional events/outbox | Last because events must not become authority and depend on final event contract decisions. |

## 2. Sequencing Rules

1. Do not design fiscal document objects before fiscal authority and channel/terminal boundary are clear.
2. Do not design numbering/counter objects before fiscal document scope and Site POS Server scope are clear.
3. Do not design report/export objects before canonical fiscal documents, lines, totals, and counters are planned.
4. Do not design Digital SI URL objects before Security/Privacy confirms enough token/access posture for physical persistence.
5. Do not design outbox/event objects until Engineering Pack confirms whether persistence is needed.
6. Do not create SQL/object files during sequencing or object design planning.

## 3. Authority-Safe Sequencing Notes

- Central PMS references should be planned with integration/reference objects only.
- Payment finality, PaymentAttempt, PaymentConfirmation, and ExitAuthorization remain Central PMS-owned.
- Audit and events remain evidence only.
- ONLINE/OFFLINE status remains observability only and must not enable offline fiscal issuance.
