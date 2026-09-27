using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Audit;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Menu;
using CoreAndSkill.Core.Domain.Permissions;
using CoreAndSkill.Core.Domain.Tenants;
using CoreAndSkill.Core.Infrastructure.Audit;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAndSkill.Core.Infrastructure.Tenants;

// Service tạo đơn vị DÙNG CHUNG — docs/adr/0023-dich-vu-tao-don-vi-dung-chung.md. Lệnh bootstrap
// (Core.Web runner) và endpoint tạo đơn vị của khu quản trị hệ thống (B3+) gọi CÙNG hiện thực này.
// Từ B3, service này còn gánh BA thao tác không-phải-tạo của khu quản trị hệ thống tác động lên một
// đơn vị nghiệp vụ — ngưng/bật lại, khôi phục quản trị, tạo quản trị bổ sung
// (docs/adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md §3, §5) — để KHÔNG thêm mục nào
// vào allowlist mở phạm vi ngữ cảnh (luật A12): service đã nằm trong allowlist đó.
//
// Service KHÔNG tự mở transaction — mỗi thao tác chạy trong transaction của NGƯỜI GỌI (ADR-0023 §1 "mỗi thao
// tác một transaction"; docs/quy-uoc/be-cqrs-handler.md §3.1, §5.3). Đường HTTP: TransactionBehavior bọc
// command. Đường runner: CoreCommandRunner bọc từng lời gọi bằng IUnitOfWork.ExecuteInTransactionAsync. Mở
// thêm một transaction ở đây là transaction LỒNG trên cùng kết nối Scoped — Npgsql từ chối, mọi lệnh ghi của
// /system/tenants ra 500. Gọi service ngoài transaction thì ném ngay (EnsureInsideTransaction): thiếu nó, từng
// lần lưu của UserManager tự commit riêng và một lần hỏng giữa chừng để lại đơn vị "đã tạo nhưng thiếu seed".
internal sealed class TenantProvisioningService(
    IExecutionContextScope executionContextScope,
    CoreDbContext db,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IEnumerable<ITenantSeedSource> seedSources,
    IEnumerable<IPermissionCatalogSource> catalogSources,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IClientAddressAccessor clientAddress,
    TimeProvider timeProvider,
    CrossTenantActorScope crossTenantActor,
    ILogger<TenantProvisioningService> logger)
    : ITenantProvisioningService
{
    public async Task<Result<TenantProvisioningResult>> CreateTenantAsync(CreateTenantInput input, CancellationToken ct)
    {
        EnsureInsideTransaction();

        // Đơn vị của người gọi TRƯỚC khi service mở phạm vi nào — quyết định dòng audit có xuyên đơn vị hay không
        // (docs/contracts/tenants.md §5: tạo đơn vị bởi tài khoản vận hành ghi HAI dòng; lệnh bootstrap không có ai gọi thì MỘT).
        var callerTenantId = tenantContext.TenantId;

        var normalizedCode = input.Code.Trim().ToUpperInvariant();

        // Quy tắc 3 (ADR-0023 §3): chạy lại KHÔNG nhân đôi — nhận ra đơn vị hệ thống bằng
        // IsSystem, đơn vị nghiệp vụ bằng Code.
        var existingTenant = input.IsSystem
            ? await db.Tenants.FirstOrDefaultAsync(t => t.IsSystem, ct)
            : await db.Tenants.FirstOrDefaultAsync(t => t.Code == normalizedCode, ct);

        // Endpoint POST /system/tenants (docs/contracts/tenants.md §2): đơn vị đã có là CONFLICT
        // tường minh, không phải một no-op im lặng — khác hẳn ngữ nghĩa của core bootstrap.
        if (existingTenant is not null && input.FailIfExists)
            return Result.Failure<TenantProvisioningResult>(
                TenantProvisioningErrors.CodeDuplicate.WithParams(("Code", input.Code)));

        bool tenantWasCreated;
        Tenant tenant;

        if (existingTenant is not null)
        {
            tenant = existingTenant;
            tenantWasCreated = false;
        }
        else
        {
            // Đơn vị sắp dựng ⇒ seed sắp được ghi (ApplySeedAsync bên dưới chạy đúng khi tenantWasCreated). Kiểm nguồn seed
            // ở ĐÂY, trước dòng ghi đầu tiên của thao tác — không dựa vào TenantSeedValidationHostedService: lệnh
            // `core bootstrap` không khởi động hosted service nào (chú thích đầu TenantSeedValidator). Nguồn sai thì không
            // dòng nào được dàn dựng, và người gọi rollback một transaction rỗng.
            try
            {
                TenantSeedValidator.Validate(seedSources, catalogSources);
            }
            catch (TenantSeedException ex)
            {
                logger.LogError(ex, "Nguồn seed đơn vị không hợp lệ — không ghi dòng nào cho đơn vị '{TenantCode}'.", normalizedCode);
                return Result.Failure<TenantProvisioningResult>(TenantProvisioningErrors.SeedFailed);
            }

            var createResult = Tenant.Create(input.Code, input.Name, input.IsSystem);
            if (createResult.IsFailure)
                return Result.Failure<TenantProvisioningResult>(createResult.Error!);

            tenant = createResult.Value;
            db.Tenants.Add(tenant);
            tenantWasCreated = true;
        }

        // Mở phạm vi ngữ cảnh thực thi của đơn vị vừa tạo/đã có — allowlist luật A12. Từ đây
        // mọi ghi ITenantScoped (AppUser, AppRole, RolePermission, MenuItem…) được interceptor
        // gán đúng TenantId.
        using var scope = EnterTenantScope(tenant.Id);

        var existingUser = await userManager.FindByNameAsync(input.AdminUserName);
        bool adminWasCreated;
        Guid adminUserId;

        if (existingUser is not null)
        {
            adminUserId = existingUser.Id;
            adminWasCreated = false;
        }
        else
        {
            var newUser = AppUser.NewAccount(
                input.AdminUserName, input.AdminEmail, input.AdminFullName ?? input.AdminUserName,
                hasPermissionBypass: input.AdminHasPermissionBypass, isSystemOperator: input.AdminIsSystemOperator);

            var createUserResult = await userManager.CreateAsync(newUser, input.AdminPassword);
            if (!createUserResult.Succeeded)
                return Result.Failure<TenantProvisioningResult>(
                    MapFirstAdminCreateErrors(createUserResult.Errors, newUser, userManager.Options.Password));

            adminUserId = newUser.Id;
            adminWasCreated = true;
        }

        // Seed chỉ chạy khi đơn vị VỪA tạo — đơn vị đã có coi như đã seed từ lần chạy trước.
        if (tenantWasCreated)
        {
            try
            {
                await ApplySeedAsync(ct);
            }
            catch (TenantSeedException ex)
            {
                // Nguồn ITenantSeedSource không nhất quán — phần không cần database đã bị TenantSeedValidator chặn ở đầu
                // thao tác, nhưng phần phụ thuộc DATABASE thì phép kiểm đó không thấy: khoá
                // quyền có trong danh mục C# mà chưa có dòng trong core.permission, vai trò Identity từ chối. Đó là lỗi
                // NGHIỆP VỤ báo lại được người vận hành (docs/contracts/tenants.md §2 CORE.TENANT.SEED_FAILED) — không phải
                // 500: rollback sạch, không đơn vị nào "đã tạo nhưng thiếu seed" (ADR-0023 §1). Chỉ bắt ĐÚNG lỗi seed:
                // InvalidOperationException của EF hay của một nguồn seed viết sai là lỗi hệ thống, đi ra thành 500.
                logger.LogError(ex, "Seed đơn vị '{TenantCode}' thất bại giữa chừng — rollback toàn bộ.", tenant.Code);
                return Result.Failure<TenantProvisioningResult>(TenantProvisioningErrors.SeedFailed);
            }
        }

        if (tenantWasCreated)
        {
            await WriteCrossTenantAuditAsync(
                callerTenantId, tenant.Id, AuditActionCodes.TenantCreate, "core.tenant", tenant.Id.ToString(), tenant.Name, ct);
        }

        return Result.Success(new TenantProvisioningResult(tenant.Id, adminUserId, tenantWasCreated, adminWasCreated));
    }

    public async Task<Result> ResetOperatorPasswordAsync(string operatorUserName, string newPassword, CancellationToken ct)
    {
        EnsureInsideTransaction();

        var systemTenant = await db.Tenants.FirstOrDefaultAsync(t => t.IsSystem, ct);
        if (systemTenant is null)
            return Result.Failure(TenantProvisioningErrors.OperatorNotFound);

        using var scope = EnterTenantScope(systemTenant.Id);

        var user = await userManager.FindByNameAsync(operatorUserName);
        if (user is null || !user.IsSystemOperator)
            return Result.Failure(TenantProvisioningErrors.OperatorNotFound);

        // Gỡ mật khẩu cũ rồi đặt mật khẩu mới — không cần biết mật khẩu cũ, đúng ngữ nghĩa "khôi phục", không phải "tự
        // đổi". Mỗi lệnh ghi dừng ngay khi hỏng — xem ResetPasswordWritesAsync.
        return await ResetPasswordWritesAsync(user, newPassword)
            ? Result.Success()
            : Result.Failure(TenantProvisioningErrors.OperatorPasswordResetFailed);
    }

    public async Task<Result> SetTenantActiveAsync(Guid tenantId, bool isActive, CancellationToken ct)
    {
        EnsureInsideTransaction();

        // Vốn của người gọi TRƯỚC khi service mở phạm vi nào — quyết định dòng audit có xuyên
        // đơn vị hay không (docs/contracts/tenants.md §5).
        var callerTenantId = tenantContext.TenantId;

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
            return Result.Failure(TenantProvisioningErrors.NotFound);

        var mutateResult = isActive ? tenant.Activate() : tenant.Deactivate();
        if (mutateResult.IsFailure)
            return mutateResult;

        await db.SaveChangesAsync(ct); // Tenant KHÔNG ITenantScoped — không cần phạm vi để ghi.

        await WriteCrossTenantAuditAsync(
            callerTenantId, tenant.Id,
            isActive ? AuditActionCodes.TenantActivate : AuditActionCodes.TenantDeactivate,
            "core.tenant", tenant.Id.ToString(), tenant.Name, ct);

        return Result.Success();
    }

    public async Task<Result> RecoveryResetAdminPasswordAsync(Guid tenantId, string userName, string tempPassword, CancellationToken ct)
    {
        EnsureInsideTransaction();

        var callerTenantId = tenantContext.TenantId;

        // Đơn vị hệ thống KHÔNG phải đích — khôi phục tài khoản vận hành chỉ đi lệnh chạy tay
        // (docs/contracts/tenants.md §4 "Ghi chú"). Cùng mã với "không có": không xác nhận đích tồn tại.
        var tenant = await FindBusinessTenantAsync(tenantId, ct);
        if (tenant is null)
            return Result.Failure(TenantProvisioningErrors.NotFound);

        // docs/contracts/tenants.md §4 "Ghi chú": chính sách mật khẩu kiểm TRƯỚC khi tra tài
        // khoản đích, để RECOVERY_RESET_FAILED không nói gì về việc đích có tồn tại hay không —
        // nếu kiểm SAU (tức chỉ chạy được với đích hợp lệ), một mật khẩu yếu trên một userName
        // CÓ THẬT sẽ ra mã khác một mật khẩu yếu trên userName KHÔNG TỒN TẠI, và khác biệt đó
        // chính là phép dò tên đăng nhập mà mã gộp ở §4 tồn tại để chặn.
        var policyError = await ValidatePasswordPolicyAsync(tempPassword, TenantProvisioningErrors.RecoveryResetFailed);
        if (policyError is not null)
            return Result.Failure(policyError);

        Guid targetUserId;
        string targetDisplay;

        using (EnterTenantScope(tenant.Id))
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user is null)
                return Result.Failure(TenantProvisioningErrors.RecoveryTargetNotEligible);

            var eligible = user.HasPermissionBypass || await db.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(db.Roles.Where(r => r.IsSystem), ur => ur.RoleId, r => r.Id, (ur, r) => r.Id)
                .AnyAsync(ct);

            if (!eligible)
                return Result.Failure(TenantProvisioningErrors.RecoveryTargetNotEligible);

            // Hỏng ở lệnh ghi nào thì trả lỗi NGAY — không tới dòng nhật ký bên dưới, và người gọi rollback.
            if (!await ResetPasswordWritesAsync(user, tempPassword))
                return Result.Failure(TenantProvisioningErrors.RecoveryResetFailed);

            targetUserId = user.Id;
            targetDisplay = user.FullName;
        }

        await WriteCrossTenantAuditAsync(
            callerTenantId, tenant.Id, AuditActionCodes.TenantAdminRecoveryReset,
            "core.user", targetUserId.ToString(), targetDisplay, ct);

        return Result.Success();
    }

    public async Task<Result<Guid>> CreateAdditionalAdminAsync(
        Guid tenantId, string userName, string email, string fullName, string tempPassword, CancellationToken ct)
    {
        EnsureInsideTransaction();

        var callerTenantId = tenantContext.TenantId;

        // Đơn vị hệ thống KHÔNG phải đích (docs/contracts/tenants.md §6): một tài khoản mang cờ bypass trong đơn vị
        // hệ thống đứng cùng đơn vị với tài khoản vận hành — và đặt lại được mật khẩu của nó qua /users.
        var tenant = await FindBusinessTenantAsync(tenantId, ct);
        if (tenant is null)
            return Result.Failure<Guid>(TenantProvisioningErrors.NotFound);

        // docs/contracts/tenants.md §6 "Ghi chú": chính sách mật khẩu kiểm TRƯỚC khi gọi UserManager, trên tài khoản
        // thăm dò — fieldErrors["TempPassword"] vì thế không phụ thuộc gì vào tên đăng nhập đích.
        var policyError = await ValidatePasswordPolicyAsync(tempPassword, TenantProvisioningErrors.AdminCreateFailed);
        if (policyError is not null)
            return Result.Failure<Guid>(policyError);

        Guid newUserId;
        using (var scope = EnterTenantScope(tenant.Id))
        {
            // Luật M12 — cờ chỉ sinh CÙNG LÚC với tài khoản mang nó.
            var newUser = AppUser.NewAccount(userName, email, fullName, hasPermissionBypass: true);

            var createResult = await userManager.CreateAsync(newUser, tempPassword);
            // docs/contracts/tenants.md §6 "Ghi chú" — MỌI ca gộp một mã (chống dò
            // userName trong đơn vị); CHỈ lý do chính sách mật khẩu lộ ra fieldErrors.
            if (!createResult.Succeeded)
                return Result.Failure<Guid>(WithPasswordOnlyFieldErrors(
                    TenantProvisioningErrors.AdminCreateFailed, createResult.Errors, userManager.Options.Password));

            newUserId = newUser.Id;
        }

        await WriteCrossTenantAuditAsync(
            callerTenantId, tenant.Id, AuditActionCodes.TenantAdminCreate,
            "core.user", newUserId.ToString(), fullName, ct);

        return Result.Success(newUserId);
    }

    // Mở phạm vi của một đơn vị (allowlist luật A12) và GIỮ NGUYÊN danh tính người gọi: phạm vi thay cả đơn vị lẫn
    // người mà ICurrentUser trả, nên truyền null ở đây là biến việc của người vận hành thành việc của `system` trên mọi
    // dòng tài khoản ghi bên trong — created_by/updated_by (AuditInterceptor) và nhật ký core.user.* (AuditLogInterceptor).
    // docs/contracts/tenants.md §5: nhật ký mang danh tính tài khoản vận hành; chỉ khi KHÔNG có người (lệnh bootstrap)
    // mới ghi `system` (docs/quy-uoc/be-architecture.md §1.1). Đối số đọc TRƯỚC khi Enter đổi phạm vi.
    //
    // Luật M14 (schema-core.md §9.4 điểm 4): người gọi đứng ở đơn vị KHÁC đơn vị sắp mở thì đánh dấu đơn vị của người gọi
    // vào CrossTenantActorScope — hai đường ghi nhật ký (AuditLogInterceptor, EfAuditTrail) đọc dấu đó để điền
    // actor_tenant_id cho mọi dòng sinh ra bên trong phạm vi. Đơn vị người gọi đọc từ ITenantContext TRƯỚC khi Enter, tức là
    // claim của phiếu (hoặc null ở lệnh bootstrap — không có ai để mà xuyên đơn vị).
    private IDisposable EnterTenantScope(Guid tenantId)
    {
        var callerTenantId = tenantContext.TenantId;
        var scope = executionContextScope.Enter(tenantId, currentUser.UserId, currentUser.UserName);

        if (callerTenantId is not { } caller || caller == tenantId)
            return scope;

        return new CompositeScope(crossTenantActor.Begin(caller), scope);
    }

    // Đóng theo thứ tự ngược với mở: phạm vi ngữ cảnh trước, dấu xuyên đơn vị sau.
    private sealed class CompositeScope(IDisposable actorMark, IDisposable contextScope) : IDisposable
    {
        public void Dispose()
        {
            contextScope.Dispose();
            actorMark.Dispose();
        }
    }

    // Chuỗi ghi chung của hai đường đặt lại mật khẩu hộ (ResetOperatorPasswordAsync, RecoveryResetAdminPasswordAsync): gỡ
    // mật khẩu cũ, đặt mật khẩu mới, bật cờ phải đổi mật khẩu, đổi security stamp để mọi phiên đang mở trượt ở request kế
    // tiếp (ADR-0023 §5, ADR-0029 §3). Trả false ở lệnh ghi ĐẦU TIÊN hỏng, và không chạy lệnh ghi nào sau nó.
    //
    // Mỗi lệnh ghi của UserManager tự lưu, và UserStore đổi lỗi xung đột đồng thời thành một IdentityResult thất bại thay vì
    // ngoại lệ. Bỏ qua kết quả rồi lưu tiếp là đúng điều kiện kích hoạt (a) của ADR-0092: lượt lưu hỏng để lại trong bộ theo
    // dõi dòng nhật ký / outbox của một thay đổi không xảy ra, và lượt lưu sau mang chúng đi. Kể cả RemovePasswordAsync: nó
    // KHÔNG hỏng khi tài khoản chưa có mật khẩu (Identity không kiểm điều đó) — nó hỏng khi bộ kiểm người dùng từ chối hoặc
    // khi xung đột đồng thời, và cả hai ca đó đều phải dừng ở đây.
    private async Task<bool> ResetPasswordWritesAsync(AppUser user, string newPassword)
    {
        if (!(await userManager.RemovePasswordAsync(user)).Succeeded)
            return false;

        if (!(await userManager.AddPasswordAsync(user, newPassword)).Succeeded)
            return false;

        user.RequirePasswordChange();
        if (!(await userManager.UpdateAsync(user)).Succeeded)
            return false;

        return (await userManager.UpdateSecurityStampAsync(user)).Succeeded;
    }

    private Task<Tenant?> FindBusinessTenantAsync(Guid tenantId, CancellationToken ct)
        => db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsSystem, ct);

    // Chặn lời gọi ngoài transaction của người gọi — xem chú thích đầu lớp. Lỗi LẬP TRÌNH, không phải lỗi nghiệp
    // vụ, nên ném thay vì trả Result: không người dùng nào sửa được nó.
    private void EnsureInsideTransaction()
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "TenantProvisioningService phải chạy trong transaction của người gọi — TransactionBehavior (command) " +
                "hoặc IUnitOfWork.ExecuteInTransactionAsync (runner). Xem docs/adr/0023-dich-vu-tao-don-vi-dung-chung.md §1.");
    }

    // docs/contracts/tenants.md §5, ADR-0029 §4 — thao tác của tài khoản vận hành tác động lên một
    // đơn vị khác chính nó ghi HAI dòng: một ở đơn vị hệ thống (nhật ký của người vận hành), một ở
    // đơn vị đích (đánh dấu actorTenantId khác null — dấu hiệu xuyên đơn vị). Không xuyên đơn vị
    // (callerTenantId null hoặc trùng đích — ví dụ core bootstrap không có operator) thì MỘT dòng.
    // AuditLog KHÔNG implement ITenantScoped (docs/database/schema-core.md §9.4, AuditLog.cs) nên
    // TenantId của mỗi dòng nhận trực tiếp qua tham số — không cần mở phạm vi ngữ cảnh nào.
    private async Task WriteCrossTenantAuditAsync(
        Guid? callerTenantId, Guid targetTenantId, string actionCode, string targetType, string targetId,
        string? targetDisplay, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var actorUserId = currentUser.UserId;
        var actorDisplay = currentUser.UserName ?? SystemActor.UserName;
        var traceId = TraceId();
        var ip = clientAddress.RemoteIpAddress;

        if (callerTenantId is { } caller && caller != targetTenantId)
        {
            db.AuditLogs.Add(AuditLog.Record(
                caller, now, actorUserId, actorDisplay, actionCode, targetType, targetId, targetDisplay,
                actorTenantId: null, ipAddress: ip, traceId: traceId).Value);
            db.AuditLogs.Add(AuditLog.Record(
                targetTenantId, now, actorUserId, actorDisplay, actionCode, targetType, targetId, targetDisplay,
                actorTenantId: caller, ipAddress: ip, traceId: traceId).Value);
        }
        else
        {
            db.AuditLogs.Add(AuditLog.Record(
                targetTenantId, now, actorUserId, actorDisplay, actionCode, targetType, targetId, targetDisplay,
                actorTenantId: null, ipAddress: ip, traceId: traceId).Value);
        }

        await db.SaveChangesAsync(ct);
    }

    private static string? TraceId() => System.Diagnostics.Activity.Current?.TraceId.ToHexString();

    // Kiểm chính sách mật khẩu ĐỘC LẬP với một tài khoản cụ thể — chạy các PasswordValidators của
    // Identity trên một AppUser "thăm dò", KHÔNG lưu. Các validator mặc định (độ dài, chữ hoa/thường,
    // số, ký tự đặc biệt) không đọc dữ liệu của user, nên kết quả đúng cho MỌI user — cho phép kiểm
    // chính sách TRƯỚC khi biết đích có tồn tại hay không (docs/contracts/tenants.md §4).
    private async Task<Error?> ValidatePasswordPolicyAsync(string password, Error rootError)
    {
        var probe = new AppUser();
        var errors = new List<IdentityError>();

        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, probe, password);
            if (!result.Succeeded)
                errors.AddRange(result.Errors);
        }

        return errors.Count == 0 ? null : WithPasswordOnlyFieldErrors(rootError, errors, userManager.Options.Password);
    }

    // docs/contracts/tenants.md §4, §6 — mã gộp MỌI nguyên nhân, nhưng fieldErrors CHỈ lộ lý do
    // chính sách mật khẩu (khoá TempPassword); lỗi liên quan userName/email bị BỎ QUA có chủ đích —
    // lộ ra là một phép dò tên đăng nhập trong đơn vị. Ánh xạ mã và tham số: IdentityErrorMapper.
    // Không đưa tài khoản vào ánh xạ: hai Subject mang giá trị tài khoản (UserName, Email) không bao giờ lộ ở đây.
    // internal — cho phép test thẳng logic ánh xạ (docs/quy-uoc/be-architecture.md §9.1,
    // InternalsVisibleTo khai cho IntegrationTests).
    internal static Error WithPasswordOnlyFieldErrors(Error rootError, IEnumerable<IdentityError> errors, PasswordOptions policy)
    {
        var fieldErrors = IdentityErrorMapper.ToFieldErrors(errors, rejectedUser: null, policy, subject =>
            subject == IdentityErrorMapper.Subject.NewPassword ? "TempPassword" : null);

        return fieldErrors.Count == 0 ? rootError : rootError.WithFieldErrors(fieldErrors);
    }

    // docs/contracts/tenants.md §2 — KHÁC §4/§6: tạo đơn vị mới không phải chống dò trong một tập
    // dữ liệu đã tồn tại (đơn vị vừa tạo, chưa ai dùng), nên lỗi userName/email được lộ tường minh.
    // Mã không quy được cho ô nào của card §2 thì không lộ. rejectedUser — tài khoản UserManager vừa từ chối — là nguồn
    // tham số UserName/Email của mã trùng lặp.
    internal static Error MapFirstAdminCreateErrors(IEnumerable<IdentityError> errors, AppUser rejectedUser, PasswordOptions policy)
        => TenantProvisioningErrors.AdminCreateFailed.WithFieldErrors(IdentityErrorMapper.ToFieldErrors(
            errors, rejectedUser, policy, subject => subject switch
            {
                IdentityErrorMapper.Subject.UserName => "AdminUserName",
                IdentityErrorMapper.Subject.Email => "AdminEmail",
                IdentityErrorMapper.Subject.NewPassword => "AdminTempPassword",
                _ => null,
            }));

    // docs/wiki-core/be/17-multi-tenant.md §11.4 — bốn thứ theo thứ tự: vai trò, ánh xạ quyền, (tài
    // khoản đã tạo ở trên), menu. Nhiều nguồn được GỘP — CoreTenantSeedSource của Core cùng nguồn của
    // từng module; một nguồn không khai vai trò/ánh xạ quyền thì phần đó không sinh dòng nào.
    private async Task ApplySeedAsync(CancellationToken ct)
    {
        var roles = seedSources.SelectMany(s => s.GetRoles()).ToList();
        var rolePermissions = seedSources.SelectMany(s => s.GetRolePermissions()).ToList();
        var menuItems = seedSources.SelectMany(s => s.GetMenuItems()).ToList();

        var roleIdsByName = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var seedRole in roles)
        {
            var role = AppRole.NewRole(seedRole.Name, seedRole.IsSystem);
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new TenantSeedException(
                    $"Không tạo được vai trò seed '{seedRole.Name}': {string.Join(", ", result.Errors.Select(e => e.Description))}");

            roleIdsByName[seedRole.Name] = role.Id;
        }

        if (rolePermissions.Count > 0)
        {
            var permissionCodes = rolePermissions.Select(rp => rp.PermissionCode).Distinct().ToList();
            var permissionIdsByCode = await db.Permissions
                .Where(p => permissionCodes.Contains(p.Code))
                .ToDictionaryAsync(p => p.Code, p => p.Id, ct);

            foreach (var seedRolePermission in rolePermissions)
            {
                if (!roleIdsByName.TryGetValue(seedRolePermission.RoleName, out var roleId))
                    throw new TenantSeedException(
                        $"Ánh xạ quyền trỏ tới vai trò '{seedRolePermission.RoleName}' không có trong seed roles — nguồn ITenantSeedSource không nhất quán.");

                if (!permissionIdsByCode.TryGetValue(seedRolePermission.PermissionCode, out var permissionId))
                    throw new TenantSeedException(
                        $"Ánh xạ quyền trỏ tới khoá '{seedRolePermission.PermissionCode}' không có trong danh mục core.permission — " +
                        "kiểm IPermissionCatalogSource đã gộp đủ chưa.");

                var createResult = RolePermission.Create(roleId, permissionId);
                db.RolePermissions.Add(createResult.Value);
            }
        }

        if (menuItems.Count > 0)
        {
            var permissionCodes = menuItems
                .Where(m => m.RequiredPermissionCode is not null)
                .Select(m => m.RequiredPermissionCode!)
                .Distinct()
                .ToList();

            var permissionIdsByCode = permissionCodes.Count == 0
                ? new Dictionary<string, Guid>(StringComparer.Ordinal)
                : await db.Permissions.Where(p => permissionCodes.Contains(p.Code)).ToDictionaryAsync(p => p.Code, p => p.Id, ct);

            // Chỉ mục CẤP MỘT vào bảng tra — cây menu đúng MỘT cấp (docs/database/schema-core.md §6.1 luật 1).
            // Ghi cả mục con vào đây thì một mục con trở thành cha hợp lệ cho mục kế tiếp, và seed dựng được cây ba cấp:
            // TenantSeedValidator đã chặn ở đầu CreateTenantAsync, còn dòng này giữ cho chính vòng GHI không dựng nổi
            // hình dạng đó dù phép kiểm kia có bị gỡ. Khoá tra là mã đã chuẩn hoá (MenuItem.NormalizeCode) — cùng khoá
            // TenantSeedValidator tra ParentCode: bộ kiểm nhận một ParentCode mà vòng ghi không nối được thì lỗi seed chỉ
            // dời từ lúc khởi động sang lần tạo đơn vị đầu tiên.
            var rootIdsByCode = new Dictionary<string, Guid>(StringComparer.Ordinal);

            // Cha trước, con sau — MenuItem.ParentId trỏ vào một MenuItem khác đã tồn tại.
            foreach (var seedMenuItem in menuItems.Where(m => m.ParentCode is null))
                rootIdsByCode[MenuItem.NormalizeCode(seedMenuItem.Code)] =
                    CreateAndTrackMenuItem(seedMenuItem, parentId: null, permissionIdsByCode);

            foreach (var seedMenuItem in menuItems.Where(m => m.ParentCode is not null))
            {
                if (!rootIdsByCode.TryGetValue(MenuItem.NormalizeCode(seedMenuItem.ParentCode!), out var parentId))
                    throw new TenantSeedException(
                        $"Mục menu '{seedMenuItem.Code}' trỏ ParentCode '{seedMenuItem.ParentCode}' không phải một mục CẤP MỘT trong cùng nguồn seed.");

                CreateAndTrackMenuItem(seedMenuItem, parentId, permissionIdsByCode);
            }
        }
    }

    private Guid CreateAndTrackMenuItem(
        SeedMenuItem seed,
        Guid? parentId,
        IReadOnlyDictionary<string, Guid> permissionIdsByCode)
    {
        Guid? requiredPermissionId = null;
        if (seed.RequiredPermissionCode is not null)
        {
            if (!permissionIdsByCode.TryGetValue(seed.RequiredPermissionCode, out var id))
                throw new TenantSeedException(
                    $"Mục menu '{seed.Code}' trỏ RequiredPermissionCode '{seed.RequiredPermissionCode}' không có trong danh mục core.permission.");

            requiredPermissionId = id;
        }

        var createResult = MenuItem.Create(
            seed.Code, seed.LabelKey, seed.Icon, seed.Route, parentId, seed.DisplayOrder, requiredPermissionId, seed.ModuleKey);

        db.MenuItems.Add(createResult.Value);
        return createResult.Value.Id;
    }
}
