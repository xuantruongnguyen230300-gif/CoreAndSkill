using CoreAndSkill.Core.Application.Roles;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Roles;

public class RoleValidatorsTests
{
    [Fact]
    public void CreateRoleCommandValidator_Valid_IsValid()
        => new CreateRoleCommandValidator().Validate(new CreateRoleCommand("Kế toán")).IsValid.ShouldBeTrue();

    [Fact]
    public void CreateRoleCommandValidator_EmptyName_IsInvalid()
        => new CreateRoleCommandValidator().Validate(new CreateRoleCommand("")).IsValid.ShouldBeFalse();

    [Fact]
    public void CreateRoleCommandValidator_NameTooLong_IsInvalid()
        => new CreateRoleCommandValidator().Validate(new CreateRoleCommand(new string('a', 257))).IsValid.ShouldBeFalse();

    [Fact]
    public void UpdateRoleCommandValidator_Valid_IsValid()
        => new UpdateRoleCommandValidator().Validate(new UpdateRoleCommand(Guid.NewGuid(), "Kế toán", "stamp")).IsValid.ShouldBeTrue();

    [Fact]
    public void UpdateRoleCommandValidator_EmptyName_IsInvalid()
        => new UpdateRoleCommandValidator().Validate(new UpdateRoleCommand(Guid.NewGuid(), "", "stamp")).IsValid.ShouldBeFalse();

    // Thiếu version KHÔNG phải lỗi validation: 06-concurrency-control.md §6.3 luật 3 — thiếu ⇒ 409 ở tầng ghi.
    [Fact]
    public void UpdateRoleCommandValidator_MissingVersion_IsStillValid()
        => new UpdateRoleCommandValidator().Validate(new UpdateRoleCommand(Guid.NewGuid(), "Kế toán", null)).IsValid.ShouldBeTrue();

    [Fact]
    public void GetRolesListQueryValidator_Defaults_IsValid()
        => new GetRolesListQueryValidator().Validate(new GetRolesListQuery()).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("name")]
    [InlineData("createdAt")]
    public void GetRolesListQueryValidator_AllowedSortBy_IsValid(string sortBy)
        => new GetRolesListQueryValidator().Validate(new GetRolesListQuery(SortBy: sortBy)).IsValid.ShouldBeTrue();

    [Fact]
    public void GetRolesListQueryValidator_SortByOutsideAllowlist_IsInvalid()
        => new GetRolesListQueryValidator().Validate(new GetRolesListQuery(SortBy: "id")).IsValid.ShouldBeFalse();

    [Fact]
    public void GetRolesListQueryValidator_PageBelowOne_IsInvalid()
        => new GetRolesListQueryValidator().Validate(new GetRolesListQuery(Page: 0)).IsValid.ShouldBeFalse();

    [Fact]
    public void GetRolesListQueryValidator_PageSizeTooLarge_IsInvalid()
        => new GetRolesListQueryValidator().Validate(new GetRolesListQuery(PageSize: 500)).IsValid.ShouldBeFalse();

    [Fact]
    public void GetRolesListQueryValidator_SearchTextTooLong_IsInvalid()
        => new GetRolesListQueryValidator().Validate(new GetRolesListQuery(SearchText: new string('a', 201))).IsValid.ShouldBeFalse();
}
