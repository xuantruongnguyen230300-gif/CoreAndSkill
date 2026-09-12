---
kind: luat
scope: core
verified: chua-doi-chieu
---

# LanguageSwitcher

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, biến thể `inline` chỉ là một nhóm nút. Biến thể `menu` có một lớp nổi, tức là chạm vào nhóm "khó"; nó **dùng lại** cơ chế menu đã có của Core chứ không tự khai một popover thứ hai — xem `Cần chốt`.

---

## Mục đích

Cho người dùng đổi ngôn ngữ giao diện ngay lúc chạy, không cần đăng nhập lại và không cần tải lại trang.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Góc [`Topbar.md`](./Topbar.md) của khung ứng dụng | 🛑 Đổi theme sáng/tối → nút riêng trong [`Topbar.md`](./Topbar.md) với `pi-sun` · `pi-moon` |
| Trong [`AuthCard.md`](./AuthCard.md), cuối thứ tự Tab | 🛑 Chọn ngôn ngữ **của dữ liệu** (nội dung song ngữ của một bản ghi) → đó là trường nghiệp vụ, dùng [`Input.md`](./Input.md) |
| Trang lỗi và trang ngoài khung ứng dụng | 🛑 Chọn quốc gia, múi giờ, đơn vị tiền → trường biểu mẫu bình thường; 🛑 chuyển giữa vài chế độ xem loại trừ nhau → [`SegmentedControl.md`](./SegmentedControl.md) |

## Biến thể

| Biến thể | Hình thức | Dùng khi |
| --- | --- | --- |
| `inline` | Một nút cho mỗi ngôn ngữ, xếp ngang | **Mặc định khi có hai hoặc ba ngôn ngữ.** Đổi ngôn ngữ chỉ tốn một lần bấm |
| `menu` | Một nút mở menu thả xuống, nhãn nút là ngôn ngữ hiện hành | Từ bốn ngôn ngữ trở lên, hoặc chỗ chật |
| `icon` | Nút chỉ có `pi-globe`, mở cùng menu như trên | `Topbar` ở màn nhỏ, nơi không còn chỗ cho chữ |

**Ngưỡng chuyển giữa `inline` và `menu` là số ngôn ngữ, không phải sở thích.** Hai ngôn ngữ mà giấu sau menu thì mỗi lần đổi tốn hai lần bấm cho một thao tác vốn chỉ có hai lựa chọn. Năm ngôn ngữ mà bày hết ra hàng ngang thì chúng ăn hết chỗ của khu người dùng trong `Topbar`. Nơi gọi chọn biến thể; component không tự quyết theo độ dài mảng, vì như vậy hình dạng `Topbar` sẽ đổi khi thêm một gói ngôn ngữ và không ai lường trước được.

## Kích thước

| Cỡ | Chiều cao | Đệm ngang | Cỡ chữ | Dùng khi |
| --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` (28px) | `--sp-4` | `--fs-xs` | Trong `Topbar`, nơi chiều cao đã bị `--layout-topbar-h` khoá |
| `md` | `--size-control-md` (34px) | `--sp-5` | `--fs-sm` | **Mặc định.** `AuthCard`, trang lỗi, trang cấu hình |

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Ngôn ngữ hiện hành: nền `--color-brand-subtle`, chữ `--color-brand-on-subtle`, `--fw-semibold`. Ngôn ngữ khác: nền trong suốt, chữ `--color-text-muted`, `--fw-medium`; `--radius-sm` | Có |
| `hover` | Mục chưa chọn: nền `--color-surface-2`, chữ `--color-text`. Bọc trong `@media (hover: hover)` | Có |
| `focus-visible` | `outline` `--border-w-strong` màu `--color-focus`, `outline-offset` theo [`../DESIGN.md`](../DESIGN.md) §2.6 | Có |
| `active` | Nền đậm thêm một bậc; không dịch chuyển vị trí — nhãn ngôn ngữ ngắn, dịch 1px đọc như lỗi render | Có |
| `disabled` | Cả nhóm nhận `disabled` trong lúc gói ngôn ngữ đang tải, để tránh bấm liên tiếp hai ngôn ngữ. Thuộc tính `disabled` thật, kèm con trỏ `not-allowed` | Có |
| `loading` | Ngôn ngữ vừa bấm hiện `pi-spinner` thay dấu hiệu chọn; **nhãn giữ nguyên**; `aria-busy="true"` trên nhóm | Có |
| `error` | Gói ngôn ngữ tải hỏng → **giữ nguyên ngôn ngữ cũ**, không đổi dấu hiệu chọn. Lỗi báo qua [`Toast.md`](./Toast.md); component này không tự hiện thông báo lỗi vì nó không biết vì sao hỏng | Có |
| `empty` | Danh sách rỗng, hoặc chỉ có đúng một ngôn ngữ → **không render phần tử nào**. Một nút đổi ngôn ngữ không đổi được gì là nhiễu thuần tuý | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-surface`, `--color-surface-2`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-focus` |
| Chữ, khoảng cách | `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-semibold`, `--lh-snug`, `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng, kích thước | `--radius-sm`, `--radius-md`, `--border-w`, `--border-w-strong`, `--size-control-sm`, `--size-control-md`, `--icon-md` |
| Lớp nổi | `--shadow-3` và `--z-popover` — chỉ cho lớp menu của biến thể `menu` và `icon` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

Icon `pi-globe` theo [`../Icons.md`](../Icons.md) §5 dòng "Đổi ngôn ngữ".

⚠️ `--color-brand-subtle` chỉ chênh với bề mặt 1.25:1 nên nó **không được là dấu hiệu duy nhất** của ngôn ngữ đang chọn ([`../DESIGN.md`](../DESIGN.md) §2.4). Kênh thứ hai ở đây là chữ đổi sang `--color-brand-on-subtle` (7.47:1 sáng / 8.35:1 tối) **và** nét chữ đổi sang `--fw-semibold`.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | `inline` xếp ngang, khe `--sp-2`; `menu` neo mép phải của nút |
| `--bp-sm` … `--bp-md` | `inline` với ba ngôn ngữ trở lên chuyển sang `menu`; hai ngôn ngữ giữ nguyên |
| < `--bp-sm` | Trong `Topbar` chuyển sang biến thể `icon`. Trong `AuthCard` giữ `inline` — ở đó có đủ chỗ và một lần bấm vẫn tốt hơn hai |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<button>` thật cho mỗi lựa chọn. 🛑 Không dùng `<select>` gốc: nhãn của nó bị hệ điều hành tạo hình và không kiểm soát được `lang` từng dòng |
| Nhóm, hiện hành | Biến thể `inline` bọc trong một nhóm có nhãn ("Ngôn ngữ") để trình đọc màn hình biết các nút thuộc cùng một lựa chọn; nút của ngôn ngữ đang dùng mang `aria-current="true"` |
| `lang` | **Mỗi nhãn ngôn ngữ mang thuộc tính `lang` của chính ngôn ngữ đó.** Không có nó, trình đọc màn hình sẽ đọc "English" bằng bộ phát âm tiếng Việt và người dùng nghe ra một chuỗi vô nghĩa |
| Nút mở menu | `aria-haspopup="menu"`, `aria-expanded` phản ánh trạng thái; nhãn nút nói cả ngôn ngữ hiện hành |
| Bàn phím | `inline`: Tab đi qua từng nút. `menu`: `Enter`/`Space` mở, mũi tên lên/xuống di chuyển, `Escape` đóng và **trả focus về nút mở** |
| Focus | Đổi ngôn ngữ **không** làm mất focus — focus ở lại đúng nút vừa bấm. Thuộc tính `lang` trên thẻ gốc của tài liệu đổi theo; việc này do tầng i18n làm nhưng là điều kiện để mọi thứ ở trên có nghĩa |
| Vùng bấm | ≥ 28×28px kể cả cỡ `sm`, nới bằng `padding` |
| Chữ | Tên ngôn ngữ **không** đi qua tầng i18n — xem ngay dưới |

🛑 **Tên ngôn ngữ viết bằng CHÍNH ngôn ngữ đó.** "Tiếng Việt", "English" — không phải "Việt Nam / Anh" khi đang ở tiếng Việt rồi đổi thành "Vietnamese / English" khi sang tiếng Anh. Lý do rất thực: người cần đổi ngôn ngữ là người **không đọc được ngôn ngữ đang hiển thị**. Nếu tên các ngôn ngữ đều được dịch sang ngôn ngữ hiện hành, người đó nhìn vào một danh sách mà không từ nào đọc được — đúng lúc họ cần tìm ngôn ngữ của mình nhất. Đây là ngoại lệ có chủ đích với luật i18n ở [`../../RULES.md`](../../RULES.md) §7 F8: tên ngôn ngữ là **dữ liệu**, không phải chuỗi giao diện, và nó nằm trong danh sách truyền vào qua `input()`.

🛑 **Cờ quốc gia không đại diện cho ngôn ngữ.** Một lá cờ chỉ một quốc gia, còn một ngôn ngữ trải qua nhiều quốc gia và một quốc gia dùng nhiều ngôn ngữ. Chọn cờ nào cho tiếng Anh cũng đều nói sai với một phần người dùng, và ở vài cặp quốc gia — ngôn ngữ thì lựa chọn đó còn mang nghĩa chính trị. Dùng **tên ngôn ngữ**; cần một dấu hiệu hình thì dùng `pi-globe` cho cả nhóm, không dùng cờ cho từng dòng.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'inline' \| 'menu' \| 'icon'` | `'inline'` | Nơi gọi quyết, không suy ra từ độ dài `languages` |
| `size` | input | `'sm' \| 'md'` | `'md'` | |
| `languages` | input | `ReadonlyArray<LanguageOption>` | `[]` | Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. Tên **viết bằng chính ngôn ngữ đó** |
| `currentLanguage` | input | `string \| null` | `null` | Mã ngôn ngữ đang dùng. `null` thì không mục nào mang `aria-current` |
| `disabled` · `loading` | input | `boolean` | `false` | |
| `pendingLanguage` | input | `string \| null` | `null` | Mã đang chờ tải, để đặt spinner đúng chỗ |
| `changeRequested` | output | `string` | — | Mã ngôn ngữ người dùng **yêu cầu**. Không phát khi bấm lại ngôn ngữ đang dùng |

**Đây là ca dễ nhầm nhất trong bảng dumb/smart** ([`../COMPONENTS.md`](../COMPONENTS.md) §5). Nhìn thì `LanguageSwitcher` "phải" biết service i18n: nó hiển thị ngôn ngữ hiện hành và nó đổi ngôn ngữ. Nhưng nó **không được inject service i18n**. Nó nhận danh sách và ngôn ngữ hiện hành qua `input()`, phát yêu cầu đổi qua `output()`; component khung ở tầng nền tảng nghe yêu cầu đó, gọi service, đợi gói ngôn ngữ về, rồi truyền `currentLanguage` mới xuống.

Vòng đi vòng lại này có vẻ thừa, và nó mua hai thứ cụ thể. Thứ nhất: đổi ngôn ngữ là một thao tác **có thể thất bại** — gói dịch tải hỏng thì phải giữ nguyên ngôn ngữ cũ. Nếu component tự đổi ngay khi bấm, dấu hiệu chọn đã nhảy sang ngôn ngữ mới trong khi giao diện vẫn là ngôn ngữ cũ, và không có đường quay lại sạch. Thứ hai: cùng một `LanguageSwitcher` chạy được ở cả `Topbar` (đã đăng nhập, ghi lựa chọn vào hồ sơ người dùng) lẫn `AuthCard` (chưa đăng nhập, chỉ ghi vào bộ nhớ trình duyệt). Hai nơi lưu khác nhau, một component.

## Do / Don't

- ✅ Tên ngôn ngữ viết bằng chính ngôn ngữ đó, kèm `lang` đúng.
- ✅ `aria-current="true"` cho ngôn ngữ đang dùng, kèm kênh thứ hai ngoài màu.
- ✅ Không render gì khi chỉ có một ngôn ngữ.
- ✅ Giữ nguyên ngôn ngữ cũ khi gói dịch tải hỏng.
- ❌ Không dùng cờ quốc gia.
- ❌ Không dịch tên các ngôn ngữ sang ngôn ngữ hiện hành.
- ❌ Không inject service i18n vào component này, và không tải lại cả trang để áp ngôn ngữ mới.
- ❌ Không tự chuyển `inline` ↔ `menu` theo độ dài mảng ngôn ngữ.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Biến thể `menu` cần một lớp nổi, nhưng Core **chưa có** một component menu dùng chung trong bảng [`../COMPONENTS.md`](../COMPONENTS.md) §3 — `Topbar` cũng cần một cái cho khu người dùng. Dựng menu riêng ở đây là đẻ ra lớp nổi thứ hai | Người dựng khung ứng dụng |
| 2 | Bảng tầng lồng nhau ở [`../COMPONENTS.md`](../COMPONENTS.md) §2.4 **không xếp tầng** cho `LanguageSwitcher`. Nó là control (tầng 1) hay cụm (tầng 2)? Câu trả lời quyết định nó lồng được vào đâu | Người giữ mục lục component |
| 3 | Lựa chọn ngôn ngữ trước khi đăng nhập có được mang theo sau khi đăng nhập không, hay hồ sơ người dùng ghi đè? Ghi đè thì đúng về dữ liệu nhưng gây bất ngờ ngay sau khi bấm đăng nhập | Chủ sản phẩm |
