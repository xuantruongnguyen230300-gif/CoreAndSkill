---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng — `be-cqrs-handler.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`../../../quy-uoc/be-cqrs-handler.md`](../../../quy-uoc/be-cqrs-handler.md);
> luật ở đó. Số mục ở đây **trùng số §** của file luật; mục không có gì dời thì ghi "—".
>
> Nhãn trạng thái đặt **tại từng khối**, không cho cả tệp: khối nào chỉ là khuôn minh hoạ thì nói ngay ở
> khối đó; khối còn lại tả mã trong `src/BE` nhưng chưa đối chiếu từng khối (`verified: chua-doi-chieu`) —
> so với tệp nguồn trước khi dựa vào.

---

## 1. Command / Query qua MediatR

Command/Query là `sealed record` bất biến còn vì so sánh theo giá trị dùng được cho cache và test.
`ICommandBase` rỗng để `TransactionBehavior` ràng buộc theo nó, nên query **không bao giờ** đi qua
transaction — [`be-cqrs-handler.md`](../../../quy-uoc/be-cqrs-handler.md) §5.

---

## 2. `Result<T>` — hợp đồng đầy đủ

### 2.1 `ErrorType` và `Error`

`Unauthorized` không phải kết quả của một use case: "chưa đăng nhập" là sự thật của **đường vào HTTP**,
cookie scheme phát nó trước khi request tới handler nào. Ánh xạ `ErrorType → HTTP` ở
[`be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §1.1 là một `switch` đầy đủ nhánh, nên thêm
hay bớt một giá trị mà quên sửa ánh xạ là **lỗi biên dịch** — đó là lý do hai chỗ phải sửa cùng lượt.

Ba điều cố ý trong thiết kế `Error`:

- **`Code` là khoá, `MessageTemplate` là dự phòng.** Client dựng câu từ `Code`; câu tiếng Việt trong
  `MessageTemplate` chỉ dùng cho log và cho trường hợp client chưa có bản dịch.
- **`Params` là từ điển theo TÊN**, không phải mảng theo thứ tự. Lý do đầy đủ ở §8.2 dưới đây.
- **`Error` là `record` bất biến**, `WithParams` trả bản mới. Nhờ vậy các `Error` trong catalog là
  `static readonly` dùng chung được cho mọi request mà không sợ một request ghi đè tham số của request
  khác — cùng bài học với `ValidationContext` ở §6.2.

### 2.2 `Result` và `Result<T>`

**`IResultFactory<TSelf>` với `static abstract` là chi tiết quan trọng, không phải trang trí.** Nó cho
phép pipeline behavior dựng một `Result` thất bại của **đúng kiểu trả về của request** mà không cần
reflection — §5.4 dưới đây giải thích vì sao đó là điểm mấu chốt.

`Value` ném khi `IsFailure` — có chủ đích. Đọc `Value` mà chưa kiểm `IsSuccess` là **bug lập trình**,
không phải lỗi nghiệp vụ, nên nó thuộc nhóm được phép ném
([`be-entity-domain.md`](../../../quy-uoc/be-entity-domain.md) §3.4).

### 2.3 Compose — map/bind thay vì lồng `if`

So sánh hai cách viết cùng một use case:

```csharp
// ❌ Lồng if — mỗi bước thêm một mức thụt lề
var itemResult = MenuItem.Create(/* … các trường của cmd */);
if (itemResult.IsFailure) return Result.Failure<Guid>(itemResult.Error!);

var attached = await EnsureParentAttachableAsync(itemResult.Value, ct);   // Result<MenuItem>
if (attached.IsFailure) return Result.Failure<Guid>(attached.Error!);

var saved = await AddWhenCodeIsFreeAsync(attached.Value, ct);             // Result<MenuItem>
if (saved.IsFailure) return Result.Failure<Guid>(saved.Error!);

return Result.Success(saved.Value.Id);
```

Bản `Bind` tương đương:

```csharp
// ✅ Bind — nhánh lỗi tự truyền, chỉ còn nhánh thành công phải đọc
return await MenuItem.Create(/* … các trường của cmd */)
    .BindAsync(item => EnsureParentAttachableAsync(item, ct))
    .BindAsync(item => AddWhenCodeIsFreeAsync(item, ct))
    .MapAsync(item => item.Id);
```

> ⚠️ Ví dụ minh hoạ — dùng để dạy khuôn `Bind`, không phải mã đang chạy.
> ✅ CÓ THẬT (đối chiếu 2026-10-06): `MenuItem` có trong `src/BE/Core/CoreAndSkill.Core.Domain/Menu/MenuItem.cs`
> (chuỗi `public static Result<MenuItem> Create(`), nhưng không lệnh nào tạo nó qua handler — menu ở v1 là dữ
> liệu seed, luật "cây một cấp" kiểm trên tập mục ở `TenantSeedValidator.ValidateMenuItems`. Hai hàm phụ
> `EnsureParentAttachableAsync` và `AddWhenCodeIsFreeAsync` không có trong `src/BE`.
> Trong ví dụ, `EnsureParentAttachableAsync` kiểm luật "cây menu đúng một cấp", `AddWhenCodeIsFreeAsync` kiểm
> mã trùng trong đơn vị rồi thêm vào repository — luật của bảng ở
> [`schema-core.md`](../../../database/schema-core.md) §6.1.

Vì sao có ngưỡng "từ ba bước trở lên": để chuỗi không trở thành thứ khó đọc hơn cái nó thay thế. Khi
một bước cần thêm dữ liệu từ hai bước trước đó, chuỗi phải bọc tuple và tuple làm mất luôn cái lợi.

---

## 3. Handler — bốn bước, KHÔNG tự quản transaction

### 3.1 Handler KHÔNG làm gì

—

### 3.2 Handler PHẢI tự làm

Validator giữ thuần, không truy cập DB, để nó test được mà không cần hạ tầng, và để không có hai chỗ
cùng đọc DB trong một lượt xử lý.

Phân biệt hàng 1 và hàng 2 của bảng `ErrorType` là chỗ hay sai nhất: *"resource chính của route"* trả
404; *"một tham chiếu bên trong dữ liệu gửi lên"* trả 422. Trộn hai ca này khiến FE không phân biệt
được "gọi sai URL" với "dữ liệu gửi lên tham chiếu sai".

Ca soft delete cố ý **không** tách mã riêng: trả một mã khác cho "đã xoá" so với "không tồn tại" là
tiết lộ sự tồn tại của dữ liệu đã xoá cho người không có quyền thấy nó.

### 3.3 Vì sao handler không tự quản transaction

Ba lý do, lý do thứ ba là lý do thật:

1. **Lặp lại.** Mỗi command sẽ phải mở/commit/rollback giống hệt nhau.
2. **Dễ quên.** Một command quên `SaveChangesAsync` thì im lặng không ghi gì. Không lỗi, không cảnh
   báo, chỉ là dữ liệu không xuất hiện.
3. **Chiến lược thử lại của EF không bọc được transaction do code tự mở.** Khi `EnableRetryOnFailure`
   bật (và nó phải bật — [`be-performance.md`](../../../quy-uoc/be-performance.md) §7.2), một
   `BeginTransactionAsync` gọi tay sẽ ném `InvalidOperationException` lúc **chạy**, không lúc biên dịch,
   và chỉ trên đúng đường ghi đó. Gom transaction về một chỗ nghĩa là chỉ có **một** chỗ phải bọc bằng
   execution strategy cho đúng.

---

## 4. `IUnitOfWork` — hợp đồng transaction

Mọi `DbContext` của một request dùng chung một `DbConnection`, nên outbox và nhật ký ở schema `core`
commit cùng dữ liệu của module, hoặc cùng rollback.

Khối `UnitOfWork` chép từ mã thật, rút gọn chú thích — thứ tự các bước là luật ở
[`be-cqrs-handler.md`](../../../quy-uoc/be-cqrs-handler.md) §4. ✅ CÓ THẬT (đối chiếu 2026-10-06):
`src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/UnitOfWork.cs` (chuỗi
`public sealed class UnitOfWork(DbConnection connection, IEnumerable<DbContext> contexts, ILogger<UnitOfWork> logger)`).

```csharp
// Core.Infrastructure/Persistence/UnitOfWork.cs
public sealed class UnitOfWork(DbConnection connection, IEnumerable<DbContext> contexts, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private readonly IReadOnlyList<DbContext> _contexts = contexts.ToList();

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var total = 0;
        foreach (var context in _contexts)
            total += await context.SaveChangesAsync(ct);

        return total;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<TransactionOutcome<T>>> operation,
        CancellationToken ct = default)
    {
        // Mọi context trỏ vào cùng một kết nối ⇒ chung provider ⇒ chung chiến lược thử lại.
        var strategy = _contexts[0].Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Mỗi lần thử lại phải bắt đầu từ dữ liệu SẠCH: giữ lại instance đã sửa dở của
            // lần trước sẽ hoặc ghi đè bằng dữ liệu cũ, hoặc vấp lỗi concurrency khó hiểu.
            foreach (var context in _contexts)
                context.ChangeTracker.Clear();

            // Kết nối do UoW mở, không phải EF — lượt thử lại sau khi kết nối đứt phải tự mở lại. Kết nối đứt
            // có thể ở trạng thái Broken: Close trước (no-op khi đã Closed) rồi mới Open.
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.CloseAsync();
                await connection.OpenAsync(ct);
            }

            await using var transaction = await connection.BeginTransactionAsync(ct);

            foreach (var context in _contexts)
                await context.Database.UseTransactionAsync(transaction, ct);

            try
            {
                var outcome = await operation(ct);

                if (outcome.ShouldCommit)
                {
                    foreach (var context in _contexts)
                        await context.SaveChangesAsync(ct);

                    await CommitAsync(transaction);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                }

                return outcome.Value;
            }
            finally
            {
                foreach (var context in _contexts)
                    await context.Database.UseTransactionAsync(null, ct);
            }
        });
    }

    // Luật E12 — be-cqrs-handler.md §4 luật 2: lỗi ném từ commit mà KHÔNG phải PostgresException là lỗi đường
    // truyền, không biết máy chủ đã commit hay chưa — bọc thành ngoại lệ KHÔNG tạm thời để execution strategy
    // không chạy lại. Luật E14 — ADR-0055: commit nhận CancellationToken.None, KHÔNG nhận token của request.
    private async Task CommitAsync(DbTransaction transaction)
    {
        try
        {
            await transaction.CommitAsync(CancellationToken.None);
        }
        catch (Exception ex) when (ex is not PostgresException)
        {
            var traceId = Activity.Current?.TraceId.ToHexString();
            logger.LogError(
                "Commit không rõ kết quả (traceId {TraceId}) — dữ liệu có thể đã được ghi; KHÔNG chạy lại đơn vị công việc. " +
                "Cần người đối soát. {Failure}",
                traceId, FailureText.DescribeWithStack(ex));
            throw new CommitOutcomeUnknownException(traceId, ex);
        }
    }
}
```

Ba bẫy đã đóng sẵn trong đoạn trên, cả ba đều nổ **lúc chạy** chứ không lúc build:

- Transaction tự mở mà không bọc bằng `CreateExecutionStrategy()` → `InvalidOperationException`.
- Không `ChangeTracker.Clear()` ở đầu mỗi lượt thử lại → lượt thứ hai dùng lại instance đã sửa dở của
  lượt đầu.
- Một `DbContext` không được gắn vào transaction chung (`UseTransactionAsync`) → nó commit riêng: dòng
  outbox của Core đã lưu trong khi dữ liệu module rollback, hoặc ngược lại. Không lỗi, không cảnh báo —
  chỉ là một event được phát cho một thay đổi không tồn tại.

---

## 5. Pipeline behavior — đúng HAI cái ở v1

### 5.1 Danh sách và thứ tự đăng ký

**`includeInternalTypes: true`:** bộ quét của FluentValidation mặc định **chỉ nhận kiểu public**. Thiếu
cờ này thì request đi thẳng vào handler với dữ liệu chưa kiểm — không lỗi, không cảnh báo.

**Thứ tự đăng ký không đổi được, và lý do là chi phí:** `ValidationBehavior` ngoài `TransactionBehavior`
nghĩa là input sai bị chặn lại **trước khi** một transaction được mở. Đảo lại thì mọi request hỏng đều
mở rồi rollback một transaction — tốn một round-trip tới DB cho một lỗi đáng lẽ tính được từ payload.

Luật A10 (`EveryPipelineBehavior_IsRegistered_ExactlyOnce` — 📐 chưa có ArchTest,
[`RULES-BE.md`](../../../RULES-BE.md)) đòi đúng một lần đăng ký: đăng ký hai lần khiến mỗi request chạy
behavior hai lượt. Đây là lý do **module không được tự đăng ký behavior** — behavior là open-generic, tự
áp cho mọi request bất kể handler nằm ở assembly nào.

### 5.2 `ValidationBehavior` — trả `Result` lỗi, KHÔNG ném

Khối đầy đủ — hợp đồng từng bước là luật ở [`be-cqrs-handler.md`](../../../quy-uoc/be-cqrs-handler.md) §5.2:

```csharp
// Core.Application/Common/Behaviors/ValidationBehavior.cs
internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultFactory<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();

        foreach (var validator in validators)
        {
            // MỘT ValidationContext RIÊNG cho MỖI validator — xem §6.2
            var context = new ValidationContext<TRequest>(request);
            var result = await validator.ValidateAsync(context, ct);

            if (!result.IsValid)
                failures.AddRange(result.Errors);
        }

        if (failures.Count == 0)
            return await next(ct);

        return TResponse.FromError(BuildValidationError(failures));
    }

    private static Error BuildValidationError(List<ValidationFailure> failures)
    {
        var byField = failures
            .GroupBy(f => f.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<FieldError>)g.Select(ToFieldError).ToList(),
                StringComparer.Ordinal);

        return CommonErrors.ValidationFailed.WithFieldErrors(byField);
    }

    private static FieldError ToFieldError(ValidationFailure failure)
        => new(failure.ErrorCode,   // luôn là mã catalog — mỗi rule bắt buộc WithErrorCode (be-cqrs-handler.md §7.5), không có mã dự phòng
               MessageParamPolicy.Filter(failure.FormattedMessagePlaceholderValues));
}
```

**Vì sao trả `Result` chứ không ném `ValidationException`:**

| | Ném exception | Trả `Result` (chọn cách này) |
| --- | --- | --- |
| Ai bắt | Middleware toàn cục — cách handler bốn tầng | `ResultToHttpMapper`, cùng đường với mọi lỗi khác |
| Chi phí | Dựng stack trace cho một luồng **mong đợi** | Không |
| Số đường dựng envelope lỗi | Hai (một cho exception, một cho `Result`) | **Một** |
| Test được không | Phải `Assert.Throws` | `Assert` trên giá trị trả về |

Vế "số đường dựng envelope lỗi" là vế quyết định: hai đường thì chúng sẽ lệch nhau, và ở dự án tiền
nhiệm chúng đã lệch — nhánh validation từng dựng envelope **bằng tay**, không mang `businessCode` nào,
trong khi nhánh còn lại đi qua factory chuẩn.

**Vì sao `MessageParamPolicy.Filter` bắt buộc:** chuyển tiếp cả từ điển
`FormattedMessagePlaceholderValues` nghĩa là mật khẩu mới vừa nhập đi ra HTTP response ở đúng lần nhập
hỏng — vì khoá `PropertyValue` luôn có mặt và luôn mang giá trị người dùng vừa gõ.

### 5.3 `TransactionBehavior` — chỉ bọc command

`IQuery<T>` không kế thừa `ICommandBase`, nên DI của .NET bỏ qua behavior này khi đóng generic cho một
query — không có chi phí nào, và không có nhánh nào để ai đó sửa nhầm sau này.

Vì sao query không cần transaction: một câu `SELECT` đơn lẻ đã atomic. Bọc nó lại chỉ thêm
`BEGIN`/`COMMIT` cho mỗi lượt đọc — nghĩa là gấp ba số round-trip cho việc không đổi lấy gì.

Điểm commit chỉ khi `response.IsSuccess`: handler trả `Result` thất bại thì mọi thay đổi bị rollback —
kể cả khi handler đã kịp `Add` vài entity trước khi phát hiện vi phạm. Handler mà tự gọi
`SaveChangesAsync` thì lỗi phát hiện sau đó không rollback được nữa.

**Vì sao cần `INoTransaction`, và vì sao chỉ một ca.** Quy tắc "thất bại thì rollback" đúng cho mọi lệnh
nghiệp vụ — trừ lệnh mà chính *thất bại* là thứ phải được ghi lại. Đăng nhập sai mật khẩu trả `Result`
thất bại (`CORE.AUTH.INVALID_CREDENTIALS`), nhưng `UserManager.AccessFailedAsync` vừa tăng bộ đếm và có
thể vừa đặt mốc khoá; bọc trong transaction thì `IsSuccess == false` làm rollback đúng hai giá trị đó, và
khoá tài khoản sau N lần sai **không bao giờ xảy ra** — đúng dấu hiệu *"test khoá tài khoản phải được nới
mới xanh"* mà ADR-0026 liệt kê. Marker đặt ở tầng kiểu để ai đọc `LoginCommand` thấy ngay nó là ngoại
lệ; điều kiện là một dòng ở đầu `Handle` để behavior không mọc thêm nhánh.

Không ai gọi `SaveChangesAsync` thay cho lệnh này vì không cần: `UserManager` đi qua store EF với
`AutoSaveChanges` mặc định bật, mỗi thao tác tăng bộ đếm, đặt mốc khoá, xoá bộ đếm tự lưu ngay. Đó cũng là
lý do marker **không** dùng được cho lệnh ghi qua repository của chính ta — không có transaction thì không
có `SaveChangesAsync` nào, và thay đổi im lặng biến mất. Ca thứ hai xuất hiện thì phải qua `architect`.

### 5.4 Vì sao behavior KHÔNG dùng reflection

Cách thường gặp để dựng một `Result<T>` thất bại bên trong một behavior generic là gọi
`typeof(Result<>).MakeGenericType(...)` rồi `Activator.CreateInstance`. `IResultFactory<TSelf>` với
`static abstract` (có từ C# 11, dùng bình thường trên .NET 10) cho phép viết `TResponse.FromError(error)`
— compiler phân giải, không reflection, không boxing, và **đổi chữ ký thì đỏ lúc biên dịch** thay vì
trả `null` lúc chạy.

Đây không phải sở thích. Ở dự án tiền nhiệm, cầu nối lỗi dựng bằng `GetMethod` + `Invoke` với danh
sách kiểu hardcode; khi chữ ký đổi, `GetMethod` trả `null` và **mọi lỗi nghiệp vụ biến thành
`NullReferenceException`** — hỏng đúng nhánh lỗi, nên chỉ lộ ra ở Production. Xem
[`2026-09-05-reflection-envelope.md`](../../../audit/2026-09-05-reflection-envelope.md).

### 5.5 Vì sao CHƯA có Logging / Performance / Caching behavior

| Behavior thường thấy | Vì sao chưa làm ở v1 |
| --- | --- |
| `LoggingBehavior` | ASP.NET Core đã log request/response ở tầng HTTP kèm `traceId`. Thêm một tầng log nữa nghĩa là mỗi request sinh hai bộ log gần giống nhau, và người đọc log phải học cách phân biệt chúng |
| `PerformanceBehavior` | Nó đo thời gian handler, nhưng thứ chậm gần như luôn là **query**, và EF Core đã log câu SQL kèm thời gian. Một cảnh báo "handler chạy quá 500ms" không nói được chậm ở đâu |
| `CachingBehavior` | **Chưa đo được vấn đề nào.** Cache đặt trước khi đo chỉ **che** lỗi: lần miss vẫn chậm y hệt, seq scan vẫn nguyên, và có thêm một tầng để debug khi số liệu hiển thị sai |
| `AuthorizationBehavior` | Phân quyền đã chặn ở tầng HTTP bằng `RequirePermissionAttribute`. Hai cơ chế song song cho cùng một việc là hai nguồn có thể lệch |

Mỗi behavior là một tầng mọi request phải đi qua và một chỗ nữa để nhìn khi có gì đó sai.

---

## 6. Validator (FluentValidation)

### 6.1 Quy ước

`internal sealed` vì không ai ngoài assembly gọi trực tiếp; MediatR/DI resolve qua interface. Một
validator viết ra mà không đăng ký sẽ không bao giờ chạy — code trông như đã có kiểm nhưng thực tế
không.

### 6.2 ⚠️ Nhiều validator cho MỘT request — mỗi cái phải có `ValidationContext` riêng

Rule kiểu `Custom` / `CustomAsync` báo lỗi bằng `context.AddFailure(...)` — tức **ghi thẳng vào
`ValidationContext`**, không phải trả về kết quả. Nếu behavior dựng **một** context rồi truyền cho tất
cả validator, thì `ValidationResult` của **mọi** validator đều chứa lỗi đó, và việc gộp kết quả sẽ đếm
nó lặp lại đúng bằng số validator.

**Triệu chứng:** người dùng vi phạm đúng một luật nhưng nhận thông điệp lặp lại trong `fieldErrors`.
Không lỗi biên dịch, không exception, không log gì.

**Vì sao bẫy này ẩn lâu:** nó chỉ kích hoạt khi một request có từ hai validator trở lên. Trong phần lớn
vòng đời một dự án, mỗi request chỉ có một validator, nên điều kiện kích hoạt chưa từng tồn tại — rồi
một ngày ai đó tách validator theo nhóm luật và nó nổ.

Bài học rộng hơn FluentValidation: **thứ gì nhận trạng thái chia sẻ để ghi kết quả vào thì không dùng
chung được giữa nhiều lượt chạy.** Cùng lý do khiến `Error` trong catalog là `record` bất biến và
`WithParams` trả bản mới (§2.1).

---

## 7. `ErrorDescriptor`, catalog lỗi, và khuôn mã

Tên ArchTest giữ từ `ErrorDescriptor` vì nó là định danh đã khai trong `RULES-BE.md`; tên kiểu C# là
`Error` vì ngắn hơn và đọc xuôi hơn tại chỗ dùng (`Result.Failure(UserErrors.NotFound)`).

### 7.1 Catalog tập trung — mã không được dựng từ chuỗi literal

Vì sao luật này đáng có một cổng riêng: một chuỗi gõ tại chỗ `throw`/`return` đi **thẳng** ra field
`code` của envelope mà không qua catalog nào. Hệ quả là hai hệ mã cùng tồn tại trong một field — một
hệ có kiểm, một hệ không. FE tra bảng dịch theo mã, nên mã ngoài catalog nghĩa là một câu không bao
giờ dịch được.

Mẫu catalog theo tài nguyên ([`be-cqrs-handler.md`](../../../quy-uoc/be-cqrs-handler.md) §7.1):

```csharp
// Core.Application/Users/UserErrors.cs
public static class UserErrors
{
    public static readonly Error NotFound = new(
        "CORE.USER.NOT_FOUND", "Không tìm thấy người dùng.", ErrorType.NotFound);

    public static readonly Error EmailDuplicated = new(
        "CORE.USER.EMAIL_DUPLICATED", "Email '{Email}' đã được dùng.", ErrorType.Conflict);

    public static readonly Error CannotLockSelf = new(
        "CORE.USER.CANNOT_LOCK_SELF", "Không tự khoá tài khoản của chính mình.", ErrorType.BusinessRule);

    public static readonly Error SelfSystemRoleRemovalForbidden = new(
        "CORE.USER.SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN",
        "Không tự gỡ vai trò hệ thống của chính mình.", ErrorType.BusinessRule);

    public static readonly Error RoleEscalationForbidden = new(
        "CORE.USER.ROLE_ESCALATION_FORBIDDEN",
        "Không thao tác trên vai trò cấp quyền mà người gọi không có.", ErrorType.Forbidden);

    public static readonly Error SystemRoleLockForbidden = new(
        "CORE.USER.SYSTEM_ROLE_LOCK_FORBIDDEN",
        "Không đủ quyền khoá tài khoản này.",
        ErrorType.Forbidden);

    public static readonly Error RoleNotFound = new(
        "CORE.USER.ROLE_NOT_FOUND", "Vai trò '{RoleId}' không tồn tại.", ErrorType.BusinessRule);
}
```

**Các mã cuối là chỗ hay chọn sai `ErrorType` nhất, và ranh giới nằm ở §3.2.**

| Mã | `ErrorType` | Vì sao |
| --- | --- | --- |
| `ROLE_ESCALATION_FORBIDDEN` · `SYSTEM_ROLE_LOCK_FORBIDDEN` | `Forbidden` | Người gọi **thiếu tư cách** trên bản ghi cụ thể: họ không có đủ tập quyền mà vai trò đích cấp, không mang vai trò hệ thống, hoặc đích là tài khoản mang cờ đặc quyền. Đó là hàng "không đủ quyền trên bản ghi cụ thể" |
| `CANNOT_LOCK_SELF` · `SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN` | `BusinessRule` | Người gọi **có đủ quyền**; thứ bị từ chối là *thao tác nhắm vào chính mình*. Trả 403 ở đây làm FE hiện "bạn không có quyền" cho một người đang có quyền |
| `ROLE_NOT_FOUND` | `BusinessRule` | Id **trong payload** trỏ tới bản ghi không tồn tại — §3.2 hàng 2. Không phải `Validation`: `Validation` theo định nghĩa tính được từ payload mà **không cần DB**, còn "vai trò này có tồn tại không" bắt buộc đọc DB. Kèm theo, mã `Validation` luôn đi cùng `fieldErrors`; mã này thì không |

Hợp đồng phía dây của các mã này: [`../contracts/users.md`](../../../contracts/users.md) §2.

### 7.2 Ranh giới của luật R8 — tiếng Việt chỉ ở tám hình dạng cú pháp

Luật R8 và catalog chỉ cùng đúng khi ranh giới được khai ra — nếu không, detector viết theo đúng tên
luật sẽ báo đỏ chính hình dạng mà catalog bắt buộc, và một mục cổng hay báo sai là một mục sẽ bị tắt đi.

**Vì sao câu luật nói về câu hiển thị cho người dùng, không về mọi chuỗi tiếng Việt.** Code Core mang tiếng
Việt ở template log, mô tả chỉ số, mô tả health check, thông điệp lỗi lập trình, đầu ra dòng lệnh: không chỗ
nào tới mắt người dùng ứng dụng. Cấm mọi chuỗi tiếng Việt ngoài catalog thì phải dịch hết những chuỗi đó sang
tiếng Anh cho một luật không bảo vệ thêm được gì. Hai phương án người dùng loại (2026-09-29): giữ câu cấm mọi
chuỗi rồi dịch code; chỉ sửa câu luật và hoãn bộ dò.

**Vì sao danh sách cho phép, đóng mặc định — không danh sách cấm.** Tập chỗ *được* mang tiếng Việt nhỏ và
liệt kê hết được; tập chỗ *có thể* đưa câu tới người dùng thì không. Danh sách cấm im lặng với mọi đường
chưa ai nghĩ tới — đúng loại đường mà một luật về rò câu chữ cần bắt. Đóng mặc định đổi chiều hỏng: hình
dạng mới làm cổng đỏ, và người viết phải trả lời *"cái này có tới người dùng không"* thành lời, trước khi
bảng có thêm một dòng.

**Vì sao theo hình dạng cú pháp, không theo tên tệp.** Miễn trừ theo tệp mục ruỗng từ tệp catalog thứ hai,
và một tệp được miễn thì mọi chuỗi trong nó được miễn — kể cả câu trả thẳng về client viết thêm ba tháng
sau. Hình dạng gắn quyền được mang tiếng Việt vào **đúng chỗ đứng** của chuỗi.

**Vì sao cần cây cú pháp.** Template log thường xuống dòng giữa lời gọi, nối `+` qua hai dòng; một chuỗi nội
suy có thể chứa `?:` bên trong. Grep theo dòng thấy một literal lẻ loi ở dòng tiếp theo mà không biết nó
thuộc lời gọi nào, và đọc cả chú thích — code Core chú thích bằng tiếng Việt khắp nơi. Kết cục là hoặc báo sai
hàng loạt, hoặc phải miễn trừ, và cả hai đều giết cổng.

**Ví dụ đỏ hay gặp** — không hình dạng nào nhận: `WithMessage("…")` của validator, vì validator gắn **mã** vào
`fieldErrors`; chuỗi trả thẳng từ controller, handler, middleware; câu gán vào biến, hằng hay trường để dùng
về sau, kể cả hằng đem làm đối số attribute và câu gom vào danh sách rồi mới trao cho `ValidateOptionsResult.Fail`;
thân của hàm phụ dựng ngoại lệ, vì nó nằm ngoài `throw`; nhãn truyền vào hàm phụ ghi log hay ghi dòng lệnh
(`GuardedAsync("…")`); giá trị — không phải template — của lời gọi log (`stack ?? "(không có stack)"`).

**Vì sao bốn nhóm ngoài bảng chia ba cách** (người dùng chốt 2026-09-30). Thông điệp kiểm cấu hình lúc khởi
động có vài hình dạng cố định của framework, nên thêm **hình dạng 8** hẹp đúng các hình dạng đó; nhận cả danh
sách lỗi hay hằng thì phải nhận mọi chuỗi gán vào biến — lại đúng lỗ mà danh sách cho phép tồn tại để bịt.
Ngoại lệ dựng qua hàm phụ hay hàm dựng lớp cơ sở thì **mở rộng hình dạng 3**, vì điều kiện an toàn của nó —
thông điệp ngoại lệ không lên dây — không phụ thuộc chuỗi đi qua bao nhiêu lời gọi trước khi thành ngoại lệ.
Nhãn truyền vào hàm phụ và giá trị log **viết lại code**: hình dạng cho chúng là *"đối số chuỗi của một hàm
tự viết"* — không nhận ra bằng cú pháp nếu không miễn trừ theo tên hàm. Phương án bị loại: chỉ thêm hình
dạng 8 và viết lại ba nhóm kia; thêm hình dạng cho cả bốn nhóm.

**Vì sao mỗi hình dạng an toàn:**

| # | Lý do |
| --- | --- |
| 1 | Câu ở catalog là thứ duy nhất trong BE gắn **một–một** với một mã; FE thay nó bằng bản dịch theo mã. Giới hạn trong lớp `static …Errors` để một `new Error(…)` rải trong handler — thứ R2 cấm — không mượn được quyền này |
| 2 | Log là kênh của người vận hành |
| 3 | An toàn **chỉ vì** không đường nào đưa thông điệp ngoại lệ ra dây. Điều kiện đó cần một test chứ không cần niềm tin: A15 khẳng định thân 500 không lộ kiểu và ngăn xếp, và phải khẳng định thêm không lộ thông điệp. Nhận mọi chuỗi bên trong biểu thức `throw` vì mọi thứ ở đó dựng ra ngoại lệ sắp ném; nhận đối số `base(…)` của lớp ngoại lệ vì nó thành thông điệp của chính ngoại lệ. Thân hàm phụ dựng ngoại lệ vẫn đỏ — hàm đó gọi được ở chỗ khác ngoài `throw`, như một chuỗi thường —, nên câu cố định đặt ở lời gọi trong `throw` hay ở hàm dựng của lớp ngoại lệ riêng. **Cái giá của mở rộng:** hàm phụ gọi trong `throw` mà dùng chuỗi vào việc khác ngoài dựng ngoại lệ — ghi ra phản hồi chẳng hạn — thì lọt bộ dò; review canh |
| 4 | Attribute là siêu dữ liệu biên dịch; không đường nào của Core trả đối số attribute ra dây |
| 5, 6 | Đích là công cụ thu chỉ số và log; `/health/*` chỉ ghi trạng thái |
| 7 | Người đọc là người vận hành chạy lệnh `core …` trên máy chủ. Giới hạn trong thư mục lệnh để một `Console.WriteLine` lạc vào controller vẫn đỏ |
| 8 | Thông điệp đi vào ngoại lệ kiểm cấu hình, làm app từ chối khởi động — người đọc là người vận hành. Cùng kiểu dùng cho kiểm model của MVC thì thông điệp dừng ở `ModelState`, và bộ dựng phản hồi lỗi binding chỉ ghi mã. Chỉ nhận chuỗi viết thẳng ở chỗ trao cho framework: chuỗi đi qua biến hay danh sách trước đó không phân biệt được với câu dùng vào việc khác |

**Hai điểm mù, và vì sao chấp nhận:** chuỗi tiếng Việt viết không dấu, và câu hiển thị viết bằng tiếng Anh.
Bắt được chúng đòi hiểu nghĩa câu, không còn là phép thử cú pháp. Review là lớp bắt; dấu hiệu cần quay lại
là thấy chuỗi không dấu xuất hiện ở chỗ bảng không nhận — lúc đó người viết đang né cổng, không phải tuân luật.

**Cái giá.** Mỗi kiểu thông điệp nội bộ mới — một hàm phụ ghi log nhận nhãn, một bộ kiểm cấu hình gom lỗi vào
danh sách — làm cổng đỏ cho tới khi code viết lại vào một hình dạng có sẵn hoặc người dùng thêm hình dạng.
Lượt viết lại 2026-10-01 đã trả cái giá đó một lần — bộ kiểm cấu hình nay báo lỗi đầu tiên rồi dừng, không gom cả danh sách. **Dấu hiệu quyết định này bắt đầu sai:** bảng phải
thêm hình dạng nhiều lần trong một quý, hoặc người viết bắt đầu chuyển chuỗi nội bộ sang không dấu cho cổng xanh.

### 7.3 Khuôn mã lỗi

- *Duy nhất toàn hệ* — hai mã trùng nhau ở hai module là hai câu khác nhau tranh cùng một khoá dịch.
- *Đổi mã là breaking change* — mã nằm trong bảng dịch của FE và có thể nằm trong tài liệu người dùng.
- *Mã mô tả nguyên nhân* — `CORE.USER.SHOW_RED_TOAST` khoá chặt BE vào một quyết định giao diện.

### 7.4 Mã cho điều kiện phía client

**Vì sao danh mục nằm ở tệp luật BE mà hằng số thì không nằm trong mã BE:** luật R3 đòi mã **duy nhất
trong toàn hệ**. Cách rẻ nhất để một mã phía client không bao giờ trùng mã BE là **tách không gian tên**:
tài nguyên `CLIENT` chỉ dùng cho bảng §7.4, và không catalog BE nào khai mã mang nó. Đặt hằng số cho
những mã đó trong catalog BE sẽ để BE mang những mã nó không bao giờ phát; phép dò *"mã trong catalog
có lên dây không"* của luật **F16** ([`RULES-FE.md`](../../../RULES-FE.md)) lại phải miễn trừ chúng.

Bảng nằm cạnh khuôn mã ở §7.3 vì đó là chỗ người thêm mã mới — ở bất kỳ phía nào — đã phải mở ra.
FE tra bảng dịch theo mã; mã phía client chưa có dòng ở bảng thì chưa được dùng.

### 7.5 Ranh giới với mã của `fieldErrors`

Giữ cùng khuôn cho cả hai là quyết định của repo này: dự án tiền nhiệm để mã field là tên validator của
FluentValidation — `"NotEmptyValidator"` — nên FE phải học **hai** hệ mã. Cái được khi cùng khuôn: FE có
**một** bảng dịch, tra bằng **một** hàm.

**Vì sao rỗng / độ dài / khuôn dạng dùng chung `CORE.VALIDATION.*` thay vì mã theo tài nguyên.** Câu hiển
thị của "bắt buộc" không đổi theo ô: `CORE.USER.USERNAME_REQUIRED`, `CORE.ROLE.NAME_REQUIRED`,
`SKILL.COURSE.TITLE_REQUIRED` là ba mã cho **một** câu, và FE phải dịch cả ba — mỗi module mới thêm một
bộ bản dịch chỉ để nói lại điều cũ. Ô nào bị lỗi đã nằm ở khoá của `fieldErrors`, nên mã không cần mang tên
ô. Mã theo tài nguyên chỉ đáng khai khi câu **thật sự khác** (trùng email, mật khẩu không đạt chính sách) —
đó là lý do catalog mẫu ở §7.1 không có mã `*_REQUIRED` theo tài nguyên.

---

## 8. i18n — BE trả MÃ + THAM SỐ ĐẶT TÊN, không trả câu đã ghép

### 8.1 Hợp đồng thông điệp lỗi

—

### 8.2 Vì sao tham số phải theo TÊN, không theo thứ tự (`{0}`)

Đây là điểm dễ bị coi là chi tiết nhỏ, nhưng nó quyết định bảng dịch có dùng được hay không.

**Trật tự từ mỗi ngôn ngữ một khác.** Câu *"Email {0} đã được {1} dùng"* dịch sang một ngôn ngữ khác có
thể phải đảo hai chỗ giữ, hoặc lặp lại một chỗ giữ hai lần, hoặc bỏ hẳn một chỗ giữ vì ngôn ngữ đó
không cần nó. Với `{0}`/`{1}`, bảng dịch buộc phải giữ nguyên số lượng và **suy đoán** ý nghĩa của
từng vị trí.

Với tham số đặt tên, bảng dịch tự do sắp xếp:

| Ngôn ngữ | Bản dịch cho `CORE.USER.EMAIL_DUPLICATED` |
| --- | --- |
| vi | `Email {Email} đã được dùng.` |
| en | `The email address {Email} is already taken.` |
| (một ngôn ngữ đảo trật tự) | `{Email} — địa chỉ này đã có người dùng.` |

Ba bản dịch dùng cùng một bộ tham số, không bản nào phải biết vị trí của chỗ giữ trong bản gốc.

**Hệ quả quan trọng hơn:** nếu BE gửi câu **đã ghép**, client không tách lại được tham số ra khỏi chuỗi
đó. Nó buộc phải hiển thị nguyên câu tiếng Việt — và toàn bộ công i18n dừng lại đúng trước cửa. Đây là
lý do `messageParams` phải nằm **cạnh mã mà nó tham số hoá**.

Vì sao không gom mọi tham số về gốc envelope: một lần submit hỏng nhiều field thì mỗi field có bộ tham
số riêng, và hai field cùng vi phạm một luật sẽ **ghi đè khoá của nhau** — câu của field này hiện con
số của field kia. Hỏng im lặng.

### 8.3 ⚠️ KHÔNG dùng `FluentValidation.Internal.MessageFormatter`

Nó ráp được câu theo chỗ giữ đặt tên, và nó rất cám dỗ vì đã có sẵn trong package. Nhưng kiểu trong
namespace `Internal` không nằm trong hợp đồng public của thư viện: nó đổi chữ ký, đổi hành vi, hoặc biến
mất ở **bất kỳ bản minor nào**, và điều đó hoàn toàn hợp lệ về mặt semver. Dựng chỗ ráp câu của toàn hệ
trên một kiểu như vậy nghĩa là một lần `dotnet restore` bình thường có thể làm vỡ mọi thông điệp lỗi.

Ba quyết định trong mười dòng của `MessageTemplateRenderer`:

- **Chỗ giữ thiếu tham số thì giữ nguyên literal**, không thay bằng chuỗi rỗng. Một câu hiện `{Email}`
  giữa giao diện là lỗi **nhìn thấy được**; một câu thiếu mất chỗ đó thì đọc vẫn xuôi và không ai phát
  hiện.
- **`[GeneratedRegex]`** — regex biên dịch lúc build, không dựng lại mỗi lần gọi.
- **Đặt ở `Core.Domain`** — nó chỉ dùng BCL, và cả Domain lẫn Application đều cần nó.

---

## 9. Grid / danh sách — `PagedList<T>`

### 9.1 Hình dạng

Không có `TotalPages` vì gửi kèm dữ liệu suy ra được nghĩa là tạo hai nguồn có thể lệch nhau; component
phân trang của PrimeNG chỉ cần tổng số bản ghi và tự tính số trang. `Total` không nói rõ tổng của cái
gì — dòng? trang? byte? — nên tên là `TotalCount`.

### 9.2 Envelope nhất quán

"Envelope drift" — mỗi loại endpoint một hình dạng — là lỗi đã xảy ra thật ở hệ tham chiếu, và nó
không lộ ra ở BE: nó lộ ra ở FE dưới dạng `undefined` chỗ này chỗ kia.

### 9.3 Quy ước paging / sort / filter

Model binder khớp tên không phân biệt hoa thường, nên một tên lệch (`Keyword`, `PageNumber`) không gây
lỗi, không cảnh báo — chỉ là bộ lọc không có tác dụng.

Ghép thẳng chuỗi client gửi vào `OrderBy(...)` dạng chuỗi động là mở đường cho lỗi lúc chạy ở tốt
nhất, và cho injection ở tệ nhất. `ORDER BY UpdatedAt DESC` trên dữ liệu có giá trị trùng cho thứ tự
**không xác định** giữa các trang: cùng một bản ghi xuất hiện ở trang 1 và trang 2, một bản ghi khác
không xuất hiện ở đâu cả — vì thế luôn thêm `Id` làm tiêu chí cuối.

### 9.4 `GET` hay `POST` cho danh sách

Cái giá của `POST`: mất khả năng bookmark, mất cache của tầng HTTP, và mất tính idempotent mà công cụ
giám sát trông vào. Đừng trả cái giá đó cho một màn hình có ba ô lọc.

---

## 10. Command chạy lâu → job nền

### 10.1 Ngưỡng tách

Command CRUD thường không tách sang job nền vì nó chỉ thêm phức tạp và thêm một trạng thái trung gian mà
người dùng phải chờ.

### 10.2 Hợp đồng

—

### 10.3 Khuôn `jobId + polling`

Khối handler, rút gọn từ mã thật — bỏ các tham số và phép kiểm đứng trước lần ghi đầu tiên.
✅ CÓ THẬT (đối chiếu 2026-09-25): `src/BE/Core/CoreAndSkill.Core.Application/Import/StartImportCommandHandler.cs`
(chuỗi `var job = Job.Create(definition.Type, callerId, tempKey);`).

```csharp
// Handler làm TRỌN use case: lưu dữ liệu tạm + tạo bản ghi job + GHI Ý ĐỊNH enqueue vào outbox
internal sealed class StartImportCommandHandler(
    IFileStorage storage,
    IJobRepository jobs,
    ICurrentUser currentUser /* , … */) : IRequestHandler<StartImportCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(StartImportCommand command, CancellationToken ct)
    {
        // … mọi phép kiểm (định dạng, cột, trần số dòng) chạy TRƯỚC lần ghi đầu tiên.

        // Ghi file tạm nằm NGOÀI transaction — xem ràng buộc 4. Lỗi hạ tầng là exception, không Result.
        content.Position = 0;
        var tempKey = await storage.SaveTempAsync(content, ct);

        var job = Job.Create(definition.Type, callerId, tempKey);
        if (job.IsFailure)
            return Result.Failure<Guid>(job.Error!);

        // Job.Create ghi nhận domain event JobQueuedEvent lên chính entity.
        // KHÔNG gọi scheduler ở đây: bộ chặn ở tầng dữ liệu chuyển event đó thành một dòng
        // outbox trong CÙNG transaction, và tiến trình phát nền gọi
        // IBackgroundJobScheduler sau khi commit.
        await jobs.AddAsync(job.Value, ct);
        return job.Value.Id;
    }
}
```

Lý do của từng ràng buộc ở [`be-cqrs-handler.md`](../../../quy-uoc/be-cqrs-handler.md) §10.3:

1. *Không trả HTTP 202* — "đã bắt đầu chứ chưa xong" thể hiện ở tầng **dữ liệu**, không ở HTTP status.
   Cho controller tự đặt status là tạo nguồn sự thật thứ hai cho ánh xạ `Result` → HTTP.
2. *Enqueue đi qua seam* — gọi thẳng nghĩa là `Core.Application` phụ thuộc hạ tầng cụ thể. Đặt lời
   enqueue lên controller **không** phải cách sửa: nó chỉ đổi một vi phạm lấy một vi phạm khác
   (controller chứa logic) và cắt đôi một use case.
3. *Handler ghi outbox, không gọi scheduler* — `TransactionBehavior` bọc trọn handler, nên một lời gọi ra
   ngoài đặt bên trong handler là một lời gọi **bên trong transaction** — đúng thứ mà
   [`0006-pipeline-behavior.md`](../../../adr/0006-pipeline-behavior.md) §Tiêu cực cấm: *"không gọi dịch
   vụ ngoài bên trong command; việc đó đẩy sang outbox"*. Hai hỏng thật nếu gọi thẳng, và chúng ngược
   nhau nên không có cách viết nào tránh được cả hai mà không có outbox:

   | Ca | Chuyện gì xảy ra |
   | --- | --- |
   | Enqueue xong, transaction **rollback** | Job chạy đi tìm một bản ghi `Job` **không tồn tại** — worker ném ở một chỗ rất xa nguyên nhân |
   | Enqueue **chậm** (hàng job ở xa, đang nghẽn) | Transaction đứng mở suốt lượt gọi đó, giữ khoá trên bảng job. Đây là công thức của khoá kéo dài |

   Dòng outbox ghi trong **cùng** transaction với bản ghi job, nên hai thứ đó hoặc cùng có hoặc cùng
   không. Handler thậm chí không tự ghi dòng đó: entity ghi nhận một domain event, và bộ chặn ở tầng dữ
   liệu chuyển nó thành bản ghi outbox ngay trước khi lưu — nhờ vậy handler **không thể quên**. Hệ quả
   phải chấp nhận: job **không** chạy ngay lập tức, mà sau một nhịp phát outbox. Với một việc vốn đã là
   "chạy nền và poll kết quả" thì độ trễ đó không đổi gì về trải nghiệm.
4. *File tạm ghi trước, ngoài outbox* — nó phải xong **trước** khi có bản ghi job, nên không thể đi qua
   outbox. Cái giá là một file tạm mồ côi khi transaction rollback — rẻ, và dọn được bằng một job quét
   file tạm quá hạn. Đổi lại nếu ghi file sau commit thì có một khoảng thời gian bản ghi job trỏ vào một
   file **chưa tồn tại**, và worker có thể chạy đúng vào khoảng đó.
5. *Không WebSocket* — poll vài giây một lần là đủ ở quy mô này, và nó không cần thêm hạ tầng nào.

---

## 11. Trên dây là DTO, không bao giờ là entity

### 11.1 Vì sao không trả entity ra API

| Vấn đề | Hậu quả cụ thể |
| --- | --- |
| **Rò trường không định gửi** | Kiểu người dùng của Identity mang mã băm mật khẩu, dấu bảo mật, cờ hệ thống. Thêm một cột nhạy cảm vào bảng là **tự động** lộ nó ra API — không ai phải sửa dòng nào, nên không ai review được |
| **Hợp đồng API khoá vào schema DB** | Đổi tên một cột là vỡ FE. Đổi tên cột là việc bình thường; đổi hợp đồng API thì phải tăng phiên bản ([`be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §8.1) |
| **Quan hệ vòng** | Người dùng → vai trò → người dùng. Bộ tuần tự hoá đi vào vòng lặp, hoặc phải cấu hình cắt vòng — và khi đó không ai đoán được API trả về gì |
| **Lấy thừa dữ liệu** | Entity nạp hàng chục cột cho một màn hiển thị ba cột |

### 11.2 Cách làm: truy vấn thẳng vào DTO

—

### 11.3 Không thêm thư viện ánh xạ tự động

Hai lý do đo được:

- Nó **che mất** chỗ đang nạp thừa cột: cấu hình ánh xạ nằm ở file khác, còn câu truy vấn trông vẫn gọn.
- Nó hỏng **lúc chạy**, không hỏng lúc biên dịch: đổi tên một trường thì bên nhận nhận `null` và mọi
  thứ vẫn chạy. Viết tay thì cùng thay đổi đó là một lỗi biên dịch, phát hiện ngay trên máy người sửa.

Cái giá phải chấp nhận: mỗi DTO có một đoạn phép chiếu viết tay, và nó lặp lại về mặt thị giác. Đổi lại
là mọi trường đi ra ngoài đều do một người viết ra và một người review thấy.
