-- Additive POS Z Reading early-close snapshot migration.
-- Apply with psql ON_ERROR_STOP enabled while fiscal issuance and reporting are quiesced.

BEGIN;

SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';

LOCK TABLE pos.x_z_reports IN ACCESS EXCLUSIVE MODE;

CREATE TEMP TABLE fiscal_z_early_close_preflight ON COMMIT DROP AS
SELECT
    count(*) AS row_count,
    md5(COALESCE(string_agg(
        (to_jsonb(row_value)-'is_early_closure'-'early_closure_reason')::text,
        '' ORDER BY x_z_report_id::text), '')) AS row_fingerprint
FROM pos.x_z_reports row_value;

ALTER TABLE pos.x_z_reports
    ADD COLUMN IF NOT EXISTS is_early_closure boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS early_closure_reason text NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema='pos' AND table_name='x_z_reports'
          AND column_name='is_early_closure' AND data_type='boolean' AND is_nullable='NO'
    ) THEN
        RAISE EXCEPTION 'pos.x_z_reports.is_early_closure must be non-null boolean';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema='pos' AND table_name='x_z_reports'
          AND column_name='early_closure_reason' AND data_type='text' AND is_nullable='YES'
    ) THEN
        RAISE EXCEPTION 'pos.x_z_reports.early_closure_reason must be nullable text';
    END IF;
END;
$$;

ALTER TABLE pos.x_z_reports
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__timestamps,
    DROP CONSTRAINT IF EXISTS ck_x_z_reports__early_closure;

ALTER TABLE pos.x_z_reports
    ADD CONSTRAINT ck_x_z_reports__timestamps CHECK (
        (
            (report_kind_code_id = '5dc3cc94-b3ab-5582-a598-e779871fc3e2'
                AND NOT is_early_closure
                AND generated_at >= period_start_at)
            OR
            (report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'
                AND (
                    (NOT is_early_closure AND generated_at >= period_end_at)
                    OR
                    (is_early_closure AND generated_at >= period_start_at AND generated_at < period_end_at)
                ))
        )
        AND committed_at >= generated_at
        AND updated_at = created_at
    ),
    ADD CONSTRAINT ck_x_z_reports__early_closure CHECK (
        (is_early_closure
            AND report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'
            AND early_closure_reason IS NOT NULL
            AND char_length(btrim(early_closure_reason)) > 0)
        OR (NOT is_early_closure AND early_closure_reason IS NULL)
    );

DO $$
DECLARE
    before_state record;
    after_count bigint;
    after_fingerprint text;
BEGIN
    SELECT * INTO STRICT before_state FROM fiscal_z_early_close_preflight;

    SELECT count(*), md5(COALESCE(string_agg(
        (to_jsonb(row_value)-'is_early_closure'-'early_closure_reason')::text,
        '' ORDER BY x_z_report_id::text), ''))
      INTO after_count, after_fingerprint
      FROM pos.x_z_reports row_value;

    IF after_count <> before_state.row_count
       OR after_fingerprint <> before_state.row_fingerprint THEN
        RAISE EXCEPTION 'pos.x_z_reports historical rows changed during migration';
    END IF;

    IF EXISTS (
        SELECT 1 FROM pos.x_z_reports
        WHERE is_early_closure OR early_closure_reason IS NOT NULL
    ) AND NOT EXISTS (
        SELECT 1 FROM pos.x_z_reports
        WHERE is_early_closure
          AND report_kind_code_id = '1c628bc2-49c3-53e8-ae83-2082bcf28467'
          AND generated_at >= period_start_at
          AND generated_at < period_end_at
          AND char_length(btrim(early_closure_reason)) > 0
    ) THEN
        RAISE EXCEPTION 'existing early-close snapshot facts are inconsistent';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid='pos.x_z_reports'::regclass
          AND conname='ck_x_z_reports__timestamps' AND contype='c' AND convalidated
    ) OR NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid='pos.x_z_reports'::regclass
          AND conname='ck_x_z_reports__early_closure' AND contype='c' AND convalidated
    ) THEN
        RAISE EXCEPTION 'early-close Z Reading constraints are missing or unvalidated';
    END IF;
END;
$$;

COMMENT ON COLUMN pos.x_z_reports.is_early_closure IS
    'Immutable true only for an authorized Z Reading committed before the scheduled period end.';
COMMENT ON COLUMN pos.x_z_reports.early_closure_reason IS
    'Immutable operator-provided reason required only for an authorized early Z Reading.';

COMMIT;
