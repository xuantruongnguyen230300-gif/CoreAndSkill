---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng của `migration-policy.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`docs/database/migration-policy.md`](../../../database/migration-policy.md); luật, bảng và danh mục việc ở đó. Số mục dưới đây trùng số mục của file luật — mục không có gì dời thì ghi "—".

---

## 1. Ai sở hữu migration

—

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

**Vì sao dự án tiền nhiệm KHÔNG chọn được đường này:** họ giữ một `DbContext` duy nhất, và lý do
là **khoá ngoại xuyên schema**. EF chỉ sinh được FK trong phạm vi một model, nên FK từ bảng
nghiệp vụ sang bảng `core` đòi hai bên nằm chung context. Bỏ một context là mất FK đó, tức mất
một ràng buộc toàn vẹn.

Repo này **đã cấm FK xuyên schema từ đầu** (luật E5, [`schema-core.md`](../../../database/schema-core.md) §1.1).
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

## 2. Đánh đổi ghi thẳng — `Core.Infrastructure` nay biết provider là PostgreSQL

**Vì sao chấp nhận được** — hai lý do:

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
| 2 | Thay **unique index một phần** bằng cơ chế khác của provider mới. SQL Server có filtered index (tương đương); MySQL/Oracle **không có** ⇒ tính duy nhất phải chuyển thành cột sinh (`code_active = CASE WHEN is_deleted THEN NULL ELSE code END`) hoặc kiểm ở tầng ứng dụng | Nặng — chạm luật §3.3 của [`schema-core.md`](../../../database/schema-core.md) |
| 3 | Thay **`xmin`** bằng concurrency token của provider mới (`rowversion` ở SQL Server), tức **thêm một cột thật** vào mọi bảng và thêm một interceptor giữ nó | Nặng |
| 4 | Thay **`jsonb`** ở `outbox_message.payload` và `notification.params` bằng kiểu JSON tương ứng; mất phép kiểm cú pháp lúc ghi nếu provider mới chỉ có `text` | Trung bình |
| 5 | Rà lại mọi `migrationBuilder.Sql(...)` viết tay | Trung bình |
| 6 | Chạy lại **toàn bộ** integration test trên provider mới (luật T2 — DB thật, không mock) | Bắt buộc |

## 3. Quy ước đặt tên migration

—

### 3.1 Một migration làm MỘT việc

Vì sao đây là luật chứ không phải sở thích:

- **Rollback theo migration là bất khả thi khi một migration làm ba việc.** Cần hoàn tác đúng một
  việc trong ba, không có đường nào ngoài viết tay.
- **Đọc diff của một migration là cách rẻ nhất để bắt lỗi.** Một migration một việc thì bất
  thường lộ ra ngay; một migration ba việc thì `DropTable` lẫn giữa hai chục lệnh khác không ai
  thấy. Ở dự án tiền nhiệm, đúng ca này suýt xảy ra: snapshot lệch làm migration kế tiếp chứa
  năm lệnh `DropTable`, và **lưới an toàn duy nhất là người đọc phát hiện ra**.
- **Xung đột merge giải được.** Hai migration nhỏ độc lập merge được; hai migration to chồng lấn
  thì phải sinh lại.

## 4. Migration schema và migration dữ liệu — hai loại, hai chỗ

—

### 4.2 Backfill lớn KHÔNG nằm trong migration — bốn lý do

Ví dụ backfill lớn: điền `label_key` cho toàn bộ `menu_item` cũ, chuẩn hoá `created_by` từ id
sang tên đăng nhập trên vài triệu dòng.

| # | Vì sao không | Chi tiết |
| --- | --- | --- |
| 1 | **Khoá bảng lâu** | Migration chạy trong **một** transaction. Một `UPDATE` vài triệu dòng giữ khoá suốt thời gian đó — mọi ghi vào bảng ấy xếp hàng, và ứng dụng nhìn như treo. Với migration chạy lúc deploy, đó là downtime không ai lường trước |
| 2 | **Không rollback được** | Migration `Down` của một backfill hoặc không viết được (giá trị cũ đã mất), hoặc lại là một `UPDATE` khổng lồ nữa. Nút "hoàn tác" tồn tại trên giấy chứ không dùng được |
| 3 | **Không đo tiến độ được** | Migration là hộp đen: nó đang ở dòng thứ mấy trong ba triệu dòng, không có cách nào biết. Người vận hành chỉ có hai trạng thái — *"chưa xong"* và *"đã xong"* — và một cửa sổ chờ không biết dài bao lâu |
| 4 | **Không dừng và tiếp lại được** | Timeout ở giữa chừng thì transaction rollback sạch. Ba tiếng chạy đổi lấy con số không, và lần chạy lại cũng có ngần ấy rủi ro |

## 5. Thay đổi phá vỡ — quy trình bốn bước

—

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

## 7. Cổng nào canh những luật này

—

## 8. Danh mục việc phải làm khi đổi schema `core`

—
