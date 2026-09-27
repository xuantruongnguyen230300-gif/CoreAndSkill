using CoreAndSkill.Core.Application.Permissions;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Permissions;

public class UpdatePermissionMatrixCommandValidatorTests
{
    private readonly UpdatePermissionMatrixCommandValidator _validator = new();

    [Fact]
    public void Validate_EntriesProvided_IsValid()
    {
        var result = _validator.Validate(new UpdatePermissionMatrixCommand("v1", []));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NullEntries_IsInvalid()
    {
        var result = _validator.Validate(new UpdatePermissionMatrixCommand("v1", null));

        result.IsValid.ShouldBeFalse();
    }

    // Không chặn ở đây thì handler chạm phần tử null ⇒ NullReferenceException ⇒ 500.
    [Fact]
    public void Validate_NullElement_IsInvalid_OnTheIndexedEntry()
    {
        var result = _validator.Validate(new UpdatePermissionMatrixCommand("v1", [null!]));

        result.Errors.Select(e => e.PropertyName).ShouldBe(["Entries[0]"]);
    }

    // entries[].roleIds là field bắt buộc (card §6) — null đi xuống service là NullReferenceException ⇒ 500.
    [Fact]
    public void Validate_NullRoleIds_IsInvalid_OnTheIndexedField()
    {
        var result = _validator.Validate(new UpdatePermissionMatrixCommand("v1", [new UpdatePermissionMatrixEntry(Guid.NewGuid(), null!)]));

        result.Errors.Select(e => e.PropertyName).ShouldBe(["Entries[0].RoleIds"]);
    }
}
