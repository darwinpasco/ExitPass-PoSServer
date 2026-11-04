-- Additive POS fiscal report actor display-name migration.
-- Apply only to a verified non-production database with psql ON_ERROR_STOP enabled.

BEGIN;

SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';

LOCK TABLE pos.fiscal_report_requests, pos.annex_e1_workbooks IN ACCESS EXCLUSIVE MODE;

CREATE TEMP TABLE fiscal_report_actor_display_name_preflight ON COMMIT DROP AS
SELECT
    EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema='pos' AND table_name='fiscal_report_requests'
          AND column_name='requested_by_display_name'
    ) AS request_column_existed,
    EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema='pos' AND table_name='annex_e1_workbooks'
          AND column_name='generated_by_display_name'
    ) AS workbook_column_existed,
    (SELECT count(*) FROM pos.fiscal_report_requests) AS request_row_count,
    (SELECT md5(COALESCE(string_agg(
        (to_jsonb(row_value)-'requested_by_display_name')::text,
        '' ORDER BY fiscal_report_request_id::text), ''))
     FROM pos.fiscal_report_requests row_value) AS request_fingerprint,
    (SELECT count(*) FROM pos.annex_e1_workbooks) AS workbook_row_count,
    (SELECT md5(COALESCE(string_agg(
        (to_jsonb(row_value)-'generated_by_display_name')::text,
        '' ORDER BY annex_e1_workbook_id::text), ''))
     FROM pos.annex_e1_workbooks row_value) AS workbook_fingerprint;

ALTER TABLE pos.fiscal_report_requests
    ADD COLUMN IF NOT EXISTS requested_by_display_name text NULL;

ALTER TABLE pos.annex_e1_workbooks
    ADD COLUMN IF NOT EXISTS generated_by_display_name text NULL;

DO $$
DECLARE
    request_definition text;
    workbook_definition text;
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema='pos' AND table_name='fiscal_report_requests'
          AND column_name='requested_by_display_name' AND data_type='text' AND is_nullable='YES'
    ) THEN
        RAISE EXCEPTION 'pos.fiscal_report_requests.requested_by_display_name must be nullable text';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema='pos' AND table_name='annex_e1_workbooks'
          AND column_name='generated_by_display_name' AND data_type='text' AND is_nullable='YES'
    ) THEN
        RAISE EXCEPTION 'pos.annex_e1_workbooks.generated_by_display_name must be nullable text';
    END IF;

    SELECT regexp_replace(lower(pg_get_constraintdef(oid)), '[[:space:]()]', '', 'g')
      INTO request_definition
      FROM pg_constraint
     WHERE conrelid='pos.fiscal_report_requests'::regclass
       AND conname='ck_fiscal_report_requests__actor_display_name';

    IF request_definition IS NULL THEN
        ALTER TABLE pos.fiscal_report_requests
            ADD CONSTRAINT ck_fiscal_report_requests__actor_display_name CHECK (
                requested_by_display_name IS NULL OR char_length(btrim(requested_by_display_name)) > 0
            );
    ELSIF request_definition <> 'checkrequested_by_display_nameisnullorchar_lengthbtrimrequested_by_display_name>0' THEN
        RAISE EXCEPTION 'existing ck_fiscal_report_requests__actor_display_name is incompatible: %', request_definition;
    END IF;

    SELECT regexp_replace(lower(pg_get_constraintdef(oid)), '[[:space:]()]', '', 'g')
      INTO workbook_definition
      FROM pg_constraint
     WHERE conrelid='pos.annex_e1_workbooks'::regclass
       AND conname='ck_annex_e1_workbooks__generated_by_display_name';

    IF workbook_definition IS NULL THEN
        ALTER TABLE pos.annex_e1_workbooks
            ADD CONSTRAINT ck_annex_e1_workbooks__generated_by_display_name CHECK (
                generated_by_display_name IS NULL OR char_length(btrim(generated_by_display_name)) > 0
            );
    ELSIF workbook_definition <> 'checkgenerated_by_display_nameisnullorchar_lengthbtrimgenerated_by_display_name>0' THEN
        RAISE EXCEPTION 'existing ck_annex_e1_workbooks__generated_by_display_name is incompatible: %', workbook_definition;
    END IF;
END;
$$;

DO $$
DECLARE
    before_state record;
    request_count bigint;
    request_fingerprint text;
    workbook_count bigint;
    workbook_fingerprint text;
BEGIN
    SELECT * INTO STRICT before_state FROM fiscal_report_actor_display_name_preflight;

    SELECT count(*), md5(COALESCE(string_agg(
        (to_jsonb(row_value)-'requested_by_display_name')::text,
        '' ORDER BY fiscal_report_request_id::text), ''))
      INTO request_count, request_fingerprint
      FROM pos.fiscal_report_requests row_value;

    SELECT count(*), md5(COALESCE(string_agg(
        (to_jsonb(row_value)-'generated_by_display_name')::text,
        '' ORDER BY annex_e1_workbook_id::text), ''))
      INTO workbook_count, workbook_fingerprint
      FROM pos.annex_e1_workbooks row_value;

    IF request_count <> before_state.request_row_count
       OR request_fingerprint <> before_state.request_fingerprint THEN
        RAISE EXCEPTION 'pos.fiscal_report_requests historical rows changed during migration';
    END IF;

    IF workbook_count <> before_state.workbook_row_count
       OR workbook_fingerprint <> before_state.workbook_fingerprint THEN
        RAISE EXCEPTION 'pos.annex_e1_workbooks historical rows changed during migration';
    END IF;

    IF NOT before_state.request_column_existed
       AND EXISTS (SELECT 1 FROM pos.fiscal_report_requests WHERE requested_by_display_name IS NOT NULL) THEN
        RAISE EXCEPTION 'existing fiscal report requests were unexpectedly backfilled';
    END IF;

    IF NOT before_state.workbook_column_existed
       AND EXISTS (SELECT 1 FROM pos.annex_e1_workbooks WHERE generated_by_display_name IS NOT NULL) THEN
        RAISE EXCEPTION 'existing Annex E-1 workbooks were unexpectedly backfilled';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid='pos.fiscal_report_requests'::regclass
          AND conname='ck_fiscal_report_requests__actor_display_name'
          AND contype='c' AND convalidated
    ) OR NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid='pos.annex_e1_workbooks'::regclass
          AND conname='ck_annex_e1_workbooks__generated_by_display_name'
          AND contype='c' AND convalidated
    ) THEN
        RAISE EXCEPTION 'actor display-name constraints are missing or unvalidated';
    END IF;
END;
$$;

COMMIT;
