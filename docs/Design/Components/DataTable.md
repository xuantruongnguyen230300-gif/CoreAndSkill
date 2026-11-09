---
kind: luat
scope: core
verified: chua-doi-chieu
---

# DataTable

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có ở `src/FE`, hiện thực biến thể `paged` và **không** công tắc nào. Bảng dưới đây khai đúng những mục đã mở source ra so.

**Nền:** **bọc PrimeNG**. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 đây là ca rõ nhất của nhóm "khó": ảo hoá dòng, cột ghim, sắp xếp, phân trang phía máy chủ — vừa nhiều ca biên vừa nhạy hiệu năng.

**Đây là nơi DUY NHẤT trong toàn ứng dụng được import lưới dữ liệu của PrimeNG.** Một màn nghiệp vụ import thẳng là vi phạm [`../../RULES.md`](../../RULES.md) §7 F5, và quan trọng hơn: nó phá luôn khả năng đổi thư viện sau này.

**`DataTable` là chủ hợp đồng phân trang của màn danh sách.** Ở biến thể `paged`, dải phân trang là **phần cấu trúc của lớp bọc** ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5): `DataTable` dùng [`Pagination.md`](./Pagination.md) bên trong và vẽ nó ở **dải đáy, bên trong khung bảng** (§"Khung — khối danh sách liền một viền") — màn **không** tự đặt thêm một `Pagination` nào. Vì vậy `page`, `pageSize`, `totalRecords` và sự kiện đổi trang vào ra qua API của `DataTable` ở mục API; [`Pagination.md`](./Pagination.md) giữ hình thức, trạng thái và accessibility của chính dải đó, không giữ hợp đồng của màn. Hai nơi cùng khai một hợp đồng là cách chúng lệch nhau ([`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §5).

**Lớp bọc không import component tự dựng** — luật chiều import giữa hai tầng ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2. Vì vậy nội dung do màn quyết — khối **trống**, khung **đang tải lần đầu**, khối **lỗi** kèm nút "Thử lại" — vào qua input `TemplateRef`: màn ghép [`EmptyState.md`](./EmptyState.md) và [`SkeletonLoader.md`](./SkeletonLoader.md) vào đó. Hợp đồng slot ở mục API. Phần cấu trúc của chính bảng — như nút "Đóng hết" của công tắc `expandable` — do thư viện vẽ, tạo hình bằng token ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5).

## Đã có → còn thiếu

Component: `src/FE/src/app/shared/ui/data-table/data-table.component.ts` — selector `app-data-table`, chữ ký cột `DataColumnDef`, kiểu trạng thái `DataTableViewState`, class gốc `.data-table`.

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| Nơi duy nhất import `primeng/table` | Đúng — chú thích đầu file khai vai trò này | Chưa tự kiểm bằng lệnh là không còn chỗ nào khác import |
| API | `rows`, `columns`, `caption`, `rowKey`, `state`, nhóm phân trang (`totalRecords`, `page`, `pageSize`, `pageSizeOptions`, `paginationAriaLabel`), nhóm sắp xếp (`sortBy`, `sortDescending`), ba slot template, cột hành động (`rowActionsTemplate`, `rowActionsLabel`); output `pageChange`, `sortChange` | — |
| Ghép `Pagination` bên trong ở chân khung | **Có, và nằm trong viền** (đọc mã 2026-10-08, FE-2): viền `--color-border-strong` + `--radius-lg` nay ở `.data-table` (`data-table.component.scss` § `.data-table`, `overflow: clip`) bao cả dải đầu, vùng cuộn và `.data-table__phan-trang` (vạch `--color-border-subtle` phía trên); `<app-pagination>` chỉ vẽ khi `totalRecords() > 0` và nhận `loading` theo `state()`. Ca `một viền bao cả dải đầu, vùng cuộn và dải phân trang; các dải trong không tự vẽ viền ngoài` (`data-table.khung.spec.ts`; người soạn chưa chạy test) | Giữ nguyên |
| Chỗ cắm nội dung đầu khung (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08, FE-2): `data-table.component.html` chuỗi `<div class="data-table__dau"><ng-content select="[dataTableTop]" /></div>`; `.data-table__dau:empty { display: none }` và vạch `--color-border-subtle` dưới chỉ khi có nội dung. Ba màn danh sách đặt `Toolbar` `placement="attached"` vào đó. Ca `slot [dataTableTop] có nội dung → dải đầu + vạch ngăn; trống → không vẽ dải lẫn vạch` | Giữ nguyên |
| Vùng cuộn dọc lấp chiều cao, `th` dính đỉnh (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08, FE-2): `data-table.component.scss` — `:host` và `.data-table` là cột flex `flex: 1 1 0px`; `.data-table__than` có `min-height: var(--data-table-scroll-h-floor, auto)` — biến sàn do mixin `man-danh-sach` (`src/FE/src/styles/_spacing.scss` § `@mixin man-danh-sach`) đặt bằng `var(--data-table-scroll-h-min)` trên host của màn, bảng không nằm trong màn gọi mixin thì sàn là `auto` (đọc lại 2026-10-08, sau lượt sửa cùng ngày); `.data-table__khung` là vùng cuộn duy nhất (`overflow: auto`); `th` khai `position: sticky; top: 0` với `z-index: var(--z-sticky)`. Token `--data-table-scroll-h-min` khai ở `src/FE/src/styles/_spacing.scss`; mixin `man-danh-sach` cho màn gọi. Ca `khung lấp chiều cao vùng chứa; CHỈ vùng cuộn cuộn dọc (trang không tràn)`, `vùng chứa thấp hơn sàn → vùng cuộn giữ sàn…`, `tiêu đề cột dính đỉnh vùng cuộn…` (`data-table.khung.spec.ts`; người soạn chưa chạy test) | Hình thức thật chưa đo trên trình duyệt |
| Cột hành động dùng chung — `rowActionsTemplate`, `rowActionsLabel` (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08, FE-2): `data-table.component.ts` khai `rowActionsTemplate`, `rowActionsLabel`, kiểu ngữ cảnh `RowActionsContext` (`$implicit`, `compact`); có template mà thiếu nhãn thì ném lỗi (`DataTable: rowActionsLabel bắt buộc…`). `data-table.component.html` vẽ `<th class="data-table__o-hanh-dong">` mang `<span class="sr-only">` và một `<td>` mỗi dòng. `data-table.component.scss` § `.data-table__o-hanh-dong` ghim mép phải (`position: sticky; right: 0`), nền đặc; § `.data-table__hanh-dong` ẩn bằng `opacity: 0` (không `display: none`), hiện khi `:focus-within`, `:has([aria-expanded='true'])`, `@media (hover: hover)` rê dòng, và luôn hiện khi `(hover: none)` hoặc dưới `$bp-xs`; `compact` đổi theo `matchMedia` dưới `$bp-xs`. Cả ba màn danh sách Core truyền `[rowActionsTemplate]` (`danh-sach-nguoi-dung.page.html`, `danh-sach-vai-tro.page.html`, `danh-sach-don-vi.page.html`). Ca ở khối `describe('DataTableComponent — cột hành động dùng chung (rowActionsTemplate)'` trong `data-table.khung.spec.ts` — luôn là cột cuối, thiếu nhãn ném lỗi, ghim phải, nghỉ ẩn bằng opacity vẫn trong thứ tự Tab, không transition, `:focus-within` hiện, `aria-expanded="true"` giữ hiện, `compact` theo truy vấn (người soạn chưa chạy test) | Hình thức thật (bề rộng cột, nền đặc ở mọi trạng thái hover) **chưa đo trên trình duyệt**. Dạng thẻ dưới `$bp-xs` của chính bảng chưa soát |
| Biến thể `paged` | Có, là hình dạng duy nhất dựng | — |
| Biến thể `scroll` | — | Chưa dựng. Không có input `variant` nào |
| Sáu công tắc (`selectable`, `frozen`, `tree`, `grouped`, `expandable`, `summary`) | — | **Chưa dựng cái nào.** Chú thích component khai không màn nào cần. Toàn bộ mục §Lưới cây và §Dòng tổng của spec vì thế chưa có gì để đối chiếu |
| Sắp xếp | Có: `th` sắp xếp được bọc một `<button>` thật, `aria-sort` tính đúng ba giá trị, đổi cột thì về `ascending` trước; icon đổi theo `pi-sort-alt` / `pi-sort-amount-up-alt` / `pi-sort-amount-down` (`data-table.component.html` § `[class.pi-sort-amount-up-alt]`) — khớp bảng [`../Icons.md`](../Icons.md) §5, đối chiếu 2026-09-22 | — |
| `scope="col"` trên mọi `th` | Có | — |
| Tên truy cập của bảng cho trình đọc màn hình | Có (đối chiếu 2026-09-22) — `data-table.component.ts` § `ptBang` dựng `{ table: { 'aria-label': … } }` từ input `caption` và truyền vào `p-table` qua § `[pt]="ptBang()"`, nên `aria-label` nằm **thẳng trên `<table>`**. Slot `#caption` không còn dùng: chú thích trong `data-table.component.ts` ghi lý do — bản PrimeNG đang cài chiếu slot đó vào một `div` **ngoài** `<table>`, một `<caption>` đặt ở đó không thuộc bảng. Vùng cuộn bao ngoài vẫn có tên riêng (`role="region"` + `aria-label`) | — (đủ theo dòng "Tên truy cập của bảng" ở §Accessibility) |
| Khung cuộn nhận được focus | Có, `tabindex="0"` + `role="region"` + `aria-label` — đúng khuôn cho vùng cuộn ngang | — |
| `aria-busy` khi đang tải | Có, trên `p-table` | — |
| Vùng báo kết quả (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08): `data-table.component.html` chuỗi `<div class="sr-only" role="status">`, đặt ngoài mọi nhánh `@if` — có mặt từ lần vẽ đầu, kể cả khi tổng bằng 0. Câu tính ở `data-table.component.ts` § `cauKetQua` (`linkedSignal`): `'loading'` giữ câu cũ; `'error'` → `chung.bang.khongTaiDuoc`; `'empty'` / `'empty-filtered'` → `chung.bang.khongCoKetQua`; `'idle'` → `chung.bang.ketQua` với `tu` / `den` / `tong` tính từ `page`, `pageSize`, `totalRecords`. `src/FE/public/i18n/vi.json` nhóm `chung.bang` có đủ ba khoá, câu khớp bảng câu ở §Accessibility. `aria-live` đã gỡ khỏi `Pagination` ([`Pagination.md`](./Pagination.md) bảng đầu tệp). Ca ở khối `describe('DataTableComponent — vùng báo kết quả'` trong `data-table.component.spec.ts`: có mặt từ lần vẽ đầu, ba nhánh câu, giữ câu khi `'loading'`, `dải Pagination bên trong không còn aria-live` (người soạn chưa chạy test). Luật "màn và component khác không đặt vùng báo kết quả" kiểm bằng lệnh `grep -rn 'aria-live\|role="status"' src/FE/src/app --include=*.html` — PASS khi không dòng nào thuộc màn danh sách, `Toolbar`, `FilterPanel`, `FilterChip`, `EmptyState`; chạy 2026-10-08: PASS | Trình đọc màn hình thật đọc ra sao **chưa nghe thử** |
| Ba trạng thái rỗng/lỗi/tải đi qua slot | Có, đúng luật spec: một `#emptymessage` duy nhất phân nhánh `loadingTemplate` / `errorTemplate` / `emptyTemplate`, và slot trống nhận `state()` làm ngữ cảnh nên màn phân biệt được `'empty'` với `'empty-filtered'` | — |
| Tiêu đề cột giữ nguyên khi lỗi/rỗng | Có — `#header` luôn vẽ, ba trạng thái chỉ thay phần thân | — |
| Hai ca `loading` | Phân biệt đúng ở mức logic: `dangTai` (lần đầu, chưa có dòng) dùng `loadingTemplate`; `dangTaiLai` (đã có dòng) chỉ gắn class `--tai-lai` để giữ nguyên dữ liệu cũ | Chưa kiểm `.data-table__khung--tai-lai` có thật sự phủ `--color-scrim` + spinner và khoá thao tác như §Trạng thái đòi — mới thấy tên class, chưa soát nội dung quy tắc |
| Ẩn cột ở màn hẹp | `DataColumnDef` khai `hideBelow` và `priority`; template phát `[attr.data-priority]` lên `th` và `td` | ⚠️ **Không có tác dụng.** Không có quy tắc CSS nào đọc `data-priority` trong toàn bộ `src/FE`, và `hideBelow` **không được đọc ở đâu cả** — kể cả trong template. Chú thích component khai việc này "xử lý bằng CSS thuần", nhưng phần CSS đó chưa tồn tại. Đây **không** phải khoảng trống lý thuyết: `danh-sach-nguoi-dung.page.ts` đã khai `hideBelow: 'md'` và `hideBelow: 'sm'` cho hai cột thật, và hai cột đó hiện **không** ẩn ở màn hẹp |
| `width` và `align` của cột | `width` gắn qua `[style.width]`; `align: 'end'` gắn class `.data-table__o--end` | — |
| §Token dùng — `--dur-fast` | Có (đối chiếu 2026-10-06, chỉ đọc mã) — chuyển tiếp màu đi qua **một** khoá semantic: `src/FE/src/app/core/theme/prime-preset.ts` chuỗi `transitionDuration: 'var(--dur-fast)',` (ADR-0073, [`../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md)). Sub-preset của component trỏ về `{transition.duration}` hoặc `{form.field.transition.duration}`, nhánh sau cũng đổ về khoá này. Không khai biến riêng từng component — phương án đó đã bị ADR-0073 loại | Giá trị thật `120ms` trên trình duyệt **chưa đo**; mới đối chiếu mã |
| Chuyển động **không** tới được bằng token — nhưng chưa dùng tới (đối chiếu 2026-09-23) | `primeng/table` còn một hoạt ảnh ghi cứng, không qua input nào: khối `animations:` của `ColumnFilter` § `animate('.12s cubic-bezier(0, 0, 0.2, 1)')` vào và § `animate('.1s linear'` ra. Hai con số đó là literal trong mã thư viện | ✅ **Không phải lỗi đang hoạt động.** Chúng thuộc lớp nổi lọc-theo-cột, mà lớp bọc Core không dựng: không chỗ nào trong `shared/ui/data-table/` dùng `p-columnFilter`, và spec cũng không khai tính năng lọc theo cột. Ghi ở đây để lần sau ai mở tính năng đó thì biết trước là thời lượng của nó **không đặt được** |
| `rowKey` | Có (đối chiếu 2026-09-24) — dòng track theo khoá: `data-table.component.html` § `[rowTrackBy]="khoaHang"` truyền hàm `data-table.component.ts` § `khoaHang` (đọc trường `rowKey()` của dòng) vào `p-table`, thay mặc định track theo tham chiếu của thư viện; § `[dataKey]="rowKey()"` dùng cùng khoá. Ca kiểm trong `data-table.component.spec.ts`: § `tải lại trả object MỚI cùng khoá` (mọi `<tr>` giữ nguyên phần tử, nút đang focus giữ focus) và § `xoá một dòng rồi tải lại` (track theo khoá, không theo vị trí) | — |

Chưa đối chiếu — **lỗ mù lớn của file này**: toàn bộ hình thức ở §Trạng thái (`default` nền dòng và vạch ngăn, `th` nền `--color-surface-2` dính đỉnh, `hover`, `focus-visible` với `outline-offset` âm, dòng `active`, dòng `disabled`) chưa soát — phần lớn đến từ sub-preset `table` của PrimeNG, và tôi mới mở SCSS của component chứ chưa mở preset. Cũng chưa đối chiếu §Responsive, §Accessibility ngoài các khoản ghi ở bảng, và mọi luật thuộc các công tắc chưa dựng.

---

## Mục đích

Hiển thị một tập bản ghi lớn với phân trang, sắp xếp và chọn dòng — trong một khung có chiều cao cố định và cuộn bên trong.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Danh sách bản ghi từ máy chủ, có phân trang | 🛑 Bảng tĩnh vài dòng, không phân trang, không sắp xếp → [`Table.md`](./Table.md). Kéo cả lưới dữ liệu vào chỉ để hiện năm dòng là trả giá hiệu năng cho không |
| Danh sách cần sắp xếp theo cột | 🛑 Danh sách không phải dạng bảng (thẻ, dòng thời gian) → dựng bằng [`Card.md`](./Card.md) |
| Danh sách cần chọn nhiều dòng để thao tác hàng loạt | 🛑 Ma trận tick quyền — dùng [`Table.md`](./Table.md) + [`Check.md`](./Check.md), vì nó không phân trang và mọi ô đều tương tác |
| Bảng cần ghim cột đầu khi cuộn ngang | 🛑 So sánh hai bản ghi cạnh nhau → bố cục riêng của màn |

## Biến thể

Hai **biến thể** loại trừ nhau — chúng quyết định dữ liệu về bằng cách nào:

| Biến thể | Khác gì | Dùng khi |
| --- | --- | --- |
| `paged` | Phân trang phía máy chủ, [`Pagination.md`](./Pagination.md) ở chân | **Mặc định.** Mọi danh sách bản ghi |
| `scroll` | Cuộn vô hạn, tải thêm khi tới đáy | Chỉ khi thứ tự đọc là tuần tự (nhật ký, dòng sự kiện). 🛑 Không dùng cho danh sách người ta cần quay lại một mục cụ thể — cuộn vô hạn làm mất chỗ. Không có ngưỡng số dòng nào để đổi sang `scroll`: chọn theo bản chất dữ liệu, mặc định luôn là `paged` |

Sáu **công tắc** cộng thêm vào, kết hợp được với cả hai biến thể:

| Công tắc | Khác gì | Dùng khi |
| --- | --- | --- |
| `selectable` | Cột ô đánh dấu ở đầu, có thanh hành động hàng loạt | Khi có thao tác áp cho nhiều bản ghi |
| `frozen` | Ghim cột đầu khi cuộn ngang. Cột hành động **luôn** ghim mép phải, không cần công tắc — §"Cột hành động" | Bảng nhiều cột trên màn hẹp |
| `tree` | Dòng có dòng con, xoè và thu được; ô đầu tiên thụt lề theo cấp | Cây đơn vị, hệ thống tài khoản, danh mục phân cấp, địa bàn |
| `grouped` | Dòng gộp theo một cột, mỗi nhóm có dòng tiêu đề mang số cộng dồn | Bảng lương gộp theo phòng, chi phí theo khoản mục |
| `expandable` | Bấm một dòng bung ra một khối chi tiết ngay dưới nó | Xem nhanh dòng chi tiết, hoặc chứa các cột đã bị ẩn ở màn hẹp |
| `summary` | Một dòng tổng khoá cứng ở đáy khung | Bất cứ bảng nào có cột số cần cộng. **Độc lập với `grouped`** — xem dưới |

**Vì sao tách biến thể khỏi công tắc:** [`../COMPONENTS.md`](../COMPONENTS.md) §2.3 cảnh báo một component quá năm biến thể gần như chắc chắn đang gánh hai vai. Nhưng những thứ ở trên không phải bấy nhiêu vai — chỉ hai biến thể đầu loại trừ nhau, các công tắc còn lại cộng dồn được. Một bảng `paged` + `selectable` + `tree` là hợp lệ và có thật. Gọi tất cả là "biến thể" làm luật ngưỡng năm báo động nhầm.

### Công tắc `tree` — mười hai quyết định đi kèm

Đây là công tắc nặng nhất, và phần lớn cái khó của nó không nhìn thấy được ở ảnh tĩnh:

| Câu hỏi | Quyết định |
| --- | --- |
| Thụt lề mỗi cấp | `--tree-indent` ([`../DESIGN.md`](../DESIGN.md) §6.1). Cùng giá trị với [`TreeSelect.md`](./TreeSelect.md), để hai chỗ hiện cùng một cây trông giống nhau |
| Nút xoè đặt đâu | Trước nội dung ô đầu. **Nút lá vẫn chiếm chỗ đúng bằng nút xoè** (`visibility: hidden`, không phải `display: none`) — bỏ hẳn chỗ thì nhãn của lá lệch trái so với nhãn của cha cùng cấp, và mắt đọc thành hai cấp khác nhau |
| Đường nối dọc | Có, một vạch `--color-border-subtle` mỗi cấp. Từ cấp ba trở lên, không có nó thì không đếm được mình đang ở nhánh nào. Dùng bậc `subtle` vì nó là vạch **bên trong** một bề mặt |
| Chọn cha có kéo theo con | Có, và cha thành nửa chọn khi con chọn một phần — dùng lại trạng thái `indeterminate` của [`Check.md`](./Check.md). 🛑 Gửi lên máy chủ **cha** hay **danh sách con** là quyết định nghiệp vụ, màn hình phải khai rõ |
| Sắp xếp | **Không phẳng hoá cây.** Chỉ sắp xếp giữa các anh em cùng một cha. Phẳng hoá khi bấm sắp xếp là cách nhanh nhất làm người dùng mất phương hướng |
| Lọc | **Giữ lại cha không khớp**, hiện mờ, để thấy đường dẫn tới kết quả. Cha giữ vì *đường dẫn*, không vì nó khớp — nên **không tính vào số kết quả** |
| Phân trang | Theo **nút gốc**, con luôn đi cùng cha. Cắt ngang một nhánh giữa hai trang là vô nghĩa. Chỉ báo phải nói "trên 6 **đơn vị gốc**", không nói "trên 6 bản ghi" |
| Nhánh chưa tải | Nút xoè đổi thành `pi-spinner`, `aria-busy` trên chính nút đó |
| Cha hoá ra không có con | Sau khi tải xong, nút xoè **biến thành dạng lá**. Để lại một nút bấm mãi không ra gì là lỗi người dùng báo nhiều nhất ở lưới cây |
| Cột đầu | **Bắt buộc ghim** khi có cuộn ngang. Cột cây trôi đi thì thụt lề mất hết nghĩa — người dùng thấy số liệu mà không biết của đơn vị nào |
| Dòng cha ở cột số | Tổng của các con, màu `--color-text-muted` và đậm để phân biệt với số liệu của chính nó. Cha vừa có số riêng vừa có tổng con thì **phải tách hai cột** — trộn vào một là chỗ sai không ai phát hiện cho tới lúc đối chiếu sổ |
| Nhớ trạng thái mở | Trong một phiên làm việc, theo **khoá bản ghi** chứ không theo chỉ số dòng. Không nhớ qua lần đăng nhập khác — trạng thái cũ thường không còn đúng với dữ liệu mới |

### Công tắc `grouped` — dòng tiêu đề nhóm

Mỗi nhóm mở đầu bằng một dòng tiêu đề mang **số cộng dồn của riêng nhóm đó**, xoè và thu được. Đây là số của nhóm, khác hẳn dòng tổng của cả bảng — dòng tổng thuộc công tắc `summary`.

### Công tắc `summary` — dòng tổng khoá cứng ở đáy

**Dòng tổng là một công tắc riêng, không phải một phần của `grouped`.** Một bảng phẳng có phân trang vẫn cần dòng tổng, và đó là ca phổ biến hơn cả. Trói nó vào `grouped` là để một bảng phẳng không có đường nào bật nó lên.

#### Ghim thế nào — và vì sao `<tfoot>` một mình không đủ

🛑 **`position: sticky; bottom: 0` trên `<tfoot>` KHÔNG đủ.** Nó chỉ ghim khi bảng **cao hơn** vùng cuộn; bảng ngắn hơn khung thì dòng tổng nằm lửng ngay sau dòng dữ liệu cuối, giữa khung là một khoảng trống. Đó không phải "khoá cứng ở đáy".

Dòng tổng vì vậy **nằm ngoài vùng cuộn dọc** — nó là một dải riêng của khung bảng, đặt dưới vùng cuộn và trên chân khung. Hệ quả phải chấp nhận, nói thẳng:

| Cái giá | Nghĩa là |
| --- | --- |
| Hai bảng thay vì một | Dải tổng là bảng thứ hai, nên cả hai phải `table-layout: fixed` và dùng **cùng một `colgroup`**; nếu không, cột số của hai bảng lệch nhau |
| Phải đồng bộ cuộn ngang bằng script | Thân bảng cuộn sang phải thì dải tổng phải cuộn theo đúng bấy nhiêu. Không đồng bộ thì con số nằm dưới sai cột — một lỗi trông như lỗi dữ liệu |
| Công tắc `frozen` giao với dải tổng | Cột đầu ghim thì ô đầu của dải tổng cũng phải ghim cùng vị trí. Đây là giao điểm khó nhất của bố cục bảng và là chỗ phải kiểm kỹ nhất khi dựng |

Dải tổng dùng nền `--color-surface-3`, viền trên `--border-w-strong` màu `--color-border`, chữ `--fw-bold`. Nó nằm **trong** khung bảng, ngay dưới vùng cuộn; dải phân trang của [`Pagination.md`](./Pagination.md) cũng nằm trong khung, **dưới** dải tổng (chốt 2026-10-08 — §"Khung"). Hai dải ngang liền nhau ở đáy khung phân biệt bằng hình thức của chính chúng: dải tổng nền `--color-surface-3`, viền trên đậm, chữ đậm — dải phân trang nền `--color-surface`, ngăn bằng vạch `--color-border-subtle`, chữ thường.

#### Nhãn dòng tổng phải nói rõ PHẠM VI

🛑 **Đây là ràng buộc cứng, không phải gợi ý.** Trên một bảng có phân trang, "Tổng cộng" có thể là tổng của **trang đang xem** hoặc tổng của **toàn bộ kết quả sau lọc**. Hai số khác nhau, trông giống hệt nhau, và người dùng đối chiếu sổ sách sẽ lấy nhầm.

Nhãn vì vậy luôn mang phạm vi: **"Tổng cộng 137 bản ghi"** hoặc **"Tổng cộng trang này"**. Không bao giờ chỉ hai chữ "Tổng cộng".

Khi bộ lọc đổi mà tổng là tổng toàn bộ kết quả, con số phải đổi theo — một dòng tổng không phản ứng với bộ lọc là một dòng tổng nói dối.

#### Tổng do máy chủ tính

🛑 **Không cộng ở trình duyệt.** Bảng chỉ hiện một trang; cộng những gì đang hiện ra một dòng "Tổng cộng" là một con số sai trông rất giống số đúng. Máy chủ trả về số tổng cùng với trang dữ liệu, và component nhận nó qua `summary` ở mục API.

### Công tắc `expandable` — mở nhiều dòng cùng lúc

Khối chi tiết có dải trái `--border-w-accent` màu `--color-brand` để mắt nối nó với dòng cha. **Cho mở nhiều dòng cùng lúc**, kèm nút "Đóng hết" do `DataTable` vẽ trong một dải ngay trên hàng tiêu đề cột. Dải có mặt suốt lúc bật công tắc; nút chỉ bấm được khi có ít nhất một dòng đang mở — dải không hiện ra rồi biến mất theo số dòng mở, để khung bảng không nhảy. Nút mượn hình thức của [`Button.md`](./Button.md) theo [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5. Cho mở một dòng thì bảng gọn hơn nhưng mất khả năng đối chiếu hai bản ghi, và đối chiếu đúng là lý do người ta mở chi tiết.

## Khung — khối danh sách liền một viền — định nghĩa gốc

Chốt 2026-10-08 (người dùng): ở màn danh sách, `Toolbar`, bảng và dải phân trang là **một khối** chung một đường viền, không phải ba hộp xếp chồng. Khung đó là của `DataTable`: nó vẽ đường viền ngoài và mọi vạch ngăn giữa các dải; các dải bên trong không vẽ đường nào của riêng mình.

```text
┌ khung — viền --border-w --color-border-strong, --radius-lg, nền --color-surface ┐
│ chỗ cắm đầu khung — màn đặt Toolbar placement="attached" (tuỳ chọn)               │
├─ vạch --border-w --color-border-subtle ─────────────────────────────────────────┤
│ dải "Đóng hết" — chỉ khi bật công tắc expandable                                  │
├─ vạch --color-border-subtle ────────────────────────────────────────────────────┤
│ vùng cuộn — th dính đỉnh (--z-sticky) + thân bảng; cuộn dọc và ngang;            │
│             cột hành động ghim mép phải                                           │
├─ viền trên --border-w-strong --color-border (đã khai ở công tắc summary) ───────┤
│ dải tổng — chỉ khi bật công tắc summary                                           │
├─ vạch --color-border-subtle ────────────────────────────────────────────────────┤
│ dải phân trang — Pagination, chỉ ở paged và khi totalRecords > 0                 │
└──────────────────────────────────────────────────────────────────────────────────┘
```

| Khoản | Quy định |
| --- | --- |
| Đường viền ngoài | Một viền `--border-w` `--color-border-strong`, bo `--radius-lg`, bao **mọi** dải. Khung cắt nội dung theo bo góc; lớp nổi mở từ trong khung (`Menu` của dòng, lớp nổi ô chọn) dựng qua portal nên không bị cắt |
| Vạch ngăn | `--border-w` `--color-border-subtle` giữa hai dải liền nhau — bậc `subtle` vì đó là vạch **bên trong** một bề mặt ([`../DESIGN.md`](../DESIGN.md) §2.3). Dải nào vắng thì vạch của nó vắng theo — không bao giờ có hai vạch liền nhau |
| Chỗ cắm đầu khung | Slot nội dung `[dataTableTop]` — màn đặt [`Toolbar.md`](./Toolbar.md) với `placement="attached"` vào đó. Đây là vùng đã là slot nội dung của màn, nên màn đặt thẳng component tự dựng, không cần `TemplateRef` ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5, gạch đầu dòng cuối). `DataTable` không biết trong slot có gì; slot trống thì không vẽ dải lẫn vạch |
| Dải phân trang | Đệm `--sp-3` dọc, `--sp-5` ngang — cùng đệm ngang với `Toolbar` cỡ `md`, để mép trái chữ ở dải đầu và dải đáy thẳng hàng. Hình thức, trạng thái và accessibility của chính dải: [`Pagination.md`](./Pagination.md) |
| Chiều cao | Khung **lấp chiều cao vùng chứa** do trang cấp. Dải đầu, dải "Đóng hết", dải tổng, dải phân trang cao theo nội dung; vùng cuộn ăn phần còn lại, **không thấp hơn** `--data-table-scroll-h-min`. Trang không cấp chiều cao — bảng con trong một màn cuộn — thì khung cao theo nội dung và thân bảng không cuộn dọc |
| Vùng cuộn | Vùng cuộn **duy nhất** của khung, cả dọc lẫn ngang; `th` dính đỉnh khi cuộn dọc. Tên truy cập và khả năng nhận focus: §Accessibility, dòng "Vùng cuộn" |

**Vì sao khung thuộc `DataTable` chứ không thuộc màn:** màn tự vẽ viền quanh ba component thì mỗi màn vẽ một kiểu — đúng lỗi "một khung bảng định nghĩa hai lần" mà [`../COMPONENTS.md`](../COMPONENTS.md) §1 cấm. `DataTable` đã là chủ của khung bảng và của dải phân trang; thêm một chỗ cắm ở đầu là phần mở rộng nhỏ nhất làm cả khối thành một.

## Cột hành động — hợp đồng dùng chung — định nghĩa gốc

Chốt 2026-10-08 (người dùng). Mọi màn danh sách có thao tác trên **một** dòng dùng đúng một hợp đồng: **lớp bọc lo vị trí và luật hiện/ẩn; màn lo nút nào**, đưa vào qua `rowActionsTemplate` ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 — lớp bọc quyết *khi nào* và *ở đâu*, màn quyết *hiện gì*). Màn không khai cột hành động như một cột thường trong `columns`, và không tự viết luật ghim hay rê chuột.

| Khoản | Quy định |
| --- | --- |
| Có mặt khi | `rowActionsTemplate` khác `null`. Người xem không có quyền nào cho bất kỳ hành động dòng nào → màn truyền `null`, cột không vẽ, kể cả `th` |
| Vị trí | Luôn là cột **cuối**, sau mọi cột của `columns` kể cả cột dự án thêm qua seam. **Ghim mép phải** bất kể công tắc `frozen` — bảng cuộn ngang thì nút vẫn với tới được. Lớp `--z-sticky` |
| Bề rộng | Vừa khít nội dung, không co giãn, không xuống dòng |
| Nền ô | Đặc, theo nền của dòng ở mọi trạng thái (`default`, `hover`, `active`) — nội dung cuộn ngang trượt **dưới** cột, không nhìn xuyên qua |
| Tiêu đề cột | `th scope="col"` mang `rowActionsLabel` **ẩn thị giác**: ô nhìn trống, trình đọc màn hình vẫn đọc được tên cột |
| Mức hiện — nghỉ | Nút trong ô ẩn bằng `opacity` — dòng không nhảy, nút **vẫn** trong cây accessibility và trong thứ tự Tab. 🛑 Không `display: none`, không `visibility: hidden`: hai cách đó rút nút khỏi đường Tab, người dùng bàn phím không bao giờ tới được |
| Mức hiện — hiện | Dòng đang được rê chuột (bọc `@media (hover: hover)`), **hoặc** có phần tử trong dòng nhận focus (`:focus-within`), **hoặc** một nút trong ô đang giữ lớp nổi mở (`aria-expanded="true"`). Điều kiện thứ ba không thừa: [`Menu.md`](./Menu.md) dựng qua portal nên focus rời dòng khi menu mở — thiếu nó thì nút neo biến mất ngay dưới chính menu nó vừa mở |
| Thiết bị không rê chuột | `@media (hover: none)` → **luôn hiện**. Không có hover thì không còn đường nào khác để thấy nút |
| Chuyển động | Hiện và ẩn **tức thì**, không transition: rê chuột dọc bảng mà mỗi dòng mờ dần sẽ để lại một vệt nút mờ trên những dòng vừa đi qua |
| Dưới `$bp-xs` | Dạng thẻ (§Responsive): lớp bọc đặt `compact: true` trong ngữ cảnh template; màn vẽ **một** nút ⋯ mở `Menu` gồm **mọi** hành động, kể cả hành động đang là nút riêng ở khổ rộng. Nút luôn hiện, ở góc trên phải của thẻ |
| Khuôn nút trong ô | [`IconButton.md`](./IconButton.md) `ghost` cỡ `sm`. Nhiều nhất **hai** nút nhìn thấy: hành động hay dùng nhất của dòng, và một nút ⋯ (icon `ellipsis-v`, [`../Icons.md`](../Icons.md) §5) mở [`Menu.md`](./Menu.md) `anchored` cỡ `sm` cho phần còn lại. Mỗi nút mang `aria-label` nêu **bản ghi nào** |
| Lối vào chi tiết | **Không** là nút trong cột này — nó là liên kết ở ô định danh ([`Button.md`](./Button.md) §Khi nào dùng). Không có nút "xem" |
| Bấm trong ô | Không phát `rowClick` — luật chung của output đó (§API) |

**Vì sao hiện khi rê chuột thay vì luôn hiện:** một cột nút lặp lại trên mọi dòng là nhiễu thị giác lớn nhất của một bảng quản trị, và nó kéo mắt khỏi dữ liệu. Cái giá — người mới chưa biết có nút cho tới khi rê qua một dòng — được trả bằng ba lối luôn mở: thiết bị cảm ứng luôn thấy, người dùng bàn phím thấy ngay khi Tab vào dòng, và dạng thẻ ở màn hẹp luôn hiện.

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Chiều cao vùng cuộn | Phần còn lại của khung sau các dải — khung lấp chiều cao trang cấp (§"Khung"); sàn `--data-table-scroll-h-min` ([`../DESIGN.md`](../DESIGN.md) §6.5) |
| Chiều cao một dòng | `--size-control-md` (34px) + đệm dọc `--sp-3` |
| Đệm ô | `--sp-3` dọc / `--sp-4` ngang |
| Cỡ chữ ô | `--fs-sm` |
| Cỡ chữ tiêu đề cột | `--fs-xs`, `--fw-semibold`, màu `--color-text-muted` |
| Bo góc khung | `--radius-lg` |
| Viền khung | `--border-w` `--color-border-strong` |

**Chiều cao của khung là quyết định của trang, không phải của component.** Đặt một con số cố định trong `DataTable` nghe tiện và sai: phần nằm trên khung (`PageHeader`, có khi thêm `NoticeBanner`) khác nhau ở mỗi màn. Màn danh sách cấp chiều cao bằng một chuỗi flex để khung ăn hết chỗ còn lại của `main` ([`../Templates/ListScreen.md`](../Templates/ListScreen.md) §4); `DataTable` chỉ giữ **sàn** của vùng cuộn, không giữ chiều cao nào khác.

**Vì sao khung dùng `--color-border-strong` chứ không `--color-border`:** khung bảng là vùng nội dung **tương tác được** — bấm sắp xếp, tick chọn, cuộn. Nó cùng nhóm với ô nhập trong luật ba bậc viền ở [`../DESIGN.md`](../DESIGN.md) §2.3.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Dòng nền `--color-surface`; vạch giữa hai dòng `--color-border-subtle`; `th` nền `--color-surface-2`, dính đỉnh khi cuộn (`--z-sticky`) | Có |
| `hover` | Dòng đổi nền `--color-surface-2`; con trỏ đổi thành `pointer` **chỉ khi** dòng bấm được. Bọc trong `@media (hover: hover)` | Có |
| `focus-visible` | Dòng nhận focus có `outline` `--color-focus` **bên trong** (`outline-offset: -2px`) — vòng ngoài sẽ bị cắt bởi vùng cuộn. `th` sắp xếp được nhận vòng focus như một nút | Có |
| `active` | Dòng đang được chọn: nền `--color-brand-subtle`, dải `--border-w-accent` `--color-brand` ở mép trái. **Cần cả hai** — nền chênh `--color-surface` chỉ 1.25:1 nên không đủ làm tín hiệu duy nhất ([`../DESIGN.md`](../DESIGN.md) §2.4) | Có |
| `disabled` | Dòng không thao tác được: chữ `--color-text-disabled`, không hover, ô đánh dấu bị khoá. **Vẫn đọc được** — không giảm opacity cả dòng | Có |
| `loading` | **Hai ca khác nhau.** *Lần đầu* — chưa có dòng nào để giữ lại: thân bảng hiện **slot đang tải** (`loadingTemplate`), màn ghép [`SkeletonLoader.md`](./SkeletonLoader.md) dạng hàng, đúng số dòng của trang. *Đổi trang / sắp xếp lại:* `DataTable` tự giữ nguyên dữ liệu cũ, phủ `--color-scrim` + spinner, khoá thao tác. `aria-busy="true"` cả hai ca | Có |
| `error` | Thân bảng hiện **slot lỗi** (`errorTemplate`): màn ghép [`EmptyState.md`](./EmptyState.md) biến thể `error` kèm nút "Thử lại" gọi thẳng việc tải lại của màn. Tiêu đề cột **giữ nguyên** — chúng cho biết đây là bảng gì | Có |
| `empty` | **Hai ca phải phân biệt**, và thân bảng hiện **slot trống** (`emptyTemplate`) cho cả hai. Màn chọn nội dung theo `state`: *Chưa có bản ghi nào* (`'empty'`) → [`EmptyState.md`](./EmptyState.md) biến thể `first-use`, mời tạo mới. *Lọc không ra kết quả* (`'empty-filtered'`) → biến thể `no-results`, nút "Xoá bộ lọc", **không** mời tạo mới. Tiêu đề cột giữ nguyên | Có |

**Ca `loading` khi đổi trang là chỗ hay làm sai nhất.** Thay dữ liệu cũ bằng skeleton mỗi lần bấm sang trang làm bảng nhấp nháy và cao thấp thất thường. Giữ dữ liệu cũ + phủ mờ giữ cho khung ổn định, và người dùng vẫn thấy mình đang ở đâu.

**Hai ca `empty` trộn làm một là lỗi hay gặp nhất ở màn danh sách.** Người dùng vừa gõ một bộ lọc và thấy màn hình mời "Thêm bản ghi đầu tiên" sẽ tưởng dữ liệu đã mất.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-subtle`, `--color-border-strong`, `--color-brand`, `--color-brand-subtle`, `--color-focus`, `--color-scrim` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fw-semibold`, `--fw-regular`, `--lh-snug` |
| Khoảng cách | `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-lg`, `--border-w`, `--border-w-strong`, `--border-w-accent` |
| Kích thước | `--size-control-md`, `--size-control-sm`, `--icon-sm`, `--icon-md`, `--tree-indent`, `--data-table-scroll-h-min` (tầng 3, [`../DESIGN.md`](../DESIGN.md) §6.5) |
| Công tắc | `--data-table-scroll-h-floor` (tầng 3, [`../DESIGN.md`](../DESIGN.md) §6.5) — bật sàn vùng cuộn của §"Khung", dòng *Chiều cao*. `DataTable` chỉ đọc; chỉ mixin `man-danh-sach` được đặt nó, màn không tự đặt |
| Lớp | `--z-sticky` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Mọi cột hiện; cột hành động ghim mép phải |
| `$bp-md` … `$bp-lg` | Ẩn các cột đánh dấu `priority: low`; bảng bắt đầu cuộn ngang |
| < `$bp-md` | Chỉ giữ cột định danh + một cột trạng thái + cột hành động; ghim cột đầu |
| < `$bp-xs` | Chuyển sang dạng thẻ: mỗi bản ghi một khối, nhãn cột thành nhãn trường. Dạng thẻ là hành vi responsive của chính `DataTable`, không phải component riêng ([`../COMPONENTS.md`](../COMPONENTS.md) §1 — mở rộng, không đẻ mới); ngữ nghĩa bảng giữ như [`Table.md`](./Table.md) §Accessibility dòng dạng thẻ. Cột hành động gom về **một** nút ⋯ luôn hiện ở góc trên phải mỗi thẻ (§"Cột hành động", ngữ cảnh `compact`) |

🛑 **Cột bị ẩn phải nói rõ đi đâu.** Thông tin không được biến mất — hoặc nó có ở màn chi tiết, hoặc nó hiện khi mở rộng dòng. Ẩn mà không nói đi đâu là mất dữ liệu ở màn nhỏ.

**Vì sao cần dạng thẻ chứ không chỉ cuộn ngang:** một bảng bảy cột trên màn 390px phải cuộn ba lần mới đọc hết một dòng, và người dùng mất mốc dòng nào là dòng nào. Cái giá của dạng thẻ: một bản ghi chiếm nhiều chiều dọc, so sánh giữa các bản ghi khó hơn hẳn. Đó là đánh đổi đúng ở khổ màn này.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<table>` thật với `<thead>`, `<tbody>`, `<th scope="col">`. 🛑 Không dựng bảng bằng `<div role="grid">` khi dữ liệu là bảng thật |
| Tên truy cập của bảng | `<table>` **phải có** tên truy cập, ẩn về mặt hình ảnh nếu tiêu đề đã hiện ở `PageHeader`. Cơ chế: `aria-label` đặt **thẳng trên `<table>`** qua pass-through của thư viện, từ input `caption`. Không dùng `<caption>` — đổi ngày 2026-09-22: slot chú thích của bản PrimeNG đang cài nằm ngoài `<table>`, nên `<caption>` đặt qua slot đó không thuộc bảng và tên không tới được trình đọc màn hình. Tên trên vùng cuộn (`role="region"`) là tên **khác**, không thay được tên của bảng |
| Sắp xếp | `th` sắp xếp được chứa một `<button>`; `aria-sort` nhận `ascending` / `descending` / `none` |
| Chọn dòng | Ô đánh dấu có `aria-label` nêu **bản ghi nào** ("Chọn người dùng Nguyễn Văn A"), không phải "Chọn" |
| Chọn tất cả | Ô đánh dấu ở `th` dùng trạng thái `indeterminate` khi chỉ chọn một phần. Phạm vi là **trang hiện tại** — `rows` và `selection` chỉ chứa dòng đã tải (§API); nhãn nói rõ điều đó, vì người dùng luôn hiểu nhầm thành toàn bộ kết quả. Chọn toàn bộ kết quả sau lọc cần hợp đồng phía máy chủ, không có ở v1 |
| Vùng cuộn | Vùng cuộn — dọc lẫn ngang — phải nhận focus (`tabindex="0"` + `role="region"` + `aria-label`). Không có nó, người dùng bàn phím **không cuộn được** |
| Cột hành động | `th` mang tên cột ẩn thị giác (`rowActionsLabel`). Nút trong ô ẩn bằng `opacity` nên vẫn nằm trong thứ tự Tab; Tab tới nút thì `:focus-within` làm nó hiện — người dùng bàn phím không bao giờ focus vào một nút vô hình. Mỗi nút có `aria-label` nêu hành động **và** bản ghi ("Sửa người dùng nva"), vì tên cột không được đọc lại khi Tab trong dòng. Nút ⋯ mang `aria-haspopup="menu"` + `aria-expanded` theo [`Menu.md`](./Menu.md) |
| Chỗ cắm đầu khung | `DataTable` không gán vai trò nào cho dải đầu; `Toolbar` đặt vào đó giữ nguyên ngữ nghĩa của nó (khu tìm `role="search"`). Dải đầu đứng **trước** vùng cuộn trong DOM, nên thứ tự Tab là: `Toolbar` → vùng cuộn → control trong bảng → dải phân trang |
| Bàn phím | Tab đi qua các control trong bảng theo thứ tự đọc. Không bẫy focus trong vùng cuộn |
| Báo kết quả | Một vùng duy nhất do `DataTable` giữ — §"Vùng báo kết quả" ngay dưới bảng |
| `loading` | `aria-busy="true"` trên vùng bảng |
| Công tắc `tree` — vai trò | Bảng đổi sang `role="treegrid"`. Mỗi dòng khai `aria-level`, `aria-expanded` (nếu có con), `aria-setsize`, `aria-posinset`. Thụt lề là tín hiệu **thị giác**; không khai bốn thuộc tính này thì trình đọc màn hình nghe thấy một danh sách phẳng và toàn bộ cấu trúc biến mất |
| Công tắc `tree` — bàn phím | `→` mở nhánh, đang mở thì sang ô kế. `←` đóng nhánh, đang đóng thì **nhảy về dòng cha**. `↑` `↓` giữa các dòng **đang hiện**. `Home` / `End` về đầu / cuối. Thiếu phần này thì cây chỉ dùng được bằng chuột |
| Công tắc `grouped` | Dòng tiêu đề nhóm là `role="row"` với một ô `colspan`, mang `aria-expanded` |
| Công tắc `summary` | Dải tổng là một `<table>` riêng nên nó **mất quan hệ hàng–cột** với bảng chính. Bù lại bằng hai thứ: mỗi ô của dải mang `headers` trỏ tới `id` của `<th>` tương ứng, và cả dải mang `aria-label` nói rõ phạm vi ("Tổng cộng 137 bản ghi"). Không bù thì trình đọc màn hình đọc ra một dãy số không biết của cột nào |
| Công tắc `summary` — lớp | Dải tổng nằm ngoài vùng cuộn nên **không cần** `--z-sticky`; nó không chồng lên gì cả. Chỉ ô đầu khi bật kèm `frozen` mới cần lớp, và dùng cùng lớp với cột ghim của thân bảng |
| Công tắc `expandable` | Nút bung là `<button>` thật với `aria-expanded` và `aria-controls` trỏ tới `id` của khối chi tiết. Khối chi tiết là một dòng `<tr>` thật với ô `colspan`, không phải một khối tuyệt đối chèn ngoài bảng. Nút "Đóng hết" là `<button>` thật, mang `disabled` khi không dòng nào đang mở |
| Chữ | Tiêu đề cột, nhãn, thông báo qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Vùng cuộn không nhận được focus là lỗi accessibility bị bỏ sót nhiều nhất ở bảng.** Chuột có thanh cuộn, cảm ứng có ngón tay, bàn phím thì **không có gì cả** trừ khi vùng đó nhận được focus.

### Vùng báo kết quả — định nghĩa gốc

Chốt 2026-10-08 (người dùng): `DataTable` giữ việc đọc to kết quả của danh sách cho trình đọc màn hình, **kể cả khi không có kết quả**. Đây là vùng **duy nhất** báo kết quả của một màn danh sách: dải [`Pagination.md`](./Pagination.md) mà `DataTable` vẽ bên trong, [`FilterPanel.md`](./FilterPanel.md), [`Toolbar.md`](./Toolbar.md), [`FilterChip.md`](./FilterChip.md), [`EmptyState.md`](./EmptyState.md) và màn đều **không** đặt vùng `aria-live` nào cho kết quả — hai vùng cùng báo một kết quả là trình đọc màn hình đọc hai lần.

| Khoản | Quy định |
| --- | --- |
| Phần tử | Một phần tử ẩn thị giác (lớp `sr-only`) mang `role="status"` — ngầm định `aria-live="polite"` và `aria-atomic="true"`, nên mỗi lần đổi là đọc trọn câu. Nằm trong khung `DataTable` và **luôn có mặt** từ lần vẽ đầu: không nằm trong nhánh nào phụ thuộc `state` hay `totalRecords`. Vùng sinh ra cùng lúc với câu của nó thì trình đọc màn hình thường bỏ qua — vì vậy chuỗi "x–y trên tổng z" của `Pagination`, vốn biến mất khi tổng bằng 0, không gánh được vai này |
| Khi nào đổi câu | Khi một lần tải kết thúc — `state` rời `'loading'`. Mọi đường dẫn tới một lần tải đều đi qua đây: đổi trang, đổi số dòng, sắp xếp, tìm, áp hoặc xoá bộ lọc, gỡ chip. Trong lúc `'loading'` giữ nguyên câu cũ, không đọc "đang tải" — `aria-busy` đã báo |
| Có kết quả — `'idle'` | Câu "có kết quả" ở bảng dưới; tham số tính từ `page`, `pageSize`, `totalRecords` (biến thể `paged`) |
| Không có kết quả — `'empty'`, `'empty-filtered'` | Câu "không có kết quả" ở bảng dưới. Khối [`EmptyState.md`](./EmptyState.md) trong `emptyTemplate` vẫn hiện như cũ và không tự báo |
| `'error'` | Vùng đọc câu lỗi tải chung ở bảng dưới (chốt 2026-10-08, người dùng) — câu của lần tải trước đã không còn đúng nên bị thay, không về rỗng. Đây là đường **duy nhất** đọc to lỗi tải: khối lỗi trong `errorTemplate` ([`EmptyState.md`](./EmptyState.md) biến thể `error`) **không** mang `aria-live` hay `role="status"`/`"alert"`, để cả màn vẫn chỉ có đúng một vùng báo kết quả. Câu chung không nêu nguyên nhân; nguyên nhân và nút "Thử lại" nằm trong khối lỗi nhìn thấy được |

| Câu đọc lên | Câu | Khoá i18n |
| --- | --- | --- |
| Có kết quả | Đang hiện {{tu}} đến {{den}} trên tổng {{tong}} | `chung.bang.ketQua` |
| Không có kết quả | Không có kết quả | `chung.bang.khongCoKetQua` |
| Lỗi tải | Không tải được danh sách. | `chung.bang.khongTaiDuoc` |

Ba câu do người dùng duyệt 2026-10-08. Khoá theo khuôn `chung.<nhóm>.<khoá>`; `khongTaiDuoc` là khoá mới, đặt cạnh hai khoá kia.

Câu đọc lên cố ý khác chuỗi hiển thị "x–y trên tổng z" của `Pagination`: dấu gạch giữa hai số được trình đọc màn hình đọc tuỳ cấu hình — có khi thành "gạch ngang", có khi bỏ qua — nên câu đọc lên viết chữ "đến". Câu không mang danh từ đếm được nên không cần khuôn số nhiều ([`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §5.4).

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `rows` | input | `ReadonlyArray<T>` | `[]` | Dữ liệu của **trang hiện tại**, không phải toàn bộ |
| `caption` | input | `string` | — | **Bắt buộc.** Tên truy cập của bảng, đã dịch — đặt lên `<table>` theo dòng "Tên truy cập của bảng" ở §Accessibility; không hiện ra hình |
| `columns` | input | `ReadonlyArray<DataColumnDef<T>>` | `[]` | Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. **Đây là điểm mở rộng chính**: dự án hạ nguồn thêm hoặc đổi cột qua đây, không sửa file Core |
| `variant` | input | `'paged' \| 'scroll'` | `'paged'` | Hai biến thể loại trừ nhau ở mục Biến thể. `paged` vẽ dải [`Pagination.md`](./Pagination.md) ở chân khung và nhận `page` / `pageSize` / `totalRecords`; `scroll` không vẽ dải nào và tải thêm khi tới đáy vùng cuộn |
| `switches` | input | `DataTableSwitches` | `{}` | Sáu công tắc ở mục Biến thể, khai bằng **một** object có tên. Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 — `frozen` là `boolean`: bật thì ghim cột đầu; cột hành động ghim phải không qua công tắc (§"Cột hành động") |
| `groupBy` | input | `string \| null` | `null` | Khoá cột để gộp nhóm. Bắt buộc khác `null` khi bật công tắc `grouped` |
| `rowDetail` | input | `TemplateRef<{ $implicit: T }> \| null` | `null` | Nội dung khối bung ra. Bắt buộc khác `null` khi bật công tắc `expandable` |
| `emptyTemplate` | input | `TemplateRef<{ $implicit: 'empty' \| 'empty-filtered' }> \| null` | `null` | Nội dung slot trống; context mang ca rỗng để màn chọn biến thể `EmptyState`. Màn danh sách luôn truyền — thiếu thì thân bảng trống trơn |
| `loadingTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Nội dung slot đang tải lần đầu. Màn danh sách luôn truyền |
| `errorTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Nội dung slot lỗi, **gồm cả nút "Thử lại"**. Màn danh sách luôn truyền |
| `rowActionsTemplate` | input | `TemplateRef<{ $implicit: T; compact: boolean }> \| null` | `null` | Nội dung ô của cột hành động, một lần mỗi dòng. `compact` là `true` dưới `$bp-xs` (dạng thẻ) — màn vẽ một nút ⋯ gom mọi hành động; `false` thì vẽ theo khuôn hai nút. Vị trí, ghim, hiện/ẩn do lớp bọc lo — §"Cột hành động". `null` = không có cột |
| `rowActionsLabel` | input | `string` | `''` | Tên cột hành động, **đã dịch**, đặt ẩn thị giác trong `th`. Bắt buộc khác rỗng khi `rowActionsTemplate` khác `null` |
| `nodeExpanded` | output | `string` | — | Chỉ ở công tắc `tree`: phát khoá nhánh vừa xoè, để trang cha tải con nếu `hasChildren` là `true` mà `children` rỗng |
| `totalRecords` | input | `number` | `0` | Tổng số bản ghi phía máy chủ. Chỉ có nghĩa ở `variant = 'paged'` |
| `page` / `pageSize` | input | `number` | `1` / `20` | Chỉ có nghĩa ở `variant = 'paged'`. Danh sách số dòng mỗi trang cho dải phân trang: [`Pagination.md`](./Pagination.md) §API (`pageSizeOptions`) |
| `sortBy` / `sortDescending` | input | `string \| null` / `boolean` | `null` / `false` | Đúng tên tham số trên dây ở [`../../contracts/README.md`](../../contracts/README.md) §8. Quy đổi sang dạng `1 \| -1` của PrimeNG nằm **trong** lớp bọc |
| `selection` | input | `ReadonlyArray<T>` | `[]` | |
| `state` | input | `'idle' \| 'loading' \| 'error' \| 'empty' \| 'empty-filtered'` | `'idle'` | **Một biến duy nhất**, không phải bốn cờ boolean rời — xem dưới |
| `rowKey` | input | `string` | — | Bắt buộc. Khoá định danh dòng, dùng cho `track` ([`../../RULES.md`](../../RULES.md) §7 F13) |
| `summary` | input | `SummaryRow \| null` | `null` | Dữ liệu dòng tổng **do máy chủ tính**. `null` = không bật công tắc `summary`. Gồm: nhãn (đã kèm phạm vi), và giá trị đã định dạng theo từng khoá cột |
| `summaryScope` | input | `'page' \| 'filtered'` | `'filtered'` | Phạm vi của số tổng. Mặc định là **toàn bộ kết quả sau lọc** — sai theo hướng an toàn: người đối chiếu sổ cần con số đó, còn tổng một trang hiếm khi là thứ ai muốn |
| `pageChange` | output | `{ page: number; pageSize: number }` | — | Đổi trang **và** đổi số dòng mỗi trang đều ra bằng sự kiện này. Đổi số dòng thì `page` đã được đưa về `1` bên trong lớp bọc — luật và lý do ở [`Pagination.md`](./Pagination.md) §API. Chỉ phát ở `variant = 'paged'` |
| `sortChange` | output | `{ sortBy: string; sortDescending: boolean }` | — | Phát đúng tên trên dây; dạng `1 \| -1` của PrimeNG không lọt ra ngoài |
| `selectionChange` | output | `ReadonlyArray<T>` | — | |
| `rowClick` | output | `T` | — | Không phát khi bấm trúng một control bên trong dòng |

**`state` là một biến, không phải bốn cờ.** Bốn cờ `loading`/`error`/`empty`/`emptyFiltered` cho phép biểu diễn những tổ hợp vô nghĩa (`loading` và `error` cùng `true`), và mỗi nơi gọi lại xử lý tổ hợp đó một kiểu. Một biến kiểu liệt kê thì trình biên dịch bắt được ca thiếu.

**Chỗ cắm đầu khung là slot nội dung, không phải input.** Màn đặt `Toolbar` vào bằng thuộc tính chọn `dataTableTop` trên phần tử con (cùng khuôn slot `[toolbarActions]` của [`Toolbar.md`](./Toolbar.md)). Dải đầu luôn hiện khi slot có nội dung, không theo `state` nào — nên nó không cần `TemplateRef` ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5, gạch đầu dòng cuối).

**Ba slot nội dung trạng thái.** Cùng khuôn `rowDetail`: mỗi slot là một input `TemplateRef` do màn cấp.

| Slot | Input | Hiện khi | Màn ghép gì |
| --- | --- | --- | --- |
| trống | `emptyTemplate` | `state` là `'empty'` hoặc `'empty-filtered'` | [`EmptyState.md`](./EmptyState.md) — biến thể theo ca, xem mục Trạng thái |
| đang tải | `loadingTemplate` | `state` là `'loading'` và chưa có dòng nào để giữ lại | [`SkeletonLoader.md`](./SkeletonLoader.md) dạng hàng |
| lỗi | `errorTemplate` | `state` là `'error'` | [`EmptyState.md`](./EmptyState.md) biến thể `error`, nút "Thử lại" gọi việc tải lại của màn |

`DataTable` quyết **khi nào** hiện slot nào và giữ tiêu đề cột quanh nó; màn quyết **hiện gì** trong slot. Tách như vậy vì `DataTable` nằm ở tầng bọc và không được import component tự dựng — luật ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2. Nút trong slot gọi thẳng hàm của màn, nên `DataTable` không có output riêng cho "Thử lại"; khuôn chung cho mọi lớp bọc ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5. Cú pháp khai slot là việc thi công, mẫu ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md).

`DataTable` là component **dumb**: nó **phát** yêu cầu đổi trang và đổi sắp xếp; trang cha gọi API rồi truyền dữ liệu mới xuống ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

Trang, sắp xếp, từ khoá tìm và bộ lọc sống trên URL, do tầng trạng thái danh sách của màn giữ — hợp đồng ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8. `DataTable` không đọc và không ghi URL.

## Do / Don't

- ✅ Phân trang **phía máy chủ**. Tải hết rồi phân trang ở trình duyệt chỉ chạy được cho tới ngày bảng có mười nghìn dòng.
- ✅ Luôn truyền `rowKey`. Không có nó, mọi lần dữ liệu đổi là dựng lại toàn bộ DOM.
- ✅ Tách hai ca `empty`.
- ✅ Giữ dữ liệu cũ khi đang đổi trang; đừng thay bằng skeleton.
- ✅ Nói rõ "chọn tất cả" là chọn trang hiện tại hay toàn bộ kết quả.
- ✅ Ở màn danh sách: `Toolbar` vào chỗ cắm đầu khung; dải phân trang nằm trong khung; chỉ vùng cuộn cuộn.
- ✅ Hành động trên một dòng đi qua `rowActionsTemplate`.
- ❌ Không khai cột hành động như một cột thường trong `columns`, và không tự viết luật ghim hay rê chuột cho nó ở màn.
- ❌ Không ẩn nút hành động bằng `display: none` hay `visibility: hidden` — nút phải còn trong thứ tự Tab.
- ❌ Không import lưới dữ liệu của PrimeNG ở bất kỳ đâu khác.
- ❌ Không để lọt kiểu dữ liệu hay tên sự kiện của PrimeNG ra API công khai — làm thế là mất luôn cái lợi duy nhất của việc bọc.
- ❌ Không đè style bằng `::ng-deep`.
- ❌ Không ẩn cột ở màn nhỏ mà không nói thông tin đó đi đâu.
- ❌ Không đặt chiều cao cố định bên trong component.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có cần cho người dùng ẩn/hiện cột và lưu lại lựa chọn đó không? | Sau F3 — dự án hạ nguồn đầu tiên có bảng nhiều cột |
| 2 | **Đã chốt 2026-10-08 (người dùng):** duyệt hai câu đọc lên của §Accessibility, "Vùng báo kết quả" (`chung.bang.ketQua`, `chung.bang.khongCoKetQua`) | Đã trả lời |
| 3 | **Đã chốt 2026-10-08 (người dùng):** lỗi tải được đọc to, qua chính vùng báo kết quả, câu `chung.bang.khongTaiDuoc` "Không tải được danh sách."; khối lỗi trong `errorTemplate` không mang `aria-live` | Đã trả lời |