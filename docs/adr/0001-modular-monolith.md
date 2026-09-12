---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0001 — Modular Monolith, không microservices và không monolith phẳng

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

CoreAndSkill là bộ khung dùng chung, mang đi dựng nhiều dự án. Các dự án đó cùng một hình dạng: ứng dụng quản trị nội bộ, người dùng đếm bằng trăm chứ không bằng triệu, một đội phát triển nhỏ, một môi trường vận hành.

Dự án tiền nhiệm đã chạy thật theo hình thái một solution — một process — nhiều module, và chưa lần nào chạm ngưỡng cần tách. Đó là bằng chứng thực nghiệm về mức tải, không phải phỏng đoán.

Ràng buộc còn lại là con người: đội vận hành nhỏ, không có SRE riêng. Mọi thứ thêm vào phải có ai đó trực nó lúc hai giờ sáng.

## Quyết định

Một solution, một process, N module.

- Core là **framework nội bộ** — không biết module nào tồn tại.
- Module là **nghiệp vụ**, lắp lên Core.
- Module A **không** tham chiếu Module B; giao tiếp đi qua `Core.Contracts`.
- Mỗi module sở hữu một schema DB riêng; **cấm khoá ngoại vật lý xuyên schema**, tham chiếu bằng ID.

Hình dạng đầy đủ ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §1.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Microservices

**Được:** scale từng phần độc lập, deploy độc lập, cô lập lỗi ở mức process, mỗi service tự chọn ngôn ngữ.

**Mất — và đây là phần thường bị bỏ qua khi so sánh:**

| Chi phí | Nội dung |
| --- | --- |
| Distributed transaction | Một thao tác chạm hai service không còn transaction. Phải dựng saga, viết bước bù, rồi xử lý cả trường hợp bước bù thất bại |
| Tracing phân tán | Không có collector và correlation id xuyên service thì một lỗi trở thành không truy được |
| N pipeline deploy | Mỗi service một pipeline, một bộ secret, một lịch nâng cấp runtime |
| N môi trường phải đồng bộ | Dev muốn chạy toàn hệ trên máy mình phải dựng N dịch vụ |
| Lớp lỗi mới | Timeout, retry sinh bản ghi trùng, thứ tự message đảo — đội chưa từng gặp lớp lỗi này |

**Vì sao loại:** toàn bộ lợi ích ở trên chỉ hiện ra khi có **nhiều đội làm song song** hoặc **một phần chịu tải lệch hẳn phần còn lại**. Hệ tầm trung không chạm điều kiện nào trong hai. Trả chi phí vận hành vĩnh viễn để mua một khả năng chưa dùng là lỗ.

Quan trọng hơn: quyết định này **không đóng cửa**. Vì ranh giới module đã có sẵn — schema riêng, không khoá ngoại xuyên schema, giao tiếp qua `Core.Contracts` — nên tách một module thành service độc lập sau này là việc **cơ học**, không phải viết lại.

### Phương án B — Monolith phẳng

**Được:** đơn giản nhất, không phải trả lời câu hỏi *cái này thuộc module nào* cho mỗi file mới.

**Mất:** không có ranh giới nào cả. Mọi lớp gọi thẳng mọi lớp, mọi bảng join thẳng mọi bảng.

**Vì sao loại:** ranh giới không tự mọc ra sau. Khi cần tách, thứ chặn không phải là code — mà là **đồ thị phụ thuộc đã rối tới mức không còn đường cắt nào**. Ở dự án tiền nhiệm có một dạng nhẹ của đúng bệnh này: hạ tầng web rơi vào project host thay vì nằm trong Core, và hệ quả là dự án thứ hai muốn dùng lại Core phải nhân bản hơn sáu trăm dòng cấu hình khởi động cùng chục file hạ tầng. Đó mới chỉ là **một** ranh giới bị bỏ lỡ.

## Hệ quả

### Tích cực

- **Một transaction thật.** Thao tác chạm nhiều aggregate vẫn nằm trong một transaction cục bộ — xem [`0006-pipeline-behavior.md`](0006-pipeline-behavior.md).
- **Một pipeline deploy, một bộ cấu hình.** Dev mới clone về chạy được mà không cần dựng hạ tầng phụ.
- **Refactor xuyên module vẫn được compiler kiểm.** Đổi chữ ký trong `Core.Contracts` làm vỡ build ngay, chứ không vỡ lúc chạy.
- **Đường tách sau này còn mở**, và chi phí giữ nó mở gần bằng không.

### Tiêu cực — cái giá thật

- **Không có bức tường lửa ở mức process.** Một module rò bộ nhớ hoặc rơi vào vòng lặp vô hạn sẽ kéo cả ứng dụng xuống, kể cả những module không liên quan gì tới nó.
- **Không scale riêng được.** Một module ngốn CPU thì phải nhân bản toàn bộ ứng dụng, trả tiền cho cả phần không cần.
- **Cửa sổ deploy dùng chung.** Sửa một dòng ở một module vẫn phải deploy toàn hệ, và mọi module buộc phải dùng cùng phiên bản runtime, cùng phiên bản thư viện. Một module cần nâng cấp thư viện là việc của tất cả.
- **Ranh giới do quy ước giữ, không do mạng giữ.** Một tham chiếu project thêm nhầm là đủ phá ranh giới, và nó không gây lỗi gì cả — chỉ âm thầm khiến module không tách được nữa. Đây là lý do các luật A3, A4, E5 trong [`../RULES.md`](../RULES.md) phải được ép bằng test kiến trúc, và vì sao phải chăm chúng.

## Liên quan

- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) — hình dạng đầy đủ, tiêu chí Core vs Module
- [`../RULES.md`](../RULES.md) — luật A3, A4, E5
- [`0002-core-5-project.md`](0002-core-5-project.md) — cách chia bên trong Core
