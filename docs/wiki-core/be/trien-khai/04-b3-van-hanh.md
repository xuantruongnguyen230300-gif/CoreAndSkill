---
kind: luat
scope: core
verified: chua-doi-chieu
---

# B3 — Vận hành

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**
>
> **Định nghĩa hoàn thành:** nhật ký kiểm toán ghi được và tra được; chỉ số cùng health check phản ánh đúng trạng thái thật; một lần triển khai và một lần quay lui đi đúng thứ tự đã khai; tạo một đơn vị mới chạy lại được nhiều lần mà không nhân đôi dữ liệu.

---

## 1. Dựng gì ở pha này

| Thứ | File chủ |
| --- | --- |
| Nhật ký kiểm toán — đường ghi | [`../10-data-retention.md`](../10-data-retention.md) §5 · [`../../../database/schema-core.md`](../../../database/schema-core.md) §9.4 |
| Vòng đời dữ liệu, dọn dữ liệu quá hạn | [`../10-data-retention.md`](../10-data-retention.md) |
| Chỉ số, cảnh báo sớm, mức log | [`../07-observability.md`](../07-observability.md) §4, §7 |
| Triển khai, bí mật theo môi trường, quay lui, truy sự cố | [`../18-trien-khai-va-van-hanh.md`](../18-trien-khai-va-van-hanh.md) |
| Seed cho đơn vị mới, chạy lại được | [`../17-multi-tenant.md`](../17-multi-tenant.md) §10, §11.4 |
| Hiệu năng: chỉ sửa sau khi đo | [`../11-performance-caching.md`](../11-performance-caching.md) |

Pha này **không** dựng thành phần Nhóm B. Outbox, thông báo, lưu file, nhập/xuất chỉ dựng khi ngưỡng ở [`../01-core-components.md`](../01-core-components.md) §2 chạm tới.

---

## 2. Thứ tự viết

1. **Đường ghi nhật ký kiểm toán**, gắn vào tầng dữ liệu để không phụ thuộc người viết handler nhớ gọi.
2. **Chính sách vòng đời dữ liệu**, gồm cả thứ không được xoá vì nhật ký đang tham chiếu.
3. **Chỉ số và mức log**, đủ để trả lời ba câu hỏi vận hành ở [`../07-observability.md`](../07-observability.md) §1.
4. **Diễn tập triển khai**: áp schema, triển khai app, rồi **quay lui** một lần trên môi trường thử.

Bước 4 là bước hay bị bỏ nhất, và là bước duy nhất chứng minh runbook đúng.

---

## 3. Ba thứ hay bị làm sai ở B3

| Sai | Hậu quả |
| --- | --- |
| **Ghi cả nội dung nhạy cảm vào nhật ký** | [`../07-observability.md`](../07-observability.md) §5 liệt kê thứ không bao giờ được log. Nhật ký rò dữ liệu còn tệ hơn không có nhật ký |
| **Quay lui app kèm quay lui schema** | Trái [`../18-trien-khai-va-van-hanh.md`](../18-trien-khai-va-van-hanh.md) §4: schema đi tiến, app quay lui được — ngược lại thì mất dữ liệu |
| **Thêm cache khi chưa đo** | [`../11-performance-caching.md`](../11-performance-caching.md): cache sai chỗ tạo ra một nguồn dữ liệu cũ và một bài toán vô hiệu hoá cache |

---

## 4. Nghiệm thu B3

- [ ] Sửa một bản ghi → có đúng một dòng nhật ký, mang nhãn hiển thị tại thời điểm ghi.
- [ ] Xoá dữ liệu quá hạn theo chính sách → thứ nhật ký đang tham chiếu **không** bị xoá theo.
- [ ] Ngắt database → `/health/ready` đỏ, `/health/live` vẫn xanh.
- [ ] Diễn tập một lần triển khai và một lần quay lui trên môi trường thử, đi đúng thứ tự tài liệu.
- [ ] Chạy lệnh tạo đơn vị mới hai lần → không nhân đôi vai trò, menu hay tài khoản.
- [ ] Bí mật không nằm trong artifact; thiếu bí mật thì app không khởi động.

---

## 5. Sau B3

Pha kế: [`05-b4-tep-nhap-xuat-thong-bao.md`](05-b4-tep-nhap-xuat-thong-bao.md) — tệp, nhập/xuất, thông báo.

Thủ tục thêm thành phần mới vào Core — và ngưỡng để một thứ được vào Core — ở [`../01-core-components.md`](../01-core-components.md) §6 và [`../../../kien-truc-core-module.md`](../../../kien-truc-core-module.md) §4.
