using CoreAndSkill.Core.Application.ClientErrors;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.ClientErrors;

public class ReportClientErrorCommandValidatorTests
{
    private static ReportClientErrorCommand ValidCommand() => new(
        "TypeError", "Cannot read properties of undefined", "at main.js:1:1", "/phieu/danh-sach",
        "c1807b11710f43c1964ec2988f532e56", "2026.09.10-a1b2c3d");

    [Fact]
    public void Valid_IsValid()
        => new ReportClientErrorCommandValidator().Validate(ValidCommand()).IsValid.ShouldBeTrue();

    [Fact]
    public void MissingStack_StillValid()
        => new ReportClientErrorCommandValidator().Validate(ValidCommand() with { Stack = null }).IsValid.ShouldBeTrue();

    [Fact]
    public void MissingTraceId_StillValid()
        => new ReportClientErrorCommandValidator().Validate(ValidCommand() with { TraceId = null }).IsValid.ShouldBeTrue();

    [Fact]
    public void EmptyKind_IsInvalid()
        => new ReportClientErrorCommandValidator().Validate(ValidCommand() with { Kind = "" }).IsValid.ShouldBeFalse();

    [Fact]
    public void EmptyMessage_IsInvalid()
        => new ReportClientErrorCommandValidator().Validate(ValidCommand() with { Message = "" }).IsValid.ShouldBeFalse();

    [Fact]
    public void EmptyDuongDan_IsInvalid()
        => new ReportClientErrorCommandValidator().Validate(ValidCommand() with { DuongDan = "" }).IsValid.ShouldBeFalse();

    [Fact]
    public void EmptyPhienBanApp_IsInvalid()
        => new ReportClientErrorCommandValidator().Validate(ValidCommand() with { PhienBanApp = "" }).IsValid.ShouldBeFalse();

    [Fact]
    public void MessageTooLong_IsInvalid_NotTruncated()
        => new ReportClientErrorCommandValidator().Validate(ValidCommand() with { Message = new string('a', 501) })
            .IsValid.ShouldBeFalse();

    [Fact]
    public void StackTooLong_IsInvalid()
        => new ReportClientErrorCommandValidator().Validate(ValidCommand() with { Stack = new string('a', 4001) })
            .IsValid.ShouldBeFalse();

    [Fact]
    public void KindTooLong_IsInvalid()
        => new ReportClientErrorCommandValidator().Validate(ValidCommand() with { Kind = new string('a', 101) })
            .IsValid.ShouldBeFalse();
}
