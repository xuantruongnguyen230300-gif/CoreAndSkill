---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N9` — Người dùng sửa hồ sơ của chính mình

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng duy nhất trong Core mà **mọi người dùng đều gọi được, không cần quyền nào**. Đó là quyết định, không phải sơ suất — xem §3.

---

## 1. Ai bắt đầu, ở đâu

Bất kỳ người dùng đã đăng nhập nào, từ menu tài khoản.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Phiên hợp lệ | Không cần khoá quyền nào |
| Token chống giả mạo | Sửa hồ sơ là request ghi |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Người dùng | `GET /profile` | [`../contracts/profile.md`](../contracts/profile.md) §1 |
| 2 | Người dùng | Sửa họ tên, số điện thoại, ngôn ngữ ưa thích | cùng trên §2 |
| 3 | Người dùng | `PUT /profile` | cùng trên |
| 4 | BE | Ghi **chỉ bản ghi của chính người đang gọi** — định danh lấy từ phiên, không lấy từ thân request | cùng trên |

### Vì sao endpoint "của chính mình" không kiểm quyền

Ma trận phân quyền trả lời câu *"người này có được tác động lên **người khác** không"*. Với thao tác lên chính mình, câu đó không có nghĩa — và nếu bắt phải có quyền thì một tài khoản mới tạo, chưa gán vai trò, sẽ **không đổi được mật khẩu của chính nó**, tức không thoát khỏi luồng `D2`.

Đây cũng là lời giải cho tài khoản vận hành hệ thống: nó bị luật **M9** cấm giữ vai trò nghiệp vụ, nên nếu các endpoint "của chính mình" đòi quyền thì nó không xem nổi hồ sơ của chính nó.

> 🛑 **Ranh giới phải hẹp và phải viết ra.** "Của chính mình" nghĩa là thao tác lấy định danh **từ phiên**, không nhận định danh từ thân request. Một endpoint nhận `userId` trong thân rồi tự tin đó là người gọi thì **không** thuộc nhóm này — đó là đường leo thang quyền cổ điển nhất.

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Họ tên rỗng hoặc quá dài; số điện thoại sai khuôn | `CORE.VALIDATION.FAILED` (400) | Lỗi tại đúng ô đang sai |
| Thiếu token chống giả mạo | `CORE.AUTH.CSRF_REJECTED` (403) | |
| Chưa đăng nhập | `CORE.AUTH.NOT_AUTHENTICATED` (401) | |
| Bước 4 lấy định danh từ thân request | không có mã lỗi | 🛑 Bất kỳ ai sửa được hồ sơ của bất kỳ ai. Không lỗi, không log bất thường — chỉ là một dòng dữ liệu đổi chủ |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị**, nhưng theo cách chặt hơn mọi luồng khác: phạm vi không phải "đơn vị của tôi" mà là **"đúng một bản ghi, là tôi"**.

Bộ lọc đơn vị vẫn áp, nhưng nó **không phải** hàng rào chính ở đây — hàng rào chính là việc định danh lấy từ phiên. Một lỗi ở bước 4 vẫn bị bộ lọc đơn vị chặn lại ở ranh giới đơn vị, nghĩa là thiệt hại giới hạn trong cùng đơn vị. Đó là một lớp phòng thủ thứ hai tình cờ có, không phải lớp được thiết kế cho ca này.

## 6. Câu chưa trả lời được

- **Danh sách đầy đủ các endpoint "của chính mình" nằm ở đâu?** Hôm nay nhóm đó gồm `GET /auth/me`, `POST /auth/change-password`, `GET /profile`, `PUT /profile` — nhưng không file nào **khai** nhóm này, nên nó sẽ nới dần mỗi lần có người thấy tiện. Một khoảng trống như vậy là chỗ ranh giới bị xói mòn mà không ai quyết định.
- **Người dùng đổi được email của chính mình không?** `PUT /profile` nêu họ tên, số điện thoại, ngôn ngữ. Email vắng mặt — nếu là cố ý thì lý do chưa viết, mà email là thứ luồng `D3` dùng để đặt lại mật khẩu.
- **Đổi ngôn ngữ ưa thích có ảnh hưởng gì ngoài giao diện không?** Ví dụ tệp xuất ra ở luồng `N3` dùng ngôn ngữ nào — của người xuất, hay của đơn vị.
