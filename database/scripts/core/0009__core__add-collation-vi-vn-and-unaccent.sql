START TRANSACTION;

-- Tu chan — docs/database/script-runbook.md §3.1. Sai database thi DUNG truoc moi lenh. Chay bang tai khoan
-- nao cung duoc (postgres, coreandskill_owner...) mien la dong duoc vai chu schema core: khoi nay tu
-- SET LOCAL ROLE sang chu schema core cho het transaction, nen doi tuong tao ra van thuoc chu schema core.
DO $guard$
DECLARE
  chu_core text;
BEGIN
  IF to_regclass('core.schema_script_history') IS NULL THEN
    RAISE EXCEPTION 'Database % khong co core.schema_script_history - sai database, DUNG', current_database();
  END IF;
  SELECT pg_get_userbyid(nspowner) INTO chu_core FROM pg_namespace WHERE nspname = 'core';
  IF NOT pg_has_role(current_user, chu_core, 'MEMBER') THEN
    RAISE EXCEPTION 'Tai khoan % khong dong duoc vai % (chu schema core) - DUNG', current_user, chu_core;
  END IF;
  EXECUTE format('SET LOCAL ROLE %I', chu_core);
END $guard$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085832_TaoCollationViVnVaExtensionUnaccent') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'core') THEN
            CREATE SCHEMA core;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085832_TaoCollationViVnVaExtensionUnaccent') THEN
    CREATE COLLATION core.vi_vn (LOCALE = 'vi-VN',
        PROVIDER = icu,
        DETERMINISTIC = True
    );
    CREATE EXTENSION IF NOT EXISTS unaccent SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085832_TaoCollationViVnVaExtensionUnaccent') THEN
    ALTER TABLE core.tenant ALTER COLUMN name TYPE character varying(200) COLLATE core.vi_vn;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085832_TaoCollationViVnVaExtensionUnaccent') THEN
    ALTER TABLE core.app_user ALTER COLUMN full_name TYPE character varying(200) COLLATE core.vi_vn;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085832_TaoCollationViVnVaExtensionUnaccent') THEN
    ALTER TABLE core.app_role ALTER COLUMN name TYPE character varying(256) COLLATE core.vi_vn;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085832_TaoCollationViVnVaExtensionUnaccent') THEN
    INSERT INTO core.__ef_migrations_history (migration_id, product_version)
    VALUES ('20261006085832_TaoCollationViVnVaExtensionUnaccent', '10.0.12');
    END IF;
END $EF$;

-- Ghi nhận đã áp — docs/database/script-runbook.md §3.1. Checksum tính trên nội dung file NGAY
-- TRƯỚC khối này (§3.2: grep -abm1 '^-- Ghi nhận đã áp' rồi head -c "$offset" | sha256sum).
INSERT INTO core.schema_script_history (script_name, checksum_sha256, owner, applied_by, note)
VALUES ('0009__core__add-collation-vi-vn-and-unaccent.sql',
        '63ebb6902c1b8493dedcd5c2825fe932790859e1b2d7c106f1a613a14c8ed9db',
        'core',
        current_user,
        NULL)
ON CONFLICT (script_name) DO NOTHING;

COMMIT;
