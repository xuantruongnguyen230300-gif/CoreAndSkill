---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Chính sách migration — ai sở hữu cái gì, và đổi schema thế nào

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`, chưa có migration nào tồn tại. File này là
> luật cho giai đoạn 2, không phải mô tả hiện trạng.
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

Luật E6 ([`../RULES.md`](../RULES.md) §4) ép điều này bằng ArchTest
`EveryMigration_LivesIn_ItsOwningProject`. Host là composition root, luật A7 đã cấm nó chứa
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
| Test CI đối chiếu **hai chiều** của luật B7 phủ cả khoá của module | Khoá có trong code module mà thiếu trong migration module thì deny-by-default trả 403 cho mọi người |
| Script của module áp **sau** script của Core ([`script-runbook.md`](script-runbook.md) §3.3 bước 2) | Bảng đích phải có trước khi module ghi vào |

Hai đường khác cho khoá của module đều đã bị chặn: migration của **Core** ghi khoá của module là Core
biết module tồn tại (luật A3); tiến trình ứng dụng tự ghi danh mục là hướng đã khoá — §4.1.

### 1.1 Đây là ĐẢO NGƯỢC so với dự án tiền nhiệm — và vì sao

Ở dự án tiền nhiệm, **host sở hữu toàn bộ migration**: mọi file `Migrations/*.cs` cùng
`ModelSnapshot` nằm trong project host, Core chỉ giao đi các file `.sql` đã sinh sẵn.

Lý do họ chọn thế là một lý do thật, không phải sơ suất — đọc kỹ trước khi đảo:

> `ModelSnapshot` là **một file trạng thái dùng chung mà EF bắt buộc phải chính xác**. EF không
> đọc database để biết cần sinh gì; `dotnet ef migrations add` so **model hiện tại** với
> snapshot, và **ghi đè toàn bộ** snapshot bằng model mới sau mỗi lần sinh.
>
> Nếu Core và dự án tiêu thụ cùng ghi vào một snapshot, ngày Core ship bản vá là ngày hỏng. Có
> đúng hai đường đi khi merge, cả hai đều mất:
>
> | Giữ snapshot của ai | Chuyện gì xảy ra |
> | --- | --- |
> | Của **Core** (không biết bảng dự án) | Lần `migrations add` kế tiếp ở dự án so model (**có** bảng) với snapshot (**không** có) ⇒ EF sinh `CREATE TABLE` cho bảng **đang có dữ liệu thật** |
> | Của **dự án** (không có thay đổi Core) | Thay đổi schema của Core biến mất khỏi trạng thái ⇒ lần sinh sau EF sinh **lại** thứ đã áp, hoặc âm thầm bỏ qua thứ Core vừa đổi |
>
> Triệu chứng **không gợi ra nguyên nhân**: người gặp thấy một file sinh tự động chứa
> `CREATE TABLE` cho bảng mình biết chắc là đã có, và phản xạ đầu tiên là nghi database, nghi
> connection string — chứ không nghi một file `.cs` mình chưa bao giờ mở. Không lỗi biên dịch,
> không test nào bắt.

**Vấn đề đó là thật. Nhưng nguyên nhân gốc của nó không phải "ai sở hữu migration" — mà là
"MỘT `DbContext` cho cả hai bên".**

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

**Vì sao dự án tiền nhiệm KHÔNG chọn được đường này:** họ giữ một `DbContext` duy nhất, và lý do
là **khoá ngoại xuyên schema**. EF chỉ sinh được FK trong phạm vi một model, nên FK từ bảng
nghiệp vụ sang bảng `core` đòi hai bên nằm chung context. Bỏ một context là mất FK đó, tức mất
một ràng buộc toàn vẹn.

Repo này **đã cấm FK xuyên schema từ đầu** (luật E5, [`schema-core.md`](schema-core.md) §1.1).
Ràng buộc mà họ phải bảo vệ, ta cố ý không có. Vì vậy cái giá của việc tách context, với ta,
bằng không — và ta nhận về đúng thứ họ phải trả giá để mua: **mỗi bên một trạng thái riêng**.

Đây là ví dụ điển hình của việc **không chép mù một quyết định cùng với lý do đã hết hiệu lực**.

### 1.3 Vì sao phải đảo — nhu cầu thật của repo này

Dự án tiền nhiệm chỉ có một sản phẩm. Repo này là **bộ khung mang đi nhiều dự án**, và đó là
toàn bộ lý do tồn tại của nó. Với mô hình cũ:

| Việc | Mô hình cũ (host sở hữu tất cả) | Mô hình này |
| --- | --- | --- |
| Dự án mới dựng bảng `core` | Phải tự sinh lại `CoreBaseline` từ số 0, rồi đối chiếu tay với `.sql` của Core | Nhận nguyên migration của Core, không sinh lại gì |
| Core sửa một cột `core` | Ship `.sql`, mỗi dự án tự chạy rồi tự `migrations add DongBoCore…` để snapshot đuổi kịp | Merge migration của Core như merge code thường |
| Ai biết `core` đang ở phiên bản nào | Không ai — phải đọc xem file `.sql` nào đã chạy tay | Đọc `core.__ef_migrations_history` |

Cột giữa là chuỗi thao tác tay, mỗi bước đều có đường làm sai và không bước nào có cổng canh.
Với **N** dự án, nó phải làm đúng **N** lần.

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

**Chấp nhận được**, vì hai lý do:

1. PostgreSQL đã chốt, và không có kế hoạch đổi. Một lớp trừu tượng cho một khả năng không xảy ra
   là chi phí trả **mỗi ngày** để mua một quyền chọn **không bao giờ dùng**.
2. Lớp trừu tượng đó cũng **không giữ được lời hứa**. Index một phần (`WHERE is_deleted = false`)
   là nền tảng của soft delete ở repo này, `xmin` là concurrency token, `jsonb` là kiểu của
   `payload` outbox. Cả ba đều không có tương đương trực tiếp ngoài Postgres. Một `IDbProvider`
   che được cú pháp nhưng không che được **ngữ nghĩa**, nên nó chỉ dời chỗ vấn đề chứ không gỡ.

### 2.1 Nếu một ngày phải đổi provider — chính xác phải làm gì

Ghi ra để lần đó không ai phải tự dò, và để không ai tưởng rằng "vì đã tách 5 project nên đổi
provider là dễ":

| # | Việc | Mức độ |
| --- | --- | --- |
| 1 | **Baseline lại toàn bộ migration** cho provider mới. Migration cũ không chuyển đổi được, chỉ xoá và sinh lại từ model | Bắt buộc |
| 2 | Thay **unique index một phần** bằng cơ chế khác của provider mới. SQL Server có filtered index (tương đương); MySQL/Oracle **không có** ⇒ tính duy nhất phải chuyển thành cột sinh (`code_active = CASE WHEN is_deleted THEN NULL ELSE code END`) hoặc kiểm ở tầng ứng dụng | Nặng — chạm luật §3.3 của [`schema-core.md`](schema-core.md) |
| 3 | Thay **`xmin`** bằng concurrency token của provider mới (`rowversion` ở SQL Server), tức **thêm một cột thật** vào mọi bảng và thêm một interceptor giữ nó | Nặng |
| 4 | Thay **`jsonb`** ở `outbox_message.payload` và `notification.params` bằng kiểu JSON tương ứng; mất phép kiểm cú pháp lúc ghi nếu provider mới chỉ có `text` | Trung bình |
| 5 | Rà lại mọi `migrationBuilder.Sql(...)` viết tay | Trung bình |
| 6 | Chạy lại **toàn bộ** integration test trên provider mới (luật T2 — DB thật, không mock) | Bắt buộc |

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

Vì sao đây là luật chứ không phải sở thích:

- **Rollback theo migration là bất khả thi khi một migration làm ba việc.** Cần hoàn tác đúng một
  việc trong ba, không có đường nào ngoài viết tay.
- **Đọc diff của một migration là cách rẻ nhất để bắt lỗi.** Một migration một việc thì bất
  thường lộ ra ngay; một migration ba việc thì `DropTable` lẫn giữa hai chục lệnh khác không ai
  thấy. Ở dự án tiền nhiệm, đúng ca này suýt xảy ra: snapshot lệch làm migration kế tiếp chứa
  năm lệnh `DropTable`, và **lưới an toàn duy nhất là người đọc phát hiện ra**.
- **Xung đột merge giải được.** Hai migration nhỏ độc lập merge được; hai migration to chồng lấn
  thì phải sinh lại.

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
có tên của luật E6 ở §1, cùng năm điều kiện trên, cùng test B7.

**Không bao giờ nằm trong migration:**

| Dữ liệu | Đi đường nào |
| --- | --- |
| Tài khoản người dùng | Mật khẩu Identity không tạo được bằng SQL ([`schema-core.md`](schema-core.md) §4.1). Tài khoản đầu tiên do lệnh bootstrap tạo — [`script-runbook.md`](script-runbook.md) §3.3 |
| Dữ liệu của một đơn vị — vai trò mặc định, ánh xạ vai trò → quyền, menu | Mang `tenant_id`, sinh ra cùng đơn vị: service tạo đơn vị ghi — [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) |

### 4.2 Backfill lớn KHÔNG nằm trong migration — bốn lý do

Ví dụ backfill lớn: điền `label_key` cho toàn bộ `menu_item` cũ, chuẩn hoá `created_by` từ id
sang tên đăng nhập trên vài triệu dòng.

| # | Vì sao không | Chi tiết |
| --- | --- | --- |
| 1 | **Khoá bảng lâu** | Migration chạy trong **một** transaction. Một `UPDATE` vài triệu dòng giữ khoá suốt thời gian đó — mọi ghi vào bảng ấy xếp hàng, và ứng dụng nhìn như treo. Với migration chạy lúc deploy, đó là downtime không ai lường trước |
| 2 | **Không rollback được** | Migration `Down` của một backfill hoặc không viết được (giá trị cũ đã mất), hoặc lại là một `UPDATE` khổng lồ nữa. Nút "hoàn tác" tồn tại trên giấy chứ không dùng được |
| 3 | **Không đo tiến độ được** | Migration là hộp đen: nó đang ở dòng thứ mấy trong ba triệu dòng, không có cách nào biết. Người vận hành chỉ có hai trạng thái — *"chưa xong"* và *"đã xong"* — và một cửa sổ chờ không biết dài bao lâu |
| 4 | **Không dừng và tiếp lại được** | Timeout ở giữa chừng thì transaction rollback sạch. Ba tiếng chạy đổi lấy con số không, và lần chạy lại cũng có ngần ấy rủi ro |

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

**Ba lý do `Down()` không phải nút bấm:**

1. **`Down()` của một thay đổi có mất mát thì không đảo ngược được.** `Down()` của
   `DROP COLUMN name` là `ADD COLUMN name` — nó dựng lại **cột rỗng**, không dựng lại dữ liệu.
   Schema trở về hình dạng cũ trong khi dữ liệu đã mất; đó là trạng thái **tệ hơn** cả hai đầu, vì
   nhìn thì như đã khôi phục.
2. **`Down()` gần như không bao giờ được chạy thử.** Nó không nằm trong đường đi hằng ngày, không
   test nào chạm tới. Một đoạn code chưa từng chạy mà lại được dùng lần đầu vào lúc đang có sự cố
   là công thức của sự cố thứ hai.
3. **Down trên production đòi database đang có dữ liệu thật quay ngược trạng thái.** Nếu bản mới
   đã ghi dữ liệu theo hình dạng mới, việc quay ngược schema làm dữ liệu đó không đọc được nữa.

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

| Luật | Ép bằng | Bắt được gì |
| --- | --- | --- |
| E4 `EveryMappedEntity_LivesInTheSchemaOfItsSide` | ArchTest | Entity của Core rơi vào schema module, hoặc ngược lại. Đây là lỗi im lặng: bảng vẫn dựng, vẫn chạy, chỉ sai chỗ — và chỉ lộ ra vào ngày tách module |
| E5 `NoForeignKey_CrossesSchemaBoundary` | ArchTest | Navigation property nối hai schema, tức FK vật lý xuyên ranh giới. Bắt ở tầng model; câu kiểm (4) ở [`schema-core.md`](schema-core.md) §11 bắt phần SQL viết tay |
| E6 `EveryMigration_LivesIn_ItsOwningProject` | ArchTest | Migration chạm schema `core` nằm trong project module ngoài ngoại lệ khoá quyền (§1), hoặc ngược lại — tức chính ca hai chủ sở hữu mà §1.1 mô tả |
| E8 `Startup_Fails_When_PendingMigrationsExist` | Integration test | App khởi động được trong khi DB còn thiếu migration. Xem [`script-runbook.md`](script-runbook.md) §5 |

Bốn dòng này nằm trong bảng §4 của [`../RULES.md`](../RULES.md), trạng thái `📐` — cổng chưa tồn
tại vì chưa có `src/`. Khi viết chúng ở giai đoạn 2, nhớ luật T1: **mỗi detector phải có test
kiểm chính detector đó**. Một ArchTest xanh vì nó không quét gì cả là tệ hơn không có ArchTest,
vì nó tạo cảm giác được bảo vệ.

### 7.1 E6 kiểm gì cho cụ thể

Detector đọc từng file migration, tìm tên schema xuất hiện trong nó, rồi đối chiếu với project
chứa file:

| File migration nằm ở | Được phép chạm schema |
| --- | --- |
| `Core.Infrastructure` | `core` — và chỉ `core` |
| `Modules.<X>.Infrastructure` | `<x>`; với `core` **chỉ** đúng dạng của ngoại lệ khoá quyền ở §1 |

Một migration của module chạm bảng `core` **ngoài** ngoại lệ đó là dấu hiệu module đang "sửa hộ" Core,
và bản vá Core lần sau sẽ đè lên nó hoặc xung đột với nó.

Ngoại lệ đi bằng SQL viết tay trong migration — dạng `ON CONFLICT … DO NOTHING` không có trong thao tác
chèn dữ liệu dựng sẵn của EF — nên phần này của detector buộc phải đọc văn bản lệnh SQL của migration
module: mọi tham chiếu `core.` chỉ được nằm trong câu `INSERT` vào hai bảng đã nêu, kèm
`ON CONFLICT … DO NOTHING`. Lời cảnh báo ngay dưới áp nguyên văn cho phần đó.

> ⚠️ **Bài học khi thi công detector này:** ở dự án tiền nhiệm, một detector cùng loại quét **văn
> bản nguồn** thay vì cấu trúc, nên code phải viết vòng để né nó — đuôi vẫy chó. Ưu tiên đọc
> model EF (`IModel` của từng `DbContext`) thay vì `grep` chuỗi trong file `.cs`. Nếu buộc phải
> quét văn bản, detector phải có test chứng minh nó bỏ qua comment và định danh.

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
