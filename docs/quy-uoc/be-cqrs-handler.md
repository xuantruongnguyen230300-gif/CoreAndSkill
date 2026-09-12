---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quy ước Backend — CQRS, Handler, `Result<T>`, mã lỗi

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi code mẫu là khuôn cho `src/BE` sẽ xây ở giai đoạn 2.
>
> Đây là file chủ của: `ICommand<T>`/`IQuery<T>`, `Result<T>`, `Error`, `ErrorType`,
> `ErrorDescriptor`, pipeline behavior, validator, `PagedList<T>`, tham số thông điệp đặt
> tên. File khác chỉ được **trỏ** tới đây, không chép lại.

---

## 1. Command / Query qua MediatR

Mỗi use case = một Command hoặc Query + một Handler. Khai qua `ICommand<T>` / `IQuery<T>`,
**không** khai `IRequest<Result<T>>` trực tiếp — envelope bị ép ở tầng **kiểu**, nên
handler không thể "quên" trả `Result`.

```csharp
// Core.Application/Common/Cqrs/ICommand.cs
public interface ICommandBase;                                   // marker cho TransactionBehavior
public interface ICommand : ICommandBase, IRequest<Result>;
public interface ICommand<TResult> : ICommandBase, IRequest<Result<TResult>>;

// Core.Application/Common/Cqrs/IQuery.cs
public interface IQuery<TResult> : IRequest<Result<TResult>>;
```

```csharp
// Core.Application/Users/CreateUserCommand.cs
public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string FullName,
    IReadOnlyList<string> RoleNames) : ICommand<Guid>;

// Core.Application/Users/GetUserByIdQuery.cs
public sealed record GetUserByIdQuery(Guid Id) : IQuery<UserDto>;

// Core.Application/Users/GetUsersListQuery.cs
public sealed record GetUsersListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Keyword = null,
    string? SortBy = null,
    bool SortDescending = false) : IQuery<PagedList<UserDto>>;
```

Quy ước đặt tên và hình dạng:

| Quy ước | Lý do |
| --- | --- |
| Command/Query là **`sealed record`** bất biến | Không ai sửa được request giữa các behavior; so sánh theo giá trị dùng được cho cache/test |
| Query danh sách: `Get{Entity}sListQuery` | Một khuôn duy nhất để `grep` ra mọi endpoint danh sách |
| `ICommandBase` là marker **rỗng** | `TransactionBehavior` ràng buộc theo nó, nên query không bao giờ đi qua transaction — xem §5 |
| Một file một kiểu, đặt trong thư mục feature | Vertical slice — [`be-architecture.md`](be-architecture.md) §6 |

---

## 2. `Result<T>` — hợp đồng đầy đủ

### 2.1 `ErrorType` và `Error`

```csharp
// Core.Domain/Common/ErrorType.cs
public enum ErrorType
{
    Validation,     // input sai — tính được từ payload, không cần DB
    NotFound,       // resource chính của route không tồn tại
    Conflict,       // xung đột trạng thái: trùng unique, concurrency
    Forbidden,      // đã đăng nhập nhưng thiếu quyền
    BusinessRule,   // vi phạm quy tắc nghiệp vụ, cần đọc dữ liệu mới biết
    Unauthorized,   // chưa đăng nhập / phiên hết hạn
}
```

```csharp
// Core.Domain/Common/Error.cs
public sealed record Error(string Code, string MessageTemplate, ErrorType Type)
{
    public IReadOnlyDictionary<string, string> Params { get; init; }
        = ReadOnlyDictionary<string, string>.Empty;

    public IReadOnlyDictionary<string, IReadOnlyList<FieldError>> FieldErrors { get; init; }
        = ReadOnlyDictionary<string, IReadOnlyList<FieldError>>.Empty;

    public Error WithParams(params ReadOnlySpan<(string Name, object? Value)> args)
    {
        var map = new Dictionary<string, string>(args.Length, StringComparer.Ordinal);
        foreach (var (name, value) in args)
            map[name] = value?.ToString() ?? string.Empty;

        return this with { Params = map.AsReadOnly() };
    }

    public Error WithFieldErrors(IReadOnlyDictionary<string, IReadOnlyList<FieldError>> fieldErrors)
        => this with { FieldErrors = fieldErrors };
}

// Core.Domain/Common/FieldError.cs
public sealed record FieldError(string Code, IReadOnlyDictionary<string, string> Params);
```

Ba điều cố ý trong thiết kế trên:

- **`Code` là khoá, `MessageTemplate` là dự phòng.** Client dựng câu từ `Code`; câu tiếng
  Việt trong `MessageTemplate` chỉ dùng cho log và cho trường hợp client chưa có bản dịch.
- **`Params` là từ điển theo TÊN**, không phải mảng theo thứ tự. Lý do đầy đủ ở §7.
- **`Error` là `record` bất biến**, `WithParams` trả bản mới. Nhờ vậy các `Error` trong
  catalog là `static readonly` dùng chung được cho mọi request mà không sợ một request ghi
  đè tham số của request khác — cùng bài học với `ValidationContext` ở §6.2.

### 2.2 `Result` và `Result<T>`

```csharp
// Core.Domain/Common/Result.cs
public class Result : IResult, IResultFactory<Result>
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess == (error is not null))
            throw new InvalidOperationException("Result thành công không được mang Error, và ngược lại.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, null);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    static Result IResultFactory<Result>.FromError(Error error) => Failure(error);
}

// Core.Domain/Common/Result{T}.cs
public sealed class Result<TValue> : Result, IResultFactory<Result<TValue>>
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error? error) : base(isSuccess, error)
        => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Không đọc được Value của một Result thất bại.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    static Result<TValue> IResultFactory<Result<TValue>>.FromError(Error error) => Failure<TValue>(error);
}
```

```csharp
// Core.Domain/Common/IResult.cs — hai interface nhỏ, dùng cho pipeline behavior
public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    Error? Error { get; }
}

public interface IResultFactory<out TSelf> where TSelf : IResultFactory<TSelf>
{
    static abstract TSelf FromError(Error error);
}
```

> **`IResultFactory<TSelf>` với `static abstract` là chi tiết quan trọng, không phải trang
> trí.** Nó cho phép pipeline behavior dựng một `Result` thất bại của **đúng kiểu trả về
> của request** mà không cần reflection. Xem §5.4 để biết vì sao đây là điểm mấu chốt.

`Value` ném khi `IsFailure` — có chủ đích. Đọc `Value` mà chưa kiểm `IsSuccess` là **bug
lập trình**, không phải lỗi nghiệp vụ, nên nó thuộc nhóm được phép ném (xem
[`be-entity-domain.md`](be-entity-domain.md) §3.4).

### 2.3 Compose — map/bind thay vì lồng `if`

```csharp
// Core.Domain/Common/ResultExtensions.cs
public static class ResultExtensions
{
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> selector)
        => result.IsSuccess ? Result.Success(selector(result.Value)) : Result.Failure<TOut>(result.Error!);

    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, TOut> selector)
        => (await resultTask).Map(selector);

    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder)
        => result.IsSuccess ? binder(result.Value) : Result.Failure<TOut>(result.Error!);

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result, Func<TIn, Task<Result<TOut>>> binder)
        => result.IsSuccess ? await binder(result.Value) : Result.Failure<TOut>(result.Error!);

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> binder)
        => await (await resultTask).BindAsync(binder);

    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error)
        => result.IsFailure || predicate(result.Value) ? result : Result.Failure<T>(error);

    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess) action(result.Value);
        return result;
    }
}
```

So sánh hai cách viết cùng một use case:

```csharp
// ❌ Lồng if — mỗi bước thêm một mức thụt lề
var emailResult = EmailAddress.Create(cmd.Email);
if (emailResult.IsFailure) return Result.Failure<Guid>(emailResult.Error!);

var userResult = User.Create(cmd.UserName, emailResult.Value);
if (userResult.IsFailure) return Result.Failure<Guid>(userResult.Error!);

var saved = await repo.AddAsync(userResult.Value, ct);
if (saved.IsFailure) return Result.Failure<Guid>(saved.Error!);

return Result.Success(saved.Value.Id);
```

```csharp
// ✅ Bind — nhánh lỗi tự truyền, chỉ còn nhánh thành công phải đọc
return await EmailAddress.Create(cmd.Email)
    .Bind(email => User.Create(cmd.UserName, email))
    .BindAsync(user => repo.AddAsync(user, ct))
    .MapAsync(user => user.Id);
```

**Ngưỡng dùng, để chuỗi không trở thành thứ khó đọc hơn cái nó thay thế:** dùng `Bind` khi
chuỗi có **từ ba bước trở lên** và mỗi bước chỉ nhận kết quả của bước trước. Khi một bước
cần thêm dữ liệu từ hai bước trước đó, viết `if` phẳng — chuỗi lúc đó phải bọc tuple và
tuple làm mất luôn cái lợi.

---

## 3. Handler — bốn bước, KHÔNG tự quản transaction

```csharp
// Core.Application/Users/CreateUserCommandHandler.cs
internal sealed class CreateUserCommandHandler(
    IUserRepository users,
    IUserAdminService userAdmin)
    : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        // 1. Luật nghiệp vụ cần dữ liệu — trả Result lỗi, KHÔNG ném
        if (await users.EmailExistsAsync(cmd.Email, ct))
            return Result.Failure<Guid>(UserErrors.EmailDuplicated.WithParams(("Email", cmd.Email)));

        // 2. Dựng qua factory của Domain — không new + gán property
        var emailResult = EmailAddress.Create(cmd.Email);
        if (emailResult.IsFailure)
            return Result.Failure<Guid>(emailResult.Error!);

        // 3. Gọi ra ngoài qua interface; KHÔNG gọi SaveChangesAsync ở đây
        var created = await userAdmin.CreateAsync(
            new CreateUserInput(cmd.UserName, emailResult.Value, cmd.FullName, cmd.RoleNames), ct);

        // 4. Trả kết quả
        return created;
    }
}
```

### 3.1 Handler KHÔNG làm gì

| Không làm | Ai làm thay |
| --- | --- |
| `try/catch` cho lỗi hạ tầng | `ExceptionHandlingMiddleware` ở `Core.Web` |
| Validate định dạng input (rỗng, độ dài, regex) | `ValidationBehavior` + FluentValidation, chạy **trước** handler |
| `BeginTransaction` / `Commit` / `Rollback` | `TransactionBehavior` — xem §5 |
| `SaveChangesAsync` | `TransactionBehavior`, và chỉ khi kết quả thành công |
| Đặt HTTP status | `ResultToHttpMapper` ở `Core.Web` — [`be-api-controller.md`](be-api-controller.md) |
| Ghép câu tiếng Việt cho người dùng | Client, từ `Code` + `Params` — §7 |

### 3.2 Handler PHẢI tự làm

- **Validate cần dữ liệu**: trùng unique, tham chiếu có tồn tại không, trạng thái hiện tại
  có cho phép thao tác không. Validator giữ **thuần**, không truy cập DB — để nó test được
  mà không cần hạ tầng, và để không có hai chỗ cùng đọc DB trong một lượt xử lý.
- **Chọn đúng `ErrorType`**. Đây là quyết định của handler, không của controller:

| Tình huống | `ErrorType` |
| --- | --- |
| `{id}` trên route không tồn tại (kể cả đã soft delete) | `NotFound` |
| Một Id **trong payload** trỏ tới bản ghi không tồn tại | `BusinessRule` |
| Trùng giá trị unique, hoặc `DbUpdateConcurrencyException` | `Conflict` |
| Đã đăng nhập nhưng không đủ quyền trên **bản ghi cụ thể** này | `Forbidden` |
| Trạng thái không cho phép thao tác ("đã duyệt thì không sửa") | `BusinessRule` |

Phân biệt hàng 1 và hàng 2 là chỗ hay sai nhất: *"resource chính của route"* trả 404;
*"một tham chiếu bên trong dữ liệu gửi lên"* trả 422. Trộn hai ca này khiến FE không phân
biệt được "gọi sai URL" với "dữ liệu gửi lên tham chiếu sai".

Ca soft delete cố ý **không** tách mã riêng: trả một mã khác cho "đã xoá" so với "không
tồn tại" là tiết lộ sự tồn tại của dữ liệu đã xoá cho người không có quyền thấy nó.

### 3.3 Vì sao handler không tự quản transaction

Ba lý do, lý do thứ ba là lý do thật:

1. **Lặp lại.** Mỗi command sẽ phải mở/commit/rollback giống hệt nhau.
2. **Dễ quên.** Một command quên `SaveChangesAsync` thì im lặng không ghi gì. Không lỗi,
   không cảnh báo, chỉ là dữ liệu không xuất hiện.
3. **Chiến lược thử lại của EF không bọc được transaction do code tự mở.** Khi
   `EnableRetryOnFailure` bật (và nó phải bật — xem [`be-performance.md`](be-performance.md)),
   một `BeginTransactionAsync` gọi tay sẽ ném `InvalidOperationException` lúc **chạy**,
   không lúc biên dịch, và chỉ trên đúng đường ghi đó. Gom transaction về một chỗ nghĩa là
   chỉ có **một** chỗ phải bọc bằng execution strategy cho đúng.

---

## 4. `IUnitOfWork` — hợp đồng transaction

```csharp
// Core.Application/Common/Interfaces/IUnitOfWork.cs
public readonly record struct TransactionOutcome<T>(T Value, bool ShouldCommit);

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<TransactionOutcome<T>>> operation,
        CancellationToken ct = default);
}
```

```csharp
// Core.Infrastructure/Persistence/UnitOfWork.cs
internal sealed class UnitOfWork(CoreDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<TransactionOutcome<T>>> operation,
        CancellationToken ct = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async innerCt =>
        {
            // Mỗi lần thử lại phải bắt đầu từ dữ liệu SẠCH: giữ lại instance đã sửa dở của
            // lần trước sẽ hoặc ghi đè bằng dữ liệu cũ, hoặc vấp lỗi concurrency khó hiểu.
            db.ChangeTracker.Clear();

            await using var tx = await db.Database.BeginTransactionAsync(innerCt);

            var outcome = await operation(innerCt);

            if (outcome.ShouldCommit)
            {
                await db.SaveChangesAsync(innerCt);
                await tx.CommitAsync(innerCt);
            }
            else
            {
                await tx.RollbackAsync(innerCt);
            }

            return outcome.Value;
        }, ct);
    }
}
```

Hai bẫy đã đóng sẵn trong đoạn trên, cả hai đều nổ **lúc chạy** chứ không lúc build:

- Transaction tự mở mà không bọc bằng `CreateExecutionStrategy()` → `InvalidOperationException`.
- Không `ChangeTracker.Clear()` ở đầu mỗi lượt thử lại → lượt thứ hai dùng lại instance đã
  sửa dở của lượt đầu.

---

## 5. Pipeline behavior — đúng HAI cái ở v1

### 5.1 Danh sách và thứ tự đăng ký

```csharp
// Core.Application/DependencyInjection.cs
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<ICommandBase>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));    // 1 — chạy TRƯỚC
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));   // 2 — chạy SAU
});

services.AddValidatorsFromAssemblyContaining<ICommandBase>(ServiceLifetime.Transient);
```

**Thứ tự này không đổi được, và lý do là chi phí:** behavior đăng ký trước nằm **ngoài
cùng**. `ValidationBehavior` ngoài `TransactionBehavior` nghĩa là input sai bị chặn lại
**trước khi** một transaction được mở. Đảo lại thì mọi request hỏng đều mở rồi rollback
một transaction — tốn một round-trip tới DB cho một lỗi đáng lẽ tính được từ payload.

Luật `EveryPipelineBehavior_IsRegistered_ExactlyOnce` ([`../RULES.md`](../RULES.md) §3)
canh: đăng ký hai lần khiến mỗi request chạy behavior hai lượt. Đây là lý do **module
không được tự đăng ký behavior** — behavior là open-generic, tự áp cho mọi request bất kể
handler nằm ở assembly nào.

### 5.2 `ValidationBehavior` — trả `Result` lỗi, KHÔNG ném

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
        => new(failure.ErrorCode ?? CommonErrors.UnspecifiedFieldCode,
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

Vế "số đường dựng envelope lỗi" là vế quyết định: hai đường thì chúng sẽ lệch nhau, và ở
dự án tiền nhiệm chúng đã lệch — nhánh validation từng dựng envelope **bằng tay**, không
mang `businessCode` nào, trong khi nhánh còn lại đi qua factory chuẩn.

> ⚠️ **`MessageParamPolicy.Filter` không phải chi tiết bỏ qua được.**
> `ValidationFailure.FormattedMessagePlaceholderValues` **luôn** chứa khoá `PropertyValue`
> = **giá trị người dùng vừa gõ**. Chuyển tiếp cả từ điển nghĩa là mật khẩu mới vừa nhập
> đi ra HTTP response ở đúng lần nhập hỏng. Chỉ khoá trong allowlist mới ra ngoài, và
> `PropertyValue` không bao giờ nằm trong allowlist đó.

### 5.3 `TransactionBehavior` — chỉ bọc command

```csharp
// Core.Application/Common/Behaviors/TransactionBehavior.cs
internal sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork uow)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommandBase
    where TResponse : IResult
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
        => uow.ExecuteInTransactionAsync(async innerCt =>
        {
            var response = await next(innerCt);
            return new TransactionOutcome<TResponse>(response, response.IsSuccess);
        }, ct);
}
```

**Query không đi qua behavior này**, và cơ chế loại trừ là **ràng buộc generic**
`where TRequest : ICommandBase`, không phải một `if` bên trong. `IQuery<T>` không kế thừa
`ICommandBase`, nên DI của .NET bỏ qua behavior này khi đóng generic cho một query — không
có chi phí nào, và không có nhánh nào để ai đó sửa nhầm sau này.

Vì sao query không cần transaction: một câu `SELECT` đơn lẻ đã atomic. Bọc nó lại chỉ thêm
`BEGIN`/`COMMIT` cho mỗi lượt đọc — nghĩa là gấp ba số round-trip cho việc không đổi lấy
gì.

Điểm commit: **chỉ khi `response.IsSuccess`**. Handler trả `Result` thất bại thì mọi thay
đổi bị rollback — kể cả khi handler đã kịp `Add` vài entity trước khi phát hiện vi phạm.
Đây là lý do handler **không được** tự gọi `SaveChangesAsync`: gọi rồi thì lỗi phát hiện
sau đó không rollback được nữa.

### 5.4 Vì sao behavior KHÔNG dùng reflection

Cách thường gặp để dựng một `Result<T>` thất bại bên trong một behavior generic là gọi
`typeof(Result<>).MakeGenericType(...)` rồi `Activator.CreateInstance`. Repo này **cấm**.

`IResultFactory<TSelf>` với `static abstract` (có từ C# 11, dùng bình thường trên .NET 10)
cho phép viết `TResponse.FromError(error)` — compiler phân giải, không reflection, không
boxing, và **đổi chữ ký thì đỏ lúc biên dịch** thay vì trả `null` lúc chạy.

Đây không phải sở thích. Ở dự án tiền nhiệm, cầu nối lỗi dựng bằng `GetMethod` + `Invoke`
với danh sách kiểu hardcode; khi chữ ký đổi, `GetMethod` trả `null` và **mọi lỗi nghiệp vụ
biến thành `NullReferenceException`** — hỏng đúng nhánh lỗi, nên chỉ lộ ra ở Production.
Xem [`../audit/2026-09-05-reflection-envelope.md`](../audit/2026-09-05-reflection-envelope.md).

### 5.5 Vì sao CHƯA có Logging / Performance / Caching behavior

Ghi rõ ở đây để người sau không tưởng là bỏ sót.

| Behavior thường thấy | Vì sao chưa làm ở v1 | Điều kiện để thêm |
| --- | --- | --- |
| `LoggingBehavior` | ASP.NET Core đã log request/response ở tầng HTTP kèm `traceId`. Thêm một tầng log nữa nghĩa là mỗi request sinh hai bộ log gần giống nhau, và người đọc log phải học cách phân biệt chúng | Khi có nhu cầu log **theo use case** mà tầng HTTP không thấy được (ví dụ tham số đã giải mã) |
| `PerformanceBehavior` | Nó đo thời gian handler, nhưng thứ chậm gần như luôn là **query**, và EF Core đã log câu SQL kèm thời gian. Một cảnh báo "handler chạy quá 500ms" không nói được chậm ở đâu | Khi đã có tracing và vẫn cần mốc đo ở ranh giới use case |
| `CachingBehavior` | **Chưa đo được vấn đề nào.** Cache đặt trước khi đo chỉ **che** lỗi: lần miss vẫn chậm y hệt, seq scan vẫn nguyên, và có thêm một tầng để debug khi số liệu hiển thị sai | Xem [`be-performance.md`](be-performance.md) — ba điều kiện bắt buộc trước khi thêm bất kỳ cache nào |
| `AuthorizationBehavior` | Phân quyền đã chặn ở tầng HTTP bằng `RequirePermissionAttribute`. Hai cơ chế song song cho cùng một việc là hai nguồn có thể lệch | Khi cần phân quyền theo **từng bản ghi** mà attribute không biểu diễn được |

Nguyên tắc chung áp cho cả bốn dòng: **thêm sau khi đo, không thêm để phòng xa.** Mỗi
behavior là một tầng mọi request phải đi qua và một chỗ nữa để nhìn khi có gì đó sai.

---

## 6. Validator (FluentValidation)

### 6.1 Quy ước

```csharp
// Core.Application/Users/CreateUserCommandValidator.cs
internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithErrorCode(UserErrors.UserNameRequired.Code)
            .MaximumLength(64).WithErrorCode(UserErrors.UserNameTooLong.Code);

        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(UserErrors.EmailRequired.Code)
            .EmailAddress().WithErrorCode(UserErrors.EmailMalformed.Code);

        RuleFor(x => x.RoleNames)
            .NotEmpty().WithErrorCode(UserErrors.RoleRequired.Code);
    }
}
```

| Quy ước | Lý do |
| --- | --- |
| Validator chỉ kiểm thứ **tính được từ payload** | Kiểm cần DB thuộc handler — giữ validator thuần, test không cần hạ tầng |
| **Không** `WithMessage("câu tiếng Việt")` | Câu hiển thị do client dựng từ mã. Luật `NoUserFacingVietnameseString_InBackend` |
| **Bắt buộc** `WithErrorCode(...)` lấy từ catalog | Không có mã thì FE không tra được bảng dịch, và `fieldErrors` chỉ còn chuỗi |
| Một validator một file, cùng thư mục với command | Vertical slice |
| `internal sealed` | Không ai ngoài assembly gọi trực tiếp; MediatR/DI resolve qua interface |

Luật `EveryAbstractValidator_IsRegistered` canh: một validator viết ra mà không đăng ký sẽ
**không bao giờ chạy**, và không có gì báo — code trông như đã có kiểm nhưng thực tế không.

### 6.2 ⚠️ Nhiều validator cho MỘT request — mỗi cái phải có `ValidationContext` riêng

Một request được phép có nhiều validator (`ValidationBehavior` chạy mọi
`IValidator<TRequest>` đã đăng ký). Ở đó có một cái bẫy im lặng.

Rule kiểu `Custom` / `CustomAsync` báo lỗi bằng `context.AddFailure(...)` — tức **ghi thẳng
vào `ValidationContext`**, không phải trả về kết quả. Nếu behavior dựng **một** context rồi
truyền cho tất cả validator, thì `ValidationResult` của **mọi** validator đều chứa lỗi đó,
và việc gộp kết quả sẽ đếm nó lặp lại đúng bằng số validator.

**Triệu chứng:** người dùng vi phạm đúng một luật nhưng nhận thông điệp lặp lại trong
`fieldErrors`. Không lỗi biên dịch, không exception, không log gì.

**Vì sao bẫy này ẩn lâu:** nó chỉ kích hoạt khi một request có từ hai validator trở lên.
Trong phần lớn vòng đời một dự án, mỗi request chỉ có một validator, nên điều kiện kích
hoạt chưa từng tồn tại — rồi một ngày ai đó tách validator theo nhóm luật và nó nổ.

Bài học rộng hơn FluentValidation: **thứ gì nhận trạng thái chia sẻ để ghi kết quả vào thì
không dùng chung được giữa nhiều lượt chạy.** Cùng lý do khiến `Error` trong catalog là
`record` bất biến và `WithParams` trả bản mới (§2.1).

Trong đoạn mẫu §5.2, lời dựng context nằm **bên trong** vòng lặp theo từng validator. Đó
là chỗ duy nhất nó được phép nằm.

---

## 7. `ErrorDescriptor`, catalog lỗi, và khuôn mã

> **Về tên gọi.** [`../RULES.md`](../RULES.md) §5 và [`../README.md`](../README.md) dùng từ
> **`ErrorDescriptor`** cho khái niệm "bản mô tả một lỗi nghiệp vụ đã khai sẵn trong
> catalog". Kiểu C# hiện thực khái niệm đó tên là **`Error`** (§2.1) — ngắn hơn và đọc
> xuôi hơn tại chỗ dùng (`Result.Failure(UserErrors.NotFound)`). Hai từ, một thứ. Tên
> ArchTest giữ nguyên `ErrorDescriptor_IsNever_BuiltFrom_AStringLiteral_OutsideACatalog`
> vì nó là định danh đã khai trong `RULES.md`.

### 7.1 Catalog tập trung — mã không được dựng từ chuỗi literal

```csharp
// Core.Application/Users/UserErrors.cs
public static class UserErrors
{
    public static readonly Error NotFound = new(
        "CORE.USER.NOT_FOUND", "Không tìm thấy người dùng.", ErrorType.NotFound);

    public static readonly Error EmailDuplicated = new(
        "CORE.USER.EMAIL_DUPLICATED", "Email '{Email}' đã được dùng.", ErrorType.Conflict);

    public static readonly Error UserNameRequired = new(
        "CORE.USER.USERNAME_REQUIRED", "Tên đăng nhập không được để trống.", ErrorType.Validation);

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
        "Chỉ người mang vai trò hệ thống mới khoá được tài khoản mang vai trò hệ thống.",
        ErrorType.Forbidden);

    public static readonly Error RoleNotFound = new(
        "CORE.USER.ROLE_NOT_FOUND", "Vai trò '{RoleId}' không tồn tại.", ErrorType.BusinessRule);
}
```

> **Bốn mã cuối là chỗ hay chọn sai `ErrorType` nhất, và ranh giới nằm ở §3.2.**
>
> | Mã | `ErrorType` | Vì sao |
> | --- | --- | --- |
> | `ROLE_ESCALATION_FORBIDDEN` · `SYSTEM_ROLE_LOCK_FORBIDDEN` | `Forbidden` | Người gọi **thiếu tư cách** trên bản ghi cụ thể: họ không có đủ tập quyền mà vai trò đích cấp, hoặc không mang vai trò hệ thống. Đó là hàng "không đủ quyền trên bản ghi cụ thể" |
> | `CANNOT_LOCK_SELF` · `SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN` | `BusinessRule` | Người gọi **có đủ quyền**; thứ bị từ chối là *thao tác nhắm vào chính mình*. Trả 403 ở đây làm FE hiện "bạn không có quyền" cho một người đang có quyền |
> | `ROLE_NOT_FOUND` | `BusinessRule` | Id **trong payload** trỏ tới bản ghi không tồn tại — §3.2 hàng 2. Không phải `Validation`: `Validation` theo định nghĩa tính được từ payload mà **không cần DB**, còn "vai trò này có tồn tại không" bắt buộc đọc DB. Kèm theo, mã `Validation` luôn đi cùng `fieldErrors`; mã này thì không |
>
> Hợp đồng phía dây của bốn mã này: [`../contracts/users.md`](../contracts/users.md) §2.

**Luật:** không nơi nào ngoài một catalog được viết `new Error("...", ...)` với chuỗi
literal. Luật `ErrorDescriptor_IsNever_BuiltFrom_AStringLiteral_OutsideACatalog`
([`../RULES.md`](../RULES.md) §5) canh điều này bằng cách quét mã nguồn.

Vì sao luật này đáng có một cổng riêng: một chuỗi gõ tại chỗ `throw`/`return` đi **thẳng**
ra field `code` của envelope mà không qua catalog nào. Hệ quả là hai hệ mã cùng tồn tại
trong một field — một hệ có kiểm, một hệ không. FE tra bảng dịch theo mã, nên mã ngoài
catalog nghĩa là một câu không bao giờ dịch được.

### 7.2 Ranh giới của luật R8 — chuỗi tiếng Việt được phép ở ĐÚNG một chỗ

Luật R8 ([`../RULES.md`](../RULES.md) §5) cấm hardcode câu hiển thị trong BE, và ArchTest
`NoUserFacingVietnameseString_InBackend` ép nó. Nhưng catalog ở §7.1 **bắt buộc** mang một
câu tiếng Việt cho mỗi mã. Hai điều đó chỉ cùng đúng khi ranh giới được khai ra — nếu không,
detector viết theo đúng tên luật sẽ báo đỏ chính hình dạng mà mục trên bắt buộc, và một mục
cổng hay báo sai là một mục sẽ bị tắt đi.

> **Chuỗi tiếng Việt chỉ được phép ở tham số `MessageTemplate` của một `Error` khai trong
> catalog. Mọi vị trí khác trong BE là vi phạm.**

| Vị trí | Được? | Vì sao |
| --- | --- | --- |
| `MessageTemplate` của `Error` trong catalog | ✅ | Đây là **câu dự phòng**, không phải nguồn hiển thị — lý do đầy đủ ở [`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §4.1 |
| `WithMessage("…")` của validator | 🛑 | Validator sinh `fieldErrors`, và mỗi mục ở đó mang **mã**; câu đến từ i18n phía FE |
| Chuỗi trả thẳng từ controller, handler, middleware | 🛑 | Bỏ qua catalog ⇒ không có mã ⇒ FE không có gì để tra |
| Thông điệp log, tên biến, comment | ✅ | Không hiển thị cho người dùng; R8 nói về **câu hiển thị** |
| Chuỗi trong exception của hạ tầng ta không sở hữu | ✅ | Không ánh xạ ra envelope; xem §7.1 |

**Vì sao ranh giới nằm đúng ở đó:** câu ở catalog là thứ duy nhất trong BE gắn **một–một**
với một mã. Mọi chuỗi khác không có mã đi kèm, nên FE không thể thay nó bằng bản dịch — và
đó chính xác là thứ R8 tồn tại để ngăn.

**Hệ quả cho người viết detector:** cho phép chuỗi tiếng Việt ở tham số thứ hai của một
`new Error(...)` nằm trong file catalog; cấm ở mọi vị trí còn lại. Đừng viết detector quét
toàn bộ chuỗi tiếng Việt rồi thêm danh sách miễn trừ theo tên file — danh sách đó sẽ mục
ruỗng ngay từ file catalog thứ hai.

### 7.3 Khuôn mã lỗi — định nghĩa gốc

```
<MIỀN>.<TÀI NGUYÊN>.<LÝ DO>
```

| Thành phần | Quy tắc | Ví dụ |
| --- | --- | --- |
| MIỀN | `CORE` cho Core; tên module viết hoa cho module | `CORE`, `SKILL` |
| TÀI NGUYÊN | Danh từ số ít, viết hoa | `USER`, `ROLE`, `MENU` |
| LÝ DO | `UPPER_SNAKE_CASE`, mô tả **nguyên nhân**, không mô tả câu hiển thị | `EMAIL_DUPLICATED`, `NOT_FOUND` |

Biểu thức kiểm: `^[A-Z][A-Z0-9]*(\.[A-Z][A-Z0-9_]*){2}$`

Ba luật kèm theo:

1. **Duy nhất toàn hệ.** Hai mã trùng nhau ở hai module là hai câu khác nhau tranh cùng một
   khoá dịch. `EveryBusinessCode_Matches_Format` và `_IsUnique` canh cả hai vế.
2. **Mã là hợp đồng công khai, đổi mã là breaking change.** Nó nằm trong bảng dịch của FE
   và có thể nằm trong tài liệu người dùng. Đổi câu chữ thì tự do; đổi mã thì phải qua
   contract.
3. **Mã mô tả nguyên nhân, không mô tả giao diện.** `CORE.USER.EMAIL_DUPLICATED` đúng;
   `CORE.USER.SHOW_RED_TOAST` sai — nó khoá chặt BE vào một quyết định giao diện.

### 7.4 Mã cho điều kiện PHÍA CLIENT — vẫn do BE khai

Có những lỗi mà **BE không bao giờ phát ra**, vì lúc đó không có phản hồi nào cả: mất mạng,
tải chunk hỏng, proxy trả HTML thay vì envelope. FE vẫn cần một mã để tra bảng dịch.

> 🛑 **FE KHÔNG được tự chế mã.** Mọi mã — kể cả mã chỉ dùng ở phía client — khai trong
> catalog của BE, cùng một khuôn, cùng một danh mục.

| Mã | Khi nào FE dùng |
| --- | --- |
| `CORE.CLIENT.NO_CONNECTION` | Không nhận được phản hồi nào (mất mạng, DNS hỏng, CORS chặn) |
| `CORE.CLIENT.RESOURCE_LOAD_FAILED` | Tải một tài nguyên của app thất bại (chunk sau khi triển khai bản mới) |

**Vì sao vẫn để BE khai một thứ BE không phát ra:** luật R3 đòi mã **duy nhất trong toàn
hệ**, và ArchTest ép nó bằng cách quét catalog của BE. Một mã sống ngoài catalog thì không
phép kiểm nào chạm tới — nó có thể trùng với một mã BE thêm sau, và bảng dịch có thể thiếu
nó mà không gì báo. Một danh mục, một phép kiểm.

Hệ quả cho nợ **F16** ([`../RULES.md`](../RULES.md) §10): script đối chiếu *"mọi khoá i18n
FE tra cứu phải khớp một mã BE đã khai"* chạy được **hai chiều** mà không cần ngoại lệ —
vì nhóm `CORE.CLIENT.*` cũng nằm trong catalog.

### 7.5 Ranh giới với mã của `fieldErrors`

Hai hệ mã, hai field, **có chủ đích**:

| Field | Mã đến từ | Khuôn |
| --- | --- | --- |
| `error.code` | Catalog của feature | `MIỀN.TÀI_NGUYÊN.LÝ_DO` |
| `error.fieldErrors[<Field>][].code` | `WithErrorCode(...)` của validator, lấy từ cùng catalog | Cùng khuôn |

Giữ cùng khuôn cho cả hai là quyết định của repo này (dự án tiền nhiệm để mã field là tên
validator của FluentValidation — `"NotEmptyValidator"` — nên FE phải học **hai** hệ mã).
Cái giá: mỗi rule validator phải khai `WithErrorCode` tường minh, không dùng được mã mặc
định. Cái được: FE có **một** bảng dịch, tra bằng **một** hàm.

---

## 8. i18n — BE trả MÃ + THAM SỐ ĐẶT TÊN, không trả câu đã ghép

### 8.1 Hợp đồng thông điệp lỗi — định nghĩa gốc

Backend **không bao giờ** gửi ra một câu đã ghép hoàn chỉnh để hiển thị cho người dùng. Nó
gửi:

- `code` — khoá tra bảng dịch,
- `messageParams` — từ điển tham số **theo tên**,
- `message` — câu tiếng Việt **dev-facing + dự phòng**, dùng cho log; client không bị buộc
  hiển thị nó.

```json
{
  "code": "CORE.USER.EMAIL_DUPLICATED",
  "message": "Email 'an@vd.vn' đã được dùng.",
  "messageParams": { "Email": "an@vd.vn" }
}
```

### 8.2 Vì sao tham số phải theo TÊN, không theo thứ tự (`{0}`)

Đây là điểm dễ bị coi là chi tiết nhỏ, nhưng nó quyết định bảng dịch có dùng được hay
không.

**Trật tự từ mỗi ngôn ngữ một khác.** Câu *"Email {0} đã được {1} dùng"* dịch sang một
ngôn ngữ khác có thể phải đảo hai chỗ giữ, hoặc lặp lại một chỗ giữ hai lần, hoặc bỏ hẳn
một chỗ giữ vì ngôn ngữ đó không cần nó. Với `{0}`/`{1}`, bảng dịch buộc phải giữ nguyên
số lượng và **suy đoán** ý nghĩa của từng vị trí.

Với tham số đặt tên, bảng dịch tự do sắp xếp:

| Ngôn ngữ | Bản dịch cho `CORE.USER.EMAIL_DUPLICATED` |
| --- | --- |
| vi | `Email {Email} đã được dùng.` |
| en | `The email address {Email} is already taken.` |
| (một ngôn ngữ đảo trật tự) | `{Email} — địa chỉ này đã có người dùng.` |

Ba bản dịch dùng cùng một bộ tham số, không bản nào phải biết vị trí của chỗ giữ trong bản
gốc.

**Hệ quả quan trọng hơn:** nếu BE gửi câu **đã ghép**, client không tách lại được tham số
ra khỏi chuỗi đó. Nó buộc phải hiển thị nguyên câu tiếng Việt — và toàn bộ công i18n dừng
lại đúng trước cửa. Đây là lý do `messageParams` phải nằm **cạnh mã mà nó tham số hoá**:
cạnh `code` ở gốc, và cạnh `code` trong từng phần tử của `fieldErrors`.

Vì sao không gom mọi tham số về gốc envelope: một lần submit hỏng nhiều field thì mỗi
field có bộ tham số riêng, và hai field cùng vi phạm một luật sẽ **ghi đè khoá của nhau** —
câu của field này hiện con số của field kia. Hỏng im lặng.

Luật `MessageParams_AreNamed_NotPositional` và `NoUserFacingVietnameseString_InBackend`
([`../RULES.md`](../RULES.md) §5) canh hai vế này.

### 8.3 ⚠️ KHÔNG dùng `FluentValidation.Internal.MessageFormatter`

Nó ráp được câu theo chỗ giữ đặt tên, và nó rất cám dỗ vì đã có sẵn trong package.

**Namespace của nó là `Internal`.** Kiểu trong `Internal` không nằm trong hợp đồng public
của thư viện: nó đổi chữ ký, đổi hành vi, hoặc biến mất ở **bất kỳ bản minor nào**, và
điều đó hoàn toàn hợp lệ về mặt semver. Dựng chỗ ráp câu của toàn hệ trên một kiểu như vậy
nghĩa là một lần `dotnet restore` bình thường có thể làm vỡ mọi thông điệp lỗi.

Bộ ráp câu tự viết — đủ ngắn để không phải mượn:

```csharp
// Core.Domain/Common/MessageTemplateRenderer.cs
public static partial class MessageTemplateRenderer
{
    [GeneratedRegex(@"\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}")]
    private static partial Regex Placeholder();

    public static string Render(string template, IReadOnlyDictionary<string, string>? args)
    {
        if (args is null || args.Count == 0)
            return template;

        return Placeholder().Replace(template, match =>
            args.TryGetValue(match.Groups["name"].Value, out var value)
                ? value
                : match.Value);   // chỗ giữ không có tham số: GIỮ NGUYÊN, không xoá
    }
}
```

Ba quyết định trong mười dòng đó:

- **Chỗ giữ thiếu tham số thì giữ nguyên literal**, không thay bằng chuỗi rỗng. Một câu
  hiện `{Email}` giữa giao diện là lỗi **nhìn thấy được**; một câu thiếu mất chỗ đó thì
  đọc vẫn xuôi và không ai phát hiện.
- **`[GeneratedRegex]`** — regex biên dịch lúc build, không dựng lại mỗi lần gọi.
- **Đặt ở `Core.Domain`** — nó chỉ dùng BCL, và cả Domain lẫn Application đều cần nó.

Câu ráp ra đi vào field `message` (dev-facing). Client vẫn dựng câu của mình từ `code` +
`messageParams`.

---

## 9. Grid / danh sách — `PagedList<T>`

### 9.1 Hình dạng

```csharp
// Core.Application/Common/Paging/PagedList.cs
public sealed class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
```

**Không có `TotalPages`.** Nó suy ra được từ `TotalCount` và `PageSize`; gửi kèm dữ liệu
suy ra được nghĩa là tạo hai nguồn có thể lệch nhau. Component phân trang của PrimeNG chỉ
cần tổng số bản ghi và tự tính số trang.

**Tên là `TotalCount`, không phải `Total`.** `Total` không nói rõ tổng của cái gì — dòng?
trang? byte?

### 9.2 Envelope nhất quán

Endpoint danh sách trả **cùng envelope** với endpoint đơn: `Result<PagedList<UserDto>>`,
không trả `PagedList<T>` trần. Đây là quyết định có chủ đích để FE viết **một** hàm bóc
envelope, không phải hai nhánh tuỳ loại endpoint.

"Envelope drift" — mỗi loại endpoint một hình dạng — là lỗi đã xảy ra thật ở hệ tham
chiếu, và nó không lộ ra ở BE: nó lộ ra ở FE dưới dạng `undefined` chỗ này chỗ kia.

### 9.3 Quy ước paging / sort / filter

| Tham số | Quy ước | Ràng buộc |
| --- | --- | --- |
| `Page` | Bắt đầu từ **1** | `>= 1`; validator ép |
| `PageSize` | Mặc định 20 | `1..200`; validator ép trần **ở BE**, không tin client |
| `SortBy` | Tên **field của DTO**, không phải tên cột DB | Phải nằm trong allowlist của từng query |
| `SortDescending` | `bool`, mặc định `false` | — |
| Filter | Field rời trên record query, không phải một chuỗi biểu thức | — |

Hai luật cứng:

1. **`SortBy` phải qua allowlist.** Ghép thẳng chuỗi client gửi vào `OrderBy(...)` dạng
   chuỗi động là mở đường cho lỗi lúc chạy ở tốt nhất, và cho injection ở tệ nhất.
2. **Sắp xếp luôn có tiêu chí phụ ổn định.** `ORDER BY UpdatedAt DESC` trên dữ liệu có
   giá trị trùng cho thứ tự **không xác định** giữa các trang: cùng một bản ghi xuất hiện
   ở trang 1 và trang 2, một bản ghi khác không xuất hiện ở đâu cả. Luôn thêm `Id` làm
   tiêu chí cuối.

Cách phân trang keyset cho dữ liệu lớn: [`be-performance.md`](be-performance.md).

### 9.4 `GET` hay `POST` cho danh sách

Mặc định `GET` + query string. Chuyển sang `POST` với body chỉ khi bộ lọc **thật sự**
phức tạp (nhiều nhóm điều kiện lồng nhau, danh sách giá trị dài vượt giới hạn độ dài URL).

Cái giá của `POST`: mất khả năng bookmark, mất cache của tầng HTTP, và mất tính idempotent
mà công cụ giám sát trông vào. Đừng trả cái giá đó cho một màn hình có ba ô lọc.

---

## 10. Command chạy lâu → job nền

### 10.1 Ngưỡng tách

Tách sang job nền khi **một** trong các điều sau đúng:

- Khối lượng xử lý **không có trần rõ ràng** (import/export file do người dùng cung cấp).
- Gọi ra ngoài mà latency **không kiểm soát được** (gửi email, gọi hệ thống bên thứ ba).
- Thời gian xử lý kỳ vọng vượt ngưỡng timeout của tầng proxy/gateway.

Command CRUD thường (tạo/sửa một bản ghi) **không** áp dụng — nó chỉ thêm phức tạp và
thêm một trạng thái trung gian mà người dùng phải chờ.

### 10.2 Hợp đồng

```csharp
// Core.Application/Common/Interfaces/IBackgroundJobScheduler.cs
public interface IBackgroundJobScheduler
{
    Task<string> EnqueueAsync<TJob>(
        Expression<Func<TJob, CancellationToken, Task>> methodCall,
        CancellationToken ct = default);

    Task<string> ScheduleAsync<TJob>(
        Expression<Func<TJob, CancellationToken, Task>> methodCall,
        TimeSpan delay,
        CancellationToken ct = default);
}
```

Interface chỉ dùng `System.Linq.Expressions` của BCL — không lộ kiểu nào của thư viện job
nền ra `Core.Application`.

### 10.3 Khuôn `jobId + polling`

```csharp
// Handler làm TRỌN use case: lưu dữ liệu tạm + tạo bản ghi job + GHI Ý ĐỊNH enqueue vào outbox
internal sealed class StartImportCommandHandler(
    IImportJobRepository jobs,
    IFileStorage storage) : IRequestHandler<StartImportCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(StartImportCommand cmd, CancellationToken ct)
    {
        // Ghi file tạm nằm NGOÀI transaction — xem ràng buộc 3.
        var stored = await storage.SaveTempAsync(cmd.Content, cmd.FileName, ct);
        if (stored.IsFailure)
            return Result.Failure<Guid>(stored.Error!);

        var job = ImportJob.Create(stored.Value, cmd.FileName);
        if (job.IsFailure)
            return Result.Failure<Guid>(job.Error!);

        // ImportJob.Create ghi nhận domain event ImportJobQueued lên chính entity.
        // KHÔNG gọi scheduler ở đây: bộ chặn ở tầng dữ liệu chuyển event đó thành một dòng
        // outbox trong CÙNG transaction, và tiến trình phát nền gọi
        // IBackgroundJobScheduler sau khi commit.
        await jobs.AddAsync(job.Value, ct);

        return Result.Success(job.Value.Id);
    }
}
```

Năm ràng buộc bắt buộc:

1. **Không trả HTTP 202.** Endpoint trả 200 + envelope như mọi endpoint khác; `data` mang
   `jobId`. "Đã bắt đầu chứ chưa xong" thể hiện ở tầng **dữ liệu**, không ở HTTP status.
   Cho controller tự đặt status là tạo nguồn sự thật thứ hai cho ánh xạ `Result` → HTTP.
2. **Enqueue đi qua seam**, không gọi thẳng API của thư viện job nền trong handler — gọi
   thẳng nghĩa là `Core.Application` phụ thuộc hạ tầng cụ thể. Đặt lời enqueue lên
   controller **không** phải cách sửa: nó chỉ đổi một vi phạm lấy một vi phạm khác
   (controller chứa logic) và cắt đôi một use case.
3. **Handler KHÔNG gọi `IBackgroundJobScheduler` trực tiếp — nó ghi một dòng outbox.**
   `TransactionBehavior` bọc trọn handler (§5.3), nên một lời gọi ra ngoài đặt bên trong
   handler là một lời gọi **bên trong transaction** — đúng thứ mà
   [`../adr/0006-pipeline-behavior.md`](../adr/0006-pipeline-behavior.md) §Tiêu cực cấm:
   *"không gọi dịch vụ ngoài bên trong command; việc đó đẩy sang outbox"*.

   Hai hỏng thật nếu gọi thẳng, và chúng ngược nhau nên không có cách viết nào tránh được cả
   hai mà không có outbox:

   | Ca | Chuyện gì xảy ra |
   | --- | --- |
   | Enqueue xong, transaction **rollback** | Job chạy đi tìm một bản ghi `ImportJob` **không tồn tại** — worker ném ở một chỗ rất xa nguyên nhân |
   | Enqueue **chậm** (hàng job ở xa, đang nghẽn) | Transaction đứng mở suốt lượt gọi đó, giữ khoá trên bảng job. Đây là công thức của khoá kéo dài |

   Dòng outbox ghi trong **cùng** transaction với bản ghi job, nên hai thứ đó hoặc cùng có
   hoặc cùng không. Handler thậm chí không tự ghi dòng đó: entity ghi nhận một domain event,
   và bộ chặn ở tầng dữ liệu chuyển nó thành bản ghi outbox ngay trước khi lưu — nhờ vậy
   handler **không thể quên**. Cơ chế, bảng, và đảm bảo *ít nhất một lần*:
   [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §2,
   [`../database/schema-core.md`](../database/schema-core.md) §8.

   Hệ quả phải chấp nhận: job **không** chạy ngay lập tức, mà sau một nhịp phát outbox. Với một
   việc vốn đã là "chạy nền và poll kết quả" thì độ trễ đó không đổi gì về trải nghiệm.
4. **File upload không sống sót qua ranh giới request → job.** Phải ghi ra storage tạm
   **trước** khi ghi dòng outbox; job đọc lại từ đó. Không truyền `Stream`/`IFormFile` vào job.

   Lời ghi file này **cố ý nằm ngoài** ràng buộc 3: nó phải xong **trước** khi có bản ghi job,
   nên nó không thể đi qua outbox. Cái giá là một file tạm mồ côi khi transaction rollback —
   rẻ, và dọn được bằng một job quét file tạm quá hạn. Đổi lại nếu ghi file sau commit thì có
   một khoảng thời gian bản ghi job trỏ vào một file **chưa tồn tại**, và worker có thể chạy
   đúng vào khoảng đó.
5. **Job worker không có `HttpContext`.** Nó tự tạo scope DI riêng bằng
   `IServiceScopeFactory`, và **không** inject `ICurrentUser`. Danh tính người khởi tạo job
   phải được lưu vào chính bản ghi job lúc tạo.

Kết quả job ghi vào chính bản ghi job (trạng thái, số dòng thành công/thất bại, thông điệp
lỗi); FE poll qua một query riêng. Không cần WebSocket ở quy mô này — poll vài giây một lần
là đủ, và nó không cần thêm hạ tầng nào.

---

## 11. Trên dây là DTO, không bao giờ là entity — định nghĩa gốc

> **Handler trả DTO. Entity không đi ra khỏi tầng ứng dụng.**

Entity Framework Core và DTO **không thay thế nhau**: EF Core ánh xạ bảng thành entity trong bộ nhớ,
còn DTO là hình dạng dữ liệu đi trên dây. Cả hai cùng tồn tại, và chỗ chúng gặp nhau là câu truy vấn.

### 11.1 Vì sao không trả entity ra API

| Vấn đề | Hậu quả cụ thể |
| --- | --- |
| **Rò trường không định gửi** | Kiểu người dùng của Identity mang mã băm mật khẩu, dấu bảo mật, cờ hệ thống. Thêm một cột nhạy cảm vào bảng là **tự động** lộ nó ra API — không ai phải sửa dòng nào, nên không ai review được |
| **Hợp đồng API khoá vào schema DB** | Đổi tên một cột là vỡ FE. Đổi tên cột là việc bình thường; đổi hợp đồng API thì phải tăng phiên bản (§8.1 của [`be-api-controller.md`](be-api-controller.md)) |
| **Quan hệ vòng** | Người dùng → vai trò → người dùng. Bộ tuần tự hoá đi vào vòng lặp, hoặc phải cấu hình cắt vòng — và khi đó không ai đoán được API trả về gì |
| **Lấy thừa dữ liệu** | Entity nạp hàng chục cột cho một màn hiển thị ba cột |

### 11.2 Cách làm: truy vấn thẳng vào DTO

Viết phép chiếu **ngay trong câu truy vấn** để EF Core sinh SQL chỉ lấy đúng các cột của DTO. Không
có bước trung gian "nạp entity rồi chép sang DTO" — xem [`be-performance.md`](be-performance.md) §3.

### 11.3 Không thêm thư viện ánh xạ tự động

Viết phép chiếu bằng tay. Không dùng thư viện ánh xạ tự động, vì hai lý do đo được:

- Nó **che mất** chỗ đang nạp thừa cột: cấu hình ánh xạ nằm ở file khác, còn câu truy vấn trông vẫn gọn.
- Nó hỏng **lúc chạy**, không hỏng lúc biên dịch: đổi tên một trường thì bên nhận nhận `null` và mọi
  thứ vẫn chạy. Viết tay thì cùng thay đổi đó là một lỗi biên dịch, phát hiện ngay trên máy người sửa.

Cái giá phải chấp nhận: mỗi DTO có một đoạn phép chiếu viết tay, và nó lặp lại về mặt thị giác. Đổi lại
là mọi trường đi ra ngoài đều do một người viết ra và một người review thấy.

