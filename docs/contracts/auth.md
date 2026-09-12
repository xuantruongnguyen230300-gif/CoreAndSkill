---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Auth

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi card trong file này mang `Status: DRAFT`: chưa có `src/`,
> BE chưa cam kết, ví dụ JSON là **phác thảo** chứ không phải body thật.
>
> Envelope, `ErrorType` → HTTP, khuôn mã lỗi, phân trang: [`README.md`](README.md). Định nghĩa
> đầy đủ: [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md). Card này không
> chép lại.

---

## 1. Cơ chế phiên — đọc trước mọi card

**Cookie phiên (ASP.NET Core Identity), KHÔNG JWT.** Quyết định đã chốt:
[`../adr/0004-giu-aspnet-identity.md`](../adr/0004-giu-aspnet-identity.md).

| Thứ | Giá trị |
| --- | --- |
| Cookie phiên | Do Identity phát, `HttpOnly`, `Secure`, không đọc được từ JS |
| Token CSRF | Nằm trong **thân** phản hồi của `GET /api/v1/core/antiforgery/token`. FE giữ **trong bộ nhớ** — **không** có cookie CSRF nào cho JS đọc |
| Header CSRF | `X-XSRF-TOKEN` |
| FE phải bật | `withCredentials: true` cho mọi request |

FE tự gắn header — cách gắn và vì sao không dùng cơ chế có sẵn của Angular:
[`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §2.1.

**Antiforgery áp theo METHOD, không theo endpoint.** Mọi `POST`/`PUT`/`PATCH`/`DELETE` phải mang
`X-XSRF-TOKEN` — **kể cả `POST /api/v1/core/auth/login`**. Không có allowlist ngoại lệ, vì một
allowlist là một danh sách sẽ dài dần và không ai rà lại.

### 1.1 🪤 Bẫy CSRF phải biết trước khi code FE

`IAntiforgery` của ASP.NET Core gắn request-token với **danh tính tại thời điểm phát hành**.
Token phát lúc **chưa đăng nhập** không dùng được cho request ghi **sau khi** đã đăng nhập —
server từ chối 403 với lý do *"meant for a different claims-based user"*.

Token FE đang giữ **không tự đổi** khi đăng nhập; chỉ có token mới khi FE gọi lại
`GET /api/v1/core/antiforgery/token`. Hệ quả cho FE:

> **Gọi `GET /api/v1/core/antiforgery/token` ở ba thời điểm: lúc app khởi động, ngay sau khi
> `POST /api/v1/core/auth/login` trả 200, VÀ ngay sau khi đăng xuất.** Token gắn với danh tính lúc
> phát — sau đăng xuất, token của người cũ làm lần đăng nhập kế tiếp bị 403.

Thiếu lần gọi thứ hai: **request ghi đầu tiên sau khi đăng nhập** (đổi mật khẩu, tạo người
dùng…) trả 403, trong khi người dùng thao tác hoàn toàn bình thường. Đây là lỗi đã xảy ra thật ở
dự án tiền nhiệm, và triệu chứng của nó không gợi ra nguyên nhân chút nào.

### 1.2 Trạng thái "bắt buộc đổi mật khẩu" chặn gần như mọi endpoint

Khi người dùng có `mustChangePassword = true`, mọi endpoint trả **403
`CORE.AUTH.PASSWORD_CHANGE_REQUIRED`**, trừ đúng bốn đường sau:

| Đường được phép | Vì sao |
| --- | --- |
| `GET /api/v1/core/antiforgery/token` | Cần để gửi được request ghi |
| `GET /api/v1/core/auth/me` | FE cần biết trạng thái để điều hướng |
| `POST /api/v1/core/auth/change-password-required` | Đường thoát duy nhất |
| `POST /api/v1/core/auth/logout` | Luôn phải thoát được |

**Vì sao chặn ở giữa (middleware) chứ không để FE tự điều hướng.** FE điều hướng là trải nghiệm;
nếu đó là lớp chặn duy nhất thì một request gọi thẳng bằng `curl` đi qua được. Với tài khoản
mang mật khẩu tạm do người khác đặt, đó là lỗ thật.

---

## 2. `GET /api/v1/core/antiforgery/token`

**Status:** DRAFT
**Quyền:** không cần đăng nhập · **không** bị rate-limit

Phát hành request-token chống CSRF trong **thân** phản hồi, và set cookie **nội bộ** của antiforgery
(`HttpOnly`) giữ nửa còn lại của cặp token. **Không** set cookie nào cho JS đọc.

### Response 200

**Có envelope, như mọi endpoint dưới `/api`.** Luật *"mọi response dưới `/api` đều mang
envelope, không có ngoại lệ"* ở [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md)
§2.2 áp cả ở đây, và mẫu code của endpoint này trong chính file đó cũng trả envelope.

```json
{
  "success": true,
  "data": { "token": "CfDJ8Nl4…" },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000009"
}
```

FE **đọc** body này để lấy token — nên nó đi qua đúng một nhánh bóc envelope như mọi endpoint khác.

Kèm header — cookie **nội bộ** của antiforgery, JS không đọc được:

```text
Set-Cookie: <tên khai ở cấu hình>=CfDJ8Kx2…; Path=/; HttpOnly; Secure; SameSite=Lax
```

### Lỗi

Không có nhánh lỗi nghiệp vụ.

### Ghi chú

> ⚠️ **`IAntiforgery` có HAI nửa, và chúng đi hai đường khác nhau.** *Cookie-token* là bí mật
> server tự quản trong cookie nội bộ `HttpOnly` — trình duyệt tự gửi. *Request-token* nằm trong
> thân phản hồi — FE giữ trong bộ nhớ và gửi lại qua header `X-XSRF-TOKEN`. Thiếu nửa nào cũng 403.
> Vì sao không có cookie thứ hai chứa request-token: [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.5.

---

## 3. `POST /api/v1/core/auth/login`

**Status:** DRAFT
**Quyền:** không cần đăng nhập

### Request

```json
{
  "tenantCode": "SO-GD",
  "userName": "an.nv",
  "password": "…",
  "rememberMe": false
}
```

| Field | Kiểu | Bắt buộc | Ghi chú |
| --- | --- | --- | --- |
| `tenantCode` | string | ✔ | **Mã đơn vị.** Ô nhập cùng cấp với ô tên đăng nhập, điền **trước** khi xác thực |
| `userName` | string | ✔ | Tên đăng nhập, **không** phải email |
| `password` | string | ✔ | |
| `rememberMe` | bool | ✘ | Mặc định `false` |

> 🚨 **`tenantCode` là field BẮT BUỘC, không phải tuỳ chọn — bỏ nó ra là hỏng luồng đăng
> nhập, không phải "thiếu một tiện ích".** Tên đăng nhập chỉ duy nhất theo cặp
> `(TenantId, tên chuẩn hoá)`: hai cơ quan cùng có `admin`, cùng có `vanthu`. Không có mã đơn vị
> thì `FindByNameAsync` **không định danh được một người**. Chi tiết và các phương án đã loại:
> [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11 ·
> [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §6.2.
>
> Đây **không** phải màn chọn tenant sau khi đăng nhập, và cũng không phải một tham số mà client
> tự khai để chọn dữ liệu mình muốn thấy: nó chỉ là **điều kiện tra cứu** trước bước xác thực.
> Sau khi đăng nhập, đơn vị đến từ claim trong phiếu, và **không** endpoint nào khác nhận
> `tenantId`/`tenantCode` từ request (luật M2).

**`rememberMe` mặc định `false` nằm ở bên an toàn có chủ đích:** client cũ chưa biết trường này
nhận phiên **ngắn hơn**, không phải dài hơn.

| `rememberMe` | Cookie |
| --- | --- |
| `false` | Cookie phiên — chết khi đóng trình duyệt |
| `true` | Cookie 14 ngày, trượt theo hoạt động |

### Response 200

```json
{
  "success": true,
  "data": {
    "id": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234",
    "userName": "an.nv",
    "email": "an.nv@vd.vn",
    "fullName": "Nguyễn Văn An",
    "roles": ["Quản trị hệ thống"],
    "permissions": ["core.user.read", "core.user.write", "core.menu.read"],
    "mustChangePassword": false,
    "isSystemOperator": false,
    "sessionMinutes": 30
  },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000010"
}
```

| Field | Kiểu | Ghi chú |
| --- | --- | --- |
| `roles` | string[] | **Tên vai trò từ database.** Dùng để **hiển thị**, không bao giờ để phân nhánh quyền (luật S2) |
| `permissions` | string[] | Tập mã quyền hiệu lực. **Đây là thứ FE dùng để ẩn/hiện chức năng** |
| `mustChangePassword` | bool | `true` ⇒ FE điều hướng thẳng sang màn đổi mật khẩu bắt buộc |
| `isSystemOperator` | bool | `true` ⇒ tài khoản vận hành hệ thống ([`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md)). FE hiện khu quản trị đơn vị và **ẩn** mọi màn nghiệp vụ. Cờ này **không** nằm trong `permissions` — nó là đường phân quyền riêng, đúng như thiết kế |
| `sessionMinutes` | int | Phiên sống bao lâu tính từ **request thành công gần nhất**. FE dùng để hẹn giờ cảnh báo sắp hết phiên — [`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md) §7.5. Chốt v1: **30**. Giá trị và lý do: [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.4 |

> **Vì sao trả `permissions` chứ không để FE tự suy từ `roles`.** Suy từ `roles` đòi FE mang một
> bản sao của ma trận quyền — bản sao đó sẽ lệch, và lệch theo chiều nguy hiểm: FE hiện một nút
> mà BE sẽ từ chối. Trả thẳng tập quyền hiệu lực là một nguồn, một lần.
>
> `permissions` chỉ điều khiển **hiển thị**. Chặn thật luôn nằm ở BE — FE ẩn nút không phải là
> phân quyền.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | `tenantCode`, `userName` hoặc `password` rỗng. Kèm `fieldErrors["TenantCode"]` / `["UserName"]` / `["Password"]` |
| `CORE.AUTH.INVALID_CREDENTIALS` | `BusinessRule` | 422 | Sai mã đơn vị, đơn vị đã ngưng hoạt động, sai tên đăng nhập, hoặc sai mật khẩu — **bốn ca, một mã** |
| `CORE.AUTH.LOCKED_OUT` | `BusinessRule` | 422 | Tài khoản đang bị khoá |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.RATE_LIMIT.EXCEEDED` | `BusinessRule` | 429 | Chạm hạn mức. Kèm header `Retry-After` |

### Ghi chú

> **Vì sao `CORE.AUTH.LOCKED_OUT` KHÔNG gộp vào `CORE.AUTH.INVALID_CREDENTIALS`.** Thông điệp
> riêng xác nhận tài khoản đó có thật, nên nhìn qua là một rò rỉ. Nhưng nó chỉ rò rỉ **một** tài
> khoản sau nhiều lần đoán sai cho riêng nó — không liệt kê hàng loạt được.
>
> Đổi lại, gộp hai mã khiến người dùng thật bị khoá không hiểu chuyện gì và thử lại liên tục, mà
> mỗi lần thử lại **gia hạn khoá**. Đó là một vòng lặp người dùng không tự thoát được.
>
> Đường liệt kê hàng loạt thật là **chênh lệch thời gian phản hồi** giữa "user không tồn tại" và
> "user tồn tại, sai mật khẩu" — vá đường đó, đừng vá bằng cách làm thông điệp mơ hồ. Xem
> [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md).

> **Bốn ca gộp vào `CORE.AUTH.INVALID_CREDENTIALS` là cố ý.** Mã đơn vị không tồn tại, đơn vị đã
> ngưng hoạt động, sai tên đăng nhập, sai mật khẩu — cùng một mã, cùng một câu. Tách chúng ra là
> mở đường dò: một người ngoài thử lần lượt các mã đơn vị và biết được **cơ quan nào đang dùng hệ
> thống**, mà đó là thông tin họ không có quyền biết. Cùng họ với chống dò tài khoản ở
> [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md);
> luật gốc ở [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §6.3.
>
> Hệ quả cho FE: **không** tô đỏ riêng ô mã đơn vị khi nhận mã này. Tô riêng một ô là làm lộ
> đúng thứ việc gộp mã đang che.

> **Đừng nhầm 429 với `CORE.AUTH.LOCKED_OUT` (422).** 422 là **tài khoản** bị khoá, cần quản trị
> mở. 429 là **người gọi** đang bị siết, tự hết sau khoảng thời gian trong `Retry-After`.

---

## 4. `POST /api/v1/core/auth/logout`

**Status:** DRAFT
**Quyền:** `[Authorize]`

Không có body. Xoá cookie phiên phía server.

### Response 200

```json
{ "success": true, "data": true, "error": null, "traceId": "0HNO9S8JAP586:00000011" }
```

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | `Unauthorized` | 401 | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu hoặc sai `X-XSRF-TOKEN` |

### Ghi chú

Gọi khi đã hết phiên vẫn trả 401 chứ không 200 — FE nên coi cả hai là "đã đăng xuất" và dọn
trạng thái cục bộ như nhau.

---

## 5. `GET /api/v1/core/auth/me`

**Status:** DRAFT
**Quyền:** `[Authorize]`

Thông tin người dùng của phiên hiện tại. FE gọi lúc khởi động để biết đã đăng nhập chưa.

### Response 200

`data` **cùng một DTO** với response của `login`, không phải hai kiểu gần giống nhau.

```json
{
  "success": true,
  "data": {
    "id": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234",
    "userName": "an.nv",
    "email": "an.nv@vd.vn",
    "fullName": "Nguyễn Văn An",
    "roles": ["Quản trị hệ thống"],
    "permissions": ["core.user.read", "core.user.write", "core.menu.read"],
    "mustChangePassword": false,
    "isSystemOperator": false,
    "sessionMinutes": 30
  },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000012"
}
```

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | `Unauthorized` | 401 | Chưa đăng nhập, hoặc phiên đã hết hạn |

### Ghi chú

**401 phải là JSON sạch, không 302 redirect.** Mặc định của cookie authentication trong ASP.NET
Core là redirect sang trang đăng nhập — hành vi đúng cho ứng dụng render server, **sai** cho SPA:
`HttpClient` đi theo redirect và FE nhận về 200 kèm một trang HTML. Phải khai
`OnRedirectToLogin` trả 401.

Đây là điểm rủi ro cao nhất của cấu hình xác thực, và là thứ phải có integration test canh.

---

## 6. `POST /api/v1/core/auth/change-password`

**Status:** DRAFT
**Quyền:** `[Authorize]` — dùng khi người dùng **tự nguyện** đổi mật khẩu

### Request

```json
{
  "currentPassword": "…",
  "newPassword": "…"
}
```

### Response 200

```json
{ "success": true, "data": true, "error": null, "traceId": "0HNO9S8JAP586:00000013" }
```

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | Thiếu field, hoặc `newPassword` trùng `currentPassword` |
| `CORE.AUTH.NOT_AUTHENTICATED` | `Unauthorized` | 401 | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.CHANGE_PASSWORD_FAILED` | `BusinessRule` | 422 | Identity từ chối — sai mật khẩu hiện tại, hoặc mật khẩu mới không đạt chính sách. **Lý do nằm ở `fieldErrors`** |
| `CORE.USER.NOT_FOUND` | `NotFound` | 404 | Tài khoản bị xoá trong khi phiên còn sống |

**`CORE.AUTH.CHANGE_PASSWORD_FAILED` — chi tiết đi qua `fieldErrors`:**

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "CORE.AUTH.CHANGE_PASSWORD_FAILED",
    "type": "BusinessRule",
    "message": "Đổi mật khẩu thất bại.",
    "messageParams": null,
    "fieldErrors": {
      "CurrentPassword": [ { "code": "CORE.AUTH.PASSWORD_MISMATCH", "messageParams": null } ],
      "NewPassword":     [ { "code": "CORE.AUTH.PASSWORD_TOO_SHORT",     "messageParams": { "MinLength": "8" } },
                           { "code": "CORE.AUTH.PASSWORD_REQUIRES_DIGIT", "messageParams": null } ]
    }
  },
  "traceId": "0HNO9S8JAP586:00000014"
}
```

> **Ánh xạ mã Identity đi theo MÃ, không theo endpoint.** `CORE.AUTH.PASSWORD_MISMATCH` nói về
> mật khẩu **hiện tại**, nên nó rơi vào `CurrentPassword`, không phải `NewPassword`. Mã không
> thuộc ô nhập nào (ví dụ một xung đột concurrency — nó nói về **bản ghi**) rơi vào khoá
> `"$record"`; khoá đó cố ý bắt đầu bằng `$` để không bao giờ trùng tên một property thật.
>
> Mã Identity thô (`PasswordTooShort`, `PasswordMismatch`…) **không** đi thẳng ra dây: chúng
> được ánh xạ sang mã của catalog, đúng khuôn `MIỀN.TÀI_NGUYÊN.LÝ_DO`. Một hệ đặt tên thứ hai
> trên dây là một hệ FE phải học riêng.
>
> Ở dự án tiền nhiệm, "chi tiết" từng là danh sách mã Identity nối bằng dấu chấm phẩy **nhét vào
> giữa một câu tiếng Việt** — không dịch được, và không ô nhập nào được tô đỏ.

### Ghi chú — ảnh hưởng tới các phiên khác

| Phiên | Sau khi đổi mật khẩu thành công |
| --- | --- |
| Phiên đang gọi endpoint này | **Giữ nguyên** — không bị đăng xuất |
| Mọi phiên khác của chính người đó | **Bị chấm dứt** trong khoảng một chu kỳ kiểm `SecurityStamp`; request kế tiếp của chúng trả 401 |

Đây là hành vi cố ý: đổi mật khẩu phải vô hiệu hoá phiên cũ, nhưng không có lý do gì đá người
vừa **chủ động** đổi mật khẩu ra khỏi hệ thống. BE giữ phiên hiện tại bằng
`SignInManager.RefreshSignInAsync` ngay sau khi đổi thành công.

**FE không cần tự gọi `logout` rồi bắt đăng nhập lại** — cookie hiện tại vẫn hợp lệ.

---

## 7. `POST /api/v1/core/auth/change-password-required`

**Status:** DRAFT
**Quyền:** `[Authorize]` — **chỉ dùng được khi `mustChangePassword = true`**

Đường thoát duy nhất khỏi trạng thái §1.2. Tách khỏi §6 vì ba lý do:

1. Nó là endpoint **duy nhất** ngoài ba đường ở §1.2 mà middleware cho đi qua. Một endpoint riêng
   làm allowlist đó thành một danh sách đường dẫn cố định, không phải một điều kiện lồng trong
   logic.
2. Sau khi thành công, nó đặt `must_change_password = false` — hành vi mà §6 **không** có.
3. Tập lỗi khác: nó từ chối khi người dùng **không** ở trạng thái bắt buộc đổi.

### Request

```json
{
  "currentPassword": "<mật khẩu tạm do quản trị đặt>",
  "newPassword": "…"
}
```

### Response 200

```json
{ "success": true, "data": true, "error": null, "traceId": "0HNO9S8JAP586:00000015" }
```

Sau lệnh này, `GET /api/v1/core/auth/me` trả `mustChangePassword: false` và mọi endpoint khác mở
lại.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | Thiếu field, hoặc `newPassword` trùng `currentPassword` |
| `CORE.AUTH.NOT_AUTHENTICATED` | `Unauthorized` | 401 | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.PASSWORD_CHANGE_NOT_REQUIRED` | `BusinessRule` | 422 | Người dùng **không** ở trạng thái bắt buộc đổi — dùng §6 |
| `CORE.AUTH.CHANGE_PASSWORD_FAILED` | `BusinessRule` | 422 | Identity từ chối. `fieldErrors` như §6 |

### Ghi chú

**Cấm `newPassword` trùng `currentPassword`** ở cả hai endpoint. Không có luật này, luồng "bắt
buộc đổi mật khẩu" trở thành hình thức: người dùng nhập lại đúng mật khẩu tạm, cờ tắt, và mật
khẩu do người khác biết vẫn còn hiệu lực.

---

## 8. `POST /api/v1/core/auth/forgot-password`

**Status:** DRAFT
**Quyền:** không cần đăng nhập

Gửi thư chứa liên kết đặt lại mật khẩu. Endpoint này nằm trong allowlist ẩn danh ở
[`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §5.

### Request

```json
{
  "tenantCode": "SO-GD",
  "email": "an.nv@vd.vn"
}
```

| Field | Kiểu | Bắt buộc | Ghi chú |
| --- | --- | --- | --- |
| `tenantCode` | string | ✔ | Cùng ô mã đơn vị như form đăng nhập |
| `email` | string | ✔ | |

**Vì sao form này cũng hỏi mã đơn vị.** Một địa chỉ email có thể ứng với nhiều tài khoản ở
nhiều đơn vị, nên chỉ có email thì không có câu trả lời đúng cho *"đặt lại mật khẩu cho tài
khoản nào"* — [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.2.

### Response 200

```json
{ "success": true, "data": true, "error": null, "traceId": "0HNO9S8JAP586:00000017" }
```

> 🚨 **Trả 200 kể cả khi không tìm thấy tài khoản nào.** Phân biệt "đã gửi" với "không có
> tài khoản này" biến endpoint thành công cụ **liệt kê email và liệt kê mã đơn vị** cho bất kỳ
> ai — không cần đăng nhập. Cùng lý do khiến bốn ca đăng nhập gộp một mã ở §3.
>
> Hệ quả cho FE: câu hiển thị sau khi gửi phải là *"nếu địa chỉ này có tài khoản, chúng tôi đã
> gửi thư"*, không phải *"đã gửi thư tới …"*.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | `tenantCode` hoặc `email` rỗng, `email` sai định dạng. Kèm `fieldErrors["TenantCode"]` / `["Email"]` |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.RATE_LIMIT.EXCEEDED` | `BusinessRule` | 429 | Chạm hạn mức. Kèm header `Retry-After` |

### Ghi chú

Endpoint này **phải** bị rate-limit theo IP: không có nó, nó là một máy gửi thư miễn phí nhắm
vào hộp thư của người khác.

---

## 9. `POST /api/v1/core/auth/reset-password`

**Status:** DRAFT
**Quyền:** không cần đăng nhập — quyền đến từ **token** trong thư

### Request

```json
{
  "token": "CfDJ8…",
  "newPassword": "…"
}
```

| Field | Kiểu | Bắt buộc | Ghi chú |
| --- | --- | --- | --- |
| `token` | string | ✔ | Lấy từ liên kết trong thư. **Không** gửi kèm `tenantCode`: token đã gắn với đúng một tài khoản, nên đơn vị suy ra được từ nó |
| `newPassword` | string | ✔ | |

### Response 200

```json
{ "success": true, "data": true, "error": null, "traceId": "0HNO9S8JAP586:00000018" }
```

Sau lệnh này, **mọi phiên đang mở của tài khoản đó bị chấm dứt** — `SecurityStamp` đổi. Khác
với §6, ở đây không có phiên nào để giữ lại.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | Thiếu field |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.RESET_TOKEN_INVALID` | `BusinessRule` | 422 | Token sai, đã dùng, hoặc đã hết hạn — **ba ca, một mã** |
| `CORE.AUTH.CHANGE_PASSWORD_FAILED` | `BusinessRule` | 422 | Mật khẩu mới không đạt chính sách. `fieldErrors` như §6 |

### Ghi chú

**Ba ca gộp một mã là cố ý.** Phân biệt "token sai" với "token hết hạn" cho người thử biết họ
đã đoán trúng một token thật. FE hiển thị một câu duy nhất kèm nút xin gửi lại thư.

**`tenantCode` KHÔNG có trong request này**, và đó là điểm khác với §8: token là thứ do server
phát và gắn với đúng một tài khoản. Nhận thêm `tenantCode` ở đây là cho client khai đơn vị sau
khi server đã biết — đúng ca luật M2 cấm.

---

## 10. Rate limit — áp cho toàn hệ, không riêng đăng nhập

Ba tầng **cộng dồn**, mốc chặt hơn chạm trước:

| Tầng | Áp cho | Chặn được gì |
| --- | --- | --- |
| Toàn cục theo IP | **Mọi** endpoint, kể cả endpoint không khai gì | Quét/dò hàng loạt |
| Riêng đăng nhập, theo IP | `POST /api/v1/core/auth/login` | Dò mật khẩu từ một máy |
| Riêng đăng nhập, theo **tên đăng nhập** | `POST /api/v1/core/auth/login` | Dò phân tán từ nhiều IP vào một tài khoản — tầng thứ hai **không** bắt được ca này |

Con số cụ thể và thuật toán là của file chủ
[`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §6; card này không chép lại,
vì con số chép ra chỗ thứ hai là con số sẽ lệch.

**Miễn rate-limit:** endpoint kiểm tra sức khoẻ và `GET /api/v1/core/antiforgery/token`. Siết
endpoint token nghĩa là chặn đúng cái FE cần để gửi được request hợp lệ.

### Response 429

```text
HTTP/1.1 429 Too Many Requests
Retry-After: 47
```
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "CORE.RATE_LIMIT.EXCEEDED",
    "type": "BusinessRule",
    "message": "Bạn thao tác quá nhanh. Vui lòng thử lại sau ít phút.",
    "messageParams": { "RetryAfterSeconds": "47" },
    "fieldErrors": null
  },
  "traceId": "0HNO9S8JAP586:00000016"
}
```

- Envelope này **dựng tay** ở `OnRejected` của rate limiter, không đi qua `Result` — nên luật R6
  (envelope lỗi dựng tay luôn mang mã) áp thẳng vào đây. Nó là một trong các đường dựng envelope
  ở [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §2.4.
- **`type` là `"BusinessRule"`, và 429 là ca duy nhất mà `type` không suy ra được HTTP status.**
  Ánh xạ `ErrorType` → HTTP ở file chủ đưa `BusinessRule` về 422; ở đây status do rate limiter
  đặt (`RejectionStatusCode`), không do `ResultToHttpMapper`. FE vì vậy **không** được suy status
  từ `type` — nó đọc status thật, và dùng `code` để phân nhánh. Trường `type` vẫn phải có mặt vì
  hình dạng envelope không có nhánh thứ hai.
- `Retry-After` (giây) lấy **từ metadata của limiter**, không hardcode — cửa sổ đổi thì giá trị
  tự đi theo. Dùng chính con số này cho đồng hồ đếm ngược, đừng giả định 60.
- **429 không còn là chuyện riêng của màn đăng nhập.** Bất kỳ màn nào cũng gặp được, nên xử lý nó
  ở interceptor chung chứ không ở màn đăng nhập.

---

## 11. Bốn mã lỗi hạ tầng dùng chung

| `code` | HTTP | Khi nào | FE nên làm gì |
| --- | ---: | --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | 401 | Chưa đăng nhập / hết phiên | Dọn trạng thái, đưa về màn đăng nhập |
| `CORE.AUTH.FORBIDDEN` | 403 | Đã đăng nhập, thiếu quyền | Hiện thông báo thiếu quyền, **không** đưa về đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | 403 | Thiếu/sai `X-XSRF-TOKEN` | Gọi lại `GET /api/v1/core/antiforgery/token` rồi thử lại **một** lần |
| `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` | 403 | Đang ở trạng thái §1.2 | Điều hướng sang màn đổi mật khẩu bắt buộc |

**Ba mã 403 phải phân biệt được bằng `code`, không bằng `message`.** Chúng cùng HTTP status, cùng
`type: "Forbidden"`, nhưng ba đường xử lý hoàn toàn khác nhau: một cái thử lại được, một cái phải
điều hướng, một cái thì không làm gì được cả. Ở dự án tiền nhiệm, tài liệu từng viết rằng
*"message khác biệt là điểm phân biệt duy nhất"* — câu đó đã phải lật ngược.
