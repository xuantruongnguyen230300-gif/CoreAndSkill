---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0066 — Dòng nhật ký của lệnh xuất nghĩa là "đã cho phép kéo dữ liệu ra", ghi trong giao dịch của lệnh; `rowCount` là số dòng khớp bộ lọc lúc đếm

> **Trạng thái:** Đã chấp nhận (2026-09-23)

## Bối cảnh

[`../contracts/exports.md`](../contracts/exports.md) §1 là **card khuôn**: mọi endpoint xuất, kể cả của module hạ nguồn, phải theo nó. Card khai **nội dung** dòng nhật ký — *ai xuất, bộ lọc nào, bao nhiêu dòng* — nhưng **không khai nghĩa** của dòng đó. Hai câu hỏi card không trả lời: dòng có mặt nghĩa là người dùng **đã nhận** tệp, hay chỉ là hệ thống **đã cho phép**? Và `rowCount` đếm dòng nào?

[ADR-0062](0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md) dựng cả một ngoại lệ antiforgery cho `GET` **vì** dòng nhật ký này: không có token thì dòng nhật ký mang tên người bấm là dòng giả. Tức nhật ký xuất tồn tại để trả lời **ai được phép kéo dữ liệu ra**. Nghĩa đó đã ngầm được chọn, nhưng chưa file nào viết ra — nên mỗi module dựng endpoint xuất sẽ tự chọn lại, và bản chọn khác sẽ không làm gì đỏ.

Kiến trúc sư đối chiếu code ngày 2026-09-23:

| Đo gì | Kết quả |
| --- | --- |
| Loại lệnh | `src/BE/Core/CoreAndSkill.Core.Application/Users/ExportUsersCommand.cs` — chuỗi `: ICommand<ExportFile>`, tức `TransactionBehavior` bọc |
| Điều kiện commit | `src/BE/Core/CoreAndSkill.Core.Application/Common/Behaviors/TransactionBehavior.cs` — chuỗi `response.IsSuccess`; commit khi handler trả thành công |
| Chỗ ghi nhật ký | `src/BE/Core/CoreAndSkill.Core.Application/Users/ExportUsersCommandHandler.cs` — chuỗi `auditTrail.Record(new AuditEntry(`, chạy **trong** handler |
| Chỗ ghi tệp | Cùng tệp, chuỗi `return new ExportFile(fileName,` — thân là một delegate, chạy ở `src/BE/Core/CoreAndSkill.Core.Web/Http/ExportFileResult.cs` (chuỗi `await file.WriteToAsync(response.Body`), tức **sau khi** action trả về và **sau khi** giao dịch đã commit |
| `rowCount` lấy ở đâu | Cùng handler, chuỗi `var rowCount = await users.CountAsync(criteria, ct);` |
| Đường ghi có bị chặn riêng không | Có — cùng handler, chuỗi `users.StreamAsync(criteria, maxRows, token)`; trần `maxRows` chặn **lần nữa** ở lúc ghi |

Ba hệ quả đã có thật, và không cái nào được viết ra:

1. Mất kết nối giữa chừng vẫn **để lại** dòng nhật ký; xuất lại tạo dòng **thứ hai**.
2. `rowCount` là số dòng khớp bộ lọc **lúc đếm**, không phải số dòng thực ghi vào tệp — dữ liệu đổi giữa lúc đếm và lúc ghi thì hai số khác nhau, theo cả hai chiều.
3. Đường ghi bị chặn ở `maxRows` nên số dòng thực giao không bao giờ vượt trần, kể cả khi dữ liệu tăng sau lúc đếm — nhưng dòng nhật ký không ghi lại việc đã chạm trần.

## Quyết định

Kiến trúc sư chốt bốn câu:

1. **Dòng nhật ký của một lệnh xuất nghĩa là *hệ thống đã cho phép người này kéo tập dữ liệu khớp bộ lọc này ra*** — không phải *người này đã nhận đủ tệp*. Nó ghi lúc **cho phép**, trong giao dịch của lệnh, trước khi byte đầu tiên ra luồng.
2. **`rowCount` là số dòng khớp bộ lọc tại thời điểm đếm**, không phải số dòng thực ghi.
3. **Một lần xuất trượt giữa chừng rồi xuất lại để lại hai dòng, và đó là hành vi đúng** — không phải lỗi cần gộp.
4. Ba câu trên khai ở [`../contracts/exports.md`](../contracts/exports.md) §1. **Endpoint xuất của module không được ghi dòng nhật ký sau khi giao xong**: một hệ có hai nghĩa cho cùng một hành động nhật ký là một hệ không rà soát được.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Ghi sau khi luồng ghi xong, kèm số dòng thực giao

**Được:** con số trong nhật ký bằng con số trong tệp; lần xuất hỏng không để lại dòng nào, nên đếm dòng là đếm tệp thật sự tới tay người dùng.

**Mất:** luồng chạy **sau** khi action trả về và sau commit, nên đường ghi phải ra ngoài giao dịch của lệnh — tức một đường ghi nhật ký **thứ hai**, không qua `IAuditTrail`/`IUnitOfWork`. Nặng hơn: kết nối đứt thì **không còn dòng nào**, và lần kéo dữ liệu đó biến mất khỏi nhật ký.

**Vì sao loại:** nó đổi **tính đầy đủ của nhật ký** — thứ không lấy lại được — lấy **độ chính xác của một con số**. Người kéo nửa tệp rồi ngắt trở thành người không để lại vết, đúng lỗ mà ADR-0062 dựng ngoại lệ antiforgery để bịt.

### Phương án B — Ghi hai dòng: một lúc cho phép, một lúc giao xong

**Được:** trả lời được cả hai câu hỏi, và vẫn giữ được vết cho lần hỏng.

**Mất:** mỗi lần xuất thành công sinh hai dòng, và dòng thứ hai vẫn phải ghi ngoài giao dịch — cùng đường ghi thứ hai của phương án A. Người đọc nhật ký phải biết ghép cặp, và một cặp thiếu vế hai có **hai** nghĩa không phân biệt được: đứt kết nối, hay lỗi của chính đường ghi nhật ký.

**Vì sao loại:** gấp đôi khối lượng nhật ký của đường dữ liệu rời hệ thống để trả lời một câu hỏi **chưa ai hỏi**. Đây là phương án để dành, không phải phương án bị bác — xem *Rút lui*.

### Phương án C — Bỏ `rowCount` khỏi dòng nhật ký

**Được:** hết hẳn khả năng đọc nhầm một con số xấp xỉ thành con số chính xác.

**Mất:** mất thứ duy nhất cho biết **quy mô** của lần kéo dữ liệu. *Xuất 5 dòng* và *xuất 50.000 dòng* là hai mức rủi ro khác nhau, và quy mô là câu hỏi đầu tiên của một lượt rà soát.

**Vì sao loại:** một con số xấp xỉ **có tên đúng** vẫn hơn không có con số. Cái phải sửa là tài liệu khai nghĩa, không phải con số.

## Hệ quả

### Tích cực

- Một lần kéo dữ liệu **không bao giờ** không để lại vết, kể cả khi hỏng giữa chừng — thứ ADR-0062 bảo vệ vẫn đứng.
- Lệnh xuất ghi nhật ký **y hệt mọi lệnh ghi khác**: một `IAuditTrail.Record` trong giao dịch. Core không có đường ghi nhật ký thứ hai, nên cũng không có đường thứ hai để quên lọc trường nhạy cảm.
- Module dựng endpoint xuất theo khuôn không phải quyết lại nghĩa, và không sinh ra nghĩa thứ hai trong cùng một hệ.

### Tiêu cực

- **`rowCount` trong nhật ký có thể khác số dòng trong tệp người dùng nhận, theo cả hai chiều, và không gì trong chính dòng nhật ký nói ra điều đó.** Người rà soát phải biết luật này từ tài liệu; tên trường không tự khai. Đây là cái giá thật, không gỡ được bằng nỗ lực — chỉ gỡ được bằng phương án A hoặc B.
- **Nhật ký đếm nhiều hơn số tệp thật sự tới tay người dùng.** Một người mạng kém bấm xuất năm lần để lại năm dòng; một lượt rà soát đếm *số lần dữ liệu rời hệ thống* theo số dòng sẽ đếm thừa.
- **Lần xuất chạm trần `maxRows` trông giống hệt lần xuất không chạm trần.** Dòng nhật ký ghi số lúc đếm, còn việc đường ghi bị cắt ở trần thì không chỗ nào ghi lại.

### Rút lui nếu sai

Dấu hiệu sai: có người cần trả lời *"đã giao bao nhiêu dòng"* hoặc *"lần xuất đó có tới nơi không"* và nhật ký không trả lời được.

Quy trình khi đó là **phương án B, không phải phương án A**:

1. **Giữ nguyên nghĩa của dòng đang có.** Các dòng đã ghi mang nghĩa cũ và không gì phân biệt chúng với dòng nghĩa mới — đổi nghĩa tại chỗ là thao tác **không đảo được**.
2. Thêm một **hành động nhật ký mới** (ví dụ `core.user.export.delivered`) ghi ở đường sau luồng, mang số dòng thực giao.
3. Card [`../contracts/exports.md`](../contracts/exports.md) §1 khai cả hai dòng và cách ghép cặp.

Chi phí: một đường ghi nhật ký ngoài giao dịch, một mục danh mục hành động, một lượt sửa card. Ước lượng một ngày; **không** phải chuyển đổi dữ liệu cũ, vì nghĩa dòng cũ không đổi.

## Liên quan

- [`0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md`](0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md) — vì sao dòng nhật ký này đáng được bảo vệ bằng một ngoại lệ antiforgery.
- [`0064-nhanh-tra-tep-di-qua-apicontrollerbase.md`](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md) — nhánh trả tệp, tức đường chạy **sau** khi dòng nhật ký đã commit.
- [`../contracts/exports.md`](../contracts/exports.md) §1 · [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §5.5 · [`../RULES.md`](../RULES.md) §10 dòng **B17**.
