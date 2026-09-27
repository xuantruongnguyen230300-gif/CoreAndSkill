---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0086 — Mã gốc thuộc về một ô được gắn vào ô đó qua `applyFormFailure`, bằng một bảng mã gốc → control do nơi gọi khai; câu đi qua `dichLoi`

> **Trạng thái:** Đã chấp nhận (2026-09-24)

## Bối cảnh

[`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §6.2 chốt cách chọn câu cho hai trường hợp: mã nằm trong `fieldErrors`, và mã ở gốc envelope khi `fieldErrors` rỗng. Còn một trường hợp thứ ba §6.2 không khai: **mã gốc thuộc về đúng một ô**. Đó là các mã trùng lặp, trả 409 `Conflict` và không kèm `fieldErrors`, ví dụ `CORE.TENANT.CODE_DUPLICATE` ([`../contracts/tenants.md`](../contracts/tenants.md) §2) và `CORE.ROLE.NAME_DUPLICATE` ([`../contracts/roles.md`](../contracts/roles.md) §2, §3). Screen spec lại đặt các mã này **dưới ô**, trong bảng *Mã lỗi → chỗ hiện* của từng màn.

Vì không có luật nào, bốn store dưới `src/FE/src/app/platform/` tự viết nhánh gắn tay. Lượt `architect` ngày 2026-09-24 đối chiếu và thấy bốn bản đó đã **lệch nhau**:

- Hai store dịch qua `dichLoi` (`tao-nguoi-dung.store.ts`, `hop-chi-tiet-nguoi-dung.store.ts`).
- Hai store gọi thẳng `translate.instant` (`tao-don-vi.store.ts`, `hop-vai-tro.store.ts`), nên mất đường lùi về câu BE gửi khi thiếu khoá dịch.

Một lỗi đã hiện ra với người dùng: hộp *Sửa* ở `hop-chi-tiet-nguoi-dung.store.ts` gắn `loi.CORE.USER.EMAIL_DUPLICATED` mà không truyền `messageParams`, nên màn hình hiện nguyên chữ `{{Email}}`.

Hàm `applyFormFailure` nằm trong `src/FE/src/app/shared/`, tức thuộc khối `core-paths` ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §10). Đổi hàm này là đổi Core.

## Quyết định

Người dùng chốt ngày 2026-09-24 theo **phương án A**:

1. `applyFormFailure` nhận thêm một tham số **tuỳ chọn**: bảng *mã gốc → tên control*.
2. Mã gốc có trong bảng thì câu của mã đó đi qua `dichLoi` — giữ `messageParams` và giữ đường lùi — rồi được gắn vào control tương ứng, dưới khoá `server`.
3. Bốn store `tao-don-vi`, `tao-nguoi-dung`, `hop-chi-tiet-nguoi-dung`, `hop-vai-tro` chỉ khai bảng, lấy từ bảng *Mã lỗi → chỗ hiện* của screen spec. Chúng không còn nhánh gắn tay nào.

Chữ ký và hành vi cụ thể nằm ở [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §6.2 — chủ của hàm theo [`../OWNERSHIP.md`](../OWNERSHIP.md). ADR này không chép lại chúng.

## Phương án đã cân nhắc và vì sao loại

### Phương án B — Một hàm riêng để gắn mã gốc vào ô

**Được:** thay đổi nhỏ, không đụng chữ ký của `applyFormFailure`.

**Mất:** mỗi store vẫn phải tự rẽ nhánh theo mã để quyết khi nào gọi hàm nào. Khuôn nhánh lặp ở bốn nơi — thứ đã lệch — vẫn còn nguyên, chỉ thân nhánh ngắn đi.

**Vì sao loại:** lỗi thật nằm ở nhánh viết tay, không nằm ở dòng dịch câu.

### Phương án C — Chỉ viết thành luật, không đổi code

**Được:** không đổi dòng code nào.

**Mất:** không cổng nào ép được luật này. Bằng chứng là bốn bản hôm nay đã lệch nhau khi chưa có luật, và một luật chỉ nằm trên giấy không ngăn được bản thứ năm lệch tiếp.

**Vì sao loại:** trường hợp này đã có bằng chứng, không cần đoán — luật chữ không đủ.

### Phương án D — BE trả mã trùng trong `fieldErrors`

**Được:** FE không cần cơ chế mới, vì `applyFieldErrors` đã gắn được sẵn.

**Mất:** phải đổi hợp đồng API của bốn endpoint. Mã trùng phải chuyển từ 409 sang 400 `Validation`, trái bảng phân loại ở [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §3.2: *trùng* là trạng thái của database, không tính được chỉ từ payload.

**Vì sao loại:** đổi phân loại lỗi của BE để giải một việc hiển thị của FE.

## Hệ quả

### Tích cực

- Một luật và một hàm quyết cả câu lẫn chỗ gắn. Hai lỗi đã thấy — thiếu `messageParams` và mất đường lùi — không tái phát được qua đường này.
- Store mới chỉ khai bảng, lấy thẳng từ screen spec. Không có nhánh nào để chép sai.
- Tham số là tuỳ chọn, nên mọi nơi gọi không truyền bảng vẫn giữ nguyên hành vi.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Đổi chữ ký của một hàm dùng chung trong Core** | Thêm một tham số tuỳ chọn vào hàm ở `shared/forms/`. Mọi dự án dựng trên Core nhận đổi này. Vì tham số là tuỳ chọn nên không gãy nơi gọi cũ, nhưng §6.2 phải sửa theo |
| **Bảng một-một** | Mỗi mã gốc chỉ gắn được vào **một** control. Mã nào cần hiện ở nhiều ô cùng lúc thì bảng không diễn đạt được, và lúc đó phải đổi kiểu của bảng — tức đổi chữ ký lần nữa |
| **Bốn store phải sửa cùng lúc** | Lượt thi công chạm cả khu đơn vị, người dùng và vai trò. `frontend-expert` làm song song với ADR này |
| **Chưa có cổng nào cấm nhánh gắn tay** | Một store mới vẫn có thể gọi `setErrors({ server: … })` cho một mã gốc thay vì khai bảng. Hiện chỉ review bắt được việc này — nợ **F40** ở [`../DEBT.md`](../DEBT.md) |

### Rút lui nếu sai

1. Gỡ tham số khỏi `applyFormFailure`.
2. Bốn store quay về nhánh viết tay.
3. Sửa §6.2 và viết một ADR thay thế.

Cả ba bước làm trong một lượt. Không đụng dữ liệu, không đụng hợp đồng API.

### Dấu hiệu quyết định này bắt đầu sai

- Một mã gốc cần hiện ở hai ô cùng lúc.
- Bảng của một store bắt đầu mang logic, ví dụ chọn control theo trạng thái form, thay vì chỉ ánh xạ tĩnh.
- Một nơi gọi cần câu khác câu `dichLoi` trả cho cùng một mã gốc.

## Sáu câu hỏi — trả lời lúc đề xuất

1. **Vấn đề đã đo chưa:** rồi. Bốn bản đã lệch nhau, và lỗi `{{Email}}` đã quan sát được.
2. **Phương án đơn giản hơn:** C chưa đủ, vì đã có bằng chứng lệch. B vẫn giữ nhánh lặp.
3. **Chi phí vận hành:** không thêm thành phần nào.
4. **Ai bảo trì:** `frontend-expert`.
5. **Rút lui:** như mục *Rút lui nếu sai* ở trên.
6. **Core có phải biết nghiệp vụ không:** không. Bảng mã → control do store khai. Hàm chỉ biết đến *một bảng*, không biết mã cụ thể nào. Cả bốn nơi dùng đều nằm trong Core.

## Liên quan

- [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §6.2 — chữ ký và hành vi của `applyFormFailure`
- [`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md) §4 — `applyFieldErrors`
- [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §3.2 — lý do mã trùng giữ 409
