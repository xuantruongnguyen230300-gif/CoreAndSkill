---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quy ước giao diện — Angular 20, PrimeNG, token, i18n, form

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`. File này là quy ước thi công cho `src/FE`.
>
> **Nguồn giao diện duy nhất là [`../Design/`](../Design/)** — layout, câu chữ, token màu, trạng thái component, ảnh màn hình. File này nói *code phải viết thế nào*; `Design/` nói *nó phải trông thế nào*. Khi hai bên lệch, `Design/` thắng.

---

## 1. Angular 20 — khuôn bắt buộc cho mọi code mới

Stack đã chốt: **Angular 20.3 standalone + signals**, **PrimeNG 20**, **ngx-translate 18**.

| Dùng | **Cấm** |
| --- | --- |
| `standalone: true` (mặc định từ Angular 19) | `NgModule` cho bất kỳ mục đích nào |
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
    this.nguoiDung().hoTen.charAt(0).toUpperCase(),
  );

  protected phatChon(): void {
    this.chon.emit(this.nguoiDung().id);
  }
}
```

Ba điều trong mẫu này là bắt buộc, không phải tuỳ chọn:

1. **`ChangeDetectionStrategy.OnPush` cho mọi component.** Với signal, `OnPush` là chiến lược đúng và không có nhược điểm — Angular biết chính xác signal nào đọc ở template nào. Bỏ `OnPush` nghĩa là mỗi sự kiện trên trang đều chạy lại kiểm tra cho toàn bộ cây component.
2. **`input.required()` khi input thật sự bắt buộc.** Nó biến "quên truyền input" thành lỗi lúc dựng component chứ không phải `undefined` lan xuống template rồi hiện ra ô trống.
3. **Thành viên chỉ dùng trong template để `protected`.** `public` mời gọi component khác gọi vào; `private` thì template không đọc được. `protected` là đúng mức.

### 1.2 `@for` phải có `track` — luật F13

```html
@for (item of items(); track item.id) {
  <app-the-nguoi-dung [nguoiDung]="item" (chon)="moChiTiet($event)" />
} @empty {
  <p class="rong">{{ 'nguoiDung.khongCoDuLieu' | translate }}</p>
}
```

`track` là **bắt buộc về cú pháp** trong Angular 20 — thiếu nó là lỗi biên dịch template. Nhưng cú pháp đúng chưa đủ:

| Biểu thức track | Đánh giá |
| --- | --- |
| `track item.id` | ✔ đúng — khoá ổn định của bản ghi |
| `track $index` | ⚠ chỉ đúng cho danh sách **không bao giờ** chèn/xoá/sắp xếp lại |
| `track item` | ✘ so sánh theo tham chiếu; mapper tạo object mới mỗi lần tải là render lại toàn bộ |

Dùng `$index` cho danh sách có sắp xếp lại gây một lỗi rất khó chẩn đoán: DOM được tái sử dụng cho bản ghi khác, nên trạng thái nằm trong DOM (ô nhập đang gõ dở, checkbox đang chọn, hoạt ảnh) **nhảy sang dòng khác** trong khi dữ liệu hiển thị vẫn đúng.

### 1.3 `@if` và cách viết nhánh

```html
@if (dangTai()) {
  <app-khung-cho-tai />
} @else if (loi()) {
  <app-khoi-loi (thuLai)="tai()" />
} @else {
  <app-bang-du-lieu [items]="items()" />
}
```

**`@empty` của `@for` không thay được nhánh `@if (loi())`.** Danh sách rỗng và tải thất bại là hai trạng thái khác nhau, và gộp chúng lại chính là dạng lỗi đã mô tả ở [`fe-api-client.md`](fe-api-client.md) §1.2: người dùng không phân biệt được "không có gì" với "hỏng".

### 1.4 Cổng

```bash
# Luật F9 — không còn cú pháp Angular cũ.
# PASS khi không in ra dòng nào.
grep -rnE '\*ngIf|\*ngFor|\*ngSwitch|@Input\(\)|@Output\(\)|@ViewChild\(|NgModule' \
  src/FE/src/app --include='*.ts' --include='*.html'
```

Cổng này quét **văn bản**, nên nó cũng bắt cả chú thích nhắc tới cú pháp cũ. Đó là đánh đổi có chủ đích: mẫu đơn giản, không báo nhầm về phía nguy hiểm. Khi cần nhắc tới cú pháp cũ trong comment, **gọi tên** nó ("chỉ thị cấu trúc đời cũ") thay vì gõ lại.

### 1.5 Đặt tên và selector

| Loại | Tên file | Tên class | Selector |
| --- | --- | --- | --- |
| Page (smart) | `<ten>.page.ts` | `TenPage` | `app-<ten>` |
| Component (dumb) | `<ten>.component.ts` | `TenComponent` | `app-<ten>` |
| Directive | `<ten>.directive.ts` | `TenDirective` | `appTen` (camelCase) |
| Service | `<ten>.service.ts` | `TenService` | — |
| Mapper | `<ten>.mapper.ts` | hàm `mapTen()` | — |

Tiền tố `app` khai một lần trong `eslint.config.js` (`@angular-eslint/component-selector` và `directive-selector`), không phải quy ước bằng niềm tin.

**Tên tiếng Việt không dấu cho định danh miền nghiệp vụ** (`NguoiDung`, `PhanQuyen`) là chấp nhận được và nhất quán với phía BE. Định danh kỹ thuật giữ tiếng Anh (`input`, `signal`, `PagedList`). Không trộn hai thứ trong cùng một danh từ.

---

## 2. Bọc PrimeNG — luật có cổng, không phải lời khuyên

### 2.1 Luật F5

> **Component nghiệp vụ không import trực tiếp `primeng/*`. Mọi thứ đi qua lớp bọc ở `shared/ui/`.**

`shared/ui/` giữ các bọc **mỏng** của thư viện; `shared/components/` ghép chúng thành khối có nghĩa với sản phẩm. Phần còn lại của app chỉ biết component `app-*`. Ranh giới giữa hai thư mục: [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §3.

```typescript
// shared/ui/nut/nut.component.ts — bọc, không phải bản sao
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';

type KieuNut = 'chinh' | 'phu' | 'nguyHiem' | 'phang';

@Component({
  selector: 'app-nut',
  standalone: true,
  imports: [ButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-button
      [label]="nhan()"
      [icon]="bieuTuong()"
      [severity]="mucDo()"
      [text]="kieu() === 'phang'"
      [loading]="dangChay()"
      [disabled]="voHieu() || dangChay()"
      (onClick)="bam.emit()"
    />
  `,
})
export class NutComponent {
  readonly nhan = input.required<string>();
  readonly bieuTuong = input<string | undefined>(undefined);
  readonly kieu = input<KieuNut>('chinh');
  readonly dangChay = input(false);
  readonly voHieu = input(false);

  readonly bam = output<void>();

  protected mucDo = computed(() =>
    ({ chinh: 'primary', phu: 'secondary', nguyHiem: 'danger', phang: 'secondary' })[this.kieu()],
  );
}
```

### 2.2 Allowlist — đường dẫn duy nhất được import `primeng/`

> 📖 **Allowlist đầy đủ (đường dẫn nào được, đường dẫn nào cấm, và vì sao từng dòng): đọc [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §2.4.** Cổng F5 đọc đúng bảng đó.

Một allowlist viết ở hai chỗ là một allowlist sẽ lệch; chỗ lệch nằm ở cổng, và cổng lệch thì hoặc chặn nhầm, hoặc không chặn gì.

> Ở dự án tiền nhiệm, số chỗ import trực tiếp thư viện UI trong toàn bộ FE đếm được trên đầu ngón tay, và tất cả đều nằm đúng các nhóm đường dẫn có trong allowlist — cấu hình i18n, cấu hình app, và một component bọc bảng dữ liệu. Đó là **hình mẫu đúng**, và nó chứng minh luật này thi công được chứ không phải lý tưởng trên giấy.

### 2.3 Vì sao bọc — và cái giá của việc bọc

**Vì sao:** breaking change giữa các major của PrimeNG khá nặng, đặc biệt ở phần theming — cơ chế đổi giao diện đã thay hẳn ít nhất một lần trong lịch sử thư viện, kéo theo tên thuộc tính và cách khai màu. Khi đó, một app có mọi component nghiệp vụ import thẳng `primeng/*` phải sửa hàng trăm chỗ; một app đã bọc thì sửa đúng tầng `shared/ui/`.

Cùng lý do đó áp cho cả việc **đổi hẳn thư viện UI**. Không ai lên kế hoạch đổi, nhưng cái giá của việc không thể đổi là thứ chỉ nhận ra khi đã quá muộn.

**Cái giá phải trả, nói thẳng:**

| Chi phí | Mức độ |
| --- | --- |
| Mỗi component thư viện cần một file bọc | Thật, nhưng một lần |
| Bọc dễ trở thành "phơi lại toàn bộ API của thư viện" | Đây là rủi ro chính — xem dưới |
| Một tính năng ít dùng của thư viện có thể chưa được phơi ra | Thêm khi cần, không phơi trước |

🛑 **Bọc không phải là chuyển tiếp mọi thuộc tính.** Một bọc phơi lại đủ mọi `input` của component gốc thì không cách ly được gì: đổi thư viện vẫn phải sửa mọi nơi gọi, vì tên và ngữ nghĩa thuộc tính đến từ thư viện cũ. Bọc đúng là **thu hẹp** — phơi ra đúng những gì sản phẩm này cần, đặt tên theo ngôn ngữ của sản phẩm (`kieu`, `dangChay`), và giấu phần còn lại.

Phép thử một bọc có giá trị hay không: *đổi thư viện bên dưới, có sửa được chỉ trong file bọc này không?* Không → nó chỉ là một lớp gián tiếp.

### 2.4 Khi thư viện thiếu thứ cần

Thứ tự ưu tiên, không đảo:

1. Dùng component có sẵn của thư viện, bọc lại.
2. Ghép nhiều component của thư viện trong một bọc.
3. Tự viết component trong `shared/components/`, không phụ thuộc thư viện — khi đó nó không thuộc `shared/ui/` nữa.
4. Override CSS của thư viện — **chỉ trong file style dành riêng cho override** (§4.4), không rải trong style của component nghiệp vụ.

Bước 4 là bước phải cân nhắc kỹ: nó neo vào tên lớp CSS nội bộ của thư viện, tức neo vào thứ thư viện có quyền đổi trong bản vá nhỏ. Mỗi override phải kèm một chú thích nói **vì sao** và **cái gì sẽ hỏng nếu thư viện đổi**.

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

// ✔ đúng — dạng pha alpha, khi cần một lớp phủ
.lop-phu {
  background: rgb(var(--color-overlay-rgb) / 0.08);
}

// ✘ sai — cả ba dòng
.the-sai {
  background: #ffffff;                    // F6
  border: 1px solid rgba(15, 91, 215, .2); // F7
  color: rgb(31, 41, 55);                  // F7
}
```

### 3.2 Miễn trừ theo CÚ PHÁP, không theo GIÁ TRỊ

`rgb(var(--x) / a)` được tha vì nó **không mang giá trị màu nào** — nó đọc token ra rồi pha độ trong suốt. Đổi theme thì nó đổi theo, không có gì để lệch.

🛑 **Không bao giờ miễn trừ theo giá trị.** Một màu đen nửa trong suốt trông vô hại, nhưng nó vẫn là một **quyết định thiết kế**, và quyết định thiết kế thuộc về [`../Design/`](../Design/). Miễn trừ theo giá trị là mở lại đúng cánh cửa mà luật này sinh ra để đóng — vì danh sách "giá trị vô hại" không có điểm dừng tự nhiên.

Cổng phải gỡ dạng được phép ra khỏi dòng **rồi mới hỏi lại**, chứ không loại cả dòng:

```bash
# Luật F7 — PASS khi không in ra dòng nào.
grep -rnE 'rgba?\(' src/FE/src/app --include='*.scss' \
  | sed -E 's/rgba?\( *var\(--[A-Za-z0-9_-]+\) *\/[^)]*\)//g' \
  | grep -E 'rgba?\('
```

Làm hai bước như vậy để một dòng vừa có dạng hợp lệ vừa có literal trần **vẫn bị bắt**. Lọc bằng một lệnh loại dòng sẽ tha nhầm cả dòng.

### 3.3 Nơi duy nhất được giữ literal

File khai token global (`src/styles/_tokens.scss`) là chỗ token được **định nghĩa**, nên literal ở đó là định nghĩa chứ không phải bản sao. Đó là ngoại lệ duy nhất, và cổng loại trừ đúng file đó — **không** loại trừ cả thư mục.

```scss
// src/styles/_tokens.scss — NƠI DUY NHẤT trong src/ được phép có literal màu
:root {
  /* Mỗi token khai ở docs/Design/DESIGN.md §2 chiếm đúng một dòng ở đây,
     theo dạng:   <tên token>: <giá trị cột "sáng">;
     Khối tối đặt trong @media (prefers-color-scheme: dark) và :root[data-theme].
     Danh sách KHÔNG chép sang tài liệu nào — kể cả file này. */
}
```

🛑 **Khối trên cố ý rỗng.** Nó dạy **hình dạng** của file token, không dạy nội dung. Một khối minh hoạ có tên token thật là cách hệ token thứ hai ra đời: người đọc file quy ước FE lấy luôn tên trong ví dụ đi dùng, và không bao giờ mở [`../Design/DESIGN.md`](../Design/DESIGN.md) ra xem tên thật là gì.

Đây cũng là lý do file này **không được** chứa một dòng `--token: giá-trị` nào — xem luật **D30** ở [`../RULES.md`](../RULES.md).

### 3.4 Chiều cập nhật — `Design/` là nguồn, code đuổi theo

```
docs/Design/  ──(người quyết định thiết kế sửa)──▶  src/styles/_tokens.scss  ──▶  component
```

Không có chiều ngược. Khi cần một màu mới:

1. Thêm token vào [`../Design/DESIGN.md`](../Design/DESIGN.md) kèm tên và ngữ cảnh dùng.
2. Thêm biến vào file token global.
3. Dùng `var(--ten-token)` trong component.

Bỏ bước 1 nghĩa là bảng màu thật nằm trong code còn tài liệu thiết kế nói dối — và lần sau có người mở tài liệu ra làm chuẩn, họ sẽ tạo màu thứ hai gần giống.

> ⚠️ **Đừng chép danh sách token vào file này hay vào bất kỳ tài liệu nào khác.** Ở dự án tiền nhiệm, một bảng liệt kê các chỗ hardcode màu sai quá nửa số dòng đồng thời bỏ sót vài dòng đúng — đúng khuôn mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6 cấm. Đếm bằng lệnh ở §3.2.

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

**Vì sao tách bốn file thay vì một `styles.scss` lớn:** ở dự án tiền nhiệm, một file style vượt một nghìn dòng. Không ai đọc hết một file như vậy trước khi thêm dòng thứ một nghìn lẻ một, nên nó tích tụ ba thứ cùng lúc — quy tắc trùng nhau, override chồng override, và literal màu ẩn giữa hàng trăm dòng không liên quan. Bốn file theo bốn chủ đề khiến câu hỏi *"dòng này thuộc đâu"* có câu trả lời, và khiến việc một file phình lên trở thành tín hiệu đọc được.

Ngưỡng dòng cho từng file: [`fe-architecture.md`](fe-architecture.md) §5.

### 4.2 Style của component

Style của component nằm trong `styleUrl` của chính nó, phạm vi đóng gói mặc định của Angular. Ba luật:

1. **Không `::ng-deep`.** Nó là API đã bị đánh dấu ngừng dùng và nó rò style ra ngoài phạm vi component — tức phá đúng thứ đóng gói style sinh ra để làm. Cần chạm vào bên trong component thư viện → dùng file override (§4.4).
2. **Không `!important`** trừ khi kèm chú thích nói rõ nó đang đè cái gì và vì sao không đè được bằng độ ưu tiên chọn lọc. Không có chú thích thì đó là finding trong review.
3. **Không selector theo thẻ trần** (`div`, `span`) ở cấp cao. Đặt lớp có tên.

### 4.3 Lớp tiện ích

Được phép có một tập lớp tiện ích nhỏ trong `styles.scss` (căn lề, ẩn khi in, chỉ hiện cho trình đọc màn hình). **Không** dựng cả một hệ tiện ích tự chế song song với hệ token — hai hệ thì mỗi màn hình sẽ dùng một hệ.

### 4.4 Override thư viện

Gom vào `_thu-vien.scss`. Mỗi khối override bắt buộc có chú thích trả lời hai câu:

```scss
// Vì sao: bảng của thư viện đặt padding dòng lớn hơn thang khoảng cách của Design.
// Hỏng khi nào: thư viện đổi tên lớp nội bộ này ở bản nâng cấp — kiểm lại sau mỗi lần nâng major.
.p-datatable .p-datatable-tbody > tr > td {
  padding: var(--khoang-cach-2) var(--khoang-cach-3);
}
```

Không có hai câu đó, người nâng cấp thư viện sau này không có cách nào biết dòng nào xoá được.

---

## 5. i18n — template không chứa chữ tiếng Việt

### 5.1 Luật F8

> **Mọi câu người dùng đọc đến từ file ngôn ngữ. Template `.html` không chứa chữ tiếng Việt.**

```html
<!-- ✔ đúng -->
<h1>{{ 'nguoiDung.tieuDe' | translate }}</h1>
<app-nut [nhan]="'chung.luu' | translate" (bam)="luu()" />

<!-- ✘ sai -->
<h1>Quản trị người dùng</h1>
```

### 5.2 Cổng dò dấu thanh

```bash
# Luật F8 — PASS khi không in ra dòng nào.
find src/FE/src/app -name '*.html' | while IFS= read -r f; do
  perl -0777 -pe 's{<!--.*?-->}{ "\n" x (() = ($& =~ /\n/g)) }gse' "$f" \
    | grep -nE '[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]' \
    | sed "s|^|$f:|"
done
```

Ba quyết định trong thiết kế cổng này:

1. **Dò dấu thanh, không dò "text node ngoài ống dịch".** Dấu thanh không thể là tên biến, tên lớp CSS hay từ khoá Angular, nên gần như không báo nhầm — mà lại không cần phân tích cú pháp template.
2. **Xoá comment HTML trước khi dò**, vì chú thích viết tiếng Việt là chuyện bình thường và không ai đọc chú thích trên màn hình.
3. **Thay comment bằng đúng số xuống dòng nó chiếm, không xoá trắng.** Xoá trắng làm luồng ngắn lại, và số dòng cổng báo ra sẽ lệch so với file thật. Một cổng chỉ sai số dòng thôi cũng đủ làm người sửa mở nhầm chỗ rồi kết luận cổng báo bậy.

**Giới hạn đã biết:** cổng không bắt được chuỗi cứng tiếng Anh (`"Save"`). Đây là đánh đổi có chủ đích — mẫu đơn giản, gần như không báo nhầm, đổi lại một khoảng mù hẹp mà review người vẫn thấy.

### 5.3 Đặt khoá dịch

Khoá phẳng theo miền, phân cấp bằng dấu chấm, camelCase ở mỗi đoạn:

```
chung.luu
chung.huy
chung.xoa
chung.xacNhanXoa
nguoiDung.tieuDe
nguoiDung.cot.hoTen
nguoiDung.thongBao.taoThanhCong
loi.CORE.USER.EMAIL_DUPLICATED
```

| Quy tắc | Vì sao |
| --- | --- |
| Đoạn đầu là **miền**, không phải tên màn hình | Màn hình đổi tên nhiều hơn miền đổi tên |
| Khoá dưới `chung.` chỉ cho câu thật sự dùng ở nhiều miền | Nếu không, `chung.` biến thành ngăn kéo rác |
| Khoá lỗi là `loi.<mã lỗi BE>` — giữ nguyên mã BE, **không** đổi sang camelCase | Để tra được bằng chính mã BE trả về, không cần một bảng chuyển đổi thứ hai |
| Mọi ngôn ngữ có **cùng tập khoá** | Thiếu khoá ở một ngôn ngữ thì câu đó hiện ra dưới dạng chính khoá |

```bash
# Hai file ngôn ngữ phải có cùng tập khoá. PASS khi diff không in dòng nào.
diff <(jq -r 'paths(scalars) | join(".")' src/FE/public/i18n/vi.json | sort) \
     <(jq -r 'paths(scalars) | join(".")' src/FE/public/i18n/en.json | sort)
```

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

**Tham số truyền theo TÊN, không theo thứ tự.** Tham số theo thứ tự hỏng im lặng khi câu dịch của ngôn ngữ khác đảo trật tự — mà tiếng Việt và tiếng Anh đảo trật tự thường xuyên. Đây cũng là luật phía BE khi trả `messageParams` ([`be-api-controller.md`](be-api-controller.md)).

**Số nhiều:** tiếng Việt không biến đổi theo số, nên cám dỗ là bỏ qua hẳn vấn đề. Đừng — bảng tiếng Anh cần, và câu tiếng Việt viết cho một dạng số sẽ đọc kỳ khi ghép. Khuôn: khai hai khoá con và chọn ở template.

```json
{ "nguoiDung": { "soDong": { "mot": "1 dòng", "nhieu": "{{soLuong}} dòng" } } }
```

```html
{{ (soDong() === 1 ? 'nguoiDung.soDong.mot' : 'nguoiDung.soDong.nhieu') | translate: { soLuong: soDong() } }}
```

📐 Nếu sau này có ngôn ngữ với nhiều hơn hai dạng số, khuôn này phải đổi sang cơ chế số nhiều đầy đủ. Ghi lại đây để lúc đó biết chỗ nào phải sửa, thay vì phát hiện qua một câu hiển thị sai.

### 5.5 Ngày, giờ, số

Định dạng bằng pipe chuẩn của Angular với locale lấy từ ngôn ngữ hiện tại — **không** tự ghép chuỗi ngày, không tự chèn dấu phân cách hàng nghìn.

```html
{{ nguoiDung().lanDangNhapCuoi | date: 'short' }}
{{ tongTien() | number: '1.0-0' }}
```

Đăng ký dữ liệu locale ở `app.config.ts` cùng chỗ cấu hình ngôn ngữ. Quên bước đó thì pipe im lặng rơi về locale mặc định và ngày hiện ra theo khuôn của một nước khác.

---

## 6. Form — typed reactive form

### 6.1 Khuôn

Chỉ dùng **reactive form có kiểu**. Không template-driven form (`ngModel`) cho form nghiệp vụ: nó không kiểm được kiểu, không đặt được validator theo nhóm, và trạng thái nằm rải trong template.

```typescript
@Component({
  selector: 'app-form-nguoi-dung',
  standalone: true,
  imports: [ReactiveFormsModule, TranslateModule, InputTextComponent, NutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './form-nguoi-dung.page.html',
})
export class FormNguoiDungPage {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly service = inject(NguoiDungService);

  protected readonly dangGui = signal(false);

  protected readonly form = this.fb.group({
    ho: this.fb.control('', [Validators.required, Validators.maxLength(50)]),
    ten: this.fb.control('', [Validators.required, Validators.maxLength(50)]),
    email: this.fb.control('', [Validators.required, Validators.email]),
  });

  protected gui(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.dangGui.set(true);
    this.service
      .them(this.form.getRawValue())
      .pipe(finalize(() => this.dangGui.set(false)))
      .subscribe({
        next: () => this.dieuHuongVeDanhSach(),
        error: (err) => this.ganLoiTuServer(err),
      });
  }
}
```

**`NonNullableFormBuilder` chứ không `FormBuilder`.** Với `FormBuilder`, kiểu của mọi control là `T | null` vì `reset()` có thể đưa nó về null — nên mọi chỗ đọc giá trị đều phải xử lý null mà thực tế không bao giờ null. `NonNullableFormBuilder` cho `reset()` quay về giá trị khởi tạo và kiểu sạch.

**`getRawValue()` chứ không `value`.** `value` bỏ qua control đang bị vô hiệu hoá — nên một field chỉ-đọc sẽ biến mất khỏi payload, im lặng, và BE nhận thiếu field.

### 6.2 Gắn lỗi từ `fieldErrors` của BE

Page **không** tự viết vòng lặp gắn lỗi. Nó gọi hàm dùng chung ở `shared/forms/`:

```typescript
private ganLoiTuServer(err: unknown): void {
  if (!(err instanceof ApiFailureError)) {
    return; // toast đã do interceptor bắn
  }
  const khongKhop = applyFieldErrors(this.form, err.body?.error?.fieldErrors, this.translate);
  if (khongKhop.length > 0) {
    // Field BE báo lỗi nhưng form không có ô tương ứng: KHÔNG im lặng bỏ qua.
    // Người dùng sẽ thấy form không báo gì và nút không có tác dụng.
    this.toast.loi(this.translate.instant('loi.duLieuKhongHopLe'));
  }
}
```

> 📖 **Hàm `applyFieldErrors` — chữ ký, các chi tiết bắt buộc, cách xử lý khoá lồng nhau và khoá không khớp: đọc [`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md) §4.** Đó là chỗ **duy nhất** hiện thực việc gắn `fieldErrors` vào form. Không viết lại việc đó thành phương thức private trong page — hai bản của cùng một hàm thì bản không ai sửa sẽ lệch ở đúng chỗ hỏng im lặng nhất.

Ba điểm phải nhớ khi gọi:

1. **Khoá của `fieldErrors` là tên property gốc phía BE**, thường PascalCase — bước chuyển về tên control nằm **trong** hàm dùng chung, không lặp lại ở page ([`fe-api-client.md`](fe-api-client.md) §1.3).
2. **Giá trị là danh sách mã lỗi, không phải danh sách câu.** Hàm dùng chung dịch từng mã; gắn thẳng object vào control cho ra `[object Object]` trên màn hình và vứt mất mã lỗi.
3. **Lỗi từ server bị xoá ngay khi người dùng sửa ô đó.** Nếu không, lỗi cũ đứng lì và người dùng sửa đúng rồi vẫn thấy báo sai.

### 6.3 Validate phía client hay phía server

| Loại kiểm tra | Client | Server | Vì sao |
| --- | --- | --- | --- |
| Bắt buộc, độ dài, khuôn dạng, kiểu | ✔ | ✔ | Client cho phản hồi tức thì; server vì client bỏ qua được |
| So sánh giữa các field trong cùng form (ngày kết thúc sau ngày bắt đầu) | ✔ | ✔ | Như trên |
| Trùng lặp trong cơ sở dữ liệu (email đã tồn tại) | ✘ | ✔ | Client không biết dữ liệu; kiểm ở client là một lời hứa không giữ được — hai người có thể đăng ký cùng lúc |
| Quyền được thực hiện thao tác | ✘ | ✔ | Quyền phía client chỉ để **ẩn nút**, không để quyết định |
| Luật nghiệp vụ (hạn mức, trạng thái hợp lệ để chuyển tiếp) | ✘ | ✔ | Luật thuộc về Domain; chép sang FE là tạo bản sao sẽ lệch |

**Luật một câu:** client validate để *người dùng đỡ chờ*; server validate để *dữ liệu đúng*. Không bao giờ chỉ có vế đầu.

**Hệ quả với thông báo:** lỗi mà chỉ server biết (dòng 3–5 trong bảng) sẽ tới dưới dạng `error.fieldErrors` hoặc `error.code`. Form phải hiển thị được chúng — nghĩa là ô nhập phải có chỗ hiện lỗi kể cả khi không có validator client nào gắn vào nó.

### 6.4 Hiển thị lỗi

```html
<app-input-text
  [control]="form.controls.email"
  [nhan]="'nguoiDung.cot.email' | translate"
  [batBuoc]="true"
/>
```

Component bọc ô nhập trong `shared/` tự chịu trách nhiệm hiện lỗi, theo một khuôn duy nhất cho cả app: chỉ hiện khi control `touched` hoặc form đã gửi một lần. Hiện lỗi ngay khi người dùng chưa gõ gì là cách nhanh nhất làm một form trông như đã hỏng.

Chi tiết nền về form: [`../wiki-core/fe/09-forms-validation.md`](../wiki-core/fe/09-forms-validation.md).

---

## 7. Responsive

Ngưỡng breakpoint khai một chỗ trong `_spacing.scss`, dùng qua mixin — không gõ số pixel rải rác.

```scss
@use 'spacing' as s;

.luoi {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--khoang-cach-3);

  @include s.tu-man-hinh(md) {
    grid-template-columns: repeat(2, 1fr);
  }

  @include s.tu-man-hinh(lg) {
    grid-template-columns: repeat(3, 1fr);
  }
}
```

| Luật | Vì sao |
| --- | --- |
| Thiết kế từ màn hình nhỏ trở lên (`min-width`), không ngược lại | Quy tắc cộng dồn dễ đọc hơn quy tắc trừ bớt |
| Không ẩn nội dung ở màn hình nhỏ để "cho gọn" | Nội dung bị ẩn là nội dung người dùng di động không bao giờ có |
| Bảng dữ liệu rộng: cuộn ngang **trong khung của nó**, không để cả trang cuộn ngang | Trang cuộn ngang làm mất luôn thanh điều hướng |
| Giá trị breakpoint chỉ khai ở `_spacing.scss` | Ba màn hình dùng ba ngưỡng khác nhau là lỗi không ai báo |

---

## 8. Accessibility — mức tối thiểu bắt buộc

Đây là mức sàn, không phải mục tiêu. Nền đầy đủ ở [`../wiki-core/fe/15-accessibility.md`](../wiki-core/fe/15-accessibility.md).

| Yêu cầu | Kiểm bằng |
| --- | --- |
| Mọi ô nhập có `<label>` gắn đúng, hoặc `aria-label` khi không có nhãn nhìn thấy | Rule accessibility của angular-eslint trong lệnh lint |
| Nút chỉ có biểu tượng phải có `aria-label` | Như trên |
| Thao tác được bằng bàn phím: tab tới được, Enter/Space kích hoạt | Thử tay khi làm màn hình mới |
| Không dùng **chỉ** màu để truyền trạng thái — kèm biểu tượng hoặc chữ | Review |
| Tương phản chữ/nền đạt mức AA | Token trong [`../Design/`](../Design/) đã tính; không tự chế màu mới |
| `@if` đổi nội dung vùng động thì vùng đó có `aria-live` | Review |

Bộ rule accessibility cho template bật sẵn trong `eslint.config.js` và **không** được tắt — nó nằm trong danh sách rule cấm `eslint-disable` ở [`fe-architecture.md`](fe-architecture.md) §4.5.

---

## 9. Kiểu dữ liệu công khai của thư viện UI — định nghĩa gốc

Component Core nhận cấu hình qua những **kiểu có tên**, và cho tới nay chúng chỉ được mô tả bằng một câu văn xuôi trong cột ghi chú của bảng API. Một câu văn xuôi không phải hợp đồng: dự án thứ nhất sẽ khai nó một kiểu, dự án thứ hai khai kiểu khác, và lúc kéo bản vá Core theo [`../adr/0016-phan-phoi-core-bang-clone.md`](../adr/0016-phan-phoi-core-bang-clone.md) thì hai bên không hợp nhất được. Đó là **fork** — chỉ khác là nó xảy ra ở tầng kiểu chứ không ở tầng file.

Mọi kiểu trong mục này là **bề mặt công khai của Core**. Dự án hạ nguồn dựa vào chúng; đổi chúng là đổi hợp đồng.

🛑 **Đừng đếm số kiểu trong mục này và chép con số đó ra chỗ khác** ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6). Mục này còn dài ra.

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

/** Một mục trong Timeline, dùng cho cả hai biến thể. */
export interface TimelineItem {
  key: string;
  state: 'done' | 'current' | 'rejected' | 'upcoming';
  title: string;
  meta?: string;                        // thời gian tuyệt đối, địa chỉ IP…
  quote?: string;                       // lý do, ghi chú
  change?: { from: string; to: string };
  interactive?: boolean;
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

### Họ kiểu CỘT — một gốc, hai mở rộng

Ba component cùng hiển thị dữ liệu dạng cột, và nếu mỗi cái khai một kiểu riêng thì repo có ba định nghĩa cho cùng một khái niệm. Chúng dùng chung một gốc:

```typescript
/** Gốc chung. Table dùng thẳng kiểu này. */
export interface ColumnDef<T = unknown> {
  key: string;
  header: string;
  width?: string;
  align?: 'start' | 'end';
  hideBelow?: 'xs' | 'sm' | 'md' | 'lg';   // điểm ngắt mà cột bị ẩn
  cell?: TemplateRef<{ $implicit: T }>;
}

/** DataTable thêm ba khả năng của một lưới đọc. */
export interface DataColumnDef<T = unknown> extends ColumnDef<T> {
  sortable?: boolean;
  priority?: 'high' | 'low';               // 'low' bị ẩn trước khi thu hẹp
  frozen?: boolean;
}

/** EditableGrid thêm ba khả năng của một lưới ghi. */
export interface EditableColumnDef<T = unknown> extends ColumnDef<T> {
  editor?: 'text' | 'number' | 'date' | 'select' | 'check';
  computed?: boolean;                      // máy tính ra, không gõ được
  validate?: (value: unknown, row: T) => string | null;  // null = hợp lệ
}

/** Sáu công tắc của DataTable. Khai bằng MỘT object có tên, không phải sáu input
 *  boolean rời: sáu cờ rời cho phép biểu diễn tổ hợp vô nghĩa, và mỗi nơi gọi
 *  lại xử lý tổ hợp đó một kiểu. */
export interface DataTableSwitches {
  selectable?: boolean;
  frozen?: boolean | 'first' | 'both';   // 'both' = ghim cả cột đầu và cột hành động
  tree?: boolean;
  grouped?: boolean;                     // đi kèm `groupBy`
  expandable?: boolean;                  // đi kèm `rowDetail`
  summary?: boolean;                     // đi kèm `summary` và `summaryScope`
}

/** Dòng tổng của DataTable, khi bật công tắc `summary`. */
export interface SummaryRow {
  label: string;                           // ĐÃ kèm phạm vi: "Tổng cộng 137 bản ghi"
  values: Record<string, string>;          // khoá cột → giá trị ĐÃ định dạng
}
```

🛑 **Ba tên `TableColumn`, `GridColumn`, `ColumnDef` từng cùng tồn tại cho cùng một khái niệm.** Đó là ba định nghĩa sẽ lệch nhau, và lệch ở chỗ không ai thấy: dự án thứ nhất thêm một trường vào `GridColumn`, dự án thứ hai thêm trường cùng nghĩa khác tên vào `TableColumn`, và bản vá Core không hợp nhất được. Một gốc với hai mở rộng giữ cho phần chung thật sự chung.

**`values` của `SummaryRow` là chuỗi đã định dạng, không phải số.** Định dạng tiền tệ, phân cách nghìn và số chữ số thập phân là quyết định của nghiệp vụ; component không được đoán hộ. Nhận số rồi tự định dạng là đường ngắn nhất để một bảng hiện `2,152` ở chỗ lẽ ra phải là `2.152`.

### Bảy kiểu danh sách lựa chọn

Bảy component nhận một mảng lựa chọn. Chúng **không** gộp thành một kiểu chung: mỗi cái mang một trường riêng mà các cái khác không có nghĩa để dùng.

```typescript
export interface Option { key: string; label: string; hint?: string }          // Autocomplete
export interface SegmentOption { value: string; label: string; icon?: string; disabled?: boolean }
export interface TabItem { key: string; label: string; icon?: string; badge?: number; disabled?: boolean }
export interface StepItem { key: string; label: string; note?: string; state: 'done' | 'current' | 'upcoming' | 'error' }
export interface LanguageOption { code: string; nativeName: string }           // tên viết bằng CHÍNH ngôn ngữ đó
export interface FooterLink { label: string; href: string; external?: boolean }
export interface ChartSeries { key: string; label: string; points: ReadonlyArray<number | null> }
export interface DateRangePreset { key: string; label: string; from: Date; to: Date }   // Input type='daterange'

/** Một trường trong FilterPanel. Đây là điểm mở rộng của bộ lọc: dự án hạ nguồn
 *  thêm trường qua CORE_SCREEN_EXT.filterFields, không sửa file Core. */
export interface FilterField {
  key: string;
  label: string;
  kind: 'text' | 'number' | 'date' | 'daterange' | 'select' | 'multiselect' | 'tree' | 'check';
  options?: ReadonlyArray<SegmentOption>;   // cho 'select' / 'multiselect'
  nodes?: ReadonlyArray<UiTreeNode>;        // cho 'tree'
  placeholder?: string;
  group?: string;                           // trường cùng group xếp chung <fieldset>
}
```

**`DateRangePreset` là điểm mở rộng của biến thể `daterange`.** Core khai bộ lối tắt mặc định độc lập nghiệp vụ (hôm nay, 7 ngày qua, tháng này, quý này, năm nay); lối tắt mang nghĩa nghiệp vụ — "kỳ đang mở", "kỳ lương" — do trang truyền vào qua đây. `from`/`to` là ngày đã tính sẵn, **không** phải một biểu thức để component tự diễn giải: tính kỳ là việc của nghiệp vụ.

### Ba kiểu của khung ứng dụng

```typescript
export interface NavItem {                                  // Sidebar — tối đa hai cấp
  id: string; label: string; icon?: string;
  route?: string;                                           // bỏ trống = mục chỉ để nhóm
  children?: ReadonlyArray<NavItem>; disabled?: boolean;
}
export interface ToastItem {                                // Toast — service cắt còn tối đa ba
  id: string; severity: 'success' | 'warning' | 'danger' | 'info';
  title: string; message?: string; actionLabel?: string; duration?: number;
}
export interface UploadItem {                               // FileUpload
  id: string; name: string; size: number;
  status: 'queued' | 'uploading' | 'done' | 'failed' | 'rejected';
  progress: number | null;                                  // null khi chưa bắt đầu hoặc không đo được
  error?: string;
}
```

`UploadItem.progress` nhận `null` **có chủ đích**: tải lên qua một proxy không trả về tiến độ là chuyện thật, và lúc đó [`../Design/Components/ProgressBar.md`](../Design/Components/ProgressBar.md) phải chuyển sang biến thể `indeterminate` chứ không được bịa một con số.

Hai điều đáng chú ý:

- **`StepItem`, không phải `Step`.** `Step` quá chung và sẽ trùng với từ vựng của nghiệp vụ đầu tiên có quy trình nhiều bước.
- **`points` của `ChartSeries` cho phép `null`.** `null` nghĩa là *không có dữ liệu tại điểm đó*, khác hẳn `0` nghĩa là *có dữ liệu và bằng không*. Trộn hai thứ làm đường biểu đồ tụt xuống đáy ở những tháng chưa phát sinh — một cú sụt không có thật, đúng thứ [`../Design/Components/Chart.md`](../Design/Components/Chart.md) cấm ở Do / Don't.

Ba luật đi kèm, và cả ba đều có lý do đã trả giá:

1. **`UiMenuItem` và `UiTreeNode` mang tiền tố `Ui`, hai kiểu kia thì không.** Không phải thiếu nhất quán: `MenuItem` đã là tên một entity phía backend ([`be-entity-domain.md`](be-entity-domain.md)), và `TreeNode` là tên PrimeNG dùng. Trùng tên với một thứ khác nghĩa là mỗi lần import phải đọc kỹ dòng `import` mới biết đang nói về cái gì — và có lúc sẽ đọc nhầm.
2. **Không kiểu nào để lọt kiểu của PrimeNG ra ngoài.** [`../Design/COMPONENTS.md`](../Design/COMPONENTS.md) §4 luật 2: bọc nghĩa là giấu hẳn. Lọt ra là mất luôn cái lợi duy nhất của việc bọc.
3. **Mọi chuỗi hiển thị trong MỌI kiểu ở mục này đã đi qua i18n TRƯỚC khi tới component.** Không kiểu nào mang khoá dịch; tất cả mang **chuỗi đã dịch**.

   Đây là luật quyết định `Sidebar`, `Menu`, `Tabs` và mọi component nhận danh sách còn **dumb** hay không. Nhận khoá dịch nghĩa là component phải inject một service dịch để hiển thị được — và lúc đó nó vi phạm [`../Design/COMPONENTS.md`](../Design/COMPONENTS.md) §5 cùng luật F11. Trang cha phân giải khoá rồi truyền chuỗi xuống; đó là việc của tầng smart.

   Cái giá phải trả, nói thẳng: **đổi ngôn ngữ lúc chạy buộc trang cha dựng lại mảng**, không phải component tự cập nhật. Với `ngx-translate` thì đó là một `computed()` đọc tín hiệu ngôn ngữ hiện hành — rẻ, nhưng phải nhớ làm, và quên thì menu đứng nguyên tiếng cũ sau khi đổi ngôn ngữ.

## 10. Đối chiếu luật

| Luật | Nội dung | Mục |
| --- | --- | --- |
| F5 | Không import trực tiếp `primeng/*` ngoài allowlist | §2 |
| F6 | Không hex literal trong SCSS component | §3 |
| F7 | Không `rgb()`/`rgba()` literal, trừ dạng đọc token | §3.2 |
| F8 | Template không chứa chữ tiếng Việt | §5 |
| F9 | Không còn cú pháp Angular cũ | §1.4 |
| F13 | Mọi `@for` có `track` | §1.2 |

Bảng đầy đủ: [`../RULES.md`](../RULES.md) §7. Nền lý thuyết: [`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md) · [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) · [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md).
