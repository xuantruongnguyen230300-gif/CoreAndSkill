---
kind: luat
scope: core
verified: chua-doi-chieu
---

# FilterPanel

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có ở `src/FE/src/app/shared/components/filter-panel/` (FE-3, 2026-10-08) — đã có biến thể duy nhất (`drawer`), mới ba kind trường. Bảng "Đã có → còn thiếu" ngay dưới khai đúng những mục đã mở source ra so; mục không có tên trong bảng thì chưa ai đối chiếu.

**Nền:** tự dựng, không bọc PrimeNG. Theo [`../COMPONENTS.md`](../COMPONENTS.md) §4 nó là một bố cục xếp các ô nhập đã có thành hàng dọc — không có hành vi nào thuộc nhóm "khó". Lớp nổi chứa nó thì có, nhưng lớp nổi đó là [`Drawer.md`](./Drawer.md), không phải component này.

## Đã có → còn thiếu

Component: `src/FE/src/app/shared/components/filter-panel/filter-panel.component.ts` — selector `app-filter-panel`, class `FilterPanelComponent`. Đối chiếu 2026-10-08 bằng **đọc mã** `.ts` / `.html`; có ca kiểm ở `filter-panel.component.spec.ts` (tên ca ở cột "Đã có") và ca đầu-cuối `src/FE/e2e/man/10-nguoi-dung.spec.ts` (chuỗi `nguoi-dung-bang-loc`). **Người soạn spec chưa tự chạy** các bộ test — xanh/đỏ do lượt chạy test báo. Hình thức thật trên trình duyệt (khe, bề rộng, neo đáy < `$bp-sm`) **chưa đo**.

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| Biến thể `drawer` | Có, là biến thể duy nhất: `filter-panel.component.html` mở bằng `<app-drawer variant="edit" size="lg" position="right"` kèm `[open]`, `[title]`, `[panelId]`; `dirty` không truyền (Drawer chưa có `dirty`) nên đóng chưa áp không hỏi xác nhận — ca `đóng mà chưa áp › nút đóng: phát closed, KHÔNG hỏi xác nhận, bỏ phần chưa áp…` | — |
| Không có biến thể `inline` (rút 2026-10-08) | Khớp: `filter-panel.component.ts` không có input `variant`; `filter-panel.component.scss` chỉ có một bố cục — `.filter-panel` cột dọc. Chú thích đầu `filter-panel.component.ts` đã bỏ `inline` khỏi danh sách việc chưa dựng (đọc mã 2026-10-08: thư mục `shared/components/filter-panel/` không còn chuỗi `inline`; câu `Cũng chưa dựng:` nay chỉ nêu `<fieldset>` cho `group`) | — |
| Kind `text`, `select` | Có: `@case ('text')` và `@case ('select')` trong `.html`, mỗi trường một `app-form-row` (`stacked` theo mặc định của `FormRow`) với `<label>` thật; `text` vẽ bằng `app-input type="text"`, `select` bằng `app-input type="select"`. Ca `trường text: gõ rồi Áp dụng…`; ca `mở: Drawer là hộp thoại có tên "Lọc người dùng", form role="search", mỗi trường có <label> thật` | Cỡ `md` của `FormRow` chưa so riêng |
| Kind `autocomplete` | Có: `@case ('autocomplete')` vẽ `app-autocomplete variant="single" size="md"`; `searchStates` (loading/error riêng từng trường, nút "Thử lại" phát lại `fieldSearch` với chuỗi tìm gần nhất), `selectedLabels` (ô trống khi chưa có nhãn, không hiện khoá thô). Ca trong khối `describe('trường autocomplete'` — `gõ tìm → fieldSearch`, `chọn một mục… → applied mang KHOÁ`, `value có khoá mà CHƯA có nhãn → ô TRỐNG…`, `lỗi tải: CHỈ trường đó hiện câu lỗi kèm "Thử lại"…` | — |
| Kind `number`, `date`, `daterange`, `multiselect`, `tree`, `check` | Kiểu `FilterField` **khai** đủ chín kind, nhưng `.html` không có nhánh cho sáu kind này: trường mang kind đó **không vẽ gì** và không làm hỏng các trường khác — ca `kind chưa dựng (date) → không vẽ trường, không làm hỏng các trường còn lại` | **Chưa dựng sáu kind.** `DatePicker`, ô chọn nhiều, cây, ô số chưa có component Core để vẽ. Không có cảnh báo nào ở nơi gọi khi khai kind chưa dựng — rủi ro thầm lặng |
| `<fieldset>` + `<legend>` cho trường liên quan (`group`) | — | **Chưa dựng.** `FilterField.group` có trong kiểu nhưng không ai đọc; chỉ cần khi có `daterange` |
| Nút chân: Xoá hết (`ghost`) rồi Áp dụng (`primary`, `type="submit"`) | Có: hai `app-button` ở `slot="footer"`; Áp dụng gắn vào form bằng input `form` của [`Button.md`](./Button.md) vì nằm ngoài thẻ `<form>` (ca `Enter trong một trường (gửi form) cũng áp bộ lọc`); thứ tự Tab ca `thứ tự Tab: trường → … → Xoá hết → Áp dụng (Áp dụng đứng cuối)` | — |
| Áp dụng | Có: khoá khi chưa có gì đổi so với `value` (ca `khoá khi chưa có gì đổi so với value; mở khoá khi đổi một trường`); phát `applied` với giá trị **mọi** trường (trống là `null`) rồi `closed` (ca `bấm Áp dụng → applied mang giá trị MỌI trường (trống là null), rồi closed…`); không phát ở mỗi lần đổi một trường (ca `KHÔNG phát applied ở mỗi lần đổi một trường`) | — |
| Xoá hết | Có, **đúng spec** §"Trong `Drawer`" dòng "Xoá hết": phát `cleared` (không phát `applied`), đưa mọi trường về trống, panel **giữ mở**, focus chuyển về trường đầu trước khi nút tự khoá — ca `phát cleared, đưa mọi trường về trống, panel GIỮ MỞ, focus về trường đầu, nút tự khoá`. Nút khoá khi mọi trường đã trống (`khongCoDieuKien`) | Việc **áp ngay** (đẩy URL) là phần của trang cha nghe `cleared`; component không làm — đúng vì nó dumb |
| Mở / đóng không áp | Có: mở thì nháp nạp lại đúng `value` (ca `lần mở sau hiện lại đúng value`); đóng không áp bỏ phần chưa áp; trang cha đặt `open = false` thì focus vẫn về nút lọc nhờ `Drawer` | — |
| Focus khi mở | Có: vào trường đầu tiên, không vào nút đóng (ca `mở: focus vào TRƯỜNG ĐẦU TIÊN, không vào nút đóng`; e2e dùng `truongDau`) | — |
| `disabled` | Có: mọi trường và hai nút khoá thật (ca `disabled: mọi trường khoá thật và nút Áp dụng khoá`) | — |
| Chữ cố định | Có: `chung.loc.apDung`, `chung.loc.xoaHet`, `chung.thuLai` — cả ba đều có trong `src/FE/public/i18n/vi.json` | — |
| `panelId` → `id` của `Drawer` | Có: `[panelId]="panelId()"` truyền xuống; e2e dựng id form `nguoi-dung-bang-loc-form` | — |
| Không tự báo kết quả sau khi áp (chốt 2026-10-08) | Khớp: `filter-panel.component.html` không có chuỗi `aria-live` nào | — Phần đọc to kết quả thuộc `DataTable` và chưa dựng: [`DataTable.md`](./DataTable.md) bảng "Đã có → còn thiếu", dòng "Vùng báo kết quả" |

---

## Mục đích

Cho người dùng đặt nhiều điều kiện lọc cùng lúc, rồi áp một lần.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Màn danh sách có từ **một** trường lọc trở lên — luôn, không theo ngưỡng số trường (chốt 2026-10-08; lý do ở [`Toolbar.md`](./Toolbar.md) §"Panel lọc nằm ở đâu") | 🛑 Danh sách không có trường lọc nào → [`Toolbar.md`](./Toolbar.md) biến thể `search`, không có panel. 🛑 Không đặt ô lọc thẳng vào dải `Toolbar`, dù chỉ một hai trường |
| Bộ lọc có trường ngày, trường chọn nhiều, trường cây | 🛑 Tìm theo một chuỗi tự do → ô tìm của `Toolbar` |
| Bộ lọc của một báo cáo có tham số | 🛑 Form nhập dữ liệu → [`Dialog.md`](./Dialog.md) chứa [`FormRow.md`](./FormRow.md). Lọc **không đổi dữ liệu**, form thì có — hai thứ này không dùng chung nút |

### Ba component, ba vai

| Component | Sở hữu gì |
| --- | --- |
| [`Toolbar.md`](./Toolbar.md) | **Nút** mở panel, và badge đếm số điều kiện đang bật |
| **`FilterPanel`** *(file này)* | **Nơi nhập** điều kiện — các trường, nút Áp dụng, nút Xoá hết |
| [`FilterChip.md`](./FilterChip.md) | **Kết quả** — mỗi điều kiện đang bật thành một chip gỡ được |

Trước khi có file này, vai giữa không thuộc về ai: `Toolbar` đẩy sang một panel nó không mô tả, `FilterChip` nhận một hình dạng dữ liệu không ai khai. Mỗi dự án tự lấp khoảng đó là ba bộ lọc khác nhau sau ba dự án — và `FilterChip`, vốn là component Core, nhận dữ liệu theo ba hình dạng.

## Biến thể

| Biến thể | Ở đâu | Dùng khi |
| --- | --- | --- |
| `drawer` | Trong [`Drawer.md`](./Drawer.md) cỡ `lg`, neo phải | **Biến thể duy nhất** — lọc chỉ đi qua `Drawer`. Đặc tả thi công ở §"Trong `Drawer`" |

🛑 **Không có biến thể nằm thẳng (`inline`)** — rút khỏi spec 2026-10-08 (người dùng). Một khu bung ra ngay dưới `Toolbar` đẩy bảng xuống, nên bảng nhảy mỗi lần mở panel; và khối danh sách cao đúng phần còn lại của màn, nên khu đó ăn thẳng vào chiều cao vùng cuộn. `drawer` phủ lên chứ không đẩy.

🛑 **Không có biến thể popover.** `Toolbar` gần như luôn nằm trong một vùng cuộn, và một popover nhiều trường sẽ bị cắt ở mép vùng đó — lỗi chỉ lộ ra ở màn thấp hoặc khi danh sách đã cuộn, tức là muộn.

## Kích thước

`FilterPanel` **không** dùng thang `--size-control-*` — nó là vùng chứa. Control bên trong dùng cỡ `md`.

| Khoản | Giá trị |
| --- | --- |
| Khe dọc giữa hai trường | `--sp-6` |
| Khe giữa hai nhóm trường | `--sp-7` |
| Đệm panel | `--sp-6` |
| Bề rộng ở `drawer` | Theo `Drawer` cỡ `lg` |
| Bố cục trường | **Một cột** |

**Một cột ở `drawer`, không phải hai.** Hai cột tiết kiệm chiều dọc nhưng buộc mắt quét zíc-zắc, và với nhãn tiếng Việt dài ngắn khác nhau thì hai cột trông răng cưa — cùng lý do [`FormRow.md`](./FormRow.md) cấm biến thể `inline` cho form nhập.

## Trong `Drawer` — đặc tả thi công

Chốt 2026-10-08. `FilterPanel` biến thể `drawer` **tự đặt** một [`Drawer.md`](./Drawer.md) quanh form của nó — chiều `shared/components/` dùng `shared/ui/` là chiều được phép ([`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2), cùng cách [`Sidebar.md`](./Sidebar.md) bọc `Drawer` ở dạng drawer. Màn không tự dựng `Drawer` cho bộ lọc.

| Khoản | Quy định |
| --- | --- |
| `Drawer` | `variant="edit"` — có lớp phủ, **bẫy focus**, `aria-modal="true"`; `size="lg"`; `position="right"` (dưới `$bp-sm` tự neo đáy theo [`Drawer.md`](./Drawer.md) §Responsive); `title` = input `title`; `open` = input `open` |
| Vì sao `edit` chứ không `inspect` | Panel là một form đang nhập dở; một cú bấm nhầm ra danh sách phía sau không được làm mất nó, và người dùng bàn phím không được Tab lạc ra bảng khi đang đặt điều kiện. Danh sách vẫn nhìn thấy mờ sau lớp phủ |
| `dirty` của `Drawer` | Luôn `false`. Lọc **không đổi dữ liệu**, nên đóng panel khi chưa áp không hỏi xác nhận — phần chưa áp bị bỏ, lần mở sau các trường hiện lại đúng `value` đang áp |
| Ba slot của `Drawer` | Đầu: tiêu đề + nút đóng (do `Drawer` vẽ). Thân: `<form role="search">` chứa các trường, mỗi trường một [`FormRow.md`](./FormRow.md) `stacked` với `<label>` thật **nhìn thấy được**. Chân: nút **Xoá hết** (`ghost`) rồi nút **Áp dụng** (`primary`, `type="submit"`), ghim phải |
| `id` | Phần tử gốc của `Drawer` mang `panelId` — đích `aria-controls` của nút lọc ở [`Toolbar.md`](./Toolbar.md) |
| Mở | Các trường nạp `value` đang áp. Focus vào **trường đầu tiên** (§Accessibility) |
| Áp dụng | Bấm nút hoặc `Enter` trong một trường → phát `applied` với toàn bộ giá trị các trường, rồi panel đóng (phát `closed`). Nút khoá khi chưa có gì đổi so với `value` |
| Xoá hết | Đưa mọi trường của panel về trống **và áp ngay** — phát `cleared`; panel **giữ mở** để đặt lại điều kiện mới. Nút vừa bấm thành `disabled` nên focus chuyển về trường đầu tiên — không để focus rơi về `<body>`. Chỉ gỡ điều kiện của panel; từ khoá ô tìm không thuộc panel và giữ nguyên |
| Đóng không áp | Nút đóng, `Escape`, bấm lớp phủ → phát `closed`, bỏ phần chưa áp |
| Focus khi đóng | Trả về **nút lọc** đã mở panel — mọi đường đóng. Việc trả focus do `Drawer` gánh ([`Drawer.md`](./Drawer.md) §Accessibility, dòng "Focus khi đóng") |
| Sau khi áp | Chip điều kiện cập nhật ở hàng chip của `Toolbar`, số trên nút lọc đổi theo, kết quả do `DataTable` đọc to — panel không tự báo (§Accessibility, dòng "Thông báo kết quả") |

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Các trường xếp dọc; chân panel có nút **Áp dụng** (`primary`) và **Xoá hết** (`ghost`). Nút Áp dụng bị khoá khi chưa có gì đổi so với lần áp gần nhất | Có |
| `hover` | **Không áp dụng cho chính panel.** Nó là vùng chứa; hover thuộc về từng control bên trong | — |
| `focus-visible` | **Không áp dụng cho chính panel.** Nhưng thứ tự Tab bắt buộc: trường đầu → … → trường cuối → Xoá hết → Áp dụng. Nút Áp dụng đứng **cuối** vì nó là hành động kết thúc | — |
| `active` | **Không áp dụng.** Không có gì để nhấn xuống | — |
| `disabled` | Cả panel chỉ đọc (không đủ quyền xem dữ liệu đã lọc): mọi trường `disabled` thật, nút Áp dụng khoá. 🛑 Không phủ `pointer-events: none` lên cả panel — control biến mất khỏi thứ tự Tab mà trình đọc màn hình không biết vì sao | Có |
| `loading` | Trường có danh mục tải theo yêu cầu (chọn đơn vị, chọn vai trò) hiện trạng thái `loading` của **riêng trường đó**; panel không khoá. Người dùng vẫn đặt được điều kiện ở các trường đã sẵn sàng | Có |
| `error` | Tải một danh mục hỏng: **chỉ trường đó** hiện lỗi kèm nút "Thử lại"; các trường khác dùng được bình thường. Panel **không** đóng | Có |
| `empty` | Chưa điều kiện nào được đặt: panel vẫn hiện đủ trường, nút Xoá hết bị khoá. Panel không có "trạng thái rỗng" theo nghĩa `EmptyState` — nó luôn có nội dung | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-border`, `--color-border-subtle`, `--color-text`, `--color-text-muted`, `--color-focus` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-semibold`, `--ls-wide` |
| Khoảng cách | `--sp-3`, `--sp-4`, `--sp-5`, `--sp-6`, `--sp-7` |
| Hình dạng | `--radius-md`, `--border-w` |
| Kích thước | `--size-control-md` |
| Icon | `pi-filter`, `pi-filter-slash`, `pi-times` — [`../Icons.md`](../Icons.md) §5 |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | `drawer` neo phải, bề rộng cỡ `lg`; danh sách phía sau vẫn nhìn thấy |
| `$bp-sm` … `$bp-lg` | Giữ nguyên — bề rộng theo `Drawer` cỡ `lg` ([`Drawer.md`](./Drawer.md) §Responsive) |
| < `$bp-sm` | `Drawer` neo **đáy**, cao tối đa 85vh, nút Áp dụng dính đáy panel để ngón cái với tới |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | Panel là một `<form>` thật — `Enter` trong một trường áp bộ lọc, đúng kỳ vọng. `role="search"` trên form |
| Quan hệ với nút mở | Nút ở `Toolbar` mang `aria-haspopup="dialog"`, `aria-expanded` và `aria-controls` trỏ tới `panelId` — [`Toolbar.md`](./Toolbar.md) §Accessibility, dòng "Nút lọc" |
| Nhãn của panel | `Drawer` mang `role="dialog"`, `aria-modal="true"`, `aria-labelledby` trỏ vào tiêu đề — tiêu đề nói **lọc cái gì** ("Lọc người dùng"), không chỉ "Bộ lọc" |
| Nhãn trường | `<label>` thật cho mọi trường qua [`FormRow.md`](./FormRow.md). Placeholder **không** thay được nhãn |
| Bàn phím | `Escape` đóng panel và **trả focus về nút mở**. `Enter` áp bộ lọc. Focus bẫy trong panel (`Drawer` `edit`) |
| Focus khi mở | Vào **trường đầu tiên**, không vào nút đóng — người dùng mở panel để nhập, không để đóng |
| Thông báo kết quả | Panel **không** mang vùng `aria-live` nào và không tự báo kết quả sau khi áp (chốt 2026-10-08, người dùng). Kết quả — kể cả "không có kết quả" — do `DataTable` đọc to: [`DataTable.md`](./DataTable.md) §Accessibility, "Vùng báo kết quả". Panel báo thêm thì trình đọc màn hình đọc hai lần |
| Nhóm trường | Trường liên quan nhau (từ ngày / đến ngày) bọc trong `<fieldset>` với `<legend>`, không phải hai label rời |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `fields` | input | `ReadonlyArray<FilterField>` | `[]` | **Đây là điểm mở rộng chính.** Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. `kind: 'date'` vẽ bằng [`DatePicker.md`](./DatePicker.md) `mode = 'single'`, `kind: 'daterange'` bằng `mode = 'range'` — không có ô ngày riêng cho panel. `kind: 'select'` vẽ bằng [`Input.md`](./Input.md) `select`. `kind: 'autocomplete'` vẽ bằng [`Autocomplete.md`](./Autocomplete.md) `variant = 'single'` cỡ `md` — kết quả tìm và nhãn vào qua `searchStates`, `selectedLabels`, không qua `FilterField` |
| `value` | input | `Record<string, unknown>` | `{}` | Điều kiện đang áp, khoá theo `key` của trường |
| `open` | input | `boolean` | `false` | Trang cha giữ nguồn sự thật — cùng giá trị nó truyền vào `filterPanelOpen` của `Toolbar` |
| `title` | input | `string` | — | **Bắt buộc.** Tiêu đề `Drawer`, **đã dịch**, nói lọc cái gì |
| `panelId` | input | `string` | — | **Bắt buộc.** `id` của panel — cùng chuỗi màn truyền vào `filterPanelId` của `Toolbar` |
| `disabled` | input | `boolean` | `false` | |
| `searchStates` | input | `Readonly<Record<string, { options: ReadonlyArray<Option>; loading: boolean; error: string \| null }>>` | `{}` | Chỉ cho trường `kind: 'autocomplete'`, khoá theo `key` (chốt 2026-10-08 — [`../../adr/nhat-ky-quyet-dinh.md`](../../adr/nhat-ky-quyet-dinh.md)). Kết quả của lần tìm gần nhất; `error` là câu **đã dịch**. Vẽ bằng [`Autocomplete.md`](./Autocomplete.md) `single`, trạng thái `loading`/`error` của **riêng** trường đó (§Trạng thái) |
| `selectedLabels` | input | `Readonly<Record<string, string>>` | `{}` | Nhãn của giá trị đang áp ở trường `autocomplete`, để ô hiện **tên** chứ không hiện khoá khi panel mở với `value` đọc từ URL. Màn đã tra tên này cho chip, dùng chung một nguồn. Khi `value` có khoá mà `selectedLabels` chưa có nhãn thì ô **trống**, không hiện khoá thô (khớp luật "chip chỉ vẽ khi đã có tên" của [`../Screens/10-nguoi-dung.md`](../Screens/10-nguoi-dung.md)) |
| `applied` | output | `Record<string, unknown>` | — | Phát khi bấm Áp dụng. **Không phát ở mỗi lần đổi một trường** — xem dưới |
| `cleared` | output | `void` | — | Phát khi bấm Xoá hết — điều kiện của panel đã về trống và áp ngay (§"Trong `Drawer`") |
| `closed` | output | `void` | — | Phát mọi lần panel đóng |
| `fieldSearch` | output | `{ key: string; text: string }` | — | Chuyển tiếp lần tìm của ô `autocomplete`; ngưỡng ký tự và chờ ngừng gõ theo [`Autocomplete.md`](./Autocomplete.md) §API. Màn gọi API rồi truyền `searchStates` mới. Nút "Thử lại" ở trạng thái `error` của trường phát lại sự kiện này với chuỗi tìm gần nhất — không có output riêng cho thử lại |

**Chữ cố định của panel** — nhãn hai nút chân — là chữ của chính component, cùng cách [`Dialog.md`](./Dialog.md) và [`ConfirmDialog.md`](./ConfirmDialog.md) dịch chữ cố định của mình; màn không truyền:

| Phần tử | Câu | Khoá i18n |
| --- | --- | --- |
| Nút áp | Áp dụng | `chung.loc.apDung` |
| Nút gỡ mọi điều kiện của panel | Xoá hết | `chung.loc.xoaHet` |
| Nút thử lại của trường `autocomplete` lỗi | Thử lại | `chung.thuLai` |

🛑 **Panel áp một lần, không lọc theo từng phím gõ.** Ô tìm của `Toolbar` thì lọc ngay (tầng trạng thái danh sách chờ ngừng gõ rồi mới gọi); panel thì không. Lý do: người dùng đặt bốn điều kiện sẽ sinh bốn lần gọi máy chủ, ba trong đó là kết quả trung gian không ai muốn thấy — và với dữ liệu lớn thì ba lần đó đủ làm màn hình giật.

`FilterPanel` là component **dumb** — nó không tự gọi API lấy danh mục cho các trường; trang cha truyền `options` xuống qua từng `FilterField` ([`../COMPONENTS.md`](../COMPONENTS.md) §5). Điều kiện đã áp chỉ sống trên URL ([`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8) — không lưu theo người dùng cho lần mở màn sau. Mỗi trường mang **một toán tử cố định** do card hợp đồng khai ở bộ lọc của nó ([`../../contracts/README.md`](../../contracts/README.md) §8); panel không có ô chọn toán tử.

## Do / Don't

- ✅ Áp một lần bằng nút, không áp theo từng thay đổi.
- ✅ Mỗi điều kiện đã áp phải hiện thành một `FilterChip` ngoài panel — người dùng đóng panel rồi vẫn phải thấy mình đang lọc gì.
- ✅ Khoá nút Áp dụng khi chưa có gì đổi.
- ✅ Lỗi tải danh mục của một trường chỉ làm hỏng trường đó.
- ❌ Không dùng popover.
- ❌ Không đặt trường lọc thẳng vào dải `Toolbar` — có từ một trường lọc thì mọi trường vào panel.
- ❌ Không hỏi xác nhận khi đóng panel chưa áp — lọc không đổi dữ liệu.
- ❌ Không để nút Áp dụng đứng trước các trường trong thứ tự Tab.
- ❌ Không đóng panel khi tải một danh mục bị lỗi.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có cần "bộ lọc đã lưu" đặt tên được không? Đây là tính năng người dùng kế toán hay xin, nhưng nó cần một endpoint và một chỗ lưu mà Core chưa có | Sau F3 — `architect`, khi dự án hạ nguồn đầu tiên có nhu cầu thật |
| 2 | **Đã chốt 2026-10-08 (người dùng): rút biến thể `inline`** — lọc chỉ qua `Drawer`. Đã gỡ khỏi §Biến thể, §Kích thước, §Responsive, §API | Đã trả lời |
| 3 | **Đã chốt 2026-10-08 (người dùng):** `DataTable` làm chủ vùng đọc to kết quả, kể cả "không có kết quả"; panel không tự báo — §Accessibility, dòng "Thông báo kết quả" | Đã trả lời |
