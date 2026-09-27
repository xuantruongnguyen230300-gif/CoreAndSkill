using CoreAndSkill.Core.Application.Users;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

public class GetUsersListQueryValidatorTests
{
    private readonly GetUsersListQueryValidator _validator = new();

    [Fact]
    public void Validate_Defaults_IsValid()
    {
        var result = _validator.Validate(new GetUsersListQuery());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("userName")]
    [InlineData("fullName")]
    [InlineData("email")]
    [InlineData("createdAt")]
    public void Validate_AllowedSortBy_IsValid(string sortBy)
    {
        var result = _validator.Validate(new GetUsersListQuery(SortBy: sortBy));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_SortByOutsideAllowlist_IsInvalid()
    {
        var result = _validator.Validate(new GetUsersListQuery(SortBy: "password"));

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("active")]
    [InlineData("locked")]
    public void Validate_AllowedStatus_IsValid(string status)
    {
        var result = _validator.Validate(new GetUsersListQuery(Status: status));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_StatusOutsideAllowlist_IsInvalid()
    {
        var result = _validator.Validate(new GetUsersListQuery(Status: "pending"));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_PageBelowOne_IsInvalid()
    {
        var result = _validator.Validate(new GetUsersListQuery(Page: 0));

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Validate_PageSizeOutOfRange_IsInvalid(int pageSize)
    {
        var result = _validator.Validate(new GetUsersListQuery(PageSize: pageSize));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_SearchTextTooLong_IsInvalid()
    {
        var result = _validator.Validate(new GetUsersListQuery(SearchText: new string('a', 201)));

        result.IsValid.ShouldBeFalse();
    }
}
