---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Footer

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có ở `src/FE`. Bảng dưới đây khai đúng những mục đã mở source ra so.

**Nền:** tự dựng, không bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, đây là component thuần trình bày: vài dòng chữ và vài liên kết, không có hành vi nào để đi mượn.

## Đã có → còn thiếu

Component: `src/FE/src/app/shared/components/footer/footer.component.ts` — selector `app-footer`, class gốc `.footer`, chữ ký liên kết `FooterLink`.

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| API | `variant` (`'full' \| 'compact' \| 'auth' \| 'sidebar'`), `size`, `version`, `copyright`, `links`, `showDivider`; không có output nào | — |
| Component dumb | Đúng — không inject service cấu hình, `version` và `copyright` do nơi gọi truyền vào | — |
| Nguồn của `version` | ✅ Có (đã đọc mã, đối chiếu 2026-10-06; FE-C2, Q59): `core/config/build-info.ts` chuỗi `export const MA_COMMIT: string | null = null;` (cây nguồn); `core/config/phien-ban.ts` chuỗi `maCommitNgan` cắt `slice(0, 7)`, rỗng → `null`; `shell.component.html`, `dang-nhap.page.html`, `doi-mat-khau-bat-buoc.page.html` đều có chuỗi `[version]="maCommit ? ('chung.chanTrang.phienBan' | translate: { ma: maCommit }) : null"`; `vi.json` khoá `chung.chanTrang.phienBan` = `Phiên bản {{ma}}`. `footer.component.html` vẽ `·` `aria-hidden="true"` chỉ khi có bản quyền; `version = null` thì ẩn cả phần. Test `footer.component.spec.ts` (3 ca: có mã, `null`, không bản quyền) và `phien-ban.spec.ts` (3 ca) xanh 2026-10-06 | **Chưa có bằng chứng đầu-cuối:** CI ghi đè `build-info.ts` bằng `$GITHUB_SHA` (`.github/workflows/docs-gate.yml`, chuỗi `export const MA_COMMIT`) nhưng job FE chưa từng chạy (`.claude/CLAUDE.md` §4) nên chưa ai thấy mã thật trên bản dựng; máy dev `MA_COMMIT = null` → phần phiên bản ẩn, đúng spec. Chưa kiểm khổ < `$bp-md` (xếp dọc) trên trình duyệt thật |
| Trạng thái `empty` | Có, đúng luật spec: `@if (copyright() \|\| version() \|\| links().length > 0)` bọc cả `<footer>`, nên không truyền gì thì **không render phần tử nào** — không còn một dải trống mang vạch phân cách | — |
| Biến thể `compact` | Có trên thực tế, qua điều kiện `variant() === 'full'` ở khối liên kết — `compact` giữ bản quyền và phiên bản, bỏ nhóm liên kết | Không có class riêng `.footer--compact`; `compact` là "không phải `full`", đúng kết quả nhưng khác cách spec mô tả |
| Biến thể `auth` | `.footer--auth` căn giữa nội dung | **Chưa bỏ vạch phân cách.** §Biến thể khai `auth` là "căn giữa, **không có vạch phân cách**", nhưng `.footer--auth` không đụng `border-top`, và `showDivider` mặc định `true` — nên `auth` vẫn có vạch trừ khi nơi gọi tự truyền `showDivider = false` |
| Hai cỡ | `md` ở trạng thái nghỉ (`--sp-6` / `--layout-page-pad`), `.footer--sm` (`--sp-4` / `--sp-5`); cỡ chữ `--fs-2xs` chung cho cả hai — khớp §Kích thước | — |
| `default` | Chữ `--color-text-muted`, `--fw-regular`; vạch trên `--border-w` màu `--color-border-subtle` | Chưa đặt `--lh-normal` mà §Trạng thái khai |
| `hover` của liên kết | Có, đúng spec: chữ chuyển `--color-brand`, gạch chân hiện, bọc trong `@media (hover: hover)`; bản thân dải không có hover | — |
| `focus-visible` của liên kết | Có, `--border-w-strong` + `--color-focus` + `outline-offset: 2px` | — |
| `active` của liên kết | — | **Chưa có.** §Trạng thái đòi chữ chuyển `--color-brand-active` khi đang nhấn; không có quy tắc `:active` nào trong SCSS |
| Ba trạng thái "không áp dụng" | Đúng — không có control nào để `disabled`, không có đường dữ liệu nào để `loading`/`error` | — |
| Liên kết ngoài | Có, `target="_blank"` kèm `rel="noopener"` và icon `pi-external-link` mang `aria-hidden` | — |
| Vị trí trong khung ứng dụng — đáy `Sidebar`, biến thể `sidebar` (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08, FE-1): `footer.component.ts` chuỗi `readonly variant = input<'full' \| 'compact' \| 'auth' \| 'sidebar'>('full');`; `footer.component.html` gắn `[class.footer--sidebar]`, bỏ dấu `·` khi `variant() === 'sidebar'`; `footer.component.scss` § `.footer--sidebar` khai cột hai dòng, căn trái. `shell.component.html` đặt `<app-footer sidebarFooter variant="sidebar" size="sm"` trong `<app-sidebar>`, không còn Footer sau `main`. Ca `FooterComponent — biến thể sidebar › hai dòng (bản quyền, phiên bản), không dấu ·` (`footer.component.spec.ts`; người soạn chưa chạy test) | Hình thức (nền trong suốt lấy nền `Sidebar`, vạch phân cách trên) **chưa đo** — `.footer--sidebar` không đụng `border-top`, nên vạch trên theo `showDivider` mặc định |

Chưa đối chiếu — **lỗ mù chính của file này**: §Responsive của `full` và `compact` (biến thể `auth` đã đọc mã 2026-10-08 — đoạn ✅ trong §Responsive). SCSS của component chỉ có `flex-wrap: wrap`; không có `@media` nào cho ba ngưỡng spec khai, nên hành vi "xếp dọc căn trái dưới `$bp-md`" và việc đổi đệm ngang sang `--layout-page-pad-sm` **không** thấy ở đây. Chưa kiểm được là chúng nằm ở tầng bố cục khác hay chưa dựng. Cũng chưa soát §Accessibility ngoài hai khoản liên kết ngoài, và chưa soát tầng i18n của `copyright`/`label`.

---

## Mục đích

Đóng cuối một trang bằng thông tin nhận dạng bản dựng: phiên bản, bản quyền, và một nhóm nhỏ liên kết phụ.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Đáy [`Sidebar.md`](./Sidebar.md) của khung ứng dụng — biến thể `sidebar` | 🛑 Điều hướng chính của ứng dụng → [`Sidebar.md`](./Sidebar.md). `Footer` không phải chỗ để chứa menu |
| Cuối một `AuthCard` — biến thể `auth` | 🛑 Hành động của người dùng đang đăng nhập (đăng xuất, đổi mật khẩu) → [`Topbar.md`](./Topbar.md) |
| Ghi số phiên bản để người dùng đọc khi báo lỗi; liên kết phụ: điều khoản, trợ giúp, liên hệ | 🛑 Hành động của một biểu mẫu (Lưu, Huỷ) → chân [`Dialog.md`](./Dialog.md) hoặc [`Toolbar.md`](./Toolbar.md); 🛑 thông báo cần đọc ngay → [`NoticeBanner.md`](./NoticeBanner.md); 🛑 cuối `main` của khung ứng dụng — xem dưới |

**Quyết định (2026-10-08, người dùng chốt): trong khung ứng dụng, `Footer` nằm ở đáy `Sidebar`, không ở cuối `main`.**

Vì sao: khung ứng dụng cao đúng khung nhìn và màn chính của Core — màn danh sách — lấp đúng chiều cao còn lại của `main`, chỉ thân bảng cuộn ([`../Templates/ListScreen.md`](../Templates/ListScreen.md) §4). Đặt `Footer` cuối `main` ở bố cục đó chỉ có hai kết cục, cả hai đều tệ: hoặc nó ăn vĩnh viễn một dải chiều cao của bảng để hiện một dòng bản quyền người dùng đọc đúng một lần, hoặc nó đẩy `main` thành vùng cuộn thứ hai ngay ở màn danh sách. Đáy `Sidebar` thì có sẵn chỗ trống dọc bên dưới cây menu: chân trang luôn thấy, không lấy chiều cao nào của nội dung.

Cái giá, nói thẳng: dải `Sidebar` hẹp, nên bản quyền và phiên bản xếp thành hai dòng; ở `Sidebar` thu gọn thì chân trang không vẽ (§Responsive). Đây vẫn **không** phải `position: fixed` — `Footer` đứng ở đáy cột `Sidebar` nhờ bố cục của chính cột đó.

Khung xác thực không có `Sidebar`, nên giữ cách riêng của nó: một cột cao tối thiểu `100dvh` gồm `main` và `Footer` `auth`, `Footer` ở đáy cột và thuộc luồng cuộn — [`AuthCard.md`](./AuthCard.md) §Responsive.

## Biến thể

| Biến thể | Chứa gì | Dùng khi |
| --- | --- | --- |
| `full` | Bản quyền + phiên bản + nhóm liên kết phụ, một hàng | **Mặc định của API.** Hôm nay không màn Core nào dùng — §Cần chốt 1 |
| `compact` | Bản quyền + phiên bản, không có liên kết | Trang có nội dung dài, không muốn thêm nhiễu ở cuối |
| `sidebar` | Bản quyền + phiên bản, **xếp hai dòng**, căn trái, vạch trên; không có liên kết, không dấu `·` ngăn (hai dòng đã tách) | Đáy [`Sidebar.md`](./Sidebar.md) của khung ứng dụng — chốt 2026-10-08 |
| `auth` | Cùng nội dung `compact` nhưng căn giữa ở mọi bề rộng (§Responsive), không có vạch phân cách | Dưới [`AuthCard.md`](./AuthCard.md), nơi không có khung ứng dụng. Nằm ở đáy cột xác thực cao tối thiểu `100dvh`: đủ chỗ thì sát đáy khung nhìn, thiếu chỗ thì ngay sau khung — [`AuthCard.md`](./AuthCard.md) §Responsive |

## Kích thước

| Cỡ | Đệm dọc | Đệm ngang | Cỡ chữ | Dùng khi |
| --- | --- | --- | --- | --- |
| `md` | `--sp-6` | `--layout-page-pad` | `--fs-2xs` | **Mặc định.** Khớp lề của `main` |
| `sm` | `--sp-4` | `--sp-5` | `--fs-2xs` | Biến thể `auth`, biến thể `sidebar`; trang trong `Dialog`. Đệm ngang `--sp-5` trùng đệm ngang mục nav của `Sidebar`, nên chữ chân trang thẳng mép với nhãn menu |

Cỡ chữ **không đổi giữa hai cỡ**. `--fs-2xs` đã là bậc nhỏ nhất của thang ([`../DESIGN.md`](../DESIGN.md) §3.2) và thang đó cấm đẻ bậc mới; hai cỡ của `Footer` chỉ khác nhau ở đệm.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Chữ `--color-text-muted`, `--fw-regular`, `--lh-normal`; vạch trên bằng `--color-border-subtle` dày `--border-w` | Có |
| `hover` | Chỉ áp cho **liên kết** bên trong: chữ chuyển `--color-brand`, gạch chân hiện. Bọc trong `@media (hover: hover)`. Bản thân dải không có hover | Có |
| `focus-visible` | Liên kết nhận `outline` `--border-w-strong` màu `--color-focus`, `outline-offset` theo [`../DESIGN.md`](../DESIGN.md) §2.6. Dải không nhận focus | Có |
| `active` | Liên kết đang nhấn: chữ `--color-brand-active`. Không dịch chuyển vị trí — chữ nhỏ mà nhảy 1px trông như lỗi render | Có |
| `disabled` | **Không áp dụng.** `Footer` không chứa control có thể vô hiệu hoá. Một liên kết tạm thời không dùng được thì bỏ hẳn khỏi danh sách, đừng để nó xám ở đó | — |
| `loading` | **Không áp dụng.** Toàn bộ nội dung là hằng số của bản dựng, không đến từ một lần gọi API nào | — |
| `error` | **Không áp dụng.** Không có thao tác nào ở đây để hỏng. Không có mã commit (bản chạy máy dev, bản dựng không qua CI) **không phải lỗi**: nơi gọi truyền `version = null` và phần phiên bản biến mất — §Phiên bản hiển thị | — |
| `empty` | Không truyền phiên bản lẫn liên kết → chỉ còn dòng bản quyền. Không truyền gì cả → **không render phần tử nào**, không để lại một dải trống có vạch phân cách | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-text-muted`, `--color-border-subtle`, `--color-brand`, `--color-brand-active`, `--color-focus`, `--color-bg` |
| Chữ, khoảng cách | `--fs-2xs`, `--fw-regular`, `--lh-normal`, `--sp-4`, `--sp-5`, `--sp-6`, `--sp-8` |
| Hình dạng, chuyển động | `--border-w`, `--border-w-strong`, `--dur-fast`, `--ease-standard` |
| Bố cục | `--layout-page-pad`, `--layout-page-pad-sm` |

Nền `Footer` **để trong suốt**, lấy nền của vùng chứa — `--color-bg` ở khung xác thực, `--color-surface` của `Sidebar` ở biến thể `sidebar`. Cho nó một nền riêng sẽ tạo ra một khối bề mặt lơ lửng, đọc như một `Card` không có tiêu đề.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Một hàng: bản quyền và phiên bản bên trái, nhóm liên kết bên phải; đệm ngang `--layout-page-pad` |
| < `$bp-md` | Xếp dọc, căn trái: bản quyền, rồi phiên bản, rồi liên kết; đệm ngang đổi sang `--layout-page-pad-sm` |
| < `$bp-xs` | Nhóm liên kết xuống dòng tự do, khe dọc `--sp-3`; vùng bấm mỗi liên kết nới bằng `padding` để đạt tối thiểu |

Bảng trên áp cho biến thể `full` và `compact`.

**Biến thể `auth` không đổi theo điểm ngắt** (chốt 2026-10-08): ở mọi bề rộng, kể cả dưới `$bp-md`, nó giữ một hàng **căn giữa** — bản quyền rồi phiên bản, ngăn bằng dấu `·` — với đệm của cỡ `sm`. Không xếp dọc, không căn trái, không đổi đệm ngang. Vì sao: chân trang xác thực nằm dưới một khung cũng căn giữa ([`AuthCard.md`](./AuthCard.md) §Responsive); đổi sang căn trái ở màn hẹp làm nó lệch trục với khung ngay trên, trong khi nội dung chỉ một dòng ngắn và không có nhóm liên kết nào cần xếp lại.

✅ **CÓ THẬT** (đọc mã 2026-10-08; chưa đo trên trình duyệt): `footer.component.scss` § `.footer--auth` khai `justify-content: center` và `text-align: center`; tệp không có `@media` nào, nên hành vi trên giống nhau ở mọi bề rộng.

Biến thể `sidebar` đi theo dạng của [`Sidebar.md`](./Sidebar.md), không theo điểm ngắt của riêng nó:

| Dạng `Sidebar` | Biến thể `sidebar` |
| --- | --- |
| `expanded` | Hiện ở đáy dải, dưới cây menu; cây menu dài thì cuộn bên trên, chân trang đứng yên |
| `collapsed` | **Không vẽ** — dải chỉ icon không đủ chỗ cho chữ. Người dùng mở rộng `Sidebar` để đọc phiên bản |
| `drawer` | Ở chân của `Drawer` điều hướng — slot chân, dưới cây menu |

`Footer` có mặt ở **mọi** trang trong khung ứng dụng, kể cả màn bảng chiếm toàn màn hình: khung ở [`../Screens/00-khung-ung-dung.md`](../Screens/00-khung-ung-dung.md) đặt nó một lần, trong `Sidebar`, cho mọi tuyến con.

## Phiên bản hiển thị

Quyết định 2026-10-06 (Q59): số phiên bản ở chân trang là **mã commit của bản dựng**, do CI nhúng vào lúc build. Truy ngược được từ lời báo lỗi của người dùng về đúng commit.

| Khoản | Quy định |
| --- | --- |
| Nguồn | Mã commit đầy đủ nhúng lúc build — cơ chế nhúng là việc thi công của `frontend-expert`, spec chỉ ràng giá trị. Là hằng số của bản dựng: không gọi API, không đọc lúc chạy |
| Cắt | 7 ký tự đầu của mã. Nơi gọi cắt, `Footer` không cắt |
| Câu hiển thị | `Phiên bản {{ma}}` — khoá `chung.chanTrang.phienBan`, tham số `ma`. Mã là dữ liệu, không nhúng vào chuỗi dịch (cùng cách với năm bản quyền) |
| Chỗ đặt | Cùng dải với bản quyền, **đứng sau bản quyền**, ngăn bằng dấu `·` trang trí (`aria-hidden="true"`); riêng biến thể `sidebar` thì phiên bản là **dòng thứ hai**, không có dấu `·`. Khung ứng dụng (`sidebar`) và khung xác thực (`auth`) đều hiện — bản này truyền `version` ở cả hai nơi gọi |
| Kiểu chữ | Như dòng bản quyền (`--fs-2xs`, `--color-text-muted`). Mã là chuỗi cho người đọc đọc ra miệng khi báo lỗi: không đổi hoa/thường, không biến thành liên kết |
| **Không có mã commit** | **Ẩn hẳn phần phiên bản**, cả dấu `·` ngăn. Không chữ giữ chỗ ("dev", "không rõ", "unknown"): một chữ giữ chỗ trông như một số phiên bản thật và bị báo lỗi kèm theo |

Chọn "ẩn" thay vì chữ giữ chỗ vì `version = null` đã là hợp đồng có sẵn của component (§API dự kiến) — không thêm nhánh hiển thị nào cho `Footer`.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<footer>` thật |
| Vai trò ARIA | `contentinfo` — có sẵn khi `<footer>` là con trực tiếp của `<body>`. 🛑 **Bẫy:** đặt `<footer>` **bên trong** `<main>` hay bên trong một `<section>` sẽ **mất** vai trò `contentinfo`, và người dùng trình đọc màn hình mất một điểm nhảy landmark. Nếu bố cục buộc phải lồng, đặt `role="contentinfo"` tường minh |
| Số lượng | Chỉ **một** landmark `contentinfo` trên một trang |
| Biến thể `sidebar` | `<footer>` đứng **ngoài** `<nav>` của `Sidebar` — trong khối ngoài không mang vai trò của dải ([`Sidebar.md`](./Sidebar.md) §"Chân dải"). Đặt trong `<nav>` thì nó mất `contentinfo` và bị đọc như một phần của điều hướng. Khối ngoài của `Sidebar` mà là `<aside>` thì đặt `role="contentinfo"` tường minh theo dòng "Vai trò ARIA" |
| Liên kết | `<a>` thật với `href` hoặc `routerLink`. Liên kết ra ngoài mang icon `pi-external-link` ([`../Icons.md`](../Icons.md) §5) và nhãn nói rõ là mở tab mới |
| Bàn phím | Nằm cuối thứ tự Tab tự nhiên. Không đặt `tabindex` dương cho bất cứ thứ gì ở đây |
| Focus | Vòng focus của liên kết đạt ≥ 3:1 nhờ `outline-offset` |
| Vùng bấm | Mỗi liên kết ≥ 24×24px, nới bằng `padding` dọc. Tương phản `--color-text-muted` trên `--color-bg` là 6.68:1 sáng / 7.74:1 tối ([`../DESIGN.md`](../DESIGN.md) §2.2) — qua AA dù chữ nhỏ |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8. Năm bản quyền là dữ liệu, không nhúng vào chuỗi dịch |

**Vì sao `--fs-2xs` được phép ở đây mà không được phép cho một câu:** [`../DESIGN.md`](../DESIGN.md) §3.2 giới hạn bậc nhỏ nhất cho **nhãn ngắn**. Nội dung `Footer` đúng là nhãn ngắn — một dòng bản quyền, một chuỗi phiên bản, vài từ mỗi liên kết. Ngày `Footer` phải chứa một đoạn văn thì đoạn đó dùng `--fs-xs` trở lên, hoặc nó không thuộc về `Footer`.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'full' \| 'compact' \| 'auth' \| 'sidebar'` | `'full'` | `platform/shell` truyền `'sidebar'` (chốt 2026-10-08) |
| `size` | input | `'sm' \| 'md'` | `'md'` | Biến thể `sidebar` và `auth` dùng `'sm'` |
| `version` | input | `string \| null` | `null` | **Dòng phiên bản đã dịch và đã ghép sẵn ở nơi gọi**, nguồn là mã commit lúc build — định dạng ở §Phiên bản hiển thị. Thời điểm dựng không hiện ở đây — nó ở health check và log. `null` thì ẩn hẳn phần này |
| `copyright` | input | `string \| null` | `null` | Đã dịch và đã ghép năm ở nơi gọi |
| `links` | input | `ReadonlyArray<FooterLink>` | `[]` | Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 |
| `showDivider` | input | `boolean` | `true` | Biến thể `auth` đặt `false` |

`Footer` là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5): nó **không** tự đọc phiên bản từ một service cấu hình. Component khung ở tầng nền tảng đọc phiên bản rồi truyền xuống. Nghe thừa một lớp, nhưng đó là thứ giữ cho `Footer` dựng được trong test chỉ bằng vài input, không cần mock cấu hình bản dựng.

`Footer` không có `output()` nào. Mọi thứ bấm được ở đây là điều hướng, và điều hướng đi bằng `<a>` chứ không bằng một sự kiện — cùng lý do đã ghi ở [`Button.md`](./Button.md).

## Do / Don't

- ✅ Dùng `<footer>` và kiểm rằng nó thật sự mang `contentinfo`.
- ✅ Trong khung ứng dụng, đặt `Footer` ở đáy `Sidebar` (biến thể `sidebar`); ở khung xác thực, để nó cuộn cùng trang.
- ✅ Ghi số phiên bản — nó là thứ đầu tiên cần hỏi khi người dùng báo lỗi.
- ✅ Giữ danh sách liên kết ngắn; `Footer` không phải bản đồ trang.
- ❌ Không dính `Footer` vào đáy màn hình bằng `position: fixed`.
- ❌ Không cho `Footer` một nền bề mặt riêng — nó sẽ đọc như một `Card`.
- ❌ Không đặt hành động nghiệp vụ vào `Footer`, và không render một dải rỗng có vạch phân cách khi không có nội dung nào.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Biến thể `full` (có nhóm liên kết phụ) không còn nơi dùng trong Core từ khi khung ứng dụng chuyển sang `sidebar`. Giữ làm điểm cho dự án hạ nguồn có liên kết điều khoản/trợ giúp — và nếu giữ thì nhóm liên kết đặt ở đâu khi chân trang nằm trong dải `Sidebar` hẹp — hay rút? | Người dùng — khi dự án hạ nguồn đầu tiên cần liên kết phụ ở chân trang |
