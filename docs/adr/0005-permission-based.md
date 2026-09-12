---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0005 — Bỏ hằng số role khỏi Core; phân quyền bằng permission

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

Ở dự án tiền nhiệm, tầng Application của Core có một lớp hằng số khai ba tên role: quản trị tối cao, quản trị, người dùng. Ba chuỗi đó nằm trong Core, nghĩa là **mọi dự án dựng trên Core buộc phải có đúng ba role này, đúng tên này** — kể cả dự án mà mô hình tổ chức hoàn toàn khác.

Điều làm nó thành mâu thuẫn nội tại: hệ **đã có sẵn phân quyền theo permission** — thuộc tính đánh dấu yêu cầu quyền trên endpoint, dịch vụ kiểm quyền, và một ma trận quyền theo tài nguyên lưu trong DB. Đã có ma trận quyền rồi thì role cứng **vừa thừa vừa trói**: thừa vì mọi quyết định cho phép hay từ chối cuối cùng đều quy về permission; trói vì tên role đã lọt vào Core thì không dự án nào bỏ được.

Có thêm một lỗi phụ đáng ghi lại vì nó nói lên bản chất vấn đề: chú thích của lớp hằng số đó trỏ về một tài liệu đã mang nhãn lịch sử — tức code đang trỏ vào tài liệu đã chết, mà cổng chỉ kiểm đường dẫn có tồn tại nên vẫn xanh. Quyết định này đi kèm việc bịt lỗ hổng đó bằng luật D10 trong [`../RULES.md`](../RULES.md).

## Quyết định

- **Không có hằng số role nào trong Core.** Luật S1 ép bằng test kiến trúc.
- **Phân quyền kiểm bằng permission, không bằng tên role.** Luật S2 ép điều này — kể cả trong guard của FE, xem [`../quy-uoc/fe-routing-guard.md`](../quy-uoc/fe-routing-guard.md).
- **Role là dữ liệu trong DB**, không phải khái niệm trong code. Một role là một cái tên cộng một tập permission; dự án tự khai role của mình khi seed.
- Mã permission theo khuôn `<tài-nguyên>.<hành-động>`, khai ở một catalog tập trung.

### Tài khoản bootstrap lấy toàn quyền bằng cách nào

Đây là câu phải trả lời dứt điểm, vì bỏ role cứng thì mất luôn khái niệm "quản trị tối cao có sẵn mọi quyền".

Cách chọn: **không có tài khoản nào được đặc cách trong code.** Thay vào đó, bước seed lúc khởi tạo dự án:

1. Đọc catalog permission — đây là danh sách **suy ra từ code**, không phải danh sách gõ tay.
2. Tạo một role quản trị với **toàn bộ** permission trong catalog đó. Tên role do dự án đặt, Core không biết tên đó.
3. Gán role vừa tạo cho tài khoản bootstrap, với thông tin đăng nhập lấy từ cấu hình, không hardcode.
4. Bước seed chạy được nhiều lần mà không nhân đôi dữ liệu, và **khi có permission mới thì lần chạy sau bổ sung nó vào role quản trị** — nếu không, mỗi lần thêm quyền mới là một lần quản trị viên mất quyền vừa thêm mà không ai báo.

Điểm mấu chốt: "toàn quyền" là **dữ liệu được sinh ra từ catalog**, không phải một nhánh `if` kiểm tên role trong code. Nhánh `if` đó chính là thứ luật S2 cấm.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ hằng số role trong Core như dự án tiền nhiệm

**Được:** kiểm quyền viết nhanh và đọc dễ; seed đơn giản; không cần catalog permission.

**Vì sao loại:** đó là **nghiệp vụ nằm trong Core**. Core là thứ mang đi mọi dự án; tên role thì mỗi tổ chức một khác. Ngoài ra nó tạo ra hai đường kiểm quyền song song — một theo role, một theo permission — và khi hai đường bất đồng thì không ai biết đường nào thắng, ngoại trừ đọc code.

### Phương án B — Giữ một hằng số duy nhất cho vai quản trị tối cao, bỏ phần còn lại

**Được:** giải quyết gọn bài toán bootstrap; chỉ còn một chuỗi trong Core.

**Vì sao loại:** một chuỗi cũng đủ mở lại cửa. Ngay khi có một nhánh kiểm *"nếu là quản trị tối cao thì cho qua"*, mọi kiểm tra permission phía sau trở thành trang trí, và mọi tính năng mới sẽ bị cám dỗ dùng lại đúng nhánh đó. Một ngoại lệ trong luật phân quyền là một ngoại lệ sẽ được nhân bản.

### Phương án C — Role trong Core nhưng cho phép cấu hình lại tên

**Được:** mềm dẻo hơn phương án A.

**Vì sao loại:** vẫn ràng **số lượng** và **ngữ nghĩa** của các vai vào Core. Một dự án có năm cấp phê duyệt vẫn không diễn đạt được, chỉ khác là nay tên các vai nằm trong tệp cấu hình.

## Hệ quả

### Tích cực

- **Core không còn biết gì về mô hình tổ chức của dự án**, nên mang đi được thật.
- **Một đường kiểm quyền duy nhất**, kiểm được bằng máy qua luật S1 và S2.
- Thêm một vai mới là **thêm dữ liệu**, không phải sửa và deploy lại code.

### Tiêu cực — cái giá thật

- **Không đọc code là không biết hệ có những vai nào.** Câu hỏi *"ai được duyệt phiếu"* trước đây tra bằng cách tìm tên role trong source; nay phải tra trong DB của từng môi trường, và hai môi trường có thể lệch nhau mà không có gì báo.
- **Bước seed trở thành hạ tầng phải chăm.** Nó phải chạy lại được, phải bổ sung permission mới vào role quản trị, và nếu nó hỏng thì hệ vẫn khởi động bình thường nhưng quản trị viên thiếu quyền — một cách hỏng lặng lẽ.
- **Số lượng permission phình theo số tính năng.** Đặt tên và gom nhóm chúng trở thành việc phải làm nghiêm túc, nếu không màn hình gán quyền sẽ thành một danh sách vài trăm dòng không ai đọc.
- **Debug phân quyền khó hơn.** "Người này thiếu quyền gì" phải suy từ chuỗi người dùng → role → permission, thay vì nhìn một cái tên role là đoán ra.

## Liên quan

- [`../RULES.md`](../RULES.md) — luật S1, S2, D10
- [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) — seed role và permission mặc định
- [`../contracts/permissions.md`](../contracts/permissions.md) — hợp đồng API phân quyền
- [`0004-giu-aspnet-identity.md`](0004-giu-aspnet-identity.md) — ranh giới Identity
