---
kind: luat
scope: core
verified: chua-doi-chieu
---

# DESIGN.md — hệ thống thiết kế nền của Core

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Giá trị token của file này đã được áp vào các stylesheet dưới `src/FE/src/styles/`; bảng dưới khai những mục đã mở source ra so ngày 2026-09-22, chỉ bằng đọc mã — dòng nào mang ngày khác, hoặc dựa vào một phép đo trên trình duyệt, thì tự ghi rõ trong ô của nó. Luật **dùng** token theo từng component chưa ai đối chiếu, nên `verified:` giữ `chua-doi-chieu`. Chiều cập nhật (`Design/` quyết, code áp) ở [`CLAUDE.md`](./CLAUDE.md) §5.

| Mục | Có thật hôm nay (neo bằng chuỗi trong `src/FE/src`) | Sẽ thành |
| --- | --- | --- |
| §2.1–§2.6 và §2.8 — mọi dòng token màu, cả cột sáng lẫn cột tối, gồm hai bảng bốn vai trạng thái | So từng giá trị, không lệch: `styles/_tokens.scss` § `:root` (giá trị sáng) và § `@mixin dark-tokens` (giá trị tối). Token vắng khỏi khối tối thì giữ giá trị sáng — đúng với `--chart-6`, token duy nhất bảng §2.8 khai trùng giá trị hai theme | Giữ nguyên |
| §3 Chữ — họ font, thang cỡ, cân nặng, chiều cao dòng, giãn chữ | Khớp: `styles/_typography.scss` § `--font-sans`, § `--fs-3xl`, § `--fw-bold`, § `--lh-loose`, § `--ls-wide`. Font tự phục vụ, đúng bốn nét: § `@font-face` trỏ `/fonts/be-vietnam-pro-700-vietnamese.woff2` và các tệp cùng họ dưới `src/FE/public/fonts/` | Giữ nguyên |
| §4 Khoảng cách; §6.1 khung; §6.2 chiều cao control và cỡ icon; §6.5 token tầng 3 | Khớp — so lại cả bốn bảng với mã ngày 2026-10-08, sau thay đổi chốt cùng ngày; bí danh giữ đúng dạng `var(...)` như bảng khai: `styles/_spacing.scss` § `--sp-12`, § `--layout-popover-h-max`, § `--size-control-lg`, § `--icon-circle-lg`, § `--timeline-line-w`, § `--data-table-scroll-h-min: 280px`, § `--toolbar-search-w-max: 360px`. Trần `main` đã gỡ: không tệp nào dưới `src/FE/src` còn chuỗi `--layout-container-max`, `platform/shell/shell.component.scss` không còn `max-width`. Hai token mới có nơi đọc: `app/shared/components/toolbar/toolbar.component.scss` § `max-width: var(--toolbar-search-w-max)`; `--data-table-scroll-h-min` đi qua biến trung gian `--data-table-scroll-h-floor` mà `_spacing.scss` § `@mixin man-danh-sach` đặt trên host của màn, `app/shared/ui/data-table/data-table.component.scss` đọc § `min-height: var(--data-table-scroll-h-floor, auto)`. Biến trung gian này có dòng riêng ở §6.5 (khai 2026-10-09); đọc lại mã cùng ngày: ngoài tệp kiểm `data-table.khung.spec.ts`, chỉ mixin đó đặt nó — kiểm bằng `grep -rln -- '--data-table-scroll-h-floor' src/FE/src`, PASS khi chỉ in ba tệp: `styles/_spacing.scss`, `data-table.component.scss`, `data-table.khung.spec.ts` | Giữ nguyên |
| §6.5 — ba token đệm `AuthField` / `Input` (khai 2026-10-08) | **Khớp** (đọc mã 2026-10-08): `styles/_spacing.scss` § `--auth-field-pad-icon:`, § `--auth-field-pad-reveal:`, § `--input-pad-reveal: var(--auth-field-pad-reveal)` — đúng phép cộng và bí danh của bảng §6.5. Mỗi component đọc tên token của chính mình: `app/shared/components/auth-field/auth-field.component.scss` § `padding-left: var(--auth-field-pad-icon)`, § `padding-right: var(--auth-field-pad-reveal)`; `app/shared/components/input/input.component.scss` § `.input--co-nut` với `padding-right: var(--input-pad-reveal)` | Giữ nguyên. Hình thức sau khi đổi sang tên token chưa đo lại trên trình duyệt |
| §6.3 Điểm ngắt là biến SCSS | Khớp (đọc mã 2026-10-08): `styles/_spacing.scss` § `$bp-xl`, dùng qua § `@mixin tu-man-hinh` và § `@mixin duoi-man-hinh` (phép trừ `0.02px` khai một lần). Ngoại lệ TypeScript đã vào code: `app/core/theme/diem-ngat.ts` § `TRUY_VAN_DUOI_BP_XS`, § `TRUY_VAN_DUOI_BP_SM`, mỗi hằng nêu đích danh `$bp-*` nó phản chiếu; ba component TypeScript import từ đó — `shared/components/topbar/topbar.component.ts`, `shared/ui/data-table/data-table.component.ts`, `shared/ui/drawer/drawer.component.ts` (chuỗi `core/theme/diem-ngat`). Cổng F58 của `scripts/fe-gate.sh` chạy 2026-10-08: OK | Giữ nguyên |
| §5 Bo góc, bóng, độ dày viền; §6.4 lớp xếp chồng; §7 thời lượng và đường cong | Khớp: `styles/_tokens.scss` § `--radius-full`, § `--shadow-4`, § `--border-w-accent`, § `--z-toast`, § `--ease-accelerate` | Giữ nguyên |
| §7 giảm chuyển động — khối CSS toàn cục, lớp chính (loại 1) | Có, đúng bốn thuộc tính và `0.01ms`, cả bốn mang `!important`: `styles/styles.scss` § `@media (prefers-reduced-motion: reduce)`. Phạm vi chỉ là loại 1: đo trên trình duyệt ngày 2026-09-24, khối này không chạm hoạt ảnh Angular — cách đo ở §7 | Giữ nguyên, kể cả `!important` — luật F63 ở [`../RULES-FE.md`](../RULES-FE.md) |
| §7 giảm chuyển động — lớp phụ `safe` của thư viện | App không đặt `safe` ở đâu (đọc mã 2026-11-07): `grep -rnwE 'safe\|pMotionSafe\|motionOptions\|overlayOptions' src/FE/src/app --exclude='*.spec.ts'` không in dòng nào — bỏ cờ loại trừ thì chỉ thêm dòng chú thích của bốn tệp spec chuyển động, không dòng mã nào. Ba chỗ truyền `[pt]` — `shared/ui/check/`, `shared/ui/data-table/`, `shared/ui/toast/` — không mang `safe`; chú thích của khối pass-through ở toast khai thẳng § `Cố ý KHÔNG khai khoá`. Mặc định của thư viện chỉ đọc tĩnh: [ADR-0124](../adr/0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md) bảng Bối cảnh, dòng *Lớp `safe` của thư viện* | Giữ nguyên — F63. Chưa đo trên trình duyệt với cờ bật. Lỗi lớp nổi neo đóng ngay dưới cờ bật (báo 2026-11-07) có nguyên nhân ở nơi gắn lớp nổi, không ở `safe`, và đã sửa ở lớp bọc — [`Components/Menu.md`](./Components/Menu.md), dòng *Lớp nổi gắn vào `body`*. Lượt sửa đó không đo `safe`: đừng đọc dòng này là lớp phụ đã được đo trên màn hình |
| §7 giảm chuyển động — provider hoạt ảnh của Angular | **Đã gỡ** (đọc mã 2026-11-07): `app/core/theme/` không còn tệp `core-animations.ts`; `grep -rnE 'provideCoreAnimations\|provideNoopAnimations\|@angular/animations' src/FE/src src/FE/e2e src/FE/package.json` không in dòng nào — tiêu chí PASS của ADR-0124 quyết định 1, 3, 4. `src/FE/package-lock.json` không còn mục cài của gói đó (đối chiếu 2026-11-07: `grep -c '"node_modules/@angular/animations"' src/FE/package-lock.json` in `0`); trạng thái sống của rào ở ô *Ép bằng gì* của F64 | Giữ nguyên. Đưa bộ chạy hoạt ảnh của Angular trở lại là thêm phụ thuộc ngoài, phải có ADR; luật cấm import nó là **F64** ở [`../RULES-FE.md`](../RULES-FE.md), vì sao và cách đóng: [ADR-0128](../adr/0128-tai-nhap-bo-chay-hoat-anh-khong-bi-lock-chan-luat-f64-review-canh-chua-dung-cong.md) — spec này không khẳng định cơ chế canh. Dòng này giữ trong bảng thay vì rời bảng, vì §7 *Giảm chuyển động* trỏ về đây làm neo cho câu *app không khai provider hoạt ảnh nào* |
| §8 khuôn theme — `:root` trần đủ token, khối tối tách khỏi media query, `color-scheme` | Có: `styles/_tokens.scss` § `:root[data-theme='dark']`, § `:root:not([data-theme='light'])`, § `color-scheme: light dark`. Khối tối viết một lần trong mixin rồi `@include` hai lần | Giữ nguyên |
| §8 mặc định `light`, áp trước khi trang vẽ | Có: script nội tuyến trong `index.html` § `localStorage.getItem('theme')` — vắng hoặc giá trị lạ thì đặt `light`, `system` thì bỏ thuộc tính | Giữ nguyên |
| §1 Preset thư viện không giữ bảng màu thứ hai | `app/core/theme/prime-preset.ts` không chứa mã hex hay `rgb(` literal nào; màu trỏ `var(--color-*)` | Giữ nguyên |
| §2.6 *Ô nhập chữ* (chốt 2026-10-05) — ô của `AuthField` | **Có — đo trên trình duyệt 2026-10-06** ở `/dang-nhap`, hai chế độ (Chromium do Playwright điều khiển, `ng serve`, bề rộng 1280/768/390, sáng và tối): ô hợp lệ có focus — viền `--border-w` màu `--color-text` cộng bóng `inset` thành `--border-w-strong`, không `outline`; ô lỗi nghỉ `--border-w-strong` · `--color-danger-border`; ô lỗi có focus `--border-w-accent`, giữ màu lỗi; hover không đổi viền; ép `:autofill` qua giao thức DevTools thì nền ô giữ `--color-surface`, chữ `--color-text`. Neo: `app/shared/components/auth-field/auth-field.component.scss` § `&:focus-visible`, § `.auth-field__khung--loi .auth-field__input`, § `&:-webkit-autofill`. Lượt đo chạy trước một lượt sửa cùng ngày của tệp đó (đệm phải, nút hiện/ẩn); bằng chứng sau sửa là phép đo A5, A7 của lưới `src/FE/e2e/` (`e2e/ha-tang/lop-a/a5-o-nhap.ts`, `a7-tu-dien.ts`) — phiên chính báo xanh, kết quả không lưu trong repo. Chi tiết: [`Components/AuthField.md`](./Components/AuthField.md), bảng đầu | Giữ nguyên. Tự điền **thật** của trình duyệt còn phải kiểm tay một lần |
| §2.6 *Ô nhập chữ* — `Input`, ô tìm của `Toolbar`, ô của các lớp bọc PrimeNG | **Đã đổi trong mã, chưa đo** (đọc mã 2026-10-06): `app/shared/components/input/input.component.scss` § `&:focus-visible` và § `.input--error`; `app/shared/components/toolbar/toolbar.component.scss` § `.toolbar__tim-input`; `app/core/theme/prime-preset.ts` § `focusBorderColor: 'var(--color-text)'`, `focusRing` của ô chỉ còn bóng `inset`. Nền tự điền khai ở hai tệp SCSS (§ `&:-webkit-autofill`); lớp bọc PrimeNG không có quy tắc tự điền nào. Ô lỗi có focus của lớp bọc PrimeNG chưa so. Không màn nào trong lưới Playwright đợt 2026-10-06 dùng ba loại ô này | Lật khi đo trên trình duyệt — mỗi loại ô một lần |
| Cột "Dùng ở đâu" của mọi bảng; luật §1 *component chỉ đọc token tầng 2 và tầng 3 của chính nó*; §2.7; §9; dòng `hover` của §2.6 *Ô nhập chữ* và luật *phần tử nào dùng họ nào* ở §3.1 (cả hai chốt 2026-10-06) | **Chưa đối chiếu.** Đây là luật áp lên từng component — đối chiếu theo từng spec ở [`Components/`](./Components/) | Đối chiếu hết thì lật `verified:` và nhãn của file này |
| Các cột tỉ lệ tương phản | Không đối chiếu với code: số tính từ giá trị màu, không phải thứ code hiện thực | — |

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

**Thư viện UI không giữ bảng màu thứ hai.** Preset PrimeNG chỉ trỏ tầng semantic của thư viện vào `var(--color-*)`, không khai mã màu nào. Giá trị màu quyết ở §2; nơi áp trong code là file token SCSS — [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md). Cơ chế preset ở [`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md).

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
| `--color-text` | `#111827` | `#e6edf3` | Chữ chính, tiêu đề, giá trị ô bảng; đường biên ô nhập chữ hợp lệ khi có focus (§2.6 *Ô nhập chữ*) | **17.74:1** / **14.64:1** |
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
| `--color-focus` | `#1a56b8` | `#8fb9f5` | Vòng focus của **mọi** control, trừ ô nhập chữ (§2.6 *Ô nhập chữ*) |
| `--color-scrim` | `rgb(var(--color-overlay-rgb) / 0.08)` | `rgb(var(--color-overlay-rgb) / 0.24)` | Lớp mờ phủ nội dung khi đang `loading` |

Alpha ở theme tối cao hơn (0.70 so với 0.55) vì backdrop tối trên nền vốn đã tối cần đậm hơn mới tách được lớp dialog.

**Vòng focus và cái bẫy của nó.** `--color-focus` ở theme sáng **trùng giá trị** với `--color-brand` — nghĩa là vòng focus vẽ sát nền một nút primary sẽ có tương phản **1.00:1**, tức vô hình. Đây là lỗi thường gặp và hệ này chặn nó bằng hình học, không bằng màu:

```text
outline: 2px solid var(--color-focus);
outline-offset: 2px;
```

`outline-offset: 2px` đẩy vòng ra khỏi mép nút, nên nó luôn nằm trên `--color-surface` hoặc `--color-bg` — nơi tương phản là **6.85:1** (sáng) và **8.59:1** (tối). Khoảng hở 2px chính là thứ làm vòng focus hợp lệ. Control nằm trong `NoticeBanner` có vòng focus rơi lên nền `*-bg` của vai: thấp nhất **5.44:1** (sáng, `--color-danger-bg`) và **6.68:1** (tối, `--color-info-bg`) — đều qua 3:1, không cần ngoại lệ.

🛑 **Cấm `outline: none` mà không thay bằng dấu hiệu focus khác.** Đây là lỗi accessibility bị bắt nhiều nhất trong mọi audit; nếu cần vòng bám sát hình dạng bo góc, dùng `box-shadow: 0 0 0 2px var(--color-surface), 0 0 0 4px var(--color-focus)` — vẫn có khoảng hở, vẫn đạt tỉ lệ.

Focus chỉ vẽ ở `:focus-visible`, không ở `:focus`. Chuột bấm thì không hiện vòng; bàn phím thì hiện — **trừ ô nhập chữ**, xem mục dưới.

#### Ô nhập chữ — một dấu hiệu, nằm đúng chỗ viền

Khoảng hở 2px ở trên sinh ra cho control **nền đặc**: nút primary có nền trùng màu vòng focus. Ô nhập chữ — `Input`, `AuthField`, phần ô gõ của `DatePicker`, `Autocomplete`, `TreeSelect`, ô tìm của `Toolbar` — không có cái bẫy đó. Vòng tách khỏi ô chỉ đẻ ra **hai đường song song** quanh một ô: viền của ô, rồi một vòng nữa cách ra 2px; ở ô đang lỗi, hai đường đó là một đỏ một xanh.

Giả định "vòng chỉ hiện khi dùng bàn phím" **sai với ô nhập chữ**. Đặc tả Selectors Level 4, mục `:focus-visible`, gợi ý trình duyệt luôn khớp `:focus-visible` cho phần tử nhận gõ phím, dù focus đến bằng chuột hay bằng script — và trình duyệt làm đúng như vậy: đo 2026-10-05, ô mã đơn vị nhận focus do trang đặt lúc mở màn đã hiện vòng. Nên mọi người dùng đều thấy mọi dấu hiệu focus mà ô nhập vẽ, cùng một lúc.

**Luật.** Ô nhập chữ có **đúng một đường biên** ở mọi thời điểm, nằm đúng chỗ viền: không vòng ngoài, không khoảng hở, không dùng `--color-focus`. Đường biên đọc theo hai trục — **màu nói ô hợp lệ hay lỗi, độ dày nói ô có focus hay không**:

| Ô | Không focus | `focus-visible` |
| --- | --- | --- |
| Hợp lệ | `--border-w` · `--color-border-strong` | `--border-w-strong` · `--color-text` — viền tối lên và dày lên |
| Lỗi | `--border-w-strong` · `--color-danger-border` | `--border-w-accent` · `--color-danger-border` — **giữ màu lỗi**, chỉ dày lên |

| Khoản | Luật |
| --- | --- |
| Kích thước | Phần dày thêm vẽ vào **phía trong** viền. Kích thước ô và vị trí chữ không đổi giữa bốn ô của bảng trên |
| `disabled` | Thắng mọi trạng thái — ô `disabled` không nhận focus. Hình thức ở spec component |
| `hover` | Chỉ áp khi ô **không** có focus — ô đang có focus giữ đúng đường biên cột `focus-visible` của bảng trên. Ô hợp lệ: chỉ đổi con trỏ sang `text`; viền giữ `--border-w` · `--color-border-strong` như lúc nghỉ, không đậm thêm bậc nào. Ô đang lỗi: hover không đổi gì — giữ `--border-w-strong` · `--color-danger-border` |
| Ô đang lỗi | Màu lỗi ở lại suốt lúc gõ. Icon, dòng lỗi dưới ô và `aria-invalid` vẫn là ba kênh mà §2.7 bắt buộc |
| Trình duyệt tự điền | Ô giữ nền `--color-surface` và chữ `--color-text` của trạng thái nghỉ, ở cả hai theme; đường biên theo bảng trên. Không có dấu hiệu "đã tự điền" riêng. `disabled` vẫn thắng: ô đã tự điền rồi bị khoá khi form đang gửi thì mang nền và chữ `disabled` |
| Phạm vi | **Chỉ ô nhập chữ.** Nút, liên kết, ô đánh dấu, nút hiện/ẩn mật khẩu nằm trong ô, và mọi control khác giữ vòng `--color-focus` có khoảng hở ở trên |

**Vì sao `--color-text`, không thêm token.** Thang trung tính không có bậc viền nào đậm hơn `--color-border-strong`; bậc đậm kế tiếp là chính màu chữ chính. Ở theme tối `--color-text` lật thành gần trắng, nên "viền tối lên" ở theme sáng là "viền sáng lên" ở theme tối — cùng một nghĩa: đường biên tiến về phía chữ, xa khỏi nền. Một token riêng chỉ để trỏ về `--color-text` thì không quyết thêm điều gì.

Tương phản — cùng công thức với §2. Dấu hiệu focus là phần điểm ảnh **đổi** khi ô nhận focus:

| Ô | Cặp | Sáng | Tối | Nghĩa |
| --- | --- | --- | --- | --- |
| Hợp lệ | Dải `--color-text` trên `--color-surface` | **17.74:1** | **14.64:1** | So với nền ô và khung quanh ô — SC 1.4.11 ≥ 3:1 ✅ |
| Hợp lệ | Dải `--color-text` trên `--color-bg` | **16.40:1** | **16.02:1** | Ô đặt thẳng trên nền trang ✅ |
| Hợp lệ | Hàng trong của dải: nền ô → `--color-text` | **17.74:1** | **14.64:1** | Đổi so với lúc chưa focus ✅ |
| Hợp lệ | Hàng ngoài của dải: `--color-border-strong` → `--color-text` | **3.37:1** | 2.98:1 | Đổi so với lúc chưa focus — theme tối thiếu 0.02 |
| Lỗi | Dải `--color-danger-border` trên `--color-surface` | **4.19:1** | **3.72:1** | SC 1.4.11 ✅ |
| Lỗi | Dải `--color-danger-border` trên `--color-bg` | **3.88:1** | **4.07:1** | ✅ |
| Lỗi | Hàng thêm vào: nền ô → `--color-danger-border` | **4.19:1** | **3.72:1** | Đổi so với lúc chưa focus ✅. Hai hàng cũ giữ màu, chỉ đổi độ dày |

**Mức đạt, nói thẳng.** SC 2.4.7 Focus Visible và SC 1.4.11 Non-text Contrast, hai tiêu chí mức AA, đúng mục tiêu của Core ở §2: đạt ở cả bốn ô, cả hai theme — dải luôn ≥ 3:1 với nền, và lúc nhận focus luôn có ít nhất một hàng điểm ảnh dày 1px đổi ≥ 3:1. SC 2.4.13 Focus Appearance (mức AAA) đòi vùng đổi ≥ 3:1 dày bằng chu vi 2px: ô hợp lệ đạt ở theme sáng (cả hai hàng ≥ 3:1), thiếu ở theme tối vì hàng ngoài 2.98:1; ô lỗi không đạt ở theme nào vì chỉ dày thêm 1px. Muốn ô lỗi đạt AAA thì phần dày thêm phải bằng 2px — cần một bậc độ dày chưa có ở §5.3, tức một quyết định riêng. Dải nằm trong khung ô nên khung cha có `overflow: hidden` không cắt được nó — cùng lý do `DataTable`, `Sidebar` dùng vòng nằm trong.

**Vì sao trình duyệt tự điền không được đổi nền.** Trình duyệt tô nền riêng cho ô nó vừa điền — Chrome dùng một nền xanh nhạt khai bằng `!important` trong stylesheet của chính trình duyệt, nên một khai báo nền thông thường không thắng được. Màu đó không đến từ bảng §2, ở theme tối do trình duyệt tự chọn, và trong hệ này một nền xanh nhạt mang nghĩa *đang chọn* (`--color-brand-subtle`, §2.4) — để nguyên là phát một tín hiệu sai nghĩa. Giá trị đã hiện trong ô là đủ phản hồi rằng trình duyệt vừa điền.

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

Biểu đồ **có** bảng màu riêng, và đây là bảng đó. Mọi giá trị dưới đây chạy qua phép kiểm bằng máy ở cả hai theme — số đo ghi kèm, không ước lượng bằng mắt.

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

**Phần tử nào dùng họ nào.** Mọi phần tử hiển thị chữ dùng `--font-sans` — kể cả `button`, `input`, `select`, `textarea`. Bốn phần tử này **không kế thừa** họ font của trang: stylesheet mặc định của trình duyệt gán cho chúng font hệ thống, nên họ font phải khai tường minh cho chúng; trông vào kế thừa thì nhãn nút rơi về font hệ thống trong khi cả màn là `Be Vietnam Pro`. `--font-mono` chỉ dùng cho chữ đơn cách.

**Vì sao `Be Vietnam Pro`:** giao diện của Core hiển thị tiếng Việt, và dấu tiếng Việt là chỗ nhiều font phương Tây gãy — dấu bị cắt, dấu chồng lên chữ hoa, dấu ngã và dấu hỏi khó phân biệt ở cỡ nhỏ. `Be Vietnam Pro` được thiết kế cho tiếng Việt và có đủ các nét 400/500/600/700.

**Font dự phòng:** `Segoe UI` (Windows) và `Roboto` (Android/ChromeOS) là hai font hệ thống có dấu tiếng Việt đầy đủ. `Arial` là chốt chặn cuối. Không để `sans-serif` đứng một mình ở vị trí thứ hai — trên vài môi trường nó rơi vào một font không có dấu.

**Chỉ nạp bốn nét: 400, 500, 600, 700.** Mọi khai báo `font-weight` phải là một trong bốn số này. Viết `750` hay `850` trên một font tĩnh sẽ bị trình duyệt làm tròn lên nét gần nhất — hai chỗ tưởng khác nhau sẽ render **giống hệt nhau**, và không có cảnh báo nào. Đây là bẫy đã xảy ra thật ở dự án tiền nhiệm.

Font **tự phục vụ** (self-host), không lấy từ CDN ngoài: lý do là quyền riêng tư và độ ổn định khi mạng nội bộ chặn ngoài. Đường dẫn file font là quyết định thi công, thuộc [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §3.5.

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
| `--fs-3xl` | `1.75rem` | 28px | Số liệu lớn, trang lỗi 403/404 |

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
| `--sp-4` | `0.5rem` | 8px | Khe nhãn → ô nhập, ở mọi form; đệm ngang của nút; khe giữa các control trong `Toolbar` |
| `--sp-5` | `0.75rem` | 12px | Đệm của `Toolbar` |
| `--sp-6` | `1rem` | 16px | **Đệm mặc định của `Card` và `Dialog`**; khe giữa hai trường form |
| `--sp-7` | `1.25rem` | 20px | Khe giữa hai nhóm trong một form |
| `--sp-8` | `1.5rem` | 24px | Khe giữa hai `Card`; đệm của `main` |
| `--sp-9` | `2rem` | 32px | Khe giữa hai vùng lớn của trang |
| `--sp-10` | `2.5rem` | 40px | Đệm dọc của `EmptyState` |
| `--sp-11` | `3rem` | 48px | Đệm của `AuthCard` |
| `--sp-12` | `4rem` | 64px | Khe trên/dưới trang lỗi toàn màn hình |

Quy tắc dùng:

1. **Mọi `margin`, `padding`, `gap` lấy từ thang.** Không có `padding: 7px`. Cần một giá trị không có trong thang → gần như luôn là dấu hiệu bố cục đang sai, không phải dấu hiệu thang thiếu bậc.
2. **Khoảng cách theo nhóm, không theo cặp.** Hai thứ cùng nhóm cách nhau `--sp-2` … `--sp-4` — nhãn cách ô của nó `--sp-4`; hai nhóm khác nhau cách nhau `--sp-6` trở lên. Khoảng cách chính là thứ tạo ra cảm giác "cùng một nhóm".
3. **Ưu tiên `gap` của flex/grid hơn `margin`.** `margin` gộp (collapse) và tràn ra ngoài container; `gap` thì không.
4. **Chiều dọc dùng bậc chẵn hơn.** Nhịp dọc của một trang nên đi theo `--sp-6` / `--sp-8`; các bậc nhỏ dành cho bên trong một control.

---

## 5. Bo góc, đổ bóng, viền

### 5.1 Bo góc

| Token | Giá trị | Dùng ở đâu |
| --- | --- | --- |
| `--radius-0` | `0` | Cạnh dính mép màn hình của `Drawer` — cùng vai với `--sp-0` và `--shadow-0`: giá trị 0 vẫn đi qua token |
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
| `--shadow-2` | `0 2px 8px rgb(var(--color-overlay-rgb) / 0.10)` | `0 2px 8px rgb(var(--color-overlay-rgb) / 0.50)` | Nút khi hover, `Topbar` khi trang đã cuộn, `AuthCard` |
| `--shadow-3` | `0 8px 24px rgb(var(--color-overlay-rgb) / 0.14)` | `0 8px 24px rgb(var(--color-overlay-rgb) / 0.60)` | Menu thả xuống, popover, `Sidebar` dạng drawer |
| `--shadow-4` | `0 16px 48px rgb(var(--color-overlay-rgb) / 0.20)` | `0 16px 48px rgb(var(--color-overlay-rgb) / 0.70)` | `Dialog`, `Toast` |

🛑 **Bóng không bao giờ là ranh giới.** Một `Card` phải có `--color-border`; bóng chỉ thêm chiều sâu. Người dùng chế độ tương phản cao và nhiều màn hình rẻ sẽ không thấy bóng nào cả.

### 5.3 Độ dày viền

| Token | Giá trị | Dùng ở đâu |
| --- | --- | --- |
| `--border-w` | `1px` | Mặc định cho mọi viền |
| `--border-w-strong` | `2px` | Viền ô nhập chữ khi lỗi, và khi hợp lệ có focus (§2.6 *Ô nhập chữ*); dải cạnh trái của `NoticeBanner`; vòng focus |
| `--border-w-accent` | `3px` | Dải cạnh trái của mục nav đang chọn; viền ô nhập chữ đang lỗi khi có focus (§2.6 *Ô nhập chữ*) |

---

## 6. Lưới và điểm ngắt

### 6.1 Kích thước khung

| Token | Giá trị | Ý nghĩa |
| --- | --- | --- |
| `--layout-sidebar-w` | `240px` | Bề rộng `Sidebar` mở |
| `--layout-sidebar-w-collapsed` | `64px` | Bề rộng `Sidebar` thu gọn (chỉ icon) |
| `--layout-topbar-h` | `56px` | Chiều cao `Topbar` |
| `--layout-page-pad` | `var(--sp-8)` | Đệm quanh `main` ở desktop |
| `--layout-page-pad-sm` | `var(--sp-5)` | Đệm quanh `main` dưới `$bp-md` |
| `--layout-dialog-w-sm` | `420px` | Bề rộng tối đa `Dialog` cỡ `sm` |
| `--layout-dialog-w-md` | `640px` | Bề rộng tối đa `Dialog` cỡ `md` |
| `--layout-dialog-w-lg` | `880px` | Bề rộng tối đa `Dialog` cỡ `lg` |
| `--layout-auth-w` | `var(--layout-dialog-w-sm)` | Bề rộng cố định `AuthCard` cỡ `md` |
| `--layout-auth-w-wide` | `var(--layout-dialog-w-md)` | Bề rộng cố định `AuthCard` cỡ `lg` (biến thể `wide`) |
| `--layout-form-w` | `var(--layout-dialog-w-md)` | Bề rộng tối đa của khối form trong trang, ở khung ứng dụng |
| `--layout-drawer-w-sm` | `var(--layout-dialog-w-sm)` | Bề rộng `Drawer` cỡ `sm` |
| `--layout-drawer-w-md` | `var(--layout-dialog-w-md)` | Bề rộng `Drawer` cỡ `md` |
| `--layout-drawer-w-lg` | `var(--layout-dialog-w-lg)` | Bề rộng `Drawer` cỡ `lg` |
| `--layout-toast-w` | `var(--layout-dialog-w-sm)` | Bề rộng **cố định** của `Toast` |
| `--layout-tooltip-w-max` | `240px` | Trần bề rộng `Tooltip`, mọi điểm ngắt. **Giá trị riêng**, không bí danh `--layout-sidebar-w` dù trùng số — đổi bề rộng sidebar không được kéo trần tooltip theo |
| `--layout-popover-h-max` | `420px` | Trần chiều cao lớp nổi thả xuống của `TreeSelect`, `Autocomplete`. **Giá trị riêng**, không bí danh `--layout-dialog-w-sm` dù trùng số — chiều cao một danh sách không có lý do gì đi theo bề rộng hộp thoại |
| `--tree-indent` | `20px` | Thụt lề **một cấp** trong lưới cha–con và trong ô chọn dạng cây |

**`main` không có trần bề rộng** (chốt 2026-10-08, §11): nó ăn hết bề rộng còn lại sau `Sidebar`, trừ đệm `--layout-page-pad`. Thứ cần trần vì lý do đọc — khối form, mô tả của `PageHeader` — mang trần riêng `--layout-form-w` ngay trong chính nó.

**Khung căn giữa và khối form trong trang dùng chung thang bề rộng của `Dialog`.** `--layout-auth-w`, `--layout-auth-w-wide` và `--layout-form-w` là bí danh của các bậc `Dialog`, không mang giá trị riêng: đổi một bậc `Dialog` là đổi luôn mọi chỗ dùng bí danh của bậc đó. Một khung xác thực hay một khối form rộng lệch vài pixel so với hộp thoại cùng cỡ là một bậc thứ tư không ai chủ ý tạo ra. `--layout-form-w` trỏ bậc `md` vì đó là bậc "form thông thường" của `Dialog` — form trong trang và form trong hộp thoại đọc cùng một bề rộng dòng. Dùng ở [`Components/Dialog.md`](./Components/Dialog.md), [`Components/AuthCard.md`](./Components/AuthCard.md) và spec màn có form một cột trong khung ứng dụng.

Bốn bí danh lớp nổi đi cùng luật đó, mỗi cái một lý do: `Drawer` mượn đúng ba bậc `Dialog` vì cùng họ lớp nổi có đầu–thân–chân; `Toast` cố định ở bậc `sm` để ngăn xếp thẳng mép. Hai trần `--layout-tooltip-w-max` và `--layout-popover-h-max` **cố ý không là bí danh**: bí danh chỉ đúng khi hai chỗ phải đổi cùng nhau, còn trùng số mà khác lý do thì bí danh là một phụ thuộc ngầm — đổi sidebar, tooltip đổi theo mà không ai chủ ý.

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
| `--icon-circle-sm` | `var(--sp-10)` | Vòng tròn nền icon của `EmptyState` cỡ `compact` |
| `--icon-circle-md` | `var(--sp-11)` | Vòng tròn nền icon của `EmptyState` cỡ `default` |
| `--icon-circle-lg` | `var(--sp-12)` | Vòng tròn nền icon của `EmptyState` cỡ `page` |

Icon dùng `px`, không dùng `rem` — nó là hình, không phải chữ. Cho nó phóng theo cỡ chữ hệ thống là làm nó vỡ khỏi khung nút. Ba vòng tròn nền là bí danh mượn ba bậc liền nhau của thang `--sp-*` — `--sp-10` vốn là đệm dọc của `EmptyState`, nên vòng tròn nhỏ nhất đi đúng nhịp đó; vòng tròn không nằm trong nút nên phóng theo chữ là được phép.

> Bốn giá trị này sống **ở đây**, vì file bạn đang đọc là nơi duy nhất chứa số px thật; [`Icons.md`](./Icons.md) §3 trỏ về. Quy tắc *chọn icon nào cho việc gì* và *icon hành xử ra sao với trình đọc màn hình* thuộc `Icons.md`; con số thì không.

### 6.3 Điểm ngắt

Điểm ngắt là **biến SCSS** (`$bp-*`), **không** phải CSS custom property: `var()` không dùng được trong điều kiện của `@media`, nên một biến khai ở `:root` không có media query nào đọc được. Giá trị **quyết** ở bảng dưới; nơi **áp** trong code là `_spacing.scss`, dùng qua mixin — [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §7. Không file nào khác khai lại con số, trừ một ngoại lệ có tên: TypeScript đọc điểm ngắt từ `src/FE/src/app/core/theme/diem-ngat.ts`, mỗi hằng nêu đích danh `$bp-*` nó phản chiếu, và cổng F58 so với `_spacing.scss` ([`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §7.1).

| Biến SCSS | Giá trị | Thiết bị điển hình |
| --- | --- | --- |
| `$bp-xs` | `480px` | Điện thoại dọc |
| `$bp-sm` | `640px` | Điện thoại ngang |
| `$bp-md` | `900px` | Máy tính bảng |
| `$bp-lg` | `1200px` | Laptop |
| `$bp-xl` | `1600px` | Màn hình rộng |

Ba mốc quyết định hình dạng khung:

| Ngưỡng | Điều gì xảy ra |
| --- | --- |
| ≥ `$bp-lg` | `Sidebar` cố định bên trái, `main` lệch một khoảng bằng bề rộng sidebar |
| `$bp-md` … `$bp-lg` | `Sidebar` mặc định thu gọn còn dải icon; hiện nhãn khi hover |
| < `$bp-md` | `Sidebar` thành drawer trượt trên backdrop; `Topbar` mọc nút hamburger; lưới form về một cột; `Toolbar` xuống dòng |
| < `$bp-xs` | Nút trong `Toolbar` giãn full width; bảng chuyển sang dạng thẻ. `DataTable` còn một lựa chọn thứ hai là cuộn ngang có cột đầu ghim; `Table` thì không — nó không có cột ghim ([`Components/Table.md`](./Components/Table.md) §Responsive) |

**Viết media query theo hướng mobile-first** — `min-width` là mặc định. Dùng `max-width` chỉ khi thật sự cần gỡ một hành vi chỉ có ở desktop.

### 6.4 Lớp xếp chồng

Khai một chỗ. Số rời rạc trong component là cách một hệ có `z-index: 99999`.

| Token | Giá trị | Cho ai |
| --- | --- | --- |
| `--z-base` | `0` | Nội dung thường |
| `--z-sticky` | `100` | `th` ghim, cột ghim của bảng |
| `--z-topbar` | `200` | `Topbar` |
| `--z-sidebar` | `300` | `Sidebar` |
| `--z-backdrop` | `400` | `Dialog` (PrimeNG vẽ mặt nạ và hộp trong **một** phần tử nên chỉ nhận một giá trị — xem luật dưới bảng), backdrop của drawer |
| `--z-dialog` | `500` | `ConfirmDialog` — cả backdrop lẫn hộp của nó |
| `--z-popover` | `600` | Menu thả xuống, tooltip |
| `--z-toast` | `700` | `Toast` — luôn trên cùng |

**Luật thang lớp chồng — định nghĩa gốc.** ✅ **CÓ THẬT** cho các bậc đã có test (đối chiếu 2026-10-06, đọc mã + `src/FE/src/app/platform/shell/lop-chong.spec.ts` chạy xanh trong Chrome: mặt nạ `Dialog` < hộp `Dialog` < nền = hộp `ConfirmDialog` < `Toast`; tâm nút xác nhận của `ConfirmDialog` bấm được). Bậc `--z-popover` (Menu, Tooltip, lớp nổi) **chưa có test** thứ tự so với `Dialog`/`ConfirmDialog`; đo bằng mắt trên trình duyệt chạy thật **chưa ai đo** (quyết định của người dùng 2026-10-06, Q09).

1. **Chỉ có MỘT thang: `--z-*` ở bảng trên.** Không component nào, kể cả lớp bọc PrimeNG, được đi theo thang `z-index` riêng của thư viện (mặc định của PrimeNG nằm quanh 1100 và vượt cả `--z-toast`).
2. **Thứ tự bắt buộc, thấp → cao:** `Dialog` (`--z-backdrop`) < `ConfirmDialog` (`--z-dialog`, backdrop và hộp cùng giá trị; backdrop đứng trước hộp trong DOM) < Menu thả xuống, `Tooltip`, các lớp nổi PrimeNG khác (`--z-popover`) < `Toast` (`--z-toast`). Hệ quả cần có: hộp "bỏ thay đổi?" mở **trên** một `Dialog` đang mở; một `Tooltip` hay lớp nổi của ô chọn trong `Dialog` hiện trên hộp; `Toast` trên tất cả.
3. **Cấu hình PrimeNG ghi bằng hằng số TypeScript phản chiếu token** — thư viện cộng số học lên giá trị nên không nhận `var()` (bản đang cài vẫn vậy, đọc mã 2026-11-06: tệp `primeng-dialog.mjs` trong gói `primeng` đã cài, chuỗi `this.baseZIndex + this.config.zIndex.modal`): ở `providePrimeNG` khai `zIndex.modal` = giá trị của `--z-backdrop`; `zIndex.overlay`, `zIndex.menu`, `zIndex.tooltip` = giá trị của `--z-popover`. Mỗi hằng kèm chú thích nêu đích danh token. Hai bên được so bằng spec chạy trên trình duyệt thật — `src/FE/src/app/core/theme/prime-z-index.spec.ts`, ca `modal = --z-backdrop` và `overlay, menu, tooltip = --z-popover` — không bằng luật cổng nào. Riêng `Tooltip` đã đi đường khác: `tooltipZIndex: 'var(--z-popover)'` ở `Tooltip.md`. Đổi một giá trị `--z-*` thì phải rà hằng số này — hai nơi, truy được về nhau, không phải một nơi.
4. **`ConfirmDialog` tự dựng giữ nguyên tới khi dựng lại trên `Dialog`** ([`Components/ConfirmDialog.md`](./Components/ConfirmDialog.md)). Tới lúc đó nó tự đặt `--z-dialog` cho cả backdrop lẫn hộp (đã đúng hôm nay, bảng dưới). Khi dựng lại trên `Dialog` thì luật 2 phải được giữ bằng cách khác, không được bỏ.
5. **Thư viện tự cộng thêm một bước nhỏ cho mỗi lớp mở chồng** (hành vi của PrimeNG, chưa kiểm trên trình duyệt). Khoảng cách 100 giữa các bậc `--z-*` là chỗ để việc đó không làm lớp này nhảy qua bậc kế; `lop-chong.spec.ts` đo `z-index` tính ra trong Chrome cho các bậc nêu ở đầu mục; kịch bản mở chồng nhiều lớp ngoài test đó chưa đo.

| Có thật (đối chiếu 2026-10-06, neo bằng chuỗi) | Ghi chú |
| --- | --- |
| `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.component.scss` § `z-index: var(--z-dialog)` ở `.confirm-dialog__backdrop` và ở `.confirm-dialog` | Khớp luật 2, 4 |
| `src/FE/src/app/core/theme/prime-z-index.ts` § `CORE_PRIME_Z_INDEX`; `src/FE/src/app/app.config.ts` chuỗi `zIndex: CORE_PRIME_Z_INDEX` | Khớp luật 3 |
| `src/FE/src/app/shared/ui/toast/toast.component.scss` § `z-index: var(--z-toast)`; `toast.component.html` chuỗi `[autoZIndex]="false"` | `Toast` cao nhất |

### 6.5 Token tầng 3 theo component — định nghĩa gốc

Giá trị px riêng của một component (tầng 3, §1) sống **ở đây**; spec component chỉ trỏ tên. Tên theo khuôn `--<component>-<thuộc tính>`.

| Token | Giá trị | Component · thuộc tính |
| --- | --- | --- |
| `--auth-field-pad-icon` | `calc(var(--sp-5) + var(--icon-md) + var(--sp-4))` | `AuthField` — đệm trái của ô có icon dẫn: icon cách mép `--sp-5`, rộng `--icon-md`, chữ bắt đầu cách icon `--sp-4` |
| `--auth-field-pad-reveal` | `calc(var(--sp-4) + var(--size-control-sm) + var(--sp-4))` | `AuthField` — đệm phải của biến thể `password`: nút hiện/ẩn cách mép `--sp-4`, vùng bấm `--size-control-sm`, chữ dừng cách nút `--sp-4` |
| `--chart-h-sm` | `120px` | `Chart` — chiều cao vùng vẽ cỡ `sm` |
| `--chart-h-md` | `200px` | `Chart` — chiều cao vùng vẽ cỡ `md` |
| `--chart-h-lg` | `320px` | `Chart` — chiều cao vùng vẽ cỡ `lg` |
| `--data-table-scroll-h-floor` | `var(--data-table-scroll-h-min)` — **không khai ở `:root`**; chỉ mixin `man-danh-sach` đặt, trên host của màn | `DataTable` — công tắc bật sàn vùng cuộn. Có biến thì vùng cuộn không thấp hơn `--data-table-scroll-h-min`; vắng thì không có sàn, khung cao theo nội dung — bảng con trong một màn cuộn. 🛑 Màn không tự đặt biến này và không đặt giá trị nào khác; muốn có sàn thì gọi mixin ([`Templates/ListScreen.md`](./Templates/ListScreen.md) §4) |
| `--data-table-scroll-h-min` | `280px` | `DataTable` — chiều cao tối thiểu của vùng cuộn (hàng tiêu đề cột + thân bảng) khi khối danh sách lấp chiều cao còn lại của `main` |
| `--file-upload-dropzone-h` | `140px` | `FileUpload` — chiều cao tối thiểu dropzone |
| `--input-pad-reveal` | `var(--auth-field-pad-reveal)` | `Input` — đệm phải của biến thể `password`. Bí danh: nút hiện/ẩn của hai component dùng chung một hợp đồng ([`Components/AuthField.md`](./Components/AuthField.md) §Accessibility), đổi cùng nhau |
| `--progress-bar-h-sm` | `4px` | `ProgressBar` — chiều cao máng cỡ `sm` |
| `--progress-bar-h-md` | `8px` | `ProgressBar` — chiều cao máng cỡ `md` |
| `--progress-bar-h-lg` | `14px` | `ProgressBar` — chiều cao máng cỡ `lg` |
| `--stepper-dot` | `24px` | `Stepper` — đường kính chấm bước |
| `--stepper-line-w` | `2px` | `Stepper` — độ dày vạch nối |
| `--timeline-dot` | `var(--stepper-dot)` | `Timeline` — đường kính chấm từ `$bp-sm`. Bí danh: chấm–vạch của hai component là cùng một ngôn ngữ tiến trình, đổi cùng nhau |
| `--timeline-dot-sm` | `16px` | `Timeline` — đường kính chấm dưới `$bp-sm` |
| `--timeline-line-w` | `var(--stepper-line-w)` | `Timeline` — độ dày vạch nối, bí danh cùng lý do |
| `--toolbar-search-w-max` | `360px` | `Toolbar` — trần bề rộng ô tìm từ `$bp-md` trở lên; dưới ngưỡng đó ô tìm trải hết hàng |

**Hai giá trị khai 2026-10-08, cả hai là số riêng chứ không bí danh** — trùng số với một bậc nào đó thì cũng không có lý do đổi cùng nhau (§6.1):

- `--data-table-scroll-h-min` = `280px` vì hai điều kiện cùng phải đạt: (1) hàng tiêu đề cột cộng **năm** dòng dữ liệu cỡ `md` — ít hơn thì người dùng không còn so được dòng đang đọc với vài dòng quanh nó; (2) [`Components/EmptyState.md`](./Components/EmptyState.md) cỡ `compact` nằm trọn dưới hàng tiêu đề cột mà không phải cuộn bên trong khung — một khối trống bị cắt đôi trông như lỗi. Số chọn là bội của 4px vừa vượt cả hai. Đây là **sàn**, không phải chiều cao mặc định: bình thường vùng cuộn cao bằng phần còn lại của màn; chỉ khi màn thấp hơn mức đó thì `main` mới cuộn (đường lui ở [`Templates/ListScreen.md`](./Templates/ListScreen.md) §4).
- `--toolbar-search-w-max` = `360px` vì: (1) đủ hiện trọn câu gợi ý dài nhất của ô tìm ở ba màn danh sách Core, ở `--fs-sm`, cộng icon tìm và chỗ cho spinner; (2) ô tìm, nút lọc và nút chính vẫn nằm **một hàng** ở `$bp-md` khi `Sidebar` thu gọn. Cả hai là ước lượng từ cỡ chữ, **chưa đo trên trình duyệt**.

**Ba token đệm khai 2026-10-08 — `--auth-field-pad-icon`, `--auth-field-pad-reveal`, `--input-pad-reveal` — không mang số mới**: chúng là phép cộng các bậc đã có, nên tự đúng khi bậc đổi. `--auth-field-pad-icon` trước đó khai ngay trong [`Components/AuthField.md`](./Components/AuthField.md) §Kích thước, nay về đây; hai token phía phải là quyết định của người dùng ngày 2026-10-08 (phương án bằng đúng hình thức đang chạy).

---

## 7. Chuyển động

| Token | Giá trị | Dùng ở đâu |
| --- | --- | --- |
| `--dur-instant` | `0ms` | Thay đổi phải tức thì |
| `--dur-fast` | `120ms` | Đổi màu khi hover/focus |
| `--dur-base` | `200ms` | Mở/đóng `Dialog`, trượt drawer, chuyển tab; bung và thu mọi lớp nổi neo vào phần tử đã gọi nó — mục *Lớp nổi neo* ngay dưới |
| `--dur-slow` | `320ms` | `Toast` bay vào, `Sidebar` thu/mở |
| `--dur-loop` | `1600ms` | Một vòng của chỉ báo loading lặp — dải sáng `SkeletonLoader`, dải chạy `indeterminate` của `ProgressBar`. Bằng 5 × `--dur-slow`: bội của thang, và đủ chậm để một vòng lặp không nhấp nháy |
| `--ease-standard` | `cubic-bezier(0.2, 0, 0, 1)` | Mặc định cho mọi chuyển tiếp |
| `--ease-decelerate` | `cubic-bezier(0, 0, 0, 1)` | Phần tử **đi vào** màn hình — kể cả chiều bung của mọi lớp nổi neo |
| `--ease-accelerate` | `cubic-bezier(0.3, 0, 1, 1)` | Phần tử **rời khỏi** màn hình — kể cả chiều thu của mọi lớp nổi neo |

**Bậc chia theo THỨ ĐANG ĐỔI, không theo con số nào đang trông giống.** `--dur-fast` giữ đúng một vai và vai đó thuần sơn màu: `background`, `color`, `border-color`, `outline-color`, `box-shadow`. Chuyển động hình học — `transform` và `opacity` của một hộp, một tấm, một lớp nổi — bắt đầu ở `--dur-base`, và chỉ lên `--dur-slow` khi phần tử đi vào từ rìa tầm nhìn nên mắt cần thêm thời gian để bắt được nó. Một khoản mới tra bậc bằng câu hỏi *nó đổi thuộc tính nào*, không bằng câu hỏi *bậc nào đang trùng số với thứ thư viện đặt sẵn*.

Hệ quả: khi không bậc nào vừa ý, câu trả lời **không** phải thêm một bậc. Lấy bậc gần nhất **theo vai** và chấp nhận nhịp đổi; một giá trị nằm ngoài thang là câu hỏi cho chính mục này, tức một quyết định của hệ thiết kế, chứ không phải một lựa chọn của component đang cần nó. Căn cứ và cái giá đã trả: [`../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md`](../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md).

### Lớp nổi neo — một bộ chuyển động cho mọi lớp nổi neo vào phần tử đã gọi nó

**Lớp nổi neo — định nghĩa gốc.** Mục này là **nơi duy nhất quyết** nhịp bung và thu của một lớp nổi neo: hộp của `Menu`, bảng gợi ý của `Autocomplete`, panel lịch của `DatePicker`, lớp nổi của ô chọn số dòng trong `Pagination`, và mọi lớp nổi neo dựng sau. Spec component trỏ về đây, không đặt số riêng (chốt 2026-11-06, người dùng).

| Khoản | Chiều bung | Chiều thu |
| --- | --- | --- |
| Thời lượng | `--dur-base` | `--dur-base` |
| Đường cong | `--ease-decelerate` | `--ease-accelerate` |
| Hình dạng | Mờ dần hiện ra, kèm phóng nhẹ tới cỡ thật | Mờ dần, kèm thu nhẹ |

Tắt khi `prefers-reduced-motion: reduce` — chuyển động loại 1 ở mục *Giảm chuyển động*, không cần nhánh riêng.

**Vì sao một thời lượng mà hai đường cong.** Bung và thu là chuyển động `transform` và `opacity` của một lớp nổi, tức vai của `--dur-base`, không phải `--dur-fast` dù con số mặc định của thư viện từng trùng bậc đó ([ADR-0075](../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) quyết định 2). Thang không có vai nào cho một thời lượng riêng chiều thu, nên thời lượng không tách; hai vai hướng thì bảng token trên khai đích danh, nên đường cong tách (cùng ADR, quyết định 3).

**Vì sao một nguồn.** Thư viện dựng mọi lớp nổi neo trên **một** lớp pha chung, và bộ trên áp bằng **một** khối đè trên lớp đó ở `src/FE/src/styles/_thu-vien.scss` — không theo lớp bọc, không bằng TypeScript (luật **F62** ở [`../RULES-FE.md`](../RULES-FE.md); [ADR-0123](../adr/0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) quyết định 4). Một chỗ áp mà nhiều spec cùng quyết là nhiều nguồn phải đồng bộ cho một giá trị. Muốn một lớp nổi neo chạy nhịp khác thì đó là câu hỏi cho mục này — một vai mới — không phải sửa riêng một spec. Hình dạng là keyframes của thư viện, giữ nguyên (ADR-0123 quyết định 1); bảng trên tả chúng bằng lời. Nhịp này nặng hay vừa tay: §10 câu 5.

### Khi nào KHÔNG animate

| Tình huống | Vì sao không |
| --- | --- |
| Người dùng khai `prefers-reduced-motion: reduce` | Bắt buộc — WCAG 2.2 SC 2.3.3. Thi hành ra sao, và loại chuyển động nào nằm ngoài cách thi hành chung: mục *Giảm chuyển động* ngay dưới |
| Nội dung bảng đổi sau khi lọc/sắp xếp | Hàng nhấp nháy làm mắt mất chỗ đang đọc. Đổi ngay, không transition |
| Thông báo lỗi xuất hiện | Lỗi phải thấy ngay. Fade 300ms là 300ms người dùng không biết mình vừa sai |
| Con số đang cập nhật liên tục | Đếm dần trông đẹp nhưng khiến giá trị không đọc được lúc đang chạy |
| Bất cứ thứ gì lặp vô hạn ngoài chỉ báo loading | Chuyển động lặp là tác nhân gây khó chịu và mất tập trung. Chỉ báo loading là ngoại lệ duy nhất, và chỉ nó được dùng `--dur-loop` |

Chỉ animate `transform` và `opacity`. Animate `width`, `height`, `top`, `left` bắt trình duyệt tính lại bố cục mỗi khung hình.

**Một ngoại lệ có tên, không hơn: pha ra của `Toast`.** Spec của nó đòi toast rời đi gập chiều cao để những cái còn lại trượt lên; hai thuộc tính trên không làm được việc đó nếu không đo vị trí bằng JavaScript — loại 3 ở dưới, thứ không lớp giảm chuyển động nào với tới. Keyframes ra của thư viện gập bằng thuộc tính bố cục, và spec nhận chúng. Biên của ngoại lệ — thuộc tính nào, pha nào, vì sao cái giá chấp nhận được: [`Components/Toast.md`](./Components/Toast.md) §Trạng thái. Component khác cần một ngoại lệ thì đó là câu hỏi cho mục này, không phải lựa chọn của spec đang cần nó.

### Giảm chuyển động — một khối CSS toàn cục, ba loại chuyển động

Cờ `prefers-reduced-motion: reduce` được thi hành bằng **một khối CSS toàn cục** — lớp chính — cộng một **lớp phụ** của thư viện cho component PrimeNG. Cả hai chỉ với tới chuyển động chạy bằng CSS; app không khai provider hoạt ảnh nào của Angular ([ADR-0124](../adr/0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md) quyết định 1; hiện trạng và lệnh kiểm ở bảng đầu tệp). Phân loại theo **thứ đang chạy chuyển động**:

| Loại | Thứ đang chạy chuyển động — nhận ra bằng gì | Lớp nào phủ | Spec component có phải tự khai nhánh giảm chuyển động? |
| --- | --- | --- | --- |
| 1 | **Trình duyệt, từ khai báo CSS** — `transition`, hoặc `animation` kèm `@keyframes`. CSS của app hay CSS do thư viện nạp đều tính, và lớp CSS mà `animate.enter` / `animate.leave` của Angular gắn lên phần tử cũng tính. Chuyển động vào/ra của component PrimeNG bản đang cài thuộc loại này: thư viện gắn rồi gỡ lớp CSS theo pha ([ADR-0123](../adr/0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md), bảng Bối cảnh) | Khối CSS toàn cục. Component PrimeNG có thêm lớp phụ `safe` — dưới đây | Không. Một câu trỏ về mục này là đủ |
| 2 | **Bộ chạy hoạt ảnh của Angular** — mã gọi `trigger()` của `@angular/animations`, trực tiếp hay qua một thư viện. Hôm nay không chuyển động nào trong app thuộc loại này (đọc mã 2026-11-07): không tệp nào trong `primeng/fesm2022` của gói đang cài import `@angular/animations`, không tệp `.ts` nào dưới `src/FE/src` gọi `trigger(`, và app không còn khai gói đó (bảng đầu tệp) | **Không lớp nào** | **Không được đặt.** Đưa bộ chạy trở lại là thêm phụ thuộc ngoài, phải có ADR (ADR-0124 quyết định 4); luật cấm import nó là **F64** ở [`../RULES-FE.md`](../RULES-FE.md), chốt ở [ADR-0128](../adr/0128-tai-nhap-bo-chay-hoat-anh-khong-bi-lock-chan-luat-f64-review-canh-chua-dung-cong.md) — mục này không khẳng định cơ chế canh luật đó; chuyển động mới viết bằng CSS hoặc `animate.enter` / `animate.leave` — tức loại 1 (ADR-0124, *Điều kiện xét lại* 3) |
| 3 | **Mã tự chạy chuyển động** — JS ghi `style` nội tuyến theo từng khung hình, gọi thẳng `element.animate()`, vẽ lên `<canvas>` | **Không lớp nào** | **Có, bắt buộc** — kèm cách kiểm |

Một component mang được nhiều loại cùng lúc. Nên phân loại **từng chuyển động**, không phân loại cả component.

**Lớp chính — khối toàn cục ở stylesheet gốc:**

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

Khối này chỉ ép thuộc tính CSS, nên phạm vi của nó dừng ở loại 1. Bộ chạy hoạt ảnh của Angular đi qua Web Animations API: thời lượng là tham số truyền bằng mã, không đọc từ `animation-duration` hay `transition-duration` — phép đo xác nhận điều này (Chrome Headless, `playState` vẫn `running` giữa chừng một hoạt ảnh) nằm ở [ADR-0080](../adr/0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md) mục Bối cảnh. Đó là lý do loại 2 ở bảng trên không lớp nào phủ.

🛑 **`!important` của khối là điều kiện để nó thắng, không phải thói quen.** CSS của thư viện nằm trong một cascade layer riêng, và khối đè thời lượng ở `_thu-vien.scss` (luật F62) là khai báo thường; khai báo `!important` thắng mọi khai báo thường, có layer hay không ([ADR-0124](../adr/0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md), bảng Bối cảnh, dòng *Lớp CSS*). Gỡ dấu đó thì khối này không còn chắc thắng CSS thư viện hay khối đè — luật **F63** ở [`../RULES-FE.md`](../RULES-FE.md).

**Lớp phụ — tuỳ chọn `safe` của bộ chuyển động PrimeNG.** Bộ chuyển động mà thư viện dùng để gắn rồi gỡ lớp pha (gói `@primeuix/motion`) có tuỳ chọn `safe`, mặc định bật: khi cờ khớp **lúc tạo** bộ chuyển động, nó bỏ qua cả pha và gọi thẳng các hook ([ADR-0124](../adr/0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md) quyết định 2, bảng Bối cảnh). App không đặt `safe` ở đâu cả — không `pMotionSafe`, không `motionOptions.safe`, không qua `overlayOptions` hay `pt` (luật F63; hiện trạng ở bảng đầu tệp). Lớp này chỉ là lớp phụ: khối CSS tự đứng không cần nó. Ba giới hạn:

| Giới hạn | Nghĩa là |
| --- | --- |
| Mặc định của bên thứ ba, ở phiên bản 0.0.x | Một lượt nâng đổi mặc định thì lớp này tắt lặng lẽ. Khối CSS vẫn đứng |
| Đọc cờ một lần, lúc tạo bộ chuyển động | Tắt cờ giữa phiên thì component đã dựng khi cờ còn bật vẫn không chuyển động, cho tới khi được dựng lại — thiếu chuyển động, không thừa |
| Vắng `matchMedia` thì coi như không giảm | Mọi trình duyệt app nhắm tới đều có `matchMedia`, và app không chạy SSR |

⚠️ **Đổi cờ giữa phiên.** Khối CSS là một media query, trình duyệt đánh giá lại ngay khi cờ đổi, nên với loại 1 bật hay tắt cờ giữa phiên đều có hiệu lực ngay. Chỉ lớp `safe` chậm một nhịp — giới hạn thứ hai ở trên. Hệ quả cho mọi mục kiểm: bật cờ — hoặc giả lập cờ trong công cụ của trình duyệt — **rồi tải lại trang**, để cả hai lớp cùng đọc cờ từ đầu.

**Loại 3 — không lớp nào với tới.** Một chuyển động do JavaScript ghi thẳng vào thuộc tính `style` của phần tử — chẳng hạn một vòng `requestAnimationFrame` nhích dần `opacity` — không đi qua thuộc tính CSS nào mà khối toàn cục ép, cũng không đi qua bộ chuyển động mà `safe` điều khiển. Khai báo nội tuyến thì không độ ưu tiên chọn lọc nào đè được; chỉ `!important` đè được. Cả hai lớp đi qua nó mà không đổi gì, và không có dấu hiệu nào báo rằng chúng vừa không làm gì cả. Mã gọi thẳng `element.animate()` cũng rơi vào loại này: nó là Web Animations, thời lượng truyền bằng mã như bộ chạy của Angular ở loại 2, nên khối CSS không với tới.

Ca đã chứng minh trong repo, kèm cách xử duy nhất còn lại (đối chiếu 2026-09-24): `src/FE/src/styles/_thu-vien.scss` mở một khối `@media (prefers-reduced-motion: reduce)` riêng cho `.app-tooltip` và đè bằng `opacity: 1 !important`; chú thích ngay trên khối đó (§ `NỘI TUYẾN`) khai thẳng vì sao phải làm vậy. Phần thuộc về component nằm ở [`Components/Tooltip.md`](./Components/Tooltip.md) §Accessibility.

**Hệ quả khi viết spec và khi dựng.** Câu hỏi đặt trước là *chuyển động này do cái gì chạy*, rồi tra bảng đầu mục. Loại 1: spec chỉ cần một câu *tắt khi `prefers-reduced-motion: reduce`* trỏ về mục này, người dựng không thêm gì. Loại 2: không đặt — viết lại thành loại 1. Loại 3: spec phải tự khai nhánh giảm chuyển động của riêng nó — tắt hẳn, hay thay bằng trạng thái tĩnh — kèm cách kiểm, và người dựng phải hiện thực nhánh đó. Không xác định được loại thì coi như loại 3 cho tới khi đã kiểm bằng trình duyệt với cờ bật và trang đã tải lại.

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
2. **Khối `[data-theme="dark"]` phải tách khỏi media query**, để lựa chọn tường minh của người dùng thắng cả hai chiều: người dùng chọn sáng trên máy đang ở chế độ tối vẫn ra sáng.
3. **`color-scheme: light dark` khai trên `:root`**, để thanh cuộn, ô chọn ngày và control mặc định của trình duyệt cũng đổi theo. Thiếu dòng này sẽ có một thanh cuộn trắng chói giữa giao diện tối.

Lựa chọn của người dùng lưu ở `localStorage` và **áp trước khi trang vẽ lần đầu**, nếu không sẽ có một nháy sáng trước khi theme tối kịp áp. Cách áp là quyết định thi công — [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §3.5.

### Token nào đổi, token nào không

| Nhóm | Đổi theo theme? |
| --- | --- |
| Toàn bộ `--color-*` | ✅ Đổi |
| `--shadow-*` | ✅ Đổi (alpha đậm hơn ở tối) |
| `--fs-*`, `--fw-*`, `--lh-*`, `--ls-*`, `--font-*` | 🛑 Không |
| `--sp-*` | 🛑 Không |
| `--radius-*`, `--border-w-*` | 🛑 Không |
| `--layout-*`, `--size-*` | 🛑 Không |
| `$bp-*` | 🛑 Không — biến SCSS, cố định lúc build |
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
- ✅ Vòng focus của nút và control nền đặc có `outline-offset` ≥ 2px. Không có khoảng hở thì vòng focus trên nút primary là vô hình. Ô nhập chữ là ngoại lệ có luật riêng ở §2.6 *Ô nhập chữ*: một đường biên nằm đúng chỗ viền, tối lên và dày lên khi có focus, không khoảng hở, không `--color-focus`.
- ❌ Không vẽ hai dấu hiệu focus chồng nhau trên một ô nhập — viền đổi màu **và** vòng ngoài. Ô nhập chữ khớp `:focus-visible` cả khi bấm chuột, nên cả hai luôn hiện cùng lúc (§2.6).
- ✅ Đặt tên token mới theo **vai**, không theo màu: `--color-danger`, không phải `--color-red`. Ngày đổi sang cam thì tên vẫn đúng.
- ✅ Mỗi màu mới phải kèm **cả giá trị tối lẫn tỉ lệ tương phản đã tính**, ngay trong lần thêm đầu tiên.
- ❌ Không viết hex, không viết `rgb()` literal trong SCSS component. Cổng FE bắt — [`../RULES.md`](../RULES.md) §7 F6/F7.
- ❌ Không đẻ cỡ chữ, bậc khoảng cách, bo góc ngoài thang. Thiếu thì thêm bậc vào file này kèm lý do.
- ❌ Không dùng `--color-brand-subtle` làm tín hiệu duy nhất cho trạng thái "đang chọn" — 1.25:1.
- ❌ Không animate `width`/`height`/`top`/`left`. Chỉ `transform` và `opacity` — ngoại lệ có tên duy nhất ở §7.
- ❌ Không khai token màu chỉ ở một nhánh theme. Mọi token có mặt ở `:root` trần trước.
- ❌ Không để khoá dịch hiện ra thành chữ trên giao diện — mọi chữ hiển thị đến từ bản dịch. Khoá thiếu bản dịch thì thư viện dịch trả về chính chuỗi khoá; cách chặn ở [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §2.2, §9.
- ❌ Không tự đổi giá trị ở code rồi cập nhật ngược file này. Chiều là `Design/` → code ([`CLAUDE.md`](./CLAUDE.md) §5).

---

## 10. Cần chốt

| # | Câu hỏi để ngỏ | Ai trả lời được |
| --- | --- | --- |
| 1 | Có làm chế độ tương phản cao (`forced-colors`) không? Hôm nay chỉ đặt mục tiêu AA. Chế độ cưỡng bức màu của Windows sẽ ghi đè toàn bộ bảng màu và cần một lượt kiểm riêng | Sau khi có màn hình thật để kiểm |
| 2 | ~~`--layout-container-max` = 1440px hay rộng hơn?~~ **Đã đóng 2026-10-08 (người dùng chốt):** bỏ trần bề rộng của `main` và gỡ token; khối form và mô tả `PageHeader` giữ trần riêng `--layout-form-w` — §6.1, §11 | Đã trả lời |
| 3 | Có khai token riêng cho mật độ (compact/comfortable) không? Hôm nay chỉ có một mật độ, đặc | Khi có yêu cầu thật |
| 4 | `--dur-fast` có làm chuyển tiếp sơn màu thành **cộc** ở chỗ nào không? Lượt rà 2026-09-23 đã bác được rủi ro *giật* bằng cấu trúc — mọi chỗ ăn khoá thời lượng chung của thư viện đều là chuyển tiếp sơn màu, và một chuyển tiếp sơn màu ngắn đi thì có ít khung hình phải vẽ hơn chứ không nhiều hơn. Phần chưa trả lời được là thẩm mỹ, và nó là một câu hỏi khác hẳn: gọn quá tay thì nhịp đổi màu mất cảm giác liên tục. 🛑 Đừng đọc chữ "đã rà" thành "đã xong cả hai" | Một lượt rà giao diện khi FE chạy được — cần mắt người trên ứng dụng thật, không lượt đọc tĩnh nào thay được |
| 5 | Nhịp `--dur-base` áp cho lớp nổi neo (§7 *Lớp nổi neo*) có **nặng tay** không? Các lớp nổi neo trước đây chạy nhịp mặc định của bản thư viện cũ, ngắn hơn hẳn; bậc mới đến từ vai chứ không từ một lần ai đó nhìn thấy chúng bung ra. Nếu nặng thì đường sửa là xem lại vai của các bậc ở §7, **không** phải khai một khoá riêng cho từng component — [`../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md`](../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) §Hệ quả | Cùng lượt rà giao diện với câu 4 |

## 11. Lịch sử quyết định

| Quyết định | Nội dung |
| --- | --- |
| Bảng màu khởi tạo | Tính từ công thức tương phản WCAG, không chọn bằng mắt. Mọi cặp chữ/nền đạt AA ở **cả hai** theme |
| Có theme tối ngay từ đầu | Lệch dự án tiền nhiệm (chỉ một theme sáng). Thêm theme tối sau tốn hơn nhiều lần, vì phải rà lại từng component |
| Ba bậc viền | Lệch dự án tiền nhiệm (hai bậc). Bậc `subtle` tách ra để không phải chọn giữa "vạch bảng quá đậm" và "viền card dưới chuẩn" |
| Bốn nét font, không hơn | Ở dự án tiền nhiệm có khai nét không nạp, hai chỗ tưởng khác nhau render giống hệt |
| Cỡ chữ khai bằng `rem` | Lệch dự án tiền nhiệm (`px`). `px` chặn tính năng phóng chữ của trình duyệt |
| Viền lỗi của control nhập là `--color-danger-border` | Một token cho mọi control (`Input`, `AuthField`, `DatePicker`, `Check`, `FileUpload`); `--color-danger` dành cho chữ và icon |
| Alpha đi qua biến kênh `*-rgb` | Cách duy nhất vừa giữ được token vừa tuân luật cấm `rgba()` literal |
| Bảng màu biểu đồ — chốt là **có** | Khai ở §2.8: tám màu thứ tự cố định, dải mức độ, dải hai chiều, đều đã chạy qua phép kiểm bằng máy ở cả hai theme. Cách vẽ khai ở [`Components/Chart.md`](./Components/Chart.md) |
| `Chart` là component bọc PrimeNG | Dạng `line` dùng component biểu đồ của PrimeNG; bốn dạng còn lại vẽ bằng HTML/CSS trong cùng lớp bọc. Cái giá: `line` vẽ canvas, không tự kế thừa token và không để lại gì cho trình đọc màn hình — bù bằng bảng số liệu bắt buộc ở [`Components/Chart.md`](./Components/Chart.md) |
| Bề rộng `Dialog` và `AuthCard` là token | Ba bậc `--layout-dialog-w-*` ở §6.1 lấy đúng ba bề rộng của `Dialog`. `AuthCard` không có giá trị riêng: `--layout-auth-w` là bậc `sm`, `--layout-auth-w-wide` là bậc `md` — hai khung căn giữa dùng một thang, không đẻ bậc mới |
| Bề rộng form trong trang là bí danh | `--layout-form-w` trỏ bậc `md` của `Dialog`, bậc dành cho form thông thường. Form trong trang không có giá trị riêng — cùng thang với hộp thoại và khung xác thực, không đẻ bậc mới |
| `--chart-1` ở theme tối **khác** `--color-brand` tối | Màu thương hiệu tối `#6ba1f0` có độ sáng 0,705, vượt dải cho phép của mark trên nền tối. Hạ một bậc thành `#3987e5`. Hai token cố ý khác nhau — xem §2.8 ràng buộc 1 |
| Thêm bậc `hover`/`active` cho `danger` | Hai spec `Button` và `IconButton` đã khai trạng thái hover cho biến thể `danger` trong khi token chưa tồn tại. Khai ở §2.5. Ba vai trạng thái còn lại cố ý **không** có bậc hover |
| Cặp bề mặt đảo cho `Tooltip` | Tooltip là lời chú, không phải mặt giấy để thao tác. Đảo màu thay cho thêm một đường viền nữa. Bề mặt duy nhất trong hệ được phép đảo — §2.6 |
| Điểm ngắt là biến SCSS `$bp-*` | `var()` không đọc được trong điều kiện `@media`. Giá trị vẫn chỉ quyết ở §6.3; `_spacing.scss` là nơi áp |
| Cỡ icon chuyển từ `Icons.md` sang đây | Bốn giá trị px sống ở hai nơi trong khi file này tự khai là nơi duy nhất giữ px thật. Nay có mốc `— định nghĩa gốc` ở §6.2 và một dòng trong [`../OWNERSHIP.md`](../OWNERSHIP.md) |
| `--tree-indent` khai bằng `px` | Nó là hình học của khung, không phải của chữ — cùng lý do với bo góc. 20px là biên hai đầu, xem §6.1 |
| Thang trung tính sáng nhạt đi một bậc | `bg` `#eef1f6` → `#f4f6fa`, `surface-2` `#e2e8f0` → `#eef1f6`, `surface-3` `#d5dde8` → `#e2e8f0`, `border-subtle` `#dbe1ea` → `#e4e9f0`. Thứ làm giao diện nặng là dải nền `th` và viền, không phải nền trang — nên cả thang nhấc lên cùng lúc, không chỉnh lẻ. Lý do đầy đủ và các phương án đã loại: [`../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md`](../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md) |
| Viền chỉ nhạt được tới `#7f8da3` | `border` `#7c8ba3` → `#7f8da3`. Ngưỡng chặn là **`border` trên `bg`**, không phải trên `surface`: nhạt thêm một bậc thì viền `Card` còn 2.99:1 và vi phạm SC 1.4.11. Biên do phép đo đặt, xem §2.3 |
| Theme mặc định là `light`, không theo hệ điều hành | Ứng dụng quản trị dùng cả ngày trong phòng sáng; cờ `prefers-color-scheme` của máy là tín hiệu yếu. Cái giá và điều kiện chấp nhận ở §8, quyết định đầy đủ ở [`../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md`](../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md) |
| Kích thước lớp nổi và vòng tròn `EmptyState` là bí danh | `Drawer`, `Toast`, `Tooltip`, lớp nổi thả xuống mượn thang `--layout-*` (§6.1); vòng tròn nền icon mượn `--sp-*` (§6.2). Không bậc mới nào được đẻ — năm chỗ từng giữ số thô nay trỏ về thang có sẵn |
| Thêm bậc `--dur-loop` | Thang §7 dừng ở `--dur-slow`, dành cho chuyển tiếp một lần; chỉ báo loading lặp cần thời lượng dài hơn hẳn. Việc thêm bậc do người dùng duyệt 2026-09-15; giá trị 5 × `--dur-slow` = `1600ms` chọn để giữ là bội của thang |
| Bỏ "hiện/ẩn tooltip" khỏi ô *Dùng ở đâu* của `--dur-fast` | **Giá trị `120ms` không đổi**; chỉ ví dụ dùng bị gỡ. [`Components/Tooltip.md`](./Components/Tooltip.md) chốt 2026-09-23 rằng thời lượng hiện/ẩn của tooltip do thư viện quyết, không đọc CSS — nên token này không tới được chỗ đó và ví dụ cũ là một tuyên bố sai. 🛑 Các ví dụ dùng còn lại ở §7 **chưa được đối chiếu với thư viện đang cài**, đừng đọc chúng như đã kiểm |
| `--layout-tooltip-w-max`, `--layout-popover-h-max` về giá trị riêng | `var(--layout-sidebar-w)` → `240px`, `var(--layout-dialog-w-sm)` → `420px`. Số không đổi; bỏ bí danh vì hai trần này không có lý do đổi cùng sidebar hay hộp thoại — bí danh theo số trùng là phụ thuộc ngầm (§6.1) |
| Token tầng 3 theo component khai ở §6.5 | Số px riêng của `Chart`, `FileUpload`, `ProgressBar`, `Stepper`, `Timeline` sống một chỗ, spec chỉ trỏ tên. `Timeline` không đọc token của `Stepper` (§1 luật cứng) — nó có bí danh riêng trỏ sang, khai tại §6.5 |
| Ô *Dùng ở đâu* của `--dur-base` nhận thêm lớp nổi neo | **Không giá trị nào đổi**; `200ms` và `120ms` giữ nguyên. Ba lớp nổi ở [`Components/Menu.md`](./Components/Menu.md), [`Components/DatePicker.md`](./Components/DatePicker.md), [`Components/Autocomplete.md`](./Components/Autocomplete.md) nối về bậc này thay vì bậc `--dur-fast` mà con số mặc định của thư viện đang trùng. Chọn theo vai giữ cho `--dur-fast` thuần sơn màu; treo ba lớp nổi lên nó là cho nó một vai `transform`/`opacity` thứ hai. Cái giá — ba component bung và thu chậm hơn hôm nay — người dùng duyệt 2026-09-23; câu hỏi thẩm mỹ còn mở ở §10 câu 5. Quyết định đầy đủ: [`../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md`](../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) |
| Ngoại lệ có tên của luật *chỉ animate `transform` và `opacity`*: pha ra của `Toast` | Luật §7 không đổi cho mọi chỗ khác. Pha ra của `Toast` được gập chiều cao bằng thuộc tính bố cục vì spec của nó đòi những toast còn lại trượt lên, và chỉ `transform`, `opacity` thì không làm được nếu không đo bằng JavaScript — chấp nhận một lệch về hiệu năng, giới hạn ở ngăn chứa tối đa ba toast, thay vì một chuyển động loại 3 mà không nửa giảm chuyển động nào với tới. Chọn ghi lệch thay vì đòi đè keyframes vì [ADR-0123](../adr/0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) quyết định 1 giữ keyframes của thư viện. `design-expert` quyết 2026-11-06 khi thi công phần `docs/Design/` của ADR đó; người dùng chưa duyệt riêng |
| `prefers-reduced-motion` thi hành bằng khối CSS toàn cục, không provider (2026-11-06, người dùng chốt) | Cũ: hai nửa — khối CSS toàn cục cộng nửa provider, bộ chạy rỗng chọn một lần lúc khởi động khi cờ bật ([ADR-0080](../adr/0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md)); cái giá *hai nửa lệch nhau khi cờ đổi giữa phiên* người dùng đã chấp nhận khi chốt. Mới: khối CSS toàn cục là lớp chính, `safe` mặc định của thư viện là lớp phụ cho component PrimeNG; app không khai provider hoạt ảnh nào; loại 2 không lớp nào phủ và không được đặt — §7. Vì sao: từ PrimeNG 21 không component nào của thư viện chạy bằng bộ chạy của Angular, nên nửa provider không còn thay gì trên màn hình. Cái giá: giảm chuyển động không còn dòng mã nào của app chịu trách nhiệm và không máy nào canh — luật F63 mang `🕳️`; ba giới hạn của `safe` ở §7. Quyết định đầy đủ: [ADR-0124](../adr/0124-go-nua-provider-hoat-anh-giam-chuyen-dong-dua-vao-css-toan-cuc-va-safe-f39-het-ap-dung.md) |
| Bộ chuyển động của lớp nổi neo có một chủ: §7 (2026-11-06, người dùng chốt) | **Không giá trị nào đổi.** Cũ: spec `Menu`, `Autocomplete`, `DatePicker` mỗi spec giữ quyền đặt thời lượng và đường cong bung/thu ([ADR-0075](../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) quyết định 1, phần phân loại), ba spec chép cùng một bộ ba giá trị; ô chọn số dòng của `Pagination` nhận bộ đó mà không spec nào nói. Mới: §7 *Lớp nổi neo* là nơi duy nhất quyết, ô *Dùng ở đâu* của `--dur-base`, `--ease-decelerate`, `--ease-accelerate` viết theo vai; bốn spec trỏ về. Vì sao: bộ giá trị áp bằng một khối đè trên lớp pha chung của mọi lớp nổi neo ([ADR-0123](../adr/0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) quyết định 4), nên nhiều spec cùng quyết là nhiều nguồn cho một giá trị. Câu chữ hình dạng đổi theo keyframes thư viện giữ nguyên (ADR-0123 quyết định 1): *mờ dần* thành *mờ dần kèm co giãn nhẹ* |
| Ô nhập chữ: một đường biên thay cho vòng + viền (2026-10-05, người dùng chốt) | **Không giá trị token nào đổi, không token mới.** Cũ: ô nhập dùng vòng `--color-focus` cách ô 2px **và** viền chuyển `--color-brand` — [`Components/Input.md`](./Components/Input.md) gọi đó là "vòng cho bàn phím, viền cho chuột"; ô đang lỗi nhận focus và trạng thái tự điền không ai khai. Mới: một đường biên theo hai trục — hợp lệ có focus `--border-w-strong` `--color-text`, lỗi có focus `--border-w-accent` giữ `--color-danger-border`; không vòng ngoài, không `--color-focus`; nền tự điền giữ `--color-surface` — §2.6 *Ô nhập chữ*. Vì sao: trình duyệt khớp `:focus-visible` ở ô nhập chữ cả khi bấm chuột, nên hai dấu hiệu luôn hiện cùng lúc; người dùng phản ánh vòng đỏ lồng vòng xanh và nền xanh nhạt lạ ở màn đăng nhập, rồi chọn kiểu "viền tối, không xanh" và giữ màu lỗi khi focus. Cái giá: không đạt trọn SC 2.4.13 (mức AAA), số ở §2.6 |
| Khe nhãn → ô nhập là `--sp-4` ở mọi form (2026-10-05, người dùng chốt) | `--sp-5` → `--sp-4` ở bảng §4; quy tắc 2 của §4 nới nhóm "cùng nhóm" tới `--sp-4`. Vì sao: bảng §4 cũ gán `--sp-5` trong khi quy tắc 2 cùng mục nói cùng nhóm chỉ tới `--sp-3`, và code đi theo cả hai phía (`FormRow` một bậc, `AuthField` một bậc khác). Với `--sp-4`, nhãn cách ô của nó 8px còn khe giữa hai trường là 16px (`--sp-6`) — gấp đôi, đủ để nhãn đọc là của ô bên dưới; 8px cũng là khoảng nhãn → ô quen thuộc ở các bộ form phổ biến |
| `Footer` của khung xác thực nằm trong khung nhìn (2026-10-06, người dùng chốt) | **Không token nào đổi, không token mới.** Cũ: vùng `main` của [`Components/AuthCard.md`](./Components/AuthCard.md) tự cao `100dvh`, `Footer` xếp sau nó — nên `Footer` luôn nằm dưới mép khung nhìn và trang luôn cuộn thêm đúng bằng chiều cao `Footer`. Mới: một cột cao **tối thiểu** `100dvh` gồm `main` và `Footer`; `Footer` ở đáy cột, trang chỉ cuộn khi khung nhìn thấp hơn nội dung; ngưỡng cuộn tính theo nội dung, không phải một con số px — [`Components/AuthCard.md`](./Components/AuthCard.md) §Responsive. Áp cho mọi màn dùng khung xác thực. Vì sao: chân trang thấy được ngay mà không có thanh cuộn thừa — thông lệ của các trang đăng nhập phổ biến; `Footer` vẫn thuộc luồng cuộn, không `position: fixed`, đúng quyết định chung ở [`Components/Footer.md`](./Components/Footer.md) §Khi nào dùng |
| Liên kết phụ của khung xác thực (2026-10-06) | **Không token nào đổi.** Hai luật component đổi theo màn đăng nhập hai chế độ của [ADR-0116](../adr/0116-ma-don-vi-nghiep-vu-lay-tu-cau-hinh-ban-cai-form-hai-o-van-hanh-qua-lien-ket-rieng.md). (1) Ngoại lệ *thẻ `<a>` mượn hình thức nút* ở [`Components/Button.md`](./Components/Button.md) §Khi nào dùng nhận thêm chỗ thứ tư: liên kết phụ đứng riêng dưới form của `AuthCard`, chỉ biến thể `ghost`. Vì sao: liên kết chuyển chế độ là điều hướng nên phải là `<a>`, và khu Design không có component liên kết riêng; một liên kết chữ đứng riêng chỉ có ở chỗ này nên không đẻ component mới. (2) Thứ tự Tab ở [`Components/AuthCard.md`](./Components/AuthCard.md) §Accessibility: cũ — ô nhập → liên kết phụ → nút chính; mới — ô nhập → nút chính → liên kết phụ. Vì sao: dòng cũ ngược với thứ tự vùng của chính tệp đó (form, trong đó có nút chính, đứng trước liên kết phụ); thứ tự Tab phải theo thứ tự đọc |
| Thẻ `<a>` mượn hình thức nút thành `a[app-button]` (2026-10-06, người dùng chốt Q62) | Thay ngoại lệ *thẻ `<a>` mượn hình thức nút* ở dòng liền trên: liên kết dùng chính `Button` qua selector `a[app-button]`, không còn lớp nút gắn tay lên `<a>`. Dòng cũ giữ nguyên làm lịch sử; chi tiết ở [`Components/Button.md`](./Components/Button.md) §Liên kết mang hình thức nút |
| Hover của ô nhập chữ không đổi viền (2026-10-06, người dùng chốt) | **Không token nào đổi, không token mới.** Cũ: dòng `hover` của §2.6 *Ô nhập chữ* chỉ nói "chỉ áp khi ô không có focus", hình thức để spec component khai; [`Components/Input.md`](./Components/Input.md) và [`Components/AuthField.md`](./Components/AuthField.md) khai "viền đậm thêm một bậc", [`Components/TreeSelect.md`](./Components/TreeSelect.md) khai viền `--color-border-strong` "đậm thêm". Mới: ô hợp lệ hover chỉ đổi con trỏ sang `text`, viền giữ `--border-w` · `--color-border-strong`; ô đang lỗi hover không đổi gì, giữ `--border-w-strong` · `--color-danger-border`; ô có focus giữ đường biên focus — §2.6. Vì sao: thang trung tính không có bậc viền nào đậm hơn `--color-border-strong` (§2.3), nên "đậm thêm một bậc" không thi công được — code đặt lại đúng màu nghỉ; và khối hover đè viền lỗi là lỗi người dùng báo 2026-10-06, lỗi 7 ở [ADR-0117](../adr/0117-luoi-giao-dien-playwright-do-dom-theo-spec-design-axe-aa-api-gia-anh-chi-tham-khao.md) §Bối cảnh |
| Phần tử nào dùng họ font nào (2026-10-06, người dùng chốt) | **Không token nào đổi.** Cũ: §3.1 chỉ khai giá trị hai token, không nói phần tử nào dùng token nào. Mới: mọi phần tử hiển thị chữ dùng `--font-sans`, kể cả `button`, `input`, `select`, `textarea`; `--font-mono` chỉ cho chữ đơn cách — §3.1. Vì sao: trình duyệt không cho bốn phần tử biểu mẫu kế thừa họ font của trang; nhãn nút chính ở màn đăng nhập đã rơi về font hệ thống, lỗi 8 ở [ADR-0117](../adr/0117-luoi-giao-dien-playwright-do-dom-theo-spec-design-axe-aa-api-gia-anh-chi-tham-khao.md) §Bối cảnh |
| Khoá dịch không hiện ra thành chữ (2026-10-06, người dùng chốt) | **Không token nào đổi.** Thêm một dòng ❌ ở §9. Cũ: khu Design chỉ đòi mỗi câu có khoá i18n ([`Templates/Screen.md`](./Templates/Screen.md) mục *Câu chữ*), chưa dòng nào cấm khoá hiện ra thành chữ. Vì sao: làm nguồn cho nhánh *chuỗi mang hình khoá dịch* của phép đo A2 ở [ADR-0117](../adr/0117-luoi-giao-dien-playwright-do-dom-theo-spec-design-axe-aa-api-gia-anh-chi-tham-khao.md) — phép đo chưa có dòng spec làm nguồn thì không bật |
| Bỏ trần bề rộng `main` (2026-10-08, người dùng chốt) | `--layout-container-max` `1440px` → **gỡ token** khỏi §6.1. Vì sao: màn chính của Core là bảng dữ liệu; trần 1440px để lại hai dải trống trên màn rộng trong khi bảng nhiều cột thiếu chỗ — câu hỏi mở §10 câu 2. Thứ cần trần vì lý do đọc đã có trần riêng `--layout-form-w`; `Footer` — nơi dùng thứ hai của token — chuyển xuống đáy `Sidebar` cùng ngày. Cái giá: ở màn ≥ `$bp-xl`, bảng ít cột trải rất rộng; chấp nhận vì bảng đọc theo cột, không theo dòng văn |
| Thêm `--toolbar-search-w-max` = `360px` (2026-10-08, người dùng chốt) | Token mới ở §6.5. Cũ: ô tìm co giãn hết phần còn lại của hàng `Toolbar`. Mới: trần `360px` từ `$bp-md`, dưới ngưỡng đó trải hết hàng. Vì sao: hàng `Toolbar` nay gánh thêm nút lọc và nút chính của màn; một ô tìm rộng vô hạn đẩy chúng ra tận mép và đọc như một thanh địa chỉ. Cách chọn số: §6.5 |
| Thêm `--data-table-scroll-h-min` = `280px` (2026-10-08, người dùng chốt) | Token mới ở §6.5. Cũ: [`Components/DataTable.md`](./Components/DataTable.md) §Kích thước chỉ nói "tối thiểu đủ chỗ cho vài dòng", không có con số. Mới: sàn `280px` cho vùng cuộn khi khối danh sách lấp chiều cao còn lại; màn thấp hơn mức đó thì `main` cuộn. Vì sao cần sàn: chỉ thân bảng cuộn, nên trên màn thấp bảng có thể co còn một hai dòng mà không gì báo. Cách chọn số: §6.5 |
| Khung ứng dụng và màn danh sách (2026-10-08, người dùng chốt) | **Không token màu nào đổi.** Bảy thay đổi, nguồn sống ở từng file: (1) khung cao đúng `100dvh`, `main` là vùng cuộn dọc; màn danh sách lấp đúng chiều cao nên chỉ thân bảng cuộn — [`Screens/00-khung-ung-dung.md`](./Screens/00-khung-ung-dung.md), [`Templates/ListScreen.md`](./Templates/ListScreen.md) §4; (2) `Footer` chuyển xuống đáy `Sidebar` — [`Components/Footer.md`](./Components/Footer.md), [`Components/Sidebar.md`](./Components/Sidebar.md); (3) `Topbar` bỏ tiêu đề tuyến — [`Components/Topbar.md`](./Components/Topbar.md); (4) màn danh sách: `PageHeader` chỉ còn tiêu đề và mô tả, nút chính sang đầu phải `Toolbar` — [`Components/PageHeader.md`](./Components/PageHeader.md), [`Components/Toolbar.md`](./Components/Toolbar.md); (5) `Toolbar` + `DataTable` + dải phân trang chung một khung viền `--color-border-strong`, vạch ngăn `--color-border-subtle` — [`Components/DataTable.md`](./Components/DataTable.md); (6) có từ một trường lọc thì luôn là nút lọc mở [`Components/FilterPanel.md`](./Components/FilterPanel.md) trong `Drawer` — bỏ ngưỡng "1–2 trường nằm thẳng trong dải"; (7) cột hành động dùng chung của `DataTable`, hiện khi rê chuột hoặc có focus trong dòng. Vì sao: người dùng soát giao diện màn danh sách và chọn trọn bộ đề xuất của lượt điều tra 2026-10-08 |
| Ngoại lệ điểm ngắt cho TypeScript (2026-10-08, kiến trúc sư chốt luật F58) | **Không giá trị `$bp-*` nào đổi.** §6.3 nhận một ngoại lệ có tên cho câu "không file nào khác khai lại con số": TypeScript đọc điểm ngắt từ đúng một tệp, `diem-ngat.ts`. Vì sao: `matchMedia` cần điểm ngắt mà điều kiện `@media` của SCSS không đưa sang được TypeScript, và review FE vòng 1 thấy ba component tự khai lại mỗi chỗ một kiểu. Luật và hình dạng tệp: [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §7.1; quyết định: [`../adr/nhat-ky-quyet-dinh.md`](../adr/nhat-ky-quyet-dinh.md), dòng 2026-10-08 "Điểm ngắt trong TypeScript" |
| Ghi `--data-table-scroll-h-floor` vào §6.5 (2026-10-09, người dùng chốt) | **Không giá trị nào đổi, không số mới.** Biến đã chạy trong mã từ 2026-10-08 nhưng chưa có dòng nào trong file này. Nó là biến tầng 3 duy nhất mà một tệp ngoài component ghi lên, và cố ý không khai ở `:root` — khai ở đó thì mọi bảng có sàn, kể cả bảng con trong màn cuộn. Ghi ra để màn danh sách mới không tự đặt một giá trị khác và bẻ sàn vùng cuộn |
