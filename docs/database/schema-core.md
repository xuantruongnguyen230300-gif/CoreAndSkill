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

`public` rỗng là một quy ước có ích: nhìn cây schema trong pgAdmin/DBeaver là biết ngay bảng
nào thuộc nền tảng, bảng nào thuộc nghiệp vụ, không phải đọc code. Rẻ khi làm từ lúc DB còn
trống, đắt khi tách sau.

Luật ranh giới ở tầng code: [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §5.

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
`NoForeignKey_CrossesSchemaBoundary`.

**Cái giá phải trả, nói thẳng:** database **không còn** đảm bảo `nguoi_tao_id` trỏ tới một user
có thật. Xoá một user để lại id mồ côi trong bảng module, và không có lỗi nào bật ra. Đổi lại,
mỗi module tách thành service độc lập được mà **không phải sửa schema** — một FK xuyên schema là
sợi dây trói vĩnh viễn, gỡ nó trên dữ liệu thật là việc đắt nhất trong vòng đời hệ thống.

Bù lại phần mất: user **không bị xoá cứng** (§1.2), nên mọi id người dùng mà module đang giữ vẫn
tra ngược được. Id mồ côi trên thực tế chỉ xuất hiện khi có người `DELETE` bằng SQL tay.

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
| `code` | `varchar(50)` | NOT NULL | — | **Mã đơn vị người dùng gõ ở form đăng nhập.** Chuẩn hoá về chữ HOA ở tầng ứng dụng trước khi ghi và trước khi tra — cùng lý do Identity chuẩn hoá tên đăng nhập |
| `name` | `varchar(200)` | NOT NULL | — | Tên đơn vị, hiển thị |
| `is_active` | `boolean` | NOT NULL | `true` | Sai ⇒ **mọi tài khoản của đơn vị đó không đăng nhập được** |
| `is_system` | `boolean` | NOT NULL | `false` | **Đơn vị hệ thống** — nơi tài khoản vận hành trú ([`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md)). Thiếu cột này thì mã lỗi `CORE.TENANT.SYSTEM_IMMUTABLE` ở [`../contracts/tenants.md`](../contracts/tenants.md) **không ai viết nổi** |
| `created_at` · `created_by` · `updated_at` · `updated_by` | như §3.2 | NULL | — | Điền tay ở tầng service, xem dưới |

- **PK:** `pk_tenant (id)`
- **Unique constraint:** `uq_tenant_code (code)` — unique **đầy đủ**, không lọc
- **Unique index một phần:** `ux_tenant_is_system` trên `(is_system) WHERE is_system` — cho phép **nhiều nhất một** đơn vị hệ thống. Luật **M10**; database ép, không phải test.

> **`Tenant` KHÔNG kế thừa `BaseEntity`, và đó là chủ đích** — cùng khuôn với `AppUser` (§4.1).
> `BaseEntity` mang theo `is_deleted`, mà **tenant không bao giờ được xoá**
> ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10). Cho nó một
> cờ xoá mềm là mở đúng cánh cửa vừa đóng: một dòng `is_deleted = true` làm cả đơn vị biến mất
> khỏi mọi màn hình trong khi dữ liệu của họ vẫn nằm nguyên trong mọi bảng — và hai cơ chế vô
> hiệu hoá song song (`is_deleted` và `is_active`) thì ngày chúng bất đồng không cơ chế nào sai
> rõ ràng để mà sửa.
>
> Hệ quả: bốn cột audit ở đây **điền tay** ở tầng service, không qua interceptor — interceptor
> chỉ chạm hậu duệ `BaseEntity`. Đây là ngoại lệ thứ hai cùng khuôn với §4.1.

> 🪤 **`is_active` của `tenant` KHÔNG liên quan gì tới hậu tố `_active` của tên index** (§2.3).
> Hậu tố đó là hợp đồng *"index này có `WHERE is_deleted = false`"*. Bảng `tenant` không có
> `is_deleted`, nên khoá duy nhất của nó mang tiền tố `uq_`, không phải `ux_…_active`.

#### Danh sách miễn trừ — bảng KHÔNG mang `tenant_id`

Mỗi mục dưới đây là **dữ liệu dùng chung toàn hệ**, không thuộc đơn vị nào. Luật M4
(`EveryBusinessEntity_IsTenantScoped_OrExempt`) đọc đúng danh sách này.

| Bảng | Vì sao miễn trừ |
| --- | --- |
| `core.tenant` | Là chính danh tính tenant. Một bảng tự trỏ vào mình bằng `tenant_id` thì không còn gốc để bắt đầu |
| `core.permission` | **Danh mục** quyền là hợp đồng giữa code và dữ liệu: `[RequirePermission("core.user.read")]` phải khớp một dòng, và chuỗi đó nằm trong Core chứ không thuộc đơn vị nào. Cho mỗi tenant một bản sao nghĩa là mỗi tenant có thể có một danh mục **lệch**, và một endpoint sẽ từ chối ở đơn vị này mà cho qua ở đơn vị kia. Thứ **thuộc tenant** là ánh xạ vai trò → quyền (`role_permission`), không phải danh mục |
| `core.permission_resource` | Cùng lý do — nó là trục hàng của ma trận phân quyền, do lập trình viên và module khai, không do người dùng tạo |
| `core.schema_script_history` | Nhật ký **vận hành của database**, không của một đơn vị. Một script chạy một lần cho cả bản cài |
| `core.__ef_migrations_history` | Của EF, cùng lý do trên. Không đụng vào |
| `core.data_protection_key` (§9.3) | Khoá ký và mã hoá phiếu xác thực là của **bản cài**. Tách theo tenant thì một instance không giải được phiếu của tenant khác dù cùng tiến trình — và mọi tenant vẫn dùng chung tiến trình đó |

> 🛑 **Danh sách này phải NGẮN. Một danh sách miễn trừ dài là dấu hiệu ranh giới tenant đang bị
> hiểu sai** — thường là ai đó gặp một bảng khó gắn `tenant_id` rồi miễn trừ nó thay vì hỏi tại
> sao nó khó. Thêm một dòng vào bảng trên là một quyết định kiến trúc: nó phải nêu lý do **dữ
> liệu này có ý nghĩa như nhau với mọi đơn vị**, không phải lý do *"thêm cột vào đây phiền"*.

**Mọi bảng `core` khác đều mang `tenant_id`** — bảy bảng Identity, ba bảng phân quyền (trừ hai
danh mục ở trên), hai bảng menu, hai bảng thông báo, `outbox_message`, và `audit_log`. Quy ước cột ở §3.7.

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

**Vì sao snake_case chứ không PascalCase như dự án tiền nhiệm.** PostgreSQL hạ mọi định danh
không trích dẫn về chữ thường. Đặt tên `"AspNetUsers"` buộc **mọi** câu SQL viết tay phải trích
dẫn kép — quên một dấu nháy là `relation "aspnetusers" does not exist`, một lỗi vừa khó đọc vừa
chỉ lộ ra lúc chạy. Ở dự án tiền nhiệm, mọi script vận hành đều phải viết `core."RolePermissions"`.
snake_case gỡ hẳn lớp phiền đó: `SELECT * FROM core.role_permission` chạy đúng như đã gõ.

**Vì sao số ít.** Một dòng là một thực thể; `app_user` đọc là *"bảng của thực thể app_user"*.
Số ít cũng tránh phải quyết định số nhiều bất quy tắc (`person`/`people`, `status`/`statuses`) —
quyết định mà mỗi người sẽ tự trả lời khác nhau.

> ⚠️ **ASP.NET Core Identity đặt tên PascalCase số nhiều (`AspNetUsers`).** Ta **đổi tên tường
> minh** trong EF configuration, xem §4.0. Đây là thao tác bắt buộc, không phải tuỳ chọn: bỏ qua
> nó thì bảy bảng Identity trở thành ngoại lệ duy nhất của quy ước, và ngoại lệ duy nhất là thứ
> không ai nhớ.

#### Cơ chế ánh xạ, và một bẫy đi kèm

Property C# giữ PascalCase (`CreatedAt`); **cột vật lý** là snake_case (`created_at`). Ánh xạ do
một naming convention áp cho toàn model, khai **một lần** khi cấu hình `DbContext` — không phải
`HasColumnName(...)` gõ tay ở từng property, vì cách đó là một danh sách sẽ thiếu.

> 🪤 **Mọi mảnh SQL THÔ trong EF configuration phải dùng tên VẬT LÝ, không phải tên property.**
> Naming convention chỉ dịch tên property; nó **không** đụng tới chuỗi ta tự viết. Cụ thể ở
> mệnh đề lọc của unique index:
>
> ```csharp
> builder.HasIndex(x => x.Code).IsUnique().HasFilter("is_deleted = false");   // ĐÚNG
> builder.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false"); // SAI ở repo này
> ```
>
> Dòng thứ hai là khuôn đúng cho một model **không** đổi tên cột; ở đây nó sinh SQL trỏ vào một
> cột không tồn tại. Migration sẽ đỏ ngay lúc áp, nên bẫy này ồn ào — nhưng vẫn tốn một vòng nếu
> chép mẫu từ nơi khác mà không đọc.
>
> Cùng lý do, mọi `migrationBuilder.Sql(...)` viết tay dùng tên vật lý.

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

> 🪤 **Bẫy: PostgreSQL cắt định danh ở 63 byte, âm thầm.** Không lỗi, không cảnh báo — tên bị
> cụt. Hai index dài mà 63 byte đầu trùng nhau sẽ va vào nhau với thông báo *"relation already
> exists"* trỏ vào một cái tên bạn chưa từng gõ. Kiểm trước khi đặt tên dài:
>
> ```sql
> SELECT length('ux_notification_recipient_notif_user_active');
> ```

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

Ba quyết định gói trong một dòng:

| Quyết định | Vì sao |
| --- | --- |
| Kiểu `uuid`, không `bigserial` | Id sinh được ở phía ứng dụng **trước khi** chạm DB, nên gộp nhiều insert vào một transaction mà vẫn nối được quan hệ cha–con. Cũng là điều kiện để tách module thành service mà không phải điều phối dãy số |
| **Không** `DEFAULT gen_random_uuid()` | Ứng dụng tự sinh (`Guid.CreateVersion7()`). Để DB sinh thì EF phải đọc ngược giá trị sau insert, và entity con thêm vào một collection đã tracked bị EF hiểu nhầm là "đã tồn tại" |
| **UUID v7**, không v4 | v7 có tiền tố thời gian nên **tăng dần**. v4 ngẫu nhiên hoàn toàn làm mỗi insert rơi vào một trang B-tree khác nhau — index phình, cache miss cao, và chi phí đó không bao giờ giảm |

**Ngoại lệ đã biết, đừng "sửa cho đồng bộ":**

| Bảng | Kiểu `id` | Vì sao |
| --- | --- | --- |
| `app_user_claim`, `app_role_claim` | `integer GENERATED BY DEFAULT AS IDENTITY` | Schema chuẩn của Identity. Sửa nó không mua được gì và làm bản nâng cấp Identity sau này khó hơn |
| `schema_script_history` | `bigint GENERATED ALWAYS AS IDENTITY` | Bảng vận hành, cần **thứ tự áp dụng** đọc được bằng mắt. Một id tăng dần trả lời thẳng câu *"script nào chạy trước"* |

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
| `created_at` / `created_by` | Interceptor EF, lúc `Added` | `created_by` giữ **tên đăng nhập**, không phải id — để đọc log không phải join |
| `updated_at` / `updated_by` | Interceptor EF, lúc `Added` **và** `Modified` | Điền ngay từ lúc tạo (bằng đúng giá trị của `created_*`), **không** để `NULL`. Xoá mềm cũng là `Modified`, nên hai cột này trả lời luôn *"ai xoá, lúc nào"* |

> **Vì sao `updated_*` được điền ngay lúc tạo.** Để `NULL` thì mọi chỗ hiển thị *"sửa lần cuối"*
> phải tự viết `updated_at ?? created_at`, và mọi câu `ORDER BY updated_at DESC` đẩy bản ghi mới
> tạo xuống cuối. Bù lại, phép thử *"bản ghi này đã từng bị sửa chưa"* không còn là `updated_at IS
> NOT NULL` mà là `updated_at <> created_at` — nên interceptor phải gán **cùng một** giá trị thời
> gian cho cả hai, không phải gọi đồng hồ hai lần cách nhau vài mili giây.
>
> Chi tiết interceptor: [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md).
| `is_deleted` | Ứng dụng | `NOT NULL DEFAULT false` — cột nullable làm mọi query filter phải xử lý ba trạng thái thay vì hai |

Luật E1 ([`../RULES.md`](../RULES.md) §4) cho phép **đúng năm** field audit có public setter.
Detector của nó đếm đúng năm tên trên.

**Vì sao KHÔNG có `deleted_at` / `deleted_by`.** Chúng trả lời đúng câu mà `updated_at`/`updated_by`
đã trả lời tại thời điểm xoá mềm, vì xoá mềm là một lần `UPDATE`. Thêm hai cột nữa là thêm hai
cột phải giữ đồng bộ và hai cột có thể lệch. Dự án nào thật sự cần tách *"lần sửa cuối"* khỏi
*"lần xoá"* thì thêm — nhưng phải nới detector E1 lên bảy tên **cùng lượt**, nếu không cổng đỏ
ở một chỗ không liên quan gì tới thay đổi vừa làm.

**Bảng KHÔNG mang khối này** — mỗi ca đều có lý do, không phải bỏ sót:

| Bảng | Vì sao |
| --- | --- |
| Bảy bảng Identity | Identity tự quản vòng đời (`lockout_end`, `security_stamp`). Xem §4.1 |
| `tenant` | Có bốn cột audit nhưng **không** có `is_deleted`: tenant không bao giờ bị xoá. Xem §1.3 |
| `outbox_message` | Hàng đợi vận hành, dòng bị dọn theo chính sách lưu trữ chứ không xoá mềm. Xem §8 |
| `audit_log` | Chỉ ghi thêm — sửa hay xoá đều là phá bằng chứng. Xem §9.4 |
| `schema_script_history` | Nhật ký chỉ-ghi-thêm. Xoá mềm một dòng nhật ký là tự nói dối. Xem §9.1 |
| `__ef_migrations_history` | Của EF, không đụng vào. Xem §9.2 |

### 3.3 🪤 Soft delete phá unique index — bẫy thật, đọc kỹ

**Đây là cái bẫy đắt nhất của cả file này.**

Có `is_deleted`, một unique index viết theo thói quen sẽ hỏng:

```sql
-- SAI
CREATE UNIQUE INDEX ux_menu_item_code ON core.menu_item (code);
```

Kịch bản hỏng, đủ ba bước:

1. Tạo menu `code = 'quan-tri'`. Thành công.
2. Xoá mềm nó — `UPDATE core.menu_item SET is_deleted = true WHERE code = 'quan-tri'`.
   Dòng **vẫn nằm trong bảng**.
3. Tạo lại menu `code = 'quan-tri'` → `23505 duplicate key value violates unique constraint`.

Người dùng thấy: *"Tôi vừa xoá nó xong mà, sao bảo trùng?"* — và không cách nào tự gỡ, vì thứ
đang chiếm khoá là một dòng không màn hình nào hiển thị.

**Cách đúng — unique index MỘT PHẦN:**

```sql
-- ĐÚNG
CREATE UNIQUE INDEX ux_menu_item_code_active
    ON core.menu_item (code)
    WHERE is_deleted = false;
```

Ba hệ quả bắt buộc phải nhớ, mỗi cái đều từng làm hỏng việc ở dự án tiền nhiệm:

1. **Khoá chính không bao giờ là khoá nghiệp vụ.** `role_permission` có PK đơn `id`; tính duy
   nhất của cặp `(role_id, permission_id)` hạ xuống thành unique index một phần. PK ghép cấm
   đúng điều ta cần cho phép: **cấp lại một quyền đã từng thu hồi**.
2. **`ON CONFLICT` phải lặp lại NGUYÊN VĂN vị từ.** Postgres chỉ nhận index một phần làm đích
   khi mệnh đề `WHERE` khớp đúng. Thiếu nó, câu lệnh **abort** với *"there is no unique or
   exclusion constraint matching the ON CONFLICT specification"* — một script tự nhận là
   "idempotent" chết trước khi ghi được dòng nào:

   ```sql
   INSERT INTO core.role_permission (id, role_id, permission_id, is_deleted, created_at, created_by)
   SELECT gen_random_uuid(), r.id, p.id, false, now(), 'system'
   FROM   core.app_role r
   CROSS  JOIN core.permission p
   WHERE  r.is_system = true
   ON CONFLICT (role_id, permission_id) WHERE is_deleted = false DO NOTHING;
   ```
3. **Hàm băm phiên bản ma trận không được `IgnoreQueryFilters()`.** Nếu token phiên bản ở
   [`../contracts/permissions.md`](../contracts/permissions.md) tính cả dòng đã xoá mềm, nó đổi
   giá trị sau mỗi lần lưu kể cả khi trạng thái nhìn thấy được không đổi — và hai người lưu
   **tuần tự** sẽ nhận 409 sai.

**Hai phép thử phải có trong integration test** (luật T2, chạy trên Postgres thật):

- Xoá mềm một `code` rồi tạo lại đúng `code` đó **phải thành công** — bắt ca thiếu `WHERE`.
- Hai dòng cùng `code` cùng chưa xoá **phải** bị chặn `23505` — bắt ca gỡ nhầm cả index.

Một mình ca thứ nhất không đủ: gỡ hẳn unique index cũng làm nó xanh.

### 3.4 Thời gian: `timestamptz`, không bao giờ `timestamp`

```sql
created_at timestamptz NULL      -- ĐÚNG
created_at timestamp   NULL      -- CẤM
```

`timestamp without time zone` lưu một con số **không có nghĩa** nếu không biết nó thuộc múi giờ
nào. Hai tiến trình chạy khác `TimeZone` ghi cùng một khoảnh khắc thành hai giá trị khác nhau,
và không có cách nào phát hiện sau đó. `timestamptz` chuẩn hoá về UTC lúc ghi, đổi về múi giờ
phiên lúc đọc — đây là kiểu duy nhất đúng cho một hệ có thể chạy nhiều instance, hoặc có người
dùng ở nhiều múi giờ.

Hệ quả cho code: mọi mốc thời gian sinh bằng `DateTimeOffset.UtcNow`, không `DateTime.Now`.

### 3.5 `text` hay `varchar(n)`

PostgreSQL lưu hai kiểu này **giống hệt nhau**; `varchar(n)` chỉ thêm một phép kiểm độ dài.
Không có chênh lệch hiệu năng. Vì vậy chọn theo **ý nghĩa**:

| Dùng | Khi nào | Ví dụ |
| --- | --- | --- |
| `varchar(n)` | `n` là **luật nghiệp vụ** mà FE cũng phải biết (`maxlength` của ô nhập, độ rộng cột lưới) | `code varchar(100)`, `full_name varchar(200)` |
| `text` | Không có giới hạn nghiệp vụ nào, hoặc giới hạn do thư viện quyết | `password_hash text`, `last_error text` |

> 🪤 Nới `varchar(50)` → `varchar(100)` là `ALTER TABLE` rẻ (Postgres không rewrite bảng).
> **Siết lại thì không** — phải quét toàn bảng, và sẽ hỏng nếu có dòng vượt giới hạn mới. Chọn
> rộng hơn một bậc so với nhu cầu hôm nay.

### 3.6 Concurrency token — hệ quả trên schema

> 📖 **File chủ của chủ đề concurrency là**
> [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §6 — cách khai property,
> kiểu CLR bắt buộc, và bẫy provider. Mục này chỉ ghi phần **chạm tới schema**, không chép lại
> recipe.

Hệ quả duy nhất trên schema: **không có cột `row_version` nào trong `core`.** Token dùng cột
hệ thống `xmin` mà PostgreSQL đã có sẵn cho mọi dòng, nên nó không xuất hiện trong DDL, không
xuất hiện trong `information_schema.columns`, và không cần migration riêng.

Hai điều kéo theo, cần biết khi đọc schema:

| | |
| --- | --- |
| Đối soát schema | Đừng đi tìm một cột phiên bản trong bảng nào cả. Không thấy **là đúng** |
| Bảng Identity | `app_user` và `app_role` đã có `concurrency_stamp` do Identity quản. **Không** thêm token thứ hai — hai token trên cùng một dòng nghĩa là hai lớp code cùng tin mình đang giữ quyền quyết định, và ca chúng bất đồng không có đường xử lý đúng |

Ma trận quyền dùng cơ chế khác hẳn — token băm cấp **tập hợp**, không phải cấp dòng. Xem
[`../contracts/permissions.md`](../contracts/permissions.md) và
[`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md).

### 3.7 Cột `tenant_id` — áp cho mọi bảng dữ liệu tenant

```sql
tenant_id uuid NOT NULL,
CONSTRAINT fk_menu_item_tenant_id FOREIGN KEY (tenant_id) REFERENCES core.tenant (id)
    ON DELETE RESTRICT
```

| Quyết định | Vì sao |
| --- | --- |
| `NOT NULL`, không mặc định | Cột nullable nghĩa là có trạng thái *"dòng không thuộc đơn vị nào"* — và bộ lọc `tenant_id = @tenant` sẽ **giấu** dòng đó khỏi mọi người, kể cả người vừa tạo. Interceptor điền giá trị, không phải DB |
| Ứng dụng điền, **không** `DEFAULT` | Giá trị đến từ claim của phiếu xác thực, DB không biết ai đang gọi. Chi tiết: [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §2 |
| Không bao giờ `UPDATE` | Một bản ghi không đổi chủ. Interceptor chặn ca `Modified` có `tenant_id` bị sửa |
| FK `ON DELETE RESTRICT` **trong schema `core`** | Tenant không xoá được, và `RESTRICT` biến điều đó thành một ràng buộc DB thay vì một lời hứa |
| **Bảng của module: `tenant_id` là id trần, KHÔNG có FK** | FK sẽ vượt ranh giới schema — vi phạm luật E5 (§1.1). Đây là cùng quy tắc đã áp cho `nguoi_tao_id` |

**Vị trí trong index:** `tenant_id` đứng **đầu** mọi index đa cột trên bảng có nó, vì mọi truy vấn
đều lọc theo tenant trước. Ngoại lệ có chủ đích duy nhất là index của `outbox_message` (§8) — bộ
phát chạy xuyên tenant nên lọc theo `next_attempt_at` trước.

> 🛑 **Mọi index duy nhất trên bảng có `tenant_id` phải GỒM `tenant_id`** — luật M3, ArchTest
> `EveryUniqueIndex_OnTenantScoped_Includes_TenantId`. Thiếu nó thì đơn vị B không tạo được bản
> ghi mang mã mà đơn vị A đã dùng, **và không tìm thấy bản ghi đang chiếm khoá** vì bộ lọc đã
> giấu nó đi. Triệu chứng người dùng báo: *"hệ thống nói mã trùng nhưng tôi tìm không thấy"*.
>
> Cộng với §3.3, một unique index trên bảng vừa có `tenant_id` vừa có `is_deleted` phải gồm **ba**
> thứ: cột nghiệp vụ, `tenant_id`, và mệnh đề `WHERE is_deleted = false`.

---

## 4. Nhóm Identity — bảy bảng

`Core.Infrastructure` giữ `AppUser : IdentityUser<Guid>` và `AppRole : IdentityRole<Guid>`.
Luật S5 ([`../RULES.md`](../RULES.md) §6) cấm hai kiểu này rò ra ngoài `Core.Infrastructure`;
`Core.Application` chỉ thấy `IIdentityService`.

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
> Chúng là tên do Identity khai trong model; đổi chúng thêm một điểm phải đồng bộ tay mỗi lần
> nâng cấp Identity, đổi lại được đúng sự nhất quán về hình thức. Ghi ngoại lệ ở đây để lần sau
> không ai "dọn" chúng rồi phá một bản nâng cấp.
>
> 🛑 **Giữ TÊN không có nghĩa là giữ ĐỊNH NGHĨA.** Cả ba index trên đều được **khai lại với tập
> cột mới** để gồm `tenant_id` (§4.1, §4.2). Tên đứng yên, cột đổi — đây là chỗ dễ đọc lướt rồi
> để nguyên định nghĩa cũ nhất trong cả file.

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
| `phone_number` | `text` | NULL | — | Identity |
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
| `created_at` | `timestamptz` | NULL | — | **Ta thêm** |
| `created_by` | `varchar(100)` | NULL | — | **Ta thêm** |
| `updated_at` | `timestamptz` | NULL | — | **Ta thêm** |
| `updated_by` | `varchar(100)` | NULL | — | **Ta thêm** |

- **PK:** `pk_app_user (id)`
- **FK:** `fk_app_user_tenant_id → core.tenant (id) ON DELETE RESTRICT`
- **Index:** `UserNameIndex` UNIQUE (`tenant_id`, `normalized_user_name`) ·
  `EmailIndex` UNIQUE (`tenant_id`, `normalized_email`)
- **Không có `is_deleted`** — xem ghi chú dưới đây.

> 🛑 **Tên đăng nhập và email KHÔNG còn duy nhất toàn hệ.** Hai đơn vị hoàn toàn có thể có cùng
> một `admin`, cùng một `vanthu@…`; duy nhất tính theo **cặp** với `tenant_id`. Đây là thay đổi
> có ảnh hưởng lan rộng nhất của cả mô hình multi-tenant:
>
> | Hệ quả | Ở đâu |
> | --- | --- |
> | `UserManager.FindByNameAsync` không còn định danh được một người nếu chưa biết tenant | Luồng đăng nhập phải nạp tenant **trước** khi gọi Identity |
> | Form đăng nhập và form quên mật khẩu cần thêm **ô mã đơn vị** | [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11 |
> | `EmailIndex` chuyển từ **không** duy nhất sang duy nhất theo cặp | Chỉ đúng khi tuỳ chọn *"email phải duy nhất"* của Identity đang được dùng; nếu tắt nó thì bỏ `UNIQUE` và giữ nguyên hai cột |
>
> Chi tiết luồng đăng nhập, luồng quên mật khẩu và phương án đã loại:
> [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11 ·
> [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §6.2.

> **`AppUser` KHÔNG kế thừa `BaseEntity`, và đó là chủ đích.** Identity quản vòng đời bằng field
> riêng. Ép nó kế thừa `BaseEntity` để "cho đồng bộ" tạo ra hai cơ chế vô hiệu hoá song song
> (`is_deleted` và `lockout_end`), và ngày chúng bất đồng thì không cơ chế nào sai rõ ràng để mà
> sửa: một user `is_deleted = true` nhưng `lockout_end IS NULL` đăng nhập được hay không?
>
> Hệ quả: bốn cột audit ở đây **điền tay** ở tầng service, không qua interceptor — interceptor
> chỉ chạm hậu duệ `BaseEntity`. Đây là ngoại lệ phải nhớ khi viết `IIdentityService`.

**Khoá / mở khoá đi qua `lockout_end`, KHÔNG thêm cột `is_active`:**

```text
lockout_end IS NULL  hoặc  lockout_end < now()   →  đang hoạt động
lockout_end >= now()                             →  đã khoá
```

Khoá bằng `UserManager.SetLockoutEndDateAsync` với một mốc xa trong tương lai. **Không ghi cột
này bằng SQL tay** — Identity còn cập nhật `security_stamp` cùng lượt, và bỏ qua bước đó nghĩa
là phiên đang chạy của người vừa bị khoá vẫn sống bình thường.

> 🛑 **Hai cờ đặc quyền, hai cột, LOẠI TRỪ nhau.** Đừng nhầm `has_permission_bypass` với
> `is_system_operator` — chúng là hai vai ngược nhau, và một tài khoản không được mang cả hai:
>
> | | `has_permission_bypass` | `is_system_operator` |
> | --- | --- | --- |
> | Thuộc | Một đơn vị **nghiệp vụ** | Đơn vị **hệ thống** |
> | Thấy | Mọi thứ **trong đơn vị mình** | Danh sách đơn vị + trạng thái |
> | **Không** thấy | Đơn vị khác | **Dữ liệu nghiệp vụ của mọi đơn vị** (luật M9) |
>
> **Ràng buộc kiểm tra ở database**, không phải ở test: `NOT (has_permission_bypass AND
> is_system_operator)`. Luật **M11** — nó đúng cả với dòng ghi bằng SQL tay, thứ test không canh được.

> 🪤 **KHÔNG có cách nào tạo mật khẩu hợp lệ bằng SQL.** `PasswordHasher<TUser>` dùng PBKDF2 với
> salt ngẫu nhiên mỗi lần. Tài khoản đầu tiên **phải** tạo qua `UserManager.CreateAsync` — không
> qua migration, không qua script seed. Đây là lý do runbook có một bước seed riêng chạy bằng
> binary chứ không bằng `psql`: [`script-runbook.md`](script-runbook.md) §6.

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

> **Vai trò thuộc về một đơn vị.** Mỗi tenant dựng bộ vai trò riêng, đặt tên riêng, sửa riêng —
> hai đơn vị cùng có một vai trò tên *"Văn thư"* là hai dòng khác nhau, không phải một. Thứ dùng
> chung toàn hệ là **danh mục quyền** (§5.1, §5.2), không phải vai trò.

> **`is_system` là câu trả lời cho "vai trò quản trị tối cao" mà KHÔNG hardcode role.** Một vai
> trò `is_system = true` không xoá được, không đổi tên được, và không bị thu hồi quyền
> `core.permission.write` (§5.3). Thứ được bảo vệ là **một dòng dữ liệu mang cờ**, không phải một
> chuỗi trong `Core.Application`. Dự án thứ hai đặt tên vai trò đó là gì cũng được — Core không
> cần biết.
>
> Luật S2 vẫn nguyên: phân quyền kiểm bằng **permission**, không bằng `name` của vai trò.
> `is_system` chỉ bảo vệ **vòng đời** của chính dòng đó, không bao giờ được dùng làm điều kiện
> cho phép đi qua một endpoint.

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
`ix_app_user_login_tenant_user_id`.

> **Cái giá của quyết định này, nói thẳng:** năm bảng trên do Identity sinh, và thêm một cột vào
> chúng nghĩa là phải **khai lớp con** cho từng kiểu join (`AppUserRole : IdentityUserRole<Guid>`
> và bốn kiểu tương tự), rồi chuyển `IdentityDbContext` sang dạng khai đủ kiểu. Đó là công thật,
> và nó không mua thêm gì cho `app_user_role` — `user_id` đã trỏ vào một dòng đã lọc theo tenant.
>
> **Vẫn làm, vì hai lý do.** Thứ nhất, `app_user_login` có khoá chính **toàn cục**
> (`login_provider`, `provider_key`); không có `tenant_id` trong đó thì ngày bật SSO, hai đơn vị
> dùng chung một nhà cung cấp sẽ va khoá nhau — và triệu chứng sẽ là *"không đăng nhập được"* ở
> một đơn vị mà không ai nghĩ tới nguyên nhân. Thứ hai, một bảng mà cách ly **suy ra từ việc luôn
> có người join tới bảng cha** là đúng loại quy ước hỏng khi ai đó viết một câu SQL tay (luật M6).
> Miễn trừ chúng sẽ làm danh sách ở §1.3 dài thêm năm dòng — dấu hiệu chính xác mà §1.3 cảnh báo.

> **`app_user_claim` / `app_role_claim` / `app_user_login` chưa dùng ở v1 — vẫn phải dựng.**
> Identity truy vấn chúng vô điều kiện ở một số luồng, và `app_user_token` được ghi ngay ở luồng
> đặt lại mật khẩu. Thiếu bảng thì lỗi bật ra ở một luồng không ai ngờ tới, rất xa nguyên nhân.
>
> **`app_user_role` cố ý dùng PK ghép, không PK đơn `id`.** Đây là bảng do Identity sở hữu,
> `UserManager` tra theo cặp; nó cũng không mang `is_deleted` nên bẫy §3.3 không áp dụng.

---

## 5. Nhóm phân quyền — ba bảng

Mô hình: **người dùng → vai trò → quyền**. Kiểm tra luôn kết thúc ở một mã quyền, không bao giờ
ở một tên vai trò.

```text
app_user ──< app_user_role >── app_role ──< role_permission >── permission ──> permission_resource
```

### 5.1 `core.permission_resource` — danh mục tài nguyên

**Mục đích:** trục **hàng** của ma trận phân quyền. Bảng này tồn tại để màn hình phân quyền liệt
kê được **mọi** tài nguyên, kể cả tài nguyên chưa cấp cho vai trò nào.

> **Không mang `tenant_id`** — miễn trừ đã khai ở §1.3. Danh mục tài nguyên là hợp đồng giữa code
> và dữ liệu, giống nhau ở mọi đơn vị.

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

> **Vì sao deny-by-default cần bảng này.** Không cấp quyền = không có dòng trong `role_permission`.
> Nếu màn hình dựng danh sách hàng **từ `role_permission`**, tài nguyên chưa ai được cấp sẽ biến
> mất khỏi UI — và không còn đường nào cấp nó nữa. Ở dự án tiền nhiệm, danh mục này là một lớp
> hằng số trong code, phải sửa code mới thêm được tài nguyên. Chuyển nó thành bảng cho phép module
> tự đóng góp tài nguyên mà không phải mổ vào Core.

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

`core.role.*` có mặt vì vai trò là **dữ liệu** tạo/sửa/xoá được từ màn quản trị (§4.2) — thiếu
cặp khoá này thì màn đó không có gì để kiểm.

Ba ràng buộc khi thêm dòng:

1. **Một khoá đã phát hành thì không đổi tên.** Dữ liệu phân quyền cũ sẽ trỏ vào hư không, và
   nó hỏng **im lặng**: người dùng chỉ mất quyền, không có lỗi nào bật ra.
2. **Không tách khoá theo tên vai trò.** Vai trò là dữ liệu; danh mục là hợp đồng giữa code và
   dữ liệu ([`../adr/0005-permission-based.md`](../adr/0005-permission-based.md)).
3. **Tách khoá theo sức phá hoại, không theo màn hình.** `lock`, `reset-password`,
   `role.assign` tách khỏi `write` để cấp được một vai trò "trực hỗ trợ" chỉ đặt lại được mật
   khẩu mà không đụng được vai trò của ai. Gộp chúng thì mọi người sửa được email đều gán được
   vai trò.

Module khai khoá của mình theo cùng khuôn, với `resource_key` mang tiền tố của module — Core
không biết chúng tồn tại.

> 🪤 **FK trỏ vào `permission_resource.key`, không vào `id` — và điều đó buộc một ngoại lệ.**
> Foreign key cần một unique constraint trên cột đích, mà Postgres **không** nhận index **một
> phần** làm đích của foreign key. Nếu `permission_resource.key` chỉ có
> `ux_permission_resource_key_active`, câu `ADD FOREIGN KEY` thất bại với *"there is no unique
> constraint matching given keys"*.
>
> Hai đường, phải chọn một và ghi rõ đã chọn gì:
>
> 1. Dùng `uq_permission_resource_key UNIQUE (key)` — unique **đầy đủ**. Hệ quả: một tài nguyên
>    đã xoá mềm vẫn giữ khoá, không tạo lại được cùng `key`.
> 2. Bỏ FK, kiểm toàn vẹn ở tầng ứng dụng.
>
> **Chốt: đường 1.** `permission_resource.key` là định danh kỹ thuật do lập trình viên đặt, gần
> như không bao giờ xoá rồi dựng lại bằng tay — trả cái giá đó rẻ hơn mất ràng buộc. Đây là ngoại
> lệ **có chủ đích duy nhất** của §3.3 trong schema `core`; mọi unique khác vẫn phải là index một
> phần. Câu kiểm (3) ở §11 sẽ báo dòng này — đó là dòng duy nhất được phép xuất hiện.
>
> Muốn xoá rồi tạo lại cùng `key`: xoá **cứng** dòng cũ sau khi đã dời hết `permission` con sang
> tài nguyên khác. Đây là thao tác hiếm, chấp nhận làm tay có kiểm soát.

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
- **Index:** `ix_role_permission_tenant_perm_role (tenant_id, permission_id, role_id)`

> **Đây là bảng thể hiện rõ nhất ranh giới tenant chạy ở đâu.** `permission` là danh mục dùng
> chung; `app_role` thuộc về một đơn vị; và **ô ma trận nối hai thứ đó thuộc về đơn vị**. Hai
> đơn vị cấp cùng một quyền cho hai vai trò cùng tên là hai dòng độc lập, không đụng nhau.

> **Vì sao `ix_role_permission_tenant_perm_role` là bắt buộc, không phải tối ưu sớm.**
> Truy vấn nóng nhất hệ thống — kiểm quyền, chạy trên **mọi** request có `[RequirePermission]` —
> lọc theo tenant (bộ lọc toàn cục tự thêm), rồi theo `permission_id`, rồi mới ghép `role_id`
> của người gọi. PK là khoá thay thế nên không seek được cho truy vấn nào; unique index nghiệp
> vụ thì dẫn đầu bằng `tenant_id, role_id`, sai cột thứ hai. Index này dẫn đúng thứ tự cột được
> lọc và phủ luôn cột join → index-only scan.
>
> 📖 Quy tắc thứ tự cột trong index: [`../quy-uoc/be-performance.md`](../quy-uoc/be-performance.md)

> 📌 **Đọc `EXPLAIN` cho đúng ở bảng nhỏ.** Với vài chục dòng, Postgres vẫn chọn Seq Scan vì cả
> bảng nằm gọn trong một page. Đó là lựa chọn **đúng** của planner, không phải bằng chứng index
> vô dụng. Giá trị của index xuất hiện khi số tổ hợp (vai trò × quyền) tăng; chi phí duy trì gần
> bằng 0 vì bảng này ghi rất hiếm. Đừng gỡ index vì `EXPLAIN` trên dữ liệu seed không dùng tới nó.

**Bất biến chống tự khoá cửa:** không được thu hồi quyền `core.permission.write` khỏi một vai trò
`is_system = true`. Không có bất biến này, một lần lưu ma trận sai làm **không ai** còn sửa được
ma trận nữa — và đường sửa duy nhất còn lại là `UPDATE` bằng SQL tay trên production.

Bất biến này kiểm bằng một truy vấn trên **đúng các dòng đang ghi**, không cần đếm toàn bảng, nên
nó không dính bài toán tranh chấp giữa hai request đồng thời. Đây là điểm khác với luật *"không
được hạ vai trò quản trị cuối cùng"* — luật đó phải đếm toàn bảng mỗi lần ghi và vẫn thua một ca
race, nên **đã loại**.

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

> **Menu thuộc về đơn vị.** Mỗi cơ quan chỉnh cây điều hướng của mình mà không đụng cơ quan khác;
> `code` duy nhất **trong một đơn vị**, nên hai đơn vị cùng có mục `code = 'quan-tri'` là bình
> thường. `required_permission_id` vẫn trỏ vào danh mục quyền dùng chung (§5.2).
- **Check:** `ck_menu_item_not_self_parent CHECK (parent_id IS NULL OR parent_id <> id)`

**`label_key` là khoá i18n, không phải nhãn.** Dự án tiền nhiệm lưu thẳng câu tiếng Việt vào cột
`Name`; khi thêm cơ chế đổi ngôn ngữ ngay trong app, mọi nhãn menu trở thành chuỗi **không dịch
được** — nó đã cố định trước khi người dùng chọn ngôn ngữ, và không có gì ở FE tra ngược lại
được. Luật R8 và F8 cùng chặn hướng đó.

**Ba luật không diễn đạt được bằng constraint** — phải kiểm ở tầng ứng dụng và phải có test:

1. **Cây đúng một cấp.** Một mục đã có `parent_id` thì không được có con. `CHECK` không nhìn được
   sang dòng khác; ràng buộc này cần trigger hoặc kiểm ở handler. **Chọn kiểm ở handler** —
   trigger là logic nghiệp vụ nằm ngoài tầm mọi test C# và ngoài tầm mọi lượt review code.
2. **Mục cha có `route = NULL`.** Cha chỉ đóng/mở, không điều hướng.
3. **`code` không bao giờ đổi sau khi đã dùng.** Route và quyền đổi được; `code` thì không — nó
   là khoá `track` phía FE và là điểm neo của mọi tham chiếu menu.

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

**Bước 1 thắng tuyệt đối** là chỗ dễ làm sai nhất. Nếu hai cơ chế cùng có hiệu lực — giao hoặc
hợp — sẽ có ca một mục ẩn đi mà không ai chỉ ra được dòng dữ liệu nào gây ra, vì cả hai đều
"đúng một nửa". Ưu tiên cứng cho permission cũng là hướng hợp với luật S2.

**Mục cha luôn được kéo vào khi có bất kỳ con nào hiện** — kể cả khi bản thân mục cha bị quy tắc
trên loại. Thiếu dòng cha trong response, FE dựng cây từ `parent_id` sẽ coi con là mục gốc và
đặt nó sai chỗ trên sidebar. Hợp đồng cụ thể:
[`../contracts/meta-menu.md`](../contracts/meta-menu.md).

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

`code` + `params` thay vì một cột `message` chứa câu hoàn chỉnh — cùng lý do như `label_key` ở
§6.1, và cùng luật R7: **tham số truyền theo TÊN**, không theo thứ tự. `params` dạng
`{"UserName": "an.nguyen"}` dịch được sang mọi ngôn ngữ; `params` dạng `["an.nguyen"]` thì không,
vì vị trí chỗ trống trong câu đổi theo ngôn ngữ.

Xem [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) cho cơ chế phát.

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

Chuông thông báo hỏi *"người này còn mấy tin chưa đọc"* trên **mọi** lần tải trang. Index một
phần chỉ chứa dòng chưa đọc, nên nó nhỏ hơn hẳn bảng và **không phình theo lịch sử đã đọc** —
đúng đặc tính cần cho một bảng chỉ tăng.

Tên unique rút gọn thành `notif_user` **có chủ đích**: tên đầy đủ chạm ngưỡng 63 byte của §2.2.
Thêm `tenant` vào giữa vẫn nằm dưới ngưỡng — **phần được rút gọn là tên cột, không bao giờ là
phần `tenant`**, vì phần đó mới là thứ người đọc `\di core.*` cần thấy.

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
| `processed_at` | `timestamptz` | NULL | — |
| `attempt_count` | `integer` | NOT NULL | `0` |
| `next_attempt_at` | `timestamptz` | NULL | — |
| `last_error` | `text` | NULL | — |

- **PK:** `pk_outbox_message (id)`
- **FK:** `fk_outbox_message_tenant_id → core.tenant (id) ON DELETE RESTRICT`
- **Index (một phần):**

  ```sql
  CREATE INDEX ix_outbox_message_pending
      ON core.outbox_message (next_attempt_at)
      WHERE processed_at IS NULL;
  ```

> **`tenant_id` ở đây trả lời một câu bắt buộc: "phát event này trong ngữ cảnh của ai".** Bộ phát
> chạy như một job nền, ngoài mọi request, nên nó **bỏ bộ lọc tenant** — và lời gọi đó nằm trong
> allowlist của luật M5 ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md)
> §7). Nhưng khi giao event cho handler, nó phải nạp lại `tenant_id` của **chính dòng đó** vào
> ngữ cảnh; không có cột này thì không có cách nào làm đúng, và handler sẽ chạy với một tenant
> tuỳ tiện.
>
> **Đây cũng là ngoại lệ có chủ đích của quy tắc "`tenant_id` đứng đầu index"** (§3.7): index
> `ix_outbox_message_pending` dẫn đầu bằng `next_attempt_at` vì truy vấn duy nhất chạm nó lọc
> xuyên tenant. Thêm `tenant_id` vào đầu ở đây làm index vô dụng cho đúng truy vấn nó sinh ra để
> phục vụ.

**Bảng này KHÔNG mang khối audit và KHÔNG kế thừa `BaseEntity`**, nên nó nằm ngoài luật E3
(soft-delete query filter). Lý do: nó là hàng đợi vận hành, không phải dữ liệu nghiệp vụ. Xoá
mềm một dòng outbox không có nghĩa gì; dòng đã xử lý được dọn theo chính sách lưu trữ —
[`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md).

> **Vì sao index là index MỘT PHẦN trên `processed_at IS NULL`.** Bảng này chỉ tăng, và tuyệt
> đại đa số dòng là đã xử lý. Index đầy đủ sẽ lớn dần vô hạn trong khi truy vấn duy nhất chạm
> tới nó chỉ quan tâm phần chưa xử lý — phần gần như luôn nhỏ. Đây là ca sách giáo khoa của
> partial index, và bỏ mệnh đề `WHERE` là một trong những cách rẻ nhất để làm chậm hệ thống sau
> sáu tháng chạy.

> **`payload` là `jsonb`, không `text`.** `jsonb` kiểm cú pháp lúc ghi, nên một payload hỏng bị
> chặn tại nguồn thay vì nổ ở tiến trình dispatcher lúc 2 giờ sáng, cách xa nơi sinh ra nó.

`trace_id` giữ correlation id của request đã sinh ra event, để nối được nhật ký hai bên hàng đợi.
Không có nó, một event lỗi là một dòng không truy ngược được về thao tác nào —
[`../wiki-core/be/07-observability.md`](../wiki-core/be/07-observability.md).

---

## 9. Bảng lịch sử và bảng hạ tầng

### 9.1 `core.schema_script_history` — script nào đã chạy

**Mục đích:** trả lời câu *"database này đang ở đâu"* cho **con người**. Bảng này là nguồn sự
thật của quy trình vận hành — [`script-runbook.md`](script-runbook.md) §3.

> **Không mang `tenant_id`** — miễn trừ đã khai ở §1.3. Một script chạy một lần cho cả bản cài,
> không chạy một lần cho mỗi đơn vị.

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

`checksum_sha256` là băm nội dung file lúc chạy. Nó bắt được ca **sửa một script đã áp** — người
sửa tin rằng cả hệ đã có thay đổi, trong khi mọi database đã chạy bản cũ thì không, và không có
gì báo. `owner` nhận `'core'` hoặc khoá module, để lọc được lịch sử theo bên sở hữu.

### 9.2 `core.__ef_migrations_history`

Bảng của EF Core, **không mang `tenant_id`** (miễn trừ §1.3). Ta chỉ quyết định **hai** điều về
nó, khai tường minh khi cấu hình `DbContext`:

```csharp
options.UseNpgsql(connectionString, npgsql =>
    npgsql.MigrationsHistoryTable("__ef_migrations_history", "core"));
```

- **Đặt trong schema `core`**, không để rơi vào `public` — nếu không, `public` không còn rỗng và
  câu kiểm (1) ở §11 đỏ.
- **Đổi tên sang snake_case** để không phải trích dẫn kép khi tra tay.

Vai của nó khác hẳn `schema_script_history`: nó nói *"EF tin rằng migration nào đã áp"*, và được
ghi bởi chính script sinh từ `dotnet ef migrations script --idempotent`. **Cơ chế phát hiện DB
lệch model đọc bảng này**; con người tra tiến độ vận hành thì đọc bảng kia. Hai bảng, hai câu
hỏi — đừng gộp, và đừng để một bên suy ra bên kia.

### 9.3 `core.data_protection_key` — kho khoá bảo vệ dữ liệu

Phiên đăng nhập dùng **cookie** mã hoá bằng ASP.NET Core Data Protection. Mặc định, khoá bảo vệ
nằm trên đĩa của từng máy. Chạy **nhiều instance** thì instance A không giải mã được cookie do B
phát ra, và triệu chứng nhìn thấy là *"đang dùng thì bị đăng xuất ngẫu nhiên"* — một triệu chứng
gần như không ai truy ra nguyên nhân, vì nó không giống lỗi cấu hình chút nào.

Cách xử lý chuẩn là lưu key ring vào DB (`PersistKeysToDbContext`), tức thêm một bảng:

| Cột | Kiểu |
| --- | --- |
| `id` | `integer GENERATED BY DEFAULT AS IDENTITY` |
| `friendly_name` | `text` |
| `xml` | `text` |

**Đã chốt: dựng bảng này ngay từ đầu**, kể cả khi v1 chỉ chạy một instance —
[`../adr/0014-mot-instance-key-ring-postgres.md`](../adr/0014-mot-instance-key-ring-postgres.md)
§2. Lý do không phải để sẵn sàng cho nhiều instance: khoá để trên đĩa cục bộ **mất khi
container được tạo lại**, nên ngay một instance cũng đá toàn bộ người dùng ra mỗi lần triển
khai lại. Cấu hình: `PersistKeysToDbContext`. Xem thêm
[`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md).

> **Bảng này KHÔNG mang `tenant_id`** — miễn trừ đã khai ở §1.3. Khoá bảo vệ dữ liệu
> thuộc về **bản cài**, không thuộc đơn vị nào: mọi tenant dùng chung tiến trình, nên phiếu xác
> thực của mọi tenant đều được ký bằng cùng một key ring. Tách key ring theo tenant không thêm
> cách ly nào mà chỉ thêm N thứ phải xoay vòng.

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
| `tenant_id` | `uuid` | ✘ | Đơn vị nơi hành động xảy ra (§3.7) |
| `occurred_at` | `timestamptz` | ✘ | |
| `actor_user_id` | `uuid` | ✔ | `null` khi hành động do hệ thống (job nền, seed) |
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
- **FK:** `fk_audit_log_tenant_id → core.tenant (id) ON DELETE RESTRICT`
- **Index:** `ix_audit_log_tenant_occurred (tenant_id, occurred_at DESC)` · `ix_audit_log_tenant_target (tenant_id, target_type, target_id)`

Ba điểm khác mọi bảng `core` còn lại:

1. **Không có năm cột audit và không xoá mềm** (§3.2, §3.3). Bảng **chỉ ghi thêm**; `occurred_at`
   và `actor_*` đã là thông tin audit của chính nó.
2. **Tài khoản DB của ứng dụng chỉ có `INSERT` và `SELECT`** trên bảng này — không `UPDATE`,
   không `DELETE`. Bất biến được ép bằng quyền của DB, không bằng quy ước.
3. **Không FK tới `app_user`.** Người thực hiện có thể bị đổi tên hay xoá mềm; dòng nhật ký
   phải còn nguyên nghĩa. Đó là lý do lưu `actor_display`.

---

### 9.5 `core.setting` — cấu hình theo đơn vị

> **Bảng của một thành phần Nhóm B** — chỉ tạo khi thành phần được bật. Cơ chế, thứ tự ưu tiên và
> ba ràng buộc: [`../wiki-core/be/19-cau-hinh-theo-don-vi.md`](../wiki-core/be/19-cau-hinh-theo-don-vi.md).

| Cột | Kiểu | Ghi chú |
| --- | --- | --- |
| `id` | `uuid` | §3.1 |
| `tenant_id` | `uuid` **null** | Rỗng ⇒ phạm vi hệ thống. Có giá trị ⇒ phạm vi đơn vị hoặc người dùng |
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
| Năm cột audit | | §3.2 |

- **Unique:** `(tenant_id, code_type, period)` — không mệnh đề xoá mềm: bảng này **không xoá mềm**.
- **FK:** `tenant_id → core.tenant (id) ON DELETE RESTRICT`.

Bảng chỉ được đọc-ghi bằng **một** câu lệnh cập nhật-và-trả-về. Đọc rồi ghi bằng hai câu là bẫy thứ hai ở file cơ chế §3.

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

Luật E4 ép điều này bằng ArchTest `EveryMappedEntity_LivesInTheSchemaOfItsSide`.

> 🪤 **Bẫy "schema mặc định" — và vì sao repo này không dính.** Ở dự án tiền nhiệm, mọi entity
> nằm chung **một** `DbContext` với schema mặc định là `core`. Một bảng nghiệp vụ quên khai schema
> rơi vào `core` mà không lỗi gì, và chỉ lộ ra vào đúng ngày người ta thật sự tách Core mang đi.
>
> Ở repo này, **mỗi bên một `DbContext`** ([`migration-policy.md`](migration-policy.md) §1.2), và
> mỗi `DbContext` đặt schema mặc định của **chính bên mình**. Entity của module quên khai schema
> rơi vào schema **của module đó**, không phải `core` — tức mặc định luôn nghiêng về câu trả lời
> đúng.
>
> Vẫn còn một ca sai: một entity `core` bị đăng ký nhầm vào `DbContext` của module. Luật E4
> (`EveryMappedEntity_LivesInTheSchemaOfItsSide`) bắt đúng ca đó.

---

## 11. Kiểm sau khi áp schema

Các truy vấn dưới là phép nghiệm thu, chạy sau **mỗi** lần dựng lại database. Chúng kiểm đúng
những thứ hay sai nhất, và mỗi câu có "mong đợi" cụ thể để đối chiếu.

```sql
-- (1) public phải RỖNG.  Mong đợi: 0 dòng.
SELECT tablename FROM pg_tables WHERE schemaname = 'public';

-- (2) Mọi bảng core kế thừa BaseEntity phải đủ 5 cột audit.
--     Mong đợi: cột "thieu" đều là {} (mảng rỗng).
SELECT t.tbl AS "bang",
       ARRAY(SELECT c FROM unnest(ARRAY['created_at','created_by','updated_at','updated_by','is_deleted']) c
             WHERE NOT EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_schema = 'core'
                                 AND table_name  = t.tbl
                                 AND column_name = c)) AS "thieu"
FROM (VALUES ('permission'), ('permission_resource'), ('role_permission'),
             ('menu_item'), ('menu_item_role'),
             ('notification'), ('notification_recipient')) AS t(tbl)
ORDER BY 1;

-- (3) Mọi unique index trên bảng có is_deleted phải LỌC theo is_deleted.
--     Mong đợi: "co_loc" = true ở mọi dòng, TRỪ đúng một ngoại lệ đã khai:
--     uq_permission_resource_key (§5.2) — nó không lọc, có chủ đích.
--
--     Lọc theo THUỘC TÍNH (unique + bảng có cột is_deleted), KHÔNG theo tên: lọc theo
--     tên thì mọi index đặt tên lệch quy ước vô hình với câu kiểm — câu kiểm tin vào
--     chính quy ước mà nó phải kiểm.
SELECT i.indexname,
       (i.indexdef LIKE '%is_deleted%') AS "co_loc"
FROM pg_indexes i
WHERE i.schemaname = 'core'
  AND i.indexdef LIKE '%UNIQUE%'
  AND EXISTS (
        SELECT 1 FROM information_schema.columns c
        WHERE c.table_schema = i.schemaname
          AND c.table_name   = i.tablename
          AND c.column_name  = 'is_deleted')
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
SELECT i.indexname, (i.indexdef LIKE '%tenant_id%') AS "co_tenant"
FROM   pg_indexes i
WHERE  i.schemaname = 'core'
  AND  i.indexdef LIKE '%UNIQUE%'
  AND  EXISTS (SELECT 1 FROM information_schema.columns c
               WHERE c.table_schema = 'core'
                 AND c.table_name   = i.tablename
                 AND c.column_name  = 'tenant_id')
ORDER BY 1;
```

Câu (3) là câu quan trọng nhất: nó bắt đúng cái bẫy §3.3, và bắt được cả khi lỗi do một script
SQL viết tay chứ không do EF sinh — tức đúng loại lỗi mà `dotnet test` không bao giờ thấy.

Câu (4) là bản kiểm chạy trên **database thật**, bổ sung cho ArchTest E5 vốn chỉ đọc model EF.
Hai lưới này bắt hai lớp lỗi khác nhau: E5 bắt người viết entity, câu (4) bắt người viết SQL tay.

**Câu (5) và (6) canh rủi ro nghiêm trọng nhất của toàn hệ: rò dữ liệu giữa hai đơn vị.** Chúng
đứng cạnh ArchTest M3 và M4 chứ không thay thế: ArchTest đọc model EF, hai câu này đọc **database
đã áp**, nên chúng bắt được cả ca một script SQL viết tay tạo bảng hoặc index mà không ai nhìn.

Danh sách miễn trừ trong câu (5) là bản sao của bảng ở §1.3 — **thêm một bảng vào một chỗ thì phải
thêm vào chỗ kia cùng lượt**. Đây là chỗ duy nhất trong file này danh sách đó xuất hiện hai lần,
và nó xuất hiện hai lần vì một câu SQL không đọc được bảng markdown.
