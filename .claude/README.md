# `.claude/` — hệ thống agent & skill của CoreAndSkill

> **Đây là tài liệu VỀ bộ agent, không phải tri thức về sản phẩm.**
>
> [`CLAUDE.md`](CLAUDE.md) §2 chốt ranh giới: `.claude/` chứa **quy trình và ràng buộc**, `docs/` chứa **quy tắc**, `spec/` chứa **nghiệp vụ**. File này là **ngoại lệ duy nhất** được phép ở `.claude/` mà không phải file agent hay file cấu hình — vì nó mô tả chính `.claude/`.
>
> Phép thử khi phân vân: *"xoá hết agent đi thì file này còn giá trị không?"* Còn → thuộc `docs/`. Không → thuộc `.claude/`.

---

## 1. Nội dung `.claude/`

| Đường dẫn | Chứa gì |
| --- | --- |
| [`CLAUDE.md`](CLAUDE.md) | Luật toàn repo: git, ranh giới `.claude` ↔ `docs` ↔ `spec`, nhãn trạng thái, ba khoá frontmatter |
| `settings.json` | Cấu hình harness — `permissions.allow` / `permissions.deny` và khối `hooks`, nơi lệnh git ghi bị **cưỡng chế bằng máy** |
| `check-docs.sh` | Cổng tài liệu. Hook `Stop` chạy nó ở cuối mỗi lượt và CI chạy lại, nên **không lượt nào thoát được nó**. Chạy tay là **tuỳ chọn** — để thấy đỏ ngay lúc đang sửa thay vì lúc kết thúc lượt. Chạy tay qua **công cụ Bash** (Git Bash): trên máy Windows, `bash` gọi từ công cụ PowerShell có thể trỏ vào WSL và không chạy được |
| `hooks/` | Script do harness chạy. Script nào đang gắn vào sự kiện nào: đọc khối `hooks` của `settings.json` — file có mặt trong thư mục chưa chắc đã được gắn. Sửa hook thì chạy bộ test payload trước khi coi là xong, qua công cụ Bash: `bash .claude/hooks/tests/run-tests.sh` (CI cũng chạy) |
| `agents/` | Định nghĩa agent — `ls .claude/agents` là danh sách thật |
| `skills/` | Skill gọi bằng `/<tên>` — `ls .claude/skills` là danh sách thật |

Danh sách thật **đếm bằng lệnh, không chép tay** ([`CLAUDE.md`](CLAUDE.md) §6):

```bash
ls .claude/agents
ls .claude/skills
```

---

## 2. Bộ agent — mỗi người một câu

| Agent | Vai trò | Sửa được gì |
| --- | --- | --- |
| [`ba-analyst`](agents/ba-analyst.md) | Chuyển yêu cầu thô thành User Story, Acceptance Criteria kiểm chứng được, Business Rule | `spec/` |
| [`architect`](agents/architect.md) | Viết ADR, phản biện thiết kế, canh cổng cho mọi thay đổi chạm `Core/` | `docs/adr/` và file luật trong `docs/` |
| [`design-expert`](agents/design-expert.md) | Sở hữu `docs/Design/` — token, spec component, spec màn hình, audit giao diện | `docs/Design/` |
| [`backend-expert`](agents/backend-expert.md) | Viết code backend theo quy ước trong `docs/quy-uoc/be-*.md` | `src/BE/` |
| [`frontend-expert`](agents/frontend-expert.md) | Viết code frontend theo quy ước trong `docs/quy-uoc/fe-*.md` và spec trong `docs/Design/` | `src/FE/` |
| [`test-engineer`](agents/test-engineer.md) | Viết test, tìm ca biên người viết code bỏ sót, kiểm chính các detector kiến trúc | **chỉ file test** |
| [`core-reviewer`](agents/core-reviewer.md) | Đối chiếu code thật với quy tắc trong `docs/`, báo PASS/PARTIAL/MISSING kèm bằng chứng | **không sửa gì** |
| [`tech-writer`](agents/tech-writer.md) | Viết TechDoc, UserGuide, tài liệu UnitTest cho một User Story đã xong | file tài liệu bàn giao |

### Ai gọi ai

```
                        người dùng
                             │
                             ▼
                       ba-analyst ────────────────┐
                             │                    │ feature Core hay Module?
                             │                    ▼
                             │               architect ◄──── mọi thay đổi chạm Core/
                             │                    │          (từ bất kỳ agent nào)
              cần màn hình mới?                   │
                             ▼                    │ ADR
                      design-expert                │
                             │ spec màn hình       │
              ┌──────────────┴──────────────┐     │
              ▼                             ▼     ▼
       backend-expert ◄─ Contract Card ─► frontend-expert
              │                             │
              └──────────────┬──────────────┘
                             ▼
                      test-engineer
                             │ test đỏ vì code sai → trả về agent thi công
                             ▼
                      core-reviewer          (chỉ đọc, không sửa)
                             │ finding → trả về agent thi công
                             ▼
                       tech-writer
```

Những chiều mũi tên đáng chú ý:

- **`architect` nhận từ mọi phía.** Bất kỳ agent nào gặp một quyết định kiến trúc đều dừng và chuyển sang đó. Không có đường vòng.
- **`core-reviewer` do phiên chính gọi, không do agent thi công gọi.** `backend-expert`, `frontend-expert`, `test-engineer` chạm Core thì kết thúc báo cáo bằng dòng `CẦN CORE-REVIEW: BE` và/hoặc `CẦN CORE-REVIEW: FE`; phiên chính hoặc `/feature-kickoff` đọc dòng đó rồi gọi, chỉ truyền phạm vi.
- **`test-engineer` và `core-reviewer` chỉ trả việc ngược lên**, không tự sửa. Đó là thiết kế, không phải hạn chế — xem §4.
- **`ba-analyst` ↔ `architect`.** BA **không** tự quyết một feature thuộc Core hay Module. Đó là quyết định ranh giới kiến trúc.

---

## 3. Luồng làm một feature từ đầu tới cuối

```
yêu cầu thô của người dùng
        │
        ▼
[1] ba-analyst — làm rõ yêu cầu, viết US + AC + Business Rule
        │        → spec/<feature>/business-rules.md, spec/<feature>/ui-spec.md
        │
        ├──── 🙋 CẦN NGƯỜI: trả lời câu hỏi nghiệp vụ còn mở
        ├──── 🙋 CẦN NGƯỜI hoặc architect: feature này thuộc Core hay Module?
        ├──── 🛑 kết luận Core → LUÔN qua architect trước [3]
        │
        ▼
[2] design-expert — CHỈ khi feature cần màn hình/component chưa có spec
        │            → docs/Design/Components/, spec màn hình
        │
        ├──── 🙋 CẦN NGƯỜI: giá trị token mới không suy ra được từ hệ hiện có
        │
        ▼
[3] backend-expert  ║  frontend-expert          ← chạy SONG SONG
        │           ║        │
        │  API Contract Card ─┘  (BE gửi ngay khi hợp đồng chốt, FE không đợi BE code xong)
        │
        ├──── 🛑 DỪNG: việc đòi quyết định kiến trúc mới → architect
        ├──── 🛑 DỪNG: thiếu spec nghiệp vụ → ba-analyst
        ├──── chạm Core → báo cáo kết thúc bằng dòng CẦN CORE-REVIEW: BE | FE
        │
        ▼
[4] test-engineer — tìm ca biên hai agent trên bỏ sót
        │
        ├──── 🛑 DỪNG: test đỏ vì CODE SAI → trả về [3], KHÔNG sửa code cho test xanh
        │
        ▼
[5] core-reviewer — phiên chính gọi khi có dòng CẦN CORE-REVIEW; một lượt = một phạm vi (BE hoặc FE)
        │
        ├──── finding → trả về [3] để sửa. core-reviewer KHÔNG sửa.
        │
        ▼
   🙋 CẦN NGƯỜI: chốt bàn giao
        │
        ▼
[6] tech-writer — TechDoc + UserGuide + tài liệu UnitTest
        │
        └──── 🛑 DỪNG: code và spec nói ngược nhau → báo, không chọn bên
```

### Bốn chỗ bắt buộc có người

| Ở đâu | Vì sao |
| --- | --- |
| Sau [1] — trả lời câu hỏi nghiệp vụ | Nghiệp vụ là thứ **chỉ người dùng biết**. Suy diễn từ tên feature là bịa |
| Trong [1]/[2] — Core hay Module | Đặt sai chỗ thì hoặc Core mang theo thứ không ai dùng, hoặc mỗi dự án dựng lại một bản |
| Trước [6] — chốt bàn giao | Viết tài liệu cho tính năng chưa qua review là viết tài liệu cho thứ còn sắp đổi |
| Mọi lệnh git ghi, ở bất kỳ bước nào | [`CLAUDE.md`](CLAUDE.md) §1 — cưỡng chế bằng `permissions.deny` và hook `PreToolUse`, không phải bằng câu văn |

Ngoài ra `design-expert` **luôn dừng xin xác nhận riêng trước khi xuất bản ra định dạng ngoài**, kể cả đang chạy chuỗi stage tự động — vì bên trong repo thì hoàn tác được, ra ngoài thì không.

### Luồng rút gọn

Không phải feature nào cũng đi hết sáu bước:

- **Sửa lỗi nhỏ trong một module**: [3] → [4]. Không cần [1], [2], [5].
- **Feature kết luận thuộc Core, hoặc thay đổi chạm `Core/`**: **luôn** qua `architect` **trước** [3], và bắt buộc có [5] sau đó.
- **Chỉ đổi giao diện, không đổi hành vi**: [2] → [3] FE → [5] nếu chạm Core FE.
- **Khi chưa có `src/`**: phần lớn việc dừng ở [1], [2] và `architect` — đó là lúc quyết định kiến trúc rẻ nhất để đưa ra và rẻ nhất để đảo. Giai đoạn hiện tại của repo đọc ở `docs/README.md` §Trạng thái repo, không khai ở đây. Điều kiện chuyển giai đoạn và danh sách chỗ phải lật nhãn: `docs/adr/0030-dieu-kien-chuyen-giai-doan-2.md`; nhãn trong `.claude/` và `README.md` gốc do **phiên chính** lật, theo đúng danh sách đó.

---

## 4. Vì sao tách vai trò

Ba ranh giới dưới đây là **toàn bộ lý do** bộ này có nhiều agent thay vì một.

### Người viết code không tự nghiệm thu

Để viết được một hàm, người viết phải dựng trong đầu một mô hình về những gì có thể xảy ra. Test do chính người đó viết sau đấy sẽ kiểm **đúng cái mô hình ấy** — kể cả những chỗ mô hình thiếu.

Kết quả là bộ test xanh rực nhưng phủ đúng những tình huống tác giả đã nghĩ tới, và không phủ tình huống nào tác giả chưa nghĩ tới — tức là không phủ đúng những chỗ có bug.

`backend-expert`/`frontend-expert` **vẫn** tự viết test cho phần mình viết. `test-engineer` bắt đầu ở chỗ họ dừng.

Hệ quả cho cách vận hành: **`core-reviewer` và `test-engineer` không nhận bản tóm tắt từ agent vừa viết code.** Nhận tóm tắt thì họ thừa hưởng luôn mô hình thiếu sót của người kia — và đó đúng là thứ hai vai trò này sinh ra để chống.

### Người kiểm không có quyền sửa

`core-reviewer` **không được cấp công cụ ghi file**. `test-engineer` có quyền ghi nhưng phạm vi là **file test và chỉ file test**.

Đây không phải hình thức. Người kiểm có quyền sửa sẽ luôn có đường thoát dễ hơn việc báo cáo: sửa nhanh một chỗ rồi đi tiếp, hoặc nới một test cho nó xanh. Nới một test để nó xanh là cách biến một bug **đã bị phát hiện** thành một bug **đã được ghi nhận là đúng** — kể từ đó không ai tìm nó nữa.

### Người quyết kiến trúc tách khỏi người thi công

`architect` **không sửa code sản phẩm**. Người đang thi công một việc luôn có áp lực chọn phương án đi tiếp được ngay; người không phải thi công thì không.

Và `architect` **được quyền nói không**. Một đề xuất bị từ chối kèm lý do rõ ràng là một lượt làm việc thành công, không phải thất bại.

---

## 5. Nguyên tắc thiết kế bộ agent

### Tri thức không bao giờ chép vào `.claude/`

Mỗi file agent có một **bảng định tuyến**: mỗi dòng là *"đang làm gì → đọc file nào"*. Một dòng, không kèm tóm tắt nội dung file.

Phép thử ([`CLAUDE.md`](CLAUDE.md) §2): **`.claude/` không được chứa câu nào có thể trở thành SAI khi code thay đổi.**

Hệ quả cứng: file trong `.claude/` **không được chứa code block ngôn ngữ lập trình**. Code mẫu là tri thức. Cổng `check-docs.sh` bắt việc này.

Bốn lý do đã trả giá thật ở dự án tiền nhiệm nằm ở mục *Vì sao* cuối file này, phần §3 — lý do thứ ba là lý do đáng nhớ nhất.

### Agent đọc theo bảng định tuyến, không đọc cả `docs/`

Corpus đầy đủ đủ lớn để **giết một lượt review trước khi nó kết luận được gì**. Ở dự án tiền nhiệm, corpus bắt buộc từng lên tới ~780 KB; ba lượt review liên tiếp chết giữa chừng, một lượt còn để lại lỗi cố ý trong code. Trỏ đường thay vì chép giữ corpus ở mức ~264 KB.

Vì vậy mỗi bảng định tuyến cố ý **ngắn**, và mỗi dòng dẫn tới **đúng một** file. Chủ đề không có trong bảng → tra `docs/README.md` rồi mở đúng một file.

Bảng tách hai phần theo tiêu đề mục — **Bộ luật** (agent mở theo việc đang làm; tổng cỡ có ngưỡng, cổng `check-docs.sh` §23 canh) và **Tra cứu** (mở đúng một file khi chủ đề chạm tới; không cộng vào ngưỡng). Ngưỡng và cách đo: luật D36 ở `docs/RULES.md`. Phần "vì sao, bẫy, ví dụ mở rộng" của mỗi file luật nằm ở file lý do cùng tên trong `docs/wiki-core/` — agent thi công chỉ mở nó khi cần lý do.

### Một lượt review một phạm vi

`core-reviewer` soát **BE hoặc FE, không bao giờ cả hai** trong một lượt. Cùng lý do trên.

### Mọi báo cáo phải nói mình bỏ sót gì

Báo cáo của `core-reviewer` bắt buộc có mục **Lỗ mù**. Báo cáo của `test-engineer` bắt buộc nêu ca biên đã cân nhắc và cố ý bỏ qua. Tài liệu UnitTest của `tech-writer` bắt buộc có mục "chưa kiểm".

Một báo cáo không nói mình bỏ sót gì sẽ được đọc như thể nó phủ hết — và đó là cách một lượt review tạo ra **cảm giác an toàn giả**.

### Bàn giao bằng đường dẫn, không bằng tóm tắt

Mọi bàn giao giữa agent đều gửi **đường dẫn tới file gốc**, không gửi bản tóm tắt nội dung. Bản tóm tắt là một bản sao, và bản sao thì lệch.

---

## 6. Skill

Skill là điểm vào gọi bằng `/<tên>`. Mỗi skill là một quy trình đã đóng gói: nó biết đọc file nào, gọi agent nào, và dừng lại hỏi ở đâu — để người dùng không phải nhớ trình tự.

**Danh sách thật đọc bằng lệnh, đừng tin bảng dưới đây là đầy đủ:**

```bash
ls .claude/skills
```

| Skill | Dùng khi | Gọi agent nào |
| --- | --- | --- |
| `/feature-kickoff` | Bắt đầu một feature — điểm vào duy nhất, tự điều phối các bước | điều phối cả bộ |
| `/ba-story` | Chuyển yêu cầu thô thành spec nghiệp vụ | `ba-analyst` |
| `/core-review` | Soát phần Core sau khi vừa sửa nó | `core-reviewer` |
| `/arch-check` | Soát tuân thủ kiến trúc và tính nhất quán của luật | — |
| `/adr-new` | Ghi lại một quyết định kiến trúc | `architect` |
| `/audit-log` | Ghi postmortem sau một sự cố | — |
| `/doc-sync` | Dò lệch trong `docs/` mà cổng không bắt được | — |
| `/review-doc` | Review một thay đổi tài liệu | — |

🛑 **Không viết tên skill vào tài liệu trước khi skill đó tồn tại.** Một bảng liệt kê skill chưa có là đúng khuôn sai mà [`CLAUDE.md`](CLAUDE.md) §4 cấm: mô tả thứ không tồn tại, và không có gì báo.

> Bảng này từng ghi *"hiện chưa có skill nào"* — đúng lúc viết, sai vài giờ sau đó khi skill được thêm. Đó là lý do cột danh sách thật là **lệnh**, không phải văn bản. Xem [`CLAUDE.md`](CLAUDE.md) §6.

### Skill CHƯA có — cố ý hoãn

Nhóm skill **sinh code** (`/core-new-module`, `/core-new-usecase`, `/core-new-entity`, `/fe-new-feature`) chưa được viết.

Lý do: một skill scaffold về bản chất là máy sao chép có sửa tên — **nó cần một bản gốc**: một module mẫu đã chạy được. Viết trước là viết theo tưởng tượng về một thứ chưa tồn tại, và sản phẩm sẽ là skill sinh ra code không build được mà không có đáp án đúng nào để đối chiếu khi đi sửa.

> 📖 Module mẫu dựng ở đâu, skill scaffold viết ở đâu và khi nào đưa về Core: đọc `docs/adr/0032-module-mau-o-du-an-ha-nguon.md`

### Kích hoạt agent không qua skill

- **Theo `description`**: mỗi file agent khai rõ khi nào dùng nó, harness dựa vào đó để tự chọn.
- **Gọi thẳng**: nêu đích danh tên agent trong yêu cầu.
## 7. Thêm một agent mới

1. **Kiểm tra vai trò đó chưa thuộc về agent nào.** Hai agent chồng vai là hai agent sẽ đưa ra kết luận khác nhau cho cùng một việc, và không có cách nào biết nghe ai. Mở rộng một agent có sẵn thường đúng hơn thêm một agent mới.
2. **Tạo `.claude/agents/<tên>.md`** theo khuôn chung: frontmatter (`name`, `description`, `tools`, `model: inherit`), rồi các mục *Vai trò* → *STEP -1 Resolve root* → *bảng định tuyến* → *Bàn giao* → *Dừng lại và hỏi* → *Lệnh & công cụ* → *Trước khi coi việc là xong* → *Ngôn ngữ*.
3. **Cân nhắc kỹ danh sách `tools`.** Đây là ranh giới thật, không phải lời khuyên: agent kiểm tra thì **không cấp công cụ ghi**. Cấp rồi thì nó sẽ dùng.
4. **Viết bảng định tuyến chỉ trỏ đường.** Mỗi dòng một file, không kèm tóm tắt. Đang viết câu thứ hai giải thích *nội dung* một file `docs/` → dừng, đó là vi phạm §3.
5. **Viết mục "dừng lại và hỏi" cụ thể cho vai trò đó.** Một danh sách chung chung sao chép từ agent khác không có tác dụng — nó không mô tả đúng những ranh giới mà agent này thật sự gặp.
6. **Cập nhật file này**: bảng §2, sơ đồ "ai gọi ai", và luồng feature §3 nếu agent mới nằm trong luồng.
7. **Cập nhật mục bàn giao của những agent sẽ giao việc cho nó** — bàn giao là hai chiều, khai một chiều thì không ai gọi tới.
8. **Chạy cổng**: `bash .claude/check-docs.sh` — hook `Stop` cũng chạy nó ở cuối lượt, nên bước này là để thấy đỏ sớm, không phải để cổng có chạy hay không.

Bỏ bước 6 hoặc 7 là cách phổ biến nhất để có một agent tồn tại nhưng không bao giờ được gọi.

---

## 8. Giới hạn đã biết của bộ agent này

Mục này bắt buộc có, và bắt buộc trung thực. Một tài liệu chỉ mô tả bộ agent làm được gì sẽ được đọc như thể nó đảm bảo những điều đó.

### "PROACTIVELY" là niềm tin — nhưng một phần đã được cưỡng chế

Nhiều agent khai trong `description` rằng nên được dùng "PROACTIVELY" sau một sự kiện nào đó. Cơ chế đó **phụ thuộc vào việc agent nhớ gọi**: không có gì chặn một lượt kết thúc mà bỏ qua bước đó — không lỗi, không cảnh báo, không dấu vết. Nó chỉ đơn giản là không xảy ra.

**Một phần đã có hook canh**, xem [`CLAUDE.md`](CLAUDE.md) §8:

| Việc | Hook | Làm gì |
| --- | --- | --- |
| Không chạy lệnh cấm | `PreToolUse` | Chặn trước khi lệnh chạy — lớp thứ hai sau `permissions.deny` |
| Chạy cổng tài liệu sau khi sửa `docs/`, `.claude/` hoặc `spec/` | `Stop` chạy cổng khi có tệp mới hơn lần xanh cuối | Cổng đỏ thì **chặn lượt**, lặp tới khi xanh |
| Gọi `core-reviewer` sau khi chạm Core | `PostToolUse` ghi file chạm Core, `SubagentStop` ghi dấu review, `Stop` so dấu với file đã sửa | **Chặn phiên đã chạm Core** mỗi lần kết thúc lượt mà không còn agent nền nào chạy, tới khi có lượt review mới hơn — **chỉ khi có `src/`** |
| Chạy cổng build/test sau khi sửa `src/` | `Stop` | **Nhắc**, không chặn — CI là nơi chặn |

Hook do harness chạy, không do model quyết định — nó không quên. Cơ chế ba lớp của `core-reviewer` và lối thoát: [`CLAUDE.md`](CLAUDE.md) §8.

**Nhưng phần còn lại vẫn là niềm tin, và đừng nhầm hai thứ:**

- Hook chặn tới khi **có một lượt** `core-reviewer` kết thúc sau lần sửa. Nó không biết lượt đó soát đúng phạm vi hay chưa — một lượt soát FE cũng ghi dấu cho thay đổi BE — và không buộc ai **đọc kỹ** báo cáo hay **sửa** theo báo cáo.
- Người dùng vẫn bỏ qua được lớp chặn đó bằng cách tự xoá dấu — đó là quyết định có chủ ý, không phải lỗ hổng.
- Hook chỉ canh các việc trong bảng. Mọi "PROACTIVELY" khác — gọi `ba-analyst` khi thiếu spec, gọi `design-expert` khi thiếu spec màn hình, gọi `test-engineer` tìm ca biên — vẫn hoàn toàn phụ thuộc trí nhớ.
- Việc agent thi công có ghi dòng `CẦN CORE-REVIEW` hay không cũng là niềm tin. Lưới đỡ của nó là hook `Stop` ở trên — hook tự nhìn file đã sửa, không đọc báo cáo.
- Hook `PreToolUse` chỉ đọc chuỗi lệnh của một lời gọi công cụ. Đường vòng nó không thấy: các dòng nợ C4 ở `docs/DEBT.md`.

Đó là cải thiện thật, không phải giải pháp trọn vẹn.

### Cổng chỉ bắt được thứ máy kiểm được

`check-docs.sh` kiểm được: đường dẫn có tồn tại không, liên kết có resolve không, tuyên bố có kèm ngày không, file có chứa code block ngôn ngữ lập trình không.

Nó **không** đọc hiểu nội dung. Ba loại lỗi nó không bao giờ bắt được ([`CLAUDE.md`](CLAUDE.md) §8):

1. Văn xuôi mô tả thứ không tồn tại.
2. Sơ đồ và cây thư mục chép sai — khối không gắn ngôn ngữ không bị chặn.
3. Ngày đúng nhưng nội dung sai.

Gate là lưới **chặn hồi quy**, không phải chứng nhận chất lượng.

### Người kiểm cũng là một agent

`core-reviewer` bị ràng buộc không sửa code, nhưng nó vẫn có thể bỏ sót. Nó đọc theo bảng định tuyến, nên thứ nằm ngoài bảng thì nó không nhìn thấy. Mục **Lỗ mù** trong báo cáo tồn tại chính vì lý do này — đọc mục đó, đừng chỉ đọc phần kết luận.

### Ranh giới `tools` mạnh hơn ranh giới bằng câu văn

Ràng buộc *"chỉ ghi file test"* của `test-engineer` và *"không sửa code sản phẩm"* của `architect` là **ràng buộc bằng câu văn**, không phải bằng cấu hình — cả hai vẫn được cấp công cụ ghi vì chúng cần ghi ở nơi khác. Chỉ `core-reviewer` là ràng buộc thật, vì nó không được cấp công cụ ghi.

Chỗ nào ép được bằng `tools` hoặc bằng `permissions.deny` thì ép; chỗ nào không, biết rằng nó chỉ được tuân khi agent nhớ.

### Luồng sáu bước ở §3 chưa chạy trọn lần nào

Một phần bộ agent **đã** chạy trên code thật: `architect`, `backend-expert`, `frontend-expert`, `test-engineer` và `core-reviewer` đã đi qua nhiều lượt dựng và soát Core.

Chưa chạy: **trọn vẹn sáu bước trên một feature nghiệp vụ**. Nhánh `ba-analyst` → `spec/<feature>/` → thi công → `tech-writer` chưa có lượt nào — kiểm bằng lệnh, đừng tin câu này:

```bash
ls spec/            # chỉ có README.md và _template thì nhánh đó chưa chạy
```

Nên các bước [1] và [6] vẫn là **thiết kế**; chỗ nào của chúng không hoạt động thì hiện chưa ai biết.

---

## 9. Đọc thêm

- [`CLAUDE.md`](CLAUDE.md) — luật toàn repo; lý do của từng luật ở mục *Vì sao* cuối file này
- [`../docs/README.md`](../docs/README.md) — mục lục cấp cao nhất, đường vào duy nhất tới mọi tri thức
- [`../docs/RULES.md`](../docs/RULES.md) — toàn bộ luật và cột "ép bằng gì"
- [`../docs/kien-truc-core-module.md`](../docs/kien-truc-core-module.md) — ranh giới Core ↔ Module

**Khu nào của `docs/` giữ chủ đề nào — cố ý không liệt ở đây.** Bản liệt kê như vậy mục ruỗng ngay lần `docs/` tách hoặc gộp một file chủ, mà không có gì báo. Đường vào duy nhất là `docs/README.md`; đường tắt theo chủ đề nằm ở bảng định tuyến trong từng file `agents/*.md`.

---

## 10. Ngôn ngữ

Toàn bộ `.claude/` viết bằng **tiếng Việt**. Định danh kỹ thuật — tên agent, tên công cụ, tên file — giữ nguyên tiếng Anh.

---

## Vì sao — lý do của các luật trong CLAUDE.md

> [`CLAUDE.md`](CLAUDE.md) được nạp ở mọi phiên và mọi subagent, nên nó chỉ giữ câu luật. Lý do, chuyện đã trả giá ở dự án tiền nhiệm, ví dụ và cơ chế bên trong của hook nằm ở đây, chia theo số mục của `CLAUDE.md`. Đọc khi cần hiểu một luật, và trước khi sửa nó.

### §1 — Git

- `git restore` nằm trong danh sách cấm vì nó là bản thay thế hiện đại của `git checkout -- <file>` và xoá thay đổi working tree **không hoàn tác được**.
- Bảng cấm và hai dòng liệt kê ngay dưới nó là **đầu vào của cổng**, không phải văn xuôi: `check-docs.sh` §1 đọc chính chúng rồi đối chiếu từng lệnh với `permissions.deny`, và kiểm đủ ba dạng cho mọi mục cấm lệnh.
- Vì sao cần đủ ba dạng: dạng `Bash(…)` không khớp lệnh chạy qua công cụ PowerShell hay lệnh mang tiền tố `rtk` — thiếu một dạng là lệnh lọt qua đúng đường đó.
- Mục chặn **công cụ ghi tệp** vào thư mục trạng thái khai theo tên từng công cụ vì nó không nhận một chuỗi lệnh nào. Đường qua lệnh shell vào thư mục đó vẫn hở, và hở đó là một dòng nợ C4 ở `docs/DEBT.md`.
- Vì sao cần lớp chặn thứ hai: `permissions.deny` chỉ so tiền tố của cả chuỗi lệnh. Hook `PreToolUse` gắn cho công cụ Bash và PowerShell, đọc danh sách tiền tố cấm từ chính `permissions.deny` — không giữ danh sách riêng — rồi tách chuỗi lệnh thành từng đoạn, bóc tiền tố bọc và khớp từng đoạn. Lệnh cấm đứng sau `&&`, mang tiền tố `rtk`, hay chạy qua PowerShell đều bị chặn; lệnh chỉ *nhắc tới* chuỗi cấm, như tìm chữ trong file, thì được cho qua.
- Hook không phân tích được một lệnh thì không chặn vì lỗi đó, và ghi lý do vào tệp `pretool-error.log` trong thư mục trạng thái.

### §2 — Ranh giới `.claude` ↔ `docs`

- Ba khu, đủ chi tiết: `.claude/` giữ **quy trình và ràng buộc** — agent nào tồn tại, làm gì, đọc file nào, bàn giao ra sao, bị cấm gì — cùng cấu hình harness. `docs/` giữ **quy tắc**: kiến trúc, quy ước code, hợp đồng API, schema, giao diện. `spec/` giữ **nghiệp vụ** theo từng feature: `spec/<feature>/business-rules.md`, `spec/<feature>/ui-spec.md`. Ngoại lệ "tài liệu VỀ chính hệ thống agent" là loại tài liệu nói agent nào tồn tại, nạp tri thức từ đâu, kích hoạt thế nào.
- Ví dụ áp phép thử "câu này có thể thành SAI khi code đổi không":

| Câu | Code đổi thì có sai không? | Thuộc |
| --- | --- | --- |
| "Không chạy lệnh git ghi" | Không | `.claude` ✓ |
| "Sửa envelope thì đọc `docs/quy-uoc/be-api-controller.md` trước" | Không (chỉ sai nếu **doc** đổi chỗ) | `.claude` ✓ |
| "Xong việc chạm Core thì gọi `core-reviewer`" | Không | `.claude` ✓ |
| "Handler trả `Result<T>`, lỗi khai qua `ErrorDescriptor`" | **Có** | `docs` |
| "`Core/` có 5 project" | **Có** | `docs` |
| "Màu cảnh báo là `#965e08`" | **Có** | `docs` |

- Ví dụ ngôn ngữ lập trình bị cấm trong code block: `csharp`, `typescript`, `scss`, `sql`, `json`. Danh sách ngôn ngữ **được phép** không chép ra đây vì bản chép tay sẽ lệch — lệnh đọc nó nằm ở `CLAUDE.md` §2. Đại ý: lệnh chạy, mẫu báo cáo, khối placeholder, và khối không gắn ngôn ngữ (sơ đồ cây, ASCII).

### §3 — Chiều cập nhật

Đây là luật chống tái phát. Bốn lý do đã trả giá thật ở dự án tiền nhiệm:

1. **Hai nguồn thì chúng sẽ lệch nhau.** Một file luật khẳng định *"cả 5 project đã tồn tại"* trong khi chưa có project nào; một đoạn mẫu rate limit dùng sai overload kèm lý do sai, và code chép y theo nên mang nguyên lỗi. **Rule sai không nằm yên — nó sinh ra code sai.**
2. **Bản sao không bao giờ được sửa cùng lúc.** Một recipe concurrency sai provider tồn tại **song song** ở hai chỗ. Sửa một nơi không chạm nơi kia.
3. **Agent không "thấy" conflict — nó im lặng dùng bản sao.** Đọc file agent xong nó đã có câu trả lời tự tin, đầy đủ, có code mẫu, nên **không bao giờ mở `docs/`**. Lỗi loại này không tự lộ ra, và không test nào bắt được.
4. **Chép nội dung làm agent chết vì cạn context.** Số đo nằm ở §5 của file này, mục *Agent đọc theo bảng định tuyến*.

- Sửa một luật viết sai thì chỉ ghi bản mới, vì bản cũ đã nằm trong lịch sử git.
- Ví dụ dòng trỏ đường đúng dạng chuẩn:

```markdown
> 📖 Envelope & error → HTTP: đọc `docs/quy-uoc/be-api-controller.md`
```

- Chọn phần cho dòng trỏ tới file chủ mới: *Bộ luật* nếu agent phải tuân file đó ở mọi việc, *Tra cứu* nếu chỉ mở khi chủ đề chạm tới. "Vào agent cần nó" nghĩa là không thêm vào mọi agent cho đủ bộ. Hai mục lục đưa agent tới chủ đề mới là `docs/README.md` và `docs/wiki-core/README.md`.

### §4 — Nhãn trạng thái

- Ngoại lệ "một câu khai thay bảng" của nhãn `🚧` tồn tại vì ép điền bảng cho một file chưa ai mở source ra so chỉ sinh ra những dòng "có thật" bịa, đúng khuôn sai mà §4 cấm. Nó **không** phải đường để né bảng khi đã có thứ đối chiếu được.
- "Chỉ lật khi có người mở đúng file đó" loại mọi sự kiện thay thế: một quyết định chuyển giai đoạn, một lần `src/` xuất hiện, một đợt build xanh — không cái nào cho phép lật nhãn thay cho việc đối chiếu. `verified: chua-doi-chieu` vì thế giữ nguyên kể cả khi `src/` đã tồn tại và build xanh; đóng dấu ngày cho một file chưa ai mở source ra so mới đúng là khuôn sai §4 cấm.
- Vì sao cấm tuyệt đối việc sửa mô tả cho khớp rồi đánh dấu xong: ở dự án tiền nhiệm, một đợt rà soát tìm ra **7 ca** cùng khuôn này — `"FIXED"` khi giá trị chưa hề vào code, `"Đã bật"` cho một hằng số chỉ tồn tại trong đúng câu nói nó tồn tại, `"✅ Xong"` cho năm mục chưa làm. Đây là dạng sai đắt nhất: nó không gây lỗi biên dịch, không bị test bắt, và nhãn "đã xong" được thiết kế để **không ai kiểm lại**.

### §5 — Một nguồn

- **Không "giữ cả hai cho chắc"** — đó là cách một repo có thể có bốn sơ đồ đặt tên project và bốn nguồn mô tả database nói ngược nhau.
- Sửa bằng cách gỡ một bản vì hai bản đã đồng bộ sẽ lệch lại — người sửa chỉ sửa một, và không có gì báo. Đồng bộ là hoãn vấn đề; gỡ một bản mới là giải nó.
- Luật này có cổng: sổ `docs/OWNERSHIP.md` khai *nội dung nào thuộc file nào*, và `check-docs.sh` §15 đọc chính sổ đó để kiểm. Sổ là **đầu vào của cổng**, nên nó không lệch khỏi thứ nó ép được. Câu văn xuôi chép nguyên văn sang file khác thì §24 bắt (luật D37) — không cần đăng ký vào sổ.
- "Định nghĩa mới" cần tra sổ là, ví dụ, một catalog, một bảng ánh xạ, một chữ ký kiểu.

### §6 — Thứ đếm được bằng lệnh

Bảng liệt kê tay sẽ luôn mục ruỗng. Ở dự án tiền nhiệm, một đợt rà tìm ra **7 chỗ đếm sai** cùng lúc, và một bảng "9 chỗ hardcode màu" sai 4 trong 7 dòng đồng thời bỏ sót 2 dòng đúng.

### §7 — Nguồn giao diện

"Tham chiếu giao diện" gồm layout, câu chữ, token, trạng thái component, ảnh màn hình. Quy tắc riêng của khu Design nằm ở `docs/Design/CLAUDE.md` vì đó là tri thức — thuộc `docs/`, đúng chỗ.

### §8 — Cổng và hook

- Cổng chỉ có tác dụng khi harness không hỏi lại giữa chừng: một cổng bị prompt chặn là một cổng, trên thực tế, không ai chạy.
- Cổng BE và cổng FE là **hai job CI** khai trong `.github/workflows/docs-gate.yml`, bật theo điều kiện thư mục `src/BE` / `src/FE` có mặt trong cây CI checkout — tức có trong **git**, không phải trên đĩa máy ai đó.
- Cơ chế bên trong từng hook, ngoài bảng ở §8 của file này:
  - `PreToolUse` không ghi dấu, không chạy cổng.
  - `PostToolUse` chạy sau mỗi lần ghi file bằng Edit / Write / NotebookEdit **và sau mỗi lệnh Bash hoặc PowerShell**. Chỉ khi có `src/`, nó ghi dấu "phiên này đã sửa `src/`", và ghi file chạm Core vào nhật ký chung kèm cờ "phiên này đã chạm Core". Lệnh shell không bị đoán từ chuỗi lệnh — hook hỏi hệ thống tệp xem file nào đổi sau lần quét trước của phiên. Nó không chạy cổng.
  - `SubagentStop` không chặn gì. Payload không khai tên agent thì **không** ghi dấu, và ghi lý do vào `subagent-stop-error.log`.
  - `Stop`: còn agent nền thì hook không làm gì vì phiên đang chờ, chưa dừng. Cổng **không chạy được** (mã thoát 2 — sai thư mục, cây repo thiếu) thì chặn **một lần** để nói ra rồi thả — đó không phải vi phạm tài liệu, và chặn lặp thì thành vòng không đáy. Không đọc được khối `core-paths` thì chặn một lần mỗi phiên để nói ra. Phiên không chạm Core thì không bị chặn vì việc của phiên khác.
  - Hook đọc thẳng khối `core-paths`, không giữ bản sao.
- Thư mục trạng thái là thư mục `.state` bên trong `.claude`, bị gitignore. Dấu riêng của từng phiên nằm trong thư mục con theo `session_id`; nhật ký chạm Core và dấu review là chung, vì review phủ cả cây.
- Chặn cứng ở lớp 3 cần hook `SubagentStop` vì không có gì ghi dấu review thì không lượt nào thoát được.
- Lớp chặn `core-reviewer` bật theo `src/` vì vai của agent đó là *đối chiếu code thật với quy tắc*: khi chưa có code thì nó không có thứ gì để đối chiếu, và chặn lúc đó chỉ ép chạy agent mà không được gì.
- Nới luật để qua cổng là đúng hành vi §4 tồn tại để ngăn.
- Script hook không dùng `grep -P` và tự ép locale vì trên Git Bash với `LANG` rỗng, `grep -P` thoát lỗi mà **bên trong một hook thì lỗi đó không hiện ra đâu cả** — dấu đơn giản không được ghi và cổng không bao giờ chạy.
- Ví dụ cho ba loại lỗi cổng không bắt (danh sách ở §8 của file này): gỡ một trích dẫn chết làm cổng xanh, nhưng đoạn văn bên cạnh vẫn có thể đang tả một màn hình chưa ai xây; khối không gắn ngôn ngữ không bị §2 chặn, nên một cây thư mục sai từng lọt qua mọi luật cho tới khi có người đọc; một tuyên bố `✅ Xong` kèm ngày hợp lệ vẫn qua được cổng kể cả khi việc đó chưa làm.

### §9 — Ba khoá phân loại

- Ba khoá biến ba câu hỏi phải-đọc-mới-biết thành ba câu **máy đọc được**.
- `kind: tham-chieu` quan trọng nhất vì tài liệu mô tả lộ trình của một dự án khác mà bị nhầm thành luật sẽ khiến agent báo *"doc yêu cầu X, code không có X"* cho những X chưa bao giờ là luật của repo này. Đã xảy ra thật ở dự án tiền nhiệm.
- Cổng chỉ nhận `khong-ap-dung` theo điều kiện máy kiểm được vì lý do miễn trừ phải là một sự thật, không phải một câu tự nhận. Đây là chỗ dễ lạm dụng nhất của cả ba khoá: dán `khong-ap-dung` lên một file khó đối chiếu là cách nhanh nhất để làm con số đẹp lên mà không kiểm gì cả.
- Neo được thì máy kiểm được; không neo thì không ai kiểm. Chuỗi neo vào `src/` là tên thành viên, tên khoá, hoặc một đoạn trích đủ duy nhất. Vì sao chuỗi chứ không số dòng: ADR-0046, nơi `CLAUDE.md` §9 trỏ tới.

### §10 — Ngôn ngữ

"Định danh kỹ thuật" gồm tên class, tên file, tên package, thuật ngữ chuẩn ngành — giữ nguyên tiếng Anh, không dịch.

### §11 — Điều phối agent

Model đang ghim của từng agent đọc bằng lệnh, không chép ra đây: `grep -m1 '^model:' .claude/agents/*.md`.
