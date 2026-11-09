---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng — `fe-ui-conventions.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`fe-ui-conventions.md`](../../../quy-uoc/fe-ui-conventions.md); luật ở đó. Số mục dưới đây trùng số mục của file luật; mục không có gì dời ghi "—".

---

## 1. Angular — khuôn bắt buộc cho mọi code mới

—

### 1.1 Khuôn một component

- **Vì sao `OnPush` cho mọi component** — Với signal, `OnPush` là chiến lược đúng và không có nhược điểm — Angular biết chính xác signal nào đọc ở template nào. Bỏ `OnPush` nghĩa là mỗi sự kiện trên trang đều chạy lại kiểm tra cho toàn bộ cây component.

- **Vì sao `input.required()`** — Nó biến "quên truyền input" thành lỗi lúc dựng component chứ không phải `undefined` lan xuống template rồi hiện ra ô trống.

- **Vì sao `protected`** — `public` mời gọi component khác gọi vào; `private` thì template không đọc được. `protected` là đúng mức.

### 1.2 `@for` phải có `track` — luật F13

Dùng `$index` cho danh sách có sắp xếp lại gây một lỗi rất khó chẩn đoán: DOM được tái sử dụng cho bản ghi khác, nên trạng thái nằm trong DOM (ô nhập đang gõ dở, checkbox đang chọn, hoạt ảnh) **nhảy sang dòng khác** trong khi dữ liệu hiển thị vẫn đúng.

### 1.3 `@if` và cách viết nhánh

- **Vì sao không đặt khoá minh hoạ riêng** — một khoá minh hoạ sẽ được chép vào code rồi tra trượt, và tra trượt thì ngx-translate **im lặng** hiện nguyên chuỗi khoá.

- **Vì sao `@empty` không thay được nhánh lỗi** — Danh sách rỗng và tải thất bại là hai trạng thái khác nhau, và gộp chúng lại chính là dạng lỗi đã mô tả ở [`fe-api-client.md`](../../../quy-uoc/fe-api-client.md) §1.2: người dùng không phân biệt được "không có gì" với "hỏng".

### 1.4 Cổng

Cổng này quét **văn bản**, nên nó cũng bắt cả chú thích nhắc tới cú pháp cũ. Đó là đánh đổi có chủ đích: mẫu đơn giản, không báo nhầm về phía nguy hiểm. Khi cần nhắc tới cú pháp cũ trong comment, **gọi tên** nó ("chỉ thị cấu trúc đời cũ") thay vì gõ lại.

### 1.5 Đặt tên và selector

—

## 2. Bọc PrimeNG — luật có cổng, không phải lời khuyên

—

### 2.1 Luật F5

```typescript
// shared/ui/data-table/data-table.component.ts — bọc, không phải bản sao. Trích, đối chiếu 2026-10-06:
// giữ phần sắp xếp và phần chọn slot trạng thái; cột, thân bảng, phân trang, tên truy cập, track dòng lược bỏ.
import { TableModule } from 'primeng/table';

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [NgTemplateOutlet, TableModule, PaginationComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.scss',
})
export class DataTableComponent<T> {
  readonly rows = input<readonly T[]>([]);
  readonly state = input<DataTableViewState>('idle');
  readonly sortBy = input<string | null>(null);
  readonly sortDescending = input(false);

  readonly emptyTemplate = input<TemplateRef<{ $implicit: 'empty' | 'empty-filtered' }> | null>(
    null,
  );
  readonly loadingTemplate = input<TemplateRef<unknown> | null>(null);
  readonly errorTemplate = input<TemplateRef<unknown> | null>(null);

  readonly sortChange = output<{ sortBy: string; sortDescending: boolean }>();

  protected readonly dangTai = computed(
    () => this.state() === 'loading' && this.rows().length === 0,
  );
  protected readonly rong = computed(
    () => this.state() === 'empty' || this.state() === 'empty-filtered',
  );

  /** `error` thì thân bảng là slot lỗi BẤT KỂ `rows`: PrimeNG chỉ vẽ `#emptymessage` khi `value` rỗng. */
  protected readonly dsHangMutable = computed(() =>
    this.state() === 'error' ? [] : [...this.rows()],
  );

  protected sapXep(cot: DataColumnDef<T>): void {
    if (!cot.sortable) return;
    const dangCungCot = this.sortBy() === cot.key;
    const desc = dangCungCot ? !this.sortDescending() : false;
    this.sortChange.emit({ sortBy: cot.key, sortDescending: desc });
  }
}
```

```html
<!-- shared/ui/data-table/data-table.component.html — trích: chỉ nút sắp xếp và khe chọn slot -->
<p-table [value]="dsHangMutable()" [dataKey]="rowKey()" [rowTrackBy]="khoaHang">
  <ng-template #header>
    <!-- cột sortable: <th scope="col" [attr.aria-sort]="ariaSort(cot)"> bọc một nút tự vẽ -->
    <button type="button" class="data-table__nut-sap-xep" (click)="sapXep(cot)">…</button>
  </ng-template>
  <ng-template #emptymessage>
    <tr>
      <td [attr.colspan]="cotHopLe().length">
        @if (dangTai()) {
          <ng-container [ngTemplateOutlet]="loadingTemplate()" />
        } @else if (state() === 'error') {
          <ng-container [ngTemplateOutlet]="errorTemplate()" />
        } @else if (rong()) {
          <ng-container
            [ngTemplateOutlet]="emptyTemplate()"
            [ngTemplateOutletContext]="{ $implicit: state() }"
          />
        }
      </td>
    </tr>
  </ng-template>
</p-table>
```

- **Vì sao `DataTable` nhận khối trạng thái qua `TemplateRef`** — Lớp bọc ở `shared/ui/` không import component tự dựng, nên khối trống, khung đang tải và khối lỗi vào `DataTable` qua ba input `TemplateRef`; màn ghép chúng vào ([`fe-architecture.md`](../../../quy-uoc/fe-architecture.md) §2.8).

- **Về mẫu `DataTableComponent`** — Mẫu giữ phần **sắp xếp** và phần **chọn slot**, vì đó là hai việc lớp bọc tồn tại để làm. Sắp xếp đi qua nút tiêu đề tự vẽ, phát thẳng tên trên dây (`sortBy`, `sortDescending`); lớp bọc không dùng cơ chế sắp xếp của `p-table`, nên không có dạng `1 | -1` nào của thư viện phải quy đổi. Chọn slot nằm trong khe `#emptymessage`, mà PrimeNG chỉ vẽ khe đó khi `value` rỗng — vì vậy nhánh `error` đưa mảng rỗng vào `p-table` qua `dsHangMutable`, để dòng của trang trước không che slot lỗi. `DataTable` quyết **khi nào** hiện slot nào, màn quyết **hiện gì**. API công khai đầy đủ ở [`../Design/Components/DataTable.md`](../../../Design/Components/DataTable.md).

### 2.2 Allowlist — đường dẫn duy nhất được import `primeng/`

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

- **Bọc chuyển tiếp mọi thuộc tính thì sao** — Một bọc phơi lại đủ mọi `input` của component gốc thì không cách ly được gì: đổi thư viện vẫn phải sửa mọi nơi gọi, vì tên và ngữ nghĩa thuộc tính đến từ thư viện cũ.

- **Phép thử nhanh để biết một bọc có hẹp đúng cách** — đổi thư viện bên dưới, có sửa được chỉ trong file bọc này không? Không thì đó chỉ là một lớp gián tiếp, không phải một lớp bọc.

### 2.4 Khi thư viện thiếu thứ cần

- **Vì sao bước 4 phải cân nhắc kỹ** — Override CSS neo vào tên lớp CSS nội bộ của thư viện, tức neo vào thứ thư viện có quyền đổi trong bản vá nhỏ.

- **Bước 3 và `shared/ui/`** — Component tự viết không phụ thuộc thư viện thì không thuộc `shared/ui/` nữa; nó ở `shared/components/`.

## 3. Design token — màu khai một chỗ

—

### 3.1 Hai luật

Dạng pha alpha được phép, và ba dòng sai điển hình:

```scss
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

- **Vì sao không miễn trừ theo giá trị** — Một màu đen nửa trong suốt trông vô hại, nhưng nó vẫn là một **quyết định thiết kế**, và quyết định thiết kế thuộc về [`../Design/`](../../../Design/). Miễn trừ theo giá trị là mở lại đúng cánh cửa mà luật này sinh ra để đóng — vì danh sách "giá trị vô hại" không có điểm dừng tự nhiên.

Làm hai bước như vậy để một dòng vừa có dạng hợp lệ vừa có literal trần **vẫn bị bắt**. Lọc bằng một lệnh loại dòng sẽ tha nhầm cả dòng.

### 3.3 Nơi duy nhất được giữ literal

- **Vì sao khối token mẫu cố ý rỗng** — Một khối minh hoạ có tên token thật là cách hệ token thứ hai ra đời: người đọc file quy ước FE lấy luôn tên trong ví dụ đi dùng, và không bao giờ mở [`../Design/DESIGN.md`](../../../Design/DESIGN.md) ra xem tên thật là gì.

- **Vì sao chỉ một file được giữ literal** — literal ở `_tokens.scss` là định nghĩa, không phải bản sao; loại trừ cả thư mục là mở cửa cho bản sao thứ hai.

### 3.4 Chiều cập nhật — `Design/` là nguồn, code đuổi theo

Bỏ bước 1 nghĩa là bảng màu thật nằm trong code còn tài liệu thiết kế nói dối — và lần sau có người mở tài liệu ra làm chuẩn, họ sẽ tạo màu thứ hai gần giống.

> **Bảng liệt kê chỗ hardcode ở dự án tiền nhiệm** — Ở dự án tiền nhiệm, một bảng liệt kê các chỗ hardcode màu sai quá nửa số dòng đồng thời bỏ sót vài dòng đúng — đúng khuôn mà [`../../.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §6 cấm.

### 3.5 Theme trước paint và font

- **Vì sao script nội tuyến, không phải `ThemeService`** — Angular chỉ chạy sau khi bundle tải và khởi động xong; khung hình đầu tiên đã vẽ trước đó bằng token sáng ở `:root`. Người chọn tối thấy một nháy sáng mỗi lần tải trang, và nháy đó xảy ra đúng ở lúc họ nhìn kỹ nhất.

- **Vì sao hash chứ không `'unsafe-inline'`** — `'unsafe-inline'` mở cửa cho **mọi** script nội tuyến, kể cả thứ một lỗ XSS chèn vào — tức vô hiệu hoá đúng phần CSP tồn tại để làm. Hash chỉ cho phép đúng một nội dung. Cái giá: đổi một ký tự trong script là phải tính lại hash và cập nhật header — nên script này cố ý ngắn và gần như không bao giờ sửa.

- **Vì sao không nonce** — Nonce đòi máy chủ sinh giá trị mới mỗi lần phục vụ `index.html`; app là tệp tĩnh sau proxy ([`17-phuc-vu-va-trien-khai.md`](../17-phuc-vu-va-trien-khai.md)), không có bước render phía máy chủ để chèn nonce.

- **Vì sao lưu ba giá trị trong khi thuộc tính chỉ có hai** — `'system'` phải phân biệt được với *chưa từng chọn*: chưa chọn thì mặc định sáng ([`../../../Design/DESIGN.md`](../../../Design/DESIGN.md) §8), còn chọn *theo hệ điều hành* thì bỏ thuộc tính. Không có giá trị thứ ba thì hai ca này trùng nhau và mặc định sáng bị mất.

- **Vì sao `try/catch` trong một script tám dòng** — Ở chế độ riêng tư hoặc khi người dùng chặn lưu trữ, `localStorage.getItem` ném lỗi; script chạy trước Angular nên không có bộ bắt lỗi nào phía trên, và một lỗi ở đây là trang trắng vì một tuỳ chọn hiển thị.

- **Vì sao font tự phục vụ** — Lý do ở [`../../../Design/DESIGN.md`](../../../Design/DESIGN.md) §3.1; hệ quả kỹ thuật là CSP không cần nguồn font ngoài, và mạng nội bộ chặn ra ngoài vẫn hiện đúng dấu tiếng Việt.

## 4. Tổ chức file style

—

### 4.1 Bốn nhóm, bốn file

**Vì sao tách bốn file thay vì một `styles.scss` lớn:** ở dự án tiền nhiệm, một file style vượt một nghìn dòng. Không ai đọc hết một file như vậy trước khi thêm dòng thứ một nghìn lẻ một, nên nó tích tụ ba thứ cùng lúc — quy tắc trùng nhau, override chồng override, và literal màu ẩn giữa hàng trăm dòng không liên quan. Bốn file theo bốn chủ đề khiến câu hỏi *"dòng này thuộc đâu"* có câu trả lời, và khiến việc một file phình lên trở thành tín hiệu đọc được.

### 4.2 Style của component

- **Vì sao không `::ng-deep`** — Nó là API đã bị đánh dấu ngừng dùng và nó rò style ra ngoài phạm vi component — tức phá đúng thứ đóng gói style sinh ra để làm.

- **Vì sao cho phép `ViewEncapsulation.None` có điều kiện, không cấm hẳn** — Ca có thật là `AuthCard`: khe giữa các trường của form xác thực thuộc về khung ([`../../../Design/Components/AuthCard.md`](../../../Design/Components/AuthCard.md) §Kích thước), nhưng `<form>` do trang chiếu vào qua `<ng-content>`. Cấm `None` thì khe phải thành một lớp tiện ích toàn cục mà mọi trang xác thực tự gắn vào `<form>` — khe rời khỏi component sở hữu nó, spec component phải đổi API, và một trang quên gắn lớp thì mất khe. Viết khe ở từng trang thì lặp đúng thứ spec nói là của khung. `None` giữ khe ở trong component, đổi lại style thành toàn cục; điều kiện *mọi selector cấp cao nhất mở đầu bằng selector phần tử của component* kéo phạm vi về đúng cây DOM của component, nên rò ra ngoài chỉ còn khi viết sai — và viết sai thì lệnh dò thấy. Điều kiện bám selector phần tử chứ không bám lớp gốc BEM vì selector phần tử đọc được từ `selector:` của chính component, không phụ thuộc người viết đặt tên lớp có kỷ luật hay không.

- **Vì sao chưa có cổng** — Tới 2026-10-06 mới có một vi phạm thật: `AuthPage` tắt đóng gói và viết style nội tuyến trong khi style của nó chỉ chạm phần tử chủ. Cổng dựng khi vi phạm thật lặp lại ([ADR-0114](../../../adr/0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)); tới lúc đó lệnh dò ở file luật là thứ review chạy.

### 4.3 Lớp tiện ích

- **Vì sao không dựng hệ tiện ích song song với hệ token** — Hai hệ thì mỗi màn hình sẽ dùng một hệ.

### 4.4 Override thư viện

Không có hai câu đó, người nâng cấp thư viện sau này không có cách nào biết dòng nào xoá được.

## 5. i18n — template không chứa chữ tiếng Việt

—

### 5.1 Luật F8

—

### 5.2 Cổng dò dấu thanh

Ba quyết định trong thiết kế cổng này:

1. **Dò dấu thanh, không dò "text node ngoài ống dịch".** Dấu thanh không thể là tên biến, tên lớp CSS hay từ khoá Angular, nên gần như không báo nhầm — mà lại không cần phân tích cú pháp template.
2. **Xoá comment HTML trước khi dò**, vì chú thích viết tiếng Việt là chuyện bình thường và không ai đọc chú thích trên màn hình.
3. **Thay comment bằng đúng số xuống dòng nó chiếm, không xoá trắng.** Xoá trắng làm luồng ngắn lại, và số dòng cổng báo ra sẽ lệch so với file thật. Một cổng chỉ sai số dòng thôi cũng đủ làm người sửa mở nhầm chỗ rồi kết luận cổng báo bậy.

- **Giới hạn của cổng F8** — Đây là đánh đổi có chủ đích — mẫu đơn giản, gần như không báo nhầm, đổi lại một khoảng mù hẹp mà review người vẫn thấy.

### 5.3 Đặt khoá dịch

- **Vì sao F23 thoát 0 khi chỉ có một tệp** — v1 có một ngôn ngữ ([`../08-i18n.md`](../08-i18n.md) §9, §12). Cổng đòi `en.json` khi chưa có ai cần nó thì hoặc ai đó tạo một tệp rỗng cho cổng xanh — một tệp luôn thiếu khoá — hoặc cổng bị tắt. Cả hai đều tệ hơn một cổng biết mình không có gì để đối chiếu. Thiếu `vi.json` vẫn đỏ vì đó là tệp Core bắt buộc, không phải chuyện số ngôn ngữ.

- **Vì sao vẫn giữ cổng thay vì bỏ tới khi có ngôn ngữ thứ hai** — Ngày tệp thứ hai xuất hiện, cổng tự bắt đầu đối chiếu mà không ai phải nhớ bật; đó là khác biệt giữa *cổng có sẵn* và *cổng trong ghi chú*.

- **Vì sao F23 quét từng tầng riêng** — khoá thiếu ở tầng dự án mà tầng Core che lấp vẫn là khoá thiếu.

Một bộ khoá mẫu theo quy ước ở [`fe-ui-conventions.md`](../../../quy-uoc/fe-ui-conventions.md) §5.3:

```
chung.luu
chung.huy
chung.xoa
chung.xacNhanXoa
nguoiDung.tieuDe
nguoiDung.cot.hoTen
nguoiDung.thongBao.taoThanhCong
loi.CORE.USER.USERNAME_DUPLICATED
loi.CORE.CLIENT.VALIDATION_REQUIRED
```

### 5.4 Tham số và số nhiều

- **Vì sao tham số theo tên** — Tham số theo thứ tự hỏng im lặng khi câu dịch của ngôn ngữ khác đảo trật tự — mà tiếng Việt và tiếng Anh đảo trật tự thường xuyên.

- **Vì sao vẫn lo số nhiều** — tiếng Việt không biến đổi theo số, nên cám dỗ là bỏ qua hẳn vấn đề. Đừng — bảng tiếng Anh cần, và câu tiếng Việt viết cho một dạng số sẽ đọc kỳ khi ghép.

Khuôn số nhiều — hai khoá con và chọn ở template:

```json
{ "nguoiDung": { "soDong": { "mot": "1 dòng", "nhieu": "{{soLuong}} dòng" } } }
```

```html
{{ (soDong() === 1 ? 'nguoiDung.soDong.mot' : 'nguoiDung.soDong.nhieu') | translate: { soLuong: soDong() } }}
```

📐 Nếu sau này có ngôn ngữ với nhiều hơn hai dạng số, khuôn này phải đổi sang cơ chế số nhiều đầy đủ. Ghi lại đây để lúc đó biết chỗ nào phải sửa, thay vì phát hiện qua một câu hiển thị sai.

### 5.5 Ngày, giờ, số

- **Quên đăng ký locale** — Quên bước đó thì pipe im lặng rơi về locale mặc định và ngày hiện ra theo khuôn của một nước khác.
- **Tên khuôn đi theo bản Angular đang cài, không theo sản phẩm.** `'short'`, `'shortDate'` đọc khuôn từ dữ liệu CLDR đóng gói cùng `@angular/common`. Bản 21 đổi `shortDate` của `vi` từ `dd/MM/y` sang `d/M/yy`, nên mọi template viết `date: 'short'` đổi từ `03:12 10/09/2026` sang `03:12 10/9/26` cùng lúc — build xanh, không test màn nào đỏ. Khuôn của `fr` không đổi ở bản đó; lần nâng sau có thể đổi ngôn ngữ khác.
- **Vì sao khuôn ở seam.** Người dùng chốt 2026-11-06 giữ dạng cũ: năm hai chữ số dễ đọc nhầm trên dữ liệu quản trị. Loại: chấp nhận `d/M/yy`; viết chuỗi khuôn ở template — mỗi chỗ dùng một bản sao, và khuôn không đổi theo ngôn ngữ; vá mảng dữ liệu locale trước khi đăng ký — dựa vào chỉ mục nội bộ của mảng, thứ Angular không công khai, nên nâng bản là vỡ không báo; pipe ngày tự viết — thêm mã Core cho việc `DATE_PIPE_DEFAULT_OPTIONS`, một API công khai, đã làm; `dateTimeFormat` tuỳ chọn — mục thiếu nó thì pipe im lặng về `mediumDate` (`10 thg 9, 2026`), đúng dạng hỏng của gạch đầu dòng trên.
- **Cái giá.** `DATE_PIPE_DEFAULT_OPTIONS` tĩnh như `LOCALE_ID`: đổi ngôn ngữ lúc chạy ([`../08-i18n.md`](../08-i18n.md) §7) phải làm cả hai thành động. Seam cấp một khuôn mặc định, nên hiện chỉ ngày chưa có đường đi. Tài liệu Angular dạy `date: 'short'` — người mới sẽ "sửa lại", và vế này chưa có cổng. Mọi mục ngôn ngữ, kể cả trong spec, phải khai khuôn.
- **Dấu hiệu quyết định sai.** Ca kiểm `shortDate` của `vi` trong `src/FE/src/app/core/config/core-i18n.spec.ts` — chuỗi `'10/9/26',` — đỏ sau một lần nâng Angular, tức CLDR quay về `dd/MM/y`: xét gỡ trường. Màn đầu tiên cần hiện chỉ ngày: phải thiết kế khuôn thứ hai.

## 6. Form — typed reactive form

—

### 6.1 Khuôn

- **Vì sao không template-driven form** — nó không kiểm được kiểu, không đặt được validator theo nhóm, và trạng thái nằm rải trong template.

- **Vì sao `NonNullableFormBuilder`** — Với `FormBuilder`, kiểu của mọi control là `T | null` vì `reset()` có thể đưa nó về null — nên mọi chỗ đọc giá trị đều phải xử lý null mà thực tế không bao giờ null. `NonNullableFormBuilder` cho `reset()` quay về giá trị khởi tạo và kiểu sạch.

- **Vì sao `getRawValue()`** — `value` bỏ qua control đang bị vô hiệu hoá — nên một field chỉ-đọc sẽ biến mất khỏi payload, im lặng, và BE nhận thiếu field.

- **Vì sao tên control phải trùng tên field của request** — Bước chuyển khoá `fieldErrors` về tên control ở `applyFieldErrors` chỉ đổi chữ hoa đầu; nó tìm thấy ô khi và chỉ khi hai tên này trùng nhau.

- **Vì sao control tự dựng phải cài `ControlValueAccessor`** — `[formControl]` chỉ gắn được vào phần tử có một value accessor; không có thì Angular ném `NG01203` ngay lúc render. Cách né bằng cách đặt `[formControl]` lên `<input>` bên trong và truyền control vào qua input là để lộ ruột của component ra template màn, và mọi màn phải lặp lại cùng dây nối. Với CVA, màn viết `app-input` như một `<input>` thường.

- **Vì sao đăng ký CVA bằng `ngControl.valueAccessor = this`, không qua `providers` với `NG_VALUE_ACCESSOR`** — Spec của các control tự dựng suy trạng thái `error` từ `invalid && touched` của control chủ, tức component phải inject `NgControl`. `NgControl` trên cùng phần tử lại tìm accessor qua `NG_VALUE_ACCESSOR`; provide token đó ở chính component là hai bên chờ nhau — Angular ném `NG0200` lúc dựng. Gán accessor trong constructor cắt vòng đó. `{ self: true }` vì thiếu nó `inject` với lên `NgControl` của form cha và control con nhận nhầm chủ; `{ optional: true }` vì `Input` và `Check` còn ca không gắn control ([`../../../Design/Components/Input.md`](../../../Design/Components/Input.md) mục *API dự kiến*). Token này không lấy dữ liệu nên F11 tha ([`../trien-khai/05-gate.md`](../trien-khai/05-gate.md) §8.8).

Khuôn ví dụ đầy đủ nhắc tới ở [`fe-ui-conventions.md`](../../../quy-uoc/fe-ui-conventions.md) §6.1 — **ví dụ minh hoạ**, chưa khớp tên file hay tên class thật nào trong `src/FE` hôm nay (đối chiếu 2026-09-27: không có `form-nguoi-dung.page.ts`, không có class `FormNguoiDungPage`; thực tế màn tạo người dùng dùng khuôn "hộp thoại" — `hop-tao-nguoi-dung.component.ts`, cùng tín hiệu `loiChung`/`lanGuiSai`; form và lời gọi `tao` nằm ở `tao-nguoi-dung.store.ts`, khuôn thật nên đọc). Service, phương thức và tên control của mẫu khớp code thật, đối chiếu 2026-10-06:

```typescript
// platform/quan-tri/nguoi-dung/pages/form/form-nguoi-dung.page.ts
import { ChangeDetectionStrategy, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { ApiFailureError } from '../../../../../core/http/api-result.model';
import { cauLoiKhongPhaiApi } from '../../../../../core/http/dich-loi';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import { FormRowComponent } from '../../../../../shared/components/form-row/form-row.component';
import { InputComponent } from '../../../../../shared/components/input/input.component';
import { NoticeBannerComponent } from '../../../../../shared/components/notice-banner/notice-banner.component';
import { applyFormFailure } from '../../../../../shared/forms/apply-form-failure';
import { fieldErrorText } from '../../../../../shared/forms/field-error-text';
import { focusOSaiKhiGuiSai } from '../../../../../shared/forms/focus-o-sai-dau-tien';
import { NguoiDungService } from '../../services/nguoi-dung.service';

type TenTruong = 'userName' | 'fullName' | 'email' | 'tempPassword';

@Component({
  selector: 'app-form-nguoi-dung',
  standalone: true,
  imports: [
    ReactiveFormsModule, TranslatePipe, FormRowComponent, InputComponent, ButtonComponent,
    NoticeBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './form-nguoi-dung.page.html',
})
export class FormNguoiDungPage {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly service = inject(NguoiDungService);
  private readonly translate = inject(TranslateService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly formEl = viewChild<ElementRef<HTMLFormElement>>('formEl');

  protected readonly dangGui = signal(false);
  protected readonly daGui = signal(false);
  /** Câu cho khu lỗi chung của form (`NoticeBanner`); `null` = không có. Xem quy-uoc/fe-ui-conventions.md §6.2. */
  protected readonly loiChung = signal<string | null>(null);
  /** Tăng mỗi lần bấm gửi mà form không hợp lệ — `focusOSaiKhiGuiSai` đọc nó. Xem quy-uoc/fe-ui-conventions.md §6.2. */
  protected readonly lanGuiSai = signal(0);

  /** Tên control = tên field của request ở contracts/users.md §5. */
  protected readonly form = this.fb.group({
    userName: this.fb.control('', [Validators.required]),
    fullName: this.fb.control('', [Validators.required, Validators.maxLength(200)]),
    email: this.fb.control('', [Validators.required, Validators.email]),
    tempPassword: this.fb.control('', [Validators.required]),
    roleIds: this.fb.control<readonly string[]>([]),
  });

  constructor() {
    focusOSaiKhiGuiSai(this.lanGuiSai, () => this.formEl()?.nativeElement);
  }

  protected gui(): void {
    this.daGui.set(true);
    this.loiChung.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.lanGuiSai.update((n) => n + 1);
      return;
    }
    this.dangGui.set(true);
    this.service
      .tao(this.form.getRawValue())
      .pipe(finalize(() => this.dangGui.set(false)))
      .subscribe({
        next: () => void this.router.navigate(['..'], { relativeTo: this.route }),
        error: (err: unknown) => this.xuLyLoi(err),
      });
  }

  private xuLyLoi(err: unknown): void {
    if (!(err instanceof ApiFailureError)) {
      this.loiChung.set(cauLoiKhongPhaiApi(this.translate)); // lỗi không đến từ HTTP: không có envelope — F45
      return;
    }
    this.loiChung.set(applyFormFailure(this.form, err, this.translate));
  }

  /** Uỷ quyền: dịch lỗi và thời điểm hiện lỗi nằm ở shared/forms/, không ở page. */
  protected loiCua(ten: TenTruong): string | null {
    return fieldErrorText(this.form.controls[ten], this.daGui(), this.translate);
  }
}
```

### 6.2 Gắn lỗi từ `fieldErrors` của BE

> **Vì sao không viết lại `applyFieldErrors` trong page** — Không viết lại việc đó thành phương thức private trong page — hai bản của cùng một hàm thì bản không ai sửa sẽ lệch ở đúng chỗ hỏng im lặng nhất.

- **Vì sao không im lặng bỏ qua field không khớp ô** — Người dùng sẽ thấy form không báo gì và nút không có tác dụng.

- **Vì sao giá trị `fieldErrors` phải là mã, không phải câu** — Gắn thẳng object vào control cho ra `[object Object]` trên màn hình và vứt mất mã lỗi.

- **Vì sao lỗi server bị xoá khi người dùng sửa ô** — Nếu không, lỗi cũ đứng lì và người dùng sửa đúng rồi vẫn thấy báo sai.

- **Vì sao câu banner là câu của mã con, kể cả khi mã gốc không phải `CORE.VALIDATION.FAILED`** — Phương án "mã gốc không bao thì dùng câu của chính nó" làm mã con chưa hiện **biến mất**: `ADMIN_CREATE_FAILED` kèm khoá lệch cho ra *"không tạo được tài khoản quản trị"*, còn lý do cụ thể (chính sách mật khẩu) không hiện ở đâu — trái [`../09-forms-validation.md`](../09-forms-validation.md) §4.3, *khoá không khớp phải gộp vào thông báo chung*. Trong một hộp, việc đang làm đã rõ; thứ người dùng thiếu là **vì sao**. Hệ quả phụ: không còn danh sách "mã bao" nào phải giữ.
- **Vì sao nhận nguyên `ApiFailureError`, không `err.body`** — Hàm cần `status` để nhận lớp lỗi xuyên suốt; `body` không mang nó. Truyền `err.body` là lỗi biên dịch, và lối thoát dễ nhất — bọc lại `new ApiFailureError(err.body)` — cho `status = 0`: `laLoiXuyenSuot` không nhận ra 5xx, khu lỗi hiện nó thêm một lần cạnh toast.
- **Vì sao `\n` chứ không mảng** — Giữ chữ ký `string | null` cho mọi nơi gọi; ngắt dòng là việc của chỗ hiện (`NoticeBanner`, khu lỗi `AuthCard`), một khai báo style ở component thay vì một vòng `@for` ở từng page.
- **Vì sao screen spec không khai câu** — Hai nơi cùng quyết câu thì lệch, và code đi theo nơi người thi công mở ra trước. Luật chọn câu là một, cho mọi form; màn chỉ khác nhau ở chỗ đặt banner.
- **Vì sao `CORE.VALIDATION.FAILED` là ca đắt nhất của luật *mã gốc không nói thay mã con*** — Câu của nó (*"kiểm tra lại các trường được đánh dấu"*) sai khi không ô nào được đánh dấu: người dùng đi tìm một dấu không có trên màn.
- **Vì sao màn đăng nhập tự chọn câu ở hai ca** — `CORE.AUTH.INVALID_CREDENTIALS` ở chế độ vận hành: câu của mã chỉ nói tới hai ô của chế độ thường; câu riêng đổi theo chế độ người dùng chọn, không theo nguyên nhân trượt, nên không mở đường dò. `CORE.VALIDATION.FAILED` mang khoá `TenantCode` ở chế độ thường: chế độ này không có ô mã đơn vị, nên câu của mã con nói về một ô không có trên màn; nguyên nhân là cấu hình bản cài, người dùng không sửa được.
- **Vì sao focus đặt sau lần render kế tiếp** — `aria-invalid="true"` chỉ có trên DOM sau khi lỗi được vẽ; tìm ô sai ngay trong nhánh xử lý bấm gửi thì không thấy ô nào.
- **Vì sao mã con chưa có khoá ra câu dự phòng chung** — Mã gốc thiếu khoá còn lùi được về câu BE gửi kèm; mã con chỉ mang `code` và `messageParams`, không câu nào để lùi. Không chặn thì ngx-translate trả lại nguyên khoá và người dùng đọc `loi.CORE.AUTH.PASSWORD_REQUIRES_LOWER` dưới ô. Thêm khoá thiếu chỉ chữa mã đã biết; mã BE thêm sau vẫn lộ ra tới khi có cổng F16, mà cổng chỉ dựng khi vi phạm lặp lại ([ADR-0114](../../../adr/0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)). Cái giá: câu dự phòng không nói lý do — người dùng biết ô sai nhưng không biết sai thế nào. Vì vậy câu dự phòng không thay việc thêm khoá.

### 6.3 Validate phía client hay phía server

- **Một câu** — client validate để *người dùng đỡ chờ*; server validate để *dữ liệu đúng*. Không bao giờ chỉ có vế đầu.

### 6.4 Hiển thị lỗi

- **Vì sao lỗi là `null` cho tới khi chạm ô** — Hiện lỗi ngay khi người dùng chưa gõ gì là cách nhanh nhất làm một form trông như đã hỏng.

- **Vì sao viền lỗi suy từ control, không nhận qua input** — Trang truyền `invalid` riêng là tạo nguồn thứ hai cho một trạng thái: ô viền đỏ mà trình đọc màn hình không đọc lỗi nào, hoặc đọc một lỗi không hiện trên màn. Control đã cài `ControlValueAccessor` nên là nguồn chung: `fieldErrorText` đọc nó để ra câu, component đọc nó để ra viền và `aria-invalid`.

- **Id lệch khuôn** — Gõ một id lệch khuôn thì `aria-describedby` trỏ vào phần tử không tồn tại: trình đọc màn hình im lặng, không lỗi biên dịch, không lỗi lúc chạy. `id` của `app-input` lệch `controlId` của `app-form-row` thì `<label for>` không trỏ vào ô nào.

- **Vì sao `describedBy` vẫn do trang truyền** — nó nối ô với dòng lỗi qua `aria-describedby`, và `aria-describedby` nhận danh sách id nên ô có cả gợi ý lẫn lỗi truyền hai id. Component không tự suy được: nó không biết `FormRow` có dòng gợi ý hay không. `app-input` không tự vẽ nhãn — nhãn là việc của `app-form-row`.

- **Vì sao `error` là `null` cho tới khi chạm ô hoặc đã gửi** — Luật hiển thị ở mục Trạng thái của [`../Design/Components/FormRow.md`](../../../Design/Components/FormRow.md); thi công ở `fieldErrorText`.

- **Vì sao nút gửi dùng `type="submit"` và `loading`** — Form tự phát `ngSubmit`, còn `loading` khoá nút trong lúc gửi nên bấm đúp không tạo hai bản ghi.

- **Vì sao ba dấu hiệu bật cùng lúc** — cùng đọc một control: `gui()` gọi `markAllAsTouched()` và `applyFieldErrors` gọi `markAsTouched()`, nên control `touched` đúng lúc `fieldErrorText` bắt đầu trả câu ([`fe-ui-conventions.md`](../../../quy-uoc/fe-ui-conventions.md) §6.5).

### 6.5 `fieldErrorText` — lỗi hiện lúc nào, câu nào

Lý do của bảy dòng luật, theo số dòng ở file luật:

1. `FormRow` coi `null` là không lỗi.
2. Người dùng đang gõ lần đầu, chưa rời ô.
3. Người dùng không chọn vào ô đó — màn đặt họ vào đó. Rời đi khi chưa gõ là chưa bắt đầu điền; chuyển cửa sổ cũng làm ô mất focus nên hai ca không tách được ([`../../../Design/Components/FormRow.md`](../../../Design/Components/FormRow.md), dòng *ô màn tự đặt focus*).
4. Ba nhánh còn lại của bảng *Lỗi hiện khi nào*: rời ô, bấm gửi, đang sửa một ô đã báo lỗi.
5. `error` của `FormRow` là một chuỗi.
6. Hiện hết mọi lỗi làm form nhảy. Lỗi client tính lại theo giá trị, nên sửa xong lỗi đầu là lỗi kế hiện ngay; lỗi server bị gỡ cả nhóm khi giá trị đổi ([`../09-forms-validation.md`](../09-forms-validation.md) §4.2), nên mã server kế hiện ra ở lần gửi lại nếu giá trị mới vẫn vi phạm nó.
7. Lỗi server là câu trả lời về **chính giá trị đang nằm trong ô**. Giá trị đổi thì khoá `server` bị gỡ (cùng §4.2) và lỗi client, nếu còn, hiện ra.

**Ô tự đặt focus lúc mở — vì sao chặn ở `touched`, không ở `fieldErrorText`.** Ba thứ cùng đọc `touched` của control: câu lỗi (`fieldErrorText`), viền lỗi cùng `aria-invalid` của `app-input` và `app-check` (`invalid && touched` — `input.component.ts`, `check.component.ts`, chuỗi `this.ngControl.touched === true`), và `focusOSaiDauTien`, vốn tìm ô sai qua `aria-invalid="true"`. Chỉ cho `fieldErrorText` trả `null` *kể cả khi đã `touched`* thì ô viền đỏ, trình đọc màn hình đọc "không hợp lệ", mà không có dòng lỗi nào — ba kênh báo lỗi lệch nhau. Giữ control chưa `touched` thì cả ba cùng im, cùng bật, không chỗ nào phải biết luật này.

**Vì sao cờ trên phần tử, không tham số hay chỉ thị.** Ô nào nhận focus lúc mở được quyết **lúc chạy**: `Drawer` chọn ô nhập đầu trong DOM, `Dialog` cũng vậy, màn đăng nhập chọn ô trống đầu tiên theo chế độ. Một tham số của `fieldErrorText` hay một chỉ thị gắn trên ô trong template là câu trả lời thứ hai, khai tĩnh — đổi thứ tự ô, hay ô đầu đã có giá trị, là hai câu lệch nhau; quên khai thì im lặng về hành vi cũ. Cờ do chính lời đặt focus gắn thì không lệch được với chỗ focus thật. Thuộc tính trên phần tử, không một tập `WeakSet` trong module: lưới giao diện đọc được, và nó mất cùng phần tử chứ không sống qua các ca kiểm.

**Bẫy:**

- Gọi thẳng `focus()` lúc mở — ô không mang cờ, rời đi là báo "bắt buộc" như cũ. Không cổng nào bắt; review canh, và ca kiểm của chỗ đặt focus khẳng định cờ có mặt.
- Để `focusOnShow` của PrimeNG đặt focus rồi gắn cờ sau: thư viện tự chọn phần tử và đặt focus trong một lần hẹn giờ riêng, dài bằng thời lượng chuyển động — đọc `primeng-dialog.mjs` của gói `primeng` bản ghim ở `src/FE/package.json` (đọc lại 2026-11-06), chuỗi `setTimeout(() => _focusableElements[0].focus()`; tệp không nằm trong git. Lớp bọc không biết lúc nào phần tử nhận focus, và phần tử thư viện chọn là câu trả lời thứ hai cho câu "ô nào".

- **Khoá và tham số dịch của lỗi validator client** — Khoá lỗi `required` của Angular thành `loi.CORE.CLIENT.VALIDATION_REQUIRED`; tham số dịch là object lỗi mà validator gắn vào control — `maxlength` mang `requiredLength`. Thứ tự mảng lỗi server giữ đúng thứ tự BE trả ([`../wiki-core/fe/09-forms-validation.md`](../../../wiki-core/fe/09-forms-validation.md) §4.1).

## 7. Responsive

- **Vì sao không khai điểm ngắt thành CSS custom property** — `var()` không đọc được trong điều kiện của `@media`.
- **Vì sao trừ `0.02px`, không trừ `1px`** — bề rộng khung nhìn có thể lẻ: phóng to trình duyệt, màn tỉ lệ 125 % hay 150 % cho ra bề rộng như "điểm ngắt trừ 0,5px". Ở dải đó, truy vấn trừ `1px` không áp mà `tu-man-hinh` cũng chưa áp — không kiểu nào chạy. Trừ `0.02px` thu dải hở xuống mức không bề rộng thật nào rơi vào, cùng cách Bootstrap và Angular CDK khai ngưỡng "dưới". Viết số nguyên là trừ `1px` trá hình.
- **Vì sao chưa dùng cú pháp dải `(width < …)`** — nó bỏ hẳn được phép trừ. Lý do loại: Safari và iOS chỉ hiểu nó từ 16.4 (Chrome, Edge từ 104, Firefox từ 63), và trình duyệt không hiểu bỏ cả truy vấn — kiểu "dưới điểm ngắt" mất im lặng. Từ 2026-11-06 không trình duyệt nào trong `src/FE/.browserslistrc` còn dưới các mốc đó (đọc: `grep -vE '^[[:space:]]*(#|$)' src/FE/.browserslistrc`), nên lý do loại không còn đúng. Luật ở §7 giữ nguyên tới khi có người quyết đổi; đổi là sửa luật F58 cùng mixin ở `_spacing.scss` và hằng ở `diem-ngat.ts`.
- **Vì sao TypeScript nhận chuỗi truy vấn dựng sẵn, không nhận số px** — ba nơi dùng hôm nay đều chỉ cần chuỗi cho `matchMedia`. Xuất số thì mỗi nơi tự dựng truy vấn và tự chọn phép trừ: đúng cách ba tệp đã trôi thành ba kiểu. Chuỗi dựng sẵn để phép trừ nằm ở một tệp, nơi cổng F58 so được.
- **Vì sao tệp phản chiếu nằm ở `core/theme/`** — cạnh `prime-z-index.ts`, tệp phản chiếu token cho TypeScript đã có; `core/` là tầng đáy nên `shared/` và `platform/` cùng import được. Cái giá: giá trị điểm ngắt vẫn sống ở hai nơi — cổng giữ chúng khớp, không xoá được bản thứ hai.
- **Bẫy:** đổi `$bp-xs` ở `_spacing.scss` mà không đổi hằng TypeScript thì CSS và `matchMedia` bất đồng ở dải giữa hai giá trị. Ví dụ: CSS đã ẩn nút theme của `Topbar` nhưng `matchMedia` chưa báo "dưới điểm ngắt", nên menu tài khoản chưa có mục "Giao diện" — người dùng mất mọi đường đổi theme. Đây là ca F58 sinh ra để chặn.

Ví dụ dùng mixin trong component:

```scss
@use 'spacing' as s;

.luoi {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--sp-6);

  @include s.tu-man-hinh(s.$bp-md) {
    grid-template-columns: repeat(2, 1fr);
  }

  @include s.tu-man-hinh(s.$bp-lg) {
    grid-template-columns: repeat(3, 1fr);
  }
}
```

## 8. Accessibility — mức tối thiểu bắt buộc

—

## 9. Kiểu dữ liệu công khai của thư viện UI

Component Core nhận cấu hình qua những **kiểu có tên**, và cho tới nay chúng chỉ được mô tả bằng một câu văn xuôi trong cột ghi chú của bảng API. Một câu văn xuôi không phải hợp đồng: dự án thứ nhất sẽ khai nó một kiểu, dự án thứ hai khai kiểu khác, và lúc kéo bản vá Core theo [`../adr/0016-phan-phoi-core-bang-clone.md`](../../../adr/0016-phan-phoi-core-bang-clone.md) thì hai bên không hợp nhất được. Đó là **fork** — chỉ khác là nó xảy ra ở tầng kiểu chứ không ở tầng file.

### Họ kiểu CỘT — mỗi component bảng tự khai kiểu cột

- **Vì sao không có kiểu gốc chung** — `Table` và `DataTable` khác nhau đúng ở trường quan trọng nhất là ô. Cột `Table` bắt buộc `cell`, và context của ô mang cả cột. Cột `DataTable` có đúng một trong `cell` hoặc `value`, vì cột thêm qua seam `CORE_SCREEN_EXT` không dựng được `TemplateRef` ([`../../../adr/0057-seam-fe-chi-mang-gia-tri-dung-duoc-o-composition-root.md`](../../../adr/0057-seam-fe-chi-mang-gia-tri-dung-duoc-o-composition-root.md)). Một gốc chung chỉ chung được bốn trường `key`, `header`, `width`, `align`, và mỗi bên phải đè lại `cell` theo một cách khác nhau. Hai component lại nằm ở hai thư mục: `Table` tự dựng ở `shared/components/`, `DataTable` bọc PrimeNG ở `shared/ui/`. Gốc chung không được nằm ở `shared/components/`, vì luật F24 cấm `shared/ui/` import tầng đó. `Table` vì thế phải phụ thuộc một tệp ngoài tầng của nó chỉ để lấy bốn trường. Đổi API của `Table` cho khớp một gốc còn chạm cả ma trận phân quyền, nơi đang dùng `Table`.

- **Cái giá** — Rủi ro cũ vẫn còn đó: trường cùng nghĩa có thể trôi khác tên giữa hai kiểu, và bản vá Core khi ấy không hợp nhất được. Hôm nay chỉ một câu luật ở §9 canh nó (*trường cùng nghĩa mang cùng tên và cùng kiểu*), và người review kiểm câu đó. Dấu hiệu nên dựng lại một gốc chung: kiểu cột thứ ba ra đời, chẳng hạn của `EditableGrid`, hoặc hai kiểu cột bắt đầu có trường cùng nghĩa mà khác tên.

- **`values` của `SummaryRow` nhận số thì sao** — Nhận số rồi tự định dạng là đường ngắn nhất để một bảng hiện `2,152` ở chỗ lẽ ra phải là `2.152`. Định dạng tiền tệ, phân cách nghìn và số chữ số thập phân là quyết định của nghiệp vụ.

- **Vì sao `DataTableSwitches` là một object, không phải sáu input rời** — Sáu cờ rời cho phép biểu diễn tổ hợp vô nghĩa, và mỗi nơi gọi lại xử lý tổ hợp đó một kiểu.

### Bảy kiểu danh sách lựa chọn

- **Vì sao không gộp thành một kiểu chung** — Mỗi cái mang một trường riêng mà các cái khác không có nghĩa để dùng.

- **`DateRangePreset`** — Lối tắt mang nghĩa nghiệp vụ — "kỳ đang mở", "kỳ lương" — do trang truyền vào; `from`/`to` là ngày đã tính sẵn vì tính kỳ là việc của nghiệp vụ, không phải của component.

### Ba kiểu của khung ứng dụng

- **Vì sao `StepItem`** — `Step` quá chung và sẽ trùng với từ vựng của nghiệp vụ đầu tiên có quy trình nhiều bước.

- **Vì sao `points` cho phép `null`** — Trộn hai thứ làm đường biểu đồ tụt xuống đáy ở những tháng chưa phát sinh — một cú sụt không có thật, đúng thứ [`../Design/Components/Chart.md`](../../../Design/Components/Chart.md) cấm ở Do / Don't.

- **Vì sao tiền tố `Ui`** — Trùng tên với một thứ khác nghĩa là mỗi lần import phải đọc kỹ dòng `import` mới biết đang nói về cái gì — và có lúc sẽ đọc nhầm.

- **Vì sao `UploadItem.progress` nhận `null`** — Tải lên qua một proxy không trả về tiến độ là chuyện thật; lúc đó `ProgressBar` phải chuyển sang `indeterminate` chứ không được bịa một con số.

- **Vì sao không lọt kiểu PrimeNG** — Lọt ra là mất luôn cái lợi duy nhất của việc bọc: bọc nghĩa là giấu hẳn.

   Đây là luật quyết định `Sidebar`, `Menu`, `Tabs` và mọi component nhận danh sách còn **dumb** hay không. Nhận khoá dịch nghĩa là component phải inject một service dịch để hiển thị được — và lúc đó nó vi phạm [`../Design/COMPONENTS.md`](../../../Design/COMPONENTS.md) §5 cùng luật F11. Trang cha phân giải khoá rồi truyền chuỗi xuống; đó là việc của tầng smart.

   Cái giá phải trả, nói thẳng: **đổi ngôn ngữ lúc chạy buộc trang cha dựng lại mảng**, không phải component tự cập nhật. Với `ngx-translate` thì đó là một `computed()` đọc tín hiệu ngôn ngữ hiện hành — rẻ, nhưng phải nhớ làm, và quên thì menu đứng nguyên tiếng cũ sau khi đổi ngôn ngữ.

## 10. Đối chiếu luật

—
