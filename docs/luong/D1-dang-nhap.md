---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `D1` — Đăng nhập

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng bị gọi nhiều nhất trong cả hệ, và là chỗ **bước 3 dễ bỏ sót nhất** — bỏ nó thì không có lỗi nào bắn ra, chỉ có một tài khoản không tra thấy hoặc tra nhầm đơn vị.

---

## 1. Ai bắt đầu, ở đâu

Bất kỳ người dùng nào, từ **form đăng nhập**. Form có **ba** ô: mã đơn vị · tên đăng nhập · mật khẩu.

Ô mã đơn vị nằm **cùng cấp** với hai ô kia — nó điền **trước** khi xác thực. Đây **không** phải màn chọn đơn vị sau khi xác thực; hướng đó đã bị loại và khoá (`K01`).

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Đơn vị tồn tại và **đang hoạt động** | Đơn vị ngưng hoạt động ⇒ mọi tài khoản của nó không đăng nhập được |
| Tài khoản tồn tại trong đúng đơn vị đó | Một tên đăng nhập chỉ duy nhất **trong phạm vi một đơn vị** |
| FE đã lấy token chống giả mạo | `GET /api/v1/core/antiforgery/token` |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | FE | Lấy token chống giả mạo, gắn vào header `X-XSRF-TOKEN` | [`../contracts/auth.md`](../contracts/auth.md) §2 |
| 2 | FE | `POST /auth/login` với `(tenantCode, userName, password)` | [`../contracts/auth.md`](../contracts/auth.md) §3 |
| 3 | BE | Tra đơn vị theo mã | [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §6.2 |
| 4 | BE | 🛑 **Nạp `TenantId` vừa tra được vào ngữ cảnh của request này** | cùng trên — **bước dễ bỏ sót nhất**, xem §4 |
| 5 | BE | Gọi `UserManager`: kiểm mật khẩu, kiểm khoá tài khoản, đọc cờ buộc đổi mật khẩu | [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4 |
| 6 | BE | Phát phiếu phiên mang claim `TenantId` | cùng trên §6.2 |
| 7 | FE | `GET /auth/me` → lấy hồ sơ và tập quyền để dựng giao diện | [`../contracts/auth.md`](../contracts/auth.md) §5 |
| 8 | FE | Cờ buộc đổi mật khẩu bật ⇒ chuyển sang màn đổi mật khẩu và **không cho thoát ra** | luồng `D2` |

### Vì sao thứ tự 3 → 4 → 5 không đảo được

`UserManager` tra người dùng **qua bộ lọc đơn vị**. Chạy nó khi ngữ cảnh đơn vị còn rỗng thì hoặc không tra thấy ai, hoặc tra trong phạm vi sai. Cả hai ca đều **không ném exception** — chúng trả về "sai thông tin đăng nhập" cho một mật khẩu đúng.

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Thiếu ô bất kỳ | `CORE.VALIDATION.FAILED` (400) | Lỗi ngay tại ô đang thiếu |
| Sai mã đơn vị · đơn vị đã ngưng · sai tên đăng nhập · sai mật khẩu | `CORE.AUTH.INVALID_CREDENTIALS` (422) | **Cùng một câu cho cả bốn ca** — cố ý. Phân biệt được bốn ca này là cho kẻ tấn công biết tài khoản nào có thật |
| Tài khoản đang bị khoá | `CORE.AUTH.LOCKED_OUT` (422) | Câu riêng, vì người dùng thật cần biết để đi tìm quản trị. Đánh đổi có chủ ý, lý do ở [`../contracts/auth.md`](../contracts/auth.md) §3 |
| Thiếu hoặc sai token chống giả mạo | `CORE.AUTH.CSRF_REJECTED` (403) | |
| Gõ sai quá nhiều lần | `CORE.RATE_LIMIT.EXCEEDED` (429) kèm `Retry-After` | **Khác** `LOCKED_OUT`: 429 là hạn mức theo nhịp gọi, 422 là tài khoản bị khoá và cần quản trị mở |
| **Bước 4 bị bỏ** | không có mã lỗi nào | 🛑 Mật khẩu đúng mà báo sai thông tin đăng nhập. Không log lỗi, không exception — người dùng và người trực đều đi tìm bug ở chỗ khác |

Khoá tài khoản **theo tài khoản là chưa đủ**: kẻ tấn công thử một mật khẩu phổ biến trên hàng nghìn tài khoản thì không tài khoản nào chạm ngưỡng. Và khoá theo tài khoản mở đường cho tấn công từ chối dịch vụ nhắm vào một người ([`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.2).

## 5. Quan hệ với đơn vị

**Thuộc đơn vị, và luồng này là nơi ngữ cảnh đơn vị được THIẾT LẬP cho toàn bộ phiên.**

Mọi luồng khác thừa hưởng `TenantId` từ claim trong phiếu phát ở bước 6. Nghĩa là **một lỗi ở bước 4 không dừng lại ở màn đăng nhập** — nó đi theo người dùng suốt phiên.

Đơn vị ngưng hoạt động bị chặn ở **hai chỗ**, không phải một ([`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §6.3) — thiếu chỗ nào cũng hỏng theo một kiểu riêng.

## 6. Câu chưa trả lời được

- **Phiên đang mở khi đơn vị bị ngưng hoạt động thì sao?** §6.3 chặn ở hai chỗ, nhưng cả hai đều nằm trên đường **đăng nhập**. Một người đã đăng nhập từ trước, giữ phiếu còn hạn, thì ai đá ra?
- **`superadmin` (vận hành hệ thống) đăng nhập bằng mã đơn vị nào?** Nó thuộc đơn vị hệ thống, và đơn vị đó nhận ra bằng cột `is_system` chứ không bằng mã. Form đăng nhập vẫn đòi ô mã đơn vị — chưa file nào nói người vận hành gõ gì vào ô đó.
