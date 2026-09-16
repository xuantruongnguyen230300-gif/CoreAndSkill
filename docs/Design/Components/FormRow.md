---
kind: luat
scope: core
verified: chua-doi-chieu
---

# FormRow

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 đây là component thuần trình bày — nó không có hành vi khó, chỉ có **luật hiển thị lỗi**, và luật đó là thứ phải tự quyết chứ không nhận từ thư viện.

---

## Mục đích

Gói một ô nhập cùng nhãn, dấu bắt buộc, gợi ý và chỗ hiện lỗi — và là **nơi duy nhất** trong hệ quyết định lỗi hiện **ở đâu**. Lỗi hiện **lúc nào** là luật ở mục Trạng thái, và cơ chế form thi công luật đó trước khi chuỗi lỗi tới component này.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Mọi trường trong một form hoặc `Dialog` có form | 🛑 Ô nhập không có nhãn nhìn thấy (ô tìm trong `Toolbar`) → dùng [`Input.md`](./Input.md) trần với `aria-label` |
| Nhóm `Check` cần một nhãn chung ("Quyền hạn") | 🛑 Trường của màn xác thực → [`AuthField.md`](./AuthField.md), nó có bố cục riêng |
| Trường chỉ đọc trong màn chi tiết | 🛑 Hiện lỗi cấp toàn form (API trả lỗi chung) → [`NoticeBanner.md`](./NoticeBanner.md) |

## Biến thể

| Biến thể | Bố cục | Dùng khi |
| --- | --- | --- |
| `stacked` | Nhãn trên, ô dưới | **Mặc định.** Hợp mọi form, hợp mọi bề rộng |
| `inline` | Nhãn trái, ô phải, nhãn rộng cố định | Màn chi tiết chỉ đọc, nơi nhãn ngắn và đều nhau. 🛑 Không dùng cho form nhập — nhãn dài ngắn khác nhau sẽ răng cưa |
| `group` | Một nhãn chung, nhiều control con (`Check`, `SegmentedControl`) | Nhóm lựa chọn. Dùng `<fieldset>` + `<legend>`, không dùng `<label>` |

**Biến thể `group` bắt buộc dùng `<fieldset>`/`<legend>`, không phải một `<label>` to.** Một `<label>` chỉ gắn được với **một** control; nhóm ba ô đánh dấu dưới một `<label>` khiến trình đọc màn hình đọc nhãn nhóm cho đúng một ô và bỏ hai ô còn lại.

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Khe nhãn → ô | `--sp-5` |
| Khe ô → dòng gợi ý/lỗi | `--sp-3` |
| Khe giữa hai `FormRow` | `--sp-6` |
| Khe giữa hai nhóm trường | `--sp-7` |
| Bề rộng nhãn ở biến thể `inline` | Cố định, chọn một lần cho cả form |

`FormRow` **không có ba cỡ** như control. Nó là vùng chứa; cỡ do `Input` bên trong quyết định.

**Chiều cao của `FormRow` phải ổn định giữa trạng thái có lỗi và không lỗi.** Dòng lỗi xuất hiện làm cả form dịch xuống, đẩy trường tiếp theo ra khỏi chỗ con trỏ đang hướng tới — người dùng đang định bấm vào ô sau thì bấm trúng ô khác. Chừa sẵn chiều cao một dòng dưới mỗi ô, hoặc gộp chỗ cho gợi ý và lỗi vào **cùng một dòng** (xem §Trạng thái).

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nhãn `--fs-sm` `--fw-medium` `--color-text`; ô ở trạng thái `default`; dòng gợi ý `--fs-xs` `--color-text-muted` nếu có | Có |
| `hover` | **Không áp dụng.** `FormRow` không tương tác; hover thuộc về `Input` bên trong | — |
| `focus-visible` | **Không áp dụng ở mức vùng chứa.** Focus thuộc `Input`. `FormRow` **không** được vẽ thêm vòng ngoài — hai vòng lồng nhau làm rối chỗ focus thật sự đang ở đâu | — |
| `active` | **Không áp dụng.** Cùng lý do với `hover` | — |
| `disabled` | Nhãn và gợi ý chuyển `--color-text-disabled`; ô ở trạng thái `disabled`. Vùng chứa **không** đổi nền — tô nền cả hàng làm form trông như bị hỏng | Có |
| `loading` | Nhãn giữ nguyên; ô ở trạng thái `loading`. Không thay cả hàng bằng skeleton — nhãn vẫn phải đọc được để người dùng biết đang chờ cái gì | Có |
| `error` | Ô ở trạng thái `error`; **dòng gợi ý bị thay bằng dòng lỗi** ở cùng vị trí, màu `--color-danger`, có icon `pi-times-circle` cỡ `--icon-sm`; nhãn **không đổi màu** | Có |
| `empty` | **Không áp dụng.** Trường rỗng là chuyện bình thường; rỗng mà bắt buộc thì là `error` sau khi người dùng đã rời trường | — |

### Ba quyết định về hiển thị lỗi

**1. Lỗi thay chỗ gợi ý, không nằm thêm một dòng.** Đây là cách giữ chiều cao ổn định. Cái giá: gợi ý biến mất đúng lúc người dùng cần nó nhất. Chấp nhận được, vì thông báo lỗi tốt phải **chứa** thông tin của gợi ý — "Mật khẩu phải từ 8 ký tự" nói được cả luật lẫn cái sai.

**2. Nhãn không đổi màu khi lỗi.** Tô đỏ cả nhãn làm mắt không biết nhìn đâu, và một form có năm lỗi sẽ đỏ rực. Tín hiệu lỗi nằm ở viền ô, ở icon và ở dòng chữ — ba kênh, đủ theo [`../DESIGN.md`](../DESIGN.md) §2.7.

**3. Lỗi hiện khi nào — luật ba nhánh:**

| Lúc nào | Hiện lỗi? | Vì sao |
| --- | --- | --- |
| Người dùng đang gõ lần đầu, chưa rời trường | 🛑 Không | Báo "email không hợp lệ" khi người ta mới gõ được ba ký tự là quấy rối |
| Người dùng đã rời trường (`blur`) | ✅ Có | Đây là lúc họ coi như đã điền xong trường đó |
| Người dùng bấm submit | ✅ Có, **mọi** trường | Kèm cuộn tới trường lỗi đầu tiên và đặt focus vào đó |
| Trường **đã từng** báo lỗi, người dùng đang sửa | ✅ Kiểm lại theo từng ký tự | Lúc này phản hồi tức thì là hữu ích: nó xác nhận họ đang sửa đúng hướng |

Nhánh thứ tư là nhánh hay bị bỏ quên và là nhánh làm form dễ chịu hẳn.

**`FormRow` không tự áp bảng này.** Nó vẽ đúng chuỗi `error` nhận vào. Bảng được thi công ở hàm `fieldErrorText` của cơ chế form: hàm trả `null` cho tới khi control đã chạm (`touched` — người dùng đã rời ô) hoặc form đã gửi một lần, và từ đó trả câu lỗi đã dịch mỗi khi giá trị đổi — [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §6.5.

**Một ô nhận nhiều mã lỗi cùng lúc** — BE trả hơn một mã cho cùng một trường trong `fieldErrors` — thì dòng lỗi hiện câu của **mã đầu tiên theo thứ tự BE trả**. Người dùng sửa xong lỗi đó, hoặc gửi lại, thì mã kế tiếp còn lại hiện ra. Không gộp nhiều câu vào một dòng: dòng lỗi thay chỗ gợi ý và phải giữ chiều cao ổn định (quyết định 1). `error` vẫn là **một** chuỗi; chọn mã nào là việc của `fieldErrorText` ở cùng mục §6.5.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-danger` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-regular`, `--lh-snug`, `--lh-normal` |
| Khoảng cách | `--sp-3`, `--sp-5`, `--sp-6`, `--sp-7` |
| Kích thước | `--icon-sm` |
| Điểm ngắt | `$bp-md`, `$bp-xs` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Lưới form tối đa hai cột; trường dài (mô tả, địa chỉ) chiếm cả hai cột |
| `$bp-md` … `$bp-lg` | Hai cột giữ nguyên nếu `Dialog` đủ rộng, ngược lại về một cột |
| < `$bp-md` | Một cột; biến thể `inline` **tự chuyển thành `stacked`** — nhãn ngang ở màn hẹp đẩy ô nhập còn vài chục pixel |
| < `$bp-xs` | Khe giữa hai `FormRow` giảm từ `--sp-6` xuống `--sp-5` |

Lưới ở bảng trên là lớp tiện ích `form-grid`, khai một lần ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §4.3 — không phải component. Bảng này là **giá trị** của lớp đó: số cột theo ngưỡng, khe cột và khe hàng bằng khe giữa hai `FormRow` ở §Kích thước; `span` ở §API chiếm cột trong lưới này.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Nhãn | `<label for="<id ô>">`. 🛑 Không dùng `<div>` hay `<span>` làm nhãn — bấm vào nhãn phải đặt focus vào ô, và đó là hành vi chỉ `<label>` có |
| Nhóm | Biến thể `group` dùng `<fieldset>` + `<legend>` |
| Bắt buộc | `required` trên ô **và** dấu hiệu nhìn thấy ở nhãn. Dấu sao phải có `aria-hidden="true"` và nghĩa của nó nói ở đầu form ("Trường có dấu * là bắt buộc") — trình đọc màn hình đọc "sao" là vô nghĩa |
| Gợi ý | `id` là `<controlId>-hint`; ô trỏ tới nó qua `aria-describedby` |
| Lỗi | `id` là `<controlId>-error`; ô trỏ tới qua `aria-describedby` **và** mang `aria-invalid="true"` |
| Thông báo lỗi động | Vùng lỗi mang `role="alert"` để trình đọc màn hình đọc lên ngay khi nó xuất hiện. 🛑 Không đặt `role="alert"` sẵn trên một vùng rỗng luôn tồn tại — nhiều trình đọc sẽ đọc lại mọi thay đổi trong đó, kể cả khi chỉ là xoá chữ |
| Submit lỗi | Đặt focus vào trường lỗi **đầu tiên**, và có một tóm tắt lỗi ở đầu form khi có từ hai lỗi trở lên |
| Chữ | Nhãn, gợi ý, thông báo lỗi qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Tóm tắt lỗi ở đầu form không phải trang trí.** Người dùng trình đọc màn hình bấm submit và không thấy gì thay đổi — họ không "nhìn thấy" năm ô viền đỏ. Một khối tóm tắt có `role="alert"`, liệt kê từng lỗi kèm liên kết nhảy tới trường, là cách duy nhất họ biết chuyện gì vừa xảy ra. Khối đó là [`NoticeBanner.md`](./NoticeBanner.md) vai `danger` — thân qua slot chứa danh sách liên kết, hành vi đặt focus vào trường đã khai ở §Accessibility của file đó; không có component riêng.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `label` | input | `string` | — | Bắt buộc. Không có mặc định |
| `layout` | input | `'stacked' \| 'inline' \| 'group'` | `'stacked'` | |
| `required` | input | `boolean` | `false` | Chỉ vẽ dấu hiệu. Việc đặt `required` lên ô là của cơ chế form |
| `hint` | input | `string \| null` | `null` | |
| `error` | input | `string \| null` | `null` | `null` = không lỗi. Chuỗi rỗng **cũng** coi là không lỗi — tránh ca "có lỗi nhưng không có chữ", vốn tạo ra một dòng đỏ trống |
| `disabled` | input | `boolean` | `false` | Truyền xuống ô |
| `controlId` | input | `string` | tự sinh | Id để `<label for>` gắn vào, và là gốc của hai id dòng phụ: `<controlId>-hint`, `<controlId>-error`. Tự sinh nếu không truyền — nhưng trang truyền `describedBy` cho [`Input.md`](./Input.md) thì phải truyền `controlId` tường minh, vì trang không biết id tự sinh |
| `span` | input | `1 \| 2` | `1` | Số cột chiếm trong lưới `form-grid` của vùng chứa form — §Responsive |

Ô nhập vào qua slot mặc định, không qua input. Như vậy `FormRow` bọc được cả `Input`, cả nhóm `Check`, cả `SegmentedControl` — nếu nhận ô qua một input kiểu chuỗi thì nó chỉ bọc được đúng một loại.

`FormRow` là component **dumb**: nó **không** biết luật kiểm tra dữ liệu. Nó nhận một chuỗi lỗi đã được cơ chế form dịch sẵn và vẽ ra.

## Do / Don't

- ✅ Mọi trường có nhãn đều đi qua `FormRow`.
- ✅ Thông báo lỗi nói **cách sửa**, không chỉ nói sai: "Mật khẩu phải từ 8 ký tự", không phải "Không hợp lệ".
- ✅ Chừa sẵn chiều cao cho dòng lỗi để form không nhảy.
- ✅ Bấm submit thì đặt focus vào trường lỗi đầu tiên.
- ❌ Không đổi màu nhãn khi lỗi.
- ❌ Không báo lỗi khi người dùng đang gõ lần đầu.
- ❌ Không dùng `inline` cho form nhập liệu.
- ❌ Không dùng `<div>` làm nhãn.
- ❌ Không vẽ vòng focus ở mức vùng chứa.

## Cần chốt

Không còn.