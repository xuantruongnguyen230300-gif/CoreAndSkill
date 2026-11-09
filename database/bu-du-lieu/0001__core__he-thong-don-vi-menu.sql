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

-- BU DU LIEU (khong phai script schema) — docs/adr/0122-don-vi-he-thong-duoc-seed-mot-muc-menu-toi-he-thong-don-vi-moi-tai-khoan-cua-no-mang-co-van-hanh.md diem 5.
-- Them muc menu 'he-thong-don-vi' cho don vi he thong DA CO. Ban cai moi khong can: lenh bootstrap da seed muc nay.
-- KHONG ghi core.schema_script_history. Chay lai cho cung ket qua: da co muc chua xoa thi khong ghi gi va bao ra.
-- Gia tri moi cot lay tu CoreTenantSeedSource.SystemTenantMenuItems (C#); test SystemTenantMenuDatabaseTests giu hai noi khop nhau.
DO $backfill$
DECLARE
  don_vi_he_thong uuid;
BEGIN
  IF to_regprocedure('uuidv7()') IS NULL THEN
    RAISE EXCEPTION 'Cum PostgreSQL nay khong co ham uuidv7() (can PostgreSQL 18) - DUNG, khong ghi gi';
  END IF;

  SELECT id INTO don_vi_he_thong FROM core.tenant WHERE is_system;
  IF don_vi_he_thong IS NULL THEN
    RAISE EXCEPTION 'Ban cai chua bootstrap; lenh bootstrap se seed muc nay - DUNG, khong ghi gi';
  END IF;

  IF EXISTS (
    SELECT 1 FROM core.menu_item
    WHERE tenant_id = don_vi_he_thong AND code = 'he-thong-don-vi' AND is_deleted = false
  ) THEN
    RAISE NOTICE 'Don vi he thong da co muc he-thong-don-vi - khong ghi gi';
    RETURN;
  END IF;

  INSERT INTO core.menu_item (
    id, tenant_id, code, label_key, icon, route, parent_id, display_order, required_permission_id, module_key,
    created_by, updated_by, created_at, updated_at, is_deleted)
  VALUES (
    uuidv7(), don_vi_he_thong, 'he-thong-don-vi', 'menu.he-thong-don-vi', 'pi-building', '/he-thong/don-vi', NULL, 100, NULL, NULL,
    'system', 'system', now(), now(), false);
  RAISE NOTICE 'Da them muc he-thong-don-vi cho don vi he thong';
END $backfill$;

COMMIT;
