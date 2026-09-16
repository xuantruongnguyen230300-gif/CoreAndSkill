---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0034 — `ErrorType.Unexpected`: 500 cũng đi qua bảng ánh xạ và mang mã

> **Trạng thái:** Đã chấp nhận (2026-09-16) · Bổ sung [`0027-errortype-unauthorized.md`](0027-errortype-unauthorized.md)
>
> **Bổ sung 0027.** Sáu giá trị và luật "`Unauthorized` chỉ hạ tầng phát" giữ nguyên; ADR này thêm giá trị thứ bảy cùng khuôn.

## Bối cảnh

Lượt đối chiếu tài liệu 2026-09-16 tìm ra ba mô tả khác nhau cho cùng một việc: lỗi đồng thời được "handler trả `Result`", "behavior dịch", hoặc "`IExceptionHandler` dịch"; và dòng 500 trong bảng ánh xạ không có `ErrorType` nào, trong khi luật R6 buộc mọi envelope lỗi mang `code`. `Error` là record bắt buộc có `ErrorType`, nên một mã 500 không thể khai vào catalog nếu enum không có giá trị cho nó.

## Quyết định

1. **`ErrorType` có giá trị thứ bảy `Unexpected`**, ánh xạ 500. Khai ở [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §2.1; hàng ánh xạ ở [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §1.1.
2. **Chỉ `IExceptionHandler` phát `Unexpected`** — handler, validator, Domain không bao giờ trả nó (luật R9 mở rộng). `CommonErrors` ở `Core.Application` là ngoại lệ có tên: nó **khai** `Unexpected` và `ConcurrencyConflict`, không trả.
3. **`DbUpdateConcurrencyException` được dịch ở đúng một chỗ: `IExceptionHandler`** → 409 `CORE.CONCURRENCY.CONFLICT`. Handler không bắt, vì `SaveChangesAsync` nằm trong `TransactionBehavior` ([`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §5.3).

## Phương án đã cân nhắc và vì sao loại

| Phương án | Vì sao loại |
| --- | --- |
| Giữ 500 ngoài `ErrorType`, `IExceptionHandler` dựng envelope tay với mã cứng | Mã cứng nằm ngoài catalog — luật R6 kiểm được envelope có `code`, nhưng không kiểm được mã đó có trong catalog; hai file đã lệch nhau đúng ở chỗ này |
| Gắn `CORE.SYSTEM.UNEXPECTED` vào `ErrorType.BusinessRule` | Sai nghĩa: `BusinessRule` → 422; bảng ánh xạ và `type` trên dây sẽ nói dối |
| Handler bắt `DbUpdateConcurrencyException` và trả `Result` | Handler không thấy exception đó — `SaveChangesAsync` chạy trong behavior sau khi handler đã trả |

## Hệ quả

- ArchTest R9 quét thêm `ErrorType.Unexpected`; ngoại lệ có tên là lớp `CommonErrors`.
- FE không rẽ nhánh theo `type` ([`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §1) nên không đổi gì phía FE ngoài câu dịch `loi.CORE.SYSTEM.UNEXPECTED`.
- Mã dùng chung ở [`../contracts/auth.md`](../contracts/auth.md) §11 có thêm một hàng.

## Liên quan

- [`0027-errortype-unauthorized.md`](0027-errortype-unauthorized.md) — giá trị thứ sáu và khuôn "chỉ hạ tầng phát".
- [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §2.4 — danh sách đường dựng envelope.
