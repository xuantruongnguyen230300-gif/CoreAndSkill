---
kind: luat
scope: core
verified: chua-doi-chieu
---

# RULES-DOCS — luật tài liệu và ranh giới `.claude` ↔ `docs`

> Mục lục, ký hiệu cột *Trạng thái*, ba mức MUST / MUST NOT / SHOULD và lệnh tự rà D39: [`RULES.md`](RULES.md). Số mục §N giữ nguyên như ở `RULES.md`.

## 1. Luật tài liệu — hiệu lực NGAY

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| D1 | Mọi file `docs/` và `spec/` khai đủ `kind` / `scope` / `verified` | MUST | `check-docs.sh` §12 | ✅ |
| D2 | `.claude/**/*.md` không chứa code block ngôn ngữ lập trình | MUST NOT | `check-docs.sh` §2 | ✅ |
| D3 | Mọi link markdown nội bộ resolve được | MUST | `check-docs.sh` §3 | ✅ |
| D4 | Mọi đường dẫn `docs/…`, `.claude/…`, `spec/…` được trích dẫn đều tồn tại | MUST | `check-docs.sh` §4 | ✅ |
| D5 | Trích dẫn không neo vào file bị `.gitignore` loại khỏi repo | MUST NOT | `check-docs.sh` §5 | ✅ |
| D6 | Dòng chứa `ĐÃ CÓ` / `✅ Xong` / `FIXED` / `Đã bật` phải kèm ngày đối chiếu | MUST | `check-docs.sh` §6 | ✅ |
| D7 | Trích dẫn dạng `file:dòng` phải nằm trong file — trừ trích dẫn bên trong ADR có số: ADR ghi tại thời điểm viết, D45 cấm sửa, nên số dòng của nó là lịch sử (người dùng chốt 2026-09-27) | MUST | `check-docs.sh` §7 — output in số trích dẫn đã xét; đọc số đó thay vì chép vào đây | ✅ |
| D8 | Bảng định tuyến trỏ đúng chủ đề — định danh trong backtick phải có trong file đích | MUST | `check-docs.sh` §8 | ✅ |
| D9 | Tên file `.md` viết trong code span ở `.claude/` phải resolve được | MUST | `check-docs.sh` §9 | ✅ |
| D10 | Trích dẫn không trỏ vào file mang `kind: lich-su` | MUST NOT | `check-docs.sh` §13 | ✅ |
| D11 | Mọi luật ở `RULES.md` và `RULES-*.md` khai cột "Ép bằng gì" | MUST | `check-docs.sh` §14 | ✅ |
| D12 | Bảng cấm git trong `CLAUDE.md` §1 khớp `permissions.deny` của `settings.json` | MUST | `check-docs.sh` §1 | ✅ |
| D13 | **Một nội dung có đúng MỘT nguồn đối chiếu.** Một định nghĩa (catalog, bảng ánh xạ, chữ ký kiểu) chỉ được nằm ở file chủ của nó; file khác chỉ trỏ đường | MUST | `check-docs.sh` §15 đọc bảng chủ quyền ở [`OWNERSHIP.md`](OWNERSHIP.md) §3 | ✅ cho định nghĩa đã đăng ký |
| D17 | Mọi dòng trong bảng chủ quyền phải có chuỗi định danh xuất hiện đúng ở file chủ — không nhiều hơn, không ít hơn | MUST | `check-docs.sh` §15 | ✅ |
| D16 | Không tuyên bố `CÓ THẬT` khi repo chưa có `src/` để đối chiếu | MUST NOT | `check-docs.sh` §11 — dùng **canary** vì 0 dòng khớp là trạng thái hợp lệ | ✅⌀ |
| D22 | **Định nghĩa gắn mốc `— định nghĩa gốc` phải có dòng trong sổ chủ quyền.** Chiều ngược của D13: D13/D17 đi từ sổ ra tài liệu, D22 đi từ tài liệu vào sổ | MUST | `check-docs.sh` §16 | ✅ |
| D23 | **Bảng định tuyến tự khai bằng TIÊU ĐỀ CỘT CUỐI.** Chỉ bảng có tiêu đề cột cuối thuộc tập đã chốt mới được §8 đối chiếu; bảng văn xuôi có kèm liên kết thì không | MUST | `check-docs.sh` §8 | ✅ |
| D24 | **File nội dung không kể lại bản trước của chính nó.** Bản cũ nằm trong lịch sử git; bài học có cơ chế hỏng vào [`audit/`](audit/). Miễn trừ: `audit/`, `adr/` (bối cảnh của một quyết định), file `kind: lich-su` | MUST NOT | `check-docs.sh` §17 — chỉ bắt các cụm đã biết; cách diễn đạt khác dựa vào [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §3 và review | ✅ một phần |
| D45 | **Nội dung một ADR đã mang trạng thái chấp nhận là BẤT BIẾN.** Chỉ dòng `> **Trạng thái:**` được sửa — thủ tục ở [`adr/README.md`](adr/README.md) §5 và §5.1. **Mọi thay đổi khác đi bằng một ADR MỚI**, kể cả khi một mục *Hệ quả* đã lỗi thời, khi một đường dẫn `src/` trong §Bối cảnh đã dời đi (miễn trừ đã khai ở **D41** và **D43**), và **khi một phép đo khai trong §Bối cảnh hoá ra đã SÓT hoặc SAI ngay từ lúc viết** — tiền lệ [ADR-0071](adr/0071-m6-nhan-allowlist-va-phu-duong-sqlquery.md), xử lý ở [ADR-0074](adr/0074-dbset-fromsql-o-lai-trong-tam-m6.md); phép đo đúng sống ở ADR mới. Thêm một mục mới vào ADR cũ cũng là sửa nội dung. Vì sao: [`adr/README.md`](adr/README.md) §5. Câu hỏi *"hôm nay có cổng chưa"* không tra ở `adr/` mà ở các bảng `RULES-*.md` — §8 cùng tệp | MUST NOT | Review — xem [nợ](DEBT.md) | 🕳️ nợ |
| D28 | **Từ vựng nghiệp vụ không được nằm ở VỊ TRÍ ĐỊNH DANH trong [`Design/Components/`](Design/Components/)** — trong code span hoặc code block, tức làm tên biến thể / tên prop / tên trường. Ví dụ nghiệp vụ trong văn xuôi và trong ô bảng thì **được phép**, kể cả ở mục `Biến thể`/`Trạng thái`/`API dự kiến` | MUST NOT | `check-docs.sh` §18 — quét cả dạng có dấu lẫn dạng ASCII (`kyKeToan`, `hachToan`…); có canary + bộ đếm đầu vào | ✅ một phần — **lát cắt** của D27, không phải cả D27 |
| D29 | **Một kiểu công khai chỉ được khai ở MỘT chỗ trong toàn `docs/`.** Áp cho `export interface` / `type` / `enum` / `const`; file khác chỉ được trỏ đường | MUST | `check-docs.sh` §19 — **cố ý bỏ `export class`**: mọi class trong `docs/` là ví dụ minh hoạ | ✅ |
| D30 | **Giá trị của một token chỉ sống ở [`Design/DESIGN.md`](Design/DESIGN.md).** Cấm gán giá trị literal cho `--token` ở nơi khác; bí danh `var(--x)` và khung minh hoạ không mang giá trị thì được phép | MUST NOT | `check-docs.sh` §20 — canary kép (pattern khớp được dòng gán thật **và** không khớp bí danh). Miễn trừ `adr/`: một ADR ghi lại quyết định tại thời điểm đó, kể cả giá trị | ✅ |
| D32 | **Mỗi hướng đã khoá (`❌ loại, không hoãn`) mang một mã `K##`.** Mã tuần tự, không tái sử dụng, không đánh số lại. Lật một hướng thì viết ADR **trích mã đó**, không xoá dòng. Quy ước đầy đủ: [`wiki-core/README.md`](wiki-core/README.md) §9.1 | MUST | `check-docs.sh` §21 — bắt thiếu mã, trùng mã, và **lỗ trống trong dãy số**: một mã biến mất mà không ADR nào trích dẫn nghĩa là một hướng khoá vừa bị lật trong im lặng | ✅ |
| D33 | **Mỗi luồng trong [`luong/`](luong/) khai đủ sáu mục, trong đó có mục *Quan hệ với đơn vị* trả lời một trong ba: *thuộc đơn vị* · *dùng chung toàn hệ* · *không áp dụng kèm lý do*.** Mục lục khu không được lệch khỏi thư mục | MUST | `check-docs.sh` §22 — kiểm **có mặt**, không kiểm đúng sai | ✅ |
| D36 | **Bộ luật của mỗi agent có ngưỡng cỡ.** Bảng định tuyến trong `.claude/agents/*.md` tách hai phần theo tiêu đề mục: mục *Bộ luật* (agent mở theo việc đang làm — cộng dồn) và mục *Tra cứu* (mở đúng một file khi chủ đề chạm tới — không cộng). Tổng cỡ phần *Bộ luật* — phần agent luôn đọc — ≤ 120 KB với mọi agent, kể cả `core-reviewer` (đo theo từng phạm vi BE/FE; người dùng chốt 2026-09-27, [ADR-0109](adr/0109-agent-doc-checklist-va-luat-theo-chu-de.md)). Vượt ngưỡng thì rút file luật hoặc chuyển hàng sang *Tra cứu* — không nới ngưỡng. Lý do và bẫy của một file luật đặt ở `wiki-core/{be,fe}/ly-do/` cùng tên | MUST | `check-docs.sh` §23 — đọc thẳng tiêu đề mục và đường dẫn trong bảng; agent không có mục *Bộ luật* là FAIL, vì "không đo" không được trộn với "đạt ngưỡng" | ✅ |
| D37 | **Một câu văn xuôi không được chép nguyên văn sang file khác.** Dòng ≥ 100 ký tự (ngoài bảng, code, tiêu đề, blockquote, nhãn trạng thái, dòng trỏ đường) xuất hiện ở ≥ 3 file trong đó có file `docs/` là vi phạm; 2 file là cảnh báo. Sửa bằng chọn một file chủ, còn lại trỏ đường | MUST NOT | `check-docs.sh` §24 — quét cả `.claude/`; cụm thuần `.claude/` chỉ cảnh báo | ✅ |
| D38 | **File luật vượt 50 KB thì tách phần "vì sao / bẫy / ví dụ mở rộng" sang `wiki-core/{be,fe}/ly-do/<cùng tên>.md`**, không bỏ luật, không nới ngưỡng | SHOULD | `check-docs.sh` §25 — cảnh báo mềm (NOTE), không chặn; áp cho `quy-uoc/*`, `kien-truc-core-module.md`, `script-runbook.md` (không áp cho `schema-core.md` — bảng định nghĩa); §23 mới chặn khi tổng bộ luật của một agent vượt ngưỡng | ✅ |
| D39 | **Cột "Trạng thái" của các bảng `RULES-*.md` phải khớp cổng thật.** Khai `✅`/`✅⌀` thì cổng phải tồn tại; khai `📐` thì **không** được có file cổng nào mang đúng tên đã chốt ở cột *Ép bằng gì*; khai `⏸️` thì cổng phải tồn tại và lý do chưa chạy phải là môi trường, không phải thiếu code | MUST | Lệnh rà hai chiều ở [`RULES.md`](RULES.md) — **chạy tay**, chưa phải mục cổng. Hai điều còn phải xử lý trước khi nâng nó thành mục cổng: [nợ](DEBT.md) | 🕳️ nợ |

> **Mã `B*` là nợ của Backend**, sinh ra ở [nợ](DEBT.md), tách khỏi dãy `A*`/`E*`/`R*`/`S*` vì lúc sinh chưa có cổng. **Tốt nghiệp khỏi sổ nợ thì GIỮ NGUYÊN SỐ** và chuyển lên bảng có cổng ở [`RULES-BE.md`](RULES-BE.md): mã là định danh — đánh số lại làm mọi trích dẫn cũ trỏ vào hư không, và dãy `S*` không còn chỗ trống (`S12` đã có chủ ở [`DEBT.md`](DEBT.md)). Liệt kê bằng lệnh, không chép: `awk '/^\| B[0-9]+ \|/ { print $2 }' docs/RULES-*.md`. Thấy một hàng `B*` trong bảng có cổng thì kiểm cột *Trạng thái* của chính hàng đó, **không** "dọn cho nhất quán"; kéo một mã `B*` còn `🕳️` lên bảng có cổng là làm bảng trông đầy đủ hơn thực tế.

> **D14 và D15 cố ý KHÔNG nằm ở bảng này** — chúng chưa có cổng nào, nên chỗ của chúng là [danh sách nợ](DEBT.md).

## 2. Luật ranh giới `.claude` ↔ `docs`

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| C1 | `.claude/` không chứa câu có thể trở thành SAI khi code đổi | MUST NOT | `check-docs.sh` §2 (bắt phần kiểm được: code block) | ✅ một phần |
| C2 | Không chép nội dung từ `docs/` sang `.claude/` — chỉ trỏ đường dẫn | MUST NOT | Review bởi `core-reviewer` | 🕳️ nợ |
| C3 | Dòng trỏ đường trong `.claude/` là một dòng, không kèm tóm tắt | MUST | Review | 🕳️ nợ |
| C4 | Agent không chạy lệnh nào trong danh sách cấm ở [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §1 — lệnh git ghi, lệnh đổi database, lệnh phát hành gói hay image | MUST NOT | **Hai lớp.** (1) `settings.json` → `permissions.deny`, mỗi mục cấm lệnh khai đủ ba dạng `Bash` / `rtk` / `PowerShell` — chỉ chặn chuỗi khớp nguyên văn tiền tố đã khai, trong đúng công cụ đã khai. (2) Hook `PreToolUse` cho công cụ Bash và PowerShell: đọc tiền tố cấm từ chính `permissions.deny`, tách lệnh ghép, bóc tiền tố bọc rồi mới khớp. Đường vòng đã thử: [`audit/2026-09-14-lenh-cam-lot-qua-powershell-va-tien-to.md`](audit/2026-09-14-lenh-cam-lot-qua-powershell-va-tien-to.md) | ✅ một phần — phần hở ở [nợ](DEBT.md) |
