---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Menu

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **bọc PrimeNG**. Theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, định vị một lớp nổi khi trang cuộn hoặc khi nó chạm mép màn hình là hành vi thuộc nhóm "khó": phải xử lý container cuộn, `position: fixed` lồng nhau, và lật hướng khi hết chỗ. Tự dựng là viết lại một bài toán đã được giải đúng.

---

## Mục đích

Hiện một danh sách hành động ngắn trong một lớp nổi neo vào phần tử đã mở nó.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Gom các hành động phụ của một hàng bảng lại sau một nút, khi ba `IconButton` cạnh nhau là quá nhiều | 🛑 Chọn một **giá trị** để điền vào form → [`Input.md`](./Input.md) biến thể `select`. Menu phát ra hành động, `select` mang dữ liệu; lẫn hai thứ làm form không bind được |
| Menu tài khoản ở [`Topbar.md`](./Topbar.md): hồ sơ, đổi mật khẩu, đăng xuất | 🛑 Chọn một nút trong cây → [`TreeSelect.md`](./TreeSelect.md); chọn từ danh mục lớn bằng cách gõ → [`Autocomplete.md`](./Autocomplete.md) |
| Hành động phụ của [`PageHeader.md`](./PageHeader.md) ở màn hẹp, khi không đủ chỗ cho nhiều nút | 🛑 Điều hướng chính của ứng dụng → [`Sidebar.md`](./Sidebar.md). Menu không phải chỗ giấu tuyến đi |
| Bảng chọn cột của [`DataTable.md`](./DataTable.md) | 🛑 Nội dung dài, có form, cần đọc kỹ → [`Dialog.md`](./Dialog.md) hoặc [`Drawer.md`](./Drawer.md). Menu quá năm bảy mục là dấu hiệu chọn sai component |

**Ngưỡng cứng:** quá **mười mục** thì đó không còn là menu. Lúc đó hoặc chia nhóm có tiêu đề, hoặc chuyển sang `Drawer` có ô tìm.

## Biến thể

| Biến thể | Neo vào | Dùng khi |
| --- | --- | --- |
| `anchored` | Phần tử đã mở nó — một [`Button.md`](./Button.md) hoặc [`IconButton.md`](./IconButton.md) | **Mặc định.** Mọi ca trong Core |
| `context` | Vị trí con trỏ lúc bấm chuột phải | Chỉ khi màn hình có thao tác lặp rất nhiều trên cùng một loại đối tượng |

`context` **phải luôn có đường đi thứ hai** — một nút mở cùng menu đó. Chuột phải không tồn tại trên thiết bị cảm ứng, và người dùng bàn phím không có cách nào gọi nó. Menu chỉ mở được bằng chuột phải là một chức năng vô hình với một nửa người dùng.

Ba công tắc nội dung, dùng được với cả hai biến thể:

| Công tắc | Hiệu ứng | Dùng khi |
| --- | --- | --- |
| `header` | Một dòng tiêu đề không bấm được ở đầu, nói menu này thuộc về đối tượng nào | Menu của một hàng bảng — để người dùng biết mình đang thao tác lên bản ghi nào |
| `groups` | Chia mục thành nhóm, mỗi nhóm một nhãn | Từ sáu mục trở lên |
| `separators` | Vạch ngăn giữa các nhóm hành động | Luôn dùng để tách mục phá huỷ khỏi phần còn lại — xem Do / Don't |

## Kích thước

| Cỡ | Chiều cao một mục | Đệm ngang | Cỡ chữ | Icon | Dùng khi |
| --- | --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` | `--sp-4` | `--fs-xs` | `--icon-sm` | Menu mở từ một control cỡ `sm` — trong hàng bảng, trong `Toolbar` |
| `md` | `--size-control-md` | `--sp-4` | `--fs-sm` | `--icon-md` | **Mặc định.** `Topbar`, `PageHeader` |

Chỉ hai cỡ. Cỡ `lg` không có nghĩa cho một danh sách hành động: chiều cao mục phải theo mật độ của chỗ gọi nó, và không chỗ nào trong Core gọi menu từ một control cỡ `lg`.

Bề rộng: tối thiểu bằng bề rộng phần tử neo, tối đa `--layout-sidebar-w`. Nhãn dài hơn thì **cắt bằng dấu ba chấm**, không xuống dòng — một mục menu hai dòng phá nhịp dọc và làm vùng bấm cao thấp không đều.

Bo góc `--radius-md`, đổ bóng `--shadow-3`, lớp `--z-popover`.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`, viền `--color-border`, `--radius-md`, `--shadow-3`. Mục ở trạng thái nghỉ dùng chữ `--color-text` | Có |
| `hover` | Mục đổi nền `--color-surface-2`. **Bọc trong `@media (hover: hover)`**. Chuyển tiếp `--dur-fast` với `--ease-standard` | Có |
| `focus-visible` | Mục đang được bàn phím trỏ tới dùng **cùng một hình thức với `hover`** cộng thêm `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: -2px`. Offset **âm** vì vòng focus vẽ sát mép menu sẽ bị đường viền menu cắt mất | Có |
| `active` | Nền `--color-surface-3` trong lúc giữ chuột. Không dùng `transform` — mục menu không phải nút nổi, dịch nó xuống trông như lỗi vẽ | Có |
| `disabled` | Chữ và icon `--color-text-disabled`, `aria-disabled="true"`, con trỏ `not-allowed`. **Mục bị khoá vẫn hiện và vẫn nhận focus**, kèm một dòng lý do ngắn ở mép phải — xem Do / Don't | Có |
| `loading` | Chỉ áp cho menu có nội dung tải theo yêu cầu: thân menu thay bằng ba dòng [`SkeletonLoader.md`](./SkeletonLoader.md), `aria-busy="true"` trên hộp menu. Bề rộng menu **giữ nguyên** theo giá trị tối thiểu, nếu không menu sẽ nhảy cỡ ngay trước mắt người dùng | Có |
| `error` | Tải danh sách mục hỏng: thân menu thay bằng một dòng chữ lỗi màu `--color-danger` kèm nút "Thử lại". Menu **không tự đóng** khi lỗi — đóng đi thì người dùng không biết vừa có chuyện gì | Có |
| `empty` | Không mục nào khả dụng: hiện đúng một dòng chữ `--color-text-muted` nói vì sao rỗng ("Không có hành động nào cho bản ghi này"). 🛑 **Không mở một menu rỗng không chữ** — người dùng sẽ bấm lại vài lần rồi nghĩ chức năng hỏng | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-subtle`, `--color-danger`, `--color-focus` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-semibold`, `--lh-snug`, `--ls-wide` |
| Khoảng cách | `--sp-1`, `--sp-2`, `--sp-3`, `--sp-4` |
| Hình dạng | `--radius-sm`, `--radius-md`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--icon-sm`, `--icon-md`, `--layout-sidebar-w` |
| Bóng, lớp | `--shadow-3`, `--z-popover` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Menu nổi, neo vào phần tử mở nó. Lật lên trên hoặc sang trái khi không đủ chỗ ở hướng mặc định |
| `--bp-sm` … `--bp-md` | Giữ nguyên dạng nổi; cỡ mục nâng lên `md` ở mọi vị trí |
| < `--bp-sm` | Menu **trượt lên từ đáy màn hình**, trải hết bề rộng, có một vạch kéo ở đầu. Mỗi mục cao tối thiểu `--size-control-lg` |

**Vì sao đổi hẳn hình dạng ở màn nhỏ thay vì chỉ phóng to:** một lớp nổi neo vào nút trên màn 390px gần như luôn che mất chính hàng dữ liệu mà người dùng vừa bấm, và ngón tay thì che nốt phần còn lại. Trượt từ đáy giữ cho nội dung phía trên nhìn thấy được, và đặt các mục vào vùng ngón cái với tới. Cái giá: hai dạng hiển thị phải cùng dựng và cùng kiểm.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | Hộp menu mang `role="menu"`; mỗi mục `role="menuitem"`. Mục bật/tắt dùng `role="menuitemcheckbox"` kèm `aria-checked` |
| Phần tử mở | Mang `aria-haspopup="menu"` và `aria-expanded` phản ánh trạng thái thật |
| Bàn phím — mở | `Enter`, `Space`, hoặc `↓` trên phần tử neo. `↓` mở và trỏ ngay vào mục đầu; `↑` mở và trỏ vào mục cuối |
| Bàn phím — di chuyển | `↑` `↓` giữa các mục **đang bật**, vòng lại khi hết. `Home` / `End` về đầu / cuối. Gõ một chữ cái nhảy tới mục bắt đầu bằng chữ đó |
| Bàn phím — đóng | `Escape` đóng và **trả focus về phần tử đã mở**. Đây là khoản hay bị bỏ nhất: không trả focus thì người dùng bàn phím bị văng về đầu trang |
| Bẫy focus | Menu **không** bẫy focus như `Dialog`. `Tab` đóng menu và đi tiếp theo thứ tự trang |
| Mục bị khoá | `aria-disabled="true"`, **vẫn nằm trong thứ tự di chuyển** của `↑` `↓`. Bỏ nó ra khỏi danh sách khiến người dùng trình đọc màn hình không biết chức năng đó tồn tại |
| Nhãn | Hộp menu có `aria-label` nói nó thuộc về đối tượng nào, không phải chỉ "Menu" |
| Chạm ngoài | Bấm ra ngoài đóng menu. Vùng bắt sự kiện **không** được chặn cú bấm đó tới đích của nó nếu đích là một control khác |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `items` | input | `ReadonlyArray<UiMenuItem>` | `[]` | Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. Tên mang tiền tố `Ui` để không trùng entity `MenuItem` phía backend |
| `variant` | input | `'anchored' \| 'context'` | `'anchored'` | |
| `size` | input | `'sm' \| 'md'` | `'md'` | |
| `header` | input | `string \| null` | `null` | `null` = không có dòng tiêu đề |
| `ariaLabel` | input | `string` | — | **Bắt buộc.** Không có mặc định; một menu không nhãn là một menu vô danh với trình đọc màn hình |
| `open` | input | `boolean` | `false` | Trang cha giữ nguồn sự thật, để đóng được menu từ bên ngoài |
| `loading` | input | `boolean` | `false` | |
| `selected` | output | `string` | — | Phát khoá của mục được chọn. **Không phát khi mục đang khoá** — chặn ở component, không bắt mỗi nơi gọi tự nhớ |
| `openChange` | output | `boolean` | — | Báo ra mọi lần menu tự đóng: chọn xong, `Escape`, bấm ra ngoài |

Mục menu vào qua `items` chứ **không** qua slot. Lý do: hành vi bàn phím ở mục Accessibility đòi component biết danh sách mục và biết mục nào đang khoá; với nội dung tuỳ ý trong slot thì nó không thể biết, và phím mũi tên sẽ nhảy sai.

`Menu` là component **dumb** — không tự quyết mục nào hiện, không gọi service. Trang cha lọc theo quyền rồi truyền xuống ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Đặt mục phá huỷ **cuối cùng, sau một vạch ngăn**. Đặt nó cạnh "Nhân bản" là mời người ta bấm nhầm.
- ✅ Giữ mục bị khoá trong menu kèm lý do ngắn. Ẩn đi thì người dùng không biết chức năng đó tồn tại để đi xin quyền.
- ✅ `Escape` luôn trả focus về nút đã mở menu.
- ✅ Nhãn mục là **động từ**: "Nhân bản chứng từ", không phải "Nhân bản".
- ❌ Không dùng menu để chọn giá trị cho form. Đó là việc của `Input` biến thể `select`.
- ❌ Không nhét form, không nhét đoạn văn, không nhét bảng vào menu.
- ❌ Không để `context` là đường đi duy nhất tới một hành động.
- ❌ Không mở menu rỗng mà không nói vì sao rỗng.
- ❌ Không tự động mở menu khi rê chuột. Menu mở bằng chủ ý, không bằng con trỏ đi ngang qua.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Menu nhiều cấp (mục mở ra menu con) có làm không? Hôm nay **không**, vì nó kéo theo cả một tập hành vi bàn phím và định vị mới. Nếu một màn cần phân cấp thật thì gần như luôn là dấu hiệu nên dùng `Drawer` có ô tìm | Màn đầu tiên gặp nhu cầu thật |
| 2 | Dạng trượt từ đáy ở màn nhỏ dùng lại `Drawer` hay tự dựng? Dùng lại thì đỡ một hiện thực, nhưng `Drawer` bẫy focus còn menu thì không — hai hành vi ngược nhau | Người dựng hai component này |
| 3 | Khoảng trễ trước khi đóng khi con trỏ rời menu là bao nhiêu? Không có trễ thì menu biến mất lúc người dùng đang với tới; trễ dài thì nó bám dai | Sau khi có màn thật để thử |
