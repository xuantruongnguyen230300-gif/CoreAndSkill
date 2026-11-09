---
kind: luat
scope: du-an
verified: chua-doi-chieu
status: template — not built
---

# `<Tên feature>` — nghiệp vụ màn hình

> **Khuôn.** Chỉ tạo file này khi feature có **màn hình mới**. Xoá dòng `status:` ở frontmatter khi chép. Xoá mọi dòng hướng dẫn (in nghiêng) sau khi điền.
>
> 🛑 **File này chỉ giữ nghiệp vụ của màn hình** — dữ liệu nào hiện, thao tác nào có, điều kiện nghiệp vụ để hiện. Bố cục, component, trạng thái màn, responsive, câu chữ hiển thị, khoá dịch và đường dẫn của màn nằm ở spec màn dưới [`../../docs/Design/Screens/`](../../docs/Design/Screens/) — nguồn giao diện duy nhất, `design-expert` viết. Mục nào thuộc tệp nào: [`../../docs/OWNERSHIP.md`](../../docs/OWNERSHIP.md) §7.
>
> Màn chưa có spec màn thì ghi dòng `BÀN GIAO: design-expert` cuối báo cáo, kèm mã màn ở §1 — đừng phác giao diện trong file này.

Luật nghiệp vụ nằm ở [`business-rules.md`](business-rules.md); ở đây trỏ mã AC/BR, không viết lại. Quyền theo hành động ở §6 của tệp đó, ý nghĩa thông báo ở §8.

---

## 1. Danh sách màn hình

| Mã | Tên | Spec màn |
| --- | --- | --- |
| M-01 | | *Đường dẫn tệp dưới `docs/Design/Screens/`, hoặc "chưa có"* |

## 2. `M-01 — <tên màn hình>`

### 2.1 Mục đích

*Người dùng tới đây để làm gì. Một câu.*

### 2.2 Dữ liệu hiển thị

| Trường | Nguồn | Định dạng | Ghi chú |
| --- | --- | --- | --- |
| | | | |

### 2.3 Hành động

*Tên hành động viết đúng như ở `business-rules.md` §6 — permission tra ở đó, không chép sang đây.*

| Hành động | Điều kiện hiện | Kết quả | AC liên quan |
| --- | --- | --- | --- |
| | | | |

### 2.4 Mặc định của danh sách

*Chỉ màn danh sách, và chỉ khi mặc định là yêu cầu nghiệp vụ: thứ tự sắp xếp, bộ lọc bật sẵn. Tên tham số trên URL là tên tham số trên dây ở card hợp đồng — [`../../docs/quy-uoc/fe-routing-guard.md`](../../docs/quy-uoc/fe-routing-guard.md) §7.2 — không khai ở đây.*

| Mặc định | Giá trị | AC/BR liên quan |
| --- | --- | --- |
| | | |

### 2.5 Bàn phím và accessibility

*Chuẩn chung ở [`../../docs/wiki-core/fe/15-accessibility.md`](../../docs/wiki-core/fe/15-accessibility.md). Ở đây chỉ ghi yêu cầu RIÊNG của người dùng ở màn này — cách hiện thực thuộc spec màn.*

-

---

## 3. Câu hỏi còn mở

| # | Câu hỏi | Chặn việc gì | Ai trả lời |
| --- | --- | --- | --- |
| | | | |
