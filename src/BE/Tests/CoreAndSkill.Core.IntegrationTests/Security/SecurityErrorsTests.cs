using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Web.Security;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Security;

// docs/quy-uoc/be-api-controller.md §7.4 — mã mà handler cũng phát khai MỘT lần ở CommonErrors, SecurityErrors trỏ tới (luật
// R3). Bằng nhau về GIÁ TRỊ là chưa đủ: Error là record, nên hai khai báo chép tay vẫn "bằng nhau" cho tới ngày một bên đổi
// câu chữ. Phép so là cùng THỰC THỂ.
public sealed class SecurityErrorsTests
{
    [Fact]
    public void Forbidden_IsTheSameInstanceAsCommonErrorsForbidden()
        => ReferenceEquals(SecurityErrors.Forbidden, CommonErrors.Forbidden).ShouldBeTrue(
            "CORE.AUTH.FORBIDDEN phải khai một lần ở CommonErrors — SecurityErrors.Forbidden trỏ tới, không khai lại");
}
