START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260918105556_ThemBangAuditLog') THEN
    CREATE TABLE core.audit_log (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        actor_user_id uuid,
        actor_tenant_id uuid,
        actor_display text NOT NULL,
        action_code text NOT NULL,
        target_type text NOT NULL,
        target_id text NOT NULL,
        target_display text,
        before_value jsonb,
        after_value jsonb,
        ip_address inet,
        trace_id text,
        CONSTRAINT pk_audit_log PRIMARY KEY (id),
        CONSTRAINT fk_audit_log_actor_tenant_id FOREIGN KEY (actor_tenant_id) REFERENCES core.tenant (id) ON DELETE RESTRICT,
        CONSTRAINT fk_audit_log_tenant_id FOREIGN KEY (tenant_id) REFERENCES core.tenant (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260918105556_ThemBangAuditLog') THEN
    CREATE INDEX ix_audit_log_actor_tenant_id ON core.audit_log (actor_tenant_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260918105556_ThemBangAuditLog') THEN
    CREATE INDEX ix_audit_log_tenant_occurred ON core.audit_log (tenant_id, occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260918105556_ThemBangAuditLog') THEN
    CREATE INDEX ix_audit_log_tenant_target ON core.audit_log (tenant_id, target_type, target_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260918105556_ThemBangAuditLog') THEN
    INSERT INTO core.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260918105556_ThemBangAuditLog', '10.0.12');
    END IF;
END $EF$;

-- Ghi nhận đã áp — docs/database/script-runbook.md §3.1. Checksum tính trên nội dung file NGAY
-- TRƯỚC khối này (§3.2: grep -abm1 '^-- Ghi nhận đã áp' rồi head -c "$offset" | sha256sum).
INSERT INTO core.schema_script_history (script_name, checksum_sha256, owner, applied_by, note)
VALUES ('0003__core__add-audit-log.sql',
        'df1e44d529b722e4a04afef5273f120035de06be7ab2a94a883bbaff2fc9a89d',
        'core',
        current_user,
        NULL)
ON CONFLICT (script_name) DO NOTHING;

COMMIT;

