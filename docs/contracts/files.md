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
**Quyền:** `[Authorize]` — quyền trên **bản ghi chủ** quyết định, xem Ghi chú

Nhận một tệp và trả về mã tệp để gắn vào bản ghi nghiệp vụ.

### Request

`multipart/form-data`:

| Phần | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `file` | ✅ | Nội dung tệp |
| `purpose` | ✅ | Mục đích lưu, do module khai. Quyết định thư mục đích và giới hạn áp dụng |

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
  "traceId": "0HNO9S8JAP586:00000031"
}
```

`originalName` là tên **người dùng đặt**, chỉ dùng để hiển thị và để đặt tên lúc tải về. Tên thật trên đĩa do hệ thống sinh và không bao giờ trả ra ngoài.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | Thiếu tệp, thiếu `purpose`, tệp rỗng |
| `CORE.FILE.TOO_LARGE` | `Validation` | 400 | Vượt giới hạn dung lượng của `purpose` đó |
| `CORE.FILE.TYPE_NOT_ALLOWED` | `Validation` | 400 | Kiểu tệp ngoài danh sách cho phép của `purpose` đó |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.RATE_LIMIT.EXCEEDED` | — | 429 | Vượt giới hạn tần suất |

Hai mã dung lượng và kiểu tệp cố ý dùng `Validation` → **400**, không dùng mã HTTP riêng cho tệp: ánh xạ loại lỗi sang HTTP nằm ở đúng một chỗ ([`README.md`](README.md) §5), và mở ngoại lệ ở đây là mở đường cho ngoại lệ kế tiếp.

---

## 2. `GET /api/v1/core/files/{id}`

**Status:** DRAFT
**Quyền:** `[Authorize]` + quyền đọc **bản ghi chủ** của tệp

Trả nội dung tệp. **Không** phải endpoint envelope: thân phản hồi là chính tệp, kèm `Content-Disposition: attachment` và tên gốc.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.FILE.NOT_FOUND` | `NotFound` | 404 | Không có tệp đó, **hoặc** người gọi không có quyền đọc bản ghi chủ |
| `CORE.AUTH.NOT_AUTHENTICATED` | `Unauthorized` | 401 | Chưa đăng nhập |

Nhánh lỗi trả envelope như mọi endpoint khác; chỉ nhánh thành công là tệp thô.

**Không quyền thì trả 404, không trả 403** — cùng khuôn luật M7: trả 403 là xác nhận tệp đó có tồn tại.

---

## 3. `DELETE /api/v1/core/files/{id}`

**Status:** DRAFT
**Quyền:** `[Authorize]` + quyền ghi **bản ghi chủ**

Gỡ liên kết tệp khỏi bản ghi. Tệp vật lý dọn theo chính sách vòng đời ([`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md)), không xoá ngay trong request.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.FILE.NOT_FOUND` | `NotFound` | 404 | Không có, hoặc không có quyền |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu hoặc sai `X-XSRF-TOKEN` |

---

## 4. Ghi chú chung

**Quyền của tệp là quyền của bản ghi chủ.** Tệp không có ma trận quyền riêng: ai đọc được hồ sơ thì đọc được tệp đính kèm của hồ sơ đó. Một hệ quyền thứ hai chỉ cho tệp là hai hệ quyền sẽ lệch.

**Không phục vụ tệp bằng đường tĩnh.** Đường tĩnh nghĩa là ai có đường dẫn cũng tải được, và lúc đó mọi thứ ở trên thành trang trí.

**Không đoán kiểu tệp từ phần mở rộng.** Kiểm bằng nội dung; đuôi tệp do người tải lên đặt.

**Quét mã độc chưa có ở v1** — [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md) khai rõ điều đó và điều kiện để làm. Đừng viết tài liệu như thể đã có.
