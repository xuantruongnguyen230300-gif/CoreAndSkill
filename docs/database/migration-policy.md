---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Chính sách migration — ai sở hữu cái gì, và đổi schema thế nào

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu 2026-09-20). Luật dưới đây chưa được đối chiếu
> toàn file với code; nhưng **giả định "chưa có migration nào" đã sai** — đọc bảng trước khi
> tạo migration mới.
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | 🛑 **Migration của schema `core` đã tồn tại**, cùng `CoreDbContextModelSnapshot.cs`, ở `src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/Migrations/`. Đếm bằng `ls src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/Migrations/*.cs`. **Migration tiếp theo KHÔNG phải migration đầu tiên**: sinh nó từ snapshot hiện tại, đừng sinh từ model rỗng | Từng mục lật sang nhãn đã đối chiếu, **riêng lẻ**, khi có người mở source ra so |
> | 🛑 **Script DDL đã tồn tại** ở `database/scripts/core/`, mỗi script mang sẵn khối ghi lịch sử kèm `checksum_sha256` (§3.1 của [`script-runbook.md`](script-runbook.md)). Số thứ tự tiếp theo lấy bằng `ls database/scripts/core/` — **đừng đặt trùng số đã dùng** | — |
> | Chưa có script nào trong số đó từng được áp lên một Postgres thật — xem bảng đầu [`schema-core.md`](schema-core.md) | Áp lần đầu theo [`script-runbook.md`](script-runbook.md) |
> | `src/` và `database/` **chưa vào git** (`git status` in chúng ở dạng chưa theo dõi) | Vào git ở lần commit đầu tiên; tới lúc đó hai lệnh đếm ở trên chỉ chạy được trên máy có sẵn cây thư mục |
>
> **File chủ về quyền sở hữu migration và quy trình đổi schema.**
> Nội dung schema: [`schema-core.md`](schema-core.md). Thao tác chạy script lên database:
> [`script-runbook.md`](script-runbook.md).

---

## 1. Ai sở hữu migration

> **Core sở hữu migration của schema `core`. Mỗi module sở hữu migration của schema mình.** — định nghĩa gốc

| Bên | Project chứa migration | Schema quản |
| --- | --- | --- |
| Core | `Core.Infrastructure` | `core` |
| Module `<X>` | `Modules.<X>.Infrastructure` | `<x>` |
| Host (`Api`) | **không chứa migration nào** | — |

Luật E6 ([`../RULES.md`](../RULES.md) §4) **sẽ** ép điều này bằng ArchTest
`EveryMigration_LivesIn_ItsOwningProject` — 📐 chưa viết, trạng thái ở hàng E6. Host là composition root, luật A7 đã cấm nó chứa
logic; migration cũng là logic.

**Bảng lịch sử migration nằm trong chính schema mà `DbContext` quản, và cùng một tên ở mọi schema:
`<schema>.__ef_migrations_history`** — `core.__ef_migrations_history` cho Core
([`schema-core.md`](schema-core.md) §9.2), `<x>.__ef_migrations_history` cho module `<X>`. Mỗi
`DbContext` khai nó bằng `MigrationsHistoryTable` trong `UseNpgsql`; mẫu của Core ở
[`../quy-uoc/be-performance.md`](../quy-uoc/be-performance.md) §7.2. Tên chung là thứ cho câu nghiệm
thu quyền ở [`script-runbook.md`](script-runbook.md) §3.6 phủ mọi schema mà không phải liệt kê schema nào.

#### Ngoại lệ có tên của E6: khoá quyền của module — định nghĩa gốc

Khoá quyền của module nằm trong hai bảng của schema `core`, nhưng chỉ module biết khoá của mình. Đường
đưa chúng vào database là migration của **chính module**, trong đúng giới hạn dưới đây:

| Migration của module **được** | Migration của module **không được** |
| --- | --- |
| `INSERT … ON CONFLICT … DO NOTHING` vào `core.permission_resource` và `core.permission`, cho khoá mà module khai qua `IPermissionCatalogSource` | `UPDATE`, `DELETE`, `TRUNCATE` trên hai bảng đó; chạm bất kỳ bảng `core` nào khác; đổi cấu trúc schema `core` |

| Ràng buộc | Vì sao |
| --- | --- |
| **Chỉ ghi thêm, bỏ qua khi đã có** | Migration của module không bao giờ sửa hay xoá dòng mà Core ghi, nên bản vá Core về sau không đè lên thứ module ghi — và ngược lại. Đó chính là ca hai chủ sở hữu mà §1.1 mô tả, thu hẹp về một thao tác không xung đột được |
| Cùng năm điều kiện của phần seed ở §4.1 | Định danh cố định, không xoá tự động, không phụ thuộc dữ liệu có sẵn — áp như với khoá của Core |
| Module mang **cổng đối chiếu hai chiều** của riêng nó — cổng B7 của Core **không** phủ khoá của module | Khoá có trong code module mà thiếu trong migration module thì deny-by-default trả 403 cho mọi người. Cổng B7 của Core quét đúng một assembly, `Core.Infrastructure`, nên nó không nhìn thấy nguồn khoá nào của module: đo bằng cách thêm một `IPermissionCatalogSource` ngoài assembly đó khai một khoá không có dòng seed — cổng xanh nguyên (2026-09-20). Chỗ trống này là nợ **B9** ở [`../DEBT.md`](../DEBT.md) |
| Script của module áp **sau** script của Core ([`script-runbook.md`](script-runbook.md) §3.3 bước 2) | Bảng đích phải có trước khi module ghi vào |

Hai đường khác cho khoá của module đều đã bị chặn: migration của **Core** ghi khoá của module là Core
biết module tồn tại (luật A3); tiến trình ứng dụng tự ghi danh mục là hướng đã khoá — §4.1.

### 1.1 Đây là ĐẢO NGƯỢC so với dự án tiền nhiệm — và vì sao

Ở dự án tiền nhiệm, host sở hữu toàn bộ migration. Vấn đề họ né là thật — `ModelSnapshot` là một file trạng thái dùng chung mà EF ghi đè toàn bộ sau mỗi lần sinh — nhưng nguyên nhân gốc là **MỘT `DbContext` cho cả hai bên**, không phải *ai sở hữu migration*.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`migration-policy.md`](../wiki-core/be/ly-do/migration-policy.md) §1.1

### 1.2 Mắt xích gỡ được nút thắt: mỗi bên một `DbContext`

Một `ModelSnapshot` thuộc về **một `DbContext`**, không thuộc về một project. Repo này có nhiều
`DbContext`:

| `DbContext` | Sống ở | Snapshot của nó |
| --- | --- | --- |
| `CoreDbContext` | `Core.Infrastructure` | Trong `Core.Infrastructure` |
| `<X>DbContext` | `Modules.<X>.Infrastructure` | Trong `Modules.<X>.Infrastructure` |

Không có file trạng thái nào dùng chung ⇒ **không có ca hai chủ sở hữu** ⇒ vấn đề §1.1 không tồn
tại. Core ship bản vá schema `core` kèm migration của chính nó; dự án tiêu thụ merge vào mà
không đụng tới snapshot của module nào.

Điều kiện để cách này đúng: **không FK xuyên schema** (luật E5, [`schema-core.md`](schema-core.md) §1.1).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`migration-policy.md`](../wiki-core/be/ly-do/migration-policy.md) §1.2

### 1.3 Vì sao phải đảo — nhu cầu thật của repo này

Repo này là bộ khung mang đi nhiều dự án: mô hình host sở hữu tất cả đòi **mỗi** dự án lặp lại chuỗi thao tác tay để đồng bộ schema `core`; mô hình này chỉ đòi merge.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`migration-policy.md`](../wiki-core/be/ly-do/migration-policy.md) §1.3

📖 Quyết định gốc: [`../adr/0008-core-so-huu-migration.md`](../adr/0008-core-so-huu-migration.md)

---

## 2. Đánh đổi ghi thẳng — `Core.Infrastructure` nay biết provider là PostgreSQL

Migration EF Core **không trung lập với provider**. Một file migration sinh cho Npgsql chứa kiểu
Postgres (`timestamptz`, `jsonb`, `uuid`), chứa `.Annotation("Npgsql:ValueGenerationStrategy", …)`,
và chứa `migrationBuilder.Sql(...)` cho những thứ EF không mô hình hoá được — index một phần
chẳng hạn.

Vì Core giữ migration, **`Core.Infrastructure` tham chiếu `Npgsql.EntityFrameworkCore.PostgreSQL`
và không giả vờ ngược lại.** Không có lớp trừu tượng provider, không có `IDatabaseProvider` với
đúng một implementation.

**Chấp nhận được** — PostgreSQL đã chốt, và lớp trừu tượng provider không che được ngữ nghĩa (index một phần, `xmin`, `jsonb`).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`migration-policy.md`](../wiki-core/be/ly-do/migration-policy.md) §2

### 2.1 Nếu một ngày phải đổi provider — chính xác phải làm gì

Danh mục việc — nặng nhất là thay index một phần và thay `xmin` — ở [`migration-policy.md`](../wiki-core/be/ly-do/migration-policy.md) §2.1.

Kết luận thẳng: **đổi provider là một dự án, không phải một cấu hình.** Biết trước điều đó khi
chốt PostgreSQL còn hơn tin vào một lớp trừu tượng không chịu nổi lần đổi thật.

---

## 3. Quy ước đặt tên migration

```text
<timestamp EF tự sinh>_<ĐộngTừ><ĐốiTượng>
```

| Ví dụ tốt | Vì sao |
| --- | --- |
| `TaoLuocDoCore` | Migration **đầu tiên** của schema `core` — dựng toàn bộ lược đồ ban đầu; không dùng `InitialCreate` |
| `AddNotificationRecipientTable` | Nói rõ thêm cái gì |
| `AddIndexRolePermissionByPermission` | Nói rõ index nào, trên bảng nào |
| `RenameMenuItemNameToLabelKey` | Nói rõ cột nào, đổi thành gì |
| `DropColumnUserIsActive` | Nói rõ bỏ cái gì |

| Ví dụ xấu | Vì sao |
| --- | --- |
| `Update1` · `Fix` · `Temp` | Không nói gì. Sáu tháng sau, đọc tên không biết nó làm gì mà không mở file |
| `AddNotificationAndFixMenuAndRenameUser` | Ba việc trong một migration, xem §3.1 |
| `InitialCreate2` | Có `InitialCreate2` nghĩa là `InitialCreate` đã sai; sửa cái sai đó, đừng đánh số tiếp |

Quy tắc: **PascalCase, động từ đứng trước**, tên đối tượng đủ để đoán được nội dung mà không mở
file. Đây là thứ người vận hành đọc trong `core.__ef_migrations_history` lúc 2 giờ sáng.

### 3.1 Một migration làm MỘT việc

> Một migration = một thay đổi schema mạch lạc, mô tả được trong một mệnh đề.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`migration-policy.md`](../wiki-core/be/ly-do/migration-policy.md) §3.1

### 3.2 ĐỌC file migration vừa sinh — luôn luôn

`dotnet ef migrations add` là lệnh sinh code, và code sinh ra có thể sai vì snapshot sai.

**Dấu hiệu phải DỪNG, không "sửa cho chạy":**

| Thấy gì | Nghĩa là |
| --- | --- |
| `DropTable` cho bảng không định xoá | Snapshot đang thừa bảng — model và snapshot lệch |
| `CreateTable` cho bảng biết chắc đã có | Snapshot đang thiếu bảng — thường do sinh migration trên một `DbContext` sai |
| Lệnh chạm schema **không thuộc** bên mình | Cấu hình `DbContext` đang gom cả entity của bên kia. Luật E4 sẽ bắt, nhưng ở đây đã thấy rồi thì dừng luôn |
| Migration **rỗng** khi ta vừa đổi model | `dotnet ef` đang đọc một `DbContext` khác với `DbContext` ta vừa sửa |

Phép thử rẻ nhất để biết snapshot có khớp model không, **không sinh file nào**: lệnh
`has-pending-model-changes` ở [`script-runbook.md`](script-runbook.md) §5.4, chạy cho đúng
`DbContext` vừa sửa. Chạy được bất cứ lúc nào, không để lại rác trong `src/`. Nên chạy trước mỗi
lần `migrations add`.

---

## 4. Migration schema và migration dữ liệu — hai loại, hai chỗ

| | Migration **schema** | Migration **dữ liệu** |
| --- | --- | --- |
| Làm gì | `CREATE`/`ALTER`/`DROP` bảng, cột, index, constraint | Seed danh mục, backfill giá trị cho dòng đã có |
| Ở đâu | Trong migration EF | **Tuỳ khối lượng** — xem dưới |
| Chạy khi | Áp script schema | Sau khi schema đã áp |

### 4.1 Danh mục quyền — dòng vào database bằng migration, và chỉ bằng migration

Danh mục **nhỏ, cố định, là hợp đồng giữa code và dữ liệu** thì nằm trong migration. Ở repo này
đó là `core.permission_resource` và `core.permission`. **Khoá** khai trong code qua seam
`IPermissionCatalogSource` và được kiểm lúc khởi động
([`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1); **dòng** tương ứng vào
database bằng migration.

**Giá trị của dòng seed lấy từ chính record khai ở seam, không có nguồn thứ hai.** Mỗi
`PermissionResourceDefinition` là một dòng `core.permission_resource` (`Key` → `key`, `NameKey` →
`name_key`, `ModuleKey` → `module_key`, `DisplayOrder` → `display_order`); mỗi `PermissionDefinition`
là một dòng `core.permission` (`Code` → `code`, `ResourceKey` → `resource_key`, `Action` → `action`,
`NameKey` → `name_key`, `DisplayOrder` → `display_order`). Migration chỉ thêm `id` cố định (điều
kiện 2 dưới đây) và cột audit; nó **không** tự đặt giá trị cho cột nào khác — một cột mà migration
tự nghĩ ra là một cột không có gì đối chiếu, và test B7 sẽ không thấy nó lệch.

Điều kiện của phần seed trong migration, thiếu một thì không được:

1. **Idempotent** — `ON CONFLICT … DO NOTHING`, chạy nhiều lần cho kết quả như chạy một lần. Nhớ
   lặp lại nguyên văn vị từ của index một phần ([`schema-core.md`](schema-core.md) §3.3).
2. **Định danh cố định** — cùng một khoá mang cùng một `id` ở mọi môi trường, không sinh ngẫu
   nhiên lúc chạy.
3. **Không xoá tự động.** Khoá bỏ khỏi code không kéo theo `DELETE` trong migration: dòng phân
   quyền trỏ tới nó sẽ thành rác âm thầm. Bỏ một khoá là một bước có chủ đích.
4. **Không phụ thuộc dữ liệu có sẵn.** Một câu `UPDATE … WHERE role_id = (SELECT … WHERE name =
   'Admin')` sẽ **im lặng không làm gì** trên database chưa có vai trò đó — và không ai biết.
5. **Đo được bằng mắt sau khi chạy.** Câu nghiệm thu `core.permission` ở
   [`script-runbook.md`](script-runbook.md) §3.3.

**Hằng số trong code và dòng seed trong migration phải khớp hai chiều** — test CI của luật **B7**
([`../RULES.md`](../RULES.md)) đối chiếu cặp `Code` + `ResourceKey` của mọi `PermissionDefinition`
với cặp `code` + `resource_key` trong migration: khoá có trong code mà thiếu dòng seed thì đỏ (deny-by-default
trả 403 cho mọi người); dòng seed không ứng với khoá nào trong code cũng đỏ — đó là dấu hiệu một khoá
bị bỏ khỏi code mà chưa đi qua bước có chủ đích của điều kiện 3.

**Tiến trình ứng dụng chỉ đọc hai bảng này.** Tài khoản database của ứng dụng chỉ có `SELECT`
trên chúng — luật **M13**, bảng quyền ở [`script-runbook.md`](script-runbook.md) §3.6. Hướng
"tiến trình ứng dụng tự ghi danh mục lúc khởi động hoặc lúc chạy" đã khoá, kèm lý do:
[`../wiki-core/be/13-core-data-migration.md`](../wiki-core/be/13-core-data-migration.md) §8.

**Khoá của module** khai qua cùng seam, và vào database bằng migration của **chính module** — ngoại lệ
có tên của luật E6 ở §1, cùng năm điều kiện trên. **Cổng** B7 thì không dùng chung được: cổng của Core
chỉ quét assembly `Core.Infrastructure`, nên module phải mang cổng đối chiếu của riêng nó (nợ **B9**,
[`../DEBT.md`](../DEBT.md)).

**Không bao giờ nằm trong migration:**

| Dữ liệu | Đi đường nào |
| --- | --- |
| Tài khoản người dùng | Mật khẩu Identity không tạo được bằng SQL ([`schema-core.md`](schema-core.md) §4.1). Tài khoản đầu tiên do lệnh bootstrap tạo — [`script-runbook.md`](script-runbook.md) §3.3 |
| Dữ liệu của một đơn vị — vai trò mặc định, ánh xạ vai trò → quyền, menu | Mang `tenant_id`, sinh ra cùng đơn vị: service tạo đơn vị ghi — [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) |

### 4.2 Backfill lớn KHÔNG nằm trong migration — bốn lý do

Backfill vài triệu dòng trong migration thì khoá bảng suốt transaction, không rollback được, không đo được tiến độ, không dừng rồi tiếp được.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`migration-policy.md`](../wiki-core/be/ly-do/migration-policy.md) §4.2

**Cách đúng: một job có trạng thái, chạy theo lô.**

| Yêu cầu | Cách |
| --- | --- |
| Chia lô | `UPDATE … WHERE id > @cursor ORDER BY id LIMIT 5000`, lặp — `id` là UUID v7 nên tăng dần, dùng làm con trỏ được |
| Nhớ vị trí | Lưu `@cursor` vào một bảng trạng thái của chính job |
| Dừng được | Kiểm cờ huỷ giữa các lô |
| Đo được | Ghi số dòng đã xử lý sau mỗi lô |
| Chạy lại an toàn | Câu `UPDATE` phải idempotent — chạy lại trên dòng đã xử lý không đổi gì |

Migration chỉ làm phần schema: **thêm cột mới (nullable)**. Việc điền dữ liệu là chuyện của job.
Xem [`../wiki-core/be/13-core-data-migration.md`](../wiki-core/be/13-core-data-migration.md).

---

## 5. Thay đổi phá vỡ — quy trình bốn bước

Đổi tên cột, đổi kiểu cột, tách một cột thành hai: **không bao giờ làm trong một lượt**, kể cả
khi hệ thống chỉ chạy một instance.

Lý do: giữa lúc script schema chạy xong và lúc bản build mới lên, luôn có một khoảng thời gian —
dù ngắn — mà **code cũ đang nói chuyện với schema mới**. Đổi tên cột trong một lượt biến khoảng
đó thành lỗi 500 hàng loạt. Với nhiều instance hoặc rolling deploy, khoảng đó dài bằng cả đợt
deploy.

### 5.1 Bốn bước — ví dụ đổi `menu_item.name` thành `menu_item.label_key`

| Bước | Schema | Code | Triển khai được một mình? |
| --- | --- | --- | --- |
| **1. Mở rộng** | `ADD COLUMN label_key varchar(200) NULL` | Chưa đọc, chưa ghi | ✔ Code cũ không biết cột mới ⇒ vô hại |
| **2. Ghi cả hai** | — | Mọi đường ghi điền **cả** `name` **và** `label_key`. Đọc vẫn từ `name` | ✔ |
| **3. Chuyển đọc** | — | Backfill xong (§4.2) rồi chuyển mọi đường đọc sang `label_key`. Vẫn ghi cả hai | ✔ Rollback về bước 2 an toàn vì `name` vẫn đúng |
| **4. Thu hẹp** | `DROP COLUMN name` | Bỏ đường ghi `name` | ✔ Chỉ làm khi đã chắc **không** rollback về trước bước 3 |

**Bước 4 là bước không hoàn tác được** — sau khi `DROP COLUMN`, dữ liệu cột cũ đã mất. Để nó cách
bước 3 ít nhất một chu kỳ phát hành, và chỉ làm khi bản chạy bước 3 đã ổn định trên production.

### 5.2 Đổi kiểu cột

Cùng khuôn, nhưng bước 1 thêm một cột **mới tên khác** thay vì `ALTER COLUMN … TYPE`.

`ALTER COLUMN … TYPE` trên bảng lớn buộc Postgres **viết lại toàn bộ bảng** và giữ khoá `ACCESS
EXCLUSIVE` suốt thời gian đó — không ai đọc được, không riêng ghi. Thêm cột mới rồi backfill theo
lô tránh hẳn điều đó.

**Ngoại lệ rẻ, được phép làm thẳng:** nới `varchar(n)` lên số lớn hơn — Postgres chỉ đổi metadata,
không rewrite ([`schema-core.md`](schema-core.md) §3.5).

### 5.3 Thêm cột `NOT NULL`

```sql
-- Nguy hiểm trên bảng lớn với Postgres cũ, và luôn hỏng nếu bảng đã có dữ liệu mà không có DEFAULT
ALTER TABLE core.menu_item ADD COLUMN module_key varchar(100) NOT NULL;
```

Đường an toàn: thêm **nullable** → backfill theo lô → `SET NOT NULL`. Bước `SET NOT NULL` vẫn quét
toàn bảng để kiểm, nhưng không viết lại nó.

---

## 6. Rollback — vì sao migration xuôi-chỉ thường an toàn hơn

EF sinh sẵn `Down()` cho mọi migration, và điều đó tạo ảo giác rằng rollback là một nút bấm.

`Down()` của thay đổi có mất mát không dựng lại được dữ liệu, gần như không bao giờ được chạy thử, và không quay ngược được dữ liệu đã ghi theo hình dạng mới.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`migration-policy.md`](../wiki-core/be/ly-do/migration-policy.md) §6

**Chính sách: xuôi-chỉ.** Hỏng thì **đi tiếp** bằng một migration sửa lỗi, không đi lùi.

| Tình huống | Làm gì |
| --- | --- |
| Migration vừa áp sai, chưa ai ghi dữ liệu mới | Viết migration mới đảo lại thay đổi. Nhanh như `Down()` và có qua review |
| Migration sai đã mất dữ liệu | Khôi phục từ **backup**. `Down()` không giúp gì ở đây — đây là lý do checklist production ở [`script-runbook.md`](script-runbook.md) §7 bắt đầu bằng backup |
| Không chắc migration đúng | **Đừng chạy.** Chạy thử trên bản sao của production trước — rẻ hơn mọi phương án khôi phục |

`Down()` vẫn để nguyên trong file, không xoá: nó hữu ích trên máy dev để dựng lại nhanh. Nhưng
**không** phải là kế hoạch khôi phục production, và đừng viết trong quy trình vận hành như thể
nó là.

---

## 7. Cổng nào canh những luật này

> 📐 Phần chưa thi công: [`migration-policy-chua-thi-cong.md`](migration-policy-chua-thi-cong.md)

### 7.1 E6 kiểm gì cho cụ thể

> 📐 Phần chưa thi công: [`migration-policy-chua-thi-cong.md`](migration-policy-chua-thi-cong.md) §7.1

---

## 8. Danh mục việc phải làm khi đổi schema `core`

Danh sách này áp cho **mỗi** thay đổi schema `core`, không có ngoại lệ "sửa nhỏ":

1. Sửa entity + EF configuration trong `Core.Infrastructure`.
2. `dotnet ef migrations has-pending-model-changes` — xác nhận snapshot đang khớp **trước** khi
   sinh (§3.2).
3. `dotnet ef migrations add <Tên>` — một việc, đặt tên theo §3.
4. **Đọc file vừa sinh**, đối chiếu với bảng dấu hiệu ở §3.2.
5. Nếu là thay đổi phá vỡ → chia bốn bước theo §5, migration này chỉ làm bước hiện tại.
6. Sinh script `.sql` idempotent và đặt vào thư mục `<gốc repo>/database/scripts/` — [`script-runbook.md`](script-runbook.md) §4.
7. Cập nhật [`schema-core.md`](schema-core.md): cột, index, ràng buộc, và **lý do**.
   Bảng mới cần quyền khác mặc định cho tài khoản ứng dụng ⇒ sửa bảng quyền ở
   [`script-runbook.md`](script-runbook.md) §3.6 cùng lượt. Thêm khoá quyền của Core ⇒ thêm dòng
   seed vào migration cùng lượt (§4.1).
8. Cập nhật hợp đồng API nào chịu ảnh hưởng trong [`../contracts/`](../contracts/README.md).
9. Chạy integration test — luật T2 đòi Postgres thật, không mock.

Bước 7 hay bị bỏ nhất, và hậu quả của nó là thứ [`../RULES.md`](../RULES.md) §1 D13 sinh ra để
chặn: tài liệu schema và schema thật lệch nhau, mà không có cổng nào bắt được.
