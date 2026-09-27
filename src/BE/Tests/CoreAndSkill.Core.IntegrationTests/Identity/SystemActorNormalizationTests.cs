using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// SystemActor.IsReservedUserName (Application — không tham chiếu Identity) lặp lại phép chuẩn hoá tên của Identity. Test này
// đối chiếu nó với ILookupNormalizer THẬT mà host đăng ký — bộ sinh NormalizedUserName, tức thứ index UserNameIndex so. Đổi bộ
// chuẩn hoá của host mà quên SystemActor thì hai phép lệch nhau và test đỏ: tên bị chặn ở validator phải là ĐÚNG những tên
// Identity coi là "system".
public sealed class SystemActorNormalizationTests
{
    private static readonly string[] Samples =
    [
        "system", "System", "SYSTEM", " system ", "\tsYsTeM\n",
        "system1", "systém", "sys tem", "vanhanh",
    ];

    [Fact]
    public void IsReservedUserName_AgreesWithTheHostIdentityNormalizer()
    {
        using var factory = new CoreWebApplicationFactory();
        var normalizer = factory.Services.GetRequiredService<ILookupNormalizer>();
        var systemNormalized = normalizer.NormalizeName(SystemActor.UserName);

        var disagreements = Samples
            .Where(sample => SystemActor.IsReservedUserName(sample)
                             != string.Equals(normalizer.NormalizeName(sample.Trim()), systemNormalized, StringComparison.Ordinal))
            .ToList();

        disagreements.ShouldBeEmpty();

        // Chống test rỗng: mẫu phải có cả hai phía — tên bị chặn và tên không bị chặn.
        Samples.Count(SystemActor.IsReservedUserName).ShouldBe(5);
    }
}
