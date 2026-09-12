---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Tooltip

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **bọc PrimeNG**. Cùng lý do với [`Menu.md`](./Menu.md): định vị một lớp nổi khi trang cuộn hoặc khi nó chạm mép màn hình thuộc nhóm "khó" ở [`../COMPONENTS.md`](../COMPONENTS.md) §4. Phần Core tự viết là luật **khi nào được hiện** và **hiện cái gì** — hai thứ thư viện không quyết hộ được.

---

## Mục đích

Hiện một dòng chú ngắn cho một phần tử, khi và chỉ khi người dùng tỏ ý muốn biết thêm.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Hiện nhãn chữ của một mục nav khi [`Sidebar.md`](./Sidebar.md) đang thu gọn còn dải icon | 🛑 **Làm nhãn cho một [`IconButton.md`](./IconButton.md).** Nhãn là việc của `aria-label` — xem ràng buộc cứng dưới đây |
| Nói vì sao một control đang bị khoá: "Cần quyền duyệt chứng từ" | 🛑 Thông tin người dùng **cần** để hoàn thành việc → dòng gợi ý của [`FormRow.md`](./FormRow.md), luôn nhìn thấy được |
| Hiện giá trị đầy đủ của một ô bảng đã bị cắt bằng dấu ba chấm | 🛑 Nội dung dài hơn hai dòng → [`Dialog.md`](./Dialog.md) hoặc một dòng mô tả tại chỗ |
| Giải thích một icon trạng thái trong ô bảng | 🛑 Bất cứ thứ gì **bấm được** bên trong — xem Do / Don't |

### Ràng buộc cứng — tooltip không bao giờ là nhãn

🛑 **Tooltip KHÔNG thay được `aria-label`.** Ba lý do, mỗi lý do đủ để một mình quyết định:

1. Nhiều hiện thực không hiện tooltip khi phần tử nhận focus bằng bàn phím — người dùng bàn phím không bao giờ thấy nó.
2. Nó biến mất ngay khi chạm, nên trên thiết bị cảm ứng nó gần như không tồn tại.
3. Trình đọc màn hình đọc nhãn, không đọc lớp nổi.

Tooltip là thứ **bổ sung** cho một phần tử đã có nhãn. [`IconButton.md`](./IconButton.md) ghi đúng dòng này trong mục Accessibility của nó, và đây là lời giải thích đầy đủ cho dòng đó.

## Biến thể

| Biến thể | Nội dung | Dùng khi |
| --- | --- | --- |
| `label` | Đúng một cụm từ, không dấu chấm câu | **Mặc định.** Nhãn của icon, tên mục nav thu gọn, giá trị ô bị cắt |
| `hint` | Một câu đầy đủ, tối đa hai dòng | Giải thích vì sao một control bị khoá, hoặc một quy tắc ngắn |

Chỉ hai biến thể, và ranh giới giữa chúng là **độ dài**. Tooltip quá hai dòng là dấu hiệu nội dung đó thuộc về trang chứ không thuộc về một lớp nổi biến mất sau một giây.

🛑 **Không có biến thể mang màu trạng thái.** Một tooltip đỏ trông như một thông báo lỗi, trong khi nó chỉ là lời chú. Lỗi thuộc về [`NoticeBanner.md`](./NoticeBanner.md), [`Toast.md`](./Toast.md) và dòng lỗi của `FormRow`.

## Kích thước

`Tooltip` **không** dùng thang `--size-control-*` — nó không phải control, chiều cao sinh ra từ cỡ chữ và đệm.

| Khoản | Giá trị |
| --- | --- |
| Cỡ chữ | `--fs-xs` |
| Cân nặng | `--fw-medium` |
| Đệm | `--sp-2` dọc / `--sp-4` ngang |
| Bo góc | `--radius-xs` |
| Bề rộng tối đa | 280px — quá ngưỡng này thì xuống dòng |
| Khe tới phần tử neo | `--sp-3` |
| Đổ bóng, lớp | `--shadow-2`, `--z-popover` |

Một cỡ duy nhất, có chủ đích. Tooltip luôn là chữ phụ đứng cạnh thứ khác; cho nó nhiều cỡ là mời người ta dùng nó làm tiêu đề.

Mũi nhọn chỉ về phần tử neo là **bắt buộc**. Không có nó, một tooltip nằm giữa hai nút cạnh nhau không nói được nó đang chú cho nút nào.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-inverse-surface`, chữ `--color-inverse-text` ([`../DESIGN.md`](../DESIGN.md) §2.6). Không viền — nền đảo đã tách khỏi bề mặt ở mức 14.68:1 sáng / 14.64:1 tối | Có |
| `hover` | **Đây chính là cơ chế hiện**, không phải một hình thức riêng. Hiện sau `--dur-slow` rê chuột; ẩn ngay khi con trỏ rời. Bọc trong `@media (hover: hover)` | Có |
| `focus-visible` | Hiện **ngay lập tức**, không có độ trễ, khi phần tử neo nhận focus bàn phím. Người dùng bàn phím đã chủ động đi tới đó — bắt họ chờ thêm là vô nghĩa | Có |
| `active` | **Không áp dụng.** Tooltip không bấm được; nó không có trạng thái đang-bị-nhấn | — |
| `disabled` | **Không áp dụng cho chính tooltip.** Nhưng phần tử neo bị khoá thì tooltip **vẫn phải hiện** — đó là ca dùng chính của biến thể `hint`. Một nút khoá dùng `aria-disabled` thay cho `disabled` để vẫn nhận được focus | — |
| `loading` | **Không áp dụng.** Nội dung tooltip là chữ tĩnh truyền vào lúc dựng. Cần tải mới có nội dung thì đó không phải tooltip mà là một popover, và Core chưa có component đó | — |
| `error` | **Không áp dụng.** Tooltip không mang lỗi. Xem ghi chú ở mục Biến thể | — |
| `empty` | Nội dung rỗng thì **không dựng gì cả** — không hiện một hộp rỗng. Đây là hành vi bắt buộc, vì chuỗi i18n thiếu khoá sẽ trả về rỗng và một hộp đen nhỏ nhảy ra giữa màn hình là lỗi rất khó truy | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-inverse-surface`, `--color-inverse-text` |
| Chữ | `--fs-xs`, `--fw-medium`, `--lh-snug` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4` |
| Hình dạng | `--radius-xs` |
| Bóng, lớp | `--shadow-2`, `--z-popover` |
| Chuyển động | `--dur-fast`, `--dur-slow`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Hiện bên trên phần tử neo; lật xuống dưới, sang trái hoặc sang phải khi không đủ chỗ |
| `--bp-sm` … `--bp-md` | Giữ nguyên. Bề rộng tối đa co xuống còn 220px |
| < `--bp-sm` | 🛑 **Tắt hẳn.** Xem dưới |

**Vì sao tắt hẳn ở màn nhỏ thay vì đổi cách hiện:** không có chuột thì không có "rê chuột", và mọi cách giả lập — chạm giữ, chạm lần một để xem chạm lần hai để bấm — đều tạo ra một nút hành xử khác với mọi nút còn lại. Cái giá phải trả rất cụ thể và phải chấp nhận công khai: **mọi thông tin chỉ có trong tooltip là thông tin người dùng di động không bao giờ thấy.** Đó chính là lý do luật ở mục Biến thể cấm đặt thông tin cần thiết vào đây.

Riêng ca `Sidebar` thu gọn thì không bị ảnh hưởng: dưới `--bp-md` sidebar đã chuyển sang dạng drawer và hiện nhãn đầy đủ.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | `role="tooltip"` trên hộp; phần tử neo trỏ tới nó bằng `aria-describedby` — **không** bằng `aria-labelledby` |
| Quan hệ với nhãn | Tooltip **mô tả thêm**, không thay nhãn. Phần tử neo phải tự có nhãn hợp lệ trước đã |
| Bàn phím | Hiện khi neo nhận focus, ẩn khi mất focus. `Escape` ẩn tooltip mà **không** làm mất focus khỏi phần tử neo |
| Nội dung tương tác | 🛑 Không bao giờ. Không liên kết, không nút. Chuột không đi tới đó được mà không rời vùng kích hoạt, nên nội dung bấm được trong tooltip là nội dung không bấm được |
| Con trỏ chạm qua | Tooltip không chặn sự kiện chuột (`pointer-events: none`), nếu không nó tự che mất chính phần tử vừa mở nó |
| Chuyển động | Hiện/ẩn bằng `opacity`, thời lượng `--dur-fast`. Khai `prefers-reduced-motion: reduce` thì bỏ hẳn chuyển tiếp |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8. Chuỗi dịch dài ra ở ngôn ngữ khác vẫn phải vừa hai dòng |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `text` | input | `string` | — | **Bắt buộc.** Rỗng thì không dựng gì — xem trạng thái `empty` |
| `variant` | input | `'label' \| 'hint'` | `'label'` | Mặc định là dạng ngắn nhất, sai theo hướng an toàn |
| `position` | input | `'top' \| 'bottom' \| 'left' \| 'right'` | `'top'` | Chỉ là **hướng ưu tiên**; component tự lật khi không đủ chỗ |
| `delay` | input | `number` | `400` | Mili-giây, chỉ áp cho chuột. Bàn phím luôn hiện ngay, không nhận giá trị này |
| `disabled` | input | `boolean` | `false` | Tắt tooltip mà không phải gỡ nó khỏi template |

Không có output. Tooltip không phát sự kiện nào ra ngoài — nó không phải một điểm tương tác.

`Tooltip` là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Coi tooltip là thứ **thêm vào**, không phải thứ mang thông tin duy nhất.
- ✅ Hiện ngay khi nhận focus bàn phím; chỉ chuột mới phải chờ.
- ✅ Giữ nội dung trong hai dòng.
- ✅ Luôn có mũi nhọn chỉ về phần tử neo.
- ❌ Không dùng tooltip làm nhãn cho `IconButton`.
- ❌ Không đặt liên kết, nút, hay bất cứ thứ gì bấm được vào trong.
- ❌ Không tô màu trạng thái cho tooltip.
- ❌ Không hiện tooltip cho phần tử mà nhãn của nó đã nói đúng điều đó — trình đọc màn hình sẽ đọc hai lần.
- ❌ Không đặt thông tin bắt buộc phải biết vào tooltip: người dùng di động sẽ không bao giờ thấy nó.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Ô bảng bị cắt có tự gắn tooltip không, hay phải khai từng cột? Tự động thì tiện nhưng phải đo bề rộng lúc chạy và đo lại mỗi lần đổi cỡ cửa sổ — một phép đo bố cục chạy trên mọi ô của một bảng lớn | Người dựng [`DataTable.md`](./DataTable.md) |
| 2 | `--dur-slow` (320ms) có đúng là độ trễ tốt không? Ngắn quá thì tooltip nhảy ra khi con trỏ chỉ đi ngang; dài quá thì người dùng bỏ cuộc trước khi nó kịp hiện | Sau khi có màn thật để thử |
| 3 | Có cần một component `Popover` riêng cho nội dung giàu và bấm được không? Hôm nay chưa có, và mọi nhu cầu kiểu đó đang bị đẩy sang `Dialog` — chấp nhận được nhưng nặng tay với những ca nhỏ | Màn đầu tiên gặp nhu cầu thật |
