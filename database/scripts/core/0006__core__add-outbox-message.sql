START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054915_ThemBangOutbox') THEN
    CREATE TABLE core.outbox_message (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        event_type character varying(300) NOT NULL,
        payload jsonb NOT NULL,
        trace_id character varying(64),
        triggered_by_user_id uuid,
        triggered_by_user_name character varying(100),
        status character varying(20) NOT NULL DEFAULT 'pending',
        processed_at timestamp with time zone,
        attempt_count integer NOT NULL DEFAULT 0,
        next_attempt_at timestamp with time zone,
        last_error text,
        CONSTRAINT pk_outbox_message PRIMARY KEY (id),
        CONSTRAINT ck_outbox_message_status CHECK (status IN ('pending','dead','done')),
        CONSTRAINT fk_outbox_message_tenant_id FOREIGN KEY (tenant_id) REFERENCES core.tenant (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054915_ThemBangOutbox') THEN
    CREATE INDEX ix_outbox_message_dead ON core.outbox_message (occurred_at) WHERE status = 'dead';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054915_ThemBangOutbox') THEN
    CREATE INDEX ix_outbox_message_pending ON core.outbox_message (next_attempt_at) WHERE status = 'pending';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054915_ThemBangOutbox') THEN
    CREATE INDEX ix_outbox_message_tenant_id ON core.outbox_message (tenant_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054915_ThemBangOutbox') THEN
    INSERT INTO core.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260921054915_ThemBangOutbox', '10.0.12');
    END IF;
END $EF$;

-- Ghi nhận đã áp — docs/database/script-runbook.md §3.1. Checksum tính trên nội dung file NGAY
-- TRƯỚC khối này (§3.2: grep -abm1 '^-- Ghi nhận đã áp' rồi head -c "$offset" | sha256sum).
INSERT INTO core.schema_script_history (script_name, checksum_sha256, owner, applied_by, note)
VALUES ('0006__core__add-outbox-message.sql',
        'f789c17984b466cc4ce1be784bea6b279ee56f36ab5a136057785d3da1f06ca1',
        'core',
        current_user,
        NULL)
ON CONFLICT (script_name) DO NOTHING;

COMMIT;

