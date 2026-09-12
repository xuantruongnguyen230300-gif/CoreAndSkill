---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0002 — Core gồm năm project: Domain, Application, Infrastructure, Web, Contracts

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

Ở dự án tiền nhiệm, Core chỉ có ba project: `Domain`, `Application`, `Infrastructure`. Toàn bộ hạ tầng web — lớp controller cơ sở, các middleware, bộ xử lý ngoại lệ toàn cục, lớp đọc người dùng hiện tại từ `HttpContext`, chính sách CORS, giới hạn tần suất đăng nhập — nằm trong project host, cùng với bốn controller vốn là của chính Core: xác thực, người dùng, phân quyền, menu.

Hệ quả đã đo được và chính họ đã ghi nhận: **dự án thứ hai muốn dùng Core phải copy-paste hơn sáu trăm dòng cấu hình khởi động cộng mười bốn file hạ tầng.** Đó không phải tái sử dụng, đó là nhân bản — và mọi bugfix hạ tầng sau đó phải sửa lại ở từng dự án.

Tài liệu của họ đã ghi nhận đích đến là tách thêm project, ở dạng phương án sáu project, nhưng việc đó chưa được thi công.

CoreAndSkill sinh ra chính để giải quyết việc này, nên câu hỏi không còn là *có tách không* mà là *tách thành mấy và theo đường nào*.

## Quyết định

Core gồm đúng năm project:

| Project | Chứa | Tham chiếu được |
| --- | --- | --- |
| `Core.Domain` | Entity, Value Object, `Result<T>`, `Error`, `ErrorType`, luật nghiệp vụ thuần | — (zero package reference) |
| `Core.Application` | Command/Query, handler, validator, pipeline behavior, interface hướng ra ngoài | `Domain` |
| `Core.Infrastructure` | `DbContext`, repository, EF configuration, migration schema `core`, Identity, cache, job nền | `Domain`, `Application` |
| `Core.Web` | `ApiControllerBase`, middleware, ánh xạ `Result` → HTTP, controller Core, `AddCore()` / `UseCore()` | `Domain`, `Application`, `Infrastructure` |
| `Core.Contracts` | Integration event, DTO dùng chung **giữa các module** | — |

Host (`Api`) chỉ là composition root: gọi `AddCore()`, `UseCore()`, đăng ký module. Ngưỡng tự kiểm: `Program.cs` dưới năm mươi dòng.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ ba project như dự án tiền nhiệm

**Được:** không phải làm gì; không phải trả lời câu *file này bỏ vào đâu* cho hạ tầng web.

**Vì sao loại:** đây chính là bệnh đang phải chữa. Hạ tầng web rơi vào host nghĩa là **Core không tự đứng được** — nó chỉ là một nửa framework, nửa còn lại phải chép tay vào từng dự án. Bằng chứng không phải suy đoán: dự án thứ hai đã phải nhân bản hơn sáu trăm dòng khởi động và chục file hạ tầng. Một bộ khung dùng chung mà phần dùng chung phải chép tay thì nó không dùng chung.

### Phương án B — Sáu project: thêm `Core.Common`, tách `Core.Persistence` khỏi `Core.Infrastructure`

**Được:** `Common` cho một chỗ để guard clause và extension method; `Persistence` cô lập tầng dữ liệu để "đổi provider mà không đụng hạ tầng khác".

**Vì sao loại — hai lý do khác nhau:**

- **`Core.Common` không có tiêu chí từ chối.** Một project tên "Common" không trả lời được câu *cái gì KHÔNG được vào đây*, vì cái gì cũng lý luận được là dùng chung. Không có tiêu chí từ chối thì nó thành ngăn kéo rác — và ngăn kéo rác là thứ mọi tầng đều tham chiếu, tức một cạnh trong đồ thị phụ thuộc mà không luật nào chặn được. Guard clause và extension method thuộc về `Domain`, nơi chúng có ngữ cảnh.
- **`Core.Persistence` bán một lợi ích gần như không bao giờ đến.** Đổi provider DB ở hệ tầm trung là việc hầu như không xảy ra; và nếu xảy ra, phần đau không nằm ở chỗ chia project mà nằm ở SQL thô, kiểu dữ liệu, hành vi transaction. Chi phí thì trả ngay và trả mãi: thêm một project, và thêm câu hỏi *file này bỏ vào đâu* cho mọi file mới.

### Phương án C — Gộp hết vào một `Core.dll`

**Được:** gọn nhất, không phải quản đồ thị tham chiếu, build nhanh.

**Vì sao loại:** mất hàng rào compiler. Tách project là **cách duy nhất** để compiler ép luật layering — `Core.Domain` không tham chiếu `Core.Infrastructure` thì code trong Domain **không thể** chạm `DbContext`, dù người viết có muốn. Hàng rào này chi phí bằng không khi chạy, không bao giờ hỏng, và không phụ thuộc ai nhớ luật. Luật viết trong tài liệu thì người ta quên; luật viết trong đồ thị tham chiếu thì compiler nhắc.

## Hệ quả

### Tích cực

- **Core tự đứng được.** `Program.cs` của một dự án mới còn lại vài dòng, và một bugfix hạ tầng sửa một chỗ là mọi dự án nhận được.
- **Luật layering do compiler ép**, không do review ép.
- **Mỗi project trả lời được câu "cái gì không được vào đây"**, nên chỗ đặt file mới ít khi phải bàn.

### Tiêu cực — cái giá thật

- **Năm project là năm lần phải quyết chỗ đặt file**, và có những file thật sự nằm ở vùng xám — ví dụ một lớp tiện ích định dạng chuỗi mà cả Application lẫn Web đều cần. Không có `Common` nghĩa là phải quyết dứt khoát chứ không có chỗ để né. Đó là chủ ý, nhưng nó tốn thời gian tranh luận, và tranh luận đó sẽ lặp lại.
- **`Core.Web` kéo phụ thuộc ASP.NET Core vào bên trong Core.** Từ nay Core ràng vào một web framework cụ thể; đổi sang stack HTTP khác là viết lại project này.
- **Build lâu hơn** một project gộp, và mỗi project là thêm một dòng phải bảo trì trong solution cùng trong file quản lý phiên bản package.
- **`Core.Contracts` có nguy cơ trở thành ngăn kéo rác thứ hai.** Nó chỉ được chứa thứ **từ hai module trở lên** cùng cần. Một DTO chỉ một module dùng mà bị đẩy vào đây là đã bắt đầu đi vào đúng vết xe của `Common` — và lần này không có ADR nào cấm, chỉ có kỷ luật review.

## Liên quan

- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2 — bảng chi tiết từng project chứa gì, cấm chứa gì
- [`../RULES.md`](../RULES.md) — luật A1, A2, A6, A7
- [`0001-modular-monolith.md`](0001-modular-monolith.md) — bối cảnh Core ↔ Module
