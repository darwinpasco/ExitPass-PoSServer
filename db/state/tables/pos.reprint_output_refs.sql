-- ExitPass POS Server Slice 5 table artifact.
-- Reprint output reference posture only.
-- This table stores output references and metadata only; it does not store generated PDF, print, or report/export binaries.

CREATE TABLE IF NOT EXISTS pos.reprint_output_refs (
    reprint_output_ref_id uuid NOT NULL,
    reprint_request_id uuid NOT NULL,
    output_type_code_id uuid NOT NULL,
    output_ref text NULL,
    reprint_label_applied boolean NOT NULL DEFAULT true,
    reprinted_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    printed_by_ref text NULL,
    channel_terminal_id uuid NULL,
    output_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_reprint_output_refs PRIMARY KEY (reprint_output_ref_id),
    CONSTRAINT fk_reprint_output_refs__request FOREIGN KEY (reprint_request_id)
        REFERENCES pos.reprint_requests (reprint_request_id),
    CONSTRAINT fk_reprint_output_refs__output_type FOREIGN KEY (output_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_reprint_output_refs__channel FOREIGN KEY (channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT ck_reprint_output_refs__output_ref CHECK (
        output_ref IS NULL OR char_length(btrim(output_ref)) > 0
    ),
    CONSTRAINT ck_reprint_output_refs__printed_by_ref CHECK (
        printed_by_ref IS NULL OR char_length(btrim(printed_by_ref)) > 0
    ),
    CONSTRAINT ck_reprint_output_refs__context_object CHECK (
        output_context IS NULL OR jsonb_typeof(output_context) = 'object'
    )
);

COMMENT ON TABLE pos.reprint_output_refs IS 'Reprint output reference posture. Supports REPRINT and DATE / TIME REPRINTED evidence metadata without storing generated output binaries or report/export objects.';
COMMENT ON COLUMN pos.reprint_output_refs.reprint_label_applied IS 'Indicates whether the required reprint label posture was applied to the referenced output.';
COMMENT ON COLUMN pos.reprint_output_refs.output_ref IS 'Optional output reference only; generated PDF, print, or binary output is not stored here.';
COMMENT ON COLUMN pos.reprint_output_refs.output_context IS 'Flexible output context for unresolved reprint output attributes. Must remain a JSON object when present.';

