---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Xuất và nhập dữ liệu

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu 2026-09-21, **chỉ card §1 và chỉ ở mức thân, header, mã lỗi — trên host thật với repository trong bộ nhớ**). Mọi card giữ
> `Status: DRAFT`: chưa endpoint nào được gọi thử trên PostgreSQL thật ([`README.md`](README.md) §3).
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | Card §1 có **một** bản cài: `src/BE/Core/CoreAndSkill.Core.Web/Controllers/UsersController.cs` action `Export` (`GET /api/v1/core/users/export`, quyền `core.user.export`). Thân là chính tệp, `Content-Disposition: attachment`, `X-Content-Type-Options: nosniff`, `Cache-Control: no-store`; ô công thức bị vô hiệu hoá; `CORE.EXPORT.TOO_MANY_ROWS` 422 nêu cả số dòng thực tế và giới hạn, không ghi tệp và không ghi nhật ký; đã kiểm ở `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Web/B4ExportEndpointTests.cs` | Tệp xuất **khớp bộ lọc đang xem** chỉ chứng minh được trên DB thật: `ExportUsersDatabaseTests.cs` (`RequiresDocker`) — **chưa chạy** |
> | `format` vắng thì mặc định `csv`; giá trị ngoài `csv` / `xlsx` là `CORE.VALIDATION.FAILED` | Giữ nguyên |
> | Card §1 **kiểm `X-XSRF-TOKEN` như lệnh ghi**: action `Export` mang `[RequireAntiforgery]`, `AntiforgeryValidationMiddleware` đọc `GetMetadata<RequireAntiforgeryAttribute>()` ở bước 1, `Content-Disposition` nằm trong `.WithExposedHeaders("Retry-After", "Content-Disposition")`. Thiếu token → 403 `CORE.AUTH.CSRF_REJECTED` và **không dòng nhật ký nào**; `Origin` lạ kèm token đúng → 403 `CORE.AUTH.ORIGIN_REJECTED`; chưa đăng nhập → 401. Kiểm qua HTTP ở `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Web/B4ExportEndpointTests.cs`, 14/14 xanh (đối chiếu 2026-09-23) — [ADR-0062](../adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md) | FE tải bằng blob theo [`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §6.4 — **chưa có màn nào gọi** |
> | Card §2 (nhập): Core có cơ chế (`StartImportCommand`, `ImportJobExecutor`) nhưng **không có controller nhập nào** — endpoint nhập là của từng tài nguyên/module. `result` của việc nhập mang thêm `failedCount` và cắt `failed` ở `Core:Import:MaxFailedRowsInResult` dòng | Endpoint nhập đầu tiên do module dựng theo khuôn này |
>
> Cơ chế, định dạng, bẫy của tệp bảng tính: [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md). Envelope và mã lỗi: [`README.md`](README.md).

> **Đây là card KHUÔN, không phải card của một endpoint cụ thể.** Mỗi tài nguyên có endpoint xuất/nhập của riêng nó, đặt dưới khu của tài nguyên đó, và **phải** theo đúng khuôn dưới đây. Module không tự nghĩ khuôn khác.
>
> Nhánh thành công dựng bằng `ApiControllerBase.HandleExport(result)` — [ADR-0064](../adr/0064-nhanh-tra-tep-di-qua-apicontrollerbase.md). ✅ CÓ THẬT (đối chiếu 2026-09-23): `src/BE/Core/CoreAndSkill.Core.Web/Controllers/ApiControllerBase.cs` (chuỗi `protected IActionResult HandleExport(Result<ExportFile> result)`), bản dùng thật ở `src/BE/Core/CoreAndSkill.Core.Web/Controllers/UsersController.cs` (chuỗi `=> HandleExport(await mediator.Send(command, ct))`). Module **không** tự viết `IActionResult`: ba header bắt buộc (`Content-Disposition`, `X-Content-Type-Options: nosniff`, `Cache-Control: no-store`) và quy tắc đặt tên tệp nằm trong lớp kết quả của Core, không trong tài liệu — `ExportFileResult` giữ `internal` nên module không với tới được.

---

## 1. `GET /api/v1/<khu>/<tài nguyên>/export`

**Status:** DRAFT
**Quyền:** quyền **xuất riêng** của tài nguyên đó — ví dụ `core.user.export`, không dùng lại quyền đọc

Xuất danh sách theo **đúng bộ lọc đang xem**.

### Request

Nhận **cùng tập tham số truy vấn với endpoint danh sách** của tài nguyên đó ([`README.md`](README.md) §8) — trừ `page` và `pageSize`, vì xuất là xuất cả tập.

Thêm một tham số: `format` — `csv` hoặc `xlsx`.

**Header `X-XSRF-TOKEN` là bắt buộc, như một lệnh ghi** — dù method là `GET`. Mỗi lần xuất ghi nhật ký kiểm toán, tức `GET` này có tác dụng phụ; đây là ngoại lệ có tên của luật *antiforgery chỉ áp cho lệnh ghi* ở [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.2, quyết định ở [ADR-0062](../adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md). Hệ quả cho người gọi: **không mở được bằng URL** — không dán link, không bookmark, không `<a href>`; FE tải qua `HttpClient` thành blob ([`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §6.4). Endpoint xuất của mỗi tài nguyên **phải** mang `[RequireAntiforgery]`; thiếu dấu thì middleware coi nó như mọi `GET` khác — không có ngoại lệ ngầm, và không có gì báo.

### Response 200

Thân là chính tệp, kèm `Content-Disposition: attachment`. Tên tệp gồm tên tài nguyên và thời điểm xuất; **header này phải nằm trong `Access-Control-Expose-Headers`** ([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.3) — FE ở origin khác không đọc được nó nếu thiếu, và lỗi đó không có thông báo. Nhánh lỗi vẫn là envelope — ngoại lệ có tên ở [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §2.2 luật 1.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.EXPORT.TOO_MANY_ROWS` | `BusinessRule` | 422 | Vượt giới hạn số dòng của đường đồng bộ. Thông báo **phải** nói rõ giới hạn và gợi ý siết bộ lọc |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | `format` ngoài hai giá trị cho phép; tham số lọc sai khuôn |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập — vẫn 401, không 403, cho phiên hết hạn |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` — kể cả khi mở bằng URL |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Không có quyền xuất |

### Ghi chú

**Quyền xuất tách khỏi quyền đọc, có chủ đích.** Xem một trang danh sách và mang cả tập dữ liệu ra ngoài là hai mức rủi ro khác nhau.

**Mọi lần xuất ghi nhật ký kiểm toán**: ai xuất, bộ lọc nào, bao nhiêu dòng ([`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §5). Chính dòng nhật ký này là lý do endpoint đòi token: cookie `SameSite=Lax` vẫn đi theo một cú bấm liên kết từ trang lạ, và không có token thì dòng nhật ký mang tên người bấm là dòng giả ([ADR-0062](../adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md)).

**Nghĩa của dòng nhật ký — ba câu, [ADR-0066](../adr/0066-dong-nhat-ky-xuat-ghi-luc-cho-phep-khong-luc-giao-xong.md).** Endpoint xuất của mọi tài nguyên theo đúng ba câu này; không module nào được ghi dòng sau khi giao xong.

1. **Dòng có mặt nghĩa là *đã cho phép kéo tập dữ liệu khớp bộ lọc này ra*, không phải *người dùng đã nhận đủ tệp*.** Nó ghi **trong giao dịch của lệnh**, trước khi byte đầu tiên ra luồng — mất kết nối giữa chừng vẫn để lại dòng.
2. **`rowCount` là số dòng khớp bộ lọc lúc đếm**, không phải số dòng thực ghi vào tệp. Dữ liệu đổi giữa lúc đếm và lúc ghi thì hai số khác nhau, theo cả hai chiều; đường ghi còn bị chặn lần nữa ở trần số dòng.
3. **Xuất hỏng giữa chừng rồi xuất lại để lại hai dòng** — hành vi đúng, không phải lỗi cần gộp.

✅ CÓ THẬT (đối chiếu 2026-09-23): `src/BE/Core/CoreAndSkill.Core.Application/Users/ExportUsersCommand.cs` khai `: ICommand<ExportFile>` nên `TransactionBehavior` bọc; `src/BE/Core/CoreAndSkill.Core.Application/Users/ExportUsersCommandHandler.cs` gọi `auditTrail.Record(new AuditEntry(` trong thân handler, lấy số từ `var rowCount = await users.CountAsync(criteria, ct);`, rồi trả một `ExportFile` mà thân chỉ chạy ở `src/BE/Core/CoreAndSkill.Core.Web/Http/ExportFileResult.cs` (`await file.WriteToAsync(response.Body`) — tức sau commit.

**Ô bắt đầu bằng ký tự công thức phải bị vô hiệu hoá** trước khi ghi vào tệp — bẫy đặc thù của tệp bảng tính, xem file cơ chế.

**Xuất chạy nền chưa có ở v1.** Vì vậy giới hạn số dòng ở trên là bắt buộc, không phải tuỳ chọn: nó là thứ giữ cho một lần xuất không treo cả tiến trình. Giá trị và khoá cấu hình của giới hạn: [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §5.3.

**Tệp xuất không lưu lại** — ghi thẳng ra luồng phản hồi ([`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §5.2); mất kết nối giữa chừng thì xuất lại.

---

## 2. `POST /api/v1/<khu>/<tài nguyên>/import`

**Status:** DRAFT
**Quyền:** quyền nhập riêng của tài nguyên đó

Nhận một tệp, kiểm trần số dòng, rồi **luôn** xử lý theo dòng ở một việc chạy nền — không có đường đồng bộ.

### Request

`multipart/form-data` với phần `file`.

### Response 200

```json
{
  "success": true,
  "data": { "jobId": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234" },
  "error": null,
  "traceId": "a3330d668e235ba9f40ef96936a1769b"
}
```

`jobId` là `{id}` của [`jobs.md`](jobs.md) §1. Trả 200, không 202 ([`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §10.3, ràng buộc 1).

### Kết quả việc nhập

Là `result` của [`jobs.md`](jobs.md) §1 khi việc `succeeded`:

```json
{
  "totalRows": 120,
  "succeeded": 118,
  "failedCount": 2,
  "failed": [
    { "row": 17, "code": "CORE.VALIDATION.FAILED", "field": "Email", "message": "Email sai định dạng" },
    { "row": 92, "code": "CORE.USER.DUPLICATE_ROLE_ENTRY", "field": null, "message": "Vai trò trùng" }
  ]
}
```

**Nhập một phần vẫn là `succeeded`.** Dòng hỏng nằm ở `failed`, không ở `error` của việc — việc chỉ `failed` khi tệp không đọc được. Danh sách dòng hỏng còn tải về được qua `resultFileId` ([`jobs.md`](jobs.md) §1).

`failedCount` là **tổng thật** số dòng hỏng; `failed` chỉ chứa tối đa `Core:Import:MaxFailedRowsInResult` dòng đầu tiên ([`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §6). `failedCount` lớn hơn độ dài `failed` nghĩa là danh sách trên màn hình bị cắt — danh sách đầy đủ nằm ở tệp `resultFileId`.

`row` là **số dòng của tệp gốc**, kể cả dòng tiêu đề — người dùng mở tệp lên và nhảy tới đúng dòng đó.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.IMPORT.COLUMNS_MISMATCH` | `Validation` | 400 | Thiếu cột bắt buộc, hoặc tiêu đề không khớp mẫu — kiểm cùng lượt đếm dòng, trước khi tạo việc |
| `CORE.IMPORT.TOO_MANY_ROWS` | `BusinessRule` | 422 | Vượt trần `Core:Import:MaxRows` — đếm **trước** khi tạo việc; giá trị ở [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §6 |
| `CORE.IMPORT.DUPLICATE_ROW` | — | — | **Không phải lỗi của request** — mã của một phần tử `failed` trong kết quả việc: dòng trùng khoá tự nhiên với bản ghi đã có hoặc với dòng trước trong cùng tệp, bị bỏ qua |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu tệp, tệp rỗng, sai định dạng |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Không có quyền nhập |

### Ghi chú

**Một dòng lỗi không được kéo theo dòng sau.** Sau mỗi dòng lỗi phải huỷ thay đổi đang theo dõi, nếu không thì dòng kế tiếp mang theo trạng thái bẩn của dòng trước — [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §4.2.

**Chống trùng theo khoá tự nhiên do module khai** cho từng luồng nhập: dòng trùng **bị bỏ qua**, không ghi đè, và xuất hiện trong `failed` với mã `CORE.IMPORT.DUPLICATE_ROW` — nhập lại cùng một tệp không nhân đôi dữ liệu. Khoá tự nhiên là phần **bắt buộc** của định nghĩa một luồng nhập.

**Tệp gốc giữ tới khi việc kết thúc**; khoá cấu hình trần số dòng và số việc chạy đồng thời: [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §6.
