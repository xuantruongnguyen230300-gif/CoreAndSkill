---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Theo dõi việc chạy nền

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu 2026-09-21, **ở mức định tuyến, thân và mã lỗi — trên host thật với repository trong bộ nhớ**). Card giữ `Status: DRAFT`: chưa được gọi thử trên PostgreSQL thật ([`README.md`](README.md) §3). Thi công ở pha **B4** ([`../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)).
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | `src/BE/Core/CoreAndSkill.Core.Web/Controllers/JobsController.cs` có action `GetById` — khớp route §1; các field, việc người khác/không có cùng trả `CORE.JOB.NOT_FOUND`, và tệp kết quả của việc chỉ người khởi tạo tải được: kiểm qua HTTP ở `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Web/B4JobsAndNotificationsEndpointTests.cs` | Gọi thử thật trên DB, rồi mới lật `Status:` |
> | Vòng đời việc (queued → running → succeeded/failed), khôi phục việc dở dang lúc khởi động, việc kết thúc phát dòng outbox: viết thành test ở `OutboxDatabaseTests.cs`, `ImportJobDatabaseTests.cs`, `JobRecoveryDatabaseTests.cs` (`RequiresDocker`) — **chưa chạy** | Chạy được ở CI có Docker |
> | Ba mã đi vào `core.job.error` khai ở `src/BE/Core/CoreAndSkill.Core.Application/Jobs/JobErrors.cs` (`Ba mã dưới chỉ đi vào cột`); nhánh ngoại lệ ghi `CORE.JOB.UNEXPECTED` ở `src/BE/Core/CoreAndSkill.Core.Application/Jobs/JobRunner.cs` (`JobOutcome.Failure(JobErrors.Unexpected)`) — đối chiếu 2026-09-23, kiểm bằng `src/BE/Tests/CoreAndSkill.Core.UnitTests/Jobs/JobRunnerTests.cs` (`ShouldNotContain(CommonErrors.Unexpected.Code`) | Giữ nguyên |
> | `result` của việc nhập mang thêm `failedCount` và **cắt** `failed` ở `Core:Import:MaxFailedRowsInResult` dòng — danh sách đầy đủ nằm ở tệp `resultFileId`; xem [`exports.md`](exports.md) §2 | Giữ nguyên |
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
| `error` | object \| null | Chỉ có khi `failed`: `{ code, message, messageParams }` theo hợp đồng thông điệp lỗi ([`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §7). Mã tra ở *Mã của việc hỏng* dưới — **không** tra ở bảng *Lỗi* |

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.JOB.NOT_FOUND` | `NotFound` | 404 | Không có việc đó, **hoặc** việc không do người gọi khởi tạo — gộp hai ca, cùng khuôn luật M7 |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |

### Mã của việc hỏng — nằm trong `data.error`, không phải lỗi của request

Ba mã dưới là giá trị `code` BE ghi vào cột `error` của `core.job` khi việc sang `failed`. Client gặp chúng trong `data.error` của chính phản hồi **200** ở trên. Chúng **không bao giờ** là `code` của một envelope thất bại, nên bảng này không có cột HTTP:

| `code` | Khi nào | `messageParams` |
| --- | --- | --- |
| `CORE.JOB.NO_EXECUTOR` | Không bộ chạy nào nhận loại việc — sai cấu hình, cần người can thiệp | `JobType` |
| `CORE.JOB.INTERRUPTED` | Tiến trình dừng khi việc đang chạy; dịch vụ khôi phục đánh dấu lúc khởi động lại. Người dùng khởi tạo lại việc | — |
| `CORE.JOB.UNEXPECTED` | Việc ném ngoại lệ ngoài dự kiến. Chi tiết chỉ nằm trong log máy chủ; thân phản hồi không mang thêm gì | — |

**`CORE.JOB.UNEXPECTED` không phải `CORE.SYSTEM.UNEXPECTED`.** Chuỗi thứ hai chỉ xuất hiện khi một *request* ra 500. Việc nền hỏng sau khi request tạo nó đã trả về thì mang chuỗi thứ nhất — hai sự kiện khác nhau, hai tên khác nhau: [`../adr/0077-loi-ngoai-le-cua-viec-nen-mang-ma-core-job-unexpected.md`](../adr/0077-loi-ngoai-le-cua-viec-nen-mang-ma-core-job-unexpected.md).

FE dựng câu hiển thị từ ba mã này đúng như với mọi mã khác — tra `code`, không đọc `message` ([`README.md`](README.md) §4).

### Ghi chú

**FE hỏi theo chu kỳ, không giữ kết nối.** Dừng hỏi khi `status` là `succeeded`, `failed` hoặc `cancelled`.

**Việc kết thúc ⇒ một thông báo tới người khởi tạo, qua outbox** ([`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §1.2) — người dùng rời màn vẫn biết việc đã xong.

**Không có endpoint huỷ hay chạy lại ở v1.** Việc hỏng thì người dùng sửa dữ liệu rồi khởi tạo việc mới; `cancelled` là giá trị dành sẵn của cột, chưa có đường nào đặt nó.
