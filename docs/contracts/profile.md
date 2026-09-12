---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Hồ sơ cá nhân

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi card trong file này mang `Status: DRAFT`.
>
> Envelope, mã lỗi, bảo mật chung: [`README.md`](README.md). Danh tính và phiên: [`auth.md`](auth.md).

> **Khác `GET /api/v1/core/auth/me` chỗ nào.** `auth/me` trả **danh tính của phiên**: quyền, cờ buộc đổi mật khẩu, cờ vận hành — thứ FE cần để dựng giao diện. File này trả **thông tin cá nhân sửa được**. Hai endpoint, hai mục đích; không gộp, vì gộp nghĩa là mỗi lần lưu hồ sơ lại phải trả về cả tập quyền.

---

## 1. `GET /api/v1/core/profile`

**Status:** DRAFT
**Quyền:** `[Authorize]` — luôn là hồ sơ **của chính người gọi**, không nhận id

### Response 200

```json
{
  "success": true,
  "data": {
    "userName": "an.nv",
    "email": "an.nv@vd.vn",
    "fullName": "Nguyễn Văn An",
    "phoneNumber": "0912345678",
    "preferredLanguage": "vi"
  },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000051"
}
```

| Field | Sửa được? | Ghi chú |
| --- | --- | --- |
| `userName` | ❌ | Định danh đăng nhập. Đổi nó là đổi danh tính — việc của quản trị viên |
| `email` | ❌ | Cũng là định danh, và là đường khôi phục mật khẩu. Đổi email tự do là mở một đường chiếm tài khoản |
| `fullName` · `phoneNumber` | ✅ | |
| `preferredLanguage` | ✅ | Rỗng ⇒ theo ngôn ngữ mặc định của hệ thống |

---

## 2. `PUT /api/v1/core/profile`

**Status:** DRAFT
**Quyền:** `[Authorize]`

### Request

```json
{
  "fullName": "Nguyễn Văn An",
  "phoneNumber": "0912345678",
  "preferredLanguage": "vi"
}
```

Chỉ ba trường trên. Gửi thêm trường khác thì **bỏ qua**, không báo lỗi — nhưng cũng không được ghi.

### Response 200

`data` cùng hình dạng §1.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | `fullName` rỗng hoặc quá dài; `phoneNumber` sai khuôn; `preferredLanguage` ngoài danh sách ngôn ngữ đã khai |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu hoặc sai `X-XSRF-TOKEN` |

### Ghi chú

**Không có endpoint đổi email hay tên đăng nhập ở đây** — cố ý. Cả hai là định danh và là đường khôi phục tài khoản; đổi chúng đi qua màn quản trị người dùng ([`users.md`](users.md)), nơi có vết kiểm toán và có người thứ hai nhìn vào.

**Đổi mật khẩu không nằm ở file này** — [`auth.md`](auth.md) §6, vì nó cần mật khẩu hiện tại và có luật riêng.

**Ngôn ngữ lưu ở hồ sơ, không chỉ ở trình duyệt.** Lý do: BE cần biết ngôn ngữ của người nhận khi gửi thông báo ngoài phiên làm việc ([`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §4.2) — lúc đó không có trình duyệt nào để hỏi. Cách FE dung hoà hai nơi lưu: [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §7.

**Ảnh đại diện chưa có ở v1.** Nó kéo theo lưu trữ tệp, cắt ảnh, giới hạn dung lượng và một đường phục vụ ảnh công khai — đủ để là một quyết định riêng, không phải một trường thêm vào card này.
