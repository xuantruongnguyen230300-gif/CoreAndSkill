START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921055100_ThemBangThongBao') THEN
    CREATE TABLE core.notification (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        code character varying(100) NOT NULL,
        params jsonb,
        severity character varying(20) NOT NULL DEFAULT 'info',
        link_route character varying(200),
        module_key character varying(100),
        created_by character varying(100),
        updated_by character varying(100),
        created_at timestamp with time zone,
        updated_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        CONSTRAINT pk_notification PRIMARY KEY (id),
        CONSTRAINT ck_notification_severity CHECK (severity IN ('info','success','warning','error')),
        CONSTRAINT fk_notification_tenant_id FOREIGN KEY (tenant_id) REFERENCES core.tenant (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921055100_ThemBangThongBao') THEN
    CREATE TABLE core.notification_recipient (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        notification_id uuid NOT NULL,
        user_id uuid NOT NULL,
        read_at timestamp with time zone,
        created_by character varying(100),
        updated_by character varying(100),
        created_at timestamp with time zone,
        updated_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        CONSTRAINT pk_notification_recipient PRIMARY KEY (id),
        CONSTRAINT fk_notification_recipient_notification_id FOREIGN KEY (notification_id) REFERENCES core.notification (id) ON DELETE CASCADE,
        CONSTRAINT fk_notification_recipient_tenant_id FOREIGN KEY (tenant_id) REFERENCES core.tenant (id) ON DELETE RESTRICT,
        CONSTRAINT fk_notification_recipient_user_id FOREIGN KEY (user_id) REFERENCES core.app_user (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921055100_ThemBangThongBao') THEN
    CREATE INDEX ix_notification_tenant_created_at ON core.notification (tenant_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921055100_ThemBangThongBao') THEN
    CREATE INDEX ix_notification_recipient_notification_id ON core.notification_recipient (notification_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921055100_ThemBangThongBao') THEN
    CREATE INDEX ix_notification_recipient_unread ON core.notification_recipient (tenant_id, user_id, created_at DESC) WHERE read_at IS NULL AND is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921055100_ThemBangThongBao') THEN
    CREATE INDEX ix_notification_recipient_user_id ON core.notification_recipient (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921055100_ThemBangThongBao') THEN
    CREATE UNIQUE INDEX ux_notification_recipient_tenant_notif_user_active ON core.notification_recipient (tenant_id, notification_id, user_id) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921055100_ThemBangThongBao') THEN
    INSERT INTO core.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260921055100_ThemBangThongBao', '10.0.12');
    END IF;
END $EF$;

-- Ghi nhận đã áp — docs/database/script-runbook.md §3.1. Checksum tính trên nội dung file NGAY
-- TRƯỚC khối này (§3.2: grep -abm1 '^-- Ghi nhận đã áp' rồi head -c "$offset" | sha256sum).
INSERT INTO core.schema_script_history (script_name, checksum_sha256, owner, applied_by, note)
VALUES ('0007__core__add-notification.sql',
        'c17fa40f49f14fdfd4ad52843bb2ad5205818a87d8c2e865b8b2053f549b3a28',
        'core',
        current_user,
        NULL)
ON CONFLICT (script_name) DO NOTHING;

COMMIT;

