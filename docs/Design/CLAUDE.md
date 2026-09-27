---
kind: luat
scope: core
verified: chua-doi-chieu
---

# CLAUDE.md — luật riêng khu `docs/Design/`

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Repo đã có `src/FE`. Bộ luật trong file này có hiệu lực đầy đủ; thứ đang dở là **khu Design**: một phần spec đã neo vào code thật, phần còn lại chưa ai mở source ra so.

| Có thật hôm nay | Sẽ thành |
| --- | --- |
| Nhãn cấp tệp của khu này chia hai: spec đã neo vào `src/FE` mang `🚧`, spec chưa có component thật mang `📐`. Đọc bằng lệnh ở §2, đừng chép danh sách | Tệp nào đã mở source ra so trọn vẹn thì mang `✅ ĐÃ ĐỐI CHIẾU` kèm ngày |
| Phần nào của [`DESIGN.md`](./DESIGN.md) đã đối chiếu với stylesheet dưới `src/FE` thì đọc ở bảng đầu chính tệp đó — không chép sang đây (§4) | Lật nhãn của chính tệp đó khi đối chiếu trọn — từng tệp một, không theo đợt |
| `verified:` của file này là `chua-doi-chieu`: chưa ai mở `src/FE` ra so với toàn bộ luật ở đây | Lật `verified:` khi có người đối chiếu trọn tệp ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §9) |

> File này chỉ nói **luật của khu Design**. Luật toàn repo ở [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md); bảng "luật nào ép bằng gì" ở [`../RULES.md`](../RULES.md). Không chép lại hai file đó vào đây.

---

## 1. Khu này là nguồn UI duy nhất

[`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §7 đã chốt: mọi tham chiếu về giao diện — layout, câu chữ, token, trạng thái component, ảnh màn hình — lấy từ `docs/Design/`. File bạn đang đọc là phần chi tiết của quyết định đó.

Hệ quả cứng, ba điều:

1. **Không dựng prototype HTML song song.** Không có trang demo tĩnh, không có Storybook được coi là nguồn. Một prototype là **bản xem trước**, không phải nguồn; giữ nó như nguồn thứ hai là mua đúng lỗi mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5 cấm.
2. **Không có nguồn UI thứ hai trong `docs/`.** [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) nói *cách viết* component (control flow, cách bọc PrimeNG, cách chia file SCSS). Nó **không** giữ giá trị token, không giữ bảng trạng thái component, không mô tả layout màn hình. Ranh giới: *quy ước = cách viết, Design = trông ra sao*.
3. **Không có nguồn UI thứ hai trong code.** Khi có `src/`, file SCSS khai `:root` là **nơi giá trị được áp**, không phải nơi giá trị được **quyết**. Xem §5.

### Vì sao khu này phủ cả Core lẫn nghiệp vụ — ngoại lệ có chủ đích

Luật xương sống của repo là **gỡ nghiệp vụ khỏi Core**: [`../kien-truc-core-module.md`](../kien-truc-core-module.md) tách `Core.*` khỏi `Modules.*`, [`../RULES.md`](../RULES.md) §3 còn cấm cả chuỗi literal đặt tên tầng nghiệp vụ trong source Core.

`docs/Design/` là **ngoại lệ duy nhất được ghi nhận** của luật đó: một screen spec được phép mô tả màn nghiệp vụ, và một prompt pack được phép trộn màn Core với màn nghiệp vụ trong cùng một luồng.

Lý do rất cụ thể — chia đôi khu này sẽ phá đúng thứ nó sinh ra để làm. Câu hỏi mà khu Design tồn tại để trả lời là *"giao diện chỗ này trông ra sao"*. Người hỏi câu đó đang nhìn **một màn hình**, và một màn hình thật luôn là hỗn hợp: `Sidebar` + `Topbar` của Core bọc quanh một `DataTable` liệt kê dữ liệu nghiệp vụ. Nếu tách thành `docs/Design/` (Core) và `spec/<feature>/ui-spec.md` (nghiệp vụ), người đọc phải ghép hai file mới dựng lại được một màn — và hai file đó sẽ lệch nhau, đúng khuôn hỏng ở [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §3.

**Nhưng ngoại lệ có biên, và biên nằm ở `Components/`:**

| Thư mục | Được chứa nghiệp vụ? |
| --- | --- |
| `Components/` | 🛑 **KHÔNG.** Chỉ component Core — thứ đi theo khi mang bộ khung sang dự án khác |
| `DESIGN.md`, `Icons.md`, `Templates/` | 🛑 **KHÔNG.** Nền tảng dùng chung |
| `Screens/` *(tạo trước pha FE có màn đó)*, `Prompts/` *(tạo khi có dự án thật)* | ✅ **CÓ.** Đây là chỗ ngoại lệ sống |

Phép thử một dòng cho `Components/`: **xoá phần nghiệp vụ khỏi spec mà spec vẫn còn nghĩa → component Core. Spec sụp → component nghiệp vụ, không thuộc thư mục này.** Một ô số liệu "có nhãn, giá trị, biến động, dùng cho dashboard chỉ số" sụp ngay khi bỏ chữ "chỉ số"; một `Card` thì không.

Ranh giới với `spec/`: `spec/<feature>/ui-spec.md` giữ **luật hiển thị theo nghiệp vụ** (trường nào bắt buộc, giá trị nào tô đỏ, ai thấy nút nào). Nó **không** khai màu, không khai khoảng cách, không khai trạng thái component — những thứ đó ở đây.

---

## 2. Fidelity Policy — spec mô tả thứ ĐÃ CÓ hay thứ CHƯA CÓ

Đây là luật quan trọng nhất của khu này, vì nó là chỗ dễ nói dối nhất mà không ai phát hiện được.

Một spec giao diện có hai vai hoàn toàn khác nhau:

| Vai | Câu spec đang nói | Ai dùng, dùng để làm gì |
| --- | --- | --- |
| **Đích đến** | *"Nút primary **sẽ** có nền `--color-brand`"* | Người viết code — để dựng cho đúng |
| **Hiện trạng** | *"Nút primary **đang** có nền `--color-brand`"* | Người sinh màn hình bằng AI, người audit — để tái tạo đúng cái đang chạy |

**Hai vai này không được lẫn trong cùng một câu, và không được lẫn trong cùng một file mà không có nhãn.**

Lẫn vai là loại lỗi đắt nhất trong tài liệu giao diện: một prompt pack sinh ảnh từ spec "đích đến" sẽ tạo ra ảnh của một sản phẩm **không tồn tại**, rồi ảnh đó được đem đi review, và người review tưởng mình vừa duyệt sản phẩm thật.

### Nhãn bắt buộc

Mỗi file trong khu này mở đầu bằng **đúng một** nhãn, ngay dưới frontmatter:

| Nhãn | Nghĩa | Bắt buộc kèm |
| --- | --- | --- |
| `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG` | Chưa có code nào hiện thực hoá | — |
| `🚧 ĐÃ CHỐT — ĐANG THI CÔNG` | Một phần đã vào code, phần còn lại chưa | Bảng *"có thật hôm nay → sẽ thành"* |
| `✅ ĐÃ ĐỐI CHIẾU` | Toàn bộ file đã so với source | Ngày đối chiếu + neo bằng chứng theo §3 |

Ba nhãn này **chính là** ba nhãn của [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4, không phải một hệ nhãn thứ hai. Đừng phát minh nhãn mới.

### Nhãn dán theo từng tệp, không dán theo giai đoạn của repo

Nhãn cấp tệp khai **thứ có thật của riêng tệp đó**. Bốn luật cứng:

- 🛑 **`📐` bị cấm khi một neo khai trong chính tệp trỏ tới code có thật.** Với `Components/<Tên>.md` thì neo đó là **tên component**: có tệp component cùng tên ở dạng kebab dưới `src/FE/src/app/` thì spec không được mang `📐`. Luật D42 ở [`../DEBT.md`](../DEBT.md), ép bằng mục §27 của [`../../.claude/check-docs.sh`](../../.claude/check-docs.sh).
- 🛑 **Nhãn không lật hàng loạt.** Một đợt code về, một lần build xanh, một quyết định chuyển giai đoạn — không cái nào thay được việc mở đúng tệp đó ra đối chiếu ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4). Tệp chưa ai đối chiếu thì `📐` và `chua-doi-chieu` là giá trị **đúng**, không phải nợ.
- 🛑 **Câu khẳng định hiện trạng phải neo được.** Viết *"component này hiện trông thế này"* thì phải trả lời được *"kiểm bằng gì"*, theo khuôn neo ở §3. Không neo được thì đừng viết câu đó.
- ✅ **Mục `Normalize on redesign` chỉ mở cho tệp đã có code thật.** Mục đó là danh sách *"cái đang chạy xấu ở chỗ nào"*, nên nó vô nghĩa với một tệp còn mang `📐` — chỗ để ngỏ của những tệp ấy ghi vào mục `Cần chốt` kèm câu hỏi cụ thể phải trả lời trước khi dựng.

Đọc nhãn cấp tệp của cả khu bằng lệnh, không chép danh sách vào tài liệu ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6):

```bash
grep -m1 -oE '^(📐|🚧|✅) ' docs/Design/Components/*.md docs/Design/Screens/*.md
```

---

## 3. Quy tắc trích dẫn — khẳng định hiện trạng phải neo được

Một spec khẳng định *"component này hiện trông thế này"* thì phải trả lời được câu *"kiểm bằng gì"*. Neo theo **định danh**, không theo số dòng.

Khuôn neo hợp lệ — đường dẫn tệp kèm **một chuỗi tìm được trong chính tệp đó**:

```text
<file stylesheet> § --color-brand
<file stylesheet> § .btn
<file stylesheet> § @media (prefers-reduced-motion: reduce)
```

Vị trí hôm nay đọc bằng lệnh, **không chép vào tài liệu** ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6):

```bash
grep -rn -- '--color-brand:' src/FE/src
grep -rn '^\.btn' src/FE/src
```

**Vì sao không dùng số dòng:** trong một stylesheet vài trăm dòng thì *mọi* số nhỏ hơn độ dài file đều "nằm trong file", nên một cổng kiểu D7 ([`../RULES.md`](../RULES.md) §1) cho qua cả trích dẫn đã trôi. Đổi tên selector thì grep biết ngay; đổi số dòng thì không ai biết. Cùng một lý do với việc neo test bằng **tên ca kiểm** thay vì `file:dòng` đã áp ở [`../RULES.md`](../RULES.md) §3.

Đây là luật toàn repo, không phải quy ước riêng khu này: **D41** ở [`../DEBT.md`](../DEBT.md), chốt ở [`../adr/0046-neo-trich-dan-vao-src-bang-chuoi-tim-duoc.md`](../adr/0046-neo-trich-dan-vao-src-bang-chuoi-tim-duoc.md). Nó phủ chiều `docs/` → `src/`; trích dẫn `docs/` → `docs/` vẫn dùng số dòng được và vẫn do D7 ép.

### Hai điều cấm tuyệt đối

1. 🛑 **Cấm neo bằng số dòng vào một tệp dưới `src/`.** Dạng bị cấm là đường dẫn tệp kèm dấu hai chấm và một con số; dạng đúng là đường dẫn tệp kèm một tên thành viên, một selector, hoặc một tên token — xem khuôn ở trên. Một neo số dòng đã trôi không trỏ vào chỗ trống mà trỏ sang **một đoạn code khác cũng có thật**, nên người đọc nhận câu sai kèm bằng chứng trông hợp lệ.
2. 🛑 **Cấm trích dẫn đường dẫn ảnh không tồn tại.** Khu này chưa có thư mục `Assets/` — điều kiện tạo nó ở §9. Cho tới khi có, mục `Screenshots` của một screen spec viết đúng một dòng nói rõ chưa có ảnh nào. Không tạo trước cây thư mục ảnh, không viết trước tên file ảnh.

Ở dự án tiền nhiệm, một danh sách **ảnh chờ chụp** dài hàng chục dòng đã tồn tại nhiều tuần mà không ai chụp dòng nào. Một danh sách chờ mà không ai làm thì giá trị bằng không, và tệ hơn: nó **che mất** những màn thật sự chưa có tham chiếu hình ảnh nào.

Được phép kể lại bài học từ dự án tiền nhiệm bằng **văn xuôi** ("ở dự án tiền nhiệm…"), không viết dạng `file:dòng`.

---

## 4. Trạng thái thi công của một component đọc ở đâu

🛑 **Ở nhãn cấp tệp đầu spec của chính component đó (§2) — không ở mục lục.** [`COMPONENTS.md`](./COMPONENTS.md) khai *danh sách* component Core và *nền* của từng cái; nó cố ý không có cột trạng thái. Lệnh đọc kèm tiêu chí PASS ở [`COMPONENTS.md`](./COMPONENTS.md) §3.

Ba nhãn của §2 đọc như sau khi áp cho một spec component:

| Nhãn | Nghĩa cho một component | Điều kiện để dán | Screen spec dùng được? |
| --- | --- | --- | --- |
| `📐` | Chưa có tệp component cùng tên dạng kebab dưới `src/FE/src/app/` | File spec có đủ các mục §6 bắt buộc. Cổng §27 đỏ nếu component đã có thật | ✅ Có — đây chính là việc spec tồn tại |
| `🚧` | Code đã về nhưng **chưa đủ** so với spec: thiếu biến thể, thiếu trạng thái, hoặc thiếu accessibility | Spec phải khai ngay trong nó phần đã mở source ra so và phần còn nợ | ⚠️ Có, nhưng task dựng màn phải đọc phần còn nợ trước |
| `✅` | Có người mở source ra so **toàn bộ** spec với code và không còn lệch | Ngày đối chiếu + neo định danh theo §3 | ✅ Có |

**Component đã rút không có nhãn thứ tư.** Nó rời bảng ở [`COMPONENTS.md`](./COMPONENTS.md) §3 và tệp spec đổi `kind` thành `lich-su` — thủ tục bốn bước ở [`COMPONENTS.md`](./COMPONENTS.md) §8. Một screen spec **không** được ghép component đã rút.

Hai luật đi kèm:

- **Không chép trạng thái sang chỗ khác.** Không dán vào [`../README.md`](../README.md), không dán vào một mục lục, không dán vào spec của component khác. [`../00-overview/ke-hoach-chi-tiet.md`](../00-overview/ke-hoach-chi-tiet.md) đã chốt câu chung cho cả repo sau khi đối chiếu ở dự án tiền nhiệm tìm ra **năm trên bảy** nhãn mục lục đã sai: trạng thái của một file đọc ở đầu chính file đó, mục lục chỉ giữ nhãn **cấp khu**.
- **`✅` không tự dán được.** Nó đòi một hành động thật: mở source, đọc hết spec, so từng mục. Dán nhãn này mà không làm việc đó là đúng khuôn hỏng [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 cấm — đóng một việc bằng cách sửa mô tả.

---

## 5. Chiều cập nhật — `Design/` là nguồn, code đuổi theo

**Quyết định: `docs/Design/` *quyết* giá trị; `src/FE/` *áp* giá trị.**

Khi code lệch spec: **sửa code**, không sửa spec cho khớp code.

### Vì sao chiều này chứ không chiều ngược lại

Chiều ngược — "trích xuất token từ stylesheet vào tài liệu" — nghe hợp lý và **sai**. Một spec chép lại từ code chỉ là **bản ghi lại của code**. Nó không quyết được gì, và mọi quyết định thiết kế từng thoả thuận trong khu này sẽ bị xoá âm thầm ở lần trích xuất kế tiếp. Người đọc không thấy gì bất thường: tài liệu vẫn khớp code, chỉ là quyết định đã biến mất.

Chiều xuôi có một cái giá thật, phải nói ra: **spec đi trước code, nên sẽ có lúc spec mô tả thứ chưa chạy.** Đó chính là lý do §2 và §4 tồn tại — nhãn là thứ trả giá đó. Không có nhãn thì chiều xuôi biến thành nói dối.

### Khi cần đổi một giá trị đã spec

Thứ tự bắt buộc, không đảo:

1. Sửa [`DESIGN.md`](./DESIGN.md) (hoặc spec component liên quan).
2. Ghi lại **quyết định đổi**: một dòng trong mục `Lịch sử quyết định` của chính file vừa sửa, gồm *giá trị cũ → giá trị mới → vì sao*. Nếu là thay đổi lớn (đổi cả hệ màu, đổi cơ chế theme), viết một ADR trong [`../adr/`](../adr/) và trỏ tới nó.
3. Áp vào code.

**Điều kiện duy nhất để sửa spec cho khớp code là bước 2 đã xảy ra.** Không có bước 2, "sửa spec cho khớp code" chỉ là chiều ngược trá hình.

### Đọc code là được phép, và cần thiết

Đọc `src/FE/` để **kiểm** xem spec có bị code bỏ lại phía sau không — đó là việc đúng, và là cách duy nhất chuyển một dòng sang `✅ đã dựng, đã đối chiếu`. Chỉ là: đọc để **kiểm**, không đọc để **quyết**.

---

## 6. Một spec component phải có gì

Bắt buộc; thiếu mục nào là spec chưa xong:

| Mục | Vì sao bắt buộc |
| --- | --- |
| Mục đích | Một câu. Không có nó thì không ai phân biệt được `NoticeBanner` với `Toast` |
| Khi nào dùng / **khi nào KHÔNG dùng** | Vế thứ hai là vế ngăn được việc phát sinh component trùng vai |
| Biến thể + kích thước | Bảng. Mỗi biến thể nói rõ *dùng khi nào* |
| **Đầy đủ trạng thái** | `default` · `hover` · `focus-visible` · `active` · `disabled` · `loading` · `error` · `empty`. Cái nào không áp dụng thì **ghi "không áp dụng" kèm lý do**, không bỏ trống dòng |
| Token dùng | Chỉ tên token từ [`DESIGN.md`](./DESIGN.md). Một giá trị thô không có token đằng sau là lỗi phải ghi vào `Cần chốt` |
| Responsive | Hành vi ở từng điểm ngắt |
| Accessibility | Vai trò ARIA, phím tắt, thứ tự focus, cái gì được đọc lên |
| API dự kiến | Input/output. Đây là hợp đồng người dựng phải khớp |
| Bọc thư viện hay tự dựng | PrimeNG hay hand-rolled — quyết định này đổi hoàn toàn khối lượng việc |
| `Cần chốt` | Chỗ cố ý để ngỏ. Không còn gì thì ghi `Không còn` |

**Bỏ trống một dòng trạng thái là lỗi nặng hơn viết sai nó.** Dòng sai thì có người cãi; dòng trống thì người dựng tự bịa, và mỗi người bịa một kiểu.

---

## 7. Quan hệ với các khu khác

| Khu | Giữ gì | Chiều |
| --- | --- | --- |
| [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) | **Cách thi công**: cú pháp Angular, cách bọc PrimeNG, cách tổ chức SCSS, luật cấm hex literal | Nó **ép** luật; `Design/` **cung cấp** giá trị hợp lệ để tuân luật đó |
| [`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md) | **Lý thuyết hệ token**: vì sao có nhiều tầng token, vì sao dùng CSS custom property, cơ chế theme hoạt động ra sao | Nó giải thích **cơ chế**; `Design/` khai **giá trị cụ thể** |
| [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) | **Lý thuyết thư viện component**: dumb/smart, khi nào bọc thư viện ngoài | Nó giải thích **nguyên tắc**; `Design/` khai **từng component** |
| [`../wiki-core/fe/15-accessibility.md`](../wiki-core/fe/15-accessibility.md) | **Chuẩn accessibility chung** và cách kiểm | `Design/` áp chuẩn đó vào từng component |

**Phép thử khi phân vân bỏ nội dung vào đâu:**

- Câu trả lời là một **con số hoặc một mã màu** → `Design/`.
- Câu trả lời là một **đoạn code mẫu** → `quy-uoc/`.
- Câu trả lời là *"vì sao ngành làm thế"* → `wiki-core/`.

---

## 8. Luật viết trong khu này

- **Tiếng Việt.** Định danh kỹ thuật giữ tiếng Anh (`focus-visible`, `aria-live`, `TemplateRef`). Đây là chỗ khu này lệch dự án tiền nhiệm — tài liệu Design của nó viết tiếng Anh; repo này thống nhất tiếng Việt theo [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §10.
- **Không hardcode giá trị trong spec component.** Viết tên token, không viết mã màu. Mã màu thật xuất hiện ở **đúng một chỗ**: bảng token trong [`DESIGN.md`](./DESIGN.md).
- **Mở rộng component có sẵn thay vì đẻ component mới.** Cần một biến thể chưa có → thêm dòng vào spec đang có. Đẻ một class song song là cách một hệ có hai định nghĩa nút bấm.
- **Không chép số đếm được.** Không viết "khu này có N component". Đếm bằng lệnh:

```bash
ls docs/Design/Components/*.md | wc -l
grep -c 'spec xong, chưa dựng' docs/Design/COMPONENTS.md
```

- **Không chép secret, khoá API, thông tin đăng nhập** vào bất kỳ spec, prompt pack, hay hướng dẫn chụp màn hình nào.
- **Trước khi coi một thay đổi là xong:**

```bash
bash .claude/check-docs.sh
```

Cổng chỉ bắt được thứ máy kiểm được. Nó **không** đọc hiểu nội dung — xem [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8 để biết ba loại lỗi nó không bao giờ bắt được.

### Câu chữ chưa có nguồn — khuôn nhãn *Chờ duyệt:*

Một câu hiển thị mà nguồn (card hợp đồng, luồng, spec nghiệp vụ) chỉ cho ý, không cho câu, thì `design-expert` soạn **bản đề xuất** ngay trong spec và đánh dấu bằng đúng một khuôn:

| Khoản | Luật |
| --- | --- |
| Hình dạng | Tiền tố in nghiêng, đúng chuỗi `*Chờ duyệt:*`. Viết khác đi thì lệnh tìm dưới đây không thấy |
| Vị trí | **Đầu ô** chứa câu, trước câu đề xuất |
| Khi người dùng duyệt | **Xoá tiền tố**, giữ câu đã duyệt. Không thêm nhãn "đã duyệt" nào: ô không có tiền tố chính là câu đã chốt |
| Hiệu lực | Ô còn tiền tố thì câu **chưa chốt**. Người dựng không được coi nó là câu cuối |

Spec màn còn ô mang tiền tố thì mục `Cần chốt` của nó có một dòng gom việc duyệt các câu đó. Tìm bằng lệnh, không chép danh sách:

```bash
grep -rn '\*Chờ duyệt:\*' docs/Design
```

PASS trước khi dựng một màn = lệnh không in dòng nào thuộc file spec của màn đó.

---

## 9. Cấu trúc thư mục

```text
docs/Design/
  CLAUDE.md        file này — luật riêng khu Design
  DESIGN.md        hệ thống thiết kế nền: token màu, chữ, khoảng cách, chuyển động, theme
  COMPONENTS.md    mục lục component Core + bảng trạng thái
  Icons.md         bộ icon, quy tắc chọn, accessibility cho icon
  Templates/       khuôn để người sau viết spec mới đúng dạng
  Components/      một file một component Core
```

Ba thư mục sau **không tạo trước điều kiện của chính nó** — mỗi thư mục có một điều kiện tạo riêng:

| Thư mục | Chứa gì | Tạo khi nào |
| --- | --- | --- |
| `Screens/` | Spec màn hình theo luồng. Đây là chỗ ngoại lệ Core↔nghiệp vụ ở §1 sống | Trước pha FE có màn đó — spec màn phải xong trước khi pha đó bắt đầu dựng màn |
| `Prompts/` | Bộ prompt sinh màn hình | Khi screen spec của luồng đó đã xong |
| `Assets/` | Ảnh chụp màn hình, tài sản thương hiệu | Khi có app chạy được để chụp — xem §3 |

Tạo trước một thư mục rỗng là tạo trước một chỗ để bịa.

### Khuôn nào dùng cho việc nào

Mỗi việc có đúng một khuôn được gọi tên. Chỉ trỏ tới **thư mục** `Templates/` thì một agent được bảo "viết theo mẫu" vẫn phải đoán mẫu nào.

| Đang viết gì | Khuôn |
| --- | --- |
| Spec một component dùng chung | [`Templates/Components.md`](./Templates/Components.md) |
| Spec một màn hình | [`Templates/Screen.md`](./Templates/Screen.md) |
| Spec một màn **danh sách** | [`Templates/Screen.md`](./Templates/Screen.md) cho cấu trúc file **và** [`Templates/ListScreen.md`](./Templates/ListScreen.md) cho bố cục bắt buộc |
| Khai một nhóm design token | [`Templates/Tokens.md`](./Templates/Tokens.md) |
| Khai bộ icon của một dự án | [`Templates/Icons.md`](./Templates/Icons.md) |
| Kiểm kê giao diện đang có | [`Templates/UiInventory.md`](./Templates/UiInventory.md) |
| Báo cáo một lượt audit giao diện | [`Templates/AuditReport.md`](./Templates/AuditReport.md) |
| Bộ prompt sinh màn hình | [`Templates/PromptPack.md`](./Templates/PromptPack.md) |
| Nhật ký xuất bản thiết kế | [`Templates/ExportLog.md`](./Templates/ExportLog.md) |
| `DESIGN.md` cho một dự án dựng trên Core | [`Templates/DesignMd.md`](./Templates/DesignMd.md) |
| `README.md` cho khu Design của một dự án | [`Templates/ProjectReadme.md`](./Templates/ProjectReadme.md) |

🛑 **Khu này KHÔNG có khuôn cho TechDoc, UserGuide hay tài liệu UnitTest.** Ba loại đó là tài liệu bàn giao, không phải tài liệu thiết kế. Chưa có khuôn nào cho chúng trong repo.

---

## 10. Lịch sử quyết định

| Quyết định | Nội dung |
| --- | --- |
| Chiều cập nhật | `Design/` quyết, code áp. Lý do và cái giá phải trả ở §5 |
| Ngoại lệ Core↔nghiệp vụ | Khu Design phủ cả hai, trừ `Components/`. Phép thử ở §1 |
| Ngôn ngữ | Tiếng Việt, lệch dự án tiền nhiệm (tiếng Anh). Lý do ở §8 |
| Không có `Prototypes/` | Nguồn UI duy nhất, không có bản xem trước song song. Lý do ở §1 |
| Bỏ mục `Normalize on redesign` ở giai đoạn 1 | Chưa có app để chê. Thay bằng `Cần chốt`. Lý do ở §2 |
| Khuôn nhãn *Chờ duyệt:* | Câu chữ chưa có nguồn mang tiền tố in nghiêng ở đầu ô; người dùng duyệt thì xoá tiền tố. Luật ở §8 |
