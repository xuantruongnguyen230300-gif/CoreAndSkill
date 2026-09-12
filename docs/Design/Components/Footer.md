---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Footer

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, đây là component thuần trình bày: vài dòng chữ và vài liên kết, không có hành vi nào để đi mượn.

---

## Mục đích

Đóng cuối một trang bằng thông tin nhận dạng bản dựng: phiên bản, bản quyền, và một nhóm nhỏ liên kết phụ.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Cuối vùng nội dung của khung ứng dụng | 🛑 Điều hướng chính của ứng dụng → [`Sidebar.md`](./Sidebar.md). `Footer` không phải chỗ để chứa menu |
| Cuối một `AuthCard` | 🛑 Hành động của người dùng đang đăng nhập (đăng xuất, đổi mật khẩu) → [`Topbar.md`](./Topbar.md) |
| Ghi số phiên bản để người dùng đọc khi báo lỗi; liên kết phụ: điều khoản, trợ giúp, liên hệ | 🛑 Hành động của một biểu mẫu (Lưu, Huỷ) → chân [`Dialog.md`](./Dialog.md) hoặc [`Toolbar.md`](./Toolbar.md); 🛑 thông báo cần đọc ngay → [`NoticeBanner.md`](./NoticeBanner.md) |

**Quyết định: `Footer` là nội dung trang, không phải khung ứng dụng — nó nằm trong luồng cuộn, không dính đáy màn hình.**

Cái giá của lựa chọn ngược lại rất cụ thể. Một `Footer` dính đáy ăn mất một dải chiều cao ở **mọi** thời điểm, trên **mọi** màn hình. Màn hình chính của Core là bảng dữ liệu, và chiều cao khả dụng của một bảng đã bị `Topbar` cắt mất một phần rồi ([`../DESIGN.md`](../DESIGN.md) §6.1). Cắt thêm một dải nữa để hiển thị vĩnh viễn một dòng bản quyền là đổi số hàng dữ liệu nhìn thấy được lấy một thông tin người dùng đọc đúng một lần trong đời.

Cái giá của lựa chọn đã chọn cũng phải nói thẳng: trên một trang ngắn, `Footer` sẽ nằm lơ lửng giữa màn hình với một khoảng trống bên dưới. Xử lý bằng cách cho vùng nội dung chiếm tối thiểu toàn bộ chiều cao còn lại và đẩy `Footer` xuống đáy vùng đó — nó vẫn thuộc luồng cuộn, chỉ là không bị treo giữa trang. Đây **không** phải `position: fixed`.

## Biến thể

| Biến thể | Chứa gì | Dùng khi |
| --- | --- | --- |
| `full` | Bản quyền + phiên bản + nhóm liên kết phụ | **Mặc định.** Cuối trang trong khung ứng dụng |
| `compact` | Bản quyền + phiên bản, không có liên kết | Trang có nội dung dài, không muốn thêm nhiễu ở cuối |
| `auth` | Cùng nội dung `compact` nhưng căn giữa, không có vạch phân cách | Dưới [`AuthCard.md`](./AuthCard.md), nơi không có khung ứng dụng |

## Kích thước

| Cỡ | Đệm dọc | Đệm ngang | Cỡ chữ | Dùng khi |
| --- | --- | --- | --- | --- |
| `md` | `--sp-6` | `--layout-page-pad` | `--fs-2xs` | **Mặc định.** Khớp lề của `main` |
| `sm` | `--sp-4` | `--sp-5` | `--fs-2xs` | Biến thể `auth`; trang trong `Dialog` |

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
| `error` | **Không áp dụng.** Không có thao tác nào ở đây để hỏng. Nếu số phiên bản không đọc được thì đó là lỗi cấu hình bản dựng, xử lý ở tầng dựng chứ không hiển thị trong `Footer` | — |
| `empty` | Không truyền phiên bản lẫn liên kết → chỉ còn dòng bản quyền. Không truyền gì cả → **không render phần tử nào**, không để lại một dải trống có vạch phân cách | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-text-muted`, `--color-border-subtle`, `--color-brand`, `--color-brand-active`, `--color-focus`, `--color-bg` |
| Chữ, khoảng cách | `--fs-2xs`, `--fw-regular`, `--lh-normal`, `--sp-4`, `--sp-5`, `--sp-6`, `--sp-8` |
| Hình dạng, chuyển động | `--border-w`, `--border-w-strong`, `--dur-fast`, `--ease-standard` |
| Bố cục | `--layout-page-pad`, `--layout-page-pad-sm`, `--layout-container-max` |

Nền `Footer` **để trong suốt**, lấy `--color-bg` của trang. Cho nó một nền `--color-surface` riêng sẽ tạo ra một khối bề mặt lơ lửng ở cuối trang, đọc như một `Card` không có tiêu đề.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Một hàng: bản quyền và phiên bản bên trái, nhóm liên kết bên phải; đệm ngang `--layout-page-pad` |
| < `--bp-md` | Xếp dọc, căn trái: bản quyền, rồi phiên bản, rồi liên kết; đệm ngang đổi sang `--layout-page-pad-sm` |
| < `--bp-xs` | Nhóm liên kết xuống dòng tự do, khe dọc `--sp-3`; vùng bấm mỗi liên kết nới bằng `padding` để đạt tối thiểu |

Bề rộng nội dung khớp `--layout-container-max` giống `main`, để dòng bản quyền thẳng lề với nội dung phía trên chứ không dạt ra mép màn hình rộng.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<footer>` thật |
| Vai trò ARIA | `contentinfo` — có sẵn khi `<footer>` là con trực tiếp của `<body>`. 🛑 **Bẫy:** đặt `<footer>` **bên trong** `<main>` hay bên trong một `<section>` sẽ **mất** vai trò `contentinfo`, và người dùng trình đọc màn hình mất một điểm nhảy landmark. Nếu bố cục buộc phải lồng, đặt `role="contentinfo"` tường minh |
| Số lượng | Chỉ **một** landmark `contentinfo` trên một trang |
| Liên kết | `<a>` thật với `href` hoặc `routerLink`. Liên kết ra ngoài mang icon `pi-external-link` ([`../Icons.md`](../Icons.md) §5) và nhãn nói rõ là mở tab mới |
| Bàn phím | Nằm cuối thứ tự Tab tự nhiên. Không đặt `tabindex` dương cho bất cứ thứ gì ở đây |
| Focus | Vòng focus của liên kết đạt ≥ 3:1 nhờ `outline-offset` |
| Vùng bấm | Mỗi liên kết ≥ 24×24px, nới bằng `padding` dọc. Tương phản `--color-text-muted` trên `--color-bg` là 6.68:1 sáng / 7.74:1 tối ([`../DESIGN.md`](../DESIGN.md) §2.2) — qua AA dù chữ nhỏ |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8. Năm bản quyền là dữ liệu, không nhúng vào chuỗi dịch |

**Vì sao `--fs-2xs` được phép ở đây mà không được phép cho một câu:** [`../DESIGN.md`](../DESIGN.md) §3.2 giới hạn bậc nhỏ nhất cho **nhãn ngắn**. Nội dung `Footer` đúng là nhãn ngắn — một dòng bản quyền, một chuỗi phiên bản, vài từ mỗi liên kết. Ngày `Footer` phải chứa một đoạn văn thì đoạn đó dùng `--fs-xs` trở lên, hoặc nó không thuộc về `Footer`.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'full' \| 'compact' \| 'auth'` | `'full'` | |
| `size` | input | `'sm' \| 'md'` | `'md'` | |
| `version` | input | `string \| null` | `null` | Chuỗi phiên bản đã định dạng sẵn. `null` thì ẩn hẳn phần này |
| `copyright` | input | `string \| null` | `null` | Đã dịch và đã ghép năm ở nơi gọi |
| `links` | input | `ReadonlyArray<FooterLink>` | `[]` | Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 |
| `showDivider` | input | `boolean` | `true` | Biến thể `auth` đặt `false` |

`Footer` là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5): nó **không** tự đọc phiên bản từ một service cấu hình. Component khung ở tầng nền tảng đọc phiên bản rồi truyền xuống. Nghe thừa một lớp, nhưng đó là thứ giữ cho `Footer` dựng được trong test chỉ bằng vài input, không cần mock cấu hình bản dựng.

`Footer` không có `output()` nào. Mọi thứ bấm được ở đây là điều hướng, và điều hướng đi bằng `<a>` chứ không bằng một sự kiện — cùng lý do đã ghi ở [`Button.md`](./Button.md).

## Do / Don't

- ✅ Dùng `<footer>` và kiểm rằng nó thật sự mang `contentinfo`.
- ✅ Để `Footer` cuộn cùng trang.
- ✅ Ghi số phiên bản — nó là thứ đầu tiên cần hỏi khi người dùng báo lỗi.
- ✅ Giữ danh sách liên kết ngắn; `Footer` không phải bản đồ trang.
- ❌ Không dính `Footer` vào đáy màn hình bằng `position: fixed`.
- ❌ Không cho `Footer` một nền bề mặt riêng — nó sẽ đọc như một `Card`.
- ❌ Không đặt hành động nghiệp vụ vào `Footer`, và không render một dải rỗng có vạch phân cách khi không có nội dung nào.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Chuỗi phiên bản hiển thị tới mức nào — chỉ số phiên bản, hay kèm mã commit và thời điểm dựng? Kèm mã commit rất hữu ích khi gỡ lỗi nhưng lộ thông tin nội bộ ra màn hình người dùng cuối | Chủ sản phẩm cùng người vận hành |
| 2 | `Footer` có hiện ở mọi trang không, hay ẩn ở màn có bảng chiếm toàn màn hình? Ẩn có điều kiện làm vị trí `Footer` khó đoán; hiện ở mọi trang thì trang bảng có thêm một khoảng cuộn không ai cần | Sau khi có màn danh sách thật |
| 3 | Nội dung `Footer` nên có một token riêng cho bề rộng tối đa không, hay dùng chung `--layout-container-max` với `main`? Dùng chung là mặc định hôm nay và chưa gặp phản ví dụ | Người dựng khung ứng dụng |
