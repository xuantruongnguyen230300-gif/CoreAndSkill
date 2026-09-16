---
kind: luat
scope: core
verified: chua-doi-chieu
---

# SegmentedControl

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Lý do ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — nó chỉ là một hàng nút cùng một máng nền, không có overlay, không có ảo hoá, không có bẫy focus. Phần khó duy nhất là bàn phím theo chuẩn `radiogroup`, và phần đó ngắn hơn nhiều so với lượng CSS mặc định phải đè nếu đi bọc.

---

## Mục đích

Chuyển giữa vài chế độ xem loại trừ nhau của **cùng một tập nội dung**, hiện cả các lựa chọn cùng lúc.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Lọc nhanh một danh sách theo vài nhóm cố định: "Tất cả / Đang hoạt động / Đã khoá" | 🛑 Chuyển sang một **vùng nội dung khác** (tab panel) → [`Tabs.md`](./Tabs.md). Đó là `tablist`, có ngữ nghĩa panel; cái này thì không |
| Đổi cách trình bày cùng một dữ liệu: bảng / thẻ, ngày / tuần / tháng | 🛑 Hơn bốn lựa chọn, hoặc nhãn dài → dùng ô chọn theo [`Input.md`](./Input.md). Xem lý do dưới |
| Chọn một giá trị ngắn, loại trừ, trong `Toolbar` — nơi hiện hết lựa chọn nhanh hơn mở một danh sách | 🛑 Chọn một **giá trị** để gửi lên trong form → [`Check.md`](./Check.md) biến thể `radio` |
| Bật/tắt hai chế độ có tên rõ ràng, ví dụ "Của tôi / Tất cả" | 🛑 Chọn được nhiều mục cùng lúc → [`Check.md`](./Check.md); một segmented control chọn được nhiều là một hàng nút bật/tắt, không phải component này |

**Tối đa bốn lựa chọn, và đây là biên cứng chứ không phải khuyến nghị.** Component này đánh đổi bề rộng lấy tốc độ: nó chiếm chỗ của cả bốn nhãn để người dùng khỏi phải mở một danh sách. Từ lựa chọn thứ năm trở đi, đánh đổi đó lật ngược — hàng nút tràn ra khỏi `Toolbar`, phải xuống dòng, và một danh sách thả xuống vừa gọn hơn vừa đọc nhanh hơn.

**Vì sao là `radiogroup` chứ không `tablist`.** Nhìn thì giống hệt nhau, nhưng `tablist` là một hợp đồng: trình đọc màn hình sẽ hứa với người dùng rằng có một vùng `tabpanel` đi kèm, được `aria-controls` trỏ tới, và focus sẽ đi vào đó. Component này không đổi panel — nó **lọc dữ liệu trong panel đang có**. Khai `tablist` ở đây là hứa một thứ không tồn tại, và người dùng bàn phím sẽ đi tìm một vùng nội dung không bao giờ đến.

## Biến thể

| Biến thể | Vai | Máng nền | Ghi chú |
| --- | --- | --- | --- |
| `default` | **Mặc định.** Đứng trong `Toolbar` hoặc dưới [`PageHeader.md`](./PageHeader.md) | `--color-surface-2` | Bề rộng theo nội dung |
| `block` | Chiếm hết bề rộng vùng chứa, các đoạn chia đều nhau | `--color-surface-2` | Dùng ở màn nhỏ và trong [`Card.md`](./Card.md) hẹp |

Chỉ hai biến thể, và không có biến thể "chỉ icon". Một hàng đoạn chỉ có icon buộc người dùng đoán nghĩa của từng cái mà không có chữ nào để đối chiếu — [`../Icons.md`](../Icons.md) §2 bước 3 nói rõ: icon không tự giải thích được thì thêm chữ, đừng bỏ chữ. Icon dẫn **trước** nhãn thì được, và luôn là trang trí (`aria-hidden="true"`).

## Kích thước

| Cỡ | Chiều cao đoạn | Đệm ngang | Cỡ chữ | Icon | Dùng khi |
| --- | --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` (28px) | `--sp-4` | `--fs-xs` | `--icon-sm` | Trong `Toolbar` dày đặc, trong đầu [`Card.md`](./Card.md) |
| `md` | `--size-control-md` (34px) | `--sp-5` | `--fs-sm` | `--icon-md` | **Mặc định.** `Toolbar`, dưới `PageHeader` |
| `lg` | `--size-control-lg` (42px) | `--sp-6` | `--fs-md` | `--icon-md` | Bộ chọn chính của một màn, ở màn nhỏ |

Máng bo `--radius-sm`; các đoạn bên trong bo theo máng ở hai đầu, vuông ở giữa. Nhãn **không xuống dòng** (`white-space: nowrap`) — một đoạn cao gấp đôi làm cả hàng lệch. Nhãn dài thì rút ngắn câu, hoặc chuyển sang ô chọn. Đoạn đang chọn phân biệt bằng **nền lấp đầy trên máng**, không có dải accent — dải cạnh dưới là dấu hiệu của [`Tabs.md`](./Tabs.md); thứ phân biệt hai component là cái máng có viền bao quanh cả nhóm, `Tabs` không có máng.

## Trạng thái

Trạng thái ở đây có hai tầng: của **cả nhóm** và của **từng đoạn**. Bảng dưới nói rõ tầng nào.

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nhóm: máng `--color-surface-2`, viền `--color-border`, `--radius-sm`. Đoạn chưa chọn: nền trong suốt, chữ `--color-text-muted`, `--fw-medium`. Đoạn đang chọn: nền `--color-brand-subtle`, chữ `--color-brand-on-subtle`, `--fw-semibold` | Có |
| `hover` | Đoạn chưa chọn → chữ `--color-text`, nền `--color-surface-3`. Đoạn đang chọn không đổi. `--dur-fast` với `--ease-standard`. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px` trên **đoạn** đang có focus, không trên cả máng | Có |
| `active` | Nền `--color-surface-3`; không dịch chuyển hình — cả hàng nhích 1px trông như lỗi vẽ | Có |
| `disabled` | Cả nhóm: `opacity` giảm, chữ `--color-text-disabled`, `cursor: not-allowed`, mọi đoạn mang `disabled`. Một đoạn lẻ vô hiệu hoá: chỉ đoạn đó, và phím mũi tên **bỏ qua** nó | Có |
| `loading` | Nhóm mang `aria-busy="true"` và bị khoá trong lúc dữ liệu của chế độ mới đang về. Đoạn vừa bấm **đổi sang trạng thái chọn ngay**, không chờ máy chủ — xem giải thích dưới | Có |
| `error` | **Không áp dụng.** Component không tự mang lỗi. Nếu chế độ vừa chọn tải hỏng, lỗi hiện trong vùng nội dung bằng [`EmptyState.md`](./EmptyState.md) hoặc [`NoticeBanner.md`](./NoticeBanner.md), còn bộ chọn giữ nguyên chế độ đã chọn | — |
| `empty` | **Không áp dụng.** Một nhóm không có đoạn nào là lỗi thi công — nơi gọi phải ẩn hẳn component thay vì hiện một cái máng rỗng | — |

**Vì sao đoạn vừa bấm đổi ngay thay vì chờ máy chủ:** người dùng bấm "Đã khoá" và chờ 400ms mà bộ chọn vẫn nằm ở "Tất cả" sẽ bấm lần hai. Đổi ngay giữ được cảm giác điều khiển; phần chờ dồn vào vùng nội dung, nơi [`SkeletonLoader.md`](./SkeletonLoader.md) làm việc đó. Cái giá thật: nếu yêu cầu hỏng, bộ chọn đang hiển thị một chế độ mà nội dung bên dưới không khớp — vì vậy dòng `error` ở trên bắt buộc vùng nội dung phải nói ra điều đó, không được im lặng.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-surface-2`, `--color-surface-3`, `--color-border`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fs-md`, `--fw-medium`, `--fw-semibold`, `--lh-snug` |
| Khoảng cách | `--sp-1`, `--sp-3`, `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-sm`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-sm`, `--icon-md` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

**Đoạn đang chọn có hai kênh, và kênh chữ là kênh gánh ngưỡng.** `--color-brand-subtle` chênh nền chỉ 1.25:1 nên [`../DESIGN.md`](../DESIGN.md) §2.4 cấm nó làm tín hiệu duy nhất; cùng mục đó chỉ đòi **một** dấu hiệu đạt 3:1 đi kèm. Ở đây dấu hiệu đó là chữ `--color-brand-on-subtle` (7.47:1 sáng / 8.35:1 tối), thêm nét `--fw-semibold`. Bỏ kênh chữ là hồi quy accessibility, không phải lựa chọn thẩm mỹ; không có dải accent để bù.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Biến thể `default`, bề rộng theo nội dung, đứng cùng hàng với các control khác trong `Toolbar` |
| `$bp-sm` … `$bp-md` | Chuyển sang `block`, các đoạn chia đều bề rộng; nhãn rút về dạng ngắn nếu có |
| < `$bp-sm` | Vẫn `block`; cỡ tối thiểu nâng lên `md` để vùng chạm đủ rộng |
| < `$bp-xs` | Từ ba lựa chọn trở lên và nhãn không rút ngắn được → **đổi hẳn sang ô chọn**, không cho hàng xuống dòng. Ngưỡng nằm **trong component** theo đúng dòng này; nơi gọi chỉ cấp nhãn ngắn, không tự quyết |

🛑 **Không bao giờ để hàng đoạn xuống dòng.** Một `SegmentedControl` vỡ thành hai dòng mất luôn thứ nó bán: hình ảnh một cái công tắc liền khối. Hai dòng trông y hệt hai nhóm khác nhau. Đổi sang ô chọn là mất một lần chạm, và đó là cái giá rẻ hơn.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Máng là một phần tử bao; mỗi đoạn là `<button type="button">` thật |
| Vai trò ARIA | Máng `role="radiogroup"`; mỗi đoạn `role="radio"` với `aria-checked`. 🛑 Không `tablist`/`tab` — lý do ở mục Biến thể |
| Nhãn nhóm | `aria-label` hoặc `aria-labelledby` trên máng, nói **nhóm này chọn cái gì**: "Lọc theo trạng thái", không phải "Bộ lọc" |
| Bàn phím | `Tab` vào **cả nhóm một lần**. `ArrowRight`/`ArrowDown` sang đoạn kế, `ArrowLeft`/`ArrowUp` về đoạn trước, có vòng lại từ cuối về đầu. `Home`/`End` nhảy đầu/cuối |
| Roving tabindex | Đúng một đoạn mang `tabindex="0"` (đoạn đang chọn), các đoạn còn lại `tabindex="-1"`. Thiếu điều này, `Tab` phải bấm bốn lần mới qua hết một bộ lọc |
| Kích hoạt | Mũi tên di chuyển **và chọn luôn** — đúng khuôn radio gốc. Đoạn bị `disabled` bị bỏ qua khi di chuyển |
| Focus | `:focus-visible` trên đoạn, `outline-offset` ≥ 2px |
| Icon | Icon dẫn luôn `aria-hidden="true"` — nó lặp lại điều nhãn đã nói ([`../Icons.md`](../Icons.md) §7) |
| Thông báo đổi | Vùng nội dung bị lọc tự báo số kết quả qua `aria-live="polite"`. Bộ chọn **không** tự đọc lên — `aria-checked` đã đủ |
| Vùng bấm | Mỗi đoạn ≥ 28×28px, nới bằng `padding` |
| Chữ | Nhãn đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `options` | input | `ReadonlyArray<SegmentOption>` | — | **Bắt buộc.** Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. Component **không** kiểm số lượng lúc chạy, nhưng quá bốn là sai spec |
| `value` | input | `string \| number \| null` | `null` | Giá trị đang chọn. `null` nghĩa là chưa chọn gì — hợp lệ nhưng hiếm; phần lớn ca nên có sẵn một chế độ mặc định |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `block` | input | `boolean` | `false` | |
| `disabled` | input | `boolean` | `false` | Khoá cả nhóm. Khoá lẻ từng đoạn thì đặt trong `options` |
| `loading` | input | `boolean` | `false` | Sinh `aria-busy` và khoá tạm cả nhóm |
| `ariaLabel` | input | `string` | — | **Bắt buộc.** Nhóm phải có tên, nếu không trình đọc màn hình chỉ đọc "nhóm nút chọn" |
| `valueChange` | output | `string \| number` | — | Phát **sau khi** trạng thái chọn đã đổi. Không phát khi bấm lại chính đoạn đang chọn, và không phát khi `disabled` hoặc `loading` |

**Vì sao bấm lại đoạn đang chọn không phát sự kiện:** nơi gọi hầu như luôn nối `valueChange` vào một lần gọi máy chủ. Phát lại giá trị cũ sinh ra một yêu cầu thừa mỗi khi người dùng bấm nhầm hai lần. Chặn ở component là chặn một lần; để nơi gọi tự nhớ là để mỗi màn tự quên một kiểu.

`SegmentedControl` là component **dumb** — nhận `options` qua `input()`, phát `valueChange` qua `output()`, không tự gọi API lọc ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Giữ tối đa bốn lựa chọn, nhãn một hoặc hai từ.
- ✅ Luôn khai `ariaLabel` cho nhóm.
- ✅ Cài roving tabindex — cả nhóm là một điểm dừng `Tab`.
- ✅ Đổi trạng thái chọn ngay khi bấm, để phần chờ cho vùng nội dung.
- ❌ Không dùng `role="tablist"` cho component này.
- ❌ Không để hàng đoạn xuống dòng — đổi sang ô chọn.
- ❌ Không dựa vào riêng `--color-brand-subtle` để báo đoạn đang chọn.
- ❌ Không làm biến thể chỉ có icon.
- ❌ Không cho chọn nhiều đoạn cùng lúc.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có cho mỗi đoạn mang một `Badge` đếm số không ("Đang hoạt động 12")? Nó hữu ích ở màn lọc, nhưng làm bề rộng đoạn đổi mỗi lần dữ liệu đổi, và cả hàng sẽ nhảy chỗ dưới ngón tay người dùng | Sau F3 — dự án hạ nguồn đầu tiên cần số đếm theo trạng thái (card phải trả số) |
