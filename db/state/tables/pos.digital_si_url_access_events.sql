-- ExitPass POS Server Slice 5 table artifact.
-- Digital SI URL access event posture only.
-- This table captures access evidence metadata without creating a full audit subsystem or storing credentials.

CREATE TABLE IF NOT EXISTS pos.digital_si_url_access_events (
    digital_si_url_access_event_id uuid NOT NULL,
    digital_si_url_id uuid NOT NULL,
    access_event_type_code_id uuid NOT NULL,
    access_result_code_id uuid NULL,
    accessed_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    accessor_ref text NULL,
    request_ref text NULL,
    channel_terminal_id uuid NULL,
    access_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_digital_si_url_access_events PRIMARY KEY (digital_si_url_access_event_id),
    CONSTRAINT fk_digital_si_url_access_events__url FOREIGN KEY (digital_si_url_id)
        REFERENCES pos.digital_si_urls (digital_si_url_id),
    CONSTRAINT fk_digital_si_url_access_events__type_code FOREIGN KEY (access_event_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_digital_si_url_access_events__result_code FOREIGN KEY (access_result_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_digital_si_url_access_events__channel FOREIGN KEY (channel_terminal_id)
        REFERENCES pos.channel_terminals (channel_terminal_id),
    CONSTRAINT ck_digital_si_url_access_events__accessor_ref CHECK (
        accessor_ref IS NULL OR char_length(btrim(accessor_ref)) > 0
    ),
    CONSTRAINT ck_digital_si_url_access_events__request_ref CHECK (
        request_ref IS NULL OR char_length(btrim(request_ref)) > 0
    ),
    CONSTRAINT ck_digital_si_url_access_events__context_object CHECK (
        access_context IS NULL OR jsonb_typeof(access_context) = 'object'
    )
);

COMMENT ON TABLE pos.digital_si_url_access_events IS 'Digital SI URL access event posture. Captures access metadata only and does not create a full audit subsystem or store credentials.';
COMMENT ON COLUMN pos.digital_si_url_access_events.accessor_ref IS 'Optional accessor reference only; do not store raw sensitive identity evidence in this field.';
COMMENT ON COLUMN pos.digital_si_url_access_events.request_ref IS 'Optional request correlation reference only.';
COMMENT ON COLUMN pos.digital_si_url_access_events.access_context IS 'Flexible access context for unresolved Security/Privacy attributes. Must remain a JSON object when present.';

