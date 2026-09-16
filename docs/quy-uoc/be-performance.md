---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quy ước Backend — repository, query, index, cache

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi code mẫu là khuôn cho `src/BE` sẽ xây ở giai đoạn 2.
>
> Đọc file này khi viết repository/query mới, hoặc khi nhận bất kỳ task nào có chữ
> *"chậm"* / *"tối ưu"* / *"cache"*.
>
> 📖 Kiến thức nền, ngưỡng áp dụng và cách đo: [`../wiki-core/be/11-performance-caching.md`](../wiki-core/be/11-performance-caching.md).
> Lý do, bẫy, ví dụ mở rộng của từng mục — cùng số §: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md).

---

## 1. Thứ tự bắt buộc — không nhảy cóc

```
query pattern  →  index  →  ĐO LẠI  →  thuật toán  →  ĐO LẠI  →  cache
```

Cache đặt trước ba bước đầu chỉ **che** lỗi chứ không sửa.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §1

---

## 2. Repository — ranh giới và hình dạng

### 2.1 Interface ở Application, implementation ở Infrastructure

```csharp
// Core.Application/Menu/IMenuItemRepository.cs
public interface IMenuItemRepository
{
    Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);
    Task<PagedList<MenuItemListItemDto>> SearchAsync(MenuItemSearchCriteria criteria, CancellationToken ct);
    Task AddAsync(MenuItem item, CancellationToken ct);
    void Remove(MenuItem item);
}
```

Không method nào nhận tham số đơn vị: `MenuItem` là entity `ITenantScoped`, bộ lọc tenant đã giới hạn
mọi truy vấn trong đơn vị hiện hành ([`be-entity-domain.md`](be-entity-domain.md) §5.1).

| Luật | Vì sao |
| --- | --- |
| Interface khai ở **Application**, cạnh feature dùng nó | Vertical slice; và Application không được biết Infrastructure |
| **Không** `IRepository<T>` tổng quát | Nó buộc mọi entity mang cùng một bề mặt — vi phạm ISP |
| Repository **không** tự `SaveChangesAsync` | `TransactionBehavior` sở hữu điểm commit — [`be-cqrs-handler.md`](be-cqrs-handler.md) §5.3 |
| Method trả về **entity** hoặc **DTO đã projection**, không trả `IQueryable` | Xem §2.2 |
| `Remove` là `void` | Nó chỉ đánh dấu trên `ChangeTracker`; đặt `async` cho nó là nói dối về việc có I/O |

### 2.2 `IQueryable` không được rò ra khỏi Application

> **Không method public nào trả `IQueryable<T>`.** Handler viết `.Include(...)` hay
> `EF.Functions.ILike(...)` là đang viết code EF Core trong tầng Application.

Repository nhận một **criteria object** và trả kết quả đã materialize:

```csharp
// Core.Application/Menu/MenuItemSearchCriteria.cs
public sealed record MenuItemSearchCriteria(
    string? SearchText,
    Guid? ParentId,
    int Page,
    int PageSize,
    MenuItemSortField SortBy,
    bool SortDescending);

public enum MenuItemSortField { Code, DisplayOrder, CreatedAt }
```

`SortBy` là **enum**, không phải chuỗi — cách rẻ nhất để cưỡng chế allowlist sắp xếp
([`be-cqrs-handler.md`](be-cqrs-handler.md) §9.3).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §2.2

---

## 3. Projection thay vì load cả entity

```csharp
// ✅ Projection — chỉ những cột thật sự cần rời khỏi DB
return await db.MenuItems
    .AsNoTracking()
    .Where(m => m.ParentId == null)
    .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Id)
    .Select(m => new MenuItemListItemDto(m.Id, m.Code, m.LabelKey))
    .ToListAsync(ct);
```

**Với projection, `AsNoTracking()` là thừa** — giữ lại vẫn vô hại và làm rõ ý định; đừng coi việc thiếu
nó ở một câu projection là finding.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §3

### 3.1 `AsNoTracking` — luật hai chiều

| Query | `AsNoTracking()` |
| --- | --- |
| Chỉ đọc, không sửa gì sau đó | **Có** |
| Lấy entity ra **để sửa rồi lưu** | **KHÔNG** — thay đổi sẽ không được ghi |

**Đọc call-site trước khi thêm**, đừng áp `AsNoTracking()` hàng loạt bằng find-replace. Đừng bật
`QueryTrackingBehavior.NoTracking` ở mức `DbContext`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §3.1

---

## 4. Chống N+1

### 4.1 Ba khuôn sinh ra N+1

```csharp
// ❌ 1. await trong vòng lặp
foreach (var id in ids)
    result.Add(await repo.GetByIdAsync(id, ct));

// ❌ 2. Lazy loading chạm navigation property sau khi đã materialize
var orders = await db.Orders.ToListAsync(ct);
foreach (var o in orders)
    Console.WriteLine(o.Customer.Name);   // mỗi vòng một câu SELECT

// ❌ 3. Gọi service trong Select
var dtos = users.Select(u => new UserDto(u.Id, lookup.GetRoleName(u.RoleId))).ToList();
```

### 4.2 Cách sửa

```csharp
// ✅ 1. Một câu, lọc theo tập
var users = await db.Users
    .AsNoTracking()
    .Where(u => ids.Contains(u.Id))
    .Select(u => new UserListItemDto(u.Id, u.UserName, u.FullName))
    .ToListAsync(ct);

// ✅ 2. Projection kéo luôn dữ liệu liên quan
var orders = await db.Orders
    .AsNoTracking()
    .Select(o => new OrderListItemDto(o.Id, o.Code, o.Customer.Name))
    .ToListAsync(ct);
```

Khi thật sự cần entity kèm collection con: `Include` + `AsSplitQuery()` — chấp nhận được với đường đọc
thuần.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §4.2

### 4.3 Phát hiện N+1

Ba cách, xếp theo thứ tự nên áp dụng:

1. **Bật log SQL ở Development.** `EnableSensitiveDataLogging()` **chỉ** ở Development.
2. **Đếm câu lệnh trong integration test** — `DbCommandInterceptor` đếm `ReaderExecuting`, test khẳng
   định một trần:

```csharp
// Tests/.../CommandCountingInterceptor.cs
public sealed class CommandCountingInterceptor : DbCommandInterceptor
{
    private int _count;
    public int Count => _count;
    public void Reset() => Interlocked.Exchange(ref _count, 0);

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Interlocked.Increment(ref _count);
        return base.ReaderExecuting(command, eventData, result);
    }
}
```

```csharp
[Fact]
public async Task GetUsersList_Issues_AtMost_TwoQueries()
{
    interceptor.Reset();
    await client.GetAsync("/api/v1/core/users?page=1&pageSize=50");
    Assert.True(interceptor.Count <= 2, $"Đã phát {interceptor.Count} câu lệnh — dấu hiệu N+1.");
}
```

   Trần "2" là câu đếm + câu lấy trang.

3. **Xem `EXPLAIN (ANALYZE, BUFFERS)`** cho câu chậm nhất khi đã có dữ liệu thật.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §4.3

---

## 5. Index trên PostgreSQL

### 5.1 Đặt ở đâu

Index khai trong `IEntityTypeConfiguration<T>`, **cùng project với entity**, không khai rải trong
migration viết tay. Migration là **kết quả** sinh ra từ model, không phải nguồn.

```csharp
// Core.Infrastructure/Persistence/Configurations/MenuItemConfiguration.cs
public sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_item", CoreSchema.Name);

        builder.HasIndex(m => new { m.TenantId, m.Code })
               .IsUnique()
               .HasFilter("is_deleted = false")
               .HasDatabaseName("ux_menu_item_tenant_code_active");

        builder.HasIndex(m => new { m.TenantId, m.ParentId, m.DisplayOrder })
               .HasDatabaseName("ix_menu_item_tenant_parent_order");
    }
}
```

Tên bảng, tên cột trong `HasFilter` và tên index lấy **nguyên văn** từ
[`../database/schema-core.md`](../database/schema-core.md) §6.1. Chuỗi trong `HasFilter` là SQL thô — tên
**vật lý** của cột, không phải tên property — [`be-entity-domain.md`](be-entity-domain.md) §5.3.

### 5.2 Bảy quy tắc

1. **Mỗi predicate lọc nóng phải có index dẫn đầu đúng cột đó** — `(A, B)` **không** seek được cho
   query chỉ lọc theo `B`.
2. **Thứ tự cột: lọc bằng `=` trước, khoảng/sắp xếp sau.**
3. **Index unique trên bảng có soft delete PHẢI có `HasFilter`** — [`be-entity-domain.md`](be-entity-domain.md) §5.3.
4. **Tìm chuỗi không phân biệt hoa thường** dùng `ILIKE` + index `gin` (`pg_trgm`), không `LOWER(col) = LOWER(@p)`.
5. **Cột JSON truy vấn thường xuyên** dùng `jsonb` + index `gin`; khoá bên trong JSON lọc thường xuyên
   thì nên là **cột thật**.
6. **Index có giá** — làm chậm mọi `INSERT`/`UPDATE`. Không thêm index vì "có thể sau này cần".
7. **Đặt tên tường minh** khi index cần được nhắc tới ở nơi khác (runbook, script vận hành).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §5.2

### 5.3 Cột `CreatedAt` và múi giờ

Dùng `DateTimeOffset` ở CLR và `timestamptz` ở PostgreSQL (mặc định của Npgsql). **Luôn ghi UTC.**
Chuyển sang giờ địa phương là việc của tầng hiển thị.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §5.3

---

## 6. Phân trang — offset và keyset

### 6.1 Offset (`Skip`/`Take`) — mặc định

```csharp
var query = db.Users.AsNoTracking().Where(/* filter */);

var total = await query.CountAsync(ct);

var items = await query
    .OrderBy(u => u.UserName).ThenBy(u => u.Id)
    .Skip((criteria.Page - 1) * criteria.PageSize)
    .Take(criteria.PageSize)
    .Select(u => new UserListItemDto(u.Id, u.UserName, u.FullName))
    .ToListAsync(ct);

return new PagedList<UserListItemDto>
{
    Items = items, Page = criteria.Page, PageSize = criteria.PageSize, TotalCount = total,
};
```

**Bắt buộc có tiêu chí sắp xếp phụ ổn định** (`ThenBy(u => u.Id)`).

### 6.2 Khi nào offset không còn dùng được

Ngưỡng chuyển sang keyset — thoả **một** là đủ:

- Bảng vượt vài trăm nghìn dòng **và** người dùng thật sự lật tới các trang sâu.
- Đây là API cuộn vô hạn (mobile, infinite scroll).
- Dữ liệu thay đổi liên tục trong lúc người dùng lật trang.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §6

### 6.3 Keyset

```csharp
// Cursor = giá trị sắp xếp của bản ghi cuối trang trước
public sealed record UserCursor(string UserName, Guid Id);

var query = db.Users.AsNoTracking().Where(/* filter */);

if (cursor is not null)
{
    query = query.Where(u =>
        string.Compare(u.UserName, cursor.UserName) > 0 ||
        (u.UserName == cursor.UserName && u.Id > cursor.Id));
}

var items = await query
    .OrderBy(u => u.UserName).ThenBy(u => u.Id)
    .Take(pageSize)
    .Select(u => new UserListItemDto(u.Id, u.UserName, u.FullName))
    .ToListAsync(ct);
```

| | Offset | Keyset |
| --- | --- | --- |
| Chi phí trang thứ N | Tăng theo N | Không đổi |
| Nhảy tới trang bất kỳ | Có | **Không** — chỉ đi tiếp/lùi |
| Tổng số trang | Có (`CountAsync`) | Thường bỏ, vì `COUNT` mới là câu đắt |
| Dữ liệu đổi giữa chừng | Bỏ sót / lặp bản ghi | Ổn định |

Keyset **không phải mặc định**: chọn nó khi một trong ba ngưỡng ở §6.2 thoả, không phải để cho nhanh.

---

## 7. Batching và cấu hình kết nối

### 7.1 Ghi nhiều bản ghi

```csharp
// ❌ SaveChanges mỗi dòng — N round-trip, N transaction
foreach (var row in rows) { db.Add(Map(row)); await db.SaveChangesAsync(ct); }

// ✅ Một lượt — EF gộp thành ít câu lệnh, một transaction
db.AddRange(rows.Select(Map));
await db.SaveChangesAsync(ct);
```

Khối lượng rất lớn (từ hàng chục nghìn dòng): chia lô cố định, `ChangeTracker.Clear()` sau mỗi lô;
ngưỡng phải **đo** — và việc đó thuộc về job nền ([`be-cqrs-handler.md`](be-cqrs-handler.md) §10).

Cập nhật/xoá hàng loạt theo điều kiện: `ExecuteUpdateAsync` / `ExecuteDeleteAsync`. Chúng **bỏ qua
`ChangeTracker`** và interceptor — `AuditInterceptor` **không** chạy, phải tự set cột vết trong câu
`ExecuteUpdate`. `ExecuteDeleteAsync` là **xoá cứng** — entity có soft delete dùng `ExecuteUpdateAsync`
đặt `IsDeleted = true`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §7.1

### 7.2 Cấu hình `UseNpgsql`

```csharp
services.AddDbContext<CoreDbContext>((sp, options) => options
    .UseNpgsql(sp.GetRequiredService<DbConnection>(), npgsql =>
    {
        npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        npgsql.CommandTimeout(30);
        npgsql.MigrationsHistoryTable("__ef_migrations_history", CoreSchema.Name);
    })
    .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
```

| Thiết lập | Vì sao khai tường minh |
| --- | --- |
| `DbConnection` dùng chung thay vì chuỗi kết nối | Mọi `DbContext` của một request đi chung một kết nối để đứng chung một transaction — [`be-cqrs-handler.md`](be-cqrs-handler.md) §4 |
| `EnableRetryOnFailure` | Một nhịp chớp mạng giữa app và Postgres không được biến thành 500 cho người dùng |
| `CommandTimeout(30)` | Bằng đúng mặc định — khai ra để nó là con số **đã cân nhắc**. Hạ xuống khi có số đo p99 thật |
| `MigrationsHistoryTable` trong schema `core` | Core sở hữu lịch sử migration của mình; module có bảng riêng — [`../database/migration-policy.md`](../database/migration-policy.md) |

> ⚠️ **Bẫy đi kèm `EnableRetryOnFailure`:** chiến lược thử lại **không** bọc được transaction do code tự
> mở — ném `InvalidOperationException` lúc **chạy**. Transaction gom về `IUnitOfWork.ExecuteInTransactionAsync`
> — [`be-cqrs-handler.md`](be-cqrs-handler.md) §4.

---

## 8. Cache — CHƯA làm ở v1

### 8.1 Trạng thái và lý do

> 📐 **Redis và `CachingBehavior` KHÔNG có ở v1.** Không phải bỏ sót — chưa đo được vấn đề nào để cache
> giải quyết. Quyết định về số lượng behavior ở v1:
> [`../adr/0006-pipeline-behavior.md`](../adr/0006-pipeline-behavior.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §8.1

### 8.2 Interface khai trước — để đổi implementation không sửa call site

```csharp
// Core.Application/Common/Interfaces/ICacheStore.cs
public interface ICacheStore
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);

    ValueTask SetAsync<T>(string key, T value, TimeSpan ttl,
                          IReadOnlyCollection<string>? tags = null, CancellationToken ct = default);

    ValueTask RemoveAsync(string key, CancellationToken ct = default);

    ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default);
}
```

Bốn method, không hơn. Implementation ở v1: **không có**; chỗ nào thật sự cần trước thì đăng ký một
implementation **no-op** (luôn miss, `Set` không làm gì). `Core.Application` **không bao giờ** chạm thẳng
`HybridCache`/`IMemoryCache`/`IDistributedCache` — chỉ qua `ICacheStore`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §8.2

### 8.3 Ba điều kiện bắt buộc trước khi thêm bất kỳ cache nào

Thiếu **một** thì dừng lại và hỏi, không tự quyết:

1. **Số đo** chứng minh chỗ đó tốn thật — thời gian, tần suất, tỉ lệ so với tổng thời gian request.
2. **Danh sách đầy đủ đường ghi phải invalidate**, kể cả job nền (không có `HttpContext` — chỗ dễ quên nhất).
3. **Test xác nhận invalidation chạy**, không chỉ test cache hit.

> **Cache dữ liệu phân quyền mà chỉ dựa TTL → quyền đã thu hồi còn hiệu lực tới hết TTL.** Đó là lỗ hổng
> bảo mật. Nếu cache phân quyền, invalidation phải **đồng bộ** trong cùng lượt ghi ma trận.

### 8.4 Cái gì được cache mà không cần ba điều kiện trên

`ConcurrentDictionary` cache **metadata bất biến trong một process** — kết quả phân tích assembly, bảng
ánh xạ dựng một lần lúc khởi động. `static Dictionary` giữ dữ liệu **từ DB** thì **không** hợp lệ.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §8.4

---

## 9. Khi sửa code tính toán nghiệp vụ

Tối ưu code tính ra con số hiển thị cho người dùng: output phải **giống hệt** trước khi sửa, trên cùng
dữ liệu — đối chiếu bằng một test so sánh. Khuôn an toàn: giữ implementation cũ tạm thời, chạy cả hai
trên cùng tập dữ liệu, so sánh, rồi mới xoá bản cũ.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §9

---

## 10. Checklist khi viết một repository/query mới

Áp ngay, không chờ ai nhắc:

- [ ] Method **không** trả `IQueryable`.
- [ ] Query chỉ đọc có `AsNoTracking()`; query lấy entity để sửa thì **không** có.
- [ ] Dùng projection `Select` thay vì load cả entity, trừ khi thật sự cần entity để sửa.
- [ ] Mọi predicate lọc nóng có index dẫn đầu đúng cột đó.
- [ ] Index unique trên bảng soft delete có `HasFilter`.
- [ ] `Distinct` / `GroupBy` / `Count` / phân trang chạy ở **SQL**, không `ToListAsync()` rồi làm trong C#.
- [ ] Không `await` trong vòng lặp.
- [ ] Sắp xếp có tiêu chí phụ ổn định (`ThenBy(x => x.Id)`).
- [ ] `SortBy` đến từ enum hoặc allowlist, không phải chuỗi client gửi thẳng vào `OrderBy`.
- [ ] Nếu bỏ qua một mục trên: comment nêu **con số** trần trên và điều kiện làm nó hết đúng.
      *"Dataset hiện tại nhỏ"* suông **không** phải ngoại lệ hợp lệ.
