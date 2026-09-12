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
| `settings.json` | Cấu hình harness — `permissions.allow` / `permissions.deny`, nơi lệnh git ghi bị **cưỡng chế bằng máy** |
| `check-docs.sh` | Cổng tài liệu. **Không phải chạy tay** — hook `Stop` và CI đều gọi nó |
| `hooks/` | Script do harness chạy: `on-edit.sh` ghi dấu file đã sửa, `on-stop.sh` chạy cổng cuối lượt |
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

Ba chiều mũi tên đáng chú ý:

- **`architect` nhận từ mọi phía.** Bất kỳ agent nào gặp một quyết định kiến trúc đều dừng và chuyển sang đó. Không có đường vòng.
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
        │
        ▼
[4] test-engineer — tìm ca biên hai agent trên bỏ sót
        │
        ├──── 🛑 DỪNG: test đỏ vì CODE SAI → trả về [3], KHÔNG sửa code cho test xanh
        │
        ▼
[5] core-reviewer — chỉ khi việc chạm Core; một lượt = một phạm vi (BE hoặc FE)
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
| Mọi lệnh git ghi, ở bất kỳ bước nào | [`CLAUDE.md`](CLAUDE.md) §1 — cưỡng chế bằng `permissions.deny`, không phải bằng câu văn |

Ngoài ra `design-expert` **luôn dừng xin xác nhận riêng trước khi xuất bản ra định dạng ngoài**, kể cả đang chạy chuỗi stage tự động — vì bên trong repo thì hoàn tác được, ra ngoài thì không.

### Luồng rút gọn

Không phải feature nào cũng đi hết sáu bước:

- **Sửa lỗi nhỏ trong một module**: [3] → [4]. Không cần [1], [2], [5].
- **Thay đổi chạm `Core/`**: bắt buộc qua `architect` **trước** [3], và bắt buộc có [5] sau đó.
- **Chỉ đổi giao diện, không đổi hành vi**: [2] → [3] FE → [5] nếu chạm Core FE.
- **Giai đoạn chưa có `src/`** (trạng thái hiện tại của repo — đọc `docs/README.md` §Trạng thái repo): phần lớn việc dừng ở [1], [2] và `architect`. Đây là lúc quyết định kiến trúc rẻ nhất để đưa ra và rẻ nhất để đảo.

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

Bốn lý do đã trả giá thật ở dự án tiền nhiệm nằm ở [`CLAUDE.md`](CLAUDE.md) §3. Lý do đáng nhớ nhất: **agent không "thấy" conflict — nó im lặng dùng bản sao.** Đọc file agent xong nó đã có câu trả lời tự tin, đầy đủ, có code mẫu, nên không bao giờ mở `docs/`. Lỗi loại này không tự lộ ra, và không test nào bắt được.

### Agent đọc theo bảng định tuyến, không đọc cả `docs/`

Corpus đầy đủ đủ lớn để **giết một lượt review trước khi nó kết luận được gì**. Ở dự án tiền nhiệm, corpus bắt buộc từng lên tới ~780 KB; ba lượt review liên tiếp chết giữa chừng, một lượt còn để lại lỗi cố ý trong code. Trỏ đường thay vì chép giữ corpus ở mức ~264 KB.

Vì vậy mỗi bảng định tuyến cố ý **ngắn**, và mỗi dòng dẫn tới **đúng một** file. Chủ đề không có trong bảng → tra `docs/README.md` rồi mở đúng một file.

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

Lý do: một skill scaffold về bản chất là máy sao chép có sửa tên — **nó cần một bản gốc**. Repo chưa có `src/`, nên chưa có module mẫu nào đã chạy được để sao. Viết trước là viết theo tưởng tượng về một thứ chưa tồn tại, và sản phẩm sẽ là skill sinh ra code không build được mà không có đáp án đúng nào để đối chiếu khi đi sửa.

Chúng thuộc giai đoạn 2, sau khi module mẫu đầu tiên chạy được.

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
8. **Chạy cổng**: `bash .claude/check-docs.sh`.

Bỏ bước 6 hoặc 7 là cách phổ biến nhất để có một agent tồn tại nhưng không bao giờ được gọi.

---

## 8. Giới hạn đã biết của bộ agent này

Mục này bắt buộc có, và bắt buộc trung thực. Một tài liệu chỉ mô tả bộ agent làm được gì sẽ được đọc như thể nó đảm bảo những điều đó.

### "PROACTIVELY" là niềm tin — nhưng một phần đã được cưỡng chế

Nhiều agent khai trong `description` rằng nên được dùng "PROACTIVELY" sau một sự kiện nào đó. Cơ chế đó **phụ thuộc vào việc agent nhớ gọi**: không có gì chặn một lượt kết thúc mà bỏ qua bước đó — không lỗi, không cảnh báo, không dấu vết. Nó chỉ đơn giản là không xảy ra.

**Hai trường hợp nay đã được cưỡng chế bằng hook**, xem [`CLAUDE.md`](CLAUDE.md) §8:

| Việc | Trước | Nay |
| --- | --- | --- |
| Chạy cổng tài liệu sau khi sửa `docs/` hoặc `.claude/` | Agent phải nhớ | Hook `Stop` chạy, cổng đỏ thì **chặn lượt** |
| Gọi `core-reviewer` sau khi chạm Core | Agent phải nhớ | Hook `Stop` **chặn một lần** kèm yêu cầu gọi |

Hook do harness chạy, không do model quyết định — nó không quên.

**Nhưng phần còn lại vẫn là niềm tin, và đừng nhầm hai thứ:**

- Hook **buộc gọi** `core-reviewer`, nó không buộc ai **đọc kỹ** báo cáo hay **sửa** theo báo cáo.
- Hook chỉ canh hai sự kiện trên. Mọi "PROACTIVELY" khác — gọi `ba-analyst` khi thiếu spec, gọi `design-expert` khi thiếu spec màn hình, gọi `test-engineer` tìm ca biên — vẫn hoàn toàn phụ thuộc trí nhớ.
- Nhánh chặn của hook chấp nhận lý do "lượt này không cần review". Một agent muốn đi vòng thì vẫn đi vòng được; hook chỉ làm việc đó thành **một hành động có ý thức, phải nói ra**, thay vì một chỗ bị bỏ quên trong im lặng.

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

### Bộ agent này chưa chạy trên code thật

Repo đang ở giai đoạn chưa có `src/` — đọc `docs/README.md` §Trạng thái repo. Toàn bộ luồng ở §3 mới là **thiết kế**, chưa có lượt nào chạy hết sáu bước trên một feature thật. Chỗ nào của nó không hoạt động thì hiện chưa ai biết.

---

## 9. Đọc thêm

- [`CLAUDE.md`](CLAUDE.md) — luật toàn repo, mười mục
- [`../docs/README.md`](../docs/README.md) — mục lục cấp cao nhất, đường vào duy nhất tới mọi tri thức
- [`../docs/RULES.md`](../docs/RULES.md) — toàn bộ luật và cột "ép bằng gì"
- [`../docs/kien-truc-core-module.md`](../docs/kien-truc-core-module.md) — ranh giới Core ↔ Module

**Khu nào của `docs/` giữ chủ đề nào — cố ý không liệt ở đây.** Bản liệt kê như vậy mục ruỗng ngay lần `docs/` tách hoặc gộp một file chủ, mà không có gì báo. Đường vào duy nhất là `docs/README.md`; đường tắt theo chủ đề nằm ở bảng định tuyến trong từng file `agents/*.md`.

---

## 10. Ngôn ngữ

Toàn bộ `.claude/` viết bằng **tiếng Việt**. Định danh kỹ thuật — tên agent, tên công cụ, tên file — giữ nguyên tiếng Anh.
