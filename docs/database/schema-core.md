---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Schema `core` — bảng, cột, index, ràng buộc

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo đang ở giai đoạn 1, chưa có `src/`. Toàn bộ file này
> mô tả schema mà `src/` phải dựng ra, **không** mô tả một database đang chạy. Không có DDL nào
> dưới đây đã từng được áp lên một Postgres thật.
>
> **File chủ về schema `core`.** Ai sở hữu migration nào: [`migration-policy.md`](migration-policy.md).
> Chạy script lên database thế nào: [`script-runbook.md`](script-runbook.md).

---

## 1. Phạm vi và ranh giới

Một database PostgreSQL, nhiều schema:

| Schema | Ai sở hữu | Chứa gì |
| --- | --- | --- |
| `core` | `Core.Infrastructure` | Người dùng, vai trò, quyền, menu, thông báo, outbox, lịch sử script |
| `<tên module>` | `Modules.<X>.Infrastructure` | Bảng nghiệp vụ của đúng module đó |
| `public` | — | **Rỗng.** Không object nào của ứng dụng nằm ở đây |

Luật ranh giới ở tầng code: [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §5.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §1

### 1.1 Cấm foreign key vật lý xuyên schema

> **Bảng module tham chiếu bảng `core` bằng cột id trần — KHÔNG khai `FOREIGN KEY`, KHÔNG khai
> navigation property.**

```sql
-- ĐÚNG: cột id trần trong bảng của module
CREATE TABLE banhang.don_hang (
    id           uuid PRIMARY KEY,
    nguoi_tao_id uuid NOT NULL,          -- trỏ core.app_user.id, KHÔNG có FK
    tong_tien    numeric(18,2) NOT NULL
);

-- SAI: khoá ngoại vượt ranh giới schema
ALTER TABLE banhang.don_hang
    ADD CONSTRAINT fk_don_hang_app_user
    FOREIGN KEY (nguoi_tao_id) REFERENCES core.app_user (id);   -- vi phạm luật E5
```

Luật E5 ở [`../RULES.md`](../RULES.md) §4 ép điều này bằng ArchTest
`NoForeignKey_CrossesSchemaBoundary`. Database không còn đảm bảo id trần trỏ tới một dòng có thật; bù lại user **không bị xoá cứng** (§1.2).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §1.1

### 1.2 Người dùng không bị xoá cứng

`core.app_user` không có `is_deleted` và cũng không được `DELETE`. Vô hiệu hoá một tài khoản đi
qua `lockout_end` của Identity — chi tiết ở §4.1.

### 1.3 Ranh giới tenant — `core.tenant` và danh sách miễn trừ

> **Nhiều đơn vị hành chính độc lập dùng chung MỘT bản cài, MỘT database, MỘT schema.** Thứ tách
> dữ liệu của họ là cột `tenant_id` cộng bộ lọc truy vấn toàn cục — không phải ranh giới vật lý.
> Quyết định: [`../adr/0013-multi-tenant.md`](../adr/0013-multi-tenant.md) · thi công:
> [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) · luật M1–M7:
> [`../RULES.md`](../RULES.md) §9.

#### `core.tenant` — một dòng là một đơn vị

**Mục đích:** danh tính của đơn vị. Bảng này là **gốc** của mô hình, nên bản thân nó **không**
mang `tenant_id`.

| Cột | Kiểu | Null | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | — | UUID v7, ứng dụng sinh (§3.1) |
| `code` | `varchar(50)` | NOT NULL | — | **Mã đơn vị người dùng gõ ở form đăng nhập.** Chuẩn hoá về chữ HOA ở tầng ứng dụng trước khi ghi và trước khi tra |
| `name` | `varchar(200)` | NOT NULL | — | Tên đơn vị, hiển thị |
| `is_active` | `boolean` | NOT NULL | `true` | Sai ⇒ **mọi tài khoản của đơn vị đó không đăng nhập được** |
| `is_system` | `boolean` | NOT NULL | `false` | **Đơn vị hệ thống** — nơi tài khoản vận hành trú ([`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md)). Thiếu cột này thì mã lỗi `CORE.TENANT.SYSTEM_IMMUTABLE` ở [`../contracts/tenants.md`](../contracts/tenants.md) **không ai viết nổi** |
| `created_at` · `created_by` · `updated_at` · `updated_by` | như §3.2 | NULL | — | Interceptor điền, qua interface audit — xem dưới |

- **PK:** `pk_tenant (id)`
- **Unique constraint:** `uq_tenant_code (code)` — unique **đầy đủ**, không lọc
- **Unique index một phần:** `ux_tenant_is_system` trên `(is_system) WHERE is_system` — cho phép **nhiều nhất một** đơn vị hệ thống. Luật **M10**; database ép, không phải test.

> **`Tenant` KHÔNG kế thừa `BaseEntity`, và đó là chủ đích** — cùng khuôn với `AppUser` (§4.1): tenant không bao giờ được xoá nên không có `is_deleted`; nó implement trực tiếp interface audit để interceptor điền bốn cột audit — [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §1.4. Khoá duy nhất mang tiền tố `uq_`, không phải `ux_…_active` (§2.3).

#### Danh sách miễn trừ — bảng KHÔNG mang `tenant_id`

Mỗi mục dưới đây là **dữ liệu dùng chung toàn hệ**, không thuộc đơn vị nào. Luật M4
(`EveryBusinessEntity_IsTenantScoped_OrExempt`) đọc đúng danh sách này.

Hình thức thi công: danh sách đi vào `CoreAndSkill.ArchTests` dưới dạng một dictionary C# — khoá
là `schema.table` lấy từ EF model, giá trị là lý do — cùng khuôn hai allowlist của luật S4 và M5.
Ở giai đoạn 1, bảng dưới đây là chủ; việc chuyển chủ sang dictionary đó nằm trong
[`../adr/0030-dieu-kien-chuyen-giai-doan-2.md`](../adr/0030-dieu-kien-chuyen-giai-doan-2.md).

| Bảng | Vì sao miễn trừ |
| --- | --- |
| `core.tenant` | Là chính danh tính tenant. Một bảng tự trỏ vào mình bằng `tenant_id` thì không còn gốc để bắt đầu |
| `core.permission` | **Danh mục** quyền là hợp đồng giữa code và dữ liệu: `[RequirePermission("core.user.read")]` phải khớp một dòng, và chuỗi đó nằm trong Core chứ không thuộc đơn vị nào. Cho mỗi tenant một bản sao nghĩa là mỗi tenant có thể có một danh mục **lệch**, và một endpoint sẽ từ chối ở đơn vị này mà cho qua ở đơn vị kia. Thứ **thuộc tenant** là ánh xạ vai trò → quyền (`role_permission`), không phải danh mục |
| `core.permission_resource` | Cùng lý do — nó là trục hàng của ma trận phân quyền, do lập trình viên và module khai, không do người dùng tạo |
| `core.schema_script_history` | Nhật ký **vận hành của database**, không của một đơn vị. Một script chạy một lần cho cả bản cài |
| `core.__ef_migrations_history` | Của EF, cùng lý do trên. Không đụng vào |
| `core.data_protection_key` (§9.3) | Khoá ký và mã hoá phiếu xác thực là của **bản cài**. Tách theo tenant thì một instance không giải được phiếu của tenant khác dù cùng tiến trình — và mọi tenant vẫn dùng chung tiến trình đó |

#### Miễn trừ thứ hai — bảng CÓ `tenant_id` nhưng cho phép NULL

Luật cột ở §3.7 là `tenant_id NOT NULL`. Đúng **một** bảng được miễn, và nó phải có tên ở đây:

| Bảng | Vì sao miễn trừ |
| --- | --- |
| `core.setting` (§9.5) | Bảng này có **phạm vi hệ thống** — một giá trị dùng chung cho mọi đơn vị, do người vận hành đặt lúc cài đặt ([`../wiki-core/be/19-cau-hinh-theo-don-vi.md`](../wiki-core/be/19-cau-hinh-theo-don-vi.md) §2, tầng 3). Dòng phạm vi hệ thống theo định nghĩa **không thuộc đơn vị nào**, nên không có giá trị `tenant_id` nào đúng cho nó. Cột `scope` là thứ nói dòng đang ở phạm vi nào; `tenant_id` rỗng **chỉ** hợp lệ khi `scope = 'system'` |

Miễn trừ này kéo theo một nghĩa vụ: đường đọc cấu hình phải lấy được cả ba phạm vi trong một lần tra, không dựa vào bộ lọc `tenant_id = @tenant` mặc định.

> 🛑 **Danh sách này phải NGẮN.** Thêm một dòng vào bảng trên là một quyết định kiến trúc: nó phải nêu lý do **dữ liệu này có ý nghĩa như nhau với mọi đơn vị**.

**Mọi bảng `core` khác đều mang `tenant_id`** — bảy bảng Identity, ba bảng phân quyền (trừ hai
danh mục ở trên), hai bảng menu, hai bảng thông báo, `outbox_message`, `audit_log`, `file` và `job`. Quy ước cột ở §3.7.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §1.3

---

## 2. Quy ước đặt tên — bắt buộc

### 2.1 Bảng và cột

| Đối tượng | Quy tắc | Ví dụ |
| --- | --- | --- |
| Schema | snake_case, một từ | `core`, `banhang` |
| Bảng | **snake_case, SỐ ÍT** | `app_user`, `role_permission`, `menu_item` |
| Cột | snake_case | `created_at`, `must_change_password` |
| Cột khoá ngoại | `<bảng được trỏ>_id` | `role_id`, `menu_item_id` |
| Cột boolean | tiền tố `is_` / `has_` / `must_` | `is_deleted`, `must_change_password` |
| Cột thời điểm | hậu tố `_at` | `created_at`, `read_at`, `processed_at` |

> ⚠️ **ASP.NET Core Identity đặt tên PascalCase số nhiều (`AspNetUsers`).** Ta **đổi tên tường
> minh** trong EF configuration, xem §4.0. Đây là thao tác bắt buộc, không phải tuỳ chọn.

#### Cơ chế ánh xạ, và một bẫy đi kèm

Property C# giữ PascalCase (`CreatedAt`); **cột vật lý** là snake_case (`created_at`). Ánh xạ do
một naming convention áp cho toàn model, khai **một lần** khi cấu hình `DbContext` — không phải
`HasColumnName(...)` gõ tay ở từng property.

> 🪤 **Mọi mảnh SQL THÔ trong EF configuration phải dùng tên VẬT LÝ, không phải tên property.**
> Naming convention chỉ dịch tên property; nó **không** đụng tới chuỗi ta tự viết. Cụ thể ở
> mệnh đề lọc của unique index:
>
> ```csharp
> builder.HasIndex(x => x.Code).IsUnique().HasFilter("is_deleted = false");   // ĐÚNG
> builder.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false"); // SAI ở repo này
> ```
>
> Cùng lý do, mọi `migrationBuilder.Sql(...)` viết tay dùng tên vật lý.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §2.1

### 2.2 Ràng buộc và index

| Loại | Tiền tố | Khuôn | Ví dụ |
| --- | --- | --- | --- |
| Primary key | `pk_` | `pk_<bảng>` | `pk_app_user` |
| Foreign key | `fk_` | `fk_<bảng>_<cột>` | `fk_role_permission_role_id` |
| Unique index | `ux_` | `ux_<bảng>_<cột…>` | `ux_permission_code` |
| Unique index **lọc theo soft delete** | `ux_` + hậu tố `_active` | `ux_<bảng>_<cột…>_active` | `ux_menu_item_tenant_code_active` |
| Unique index trên bảng **có `tenant_id`** | `ux_` + `tenant` ngay sau tên bảng | `ux_<bảng>_tenant_<cột…>[_active]` | `ux_role_permission_tenant_role_perm_active` |
| Unique constraint (không lọc) | `uq_` | `uq_<bảng>_<cột…>` | `uq_permission_resource_key` |
| Index thường | `ix_` | `ix_<bảng>_<cột…>` | `ix_menu_item_parent_id` |
| Check constraint | `ck_` | `ck_<bảng>_<luật>` | `ck_notification_severity` |

> 🪤 **PostgreSQL cắt định danh ở 63 byte, âm thầm.** Kiểm trước khi đặt tên dài:
>
> ```sql
> SELECT length('ux_notification_recipient_notif_user_active');
> ```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §2.2

### 2.3 Hậu tố `_active` là một hợp đồng, không phải trang trí

Thấy `_active` ở cuối tên index nghĩa là index đó **có mệnh đề `WHERE is_deleted = false`**.
Không có hậu tố thì không có mệnh đề. Quy ước này tồn tại để đọc `\di core.*` là biết ngay index
nào tính tới soft delete — §3.3 giải thích vì sao đó là câu hỏi quan trọng nhất của cả schema.

---

## 3. Quy ước cột — áp cho mọi bảng `core`

### 3.1 Khoá chính: `uuid`, ứng dụng tự sinh, UUID v7

```sql
id uuid NOT NULL,          -- KHÔNG có DEFAULT gen_random_uuid()
CONSTRAINT pk_menu_item PRIMARY KEY (id)
```

Ba quyết định gói trong một dòng: kiểu `uuid`, không `bigserial`; **không** `DEFAULT gen_random_uuid()` — ứng dụng tự sinh (`Guid.CreateVersion7()`); **UUID v7**, không v4.

**Ngoại lệ đã biết, đừng "sửa cho đồng bộ":**

| Bảng | Kiểu `id` |
| --- | --- |
| `app_user_claim`, `app_role_claim` | `integer GENERATED BY DEFAULT AS IDENTITY` — schema chuẩn của Identity |
| `schema_script_history` | `bigint GENERATED ALWAYS AS IDENTITY` — bảng vận hành, cần thứ tự áp dụng đọc được bằng mắt |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §3.1

### 3.2 Năm cột audit

Mọi bảng `core` kế thừa `BaseEntity` mang **đúng năm** cột này, không hơn:

```sql
created_at  timestamptz  NULL,
created_by  varchar(100) NULL,
updated_at  timestamptz  NULL,
updated_by  varchar(100) NULL,
is_deleted  boolean      NOT NULL DEFAULT false
```

| Cột | Ai ghi | Ghi chú |
| --- | --- | --- |
| `created_at` / `created_by` | Interceptor EF, lúc `Added` | `created_by` giữ **tên đăng nhập**, không phải id |
| `updated_at` / `updated_by` | Interceptor EF, lúc `Added` **và** `Modified` | Điền ngay từ lúc tạo (bằng đúng giá trị của `created_*`), **không** để `NULL`. Interceptor gán **cùng một** giá trị thời gian cho cả hai — phép thử "đã từng bị sửa" là `updated_at <> created_at` |
| `is_deleted` | Ứng dụng | `NOT NULL DEFAULT false` |

Chi tiết interceptor: [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md).

Luật E1 ([`../RULES.md`](../RULES.md) §4) cho phép **đúng năm** field audit có public setter.
Detector của nó đếm đúng năm tên trên. **Không có `deleted_at` / `deleted_by`** — dự án nào thêm thì phải nới detector E1 lên bảy tên **cùng lượt**.

**Bảng KHÔNG mang khối này** — mỗi ca đều có lý do, không phải bỏ sót:

| Bảng | Vì sao |
| --- | --- |
| Bảy bảng Identity | Identity tự quản vòng đời. Xem §4.1 |
| `tenant` | Có bốn cột audit nhưng **không** có `is_deleted`: tenant không bao giờ bị xoá. Xem §1.3 |
| `code_sequence` | Có bốn cột audit nhưng **không** có `is_deleted`: bộ đếm không xoá mềm. Xem §9.6 |
| `outbox_message` | Hàng đợi vận hành, dòng bị dọn theo chính sách lưu trữ chứ không xoá mềm. Xem §8 |
| `audit_log` | Chỉ ghi thêm. Xem §9.4 |
| `schema_script_history` | Nhật ký chỉ-ghi-thêm. Xem §9.1 |
| `__ef_migrations_history` | Của EF, không đụng vào. Xem §9.2 |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §3.2

### 3.3 🪤 Soft delete phá unique index — bẫy thật, đọc kỹ

Có `is_deleted`, một unique index viết theo thói quen sẽ hỏng: dòng đã xoá mềm **vẫn nằm trong bảng** và chiếm khoá, tạo lại cùng `code` → `23505 duplicate key value violates unique constraint`.

```sql
-- SAI
CREATE UNIQUE INDEX ux_menu_item_code ON core.menu_item (code);

-- ĐÚNG: unique index MỘT PHẦN
CREATE UNIQUE INDEX ux_menu_item_code_active
    ON core.menu_item (code)
    WHERE is_deleted = false;
```

Ba hệ quả bắt buộc phải nhớ:

1. **Khoá chính không bao giờ là khoá nghiệp vụ.** `role_permission` có PK đơn `id`; tính duy
   nhất của cặp `(role_id, permission_id)` hạ xuống thành unique index một phần.
2. **`ON CONFLICT` phải lặp lại NGUYÊN VĂN vị từ.** Thiếu mệnh đề `WHERE` khớp đúng, câu lệnh **abort**:

   ```sql
   INSERT INTO core.role_permission (id, role_id, permission_id, is_deleted, created_at, created_by)
   SELECT gen_random_uuid(), r.id, p.id, false, now(), 'system'
   FROM   core.app_role r
   CROSS  JOIN core.permission p
   WHERE  r.is_system = true
   ON CONFLICT (role_id, permission_id) WHERE is_deleted = false DO NOTHING;
   ```
3. **Hàm băm phiên bản ma trận không được `IgnoreQueryFilters()`** — token phiên bản ở
   [`../contracts/permissions.md`](../contracts/permissions.md) chỉ tính dòng chưa xoá mềm.

**Hai phép thử phải có trong integration test** (luật T2, chạy trên Postgres thật):

- Xoá mềm một `code` rồi tạo lại đúng `code` đó **phải thành công** — bắt ca thiếu `WHERE`.
- Hai dòng cùng `code` cùng chưa xoá **phải** bị chặn `23505` — bắt ca gỡ nhầm cả index.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §3.3

### 3.4 Thời gian: `timestamptz`, không bao giờ `timestamp`

```sql
created_at timestamptz NULL      -- ĐÚNG
created_at timestamp   NULL      -- CẤM
```

Hệ quả cho code: mọi mốc thời gian sinh bằng `DateTimeOffset.UtcNow`, không `DateTime.Now`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §3.4

### 3.5 `text` hay `varchar(n)`

Chọn theo **ý nghĩa**, không theo hiệu năng:

| Dùng | Khi nào | Ví dụ |
| --- | --- | --- |
| `varchar(n)` | `n` là **luật nghiệp vụ** mà FE cũng phải biết (`maxlength` của ô nhập, độ rộng cột lưới) | `code varchar(100)`, `full_name varchar(200)` |
| `text` | Không có giới hạn nghiệp vụ nào, hoặc giới hạn do thư viện quyết | `password_hash text`, `last_error text` |

Với `varchar(n)`: chọn rộng hơn một bậc so với nhu cầu hôm nay — nới thì rẻ, siết lại thì không.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §3.5

### 3.6 Concurrency token — hệ quả trên schema

> 📖 **File chủ của chủ đề concurrency là**
> [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §6 — cách khai property,
> kiểu CLR bắt buộc, và bẫy provider. Mục này chỉ ghi phần **chạm tới schema**.

Hệ quả duy nhất trên schema: **không có cột `row_version` nào trong `core`.** Token dùng cột
hệ thống `xmin` mà PostgreSQL đã có sẵn cho mọi dòng, nên nó không xuất hiện trong DDL, không
xuất hiện trong `information_schema.columns`, và không cần migration riêng.

| | |
| --- | --- |
| Đối soát schema | Đừng đi tìm một cột phiên bản trong bảng nào cả. Không thấy **là đúng** |
| Bảng Identity | `app_user` và `app_role` đã có `concurrency_stamp` do Identity quản. **Không** thêm token thứ hai |

Ma trận quyền dùng cơ chế khác hẳn — token băm cấp **tập hợp**, không phải cấp dòng. Xem
[`../contracts/permissions.md`](../contracts/permissions.md) và
[`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §3.6

### 3.7 Cột `tenant_id` — áp cho mọi bảng dữ liệu tenant

```sql
tenant_id uuid NOT NULL,
CONSTRAINT fk_menu_item_tenant_id FOREIGN KEY (tenant_id) REFERENCES core.tenant (id)
    ON DELETE RESTRICT
```

| Quyết định | Chi tiết |
| --- | --- |
| `NOT NULL`, không mặc định | Interceptor điền giá trị, không phải DB. Miễn trừ có tên duy nhất: `core.setting`, khai ở §1.3 |
| Ứng dụng điền, **không** `DEFAULT` | Giá trị đến từ claim của phiếu xác thực — [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §2 |
| Không bao giờ `UPDATE` | Interceptor chặn ca `Modified` có `tenant_id` bị sửa |
| FK `ON DELETE RESTRICT` **trong schema `core`** | Tenant không xoá được; `RESTRICT` biến điều đó thành ràng buộc DB |
| **Bảng của module: `tenant_id` là id trần, KHÔNG có FK** | FK sẽ vượt ranh giới schema — vi phạm luật E5 (§1.1) |

**Vị trí trong index:** `tenant_id` đứng **đầu** mọi index đa cột trên bảng có nó. Ngoại lệ có chủ đích duy nhất là index của `outbox_message` (§8).

> 🛑 **Mọi index duy nhất trên bảng có `tenant_id` phải GỒM `tenant_id`** — luật M3, ArchTest
> `EveryUniqueIndex_OnTenantScoped_Includes_TenantId`. Cộng với §3.3, một unique index trên bảng vừa có `tenant_id` vừa có `is_deleted` phải gồm **ba**
> thứ: cột nghiệp vụ, `tenant_id`, và mệnh đề `WHERE is_deleted = false`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §3.7

---

## 4. Nhóm Identity — bảy bảng

`Core.Infrastructure` giữ `AppUser : IdentityUser<Guid>` và `AppRole : IdentityRole<Guid>`.
Luật S5 ([`../RULES.md`](../RULES.md) §6) cấm hai kiểu này rò ra ngoài `Core.Infrastructure`;
`Core.Application` chỉ thấy các seam ở [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §7.1.

### 4.0 Bảng đổi tên — bắt buộc khai tường minh

| Tên Identity sinh mặc định | Tên vật lý trong schema `core` |
| --- | --- |
| `AspNetUsers` | `app_user` |
| `AspNetRoles` | `app_role` |
| `AspNetUserRoles` | `app_user_role` |
| `AspNetUserClaims` | `app_user_claim` |
| `AspNetRoleClaims` | `app_role_claim` |
| `AspNetUserLogins` | `app_user_login` |
| `AspNetUserTokens` | `app_user_token` |

Đổi tên khai một chỗ, trong `OnModelCreating` của `CoreDbContext`. Cột cũng đổi sang snake_case
cùng lượt (`NormalizedUserName` → `normalized_user_name`).

> **Không đổi tên ba index Identity tự đặt** — `UserNameIndex`, `EmailIndex`, `RoleNameIndex`.
>
> 🛑 **Giữ TÊN không có nghĩa là giữ ĐỊNH NGHĨA.** Cả ba index trên đều được **khai lại với tập
> cột mới** để gồm `tenant_id` (§4.1, §4.2).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §4.0

### 4.1 `core.app_user`

**Mục đích:** tài khoản đăng nhập. Đây là bảng người dùng **duy nhất** — cần thêm thông tin thì
thêm cột vào đây, tuyệt đối không dựng một bảng `user_profile` song song.

| Cột | Kiểu | Null | Mặc định | Nguồn |
| --- | --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | — | Identity |
| `tenant_id` | `uuid` | NOT NULL | — | **Ta thêm** (§3.7) |
| `user_name` | `varchar(256)` | NULL | — | Identity |
| `normalized_user_name` | `varchar(256)` | NULL | — | Identity |
| `email` | `varchar(256)` | NULL | — | Identity |
| `normalized_email` | `varchar(256)` | NULL | — | Identity |
| `email_confirmed` | `boolean` | NOT NULL | `false` | Identity |
| `password_hash` | `text` | NULL | — | Identity |
| `security_stamp` | `text` | NULL | — | Identity |
| `concurrency_stamp` | `text` | NULL | — | Identity |
| `phone_number` | `varchar(20)` | NULL | — | Identity — **nới kiểu so với cột gốc** theo §3.5: trần 20 ký tự là luật nghiệp vụ ([`../contracts/profile.md`](../contracts/profile.md) §2) |
| `phone_number_confirmed` | `boolean` | NOT NULL | `false` | Identity |
| `two_factor_enabled` | `boolean` | NOT NULL | `false` | Identity |
| `lockout_end` | `timestamptz` | NULL | — | Identity |
| `lockout_enabled` | `boolean` | NOT NULL | `true` | Identity |
| `access_failed_count` | `integer` | NOT NULL | `0` | Identity |
| `full_name` | `varchar(200)` | NOT NULL | — | **Ta thêm** |
| `must_change_password` | `boolean` | NOT NULL | `true` | **Ta thêm** |
| `preferred_language` | `varchar(10)` | NULL | — | **Ta thêm.** Ngôn ngữ ưa thích, người dùng tự đặt ở màn hồ sơ ([`../contracts/profile.md`](../contracts/profile.md)). Rỗng ⇒ dùng ngôn ngữ mặc định của hệ thống. Đây cũng là nguồn ngôn ngữ khi gửi thông báo ([`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §4.2) |
| `is_system_operator` | `boolean` | NOT NULL | `false` | **Ta thêm.** Cờ vào khu quản trị hệ thống — [`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md). Không đi qua ma trận quyền, và luật M9 cấm gán vai trò nghiệp vụ cho tài khoản mang cờ này |
| `has_permission_bypass` | `boolean` | NOT NULL | `false` | **Ta thêm.** Cờ của tài khoản quản trị đầu tiên **của một đơn vị**: bỏ qua kiểm **permission**, **KHÔNG** bỏ qua bộ lọc đơn vị ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.3). Xem [`../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) |
| `locked_by_admin` | `boolean` | NOT NULL | `false` | **Ta thêm.** `true` khi khoá do quản trị đặt tay ([`../contracts/users.md`](../contracts/users.md) §8); khoá tự động sau nhiều lần sai không đặt; mở khoá đặt lại `false` |
| `created_at` | `timestamptz` | NULL | — | **Ta thêm** |
| `created_by` | `varchar(100)` | NULL | — | **Ta thêm** |
| `updated_at` | `timestamptz` | NULL | — | **Ta thêm** |
| `updated_by` | `varchar(100)` | NULL | — | **Ta thêm** |

- **PK:** `pk_app_user (id)`
- **FK:** `fk_app_user_tenant_id → core.tenant (id) ON DELETE RESTRICT`
- **Index:** `UserNameIndex` UNIQUE (`tenant_id`, `normalized_user_name`) ·
  `EmailIndex` UNIQUE (`tenant_id`, `normalized_email`)
- **Không có `is_deleted`** — xem ghi chú dưới đây.

> 🛑 **Tên đăng nhập và email KHÔNG còn duy nhất toàn hệ.** Duy nhất tính theo **cặp** với `tenant_id`, nên luồng đăng nhập phải nạp tenant **trước** khi gọi Identity ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11). `EmailIndex` UNIQUE chỉ đúng khi tuỳ chọn *"email phải duy nhất"* của Identity đang được dùng; nếu tắt nó thì bỏ `UNIQUE` và giữ nguyên hai cột.

> **`AppUser` KHÔNG kế thừa `BaseEntity`, và đó là chủ đích** — Identity quản vòng đời bằng field riêng. `AppUser` implement interface audit, nên
> interceptor điền bốn cột audit ở đây như ở mọi bảng khác —
> [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §1.4.

**Khoá / mở khoá đi qua `lockout_end`, KHÔNG thêm cột `is_active`:**

```text
lockout_end IS NULL  hoặc  lockout_end < now()   →  đang hoạt động
lockout_end >= now()                             →  đã khoá
```

Khoá bằng `UserManager.SetLockoutEndDateAsync` với một mốc xa trong tương lai. **Không ghi cột
này bằng SQL tay** — thao tác khoá còn phải đổi `security_stamp` tường minh trong cùng thao tác
([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.4).

> 🛑 **Hai cờ đặc quyền, hai cột, LOẠI TRỪ nhau.** Một tài khoản không được mang cả `has_permission_bypass` lẫn `is_system_operator`:
>
> | | `has_permission_bypass` | `is_system_operator` |
> | --- | --- | --- |
> | Thuộc | Một đơn vị **nghiệp vụ** | Đơn vị **hệ thống** |
> | Thấy | Mọi thứ **trong đơn vị mình** | Danh sách đơn vị + trạng thái |
> | **Không** thấy | Đơn vị khác | **Dữ liệu nghiệp vụ của mọi đơn vị** (luật M9) |
>
> **Ràng buộc kiểm tra ở database**, không phải ở test: `NOT (has_permission_bypass AND
> is_system_operator)`. Luật **M11**.

> 🪤 **KHÔNG có cách nào tạo mật khẩu hợp lệ bằng SQL.** Tài khoản đầu tiên **phải** tạo qua `UserManager.CreateAsync` — không
> qua migration, không qua script `.sql` — tức lệnh bootstrap ở [`script-runbook.md`](script-runbook.md) §3.3.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §4.1

### 4.2 `core.app_role`

**Mục đích:** vai trò. **Vai trò là DỮ LIỆU, không phải hằng số trong code** — luật S1
([`../RULES.md`](../RULES.md) §6). Không có `Roles.SuperAdmin` ở bất kỳ đâu trong `Core.*`.

| Cột | Kiểu | Null | Mặc định | Nguồn |
| --- | --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | — | Identity |
| `tenant_id` | `uuid` | NOT NULL | — | **Ta thêm** (§3.7) |
| `name` | `varchar(256)` | NULL | — | Identity |
| `normalized_name` | `varchar(256)` | NULL | — | Identity |
| `concurrency_stamp` | `text` | NULL | — | Identity |
| `description` | `varchar(500)` | NULL | — | **Ta thêm** |
| `is_system` | `boolean` | NOT NULL | `false` | **Ta thêm** |
| `created_at` · `created_by` · `updated_at` · `updated_by` | như §3.2 | NULL | — | **Ta thêm** |

- **PK:** `pk_app_role (id)` · **Index:** `RoleNameIndex` UNIQUE (`tenant_id`, `normalized_name`)
- **FK:** `fk_app_role_tenant_id → core.tenant (id) ON DELETE RESTRICT`

Vai trò thuộc về một đơn vị; thứ dùng chung toàn hệ là **danh mục quyền** (§5.1, §5.2), không phải vai trò.

> Một vai trò `is_system = true` không xoá được, không đổi tên được, và không bị thu hồi quyền
> `core.permission.write` (§5.3). Luật S2 vẫn nguyên: phân quyền kiểm bằng **permission**, không bằng `name` của vai trò —
> `is_system` chỉ bảo vệ **vòng đời** của chính dòng đó, không bao giờ được dùng làm điều kiện
> cho phép đi qua một endpoint.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §4.2

### 4.3 Năm bảng Identity còn lại

| Bảng | Mục đích | PK | FK |
| --- | --- | --- | --- |
| `app_user_role` | Gán vai trò cho người dùng (nhiều–nhiều) | `(user_id, role_id)` | `→ app_user`, `→ app_role`, cả hai `ON DELETE CASCADE` |
| `app_user_claim` | Claim cấp người dùng | `id` (`integer` identity) | `→ app_user` CASCADE |
| `app_role_claim` | Claim cấp vai trò | `id` (`integer` identity) | `→ app_role` CASCADE |
| `app_user_login` | Đăng nhập ngoài (SSO/OAuth) | `(tenant_id, login_provider, provider_key)` | `→ app_user` CASCADE |
| `app_user_token` | Token nội bộ Identity (đặt lại mật khẩu, 2FA) | `(user_id, login_provider, name)` | `→ app_user` CASCADE |

**Cả năm bảng mang `tenant_id`** (§3.7), kèm FK `→ core.tenant (id) ON DELETE RESTRICT`.
Index kèm theo dẫn đầu bằng `tenant_id`: `ix_app_user_role_tenant_role_id`,
`ix_app_user_claim_tenant_user_id`, `ix_app_role_claim_tenant_role_id`,
`ix_app_user_login_tenant_user_id`. Phía C#: năm kiểu join là **lớp con cài `ITenantScoped`** và
`CoreDbContext` khai đủ kiểu — chữ ký ở [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §5.1.

> **`app_user_claim` / `app_role_claim` / `app_user_login` chưa dùng ở v1 — vẫn phải dựng.**
>
> **`app_user_role` cố ý dùng PK ghép, không PK đơn `id`** — bảng do Identity sở hữu, không mang `is_deleted`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §4.3

---

## 5. Nhóm phân quyền — ba bảng

Mô hình: **người dùng → vai trò → quyền**. Kiểm tra luôn kết thúc ở một mã quyền, không bao giờ
ở một tên vai trò.

```text
app_user ──< app_user_role >── app_role ──< role_permission >── permission ──> permission_resource
```

Hai bảng danh mục (§5.1, §5.2) vào database **chỉ** bằng migration
([`migration-policy.md`](migration-policy.md) §4.1); tiến trình ứng dụng chỉ đọc chúng — luật M13,
bảng quyền ở [`script-runbook.md`](script-runbook.md) §3.6.

### 5.1 `core.permission_resource` — danh mục tài nguyên

**Mục đích:** trục **hàng** của ma trận phân quyền. Bảng này tồn tại để màn hình phân quyền liệt
kê được **mọi** tài nguyên, kể cả tài nguyên chưa cấp cho vai trò nào.

> **Không mang `tenant_id`** — miễn trừ đã khai ở §1.3.

| Cột | Kiểu | Null | Mặc định |
| --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | — |
| `key` | `varchar(100)` | NOT NULL | — |
| `name_key` | `varchar(200)` | NOT NULL | — |
| `module_key` | `varchar(100)` | NULL | — |
| `display_order` | `integer` | NOT NULL | `0` |
| 5 cột audit §3.2 | | | |

- **PK:** `pk_permission_resource (id)`
- **Unique constraint:** `uq_permission_resource_key (key)` — **không lọc**, xem cảnh báo ở §5.2
- **Index:** `ix_permission_resource_module_key (module_key)`

`name_key` là **khoá i18n**, không phải câu tiếng Việt — luật R8 cấm câu hiển thị nằm trong BE.
`module_key = NULL` nghĩa là tài nguyên của Core.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §5.1

### 5.2 `core.permission` — danh mục quyền

**Mục đích:** một dòng là một hành động kiểm được. `code` là thứ `[RequirePermission("…")]` nhận.

> **Không mang `tenant_id`** — miễn trừ đã khai ở §1.3. Vì vậy `ux_permission_code_active` giữ
> nguyên tập cột: nó là khoá duy nhất **toàn hệ**, đúng như phải thế.

| Cột | Kiểu | Null | Mặc định |
| --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | — |
| `code` | `varchar(150)` | NOT NULL | — |
| `resource_key` | `varchar(100)` | NOT NULL | — |
| `action` | `varchar(50)` | NOT NULL | — |
| `name_key` | `varchar(200)` | NOT NULL | — |
| `is_system` | `boolean` | NOT NULL | `false` |
| `display_order` | `integer` | NOT NULL | `0` |
| 5 cột audit §3.2 | | | |

- **PK:** `pk_permission (id)`
- **Unique:** `ux_permission_code_active (code) WHERE is_deleted = false`
- **FK:** `fk_permission_resource_key (resource_key) → core.permission_resource (key) ON DELETE RESTRICT`
- **Index:** `ix_permission_resource_key_display_order (resource_key, display_order)`

**Khuôn `code`: `<resource_key>.<action>`**, chữ thường, phân đoạn nối bằng dấu chấm.

#### Danh mục khoá phân quyền Core — định nghĩa gốc

Bảng dưới đây là **nguồn duy nhất** của tập khoá mà Core seed vào `core.permission`. Hằng số
trong `Core.Application` và mọi `[RequirePermission("…")]` phải khớp **đúng chuỗi** ở cột
`code`. File khác chỉ được nhắc **một** khoá của đúng ca đang nói tới; chép lại cả danh sách
là vi phạm [`../OWNERSHIP.md`](../OWNERSHIP.md) §2.

| `code` | `resource_key` | `action` | Cho phép |
| --- | --- | --- | --- |
| `core.user.read` | `core.user` | `read` | Xem danh sách và chi tiết người dùng |
| `core.user.write` | `core.user` | `write` | Tạo, sửa thông tin người dùng |
| `core.user.lock` | `core.user` | `lock` | Khoá / mở khoá tài khoản |
| `core.user.reset-password` | `core.user` | `reset-password` | Đặt lại mật khẩu cho người khác |
| `core.user.export` | `core.user` | `export` | Xuất danh sách người dùng ra tệp |
| `core.user.role.assign` | `core.user.role` | `assign` | Gán / gỡ vai trò của một người dùng |
| `core.role.read` | `core.role` | `read` | Xem danh sách và chi tiết vai trò |
| `core.role.write` | `core.role` | `write` | Tạo, sửa, xoá vai trò từ màn quản trị |
| `core.permission.read` | `core.permission` | `read` | Xem ma trận và danh mục quyền |
| `core.permission.write` | `core.permission` | `write` | Ghi ma trận phân quyền |
| `core.menu.read` | `core.menu` | `read` | Xem cấu hình menu |
| `core.menu.write` | `core.menu` | `write` | Sửa cấu hình menu |

Ba ràng buộc khi thêm dòng:

1. **Một khoá đã phát hành thì không đổi tên.**
2. **Không tách khoá theo tên vai trò.**
3. **Tách khoá theo sức phá hoại, không theo màn hình** — `lock`, `reset-password`, `role.assign` tách khỏi `write`.

Module khai khoá của mình theo cùng khuôn, với `resource_key` mang tiền tố của module — Core
không biết chúng tồn tại.

> 🪤 **FK trỏ vào `permission_resource.key`, không vào `id` — và điều đó buộc một ngoại lệ.** Postgres không nhận index **một phần** làm đích của foreign key, nên **chốt:** `uq_permission_resource_key UNIQUE (key)` — unique **đầy đủ**; tài nguyên đã xoá mềm vẫn giữ khoá. Đây là ngoại lệ **có chủ đích duy nhất** của §3.3 trong schema `core`; câu kiểm (3) ở §11 sẽ báo đúng dòng này.
>
> Muốn xoá rồi tạo lại cùng `key`: xoá **cứng** dòng cũ sau khi đã dời hết `permission` con sang
> tài nguyên khác — thao tác hiếm, làm tay có kiểm soát.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §5.2

### 5.3 `core.role_permission` — cấp quyền

**Mục đích:** ô của ma trận. Có dòng = được cấp; không có dòng = **bị từ chối** (deny-by-default).

| Cột | Kiểu | Null | Mặc định |
| --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | — |
| `tenant_id` | `uuid` | NOT NULL | — |
| `role_id` | `uuid` | NOT NULL | — |
| `permission_id` | `uuid` | NOT NULL | — |
| 5 cột audit §3.2 | | | |

- **PK:** `pk_role_permission (id)` — PK **đơn**, không ghép; lý do ở §3.3
- **FK:** `fk_role_permission_tenant_id → core.tenant (id) ON DELETE RESTRICT` ·
  `fk_role_permission_role_id → core.app_role (id) ON DELETE CASCADE` ·
  `fk_role_permission_permission_id → core.permission (id) ON DELETE CASCADE`
- **Unique:** `ux_role_permission_tenant_role_perm_active (tenant_id, role_id, permission_id) WHERE is_deleted = false`
- **Index:** `ix_role_permission_tenant_perm_role (tenant_id, permission_id, role_id)` — **bắt buộc**, phục vụ truy vấn kiểm quyền trên mọi request có `[RequirePermission]`; đừng gỡ vì `EXPLAIN` trên dữ liệu seed không dùng tới nó. Quy tắc thứ tự cột: [`../quy-uoc/be-performance.md`](../quy-uoc/be-performance.md)

**Bất biến chống tự khoá cửa:** không được thu hồi quyền `core.permission.write` khỏi một vai trò
`is_system = true`. Kiểm bằng một truy vấn trên **đúng các dòng đang ghi**, không đếm toàn bảng.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §5.3

---

## 6. Nhóm menu — hai bảng

### 6.1 `core.menu_item`

**Mục đích:** cây điều hướng động, **đúng một cấp** cha–con.

| Cột | Kiểu | Null | Mặc định |
| --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | — |
| `tenant_id` | `uuid` | NOT NULL | — |
| `code` | `varchar(100)` | NOT NULL | — |
| `label_key` | `varchar(200)` | NOT NULL | — |
| `icon` | `varchar(50)` | NULL | — |
| `route` | `varchar(200)` | NULL | — |
| `parent_id` | `uuid` | NULL | — |
| `display_order` | `integer` | NOT NULL | `0` |
| `required_permission_id` | `uuid` | NULL | — |
| `module_key` | `varchar(100)` | NULL | — |
| 5 cột audit §3.2 | | | |

- **PK:** `pk_menu_item (id)`
- **Unique:** `ux_menu_item_tenant_code_active (tenant_id, code) WHERE is_deleted = false`
- **FK:** `fk_menu_item_tenant_id → core.tenant (id) ON DELETE RESTRICT` ·
  `fk_menu_item_parent_id → core.menu_item (id) ON DELETE RESTRICT` ·
  `fk_menu_item_required_permission_id → core.permission (id) ON DELETE RESTRICT`
- **Index:** `ix_menu_item_tenant_parent_order (tenant_id, parent_id, display_order)`
- **Check:** `ck_menu_item_not_self_parent CHECK (parent_id IS NULL OR parent_id <> id)`

Menu thuộc về đơn vị: `code` duy nhất **trong một đơn vị**; `required_permission_id` trỏ vào danh mục quyền dùng chung (§5.2).

**`label_key` là khoá i18n, không phải nhãn** — luật R8 và F8.

**Ba luật không diễn đạt được bằng constraint** — kiểm ở tầng ứng dụng và phải có test:

1. **Cây đúng một cấp.** Một mục đã có `parent_id` thì không được có con. **Kiểm ở handler**, không trigger.
2. **Mục cha có `route = NULL`.** Cha chỉ đóng/mở, không điều hướng.
3. **`code` không bao giờ đổi sau khi đã dùng.** Route và quyền đổi được; `code` thì không.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §6.1

### 6.2 `core.menu_item_role`

**Mục đích:** ghim một mục menu cho danh sách vai trò cụ thể. Dùng cho mục **không** gắn với
permission nào — mục chỉ điều hướng, không gọi API nào có thể từ chối.

| Cột | Kiểu | Null | Mặc định |
| --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | — |
| `tenant_id` | `uuid` | NOT NULL | — |
| `menu_item_id` | `uuid` | NOT NULL | — |
| `role_id` | `uuid` | NOT NULL | — |
| 5 cột audit §3.2 | | | |

- **PK:** `pk_menu_item_role (id)`
- **FK:** `fk_menu_item_role_tenant_id → core.tenant (id) ON DELETE RESTRICT` ·
  `fk_menu_item_role_menu_item_id → core.menu_item (id) ON DELETE CASCADE` ·
  `fk_menu_item_role_role_id → core.app_role (id) ON DELETE CASCADE`
- **Unique:** `ux_menu_item_role_tenant_item_role_active (tenant_id, menu_item_id, role_id) WHERE is_deleted = false`
- **Index:** `ix_menu_item_role_tenant_role_id (tenant_id, role_id)`

### 6.3 Thứ tự quyết định mục menu có hiện hay không

Đúng một thứ tự, không có nhánh nào khác:

| Bước | Điều kiện | Kết quả |
| --- | --- | --- |
| 1 | `required_permission_id IS NOT NULL` | Hiện ⇔ người dùng có quyền đó. **Bỏ qua hoàn toàn `menu_item_role`** |
| 2 | Có ít nhất một dòng `menu_item_role` chưa xoá | Hiện ⇔ người dùng thuộc một trong các vai trò đó |
| 3 | Không thoả 1 lẫn 2 | Hiện cho **mọi** người dùng đã đăng nhập |

**Bước 1 thắng tuyệt đối.** **Mục cha luôn được kéo vào khi có bất kỳ con nào hiện** — kể cả khi bản thân mục cha bị quy tắc
trên loại. Hợp đồng cụ thể: [`../contracts/meta-menu.md`](../contracts/meta-menu.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §6.3

---

## 7. `core.notification` và `core.notification_recipient`

> **Bảng thuộc v1**, tạo ở pha B4 ([`../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)) —
> không tạo ở script schema đầu tiên, vì trước pha đó không có gì ghi vào nó.

### 7.1 `core.notification` — nội dung

| Cột | Kiểu | Null | Mặc định |
| --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | — |
| `tenant_id` | `uuid` | NOT NULL | — |
| `code` | `varchar(100)` | NOT NULL | — |
| `params` | `jsonb` | NULL | — |
| `severity` | `varchar(20)` | NOT NULL | `'info'` |
| `link_route` | `varchar(200)` | NULL | — |
| `module_key` | `varchar(100)` | NULL | — |
| 5 cột audit §3.2 | | | |

- **PK:** `pk_notification (id)`
- **FK:** `fk_notification_tenant_id → core.tenant (id) ON DELETE RESTRICT`
- **Check:** `ck_notification_severity CHECK (severity IN ('info','success','warning','error'))`
- **Index:** `ix_notification_tenant_created_at (tenant_id, created_at DESC)`

> **`severity` và `link_route` — CHƯA DÙNG ở v1.** Không endpoint nào ở
> [`../contracts/notifications.md`](../contracts/notifications.md) trả hai giá trị này; `severity` luôn giữ `'info'`, `link_route` luôn rỗng cho tới khi có card khai chúng.

`code` + `params` thay vì một cột `message` chứa câu hoàn chỉnh — luật R7: **tham số truyền theo TÊN**, không theo thứ tự (`{"UserName": "an.nguyen"}`, không `["an.nguyen"]`).

Xem [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) cho cơ chế phát.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §7.1

### 7.2 `core.notification_recipient` — ai nhận, đã đọc chưa

| Cột | Kiểu | Null | Mặc định |
| --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | — |
| `tenant_id` | `uuid` | NOT NULL | — |
| `notification_id` | `uuid` | NOT NULL | — |
| `user_id` | `uuid` | NOT NULL | — |
| `read_at` | `timestamptz` | NULL | — |
| 5 cột audit §3.2 | | | |

- **PK:** `pk_notification_recipient (id)`
- **FK:** `fk_notification_recipient_tenant_id → core.tenant (id) ON DELETE RESTRICT` ·
  `fk_notification_recipient_notification_id → core.notification (id) ON DELETE CASCADE` ·
  `fk_notification_recipient_user_id → core.app_user (id) ON DELETE CASCADE`
- **Unique:** `ux_notification_recipient_tenant_notif_user_active (tenant_id, notification_id, user_id) WHERE is_deleted = false`
- **Index (một phần, cho truy vấn nóng):**

  ```sql
  CREATE INDEX ix_notification_recipient_unread
      ON core.notification_recipient (tenant_id, user_id, created_at DESC)
      WHERE read_at IS NULL AND is_deleted = false;
  ```

Tên unique rút gọn thành `notif_user` **có chủ đích** (ngưỡng 63 byte, §2.2): **phần được rút gọn là tên cột, không bao giờ là phần `tenant`**.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §7.2

---

## 8. `core.outbox_message` — outbox cho integration event

> **Bảng thuộc v1**, tạo ở pha B4 ([`../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)) —
> không tạo ở script schema đầu tiên, vì trước pha đó không có gì ghi vào nó.

**Mục đích:** v1 **không có message broker**
([`../kien-truc-core-module.md`](../kien-truc-core-module.md)). Integration event ghi vào bảng
này **trong cùng transaction** với thay đổi nghiệp vụ; một hosted service đọc và phát sau đó.

| Cột | Kiểu | Null | Mặc định |
| --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | — |
| `tenant_id` | `uuid` | NOT NULL | — |
| `occurred_at` | `timestamptz` | NOT NULL | — |
| `event_type` | `varchar(300)` | NOT NULL | — |
| `payload` | `jsonb` | NOT NULL | — |
| `trace_id` | `varchar(64)` | NULL | — |
| `triggered_by_user_id` | `uuid` | NULL | — |
| `triggered_by_user_name` | `varchar(100)` | NULL | — |
| `status` | `varchar(20)` | NOT NULL | `'pending'` |
| `processed_at` | `timestamptz` | NULL | — |
| `attempt_count` | `integer` | NOT NULL | `0` |
| `next_attempt_at` | `timestamptz` | NULL | — |
| `last_error` | `text` | NULL | — |

- **PK:** `pk_outbox_message (id)`
- **FK:** `fk_outbox_message_tenant_id → core.tenant (id) ON DELETE RESTRICT`
- **Check:** `ck_outbox_message_status CHECK (status IN ('pending','dead','done'))`
- **Index (một phần):**

  ```sql
  CREATE INDEX ix_outbox_message_pending
      ON core.outbox_message (next_attempt_at)
      WHERE status = 'pending';
  ```

`status` là **nguồn duy nhất** của trạng thái dòng: `pending` — chờ phát, `next_attempt_at` chỉ là lịch của lần thử kế; `dead` — chạm ngưỡng thử lại, bộ phát không tự chạm nữa; `done` — đã phát, `processed_at` ghi thời điểm. Ngưỡng và cách phát lại: [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §2.5.

Bộ phát chạy như job nền, **bỏ bộ lọc tenant** (allowlist của luật M5 — [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §7) và mở phạm vi ngữ cảnh thực thi theo **từng dòng** bằng `tenant_id` cùng `triggered_by_user_id` / `triggered_by_user_name` — **người kích hoạt** thao tác đã sinh ra event, do interceptor ghi outbox chụp lúc ghi dòng; hai cột rỗng ⇒ không có người, nhật ký ghi `system`. Cơ chế: [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1 — mục *Danh tính và đơn vị khi không có request*. Index `ix_outbox_message_pending` là ngoại lệ có chủ đích của quy tắc "`tenant_id` đứng đầu index" (§3.7).

**Bảng này KHÔNG mang khối audit và KHÔNG kế thừa `BaseEntity`**, nên nó nằm ngoài luật E3
(soft-delete query filter); dòng đã xử lý được dọn theo chính sách lưu trữ —
[`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md).

`payload` là `jsonb`, không `text`. `trace_id` giữ correlation id của request đã sinh ra event
([`../wiki-core/be/07-observability.md`](../wiki-core/be/07-observability.md)).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §8

---

## 9. Bảng lịch sử và bảng hạ tầng

### 9.1 `core.schema_script_history` — script nào đã chạy

**Mục đích:** trả lời câu *"database này đang ở đâu"* cho **con người**. Bảng này là nguồn sự
thật của quy trình vận hành — [`script-runbook.md`](script-runbook.md) §3.

> **Không mang `tenant_id`** — miễn trừ đã khai ở §1.3.

| Cột | Kiểu | Null | Mặc định |
| --- | --- | --- | --- |
| `id` | `bigint GENERATED ALWAYS AS IDENTITY` | NOT NULL | — |
| `script_name` | `varchar(200)` | NOT NULL | — |
| `checksum_sha256` | `char(64)` | NOT NULL | — |
| `owner` | `varchar(50)` | NOT NULL | — |
| `applied_at` | `timestamptz` | NOT NULL | `now()` |
| `applied_by` | `varchar(100)` | NOT NULL | — |
| `duration_ms` | `integer` | NULL | — |
| `note` | `text` | NULL | — |

- **PK:** `pk_schema_script_history (id)`
- **Unique:** `ux_schema_script_history_script_name (script_name)` — unique **đầy đủ**, không lọc:
  bảng này không có `is_deleted`
- **Index:** `ix_schema_script_history_applied_at (applied_at DESC)`

`checksum_sha256` là băm nội dung file lúc chạy. `owner` nhận `'core'` hoặc khoá module, để lọc được lịch sử theo bên sở hữu.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §9.1

### 9.2 `core.__ef_migrations_history`

Bảng của EF Core, **không mang `tenant_id`** (miễn trừ §1.3). Ta chỉ quyết định **hai** điều về
nó, khai tường minh khi cấu hình `DbContext`:

```csharp
options.UseNpgsql(connectionString, npgsql =>
    npgsql.MigrationsHistoryTable("__ef_migrations_history", "core"));
```

- **Đặt trong schema `core`**, không để rơi vào `public` — nếu không, `public` không còn rỗng và
  câu kiểm (1) ở §11 đỏ.
- **Đổi tên sang snake_case.**

**Cơ chế phát hiện DB lệch model đọc bảng này**; con người tra tiến độ vận hành thì đọc `schema_script_history`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §9.2

### 9.3 `core.data_protection_key` — kho khoá bảo vệ dữ liệu

Key ring của ASP.NET Core Data Protection lưu vào DB (`PersistKeysToDbContext`):

| Cột | Kiểu |
| --- | --- |
| `id` | `integer GENERATED BY DEFAULT AS IDENTITY` |
| `friendly_name` | `text` |
| `xml` | `text` |

**Đã chốt: dựng bảng này ngay từ đầu**, kể cả khi v1 chỉ chạy một instance —
[`../adr/0014-mot-instance-key-ring-postgres.md`](../adr/0014-mot-instance-key-ring-postgres.md)
§2.

> **Bảng này KHÔNG mang `tenant_id`** — miễn trừ đã khai ở §1.3.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §9.3

### 9.4 `core.audit_log` — Bảng nhật ký kiểm toán — định nghĩa gốc

> **Bảng này thuộc v1**, khác với hai bảng ở §7 và §8. Đường **ghi** nhật ký là yêu cầu của
> [`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5. Thứ thuộc Nhóm B
> là **màn hình đọc** nhật ký, không phải bảng.

**Mục đích:** trả lời *"ai đã làm gì, lúc nào, trên cái gì"* — yêu cầu ở
[`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5. Mục này là
**nguồn duy nhất** của danh sách cột; file kia nói *ghi gì và vì sao*, không khai lại cột.

| Cột | Kiểu | Null | Ghi chú |
| --- | --- | --- | --- |
| `id` | `uuid` | ✘ | UUID v7 (§3.1) — sắp theo thời điểm ghi |
| `tenant_id` | `uuid` | ✘ | Đơn vị mà dòng nhật ký thuộc về (§3.7). Thao tác xuyên đơn vị ghi hai dòng, mỗi dòng một đơn vị — điểm 4 dưới |
| `occurred_at` | `timestamptz` | ✘ | |
| `actor_user_id` | `uuid` | ✔ | `null` khi hành động do hệ thống (job nền, lệnh bootstrap) |
| `actor_tenant_id` | `uuid` | ✔ | Đơn vị của người thực hiện **khi khác** `tenant_id`. `null` ở mọi dòng khác — giá trị khác `null` chính là dấu **xuyên đơn vị** |
| `actor_display` | `text` | ✘ | Tên hiển thị **tại thời điểm ghi**; `system` khi không có người |
| `action_code` | `text` | ✘ | Mã ổn định, khuôn `<tài nguyên>.<hành động>` — cùng khuôn với khoá quyền (§5.2) |
| `target_type` | `text` | ✘ | Loại đối tượng, ví dụ `core.user` |
| `target_id` | `text` | ✘ | `text` chứ không `uuid` — đối tượng của module có thể không mang khoá UUID |
| `target_display` | `text` | ✔ | Nhãn của đối tượng **tại thời điểm ghi** |
| `before_value` | `jsonb` | ✔ | Chỉ với thay đổi quan trọng |
| `after_value` | `jsonb` | ✔ | |
| `ip_address` | `inet` | ✔ | IP **sau** bước chuyển tiếp của proxy — [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3.1 |
| `trace_id` | `text` | ✔ | Nối với log vận hành |

- **PK:** `pk_audit_log (id)`
- **FK:** `fk_audit_log_tenant_id → core.tenant (id) ON DELETE RESTRICT` ·
  `fk_audit_log_actor_tenant_id → core.tenant (id) ON DELETE RESTRICT`
- **Index:** `ix_audit_log_tenant_occurred (tenant_id, occurred_at DESC)` · `ix_audit_log_tenant_target (tenant_id, target_type, target_id)`

Bốn điểm khác mọi bảng `core` còn lại:

1. **Không có năm cột audit và không xoá mềm** (§3.2, §3.3). Bảng **chỉ ghi thêm**; `occurred_at`
   và `actor_*` đã là thông tin audit của chính nó.
2. **Tài khoản DB của ứng dụng chỉ có `INSERT` và `SELECT`** trên bảng này — không `UPDATE`,
   không `DELETE`. Bất biến được ép bằng quyền của DB, không bằng quy ước: luật M13, bảng quyền
   và câu nghiệm thu ở [`script-runbook.md`](script-runbook.md) §3.6.
3. **Không FK tới `app_user`.** Người thực hiện có thể bị đổi tên hay xoá mềm; dòng nhật ký
   phải còn nguyên nghĩa. Đó là lý do lưu `actor_display`.
4. **Thao tác xuyên đơn vị ghi hai dòng**
   ([`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)).
   Dòng ở đơn vị hệ thống: `tenant_id` = đơn vị hệ thống, `actor_tenant_id` rỗng — nhật ký của
   người vận hành. Dòng ở đơn vị đích: `tenant_id` = đơn vị đích, `actor_tenant_id` = đơn vị hệ
   thống. Hai dòng nối nhau bằng `trace_id`. `actor_user_id` của dòng thứ hai trỏ một người mà bộ
   lọc đơn vị giấu khỏi người đọc ở đơn vị đích — nên `actor_display` là thứ họ đọc được.

---

### 9.5 `core.setting` — cấu hình theo đơn vị

> **Bảng của một thành phần Nhóm B** — chỉ tạo khi thành phần được bật. Cơ chế, thứ tự ưu tiên và
> ba ràng buộc: [`../wiki-core/be/19-cau-hinh-theo-don-vi.md`](../wiki-core/be/19-cau-hinh-theo-don-vi.md).

| Cột | Kiểu | Ghi chú |
| --- | --- | --- |
| `id` | `uuid` | §3.1 |
| `tenant_id` | `uuid` **null** | Rỗng ⇒ phạm vi hệ thống. Có giá trị ⇒ phạm vi đơn vị hoặc người dùng. **Miễn trừ có tên của luật `NOT NULL` ở §3.7** — khai ở §1.3 kèm nghĩa vụ đi cùng |
| `scope` | `text` | `system` · `tenant` · `user` |
| `scope_id` | `uuid` **null** | Id người dùng khi `scope = user`; rỗng ở hai phạm vi kia |
| `key` | `text` | Mã khoá do module khai |
| `value` | `text` | Lưu dạng chuỗi; kiểu thật nằm ở khai báo khoá |
| Năm cột audit | | §3.2 |

- **Unique:** `(tenant_id, scope, scope_id, key)` — kèm mệnh đề xoá mềm theo §3.3.
- **FK:** `tenant_id → core.tenant (id) ON DELETE RESTRICT`.

Không lưu bí mật hạ tầng ở bảng này — lý do ở file cơ chế §5.

### 9.6 `core.code_sequence` — bộ đếm sinh mã nghiệp vụ

> **Bảng của một thành phần Nhóm B** — chỉ tạo khi thành phần được bật. Khuôn sinh mã và ba bẫy:
> [`../wiki-core/be/20-sinh-ma-nghiep-vu.md`](../wiki-core/be/20-sinh-ma-nghiep-vu.md).

| Cột | Kiểu | Ghi chú |
| --- | --- | --- |
| `id` | `uuid` | §3.1 |
| `tenant_id` | `uuid` | Mỗi đơn vị đếm riêng |
| `code_type` | `text` | Loại mã: phiếu, văn bản, hồ sơ… |
| `period` | `text` | Kỳ đếm, thường là năm. Chuỗi để khuôn kỳ đổi được mà không đổi kiểu cột |
| `current_value` | `bigint` | Giá trị đã phát gần nhất |
| Bốn cột audit — **không** `is_deleted` | | §3.2, bảng *"Bảng KHÔNG mang khối này"* |

- **Unique:** `(tenant_id, code_type, period)` — không mệnh đề xoá mềm: bảng này **không xoá mềm**.
- Kiểu ánh xạ bảng này, nếu có, **không** kế thừa `BaseEntity`: luật E3 buộc mọi hậu duệ `BaseEntity` mang bộ lọc xoá mềm ([`../RULES.md`](../RULES.md) §4). Nó implement trực tiếp interface audit để interceptor điền bốn cột audit — [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §1.4.
- **FK:** `tenant_id → core.tenant (id) ON DELETE RESTRICT`.

Bảng chỉ được đọc-ghi bằng **một** câu lệnh cập nhật-và-trả-về. Đọc rồi ghi bằng hai câu là bẫy thứ hai ở file cơ chế §3.

### 9.7 `core.file` — tệp đính kèm — định nghĩa gốc

> **Bảng thuộc v1**, tạo ở pha B4 ([`../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)). Cơ chế: [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md); hợp đồng: [`../contracts/files.md`](../contracts/files.md).

| Cột | Kiểu | Null | Ghi chú |
| --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | §3.1 |
| `tenant_id` | `uuid` | NOT NULL | §3.7 |
| `owner_table` | `varchar(100)` | NULL | Bảng của bản ghi chủ, dạng `schema.table`. NULL tới khi tệp được gắn vào bản ghi chủ ([`../luong/N2-dinh-kem-tep.md`](../luong/N2-dinh-kem-tep.md) bước 5) |
| `owner_id` | `uuid` | NULL | Id bản ghi chủ — id trần, **không** FK (§1.1) |
| `original_name` | `varchar(255)` | NOT NULL | Tên người dùng đặt; chỉ để hiển thị và đặt tên lúc tải về |
| `content_type` | `varchar(150)` | NOT NULL | Kiểm bằng nội dung, không theo đuôi tệp |
| `size_bytes` | `bigint` | NOT NULL | |
| `storage_key` | `varchar(300)` | NOT NULL | Khoá trong kho lưu, hệ thống sinh — **không** phải đường dẫn tuyệt đối, không bao giờ trả ra ngoài |
| `purpose` | `varchar(50)` | NULL | Mục đích lưu do module khai ([`../contracts/files.md`](../contracts/files.md) §1); quyết định thư mục đích và giới hạn áp dụng |
| Năm cột audit | | | §3.2 — kế thừa `BaseEntity`, xoá mềm |

- **PK:** `pk_file (id)`
- **FK:** `fk_file_tenant_id → core.tenant (id) ON DELETE RESTRICT`
- **Index:** `ix_file_owner (tenant_id, owner_table, owner_id)`
- **Unique:** `uq_file_storage_key (tenant_id, storage_key) WHERE is_deleted = false` — gồm `tenant_id` theo luật M3, mệnh đề xoá mềm theo §3.3

Quyền đọc tệp là quyền đọc **bản ghi chủ** ([`../contracts/files.md`](../contracts/files.md) §4); bộ lọc đơn vị áp như mọi bảng có `tenant_id`. Mức gắn tệp vào bản ghi chủ: [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §3.1.

### 9.8 `core.job` — việc chạy nền — định nghĩa gốc

> **Bảng thuộc v1**, tạo ở pha B4 ([`../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)). Khuôn `jobId + polling`: [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §10.3; hợp đồng: [`../contracts/jobs.md`](../contracts/jobs.md); khoá cấu hình: [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §6.

| Cột | Kiểu | Null | Ghi chú |
| --- | --- | --- | --- |
| `id` | `uuid` | NOT NULL | §3.1 — chính là `jobId` trả cho FE |
| `tenant_id` | `uuid` | NOT NULL | §3.7 |
| `type` | `varchar(50)` | NOT NULL | Loại việc, do luồng khởi tạo đặt |
| `status` | `varchar(20)` | NOT NULL | `queued` · `running` · `succeeded` · `failed` · `cancelled` |
| `created_by_user_id` | `uuid` | NOT NULL | Người khởi tạo — id trần, **không** FK; handler kiểm *"việc của chính người gọi"* bằng cột này |
| `started_at` | `timestamptz` | NULL | |
| `finished_at` | `timestamptz` | NULL | |
| `progress` | `smallint` | NOT NULL | 0–100, mặc định `0` |
| `result` | `jsonb` | NULL | Hình dạng theo `type`; chỉ có khi `succeeded` |
| `error` | `jsonb` | NULL | `{ code, message, messageParams }`; chỉ có khi `failed` |
| `result_file_id` | `uuid` | NULL | Tệp kết quả (ví dụ danh sách dòng lỗi) — tải qua [`../contracts/files.md`](../contracts/files.md) §2 |
| Năm cột audit | | | §3.2 — kế thừa `BaseEntity`, xoá mềm |

- **PK:** `pk_job (id)`
- **FK:** `fk_job_tenant_id → core.tenant (id) ON DELETE RESTRICT` · `fk_job_result_file_id → core.file (id) ON DELETE RESTRICT`
- **Check:** `ck_job_status CHECK (status IN ('queued','running','succeeded','failed','cancelled'))` · `ck_job_progress CHECK (progress BETWEEN 0 AND 100)`
- **Index:** `ix_job_tenant_created_at (tenant_id, created_at)`

Việc nền chạy ngoài request: đơn vị và người kích hoạt đi theo dòng outbox (§8) — [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1.

---

## 10. Thêm bảng mới — vào schema nào

Cùng câu hỏi đã dùng để quyết `Core.*` hay `Modules.*` ở tầng code
([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4):

> Bảng này có ý nghĩa với **mọi** sản phẩm dựng trên nền tảng không?

Có → schema `core`. Không → schema của module. Ngưỡng định lượng vẫn là **hai module trở lên cần
nó** mới nâng lên `core`.

**Câu hỏi thứ hai, hỏi ngay sau câu trên:** *dữ liệu trong bảng này có thuộc về một đơn vị cụ thể
không?* Có → bảng mang `tenant_id` theo §3.7. Không → nó phải được **thêm vào danh sách miễn trừ
ở §1.3 kèm lý do**, và đó là một quyết định kiến trúc, không phải một dòng bỏ qua.

Luật E4 ép điều này bằng ArchTest `EveryMappedEntity_LivesInTheSchemaOfItsSide`. Mỗi bên một `DbContext` ([`migration-policy.md`](migration-policy.md) §1.2), mỗi `DbContext` đặt schema mặc định của **chính bên mình**; E4 bắt ca một entity `core` bị đăng ký nhầm vào `DbContext` của module.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §10

---

## 11. Kiểm sau khi áp schema

Các truy vấn dưới là phép nghiệm thu, chạy sau **mỗi** lần dựng lại database. Chúng kiểm đúng
những thứ hay sai nhất, và mỗi câu có "mong đợi" cụ thể để đối chiếu.

```sql
-- (1) public phải RỖNG.  Mong đợi: 0 dòng.
SELECT tablename FROM pg_tables WHERE schemaname = 'public';

-- (2) Mọi bảng core kế thừa BaseEntity phải đủ 5 cột audit.
--     Mong đợi: cột "thieu" đều là {} (mảng rỗng).
--     Danh sách chỉ gồm bảng có từ script schema đầu tiên. Bảng tạo ở pha sau (§7)
--     thêm vào đây CÙNG LƯỢT với script tạo nó — thêm sớm thì câu này đỏ oan.
SELECT t.tbl AS "bang",
       ARRAY(SELECT c FROM unnest(ARRAY['created_at','created_by','updated_at','updated_by','is_deleted']) c
             WHERE NOT EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_schema = 'core'
                                 AND table_name  = t.tbl
                                 AND column_name = c)) AS "thieu"
FROM (VALUES ('permission'), ('permission_resource'), ('role_permission'),
             ('menu_item'), ('menu_item_role')) AS t(tbl)
ORDER BY 1;

-- (3) Mọi unique index trên bảng có is_deleted phải LỌC theo is_deleted.
--     Mong đợi: "co_loc" = true ở mọi dòng, TRỪ đúng một ngoại lệ đã khai:
--     uq_permission_resource_key (§5.2) — nó không lọc, có chủ đích.
--
--     Lọc theo THUỘC TÍNH (unique + bảng có cột is_deleted), KHÔNG theo tên: lọc theo
--     tên thì mọi index đặt tên lệch quy ước vô hình với câu kiểm — câu kiểm tin vào
--     chính quy ước mà nó phải kiểm.
--
--     Khoá chính bị loại bằng pg_index.indisprimary: PK là index UNIQUE nhưng không bao
--     giờ mang vị từ, nên không loại thì mọi pk_* hiện "co_loc" = false.
WITH ung_vien AS (
    SELECT i.indexname,
           (i.indexdef LIKE '%is_deleted%') AS co_loc
    FROM pg_indexes i
    WHERE i.schemaname = 'core'
      AND i.indexdef LIKE '%UNIQUE%'
      AND NOT EXISTS (
            SELECT 1 FROM pg_index x
            WHERE x.indexrelid = format('%I.%I', i.schemaname, i.indexname)::regclass
              AND x.indisprimary)
      AND EXISTS (
            SELECT 1 FROM information_schema.columns c
            WHERE c.table_schema = i.schemaname
              AND c.table_name   = i.tablename
              AND c.column_name  = 'is_deleted')
)
SELECT indexname, co_loc FROM ung_vien
UNION ALL
-- Chốt tập đầu vào: bộ lọc trên khớp 0 index thì câu này đang không kiểm gì, mà
-- màn hình vẫn "sạch". Dòng sau là thứ duy nhất phân biệt hai trạng thái đó.
SELECT 'KHONG CO UNIQUE INDEX NAO TREN BANG CO is_deleted', NULL
WHERE NOT EXISTS (SELECT 1 FROM ung_vien)
ORDER BY 2, 1;

-- (4) KHÔNG có foreign key nào vượt ranh giới schema (luật E5).  Mong đợi: 0 dòng.
SELECT con.conname, ns_src.nspname AS tu_schema, ns_dst.nspname AS toi_schema
FROM pg_constraint con
JOIN pg_class     c_src  ON c_src.oid  = con.conrelid
JOIN pg_namespace ns_src ON ns_src.oid = c_src.relnamespace
JOIN pg_class     c_dst  ON c_dst.oid  = con.confrelid
JOIN pg_namespace ns_dst ON ns_dst.oid = c_dst.relnamespace
WHERE con.contype = 'f'
  AND ns_src.nspname <> ns_dst.nspname;

-- (5) Mọi bảng core phải có tenant_id, TRỪ danh sách miễn trừ ở §1.3.
--     Mong đợi: 0 dòng. Một dòng lạ ở đây là một bảng dữ liệu tenant chưa được cách ly.
SELECT t.table_name
FROM   information_schema.tables t
WHERE  t.table_schema = 'core'
  AND  t.table_type   = 'BASE TABLE'
  AND  t.table_name NOT IN ('tenant', 'permission', 'permission_resource',
                            'schema_script_history', '__ef_migrations_history',
                            'data_protection_key')
  AND  NOT EXISTS (SELECT 1 FROM information_schema.columns c
                   WHERE c.table_schema = 'core'
                     AND c.table_name   = t.table_name
                     AND c.column_name  = 'tenant_id');

-- (6) Mọi index DUY NHẤT trên bảng có tenant_id phải GỒM tenant_id (luật M3).
--     Mong đợi: "co_tenant" = true ở mọi dòng.
--     Khoá chính bị loại bằng pg_index.indisprimary — cùng lý do câu (3): pk_* là khoá
--     thay thế trên id, không phải khoá nghiệp vụ.
WITH ung_vien AS (
    SELECT i.indexname, (i.indexdef LIKE '%tenant_id%') AS co_tenant
    FROM   pg_indexes i
    WHERE  i.schemaname = 'core'
      AND  i.indexdef LIKE '%UNIQUE%'
      AND  NOT EXISTS (
             SELECT 1 FROM pg_index x
             WHERE x.indexrelid = format('%I.%I', i.schemaname, i.indexname)::regclass
               AND x.indisprimary)
      AND  EXISTS (SELECT 1 FROM information_schema.columns c
                   WHERE c.table_schema = 'core'
                     AND c.table_name   = i.tablename
                     AND c.column_name  = 'tenant_id')
)
SELECT indexname, co_tenant FROM ung_vien
UNION ALL
-- Chốt tập đầu vào: cùng lý do câu (3).
SELECT 'KHONG CO UNIQUE INDEX NAO TREN BANG CO tenant_id', NULL
WHERE NOT EXISTS (SELECT 1 FROM ung_vien)
ORDER BY 1;
```

Câu (3) và (6) có nhánh **chốt tập đầu vào**: thấy dòng `KHONG CO UNIQUE INDEX NAO…` là **đỏ**, không phải xanh.

Danh sách miễn trừ trong câu (5) là bản sao của bảng ở §1.3 — **thêm một bảng vào một chỗ thì phải
thêm vào chỗ kia cùng lượt**. Đây là chỗ duy nhất trong file này danh sách đó xuất hiện hai lần.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`schema-core.md`](../wiki-core/be/ly-do/schema-core.md) §11
