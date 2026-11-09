---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0111 — Git chặn theo danh sách cho phép; cổng tài liệu chặn một lần mỗi tập lỗi; `architect` đứng trước thi công chỉ khi có dấu hiệu ADR hoặc code mới vào Core, và sửa luật theo finding; luật hiệu lực không nằm dưới `📐`; skill gọn lại; sổ nợ ghi người sửa

> **Trạng thái:** Đã chấp nhận (2026-09-29) · Bổ sung bởi [ADR-0112](0112-so-no-them-nhan-cho-quyet-dinh-kem-lenh-dem-thu-ba-cham-core-khong-quyet-dinh-khong-ghi-nhat-ky.md) (2026-09-29) · Bổ sung bởi [ADR-0113](0113-ban-giao-qua-phien-chinh-no-review-phien-chinh-ghi-tai-lieu-ban-giao-o-spec-go-muc-11-quet-shell-theo-loi-goi.md) (2026-09-30) · Sửa một phần bởi [ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md) (2026-10-05)

## Bối cảnh

Bản đánh giá quy trình ngày 2026-09-29 soát `.claude/` và `docs/` sau đợt refactor của [ADR-0108](0108-hook-stop-hoan-khi-cho-agent-nen-docs-khong-keo-review.md) và [ADR-0109](0109-agent-doc-checklist-va-luat-theo-chu-de.md). Người dùng trả lời từng câu trong ngày; câu nào cũng chọn phương án được đề xuất. Những điểm của bản đánh giá mà ADR này trả lời:

- `.claude/agents/architect.md` (chuỗi `phải qua agent này, và phải có ghi nhận quyết định`) và dòng *Cơ chế chống* ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 buộc **mọi** thay đổi chạm Core qua `architect`. Dòng sau còn đòi ADR, trong khi quyết định 6 của ADR-0109 đã đưa quyết định nhỏ về nhật ký.
- Lớp chặn git là một danh sách cấm. `.claude/settings.json` không chứa chuỗi `git update-ref`, `git symbolic-ref`, `git gc`, `git notes`; bản đánh giá đã tái hiện bốn lệnh ghi đó lọt qua.
- Hook `Stop` chặn tới khi cổng tài liệu xanh. Lỗi do phiên khác hay do người dùng đang sửa làm phiên bị chặn không đáy. ADR-0108 từng loại *"chặn một lần rồi thả"* để giữ chốt bằng máy; nguyên nhân mới này chưa có lúc đó.
- Có luật đang hiệu lực nằm trong tệp `-chua-thi-cong` dưới nhãn `📐`, và [`../quy-uoc/tieu-chi-review.md`](../quy-uoc/tieu-chi-review.md) §3.2 miễn trừ nhãn đó khỏi tầm chấm.
- Skill `/feature-kickoff` kể lại luồng mà `.claude/README.md` §3 làm chủ. `doc-sync` và `arch-check` trùng phần với cổng. `/core-review` còn khuyên thu hẹp theo chủ đề: `.claude/skills/core-review/SKILL.md`, chuỗi `Thu hẹp thêm nếu có thể: một chủ đề cụ thể`.
- `design-expert` ghim `model: sonnet`, trong khi `.claude/CLAUDE.md` §11 giao việc soát và viết luật cho Opus.
- Cột *Chặn bởi* ở [`../DEBT.md`](../DEBT.md) không nói ai sửa, khi nào kích hoạt.

## Quyết định

Người dùng chốt ngày 2026-09-29, từng câu.

| # | Quyết định |
| --- | --- |
| 1 | Finding của review đòi sửa **nội dung** luật hay quy ước thì `architect` sửa, do phiên chính giao kèm mã finding. Sửa **cơ học** — đường dẫn, số §, neo cũ, nhãn trạng thái — thì phiên chính sửa thẳng. `tech-writer` chỉ viết tài liệu bàn giao. Phạm vi `docs/Design/` của `design-expert` không đổi |
| 2 | `architect` là bước bắt buộc **trước** thi công chỉ khi việc mang ít nhất một dấu hiệu ở [`README.md`](README.md) §2, hoặc đưa code mới vào Core — việc phải xét ngưỡng ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2. Việc chạm Core khác thi công thẳng rồi qua `core-reviewer`; hook vẫn ép bước đó |
| 3 | Skill `/feature-kickoff` rút thành checklist khoảng 40–60 dòng cho feature nghiệp vụ của Module. Luồng chi tiết do `.claude/README.md` §3 làm chủ |
| 4 | Lớp chặn git đổi sang **danh sách cho phép** các lệnh đọc: `status`, `diff`, `log`, `show`, `blame`, `ls-files`, `rev-parse`, `check-ignore`, `remote -v`, `cat-file`, `ls-tree`, `grep`, `describe`, `shortlog`. Hook chặn mọi lệnh git khác. Bảng ở `.claude/CLAUDE.md` §1 đổi theo |
| 5 | Hook `Stop` nhớ dấu vân tay của tập lỗi cổng tài liệu. Mỗi tập lỗi chỉ chặn **một lần** mỗi phiên: phiên dừng lại mà vẫn đúng tập đó thì hook không chặn nữa và báo người dùng; có lỗi mới thì chặn lại. Lớp chặn `core-reviewer` không đổi |
| 6 | Luật đang hiệu lực không nằm dưới nhãn `📐`. **(a)** Đưa luật hiệu lực về tệp chính và thêm dòng ở `RULES-*`. Các ca đã biết: cấm retry `POST`/`PUT`/`DELETE` và timeout ở [`../quy-uoc/fe-api-client-chua-thi-cong.md`](../quy-uoc/fe-api-client-chua-thi-cong.md); Swagger ở Production ở [`../quy-uoc/be-api-controller-chua-thi-cong.md`](../quy-uoc/be-api-controller-chua-thi-cong.md); bảng *Tính chất* ở [`../database/script-runbook.md`](../database/script-runbook.md) §9–§10. **(b)** Luật mới: tệp `-chua-thi-cong` chỉ chứa chữ ký và ví dụ; mọi câu MUST/MUST NOT *"khi thi công X"* phải có dòng `📐` ở `RULES-*`. Chưa có cổng — hàng nợ và mã luật thêm khi thi công. **(c)** [`../quy-uoc/tieu-chi-review.md`](../quy-uoc/tieu-chi-review.md) §3.2 thu hẹp: `📐` chỉ miễn finding *"code thiếu X"*, không miễn *"code có X mà trái luật của X"* |
| 7 | Skill `doc-sync` và `arch-check` gộp thành **một** skill soát định kỳ, chạy trong subagent riêng, bỏ các phép kiểm đã trùng cổng. `review-doc` giữ nguyên |
| 8 | Skill `/core-review` viết lại, khoảng 30 dòng. Lời giao có đúng ba trường: phạm vi `BE` hoặc `FE` · phần đổi hay toàn bộ · `Vòng n/2`. Bỏ việc thu hẹp theo chủ đề. 🔴 thì sửa rồi review vòng sau; từ 🟠 trở xuống thì ghi [`../DEBT.md`](../DEBT.md) kèm người sửa; hết vòng 2 còn 🔴 thì hỏi người dùng |
| 9 | `design-expert` giữ Sonnet. Phiên chính nâng Opus cho lượt audit giao diện, hoặc lượt sửa `docs/Design/DESIGN.md` hay `docs/Design/COMPONENTS.md` |
| 10 | Sổ nợ [`../DEBT.md`](../DEBT.md): cột *Chặn bởi* ghi thêm `người sửa · điều kiện kích hoạt` ngay trong ô; soát lại các hàng ⏳ một lượt; đóng E17, E18; tách mục nợ của review khỏi nợ cổng. Giá trị mới `⏳ chờ quyết định` người dùng đã chọn nhưng mâu thuẫn quyết định 4 của [ADR-0045](0045-cho-src-sua-dinh-nghia-khong-them-ky-hieu-thu-tu.md), nên **chưa thi công** tới khi có câu trả lời cho *Câu hỏi còn mở* 1 |

Quan hệ với ADR cũ, theo [`README.md`](README.md) §5.1:

- **ADR-0108 — sửa một phần.** Hết hiệu lực: cụm *"hết agent nền thì chặn như trước"* của quyết định 1, với lỗi cổng tài liệu cùng tập đã chặn một lần trong phiên. Còn hiệu lực: phần hoãn khi còn agent nền, lớp chặn `core-reviewer`, quyết định 2–6.
- **ADR-0109 — bổ sung.** Quyết định 1 thêm vào quyết định 5 của nó; 6 vế (b) thêm vào 8; 8 thêm vào 3–4; 9 thêm vào 1. Không phần nào hết hiệu lực.

## Phương án đã cân nhắc và vì sao loại

Mỗi quyết định có hai phương án người dùng đã thấy và không chọn. Quyết định 6 và 10: lời giao không nêu phương án bị loại; hai dòng đó do kiến trúc sư ghi.

| # | Phương án bị loại | Vì sao loại |
| --- | --- | --- |
| 1 | Mở rộng `tech-writer` sang sửa luật | `tech-writer` không đổi quy ước (`.claude/agents/tech-writer.md`, chuỗi `đổi quy ước trong`), và viết luật là việc `.claude/CLAUDE.md` §11 giao Opus |
| 1 | Người dùng duyệt từng thay đổi trước | Mỗi finding thêm một lượt chờ người dùng |
| 2 | Giữ nguyên — mọi thay đổi chạm Core qua `architect` trước | Sửa lỗi Core không mang quyết định nào cũng chờ một lượt Opus; hàng rào sau thi công đã có |
| 2 | Bỏ hẳn bước trước thi công | Quyết định có dấu hiệu ADR và việc đưa code vào Core rơi về người thi công — đúng thứ vai `architect` tách ra để tránh |
| 3 | Giữ skill, sửa chỗ sai | Luồng vẫn hai bản, lệch lại được (`.claude/CLAUDE.md` §5) |
| 3 | Xoá skill | Mất lối vào cho feature nghiệp vụ |
| 4 | Giữ danh sách cấm, thêm bốn lệnh | Danh sách liệt kê luôn thiếu lệnh ghi kế tiếp |
| 4 | Giữ nguyên | Bốn đường đã tái hiện vẫn mở |
| 5 | Chỉ chặn lỗi ở tệp phiên đã sửa | Lỗi phiên gây ra có thể nằm ở tệp khác — ví dụ link gãy vì phiên đổi tên tệp đích |
| 5 | Giữ nguyên | Vòng chặn không đáy |
| 6 | Chỉ vế (a) — sửa các ca đã biết | Không có (b) và (c) thì ca mới lặp lại, và review vẫn miễn chấm nó |
| 7 | Giữ hai skill, cắt phần trùng | Hai skill soát định kỳ vẫn chồng phạm vi nhau |
| 7 | Xoá cả hai | Mất lượt soát thứ cổng không bắt được, như phần diễn đạt lại của D13 |
| 8 | Xoá skill | Lời giao `core-reviewer` không còn khuôn |
| 8 | Chỉ thêm giới hạn vòng | Giữ lời khuyên thu hẹp theo chủ đề, ngược quyết định 7 của ADR-0109: người soát mở đủ tệp của mọi chủ đề trong phần đổi |
| 9 | Ghim Opus | Mọi lượt, kể cả viết spec màn — loại việc ADR-0109 quyết định 1 giao Sonnet — chạy Opus |
| 9 | Không quy định | Lượt audit và sửa luật Design chạy Sonnet, ngược `.claude/CLAUDE.md` §11 |
| 10 | Thêm cột *Người sửa*, *Điều kiện kích hoạt* | Lệnh đếm ở đầu bảng neo vào hình dạng 5 cột |

## Hệ quả

### Tích cực

- Bốn lệnh git ghi đã tái hiện bị chặn; lệnh git chưa ai nghĩ tới cũng bị chặn mặc định.
- Phiên không còn bị cổng tài liệu chặn không đáy vì lỗi của người khác.
- Việc chạm Core không mang quyết định không phải chờ `architect`.
- Luật đang hiệu lực không còn được miễn chấm chỉ vì nằm cạnh phần chưa thi công.
- Skill không còn kể lại luồng mà `.claude/README.md` làm chủ.
- Hàng nợ nói được ai sửa và khi nào.

### Tiêu cực

- **Quyết định 2 dựa vào phán đoán của người giao việc.** Đọc sai "có dấu hiệu không" thì code vào Core mà không ai hỏi ngưỡng trước. `core-reviewer` chỉ bắt được khi code đã viết, lúc dời đi tốn hơn.
- **Danh sách cho phép chặn cả lệnh đọc hợp lệ chưa có tên** — ví dụ `git merge-base`, `git reflog`, `git config --get`. Mỗi lần cần là một lần sửa danh sách.
- **Lệnh trong danh sách vẫn có tuỳ chọn ghi tệp hoặc chạy lệnh:** `git diff --output=<tệp>`, `git grep -O<lệnh>`, tuỳ chọn toàn cục `git -c`. Hook phải chặn thêm các tuỳ chọn đó, không thì chúng thành đường vòng mới.
- **`permissions.deny` khớp theo tiền tố đã khai** (hàng C4 ở [`../RULES-DOCS.md`](../RULES-DOCS.md)), nên không diễn đạt được "mọi lệnh git trừ …". Lệnh git ghi mà `deny` không liệt kê chỉ còn một lớp chặn là hook. Các đường hook không thấy ở hàng C4 của [`../DEBT.md`](../DEBT.md) không đổi.
- **Cổng tài liệu thôi là chốt cứng với lỗi tồn tại lâu.** Agent dừng lần hai với cùng tập lỗi thì lượt kết thúc khi cổng còn đỏ. Lớp bắt còn lại là người dùng đọc lời báo và job `check-docs` trên CI.
- `architect` thành điểm nghẽn cho mọi finding sửa nội dung luật; mỗi finding một lượt Opus.
- Quyết định 6 vế (a) thêm dòng vào `RULES-*` — phần luôn đọc — và làm tệp quy ước chính dài thêm. Vế (c) làm lượt review ở khu `📐` ra nhiều finding hơn.
- Quyết định 9 dựa vào phiên chính nhớ nâng model; không hook nào ép.
- Skill gộp chạy trong subagent riêng: phiên chính chỉ thấy báo cáo, không thấy từng phép kiểm.

### Rút lui nếu sai

Không quyết định nào chạm `src/` hay dữ liệu. Mỗi quyết định sửa những tệp riêng trong `.claude/` hoặc `docs/`, liệt kê ở mục *Thi công*.

1. Viết ADR mới nêu quyết định nào sai. Dòng trạng thái của ADR này thêm `· Sửa một phần bởi ADR-NNNN (…)`.
2. Lấy lại bản cũ của đúng các tệp mà quyết định đó sửa, từ lịch sử git. Người dùng chạy lệnh git.
3. Quyết định 4 và 5: chạy lại `bash .claude/hooks/tests/run-tests.sh` sau khi trả hook về. Dấu vân tay trong thư mục `.state` hết theo lượt dọn phiên cũ của hook `Stop` (ADR-0108).
4. Luật mới ở quyết định 6 vế (b): gỡ hàng khỏi `RULES-*` và `DEBT.md`; mã luật không dùng lại.

### Dấu hiệu quyết định này bắt đầu sai

- `core-reviewer` ra 🔴 loại *"thứ này thuộc module"* trên một việc chạm Core đã bỏ qua `architect`.
- Danh sách cho phép git phải sửa nhiều lần, hoặc có người xin thêm một lệnh có dạng ghi.
- PR ra với job `check-docs` đỏ sau những phiên đã dừng với lời báo "đã chặn một lần".
- Một tệp `-chua-thi-cong` lại mang câu MUST/MUST NOT.
- Hàng nợ ghi người sửa nhưng không ai nhận.

## Câu hỏi còn mở — chờ người dùng

Câu trả lời ghi bằng ADR mới hoặc một dòng ở [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md), không sửa mục này (luật D45).

1. **Giá trị `⏳ chờ quyết định` của quyết định 10 mâu thuẫn quyết định 4 của ADR-0045: *"Ký hiệu thứ tư bị loại"*.** ADR-0045 loại vì hai lý do đã đo:
   - giá trị không chứa `src/` nằm ngoài cả hai lệnh đếm ở đầu bảng của `DEBT.md`, thành vùng mù thứ hai;
   - tách vài hàng ra làm các hàng còn lại trông như đã soát.

   Lượt soát lại hàng ⏳ của quyết định 10 trả lời lý do thứ hai; lý do thứ nhất còn nguyên. Hai hướng:
   - **(a)** nhận giá trị thứ tư kèm một lệnh đếm thứ ba — ADR-0045 đổi dòng trạng thái thành *Sửa một phần*;
   - **(b)** không thêm giá trị — hàng chờ quyết định ghi `🔧 làm được ngay — chờ <ai> quyết`, như cách các hàng chờ người dùng đã ghi tới nay, và lệnh đếm `🔧` vẫn phủ chúng.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Bốn lệnh git lọt: bản đánh giá đã tái hiện. Vòng chặn không đáy: bản đánh giá nêu; lượt ghi này **chưa đo** số lần. Các mục khác là mâu thuẫn đọc được trên giấy, neo ở *Bối cảnh* |
| 2 | Phương án đơn giản hơn | *Giữ nguyên* ở từng quyết định; bảng *Phương án* nêu chỗ nó thiếu |
| 3 | Chi phí vận hành thêm | Giữ danh sách cho phép git cùng bộ test hook; phần nhớ dấu vân tay ở hook `Stop`; một luật mới cần cổng (6b) |
| 4 | Ai bảo trì | Hook, skill, `.claude/`, `check-docs.sh`: phiên chính. Tệp luật, `DEBT.md`: `architect` |
| 5 | Rút lui thế nào | Mục *Rút lui nếu sai*: ADR mới cộng lấy lại bản cũ của tệp; không dữ liệu, không `src/` |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Không quyết định nào chạm `src/`; quyết định 2 giữ câu hỏi ngưỡng cho mọi code mới vào Core |

## Thi công

Chưa quyết định nào được thi công ở lượt ghi ADR này.

| # | Tệp phải sửa | Ai |
| --- | --- | --- |
| 1 | `.claude/agents/architect.md` (phạm vi ghi), `.claude/agents/tech-writer.md`, `.claude/README.md` §2–§3 | Phiên chính |
| 2 | `.claude/agents/architect.md`, `.claude/README.md` §3; dòng *Cơ chế chống* ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 | Phiên chính; `architect` |
| 3 | `.claude/skills/feature-kickoff/SKILL.md` | Phiên chính |
| 4 | `.claude/CLAUDE.md` §1 cùng lượt với mục §1 của `.claude/check-docs.sh` — bảng đó là đầu vào của mục; hook `PreToolUse` và bộ test hook; hàng C4 ở [`../RULES-DOCS.md`](../RULES-DOCS.md) | Phiên chính; `architect` |
| 5 | Hook `Stop` và bộ test hook, `.claude/CLAUDE.md` §8 | Phiên chính |
| 6 | Vế (a): ba chỗ nêu ở quyết định và `RULES-*`. Vế (b): `RULES-DOCS.md`, hàng nợ ở `DEBT.md`. Vế (c): `tieu-chi-review.md` §3.2 | `architect` |
| 7 | `.claude/skills/doc-sync/`, `.claude/skills/arch-check/`, `.claude/README.md` §6; cột ý tưởng của hàng D13 ở `DEBT.md` nhắc `/doc-sync` | Phiên chính |
| 8 | `.claude/skills/core-review/SKILL.md`; `.claude/CLAUDE.md` §8 lớp 1 (cụm *"chỉ truyền phạm vi"*) và §11 | Phiên chính |
| 9 | `.claude/agents/design-expert.md`, `.claude/CLAUDE.md` §11 | Phiên chính |
| 10 | `DEBT.md` — bảng chú giải, cột *Chặn bởi*, hàng E17 và E18, tách mục | `architect` |

## Liên quan

- [ADR-0108](0108-hook-stop-hoan-khi-cho-agent-nen-docs-khong-keo-review.md) — sửa một phần bởi quyết định 5.
- [ADR-0109](0109-agent-doc-checklist-va-luat-theo-chu-de.md) — bổ sung bởi quyết định 1, 6 vế (b), 8, 9.
- [ADR-0045](0045-cho-src-sua-dinh-nghia-khong-them-ky-hieu-thu-tu.md) — *Câu hỏi còn mở* 1.
- [ADR-0048](0048-cam-nhan-dich-den-cap-file-khi-neo-tro-toi-code-that.md) — quyết định 6 vế (c) thu hẹp miễn trừ mà ADR đó dựa vào; quyết định của nó không đổi.
- [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md), các dòng 2026-09-29 — nợ B26, B27, B28, F44, F45, F47, R8 và đoạn *Hết hoãn*: quyết định nhỏ cùng đợt.
