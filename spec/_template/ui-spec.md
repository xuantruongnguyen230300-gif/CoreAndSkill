---
kind: luat
scope: du-an
verified: chua-doi-chieu
status: template — not built
---

# `<Tên feature>` — đặc tả giao diện

> **Khuôn.** Chỉ tạo file này khi feature có **màn hình mới**. Xoá mọi dòng hướng dẫn (in nghiêng) sau khi điền.
>
> 🛑 **File này KHÔNG phải nguồn thiết kế.** Token, component, trạng thái và quy tắc hiển thị nằm ở [`../../docs/Design/`](../../docs/Design/) — nguồn giao diện duy nhất. Ở đây chỉ mô tả **màn hình này ghép các thứ đó lại ra sao**.
>
> Cần một component chưa có trong [`../../docs/Design/Components/`](../../docs/Design/Components/) thì **dừng lại** — gọi `design-expert`, đừng tự thiết kế trong file này.

Nghiệp vụ của màn hình nằm ở [`business-rules.md`](business-rules.md). Đừng viết lại luật ở đây; trỏ tới số hiệu AC/BR.

---

## 1. Danh sách màn hình

| Mã | Tên | Đường dẫn route | Permission cần có |
| --- | --- | --- | --- |
| M-01 | | | |

## 2. `M-01 — <tên màn hình>`

### 2.1 Mục đích

*Người dùng tới đây để làm gì. Một câu.*

### 2.2 Bố cục

*Sơ đồ khối bằng ký tự — vùng nào ở đâu. Không vẽ pixel, không chọn màu; đó là việc của khu Design.*

```
┌─────────────────────────────────────────┐
│ PageHeader: tiêu đề + hành động chính    │
├─────────────────────────────────────────┤
│ Bộ lọc                                   │
├─────────────────────────────────────────┤
│ DataTable                                │
└─────────────────────────────────────────┘
```

### 2.3 Component sử dụng

*Chỉ liệt kê và trỏ. Component nào chưa có spec thì dừng lại (xem đầu file).*

| Component | Spec | Dùng để |
| --- | --- | --- |
| | [`../../docs/Design/Components/`](../../docs/Design/Components/) | |

### 2.4 Dữ liệu hiển thị

| Trường | Nguồn | Định dạng | Ghi chú |
| --- | --- | --- | --- |
| | | | |

### 2.5 Hành động

| Hành động | Điều kiện hiện | Permission | Kết quả | AC liên quan |
| --- | --- | --- | --- | --- |
| | | | | |

### 2.6 Trạng thái màn hình

*Bắt buộc điền đủ. Màn hình thiếu trạng thái rỗng hoặc trạng thái lỗi là màn hình sẽ phải sửa sau khi lên thật.*

| Trạng thái | Hiển thị gì |
| --- | --- |
| Đang tải | |
| Có dữ liệu | |
| **Rỗng** (chưa có bản ghi nào) | |
| **Rỗng do lọc** (có dữ liệu nhưng bộ lọc không khớp) | |
| **Lỗi tải** | |
| **Không đủ quyền** | |

### 2.7 Trạng thái trên URL

*Bộ lọc, phân trang, sắp xếp phải nằm trên query param — để tải lại trang và chia sẻ link không mất trạng thái. Xem [`../../docs/quy-uoc/fe-routing-guard.md`](../../docs/quy-uoc/fe-routing-guard.md).*

| Tham số | Kiểu | Mặc định |
| --- | --- | --- |
| | | |

### 2.8 Responsive

| Điểm ngắt | Thay đổi bố cục |
| --- | --- |
| Nhỏ | |
| Vừa | |
| Lớn | |

### 2.9 Bàn phím và accessibility

*Chuẩn chung ở [`../../docs/wiki-core/fe/15-accessibility.md`](../../docs/wiki-core/fe/15-accessibility.md). Ở đây chỉ ghi thứ RIÊNG của màn hình này.*

-

---

## 3. Câu chữ hiển thị

🛑 **Không viết chữ tiếng Việt thẳng vào template.** Mọi câu người dùng đọc đến từ file ngôn ngữ — cổng FE dò dấu thanh trong template và sẽ báo lỗi.

Ở đây chỉ khai **khoá dịch** và **ý nghĩa**; câu thật nằm ở file ngôn ngữ.

| Khoá | Ý nghĩa | Tham số |
| --- | --- | --- |
| | | |

## 4. Câu hỏi còn mở

| # | Câu hỏi | Chặn việc gì | Ai trả lời |
| --- | --- | --- | --- |
| | | | |
