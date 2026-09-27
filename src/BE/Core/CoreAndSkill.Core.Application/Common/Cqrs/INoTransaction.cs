namespace CoreAndSkill.Core.Application.Common.Cqrs;

// Marker RỖNG — lối thoát có tên duy nhất khỏi TransactionBehavior (docs/quy-uoc/be-cqrs-handler.md §5.3,
// docs/adr/0033-luong-dang-nhap-outcome-va-claim.md). Chỉ dùng cho lệnh mà tác dụng phụ PHẢI giữ lại khi
// Result thất bại — ca duy nhất ở B1: LoginCommand (bộ đếm sai mật khẩu, mốc khoá của Identity).
public interface INoTransaction;
