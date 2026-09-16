---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N1` — Tạo một người dùng mới

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Vì hệ **không có đăng ký**, đây là đường **duy nhất** một tài khoản thường ra đời. Hai đường còn lại sinh tài khoản đều là đường đặc quyền: lệnh bootstrap (`V1`) và tạo đơn vị (`V2`).

---

## 1. Ai bắt đầu, ở đâu

Quản trị đơn vị, từ màn danh sách người dùng.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền `core.user.write` | Thêm `core.user.role.assign` nếu gán vai trò ngay lúc tạo |
| Bộ vai trò đã có quyền | Luồng `P1`. Tạo người rồi gán một vai trò rỗng là tạo một tài khoản không làm được gì |
| Mật khẩu tạm do quản trị tự gõ, đạt chính sách | Chính sách **giống nhau ở mọi môi trường**, luật **S9** |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Quản trị | `POST /api/v1/core/users` với tên đăng nhập, email, họ tên và **mật khẩu tạm do quản trị tự gõ** | [`../contracts/users.md`](../contracts/users.md) §5 |
| 2 | BE | Tạo tài khoản qua `UserManager` với mật khẩu tạm đó | cùng trên |
| 3 | BE | Bật cờ buộc đổi mật khẩu | cùng trên |
| 4 | BE | Trả định danh tài khoản mới — **không** trả mật khẩu | cùng trên |
| 5 | Quản trị | Chuyển mật khẩu tạm cho người dùng | thủ công — xem luồng `D4` §6 |
| 6 | Quản trị | Gán vai trò: luồng `P2` — hoặc gửi kèm vai trò ngay ở bước 1, cùng một transaction | [`../contracts/users.md`](../contracts/users.md) §5 |
| 7 | Người dùng | Đăng nhập (`D1`), rồi buộc đổi mật khẩu (`D2`) | |

### Vì sao quản trị tự gõ mật khẩu tạm

Hệ **không giả định có kênh gửi email**. Việc cấp tài khoản không phụ thuộc vào SMTP có chạy hay không — quan trọng với một hệ nội bộ, nơi việc cấp tài khoản thường diễn ra khi hai người đang ngồi cạnh nhau.

Mật khẩu tạm **không** đi ra ngoài qua phản hồi hay log: nó đi từ người gõ tới `UserManager` và dừng ở đó. Chất lượng của nó do chính sách mật khẩu kiểm — cùng một cách với luồng `D4` và `V2`.

Đánh đổi: mật khẩu tạm đi qua tay quản trị, nên nó chỉ được sống tới lần đăng nhập đầu — cờ ở bước 3 là thứ ép điều đó.

### Vì sao không có endpoint xoá người dùng

[`../contracts/users.md`](../contracts/users.md) §10 khai rõ lý do. Tóm tắt: một tài khoản đã thao tác thì tên nó nằm trong nhật ký kiểm toán và trong các bản ghi nghiệp vụ; xoá nó làm những bản ghi đó trỏ vào hư không. Đường đúng là **khoá** (luồng `D5`).

## 4. Hỏng ở đâu — và ai thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/users.md`](../contracts/users.md) §5. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Identity từ chối | `CORE.USER.CREATE_FAILED` | Lý do ở `fieldErrors` — thường là mật khẩu tạm không đạt chính sách |
| Tên đăng nhập hoặc email đã có | `CORE.USER.USERNAME_DUPLICATED` · `CORE.USER.EMAIL_DUPLICATED` | Trùng **trong đơn vị này** — hai đơn vị có thể cùng có một tên đăng nhập ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11) |
| Thiếu quyền | `CORE.AUTH.FORBIDDEN` | |
| Tạo xong, quên bước 6 | không có mã lỗi | 🛑 Tài khoản đăng nhập được, đổi mật khẩu xong, rồi **không làm được gì**. Cùng triệu chứng với `P1` bước 5 và `V2` bước 5 — ba nguyên nhân khác nhau, một biểu hiện |

Ba luồng cho ra cùng một triệu chứng. Màn người dùng chỉ phân biệt được **"chưa có vai trò"** — không response nào của danh sách người dùng mang tập quyền của vai trò; vai trò chưa có quyền kiểm ở màn ma trận quyền (luồng [`P1`](P1-tao-vai-tro-va-gan-quyen.md)).

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Tài khoản mới mang `tenant_id` của người tạo ra nó — không có ô chọn đơn vị trên form, và **không nên có**: cho quản trị đơn vị A tạo tài khoản ở đơn vị B là mở đúng cánh cửa mà mô hình này đóng.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Kênh chuyển mật khẩu tạm tới người dùng ở bước 5 là câu hỏi chung, giữ ở luồng `D4` §6.
