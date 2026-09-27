START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260918031217_SeedCorePermissionCatalog') THEN
    INSERT INTO core.permission_resource (id, key, name_key, module_key, display_order, is_deleted, created_at, created_by, updated_at, updated_by)
    VALUES
        ('8f1e0000-0000-7000-8000-000000000001', 'core.user',       'resource.core.user',       NULL, 10, false, now(), 'system', now(), 'system'),
        ('8f1e0000-0000-7000-8000-000000000002', 'core.user.role',  'resource.core.user.role',  NULL, 20, false, now(), 'system', now(), 'system'),
        ('8f1e0000-0000-7000-8000-000000000003', 'core.role',       'resource.core.role',       NULL, 30, false, now(), 'system', now(), 'system'),
        ('8f1e0000-0000-7000-8000-000000000004', 'core.permission', 'resource.core.permission', NULL, 40, false, now(), 'system', now(), 'system'),
        ('8f1e0000-0000-7000-8000-000000000005', 'core.menu',       'resource.core.menu',       NULL, 50, false, now(), 'system', now(), 'system')
    ON CONFLICT (key) DO NOTHING;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260918031217_SeedCorePermissionCatalog') THEN
    INSERT INTO core.permission (id, code, resource_key, action, name_key, is_system, display_order, is_deleted, created_at, created_by, updated_at, updated_by)
    VALUES
        ('8f1e0001-0000-7000-8000-000000000001', 'core.user.read',           'core.user',      'read',           'permission.core.user.read',           true, 10, false, now(), 'system', now(), 'system'),
        ('8f1e0001-0000-7000-8000-000000000002', 'core.user.write',          'core.user',      'write',          'permission.core.user.write',          true, 20, false, now(), 'system', now(), 'system'),
        ('8f1e0001-0000-7000-8000-000000000003', 'core.user.lock',           'core.user',      'lock',           'permission.core.user.lock',           true, 30, false, now(), 'system', now(), 'system'),
        ('8f1e0001-0000-7000-8000-000000000004', 'core.user.reset-password', 'core.user',      'reset-password', 'permission.core.user.reset-password', true, 40, false, now(), 'system', now(), 'system'),
        ('8f1e0001-0000-7000-8000-000000000005', 'core.user.export',         'core.user',      'export',         'permission.core.user.export',         true, 50, false, now(), 'system', now(), 'system'),
        ('8f1e0001-0000-7000-8000-000000000006', 'core.user.role.assign',    'core.user.role', 'assign',         'permission.core.user.role.assign',    true, 10, false, now(), 'system', now(), 'system'),
        ('8f1e0001-0000-7000-8000-000000000007', 'core.role.read',           'core.role',      'read',           'permission.core.role.read',           true, 10, false, now(), 'system', now(), 'system'),
        ('8f1e0001-0000-7000-8000-000000000008', 'core.role.write',          'core.role',      'write',          'permission.core.role.write',          true, 20, false, now(), 'system', now(), 'system'),
        ('8f1e0001-0000-7000-8000-000000000009', 'core.permission.read',     'core.permission','read',           'permission.core.permission.read',     true, 10, false, now(), 'system', now(), 'system'),
        ('8f1e0001-0000-7000-8000-00000000000a', 'core.permission.write',    'core.permission','write',          'permission.core.permission.write',    true, 20, false, now(), 'system', now(), 'system'),
        ('8f1e0001-0000-7000-8000-00000000000b', 'core.menu.read',           'core.menu',      'read',           'permission.core.menu.read',           true, 10, false, now(), 'system', now(), 'system'),
        ('8f1e0001-0000-7000-8000-00000000000c', 'core.menu.write',          'core.menu',      'write',          'permission.core.menu.write',          true, 20, false, now(), 'system', now(), 'system')
    ON CONFLICT (code) WHERE is_deleted = false DO NOTHING;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260918031217_SeedCorePermissionCatalog') THEN
    INSERT INTO core.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260918031217_SeedCorePermissionCatalog', '10.0.12');
    END IF;
END $EF$;

-- Ghi nhận đã áp — docs/database/script-runbook.md §3.1. Checksum tính trên nội dung file NGAY
-- TRƯỚC khối này (§3.2: grep -abm1 '^-- Ghi nhận đã áp' rồi head -c "$offset" | sha256sum).
INSERT INTO core.schema_script_history (script_name, checksum_sha256, owner, applied_by, note)
VALUES ('0002__core__seed-permission-catalog.sql',
        'cae69a06db337c8966140b6841bde5e983acd44e5d8cc9446cee80ddb6ad24b6',
        'core',
        current_user,
        NULL)
ON CONFLICT (script_name) DO NOTHING;

COMMIT;

