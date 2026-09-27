START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054652_ThemBangTepVaViec') THEN
    CREATE TABLE core.file (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        owner_table character varying(100),
        owner_id uuid,
        original_name character varying(255) NOT NULL,
        content_type character varying(150) NOT NULL,
        size_bytes bigint NOT NULL,
        storage_key character varying(300) NOT NULL,
        purpose character varying(50),
        created_by character varying(100),
        updated_by character varying(100),
        created_at timestamp with time zone,
        updated_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        CONSTRAINT pk_file PRIMARY KEY (id),
        CONSTRAINT fk_file_tenant_id FOREIGN KEY (tenant_id) REFERENCES core.tenant (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054652_ThemBangTepVaViec') THEN
    CREATE TABLE core.job (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        type character varying(50) NOT NULL,
        status character varying(20) NOT NULL DEFAULT 'queued',
        created_by_user_id uuid NOT NULL,
        started_at timestamp with time zone,
        finished_at timestamp with time zone,
        progress smallint NOT NULL DEFAULT 0,
        result jsonb,
        error jsonb,
        result_file_id uuid,
        created_by character varying(100),
        updated_by character varying(100),
        created_at timestamp with time zone,
        updated_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        CONSTRAINT pk_job PRIMARY KEY (id),
        CONSTRAINT ck_job_progress CHECK (progress BETWEEN 0 AND 100),
        CONSTRAINT ck_job_status CHECK (status IN ('queued','running','succeeded','failed','cancelled')),
        CONSTRAINT fk_job_result_file_id FOREIGN KEY (result_file_id) REFERENCES core.file (id) ON DELETE RESTRICT,
        CONSTRAINT fk_job_tenant_id FOREIGN KEY (tenant_id) REFERENCES core.tenant (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054652_ThemBangTepVaViec') THEN
    CREATE INDEX ix_file_owner ON core.file (tenant_id, owner_table, owner_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054652_ThemBangTepVaViec') THEN
    CREATE UNIQUE INDEX ux_file_tenant_storage_key_active ON core.file (tenant_id, storage_key) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054652_ThemBangTepVaViec') THEN
    CREATE INDEX ix_job_result_file_id ON core.job (result_file_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054652_ThemBangTepVaViec') THEN
    CREATE INDEX ix_job_tenant_created_at ON core.job (tenant_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = '20260921054652_ThemBangTepVaViec') THEN
    INSERT INTO core.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260921054652_ThemBangTepVaViec', '10.0.12');
    END IF;
END $EF$;

-- Ghi nhận đã áp — docs/database/script-runbook.md §3.1. Checksum tính trên nội dung file NGAY
-- TRƯỚC khối này (§3.2: grep -abm1 '^-- Ghi nhận đã áp' rồi head -c "$offset" | sha256sum).
INSERT INTO core.schema_script_history (script_name, checksum_sha256, owner, applied_by, note)
VALUES ('0005__core__add-file-and-job.sql',
        'de66b68c7166490f608bb11ae7f45da2504a2221a6cf14ccc001da802ba347a1',
        'core',
        current_user,
        NULL)
ON CONFLICT (script_name) DO NOTHING;

COMMIT;

