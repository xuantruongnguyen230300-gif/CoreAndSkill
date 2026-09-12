---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quy ước Backend — Entity & Domain

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi code mẫu dưới đây là khuôn cho `src/BE` sẽ được
> xây ở giai đoạn 2, không phải mô tả code đang chạy.
>
> Điểm khác lớn nhất so với mọi tài liệu tiền nhiệm: **Domain không ném exception cho lỗi
> nghiệp vụ.** Factory và mutation method trả `Result<T>`. Xem §3 và
> [`../adr/0003-result-thuan.md`](../adr/0003-result-thuan.md).

---

## 1. `BaseEntity`

```csharp
// Core.Domain/Common/EntityId.cs — điểm sinh Id DUY NHẤT của toàn hệ
public static class EntityId
{
    public static Guid New() => Guid.CreateVersion7();
}

// Core.Domain/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; init; } = EntityId.New();

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

### 1.1 `Id` là `init`, sinh bằng UUID v7

Hai quyết định gộp trong một dòng.

**`init` chứ không `set`:** entity có danh tính hợp lệ ngay lúc khởi tạo, và không ai gán
lại từ ngoài sau đó. Cách làm ngược lại — để interceptor sinh Id lúc `SaveChangesAsync` —
khiến entity mang `Guid.Empty` trong suốt khoảng từ khi tạo tới khi lưu, và **mọi thứ cần
Id trước đó đều vỡ**: gắn quan hệ giữa hai entity mới, phát domain event, trả Id về cho
caller trong cùng một handler.

**UUID v7 chứ không v4 (`Guid.NewGuid()`):** v7 mang timestamp ở phần đầu nên các giá trị
sinh gần nhau về thời gian thì gần nhau về thứ tự. Hệ quả trên PostgreSQL:

| | UUID v4 (ngẫu nhiên) | UUID v7 (tuần tự theo thời gian) |
| --- | --- | --- |
| Vị trí chèn trong B-tree của khoá chính | Rải khắp cây | Luôn ở mép phải |
| Page split | Xảy ra liên tục ở các trang giữa | Gần như chỉ ở trang cuối |
| Phân mảnh index | Tăng dần theo số bản ghi | Thấp và ổn định |
| Cache hit của trang index nóng | Kém — trang nóng trải rộng | Tốt — trang nóng là vài trang cuối |

Đây là vấn đề thật ở bảng ghi nhiều, không phải tối ưu sớm: nó không sửa được sau khi đã
có dữ liệu mà không đổi khoá chính.

**Điểm sinh Id là `EntityId.New()`, không phải `Guid.CreateVersion7()` viết trực tiếp.**
Một seam một dòng cho phép đổi cách sinh Id ở đúng một chỗ, và cho phép các kiểu **không**
kế thừa `BaseEntity` (như `AppUser`) dùng chung cùng quy tắc.

Luật `BaseEntityId_MustBe_InitOnly` ([`../RULES.md`](../RULES.md) §4) canh vế `init`.

### 1.2 Năm field audit có public setter — CÓ CHỦ ĐÍCH, và có giới hạn

`CreatedBy` / `UpdatedBy` / `CreatedAt` / `UpdatedAt` / `IsDeleted` dùng `public get; set;`.
Đây **không** phải sơ suất encapsulation.

`AuditInterceptor` sống ở `Core.Infrastructure` và chạy trong `SaveChangesAsync`. Nó phải
**ghi** được năm field này từ bên ngoài entity. Nếu để `protected`/`private`, interceptor
buộc phải dùng reflection hoặc shadow property — cả hai đều phức tạp hơn nhiều so với cái
chúng bảo vệ, và reflection ở đường ghi nóng là thứ ta đang cố loại khỏi codebase này.

**Ranh giới của ngoại lệ, viết thành ba câu phủ định:**

- Ngoại lệ này **không** mở rộng sang field nghiệp vụ. Field nghiệp vụ luôn `private set`.
- Ngoại lệ này **không** áp cho `Id` — `Id` là `init`.
- Ngoại lệ này **không** là giấy phép để "field nào tiện thì mở setter". Đúng năm field
  trên, không thêm.

Luật `BaseEntity_Descendants_MustNotHave_PublicSetter` canh: mọi hậu duệ của `BaseEntity`
không được khai public setter cho field của riêng nó. Năm field kể trên nằm ở lớp cơ sở
nên nằm ngoài phạm vi detector — đó chính là cách phân biệt được viết thành cổng.

### 1.3 `AuditInterceptor` ghi gì ở mỗi trạng thái

| `EntityState` | Field được ghi |
| --- | --- |
| `Added` | `CreatedAt`, `CreatedBy`, **và** `UpdatedAt` = `CreatedAt`, `UpdatedBy` = `CreatedBy` |
| `Modified` | `UpdatedAt`, `UpdatedBy` |

**Vì sao ghi luôn `Updated*` lúc tạo:** để hai cột đó **không null** từ lúc bản ghi ra
đời. Nếu để null, mọi nơi hiển thị "sửa lần cuối" phải tự viết `UpdatedAt ?? CreatedAt` —
ở từng màn danh sách, từng báo cáo, từng câu `ORDER BY`. Chỉ cần một chỗ quên là bản ghi
**chưa sửa lần nào** rơi xuống cuối danh sách sắp theo thời gian sửa: sai im lặng, không
lỗi, không test nào bắt.

**Cái giá đã cân và chấp nhận:** mất khả năng đọc `UpdatedAt is null` để biết "bản ghi này
chưa từng bị sửa". Câu hỏi đó hiếm khi được hỏi; ai cần phân biệt thì so
`UpdatedAt != CreatedAt`.

Cả bốn field lấy chung một biến `now` trong cùng lượt `SaveChangesAsync`, để bản ghi mới
có `CreatedAt` **bằng đúng** `UpdatedAt`, không lệch vài mili giây.

Luật `EveryInterceptor_IsWiredInto_DbContextOptions` canh vế "khai rồi có được nối vào
không" — một interceptor không được đăng ký là một cơ chế tồn tại trên giấy.

---

## 2. Encapsulation — field nghiệp vụ `private set`

```csharp
public sealed class Role : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }

    private Role() { }   // EF Core cần ctor không tham số
}
```

Mutate qua method mang **tên nghiệp vụ**, không qua setter:

```csharp
public Result Rename(string name) { ... }        // ĐÚNG
role.Name = "Quản trị";                           // SAI — không biên dịch được, và đó là mục đích
```

**Vì sao:** invariant chỉ là luật khi nó **không thể** bị vượt qua. Nếu ai đó gán được
`role.Name = ""` từ bên ngoài, luật "tên vai trò không được rỗng" chỉ còn là quy ước bằng
lời — và quy ước bằng lời thì lần thứ một trăm sẽ có người quên.

Tên method cũng là tài liệu: `Deactivate()` nói được lý do nghiệp vụ mà
`IsActive = false` không nói được — nó chặn hay chỉ ẩn? có ghi vết không? có kéo theo gì
không? Câu trả lời nằm trong thân method, đúng một chỗ.

---

## 3. Factory & mutation method trả `Result<T>` — KHÔNG ném exception

Đây là điểm lệch lớn nhất khỏi dự án tiền nhiệm. Đọc kỹ.

### 3.1 Cách cũ — và vì sao bỏ

```csharp
// ❌ KHÔNG dùng ở repo này
public static Criteria Create(string code, decimal maxScore)
{
    if (string.IsNullOrWhiteSpace(code))
        throw new DomainException(CriteriaErrors.CodeRequired);
    if (maxScore <= 0)
        throw new DomainException(CriteriaErrors.MaxScoreInvalid);
    return new Criteria { Code = code, MaxScore = maxScore };
}
```

Bốn vấn đề, xếp theo mức độ đắt:

1. **Lỗi nghiệp vụ là kết quả mong đợi, không phải sự cố.** "Mã bị trùng" xảy ra hàng ngày
   trong vận hành bình thường. Dùng exception cho nó là dùng cơ chế thoát hiểm cho luồng
   chính.
2. **Chữ ký nói dối.** `Criteria Create(...)` hứa trả về một `Criteria`. Người gọi không
   thấy trong kiểu bất kỳ dấu hiệu nào rằng lời gọi này có thể thất bại, nên rất dễ quên
   bọc.
3. **Cần một cầu nối exception → HTTP.** Cầu nối đó ở dự án tiền nhiệm dựng bằng
   reflection và **đã nổ trên Production** — xem
   [`../audit/2026-09-05-reflection-envelope.md`](../audit/2026-09-05-reflection-envelope.md).
   Bỏ exception là bỏ luôn lý do tồn tại của cầu nối đó.
4. **Chỉ báo được một lỗi mỗi lần.** `throw` đầu tiên kết thúc hàm, nên người dùng sửa
   một lỗi rồi lại thấy lỗi tiếp theo. `Result` gom được nhiều lỗi trong một lượt.

### 3.2 Cách của repo này

```csharp
// Core.Domain/Roles/RoleErrors.cs — catalog khai TRƯỚC, cạnh code ném ra nó
public static class RoleErrors
{
    public static readonly Error CodeRequired = new(
        "CORE.ROLE.CODE_REQUIRED", "Mã vai trò không được để trống.", ErrorType.Validation);

    public static readonly Error NameRequired = new(
        "CORE.ROLE.NAME_REQUIRED", "Tên vai trò không được để trống.", ErrorType.Validation);

    public static readonly Error SystemRoleImmutable = new(
        "CORE.ROLE.SYSTEM_IMMUTABLE", "Vai trò hệ thống '{Code}' không được sửa.", ErrorType.BusinessRule);
}
```

```csharp
// Core.Domain/Entities/Role.cs
public sealed class Role : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }

    private Role() { }

    public static Result<Role> Create(string code, string name, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<Role>(RoleErrors.CodeRequired);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Role>(RoleErrors.NameRequired);

        return Result.Success(new Role
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsSystem = isSystem,
        });
    }

    public Result Rename(string name)
    {
        if (IsSystem)
            return Result.Failure(RoleErrors.SystemRoleImmutable.WithParams(("Code", Code)));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(RoleErrors.NameRequired);

        Name = name.Trim();
        return Result.Success();
    }
}
```

> 🚨 **Catalog ở đoạn trên nằm ở `Core.Domain`, không phải `Core.Application`.** Entity
> `Role` gọi `RoleErrors.*`, mà `Core.Domain` **không được** tham chiếu `Core.Application`
> (bảng ranh giới project ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2,
> luật A1). Đặt catalog này ở `Core.Application`
> thì đoạn code trên **không biên dịch được** — và một mẫu không biên dịch được là một mẫu sẽ
> được chép nguyên xi rồi mới phát hiện. Quy tắc quyết định ở §3.3.

Ba điều bắt buộc trong đoạn trên:

- **Không gán `Id`** trong factory — `BaseEntity` đã sinh sẵn. Gán lại là sinh GUID thứ
  hai rồi ghi đè cái thứ nhất: không sai kết quả, nhưng làm mờ chỗ nào thật sự sở hữu Id.
- **Không gán `CreatedAt`/`UpdatedAt`** — `AuditInterceptor` điền lúc `SaveChangesAsync`.
  Gán tay là dạy đúng thứ interceptor sinh ra để khỏi phải làm, và tạo nguồn thứ hai lệch
  được với nguồn thứ nhất.
- **Mutation method trả `Result`** (không generic) khi nó không sinh giá trị mới.

### 3.3 `Error` sống ở đâu

`Error`, `ErrorType`, `Result`, `Result<T>` khai ở **`Core.Domain`** — vì `Domain` cần
chúng và `Domain` không được tham chiếu `Application`.

Catalog lỗi khai ở **cùng project với code ném ra nó** — không có ngoại lệ, và không có
chỗ mặc định:

| Catalog | Ở đâu | Vì sao |
| --- | --- | --- |
| `RoleErrors` | **`Core.Domain/`** | Entity `Role` gọi nó trong chính invariant của mình (§3.2). Đặt ở `Application` thì `Core.Domain` không biên dịch được — xem khối 🚨 ở §3.2 |
| Catalog chỉ được handler dùng | **`Core.Application/<Feature>/`** | Không entity nào gọi tới, nên không kéo `Domain` phụ thuộc ngược |

Quy tắc một dòng: **catalog nằm cùng project với code ném ra nó.** Đừng đọc nó thành
"catalog thuộc `Application`" — đặt sai chỗ thì hỏng lúc biên dịch `Core.Domain`.
Hợp đồng đầy đủ của
`Result<T>` và `Error`: [`be-cqrs-handler.md`](be-cqrs-handler.md).

### 3.4 Khi nào vẫn còn được ném exception

Chỉ cho **lỗi ngoài dự kiến**: mất kết nối DB, bug lập trình, vi phạm bất biến nội bộ
đáng lẽ không thể xảy ra. Ba dấu hiệu nhận biết:

- Không có hành động nào người dùng làm được để tránh nó.
- Không có câu nào hiển thị cho người dùng ngoài "đã có lỗi hệ thống".
- Nó cần được log kèm stack trace và cần ai đó xem.

Luật `DomainAndApplication_MustNotThrow_BusinessException` ([`../RULES.md`](../RULES.md)
§5) canh vế còn lại.

---

## 4. Value Object

Dùng khi khái niệm có **từ hai field đi cùng nhau**, hoặc có **luật định dạng** cần
validate. **Không** bọc VO cho một `decimal`/`string` đơn lẻ không có luật gì đặc biệt —
đó là thêm một tầng để không đổi lấy gì.

```csharp
// Core.Domain/ValueObjects/EmailAddress.cs
public sealed record EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value) => Value = value;

    public static Result<EmailAddress> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<EmailAddress>(EmailErrors.Required);

        var normalized = value.Trim().ToLowerInvariant();

        if (!MailAddress.TryCreate(normalized, out _))
            return Result.Failure<EmailAddress>(EmailErrors.Malformed.WithParams(("Value", normalized)));

        return Result.Success(new EmailAddress(normalized));
    }

    public override string ToString() => Value;
}
```

`record` cho sẵn so sánh theo giá trị — đúng bản chất của Value Object: hai địa chỉ email
cùng chuỗi là **một**, không phải hai thứ giống nhau.

### 4.1 Ánh xạ EF Core

| VO có | Dùng | Lý do |
| --- | --- | --- |
| Đúng một giá trị (`EmailAddress`) | `HasConversion` | Ánh xạ vào **một cột**, không đổi hình dạng bảng |
| Nhiều field (`Address` gồm phố/quận/tỉnh) | `ComplexProperty` (hoặc `OwnsOne` khi cần navigation) | Nhiều cột trong cùng bảng |

```csharp
builder.Property(x => x.Email)
    .HasConversion(
        vo => vo.Value,
        raw => EmailAddress.Create(raw).Value)
    .HasMaxLength(256);
```

> ⚠️ Lời gọi `.Value` trong nhánh đọc sẽ **ném** nếu dữ liệu trong DB không hợp lệ. Đó là
> hành vi đúng: dữ liệu đã lưu mà không dựng lại được VO là lỗi ngoài dự kiến, không phải
> lỗi nghiệp vụ. Nó thuộc đúng nhóm §3.4.

---

## 5. Soft delete và tenant — hai vòng lặp, không khai lẻ

> Mục này giữ **cách khai** hai bộ lọc toàn cục và cái bẫy index unique đi kèm cả hai.
> Vì sao multi-tenant làm như vậy, bẫy model cache, allowlist bỏ lọc, và ảnh hưởng lên
> Identity: [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md).

### 5.1 Chữ ký CoreDbContext — định nghĩa gốc

Khối dưới đây là **nguồn duy nhất** của chữ ký `CoreDbContext` và của hai bộ lọc toàn cục.
File khác — kể cả [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md),
nơi giải thích *vì sao* cơ chế tenant làm như vậy — chỉ được trỏ về đây, không chép lại
([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

Cả hai filter khai **một lần** trong `OnModelCreating` bằng vòng lặp trên model đã build.
**Không** thêm `.Where(x => !x.IsDeleted)` ở từng query, và **không** khai lẻ ở từng
`IEntityTypeConfiguration<T>`.

```csharp
// Core.Infrastructure/Persistence/CoreDbContext.cs
public class CoreDbContext(
    DbContextOptions<CoreDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public const string SoftDeleteFilterKey = "SoftDelete";
    public const string TenantFilterKey     = "Tenant";

    // Đọc MỖI LẦN truy vấn chạy, không phải một lần lúc dựng model.
    public Guid CurrentTenantId => tenantContext.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(CoreSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);

        ApplySoftDeleteQueryFilters(modelBuilder);   // PHẢI chạy SAU dòng trên
        ApplyTenantQueryFilters(modelBuilder);       // PHẢI chạy SAU dòng trên
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Not(
                Expression.Property(parameter, nameof(BaseEntity.IsDeleted)));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(SoftDeleteFilterKey, Expression.Lambda(body, parameter));
        }
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType)) continue;

            var e = Expression.Parameter(entityType.ClrType, "e");
            // e.TenantId == this.CurrentTenantId
            // Vế phải trỏ vào INSTANCE DbContext, KHÔNG chụp một giá trị Guid lúc dựng model.
            var body = Expression.Equal(
                Expression.Property(e, nameof(ITenantScoped.TenantId)),
                Expression.Property(Expression.Constant(this), nameof(CurrentTenantId)));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(TenantFilterKey, Expression.Lambda(body, e));
        }
    }
}
```

`ITenantContext` là seam khai ở `Core.Application` — [`be-architecture.md`](be-architecture.md)
§1.1 (bảng seam) và §5.2 (vòng đời). `CoreDbContext` nhận nó qua constructor chứ không đọc
`HttpContext`: `Core.Infrastructure` bị **cấm** phụ thuộc `HttpContext` — bảng ranh giới
project ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2.

`ITenantScoped` **độc lập với `BaseEntity`**: `AppUser` và `AppRole` khai `ITenantScoped`
mà không kế thừa `BaseEntity`, nên hai vòng lặp lọc theo **hai điều kiện khác nhau** —
`typeof(BaseEntity).IsAssignableFrom(...)` cho vòng thứ nhất, `typeof(ITenantScoped)…` cho
vòng thứ hai. Chép điều kiện của vòng này sang vòng kia là cách làm `AppUser` mất filter
tenant mà không có gì báo.

> 🛑 **Đừng GỘP hai điều kiện vào một biểu thức.** Filter **không tên** thì lần khai sau
> ghi đè lần khai trước, nên một filter gộp không tên sẽ **xoá** filter kia — không lỗi
> biên dịch, không cảnh báo, không ngoại lệ. Nếu thứ bị xoá là filter tenant thì hậu quả là
> **đơn vị A nhìn thấy dữ liệu đơn vị B**. Gộp cũng làm hai luật (E3 và M1) mất khả năng
> kiểm độc lập. Chi tiết:
> [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §3.1.
>
> Cùng lý do, **đừng chụp giá trị `TenantId` vào biến cục bộ** rồi nhúng nó vào biểu thức:
> model được cache theo kiểu `DbContext`, nên giá trị của request đầu tiên sẽ được dùng lại
> cho **mọi tenant** sau đó. Đây là lỗi rò dữ liệu nặng nhất của cả mô hình —
> [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §4.

Ba việc bắt buộc kèm theo, mỗi việc có cổng riêng ở [`../RULES.md`](../RULES.md) §9:
`TenantId` do interceptor gán lúc `Added` và không bao giờ được sửa (M2); mọi index unique
gồm `TenantId` (M3, §5.3); mọi lời gọi `IgnoreQueryFilters` nằm trong allowlist (M5, §5.4).

### 5.2 Ba ràng buộc, cả ba hỏng im lặng nếu bỏ

1. **Thứ tự.** Vòng lặp phải chạy **sau** `ApplyConfigurationsFromAssembly`. Đảo thứ tự
   thì các entity chỉ được đăng ký vào model bởi configuration sẽ chưa có mặt lúc vòng lặp
   chạy, và **mọi** entity đó mất filter — không lỗi, không cảnh báo.
2. **Filter phải có TÊN.** EF Core cho phép nhiều query filter đặt tên trên cùng một
   entity; filter **không tên** thì lần khai thứ hai **ghi đè** lần thứ nhất. Đặt tên là
   cách để một module thêm filter riêng (ví dụ lọc theo đơn vị) mà không xoá mất filter
   soft delete của Core.
3. **Khai lẻ ở từng configuration là sai.** Entity mới nào quên khai thì mất filter, và
   không có gì báo. Đây là dạng lỗi rò rỉ dữ liệu đã xoá ra API mà chỉ người dùng phát
   hiện.

Luật `EveryBaseEntityDescendant_HasNamedSoftDeleteQueryFilter` ([`../RULES.md`](../RULES.md)
§4) canh cả ba: nó duyệt model đã build và kiểm từng hậu duệ `BaseEntity` có đúng filter
mang tên `SoftDelete`. Luật `EveryTenantScopedEntity_HasTenantQueryFilter`
([`../RULES.md`](../RULES.md) §9) canh đúng ba điều đó cho filter `Tenant`.

**Cả ba ràng buộc trên áp cho vòng lặp tenant y hệt**, và ràng buộc số 2 ở đó nặng hơn hẳn:
mất filter soft delete làm dữ liệu đã xoá hiện lại — phiền, người dùng báo ngay. Mất filter
tenant làm **đơn vị này nhìn thấy dữ liệu đơn vị khác** — và có thể không ai báo trong nhiều
tháng.

### 5.3 Cái bẫy đi kèm — index unique phải gồm BA thứ

Xoá mềm nghĩa là dòng cũ **vẫn nằm trong bảng**. Một index unique không lọc sẽ chặn việc
chèn lại đúng cặp giá trị đã xoá. Và trên một entity `ITenantScoped`, một index unique
không gồm `TenantId` sẽ chặn đơn vị B dùng một mã mà đơn vị A đã dùng.

Trên một entity vừa kế thừa `BaseEntity` vừa khai `ITenantScoped` — tức **đa số entity
nghiệp vụ** — index unique phải gồm cả ba:

```csharp
builder.HasIndex(x => new { x.TenantId, x.Code })   // 1. tenant  2. cột nghiệp vụ
    .IsUnique()
    .HasFilter("is_deleted = false")                // 3. mệnh đề lọc xoá mềm
    .HasDatabaseName("ux_menu_item_tenant_code_active");
```

`TenantId` **đứng đầu**: mọi truy vấn đều lọc theo tenant trước, nên index dẫn đầu bằng cột
được lọc trước thì seek được ([`be-performance.md`](be-performance.md)).

Hai cách hỏng, hai triệu chứng khác hẳn nhau — và cả hai đều không có lỗi biên dịch:

| Thiếu | Triệu chứng người dùng thấy |
| --- | --- |
| Mệnh đề `is_deleted` | *"Tôi vừa xoá nó xong mà, sao bảo trùng?"* — dòng chiếm khoá là một dòng đã xoá, không màn hình nào hiển thị |
| `TenantId` | *"Hệ thống nói mã trùng nhưng tôi tìm không thấy"* — dòng chiếm khoá thuộc **đơn vị khác**, và bộ lọc tenant đã giấu nó đi |

> 🪤 **Tên index có trần 63 byte.** Thêm `_tenant` vào mọi tên đẩy một số tên chạm trần, và
> PostgreSQL cắt **âm thầm** — không lỗi, không cảnh báo. Hai index rồi va nhau với thông
> báo *"relation already exists"* trỏ vào một cái tên chưa ai từng gõ. Rút gọn phần **tên
> cột**; không bao giờ rút gọn hay bỏ phần `tenant`.

Ca thứ nhất là bẫy đã dính thật ở dự án tiền nhiệm, và cả hai chỉ lộ ra khi có người thử
tạo lại một bản ghi — thường là vài tháng sau khi tính năng lên. Luật `M3`
(`EveryUniqueIndex_OnTenantScoped_Includes_TenantId`) canh vế thứ hai; quy ước đặt tên
index và danh sách bảng miễn trừ `TenantId` ở
[`../database/schema-core.md`](../database/schema-core.md) §1.3 và §3.7.

> 🛑 **`HasFilter` nhận SQL THÔ, nên chuỗi trong đó là TÊN VẬT LÝ của cột, không phải tên
> property C#.** Schema dùng `snake_case` (xem [`../database/schema-core.md`](../database/schema-core.md)),
> nên phải viết `is_deleted`, không phải `"IsDeleted"`.
>
> Đây là chỗ trừu tượng hoá của EF Core **rò ra**: mọi nơi khác bạn viết tên property và
> quy ước đặt tên tự dịch sang tên cột, nhưng chuỗi SQL thô thì không ai dịch hộ. Viết sai
> tên ở đây **không gây lỗi biên dịch** — nó gây lỗi lúc chạy migration, hoặc tệ hơn, tạo
> ra một index lọc theo một cột không tồn tại nếu cơ sở dữ liệu chấp nhận.
>
> Cùng luật này áp cho mọi chuỗi SQL thô khác: `HasComputedColumnSql`, `HasDefaultValueSql`,
> `FromSqlRaw`, và ràng buộc kiểm tra.

### 5.4 Khi nào bỏ qua filter

`IgnoreQueryFilters()` được dùng đúng ba chỗ: màn hình khôi phục dữ liệu đã xoá, báo cáo
audit, và migration dữ liệu. Mỗi lời gọi phải nêu được thuộc nhóm nào — nếu không nêu
được thì đó là đang vá một query sai chỗ khác.

> 🛑 **Dạng KHÔNG THAM SỐ gỡ MỌI filter — cả tenant.** Ở một màn hình khôi phục dữ liệu,
> đó là một lỗ rò toàn phần cho một nhu cầu chỉ cần gỡ một nửa. Nêu tên đúng filter cần gỡ:
>
> ```csharp
> .IgnoreQueryFilters([CoreDbContext.SoftDeleteFilterKey])   // giữ nguyên filter tenant
> ```
>
> Dạng không tham số chỉ được dùng ở ba ca chạy **ngoài ngữ cảnh một đơn vị** — job nền
> toàn hệ, quản trị vận hành, migration dữ liệu — và mọi lời gọi phải nằm trong allowlist
> khai tường minh, kèm lý do tại chỗ khai (luật M5). Danh sách và cách khai:
> [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §7.

---

## 6. Concurrency — dùng `xmin` của PostgreSQL

> 📖 **Mục này là recipe: khai token thế nào, cho entity nào.** Hai thứ KHÔNG có ở đây và bỏ sót thì hỏng nặng — đọc [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §5–§6:
> - **Token ở cấp TẬP HỢP** khi màn hình thay thế cả một tập (ma trận phân quyền), không phải sửa một dòng.
> - **Xử lý khi xung đột xảy ra** — bắt ở đúng một chỗ, và **cấm tự thử lại**. Tự thử lại nghĩa là ghi đè thay đổi của người khác một cách tự động, tức mất trắng giá trị của cả cơ chế, trong khi mọi thứ nhìn vẫn như đã làm đúng.

### 6.1 Entity nào cần token — và entity nào không

Thêm token cho entity thoả **cả hai**:

1. Có **từ hai luồng ghi độc lập** chạm cùng một bản ghi.
2. Việc mất một thay đổi gây hậu quả người dùng quan tâm.

Khuôn nhận diện điển hình: một entity vừa nhận **ghi hàng loạt** (import ghi đè toàn bộ
field) vừa nhận **sửa tay từng field** (đọc-rồi-ghi). Không có gì phát hiện khi hai luồng
ghi đè lên nhau; người sửa sau âm thầm mất thay đổi của người trước.

| Loại dữ liệu | Cần? | Vì sao |
| --- | --- | --- |
| Danh mục dùng chung, cấu hình hệ thống | ✅ | Nhiều người quản trị cùng sửa |
| Bản ghi có trạng thái theo quy trình | ✅ | Hai người cùng chuyển trạng thái là ca kinh điển |
| Người dùng, vai trò, phân quyền | ✅ | Màn quản trị và các luồng khác cùng ghi |
| Hồ sơ cá nhân, chỉ chính chủ sửa | ➖ | Một luồng ghi. Thêm cũng không hại, nhưng không giải quyết gì |
| Bảng chỉ ghi thêm, không sửa | ❌ | Không có cập nhật thì không có ghi đè |
| Bảng outbox, nhật ký | ❌ | Đồng thời xử lý bằng khoá lúc lấy bản ghi, không bằng token |

> ⚠️ **Cẩn thận với suy luận "chỉ có một luồng ghi".** Ở dự án tiền nhiệm, một tài liệu
> khẳng định vài bảng chỉ được ghi bởi tiến trình seed — trong khi màn hình phân quyền cũng
> ghi chúng qua một endpoint khác. Giới hạn **ai** ghi được không giới hạn **bao nhiêu
> người** ghi cùng lúc.
>
> Phép kiểm: liệt kê mọi đường ghi tới bảng đó bằng cách tìm trong code, đừng suy từ trí nhớ.

### 6.2 Recipe concurrency token cho Npgsql

```csharp
public sealed class MenuItem : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    // Concurrency token — PHẢI là uint. KHÔNG byte[], KHÔNG shadow property.
    public uint Version { get; private set; }
}
```

```csharp
// Core.Infrastructure/Persistence/Configurations/MenuItemConfiguration.cs
builder.Property(x => x.Version).IsRowVersion();
```

Npgsql provider nhận diện property CLR kiểu `uint` mang `IsRowVersion()` và bind thẳng vào
cột hệ thống **`xmin`** có sẵn của PostgreSQL. Không tạo cột mới, không cần migration riêng
cho property này. EF Core tự thêm `WHERE xmin = @original` vào câu UPDATE; bị ghi đè trong
lúc đang sửa thì ném `DbUpdateConcurrencyException`, và tầng trên dịch thành
`ErrorType.Conflict` (409) thay vì âm thầm ghi đè.

### 6.3 Kiểu CLR nào hợp lệ cho concurrency token — định nghĩa gốc

⚠️ Đây là bẫy đã dính thật, và là mục chở luật chống lỗi im lặng của cả chủ đề này.

> `.IsRowVersion()` **đúng hay sai tuỳ KIỂU CLR của property, không tuỳ provider.**

| Kiểu CLR | Trên SQL Server | Trên PostgreSQL (Npgsql) |
| --- | --- | --- |
| `byte[]` | Ánh xạ sang kiểu `rowversion`, **DB tự tăng mỗi lần UPDATE** — đúng | Tạo một cột `bytea` bình thường mà **không ai cập nhật** → `WHERE "RowVersion" = @original` **luôn khớp** → check concurrency **vô hiệu hoàn toàn, im lặng** |
| `uint` | Không phải khuôn của SQL Server | Bind vào cột hệ thống `xmin` — **đúng** |

Nhánh sai không có lỗi biên dịch, không có lỗi lúc chạy, không có cảnh báo. Ghi đè vẫn
xảy ra đúng như khi chưa làm gì cả. Đây chính là hình dạng của sự cố ở dự án tiền nhiệm:
một recipe dùng API của SQL Server tồn tại **song song ở nhiều file tài liệu** trong một
dự án chạy Npgsql, và sửa một nơi không chạm nơi kia.

Hai hệ quả cho repo này:

- File này là **file chủ duy nhất** của chủ đề concurrency ở khu `quy-uoc/`. File khác chỉ
  được trỏ tới đây, không được chép lại recipe.
- Cũng đừng đi tìm `UseXminAsConcurrencyToken()`. Method đó đã bị Npgsql đánh obsolete rồi
  gỡ hẳn; ai còn thấy nó trong code chép từ đâu đó thì đó là dấu hiệu code chưa từng build
  được với package version hiện tại, không phải một lựa chọn thiết kế.

Nếu sau này đổi sang SQL Server: đây là **một trong số ít chỗ phải sửa theo provider**, và
lúc đó mới dùng `byte[] RowVersion`.

### 6.4 Không áp cho `AppUser`/`AppRole`

Chúng là entity của ASP.NET Core Identity và **đã có sẵn `ConcurrencyStamp`**. Đường cập
nhật người dùng dùng đúng nó. Thêm cột thứ hai chồng lên là hai nguồn cho cùng một sự
thật, và chúng lệch được mà vẫn biên dịch.

---

## 7. Ranh giới Identity — `AppUser` không rời khỏi Infrastructure

### 7.1 Luật

`AppUser : IdentityUser<Guid>` và `AppRole : IdentityRole<Guid>` sống **chỉ** trong
`Core.Infrastructure`. `Core.Application` và `Core.Domain` **không bao giờ** thấy hai kiểu
này — chúng chỉ thấy interface.

```csharp
// Core.Application/Identity/IIdentityService.cs — đăng nhập, phiên, mật khẩu
public interface IIdentityService
{
    Task<Result<Guid>> SignInAsync(string userName, string password, CancellationToken ct);
    Task<Result> SignOutAsync(CancellationToken ct);
    Task<Result> ChangePasswordAsync(Guid userId, string current, string next, CancellationToken ct);
}

// Core.Application/Identity/IUserLookupService.cs — chỉ đọc
public interface IUserLookupService
{
    Task<UserSummaryDto?> FindByIdAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<UserSummaryDto>> FindByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken ct);
}

// Core.Application/Identity/IUserAdminService.cs — hành động quản trị
public interface IUserAdminService
{
    Task<Result<Guid>> CreateAsync(CreateUserInput input, CancellationToken ct);
    Task<Result> LockAsync(Guid userId, CancellationToken ct);
    Task<Result> UnlockAsync(Guid userId, CancellationToken ct);
    Task<Result> AssignRolesAsync(Guid userId, IReadOnlyCollection<string> roleNames, CancellationToken ct);
}
```

Ba interface thay vì một là áp dụng ISP: một handler chỉ cần tra cứu người dùng không phải
phụ thuộc vào bề mặt cho phép khoá tài khoản.

Luật `IdentityTypes_MustNotLeak_OutsideInfrastructure` ([`../RULES.md`](../RULES.md) §6)
canh điều này ở mức kiểu.

### 7.2 Hệ quả bắt buộc

- **DTO ra khỏi Identity là DTO của Application** (`UserSummaryDto`), không phải `AppUser`.
  Trả `AppUser` ra ngoài là rò rỉ toàn bộ bề mặt Identity — gồm cả `PasswordHash` và
  `SecurityStamp`.
- **FK nghiệp vụ trỏ tới người dùng là một `Guid` thuần**, không phải navigation property
  sang `AppUser`. Điều này còn là hệ quả của luật cấm FK vật lý xuyên schema
  ([`../database/migration-policy.md`](../database/migration-policy.md)).
- **`AppUser` không kế thừa `BaseEntity`** — C# không cho kế thừa hai lớp cơ sở, và
  `IdentityUser<Guid>` là bắt buộc. Nó mang **trường vết tương đương** (`CreatedBy`,
  `UpdatedBy`, `CreatedAt`, `UpdatedAt`) khai tay.
- **Hệ quả kéo theo của gạch đầu dòng trên:** `AuditInterceptor` lọc bằng
  `ChangeTracker.Entries<BaseEntity>()`, nên `AppUser` **nằm ngoài tầm với của nó**. Bốn
  cột vết của `AppUser` phải ghi tay ở tầng service, tại **từng** điểm tạo và sửa. Đây là
  chỗ dễ suy diễn nhầm nhất của cả mục: đọc §1.3 rồi tưởng bảng người dùng cũng được điền
  tự động.

### 7.3 Đây là đánh đổi có ý thức

**Lợi:** dùng ngay được toàn bộ ASP.NET Core Identity — băm mật khẩu đúng chuẩn, lockout,
`SecurityStamp` (thu hồi phiên được), xác nhận email, two-factor. Tự viết lại những thứ
này là công việc nhiều tháng và là chỗ dễ sai nhất trong một hệ thống.

**Giá phải trả, nói thẳng:** đổi sang SSO/LDAP/OIDC sau này **phải sửa `Core.Infrastructure`**.
Không đổi được bằng cấu hình.

Cụ thể phần nào phải viết lại: implementation của ba interface trên, cơ chế phát cookie
phiên, và cách ánh xạ nhóm/vai trò từ nguồn ngoài về bảng vai trò nội bộ. Cụ thể phần nào
**không** phải sửa: `Core.Application`, `Core.Domain`, mọi handler, mọi controller — vì
chúng chỉ thấy ba interface.

Đó chính là giá trị của ranh giới này: nó không làm việc đổi trở nên **rẻ**, nó làm việc
đổi trở nên **có phạm vi biết trước**.

---

## 8. FK và ranh giới schema

| Phạm vi | Loại FK | `DeleteBehavior` |
| --- | --- | --- |
| Cùng aggregate | Hard FK | `Cascade` |
| Cùng schema, khác aggregate | Hard FK | `Restrict` |
| **Khác schema** (Core ↔ module, module ↔ module) | **Soft FK** — `Guid` thuần, không constraint DB | Không có — chỉ `HasIndex`, kiểm tồn tại ở tầng Application |

Cấm FK vật lý xuyên schema là **điều kiện tiên quyết để sau này tách microservice**: một FK
xuyên schema là một sợi dây trói vĩnh viễn — không tách được mà không sửa schema, và sửa
schema trên dữ liệu thật là việc đắt nhất trong vòng đời một hệ thống.

Luật `NoForeignKey_CrossesSchemaBoundary` và `EveryMappedEntity_LivesInTheSchemaOfItsSide`
canh hai vế này.

---

## 9. ArchTest canh gì trong file này

> 📖 **Danh mục tên ArchTest là cột "Ép bằng gì" của [`../RULES.md`](../RULES.md) — nguồn duy nhất.**
> Luật của file này nằm ở §4 (Domain & dữ liệu) và §9 (multi-tenant) của bảng đó.
> Giải thích từng test canh điều gì: [`../wiki-core/be/04-testing-strategy.md`](../wiki-core/be/04-testing-strategy.md) §2.2.

## 10. Khi thêm một entity mới — thứ tự

1. Đối chiếu schema đã khai: [`../database/schema-core.md`](../database/schema-core.md).
2. Quyết định phía sở hữu (Core hay module) theo [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.
3. Entity → `<Phía>.Domain/Entities/`, kế thừa `BaseEntity`, factory trả `Result<T>`.
   Dữ liệu thuộc về một đơn vị thì khai thêm `ITenantScoped`; không thuộc đơn vị nào thì
   phải có tên trong danh sách miễn trừ ở
   [`../database/schema-core.md`](../database/schema-core.md) §1.3, kèm lý do.
4. Catalog lỗi → cùng project với code trả ra nó.
5. EF configuration → `<Phía>.Infrastructure/Persistence/Configurations/`. Index unique
   nào chạm field có soft delete thì **phải** kèm `HasFilter`, và trên entity
   `ITenantScoped` thì **phải** gồm cả `TenantId` — ba thứ, xem §5.3.
6. FK sang schema khác → soft FK, kiểm tồn tại ở Application.
7. Migration → project sở hữu schema. Áp schema theo
   [`../database/script-runbook.md`](../database/script-runbook.md).
8. Test: unit test cho factory và từng mutation method; integration test kiểm query filter
   soft delete có hiệu lực trên chính entity mới, và — với entity `ITenantScoped` — kiểm
   tenant A không đọc được bản ghi của tenant B (`CrossTenantAccess_Returns_NotFound`).
