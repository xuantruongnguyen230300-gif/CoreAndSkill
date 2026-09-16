---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Icons.md — hệ icon của Core

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo chưa có `src/`. Bảng ánh xạ dưới đây là **quy ước đã quyết**, chưa có màn hình nào dùng.

> Kích thước và màu icon lấy token từ [`DESIGN.md`](./DESIGN.md). File này quyết **chọn icon nào cho việc gì** và **icon phải hành xử ra sao với trình đọc màn hình**.

---

## 1. Bộ icon: PrimeIcons

**Bộ chuẩn duy nhất của Core là PrimeIcons** (đi kèm PrimeNG — phiên bản khoá ở [`../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md)).

Ba lý do chọn, và một cái giá:

| Lý do | Cụ thể |
| --- | --- |
| Đã có sẵn | Là dependency của PrimeNG. Không thêm gói mới, không thêm bước build |
| Nhất quán với component bọc | `DataTable`, `Dialog`, `Toast`, `Pagination`, `FileUpload` **tự chèn icon PrimeIcons của chúng** khi chạy. Chọn bộ khác nghĩa là ứng dụng có hai phong cách icon cạnh nhau, và một nửa không sửa được |
| Đủ dùng cho ứng dụng quản trị | Bao phủ CRUD, điều hướng, trạng thái, tệp, bảng |

**Cái giá, nói thẳng:** PrimeIcons là **icon font**, không phải SVG. Hệ quả thật:

- Font tải chậm → icon nhấp nháy hoặc hiện ô vuông trong chốc lát. Chấp nhận được vì font icon nhỏ và được cache; nhưng đừng dùng icon làm nội dung duy nhất của một nút quan trọng ở lần vẽ đầu.
- Không tô được hai màu trong một icon.
- Trình duyệt căn font theo đường cơ sở chữ, nên icon hay lệch nhẹ so với chữ bên cạnh. Xem §3.

🛑 **Không trộn bộ icon.** Không Font Awesome, không Material Icons, không SVG lẻ chép từ mạng. Một ứng dụng có hai bộ icon là một ứng dụng có hai độ dày nét, hai bán kính bo, hai cách vẽ mũi tên — và người dùng thấy ngay dù không gọi tên được.

**Ngoại lệ duy nhất được phép:** logo và hình minh hoạ thương hiệu. Chúng là tài sản, không phải icon, và sống ở `Assets/` của dự án chứ không ở đây.

---

## 2. Quy tắc chọn icon

Theo thứ tự. Chỉ xuống bước sau khi bước trước không trả lời được.

1. **Việc này đã có trong bảng §5 chưa?** Có thì dùng đúng dòng đó. Bảng §5 là cổng — cùng một hành động phải có cùng một icon ở mọi màn hình.
2. **Có icon nào là quy ước ngành cho việc này không?** Bút chì = sửa, thùng rác = xoá, kính lúp = tìm. Đừng sáng tạo ở chỗ người dùng đã có sẵn phản xạ.
3. **Icon có tự giải thích được không, khi bỏ hết chữ đi?** Không thì **thêm nhãn chữ**, đừng đổi icon. Rất nhiều hành động (Duyệt, Bàn giao, Kết chuyển) đơn giản là không có icon nào nói được — đó là lúc dùng `Button` có chữ chứ không phải `IconButton`.
4. **Vẫn cần một icon mới?** Theo §6.

Ba điều cấm:

- 🛑 **Một icon không mang hai nghĩa trong cùng ứng dụng.** Nếu `pi-check` vừa là "lưu" vừa là "đã duyệt", người dùng sẽ đọc sai một trong hai.
- 🛑 **Một nghĩa không có hai icon.** Xoá ở màn này là thùng rác thì xoá ở màn kia cũng phải là thùng rác.
- 🛑 **Icon không thay được chữ ở hành động phá huỷ.** Nút xoá cuối cùng trong `ConfirmDialog` luôn có chữ.

Ba ghi chú áp luật trên:

- "Xem / hiện" là **một** nghĩa của `pi-eye` (hôm nay: hiện mật khẩu, §5). Cần "xem" ở chỗ khác thì dùng nó, không đẻ icon thứ hai.
- **Đi vào bản ghi từ danh sách không có icon.** Lối vào chi tiết là `<a routerLink>` ở ô định danh của hàng ([`Components/Button.md`](./Components/Button.md) §Khi nào dùng) — không có nút "xem chi tiết".
- `pi-check` là dấu **bước / mục đã xong** trong một luồng (`Stepper`, `Timeline`); `pi-check-circle` là icon vai **thông báo kết quả thành công** (`Toast`, `NoticeBanner`). Hai nghĩa tách bạch, không thay nhau.

---

## 3. Kích thước

> 📖 Giá trị bốn token `--icon-sm` · `--icon-md` · `--icon-lg` · `--icon-xl`: đọc [`DESIGN.md`](./DESIGN.md) §6.2.

File này quyết **chọn icon nào cho việc gì** và **icon hành xử ra sao với trình đọc màn hình**; con số thuộc về `DESIGN.md`, nơi duy nhất trong repo giữ giá trị px thật. Bốn luật hình học dưới đây thì thuộc về đây, vì chúng là luật về cách dùng chứ không phải giá trị.

Bốn luật hình học:

1. **Icon là hình vuông.** `width` bằng `height`, đặt cả hai — icon font kế thừa `line-height` của chữ bao quanh và sẽ méo nếu chỉ đặt `font-size`.
2. **Icon dùng `px`, không dùng `rem`.** Icon là hình, không phải chữ. Cho nó phóng theo cỡ chữ hệ thống làm nó vỡ khỏi khung nút.
3. **Khe giữa icon và chữ là `--sp-2`** (4px) ở cỡ `sm`, `--sp-3` (6px) ở `md` và `lg`.
4. **Căn giữa theo trục dọc bằng flex, không bằng `vertical-align`.** `display: inline-flex; align-items: center` trên phần tử bao. `vertical-align` phụ thuộc đường cơ sở của font và sẽ lệch mỗi khi cỡ chữ đổi.

`EmptyState` là ca duy nhất cần icon lớn hơn `--icon-xl`. Nó **không** phóng to icon font — chữ phóng lên 48px sẽ vỡ nét. Thay vào đó nó dùng một hình minh hoạ hoặc một icon đặt trong vòng tròn nền `--color-surface-2`, với icon bên trong giữ `--icon-xl`. Đường kính vòng tròn theo từng cỡ khai ở [`Components/EmptyState.md`](./Components/EmptyState.md) §Kích thước — file này không giữ bản sao của con số đó.

---

## 4. Màu

**Mặc định: icon kế thừa màu chữ của phần tử chứa nó** (`color: currentColor`). Đây là hành vi đúng cho gần như mọi chỗ — icon trong nút primary tự thành màu chữ nút primary, icon trong ô bảng tự thành `--color-text`.

Chỉ bốn trường hợp icon được **tự đặt màu**:

| Trường hợp | Token | Vì sao được phá luật kế thừa |
| --- | --- | --- |
| Icon vai của `NoticeBanner` / `Toast` | `--color-success` · `--color-warning` · `--color-danger` · `--color-info` | Icon **chính là** kênh thứ hai bên cạnh màu nền, theo [`DESIGN.md`](./DESIGN.md) §2.7 |
| Icon dẫn trong ô nhập | `--color-text-muted` | Nó là trang trí, phải lùi lại sau chữ người dùng gõ |
| Icon của `IconButton` biến thể `danger` | `--color-danger` | Cảnh báo phá huỷ phải thấy trước khi bấm |
| Icon trạng thái trong ô bảng | Token trạng thái tương ứng | Cùng lý do dòng đầu |

Ba luật màu:

- **Icon mang nghĩa phải đạt ≥ 3:1** với nền của nó (WCAG 2.2 SC 1.4.11). Mọi token trạng thái ở [`DESIGN.md`](./DESIGN.md) §2.5 đều vượt xa ngưỡng này — thấp nhất là 3.27:1 cho viền, còn màu chữ/icon thấp nhất là 5.38:1.
- **Icon thuần trang trí không có ngưỡng**, vì nó không mang thông tin — nhưng nó cũng phải **vô hình với trình đọc màn hình**. Xem §7.
- 🛑 **Không hardcode màu icon.** Luật cấm hex literal ở [`../RULES.md`](../RULES.md) §7 F6 áp cho icon như mọi thứ khác.

---

## 5. Bảng ánh xạ hành động → icon

Đây là cổng của §2 bước 1. Một hành động có trong bảng thì **phải** dùng đúng icon ở đây.

### Thao tác dữ liệu

| Hành động | Icon | Dùng ở |
| --- | --- | --- |
| Thêm mới | `pi-plus` | `Toolbar`, `PageHeader` |
| Sửa | `pi-pencil` | `IconButton` trong hàng bảng |
| Xoá | `pi-trash` | `IconButton` trong hàng bảng, `ConfirmDialog` |
| Nhân bản | `pi-copy` | Menu hành động của hàng |
| Mở menu hành động của hàng | `pi-ellipsis-v` | `IconButton` trong hàng bảng, mở `Menu` |
| Lưu | `pi-save` | Chân `Dialog`, `Toolbar` |
| Huỷ / đóng — **chỉ** nghĩa này | `pi-times` | Đầu `Dialog`, `Toast`, nút gỡ chip lọc. Bước lỗi, mục trả lại → dòng Lỗi |
| Tải lại | `pi-refresh` | `Toolbar`, `EmptyState` khi lỗi |

### Tìm, lọc, sắp xếp

| Hành động | Icon | Dùng ở |
| --- | --- | --- |
| Tìm kiếm | `pi-search` | Ô tìm trong `Toolbar` |
| Mở bộ lọc | `pi-filter` | `Toolbar` |
| Xoá bộ lọc | `pi-filter-slash` | `Toolbar` |
| Mở lịch chọn ngày | `pi-calendar` | Nút `suffix` của [`Components/DatePicker.md`](./Components/DatePicker.md) |
| Sắp xếp — chưa chọn | `pi-sort-alt` | `th` trong `DataTable` |
| Sắp xếp tăng | `pi-sort-amount-up-alt` | `th` trong `DataTable` |
| Sắp xếp giảm | `pi-sort-amount-down` | `th` trong `DataTable` |

### Điều hướng

| Hành động | Icon | Dùng ở |
| --- | --- | --- |
| Mở/đóng menu | `pi-bars` | `Topbar` ở màn nhỏ |
| Quay lại | `pi-arrow-left` | `PageHeader` của màn chi tiết |
| Mở rộng nhánh | `pi-chevron-right` | Cây menu `Sidebar`, hàng cha trong bảng |
| Thu nhánh | `pi-chevron-down` | Như trên, khi đang mở |
| Trang trước / sau | `pi-angle-left` · `pi-angle-right` | `Pagination` |
| Trang đầu / cuối | `pi-angle-double-left` · `pi-angle-double-right` | `Pagination` |
| Mở tab mới | `pi-external-link` | Liên kết ra ngoài |

### Trạng thái và phản hồi

| Hành động | Icon | Dùng ở |
| --- | --- | --- |
| Thành công | `pi-check-circle` | `Toast`, `NoticeBanner` |
| Cảnh báo | `pi-exclamation-triangle` | `Toast`, `NoticeBanner`, `ConfirmDialog` mức nguy hiểm |
| Lỗi | `pi-times-circle` | `Toast`, `NoticeBanner`, `FormRow` khi lỗi; chấm bước `error` của [`Components/Stepper.md`](./Components/Stepper.md); chấm mục `rejected` (trả lại) ở biến thể `approval` của [`Components/Timeline.md`](./Components/Timeline.md) |
| Thông tin | `pi-info-circle` | `Toast`, `NoticeBanner`, gợi ý bên trường |
| Đang xử lý | `pi-spinner` (quay) | `Button` khi `loading`, `DataTable` khi tải |
| Hỏi xác nhận | `pi-question-circle` | `ConfirmDialog` mức thường |
| Bước · mục đã xong, đã duyệt | `pi-check` | Chấm bước `done` của [`Components/Stepper.md`](./Components/Stepper.md); chấm mục `done` ở biến thể `approval` và sự việc "duyệt" ở biến thể `history` của [`Components/Timeline.md`](./Components/Timeline.md). Khác `pi-check-circle` (dòng Thành công) — đây là dấu trên một chấm tiến trình, không phải icon vai của thông báo |
| Bước · mục chưa tới lượt | `pi-circle` | Chấm mục `upcoming` ở biến thể `approval` của [`Components/Timeline.md`](./Components/Timeline.md). `Stepper` không dùng icon ở bước chưa tới — chấm mang số thứ tự |

### Tài khoản, tệp, hệ thống

| Hành động | Icon | Dùng ở |
| --- | --- | --- |
| Người dùng | `pi-user` | `Avatar` khi không có ảnh, `Topbar` |
| Đăng xuất | `pi-sign-out` | Menu người dùng ở `Topbar` |
| Đổi mật khẩu · đặt lại mật khẩu hộ | `pi-key` | Menu người dùng; menu hành động của hàng |
| Hiện mật khẩu | `pi-eye` | Nút hiện/ẩn của [`Components/Input.md`](./Components/Input.md) biến thể `password` khi `revealable` bật, và của [`Components/AuthField.md`](./Components/AuthField.md) biến thể `password` |
| Ẩn mật khẩu | `pi-eye-slash` | Cùng nút, khi mật khẩu đang hiện |
| Khoá · không có quyền (403) — **chỉ** nghĩa này | `pi-lock` | Menu hành động của hàng; `EmptyState` biến thể `no-permission`; sự việc "khoá" ở biến thể `history` của `Timeline`. Chip lọc không gỡ được **không có icon** |
| Mở khoá | `pi-lock-open` | Menu hành động của hàng |
| Ngưng · bật lại hoạt động của đơn vị | `pi-power-off` | Menu hành động của hàng |
| Đơn vị | `pi-building` | `AuthField` biến thể `tenantCode` |
| Đổi ngôn ngữ | `pi-globe` | `LanguageSwitcher`; mục "Ngôn ngữ" trong menu người dùng của `Topbar` |
| Đổi theme | `pi-sun` · `pi-moon` · `pi-desktop` | `Topbar` — icon hiện giá trị **đang áp**: sáng · tối · theo hệ điều hành |
| Tải tệp lên | `pi-upload` | `FileUpload` |
| Tải tệp xuống | `pi-download` | `Toolbar` |
| Tệp đính kèm | `pi-file` | Danh sách tệp của `FileUpload` |
| Không có dữ liệu | `pi-inbox` | `EmptyState` |
| Cần cấu hình trước | `pi-cog` | `EmptyState` biến thể `not-configured` |
| Không tìm thấy đích — đường dẫn không khớp trang nào (404), hoặc bản ghi không còn trong khi tuyến vẫn đúng | `pi-compass` | `EmptyState` biến thể `not-found` và `record-not-found` — một nghĩa ("thứ được trỏ tới không có ở đây"), hai chỗ dùng |

Cần một hành động chưa có trong bảng → thêm dòng vào đây **trước**, rồi mới dùng.

---

## 6. Thêm một icon mới

1. **Kiểm bảng §5.** Trùng nghĩa với một dòng đã có thì dùng dòng đó — đừng thêm.
2. **Kiểm bộ PrimeIcons còn icon nào hợp không.** Bộ này khá rộng; phần lớn "thiếu icon" thật ra là chưa tìm kỹ.
3. **Thêm một dòng vào bảng §5**, nêu rõ hành động và chỗ dùng.
4. **PrimeIcons thật sự không có** → chỉ khi đó mới thêm SVG riêng, và phải:
   - Đặt trong tài sản dùng chung, không nội tuyến vào template từng màn.
   - Vẽ trên khung 24×24, nét 1.5px, đầu nét bo — để đứng cạnh PrimeIcons không lệch phong cách.
   - Dùng `fill="currentColor"` hoặc `stroke="currentColor"`, **không** hardcode màu, để luật §4 vẫn đúng.
   - Ghi vào bảng §5 với ghi chú `SVG riêng` ở cột icon.
5. **Không bao giờ nạp thêm một bộ icon thứ hai** chỉ để lấy một icon.

---

## 7. Accessibility — icon trang trí và icon mang nghĩa

Đây là phần dễ làm sai nhất và ảnh hưởng thật tới người dùng trình đọc màn hình. Chỉ có hai loại icon, và mỗi loại có một cách xử lý duy nhất.

### Loại 1 — icon trang trí

Icon đứng **cạnh chữ nói cùng một điều**. Chữ đã mang hết thông tin.

```text
<button class="btn">
  <i class="pi pi-plus" aria-hidden="true"></i>
  Thêm người dùng
</button>
```

- ✅ **Bắt buộc `aria-hidden="true"`.**
- 🛑 Không đặt `aria-label` cho nó. Trình đọc sẽ đọc "Thêm Thêm người dùng".

**Vì sao icon font đặc biệt nguy hiểm ở đây:** ký tự icon nằm trong vùng Private Use Area của Unicode. Không có `aria-hidden`, một số trình đọc màn hình sẽ đọc lên tên ký tự riêng hoặc một chuỗi rác trước mỗi nhãn nút. Người dùng nghe thấy tạp âm trước từng nút, suốt cả ứng dụng.

### Loại 2 — icon mang nghĩa

Icon là **thông tin duy nhất** — nút chỉ có icon, hoặc icon trạng thái trong ô bảng.

```text
<button class="icon-btn" aria-label="Sửa người dùng">
  <i class="pi pi-pencil" aria-hidden="true"></i>
</button>
```

- ✅ Nhãn đặt trên **phần tử tương tác** (`aria-label` trên `<button>`), không trên `<i>`.
- ✅ Icon bên trong **vẫn** `aria-hidden="true"`.
- ✅ Nhãn phải nói **hành động và đối tượng**: "Sửa người dùng", không phải "Sửa".
- ✅ Nhãn đi qua tầng i18n. Không viết chữ tiếng Việt thẳng vào template — cổng FE bắt, xem [`../RULES.md`](../RULES.md) §7 F8.

Với icon **không** tương tác nhưng mang nghĩa (chấm trạng thái trong ô bảng), dùng chữ chỉ dành cho trình đọc màn hình đặt cạnh icon, thay vì `aria-label` trên một thẻ `<i>` — `<i>` không có vai trò ARIA nên nhiều trình đọc bỏ qua nhãn của nó.

### Phép thử một câu

> **Tắt hết icon đi, nội dung có còn hiểu được không?**
>
> Còn → icon là trang trí → `aria-hidden="true"`, hết.
> Không → icon mang nghĩa → phần tử chứa nó phải có nhãn chữ.

### Ba luật còn lại

| Luật | Vì sao |
| --- | --- |
| Icon quay (`pi-spinner`) phải dừng khi `prefers-reduced-motion: reduce` — thay bằng chỉ báo tĩnh | Chuyển động lặp vô hạn là tác nhân gây khó chịu. Xem [`DESIGN.md`](./DESIGN.md) §7 |
| Icon không bao giờ là kênh phân biệt duy nhất giữa hai trạng thái **cùng hình dạng, khác màu** | Người mù màu không phân biệt được. Đổi hình dạng, đừng chỉ đổi màu |
| Vùng bấm của `IconButton` ≥ 28×28px kể cả khi icon 16px | WCAG 2.2 SC 2.5.8. Nới bằng `padding` |

---

## 8. Cần chốt

| # | Câu hỏi để ngỏ | Ai trả lời được |
| --- | --- | --- |
| 1 | Có gom PrimeIcons thành subset chỉ chứa icon dùng thật không? Bộ đầy đủ tốn băng thông không cần thiết, nhưng subset làm việc thêm một icon phải chạy lại bước build | Sau khi có ngân sách bundle thật ([`../RULES.md`](../RULES.md) §7 F14) |
| 2 | Icon do PrimeNG tự chèn lúc chạy (`DataTable`, `Pagination`, `FileUpload`) có `aria-hidden` đúng không? Chúng không xuất hiện trong source của mình nên grep không thấy | Khi có `src/` để kiểm bằng trình đọc màn hình |
| 3 | Có cần một icon riêng cho "bản ghi đã xoá mềm" không? Core có soft delete ([`../RULES.md`](../RULES.md) §4 E3) nhưng chưa quyết cách hiển thị | Dự án đầu tiên có màn thùng rác |
