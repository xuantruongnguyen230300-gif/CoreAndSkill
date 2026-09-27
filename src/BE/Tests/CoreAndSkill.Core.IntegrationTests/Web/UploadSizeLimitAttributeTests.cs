using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// docs/wiki-core/be/14-file-storage.md §7. Ba lớp giới hạn của UploadSizeLimitAttribute, kiểm TRỰC TIẾP trên các
// tính năng của HttpContext. Test HTTP (B4FilesEndpointTests.Upload_*) kiểm lớp Content-Length và lớp handler; hai
// lớp còn lại — trần thân của Kestrel và trần multipart — chỉ TestServer không ép thật được, nên phải kiểm ở đây
// rằng attribute ĐẶT đúng giá trị. Kestrel có THỰC SỰ ngắt luồng khi vượt trần hay không là hành vi của framework,
// KHÔNG được chứng minh ở repo này (cần host Kestrel thật + client gửi chunked).
public class UploadSizeLimitAttributeTests
{
    private const long OneMb = 1024 * 1024;
    private const long MultipartOverhead = 64 * 1024;

    private sealed class FakeBodySizeFeature(bool readOnly = false) : IHttpMaxRequestBodySizeFeature
    {
        public bool IsReadOnly => readOnly;

        public long? MaxRequestBodySize { get; set; } = 30_000_000;
    }

    private static (ResourceExecutingContext Context, FakeBodySizeFeature Feature) NewContext(long? contentLength, bool readOnly = false, int maxMb = 1)
    {
        var services = new ServiceCollection()
            .AddSingleton<IOptions<CoreFileOptions>>(Options.Create(new CoreFileOptions { RootPath = "x", MaxUploadMb = maxMb }))
            .BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.ContentLength = contentLength;
        var feature = new FakeBodySizeFeature(readOnly);
        http.Features.Set<IHttpMaxRequestBodySizeFeature>(feature);

        var context = new ResourceExecutingContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            new List<IValueProviderFactory>());
        return (context, feature);
    }

    [Fact]
    public void ADeclaredLengthFarOverTheLimit_IsRefusedImmediately_WithoutTouchingTheBody()
    {
        var (context, feature) = NewContext(contentLength: 50 * OneMb);

        new UploadSizeLimitAttribute().OnResourceExecuting(context);

        var result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(400);
        result.Value.ShouldBeOfType<ApiEnvelope<object>>().Error!.Code.ShouldBe("CORE.FILE.TOO_LARGE");
        feature.MaxRequestBodySize.ShouldBe(30_000_000, "từ chối sớm — chưa đụng tới trần thân");
    }

    [Fact]
    public void ADeclaredLengthWithinLimitPlusMultipartOverhead_PassesOn_ForTheHandlerToJudgeTheFile()
    {
        var (context, _) = NewContext(contentLength: OneMb + 1000);

        new UploadSizeLimitAttribute().OnResourceExecuting(context);

        context.Result.ShouldBeNull();
    }

    [Fact]
    public void TheKestrelBodyLimit_IsSetToTheLimitPlusTheMultipartOverhead()
    {
        var (context, feature) = NewContext(contentLength: 100);

        new UploadSizeLimitAttribute().OnResourceExecuting(context);

        feature.MaxRequestBodySize.ShouldBe(OneMb + MultipartOverhead);
    }

    [Fact]
    public void AChunkedBodyWithNoDeclaredLength_StillGetsTheKestrelLimit_BecauseNothingWasDeclaredToTrust()
    {
        var (context, feature) = NewContext(contentLength: null);

        new UploadSizeLimitAttribute().OnResourceExecuting(context);

        context.Result.ShouldBeNull();
        feature.MaxRequestBodySize.ShouldBe(OneMb + MultipartOverhead);
    }

    [Fact]
    public void ALargerConfiguredLimit_IsHonoured()
    {
        var (context, feature) = NewContext(contentLength: 100, maxMb: 20);

        new UploadSizeLimitAttribute().OnResourceExecuting(context);

        feature.MaxRequestBodySize.ShouldBe((20 * OneMb) + MultipartOverhead);
    }

    [Fact]
    public void AReadOnlyBodyLimitFeature_IsLeftAlone_NotAnError()
    {
        var (context, feature) = NewContext(contentLength: 100, readOnly: true);

        Should.NotThrow(() => new UploadSizeLimitAttribute().OnResourceExecuting(context));

        feature.MaxRequestBodySize.ShouldBe(30_000_000);
    }

    [Fact]
    public void TheFormFeature_IsReplaced_SoTheMultipartLimitIsOurs_NotTheFrameworkDefault()
    {
        var (context, _) = NewContext(contentLength: 100);
        var before = context.HttpContext.Features.Get<IFormFeature>();

        new UploadSizeLimitAttribute().OnResourceExecuting(context);

        var after = context.HttpContext.Features.Get<IFormFeature>();
        after.ShouldNotBeNull();
        after.ShouldNotBeSameAs(before);
    }

    [Fact]
    public void TheMultipartLimit_IsNotBelowTheBodyLimit_SoAFileJustOverTheLimitReachesTheHandlerAsTooLarge()
    {
        // Hồi quy của lỗi đã gặp: đặt trần multipart bằng đúng trần TỆP làm parser ném InvalidDataException trong lúc
        // đọc form, và người gọi nhận problem+json của framework thay vì CORE.FILE.TOO_LARGE. Bằng chứng ở mức HTTP:
        // B4FilesEndpointTests.Upload_JustOverTheLimit_StillInsideTheMultipartOverhead_*. Ở đây khoá bằng mã nguồn:
        // attribute phải để lọt một thân lớn hơn trần tệp một chút.
        var (context, _) = NewContext(contentLength: OneMb + 100);

        new UploadSizeLimitAttribute().OnResourceExecuting(context);

        context.Result.ShouldBeNull();
    }

    [Fact]
    public void TheAttribute_IsOnlyValidOnAMethod_ItIsNotInheritedByAControllerByAccident()
    {
        var usage = typeof(UploadSizeLimitAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.ShouldBe(AttributeTargets.Method);
        usage.AllowMultiple.ShouldBeFalse();
    }
}
