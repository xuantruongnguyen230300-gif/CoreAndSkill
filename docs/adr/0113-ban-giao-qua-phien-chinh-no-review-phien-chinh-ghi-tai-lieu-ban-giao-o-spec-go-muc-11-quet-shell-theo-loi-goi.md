---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0113 — Bàn giao giữa agent đi qua phiên chính; phiên chính ghi nợ review; tài liệu bàn giao ở `spec/<feature>/ban-giao/`; gỡ mục §11 của cổng tài liệu, D16 hết áp dụng; nhánh shell của `on-edit.sh` quét đúng khoảng chạy của lời gọi

> **Trạng thái:** Đã chấp nhận (2026-09-30) · Sửa một phần bởi [ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md) (2026-10-05)

## Bối cảnh

Ngày 2026-09-30 người dùng trả lời từng câu một loạt câu hỏi về quy trình; câu nào cũng chọn phương án được đề xuất. ADR này gom năm câu mang dấu hiệu ở [`README.md`](README.md) §2 — cách giao việc cho agent, cổng, hook. Ba câu còn lại là quyết định nhỏ, ghi ở [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md) cùng ngày.

Tệp dưới `.claude/` trích ở mục này là bản ở commit `ac807bd`, vì một lượt song song đang sửa chúng — trừ chú thích của `on-edit.sh` ở câu năm, là bản trên đĩa ngày 2026-09-30.

**Một — kênh bàn giao.** Agent bàn giao thẳng cho nhau bằng `SendMessage`: `.claude/agents/design-expert.md`, chuỗi `Đường dẫn tới file spec, qua`; `.claude/agents/core-reviewer.md`, chuỗi `Báo cáo xong thì gửi cho agent đã gọi bạn qua`. Trong khi đó đếm vòng review, phạm vi và cỡ việc 30–40 phút là việc của phiên chính (`.claude/CLAUDE.md` §11; quyết định 2 và 4 của [ADR-0109](0109-agent-doc-checklist-va-luat-theo-chu-de.md)). Lớp 1 của hàng rào `core-reviewer` đã buộc agent thi công không tự gọi người soát mà ghi dòng `CẦN CORE-REVIEW` cho phiên chính gọi (`.claude/CLAUDE.md` §8). Bàn giao thẳng chạy ngoài tầm nhìn của phiên chính, nên những thứ phiên chính phải giữ không có chỗ để giữ.

**Hai — ai ghi nợ review.** Quyết định 8 của [ADR-0111](0111-git-danh-sach-cho-phep-cong-docs-chan-mot-lan-architect-truoc-thi-cong-theo-dau-hieu.md) đưa finding từ 🟠 trở xuống vào [`../DEBT.md`](../DEBT.md) kèm người sửa, nhưng không nói ai ghi. Quyết định 1 của nó giao sửa nội dung luật cho `architect`, sửa cơ học cho phiên chính. Chép một finding thành một hàng là việc cơ học; giao `architect` thì mỗi hàng tốn một lượt Opus.

**Ba — tài liệu bàn giao.** `.claude/agents/tech-writer.md` ghi ở chỗ người dùng chỉ định (chuỗi `ở nơi người dùng chỉ định`) và dừng hỏi khi chưa có khuôn (chuỗi `Người dùng chưa chỉ định khuôn`). [`../Design/CLAUDE.md`](../Design/CLAUDE.md) khai repo chưa có khuôn nào cho TechDoc, UserGuide, tài liệu UnitTest. Mục *chưa kiểm* của tài liệu UnitTest lấy từ `test-engineer` (chuỗi `Nguồn cho mục này là`) bằng một lần bàn giao thẳng — đúng kênh mà câu một đóng. Chưa feature nào đi tới bước bàn giao.

**Bốn — mục §11 của cổng tài liệu.** Mục đó ép D16 — không tuyên bố `CÓ THẬT` khi repo chưa có `src/` — và chỉ xét khi thư mục `src/` vắng mặt; có `src/` thì nó in một dòng NOTE (`.claude/check-docs.sh`, chuỗi `§11 chỉ áp ở giai đoạn 1`). `src/` vào git từ commit `d8efddc`. Dự án dẫn xuất nhận Core bằng bản clone đầy đủ ([ADR-0016](0016-phan-phoi-core-bang-clone.md)), nên repo đi theo đường phân phối đó luôn có `src/`, và điều kiện của D16 không còn đúng ở đâu.

**Năm — cửa sổ quét của nhánh shell.** Quyết định 6 của [ADR-0108](0108-hook-stop-hoan-khi-cho-agent-nen-docs-khong-keo-review.md) cho nhánh lệnh shell của `.claude/hooks/on-edit.sh` quét tệp đổi kể từ mốc lần quét trước của phiên. Cửa sổ đó gom cả thay đổi do phiên khác, IDE hay người dùng làm giữa hai lời gọi: phiên chỉ chạy lệnh đọc cũng mang cờ chạm Core — đã tái hiện. Lượt thi công ngày 2026-09-30 đo trên Claude Code 2.1.282 và 2.1.283: khoá `duration_ms` ở tầng ngoài payload `PostToolUse` và `PostToolUseFailure` là thời gian lệnh thật sự chạy, không gồm hook `PreToolUse` và lúc chờ duyệt quyền; lệnh thoát khác 0 bắn `PostToolUseFailure` thay cho `PostToolUse`. Số đo ghi ở chú thích nhánh shell của `on-edit.sh` (chuỗi `Khoảng lời gọi =`); lượt ghi ADR này không đo lại.

## Quyết định

Người dùng chốt ngày 2026-09-30, từng câu.

| # | Quyết định |
| --- | --- |
| 1 | Mọi bàn giao giữa agent đi qua phiên chính. Agent kết thúc báo cáo bằng dòng `BÀN GIAO: <agent> — <việc / đường dẫn>`; phiên chính gọi agent kế và giữ đếm vòng, phạm vi, cỡ việc 30–40 phút. `SendMessage` chỉ dùng giữa `backend-expert` và `frontend-expert` đang chạy song song, để gửi API Contract Card |
| 2 | Phiên chính ghi hàng nợ review — finding từ 🟠 trở xuống sau review — vào mục *Nợ review* của [`../DEBT.md`](../DEBT.md): chép nguyên finding, thêm cụm `· người sửa:` và điều kiện kích hoạt. Finding đòi đổi nội dung luật vẫn giao `architect`, theo quyết định 1 của ADR-0111 |
| 3 | `tech-writer` ghi vào `spec/<feature>/ban-giao/`, ba tệp `TechDoc.md`, `UserGuide.md`, `UnitTest.md`. Khuôn: lần chạy đầu `tech-writer` đề xuất, người dùng duyệt một lần, rồi khuôn nằm ở thư mục con `ban-giao/` của `spec/_template/`. Mục *ca cố ý bỏ qua* của `UnitTest.md` do phiên chính chuyển nguyên văn từ báo cáo của `test-engineer` |
| 4 | Gỡ mục §11 của `.claude/check-docs.sh`. Luật D16 hết áp dụng: giữ mã, không dùng lại |
| 5 | Nhánh lệnh shell của `.claude/hooks/on-edit.sh` quét tệp đổi trong đúng khoảng chạy của lời gọi: `duration_ms` của payload `PostToolUse` hay `PostToolUseFailure`, cộng 5 s; payload thiếu `duration_ms` thì lùi về cửa sổ 90 s. Lệnh thất bại đi qua sự kiện `PostToolUseFailure`, đăng ký trong `.claude/settings.json` (người dùng duyệt) |

Phần của kiến trúc sư, trong quyết định 4: cột *Trạng thái* của D16 ghi bằng chữ, vì chú giải ở [`../RULES.md`](../RULES.md) không có ký hiệu cho luật hết áp dụng. Các mục khác của cổng giữ nguyên số và số §11 không dùng lại, vì cột *Ép bằng gì* của mọi bảng `RULES-*` trích mục cổng theo số.

Quan hệ với ADR cũ, theo [`README.md`](README.md) §5.1:

- **ADR-0108 — sửa một phần.** Hết hiệu lực: vế đầu của quyết định 6 — nhánh shell quét theo mốc lần quét trước của phiên. Còn hiệu lực: vế sau của quyết định 6 — nhánh Edit/Write so với mọi mục của khối, không lọc khu vực trước; quyết định 1–5 trong phạm vi ADR-0111 chưa sửa.
- **ADR-0111 — bổ sung.** Quyết định 2 nói ai ghi hàng mà quyết định 8 của ADR-0111 đòi; quyết định 3 nói tài liệu bàn giao của quyết định 1 của nó nằm ở đâu. Không phần nào hết hiệu lực.
- ADR-0109 không đổi; quyết định 1 dựa vào quyết định 2 và 4 của nó.

## Phương án đã cân nhắc và vì sao loại

Câu 1–3: hai phương án người dùng đã thấy và không chọn. Câu 4, 5: lời giao không nêu phương án bị loại; dòng câu 4 do kiến trúc sư ghi, dòng câu 5 lấy từ chú thích của lượt thi công ở `on-edit.sh` và bản ở commit `ac807bd`.

| # | Phương án bị loại | Vì sao loại |
| --- | --- | --- |
| 1 | Giữ `SendMessage` tự do giữa mọi agent | Chuỗi bàn giao chạy ngoài tầm nhìn của phiên chính; đếm vòng, phạm vi, cỡ việc không ai giữ |
| 1 | Cấm hẳn `SendMessage` | Hợp đồng API giữa BE và FE chạy song song phải vòng qua phiên chính, phía FE chờ card lâu hơn — `.claude/agents/backend-expert.md` gửi card ngay khi hợp đồng chốt để FE đi trước |
| 2 | `architect` ghi | Mỗi hàng một lượt Opus cho một việc chép; `architect` thành điểm nghẽn cả với finding không đổi luật |
| 3 | Hoãn cả nơi ghi lẫn khuôn tới feature đầu | Lượt `tech-writer` đầu dừng hai lần hỏi người dùng; hai feature đầu có thể ghi hai nơi khác nhau |
| 3 | Chốt cả nơi ghi lẫn khuôn ngay | Khuôn chốt khi chưa có tính năng nào để thử là khuôn viết cho tài liệu tưởng tượng |
| 4 | Giữ §11 | Mục cổng không bao giờ xét gì nữa mà vẫn in NOTE mỗi lần chạy, và bảng luật vẫn khai `✅⌀` cho một luật không còn điều kiện áp |
| 4 | Bỏ điều kiện `src/`, cho §11 luôn xét | Thành một luật khác. Có `src/` thì câu `CÓ THẬT` đối chiếu được; đòi ngày cho nó là việc của mục §6 (D6, bắt cả `CÓ THẬT`), đòi neo là `.claude/CLAUDE.md` §4 |
| 5 | Giữ mốc lần quét trước của phiên | Gom thay đổi không do lời gọi này làm; đã tái hiện |
| 5 | Đặt mốc ở hook `PreToolUse` | Hook đó chạy trước bước chờ duyệt quyền; khoảng gộp cả lúc chờ, có thể nhiều phút |
| 5 | Cửa sổ cố định 90 s cho mọi lệnh | Lệnh chạy quá 90 s sửa tệp ở đầu lệnh thì lọt — lý do quyết định 6 của ADR-0108 rời cửa sổ cố định. Chỉ giữ làm đường lùi khi payload thiếu `duration_ms` |

## Ép bằng gì

| # | Cơ chế |
| --- | --- |
| 1 | Chưa có cổng. Harness ép được nếu dòng `tools:` của mọi agent trừ `backend-expert`, `frontend-expert` bỏ `SendMessage` — phiên chính quyết khi thi công. Không bỏ thì chỉ người đọc báo cáo canh |
| 2 | Không ép được bằng máy. Người kiểm: người dùng đọc diff của `DEBT.md`; `core-reviewer` lượt sau gặp lại finding đã ghi |
| 3 | Mục §12 của cổng tài liệu ép ba khoá frontmatter trên từng tệp, vì `spec/` nằm trong tầm quét. Vị trí và tên tệp: chưa có cổng |
| 4 | Không áp — quyết định gỡ một cổng |
| 5 | Bộ test hook, nhóm `on-edit` — `.claude/hooks/tests/run-tests.sh`, chuỗi `cửa sổ — payload PostToolUseFailure`. Lượt ghi ADR này không chạy lại bộ test, và không tìm thấy ca nào đỏ khi mục đăng ký `PostToolUseFailure` bị gỡ khỏi `settings.json` |

## Hệ quả

### Tích cực

- Phiên chính thấy mọi bước bàn giao, nên giữ được đếm vòng review, phạm vi và cỡ việc. Cách của lớp 1 hàng rào `core-reviewer` thành cách chung.
- Hàng nợ review không chờ lượt Opus.
- `tech-writer` biết ghi ở đâu từ lượt đầu; mọi feature cùng một chỗ, và cùng một khuôn sau lần duyệt.
- Output cổng tài liệu bớt một NOTE không kiểm gì; bảng luật thôi khai `✅⌀` cho luật không còn điều kiện áp.
- Phiên chỉ chạy lệnh đọc không còn mang cờ chạm Core vì tệp người khác sửa. Lệnh thất bại mà vẫn ghi tệp — build hỏng giữa chừng, format lỗi — được quét.

### Tiêu cực

- **Mỗi bàn giao thêm một vòng về phiên chính.** Báo cáo của mọi agent dồn vào ngữ cảnh phiên chính. Dòng `BÀN GIAO:` là chữ tự do; phiên chính bỏ sót thì chuỗi việc dừng, và không hook nào nhắc — khác dòng `CẦN CORE-REVIEW`, có hook `Stop` đứng sau.
- **Kênh ngang còn lại nằm ngoài tầm nhìn của phiên chính.** Card đổi giữa lúc BE và FE chạy song song thì phiên chính chỉ biết qua báo cáo cuối của hai bên.
- **`DEBT.md` có hai người ghi.** `architect` giữ mục *Luật chưa có cổng* và cấu trúc bảng; phiên chính thêm hàng ở mục *Nợ review*. Hàng sai hình dạng 5 cột, hay có ký tự gạch đứng trong ô, rơi khỏi cả ba lệnh đếm mà không báo lỗi; không cổng nào kiểm hình dạng hàng.
- **Phiên chính phán đoán finding nào là cơ học.** Phán sai thì một finding đòi đổi luật nằm ở sổ nợ như việc đã giao, và luật sai vẫn đứng.
- **`spec/` nhận một loại tài liệu không phải nghiệp vụ.** `.claude/CLAUDE.md` §2 và [`../../spec/README.md`](../../spec/README.md) tả `spec/<feature>/` là nghiệp vụ; thiếu câu cho `ban-giao/` thì người đọc theo §2 cho tài liệu bàn giao là đặt sai chỗ. `tech-writer` ghi vào khu mà `ba-analyst` làm chủ.
- **Khuôn dựng từ feature đầu tiên.** Nó vừa với loại feature đó. Feature chỉ có API, không màn hình, có thể không cần `UserGuide.md`, và quyết định chưa nói tệp đó để trống hay bỏ. Khuôn nằm trong repo Core và theo bản clone sang dự án dẫn xuất: từ vựng nghiệp vụ của feature đầu lọt vào khuôn thì đi theo.
- **Mục *ca cố ý bỏ qua* chỉ đủ khi báo cáo `test-engineer` đủ.** Báo cáo không liệt kê thì mục rỗng, và mục rỗng đọc như *không bỏ sót gì*.
- **Repo mang bộ cổng mà chưa có `src/`** — dự án mới dựng từ `docs/`, `.claude/` trước khi có code, ngoài đường phân phối của ADR-0016 — không còn cổng nào chặn câu `CÓ THẬT`; chỉ còn D6 đòi ngày, không đòi đối chiếu.
- **Cửa sổ quét còn ba chỗ hở**, ghi ở hàng C5 của [`../DEBT.md`](../DEBT.md). Chỗ nặng nhất hỏng theo hướng bỏ sót: tiến trình chạy nền ghi tệp sau khi `PostToolUse` đã bắn thì không tính cho phiên nào, và hàng rào không bật.
- **Hook đứng trên nghĩa của `duration_ms`, đo ở hai phiên bản harness.** Bản sau đổi nghĩa khoá đó — ví dụ gộp lúc chờ duyệt — thì cửa sổ rộng ra im lặng; bộ test dùng payload dựng tay nên không thấy.

### Rút lui nếu sai

Không quyết định nào chạm `src/` hay dữ liệu. Người dùng chạy mọi lệnh git.

1. Quyết định 1: lấy lại bản cũ của các agent, skill và `.claude/README.md` từ lịch sử git; viết ADR mới; dòng trạng thái của ADR này thêm đoạn quan hệ theo §5.1.
2. Quyết định 2: sửa `.claude/CLAUDE.md` §11 và `.claude/skills/core-review/SKILL.md` giao việc ghi hàng về `architect`. Hàng đã ghi đúng hình dạng thì giữ.
3. Quyết định 3 — phần khó đảo nhất. Tài liệu đã sinh nằm ở `ban-giao/` của từng feature; dời đi là dời từng thư mục và sửa mọi link trỏ tới chúng, chi phí tăng theo số feature đã bàn giao. Khuôn dời một lần. Muốn đổi thì đổi trước feature thứ hai.
4. Quyết định 4: lấy lại mục §11 từ lịch sử git, trả D16 về `✅⌀` và cột *Ép bằng gì* về mục đó. Số §11 còn trống nên không trích dẫn nào gãy.
5. Quyết định 5: lấy lại nhánh shell của `on-edit.sh` từ lịch sử git; gỡ mục `PostToolUseFailure` khỏi `settings.json` nếu không hook nào khác dùng; chạy `bash .claude/hooks/tests/run-tests.sh` tới khi xanh. Cơ chế mốc ở commit `ac807bd` lùi về 90 s khi phiên chưa có mốc, nên trả về không phải dọn gì trong thư mục trạng thái.

### Dấu hiệu quyết định này bắt đầu sai

- Chuỗi việc dừng giữa chừng vì phiên chính đọc sót dòng `BÀN GIAO:`; hoặc agent vẫn gọi `SendMessage` ngoài cặp BE–FE.
- Lượt review sau gặp lại một finding đã ghi nợ mà hàng nợ lệch finding gốc; hoặc ba lệnh đếm của `DEBT.md` lệch số hàng đếm bằng mắt.
- Khuôn bàn giao phải sửa ngay ở feature thứ hai; hoặc xuất hiện `UserGuide.md` rỗng.
- Có repo dùng bộ cổng này mà chưa có `src/`.
- Hàng rào `core-reviewer` không bật sau một phiên có lệnh chạy nền ghi vào Core; hoặc sau khi nâng Claude Code, cờ chạm Core lại gắn cho phiên chỉ chạy lệnh đọc.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Câu 5: cờ chạm Core gắn nhầm đã tái hiện; nghĩa của `duration_ms` đo trên hai phiên bản harness. Câu 4: NOTE đọc được ở output mỗi lần chạy cổng. Câu 1–3: mâu thuẫn đọc được trên giấy, neo ở *Bối cảnh*. **Chưa đo** lần bàn giao thẳng nào đã vượt đếm vòng; chưa feature nào tới bước bàn giao |
| 2 | Phương án đơn giản hơn | *Giữ nguyên* ở từng câu; bảng *Phương án* nêu chỗ nó thiếu |
| 3 | Chi phí vận hành thêm | Phiên chính đọc dòng `BÀN GIAO:`, ghi hàng nợ, chuyển mục *ca cố ý bỏ qua*. Bộ test hook giữ thêm ca cho cửa sổ và `PostToolUseFailure`. Mỗi lần nâng Claude Code phải đo lại nghĩa `duration_ms` — chưa có gì nhắc việc này |
| 4 | Ai bảo trì | Agent, skill, hook, `settings.json`, `check-docs.sh`: phiên chính. Bảng `RULES-*` và mục *Luật chưa có cổng* của `DEBT.md`: `architect`. Hàng ở mục *Nợ review*: phiên chính. Khuôn bàn giao: `tech-writer` đề xuất, người dùng duyệt mọi lần đổi |
| 5 | Rút lui thế nào | Mục *Rút lui nếu sai*. Khó nhất là quyết định 3, chi phí tăng theo số feature đã bàn giao |
| 6 | Có buộc Core biết nghiệp vụ không | Không quyết định nào chạm `src/`. Chỗ gần nhất: khuôn bàn giao dựng từ feature đầu nằm trong repo Core — người dùng soát từ vựng nghiệp vụ lúc duyệt khuôn |

## Thi công

| # | Tệp phải sửa | Ai |
| --- | --- | --- |
| 1 | `.claude/agents/*.md` — mục bàn giao và dòng `tools:`; `.claude/README.md` mục *Bàn giao bằng đường dẫn, không bằng tóm tắt*; `.claude/CLAUDE.md` §11 | Phiên chính |
| 2 | `.claude/CLAUDE.md` §11, `.claude/skills/core-review/SKILL.md` | Phiên chính |
| 3 | `.claude/agents/tech-writer.md` — phạm vi ghi, bước khuôn; `.claude/agents/test-engineer.md` — báo cáo liệt kê ca cố ý bỏ qua; `.claude/CLAUDE.md` §2. [`../../spec/README.md`](../../spec/README.md): câu cho `ban-giao/`. Câu *chưa có khuôn* ở [`../Design/CLAUDE.md`](../Design/CLAUDE.md), sau khi khuôn được duyệt | Phiên chính; `ba-analyst`; `design-expert` |
| 4 | Mục §11 của `.claude/check-docs.sh` — lượt song song. Hàng D16 ở [`../RULES-DOCS.md`](../RULES-DOCS.md) — lượt ghi ADR này. Đoạn trích D16 và §11 ở [`../contracts/README.md`](../contracts/README.md) §3 | Phiên chính; `architect` |
| 5 | `.claude/hooks/on-edit.sh`, `.claude/settings.json`, bộ test hook — đối chiếu 2026-09-30: nhánh shell mang chuỗi `Khoảng lời gọi =`, `settings.json` mang mục `PostToolUseFailure`, chưa commit. Hàng C5 ở [`../DEBT.md`](../DEBT.md) — lượt ghi ADR này | Phiên chính; `architect` |

## Liên quan

- [ADR-0108](0108-hook-stop-hoan-khi-cho-agent-nen-docs-khong-keo-review.md) — sửa một phần bởi quyết định 5.
- [ADR-0111](0111-git-danh-sach-cho-phep-cong-docs-chan-mot-lan-architect-truoc-thi-cong-theo-dau-hieu.md) — bổ sung bởi quyết định 2, 3.
- [ADR-0109](0109-agent-doc-checklist-va-luat-theo-chu-de.md) — quyết định 2 và 4 của nó là thứ quyết định 1 giữ.
- [ADR-0016](0016-phan-phoi-core-bang-clone.md) — lý do D16 hết áp dụng.
- [ADR-0030](0030-dieu-kien-chuyen-giai-doan-2.md) — *Bối cảnh* của nó nêu §11 là một chỗ máy đổi hành vi theo `src/`; quyết định của nó không đổi.
- [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md), các dòng 2026-09-30 — `/ba-story` hỏi từng câu, ngoại lệ của D14, thi công quyết định 5 của ADR-0111: quyết định nhỏ cùng đợt.
