using CoreAndSkill.ArchTests.Support;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Application.Users;
using FluentValidation;
using MediatR;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Luật B3 — docs/RULES.md §10: mọi record request (kể cả command — ExportUsersCommand) có property `SortBy` phải có một
// validator khai rule kiểm miền giá trị trên chính property đó. Điểm mù của phép dò nằm ở đầu
// Support/SortByAllowlistScanner.cs.
public class SortByAllowlistTests
{
    [Fact]
    public void EveryRequestWithSortBy_HasAnAllowlistRule()
    {
        var offenders = SortByAllowlistScanner.FindRequestsWithoutSortByAllowlist(ArchitectureFixture.ApplicationAssembly);

        offenders.ShouldBeEmpty(
            "SortBy không có allowlist: giá trị do client gửi đi thẳng vào câu sắp xếp — tốt nhất là lỗi lúc chạy, tệ nhất là injection");
    }

    // T6 — tập đầu vào khác rỗng VÀ chạm đúng bốn request thật đang có SortBy (ba query danh sách + một command xuất).
    [Fact]
    public void EveryRequestWithSortBy_ScansTheRealRequests()
    {
        var requests = SortByAllowlistScanner.RequestsWithSortBy(ArchitectureFixture.ApplicationAssembly);

        requests.ShouldContain(typeof(GetUsersListQuery));
        requests.ShouldContain(typeof(GetRolesListQuery));
        requests.ShouldContain(typeof(GetTenantsListQuery));
        requests.ShouldContain(typeof(ExportUsersCommand), "command cũng phải nằm trong tầm — xuất nhận cùng tham số với danh sách");
    }

    [Fact]
    public void Detector_B3_Catches_ARequestWhoseValidatorIgnoresSortBy()
    {
        var offenders = SortByAllowlistScanner.FindRequestsWithoutSortByAllowlist(typeof(FakeSortableQuery).Assembly);

        offenders.ShouldContain(o => o.StartsWith(typeof(FakeSortableQuery).FullName!, StringComparison.Ordinal));
    }

    // Rule có mặt nhưng KHÔNG kiểm miền giá trị (chỉ giới hạn độ dài) vẫn là vi phạm: độ dài không nói gì về tên cột.
    [Fact]
    public void Detector_B3_Catches_ASortByRuleThatOnlyChecksLength()
    {
        var offenders = SortByAllowlistScanner.FindRequestsWithoutSortByAllowlist(typeof(FakeSortableQuery).Assembly);

        offenders.ShouldContain(o => o.StartsWith(typeof(FakeLengthOnlyQuery).FullName!, StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_B3_Ignores_ARequestWithAnAllowlistRule()
    {
        var offenders = SortByAllowlistScanner.FindRequestsWithoutSortByAllowlist(typeof(FakeSortableQuery).Assembly);

        offenders.ShouldNotContain(o => o.StartsWith(typeof(FakeAllowlistedQuery).FullName!, StringComparison.Ordinal));
    }

    // ---- Mẫu đối chứng, chỉ sống trong assembly test -------------------------------------------

    public sealed record FakeSortableQuery(string? SortBy) : IRequest<string>;

    public sealed class FakeSortableQueryValidator : AbstractValidator<FakeSortableQuery>
    {
        public FakeSortableQueryValidator() => RuleFor(x => x.SortBy).NotNull();
    }

    public sealed record FakeLengthOnlyQuery(string? SortBy) : IRequest<string>;

    public sealed class FakeLengthOnlyQueryValidator : AbstractValidator<FakeLengthOnlyQuery>
    {
        public FakeLengthOnlyQueryValidator() => RuleFor(x => x.SortBy).MaximumLength(32);
    }

    public sealed record FakeAllowlistedQuery(string? SortBy) : IRequest<string>;

    public sealed class FakeAllowlistedQueryValidator : AbstractValidator<FakeAllowlistedQuery>
    {
        private static readonly string[] Allowlist = ["userName", "createdAt"];

        public FakeAllowlistedQueryValidator()
            => RuleFor(x => x.SortBy).Must(sortBy => sortBy is null || Allowlist.Contains(sortBy));
    }
}
