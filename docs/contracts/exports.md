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
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | `format` ngoài hai giá trị cho phép; tham số lọc sai khuôn |
| `CORE.AUTH.FORBIDDEN` | `Forbidden` | 403 | Không có quyền xuất |
| `CORE.EXPORT.TOO_MANY_ROWS` | `BusinessRule` | 422 | Vượt giới hạn số dòng của đường đồng bộ. Thông báo **phải** nói rõ giới hạn và gợi ý siết bộ lọc |

### Ghi chú

**Quyền xuất tách khỏi quyền đọc, có chủ đích.** Xem một trang danh sách và mang cả tập dữ liệu ra ngoài là hai mức rủi ro khác nhau.

**Mọi lần xuất ghi nhật ký kiểm toán**: ai xuất, bộ lọc nào, bao nhiêu dòng ([`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §5).

**Ô bắt đầu bằng ký tự công thức phải bị vô hiệu hoá** trước khi ghi vào tệp — bẫy đặc thù của tệp bảng tính, xem file cơ chế.

**Xuất chạy nền chưa có ở v1.** Vì vậy giới hạn số dòng ở trên là bắt buộc, không phải tuỳ chọn: nó là thứ giữ cho một lần xuất không treo cả tiến trình.

---

## 2. `POST /api/v1/<khu>/<tài nguyên>/import`

**Status:** DRAFT
**Quyền:** quyền nhập riêng của tài nguyên đó

Nhận một tệp, xử lý **theo dòng**, trả báo cáo theo dòng.

### Request

`multipart/form-data` với phần `file`.

### Response 200

```json
{
  "success": true,
  "data": {
    "totalRows": 120,
    "succeeded": 118,
    "failed": [
      { "row": 17, "code": "CORE.VALIDATION.FAILED", "field": "Email", "message": "Email sai định dạng" },
      { "row": 92, "code": "CORE.USER.DUPLICATE_ROLE_ENTRY", "field": null, "message": "Vai trò trùng" }
    ]
  },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000032"
}
```

**Nhập một phần vẫn là `success: true`.** Kết quả nằm ở `data.failed`, không ở `error` — vì bản thân lời gọi đã thành công. Chỉ khi tệp không đọc được thì mới là nhánh lỗi.

`row` là **số dòng của tệp gốc**, kể cả dòng tiêu đề — người dùng mở tệp lên và nhảy tới đúng dòng đó.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | Thiếu tệp, tệp rỗng, sai định dạng |
| `CORE.IMPORT.COLUMNS_MISMATCH` | `Validation` | 400 | Thiếu cột bắt buộc, hoặc tiêu đề không khớp mẫu |
| `CORE.AUTH.FORBIDDEN` | `Forbidden` | 403 | Không có quyền nhập |
| `CORE.IMPORT.TOO_MANY_ROWS` | `BusinessRule` | 422 | Vượt giới hạn số dòng của đường đồng bộ |

### Ghi chú

**Một dòng lỗi không được kéo theo dòng sau.** Sau mỗi dòng lỗi phải huỷ thay đổi đang theo dõi, nếu không thì dòng kế tiếp mang theo trạng thái bẩn của dòng trước — [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §4.2.

**Chính sách nhập lại khai tường minh cho từng luồng**: bỏ qua dòng đã có, ghi đè, hay báo lỗi. Không có mặc định ngầm.
