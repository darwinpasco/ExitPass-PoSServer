-- ExitPass POS Server Slice 5 table artifact.
-- Digital SI URL lifecycle posture only.
-- This table stores URL and token references only; it does not store QR images, raw tokens, credentials, or create fiscal mutation paths.

CREATE TABLE IF NOT EXISTS pos.digital_si_urls (
    digital_si_url_id uuid NOT NULL,
    fiscal_document_id uuid NOT NULL,
    digital_si_url_status_code_id uuid NOT NULL,
    digital_si_url_ref text NULL,
    access_token_ref text NULL,
    issued_at timestamptz NULL,
    expires_at timestamptz NULL,
    revoked_at timestamptz NULL,
    blocked_at timestamptz NULL,
    status_reason_code_id uuid NULL,
    status_reason_text text NULL,
    digital_si_url_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_digital_si_urls PRIMARY KEY (digital_si_url_id),
    CONSTRAINT fk_digital_si_urls__document FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_digital_si_urls__status_code FOREIGN KEY (digital_si_url_status_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_digital_si_urls__reason_code FOREIGN KEY (status_reason_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT ck_digital_si_urls__url_ref CHECK (
        digital_si_url_ref IS NULL OR char_length(btrim(digital_si_url_ref)) > 0
    ),
    CONSTRAINT ck_digital_si_urls__access_token_ref CHECK (
        access_token_ref IS NULL OR char_length(btrim(access_token_ref)) > 0
    ),
    CONSTRAINT ck_digital_si_urls__expires_after_issued CHECK (
        issued_at IS NULL OR expires_at IS NULL OR expires_at >= issued_at
    ),
    CONSTRAINT ck_digital_si_urls__revoked_after_issued CHECK (
        issued_at IS NULL OR revoked_at IS NULL OR revoked_at >= issued_at
    ),
    CONSTRAINT ck_digital_si_urls__blocked_after_issued CHECK (
        issued_at IS NULL OR blocked_at IS NULL OR blocked_at >= issued_at
    ),
    CONSTRAINT ck_digital_si_urls__reason_text CHECK (
        status_reason_text IS NULL OR char_length(btrim(status_reason_text)) > 0
    ),
    CONSTRAINT ck_digital_si_urls__context_object CHECK (
        digital_si_url_context IS NULL OR jsonb_typeof(digital_si_url_context) = 'object'
    )
);

COMMENT ON TABLE pos.digital_si_urls IS 'Digital SI URL lifecycle posture for read-only customer view references. Does not store QR image binaries, raw tokens, credentials, or create fiscal mutation paths.';
COMMENT ON COLUMN pos.digital_si_urls.digital_si_url_ref IS 'Reference to the Digital SI URL or URL handle; reference only.';
COMMENT ON COLUMN pos.digital_si_urls.access_token_ref IS 'Reference to access token material or token handle only; raw token and credential storage are outside this table.';
COMMENT ON COLUMN pos.digital_si_urls.digital_si_url_context IS 'Flexible Digital SI URL context for unresolved Security/Privacy attributes. Must remain a JSON object when present.';

