START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260918171926_SuaChieuIndexAuditLogTenantOccurred') THEN
    DROP INDEX core.ix_audit_log_tenant_occurred;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260918171926_SuaChieuIndexAuditLogTenantOccurred') THEN
    CREATE INDEX ix_audit_log_tenant_occurred ON core.audit_log (tenant_id, occurred_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260918171926_SuaChieuIndexAuditLogTenantOccurred') THEN
    INSERT INTO core.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260918171926_SuaChieuIndexAuditLogTenantOccurred', '10.0.12');
    END IF;
END $EF$;

-- Ghi nhận đã áp — docs/database/script-runbook.md §3.1. Checksum tính trên nội dung file NGAY
-- TRƯỚC khối này (§3.2: grep -abm1 '^-- Ghi nhận đã áp' rồi head -c "$offset" | sha256sum).
INSERT INTO core.schema_script_history (script_name, checksum_sha256, owner, applied_by, note)
VALUES ('0004__core__fix-audit-log-index-direction.sql',
        '14d40d09e696acdb6a241e937c06cf67e121f2b6e8e2b446e22ff8a55db15c17',
        'core',
        current_user,
        NULL)
ON CONFLICT (script_name) DO NOTHING;

COMMIT;

