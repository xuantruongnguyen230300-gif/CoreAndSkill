---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0096 — Lệnh ghi qua `UserManager` trong Core kiểm kết quả, hỏng thì dừng; luồng đăng nhập là ngoại lệ có tên duy nhất

> **Trạng thái:** Đã chấp nhận (2026-09-25)

## Bối cảnh

[ADR-0092](0092-dong-interceptor-them-sau-luot-luu-hong-ghi-no-chua-don.md), §Bối cảnh điểm 1, nêu hai chỗ gọi bỏ qua kết quả `UserManager.UpdateAsync` rồi lưu tiếp: `ResetOperatorPasswordAsync` và `RecoveryResetAdminPasswordAsync`. Cả hai nằm trong `TenantProvisioningService.cs`. ADR đó đưa lại cho người dùng câu hỏi: hai chỗ này có tính là điều kiện kích hoạt (a) của nó không.

Mỗi lệnh ghi của `UserManager` tự lưu. Kho người dùng đổi lỗi xung đột đồng thời thành một `IdentityResult` thất bại, không ném ngoại lệ. Bỏ qua kết quả thì có hai hậu quả:

1. Lượt lưu hỏng để lại trong bộ theo dõi dòng nhật ký và dòng outbox của một thay đổi không xảy ra. Lượt lưu sau sẽ mang chúng đi. Đó là nợ E19.
2. Người gọi vẫn trả thành công, nên transaction **commit phần đã ghi được**. Ví dụ: `RemovePasswordAsync` qua, `AddPasswordAsync` hỏng. Tài khoản mất mật khẩu cũ và không có mật khẩu mới, mà lệnh vẫn báo xong.

Người dùng trả lời ngày 2026-09-25: **kiểm kết quả, hỏng thì dừng**. `backend-expert` đã sửa cùng ngày. Kiến trúc sư đối chiếu cùng ngày:

| Chỗ | Neo | Hỏng thì trả |
| --- | --- | --- |
| Đặt lại mật khẩu vận hành, khôi phục quản trị | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Tenants/TenantProvisioningService.cs`, hàm `ResetPasswordWritesAsync(`: bốn lệnh ghi, mỗi lệnh `if (!(await userManager.` … `.Succeeded)` rồi `return false` | `CORE.TENANT.OPERATOR_PASSWORD_RESET_FAILED` · `CORE.TENANT.RECOVERY_RESET_FAILED`; không ghi nhật ký, không lưu |
| Đổi mật khẩu, gỡ cờ bắt đổi | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Identity/IdentityService.cs`, chuỗi `var clearResult = await userManager.UpdateAsync(user);` | `CORE.CONCURRENCY.CONFLICT` hoặc `CORE.AUTH.CHANGE_PASSWORD_FAILED` |
| Đặt lại mật khẩu hộ, khoá tài khoản | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Identity/UserAdminService.cs`, chuỗi `UserErrors.ResetPasswordFailed` và `UserErrors.LockFailed` | Xung đột, hoặc `RESET_PASSWORD_FAILED` · `LOCK_FAILED` |

Test không database xanh ngày 2026-09-25: `PasswordResetWriteFailureTests`, `UserAdminStampFailureTests`.

Còn đúng hai lời gọi bỏ kết quả dưới `src/BE/Core`. Cả hai ở luồng đăng nhập: `AccessFailedAsync` và `ResetAccessFailedCountAsync` trong `IdentityService.cs`. Luồng đó chạy ngoài transaction (`INoTransaction`) và không lưu gì sau hai lời gọi này.

## Quyết định

Người dùng chốt ngày 2026-09-25: **mọi lệnh ghi qua `UserManager` trong Core kiểm `IdentityResult`. Hỏng thì dừng: trả lỗi ngay, không chạy lệnh ghi nào sau nó, không ghi nhật ký.**

Kiến trúc sư ghi phần đi kèm:

1. **Ngoại lệ có tên duy nhất là hai lời gọi đếm lần sai của luồng đăng nhập.** Ngoại lệ mới cần ADR mới.
2. **Luật này không kích hoạt E19.** Điều kiện (a) của ADR-0092 là *lưu tiếp sau một lệnh ghi hỏng*. Hai chỗ gọi gần chạm điều kiện đó nay dừng ngay khi hỏng, nên (a) chưa xảy ra. Quyết định 3 của ADR-0092, interceptor tự dọn, vẫn chờ kích hoạt như cũ.
3. Luật mang mã **E20**, chưa có cổng. Nợ ở [`../DEBT.md`](../DEBT.md).

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Coi hai chỗ gọi là kích hoạt (a), thi công ngay quyết định 3 của ADR-0092

**Được:** bộ theo dõi sạch sau mọi lượt lưu hỏng, kể cả ở chỗ gọi viết sau này.

**Mất:** chỉ giải hậu quả 1. Hậu quả 2 vẫn nguyên: người gọi báo thành công, và transaction commit một lượt đặt lại mật khẩu dở dang.

**Vì sao loại:** giải nửa nhẹ hơn của vấn đề.

### Phương án B — Để `UserManager` ném ngoại lệ khi hỏng

**Được:** không lời gọi nào quên kiểm được. Ngoại lệ tự dừng luồng.

**Mất:** xung đột đồng thời thành 500 thay vì 409. Lỗi của bộ kiểm người dùng mất đường về `fieldErrors`. Muốn vậy phải bọc hoặc thay kho mặc định của Identity, đúng chỗ ADR-0089 đã chọn *trả kết quả* thay vì ném.

**Vì sao loại:** đổi hợp đồng lỗi của mọi đường ghi tài khoản chỉ để khỏi viết một câu `if`.

### Phương án C — Không có ngoại lệ: luồng đăng nhập cũng kiểm và dừng

**Được:** luật không có ngoại lệ, nên cổng không cần allowlist.

**Mất:** người gõ đúng mật khẩu bị từ chối đăng nhập chỉ vì lệnh xoá bộ đếm vấp xung đột với một lần sai song song. Lần sai bị xung đột, nếu dừng, cũng chỉ trả đúng mã mà nó vốn trả.

**Vì sao loại:** dừng ở đây không bảo vệ dữ liệu nào, vì luồng không lưu gì sau đó. Nó chỉ đổi một lần đăng nhập đúng thành một lần thất bại.

## Hệ quả

### Tích cực

- Không lệnh đặt lại mật khẩu nào báo xong mà để tài khoản ở trạng thái dở dang.
- Câu hỏi ADR-0092 để ngỏ có câu trả lời. E19 giữ đúng hai điều kiện kích hoạt ban đầu.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Chưa cổng nào ép** | Một lời gọi mới viết `await userManager.XAsync(user);` rồi bỏ kết quả vẫn biên dịch sạch, và test xanh cho tới khi có người dựng đúng ca hỏng. Lớp bắt được hôm nay là `core-reviewer` |
| **Lần sai bị xung đột có thể không được đếm** | `AccessFailedAsync` hỏng vì xung đột với một lần sai song song thì bộ đếm khoá tài khoản chỉ tăng một cho cả hai lần. Dò mật khẩu song song vào một tài khoản vì thế có thể thử nhiều lần hơn ngưỡng khoá. Lớp còn chặn là tầng rate limit theo tài khoản ở [`../contracts/auth.md`](../contracts/auth.md) §10, không phụ thuộc bộ đếm này |
| **Bộ đếm có thể không về 0 sau một lần đăng nhập đúng** | `ResetAccessFailedCountAsync` vấp xung đột thì đăng nhập vẫn thành công, nhưng bộ đếm giữ số cũ. Vài lần sai sau đó khoá tài khoản sớm hơn người dùng tưởng |
| **Đường khôi phục trả 422 cho cả xung đột đồng thời** | Card §4 của [`../contracts/tenants.md`](../contracts/tenants.md) không khai `CONFLICT`, nên xung đột ở đó ra `RECOVERY_RESET_FAILED` 422. Request không mang token phiên bản, nên FE không có bản ghi nào để tải lại. Việc đúng là thử lại, và mã 422 không nói điều đó rõ bằng một mã riêng |

### Rút lui nếu sai

Không có dữ liệu nào để chuyển. Rút lui chỉ là bỏ phép kiểm ở chỗ gọi cần nới, rồi viết ADR mới nêu lý do. Nếu lý do là để lưu tiếp sau một lệnh ghi hỏng, thì điều kiện (a) của ADR-0092 kích hoạt ngay, và quyết định 3 của ADR đó phải thi công cùng lượt.

### Dấu hiệu quyết định này bắt đầu sai

- Một luồng cần lưu tiếp sau một lệnh ghi `UserManager` hỏng. Ví dụ: đồng bộ hàng loạt tài khoản, bỏ qua tài khoản lỗi rồi làm tiếp.
- Log cho thấy `AccessFailedAsync` vấp xung đột thường xuyên. Khi đó bộ đếm khoá đang đếm thiếu một cách có hệ thống.

## Liên quan

- [`0092-dong-interceptor-them-sau-luot-luu-hong-ghi-no-chua-don.md`](0092-dong-interceptor-them-sau-luot-luu-hong-ghi-no-chua-don.md): bổ sung cho ADR đó
- [`0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md`](0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md): kho trả kết quả thay vì ném
- [`../DEBT.md`](../DEBT.md) E19, E20
- [`../contracts/tenants.md`](../contracts/tenants.md) §4
