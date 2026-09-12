---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 09. Form và kiểm tra dữ liệu

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`; đây là hạ tầng form phải dựng ở pha F3.
>
> Hình dạng `fieldErrors` từ BE: [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §2.1 · kiểu phía FE: [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §1. Luật validation phía BE: [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md).

---

## 1. Reactive form, không dùng template-driven

Chọn reactive form cho **mọi** form, kể cả form một ô.

| | Reactive | Template-driven |
| --- | --- | --- |
| Kiểu dữ liệu | Có kiểu thật, compiler kiểm được | Ràng buộc lỏng, sai chỉ lộ lúc chạy |
| Bind lỗi từ server vào từng ô | Gọi một phương thức trên control | Phải đi vòng qua tham chiếu template |
| Test | Dựng form trực tiếp, không cần DOM | Phải dựng cả component |
| Form động từ metadata | Tự nhiên | Rất khó |

Trộn hai kiểu trong một codebase là chi phí thuần: mỗi người phải nhớ màn này thuộc kiểu nào. Chọn một và giữ.

---

## 2. Typed form — và cái bẫy `null`

```typescript
private readonly fb = inject(NonNullableFormBuilder);

readonly form = this.fb.group({
  email:    ['', [Validators.required, Validators.email]],
  hoTen:    ['', [Validators.required, Validators.maxLength(200)]],
  vaiTroId: this.fb.control<string | null>(null),
});
```

**Vì sao dùng `NonNullableFormBuilder` làm mặc định:** với `FormBuilder` thường, mọi control có kiểu suy ra là `T | null`, vì `reset()` đưa giá trị về `null`. Hệ quả là mọi chỗ đọc giá trị đều phải xử lý `null` — và người ta sẽ dùng dấu chấm than để cho qua, tức vứt bỏ đúng lợi ích của typed form.

`NonNullableFormBuilder` làm `reset()` quay về **giá trị khởi tạo** thay vì `null`. Đó gần như luôn là thứ ta muốn ở một form nghiệp vụ.

Khi một trường **thật sự** có nghĩa "chưa chọn" (khoá ngoại tuỳ chọn), khai `null` tường minh như dòng cuối ở trên. Lúc đó `null` mang nghĩa, không phải là tai nạn của API.

### 2.1 Đọc giá trị: `getRawValue()`, không phải `value`

`form.value` **bỏ qua control đang bị vô hiệu hoá**. Một form có ô chỉ đọc (mã tự sinh, đơn vị cố định) sẽ gửi lên payload thiếu đúng những trường đó — và không có gì báo. Dùng `getRawValue()` khi dựng payload.

---

## 3. Ranh giới: kiểm ở client và kiểm ở server

| | Client | Server |
| --- | --- | --- |
| Vai | Trải nghiệm — báo sớm, không phải chờ vòng gọi | **Bảo vệ** — nguồn sự thật duy nhất |
| Kiểm gì | Bắt buộc, độ dài, định dạng, khớp hai ô | Tất cả những thứ đó, cộng luật nghiệp vụ |
| Bỏ qua được không | Được — mở công cụ dev là bỏ qua | Không |

**Luật nền: mọi thứ client kiểm thì server cũng phải kiểm.** Không có ngoại lệ. Ngược lại thì không: server có những luật client **không nên** kiểm.

### 3.1 Ba loại luật client KHÔNG được tự kiểm

| Loại | Ví dụ | Vì sao |
| --- | --- | --- |
| Cần dữ liệu client không có | Email đã tồn tại chưa | Client không có bảng người dùng. Đoán bằng một lần gọi tra cứu là tạo ra đua điều kiện: giữa lúc tra và lúc lưu, người khác đã đăng ký |
| Luật nghiệp vụ có thể đổi | Chính sách độ mạnh mật khẩu | Nhân đôi luật ở hai nơi thì chúng sẽ lệch, và bản ở client là bản không ai nhớ sửa |
| Phụ thuộc quyền của người gọi | Trường này người dùng có được sửa không | Kiểm ở client là gợi ý; quyết định thuộc server |

**Hệ quả thiết kế:** form phải xử lý tốt trường hợp *"client thấy hợp lệ, server từ chối"*. Đó là trạng thái **bình thường**, không phải lỗi hệ thống — và §4 là cách xử lý nó.

---

## 4. Hiển thị `fieldErrors` từ BE vào đúng ô

### 4.1 Cơ chế — hiện thực ở ĐÚNG một chỗ

Hàm dưới đây là **Hàm gắn fieldErrors vào form — định nghĩa gốc**: `shared/forms/apply-field-errors.ts` là nơi duy nhất gắn `fieldErrors` vào form. Page gọi nó; page **không** viết lại vòng lặp này dưới dạng phương thức private của mình — hai bản của cùng một hàm là hai bản sẽ lệch, và chúng lệch ở đúng chỗ hỏng im lặng nhất.

```typescript
// shared/forms/apply-field-errors.ts
export function applyFieldErrors(
  form: FormGroup,
  fieldErrors: Readonly<Record<string, readonly ApiFieldError[]>> | null | undefined,
  translate: TranslateService,
): string[] {
  if (!fieldErrors) return [];
  const khongKhop: string[] = [];

  for (const [khoaBE, loi] of Object.entries(fieldErrors)) {
    // Khoá BE là tên property của DTO (PascalCase). Bước chuyển sang tên
    // control nằm Ở ĐÂY, không rải ra từng màn.
    const control = form.get(tenControlTuKhoaBE(khoaBE));
    if (!control) { khongKhop.push(khoaBE); continue; }   // KHÔNG nuốt im lặng — xem §4.3
    const cau = loi.map((l) => translate.instant(`loi.${l.code}`, l.messageParams ?? {}));
    control.setErrors({ ...(control.errors ?? {}), server: cau });
    control.markAsTouched();
  }
  return khongKhop;
}
```

Bốn chi tiết bắt buộc:

| Chi tiết | Vì sao |
| --- | --- |
| Chuyển khoá BE → tên control, **trong hàm này** | Khoá trên dây giữ PascalCase ([`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §2.3), tên control là camelCase. Tra thẳng thì không khớp ô nào và **không có gì báo** |
| **Dịch từng `code`**, không gắn thẳng giá trị vào control | Giá trị là mảng object `{code, messageParams}`. Bind thẳng cho ra `[object Object]`; ép về chuỗi thì vứt mất mã và lỗi từng ô không dịch được |
| Gộp vào `errors` sẵn có, không ghi đè | Ghi đè xoá mất lỗi client đang hiển thị trên cùng ô |
| `markAsTouched()` | Phần lớn cách hiển thị lỗi chỉ hiện khi control đã "chạm tới". Không gọi thì lỗi có mà không hiện |
| Trả về danh sách khoá không khớp | Đây là điểm phát hiện lệch hợp đồng — xem §4.3 |

> 📖 Kiểu `ApiFieldError` và chỗ khai nó: [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §1. Cách một page gọi hàm này: [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §6.2.

### 4.2 Xoá lỗi server khi người dùng gõ lại

Lỗi server gắn vào control sẽ **dính lại vĩnh viễn** nếu không xoá: người dùng sửa email, ô vẫn đỏ, và họ không biết phải làm gì.

Quy tắc: khi giá trị của một control đổi, xoá khoá lỗi `server` của **chính** control đó, giữ nguyên các lỗi khác. Đừng xoá lỗi server của toàn form — người dùng sửa một ô không có nghĩa là ô kia đã đúng.

### 4.3 Khoá không khớp là lỗi, không phải chuyện nhỏ

Khi BE trả một khoá mà form không có control tương ứng, thông báo lỗi đó **biến mất hoàn toàn**: không vào ô nào, không vào thông báo chung. Người dùng bấm Lưu, không có gì xảy ra, không có gì hiện ra.

Đây là dạng hỏng im lặng tệ nhất trong toàn bộ tài liệu này, và nó xảy ra vì những nguyên nhân rất tầm thường: BE đổi tên trường, bước chuyển khoá hoa/thường bị bỏ ([`02-http-envelope.md`](02-http-envelope.md) §2.2), hoặc trường lồng trong đối tượng con.

Xử lý bắt buộc: khoá không khớp phải được **gộp vào thông báo lỗi chung** để người dùng ít nhất thấy có gì đó sai, và ghi log ở môi trường phát triển để lập trình viên thấy ngay.

### 4.4 Trường lồng nhau

Nếu payload có đối tượng con, BE trả khoá dạng `diaChi.tinh` và `form.get('diaChi.tinh')` xử lý được. Nhưng với **mảng** thì khoá thường mang chỉ số (`dongHang[0].soLuong`), và ký hiệu ngoặc vuông **không** tra được trực tiếp. Phải chuyển sang dạng dấu chấm trước khi tra. Đây là chỗ luôn phải kiểm bằng một trường hợp thật, không suy đoán.

---

## 5. Hiển thị lỗi cho người dùng

| Quy tắc | Vì sao |
| --- | --- |
| Lỗi hiện **ngay dưới ô**, không chỉ trong một hộp ở đầu form | Người dùng không phải tự nối thông báo với ô |
| Chỉ hiện sau khi ô đã "chạm tới" hoặc sau khi bấm Lưu | Hiện lỗi ngay khi form vừa mở là mắng người dùng trước khi họ gõ |
| Bấm Lưu trên form không hợp lệ → hiện hết lỗi **và** đưa focus về ô sai đầu tiên | Trên form dài, lỗi có thể nằm ngoài màn hình. Không cuộn tới thì trông như nút Lưu bị hỏng |
| Câu lỗi nói **cách sửa**, không chỉ nói sai | "Mật khẩu tối thiểu 8 ký tự" hơn hẳn "Giá trị không hợp lệ" |
| Ô lỗi phải liên kết với thông báo bằng thuộc tính ARIA | Trình đọc màn hình không "thấy" chữ đỏ bên dưới — [`15-accessibility.md`](15-accessibility.md) §5 |

**Chống bấm Lưu nhiều lần:** vô hiệu hoá nút trong lúc đang gửi. Không làm thì một lần bấm đúp tạo hai bản ghi — và ở form tạo mới, đó là lỗi người dùng nhìn thấy ngay.

---

## 6. Form động từ metadata

Có màn mà cấu trúc form đến từ dữ liệu, không từ code: bộ tiêu chí cấu hình được, biểu mẫu do quản trị viên định nghĩa. Khi đó form được dựng lúc chạy từ một mô tả.

**Ngưỡng để làm form động — chỉ khi thoả cả hai:** (1) người dùng cuối thật sự thêm/bớt trường được, và (2) số biến thể quá nhiều để viết tay.

Không thoả cả hai thì form động là chi phí thuần: khó test hơn, khó đọc hơn, và mọi trường hợp đặc biệt đều phải đưa vào mô tả metadata dưới dạng một cờ mới.

Ba ràng buộc khi làm:

| Ràng buộc | Vì sao |
| --- | --- |
| Metadata **không** chứa mã JavaScript hay biểu thức chạy được | Đó là thực thi mã từ dữ liệu — xem [`14-security.md`](14-security.md) §1 |
| Kiểu trường là tập đóng, khai ở FE | Kiểu lạ từ server phải suy giảm êm về ô nhập chữ, không làm trắng màn hình |
| Luật kiểm tra trong metadata vẫn phải có bản ở server | §3 không có ngoại lệ cho form động |

Cơ chế metadata phía BE: [`../be/03-metadata-driven-design.md`](../be/03-metadata-driven-design.md).

---

## 7. Cảnh báo mất dữ liệu khi rời trang

Có hai đường rời trang và chúng **hoàn toàn khác nhau**:

| Đường | Chặn bằng | Kiểm soát được gì |
| --- | --- | --- |
| Điều hướng trong app (bấm menu, nút Quay lại) | Guard `CanDeactivate` | Toàn quyền — hiện hộp thoại của mình, câu chữ của mình |
| Đóng tab, tải lại, gõ URL khác | Sự kiện `beforeunload` của trình duyệt | Rất ít — trình duyệt hiện câu của **nó**, không dùng được câu của mình |

Phải làm **cả hai**, vì mỗi đường chỉ bắt được một nửa.

### 7.1 Bốn ràng buộc

1. **Chỉ cảnh báo khi form thật sự bẩn.** Cảnh báo khi người dùng chưa sửa gì là cách nhanh nhất để họ học phản xạ bấm "Rời đi" mà không đọc.
2. **`dirty` không đủ chính xác.** Angular đánh dấu `dirty` ngay khi một control bị chạm, kể cả khi người dùng gõ vào rồi xoá về giá trị cũ. Muốn chính xác thì so giá trị hiện tại với ảnh chụp lúc mở form.
3. **Lưu xong phải gỡ cờ bẩn trước khi điều hướng.** Quên thì người dùng lưu thành công vẫn bị hỏi "bạn có chắc muốn rời đi".
4. **Đăng xuất và hết phiên bỏ qua cảnh báo.** Giữ người dùng lại ở một form không lưu được nữa là vô nghĩa.

---

## 8. Test form

| Test | Cụ thể |
| --- | --- |
| Trạng thái hợp lệ | Form rỗng không hợp lệ; điền đủ thì hợp lệ |
| Bind lỗi server | Đưa vào một `fieldErrors` giả → đúng control mang lỗi, đúng thông điệp |
| **Khoá không khớp** | Đưa vào một khoá không tồn tại → hàm trả về khoá đó, không nuốt im lặng |
| Xoá lỗi server | Gõ lại vào ô đang lỗi → lỗi `server` biến mất, lỗi client giữ nguyên |
| `getRawValue` | Vô hiệu hoá một control → payload vẫn có trường đó |
| Guard rời trang | Form bẩn → chặn; form sạch → cho qua; sau khi lưu → cho qua |

Ba dòng đầu là ba dòng đáng viết nhất — chúng bắt đúng những lỗi không lộ ra khi bấm thử.

---

## 9. Kiểm chứng

- [ ] Tạo bản ghi với dữ liệu sai → lỗi hiện **trên từng ô**, không chỉ một thông báo chung
- [ ] Mỗi khoá trong `fieldErrors` gắn được vào đúng một control sau bước chuyển khoá, và mã lỗi từng ô **được dịch** (kiểm bằng tab Network)
- [ ] Một khoá lỗi không khớp control → vẫn hiện ở thông báo chung, không biến mất
- [ ] Sửa ô đang lỗi → lỗi server biến mất, lỗi client còn nguyên nếu vẫn sai
- [ ] Bấm Lưu trên form dài không hợp lệ → cuộn và đưa focus tới ô sai đầu tiên
- [ ] Bấm Lưu hai lần nhanh → chỉ một bản ghi được tạo
- [ ] Sửa form rồi bấm menu khác → có hỏi; sau khi lưu rồi rời đi → không hỏi
- [ ] Vô hiệu hoá một control → payload gửi lên vẫn đủ trường
- [ ] Trình đọc màn hình đọc được thông báo lỗi của ô đang focus

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Typed reactive form, không nhận `null` mặc định | ✅ sẽ có | §2 |
| Hạ tầng bind `fieldErrors` ở `shared/forms/`, trả về khoá không khớp | ✅ sẽ có | §4.1, §4.3 — một hiện thực, không có bản sao trong page |
| Xoá lỗi server khi người dùng gõ lại | ✅ sẽ có | §4.2 |
| Cảnh báo rời trang — cả hai đường | ✅ sẽ có | §7 |
| Chống bấm Lưu nhiều lần | ✅ sẽ có | §5 |
| Form động từ metadata | ❌ chưa | Điều kiện: thoả **cả hai** ngưỡng ở §6 — [`../be/03-metadata-driven-design.md`](../be/03-metadata-driven-design.md) |
| Tự lưu nháp | ❌ chưa | Điều kiện: có form dài thật mà mất dữ liệu gây hậu quả |
| Form nhiều bước | ❌ chưa | Điều kiện: có quy trình nghiệp vụ thật cần chia bước |
| Template-driven form | ❌ loại, không hoãn `K27` | §1 — trộn hai kiểu là chi phí thuần |
| Kiểm luật nghiệp vụ ở client | ❌ loại, không hoãn `K28` | §3.1 — nhân đôi luật thì hai bản sẽ lệch |
| Metadata chứa biểu thức chạy được | ❌ loại, không hoãn `K29` | §6 — đó là thực thi mã từ dữ liệu |

Một finding dạng *"FE thiếu X"* chỉ hợp lệ khi X mang trạng thái **✅ sẽ có** mà vắng mặt, hoặc khi điều kiện ở cột ghi chú của một dòng **❌ chưa** đã xảy ra. Dòng **❌ loại, không hoãn** chỉ đổi được bằng một ADR mới, không đổi được bằng một finding.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Hình dạng `fieldErrors` trên dây | [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §2.1 |
| Kiểu `ApiFieldError` phía FE | [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §1 |
| Component form field dùng chung | [`05-component-library.md`](05-component-library.md) |
| Câu lỗi lấy từ đâu | [`08-i18n.md`](08-i18n.md) §5 |
| Validation phía BE | [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) |
| Metadata phía BE | [`../be/03-metadata-driven-design.md`](../be/03-metadata-driven-design.md) |
| ARIA cho thông báo lỗi | [`15-accessibility.md`](15-accessibility.md) §5 |
