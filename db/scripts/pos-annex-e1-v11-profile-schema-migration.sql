-- Permit the approved Annex E-1 calculation profiles v1.0 and v1.1.
-- The caller must set exitpass.migration_environment and exitpass.migration_database
-- on the PostgreSQL session before applying this migration with ON_ERROR_STOP enabled.

BEGIN;

SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';

DO $$
DECLARE
    deployment_environment text := current_setting('exitpass.migration_environment', true);
    expected_database text := current_setting('exitpass.migration_database', true);
BEGIN
    IF deployment_environment IS NULL OR btrim(deployment_environment) = '' THEN
        RAISE EXCEPTION 'exitpass.migration_environment must be set explicitly';
    END IF;

    IF expected_database IS NULL OR btrim(expected_database) = '' THEN
        RAISE EXCEPTION 'exitpass.migration_database must be set explicitly';
    END IF;

    IF expected_database <> current_database() THEN
        RAISE EXCEPTION 'database guard mismatch: expected %, connected %', expected_database, current_database();
    END IF;

    IF deployment_environment = 'CLEAN-NOVEMBER-UAT' THEN
        IF current_database() <> 'exitpass_pos_clean_november' THEN
            RAISE EXCEPTION 'CLEAN-NOVEMBER-UAT requires database exitpass_pos_clean_november';
        END IF;
    ELSIF deployment_environment IN ('TEST', 'VALIDATION') THEN
        IF current_database() !~* '(test|validation)' THEN
            RAISE EXCEPTION '% requires a test or validation database', deployment_environment;
        END IF;
    ELSE
        RAISE EXCEPTION 'unsupported migration environment: %', deployment_environment;
    END IF;

    IF to_regclass('pos.annex_e1_workbooks') IS NULL THEN
        RAISE EXCEPTION 'required table pos.annex_e1_workbooks is unavailable';
    END IF;
END;
$$;

LOCK TABLE pos.annex_e1_workbooks IN ACCESS EXCLUSIVE MODE;

CREATE TEMP TABLE annex_e1_v11_profile_preflight ON COMMIT DROP AS
SELECT
    count(*) AS workbook_count,
    md5(COALESCE(string_agg(to_jsonb(row_value)::text, ''
        ORDER BY annex_e1_workbook_id::text), '')) AS workbook_fingerprint
FROM pos.annex_e1_workbooks row_value;

CREATE TEMP TABLE annex_e1_v11_profile_contract (
    profile_ref text NOT NULL,
    calculation_profile_ref text NOT NULL,
    calculation_profile_sha256 char(64) NOT NULL,
    CONSTRAINT expected_legacy CHECK (
        profile_ref='pos-server-bir-annex-e1-rmo24-2023:v1'
        AND calculation_profile_ref='pos-server-annex-e1-accounting-calculation:v1'
        AND calculation_profile_sha256='36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f'
    ),
    CONSTRAINT expected_target CHECK (
        profile_ref='pos-server-bir-annex-e1-rmo24-2023:v1'
        AND (
            (calculation_profile_ref='pos-server-annex-e1-accounting-calculation:v1'
                AND calculation_profile_sha256='36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f')
            OR (calculation_profile_ref='pos-server-annex-e1-accounting-calculation:v1.1'
                AND calculation_profile_sha256='02ba18516878ee46364e7c35639fcb89d2976fab18e3a57babd705a3d01bfebb')
        )
    )
) ON COMMIT DROP;

DO $$
DECLARE
    actual_definition text;
    legacy_definition text;
    target_definition text;
BEGIN
    SELECT regexp_replace(lower(pg_get_constraintdef(oid)), '::(text|bpchar)|[[:space:]()]', '', 'g')
      INTO actual_definition
      FROM pg_constraint
     WHERE conrelid='pos.annex_e1_workbooks'::regclass
       AND conname='ck_annex_e1_workbooks__profile'
       AND contype='c'
       AND convalidated;

    SELECT regexp_replace(lower(pg_get_constraintdef(oid)), '::(text|bpchar)|[[:space:]()]', '', 'g')
      INTO legacy_definition
      FROM pg_constraint
     WHERE conrelid='annex_e1_v11_profile_contract'::regclass
       AND conname='expected_legacy';

    SELECT regexp_replace(lower(pg_get_constraintdef(oid)), '::(text|bpchar)|[[:space:]()]', '', 'g')
      INTO target_definition
      FROM pg_constraint
     WHERE conrelid='annex_e1_v11_profile_contract'::regclass
       AND conname='expected_target';

    IF actual_definition IS NULL THEN
        RAISE EXCEPTION 'validated ck_annex_e1_workbooks__profile is missing';
    ELSIF actual_definition = target_definition THEN
        NULL;
    ELSIF actual_definition = legacy_definition THEN
        ALTER TABLE pos.annex_e1_workbooks
            DROP CONSTRAINT ck_annex_e1_workbooks__profile;

        ALTER TABLE pos.annex_e1_workbooks
            ADD CONSTRAINT ck_annex_e1_workbooks__profile CHECK (
                profile_ref='pos-server-bir-annex-e1-rmo24-2023:v1'
                AND (
                    (calculation_profile_ref='pos-server-annex-e1-accounting-calculation:v1'
                        AND calculation_profile_sha256='36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f')
                    OR (calculation_profile_ref='pos-server-annex-e1-accounting-calculation:v1.1'
                        AND calculation_profile_sha256='02ba18516878ee46364e7c35639fcb89d2976fab18e3a57babd705a3d01bfebb')
                )
            );
    ELSE
        RAISE EXCEPTION 'existing ck_annex_e1_workbooks__profile is incompatible: %', actual_definition;
    END IF;
END;
$$;

DO $$
DECLARE
    before_state record;
    workbook_count bigint;
    workbook_fingerprint text;
    actual_definition text;
    target_definition text;
BEGIN
    SELECT * INTO STRICT before_state FROM annex_e1_v11_profile_preflight;

    SELECT count(*), md5(COALESCE(string_agg(to_jsonb(row_value)::text, ''
        ORDER BY annex_e1_workbook_id::text), ''))
      INTO workbook_count, workbook_fingerprint
      FROM pos.annex_e1_workbooks row_value;

    IF workbook_count <> before_state.workbook_count
       OR workbook_fingerprint <> before_state.workbook_fingerprint THEN
        RAISE EXCEPTION 'pos.annex_e1_workbooks historical rows changed during migration';
    END IF;

    SELECT regexp_replace(lower(pg_get_constraintdef(oid)), '::(text|bpchar)|[[:space:]()]', '', 'g')
      INTO actual_definition
      FROM pg_constraint
     WHERE conrelid='pos.annex_e1_workbooks'::regclass
       AND conname='ck_annex_e1_workbooks__profile'
       AND contype='c'
       AND convalidated;

    SELECT regexp_replace(lower(pg_get_constraintdef(oid)), '::(text|bpchar)|[[:space:]()]', '', 'g')
      INTO target_definition
      FROM pg_constraint
     WHERE conrelid='annex_e1_v11_profile_contract'::regclass
       AND conname='expected_target';

    IF actual_definition IS DISTINCT FROM target_definition THEN
        RAISE EXCEPTION 'ck_annex_e1_workbooks__profile did not reach the approved target';
    END IF;
END;
$$;

COMMIT;
