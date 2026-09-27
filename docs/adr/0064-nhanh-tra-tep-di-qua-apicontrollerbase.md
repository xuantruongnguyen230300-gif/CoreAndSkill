---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0064 — Nhánh trả tệp đi qua `ApiControllerBase.HandleExport`; `ExportFileResult` giữ `internal`

> **Trạng thái:** Đã chấp nhận (2026-09-23) · Bổ sung bởi [ADR-0068](0068-nhanh-tai-tep-di-qua-handlefile.md) (2026-09-23) — nhánh **tải tệp** nhận lối riêng `HandleFile`; mọi quyết định của ADR này giữ nguyên hiệu lực

## Bối cảnh

[`../contracts/exports.md`](../contracts/exports.md) §1 là **card khuôn**: mỗi tài nguyên, kể cả của module hạ nguồn, có endpoint xuất của riêng nó và phải theo đúng khuôn đó. Kiến trúc sư đối chiếu ngày 2026-09-23:

| Đo gì | Kết quả |
| --- | --- |
| Lớp dựng phản hồi tệp | `src/BE/Core/CoreAndSkill.Core.Web/Http/ExportFileResult.cs` — `internal sealed class ExportFileResult`, đặt `Content-Disposition`, `X-Content-Type-Options: nosniff`, `Cache-Control: no-store` rồi ghi thẳng ra luồng |
| Cách action gọi | `UsersController.Export` tự `new ExportFileResult(result.Value)` trong thân action |
| Module gọi được không | **Không** — `internal` chỉ mở trong assembly `Core.Web`; controller của module nằm ở assembly khác |
| Cổng S20 nhận diện action xuất bằng gì | Nhánh cú pháp của `AntiforgeryMarkScanner` tìm `new ExportFileResult(...)` trong thân action |

Hệ quả: module **không dựng được** khuôn xuất. Đường vòng duy nhất còn lại là tự viết một `IActionResult` khác — và khi đó nó tự đặt ba header bảo mật ở trên, tự đặt tên tệp, tự quyết có `no-store` hay không; ba thứ này là luật, không phải tuỳ chọn ([ADR-0050](0050-v1-chua-quet-ma-doc-siet-zip-va-duoi-tai-xuong.md) biện pháp 2, [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md) §6). Cổng S20 cũng không thấy action đó, nên nó vừa thiếu header vừa thiếu dấu antiforgery mà không gì báo.

## Quyết định

Kiến trúc sư chốt:

1. **`ApiControllerBase` nhận một lối đặt status thứ tư: `HandleExport(Result<ExportFile> result)`** — thành công trả nhánh tệp, thất bại đi chung `Failure` với `HandleResult`. `ExportFile` đã là kiểu công khai của `Core.Application`, nên chữ ký dùng được từ assembly module.
2. **`ExportFileResult` giữ `internal`.** Chi tiết HTTP của nhánh tệp — ba header, cách ghi luồng — là việc của `Core.Web`, không phải bề mặt module phải biết.
3. **`UsersController.Export` đổi sang `HandleExport`**, để trong toàn repo chỉ có **một** hình dạng cho nhánh tệp.
4. **Cổng S20 nhận diện action xuất bằng lời gọi `HandleExport`**, giữ thêm `ExportFileResult` làm dấu hiệu thứ hai để một action trong `Core.Web` đi vòng qua helper vẫn bị thấy.
5. Luật §3 của [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) đổi từ *"hai chỗ duy nhất đặt status"* thành **ba**, và câu *"nhánh trả tệp là ngoại lệ có tên"* mất hiệu lực: nhánh tệp nay đi qua lớp cơ sở như hai nhánh kia.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Mở `ExportFileResult` thành `public`

**Được:** một dòng sửa; module `new` nó y như `UsersController` đang làm; cổng S20 không phải đổi.

**Mất:** giữ nguyên ngoại lệ *"controller tự dựng `IActionResult`"* ở §3 — và đó là ngoại lệ duy nhất, tức luật *"chỉ `Handle*` đặt status"* có một lỗ mà mọi controller đều dùng được. Bề mặt công khai của `Core.Web` lớn thêm một kiểu mà module không cần biết.

**Vì sao loại:** nó mở đúng thứ module **không** cần (lớp kết quả HTTP) thay vì thứ module cần (một lối gọi có sẵn), và giữ lại một ngoại lệ mà quyết định này gỡ được miễn phí.

### Phương án B — Module tự viết `IActionResult` của mình, Core chỉ khai hợp đồng bằng văn bản

**Vì sao loại:** ba header bảo mật và quy tắc đặt tên tệp khi ấy sống trong tài liệu chứ không trong code — mỗi module một bản, và bản quên `no-store` không làm gì đỏ. Đúng khuôn "hai nguồn sẽ lệch" mà [`../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5 cấm.

### Phương án C — `HandleExport` nhận `ExportFile` trần, không nhận `Result<ExportFile>`

**Vì sao loại:** chỗ gọi lại phải tự rẽ nhánh `IsSuccess`, tức lại tự quyết status cho nhánh lỗi — đúng thứ ba `Handle*` tồn tại để gom.

## Hệ quả

### Tích cực

- Module dựng được khuôn xuất mà không chạm chi tiết HTTP nào; quên header bảo mật là chuyện không xảy ra được.
- Luật §3 hết ngoại lệ: **mọi** status đi qua ba `Handle*`.
- Cổng S20 có một dấu hiệu ổn định để nhận diện action xuất của module.

### Tiêu cực

- **Bề mặt của `ApiControllerBase` lớn thêm một method** — và mỗi method ở lớp cơ sở là một thứ mọi controller nhìn thấy, kể cả controller không bao giờ xuất gì.
- **Ba chỗ phải sửa cùng lượt**: lớp cơ sở, `UsersController.Export`, và nhánh cú pháp của `AntiforgeryMarkScanner`. Sửa lệch nhau thì cổng S20 mù đúng ca nó sinh ra để bắt — nên PR thi công phải chạy lại `AntiforgeryMarkTests` và thấy ca đối chứng *thiếu dấu* vẫn đỏ.
- **Quyết định này chỉ dựng được khuôn, không dựng được nghiệp vụ**: module vẫn phải tự viết handler, quyền xuất riêng, và `[RequireAntiforgery]` (S20). `HandleExport` không nhắc module khai dấu — cổng S20 mới là thứ nhắc.

### Rút lui nếu sai

Gỡ `HandleExport`, mở `ExportFileResult` thành `public` (phương án A) và trả `UsersController.Export` về dạng `new`. Nửa buổi; không dữ liệu nào đổi.

## Liên quan

- [`0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md`](0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md) — dấu `[RequireAntiforgery]` và cổng S20.
- [`0050-v1-chua-quet-ma-doc-siet-zip-va-duoi-tai-xuong.md`](0050-v1-chua-quet-ma-doc-siet-zip-va-duoi-tai-xuong.md) — quy tắc đặt tên tệp tải xuống.
- [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §3, §2.2 · [`../contracts/exports.md`](../contracts/exports.md) §1.
