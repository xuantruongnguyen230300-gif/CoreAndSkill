---
kind: luat
scope: core
verified: chua-doi-chieu
---

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Bốn behavior dưới đây chưa tồn tại trong `src/BE` — xác nhận
> bằng cách không tìm thấy `class LoggingBehavior`/`PerformanceBehavior`/`CachingBehavior`/
> `AuthorizationBehavior` ở đâu trong `Core.Application`.

# Phần chưa thi công — `be-cqrs-handler.md`

> File này giữ đặc tả cho thứ [`be-cqrs-handler.md`](be-cqrs-handler.md) mô tả nhưng chưa ai xây.
> Khi một behavior được viết xong, chuyển đúng phần của nó về lại file gốc (kèm bằng chứng đối
> chiếu) và xoá khỏi đây.

---

## 5.5 Vì sao CHƯA có Logging / Performance / Caching behavior

Nguyên tắc: **thêm sau khi đo, không thêm để phòng xa.**

| Behavior thường thấy | Điều kiện để thêm |
| --- | --- |
| `LoggingBehavior` | Khi có nhu cầu log **theo use case** mà tầng HTTP không thấy được (ví dụ tham số đã giải mã) |
| `PerformanceBehavior` | Khi đã có tracing và vẫn cần mốc đo ở ranh giới use case |
| `CachingBehavior` | Ba điều kiện bắt buộc ở [`be-performance.md`](be-performance.md) §8.3 |
| `AuthorizationBehavior` | Khi cần phân quyền theo **từng bản ghi** mà `RequirePermissionAttribute` không biểu diễn được |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-cqrs-handler.md`](../wiki-core/be/ly-do/be-cqrs-handler.md) §5.5
