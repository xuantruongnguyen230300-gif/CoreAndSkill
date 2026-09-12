---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Báo lỗi từ trình duyệt

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Card trong file này mang `Status: DRAFT`.
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
  "traceId": "0HNO9S8JAP586:00000012",
  "phienBanApp": "2026.09.10-a1b2c3d"
}
```

| Field | Kiểu | Bắt buộc | Ghi chú |
| --- | --- | --- | --- |
| `kind` | string | ✅ | Loại lỗi. Tối đa 100 ký tự |
| `message` | string | ✅ | Câu lỗi gốc, FE cắt ở 500 ký tự trước khi gửi |
| `stack` | string | — | FE cắt ở 4000 ký tự. Thiếu vẫn nhận: một số trình duyệt không cho |
| `duongDan` | string | ✅ | Đường dẫn FE lúc lỗi, **không kèm query string** — query có thể mang giá trị người dùng nhập |
| `traceId` | string | — | `traceId` của request BE gần nhất, nếu có. Đây là thứ nối báo cáo này với log BE |
| `phienBanApp` | string | ✅ | Bản build FE. Thiếu nó thì `stack` trỏ vào tệp băm không còn tồn tại và không lần ngược được |

Server tự đọc `User-Agent` và IP từ header — **không** nhận chúng từ thân request, vì giá trị do client gửi thì client sửa được.

### Response 200

```json
{
  "success": true,
  "data": null,
  "error": null,
  "traceId": "0HNO9S8JAP586:00000013"
}
```

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | Thiếu field bắt buộc, hoặc vượt giới hạn độ dài |
| `CORE.AUTH.CSRF_REJECTED` | `Forbidden` | 403 | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.RATE_LIMIT.EXCEEDED` | — | 429 | Vượt giới hạn tần suất. Kèm header `Retry-After` |

### Ghi chú

**Đây là endpoint best-effort.** FE gọi rồi quên: không thử lại, không hiện thông báo, không để lỗi của lần gọi này sinh ra một báo cáo mới — đó là vòng lặp cổ điển làm sập chính hệ thống thu lỗi.

**CSRF vẫn áp như mọi `POST`** ([`README.md`](README.md) §9). Không có ngoại lệ theo endpoint. Hệ quả: lỗi xảy ra **trước khi** FE lấy được token thì báo cáo bị bỏ — chấp nhận, vì đổi lại không phải mở một lỗ trong luật áp theo method. FE **không** được xin token chỉ để gửi báo cáo lỗi.

**Không gửi dữ liệu cá nhân hay nội dung form.** Ràng buộc này thuộc FE và nằm ở [`../wiki-core/fe/10-observability.md`](../wiki-core/fe/10-observability.md) §4.1. Phía server: nếu một field vượt giới hạn độ dài thì **từ chối**, không cắt bớt rồi lưu — cắt bớt là cách một chuỗi chứa dữ liệu nhạy cảm vẫn vào log dưới dạng đã cụt.

**Server ghi ra log, không dựng bảng riêng ở v1.** Một dòng log mức `Error` với nguồn là client, theo khuôn ở [`../wiki-core/be/07-observability.md`](../wiki-core/be/07-observability.md) §2.2, kèm `traceId` nhận được. Bảng trong database chỉ cần khi phải truy vấn và thống kê — chưa có nhu cầu đó.

**Giới hạn tần suất là bắt buộc, không phải tuỳ chọn**: policy riêng theo IP, khai ở [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §6.
