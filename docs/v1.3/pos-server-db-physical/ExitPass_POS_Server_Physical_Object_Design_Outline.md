# ExitPass POS Server Physical Object Design v1.0 Proposed Outline

## 1. Purpose

This outline proposes the future `ExitPass_POS_Server_Physical_Object_Design_v1.0.md` document. It does not create the final design or any SQL/object artifacts.

## 2. Proposed Document Structure

1. Document Control
2. Approval / Baseline Status
3. Purpose and Scope
4. Approved Baseline References
5. Repository and Artifact Boundary
6. Authority Boundary
7. Physical Design Gate Status
8. Schema and Naming Baseline
9. Physical Object Design Principles
10. Object Design Sequencing
11. Foundation and Controlled Codes
12. Fiscal Identity and Site POS Server Boundary Objects
13. Channel and Terminal Registry Objects
14. Fiscal Document Core Objects
15. Fiscal Lines, Tender, Tax, Discount, and Totals Objects
16. Numbering and Counter State Objects
17. Idempotency and Retry Objects
18. Digital SI URL and Access Audit Objects
19. Reprint Objects
20. Fiscal Adjustment Objects
21. X-read and Z-read Objects
22. BIR Sales Summary and Annex E Objects
23. EJ, POSLog, JSON, and Export Objects
24. Audit Trail Objects
25. Recovery and Tamper-Evident Continuity Objects
26. Security/Privacy Reference Objects
27. Central PMS and Vendor Integration Reference Objects
28. Optional Events/Outbox Objects, If Approved
29. Cross-Object Dependency Plan
30. Validation and Drift-Check Implications
31. Open Questions
32. Risks and Mitigations
33. Non-Decisions
34. Appendices

## 3. Required Content Per Future Object Area

Each future object area should include:

- purpose;
- candidate physical object names, still provisional until artifact creation;
- relationship to logical database design;
- dependencies;
- candidate attributes at design level;
- authority-boundary considerations;
- idempotency/retry impact where relevant;
- retention/partitioning considerations;
- validation implications;
- open questions;
- whether SQL/object artifact creation is approved or still blocked.

## 4. Non-Decisions For Future Draft

The future design draft should not create SQL, Atlas files, migrations, object files, seed/reference/sample data, scripts, CI workflows, source code, DOCX files, or diagrams unless a separate task explicitly approves those artifacts.
