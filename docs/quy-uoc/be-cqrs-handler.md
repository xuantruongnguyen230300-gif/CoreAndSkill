---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quy ước Backend — CQRS, Handler, `Result<T>`, mã lỗi

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu 2026-09-20). `src/BE` đã có trên đĩa, nên code mẫu
> dưới đây **có bản thật để đối chiếu**. Chưa ai đối chiếu toàn file, nên `verified:` giữ
> `chua-doi-chieu`: khớp hay không thì mở file mã nguồn ra so và nêu `file:dòng`.
>
> Đây là file chủ của: `ICommand<T>`/`IQuery<T>`, `Result<T>`, `Error`, `ErrorType`,
> `ErrorDescriptor`, pipeline behavior, validator, `PagedList<T>`, tham số thông điệp đặt
> tên. File khác chỉ được **trỏ** tới đây, không chép lại.
>
> 📖 Lý do, bẫy, ví dụ mở rộng của từng mục — cùng số §: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md).

---

## 1. Command / Query qua MediatR

Mỗi use case = một Command hoặc Query + một Handler. Khai qua `ICommand<T>` / `IQuery<T>`,
**không** khai `IRequest<Result<T>>` trực tiếp — envelope bị ép ở tầng **kiểu**.

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
    string TempPassword,
    IReadOnlyList<Guid> RoleIds) : ICommand<Guid>;

// Core.Application/Users/GetUserByIdQuery.cs
public sealed record GetUserByIdQuery(Guid Id) : IQuery<UserListItemDto>;

// Core.Application/Users/GetUsersListQuery.cs — tên tham số theo §9.3
public sealed record GetUsersListQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool SortDescending = false,
    string? SearchText = null) : IQuery<PagedList<UserDto>>;
```

| Quy ước | Lý do |
| --- | --- |
| Command/Query là **`sealed record`** bất biến | Không ai sửa được request giữa các behavior |
| Query danh sách: `Get{Entity}sListQuery` | Một khuôn duy nhất để `grep` ra mọi endpoint danh sách |
| `ICommandBase` là marker **rỗng** | `TransactionBehavior` ràng buộc theo nó — §5 |
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
    Unauthorized,   // chưa đăng nhập / phiên hết hạn — CHỈ hạ tầng phát (luật R9)
    Unexpected,     // lỗi hệ thống ngoài dự kiến — CHỈ IExceptionHandler phát; handler và Domain không bao giờ trả
}
```

`Unauthorized` và `Unexpected`: handler và Domain **không bao giờ** trả — cookie scheme phát `Unauthorized`, `IExceptionHandler` phát `Unexpected`
([`be-api-controller.md`](be-api-controller.md) §2.4,
[`../adr/0027-errortype-unauthorized.md`](../adr/0027-errortype-unauthorized.md)). Thêm/bớt giá trị thì
sửa ánh xạ ở [`be-api-controller.md`](be-api-controller.md) §1.1 cùng lượt.

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

`Code` là khoá; `MessageTemplate` là câu dự phòng (§8.1); `Params` theo **TÊN** (§8.2); `Error` bất
biến — `WithParams` trả bản mới, nên `Error` trong catalog là `static readonly` dùng chung.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §2.1

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

`IResultFactory<TSelf>` (`static abstract`) cho behavior dựng `Result` thất bại **đúng kiểu trả về** mà
không reflection — §5.4. `Value` ném khi `IsFailure` — có chủ đích: bug lập trình, thuộc nhóm được phép
ném ([`be-entity-domain.md`](be-entity-domain.md) §3.4).

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

**Ngưỡng dùng:** `Bind` khi chuỗi có **từ ba bước trở lên** và mỗi bước chỉ nhận kết quả của bước
trước. Một bước cần dữ liệu từ hai bước trước đó thì viết `if` phẳng.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §2.3

---

## 3. Handler — bốn bước, KHÔNG tự quản transaction

1. **Luật nghiệp vụ cần dữ liệu** — trả `Result` lỗi, **không** ném.
2. **Dựng entity qua factory của Domain** — không `new` rồi gán property.
3. **Gọi ra ngoài qua interface** — **không** gọi `SaveChangesAsync`.
4. **Trả kết quả.**

Mẫu thật đủ bốn bước: `src/BE/Core/CoreAndSkill.Core.Application/Files/UploadFileCommandHandler.cs` — `StoredFile.Create(`, rồi `files.AddAsync(`. Không có entity Domain thì không có bước 2 — người dùng là kiểu Identity: `src/BE/Core/CoreAndSkill.Core.Application/Users/CreateUserCommandHandler.cs` gọi `userAdmin.CreateAsync(`.

### 3.1 Handler KHÔNG làm gì

| Không làm | Ai làm thay |
| --- | --- |
| `try/catch` cho lỗi hạ tầng | `IExceptionHandler` của Core ở `Core.Web` — [`be-architecture.md`](be-architecture.md) §2.1 |
| Validate định dạng input (rỗng, độ dài, regex) | `ValidationBehavior` + FluentValidation, chạy **trước** handler |
| `BeginTransaction` / `Commit` / `Rollback` | `TransactionBehavior` — xem §5 |
| `SaveChangesAsync` | `TransactionBehavior`, và chỉ khi kết quả thành công |
| Đặt HTTP status | `ResultToHttpMapper` ở `Core.Web` — [`be-api-controller.md`](be-api-controller.md) |
| Ghép câu tiếng Việt cho người dùng | Client, từ `Code` + `Params` — §7 |

### 3.2 Handler PHẢI tự làm

- **Validate cần dữ liệu**: trùng unique, tham chiếu có tồn tại, trạng thái có cho phép thao tác.
  Validator giữ **thuần**, không truy cập DB.
- **Chọn đúng `ErrorType`** — quyết định của handler, không của controller:

| Tình huống | `ErrorType` |
| --- | --- |
| `{id}` trên route không tồn tại (kể cả đã soft delete) | `NotFound` |
| Một Id **trong payload** trỏ tới bản ghi không tồn tại | `BusinessRule` |
| Trùng giá trị unique (handler kiểm trước khi ghi) | `Conflict` — `DbUpdateConcurrencyException` **không** tới handler: `IExceptionHandler` dịch thành 409 `CommonErrors.ConcurrencyConflict`, trừ ghi qua `UserManager` ([`be-api-controller.md`](be-api-controller.md) §2.4) |
| Đã đăng nhập nhưng không đủ quyền trên **bản ghi cụ thể** này | `Forbidden` |
| Trạng thái không cho phép thao tác ("đã duyệt thì không sửa") | `BusinessRule` |

Ca soft delete cố ý **không** tách mã riêng so với "không tồn tại".

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §3.2

### 3.3 Vì sao handler không tự quản transaction

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §3.3

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

**Một lần ghi là MỘT transaction trên MỌI `DbContext` đã đăng ký** — Core và mọi module
([`../adr/0025-luu-du-lieu-module-mot-transaction.md`](../adr/0025-luu-du-lieu-module-mot-transaction.md)),
qua **chung một** `DbConnection` scoped; mỗi `DbContext` đăng ký lại dưới kiểu `DbContext` để `UnitOfWork`
nhận đủ — mã ở `CoreInfrastructureServiceCollectionExtensions.cs`, chuỗi `services.AddScoped<DbContext>(`. Cấu hình
đủ: [`be-performance.md`](be-performance.md) §7.2. `DbContext` của module: 📐 chưa hỗ trợ, [nợ](../DEBT.md) B22.

`UnitOfWork` (`Core.Infrastructure/Persistence/`) nhận `DbConnection` + `IEnumerable<DbContext>`.
`ExecuteInTransactionAsync` theo đúng thứ tự: bọc bằng `Database.CreateExecutionStrategy().ExecuteAsync`
→ `ChangeTracker.Clear()` mọi context ở đầu **mỗi** lượt thử lại → mở kết nối, `BeginTransactionAsync`,
gắn **mọi** context bằng `UseTransactionAsync(tx)` → chạy `operation` → `ShouldCommit` thì
`SaveChangesAsync` từng context rồi `CommitAsync`, ngược lại `RollbackAsync` → `finally`
`UseTransactionAsync(null)`. Ba bẫy (execution strategy, `Clear`, gắn đủ context) hỏng **lúc chạy** nếu bỏ.

**`operation` có thể chạy nhiều lần** — chiến lược thử lại chạy lại cả handler. Hai luật
([`../adr/0053-thu-lai-khong-ap-cho-het-han-cho-va-commit-khong-ro-ket-qua.md`](../adr/0053-thu-lai-khong-ap-cho-het-han-cho-va-commit-khong-ro-ket-qua.md)):

1. Tác dụng ngoài database trong `operation` chỉ được là thứ mà một lượt chạy thừa để lại **rác có
   job dọn sẵn**, ví dụ tệp vào kho, và đầu vào phải đọc lại được (stream tua về 0). Mọi tác dụng
   nhìn thấy từ ngoài hệ thống — gửi thư, gọi HTTP ra ngoài — đi qua outbox, không gọi trực tiếp.
2. Lỗi đường truyền ném từ `CommitAsync` **không** được thử lại: `UnitOfWork` bọc nó thành ngoại lệ
   không tạm thời. `PostgresException` từ commit là máy chủ đã từ chối, nên vẫn theo phân loại thường.
   ✅ CÓ THẬT (đối chiếu 2026-09-22): `src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/UnitOfWork.cs`
   (chuỗi `throw new CommitOutcomeUnknownException(traceId, ex)`).
   Commit gọi với `CancellationToken.None`, không với token của request: đã tới commit thì client rời đi không
   đổi việc dữ liệu có nên ghi hay không. Huỷ **trước** commit đi ra như thường, không bị bọc —
   [ADR-0055](../adr/0055-commit-khong-nhan-token-huy-va-het-han-mo-ket-noi-khong-thu-lai.md), luật E14.
   ✅ CÓ THẬT (đối chiếu 2026-09-23): `src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/UnitOfWork.cs`
   (chuỗi `await transaction.CommitAsync(CancellationToken.None)`).

> 📖 Lý do, bẫy, ví dụ mở rộng (gồm khối `UnitOfWork` đầy đủ): [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §4

---

## 5. Pipeline behavior — đúng HAI cái ở v1

### 5.1 Danh sách và thứ tự đăng ký

```csharp
// Core.Application/DependencyInjection.cs
public static IServiceCollection AddCoreApplication(this IServiceCollection services)
{
    // Assembly module do AddXModule() ghi nhận trước đó; gom xong thì niêm — be-architecture.md §5.1.
    Assembly[] assemblies = [typeof(ICommandBase).Assembly, .. services.SealModuleAssemblies()];

    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblies(assemblies);
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));    // 1 — chạy TRƯỚC
        cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));   // 2 — chạy SAU
    });

    services.AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Transient, includeInternalTypes: true);
    return services;
}
```

**Một lời quét cho Core và mọi module**
([`../adr/0025-luu-du-lieu-module-mot-transaction.md`](../adr/0025-luu-du-lieu-module-mot-transaction.md)):
module ghi nhận assembly trong `AddXModule()` ([`be-architecture.md`](be-architecture.md) §5.1); không tự
gọi `AddMediatR`, không tự quét validator, **không tự đăng ký behavior** — luật A10
([`../RULES.md`](../RULES.md) §3).

> ⚠️ **`includeInternalTypes: true` không phải tuỳ chọn.** Validator là `internal sealed` (§6.1); thiếu
> cờ này thì mọi validator bị bỏ qua **im lặng**. Một assembly:
> `AddValidatorsFromAssemblyContaining<T>(ServiceLifetime.Transient, includeInternalTypes: true)`.

**Thứ tự không đổi được:** đăng ký trước nằm **ngoài cùng** — `ValidationBehavior` chặn input sai
**trước khi** transaction được mở.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §5.1

### 5.2 `ValidationBehavior` — trả `Result` lỗi, KHÔNG ném

`ValidationBehavior<TRequest, TResponse>` (`Core.Application/Common/Behaviors/`, `internal sealed`,
`where TResponse : IResultFactory<TResponse>`, nhận `IEnumerable<IValidator<TRequest>>`):

- **Mỗi** validator một `ValidationContext<TRequest>` riêng rồi `ValidateAsync` (§6.2).
- Không lỗi ⇒ `return await next(ct)`.
- Có lỗi ⇒ gộp theo `PropertyName` (`StringComparer.Ordinal`) thành `fieldErrors`, mỗi mục
  `FieldError(failure.ErrorCode, MessageParamPolicy.Filter(failure.FormattedMessagePlaceholderValues))`
  — **không có mã dự phòng**: mỗi rule bắt buộc `WithErrorCode` (§6.1, §7.5), nên `ErrorCode` luôn là
  mã catalog; rule thiếu nó để lộ tên validator của FluentValidation ra dây, và đó là lỗi phải sửa
  ở validator, không phải ở behavior; trả
  `TResponse.FromError(CommonErrors.ValidationFailed.WithFieldErrors(byField))`. **Không ném**
  `ValidationException` — mọi lỗi đi qua **một** đường dựng envelope.

> ⚠️ **`MessageParamPolicy.Filter` không bỏ qua được.** `ValidationFailure.FormattedMessagePlaceholderValues` **luôn** chứa
> `PropertyValue` = giá trị người dùng vừa gõ. Chỉ khoá trong allowlist mới ra ngoài; `PropertyValue`
> không bao giờ nằm trong allowlist đó.

> 📖 Lý do, bẫy, ví dụ mở rộng (gồm khối `ValidationBehavior` đầy đủ): [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §5.2

### 5.3 `TransactionBehavior` — chỉ bọc command

```csharp
// Core.Application/Common/Cqrs/INoTransaction.cs — marker rỗng: TransactionBehavior bỏ qua request cài nó
public interface INoTransaction { }

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
    {
        if (request is INoTransaction)
            return next(ct);   // không mở transaction, không rollback — bảng dưới

        return uow.ExecuteInTransactionAsync(async innerCt =>
        {
            var response = await next(innerCt);
            return new TransactionOutcome<TResponse>(response, response.IsSuccess);
        }, ct);
    }
}
```

**Query không đi qua behavior này** — loại trừ bằng **ràng buộc generic** `where TRequest : ICommandBase`,
không phải `if`. Commit **chỉ khi `response.IsSuccess`** — vì thế handler **không được** tự gọi
`SaveChangesAsync`.

| Luật cho `INoTransaction` | Nội dung |
| --- | --- |
| Chỉ dùng cho lệnh mà tác dụng phụ **phải giữ lại khi `Result` thất bại** | Ca duy nhất hiện có: `LoginCommand` — bộ đếm sai mật khẩu và mốc khoá của Identity phải được lưu dù đăng nhập thất bại ([`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.2) |
| Là **đúng một** điều kiện ở đầu `Handle`; không thêm nhánh nào khác vào behavior | Marker là cách duy nhất một command thoát transaction — không có cờ, không có cấu hình |
| Lệnh cài marker vẫn **không** gọi `SaveChangesAsync` (§3.1) | Tác dụng phụ do seam hạ tầng thực hiện tự lưu — lý do ở file lý do §5.3 |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §5.3

### 5.4 Vì sao behavior KHÔNG dùng reflection

**Cấm** `typeof(Result<>).MakeGenericType(...)` + `Activator.CreateInstance` trong behavior. Dùng
`TResponse.FromError(error)` qua `IResultFactory<TSelf>` (`static abstract`, C# 11) — đổi chữ ký thì đỏ
lúc biên dịch.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §5.4

### 5.5 Vì sao CHƯA có Logging / Performance / Caching behavior

> 📐 Phần chưa thi công: [`be-cqrs-handler-chua-thi-cong.md`](be-cqrs-handler-chua-thi-cong.md) §5.5

---

## 6. Validator (FluentValidation)

### 6.1 Quy ước

✅ CÓ THẬT (đối chiếu 2026-09-27): `src/BE/Core/CoreAndSkill.Core.Application/Users/CreateUserCommandValidator.cs`
(chuỗi `.NotReservedUserName();`).

```csharp
// Core.Application/Users/CreateUserCommandValidator.cs
internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code)
            .NotReservedUserName(); // danh tính hệ thống — SystemActor

        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .EmailAddress().WithErrorCode(CommonErrors.Format.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.FullName)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(200).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.TempPassword)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code);

        RuleFor(x => x.RoleIds)
            .NotNull().WithErrorCode(CommonErrors.Required.Code)
            .MaximumItems(UserRoleAssignmentRules.MaxRoleIds);
    }
}
```

| Quy ước | Lý do |
| --- | --- |
| Validator chỉ kiểm thứ **tính được từ payload** | Kiểm cần DB thuộc handler — validator thuần, test không cần hạ tầng |
| **Không** `WithMessage("câu tiếng Việt")` | Câu hiển thị do client dựng từ mã. Luật R8 ([`../RULES.md`](../RULES.md) §5) |
| **Bắt buộc** `WithErrorCode(...)` lấy từ catalog | Không có mã thì FE không tra được bảng dịch |
| Rỗng, độ dài, khuôn dạng dùng nhóm `CORE.VALIDATION.*` của `CommonErrors` (§7.1) | Một mã một bản dịch cho mọi module; mã riêng theo tài nguyên chỉ khi lý do không thuộc nhóm đó |
| Một validator một file, cùng thư mục với command | Vertical slice |
| `internal sealed` | Bộ quét phải bật `includeInternalTypes` — §5.1 |

Validator không đăng ký **không bao giờ chạy** — luật A11 ([`../RULES.md`](../RULES.md) §3).

### 6.2 ⚠️ Nhiều validator cho MỘT request — mỗi cái phải có `ValidationContext` riêng

Rule `Custom` / `CustomAsync` ghi lỗi **thẳng vào `ValidationContext`**; dùng chung một context làm lỗi
bị đếm lặp theo số validator. Lời dựng context nằm **bên trong** vòng lặp từng validator (§5.2) — chỗ
duy nhất nó được phép nằm.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §6.2

---

## 7. `ErrorDescriptor`, catalog lỗi, và khuôn mã

> **Về tên gọi.** [`../RULES.md`](../RULES.md) §5 và [`../README.md`](../README.md) gọi khái niệm này là
> **`ErrorDescriptor`**; kiểu C# hiện thực nó tên là **`Error`** (§2.1). Hai từ, một thứ. Tên ArchTest
> giữ nguyên `ErrorDescriptor_IsNever_BuiltFrom_AStringLiteral_OutsideACatalog`.

### 7.1 Catalog tập trung — mã không được dựng từ chuỗi literal

> **Khối dưới đây là KHUÔN và MẪU, không phải danh mục sống.** Mã một endpoint trả về, cùng `ErrorType`,
> sống ở card hợp đồng của endpoint đó ([`../contracts/README.md`](../contracts/README.md) §2); mã dùng
> chung của hạ tầng ở [`../contracts/auth.md`](../contracts/auth.md) §11. Chủ chuyển sang các tệp
> `*Errors.cs` khi có `src/` —
> [`../adr/0030-dieu-kien-chuyen-giai-doan-2.md`](../adr/0030-dieu-kien-chuyen-giai-doan-2.md).

Khuôn: mỗi tài nguyên một lớp `static` tên `<TàiNguyên>Errors`, mỗi mã một trường `public static readonly Error` dựng bằng `new(code, messageTemplate, ErrorType)`. Mẫu `UserErrors`: ly-do §7.1.

✅ CÓ THẬT (đối chiếu 2026-09-27): `src/BE/Core/CoreAndSkill.Core.Application/Common/CommonErrors.cs`
mang đúng chín trường dưới đây, gồm `Forbidden` (chuỗi `"CORE.AUTH.FORBIDDEN"`) — dùng khi **handler**
(không phải `RequirePermissionAttribute`) phải tự trả 403 vì điều kiện phụ thuộc dữ liệu payload.

**Mã validation dùng chung — khai MỘT lần ở catalog Core, module không khai lại:**

```csharp
// Core.Application/Common/CommonErrors.cs — cùng catalog với ValidationFailed của §5.2
public static class CommonErrors
{
    // Mã GỐC của envelope khi validator trượt — luôn kèm fieldErrors (contracts/auth.md §11)
    public static readonly Error ValidationFailed = new(
        "CORE.VALIDATION.FAILED", "Dữ liệu gửi lên không hợp lệ.", ErrorType.Validation);

    // Mã REQUIRED, MAX_LENGTH, MIN_LENGTH, FORMAT, MAX_ITEMS CHỈ xuất hiện ở fieldErrors[<Field>][].code — validator của Core và mọi module dùng lại qua WithErrorCode
    public static readonly Error Required = new(
        "CORE.VALIDATION.REQUIRED", "Trường này bắt buộc.", ErrorType.Validation);

    public static readonly Error MaxLength = new(
        "CORE.VALIDATION.MAX_LENGTH", "Tối đa {MaxLength} ký tự.", ErrorType.Validation);

    public static readonly Error MinLength = new(
        "CORE.VALIDATION.MIN_LENGTH", "Tối thiểu {MinLength} ký tự.", ErrorType.Validation);

    // CORE.SYSTEM.UNEXPECTED CHỈ IExceptionHandler phát (be-api-controller.md §2.4) — handler không
    // trả. CORE.CONCURRENCY.CONFLICT chủ yếu cũng vậy — NGOẠI LỆ: ghi qua UserManager trả thẳng mã
    // này qua Result (be-api-controller.md §2.4; wiki-core/be/06-concurrency-control.md §6.1).
    public static readonly Error ConcurrencyConflict = new(
        "CORE.CONCURRENCY.CONFLICT", "Bản ghi đã bị thay đổi bởi người khác. Tải lại rồi thử lại.", ErrorType.Conflict);

    public static readonly Error Unexpected = new(
        "CORE.SYSTEM.UNEXPECTED", "Lỗi hệ thống. Vui lòng thử lại; nếu lặp lại, báo mã theo dõi cho quản trị.", ErrorType.Unexpected);

    public static readonly Error Format = new(
        "CORE.VALIDATION.FORMAT", "Không đúng định dạng.", ErrorType.Validation);

    public static readonly Error MaxItems = new(
        "CORE.VALIDATION.MAX_ITEMS", "Tối đa {MaxItems} phần tử.", ErrorType.Validation);

    // Dùng khi HANDLER (không phải RequirePermissionAttribute) cần trả 403 do một điều kiện phụ
    // thuộc dữ liệu mà mức khai báo tĩnh của S11 không biểu diễn được.
    public static readonly Error Forbidden = new(
        "CORE.AUTH.FORBIDDEN", "Không có quyền thực hiện thao tác này.", ErrorType.Forbidden);
}
```

| Mã trong `fieldErrors` | `messageParams` | Rule FluentValidation gắn mã này |
| --- | --- | --- |
| `CORE.VALIDATION.REQUIRED` | — | `NotEmpty` / `NotNull` |
| `CORE.VALIDATION.MAX_LENGTH` | `MaxLength` | `MaximumLength(n)` |
| `CORE.VALIDATION.MIN_LENGTH` | `MinLength` | `MinimumLength(n)` |
| `CORE.VALIDATION.FORMAT` | — | `Matches`, `EmailAddress`, và mọi rule kiểm khuôn dạng |
| `CORE.VALIDATION.MAX_ITEMS` | `MaxItems` | `Must(x => x.Count <= n)` trên danh sách — trần số phần tử |

Mã gốc của envelope **vẫn là `CORE.VALIDATION.FAILED`** — các mã trên không bao giờ đứng ở `error.code`.
Khoá `messageParams` giữ đúng tên placeholder của FluentValidation (PascalCase — khoá của
`fieldErrors` và `messageParams` không đi qua camelCase, [`be-api-controller.md`](be-api-controller.md) §2.3);
allowlist của `MessageParamPolicy.Filter` (§5.2) phải chứa `MaxLength`, `MinLength` và `MaxItems`. Mã theo tài nguyên
(`CORE.USER.EMAIL_DUPLICATED`) chỉ khai khi lý do **không** thuộc các nhóm này.

Chọn `ErrorType` cho mã **thiếu tư cách** (`Forbidden`) hay **thao tác nhắm vào chính mình** (`BusinessRule`) theo ranh giới §3.2; bảng ví dụ ở ly-do §7.1.

**Luật:** không nơi nào ngoài một catalog được viết `new Error("...", ...)` với chuỗi literal —
luật R2 ([`../RULES.md`](../RULES.md) §5).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §7.1

### 7.2 Ranh giới của luật R8 — chuỗi tiếng Việt được phép ở ĐÚNG một chỗ

Luật R8 ([`../RULES.md`](../RULES.md) §5) cấm hardcode
câu hiển thị trong BE; catalog §7.1 lại **bắt buộc** mang câu tiếng Việt. Ranh giới để cả hai cùng đúng:

> **Chuỗi tiếng Việt chỉ được phép ở tham số `MessageTemplate` của một `Error` khai trong
> catalog. Mọi vị trí khác trong BE là vi phạm.**

| Vị trí | Được? | Vì sao |
| --- | --- | --- |
| `MessageTemplate` của `Error` trong catalog | ✅ | Đây là **câu dự phòng**, không phải nguồn hiển thị — lý do đầy đủ ở [`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §4.1 |
| `WithMessage("…")` của validator | 🛑 | Validator sinh `fieldErrors`, và mỗi mục ở đó mang **mã**; câu đến từ i18n phía FE |
| Chuỗi trả thẳng từ controller, handler, middleware | 🛑 | Bỏ qua catalog ⇒ không có mã ⇒ FE không có gì để tra |
| Thông điệp log, tên biến, comment | ✅ | Không hiển thị cho người dùng; R8 nói về **câu hiển thị** |
| Chuỗi trong exception của hạ tầng ta không sở hữu | ✅ | Không ánh xạ ra envelope; xem §7.1 |

Detector của R8 nhận diện theo **hình dạng** — chuỗi tiếng Việt ở tham số thứ hai của `new Error(...)` trong
file catalog thì cho qua, mọi vị trí khác thì bắt; **không** quét toàn bộ rồi miễn trừ theo tên file.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §7.2

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

1. **Duy nhất toàn hệ.** Luật R3 ([`../RULES.md`](../RULES.md) §5) phủ cả hai vế.
2. **Mã là hợp đồng công khai, đổi mã là breaking change.** Đổi câu chữ thì tự do; đổi mã thì phải
   qua contract.
3. **Mã mô tả nguyên nhân, không mô tả giao diện.** `CORE.USER.EMAIL_DUPLICATED` đúng;
   `CORE.USER.SHOW_RED_TOAST` sai.

### 7.4 Mã cho điều kiện phía client — định nghĩa gốc

Lỗi **BE không bao giờ phát ra** (mất mạng, tải chunk hỏng, proxy trả HTML) vẫn cần mã để FE tra bảng dịch.

> 🛑 **FE KHÔNG được tự chế mã.** Mã phía client khai ở **bảng dưới**, khuôn §7.3, **trước** khi code dùng.
> BE không mang hằng số cho chúng; tài nguyên `CLIENT` dành riêng cho bảng này (R3).

| Mã | Khi nào FE dùng |
| --- | --- |
| `CORE.CLIENT.NO_CONNECTION` | Không nhận được phản hồi nào (mất mạng, DNS hỏng, CORS chặn) |
| `CORE.CLIENT.SERVER_UNAVAILABLE` | Nhận 5xx **không mang envelope** (proxy trả 502/504) — thay câu mất kết nối |
| `CORE.CLIENT.RESOURCE_LOAD_FAILED` | Tải một tài nguyên của app thất bại (chunk sau khi triển khai bản mới) |
| `CORE.CLIENT.VALIDATION_REQUIRED` | Validator phía client: ô bắt buộc để trống |
| `CORE.CLIENT.VALIDATION_EMAIL` | Validator phía client: sai định dạng email |
| `CORE.CLIENT.VALIDATION_MAXLENGTH` | Validator phía client: vượt độ dài tối đa |
| `CORE.CLIENT.VALIDATION_PATTERN` | Validator phía client: không khớp khuôn dạng khai ở ô nhập |
| `CORE.CLIENT.VALIDATION_MISMATCH` | Validator phía client: ô nhập lại không khớp ô gốc |

**Validator phía client theo khuôn `CORE.CLIENT.VALIDATION_<VALIDATOR>`** — `<VALIDATOR>` là khoá lỗi
validator gắn vào control, viết HOA; `VALIDATION` nằm **trong** đoạn LÝ DO, nối bằng `_` (giữ đúng ba
đoạn của §7.3). Dùng thêm validator thì thêm dòng vào bảng trên **trước**. Khoá dịch:
[`fe-ui-conventions.md`](fe-ui-conventions.md) §5.3.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §7.4

### 7.5 Ranh giới với mã của `fieldErrors`

Hai hệ mã, hai field, **có chủ đích**:

| Field | Mã đến từ | Khuôn |
| --- | --- | --- |
| `error.code` | Catalog của feature | `MIỀN.TÀI_NGUYÊN.LÝ_DO` |
| `error.fieldErrors[<Field>][].code` | `WithErrorCode(...)` của validator, lấy từ cùng catalog — rỗng / độ dài / khuôn dạng dùng nhóm `CORE.VALIDATION.*` của §7.1 | Cùng khuôn |

Cái giá: mỗi rule validator phải khai `WithErrorCode` tường minh, không dùng được mã mặc định.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §7.5

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

`messageParams` nằm **cạnh mã mà nó tham số hoá** — cạnh `code` ở gốc, cạnh `code` trong từng phần
tử của `fieldErrors`, không gom về gốc envelope. Luật R7 và R8 ([`../RULES.md`](../RULES.md) §5).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §8.2

### 8.3 ⚠️ KHÔNG dùng `FluentValidation.Internal.MessageFormatter`

Namespace `Internal` không nằm trong hợp đồng public của thư viện. Bộ ráp câu tự viết:

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

Câu ráp ra đi vào field `message` (dev-facing). Client vẫn dựng câu của mình từ `code` +
`messageParams`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §8.3

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

**Không có `TotalPages`** — suy ra được từ `TotalCount` và `PageSize`. **Tên là `TotalCount`, không
phải `Total`.**

### 9.2 Envelope nhất quán

Endpoint danh sách trả **cùng envelope** với endpoint đơn: `Result<PagedList<UserDto>>`, không trả
`PagedList<T>` trần — FE viết **một** hàm bóc envelope.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §9

### 9.3 Quy ước paging / sort / filter

> 📖 Tên, kiểu, miền giá trị, mặc định, allowlist `sortBy` và luật sắp xếp có tiêu chí phụ ổn định
> của tham số danh sách trên dây: đọc [`../contracts/README.md`](../contracts/README.md) §8.

### 9.4 `GET` hay `POST` cho danh sách

Mặc định `GET` + query string. `POST` với body chỉ khi bộ lọc **thật sự** phức tạp (nhiều nhóm điều
kiện lồng nhau, danh sách giá trị vượt giới hạn độ dài URL).

---

## 10. Command chạy lâu → job nền

### 10.1 Ngưỡng tách

Tách sang job nền khi **một** trong các điều sau đúng:

- Khối lượng xử lý **không có trần rõ ràng** (import/export file do người dùng cung cấp).
- Gọi ra ngoài mà latency **không kiểm soát được** (gửi email, gọi hệ thống bên thứ ba).
- Thời gian xử lý kỳ vọng vượt ngưỡng timeout của tầng proxy/gateway.

Command CRUD thường (tạo/sửa một bản ghi) **không** áp dụng.

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

Handler (`IJobRepository jobs`, `IFileStorage storage`) làm **trọn** use case:
`storage.SaveTempAsync` → `Job.Create(...)` (entity ghi nhận domain event `JobQueuedEvent`) →
`jobs.AddAsync` → trả `Id`. `Job` ánh xạ `core.job`
([`../database/schema-core.md`](../database/schema-core.md) §9.8); `Id` là `jobId` của
[`../contracts/jobs.md`](../contracts/jobs.md) §1. ✅ CÓ THẬT (đối chiếu 2026-09-25):
`src/BE/Core/CoreAndSkill.Core.Application/Import/StartImportCommandHandler.cs`
(chuỗi `Job.Create(definition.Type, callerId, tempKey)`). Năm ràng buộc:

1. **Không trả HTTP 202.** 200 + envelope như mọi endpoint; `data` mang `jobId`.
2. **Enqueue đi qua seam** — không gọi thẳng API thư viện job nền, không đẩy lời enqueue lên controller.
3. **Handler KHÔNG gọi `IBackgroundJobScheduler` — nó ghi một dòng outbox.** Bộ chặn ở tầng dữ liệu
   chuyển domain event thành bản ghi outbox trong **cùng** transaction, kèm đơn vị và người kích hoạt
   ([`be-architecture.md`](be-architecture.md) §1.1); tiến trình phát nền gọi scheduler sau commit. Gọi scheduler
   từ request thì nó ném — cùng mục đó của `be-architecture.md`.
   Cơ chế, bảng, đảm bảo *ít nhất một lần*:
   [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §2,
   [`../database/schema-core.md`](../database/schema-core.md) §8.
4. **File upload không sống sót qua ranh giới request → job.** Ghi storage tạm **trước** khi ghi dòng
   outbox (cố ý ngoài ràng buộc 3); job đọc lại từ đó. Không truyền `Stream`/`IFormFile` vào job.
5. **Job worker không có `HttpContext`** — tự tạo scope DI bằng `IServiceScopeFactory`; đơn vị và người
   kích hoạt đi theo dòng outbox ([`be-architecture.md`](be-architecture.md) §1.1, mục *Danh tính và
   đơn vị khi không có request*).

Kết quả ghi vào chính bản ghi job (`status`, `progress`, `result`, `error`, `result_file_id`); FE poll
`GET /api/v1/core/jobs/{id}` ([`../contracts/jobs.md`](../contracts/jobs.md)). Không dùng WebSocket.

> 📖 Lý do, bẫy, ví dụ mở rộng (gồm khối handler đầy đủ): [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §10.3

---

## 11. Trên dây là DTO, không bao giờ là entity — định nghĩa gốc

> **Handler trả DTO. Entity không đi ra khỏi tầng ứng dụng.**

EF Core ánh xạ bảng thành entity; DTO là hình dạng đi trên dây. Chỗ chúng gặp nhau là câu truy vấn.

### 11.1 Vì sao không trả entity ra API

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §11.1

### 11.2 Cách làm: truy vấn thẳng vào DTO

Phép chiếu **ngay trong câu truy vấn** — không "nạp entity rồi chép sang DTO" —
[`be-performance.md`](be-performance.md) §3.

### 11.3 Không thêm thư viện ánh xạ tự động

Viết phép chiếu bằng tay. Không dùng thư viện ánh xạ tự động.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §11.3
