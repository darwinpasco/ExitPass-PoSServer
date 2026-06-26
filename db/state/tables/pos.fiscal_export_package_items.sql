-- POS Server Slice 7: fiscal export package item membership posture.
-- This table stores item membership references only and does not store
-- generated export payload content.

CREATE TABLE IF NOT EXISTS pos.fiscal_export_package_items (
    fiscal_export_package_item_id uuid NOT NULL,
    fiscal_export_package_id uuid NOT NULL,
    export_item_type_code_id uuid NOT NULL,
    fiscal_document_id uuid NULL,
    fiscal_report_request_id uuid NULL,
    fiscal_report_output_ref_id uuid NULL,
    electronic_journal_record_id uuid NULL,
    item_ref text NULL,
    item_hash_ref text NULL,
    item_sequence integer NULL,
    item_context jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_fiscal_export_package_items PRIMARY KEY (fiscal_export_package_item_id),
    CONSTRAINT fk_fiscal_export_pkg_items__package
        FOREIGN KEY (fiscal_export_package_id)
        REFERENCES pos.fiscal_export_packages (fiscal_export_package_id),
    CONSTRAINT fk_fiscal_export_pkg_items__item_type
        FOREIGN KEY (export_item_type_code_id)
        REFERENCES pos.controlled_codes (controlled_code_id),
    CONSTRAINT fk_fiscal_export_pkg_items__fiscal_document
        FOREIGN KEY (fiscal_document_id)
        REFERENCES pos.fiscal_documents (fiscal_document_id),
    CONSTRAINT fk_fiscal_export_pkg_items__report_request
        FOREIGN KEY (fiscal_report_request_id)
        REFERENCES pos.fiscal_report_requests (fiscal_report_request_id),
    CONSTRAINT fk_fiscal_export_pkg_items__report_output_ref
        FOREIGN KEY (fiscal_report_output_ref_id)
        REFERENCES pos.fiscal_report_output_refs (fiscal_report_output_ref_id),
    CONSTRAINT fk_fiscal_export_pkg_items__ej_record
        FOREIGN KEY (electronic_journal_record_id)
        REFERENCES pos.electronic_journal_records (electronic_journal_record_id),
    CONSTRAINT ck_fiscal_export_pkg_items__item_ref
        CHECK (item_ref IS NULL OR btrim(item_ref) <> ''),
    CONSTRAINT ck_fiscal_export_pkg_items__item_hash_ref
        CHECK (item_hash_ref IS NULL OR btrim(item_hash_ref) <> ''),
    CONSTRAINT ck_fiscal_export_pkg_items__item_seq_positive
        CHECK (item_sequence IS NULL OR item_sequence > 0),
    CONSTRAINT ck_fiscal_export_pkg_items__context_object
        CHECK (item_context IS NULL OR jsonb_typeof(item_context) = 'object')
);

COMMENT ON TABLE pos.fiscal_export_package_items IS
    'Export package item membership posture. References fiscal documents, reports, output refs, EJ records, or external item refs without storing payload content.';
COMMENT ON COLUMN pos.fiscal_export_package_items.fiscal_export_package_item_id IS
    'Internal identifier for the fiscal export package item.';
COMMENT ON COLUMN pos.fiscal_export_package_items.fiscal_export_package_id IS
    'Export package that owns the item membership row.';
COMMENT ON COLUMN pos.fiscal_export_package_items.export_item_type_code_id IS
    'Controlled code identifying the export item type.';
COMMENT ON COLUMN pos.fiscal_export_package_items.fiscal_document_id IS
    'Optional fiscal document reference included in the package.';
COMMENT ON COLUMN pos.fiscal_export_package_items.fiscal_report_request_id IS
    'Optional fiscal report request reference included in the package.';
COMMENT ON COLUMN pos.fiscal_export_package_items.fiscal_report_output_ref_id IS
    'Optional report output reference included in the package.';
COMMENT ON COLUMN pos.fiscal_export_package_items.electronic_journal_record_id IS
    'Optional Electronic Journal record reference included in the package.';
COMMENT ON COLUMN pos.fiscal_export_package_items.item_ref IS
    'External item reference only; payload content is not stored here.';
COMMENT ON COLUMN pos.fiscal_export_package_items.item_hash_ref IS
    'Item hash reference only; no anchoring implementation is created.';
COMMENT ON COLUMN pos.fiscal_export_package_items.item_context IS
    'Optional package item metadata as a JSON object.';

