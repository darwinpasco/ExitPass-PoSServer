# ExitPass POS Server Electronic Journal v1

## Purpose

This package records the Z-011A canonical fiscal event stream and Electronic Journal implementation. Read it in this order:

1. [Implementation Note](ExitPass_POS_Server_Electronic_Journal_Implementation_Note_v1.0.md)
2. [Event Coverage and Source-Transition Traceability](ExitPass_POS_Server_Electronic_Journal_Event_Coverage_Traceability_v1.0.md)
3. [Gap and Residual-Risk Register](ExitPass_POS_Server_Electronic_Journal_Gap_and_Residual_Risk_Register_v1.0.md)
4. [Machine-readable API contract](../../../../contracts/pos-server/electronic-journal-api.v1.json)

## Readiness

The canonical stream, fiscal-document creation/void/reprint hooks, X/Z/BIR hooks, governed readback, JSON/CSV export, integrity verification, and non-destructive retention controls are implemented. ARTS POSLog 6.0.0 mapping, Annex E, unsupported adjustment/publication writers, physical archival, and destructive purge remain outside Z-011A.

Controlled UAT and production rollout are not authorized by this package.
