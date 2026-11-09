---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Toolbar

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có thật ở `src/FE/src/app/shared/components/toolbar/` — selector `app-toolbar`, class `ToolbarComponent`. Bảng dưới khai **đúng những mục đã mở ba tệp `.ts` / `.html` / `.scss` ra so, ngày 2026-09-20 (các dòng ghi ngày 2026-10-08 đối chiếu thêm bằng đọc mã sau FE-1…FE-3)**; mục không có tên trong bảng thì chưa ai đối chiếu. Bản đang chạy tự khai là **F3 tối giản** trong chính chú thích đầu `toolbar.component.ts`.

| Mục trong spec | Có thật hôm nay (neo bằng chuỗi trong `src/FE`) | Sẽ thành |
| --- | --- | --- |
| §Nền — tự dựng, không bọc PrimeNG | `toolbar.component.ts` không import gì từ `primeng/`; template dựng `<div class="toolbar">` với `<input type="text">` thật | Giữ nguyên |
| Component dumb, **không** debounce, không gọi API | `onSearchInput` chỉ có đúng một dòng `this.searchChanged.emit(gt)`; `.ts` không inject service nào | Giữ nguyên |
| Chiều phụ thuộc: `Toolbar` **import thẳng** `FilterChip` | `toolbar.component.ts` import `FilterChipComponent` từ `../filter-chip/filter-chip.component` và khai trong `imports` | Giữ nguyên |
| Kiểu `ToolbarChip` khớp `../../quy-uoc/fe-ui-conventions.md` §9 | `toolbar.component.ts` khai `export interface ToolbarChip` với đúng năm trường `key`, `label`, `value`, `removable`, `lockReason?` | Giữ nguyên |
| `default`: nền `--color-surface`, viền `--color-border`, `--radius-md`, `--shadow-1` | `.toolbar` khai đủ cả bốn | Giữ nguyên |
| Đệm và khe theo cỡ | `.toolbar` khai `padding: var(--sp-5)` / `gap: var(--sp-4)`; `.toolbar--sm` hạ về `--sp-4` / `--sp-3` — khớp cả hai dòng của bảng §Kích thước | Giữ nguyên |
| Khu tìm mang `role="search"`, khối ngoài không mang vai trò | `.toolbar__tim` mang `role="search"`; `<div class="toolbar">` không có `role` nào | Giữ nguyên |
| Nhãn ô tìm là `<label>` thật, ẩn thị giác | Template có `<label class="sr-only" for="toolbar-tim">{{ searchLabel() }}</label>` | Giữ nguyên |
| `loading`: spinner ở đuôi ô tìm, **không** khoá ô | `@if (loading())` vẽ `<i class="pi pi-spinner pi-spin toolbar__tim-loading">`; `[disabled]` của input chỉ gắn vào `disabled()`, không gắn vào `loading()` | Giữ nguyên |
| `disabled`: dải **không** dùng `pointer-events: none` | `.scss` không chứa chuỗi `pointer-events` ở đâu | Giữ nguyên |
| `empty`: hàng chip biến mất hẳn, và chip chỉ đọc tính là điều kiện đang bật | Template bọc hàng chip trong `@if (readonlyChips().length > 0 \|\| chips().length > 0)` | Giữ nguyên |
| §Chip chỉ đọc — chip `readonly` đặt **trước** chip gỡ được, **cùng một** hàng | Trong `.toolbar__chips` có hai vòng `@for` liên tiếp: `readonlyChips()` với `variant="readonly"` trước, rồi `chips()` | Giữ nguyên |
| `lockReason` truyền xuống `FilterChip` | Vòng `readonlyChips()` gắn `[lockReason]="chip.lockReason ?? null"` | Giữ nguyên |
| Nút xoá tất cả từ hai chip trở lên | `@if (chips().length > 1)` vẽ `<app-button ... icon="filter-slash" (clicked)="filtersCleared.emit()">` | Giữ nguyên |
| Khu lọc nằm thẳng trong dải, vào qua slot ngay sau ô tìm | **Đã gỡ** (đọc mã 2026-10-08): `toolbar.component.html` không còn `<ng-content select="[toolbarFilters]"`; không tệp nào dưới `src/FE/src/app` còn chuỗi `toolbarFilters`. Mọi trường lọc vào [`FilterPanel.md`](./FilterPanel.md) | Giữ nguyên — mục này chỉ còn để người sửa sau biết slot đó đã bị gỡ có chủ ý (chốt 2026-10-08, §"Panel lọc nằm ở đâu") |
| Nhóm hành động ghim phải | `.toolbar__hanh-dong` khai `margin-left: auto`; slot `<ng-content select="[toolbarActions]" />` (đọc mã 2026-10-08) | Giữ nguyên. Ở màn danh sách, nút chính của màn vào slot này và đứng **cuối** nhóm (§Khi nào dùng) |
| Trần bề rộng ô tìm `--toolbar-search-w-max` (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08): `toolbar.component.scss` § `.toolbar__tim` khai `max-width: var(--toolbar-search-w-max)` từ `$bp-md` trở lên; token khai ở `src/FE/src/styles/_spacing.scss` chuỗi `--toolbar-search-w-max:` | Giữ nguyên |
| Cách đặt `placement="attached"` — gắn vào khung `DataTable` (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08): `toolbar.component.ts` chuỗi `readonly placement = input<'standalone' \| 'attached'>('standalone');`; `.toolbar--attached` khai `border: none`, `border-radius: 0`, `box-shadow: none`. Nơi dùng: ba trang danh sách (chuỗi `placement="attached"`) đặt `<app-toolbar … dataTableTop>` hoặc trong `<div dataTableTop>` vào chỗ cắm đầu khung `DataTable` | Giữ nguyên. Vạch ngăn dưới dải do `DataTable` vẽ (`data-table.component.scss` § `.data-table__dau`) |
| Hàng chip hiện đủ và xuống dòng khi tràn (≥ `$bp-md`) | `.toolbar__chips` khai `flex-wrap: wrap` | Giữ nguyên |
| `focus-visible` của ô tìm | **Khớp** luật chốt 2026-10-05 (đọc mã 2026-10-08): `.toolbar__tim-input:focus-visible` khai `outline: none`, `border-color: var(--color-text)` và bóng `inset` dày thêm `--border-w-strong` trừ `--border-w` — một đường biên 2px màu `--color-text`, không vòng ngoài, không `--color-focus`. Ba ca kiểm trong `toolbar.component.spec.ts`: `có focus: không outline ngoài, viền 1px + bóng trong 1px = 2px màu --color-text`, `kích thước hộp ô không đổi giữa nghỉ và focus`, `font-family tính ra bắt đầu bằng Be Vietnam Pro…` (người soạn chưa chạy test) | Giữ nguyên. Các control khác trong dải giữ vòng focus của chính chúng |
| Biến thể `actions` và `selection` | **Chưa có.** `variant` nhận cả bốn giá trị nhưng `toolbar.component.html` chỉ đọc nó ở đúng một chỗ (`variant() === 'full'`, để vẽ nút lọc); `search` đã chạy thật ở `danh-sach-vai-tro.page.html` và `danh-sach-don-vi.page.html` (chuỗi `variant="search"`). Không nhánh nào ẩn ô tìm hay vẽ số dòng đang chọn | Dựng `actions`/`selection` theo bảng §Biến thể khi có màn cần |
| Chiều cao control con theo cỡ | **Có một phần** (đọc mã 2026-10-08): `.toolbar--sm .toolbar__tim-input` khai `height: var(--size-control-sm)`; nút lọc nhận `[size]="size()"` | Nút gỡ tất cả luôn `size="sm"`; chưa so từng dòng bảng §Kích thước; chưa thấy ca kiểm cho cỡ `sm` |
| Nút lọc mở panel: `activeFilterCount`, `filterLabel`, `filterAriaLabel`, `filterPanelOpen`, `filterPanelId`, `filterPanelToggled`, `aria-expanded`, `aria-controls` | **Có** (đọc mã 2026-10-08, FE-2): `toolbar.component.ts` khai đủ năm input và output `filterPanelToggled` (phát `!filterPanelOpen()`); `toolbar.component.html` dựng `<app-button class="toolbar__nut-loc" … icon="filter" haspopup="dialog" [expanded]="filterPanelOpen()" [controls]="filterPanelId()"`, `aria-label` chỉ gắn khi `activeFilterCount() > 0`, kèm `<app-badge variant="neutral" size="sm" aria-hidden="true">` mang số; chỉ vẽ ở `variant === 'full'`. Nơi dùng thật: `danh-sach-nguoi-dung.page.html` (chuỗi `variant="full"`, `[filterPanelId]="idPanelLoc"`) | Chưa có ca kiểm riêng trong `toolbar.component.spec.ts` (chỉ có ca đường biên focus của ô tìm); e2e `10-nguoi-dung.spec.ts` chạm nút (`.toolbar__nut-loc button`, `.toolbar__nut-loc app-badge`). Người soạn chưa chạy test |
| Thứ tự Tab: ô tìm → nút lọc → chip → nhóm hành động | **Đã khớp ở DOM** (đọc mã 2026-10-08): `toolbar.component.html` đặt `.toolbar__tim`, `<app-button class="toolbar__nut-loc"`, `.toolbar__chips`, `.toolbar__hanh-dong` theo đúng thứ tự đó; vị trí nhìn thấy do `order` trong `toolbar.component.scss` (`.toolbar__nut-loc` 1, `.toolbar__hanh-dong` 2, `.toolbar__chips` 3) | ⚠️ **Thứ tự nhìn thấy khác thứ tự Tab:** hàng chip (DOM thứ ba) hiện **dưới** nhóm hành động (DOM thứ tư) vì `order`. Với bố cục một hàng + hàng chip dưới, Tab đi nhóm hành động **sau** chip dù nhóm đứng trên và bên phải; chưa ai đo bằng bàn phím trên trình duyệt. Spec chưa nói gì về thứ tự nhìn thấy của hai khối này |
| `Escape` trong ô tìm xoá nội dung ô | **Chưa có** (đọc mã 2026-10-08). `toolbar.component.html` không gắn trình xử lý bàn phím nào lên `<input>` | Dựng theo §Accessibility |
| §Responsive — ba ngưỡng | **Có một phần, bằng CSS thuần** (đọc mã 2026-10-08): `toolbar.component.scss` § `.toolbar__tim` mặc định `flex: 1 1 100%` (trải hết hàng), từ `$bp-md` mới `flex: 1 1 240px` kèm `max-width: var(--toolbar-search-w-max)`; § `.toolbar__chips` dưới `$bp-md` đổi `flex-wrap: nowrap` + `overflow-x: auto` (chip cuộn ngang); § `.toolbar__hanh-dong` dưới `$bp-xs` trải hết bề rộng, mỗi nút `flex: 1 1 auto` | Chưa dựng: dòng `$bp-md … $bp-lg` — nhãn hành động **phụ** rút còn icon giữ `aria-label` (component không biết nút nào là phụ; việc của nơi gọi hoặc cần input mới). Chưa đo trên trình duyệt |
| `readonlyChips` nhận đúng mục khai `removable: false` | **Không được ép ở đâu.** Trường `removable` có trong `ToolbarChip` nhưng không tệp nào trong thư mục component đọc tới nó; việc chia hai mảng hoàn toàn do nơi gọi | Kiểm ở nơi gọi, hoặc bỏ trường không ai đọc |

**Lỗi `id` cố định đã sửa (đọc mã 2026-10-08):** `toolbar.component.ts` khai bộ đếm `boDemIdOTim` và `idOTim` sinh `toolbar-tim-<số thứ tự>` (chuỗi `++boDemIdOTim`); `toolbar.component.html` gắn `[id]="idOTim"` cho ô và `[attr.for]="idOTim"` cho `<label>` — hai `Toolbar` cùng trang không còn trùng `id`. Chưa có ca kiểm riêng cho điều này trong `toolbar.component.spec.ts`.

📐 **Những mục dưới đây CHƯA đối chiếu, và vẫn là đích đến:** `aria-label` của nút gỡ chip và vùng bấm ≥ 28×28px (thuộc `FilterChip.md`), chip luôn ở cỡ `sm`, `aria-busy` trên vùng kết quả (đặt ở **nơi gọi**, cạnh bảng), `disabled` truyền xuống các control vào qua slot, khoản i18n, §Khi nào dùng / khi nào KHÔNG dùng, §Do / Don't.

Hai input `searchLabel` (bắt buộc) và `clearAllLabel` có trong `toolbar.component.ts` từ trước khi spec khai; §API dự kiến đã khai bổ sung 2026-10-08, cùng khuôn với hai nhãn mới của nút lọc.

**Nền:** tự dựng, không bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, `Toolbar` chỉ là một dải bố cục gom các control đã có sẵn; không có hành vi nào thuộc nhóm "khó" để phải đi mượn.

---

## Mục đích

Gom mọi thao tác tác động lên **một** danh sách — tìm, lọc, hành động — vào đúng một dải nằm ngay trên danh sách đó.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Dải điều khiển ngay trên một [`Table.md`](./Table.md), hoặc dải đầu khung của [`DataTable.md`](./DataTable.md) (cách đặt `attached`) | 🛑 Đầu trang: đường dẫn phân cấp, tiêu đề màn, mô tả → [`PageHeader.md`](./PageHeader.md). `Toolbar` thuộc về **danh sách**, `PageHeader` thuộc về **trang** |
| Ô tìm + nút lọc + nhóm hành động của danh sách | 🛑 Chuyển giữa các phần nội dung khác nhau → [`Tabs.md`](./Tabs.md) |
| Hành động hàng loạt khi đang chọn nhiều dòng | 🛑 Chuyển chế độ xem loại trừ nhau → [`SegmentedControl.md`](./SegmentedControl.md), đặt **bên trong** `Toolbar` |
| **Nút chính của màn danh sách** (Thêm mới) — đúng một `primary`, giữ nhãn đầy đủ, đứng **cuối** nhóm hành động tức sát mép phải hàng; cùng nhóm với Tải xuống, Tải lại, liên kết sang màn liên quan | 🛑 Đổi trang → [`Pagination.md`](./Pagination.md), dải đáy khung `DataTable`; 🛑 thanh đầu ứng dụng → [`Topbar.md`](./Topbar.md); 🛑 hành động trên **một** dòng → cột hành động của [`DataTable.md`](./DataTable.md) |

**Ranh giới với `PageHeader` ở màn danh sách — một chỗ đặt (chốt 2026-10-08).** Màn danh sách đặt **mọi** nút của màn ở `Toolbar`, kể cả nút chính tạo bản ghi mới; `PageHeader` của màn đó chỉ còn tiêu đề và mô tả. Vì sao: nút chính đứng cùng hàng với ô tìm và nút lọc — đúng chỗ mắt đang làm việc với danh sách — và màn không còn hai vùng cùng có nút để người dùng phải quét cả hai. Màn **không** phải danh sách (chi tiết, hồ sơ, cấu hình) giữ hành động của trang ở `PageHeader` như cũ — [`PageHeader.md`](./PageHeader.md) §Khi nào dùng; một `Toolbar` đặt trên bảng con trong màn đó chỉ mang thao tác của bảng con.

## Biến thể

| Biến thể | Chứa gì | Dùng khi |
| --- | --- | --- |
| `full` | Ô tìm + nút lọc (có badge đếm) + hàng chip + nhóm hành động. Mọi trường lọc nằm trong [`FilterPanel.md`](./FilterPanel.md) do nút lọc mở — xem §"Panel lọc nằm ở đâu" | **Mặc định.** Danh sách có ít nhất một trường lọc |
| `search` | Ô tìm + nhóm hành động, không có khu lọc. Có hàng chip khi `readonlyChips` khác rỗng — xem §"Chip chỉ đọc" | Danh sách không có trường lọc nào người dùng đặt được, nhưng vẫn có thể đang chịu một điều kiện hệ thống áp |
| `actions` | Chỉ nhóm hành động, ghim phải | Bảng cấu hình nhỏ trong `Dialog` hoặc trong một `Card` |
| `selection` | Số dòng đang chọn + hành động hàng loạt + nút bỏ chọn | Thay chỗ `full` **tại chỗ** khi có ít nhất một dòng được chọn |

**Vì sao `selection` thay chỗ chứ không mọc thêm một dải thứ hai:** một dải thứ hai xuất hiện sẽ đẩy cả bảng xuống, và người dùng vừa tick một dòng thì hàng đang nằm dưới con trỏ đã trượt đi chỗ khác. Thay nội dung trong cùng một khung giữ nguyên chiều cao — cái giá là ô tìm biến mất trong lúc đang chọn, nên nút bỏ chọn phải rất dễ thấy.

### Cách đặt — đứng riêng hay gắn vào khung bảng

Một trục thứ hai, **độc lập** với bốn biến thể nội dung ở trên — chọn qua input `placement`. Bốn biến thể nói dải **chứa gì**; cách đặt nói dải **nằm trên bề mặt nào**. Tách hai trục vì chúng kết hợp tự do: `full` gắn vào bảng, `selection` thay chỗ `full` ngay trong khung bảng, `actions` đứng riêng trong một `Card`.

| Cách đặt | Hình thức | Dùng khi |
| --- | --- | --- |
| `standalone` | Bề mặt riêng: nền `--color-surface`, viền `--border-w` `--color-border`, `--radius-md`, `--shadow-1` | **Mặc định.** Dải trên một [`Table.md`](./Table.md), trong một `Card`, trong `Dialog` |
| `attached` | **Không** viền, **không** bo góc, **không** bóng của riêng nó; nền `--color-surface`. Đặt vào chỗ cắm đầu khung của [`DataTable.md`](./DataTable.md) — khung bảng vẽ viền ngoài và vạch ngăn dưới dải, `Toolbar` không vẽ đường nào | 🛑 **Bắt buộc ở màn danh sách** — [`../Templates/ListScreen.md`](../Templates/ListScreen.md) §2 |

**Vì sao là một cách đặt của `Toolbar` chứ không phải một component gộp "khối danh sách":** đúng lý do [`../Templates/ListScreen.md`](../Templates/ListScreen.md) §1 đã cân — component gộp phải phơi biến thể của cả ba con ra thành input và phải biết "lọc theo trường nào"; còn ghép ba component rời trong một khung thì gỡ ra được mà không sửa màn nào. Mở rộng component có sẵn, không đẻ mới ([`../COMPONENTS.md`](../COMPONENTS.md) §1).

## Kích thước

| Cỡ | Chiều cao control con | Đệm trong | Khe giữa control | Dùng khi |
| --- | --- | --- | --- | --- |
| `md` | `--size-control-md` (34px) | `--sp-5` | `--sp-4` | **Mặc định.** Danh sách toàn trang |
| `sm` | `--size-control-sm` (28px) | `--sp-4` | `--sp-3` | `Toolbar` lồng trong `Dialog` hoặc trong một `Card` hẹp |

Chip điều kiện luôn ở cỡ `sm` kể cả trong `Toolbar` cỡ `md` — chip là nhãn phụ; để nó cao bằng ô tìm sẽ giành mất trọng lượng thị giác của chính ô tìm.

**Ô tìm có trần bề rộng** `--toolbar-search-w-max` từ `$bp-md` trở lên: nó co giãn theo chỗ còn dư nhưng không vượt trần, phần còn lại của hàng để trống giữa nút lọc và nhóm hành động. Dưới `$bp-md` ô tìm trải hết hàng của nó (§Responsive). Vì sao có trần và con số chọn thế nào: [`../DESIGN.md`](../DESIGN.md) §6.5.

Nút lọc và các nút của nhóm hành động cao bằng ô tìm — cùng cỡ control của dải theo bảng trên.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | `standalone`: nền `--color-surface`, viền `--color-border`, `--radius-md`, `--shadow-1`. `attached`: nền `--color-surface`, không viền, không bo, không bóng — đường bao do khung `DataTable` vẽ | Có |
| `hover` | **Không áp dụng cho chính dải.** `Toolbar` là vùng bố cục, không phải control bấm được; hover thuộc về từng nút, ô nhập, chip bên trong nó | — |
| `focus-visible` | **Không áp dụng cho chính dải** — dải không nhận focus. Nhưng thứ tự Tab bên trong là bắt buộc: ô tìm → nút lọc → chip theo thứ tự hiển thị → nhóm hành động | — |
| `active` | **Không áp dụng.** Cùng lý do với `hover`: không có gì để nhấn xuống | — |
| `disabled` | Từng control con nhận `disabled` thật; dải **không** dùng `pointer-events: none` — chặn cả dải bằng lớp phủ làm control biến mất khỏi thứ tự Tab mà trình đọc màn hình không biết vì sao | Có |
| `loading` | Ô tìm hiện `pi-spinner` ở đuôi; `aria-busy="true"` trên vùng kết quả. **Không khoá ô tìm** — khoá sẽ nuốt mất ký tự đang gõ, lỗi tệ hơn nhiều so với việc phải huỷ một request thừa | Có |
| `error` | Dải **giữ nguyên hình dạng**; lỗi tải danh sách hiện ở [`NoticeBanner.md`](./NoticeBanner.md) ngay dưới — riêng `attached` thì lỗi hiện ở slot lỗi (`errorTemplate`) của `DataTable`, không chèn banner vào giữa khung ([`../Templates/ListScreen.md`](../Templates/ListScreen.md) §2). Tô đỏ cả dải làm người dùng tưởng điều kiện lọc mình vừa đặt là sai | Có |
| `empty` | Không có điều kiện nào bật → hàng chip biến mất hẳn, không để lại hàng cao rỗng; badge đếm ẩn. Chip chỉ đọc **là** một điều kiện đang bật: còn một chip trong `readonlyChips` thì hàng chip còn, kể cả khi người dùng chưa đặt điều kiện nào. Bản thân `Toolbar` không có trạng thái rỗng vì ô tìm luôn còn đó | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-border`, `--color-border-strong`, `--color-text`, `--color-text-muted`, `--color-brand`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-focus` |
| Chữ, khoảng cách | `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-semibold`, `--lh-snug`, `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-md`, `--radius-sm`, `--radius-pill`, `--border-w` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--icon-sm`, `--icon-md`, `--toolbar-search-w-max` (tầng 3, [`../DESIGN.md`](../DESIGN.md) §6.5). Icon theo [`../Icons.md`](../Icons.md) §5: `pi-search`, `pi-filter`, `pi-filter-slash`, `pi-times`, `pi-plus`, `pi-download`, `pi-refresh` |
| Bóng, chuyển động | `--shadow-1`, `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Một hàng: ô tìm bên trái, co giãn tới trần `--toolbar-search-w-max`; nút lọc kề bên; nhóm hành động ghim phải, nút chính đứng cuối — sát mép phải. Hàng chip nằm dưới, trải hết bề rộng, **hiện đủ mọi chip và xuống dòng khi tràn** |
| `$bp-md` … `$bp-lg` | Ô tìm co theo bề rộng còn dư, không vượt trần; nhãn của hành động **phụ** rút còn icon nhưng giữ nguyên `aria-label`. Nút lọc và nút chính **giữ nhãn đầy đủ**. Hàng chip như trên |
| < `$bp-md` | Xuống dòng: hàng một là ô tìm trải hết bề rộng (bỏ trần), hàng hai là nút lọc + nhóm hành động, hàng ba là chip. Nhóm hành động trải hết bề rộng dưới `$bp-xs`; chip cuộn ngang trong một dải riêng thay vì xuống nhiều dòng. Nút chính giữ nhãn đầy đủ ở mọi ngưỡng |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Một khối bọc ngoài không mang vai trò. Riêng khu tìm là một phần tử con mang `role="search"` |
| Nhãn ô tìm | `<label>` thật, ẩn về mặt thị giác nhưng có trong cây accessibility. Placeholder **không** thay được nhãn — nó biến mất ngay khi gõ ký tự đầu |
| Nút lọc | `<button type="button">` thật. `aria-haspopup="dialog"` — nó mở một `Drawer` mang `role="dialog"`; `aria-expanded` = `filterPanelOpen`; `aria-controls` = `filterPanelId`. Số điều kiện đang bật phải nằm trong nhãn đọc lên, không chỉ là con số vẽ cạnh icon: khi `activeFilterCount` > 0, `aria-label` = `filterAriaLabel` ("Bộ lọc, 3 điều kiện đang bật") và badge số mang `aria-hidden="true"`; khi bằng 0, không đặt `aria-label` — tên đọc lên là chữ nhìn thấy. `filterAriaLabel` phải **mở đầu bằng đúng** `filterLabel`, để người dùng điều khiển bằng giọng nói gọi được nút bằng chữ họ nhìn thấy (WCAG 2.2 SC 2.5.3) |
| Chip | Nút gỡ trên mỗi chip là một [`IconButton.md`](./IconButton.md) với `aria-label` nói **cả tên điều kiện**: "Gỡ lọc Trạng thái: Hoạt động" |
| Chip chỉ đọc | Không có nút gỡ, vẫn nhận focus để người dùng đọc được lý do — luật thuộc [`FilterChip.md`](./FilterChip.md) §Accessibility, `Toolbar` chỉ truyền `lockReason` xuống. Hàng chip giữ nguyên một thứ tự Tab: chip chỉ đọc trước, chip gỡ được sau, đúng thứ tự hiển thị |
| Bàn phím | `Escape` trong ô tìm xoá nội dung ô và trả về danh sách chưa lọc; `Escape` khi panel lọc đang mở thì đóng panel và **trả focus về nút lọc** |
| Focus | `outline` kèm `outline-offset` theo [`../DESIGN.md`](../DESIGN.md) §2.6 cho mọi control con. `Toolbar` không mang vùng `aria-live` nào; kết quả sau khi tìm hay lọc do `DataTable` đọc to — [`DataTable.md`](./DataTable.md) §Accessibility, "Vùng báo kết quả" |
| Vùng bấm | Nút gỡ trên chip ≥ 28×28px, nới bằng `padding` chứ không bằng `margin` |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'full' \| 'search' \| 'actions' \| 'selection'` | `'full'` | |
| `placement` | input | `'standalone' \| 'attached'` | `'standalone'` | §"Cách đặt". Màn danh sách luôn truyền `'attached'` và đặt `Toolbar` vào chỗ cắm đầu khung của `DataTable` |
| `size` | input | `'sm' \| 'md'` | `'md'` | Chỉ hai cỡ; `lg` không có nghĩa cho một dải điều khiển |
| `searchValue` | input | `string` | `''` | Giá trị hiện tại của ô tìm; trang cha giữ nguồn sự thật |
| `searchLabel` | input | `string` | — | **Bắt buộc** khi có ô tìm. Câu **đã dịch**: nhãn `<label>` ẩn thị giác của ô tìm. `id` của ô tìm do component tự sinh duy nhất mỗi thể hiện — không cố định trong template (lỗi `id` trùng ở bảng đầu file) |
| `clearAllLabel` | input | `string` | `''` | Câu **đã dịch** của nút xoá tất cả điều kiện, hiện từ hai chip trở lên |
| `activeFilterCount` | input | `number` | `0` | Bằng `0` thì badge ẩn hẳn, không hiện số không |
| `filterLabel` | input | `string` | `''` | Câu **đã dịch**, chữ nhìn thấy của nút lọc ("Bộ lọc"). Bắt buộc khác rỗng ở `variant = 'full'` |
| `filterAriaLabel` | input | `string \| null` | `null` | Câu **đã dịch**, đầy đủ, mang số điều kiện ("Bộ lọc, 3 điều kiện đang bật"). Nơi gọi dịch câu có tham số, `Toolbar` không ghép chuỗi. `null` khi `activeFilterCount` = 0 — §Accessibility, dòng "Nút lọc" |
| `filterPanelOpen` | input | `boolean` | `false` | Panel lọc đang mở hay đóng — đặt `aria-expanded` của nút lọc. Trang cha giữ nguồn sự thật, cùng giá trị nó truyền vào `open` của [`FilterPanel.md`](./FilterPanel.md) |
| `filterPanelId` | input | `string \| null` | `null` | `id` của panel lọc — đặt `aria-controls`. Trang cha truyền cùng một chuỗi cho `FilterPanel` |
| `chips` | input | `ReadonlyArray<ToolbarChip>` | `[]` | Điều kiện người dùng đặt và gỡ được. Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 |
| `readonlyChips` | input | `ReadonlyArray<ToolbarChip>` | `[]` | Điều kiện **hệ thống áp**, người dùng không gỡ được — thường là bộ lọc đọc ra từ tham số trên URL lúc vào màn. Nhận đúng những mục khai `removable: false` theo chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9, nên `lockReason` bắt buộc có. Không mục nào ở đây phát `chipRemoved` |
| `disabled` | input | `boolean` | `false` | Truyền xuống mọi control con |
| `loading` | input | `boolean` | `false` | Chỉ hiện spinner ở ô tìm, không khoá ô |
| `searchChanged` | output | `string` | — | Phát mỗi lần nội dung ô tìm đổi, **không** debounce — xem ghi chú dưới |
| `filterPanelToggled` | output | `boolean` | — | Trạng thái mở/đóng mong muốn của panel lọc — bấm nút lọc phát `!filterPanelOpen` |
| `chipRemoved` | output | `string` | — | Khoá của chip vừa bị gỡ |
| `filtersCleared` | output | `void` | — | Nút xoá tất cả điều kiện |

**Nhóm hành động vào qua slot nội dung `[toolbarActions]`:** màn đặt các [`Button.md`](./Button.md) của mình vào đó, theo thứ tự hiển thị trái → phải; nút chính `primary` đặt **cuối**. `Toolbar` không nhận mảng hành động qua input — cùng lý do với nhóm hành động của [`PageHeader.md`](./PageHeader.md) §API: một mảng chuỗi không diễn tả được nhãn, icon, trạng thái `loading` và quyền của từng nút. Không còn slot nào cho ô lọc — ô lọc sống trong [`FilterPanel.md`](./FilterPanel.md).

**`Toolbar` chỉ phát `searchChanged` — không chờ ngừng gõ, không huỷ request.** Nó là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5). Chờ ngừng gõ, bỏ truy vấn trùng và bỏ kết quả của request cũ đều thuộc tầng trạng thái danh sách của màn (`setSearchText`) — hợp đồng ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8. Ghi ở đây để không ai dựng xong `Toolbar` rồi thêm một lớp chờ thứ hai bên trong nó: hai lớp chờ cộng dồn thành độ trễ không ai khai.

**Vì sao bộ lọc giấu sau một nút thay vì phơi hết ra dải:** phơi bốn ô lọc ra `Toolbar` ăn mất hai hàng chiều cao ở **mọi** lần vào màn, kể cả khi người dùng không lọc gì. Giấu sau nút trả lại chiều cao đó cho bảng — nhưng cái giá là điều kiện đang bật trở nên vô hình. Hàng chip trả đúng cái giá đó: chip nằm **ngoài** panel nên điều kiện đang bật luôn nhìn thấy được mà không phải mở panel ra kiểm.

## Do / Don't

- ✅ Ô tìm luôn có `<label>` thật, dù ẩn về mặt thị giác.
- ✅ Mỗi chip gỡ được riêng, và luôn có nút xoá tất cả khi có từ hai chip trở lên.
- ✅ Nhóm hành động ghim phải; hành động chính là `primary`, chỉ đúng một cái, đứng cuối nhóm và giữ nhãn đầy đủ.
- ✅ Ở màn danh sách, `Toolbar` gắn vào khung `DataTable` (`placement="attached"`).
- ❌ Không debounce và không gọi API trong `Toolbar`. Chống gọi dồn dập thuộc tầng trạng thái danh sách.
- ❌ Không đặt ô lọc thẳng vào dải — có từ một trường lọc thì mọi trường vào `FilterPanel`.
- ❌ Không dùng placeholder thay cho nhãn.
- ❌ Không nhét `Pagination` vào `Toolbar`.
- ❌ Không khoá cả dải bằng lớp phủ khi `loading`.

## Chip chỉ đọc — điều kiện `Toolbar` không cho gỡ

`Toolbar` vẽ mỗi mục của `readonlyChips` bằng [`FilterChip.md`](./FilterChip.md) biến thể `readonly`, đặt **trước** các chip gỡ được trong cùng một hàng chip. Thứ tự đó cố định: một điều kiện không gỡ được là **bối cảnh của cả danh sách**, nên nó phải đọc được trước những điều kiện người dùng vừa tự đặt. Hai nhóm chip không tách thành hai hàng — một hàng thứ hai ăn thêm chiều cao ở mọi lần vào màn, đúng cái giá mà §"Panel lọc nằm ở đâu" đã từ chối trả.

Hàng chip **không gộp**: từ `$bp-md` trở lên hiện đủ mọi chip và xuống dòng khi tràn; dưới `$bp-md` cuộn ngang (§Responsive). Điều kiện đang bật phải nhìn thấy được — đó là lý do hàng chip tồn tại (§"Panel lọc nằm ở đâu"), và nút xoá tất cả đã có từ hai chip. Khác ô chọn nhiều của [`Autocomplete.md`](./Autocomplete.md), nơi chip nằm **trong** một ô nhập có chiều cao phải giữ.

`activeFilterCount` **không** đếm chip chỉ đọc. Con số trên nút lọc trả lời câu *"tôi đã đặt mấy điều kiện"*, và một số đếm cộng thêm những điều kiện người dùng không đặt, không gỡ và không thấy trong panel sẽ không bao giờ khớp với thứ họ nhìn thấy khi mở panel ra.

**Chiều phụ thuộc — đã kiểm, không vi phạm.** Cột "Nền" ở [`../COMPONENTS.md`](../COMPONENTS.md) §3 xếp cả `Toolbar` lẫn `FilterChip` vào cùng nhóm `tự dựng`, và ánh xạ ở [`../../wiki-core/fe/05-component-library.md`](../../wiki-core/fe/05-component-library.md) đưa cả hai về cùng một thư mục. Luật F24 — [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §4.6 — chỉ cấm chiều từ lớp bọc thư viện sang component tự dựng, nên chiều này không chạm nó: `Toolbar` **import thẳng** `FilterChip`, không phải nhận qua slot. Slot ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 là lối đi dành cho lớp bọc thư viện; dùng nó ở đây chỉ đẩy việc dựng một chip lên cho từng màn, và mỗi màn sẽ dựng một kiểu. Tầng lồng nhau cũng hợp lệ: `FilterChip` ở tầng 1, `Toolbar` ở tầng 3 ([`../COMPONENTS.md`](../COMPONENTS.md) §2.4).

## Panel lọc nằm ở đâu — một quy tắc, không ngưỡng

`Toolbar` sở hữu **nút** mở lọc; [`FilterChip.md`](./FilterChip.md) sở hữu **kết quả** lọc. Phần ở giữa — nơi người dùng thật sự nhập điều kiện — luôn là **[`FilterPanel.md`](./FilterPanel.md) đặt trong [`Drawer.md`](./Drawer.md) cỡ `lg`**, hễ danh sách có từ **một** trường lọc trở lên (chốt 2026-10-08). Không trường lọc nào thì dùng biến thể `search`, không có nút lọc.

| Danh sách có | Hàng `Toolbar` | Ô nhập điều kiện ở đâu |
| --- | --- | --- |
| 0 trường lọc | `search`: ô tìm · nhóm hành động | — |
| Từ 1 trường lọc | `full`: ô tìm · nút lọc có badge đếm · nhóm hành động | `FilterPanel` trong `Drawer` cỡ `lg`, neo phải |

**Vì sao không chia theo số trường** (kiểu "1–2 trường thì nằm thẳng trong dải"): chia như vậy cho mỗi màn một hình dạng `Toolbar` khác nhau tuỳ số trường, và ô lọc nằm thẳng ăn bề rộng của chính hàng phải chứa nút chính của màn — hàng vỡ xuống dòng sớm ở `$bp-md`. Một hình dạng cho mọi danh sách có lọc: ô tìm ngắn, nút lọc mang số điều kiện, nút chính ở đầu phải. Cái giá, nói thẳng: lọc theo một trường tốn thêm một lần bấm mở panel và một lần bấm Áp dụng. Chấp nhận vì điều kiện đang bật vẫn luôn thấy được ở hàng chip và ở số trên nút lọc, không phải mở panel ra kiểm.

🛑 **Không dùng popover cho panel lọc.** Một popover nhiều trường neo vào nút trong khung bảng sẽ bị cắt ở mép vùng chứa — lỗi chỉ lộ ra ở màn thấp, tức là muộn.

## Nút lọc — đặc tả thi công

| Khoản | Quy định |
| --- | --- |
| Có mặt khi | `variant = 'full'`. Đứng ngay sau ô tìm, trước nhóm hành động |
| Hình thức | Hình thức [`Button.md`](./Button.md) `secondary`, cỡ theo `size` của dải, icon `filter` (tên trần, [`../Icons.md`](../Icons.md) §5), chữ `filterLabel` |
| Badge đếm | `activeFilterCount` > 0 → một [`Badge.md`](./Badge.md) `neutral` cỡ `sm` mang con số, đứng sau chữ, bên trong nút. Bằng 0 → không vẽ gì. Không đếm chip chỉ đọc (§"Chip chỉ đọc"). `Toolbar` import thẳng `Badge` — cùng nhóm `tự dựng`, cùng chiều phụ thuộc với `FilterChip` ở §"Chip chỉ đọc" |
| Bấm | Phát `filterPanelToggled` với `!filterPanelOpen`. Trang cha đổi `open` của `FilterPanel` và truyền `filterPanelOpen` mới xuống |
| Trạng thái mở | Khi `filterPanelOpen` là `true`, nút giữ nền như `hover` của `secondary` cho tới khi panel đóng — người dùng thấy nút nào đang giữ panel. Cùng cách nút theme ở [`Topbar.md`](./Topbar.md) §"Nút theme" |
| `disabled` | Theo `disabled` của dải — `disabled` thật trên nút |
| `loading` | Không áp dụng — nút không chờ dữ liệu; danh mục của từng trường tải trong panel ([`FilterPanel.md`](./FilterPanel.md) §Trạng thái) |
| Focus | Panel đóng bằng bất cứ đường nào — `Escape`, nút đóng, Áp dụng, bấm lớp phủ — thì focus **trả về nút lọc**. Việc trả focus do [`Drawer.md`](./Drawer.md) gánh (dòng "Focus khi đóng"), `Toolbar` không tự làm |
| Trợ năng | §Accessibility, dòng "Nút lọc" |

## Cần chốt

Không còn.
