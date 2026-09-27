---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0062 — Endpoint xuất dữ liệu (`GET` có ghi nhật ký kiểm toán) kiểm `X-XSRF-TOKEN` như lệnh ghi, khai bằng dấu tường minh trên action; FE tải tệp qua `HttpClient` thành blob

> **Trạng thái:** Đã chấp nhận (2026-09-22)

## Bối cảnh

Luật antiforgery ở [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.2 chỉ kiểm token cho method ghi; `GET` được nhường qua với điều kiện *"`GET` không được gây thay đổi trạng thái"*. Endpoint xuất theo khuôn [`../contracts/exports.md`](../contracts/exports.md) §1 là `GET` — nó nhận cùng tập tham số lọc với endpoint danh sách — nhưng **mỗi lần xuất ghi một dòng nhật ký kiểm toán**: handler `src/BE/Core/CoreAndSkill.Core.Application/Users/ExportUsersCommandHandler.cs` nhận `IAuditTrail auditTrail` và ghi `AuditActions.UserExport`. Đó là một `GET` có tác dụng phụ, và tác dụng phụ ấy là một bản ghi hệ thống không bao giờ xoá, mang tên người gọi.

Cookie phiên là `SameSite=Lax` ([`0015-fe-va-api-khac-nguon.md`](0015-fe-va-api-khac-nguon.md)). `Lax` **vẫn gửi cookie** theo điều hướng cấp cao nhất xuyên site — bấm một liên kết, `window.open`, form `GET` — và trình duyệt không gắn `Origin` cho điều hướng `GET`, nên lớp 1 của §7.2 không thấy gì. Hệ quả: một trang lạ đặt liên kết tới `…/users/export?format=csv`; người dùng đang đăng nhập bấm vào, server xuất toàn bộ danh sách, tệp rơi xuống máy họ, và nhật ký ghi rằng **họ** đã xuất. Kẻ tấn công không đọc được tệp — chính sách cùng nguồn chặn — nhưng nhật ký kiểm toán, thứ tồn tại để quy trách nhiệm, nay mang một dòng giả không phân biệt được với dòng thật; và một lần xuất tốn tới trần `Core:Export:MaxRows` dòng chạy theo ý người ngoài.

Trạng thái lúc quyết định: `UsersController.Export` là `[HttpGet("export")]` (`src/BE/Core/CoreAndSkill.Core.Web/Controllers/UsersController.cs`); `B4ExportEndpointTests.cs` gọi endpoint không kèm token; FE chưa có màn nào gọi export và chưa có mã tải blob nào. Ba phương án A, B, C dưới đây đã được trình bày; người dùng chọn C.

## Quyết định

Kiến trúc sư đề xuất, người dùng chốt ngày 2026-09-22:

1. **Endpoint xuất theo khuôn exports.md §1 kiểm `X-XSRF-TOKEN` như lệnh ghi** — cả hai lớp của §7.2, cùng hai mã `CORE.AUTH.CSRF_REJECTED` và `CORE.AUTH.ORIGIN_REJECTED`, và vẫn nhường 401 cho phiên hết hạn. Đây là **ngoại lệ có tên** của luật *"antiforgery chỉ áp cho lệnh ghi"*, ghi ngay trong bảng của §7.2. Không có ngoại lệ ngầm nào khác.
2. **Ngoại lệ khai tường minh trên action bằng một attribute đánh dấu**; middleware đọc metadata của endpoint. Không nhận diện bằng đường dẫn, không bằng tên action. Tên attribute do `backend-expert` chốt khi thi công; tài liệu luật cập nhật tên khi có, ADR này không ghi tên.
3. **Một `GET` ghi nhật ký kiểm toán mà không mang dấu là lỗ hổng, không phải lựa chọn thiết kế** — câu này nằm ở bảng §7.2 để người thêm endpoint xuất của một module đọc thấy.
4. **FE tải tệp xuất qua `HttpClient` với `responseType: 'blob'`** — không điều hướng, không `<a href>` trỏ thẳng API: điều hướng không mang được header. Request được đánh dấu tường minh để `authInterceptor` gắn token và `errorInterceptor` gửi lại đúng một lần khi `CSRF_REJECTED`, y như lệnh ghi. Tên tệp đọc từ `Content-Disposition`; BE khai header đó trong `Access-Control-Expose-Headers`. Luật ở [`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §6.4.
5. Luật **S20** ở [`../RULES.md`](../RULES.md) §6, trạng thái 📐 với tên cổng đã chốt.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ nguyên: `GET` không kiểm token, chấp nhận rủi ro

**Được:** không sửa gì; FE tải bằng `<a href>` — cách đơn giản nhất, có thanh tiến trình của trình duyệt.

**Vì sao loại:** rủi ro rơi đúng vào nhật ký kiểm toán, thứ không sửa được sau khi ghi (cùng tinh thần [`0054-o-ma-tran-chi-doi-bang-them-dong-va-xoa-mem.md`](0054-o-ma-tran-chi-doi-bang-them-dong-va-xoa-mem.md)). Một dòng giả trong nhật ký là bằng chứng giả.

### Phương án B — Đổi endpoint xuất thành `POST`

**Được:** antiforgery áp tự động theo method, không sửa middleware; hợp thói quen *"tác dụng phụ đi với `POST`"*.

**Mất:** tham số lọc của export **phải** trùng với endpoint danh sách — exports.md §1 nhận cùng tập tham số truy vấn, và [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §5.1 đòi cùng một chỗ dựng truy vấn. `POST` đẩy tham số vào thân request, tức một hình dạng thứ hai của cùng bộ lọc, hai validator, hai chỗ binding — đúng khuôn hai nguồn sẽ lệch. Endpoint đã dựng và đã kiểm ở B4; khuôn áp cho mọi module, nên đổi là đổi hợp đồng của mọi export tương lai.

**Vì sao loại:** cái giá B trả là hai nguồn cho một bộ lọc, trả mãi và ở mọi module; cái giá C trả là một attribute và một nhánh middleware, trả một lần ở Core.

### Phương án C — `GET` giữ nguyên, kiểm token theo dấu tường minh trên action (chọn)

**Mất:** một chiều ngoại lệ mới trong luật antiforgery, và FE mất cách tải đơn giản nhất — xem Hệ quả.

### Phương án D — Middleware nhận diện endpoint xuất bằng đường dẫn `…/export`, không cần attribute

**Vì sao loại:** middleware phải biết hình dạng URL của một khuôn contract — một chuỗi ẩn trong Core; module đặt tên khác là lọt; và không cổng nào giữ được quy ước đó. Attribute là metadata máy đọc được, ArchTest soi được từng action.

### Phương án E — Bỏ ghi nhật ký kiểm toán khi xuất để `GET` hết tác dụng phụ

**Vì sao loại:** nhật ký của lần xuất là yêu cầu có chủ đích — xuất là đường dữ liệu rời khỏi hệ thống ([`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §5.5). Hạ nó xuống log là bỏ đúng thứ cần giữ.

## Hệ quả

### Tích cực

- Dòng nhật ký của một lần xuất chỉ có thể do chính FE của phiên tạo ra.
- Ngoại lệ có tên, khai bằng metadata: người đọc controller thấy ngay; ArchTest kiểm được mọi action theo khuôn export.

### Tiêu cực

- **Luật *"antiforgery áp theo method, không theo endpoint"* ở [`../contracts/auth.md`](../contracts/auth.md) §1 nay có một chiều ngoại lệ.** Chiều này chỉ *thêm* kiểm, không bớt — nhưng nó là một danh sách, hôm nay một mục, và danh sách thì dài dần. Chốt kèm: mục mới chỉ được thêm khi `GET` đó ghi nhật ký kiểm toán hoặc đổi dữ liệu; loại tác dụng phụ khác cần ADR mới.
- **Export không còn mở được bằng URL.** Không dán link cho đồng nghiệp, không bookmark, không `curl` thiếu token. Đó là chủ đích, nhưng là thứ người dùng sẽ hỏi.
- **Tệp đi qua bộ nhớ trình duyệt** thay vì luồng tải của trình duyệt: trần `Core:Export:MaxRows` nay cũng là trần cho bộ nhớ phía FE; thanh tiến trình tải của trình duyệt không hiện, FE phải tự báo đang chờ.
- **Middleware kiểm `GET` theo dấu phải đi đường khác `IsRequestValidAsync`**: hàm đó trả `true` cho mọi method an toàn theo hợp đồng của chính nó, nên gọi nó cho một `GET` mang dấu là một cổng luôn xanh. Bẫy ghi ở [`../wiki-core/be/ly-do/be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §7.2.
- **Mẫu mã phía FE trong tài liệu luật lệch code cho tới khi FE thi công** — `authInterceptor` và `errorInterceptor` ở fe-api-client.md §2.1–§2.2 chưa có điều kiện "mang dấu". Sửa cùng lượt với code theo luật D44.
- `B4ExportEndpointTests` hiện gọi export không token sẽ đỏ — sửa cùng PR.

### Rút lui nếu sai

Gỡ attribute khỏi action, gỡ nhánh middleware; card và FE quay về `<a href>`. Dữ liệu không đổi, nhật ký đã ghi trong thời gian bật vẫn đúng. Chi phí một buổi.

### Dấu hiệu quyết định bắt đầu sai

- Mục thứ hai, thứ ba xin vào danh sách ngoại lệ với lý do không phải nhật ký kiểm toán hay đổi dữ liệu.
- Người dùng cần chia sẻ link xuất → xét cơ chế *xuất chạy nền, tải theo `resultFileId`* của [`../contracts/jobs.md`](../contracts/jobs.md) thay vì nới luật.

## Liên quan

- [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.2, §7.3, §7.5 · [`../contracts/exports.md`](../contracts/exports.md) §1 · [`../contracts/auth.md`](../contracts/auth.md) §1 · [`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §2.1, §6.4 · [`../RULES.md`](../RULES.md) S20.
- [`0015-fe-va-api-khac-nguon.md`](0015-fe-va-api-khac-nguon.md) — vì sao `SameSite=Lax` chưa đủ.
- [`0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md`](0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md) — nhật ký kiểm toán là bằng chứng.
