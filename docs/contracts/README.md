---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `contracts/` — hợp đồng API giữa BE và FE

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/` — không endpoint nào đã được viết. BE đã cam
> kết endpoint nào chưa: **đọc dòng `Status:` của từng card** (§3).

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
| [`tenants.md`](tenants.md) | Khu quản trị hệ thống: danh sách đơn vị, tạo đơn vị, ngưng và bật lại, khôi phục tài khoản quản trị đơn vị, tạo tài khoản quản trị mới |
| [`roles.md`](roles.md) | Quản trị vai trò: danh sách, chi tiết, tạo, đổi tên, xoá |
| [`profile.md`](profile.md) | Hồ sơ cá nhân: xem và tự sửa thông tin cơ bản, tự bỏ cờ bỏ qua kiểm quyền |
| [`files.md`](files.md) | Tải tệp đính kèm lên, tải xuống có kiểm quyền, gỡ tệp |
| [`exports.md`](exports.md) | **Khuôn** xuất và nhập dữ liệu — mỗi tài nguyên theo khuôn này |
| [`jobs.md`](jobs.md) | Theo dõi một việc chạy nền: trạng thái, kết quả |
| [`notifications.md`](notifications.md) | Thông báo trong ứng dụng: danh sách, số chưa đọc, đánh dấu đã đọc |

**Không thuộc khu này — đây là file chủ ở nơi khác, card chỉ được trỏ tới:**

| Chủ đề | File chủ |
| --- | --- |
| Hình dạng envelope, `Result` → HTTP, rate limit, CORS/antiforgery | [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) |
| `Result<T>`, `Error`, `ErrorType`, `ErrorDescriptor`, `PagedList<T>`, khuôn mã lỗi | [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) |
| Token đồng thời đi trên dây thế nào — field `version` | [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6.3 |
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
Bảng 1 — mã RIÊNG của endpoint: mã lỗi · `ErrorType` · HTTP · khi nào xảy ra.
Bảng 2 — mã DÙNG CHUNG: mã lỗi · khi nào xảy ra, kèm dòng trỏ `auth.md` §11.

### Ghi chú
Bẫy, ràng buộc, thứ FE phải biết.
```

**Bảng 2 KHÔNG có cột `type` và HTTP.** Mã dùng chung có đúng một chủ —
[`auth.md`](auth.md) §11 — và chép hai cột đó vào từng card là dựng lại chính bản sao mà
[`../OWNERSHIP.md`](../OWNERSHIP.md) §1 cấm: đổi ánh xạ của một mã dùng chung thì phải sửa mọi
card, và người sửa sẽ chỉ sửa một. Card giữ cột *"khi nào"* vì **ca** sinh ra mã là thứ riêng của
endpoint đó.

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

Hai status còn lại **không** đến từ `ErrorType` — 405 do định tuyến, 429 do rate limiter. Hạ tầng dựng
envelope tay, và envelope đó vẫn mang `code` (luật R6). 500 đi qua `ErrorType.Unexpected` — chỉ bộ xử
lý exception toàn cục phát, **không** lộ chi tiết, chỉ `traceId`.

> 📖 **Mã của 405, 429 và mọi mã hạ tầng dùng chung khác: đọc [`auth.md`](auth.md) §11.**

409 do xung đột đồng thời có **một** khuôn trên dây cho mọi endpoint ghi đè bản ghi: `GET` trả
`version`, request ghi gửi lại trong body. Card chỉ khai field `version` ở bảng field của mình,
**không** tả lại cơ chế.

> 📖 **Token đồng thời — nguồn giá trị, đi trên dây thế nào, endpoint nào không mang nó: đọc
> [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6.3.**

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

## 8. Tham số danh sách dùng chung: phân trang, sắp xếp, lọc — định nghĩa gốc

Bảng dưới là tên **trên dây**, camelCase, cho mọi danh sách. Query record phía BE
([`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §9) theo bảng này:

| Tham số | Kiểu | Hợp lệ | Bỏ trống | Ngoài khoảng |
| --- | --- | --- | --- | --- |
| `page` | int | `>= 1` | `1` | **400** `CORE.VALIDATION.FAILED`, `fieldErrors["Page"]` |
| `pageSize` | int | `1..200` | `20` | **400**, `fieldErrors["PageSize"]` |
| `sortBy` | string | Tên **field của DTO**, thuộc allowlist của endpoint | Mặc định của endpoint | **400**, `fieldErrors["SortBy"]` |
| `sortDescending` | bool | | `false` | — |
| `searchText` | string | Chuỗi tìm; card khai **field nào** được tìm và trần độ dài | Không lọc | **400**, `fieldErrors["SearchText"]` |

Bộ lọc là **field rời** trên query record (`roleId`, `status`), không phải một chuỗi biểu thức.

**Lựa chọn số dòng mỗi trang trên giao diện** — `pageSizeOptions` của dải phân trang
([`../Design/Components/Pagination.md`](../Design/Components/Pagination.md)): **`10`, `20`, `50`, `100`**.
Mặc định `20` của `pageSize` là một phần tử của danh sách này. Trần `200` là giới hạn API áp cho
**mọi** người gọi, không phải một lựa chọn trên giao diện: người dùng chỉ chọn được bốn giá trị
trên, còn API vẫn nhận mọi giá trị trong `1..200`. Danh sách khai **một lần** ở đây; component và
màn hình truyền từ nguồn này, không giữ mặc định riêng.

Card của từng endpoint danh sách **không** khai lại bảng trên. Card chỉ khai phần riêng: allowlist
`sortBy` và giá trị mặc định của nó, phạm vi của `searchText`, và bộ lọc riêng.

**Response** — hình dạng duy nhất cho mọi danh sách, bọc trong envelope §4:

```json
{
  "success": true,
  "data": { "items": [], "page": 1, "pageSize": 20, "totalCount": 137 },
  "error": null,
  "traceId": "a6a8bce037eb102c3057e2bc84b48611"
}
```

Phần tử của `items` là DTO riêng của endpoint. Bản ghi mà màn hình ghi đè được thì phần tử mang
`version` — cùng field, cùng khuôn với `GET` chi tiết
([`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6.3).

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
| Phân quyền | Kiểm bằng **permission**, không bằng tên vai trò (luật S2). Mỗi card khai `Quyền:` bằng **đúng một** mức của luật S11 — một khoá quyền (`[RequirePermission]`), `[RequireSystemOperator]`, hoặc `[AuthenticatedOnly("<lý do>")]` với chuỗi lý do viết ngay trong card để người thi công chép nguyên vào attribute — hoặc ghi rõ không cần đăng nhập. Quyết định: [`../adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md`](../adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md) |
| Chưa đăng nhập | **401 JSON sạch**, không 302 redirect. FE là SPA; redirect làm `HttpClient` nhận về 200 kèm một trang HTML |
| Rate limit | Mọi endpoint đều có thể trả **429**, không riêng màn đăng nhập |
| 404 / 405 do định tuyến | Cũng mang envelope §4. Thân rỗng là bug |
