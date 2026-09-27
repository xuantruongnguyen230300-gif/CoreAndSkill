---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `diagnostics.md` — endpoint thử của B0

> Không phải hợp đồng nghiệp vụ. Endpoint này tồn tại để tự-kiểm cầu nối `Result` → HTTP và
> `IExceptionHandler` trước khi CQRS/Identity (B1) tồn tại —
> [`../wiki-core/be/trien-khai/01-b0-nen-mong.md`](../wiki-core/be/trien-khai/01-b0-nen-mong.md) §2
> bước 4. Card này ban đầu tồn tại chỉ vì [`../RULES.md`](../RULES.md) đòi mọi chú thích trong
> `src/` trỏ `docs/` phải trỏ tới một file có thật (§10) — nhưng nghiệm thu F0
> ([`../wiki-core/fe/trien-khai/01-f0-nen-mong.md`](../wiki-core/fe/trien-khai/01-f0-nen-mong.md) §7)
> lại **đòi đúng** FE phải gọi thật endpoint này ở nhánh lỗi để đóng pha, nên trên thực tế nó **có**
> bị gọi — không phải trong một luồng nghiệp vụ, mà trong luồng tự-kiểm của cả hai phía.

## `GET /api/v1/core/diagnostics/probe`

**Status:** IMPLEMENTED — xác nhận bằng lời gọi thật hai chiều độc lập ngày 2026-09-16:
`backend-expert` gọi qua `curl` (cả ba nhánh), `frontend-expert` gọi qua trình duyệt thật
(ChromeHeadless) và nhúng response nhánh lỗi thật vào `error.interceptor.spec.ts`. Ba khối JSON
dưới đây là response **thật**, không phải ví dụ dựng sẵn — trừ `serverTimeUtc`/`traceId` đã thay
bằng giá trị mẫu vì hai trường đó đổi theo từng lần gọi.
**Môi trường:** chỉ ngoài Production — ở Production là 404 như tuyến không tồn tại (mục *Ghi chú*).
**Quyền:** không cần đăng nhập (`[AllowAnonymous]`) — không lộ dữ liệu nghiệp vụ, chỉ lặp lại
tham số đầu vào.

Trả một trong ba nhánh theo query `outcome`, để chứng minh cả ba đường Result → HTTP đều đúng
khuôn.

### Request

| Query | Kiểu | Hợp lệ | Bỏ trống |
| --- | --- | --- | --- |
| `outcome` | string | `failure`, `exception`, hoặc vắng mặt | Nhánh thành công |

### Response 200 — `outcome` vắng mặt hoặc giá trị khác `failure`/`exception`

```json
{
  "success": true,
  "data": { "message": "pong", "serverTimeUtc": "2026-09-16T10:00:00Z" },
  "error": null,
  "traceId": "a6a8bce037eb102c3057e2bc84b48611"
}
```

### Lỗi

Bảng 1 — mã riêng của endpoint:

| Mã | `ErrorType` | HTTP | Khi nào |
| --- | --- | --- | --- |
| `CORE.DIAGNOSTICS.PROBE_FAILURE_REQUESTED` | `BusinessRule` | 422 | `outcome=failure` |

Bảng 2 — mã dùng chung, chủ ở [`auth.md`](auth.md) §11:

| Mã | Khi nào xảy ra ở endpoint này |
| --- | --- |
| `CORE.SYSTEM.UNEXPECTED` | `outcome=exception` — ném `InvalidOperationException` mô phỏng để chứng minh `IExceptionHandler` không lộ stack trace |
| `CORE.ROUTE.NOT_FOUND` | Gọi một tuyến không tồn tại dưới `/api/v1/core/` |
| `CORE.ROUTE.METHOD_NOT_ALLOWED` | Gọi `probe` bằng verb khác `GET` |

### Ghi chú

- `traceId` phản chiếu header `traceparent` khi FE/công cụ gọi thử gửi kèm — dùng để đối chiếu log
  xuyên tầng.
- Endpoint này **không** đi qua pipeline CQRS/MediatR — B0 chưa dựng hạ tầng đó
  ([`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §1, §5 chưa áp cho B0). Controller
  gọi thẳng `DiagnosticsProbe`, giữ đúng khuôn `Result<T>` + catalog lỗi.
- **Chỉ có ngoài Production** — người dùng chốt ngày 2026-09-21. Ở Production tuyến không được
  đăng ký: mọi lời gọi, kể cả `outcome=exception`, nhận **404 `CORE.ROUTE.NOT_FOUND`** giống hệt một
  đường dẫn không tồn tại, không có hình dạng riêng nào cho biết endpoint có ở đó. Mọi môi trường
  khác Production (Development, Testing, Staging…) giữ nguyên ba nhánh ở trên. Lý do: một endpoint
  ẩn danh mà ai cũng gọi được để sinh 500 và một dòng log lỗi mỗi lần không có chỗ trên host thật.
  Dòng của endpoint trong allowlist ẩn danh vẫn giữ — cổng S4 quét attribute, không quét môi trường.
