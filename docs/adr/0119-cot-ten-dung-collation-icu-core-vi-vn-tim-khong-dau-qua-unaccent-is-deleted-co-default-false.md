---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0119 — Cột tên tiếng Việt dùng collation ICU `core.vi_vn` do migration Core tạo; `searchText` so khớp không phân biệt dấu qua `unaccent`; `is_deleted` mang `DEFAULT false`

> **Trạng thái:** Đã chấp nhận (2026-10-06)

## Bối cảnh

Đợt rà đồng bộ ngày 2026-10-06 mở hai finding về schema `core`: BE-18 (collation) và DB-06 (`DEFAULT` lệch tài liệu). Người dùng duyệt toàn bộ đề xuất của đợt rà cùng ngày. ADR này ghi hai câu — Q18 và Q25 của đợt rà. Chúng đi chung vì cùng đổi schema `core` trước khi có dữ liệu thật, và cùng đến database qua một đợt migration và script.

### Collation — lời hứa chưa giữ

[`16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §6.3 viết: sắp xếp chuỗi tiếng Việt phải theo quy tắc đối chiếu của ngôn ngữ, khai ở tầng DB, và phải chốt **trước** khi có dữ liệu thật. Hàng tương ứng ở §7 cùng tệp ghi *"✅ sẽ có"*. Đọc ngày 2026-10-06:

| Chỗ | Đọc được |
| --- | --- |
| Bảy script ở `database/scripts/core/` | Không câu `COLLATE` nào |
| Tạo database | Runbook §3.3 bước 1 dùng `createdb -E UTF8 -T template0`; `database/dev/tao-db-local.sql` dùng `ENCODING 'UTF8' TEMPLATE template0`. Không cờ locale nào, nên database kế thừa locale của cụm |
| Locale của cụm | Image `postgres:18` (compose, test tích hợp — `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Support/PostgresFixture.cs`, chuỗi `"postgres:18"`) mặc định `en_US.utf8`. Cụm cài bằng bộ cài trên Windows lấy locale của hệ điều hành. **Chưa đo trên máy dev nào** |
| Sắp xếp theo tên | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Users/UserQueryService.cs`, chuỗi `users.OrderBy(u => u.FullName)`; `Tenants/TenantAdminQueryService.cs`, chuỗi `tenants.OrderBy(t => t.Name)`; danh sách vai trò cho `sortBy=name` — `src/BE/Core/CoreAndSkill.Core.Application/Roles/GetRolesListQueryValidator.cs`, chuỗi `["name", "createdAt"]` |
| Tìm theo `searchText` | Ba dịch vụ truy vấn trên dùng `EF.Functions.ILike` — không phân biệt hoa thường, **có** phân biệt dấu: gõ `nguyen` không ra `Nguyễn` |

Bảng chữ cái tiếng Việt coi `ă`, `â`, `đ`, `ê`, `ô`, `ơ`, `ư` là chữ cái riêng, đứng sau chữ gốc. Collation libc của locale tiếng Anh không biết điều đó, và kết quả còn khác nhau giữa máy dev và bản cài vì locale của cụm khác nhau.

### `DEFAULT` lệch tài liệu

| Cột | Tài liệu ([`schema-core.md`](../database/schema-core.md)) | DDL thật |
| --- | --- | --- |
| `is_deleted` của mọi bảng kế thừa `BaseEntity` | §3.2: `NOT NULL DEFAULT false` | `is_deleted boolean NOT NULL` — không `DEFAULT`, ví dụ `database/scripts/core/0001__core__initial.sql` và `0007__core__add-notification.sql` |
| Năm cột Identity của `core.app_user`: `email_confirmed`, `phone_number_confirmed`, `two_factor_enabled`, `lockout_enabled`, `access_failed_count` | §4.1: mặc định `false`, `false`, `false`, `true`, `0` | `NOT NULL`, không `DEFAULT` — `0001__core__initial.sql`, chuỗi `access_failed_count integer NOT NULL,` |

EF luôn ghi đủ cột, nên ứng dụng không hỏng. Câu `INSERT` viết tay mà bỏ `is_deleted` thì hỏng với `23502`. Module sẽ seed dữ liệu bằng SQL viết tay. Năm cột Identity thì khác: không có cách nào tạo một tài khoản hợp lệ bằng SQL ([`schema-core.md`](../database/schema-core.md) §4.1, cảnh báo về mật khẩu), nên không câu `INSERT` tay nào cần chúng.

### Chưa có dữ liệu thật

Đầu [`schema-core.md`](../database/schema-core.md) ghi chưa DDL nào từng được áp lên một Postgres thật ngoài máy dev. Đổi collation trên bảng đã có dữ liệu và index là việc tốn hơn nhiều so với lúc này.

## Quyết định

Người dùng chốt ngày 2026-10-06, theo đề xuất của kiến trúc sư:

1. **Một migration của Core tạo collation `core.vi_vn`** — `provider = icu`, `locale = 'vi-VN'`, deterministic. Core tự tạo, không dựa vào tên collation do `initdb` nạp sẵn.
2. **Ba cột mang collation `core.vi_vn`:** `core.app_user.full_name`, `core.tenant.name`, `core.app_role.name`. Luật từ nay: **cột chữ do người dùng nhập bằng tiếng Việt mà API cho sắp xếp theo thì mang `core.vi_vn`** — áp cho Core và cho module. Luật E25 ở [`RULES-BE.md`](../RULES-BE.md).
3. **Cùng migration đó cài extension `unaccent` vào schema `public`.** `searchText` của danh sách so khớp `unaccent(cột) ILIKE unaccent(mẫu)` trên mọi cột nó khớp: không phân biệt hoa thường **và** không phân biệt dấu, `đ` khớp `d`. Lời gọi không ghi schema, nên extension nằm trong `public`, có sẵn trong `search_path` mặc định.
4. **`is_deleted` của mọi bảng kế thừa `BaseEntity` mang `DEFAULT false`**, khai một chỗ trong cấu hình EF, kèm migration và script. Năm cột Identity giữ không `DEFAULT`; tài liệu sửa cho khớp DDL.
5. **Giao bằng hai migration** — một việc mỗi migration ([`migration-policy.md`](../database/migration-policy.md) §3.1) — và hai script mới theo [`script-runbook.md`](../database/script-runbook.md) §4, mang khối tự chặn của script mới ([`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md), 2026-10-06). Người dùng áp script.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Hoãn, ghi *"❌ chưa"* kèm điều kiện *"trước bản cài đầu"*

**Được:** không việc gì hôm nay. **Mất:** lời hứa ở 16 §6.3 trôi thêm; mỗi database dựng thêm là một chỗ phải sửa sau. **Vì sao loại:** người dùng chọn chốt ngay; chi phí đổi tăng theo dữ liệu.

### Phương án B — Locale ICU cấp database (`createdb --locale-provider=icu --icu-locale=vi-VN`)

**Được:** một cờ, phủ mọi cột chữ; không cần khai cột nào. **Mất:** chỉ đặt được lúc tạo database, nên không đi qua kênh script — mọi database đã dựng phải dựng lại; áp lên cả cột mã, email, tên đăng nhập; Core không biết database nào đã đặt đúng nếu không thêm một phép kiểm lúc khởi động. **Vì sao loại:** quyết định nằm ở bước tạo database của từng môi trường thay vì ở migration mà Core sở hữu.

### Phương án C — Dùng collation `"vi-VN-x-icu"` do `initdb` nạp sẵn

**Được:** không tạo đối tượng mới. **Mất:** tên đó chỉ có khi cụm build với ICU **và** `initdb` nạp collation hệ thống cho locale đó; migration phụ thuộc một bước Core không kiểm soát. **Vì sao loại:** collation do Core tạo nằm trong schema `core`, đi cùng migration, tên do Core giữ. Cả hai phương án đều cần cụm build với ICU.

### Phương án D — Cột chuẩn hoá lưu sẵn cho tìm kiếm (generated column bỏ dấu, có index)

**Được:** tìm kiếm dùng được index. **Mất:** `unaccent` không `IMMUTABLE`, nên phải bọc hàm; thêm một cột cho mỗi cột tìm. **Vì sao loại:** quy mô v1 chưa cần index — mở lại khi đo thấy tìm kiếm chậm.

### Phương án E — `is_deleted`: chỉ sửa tài liệu, ghi *"ứng dụng luôn ghi"*

**Được:** rẻ, không migration. **Mất:** mỗi câu `INSERT` tay của module phải nhớ ghi `is_deleted`; quên thì script dừng với `23502` lúc áp. **Vì sao loại:** module sẽ seed bằng SQL tay. Với năm cột Identity thì đây là phương án đúng, và đã được chọn — không câu `INSERT` tay nào tạo được tài khoản.

## Hệ quả

### Tích cực

- Danh sách sắp xếp theo tên đúng bảng chữ cái tiếng Việt, và giống nhau ở mọi máy — không còn phụ thuộc locale của cụm.
- Tìm `nguyen` ra `Nguyễn`, tìm `do` ra `Đỗ`.
- Câu `INSERT` tay không cần nhớ `is_deleted`.
- Tài liệu năm cột Identity khớp DDL.

### Tiêu cực — cái giá thật

- **Mọi cụm Postgres phải build với ICU và có gói contrib.** Image chính thức và bộ cài trên Windows có cả hai; Postgres tự build thiếu một trong hai thì migration hỏng. Chưa kiểm trên máy dev nào.
- **Nâng phiên bản ICU có thể đổi thứ tự sắp xếp.** Đổi image Postgres hay nâng hệ điều hành của cụm làm Postgres báo lệch phiên bản collation; người vận hành dựng lại index dùng `core.vi_vn`, rồi chạy `ALTER COLLATION core.vi_vn REFRESH VERSION`. Hôm nay không index nào dùng nó, nhưng index thêm sau này sẽ dùng.
- **Tìm kiếm qua `unaccent` không dùng index** — quét cả bảng mỗi lần tìm. Chấp nhận ở quy mô v1.
- **Nghĩa của `searchText` đổi** — thêm *"không phân biệt dấu"*. Ba card hợp đồng `users`, `roles`, `tenants` phải sửa; FE không đổi.
- **Core mang một giả định ngôn ngữ cố định.** Dự án ngôn ngữ khác phải tự thêm migration đổi collation của ba cột.
- **Lần đầu migration Core tạo đối tượng ngoài schema `core`** — extension nằm ở `public`.
- **So sánh bằng (`=`) vẫn phân biệt hoa thường và dấu**, vì collation deterministic. Đúng ý, nhưng người đọc *"collation tiếng Việt"* dễ tưởng `=` cũng bỏ qua dấu.
- **Với `DEFAULT false`, EF không gửi cột khi giá trị là `false`** — database điền thay. Kết quả như cũ, nhưng câu SQL sinh ra khác trước.

## Sáu câu hỏi bắt buộc

| # | Câu hỏi | Trả lời | Chắc chắn? |
| --- | --- | --- | --- |
| 1 | Vấn đề thật | Collation: chưa đo — không có dữ liệu thật, chưa ai chạy câu sắp xếp trên máy nào; sai thứ tự là hành vi đã biết của collation không phải tiếng Việt. Làm ngay vì chi phí đổi tăng theo dữ liệu. `DEFAULT`: lệch giữa tài liệu và DDL đọc được trên tệp | Collation: **không** — dự đoán có căn cứ, chưa quan sát. `DEFAULT`: có |
| 2 | Phương án đơn giản hơn | Hoãn (A); locale cấp database (B); chỉ sửa tài liệu cho `is_deleted` (E) | Có |
| 3 | Chi phí vận hành thêm | Dựng lại index và `REFRESH VERSION` sau mỗi lần nâng ICU; yêu cầu ICU và contrib ở mọi cụm; tìm kiếm quét cả bảng | Tần suất nâng ICU chưa biết |
| 4 | Ai bảo trì | Migration, script, truy vấn: `backend-expert`. Luật E25, `schema-core.md`, card hợp đồng: `architect`. Áp script, `REFRESH VERSION`: người vận hành — ở máy dev là người dùng | Có |
| 5 | Rút lui thế nào | Một migration mới: đưa ba cột về collation mặc định (`ALTER COLUMN … TYPE varchar(n) COLLATE "default"`), `DROP COLLATION core.vi_vn`, gỡ `unaccent` khỏi truy vấn rồi `DROP EXTENSION unaccent`, `ALTER COLUMN is_deleted DROP DEFAULT`; một script mới áp theo runbook §3.4. Chỉ đổi metadata và dựng lại index trên ba cột (hôm nay không có) — dữ liệu đã lưu không đổi | Có |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Ba cột là cột của Core; collation là ngôn ngữ của sản phẩm, không phải nghiệp vụ. Module chỉ dùng lại collation | Có |

## Dấu hiệu quyết định này bắt đầu sai

- Một bản cài, hay một máy dev, không áp được migration vì thiếu ICU hay `unaccent`.
- Người dùng báo thứ tự tên sai dù cột đã mang `core.vi_vn` — locale `vi-VN` của ICU không khớp kỳ vọng nghiệp vụ.
- Tìm kiếm danh sách chậm thấy được — xét phương án D, cùng index trigram.
- Một module thêm cột tên mà quên collation, lần thứ hai — xét dựng cổng cho E25.
- Một dự án ngôn ngữ khác lấy Core — xét đưa tên locale ra cấu hình của dự án.

## Liên quan

- [`16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §6.3 — lời hứa mà ADR này giữ.
- [`schema-core.md`](../database/schema-core.md) §3.2, §3.9, §4.1, §4.2, §1.3 — nơi ghi cột, collation, `DEFAULT`.
- [`migration-policy.md`](../database/migration-policy.md) §3.1, §8 — một việc mỗi migration; danh mục việc khi đổi schema.
- [`script-runbook.md`](../database/script-runbook.md) §3.4, §4 — sinh và áp script trên database đang có dữ liệu.
- Luật E25 ở [`RULES-BE.md`](../RULES-BE.md).
- [ADR-0009](0009-ap-schema-chay-tay.md) — app không tự áp schema.
