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
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085811_ThemDefaultIsDeleted') THEN
    ALTER TABLE core.role_permission ALTER COLUMN is_deleted SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085811_ThemDefaultIsDeleted') THEN
    ALTER TABLE core.permission_resource ALTER COLUMN is_deleted SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085811_ThemDefaultIsDeleted') THEN
    ALTER TABLE core.permission ALTER COLUMN is_deleted SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085811_ThemDefaultIsDeleted') THEN
    ALTER TABLE core.notification_recipient ALTER COLUMN is_deleted SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085811_ThemDefaultIsDeleted') THEN
    ALTER TABLE core.notification ALTER COLUMN is_deleted SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085811_ThemDefaultIsDeleted') THEN
    ALTER TABLE core.menu_item_role ALTER COLUMN is_deleted SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085811_ThemDefaultIsDeleted') THEN
    ALTER TABLE core.menu_item ALTER COLUMN is_deleted SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085811_ThemDefaultIsDeleted') THEN
    ALTER TABLE core.job ALTER COLUMN is_deleted SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085811_ThemDefaultIsDeleted') THEN
    ALTER TABLE core.file ALTER COLUMN is_deleted SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20261006085811_ThemDefaultIsDeleted') THEN
    INSERT INTO core.__ef_migrations_history (migration_id, product_version)
    VALUES ('20261006085811_ThemDefaultIsDeleted', '10.0.12');
    END IF;
END $EF$;

-- Ghi nhận đã áp — docs/database/script-runbook.md §3.1. Checksum tính trên nội dung file NGAY
-- TRƯỚC khối này (§3.2: grep -abm1 '^-- Ghi nhận đã áp' rồi head -c "$offset" | sha256sum).
INSERT INTO core.schema_script_history (script_name, checksum_sha256, owner, applied_by, note)
VALUES ('0008__core__add-default-is-deleted.sql',
        '5a147d57f2863bce1c8f77466e2bbb7f057321a74a0f848f1ab690781b0aec20',
        'core',
        current_user,
        NULL)
ON CONFLICT (script_name) DO NOTHING;

COMMIT;
