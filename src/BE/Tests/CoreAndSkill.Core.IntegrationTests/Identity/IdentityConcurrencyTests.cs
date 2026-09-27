using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// IdentityConcurrency.DetectConflict là hàm THUẦN — chỉ đọc IdentityResult.Errors, không chạm DB —
// nên kiểm bằng test trực tiếp, không cần Postgres/Testcontainers/WebApplicationFactory. Đặt ở đây
// (không phải Core.UnitTests) vì Core.UnitTests chỉ ràng buộc Domain + Application
// (docs/quy-uoc/be-architecture.md §9.1); IdentityConcurrency nằm ở Core.Infrastructure, và project
// này là project test DUY NHẤT đã có đường tới đó (qua CoreAndSkill.Core.Web).
//
// Vì sao hàm này tồn tại và trả cùng CORE.CONCURRENCY.CONFLICT với IExceptionHandler:
// docs/wiki-core/be/06-concurrency-control.md §6.1.
public class IdentityConcurrencyTests
{
    [Fact]
    public void DetectConflict_ConcurrencyFailureCode_ReturnsConcurrencyConflict()
    {
        var identityResult = IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" });

        var error = IdentityConcurrency.DetectConflict(identityResult);

        error.ShouldBe(CommonErrors.ConcurrencyConflict);
    }

    [Fact]
    public void DetectConflict_OtherFailureCode_ReturnsNull()
    {
        var identityResult = IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch" });

        var error = IdentityConcurrency.DetectConflict(identityResult);

        error.ShouldBeNull();
    }

    [Fact]
    public void DetectConflict_Success_ReturnsNull()
    {
        var error = IdentityConcurrency.DetectConflict(IdentityResult.Success);

        error.ShouldBeNull();
    }
}
