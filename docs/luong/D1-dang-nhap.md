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

Tài khoản vận hành hệ thống cũng đi đúng form này: nó gõ **mã của đơn vị hệ thống** — mã do người vận hành đặt qua cấu hình lúc cài đặt (luồng `V1` bước 3, [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)). Hệ vẫn nhận ra đơn vị hệ thống bằng cột `is_system`, không bằng mã.

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
| 2 | FE | `POST /api/v1/core/auth/login` với `(tenantCode, userName, password)` | [`../contracts/auth.md`](../contracts/auth.md) §3 |
| 3 | BE | Tra đơn vị theo mã | [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §6.2 |
| 4 | BE | 🛑 **Nạp `TenantId` vừa tra được vào ngữ cảnh của request này** | cùng trên — **bước dễ bỏ sót nhất**, xem §4 |
| 5 | BE | Gọi `UserManager`: kiểm mật khẩu **trước**, kiểm khoá tài khoản **sau** — sai mật khẩu thì dừng ở đó, kể cả khi tài khoản đang bị khoá; rồi đọc cờ buộc đổi mật khẩu | [`../contracts/auth.md`](../contracts/auth.md) §3 · [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4 |
| 6 | BE | Phát phiếu phiên mang claim `TenantId` | cùng trên §6.2 |
| 7 | FE | 🛑 **Lấy lại token chống giả mạo** ngay khi đăng nhập trả thành công — token cũ gắn với danh tính lúc chưa đăng nhập | [`../contracts/auth.md`](../contracts/auth.md) §1.1 |
| 8 | FE | Lấy DTO phiên **từ chính response của bước 2** (tập quyền, đơn vị, ngôn ngữ ưa thích) để dựng giao diện và áp ngôn ngữ — **không** gọi `me`, **không** gọi hồ sơ để lấy ngôn ngữ. Bước 7 và 8 **không phụ thuộc thứ tự**; cả hai xong trước khi điều hướng | [`../contracts/auth.md`](../contracts/auth.md) §3 · [`../quy-uoc/fe-routing-guard.md`](../quy-uoc/fe-routing-guard.md) §3.6 · [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §7 |
| 9 | FE | Cờ buộc đổi mật khẩu bật ⇒ chuyển sang màn đổi mật khẩu và **không cho thoát ra** | luồng `D2` |

### Vì sao thứ tự 3 → 4 → 5 không đảo được

`UserManager` tra người dùng **qua bộ lọc đơn vị**. Chạy nó khi ngữ cảnh đơn vị còn rỗng thì hoặc không tra thấy ai, hoặc tra trong phạm vi sai. Cả hai ca đều **không ném exception** — chúng trả về "sai thông tin đăng nhập" cho một mật khẩu đúng.

### Vì sao bước 7 không gộp được vào bước 1

Token chống giả mạo gắn với **danh tính tại lúc phát**. Token lấy ở bước 1 thuộc về một người chưa đăng nhập, và server từ chối nó cho mọi request ghi sau bước 6. Token không tự đổi khi phiên đổi — chỉ có token mới khi FE gọi lại.

## 4. Hỏng ở đâu — và người dùng thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/auth.md`](../contracts/auth.md) §3 và §11. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Thiếu ô bất kỳ | `CORE.VALIDATION.FAILED` | Lỗi ngay tại ô đang thiếu |
| Sai mã đơn vị · đơn vị đã ngưng · sai tên đăng nhập · sai mật khẩu — **kể cả khi tài khoản đang bị khoá** | `CORE.AUTH.INVALID_CREDENTIALS` | **Cùng một câu cho cả bốn ca** — cố ý. Phân biệt được bốn ca này là cho kẻ tấn công biết tài khoản nào có thật |
| Mật khẩu **đúng**, tài khoản đang bị khoá | `CORE.AUTH.LOCKED_OUT` | Câu riêng, vì người dùng thật cần biết để đi tìm quản trị. Lý do ở [`../contracts/auth.md`](../contracts/auth.md) §3; hai đường khoá ở luồng `D5` |
| Thiếu hoặc sai token chống giả mạo | `CORE.AUTH.CSRF_REJECTED` | |
| Gõ sai quá nhiều lần | `CORE.RATE_LIMIT.EXCEEDED` kèm `Retry-After` | **Khác** `CORE.AUTH.LOCKED_OUT`: mã này là hạn mức theo nhịp gọi và tự hết sau `Retry-After`; `LOCKED_OUT` là tài khoản bị khoá — luồng `D5` |
| **Bước 4 bị bỏ** | không có mã lỗi nào | 🛑 Mật khẩu đúng mà báo sai thông tin đăng nhập. Không log lỗi, không exception — người dùng và người trực đều đi tìm bug ở chỗ khác |
| **Bước 7 bị bỏ** | không có mã lỗi riêng | 🛑 Đăng nhập thành công, rồi **request ghi đầu tiên** bị `CORE.AUTH.CSRF_REJECTED` trong khi người dùng thao tác hoàn toàn bình thường. Triệu chứng không gợi ra nguyên nhân |

Khoá tài khoản **theo tài khoản là chưa đủ**: kẻ tấn công thử một mật khẩu phổ biến trên hàng nghìn tài khoản thì không tài khoản nào chạm ngưỡng. Và khoá theo tài khoản mở đường cho tấn công từ chối dịch vụ nhắm vào một người ([`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.2).

## 5. Quan hệ với đơn vị

**Thuộc đơn vị, và luồng này là nơi ngữ cảnh đơn vị được THIẾT LẬP cho toàn bộ phiên.**

Mọi luồng khác thừa hưởng `TenantId` từ claim trong phiếu phát ở bước 6. Nghĩa là **một lỗi ở bước 4 không dừng lại ở màn đăng nhập** — nó đi theo người dùng suốt phiên.

Đơn vị ngưng hoạt động bị chặn ở **hai chỗ**, không phải một: bước 3 của luồng này, và bước dựng danh tính của **mỗi request** sau đó ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10). Thiếu chỗ thứ hai thì người đã đăng nhập từ trước khi đơn vị bị ngưng vẫn thao tác tiếp tới khi phiên hết — xem luồng `V3`.

## 6. Câu chưa trả lời được

Không còn.
