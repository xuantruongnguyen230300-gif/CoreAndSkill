---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `contracts/` — hợp đồng API giữa BE và FE

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`. **Mọi card trong khu này mang
> `Status: DRAFT`** — không endpoint nào đã được viết, và không endpoint nào BE đã cam kết làm.

## 1. Khu này là gì

Một **card** mô tả đúng một endpoint: gọi thế nào, cần quyền gì, trả về gì, **và hỏng ra sao**.
Card là nơi FE và BE thoả thuận **trước khi** cả hai viết code, để hai bên làm song song mà không
phải chờ nhau.

| Card | Nội dung |
| --- | --- |
| [`auth.md`](auth.md) | Đăng nhập, đăng xuất, người dùng hiện tại, đổi mật khẩu, XSRF token |
| [`users.md`](users.md) | CRUD người dùng, danh sách, khoá/mở khoá, gán vai trò, đặt lại mật khẩu |
| [`permissions.md`](permissions.md) | Ma trận quyền, ma trận theo tài nguyên, danh mục permission |
| [`meta-menu.md`](meta-menu.md) | Menu động theo quyền, metadata lưới/form |
| [`client-errors.md`](client-errors.md) | Báo lỗi runtime của trình duyệt về server |
| [`tenants.md`](tenants.md) | Khu quản trị hệ thống: danh sách đơn vị, tạo đơn vị, ngưng và bật lại |
| [`roles.md`](roles.md) | Quản trị vai trò: danh sách, tạo, đổi tên, xoá |
| [`profile.md`](profile.md) | Hồ sơ cá nhân: xem và tự sửa thông tin cơ bản |
| [`files.md`](files.md) | Tải tệp đính kèm lên, tải xuống có kiểm quyền, gỡ tệp |
| [`exports.md`](exports.md) | **Khuôn** xuất và nhập dữ liệu — mỗi tài nguyên theo khuôn này |
| [`notifications.md`](notifications.md) | Thông báo trong ứng dụng: danh sách, số chưa đọc, đánh dấu đã đọc |

**Không thuộc khu này — đây là file chủ ở nơi khác, card chỉ được trỏ tới:**

| Chủ đề | File chủ |
| --- | --- |
| Hình dạng envelope, `Result` → HTTP, rate limit, CORS/antiforgery | [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) |
| `Result<T>`, `Error`, `ErrorType`, `ErrorDescriptor`, `PagedList<T>`, khuôn mã lỗi | [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) |
| Bảng, cột, index | [`../database/schema-core.md`](../database/schema-core.md) |

Mục §4–§7 dưới đây **trỏ đường** sang các file chủ đó và chỉ giữ phần thuộc về riêng khu
`contracts/`. Chúng **không nhắc lại** định nghĩa — một bản nhắc lại là một bản sao, và bản
sao sẽ lệch ([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

## 2. Khuôn một card

Mỗi card có đúng các mục sau, theo đúng thứ tự:

```markdown
## `POST /api/v1/core/…`

**Status:** DRAFT
**Quyền:** `core.user.write`

Mô tả một câu: endpoint này làm gì.

### Request
Schema + ví dụ JSON.

### Response 200
Schema + ví dụ JSON.

### Lỗi
Bảng: mã lỗi · `ErrorType` · HTTP · khi nào xảy ra.

### Ghi chú
Bẫy, ràng buộc, thứ FE phải biết.
```

**Mục "Lỗi" là mục quan trọng nhất, và là mục hay bị bỏ nhất.** FE không đoán được tập lỗi có
thể xảy ra; thiếu nó, FE sẽ viết đúng một nhánh `catch` chung và mọi lỗi nghiệp vụ hiện ra thành
*"Đã có lỗi xảy ra"*. Một card không liệt kê hết mã lỗi là một card chưa xong.

## 3. Ba trạng thái

| `Status:` | Nghĩa | BE đã cam kết chưa |
| --- | --- | --- |
| `DRAFT` | Đề xuất. Hình dạng có thể đổi bất cứ lúc nào | **CHƯA.** FE code theo card DRAFT là chấp nhận rủi ro phải sửa |
| `AGREED` | Hai bên đã chốt hình dạng. Đổi phải qua thoả thuận lại | Rồi — nhưng code có thể chưa có |
| `IMPLEMENTED` | Code đã có, đã gọi thử thật, ví dụ trong card là body thật | Rồi, và chạy được |

**`DRAFT` → `AGREED`** khi BE và FE cùng xác nhận. **`AGREED` → `IMPLEMENTED`** chỉ khi đã gọi
thử endpoint thật và **thay ví dụ trong card bằng response thật** — không phải khi code biên dịch
xong.

> 🛑 Giai đoạn 1 chưa có `src/`, nên **không card nào được mang `IMPLEMENTED`**. Dán nhãn đó cho
> một endpoint chưa ai viết là đúng khuôn sai mà [`../RULES.md`](../RULES.md) §1 **D16** cấm —
> luật *"không tuyên bố `CÓ THẬT` khi repo chưa có `src/` để đối chiếu"*.

## 4. Envelope

> 📖 **Hình dạng envelope (kiểu C#, ba khối JSON mẫu, chỗ dựng duy nhất, luật casing):
> đọc [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §2.**

Card **không** lặp lại hình dạng envelope. Khối JSON trong một card chỉ mô tả phần `data` hoặc
phần `error` **của riêng endpoint đó**; phần bao ngoài đọc ở file chủ.

Bốn luật card nào cũng phải tuân:

1. **`error` là `null` khi thành công, `data` là `null` khi lỗi.** Không bao giờ cả hai cùng có
   giá trị.
2. **`traceId` luôn có mặt**, kể cả khi thành công.
3. **FE phân nhánh theo `error.code`, KHÔNG theo `error.message`.** `message` là dev-facing;
   câu hiển thị do FE dựng từ `code` + `messageParams`. Câu chữ đổi theo ngôn ngữ người dùng chọn
   ngay trong app, nên so khớp câu chữ là biến câu thành hợp đồng ngầm — và hợp đồng đó vỡ ngay
   lần đổi câu đầu tiên. Ở dự án tiền nhiệm, đúng luật này đã phải lật ngược sau khi tài liệu
   từng viết *"FE hiển thị thẳng `message` là đủ"*.
4. **Payload camelCase, nhưng KHOÁ của `fieldErrors` giữ PascalCase** — khớp tên property C# của
   DTO. Card viết đúng chuỗi FE sẽ tra (`fieldErrors["Email"]`), không viết `"email"`.

## 5. `ErrorType` → HTTP status

> 📖 **Bảng ánh xạ loại lỗi → mã HTTP: đọc [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §1.**
>
> Bảng đó **cố ý không được chép lại vào đây.** Cột `ErrorType` trong bảng "Lỗi" của mỗi card tra thẳng ở file chủ.

Ba status còn lại **không** đến từ `ErrorType` — chúng do hạ tầng dựng envelope tay, và vẫn mang
`code` (luật R6):

| HTTP | `code` | Ai sinh |
| ---: | --- | --- |
| 405 | `CORE.ROUTE.METHOD_NOT_ALLOWED` | Middleware định tuyến. Kèm header `Allow` |
| 429 | `CORE.RATE_LIMIT.EXCEEDED` | Rate limiter. Kèm header `Retry-After` (giây) |
| 500 | — | Bộ xử lý exception toàn cục. **Không** lộ chi tiết, chỉ `traceId` |

> **`CORE.ROUTE.NOT_FOUND` ≠ `CORE.USER.NOT_FOUND`.** Cùng HTTP 404, cùng `type: "NotFound"`.
> `code` là thứ **duy nhất** phân biệt "FE gọi sai URL" (bug của FE) với "bản ghi không tồn tại"
> (dữ liệu, cần hiển thị cho người dùng).

## 6. Khuôn mã lỗi

> 📖 **Khuôn mã lỗi, bảng ba thành phần, biểu thức kiểm và catalog: đọc
> [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §7.3.**

Phần thuộc riêng khu này ở dưới. Khuôn **không** được chép lại ở đây — hai bản khuôn đã đồng bộ sẽ lệch lại, và người sửa chỉ sửa một bên ([`../OWNERSHIP.md`](../OWNERSHIP.md) §1).

Luật R3 ([`../RULES.md`](../RULES.md) §5) ép hai điều: **khớp khuôn** và **duy nhất trong toàn
hệ** — ArchTest `EveryBusinessCode_Matches_Format` và `_IsUnique`. Trùng mã giữa hai module làm
FE hiển thị sai câu ở đúng một trong hai chỗ, và không có gì báo.

Mã trong `fieldErrors` dùng **cùng khuôn** với mã ở gốc — không phải một hệ đặt tên thứ hai.

## 7. Đường dẫn và versioning

> 📖 **Tiền tố đường dẫn, quy tắc phát hành, bảng "thay đổi nào là breaking": đọc
> [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §8.1.**

Phần thuộc riêng khu này: mỗi card ghi đường dẫn **đầy đủ kèm phiên bản** ngay ở tiêu đề
— tiêu đề card viết `POST /api/v1/core/users`, không viết `POST /api/core/users`. Viết thiếu `/v1` là để lại một chuỗi mà người thi công sẽ
chép thẳng vào `[Route]`, và một tiền tố lệch làm hỏng cả những thứ không liên quan gì tới
định tuyến — xem cảnh báo ở §8.1 của file chủ.

Version cũ sống song song đúng một chu kỳ phát hành, và **ngày gỡ ghi trong chính card**.

## 8. Phân trang, sắp xếp, lọc — dùng chung cho mọi danh sách

Tên tham số theo query record ở
[`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §9, camelCase trên dây:

| Tham số | Kiểu | Hợp lệ | Bỏ trống | Ngoài khoảng |
| --- | --- | --- | --- | --- |
| `page` | int | `>= 1` | `1` | **400** `CORE.VALIDATION.FAILED`, `fieldErrors["Page"]` |
| `pageSize` | int | `1..200` | `20` | **400**, `fieldErrors["PageSize"]` |
| `sortBy` | string | Tên **field của DTO**, thuộc allowlist của endpoint | Mặc định của endpoint | **400**, `fieldErrors["SortBy"]` |
| `sortDescending` | bool | | `false` | — |

Bộ lọc là **field rời** trên query record (`roleId`, `status`), không phải một chuỗi biểu thức.

**Response** — hình dạng duy nhất cho mọi danh sách, bọc trong envelope §4:

```json
{
  "success": true,
  "data": { "items": [], "page": 1, "pageSize": 20, "totalCount": 137 },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000004"
}
```

Ba quy tắc, mỗi cái đều đắt nếu làm sai:

1. **Giá trị ngoài khoảng là LỖI, không được vá âm thầm về mặc định.** Vá âm thầm khiến người gọi
   sai không bao giờ biết mình sai, và khiến hai nơi cùng "sở hữu" một quy tắc — validator và
   handler. Quy tắc chỉ được quyết ở **một** nơi: validator.
2. **`pageSize` phải có TRẦN.** Không có trần thì `?pageSize=1000000` đi thẳng xuống
   `Take(1000000)` và kéo trọn bảng trong một request.
3. **`sortBy` đi qua allowlist theo từng endpoint**, và sắp xếp **luôn có tiêu chí phụ ổn định**
   (thêm `id` làm tiêu chí cuối). Thiếu tiêu chí phụ, dữ liệu có giá trị trùng cho thứ tự không
   xác định giữa các trang: một bản ghi hiện ở cả trang 1 lẫn trang 2, một bản ghi khác không
   hiện ở đâu cả.

## 9. Bảo mật chung cho mọi endpoint

| | Luật |
| --- | --- |
| Xác thực | **Cookie phiên** (ASP.NET Core Identity), **KHÔNG JWT**. FE gọi với `withCredentials: true` |
| CSRF | Mọi `POST`/`PUT`/`PATCH`/`DELETE` phải mang header `X-XSRF-TOKEN`. Áp theo **method**, không có ngoại lệ theo endpoint — kể cả đăng nhập. Xem [`auth.md`](auth.md) |
| Phân quyền | Kiểm bằng **permission**, không bằng tên vai trò (luật S2). Mỗi card khai `Quyền:` |
| Chưa đăng nhập | **401 JSON sạch**, không 302 redirect. FE là SPA; redirect làm `HttpClient` nhận về 200 kèm một trang HTML |
| Rate limit | Mọi endpoint đều có thể trả **429**, không riêng màn đăng nhập |
| 404 / 405 do định tuyến | Cũng mang envelope §4. Thân rỗng là bug |
