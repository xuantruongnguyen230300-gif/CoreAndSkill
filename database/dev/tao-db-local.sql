-- CHI DUNG CHO DATABASE CUC BO TREN MAY DEV — docs/database/script-runbook.md muc 6.
-- Chay bang tai khoan quan tri (postgres) khi dang ket noi vao database `postgres`.
--
-- Mat khau o day CO Y cong khai va khop appsettings.Development.json: clone repo ve, chay tep nay
-- la BE ket noi duoc, khong phai sua cau hinh. Chung chi mo duoc mot database cuc bo dung tu
-- script cong khai. KHONG chay tep nay tren may chu dung chung hay ban cai that.
--
-- Chay lai duoc: role da co thi chi dat lai mat khau; database da co thi cau cuoi bao
-- "already exists" — bo qua loi do.

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'coreandskill_owner') THEN
    CREATE ROLE coreandskill_owner LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'coreandskill_app') THEN
    CREATE ROLE coreandskill_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE;
  END IF;
END $$;

ALTER ROLE coreandskill_owner PASSWORD 'coreandskill_owner';
ALTER ROLE coreandskill_app PASSWORD 'coreandskill_app';

CREATE DATABASE coreandskill_dev OWNER coreandskill_owner ENCODING 'UTF8' TEMPLATE template0;
