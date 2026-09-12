---
kind: luat
scope: core
verified: chua-doi-chieu
---

# DESIGN.md — hệ thống thiết kế nền của Core

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo chưa có `src/`. Mọi giá trị dưới đây là **giá trị đã quyết**, chưa có stylesheet nào hiện thực hoá. Chiều cập nhật (`Design/` quyết, code áp) ở [`CLAUDE.md`](./CLAUDE.md) §5.

> Đây là **nơi duy nhất** trong repo chứa mã màu, số px và số rem thật. Mọi file khác — spec component, screen spec, quy ước FE — chỉ được nhắc **tên token**.
>
> Cơ chế đằng sau hệ token (vì sao ba tầng, vì sao CSS custom property, theme chuyển thế nào) nằm ở [`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md). File này khai **giá trị**; file kia giải thích **cách hoạt động**. Đừng trộn.

---

## 1. Ba tầng token, và tầng nào được dùng ở đâu

| Tầng | Ví dụ | Ai được dùng |
| --- | --- | --- |
| **Nguyên thuỷ** *(primitive)* | mã màu thô, bậc thang px | 🛑 Không ai. Chỉ tồn tại làm nguyên liệu cho tầng 2, và chỉ trong file khai token |
| **Ngữ nghĩa** *(semantic)* | `--color-text`, `--color-danger`, `--sp-4` | ✅ Mọi component. Đây là bề mặt công khai |
| **Riêng component** *(component-scoped)* | `--btn-height`, `--sidebar-w` | ✅ Chỉ component sở hữu nó. Khai trong `:root` để theme còn ghi đè được |

**Luật cứng:** một component chỉ đọc token tầng 2 và tầng 3 của chính nó. Đọc token tầng 3 của component khác là ghép cứng hai component vào nhau.

### Quy tắc màu — không có ngoại lệ

- 🛑 **Cấm hex literal** trong SCSS của component. Ép bằng cổng FE — xem [`../RULES.md`](../RULES.md) §7 F6.
- 🛑 **Cấm `rgb()` / `rgba()` literal**, trừ đúng một dạng: `rgb(var(--x) / a)`. Ép bằng [`../RULES.md`](../RULES.md) §7 F7.
- ✅ Token nào cần dùng kèm alpha thì khai **thêm** một biến kênh (`--color-overlay-rgb: 17 24 39`) rồi ghép: `rgb(var(--color-overlay-rgb) / 0.55)`.

**Vì sao phải khai biến kênh riêng:** `rgba(var(--color-x), .5)` **không chạy** — `var()` trả về chuỗi `#111827`, còn `rgba()` cần ba số. Đây là bẫy khiến người ta quay lại viết `rgba(17,24,39,.55)` và phá luật F7. Khai sẵn biến kênh là cách duy nhất giữ được cả alpha lẫn token.

Chỉ những token thật sự cần alpha mới có biến kênh đi kèm — xem §2.6. Không nhân đôi cả bảng màu.

---

## 2. Token màu

Bảng dưới là **toàn bộ** màu của Core. Cột tỉ lệ là **tương phản đã tính** theo công thức WCAG 2.2 (relative luminance), không phải ước lượng bằng mắt.

### Mục tiêu tương phản — con số phải đạt

| Loại nội dung | Chuẩn | Ngưỡng |
| --- | --- | --- |
| Chữ thường (< 18.66px thường / < 14px đậm) | WCAG 2.2 SC 1.4.3 AA | **≥ 4.5:1** |
| Chữ lớn (≥ 18.66px thường / ≥ 14px đậm) | SC 1.4.3 AA | ≥ 3:1 |
| Ranh giới component, biểu tượng mang nghĩa, vòng focus | SC 1.4.11 | **≥ 3:1** |
| Chữ bị `disabled` | — miễn trừ | không đặt ngưỡng, nhưng vẫn ghi số thật |

Core đặt mục tiêu **AA cho mọi chữ ở cả hai theme**, và ghi số đo cho từng cặp. Không nhắm AAA: đạt 7:1 cho mọi chữ phụ sẽ đẩy `--color-text-muted` gần bằng `--color-text` và giết mất phân cấp chữ — đánh đổi đã cân nhắc và chọn.

### 2.1 Nền và bề mặt

| Token | Sáng | Tối | Dùng ở đâu |
| --- | --- | --- | --- |
| `--color-bg` | `#f4f6fa` | `#0d1117` | Nền trang. Nền vùng cuộn ngoài cùng |
| `--color-surface` | `#ffffff` | `#161b22` | `Card`, `Dialog`, `Toast`, nền `Input`, thân bảng, `Sidebar`, `Topbar` |
| `--color-surface-2` | `#eef1f6` | `#1f2630` | Nền hover của hàng bảng và mục nav; nền `th`; vạch zebra; nền `Badge` trung tính |
| `--color-surface-3` | `#e2e8f0` | `#2a323e` | Máng `ProgressBar`; nền `Input` khi `disabled`; nền `SkeletonLoader` |

Tương phản bề mặt (không phải yêu cầu accessibility, nhưng là số cần biết để không dựng bề mặt vô hình):

| Cặp | Sáng | Tối |
| --- | --- | --- |
| `surface` trên `bg` | 1.08:1 | 1.09:1 |
| `surface-2` trên `surface` | 1.13:1 | 1.14:1 |
| `surface-3` trên `surface` | 1.23:1 | 1.34:1 |

**Card không được chỉ dựa vào chênh lệch nền để hiện ranh giới.** 1.08:1 là chênh lệch của một cái bóng nhẹ, không phải của một đường biên. Ranh giới của `Card` do `--color-border` (≥ 3:1) và `--shadow-1` gánh — bỏ border đi là hồi quy accessibility, không phải lựa chọn thẩm mỹ.

### 2.2 Chữ

| Token | Sáng | Tối | Dùng ở đâu | Tương phản trên `surface` |
| --- | --- | --- | --- | --- |
| `--color-text` | `#111827` | `#e6edf3` | Chữ chính, tiêu đề, giá trị ô bảng | **17.74:1** / **14.64:1** |
| `--color-text-muted` | `#4b5563` | `#9aa7b8` | Nhãn phụ, mô tả, chú thích, placeholder | **7.56:1** / **7.08:1** |
| `--color-text-disabled` | `#6b7280` | `#6e7d90` | Chữ của control `disabled` | 4.83:1 / 4.12:1 |
| `--color-text-on-brand` | `#ffffff` | `#0d1117` | Chữ và icon trên nền `--color-brand` | **6.85:1** / **7.19:1** |

Kiểm cả trên các bề mặt khác — mọi số đều qua AA:

| Cặp | Sáng | Tối |
| --- | --- | --- |
| `text` trên `bg` | 16.40:1 | 16.02:1 |
| `text` trên `surface-2` | 15.67:1 | 12.90:1 |
| `text` trên `surface-3` | 14.39:1 | 10.94:1 |
| `text-muted` trên `bg` | 6.98:1 | 7.74:1 |
| `text-muted` trên `surface-2` | 6.68:1 | 6.24:1 |

`--color-text-disabled` **cố ý không đạt 4.5:1 ở theme tối** (4.12:1). WCAG miễn trừ control vô hiệu hoá, và kéo nó lên AA sẽ khiến một nút disabled trông như một nút bấm được — trạng thái disabled sẽ mất tín hiệu. Đánh đổi này chỉ hợp lệ vì `disabled` **không bao giờ là kênh thông tin duy nhất**: xem §2.7.

### 2.3 Viền

Ba bậc viền, mỗi bậc một vai. Nhầm bậc là phá đúng tín hiệu mà bậc đó mang.

| Token | Sáng | Tối | Vai | Trên `surface` |
| --- | --- | --- | --- | --- |
| `--color-border-subtle` | `#e4e9f0` | `#2c3441` | Vạch **bên trong** một bề mặt: giữa hai hàng bảng, giữa hai mục danh sách | 1.22:1 / 1.38:1 |
| `--color-border` | `#7f8da3` | `#5a6879` | **Ranh giới một component**: `Card`, `Toolbar`, `NoticeBanner`, khung bảng | **3.36:1** / **3.04:1** |
| `--color-border-strong` | `#5f6d80` | `#7b8a9d` | *"Chỗ này gõ được"* — chỉ `Input`, `Select`, `Textarea`, ô có thể sửa | **5.27:1** / **4.91:1** |

`--color-border-subtle` **cố ý dưới 3:1**. Nó không phải ranh giới của một UI component — SC 1.4.11 không áp cho vạch trang trí bên trong một bề mặt đã có ranh giới riêng. Dùng nó làm viền `Card` là vi phạm; đó là lý do có ba bậc chứ không phải hai.

Một số phải biết trước khi đặt component lên nền xám:

| Cặp | Sáng | Tối |
| --- | --- | --- |
| `border` trên `bg` | 3.11:1 ✅ | 3.33:1 ✅ |
| `border` trên `surface-2` | **2.97:1 ❌** | **2.68:1 ❌** |
| `border-strong` trên `surface-2` | 4.65:1 ✅ | 4.33:1 ✅ |

**`border` trên `bg` là ngưỡng chặn của cả thang trung tính sáng, không phải `border` trên `surface`.** Nền trang tối hơn bề mặt, nên mọi lần làm nhạt nền trang đều kéo con số này xuống trước tiên — và `Card` thì nằm thẳng trên nền trang. 3.11:1 là phần dư còn lại sau khi đã nhạt hết mức: nhạt thêm một bậc nữa thì viền `Card` tụt xuống 2.99:1 và vi phạm SC 1.4.11. Đây là biên do phép đo đặt ra, không phải một lựa chọn thẩm mỹ còn co giãn được.

🛑 **Component có viền đặt trên `--color-surface-2` phải dùng `--color-border-strong`**, không dùng `--color-border`. Đây là ca duy nhất trong hệ mà bậc viền đổi theo nền, và nó có thật: một `Badge` viền nằm trong ô `th`.

### 2.4 Thương hiệu

| Token | Sáng | Tối | Dùng ở đâu | Tương phản |
| --- | --- | --- | --- | --- |
| `--color-brand` | `#1a56b8` | `#6ba1f0` | Nền nút primary, link, mục nav đang chọn, vòng focus | 6.85:1 / 6.57:1 trên `surface` |
| `--color-brand-hover` | `#154497` | `#8fb9f5` | Nền nút primary khi hover | 9.11:1 / 9.40:1 với `text-on-brand` |
| `--color-brand-active` | `#103a80` | `#a9c9f8` | Nền nút primary khi đang nhấn | 10.83:1 / 11.17:1 với `text-on-brand` |
| `--color-brand-subtle` | `#dbe7fb` | `#16325c` | Nền của mục nav đang chọn, chip đang chọn, `Tabs` tab hiện hành | 1.25:1 / 1.35:1 trên `surface` |
| `--color-brand-on-subtle` | `#144393` | `#b9d3f8` | Chữ/icon **trên** `--color-brand-subtle` | **7.47:1** / **8.35:1** |

**`--color-brand-subtle` chênh với `surface` chỉ 1.25:1 — nó KHÔNG được là tín hiệu duy nhất** của trạng thái "đang chọn". Mục nav đang chọn phải kèm một dấu hiệu đạt 3:1: một dải `--color-brand` 3px ở cạnh trái, hoặc chữ đổi sang `--color-brand-on-subtle` (7.47:1). Xem [`Components/Sidebar.md`](./Components/Sidebar.md).

Ở theme tối, `--color-brand` **sáng lên** thay vì tối đi, và `--color-text-on-brand` lật thành màu nền tối. Đây là lần lật vai duy nhất trong hệ — xem §8.

### 2.5 Trạng thái

Bốn vai: thành công, cảnh báo, lỗi, thông tin. Mỗi vai ba token: chữ/icon, nền nhạt, viền.

**Theme sáng**

| Vai | `*-fg` | `*-bg` | `*-border` | fg trên surface | fg trên bg riêng | border trên surface |
| --- | --- | --- | --- | --- | --- | --- |
| Thành công | `#0f6b46` | `#d7f0e4` | `#3f8f6b` | **6.54:1** | **5.44:1** | **3.92:1** |
| Cảnh báo | `#8a5300` | `#fbeccd` | `#9c7020` | **6.33:1** | **5.42:1** | **4.42:1** |
| Lỗi | `#b02318` | `#fbdfdc` | `#bd6156` | **6.78:1** | **5.38:1** | **4.19:1** |
| Thông tin | `#0e5ba6` | `#dcebfa` | `#5589bf` | **6.85:1** | **5.65:1** | **3.67:1** |

**Theme tối**

| Vai | `*-fg` | `*-bg` | `*-border` | fg trên surface | fg trên bg riêng | border trên surface |
| --- | --- | --- | --- | --- | --- | --- |
| Thành công | `#54c79b` | `#10301f` | `#3d8a67` | **8.25:1** | **6.84:1** | **4.14:1** |
| Cảnh báo | `#e3ad4a` | `#3a2a0c` | `#8f6c24` | **8.52:1** | **6.82:1** | **3.57:1** |
| Lỗi | `#f08a80` | `#3d1a17` | `#b45a4f` | **7.13:1** | **6.37:1** | **3.72:1** |
| Thông tin | `#75b0ee` | `#12304f` | `#3d6f9f` | **7.58:1** | **5.89:1** | **3.27:1** |

Tên token đầy đủ — khai hết ra chứ không viết tắt bằng dấu sao, để mọi spec trích dẫn được một cái tên có thật:

| Vai | Chữ/icon | Nền nhạt | Viền |
| --- | --- | --- | --- |
| Thành công | `--color-success` | `--color-success-bg` | `--color-success-border` |
| Cảnh báo | `--color-warning` | `--color-warning-bg` | `--color-warning-border` |
| Lỗi | `--color-danger` | `--color-danger-bg` | `--color-danger-border` |
| Thông tin | `--color-info` | `--color-info-bg` | `--color-info-border` |

Khi cần **nền đặc** (nút danger, badge đặc), chữ dùng `--color-text-on-brand`:

| Nền đặc | Sáng | Tối |
| --- | --- | --- |
| trên `--color-danger` | 6.78:1 | 7.80:1 |
| trên `--color-success` | 6.54:1 | 9.03:1 |
| trên `--color-warning` | 6.33:1 | 9.32:1 |

Chữ `--color-text` đặt trên các nền nhạt `*-bg` cũng luôn qua AA (thấp nhất 14.10:1 ở sáng, 11.38:1 ở tối), nên một `NoticeBanner` được phép để phần thân dùng `--color-text` và chỉ tiêu đề dùng màu vai.

#### Bậc hover và active của `danger`

`--color-brand` có ba bậc (`hover`, `active`); `--color-danger` cho tới nay chỉ có một. Nhưng [`Components/Button.md`](./Components/Button.md) và [`Components/IconButton.md`](./Components/IconButton.md) đều khai trạng thái `hover` và `active` cho biến thể `danger` — tức có hai spec trỏ vào những token chưa tồn tại. Khai ra ở đây:

| Token | Sáng | Tối | Dùng ở đâu | Tương phản với `--color-text-on-brand` |
| --- | --- | --- | --- | --- |
| `--color-danger-hover` | `#961e14` | `#f4a49c` | Nền nút `danger` khi hover | **8.42:1** / **9.59:1** |
| `--color-danger-active` | `#7d1910` | `#f8bdb6` | Nền nút `danger` khi đang nhấn | **10.40:1** / **11.67:1** |

Hai bậc này đi **cùng chiều** với thang `brand`: ở theme sáng thì tối dần, ở theme tối thì sáng dần. Đây là hệ quả trực tiếp của lần lật vai đã ghi ở §8 — không phải một quy ước riêng cho `danger`.

🛑 **Ba vai trạng thái còn lại — `success`, `warning`, `info` — cố ý KHÔNG có bậc hover.** Chúng không bao giờ làm nền cho một nút bấm được: `Badge` không tương tác, `NoticeBanner` và `Toast` chỉ dùng chúng làm nền tĩnh. Thêm bậc hover cho chúng là mở đường cho một cái nút màu xanh lá, và từ đó màu trạng thái mất nghĩa.

### 2.6 Lớp phủ và focus

| Token | Sáng | Tối | Dùng ở đâu |
| --- | --- | --- | --- |
| `--color-overlay-rgb` | `17 24 39` | `2 6 12` | Biến kênh — chỉ để ghép alpha, không dùng trực tiếp |
| `--color-overlay` | `rgb(var(--color-overlay-rgb) / 0.55)` | `rgb(var(--color-overlay-rgb) / 0.70)` | Backdrop của `Dialog`, `ConfirmDialog`, drawer `Sidebar` |
| `--color-focus` | `#1a56b8` | `#8fb9f5` | Vòng focus của **mọi** control |
| `--color-scrim` | `rgb(var(--color-overlay-rgb) / 0.08)` | `rgb(var(--color-overlay-rgb) / 0.24)` | Lớp mờ phủ nội dung khi đang `loading` |

Alpha ở theme tối cao hơn (0.70 so với 0.55) vì backdrop tối trên nền vốn đã tối cần đậm hơn mới tách được lớp dialog.

**Vòng focus và cái bẫy của nó.** `--color-focus` ở theme sáng **trùng giá trị** với `--color-brand` — nghĩa là vòng focus vẽ sát nền một nút primary sẽ có tương phản **1.00:1**, tức vô hình. Đây là lỗi thường gặp và hệ này chặn nó bằng hình học, không bằng màu:

```text
outline: 2px solid var(--color-focus);
outline-offset: 2px;
```

`outline-offset: 2px` đẩy vòng ra khỏi mép nút, nên nó luôn nằm trên `--color-surface` hoặc `--color-bg` — nơi tương phản là **6.85:1** (sáng) và **8.59:1** (tối). Khoảng hở 2px chính là thứ làm vòng focus hợp lệ.

🛑 **Cấm `outline: none` mà không thay bằng dấu hiệu focus khác.** Đây là lỗi accessibility bị bắt nhiều nhất trong mọi audit; nếu cần vòng bám sát hình dạng bo góc, dùng `box-shadow: 0 0 0 2px var(--color-surface), 0 0 0 4px var(--color-focus)` — vẫn có khoảng hở, vẫn đạt tỉ lệ.

Focus chỉ vẽ ở `:focus-visible`, không ở `:focus`. Chuột bấm thì không hiện vòng; bàn phím thì hiện.

#### Bề mặt đảo — chỉ cho `Tooltip`

| Token | Sáng | Tối | Dùng ở đâu |
| --- | --- | --- | --- |
| `--color-inverse-surface` | `#1f2937` | `#e6edf3` | Nền [`Components/Tooltip.md`](./Components/Tooltip.md) |
| `--color-inverse-text` | `#f9fafb` | `#0d1117` | Chữ trên nền đó |

Tương phản chữ trên nền đảo: **14.05:1** sáng / **16.02:1** tối. Bản thân nền đảo tách khỏi `--color-surface` ở mức **14.68:1** / **14.64:1**, nên tooltip không cần viền.

**Vì sao tooltip đảo màu thay vì dùng `--color-surface` như mọi lớp nổi khác:** `Menu` và `Dialog` là bề mặt người dùng **thao tác trên đó**, nên chúng phải trông như một mặt giấy mới đặt lên. Tooltip thì không bấm được và biến mất ngay — nó là một lời chú, và đảo màu là cách nói điều đó mà không cần thêm một đường viền nữa vào màn hình. Đây là **bề mặt duy nhất** trong hệ được phép đảo; đừng dùng cặp token này cho thứ gì khác.

### 2.7 Màu không bao giờ là kênh thông tin duy nhất

WCAG 2.2 SC 1.4.1. Áp cho toàn Core, không có ngoại lệ:

| Chỗ | Kênh thứ hai bắt buộc |
| --- | --- |
| `Badge` trạng thái | Chữ nhãn ("Hoạt động" / "Khoá"), không chỉ chấm màu |
| Trường form lỗi | Icon + dòng thông báo lỗi dưới trường, không chỉ viền đỏ |
| `NoticeBanner`, `Toast` | Icon theo vai + tiền tố chữ |
| Hàng bảng bị nhấn mạnh | Icon hoặc `Badge` trong ô, không chỉ nền màu |
| Control `disabled` | `aria-disabled` hoặc `disabled` thật + con trỏ `not-allowed` |

### 2.8 Bảng màu biểu đồ

Trước đây §10 để ngỏ câu hỏi *"có cần bảng màu biểu đồ không"*. Nay đã chốt là **có**, và đây là bảng đó. Mọi giá trị dưới đây chạy qua phép kiểm bằng máy ở cả hai theme — số đo ghi kèm, không ước lượng bằng mắt.

**Tám màu, thứ tự cố định, gán lần lượt và không bao giờ quay vòng.** Thứ tự chính là cơ chế an toàn cho người mù màu: các cặp **kề nhau** trong thứ tự này là các cặp thật sự chạm nhau trên biểu đồ, và chúng đã được đo. Đổi thứ tự là phá cơ chế đó.

| Slot | Vai | Sáng | Tối |
| --- | --- | --- | --- |
| 1 | Xanh thương hiệu | `#1a56b8` | `#3987e5` |
| 2 | Cam | `#eb6834` | `#d95926` |
| 3 | Ngọc | `#1baf7a` | `#199e70` |
| 4 | Vàng | `#eda100` | `#c98500` |
| 5 | Hồng sen | `#e87ba4` | `#d55181` |
| 6 | Lục | `#008300` | `#008300` |
| 7 | Tím | `#4a3aa7` | `#9085e9` |
| 8 | Đỏ | `#e34948` | `#e66767` |

Tên token: `--chart-1` … `--chart-8`.

Số đo, đo trên `--color-surface` của từng theme:

| Phép kiểm | Sáng | Tối |
| --- | --- | --- |
| Dải độ sáng, sàn độ bão hoà | 8/8 đạt | 8/8 đạt |
| Tách màu cho người mù màu, cặp kề nhau *(ngưỡng ≥ 8)* | **9.1** — vàng ↔ ngọc | **8.4** — vàng ↔ ngọc |
| Sàn thị lực thường, cặp kề nhau *(ngưỡng cứng ≥ 15)* | **19.6** — hồng ↔ vàng | **19.3** — hồng ↔ vàng |
| Tương phản với bề mặt | ngọc 2.82 · vàng 2.17 · hồng 2.69 — **dưới 3:1** | 8/8 đạt ≥ 3:1 |

Ba ràng buộc cứng đi kèm bảng này:

1. 🛑 **Màu thương hiệu ở theme tối KHÔNG dùng làm màu chuỗi được.** `--color-brand` tối là `#6ba1f0`, có độ sáng 0,705 — vượt khỏi dải cho phép của một mark trên nền tối (0,48–0,67), tức quá nhạt để đứng làm một chuỗi dữ liệu. Vì vậy `--chart-1` ở theme tối là `#3987e5`, **hạ một bậc** so với màu thương hiệu. Hai token này cố ý khác nhau; đừng "sửa" cho chúng bằng nhau.
2. 🛑 **Ba màu ở theme sáng dưới ngưỡng 3:1 thì bắt buộc có kênh bù.** Biểu đồ nào dùng ngọc, vàng hoặc hồng phải kèm **nhãn số hiện rõ** hoặc **bảng số liệu xem được**. Đây là ràng buộc, không phải khuyến nghị.
3. 🛑 **Biểu đồ dạng phân tán chỉ được dùng ba slot đầu.** Ở biểu đồ mà hai điểm bất kỳ có thể nằm cạnh nhau — phân tán, bong bóng, bản đồ — cả tám màu không thể cùng phân biệt được. Ba slot đầu đã kiểm ở chế độ khó nhất và đạt (9.2 sáng / 9.4 tối). Quá ba thì gom phần còn lại thành *"Khác"* hoặc tách thành nhiều biểu đồ nhỏ — **không phải đổi bảng màu**.

#### Dải mức độ và dải hai chiều

**Mức độ** *(một đại lượng, ít → nhiều)*: một màu duy nhất đậm dần, token `--chart-seq-1` … `--chart-seq-5`.

| Bậc | Sáng | Tối |
| --- | --- | --- |
| 1 *(ít nhất)* | `#cde2fb` | `#104281` |
| 2 | `#9ec5f4` | `#1c5cab` |
| 3 | `#5598e7` | `#2a78d6` |
| 4 | `#2a78d6` | `#5598e7` |
| 5 *(nhiều nhất)* | `#184f95` | `#9ec5f4` |

Ở theme tối dải **đảo chiều**: bậc nhạt nhất là bậc tối nhất. Lý do đơn giản — "gần nền" mới là "gần bằng không", và nền ở theme tối là màu tối.

**Hai chiều** *(chênh lệch quanh một mốc)*: hai màu đối nhau, điểm giữa trung tính.

| Token | Sáng | Tối | Nghĩa |
| --- | --- | --- | --- |
| `--chart-div-neg` | `#1a56b8` | `#3987e5` | Dưới mốc |
| `--chart-div-mid` | `#dbe1ea` | `#2c3441` | Đúng mốc |
| `--chart-div-pos` | `#e34948` | `#e66767` | Vượt mốc |

🛑 **Không bao giờ dùng dải cầu vồng cho mức độ, và không bao giờ đặt một màu rực ở điểm giữa của dải hai chiều.** Mắt đọc ranh giới màu thành ranh giới dữ liệu ở những chỗ không có ranh giới nào.

#### Màu trạng thái không được dùng làm màu chuỗi

Bốn vai ở §2.5 là màu **dành riêng**. Một biểu đồ có chuỗi là *trạng thái* (đã duyệt / chờ duyệt / trả lại) thì dùng bảng trạng thái; một biểu đồ có chuỗi là *định danh* (phòng ban, khoản mục) thì dùng bảng này. Trộn hai bảng là cách làm màu xanh lá có lúc nghĩa là "đạt" và có lúc chỉ nghĩa là "Phòng Kế toán" — và từ đó người đọc mất khả năng suy từ màu.

> 📖 Cách vẽ, quy cách mark, luật nhãn và chú giải: [`Components/Chart.md`](./Components/Chart.md). File này chỉ khai **giá trị**.

---

## 3. Chữ

### 3.1 Họ font

| Token | Giá trị |
| --- | --- |
| `--font-sans` | `'Be Vietnam Pro', 'Segoe UI', Roboto, Arial, sans-serif` |
| `--font-mono` | `'JetBrains Mono', 'Cascadia Mono', Consolas, 'Courier New', monospace` |

**Vì sao `Be Vietnam Pro`:** giao diện của Core hiển thị tiếng Việt, và dấu tiếng Việt là chỗ nhiều font phương Tây gãy — dấu bị cắt, dấu chồng lên chữ hoa, dấu ngã và dấu hỏi khó phân biệt ở cỡ nhỏ. `Be Vietnam Pro` được thiết kế cho tiếng Việt và có đủ các nét 400/500/600/700.

**Font dự phòng:** `Segoe UI` (Windows) và `Roboto` (Android/ChromeOS) là hai font hệ thống có dấu tiếng Việt đầy đủ. `Arial` là chốt chặn cuối. Không để `sans-serif` đứng một mình ở vị trí thứ hai — trên vài môi trường nó rơi vào một font không có dấu.

**Chỉ nạp bốn nét: 400, 500, 600, 700.** Mọi khai báo `font-weight` phải là một trong bốn số này. Viết `750` hay `850` trên một font tĩnh sẽ bị trình duyệt làm tròn lên nét gần nhất — hai chỗ tưởng khác nhau sẽ render **giống hệt nhau**, và không có cảnh báo nào. Đây là bẫy đã xảy ra thật ở dự án tiền nhiệm.

Font **tự phục vụ** (self-host), không lấy từ CDN ngoài: lý do là quyền riêng tư và độ ổn định khi mạng nội bộ chặn ngoài. Đường dẫn file font là quyết định thi công, thuộc [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md).

### 3.2 Thang cỡ chữ

Gốc `html` là 16px. Token khai bằng `rem` để người dùng phóng chữ hệ thống vẫn có tác dụng — khai bằng `px` là chặn đứng một tính năng accessibility của trình duyệt.

| Token | rem | ≈ px | Dùng ở đâu |
| --- | --- | --- | --- |
| `--fs-2xs` | `0.6875rem` | 11px | Nhãn `Badge`, chú thích chân bảng, `Footer` |
| `--fs-xs` | `0.75rem` | 12px | Nhãn cột `th`, chữ phụ, `helper text` |
| `--fs-sm` | `0.8125rem` | 13px | Ô bảng, nhãn nút, nhãn trường form |
| `--fs-md` | `0.875rem` | 14px | **Chữ thân mặc định.** Đặt trên `body` |
| `--fs-lg` | `1rem` | 16px | Tiêu đề `Card`, tiêu đề `Dialog` |
| `--fs-xl` | `1.125rem` | 18px | Tiêu đề màn hình (`PageHeader`) |
| `--fs-2xl` | `1.375rem` | 22px | Tiêu đề `AuthCard` |
| `--fs-3xl` | `1.75rem` | 28px | Số liệu lớn, trang lỗi 404/500 |

Thang này **cố ý đặc** ở khoảng 11–16px: Core là bộ khung cho ứng dụng quản trị, nơi một màn hình phải chứa nhiều dữ liệu. Cái giá phải trả: 11px là nhỏ, nên `--fs-2xs` **chỉ dùng cho nhãn ngắn**, không bao giờ cho một câu.

🛑 **Không có cỡ chữ nào ngoài thang.** Cần một cỡ mới → thêm một bậc vào đây trước, kèm lý do.

### 3.3 Cân nặng, chiều cao dòng, giãn chữ

| Token | Giá trị | Dùng ở đâu |
| --- | --- | --- |
| `--fw-regular` | `400` | Chữ thân, ô bảng, mô tả |
| `--fw-medium` | `500` | Nhãn trường form, mục nav |
| `--fw-semibold` | `600` | Nhãn nút, `th`, tiêu đề `Card` |
| `--fw-bold` | `700` | Tiêu đề màn hình, tiêu đề `Dialog`, `Badge` |

| Token | Giá trị | Dùng ở đâu |
| --- | --- | --- |
| `--lh-tight` | `1.25` | Tiêu đề, số liệu lớn |
| `--lh-snug` | `1.4` | Nhãn nút, ô bảng, mục nav — chỗ chữ chỉ một dòng |
| `--lh-normal` | `1.55` | Chữ thân, mô tả, nội dung đọc |
| `--lh-loose` | `1.75` | Đoạn văn dài trong `Dialog`, trang trợ giúp |

| Token | Giá trị | Dùng ở đâu |
| --- | --- | --- |
| `--ls-tight` | `-0.01em` | Tiêu đề từ `--fs-xl` trở lên |
| `--ls-normal` | `0` | Mặc định |
| `--ls-wide` | `0.04em` | Nhãn `Badge` và nhãn cột viết hoa toàn phần |

**Chiều cao dòng của chữ thân là 1.55, không phải 1.2.** WCAG 2.2 SC 1.4.12 yêu cầu giao diện không vỡ khi người dùng đẩy line-height lên 1.5 — bắt đầu từ 1.55 nghĩa là đã ở phía an toàn sẵn, và văn bản tiếng Việt có dấu cần khoảng thở dọc hơn tiếng Anh.

---

## 4. Khoảng cách

Thang bội số **4px**, khai bằng `rem`.

| Token | rem | ≈ px | Dùng ở đâu |
| --- | --- | --- | --- |
| `--sp-0` | `0` | 0 | Xoá khoảng cách kế thừa |
| `--sp-1` | `0.125rem` | 2px | Khe giữa icon và chữ trong `Badge` |
| `--sp-2` | `0.25rem` | 4px | Khe nhỏ nhất giữa hai phần tử cùng nhóm |
| `--sp-3` | `0.375rem` | 6px | Đệm dọc của nút và ô bảng |
| `--sp-4` | `0.5rem` | 8px | Đệm ngang của nút; khe giữa các control trong `Toolbar` |
| `--sp-5` | `0.75rem` | 12px | Khe giữa nhãn và ô nhập; đệm của `Toolbar` |
| `--sp-6` | `1rem` | 16px | **Đệm mặc định của `Card` và `Dialog`**; khe giữa hai trường form |
| `--sp-7` | `1.25rem` | 20px | Khe giữa hai nhóm trong một form |
| `--sp-8` | `1.5rem` | 24px | Khe giữa hai `Card`; đệm của `main` |
| `--sp-9` | `2rem` | 32px | Khe giữa hai vùng lớn của trang |
| `--sp-10` | `2.5rem` | 40px | Đệm dọc của `EmptyState` |
| `--sp-11` | `3rem` | 48px | Đệm của `AuthCard` |
| `--sp-12` | `4rem` | 64px | Khe trên/dưới trang lỗi toàn màn hình |

Quy tắc dùng:

1. **Mọi `margin`, `padding`, `gap` lấy từ thang.** Không có `padding: 7px`. Cần một giá trị không có trong thang → gần như luôn là dấu hiệu bố cục đang sai, không phải dấu hiệu thang thiếu bậc.
2. **Khoảng cách theo nhóm, không theo cặp.** Hai thứ cùng nhóm cách nhau `--sp-2`/`--sp-3`; hai nhóm khác nhau cách nhau `--sp-6` trở lên. Khoảng cách chính là thứ tạo ra cảm giác "cùng một nhóm".
3. **Ưu tiên `gap` của flex/grid hơn `margin`.** `margin` gộp (collapse) và tràn ra ngoài container; `gap` thì không.
4. **Chiều dọc dùng bậc chẵn hơn.** Nhịp dọc của một trang nên đi theo `--sp-6` / `--sp-8`; các bậc nhỏ dành cho bên trong một control.

---

## 5. Bo góc, đổ bóng, viền

### 5.1 Bo góc

| Token | Giá trị | Dùng ở đâu |
| --- | --- | --- |
| `--radius-xs` | `4px` | `Check`, ô màu nhỏ, chấm trạng thái vuông |
| `--radius-sm` | `6px` | `Button`, `IconButton`, `Input`, `SegmentedControl`, `Badge` chữ nhật |
| `--radius-md` | `8px` | `Toolbar`, `NoticeBanner`, `Toast`, mục nav `Sidebar` |
| `--radius-lg` | `12px` | `Card`, khung bảng, `Dialog` |
| `--radius-xl` | `16px` | `AuthCard`, `EmptyState` |
| `--radius-pill` | `999px` | `Badge` dạng viên, `Avatar` tròn, chip lọc |
| `--radius-full` | `50%` | Chỉ hình tròn thật: `Avatar`, chấm trạng thái |

Bo góc dùng `px` chứ không `rem` — có chủ đích. Bo góc là đặc tính hình học của khung, không phải của chữ; scale nó theo cỡ chữ hệ thống làm nút to lên thì góc bo cũng phình, trông sai.

### 5.2 Đổ bóng

Bóng ở theme tối **yếu hơn nhiều** và được bù bằng bề mặt sáng dần — trên nền tối, bóng đen gần như không nhìn thấy.

| Token | Sáng | Tối | Dùng ở đâu |
| --- | --- | --- | --- |
| `--shadow-0` | `none` | `none` | Mặc định. Phần lớn bề mặt không cần bóng |
| `--shadow-1` | `0 1px 2px rgb(var(--color-overlay-rgb) / 0.06)` | `0 1px 2px rgb(var(--color-overlay-rgb) / 0.40)` | `Card`, `Toolbar` |
| `--shadow-2` | `0 2px 8px rgb(var(--color-overlay-rgb) / 0.10)` | `0 2px 8px rgb(var(--color-overlay-rgb) / 0.50)` | Nút khi hover, `Topbar` khi trang đã cuộn |
| `--shadow-3` | `0 8px 24px rgb(var(--color-overlay-rgb) / 0.14)` | `0 8px 24px rgb(var(--color-overlay-rgb) / 0.60)` | Menu thả xuống, popover, `Sidebar` dạng drawer |
| `--shadow-4` | `0 16px 48px rgb(var(--color-overlay-rgb) / 0.20)` | `0 16px 48px rgb(var(--color-overlay-rgb) / 0.70)` | `Dialog`, `Toast` |

🛑 **Bóng không bao giờ là ranh giới.** Một `Card` phải có `--color-border`; bóng chỉ thêm chiều sâu. Người dùng chế độ tương phản cao và nhiều màn hình rẻ sẽ không thấy bóng nào cả.

### 5.3 Độ dày viền

| Token | Giá trị | Dùng ở đâu |
| --- | --- | --- |
| `--border-w` | `1px` | Mặc định cho mọi viền |
| `--border-w-strong` | `2px` | Viền `Input` khi lỗi; dải cạnh trái của `NoticeBanner`; vòng focus |
| `--border-w-accent` | `3px` | Dải cạnh trái của mục nav đang chọn |

---

## 6. Lưới và điểm ngắt

### 6.1 Kích thước khung

| Token | Giá trị | Ý nghĩa |
| --- | --- | --- |
| `--layout-sidebar-w` | `240px` | Bề rộng `Sidebar` mở |
| `--layout-sidebar-w-collapsed` | `64px` | Bề rộng `Sidebar` thu gọn (chỉ icon) |
| `--layout-topbar-h` | `56px` | Chiều cao `Topbar` |
| `--layout-container-max` | `1440px` | Bề rộng tối đa của `main` |
| `--layout-page-pad` | `var(--sp-8)` | Đệm quanh `main` ở desktop |
| `--layout-page-pad-sm` | `var(--sp-5)` | Đệm quanh `main` dưới `--bp-md` |
| `--tree-indent` | `20px` | Thụt lề **một cấp** trong lưới cha–con và trong ô chọn dạng cây |

`--tree-indent` khai bằng `px` chứ không `rem`, cùng lý do với bo góc ở §5.1: nó là hình học của khung, không phải của chữ. Giá trị 20px là kết quả của một biên hai đầu — dưới 16px thì cấp bậc không nhìn ra, trên 24px thì tới cấp bốn là nhãn hết chỗ trong cột. Dùng ở [`Components/DataTable.md`](./Components/DataTable.md) biến thể `tree` và [`Components/TreeSelect.md`](./Components/TreeSelect.md).

### 6.2 Chiều cao control

Ba cỡ, dùng thống nhất cho `Button`, `Input`, `SegmentedControl`, ô chọn.

| Token | Giá trị | Khi nào dùng |
| --- | --- | --- |
| `--size-control-sm` | `28px` | Nút trong hàng bảng, chip lọc |
| `--size-control-md` | `34px` | **Mặc định** — form, toolbar, dialog |
| `--size-control-lg` | `42px` | Nút submit toàn chiều rộng, ô nhập trên `AuthCard` |

**Vùng chạm tối thiểu là 24×24px** (WCAG 2.2 SC 2.5.8, mức AA). `--size-control-sm` = 28px nên đã qua. Với `IconButton` cỡ `sm`, phần **có thể bấm** phải đủ 28×28px kể cả khi icon chỉ 16px — nới bằng `padding`, không bằng `margin`.

#### Cỡ icon — định nghĩa gốc

| Token | Giá trị | Dùng ở đâu |
| --- | --- | --- |
| `--icon-sm` | `14px` | Icon trong `Badge`, trong chip, trong nút cỡ `sm` |
| `--icon-md` | `16px` | **Mặc định** — trong nút, trong ô nhập, mục nav, ô bảng |
| `--icon-lg` | `20px` | `Topbar`, tiêu đề `Card`, icon vai của `NoticeBanner` và `Toast` |
| `--icon-xl` | `24px` | Icon mở đầu `Dialog`, icon của `EmptyState` |

Icon dùng `px`, không dùng `rem` — nó là hình, không phải chữ. Cho nó phóng theo cỡ chữ hệ thống là làm nó vỡ khỏi khung nút.

> Bốn giá trị này **trước đây sống ở** [`Icons.md`](./Icons.md) §3, trong khi file bạn đang đọc tự khai là nơi duy nhất chứa số px thật — tức có hai nơi giữ cùng một loại nội dung. Nay giá trị ở đây, và `Icons.md` §3 trỏ về. Quy tắc *chọn icon nào cho việc gì* và *icon hành xử ra sao với trình đọc màn hình* vẫn thuộc `Icons.md`; chỉ con số chuyển đi.

### 6.3 Điểm ngắt

| Token | Giá trị | Thiết bị điển hình |
| --- | --- | --- |
| `--bp-xs` | `480px` | Điện thoại dọc |
| `--bp-sm` | `640px` | Điện thoại ngang |
| `--bp-md` | `900px` | Máy tính bảng |
| `--bp-lg` | `1200px` | Laptop |
| `--bp-xl` | `1600px` | Màn hình rộng |

Ba mốc quyết định hình dạng khung:

| Ngưỡng | Điều gì xảy ra |
| --- | --- |
| ≥ `--bp-lg` | `Sidebar` cố định bên trái, `main` lệch một khoảng bằng bề rộng sidebar |
| `--bp-md` … `--bp-lg` | `Sidebar` mặc định thu gọn còn dải icon; hiện nhãn khi hover |
| < `--bp-md` | `Sidebar` thành drawer trượt trên backdrop; `Topbar` mọc nút hamburger; lưới form về một cột; `Toolbar` xuống dòng |
| < `--bp-xs` | Nút trong `Toolbar` giãn full width; bảng chuyển sang dạng thẻ hoặc cuộn ngang có cột đầu ghim |

**Viết media query theo hướng mobile-first** — `min-width` là mặc định. Dùng `max-width` chỉ khi thật sự cần gỡ một hành vi chỉ có ở desktop.

### 6.4 Lớp xếp chồng

Khai một chỗ. Số rời rạc trong component là cách một hệ có `z-index: 99999`.

| Token | Giá trị | Cho ai |
| --- | --- | --- |
| `--z-base` | `0` | Nội dung thường |
| `--z-sticky` | `100` | `th` ghim, cột ghim của bảng |
| `--z-topbar` | `200` | `Topbar` |
| `--z-sidebar` | `300` | `Sidebar` |
| `--z-backdrop` | `400` | Backdrop của dialog/drawer |
| `--z-dialog` | `500` | `Dialog`, `ConfirmDialog` |
| `--z-popover` | `600` | Menu thả xuống, tooltip |
| `--z-toast` | `700` | `Toast` — luôn trên cùng |

---

## 7. Chuyển động

| Token | Giá trị | Dùng ở đâu |
| --- | --- | --- |
| `--dur-instant` | `0ms` | Thay đổi phải tức thì |
| `--dur-fast` | `120ms` | Đổi màu khi hover/focus, hiện/ẩn tooltip |
| `--dur-base` | `200ms` | Mở/đóng `Dialog`, trượt drawer, chuyển tab |
| `--dur-slow` | `320ms` | `Toast` bay vào, `Sidebar` thu/mở |
| `--ease-standard` | `cubic-bezier(0.2, 0, 0, 1)` | Mặc định cho mọi chuyển tiếp |
| `--ease-decelerate` | `cubic-bezier(0, 0, 0, 1)` | Phần tử **đi vào** màn hình |
| `--ease-accelerate` | `cubic-bezier(0.3, 0, 1, 1)` | Phần tử **rời khỏi** màn hình |

### Khi nào KHÔNG animate

| Tình huống | Vì sao không |
| --- | --- |
| Người dùng khai `prefers-reduced-motion: reduce` | Bắt buộc — WCAG 2.2 SC 2.3.3. Xem khối dưới |
| Nội dung bảng đổi sau khi lọc/sắp xếp | Hàng nhấp nháy làm mắt mất chỗ đang đọc. Đổi ngay, không transition |
| Thông báo lỗi xuất hiện | Lỗi phải thấy ngay. Fade 300ms là 300ms người dùng không biết mình vừa sai |
| Con số đang cập nhật liên tục | Đếm dần trông đẹp nhưng khiến giá trị không đọc được lúc đang chạy |
| Bất cứ thứ gì lặp vô hạn ngoài chỉ báo loading | Chuyển động lặp là tác nhân gây khó chịu và mất tập trung |

```text
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }
}
```

Dùng `0.01ms` chứ không `0s` là có lý do: nhiều component dựa vào sự kiện `transitionend`/`animationend` để dọn dẹp trạng thái. `0s` khiến sự kiện không bao giờ bắn và component kẹt; `0.01ms` vẫn bắn, vẫn là tức thì với mắt người.

Chỉ animate `transform` và `opacity`. Animate `width`, `height`, `top`, `left` bắt trình duyệt tính lại bố cục mỗi khung hình.

---

## 8. Chế độ sáng và tối

### Cơ chế

Theme chọn bằng thuộc tính `data-theme` trên `<html>`, ba giá trị: `light`, `dark`, và **không có thuộc tính** (= theo hệ điều hành).

🛑 **Mặc định của ứng dụng là `light`, KHÔNG phải "theo hệ điều hành".** Lần đầu một người mở ứng dụng mà chưa từng chọn gì, họ nhận theme sáng — ứng dụng **không** đọc `prefers-color-scheme` để quyết hộ.

Lý do, và cái giá:

- Đây là ứng dụng quản trị dùng suốt ngày làm việc trong phòng sáng, cạnh giấy tờ và màn hình khác cũng nền sáng. Nền tối ở bối cảnh đó là lựa chọn của một số người, không phải mặc định hợp lý cho mọi người.
- Rất nhiều máy để mặc định hệ điều hành ở chế độ tối mà người dùng không chủ động chọn, nhất là máy mới. Đọc cờ đó ra rồi kết luận *"người này muốn nền tối"* là suy diễn từ một tín hiệu yếu.
- **Cái giá phải trả, nói thẳng:** người thật sự cần nền tối — vì mắt, vì môi trường làm việc thiếu sáng — phải tự vào đổi một lần. Chấp nhận được **chỉ vì** nút đổi theme nằm ngay trên `Topbar`, không giấu trong trang cấu hình, và lựa chọn được nhớ lại ở lần sau.

Giá trị "theo hệ điều hành" vẫn còn trong cơ chế và người dùng chọn được; nó chỉ không còn là điểm xuất phát.

```text
:root                             { /* toàn bộ token, giá trị SÁNG */ }
:root[data-theme="dark"]          { /* chỉ token đổi giá trị */ }
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) { /* cùng khối token tối */ }
}
```

Ba điều bắt buộc trong khuôn này:

1. **Mọi token có định nghĩa đầy đủ ở `:root` trần.** Một màu chỉ được khai bên trong media query hay bên trong `[data-theme]` sẽ trống rỗng ở nhánh còn lại — lỗi âm thầm, không cảnh báo.
2. **Khối `[data-theme="dark"]` phải đứng sau và tách khỏi media query**, để lựa chọn tường minh của người dùng thắng cả hai chiều: người dùng chọn sáng trên máy đang ở chế độ tối vẫn ra sáng.
3. **`color-scheme: light dark` khai trên `:root`**, để thanh cuộn, ô chọn ngày và control mặc định của trình duyệt cũng đổi theo. Thiếu dòng này sẽ có một thanh cuộn trắng chói giữa giao diện tối.

Lựa chọn của người dùng lưu ở `localStorage` và **áp trước khi trang vẽ lần đầu**, nếu không sẽ có một nháy sáng trước khi theme tối kịp áp. Cách áp là quyết định thi công — [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md).

### Token nào đổi, token nào không

| Nhóm | Đổi theo theme? |
| --- | --- |
| Toàn bộ `--color-*` | ✅ Đổi |
| `--shadow-*` | ✅ Đổi (alpha đậm hơn ở tối) |
| `--fs-*`, `--fw-*`, `--lh-*`, `--ls-*`, `--font-*` | 🛑 Không |
| `--sp-*` | 🛑 Không |
| `--radius-*`, `--border-w-*` | 🛑 Không |
| `--layout-*`, `--size-*`, `--bp-*` | 🛑 Không |
| `--z-*` | 🛑 Không |
| `--dur-*`, `--ease-*` | 🛑 Không |

Nói cách khác: **theme chỉ đổi màu và bóng.** Hình học giữ nguyên. Nhờ vậy chuyển theme không làm bố cục nhảy, và một ảnh chụp màn hình ở theme này vẫn đo được kích thước cho theme kia.

### Ba chỗ lật vai giữa hai theme

Đây là chỗ dịch thẳng "màu sáng → màu tối" sẽ sai:

1. **`--color-brand` sáng lên ở theme tối** (`#1a56b8` → `#6ba1f0`). Màu thương hiệu tối đặt trên nền tối là không đọc được. Kèm theo đó `--color-text-on-brand` lật từ trắng sang `#0d1117`.
2. **`--color-border-strong` sáng hơn `--color-border` ở theme tối** — ở theme sáng thì ngược lại. Vai không đổi ("đậm hơn = quan trọng hơn"), nhưng chiều của "đậm" thì đảo.
3. **`--color-*-bg` của trạng thái không phải là bản làm tối của màu sáng.** Chúng là màu riêng, chọn để chữ trạng thái đặt lên vẫn ≥ 5.38:1. Lấy màu sáng rồi giảm độ sáng sẽ ra nền xám bẩn và tụt tương phản.

### Kiểm theme

Một thay đổi màu chỉ được coi là xong khi cả hai theme cùng qua. Tính lại tỉ lệ chứ đừng đoán:

```bash
grep -rn -- '--color-' docs/Design/DESIGN.md | grep -c '#'
```

Bảng ở §2 phải khai đủ cả hai cột và cột tỉ lệ. Một dòng thiếu cột tối là một dòng chưa xong.

---

## 9. Do / Don't

- ✅ Dùng đúng ba bậc viền theo vai. `--color-border-strong` chỉ cho chỗ gõ được — đặt nó lên một `Card` là xoá mất tín hiệu duy nhất báo "chỗ này nhập liệu".
- ✅ `Card` luôn có `--color-border` **và** `--shadow-1`. Bóng một mình không phải ranh giới.
- ✅ Vòng focus luôn có `outline-offset` ≥ 2px. Không có khoảng hở thì vòng focus trên nút primary là vô hình.
- ✅ Đặt tên token mới theo **vai**, không theo màu: `--color-danger`, không phải `--color-red`. Ngày đổi sang cam thì tên vẫn đúng.
- ✅ Mỗi màu mới phải kèm **cả giá trị tối lẫn tỉ lệ tương phản đã tính**, ngay trong lần thêm đầu tiên.
- ❌ Không viết hex, không viết `rgb()` literal trong SCSS component. Cổng FE bắt — [`../RULES.md`](../RULES.md) §7 F6/F7.
- ❌ Không đẻ cỡ chữ, bậc khoảng cách, bo góc ngoài thang. Thiếu thì thêm bậc vào file này kèm lý do.
- ❌ Không dùng `--color-brand-subtle` làm tín hiệu duy nhất cho trạng thái "đang chọn" — 1.25:1.
- ❌ Không animate `width`/`height`/`top`/`left`. Chỉ `transform` và `opacity`.
- ❌ Không khai token màu chỉ ở một nhánh theme. Mọi token có mặt ở `:root` trần trước.
- ❌ Không tự đổi giá trị ở code rồi cập nhật ngược file này. Chiều là `Design/` → code ([`CLAUDE.md`](./CLAUDE.md) §5).

---

## 10. Cần chốt

| # | Câu hỏi để ngỏ | Ai trả lời được |
| --- | --- | --- |
| 1 | Biểu đồ vẽ bằng thư viện nào? Bảng màu đã chốt ở §2.8, nhưng cách vẽ thì chưa. PrimeNG đi kèm Chart.js — nó vẽ bằng canvas nên nhanh với dữ liệu lớn, đổi lại **không có gì cho trình đọc màn hình** và không kế thừa được token CSS, phải nạp màu bằng script. Hình đơn giản (thanh, bullet, chênh lệch, ô nhiệt) dựng bằng HTML/CSS thì thừa hưởng token và theme miễn phí | Người dựng [`Components/Chart.md`](./Components/Chart.md), khi có màn thật |
| 2 | Có làm chế độ tương phản cao (`forced-colors`) không? Hôm nay chỉ đặt mục tiêu AA. Chế độ cưỡng bức màu của Windows sẽ ghi đè toàn bộ bảng màu và cần một lượt kiểm riêng | Sau khi có màn hình thật để kiểm |
| 3 | `--layout-container-max` = 1440px hay rộng hơn? Bảng nhiều cột muốn rộng hơn; văn bản đọc thì không nên. Có thể phải cho một biến thể trang "full width" | Sau khi có `DataTable` thật |
| 4 | Có khai token riêng cho mật độ (compact/comfortable) không? Hôm nay chỉ có một mật độ, đặc | Khi có yêu cầu thật |

## 11. Lịch sử quyết định

| Quyết định | Nội dung |
| --- | --- |
| Bảng màu khởi tạo | Tính từ công thức tương phản WCAG, không chọn bằng mắt. Mọi cặp chữ/nền đạt AA ở **cả hai** theme |
| Có theme tối ngay từ đầu | Lệch dự án tiền nhiệm (chỉ một theme sáng). Thêm theme tối sau tốn hơn nhiều lần, vì phải rà lại từng component |
| Ba bậc viền | Lệch dự án tiền nhiệm (hai bậc). Bậc `subtle` tách ra để không phải chọn giữa "vạch bảng quá đậm" và "viền card dưới chuẩn" |
| Bốn nét font, không hơn | Ở dự án tiền nhiệm có khai nét không nạp, hai chỗ tưởng khác nhau render giống hệt |
| Cỡ chữ khai bằng `rem` | Lệch dự án tiền nhiệm (`px`). `px` chặn tính năng phóng chữ của trình duyệt |
| Alpha đi qua biến kênh `*-rgb` | Cách duy nhất vừa giữ được token vừa tuân luật cấm `rgba()` literal |
| Bảng màu biểu đồ — chốt là **có** | §10 trước đây để ngỏ. Nay khai ở §2.8: tám màu thứ tự cố định, dải mức độ, dải hai chiều, đều đã chạy qua phép kiểm bằng máy ở cả hai theme. Câu còn lại — vẽ bằng thư viện nào — vẫn ở §10 |
| `--chart-1` ở theme tối **khác** `--color-brand` tối | Màu thương hiệu tối `#6ba1f0` có độ sáng 0,705, vượt dải cho phép của mark trên nền tối. Hạ một bậc thành `#3987e5`. Hai token cố ý khác nhau — xem §2.8 ràng buộc 1 |
| Thêm bậc `hover`/`active` cho `danger` | Hai spec `Button` và `IconButton` đã khai trạng thái hover cho biến thể `danger` trong khi token chưa tồn tại. Khai ở §2.5. Ba vai trạng thái còn lại cố ý **không** có bậc hover |
| Cặp bề mặt đảo cho `Tooltip` | Tooltip là lời chú, không phải mặt giấy để thao tác. Đảo màu thay cho thêm một đường viền nữa. Bề mặt duy nhất trong hệ được phép đảo — §2.6 |
| Cỡ icon chuyển từ `Icons.md` sang đây | Bốn giá trị px sống ở hai nơi trong khi file này tự khai là nơi duy nhất giữ px thật. Nay có mốc `— định nghĩa gốc` ở §6.2 và một dòng trong [`../OWNERSHIP.md`](../OWNERSHIP.md) |
| `--tree-indent` khai bằng `px` | Nó là hình học của khung, không phải của chữ — cùng lý do với bo góc. 20px là biên hai đầu, xem §6.1 |
| Thang trung tính sáng nhạt đi một bậc | `bg` `#eef1f6` → `#f4f6fa`, `surface-2` `#e2e8f0` → `#eef1f6`, `surface-3` `#d5dde8` → `#e2e8f0`, `border-subtle` `#dbe1ea` → `#e4e9f0`. Thứ làm giao diện nặng là dải nền `th` và viền, không phải nền trang — nên cả thang nhấc lên cùng lúc, không chỉnh lẻ. Lý do đầy đủ và các phương án đã loại: [`../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md`](../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md) |
| Viền chỉ nhạt được tới `#7f8da3` | `border` `#7c8ba3` → `#7f8da3`. Ngưỡng chặn là **`border` trên `bg`**, không phải trên `surface`: nhạt thêm một bậc thì viền `Card` còn 2.99:1 và vi phạm SC 1.4.11. Biên do phép đo đặt, xem §2.3 |
| Theme mặc định là `light`, không theo hệ điều hành | Ứng dụng quản trị dùng cả ngày trong phòng sáng; cờ `prefers-color-scheme` của máy là tín hiệu yếu. Cái giá và điều kiện chấp nhận ở §8, quyết định đầy đủ ở [`../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md`](../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md) |
