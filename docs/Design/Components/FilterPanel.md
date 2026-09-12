---
kind: luat
scope: core
verified: chua-doi-chieu
---

# FilterPanel

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Theo [`../COMPONENTS.md`](../COMPONENTS.md) §4 nó là một bố cục xếp các ô nhập đã có thành hàng dọc — không có hành vi nào thuộc nhóm "khó". Lớp nổi chứa nó thì có, nhưng lớp nổi đó là [`Drawer.md`](./Drawer.md), không phải component này.

---

## Mục đích

Cho người dùng đặt nhiều điều kiện lọc cùng lúc, rồi áp một lần.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Từ **ba trường lọc** trở lên trên một màn danh sách | 🛑 Một hoặc hai trường → đặt thẳng vào [`Toolbar.md`](./Toolbar.md), cùng hàng ô tìm. Một panel cho hai ô chọn là hai lần bấm thừa cho mọi lần lọc |
| Bộ lọc có trường ngày, trường chọn nhiều, trường cây | 🛑 Tìm theo một chuỗi tự do → ô tìm của `Toolbar` |
| Bộ lọc của một báo cáo có tham số | 🛑 Form nhập dữ liệu → [`Dialog.md`](./Dialog.md) chứa [`FormRow.md`](./FormRow.md). Lọc **không đổi dữ liệu**, form thì có — hai thứ này không dùng chung nút |

### Ba component, ba vai — đây là chỗ trước đây bỏ trống

| Component | Sở hữu gì |
| --- | --- |
| [`Toolbar.md`](./Toolbar.md) | **Nút** mở panel, và badge đếm số điều kiện đang bật |
| **`FilterPanel`** *(file này)* | **Nơi nhập** điều kiện — các trường, nút Áp dụng, nút Xoá hết |
| [`FilterChip.md`](./FilterChip.md) | **Kết quả** — mỗi điều kiện đang bật thành một chip gỡ được |

Trước khi có file này, vai giữa không thuộc về ai: `Toolbar` đẩy sang một panel nó không mô tả, `FilterChip` nhận một hình dạng dữ liệu không ai khai. Mỗi dự án tự lấp khoảng đó là ba bộ lọc khác nhau sau ba dự án — và `FilterChip`, vốn là component Core, nhận dữ liệu theo ba hình dạng.

## Biến thể

| Biến thể | Ở đâu | Dùng khi |
| --- | --- | --- |
| `drawer` | Trong [`Drawer.md`](./Drawer.md) cỡ `lg`, neo phải | **Mặc định.** Từ ba trường trở lên |
| `inline` | Một khu bung ra ngay dưới `Toolbar`, đẩy bảng xuống | Chỉ khi panel có đúng ba trường ngắn **và** màn hình đủ cao. Xem cái giá dưới |

🛑 **Không có biến thể popover.** `Toolbar` gần như luôn nằm trong một vùng cuộn, và một popover nhiều trường sẽ bị cắt ở mép vùng đó — lỗi chỉ lộ ra ở màn thấp hoặc khi danh sách đã cuộn, tức là muộn.

**Cái giá của `inline`:** nó đẩy bảng xuống, nên bảng nhảy mỗi lần mở panel. Chấp nhận được với ba trường một hàng; với sáu trường xếp hai hàng thì bảng tụt khỏi tầm nhìn và người dùng mất chỗ đang đọc. `drawer` không có vấn đề đó vì nó phủ lên chứ không đẩy.

## Kích thước

`FilterPanel` **không** dùng thang `--size-control-*` — nó là vùng chứa. Control bên trong dùng cỡ `md`.

| Khoản | Giá trị |
| --- | --- |
| Khe dọc giữa hai trường | `--sp-6` |
| Khe giữa hai nhóm trường | `--sp-7` |
| Đệm panel | `--sp-6` |
| Bề rộng ở `drawer` | Theo `Drawer` cỡ `lg` |
| Bố cục trường | **Một cột** ở `drawer`; tối đa ba cột ở `inline` |

**Một cột ở `drawer`, không phải hai.** Hai cột tiết kiệm chiều dọc nhưng buộc mắt quét zíc-zắc, và với nhãn tiếng Việt dài ngắn khác nhau thì hai cột trông răng cưa — cùng lý do [`FormRow.md`](./FormRow.md) cấm biến thể `inline` cho form nhập.

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
| ≥ `--bp-lg` | `drawer` neo phải, bề rộng cỡ `lg`; danh sách phía sau vẫn nhìn thấy |
| `--bp-md` … `--bp-lg` | Giữ nguyên; `inline` co từ ba cột xuống hai |
| < `--bp-md` | `inline` **tự chuyển thành `drawer`** — ba trường một hàng trên màn hẹp thành ba hàng, và lúc đó nó đẩy bảng đi quá xa |
| < `--bp-sm` | `Drawer` neo **đáy**, cao tối đa 85vh, nút Áp dụng dính đáy panel để ngón cái với tới |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | Panel là một `<form>` thật — `Enter` trong một trường áp bộ lọc, đúng kỳ vọng. `role="search"` trên form |
| Quan hệ với nút mở | Nút ở `Toolbar` mang `aria-expanded` và `aria-controls` trỏ tới `id` của panel |
| Nhãn trường | `<label>` thật cho mọi trường qua [`FormRow.md`](./FormRow.md). Placeholder **không** thay được nhãn |
| Bàn phím | `Escape` đóng panel và **trả focus về nút mở**. `Enter` áp bộ lọc. Ở `drawer`, focus bẫy trong panel |
| Focus khi mở | Vào **trường đầu tiên**, không vào nút đóng — người dùng mở panel để nhập, không để đóng |
| Thông báo kết quả | Số bản ghi sau khi áp báo qua `aria-live="polite"` **đặt cạnh bảng**, không đặt trong panel — panel có thể đã đóng lúc kết quả về |
| Nhóm trường | Trường liên quan nhau (từ ngày / đến ngày) bọc trong `<fieldset>` với `<legend>`, không phải hai label rời |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `fields` | input | `ReadonlyArray<FilterField>` | `[]` | **Đây là điểm mở rộng chính.** Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 |
| `value` | input | `Record<string, unknown>` | `{}` | Điều kiện đang áp, khoá theo `key` của trường |
| `variant` | input | `'drawer' \| 'inline'` | `'drawer'` | Mặc định là dạng không đẩy bảng, sai theo hướng an toàn |
| `open` | input | `boolean` | `false` | Trang cha giữ nguồn sự thật |
| `disabled` | input | `boolean` | `false` | |
| `applied` | output | `Record<string, unknown>` | — | Phát khi bấm Áp dụng. **Không phát ở mỗi lần đổi một trường** — xem dưới |
| `cleared` | output | `void` | — | Phát khi bấm Xoá hết |
| `closed` | output | `void` | — | Phát mọi lần panel đóng |

🛑 **Panel áp một lần, không lọc theo từng phím gõ.** Ô tìm của `Toolbar` thì lọc ngay (có debounce); panel thì không. Lý do: người dùng đặt bốn điều kiện sẽ sinh bốn lần gọi máy chủ, ba trong đó là kết quả trung gian không ai muốn thấy — và với dữ liệu lớn thì ba lần đó đủ làm màn hình giật.

`FilterPanel` là component **dumb** — nó không tự gọi API lấy danh mục cho các trường; trang cha truyền `options` xuống qua từng `FilterField` ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Áp một lần bằng nút, không áp theo từng thay đổi.
- ✅ Mỗi điều kiện đã áp phải hiện thành một `FilterChip` ngoài panel — người dùng đóng panel rồi vẫn phải thấy mình đang lọc gì.
- ✅ Khoá nút Áp dụng khi chưa có gì đổi.
- ✅ Lỗi tải danh mục của một trường chỉ làm hỏng trường đó.
- ❌ Không dùng popover.
- ❌ Không đặt trường lọc vào panel khi chỉ có một hai trường.
- ❌ Không để nút Áp dụng đứng trước các trường trong thứ tự Tab.
- ❌ Không đóng panel khi tải một danh mục bị lỗi.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Bộ lọc đã đặt có lưu lại cho lần mở màn sau không? Lưu thì tiện cho người làm cùng một việc mỗi ngày; không lưu thì mỗi lần vào màn là một danh sách đầy đủ, dễ đoán hơn. Liên quan trực tiếp tới việc giữ trạng thái trên URL | Dự án đầu tiên có màn danh sách thật |
| 2 | Có cần "bộ lọc đã lưu" đặt tên được không? Đây là tính năng người dùng kế toán hay xin, nhưng nó cần một endpoint và một chỗ lưu mà Core chưa có | `architect`, khi có nhu cầu thật |
| 3 | Toán tử của một trường — bằng, chứa, lớn hơn — có để người dùng chọn không, hay mỗi trường cố định một toán tử? Cho chọn thì mạnh hơn nhiều nhưng panel phức tạp gấp đôi, và phần lớn người dùng không cần | Dự án đầu tiên có màn tra cứu phức tạp |
