---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Tệp đính kèm

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi card trong file này mang `Status: DRAFT`.
>
> Cơ chế lưu trữ và vòng đời tệp: [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md). Bảo mật khi nhận tệp: [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md) §9. Envelope, mã lỗi, bảo mật chung: [`README.md`](README.md).

---

## 1. `POST /api/v1/core/files`

**Status:** DRAFT
**Quyền:** `[AuthenticatedOnly("Tệp chưa gắn bản ghi nào — quyền kiểm lúc gắn vào bản ghi chủ")]` — quyền trên **bản ghi chủ** quyết định, xem Ghi chú

Nhận một tệp và trả về mã tệp để gắn vào bản ghi nghiệp vụ.

### Request

`multipart/form-data`:

| Phần | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `file` | ✅ | Nội dung tệp |
| `purpose` | ✅ | Mục đích lưu, do module khai. Quyết định thư mục đích và giới hạn áp dụng — lưu ở cột `purpose` của `core.file` ([`../database/schema-core.md`](../database/schema-core.md) §9.7) |

### Response 200

```json
{
  "success": true,
  "data": {
    "id": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234",
    "originalName": "quyet-dinh-12.pdf",
    "contentType": "application/pdf",
    "sizeBytes": 284913
  },
  "error": null,
  "traceId": "af591861109efa327ba9d93d738c7723"
}
```

`originalName` là tên **người dùng đặt**, chỉ dùng để hiển thị và để đặt tên lúc tải về. Tên thật trên đĩa do hệ thống sinh và không bao giờ trả ra ngoài.

Bốn field ứng với cột `id` · `original_name` · `content_type` · `size_bytes` của bảng `core.file` ([`../database/schema-core.md`](../database/schema-core.md) §9.7); `storage_key` và cặp `owner_table` / `owner_id` không đi trên dây.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.FILE.TOO_LARGE` | `Validation` | 400 | Vượt giới hạn dung lượng của `purpose` đó |
| `CORE.FILE.TYPE_NOT_ALLOWED` | `Validation` | 400 | Kiểu tệp ngoài danh sách cho phép của `purpose` đó |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu tệp, thiếu `purpose`, tệp rỗng |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.RATE_LIMIT.EXCEEDED` | Vượt giới hạn tần suất. Kèm header `Retry-After` |

Hai mã dung lượng và kiểu tệp cố ý dùng `Validation` → **400**, không dùng mã HTTP riêng cho tệp: ánh xạ loại lỗi sang HTTP nằm ở đúng một chỗ ([`README.md`](README.md) §5), và mở ngoại lệ ở đây là mở đường cho ngoại lệ kế tiếp.

---

## 2. `GET /api/v1/core/files/{id}`

**Status:** DRAFT
**Quyền:** `[AuthenticatedOnly("Quyền theo bản ghi chủ của tệp — handler kiểm, không có khoá quyền riêng cho tệp")]` + quyền đọc **bản ghi chủ** của tệp, kiểm trong handler

Trả nội dung tệp. **Không** phải endpoint envelope: thân phản hồi là chính tệp, kèm `Content-Disposition: attachment` và tên gốc.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.FILE.NOT_FOUND` | `NotFound` | 404 | Không có tệp đó, **hoặc** người gọi không có quyền đọc bản ghi chủ |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |

Nhánh lỗi trả envelope như mọi endpoint khác; chỉ nhánh thành công là tệp thô.

**Không quyền thì trả 404, không trả 403** — cùng khuôn luật M7: trả 403 là xác nhận tệp đó có tồn tại.

---

## 3. `DELETE /api/v1/core/files/{id}`

**Status:** DRAFT
**Quyền:** `[AuthenticatedOnly("Quyền theo bản ghi chủ của tệp — handler kiểm, không có khoá quyền riêng cho tệp")]` + quyền ghi **bản ghi chủ**, kiểm trong handler

Gỡ liên kết tệp khỏi bản ghi. Tệp vật lý dọn theo chính sách vòng đời ([`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md)), không xoá ngay trong request.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.FILE.NOT_FOUND` | `NotFound` | 404 | Không có, hoặc không có quyền |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |

---

## 4. Ghi chú chung

**Quyền của tệp là quyền của bản ghi chủ.** Tệp không có ma trận quyền riêng: ai đọc được hồ sơ thì đọc được tệp đính kèm của hồ sơ đó. Một hệ quyền thứ hai chỉ cho tệp là hai hệ quyền sẽ lệch.

**Không phục vụ tệp bằng đường tĩnh.** Đường tĩnh nghĩa là ai có đường dẫn cũng tải được, và lúc đó mọi thứ ở trên thành trang trí.

**Không đoán kiểu tệp từ phần mở rộng.** Kiểm bằng nội dung; đuôi tệp do người tải lên đặt.

**Quét mã độc chưa có ở v1** — [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md) khai rõ điều đó và điều kiện để làm. Đừng viết tài liệu như thể đã có.
