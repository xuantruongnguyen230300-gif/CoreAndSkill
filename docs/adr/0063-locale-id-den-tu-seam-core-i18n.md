---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0063 — `LOCALE_ID` và dữ liệu locale của pipe đến từ seam `CORE_I18N`, không cấp riêng ở `app.config.ts`

> **Trạng thái:** Đã chấp nhận (2026-09-22)

## Bối cảnh

[`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §7 khai: *danh sách ngôn ngữ có một nguồn là seam `CORE_I18N`; thêm một ngôn ngữ là thêm vào seam cùng tệp dịch, không sửa gì khác*. Kiến trúc sư đối chiếu `src/FE/src/app/app.config.ts` ngày 2026-09-22:

| Chỗ | Có gì |
| --- | --- |
| `provideCoreI18n({ … defaultLanguage: 'vi' … })` | Ngôn ngữ mặc định — đúng seam |
| `{ provide: LOCALE_ID, useValue: 'vi' }` | Cùng giá trị, viết cứng lần thứ hai, ngoài seam |
| `registerLocaleData(localeVi)` ở đầu tệp | Dữ liệu locale cho pipe `date`/`number`, gọi trực tiếp, ngoài seam |

Ba chỗ cùng nói "tiếng Việt", trong cùng một tệp, không chỗ nào biết chỗ kia. Hôm nay chúng khớp vì chỉ có một ngôn ngữ. Một dự án hạ nguồn đặt `defaultLanguage: 'en'` mà quên hai dòng còn lại thì câu chữ tiếng Anh, ngày tháng vẫn định dạng tiếng Việt — pipe không báo lỗi, đúng dạng hỏng im lặng mà 08-i18n.md §6.2 cảnh báo. Câu *"thêm ngôn ngữ chỉ sửa seam"* vì thế đang sai với mã: phải sửa ba chỗ, và không tài liệu nào kể tên hai chỗ kia.

Người thi công đọc tài liệu Angular sẽ thấy `{ provide: LOCALE_ID, useValue: … }` là cách chuẩn — nên nếu không có bản ghi này, ai đó sẽ "sửa lại cho đúng".

## Quyết định

Kiến trúc sư chốt:

1. **`provideCoreI18n` cấp `LOCALE_ID`** từ `defaultLanguage` của seam.
2. **`provideCoreI18n` đăng ký dữ liệu locale** cho mọi ngôn ngữ khai trong seam — mỗi mục ngôn ngữ mang theo dữ liệu locale của nó (import tĩnh từ `@angular/common/locales/<mã>` ở composition root, vì dữ liệu này không nạp được từ một chuỗi lúc chạy).
3. `app.config.ts` **không** cấp `LOCALE_ID` riêng và **không** gọi `registerLocaleData` trực tiếp; `core/` không đọc `LOCALE_ID` ở đâu ngoài tệp cấp seam.
4. Luật **F36** ở [`../RULES.md`](../RULES.md) §7, trạng thái 📐; `frontend-expert` thi công cùng cổng và canary.

Ngoài phạm vi: `LOCALE_ID` là token **tĩnh** — đổi ngôn ngữ lúc chạy (08-i18n.md §7) đòi pipe đọc locale từ ngôn ngữ hiện hành, việc đó chưa có thiết kế và chỉ cần khi ngôn ngữ thứ hai về. ADR này chỉ gom ba chỗ về một.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ nguyên, ghi vào tài liệu "thêm ngôn ngữ thì sửa ba chỗ"

**Được:** không sửa mã; đúng thói quen Angular.

**Vì sao loại:** đó là một danh sách phải nhớ, và chỗ hỏng khi quên là hỏng im lặng. Seam đã tồn tại để gom đúng loại giá trị này; thêm hai dòng vào nó rẻ hơn một đoạn tài liệu mà người thêm ngôn ngữ chưa chắc đọc.

### Phương án B — `provideCoreI18n` cấp `LOCALE_ID` nhưng để `registerLocaleData` ở `app.config.ts`

**Vì sao loại:** còn hai chỗ. Dữ liệu locale là phần dễ quên hơn — quên nó thì pipe rơi về khuôn Anh-Mỹ, quên `LOCALE_ID` thì ít nhất còn thấy sai ngay ở màn đầu.

## Hệ quả

### Tích cực

- Thêm ngôn ngữ đúng là một chỗ như 08-i18n.md §7 đã hứa.
- Dự án hạ nguồn không thể có câu chữ một ngôn ngữ, ngày tháng ngôn ngữ khác, chỉ vì quên một dòng.

### Tiêu cực

- **Trái thói quen Angular**: người mới tìm `LOCALE_ID` trong `app.config.ts` sẽ không thấy; phải biết seam. Cổng F36 là thứ chặn họ "sửa lại".
- **Mỗi mục ngôn ngữ của seam mang thêm một import dữ liệu locale** — kiểu của seam rộng ra, và mục ngôn ngữ không còn là dữ liệu thuần chuỗi.
- Chưa giải bài toán locale theo ngôn ngữ đang chọn lúc chạy; ADR này có thể bị bổ sung khi ngôn ngữ thứ hai về.
- Dòng *"có thật hôm nay"* của [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §5.5 đang neo vào `provide: LOCALE_ID` trong `app.config.ts` — sau thi công, người đối chiếu phải lật dòng đó theo mã mới.

### Rút lui nếu sai

Trả `LOCALE_ID` và `registerLocaleData` về `app.config.ts`, gỡ section F36. Một giờ, không dữ liệu nào đổi.

## Liên quan

- [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §6.2, §7 — vì sao phải đăng ký locale, và lời hứa "một chỗ".
- [`0057-seam-fe-chi-mang-gia-tri-dung-duoc-o-composition-root.md`](0057-seam-fe-chi-mang-gia-tri-dung-duoc-o-composition-root.md) — seam mang giá trị dựng ở composition root; dữ liệu locale import tĩnh là đúng khuôn đó.
