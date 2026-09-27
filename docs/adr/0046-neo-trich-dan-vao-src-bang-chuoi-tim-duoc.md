---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0046 — Trích dẫn từ `docs/` vào một tệp dưới `src/` neo bằng **một chuỗi tìm được trong tệp**, không bằng số dòng

> **Trạng thái:** Đã chấp nhận (2026-09-20)

## Bối cảnh

[`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 buộc mọi tuyên bố `✅ CÓ THẬT` kèm *ngày đối chiếu + `file:dòng`*, và §9 giải thích vì sao: *"neo được thì máy kiểm được"*. Luật D7 ở [`../RULES.md`](../RULES.md) là phần ép của câu đó.

Ngày 2026-09-20, lượt review BE đo được một ca cho thấy vế "máy kiểm được" không đúng như câu trên ngụ ý.

| Đo gì | Kết quả |
| --- | --- |
| Neo trong [`0044-database-scripts-core-vao-khoi-core-paths.md`](0044-database-scripts-core-vao-khoi-core-paths.md), mục Bối cảnh | Trỏ vào `PermissionScriptParityTests.cs` kèm một khoảng ba dòng, để chỉ chỗ dựng đường dẫn thư mục script |
| Chỗ đó hôm nay | Phương thức tên `ScriptDirectory()`, đã dời xuống cuối tệp. Khoảng dòng được trích không còn dựng đường dẫn nào — nó là chú thích trong thân một test khác |
| Khoảng cách thời gian | ADR ghi lúc 19:51; tệp test được ghi lại lần đầu chưa đầy nửa giờ sau, và **còn dời tiếp trong cùng buổi**. Trong chính lượt viết ADR này, `ScriptDirectory()` dời thêm gần hai trăm dòng nữa — số dòng mà lượt review đề nghị dùng thay đã mục ruỗng trước khi bản sửa kịp viết xong |
| Cổng có bắt không | Không. Mục §7 của [`../../.claude/check-docs.sh`](../../.claude/check-docs.sh) so số dòng với `wc -l` của tệp đích rồi dừng: nó kiểm **số dòng nằm trong tệp**, không bao giờ kiểm **dòng đó nói gì** |

Hai điều kiện làm ca này thành cơ chế chứ không phải tai nạn. Thứ nhất, `src/` đang ở giai đoạn thi công mạnh nhất của nó — tệp bị chèn, tách, sắp xếp lại hằng ngày, nên xác suất một số dòng còn đúng sau một tuần là thấp. Thứ hai, kiểu hỏng này **im lặng**: neo mục ruỗng không trỏ vào chỗ trống mà trỏ sang **một đoạn code khác cũng có thật**, nên người đọc nhận một câu sai kèm bằng chứng trông hợp lệ, và cổng vẫn xanh.

Một ràng buộc nữa quyết định hình dạng của luật: **ADR đã chấp nhận thì không sửa nội dung** (§5 của [`README.md`](README.md)). Neo trong 0044 vì vậy ở lại vĩnh viễn, kể cả khi đã sai.

## Quyết định

`architect` chốt ba điều:

1. **Mọi trích dẫn mới từ `docs/` vào một tệp dưới `src/` neo bằng đường dẫn tệp + một chuỗi tìm được trong tệp** — tên thành viên (`ScriptDirectory()`, `LoginCommandHandler`), tên khoá/ràng buộc trong tệp `.sql`, hoặc một đoạn trích đủ duy nhất. Không dùng số dòng làm neo.
2. **Số dòng vẫn dùng được cho trích dẫn `docs/` → `docs/`.** Luật D7 và mục §7 của cổng giữ nguyên phạm vi đó; ADR này không chạm vào chúng.
3. **Cổng ép luật này, khi được viết, miễn trừ các tệp trong `docs/adr/` đã mang trạng thái chấp nhận** — cùng cơ chế miễn trừ mà §7 đã dành cho tài liệu lịch sử. Không có miễn trừ đó thì cổng sẽ đòi sửa một ADR bất biến, tức là bắt hai luật của repo đánh nhau.

Luật này **chưa có cổng**. Cho tới khi có, nó nằm ở danh sách nợ của [`../RULES.md`](../RULES.md) §10 — viết bởi chủ của tệp đó, không bởi ADR này.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Chỉ sửa ca đã phát hiện, không đặt luật

Đây là phương án đơn giản hơn, và là phương án lượt review đề nghị trước tiên.

**Được:** không đụng tới một chỉ dẫn toàn repo, không sinh nợ cổng mới, chi phí bằng một lần sửa.

**Vì sao loại:** hai lý do đo được. Thứ nhất, ca đã phát hiện **không sửa được** — nó nằm trong một ADR đã chấp nhận, mà ADR thì bất biến; "chỉ sửa ca này" trên thực tế là "không sửa gì cả". Thứ hai, cái hỏng không nằm ở người viết mà ở dạng neo: cùng dạng đó, cùng tốc độ thay đổi của `src/`, sẽ cho ra neo mục ruỗng tiếp theo mà không ai biết. Số trích dẫn `src/…:dòng` hiện có trong `docs/` đếm được bằng một lệnh `grep`, và hôm nay nó nhỏ — thời điểm rẻ nhất để đổi dạng neo là bây giờ, không phải sau khi `src/` lớn lên.

### Phương án B — Cấm số dòng ở **mọi** trích dẫn trong `docs/`, kể cả `docs/` → `docs/`

**Được:** một luật duy nhất, không có ngoại lệ phải nhớ, cổng đơn giản hơn.

**Vì sao loại:** phạm vi này rộng hơn bằng chứng. Chưa có phép đo nào cho thấy neo `docs/` → `docs/` mục ruỗng ở mức đáng kể, và khu `docs/` đã có hai lớp khác giữ chỗ: mốc đăng ký trong [`../OWNERSHIP.md`](../OWNERSHIP.md) và mục §7 của cổng. Mở rộng luật tới chỗ chưa có vấn đề là trả chi phí chắc chắn cho một lợi ích chưa đo được.

### Phương án C — Giữ số dòng, thêm cổng đọc dòng đó và so với một chuỗi khai kèm

**Được:** giữ được độ chính xác tuyệt đối của số dòng, và biến kiểu hỏng im lặng thành cổng đỏ.

**Vì sao loại:** nó đòi người viết khai **hai** thứ cho một neo (số dòng và chuỗi mong đợi), trong khi chuỗi một mình đã đủ để định vị. Và nó không làm neo hết gãy — mỗi lần tệp đích dịch dòng, cổng đỏ ở một chỗ mà **nội dung tài liệu vẫn đúng**, nên đỏ đó là việc phải dọn chứ không phải lỗi phải sửa. Một cổng hay đỏ vì lý do không phải lỗi là cổng sẽ bị người ta học cách bỏ qua.

## Hệ quả

### Tích cực

- Neo sống sót qua việc chèn, tách, sắp xếp lại tệp — đúng những thao tác đang xảy ra hằng ngày ở `src/`.
- Neo trở nên **kiểm được bằng nội dung**, không chỉ kiểm được bằng khoảng: một cổng tương lai chỉ cần `grep` chuỗi trong tệp đích, mạnh hơn hẳn phép so `wc -l` của §7.
- Khi neo chết, nó chết **nhìn thấy được** (tìm không ra) thay vì trỏ nhầm sang một đoạn code khác cũng có thật.

### Tiêu cực

- **Mất độ chính xác ở tệp có tên trùng.** Overload, `partial class`, hai phương thức cùng tên ở hai lớp trong một tệp — chuỗi neo trỏ vào nhiều chỗ và người đọc phải tự phân biệt. Số dòng không bao giờ mơ hồ vào thời điểm viết ra nó.
- **Luật này hôm nay không được ép bằng gì.** Từ lúc chấp nhận tới lúc có cổng, nó chỉ được tuân khi có người nhớ ra — đúng loại luật mà [`README.md`](README.md) §8 cảnh báo là sẽ trôi. Cái giá này có thật và không giảm được bằng cách viết thêm chữ.
- **Repo mang hai câu chỉ dẫn khác nhau trong một khoảng thời gian.** `.claude/CLAUDE.md` §4 và §9 vẫn nói `file:dòng`, luật D7 vẫn mô tả dạng cũ. Hai tệp đó nằm ngoài phạm vi ghi của vai trò ra quyết định này; cho tới khi chủ của chúng sửa, người đọc gặp hai chỉ dẫn và phải biết cái nào mới hơn. Đây là cái giá cố hữu của việc tách người quyết khỏi người thi công, và nó chỉ chấp nhận được vì đã viết ra.
- **Neo cũ trong các ADR đã chấp nhận ở lại sai vĩnh viễn**, gồm cả ca đã đo ở 0044. Miễn trừ ở mục Quyết định số 3 là quyết định **giữ** những chỗ sai đó, không phải quyết định sửa chúng.

## Liên quan

- [`0044-database-scripts-core-vao-khoi-core-paths.md`](0044-database-scripts-core-vao-khoi-core-paths.md) — ADR chứa ca đo. Nội dung của nó **không** bị ADR này sửa; nó vẫn đúng với bối cảnh ngày viết.
- Luật D7 ở [`../RULES.md`](../RULES.md) — phần ép của dạng neo cũ, giữ nguyên cho `docs/` → `docs/`.
- [`0041-cham-tran-corpus-reviewer-thi-rut-file-theo-tieu-chi.md`](0041-cham-tran-corpus-reviewer-thi-rut-file-theo-tieu-chi.md) — cùng khuôn: sửa tiêu chí thay vì nới ngưỡng của cổng.
