---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `P3` — Menu hiện ra theo quyền của người đang đăng nhập

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng này là chỗ **giao diện và bảo mật dễ bị nhầm làm một**. Menu ẩn một mục **không** phải một lớp bảo vệ.

---

## 1. Ai bắt đầu, ở đâu

FE, ngay sau khi đăng nhập thành công (luồng `D1` bước 7).

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Phiên hợp lệ | |
| Bản ghi menu của đơn vị đã seed | Luồng `V1` bước 4 hoặc `V2` |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | FE | `GET /meta/menu` | [`../contracts/meta-menu.md`](../contracts/meta-menu.md) §1 |
| 2 | BE | Lấy cây menu **của đơn vị này** | [`../database/schema-core.md`](../database/schema-core.md) §7 |
| 3 | BE | Lọc từng mục theo hai cơ chế — xem bảng dưới | cùng trên |
| 4 | FE | Dựng thanh điều hướng từ cây đã lọc | [`../Design/Components/Sidebar.md`](../Design/Components/Sidebar.md) |

### Hai cơ chế lọc, và cái nào thắng

| Mục menu | Cách quyết định | Ghi chú |
| --- | --- | --- |
| Có khai quyền bắt buộc | Hiện ⇔ người dùng có **đúng quyền đó**. **Bỏ qua hoàn toàn** bảng gán menu theo vai trò | [`../database/schema-core.md`](../database/schema-core.md) §7 |
| Không khai quyền bắt buộc | Theo bảng gán menu theo vai trò | |

Cơ chế thứ nhất **thắng tuyệt đối** khi có mặt. Đó là chủ đích: nếu cả hai cùng áp thì một mục có thể vừa hiện theo vai trò vừa ẩn theo quyền, và không ai giải thích được kết quả.

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Biểu hiện |
| --- | --- |
| Mục menu trỏ tới màn mà người dùng không có quyền | Bấm vào nhận 403. Khó chịu nhưng **không phải lỗ hổng** |
| Màn được bảo vệ **chỉ bằng** việc ẩn mục menu | 🛑 **Là lỗ hổng.** Người dùng gõ thẳng đường dẫn là vào được. Menu là gợi ý điều hướng, không phải hàng rào — hàng rào nằm ở kiểm quyền phía server |
| Menu rỗng sau khi đăng nhập | Người dùng thấy một ứng dụng trống và không hiểu vì sao. Thường là vai trò rỗng quyền (`P1` bước 5) hoặc chưa gán vai trò (`P2`) |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Bản ghi menu mang `tenant_id` — mỗi đơn vị chỉnh menu của mình mà không đụng đơn vị khác.

Danh mục quyền thì **dùng chung toàn hệ**, nên mục menu của đơn vị A trỏ tới một khoá quyền cũng tồn tại ở đơn vị B. Đó là đúng: khoá là hợp đồng với code, còn việc ai có khoá đó mới là chuyện của từng đơn vị.

## 6. Câu chưa trả lời được

- **Chưa có endpoint quản trị menu.** [`../contracts/meta-menu.md`](../contracts/meta-menu.md) §2 khai rõ điều này. Nghĩa là hôm nay menu chỉ đổi được bằng cách seed hoặc sửa database — và mục "Sửa cấu hình menu" trong danh mục quyền chưa có đường nào dùng tới.
- **Mục menu của module lắp thêm vào lúc nào?** Bản ghi menu mang mã module, nhưng không file nào mô tả luồng "module lắp vào Host thì menu của nó xuất hiện thế nào".
- **Người dùng mang cờ `has_permission_bypass` thấy menu gì?** Nếu bộ kiểm quyền trả lời "có" cho mọi câu hỏi thì họ thấy **mọi** mục — kể cả mục của module chưa lắp, nếu bản ghi menu còn sót lại.
