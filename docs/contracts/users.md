---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Users (quản trị người dùng)

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi card trong file này mang `Status: DRAFT`.
>
> Envelope, `ErrorType` → HTTP, phân trang: [`README.md`](README.md). Cơ chế phiên và CSRF:
> [`auth.md`](auth.md). Cấu trúc bảng:
> [`../database/schema-core.md`](../database/schema-core.md) §4.

---

## 1. Quyền dùng trong file này

> 📖 **Danh mục khoá phân quyền — tập đầy đủ kèm ý nghĩa từng khoá: đọc
> [`../database/schema-core.md`](../database/schema-core.md) §5.2.** Card này chỉ nhắc **khoá
> của chính endpoint đang mô tả**, ở dòng `Quyền:` của từng mục ([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

**Các khoá dùng trong file này tách riêng chứ không gộp một khoá "quản trị người dùng" duy nhất.** Ba khoá cuối có sức
phá hoại khác hẳn việc sửa email, và tách chúng cho phép cấp một vai trò "trực hỗ trợ" chỉ đặt
lại được mật khẩu mà không sửa được vai trò của ai. Gộp lại thì mọi người sửa được email đều gán
được vai trò.

---

## 2. Luật bảo vệ tài khoản quản trị — đọc TRƯỚC các card

Các luật này ép ở **handler**, trước khi chạm tầng ghi, và nằm ở **đúng một chỗ** trong
`Core.Application`. Rải chúng ra từng handler là cách chắc chắn để một handler mới quên một luật.

> 🛑 **Không luật nào dựa trên TÊN vai trò.** Luật S1 cấm hằng số role trong Core
> ([`../adr/0005-permission-based.md`](../adr/0005-permission-based.md)). Các luật dưới đây dựa
> vào **tập quyền** của người gọi và vào cờ dữ liệu — `is_system` của vai trò
> ([`../database/schema-core.md`](../database/schema-core.md) §4.2), `has_permission_bypass` của
> tài khoản (§4.1 cùng file) — tất cả đều là dữ liệu, không phải chuỗi trong code.

### Luật 1 — Không leo thang đặc quyền

> **Người gọi chỉ được gán hoặc gỡ một vai trò R nếu bản thân họ đang có ĐỦ mọi quyền mà R cấp.**

`code`: `CORE.USER.ROLE_ESCALATION_FORBIDDEN` · `type`: `Forbidden` · HTTP **403**

Đây là bản tổng quát hoá của luật *"chỉ quản trị tối cao mới đụng được vai trò quản trị tối cao"*,
viết ra mà không cần biết tên vai trò nào. Áp cho **cả hai chiều**:

| Người gọi có | Vai trò đích cấp | Kết quả |
| --- | --- | --- |
| `{user.read, user.write}` | `{user.read}` | ✔ cho qua |
| `{user.read, user.write}` | `{user.read, permission.write}` | **403** khi **gán** — tự cấp cho người khác quyền mình không có |
| `{user.read, user.write}` | `{user.read, permission.write}` | **403** khi **gỡ** — tác động vào một vai trò mình không hiểu hết hệ quả |

**Vì sao chặn cả chiều gỡ.** Nếu chỉ chặn chiều gán, một người có `core.user.role.assign` gỡ được
vai trò quản trị của tất cả mọi người và làm hệ thống mất người quản trị — thao tác phá hoại
không cần leo thang gì cả.

### Luật 2 — Không tự gỡ vai trò hệ thống của chính mình

> **Không ai được gỡ khỏi CHÍNH MÌNH một vai trò có `is_system = true`** — kể cả khi họ có đủ
> quyền theo luật 1.

`code`: `CORE.USER.SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN` · `type`: `BusinessRule` · HTTP **422**

Đây là ca **luật 1 cho qua**: người gọi có đủ mọi quyền của vai trò đó (họ đang mang nó), nên
không có gì bất thường. Không có luật 2, một thao tác vô ý làm mất quyền quản trị mà **không có
lỗi nào bật ra** — im lặng, và đường sửa duy nhất còn lại là `UPDATE` bằng SQL tay.

Muốn gỡ thật thì nhờ một người khác cũng mang vai trò đó thực hiện.

### Luật 3 — Không tự khoá tài khoản của chính mình

Áp cho **mọi** người dùng, không riêng quản trị.

`code`: `CORE.USER.CANNOT_LOCK_SELF` · `type`: `BusinessRule` · HTTP **422**

> **422 chứ không 403.** Người gọi **có** quyền `core.user.lock` — thứ bị từ chối là *thao tác
> này trên bản ghi này*, không phải quyền. Đó đúng ô "vi phạm quy tắc nghiệp vụ" của bảng phân
> loại ở [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §3.2, và catalog ở §7.1
> của file đó khai mã này là `ErrorType.BusinessRule`. Trả 403 làm FE hiện *"bạn không có
> quyền"* cho một người đang có quyền, và đẩy họ sang màn xin quyền không giải quyết được gì.

Muốn kết thúc phiên làm việc thì đăng xuất. Tự khoá là thao tác không có ca dùng hợp lệ nào và
có một hậu quả không tự sửa được.

### Luật 4 — Chỉ người mang vai trò hệ thống mới khoá được tài khoản mang vai trò hệ thống

`code`: `CORE.USER.SYSTEM_ROLE_LOCK_FORBIDDEN` · `type`: `Forbidden` · HTTP **403**

Không có luật này, một người chỉ có `core.user.lock` khoá được toàn bộ quản trị viên và chiếm
quyền điều hành hệ thống bằng đúng một quyền.

### Luật 5 — Đặt lại mật khẩu hộ: không nhắm vào tài khoản "cao hơn" người gọi, không nhắm vào chính mình

Áp cho `POST /api/v1/core/users/{id}/reset-password` (§9).

| Tài khoản đích | `code` | `type` | HTTP |
| --- | --- | --- | ---: |
| Tập quyền hiệu lực của đích **vượt** tập quyền của người gọi | `CORE.USER.RESET_PASSWORD_TARGET_FORBIDDEN` | `Forbidden` | **403** |
| Đích mang `has_permission_bypass` | `CORE.USER.RESET_PASSWORD_TARGET_FORBIDDEN` | `Forbidden` | **403** |
| Đích là **chính** người gọi | `CORE.USER.CANNOT_RESET_OWN_PASSWORD` | `BusinessRule` | **422** |

Đặt được mật khẩu của một người là nắm được tài khoản đó. Nếu tài khoản đó có quyền mà người gọi
không có, đó là leo thang đặc quyền — cùng lý do với luật 1, chỉ khác đường đi.

**Vì sao cần vế thứ hai.** Tài khoản mang `has_permission_bypass` có tập quyền hiệu lực là **toàn
bộ** danh mục ([`auth.md`](auth.md) §3), nên vế đầu đã chặn mọi người gọi — trừ một người gọi cũng
mang cờ đó. Vế thứ hai chặn nốt ca ấy. Tài khoản mang cờ chỉ được khôi phục từ khu hệ thống
([`tenants.md`](tenants.md) §4).

**Hai vế dùng chung một mã, có chủ đích:** tách mã là cho người gọi biết tài khoản đích có mang cờ
hay không.

**Tự đặt lại cho chính mình là 422.** Endpoint này không đòi mật khẩu hiện tại; đổi mật khẩu của
chính mình đi đường [`auth.md`](auth.md) §6.

### Hai loại lỗi — ranh giới nằm ở "người gọi có thiếu tư cách không"

Các mã trên không cùng một loại, và chọn sai loại làm FE hiển thị sai hẳn câu:

| Luật | `type` | HTTP | Vì sao |
| --- | --- | ---: | --- |
| 1 — leo thang đặc quyền | `Forbidden` | 403 | Người gọi **thiếu** tập quyền mà vai trò đích cấp |
| 4 — khoá tài khoản mang vai trò hệ thống | `Forbidden` | 403 | Người gọi **không mang** vai trò hệ thống |
| 5 — đặt lại mật khẩu cho tài khoản "cao hơn" | `Forbidden` | 403 | Người gọi **thiếu** tập quyền mà tài khoản đích có |
| 2 — tự gỡ vai trò hệ thống của mình | `BusinessRule` | 422 | Người gọi **có đủ** quyền; thứ bị chặn là thao tác nhắm vào chính mình |
| 3 — tự khoá chính mình | `BusinessRule` | 422 | Như trên |
| 5 — tự đặt lại mật khẩu của mình | `BusinessRule` | 422 | Như trên |

Phép thử một câu: **403 khi câu trả lời đúng là "bạn không đủ tư cách"; 422 khi câu trả lời
đúng là "việc này không làm được, kể cả với bạn".** Trả 403 cho luật 2 và 3 làm FE hiện *"bạn
không có quyền"* rồi mời người dùng đi xin quyền — cho một người đã có đủ quyền, nên không lời
xin nào giải quyết được gì. Phân loại gốc:
[`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §3.2; catalog: §7.1 của cùng file.

### Mở khoá — CỐ Ý không chặn gì

`POST /api/v1/core/users/{id}/unlock` **không áp luật nào**. Nó đi theo chiều **khôi phục** quyền
truy cập — chặn nó là chặn đúng đường sửa sai — và không cấp thêm gì cho người gọi.

Rủi ro đã cân nhắc và chấp nhận: ai đó mở khoá một tài khoản vừa bị khoá có chủ đích. Đó là hoàn
tác một thao tác quản trị, không phải chiếm quyền, và người khoá vẫn khoá lại được.

### Đã cân nhắc và LOẠI — luật "không được hạ/khoá người quản trị CUỐI CÙNG"

Nghe hợp lý, nhưng phải **đếm toàn bảng mỗi lần ghi**, và vẫn thua ca hai request đồng thời cùng
thấy "còn hai người" rồi cùng gỡ. Mua quá ít an toàn so với chi phí.

Thay vào đó, bất biến rẻ hơn nằm ở tầng dữ liệu và không cần đếm gì:
**không được thu hồi quyền `core.permission.write` khỏi một vai trò `is_system`**
([`../database/schema-core.md`](../database/schema-core.md) §5.3). Nó không ngăn được việc mất
người quản trị, nhưng ngăn được việc mất **đường sửa**.

---

## 3. `GET /api/v1/core/users`

**Status:** DRAFT
**Quyền:** `core.user.read`

Danh sách người dùng, có phân trang, lọc, sắp xếp.

### Request — query string

| Tham số | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- |
| `page` · `pageSize` · `sortBy` · `sortDescending` | | | Theo quy ước chung, [`README.md`](README.md) §8 |
| `sortBy` allowlist | | `userName` | `userName`, `fullName`, `email`, `createdAt` |
| `searchText` | string | — | Khớp một phần trên `userName`, `fullName`, `email`; không phân biệt hoa thường. Tối đa 200 ký tự |
| `roleId` | uuid | — | Lọc theo vai trò |
| `status` | enum | — | `active` \| `locked` |

Sắp xếp luôn có tiêu chí phụ `id` ở cuối — thiếu nó, dữ liệu trùng giá trị cho thứ tự không xác
định giữa các trang ([`README.md`](README.md) §8).

### Response 200

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234",
        "userName": "an.nv",
        "email": "an.nv@vd.vn",
        "fullName": "Nguyễn Văn An",
        "roles": [
          { "id": "0192f3c2-1111-7000-8000-000000000001", "name": "Quản trị hệ thống", "isSystem": true }
        ],
        "isLocked": false,
        "lockoutEnd": null,
        "lockedByAdmin": false,
        "mustChangePassword": false,
        "createdAt": "2026-09-01T03:12:45.120Z",
        "version": "9d3b4c7e-2f10-4a8b-b1c5-6e7f8a9b0c1d"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 137
  },
  "error": null,
  "traceId": "b0ec8faf3f54d8d861cd580da7252a58"
}
```

| Field | Ghi chú |
| --- | --- |
| `email` | `string \| null` — cột `email` cho phép NULL ([`../database/schema-core.md`](../database/schema-core.md) §4.1); cùng kiểu với [`auth.md`](auth.md) §3 và [`profile.md`](profile.md) §1. `null` ⇒ tài khoản chưa đặt email |
| `roles` | **Object, không phải chuỗi.** FE cần `id` để gửi lại khi gán vai trò, cần `isSystem` để hiển thị đúng |
| `isLocked` | Đã tính sẵn từ `lockoutEnd` so với thời điểm hiện tại — FE **không** tự tính lại |
| `lockoutEnd` | Trả kèm để hiện "khoá tới lúc nào". `null` khi không khoá |
| `lockedByAdmin` | Cột `locked_by_admin` ([`../database/schema-core.md`](../database/schema-core.md) §4.1). `isLocked && lockedByAdmin` ⇒ quản trị khoá (§8); `isLocked && !lockedByAdmin` ⇒ khoá tự động, hết ở `lockoutEnd` |
| `createdAt` | `string \| null` — cột `created_at` cho phép NULL ([`../database/schema-core.md`](../database/schema-core.md) §4.1, §3.2). FE phải dựng được lưới khi ô này rỗng |
| `version` | Token đồng thời của bản ghi, **luôn** có mặt; FE gửi lại nguyên chuỗi khi gọi §6, §8, §9. Nguồn giá trị và khuôn: [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6.3 |

**`isLocked` tính sẵn ở BE có chủ đích:** để FE tự so `lockoutEnd` với thời điểm hiện tại là đặt
một quy tắc nghiệp vụ vào hai nơi, và hai đồng hồ (máy chủ và trình duyệt) có thể lệch nhau.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.USER.ROLE_NOT_FOUND` | `BusinessRule` | 422 | `roleId` không tồn tại |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | `page < 1`, `pageSize` ngoài `1..200`, `sortBy` ngoài allowlist, `searchText` quá dài. `fieldErrors` mang khoá `Page` / `PageSize` / `SortBy` / `SearchText` |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.FORBIDDEN` | Thiếu `core.user.read` |

> **`roleId` không tồn tại là LỖI, không phải "trả danh sách rỗng".** Trả rỗng làm người gọi
> tưởng không có ai mang vai trò đó, trong khi thật ra họ gõ sai id. Hai tình huống khác hẳn nhau
> thì phải trả hai kết quả khác nhau.
>
> **Và nó là 422, không phải 400.** `Validation` theo định nghĩa là thứ tính được **từ payload,
> không cần DB** — mà "vai trò này có tồn tại không" thì bắt buộc phải đọc DB. Bảng phân loại ở
> [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §3.2 xếp *"một Id trong payload
> trỏ tới bản ghi không tồn tại"* vào `BusinessRule`. Hệ quả thực tế cho FE: mã này **không** kèm
> `fieldErrors`, nên nó không bind được vào một ô nhập — trong khi mọi mã 400 `Validation` đều
> kèm `fieldErrors`. Xếp nó vào 400 là hứa với FE một thứ response không có.

---

## 4. `GET /api/v1/core/users/{id}`

**Status:** DRAFT
**Quyền:** `core.user.read`

### Response 200

`data` là một phần tử `items` của §3, cùng shape — kể cả `version`, token cho các thao tác ghi ở
§6, §8, §9.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.USER.NOT_FOUND` | `NotFound` | 404 | `{id}` không tồn tại |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.FORBIDDEN` | Thiếu `core.user.read` |

---

## 5. `POST /api/v1/core/users`

**Status:** DRAFT
**Quyền:** `core.user.write` (+ `core.user.role.assign` nếu `roleIds` khác rỗng)

### Request

```json
{
  "userName": "binh.tv",
  "email": "binh.tv@vd.vn",
  "fullName": "Trần Văn Bình",
  "tempPassword": "…",
  "roleIds": ["0192f3c2-1111-7000-8000-000000000002"]
}
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `userName` | ✔ | Duy nhất, không đổi được sau khi tạo |
| `email` | ✔ | Duy nhất |
| `fullName` | ✔ | Tối đa 200 ký tự |
| `tempPassword` | ✔ | Mật khẩu tạm, **người gọi tự gõ**. Tài khoản tạo ra có `mustChangePassword = true`. Không xuất hiện trong response hay log |
| `roleIds` | ✘ | Rỗng ⇒ tài khoản không có vai trò nào, tức không có quyền nào |

### Response 201

```json
{
  "success": true,
  "data": { "id": "0192f3c3-4a5b-7c31-9a4e-6b1f2d3c4e5f" },
  "error": null,
  "traceId": "846cb7840c4e5ca40d1ad76ee88577f1"
}
```

Kèm header `Location: /api/v1/core/users/0192f3c3-4a5b-7c31-9a4e-6b1f2d3c4e5f`.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.USER.USERNAME_DUPLICATED` | `Conflict` | 409 | `userName` đã tồn tại. `messageParams`: `{ "UserName": "…" }` |
| `CORE.USER.EMAIL_DUPLICATED` | `Conflict` | 409 | `email` đã tồn tại. `messageParams`: `{ "Email": "…" }` |
| `CORE.USER.ROLE_NOT_FOUND` | `BusinessRule` | 422 | Một `roleId` không tồn tại |
| `CORE.USER.ROLE_ESCALATION_FORBIDDEN` | `Forbidden` | 403 | Luật 1 §2 |
| `CORE.USER.CREATE_FAILED` | `BusinessRule` | 422 | Identity từ chối (mật khẩu không đạt chính sách…). Lý do ở `fieldErrors` |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu field, sai định dạng email, `fullName` quá dài |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Thiếu `core.user.write`, hoặc thiếu `core.user.role.assign` khi `roleIds` khác rỗng |

### Ghi chú

**Tạo tài khoản và gán vai trò nằm trong MỘT transaction.** Không có nó, một lỗi ở bước gán để
lại một tài khoản đã tồn tại nhưng không có vai trò nào — và người vận hành thấy tài khoản "tạo
thành công" mà đăng nhập vào không làm được gì. `TransactionBehavior` phủ ca này
([`../adr/0006-pipeline-behavior.md`](../adr/0006-pipeline-behavior.md)).

**Ghi vai trò phải KIỂM kết quả.** `UserManager.AddToRolesAsync` trả `IdentityResult`; bỏ qua giá
trị trả về nghĩa là thất bại im lặng.

---

## 6. `PUT /api/v1/core/users/{id}`

**Status:** DRAFT
**Quyền:** `core.user.write`

Sửa thông tin hồ sơ. **Không đụng vai trò** — vai trò có endpoint riêng ở §7.

### Request

```json
{
  "email": "binh.tv@congty.vn",
  "fullName": "Trần Văn Bình",
  "version": "9d3b4c7e-2f10-4a8b-b1c5-6e7f8a9b0c1d"
}
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `email` | ✔ | Duy nhất |
| `fullName` | ✔ | Tối đa 200 ký tự |
| `version` | ✔ | Token nhận từ `GET` gần nhất (§3, §4). Thiếu hoặc lệch ⇒ 409, vì `null` không bao giờ khớp |

`userName` **không** có trong request: nó là định danh đăng nhập, đổi nó là đổi thứ người dùng và
mọi bản ghi audit đang tham chiếu tới.

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "117ee389df2b2fce6b57cfae63535863" }
```

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.USER.NOT_FOUND` | `NotFound` | 404 | |
| `CORE.USER.EMAIL_DUPLICATED` | `Conflict` | 409 | Email đã thuộc người khác |
| `CORE.USER.UPDATE_FAILED` | `BusinessRule` | 422 | Identity từ chối. Lý do ở `fieldErrors` |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Sai định dạng, quá dài |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Thiếu `core.user.write` |
| `CORE.CONCURRENCY.CONFLICT` | `version` thiếu, hoặc lệch với bản ghi `{id}` trong database — một thao tác khác đã ghi xong sau khi FE đọc (khoá, đặt lại mật khẩu, chính chủ sửa hồ sơ…). Không ghi gì |

### Ghi chú — vì sao TÁCH vai trò khỏi endpoint này

Ở dự án tiền nhiệm, endpoint sửa người dùng nhận **cả** danh sách vai trò, và ghi đè trọn gói.
Hệ quả: form sửa email quên gửi lại danh sách vai trò hiện có sẽ **âm thầm hạ quyền** người dùng
đó. Tài liệu phải viết một đoạn dài dặn FE *"luôn luôn gửi lại mọi vai trò, kể cả vai trò form
không quản lý"* — tức đặt một bất biến quan trọng vào tay client.

Tách endpoint gỡ hẳn lớp đó: sửa email **không thể** đụng vai trò, vì payload không có chỗ cho nó.

---

## 7. `PUT /api/v1/core/users/{id}/roles`

**Status:** DRAFT
**Quyền:** `core.user.role.assign`

Đặt lại **toàn bộ** tập vai trò của một người dùng.

### Request

```json
{
  "roleIds": [
    "0192f3c2-1111-7000-8000-000000000001",
    "0192f3c2-1111-7000-8000-000000000002"
  ]
}
```

**Ngữ nghĩa: THAY THẾ, không phải thêm vào.** Vai trò không có trong `roleIds` bị gỡ.
`roleIds: []` nghĩa là gỡ sạch — đó là thao tác hợp lệ và **cố ý** phải viết ra tường minh.

Tài khoản đích mang `has_permission_bypass` **vẫn gán vai trò được** — BE không chặn; vai trò chỉ có tác dụng sau khi tài khoản từ bỏ cờ ([`profile.md`](profile.md) §3).

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "de0bdd57b95f558cac92cb00b49735c0" }
```

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.USER.DUPLICATE_ROLE_ENTRY` | `Validation` | 400 | Cùng một `roleId` xuất hiện từ hai lần trở lên |
| `CORE.USER.NOT_FOUND` | `NotFound` | 404 | |
| `CORE.USER.ROLE_NOT_FOUND` | `BusinessRule` | 422 | Một `roleId` không tồn tại |
| `CORE.USER.ROLE_ESCALATION_FORBIDDEN` | `Forbidden` | 403 | Luật 1 §2 — áp cho **cả** vai trò được thêm **và** vai trò bị gỡ |
| `CORE.USER.SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN` | `BusinessRule` | 422 | Luật 2 §2 |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | `roleIds` là `null` |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Thiếu `core.user.role.assign` |

> **`roleIds` trùng lặp là 400, không phải "tự lọc trùng".** `ToDictionary` trên một mảng có khoá
> trùng ném `ArgumentException`, và exception đó không có nhánh xử lý nghiệp vụ nên client nhận
> **500** — trong khi đây rõ ràng là lỗi của client. Ở dự án tiền nhiệm, đúng lỗi này xảy ra ở
> **hai** endpoint khác nhau và phải vá cả hai. Chặn ở validator, tức **trước** khi chạm dữ liệu.

### Ghi chú — hiệu lực với phiên đang chạy

Đổi vai trò của một người — và đổi ma trận quyền của một vai trò
([`permissions.md`](permissions.md) §6) — **có hiệu lực từ request kế tiếp** của người bị ảnh
hưởng. Kiểm quyền đọc tập quyền hiệu lực từ database ở mỗi request, không từ cookie phiên, nên
không phải chờ đăng nhập lại.

Giao diện thì trễ hơn: tập `permissions` FE đang giữ là thứ nó lấy lần gần nhất từ
[`auth.md`](auth.md) §5. Người vừa bị gỡ quyền vẫn thấy nút cũ; bấm vào thì nhận 403, và **FE làm
mới tập quyền ngay khi nhận 403** ([`auth.md`](auth.md) §11; nhánh xử lý phía FE:
[`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md)). Tức là: hiển thị trễ tới lần bấm kế
tiếp, **chặn thì không trễ**. Đó là hướng an toàn.

---

## 8. `POST /api/v1/core/users/{id}/lock` · `POST /api/v1/core/users/{id}/unlock`

**Status:** DRAFT
**Quyền:** `core.user.lock`

### Request

```json
{ "version": "9d3b4c7e-2f10-4a8b-b1c5-6e7f8a9b0c1d" }
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `version` | ✔ | Token nhận từ `GET` gần nhất (§3, §4) — của **bản ghi đích** `{id}`, không phải của người gọi. Thiếu hoặc lệch ⇒ 409, vì `null` không bao giờ khớp. Áp cho **cả hai** endpoint |

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "521a9c95708ae661f48611af5402ec24" }
```

`data: null` — FE tải lại chi tiết (§4) để có `version` mới trước thao tác ghi kế tiếp.

### Lỗi

| `code` | `type` | HTTP | Khi nào | Áp cho |
| --- | --- | ---: | --- | --- |
| `CORE.USER.NOT_FOUND` | `NotFound` | 404 | | cả hai |
| `CORE.USER.CANNOT_LOCK_SELF` | `BusinessRule` | 422 | Luật 3 §2 | `lock` |
| `CORE.USER.SYSTEM_ROLE_LOCK_FORBIDDEN` | `Forbidden` | 403 | Luật 4 §2 | `lock` |
| `CORE.USER.LOCK_FAILED` | `BusinessRule` | 422 | Identity từ chối thao tác | cả hai |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11; mọi dòng áp cho **cả hai** endpoint:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Thiếu `core.user.lock` |
| `CORE.CONCURRENCY.CONFLICT` | `version` thiếu, hoặc lệch với bản ghi `{id}` trong database — một thao tác khác đã ghi xong sau khi FE đọc. Không ghi gì |

> **Thao tác hỏng là LỖI, không phải `200` kèm `data: false`.** Trả 200 cho một thao tác thất bại
> buộc FE kiểm hai thứ (status code **và** giá trị `data`) cho mỗi lời gọi, và cái thứ hai luôn bị
> quên. Đây là bản sửa một finding thật ở dự án tiền nhiệm.

### Ghi chú

Khoá đi qua `lockout_end` của Identity, **không** qua một cột `is_active` riêng
([`../database/schema-core.md`](../database/schema-core.md) §4.1). Dùng
`UserManager.SetLockoutEndDateAsync`, đặt `locked_by_admin = true`, rồi **đổi security stamp tường minh trong chính thao tác đó**
([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.4) — bỏ qua bước sau nghĩa
là phiên đang chạy của người vừa bị khoá **vẫn sống**.

`unlock` đặt `lockout_end = null`, `locked_by_admin = false`, và **không** đổi security stamp — phiên nào đang mở (nếu có) giữ nguyên, người vừa được mở khoá không phải đăng nhập lại.

**Phiên đang mở của người bị khoá bị chấm dứt ở request kế tiếp** — cùng nhịp với đổi vai trò ở
§7. `SecurityStamp` đã đổi, nên request kế tiếp của phiên đó trả 401 `CORE.AUTH.NOT_AUTHENTICATED`
([`auth.md`](auth.md) §11); đăng nhập lại **đúng mật khẩu** thì nhận `CORE.AUTH.LOCKED_OUT`, sai mật khẩu thì vẫn nhận `CORE.AUTH.INVALID_CREDENTIALS` ([`auth.md`](auth.md) §3).

---

## 9. `POST /api/v1/core/users/{id}/reset-password`

**Status:** DRAFT
**Quyền:** `core.user.reset-password`

Quản trị đặt mật khẩu tạm cho **người khác trong cùng đơn vị**. Ràng buộc trên tài khoản đích: luật 5 §2.

### Request

```json
{ "tempPassword": "…", "version": "9d3b4c7e-2f10-4a8b-b1c5-6e7f8a9b0c1d" }
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `tempPassword` | ✔ | Do **người gọi tự gõ**. Không xuất hiện trong response hay log |
| `version` | ✔ | Token nhận từ `GET` gần nhất (§3, §4) — của **bản ghi đích** `{id}`. Thiếu hoặc lệch ⇒ 409, vì `null` không bao giờ khớp |

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "1861d48b008ebaa8e4f0832a12495cd0" }
```

`data: null` — FE tải lại chi tiết (§4) để có `version` mới trước thao tác ghi kế tiếp.

Sau lệnh này, tài khoản đích có `mustChangePassword = true` và **mọi phiên đang mở của họ bị chấm
dứt ở request kế tiếp** — `SecurityStamp` đổi, cùng cơ chế và cùng nhịp với khoá ở §8. Lần đăng
nhập kế tiếp buộc đi qua
`POST /api/v1/core/auth/change-password-required` ([`auth.md`](auth.md) §7).

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.USER.NOT_FOUND` | `NotFound` | 404 | |
| `CORE.USER.RESET_PASSWORD_TARGET_FORBIDDEN` | `Forbidden` | 403 | Luật 5 §2 — tài khoản đích có quyền vượt người gọi, hoặc mang `has_permission_bypass` |
| `CORE.USER.CANNOT_RESET_OWN_PASSWORD` | `BusinessRule` | 422 | Luật 5 §2 — `{id}` là chính người gọi |
| `CORE.USER.RESET_PASSWORD_FAILED` | `BusinessRule` | 422 | Identity từ chối — mật khẩu không đạt chính sách. Lý do ở `fieldErrors` |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu `tempPassword` |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Thiếu `core.user.reset-password` |
| `CORE.CONCURRENCY.CONFLICT` | `version` thiếu, hoặc lệch với bản ghi `{id}` trong database — một thao tác khác đã ghi xong sau khi FE đọc. Không ghi gì |

### Ghi chú

**Khác hẳn `POST /api/v1/core/auth/change-password`:** endpoint này **không** đòi mật khẩu hiện
tại (quản trị không biết nó), và **chấm dứt** phiên của người bị đặt lại — trong khi endpoint tự
đổi thì giữ phiên hiện tại. Hai hành vi ngược nhau, và cả hai đều đúng cho ngữ cảnh của mình:
người tự đổi thì đang ngồi đó, người bị đặt lại thì không.

**Mật khẩu tạm không bao giờ xuất hiện trong response, trong log, hay trong `messageParams`.**
Nó đi từ người gọi tới `UserManager` và dừng ở đó.

---

## 10. Vì sao KHÔNG có `DELETE /api/v1/core/users/{id}`

**Người dùng không bị xoá.** Vô hiệu hoá đi qua `lock`.

| Lý do | Chi tiết |
| --- | --- |
| Cấm FK xuyên schema | Bảng module giữ id người dùng mà **không** có khoá ngoại ([`../database/schema-core.md`](../database/schema-core.md) §1.1). Xoá user để lại id mồ côi, và không có gì báo |
| Vết audit | `created_by`/`updated_by` giữ tên đăng nhập. Xoá tài khoản làm mọi bản ghi cũ trỏ tới một cái tên không tra ngược được nữa |
| Không có ca dùng thật | Yêu cầu thật gần như luôn là *"người này nghỉ việc, chặn họ vào"* — đúng nghĩa `lock`, không phải xoá |

Yêu cầu xoá dữ liệu cá nhân theo quy định pháp lý là **việc khác**, cần một quy trình ẩn danh hoá
riêng chứ không phải một nút `DELETE` — xem
[`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md).

---

## 11. Câu hỏi còn để ngỏ

Ghi ra để không ai tưởng đã chốt:

| # | Câu hỏi | Ai quyết |
| --- | --- | --- |
| 3 | `searchText` có tìm theo số điện thoại không? | `ba-analyst` |
| 4 | Khoá có cần lý do (`reason`) ghi vào nhật ký không? | Người dùng |
