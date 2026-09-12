---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 05. Thư viện component dùng chung

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`; đây là bộ component `shared/` phải dựng.
>
> Đặc tả giao diện từng component (bố cục, trạng thái, câu chữ) thuộc [`../../Design/COMPONENTS.md`](../../Design/COMPONENTS.md). File này nói *cần component nào, vì sao, và ranh giới của chúng*.

---

## 1. Bộ component tối thiểu của một Core FE

Danh sách này sinh ra từ nhu cầu thật của hệ quản trị, không phải từ mong muốn có một thư viện đầy đủ. Mỗi dòng trả lời được câu *"màn nào cần nó, và nếu không có thì màn đó viết lại cái gì"*.

| Component | Vai | Không có thì sao |
| --- | --- | --- |
| **Data grid** (server-side) | Bảng dữ liệu có phân trang, sắp xếp, lọc ở server | Mỗi màn danh sách tự nối phân trang với API theo cách riêng — và sai theo cách riêng |
| **Form field** | Nhãn, ô nhập, chú thích, thông báo lỗi, dấu bắt buộc | Mỗi form đặt lỗi ở một chỗ khác nhau; người dùng phải học lại ở mỗi màn |
| **Modal / dialog** | Khung hộp thoại, quản lý focus, đóng bằng phím Esc | Focus lọt ra sau nền mờ — lỗi tiếp cận nghiêm trọng và không ai thấy khi dùng chuột |
| **Confirm** | Hỏi xác nhận trước thao tác không hoàn tác được | Mỗi màn tự dựng một dialog xác nhận, câu chữ mỗi nơi một kiểu |
| **Toast** | Thông báo ngắn cho thành công và lỗi | Không có nơi để interceptor lỗi hiển thị thông báo — cả §4 của [`02-http-envelope.md`](02-http-envelope.md) sụp theo |
| **Page header** | Tiêu đề màn, breadcrumb, vùng nút hành động | Bố cục đầu trang lệch nhau giữa các màn |
| **Empty state** | Màn hình khi không có dữ liệu | Người dùng thấy một bảng trắng và không biết là lỗi hay là chưa có dữ liệu |
| **Skeleton / loading** | Trạng thái đang tải | Giao diện nhảy khi dữ liệu về; xem [`13-performance.md`](13-performance.md) §5 |
| **Badge / tag** | Hiển thị trạng thái | Mỗi màn tự tô màu trạng thái, và màu đó là hex trần |
| **Upload** | Chọn tệp, hiển thị tiến trình, báo lỗi kích thước và định dạng | Xem §7 |

**Một thứ cố ý không có trong danh sách:** trình soạn thảo văn bản đa dạng thức. Nó nặng, và **chưa có màn Core nào cần**. Thêm khi có màn thật cần, và cân nhắc đặt ở module thay vì Core nếu chỉ một module cần.

> 📖 Ba thứ từng nằm cùng nhóm này — biểu đồ, bộ chọn khoảng ngày, bảng chỉnh sửa tại chỗ — **nay thuộc Core**, kèm ba ràng buộc kỹ thuật. Lý do và cái giá: [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md).

---

## 2. Bọc thư viện UI — luật F5

### 2.1 Luật

> **Component nghiệp vụ không được import trực tiếp `primeng/*`.** Mọi lời gọi tới thư viện UI đi qua một lớp bọc trong `shared/ui/`.

### 2.2 Bốn lý do, xếp theo sức nặng

**(1) Nâng cấp thư viện.** Thư viện UI đổi API giữa các major: tên thuộc tính đổi, một component bị gộp, một cách khai theme bị bỏ. Nếu ba mươi màn import thẳng, mỗi thay đổi như thế là ba mươi chỗ phải sửa và ba mươi cơ hội làm hỏng. Nếu có lớp bọc, đó là một file.

**(2) Hình dạng bundle.** Một component nặng của thư viện được import ở đúng một chỗ vẫn kéo cả thư viện đó vào chunk chứa nó. Khi lời gọi rải khắp nơi, không ai biết chunk nào nặng vì cái gì. Lớp bọc cho **một chỗ duy nhất** để quyết định tải sao và tải lúc nào.

Ở dự án tiền nhiệm, một lazy chunk nặng hơn cả bundle khởi động, và nguyên nhân là đúng một component bảng của thư viện được dùng ở một nhánh. Nó đi qua cổng im lặng vì ngân sách chỉ khai cho phần khởi động.

**(3) Áp token và tiếp cận một lần.** Ràng buộc "mọi ô nhập có nhãn liên kết đúng", "mọi nút có trạng thái focus thấy được", "mọi bảng có `aria-label`" — nếu ép ở từng nơi gọi thì sẽ có nơi quên. Ép trong lớp bọc thì nó đúng ở mọi nơi.

**(4) Khả năng thay thế.** Đây là lý do yếu nhất và hay bị nêu đầu tiên. Thực tế gần như không ai đổi thư viện UI. Đừng bọc **vì** lý do này — nhưng lớp bọc dựng cho ba lý do trên thì tự nhiên có thêm tính chất này.

### 2.3 Cái giá — nói thẳng

| Chi phí | Thực tế |
| --- | --- |
| Một tầng gián tiếp nữa | Đọc code phải nhảy thêm một file |
| Bề mặt API phải quyết định | Bọc lại bao nhiêu thuộc tính? Bọc ít thì phải sửa lớp bọc liên tục; bọc hết thì lớp bọc chỉ là bản sao |
| Cám dỗ "chỉ lần này thôi" | Mỗi ngoại lệ là một chỗ lớp bọc không còn bảo vệ |

**Quy tắc cân bằng cho bề mặt API:** lớp bọc phơi ra **thứ sản phẩm cần**, không phơi ra thứ thư viện có. Cần thêm một thuộc tính thì thêm — đó là một sửa đổi nhỏ có chủ đích, không phải dấu hiệu lớp bọc sai.

### 2.4 Allowlist import thư viện UI — định nghĩa gốc

Bảng dưới là **Allowlist import thư viện UI — định nghĩa gốc**, nguồn duy nhất của allowlist luật F5. Cổng đọc đúng bảng này; tài liệu FE khác trỏ về đây và **không** giữ bản allowlist thứ hai ([`../../OWNERSHIP.md`](../../OWNERSHIP.md) §2).

| Đường dẫn | Được import `primeng/*` | Vì sao |
| --- | --- | --- |
| `src/app/shared/ui/**` | ✔ | Đây **là** lớp bọc |
| `src/app/core/theme/**` | ✔ | Preset ánh xạ token vào hệ theming của thư viện |
| `src/app/core/i18n/**` | ✔ | Nối bảng dịch vào cấu hình ngôn ngữ của thư viện — cấu hình, không phải component |
| `src/app/app.config.ts` | ✔ | Đăng ký provider của thư viện, đúng một lần |
| `src/app/shared/components/**` | ✘ | Component dùng chung của mình — dựng trên `shared/ui/` |
| `src/app/shared/forms/**` | ✘ | Hạ tầng form — dựng trên `shared/ui/` |
| `src/app/platform/**` | ✘ | Màn Core |
| `src/app/modules/**` | ✘ | Màn nghiệp vụ |
| `src/app/core/**` (ngoài `theme/` và `i18n/`) | ✘ | Tầng đáy, không được biết tới UI |

Cổng ép luật này quét đúng các dòng ✔ làm allowlist. Thêm một đường dẫn là một quyết định phải nêu lý do trong PR **và** thêm một dòng vào bảng này, không phải một chỉnh sửa cấu hình.

🛑 **Tầng bọc là `shared/ui/`, không phải `shared/components/`.** Hai tài liệu FE từng khai hai tầng bọc khác nhau, và hậu quả không dừng ở chuyện đọc nhầm: một cổng viết theo bảng này sẽ **đỏ ngay trên đoạn mã mẫu** của tài liệu kia. Mỗi đường dẫn trong bảng phải có mặt trong sơ đồ `shared/` ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2 — một allowlist khoá theo đường dẫn không tồn tại là một cổng **luôn xanh vì không gì rơi vào vùng nó canh**.

> ⚠️ **Bẫy:** allowlist theo **đường dẫn**, không theo tên file hay theo pattern rộng. Một allowlist dạng "mọi file có `ui` trong tên" sẽ tha nhầm bất cứ file nào ai đó đặt tên khéo.

---

## 3. Ba tầng component trong `shared/`

```
shared/ui/           ← bọc thư viện UI. Mỏng. Không biết nghiệp vụ, không biết API.
shared/components/   ← component ghép của mình (data grid, page header, empty state).
shared/directives/   ← hành vi tái dùng (autofocus, chặn double-submit, hiển thị theo quyền).
```

Ranh giới giữa `shared/ui/` và `shared/components/`: `shared/ui/` **chỉ** bọc một component của thư viện; `shared/components/` ghép nhiều thứ lại thành một khối có nghĩa với sản phẩm. Trộn hai tầng này thì lớp bọc dần mang logic và mất tính "mỏng" vốn là lý do nó tồn tại.

---

## 4. Directive theo quyền — thay cho `@if` lặp lại

Ẩn/hiện theo permission xuất hiện ở mọi màn quản trị. Viết bằng `@if` ở từng chỗ dẫn tới hai vấn đề: lặp lại, và mỗi chỗ tự chọn cách hỏi permission hơi khác nhau.

```html
<app-button *appHasPermission="'core.user.write'" (clicked)="openCreate()">
  {{ 'user.them' | translate }}
</app-button>
```

Directive này thuộc `shared/directives/`, hỏi `PermissionService` ở `core/auth`, và **không** biết tên permission nào tồn tại — chuỗi do nơi gọi truyền vào.

**Chuỗi truyền vào phải là một khoá có thật trong danh mục quyền** ([`../../database/schema-core.md`](../../database/schema-core.md) §5.2). Một khoá tự chế trông vô hại: directive nhận chuỗi lạ, `PermissionService` không tìm thấy, và vì phân quyền là deny-by-default nên nút **biến mất với mọi người, kể cả tài khoản đủ quyền** — không lỗi, không cảnh báo, và người đọc template không có cách nào biết chuỗi đó sai.

> 🛑 **Đây là tiện nghi giao diện, không phải bảo mật.** Người dùng vẫn gọi được API bằng công cụ khác. Kiểm quyền thật nằm ở BE ([`../../RULES.md`](../../RULES.md) S2), và luôn phải có kể cả khi FE đã ẩn nút. Xem [`14-security.md`](14-security.md) §2.

---

## 5. Component dumb và component smart

| | Dumb (`components/`) | Smart (`pages/`) |
| --- | --- | --- |
| Nhận dữ liệu | `input()` | Tự gọi service / store |
| Báo ra ngoài | `output()` | Điều hướng, gọi API |
| Được inject service dữ liệu | **Không** (luật F11) | Có |
| Được import DTO | **Không** (luật F10) | Không — chỉ `services/` được |
| Test | Dựng với input, đọc DOM | Test luồng, mock service |

### 5.1 Vì sao ép ranh giới này bằng cổng

Một component dumb tự gọi API là một component **không dùng lại được** — nó đã gắn với một endpoint. Nó cũng khó test hơn nhiều: muốn kiểm một trạng thái hiển thị phải dựng cả tầng HTTP giả.

Điều làm quy tắc này khó giữ là nó **không gây lỗi gì cả** khi vi phạm. Component vẫn chạy, màn hình vẫn đúng. Chi phí chỉ hiện ra sáu tháng sau, khi có người muốn dùng lại nó ở màn thứ hai và phát hiện không tách ra được. Vì thế nó cần một cổng, không phải một lời nhắc trong review.

### 5.2 Ngoại lệ được biết trước

Component của layout shell (topbar, sidebar) nằm ở `shared/components/` nhưng **cần** biết người dùng hiện tại và menu. Chúng là ngoại lệ có tên, khai tường minh trong cấu hình cổng kèm lý do và đường dẫn cụ thể — **không** nới rule chung.

Nguyên tắc chung cho mọi ngoại lệ của mọi cổng FE: **loại trừ một đường dẫn cụ thể kèm lý do, đừng làm rule lỏng đi.** Rule lỏng thì lần sau không ai biết vì sao nó lỏng.

---

## 6. Bốn quy ước API cho component dùng chung

| Quy ước | Vì sao |
| --- | --- |
| Dùng `input()` / `output()` dạng hàm, không dùng decorator | Luật F9. `input()` cho signal đọc được trong `computed()` mà không cần `ngOnChanges` |
| `input.required<T>()` cho thứ thật sự bắt buộc | Thiếu là lỗi lúc chạy ngay ở lần dựng đầu, thay vì một `undefined` lan xuống dưới |
| Thuộc tính đặt tên **trung tính về nghiệp vụ** | `variant="danger"` dùng lại được; một danh sách trạng thái nghiệp vụ thì không |
| Không dùng `ViewChild` để với vào bên trong component con | Nó biến chi tiết nội bộ thành API công khai, và không có gì báo khi nội bộ đổi |

Về `@for`: **mọi `@for` phải có `track`** (luật F13). Thiếu `track`, Angular dựng lại toàn bộ danh sách sau mỗi lần đổi — mất trạng thái focus của ô đang gõ và tốn vô ích. Khoá `track` phải là định danh ổn định; dùng chỉ số mảng là gần như vô hiệu hoá nó khi danh sách sắp xếp lại.

---

## 7. Upload — cơ chế thuộc Core, giao diện đặc thù thuộc module

Tải tệp lên thuộc Nhóm B ([`01-core-components.md`](01-core-components.md) §3): làm khi có nghiệp vụ đầu tiên cần. Nhưng khi làm thì phần thuộc Core phải rõ:

| Thuộc Core | Thuộc module |
| --- | --- |
| Component chọn tệp, hiển thị tiến trình, báo lỗi | Ràng buộc số lượng và ngữ cảnh nghiệp vụ |
| Kiểm kích thước và kiểu tệp phía client | Danh sách kiểu tệp cho phép của nghiệp vụ đó |
| Hợp đồng gọi API tải lên | Endpoint cụ thể |

Ba điều phải nói ngay từ khi thiết kế, vì bỏ sót thì sửa rất đắt:

1. **Kiểm ở client là trải nghiệm, không phải bảo vệ.** Kiểm thật ở BE — xem [`../be/14-file-storage.md`](../be/14-file-storage.md).
2. **Phải huỷ được.** Người dùng chọn nhầm một tệp lớn và không có cách dừng là một lỗi trải nghiệm thường bị bỏ quên.
3. **Tên tệp là dữ liệu không tin cậy.** Hiển thị tên tệp do người dùng đặt phải qua escape như mọi chuỗi khác ([`14-security.md`](14-security.md) §1).

---

## 8. Test component dùng chung

Component `shared/` là thứ **đáng test nhất** trên FE: chúng được dùng ở nhiều nơi, nên một lỗi ở đây nhân lên theo số màn.

| Test gì | Không test gì |
| --- | --- |
| Trạng thái theo `input()` (rỗng, đang tải, có lỗi, có dữ liệu) | Rằng thư viện UI bên dưới hoạt động |
| `output()` phát đúng lúc và đúng dữ liệu | Bố cục pixel |
| Hành vi bàn phím của modal, menu, dropdown | Màu sắc — đó là việc của token |
| Nhãn liên kết đúng ô nhập | |

Chi tiết ngưỡng và cách viết: [`06-testing-strategy.md`](06-testing-strategy.md).

---

## 9. Kiểm chứng

- [ ] Tìm `primeng/` trong `src/app` → chỉ còn kết quả trong các đường dẫn ✔ của allowlist §2.4 (luật F5)
- [ ] Không component nào trong `components/` inject service lấy dữ liệu (luật F11)
- [ ] Không file nào trong `components/` hay `pages/` nhắc tới kiểu DTO (luật F10)
- [ ] Không còn cú pháp Angular cũ trong toàn bộ `src/app` (luật F9)
- [ ] Mọi `@for` có `track` với khoá ổn định (luật F13)
- [ ] Mỗi component `shared/` có đủ trạng thái hover, focus thấy được, và vô hiệu hoá
- [ ] Modal giữ focus bên trong, đóng bằng phím Esc, trả focus về phần tử đã mở nó

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Lớp bọc `shared/ui/` + allowlist §2.4 | ✅ sẽ có | Luật F5, pha F1 |
| Bộ component ở §1 trừ upload | ✅ sẽ có | Sinh ra từ nhu cầu thật của F3 |
| Directive hiển thị theo permission | ✅ sẽ có | §4 — tiện nghi giao diện, không phải bảo mật |
| Ranh giới dumb / smart có cổng | ✅ sẽ có | Luật F10, F11 |
| Upload | ❌ chưa | Điều kiện: nghiệp vụ đầu tiên cần đính kèm (§7) |
| Trình soạn thảo văn bản đa dạng thức | ❌ chưa | Điều kiện: có màn thật cần — và kèm làm sạch HTML ở BE |
| Bảng chỉnh sửa tại chỗ | ✅ sẽ có | [`../../Design/Components/EditableGrid.md`](../../Design/Components/EditableGrid.md) — component riêng, không phải chế độ của lưới đọc. Điều kiện cấp bởi [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md) |
| Bộ chọn khoảng ngày | ✅ sẽ có | [`../../Design/Components/Input.md`](../../Design/Components/Input.md) biến thể `daterange`. Cùng ADR |
| Trình soạn thảo văn bản đa dạng thức | ❌ chưa | Điều kiện: có màn thật cần; cân nhắc đặt ở module nếu chỉ một nơi dùng |
| Catalog component chạy được (Storybook hoặc tương đương) | ❌ chưa | Điều kiện: từ hai người trở lên cùng làm UI, hoặc có sản phẩm thứ hai dùng lại bộ này |
| Base class cho component hoặc cho service | ❌ loại, không hoãn `K19` | [`01-core-components.md`](01-core-components.md) §9 — dùng composition; hàm dùng chung cho CRUD ở [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §5 |
| Import thẳng thư viện UI từ màn nghiệp vụ | ❌ loại, không hoãn `K20` | Luật F5 |

Một finding dạng *"FE thiếu X"* chỉ hợp lệ khi X mang trạng thái **✅ sẽ có** mà vắng mặt, hoặc khi điều kiện ở cột ghi chú của một dòng **❌ chưa** đã xảy ra. Dòng **❌ loại, không hoãn** chỉ đổi được bằng một ADR mới, không đổi được bằng một finding.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Đặc tả giao diện từng component | [`../../Design/COMPONENTS.md`](../../Design/COMPONENTS.md) |
| Quy ước viết template và style | [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) |
| Bảng dữ liệu server-side | [`11-grid-and-metadata.md`](11-grid-and-metadata.md) |
| Form và hiển thị lỗi | [`09-forms-validation.md`](09-forms-validation.md) |
| Tiếp cận: focus, bàn phím, ARIA | [`15-accessibility.md`](15-accessibility.md) |
| Cổng ép F5, F9, F10, F11, F13 | [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
