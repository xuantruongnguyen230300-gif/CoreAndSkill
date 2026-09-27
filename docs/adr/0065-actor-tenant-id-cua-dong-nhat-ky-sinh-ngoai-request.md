---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0065 — Dòng nhật ký kiểm toán sinh ngoài request để `actor_tenant_id` rỗng: miễn trừ có điều kiện, kèm mốc mở lại

> **Trạng thái:** Đã chấp nhận (2026-09-23)

## Bối cảnh

Luật **M14** ([`../RULES.md`](../RULES.md) §9) buộc dòng `core.audit_log` mà **người thực hiện thuộc đơn vị khác** phải mang `actor_tenant_id`; giá trị khác `null` ở cột đó chính là dấu **xuyên đơn vị** ([`../database/schema-core.md`](../database/schema-core.md) §9.4 điểm 4). Cùng mục đó chốt nguồn của giá trị: **claim đơn vị của phiếu xác thực**, không phải phạm vi đơn vị đang mở — vì lúc ghi, phạm vi đã là đơn vị đích.

Kiến trúc sư đối chiếu ngày 2026-09-23: mã chạy **ngoài request** không có phiếu xác thực nào để đọc claim. `IExecutionContextScope` mang theo `(TenantId, UserId, UserName)` của **công việc** — đơn vị ở đây là đơn vị của dòng dữ liệu đang xử lý, không phải đơn vị của người đã yêu cầu công việc đó ([`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) mục *Danh tính và đơn vị khi không có request*). Hệ quả: mọi dòng nhật ký sinh từ job nền, bộ phát outbox và lệnh dòng lệnh đều có `actor_tenant_id` rỗng, và M14 đọc nguyên văn thì chúng là vi phạm.

Ba nhóm ca thật hôm nay:

| Nhóm | Người yêu cầu | `actor_tenant_id` đúng phải là gì |
| --- | --- | --- |
| Job định kỳ, dọn dữ liệu, khôi phục việc dở lúc khởi động | Không có người | Rỗng — không có "người thực hiện" nào để so đơn vị |
| Lệnh dòng lệnh (`core outbox-replay`, bootstrap) | Người vận hành ở console, **không** có tài khoản trong phiên | Rỗng; danh tính nằm ở `actor_display` là tên lệnh |
| Việc hoãn do **một người dùng** yêu cầu (job nhập, sự kiện outbox sinh từ một request) | Có | Đơn vị của người yêu cầu — **khi khác** đơn vị của dòng |

Nhóm ba là chỗ M14 thật sự có thể mất dấu. Hôm nay **chưa có** thao tác xuyên đơn vị nào chạy hoãn: mọi ca xuyên đơn vị của [`0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) chạy đồng bộ trong request, nơi claim còn nguyên.

## Quyết định

Kiến trúc sư chốt:

1. **M14 giữ nguyên hiệu lực cho mọi dòng sinh trong một request.** Không nới.
2. **Dòng sinh ngoài request được để `actor_tenant_id` rỗng — miễn trừ có tên, có điều kiện:** hợp lệ khi **không có người yêu cầu** (`actor_user_id` rỗng), hoặc khi đơn vị của người yêu cầu **bằng** `tenant_id` của chính dòng. Rỗng vì "không đọc được claim" thì **không** hợp lệ.
3. **Điều kiện mở lại, kiểm được:** ngay khi có thao tác **xuyên đơn vị** đầu tiên chạy hoãn — tức một công việc do người của đơn vị A yêu cầu mà ghi dòng nhật ký ở đơn vị B — thì đơn vị của người yêu cầu phải **đi theo dữ liệu** như `tenant_id` và `triggered_by_user_id` đã đi (thêm cột ở dòng outbox/job, thêm trường ở ảnh chụp ngữ cảnh), và điều 2 hết áp cho ca đó. Đó là một ADR mới, không phải một lần sửa lặng lẽ.
4. Miễn trừ này **không** mở cho `AuditLogInterceptor` trong request: nó vẫn đọc claim, vẫn theo M14 nguyên văn.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Mang đơn vị của người yêu cầu theo mọi việc hoãn ngay bây giờ

**Được:** M14 đúng nguyên văn ở mọi đường ghi, không ngoại lệ nào phải nhớ.

**Mất:** một cột mới ở `core.outbox` và `core.job`, một trường mới ở ảnh chụp ngữ cảnh, và mọi chỗ `Enter(...)` phải truyền thêm — cho một tập ca **rỗng** hôm nay. Cột đó sẽ mang `null` ở mọi dòng trong nhiều tháng, và một cột luôn rỗng là cột không ai kiểm được là đúng hay hỏng.

**Vì sao loại:** trả chi phí lược đồ dữ liệu ngay cho một nhu cầu chưa tồn tại, đúng khuôn "tối ưu trước khi đo". Điều kiện mở lại ở quyết định 3 giữ nó không bị quên.

### Phương án B — Miễn trừ trắng cho mọi dòng ngoài request

**Vì sao loại:** nó biến "không có người thực hiện" và "có người thực hiện nhưng mất dấu" thành cùng một trạng thái rỗng. Khi nhóm ba xuất hiện, không gì báo — dấu xuyên đơn vị lặng lẽ biến mất khỏi đúng những dòng cần nó nhất.

### Phương án C — Suy `actor_tenant_id` từ phạm vi đơn vị đang mở

**Vì sao loại:** phạm vi lúc đó là đơn vị **đích**, nên cột sẽ bằng `tenant_id` của chính dòng — tức luôn rỗng theo định nghĩa cột, hoặc tệ hơn, ghi một giá trị **sai nghĩa**. §9.4 điểm 4 đã cấm đúng cách suy này cho đường trong request.

## Hệ quả

### Tích cực

- M14 nói đúng thứ nó ép được, và không còn đọc như một luật đang bị mọi job vi phạm.
- Ranh giới "rỗng hợp lệ" ↔ "rỗng vì mất dấu" thành một câu kiểm được, không phải cảm nhận.

### Tiêu cực

- **Miễn trừ này chưa có cổng.** Không gì chặn một PR tương lai hoãn một thao tác xuyên đơn vị rồi ghi nhật ký thiếu dấu; lớp bắt được là `core-reviewer` và người đọc. Vì vậy nó vào danh sách nợ ở [`../RULES.md`](../RULES.md) §10, không nằm im trong ADR này.
- **Điều kiện mở lại phụ thuộc người nhận ra nó.** Dấu hiệu: một `IBackgroundJobScheduler.Enqueue` hay một dòng outbox được tạo trong một request mà `ITenantContext` khác đơn vị của dòng dữ liệu đích.
- Nhật ký của lệnh dòng lệnh chỉ còn `actor_display` để quy trách nhiệm — đủ để biết *lệnh nào*, không đủ để biết *ai gõ*. Ai gõ nằm ở nhật ký của máy chủ, ngoài hệ thống này.

### Rút lui nếu sai

Làm phương án A: thêm cột mang đơn vị người yêu cầu vào `core.outbox`/`core.job`, điền từ ngữ cảnh lúc tạo, đọc lúc ghi nhật ký. Dữ liệu cũ giữ `null` — và `null` ở đó vẫn đúng nghĩa theo quyết định này.

## Liên quan

- [`../database/schema-core.md`](../database/schema-core.md) §9.4 điểm 4 — nghĩa của cột.
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) mục *Danh tính và đơn vị khi không có request* — thứ đi theo việc nền hôm nay.
- [`0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) — các thao tác xuyên đơn vị đang có, tất cả đồng bộ.
