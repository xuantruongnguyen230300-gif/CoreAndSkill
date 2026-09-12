---
kind: luat
scope: core
verified: chua-doi-chieu
---

# ProgressBar

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Theo [`../COMPONENTS.md`](../COMPONENTS.md) §4 đây là component thuần trình bày — một cái máng và một phần lấp đầy. Không có hành vi nào thuộc nhóm "khó"; bọc thư viện chỉ đổi lấy một tập CSS mặc định phải đè.

---

## Mục đích

Cho biết một việc đang chạy đã đi được bao xa, hoặc chỉ đơn giản là nó vẫn đang chạy.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Việc chạy **quá vài giây** và đếm được tiến độ: tải tệp lên, nhập dữ liệu từ tệp, xuất báo cáo lớn | 🛑 Chờ một request thường vài trăm mili-giây → [`SkeletonLoader.md`](./SkeletonLoader.md), hoặc trạng thái `loading` của chính control đã gây ra nó |
| Tiến trình tải lên trong [`FileUpload.md`](./FileUpload.md) | 🛑 Một nút đang chờ kết quả → trạng thái `loading` của [`Button.md`](./Button.md); vòng quay nằm trong nút, không phải một thanh riêng |
| So sánh một giá trị với một mốc: đã chi so với dự toán, đã dùng so với hạn mức | 🛑 Đánh giá định tính không có mốc (hài lòng, mức độ rủi ro) → [`Badge.md`](./Badge.md). Thanh ngụ ý *"bao nhiêu phần trăm của một tổng"*, dùng sai là nói dối về bản chất con số |
| Chỉ báo bước trong một việc nhiều bước ở màn nhỏ | 🛑 Việc nhiều bước ở màn rộng → [`Stepper.md`](./Stepper.md), nó nói được tên từng bước |

## Biến thể

| Biến thể | Vai | Dùng khi |
| --- | --- | --- |
| `determinate` | Biết đã đi được bao nhiêu phần trăm | **Mặc định.** Bất cứ khi nào máy chủ trả về được số đã xử lý trên tổng |
| `indeterminate` | Chỉ biết là đang chạy | Khi **không** biết tổng. Một dải ngắn chạy qua lại trong máng |
| `meter` | Không phải tiến trình mà là **một giá trị so với một mốc** | Thực hiện so với dự toán, dung lượng đã dùng so với hạn mức |

🛑 **`indeterminate` không được giả vờ biết phần trăm.** Đặt một con số ước chừng vào đó là nói dối, và người dùng sẽ lập kế hoạch theo con số đó. Không biết thì nói không biết.

**`meter` khác hai biến thể kia ở một điểm quyết định:** nó **không tự đổi theo thời gian**. Nó là ảnh chụp một trạng thái, không phải một việc đang chạy — nên nó không có `aria-busy`, không có hoạt ảnh, và nó tồn tại cả khi không có việc gì đang diễn ra. Trộn hai vai làm một là lý do nhiều hệ có một component vừa báo tải vừa báo hạn mức và không cái nào đúng.

Ba mức của `meter`, quyết định bằng **ngưỡng do trang truyền xuống**, không phải bằng con số cố định trong component:

| Mức | Màu phần lấp | Nghĩa |
| --- | --- | --- |
| bình thường | `--color-brand` | Còn trong mốc |
| cảnh báo | `--color-warning` | Gần chạm mốc |
| vượt | `--color-danger` | Đã vượt mốc — phần lấp dừng ở 100%, và vạch mốc lùi vào trong |

## Kích thước

`ProgressBar` **không** dùng thang `--size-control-*` — nó không phải control, không ai bấm vào nó.

| Cỡ | Chiều cao máng | Dùng khi |
| --- | --- | --- |
| `sm` | 4px | Dưới một hàng trong danh sách tệp; chỉ báo bước ở màn nhỏ |
| `md` | 8px | **Mặc định.** Trong [`Card.md`](./Card.md), trong [`Dialog.md`](./Dialog.md), dạng `meter` |
| `lg` | 14px | Chỉ khi thanh là nội dung chính của cả vùng — màn nhập dữ liệu đang chạy |

Bo góc `--radius-pill` cho cả máng lẫn phần lấp. Máng dùng `--color-surface-3`; riêng dạng `meter` dùng **một bậc nhạt của chính hệ màu phần lấp** (`--chart-seq-1` cho mức bình thường) chứ không dùng xám trung tính — nhờ vậy trạng thái đọc được trên toàn bộ chiều dài thanh, kể cả phần chưa chạy tới.

Bề rộng do vùng chứa quyết định; `ProgressBar` không tự đặt `width`.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Máng `--color-surface-3` (hoặc `--chart-seq-1` với `meter`), phần lấp `--color-brand`, cả hai `--radius-pill`. Chuyển tiếp bề rộng `--dur-base` với `--ease-standard` | Có |
| `hover` | **Không áp dụng.** Thanh không tương tác. Con số đi kèm phải luôn nhìn thấy được, không giấu sau thao tác rê chuột | — |
| `focus-visible` | **Không áp dụng.** Thanh không nhận focus. Nút Huỷ đi kèm một việc đang chạy thì nhận focus, và nó là một [`Button.md`](./Button.md) riêng | — |
| `active` | **Không áp dụng.** Không có gì để nhấn | — |
| `disabled` | **Không áp dụng.** Một việc đã dừng thì thuộc về `error` hoặc về trạng thái xong; "thanh tiến trình bị vô hiệu hoá" không có nghĩa nào | — |
| `loading` | **Chính là biến thể `indeterminate`.** Dải chạy qua lại, `aria-busy="true"` trên vùng nội dung mà nó đang chờ. Khai `prefers-reduced-motion: reduce` thì **dừng hoạt ảnh** và thay bằng một dải tĩnh phủ toàn máng ở độ mờ thấp | Có |
| `error` | Phần lấp đổi sang `--color-danger` và **dừng ở đúng chỗ đã tới** — không lùi về 0, không chạy tiếp tới 100%. Kèm một dòng chữ nói đã xử lý được bao nhiêu trước khi hỏng. 🛑 Màu không đủ: phải có chữ | Có |
| `empty` | Tiến độ bằng 0: máng hiện đầy đủ, phần lấp không vẽ. **Không ẩn cả thanh đi** — thanh biến mất rồi hiện lại làm bố cục nhảy đúng lúc người dùng đang nhìn | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface-3`, `--color-brand`, `--color-warning`, `--color-danger`, `--color-text`, `--color-text-muted`, `--chart-seq-1` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fw-semibold` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4` |
| Hình dạng | `--radius-pill`, `--radius-xs`, `--border-w-strong` |
| Chuyển động | `--dur-base`, `--dur-slow`, `--ease-standard` |

Chiều cao máng (4 / 8 / 14px) là giá trị riêng của component này, khai trong `:root` dưới tên `--progress-h-sm|md|lg` theo tầng token thứ ba ở [`../DESIGN.md`](../DESIGN.md) §1.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Nhãn và con số nằm **cùng hàng** với nhau, phía trên thanh: nhãn bên trái, giá trị bên phải |
| `--bp-sm` … `--bp-md` | Giữ nguyên. Nhãn dài cắt bằng dấu ba chấm, con số không bao giờ bị cắt |
| < `--bp-sm` | Nhãn và con số **xuống hai dòng** nếu cộng lại vượt bề rộng. Cỡ thanh nâng lên `md` nếu đang là `sm` |

Con số luôn được ưu tiên giữ nguyên vẹn khi hết chỗ. Một nhãn bị cắt vẫn đoán được; một con số bị cắt thì sai.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | `role="progressbar"` cho `determinate` và `indeterminate`; `role="meter"` cho biến thể `meter` |
| Giá trị | `aria-valuenow`, `aria-valuemin`, `aria-valuemax`. Biến thể `indeterminate` **bỏ hẳn `aria-valuenow`** — có mặt với giá trị bịa còn tệ hơn không có |
| Nhãn | `aria-label` hoặc `aria-labelledby` nói **việc gì** đang chạy: "Đang nhập người dùng từ tệp", không phải "Tiến trình" |
| Thông báo tiến độ | Báo qua vùng `aria-live="polite"` riêng, theo **mốc 10%**, không phải mỗi phần trăm. Báo mọi thay đổi là biến trình đọc màn hình thành tiếng ồn liên tục |
| Con số nhìn thấy được | Bắt buộc với `determinate` và `meter`. Thanh một mình không đọc được giá trị chính xác, và người dùng cần con số để đối chiếu |
| Màu | Ba mức của `meter` phải kèm chữ hoặc icon. Màu là kênh thứ hai, không phải kênh duy nhất — [`../DESIGN.md`](../DESIGN.md) §2.7 |
| Chuyển động | `indeterminate` dừng chạy khi `prefers-reduced-motion: reduce` — [`../DESIGN.md`](../DESIGN.md) §7 |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'determinate' \| 'indeterminate' \| 'meter'` | `'determinate'` | |
| `value` | input | `number \| null` | `null` | `null` với `indeterminate`. Với hai biến thể kia, `null` là lỗi thi công chứ không phải một trạng thái |
| `max` | input | `number` | `100` | Với `meter` đây là **mốc**, không phải trần — giá trị vượt mốc là hợp lệ và phải vẽ được |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `label` | input | `string` | — | **Bắt buộc.** Không có mặc định; một thanh không nhãn không nói được nó đang đo cái gì |
| `showValue` | input | `boolean` | `true` | Mặc định **hiện** con số. Sai theo hướng an toàn: quên khai thì vẫn đọc được giá trị |
| `warnAt` | input | `number \| null` | `null` | Ngưỡng đổi sang mức cảnh báo, tính theo phần trăm của `max`. Chỉ có nghĩa với `meter` |
| `state` | input | `'normal' \| 'error'` | `'normal'` | |
| `errorText` | input | `string \| null` | `null` | Bắt buộc khác `null` khi `state` là `'error'` |

Không có output. Tiến độ do trang cha đẩy xuống; component không tự hỏi máy chủ ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Luôn hiện con số bên cạnh thanh ở `determinate` và `meter`.
- ✅ Dùng `indeterminate` khi thật sự không biết tổng, thay vì bịa một con số.
- ✅ Với `meter`, vẽ **vạch mốc** thật khi giá trị vượt mốc — "107%" mà không có vạch thì không thấy được nó vượt bao nhiêu.
- ✅ Để việc chạy lâu chạy nền và báo bằng `Toast` khi xong.
- ❌ Không khoá cả màn hình bằng một hộp thoại "Đang xử lý…" cho việc chạy vài phút.
- ❌ Không cho thanh lùi lại. Tiến độ giảm xuống làm người dùng mất tin vào con số.
- ❌ Không ẩn thanh khi giá trị bằng 0.
- ❌ Không dùng `meter` cho thứ không có mốc.
- ❌ Không để hoạt ảnh `indeterminate` chạy khi người dùng đã khai `prefers-reduced-motion`.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Việc chạy nền được báo ở đâu khi người dùng đã rời màn? Hôm nay chưa có chỗ nào trong khung ứng dụng dành cho "các việc đang chạy" — [`Topbar.md`](./Topbar.md) chưa khai vùng đó. Không có nó thì mọi việc chạy lâu bị trói vào đúng một màn | Khi có màn nhập dữ liệu thật |
| 2 | Ngưỡng cảnh báo của `meter` mặc định là bao nhiêu, hay luôn bắt trang truyền? Luôn bắt truyền thì rõ ràng nhưng lặp ở mọi nơi dùng | Dự án đầu tiên có màn dự toán |
| 3 | Có cần dạng vòng tròn không? Nó tiết kiệm chỗ trong ô bảng, nhưng thêm một hình thức nữa cho cùng một dữ liệu và khó gắn nhãn số | Màn đầu tiên cần tiến độ trong ô bảng |
