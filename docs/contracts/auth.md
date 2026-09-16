---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Auth

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`, ví dụ JSON là **phác thảo** chứ không phải body
> thật. BE đã cam kết endpoint nào: đọc dòng `Status:` của từng card.
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
| Cookie phiên | Do scheme cookie ở `Core.Web` phát ([`../adr/0026-ranh-gioi-identity-va-cookie.md`](../adr/0026-ranh-gioi-identity-va-cookie.md)); `HttpOnly`, `Secure`, không đọc được từ JS |
| Token CSRF | Nằm trong **thân** phản hồi của `GET /api/v1/core/antiforgery/token`. FE giữ **trong bộ nhớ** — **không** có cookie CSRF nào cho JS đọc |
| Header CSRF | `X-XSRF-TOKEN` |
| Header `Origin` | Request ghi mang `Origin` ngoài allowlist bị chặn **403 `CORE.AUTH.ORIGIN_REJECTED`**, kể cả khi token CSRF hợp lệ — lớp chặn: [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.2 |
| FE phải bật | `withCredentials: true` cho mọi request |

FE tự gắn header — cách gắn và vì sao không dùng cơ chế có sẵn của Angular:
[`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §2.1.

**Antiforgery áp theo METHOD, không theo endpoint.** Mọi `POST`/`PUT`/`PATCH`/`DELETE` phải mang
`X-XSRF-TOKEN` — **kể cả `POST /api/v1/core/auth/login`**. Không có allowlist ngoại lệ, vì một
allowlist là một danh sách sẽ dài dần và không ai rà lại.

**Xác thực chạy trước antiforgery** — thứ tự ở
[`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3.1. Request ghi của một phiên
đã hết hạn nhận **401 `CORE.AUTH.NOT_AUTHENTICATED`**, không phải 403
`CORE.AUTH.CSRF_REJECTED`: FE đưa về màn đăng nhập, không lấy lại token rồi gửi lại.

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

Khi người dùng có `mustChangePassword = true`, mọi endpoint **có danh tính** và không mang
`[AllowAnonymous]` trả **403 `CORE.AUTH.PASSWORD_CHANGE_REQUIRED`**, trừ đúng bốn đường sau
(endpoint ẩn danh như `POST …/login` hay `POST …/client-errors` không bị kiểm — cùng khuôn với
lớp antiforgery):

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

**Status:** AGREED — BE và FE cùng soát 2026-09-15
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
  "traceId": "36396a58d6ea4b4d48cd2a0ba88cf0b5"
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

**Status:** AGREED — BE và FE cùng soát 2026-09-15
**Quyền:** không cần đăng nhập

### Request

```json
{
  "tenantCode": "SO-GD",
  "userName": "an.nv",
  "password": "…"
}
```

| Field | Kiểu | Bắt buộc | Ghi chú |
| --- | --- | --- | --- |
| `tenantCode` | string | ✔ | **Mã đơn vị.** Ô nhập cùng cấp với ô tên đăng nhập, điền **trước** khi xác thực. Khuôn và cách chuẩn hoá: [`tenants.md`](tenants.md) §2 — BE chuẩn hoá về chữ HOA trước khi tra, nên người dùng gõ thường vẫn khớp |
| `userName` | string | ✔ | Tên đăng nhập, **không** phải email |
| `password` | string | ✔ | |

Request **không** có tuỳ chọn duy trì phiên: mọi phiên hết hạn sau cùng một khoảng không thao tác —
`sessionMinutes` ở Response 200.

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

**Tài khoản vận hành hệ thống cũng gõ mã đơn vị, như mọi người.** Mã của đơn vị hệ thống do người
vận hành đặt qua cấu hình lúc cài đặt
([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)).
Mã chỉ là điều kiện tra cứu: hệ thống nhận ra đơn vị hệ thống bằng cột `is_system`
([`../database/schema-core.md`](../database/schema-core.md) §1.3), không bằng mã.

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
    "sessionMinutes": 30,
    "preferredLanguage": "vi",
    "tenantCode": "SO-GD",
    "tenantName": "Sở Giáo dục và Đào tạo"
  },
  "error": null,
  "traceId": "e735e706bcd9c0efb3aaa352d9c0433e"
}
```

| Field | Kiểu | Ghi chú |
| --- | --- | --- |
| `id` | uuid | Định danh tài khoản — cột `id` của `core.app_user` ([`../database/schema-core.md`](../database/schema-core.md) §4.1) |
| `userName` | string | Tên đăng nhập, **không** phải email |
| `email` | string \| null | Cột `email` của `core.app_user` cho phép NULL ([`../database/schema-core.md`](../database/schema-core.md) §4.1). `null` ⇒ tài khoản chưa đặt email. Cùng kiểu với [`profile.md`](profile.md) §1 — FE không được giả định luôn có chuỗi |
| `fullName` | string | Cột `full_name`, không NULL |
| `preferredLanguage` | string \| null | Cột `preferred_language` của `core.app_user` ([`../database/schema-core.md`](../database/schema-core.md) §4.1). `null` ⇒ dùng ngôn ngữ mặc định của hệ thống. FE áp ngôn ngữ sau đăng nhập từ field này — [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §7. Sửa qua [`profile.md`](profile.md) §2 |
| `tenantCode` | string | Cột `code` của `core.tenant` ([`../database/schema-core.md`](../database/schema-core.md) §1.3) — đơn vị của phiên. Chỉ để **hiển thị**: không endpoint nào nhận lại nó (luật M2) |
| `tenantName` | string | Cột `name` của `core.tenant` ([`../database/schema-core.md`](../database/schema-core.md) §1.3). Chỉ để **hiển thị** |
| `roles` | string[] | **Tên vai trò từ database.** Dùng để **hiển thị**, không bao giờ để phân nhánh quyền (luật S2) |
| `permissions` | string[] | **Tập quyền hiệu lực** của tài khoản, lấy từ đúng bộ kiểm quyền mà endpoint dùng để chặn — định nghĩa, gồm cả ca tài khoản mang `has_permission_bypass`: [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §4.3. **Đây là thứ FE dùng để ẩn/hiện chức năng.** Response không mang cờ bypass, và FE không có nhánh riêng cho nó |
| `mustChangePassword` | bool | `true` ⇒ FE điều hướng thẳng sang màn đổi mật khẩu bắt buộc |
| `isSystemOperator` | bool | `true` ⇒ tài khoản vận hành hệ thống ([`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md)). FE hiện khu quản trị đơn vị và **ẩn** mọi màn nghiệp vụ. Cờ này **không** nằm trong `permissions` — nó là đường phân quyền riêng, đúng như thiết kế |
| `sessionMinutes` | int | Phiên sống bao lâu tính từ **request gần nhất mang phiên hợp lệ** — kể cả request mà handler trả lỗi nghiệp vụ. Chốt v1: **30**. FE v1 **không** hẹn giờ cảnh báo theo trường này ([`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md) §10, hàng *Đếm ngược hết phiên*); trường vẫn nằm trong DTO để FE dùng khi hàng đó lật. Giá trị, cách cookie gia hạn ở mọi request mang phiên hợp lệ, và cái giá của nó: [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.4. Phiên còn một **trần tuyệt đối** tính từ lúc đăng nhập — hết trần thì 401 dù đang thao tác; giá trị và khoá cấu hình ở cùng mục đó, response không mang |

> **Vì sao trả `permissions` chứ không để FE tự suy từ `roles`.** Suy từ `roles` đòi FE mang một
> bản sao của ma trận quyền — bản sao đó sẽ lệch, và lệch theo chiều nguy hiểm: FE hiện một nút
> mà BE sẽ từ chối. Trả thẳng tập quyền hiệu lực là một nguồn, một lần.
>
> `permissions` chỉ điều khiển **hiển thị**. Chặn thật luôn nằm ở BE — FE ẩn nút không phải là
> phân quyền.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.AUTH.INVALID_CREDENTIALS` | `BusinessRule` | 422 | Sai mã đơn vị, đơn vị đã ngưng hoạt động, sai tên đăng nhập, hoặc sai mật khẩu — **bốn ca, một mã**. Sai mật khẩu nhận mã này **kể cả khi tài khoản đang bị khoá** |
| `CORE.AUTH.LOCKED_OUT` | `BusinessRule` | 422 | Mật khẩu **đúng** nhưng tài khoản đang bị khoá — khoá tay của quản trị ([`users.md`](users.md) §8) hoặc khoá tự động sau nhiều lần sai liên tiếp |

**Mã dùng chung** — `type` và HTTP tra ở §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | `tenantCode`, `userName` hoặc `password` rỗng, hoặc `userName` quá dài. Kèm `fieldErrors["TenantCode"]` / `["UserName"]` / `["Password"]` — mã từng ô ở bảng dưới |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.RATE_LIMIT.EXCEEDED` | Chạm hạn mức — theo IP, hoặc theo cặp mã đơn vị + tên đăng nhập (§10). Kèm header `Retry-After` |

**Mã trong `fieldErrors` của `CORE.VALIDATION.FAILED`** — do validator gắn; nhóm mã dùng chung khai ở
[`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §7.1:

| Khoá | Mã trong `fieldErrors` | Khi nào |
| --- | --- | --- |
| `TenantCode` | `CORE.VALIDATION.REQUIRED` | Rỗng |
| `UserName` | `CORE.VALIDATION.REQUIRED` | Rỗng |
| `UserName` | `CORE.VALIDATION.MAX_LENGTH` — `messageParams` khoá `MaxLength` | Vượt trần độ dài; trần đi trong `messageParams`, card không chép |
| `Password` | `CORE.VALIDATION.REQUIRED` | Rỗng |

### Ghi chú

**Thứ tự kiểm — bước nào hỏng thì dừng ở đó:**

| # | Kiểm | Hỏng thì trả |
| --- | --- | --- |
| 1 | Mã đơn vị tồn tại, đơn vị đang hoạt động | `CORE.AUTH.INVALID_CREDENTIALS` |
| 2 | Tên đăng nhập tồn tại trong đơn vị đó | `CORE.AUTH.INVALID_CREDENTIALS` |
| 3 | Mật khẩu đúng | `CORE.AUTH.INVALID_CREDENTIALS` — **kể cả khi tài khoản đang bị khoá** |
| 4 | Tài khoản không bị khoá | `CORE.AUTH.LOCKED_OUT` |

Hạn mức theo `LoginPartitionKey` (§10) không nằm trong bảng này — chỗ kiểm của nó:
[`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §6.5.

**Khoá tự động:** ngưỡng số lần sai liên tiếp và thời gian khoá khai ở cấu hình
`Core:Identity:Lockout`; giá trị mặc định ở
[`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.2. Card không chép
con số.

> **Vì sao `CORE.AUTH.LOCKED_OUT` chỉ trả khi mật khẩu đúng.** Người không biết mật khẩu — kể cả
> người đang dò — luôn nhận `CORE.AUTH.INVALID_CREDENTIALS`, nên mã khoá không xác nhận với họ rằng
> tài khoản có thật. Báo khoá cho một mật khẩu sai thì ngược lại: cố tình gõ sai tới ngưỡng rồi đọc
> mã là đủ biết một tên đăng nhập tồn tại.
>
> Người nhận được `CORE.AUTH.LOCKED_OUT` là người đã gõ đúng mật khẩu, và họ cần một câu riêng.
> Gộp vào "sai thông tin đăng nhập" thì người dùng thật bị khoá tưởng mình gõ sai, thử lại liên tục
> và không biết phải đi tìm quản trị.
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

> **Đừng nhầm 429 với `CORE.AUTH.LOCKED_OUT` (422).** 422 là **tài khoản** bị khoá — hết khi quản
> trị mở khoá, hoặc khi hết thời gian của khoá tự động. 429 là **người gọi** đang bị siết, tự hết
> sau khoảng thời gian trong `Retry-After`.

Bổ sung mã `fieldErrors` 2026-09-16 — không đổi hình dạng request/response, `Status` giữ nguyên.

---

## 4. `POST /api/v1/core/auth/logout`

**Status:** AGREED — BE và FE cùng soát 2026-09-15
**Quyền:** `[AuthenticatedOnly("Đăng xuất — ai đã đăng nhập cũng phải thoát được")]`

Không có body. Xoá cookie phiên phía server — **chỉ phiên đang gọi**; phiên khác của cùng tài khoản trên máy khác giữ nguyên. Muốn chấm dứt mọi phiên: tự đổi mật khẩu (§6).

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "7a713f908fc93d7d517d098b992aa704" }
```

### Lỗi

**Mã dùng chung** — `type` và HTTP tra ở §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập, hoặc phiên đã hết hạn |
| `CORE.AUTH.CSRF_REJECTED` | Phiên còn hiệu lực, thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Phiên còn hiệu lực, header `Origin` ngoài allowlist |

### Ghi chú

Gọi khi đã hết phiên vẫn trả 401 chứ không 200 — kể cả khi token CSRF FE đang giữ đã cũ, vì xác
thực chạy trước antiforgery (§1). FE nên coi cả hai là "đã đăng xuất" và dọn trạng thái cục bộ như
nhau.

**Giới hạn:** chưa có kho phiếu phía server ([`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §7), nên đăng xuất chỉ làm trình duyệt đang gọi bỏ cookie — một bản sao của cookie đó vẫn sống tới trần phiên tuyệt đối ([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.4). Nghi cookie bị sao chép thì đổi mật khẩu (§6) để đổi security stamp.

---

## 5. `GET /api/v1/core/auth/me`

**Status:** AGREED — BE và FE cùng soát 2026-09-15
**Quyền:** `[AuthenticatedOnly("Danh tính của chính phiên đang gọi")]`

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
    "sessionMinutes": 30,
    "preferredLanguage": "vi",
    "tenantCode": "SO-GD",
    "tenantName": "Sở Giáo dục và Đào tạo"
  },
  "error": null,
  "traceId": "bd68c7e68becd7f8eec1c5f9340aac1a"
}
```

### Lỗi

**Mã dùng chung** — `type` và HTTP tra ở §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập, hoặc phiên đã hết hạn |

### Ghi chú

**401 phải là JSON sạch, không 302 redirect.** Mặc định của cookie authentication trong ASP.NET
Core là redirect sang trang đăng nhập — hành vi đúng cho ứng dụng render server, **sai** cho SPA:
`HttpClient` đi theo redirect và FE nhận về 200 kèm một trang HTML. Phải khai
`OnRedirectToLogin` trả 401.

Đây là điểm rủi ro cao nhất của cấu hình xác thực, và là thứ phải có integration test canh.

---

## 6. `POST /api/v1/core/auth/change-password`

**Status:** AGREED — BE và FE cùng soát 2026-09-15
**Quyền:** `[AuthenticatedOnly("Đổi mật khẩu của chính mình")]` — dùng khi người dùng **tự nguyện** đổi mật khẩu

### Request

```json
{
  "currentPassword": "…",
  "newPassword": "…"
}
```

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "ef249ca46fa87d26fcbc490d4f71fe96" }
```

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.AUTH.CHANGE_PASSWORD_FAILED` | `BusinessRule` | 422 | Identity từ chối — sai mật khẩu hiện tại, hoặc mật khẩu mới không đạt chính sách. **Lý do nằm ở `fieldErrors`** |

**Mã dùng chung** — `type` và HTTP tra ở §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu field — kèm `fieldErrors["CurrentPassword"]` / `["NewPassword"]`; hoặc `newPassword` trùng `currentPassword` — kèm `fieldErrors["NewPassword"]`, mã trong bảng dưới |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` | Đang ở trạng thái §1.2 — endpoint này **không** nằm trong bốn đường được phép; đường đúng lúc đó là §7 |
| `CORE.CONCURRENCY.CONFLICT` | Bản ghi tài khoản bị một thao tác khác ghi xong trước khi request này ghi — `concurrency_stamp` của Identity đổi |

**Mã trong `fieldErrors` của `CORE.VALIDATION.FAILED`** — do validator gắn, gốc envelope mang
`type: "Validation"`, HTTP 400:

| Mã trong `fieldErrors` | Khoá | Khi nào |
| --- | --- | --- |
| `CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT` | `NewPassword` | `newPassword` trùng `currentPassword` |

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
  "traceId": "78e72a44b160bd7f1a9c763c6420d720"
}
```

> **Ánh xạ mã Identity đi theo MÃ, không theo endpoint.** `CORE.AUTH.PASSWORD_MISMATCH` nói về
> mật khẩu **hiện tại**, nên nó rơi vào `CurrentPassword`, không phải `NewPassword`. Mã không
> thuộc ô nhập nào rơi vào khoá dự phòng `"$record"`; khoá đó cố ý bắt đầu bằng `$` để không
> bao giờ trùng tên một property thật. Hiện chưa mã nào của endpoint này rơi vào khoá đó —
> xung đột concurrency **không** đi đường `fieldErrors` mà là 409 ở gốc envelope (§11).
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
| Mọi phiên khác của chính người đó | **Bị chấm dứt ở request kế tiếp** — `SecurityStamp` đổi, request đó trả 401. Cùng nhịp với khoá và đặt lại mật khẩu hộ ([`users.md`](users.md) §8, §9) |

Đây là hành vi cố ý: đổi mật khẩu phải vô hiệu hoá phiên cũ, nhưng không có lý do gì đá người
vừa **chủ động** đổi mật khẩu ra khỏi hệ thống. BE giữ phiên hiện tại bằng cách **cấp lại cookie
phiên** ngay sau khi đổi thành công — cơ chế ở
[`../adr/0026-ranh-gioi-identity-va-cookie.md`](../adr/0026-ranh-gioi-identity-va-cookie.md), hệ quả 4.

**FE không cần tự gọi `logout` rồi bắt đăng nhập lại** — cookie hiện tại vẫn hợp lệ.

---

## 7. `POST /api/v1/core/auth/change-password-required`

**Status:** AGREED — BE và FE cùng soát 2026-09-15
**Quyền:** `[AuthenticatedOnly("Đường thoát duy nhất khỏi trạng thái bắt buộc đổi mật khẩu")]` — **chỉ dùng được khi `mustChangePassword = true`**

Đường thoát duy nhất khỏi trạng thái §1.2. Tách khỏi §6 vì ba lý do:

1. Nó là endpoint **duy nhất** ngoài ba đường ở §1.2 mà middleware cho đi qua. Một endpoint riêng
   làm allowlist đó thành một danh sách đường dẫn cố định, không phải một điều kiện lồng trong
   logic.
2. Sau khi thành công, nó đặt `must_change_password = false` — hành vi mà §6 **không** có (handler truyền
   `clearMustChangePassword: true` vào seam ở [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §7.1).
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
{ "success": true, "data": null, "error": null, "traceId": "cbd3d9b611d7e772465e0845084f9115" }
```

Sau lệnh này, `GET /api/v1/core/auth/me` trả `mustChangePassword: false` và mọi endpoint khác mở
lại.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.AUTH.PASSWORD_CHANGE_NOT_REQUIRED` | `BusinessRule` | 422 | Người dùng **không** ở trạng thái bắt buộc đổi — dùng §6 |
| `CORE.AUTH.CHANGE_PASSWORD_FAILED` | `BusinessRule` | 422 | Identity từ chối. `fieldErrors` như §6 |

**Mã dùng chung** — `type` và HTTP tra ở §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu field, hoặc `newPassword` trùng `currentPassword` (`fieldErrors["NewPassword"]` mang `CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT`). Khoá và mã như §6 |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.CONCURRENCY.CONFLICT` | Như §6 — `concurrency_stamp` của tài khoản đổi trước khi request này ghi |

### Ghi chú

**Cấm `newPassword` trùng `currentPassword`** ở cả hai endpoint. Không có luật này, luồng "bắt
buộc đổi mật khẩu" trở thành hình thức: người dùng nhập lại đúng mật khẩu tạm, cờ tắt, và mật
khẩu do người khác biết vẫn còn hiệu lực.

**Ảnh hưởng tới phiên: như §6.** BE **cấp lại cookie phiên** cho phiên đang gọi ngay sau khi đổi
thành công, nên người dùng đi thẳng vào ứng dụng mà không phải đăng nhập lại; mọi phiên khác của
tài khoản bị chấm dứt. Bảng và cơ chế: §6, mục *Ghi chú — ảnh hưởng tới các phiên khác*.

Thiếu bước cấp lại cookie thì `SecurityStamp` vừa đổi làm **chính phiên này** trượt ở request kế
tiếp — người dùng bị đá ra đúng lúc vừa đổi mật khẩu xong.

---

## 8. `POST /api/v1/core/auth/forgot-password`

> 📐 **NGOÀI PHẠM VI v1.** Chốt (2026-09-14): v1 không có quên mật khẩu tự phục vụ — người dùng
> liên hệ quản trị đơn vị, và quản trị đặt lại hộ ([`users.md`](users.md) §9). Card này và §9 ghi
> ra để định hình sớm, không phải để thi công. Quyết định:
> [`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md).

**Status:** DRAFT
**Quyền:** không cần đăng nhập

Gửi thư chứa liên kết đặt lại mật khẩu.

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
{ "success": true, "data": null, "error": null, "traceId": "91ef90fc054b28fc273c741a7e14eee9" }
```

> 🚨 **Trả 200 kể cả khi không tìm thấy tài khoản nào.** Phân biệt "đã gửi" với "không có
> tài khoản này" biến endpoint thành công cụ **liệt kê email và liệt kê mã đơn vị** cho bất kỳ
> ai — không cần đăng nhập. Cùng lý do khiến bốn ca đăng nhập gộp một mã ở §3.
>
> Hệ quả cho FE: câu hiển thị sau khi gửi phải là *"nếu địa chỉ này có tài khoản, chúng tôi đã
> gửi thư"*, không phải *"đã gửi thư tới …"*.

### Lỗi

**Mã dùng chung** — `type` và HTTP tra ở §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | `tenantCode` hoặc `email` rỗng, `email` sai định dạng. Kèm `fieldErrors["TenantCode"]` / `["Email"]` |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.RATE_LIMIT.EXCEEDED` | Chạm hạn mức. Kèm header `Retry-After` |

### Ghi chú

Endpoint này **phải** bị rate-limit theo IP: không có nó, nó là một máy gửi thư miễn phí nhắm
vào hộp thư của người khác.

---

## 9. `POST /api/v1/core/auth/reset-password`

> 📐 **NGOÀI PHẠM VI v1** — cùng quyết định với §8.

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
{ "success": true, "data": null, "error": null, "traceId": "9d406fdeb669a790b18640bdb28ece44" }
```

Sau lệnh này, **mọi phiên đang mở của tài khoản đó bị chấm dứt** — `SecurityStamp` đổi. Khác
với §6, ở đây không có phiên nào để giữ lại.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.AUTH.RESET_TOKEN_INVALID` | `BusinessRule` | 422 | Token sai, đã dùng, hoặc đã hết hạn — **ba ca, một mã** |
| `CORE.AUTH.CHANGE_PASSWORD_FAILED` | `BusinessRule` | 422 | Mật khẩu mới không đạt chính sách. `fieldErrors` như §6 |

**Mã dùng chung** — `type` và HTTP tra ở §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu field |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |

### Ghi chú

**Ba ca gộp một mã là cố ý.** Phân biệt "token sai" với "token hết hạn" cho người thử biết họ
đã đoán trúng một token thật. FE hiển thị một câu duy nhất kèm nút xin gửi lại thư.

**`tenantCode` KHÔNG có trong request này**, và đó là điểm khác với §8: token là thứ do server
phát và gắn với đúng một tài khoản. Nhận thêm `tenantCode` ở đây là cho client khai đơn vị sau
khi server đã biết — đúng ca luật M2 cấm.

---

## 10. Rate limit — áp cho toàn hệ, không riêng đăng nhập

Ba tầng **cộng dồn**, mốc chặt hơn chạm trước:

| Tầng | Áp cho | Kiểm ở đâu | Chặn được gì |
| --- | --- | --- | --- |
| Toàn cục theo IP | **Mọi** endpoint, kể cả endpoint không khai gì | `UseRateLimiter` | Quét/dò hàng loạt |
| Riêng đăng nhập, theo IP | `POST /api/v1/core/auth/login` | `UseRateLimiter` | Dò mật khẩu từ một máy |
| Riêng đăng nhập, theo **`LoginPartitionKey`** | `POST /api/v1/core/auth/login` | **Trong handler đăng nhập**, qua seam `ILoginAttemptLimiter` | Dò phân tán từ nhiều IP vào một tài khoản — tầng thứ hai **không** bắt được ca này |

Khoá của tầng thứ ba, seam giữ bộ đếm, exception khi vượt hạn mức và lý do tầng này nằm trong
handler: [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §6.5.
Con số cụ thể và thuật toán là của file chủ
[`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §6; card này không chép lại,
vì con số chép ra chỗ thứ hai là con số sẽ lệch.

**Cả ba tầng trả cùng một response khi vượt hạn mức** — 429 `CORE.RATE_LIMIT.EXCEEDED` kèm
`Retry-After`, mục "Response 429" dưới đây. Người gọi không phân biệt được tầng nào đã chặn, và
không cần phân biệt: cách xử lý là một.

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
  "traceId": "f7cc4d6106a463d16b49085cd74493a4"
}
```

- Envelope này **không** đi qua `Result`. Hai tầng theo IP dựng nó tay ở `OnRejected` của rate
  limiter; tầng thứ ba ném exception riêng từ handler và `IExceptionHandler` dựng envelope. Cả hai
  đều là đường dựng envelope tay, nên luật R6 (envelope lỗi dựng tay luôn mang mã) áp thẳng vào
  đây — danh sách đường ở [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §2.4.
- **`type` là `"BusinessRule"`, và 429 là ca duy nhất mà `type` không suy ra được HTTP status.**
  Ánh xạ `ErrorType` → HTTP ở file chủ đưa `BusinessRule` về 422; ở đây status do chính đường dựng
  envelope đặt — `RejectionStatusCode` của rate limiter, hoặc `IExceptionHandler` ở tầng thứ ba —
  không do `ResultToHttpMapper`. FE vì vậy **không** được suy status từ `type` — nó đọc status
  thật, và dùng `code` để phân nhánh. Trường `type` vẫn phải có mặt vì hình dạng envelope không có
  nhánh thứ hai.
- `Retry-After` (giây) lấy **từ chính bộ đếm đã chặn**, không hardcode — cửa sổ đổi thì giá trị
  tự đi theo. Dùng chính con số này cho đồng hồ đếm ngược, đừng giả định 60.
- **429 không còn là chuyện riêng của màn đăng nhập.** Bất kỳ màn nào cũng gặp được, nên xử lý nó
  ở interceptor chung chứ không ở màn đăng nhập.

---

## 11. Mã lỗi dùng chung — định nghĩa gốc

Mã mà **mọi** card đều có thể trả. Card của từng endpoint nhắc mã nào nó trả; ánh xạ mã → `type`
→ HTTP của các mã này chỉ khai ở đây.

| `code` | `type` | HTTP | Khi nào | FE nên làm gì |
| --- | --- | ---: | --- | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | Payload hoặc tham số không qua validator. Luôn kèm `fieldErrors` | Gắn lỗi vào đúng ô theo khoá `fieldErrors` |
| `CORE.AUTH.NOT_AUTHENTICATED` | `Unauthorized` | 401 | Chưa đăng nhập / hết phiên | Dọn trạng thái, đưa về màn đăng nhập |
| `CORE.AUTH.FORBIDDEN` | `Forbidden` | 403 | Đã đăng nhập, thiếu quyền | Làm mới tập quyền — quyền có thể vừa bị thu hồi ([`users.md`](users.md) §7) — rồi hiện thông báo thiếu quyền; **không** đưa về đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu/sai `X-XSRF-TOKEN` | Gọi lại `GET /api/v1/core/antiforgery/token` rồi thử lại **một** lần |
| `CORE.AUTH.ORIGIN_REJECTED` | `Forbidden` | 403 | Request ghi mang header `Origin` ngoài allowlist — chặn ở lớp antiforgery, kể cả khi token hợp lệ ([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.2) | Thông báo chung như nhóm `CORE.AUTH.*`; **không** gửi lại, **không** lấy lại token — `Origin` do trình duyệt gắn nên gửi lại vẫn bị chặn |
| `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` | `Forbidden` | 403 | Đang ở trạng thái §1.2 | Interceptor làm mới phiên, guard điều hướng sang màn đổi mật khẩu bắt buộc — [`../quy-uoc/fe-routing-guard.md`](../quy-uoc/fe-routing-guard.md) §5.4 |
| `CORE.RATE_LIMIT.EXCEEDED` | `BusinessRule` | 429 | Chạm hạn mức ở bất kỳ tầng nào của §10. Kèm header `Retry-After` (giây) | Xử lý ở interceptor chung, đếm ngược theo `Retry-After` |
| `CORE.CONCURRENCY.CONFLICT` | `Conflict` | 409 | Request ghi trượt phép kiểm đồng thời: bản ghi đích đã bị một thao tác khác ghi xong giữa lúc handler đọc và lúc ghi. Token là `concurrency_stamp` của Identity với tài khoản ([`../database/schema-core.md`](../database/schema-core.md) §3.6), `xmin` với entity khác ([`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §6). Mô hình và cách bắt ở một chỗ: [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6.1; token đi trên dây thế nào (field `version`): §6.3 cùng file. Card của từng endpoint ghi nhắc mã này; endpoint thay cả một tập có mã riêng ([`permissions.md`](permissions.md) §6) | **Không** tự gửi lại. Giữ nguyên dữ liệu người dùng đang nhập, nói rõ người khác vừa đổi bản ghi, cho tải lại rồi nhập lại — ba câu bắt buộc ở [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6.2 |
| `CORE.ROUTE.NOT_FOUND` | `NotFound` | 404 | Không route nào khớp | Bug của FE — **không** hiển thị như "bản ghi không tồn tại" |
| `CORE.ROUTE.METHOD_NOT_ALLOWED` | `NotFound` | 405 | Sai verb. Kèm header `Allow` | Bug của FE |
| `CORE.SYSTEM.UNEXPECTED` | `Unexpected` | 500 | Exception ngoài dự kiến — chỉ `IExceptionHandler` phát, không lộ chi tiết ([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §2.4) | Toast lỗi hệ thống kèm `traceId`; không thử lại tự động |

> **`CORE.ROUTE.NOT_FOUND` ≠ `CORE.USER.NOT_FOUND`.** Cùng HTTP 404, cùng `type: "NotFound"`.
> `code` là thứ **duy nhất** phân biệt "FE gọi sai URL" (bug của FE) với "bản ghi không tồn tại"
> (dữ liệu, cần hiển thị cho người dùng).
>
> **`CORE.ROUTE.METHOD_NOT_ALLOWED` mang `type: "NotFound"` dù HTTP là 405.** Không giá trị
> `ErrorType` nào ứng với 405, trong khi envelope luôn mang trường `type`; với người gọi, sai verb
> nghĩa là *"không có gì ở đây cho yêu cầu này"*. Như 429 ở §10, FE **không** suy status từ `type`
> — nó đọc status thật và phân nhánh theo `code`.

**Các mã 403 phải phân biệt được bằng `code`, không bằng `message`.** Chúng cùng HTTP status, cùng
`type: "Forbidden"`, nhưng đường xử lý khác hẳn nhau: có mã thử lại được, có mã phải điều hướng, có
mã phải làm mới tập quyền, có mã không gửi lại được. Ở dự án tiền nhiệm, tài liệu từng viết rằng
*"message khác biệt là điểm phân biệt duy nhất"* — câu đó đã phải lật ngược.
