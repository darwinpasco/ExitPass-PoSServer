# ExitPass POS Server Physical DB Schema Naming Open Questions

## 1. Physical DB Design

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Should all first-pass objects use only the `pos` schema? | Recommended default is `pos`; additional schemas require explicit approval. | Physical DB Design | Blocks final schema artifacts if unresolved. | Confirm during first physical object design task. |
| Are plural table names the final convention? | Recommended default is plural table names. | Physical DB Design / Engineering | Blocks final table artifact naming. | Confirm before table artifact creation. |
| Should table files be flat or domain-subfoldered under `db/state/tables/`? | Either `<schema>.<table>.sql` or `<domain>/<table>.sql` may be acceptable pending tooling. | Physical DB Design / Engineering | Blocks object file layout. | Decide before first table file task. |
| Should views/functions/triggers use prefixes such as `vw_`, `fn_`, and `trg_`? | Prefixes are recommended where they improve inventory clarity. | Physical DB Design / Engineering | Blocks view/routine/trigger artifacts. | Confirm before those object types are created. |

## 2. BIR / Accounting

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| What is the BIR-approved SI numbering display format? | Keep SI number distinct from internal ID. | BIR / Accounting | Blocks production numbering artifacts. | Confirm before fiscal sequence objects. |
| What is the exact adjustment numbering format by adjustment family? | Use configurable family-specific sequence posture. | BIR / Accounting | Blocks production adjustment sequence artifacts. | Confirm before adjustment sequence objects. |
| What names should be used for BIR-confirmed adjustment families? | Use approved BIR/accounting terms when confirmed. | BIR / Accounting | Blocks controlled-code finalization. | Confirm during controlled-code package. |
| What BIR/report classifications require governed controlled codes? | Favor controlled-code governance for evolving BIR classifications. | BIR / Accounting | Blocks reference-data finalization. | Confirm before reference-data artifacts. |

## 3. Security / Privacy

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| What are the exact Digital SI URL token/access/expiry/authentication names? | Use `digital_si_url` lifecycle naming and avoid QR-as-fiscal names. | Security / Privacy | Blocks Digital SI URL object artifacts. | Complete Security/Privacy Review. |
| Are separate security/privacy schemas justified? | Default is primary `pos` schema; extra schemas deferred. | Security / Privacy / Physical DB Design | May block security-sensitive artifacts. | Decide if row-level security or isolation requires it. |
| How should evidence references be named when evidence storage is external? | Use `_ref` and source-specific names. | Security / Privacy | Blocks evidence object naming. | Confirm during evidence model design. |

## 4. Engineering Pack

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| Will an outbox/event persistence pattern be used? | Optional only; events are evidence/integration/observability only. | Engineering Pack | Blocks event/outbox artifacts only. | Decide in event contract / Engineering Pack. |
| Should generated artifacts be allowed? | Generated vs hand-authored boundary must be explicit. | Engineering Pack | Blocks generated artifact workflow. | Decide before generator/tooling adoption. |
| What routine/function naming pattern should be used if routines are approved? | Verb-domain-purpose or explicit `fn_` prefix remains open. | Engineering Pack / Physical DB Design | Blocks function artifacts. | Confirm before routine creation. |

## 5. CI/CD

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| What script extension and runner conventions will validation/rebuild/drift use? | Naming patterns documented; no scripts created. | CI/CD / Engineering | Blocks script artifacts. | Confirm tooling before scripts. |
| How will schema inventory evidence files be named and stored? | Evidence required by plan; exact naming open. | CI/CD | Blocks evidence workflow. | Define with validation tooling. |
| How will drift reports be named and retained? | Drift must be reported, not automatically promoted. | CI/CD / Operations | Blocks drift tooling. | Define with drift-check task. |

## 6. Accreditation

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| What final ARTS POSLog profile and mapping names are accepted? | ARTS POSLog 6.x is default structured export reference where practical. | BIR / Accreditation / Engineering Pack | Blocks final POSLog artifacts. | Confirm in accreditation package work. |
| What sample/accreditation file names are required? | No sample/accreditation data created by this package. | BIR / Accreditation | Blocks sample artifacts. | Confirm with package preparation. |
| Which BIR Annex outputs need governed code names? | Annex E support is required; exact package naming pending. | BIR / Accreditation / Accounting | Blocks final code/reference names. | Confirm during reporting/export design. |

## 7. Vendor / Supplier

| Question | Current posture | Owner | Blocker status | Target resolution step |
| --- | --- | --- | --- | --- |
| What exact supplier/accreditation metadata names are required? | Use explicit supplier/vendor reference naming and avoid vendor authority. | Vendor / Supplier / Accreditation | May block supplier metadata artifacts. | Confirm during supplier/accreditation review. |
| What HikCentral/vendor acknowledgement identifiers are stored? | Use `vendor_ack_ref` or source-specific reference names. | Vendor / Supplier / Engineering | Blocks vendor reference artifacts. | Confirm integration payloads. |
