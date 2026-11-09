---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Menu

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có ở `src/FE`, bọc `primeng/menu`; hiện thực **một** trong hai biến thể. Bảng dưới đây khai đúng những mục đã mở source ra so.

**Nền:** **bọc PrimeNG**. Theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, định vị một lớp nổi khi trang cuộn hoặc khi nó chạm mép màn hình là hành vi thuộc nhóm "khó": phải xử lý container cuộn, `position: fixed` lồng nhau, và lật hướng khi hết chỗ. Tự dựng là viết lại một bài toán đã được giải đúng.

## Đã có → còn thiếu

Component: `src/FE/src/app/shared/ui/menu/menu.component.ts` — selector `app-menu`, chữ ký mục `UiMenuItem`, `styleClass` gắn `.app-menu` và `.app-menu--<cỡ>`. Phần chạm DOM do PrimeNG dựng nằm ở `src/FE/src/styles/_thu-vien.scss` (khối `.app-menu`).

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| Nơi duy nhất import `primeng/menu` | Đúng — chú thích đầu file khai vai trò này | Chưa tự kiểm bằng lệnh là không còn chỗ nào khác import |
| API | `items`, `variant`, `size`, `header`, `ariaLabel` (`input.required`), `open`; output `selected`, `openChange` | — |
| `anchorEvent` | Có — input **không nằm trong** §API dự kiến | ⚠️ **Sai lệch có chủ đích, đã ghi rõ chứ không giấu.** PrimeNG không có `[visible]` khai báo cho popup: nó cần một sự kiện DOM thật để định vị overlay (`Menu.show(event)`). Chú thích đầu file giải thích đầy đủ. Cần một lượt bổ sung §API dự kiến cho khớp, không phải sửa code |
| Biến thể `anchored` | Có, là hình dạng duy nhất dựng | — |
| Biến thể `context` | Khai trong kiểu của `variant` | **Không render khác gì** — không có nhánh nào đọc `variant()`. Chú thích component khai không màn nào cần |
| `header` | Có, qua `<ng-template #start>`; dòng tiêu đề không bấm được, `--fs-2xs` `--fw-semibold` `--color-text-muted`, có vạch dưới `--color-border-subtle` | — |
| `separators` tách mục phá huỷ | Có, đúng luật Do / Don't: `model` xếp mục thường trước, chèn `{ separator: true }` rồi tới nhóm `danger` — vạch chỉ chèn khi **cả hai** nhóm đều có mục | — |
| Mục `danger` | Có, `styleClass` gắn `.app-menu__muc--nguy-hiem`, override màu `--color-danger` ở `_thu-vien.scss` | — |
| Mục khoá | Có: `disabled` truyền xuống, `command` chặn lại lần nữa, và lý do hiện qua `tooltip` của PrimeNG đặt `tooltipPosition: 'left'` | Spec đòi "một dòng lý do ngắn ở **mép phải**" của chính mục; code dùng tooltip nổi bên trái. Khác cách trình bày — cần chốt bên nào đúng |
| `selected` không phát khi mục khoá | Có, chặn trong `command` | — |
| Hai cỡ | Có ở `_thu-vien.scss`: `sm` → `min-height: --size-control-sm`, `--fs-xs`; `md` → `--size-control-md`, `--fs-sm`; cả hai đệm ngang `--sp-4` — khớp §Kích thước | Cỡ icon theo cỡ menu (`--icon-sm` / `--icon-md`) **không** đặt ở đâu cả |
| `groups` nhiều nhóm có nhãn | — | **Chưa có.** `UiMenuItem` có trường `group` nhưng `model` **không đọc tới nó** — mục chỉ được chia theo `danger`. Đây là một trường khai rồi bỏ không, dễ làm nơi gọi tưởng nó có tác dụng |
| `selectedKey` (menu chọn giá trị) | ✅ Có (đã đọc mã, đối chiếu 2026-10-06; FE-C2): `menu.component.ts` chuỗi `readonly selectedKey = input<string | null>(null);`; `onShow()` vá mọi `li[data-pc-section="item"]` (chuỗi trong `menu.component.ts`, đọc lại 2026-11-06) thành `role="menuitemradio"` + `aria-checked`, đặt mục đang chọn làm focus ảo qua `focusedOptionIndex.set(li.id)`; chọn lại mục đang chọn không phát `selected` (so `muc.key === this.selectedKey()` trong `command`). Test `menu.component.spec.ts` (`chọn lại mục đang aria-checked → không phát selected`, `khi overlay hiện: mọi mục menuitemradio, đúng mục mang aria-checked=true và nhận focus ảo`) xanh 2026-10-06, trước lượt nâng PrimeNG 21; kết quả trên bản đang cài chưa ai báo cho lượt soạn spec này | **Chưa đo:** focus ảo trên menu PrimeNG thật trong trình duyệt (test chạy ở Chrome headless, không phải ca người dùng bàn phím/trình đọc màn hình thật). Vá role bằng DOM là đường vòng phụ thuộc giá trị `data-pc-section` mà PrimeNG gắn lên từng mục — hỏng im lặng khi giá trị đó đổi ở một lượt nâng major (chú thích đầu hàm đã khai, chuỗi `Hỏng khi nào: PrimeNG đổi`); spec không ràng cách này. `onShow` giả định thứ tự `li` = mục thường trước, `danger` sau, khớp `model()` |
| `loading` / `error` | — | Chưa có `loadingTemplate`, `errorTemplate`, `aria-busy`. Chú thích component khai danh sách mục hiện luôn tĩnh nên chưa cần |
| `empty` | — | **Chưa có.** Không mục nào thì `model` ra mảng rỗng và menu mở ra trống trơn — đúng thứ §Trạng thái cấm ("không mở một menu rỗng không chữ"). Không có dòng chữ giải thích nào |
| Icon mục | `sangMenuItem` ghép `pi pi-${icon}`, tức nơi gọi truyền tên trần — **đúng** chiều đã chốt ở [`../Icons.md`](../Icons.md) §5 và đúng [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 | — |
| §Trạng thái `hover` + §Token dùng — `--dur-fast` | Có (đối chiếu 2026-10-06, chỉ đọc mã) — chuyển tiếp màu đi qua **một** khoá semantic: `src/FE/src/app/core/theme/prime-preset.ts` chuỗi `transitionDuration: 'var(--dur-fast)',` (ADR-0073, [`../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md)). Sub-preset của component trỏ về `{transition.duration}` hoặc `{form.field.transition.duration}`, nhánh sau cũng đổ về khoá này. Không khai biến riêng từng component — phương án đó đã bị ADR-0073 loại | Giá trị thật `120ms` trên trình duyệt **chưa đo**; mới đối chiếu mã |
| §Trạng thái — chuyển động mở/đóng lớp nổi (đọc mã 2026-11-07, PrimeNG 21.1.10) | **Có trong mã, theo bộ chung của mọi lớp nổi neo** ([`../DESIGN.md`](../DESIGN.md) §7 *Lớp nổi neo*). `p-menu` chạy lớp pha chung của thư viện (gói `primeng` đã cài, tệp `primeng-menu.mjs`, chuỗi `[pMotionName]="'p-anchored-overlay'"`); **một** khối đè ở `src/FE/src/styles/_thu-vien.scss`: § `body .p-anchored-overlay-enter-active` mang `var(--dur-base)` + `var(--ease-decelerate)`, § `body .p-anchored-overlay-leave-active` mang `var(--dur-base)` + `var(--ease-accelerate)`; keyframes của thư viện giữ nguyên. Lớp bọc không mang mã chuyển động nào: `menu.component.ts`, `menu.component.html` không chứa `transition`, `duration`, `motion`. Ca kiểm `menu.component.spec.ts`: `pha bung: --dur-base, --ease-decelerate`, `pha thu: --dur-base, --ease-accelerate`; người soạn chưa chạy test | Nhịp bung/thu trên trình duyệt **chưa đo** — câu thẩm mỹ ở [`../DESIGN.md`](../DESIGN.md) §10 câu 5. Hai ca token trên tự `pending` khi cờ giảm chuyển động bật (chuỗi `prefers-reduced-motion bật` trong `menu.component.spec.ts`), nên chúng không canh nhánh đó; nhánh đó do dòng ngay dưới canh. Dòng ngay dưới thay cho ⚠️ "mở xong đóng ngay" từng ghi ở đây |
| Lớp nổi gắn vào `body` (đọc mã và ca kiểm, 2026-11-07, PrimeNG 21.1.10) | **Có trong mã.** `menu.component.html` mở bằng `<p-menu` mang `appendTo="body"`. Vì sao cần: giá trị mặc định của thư viện là `'self'` (gói `primeng` đã cài, tệp `primeng-config.mjs`, chuỗi `overlayAppendTo = signal('self'`; `primeng-menu.mjs` chỉ chuyển hộp sang `body` khi `$appendTo()` khác `'self'`), và app không đặt `overlayAppendTo` ở đâu (`app/core/theme/*.ts`, `app/app.config.ts` không chứa chuỗi đó). Lớp nổi để tại chỗ nằm trong khung cuộn của trang: định vị lệch theo cha định vị (ô `sticky` của bảng), lời gọi focus cuộn khung, và bộ nghe cuộn của thư viện đóng menu — nên menu hàng mở xong đóng ngay, lộ rõ khi giảm chuyển động vì không còn hoạt ảnh ra che việc biến mất (theo chú thích ngay trên `describe` bên dưới trong `menu.component.spec.ts`). Ca Karma: `menu.component.spec.ts` `describe('MenuComponent — lớp nổi gắn vào body, không nằm trong khung cuộn của trang'` — `.p-menu` là con trực tiếp của `body` và không nằm trong khung cuộn của host. Ca e2e chạy dưới cờ giảm chuyển động (`playwright.config.ts`, chuỗi `reducedMotion: 'reduce'` ở khối `use`, áp mọi project): `src/FE/e2e/man/ca-lop-noi-neo.ts` hàm `moMenuVaKiemConMo` — menu còn mở sau khi đứng yên, `role="menu"`, nằm trong khung nhìn, sát cạnh dưới nút neo, nút mang `aria-expanded="true"` — gọi từ `10-nguoi-dung.spec.ts` `describe('người dùng — danh sách — lớp nổi neo khi giảm chuyển động'` và `11-vai-tro.spec.ts` `describe('vai trò — danh sách — lớp nổi neo khi giảm chuyển động'`, cùng ca `menu hàng: mở xong còn mở, role=menu, chọn được mục`. Phiên chính báo xanh 2026-11-07: Karma 1310 ca (ba lần chạy), e2e bốn màn 287/287; kết quả không lưu trong repo, người soạn chưa chạy lại | **Chưa đo:** e2e chỉ chạy một chế độ chuyển động (cờ bật), nên nhánh có hoạt ảnh chỉ do ca Karma về nơi gắn hộp canh, không có ca "còn mở sau khi đứng yên" cho nhánh đó. Menu của `Topbar` (báo lệch 32 px trước lượt sửa) dùng cùng lớp bọc nên nhận cùng sửa; chưa có ca đo toạ độ riêng cho nó. Lớp nổi do thư viện dựng ngoài ba lớp bọc đã sửa (`Menu`, `Autocomplete`, `Pagination`) **chưa rà** có cùng mặc định `'self'` hay không. Cảm nhận bằng mắt và trình đọc màn hình (focus trả về nút neo khi `Escape`) **chưa đo** |

Chưa đối chiếu — **lỗ mù chính của file này**: toàn bộ hình thức ở §Trạng thái (`default` nền/viền/`--radius-md`/`--shadow-3`, `hover` nền `--color-surface-2`, `focus-visible` với `outline-offset` **âm**, `active` nền `--color-surface-3`, `disabled` màu chữ và `aria-disabled`) **không nằm trong** hai tệp style đã mở. Chúng đến từ sub-preset `menu` của PrimeNG; tôi chưa soát preset nên chưa kết luận được khớp hay lệch. Cũng chưa đối chiếu §Responsive và §Accessibility (ngoài `ariaLabel` bắt buộc).

---

## Mục đích

Hiện một danh sách hành động ngắn trong một lớp nổi neo vào phần tử đã mở nó.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Gom các hành động phụ của một hàng bảng lại sau một nút, khi ba `IconButton` cạnh nhau là quá nhiều | 🛑 Chọn một **giá trị** để điền vào form → [`Input.md`](./Input.md) biến thể `select`. Menu phát ra hành động, `select` mang dữ liệu; lẫn hai thứ làm form không bind được |
| Menu tài khoản ở [`Topbar.md`](./Topbar.md): hồ sơ, ngôn ngữ, đăng xuất; menu theme ở đó — chọn **một** trong vài giá trị cấu hình ngoài form, qua `selectedKey` | 🛑 Chọn một nút trong cây → [`TreeSelect.md`](./TreeSelect.md); chọn từ danh mục lớn bằng cách gõ → [`Autocomplete.md`](./Autocomplete.md) |
| Hành động phụ của [`PageHeader.md`](./PageHeader.md) ở màn hẹp, khi không đủ chỗ cho nhiều nút | 🛑 Điều hướng chính của ứng dụng → [`Sidebar.md`](./Sidebar.md). Menu không phải chỗ giấu tuyến đi |
| Bảng chọn cột của [`DataTable.md`](./DataTable.md) | 🛑 Nội dung dài, có form, cần đọc kỹ → [`Dialog.md`](./Dialog.md) hoặc [`Drawer.md`](./Drawer.md). Menu quá năm bảy mục là dấu hiệu chọn sai component |

**Ngưỡng cứng:** quá **mười mục** thì đó không còn là menu. Lúc đó hoặc chia nhóm có tiêu đề, hoặc chuyển sang `Drawer` có ô tìm. Không có menu nhiều cấp (mục mở ra menu con) — cần phân cấp thật là dấu hiệu nên dùng `Drawer` có ô tìm; hai lớp nổi **nối tiếp** như mục "Ngôn ngữ" ở [`Topbar.md`](./Topbar.md) thì được.

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

Chỉ hai cỡ. Cỡ `lg` không có nghĩa cho một danh sách hành động: chiều cao mục phải theo mật độ của chỗ gọi nó, và không chỗ nào trong Core gọi menu từ một control cỡ `lg`. Mục cao `--size-control-lg` dưới `$bp-sm` (§Responsive) là điều chỉnh theo vùng chạm, không phải một cỡ.

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
| `loading` | Chỉ áp cho menu có nội dung tải theo yêu cầu: thân menu hiện `loadingTemplate` — màn ghép ba dòng [`SkeletonLoader.md`](./SkeletonLoader.md) — và `aria-busy="true"` trên hộp menu. Bề rộng menu **giữ nguyên** theo giá trị tối thiểu, nếu không menu sẽ nhảy cỡ ngay trước mắt người dùng | Có |
| `error` | Tải danh sách mục hỏng: thân menu hiện `errorTemplate` — màn ghép một dòng chữ lỗi màu `--color-danger` kèm nút "Thử lại". Menu **không tự đóng** khi lỗi — đóng đi thì người dùng không biết vừa có chuyện gì | Có |
| `empty` | Không mục nào khả dụng: hiện đúng một dòng chữ `--color-text-muted` nói vì sao rỗng ("Không có hành động nào cho bản ghi này"). 🛑 **Không mở một menu rỗng không chữ** — người dùng sẽ bấm lại vài lần rồi nghĩ chức năng hỏng | Có |

**Mở và đóng lớp nổi:** theo bộ chung của mọi lớp nổi neo, ở mọi điểm ngắt vì hộp vẫn là popover (§Responsive) — [`../DESIGN.md`](../DESIGN.md) §7 *Lớp nổi neo*; spec này không đặt số riêng.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-subtle`, `--color-danger`, `--color-focus` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-semibold`, `--lh-snug`, `--ls-wide` |
| Khoảng cách | `--sp-1`, `--sp-2`, `--sp-3`, `--sp-4` |
| Hình dạng | `--radius-sm`, `--radius-md`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg` (chỉ dưới `$bp-sm`, §Responsive), `--icon-sm`, `--icon-md`, `--layout-sidebar-w` |
| Bóng, lớp | `--shadow-3`, `--z-popover` |
| Chuyển động | `--dur-fast`, `--ease-standard` — đổi nền một mục khi hover và focus, qua khoá thời lượng chung của preset. Bung và thu cả hộp: §Trạng thái, mục "Mở và đóng lớp nổi" |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Menu nổi, neo vào phần tử mở nó. Lật lên trên hoặc sang trái khi không đủ chỗ ở hướng mặc định |
| `$bp-sm` … `$bp-md` | Giữ nguyên dạng nổi; cỡ mục nâng lên `md` ở mọi vị trí |
| < `$bp-sm` | **Giữ popover**, mỗi mục cao `--size-control-lg` để đủ vùng chạm; thư viện lật hướng hoặc dịch vào trong viewport khi hết chỗ. Không đổi sang [`Drawer.md`](./Drawer.md) `bottom` |

**Vì sao giữ popover ở màn nhỏ, khác [`Autocomplete.md`](./Autocomplete.md) và [`TreeSelect.md`](./TreeSelect.md):** hai ô đó có bàn phím ảo đẩy lớp nổi, `Menu` không có ô gõ nên lý do đó không áp; và một menu mở từ trong `Drawer` (mục "Ngôn ngữ" ở `Topbar`, hành động trong drawer sửa) sẽ thành `Drawer` chồng `Drawer` — [`Drawer.md`](./Drawer.md) §Do / Don't cấm. Cái giá: mục cao hơn một bậc ở ngưỡng này, và nơi neo phải đủ chỗ cho tối đa mười mục.

Menu mở bằng bấm và đóng bằng chọn mục, `Escape` hoặc bấm ra ngoài (§API `openChange`) — **không** đóng khi con trỏ rời menu, nên không có khoảng trễ nào phải chốt.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | Hộp menu mang `role="menu"`; mỗi mục `role="menuitem"`. Mục bật/tắt dùng `role="menuitemcheckbox"` kèm `aria-checked`. Menu chọn một giá trị (`selectedKey` khác `null`): **mọi** mục `role="menuitemradio"`, mục trùng `selectedKey` mang `aria-checked="true"` và nhận focus khi menu mở |
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
| `items` | input | `ReadonlyArray<UiMenuItem>` | `[]` | Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. Tên mang tiền tố `Ui` để không trùng entity `MenuItem` phía backend. Trường `icon` là **tên trần**, không tiền tố — [`../Icons.md`](../Icons.md) §5; cùng kiểu này đi vào `menuItems` của [`Topbar.md`](./Topbar.md), nên một mục chuyển giữa hai chỗ phải đọc được y nguyên |
| `variant` | input | `'anchored' \| 'context'` | `'anchored'` | |
| `size` | input | `'sm' \| 'md'` | `'md'` | |
| `header` | input | `string \| null` | `null` | `null` = không có dòng tiêu đề |
| `ariaLabel` | input | `string` | — | **Bắt buộc.** Không có mặc định; một menu không nhãn là một menu vô danh với trình đọc màn hình |
| `open` | input | `boolean` | `false` | Trang cha giữ nguồn sự thật, để đóng được menu từ bên ngoài |
| `selectedKey` | input | `string \| null` | `null` | Khác `null` → menu chọn một giá trị: mọi mục `menuitemradio`, mục trùng khoá `aria-checked="true"` (§Accessibility). `selected` không phát khi chọn lại mục đang `aria-checked`. Dùng cho `Menu` theme của [`Topbar.md`](./Topbar.md) |
| `loading` | input | `boolean` | `false` | |
| `loadingTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Nội dung thân menu khi `loading`. Bắt buộc khác `null` khi menu dùng `loading` |
| `errorTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Khối lỗi trong thân menu, gồm nút "Thử lại". Màn chỉ truyền khi tải mục hỏng — [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 |
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

Không còn câu hỏi để ngỏ ở mục này.

Hai khoản spec và mã chưa khớp — cách hiện lý do của một mục bị khoá, và cỡ icon theo cỡ menu — ghi ở §Đã có → còn thiếu. Chúng chờ một lượt đối chiếu, không phải một hướng thiết kế chưa ai chọn.
