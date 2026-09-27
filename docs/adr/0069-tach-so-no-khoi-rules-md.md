---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0069 — Sổ nợ tách khỏi `RULES.md` thành `DEBT.md`; tiêu đề §10 ở lại làm bản chuyển hướng, và file mới vào mục *Tra cứu* chứ không vào *Bộ luật*

> **Trạng thái:** Đã chấp nhận (2026-09-23) · Bổ sung [`0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md`](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md)

## Bối cảnh

[ADR-0041](0041-cham-tran-corpus-reviewer-thi-rut-file-theo-tieu-chi.md) chốt thứ tự khi bộ luật của một agent chạm trần: **phân loại lại theo tiêu chí D36 trước**, tách file theo D38 là biện pháp thứ hai. Biện pháp thứ nhất đã dùng một lần.

Số đo trước quyết định này, lấy từ mục §23 của [`../../.claude/check-docs.sh`](../../.claude/check-docs.sh): `core-reviewer` **396 KB** trên trần 400, `architect` 180 KB trên trần 250. `RULES.md` một mình **131 KB**, và nó nằm ở mục *Bộ luật — mọi phạm vi* của `core-reviewer`, tức được cộng vào mọi lượt review bất kể phạm vi BE hay FE.

Đo tiếp bên trong file: §10 (danh sách nợ) chiếm **47,9 KB** — hơn một phần ba. Và nó trả lời một câu hỏi **khác** với phần còn lại. Chín mục đầu trả lời *luật này ép bằng cổng nào*; §10 trả lời *luật nào chưa có cổng nào ép*. Theo đúng tiêu chí D36, câu hỏi thứ hai không phải thứ một lượt review phải mang theo ở **mọi** lượt: nó được mở khi một finding chạm một luật không có cổng, tức đúng định nghĩa mục *Tra cứu*.

Một ràng buộc có thật phải xử lý trước khi tách: **64 chỗ trong repo trỏ tới "`RULES.md` §10"**, trong đó phần lớn nằm ở `docs/adr/` và `docs/audit/` — hai khu là bản ghi lịch sử, không được sửa nội dung.

## Quyết định

Kiến trúc sư chốt, người dùng đã duyệt:

1. **Toàn bộ §10 chuyển sang [`../DEBT.md`](../DEBT.md)**, giữ nguyên từng hàng và từng mã nợ. Dãy mã (`D…` `C…` `B…` `E…` `S…` `T…` `F…` `M…`) vẫn là **một dãy chung** với [`../RULES.md`](../RULES.md): tách file không tách không gian mã, và không mã nào được dùng lại.
2. **Tiêu đề `## 10.` ở lại `RULES.md` làm bản chuyển hướng**, không giữ nội dung. Nó là thứ giữ cho mọi trích dẫn *"`RULES.md` §10"* trong `docs/adr/` và `docs/audit/` còn tới được đích mà không ai phải sửa một dòng nào trong hai khu bất biến đó.
3. **Mọi trích dẫn ở tài liệu SỐNG trỏ thẳng sang `DEBT.md`.** Bản chuyển hướng dành cho khu lịch sử, không phải chỗ trú của tài liệu đang dùng.
4. **`DEBT.md` vào mục *Tra cứu* của `architect`, `core-reviewer`, `test-engineer`** — không vào *Bộ luật* của agent nào. Vào *Bộ luật* thì corpus không giảm và cả phép tách trở thành vô nghĩa.
5. **Hai lệnh đếm của sổ nợ đổi đầu vào sang `docs/DEBT.md`**, giữ nguyên biểu thức hàng 5 cột. Đã chạy cả hai trước và sau khi tách: **22** và **26** ở cả hai lần — số khớp là điều kiện của phép tách, không phải phần thưởng.
6. **Danh sách loại trừ của lệnh tầng B ở [ADR-0043](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md) nhận thêm `^docs/DEBT.md$`.** Hàng **D42** nhắc nguyên văn chuỗi nhãn `📐 ĐÍCH ĐẾN` để phát biểu luật; hàng đó theo sổ nợ sang file mới, nên lý do loại trừ cũng theo sang. ADR-0043 bất biến nên dòng loại trừ khai ở đây — người chạy lệnh đó đọc cả hai.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Không tách; rút bớt file khác khỏi *Bộ luật* của `core-reviewer`

**Được:** không file mới, không trích dẫn nào phải sửa, không cổng nào phải đụng. Đây là biện pháp thứ nhất của [ADR-0041](0041-cham-tran-corpus-reviewer-thi-rut-file-theo-tieu-chi.md), và nó phải được xét trước.

**Mất:** đã xét. Các file còn lại trong *Bộ luật* của `core-reviewer` đều trả lời câu hỏi *"code có tuân không"* ở mọi lượt — đó chính là tiêu chí D36 để một file ở lại. Rút thêm file theo tiêu chí đó là **nới tiêu chí cho vừa con số**, đúng thứ ADR-0041 đã loại khi nó từ chối nới trần.

**Vì sao loại:** biện pháp thứ nhất đã hết chỗ dùng đúng cách; §10 thì trượt tiêu chí D36 một cách rõ ràng, không phải do ép cho vừa.

### Phương án B — Nới trần `CORPUS_MAX_REVIEWER` từ 400 KB lên 450 KB

**Được:** một dòng sửa, không đụng gì khác.

**Vì sao loại:** [ADR-0041](0041-cham-tran-corpus-reviewer-thi-rut-file-theo-tieu-chi.md) đã loại đúng phương án này, và lý do chưa đổi: trần ấy là số đo của *một lượt review còn sống tới lúc kết luận*, không phải số đo của *tài liệu đang có bao nhiêu*. Nới nó là sửa nhiệt kế.

### Phương án C — Tách §10 nhưng xoá hẳn tiêu đề `## 10.` khỏi `RULES.md`

**Được:** sạch hơn — không có tiêu đề rỗng, không có bước nhảy hai chặng.

**Mất:** 64 trích dẫn *"`RULES.md` §10"*, phần lớn ở `docs/adr/` và `docs/audit/`, thành trích dẫn trỏ vào một mục không tồn tại. Cổng **không bắt được** loại hỏng này: §3 kiểm link resolve, mà link ở đó là `RULES.md` — vẫn tồn tại; chuỗi "§10" là văn xuôi. Đúng loại lỗi 1 mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8 khai là ngoài tầm cổng.

**Vì sao loại:** cách duy nhất để tránh nó là sửa nội dung `docs/adr/` — mà đó là thứ [`README.md`](README.md) §5 cấm, vì nội dung ADR là bản ghi *lý do lúc đó*.

### Phương án D — Tách §10 thành nhiều file nhỏ theo nhóm mã (`D*`, `B*`, `F*`…)

**Được:** mỗi agent chỉ mở phần của mình; file nhỏ hơn nữa.

**Mất:** một mã nợ phải tra ở *file nào* trước khi tra *hàng nào*, và hai lệnh đếm thành sáu. Quan trọng hơn: sổ nợ có giá trị vì nó trả lời được *toàn bộ chỗ hở của repo là bao nhiêu* trong một lần đọc. Cắt theo nhóm là đánh mất đúng tính chất đó.

**Vì sao loại:** trả một khoản corpus nhỏ thêm để mua lấy một sổ nợ không đọc được trong một lần.

## Hệ quả

### Tích cực

- `core-reviewer` **396 → 348 KB**, `architect` **180 → 133 KB** (mục §23 của cổng, đo sau khi tách). Trần 400 KB không phải nới.
- Sổ nợ thành một file có tên gọi đúng việc của nó; thêm một hàng nợ không còn làm phình bộ luật của mọi lượt review.
- Bảng luật và sổ nợ tách nhau theo đúng câu hỏi chúng trả lời, nên tiêu chí D36 áp được cho từng file thay vì cho một file làm hai việc.

### Tiêu cực

- **Một dãy mã, hai file.** Cấp mã mới nay phải tra **cả hai** file mới biết số nào còn trống. Không cổng nào canh việc này; một mã trùng sẽ lọt, và nó lọt im lặng vì hai file không bao giờ được đọc cùng lúc. Đây là cái giá thật của phép tách, và nó không biến mất theo thời gian.
- **Bản chuyển hướng là nợ vĩnh viễn.** Tiêu đề `## 10.` rỗng ở lại `RULES.md` chừng nào `docs/adr/` còn trích dẫn nó — tức là mãi mãi. Nó sẽ trông như rác chưa dọn với mọi người đọc mới, và có lúc sẽ có người dọn nó đi mà không biết vì sao nó ở đó. Câu giải thích ngay dưới tiêu đề là lớp chặn duy nhất, và nó chỉ là một câu.
- **Trích dẫn trong khu lịch sử nay đi hai chặng.** Người đọc một ADR cũ bấm vào `RULES.md` §10, gặp bản chuyển hướng, rồi mới tới nơi. Đúng, nhưng chậm hơn và trông như một lỗi.
- **Cổng §14 chỉ đọc `RULES.md`.** Hôm nay không lệch gì — hàng của sổ nợ không mang cột *Mức*, nên §14 chưa từng xét chúng. Nhưng nếu sau này có người dán một hàng `| … | MUST | … |` vào `DEBT.md`, §14 sẽ **không** thấy. Phép cắt `awk '/^## 10\./ { exit }'` ở đầu `RULES.md` giữ lại chính vì chiều ngược lại của cùng lỗ này.

### Rút lui nếu sai

Nối `DEBT.md` trở lại vào chỗ tiêu đề `## 10.`, trả hai lệnh đếm về đầu vào cũ, gỡ ba hàng *Tra cứu*, trả 23 tệp sống về trích dẫn cũ (một lệnh thay chuỗi, đảo chiều đúng lệnh đã dùng). Nửa buổi. Không dữ liệu nào đổi, và khu lịch sử không phải chạm ở cả hai chiều — đó là lợi ích thứ hai của việc giữ bản chuyển hướng.

## Liên quan

- [`0041-cham-tran-corpus-reviewer-thi-rut-file-theo-tieu-chi.md`](0041-cham-tran-corpus-reviewer-thi-rut-file-theo-tieu-chi.md) — thứ tự hai biện pháp khi chạm trần corpus; đây là biện pháp thứ hai của nó.
- [`0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md`](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md) — lệnh tầng B và danh sách loại trừ của nó.
- [`0045-cho-src-sua-dinh-nghia-khong-them-ky-hieu-thu-tu.md`](0045-cho-src-sua-dinh-nghia-khong-them-ky-hieu-thu-tu.md) — ký hiệu `⏳ chờ src/` và hai lệnh đếm neo vào nó.
- [`../DEBT.md`](../DEBT.md) · [`../RULES.md`](../RULES.md) · luật **D36**, **D38**.
