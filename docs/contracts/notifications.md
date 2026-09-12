---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Thông báo trong ứng dụng

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi card trong file này mang `Status: DRAFT`.
>
> Cơ chế sự kiện, Outbox, kênh gửi và mẫu đa ngôn ngữ: [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md). Envelope và mã lỗi: [`README.md`](README.md).

---

## 1. `GET /api/v1/core/notifications`

**Status:** DRAFT
**Quyền:** `[Authorize]` — luôn trả thông báo **của chính người gọi**

Danh sách thông báo của người đang đăng nhập, phân trang theo khuôn chung ([`README.md`](README.md) §8).

### Request

| Tham số | Ghi chú |
| --- | --- |
| `page` · `pageSize` | Khuôn chung |
| `unreadOnly` | `true` ⇒ chỉ trả thông báo chưa đọc |

### Response 200

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234",
        "typeKey": "core.user.locked",
        "params": { "userName": "an.nv" },
        "createdAt": "2026-09-10T03:12:44Z",
        "readAt": null
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20
  },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000041"
}
```

> 🛑 **`typeKey` và `params` — KHÔNG có câu đã ghép sẵn.** FE dựng câu bằng bảng dịch, đúng khuôn khoá lỗi ([`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md)). Lưu câu đã ghép thì đổi ngôn ngữ không đổi được thông báo cũ, và sửa một lỗi chính tả không sửa được cái đã gửi.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | `Unauthorized` | 401 | Chưa đăng nhập |

---

## 2. `GET /api/v1/core/notifications/unread-count`

**Status:** DRAFT
**Quyền:** `[Authorize]`

Trả số thông báo chưa đọc. Đây là endpoint FE **hỏi định kỳ** để cập nhật chỉ báo trên thanh trên cùng.

### Response 200

`data` là một số nguyên.

### Ghi chú

**Endpoint này phải rẻ.** Nó bị gọi lặp lại theo chu kỳ, nên không được kéo theo truy vấn nặng; đếm trên chỉ mục, không đếm sau khi nạp danh sách.

**Đẩy thời gian thực chưa có ở v1** ([`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §6). Đó là lý do có endpoint này; khi nào có kết nối đẩy thì nó vẫn còn giá trị làm đường lùi.

---

## 3. `PUT /api/v1/core/notifications/{id}/read`

**Status:** DRAFT
**Quyền:** `[Authorize]` — chỉ đánh dấu được thông báo **của chính mình**

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.NOTIFICATION.NOT_FOUND` | `NotFound` | 404 | Không có, **hoặc** không phải của người gọi |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu hoặc sai `X-XSRF-TOKEN` |

Thông báo của người khác trả **404**, không trả 403 — cùng khuôn luật M7.

---

## 4. `PUT /api/v1/core/notifications/read-all`

**Status:** DRAFT
**Quyền:** `[Authorize]`

Đánh dấu đã đọc toàn bộ thông báo của người gọi. Không có nhánh lỗi nghiệp vụ.

### Ghi chú

**Không có endpoint xoá thông báo.** Dọn dẹp đi theo chính sách vòng đời dữ liệu ([`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md)), không do người dùng bấm.

**Không có endpoint TẠO thông báo.** Thông báo sinh ra từ **sự kiện nghiệp vụ** đi qua Outbox — một endpoint tạo thông báo là một đường vòng qua toàn bộ cơ chế đó, và nó sẽ được dùng ngay khi tồn tại.
