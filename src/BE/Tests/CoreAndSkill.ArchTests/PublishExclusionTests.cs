using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// S23 vế (e) — docs/RULES-BE.md hàng S23; docs/adr/0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md
// quyết định 3, mục 5: "appsettings.Development.json không đi vào gói publish. [...] Project host khai loại trừ tệp
// đó khỏi publish; một ArchTest canh khai báo đó." Lý do hiện hành (thiết kế DB dev chung của ADR-0098 đang HOÃN,
// hôm nay không khoá hay bản mã hoá nào tồn tại): docs/adr/0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md
// quyết định 6, gạch vế (e) — tệp mang hai mật khẩu bootstrap công khai; máy chủ lỡ chạy ở môi trường Development sẽ
// nạp chúng.
//
// VÌ SAO LÀ ARCHTEST, KHÔNG PHẢI REVIEW. Đây là dữ liệu của .csproj, không phải hành vi lúc chạy — cùng lý do A1
// (PackageReferenceTests): ArchUnitNET chỉ đọc metadata assembly đã build, không đọc tệp dự án. Detector tự đọc XML.
//
// Bộ dò và lý do từng nhánh, kể cả điểm mù MSBuild không evaluate được: Support/PublishExclusionScanner.cs.
//
// T12:cap-xanh-do — mọi ca Detector_S23e_* kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng phép dò, khác đúng một chỗ của
// khuôn csproj (docs/RULES.md §8 luật T12).
public class PublishExclusionTests
{
    private const string FixHint =
        "Luật S23 vế (e) (docs/RULES-BE.md hàng S23; ADR-0098 quyết định 3, mục 5; lý do hiện hành ở ADR-0110 quyết "
      + "định 6): appsettings.Development.json mang hai mật khẩu bootstrap công khai — máy chủ lỡ chạy ở môi trường "
      + "Development sẽ nạp chúng. Thêm vào CoreAndSkill.Api.csproj — đúng item Content, đúng thuộc tính Update "
      + "(không Include — đụng độ item Content ngầm của Web SDK, lỗi NETSDK1022):\n"
      + "  <ItemGroup>\n"
      + "    <Content Update=\"appsettings.Development.json\" CopyToPublishDirectory=\"Never\" />\n"
      + "  </ItemGroup>";

    [Fact]
    public void Host_MustExclude_AppsettingsDevelopmentJson_FromPublish()
    {
        var content = File.ReadAllText(HostCsprojPath());

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(content).ShouldBeTrue(FixHint);
    }

    // T6 — khẳng định tệp csproj của host tồn tại, đọc được, và mang nội dung thật của project host trước khi kết
    // luận vế (e): đọc nhầm một tệp rỗng hay tệp khác thì "không vi phạm" là câu nói về một đầu vào rỗng.
    [Fact]
    public void Host_MustExclude_AppsettingsDevelopmentJson_FromPublish_ReadsRealHostCsproj()
    {
        var path = HostCsprojPath();

        File.Exists(path).ShouldBeTrue($"Không tìm thấy {path} — ProductSourceFiles.HostDirectory() đang trỏ sai.");

        var content = File.ReadAllText(path);

        content.ShouldContain("Sdk=\"Microsoft.NET.Sdk.Web\"");
        content.ShouldContain("<ProjectReference");
    }

    // ===== Detector_* (T1) — mỗi ca đỏ là ca xanh cùng hình dạng CỘNG đúng một vi phạm =====

    [Fact]
    public void Detector_S23e_Catches_MissingExclusion()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <ProjectReference Include="..\Core\CoreAndSkill.Core.Web\CoreAndSkill.Core.Web.csproj" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    [Fact]
    public void Detector_S23e_Ignores_ValidExclusion_AsChildElement()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json">
                  <CopyToPublishDirectory>Never</CopyToPublishDirectory>
                </Content>
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeTrue();
    }

    // F1 (core-reviewer, vòng 1) — SỬA từ ca xanh sai thành ca đỏ, đối chiếu bằng `dotnet publish` thật (SDK
    // 10.0.400) trên `dotnet new web`: Web SDK gỡ appsettings*.json khỏi item None và đưa vào item Content ngầm
    // (CopyToPublishDirectory=PreserveNewest). Khai CopyToPublishDirectory=Never trên một <None> không chạm được
    // item Content ngầm đó — tệp vẫn publish. Trước bản sửa này detector báo "đã loại trừ" cho ca không loại trừ gì.
    [Fact]
    public void Detector_S23e_Catches_ExclusionDeclaredOnNoneItem_BecauseWebSdkIgnoresIt()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <None Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // F1 (core-reviewer, vòng 1) — SỬA từ ca xanh sai thành ca đỏ: `<Content Include="…">` đụng độ với item Content
    // ngầm mà Web SDK đã thêm cho appsettings*.json, làm `dotnet build`/`dotnet publish` lỗi NETSDK1022 (duplicate
    // items). Một .csproj thật mang dòng này không build được — không thể coi là "đã loại trừ hợp lệ".
    [Fact]
    public void Detector_S23e_Catches_ExclusionDeclaredViaInclude_BecauseItDuplicatesTheImplicitWebSdkItem()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Include="appsettings.Development.json" CopyToPublishDirectory="Never" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    [Theory]
    [InlineData("PreserveNewest")]
    [InlineData("Always")]
    [InlineData("")]
    public void Detector_S23e_Catches_WrongCopyToPublishDirectoryValue_AsAttribute(string value)
    {
        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="{value}" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // F2 (core-reviewer, vòng 1, T12) — cặp sinh đôi với Detector_S23e_Ignores_ValidExclusion_AsChildElement, khác
    // đúng một giá trị. Trước bản sửa này, đổi `IsNever(elementValue)` thành `elementValue is not null` khiến 16
    // test khi đó vẫn xanh hết — nhánh phần tử con không có ca đỏ nào canh giá trị.
    [Fact]
    public void Detector_S23e_Catches_WrongCopyToPublishDirectoryValue_AsChildElement()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json">
                  <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
                </Content>
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Production.json")]
    [InlineData("Appsettings.Development.json")] // sai hoa thường — Windows không phân biệt, Linux thì có (§12)
    public void Detector_S23e_Catches_ExclusionDeclaredForDifferentFile(string otherFile)
    {
        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="{otherFile}" CopyToPublishDirectory="Never" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // Near-miss: Remove gỡ item khỏi tập Content — không tự mang CopyToPublishDirectory. Trông giống loại trừ publish
    // nhưng không phải; nếu detector khớp cả Remove, một .csproj chỉ có dòng này vẫn báo "đã loại trừ" trong khi
    // hành vi publish thật không đổi.
    [Fact]
    public void Detector_S23e_Catches_MissingExclusion_WhenOnlyRemoveDeclared()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Remove="appsettings.Development.json" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // Near-miss: tên thuộc tính sai — CopyToOutputDirectory chỉ chi phối thư mục build (bin/), không chi phối
    // publish. Một .csproj chỉ khai dòng này vẫn chép tệp vào gói publish.
    [Fact]
    public void Detector_S23e_Catches_WrongProperty_CopyToOutputDirectoryInsteadOfPublish()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToOutputDirectory="Never" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // Near-miss: loại item không nhận CopyToPublishDirectory — MSBuild không publish theo item Compile.
    [Fact]
    public void Detector_S23e_Catches_ExclusionDeclaredOnWrongItemType()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Compile Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // ===== F3 (core-reviewer, vòng 1) — Condition và "khai báo sau thắng" =====

    // Condition trên chính item: publish với một cấu hình khác cấu hình mà Condition đúng thì Never không được áp,
    // và một .csproj tĩnh không có cách nào biết cấu hình nào sẽ chạy lúc publish thật.
    [Fact]
    public void Detector_S23e_Catches_ConditionalExclusion_OnItem()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never"
                         Condition="'$(Configuration)' == 'Debug'" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // Cặp sinh đôi của ca trên, khác đúng một chỗ: cùng item, KHÔNG Condition → true. Chứng minh chính Condition
    // là thứ làm ca trên đỏ, không phải hình dạng nào khác của XML. Đây cũng là ca xanh nền (dạng thuộc tính) —
    // ca xanh `…_ValidExclusion_AsAttribute` trùng đầu vào với ca này đã được gỡ.
    [Fact]
    public void Detector_S23e_Ignores_SameItem_WithoutCondition()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeTrue();
    }

    // Condition trên ItemGroup cha — cùng rủi ro như Condition trên item, chỉ khác chỗ đặt trong XML.
    [Fact]
    public void Detector_S23e_Catches_ConditionalExclusion_OnParentItemGroup()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup Condition="'$(Configuration)' == 'Debug'">
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // "Khai báo sau thắng": một dòng PreserveNewest đặt SAU dòng Never vô hiệu hoá dòng Never — MSBuild áp theo thứ
    // tự tài liệu. Trước bản sửa F3, detector chỉ cần MỘT item nào đó mang Never là xanh, bỏ qua thứ tự.
    [Fact]
    public void Detector_S23e_Catches_LastDeclarationWins_WhenLastDeclarationIsWrong()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="PreserveNewest" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // Cặp sinh đôi đảo thứ tự: PreserveNewest trước, Never đặt SAU cùng — khai báo cuối vẫn là Never nên phải đạt.
    // Chứng minh detector đọc đúng "khai báo CUỐI", không phải "khai báo ĐẦU" hay "khai báo bất kỳ".
    [Fact]
    public void Detector_S23e_Ignores_LastDeclarationWins_WhenLastDeclarationIsCorrect()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="PreserveNewest" />
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeTrue();
    }

    // ===== Review Core 2026-09-29/30 — sáu cách viết .csproj vẫn cho `dotnet publish` chép tệp. Mỗi ca ĐỎ đi cùng
    // một ca XANH sinh đôi (T12): cùng hình dạng, khác đúng một chỗ — chứng minh chính chỗ đó làm ca đỏ đỏ. =====

    // 1. <None Include=… CopyToPublishDirectory="Always"> thêm vào cạnh dòng Never — một item None cùng tên tệp vẫn
    // có thể đưa tệp vào gói publish.
    [Fact]
    public void Detector_S23e_Catches_NoneIncludeAlways_AddedBesideValidExclusion()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
                <None Include="appsettings.Development.json" CopyToPublishDirectory="Always" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    [Fact]
    public void Detector_S23e_Ignores_NoneIncludeAlways_ForUnrelatedFile()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
                <None Include="README.md" CopyToPublishDirectory="Always" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeTrue();
    }

    // Tên tệp khác hoa thường ở item khác: Windows coi là cùng tệp — chiều an toàn, không đạt.
    [Fact]
    public void Detector_S23e_Catches_NoneIncludeAlways_WithDifferentCasing()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
                <None Include="APPSETTINGS.DEVELOPMENT.JSON" CopyToPublishDirectory="Always" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // 2. <Content Remove=…> rồi <Content Include=… PreserveNewest> — gỡ item ngầm rồi thêm lại với PreserveNewest.
    [Fact]
    public void Detector_S23e_Catches_ContentRemoveThenIncludePreserveNewest()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
                <Content Remove="appsettings.Development.json" />
                <Content Include="appsettings.Development.json" CopyToPublishDirectory="PreserveNewest" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    [Fact]
    public void Detector_S23e_Ignores_ContentRemoveThenIncludePreserveNewest_ForUnrelatedFile()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
                <Content Remove="other.json" />
                <Content Include="other.json" CopyToPublishDirectory="PreserveNewest" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeTrue();
    }

    // 3. Dòng Never nằm dưới <Choose><When Condition=…> — Condition ở TỔ TIÊN xa hơn ItemGroup cha.
    [Fact]
    public void Detector_S23e_Catches_ExclusionUnderChooseWhen()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <Choose>
                <When Condition="'$(Configuration)' == 'Debug'">
                  <ItemGroup>
                    <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
                  </ItemGroup>
                </When>
              </Choose>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    [Fact]
    public void Detector_S23e_Catches_ExclusionUnderChooseOtherwise()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <Choose>
                <When Condition="'$(Configuration)' == 'Debug'">
                  <ItemGroup />
                </When>
                <Otherwise>
                  <ItemGroup>
                    <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
                  </ItemGroup>
                </Otherwise>
              </Choose>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // Sinh đôi: cùng một khối Choose/When nhưng dòng loại trừ đứng NGOÀI nó; trong Choose chỉ có item không liên quan.
    [Fact]
    public void Detector_S23e_Ignores_ExclusionOutsideChoose_WhenChooseHoldsUnrelatedItem()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <Choose>
                <When Condition="'$(Configuration)' == 'Debug'">
                  <ItemGroup>
                    <Content Update="other.json" CopyToPublishDirectory="Never" />
                  </ItemGroup>
                </When>
              </Choose>
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeTrue();
    }

    // 4. <Content Update="appsettings.*.json" PreserveNewest> sau dòng Never — glob khớp tên tệp, khai báo sau thắng.
    [Theory]
    [InlineData("appsettings.*.json")]
    [InlineData("appsettings*.json")]
    [InlineData("*.json")]
    [InlineData("**/*.json")]
    [InlineData("appsettings.Development.jso?")]
    [InlineData("appsettings**")]
    [InlineData("Appsettings.*.JSON")] // sai hoa thường — MSBuild trên Windows không phân biệt, chiều an toàn
    public void Detector_S23e_Catches_GlobUpdatePreserveNewest_AfterNever(string glob)
    {
        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
                <Content Update="{glob}" CopyToPublishDirectory="PreserveNewest" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // Sinh đôi: cùng hình dạng, glob không khớp tên tệp.
    [Theory]
    [InlineData("appsettings.Staging.*")]
    [InlineData("*.yaml")]
    [InlineData("config/*.json")]
    public void Detector_S23e_Ignores_GlobUpdatePreserveNewest_ThatCannotMatchTheFile(string glob)
    {
        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json" CopyToPublishDirectory="Never" />
                <Content Update="{glob}" CopyToPublishDirectory="PreserveNewest" />
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeTrue();
    }

    // 5. Phần tử con CopyToPublishDirectory mang Condition — publish Release vẫn chép tệp.
    [Fact]
    public void Detector_S23e_Catches_ConditionalChildMetadataElement()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json">
                  <CopyToPublishDirectory Condition="'$(Configuration)' == 'Debug'">Never</CopyToPublishDirectory>
                </Content>
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // Sinh đôi: Condition nằm trên một phần tử con KHÁC, không phải trên CopyToPublishDirectory.
    [Fact]
    public void Detector_S23e_Ignores_ConditionOnUnrelatedChildMetadataElement()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json">
                  <Link Condition="'$(Configuration)' == 'Debug'">x.json</Link>
                  <CopyToPublishDirectory>Never</CopyToPublishDirectory>
                </Content>
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeTrue();
    }

    // 6. Hai phần tử con cùng tên: Never rồi PreserveNewest — khai báo sau thắng.
    [Fact]
    public void Detector_S23e_Catches_TwoChildMetadataElements_NeverThenPreserveNewest()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json">
                  <CopyToPublishDirectory>Never</CopyToPublishDirectory>
                  <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
                </Content>
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeFalse();
    }

    // Sinh đôi đảo thứ tự: PreserveNewest rồi Never — phần tử cuối là Never.
    [Fact]
    public void Detector_S23e_Ignores_TwoChildMetadataElements_PreserveNewestThenNever()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <Content Update="appsettings.Development.json">
                  <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
                  <CopyToPublishDirectory>Never</CopyToPublishDirectory>
                </Content>
              </ItemGroup>
            </Project>
            """;

        PublishExclusionScanner.ExcludesAppsettingsDevelopmentFromPublish(csproj).ShouldBeTrue();
    }

    private static string HostCsprojPath()
        => Path.Combine(ProductSourceFiles.HostDirectory(), "CoreAndSkill.Api.csproj");
}
