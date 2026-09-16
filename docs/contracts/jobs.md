---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Theo dõi việc chạy nền

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Card trong file này mang `Status: DRAFT`; thi công ở pha **B4** ([`../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)).
>
> Khuôn *"trả mã việc rồi hỏi trạng thái"*: [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §10.3. Bảng `core.job`: [`../database/schema-core.md`](../database/schema-core.md) §9.8. Khoá cấu hình (`Core:Jobs:MaxConcurrent`): [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §6. Ngữ cảnh đơn vị và người kích hoạt của việc nền: [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1. Envelope và mã lỗi: [`README.md`](README.md).

---

## 1. `GET /api/v1/core/jobs/{id}`

**Status:** DRAFT
**Quyền:** `[AuthenticatedOnly("Việc nền của chính người đã khởi tạo — handler kiểm, không có khoá quyền riêng")]`

Trạng thái và kết quả của một việc chạy nền mà `{id}` là `jobId` endpoint khởi tạo đã trả (ví dụ nhập tệp lớn — [`exports.md`](exports.md) §2).

### Response 200

```json
{
  "success": true,
  "data": {
    "id": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234",
    "kind": "import",
    "status": "succeeded",
    "progress": 100,
    "createdAt": "2026-09-15T02:10:04.000Z",
    "finishedAt": "2026-09-15T02:11:37.000Z",
    "result": { "totalRows": 12000, "succeeded": 11987, "failed": [ { "row": 17, "code": "CORE.VALIDATION.FAILED", "field": "Email", "message": "Email sai định dạng" } ] },
    "resultFileId": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a5678",
    "error": null
  },
  "error": null,
  "traceId": "9c1d3b7e5f0a4c2d8e6b1a3f5d7c9e0b"
}
```

Mỗi field ứng với một cột của `core.job` ([`../database/schema-core.md`](../database/schema-core.md) §9.8): `kind` ↔ `type`, `createdAt` ↔ `created_at`, `resultFileId` ↔ `result_file_id`, các field còn lại cùng tên.

| Field | Kiểu | Ghi chú |
| --- | --- | --- |
| `kind` | string | Loại việc, do luồng khởi tạo đặt |
| `status` | enum | `queued` · `running` · `succeeded` · `failed` · `cancelled` — đúng tập giá trị của cột `status` |
| `progress` | number | 0–100 |
| `finishedAt` | string \| null | `null` khi chưa kết thúc |
| `result` | object \| null | Chỉ có khi `succeeded`; hình dạng theo loại việc — với nhập tệp là mục *Kết quả việc nhập* của [`exports.md`](exports.md) §2 |
| `resultFileId` | string \| null | Tệp kết quả, tải qua [`files.md`](files.md) §2; `null` khi việc không sinh tệp |
| `error` | object \| null | Chỉ có khi `failed`: `{ code, message, messageParams }` theo hợp đồng thông điệp lỗi ([`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §7) |

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.JOB.NOT_FOUND` | `NotFound` | 404 | Không có việc đó, **hoặc** việc không do người gọi khởi tạo — gộp hai ca, cùng khuôn luật M7 |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |

### Ghi chú

**FE hỏi theo chu kỳ, không giữ kết nối.** Dừng hỏi khi `status` là `succeeded`, `failed` hoặc `cancelled`.

**Việc kết thúc ⇒ một thông báo tới người khởi tạo, qua outbox** ([`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §1.2) — người dùng rời màn vẫn biết việc đã xong.

**Không có endpoint huỷ hay chạy lại ở v1.** Việc hỏng thì người dùng sửa dữ liệu rồi khởi tạo việc mới; `cancelled` là giá trị dành sẵn của cột, chưa có đường nào đặt nó.
