---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `D2` — Đổi mật khẩu bắt buộc ở lần đăng nhập đầu

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Trạng thái **nửa vời có chủ đích**: đăng nhập đã thành công, nhưng gần như mọi cửa đều đóng. Bốn cửa mở là bốn cửa đã cân nhắc từng cái.

---

## 1. Ai bắt đầu, ở đâu

Người dùng vừa đăng nhập thành công (luồng `D1`) với cờ buộc đổi mật khẩu đang bật.

Các đường dẫn tới trạng thái này: tài khoản `admin` do lệnh bootstrap tạo (`V1`), tài khoản quản trị của đơn vị mới (`V2`), tài khoản do quản trị tạo (`N1`), tài khoản vừa bị quản trị đặt lại mật khẩu (`D4`), và tài khoản quản trị đơn vị vừa được vận hành khôi phục (`V4`).

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Phiên hợp lệ | Đăng nhập **vẫn thành công** — nếu chặn ngay ở đăng nhập thì người dùng không có đường nào để đổi |
| Mật khẩu mới đạt chính sách | Chính sách **giống nhau ở mọi môi trường**, luật **S9** |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | FE | Thấy `mustChangePassword = true` trong DTO phiên — từ response `login` ([`D1-dang-nhap.md`](D1-dang-nhap.md) bước 8) hoặc từ `GET /api/v1/core/auth/me` lúc khởi động app | [`../contracts/auth.md`](../contracts/auth.md) §3, §5 |
| 2 | FE | Chuyển sang màn đổi mật khẩu và **không cho thoát ra** | [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.3 |
| 3 | Người dùng | `POST /api/v1/core/auth/change-password-required` | [`../contracts/auth.md`](../contracts/auth.md) §7 |
| 4 | BE | Đổi mật khẩu, hạ cờ, **cấp lại cookie** cho phiên đang dùng; mọi phiên khác của tài khoản bị chấm dứt | cùng trên |
| 5 | FE | Vào ứng dụng bình thường | |

### Bốn cửa mở trong lúc cờ còn bật

Mọi endpoint khác trả `CORE.AUTH.PASSWORD_CHANGE_REQUIRED`, trừ đúng bốn đường:

| Đường | Vì sao phải mở |
| --- | --- |
| Lấy token chống giả mạo | Không có nó thì không gửi được request ghi — kể cả request đổi mật khẩu |
| `GET /api/v1/core/auth/me` | FE cần biết trạng thái để điều hướng đúng |
| `POST /api/v1/core/auth/change-password-required` | **Đường thoát duy nhất** |
| `POST /api/v1/core/auth/logout` | Luôn phải thoát được |

Bỏ sót bất kỳ cửa nào trong bốn cửa này tạo ra một tài khoản **bị nhốt**: đăng nhập được, không đổi được mật khẩu, không thoát được.

### Vì sao chặn ở middleware, không để FE tự điều hướng

FE điều hướng là **trải nghiệm**; middleware chặn là **bảo mật**. Người dùng gọi thẳng endpoint bằng công cụ dòng lệnh là bỏ qua được mọi điều hướng của FE.

Đây là cùng một nguyên tắc với luồng `P3`: menu ẩn một mục không phải hàng rào.

## 4. Hỏng ở đâu — và người dùng thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/auth.md`](../contracts/auth.md) §7 và §11. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Gọi bất kỳ endpoint nào ngoài bốn cửa | `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` | FE phải nhận mã này và điều hướng, không hiện màn lỗi chung |
| Mật khẩu mới không đạt chính sách | `CORE.AUTH.CHANGE_PASSWORD_FAILED`, lý do ở `fieldErrors` | BE trả **mã lỗi**, không trả câu tiếng Việt — câu chữ do FE ghép |
| FE không xử lý mã `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` | không có mã lỗi mới | 🛑 Người dùng thấy màn lỗi chung ở mọi thao tác, và **không ai nói cho họ biết phải đổi mật khẩu** |
| Bước 4 đổi mật khẩu mà không cấp lại cookie | không có mã lỗi riêng | 🛑 Đổi mật khẩu thành công rồi bị đá về màn đăng nhập ngay ở request kế tiếp — đúng lúc người dùng nghĩ mình vừa vào được |

## 5. Quan hệ với đơn vị

**Không áp dụng** — luồng này chỉ chạm bản ghi của chính người đang đăng nhập, trong đơn vị đã được thiết lập ở luồng `D1` bước 4. Nó không đọc và không ghi dữ liệu của ai khác.

## 6. Câu chưa trả lời được

Không còn.
