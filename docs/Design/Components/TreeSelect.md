---
kind: luat
scope: core
verified: chua-doi-chieu
---

# TreeSelect

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **bọc PrimeNG**. Hai lý do thuộc nhóm "khó" ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 cộng lại: định vị lớp nổi khi cuộn hoặc tràn viewport, và bàn phím theo chuẩn ARIA cho một cây có nhánh mở/đóng.

---

## Mục đích

Chọn một nút trong một cấu trúc phân cấp, khi danh sách phẳng không diễn tả được quan hệ cha–con.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Chọn đơn vị khi tạo tài khoản, khi một cơ quan có nhiều cấp trực thuộc | 🛑 Danh sách **phẳng** dưới vài chục mục → [`Input.md`](./Input.md) biến thể `select`. Kéo một cây vào chỉ để hiện tám dòng ngang hàng là trả giá cho không |
| Chọn tài khoản kế toán trong hệ thống tài khoản nhiều cấp | 🛑 Danh mục lớn **không phân cấp**, cần gõ để tìm → [`Autocomplete.md`](./Autocomplete.md) |
| Chọn danh mục phân cấp, địa bàn hành chính | 🛑 **Hiển thị** một cây để đọc và thao tác trên từng dòng → [`DataTable.md`](./DataTable.md) biến thể `tree` |
| Chọn nhánh áp dụng cho một quy tắc | 🛑 Điều hướng theo cây chức năng → [`Sidebar.md`](./Sidebar.md) |

## Biến thể

| Biến thể | Chọn được bao nhiêu | Dùng khi |
| --- | --- | --- |
| `single` | Đúng một nút | **Mặc định.** Chọn đơn vị cho một tài khoản, chọn tài khoản hạch toán |
| `multiple` | Nhiều nút, mỗi nút thành một chip trong ô. Chip là phần cấu trúc của lớp bọc: thư viện vẽ, tạo hình bằng token theo hình thức của [`FilterChip.md`](./FilterChip.md) — [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 | Chọn nhiều đơn vị áp dụng cho một quy tắc |
| `cascade` | Nhiều nút, **chọn cha kéo theo toàn bộ con** | Phân quyền theo nhánh, nơi "cả phòng này" là một ý nghĩa thật |

**`cascade` mang một câu hỏi phải trả lời ở tầng nghiệp vụ, không phải ở tầng giao diện:** khi gửi lên máy chủ, gửi **nút cha** hay gửi **danh sách con đã bung ra**? Hai cách cho kết quả khác nhau vào ngày có người thêm một đơn vị con mới — gửi cha thì đơn vị mới tự động nằm trong; gửi danh sách con thì không. Spec này không quyết thay; nó bắt màn hình dùng `cascade` phải khai rõ mình chọn cách nào.

### Nút cha có chọn được không

Không có câu trả lời chung, và đó là điều quan trọng nhất của component này:

| Ca | Nút cha chọn được? | Vì sao |
| --- | --- | --- |
| Chọn đơn vị cho một tài khoản | ✅ Có | Có người thật làm việc ở cấp sở, không thuộc phòng nào |
| Chọn tài khoản kế toán để hạch toán | 🛑 Không | Chỉ tài khoản cấp cuối mới ghi sổ được |

Nút không chọn được **vẫn hiện, vẫn xoè ra được**, nhưng làm mờ và không nhận `Enter`. Ẩn nó đi thì cây gãy và người dùng không tìm được đường tới nút con.

## Kích thước

| Cỡ | Chiều cao ô | Cỡ chữ | Dùng khi |
| --- | --- | --- | --- |
| `sm` | `--size-control-sm` | `--fs-xs` | Ô lọc trong [`Toolbar.md`](./Toolbar.md) |
| `md` | `--size-control-md` | `--fs-sm` | **Mặc định.** Form, dialog |
| `lg` | `--size-control-lg` | `--fs-md` | Chỉ khi ô chọn là hành động chính của cả màn |

Ba cỡ khớp thang chung ở [`../COMPONENTS.md`](../COMPONENTS.md) §2.2, vì khi đóng thì đây là một ô nhập và phải cao bằng các ô nhập cạnh nó.

Lớp nổi: bề rộng tối thiểu bằng ô, tối đa `--layout-dialog-w-sm`; chiều cao tối đa `--layout-popover-h-max` rồi cuộn — token dùng chung với [`Autocomplete.md`](./Autocomplete.md), khai ở [`../DESIGN.md`](../DESIGN.md) §6.1. Thụt lề mỗi cấp `--tree-indent` ([`../DESIGN.md`](../DESIGN.md) §6.1) — cùng giá trị với [`DataTable.md`](./DataTable.md) biến thể `tree`, để hai chỗ hiện cùng một cây trông giống nhau. Trạng thái xoè/thu nhớ **trong phiên, theo khoá nút** — cùng luật với [`DataTable.md`](./DataTable.md) công tắc `tree`.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Ô đóng: nền `--color-surface`, viền `--color-border-strong` — bậc viền "chỗ này gõ được" theo [`../DESIGN.md`](../DESIGN.md) §2.3. Hiện **đường dẫn đầy đủ** của nút đã chọn, phần tổ tiên dùng `--color-text-muted`, nút đích dùng `--color-text` | Có |
| `hover` | Ô đổi viền `--color-border-strong` đậm thêm; trong lớp nổi, dòng dưới con trỏ đổi nền `--color-surface-2`. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px` trên ô. Trong lớp nổi, dòng đang được bàn phím trỏ tới dùng nền `--color-brand-subtle` + chữ `--color-brand-on-subtle` | Có |
| `active` | Lớp nổi đang mở: ô giữ vòng focus, mũi tên ở mép phải xoay 180° | Có |
| `disabled` | Nền `--color-surface-3`, chữ `--color-text-disabled`, viền `--color-border`, con trỏ `not-allowed`. Thuộc tính `disabled` thật | Có |
| `loading` | **Hai ca khác nhau.** *Mở lần đầu:* lớp nổi hiện `loadingTemplate` — màn ghép ba dòng [`SkeletonLoader.md`](./SkeletonLoader.md). *Xoè một nhánh chưa tải:* **nút xoè của riêng nhánh đó** đổi thành `pi-spinner`, phần còn lại của cây vẫn dùng được. Cả hai mang `aria-busy` | Có |
| `error` | Tải cây hỏng: lớp nổi hiện `errorTemplate` — màn ghép dòng lỗi màu `--color-danger` kèm nút "Thử lại"; ô giữ nguyên giá trị đang có. Lỗi **xác thực** của trường (bắt buộc mà bỏ trống) thì viền ô đổi `--color-danger-border` dày `--border-w-strong` và dòng lỗi hiện dưới ô qua [`FormRow.md`](./FormRow.md) | Có |
| `empty` | **Ba ca phải phân biệt.** Hai ca đầu, lớp nổi hiện `emptyTemplate` với ca rỗng trong context: *cây rỗng hoàn toàn* (`'empty'`) — "Chưa có đơn vị nào"; *lọc không khớp* (`'empty-filtered'`) — "Không có kết quả cho '…'" kèm nút xoá từ khoá. *Nhánh cha hoá ra không có con:* nút xoè **biến mất**, nút trở thành lá | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-subtle`, `--color-border-strong`, `--color-brand`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-danger`, `--color-focus`, `--color-danger-border` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fs-md`, `--fw-medium`, `--lh-snug` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-sm`, `--radius-md`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-sm`, `--icon-md`, `--tree-indent`, `--layout-dialog-w-sm` (trần bề rộng lớp nổi — mượn bậc `sm` của thang bề rộng ở [`../DESIGN.md`](../DESIGN.md) §6.1, không đẻ bậc mới), `--layout-popover-h-max` |
| Bóng, lớp | `--shadow-3`, `--z-popover` |
| Icon | `pi-chevron-right` / `pi-chevron-down` để xoè-thu, `pi-search` cho ô lọc, `pi-spinner` khi tải — [`../Icons.md`](../Icons.md) §5 |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Lớp nổi neo dưới ô, lật lên trên khi không đủ chỗ |
| `$bp-sm` … `$bp-md` | Giữ nguyên; bề rộng lớp nổi bám theo bề rộng ô |
| < `$bp-sm` | Lớp nổi chuyển thành [`Drawer.md`](./Drawer.md) neo đáy, cao tối đa 85vh, ô lọc dính đỉnh. Mỗi dòng cao tối thiểu `--size-control-lg` |

Ở màn nhỏ, thụt lề giảm còn một nửa `--tree-indent`. Cây bốn cấp với thụt lề đầy đủ trên màn 390px không còn chỗ cho nhãn.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | Ô là `role="combobox"` với `aria-expanded`, `aria-controls`. Lớp nổi là `role="tree"`; mỗi dòng `role="treeitem"` |
| Thuộc tính cây | Mỗi dòng khai `aria-level`, `aria-expanded` (nếu có con), `aria-setsize`, `aria-posinset`. Thụt lề là tín hiệu **thị giác**; không khai thì trình đọc màn hình nghe thấy một danh sách phẳng |
| Bàn phím — cây | `↑` `↓` giữa các dòng **đang hiện**. `→` mở nhánh, đang mở thì xuống con đầu. `←` đóng nhánh, đang đóng thì nhảy về cha. `Home` / `End` về đầu / cuối |
| Bàn phím — chọn | `Enter` chọn và đóng ở `single`; `Space` bật/tắt ở `multiple` và `cascade`. `Escape` đóng và **trả focus về ô** |
| Nút không chọn được | `aria-disabled="true"`, **vẫn nằm trong đường đi của phím mũi tên** — nó là đường tới nút con |
| Trạng thái nửa chọn | Ở `cascade`, nút cha có con chọn một phần dùng `aria-checked="mixed"` |
| Nhãn | Ô có `<label>` thật qua `FormRow`. Placeholder **không** thay được nhãn |
| Giá trị đọc lên | Đọc **đường dẫn đầy đủ**, không chỉ tên nút. Nhiều đơn vị trùng tên ở các nhánh khác nhau |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `nodes` | input | `ReadonlyArray<UiTreeNode>` | `[]` | Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. Tên mang tiền tố `Ui` để không trùng `TreeNode` của PrimeNG |
| `value` | input | `string \| string[] \| null` | `null` | Chuỗi ở `single`, mảng ở hai biến thể kia |
| `variant` | input | `'single' \| 'multiple' \| 'cascade'` | `'single'` | Mặc định là dạng hẹp nhất, sai theo hướng an toàn |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `selectableLevels` | input | `'all' \| 'leaf'` | `'all'` | `'leaf'` cho ca tài khoản kế toán |
| `searchable` | input | `boolean` | `true` | Cây quá hai cấp mà không cho gõ tìm là bắt người dùng xoè tay từng nhánh |
| `placeholder` | input | `string` | — | **Bắt buộc.** Nói rõ đang chọn cái gì: "Chọn đơn vị" |
| `disabled` | input | `boolean` | `false` | |
| `loading` | input | `boolean` | `false` | |
| `loadingTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Nội dung lớp nổi khi mở lần đầu mà cây chưa về |
| `emptyTemplate` | input | `TemplateRef<{ $implicit: 'empty' \| 'empty-filtered' }> \| null` | `null` | Nội dung lớp nổi khi cây rỗng hoặc lọc không khớp; `TreeSelect` biết ca nào vì ô lọc là của nó |
| `errorTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Khối lỗi trong lớp nổi, gồm nút "Thử lại". Màn chỉ truyền khi tải cây hỏng — [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 |
| `valueChange` | output | `string \| string[]` | — | |
| `nodeExpanded` | output | `string` | — | Phát khoá nhánh vừa xoè, để trang cha tải con nếu cần |

`TreeSelect` là component **dumb** — nó **không** tự gọi API lấy cây. Nhánh chưa tải thì nó phát `nodeExpanded` và chờ trang cha truyền dữ liệu mới xuống ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Hiện đường dẫn đầy đủ ở ô đóng, không chỉ tên nút lá.
- ✅ Giữ nút cha không khớp khi lọc, làm mờ, để thấy đường dẫn tới kết quả.
- ✅ Tải cây theo nhánh khi cây lớn; tải cả cây lúc mở form là một lần chờ người dùng gánh mỗi lần.
- ✅ Cho gõ để lọc ngay khi cây quá hai cấp.
- ❌ Không ẩn nút không chọn được — cây sẽ gãy.
- ❌ Không dùng `TreeSelect` cho danh sách phẳng.
- ❌ Không để `cascade` mà không khai rõ gửi cha hay gửi con lên máy chủ.
- ❌ Không thụt lề bằng khoảng trắng trong nhãn. Thụt lề là bố cục, không phải nội dung.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Khi lọc, máy chủ trả về nhánh khớp **kèm tổ tiên** hay trả phẳng rồi client tự dựng lại cây? Trả kèm tổ tiên thì client đơn giản nhưng hợp đồng API phức tạp hơn | Sau F3 — dự án hạ nguồn đầu tiên có danh mục phân cấp (card hợp đồng của danh mục đó) |
| 2 | `cascade` gửi cha hay gửi danh sách con? Xem phần Biến thể. Đây là quyết định **nghiệp vụ**, và nó phải được chốt trước khi màn đầu tiên dùng biến thể này | Sau F3 — `ba-analyst` của feature đầu tiên cần phân quyền theo nhánh |
