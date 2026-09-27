using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Sàn thời gian của mọi phản hồi đăng nhập trượt (trừ 429) — định nghĩa gốc của giá trị mặc định:
// docs/wiki-core/be/02-identity-auth.md §4.2; lý do: docs/adr/0058-dang-nhap-truot-cho-du-mot-san-thoi-gian.md.
// Phép băm giả chỉ san phần băm; các nhánh trượt vẫn khác nhau ở phần truy vấn (nhánh sai mật khẩu ghi thêm một lần
// sai, nhánh đơn vị không có thì không tra tài khoản). LoginCommandHandler giữ mọi phản hồi trượt lại cho tới mốc
// sàn, tính từ lúc handler bắt đầu. Giá trị PHẢI lớn hơn p99 của nhánh trượt chậm nhất đo trên máy thật — 500 ms
// chọn khi chưa đo.
public sealed class CoreIdentityLoginOptions
{
    public const string SectionName = "Core:Identity:Login";

    // > 0 và không vượt 5000 (02-identity-auth.md §4.2): 0 là tắt sàn trong im lặng; quá 5 giây là treo người dùng thật.
    [Range(1, 5000)]
    public int FailureFloorMs { get; init; } = 500;
}
