---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Xuất và nhập dữ liệu

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi card trong file này mang `Status: DRAFT`.
>
> Cơ chế, định dạng, bẫy của tệp bảng tính: [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md). Envelope và mã lỗi: [`README.md`](README.md).

> **Đây là card KHUÔN, không phải card của một endpoint cụ thể.** Mỗi tài nguyên có endpoint xuất/nhập của riêng nó, đặt dưới khu của tài nguyên đó, và **phải** theo đúng khuôn dưới đây. Module không tự nghĩ khuôn khác.

---

## 1. `GET /api/v1/<khu>/<tài nguyên>/export`

**Status:** DRAFT
**Quyền:** quyền **xuất riêng** của tài nguyên đó — ví dụ `core.user.export`, không dùng lại quyền đọc

Xuất danh sách theo **đúng bộ lọc đang xem**.

### Request

Nhận **cùng tập tham số truy vấn với endpoint danh sách** của tài nguyên đó ([`README.md`](README.md) §8) — trừ `page` và `pageSize`, vì xuất là xuất cả tập.

Thêm một tham số: `format` — `csv` hoặc `xlsx`.

### Response 200

Thân là chính tệp, kèm `Content-Disposition: attachment`. Tên tệp gồm tên tài nguyên và thời điểm xuất.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.EXPORT.TOO_MANY_ROWS` | `BusinessRule` | 422 | Vượt giới hạn số dòng của đường đồng bộ. Thông báo **phải** nói rõ giới hạn và gợi ý siết bộ lọc |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | `format` ngoài hai giá trị cho phép; tham số lọc sai khuôn |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.FORBIDDEN` | Không có quyền xuất |

### Ghi chú

**Quyền xuất tách khỏi quyền đọc, có chủ đích.** Xem một trang danh sách và mang cả tập dữ liệu ra ngoài là hai mức rủi ro khác nhau.

**Mọi lần xuất ghi nhật ký kiểm toán**: ai xuất, bộ lọc nào, bao nhiêu dòng ([`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §5).

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
  "failed": [
    { "row": 17, "code": "CORE.VALIDATION.FAILED", "field": "Email", "message": "Email sai định dạng" },
    { "row": 92, "code": "CORE.USER.DUPLICATE_ROLE_ENTRY", "field": null, "message": "Vai trò trùng" }
  ]
}
```

**Nhập một phần vẫn là `succeeded`.** Dòng hỏng nằm ở `failed`, không ở `error` của việc — việc chỉ `failed` khi tệp không đọc được. Danh sách dòng hỏng còn tải về được qua `resultFileId` ([`jobs.md`](jobs.md) §1).

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
