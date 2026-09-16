---
kind: luat
scope: core
verified: chua-doi-chieu
---

# B0 — Nền móng

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**
>
> **Định nghĩa hoàn thành:** solution có năm project với đúng chiều tham chiếu; một endpoint thử trả envelope đúng khuôn ở cả nhánh thành công lẫn nhánh lỗi; xoá một giá trị cấu hình bắt buộc thì app **không khởi động**; ArchTest chạy trong CI và **đã từng đỏ** với một vi phạm cố ý.

---

## 1. Dựng gì ở pha này

| Thứ | File chủ |
| --- | --- |
| Năm project, chiều tham chiếu, host mỏng | [`../../../kien-truc-core-module.md`](../../../kien-truc-core-module.md) §2 · [`../../../quy-uoc/be-architecture.md`](../../../quy-uoc/be-architecture.md) §1 |
| `Result<T>` và catalog mã lỗi | [`../../../quy-uoc/be-cqrs-handler.md`](../../../quy-uoc/be-cqrs-handler.md) §7 |
| Envelope và ánh xạ sang HTTP tại đúng một chỗ | [`../../../quy-uoc/be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §1, §2 |
| Cấu hình kiểm lúc khởi động | [`../../../quy-uoc/be-architecture.md`](../../../quy-uoc/be-architecture.md) §4 |
| Log có cấu trúc và mã lần gọi xuyên suốt | [`../07-observability.md`](../07-observability.md) §2, §3 |
| Health check tách sống / sẵn sàng | [`../07-observability.md`](../07-observability.md) §8 |
| Bộ ArchTest và meta-test cho từng detector | [`../04-testing-strategy.md`](../04-testing-strategy.md) §2, §3 |

Quản lý phiên bản gói tập trung và các tệp dựng chung: [`../../../quy-uoc/repo-artifact.md`](../../../quy-uoc/repo-artifact.md) §3.1.

---

## 2. Thứ tự viết

1. **Solution và năm project trống**, cùng chiều tham chiếu. Chưa có logic nào.
2. **ArchTest cho ba luật ranh giới**, chạy trên năm project trống. Chúng phải **đỏ** khi thêm một tham chiếu sai, rồi xanh khi gỡ ra — đây là canary của cả bộ.
3. **`Result<T>` và catalog mã lỗi**, chưa cần HTTP.
4. **Envelope và ánh xạ lỗi → HTTP**, kèm một endpoint thử trả cả hai nhánh.
5. **Cấu hình có kiểm lúc khởi động**, log, mã lần gọi.
6. **Health check**, phân biệt sống với sẵn sàng.

Bước 2 đặt trước bước 3 có chủ đích: nếu để sau, bộ ArchTest đầu tiên sẽ được viết dựa trên code đã có.

---

## 3. Ba thứ hay bị làm sai ở B0

| Sai | Hậu quả |
| --- | --- |
| **Ánh xạ lỗi → HTTP bằng phản chiếu** | Đổi tên một kiểu là hỏng im lặng, không lỗi biên dịch. Luật R5 cấm; sự cố gốc ở [`../../../adr/0003-result-thuan.md`](../../../adr/0003-result-thuan.md) |
| **Đưa hạ tầng web vào host thay vì `Core.Web`** | Dự án thứ hai phải chép lại toàn bộ. Đây là bệnh mà [`../../../kien-truc-core-module.md`](../../../kien-truc-core-module.md) §2.2 tồn tại để trị |
| **Cấu hình thiếu thì app vẫn chạy** | Lỗi rơi vào lúc người dùng chạm tính năng, không phải lúc triển khai. Luật A8 |

---

## 4. Nghiệm thu B0

- [ ] Thêm một `ProjectReference` sai chiều → ArchTest đỏ; gỡ ra → xanh.
- [ ] Gọi endpoint thử ở nhánh thành công → envelope đủ ba phần và có mã lần gọi.
- [ ] Gọi endpoint thử ở nhánh lỗi → đúng mã HTTP theo bảng ánh xạ, thân là JSON sạch.
- [ ] Xoá một giá trị cấu hình bắt buộc → app **không khởi động**, thông báo nói rõ thiếu khoá nào.
- [ ] `/health/live` và `/health/ready` trả khác nhau khi chưa nối được database.
- [ ] Mỗi detector của ArchTest có một meta-test đi kèm.
- [ ] Gỡ `ManagePackageVersionsCentrally=true` khỏi `Directory.Packages.props` → test canh thuộc tính đó đỏ; trả lại → xanh. Phần hở của luật A14 ở [`../../../RULES.md`](../../../RULES.md) §10.
- [ ] Endpoint thử ném một exception ngoài dự kiến → 500 mang envelope có `code`, không lộ stack trace; gỡ `UseExceptionHandler()` hoặc lời đăng ký `IExceptionHandler` → test đó đỏ. Phần hở của luật A9 ở [`../../../RULES.md`](../../../RULES.md) §10.
- [ ] `bash .claude/check-docs.sh` xanh.

---

## 5. Đọc tiếp

Pha kế: [`02-b1-du-lieu-don-vi-danh-tinh.md`](02-b1-du-lieu-don-vi-danh-tinh.md).
