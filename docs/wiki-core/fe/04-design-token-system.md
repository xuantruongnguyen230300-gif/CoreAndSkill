---
kind: luat
scope: core
verified: 2026-11-05
---

# 04. Hệ design token

> **Hiện trạng:** hàng ghi `✅ CÓ THẬT` ở §10 đã đối chiếu với `src/` ngày 2026-11-05, neo ở từng hàng. Các khối SCSS ở §3.2, §4.1, §6.2 là minh hoạ cơ chế, không trích code đang chạy; khối ở §7.1 là khuôn — danh sách sub-preset thật đọc ở tệp nguồn. Hiện trạng repo: [`../../README.md`](../../README.md) mục *Trạng thái repo*.
>
> **Giá trị token — tên khoá, mã màu, thang chữ — nằm ở [`../../Design/DESIGN.md`](../../Design/DESIGN.md), không nằm ở file này.** File này mô tả *cơ chế* và *luật*. Mọi tên token viết dưới đây là **minh hoạ cách đặt tên**, không phải danh sách chốt.
>
> Cách viết style cho một component: [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md).

---

## 1. Token là gì và giải quyết vấn đề gì

Token là **một quyết định thiết kế có tên**. Không phải một biến CSS cho gọn — mà là chỗ duy nhất mà một quyết định được ghi.

Không có token, một câu hỏi rất thường gặp trở nên không trả lời được: *"màu viền của ô nhập là màu gì?"*. Câu trả lời trở thành "tuỳ màn hình" — vì mỗi màn có một giá trị hơi khác, và không ai biết cái nào đúng.

| Không có token | Có token |
| --- | --- |
| Đổi màu thương hiệu = tìm và sửa hàng trăm chỗ, sót vài chỗ | Sửa một dòng |
| Chế độ tối = viết lại toàn bộ style | Khai lại tập token dưới một điều kiện |
| Sản phẩm thứ hai đổi bảng màu = fork toàn bộ style | Thay tập giá trị, giữ nguyên code |
| "Màu này lấy ở đâu ra?" = không ai biết | Tên token trỏ về `Design/` |

---

## 2. Chiều nguồn — khu `Design/` là nguồn, code đuổi theo

> Đây là chiều **duy nhất**. Không có ngoại lệ theo pha, không có chiều riêng cho lúc mới dựng.

```
DESIGN.md (khu Design)   →   styles toàn cục (:root)   →   component SCSS
       (nguồn)                  (bản sao có kiểm)          (chỉ dùng var())
```

Vì sao chiều này chứ không phải chiều ngược lại (code là nguồn, tài liệu chép theo): một giá trị màu là **quyết định thiết kế**, và quyết định phải nằm ở nơi người ra quyết định làm việc. Để code làm nguồn thì mỗi lần một dev chọn đại một sắc độ, quyết định thiết kế đã đổi mà không ai duyệt.

Công cụ trích token từ code ra tài liệu — nếu có — chỉ dùng để **ghi nhận hiện trạng** trong một đợt đồng bộ lại, không phải chiều làm việc thường ngày, và không bao giờ quyết định giá trị mới.

**Hệ quả cho người thi công:** thấy trong `Design/` một token chưa có trong code thì thêm vào code. Thấy trong code một giá trị chưa có tên trong `Design/` thì **dừng lại và hỏi**, đừng tự đặt tên — token đặt tên tuỳ tiện sẽ tồn tại song song với token thật cho cùng một màu, và đó là cách một hệ token chết.

---

## 3. Bốn họ token

| Họ | Ví dụ tên | Quy tắc đặt tên | Bẫy |
| --- | --- | --- | --- |
| **Màu** | `--surface`, `--text`, `--brand`, `--line`, `--warn` | Đặt theo **vai trò**, không theo giá trị | `--xanh-duong` là tên chết ngay khi thương hiệu đổi màu |
| **Chữ** | `--font-body`, `--fs-300`, `--lh-tight`, `--fw-semibold` | Thang rời rạc, không giá trị tự do | Cho phép cỡ chữ tuỳ ý là mất luôn nhịp thị giác |
| **Khoảng cách** | `--sp-1` … `--sp-8` | Thang nhân từ một đơn vị gốc | Trộn thang với giá trị lẻ thì thang mất nghĩa |
| **Bóng & bo góc** | `--shadow-0` … `--shadow-4`, `--radius-sm`, `--radius-md` | Thang có thứ tự — bóng đánh số theo độ cao, bo góc theo cỡ; bậc nào dùng ở đâu khai ở cột *Dùng ở đâu* của [`DESIGN.md`](../../Design/DESIGN.md) §5.1, §5.2 | Đặt tên theo một component (`--shadow-card`) thì hai component cùng độ cao phải giữ hai token cho một giá trị |

### 3.1 Đặt tên theo vai trò — vì sao đây là quy tắc quan trọng nhất

`--brand` và `--xanh-duong` cùng trỏ tới một mã màu ở thời điểm hôm nay. Khác nhau ở chỗ: khi thương hiệu đổi sang màu cam, `--brand` vẫn đúng còn `--xanh-duong` biến thành lời nói dối nằm trong code.

Đây không phải chuyện thẩm mỹ. Sản phẩm thứ hai dựng trên Core này gần như chắc chắn có bảng màu khác — nếu tên token mang giá trị, mọi tên đều sai ngay ngày đầu, và người ta sẽ giữ tên sai vì đổi tên đắt hơn.

Cùng lý do, tránh tên gắn với **vị trí duy nhất**: `--mau-nen-sidebar` chỉ dùng được ở một chỗ. Đặt là `--surface-nav` thì dùng lại được cho thanh điều hướng thứ hai.

### 3.2 Tầng nguyên thuỷ và tầng ngữ nghĩa

> 📖 Ba tầng token và ai được dùng tầng nào: đọc [`../../Design/DESIGN.md`](../../Design/DESIGN.md) §1.

Tầng nguyên thuỷ là **mã màu thô và bậc px**, không phải một tập biến có tên: nó chỉ xuất hiện trong file khai token, làm giá trị của token ngữ nghĩa. Component chỉ đọc token ngữ nghĩa và token riêng của chính nó.

```scss
:root {
  /* Tầng ngữ nghĩa — mã thô (tầng nguyên thuỷ) chỉ nằm ở đây, làm giá trị */
  --color-brand: <một mã màu>;
  --color-focus: <một mã màu>;
}
```

Lợi ích: khi chế độ tối cần `--color-brand` sáng hơn để đủ tương phản, chỉ giá trị trong khối theme tối đổi; component đọc `var(--color-brand)` không sửa gì. Component viết thẳng mã màu thì không có chỗ nào để can thiệp.

Cái giá: hai token ngữ nghĩa trùng giá trị giữ hai bản mã thô — ở theme sáng `--color-focus` trùng `--color-brand` ([`DESIGN.md`](../../Design/DESIGN.md) §2.6). Đổi sắc độ thương hiệu phải sửa cả hai, và sót một chỗ thì không có gì báo.

---

## 4. Chế độ sáng và tối

### 4.1 Cơ chế

```scss
:root {                       /* mặc định: sáng */
  --color-surface: <giá trị cột "sáng" ở DESIGN.md>;
  --color-text:    <giá trị cột "sáng" ở DESIGN.md>;
}

:root[data-theme='dark'] {    /* lựa chọn tường minh của người dùng */
  --color-surface: <giá trị cột "tối">;
  --color-text:    <giá trị cột "tối">;
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) {   /* theo hệ điều hành, trừ khi người dùng đã chọn sáng */
    --color-surface: <giá trị cột "tối">;
    --color-text:    <giá trị cột "tối">;
  }
}
```

Ba trạng thái, không phải hai: **sáng**, **tối**, và **theo hệ điều hành** — người dùng chọn được cả ba.

**Mặc định là sáng.** Người mở ứng dụng lần đầu mà chưa từng chọn gì nhận theme sáng; ứng dụng **không** đọc cờ tối của hệ điều hành để quyết hộ. Lý do và cái giá: [`../../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md`](../../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md), [`../../Design/DESIGN.md`](../../Design/DESIGN.md) §8.

`:root:not([data-theme='light'])` trong khối media là chi tiết bắt buộc: thiếu nó thì người dùng chọn "sáng" trên một máy đang để chế độ tối vẫn nhận giao diện tối, và lựa chọn của họ trông như bị lờ đi.

### 4.2 Ba luật đi kèm

1. **Component không được biết đang ở chế độ nào.** Không có `@media (prefers-color-scheme: dark)` trong SCSS của component. Chế độ tối được xử lý trọn vẹn ở tầng token; component chỉ đọc `var()`. Vi phạm luật này là chế độ tối rò rỉ ra khắp codebase và không ai gom lại được nữa.
2. **Bóng đổ phải khai lại, không chỉ màu.** Bóng đen trên nền tối gần như vô hình; theme tối bù bằng bề mặt sáng dần, và mỗi bậc `--shadow-*` mang giá trị riêng cho từng chế độ ([`DESIGN.md`](../../Design/DESIGN.md) §5.2).
3. **Tương phản phải đo lại ở cả hai chế độ.** Một cặp màu đạt AA ở chế độ sáng không bảo đảm gì ở chế độ tối. Xem [`15-accessibility.md`](15-accessibility.md) §4.

### 4.3 Lưu lựa chọn ở đâu

Lựa chọn chế độ là **tuỳ chọn hiển thị của một máy**, không phải dữ liệu tài khoản. Lưu ở `localStorage` là đúng chỗ, và là một trong số ít thứ được phép lưu ở đó ([`14-security.md`](14-security.md) §6).

Bẫy khi thi công: đọc `localStorage` và đặt thuộc tính `data-theme` phải xảy ra **trước khi khung hình đầu tiên vẽ**, nếu không người dùng thấy một nháy trắng rồi mới sang tối. Đặt ở `app.config.ts` sau khi Angular khởi động đã là quá muộn — việc này thuộc về một đoạn script nhỏ chạy sớm trong tài liệu HTML gốc.

Hệ quả của mặc định sáng (§4.1): khi chưa có lựa chọn nào được lưu, thuộc tính phải mang giá trị **sáng**. **Vắng** thuộc tính nghĩa là theo hệ điều hành — đúng thứ mặc định không được làm. Cách áp: [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §3.5.

---

## 5. Vì sao cấm hex literal trong SCSS của component (luật F6)

Một mã hex trong file style của component là **một bản sao của một quyết định**. Ba hệ quả, xếp theo mức độ khó phát hiện:

1. **Đổi theme sẽ sót đúng những chỗ đó.** Tìm và sửa được, nhưng lần sau lại sót lần nữa.
2. **Chế độ tối không chạm tới được.** Token đổi, hex thì không — kết quả là một mảng màu sáng nằm giữa giao diện tối.
3. **Sản phẩm thứ hai mang theo màu của sản phẩm thứ nhất.** Đây là hệ quả đắt nhất và lâu lộ nhất.

Nơi **duy nhất** được phép giữ literal là chỗ khai token toàn cục — vì ở đó literal là **định nghĩa**, không phải bản sao.

> ⚠️ Ở dự án tiền nhiệm, luật này được dọn tay hai lần và tự tái sinh cả hai lần: hex mới xuất hiện ngay ở đợt màn hình kế tiếp. Nó chỉ dừng lại khi có script kiểm. **Luật chưa có cổng chỉ còn người canh** — dòng 🕳️, cột *Ép bằng gì* ở [`../../RULES-FE.md`](../../RULES-FE.md).

---

## 6. Vì sao cấm cả `rgb()` / `rgba()` literal (luật F7) — và vì sao miễn trừ theo CÚ PHÁP

### 6.1 Cổng hex mù đúng một nửa bài toán

Cổng chỉ tìm `#rrggbb` sẽ bỏ lọt **cùng một quyết định màu** viết bằng thập phân. `rgba(15, 91, 215, .08)` chính là màu thương hiệu pha loãng — cùng một quyết định, cùng một bản sao, chỉ khác cách viết.

Ở dự án tiền nhiệm, chỗ nặng nhất phát hiện được là **nền của mục menu đang chọn** — thấy ở mọi màn hình, và dòng ngay bên dưới nó đã dùng `var()` cho một màu khác. Tức là **sót**, không phải chủ đích.

### 6.2 Miễn trừ theo cú pháp, tuyệt đối không theo giá trị

Dạng duy nhất được tha:

```scss
background: rgb(var(--brand-rgb) / 8%);   /* đọc token ra rồi pha alpha — HỢP LỆ */
background: rgba(15, 91, 215, .08);       /* bản sao của một quyết định — CẤM */
background: rgba(0, 0, 0, .5);            /* CẤM — xem lý do dưới */
```

**Vì sao `rgba(0, 0, 0, .5)` cũng bị cấm dù trông vô hại:** nó vẫn là một quyết định thiết kế. Độ tối của lớp phủ sau modal là thứ nhà thiết kế chọn, thứ phải đổi ở chế độ tối, và thứ sản phẩm thứ hai có thể muốn khác. Nó thuộc `Design/` như mọi màu khác — tên nó là `--color-overlay` ([`DESIGN.md`](../../Design/DESIGN.md) §2.6), không phải "đen 50%".

**Vì sao miễn trừ phải theo cú pháp:** miễn trừ theo giá trị đòi hỏi một danh sách "giá trị vô hại". Danh sách đó không có tiêu chí khách quan nào để từ chối thêm một giá trị nữa, nên nó sẽ dài ra sau mỗi lần ai đó thấy phiền — và tới lúc nào đó, cổng tha nhiều hơn bắt. Miễn trừ theo cú pháp thì khác: dạng `rgb(var(--x) / a)` **không mang giá trị màu nào**, nên nó không có gì để lệch khi đổi theme. Đó là một tiêu chí đúng vĩnh viễn, không phải một danh sách phải bảo trì.

### 6.3 Bẫy khi viết cổng này

Phải **gỡ dạng được phép ra khỏi dòng trước, rồi mới hỏi lại dòng còn lại**. Nếu chỉ loại bỏ những dòng có chứa dạng hợp lệ, một dòng vừa có `rgb(var(--x) / a)` hợp lệ vừa có một literal trần sẽ được tha nhầm nguyên dòng. Đây là bẫy đã dính thật và được phát hiện bằng canary ở dự án tiền nhiệm.

---

## 7. Ánh xạ token sang thư viện UI

PrimeNG có hệ theming riêng. Để nó chạy song song với token của mình là có hai hệ màu cùng tồn tại và không ai biết chỗ nào thắng — triệu chứng là "sửa token mà nút không đổi màu".

Cách đúng: một **preset styled** ở `core/theme/`, dựng từ preset Aura bằng `definePreset`, đăng ký một lần lúc cấu hình app, và **không** để theme mặc định của thư viện chạy kèm. Preset **không giữ mã màu nào** — nó chỉ trỏ tên biến CSS của mình:

| Việc | Cơ chế |
| --- | --- |
| Tầng được ghi đè | Tầng **semantic**: primary, highlight, formField, content, overlay, text, mask nằm dưới `colorScheme`, mỗi màu trỏ một `var(--color-*)` — `mask` chỉ mang khoá `background`, lớp phủ sau hộp modal, trỏ `var(--color-overlay)`; `focusRing` nằm ngoài `colorScheme` vì dùng chung hai chế độ — màu `var(--color-focus)`, hình học outline + offset của [`DESIGN.md`](../../Design/DESIGN.md) §2.6 |
| Thời lượng chuyển động của thư viện | Cũng ở tầng semantic, nhưng **ngoài** `colorScheme` vì nó không đổi theo chế độ sáng/tối: `transitionDuration` (mọi chuyển tiếp nhỏ) và `mask.transitionDuration` (backdrop của lớp nổi modal), mỗi giá trị trỏ một `var(--dur-*)`. Hai khoá, không phải một khoá cho mỗi component — mọi sub-preset đều đổ về đây; xem §7.2 |
| Thang màu 50–950 của preset | **Không** dựng. Tầng semantic đã trỏ token của mình thì không còn chỗ nào cần thang đó |
| Component token còn trỏ thẳng palette | Đè riêng từng chỗ, cũng bằng `var(--color-*)` |
| Chế độ tối của thư viện | Tắt: `providePrimeNG` đặt `darkModeSelector: false`. Biến `--color-*` đã đổi theo `data-theme` (§4.1), nên component thư viện đổi theo mà không cần cơ chế tối thứ hai |
| Thứ tự lớp CSS | Bật `cssLayer`; lớp của thư viện xếp **trước** style của app, để style của mình thắng mà không cần `!important` |

### 7.1 Cú pháp import preset — ghép từng sub-preset, không import object `Aura` gộp

`@primeuix/themes/aura` export default một object gộp tĩnh khoảng 90 sub-preset — mỗi loại control PrimeNG một sub-preset (`button`, `toast`, `message`, `datatable`...). Import object đó kéo theo theme của toàn bộ ~90 component vào bundle initial, kể cả những component chưa màn nào dùng — đã đo thấy làm bundle initial vượt ngân sách cảnh báo của luật F14 ([`../../RULES-FE.md`](../../RULES-FE.md)). Quyết định và số liệu đo: [`../../adr/0038-preset-primeng-tung-sub-theo-phan-dung.md`](../../adr/0038-preset-primeng-tung-sub-theo-phan-dung.md).

Cú pháp đã chốt: package tách sẵn export theo sub-path cho từng sub-preset — `@primeuix/themes/aura/<x>`, với `<x>` là tên component viết thường (`base`, `button`, `toast`, `message`...). `core/theme/prime-preset.ts` ghép TỪNG sub-preset đó thành preset gốc bằng cách spread, rồi mới truyền vào `definePreset` cùng phần override token:

```typescript
import { definePreset } from '@primeuix/themes';
import AuraBase from '@primeuix/themes/aura/base';
import AuraToast from '@primeuix/themes/aura/toast';
// Thêm một dòng import tương tự cho mỗi sub-preset mới cần — xem luật F29 dưới đây.

const AURA_SUBSET = {
  ...AuraBase, // token gốc (primitive + semantic) — luôn cần, mọi sub-preset khác tham chiếu tới
  components: {
    toast: AuraToast, // tập sub-preset phải liệt kê: luật F29
  },
};

export const CORE_PRIME_PRESET = definePreset(AURA_SUBSET, {
  /* phần override semantic — xem bảng trên */
});
```

Tập sub-preset phải có — gồm cả component con mà component PrimeNG được import dựng bên trong — là **luật F29** ở [`../../RULES-FE.md`](../../RULES-FE.md) §7, lý do ở [`../../adr/0056-f29-tinh-component-con-duoc-dung-ben-trong.md`](../../adr/0056-f29-tinh-component-con-duoc-dung-ben-trong.md). Danh sách sub-preset thật cần hôm nay đọc trực tiếp ở `src/FE/src/app/core/theme/prime-preset.ts` — không chép lại danh sách đó vào tài liệu này (luật `.claude/CLAUDE.md` §6: nội dung đếm được bằng lệnh thì không chép tay).

⚠️ **Cái giá phải trả:** thiếu một sub-preset khi thêm component mới không gây lỗi biên dịch — component vẫn chạy nhưng **sai theme** (dùng token mặc định của PrimeNG thay vì `var(--color-*)`). Cổng F29 ([`../../RULES-FE.md`](../../RULES-FE.md)) bắt sub-preset thiếu cho component PrimeNG import trực tiếp; vế *component con thật sự dựng bên trong* chỉ `prime-preset.spec.ts` canh.

**Hệ quả:** literal màu chỉ còn ở tệp khai token (`_tokens.scss`, [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §3.3). Đổi bảng màu là sửa một nơi; CSS của mình và component thư viện không thể vẽ hai màu khác nhau cho cùng một token, vì cả hai đọc cùng một biến.

### 7.2 Thời lượng chuyển động — nối ở gốc, và đè trên lớp chuyển động khi gốc không với tới

Thời lượng **không** đi theo khuôn "mỗi component một chỗ đè" của màu, vì hình dạng của nó ở tầng dưới khác hẳn: mọi sub-preset đều khai `root.transitionDuration` trỏ tới **một trong hai** tên — `{transition.duration}` hoặc `{form.field.transition.duration}` — và Aura lại khai nhánh thứ hai bằng chính nhánh thứ nhất. Hai nhánh hội tụ, nên nối ở gốc là **một** dòng và mọi component bọc thêm về sau tự thừa hưởng. Khai thêm nhánh `formField` cho "dễ đọc" là dựng nguồn thứ hai cho cùng một giá trị — đúng thứ mục này cấm.

Phía kia của ranh giới là các component mà thời lượng chuyển động **không** đi qua token nào của preset: thư viện viết cứng nó trong khai báo `animation` của **lớp chuyển động theo pha** — một lớp gắn lúc vào, một lớp gắn lúc ra. Token vẫn tới được, vì bộ chạy chuyển động của thư viện, khi không được truyền thời lượng, đọc **giá trị đã tính** của phần tử — và trình duyệt đã giải `var()` trước đó. Nên luật **F62** ([`../../RULES-FE.md`](../../RULES-FE.md) §7) đè thời lượng và đường cong ngay trên lớp chuyển động, ở tệp gom override, bằng `var(--dur-*)` và `var(--ease-*)`; lớp bọc không truyền số nào từ TypeScript. Truyền số thì thư viện ghi nó thành style nội tuyến và bỏ qua CSS — giá trị lại sống ở nơi thứ hai.

⚠️ Hai bẫy, cả hai im lặng:

- **Tên lớp chuyển động là tên nội bộ của thư viện, không phải API.** Đổi tên ở một lượt nâng thì khối đè không khớp gì, thư viện chạy lại số của nó, build và lint vẫn xanh. Chỉ spec của lớp bọc — đo giá trị đã tính trên phần tử đang mang lớp pha — bắt được.
- **Thư viện định thời theo thời lượng LỚN NHẤT** trong mọi `transition`, `animation` của phần tử chuyển động. Một `transition` dài hơn giành quyền kết thúc pha, và phần tử nhảy về chỗ cũ trong khoảng lệch. Đè cả hai, cùng token.

Component nào, phương án đã loại, cái giá: [`../../adr/0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md`](../../adr/0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md).

✅ **CÓ THẬT** (ADR-0123 đã thi công; đối chiếu 2026-11-06 bằng đọc mã, cộng `ng test` cùng ngày cho hàng spec):

| Hạng mục | Neo |
| --- | --- |
| Lớp bọc không còn hằng thời lượng, không bind input chuyển động nào | `grep -rnE '\[(show\|hide)?[tT]ransitionOptions\]\|_TRANSITION_OPTIONS' src/FE/src/app/shared/ui` không in dòng nào |
| `Dialog` đè hai pha trên lớp chuyển động | `src/FE/src/styles/_thu-vien.scss`, chuỗi `&.p-dialog-enter-active {` |
| `Drawer` đè hai pha trên tấm, gỡ `transition` của thư viện trên tấm | Cùng tệp, chuỗi `&:is(.p-drawer-enter-left, .p-drawer-enter-right, .p-drawer-enter-bottom) {` và `transition: none;` |
| `Toast` đè hai pha, kể cả `transition` của phần tử con | Cùng tệp, chuỗi `p-toast .p-toast-message-enter-active {` và `p-toast .p-toast-message-content {` |
| Lớp nổi neo một khối chung | Cùng tệp, chuỗi `body .p-anchored-overlay-enter-active {` |
| Spec cạnh từng lớp bọc chứng minh token tới đích | Bốn khối `describe` nêu ở dòng F62 của [`../../RULES-FE.md`](../../RULES-FE.md) — không chép lại ở đây |
| Section F37 và canary đã gỡ | `grep -c '^section "F37' scripts/fe-gate.sh` in `0`; `ls scripts/tests/` không có `fe-gate-f37.test.sh` |

🚧 Khối đè lớp phủ của `Drawer` trong `src/FE/src/styles/_thu-vien.scss` chưa đối chiếu ở lượt này: một lượt sửa lỗi lớp phủ còn sót lại sau khi đóng đang chạy và có thể đổi khối đó. Sẽ thành: khối đè trỏ lớp pha `-active` của thư viện, và lớp phủ rời `<body>` khi pha ra xong.

Nếu chỉ nhớ một câu từ mục này: **hai nơi giữ cùng một giá trị thì chúng sẽ lệch nhau; việc phải làm là chọn một nơi làm nguồn, không phải cẩn thận hơn.**

---

## 8. Token và chữ

Thang chữ phải **rời rạc** — một tập cỡ có tên, không phải cỡ tuỳ ý. Lý do thực dụng: khi mọi cỡ đều hợp lệ, mỗi màn sẽ có một cỡ hơi khác, và giao diện trông "hơi lệch" ở mọi chỗ mà không ai chỉ ra được chỗ nào sai.

Ba thứ phải khai thành token, không chỉ cỡ chữ: **họ chữ**, **độ đậm**, **chiều cao dòng**. Chiều cao dòng hay bị bỏ quên nhất và lại là thứ ảnh hưởng tới cảm giác dày/thưa của cả trang.

**Font phải được tự phục vụ từ ứng dụng**, khai bằng `@font-face` trong stylesheet, trỏ tới đường dẫn tuyệt đối của tài nguyên tĩnh. Không nhúng bằng thẻ liên kết trong tài liệu HTML gốc với đường dẫn tương đối: bản build phân giải đường dẫn đó theo cấu hình build, và kết quả là bản chạy thật im lặng dùng font hệ điều hành trong khi máy dev vẫn đúng.

Kiểm bằng công cụ dev: giá trị `font-family` đã tính toán phải **thật sự** phân giải ra font đã khai, không phải font dự phòng.

---

## 9. Kiểm chứng

Đi **bằng tay** trên ứng dụng chạy thật, cả hai chế độ sáng và tối.

- [ ] Mọi token trong `Design/` có mặt trong stylesheet toàn cục, **đúng tên**
- [ ] Không token nào được thêm mà chưa có tên trong `Design/`
- [ ] Tìm hex trong SCSS của `src/app` → **không kết quả** (luật F6)
- [ ] Tìm `rgb(`/`rgba(` trong SCSS của `core/`, `shared/`, `platform/` → chỉ còn dạng đọc token pha alpha (luật F7)
- [ ] Đổi một token màu thương hiệu → **cả** CSS của mình lẫn component thư viện đổi theo
- [ ] Mở lần đầu, chưa từng chọn theme, trên máy đang để chế độ tối → giao diện **sáng**
- [ ] Chọn "theo hệ điều hành" trên máy đang để chế độ tối → không mảng nào còn nền sáng
- [ ] Tìm mã màu trong `core/theme/` → không kết quả; preset chỉ trỏ `var(--color-*)`
- [ ] Chọn "sáng" tường minh khi hệ điều hành đang tối → giao diện sáng thật
- [ ] Tải lại trang ở chế độ tối → **không có nháy trắng**
- [ ] `font-family` đã tính toán phân giải ra đúng font đã khai (§8)
- [ ] Tab bằng bàn phím qua một màn mẫu → thấy rõ trạng thái focus ở mọi control
- [ ] Mọi cặp màu token đạt ngưỡng tương phản AA ở **cả hai** chế độ
- [ ] Một nút và một bảng của thư viện UI render đúng màu token

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> Hàng `✅ CÓ THẬT` đã đối chiếu với `src/` ngày 2026-11-05, neo ở cột *Ghi chú*. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Tầng ngữ nghĩa là bề mặt công khai, mã thô chỉ làm giá trị trong file khai token | ✅ CÓ THẬT (2026-11-05) | §3.2, [`DESIGN.md`](../../Design/DESIGN.md) §1 — `src/FE/src/styles/_tokens.scss`, chuỗi `--color-brand: #1a56b8;`. Vế *component không viết mã thô* do cổng F6, F7 canh |
| Bốn họ token: màu, chữ, khoảng cách, bóng và bo góc | ✅ CÓ THẬT (2026-11-05) | Pha F1 — màu, bo góc, bóng ở `src/FE/src/styles/_tokens.scss` (chuỗi `--color-surface:`, `--radius-md:`, `--shadow-1:`); chữ ở `src/FE/src/styles/_typography.scss` (chuỗi `--fs-md:`); khoảng cách ở `src/FE/src/styles/_spacing.scss` (chuỗi `--sp-4:`) |
| Ba trạng thái sáng / tối / theo hệ điều hành, mặc định sáng | ✅ CÓ THẬT (2026-11-05) | §4.1 — [`../../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md`](../../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md); `src/FE/src/app/core/theme/theme.service.ts`, chuỗi `export type ThemePreference`; script sớm ở `src/FE/src/index.html`, chuỗi `t === 'dark' ? 'dark' : 'light'`; khối theo hệ điều hành ở `src/FE/src/styles/_tokens.scss`, chuỗi `:root:not([data-theme='light'])` |
| Preset ánh xạ token vào thư viện UI | ✅ CÓ THẬT (2026-11-05) | §7 — preset trỏ `var(--color-*)`, không giữ mã màu: `src/FE/src/app/core/theme/prime-preset.ts`, chuỗi `export const CORE_PRIME_PRESET = definePreset(AURA_SUBSET`; đăng ký ở `src/FE/src/app/app.config.ts`, chuỗi `darkModeSelector: false, cssLayer: true` |
| Cổng F6 và F7 | ✅ CÓ THẬT (2026-11-05) | Bật ngay khi token vừa có, ở pha F1 — `scripts/fe-gate.sh`, chuỗi `section "F6 - ` và `section "F7 - `; canary và lỗ đã biết ở dòng F6, F7 của [`../../RULES-FE.md`](../../RULES-FE.md) |
| Sinh token tự động từ nguồn Design | ❌ chưa | Điều kiện: số token vượt mức chép tay còn tin cậy được |
| Nhiều bộ thương hiệu trên cùng một bản build | ❌ chưa | Điều kiện: có sản phẩm thứ hai chạy chung một lần triển khai |
| Chế độ tương phản cao riêng | ❌ chưa | Điều kiện: có yêu cầu thật từ người dùng |
| Miễn trừ cổng màu theo **giá trị** | ❌ loại, không hoãn `K16` | §6.2 — miễn trừ chỉ theo cú pháp |
| `@media prefers-color-scheme` trong SCSS của component | ❌ loại, không hoãn `K17` | §4.2 — chế độ tối xử lý trọn vẹn ở tầng token |
| Đặt tên token theo giá trị màu | ❌ loại, không hoãn `K18` | §3.1 |

> Cách đọc ba ký hiệu của bảng trên — và khi nào *"FE thiếu X"* là finding: [`../README.md`](../README.md) §9.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Giá trị token cụ thể | [`../../Design/DESIGN.md`](../../Design/DESIGN.md) |
| Viết style component thế nào | [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) |
| Bọc thư viện UI | [`05-component-library.md`](05-component-library.md) |
| Tương phản màu và ngưỡng | [`15-accessibility.md`](15-accessibility.md) |
| Cổng ép luật F6/F7 | [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
