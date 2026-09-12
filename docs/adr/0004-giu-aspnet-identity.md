---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0004 — Giữ ASP.NET Core Identity, khoanh vùng trong `Core.Infrastructure`

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

Ở dự án tiền nhiệm, `AppUser` và `AppRole` kế thừa từ ASP.NET Core Identity và nằm trong `Core.Infrastructure`, không nằm trong Domain. Lớp entity cơ sở của Domain ghi rõ nó **không** áp dụng cho hai lớp này vì chúng tự quản vòng đời riêng.

Kết quả audit gọi đây là một vấn đề nghiêm trọng, với ba hệ quả: mọi dự án dùng Core buộc phải dùng ASP.NET Core Identity; đổi sang SSO hay LDAP phải sửa Core; và Domain không thể có luật nghiệp vụ nào về người dùng mà không đi qua Infrastructure.

Đồng thời, phần Identity đang chạy đã giải quyết những việc dễ làm sai: băm mật khẩu có tham số đúng thời, khoá tài khoản sau nhiều lần sai, token đặt lại mật khẩu có hạn dùng, xác nhận email, chuẩn hoá tên đăng nhập, tem bảo mật để vô hiệu hoá phiên khi đổi mật khẩu.

Câu hỏi là: đổi lấy sự thuần khiết của Domain có đáng không.

## Quyết định

**Giữ ASP.NET Core Identity**, và khoanh nó lại:

- `AppUser` và `AppRole` **chỉ được xuất hiện trong `Core.Infrastructure`**. Luật S5 ở [`../RULES.md`](../RULES.md) ép điều này bằng test kiến trúc.
- `Core.Application` chỉ thấy interface — `IIdentityService`, `ICurrentUser`, `IPermissionChecker` — và các DTO của riêng nó.
- `Core.Domain` **không có khái niệm người dùng như một entity**. Nơi nào cần biết "ai làm" thì dùng định danh (`Guid`), không dùng đối tượng người dùng.

Đây là **đánh đổi có ý thức**, không phải thiếu sót. Nó phải được ghi ở [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) đúng như vậy.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Domain sở hữu `User` riêng, Identity nằm sau một adapter

**Được:** Domain sở hữu khái niệm trung tâm nhất của nó; đổi nhà cung cấp danh tính không chạm Domain; luật nghiệp vụ về người dùng viết được ở đúng chỗ.

**Vì sao loại:** phải duy trì **hai bản của cùng một thực thể** — một trong Domain, một của Identity — cộng một lớp ánh xạ giữa chúng. Ba câu hỏi nảy sinh ngay và không có câu nào rẻ:

- Ai là nguồn sự thật cho email, cho trạng thái khoá, cho thời điểm đổi mật khẩu?
- Tạo người dùng là hai thao tác ghi ở hai nơi — nếu bước sau hỏng thì phía nào rollback?
- Truy vấn danh sách người dùng kèm bộ lọc theo trạng thái sẽ đọc bảng nào, và join thế nào khi hai bên có thể lệch?

Lớp ánh xạ này là code phải viết, phải test, và phải nhớ đồng bộ mỗi lần thêm một trường. Đổi lại, nó mua một khả năng — thay nhà cung cấp danh tính — mà theo bối cảnh hiện tại chưa có kế hoạch nào dùng tới.

### Phương án B — Tự quản hoàn toàn, bỏ ASP.NET Core Identity

**Được:** Domain sạch tuyệt đối, không phụ thuộc framework nào, kiểm soát toàn bộ hình dạng bảng.

**Vì sao loại:** phải tự viết lại băm mật khẩu, khoá tài khoản, token đặt lại có hạn, tem bảo mật, chuẩn hoá tên đăng nhập. Đây là nhóm chức năng mà **sai thì thành lỗ hổng bảo mật**, và cái sai không tự lộ ra — hệ vẫn chạy bình thường cho tới ngày bị khai thác. Đổi một rủi ro bảo mật lấy sự thuần khiết kiến trúc là đổi sai chiều.

## Hệ quả

### Tích cực

- **Không phải viết lại phần dễ sai nhất của xác thực**, và được nhận bản vá bảo mật theo nhịp của framework.
- **Ranh giới rõ và kiểm được bằng máy:** luật S5 bắt được ngay khi kiểu Identity rò ra khỏi `Core.Infrastructure`, nên rò rỉ không diễn ra âm thầm.
- **Application không biết Identity tồn tại**, nên tầng đó vẫn test được bằng interface giả.

### Tiêu cực — ghi thẳng, không giảm nhẹ

- **Domain không sở hữu khái niệm người dùng.** Đây là một khiếm khuyết kiến trúc thật, không phải chuyện quan điểm. Hệ quả cụ thể: một luật nghiệp vụ dạng *"người dùng bị khoá thì không được duyệt phiếu"* không viết được trong Domain mà phải viết ở Application, nơi có `IIdentityService`.
- **Đổi sang SSO hoặc LDAP sau này phải sửa Infrastructure — không sửa được bằng cấu hình.** Phải viết lại phần lưu trữ danh tính, phần đăng nhập, và phần ánh xạ người dùng ngoài sang quyền bên trong. Khối lượng đó là thật, và quyết định này chấp nhận trả nếu ngày đó tới.
- **Mọi dự án dựng trên Core bị ràng vào ASP.NET Core Identity**, kể cả dự án chỉ cần một cơ chế đăng nhập tối giản.
- **Hình dạng bảng người dùng do framework quyết.** Muốn thêm trường thì thêm vào lớp kế thừa, và phải chấp nhận những cột mình không dùng vẫn nằm đó.
- **Có hai vòng đời entity trong cùng một `DbContext`:** entity nghiệp vụ theo quy ước của Core (soft delete, trường audit, khoá lạc quan), còn `AppUser`/`AppRole` theo quy ước của Identity. Người mới sẽ nhầm, và tài liệu phải nói rõ chỗ nhầm đó.

## Liên quan

- [`../RULES.md`](../RULES.md) — luật S5
- [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) — mục ranh giới Identity
- [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) — xác thực và phiên
- [`0005-permission-based.md`](0005-permission-based.md) — phân quyền không dựa trên role cứng
