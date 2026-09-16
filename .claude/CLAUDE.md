# CLAUDE.md — Luật toàn repo CoreAndSkill

> File này mô tả **quy trình và ràng buộc**. Mọi tri thức kỹ thuật nằm ở `docs/`.
> Nếu bạn thấy mình đang viết câu thứ hai giải thích *nội dung* của một file `docs/` ở đây — dừng lại, đó là lúc luật §3 đang bị vi phạm.

---

## 1. Git — đọc được, GHI thì không

**Mọi lệnh git làm THAY ĐỔI trạng thái repo là của người dùng, không phải của agent.**

| | Lệnh | Ai chạy |
| --- | --- | --- |
| ✅ **Được phép** | `git status`, `git diff`, `git log`, `git show`, `git blame` | Agent tự chạy thoải mái — chỉ đọc, không đổi gì |
| 🛑 **CẤM** | `add`, `commit`, `push`, `checkout`, `switch`, `restore`, `merge`, `rebase`, `reset`, `stash`, `branch`, `clean`, `cherry-pick`, `revert`, `rm`, `mv`, `am`, `apply`, `pull`, `tag`, `config`, `worktree`, `submodule`, `remote` | **Chỉ người dùng** |

Lệnh cấm này **được cưỡng chế bằng máy**, không phải bằng câu văn: khối `permissions.deny` trong [`settings.json`](settings.json) chặn thẳng. Cùng khối đó chặn thêm `dotnet ef database update`, `dotnet ef database drop`, `dotnet ef migrations remove`, `npm publish`, `pnpm publish`, `dotnet nuget push`, `docker push`.

> Các **dạng ghi của** `remote` bị chặn riêng từng cái: `git remote add`, `git remote rm`, `git remote remove`, `git remote set-url`, `git remote rename`, `git remote prune`, `git remote set-head`, `git remote set-branches`, `git remote update`. `git remote -v` chỉ đọc nên không bị chặn — đúng theo bảng trên.
>
> Dòng trên là **đầu vào của cổng**, không phải văn xuôi: §1 đọc chính nó rồi đối chiếu từng lệnh với `deny`.

Mỗi mục cấm **lệnh** trong `deny` có mặt ở **ba dạng**: `Bash(<lệnh>:*)`, `Bash(rtk <lệnh>:*)` và `PowerShell(<lệnh>:*)`. Dạng `Bash` không khớp lệnh chạy qua công cụ PowerShell hay lệnh mang tiền tố `rtk` — thiếu một dạng là lệnh lọt qua đúng đường đó. Cổng §1 kiểm đủ ba dạng cho mọi mục cấm lệnh.

> `deny` còn một loại mục **không** phải cấm lệnh: mục chặn **công cụ ghi tệp** nhắm vào thư mục trạng thái của harness (§8). Loại đó khai theo tên từng công cụ ghi tệp, không theo ba dạng trên — nó không nhận một chuỗi lệnh nào. Đường qua lệnh shell vào thư mục đó vẫn hở, và hở đó là một dòng nợ C4 ở `docs/RULES.md` §10.

**Lớp chặn thứ hai là hook `PreToolUse`** (gắn cho công cụ Bash và PowerShell, xem §8). `permissions.deny` chỉ so tiền tố của cả chuỗi lệnh; hook đọc danh sách tiền tố cấm từ chính `permissions.deny` — không giữ danh sách riêng — rồi tách chuỗi lệnh thành từng đoạn, bóc tiền tố bọc và khớp từng đoạn. Lệnh cấm đứng sau `&&`, mang tiền tố `rtk`, hay chạy qua PowerShell đều bị chặn; lệnh chỉ *nhắc tới* chuỗi cấm, như tìm chữ trong file, thì được cho qua. Hook không phân tích được một lệnh thì không chặn vì lỗi đó và ghi lý do vào tệp `pretool-error.log` trong thư mục trạng thái của harness (xem §8).

Bị hook chặn thì **đừng viết lại lệnh cho lọt qua** — làm theo đoạn "dừng lại và nói rõ" bên dưới.

> 📖 Đường vòng mà cả hai lớp chặn không bắt được: các dòng nợ C4 ở `docs/RULES.md` §10

Áp dụng cho **mọi** skill và subagent, không có ngoại lệ.

Nếu một việc cần lệnh git ghi để đi tiếp: **dừng lại và nói rõ cần chạy lệnh gì** — người dùng tự chạy rồi bảo agent tiếp tục. Không "xin phép rồi tự chạy".

> `git restore` nằm trong danh sách cấm vì nó là bản thay thế hiện đại của `git checkout -- <file>` và xoá thay đổi working tree **không hoàn tác được**.

---

## 2. Ranh giới `.claude` ↔ `docs` — phép thử kiểm được bằng máy

Repo có **ba** khu tri thức:

| Thư mục | Chứa gì | Không chứa gì |
| --- | --- | --- |
| **`.claude/`** | **Quy trình và ràng buộc**: agent nào tồn tại, làm gì, đọc file nào, bàn giao ra sao, bị cấm gì. Cấu hình harness. | Tri thức. Code mẫu. |
| **`docs/`** | **QUY TẮC**: kiến trúc, quy ước code, hợp đồng API, schema, giao diện. | Luật nghiệp vụ của một feature cụ thể. |
| **`spec/`** | **NGHIỆP VỤ** theo từng feature: `spec/<feature>/business-rules.md`, `spec/<feature>/ui-spec.md`. | Quy tắc kiến trúc/code. |

**Agent và skill luôn phải tuân thủ quy tắc trong `docs/`** — kể cả khi đang làm việc thuộc `spec/`.

### Phép thử — trả lời được bằng có/không

> ### `.claude/` không được chứa câu nào có thể trở thành **SAI** khi code thay đổi.

| Câu | Code đổi thì có sai không? | Thuộc |
| --- | --- | --- |
| "Không chạy lệnh git ghi" | Không | `.claude` ✓ |
| "Sửa envelope thì đọc `docs/quy-uoc/be-api-controller.md` trước" | Không (chỉ sai nếu **doc** đổi chỗ) | `.claude` ✓ |
| "Xong việc chạm Core thì gọi `core-reviewer`" | Không | `.claude` ✓ |
| "Handler trả `Result<T>`, lỗi khai qua `ErrorDescriptor`" | **Có** | `docs` |
| "`Core/` có 5 project" | **Có** | `docs` |
| "Màu cảnh báo là `#965e08`" | **Có** | `docs` |

**Hệ quả cứng:** file trong `.claude/` **không được chứa code block ngôn ngữ lập trình** (`csharp`, `typescript`, `scss`, `sql`, `json`). Code mẫu là tri thức.

Danh sách ngôn ngữ **được phép** không liệt kê ở đây — nó là giá trị đọc được bằng lệnh, và bản chép tay sẽ lệch:

```bash
grep -n 'case "$lang" in' .claude/check-docs.sh
```

Đại ý: lệnh chạy, mẫu báo cáo, khối placeholder, và khối không gắn ngôn ngữ (sơ đồ cây, ASCII).

### Ngoại lệ duy nhất — tài liệu VỀ chính hệ thống agent

Tài liệu mô tả *bản thân bộ agent/skill* (agent nào tồn tại, nạp tri thức từ đâu, kích hoạt thế nào) thuộc **`.claude/`**, vì đó là tài liệu của `.claude` — không phải tri thức về sản phẩm. Xem [`README.md`](README.md).

---

## 3. Chiều cập nhật — nội dung vào `docs`, CHỈ đường dẫn vào `.claude`

Đây là luật chống tái phát. Khi có thay đổi, tra bảng này **trước khi mở file**:

| Việc vừa xảy ra | Sửa ở | `.claude/` có đổi không |
| --- | --- | --- |
| Chốt quyết định kiến trúc mới | `docs/` | **KHÔNG** |
| Sửa/bổ sung quy ước kỹ thuật, code mẫu, giá trị token | `docs/` | **KHÔNG** |
| Ghi nhận hiện trạng, đóng một việc tồn đọng | `docs/` | **KHÔNG** |
| **Sửa một luật đã viết sai** | `docs/` — **chỉ bản mới**. Bản cũ nằm trong lịch sử git; bài học có cơ chế hỏng vào `docs/audit/`. Không kể lại bản trước trong file nội dung (luật D24) | **KHÔNG** |
| **File `docs/` đổi tên, đổi chỗ, bị xoá, hoặc tách ra** | `docs/` | **CÓ — chỉ sửa đường dẫn, không đụng nội dung** |
| **Thêm file `docs/` mới làm file chủ của một chủ đề** | `docs/` | **CÓ — chỉ thêm MỘT dòng đường dẫn vào bảng định tuyến của agent cần nó** |
| Thêm/bỏ agent hoặc skill | `.claude/` | CÓ |
| Đổi quy trình, bàn giao, ràng buộc thi hành | `.claude/` | CÓ |

**Không ô nào cho phép chép nội dung từ `docs/` sang `.claude/`.**

Dạng trỏ đường chuẩn trong `.claude/`: một dòng, không tóm tắt kèm.

```markdown
> 📖 Envelope & error → HTTP: đọc `docs/quy-uoc/be-api-controller.md`
```

Giới hạn của ô "thêm file chủ mới": **một dòng đường dẫn, vào đúng agent thật sự cần nó, vào đúng phần** — *Bộ luật* nếu agent phải tuân nó ở mọi việc, *Tra cứu* nếu chỉ mở khi chủ đề chạm tới (luật D36, cổng §23 canh ngưỡng cỡ của phần Bộ luật). Không thêm vào mọi agent cho đủ bộ, không kèm tóm tắt nội dung. Nếu chủ đề mới đã tới được agent qua một mục lục (`docs/README.md`, `docs/wiki-core/README.md`) thì **không thêm gì cả**.

### Vì sao — bốn lý do đã trả giá thật ở dự án tiền nhiệm

1. **Hai nguồn thì chúng sẽ lệch nhau.** Một file luật khẳng định *"cả 5 project đã tồn tại"* trong khi chưa có project nào; một đoạn mẫu rate limit dùng sai overload kèm lý do sai, và code chép y theo nên mang nguyên lỗi. **Rule sai không nằm yên — nó sinh ra code sai.**
2. **Bản sao không bao giờ được sửa cùng lúc.** Một recipe concurrency sai provider tồn tại **song song** ở hai chỗ. Sửa một nơi không chạm nơi kia.
3. **Agent không "thấy" conflict — nó im lặng dùng bản sao.** Đọc file agent xong nó đã có câu trả lời tự tin, đầy đủ, có code mẫu, nên **không bao giờ mở `docs/`**. Lỗi loại này không tự lộ ra, và không test nào bắt được.
4. **Chép nội dung làm agent chết vì cạn context.** Corpus mà một agent review bị buộc đọc từng lên tới **780 KB**; ba lượt review liên tiếp chết giữa chừng, một lượt còn để lại lỗi cố ý trong code. Trỏ đường thay vì chép giữ corpus ở mức **~264 KB**.

---

## 4. Tài liệu phải mô tả thứ CÓ THẬT — và phải dán nhãn trạng thái

Mọi tuyên bố về hiện trạng trong `docs/` phải mang **một** trong ba nhãn. Không nhãn = mặc định bị coi là chưa xác minh.

| Nhãn | Nghĩa | Bắt buộc kèm |
| --- | --- | --- |
| `✅ CÓ THẬT` | Đã đối chiếu với source | **Ngày đối chiếu** + `file:dòng` |
| `🚧 ĐÃ CHỐT — ĐANG THI CÔNG` | Quyết định xong, code chưa về | Bảng *"có thật hôm nay → sẽ thành"* |
| `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG` | Mới là dự kiến | — |

**Giai đoạn 1 của repo này chưa có `src/`.** Vì vậy gần như mọi mô tả kiến trúc đều mang `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG`. Đó là trạng thái đúng, không phải thiếu sót — đừng dán `✅ CÓ THẬT` cho thứ chưa ai viết.

> 📖 Điều kiện chuyển sang giai đoạn 2, ai lật nhãn trạng thái, chỗ nào phải lật: đọc `docs/adr/0030-dieu-kien-chuyen-giai-doan-2.md`

**Cấm tuyệt đối:** đóng một việc bằng cách sửa mô tả cho khớp mong muốn rồi đánh dấu là xong. Ở dự án tiền nhiệm, một đợt rà soát tìm ra **7 ca** cùng khuôn này — `"FIXED"` khi giá trị chưa hề vào code, `"Đã bật"` cho một hằng số chỉ tồn tại trong đúng câu nói nó tồn tại, `"✅ Xong"` cho năm mục chưa làm.

Đây là dạng sai đắt nhất: nó không gây lỗi biên dịch, không bị test bắt, và nhãn "đã xong" được thiết kế để **không ai kiểm lại**.

---

## 5. Một nội dung — một nguồn đối chiếu duy nhất

Mỗi nội dung có **đúng một** file giữ định nghĩa. Mọi file khác chỉ được trỏ tới nó.

Khi phát hiện hai file cùng mô tả một thứ: chọn một làm chủ, file kia rút còn một dòng trỏ đường. **Không "giữ cả hai cho chắc"** — đó là cách một repo có thể có bốn sơ đồ đặt tên project và bốn nguồn mô tả database nói ngược nhau.

> 🛑 **Sửa bằng cách gỡ một bản, KHÔNG bằng cách đồng bộ hai bản.** Hai bản đã đồng bộ sẽ lệch lại — người sửa chỉ sửa một, và không có gì báo. Đồng bộ là hoãn vấn đề; gỡ một bản mới là giải nó.

**Luật này nay có cổng.** Sổ đăng ký chủ quyền ở `docs/OWNERSHIP.md` khai *nội dung nào thuộc file nào*, và `check-docs.sh` §15 đọc chính sổ đó để kiểm. Sổ là **đầu vào của cổng**, nên nó không lệch khỏi thứ nó ép được. Câu văn xuôi chép nguyên văn sang file khác thì §24 bắt (luật D37) — không cần đăng ký.

Trước khi viết một định nghĩa mới — một catalog, một bảng ánh xạ, một chữ ký kiểu — mở sổ đó xem đã có chủ chưa. Có rồi thì trỏ đường; chưa có thì viết, rồi **thêm một dòng vào sổ**.

Tài liệu đã chết nhưng cần giữ để tra cứu: **không xoá, không để nguyên** — dán banner lịch sử ở **đầu file** gồm banner "TÀI LIỆU LỊCH SỬ" + bảng *"Trong file này → Thực tế hiện nay"* + trỏ về nguồn sống, và đổi `kind` thành `lich-su`.


---

## 6. Không chép vào tài liệu thứ đếm được bằng lệnh

Bảng liệt kê tay sẽ luôn mục ruỗng. Thay bằng **lệnh + tiêu chí PASS**.

Cấm chép: danh sách file vi phạm, số lượng test/gate/thành phần/project, danh sách "còn N chỗ hardcode".

Ở dự án tiền nhiệm, một đợt rà tìm ra **7 chỗ đếm sai** cùng lúc, và một bảng "9 chỗ hardcode màu" sai 4 trong 7 dòng đồng thời bỏ sót 2 dòng đúng.

---

## 7. Giao diện người dùng: `docs/Design/` là nguồn DUY NHẤT

Mọi tham chiếu về giao diện — layout, câu chữ, token, trạng thái component, ảnh màn hình — lấy từ `docs/Design/`. Không dựng prototype HTML song song, không giữ nguồn UI thứ hai.

Quy tắc riêng của khu Design nằm ở [`../docs/Design/CLAUDE.md`](../docs/Design/CLAUDE.md) — đó là tri thức, thuộc `docs/`, đúng chỗ.

---

## 8. Trước khi coi một việc là xong

**Giai đoạn 1 (hiện tại) — cổng theo thứ vừa sửa:**

| Vừa sửa gì | Cổng | Ai chạy |
| --- | --- | --- |
| bất kỳ `.md` nào trong `docs/`, `.claude/` hoặc `spec/` | `bash .claude/check-docs.sh` | Hook `Stop` tự chạy; CI chạy lại |
| script trong `.claude/hooks/` hoặc khối `hooks` của `settings.json` | `bash .claude/hooks/tests/run-tests.sh` | **Người sửa chạy tay** — không hook nào chạy nó; CI có job chạy |

Cả hai cổng chạy qua **công cụ Bash** (Git Bash). Trên máy Windows, `bash` gọi từ công cụ PowerShell có thể trỏ vào WSL và không chạy được, nên `permissions.allow` chỉ khai lệnh cổng ở dạng `Bash(…)`.

Giai đoạn 2 (khi có `src/`) thêm cổng BE và cổng FE. Job CI của hai cổng đó khai trong `.github/workflows/docs-gate.yml` và chỉ chạy khi có `src/BE` / `src/FE` — ở giai đoạn 1 chúng không chạy gì, nên đừng nhắc tới chúng như thể đang canh. Điều kiện chuyển giai đoạn: `docs/adr/0030-dieu-kien-chuyen-giai-doan-2.md`.

### Cổng chạy tự động — không phụ thuộc ai nhớ

Hook trong [`settings.json`](settings.json) cưỡng chế việc này, do harness chạy:

| Hook | Khi nào | Làm gì |
| --- | --- | --- |
| `PreToolUse` | Trước mỗi lệnh Bash hoặc PowerShell | Chặn lệnh khớp mục cấm của `permissions.deny` sau khi tách đoạn và bóc tiền tố bọc — xem §1. Không ghi dấu, không chạy cổng |
| `PostToolUse` | Sau mỗi lần ghi file bằng Edit / Write / NotebookEdit, **và sau mỗi lệnh Bash hoặc PowerShell** | Ghi dấu nếu file thuộc `docs/`, `.claude/`, `spec/` hoặc `src/` và không bị gitignore. Với lệnh shell thì ghi dấu **không điều kiện** — kể cả lệnh chỉ đọc: phân biệt đọc với ghi cần phân tích dòng lệnh, và phân tích sai theo chiều "không phải lệnh ghi" làm cổng biến mất im lặng. Khi có `src/`: ghi thêm dấu "đã sửa `src/`" và danh sách file chạm Core. **Không** chạy cổng ở đây |
| `SubagentStop` | Khi một lượt `core-reviewer` kết thúc | Ghi dấu thời điểm review `core-reviewed`. Không chặn gì. Payload không khai tên agent thì **không** ghi dấu, ghi lý do vào `subagent-stop-error.log` |
| `Stop` | Khi agent kết thúc lượt | Có dấu thì chạy cổng tài liệu; cổng đỏ → **chặn lượt**, lặp tới khi xanh. Cổng **không chạy được** (mã thoát 2 — sai thư mục, cây repo thiếu) → chặn **một lần** để nói ra rồi thả: đó không phải vi phạm tài liệu, và chặn lặp thì thành vòng không đáy. **Chỉ khi có `src/`**: Core đã đổi mà chưa có lượt review mới hơn → **chặn mỗi lần kết thúc lượt** (xem dưới); không đọc được khối `core-paths` → chặn một lần để nói ra; sửa `src/` → nhắc chạy cổng build/test, **không chặn** — CI là nơi chặn |

Mọi dấu và log hook ghi ra nằm trong thư mục trạng thái của harness — thư mục `.state` bên trong `.claude`, bị gitignore. Đó là trạng thái chạy, không phải bằng chứng để trích dẫn.

> 📖 Đường dẫn nào tính là chạm Core: khối `core-paths` trong `docs/kien-truc-core-module.md` — hook đọc thẳng khối đó, không giữ bản sao.

Nghĩa là: **bạn không thể kết thúc một lượt với cổng đang đỏ**, và khi đã có `src/` thì **không thể kết thúc lượt khi Core đã đổi mà chưa có lượt `core-reviewer` nào mới hơn**.

### `core-reviewer` sau khi chạm Core — ba lớp

| Lớp | Ai | Làm gì |
| --- | --- | --- |
| 1. Báo và gọi | Agent thi công → phiên chính hoặc `/feature-kickoff` | Agent thi công chạm Core thì kết thúc báo cáo bằng dòng `CẦN CORE-REVIEW: BE` và/hoặc `CẦN CORE-REVIEW: FE`, không tự gọi. Phiên chính hoặc `/feature-kickoff` đọc dòng đó rồi gọi `core-reviewer` — mỗi lượt một phạm vi, chỉ truyền phạm vi |
| 2. Ghi dấu | Hook `SubagentStop` | Lượt `core-reviewer` kết thúc → ghi dấu `core-reviewed` |
| 3. Chặn | Hook `Stop` | Có `src/`, lượt đã chạm Core, và có file Core đã sửa không cũ hơn hẳn dấu review (hoặc chưa có dấu) → chặn, **lặp mỗi lần kết thúc lượt**. Hook tự nhìn file đã sửa, không đọc báo cáo — nên lớp 1 quên dòng `CẦN CORE-REVIEW` vẫn bị bắt |

Chặn cứng ở lớp 3 chỉ bật khi khối `hooks` của [`settings.json`](settings.json) có gắn `SubagentStop` — không có gì ghi dấu review thì không lượt nào thoát được, nên thiếu hook đó `on-stop.sh` chỉ nhắc một lần.

**Lối thoát của agent** chỉ có một: một lượt `core-reviewer` mới hơn các file đã sửa. Bỏ qua review là quyết định của **người dùng**: người dùng tự xoá dấu `core-touched`. Agent không tự tạo, sửa hay xoá file nào trong thư mục trạng thái — `permissions.deny` chặn công cụ ghi file vào đó; đường qua lệnh shell là một dòng nợ C4 ở `docs/RULES.md` §10.

> 🛑 **Lớp chặn `core-reviewer` chỉ bật khi `src/` tồn tại.** Vai của agent đó là *đối chiếu code thật với quy tắc*; giai đoạn 1 chưa có code nên một lượt sửa tài liệu không cho nó thứ gì để đối chiếu. Bật sớm thì lệnh chặn bắn ra sau **mỗi** lần chạm `docs/quy-uoc/`, và việc chạy agent liên tục để đáp ứng nó sinh ra đúng sự phình to mà §5 và §6 tồn tại để ngăn.
>
> Cổng tài liệu thì **không** phụ thuộc giai đoạn — nó chạy sau mọi lần sửa, kể cả bây giờ.

Cổng bị chặn thì đừng tìm cách đi vòng. Nếu một vi phạm là báo sai, **sửa phép dò bằng một dấu hiệu máy đọc được** rồi ghi lý do — đừng nới luật cho cổng xanh. Nới luật để qua cổng là đúng hành vi §4 tồn tại để ngăn.

> Script hook nằm ở `.claude/hooks/`. Chúng cố ý **không dùng `grep -P`** và tự ép locale: trên Git Bash với `LANG` rỗng, `grep -P` thoát lỗi mà **bên trong một hook thì lỗi đó không hiện ra đâu cả** — dấu đơn giản không được ghi và cổng không bao giờ chạy.

Đừng chép số mục của cổng vào đây (§6) — đếm bằng lệnh:

```bash
grep -c '^section ' .claude/check-docs.sh
```

Cổng chỉ có tác dụng khi harness không hỏi lại giữa chừng. Dòng lệnh nào của cổng còn bị `permissions.allow` của [`settings.json`](settings.json) bỏ sót thì **thêm vào** — một cổng bị prompt chặn là một cổng, trên thực tế, không ai chạy.

### ⚠️ PASS không có nghĩa là tài liệu ĐÚNG

Gate chỉ bắt được thứ **máy kiểm được**: đường dẫn có tồn tại không, link có resolve không, tuyên bố có kèm ngày không. Nó **không** đọc hiểu nội dung. Ba loại lỗi nó không bao giờ bắt được:

1. **Văn xuôi mô tả thứ không tồn tại.** Gỡ một trích dẫn chết làm gate xanh, nhưng đoạn văn bên cạnh vẫn có thể đang tả một màn hình chưa ai xây.
2. **Sơ đồ/cây thư mục chép sai.** Khối không gắn ngôn ngữ không bị §2 chặn — một cây thư mục sai từng lọt qua mọi luật cho tới khi có người đọc.
3. **Ngày đúng nhưng nội dung sai.** Một tuyên bố `✅ Xong` kèm ngày hợp lệ vẫn qua được cổng kể cả khi việc đó chưa làm.

Gate là lưới **chặn hồi quy**, không phải chứng nhận chất lượng. Việc đối chiếu nội dung với source thật thuộc về `core-reviewer` và người đọc.

---

## 9. Ba khoá phân loại bắt buộc cho mọi file `docs/` và `spec/`

Ba khoá biến ba câu hỏi phải-đọc-mới-biết thành ba câu **máy đọc được**:

| Khoá | Giá trị | Trả lời câu hỏi |
| --- | --- | --- |
| `kind` | `luat` · `tham-chieu` · `quyet-dinh` · `lich-su` | **Code có phải tuân file này không?** |
| `scope` | `core` · `du-an` | **File này có đi theo khi mang Core sang dự án khác không?** |
| `verified` | `YYYY-MM-DD` · `chua-doi-chieu` · `khong-ap-dung` | **Lần cuối ai mở source ra đối chiếu toàn bộ file là bao giờ?** |

**`kind: tham-chieu` là khoá quan trọng nhất và dễ bỏ sót nhất.** Tài liệu mô tả lộ trình của một dự án khác mà bị nhầm thành luật sẽ khiến agent báo *"doc yêu cầu X, code không có X"* cho những X chưa bao giờ là luật của repo này. Đã xảy ra thật ở dự án tiền nhiệm.

**`verified: chua-doi-chieu` là giá trị TRUNG THỰC, không phải lỗi cần dọn.** Đóng dấu ngày cho một file chưa ai mở source ra so mới đúng là khuôn sai §4 cấm. Giai đoạn 1 chưa có `src/` nên gần như mọi file mang giá trị này — hoàn toàn đúng.

🛑 **Không tự khai được `khong-ap-dung`.** Cổng chỉ chấp nhận nó khi file đã mang `kind: tham-chieu`, `kind: lich-su`, **hoặc** khai `status:` chứa `not built` — tức lý do miễn trừ phải là một sự thật **máy kiểm được**, không phải một câu tự nhận. Đây là chỗ dễ lạm dụng nhất của cả ba khoá: dán `khong-ap-dung` lên một file khó đối chiếu là cách nhanh nhất để làm con số đẹp lên mà không kiểm gì cả.

**Hệ quả cho người viết:** mọi khẳng định về hiện trạng nên neo bằng `file:dòng`. Neo được thì máy kiểm được; không neo thì không ai kiểm.

---

## 10. Ngôn ngữ

Toàn bộ `docs/` và `.claude/` viết bằng **tiếng Việt**. Định danh kỹ thuật (tên class, tên file, tên package, thuật ngữ chuẩn ngành) giữ nguyên tiếng Anh — không dịch.
