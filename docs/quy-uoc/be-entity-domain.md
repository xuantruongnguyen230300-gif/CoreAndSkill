---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quy ước Backend — Entity & Domain

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi code mẫu dưới đây là khuôn cho `src/BE` sẽ được
> xây ở giai đoạn 2, không phải mô tả code đang chạy.
>
> **Domain không ném exception cho lỗi nghiệp vụ.** Factory và mutation method trả `Result<T>`.
> Xem §3 và [`../adr/0003-result-thuan.md`](../adr/0003-result-thuan.md).
>
> 📖 Lý do, bẫy, ví dụ mở rộng của từng mục — cùng số §: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md).

---

## 1. `BaseEntity`

```csharp
// Core.Domain/Common/EntityId.cs — điểm sinh Id DUY NHẤT của toàn hệ
public static class EntityId
{
    public static Guid New() => Guid.CreateVersion7();
}

// Core.Domain/Common/IAuditableEntity.cs — bốn field vết mà AuditInterceptor điền (§1.3, §1.4)
public interface IAuditableEntity
{
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
    DateTimeOffset? CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
}

// Core.Domain/Common/BaseEntity.cs
public abstract class BaseEntity : IAuditableEntity
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

- **`init` chứ không `set`:** entity có danh tính hợp lệ ngay lúc khởi tạo, không ai gán lại từ ngoài.
  Luật `BaseEntityId_MustBe_InitOnly` ([`../RULES.md`](../RULES.md) §4) canh.
- **UUID v7 chứ không v4 (`Guid.NewGuid()`).**
- **Điểm sinh Id là `EntityId.New()`**, không phải `Guid.CreateVersion7()` viết trực tiếp — một seam
  một dòng, dùng chung cho cả kiểu **không** kế thừa `BaseEntity` (§1.4).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §1.1

### 1.2 Năm field audit có public setter — CÓ CHỦ ĐÍCH, và có giới hạn

`CreatedBy` / `UpdatedBy` / `CreatedAt` / `UpdatedAt` / `IsDeleted` dùng `public get; set;` để
`AuditInterceptor` (`Core.Infrastructure`, chạy trong `SaveChangesAsync`) ghi được từ ngoài entity.
Ranh giới, ba câu phủ định: **không** mở rộng sang field nghiệp vụ (luôn `private set`); **không** áp
cho `Id` (`init`); **không** là giấy phép "field nào tiện thì mở setter" — đúng năm field, không thêm.
Luật `BaseEntity_Descendants_MustNotHave_PublicSetter` canh hậu duệ; năm field ở lớp cơ sở nằm ngoài
phạm vi detector.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §1.2

### 1.3 `AuditInterceptor` ghi gì ở mỗi trạng thái

Interceptor chọn entity bằng `ChangeTracker.Entries<IAuditableEntity>()` — theo **interface** — nên phủ
cả hậu duệ `BaseEntity` lẫn các kiểu ở §1.4.

| `EntityState` | Field được ghi |
| --- | --- |
| `Added` | `CreatedAt`, `CreatedBy`, **và** `UpdatedAt` = `CreatedAt`, `UpdatedBy` = `CreatedBy` |
| `Modified` | `UpdatedAt`, `UpdatedBy` |

Cả bốn field lấy chung một biến `now` trong cùng lượt `SaveChangesAsync`. "Chưa từng sửa" thì so
`UpdatedAt != CreatedAt`. Luật `EveryInterceptor_IsWiredInto_DbContextOptions` canh vế "có được nối vào".

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §1.3

### 1.4 `IAuditableEntity` cho kiểu không kế thừa `BaseEntity` — định nghĩa gốc

Các kiểu dưới đây mang bốn cột vết nhưng **không** kế thừa `BaseEntity` — `BaseEntity` kéo theo
`IsDeleted` và bộ lọc xoá mềm (luật E3):

| Kiểu | Bảng | Vì sao không kế thừa `BaseEntity` |
| --- | --- | --- |
| `Tenant` | `core.tenant` | Không có `is_deleted`: đơn vị không bao giờ bị xoá — [`../database/schema-core.md`](../database/schema-core.md) §1.3 |
| Kiểu ánh xạ bộ đếm sinh mã, khi thành phần đó được bật | `core.code_sequence` | Không có `is_deleted`: bộ đếm không xoá mềm — [`../database/schema-core.md`](../database/schema-core.md) §9.6 |
| `AppUser` | `core.app_user` | Lớp cơ sở bắt buộc là `IdentityUser<Guid>`, và C# không cho hai lớp cơ sở. Không có `is_deleted`: vô hiệu hoá tài khoản đi qua `lockout_end` — [`../database/schema-core.md`](../database/schema-core.md) §4.1 |

Mỗi kiểu **implement `IAuditableEntity` trực tiếp** (chữ ký ở khối §1); interceptor điền bốn cột theo
bảng §1.3. `Tenant` và kiểu bộ đếm khai `Id` `init` bằng `EntityId.New()` (§1.1); `AppUser` nhận `Id` từ
`IdentityUser<Guid>` nên không khai lại thành `init` được, nhưng giá trị vẫn lấy từ `EntityId.New()`.

```csharp
// Core.Infrastructure — AppUser không rời tầng này (§7)
public class AppUser : IdentityUser<Guid>, ITenantScoped, IAuditableEntity
{
    // Bốn field của IAuditableEntity — public setter có chủ đích, §1.4
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // … TenantId (ITenantScoped, §5) và các cột "Ta thêm" ở schema-core.md §4.1
}
```

| Luật | Ghi chú |
| --- | --- |
| Interceptor chọn entity theo interface, không theo lớp cơ sở | Chọn theo `BaseEntity` thì kiểu ngoài cây kế thừa phải ghi tay bốn cột ở **từng** điểm tạo/sửa |
| Public setter chỉ cho **đúng bốn** field của interface | Cùng ranh giới §1.2. Luật E1 quét hậu duệ `BaseEntity`, nên với các kiểu này ranh giới giữ bằng review. Với `AppUser`: field kế thừa từ `IdentityUser<Guid>` nằm ngoài ranh giới; field do chính `AppUser` khai — như cờ `has_permission_bypass`, mà luật M12 cấm bật trên tài khoản đã tồn tại — thì nằm trong |

**Ghi của chính Identity cũng đi qua interceptor** (`UserManager` lưu bằng `SaveChangesAsync` cả khi tính
lần sai mật khẩu, đặt mốc khoá, đổi security stamp). Lần sai lúc đăng nhập chạy khi chưa có người —
`UpdatedBy` theo luật ca **không có người**: [`be-architecture.md`](be-architecture.md) §1.1, mục *Danh
tính và đơn vị khi không có request*.

---

## 2. Encapsulation — field nghiệp vụ `private set`

Field nghiệp vụ luôn `private set`; entity có ctor không tham số `private` cho EF Core (khối `Tenant`
đầy đủ ở §3.2). Mutate qua method mang **tên nghiệp vụ**, không qua setter:

```csharp
public Result Deactivate() { ... }               // ĐÚNG
tenant.IsActive = false;                          // SAI — không biên dịch được, và đó là mục đích
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §2

---

## 3. Factory & mutation method trả `Result<T>` — KHÔNG ném exception

### 3.1 Cách cũ — và vì sao bỏ

**Không** `throw new DomainException(...)` trong factory hay mutation method.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §3.1

### 3.2 Cách của repo này

```csharp
// Core.Domain/Tenants/TenantErrors.cs — catalog khai TRƯỚC, cạnh code trả ra nó
public static class TenantErrors
{
    public static readonly Error SystemImmutable = new(
        "CORE.TENANT.SYSTEM_IMMUTABLE", "Không được ngưng hoạt động đơn vị hệ thống.", ErrorType.BusinessRule);
}
```

```csharp
// Core.Domain/Entities/Tenant.cs
public sealed class Tenant : IAuditableEntity
{
    public Guid Id { get; init; } = EntityId.New();

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsSystem { get; private set; }

    // Bốn field của IAuditableEntity — public setter có chủ đích, §1.4
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Tenant() { }   // EF Core cần ctor không tham số

    // Khuôn mã đơn vị đã qua validator; mã lưu dạng chữ HOA — ../database/schema-core.md §1.3
    public static Result<Tenant> Create(string code, string name, bool isSystem = false)
        => Result.Success(new Tenant
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsActive = true,
            IsSystem = isSystem,
        });

    public Result Deactivate()
    {
        if (IsSystem)
            return Result.Failure(TenantErrors.SystemImmutable);

        IsActive = false;
        return Result.Success();
    }

    public Result Activate()
    {
        IsActive = true;
        return Result.Success();
    }
}
```

Mã và `ErrorType` của `CORE.TENANT.SYSTEM_IMMUTABLE` lấy từ card
[`../contracts/tenants.md`](../contracts/tenants.md) §3 ([`be-cqrs-handler.md`](be-cqrs-handler.md) §7.1);
catalog ở đây là **khuôn** đặt nó vào code.

> 🚨 **Catalog trên nằm ở `Core.Domain`, không phải `Core.Application`** — `Tenant` gọi `TenantErrors.*`,
> mà `Core.Domain` **không được** tham chiếu `Core.Application` (luật A1,
> [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2). Quy tắc ở §3.3.

Ba điều bắt buộc: **không gán `Id`** trong factory (initializer đã sinh bằng `EntityId.New()`, §1.1);
**không gán `CreatedAt`/`UpdatedAt`** (`AuditInterceptor` điền); **mutation method trả `Result`** không
generic khi không sinh giá trị mới.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §3.2

### 3.3 `Error` sống ở đâu

`Error`, `ErrorType`, `Result`, `Result<T>` khai ở **`Core.Domain`** — vì `Domain` cần chúng và `Domain`
không được tham chiếu `Application`. Catalog lỗi khai ở **cùng project với code ném ra nó** — không có
ngoại lệ, không có chỗ mặc định:

| Catalog | Ở đâu | Vì sao |
| --- | --- | --- |
| `TenantErrors` | **`Core.Domain/`** | Entity `Tenant` gọi nó trong chính invariant của mình (§3.2). Đặt ở `Application` thì `Core.Domain` không biên dịch được |
| Catalog chỉ được handler dùng | **`Core.Application/<Feature>/`** | Không entity nào gọi tới, nên không kéo `Domain` phụ thuộc ngược |

Hợp đồng đầy đủ của `Result<T>` và `Error`: [`be-cqrs-handler.md`](be-cqrs-handler.md).

### 3.4 Khi nào vẫn còn được ném exception

Chỉ cho **lỗi ngoài dự kiến**: mất kết nối DB, bug lập trình, vi phạm bất biến nội bộ. Ba dấu hiệu:
không có hành động nào người dùng làm được để tránh; không có câu nào hiển thị ngoài "đã có lỗi hệ
thống"; cần log kèm stack trace và cần ai đó xem. Luật `DomainAndApplication_MustNotThrow_BusinessException`
([`../RULES.md`](../RULES.md) §5) canh vế còn lại.

---

## 4. Value Object

Dùng khi khái niệm có **từ hai field đi cùng nhau**, hoặc có **luật định dạng** cần validate. **Không**
bọc VO cho một `decimal`/`string` đơn lẻ không có luật gì. `record` — so sánh theo giá trị.

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

> ⚠️ `.Value` trong nhánh đọc **ném** nếu dữ liệu trong DB không hợp lệ — đúng: đó là lỗi ngoài dự kiến
> (§3.4), không phải lỗi nghiệp vụ.

---

## 5. Soft delete và tenant — hai vòng lặp, không khai lẻ

> Mục này giữ **cách khai** hai bộ lọc toàn cục và cái bẫy index unique đi kèm cả hai. Vì sao
> multi-tenant làm như vậy, bẫy model cache, allowlist bỏ lọc, và ảnh hưởng lên Identity:
> [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md).

### 5.1 Chữ ký CoreDbContext — định nghĩa gốc

Khối dưới đây là **nguồn duy nhất** của chữ ký `CoreDbContext` và hai bộ lọc toàn cục; file khác — kể
cả [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) — chỉ trỏ về đây
([`../OWNERSHIP.md`](../OWNERSHIP.md) §2). Cả hai filter khai **một lần** — extension dùng chung, vòng
lặp trên model đã build, gọi ở cuối `OnModelCreating` của **mọi** `DbContext`. **Không**
`.Where(x => !x.IsDeleted)` ở từng query, **không** khai lẻ ở từng `IEntityTypeConfiguration<T>`.

```csharp
// Core.Infrastructure/Persistence/CoreQueryFilters.cs — dùng chung cho CoreDbContext và DbContext của mọi module
public interface ITenantFilteredContext
{
    // Đọc MỖI LẦN truy vấn chạy, không phải một lần lúc dựng model.
    Guid? CurrentTenantId { get; }
}

public static class CoreQueryFilters
{
    public const string SoftDeleteKey = "SoftDelete";
    public const string TenantKey     = "Tenant";

    // Gọi ở cuối OnModelCreating — PHẢI chạy SAU ApplyConfigurationsFromAssembly.
    public static void ApplyCoreQueryFilters<TContext>(this ModelBuilder modelBuilder, TContext context)
        where TContext : DbContext, ITenantFilteredContext
    {
        ApplySoftDeleteQueryFilters(modelBuilder);
        ApplyTenantQueryFilters(modelBuilder, context);
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
                .HasQueryFilter(SoftDeleteKey, Expression.Lambda(body, parameter));
        }
    }

    private static void ApplyTenantQueryFilters<TContext>(ModelBuilder modelBuilder, TContext context)
        where TContext : DbContext, ITenantFilteredContext
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType)) continue;

            var e = Expression.Parameter(entityType.ClrType, "e");
            // (Guid?)e.TenantId == context.CurrentTenantId
            // Vế phải trỏ vào INSTANCE DbContext, KHÔNG chụp một giá trị Guid lúc dựng model.
            var body = Expression.Equal(
                Expression.Convert(Expression.Property(e, nameof(ITenantScoped.TenantId)), typeof(Guid?)),
                Expression.Property(
                    Expression.Constant(context, typeof(TContext)),
                    nameof(ITenantFilteredContext.CurrentTenantId)));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(TenantKey, Expression.Lambda(body, e));
        }
    }
}
```

```csharp
// Core.Infrastructure/Identity/IdentityJoinTypes.cs — năm bảng join của Identity mang tenant_id
// (schema-core.md §4.3) nên PHẢI là lớp con cài ITenantScoped; kiểu mặc định của Identity không có TenantId
// và không qua vòng lọc tenant ở trên.
public class AppUserRole  : IdentityUserRole<Guid>,  ITenantScoped { public Guid TenantId { get; init; } }
public class AppUserClaim : IdentityUserClaim<Guid>, ITenantScoped { public Guid TenantId { get; init; } }
public class AppUserLogin : IdentityUserLogin<Guid>, ITenantScoped { public Guid TenantId { get; init; } }
public class AppRoleClaim : IdentityRoleClaim<Guid>, ITenantScoped { public Guid TenantId { get; init; } }
public class AppUserToken : IdentityUserToken<Guid>, ITenantScoped { public Guid TenantId { get; init; } }

// Core.Infrastructure/Persistence/CoreDbContext.cs — dạng khai ĐỦ KIỂU, để năm kiểu join trên thay kiểu mặc định
public class CoreDbContext(
    DbContextOptions<CoreDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<AppUser, AppRole, Guid, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken>(options),
      ITenantFilteredContext
{
    public Guid? CurrentTenantId => tenantContext.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(CoreSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);

        modelBuilder.ApplyCoreQueryFilters(this);   // PHẢI chạy SAU dòng trên
    }
}
```

```csharp
// Modules.<X>.Infrastructure/Persistence/<X>DbContext.cs — KHÔNG kế thừa IdentityDbContext
public class SkillDbContext(
    DbContextOptions<SkillDbContext> options,
    ITenantContext tenantContext)
    : DbContext(options), ITenantFilteredContext
{
    public Guid? CurrentTenantId => tenantContext.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("skill");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SkillDbContext).Assembly);

        modelBuilder.ApplyCoreQueryFilters(this);
    }
}
```

- **Module không kế thừa `IdentityDbContext`** — bộ lọc đi qua **extension**, không qua lớp cơ sở
  ([`../adr/0025-luu-du-lieu-module-mot-transaction.md`](../adr/0025-luu-du-lieu-module-mot-transaction.md)).
- **`CurrentTenantId` rỗng** ⇒ vế phải `null` ⇒ mọi truy vấn trên entity `ITenantScoped` trả **rỗng**.
- `ITenantContext` là seam ở `Core.Application` ([`be-architecture.md`](be-architecture.md) §1.1, vòng
  đời §5.2); `DbContext` nhận nó qua constructor, không đọc `HttpContext`
  ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2).
- `ITenantScoped` **độc lập với `BaseEntity`** (`AppUser`, `AppRole`), nên hai vòng lặp lọc theo **hai
  điều kiện khác nhau** — không chép điều kiện vòng này sang vòng kia.

> 🛑 **Đừng GỘP hai điều kiện vào một biểu thức** — filter không tên ghi đè nhau, và E3/M1 mất khả năng
> kiểm độc lập ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §3.1).
> **Đừng chụp giá trị `TenantId` vào biến cục bộ** rồi nhúng vào biểu thức — model được cache theo kiểu
> `DbContext` ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §4).

Ba việc bắt buộc kèm theo, mỗi việc có cổng ở [`../RULES.md`](../RULES.md) §9: `TenantId` do interceptor
gán lúc `Added`, không bao giờ sửa (M2); mọi index unique gồm `TenantId` (M3, §5.3); mọi lời gọi
`IgnoreQueryFilters` nằm trong allowlist (M5, §5.4).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §5.1

### 5.2 Ba ràng buộc, cả ba hỏng im lặng nếu bỏ

1. **Thứ tự.** Vòng lặp chạy **sau** `ApplyConfigurationsFromAssembly`.
2. **Filter phải có TÊN.** Không tên thì lần khai thứ hai **ghi đè** lần thứ nhất.
3. **Khai lẻ ở từng configuration là sai.** Entity mới quên khai thì mất filter, không có gì báo.

`EveryBaseEntityDescendant_HasNamedSoftDeleteQueryFilter` ([`../RULES.md`](../RULES.md) §4) canh cả ba
cho `SoftDelete`; `EveryTenantScopedEntity_HasTenantQueryFilter` ([`../RULES.md`](../RULES.md) §9) canh cả
ba cho `Tenant`. **Cả ba áp cho vòng lặp tenant y hệt.**

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §5.2

### 5.3 Cái bẫy đi kèm — index unique phải gồm BA thứ

Trên một entity vừa kế thừa `BaseEntity` vừa khai `ITenantScoped` — tức **đa số entity nghiệp vụ** —
index unique phải gồm cả ba:

```csharp
builder.HasIndex(x => new { x.TenantId, x.Code })   // 1. tenant  2. cột nghiệp vụ
    .IsUnique()
    .HasFilter("is_deleted = false")                // 3. mệnh đề lọc xoá mềm
    .HasDatabaseName("ux_menu_item_tenant_code_active");
```

`TenantId` **đứng đầu** ([`be-performance.md`](be-performance.md) §5.2). Luật `M3`
(`EveryUniqueIndex_OnTenantScoped_Includes_TenantId`) canh vế `TenantId`; quy ước đặt tên index và bảng
miễn trừ `TenantId`: [`../database/schema-core.md`](../database/schema-core.md) §1.3 và §3.7.

> 🪤 **Tên index có trần 63 byte**, PostgreSQL cắt **âm thầm**. Rút gọn phần **tên cột**; không bao giờ
> bỏ phần `tenant`.

> 🛑 **`HasFilter` nhận SQL THÔ — chuỗi là TÊN VẬT LÝ của cột** (`snake_case`,
> [`../database/schema-core.md`](../database/schema-core.md)): `is_deleted`, không phải `"IsDeleted"`.
> Cùng luật cho `HasComputedColumnSql`, `HasDefaultValueSql`, `FromSqlRaw`, và ràng buộc kiểm tra.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §5.3

### 5.4 Khi nào bỏ qua filter

`IgnoreQueryFilters` — luôn kèm tên filter — dùng đúng ba chỗ: màn hình khôi phục dữ liệu đã xoá, báo
cáo audit, migration dữ liệu. Mỗi lời gọi phải nêu được thuộc nhóm nào.

> 🛑 **Dạng KHÔNG THAM SỐ gỡ MỌI filter — cả tenant** — bị cấm ở **mọi** ca (luật B6). Nêu tên filter:
>
> ```csharp
> .IgnoreQueryFilters([CoreQueryFilters.SoftDeleteKey])   // giữ nguyên filter tenant
> ```
>
> Mã chạy **ngoài ngữ cảnh một đơn vị** (job nền toàn hệ, quản trị vận hành, migration) cần gỡ cả hai
> thì liệt kê **cả hai** tên. Mọi lời gọi nằm trong allowlist khai tường minh, kèm lý do (luật M5):
> [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §7.

---

## 6. Concurrency — dùng `xmin` của PostgreSQL

> 📖 **Mục này là recipe: khai token thế nào, cho entity nào.** Hai thứ KHÔNG có ở đây và bỏ sót thì hỏng
> nặng — đọc [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §5–§6:
> - **Token ở cấp TẬP HỢP** khi màn hình thay thế cả một tập (ma trận phân quyền), không phải sửa một dòng.
> - **Xử lý khi xung đột xảy ra** — bắt ở đúng một chỗ, và **cấm tự thử lại**.

### 6.1 Entity nào cần token — và entity nào không

Thêm token cho entity thoả **cả hai**:

1. Có **từ hai luồng ghi độc lập** chạm cùng một bản ghi.
2. Việc mất một thay đổi gây hậu quả người dùng quan tâm.

| Loại dữ liệu | Cần? | Vì sao |
| --- | --- | --- |
| Danh mục dùng chung, cấu hình hệ thống | ✅ | Nhiều người quản trị cùng sửa |
| Bản ghi có trạng thái theo quy trình | ✅ | Hai người cùng chuyển trạng thái là ca kinh điển |
| Người dùng, vai trò, phân quyền | ✅ | Màn quản trị và các luồng khác cùng ghi |
| Hồ sơ cá nhân, chỉ chính chủ sửa | ➖ | Một luồng ghi. Thêm cũng không hại, nhưng không giải quyết gì |
| Bảng chỉ ghi thêm, không sửa | ❌ | Không có cập nhật thì không có ghi đè |
| Bảng outbox, nhật ký | ❌ | Đồng thời xử lý bằng khoá lúc lấy bản ghi, không bằng token |

Phép kiểm "chỉ có một luồng ghi": liệt kê mọi đường ghi tới bảng đó bằng cách tìm trong code, đừng suy
từ trí nhớ.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §6.1

### 6.2 Recipe concurrency token cho Npgsql

```csharp
public sealed class MenuItem : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }

    // Concurrency token — PHẢI là uint. KHÔNG byte[], KHÔNG shadow property.
    public uint Version { get; private set; }
}
```

```csharp
// Core.Infrastructure/Persistence/Configurations/MenuItemConfiguration.cs
builder.Property(x => x.Version).IsRowVersion();
```

Npgsql bind property `uint` mang `IsRowVersion()` vào cột hệ thống **`xmin`** — không cột mới, không
migration riêng. EF Core tự thêm `WHERE xmin = @original` vào câu UPDATE; bị ghi đè thì ném
`DbUpdateConcurrencyException`, tầng trên dịch thành `ErrorType.Conflict` (409).

### 6.3 Kiểu CLR nào hợp lệ cho concurrency token — định nghĩa gốc

> `.IsRowVersion()` **đúng hay sai tuỳ KIỂU CLR của property, không tuỳ provider.**

| Kiểu CLR | Trên SQL Server | Trên PostgreSQL (Npgsql) |
| --- | --- | --- |
| `byte[]` | Ánh xạ sang kiểu `rowversion`, **DB tự tăng mỗi lần UPDATE** — đúng | Tạo một cột `bytea` bình thường mà **không ai cập nhật** → `WHERE "RowVersion" = @original` **luôn khớp** → check concurrency **vô hiệu hoàn toàn, im lặng** |
| `uint` | Không phải khuôn của SQL Server | Bind vào cột hệ thống `xmin` — **đúng** |

- File này là **file chủ duy nhất** của chủ đề concurrency ở khu `quy-uoc/`. File khác chỉ được trỏ tới
  đây, không được chép lại recipe.
- Đừng dùng `UseXminAsConcurrencyToken()` — Npgsql đã đánh obsolete rồi gỡ hẳn.
- Nếu sau này đổi sang SQL Server: đây là **một trong số ít chỗ phải sửa theo provider**, và lúc đó mới
  dùng `byte[] RowVersion`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §6.3

### 6.4 Không áp cho `AppUser`/`AppRole`

Chúng là entity của ASP.NET Core Identity và **đã có sẵn `ConcurrencyStamp`**. Đường cập nhật người
dùng dùng đúng nó; không thêm cột thứ hai.

---

## 7. Ranh giới Identity — `AppUser` không rời khỏi Infrastructure

### 7.1 Luật

`AppUser : IdentityUser<Guid>` và `AppRole : IdentityRole<Guid>` sống **chỉ** trong `Core.Infrastructure`.
`Core.Application` và `Core.Domain` **không bao giờ** thấy hai kiểu này — chúng chỉ thấy interface.

```csharp
// Core.Application/Identity/IIdentityService.cs — thông tin đăng nhập, mật khẩu, hiệu lực phiên; KHÔNG phát phiên
public interface IIdentityService
{
    Task<Result<CredentialCheck>> CheckCredentialsAsync(string userName, string password, CancellationToken ct);   // mật khẩu trước, khoá sau — wiki-core/be/02-identity-auth.md §4.2
    Task<Result<CredentialCheck>> ChangePasswordAsync(Guid userId, string current, string next, bool clearMustChangePassword, CancellationToken ct);   // stamp MỚI sau khi đổi — controller cấp lại cookie từ nó; clearMustChangePassword: true ở endpoint bắt buộc đổi (contracts/auth.md §7), false ở tự nguyện (§6)
    Task<bool> IsSessionValidAsync(Guid userId, string securityStamp, CancellationToken ct);   // gọi ở mọi request; false khi stamp lệch HOẶC đơn vị trong phạm vi ngưng hoạt động — be-api-controller.md §7.4
}

// Kết quả của hai thao tác trên — nguồn DUY NHẤT của SecurityStamp cho LoginOutcome (be-api-controller.md §7.4); handler không tra stamp ở đâu khác
public sealed record CredentialCheck(Guid UserId, string SecurityStamp, bool MustChangePassword);

// Core.Application/Identity/IUserLookupService.cs — chỉ đọc
public interface IUserLookupService
{
    Task<UserSummaryDto?> FindByIdAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<UserSummaryDto>> FindByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken ct);
}

// Đủ trường để handler `me` dựng SessionDto mà không chạm AppUser — be-api-controller.md §7.4
public sealed record UserSummaryDto(
    Guid Id,
    string UserName,
    string FullName,
    string? Email,
    bool IsLocked,
    bool IsSystemOperator,
    bool MustChangePassword,
    string? PreferredLanguage);

// Core.Application/Tenants/ITenantLookup.cs — tra đơn vị theo mã, TRƯỚC khi mở phạm vi đơn vị; không phải kiểu Identity
public interface ITenantLookup
{
    Task<TenantSummary?> FindByCodeAsync(string code, CancellationToken ct);
}

public sealed record TenantSummary(Guid Id, string Code, string Name, bool IsActive);

// Core.Application/Identity/IUserAdminService.cs — hành động quản trị
public interface IUserAdminService
{
    Task<Result<Guid>> CreateAsync(CreateUserInput input, CancellationToken ct);
    Task<Result> LockAsync(Guid userId, CancellationToken ct);
    Task<Result> UnlockAsync(Guid userId, CancellationToken ct);
    Task<Result> AssignRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct);
}
```

**Phát và thu hồi phiên không nằm ở interface nào ở đây** — cookie là việc của `Core.Web`
(`HttpContext.SignInAsync` / `SignOutAsync`), `ClaimsPrincipal` dựng qua `ISessionPrincipalFactory`:
[`be-api-controller.md`](be-api-controller.md) §7.4,
[`../adr/0026-ranh-gioi-identity-va-cookie.md`](../adr/0026-ranh-gioi-identity-va-cookie.md).
Luồng đăng nhập tra đơn vị bằng `ITenantLookup.FindByCodeAsync` — không thấy, hoặc `IsActive = false` ⇒ trượt cùng mã
với sai mật khẩu ([`../contracts/auth.md`](../contracts/auth.md) §3) — rồi mới nạp đơn vị vào phạm vi; `CheckCredentialsAsync`
chạy **sau** bước đó ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.1; thứ tự đầy đủ:
[`be-api-controller.md`](be-api-controller.md) §7.4). Luật
`IdentityTypes_MustNotLeak_OutsideInfrastructure` ([`../RULES.md`](../RULES.md) §6) canh ở mức kiểu.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §7.1

### 7.2 Hệ quả bắt buộc

- **DTO ra khỏi Identity là DTO của Application** (`UserSummaryDto`), không phải `AppUser`.
- **FK nghiệp vụ trỏ tới người dùng là `Guid` thuần**, không navigation sang `AppUser` — hệ quả của luật
  cấm FK vật lý xuyên schema ([`../database/migration-policy.md`](../database/migration-policy.md)).
- **`AppUser` không kế thừa `BaseEntity`**; nó **implement `IAuditableEntity`** — §1.4.

### 7.3 Đây là đánh đổi có ý thức

Đổi sang SSO/LDAP/OIDC sau này **phải sửa `Core.Infrastructure`** và cấu hình scheme ở `Core.Web` —
không đổi được bằng cấu hình; `Core.Application`, `Core.Domain`, handler, controller **không** phải sửa.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §7.3

---

## 8. FK và ranh giới schema

| Phạm vi | Loại FK | `DeleteBehavior` |
| --- | --- | --- |
| Cùng aggregate | Hard FK | `Cascade` |
| Cùng schema, khác aggregate | Hard FK | `Restrict` |
| **Khác schema** (Core ↔ module, module ↔ module) | **Soft FK** — `Guid` thuần, không constraint DB | Không có — chỉ `HasIndex`, kiểm tồn tại ở tầng Application |

Luật `NoForeignKey_CrossesSchemaBoundary` và `EveryMappedEntity_LivesInTheSchemaOfItsSide` canh hai vế.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-entity-domain.md`](../wiki-core/be/ly-do/be-entity-domain.md) §8

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
