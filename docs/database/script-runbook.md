---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Runbook script database — đường đi cụ thể từ model tới database

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu 2026-09-17). `src/` và `database/scripts/` đã tồn tại
> thật, nhưng chỉ một phần cơ chế trong file này có code thật. Đối chiếu diễn ra theo **từng
> mục** — mục nào bảng dưới không nhắc tới thì vẫn đứng nguyên `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG`.
>
> **File chủ về thao tác áp schema.** Nội dung schema: [`schema-core.md`](schema-core.md).
> Ai sở hữu migration nào: [`migration-policy.md`](migration-policy.md).

| Mục | Có thật hôm nay | Sẽ thành |
| --- | --- | --- |
| §1–§2 | `database/scripts/core/` đã có **nhiều hơn một** script, mọi tên đúng khuôn `NNNN__owner__mo-ta` (đối chiếu 2026-09-19). Danh sách không chép vào đây — nó đếm được bằng `ls database/scripts/core/` ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6) | Script mới theo cùng khuôn mỗi lần Core đổi model; số thứ tự tăng, không sửa script đã chạy (§2) |
| §3.1 | Khối ghi `core.schema_script_history` có thật, cuối transaction — bảng tạo ở `../../database/scripts/core/0001__core__initial.sql:15-29`, khối ghi lịch sử là khối cuối cùng của file đó, ngay trước `COMMIT;`. Khối tự chặn ở đầu script mới có từ Core `0008` (đối chiếu 2026-10-06) — tệp nào mang: `grep -rl 'DO \$guard\$' database/scripts`; PASS khi mọi script Core từ `0008` và mọi script module có mặt. Bản đóng vai chủ schema `core` khớp nguyên văn khối mẫu ở `0008`, `0009` — chuỗi `EXECUTE format('SET LOCAL ROLE %I', chu_core);` (đối chiếu 2026-10-07). Test tích hợp áp mọi script bằng `postgres` trên database trống — `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Support/PostgresFixture.cs`, chuỗi `SuperUser = "postgres"` —, nên schema `core` ở đó thuộc `postgres` và khối chỉ đóng vai chính tài khoản đã nối; phiên chính báo `ViVnCollationAndUnaccentTests` xanh 2026-10-07 | Áp `0008` bằng `postgres` trên database có schema `core` thuộc `coreandskill_owner`, rồi câu kiểm chủ bảng ở §3.3 vẫn ra đúng một dòng `coreandskill_owner` — **chưa ai chạy** |
| §3.2 | Checksum lưu ở cột `checksum_sha256` của khối ghi lịch sử cuối `0001__core__initial.sql` khớp đúng nội dung thật phía trước nó — lệnh mẫu ở §3.2/§3.5 đã sửa sang cắt theo byte (`grep -ab` + `head -c`) và tự kiểm lại khớp giá trị đó (2026-09-17), thay cho bản `awk` cũ (đọc CRLF sai byte, lệch cả mẫu regex với chú thích thật trong file). `.gitattributes` ép CRLF khi checkout — neo ở §3.2 (đối chiếu 2026-10-06) | Phép kiểm checksum của luật E26 (§2.1) |
| §3.3 — cụm có ICU và gói contrib | Script `0009__core__add-collation-vi-vn-and-unaccent.sql` tạo collation bằng `PROVIDER = icu` và chạy `CREATE EXTENSION IF NOT EXISTS unaccent SCHEMA public` (đối chiếu 2026-10-06, [ADR-0119](../adr/0119-cot-ten-dung-collation-icu-core-vi-vn-tim-khong-dau-qua-unaccent-is-deleted-co-default-false.md)) | Áp `0009` trên một cụm thật — **chưa ai áp**; quyền tạo extension của `coreandskill_owner` chưa kiểm trên cụm nào |
| §3.3, §3.6, §6 | `core bootstrap` và service tạo đơn vị có code thật — nhánh `case "bootstrap":` của `RunCoreCommandAsync` và hàm `RunBootstrapAsync` trong `../../src/BE/Core/CoreAndSkill.Core.Web/Commands/CoreCommandRunner.cs` | Chạy tay lần đầu trên Postgres thật — **chưa từng chạy**. Test không database gọi `RunBootstrapAsync`, xanh (`dotnet test`, 2026-09-25): `BootstrapReservedUserNameTests` (service tạo đơn vị giả) và `BootstrapSeedValidationTests` (runner và service thật trên `CoreDbContext` không database). **Chưa test nào** chạy lệnh trên PostgreSQL thật; service tạo đơn vị thì có — `TenantProvisioningDatabaseTests` (nhóm `RequiresDocker`) gọi thẳng service, xanh: `dotnet test` 2026-11-03 ở máy Windows, Docker `postgres:18`; CI chưa chạy với bản này. Hai khoá email ở bước 4 ([ADR-0091](../adr/0091-bootstrap-doi-hai-khoa-email-bat-buoc.md)) **đã khớp** (đối chiếu 2026-09-25): `src/BE/Core/CoreAndSkill.Core.Application/Configuration/CoreBootstrapOptions.cs` có `public string OperatorEmail` và `public string AdminEmail`, mỗi khoá mang `[EmailAddress(ErrorMessage =` và `[MaxLength(EmailMaxLength` (256, bằng cột `core.app_user.email`); `CoreCommandRunner.cs` in từng lỗi con kèm khoá cấu hình — chuỗi `(khoá {CoreBootstrapOptions.SectionName}:{configKey})`; `ITenantProvisioningService.cs` khai `string AdminEmail,` không giá trị mặc định. `BootstrapAccountConfigTests` xanh cùng ngày — Identity lấy từ đăng ký thật (`OfflineBootstrapHarness`), không database. `docker-compose.yml` có `CORE_BOOTSTRAP_OPERATOR_EMAIL` và `CORE_BOOTSTRAP_ADMIN_EMAIL`; `.env.example` có cả hai. Hai mã đơn vị trùng nhau ở bước 4 thì lệnh dừng — **đã khớp** (đối chiếu 2026-10-08): `CoreBootstrapOptions.cs` có `public IEnumerable<ValidationResult> Validate(`, so `SystemTenantCode.Trim().ToUpperInvariant()` với `FirstTenantCode.Trim().ToUpperInvariant()`; `TenantCodesEqualAfterNormalisation_ExitsOne_WritesNothing_NamesBothKeys` (`BootstrapAccountConfigTests`) và `TenantCodesEqualAfterNormalisation_FailsValidation_NamingBothKeys` (`CoreBootstrapOptionsTests`) xanh (`dotnet test`, 2026-10-08) |
| §6 — đường máy dev | `database/dev/tao-db-local.sql` (chuỗi `CREATE DATABASE coreandskill_dev OWNER coreandskill_owner`) và `src/BE/CoreAndSkill.Api/appsettings.Development.json` (chuỗi `Database=coreandskill_dev;Username=coreandskill_app;Password=coreandskill_app`) khớp nhau về database, tài khoản, mật khẩu (đối chiếu 2026-09-28) | Chạy trọn năm bước trên một máy sạch — **chưa ai chạy**. Máy giữ hai tệp khớp nhau: lớp ArchTest của S23 vế (b) — dòng S23 ở [`../RULES-BE.md`](../RULES-BE.md); lớp gitleaks ở [`../RULES-BE.md`](../RULES-BE.md) S23 không đọc tệp `.sql` |
| §6 — chữ tiếng Việt ở console Windows | 🚧 Code đang về, lượt `backend-expert` chưa qua review: `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreConsoleEncoding.cs`, chuỗi `if (!OperatingSystem.IsWindows() \|\| Console.IsOutputRedirected)`, gọi từ `CoreServiceCollectionExtensions.cs` cùng thư mục (đối chiếu 2026-10-06) | Qua review Core; chạy thử lệnh `core bootstrap` trong PowerShell 5.1 — **chưa ai chạy** |
| §4 | Khuôn `--idempotent` (`DO $EF$ … IF NOT EXISTS (… __ef_migrations_history …)`) đã dùng thật để sinh `0001__core__initial.sql` | — |
| §5.1–§5.2 | `ISchemaVerifier`/`SchemaVerifier` có code thật, khớp mẫu (`../../src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/SchemaVerifier.cs`). Phép kiểm lúc khởi động là `DatabaseSchemaStartupCheck` (`../../src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/DatabaseSchemaStartupCheck.cs`), gọi từ `VerifyDatabaseSchemaAsync` trong `CoreApplicationBuilderExtensions.cs` cùng thư mục; hai khoá `Core:SchemaCheck:*` khớp `CoreSchemaCheckOptions` (đối chiếu 2026-09-25). Lớp không database của luật E8 xanh — hàng E8 ở [`../RULES.md`](../RULES.md) §4 | Nhánh `SchemaVerifier` trên PostgreSQL thật thiếu migration **chưa có test** — ô *Ép bằng gì* của dòng E8 ở [`../RULES-BE.md`](../RULES-BE.md) |
| §8 | `core reset-operator-password` có code thật — hàm `RunResetOperatorPasswordAsync` trong `CoreCommandRunner.cs`; kết quả hỏng thì `Environment.ExitCode = 1` sau `ReportFailureAsync(`. Service dừng ở lệnh ghi hỏng: `PasswordResetWriteFailureTests` xanh (2026-09-25), gọi thẳng service. Nhánh dừng của runner chỉ kiểm hai khoá có giá trị, không kiểm mật khẩu có mới — chuỗi `string.IsNullOrWhiteSpace(options.OperatorPassword)` (đối chiếu 2026-09-29) | Chưa chạy tay; lệnh qua runner chưa có test |
| §9 | Không có gì | `seed-tenant-defaults` — **chưa có dòng code nào** trong `src/` |
| §10 | `core outbox-replay` có code thật (đối chiếu 2026-09-30): nhánh `case "outbox-replay":` trong `src/BE/Core/CoreAndSkill.Core.Web/Commands/CoreCommandRunner.cs`; service `src/BE/Core/CoreAndSkill.Core.Infrastructure/Outbox/OutboxReplayService.cs` — `.Where(o => o.Status == OutboxStatus.Dead)` cho `--all-dead`, `message.Replay(` từ chối dòng không `dead`, `RecordAudit(message);` mỗi dòng. Lớp không database của luật E23 xanh (`dotnet test`, 2026-09-30) | Chạy tay trên PostgreSQL thật — **chưa ai chạy**; lớp PostgreSQL của E23 xanh ở máy, CI chưa chạy — hàng E23 ở [`../RULES-BE.md`](../RULES-BE.md) §4 |
| §1, §11 | `database/bu-du-lieu/0001__core__he-thong-don-vi-menu.sql` có thật (đối chiếu 2026-10-08): khối tự chặn khớp nguyên văn mẫu §3.1; chuỗi `RAISE NOTICE 'Da them muc he-thong-don-vi cho don vi he thong'`; không câu nào ghi `core.schema_script_history`. `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Tenants/SystemTenantMenuDatabaseTests.cs` chạy chính tệp hai lần — chuỗi `BackfillScript_RunTwiceOnSystemTenantMissingTheItem_InsertsExactlyOneRowMatchingTheSeedList` | Test đó (nhóm `RequiresDocker`) xanh ở máy (`dotnet test` 2026-11-03, Windows, Docker `postgres:18`), CI chưa chạy; người dùng chạy tệp trên máy dev — **chưa chạy** |

---

## 0. Quyết định nền — áp schema CHẠY TAY

> **Không có `Database.Migrate()` ở bất kỳ đâu trong code sản phẩm. Không auto-migrate lúc khởi
> động. Schema áp bằng script `.sql` do con người chạy.**
>
> Phía code: luật **E9** ([`../RULES.md`](../RULES.md)).

Ràng buộc này được cưỡng chế bằng máy, không bằng câu văn: khối `permissions.deny` trong cấu
hình harness chặn thẳng `dotnet ef database update`, `dotnet ef database drop`,
`dotnet ef migrations remove`. Agent không chạm database, kể cả khi được nhờ.

📖 Quyết định gốc và các phương án đã loại:
[`../adr/0009-ap-schema-chay-tay.md`](../adr/0009-ap-schema-chay-tay.md).

> 🛑 **Không auto-migrate ở BẤT KỲ môi trường nào — kể cả máy dev.** Nguyên tắc: đường đưa thay đổi vào database phải **giống nhau ở mọi môi trường**; chỉ khác ở
> người bấm nút.

File này trả lời năm câu, mỗi câu một mục, cụ thể tới mức chép ra chạy được.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §0

---

## 1. Câu 1 — Script nằm ở đâu

**Một đường dẫn cố định, tính từ gốc repo: `<gốc repo>/database/scripts/`.**

```text
database/
  scripts/
    core/                          ← Core sở hữu, schema `core`
      0001__core__initial.sql
      0099__core__vi-du.sql        ← tên minh hoạ; danh sách thật: ls database/scripts/core/
    modules/
      banhang/                     ← Module `banhang` sở hữu, schema `banhang`
        0001__banhang__initial.sql
    README.md                      ← trỏ về chính file này, không chép nội dung
  bu-du-lieu/
    0001__core__he-thong-don-vi-menu.sql  ← bù dữ liệu cho bản cài ĐÃ bootstrap — §11
  dev/
    tao-db-local.sql               ← CHỈ máy dev: tạo hai role và database cục bộ — §6
```

| Thư mục | Ai được thêm file vào | Chạy khi nào |
| --- | --- | --- |
| `core/` | Chỉ người đang sửa `Core.Infrastructure` | **Trước** mọi script module |
| `modules/<x>/` | Chỉ người đang sửa `Modules.<X>.Infrastructure` | Sau `core/` |
| `bu-du-lieu/` | Lượt thi công một quyết định đòi bù dữ liệu cho bản cài đã có — hôm nay chỉ [ADR-0122](../adr/0122-don-vi-he-thong-duoc-seed-mot-muc-menu-toi-he-thong-don-vi-moi-tai-khoan-cua-no-mang-co-van-hanh.md) điểm 5 | Tay, từng tệp, **chỉ** trên bản cài đã bootstrap trước lượt đó — §11 |

**Dữ liệu không đi trong script schema.** Danh mục quyền là ngoại lệ: nó đi trong migration, tức nằm ngay trong script
schema ([`migration-policy.md`](migration-policy.md) §4.1). Dữ liệu của một đơn vị — vai trò mặc
định, ánh xạ vai trò → quyền, menu — do service tạo đơn vị ghi, gọi từ lệnh bootstrap (§3.3) hoặc
từ khu quản trị hệ thống ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)).
Service chỉ ghi lúc tạo đơn vị, nên mục seed mới không tự tới đơn vị đã có; `bu-du-lieu/` bù đúng chỗ đó, và chỉ khi
một quyết định đòi.

> 🛑 **Hai thư mục trên chỉ chứa script schema đánh số.** Vòng lặp ở §3.3 chạy glob `*.sql` **không có allowlist**.

`database/dev/` nằm **ngoài** `database/scripts/`, nên không vòng lặp nào của §3.3 hay §3.4 nhặt nó; không bảng lịch sử nào ghi nó. Nó không mang dòng dữ liệu nào — chỉ tạo role và database, mật khẩu công khai ([`../adr/0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md`](../adr/0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md)). **Không chạy tệp trong đó ở đâu ngoài máy dev.**

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §1

---

## 2. Câu 2 — Đặt tên thế nào

```text
NNNN__<owner>__<mo-ta-ngan-khong-dau>.sql
```

| Phần | Luật | Ví dụ |
| --- | --- | --- |
| `NNNN` | Bốn chữ số, có số 0 đứng đầu, **cấp theo từng owner** | `0007` |
| `owner` | `core` hoặc khoá module, chữ thường | `core`, `banhang` |
| Mô tả | kebab-case, **không dấu**, ngắn nhưng đủ đoán nội dung | `add-notification` |
| Ngăn cách | **Hai** dấu gạch dưới giữa các phần | `__` |

Ví dụ hợp lệ: `0001__core__initial.sql`, `0008__core__rename-menu-name-to-label-key.sql`,
`0003__banhang__add-index-don-hang-ngay-tao.sql`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §2

### 2.1 Hai người cùng thêm script — cách xử lý

Hai nhánh cùng tạo `0007__core__…` là chuyện sẽ xảy ra. Luật:

> **Số thứ tự được chốt lúc MERGE, không phải lúc tạo nhánh. Ai merge sau thì đổi tên file của
> mình.**

Đổi tên là an toàn **khi và chỉ khi** script đó chưa được áp lên bất kỳ database dùng chung nào; quy trình cấm chạy script từ một nhánh chưa vào `main` lên database dùng chung.

> 🛑 **Không bao giờ đổi tên một script đã có dòng trong `core.schema_script_history` của bất kỳ
> database nào.** Nếu buộc phải đổi: `UPDATE core.schema_script_history SET script_name = …`
> trên **mọi** database, cùng lượt.

**Cổng kiểm trùng số** — luật **E26** ([`../RULES-BE.md`](../RULES-BE.md) §4), 📐: ArchTest cạnh
`PermissionScriptParityTests` kiểm trùng số, khối ghi lịch sử (§3.1) và checksum (§3.2) của mọi script, viết sau
dòng `.gitattributes` ở §3.2. Cho tới khi có, lệnh tra tay:

```bash
for dir in database/scripts/core database/scripts/modules/*; do
  dup=$(ls "$dir" 2>/dev/null | grep -oE '^[0-9]{4}' | sort | uniq -d)
  if [ -n "$dup" ]; then echo "TRÙNG SỐ trong $dir: $dup"; exit 1; fi
done
```

PASS: không in gì, thoát 0.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §2.1

---

## 3. Câu 3 — Chạy theo thứ tự nào, và biết mình đang ở đâu

### 3.1 Bảng lịch sử áp dụng

`core.schema_script_history` — định nghĩa cột đầy đủ ở [`schema-core.md`](schema-core.md) §9.1.
Nó trả lời câu *"database này đã chạy những script nào"* cho **con người**; đừng nhầm với `core.__ef_migrations_history` — hai bảng, hai câu hỏi, không suy ra nhau được.

Mỗi script kết thúc bằng đúng khối này — **chép nguyên văn, kể cả hai dòng comment**, rồi chỉ đổi
tên file và giá trị checksum:

```sql
-- Ghi nhận đã áp — docs/database/script-runbook.md §3.1. Checksum tính trên nội dung file NGAY
-- TRƯỚC khối này (§3.2: grep -abm1 '^-- Ghi nhận đã áp' rồi head -c "$offset" | sha256sum).
INSERT INTO core.schema_script_history (script_name, checksum_sha256, owner, applied_by, note)
VALUES ('0099__core__vi-du.sql',
        'a3f1…',
        'core',
        current_user,
        NULL)
ON CONFLICT (script_name) DO NOTHING;
```

Khối trên là **thứ duy nhất được copy** khi viết script mới. Đừng copy từ script liền trước: khuôn
này chỉ có một bản, ở đây.

**`ON CONFLICT DO NOTHING` là bắt buộc.** **Nằm trong cùng transaction cũng bắt buộc** — đặt ở CUỐI
file, trước `COMMIT;`, trong cùng transaction với phần thay đổi schema.

**Hai dòng comment là một phần của khối, không phải trang trí.** Dòng đầu phải bắt đầu bằng đúng
chuỗi `-- Ghi nhận đã áp` — đó là **điểm cắt** mà lệnh §3.2 tìm để biết hash tới đâu. Cả khối nằm
**sau** điểm cắt đó, nên chữ trong nó không ảnh hưởng giá trị checksum; đổi chữ ở đầu dòng thứ nhất
thì có.

**Khối tự chặn ở đầu script mới.** Script schema mới — Core từ `0008`, mọi script module — mở đầu bằng
khối dưới, đặt **ngay sau** `START TRANSACTION;`, trước mọi lệnh tạo đối tượng (người dùng chốt 2026-10-06;
vế tài khoản đổi 2026-10-07 — [nhật ký quyết định](../adr/nhat-ky-quyet-dinh.md)). Nối nhầm database, hay
nối bằng tài khoản không đóng được vai chủ schema `core`, thì script dừng trước khi tạo gì, và transaction không
để lại dấu vết. Chép nguyên văn, không sửa chữ nào — khuôn này cũng chỉ có một bản, ở đây:

```sql
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
```

Khối nằm **trước** điểm cắt nên thuộc phần được băm (§3.2). Bảy script `0001`–`0007` không mang khối — thêm
vào là đổi checksum đã ghi trên database. Câu kiểm tay *đang ở đâu, là ai* ở §3.3 bước 2 vẫn chạy trước mọi
script: khối này chặn sau khi đã nối, câu kiểm tay chặn trước. Không cổng nào kiểm khối này — E26 chỉ kiểm khối
ghi lịch sử; lớp bắt là review.

**Tài khoản chạy script — định nghĩa gốc.** Mọi chỗ khác trong tệp này nói *chạy bằng `coreandskill_owner`* cho
một script schema thì đọc theo hai hàng dưới.

| Script | Chạy được bằng | Vì sao |
| --- | --- | --- |
| Mang khối tự chặn — Core từ `0008`, mọi script module | `coreandskill_owner`, hoặc tài khoản đóng được vai đó: superuser như `postgres` — kể cả kết nối `postgres` trên DBeaver —, hay role là thành viên của `coreandskill_owner` | Khối tự `SET LOCAL ROLE` sang chủ schema `core` tới hết transaction, nên đối tượng script tạo ra vẫn thuộc chủ đó và khối cấp quyền §3.6 chạy bằng chủ vẫn được. `coreandskill_app` không đóng được vai đó — script dừng với `khong dong duoc vai … (chu schema core)` |
| Không mang khối — Core `0001`–`0007` | **Chỉ** `coreandskill_owner` | Không có gì đổi vai: chạy bằng `postgres` thì schema `core` và mọi bảng thuộc `postgres` (lý do: đoạn *kiểm chủ bảng* ở [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §3.3). Database trống luôn chạy bảy script này, nên §3.3 bước 2 vẫn chạy bằng `coreandskill_owner` |

Hệ quả của hàng đầu:

- `applied_by` ở khối ghi lịch sử là `current_user` **sau** `SET LOCAL ROLE`, tức chủ schema `core` — không phải tài
  khoản đã nối. Với script mang khối, lịch sử không còn ghi ai thật sự bấm chạy.
- Khối đọc chủ từ chính schema `core` đang có, không so với tên `coreandskill_owner`. Schema `core` đã lỡ thuộc
  `postgres` — `0001`–`0007` chạy bằng `postgres` — thì khối cho qua và đối tượng mới cũng thuộc `postgres`; chỉ
  câu kiểm chủ bảng ở bảng nghiệm thu §3.3 bắt được.
- `SET LOCAL` hết hiệu lực ở `COMMIT`: lệnh chạy sau script trên cùng kết nối lại mang tài khoản đã nối.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §3.1

### 3.2 Tính `checksum_sha256`

**Checksum là của phần nội dung script TRƯỚC khối ghi lịch sử ở §3.1 — không bao giờ là hash của
file đã có khối đó.** Lệnh dưới luôn cắt bỏ khối `INSERT` (nếu đã có) trước khi hash, cắt theo
**byte** chứ không qua xử lý dòng của `awk`/`sed` — cắt theo dòng làm một số công cụ tự bỏ `\r`
lúc đọc, ra hash khác byte thật của file có CRLF. Dùng đúng ở cả hai thời điểm: lúc soạn thảo
(file chưa có khối) lẫn lúc đối soát lại sau này ở §3.5 (3) (file đã commit, đã có khối).

```bash
file=database/scripts/core/0099__core__vi-du.sql
offset=$(grep -abm1 '^-- Ghi nhận đã áp' "$file" | cut -d: -f1)
head -c "$offset" "$file" | sha256sum
```

Chạy trên Git Bash. Đã tự kiểm lại đúng cách này trên `0001__core__initial.sql` (2026-09-17): ra
đúng `2be447a3aa4d1c1b4add8716d9b0a2ed2f71fc88237de82f369cdf20133123e7`, khớp từng ký tự với giá
trị đã dán ở cột `checksum_sha256` của khối ghi lịch sử cuối file đó.

> 🛑 **Trỏ vào khối ghi lịch sử, không trỏ vào số dòng** — mỗi script có đúng **một** khối (§3.1).

Dán giá trị vào khối `INSERT` **trước khi** commit script.

> 🛑 **Mọi lần GHI lại một script đã áp đều có thể làm lệch checksum — kể cả khi chỉ sửa comment.**
> Script trong `database/scripts/` là UTF-8 **có BOM, xuống dòng CRLF** (khuôn `dotnet ef migrations
> script` sinh ra trên Windows). Nhiều công cụ ghi đè âm thầm chuẩn hoá: `sed -i` và một số trình
> soạn thảo đổi CRLF → LF cho **cả file**, làm mọi byte sau đó dịch chỗ và hash đổi hẳn. Đã đo thật
> trên `0001__core__initial.sql` (2026-09-19): sửa bằng công cụ giữ nguyên BOM/CRLF thì offset vẫn
> `21496` và hash vẫn `2be447a3aa4d…`; sửa bằng `sed -i` thì offset tụt còn `20964` và hash thành
> `f932765b221e…` — một script đã áp lên database thật lập tức bị câu kiểm §3.5 (3) báo `LECH`.
>
> **Vì vậy:** sửa script bằng công cụ **giữ nguyên BOM và CRLF**, và **sau mỗi lần ghi, chạy lại
> lệnh §3.2 trên chính file đó** rồi so với giá trị đang lưu trong khối `INSERT` của nó. Hash đổi ⇒
> **hoàn tác ngay**, đừng cập nhật giá trị lưu — giá trị lưu là thứ database đã ghi nhận, sửa nó chỉ
> xoá dấu vết chứ không sửa được gì.
>
> Nhắc lại cho rõ, vì hay bị suy luận nhầm theo chiều ngược: nội dung **khối ghi lịch sử ở §3.1** nằm
> **sau** điểm cắt, nên sửa chữ trong khối đó **không** đổi checksum. Thứ làm đổi checksum là công cụ
> ghi, không phải chỗ sửa.

**`.gitattributes` gốc repo giữ CRLF trên mọi máy.** Dòng `database/scripts/**/*.sql text eol=crlf` ép
xuống dòng CRLF khi checkout trên mọi hệ điều hành (người dùng chốt 2026-10-06, Q06 —
[nhật ký quyết định](../adr/nhat-ky-quyet-dinh.md)). Kho git lưu script ở LF; thiếu dòng đó thì clone trên
Linux — máy chủ bản cài, CI — ra LF, và lệnh trên ra hash khác giá trị đã lưu ở mọi script. Gỡ dòng đó,
hay thêm một dòng sau nó khớp cùng tệp với `eol` khác, là làm lệch checksum trên mọi clone mới. ✅ Có thật
(đối chiếu 2026-10-06): `.gitattributes`, chuỗi `database/scripts/**/*.sql text eol=crlf`. Chưa ai ghi lại
một lần chạy lệnh trên trong một clone sạch trên Linux.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §3.2

### 3.3 Quy trình từ database TRỐNG — một đường cho mọi môi trường

**Năm bước, theo đúng thứ tự.** Bước 2, 3, 5 giống hệt nhau ở máy dev và bản cài thật. Máy dev khác
ở bước 1 — chạy một tệp dành riêng với mật khẩu công khai — và ở bước 4 — giá trị có sẵn trong
`appsettings.Development.json`: §6.

| # | Bước | Chạy bằng |
| --- | --- | --- |
| 1 | Tạo hai tài khoản database và database rỗng | Tài khoản quản trị cụm Postgres |
| 2 | Áp script schema — Core rồi tới module. Script mang sẵn danh mục quyền: khoá Core trong script Core, khoá module trong script của module | `coreandskill_owner` — database trống chạy cả `0001`–`0007`, không mang khối tự chặn: §3.1 *Tài khoản chạy script* |
| 3 | Cấp quyền cho tài khoản ứng dụng — §3.6 | `coreandskill_owner` |
| 4 | Đặt cấu hình cho lệnh bootstrap: hai đơn vị, hai tài khoản | Người vận hành |
| 5 | Chạy lệnh bootstrap **một lần** | `coreandskill_app`, qua chuỗi kết nối của ứng dụng |

Luồng nghiệp vụ của cả quy trình: [`../luong/V1-cai-dat-lan-dau.md`](../luong/V1-cai-dat-lan-dau.md).
Vì sao hai tài khoản ở hai đơn vị: [`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md).

Ba biến dùng xuyên suốt: `PGHOST`, `PGADMIN` (tài khoản quản trị cụm Postgres), `PGDATABASE`
(tên database của ứng dụng).

**Bản cài Compose** ([ADR-0120](../adr/0120-https-ket-thuc-o-caddy-trong-compose-header-chia-doi-postgres-quan-tri-cum-rieng-quet-bundle-fe-khi-dung-ban-cai.md)
quyết định 3, QC4). Container `db` không mở cổng ra máy chủ, nên `psql` của bước 1–3 chạy **trong** container,
qua socket Unix, bằng `docker compose exec` ở thư mục gốc repo — nơi có `docker-compose.yml`. Lệnh của bước 1–3
và §3.6 giữ nguyên, đổi đúng bốn chỗ:

| Ở lệnh bước 1–3, §3.6 | Bản cài Compose |
| --- | --- |
| Ba biến | `PGHOST=/var/run/postgresql` — thư mục socket trong container. `PGADMIN=postgres` — tài khoản quản trị cụm riêng, mật khẩu `POSTGRES_ADMIN_PASSWORD` của `.env`. `PGDATABASE` = giá trị `POSTGRES_DB` của `.env` — chuỗi kết nối của `api` đọc đúng biến đó |
| `psql …`, `createdb …` | Thêm tiền tố `docker compose exec -T db` — có `-T` thì khối `<<'SQL'` mới vào được container |
| `psql … -c '\password …'` | Tiền tố `docker compose exec db`, **không** `-T` — lệnh hỏi mật khẩu cần terminal |
| `-f "$f"` trong vòng của bước 2 | `-f "/scripts/${f#database/scripts/}"` — `./database/scripts` gắn chỉ-đọc ở `/scripts` của `db` |

Mật khẩu gõ cho `coreandskill_app` ở bước 1 phải bằng `POSTGRES_APP_PASSWORD` của `.env`, và `POSTGRES_APP_USER`
phải là `coreandskill_app` — `api` nối bằng hai biến đó. Câu kiểm *đang ở đâu, là ai* ra cột thứ ba rỗng: nối qua
socket. Bước 4: khoá `Core:Bootstrap:*` đặt ở `.env`. Bước 5: `docker compose run --rm api core bootstrap` —
image đã lấy `dotnet CoreAndSkill.Api.dll` làm điểm vào, nên chỉ truyền `core bootstrap`. Qua socket trong
container, image Postgres chính thức không hỏi mật khẩu: ai chạy được `docker compose exec` trên máy chủ thì
vào được mọi tài khoản database — chấp nhận ở ADR-0120 QC4.

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.**
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | Đối chiếu 2026-10-06: `docker-compose.yml` — service `db` chuỗi `POSTGRES_USER: postgres` và `./database/scripts:/scripts:ro`; service `api` chuỗi `Database=${POSTGRES_DB:-coreandskill};Username=${POSTGRES_APP_USER}`; `src/BE/CoreAndSkill.Api/Dockerfile`, chuỗi `ENTRYPOINT ["dotnet", "CoreAndSkill.Api.dll"]` | Một lần chạy `docker compose up` rồi đường này trên máy thật — **chưa ai chạy**, chưa đo |

**Bước 1 — hai tài khoản database và database rỗng.**

```bash
# Chay bang tai khoan quan tri cum. Chay lai duoc: role da co thi bo qua.
psql -h "$PGHOST" -U "$PGADMIN" -d postgres -v ON_ERROR_STOP=1 <<'SQL'
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'coreandskill_owner') THEN
    CREATE ROLE coreandskill_owner LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'coreandskill_app') THEN
    CREATE ROLE coreandskill_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE;
  END IF;
END $$;
SQL

# Mat khau hai role: go tuong tac. KHONG dat trong tep, KHONG de lai trong lich su lenh.
psql -h "$PGHOST" -U "$PGADMIN" -d postgres -c '\password coreandskill_owner'
psql -h "$PGHOST" -U "$PGADMIN" -d postgres -c '\password coreandskill_app'

# Database rong, chu so huu la coreandskill_owner, ma hoa UTF8 — ghim ca hai co, khong
# ke thua template1 cua cum (giong database/dev/tao-db-local.sql).
createdb -h "$PGHOST" -U "$PGADMIN" -O coreandskill_owner -E UTF8 -T template0 "$PGDATABASE"
```

**Bước 2 — script schema.** Core trước, module sau; trong mỗi bên theo đúng thứ tự số.

```bash
# Dang o dau, la ai — chay TRUOC moi script. Mong doi: <database ung dung>|coreandskill_owner|
# <dia chi may chu dinh nham> (NULL khi noi qua Unix socket). Lech mot cot thi DUNG.
psql -h "$PGHOST" -U coreandskill_owner -d "$PGDATABASE" -At \
     -c 'SELECT current_database(), current_user, inet_server_addr()'

for f in database/scripts/core/*.sql; do
  echo ">>> $f"
  psql -h "$PGHOST" -U coreandskill_owner -d "$PGDATABASE" -v ON_ERROR_STOP=1 -f "$f" \
    || { echo "DUNG tai $f"; break; }
done

# Tung module du an nay that su lap — chi module CO thu muc script (doan duoi khoi).
for f in database/scripts/modules/banhang/*.sql; do
  echo ">>> $f"
  psql -h "$PGHOST" -U coreandskill_owner -d "$PGDATABASE" -v ON_ERROR_STOP=1 -f "$f" \
    || { echo "DUNG tai $f"; break; }
done
```

**Câu kiểm *đang ở đâu, là ai*** là câu `SELECT` đầu khối; §6, §7 dùng đúng câu này. Cột một luôn phải là database ứng dụng; cột hai là tài khoản sẽ chạy script, được phép theo §3.1 *Tài khoản chạy script* — ở đây `coreandskill_owner`, vì vòng trên có `0001`–`0007`. Công cụ đồ hoạ: chạy nó trong chính cửa sổ SQL sẽ chạy script. Lỡ chạy nhầm chỗ: §6 *Lỡ chạy nhầm*.

Vòng module chỉ chạy cho module có thư mục `database/scripts/modules/<module>/` (`ls database/scripts/modules/`); chưa có thì bỏ vòng đó, nếu không nó in `DUNG` oan.

Một vòng in `DUNG` thì **không sang bước kế**: đọc lỗi, sửa, dựng lại từ đầu. Xong bước 2 là `core.permission` đã có dòng của cả Core lẫn module ([`migration-policy.md`](migration-policy.md) §1, §4.1) — không có bước nạp danh mục quyền riêng.

> 🛑 **`-v ON_ERROR_STOP=1` là bắt buộc** ở mọi lần gọi `psql -f` trong file này: mặc định `psql` **chạy tiếp** sau khi một câu lệnh lỗi và thoát với mã 0. **Trong DBeaver/pgAdmin:** bấm **Execute script** (`Alt+X` ở DBeaver), **không** phải `Ctrl+Enter`.
>
> ⚠️ **Script schema mang BOM UTF-8 ở đầu tệp.** pgAdmin 9.15 bỏ BOM khi mở tệp (đọc mã nguồn pgAdmin, 2026-09-28). DBeaver và `psql`: **chưa ai kiểm** chúng xử lý BOM thế nào. Test tích hợp bỏ BOM trước khi gọi `psql -f` để khỏi phụ thuộc phiên bản `psql` — `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Support/PostgresFixture.cs`, chuỗi `WithoutUtf8Bom`. Tệp đầu tiên dừng ngay ở câu đầu với lỗi cú pháp thì nghi BOM trước.

**Cụm phải có ICU và gói contrib** ([ADR-0119](../adr/0119-cot-ten-dung-collation-icu-core-vi-vn-tim-khong-dau-qua-unaccent-is-deleted-co-default-false.md)).
Script Core `0009` tạo collation `core.vi_vn` với `PROVIDER = icu` và cài extension `unaccent` vào schema `public`
([`schema-core.md`](schema-core.md) §3.9). Kiểm gói contrib trước bước 2, nối vào database ứng dụng:
`SELECT default_version FROM pg_available_extensions WHERE name = 'unaccent'` — mong đợi **một** dòng. Cụm thiếu ICU thì
`0009` dừng ở câu `CREATE COLLATION`. Cả hai ca, transaction của script không để lại gì: cài phần cụm còn thiếu rồi xử
lý như mọi vòng in `DUNG` (đoạn trên).

**Chủ database không tạo được extension.** `0009` chạy câu `CREATE EXTENSION` bằng `coreandskill_owner` — kể cả khi nối bằng `postgres`, vì khối tự chặn đã đổi vai (§3.1); tài khoản đó
đủ quyền hay không **chưa kiểm trên cụm nào**. Script dừng ở câu đó với lỗi quyền thì tài khoản quản trị cụm tạo trước —
ngay sau bước 1 với database trống, trước khi áp `0009` với database đang có dữ liệu (§3.4):

```bash
psql -h "$PGHOST" -U "$PGADMIN" -d "$PGDATABASE" -v ON_ERROR_STOP=1 \
     -c 'CREATE EXTENSION IF NOT EXISTS unaccent SCHEMA public'
```

Câu trong `0009` khi đó không làm gì. Extension thuộc tài khoản quản trị cụm, nên gỡ nó — đường rút lui của ADR-0119 —
cũng phải bằng tài khoản đó. Bản cài Compose: thêm tiền tố `docker compose exec -T db` như bảng trên.

**Bước 3 — cấp quyền cho tài khoản ứng dụng.** Chạy khối cấp quyền ở §3.6, rồi câu nghiệm thu ngay
dưới nó.

**Bước 4 — cấu hình cho lệnh bootstrap.**

**Khoá cấu hình của lệnh bootstrap — định nghĩa gốc.** Mọi khoá nằm dưới tiền tố `Core:Bootstrap:`, khoá lá viết
PascalCase; file này là chủ của tên khoá — thêm khoá thì thêm dòng vào bảng này, không khai ở chỗ khác. Nguồn giá trị
theo môi trường: bảng *Môi trường* dưới. Tên lệnh và tham số:
[`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3, bảng *Lệnh của runner*.

| Giá trị | Khoá | Bí mật |
| --- | --- | --- |
| Mã **đơn vị hệ thống**. Mã này là thứ tài khoản vận hành gõ vào ô mã đơn vị ở chế độ vận hành của trang đăng nhập (liên kết *Quản trị hệ thống*); **không** đặt vào cấu hình FE | `Core:Bootstrap:SystemTenantCode` | Không |
| Tên **đơn vị hệ thống** | `Core:Bootstrap:SystemTenantName` | Không |
| Mã **đơn vị nghiệp vụ đầu tiên**. Phải bằng `tenantCode` trong tệp môi trường của bản cài của FE — đoạn *Cùng giá trị ở FE* dưới. Phải khác `SystemTenantCode` sau khi cắt khoảng trắng và đổi chữ HOA — trùng thì lệnh dừng, mã thoát 1, không ghi dòng nào (luật M16) | `Core:Bootstrap:FirstTenantCode` | Không |
| Tên **đơn vị nghiệp vụ đầu tiên** | `Core:Bootstrap:FirstTenantName` | Không |
| Tên đăng nhập **tài khoản vận hành hệ thống** (ví dụ `superadmin`) — mang `is_system_operator` và `must_change_password`, thuộc đơn vị hệ thống | `Core:Bootstrap:OperatorUserName` | Không |
| Mật khẩu tài khoản vận hành hệ thống | `Core:Bootstrap:OperatorPassword` | **Có** |
| Email tài khoản vận hành hệ thống — **bắt buộc**: Identity chạy với email duy nhất nên email rỗng bị từ chối ([`schema-core.md`](schema-core.md) §4.1, [`../adr/0091-bootstrap-doi-hai-khoa-email-bat-buoc.md`](../adr/0091-bootstrap-doi-hai-khoa-email-bat-buoc.md)) | `Core:Bootstrap:OperatorEmail` | Không |
| Tên đăng nhập **tài khoản quản trị đơn vị** (ví dụ `admin`) — mang `has_permission_bypass` và `must_change_password`, thuộc đơn vị nghiệp vụ đầu tiên | `Core:Bootstrap:AdminUserName` | Không |
| Mật khẩu tài khoản quản trị đơn vị | `Core:Bootstrap:AdminPassword` | **Có** |
| Email tài khoản quản trị đơn vị — **bắt buộc**, cùng lý do. Được trùng với email vận hành: hai tài khoản ở hai đơn vị | `Core:Bootstrap:AdminEmail` | Không |

> 📖 Vì sao nhóm khoá này không gắn `ValidateOnStart`, và nó được kiểm ở mốc nào thay thế: đọc
> [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §4.4

| Môi trường | Nguồn giá trị |
| --- | --- |
| Máy dev | **Mọi khoá có sẵn** trong `appsettings.Development.json` — không phải đặt gì. Hai mật khẩu ở đó là giá trị dev công khai, chỉ dùng được trên database cục bộ ([`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §6.1); hai tài khoản mang `must_change_password`. Hai khoá email là địa chỉ ví dụ trên tên miền `example.com`, không phải email thật của ai. Muốn giá trị riêng: `dotnet user-secrets set "Core:Bootstrap:<khoá>" "<giá trị>" --project src/BE/CoreAndSkill.Api` — nguồn này nạp sau tệp nên thắng. 🛑 Chuỗi kết nối trỏ database trên máy khác thì **đè luôn hai mật khẩu**, nếu không lệnh bootstrap tạo tài khoản mang mật khẩu công khai trên database đó |
| Bản cài thật | Nguồn bí mật của môi trường đó — [`../wiki-core/be/18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §2. `user-secrets` chỉ được nạp ở môi trường Development |

**Cùng giá trị ở FE.** Mã đơn vị nghiệp vụ còn một chỗ thứ hai: khoá `tenantCode` trong tệp môi
trường của bản cài, tệp mà build FE `production` bắt buộc phải có — người dùng của công ty không gõ
mã, trang đăng nhập gửi giá trị này ([`../adr/0116-ma-don-vi-nghiep-vu-lay-tu-cau-hinh-ban-cai-form-hai-o-van-hanh-qua-lien-ket-rieng.md`](../adr/0116-ma-don-vi-nghiep-vu-lay-tu-cau-hinh-ban-cai-form-hai-o-van-hanh-qua-lien-ket-rieng.md)).
Người đặt khoá bootstrap ở bước này tạo luôn tệp đó, với **đúng** mã vừa đặt cho
`Core:Bootstrap:FirstTenantCode`. Tên tệp, mẫu để chép:
[`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §1.1; build và triển khai FE:
[`../wiki-core/fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §8. Không
máy nào so hai giá trị — hàng *Đăng nhập tài khoản quản trị đơn vị* ở bảng nghiệm thu dưới là chỗ
duy nhất bắt được chúng lệch. Máy dev: cả hai có sẵn trong git và phải cùng giá trị.

Chuỗi kết nối của ứng dụng dùng `coreandskill_app`, **không** dùng `coreandskill_owner` — lý do
ở §3.6.

**Bước 5 — lệnh bootstrap.**

```bash
dotnet run --project src/BE/CoreAndSkill.Api -- core bootstrap
```

Lệnh gọi **service tạo đơn vị** — cùng service mà endpoint tạo đơn vị ở khu quản trị hệ thống gọi
([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md))
— để tạo đơn vị hệ thống, đơn vị nghiệp vụ đầu tiên kèm dữ liệu mặc định của nó, và hai tài khoản
ở bước 4. Xong thì **thoát, không mở cổng**. Tính chất bắt buộc:

- **Thiếu một giá trị ở bước 4, một email sai dạng hoặc dài quá cột, hoặc một tên đăng nhập ở bước 4 là tên dành riêng
  ([`../contracts/users.md`](../contracts/users.md) §5, *Tên dành riêng*) ⇒ thoát mã 1, không ghi dòng nào.**
- **Chạy lại không nhân đôi.**
- **Tài khoản đi qua `UserManager`, không qua SQL** ([`schema-core.md`](schema-core.md) §4.1).
- **Tiến trình API phục vụ thật không tạo gì cả.**

> 🛑 **Từ đơn vị thứ hai trở đi: không dùng dòng lệnh.** Tạo đơn vị đi qua khu quản trị hệ thống,
> bằng tài khoản vận hành vừa tạo ([`../luong/V2-tao-don-vi-moi.md`](../luong/V2-tao-don-vi-moi.md)).

Tên `superadmin` / `admin` là **dữ liệu** do người vận hành đặt, không phải vai trò (luật **S10**).

**Nghiệm thu từng bước:**

| Sau bước | Câu kiểm | Mong đợi |
| --- | --- | --- |
| 1 | `SELECT rolname, rolsuper, rolcreatedb, rolcreaterole FROM pg_roles WHERE rolname IN ('coreandskill_owner', 'coreandskill_app')` | Đúng **hai** dòng; ba cột cờ đều `false` |
| 1 | `SELECT pg_has_role('coreandskill_app', 'coreandskill_owner', 'MEMBER')` | `false` |
| 1 | Nối vào database ứng dụng: `SELECT pg_encoding_to_char(encoding) FROM pg_database WHERE datname = current_database()` | `UTF8` |
| 2 | Các câu kiểm ở [`schema-core.md`](schema-core.md) §11 | Đúng như "mong đợi" ghi trong từng câu |
| 2 | `SELECT DISTINCT tableowner FROM pg_tables WHERE schemaname = 'core'` | Đúng **một** dòng: `coreandskill_owner`. Tên khác ⇒ bước 2 chạy nhầm tài khoản — dựng lại từ đầu |
| 2 | `SELECT count(*) FROM core.permission` | Khác 0 |
| 3 | Câu nghiệm thu quyền ở §3.6 | 0 dòng |
| 5 | Máy dev: bỏ một giá trị ở bước 4, chạy bước 5, rồi `SELECT count(*) FROM core.tenant` | `0` — lệnh dừng mà không ghi gì |
| 5 | `SELECT code, is_system FROM core.tenant ORDER BY is_system DESC` | Đúng **hai** dòng: mã đơn vị hệ thống với `is_system = true`, mã đơn vị nghiệp vụ đầu tiên với `false` — khớp giá trị ở bước 4 |
| 5 | Chạy lại bước 5, rồi lặp câu kiểm trên | Vẫn đúng hai dòng |
| 5 | Đăng nhập tài khoản quản trị đơn vị ở trang đăng nhập hai ô — **không** gõ mã đơn vị | Thành công, và `mustChangePassword` là `true`. Trượt với câu *sai thông tin đăng nhập* dù mật khẩu đúng ⇒ `tenantCode` của FE lệch mã đơn vị nghiệp vụ — sửa tệp môi trường của bản cài rồi build lại FE |
| 5 | Đăng nhập tài khoản vận hành qua liên kết *Quản trị hệ thống*, gõ mã đơn vị hệ thống | Thành công, và `mustChangePassword` là `true` |
| 5 | Tài khoản vận hành chưa đổi mật khẩu gọi một endpoint của khu quản trị hệ thống | **403** `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` — buộc đổi mật khẩu trước mọi thao tác |
| 5 | Tài khoản vận hành đổi mật khẩu, rồi mở khu quản trị hệ thống qua mục *Đơn vị* cuối `Sidebar` | Vào được, **không** thấy dữ liệu nghiệp vụ của đơn vị nghiệp vụ đầu tiên |
| 5 | Luật M16: `SELECT count(*) FROM core.app_user u JOIN core.tenant t ON t.id = u.tenant_id WHERE t.is_system AND NOT u.is_system_operator` | `0`. Khác 0 thì mục *Đơn vị* hiện cho tài khoản bị 403 khi bấm — dấu hiệu sai 3 của [ADR-0122](../adr/0122-don-vi-he-thong-duoc-seed-mot-muc-menu-toi-he-thong-don-vi-moi-tai-khoan-cua-no-mang-co-van-hanh.md) |
| 5 | Luật M17: `SELECT t.is_system, m.code FROM core.menu_item m JOIN core.tenant t ON t.id = m.tenant_id WHERE m.route LIKE '/he-thong%' AND NOT m.is_deleted` | Đúng **một** dòng: `true`, `he-thong-don-vi`. Không dòng nào trên bản cài bootstrap trước lượt ADR-0122 ⇒ script bù ở §11 |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §3.3

### 3.4 Quy trình trên database ĐANG CÓ DỮ LIỆU

Khác ở chỗ: **không chạy từ đầu**, chỉ chạy phần còn thiếu.

```sql
-- Bước 1: xem đang ở đâu.
SELECT script_name, owner, applied_at, applied_by
FROM   core.schema_script_history
ORDER  BY owner, script_name;
```

```bash
# Bước 2: đối chiếu với file trong repo — in ra script còn THIẾU.
psql -h "$PGHOST" -U "$PGUSER" -d "$PGDATABASE" -At \
     -c "SELECT script_name FROM core.schema_script_history" | sort > /tmp/da-chay.txt
(cd database/scripts && ls core/*.sql modules/*/*.sql | xargs -n1 basename) | sort > /tmp/trong-repo.txt
comm -13 /tmp/da-chay.txt /tmp/trong-repo.txt        # còn thiếu, theo đúng thứ tự
```

```bash
# Bước 3: chạy đúng những file ở bước 2, theo thứ tự, TỪNG FILE MỘT, đọc kết quả từng cái.
psql -h "$PGHOST" -U coreandskill_owner -d "$PGDATABASE" -v ON_ERROR_STOP=1 \
     -f database/scripts/core/<tên file>.sql
```

**Từng file một, không vòng lặp**, khi database có dữ liệu thật. File mang khối tự chặn thì `-U` nhận cả tài khoản đóng được vai chủ schema `core`, như `postgres`; file không mang khối thì chỉ `coreandskill_owner` — §3.1 *Tài khoản chạy script*. Câu kiểm *đang ở đâu, là ai* (§3.3 bước 2) vẫn chạy trước file đầu: cột một phải là database ứng dụng, cột hai là tài khoản vừa chọn. Sắp áp Core `0009`: đọc trước *Cụm phải có ICU và gói contrib* ở §3.3.

**Bước 4: chạy lại khối cấp quyền ở §3.6, rồi câu nghiệm thu của nó.** Bảng do script vừa áp tạo
ra chưa có quyền nào cho tài khoản ứng dụng.

**Bước 5, chỉ khi câu nghiệm thu M17 ở §3.3 ra không dòng nào:** script bù dữ liệu ở §11.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §3.4

### 3.5 Ba câu kiểm sức khoẻ

```sql
-- (1) Script trong repo mà DB chưa chạy?  → dùng lệnh comm ở §3.4.

-- (2) Script DB đã chạy mà repo không còn?  Mong đợi: 0 dòng.
--     Có dòng nghĩa là ai đó đã xoá hoặc đổi tên một script đã áp (§2.1).
SELECT script_name FROM core.schema_script_history
WHERE  script_name NOT IN ( /* dán danh sách tên file từ repo vào đây */ );

-- (3) Nội dung script đã đổi sau khi áp?
--     KHÔNG so bằng sha256sum của nguyên file trong repo — file đã commit mang thêm khối ghi lịch
--     sử ở §3.1, còn checksum lưu ở đây chỉ tính trên phần TRƯỚC khối đó (§3.2). So trực tiếp
--     sha256sum của nguyên file sẽ lệch 100% số lần, kể cả với script chưa ai sửa gì.
SELECT script_name, checksum_sha256 FROM core.schema_script_history ORDER BY script_name;
```

Tính lại đúng cách (§3.2) cho toàn bộ script rồi so với DB:

```bash
psql -h "$PGHOST" -U "$PGUSER" -d "$PGDATABASE" -At -F',' \
     -c "SELECT script_name, checksum_sha256 FROM core.schema_script_history" \
| while IFS=',' read -r name db_hash; do
    file=$(find database/scripts -name "$name")
    offset=$(grep -abm1 '^-- Ghi nhận đã áp' "$file" | cut -d: -f1)
    real_hash=$(head -c "$offset" "$file" | sha256sum | cut -d' ' -f1)
    [ "$db_hash" = "$real_hash" ] || echo "LECH: $name (DB=$db_hash, file=$real_hash)"
done
```

PASS: không in gì. Đã tự kiểm cách cắt byte này khớp đúng checksum thật của
`0001__core__initial.sql` — xem §3.2.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §3.5

### 3.6 Quyền của hai tài khoản database — định nghĩa gốc

| Tài khoản | Ai dùng | Quyền |
| --- | --- | --- |
| `coreandskill_owner` | Người vận hành, **chỉ** để áp script schema (§3.3 bước 2, §3.4) và chạy khối cấp quyền dưới đây | Chủ database và mọi schema của ứng dụng |
| `coreandskill_app` | Tiến trình ứng dụng — **kể cả** lệnh bootstrap (§3.3 bước 5) và lệnh khôi phục (§8) | Bảng dưới |

| Bảng | `coreandskill_app` được | Vì sao |
| --- | --- | --- |
| `core.audit_log` | `SELECT`, `INSERT` — **không** `UPDATE`, `DELETE`, `TRUNCATE` | Nhật ký chỉ ghi thêm, và bất biến đó ép bằng quyền DB chứ không bằng quy ước ([`schema-core.md`](schema-core.md) §9.4) |
| `core.permission`, `core.permission_resource` | Chỉ `SELECT` | Danh mục vào database bằng migration ([`migration-policy.md`](migration-policy.md) §4.1); tiến trình ứng dụng không ghi danh mục |
| `core.schema_script_history`, và bảng lịch sử migration EF của **mọi** schema — `<schema>.__ef_migrations_history`, gồm `core.__ef_migrations_history` ([`migration-policy.md`](migration-policy.md) §1) | Chỉ `SELECT` | Hai loại bảng này nói *schema đang ở đâu*, và chỉ người vận hành áp script (§3.3 bước 2, §3.4) mới được ghi chúng. Ứng dụng cần đọc để tự từ chối khởi động khi còn thiếu migration (§5.1); ghi được thì một tiến trình lỗi tự đánh dấu một migration là đã áp |
| Mọi bảng khác của schema `core` và của schema module | `SELECT`, `INSERT`, `UPDATE`, `DELETE` | — |

Luật **M13** ép bảng trên bằng câu nghiệm thu cuối mục này, chạy tay; integration test chạy bằng chính
tài khoản ứng dụng là 📐 chưa viết — hàng M13 ở [`../RULES-BE.md`](../RULES-BE.md). Ứng dụng **không**
chạy bằng tài khoản chủ.

**Khối cấp quyền** — chạy bằng `coreandskill_owner` ở §3.3 bước 3 và **sau MỖI lần áp script**
(§3.4); **không** dùng `ALTER DEFAULT PRIVILEGES` thay thế. Chạy lại bao nhiêu lần cũng cho cùng một kết quả.

```bash
psql -h "$PGHOST" -U coreandskill_owner -d "$PGDATABASE" -v ON_ERROR_STOP=1 <<'SQL'
BEGIN;
GRANT USAGE ON SCHEMA core TO coreandskill_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA core TO coreandskill_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA core TO coreandskill_app;

REVOKE UPDATE, DELETE, TRUNCATE ON core.audit_log FROM coreandskill_app;
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON core.permission, core.permission_resource FROM coreandskill_app;
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON core."__ef_migrations_history", core.schema_script_history FROM coreandskill_app;

-- Moi schema module du an nay lap: lap lai ba dong GRANT dau voi ten schema do,
-- roi REVOKE INSERT, UPDATE, DELETE, TRUNCATE tren <schema>."__ef_migrations_history" cua schema do.
COMMIT;
SQL
```

`GRANT` và `REVOKE` nằm trong **một** transaction.

**Câu nghiệm thu (luật M13).** Mong đợi: **0 dòng**. Mỗi dòng là một quyền đang lệch bảng trên.

```sql
-- (a) Bảng có tên cố định
SELECT v.bang, v.quyen, v.mong_doi
FROM (VALUES
  ('core.audit_log',           'SELECT',   true),
  ('core.audit_log',           'INSERT',   true),
  ('core.audit_log',           'UPDATE',   false),
  ('core.audit_log',           'DELETE',   false),
  ('core.audit_log',           'TRUNCATE', false),
  ('core.permission',          'SELECT',   true),
  ('core.permission',          'INSERT',   false),
  ('core.permission',          'UPDATE',   false),
  ('core.permission',          'DELETE',   false),
  ('core.permission',          'TRUNCATE', false),
  ('core.permission_resource', 'SELECT',   true),
  ('core.permission_resource', 'INSERT',   false),
  ('core.permission_resource', 'UPDATE',   false),
  ('core.permission_resource', 'DELETE',   false),
  ('core.permission_resource', 'TRUNCATE', false),
  ('core.schema_script_history',   'SELECT',   true),
  ('core.schema_script_history',   'INSERT',   false),
  ('core.schema_script_history',   'UPDATE',   false),
  ('core.schema_script_history',   'DELETE',   false),
  ('core.schema_script_history',   'TRUNCATE', false)
) AS v(bang, quyen, mong_doi)
WHERE has_table_privilege('coreandskill_app', v.bang, v.quyen) IS DISTINCT FROM v.mong_doi
UNION ALL
-- (b) Bảng lịch sử migration EF của MỌI schema — khuôn tên ở migration-policy.md §1
SELECT format('%I.%I', t.schemaname, t.tablename), q.quyen, q.mong_doi
FROM pg_catalog.pg_tables t
CROSS JOIN (VALUES ('SELECT', true), ('INSERT', false), ('UPDATE', false),
                   ('DELETE', false), ('TRUNCATE', false)) AS q(quyen, mong_doi)
WHERE t.tablename = '__ef_migrations_history'
  AND has_table_privilege('coreandskill_app', format('%I.%I', t.schemaname, t.tablename), q.quyen)
      IS DISTINCT FROM q.mong_doi
UNION ALL
-- (c) Chốt tập đầu vào: không thấy bảng lịch sử nào thì phần (b) đang không kiểm gì
SELECT 'KHONG THAY __ef_migrations_history NAO', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM pg_catalog.pg_tables WHERE tablename = '__ef_migrations_history');
```

Phần (c) chốt tập đầu vào — luật T6 ([`../RULES.md`](../RULES.md) §8); khuôn tên bảng lịch sử là luật ở [`migration-policy.md`](migration-policy.md) §1.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §3.6

---

## 4. Câu 4 — Sinh script bằng lệnh gì

**Chạy từ gốc repo.** Project chứa migration là `Core.Infrastructure`; startup project là host.

```bash
# Script cho MỘT migration mới (từ migration trước đó tới nó).
dotnet ef migrations script AddIndexRolePermission AddNotificationRecipientTable \
    --idempotent \
    --project        src/BE/Core/CoreAndSkill.Core.Infrastructure \
    --startup-project src/BE/CoreAndSkill.Api \
    --context        CoreDbContext \
    --output         database/scripts/core/0009__core__add-notification-recipient.sql
```

| Tham số | Nghĩa |
| --- | --- |
| `--from` (đối số thứ nhất) | Migration **đã có** trên database đích. Không bỏ trống |
| `--to` (đối số thứ hai) | Migration đích. Bỏ trống ⇒ tới migration cuối cùng |
| `--idempotent` | Xem §4.1 — bắt buộc |
| `--project` | Nơi migration sống. Với schema `core` là `Core.Infrastructure` ([`migration-policy.md`](migration-policy.md) §1) |
| `--startup-project` | Nơi `dotnet ef` lấy cấu hình (connection string, design-time factory) |
| `--context` | **Bắt buộc khi có nhiều `DbContext`** |

Script của module đổi ba tham số cuối, giữ nguyên khuôn:

```bash
dotnet ef migrations script <từ> <tới> \
    --idempotent \
    --project        src/BE/Modules/BanHang/CoreAndSkill.Modules.BanHang.Infrastructure \
    --startup-project src/BE/CoreAndSkill.Api \
    --context        BanHangDbContext \
    --output         database/scripts/modules/banhang/0003__banhang__them-cot-ghi-chu.sql
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §4

### 4.1 Vì sao `--idempotent`

Có cờ này, **mỗi migration** được bọc trong một khối kiểm:

```sql
DO $EF$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM core."__ef_migrations_history"
                   WHERE "migration_id" = '20260908143255_AddNotificationRecipientTable') THEN
        CREATE TABLE core.notification_recipient ( ... );
    END IF;
END $EF$;
```

**Bảo vệ theo `migration_id`, không theo nội dung.** DDL viết tay chèn thêm vào file, **ngoài**
khối `DO`, hoàn toàn không được bảo vệ và sẽ chạy lại mỗi lần. Chèn tay thì phải tự bọc:

```sql
-- Ten bang, ten cot, ten index o day la GIA — chi minh hoa cach boc. Index that khai o schema-core.md.
CREATE INDEX IF NOT EXISTS ix_bang_vi_du_tenant_cot_vi_du
    ON core.bang_vi_du (tenant_id, cot_vi_du)
    WHERE is_deleted = false;
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §4.1

### 4.2 Sau khi sinh — ba việc bắt buộc

1. **Đọc file.** Đối chiếu với bảng dấu hiệu ở [`migration-policy.md`](migration-policy.md) §3.2.
   `DropTable` hoặc `CreateTable` cho bảng đã có ⇒ **dừng**, `--from` sai hoặc snapshot lệch.
2. **Đổi tên file theo §2** nếu `--output` chưa đúng khuôn.
3. **Thêm khối tự chặn** ngay sau `START TRANSACTION;` **và khối ghi `schema_script_history`** ở cuối
   file (§3.1), kèm checksum tính ở §3.2. Checksum tính **sau** khi đã thêm mọi thứ khác.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §4.2

---

## 5. Câu 5 — Làm sao biết DB đã lệch model

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §5

### 5.1 Cơ chế: app TỪ CHỐI KHỞI ĐỘNG khi thiếu migration

Lúc khởi động, với **từng** `DbContext`, so migration mà assembly biết với migration mà database
đã ghi nhận:

| Trạng thái | Nghĩa | Hành động |
| --- | --- | --- |
| Database **thiếu** migration mà app biết | Chưa chạy script | 🛑 **Từ chối khởi động** |
| Database **có thừa** migration mà app không biết | Schema đi trước code — trạng thái **hợp lệ** ở bước "mở rộng" của quy trình thay đổi phá vỡ, [`../wiki-core/be/13-core-data-migration.md`](../wiki-core/be/13-core-data-migration.md) §4 | ⚠️ Ghi log cảnh báo, **vẫn khởi động** |
| Khớp | Bình thường | Khởi động |

Ca thứ hai **cố ý không chặn**, nhưng vẫn phải **ồn ào**.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §5.1

### 5.2 Code mẫu

`Core.Infrastructure` — bộ kiểm:

```csharp
namespace CoreAndSkill.Core.Infrastructure.Persistence;

public sealed record SchemaDriftReport(
    string ContextName,
    IReadOnlyList<string> MissingInDatabase,
    IReadOnlyList<string> UnknownToApplication);

public interface ISchemaVerifier
{
    Task<IReadOnlyList<SchemaDriftReport>> InspectAsync(CancellationToken ct);
}

internal sealed class SchemaVerifier(IEnumerable<DbContext> contexts) : ISchemaVerifier
{
    public async Task<IReadOnlyList<SchemaDriftReport>> InspectAsync(CancellationToken ct)
    {
        var reports = new List<SchemaDriftReport>();

        foreach (var context in contexts)
        {
            var known   = context.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
            var applied = (await context.Database.GetAppliedMigrationsAsync(ct))
                          .ToHashSet(StringComparer.Ordinal);

            reports.Add(new SchemaDriftReport(
                ContextName:          context.GetType().Name,
                MissingInDatabase:    known.Except(applied).Order().ToList(),
                UnknownToApplication: applied.Except(known).Order().ToList()));
        }

        return reports;
    }
}
```

`Core.Web` — phép kiểm chạy bên trong `UseCoreAsync()`, **trước** khi server nhận request:
`RunAsync` của `DatabaseSchemaStartupCheck`
(`../../src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/DatabaseSchemaStartupCheck.cs`), gọi
từ `VerifyDatabaseSchemaAsync`. Code thật là nguồn của thân hàm; file này không chép lại.

Thông điệp lỗi bắt buộc nêu **đúng tên** từng migration còn thiếu, **đường dẫn thư mục script** cần chạy, và **file tài liệu** cần đọc tiếp. Thư mục script là thư mục của chủ sở hữu từng context; code hôm nay ghi cứng `database/scripts/core/`, chỉ đúng khi mọi context là `CoreDbContext`.

**Khoá cấu hình của phép kiểm khởi động — định nghĩa gốc.** Không kết nối được database thì phép
kiểm không chạy được, và *không kiểm được* không bao giờ được coi là *khớp* (§5.3). Phép kiểm thử
lại trong một khoảng có hạn. Khoá nằm dưới tiền tố `Core:SchemaCheck:`, khớp `CoreSchemaCheckOptions`:

| Khoá | Mặc định | Miền | Nghĩa |
| --- | --- | --- | --- |
| `Core:SchemaCheck:ConnectWaitSeconds` | `60` | `0`–`600` | Tổng thời gian chờ database nhận kết nối, tính từ lần thử đầu. `0` = thử đúng một lần |
| `Core:SchemaCheck:ConnectRetryIntervalSeconds` | `5` | `1`–`60` | Khoảng nghỉ giữa hai lần thử. Lần nghỉ cuối bị cắt cho vừa hạn |

- **Giá trị ngoài miền ⇒ app từ chối khởi động**, thông điệp nêu tên khoá.
- **Lỗi kết nối tạm thời** — lỗi mà Npgsql đánh dấu `IsTransient`, ví dụ bị từ chối kết nối,
  database đang khởi động ⇒ thử lại. Kết nối được thì phép kiểm §5.1 chạy như thường. Hết hạn ⇒
  từ chối khởi động, tiến trình dừng.
- **Lỗi không tạm thời** — sai mật khẩu, database không tồn tại, thiếu quyền ⇒ từ chối khởi động
  ngay, không thử lại.
- Vòng thử này là của riêng phép kiểm khởi động. Nó **không** phải chiến lược thử lại của luật E11.

> 🛑 **Trong lúc chờ, tiến trình chưa trả lời endpoint nào — kể cả `/health/live`.** Bộ điều phối
> phải cho giai đoạn khởi động dài hơn `ConnectWaitSeconds` rồi mới áp liveness. Không thì nó giết
> tiến trình giữa lúc chờ — đúng vòng lặp khởi động lại mà
> [`../wiki-core/be/07-observability.md`](../wiki-core/be/07-observability.md) §8 cảnh báo.

Luật E8 ([`../RULES-BE.md`](../RULES-BE.md) §4) canh cơ chế này. Cổng đang chạy và phần chưa có cổng
cùng khai ở ô *Ép bằng gì* của dòng E8 trong file đó.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §5.2

### 5.3 Vì sao im lặng chạy tiếp với DB lệch là cách hỏng TỆ NHẤT

Ba phương án khi phát hiện lệch: tự chạy migration (đã loại ở §0), im lặng chạy tiếp (tệ nhất), **từ chối khởi động** — hỏng **ngay**, hỏng **ồn ào**, hỏng **đúng chỗ**.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §5.3

### 5.4 Lệch chiều ngược — model đổi mà chưa sinh migration

Mục 5.1 bắt ca *"code đi trước database"*. Ca *"model đi trước migration"* — có người sửa
entity mà quên `migrations add` — bắt ở CI, không ở lúc chạy — luật **E10** ([`../RULES.md`](../RULES.md)). Job backend chạy
`dotnet tool restore` trước để có `dotnet ef`, rồi chạy lệnh dưới cho **từng** `DbContext`:

```bash
dotnet ef migrations has-pending-model-changes \
    --project        src/BE/Core/CoreAndSkill.Core.Infrastructure \
    --startup-project src/BE/CoreAndSkill.Api \
    --context        CoreDbContext
```

PASS: *"No changes have been made to the model since the last migration."*

| Lệch | Ai bắt |
| --- | --- |
| Model → migration (quên sinh) | CI, lệnh `has-pending-model-changes` — luật E10 |
| Migration → database (quên chạy script) | Lúc khởi động, §5.1 |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §5.4

### 5.5 Giới hạn thật của cơ chế — đừng coi nó là hàng rào chống mọi kiểu lệch

Phép kiểm §5.1 đọc **bảng lịch sử migration**, không đọc cấu trúc bảng. Vì vậy nó **không** bắt
được hai ca sau:

| Ca không bắt được | Tại sao | Lưới thay thế |
| --- | --- | --- |
| Ai đó sửa cột **trực tiếp** trên database | Lịch sử migration vẫn đầy đủ, app vẫn khởi động | Các câu kiểm ở [`schema-core.md`](schema-core.md) §11, chạy định kỳ |
| Ai đó **sửa nội dung** một script đã áp | Tên file không đổi nên câu đối chiếu ở §3.4 vẫn khớp | `checksum_sha256` ở §3.2 — đây chính là lý do cột đó tồn tại |

> 🛑 **Bảng lịch sử migration là thứ KHÔNG được đụng tay.** Nếu
> app từ chối khởi động, đường đúng là **chạy script**, không phải sửa bảng lịch sử.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §5.5

---

## 6. Môi trường dev — cùng đường với §3.3

**Mỗi dev dùng một database cục bộ** ([`../adr/0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md`](../adr/0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md)). `appsettings.Development.json` đã mang sẵn chuỗi kết nối, thư mục kho tệp và hai mật khẩu bootstrap, khớp tệp `database/dev/tao-db-local.sql` — giá trị dev công khai ([`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §6.1). Clone về, làm năm bước dưới, là BE kết nối được; không phải đặt `user-secrets` nào.

**Database dev dùng chung tạm hoãn, chưa áp dụng.** Luật của nó giữ nguyên cho lúc nhóm dựng lại: [`../adr/0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md`](../adr/0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md), [`../adr/0099-luat-van-hanh-db-dev-chung.md`](../adr/0099-luat-van-hanh-db-dev-chung.md), [`../adr/0104-tai-khoan-ung-dung-cua-dev-tren-db-chung-tao-qua-man-quan-tri.md`](../adr/0104-tai-khoan-ung-dung-cua-dev-tren-db-chung-tao-qua-man-quan-tri.md).

| # | Việc | Nối vào database | Tài khoản — mật khẩu |
| --- | --- | --- | --- |
| 1 | `database/dev/tao-db-local.sql`, **thay cho** §3.3 bước 1 — tạo `coreandskill_owner`, `coreandskill_app` với mật khẩu trùng tên role, rồi database `coreandskill_dev` | `postgres` | `postgres` — mật khẩu đặt lúc cài Postgres trên máy đó; container dưới: `dev` |
| 2 | Câu kiểm *đang ở đâu, là ai* rồi script schema — §3.3 bước 2. Câu kiểm phải ra `coreandskill_dev`, `coreandskill_owner` | `coreandskill_dev` | `coreandskill_owner` — trùng tên role |
| 3 | Câu kiểm như bước 2, rồi khối cấp quyền ở §3.6 và câu nghiệm thu của nó | `coreandskill_dev` | `coreandskill_owner` — trùng tên role |
| 4 | Tạo thư mục kho tệp `C:\CoreAndSkillData\files` — đường dẫn ở `Core:File:RootPath` của `appsettings.Development.json`. Core **không** tự tạo; thiếu thì API không khởi động ([`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §2) | — | Người dùng máy |
| 5 | Lệnh bootstrap — §3.3 bước 5 | `coreandskill_dev` | `coreandskill_app` — ứng dụng tự nối bằng chuỗi kết nối của `appsettings.Development.json`, không gõ |

Bảng dùng chung cho `psql` và công cụ đồ hoạ; mọi kết nối tới `localhost`, cổng `5432` — cổng trong chuỗi kết nối đó. Ở DBeaver hay pgAdmin, bước 1 và bước 2–3 là **hai kết nối riêng**: không chạy bước 2 trên kết nối vừa chạy bước 1.

Bước 2 trên database trống chạy bằng `coreandskill_owner` như bảng ghi: vòng có `0001`–`0007`. Database dev đã áp, chỉ còn thiếu script mang khối tự chặn — đường §3.4 — thì chạy được bằng `postgres` (§3.1 *Tài khoản chạy script*), trên một kết nối tới `coreandskill_dev`: kết nối tới database `postgres` thì khối dừng với `sai database` — trừ khi database đó đã lỡ nhận script, đoạn *Lỡ chạy nhầm* dưới.

Bước 2, 3, 5 chạy **đúng lệnh và đúng bảng nghiệm thu** của bản cài thật. Không có tệp `.sql` dữ liệu riêng cho dev: đơn vị và tài khoản vẫn do lệnh bootstrap tạo. BE chạy bằng `coreandskill_app`, không bằng `postgres` — không mã nào của BE từ chối chạy bằng siêu quản trị.

| Chỗ | Máy dev |
| --- | --- |
| Postgres | Container dưới đây, hoặc Postgres đã cài sẵn. **Không** dùng `docker-compose.yml` ở gốc repo: tệp đó là bản cài thật (§3.3, *Bản cài Compose*) — không mở cổng Postgres ra máy, cần tên miền và chứng chỉ cho Caddy; máy dev chỉ mượn phiên bản Postgres của nó |
| Biến của §3.3 | `PGHOST=localhost`, `PGADMIN=postgres`, `PGDATABASE=coreandskill_dev` |
| Chạy bằng `psql` | `psql --version` phải in ra phiên bản. Khối lệnh của tệp này viết cho bash — PowerShell, cmd không chạy nguyên văn; trên Windows dùng Git Bash. Windows không thấy `psql` dù đã cài Postgres, hoặc dùng công cụ đồ hoạ: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §6 |
| Máy dev không chạy Windows | Đè `Core:File:RootPath` bằng `user-secrets` — đường dẫn `C:/…` ở đó không phải đường dẫn tuyệt đối |
| Chữ tiếng Việt ở console Windows | Lệnh `core …` và log của BE in tiếng Việt; console Windows mặc định dùng code page OEM nên chữ có dấu thành ký tự lạ hay `?`, ở cả Windows Terminal. BE tự chuyển console sang UTF-8 khi chạy trên Windows và đầu ra **không** bị chuyển hướng. Đầu ra đi qua ống (`\|`) hay vào tệp (`>`) thì BE không đổi gì — chạy `[Console]::OutputEncoding = [Text.Encoding]::UTF8` trong cửa sổ PowerShell đó trước lệnh; `cmd` dùng `chcp 65001`. Vẫn ra ô vuông thì phông của console thiếu chữ Việt — đổi sang Consolas hay Cascadia Mono |

```bash
# Container Postgres (dung Postgres da cai san thi bo qua dong nay). Phien ban theo
# docker-compose.yml o goc repo — nguon phien ban PostgreSQL cua repo. Cong chi mo cho
# chinh may (127.0.0.1): mat khau cua database nay cong khai tren GitHub.
docker run -d --name coreandskill-db -e POSTGRES_PASSWORD=dev -p 127.0.0.1:5432:5432 postgres:18

export PGHOST=localhost PGADMIN=postgres PGDATABASE=coreandskill_dev

# Buoc 1 cua may dev, thay cho §3.3 buoc 1.
psql -h "$PGHOST" -U "$PGADMIN" -d postgres -v ON_ERROR_STOP=1 -f database/dev/tao-db-local.sql

# Roi §3.3 buoc 2, buoc 3 (khoi cap quyen §3.6), tao thu muc kho tep, roi §3.3 buoc 5.
```

> ⚠️ **Chưa ai chạy trọn năm bước này trên một máy sạch.** Hai điểm chưa kiểm: `psql` với BOM của script schema (§3.3 bước 2); chạy `tao-db-local.sql` trong công cụ đồ hoạ thay cho `psql`. `CREATE DATABASE` không chạy được trong một khối transaction — công cụ gửi cả tệp đi như một lệnh thì có thể gặp lỗi đó; khi đó chạy riêng câu cuối của tệp.

**Gộp năm bước thành một lệnh** bằng một script bọc được. Script đó chỉ gọi đúng các lệnh trên, và không mang giá trị nào ngoài giá trị dev công khai đã có trong hai tệp kia.

**Không chạy `tao-db-local.sql` trên máy chủ dùng chung hay bản cài thật.** Mật khẩu trong đó công khai trên GitHub. Postgres của máy dev mà nhận kết nối có mật khẩu từ mạng thì ai trong mạng đó cũng vào được database này — nên container ở trên chỉ mở cổng trên `127.0.0.1`, và Postgres cài sẵn giữ `listen_addresses = 'localhost'`.

**Dựng lại từ đầu khi lỡ tay:** `dropdb -h "$PGHOST" -U "$PGADMIN" "$PGDATABASE"`, rồi làm lại từ bước 1 — tệp đặt lại mật khẩu cho role đã có và tạo lại database. Chạy tệp khi database còn đó thì câu cuối báo *"already exists"*; bỏ qua lỗi đó.

**Lỡ chạy nhầm vào database `postgres` — chỉ dùng trên máy dev.** Dấu hiệu: ở database `postgres`, `SELECT nspname FROM pg_namespace WHERE nspname = 'core'` ra một dòng. Dọn bằng `postgres`, nối vào database `postgres`:

```sql
DO $$
BEGIN
  IF current_database() <> 'postgres' THEN
    RAISE EXCEPTION 'Dang o database %, khong phai postgres - DUNG', current_database();
  END IF;
  DROP SCHEMA IF EXISTS core CASCADE;
END $$;
```

Vòng module cũng chạy nhầm thì thêm `DROP SCHEMA IF EXISTS <schema của module> CASCADE;` vào khối. Đã chạy tới Core `0009` thì extension `unaccent` nằm ở schema `public` của database `postgres`, ngoài schema `core`: thêm `DROP EXTENSION IF EXISTS unaccent;` — trừ khi database đó đã có extension này từ trước. Rồi làm lại từ bước 2 trên đúng kết nối. Nhầm tài khoản trên đúng database (câu kiểm chủ bảng ở §3.3): dựng lại từ đầu.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §6

---

## 7. Checklist trước khi chạy script trên PRODUCTION

Chạy theo thứ tự. **Không bỏ bước nào**, kể cả với script "chỉ thêm một index".

### 7.1 Trước — chuẩn bị

| ☐ | Việc | PASS |
| --- | --- | --- |
| ☐ | **Backup database**, và **kiểm tra khôi phục được** trên một database khác | Khôi phục xong, đếm được số dòng của một bảng lớn |
| ☐ | Đọc **toàn bộ** script sắp chạy, từ dòng đầu tới dòng cuối | Không có `DROP`/`TRUNCATE`/`DELETE` ngoài dự kiến |
| ☐ | Chạy script trên **bản sao production** trước | Không lỗi; đo được thời gian chạy |
| ☐ | Xác nhận đây là bước nào trong quy trình thay đổi phá vỡ ở [`../wiki-core/be/13-core-data-migration.md`](../wiki-core/be/13-core-data-migration.md) §4 | Điều kiện ở cột *Sang bước kế khi* của bước liền trước đã đạt |
| ☐ | Đối chiếu script còn thiếu bằng lệnh ở §3.4 | Danh sách khớp với thứ định chạy |
| ☐ | Ước lượng thời gian khoá bảng | Có `ALTER COLUMN … TYPE` hoặc `SET NOT NULL` trên bảng lớn ⇒ cần cửa sổ bảo trì |
| ☐ | Xác nhận connection string trỏ đúng database, đúng tài khoản | Câu kiểm *đang ở đâu, là ai* ở §3.3 bước 2 ra database production, tài khoản được phép chạy mọi file sắp áp theo §3.1 *Tài khoản chạy script*, địa chỉ máy chủ production |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §7

### 7.2 Trong khi chạy

| ☐ | Việc |
| --- | --- |
| ☐ | Chạy **từng file một**, `-v ON_ERROR_STOP=1`, đọc kết quả từng file trước khi sang file kế |
| ☐ | Lưu toàn bộ output ra file: `psql … -f script.sql 2>&1 \| tee logs/apply-$(date +%F-%H%M).log` |
| ☐ | Có lỗi ⇒ **DỪNG**. Không chạy file tiếp theo, không "thử lại xem sao" |

### 7.3 Sau — nghiệm thu

| ☐ | Việc | PASS |
| --- | --- | --- |
| ☐ | `SELECT script_name, applied_at FROM core.schema_script_history ORDER BY id DESC LIMIT 5` | Có dòng của script vừa chạy |
| ☐ | Chạy lại khối cấp quyền ở §3.6, rồi câu nghiệm thu của nó (luật M13) | 0 dòng |
| ☐ | Các câu kiểm ở [`schema-core.md`](schema-core.md) §11 | Đúng "mong đợi" của từng câu |
| ☐ | Khởi động lại app | Lên được — tức phép kiểm §5.1 đã qua |
| ☐ | Gọi thử một endpoint chạm bảng vừa đổi | Trả đúng, không 500 |
| ☐ | Đọc log 15 phút đầu | Không có cảnh báo *"Database di TRUOC code"* ngoài dự kiến |

### 7.4 Ba thứ TUYỆT ĐỐI không làm trên production

| Không làm | Vì sao |
| --- | --- |
| Sửa script cho khớp lỗi rồi chạy tiếp | Script trong repo và thứ đã chạy trên DB sẽ khác nhau, và checksum ở §3.2 sẽ tố cáo điều đó sau — nhưng lúc đó đã muộn |
| Chạy `dotnet ef database update` | Bị chặn bằng máy, và đi ngược §0. Nó cũng bỏ qua bước ghi `schema_script_history` |
| Chạy một script từ nhánh chưa merge | Nó có thể bị đổi số thứ tự lúc merge (§2.1), và khi đó database mang một tên script không tồn tại trong repo |

---

## 8. Khôi phục mật khẩu tài khoản vận hành hệ thống

Tài khoản vận hành (`superadmin`, mang `is_system_operator`) quên mật khẩu thì **không ai trong ứng
dụng đặt lại hộ được** ([`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)).
Đường duy nhất là **một lệnh chạy tay trên máy chủ** — lệnh con của cùng runner với lệnh bootstrap
([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)).

| # | Việc |
| --- | --- |
| 1 | Đặt mật khẩu mới vào nguồn giá trị như §3.3 bước 4 — máy dev: `user-secrets`; bản cài thật: nguồn bí mật của môi trường. Tên khoá: cùng chỗ khai với §3.3 bước 4. 🛑 Máy dev mà chuỗi kết nối trỏ database trên máy khác thì **không được bỏ bước này**: bỏ nó thì lệnh vẫn chạy, và tài khoản vận hành trên database đó nhận mật khẩu dev công khai — đọc được trên GitHub |
| 2 | Chạy lệnh dưới, một lần |

```bash
dotnet run --project src/BE/CoreAndSkill.Api -- core reset-operator-password
```

| Tính chất | Vì sao |
| --- | --- |
| **Khoá tên đăng nhập hay khoá mật khẩu không có giá trị, hoặc một lệnh ghi của Identity hỏng ⇒ dừng, thoát mã 1, không ghi gì** | Cùng lý do với lệnh bootstrap (§3.3 bước 5). Lệnh ghi hỏng thì lệnh dừng ngay ở đó, không ghi nhật ký — [`../adr/0096-lenh-ghi-usermanager-kiem-ket-qua-hong-thi-dung.md`](../adr/0096-lenh-ghi-usermanager-kiem-ket-qua-hong-thi-dung.md) |
| **Máy dev: vế *khoá không có giá trị* ở dòng trên không bắn với lệnh `dotnet run`.** Quên bước 1 thì lệnh đặt mật khẩu dev công khai và thoát mã 0 | Lệnh chỉ kiểm hai khoá có giá trị, không kiểm mật khẩu có mới — `src/BE/Core/CoreAndSkill.Core.Web/Commands/CoreCommandRunner.cs`, hàm `RunResetOperatorPasswordAsync`, chuỗi `string.IsNullOrWhiteSpace(options.OperatorPassword)`. `dotnet run` chạy ở môi trường Development — `src/BE/CoreAndSkill.Api/Properties/launchSettings.json`, chuỗi `"ASPNETCORE_ENVIRONMENT": "Development"` — và môi trường đó nạp `appsettings.Development.json`, tệp mang giá trị dev công khai cho khoá này ([`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §6.1) |
| **Mật khẩu đi qua `UserManager`, không qua SQL** | Không hàm SQL nào sinh được chuỗi băm hợp lệ ([`schema-core.md`](schema-core.md) §4.1). Mật khẩu mới phải đạt chính sách mật khẩu như ở mọi môi trường (luật S9) |
| **Chạy bằng `coreandskill_app`** | Cùng chuỗi kết nối với ứng dụng — bảng quyền ở §3.6 |

Hành vi còn lại của lệnh — phiên đang mở, nhật ký kiểm toán, cờ đổi mật khẩu — theo ADR-0023.

**Nghiệm thu:**

| Kiểm | Mong đợi |
| --- | --- |
| Đăng nhập tài khoản vận hành bằng mật khẩu mới, qua liên kết *Quản trị hệ thống*, gõ mã đơn vị hệ thống | Thành công, và `mustChangePassword` là `true` |
| Đăng nhập bằng mật khẩu cũ | Trượt |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §8

---

## 9. Seed lại dữ liệu mặc định cho mọi đơn vị — khi lắp module mới

Lắp một module mới vào bản cài **đã có đơn vị**: script schema của module (§3.4) mang khoá quyền,
nhưng vai trò, ánh xạ quyền và menu của module cho từng đơn vị do nguồn seed ghi — chỉ chạy lúc tạo
đơn vị. Lệnh dưới chạy lại seed cho **mọi** đơn vị nghiệp vụ; định nghĩa gốc ở
[`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3, bảng *Lệnh của runner*.

Khoá cấu hình: **không có**. Chạy sau §3.4 và sau khối cấp quyền §3.6.

```bash
dotnet run --project src/BE/CoreAndSkill.Api -- core seed-tenant-defaults
```

| Tính chất | Vì sao |
| --- | --- |
| **Idempotent** — vai trò, ánh xạ, menu đã có thì bỏ qua | Cùng lý do với lệnh bootstrap (§3.3 bước 5): chạy lại là phản xạ đúng |
| **Mỗi đơn vị một transaction** | Một đơn vị hỏng không kéo đơn vị khác về nửa chừng; chạy lại thì đơn vị đã xong bị bỏ qua |
| **Chạy bằng `coreandskill_app`** | Cùng chuỗi kết nối với ứng dụng — bảng quyền ở §3.6 |

Hai dòng đầu bảng là luật **E22** ([`../RULES-BE.md`](../RULES-BE.md) §4), áp từ lượt dựng lệnh này — lệnh chưa có code không làm chúng thành đích đến.

**Nghiệm thu:**

| Kiểm | Mong đợi |
| --- | --- |
| Đăng nhập một đơn vị đã có từ trước | Menu của module mới xuất hiện với tài khoản có quyền |
| Chạy lại lệnh, rồi `SELECT count(*) FROM core.app_role` | Không đổi so với trước lần chạy lại |

---

## 10. Phát lại bản ghi outbox chết

Bản ghi `core.outbox_message` chạm ngưỡng thử lại mang `status = 'dead'` và bộ phát không tự chạm nữa
([`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §2.5). Sau khi sửa nguyên
nhân (đọc `last_error`), phát lại bằng lệnh dưới; định nghĩa gốc ở
[`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3, bảng *Lệnh của runner*.

Khoá cấu hình: **không có**. Chọn đối tượng bằng tham số sau động từ.

```bash
# Một dòng
dotnet run --project src/BE/CoreAndSkill.Api -- core outbox-replay --id <id>

# Mọi dòng đang dead
dotnet run --project src/BE/CoreAndSkill.Api -- core outbox-replay --all-dead
```

| Tính chất | Vì sao |
| --- | --- |
| **Chỉ chạm dòng `dead`** — `--id` trỏ dòng ở trạng thái khác ⇒ dừng, không ghi gì | Phát lại một dòng `pending` hay `done` là phát trùng |
| **Đặt `status = 'pending'`, `attempt_count = 0`**; bộ phát nhặt ở nhịp quét kế | Không gọi bên nhận ngay trong lệnh — cùng đường phát với mọi dòng khác |
| **Mỗi dòng phát lại một dòng nhật ký kiểm toán** | Phát lại là thao tác vận hành có hậu quả nghiệp vụ; danh sách việc phải ghi ở [`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5.4 |
| **Chạy bằng `coreandskill_app`** | Cùng chuỗi kết nối với ứng dụng — bảng quyền ở §3.6 |

Ba dòng đầu bảng là luật **E23** ([`../RULES-BE.md`](../RULES-BE.md) §4).

**Nghiệm thu:**

| Kiểm | Mong đợi |
| --- | --- |
| `SELECT count(*) FROM core.outbox_message WHERE status = 'dead'` sau `--all-dead` | `0` |
| `/health/ready` sau một nhịp quét | Không còn `Degraded` vì bản ghi `dead` |

---

## 11. Bù dữ liệu cho bản cài đã có — thư mục `bu-du-lieu/`

Service tạo đơn vị chỉ seed lúc tạo (§1), nên bản cài bootstrap **trước** một lượt thêm mục seed không có mục đó.
Tệp ở `database/bu-du-lieu/` bù đúng chỗ ấy. Khác script schema ở bốn điểm:

| | Script schema — `scripts/` | Bù dữ liệu — `bu-du-lieu/` |
| --- | --- | --- |
| Ai nhặt | Vòng lặp §3.3, §3.4 | Không vòng nào — người dùng chạy tay từng tệp; agent chỉ viết |
| Bản cài mới | Chạy mọi tệp | **Không** chạy: lệnh bootstrap đã seed |
| `core.schema_script_history` | Ghi một dòng | **Không** ghi — "đã chạy chưa" đọc ở chính dòng dữ liệu, bằng câu nghiệm thu |
| Chạy lại | Khối lịch sử `ON CONFLICT DO NOTHING` | Tệp tự thấy dòng đã có, không ghi gì |

Tính chất bắt buộc của tệp: [ADR-0122](../adr/0122-don-vi-he-thong-duoc-seed-mot-muc-menu-toi-he-thong-don-vi-moi-tai-khoan-cua-no-mang-co-van-hanh.md) điểm 5.

### 11.1 `0001__core__he-thong-don-vi-menu.sql` — mục *Đơn vị* của đơn vị hệ thống

Thêm mục `he-thong-don-vi` ([`../contracts/meta-menu.md`](../contracts/meta-menu.md) §1.7) cho đơn vị hệ thống đã có.
Chạy sau khi code của lượt ADR-0122 đã vào, và chỉ khi câu nghiệm thu M17 ở §3.3 ra không dòng nào.

| Chỗ | Cách |
| --- | --- |
| Tài khoản | Tệp mang khối tự chặn (§3.1), nên `postgres` hay `coreandskill_owner` đều được — hàng đầu bảng *Tài khoản chạy script*. `coreandskill_app` thì tệp dừng với `khong dong duoc vai` |
| Nối vào | Database ứng dụng; câu kiểm *đang ở đâu, là ai* (§3.3 bước 2) trước. Nối nhầm database thì tệp dừng với `sai database` |
| DBeaver, pgAdmin | Mở tệp trên kết nối tới database ứng dụng, bấm **Execute script** (`Alt+X` ở DBeaver) — tệp có nhiều câu, `Ctrl+Enter` chỉ chạy một |
| BOM | Tệp mang BOM UTF-8 như script schema — cảnh báo BOM ở §3.3 bước 2 áp nguyên |
| Bản cài Compose | `docker-compose.yml` chỉ gắn `./database/scripts` vào `db`, nên `-f` trong container không thấy tệp: đưa qua stdin của `docker compose exec -T db psql …`. Chưa ai chạy |

```bash
psql -h "$PGHOST" -U postgres -d "$PGDATABASE" -v ON_ERROR_STOP=1 \
     -f database/bu-du-lieu/0001__core__he-thong-don-vi-menu.sql
```

| Ca | Tệp báo | Ghi |
| --- | --- | --- |
| Đơn vị hệ thống chưa có mục | `NOTICE` — `Da them muc he-thong-don-vi cho don vi he thong` | Một dòng `core.menu_item` |
| Chạy lại | `NOTICE` — `Don vi he thong da co muc he-thong-don-vi - khong ghi gi` | Không gì |
| Chưa bootstrap | Lỗi — `Ban cai chua bootstrap; lenh bootstrap se seed muc nay - DUNG, khong ghi gi` | Không gì — chạy lệnh bootstrap (§3.3 bước 5) thay cho tệp này |
| Cụm thiếu `uuidv7()` | Lỗi — `… khong co ham uuidv7() (can PostgreSQL 18) - DUNG, khong ghi gi` | Không gì — phiên bản ghim ở `docker-compose.yml`, chuỗi `image: postgres:18` |

DBeaver hiện `NOTICE` ở khung output của trình soạn SQL — chưa ai kiểm trên máy này; không thấy thông báo thì đọc kết quả
bằng câu nghiệm thu.

**Nghiệm thu:** câu M17 ở §3.3 ra đúng một dòng `true`, `he-thong-don-vi`. Rồi đăng nhập **lại** bằng tài khoản vận hành —
menu chỉ tải một lần sau đăng nhập — và thấy mục *Đơn vị* cuối `Sidebar`. Gỡ mục: ADR-0122, *Rút lui nếu sai* bước 2.
