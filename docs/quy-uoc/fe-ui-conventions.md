---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quy ước giao diện — Angular, PrimeNG, token, i18n, form

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu 2026-09-21, **chỉ §5.5, §6.1, §6.2, §6.4, §6.5**; riêng §5.5 đối chiếu lại 2026-09-23 sau khi ADR-0063 thi công xong, và §6.1 đối chiếu lại 2026-09-24 sau khi ADR-0078 thi công xong). Mọi mục còn lại **chưa đối chiếu với `src/FE`**, kể cả mục đã có code.
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | §5.5 **đã khớp** ([ADR-0063](../adr/0063-locale-id-den-tu-seam-core-i18n.md) thi công xong, đối chiếu 2026-09-23): `src/FE/src/app/core/config/core-i18n.ts` có `registerLocaleData(ngonNgu.localeData, ngonNgu.code);` và `{ provide: LOCALE_ID, useValue: i18n.defaultLanguage },`; `src/FE/src/app/app.config.ts` chỉ còn truyền `localeData: localeVi` **vào** `provideCoreI18n({`; `app.config.spec.ts` có ca `KHÔNG cấp LOCALE_ID trực tiếp — giá trị đó đến từ provideCoreI18n`. Việc **vắng mặt** hai chuỗi đó ngoài seam do cổng F36 canh chứ không do câu này — [`../RULES.md`](../RULES.md) §7 | — |
> | §6.2 **đã khớp** (đối chiếu 2026-09-24; chữ ký nhận `ApiFailureError` và đoạn *Toast và banner* 2026-09-25): `src/FE/src/app/shared/forms/apply-form-failure.ts` có `export function applyFormFailure(`, `if (laLoiXuyenSuot(loi.status, loi.body))` và trả `cauTungMa(`; `field-errors-text.ts` cùng thư mục cũng chặn bằng `laLoiXuyenSuot(`; màn đăng nhập gọi `dichLoiChoMan(this.translate, err)`; `src/FE/src/app/shared/forms/field-errors-text.ts` có `.join('\n')`; `src/FE/src/app/shared/forms/focus-o-sai-dau-tien.ts` có `export function focusOSaiKhiGuiSai(` và `export function focusOSaiDauTien(`. Trạng thái tải hỏng (đối chiếu 2026-09-26): khung luôn có tiêu đề, thân `dichLoiChoMan` — `ho-so.page.ts` `loiTaiHoSo`, `chi-tiet-nguoi-dung.page.html` `#khoiLoiTai`, `ma-tran-phan-quyen.page.html` `[errorHeading]`; `tra-cuu-vai-tro.store.ts` `cauLoi(` | — |
> | §6.5: `src/FE/src/app/shared/forms/field-error-text.ts` có `export function fieldErrorText(` | — |
> | Khu đơn vị khớp §6.2 về vai: `src/FE/src/app/platform/he-thong/don-vi/state/tao-don-vi.store.ts` có `readonly lanGuiSai = signal(0)`; `src/FE/src/app/platform/he-thong/don-vi/components/hop-tao-don-vi/hop-tao-don-vi.component.ts` có `focusOSaiKhiGuiSai(this.lanGuiSai` (component nhận qua `input()`) | — |
> | §6.2 mã gốc → ô **đã khớp** (đối chiếu 2026-09-24): `src/FE/src/app/shared/forms/apply-form-failure.ts` có `maGocVaoO: BangMaGocVaoO<F> = {},` và `return oMaGoc ? null : dichLoi(translate, body);`; dưới `src/FE/src/app/platform/`, `tao-don-vi.store.ts`, `tao-nguoi-dung.store.ts`, `hop-vai-tro.store.ts` có `const MA_GOC_VAO_O:`, `hop-chi-tiet-nguoi-dung.store.ts` có `const MA_GOC_VAO_O_SUA:` | — |
> | §6.4 dưới `src/FE/src/app/platform/`: `hop-tao-don-vi.component.html` có `controlId="dv-tao-ma"` và `'dv-tao-ma-error' : 'dv-tao-ma-hint'`, khớp `FormRow`/`Input`. **Lệch ở mọi `.html` có cả `<app-dialog` lẫn `<form`:** banner lỗi đứng trước `<form>`; nút gửi `(clicked)` ở `slot="footer"`, không `type="submit"` | Chưa quyết: sửa mẫu hay ghi ngoại lệ cho form trong Dialog |
> | §6.1 CVA: `src/FE/src/app/shared/components/input/input.component.ts` có `this.ngControl.valueAccessor = this` — cách đăng ký khớp. **Hết ca lệch chỗ đặt** (đối chiếu 2026-09-24): [ADR-0078](../adr/0078-cot-nen-giu-quyen-quyet-thu-muc-button-va-input-doi-sang-shared-components.md) quyết định 2 đã thi công xong — `Button` và `Input` nay ở `shared/components/`, và `src/FE/src/app/shared/components/button/button.component.ts` mang chú thích `Tự dựng, không bọc PrimeNG (§Nền)`, khớp cột `Nền` ([`../Design/COMPONENTS.md`](../Design/COMPONENTS.md) §3 — bảng mục lục là chỗ **khai** giá trị; §4 là quy tắc **chọn** giá trị). `Check` **chưa bao giờ** là ca lệch — nó khai `bọc PrimeNG` và `src/FE/src/app/shared/ui/check/check.component.ts` có `import { CheckboxModule } from 'primeng/checkbox';`, tức đúng chỗ; `app-date-picker` chưa có trong `src/FE/src/app/shared/` | — |
> | Khu người dùng gọi hàm §6.2 (đối chiếu 2026-09-24): `src/FE/src/app/platform/quan-tri/nguoi-dung/state/tao-nguoi-dung.store.ts` có `applyFormFailure(`; `hop-chi-tiet-nguoi-dung.store.ts` cùng thư mục có `applyFormFailure(` và `fieldErrorsText(` | — |
> | §6.3 hàm nhập lại (đối chiếu 2026-09-25): `src/FE/src/app/shared/forms/nhap-lai-phai-khop.ts` có `export function nhapLaiPhaiKhop(`; `ho-so.page.ts`, `doi-mat-khau-bat-buoc.page.ts` gọi nó | — |
> | **Chưa đối chiếu:** §1–§4, §5.1–§5.4, phần còn lại của §6.3, §7–§10; mẫu `FormNguoiDungPage` ở §6.1; các lệnh cổng F5–F9, F23 chưa chạy trên `src/FE` | Đối chiếu từng mục rồi mới lật `verified:` |
>
> **Nguồn giao diện duy nhất là [`../Design/`](../Design/)**; khi hai bên lệch, `Design/` thắng.

---

## 1. Angular — khuôn bắt buộc cho mọi code mới

Stack: **Angular standalone + signals**, **PrimeNG**, **ngx-translate**. Phiên bản, toolchain: [`../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md); file này **không** chép số phiên bản (hướng đã khoá `K47` ở [`../wiki-core/fe/16-nen-tang-va-nang-cap.md`](../wiki-core/fe/16-nen-tang-va-nang-cap.md)).

| Dùng | **Cấm** |
| --- | --- |
| Component standalone (`standalone: true`) | `NgModule` cho bất kỳ mục đích nào |
| `@if` / `@for` / `@switch` | `*ngIf` / `*ngFor` / `*ngSwitch` |
| `input()` / `input.required()` / `output()` | decorator `@Input()` / `@Output()` |
| `inject()` | tiêm qua tham số constructor |
| `signal()` / `computed()` / `effect()` | `BehaviorSubject` cho state của component |
| `model()` cho hai chiều | `@Input()` + `@Output()` đặt tên theo quy ước `xChange` |
| `viewChild()` / `contentChild()` hàm | decorator `@ViewChild()` |

### 1.1 Khuôn một component

```typescript
// components/the-nguoi-dung/the-nguoi-dung.component.ts
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import type { NguoiDung } from '../../models/nguoi-dung.model';

@Component({
  selector: 'app-the-nguoi-dung',
  standalone: true,
  imports: [TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './the-nguoi-dung.component.html',
  styleUrl: './the-nguoi-dung.component.scss',
})
export class TheNguoiDungComponent {
  readonly nguoiDung = input.required<NguoiDung>();
  readonly chiTiet = input(false);

  readonly chon = output<string>();

  protected readonly chuCaiDau = computed(() =>
    this.nguoiDung().fullName.charAt(0).toUpperCase(),
  );

  protected phatChon(): void {
    this.chon.emit(this.nguoiDung().id);
  }
}
```

Ba điều bắt buộc:

1. **`ChangeDetectionStrategy.OnPush` cho mọi component.**
2. **`input.required()` khi input thật sự bắt buộc.**
3. **Thành viên chỉ dùng trong template để `protected`.**

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §1.1

### 1.2 `@for` phải có `track` — luật F13

```html
@for (item of items(); track item.id) {
  <app-the-nguoi-dung [nguoiDung]="item" (chon)="moChiTiet($event)" />
} @empty {
  <p class="rong">{{ 'nguoiDung.trong.tieuDe' | translate }}</p>
}
```

`track` **bắt buộc về cú pháp** — thiếu là lỗi biên dịch, nên F13 do `ng build` ép. Cú pháp đúng chưa đủ:

| Biểu thức track | Đánh giá |
| --- | --- |
| `track item.id` | ✔ đúng — khoá ổn định của bản ghi |
| `track $index` | ⚠ chỉ đúng cho danh sách **không bao giờ** chèn/xoá/sắp xếp lại |
| `track item` | ✘ so sánh theo tham chiếu; mapper tạo object mới mỗi lần tải là render lại toàn bộ |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §1.2

### 1.3 `@if` và cách viết nhánh

```html
@if (dangTai()) {
  <app-skeleton-loader variant="text" [lines]="3" />
} @else if (loi()) {
  <app-empty-state
    variant="error"
    [title]="'nguoiDung.loi.taiThatBai' | translate"
    [actionLabel]="'chung.thuLai' | translate"
    (actionClicked)="tai()"
  />
} @else {
  @for (item of items(); track item.id) {
    <app-the-nguoi-dung [nguoiDung]="item" />
  } @empty {
    <app-empty-state variant="first-use" [title]="'nguoiDung.trong.tieuDe' | translate" />
  }
}
```

**Khoá i18n trong mẫu là khoá thật**, từ bảng Câu chữ ở [`../Design/Screens/10-nguoi-dung.md`](../Design/Screens/10-nguoi-dung.md) — spec màn là nguồn của câu chữ **và** khoá; file này không đặt khoá minh hoạ riêng.

**`@empty` của `@for` không thay được nhánh `@if (loi())`.**

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §1.3

### 1.4 Cổng

```bash
# Luật F9 — không còn cú pháp Angular cũ.
# PASS khi không in ra dòng nào.
[ -d src/FE/src/app ] || { echo "F9: không có src/FE/src/app để quét"; exit 1; }
grep -rnE '\*ngIf|\*ngFor|\*ngSwitch|@Input\(\)|@Output\(\)|@ViewChild\(|NgModule' \
  src/FE/src/app --include='*.ts' --include='*.html'
```

Cổng quét **văn bản**: chú thích **gọi tên** cú pháp cũ, không gõ lại.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §1.4

### 1.5 Đặt tên và selector

| Loại | Tên file | Tên class | Selector |
| --- | --- | --- | --- |
| Page (smart) | `<ten>.page.ts` | `TenPage` | `app-<ten>` |
| Component (dumb) | `<ten>.component.ts` | `TenComponent` | `app-<ten>` |
| Directive | `<ten>.directive.ts` | `TenDirective` | `appTen` (camelCase) |
| Service | `<ten>.service.ts` | `TenService` | — |
| Mapper | `<ten>.mapper.ts` | hàm `mapTen()` | — |

Tiền tố `app` khai một lần trong `eslint.config.js` (`@angular-eslint/component-selector` và `directive-selector`).

**Tên tiếng Việt không dấu cho định danh miền nghiệp vụ** (`NguoiDung`, `PhanQuyen`); định danh kỹ thuật giữ tiếng Anh (`input`, `signal`, `PagedList`). Không trộn hai thứ trong cùng một danh từ.

---

## 2. Bọc PrimeNG — luật có cổng, không phải lời khuyên

### 2.1 Luật F5

> **Component nghiệp vụ không import trực tiếp `primeng/*`. Mọi thứ đi qua lớp bọc ở `shared/ui/`.**

`shared/ui/` giữ các bọc **mỏng**; `shared/components/` ghép chúng thành khối có nghĩa với sản phẩm; phần còn lại của app chỉ biết `app-*`. Ranh giới: [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §3.

Component Design ghi **bọc PrimeNG** (`DataTable`) nằm ở `shared/ui/`; component Design ghi **tự dựng** (`Button`, `Input`, `FormRow`, `Toolbar`) nằm ở `shared/components/`, không import `primeng/`. Chiều ngược cũng cấm — luật F24: [`fe-architecture.md`](fe-architecture.md) §2.2, [§2.8](fe-architecture.md), [§4.6](fe-architecture.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §2.1

### 2.2 Allowlist — đường dẫn duy nhất được import `primeng/`

> 📖 Allowlist đầy đủ: [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §2.4 — cổng F5 đọc đúng bảng đó.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §2.2

### 2.3 Vì sao bọc — và cái giá của việc bọc

🛑 **Bọc không phải là chuyển tiếp mọi thuộc tính.** Bọc đúng là **thu hẹp**: phơi đúng những gì sản phẩm cần, đặt tên theo API ở spec component của `Design/` (`sortDescending`, không phải `sortOrder` của thư viện), giấu phần còn lại.

> 📖 Lý do, bẫy, ví dụ mở rộng (gồm phép thử nhanh để biết một bọc có hẹp đúng cách): [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §2.3

### 2.4 Khi thư viện thiếu thứ cần

Thứ tự ưu tiên, không đảo:

1. Dùng component có sẵn của thư viện, bọc lại.
2. Ghép nhiều component của thư viện trong một bọc.
3. Tự viết component trong `shared/components/`, không phụ thuộc thư viện.
4. Override CSS của thư viện — **chỉ trong file style dành riêng cho override** (§4.4), không rải trong style của component nghiệp vụ.

Mỗi override (bước 4) phải kèm chú thích nói **vì sao** và **cái gì sẽ hỏng nếu thư viện đổi**.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §2.4

---

## 3. Design token — màu khai một chỗ

### 3.1 Hai luật

> **F6 — Không hex color literal trong SCSS của component.**
>
> **F7 — Không `rgb()`/`rgba()` literal trong SCSS. Ngoại lệ duy nhất là dạng `rgb(var(--x) / a)`.**

```scss
// ✔ đúng
.the {
  background: var(--color-surface);
  border: 1px solid var(--color-border-subtle);
  color: var(--color-text);
  box-shadow: var(--shadow-1);
}
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §3.1

### 3.2 Miễn trừ theo CÚ PHÁP, không theo GIÁ TRỊ

🛑 **Không bao giờ miễn trừ theo giá trị.**

Cổng gỡ dạng được phép ra khỏi dòng **rồi mới hỏi lại**, không loại cả dòng:

```bash
# Luật F7 — PASS khi không in ra dòng nào.
[ -d src/FE/src/app ] || { echo "F7: không có src/FE/src/app để quét"; exit 1; }
grep -rnE 'rgba?\(' src/FE/src/app --include='*.scss' \
  | sed -E 's/rgba?\( *var\(--[A-Za-z0-9_-]+\) *\/[^)]*\)//g' \
  | grep -E 'rgba?\('
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §3.2

### 3.3 Nơi duy nhất được giữ literal

`src/styles/_tokens.scss` là ngoại lệ duy nhất; cổng loại trừ đúng file đó, **không** loại trừ cả thư mục.

```scss
// src/styles/_tokens.scss — NƠI DUY NHẤT trong src/ được phép có literal màu
:root {
  /* Mỗi token ở docs/Design/DESIGN.md §2 chiếm đúng một dòng:   <tên token>: <giá trị cột "sáng">;
     Khối tối trong @media (prefers-color-scheme: dark) và :root[data-theme].
     Danh sách KHÔNG chép sang tài liệu nào — kể cả file này. */
}
```

🛑 **Khối trên cố ý rỗng** — dạy **hình dạng**, không dạy nội dung.

File này **không được** chứa một dòng `--token: giá-trị` nào — luật **D30** ở [`../RULES.md`](../RULES.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §3.3

### 3.4 Chiều cập nhật — `Design/` là nguồn, code đuổi theo

```
docs/Design/  ──(người quyết định thiết kế sửa)──▶  src/styles/_tokens.scss  ──▶  component
```

Không có chiều ngược. Cần màu mới:

1. Thêm token vào [`../Design/DESIGN.md`](../Design/DESIGN.md) kèm tên và ngữ cảnh dùng.
2. Thêm biến vào file token global.
3. Dùng `var(--ten-token)` trong component.

> ⚠️ **Đừng chép danh sách token vào file này hay tài liệu nào khác.** Đếm bằng lệnh ở §3.2.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §3.4

### 3.5 Theme trước paint và font — định nghĩa gốc

Cơ chế `data-theme` và mặc định sáng thuộc [`../Design/DESIGN.md`](../Design/DESIGN.md) §8; mục này chỉ khai **cách thi công**.

| Quyết định | Nội dung |
| --- | --- |
| Áp theme **trước paint** | `index.html` có **đúng một** script nội tuyến, đặt trong `<head>` trước mọi `<link rel="stylesheet">`: đọc `localStorage` khoá `theme`, đặt `data-theme` trên `<html>` |
| Giá trị lưu ↔ thuộc tính | `'light'` / `'dark'` → đặt nguyên; `'system'` → **bỏ** thuộc tính (theo hệ điều hành); vắng hoặc giá trị lạ → `'light'` ([`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md) §4.3) |
| Đọc `localStorage` | Bọc `try/catch`; ném lỗi ⇒ coi như vắng ([`../wiki-core/fe/14-security.md`](../wiki-core/fe/14-security.md) §6.3) |
| CSP cho script đó | Qua **hash** `'sha256-…'` trong `script-src`; **không** `'unsafe-inline'`. Script đổi một ký tự thì hash đổi — tính lại và cập nhật header ở tầng phục vụ ([`../wiki-core/fe/14-security.md`](../wiki-core/fe/14-security.md) §4) |
| `ThemeService` ở `core/theme/` | Đọc/ghi **cùng** khoá `localStorage` và **cùng** thuộc tính; không áp lại lúc khởi động — script đã áp |
| Font | `@font-face` khai ở `src/styles/_typography.scss`; tệp ở `src/FE/public/fonts/`, tự phục vụ, **không CDN**; chỉ bốn nét 400/500/600/700 ([`../Design/DESIGN.md`](../Design/DESIGN.md) §3.1) |

```html
<!-- index.html — script nội tuyến DUY NHẤT; CSP cho phép bằng hash của đúng nội dung này -->
<script>
  (function () {
    var t = null;
    try { t = localStorage.getItem('theme'); } catch (e) {}
    if (t === 'system') { return; }
    document.documentElement.setAttribute('data-theme', t === 'dark' ? 'dark' : 'light');
  })();
</script>
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §3.5

---

## 4. Tổ chức file style

### 4.1 Bốn nhóm, bốn file

```
src/styles/
├── _tokens.scss      # biến màu, bán kính, bóng đổ, thang z-index. NƠI DUY NHẤT có literal
├── _typography.scss  # họ chữ, cỡ chữ, cân nặng, chiều cao dòng
├── _spacing.scss     # thang khoảng cách, breakpoint
├── _thu-vien.scss    # override component thư viện — mỗi override kèm lý do
└── styles.scss       # điểm vào: @use bốn file trên + reset + lớp tiện ích toàn cục
```

Ngưỡng dòng cho từng file: [`fe-architecture.md`](fe-architecture.md) §5.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §4.1

### 4.2 Style của component

Style nằm trong `styleUrl` của chính component. Ba luật:

1. **Không `::ng-deep`.** Cần chạm vào bên trong component thư viện → dùng file override (§4.4).
2. **Không `!important`** trừ khi kèm chú thích nói rõ đè cái gì và vì sao không đè được bằng độ ưu tiên chọn lọc; thiếu chú thích là finding trong review.
3. **Không selector theo thẻ trần** (`div`, `span`) ở cấp cao. Đặt lớp có tên.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §4.2

### 4.3 Lớp tiện ích

Được phép một tập lớp tiện ích nhỏ trong `styles.scss` (căn lề, ẩn khi in, chỉ cho trình đọc màn hình); **không** dựng hệ tiện ích tự chế song song với hệ token.

Lớp `form-grid` (lưới của `<form>` màn, thân `Dialog`) khai **một lần** ở đây; số cột và khe do [`../Design/Components/FormRow.md`](../Design/Components/FormRow.md) §Responsive quyết, `span` của `FormRow` chiếm cột trong lưới này. Màn không tự viết `grid-template-columns` cho form.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §4.3

### 4.4 Override thư viện

Gom vào `_thu-vien.scss`; mỗi khối override bắt buộc có chú thích trả lời hai câu:

```scss
// Vì sao: bảng của thư viện đặt padding dòng lớn hơn thang khoảng cách của Design.
// Hỏng khi nào: thư viện đổi tên lớp nội bộ này ở bản nâng cấp — kiểm lại sau mỗi lần nâng major.
.p-datatable .p-datatable-tbody > tr > td {
  padding: var(--sp-3) var(--sp-4);
}
```

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §4.4

---

## 5. i18n — template không chứa chữ tiếng Việt

### 5.1 Luật F8

> **Mọi câu người dùng đọc đến từ file ngôn ngữ. Template `.html` không chứa chữ tiếng Việt.**

```html
<!-- ✔ đúng -->
<h1>{{ 'nguoiDung.tieuDe' | translate }}</h1>
<app-button variant="primary" (clicked)="luu()">{{ 'chung.luu' | translate }}</app-button>

<!-- ✘ sai -->
<h1>Quản trị người dùng</h1>
```

### 5.2 Cổng dò dấu thanh

```bash
# Luật F8 — PASS khi không in ra dòng nào.
[ -d src/FE/src/app ] || { echo "F8: không có src/FE/src/app để quét"; exit 1; }
find src/FE/src/app -name '*.html' | while IFS= read -r f; do
  perl -0777 -pe 's{<!--.*?-->}{ "\n" x (() = ($& =~ /\n/g)) }gse' "$f" \
    | grep -nE '[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]' \
    | sed "s|^|$f:|"
done
```

**Giới hạn:** cổng không bắt chuỗi cứng tiếng Anh (`"Save"`).

**Vế thứ hai của F8** — mọi khoá dịch viết literal trong `src/app` có trong `vi.json` — chạy cùng section; lệnh gốc ở [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.6.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §5.2

### 5.3 Đặt khoá dịch — định nghĩa gốc

Mục này là **Đặt khoá dịch — định nghĩa gốc**: nguồn duy nhất của quy ước đặt khoá i18n phía FE; tài liệu FE khác trỏ về đây.

Khoá phẳng theo miền, phân cấp bằng dấu chấm, camelCase ở mỗi đoạn — `nguoiDung.cot.hoTen`, `loi.CORE.CLIENT.VALIDATION_REQUIRED`.

| Quy tắc | Vì sao |
| --- | --- |
| Đoạn đầu là **miền**, không phải tên màn hình | Màn hình đổi tên nhiều hơn miền đổi tên |
| Khoá phản ánh **vai trò của chuỗi trong giao diện**, không phản ánh nội dung câu | `chung.luu` sống được khi câu đổi thành "Ghi lại"; `chung.luuLaiHoSo` thì không |
| Khoá dưới `chung.` chỉ cho câu thật sự dùng ở nhiều miền | Nếu không, `chung.` biến thành ngăn kéo rác |
| Khoá lỗi là `loi.<mã lỗi BE>` — giữ nguyên mã BE, **không** đổi sang camelCase | Để tra được bằng chính mã BE trả về, không cần một bảng chuyển đổi thứ hai |
| Khoá lỗi của **validator phía client** là `loi.<mã>` như mọi khoá lỗi khác — giữ nguyên mã, không đổi sang camelCase. 📖 Danh mục mã `CORE.CLIENT.*` và khuôn `VALIDATION_<VALIDATOR>`: đọc [`be-cqrs-handler.md`](be-cqrs-handler.md) §7.4 | Cùng nhóm `loi.*` nên chỉ một đường tra câu lỗi. Danh mục `CORE.CLIENT.*` ở `be-cqrs-handler.md` §7.4; chép một phần sang đây là bản sao thứ hai, và FE sẽ tra một mã không có thật ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5) |
| Khoá mà **BE cấp cả phần đuôi** giữ nguyên **đúng dạng BE gửi**, không đổi sang camelCase: `menu.<code>` ([`../contracts/meta-menu.md`](../contracts/meta-menu.md) §1.2 — `labelKey` là khoá i18n, `code` là kebab-case) và `permission.<code>` ([`../contracts/permissions.md`](../contracts/permissions.md) §4 — `nameKey`, ví dụ `permission.core.user.reset-password`; `resourceNameKey` ở §5 cùng khuôn) | Cùng lý do với `loi.<mã BE>`, một tầng nữa: hình dạng khoá **không phải của FE**. FE nhận nó trên dây và tra thẳng, nên "sửa cho đúng camelCase" ở tệp ngôn ngữ làm khoá thôi khớp và giao diện hiện **nguyên chuỗi khoá** — sidebar mất chữ, ma trận quyền mất tên hàng. Cổng F8 không bắt được vì `labelKey`/`nameKey` đến từ dây, không có trong mã nguồn FE. Đổi hình dạng khoá là đổi hợp đồng BE, không phải đổi tệp `vi.json` |
| Trong tệp JSON, khoá **lồng theo từng đoạn**, kể cả khoá lỗi: `loi` → `CORE` → `CLIENT` → `VALIDATION_REQUIRED`. Không viết một khoá phẳng chứa dấu chấm | Thư viện dịch coi dấu chấm là ranh giới cấp; khoá phẳng chứa dấu chấm tra trúng hay trượt tuỳ phiên bản thư viện, và trượt thì **im lặng** hiện nguyên chuỗi khoá. Lồng cấp thì đúng ở mọi phiên bản |
| Mọi ngôn ngữ có **cùng tập khoá** | Thiếu khoá ở một ngôn ngữ thì câu đó hiện ra dưới dạng chính khoá |


**Luật F23 — mọi tệp ngôn ngữ trong CÙNG một tầng có cùng tập khoá với `vi.json` của tầng đó.** Cổng quét **từng tầng riêng**, `public/i18n/` và `public/i18n-app/` ([`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §2.3), không gộp. Thư mục vắng hay chỉ có một tệp: báo rồi bỏ qua, thoát 0. Đỏ khi một thư mục có từ hai tệp mà thiếu `vi.json`, hoặc tập khoá lệch. Hành vi này khai **ở đây**; lệnh ở [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.15.

**`CORE_I18N.languages` có một mục ⇒ `LanguageSwitcher` không render** — trạng thái `empty` ở [`../Design/Components/LanguageSwitcher.md`](../Design/Components/LanguageSwitcher.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §5.3

### 5.4 Tham số và số nhiều

```json
{
  "nguoiDung": {
    "daChon": "Đã chọn {{soLuong}} người dùng",
    "xacNhanXoa": "Xoá {{ten}} khỏi hệ thống?"
  }
}
```

```html
<p>{{ 'nguoiDung.daChon' | translate: { soLuong: soDaChon() } }}</p>
```

**Tham số truyền theo TÊN, không theo thứ tự** — cùng luật với `messageParams` ([`be-api-controller.md`](be-api-controller.md)).

**Số nhiều** — khuôn: khai hai khoá con (`mot`, `nhieu`) và chọn ở template.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §5.4

### 5.5 Ngày, giờ, số

Định dạng bằng pipe chuẩn của Angular, locale theo ngôn ngữ hiện tại — **không** tự ghép chuỗi ngày, không tự chèn dấu phân cách hàng nghìn.

```html
{{ nguoiDung().createdAt | date: 'short' }}
{{ tongTien() | number: '1.0-0' }}
```

**`LOCALE_ID` và dữ liệu locale đến từ seam `CORE_I18N`** — luật F36, [ADR-0063](../adr/0063-locale-id-den-tu-seam-core-i18n.md): `provideCoreI18n` cấp `LOCALE_ID` từ `defaultLanguage` và đăng ký locale của mọi ngôn ngữ khai; `app.config.ts` không cấp `LOCALE_ID` riêng, không gọi `registerLocaleData` ngoài seam. Đã thi công — bảng đầu file, và cổng F36 canh.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §5.5

---

## 6. Form — typed reactive form

### 6.1 Khuôn

Chỉ dùng **reactive form có kiểu**. Không template-driven form (`ngModel`) cho form nghiệp vụ.

> 📖 Khuôn ví dụ đầy đủ (class minh hoạ, chưa khớp tên file/class thật nào trong `src/FE` hôm nay — thực tế dùng khuôn "hộp thoại" như `hop-tao-nguoi-dung.component.ts`): [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §6.1.

**Tên control trùng tên field của request** — `applyFieldErrors` chỉ đổi chữ hoa đầu khi chuyển khoá `fieldErrors` về tên control.

**`NonNullableFormBuilder` chứ không `FormBuilder`.**

**`getRawValue()` chứ không `value`.**

**Mọi control nhận `[formControl]` — `app-input`, `app-auth-field`, `app-date-picker`, `app-check` — cài `ControlValueAccessor`.** Luật này **không** phụ thuộc tầng: thư mục của một component do cột "Nền" ở [`../Design/COMPONENTS.md`](../Design/COMPONENTS.md) §3 quyết ([`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §3), nên `app-check` và `app-date-picker` — cả hai `bọc PrimeNG` — nằm ở `shared/ui/` mà vẫn cài CVA như mọi control khác. Đăng ký **không** qua `providers` với `NG_VALUE_ACCESSOR`. Khuôn:

```typescript
// shared/components/input/input.component.ts — phần đăng ký CVA
protected readonly ngControl = inject(NgControl, { self: true, optional: true }); // token được F11 tha — 05-gate.md §8.8
constructor() {
  if (this.ngControl) this.ngControl.valueAccessor = this;
}
// Trạng thái error của spec: this.ngControl?.invalid && this.ngControl?.touched
```

API chi tiết ở spec component: [`../Design/Components/Input.md`](../Design/Components/Input.md), [`../Design/Components/AuthField.md`](../Design/Components/AuthField.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §6.1

### 6.2 Gắn lỗi từ `fieldErrors` của BE

Page (hoặc store của nó) **không** tự viết vòng lặp gắn lỗi và **không** tự chọn câu cho khu lỗi chung; gọi `applyFormFailure` ở `shared/forms/` — nhánh `error` của khuôn §6.1 là dạng gọi chuẩn.

**Hàm gắn lỗi của phản hồi thất bại vào form — định nghĩa gốc:** `shared/forms/apply-form-failure.ts` — `applyFormFailure<F extends FormGroup>(form: F, loi: ApiFailureError, translate: TranslateService, maGocVaoO: BangMaGocVaoO<F> = {}): string | null`. Nó bọc `applyFieldErrors` ([`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md) §4 — chữ ký, khoá lồng nhau, khoá không khớp) để **một** luật quyết việc lỗi không được biến mất, không lặp ở từng hộp:

- Lỗi thuộc lớp lỗi xuyên suốt ([`fe-api-client.md`](fe-api-client.md) §2.2) → `null`, form không bị đụng.
- Mọi mã trong `fieldErrors` đã hiện dưới một ô → trả `null`.
- Còn mã **chưa hiện ở ô nào** — khoá không khớp control, hoặc màn không có form (hộp Gán vai trò, [`../Design/Screens/10-nguoi-dung.md`](../Design/Screens/10-nguoi-dung.md); nó gọi `fieldErrorsText(translate, loi: ApiFailureError)` cùng thư mục, cùng luật) → trả câu của **từng mã chưa hiện**, đúng thứ tự BE gửi, **không** lặp mã đã hiện dưới ô, nối bằng `\n` — mỗi mã một dòng; chỗ hiện giữ ngắt dòng. Page đặt vào `NoticeBanner` đầu form hoặc đầu thân hộp.
- `fieldErrors` không mang mã nào (vắng, `{}`, mọi danh sách rỗng) → **đường lùi**: câu của mã ở gốc envelope; `null` nếu mã gốc đã vào ô.

**Mã gốc vào ô** ([ADR-0086](../adr/0086-ma-goc-vao-o-qua-bang-cua-applyformfailure.md)): `BangMaGocVaoO<F>` là `Readonly<Record<string, Extract<keyof F['controls'], string>>>` — mã gốc → tên control **cấp một**, gõ sai tên là lỗi biên dịch. Mã gốc vào ô khi đủ ba điều kiện: `loi.body` khác `null`, bảng có mã đó, `form.get(ô)` khác `null`. Khi đó `server` của ô là câu `dichLoi` của mã gốc rồi tới câu các mã con cùng ô; lỗi server lần trước bị thay, lỗi validator client giữ nguyên, ô được `markAsTouched`; mã gốc **không bao giờ** lên khu chung. Thiếu một điều kiện → như không có bảng. Store khai bảng `MA_GOC_VAO_O` theo bảng *Mã lỗi → chỗ hiện* của screen spec, và đưa mọi `ApiFailureError` vào hàm, không lọc theo danh sách mã — trừ `CORE.CONCURRENCY.CONFLICT` ở màn có nhánh tải lại.

🛑 **Mã gốc không nói thay mã con.** Câu của mã gốc chỉ dùng ở đường lùi hoặc dưới ô của bảng trên — mã nào cũng vậy, kể cả `CORE.TENANT.ADMIN_CREATE_FAILED`. Ca đắt nhất là `CORE.VALIDATION.FAILED`: câu của nó (*"kiểm tra lại các trường được đánh dấu"*) sai khi không ô nào được đánh dấu. Screen spec quyết **chỗ hiện** (bảng *Mã lỗi → chỗ hiện*); **câu nào** thì chỉ mục này quyết. Chốt 2026-09-24.

**Ngoại lệ có tên — màn đăng nhập:** mọi mã ngoài `CORE.VALIDATION.FAILED` đi qua `dichLoiChoMan` ([`fe-api-client.md`](fe-api-client.md) §2.2) vào khu lỗi, không qua `applyFormFailure`, không vào ô nào — một câu, không chỉ ra ô nào sai ([`../Design/Screens/01-dang-nhap.md`](../Design/Screens/01-dang-nhap.md), chống dò tài khoản). Bảo đảm thật ở BE: bốn ca một mã, không `fieldErrors` ([`../contracts/auth.md`](../contracts/auth.md) §3).

**Toast và banner không đi cùng nhau.** Form dùng `applyFormFailure` tự hiện lỗi của mình, nên lời gọi ghi của nó mang `BO_QUA_TOAST_LOI` — thiếu thì lỗi hiện hai lần. Cờ chỉ tắt toast cho lỗi **của màn**: lớp lỗi xuyên suốt ([`fe-api-client.md`](fe-api-client.md) §2.2) luôn toast, và khu lỗi của màn **bỏ qua** lớp đó — `applyFormFailure`, `fieldErrorsText`, `dichLoiChoMan` trả `null`. Lỗi kèm `fieldErrors` thì interceptor không toast, có cờ hay không.

**Hàm đưa focus về ô sai khi gửi sai — định nghĩa gốc:** `shared/forms/focus-o-sai-dau-tien.ts` — `focusOSaiKhiGuiSai(lanGuiSai, goc)` gọi trong constructor của component (cần injection context), và `focusOSaiDauTien(goc)` là phần thuần bên dưới nó. Lớp logic tăng `lanGuiSai` (một `signal<number>`, khởi tạo `0`) ở nhánh "bấm gửi nhưng form không hợp lệ"; component đọc nó, và **sau lần render kế tiếp** mới đặt focus về ô sai đầu tiên trong `goc` — vì dấu nhận diện `aria-invalid="true"` (do `app-input` đặt khi `invalid && touched`) chỉ có trên DOM sau khi lỗi được vẽ. Luật hiển thị ở [`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md) §5. Hộp thoại có hai form thì `goc` chọn form của hộp đang mở. Store giữ `lanGuiSai`, component hộp nhận nó qua `input()` — component không biết store ([`fe-architecture.md`](fe-architecture.md) §3.1).

Ba điểm khi gọi:

1. **Khoá của `fieldErrors` là tên property gốc phía BE** (PascalCase) — chuyển về tên control nằm **trong** hàm dùng chung, không lặp ở page ([`fe-api-client.md`](fe-api-client.md) §1.3).
2. **Giá trị là danh sách mã lỗi, không phải danh sách câu.** Hàm dùng chung dịch từng mã.
3. **Lỗi từ server bị xoá ngay khi người dùng sửa ô đó.**

**Ngoại lệ — màn trong `AuthCard`:** `fieldErrors` không khớp ô nào đưa vào khu lỗi của `AuthCard` qua input `errorMessage` ([`../Design/Components/AuthCard.md`](../Design/Components/AuthCard.md) mục *API dự kiến*), **không** toast.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §6.2

### 6.3 Validate phía client hay phía server

| Loại kiểm tra | Client | Server | Vì sao |
| --- | --- | --- | --- |
| Bắt buộc, độ dài, khuôn dạng, kiểu | ✔ | ✔ | Client cho phản hồi tức thì; server vì client bỏ qua được |
| So sánh giữa các field trong cùng form (ngày kết thúc sau ngày bắt đầu) | ✔ | ✔ | Như trên |
| Trùng lặp trong cơ sở dữ liệu (email đã tồn tại) | ✘ | ✔ | Client không biết dữ liệu; kiểm ở client là một lời hứa không giữ được — hai người có thể đăng ký cùng lúc |
| Quyền được thực hiện thao tác | ✘ | ✔ | Quyền phía client chỉ để **ẩn nút**, không để quyết định |
| Luật nghiệp vụ (hạn mức, trạng thái hợp lệ để chuyển tiếp) | ✘ | ✔ | Luật thuộc về Domain; chép sang FE là tạo bản sao sẽ lệch |

**Form đổi mật khẩu — ranh giới cụ thể của dòng 2.** Client chỉ kiểm **ô nhập lại có khớp ô mật khẩu mới không** (`CORE.CLIENT.VALIDATION_MISMATCH`); *mật khẩu mới phải khác mật khẩu hiện tại* do **BE** kiểm, tới dưới dạng `CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT` trong `fieldErrors` ([`../contracts/auth.md`](../contracts/auth.md) §6).

**Hàm kiểm ô nhập lại phải khớp — định nghĩa gốc:** `shared/forms/nhap-lai-phai-khop.ts` — `nhapLaiPhaiKhop(tenGoc, tenNhapLai): ValidatorFn`, cấp group, đặt lỗi `mismatch` lên ô nhập lại. Form có ô nhập lại dùng nó, không tự viết. Ô gõ lại mã đơn vị (`khoi-phuc-tao-quan-tri.store.ts`) là luật khác.

**Hệ quả:** lỗi chỉ server biết (dòng 3–5) tới dưới dạng `error.fieldErrors` hoặc `error.code`; ô nhập phải có chỗ hiện lỗi kể cả khi không có validator client.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §6.3

### 6.4 Hiển thị lỗi

```html
<form #formEl [formGroup]="form" (ngSubmit)="gui()">
  @if (loiChung(); as loi) {
    <app-notice-banner severity="danger" size="sm">{{ loi }}</app-notice-banner>
  }
  @let loiEmail = loiCua('email');
  <app-form-row
    controlId="nguoi-dung-email"
    [label]="'nguoiDung.cot.email' | translate"
    [required]="true"
    [error]="loiEmail"
  >
    <app-input
      id="nguoi-dung-email"
      type="text"
      autocomplete="email"
      [formControl]="form.controls.email"
      [describedBy]="loiEmail !== null ? 'nguoi-dung-email-error' : null"
    />
  </app-form-row>

  <app-button variant="primary" type="submit" [loading]="dangGui()">
    {{ 'chung.luu' | translate }}
  </app-button>
</form>
```

Hai vai tách nhau theo [`../Design/Components/FormRow.md`](../Design/Components/FormRow.md) và [`../Design/Components/Input.md`](../Design/Components/Input.md): `app-form-row` vẽ nhãn, dấu bắt buộc và **dòng lỗi**; `app-input` chỉ vẽ ô và viền lỗi. `id` của `app-input` trùng `controlId` của `app-form-row`. Chuỗi truyền vào `error` đã dịch; có lỗi hay không do `fieldErrorText` quyết (§6.5).

**Viền lỗi và `aria-invalid` của `app-input` suy từ control** — `invalid && touched` (`Input.md`), trang **không** truyền. Chỉ `describedBy` do trang truyền, suy từ **cùng** biến truyền cho `error` của `FormRow`.

**Id của dòng lỗi và dòng gợi ý suy từ `controlId`**: `<controlId>-error` và `<controlId>-hint` (`FormRow.md`); ô có cả gợi ý lẫn lỗi thì `describedBy` nhận hai id cách nhau một dấu cách.

Nút gửi dùng `type="submit"` và `loading`, không dùng `(clicked)`.

Chi tiết nền về form: [`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §6.4

### 6.5 `fieldErrorText` — lỗi hiện lúc nào, câu nào

Hàm ở `shared/forms/field-error-text.ts`, cạnh `applyFieldErrors`; page gọi qua một dòng uỷ quyền (§6.1), không tự viết lại điều kiện hiện lỗi.

Hiện thực: `src/FE/src/app/shared/forms/field-error-text.ts` — `export function fieldErrorText(`, nhận `control`, `formSubmitted`, `translate`, trả `string | null`. Lỗi client dịch bằng khoá `loi.CORE.CLIENT.VALIDATION_<KHOÁ VIẾT HOA>`, tham số dịch là object lỗi của validator.

| Luật | Vì sao |
| --- | --- |
| Control không có lỗi → `null` | `FormRow` coi `null` là không lỗi |
| Control chưa `touched` **và** `formSubmitted` là `false` → `null` | Người dùng đang gõ lần đầu, chưa rời ô — mục Trạng thái của [`../Design/Components/FormRow.md`](../Design/Components/FormRow.md) |
| Control đã `touched`, hoặc form đã gửi → trả câu lỗi, tính lại mỗi lần giá trị đổi | Ba nhánh còn lại của cùng bảng: rời ô, bấm gửi, đang sửa một ô đã báo lỗi |
| Trả **một** câu đã dịch | `error` của `FormRow` là một chuỗi. Câu đến từ lỗi `server` (đã dịch sẵn ở [`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md) §4.1) hoặc từ mã validator phía client |
| Ô mang **nhiều** lỗi cùng lúc → trả câu của lỗi **đầu tiên**: lỗi server theo thứ tự BE trả, lỗi client theo thứ tự validator khai ở control. Sửa xong lỗi đó, hoặc gửi lại, thì lỗi kế hiện ra | `error` của `FormRow` là một chuỗi, và hiện hết mọi lỗi làm form nhảy ([`../Design/Components/FormRow.md`](../Design/Components/FormRow.md)). Lỗi client tính lại theo giá trị, nên sửa xong lỗi đầu là lỗi kế hiện ngay; lỗi server bị gỡ cả nhóm khi giá trị đổi ([`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md) §4.2), nên mã server kế hiện ra ở lần gửi lại nếu giá trị mới vẫn vi phạm nó |
| Control mang **cả** lỗi `server` lẫn lỗi validator client → trả câu của `server`, cho tới khi giá trị ô đổi | Lỗi server là câu trả lời về **chính giá trị đang nằm trong ô**. Giá trị đổi thì khoá `server` bị gỡ ([`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md) §4.2) và lỗi client, nếu còn, hiện ra |

`formSubmitted` là signal "đã gửi" của page — `daGui()` ở §6.1.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §6.5

---

## 7. Responsive

Điểm ngắt là **biến SCSS** `$bp-*`, giá trị quyết ở [`../Design/DESIGN.md`](../Design/DESIGN.md) §6.3 và áp ở `_spacing.scss`; component dùng qua mixin — không gõ số pixel rải rác. Không khai điểm ngắt thành CSS custom property.

```scss
// src/styles/_spacing.scss — phần điểm ngắt. Mixin nhận một biến $bp-*, không nhận số.
@mixin tu-man-hinh($nguong) {
  @media (min-width: $nguong) {
    @content;
  }
}
```

Component `@use 'spacing' as s;` rồi `@include s.tu-man-hinh(s.$bp-md) { … }`.

| Luật | Vì sao |
| --- | --- |
| Thiết kế từ màn hình nhỏ trở lên (`min-width`), không ngược lại | Quy tắc cộng dồn dễ đọc hơn quy tắc trừ bớt |
| Không ẩn nội dung ở màn hình nhỏ để "cho gọn" | Nội dung bị ẩn là nội dung người dùng di động không bao giờ có |
| Bảng dữ liệu rộng: cuộn ngang **trong khung của nó**, không để cả trang cuộn ngang | Trang cuộn ngang làm mất luôn thanh điều hướng |
| Giá trị breakpoint chỉ khai ở `_spacing.scss` | Ba màn hình dùng ba ngưỡng khác nhau là lỗi không ai báo |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §7

---

## 8. Accessibility — mức tối thiểu bắt buộc

Mức sàn. Nền đầy đủ: [`../wiki-core/fe/15-accessibility.md`](../wiki-core/fe/15-accessibility.md).

| Yêu cầu | Kiểm bằng |
| --- | --- |
| Mọi ô nhập có `<label>` gắn đúng, hoặc `aria-label` khi không có nhãn nhìn thấy | Rule accessibility của angular-eslint trong lệnh lint |
| Nút chỉ có biểu tượng phải có `aria-label` | Như trên |
| Thao tác được bằng bàn phím: tab tới được, Enter/Space kích hoạt | Thử tay khi làm màn hình mới |
| Không dùng **chỉ** màu để truyền trạng thái — kèm biểu tượng hoặc chữ | Review |
| Tương phản chữ/nền đạt mức AA | Token trong [`../Design/`](../Design/) đã tính; không tự chế màu mới |
| `@if` đổi nội dung vùng động thì vùng đó có `aria-live` | Review |

Bộ rule accessibility cho template bật sẵn trong `eslint.config.js`, **không** được tắt — nằm trong danh sách rule cấm `eslint-disable` ([`fe-architecture.md`](fe-architecture.md) §4.5).

---

## 9. Kiểu dữ liệu công khai của thư viện UI — định nghĩa gốc

Mọi kiểu trong mục này là **bề mặt công khai của Core**: đổi chúng là đổi hợp đồng.

🛑 **Đừng đếm số kiểu ở mục này rồi chép ra chỗ khác** ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6).

```typescript
/** Một điều kiện lọc đang bật, hiện thành FilterChip. */
export interface ToolbarChip {
  key: string;
  label: string;                        // tên điều kiện: "Trạng thái"
  value: string | null;                 // giá trị hiển thị: "Hoạt động"
  removable: boolean;                   // false = điều kiện hệ thống áp
  lockReason?: string;                  // bắt buộc khi removable = false
}

/** Một mục trong Menu. */
export interface UiMenuItem {
  key: string;
  label: string;
  icon?: string;                        // tên PrimeIcons, không kèm tiền tố
  disabled?: boolean;
  disabledReason?: string;              // hiện kèm mục bị khoá
  danger?: boolean;                     // nhóm cuối, sau vạch ngăn
  group?: string;
}

/** Một nút trong cây, dùng cho TreeSelect và cho DataTable công tắc tree. */
export interface UiTreeNode {
  key: string;
  label: string;
  code?: string;
  children?: UiTreeNode[];
  hasChildren?: boolean;                // true mà children rỗng = chưa tải
  selectable?: boolean;                 // false = vẫn hiện, vẫn xoè, không chọn
}
```

> 📐 `TimelineItem` (dùng cho `Timeline`) chưa có tệp thật trong `src/FE` hôm nay — chữ ký chuyển sang [`fe-ui-conventions-chua-thi-cong.md`](fe-ui-conventions-chua-thi-cong.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §9

### Họ kiểu CỘT — một gốc, hai mở rộng

Ba component hiển thị dữ liệu dạng cột dùng chung một gốc:

```typescript
/** Gốc chung. Table dùng thẳng kiểu này. */
export interface ColumnDef<T = unknown> {
  key: string;
  header: string;
  width?: string;
  align?: 'start' | 'end';
  hideBelow?: 'xs' | 'sm' | 'md' | 'lg';   // điểm ngắt mà cột bị ẩn
  cell?: TemplateRef<{ $implicit: T }>;
  value?: (row: T) => string;              // ĐÚNG MỘT trong cell/value; value vẽ thành chữ — cột của seam CORE_SCREEN_EXT
}

/** DataTable thêm ba khả năng của một lưới đọc. */
export interface DataColumnDef<T = unknown> extends ColumnDef<T> {
  sortable?: boolean;
  priority?: 'high' | 'low';               // 'low' bị ẩn trước khi thu hẹp
  frozen?: boolean;
}
```

> 📐 `EditableColumnDef`, `DataTableSwitches`, `SummaryRow` (dùng cho `EditableGrid`/`DataTable`) chưa có tệp thật trong `src/FE` hôm nay — chữ ký chuyển sang [`fe-ui-conventions-chua-thi-cong.md`](fe-ui-conventions-chua-thi-cong.md).

**Một cột có đúng một trong `cell` và `value`**; thiếu cả hai thì component ném lỗi lúc dựng cột. `value` dành cho cột thêm qua seam ([`fe-architecture.md`](fe-architecture.md) §2.7).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §9

### Bảy kiểu danh sách lựa chọn

Bảy component nhận mảng lựa chọn; **không** gộp thành một kiểu chung.

```typescript
export interface Option { key: string; label: string; hint?: string }          // Autocomplete
export interface SegmentOption { value: string; label: string; icon?: string; disabled?: boolean }
export type { LanguageOption } from '../../core/config/core-i18n';           // khai ở core (fe-architecture.md §2.5) — cùng kiểu với CORE_I18N.languages
export interface FooterLink { label: string; href: string; external?: boolean }

/** Một trường trong FilterPanel. Seam CORE_SCREEN_EXT chưa nhận trường lọc — hoãn tới khi có FilterPanel (ADR-0057). */
export interface FilterField {
  key: string;
  label: string;
  kind: 'text' | 'number' | 'date' | 'daterange' | 'select' | 'multiselect' | 'tree' | 'check';
                                            // 'date' → DatePicker mode='single'; 'daterange' → mode='range'
  options?: ReadonlyArray<SegmentOption>;   // cho 'select' / 'multiselect'
  nodes?: ReadonlyArray<UiTreeNode>;        // cho 'tree'
  placeholder?: string;
  group?: string;                           // trường cùng group xếp chung <fieldset>
}
```

> 📐 `TabItem`, `StepItem`, `ChartSeries`, `DateRangePreset` (dùng cho `Tabs`/`Stepper`/`Chart`/`DatePicker` mode `range`) chưa có tệp thật trong `src/FE` hôm nay — chữ ký chuyển sang [`fe-ui-conventions-chua-thi-cong.md`](fe-ui-conventions-chua-thi-cong.md).

### Ba kiểu của khung ứng dụng

```typescript
export interface NavItem {                                  // Sidebar — tối đa hai cấp
  id: string; label: string; icon?: string;
  route?: string;                                           // bỏ trống = mục chỉ để nhóm
  children?: ReadonlyArray<NavItem>; disabled?: boolean;
}
```

> 📐 `ToastItem`, `UploadItem` (dùng cho `Toast`/`FileUpload`) chưa có tệp thật trong `src/FE` hôm nay — chữ ký chuyển sang [`fe-ui-conventions-chua-thi-cong.md`](fe-ui-conventions-chua-thi-cong.md).

Ba luật đi kèm:

1. **`UiMenuItem` và `UiTreeNode` mang tiền tố `Ui`, hai kiểu kia thì không**: `MenuItem` đã là tên entity phía backend ([`be-entity-domain.md`](be-entity-domain.md)), `TreeNode` là tên PrimeNG dùng.
2. **Không kiểu nào để lọt kiểu của PrimeNG ra ngoài** ([`../Design/COMPONENTS.md`](../Design/COMPONENTS.md) §4 luật 2).
3. **Mọi chuỗi hiển thị trong MỌI kiểu ở mục này đã đi qua i18n TRƯỚC khi tới component** — không kiểu nào mang khoá dịch.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §9

## 10. Đối chiếu luật

| Luật | Nội dung | Mục |
| --- | --- | --- |
| F5 | Không import trực tiếp `primeng/*` ngoài allowlist | §2 |
| F6 | Không hex literal trong SCSS component | §3 |
| F7 | Không `rgb()`/`rgba()` literal, trừ dạng đọc token | §3.2 |
| F8 | Template không chứa chữ tiếng Việt | §5 |
| F9 | Không còn cú pháp Angular cũ | §1.4 |
| F13 | Mọi `@for` có `track` — `ng build` ép | §1.2 |
| F23 | Mọi file ngôn ngữ có cùng tập khoá | §5.3 |

Bảng đầy đủ: [`../RULES.md`](../RULES.md) §7. Nền lý thuyết: [`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md) · [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) · [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md).
