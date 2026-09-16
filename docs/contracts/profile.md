---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Hồ sơ cá nhân

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`. BE đã cam kết endpoint nào: đọc dòng `Status:` của từng card.
>
> Envelope, mã lỗi, bảo mật chung: [`README.md`](README.md). Danh tính và phiên: [`auth.md`](auth.md).

> **Khác `GET /api/v1/core/auth/me` chỗ nào.** `auth/me` trả **danh tính của phiên**: quyền, cờ buộc đổi mật khẩu, cờ vận hành — thứ FE cần để dựng giao diện. File này trả **thông tin cá nhân** — phần sửa được, cùng cờ cho chính chủ biết mình còn đường từ bỏ cờ bypass (§3). Hai endpoint, hai mục đích; không gộp, vì gộp nghĩa là mỗi lần lưu hồ sơ lại phải trả về cả tập quyền.
>
> `preferredLanguage` có mặt ở **cả hai**: phiên mang nó để FE áp ngôn ngữ ngay khi đăng nhập ([`auth.md`](auth.md) §3); sửa thì qua §2 của file này.

---

## 1. `GET /api/v1/core/profile`

**Status:** AGREED — BE và FE cùng soát 2026-09-15
**Quyền:** `[AuthenticatedOnly("Hồ sơ của chính người gọi — định danh lấy từ phiên")]` — luôn là hồ sơ **của chính người gọi**, không nhận id

### Request

Không có tham số.

### Response 200

```json
{
  "success": true,
  "data": {
    "userName": "an.nv",
    "email": "an.nv@vd.vn",
    "fullName": "Nguyễn Văn An",
    "phoneNumber": "0912345678",
    "preferredLanguage": "vi",
    "hasPermissionBypass": false,
    "version": "9d3b4c7e-2f10-4a8b-b1c5-6e7f8a9b0c1d"
  },
  "error": null,
  "traceId": "1b5db4e3b4cd17c9fe2d9c1c67e25ce6"
}
```

| Field | Kiểu | Sửa được? | Ghi chú |
| --- | --- | --- | --- |
| `userName` | string | ❌ | Định danh đăng nhập. Đổi nó là đổi danh tính — việc của quản trị viên |
| `email` | string \| null | ❌ | Cũng là định danh — đổi đi qua quản trị viên, xem Ghi chú §2. `null` ⇒ tài khoản chưa đặt email |
| `fullName` | string | ✅ | |
| `phoneNumber` | string \| null | ✅ | `null` ⇒ chưa đặt |
| `preferredLanguage` | string \| null | ✅ | `null` ⇒ dùng ngôn ngữ mặc định của hệ thống |
| `hasPermissionBypass` | bool | ❌ | Tài khoản gọi đang mang cờ `has_permission_bypass` ([`../database/schema-core.md`](../database/schema-core.md) §4.1). FE dùng để hiện thao tác từ bỏ cờ (§3) — **không** dùng để ẩn/hiện chức năng khác; thứ đó đọc `permissions` ở [`auth.md`](auth.md) §5 |
| `version` | string | ❌ | Token đồng thời của bản ghi tài khoản, **luôn** có mặt; FE gửi lại nguyên chuỗi ở §2. Nguồn giá trị và khuôn: [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6.3 |

Ba field `string | null` ứng với ba cột cho phép NULL ở [`../database/schema-core.md`](../database/schema-core.md) §4.1.

### Lỗi

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` | Đang ở trạng thái bắt buộc đổi mật khẩu ([`auth.md`](auth.md) §1.2) |

---

## 2. `PUT /api/v1/core/profile`

**Status:** AGREED — BE và FE cùng soát 2026-09-15
**Quyền:** `[AuthenticatedOnly("Sửa hồ sơ của chính người gọi — định danh lấy từ phiên")]`

Người gọi tự sửa họ tên, số điện thoại và ngôn ngữ ưa thích của chính mình.

### Request

```json
{
  "fullName": "Nguyễn Văn An",
  "phoneNumber": "0912345678",
  "preferredLanguage": "vi",
  "version": "9d3b4c7e-2f10-4a8b-b1c5-6e7f8a9b0c1d"
}
```

| Field | Kiểu | Bắt buộc | Ghi chú |
| --- | --- | --- | --- |
| `fullName` | string | ✔ | Không rỗng, tối đa 200 ký tự — cột `full_name` ở [`../database/schema-core.md`](../database/schema-core.md) §4.1 |
| `phoneNumber` | string \| null | | `null` ⇒ không đặt. **Tối đa 20 ký tự, chỉ gồm chữ số, khoảng trắng và `+` `-` `(` `)`** |
| `preferredLanguage` | string \| null | | `null` ⇒ dùng ngôn ngữ mặc định của hệ thống. BE **chỉ kiểm khuôn** mã ngôn ngữ: hai chữ thường, tuỳ chọn thêm `-` và hai chữ hoa (`vi`, `en-US`). BE **không** giữ danh sách ngôn ngữ — xem Ghi chú |
| `version` | string | ✔ | Token nhận từ `GET` §1 gần nhất. Thiếu hoặc lệch ⇒ 409, vì `null` không bao giờ khớp |

**Chuỗi rỗng `""` ở `phoneNumber` và `preferredLanguage` được chuẩn hoá thành `null`** trước khi kiểm
và ghi — hai cách gửi, một nghĩa "không đặt".

**`PUT` là thay thế toàn phần, không phải vá từng trường.** Ba trường đầu là toàn bộ phần sửa
được của hồ sơ, và body được đọc như một bản đầy đủ: trường **vắng mặt** đọc như `null` — với
`phoneNumber` và `preferredLanguage` nghĩa là **xoá giá trị đang có**, với `fullName` nghĩa là
`CORE.VALIDATION.FAILED`. FE muốn đổi một trường thì `GET` §1 rồi gửi lại đủ ba trường **cùng
`version` của chính lần `GET` đó**; không có đường ghi một phần, vì hai ngữ nghĩa "vắng mặt = giữ
nguyên" và "vắng mặt = xoá" trên cùng một verb là thứ hai bên sẽ hiểu ngược nhau mà không lỗi nào báo.

Chỉ bốn trường trên. Gửi thêm trường khác thì **bỏ qua**, không báo lỗi — nhưng cũng không được ghi.

### Response 200

`data` cùng hình dạng §1 — `version` trong đó là token **mới**; FE thay ngay, không cần `GET` lại
trước lần lưu kế tiếp.

### Lỗi

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | `fullName` rỗng hoặc quá 200 ký tự — `fieldErrors["FullName"]`; `phoneNumber` quá 20 ký tự hoặc chứa ký tự ngoài chữ số, khoảng trắng, `+` `-` `(` `)` — `fieldErrors["PhoneNumber"]`; `preferredLanguage` sai khuôn mã ngôn ngữ — `fieldErrors["PreferredLanguage"]` |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` | Đang ở trạng thái bắt buộc đổi mật khẩu ([`auth.md`](auth.md) §1.2) |
| `CORE.CONCURRENCY.CONFLICT` | `version` thiếu, hoặc lệch với bản ghi tài khoản trong database — một thao tác khác đã ghi xong sau khi FE đọc, ví dụ quản trị vừa đặt lại mật khẩu hoặc khoá tài khoản ([`users.md`](users.md) §8, §9). Không ghi gì |

### Ghi chú

**Không có endpoint đổi email hay tên đăng nhập ở đây** — cố ý. Cả hai là định danh; đổi chúng đi qua màn quản trị người dùng ([`users.md`](users.md)), nơi có vết kiểm toán và có người thứ hai nhìn vào.

**Đổi mật khẩu không nằm ở file này** — [`auth.md`](auth.md) §6, vì nó cần mật khẩu hiện tại và có luật riêng.

**Ngôn ngữ lưu ở hồ sơ, không chỉ ở trình duyệt.** Lý do: BE cần biết ngôn ngữ của người nhận khi gửi thông báo ngoài phiên làm việc ([`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §4.2) — lúc đó không có trình duyệt nào để hỏi. Cách FE dung hoà hai nơi lưu: [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §7.

**BE kiểm khuôn mã ngôn ngữ, không kiểm danh sách.** Ngôn ngữ nào dùng được là chuyện FE có tệp dịch cho nó, nên danh sách lựa chọn có **một** nguồn, ở FE ([`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md)). Một danh sách thứ hai ở BE là hai nguồn sẽ lệch nhau: FE thêm một ngôn ngữ, BE từ chối lưu nó. Mọi mã trong danh sách của FE phải khớp khuôn ở §2.

**Ảnh đại diện chưa có ở v1.** Nó kéo theo lưu trữ tệp, cắt ảnh, giới hạn dung lượng và một đường phục vụ ảnh công khai — đủ để là một quyết định riêng, không phải một trường thêm vào card này.

---

## 3. `POST /api/v1/core/profile/renounce-permission-bypass`

**Status:** AGREED — BE và FE cùng soát 2026-09-15
**Quyền:** `[AuthenticatedOnly("Tự bỏ cờ của chính tài khoản gọi — không ai bỏ hộ người khác")]` — chỉ tác động lên **chính** tài khoản gọi

Tài khoản quản trị đầu tiên của một đơn vị **tự bỏ** cờ `has_permission_bypass` của chính mình, sau khi đơn vị đã có người quản trị phân quyền qua vai trò. **Một chiều**: không endpoint nào đặt lại cờ lên một tài khoản đã tồn tại (luật M12).

### Request

Không có body.

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "f02b4d54618dd8ee8db4c3587b144d54" }
```

Từ lúc này tài khoản chỉ còn quyền đến từ vai trò của nó. FE gọi lại `GET /api/v1/core/auth/me` ([`auth.md`](auth.md) §5) để dựng lại giao diện theo `permissions` mới.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.PROFILE.PERMISSION_BYPASS_NOT_HELD` | `BusinessRule` | 422 | Tài khoản gọi **không** mang `has_permission_bypass` |
| `CORE.PROFILE.NO_OTHER_PERMISSION_ADMIN` | `BusinessRule` | 422 | Đơn vị **không có tài khoản nào khác, đang không bị khoá,** giữ `core.permission.write` qua vai trò — bỏ cờ lúc này là tự khoá đơn vị ra khỏi màn phân quyền |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` | Đang ở trạng thái bắt buộc đổi mật khẩu ([`auth.md`](auth.md) §1.2) |
| `CORE.CONCURRENCY.CONFLICT` | Bản ghi tài khoản bị một thao tác khác ghi xong trước khi request này ghi — ví dụ quản trị vừa đặt lại mật khẩu hoặc khoá tài khoản ([`users.md`](users.md) §8, §9) |

### Ghi chú

**Điều kiện chống tự khoá đếm tài khoản KHÁC, KHÔNG bị khoá, và chỉ tính quyền đến từ VAI TRÒ.** Chính người gọi không tính; một tài khoản khác cũng mang cờ `has_permission_bypass` không tính; tài khoản đang bị khoá không tính — khoá là cách vô hiệu hoá tài khoản ([`users.md`](users.md) §8, §10), và người không đăng nhập được thì không mở được màn phân quyền.

**Sau khi thành công:** `SecurityStamp` **không** đổi, **không** cấp lại cookie, không phiên nào bị chấm dứt. Permission không nằm trong cookie ([`../adr/0033-luong-dang-nhap-outcome-va-claim.md`](../adr/0033-luong-dang-nhap-outcome-va-claim.md)) mà kiểm mỗi request qua `IPermissionChecker` ([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §4.3), nên tập quyền có hiệu lực ở **request kế tiếp của mọi phiên** của tài khoản — kể cả phiên đang gọi. Thao tác ghi nhật ký kiểm toán.

**Endpoint nằm ở hồ sơ, không ở [`users.md`](users.md)**, vì chỉ chính chủ tài khoản gọi được — không ai bỏ cờ hộ người khác.

Ghi chú 2026-09-16 — chốt hành vi sau khi thành công theo ADR-0033; không đổi hình dạng request/response, `Status` giữ nguyên.
