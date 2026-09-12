---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 19. Cấu hình theo đơn vị

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> **Thành phần Nhóm B** ([`01-core-components.md`](01-core-components.md) §2) — ngưỡng bật: *có đơn vị thứ hai muốn hiển thị hoặc tính khác nhau*. Bảng dữ liệu chỉ tạo khi thành phần được bật.
>
> Bí mật hạ tầng **không** thuộc file này — chúng đi bằng biến môi trường, xem [`18-trien-khai-va-van-hanh.md`](18-trien-khai-va-van-hanh.md) §2.

---

## 1. Bài toán

Một bản cài phục vụ nhiều cơ quan ([`../../adr/0013-multi-tenant.md`](../../adr/0013-multi-tenant.md)). Mỗi cơ quan muốn logo của mình, tên hiển thị của mình, định dạng ngày của mình, số dòng mặc định trên lưới của mình.

Không có chỗ chung để khai những thứ đó thì mỗi module tự dựng một bảng tham số riêng, và sau vài module thì có ba bảng cấu hình không ai biết cái nào là thật.

---

## 2. Thứ tự ưu tiên của cấu hình — định nghĩa gốc

Ba phạm vi, giá trị ở phạm vi hẹp **ghi đè** phạm vi rộng:

| # | Phạm vi | Ai đặt | Ví dụ |
| --- | --- | --- | --- |
| 1 | **Người dùng** | Người dùng tự đặt | Ngôn ngữ, số dòng mỗi trang |
| 2 | **Đơn vị** | Quản trị viên của đơn vị | Logo, tên hiển thị, định dạng ngày |
| 3 | **Hệ thống** | Người vận hành, lúc cài đặt | Giá trị dùng chung cho mọi đơn vị |
| 4 | **Mặc định khai trong code** | Người viết tính năng | Luôn tồn tại, nên đọc cấu hình **không bao giờ** trả về rỗng |

Tầng 4 là tầng quan trọng nhất về mặt thi công: **mọi khoá phải có giá trị mặc định trong code**. Thiếu nó thì một khoá chưa ai đặt sẽ làm tính năng hỏng ở đúng đơn vị chưa cấu hình, và lỗi đó chỉ lộ ra ở nơi khó tái hiện nhất.

---

## 3. Ai khai khoá

Mỗi module tự khai khoá của mình qua một seam, **cùng khuôn với danh mục quyền** ([`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §1.1) — Core giữ cơ chế, module cấp dữ liệu. Không dựng cơ chế thứ hai cho cùng một việc.

Mỗi khoá khai: mã khoá, kiểu giá trị, giá trị mặc định, phạm vi cho phép đặt, và khoá dịch của nhãn hiển thị.

**Phạm vi cho phép đặt là một phần của khai báo, không phải quy ước miệng.** Một khoá chỉ có nghĩa ở mức hệ thống mà cho đặt ở mức người dùng thì sẽ có người đặt, và hành vi sau đó không ai giải thích được.

---

## 4. Đọc và cache

| Luật | Vì sao |
| --- | --- |
| Đọc qua một seam, không truy vấn thẳng bảng | Đổi cách lưu không phải sửa chỗ gọi |
| Cache theo **đơn vị**, không cache toàn cục | Cache toàn cục ở hệ nhiều đơn vị là đường rò dữ liệu giữa các đơn vị |
| Ghi thì **xoá cache của đúng đơn vị đó ngay**, không đợi hết hạn | "Đổi cấu hình xong mà chưa thấy đổi" là lỗi người dùng báo nhiều nhất ở mảng này |
| Đọc trả về kiểu đã khai, không trả chuỗi thô | Ép kiểu rải rác ở nơi gọi là chỗ sinh lỗi lặng |

---

## 5. Ba ràng buộc bắt buộc

| Ràng buộc | Vì sao |
| --- | --- |
| **Không để bí mật vào bảng cấu hình** | Chuỗi kết nối, khoá ký, mật khẩu thư đi bằng biến môi trường ([`18-trien-khai-va-van-hanh.md`](18-trien-khai-va-van-hanh.md) §2). Bảng này đọc được bởi quản trị viên của đơn vị — đó là đúng chỗ **không** nên có bí mật hạ tầng |
| **Mọi lần đổi ghi nhật ký kiểm toán** | Cấu hình đổi hành vi hệ thống. Không có vết thì câu hỏi *"ai bật cái này"* không trả lời được — [`10-data-retention.md`](10-data-retention.md) §5 |
| **Đổi cấu hình không được đổi luật** | Cấu hình chỉnh *tham số*, không bật/tắt luật nghiệp vụ. Muốn bật/tắt tính năng là **feature flag**, một thứ khác, cũng thuộc Nhóm B |

---

## 6. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Bốn tầng ưu tiên, mặc định nằm trong code | 📐 luật thiết kế | §2 |
| Module tự khai khoá qua seam | ✅ khi bật | §3 — cùng khuôn danh mục quyền |
| Cache theo đơn vị, xoá ngay khi ghi | ✅ khi bật | §4 |
| Ghi nhật ký kiểm toán khi đổi | ✅ khi bật | §5 |
| Bảng `core.setting` | ✅ khi bật | [`../../database/schema-core.md`](../../database/schema-core.md) §9.5 |
| **Màn hình quản trị cấu hình** | ❌ chưa | Cần khi người vận hành không phải người cài đặt |
| **Feature flag** | ❌ chưa | Nhóm B riêng, đừng gộp vào đây |
