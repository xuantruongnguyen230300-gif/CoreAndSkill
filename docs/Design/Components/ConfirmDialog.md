---
kind: luat
scope: core
verified: chua-doi-chieu
---

# ConfirmDialog

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** dựng **trên** [`Dialog.md`](./Dialog.md), không bọc thư viện riêng. Nó thừa hưởng toàn bộ bẫy focus, khoá cuộn nền, Escape từ `Dialog`; phần thêm vào chỉ là một bố cục cố định và một hợp đồng "đúng hai kết quả".

---

## Mục đích

Hỏi người dùng xác nhận một hành động trước khi thực hiện — đúng hai lựa chọn, không hơn.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Hành động **không hoàn tác được**: xoá, huỷ bỏ, đặt lại | 🛑 Hành động hoàn tác được dễ dàng → **đừng hỏi gì cả**, cứ làm rồi cho `Toast` kèm nút "Hoàn tác". Xem dưới |
| Hành động ảnh hưởng nhiều bản ghi cùng lúc | 🛑 Cần người dùng nhập thêm gì đó → [`Dialog.md`](./Dialog.md), vì đó là một form |
| Rời khỏi trang khi còn thay đổi chưa lưu | 🛑 Chỉ báo tin → [`Toast.md`](./Toast.md) hoặc [`NoticeBanner.md`](./NoticeBanner.md) |
| Hành động tốn kém hoặc gửi thông báo ra ngoài | 🛑 Hơn hai lựa chọn → dùng `Dialog` với nhóm nút riêng |

### Hỏi xác nhận là một cái thuế, và phần lớn nên bỏ

Mỗi hộp xác nhận là một cú bấm thêm cho **mọi** lần dùng, kể cả những lần người dùng hoàn toàn chắc chắn. Cái giá thật của nó không phải cú bấm — mà là **người dùng học cách bấm "Đồng ý" không đọc**. Hỏi quá nhiều thì đúng lúc câu hỏi thực sự quan trọng, nó cũng bị bấm qua.

Thứ tự ưu tiên khi thiết kế:

1. **Làm cho hoàn tác được** → làm luôn, cho `Toast` kèm nút "Hoàn tác". Đây là lựa chọn tốt nhất.
2. **Không hoàn tác được nhưng ít hậu quả** → làm luôn, không hỏi.
3. **Không hoàn tác được và có hậu quả** → mới dùng component này.

## Biến thể

| Biến thể | Icon | Màu nút xác nhận | Dùng khi |
| --- | --- | --- | --- |
| `ask` | `pi-question-circle`, màu `--color-info` | `primary` | Xác nhận thường: rời trang, gửi duyệt |
| `warning` | `pi-exclamation-triangle`, màu `--color-warning` | `primary` | Hậu quả đáng kể nhưng khắc phục được |
| `danger` | `pi-exclamation-triangle`, màu `--color-danger` | `danger` | **Phá huỷ, không hoàn tác được**: xoá vĩnh viễn |

Icon nằm trong một vòng tròn `--radius-full` nền là token `*-bg` tương ứng, đường kính bằng `--size-control-lg`.

**Chỉ biến thể `danger` được dùng nút `danger`.** Nút đỏ ở một xác nhận thường làm màu đỏ mất nghĩa đúng lúc nó cần có nghĩa nhất.

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Bề rộng | Luôn `sm` của [`Dialog.md`](./Dialog.md) (420px). **Không có biến thể rộng hơn** — một câu hỏi cần rộng hơn thế thì nó không phải một câu hỏi |
| Đệm | `--sp-6` |
| Khe icon → nội dung | `--sp-5` |
| Khe tiêu đề → mô tả | `--sp-3` |
| Khe giữa hai nút | `--sp-4` |
| Cỡ chữ tiêu đề | `--fs-lg`, `--fw-bold` |
| Cỡ chữ mô tả | `--fs-sm`, `--color-text-muted` |

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Icon vai + tiêu đề + mô tả + hai nút. Nút huỷ `secondary`, nút xác nhận theo biến thể | Có |
| `hover` | **Không áp dụng ở mức hộp.** Hover thuộc hai nút | — |
| `focus-visible` | **Không áp dụng ở mức hộp.** Focus bị bẫy bên trong — xem §Accessibility | — |
| `active` | **Không áp dụng.** Không phải control | — |
| `disabled` | Nút xác nhận vào `disabled` khi điều kiện xác nhận chưa đủ (ô "tôi hiểu" chưa tick). Nút huỷ **không bao giờ** bị vô hiệu hoá | Có |
| `loading` | Nút xác nhận vào `loading`; nút huỷ vào `disabled`; Escape **không** đóng; bấm ra ngoài **không** đóng. `aria-busy="true"` | Có |
| `error` | [`NoticeBanner.md`](./NoticeBanner.md) vai `danger` chèn giữa mô tả và nhóm nút; **hộp không tự đóng**; nút xác nhận trở lại bấm được để thử lại | Có |
| `empty` | **Không áp dụng.** Một câu hỏi luôn có nội dung | — |

**Vì sao lỗi giữ hộp mở:** đóng hộp rồi báo lỗi qua `Toast` khiến người dùng mất bối cảnh — họ phải nhớ lại mình vừa định xoá cái gì và mở lại từ đầu. Giữ hộp mở là giữ nguyên ngữ cảnh cho lần thử thứ hai.

**Vì sao nút huỷ không bao giờ bị vô hiệu hoá:** một hộp thoại không thoát ra được là một cái bẫy. Kể cả khi đang `loading` thì Escape bị chặn còn nút huỷ vẫn phải nhìn thấy — chỉ là nó không bấm được cho tới khi request xong, và đó là lý do nó ở trạng thái `disabled` chứ không bị ẩn.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-text`, `--color-text-muted`, `--color-border`, `--color-info`, `--color-info-bg`, `--color-warning`, `--color-warning-bg`, `--color-danger`, `--color-danger-bg`, `--color-overlay` |
| Chữ | `--fs-lg`, `--fs-sm`, `--fw-bold`, `--fw-regular`, `--lh-tight`, `--lh-normal` |
| Khoảng cách | `--sp-3`, `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-lg`, `--radius-full`, `--border-w` |
| Kích thước | `--size-control-lg`, `--icon-lg` |
| Bóng | `--shadow-4` |
| Lớp | `--z-dialog`, `--z-backdrop` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-sm` | Icon bên trái, nội dung bên phải; hai nút xếp ngang, ghim mép phải |
| < `--bp-sm` | Icon lên trên, căn giữa; hai nút xếp dọc, **nút xác nhận lên trên**; cả hai nút `block` |

Thứ tự trong DOM giữ nguyên (huỷ trước, xác nhận sau) và đảo bằng `order` của flex ở màn nhỏ — cùng cách xử lý với [`Button.md`](./Button.md).

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | `role="alertdialog"` — **không** phải `role="dialog"`. Khác biệt có thật: `alertdialog` báo cho trình đọc màn hình rằng đây là chuyện cần chú ý ngay, và nó đọc luôn phần mô tả khi mở |
| Nhãn | `aria-labelledby` trỏ tiêu đề, `aria-describedby` trỏ mô tả. **Cả hai bắt buộc** với `alertdialog` |
| **Focus lúc mở** | Đặt vào **nút huỷ**, không phải nút xác nhận. Đây là điểm khác `Dialog` và là điểm quan trọng nhất của component này |
| Bẫy focus | Chỉ hai nút và nút đóng nằm trong vòng Tab |
| Escape | Đóng = **huỷ**, không phải xác nhận |
| Bấm ra ngoài | Đóng = huỷ. Chặn khi đang `loading` |
| Icon | `aria-hidden="true"` — nó lặp lại thông tin đã có trong tiêu đề ([`../Icons.md`](../Icons.md) §7) |
| Nhãn nút | Nhãn nói **hành động cụ thể**: "Xoá người dùng", không phải "Đồng ý". Xem dưới |
| Chữ | Qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Vì sao focus vào nút huỷ:** người dùng bàn phím thường gõ Enter theo phản xạ. Nếu focus nằm ở nút xác nhận, một phím Enter là xoá mất bản ghi. Focus ở nút huỷ làm phản xạ đó trở thành vô hại — muốn xác nhận thì phải Tab một lần, và một lần Tab là đủ để dừng lại nghĩ.

**Vì sao nhãn nút phải nói hành động:** người dùng trình đọc màn hình nghe từng phần tử một. Nghe thấy "nút, Đồng ý" thì không biết đang đồng ý cái gì — họ phải quay lại đọc tiêu đề. "Nút, Xoá người dùng" thì tự đủ nghĩa. Điều này cũng đúng với người dùng mắt thường đang vội.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `open` | input | `boolean` | `false` | |
| `severity` | input | `'ask' \| 'warning' \| 'danger'` | `'ask'` | Mặc định là mức nhẹ nhất — sai theo hướng an toàn |
| `title` | input | `string` | — | Bắt buộc |
| `message` | input | `string` | — | Bắt buộc. `alertdialog` cần cả nhãn lẫn mô tả |
| `confirmLabel` | input | `string` | — | Bắt buộc. **Không có mặc định "Đồng ý"** — bắt nơi gọi phải nghĩ ra một nhãn nói được hành động |
| `cancelLabel` | input | `string \| null` | `null` | `null` thì dùng nhãn huỷ chung từ i18n |
| `requireAcknowledge` | input | `string \| null` | `null` | Chuỗi là nhãn của một ô đánh dấu bắt buộc tick trước khi xác nhận bật lên. Chỉ dùng cho ca thật sự nặng |
| `loading` | input | `boolean` | `false` | |
| `error` | input | `string \| null` | `null` | |
| `confirmed` | output | `void` | — | Hộp **không tự đóng** sau khi phát. Nơi gọi đóng sau khi thao tác xong |
| `cancelled` | output | `void` | — | Phát cho cả nút huỷ, Escape và bấm ra ngoài — một đường ra duy nhất |

**`confirmed` không tự đóng hộp** vì thao tác có thể thất bại, và lúc đó hộp phải còn đó để hiện lỗi. Nếu hộp tự đóng thì nơi gọi phải mở lại — và trạng thái sẽ nhấp nháy.

**`cancelled` gộp cả ba đường thoát** — nút, Escape, bấm ra ngoài. Ba sự kiện riêng nghĩa là mỗi nơi gọi phải nhớ nối cả ba, và sẽ có nơi quên một.

`ConfirmDialog` là component **dumb**: nó không thực hiện hành động, chỉ hỏi ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Trước khi thêm một hộp xác nhận, thử làm cho hành động hoàn tác được.
- ✅ Nhãn nút nói hành động cụ thể.
- ✅ Câu hỏi nói rõ **cái gì** bị ảnh hưởng và **bao nhiêu**: "Xoá 12 người dùng đã chọn?".
- ✅ Focus vào nút huỷ khi mở.
- ✅ Giữ hộp mở khi lỗi.
- ❌ Không dùng nhãn "Đồng ý" / "OK".
- ❌ Không dùng biến thể `danger` cho hành động hoàn tác được.
- ❌ Không vô hiệu hoá nút huỷ tới mức không thoát được.
- ❌ Không dùng `role="dialog"` — phải là `alertdialog`.
- ❌ Không hỏi xác nhận cho mọi thứ.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có làm kiểu "gõ tên bản ghi để xác nhận" cho ca nặng nhất không? Nó chặn được thao tác nhầm rất tốt nhưng gây khó chịu, và người dùng thành thạo sẽ ghét | Dự án đầu tiên có thao tác xoá hàng loạt |
| 2 | `requireAcknowledge` là ô đánh dấu trong hộp hay là một biến thể riêng? Hôm nay để là một input, có thể làm hộp phình | Người dựng component |
| 3 | Nút "Hoàn tác" trong `Toast` (con đường được khuyến khích hơn hỏi xác nhận) cần hạ tầng gì phía máy chủ? Đây là câu hỏi cho phía backend chứ không phải Design, nhưng nó quyết định lựa chọn số 1 ở §Khi nào dùng có khả thi không | Người thiết kế tầng dữ liệu |
