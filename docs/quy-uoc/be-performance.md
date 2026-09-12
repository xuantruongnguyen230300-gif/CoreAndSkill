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

---

## 1. Thứ tự bắt buộc — không nhảy cóc

```
query pattern  →  index  →  ĐO LẠI  →  thuật toán  →  ĐO LẠI  →  cache
```

Cache đặt trước ba bước đầu chỉ **che** lỗi chứ không sửa: lần miss vẫn chậm y hệt, seq
scan vẫn nguyên, N+1 vẫn nguyên — và giờ có thêm một tầng nữa để debug khi số liệu hiển
thị sai.

---

## 2. Repository — ranh giới và hình dạng

### 2.1 Interface ở Application, implementation ở Infrastructure

```csharp
// Core.Application/Users/IUserRepository.cs
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<PagedList<UserListItemDto>> SearchAsync(UserSearchCriteria criteria, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    void Remove(User user);
}
```

| Luật | Vì sao |
| --- | --- |
| Interface khai ở **Application**, cạnh feature dùng nó | Vertical slice; và Application không được biết Infrastructure |
| **Không** `IRepository<T>` tổng quát | Nó buộc mọi entity mang cùng một bề mặt, và phần lớn implementation sẽ có method không dùng — vi phạm ISP |
| Repository **không** tự `SaveChangesAsync` | `TransactionBehavior` sở hữu điểm commit — [`be-cqrs-handler.md`](be-cqrs-handler.md) §5.3 |
| Method trả về **entity** hoặc **DTO đã projection**, không trả `IQueryable` | Xem §2.2 |
| `Remove` là `void` | Nó chỉ đánh dấu trên `ChangeTracker`; đặt `async` cho nó là nói dối về việc có I/O |

### 2.2 `IQueryable` không được rò ra khỏi Application

> **Không method public nào trả `IQueryable<T>`.**

Bốn hậu quả nếu để rò, xếp theo mức độ khó phát hiện:

1. **Query chạy ở nơi không ai biết.** Một `IQueryable` trả về từ repository sẽ được
   materialize ở handler, ở controller, hoặc — tệ nhất — trong vòng lặp render.
2. **Ranh giới tầng vỡ mà ArchTest không bắt.** Handler viết `.Include(...)`,
   `.Where(x => EF.Functions.ILike(...))` là đang viết code EF Core trong tầng
   Application, dù nó không `using` namespace nào bị cấm.
3. **Không test được nếu không có DB.** `IQueryable` của EF khác `IQueryable` của
   `List<T>` ở đúng những chỗ quan trọng (translation, null semantics, so sánh chuỗi).
4. **Không kiểm soát được vòng đời `DbContext`.** Query chạy sau khi scope đã đóng thì ném
   `ObjectDisposedException` — ở một chỗ cách xa nguyên nhân.

Cách đúng: repository nhận một **criteria object** và trả kết quả đã materialize.

```csharp
// Core.Application/Users/UserSearchCriteria.cs
public sealed record UserSearchCriteria(
    string? Keyword,
    bool? IsLocked,
    IReadOnlyCollection<string>? RoleNames,
    int Page,
    int PageSize,
    UserSortField SortBy,
    bool SortDescending);

public enum UserSortField { UserName, FullName, CreatedAt }
```

`SortBy` là **enum**, không phải chuỗi. Đây là cách rẻ nhất để cưỡng chế allowlist sắp
xếp ([`be-cqrs-handler.md`](be-cqrs-handler.md) §9.3): giá trị ngoài danh sách không
deserialize được, nên nó không bao giờ tới được câu SQL.

---

## 3. Projection thay vì load cả entity

```csharp
// ❌ Load toàn bộ entity rồi map ở C#
var users = await db.Users.Where(u => !u.IsLocked).ToListAsync(ct);
return users.Select(u => new UserListItemDto(u.Id, u.UserName, u.FullName)).ToList();
```

```csharp
// ✅ Projection — chỉ những cột thật sự cần rời khỏi DB
return await db.Users
    .AsNoTracking()
    .Where(u => !u.IsLocked)
    .OrderBy(u => u.UserName).ThenBy(u => u.Id)
    .Select(u => new UserListItemDto(u.Id, u.UserName, u.FullName))
    .ToListAsync(ct);
```

Ba thứ nhánh sai trả giá, và không thứ nào lộ ra ở màn hình:

| | Load cả entity | Projection |
| --- | --- | --- |
| Cột đọc từ DB | Tất cả, gồm cả cột lớn không dùng | Đúng ba cột |
| Bộ nhớ `ChangeTracker` | Mọi entity được theo dõi (nếu quên `AsNoTracking`) | Không có gì để theo dõi |
| Có dùng được index-only scan không | Không | Có, nếu index phủ đủ cột |

**Với projection, `AsNoTracking()` là thừa** — kết quả không phải entity nên không có gì
để track. Giữ nó lại vẫn vô hại và làm rõ ý định; nhưng đừng coi việc thiếu nó ở một câu
projection là finding.

### 3.1 `AsNoTracking` — luật hai chiều

| Query | `AsNoTracking()` |
| --- | --- |
| Chỉ đọc, không sửa gì sau đó | **Có** |
| Lấy entity ra **để sửa rồi lưu** | **KHÔNG** — thay đổi sẽ không được ghi |

Vế thứ hai là một **lỗi im lặng**: không exception, không cảnh báo, chỉ là dữ liệu không
đổi. Vì vậy **đọc call-site trước khi thêm**, đừng áp `AsNoTracking()` hàng loạt bằng
find-replace.

Đừng bật `QueryTrackingBehavior.NoTracking` ở mức `DbContext`: nó đảo mặc định, nên mọi
đường ghi phải nhớ bật lại tracking — và chỗ quên sẽ hỏng im lặng đúng như trên.

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

Khi thật sự cần entity kèm collection con: `Include` + `AsSplitQuery()`. Một `Include`
trên collection sinh tích Descartes — cột của bảng cha lặp lại theo số dòng con, và với
hai collection thì nhân lên lần nữa. `AsSplitQuery` đổi một câu khổng lồ lấy vài câu nhỏ.

Đánh đổi của `AsSplitQuery`: các câu chạy ở thời điểm khác nhau nên **không** thấy cùng
một ảnh chụp dữ liệu, trừ khi nằm trong transaction. Với đường đọc thuần, chấp nhận được.

### 4.3 Phát hiện N+1

Ba cách, xếp theo thứ tự nên áp dụng:

1. **Bật log SQL ở Development.** `EnableSensitiveDataLogging()` **chỉ** ở Development —
   nó in cả giá trị tham số, gồm dữ liệu cá nhân, nên không được lọt ra Production.
2. **Đếm câu lệnh trong integration test.** Một `DbCommandInterceptor` đếm số lần
   `ReaderExecuting` trong một request, và test khẳng định một trần:

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

   Trần "2" là câu đếm + câu lấy trang. Test này bắt được hồi quy mà mắt không bắt được:
   một `Include` thêm vào sáu tháng sau làm số câu nhảy lên hàng chục.

3. **Xem `EXPLAIN (ANALYZE, BUFFERS)`** cho câu chậm nhất khi đã có dữ liệu thật.

---

## 5. Index trên PostgreSQL

### 5.1 Đặt ở đâu

Index khai trong `IEntityTypeConfiguration<T>`, **cùng project với entity**, không khai
rải trong migration viết tay. Migration là **kết quả** sinh ra từ model, không phải nguồn.

```csharp
// Core.Infrastructure/Persistence/Configurations/UserConfiguration.cs
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", CoreSchema.Name);

        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(u => new { u.IsLocked, u.UserName });

        builder.HasIndex(u => u.CreatedAt)
               .HasDatabaseName("ix_users_created_at");
    }
}
```

### 5.2 Bảy quy tắc

1. **Mỗi predicate lọc nóng phải có index dẫn đầu đúng cột đó.** Index `(A, B)` **không**
   seek được cho query chỉ lọc theo `B`.
2. **Thứ tự cột: lọc bằng `=` trước, khoảng/sắp xếp sau.** Index `(IsLocked, UserName)`
   phục vụ được `WHERE IsLocked = false ORDER BY UserName`; đảo lại thì không.
3. **Index unique trên bảng có soft delete PHẢI có `HasFilter`.** Không có nó, lần chèn
   lại một giá trị đã xoá mềm sẽ trùng khoá — [`be-entity-domain.md`](be-entity-domain.md) §5.3.
4. **Tìm kiếm chuỗi không dấu phân biệt hoa thường** dùng `ILIKE` với index `gin` +
   `pg_trgm`, không dùng `LOWER(col) = LOWER(@p)` — vế trái có hàm thì index thường vô
   dụng.
5. **Cột JSON truy vấn thường xuyên** dùng `jsonb` + index `gin`. Nhưng nếu một khoá bên
   trong JSON được lọc thường xuyên, đó là dấu hiệu nó nên là một **cột thật**.
6. **Index có giá.** Mỗi index làm chậm mọi `INSERT`/`UPDATE` trên bảng và chiếm dung
   lượng. Thêm index vì "có thể sau này cần" là trả chi phí chắc chắn cho lợi ích giả định.
7. **Đặt tên tường minh** khi index cần được nhắc tới ở nơi khác (runbook, script vận
   hành). Tên EF sinh tự động đổi khi đổi tên property.

### 5.3 Cột `CreatedAt` và múi giờ

Dùng `DateTimeOffset` ở CLR và `timestamptz` ở PostgreSQL. Npgsql ánh xạ mặc định như
vậy, và nó là lựa chọn đúng: `timestamp` (không `tz`) lưu một thời điểm **không xác định
được** khi hệ thống có người dùng ở nhiều múi giờ, hoặc khi server đổi múi giờ.

**Luôn ghi UTC.** Chuyển sang giờ địa phương là việc của tầng hiển thị.

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

**Bắt buộc có tiêu chí sắp xếp phụ ổn định** (`ThenBy(u => u.Id)`). Không có nó, các bản
ghi trùng giá trị sắp xếp có thứ tự **không xác định** giữa hai lần chạy — cùng một bản ghi
xuất hiện ở trang 1 và trang 2, một bản ghi khác không xuất hiện ở đâu cả.

### 6.2 Khi nào offset không còn dùng được

`OFFSET n` buộc PostgreSQL **đọc và bỏ** n dòng đầu. Chi phí tăng tuyến tính theo số trang:
trang 1000 với `pageSize = 50` nghĩa là đọc 50.000 dòng để trả về 50.

Ngưỡng chuyển sang keyset — thoả **một** là đủ:

- Bảng vượt vài trăm nghìn dòng **và** người dùng thật sự lật tới các trang sâu.
- Đây là API cuộn vô hạn (mobile, infinite scroll) — nơi "trang tiếp theo" mới là thao tác
  thật, còn "nhảy tới trang 500" thì không tồn tại.
- Dữ liệu thay đổi liên tục trong lúc người dùng lật trang — offset khi đó **bỏ sót và lặp
  lại** bản ghi, vì cửa sổ dịch chuyển dưới chân.

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

Keyset **không phải mặc định**: nó bỏ mất khả năng nhảy trang mà UI dạng bảng của trang
quản trị đang dùng. Chọn nó khi một trong ba ngưỡng ở §6.2 thoả, không phải để cho nhanh.

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

Với khối lượng rất lớn (từ hàng chục nghìn dòng), `SaveChanges` một lượt cũng không phù
hợp: `ChangeTracker` phình và bộ nhớ tăng tuyến tính. Khi đó chia lô theo kích thước cố
định, và gọi `ChangeTracker.Clear()` sau mỗi lô. Ngưỡng chính xác phải **đo**, không đoán —
và nếu khối lượng lớn tới mức phải chia lô thì việc đó gần như chắc chắn thuộc về một job
nền ([`be-cqrs-handler.md`](be-cqrs-handler.md) §10).

Cập nhật/xoá hàng loạt theo điều kiện dùng `ExecuteUpdateAsync` / `ExecuteDeleteAsync` —
chúng sinh một câu UPDATE/DELETE duy nhất, không load entity. Hai lưu ý:

- Chúng **bỏ qua `ChangeTracker`** và bỏ qua interceptor, nên `AuditInterceptor` **không**
  chạy — phải tự set các cột vết trong chính câu `ExecuteUpdate`.
- `ExecuteDeleteAsync` là **xoá cứng**. Trên entity có soft delete, dùng `ExecuteUpdateAsync`
  đặt `IsDeleted = true` thay vì xoá.

### 7.2 Cấu hình `UseNpgsql`

```csharp
services.AddDbContext<CoreDbContext>((sp, options) => options
    .UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        npgsql.CommandTimeout(30);
        npgsql.MigrationsHistoryTable("__ef_migrations_history", CoreSchema.Name);
    })
    .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
```

| Thiết lập | Vì sao khai tường minh |
| --- | --- |
| `EnableRetryOnFailure` | Một nhịp chớp mạng giữa app và Postgres không được biến thành 500 cho người dùng |
| `CommandTimeout(30)` | Bằng đúng mặc định — khai ra để nó là con số **đã cân nhắc**, không phải mặc định trôi vào. Hạ xuống khi có số đo p99 thật |
| `MigrationsHistoryTable` trong schema `core` | Core sở hữu lịch sử migration của mình; module có bảng riêng — [`../database/migration-policy.md`](../database/migration-policy.md) |

> ⚠️ **Bẫy đi kèm `EnableRetryOnFailure`:** chiến lược thử lại **không** bọc được
> transaction do code tự mở. Nó ném `InvalidOperationException` lúc **chạy**, không lúc
> biên dịch, và chỉ trên đúng đường ghi đó. Đây là một trong ba lý do transaction gom về
> `IUnitOfWork.ExecuteInTransactionAsync` — [`be-cqrs-handler.md`](be-cqrs-handler.md) §4.

---

## 8. Cache — CHƯA làm ở v1

### 8.1 Trạng thái và lý do

> 📐 **Redis và `CachingBehavior` KHÔNG có ở v1.** Không phải bỏ sót — chưa đo được vấn
> đề nào để cache giải quyết.

Ba lý do, xếp theo mức độ quyết định:

1. **Chưa có số đo.** Không có một query nào được chứng minh là chậm trên dữ liệu thật.
   Thêm cache lúc này là tối ưu một thứ chưa biết có tốn hay không.
2. **Cache là một nguồn sự thật thứ hai.** Mọi bug từ đó về sau đều có thêm câu hỏi *"dữ
   liệu này cũ hay mới?"*, và câu hỏi đó tốn thời gian ngay cả khi câu trả lời là "mới".
3. **Hệ chạy một process.** Nếu sau này thật sự cần cache, mức đầu tiên là in-process —
   Redis chỉ cần khi có từ hai process trở lên đọc chung một tập dữ liệu.

Xem [`../adr/0006-pipeline-behavior.md`](../adr/0006-pipeline-behavior.md) cho quyết định
về số lượng behavior ở v1.

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

Bốn method, không hơn. `RemoveByTagAsync` có mặt ngay từ đầu **có chủ đích**: invalidation
theo nhóm là thứ khó thêm sau, vì thêm sau nghĩa là phải đi sửa mọi lời gọi `SetAsync` để
bổ sung tag.

Implementation ở v1: **không có**. Nếu một chỗ nào đó thật sự cần trước khi tầng cache được
dựng, đăng ký một implementation **no-op** (luôn miss, `Set` không làm gì) — nó đúng về
mặt ngữ nghĩa và giữ call site không phải đổi khi implementation thật xuất hiện.

`Core.Application` **không bao giờ** chạm thẳng `HybridCache`/`IMemoryCache`/
`IDistributedCache` — chỉ qua `ICacheStore`.

### 8.3 Ba điều kiện bắt buộc trước khi thêm bất kỳ cache nào

Thiếu **một** thì dừng lại và hỏi, không tự quyết:

1. **Số đo** chứng minh chỗ đó tốn thật — thời gian, tần suất, và tỉ lệ so với tổng thời
   gian request.
2. **Danh sách đầy đủ đường ghi phải invalidate**, kể cả job nền (nó không có
   `HttpContext`, và đây là chỗ dễ quên nhất).
3. **Test xác nhận invalidation chạy**, không chỉ test cache hit.

> **Cache dữ liệu phân quyền mà chỉ dựa TTL, không invalidate tường minh khi ma trận quyền
> đổi → quyền đã thu hồi còn hiệu lực tới hết TTL.** Đó là lỗ hổng bảo mật, không phải vấn
> đề hiệu năng. Nếu cache phân quyền, invalidation phải là **đồng bộ** trong cùng lượt ghi
> ma trận.

### 8.4 Cái gì được cache mà không cần ba điều kiện trên

`ConcurrentDictionary` cache **metadata bất biến trong một process** — kết quả phân tích
assembly, bảng ánh xạ dựng một lần lúc khởi động. Nguồn dữ liệu là chính assembly nên nó
không bao giờ cũ.

`static Dictionary` giữ dữ liệu **từ DB** thì **không** hợp lệ: không eviction, không
invalidation, và không ai gọi nó là cache nên không ai nghĩ tới việc xoá nó.

---

## 9. Khi sửa code tính toán nghiệp vụ

Khi tối ưu **bất kỳ** code nào tính ra con số hiển thị cho người dùng: output phải **giống
hệt** trước khi sửa, trên cùng dữ liệu. Đối chiếu thật bằng một test so sánh, đừng suy
luận — đây là con số người dùng nhìn thấy, không phải chi tiết nội bộ.

Khuôn an toàn: giữ implementation cũ lại tạm thời, chạy cả hai trên cùng tập dữ liệu, so
sánh, rồi mới xoá bản cũ. Chi phí một buổi; cái tránh được là một sai số không ai phát hiện
cho tới khi có người đối chiếu với sổ sách bên ngoài.

---

## 10. Checklist khi viết một repository/query mới

Áp ngay, không chờ ai nhắc:

- [ ] Method **không** trả `IQueryable`.
- [ ] Query chỉ đọc có `AsNoTracking()`; query lấy entity để sửa thì **không** có.
- [ ] Dùng projection `Select` thay vì load cả entity, trừ khi thật sự cần entity để sửa.
- [ ] Mọi predicate lọc nóng có index dẫn đầu đúng cột đó.
- [ ] Index unique trên bảng soft delete có `HasFilter`.
- [ ] `Distinct` / `GroupBy` / `Count` / phân trang chạy ở **SQL**, không `ToListAsync()`
      rồi mới làm trong C#.
- [ ] Không `await` trong vòng lặp.
- [ ] Sắp xếp có tiêu chí phụ ổn định (`ThenBy(x => x.Id)`).
- [ ] `SortBy` đến từ enum hoặc allowlist, không phải chuỗi client gửi thẳng vào `OrderBy`.
- [ ] Nếu bỏ qua một mục trên: comment nêu **con số** trần trên và điều kiện làm nó hết
      đúng. *"Dataset hiện tại nhỏ"* suông **không** phải ngoại lệ hợp lệ — nó không kiểm
      chứng được, và nó luôn đúng cho tới đúng ngày nó sai.
