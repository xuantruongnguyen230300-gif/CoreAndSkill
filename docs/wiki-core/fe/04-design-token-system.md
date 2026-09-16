---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 04. Hệ design token

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`; đây là hệ thống phải dựng ở pha F1.
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
| **Bóng & bo góc** | `--shadow-card`, `--shadow-overlay`, `--radius-md` | Đặt theo **lớp độ cao**, không theo số | `--shadow-2` không cho biết dùng ở đâu |

### 3.1 Đặt tên theo vai trò — vì sao đây là quy tắc quan trọng nhất

`--brand` và `--xanh-duong` cùng trỏ tới một mã màu ở thời điểm hôm nay. Khác nhau ở chỗ: khi thương hiệu đổi sang màu cam, `--brand` vẫn đúng còn `--xanh-duong` biến thành lời nói dối nằm trong code.

Đây không phải chuyện thẩm mỹ. Sản phẩm thứ hai dựng trên Core này gần như chắc chắn có bảng màu khác — nếu tên token mang giá trị, mọi tên đều sai ngay ngày đầu, và người ta sẽ giữ tên sai vì đổi tên đắt hơn.

Cùng lý do, tránh tên gắn với **vị trí duy nhất**: `--mau-nen-sidebar` chỉ dùng được ở một chỗ. Đặt là `--surface-nav` thì dùng lại được cho thanh điều hướng thứ hai.

### 3.2 Token ngữ nghĩa và token gốc

Hai tầng, và chỉ tầng ngoài được dùng trong component:

```scss
:root {
  /* Tầng gốc — bảng màu thô, KHÔNG dùng trực tiếp trong component */
  --blue-600: <một mã màu>;

  /* Tầng ngữ nghĩa — thứ component được phép dùng */
  --brand: var(--blue-600);
  --focus-ring: var(--blue-600);
}
```

> 🛑 **Bốn tên trên là minh hoạ KHÁI NIỆM, không phải token của Core.** Tên thật và giá trị thật khai ở [`../../Design/DESIGN.md`](../../Design/DESIGN.md) — đó là nguồn duy nhất, theo [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §7. Khu này giải thích **vì sao** cần hai tầng; nó không đặt tên cho hệ nào.

Lợi ích thật: khi chế độ tối cần đổi `--brand` sang sắc độ sáng hơn để đủ tương phản, chỉ tầng ngữ nghĩa đổi. Nếu component dùng thẳng `--blue-600`, không có chỗ nào để can thiệp.

Cái giá là một tầng gián tiếp nữa. Nó đáng trả vì đúng một lý do: **chế độ tối và đổi thương hiệu đều tác động lên tầng ngữ nghĩa, không lên tầng gốc.**

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
2. **Bóng đổ phải khai lại, không chỉ màu.** Bóng đen trên nền tối gần như vô hình. Chế độ tối thường dùng đường viền sáng thay bóng — nên `--shadow-card` phải là token có giá trị riêng cho mỗi chế độ.
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

> ⚠️ Ở dự án tiền nhiệm, luật này được dọn tay hai lần và tự tái sinh cả hai lần: hex mới xuất hiện ngay ở đợt màn hình kế tiếp. Nó chỉ dừng lại khi có script kiểm. **Luật không có cổng thì không phải luật** — xem [`../../RULES.md`](../../RULES.md) §10.

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

**Vì sao `rgba(0, 0, 0, .5)` cũng bị cấm dù trông vô hại:** nó vẫn là một quyết định thiết kế. Độ tối của lớp phủ sau modal là thứ nhà thiết kế chọn, thứ phải đổi ở chế độ tối, và thứ sản phẩm thứ hai có thể muốn khác. Nó thuộc `Design/` như mọi màu khác — tên nó là `--overlay-backdrop`, không phải "đen 50%".

**Vì sao miễn trừ phải theo cú pháp:** miễn trừ theo giá trị đòi hỏi một danh sách "giá trị vô hại". Danh sách đó không có tiêu chí khách quan nào để từ chối thêm một giá trị nữa, nên nó sẽ dài ra sau mỗi lần ai đó thấy phiền — và tới lúc nào đó, cổng tha nhiều hơn bắt. Miễn trừ theo cú pháp thì khác: dạng `rgb(var(--x) / a)` **không mang giá trị màu nào**, nên nó không có gì để lệch khi đổi theme. Đó là một tiêu chí đúng vĩnh viễn, không phải một danh sách phải bảo trì.

### 6.3 Bẫy khi viết cổng này

Phải **gỡ dạng được phép ra khỏi dòng trước, rồi mới hỏi lại dòng còn lại**. Nếu chỉ loại bỏ những dòng có chứa dạng hợp lệ, một dòng vừa có `rgb(var(--x) / a)` hợp lệ vừa có một literal trần sẽ được tha nhầm nguyên dòng. Đây là bẫy đã dính thật và được phát hiện bằng canary ở dự án tiền nhiệm.

---

## 7. Ánh xạ token sang thư viện UI

PrimeNG có hệ theming riêng. Để nó chạy song song với token của mình là có hai hệ màu cùng tồn tại và không ai biết chỗ nào thắng — triệu chứng là "sửa token mà nút không đổi màu".

Cách đúng: một **preset styled** ở `core/theme/`, dựng từ preset Aura bằng `definePreset`, đăng ký một lần lúc cấu hình app, và **không** để theme mặc định của thư viện chạy kèm. Preset **không giữ mã màu nào** — nó chỉ trỏ tên biến CSS của mình:

| Việc | Cơ chế |
| --- | --- |
| Tầng được ghi đè | Tầng **semantic** `colorScheme`: primary, highlight, formField, content, overlay, text, focusRing — mỗi giá trị trỏ một `var(--color-*)` |
| Thang màu 50–950 của preset | **Không** dựng. Tầng semantic đã trỏ token của mình thì không còn chỗ nào cần thang đó |
| Component token còn trỏ thẳng palette | Đè riêng từng chỗ, cũng bằng `var(--color-*)` |
| Chế độ tối của thư viện | Tắt: `providePrimeNG` đặt `darkModeSelector: false`. Biến `--color-*` đã đổi theo `data-theme` (§4.1), nên component thư viện đổi theo mà không cần cơ chế tối thứ hai |
| Thứ tự lớp CSS | Bật `cssLayer`; lớp của thư viện xếp **trước** style của app, để style của mình thắng mà không cần `!important` |

> ⚠️ **Cú pháp API PrimeNG của phiên bản đã chốt chưa được xác minh trong repo.** Bảng trên mô tả cơ chế, không phải chữ ký hàm. Cú pháp chốt khi thi công F1, theo tài liệu PrimeNG chính thức — không chép chữ ký từ trí nhớ hay từ dự án khác.

**Hệ quả:** literal màu chỉ còn ở tệp khai token (`_tokens.scss`, [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §3.3). Đổi bảng màu là sửa một nơi; CSS của mình và component thư viện không thể vẽ hai màu khác nhau cho cùng một token, vì cả hai đọc cùng một biến.

Nếu chỉ nhớ một câu từ mục này: **hai nơi giữ cùng một giá trị thì chúng sẽ lệch nhau; việc phải làm là chọn một nơi làm nguồn, không phải cẩn thận hơn.**

---

## 8. Token và chữ

Thang chữ phải **rời rạc** — một tập cỡ có tên, không phải cỡ tuỳ ý. Lý do thực dụng: khi mọi cỡ đều hợp lệ, mỗi màn sẽ có một cỡ hơi khác, và giao diện trông "hơi lệch" ở mọi chỗ mà không ai chỉ ra được chỗ nào sai.

Ba thứ phải khai thành token, không chỉ cỡ chữ: **họ chữ**, **độ đậm**, **chiều cao dòng**. Chiều cao dòng hay bị bỏ quên nhất và lại là thứ ảnh hưởng tới cảm giác dày/thưa của cả trang.

**Font phải được tự phục vụ từ ứng dụng**, khai bằng `@font-face` trong stylesheet, trỏ tới đường dẫn tuyệt đối của tài nguyên tĩnh. Không nhúng bằng thẻ liên kết trong tài liệu HTML gốc với đường dẫn tương đối: bản build phân giải đường dẫn đó theo cấu hình build, và kết quả là bản chạy thật im lặng dùng font hệ điều hành trong khi máy dev vẫn đúng.

Kiểm bằng công cụ dev: giá trị `font-family` đã tính toán phải **thật sự** phân giải ra font đã khai, không phải font dự phòng.

---

## 9. Kiểm chứng ở pha F1

> 📖 Danh sách nghiệm thu F1: đọc [`trien-khai/02-f1-design-token.md`](trien-khai/02-f1-design-token.md) §7.

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Hai tầng token gốc và ngữ nghĩa | ✅ sẽ có | §3.2 |
| Bốn họ token: màu, chữ, khoảng cách, bóng và bo góc | ✅ sẽ có | Pha F1 |
| Ba trạng thái sáng / tối / theo hệ điều hành, mặc định sáng | ✅ sẽ có | §4.1 — [`../../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md`](../../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md) |
| Preset ánh xạ token vào thư viện UI | ✅ sẽ có | §7 — preset trỏ `var(--color-*)`, không giữ mã màu |
| Cổng F6 và F7 | ✅ sẽ có | Bật ngay khi token vừa có, ở pha F1 |
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
| Đưa token vào code ở pha nào | [`trien-khai/02-f1-design-token.md`](trien-khai/02-f1-design-token.md) |
