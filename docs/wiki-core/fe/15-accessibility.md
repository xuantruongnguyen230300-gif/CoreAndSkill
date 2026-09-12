---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 15. Khả năng tiếp cận

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> **Điểm vào chung cho chủ đề tiếp cận.** Phần riêng của biểu đồ nằm ở [`12-charting.md`](12-charting.md) §4; phần riêng của bảng ở [`11-grid-and-metadata.md`](11-grid-and-metadata.md) §9; tương phản màu nối với [`04-design-token-system.md`](04-design-token-system.md).

---

## 1. Mức chuẩn nhắm tới

**WCAG 2.2 mức AA.** Đây là mức được dùng làm chuẩn tham chiếu trong hầu hết quy định về tiếp cận, và là mức đạt được mà không phải hy sinh thiết kế.

**Không nhắm mức AAA** cho toàn bộ ứng dụng: một số tiêu chí AAA mâu thuẫn với nhau và với yêu cầu thiết kế thông thường (ví dụ ngưỡng tương phản của nó loại bỏ phần lớn bảng màu thương hiệu). Áp AAA cho từng phần cụ thể khi có lý do, không áp đại trà.

### 1.1 Vì sao đáng làm, nói thẳng chứ không nói đạo lý

| Lý do | Cụ thể |
| --- | --- |
| Người dùng thật | Thị lực kém, mù màu, khó dùng chuột — tỷ lệ trong một tổ chức không hề nhỏ |
| Người dùng bàn phím **không khuyết tật** | Người nhập liệu chuyên nghiệp làm việc bằng phím. App không dùng được bằng phím là app làm họ chậm đi |
| Chất lượng chung | Phần lớn lỗi tiếp cận cũng là lỗi ngữ nghĩa HTML — sửa chúng thường làm code sạch hơn |
| Ràng buộc pháp lý / mua sắm | Nhiều tổ chức đưa tiếp cận vào tiêu chí lựa chọn |

Lý do thứ hai là lý do thuyết phục nhất ở loại ứng dụng này và ít được nhắc nhất.

---

## 2. Checklist cho một màn hình mới

> Đây là phần dùng hằng ngày. Dán vào mô tả PR của mỗi màn mới.

**Cấu trúc**

- [ ] Trang có đúng một tiêu đề cấp một; các cấp tiêu đề không nhảy cóc
- [ ] Dùng thẻ có ngữ nghĩa (điều hướng, vùng chính, tiêu đề, chân trang) thay vì thẻ chia khối trống
- [ ] Có liên kết bỏ qua điều hướng để tới thẳng nội dung chính
- [ ] Tiêu đề tài liệu đổi theo màn — người dùng nhiều tab phân biệt được

**Bàn phím**

- [ ] Đi hết màn bằng phím Tab tới được **mọi** thứ tương tác được
- [ ] Thứ tự Tab đi theo thứ tự thị giác
- [ ] Không có bẫy focus: vào được thì ra được
- [ ] Mỗi phần tử đang focus **thấy rõ** đang ở đâu
- [ ] Phím Esc đóng lớp phủ đang mở

**Form**

- [ ] Mọi ô nhập có nhãn liên kết đúng (không chỉ chữ đặt cạnh)
- [ ] Ô bắt buộc được đánh dấu bằng cả thuộc tính lẫn dấu hiệu thị giác
- [ ] Thông báo lỗi liên kết với ô bằng thuộc tính mô tả, và ô được đánh dấu là không hợp lệ
- [ ] Không dùng riêng màu để chỉ trạng thái lỗi

**Nội dung**

- [ ] Ảnh mang thông tin có mô tả thay thế; ảnh trang trí để mô tả rỗng
- [ ] Biểu tượng đứng một mình (nút chỉ có icon) có nhãn cho công nghệ hỗ trợ
- [ ] Chữ trên nền đạt ngưỡng tương phản (§4)
- [ ] Không truyền đạt thông tin **chỉ** bằng màu, hình dạng hay vị trí

**Động**

- [ ] Nội dung xuất hiện động (toast, kết quả tìm kiếm) được công bố cho công nghệ hỗ trợ
- [ ] Có tôn trọng thiết lập giảm chuyển động của hệ điều hành
- [ ] Không có gì tự nháy hoặc tự chuyển mà không dừng được

---

## 3. Quản lý focus — phần khó nhất

Đây là phần mà một ứng dụng một trang khác hẳn một website nhiều trang, và là phần dễ sai nhất.

### 3.1 Điều hướng route

Chuyển trang trong ứng dụng một trang **không** đặt lại focus — nó ở nguyên chỗ cũ, tức ở một phần tử của màn đã biến mất. Người dùng trình đọc màn hình không được báo gì cả và tưởng chưa có gì xảy ra.

Cách xử lý: sau mỗi lần điều hướng, đưa focus về tiêu đề của màn mới (hoặc về vùng nội dung chính), và cập nhật tiêu đề tài liệu.

### 3.2 Hộp thoại

Bốn việc, thiếu một là hộp thoại không dùng được bằng bàn phím:

| Việc | Thiếu thì sao |
| --- | --- |
| Đưa focus vào trong khi mở | Người dùng bàn phím vẫn ở phía sau nền mờ |
| Giữ focus bên trong khi Tab | Tab đi ra sau nền mờ, thao tác lên thứ không thấy được |
| Đóng bằng Esc | Không có đường thoát bằng phím |
| **Trả focus về phần tử đã mở nó** | Đóng xong focus về đầu trang, mất chỗ đang làm |

Việc thứ tư hay bị quên nhất và gây khó chịu nhất trên màn danh sách: mở hộp thoại từ dòng thứ 40, đóng lại, focus về đầu trang, phải Tab lại từ đầu.

### 3.3 Nội dung xuất hiện động

Toast, kết quả tìm kiếm cập nhật, thông báo lưu thành công — người dùng trình đọc màn hình không "thấy" chúng. Phải công bố chúng qua một vùng thông báo trực tiếp.

Hai mức, dùng đúng mức: mức lịch sự cho thông tin (chờ đọc xong câu hiện tại), mức khẩn cho lỗi cần biết ngay. Đặt mọi thứ ở mức khẩn là làm trình đọc màn hình ngắt lời liên tục — khó dùng hơn là không có.

### 3.4 Trạng thái focus phải thấy được

Không được xoá đường viền focus mặc định mà không thay bằng thứ khác rõ hơn. Đây là thay đổi CSS phổ biến nhất phá hỏng khả năng dùng bàn phím, và nó thường được làm vì lý do thẩm mỹ.

Dùng cơ chế chỉ hiện viền focus khi điều hướng bằng bàn phím — nó cho cả hai: chuột không thấy viền, bàn phím thấy rõ. Định nghĩa nó **một lần** ở tầng token, không để từng component tự quyết.

---

## 4. Tương phản màu — nối với hệ token

### 4.1 Ngưỡng

| Loại | Ngưỡng AA |
| --- | --- |
| Chữ thường | 4.5:1 |
| Chữ lớn (cỡ lớn hoặc đậm) | 3:1 |
| Thành phần giao diện, viền ô nhập, biểu tượng mang nghĩa | 3:1 |

Dòng thứ ba hay bị bỏ qua: viền của một ô nhập quá nhạt là một lỗi tiếp cận, không chỉ là một lựa chọn thẩm mỹ.

### 4.2 Đo ở tầng token, không ở tầng component

Vì mọi màu đến từ token ([`04-design-token-system.md`](04-design-token-system.md)), **mọi cặp màu có thể xuất hiện đều biết trước**. Nghĩa là tương phản kiểm được một lần ở tầng token, thay vì kiểm từng màn.

Đây là một lợi ích của hệ token thường không được nêu, và nó biến một việc thủ công lặp lại thành một phép kiểm tự động được.

### 4.3 Kiểm cả hai chế độ

Một cặp màu đạt AA ở chế độ sáng **không** bảo đảm gì ở chế độ tối. Bảng kiểm phải phủ cả hai bộ giá trị token.

> ⚠️ Ở dự án tiền nhiệm, cặp màu cảnh báo và màu lỗi đo được dưới ngưỡng AA và tồn tại như vậy một thời gian dài. Nó không lộ ra vì không có phép kiểm nào — và mắt người quen dần với chính giao diện mình nhìn hằng ngày.

### 4.4 Màu không được là kênh duy nhất

Trạng thái chỉ phân biệt bằng màu là trạng thái một phần người dùng không đọc được. Luôn thêm một kênh thứ hai: chữ, biểu tượng, hoặc hình dạng.

Phép thử rẻ nhất: **in màn hình ra đen trắng.** Nếu vẫn đọc được thì đạt.

---

## 5. ARIA — dùng ít là dùng đúng

> **Luật đầu tiên của ARIA: đừng dùng ARIA nếu HTML gốc đã làm được.**

Một thẻ nút gốc đã có vai trò đúng, đã focus được, đã kích hoạt bằng phím Enter và Space. Một thẻ chia khối gắn thuộc tính vai trò "button" thì chỉ có vai trò — mọi hành vi còn lại phải tự viết, và người ta luôn quên một cái.

| Dùng ARIA khi | Không dùng khi |
| --- | --- |
| HTML không có phần tử tương ứng (tab, cây, thanh trạng thái) | Đã có phần tử gốc |
| Cần mô tả quan hệ (thông báo lỗi thuộc ô nào) | Chỉ để "cho có" |
| Cần công bố thay đổi động | Nội dung tĩnh đã đọc được |

**ARIA sai còn tệ hơn không có ARIA.** Một vai trò khai sai khiến trình đọc màn hình mô tả sai bản chất phần tử — người dùng nhận thông tin sai thay vì thiếu thông tin.

### 5.1 Bốn quan hệ hay dùng nhất

| Cần gì | Cơ chế |
| --- | --- |
| Nhãn cho phần tử không có chữ hiển thị | Thuộc tính nhãn |
| Nối một phần tử với chữ mô tả sẵn có | Thuộc tính tham chiếu nhãn |
| Nối ô nhập với thông báo lỗi của nó | Thuộc tính mô tả |
| Báo trạng thái không hợp lệ | Thuộc tính không hợp lệ |

Ưu tiên **tham chiếu tới chữ đã hiển thị** hơn là viết một chuỗi nhãn riêng: một chuỗi riêng là một bản sao, và bản sao sẽ lệch khi câu hiển thị đổi ([`08-i18n.md`](08-i18n.md)).

---

## 6. Ép bằng máy được tới đâu

| Mức | Công cụ | Bắt được gì |
| --- | --- | --- |
| Lint template | Bộ quy tắc tiếp cận của angular-eslint | Ảnh thiếu mô tả, nhãn không liên kết, sự kiện chuột không có tương đương bàn phím |
| Kiểm tự động lúc chạy | Thư viện kiểm trong test | Tương phản, vai trò sai, quan hệ thiếu |
| Kiểm bằng người | Đi bằng bàn phím, dùng trình đọc màn hình | Thứ tự hợp lý, câu chữ có nghĩa, luồng dùng được |

**Công cụ tự động bắt được khoảng một phần ba vấn đề.** Con số đó đủ để đáng bật, và không đủ để tuyên bố "đã đạt chuẩn". Ba dòng trên là ba tầng bổ sung nhau, không thay thế nhau.

Ở repo này, tầng đầu bật cùng cấu hình lint ngay từ pha F0 — nó gần như miễn phí và bắt được nhóm lỗi phổ biến nhất.

**Kiểm bằng người tối thiểu cho mỗi màn mới:** rút chuột ra và làm hết một luồng bằng bàn phím. Phép thử này mất vài phút và bắt được phần lớn lỗi nghiêm trọng.

---

## 7. Ba lỗi phổ biến nhất

| Lỗi | Vì sao xảy ra | Sửa |
| --- | --- | --- |
| Xoá viền focus vì thấy xấu | Lý do thẩm mỹ, không ai phản đối lúc đó | Thay bằng viền của mình, định nghĩa ở tầng token |
| Nút chỉ có biểu tượng, không nhãn | Trông gọn | Thêm nhãn cho công nghệ hỗ trợ |
| Thẻ chia khối có sự kiện bấm thay cho nút | Nhanh hơn khi viết | Dùng thẻ nút gốc; nếu không được thì phải tự thêm vai trò, khả năng focus, và xử lý phím |

Cả ba đều **không** ảnh hưởng gì tới người dùng chuột, nên chúng không bao giờ bị phát hiện trong quá trình phát triển thông thường.

---

## 8. Kiểm chứng

- [ ] Lint template với bộ quy tắc tiếp cận, không bỏ qua quy tắc nào
- [ ] Đi hết một luồng chính bằng **chỉ bàn phím**
- [ ] Mọi phần tử đang focus đều thấy rõ
- [ ] Hộp thoại: giữ focus, đóng bằng Esc, trả focus về chỗ cũ
- [ ] Điều hướng route đặt lại focus và đổi tiêu đề tài liệu
- [ ] Mọi cặp màu token đạt ngưỡng AA ở **cả hai** chế độ
- [ ] In màn hình đen trắng vẫn phân biệt được các trạng thái
- [ ] Thông báo lỗi của form được trình đọc màn hình đọc khi focus vào ô
- [ ] Toast được công bố cho công nghệ hỗ trợ, đúng mức khẩn cấp
- [ ] Thuộc tính ngôn ngữ của tài liệu đúng và đổi theo ngôn ngữ đang chọn

---

## 9. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Nhắm mức WCAG 2.2 AA | ✅ sẽ có | §1 |
| Bộ quy tắc tiếp cận trong lint | ✅ sẽ có | Pha F0. **Chưa có mã luật** ở [`../../RULES.md`](../../RULES.md) §7 — nợ ghi ở [`trien-khai/05-gate.md`](trien-khai/05-gate.md) §3.2 |
| Quản lý focus: route, hộp thoại, nội dung động | ✅ sẽ có | §3 |
| Kiểm tương phản ở tầng token, cả hai chế độ | ✅ sẽ có | §4.2, §4.3 — làm ở pha F1 |
| Checklist §2 cho mỗi màn mới | ✅ sẽ có | Dán vào mô tả PR |
| Kiểm tự động lúc chạy trong bộ test | ❌ chưa | Điều kiện: bộ test component đã ổn định — [`06-testing-strategy.md`](06-testing-strategy.md) |
| Kiểm định kỳ bằng trình đọc màn hình thật | ❌ chưa | Điều kiện: trước lần bàn giao đầu tiên cho người dùng ngoài đội |
| Nhắm mức AAA cho toàn ứng dụng | ❌ loại, không hoãn `K43` | §1 — một số tiêu chí AAA mâu thuẫn nhau; áp cho từng phần khi có lý do |
| Xoá viền focus mà không thay bằng thứ rõ hơn | ❌ loại, không hoãn `K44` | §3.4 — thay đổi CSS phá khả năng dùng bàn phím nhiều nhất |
| Dùng ARIA thay cho phần tử HTML gốc đã có sẵn | ❌ loại, không hoãn `K45` | §5 — ARIA sai tệ hơn không có ARIA |

Một finding dạng *"FE thiếu X"* chỉ hợp lệ khi X mang trạng thái **✅ sẽ có** mà vắng mặt, hoặc khi điều kiện ở cột ghi chú của một dòng **❌ chưa** đã xảy ra. Dòng **❌ loại, không hoãn** chỉ đổi được bằng một ADR mới, không đổi được bằng một finding.

---

## 10. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Token màu và tương phản | [`04-design-token-system.md`](04-design-token-system.md) |
| Tiếp cận của bảng dữ liệu | [`11-grid-and-metadata.md`](11-grid-and-metadata.md) §9 |
| Tiếp cận của biểu đồ | [`12-charting.md`](12-charting.md) §4 |
| Lỗi form và ARIA | [`09-forms-validation.md`](09-forms-validation.md) §5 |
| Thuộc tính ngôn ngữ tài liệu | [`08-i18n.md`](08-i18n.md) §7 |
| Đặc tả component | [`../../Design/COMPONENTS.md`](../../Design/COMPONENTS.md) |
