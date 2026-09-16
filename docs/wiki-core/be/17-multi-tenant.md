---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 17. Multi-tenant — cách ly dữ liệu giữa các đơn vị

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`. Mọi code mẫu dưới đây là khuôn cho thứ sẽ được xây, không phải mô tả code đang chạy.
>
> **File chủ về thi công multi-tenant.** Quyết định gốc: [`../../adr/0013-multi-tenant.md`](../../adr/0013-multi-tenant.md) · các luật `M*` ép nó: [`../../RULES.md`](../../RULES.md) §9 · schema: [`../../database/schema-core.md`](../../database/schema-core.md).
>
> 🛑 **Đọc phần này trước phần còn lại.** Cách ly ở đây là cách ly **logic**: dữ liệu hai cơ quan nằm chung một bảng, chung một database, chung một tiến trình. Thứ duy nhất giữ chúng tách nhau là một biểu thức lọc EF Core gắn vào mọi truy vấn — không phải ranh giới vật lý, không phải quyền của PostgreSQL, không phải tường lửa.
>
> Một chỗ hở không gây lỗi biên dịch, không ném ngoại lệ, không xuất hiện trong log. Truy vấn chạy bình thường và **trả về nhiều dòng hơn đáng ra được thấy**. Người phát hiện ra nó, nếu có, là một cán bộ của đơn vị khác đang nhìn hồ sơ không thuộc về mình. Vì vậy mọi quy ước ở đây là **MUST**, mỗi cái kèm một cổng — không mục nào là "nên làm cho đẹp".

---

## 1. Multi-tenant là gì — và KHÔNG phải là gì

Đây là chỗ nhầm đắt nhất của cả mảng này, và nhầm được theo cả hai hướng: có đội dựng cả bộ máy multi-tenant cho bài toán chỉ cần lọc theo phòng ban; có đội gọi một bài toán cách ly thật là "lọc theo đơn vị" rồi để ngỏ đường rò dữ liệu.

**Phép thử, đúng một câu:** *có bao giờ cần một báo cáo tổng hợp xuyên qua các đơn vị không?*

| Trả lời | Kết luận | Cách giải |
| --- | --- | --- |
| **Có** — dù chỉ "thỉnh thoảng", "chỉ ban giám đốc", "chỉ cuối năm" | **Không phải multi-tenant** | Các đơn vị nằm trong **cùng một** tổ chức, chỉ khác quyền nhìn. Giải bằng permission cộng bộ lọc theo phạm vi: rẻ hơn nhiều, không mang rủi ro rò dữ liệu |
| **Không** — và người hỏi câu đó đang hiểu sai nghiệp vụ | **Đúng là multi-tenant** | Toàn bộ file này |

Ở Core này câu trả lời là vế thứ hai: tenant là **đơn vị hành chính hoặc tổ chức độc lập**, nhiều cơ quan dùng chung một bản cài, và **không bao giờ** có dữ liệu xuyên tenant. Hai thứ đó khác nhau ở **hướng của giá trị mặc định**, và khác biệt đó không hoà giải được: quên lọc trong mô hình phạm vi là thấy nhiều hơn cần — phiền; quên lọc trong mô hình tenant là thấy dữ liệu đơn vị khác — sự cố. Ở mô hình phạm vi, trách nhiệm lọc thuộc về từng truy vấn; ở mô hình tenant, nó phải thuộc về hạ tầng, một lần, không ai quên được.

### 1.1 Khi có người đề nghị "thêm báo cáo tổng hợp toàn tỉnh"

Ghi sẵn câu trả lời ở đây, vì đề nghị này **sẽ** xuất hiện, và luôn xuất hiện dưới dạng một yêu cầu nhỏ:

> **Đó không phải một tính năng. Đó là một thay đổi kiến trúc, và nó cần một ADR mới.**

Mô hình hiện tại không có khái niệm "trên tenant": không cây phân cấp đơn vị, không tài khoản thuộc nhiều tenant, không tầng nào được nhìn xuyên bộ lọc ngoài allowlist §7. Thoả mãn đề nghị đó bằng một ngoại lệ nghĩa là: (1) có một đường truy vấn hợp pháp đọc được dữ liệu **mọi** tenant, và đường đó thành mục tiêu tấn công đáng giá nhất của hệ thống; (2) câu hỏi "ai được xem báo cáo tổng hợp" không trả lời được bằng mô hình quyền hiện tại, vì mọi tài khoản thuộc **đúng một** tenant; (3) ngoại lệ đầu tiên biện minh cho ngoại lệ thứ hai, và allowlist §7 mất ý nghĩa.

Điều kiện lật quyết định nằm ở [`../../adr/0013-multi-tenant.md`](../../adr/0013-multi-tenant.md). Không sửa file đó — viết ADR mới.

---

## 2. `ITenantScoped` và `BaseEntity` — hai trục độc lập

`ITenantScoped` **không** nằm trong `BaseEntity` và không kế thừa nó. `BaseEntity` trả lời *"bản ghi này có vòng đời audit và xoá mềm không"*; `ITenantScoped` trả lời *"bản ghi này thuộc về một đơn vị nào không"*. Gộp hai câu hỏi là sai — có entity thuộc về một đơn vị mà không có vòng đời audit, và ngược lại.

```csharp
// Core.Domain/Common/ITenantScoped.cs — get-only: không phải dữ liệu người dùng nhập
public interface ITenantScoped { Guid TenantId { get; } }
```

| Khai `ITenantScoped` | Ví dụ |
| --- | --- |
| ✅ Mọi entity nghiệp vụ kế thừa `BaseEntity` | `MenuItem`, `MenuItemRole`, `Notification`, `RolePermission`, mọi entity của module |
| ✅ Entity **không** kế thừa `BaseEntity` nhưng thuộc về một đơn vị | `AppUser`, `AppRole` và **năm** bảng join của Identity (`app_user_role`, `app_user_claim`, `app_role_claim`, `app_user_login`, `app_user_token` — [`../../database/schema-core.md`](../../database/schema-core.md) §4.3) — Identity tự quản vòng đời ([`02-identity-auth.md`](02-identity-auth.md)); `OutboxMessage` — event phát ra nhân danh một đơn vị |
| ❌ Danh mục dùng chung toàn hệ | `Permission`, `PermissionResource` |
| ❌ Bảng của chính cơ chế tenant và bảng vận hành của bản cài | `Tenant`, `SchemaScriptHistory`, kho khoá bảo vệ dữ liệu |

Hàng thứ hai hay bị bỏ sót nhất: **`AppUser` mang `TenantId`**. Nó không kế thừa `BaseEntity` nên vòng lặp gắn filter soft delete bỏ qua nó — nhưng vòng lặp gắn filter tenant thì **không được** bỏ qua. Hai vòng lặp, hai điều kiện lọc khác nhau. Danh sách miễn trừ đầy đủ kèm lý do từng mục ở [`../../database/schema-core.md`](../../database/schema-core.md); luật M4 canh bằng ArchTest `EveryBusinessEntity_IsTenantScoped_OrExempt`. **Ai điền `TenantId`:** một interceptor EF, cùng chỗ với interceptor audit ([`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §1.3). Mọi entry `Added` cài `ITenantScoped` được gán `TenantId` của người đang gọi; lập trình viên **không** gán tay. **Chưa có đơn vị để gán thì interceptor từ chối lưu — luật M8** — không bao giờ gán `Guid.Empty`. Gán `Guid.Empty` thì bộ lọc làm bản ghi **biến mất khỏi mọi màn hình** ngay sau khi lưu, kể cả với chính người vừa tạo — triệu chứng *"tôi bấm lưu, báo thành công, nhưng danh sách không có"*, và không ai truy ra. Mã chạy ngoài request lấy đơn vị từ phạm vi ngữ cảnh thực thi — [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §1.1. Interceptor cũng chặn ca `Modified` có `TenantId` bị sửa: `TenantId` không bao giờ đổi sau khi tạo, nên đó là lỗi lập trình, ném ngoại lệ, không phải lỗi nghiệp vụ.

---

## 3. Bộ lọc truy vấn toàn cục — một vòng lặp, không khai lẻ

> 📖 **Chữ ký `CoreDbContext`, tên hai filter, và cả hai vòng lặp gắn filter: đọc [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §5.1.**

Điểm cần nhớ ở đây, và đây là phần thuộc về file này: filter tenant gắn **một lần** trong `OnModelCreating` bằng một vòng lặp trên model đã build, đúng khuôn filter xoá mềm. Khai lẻ ở từng `IEntityTypeConfiguration<T>` là sai — entity mới nào quên khai thì mất bộ lọc, và **không có gì báo**.

Ba tính chất của vòng lặp đó, cả ba đều là chỗ đã hỏng thật ở đâu đó:

| Tính chất | Bỏ qua thì |
| --- | --- |
| Điều kiện lọc là `typeof(ITenantScoped)`, **không** phải `typeof(BaseEntity)` | `AppUser` và `AppRole` mất filter tenant — chúng không kế thừa `BaseEntity` |
| Filter mang **tên riêng** (`Tenant`), không dùng filter khuyết danh | Filter không tên thì lần khai sau **ghi đè** lần trước, và thứ bị xoá có thể là filter tenant |
| Vế phải trỏ vào **instance** `DbContext` (`this.CurrentTenantId`), không chụp một giá trị `Guid` | Model được cache theo kiểu `DbContext`, nên giá trị của request đầu tiên dùng lại cho **mọi** tenant sau đó — §4 |

Vòng lặp tenant phải chạy **sau** `ApplyConfigurationsFromAssembly`, cùng lý do đã ghi cho soft delete: đảo thứ tự thì entity chỉ được đăng ký bởi configuration chưa có mặt trong model, và **mọi** entity đó mất filter — không lỗi, không cảnh báo. Luật M1 canh bằng ArchTest `EveryTenantScopedEntity_HasTenantQueryFilter`, duyệt model đã build và kiểm từng entity cài `ITenantScoped` có đúng một filter mang tên `Tenant`.

### 3.1 🪤 Cạm bẫy: gộp lọc tenant vào cùng biểu thức với lọc xoá mềm

Công thức lan truyền rộng nhất trên mạng gộp hai điều kiện vào **một** biểu thức:

```csharp
// SAI ở repo này
modelBuilder.Entity(clrType).HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
```

**Tầng 1 — filter KHÔNG TÊN thì lần khai sau ghi đè lần khai trước.** Một entity chỉ giữ đúng một filter không tên. Vòng lặp soft delete chạy trước, vòng lặp tenant chạy sau; cả hai khai filter không tên thì một trong hai **biến mất**. Không lỗi biên dịch, không cảnh báo, không ngoại lệ. Thứ duy nhất đổi là: một hàng rào không còn tồn tại.

| Filter bị ghi đè | Triệu chứng | Mức độ |
| --- | --- | --- |
| `SoftDelete` mất | Bản ghi đã xoá hiện lại trên màn hình | Phiền, người dùng báo ngay |
| `Tenant` mất | Đơn vị A nhìn thấy dữ liệu đơn vị B | **Sự cố**, có thể không ai báo trong nhiều tháng |

**Tầng 2 — gộp làm hai luật mất khả năng kiểm độc lập.** Luật E3 kiểm filter tên `SoftDelete`, M1 kiểm filter tên `Tenant`. Gộp lại thì một detector phải đi tìm biểu thức con bên trong biểu thức lớn, tức phân tích cây biểu thức thay vì kiểm sự tồn tại của một cái tên. Detector kiểu đó rất dễ viết sai, và một detector sai thì **xanh vĩnh viễn** (luật T1).

**Cách đúng: hai filter, mỗi filter một tên.** EF Core cho phép nhiều query filter đặt tên trên cùng một entity và `AND` chúng khi dịch truy vấn. Đổi lại, mỗi cái gỡ được **riêng lẻ** (§7.2) — lợi ích lớn hơn hẳn việc tiết kiệm một vòng lặp.

---

## 4. 🪤 Bẫy vòng đời `DbContext` — model được cache, tenant thì không

**Đây là lỗi rò dữ liệu nặng nhất và khó thấy nhất của toàn bộ mô hình này.** Đọc hết mục này trước khi viết dòng `HasQueryFilter` đầu tiên.

`OnModelCreating` **không** chạy mỗi request. Nó chạy đúng một lần cho mỗi khoá cache model — mặc định là *(kiểu `DbContext`, provider)*, do `IModelCacheKeyFactory` quyết định — và kết quả được giữ trong bộ nhớ suốt vòng đời tiến trình. Nghĩa là **mọi giá trị nhúng vào biểu thức filter lúc dựng model đều là giá trị của request ĐẦU TIÊN**, và mọi request sau, của mọi tenant, dùng lại đúng biểu thức đó.

```csharp
// 🛑 SAI. Đây là một lỗ rò dữ liệu giữa hai cơ quan.
var tenantId = tenantContext.TenantId;                     // chụp GIÁ TRỊ tại đây
modelBuilder.Entity<MenuItem>()
    .HasQueryFilter(CoreQueryFilters.TenantKey, e => e.TenantId == tenantId);
```

`tenantId` là biến cục bộ, nên trình biên dịch nhúng **giá trị** của nó vào cây biểu thức, và model được cache. Kịch bản hỏng, đủ ba bước: (1) cán bộ Sở A đăng nhập, gọi request đầu tiên sau khi tiến trình khởi động — model được dựng, filter mang hằng số `TenantId = <A>`; (2) cán bộ Sở B đăng nhập, `ITenantContext` của request đó trả về `<B>` — **đúng**; (3) nhưng truy vấn của B dùng model đã cache, và model đó lọc theo `<A>` — **Sở B nhìn thấy dữ liệu của Sở A**. Không ngoại lệ, không log. Test tích hợp chạy một tenant duy nhất thì **xanh hoàn toàn**. Trên máy lập trình viên, nơi chỉ có một tenant, lỗi này không tồn tại.

### 4.1 Cách viết đúng

Vế phải phải trỏ vào **instance `DbContext`**, không vào một giá trị:

```csharp
public Guid? CurrentTenantId => tenantContext.TenantId;  // ✅ dịch vụ Scoped theo request

modelBuilder.Entity<MenuItem>()
    .HasQueryFilter(CoreQueryFilters.TenantKey, e => e.TenantId == CurrentTenantId);
```

EF Core nhận ra biểu thức chạm vào chính `DbContext` và xử lý nó khác hẳn một hằng số: lúc biên dịch truy vấn, nó thay tham chiếu đó bằng **instance đang chạy** và biến giá trị thành **tham số của câu SQL**, không phải hằng số nhúng trong model. SQL sinh ra mang `WHERE tenant_id = @__tenantId`, tham số nạp lại ở mỗi lần thực thi. Cùng lý do, khối mẫu ở [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §5.1 dùng `Expression.Constant(context, …)` — nó nhúng **instance context**, không nhúng `Guid`.

Ba hệ quả bắt buộc. **`ITenantContext` đăng ký `Scoped`** — `Singleton` thì mọi request dùng chung một giá trị và bug quay lại y nguyên, chỉ đổi chỗ gây ra; `Scoped` là đúng, cùng vòng đời với `CoreDbContext` ([`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md)). **`CurrentTenantId` là property đọc mỗi lần**, không phải field gán trong constructor: field gán một lần vẫn chạy đúng ở đây vì context là scoped, nhưng nó tạo một khuôn dễ chép sang chỗ sai, ví dụ một `DbContext` lấy từ `IDbContextFactory`. Và **không để model phụ thuộc tenant** — cần dựng model **khác nhau** theo tenant thì `IModelCacheKeyFactory` **phải** được thay để khoá cache gồm cả tenant, nếu không mỗi tenant dùng lại model của tenant chạm vào tiến trình trước; nhưng câu trả lời đúng ở Core này là một model, một filter, chỉ giá trị tham số đổi.

### 4.2 Cách canh

Một câu cảnh báo trong tài liệu không đủ — công thức sai lan truyền rộng hơn file này. Cần integration test chạy trên PostgreSQL thật (luật T2), **hai tenant trong cùng một tiến trình**:

```csharp
[Fact]
public async Task QueryFilter_UsesTenantOfCurrentRequest_NotOfTheFirstOne()
{
    await SeedAsync(tenantA, code: "A-001");
    await SeedAsync(tenantB, code: "B-001");

    var seenByA = await QueryAllCodesAsync(tenantA);   // A chạm tiến trình TRƯỚC
    var seenByB = await QueryAllCodesAsync(tenantB);

    Assert.Equal(["A-001"], seenByA);
    Assert.Equal(["B-001"], seenByB);   // B không thấy gì của A, dù model do A dựng
}
```

**Thứ tự trong test này là một phần của test.** Tenant A phải chạy trước để nó là tenant dựng model. Đảo thứ tự thì test vẫn xanh cả khi code sai.

---

## 5. Nguồn của `TenantId` — luật M2

`TenantId` đến từ **claim trong phiếu xác thực**, gán một lần lúc đăng nhập, đọc qua `ITenantContext.TenantId` — **chỉ** seam đó mang đơn vị. Chỗ **duy nhất** đọc `HttpContext` để lấy nó là `HttpContextCurrentUser` trong **`Core.Web`** ([`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §2.1).

> **Không phải `Core.Infrastructure`.** Project đó bị **cấm** chứa bất cứ thứ gì phụ thuộc `HttpContext` — bảng ranh giới project ở [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §2. Đặt `HttpContextCurrentUser` vào Infrastructure là kéo cả `Microsoft.AspNetCore.Http` vào một project mà tầng dữ liệu và job nền cùng tham chiếu — job nền không có `HttpContext`, nên nó sẽ nhận một danh tính rỗng — và mọi lần ghi của nó bị interceptor từ chối (luật M8).

> 📖 Chữ ký `ICurrentUser` và `ITenantContext`, và vì sao chúng là hai seam: đọc [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §1.1.

> **Client tự khai tenant là client tự cấp quyền.**

Một endpoint dạng `GET /api/tenants/{tenantId}/ho-so` trông rất tự nhiên và rất sai: đổi một `Guid` trên thanh địa chỉ là đọc được dữ liệu cơ quan khác. Thêm một bước kiểm *"tenantId trên route có bằng tenant của tôi không"* cũng không cứu được: bước kiểm đó phải lặp ở **mọi** endpoint — tức quay về đúng mô hình "phụ thuộc kỷ luật từng người" mà bộ lọc toàn cục sinh ra để thay thế; quên một chỗ là lộ, và không có gì báo. Ngoài ra tham số đó **không mang thêm thông tin gì**: tenant của người gọi đã nằm trong phiếu xác thực. Cùng lý do áp cho header tự đặt (`X-Tenant-Id`) và cho một trường `tenantId` trong body — cả hai đều là dữ liệu client kiểm soát.

Luật M2 canh bằng ArchTest `TenantId_IsNeverBoundFrom_RequestInput`, quét mọi DTO và mọi tham số action tìm một cái tên mà model binder có thể gắn giá trị từ request. **Ngoại lệ duy nhất là bước đăng nhập:** trước khi đăng nhập thành công thì chưa có phiếu, nên chưa có `TenantId`. Đó là chỗ duy nhất tenant được xác định từ dữ liệu người dùng nhập, và nó kéo theo cả một dây hệ quả lên Identity — §11. Điểm phải nhớ ngay: giá trị nhập ở bước đó **chỉ là điều kiện tra cứu**, không phải một lời cấp quyền.

---

## 6. Mọi index duy nhất phải gồm `TenantId` — luật M3

> 📖 **Hình dạng index, thứ tự cột, mệnh đề lọc xoá mềm và bẫy tên 63 byte: đọc [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §5.3.**

Phần thuộc riêng khu này là **triệu chứng khi quên**, vì nó khác hẳn các lỗi tenant khác: thiếu `TenantId` trong unique index **không** gây rò dữ liệu, nó gây **từ chối sai**.

Sở A tạo hồ sơ mã `HS-2026-001`, thành công. Sở B tạo cùng mã → `23505 duplicate key`. Cán bộ Sở B tìm mã đó — **không thấy gì**, vì bộ lọc tenant đã làm bản ghi của Sở A vô hình với họ. Người dùng báo *"hệ thống nói mã này trùng nhưng tôi tìm không thấy"*, và họ đúng: thứ đang chiếm khoá là một dòng không màn hình nào của họ hiển thị, không báo cáo nào chạm tới, không có cách nào tự gỡ.

Bộ phận hỗ trợ tra bằng công cụ DB (không có bộ lọc) sẽ thấy dòng đó và dễ kết luận nhầm là người dùng nhìn sót.

Luật M3 canh bằng ArchTest `EveryUniqueIndex_OnTenantScoped_Includes_TenantId`.

---

## 7. Bỏ bộ lọc có kiểm soát — luật M5

| Ca chính đáng | Ví dụ | Vì sao |
| --- | --- | --- |
| **Job nền toàn hệ** | Bộ phát outbox, bộ dọn dữ liệu hết hạn | Không chạy trong ngữ cảnh request nên không có tenant nào để lọc theo. Bộ phát outbox **đọc đơn vị và người kích hoạt ghi trên chính dòng** rồi mở phạm vi ngữ cảnh thực thi theo từng dòng trước khi giao event cho handler — [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §1.1 |
| **Quản trị vận hành** | Health check đếm bản ghi theo tenant | Đây là công việc **về** các tenant, không phải công việc **của** một tenant. Màn danh sách đơn vị **không** thuộc ca này: `core.tenant` không mang bộ lọc đơn vị nên không có gì để bỏ |
| **Migration dữ liệu** | Backfill một cột mới cho toàn bảng | Chạy một lần, ngoài luồng người dùng |

Không có ca thứ tư. Cụ thể **không** chính đáng: "báo cáo tổng hợp" (§1.1), "để tra cứu cho nhanh", "chỉ dùng ở màn hình quản trị của đơn vị".

### 7.1 Allowlist khai ở đâu, và vì sao mỗi mục phải có lý do tại chỗ

Allowlist là một **danh sách trong code test**, không phải comment rải rác:

```csharp
// CoreAndSkill.ArchTests/MultiTenancy/IgnoreQueryFiltersAllowlist.cs
internal static class IgnoreQueryFiltersAllowlist
{
    public static readonly IReadOnlyDictionary<string, string> Entries = new Dictionary<string, string>
    {
        ["Core.Infrastructure.Outbox.OutboxDispatcher.FetchPendingAsync"] =
            "Job nền chạy ngoài ngữ cảnh request — không có tenant nào để lọc theo.",
    };
}
```

Luật M5 canh bằng ArchTest `EveryIgnoreQueryFilters_IsOnTheAllowlist` — mọi lời gọi `IgnoreQueryFilters` trong source phải khớp một khoá trong danh sách.

> 📖 Danh sách miễn trừ của luật M4 (bảng không mang `tenant_id`) dùng cùng hình thức thi công này; nội dung và lý do từng mục: đọc [`../../database/schema-core.md`](../../database/schema-core.md) §1.3.

**Vì sao lý do phải nằm ngay tại chỗ khai:** một lời gọi bỏ bộ lọc đặt sai chỗ là lỗ rò **im lặng**. Người review thấy một dòng trong allowlist mà không biết vì sao nó ở đó sẽ mặc định coi nó hợp lệ — allowlist khi đó chỉ còn là danh sách phải cập nhật, không còn là cổng. Lý do viết ngay cạnh biến câu hỏi *"dòng này còn đúng không"* thành một câu trả lời được trong ba giây.

### 7.2 Gỡ đúng một filter, không gỡ tất cả

Đây là lợi ích cụ thể của việc đặt tên hai filter riêng ở §3.1:

```csharp
// Màn hình khôi phục dữ liệu đã xoá: BỎ lọc xoá mềm, GIỮ NGUYÊN lọc tenant
var deleted = await db.MenuItems
    .IgnoreQueryFilters([CoreQueryFilters.SoftDeleteKey])
    .Where(x => x.IsDeleted)
    .ToListAsync(ct);
```

`IgnoreQueryFilters()` không tham số gỡ **mọi** filter, tenant lẫn soft delete. Ở màn hình khôi phục dữ liệu, đó là một lỗ rò toàn phần cho nhu cầu chỉ cần gỡ một nửa. **Quy tắc — luật B6:** dạng không tham số bị cấm ở **mọi** ca, kể cả các ca chính đáng ở §7. Mọi lời gọi nêu tên filter cần gỡ; cần gỡ cả hai thì liệt kê **cả hai** tên.

---

## 8. SQL thô KHÔNG đi qua bộ lọc — luật M6

Bộ lọc toàn cục là cơ chế của EF Core, gắn vào cây biểu thức LINQ. Một chuỗi SQL bạn tự viết **không đi qua đó**: `FromSqlRaw`, `FromSqlInterpolated`, `ExecuteSqlRaw`, `ExecuteSqlInterpolated`, `migrationBuilder.Sql(...)`, và mọi script chạy bằng `psql` đều chạm thẳng vào bảng chứa dữ liệu của **mọi** tenant. Đây là chỗ trừu tượng hoá rò ra, và nó rò đúng ở chỗ nguy hiểm nhất: người viết SQL thô thường đang làm một việc gấp, một tối ưu, hoặc một báo cáo — tức đúng lúc ít nghĩ tới cách ly nhất.

| Loại truy vấn | Bắt buộc |
| --- | --- |
| `FromSqlRaw` / `FromSqlInterpolated` trên bảng có `tenant_id` | Mệnh đề `WHERE tenant_id = {tenantId}` viết tường minh, tham số hoá |
| `ExecuteSqlRaw` / `ExecuteSqlInterpolated` | Như trên. Một `UPDATE` thiếu điều kiện tenant sửa dữ liệu của mọi cơ quan trong một lệnh |
| `migrationBuilder.Sql(...)` | Hoặc lọc theo tenant, hoặc là backfill toàn hệ có chủ đích — ghi rõ là ca nào ngay trong comment |
| Script vận hành chạy tay | [`../../database/script-runbook.md`](../../database/script-runbook.md) — mọi câu chạm bảng có `tenant_id` phải nêu tenant đang thao tác |

```csharp
// ĐÚNG — tham số hoá, điều kiện tenant tường minh, tên cột vật lý
var rows = await db.MenuItems
    .FromSql($"""
        SELECT * FROM core.menu_item
        WHERE tenant_id = {tenantContext.TenantId} AND is_deleted = false
          AND module_key = {moduleKey}
        """)
    .ToListAsync(ct);
```

Hai điểm dễ sai đi kèm: **nối chuỗi thay vì tham số hoá** — `$"… tenant_id = '{tenantId}'"` truyền vào `FromSqlRaw` là SQL injection, và ở đây nó injection vào đúng điều kiện đang giữ cách ly; và **dùng tên property thay vì tên cột vật lý** (`tenant_id`, không phải `TenantId`). Luật M6 canh bằng ArchTest `EveryRawSqlOnTenantTable_FiltersByTenant`.

> **Giới hạn của cổng này, nói thẳng:** một detector đọc chuỗi SQL trong source **không** kiểm được ngữ nghĩa. Nó bắt được ca thiếu hẳn chữ `tenant_id`; nó không bắt được `WHERE tenant_id = <sai tenant>`. M6 là hàng rào **cuối**, không phải hàng rào duy nhất — quy tắc thật vẫn là *đừng viết SQL thô trên bảng có tenant khi LINQ làm được*.

---

## 9. Truy cập xuyên tenant trả KHÔNG TÌM THẤY — luật M7

Khi bộ lọc hoạt động đúng, truy vấn theo id của tenant khác trả về **không dòng nào**. Handler nhận `null` và phải trả lỗi **không tìm thấy** (404), **không** phải cấm truy cập (403). Trả *cấm truy cập* là **xác nhận bản ghi đó tồn tại**.

| Phản hồi | Kẻ tấn công có tài khoản hợp lệ ở Sở A học được gì |
| --- | --- |
| 404 cho mọi id | Không phân biệt được "không có" với "có nhưng của người khác" — **không học được gì** |
| 403 cho một số id | Đúng những id đó **tồn tại** trong hệ thống, thuộc một cơ quan khác |

Với mã hồ sơ tuần tự (`HS-2026-0001`, `HS-2026-0002`…), khác biệt đó đủ để dựng lại **khối lượng hồ sơ, nhịp phát sinh và khoảng mã đang dùng** của mọi cơ quan khác — không cần đọc được một dòng nội dung nào. Cùng họ tấn công với dò tài khoản ở [`09-security-beyond-auth.md`](09-security-beyond-auth.md).

Hệ quả cho code: handler **không** tự so `TenantId` — bộ lọc đã làm việc đó, và handler xử lý ca "không tìm thấy" đúng như ca bản ghi thật sự không tồn tại. Vì vậy **không có** nhánh `if (entity.TenantId != tenantContext.TenantId) return Forbidden()`: nhánh đó vừa thừa vừa sai, vì nó chỉ chạy được khi bộ lọc đã bị bỏ — đúng chỗ không nên có nó. Mã lỗi trả về là mã "không tìm thấy" thông thường của catalog ([`16-i18n-va-ma-loi.md`](16-i18n-va-ma-loi.md)), không phải một mã riêng cho ca xuyên tenant; một mã riêng cũng là tín hiệu rò ra ngoài. Luật M7 canh bằng integration test `CrossTenantAccess_Returns_NotFound`.

---

## 10. Vòng đời tenant

| Thao tác | Có làm? | Hệ quả |
| --- | --- | --- |
| **Tạo** | ✅ | Thêm một dòng vào bảng tenant rồi chạy seed §11. Không phải một quy trình vận hành, không phải một lần triển khai |
| **Ngưng hoạt động** | ✅ | Đặt cờ hoạt động về sai. **Mọi tài khoản của tenant đó không đăng nhập được**, phiên đang mở trượt ở request kế tiếp |
| **Bật lại** | ✅ | Đảo cờ. Dữ liệu chưa đi đâu cả |
| **Xoá** | ❌ không bao giờ | Xem dưới |

**Ai thực hiện ba thao tác trên.**

| Thao tác | Đường | Ghi chú |
| --- | --- | --- |
| **Lần cài đặt đầu tiên** — đơn vị hệ thống, đơn vị nghiệp vụ đầu tiên, tài khoản `superadmin` (vận hành, ở đơn vị hệ thống) và `admin` (quản trị, ở đơn vị nghiệp vụ đầu tiên) | **Lệnh bootstrap chạy tay** — máy dev và bản thật dùng **cùng một** lệnh; lệnh gọi service tạo đơn vị dùng chung với endpoint tạo đơn vị ([`../../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../../adr/0023-dich-vu-tao-don-vi-dung-chung.md) · [`02-identity-auth.md`](02-identity-auth.md) §3.6) | Không có cách khác: chưa có tài khoản nào để đăng nhập vào bất kỳ màn hình nào. Mã, tên hai đơn vị và thông tin hai tài khoản đọc từ cấu hình và `user-secrets`; thiếu một giá trị thì lệnh dừng, không ghi dòng nào; chạy lại không nhân đôi |
| **Các đơn vị sau đó** — tạo, ngưng, bật lại | **Khu quản trị hệ thống**, bằng tài khoản vận hành | [`../../adr/0017-khu-quan-tri-he-thong.md`](../../adr/0017-khu-quan-tri-he-thong.md) · hợp đồng: [`../../contracts/tenants.md`](../../contracts/tenants.md) |

Một câu `INSERT` viết tay **không** dựng nổi một đơn vị dùng được: bước 3 của §11.4 bắt buộc đi qua `UserManager` mới có mật khẩu băm hợp lệ, nên đơn vị tạo kiểu đó không ai đăng nhập được.

**Đơn vị hệ thống là một dòng đặc biệt trong bảng đơn vị**, nhận diện bằng cột `is_system` — không bằng mã. Nó không có dữ liệu nghiệp vụ và không nhận người dùng thường. Mã của nó do người vận hành đặt qua cấu hình lúc bootstrap; `superadmin` gõ mã đó vào ô mã đơn vị ở form đăng nhập như mọi người dùng khác. Mọi chỗ liệt kê đơn vị cho người dùng chọn phải **loại nó ra**; quên là lộ ra một đơn vị không có thật với người dùng.

**Ngưng hoạt động chặn ở hai chỗ, không phải một:** ở bước đăng nhập, và ở bước dựng danh tính cho mỗi request. Chặn chỉ ở đăng nhập thì người đang có phiên vẫn dùng tiếp tới khi cookie hết hạn — mất đúng thứ "thu hồi tức thì" mà mô hình cookie ở [`02-identity-auth.md`](02-identity-auth.md) §1.3 chọn để có. Chặn chỉ ở mỗi request thì màn đăng nhập trả một lỗi chung chung thay vì một câu trả lời rõ ràng. Trạng thái tenant được đọc trên **mọi** request nên nó là ứng viên cache đầu tiên đáng cân nhắc — nhưng chỉ sau khi **đo** ([`11-performance-caching.md`](11-performance-caching.md)), và cache đó phải mất hiệu lực ngay khi cờ đổi, nếu không "ngưng hoạt động tức thì" lại thành "sau vài phút".

**Vì sao KHÔNG xoá tenant.** Bốn lý do, mỗi lý do đủ để một mình quyết định. **Xoá tenant là xoá dữ liệu của một cơ quan** — không thao tác nào trong hệ này đắt hơn khi làm nhầm, và không có đường hoàn tác. **Không xoá được "sạch"**: dữ liệu một tenant nằm rải ở mọi bảng của Core và của mọi module, một lệnh xoá bỏ sót một bảng để lại dữ liệu mồ côi mà bộ lọc làm nó **vô hình**, nên không ai phát hiện. **Id tenant có thể bị dùng lại** — một tenant mới tình cờ nhận lại `Guid` đó (khôi phục từ bản sao lưu, seed lại) làm dữ liệu mồ côi ấy **hiện ra** dưới tên đơn vị mới. Và **nghĩa vụ lưu trữ**: dữ liệu hành chính có thời hạn lưu theo quy định, thời hạn đó không kết thúc khi đơn vị ngừng dùng hệ thống ([`10-data-retention.md`](10-data-retention.md)).

Cần giải phóng dung lượng thì đó là **thao tác lưu trữ có kiểm soát**: xuất dữ liệu ra ngoài, đối chiếu, rồi xoá theo quy trình có ghi chép — không phải một nút bấm trên màn hình quản trị.

---

## 11. Ảnh hưởng lên Identity — phần thi công khó nhất

> Mục cần đọc kỹ nhất sau §4. Không phải lý thuyết: đây là những luồng thật phải viết khác đi.

Hai cơ quan hoàn toàn có thể dùng chung một địa chỉ email (`vanthu@…`), và gần như chắc chắn có cùng tên đăng nhập (`admin`, `vanthu`, `ketoan`). Nên tên đăng nhập và email không còn duy nhất toàn hệ mà duy nhất theo cặp **(`TenantId`, giá trị chuẩn hoá)**. Hệ quả trên schema: hai index Identity tự khai (`UserNameIndex`, `EmailIndex`) phải được **khai lại với tập cột mới** trong `OnModelCreating`. Tên index giữ nguyên — [`../../database/schema-core.md`](../../database/schema-core.md) §4.0 cấm đổi **tên** chúng, không cấm đổi **định nghĩa**.

### 11.1 Luồng đăng nhập — tìm người dùng nào?

`UserManager.FindByNameAsync` tra theo tên đăng nhập chuẩn hoá, và **tên đó giờ không còn định danh được một người**. Ba phương án:

| Phương án | Vì sao loại / chọn |
| --- | --- |
| **Giữ tên đăng nhập duy nhất toàn hệ**, chỉ email theo tenant | ❌ Loại. Buộc các cơ quan **độc lập** phải phối hợp với nhau về một thứ nội bộ. Sớm muộn ai đó giải bằng cách thêm hậu tố tên cơ quan vào tên đăng nhập — tenant trá hình nằm trong một chuỗi |
| **Suy tenant từ tên miền / subdomain** | ❌ Loại ở Core này. ADR-0013 chốt **một instance dùng chung**; ánh xạ tên miền → tenant biến việc thêm một cơ quan từ một dòng dữ liệu thành một thao tác hạ tầng. `Host` cũng là dữ liệu client gửi lên, nên hướng này bắt buộc kèm allowlist tên miền phía server |
| **Một ô "mã đơn vị" trên chính form đăng nhập** | ✅ Chọn. Không phải màn chọn tenant (ADR-0013 loại thứ đó): nó là **một ô nhập cùng cấp với ô tên đăng nhập**, điền **trước** khi xác thực, không phải chọn **sau** khi xác thực |

**Thứ tự bắt buộc của luồng:**

1. Nhận `(mã đơn vị, tên đăng nhập, mật khẩu)`.
2. Tra tenant theo mã đơn vị. Không tìm thấy, hoặc tìm thấy nhưng đã ngưng hoạt động → **trượt**, thông điệp giống hệt ca sai mật khẩu.
3. Nạp `TenantId` vừa tra được vào `ITenantContext` của **request này**.
4. **Chỉ sau bước 3** mới gọi `UserManager`. Từ đây bộ lọc tenant có hiệu lực và `FindByNameAsync` trả về đúng một người hoặc không ai.
5. Kiểm mật khẩu rồi khoá tài khoản theo thứ tự ở [`02-identity-auth.md`](02-identity-auth.md) §4.2, rồi cờ đổi mật khẩu lần đầu ở §4.3.
6. Phát phiếu xác thực mang claim `TenantId`.

> 🛑 **Bước 3 phải nằm trước bước 4.** Đảo lại thì `UserManager` chạy khi `ITenantContext` chưa có giá trị, và tuỳ cách khai giá trị rỗng, truy vấn hoặc trả rỗng (mọi người đều "sai mật khẩu") hoặc — tệ hơn nhiều — chạy như thể không có bộ lọc.
>
> Cách thay thế là cho luồng đăng nhập gọi `IgnoreQueryFilters` rồi tự thêm điều kiện tenant. Nó chạy được, nhưng thêm một mục vào allowlist §7 ở đúng luồng nhạy cảm nhất hệ thống. **Không chọn** — nạp ngữ cảnh trước rồi truy vấn bình thường là đường ít rủi ro hơn hẳn.

### 11.2 Quên mật khẩu tự phục vụ — ngoài v1

Luồng người dùng tự đặt lại mật khẩu qua email **không có ở v1** ([`../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)). Người quên mật khẩu liên hệ quản trị đơn vị của mình, và quản trị đặt lại hộ — [`02-identity-auth.md`](02-identity-auth.md) §4.4.

Ràng buộc multi-tenant mà luồng này phải giải nếu có quyết định đưa nó vào: một địa chỉ email có thể ứng với nhiều tài khoản ở nhiều tenant, nên form chỉ hỏi email không trả lời được *"đặt lại mật khẩu cho tài khoản nào"*; và phản hồi phải giống nhau cho mọi ca ([`09-security-beyond-auth.md`](09-security-beyond-auth.md) §3).

### 11.3 Tài khoản bootstrap là bootstrap CỦA MỘT TENANT

Cờ bootstrap ở [`02-identity-auth.md`](02-identity-auth.md) §3.6 giữ nguyên cơ chế, nhưng phạm vi đổi: tài khoản mang cờ đó bỏ qua kiểm **permission**, nó **không** bỏ qua bộ lọc tenant. Nó vẫn chỉ thấy dữ liệu của tenant mình. Phải nói rõ vì nó ngược trực giác *"tài khoản toàn quyền"*: trong mô hình này **không tồn tại** tài khoản nhìn được **dữ liệu nghiệp vụ** của mọi tenant. Việc quản trị chính các tenant đi qua khu hệ thống ở §10, bằng một tài khoản vận hành thuộc đơn vị hệ thống — nó thấy danh sách đơn vị, không thấy dữ liệu bên trong đơn vị nào ([`../../adr/0017-khu-quan-tri-he-thong.md`](../../adr/0017-khu-quan-tri-he-thong.md)). Thao tác duy nhất của nó chạm tới một tài khoản bên trong đơn vị là **khôi phục mật khẩu** cho một tài khoản quản trị của chính đơn vị đó, và thao tác ấy không trả dữ liệu nào — [`../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md). Tài khoản nào đủ điều kiện làm đích: [`../../contracts/tenants.md`](../../contracts/tenants.md) §4.

### 11.4 Seed cho một tenant mới

Một dòng trong bảng tenant chưa dùng được. Dựng một đơn vị dùng được là việc của **service tạo đơn vị dùng chung** — lệnh bootstrap và endpoint tạo đơn vị gọi cùng một service ([`../../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../../adr/0023-dich-vu-tao-don-vi-dung-chung.md)). Tenant mới cần đủ bốn thứ, theo thứ tự:

| # | Thứ | Nguồn | Ghi chú |
| --- | --- | --- | --- |
| 1 | **Bộ vai trò mặc định** | Seam `ITenantSeedSource` — mọi đăng ký được gộp; Core không có hằng số vai trò ([`02-identity-auth.md`](02-identity-auth.md) §3.5) | Vai trò mang `TenantId` — mỗi tenant có bộ vai trò riêng, đặt tên riêng, sửa riêng. Không nguồn nào khai vai trò thì đơn vị chạy bằng tài khoản mang cờ bypass ở bước 3 |
| 2 | **Ánh xạ vai trò → quyền** | Cùng seam | Danh mục quyền là **dùng chung toàn hệ**; chỉ ánh xạ mới thuộc tenant. Khoá quyền trong seed không có trong danh mục ⇒ tiến trình **không khởi động** |
| 3 | **Tài khoản quản trị đầu tiên** | Service tạo đơn vị | Bắt buộc qua `UserManager.CreateAsync` — không tạo được mật khẩu hợp lệ bằng SQL ([`../../database/schema-core.md`](../../database/schema-core.md) §4.1). Mang `has_permission_bypass` và `must_change_password` ([`../../database/schema-core.md`](../../database/schema-core.md) §4.1) |
| 4 | **Menu và danh mục nghiệp vụ** | Menu qua cùng seam — Core tự đăng ký menu của Core; danh mục nghiệp vụ do module seed | Bản ghi menu mang `TenantId` — mỗi đơn vị chỉnh menu của mình mà không đụng đơn vị khác |

- **Seed chạy trong ngữ cảnh tenant vừa tạo** — service tạo đơn vị mở phạm vi ngữ cảnh thực thi của tenant đó rồi mới ghi; nó là một trong số ít nơi luật A12 cho mở phạm vi ([`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §1.1). Ghi khi chưa có đơn vị thì interceptor §2 **từ chối lưu** (luật M8).
- **Seed chạy lại được** mà không nhân đôi dữ liệu, cùng nguyên tắc idempotent ở [`13-core-data-migration.md`](13-core-data-migration.md). Chỗ nào dùng `ON CONFLICT` thì mệnh đề phải lặp lại **nguyên văn** vị từ của index một phần, nay đã gồm cả `tenant_id`.
- **Tạo đơn vị chạy trong một transaction** — đơn vị, vai trò, ánh xạ quyền, tài khoản quản trị, menu và các dòng nhật ký của lần tạo đó cùng commit, hoặc không dòng nào được ghi ([`../../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../../adr/0023-dich-vu-tao-don-vi-dung-chung.md)). Hỏng ở bước nào cũng không để lại đơn vị *"đã tạo nhưng thiếu seed"* — thứ không ai chẩn đoán được từ giao diện.

---

## 12. Kiểm thử — danh sách bắt buộc

Tên cổng của từng luật `M*` nằm ở cột *Ép bằng gì* của [`../../RULES.md`](../../RULES.md) §9 — nguồn duy nhất, không chép sang đây; giải thích từng luật canh điều gì ở [`04-testing-strategy.md`](04-testing-strategy.md) §2.2.

Mỗi detector phải có một test `Detector_*` đối chứng chứng minh nó **bắt được** vi phạm (luật T1, [`04-testing-strategy.md`](04-testing-strategy.md)). Detector hỏng thì xanh vĩnh viễn, và ở nhóm luật này "xanh vĩnh viễn" nghĩa là hàng rào cách ly không tồn tại mà không ai biết. **Ca bắt buộc, không thay thế được bằng ArchTest:** *đăng nhập bằng tài khoản của tenant A, rồi gọi endpoint với id của một bản ghi thuộc tenant B.* Chạy end-to-end trên PostgreSQL thật (luật T2), qua đúng pipeline HTTP — không gọi thẳng handler. Đây là ca duy nhất chứng minh cả chuỗi hoạt động cùng nhau: claim → `ITenantContext` → query filter → kết quả rỗng → 404.

| Biến thể | Mong đợi | Bắt được lỗi gì |
| --- | --- | --- |
| A đọc bản ghi của B theo id | **404**, không phải 403 | Vi phạm M7 |
| A sửa / xoá bản ghi của B theo id | **404**, và dữ liệu của B **không đổi** | Ca `Update`/`Delete` không đi qua cùng đường đọc |
| A truy vấn danh sách sau khi B đã tạo dữ liệu | Danh sách của A **không chứa** gì của B | Bẫy model cache §4 |
| A và B cùng tạo bản ghi mã `X` | **Cả hai thành công** | `TenantId` thiếu trong unique index (M3) |
| Cùng một tenant tạo hai bản ghi mã `X` | Bản thứ hai bị chặn `23505` | Gỡ nhầm cả index |
| A xoá mềm mã `X` rồi tạo lại mã `X` | Thành công | Mệnh đề `is_deleted` bị bỏ khi thêm `TenantId` |

**Thứ tự là một phần của test:** tenant A phải là tenant chạm vào tiến trình trước, vì đó là tenant dựng model — đảo thứ tự thì bẫy §4 không lộ ra. Ba ca cuối phải đi cùng nhau: ca nào đứng một mình cũng xanh được bằng một cách sai.

---

## 13. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| `ITenantScoped` + cột `TenantId`; bộ lọc toàn cục tên `Tenant` gắn bằng vòng lặp | ✅ sẽ có | Độc lập với `BaseEntity` (§2); hai filter đặt tên, không gộp một biểu thức (§3.1) |
| `TenantId` đọc qua instance `DbContext` | ✅ sẽ có | Chống bẫy model cache — §4, mục nguy hiểm nhất của cả file |
| Interceptor gán `TenantId` lúc `Added`, chặn sửa lúc `Modified` | ✅ sẽ có | §2. `TenantId` lấy từ claim của phiếu xác thực qua `ITenantContext` — §5 |
| Ô mã đơn vị trên form đăng nhập | ✅ sẽ có | §11.1. **Không** phải màn chọn tenant |
| Unique index gồm `TenantId` + mệnh đề `is_deleted`; allowlist `IgnoreQueryFilters` có lý do tại chỗ khai | ✅ sẽ có | §6 (ba thành phần, không phải hai) · §7.1 |
| Vòng đời tenant + seed cho tenant mới chạy lại được | ✅ sẽ có | §10, §11.4. Lệnh bootstrap và endpoint tạo đơn vị dùng chung một service — [`../../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../../adr/0023-dich-vu-tao-don-vi-dung-chung.md) |
| **Quên mật khẩu tự phục vụ** | ❌ chưa | §11.2. Ngoài v1 — [`../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) |
| **Số đơn vị dự kiến** | ✅ dưới 50 | Xem lại mô hình cột phân biệt đơn vị của [`../../adr/0013-multi-tenant.md`](../../adr/0013-multi-tenant.md) khi đạt **50 đơn vị**, hoặc khi **một** đơn vị vượt **1 triệu** bản ghi nghiệp vụ |
| Bảy cổng M1–M7 kèm test `Detector_*` đối chứng | ✅ sẽ có | §12 |
| **Xoá tenant** | ❌ loại, không hoãn `K05` | §10. Giải phóng dung lượng đi qua thao tác lưu trữ có kiểm soát |
| **Báo cáo tổng hợp xuyên tenant** | ❌ loại, không hoãn `K06` | §1.1. Muốn lật thì viết ADR mới, không mở finding |
| **Một tài khoản thuộc nhiều tenant** | ❌ loại, không hoãn `K07` | Điều kiện lật ở [`../../adr/0013-multi-tenant.md`](../../adr/0013-multi-tenant.md) |
| **Tài khoản nhìn được dữ liệu nghiệp vụ của mọi tenant** | ❌ loại, không hoãn `K08` | Khác với tài khoản vận hành ở dòng dưới — xem §11.3 |
| **Tài khoản vận hành hệ thống** | ✅ sẽ có | [`../../adr/0017-khu-quan-tri-he-thong.md`](../../adr/0017-khu-quan-tri-he-thong.md). Chỉ thấy danh sách đơn vị; bộ lọc đơn vị vẫn chặn nó khỏi mọi dữ liệu nghiệp vụ |
| **Schema riêng / database riêng cho mỗi tenant · cây phân cấp đơn vị** | ❌ loại, không hoãn `K09` | Đã cân nhắc và loại trong ADR-0013; không có tenant cha — §1 |
| **Cache trạng thái hoạt động của tenant** | ❌ chưa | §10. Chỉ sau khi đo, và phải mất hiệu lực ngay khi cờ đổi |
| **Suy tenant từ tên miền / subdomain** | ❌ chưa | §11.1. Cần khi một cơ quan muốn tên miền riêng — khi đó bắt buộc kèm allowlist tên miền phía server |

Một finding dạng *"nên có báo cáo toàn tỉnh"* hoặc *"nên cho quản trị viên xem mọi tenant"* chỉ hợp lệ khi kèm một ADR mới lật ADR-0013 — không phải khi kèm một yêu cầu nghiệp vụ.
