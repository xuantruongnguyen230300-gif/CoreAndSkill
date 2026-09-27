using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// docs/quy-uoc/be-api-controller.md §2.4, docs/quy-uoc/be-architecture.md §2.1. Lỗi model binding phải ra ĐÚNG envelope
// với một mã nghiệp vụ, không phải `application/problem+json` của framework kèm thông điệp exception nội bộ. Test HTTP
// (B4FilesEndpointTests) đã chứng minh hai đường thật; ở đây là phép chuẩn hoá khoá trường — phần logic thuần.
public class ModelBindingProblemFactoryTests
{
    private static (ObjectResult Result, ApiEnvelope<object> Envelope) Run(params string[] invalidKeys)
    {
        var context = new ActionContext(new DefaultHttpContext { TraceIdentifier = "trace-1" }, new RouteData(), new ActionDescriptor());
        foreach (var key in invalidKeys)
            context.ModelState.AddModelError(key, "thông điệp nội bộ của framework không được lộ ra");

        var result = ModelBindingProblemFactory.Create(context).ShouldBeOfType<ObjectResult>();
        return (result, result.Value.ShouldBeOfType<ApiEnvelope<object>>());
    }

    [Fact]
    public void TheResult_IsA400Envelope_WithTheGenericValidationCode()
    {
        var (result, envelope) = Run("Page");

        result.StatusCode.ShouldBe(400);
        envelope.Success.ShouldBeFalse();
        envelope.Error!.Code.ShouldBe("CORE.VALIDATION.FAILED");
        envelope.TraceId.ShouldBe("trace-1");
    }

    [Fact]
    public void ANamedField_GetsAFormatFieldError_KeyedByItsName()
    {
        var (_, envelope) = Run("Page");

        envelope.Error!.FieldErrors!["Page"].Single().Code.ShouldBe("CORE.VALIDATION.FORMAT");
    }

    [Fact]
    public void TheFrameworksMessage_NeverReachesTheWire()
    {
        var (_, envelope) = Run("Page", "");

        System.Text.Json.JsonSerializer.Serialize(envelope).ShouldNotContain("framework");
        System.Text.Json.JsonSerializer.Serialize(envelope).ShouldNotContain("nội bộ");
    }

    [Theory]
    [InlineData("")]
    [InlineData("$")]
    [InlineData("$.roleIds[0]")]
    public void AKeyThatNamesNoField_LeavesNoFieldErrors_OnlyTheGenericCode(string key)
    {
        var (_, envelope) = Run(key);

        envelope.Error!.Code.ShouldBe("CORE.VALIDATION.FAILED");
        envelope.Error.FieldErrors.ShouldBeNull();
    }

    [Theory]
    [InlineData("request.Email", "Email")]
    [InlineData("query.Filter.Status", "Status")]
    [InlineData("RoleIds[2]", "RoleIds")]
    [InlineData("Items[0].Name", "Name")]
    public void ANestedKey_UsesItsLastSegment_BecauseTheFrontEndLooksUpByFieldName(string key, string expected)
    {
        var (_, envelope) = Run(key);

        envelope.Error!.FieldErrors!.Keys.ShouldBe([expected]);
    }

    // V-02: lỗi gắn cho CHÍNH tham số body của action (tên biến C#) không phải trường DTO nào. Ở HTTP, cờ
    // SuppressImplicitRequiredAttributeForNonNullableReferenceTypes đã gỡ nguồn sinh chính của nó (ModelBindingEndpointTests);
    // test này giữ lớp lọc thứ hai cho ca [Required] tường minh hay một project tắt nullable.
    [Fact]
    public void AnErrorKeyedByTheBodyParameterName_IsNotAFieldError_ButAQueryParameterWithTheSameShapeIs()
    {
        var descriptor = new ActionDescriptor
        {
            Parameters =
            [
                new ParameterDescriptor
                {
                    Name = "body",
                    BindingInfo = new BindingInfo { BindingSource = BindingSource.Body },
                },
                new ParameterDescriptor
                {
                    Name = "page",
                    BindingInfo = new BindingInfo { BindingSource = BindingSource.Query },
                },
            ],
        };
        var context = new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor);
        context.ModelState.AddModelError("body", "x");
        context.ModelState.AddModelError("$.name", "x");
        context.ModelState.AddModelError("page", "x");

        var envelope = ModelBindingProblemFactory.Create(context).ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ApiEnvelope<object>>();

        envelope.Error!.FieldErrors!.Keys.ShouldBe(["page"]);
    }

    [Fact]
    public void NothingInvalid_StillYieldsTheGenericCode_NotAnEmptySuccess()
    {
        var (result, envelope) = Run();

        result.StatusCode.ShouldBe(400);
        envelope.Error!.Code.ShouldBe("CORE.VALIDATION.FAILED");
    }
}
