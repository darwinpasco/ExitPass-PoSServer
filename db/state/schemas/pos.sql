-- ExitPass POS Server first-slice schema artifact.
-- Defines the POS Server-owned fiscal schema boundary.
-- This schema does not own Central PMS payment finality, PaymentAttempt, PaymentConfirmation, ExitAuthorization, or gate execution authority.

CREATE SCHEMA IF NOT EXISTS pos;

COMMENT ON SCHEMA pos IS 'POS Server-owned fiscal schema. Stores POS fiscal boundary, controlled-code, identity, and channel registry objects only; Central PMS payment and exit authority records are reference-only.';

