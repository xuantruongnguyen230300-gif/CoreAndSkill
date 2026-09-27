---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0058 — Mọi phản hồi đăng nhập trượt chờ đủ một sàn thời gian chung; phép băm giả không đủ để san các nhánh

> **Trạng thái:** Đã chấp nhận (2026-09-22)

## Bối cảnh

[`../contracts/auth.md`](../contracts/auth.md) §3 gộp bốn ca trượt đăng nhập vào một mã: đơn vị không có, đơn vị ngưng hoạt động, sai tên đăng nhập, sai mật khẩu. Cùng mục đó ghi rằng đường dò thật là **chênh lệch thời gian phản hồi**, và yêu cầu vá đường đó.

Code đã vá **phần băm**. Kiến trúc sư đối chiếu ngày 2026-09-22:

| Nhánh | Việc chạy (ngoài hạn mức và tra đơn vị) | Neo |
| --- | --- | --- |
| Đơn vị không có hoặc ngưng hoạt động | Một phép băm giả. **Không** tra tài khoản | `src/BE/Core/CoreAndSkill.Core.Application/Auth/LoginCommandHandler.cs` — `SimulateCredentialCheckAsync` |
| Đơn vị có, tên đăng nhập không có | Tra tài khoản, rồi một phép băm giả | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Identity/IdentityService.cs` — `VerifyAgainstDummyHash(password);` |
| Tài khoản có, sai mật khẩu, chưa khoá | Tra tài khoản, một phép băm thật, rồi **một câu `UPDATE` có commit** | cùng tệp — `await userManager.AccessFailedAsync(user);` |

Như vậy phản hồi vẫn lộ hai điều:

- **Tên đăng nhập có tồn tại hay không**, qua một lần ghi. Mỗi tài khoản chỉ cho kẻ dò vài mẫu trước khi bị khoá, vì tài khoản đã khoá thì không ghi nữa. Nhưng hết thời gian khoá thì kẻ dò lại có thêm vài mẫu.
- **Mã đơn vị có tồn tại hay không**, qua một lần tra tài khoản. Nhánh này không có khoá nào giới hạn số mẫu, và nó lộ đúng thứ §3 muốn giấu: *cơ quan nào đang dùng hệ thống*.

**Chưa đo độ lớn.** Đo cần PostgreSQL thật, mà máy hiện tại không có Docker (luật T2 ở [`../RULES.md`](../RULES.md) §8 mang `⏸️`). Ước lượng theo loại việc: vài mili giây cho một lần ghi có commit, dưới một mili giây cho một lần tra theo index.

## Quyết định

Kiến trúc sư chốt:

1. **Mọi phản hồi trượt của endpoint đăng nhập, trừ 429, đi ra không sớm hơn một sàn thời gian chung**, tính từ lúc handler bắt đầu. Handler chờ phần còn thiếu bằng `TimeProvider`, sau khi mọi truy vấn đã xong. Nhánh thành công không chờ.
2. Sàn là một khoá cấu hình. Mặc định chọn khi chưa đo và phải lớn hơn p99 của nhánh trượt chậm nhất. Khoá và giá trị nằm ở [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.2.
3. Phép băm giả **giữ nguyên**. Sàn bù phần phép băm giả không san được, chứ không thay nó.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Chấp nhận chênh lệch và ghi nhận

**Được:** không thêm code, phản hồi trượt không chậm đi.

**Mất:** đường dò mã đơn vị không có trần số mẫu, trong khi §3 của card xếp mã đơn vị vào loại thông tin phải giấu.

**Vì sao loại:** hợp đồng hiện hành đã chốt *"vá đường đó"*. Chọn phương án này là lật hợp đồng, và đó là quyết định của người dùng chứ không phải của một lượt sửa chênh lệch.

### Phương án B — Cho nhánh ngắn làm thêm đúng phần việc của nhánh dài

Tức là tra tài khoản giả ở nhánh đơn vị không có, và ghi giả ở nhánh tên không có.

**Vì sao loại:** không có dòng nào hợp lệ để ghi giả. Thêm nữa, mỗi lần luồng đăng nhập có thêm một bước thì phải tìm lại và thêm đúng bước đó vào mọi nhánh khác. Lần đầu tiên có người quên là lần chênh lệch quay lại mà không ai hay.

### Phương án C — Cộng một độ trễ ngẫu nhiên

**Vì sao loại:** nhiễu ngẫu nhiên chỉ làm kẻ dò phải lấy nhiều mẫu hơn, vì trung bình của mỗi nhánh vẫn khác nhau. Nó không xoá chênh lệch.

### Phương án D — Chuyển lần ghi "sai một lần" ra sau khi đã trả phản hồi

**Vì sao loại:** lần ghi sẽ chạy ngoài phạm vi request, trong khi `DbContext` và phạm vi đơn vị đều đã đóng. Cách này cũng chỉ vá nhánh sai mật khẩu, không vá nhánh đơn vị không có.

## Hệ quả

### Tích cực

- Bốn ca trượt đi ra cùng một thời điểm mà không phụ thuộc bước nào nằm trong nhánh nào. Thêm bước mới vào luồng đăng nhập không mở lại chênh lệch, miễn là tổng thời gian còn dưới sàn.
- Đường dò mã đơn vị bị đóng theo cùng cơ chế với đường dò tên đăng nhập.

### Tiêu cực

- **Mọi lần gõ sai mật khẩu đều chậm hơn.** Người dùng thật chờ đủ sàn, kể cả khi hệ thống đã trả lời xong từ trước đó.
- **Sàn chỉ an toàn khi lớn hơn thời gian thật**, và hôm nay chưa ai đo thời gian thật. Mặc định có thể quá thấp trên máy yếu, hoặc sau khi tăng số vòng băm. Khi đó chênh lệch quay lại mà không có lỗi nào báo.
- **Có thêm một khoá cấu hình phải vận hành.** Đặt sai theo chiều nhỏ thì mất tác dụng; theo chiều lớn thì mọi lần đăng nhập trượt chậm vô ích.
- **Trong lúc chờ sàn, request vẫn chiếm một chỗ** trong hàng rào hạn mức đồng thời, nếu có. Việc chờ là bất đồng bộ nên không giữ luồng. Handler chờ sau khi truy vấn đã xong và `LoginCommand` chạy không transaction, nên cũng không giữ kết nối.

### Dấu hiệu quyết định này bắt đầu sai

- Số đo p99 của nhánh trượt trên môi trường thật vượt giá trị sàn đang cấu hình.
- Người dùng phàn nàn đăng nhập sai "treo". Khi đó sàn đang cao hơn mức cần.

### Rút lui nếu sai

Gỡ bước chờ khỏi handler và bỏ khoá cấu hình. Hai việc này nằm trong một PR, không có dữ liệu nào phải chuyển, và test của luật S19 đổi theo. Muốn quay về phương án A thì phải sửa §3 của card trong cùng PR, để hợp đồng không còn đòi *"vá đường đó"*.

## Liên quan

- [`../contracts/auth.md`](../contracts/auth.md) §3: hợp đồng hành vi.
- [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md): ba đường dò tài khoản.
- Luật S19 ở [`../RULES.md`](../RULES.md) §6: cổng.
