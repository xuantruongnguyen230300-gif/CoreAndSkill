---
kind: luat
scope: core
verified: chua-doi-chieu
---

# DatePicker

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, lịch thả xuống là lớp nổi phải định vị khi cuộn hoặc tràn viewport, cộng bàn phím di chuyển trong lưới ngày; bộ chọn gốc của trình duyệt không ép được `dd/mm/yyyy` và không có lối tắt. Tách khỏi [`Input.md`](./Input.md) (tự dựng) vì cột "Nền" của một component chỉ nhận một giá trị. Thư mục thi công theo ánh xạ cột "Nền": `shared/ui/date-picker`. Nằm ở nhánh tải chậm và không mang từ vựng ngành — [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md) ràng buộc 2 và 3.

---

## Mục đích

Chọn một ngày hoặc một khoảng ngày bằng lịch hoặc bằng gõ tay, luôn hiện theo `dd/mm/yyyy`.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Ô ngày trong form, dialog, `Toolbar`; ô lọc theo khoảng thời gian ở màn danh sách | 🛑 Ô nhập chữ, số, mật khẩu, chọn từ danh sách → [`Input.md`](./Input.md) |
| Kỳ báo cáo, lọc bản ghi theo khoảng | 🛑 Chọn tháng hoặc năm đơn thuần → biến thể `select` của [`Input.md`](./Input.md); 🛑 nhãn, gợi ý, chỗ hiện lỗi → bọc trong [`FormRow.md`](./FormRow.md) |

## Biến thể

| Biến thể | Hình dạng | Dùng khi |
| --- | --- | --- |
| `single` | Một ô + lịch một tháng | **Mặc định.** Ngày chứng từ, ngày sinh, ngày hiệu lực |
| `range` | Hai ô trong một khung + lịch hai tháng, cột lối tắt bên trái panel | Kỳ báo cáo, lọc theo khoảng |

### Bốn luật riêng — áp cho cả hai biến thể

1. 🛑 **Định dạng là `dd/mm/yyyy`, không thương lượng.** Hiện `mm/dd` ở bất cứ đâu là tạo lỗi nhập liệu im lặng: không sai cú pháp, chỉ sai ngày.
2. **`range` chặn khoảng ngược ngay lúc chọn**, không phải lúc bấm Tìm: chọn ngày đầu xong thì mọi ngày trước đó bị làm mờ.
3. **Gõ tay phải chấp nhận được.** Gõ `01092026` thì tự chèn dấu gạch chéo; không ép mở lịch mới chọn được.
4. **Lối tắt của `range` là đầu vào của trang.** Core chỉ khai bộ mặc định độc lập nghiệp vụ: hôm nay · 7 ngày qua · tháng này · quý này · năm nay. Lối tắt mang nghĩa nghiệp vụ sống ở `spec/<feature>/ui-spec.md`, truyền vào qua `presets` — [`../CLAUDE.md`](../CLAUDE.md) §1.

Trạng thái "khoảng đã chọn bị khoá sửa" vì lý do nghiệp vụ **không** phải trạng thái của ô: ô vẫn hợp lệ, trang cảnh báo bằng [`NoticeBanner.md`](./NoticeBanner.md) vai `warning`.

## Kích thước

| Cỡ | Chiều cao ô | Đệm | Cỡ chữ | Dùng khi |
| --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` | `--sp-3` / `--sp-4` | `--fs-xs` | Ô lọc trong `Toolbar`, ô sửa tại chỗ |
| `md` | `--size-control-md` | `--sp-3` / `--sp-4` | `--fs-sm` | **Mặc định.** Form, dialog |
| `lg` | `--size-control-lg` | `--sp-4` / `--sp-5` | `--fs-md` | Chỉ khi ô ngày là hành động chính của màn |

Bề rộng ô theo công tắc `width` như [`Input.md`](./Input.md); panel lịch không nhận cỡ.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Ô như `default` của [`Input.md`](./Input.md); icon lịch ở `suffix`. Panel: nền `--color-surface`, viền `--color-border`, `--shadow-3`, `--z-popover`; ngày hôm nay có viền `--color-brand`; ngày đã chọn nền `--color-brand`, chữ `--color-text-on-brand`; khoảng giữa hai đầu nền `--color-brand-subtle`, chữ `--color-brand-on-subtle` | Có |
| `hover` | Ô: như `Input`. Ngày trong lịch: nền `--color-surface-2`. Bọc trong `@media (hover: hover)` | Có |
| `focus-visible` | Ô: như `Input`. Ngày đang focus trong lưới: `outline` `--border-w-strong` màu `--color-focus`, `outline-offset` ≥ 2px | Có |
| `active` | **Không áp dụng cho ô** — cùng lý do với `Input`. Ngày đang nhấn: nền đậm thêm một bậc, không dịch chuyển | Có (lưới) |
| `disabled` | Ô như `disabled` của `Input`; không mở được panel. Ngày bị làm mờ (ngoài `min`/`max`, trước ngày đầu ở `range`): chữ `--color-text-disabled`, `aria-disabled="true"`, vẫn nằm trong lưới bàn phím | Có |
| `loading` | **Không áp dụng.** Component không chờ dữ liệu ngoài; lịch tính tại chỗ | — |
| `error` | Bật khi control `invalid && touched` (§API dự kiến), cùng khuôn `error` của [`Input.md`](./Input.md): viền `--color-danger-border`, `aria-invalid="true"`, `aria-describedby` trỏ dòng lỗi do `FormRow` vẽ. Chuỗi gõ tay không thành ngày hợp lệ → validator cài sẵn gắn lỗi `dateInvalid` lên control, giá trị đi qua control vẫn là `null`; câu lỗi dịch qua khoá `loi.CORE.CLIENT.VALIDATION_DATEINVALID` ([`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §6.5) | Có |
| `empty` | **Không áp dụng.** Ô trống là bình thường; placeholder `dd/mm/yyyy` đã xử lý | — |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-text-on-brand`, `--color-border`, `--color-border-strong`, `--color-brand`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-danger-border`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fs-md`, `--fw-regular`, `--fw-semibold`, `--lh-snug` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-sm`, `--radius-md`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-md` |
| Bóng, lớp | `--shadow-3`, `--z-popover` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Ô theo `width`; `range` panel hai tháng cạnh nhau, lối tắt cột trái |
| < `$bp-md` | Ô về `width: full`; `range` panel còn một tháng, lối tắt thành hàng cuộn ngang phía trên lịch |
| < `$bp-sm` | Panel dính đáy màn hình như `Dialog` ở ngưỡng này ([`Dialog.md`](./Dialog.md) §Responsive) |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Ô là `<input type="text">` thật, `inputmode="numeric"`; nút mở lịch là `<button type="button">` với `aria-label`, `aria-expanded`, `aria-haspopup="dialog"` |
| Panel | `role="dialog"` có nhãn; lưới ngày `role="grid"`, mỗi ngày `role="gridcell"`, ngày chọn `aria-selected="true"` |
| Bàn phím | Mũi tên đi giữa các ngày; `PageUp`/`PageDown` đổi tháng; `Home`/`End` về đầu/cuối tuần; `Enter` chọn; `Escape` đóng và trả focus về ô |
| Focus | Mở panel → focus vào ngày đang chọn hoặc hôm nay; đóng → về ô. Không bẫy focus khi gõ tay: người dùng gõ xong `Tab` đi tiếp mà không phải qua lịch |
| Nhãn | Do `FormRow` gắn qua `<label for>`; ô nhận `id`, `describedBy` như `Input` |
| Lỗi | `aria-invalid="true"` suy từ control: `invalid && touched` (§API dự kiến); `aria-describedby` = `describedBy` trang truyền, trỏ tới id của dòng lỗi — cùng khuôn [`Input.md`](./Input.md) §Accessibility |
| Định dạng | `dd/mm/yyyy` hiện ở placeholder và đọc lên; giá trị đọc lên là ngày đầy đủ, không phải chuỗi số |
| Chữ | Tên tháng, thứ, nhãn lối tắt mặc định đi qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `id` | input | `string \| null` | `null` | Trùng `controlId` của `FormRow` |
| `mode` | input | `'single' \| 'range'` | `'single'` | |
| `size` · `width` | input | `'sm' \| 'md' \| 'lg'` · `'full' \| 'md' \| 'sm'` | `'md'` · `'full'` | Như `Input` |
| `min` · `max` | input | `Date \| null` | `null` | Ngoài biên thì làm mờ |
| `presets` | input | `ReadonlyArray<DateRangePreset>` | `[]` | Chỉ có nghĩa khi `mode` là `'range'`; thêm vào **sau** bộ mặc định của Core. Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 |
| `disabled` · `readonly` · `describedBy` | input | như `Input` | như `Input` | `disabled` chỉ khi ô **không** gắn control — gắn control thì vô hiệu hoá đến qua `setDisabledState`; `describedBy` do trang truyền tường minh — cùng luật với [`Input.md`](./Input.md) §API |
| `valueChange` | output | `Date \| null` · `{ from: Date; to: Date } \| null` | — | Theo `mode`. Chỉ khi ô **không** gắn control — ô lọc trong [`Toolbar.md`](./Toolbar.md) |

**Form control chuẩn Angular.** `DatePicker` cài `ControlValueAccessor` cùng khuôn [`Input.md`](./Input.md) §API: nhận `[formControl]` / `formControlName`; giá trị, `touched` (lúc rời ô) và vô hiệu hoá đi qua control. Trạng thái `error` — viền, `aria-invalid` — suy từ control: `invalid && touched`; câu lỗi do trang lấy từ `fieldErrorText` ([`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §6.5) và truyền cho `error` của `FormRow`. Giá trị đi qua form control là `Date` (hoặc cặp `Date`), **không** phải chuỗi đã định dạng. **Gõ tay không thành ngày hợp lệ** thì component tự cài một `ValidatorFn` gắn lỗi `{ dateInvalid: true }` lên control — giá trị vẫn `null`, nhưng `invalid` khác `pristine`/ô trống, nên `fieldErrorText` phân biệt được "chưa nhập" với "nhập sai". Component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5): không biết kỳ nghiệp vụ, chỉ vẽ lối tắt được truyền vào. Là lớp bọc nên không import component tự dựng; nút điều hướng tháng và nút xoá do thư viện vẽ, tạo hình bằng token theo [`IconButton.md`](./IconButton.md) — [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5.

## Do / Don't

- ✅ Luôn hiện và đọc `dd/mm/yyyy`; placeholder là chính chuỗi đó.
- ✅ Cho gõ tay và tự chèn dấu; trả `Date` qua form control.
- ✅ Trong form, gắn qua `[formControl]` / `formControlName`; `valueChange` chỉ cho ô lọc trong `Toolbar`.
- ✅ Bọc trong `FormRow` khi có nhãn; trong `Toolbar` thì có `aria-label`.
- ❌ Không khai lối tắt mang nghĩa nghiệp vụ trong Core.
- ❌ Không dùng `type="date"` gốc — mất `dd/mm/yyyy` và lối tắt.
- ❌ Không để `mm/dd` xuất hiện ở bất kỳ locale nào.

## Cần chốt

Không còn. Icon nút mở lịch đã chốt: `pi-calendar`, dòng ở [`../Icons.md`](../Icons.md) §5. Lỗi gõ tay đã chốt ở mục Trạng thái và API dự kiến (`dateInvalid`, người dùng duyệt 2026-09-16).
