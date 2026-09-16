---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0027 — `ErrorType` có giá trị thứ sáu `Unauthorized`, chỉ hạ tầng xác thực ở `Core.Web` được phát; ánh xạ sang HTTP không có nhánh mặc định

> **Trạng thái:** Đã chấp nhận (2026-09-14) · Bổ sung bởi [ADR-0034](0034-errortype-unexpected.md) (2026-09-16)
>
> **Bổ sung [`0003-result-thuan.md`](0003-result-thuan.md).** Mô hình `Result<T>` giữ nguyên. ADR này mở tập đóng `ErrorType` thêm một giá trị, giới hạn ai được dùng giá trị đó, và làm cho câu *"quên xử lý một `ErrorType` là lỗi biên dịch"* của ADR-0003 trở thành đúng.

## Bối cảnh

ADR-0003 chốt `ErrorType` là tập đóng **năm** giá trị (`docs/adr/0003-result-thuan.md:28`). Tài liệu thi công đã dùng giá trị thứ sáu mà không có quyết định nào sinh ra nó. Số dòng là số dòng ngày 2026-09-14.

| Tài liệu | Có gì |
| --- | --- |
| `docs/quy-uoc/be-cqrs-handler.md:76` | Enum `ErrorType` mang `Unauthorized` |
| `docs/quy-uoc/be-api-controller.md:25` | Bảng ánh xạ có dòng `Unauthorized` → 401 |
| `docs/contracts/auth.md:704` | Mã hạ tầng `CORE.AUTH.NOT_AUTHENTICATED` → 401; FE dọn trạng thái và đưa về màn đăng nhập |
| Phần lớn card trong [`../contracts/`](../contracts/) | Dòng lỗi mang `type` là `Unauthorized` |

Cùng lúc, hai lời hứa về an toàn biên dịch **không đúng** với chính mẫu code đang khai:

- ADR-0003 hứa *"thêm một `ErrorType` mà quên xử lý là lỗi biên dịch"* (`docs/adr/0003-result-thuan.md:69`).
- Tài liệu ánh xạ hứa compiler cảnh báo thiếu nhánh (`docs/quy-uoc/be-api-controller.md:52`).

Bộ ánh xạ lại có nhánh loại bỏ `_` ném exception (`docs/quy-uoc/be-api-controller.md:48`). Một `switch` expression có nhánh `_` là **đầy đủ** trong mắt compiler, nên không có cảnh báo nào để luật T4 (`docs/RULES.md:177`) biến thành lỗi build. Quên một giá trị chỉ lộ ra khi có request thật đi vào nhánh đó.

Và 401 hôm nay không mang envelope: bộ lọc phân quyền trả một kết quả 401 không thân (`docs/quy-uoc/be-api-controller.md:333`), sự kiện cookie chỉ đặt mã trạng thái (`docs/quy-uoc/be-api-controller.md:632`). FE — vốn phân nhánh theo `code` — nhận một 401 không có `code`.

Ràng buộc có thật: FE xử lý 401 bằng **dọn phiên và đưa về đăng nhập** (`docs/contracts/auth.md:704`), còn sai mật khẩu đã chốt là 422 (`docs/contracts/auth.md:216`). Hai ca đó không bao giờ được lẫn vào nhau.

## Quyết định

1. **`ErrorType` là tập đóng sáu giá trị:** `Validation` · `NotFound` · `Conflict` · `Forbidden` · `BusinessRule` · `Unauthorized`. `Unauthorized` nghĩa là **chưa xác thực, hoặc phiên đã hết hạn hay bị thu hồi** — không nghĩa nào khác.
2. **Chỉ hạ tầng xác thực ở `Core.Web` được phát `ErrorType.Unauthorized`** — luật **R9**. Chỗ được phát: sự kiện cookie khi request chưa xác thực, bộ lọc phân quyền, và chính bộ ánh xạ `Result` → HTTP. Không kiểu nào trong `Core.Domain`, `Core.Application`, `Core.Infrastructure` hay project module nào được tham chiếu giá trị đó. Hai sự kiện cookie ghi envelope đầy đủ: `CORE.AUTH.NOT_AUTHENTICATED` cho 401, `CORE.AUTH.FORBIDDEN` cho 403.
3. **Bộ ánh xạ `Result` → HTTP không có nhánh `_`.** Luật T4 biến cảnh báo *switch không đầy đủ* (CS8509) thành lỗi build. Cảnh báo về **giá trị enum không tên** (CS8524) được tắt riêng, vì nó bắn ra ở mọi `switch` trên enum kể cả khi đã phủ đủ mọi giá trị có tên. Một giá trị không tên — ép kiểu từ số — ném exception lúc chạy và đi đường lỗi ngoài dự kiến.
4. **R9 ép bằng ArchTest quét mã nguồn**, không quét assembly: hằng enum được biên dịch thành số nguyên trong IL, nên phép kiểm trên assembly không thấy tham chiếu nào để bắt.

Vì sao chỉ `Core.Web`: theo [`0026-ranh-gioi-identity-va-cookie.md`](0026-ranh-gioi-identity-va-cookie.md), mọi chỗ biết một request chưa xác thực đều nằm trong pipeline HTTP của `Core.Web`. `Core.Infrastructure` kiểm security stamp và trả lời *còn hiệu lực hay không* — nó không dựng lỗi HTTP.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ năm giá trị; 401 dựng tay ở `Core.Web` với chuỗi `type` viết tay

**Được:** ADR-0003 không đổi; handler không thể trả 401 vì không có giá trị nào để trả.

**Vì sao loại:** từ vựng của trường `type` trên dây có **hai nguồn** — enum và một chuỗi viết tay. Luật R2 cấm dựng lỗi từ chuỗi ngoài catalog, luật R4 đòi ánh xạ sang HTTP ở đúng một chỗ; phương án này phá cả hai để tiết kiệm một giá trị enum.

### Phương án B — Thêm `Unauthorized`, không giới hạn ai dùng

**Được:** đơn giản nhất; không có ArchTest mới.

**Vì sao loại:** handler đăng nhập có một lựa chọn trông rất hợp lý cho *"sai mật khẩu"* — `Unauthorized`. FE nhận 401 thì coi là **hết phiên**: dọn trạng thái, đưa về màn đăng nhập, báo các tab khác. Người đang đứng ở màn đăng nhập gõ sai mật khẩu sẽ kích hoạt nhánh hết phiên. Card đã chốt 422 cho ca đó; không có luật thì card là lớp chặn duy nhất, và card không chặn được code.

### Phương án C — Gộp chưa xác thực vào `Forbidden` (403)

**Được:** giữ năm giá trị, không cần luật mới.

**Vì sao loại:** 403 đã mang ba mã với ba cách xử lý khác nhau ở FE (`docs/contracts/auth.md:723`). Thêm ca thứ tư có cách xử lý **ngược hẳn** — dọn phiên — thì một interceptor lỡ phân nhánh theo mã trạng thái sẽ đăng xuất người dùng khi token chống giả mạo hết hạn.

### Phương án D — Giữ nhánh `_` ném exception, dựa vào ArchTest R4

**Được:** không đụng cấu hình cảnh báo; R4 (`EveryErrorType_MapsTo_AValidHttpStatus`) đã bắt được giá trị thiếu ánh xạ khi test chạy.

**Vì sao loại:** R4 bắt ở **lúc chạy test**; bỏ nhánh `_` bắt ở **lúc biên dịch**. Hai lưới đều rẻ và không thay được nhau. Giữ nhánh `_` thì lời hứa của ADR-0003 vẫn sai — và một lời hứa sai trong ADR là thứ người đọc tin mà không kiểm. R4 vẫn giữ.

## Hệ quả

### Tích cực

- Tài liệu thi công và quyết định khớp nhau: giá trị thứ sáu có nguồn gốc.
- *"Quên một `ErrorType` là lỗi build"* trở thành sự thật kiểm được.
- Ca *sai mật khẩu bị hiểu thành hết phiên* không viết được mà vẫn qua cổng.
- Mọi 401 mang envelope và `code`, cùng khuôn với mọi lỗi khác.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Một giá trị trong tập đóng mà gần như toàn bộ code không được dùng** | Người viết handler thấy `Unauthorized` trong gợi ý của IDE và sẽ thử. ArchTest đỏ là lúc đầu tiên họ biết |
| **Tắt CS8524 mất cảnh báo cho mọi `switch` trên enum trong phạm vi tắt** | Tắt ở cấp toàn solution thì module cũng mất. Giá trị ép kiểu từ số chỉ lộ ra lúc chạy |
| **ArchTest quét nguồn có điểm mù** | Ép kiểu từ số, `Enum.Parse`, bí danh `using static` đi lọt phép dò cú pháp. Detector cần test đối chứng theo luật T1, và vẫn không kín |
| **Mọi `ErrorType` mới về sau là việc của Core** | Thêm một giá trị là sửa bộ ánh xạ, card, FE, và có thể một ADR. Đó là chủ đích của tập đóng, nhưng nó làm chậm việc thêm một loại lỗi |

## Điều kiện lật quyết định

1. **Có một ca 401 hợp lệ phát sinh ngoài pipeline HTTP của `Core.Web`** — ví dụ một kênh không đi qua cookie cần báo phiên đã hết. Allowlist của R9 khi đó cần mở, và việc mở là ADR mới trích ADR này.
2. **FE ngừng phân biệt 401 với 403 theo mã trạng thái**, chỉ đọc `code`. Lý do tách `Unauthorized` khỏi `Forbidden` khi đó yếu đi, và phương án C đáng xét lại.

### Dấu hiệu quyết định này bắt đầu sai

- Có đề xuất thêm một handler vào allowlist của R9 "vì trường hợp đặc biệt".
- Một ép kiểu sang `ErrorType` từ số xuất hiện trong code không phải test.
- CS8524 bị tắt ở phạm vi rộng hơn chỗ cần, hoặc CS8509 bị tắt kèm.

## Liên quan

- [`0003-result-thuan.md`](0003-result-thuan.md) — quyết định được bổ sung
- [`0026-ranh-gioi-identity-va-cookie.md`](0026-ranh-gioi-identity-va-cookie.md) — sự kiện cookie nằm ở `Core.Web`
- [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §1 — bảng ánh xạ và bộ ánh xạ
- [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §2.1 — `ErrorType` và `Error`
- [`../contracts/auth.md`](../contracts/auth.md) §11 — mã lỗi hạ tầng dùng chung
- [`../RULES.md`](../RULES.md) — luật R2, R4, R9, T1, T4
