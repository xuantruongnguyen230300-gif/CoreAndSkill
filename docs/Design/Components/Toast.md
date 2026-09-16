---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Toast

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **bọc PrimeNG**. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4, định vị lớp nổi khi cuộn và khi tràn viewport là ca "khó": phải xử lý container cuộn, `position: fixed` lồng nhau, và trở về đúng chỗ khi hết chỗ.

---

## Mục đích

Báo kết quả một thao tác **vừa xảy ra**, không chặn người dùng làm việc tiếp.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Xác nhận một thao tác đã thành công: "Đã lưu người dùng" | 🛑 Thông tin cần **ở lại** cho tới khi bối cảnh đổi → [`NoticeBanner.md`](./NoticeBanner.md). Toast biến mất; thứ quan trọng thì không được biến mất |
| Báo một thao tác nền đã xong | 🛑 Lỗi của một trường trong form → [`FormRow.md`](./FormRow.md). Lỗi phải nằm cạnh chỗ sai |
| Báo lỗi mà người dùng **không cần làm gì ngay** | 🛑 Lỗi buộc phải xử lý mới đi tiếp được → [`Dialog.md`](./Dialog.md) hoặc `NoticeBanner` |
| Kèm một hành động hoàn tác | 🛑 Nội dung dài hơn hai dòng → nó không đọc kịp trước khi biến mất |
| | 🛑 Nhiều thông báo cùng lúc từ một thao tác → gộp thành một |

**Phép thử một câu:** *nếu người dùng bỏ lỡ thông báo này, họ có mất gì không?* Có → không dùng `Toast`.

## Biến thể

| Biến thể | Icon | Màu icon | Thời gian tự tắt |
| --- | --- | --- | --- |
| `success` | `pi-check-circle` | `--color-success` | 4 giây |
| `info` | `pi-info-circle` | `--color-info` | 5 giây |
| `warning` | `pi-exclamation-triangle` | `--color-warning` | 8 giây |
| `danger` | `pi-times-circle` | `--color-danger` | **Không tự tắt** |

**Toast lỗi không tự tắt.** Người dùng có thể đang nhìn chỗ khác đúng lúc nó hiện. Với một thông báo thành công thì bỏ lỡ không sao — kết quả đã thấy trên màn hình. Với một lỗi thì bỏ lỡ nghĩa là họ tin thao tác đã thành công.

**Thời gian tăng dần theo mức nghiêm trọng** vì mức nghiêm trọng tỉ lệ với lượng chữ cần đọc, và với xác suất người dùng muốn đọc lại. Thời gian cố định theo vai, không tính theo độ dài chữ — nội dung đã bị chặn ở hai dòng nên không có gì để đo.

Nền `Toast` luôn là `--color-surface` với viền `--color-border`; **chỉ icon mang màu vai**. Tô cả nền theo vai làm bốn loại toast trông như bốn component khác nhau, và làm chữ trên nền màu khó đọc ở theme tối.

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Bề rộng | `--layout-toast-w`, **cố định** — bí danh bậc `sm` của thang bề rộng ([`../DESIGN.md`](../DESIGN.md) §6.1), để các toast trong ngăn xếp thẳng mép; ở khổ hẹp co theo §Responsive |
| Đệm | `--sp-5` |
| Khe icon → nội dung | `--sp-4` |
| Khe tiêu đề → nội dung | `--sp-2` |
| Khe giữa hai toast trong ngăn xếp | `--sp-4` |
| Bo góc | `--radius-md` |
| Bóng | `--shadow-4` |
| Cỡ chữ tiêu đề | `--fs-sm`, `--fw-semibold` |
| Cỡ chữ nội dung | `--fs-sm`, `--fw-regular`, `--color-text-muted` |
| Lớp | `--z-toast` — cao nhất trong hệ, trên cả `Dialog` |

**`Toast` phải nằm trên `Dialog`.** Một thao tác trong dialog thất bại thì thông báo phải thấy được; nằm dưới backdrop là thông báo không tồn tại.

**Ngăn xếp tối đa ba toast cùng lúc.** Cái thứ tư đẩy cái cũ nhất ra. Quá ba thì chúng che mất nội dung và không ai đọc kịp — mà nhiều toast cùng lúc gần như luôn là dấu hiệu nên gộp thành một.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`; viền `--border-w` `--color-border`; `--radius-md`; `--shadow-4`; icon vai bên trái; nút đóng bên phải | Có |
| `hover` | **Đồng hồ đếm ngược dừng lại** trong lúc con trỏ ở trên. Nếu không, toast biến mất đúng lúc người dùng đang với tay tới nút "Hoàn tác" | Có |
| `focus-visible` | Focus vào bất kỳ control nào bên trong cũng **dừng đồng hồ** — cùng lý do với hover, cho người dùng bàn phím | Có |
| `active` | **Không áp dụng.** `Toast` không phải control; nút bên trong có trạng thái của chúng | — |
| `disabled` | **Không áp dụng.** Một thông báo không có trạng thái vô hiệu hoá | — |
| `loading` | **Không áp dụng.** `Toast` báo việc **đã xong**. Muốn báo việc đang chạy thì dùng chỉ báo tại chỗ hoặc `NoticeBanner` | — |
| `error` | Không phải một trạng thái mà là biến thể `danger` | — |
| `empty` | **Không áp dụng.** Không có toast thì không vẽ gì cả — ngăn chứa rỗng phải **không chiếm chỗ** và không nhận sự kiện chuột | Có, dạng suy biến |

**Ngăn chứa rỗng phải trong suốt với chuột.** Một `<div>` cố định phủ góc màn hình với `pointer-events` mặc định sẽ chặn mọi cú bấm vào vùng đó — kể cả khi nó rỗng và vô hình. Đây là bug hay gặp và rất khó tìm: một góc màn hình đơn giản là không bấm được.

Vào: trượt vào từ mép + mờ dần (`--dur-slow`, `--ease-decelerate`). Ra: mờ dần + thu nhỏ chiều cao để những cái còn lại trượt lên (`--dur-base`, `--ease-accelerate`). Tắt khi `prefers-reduced-motion: reduce` ([`../DESIGN.md`](../DESIGN.md) §7) — khi đó toast hiện và biến mất tức thì.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-text`, `--color-text-muted`, `--color-border`, `--color-success`, `--color-info`, `--color-warning`, `--color-danger`, `--color-focus` |
| Chữ | `--fs-sm`, `--fw-semibold`, `--fw-regular`, `--lh-snug`, `--lh-normal` |
| Khoảng cách | `--sp-2`, `--sp-4`, `--sp-5`, `--sp-8` |
| Hình dạng | `--radius-md`, `--border-w` |
| Kích thước | `--icon-lg`, `--layout-toast-w` |
| Bóng | `--shadow-4` |
| Lớp | `--z-toast` |
| Chuyển động | `--dur-base`, `--dur-slow`, `--ease-decelerate`, `--ease-accelerate` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Ngăn xếp ở góc **phải dưới**, cách mép `--sp-8`. Toast mới ở dưới cùng |
| `$bp-sm` … `$bp-md` | Vẫn phải dưới, bề rộng co lại, cách mép `--sp-5` |
| < `$bp-sm` | Ngăn xếp lên **trên cùng**, chiếm gần hết bề rộng, cách mép `--sp-4`. Toast mới ở trên cùng |

**Vì sao đảo lên trên ở màn nhỏ:** phần dưới màn hình điện thoại là chỗ ngón tay cái nằm và là chỗ bàn phím ảo chiếm. Toast ở dưới sẽ bị ngón tay che hoặc bị bàn phím đẩy khuất.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vùng thông báo | Ngăn chứa mang `aria-live`. **Mức khác nhau theo vai**: `polite` cho `success` và `info`; `assertive` cho `warning` và `danger` |
| `role` | `role="status"` cho `polite`, `role="alert"` cho `assertive` |
| Ngăn chứa tồn tại sẵn | Vùng `aria-live` phải **có mặt trong DOM từ đầu**, rỗng. Chèn cả vùng `aria-live` cùng lúc với nội dung thì nhiều trình đọc màn hình **không đọc gì cả** — chúng chỉ theo dõi thay đổi bên trong một vùng đã tồn tại. Đây là bẫy phổ biến nhất của mọi thông báo động |
| `pointer-events` | Ngăn chứa `none`; từng toast `auto` |
| Nút đóng | Icon `pi-times` ([`../Icons.md`](../Icons.md) §5), có `aria-label`. Phần cấu trúc của lớp bọc: thư viện vẽ, tạo hình bằng token theo hình thức của [`IconButton.md`](./IconButton.md) — [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 |
| Icon | `aria-hidden="true"` — vai đã có trong chữ ([`../Icons.md`](../Icons.md) §7) |
| Thời gian đọc | Toast tự tắt sau vài giây có thể vi phạm WCAG 2.2 SC 2.2.1 nếu nội dung cần thời gian đọc. Giảm nhẹ bằng: dừng đồng hồ khi hover/focus, luôn có nút đóng, và **lỗi thì không tự tắt** |
| Bàn phím | Toast **không** tự cướp focus. Người dùng đang gõ dở mà bị nhảy focus ra chỗ khác là mất chỗ. Toast có nút hành động thì nút đó nằm trong thứ tự Tab bình thường |
| Chữ | Qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Toast không cướp focus — kể cả toast lỗi.** Với lỗi thật sự cần xử lý ngay thì `Toast` là component sai; dùng `Dialog`.

## API dự kiến

`Toast` tách làm **hai phần**, và đây là một quyết định kiến trúc chứ không phải chi tiết thi công:

| Phần | Vai | Ở đâu |
| --- | --- | --- |
| Hàng đợi | Giữ danh sách thông báo, hẹn giờ, giới hạn ba cái | Một service ở tầng `core/` — **smart** |
| Component | Vẽ danh sách nhận qua `input()` | Tầng dùng chung — **dumb** |

Không tách thì component phải là smart (nó tự giữ trạng thái toàn cục), và luật dumb ở [`../COMPONENTS.md`](../COMPONENTS.md) §5 cấm — đây đúng ca thứ hai trong bảng "dễ nhầm" của mục đó.

🛑 **Không cổng nào bắt được ca này.** [`../../RULES.md`](../../RULES.md) §7 F11 chỉ quét `shared/components/`; `Toast` là lớp bọc thư viện nên nó nằm ở `shared/ui/` ([`../COMPONENTS.md`](../COMPONENTS.md) §5, cột "Ở đâu"). Ràng buộc thật sự ép được ở tầng này là chiều import giữa hai tầng — [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2, ép bằng [`../../RULES.md`](../../RULES.md) §7 F24. Việc component không tự giữ hàng đợi thì **giữ bằng review**, không bằng lệnh: người dựng phải biết điều đó trước khi viết dòng đầu tiên.

**Component:**

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `items` | input | `ReadonlyArray<ToastItem>` | `[]` | Tối đa ba; cắt bớt là việc của service |
| `position` | input | `'bottom-right' \| 'top-center'` | `'bottom-right'` | Tự đổi theo điểm ngắt; input chỉ để ghi đè |
| `dismissed` | output | `string` | — | Id của toast bị đóng |
| `actionClicked` | output | `string` | — | Id của toast vừa được bấm nút hành động. Nút ("Hoàn tác") chỉ có khi item mang `actionLabel`; nó là phần cấu trúc của lớp bọc — thư viện vẽ, tạo hình bằng token theo hình thức của [`Button.md`](./Button.md) ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5). Tầng khung chuyển id cho hàng đợi. Chưa card nào trong [`../../contracts/README.md`](../../contracts/README.md) khai endpoint hoàn tác — nút này chỉ dùng khi card của thao tác đó có, và cửa sổ hoàn tác chốt cùng lúc với endpoint |

**`ToastItem`:** chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. `duration` bỏ trống thì lấy mặc định theo vai.

## Do / Don't

- ✅ Dùng `Toast` cho việc **đã xong**.
- ✅ Gộp nhiều kết quả từ một thao tác thành một toast: "Đã xoá 12 người dùng".
- ✅ Dừng đồng hồ khi hover hoặc focus.
- ✅ Kèm nút "Hoàn tác" thay vì hỏi xác nhận trước — xem [`ConfirmDialog.md`](./ConfirmDialog.md).
- ✅ Giữ vùng `aria-live` trong DOM từ đầu.
- ❌ Không dùng `Toast` cho thông tin người dùng không được bỏ lỡ.
- ❌ Không để toast lỗi tự tắt.
- ❌ Không cướp focus.
- ❌ Không quá ba toast cùng lúc.
- ❌ Không tô nền theo vai.
- ❌ Không để ngăn chứa rỗng chặn chuột.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có giữ lịch sử thông báo để xem lại không? Nó cứu được ca bỏ lỡ toast, nhưng thêm một chỗ chứa trạng thái và một biểu tượng chuông trên `Topbar` | Sau F3 — khu thông báo trong ứng dụng |
