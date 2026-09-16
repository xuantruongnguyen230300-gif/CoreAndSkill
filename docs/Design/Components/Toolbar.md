---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Toolbar

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, `Toolbar` chỉ là một dải bố cục gom các control đã có sẵn; không có hành vi nào thuộc nhóm "khó" để phải đi mượn.

---

## Mục đích

Gom mọi thao tác tác động lên **một** danh sách — tìm, lọc, hành động — vào đúng một dải nằm ngay trên danh sách đó.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Dải điều khiển ngay trên một [`Table.md`](./Table.md) hoặc [`DataTable.md`](./DataTable.md) | 🛑 Đầu trang: đường dẫn phân cấp, tiêu đề màn → [`PageHeader.md`](./PageHeader.md). `Toolbar` thuộc về **danh sách**, `PageHeader` thuộc về **trang** |
| Ô tìm + bộ lọc + nhóm hành động của danh sách | 🛑 Chuyển giữa các phần nội dung khác nhau → [`Tabs.md`](./Tabs.md) |
| Hành động hàng loạt khi đang chọn nhiều dòng | 🛑 Chuyển chế độ xem loại trừ nhau → [`SegmentedControl.md`](./SegmentedControl.md), đặt **bên trong** `Toolbar` |
| Nút Thêm mới, Tải xuống, Tải lại của danh sách | 🛑 Đổi trang → [`Pagination.md`](./Pagination.md), đặt **dưới** bảng; 🛑 thanh dính đầu ứng dụng → [`Topbar.md`](./Topbar.md) |

## Biến thể

| Biến thể | Chứa gì | Dùng khi |
| --- | --- | --- |
| `full` | Ô tìm + khu lọc + hàng chip + nhóm hành động. Khu lọc là 1–2 ô lọc nằm thẳng trong dải, hoặc nút mở panel lọc từ 3 trường — xem §"Panel lọc nằm ở đâu" | **Mặc định.** Danh sách có ít nhất một trường lọc |
| `search` | Ô tìm + nhóm hành động, không có khu lọc. Có hàng chip khi `readonlyChips` khác rỗng — xem §"Chip chỉ đọc" | Danh sách không có trường lọc nào người dùng đặt được, nhưng vẫn có thể đang chịu một điều kiện hệ thống áp |
| `actions` | Chỉ nhóm hành động, ghim phải | Bảng cấu hình nhỏ trong `Dialog` hoặc trong một `Card` |
| `selection` | Số dòng đang chọn + hành động hàng loạt + nút bỏ chọn | Thay chỗ `full` **tại chỗ** khi có ít nhất một dòng được chọn |

**Vì sao `selection` thay chỗ chứ không mọc thêm một dải thứ hai:** một dải thứ hai xuất hiện sẽ đẩy cả bảng xuống, và người dùng vừa tick một dòng thì hàng đang nằm dưới con trỏ đã trượt đi chỗ khác. Thay nội dung trong cùng một khung giữ nguyên chiều cao — cái giá là ô tìm biến mất trong lúc đang chọn, nên nút bỏ chọn phải rất dễ thấy.

## Kích thước

| Cỡ | Chiều cao control con | Đệm trong | Khe giữa control | Dùng khi |
| --- | --- | --- | --- | --- |
| `md` | `--size-control-md` (34px) | `--sp-5` | `--sp-4` | **Mặc định.** Danh sách toàn trang |
| `sm` | `--size-control-sm` (28px) | `--sp-4` | `--sp-3` | `Toolbar` lồng trong `Dialog` hoặc trong một `Card` hẹp |

Chip điều kiện luôn ở cỡ `sm` kể cả trong `Toolbar` cỡ `md` — chip là nhãn phụ; để nó cao bằng ô tìm sẽ giành mất trọng lượng thị giác của chính ô tìm.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`, viền `--color-border`, `--radius-md`, `--shadow-1` | Có |
| `hover` | **Không áp dụng cho chính dải.** `Toolbar` là vùng bố cục, không phải control bấm được; hover thuộc về từng nút, ô nhập, chip bên trong nó | — |
| `focus-visible` | **Không áp dụng cho chính dải** — dải không nhận focus. Nhưng thứ tự Tab bên trong là bắt buộc: ô tìm → nút lọc → chip theo thứ tự hiển thị → nhóm hành động | — |
| `active` | **Không áp dụng.** Cùng lý do với `hover`: không có gì để nhấn xuống | — |
| `disabled` | Từng control con nhận `disabled` thật; dải **không** dùng `pointer-events: none` — chặn cả dải bằng lớp phủ làm control biến mất khỏi thứ tự Tab mà trình đọc màn hình không biết vì sao | Có |
| `loading` | Ô tìm hiện `pi-spinner` ở đuôi; `aria-busy="true"` trên vùng kết quả. **Không khoá ô tìm** — khoá sẽ nuốt mất ký tự đang gõ, lỗi tệ hơn nhiều so với việc phải huỷ một request thừa | Có |
| `error` | Dải **giữ nguyên hình dạng**; lỗi tải danh sách hiện ở [`NoticeBanner.md`](./NoticeBanner.md) ngay dưới. Tô đỏ cả dải làm người dùng tưởng điều kiện lọc mình vừa đặt là sai | Có |
| `empty` | Không có điều kiện nào bật → hàng chip biến mất hẳn, không để lại hàng cao rỗng; badge đếm ẩn. Chip chỉ đọc **là** một điều kiện đang bật: còn một chip trong `readonlyChips` thì hàng chip còn, kể cả khi người dùng chưa đặt điều kiện nào. Bản thân `Toolbar` không có trạng thái rỗng vì ô tìm luôn còn đó | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-border`, `--color-border-strong`, `--color-text`, `--color-text-muted`, `--color-brand`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-focus` |
| Chữ, khoảng cách | `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-semibold`, `--lh-snug`, `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-md`, `--radius-sm`, `--radius-pill`, `--border-w` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--icon-sm`, `--icon-md`. Icon theo [`../Icons.md`](../Icons.md) §5: `pi-search`, `pi-filter`, `pi-filter-slash`, `pi-times`, `pi-plus`, `pi-download`, `pi-refresh` |
| Bóng, chuyển động | `--shadow-1`, `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Một hàng: ô tìm bên trái và co giãn, nút lọc kề bên, nhóm hành động ghim phải. Hàng chip nằm dưới, trải hết bề rộng, **hiện đủ mọi chip và xuống dòng khi tràn** |
| `$bp-md` … `$bp-lg` | Ô tìm co theo bề rộng còn dư; nhãn của hành động phụ rút còn icon nhưng giữ nguyên `aria-label`. Hàng chip như trên |
| < `$bp-md` | Xuống dòng: hàng một là ô tìm trải hết bề rộng, hàng hai là nút lọc + nhóm hành động, hàng ba là chip. Nhóm hành động trải hết bề rộng dưới `$bp-xs`; chip cuộn ngang trong một dải riêng thay vì xuống nhiều dòng |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Một khối bọc ngoài không mang vai trò. Riêng khu tìm là một phần tử con mang `role="search"` |
| Nhãn ô tìm | `<label>` thật, ẩn về mặt thị giác nhưng có trong cây accessibility. Placeholder **không** thay được nhãn — nó biến mất ngay khi gõ ký tự đầu |
| Nút lọc | `aria-expanded` phản ánh panel đang mở hay đóng; `aria-controls` trỏ tới `id` của panel; số điều kiện đang bật phải nằm trong nhãn đọc lên ("Bộ lọc, 3 điều kiện"), không chỉ là con số vẽ cạnh icon |
| Chip | Nút gỡ trên mỗi chip là một [`IconButton.md`](./IconButton.md) với `aria-label` nói **cả tên điều kiện**: "Gỡ lọc Trạng thái: Hoạt động" |
| Chip chỉ đọc | Không có nút gỡ, vẫn nhận focus để người dùng đọc được lý do — luật thuộc [`FilterChip.md`](./FilterChip.md) §Accessibility, `Toolbar` chỉ truyền `lockReason` xuống. Hàng chip giữ nguyên một thứ tự Tab: chip chỉ đọc trước, chip gỡ được sau, đúng thứ tự hiển thị |
| Bàn phím | `Escape` trong ô tìm xoá nội dung ô và trả về danh sách chưa lọc; `Escape` khi panel lọc đang mở thì đóng panel và **trả focus về nút lọc** |
| Focus | `outline` kèm `outline-offset` theo [`../DESIGN.md`](../DESIGN.md) §2.6 cho mọi control con. Số bản ghi sau khi lọc báo qua vùng `aria-live="polite"` đặt cạnh bảng, không đặt trong `Toolbar` |
| Vùng bấm | Nút gỡ trên chip ≥ 28×28px, nới bằng `padding` chứ không bằng `margin` |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'full' \| 'search' \| 'actions' \| 'selection'` | `'full'` | |
| `size` | input | `'sm' \| 'md'` | `'md'` | Chỉ hai cỡ; `lg` không có nghĩa cho một dải điều khiển |
| `searchValue` | input | `string` | `''` | Giá trị hiện tại của ô tìm; trang cha giữ nguồn sự thật |
| `activeFilterCount` | input | `number` | `0` | Bằng `0` thì badge ẩn hẳn, không hiện số không |
| `chips` | input | `ReadonlyArray<ToolbarChip>` | `[]` | Điều kiện người dùng đặt và gỡ được. Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 |
| `readonlyChips` | input | `ReadonlyArray<ToolbarChip>` | `[]` | Điều kiện **hệ thống áp**, người dùng không gỡ được — thường là bộ lọc đọc ra từ tham số trên URL lúc vào màn. Nhận đúng những mục khai `removable: false` theo chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9, nên `lockReason` bắt buộc có. Không mục nào ở đây phát `chipRemoved` |
| `disabled` | input | `boolean` | `false` | Truyền xuống mọi control con |
| `loading` | input | `boolean` | `false` | Chỉ hiện spinner ở ô tìm, không khoá ô |
| `searchChanged` | output | `string` | — | Phát mỗi lần nội dung ô tìm đổi, **không** debounce — xem ghi chú dưới |
| `filterPanelToggled` | output | `boolean` | — | Trạng thái mở/đóng mong muốn của panel lọc |
| `chipRemoved` | output | `string` | — | Khoá của chip vừa bị gỡ |
| `filtersCleared` | output | `void` | — | Nút xoá tất cả điều kiện |

**Khu lọc 1–2 trường vào qua slot nội dung**, đặt ngay sau ô tìm: màn ghép ô lọc của mình vào đó. Nút lọc, `activeFilterCount` và `filterPanelToggled` chỉ dùng khi khu lọc là nút mở panel, tức từ 3 trường. Hàng chip dùng cho cả hai dạng.

**`Toolbar` chỉ phát `searchChanged` — không chờ ngừng gõ, không huỷ request.** Nó là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5). Chờ ngừng gõ, bỏ truy vấn trùng và bỏ kết quả của request cũ đều thuộc tầng trạng thái danh sách của màn (`setSearchText`) — hợp đồng ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8. Ghi ở đây để không ai dựng xong `Toolbar` rồi thêm một lớp chờ thứ hai bên trong nó: hai lớp chờ cộng dồn thành độ trễ không ai khai.

**Vì sao bộ lọc giấu sau một nút thay vì phơi hết ra dải:** phơi bốn ô lọc ra `Toolbar` ăn mất hai hàng chiều cao ở **mọi** lần vào màn, kể cả khi người dùng không lọc gì. Giấu sau nút trả lại chiều cao đó cho bảng — nhưng cái giá là điều kiện đang bật trở nên vô hình. Hàng chip trả đúng cái giá đó: chip nằm **ngoài** panel nên điều kiện đang bật luôn nhìn thấy được mà không phải mở panel ra kiểm.

## Do / Don't

- ✅ Ô tìm luôn có `<label>` thật, dù ẩn về mặt thị giác.
- ✅ Mỗi chip gỡ được riêng, và luôn có nút xoá tất cả khi có từ hai chip trở lên.
- ✅ Nhóm hành động ghim phải; hành động chính là `primary` và chỉ đúng một cái.
- ❌ Không debounce và không gọi API trong `Toolbar`. Chống gọi dồn dập thuộc tầng trạng thái danh sách.
- ❌ Không dùng placeholder thay cho nhãn.
- ❌ Không nhét `Pagination` vào `Toolbar`.
- ❌ Không khoá cả dải bằng lớp phủ khi `loading`.

## Chip chỉ đọc — điều kiện `Toolbar` không cho gỡ

`Toolbar` vẽ mỗi mục của `readonlyChips` bằng [`FilterChip.md`](./FilterChip.md) biến thể `readonly`, đặt **trước** các chip gỡ được trong cùng một hàng chip. Thứ tự đó cố định: một điều kiện không gỡ được là **bối cảnh của cả danh sách**, nên nó phải đọc được trước những điều kiện người dùng vừa tự đặt. Hai nhóm chip không tách thành hai hàng — một hàng thứ hai ăn thêm chiều cao ở mọi lần vào màn, đúng cái giá mà §"Panel lọc nằm ở đâu" đã từ chối trả.

Hàng chip **không gộp**: từ `$bp-md` trở lên hiện đủ mọi chip và xuống dòng khi tràn; dưới `$bp-md` cuộn ngang (§Responsive). Điều kiện đang bật phải nhìn thấy được — đó là lý do hàng chip tồn tại (§"Panel lọc nằm ở đâu"), và nút xoá tất cả đã có từ hai chip. Khác ô chọn nhiều của [`Autocomplete.md`](./Autocomplete.md), nơi chip nằm **trong** một ô nhập có chiều cao phải giữ.

`activeFilterCount` **không** đếm chip chỉ đọc. Con số trên nút lọc trả lời câu *"tôi đã đặt mấy điều kiện"*, và một số đếm cộng thêm những điều kiện người dùng không đặt, không gỡ và không thấy trong panel sẽ không bao giờ khớp với thứ họ nhìn thấy khi mở panel ra.

**Chiều phụ thuộc — đã kiểm, không vi phạm.** Cột "Nền" ở [`../COMPONENTS.md`](../COMPONENTS.md) §3 xếp cả `Toolbar` lẫn `FilterChip` vào cùng nhóm `tự dựng`, và ánh xạ ở [`../../wiki-core/fe/05-component-library.md`](../../wiki-core/fe/05-component-library.md) đưa cả hai về cùng một thư mục. Luật F24 — [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §4.6 — chỉ cấm chiều từ lớp bọc thư viện sang component tự dựng, nên chiều này không chạm nó: `Toolbar` **import thẳng** `FilterChip`, không phải nhận qua slot. Slot ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 là lối đi dành cho lớp bọc thư viện; dùng nó ở đây chỉ đẩy việc dựng một chip lên cho từng màn, và mỗi màn sẽ dựng một kiểu. Tầng lồng nhau cũng hợp lệ: `FilterChip` ở tầng 1, `Toolbar` ở tầng 3 ([`../COMPONENTS.md`](../COMPONENTS.md) §2.4).

## Panel lọc nằm ở đâu — một ngưỡng, không hai lựa chọn

`Toolbar` sở hữu **nút** mở lọc; [`FilterChip.md`](./FilterChip.md) sở hữu **kết quả** lọc. Phần ở giữa — nơi người dùng thật sự nhập điều kiện — quyết như sau, theo **số trường lọc**:

| Số trường lọc | Ở đâu | Vì sao |
| --- | --- | --- |
| 1–2 trường | **Nằm thẳng trong `Toolbar`**, cùng hàng với ô tìm | Một panel cho hai ô chọn là hai lần bấm thừa cho mọi lần lọc |
| Từ 3 trường trở lên | **[`FilterPanel.md`](./FilterPanel.md)** biến thể `drawer`, đặt trong [`Drawer.md`](./Drawer.md) cỡ `lg` | Đủ chỗ cho nhãn và ô nhập xếp dọc; không bị cắt trong container cuộn; và giữ được danh sách phía sau để người dùng thấy kết quả đổi |

🛑 **Không dùng popover cho panel lọc.** `Toolbar` gần như luôn nằm trong một vùng cuộn, và một popover nhiều trường sẽ bị cắt ở mép vùng đó — lỗi chỉ lộ ra ở màn thấp hoặc khi danh sách đã cuộn, tức là muộn.

Ngưỡng ba trường là một con số chọn có chủ đích chứ không phải hằng số thiêng: dưới ngưỡng thì chi phí mở panel lớn hơn chỗ nó tiết kiệm, trên ngưỡng thì hàng `Toolbar` bắt đầu xuống dòng và vỡ nhịp.

## Cần chốt

Không còn.
