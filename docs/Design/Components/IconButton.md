---
kind: luat
scope: core
verified: chua-doi-chieu
---

# IconButton

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Lý do ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — nó là một `<button>` vuông chứa một icon, không có hành vi nào thuộc nhóm "khó". Bọc thư viện ở đây chỉ đổi lấy một tập CSS mặc định phải đè.

---

## Mục đích

Kích hoạt một hành động bằng đúng một icon, kèm nhãn chữ mà chỉ trình đọc màn hình nghe được.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Hành động lặp ở **mỗi hàng bảng**: sửa, xoá, xem chi tiết — nơi nhãn chữ nhân lên hàng chục lần sẽ nuốt mất bề rộng cột | 🛑 Hành động không có icon quy ước (Duyệt, Bàn giao, Kết chuyển) → [`Button.md`](./Button.md). Xem [`../Icons.md`](../Icons.md) §2 bước 3 |
| Hành động phụ trong `Toolbar`, `Topbar`, đầu `Dialog` — nơi chỗ trống có hạn | 🛑 Hành động chính của một màn → [`Button.md`](./Button.md) biến thể `primary`, có chữ |
| Nút đóng của một lớp nổi | 🛑 Xác nhận cuối cùng của thao tác phá huỷ — [`../Icons.md`](../Icons.md) §2 cấm: nút xoá cuối cùng luôn phải có chữ |
| Nút mở/đóng menu ở màn nhỏ | 🛑 Bật/tắt một tuỳ chọn → [`Check.md`](./Check.md); chuyển giữa vài chế độ xem → [`SegmentedControl.md`](./SegmentedControl.md) |

**Bẫy đã biết — icon font vẽ chậm.** [`../Icons.md`](../Icons.md) §1 nói rõ: PrimeIcons là icon font, và trước khi font về thì nút có thể là một ô vuông trống. Với `IconButton` thì ô vuông trống đó là **toàn bộ** nội dung nút. Vì vậy `IconButton` không được là con đường duy nhất tới một hành động quan trọng — hành động đó phải còn một lối vào khác.

## Biến thể

| Biến thể | Vai | Nền | Icon | Viền |
| --- | --- | --- | --- | --- |
| `ghost` | **Mặc định.** Hành động trong hàng bảng, trong `Toolbar` — nơi hàng chục cái viền cạnh nhau sẽ thành lưới nhiễu | trong suốt | `--color-text-muted` | không |
| `secondary` | Nút đứng một mình, cần thấy được ranh giới | `--color-surface` | `--color-text` | `--color-border` |
| `primary` | Hành động chính không đủ chỗ cho chữ — hiếm, cân nhắc kỹ trước khi dùng | `--color-brand` | `--color-text-on-brand` | trong suốt |
| `danger` | Chỉ hành động phá huỷ **có bước xác nhận sau đó** | trong suốt | `--color-danger` | không |

**Vì sao mặc định là `ghost` chứ không `secondary`:** chỗ dùng đông nhất của component này là cột hành động cuối bảng. Mỗi hàng ba nút, hai mươi hàng là một bức tường ô vuông có viền — mắt đọc nó thành một cái lưới chứ không thành ba hành động. Sai theo hướng nhẹ nhàng là sai an toàn: cần viền thì khai thêm một chữ, còn quên khai thì bảng vẫn đọc được.

**`danger` là icon đổi màu, không phải nền đỏ đặc.** Nền đỏ lặp lại ở mỗi hàng biến cả cột thành báo động và người dùng ngừng thấy nó. Chỉ icon đổi sang `--color-danger` — vẫn đạt ngưỡng 3:1 của icon mang nghĩa theo [`../Icons.md`](../Icons.md) §4, vẫn phân biệt được với hai nút bên cạnh.

## Kích thước

Nút **vuông**: bề rộng bằng chiều cao. Đệm là thứ tạo ra vùng bấm, không phải cỡ icon.

| Cỡ | Cạnh | Icon | Đệm | Dùng khi |
| --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` (28px) | `--icon-sm` | `--sp-3` | Trong hàng bảng, trong chip lọc |
| `md` | `--size-control-md` (34px) | `--icon-md` | `--sp-4` | **Mặc định.** `Toolbar`, `Topbar`, đầu `Dialog` |
| `lg` | `--size-control-lg` (42px) | `--icon-lg` | `--sp-5` | Nút hamburger ở màn nhỏ, nơi ngón tay là thiết bị trỏ |

🛑 **Vùng bấm ≥ 28×28px kể cả khi icon chỉ 14px, và nới bằng `padding`.** `margin` không nhận sự kiện chuột, nên nới bằng `margin` tạo ra một nút trông đủ to mà bấm trượt. Luật ở [`../COMPONENTS.md`](../COMPONENTS.md) §2.2 và [`../Icons.md`](../Icons.md) §7.

Bo góc `--radius-sm` cho mọi cỡ. Không dùng `--radius-full` — nút tròn dễ bị đọc nhầm thành [`Avatar.md`](./Avatar.md) hoặc thành chấm trạng thái.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Theo biến thể; `--radius-sm`; icon căn giữa bằng `display: inline-flex; align-items: center; justify-content: center` | Có |
| `hover` | `ghost` và `danger` → nền `--color-surface-2`, icon đậm lên (`--color-text`, riêng `danger` giữ `--color-danger`). `secondary` → viền `--color-border-strong`. `primary` → nền `--color-brand-hover`. Chuyển tiếp `--dur-fast` với `--ease-standard`. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px`. Chỉ `:focus-visible`. Khoảng hở 2px giữ vòng focus không tàng hình trên biến thể `primary` — [`../DESIGN.md`](../DESIGN.md) §2.6 | Có |
| `active` | Nền đậm hơn `hover` một bậc (`--color-surface-3`, hoặc `--color-brand-active` cho `primary`); `transform: translateY(1px)` | Có |
| `disabled` | Icon `--color-text-disabled`; `cursor: not-allowed`; bỏ mọi hiệu ứng hover/active. Thuộc tính `disabled` thật, không chỉ đổi màu | Có |
| `loading` | Icon đổi thành `pi-spinner` quay; nút bị vô hiệu hoá; `aria-busy="true"`. **Cạnh nút không đổi** — nút vốn vuông theo token cỡ, nên hàng bảng không nhảy chỗ | Có |
| `error` | **Không áp dụng.** Nút không tự mang lỗi. Kết quả hỏng của hành động báo qua `Toast` hoặc [`NoticeBanner.md`](./NoticeBanner.md) | — |
| `empty` | **Không áp dụng.** Không có icon nghĩa là chưa khai `icon` — lỗi thi công, không phải một trạng thái để thiết kế | — |

**Vì sao `loading` vẫn giữ vòng quay dù nút rất bé:** với nút chữ thì [`Button.md`](./Button.md) còn nhãn để người dùng bám vào; ở đây icon là toàn bộ tín hiệu. Bỏ vòng quay thì một nút đang chờ máy chủ trông y hệt nút chưa được bấm, và người dùng bấm lần hai. Cái giá phải trả: khi `prefers-reduced-motion: reduce` bật, vòng quay dừng và thay bằng chỉ báo tĩnh ([`../Icons.md`](../Icons.md) §7), lúc đó chỉ còn trạng thái vô hiệu hoá làm tín hiệu — chấp nhận được, vì nút vẫn không bấm lại được.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-brand`, `--color-brand-hover`, `--color-brand-active`, `--color-danger`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-text-on-brand`, `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-border`, `--color-border-strong`, `--color-focus` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-sm`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-sm`, `--icon-md`, `--icon-lg` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-lg` | Nhóm nút trong hàng bảng hiện đủ, xếp ngang, khe `--sp-2` |
| `--bp-md` … `--bp-lg` | Giữ nguyên; nút trong `Toolbar` dùng cỡ `md` |
| < `--bp-md` | Cỡ tối thiểu nâng lên `md` ở mọi vị trí — ngón tay không nhắm được 28px một cách thoải mái dù WCAG cho qua |
| < `--bp-xs` | Cột hành động của bảng gom về một nút mở menu, thay vì ba nút cạnh nhau |

**Ba nút 28px cạnh nhau trên điện thoại là ba lần bấm nhầm.** 28×28px qua được SC 2.5.8 mức AA nhưng đó là **ngưỡng tối thiểu**, không phải mục tiêu. Cái giá của việc gom về menu: thêm một lần chạm cho mọi hành động ở màn nhỏ — chấp nhận được, vì bấm nhầm "xoá" đắt hơn nhiều.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<button>` thật. 🛑 Không `<i role="button">`, không `<div>` — mất `Enter`/`Space`, mất `disabled` |
| `type` | Luôn đặt tường minh. Một `IconButton` xoá dòng nằm trong form mà quên `type="button"` sẽ gửi cả form |
| Vai trò ARIA | Vai trò mặc định `button`. Nút bật/tắt một trạng thái (ghim cột, hiện/ẩn mật khẩu) thêm `aria-pressed` |
| Nhãn | 🛑 **`aria-label` bắt buộc, không có ngoại lệ.** Nhãn nói **hành động và đối tượng**: "Xoá người dùng", không phải "Xoá" — [`../Icons.md`](../Icons.md) §7 |
| Icon | `aria-hidden="true"` trên thẻ icon. Thiếu nó, trình đọc màn hình đọc ký tự Private Use Area thành tạp âm trước mỗi nút |
| Bàn phím | `Enter` và `Space` kích hoạt — miễn phí nếu dùng `<button>` |
| Focus | Nhìn thấy ở `:focus-visible`, `outline-offset` ≥ 2px |
| Vùng bấm | ≥ 28×28px, nới bằng `padding` (WCAG 2.2 SC 2.5.8) |
| Nhiều nút cùng nhãn | Trong bảng, mỗi hàng phải có nhãn phân biệt được — ghép định danh hàng vào nhãn, hoặc `aria-describedby` trỏ vào ô tên. Hai mươi nút cùng đọc là "Sửa" khiến danh sách nút của trình đọc màn hình vô dụng |
| Tooltip | Tooltip **không thay được** `aria-label`: nhiều hiện thực không hiện nó với bàn phím, và nó biến mất khi chạm |
| Chữ | Nhãn đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `icon` | input | `string` | — | **Bắt buộc.** Tên icon PrimeIcons, không kèm tiền tố `pi ` |
| `ariaLabel` | input | `string` | — | **Bắt buộc.** Không có mặc định, và không suy ra từ `icon` — xem ghi chú dưới |
| `variant` | input | `'ghost' \| 'secondary' \| 'primary' \| 'danger'` | `'ghost'` | Mặc định nhẹ nhất, sai theo hướng an toàn |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `type` | input | `'button' \| 'submit' \| 'reset'` | `'button'` | |
| `disabled` | input | `boolean` | `false` | |
| `loading` | input | `boolean` | `false` | Khi `true` thì nút tự vô hiệu hoá, không cần đặt `disabled` kèm |
| `pressed` | input | `boolean \| null` | `null` | `null` = không phải nút bật/tắt, không sinh `aria-pressed` |
| `clicked` | output | `void` | — | **Không phát khi `disabled` hoặc `loading`.** Chặn ở component, không bắt mỗi nơi gọi tự nhớ |

**Vì sao `ariaLabel` không suy ra từ `icon`:** một bảng tra "pencil → Sửa" nghe tiện và hỏng ngay ở ca thật — cùng `pi-pencil` là "Sửa người dùng" ở màn này và "Sửa ghi chú" ở màn kia. Bảng tra sẽ sinh ra hàng loạt nút cùng đọc là "Sửa", đúng thứ mục Accessibility ở trên cấm. Bắt khai tường minh là chỗ duy nhất ép được người dựng nghĩ về đối tượng.

`IconButton` là component **dumb** — không inject service lấy dữ liệu, không biết route ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Luôn khai `ariaLabel`, và khai kèm đối tượng.
- ✅ Dùng đúng icon trong bảng [`../Icons.md`](../Icons.md) §5 cho hành động đã có ở đó.
- ✅ Giữ `ghost` làm mặc định trong bảng; chỉ dùng `secondary` khi nút đứng một mình.
- ✅ Nới vùng bấm bằng `padding`.
- ❌ Không dùng `IconButton` cho hành động không có icon quy ước.
- ❌ Không để `IconButton` là lối vào duy nhất tới một hành động quan trọng.
- ❌ Không dựa vào tooltip làm nhãn.
- ❌ Không đặt `outline: none` mà không thay bằng dấu hiệu focus khác.
- ❌ Không dùng nền đỏ đặc cho `danger` trong hàng bảng.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Nút bật/tắt (`pressed`) có cần biến thể hình thức riêng không, hay chỉ đổi nền sang `--color-brand-subtle`? Nền đó chênh `--color-surface` 1.25:1 nên không được là tín hiệu duy nhất, sẽ phải kèm icon đổi sang `--color-brand-on-subtle` | Người dựng component, cùng màn đầu tiên cần ghim cột |
| 2 | Ở màn < `--bp-xs`, menu gom hành động là component nào? Nó có overlay nên thuộc nhóm "khó" ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 và chưa có dòng nào trong mục lục §3 | Dự án đầu tiên có bảng chạy trên điện thoại |
| 3 | Có cần token riêng cho khe giữa các `IconButton` trong một cột hành động không? Hôm nay mượn `--sp-2`, chưa được kiểm trên bảng thật | Sau khi có màn danh sách thật |
