using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Files;

// Catalog lỗi của invariant StoredFile — cùng project với entity gọi nó.
public static class FileDomainErrors
{
    public static readonly Error AlreadyAttached = new(
        "CORE.FILE.ALREADY_ATTACHED", "Tệp đã gắn vào một bản ghi khác.", ErrorType.BusinessRule);
}
