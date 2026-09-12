---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `D6` — Đăng xuất

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng ngắn nhất trong cả khu, và là luồng **luôn phải chạy được** — kể cả khi mọi cửa khác đang đóng.

---

## 1. Ai bắt đầu, ở đâu

Người dùng đã đăng nhập, từ menu tài khoản.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Một phiên bất kỳ | **Kể cả phiên đang bị buộc đổi mật khẩu** — đây là một trong bốn cửa mở của luồng `D2` |
| Token chống giả mạo | Đăng xuất là request ghi |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Người dùng | `POST /auth/logout` | [`../contracts/auth.md`](../contracts/auth.md) §4 |
| 2 | BE | Huỷ phiếu phiên | cùng trên §1 |
| 3 | FE | Xoá trạng thái cục bộ, quay về màn đăng nhập | |

### Vì sao đăng xuất là một request ghi, không phải một liên kết

Một liên kết đăng xuất mà chỉ cần mở đường dẫn là **đăng xuất được người khác**: trang bất kỳ nhúng một ảnh trỏ tới đường dẫn đó sẽ đá người dùng ra. Không nguy hiểm, nhưng khó chịu và hoàn toàn tránh được — nên nó là `POST` kèm token chống giả mạo, như mọi request ghi.

### Vì sao nó nằm trong bốn cửa mở của luồng `D2`

Một người bị buộc đổi mật khẩu mà **không thoát được** là một tài khoản bị nhốt. Họ có thể đang đăng nhập nhầm tài khoản, hoặc đơn giản là muốn dừng lại.

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Chưa đăng nhập | `CORE.AUTH.NOT_AUTHENTICATED` (401) | FE nên coi là đã ở trạng thái mong muốn, không hiện lỗi |
| Thiếu token chống giả mạo | `CORE.AUTH.CSRF_REJECTED` (403) | 🛑 Người dùng **không thoát được**, và không hiểu vì sao. FE phải lấy token trước khi gọi |
| FE xoá trạng thái mà request hỏng | không có mã lỗi | Người dùng thấy mình đã đăng xuất, nhưng phiếu phiên vẫn còn hiệu lực ở server |

## 5. Quan hệ với đơn vị

**Không áp dụng** — luồng chỉ huỷ phiếu của chính phiên đang gọi. Nó không đọc và không ghi dữ liệu thuộc đơn vị nào.

## 6. Câu chưa trả lời được

- **Đăng xuất có huỷ mọi phiên của người đó, hay chỉ phiên hiện tại?** Hai hành vi khác nhau và cả hai đều hợp lý; không file nào chốt. Với một người đăng nhập trên nhiều máy, câu trả lời quyết định xem "đăng xuất" có nghĩa là gì.
- **Có đường nào để người dùng tự xem và huỷ các phiên khác của mình không?** Không endpoint nào trong `contracts/` làm việc này. Người nghi tài khoản bị chiếm hiện chỉ có một đường: nhờ quản trị đặt lại mật khẩu (luồng `D4`), vì bước 4 của luồng đó mới là thứ đá hết phiên.
