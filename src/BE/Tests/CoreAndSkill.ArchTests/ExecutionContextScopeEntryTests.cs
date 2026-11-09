using System.Reflection;
using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using CoreAndSkill.Core.Application.Common.Interfaces;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// A12 — docs/RULES-BE.md §3: chỉ nơi gọi thuộc allowlist (docs/quy-uoc/be-architecture.md, đoạn mốc `**Allowlist A12 —`)
// được mở phạm vi ngữ cảnh thực thi bằng `IExecutionContextScope.Enter`. Mở phạm vi sai chỗ là gán đơn vị và người
// dùng cho mã không có quyền quyết định chúng; mọi đường ghi sau đó mang danh tính đó mà không gì báo.
//
// Phép dò, điểm mù và khoá allowlist: đầu Support/ExecutionContextScopeEntryScanner.cs.
//
// T12:cap-xanh-do — mọi ca Detector_A12_* kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng phép dò, dựng từ CÙNG khuôn
// nguồn, khác đúng một chỗ (vị trí, số đối số, hay chỗ đặt câu chứa `Enter`). Ca xanh còn khẳng định phép dò đã THẤY
// lời gọi, để "xanh vì được phép" không lẫn với "xanh vì không thấy gì".
public class ExecutionContextScopeEntryTests
{
    private static readonly IReadOnlySet<(string Type, string Member)> SyntheticAllowlist
        = new HashSet<(string, string)> { ("Allowed", "Open") };

    // ===== Luật thật =====

    [Fact]
    public void ExecutionContextScope_IsEntered_OnlyByAllowedCallers()
    {
        var violations = ExecutionContextScopeEntryScanner.Violations(
            RealUses(), ExecutionContextScopeEntryScanner.Allowlist);

        violations.ShouldBeEmpty(
            "Luật A12: chỉ cặp (kiểu, thành viên) ở bảng Allowlist A12 được gọi IExecutionContextScope.Enter. "
          + "Nơi gọi mới cần allowlist thì đó là quyết định nới luật — hỏi architect, không thêm vào cổng.");
    }

    // ===== T6 =====

    [Fact]
    public void ExecutionContextScope_IsEntered_OnlyByAllowedCallers_ScansRealCalls()
    {
        var uses = RealUses();

        uses.ShouldNotBeEmpty();

        // Biến nhận không tên `executionContextScope`: cổng không được khoá theo tên biến.
        uses.ShouldContain(u => u.Type == "CoreWebServiceCollectionExtensions"
            && u.Member == "ValidatePrincipalAsync"
            && u.Kind == EnterUseKind.Call
            && u.Receiver != "executionContextScope");

        // Lời gọi nằm trong biểu thức `? :`.
        uses.ShouldContain(u => u.Type == "BackgroundJobQueueHostedService"
            && u.Member == "RunAsync"
            && u.Kind == EnterUseKind.Call
            && u.InConditional);
    }

    [Fact]
    public void EveryA12AllowlistEntry_HasARealEnterCall()
        => ExecutionContextScopeEntryScanner.DeadEntries(RealUses(), ExecutionContextScopeEntryScanner.Allowlist)
            .ShouldBeEmpty();

    [Fact]
    public void EveryA12AllowlistType_IsDeclaredExactlyOnce()
        => ExecutionContextScopeEntryScanner.TypeDeclarationProblems(
                RealTypeDeclarations(), ExecutionContextScopeEntryScanner.Allowlist)
            .ShouldBeEmpty();

    [Fact]
    public void A12Allowlist_MatchesTheDocumentTable_BothWays()
        => ExecutionContextScopeEntryScanner.CompareToDocument(
                ExecutionContextScopeEntryScanner.ReadDocTable(DocPath()),
                ExecutionContextScopeEntryScanner.Allowlist)
            .ShouldBeEmpty();

    // Cổng nhận ra `Enter` bằng tên và số đối số. Đổi chữ ký của nó (thêm/bớt tham số, đổi tên) mà cổng không đổi theo thì
    // cổng không còn bắt gì và vẫn xanh — nên chữ ký là thứ phải được ghim.
    [Fact]
    public void IExecutionContextScope_DeclaresExactlyOneEnter_WithThreeParameters()
    {
        var enters = typeof(IExecutionContextScope)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(m => m.Name == "Enter")
            .ToList();

        enters.Count.ShouldBe(1);
        enters[0].GetParameters().Length.ShouldBe(3);
    }

    // ===== Detector_A12_* — phép dò =====

    [Theory]
    [InlineData("s")]
    [InlineData("f")]
    [InlineData("scopeFactory")]
    public void Detector_A12_Red_AnyReceiverName_InRogueMember(string receiver)
        => Check($"class Rogue {{ void M(IScope {receiver}) {{ using var x = {receiver}.Enter(a, b, c); }} }}")
            .ShouldHaveSingleItem().ShouldContain("Rogue.M");

    [Theory]
    [InlineData("s")]
    [InlineData("f")]
    [InlineData("scopeFactory")]
    public void Detector_A12_Green_AnyReceiverName_InAllowedMember(string receiver)
        => CheckGreen($"class Allowed {{ void Open(IScope {receiver}) {{ using var x = {receiver}.Enter(a, b, c); }} }}");

    [Fact]
    public void Detector_A12_Red_ConditionalAccess_InRogueMember()
        => Check("class Rogue { void M(IScope s) { using var x = s?.Enter(a, b, c); } }").ShouldHaveSingleItem();

    [Fact]
    public void Detector_A12_Green_ConditionalAccess_InAllowedMember()
        => CheckGreen("class Allowed { void Open(IScope s) { using var x = s?.Enter(a, b, c); } }");

    [Fact]
    public void Detector_A12_Red_CallInsideTernary_InRogueMember()
        => Check("class Rogue { void M(IScope s, bool k) { using var x = k ? s.Enter(a, b, c) : null; } }")
            .ShouldHaveSingleItem();

    [Fact]
    public void Detector_A12_Green_CallInsideTernary_InAllowedMember()
        => CheckGreen("class Allowed { void Open(IScope s, bool k) { using var x = k ? s.Enter(a, b, c) : null; } }");

    [Fact]
    public void Detector_A12_Red_MethodGroup_InRogueMember()
        => Check("class Rogue { void M(IScope s) { Func<Guid, Guid?, string?, IDisposable> open = s.Enter; } }")
            .ShouldHaveSingleItem();

    [Fact]
    public void Detector_A12_Green_MethodGroup_InAllowedMember_IsSeenAsMethodGroup()
    {
        var uses = ExecutionContextScopeEntryScanner.Uses(
            "class Allowed { void Open(IScope s) { Func<Guid, Guid?, string?, IDisposable> open = s.Enter; } }", "fake.cs");

        uses.ShouldHaveSingleItem().Kind.ShouldBe(EnterUseKind.MethodGroup);
        ExecutionContextScopeEntryScanner.Violations(uses, SyntheticAllowlist).ShouldBeEmpty();
    }

    [Fact]
    public void Detector_A12_Red_ConditionalBindingMethodGroup_InRogueMember()
        => Check("class Rogue { void M(IScope s) { var open = s?.Enter; } }").ShouldHaveSingleItem();

    [Fact]
    public void Detector_A12_Red_LambdaInAllowedClassOtherMember()
        => Check("class Allowed { void Other(IScope s) { Action a = () => s.Enter(a, b, c); } }")
            .ShouldHaveSingleItem().ShouldContain("Allowed.Other");

    [Fact]
    public void Detector_A12_Green_LambdaCountsForEnclosingAllowedMember()
        => CheckGreen("class Allowed { void Open(IScope s) { Action a = () => s.Enter(a, b, c); } }");

    [Fact]
    public void Detector_A12_Red_LocalFunctionInAllowedClassOtherMember()
        => Check("class Allowed { void Other(IScope s) { void Local() { s.Enter(a, b, c); } Local(); } }")
            .ShouldHaveSingleItem().ShouldContain("Allowed.Other");

    [Fact]
    public void Detector_A12_Green_LocalFunctionCountsForEnclosingAllowedMember()
        => CheckGreen("class Allowed { void Open(IScope s) { void Local() { s.Enter(a, b, c); } Local(); } }");

    [Fact]
    public void Detector_A12_Green_OneArgument_IsNotAnEnterCall()
        => Check("class Rogue { void M(IScope s) { s.Enter(a); } }").ShouldBeEmpty();

    [Fact]
    public void Detector_A12_Red_ThreeArguments_IsAnEnterCall()
        => Check("class Rogue { void M(IScope s) { s.Enter(a, b, c); } }").ShouldHaveSingleItem();

    [Fact]
    public void Detector_A12_Green_TwoOrFourArguments_AreNotEnterCalls()
        => Check("class Rogue { void M(IScope s) { s.Enter(a, b); s.Enter(a, b, c, d); } }").ShouldBeEmpty();

    [Fact]
    public void Detector_A12_Green_DeclarationNamedEnter_IsNotACall()
        => Check("class Rogue { public IDisposable Enter(Guid a, Guid? b, string? c) => null!; }").ShouldBeEmpty();

    [Theory]
    [InlineData("this.Enter(a, b, c);")]
    [InlineData("Enter(a, b, c);")]
    public void Detector_A12_Red_DeclarationPlusSelfCall_IsAViolation(string call)
        => Check($"class Rogue {{ public IDisposable Enter(Guid a, Guid? b, string? c) => null!; void M() {{ {call} }} }}")
            .ShouldHaveSingleItem().ShouldContain("Rogue.M");

    [Fact]
    public void Detector_A12_Red_BaseEnterCall_IsAViolation()
        => Check("class Rogue : Parent { void M() { base.Enter(a, b, c); } }").ShouldHaveSingleItem();

    [Fact]
    public void Detector_A12_Green_CommentAndStringMentioningEnter_AreNotCalls()
        => Check("class Rogue { // s.Enter(a, b, c)\n /* s.Enter(a, b, c) */ "
               + "string t = \"s.Enter(a, b, c)\"; string u = $\"{1} s.Enter(a, b, c)\"; "
               + "string v = \"\"\"s.Enter(a, b, c)\"\"\"; }").ShouldBeEmpty();

    [Fact]
    public void Detector_A12_Red_SameCallAsRealCode_IsAViolation()
        => Check("class Rogue { void M(IScope s) { s.Enter(a, b, c); } }").ShouldHaveSingleItem();

    [Fact]
    public void Detector_A12_Red_TopLevelStatement_IsAViolation()
        => Check("IScope s = null!; s.Enter(a, b, c);").ShouldHaveSingleItem().ShouldContain("<top-level>");

    [Fact]
    public void Detector_A12_Red_NestedTypeIsKeyedOuterDotInner_NotBySimpleName()
        => Check("class Outer { class Allowed { void Open(IScope s) { s.Enter(a, b, c); } } }")
            .ShouldHaveSingleItem().ShouldContain("Outer.Allowed.Open");

    [Fact]
    public void Detector_A12_Red_ConstructorAndPropertyAccessor_AreKeyedAsCtorAndProperty()
    {
        var uses = ExecutionContextScopeEntryScanner.Uses(
            "class Allowed { Allowed(IScope s) { s.Enter(a, b, c); } "
          + "IDisposable P { get { return t.Enter(a, b, c); } } }", "fake.cs");

        uses.Select(u => $"{u.Type}.{u.Member}").ShouldBe(["Allowed..ctor", "Allowed.P"]);
        ExecutionContextScopeEntryScanner.Violations(uses, SyntheticAllowlist).Count.ShouldBe(2);
    }

    // ===== Detector_A12_* — bộ đọc bảng tài liệu =====

    private const string GoodTable =
        "intro\n\n**Allowlist A12 — nơi được gọi.** Mô tả.\n\n"
      + "| Kiểu | Thành viên | Vai trò |\n| --- | --- | --- |\n"
      + "| `Allowed` | `Open` | x |\n| `Other` | `Run` | y |\n\nhết";

    [Fact]
    public void Detector_A12_Green_DocReader_ReadsWellFormedTable_StrippingBackticks()
        => ExecutionContextScopeEntryScanner.ParseDocTable(GoodTable, "fake.md")
            .ShouldBe([("Allowed", "Open"), ("Other", "Run")]);

    [Fact]
    public void Detector_A12_Red_DocReader_Throws_WhenMarkerIsGone()
        => Should.Throw<InvalidOperationException>(() =>
            ExecutionContextScopeEntryScanner.ParseDocTable(
                GoodTable.Replace("**Allowlist A12 —", "**Danh sách A12 —", StringComparison.Ordinal), "fake.md"));

    [Fact]
    public void Detector_A12_Red_DocReader_Throws_WhenMemberColumnIsRenamed()
        => Should.Throw<InvalidOperationException>(() =>
            ExecutionContextScopeEntryScanner.ParseDocTable(
                GoodTable.Replace("Thành viên", "Phương thức", StringComparison.Ordinal), "fake.md"));

    [Fact]
    public void Detector_A12_Red_DocReader_Throws_WhenTableHasNoRows()
        => Should.Throw<InvalidOperationException>(() =>
            ExecutionContextScopeEntryScanner.ParseDocTable(
                "**Allowlist A12 — x**\n\n| Kiểu | Thành viên | Vai trò |\n| --- | --- | --- |\n\nhết", "fake.md"));

    // ===== Detector_A12_* — phép so hai chiều =====

    [Fact]
    public void Detector_A12_Green_Compare_Matching_ReportsNothing()
        => ExecutionContextScopeEntryScanner.CompareToDocument(
                [("A", "x"), ("B", "y")], new HashSet<(string, string)> { ("B", "y"), ("A", "x") })
            .ShouldBeEmpty();

    [Fact]
    public void Detector_A12_Red_Compare_ExtraInDocument_IsReportedAsDocOnly()
        => ExecutionContextScopeEntryScanner.CompareToDocument(
                [("A", "x"), ("B", "y")], new HashSet<(string, string)> { ("A", "x") })
            .ShouldHaveSingleItem().ShouldContain("Chỉ có ở TÀI LIỆU");

    [Fact]
    public void Detector_A12_Red_Compare_ExtraInGate_IsReportedAsGateOnly()
        => ExecutionContextScopeEntryScanner.CompareToDocument(
                [("A", "x")], new HashSet<(string, string)> { ("A", "x"), ("B", "y") })
            .ShouldHaveSingleItem().ShouldContain("Chỉ có ở CỔNG");

    // ===== Detector_A12_* — mục chết và trùng tên kiểu =====

    [Fact]
    public void Detector_A12_Green_DeadEntry_WhenAllowlistedPairHasARealCall()
        => ExecutionContextScopeEntryScanner.DeadEntries(
                ExecutionContextScopeEntryScanner.Uses(
                    "class Allowed { void Open(IScope s) { s.Enter(a, b, c); } }", "fake.cs"),
                SyntheticAllowlist)
            .ShouldBeEmpty();

    [Fact]
    public void Detector_A12_Red_DeadEntry_WhenAllowlistedPairHasNoCall()
        => ExecutionContextScopeEntryScanner.DeadEntries(
                ExecutionContextScopeEntryScanner.Uses(
                    "class Allowed { void Open(IScope s) { s.Enter(a); } }", "fake.cs"),
                SyntheticAllowlist)
            .ShouldHaveSingleItem().ShouldContain("Allowed.Open");

    [Fact]
    public void Detector_A12_Red_DeadEntry_WhenOnlyAMethodGroupExists()
        => ExecutionContextScopeEntryScanner.DeadEntries(
                ExecutionContextScopeEntryScanner.Uses(
                    "class Allowed { void Open(IScope s) { var f = s.Enter; } }", "fake.cs"),
                SyntheticAllowlist)
            .ShouldHaveSingleItem();

    [Fact]
    public void Detector_A12_Green_TypeDeclaredOnce_IsNotAProblem()
        => ExecutionContextScopeEntryScanner.TypeDeclarationProblems(
                ExecutionContextScopeEntryScanner.TypeDeclarations("class Allowed { }", "a.cs"),
                SyntheticAllowlist)
            .ShouldBeEmpty();

    [Fact]
    public void Detector_A12_Red_TypeDeclaredInTwoFiles_IsAProblem()
        => ExecutionContextScopeEntryScanner.TypeDeclarationProblems(
                [.. ExecutionContextScopeEntryScanner.TypeDeclarations("class Allowed { }", "a.cs"),
                 .. ExecutionContextScopeEntryScanner.TypeDeclarations("class Allowed { }", "b.cs")],
                SyntheticAllowlist)
            .ShouldHaveSingleItem().ShouldContain("2 lần");

    [Fact]
    public void Detector_A12_Red_TypeNeverDeclared_IsAProblem()
        => ExecutionContextScopeEntryScanner.TypeDeclarationProblems(
                ExecutionContextScopeEntryScanner.TypeDeclarations("class Elsewhere { }", "a.cs"),
                SyntheticAllowlist)
            .ShouldHaveSingleItem().ShouldContain("0 lần");

    // ===== Trợ giúp =====

    private static List<string> Check(string source)
        => [.. ExecutionContextScopeEntryScanner.Violations(
            ExecutionContextScopeEntryScanner.Uses(source, "fake.cs"), SyntheticAllowlist)];

    // Ca xanh phải THẤY ít nhất một lần dùng `Enter` — nếu không "xanh" chỉ có nghĩa là phép dò mù.
    private static void CheckGreen(string source)
    {
        var uses = ExecutionContextScopeEntryScanner.Uses(source, "fake.cs");

        uses.ShouldNotBeEmpty("Ca xanh mà phép dò không thấy lời dùng `Enter` nào là xanh rỗng.");
        ExecutionContextScopeEntryScanner.Violations(uses, SyntheticAllowlist).ShouldBeEmpty();
    }

    private static IReadOnlyList<EnterUse> RealUses()
        => [.. ProductSourceFiles.Core()
            .SelectMany(file => ExecutionContextScopeEntryScanner.Uses(
                File.ReadAllText(file), ProductSourceFiles.RelativePath(file)))];

    private static IReadOnlyList<TypeDeclarationKey> RealTypeDeclarations()
        => [.. ProductSourceFiles.Core()
            .SelectMany(file => ExecutionContextScopeEntryScanner.TypeDeclarations(
                File.ReadAllText(file), ProductSourceFiles.RelativePath(file)))];

    private static string DocPath([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(here)!, "..", "..", "..", "..", "docs", "quy-uoc", "be-architecture.md"));
}
