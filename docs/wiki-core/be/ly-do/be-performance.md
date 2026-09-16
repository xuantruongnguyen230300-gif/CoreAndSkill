---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng — `be-performance.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`../../../quy-uoc/be-performance.md`](../../../quy-uoc/be-performance.md);
> luật ở đó. Số mục ở đây **trùng số §** của file luật; mục không có gì dời thì ghi "—".
>
> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi khối code là khuôn cho `src/BE` sẽ xây ở giai đoạn 2.

---

## 1. Thứ tự bắt buộc — không nhảy cóc

Cache đặt trước ba bước đầu: lần miss vẫn chậm y hệt, seq scan vẫn nguyên, N+1 vẫn nguyên — và giờ có
thêm một tầng nữa để debug khi số liệu hiển thị sai.

---

## 2. Repository — ranh giới và hình dạng

### 2.1 Interface ở Application, implementation ở Infrastructure

`IRepository<T>` tổng quát buộc mọi entity mang cùng một bề mặt, và phần lớn implementation sẽ có
method không dùng.

### 2.2 `IQueryable` không được rò ra khỏi Application

Bốn hậu quả nếu để rò, xếp theo mức độ khó phát hiện:

1. **Query chạy ở nơi không ai biết.** Một `IQueryable` trả về từ repository sẽ được materialize ở
   handler, ở controller, hoặc — tệ nhất — trong vòng lặp render.
2. **Ranh giới tầng vỡ mà ArchTest không bắt.** Handler viết `.Include(...)`,
   `.Where(x => EF.Functions.ILike(...))` là đang viết code EF Core trong tầng Application, dù nó không
   `using` namespace nào bị cấm.
3. **Không test được nếu không có DB.** `IQueryable` của EF khác `IQueryable` của `List<T>` ở đúng những
   chỗ quan trọng (translation, null semantics, so sánh chuỗi).
4. **Không kiểm soát được vòng đời `DbContext`.** Query chạy sau khi scope đã đóng thì ném
   `ObjectDisposedException` — ở một chỗ cách xa nguyên nhân.

`SortBy` là enum: giá trị ngoài danh sách không deserialize được, nên nó không bao giờ tới được câu SQL.

---

## 3. Projection thay vì load cả entity

```csharp
// ❌ Load toàn bộ entity rồi map ở C#
var items = await db.MenuItems.Where(m => m.ParentId == null).ToListAsync(ct);
return items.Select(m => new MenuItemListItemDto(m.Id, m.Code, m.LabelKey)).ToList();
```

Ba thứ nhánh sai trả giá, và không thứ nào lộ ra ở màn hình:

| | Load cả entity | Projection |
| --- | --- | --- |
| Cột đọc từ DB | Tất cả, gồm cả cột lớn không dùng | Đúng ba cột |
| Bộ nhớ `ChangeTracker` | Mọi entity được theo dõi (nếu quên `AsNoTracking`) | Không có gì để theo dõi |
| Có dùng được index-only scan không | Không | Có, nếu index phủ đủ cột |

Với projection, `AsNoTracking()` thừa vì kết quả không phải entity nên không có gì để track.

### 3.1 `AsNoTracking` — luật hai chiều

Vế "lấy entity ra để sửa rồi lưu" là một **lỗi im lặng**: không exception, không cảnh báo, chỉ là dữ
liệu không đổi. Vì vậy phải đọc call-site trước khi thêm. `QueryTrackingBehavior.NoTracking` ở mức
`DbContext` đảo mặc định, nên mọi đường ghi phải nhớ bật lại tracking — và chỗ quên sẽ hỏng im lặng
đúng như trên.

---

## 4. Chống N+1

### 4.1 Ba khuôn sinh ra N+1

—

### 4.2 Cách sửa

Một `Include` trên collection sinh tích Descartes — cột của bảng cha lặp lại theo số dòng con, và với
hai collection thì nhân lên lần nữa. `AsSplitQuery` đổi một câu khổng lồ lấy vài câu nhỏ.

Đánh đổi của `AsSplitQuery`: các câu chạy ở thời điểm khác nhau nên **không** thấy cùng một ảnh chụp dữ
liệu, trừ khi nằm trong transaction. Với đường đọc thuần, chấp nhận được.

### 4.3 Phát hiện N+1

`EnableSensitiveDataLogging()` in cả giá trị tham số, gồm dữ liệu cá nhân, nên không được lọt ra
Production.

Test đếm câu lệnh bắt được hồi quy mà mắt không bắt được: một `Include` thêm vào sáu tháng sau làm số
câu nhảy lên hàng chục.

---

## 5. Index trên PostgreSQL

### 5.1 Đặt ở đâu

—

### 5.2 Bảy quy tắc

- *Thứ tự cột* — `(tenant_id, parent_id, display_order)` phục vụ được
  `WHERE tenant_id = @t AND parent_id = @p ORDER BY display_order`; đặt `display_order` lên đầu thì không.
- *Tìm chuỗi* — `LOWER(col) = LOWER(@p)` có hàm ở vế trái, nên index thường vô dụng.
- *Index có giá* — mỗi index làm chậm mọi `INSERT`/`UPDATE` trên bảng và chiếm dung lượng. Thêm index
  vì "có thể sau này cần" là trả chi phí chắc chắn cho lợi ích giả định.
- *Đặt tên tường minh* — tên EF sinh tự động đổi khi đổi tên property.

### 5.3 Cột `CreatedAt` và múi giờ

`timestamp` (không `tz`) lưu một thời điểm **không xác định được** khi hệ thống có người dùng ở nhiều
múi giờ, hoặc khi server đổi múi giờ.

---

## 6. Phân trang — offset và keyset

### 6.1 Offset (`Skip`/`Take`) — mặc định

Không có tiêu chí sắp xếp phụ ổn định, các bản ghi trùng giá trị sắp xếp có thứ tự **không xác định**
giữa hai lần chạy — cùng một bản ghi xuất hiện ở trang 1 và trang 2, một bản ghi khác không xuất hiện ở
đâu cả.

### 6.2 Khi nào offset không còn dùng được

`OFFSET n` buộc PostgreSQL **đọc và bỏ** n dòng đầu. Chi phí tăng tuyến tính theo số trang: trang 1000
với `pageSize = 50` nghĩa là đọc 50.000 dòng để trả về 50.

Ba ngưỡng, giải thích thêm: với API cuộn vô hạn, "trang tiếp theo" mới là thao tác thật, còn "nhảy tới
trang 500" thì không tồn tại. Với dữ liệu thay đổi liên tục, offset **bỏ sót và lặp lại** bản ghi, vì
cửa sổ dịch chuyển dưới chân.

### 6.3 Keyset

Keyset không phải mặc định vì nó bỏ mất khả năng nhảy trang mà UI dạng bảng của trang quản trị đang
dùng.

---

## 7. Batching và cấu hình kết nối

### 7.1 Ghi nhiều bản ghi

Với khối lượng rất lớn, `SaveChanges` một lượt cũng không phù hợp: `ChangeTracker` phình và bộ nhớ tăng
tuyến tính. `ExecuteUpdateAsync` / `ExecuteDeleteAsync` sinh một câu UPDATE/DELETE duy nhất, không load
entity.

### 7.2 Cấu hình `UseNpgsql`

`EnableRetryOnFailure` không bọc được transaction do code tự mở: nó ném `InvalidOperationException` lúc
**chạy**, không lúc biên dịch, và chỉ trên đúng đường ghi đó. Đây là một trong ba lý do transaction gom
về `IUnitOfWork.ExecuteInTransactionAsync` — [`ly-do/be-cqrs-handler.md`](be-cqrs-handler.md) §3.3.

---

## 8. Cache — CHƯA làm ở v1

### 8.1 Trạng thái và lý do

Ba lý do, xếp theo mức độ quyết định:

1. **Chưa có số đo.** Không có một query nào được chứng minh là chậm trên dữ liệu thật. Thêm cache lúc
   này là tối ưu một thứ chưa biết có tốn hay không.
2. **Cache là một nguồn sự thật thứ hai.** Mọi bug từ đó về sau đều có thêm câu hỏi *"dữ liệu này cũ hay
   mới?"*, và câu hỏi đó tốn thời gian ngay cả khi câu trả lời là "mới".
3. **Hệ chạy một process.** Nếu sau này thật sự cần cache, mức đầu tiên là in-process — Redis chỉ cần
   khi có từ hai process trở lên đọc chung một tập dữ liệu.

### 8.2 Interface khai trước — để đổi implementation không sửa call site

`RemoveByTagAsync` có mặt ngay từ đầu **có chủ đích**: invalidation theo nhóm là thứ khó thêm sau, vì
thêm sau nghĩa là phải đi sửa mọi lời gọi `SetAsync` để bổ sung tag. Implementation no-op đúng về mặt
ngữ nghĩa và giữ call site không phải đổi khi implementation thật xuất hiện.

### 8.3 Ba điều kiện bắt buộc trước khi thêm bất kỳ cache nào

—

### 8.4 Cái gì được cache mà không cần ba điều kiện trên

Metadata bất biến trong một process có nguồn là chính assembly nên nó không bao giờ cũ. `static
Dictionary` giữ dữ liệu từ DB thì không eviction, không invalidation, và không ai gọi nó là cache nên
không ai nghĩ tới việc xoá nó.

---

## 9. Khi sửa code tính toán nghiệp vụ

Đây là con số người dùng nhìn thấy, không phải chi tiết nội bộ. Chi phí của khuôn an toàn là một buổi;
cái tránh được là một sai số không ai phát hiện cho tới khi có người đối chiếu với sổ sách bên ngoài.

---

## 10. Checklist khi viết một repository/query mới

—
