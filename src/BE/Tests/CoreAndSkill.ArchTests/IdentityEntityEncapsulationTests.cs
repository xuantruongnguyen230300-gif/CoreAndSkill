using System.Reflection;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Infrastructure.Identity;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// docs/quy-uoc/be-entity-domain.md §1.4 (bảng luật dưới khối AppUser): public setter chỉ cho ĐÚNG bốn field của
// IAuditableEntity. Field kế thừa từ IdentityUser<Guid>/IdentityRole<Guid> nằm ngoài ranh giới; field do CHÍNH AppUser/AppRole
// khai thì nằm trong — gồm hai cờ đặc quyền mà luật M12 chỉ cho sinh CÙNG LÚC với tài khoản mang nó. Luật E1 quét hậu duệ
// BaseEntity nên không phủ hai kiểu này; test ở đây phủ.
//
// "Public setter" tính cả `init`: một accessor `init` công khai vẫn gán được từ ngoài lớp qua object initializer.
public class IdentityEntityEncapsulationTests
{
    // Hai cờ đặc quyền — docs/adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md, luật M12.
    [Fact]
    public void AppUser_PrivilegeFlags_HaveNoPublicSetter()
    {
        var offenders = PublicSetterScanner.Find(typeof(AppUser), [nameof(AppUser.HasPermissionBypass), nameof(AppUser.IsSystemOperator)]);

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void AppUserAndAppRole_SelfDeclaredFields_HaveNoPublicSetter()
    {
        var offenders = new[] { typeof(AppUser), typeof(AppRole) }
            .SelectMany(type => PublicSetterScanner.Find(type, PublicSetterScanner.SelfDeclaredFieldsOf(type)))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // T6 — tập xét không rỗng, và đúng là field "Ta thêm": nếu phép lọc loại nhầm hết thì test trên xanh vì không xét gì.
    [Fact]
    public void AppUserAndAppRole_SelfDeclaredFields_HaveNoPublicSetter_ScansTheRealFields()
    {
        PublicSetterScanner.SelfDeclaredFieldsOf(typeof(AppUser)).ShouldBe(
            ["FullName", "MustChangePassword", "PreferredLanguage", "IsSystemOperator", "HasPermissionBypass", "LockedByAdmin"],
            ignoreOrder: true);
        PublicSetterScanner.SelfDeclaredFieldsOf(typeof(AppRole)).ShouldBe(["Description", "IsSystem"], ignoreOrder: true);
    }

    [Fact]
    public void Detector_PublicSetter_Catches_PublicSetAndPublicInit()
    {
        PublicSetterScanner.Find(typeof(FakeOpenAccount), [nameof(FakeOpenAccount.Settable), nameof(FakeOpenAccount.Initable)])
            .Count.ShouldBe(2);
    }

    [Fact]
    public void Detector_PublicSetter_Ignores_PrivateSetAndGetOnly()
    {
        PublicSetterScanner.Find(typeof(FakeOpenAccount), [nameof(FakeOpenAccount.Guarded), nameof(FakeOpenAccount.ReadOnly)])
            .ShouldBeEmpty();
    }

    private sealed class FakeOpenAccount
    {
        public bool Settable { get; set; }
        public bool Initable { get; init; }
        public bool Guarded { get; private set; }
        public bool ReadOnly => Guarded;
    }

    private static class PublicSetterScanner
    {
        // Field "Ta thêm": khai trên CHÍNH kiểu, trừ bốn field audit (public setter có chủ đích) và TenantId (ITenantScoped —
        // gán qua bộ theo dõi của EF bằng TenantAssignmentInterceptor, cùng khuôn `init` của RolePermission).
        public static IReadOnlyList<string> SelfDeclaredFieldsOf(Type type)
        {
            var exempt = typeof(IAuditableEntity).GetProperties().Select(p => p.Name)
                .Append(nameof(ITenantScoped.TenantId))
                .ToHashSet(StringComparer.Ordinal);

            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(p => p.Name)
                .Where(name => !exempt.Contains(name))
                .ToList();
        }

        public static IReadOnlyList<string> Find(Type type, IEnumerable<string> propertyNames)
            => propertyNames
                .Select(name => type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new InvalidOperationException($"{type.Name} không còn thuộc tính '{name}' — cập nhật test."))
                .Where(property => property.SetMethod is { IsPublic: true })
                .Select(property => $"{type.Name}.{property.Name} có setter công khai")
                .ToList();
    }
}
