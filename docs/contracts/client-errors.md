---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Báo lỗi từ trình duyệt

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu 2026-09-20, **định tuyến và đường ghi log**). Card giữ
> `Status: DRAFT`: biên dịch được không phải điều kiện lên `IMPLEMENTED` ([`README.md`](README.md) §3).
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | `src/BE/Core/CoreAndSkill.Core.Web/Controllers/ClientErrorsController.cs` có action `Report` — khớp route §1 | Gọi thử thật, thay ví dụ bằng response thật |
> | `src/BE/Core/CoreAndSkill.Core.Application/ClientErrors/ReportClientErrorCommandHandler.cs` cắt `stack` còn `MaxStackLines` dòng, rồi đẩy mọi field văn bản qua `SensitiveTextRedactor.Redact` trước khi ghi — đúng hai ràng buộc ở §1 | — |
> | Danh sách trường mà `SensitiveTextRedactor` nhận ra có khớp [`../wiki-core/be/07-observability.md`](../wiki-core/be/07-observability.md) §5 hay không: **chưa đối chiếu** | Một lượt soi bộ lọc đó vào §5 |
> | Bảng lỗi §1 và giới hạn tần suất: **chưa ai đối chiếu** | Đối chiếu rồi mới lật `verified:` |
> | Phía FE (đối chiếu 2026-11-06): bản `production` gửi tới đây — `src/FE/src/app/core/observability/http-client-error-output.ts`, chuỗi `const DUONG_DAN_BAO_LOI = '/core/client-errors';`; `phienBanApp` lấy từ `src/FE/src/app/core/observability/client-error-reporter.ts`, chuỗi `phienBanApp: MA_COMMIT ?? '',`, rỗng thành `khong-ro`. Ca kiểm dùng `HttpTestingController`, chưa tới BE thật | Gọi thử thật — hàng đầu |
>
> Envelope, `ErrorType` → HTTP, khuôn mã lỗi, bảo mật chung: [`README.md`](README.md).
> Vì sao cần endpoint này và ba ràng buộc bắt buộc phía FE:
> [`../wiki-core/fe/10-observability.md`](../wiki-core/fe/10-observability.md) §4.

---

## 1. `POST /api/v1/core/client-errors`

**Status:** DRAFT
**Quyền:** `[AllowAnonymous]` — lỗi ở màn đăng nhập cũng phải báo được

Nhận một báo cáo lỗi runtime xảy ra trên trình duyệt người dùng và ghi vào log của server.

### Request

```json
{
  "kind": "TypeError",
  "message": "Cannot read properties of undefined (reading 'ten')",
  "stack": "at PhieuListPage.hienThi (main-A7F2.js:1:2481)",
  "duongDan": "/phieu/danh-sach",
  "traceId": "c1807b11710f43c1964ec2988f532e56",
  "phienBanApp": "3f9c2e7a1b5d4c8e0f6a2b9d7c3e1f5a8b4d6c20"
}
```

| Field | Kiểu | Bắt buộc | Ghi chú |
| --- | --- | --- | --- |
| `kind` | string | ✅ | Loại lỗi. Tối đa 100 ký tự |
| `message` | string | ✅ | Câu lỗi gốc, FE cắt ở 500 ký tự trước khi gửi. Chỉ văn bản nằm sẵn trong giá trị bị ném — quy tắc lấy: [`../wiki-core/fe/10-observability.md`](../wiki-core/fe/10-observability.md) §4.1 |
| `stack` | string | — | FE cắt ở 4000 ký tự. Thiếu vẫn nhận: một số trình duyệt không cho |
| `duongDan` | string | ✅ | Tối đa 2000 ký tự. Đường dẫn FE lúc lỗi, **không kèm query string** — query có thể mang giá trị người dùng nhập |
| `traceId` | string | — | Tối đa 100 ký tự. `traceId` của request BE hoàn tất gần nhất trước lỗi, nếu có. Đây là thứ nối báo cáo này với log BE — **suy luận, không phải nhân quả**: request đó không chắc là request gây ra lỗi ([`../wiki-core/fe/10-observability.md`](../wiki-core/fe/10-observability.md) §5.2) |
| `phienBanApp` | string | ✅ | Tối đa 100 ký tự. Bản build FE: mã commit đầy đủ mà bản dựng nhúng vào; bản dựng không có mã commit — máy dev — gửi `khong-ro`. Thiếu nó thì `stack` trỏ vào tệp băm không còn tồn tại và không lần ngược được |

Server tự đọc `User-Agent` và IP từ header — **không** nhận chúng từ thân request, vì giá trị do client gửi thì client sửa được.

### Response 200

```json
{
  "success": true,
  "data": null,
  "error": null,
  "traceId": "0c032e3ef835ea31065ab495b7671685"
}
```

### Lỗi

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11. Endpoint này `[AllowAnonymous]` nên **không** có nhánh 401:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu field bắt buộc, hoặc vượt giới hạn độ dài |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.RATE_LIMIT.EXCEEDED` | Vượt giới hạn tần suất. Kèm header `Retry-After` |

### Ghi chú

**Đây là endpoint best-effort.** FE gọi rồi quên: không thử lại, không hiện thông báo, không để lỗi của lần gọi này sinh ra một báo cáo mới — đó là vòng lặp cổ điển làm sập chính hệ thống thu lỗi.

**CSRF vẫn áp như mọi `POST`** ([`README.md`](README.md) §9). Không có ngoại lệ theo endpoint. Hệ quả: lỗi xảy ra **trước khi** FE lấy được token thì báo cáo bị bỏ — chấp nhận, vì đổi lại không phải mở một lỗ trong luật áp theo method. FE **không** được xin token chỉ để gửi báo cáo lỗi.

**Không gửi dữ liệu cá nhân hay nội dung form.** Ràng buộc này thuộc FE và nằm ở [`../wiki-core/fe/10-observability.md`](../wiki-core/fe/10-observability.md) §4.1. Phía server: nếu một field vượt giới hạn độ dài thì **từ chối**, không cắt bớt rồi lưu — cắt bớt là cách một chuỗi chứa dữ liệu nhạy cảm vẫn vào log dưới dạng đã cụt.

**Server ghi ra log, không dựng bảng riêng ở v1.** Một dòng log mức `Error` với nguồn là client, theo khuôn ở [`../wiki-core/be/07-observability.md`](../wiki-core/be/07-observability.md) §2.2, kèm `traceId` nhận được. Trước khi ghi: `stack` chỉ giữ **20 dòng đầu**; mọi field đi qua bộ lọc trường nhạy cảm theo danh sách ở [`../wiki-core/be/07-observability.md`](../wiki-core/be/07-observability.md) §5 — nội dung do trình duyệt soạn nên danh sách đó không tự áp. Chỉ người vận hành đọc, ở log; **không có màn nào đọc lại ở v1**. Bảng trong database chỉ cần khi phải truy vấn và thống kê — chưa có nhu cầu đó.

**Giới hạn tần suất là bắt buộc, không phải tuỳ chọn**: policy riêng theo IP, khai ở [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §6.
