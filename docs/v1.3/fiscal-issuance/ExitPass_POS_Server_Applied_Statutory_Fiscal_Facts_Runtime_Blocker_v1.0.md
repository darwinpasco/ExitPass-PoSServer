# ExitPass POS Server Applied Statutory Fiscal Facts Runtime Status v1.0

## Decision

The original Z-003 runtime blocker was:

`BLOCKED_BY_PERSISTENCE_SCHEMA`

That blocker was valid because the then-current POS Server schema did not contain a first-class
`pos.fiscal_document_applied_statutory_facts` snapshot object or the governed controlled-code
families required by the Z-002 contract. POS Server could not safely accept statutory fiscal facts
without durable first-class persistence.

Z-004 resolved the schema blocker through PR #83. The merged baseline now supplies the statutory
snapshot table, controlled-code families, seed values, keys, constraints, indexes, immutability
trigger, rebuild order, expected inventories, drift validation, and direct PostgreSQL proof.

This Z-003 resumption activates runtime support against that merged schema.

## Runtime Implementation

`POST /v1/fiscal-documents` accepts optional `appliedStatutoryFiscalFacts`.

When the object is absent, ordinary fiscal issuance remains on the existing
`sha256:v1` semantic source and persists no statutory snapshot row.

When the object is present, POS Server:

- validates the governed final applied statutory fiscal facts before idempotency resolution;
- rejects incomplete, unsupported, contradictory, non-final, or privacy-prohibited facts;
- uses `pos-server-fiscal-document-create:sha256:v2`;
- persists exactly one immutable row in `pos.fiscal_document_applied_statutory_facts`;
- resolves controlled codes by stable code family and key;
- commits the fiscal document and statutory snapshot in the same PostgreSQL transaction;
- returns idempotent replay from the original durable snapshot;
- returns deterministic semantic conflict for material request changes;
- exposes only the approved safe statutory readback object;
- renders only approved statutory fiscal rows in the Digital Sales Invoice presentation.

POS Server does not calculate entitlement, select ordinance policy, recalculate tariff, retrieve
evidence, store beneficiary/reviewer identity, contact Operator Console, contact WebPay, contact
APT, contact HikCentral, or authorize gates.

## Validation Coverage

Focused validation covers:

- request binding for absent, complete, unknown, prohibited, and incomplete statutory objects;
- finality, controlled-code, currency, monetary, VAT, tariff, policy, and terminal-cash validation;
- ordinary `sha256:v1` representative hash compatibility;
- statutory `sha256:v2` canonicalization and material mutation behavior;
- ordinary/statutory idempotent replay and semantic conflict;
- PostgreSQL insertion into `pos.fiscal_document_applied_statutory_facts`;
- replay without duplicate statutory rows or fiscal numbers;
- conflict without mutation;
- safe GET readback;
- safe Digital Sales Invoice presentation;
- privacy exclusion from public responses and presentation rows.

## Remaining Limitations

- Central PMS producer changes are not implemented in this POS Server branch.
- WebPay, APT, Operator Console, and Management Platform integrations are not implemented here.
- Controlled UAT remains unauthorized until Central PMS supplies final payment-time statutory facts
  and the end-to-end contract is proven.
- Production rollout remains unauthorized.

## Runtime Handoff

After merge, the next cross-lane task should connect Central PMS issuance to this POS Server
contract using `appliedStatutoryFiscalFacts` only after Central PMS has finalized the payment-time
application and payable basis.
