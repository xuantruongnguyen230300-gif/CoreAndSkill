using FluentValidation;

namespace CoreAndSkill.Core.Application.Common;

// Rule "trần số phần tử" dùng chung cho Core và mọi module — docs/quy-uoc/be-cqrs-handler.md §7.1 hàng
// CORE.VALIDATION.MAX_ITEMS: `Must(x => x.Count <= n)` trên danh sách, gắn mã catalog và tham số MaxItems. Gom vào một chỗ
// vì tên tham số ("MaxItems") là hợp đồng với FE (docs/contracts/users.md §5, §7) và phải nằm trong allowlist của
// MessageParamPolicy — hai validator tự viết hai lambda là hai chỗ để gõ sai tên khoá mà không test nào của FE thấy.
//
// Danh sách null không phải việc của rule này (NotNull + Required gánh); null đi qua để hai rule không dẫm lên nhau.
public static class ListRuleBuilderExtensions
{
    public static IRuleBuilderOptions<T, IReadOnlyList<TItem>> MaximumItems<T, TItem>(
        this IRuleBuilder<T, IReadOnlyList<TItem>> rule, int maxItems)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxItems, 1);

        return rule
            .Must((_, list, context) =>
            {
                // AppendArgument TRƯỚC khi trả false — FormattedMessagePlaceholderValues chụp lại lúc dựng thất bại.
                context.MessageFormatter.AppendArgument("MaxItems", maxItems);
                return list is null || list.Count <= maxItems;
            })
            .WithErrorCode(CommonErrors.MaxItems.Code);
    }
}
