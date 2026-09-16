---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Stepper

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Theo [`../COMPONENTS.md`](../COMPONENTS.md) §4 nó là một dải chỉ báo — chấm, vạch nối, nhãn. Không có hành vi nào thuộc nhóm "khó"; việc chuyển bước là của trang, không phải của component.

---

## Mục đích

Chia một việc dài thành các bước có thứ tự và cho biết đang ở bước nào, còn bước nào phía trước.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Nhập dữ liệu từ tệp: chọn tệp → khớp cột → kiểm tra → nhập | 🛑 Luồng mà **người dùng không điều khiển thứ tự**, chỉ chờ người khác duyệt → [`Timeline.md`](./Timeline.md) biến thể `approval`. Xem ranh giới dưới đây |
| Tạo đơn vị mới kèm tài khoản quản trị đầu tiên | 🛑 Chuyển giữa các phần nội dung **không có thứ tự** → [`Tabs.md`](./Tabs.md) |
| Thiết lập lần đầu sau khi cài đặt | 🛑 Một form dài nhưng làm được một lượt → chia nhóm trường trong cùng một màn, đừng cắt thành bước |
| Việc có **ít nhất ba** bước | 🛑 Hai bước → dùng hai màn thường, hoặc một [`Dialog.md`](./Dialog.md) hai trang. Một stepper hai bước là chỉ báo thừa |

### Ranh giới với `Timeline` biến thể `approval` — một câu

> **Ai quyết định bước tiếp theo xảy ra khi nào?**
>
> Người đang nhìn màn hình → `Stepper`. Người khác, hoặc hệ thống → `Timeline` biến thể `approval`.

Đó là khác biệt thật và nó đổi cả hình thức lẫn hành vi: `Stepper` có nút "Tiếp tục" vì người dùng bấm là đi tiếp; `approval` không có nút đó vì bấm cũng không đi tiếp được. Trộn hai thứ tạo ra một màn hứa rằng bấm là xong, trong khi thực tế phải chờ trưởng phòng duyệt.

## Biến thể

| Biến thể | Hướng | Dùng khi |
| --- | --- | --- |
| `horizontal` | Các bước xếp ngang | **Mặc định.** Từ ba đến năm bước, nhãn ngắn |
| `vertical` | Các bước xếp dọc, nội dung từng bước nằm ngay dưới nhãn của nó | Từ sáu bước trở lên, hoặc khi mỗi bước có mô tả dài |
| `compact` | Một dòng: "Bước 3 / 4 · Kiểm tra dữ liệu" kèm một [`ProgressBar.md`](./ProgressBar.md) mảnh | Màn nhỏ, hoặc khi stepper chỉ là chỉ báo phụ |

Quá **bảy bước** thì không còn là một quy trình người dùng theo dõi được. Lúc đó gom các bước liên quan lại, hoặc tách thành hai quy trình.

## Kích thước

`Stepper` **không** dùng thang `--size-control-*` — nó không phải control.

| Khoản | Giá trị |
| --- | --- |
| Đường kính chấm | `--stepper-dot` |
| Độ dày vạch nối | `--stepper-line-w` |
| Cỡ chữ nhãn bước | `--fs-sm`, `--fw-medium`; bước hiện hành `--fw-semibold` |
| Cỡ chữ mô tả phụ | `--fs-2xs`, màu `--color-text-muted` |
| Khe chấm → nhãn | `--sp-4` |
| Khe giữa hai bước | `--sp-2` tối thiểu, vạch nối giãn lấp phần còn lại |

Hai giá trị trên là token tầng 3 của component này; số thật khai ở [`../DESIGN.md`](../DESIGN.md) §6.5.

## Trạng thái

Bảng dưới là trạng thái của **cả dải**. Trạng thái của từng bước ở mục sau.

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Dải nằm trên nền trang, không có bề mặt riêng, không viền. Nó là chỉ báo, không phải một khối nội dung | Có |
| `hover` | Chỉ áp cho bước **đã qua** (bấm quay lại được): nhãn đổi `--color-text`, con trỏ `pointer`. Bước chưa tới không có hover — nó không bấm được. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | Bước bấm được nhận `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px` quanh cả cụm chấm + nhãn | Có |
| `active` | Bước đang bị nhấn: chấm đậm thêm một bậc. Không dùng `transform` | Có |
| `disabled` | Cả quy trình bị khoá (không đủ quyền): mọi bước dùng `--color-text-disabled`, không bước nào bấm được. Kèm [`NoticeBanner.md`](./NoticeBanner.md) nói vì sao — một dải mờ không lý do là một màn hình câm | Có |
| `loading` | Đang xử lý bước hiện hành: chấm của bước đó đổi thành `pi-spinner` quay, `aria-busy="true"` trên dải. Các bước khác giữ nguyên | Có |
| `error` | Một bước hỏng: xem bảng bước. Dải **không** đổi màu toàn bộ — chỉ bước lỗi đổi, còn lại giữ nguyên để người dùng thấy mình đã đi được tới đâu | Có |
| `empty` | **Không áp dụng.** Một stepper không có bước nào là lỗi thi công, không phải một trạng thái để thiết kế | — |

### Bốn trạng thái của một bước

| Trạng thái bước | Chấm | Nhãn | Bấm được? |
| --- | --- | --- | --- |
| `done` | Nền `--color-success`, icon `pi-check` màu `--color-text-on-brand` | `--color-text` | ✅ Có — quay lại sửa |
| `current` | Nền `--color-brand`, số thứ tự màu `--color-text-on-brand` | `--color-text`, `--fw-semibold` | — đang ở đây |
| `upcoming` | Nền `--color-surface`, viền `--color-border` dày `--border-w-strong`, số màu `--color-text-muted` | `--color-text-muted` | 🛑 Không |
| `error` | Nền `--color-danger`, icon `pi-times-circle` màu `--color-text-on-brand` | `--color-danger` | ✅ Có — quay lại sửa |

🛑 **Bốn trạng thái này khác nhau bằng HÌNH DẠNG, không chỉ bằng màu.** Dấu tích, số, dấu nhân — ba hình khác nhau. [`../DESIGN.md`](../DESIGN.md) §2.7 cấm để màu làm kênh duy nhất, và một dải bốn chấm chỉ khác sắc là bốn chấm giống hệt nhau với người mù màu.

Vạch nối giữa hai bước đã qua dùng `--color-success`; vạch tới bước chưa đi dùng `--color-border-subtle`.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-text-on-brand`, `--color-brand`, `--color-success`, `--color-danger`, `--color-border`, `--color-border-subtle`, `--color-focus` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-semibold`, `--fw-bold`, `--lh-snug` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4`, `--sp-6` |
| Hình dạng | `--radius-full`, `--border-w-strong` |
| Kích thước | `--icon-sm`, `--stepper-dot`, `--stepper-line-w` |
| Icon | `pi-check`, `pi-times-circle`, `pi-spinner` — [`../Icons.md`](../Icons.md) §5 |
| Chuyển động | `--dur-base`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | `horizontal` đầy đủ: chấm, nhãn, mô tả phụ |
| `$bp-md` … `$bp-lg` | `horizontal` bỏ mô tả phụ, giữ nhãn |
| < `$bp-md` | Chuyển sang `compact` — một dòng "Bước 3 / 4 · Kiểm tra dữ liệu" kèm thanh tiến trình mảnh |

**Vì sao đổi hẳn sang `compact` thay vì cho cuộn ngang:** một dải bốn bước cuộn ngang thì người dùng chỉ thấy hai bước một lúc, và chỉ báo tiến độ mất hết ý nghĩa — họ không còn thấy mình đang ở đâu trong toàn bộ. Một dòng chữ nói thẳng "3 / 4" giữ đúng thông tin đó trong một phần mười chỗ.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<ol>` — các bước **có thứ tự**, và thứ tự đó là thông tin |
| Bước hiện hành | `aria-current="step"` trên đúng một bước |
| Bước bấm được | `<button>` thật bên trong `<li>`. Bước chưa tới **không** bọc trong nút — nó không bấm được, và một nút không bấm được là một lời hứa hụt |
| Bước chưa tới | `aria-disabled="true"`, vẫn đọc lên được. Người dùng cần **biết** còn bao nhiêu bước nữa |
| Nhãn đọc lên | Nói cả thứ tự và trạng thái: "Bước 2 trên 4, Khớp cột, đã xong" |
| Bước lỗi | `aria-invalid="true"` và `aria-describedby` trỏ vào dòng nói lỗi gì |
| Chuyển bước | Báo qua `aria-live="polite"`: "Đã sang bước 3 trên 4, Kiểm tra dữ liệu" |
| Bàn phím | `Tab` đi qua các bước bấm được theo thứ tự. Không cần phím mũi tên — đây không phải một nhóm lựa chọn |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `steps` | input | `ReadonlyArray<StepItem>` | `[]` | Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 |
| `current` | input | `number` | `0` | Chỉ số bước hiện hành, đếm từ 0 |
| `variant` | input | `'horizontal' \| 'vertical' \| 'compact'` | `'horizontal'` | |
| `clickableDone` | input | `boolean` | `true` | Cho quay lại bước đã qua. Mặc định **cho** — chặn người dùng sửa lại thứ họ vừa nhập là chặn nhầm hướng |
| `disabled` | input | `boolean` | `false` | |
| `stepSelected` | output | `number` | — | Phát chỉ số bước được chọn. **Không phát cho bước chưa tới** — chặn ở component, không bắt mỗi nơi gọi tự nhớ |

Nội dung của từng bước **không** nằm trong component này. `Stepper` chỉ là dải chỉ báo; trang quyết định hiện gì dưới nó. Gộp nội dung vào sẽ biến nó thành một khung điều hướng, và lúc đó nó gánh hai vai.

Ba luật của trang dùng `Stepper`: rời quy trình giữa chừng thì **hỏi xác nhận** qua guard "chưa lưu" ([`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md)) — giữ tạm cần chỗ lưu bản nháp mà Core không có; "Tiếp tục" là submit của bước, nên bước lỗi **chặn** đi tiếp theo đúng luật submit của [`FormRow.md`](./FormRow.md); `compact` dùng lại [`ProgressBar.md`](./ProgressBar.md) `determinate` cỡ `sm` — file đó đã nhận "chỉ báo bước ở màn nhỏ" là chỗ dùng của nó.

`Stepper` là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Cho quay lại bước đã qua; chặn nhảy cóc tới bước chưa tới.
- ✅ Hiện bước chưa tới, chỉ là không bấm được — người dùng cần biết còn bao nhiêu.
- ✅ Phân biệt bốn trạng thái bằng hình dạng, không chỉ bằng màu.
- ✅ Hỏi xác nhận khi người dùng rời quy trình giữa chừng, hoặc giữ tạm cho họ quay lại.
- ❌ Không dùng `Stepper` cho luồng mà người dùng chỉ ngồi chờ.
- ❌ Không dùng cho hai bước.
- ❌ Không nhét nội dung từng bước vào chính component.
- ❌ Không im lặng vứt đi công sức của bốn bước khi người dùng lỡ bấm ra ngoài.

## Cần chốt

Không còn.
