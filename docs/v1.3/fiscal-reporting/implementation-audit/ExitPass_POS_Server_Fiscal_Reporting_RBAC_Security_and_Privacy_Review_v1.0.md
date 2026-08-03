# ExitPass POS Server Fiscal Reporting RBAC, Security, and Privacy Review v1.0

## 1. Verdict

Report RBAC is **NOT_IMPLEMENTED**. The only named administration policy is `SalesInvoiceHeaderProfileAdministration`, backed by the server-derived `sales_invoice_header_profile.admin` permission. No reporting permission or route exists.

## 2. Required Permission Separation

| Action | Required authority posture | Current status |
| --- | --- | --- |
| Generate X | Operational report permission scoped to Site POS Server | Missing |
| Close/generate Z | Separate privileged irreversible-close permission | Missing |
| Read/history | Scoped report-read permission | Missing |
| Reprint | Scoped report-reprint permission and durable reason/actor | Missing |
| Export BIR/Annex/POSLog | Separate compliance-export permission | Missing |
| Query/export EJ | Restricted audit permission | Missing |
| Reconcile | Finance/fiscal reconciliation permission | Missing |
| Configure reporting | Separate administration permission | Missing |

Compatibility roles or caller-supplied permission text must not grant these actions. Permissions must come from a trusted authenticated identity, with Site/Site POS Server scope enforced server-side. APT, WebPay, cashier, and ordinary fiscal-document read credentials must not inherit Z-close or compliance-export authority.

## 3. Audit Evidence Required

Durable evidence should identify operation, server-derived principal reference, scoped Site POS Server, correlation/support reference, request identity, timestamp, result, report/export identity, and close-state transition. It must not log full report payloads or statutory/customer identity.

## 4. Security Findings

- No report endpoint currently expands attack surface, but required controls are absent rather than proven.
- Generic export references have no implemented path validation, download authorization, checksum verification, or retention enforcement.
- No report-specific rate limit, anti-replay, or idempotency contract exists.
- No safe report error contract distinguishes validation, conflict, retryable outage, and uncertain commit.
- Database controlled-code FKs do not enforce family membership for report classifications.

## 5. Privacy Posture

Fiscal reports should contain aggregated fiscal facts and approved machine/site metadata. They must exclude beneficiary names, Senior Citizen/PWD identifiers, evidence images or URLs, reviewer identity, credentials, authorization headers, raw requests, and internal semantic hashes.

Annex E and POSLog remain compliance dependencies. If an approved external format requires any customer-level field, its purpose, minimization, access, retention, and customer visibility must be frozen before implementation. Generic JSON/context fields must not become an uncontrolled personal-data channel.

## 6. Scan Result

Repository search finds connection-string and credential terminology in local-development configuration, test infrastructure, runbooks, and validation scripts. No secret value was copied into this audit package. No production credential, private key, customer data, raw statutory ID, or evidence image was introduced by this task.

