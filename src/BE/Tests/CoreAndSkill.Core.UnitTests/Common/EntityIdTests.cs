using CoreAndSkill.Core.Domain.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Common;

public class EntityIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyGuid()
    {
        EntityId.New().ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void New_ReturnsVersion7Guid()
    {
        var id = EntityId.New();

        // UUID v7: ký tự đầu của nhóm thứ ba trong chuỗi "xxxxxxxx-xxxx-7xxx-..." là '7' —
        // RFC 9562 §5.7. Đọc qua ToString() để tránh bẫy thứ tự byte của Guid.ToByteArray()
        // (little-endian trên ba trường đầu, khác thứ tự mạng của chuẩn RFC).
        var text = id.ToString();
        var versionChar = text.Split('-')[2][0];

        versionChar.ShouldBe('7');
    }

    [Fact]
    public void New_CalledTwice_ReturnsDifferentValues()
    {
        EntityId.New().ShouldNotBe(EntityId.New());
    }
}
