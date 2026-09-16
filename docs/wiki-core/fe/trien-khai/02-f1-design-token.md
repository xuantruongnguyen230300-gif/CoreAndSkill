---
kind: luat
scope: core
verified: chua-doi-chieu
---

# F1 — Design token vào code

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**
>
> **Định nghĩa hoàn thành:** stylesheet toàn cục chứa đủ bộ token lấy từ [`../../../Design/DESIGN.md`](../../../Design/DESIGN.md); tìm màu literal trong SCSS của `src/app` **không còn kết quả nào**; thư viện UI render đúng màu token trên ít nhất một nút và một bảng mẫu; font khai trong `Design/` **thật sự được nạp**; chế độ tối chạy đúng ở cả ba trạng thái, và mặc định là sáng.

---

## 1. Chiều đồng bộ — giống hệt quy tắc thường ngày

`Design/` là nguồn, code đuổi theo. F1 đi **đúng** chiều đó — không phải ngoại lệ của giai đoạn dựng, không có chiều riêng cho pha này.

| Nguồn | Đích |
| --- | --- |
| [`../../../Design/DESIGN.md`](../../../Design/DESIGN.md) và các tệp token đi kèm | Tệp khai token của stylesheet toàn cục |

> 📖 Cơ chế đầy đủ, hai tầng token, quy tắc đặt tên: [`../04-design-token-system.md`](../04-design-token-system.md).

**Hệ quả phải nhớ:** giá trị màu tồn tại ở **đúng một** nơi phía code — tệp khai token. Preset của thư viện UI trỏ tên biến `var(--color-*)`, không chép mã màu ([`../04-design-token-system.md`](../04-design-token-system.md) §7). Một mã màu gõ tay vào preset là bản sao thứ hai, và từ lúc đó CSS của mình với component của thư viện có thể render hai màu khác nhau **mà không có gì báo lỗi**.

**Không lấy giá trị từ một dự án khác.** Bảng màu của dự án tiền nhiệm mang những nợ đã biết — trong đó có cặp màu cảnh báo và màu lỗi từng đo được **dưới ngưỡng tương phản AA**. Lấy từ `Design/` là món nợ đó không bao giờ tồn tại; lấy từ code cũ là chép nguyên nó sang.

---

## 2. Năm việc của F1

### 2.1 Đổ token vào stylesheet toàn cục

Dùng **đúng tên khoá** đã có trong `Design/`. Không đặt tên mới song song cho cùng một màu — hai tên cho một giá trị là cách một hệ token bắt đầu chết.

Thấy trong code một giá trị chưa có tên trong `Design/` thì **dừng lại và hỏi**, đừng tự đặt tên.

### 2.2 Khai chế độ tối

Ba trạng thái, không phải hai: sáng, tối, và theo hệ điều hành. **Mặc định là sáng** ([`../../../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md`](../../../adr/0018-thang-trung-tinh-sang-va-theme-mac-dinh.md)). Cơ chế và bẫy: [`../04-design-token-system.md`](../04-design-token-system.md) §4.

Ba thứ dễ quên:

| Quên gì | Triệu chứng |
| --- | --- |
| Áp giá trị sáng khi chưa có lựa chọn nào được lưu | Người mở lần đầu trên máy đang để chế độ tối nhận giao diện tối — trái mặc định đã chốt |
| Điều kiện loại trừ khi người dùng đã chọn "sáng" | Người chọn sáng trên máy đang để chế độ tối vẫn nhận giao diện tối |
| Đặt thuộc tính chế độ **trước khi khung hình đầu vẽ** | Một nháy trắng trước khi chuyển sang tối, ở mỗi lần tải trang |

### 2.3 Nạp font thật

Font phải được **tự phục vụ** bằng khai báo trong stylesheet, trỏ tới đường dẫn tuyệt đối của tài nguyên tĩnh.

**Không** nhúng bằng thẻ liên kết trong tài liệu HTML gốc với đường dẫn tương đối: công cụ build xử lý tài liệu đó và phân giải đường dẫn theo cấu hình build, nên bản chạy thật sẽ **im lặng** dùng font hệ điều hành trong khi máy dev vẫn đúng.

Kiểm bằng công cụ dev: giá trị `font-family` đã tính toán phải thật sự phân giải ra font đã khai, không phải font dự phòng.

### 2.4 Đủ trạng thái cho component dùng chung

Mỗi component trong `shared/` phải có `:hover`, `:focus-visible` và trạng thái vô hiệu hoá được định nghĩa thật, giá trị lấy từ token đã có — **không phát minh màu mới**.

`:focus-visible` là bắt buộc, không phải tuỳ chọn: bỏ nó nghĩa là người dùng bàn phím không biết mình đang ở đâu ([`../15-accessibility.md`](../15-accessibility.md) §3.4). Định nghĩa nó **một lần** ở tầng token, không để từng component tự quyết.

### 2.5 Preset cho thư viện UI

Dựng preset ánh xạ token của mình vào hệ theming của thư viện, đăng ký một lần lúc cấu hình app, và **không** để theme mặc định của thư viện chạy song song. Cơ chế — tầng nào ghi đè, tắt chế độ tối riêng của thư viện, thứ tự lớp CSS — và giới hạn "cú pháp chốt khi thi công theo tài liệu PrimeNG chính thức": [`../04-design-token-system.md`](../04-design-token-system.md) §7.

Để hai hệ màu cùng tồn tại thì không ai biết chỗ nào thắng — triệu chứng là "sửa token mà nút không đổi màu", và người ta sẽ đi tìm lỗi ở chỗ khác rất lâu.

---

## 3. Bật cổng màu ngay khi token vừa có

F1 là thời điểm luật F6 và F7 ([`../../../RULES.md`](../../../RULES.md) §7) bắt đầu có nghĩa: trước khi có token thì "không dùng màu literal" là một luật không thi hành được.

Bật ngay, không đợi cuối pha. Lý do rất cụ thể: ở dự án tiền nhiệm, luật này được dọn tay **hai lần** và tự tái sinh **cả hai lần** — màu literal mới xuất hiện ngay ở đợt màn hình kế tiếp, đúng vì không có máy kiểm.

Hai điều phải làm đúng khi bật:

| Điều | Chi tiết |
| --- | --- |
| Nơi khai token **không** bị quét | Ở đó literal là **định nghĩa**, không phải bản sao |
| Miễn trừ theo **cú pháp**, không theo **giá trị** | [`../04-design-token-system.md`](../04-design-token-system.md) §6.2 — đây là chỗ dễ nhân nhượng nhất và nhân nhượng là mở lại đúng cánh cửa cổng sinh ra để đóng |

---

## 4. Kiểm tương phản ngay ở F1

Vì mọi màu đến từ token, **mọi cặp màu có thể xuất hiện đều biết trước** — nên tương phản kiểm được một lần ở đây, thay vì kiểm lại ở từng màn về sau.

Kiểm cả hai chế độ sáng và tối. Một cặp đạt ngưỡng ở chế độ sáng không bảo đảm gì ở chế độ tối. Ngưỡng và cách đo: [`../15-accessibility.md`](../15-accessibility.md) §4.

Làm ở F1 rẻ hơn nhiều so với làm sau: sửa một token là sửa một dòng; phát hiện ở F3 thì phải rà lại mọi màn đã dựng bằng giá trị cũ.

---

## 5. Ba thứ hay bị làm sai ở F1

**(1) Đặt tên token theo giá trị thay vì theo vai trò.** Một tên mang màu sẽ thành lời nói dối ngay ở sản phẩm thứ hai. [`../04-design-token-system.md`](../04-design-token-system.md) §3.1.

**(2) Bỏ qua bóng đổ khi làm chế độ tối.** Bóng đen trên nền tối gần như vô hình. Bóng phải là token có giá trị riêng cho mỗi chế độ, không chỉ đổi màu chữ và nền.

**(3) Dùng thang chữ tự do.** Cho phép cỡ chữ tuỳ ý là mất nhịp thị giác ở mọi màn, và không ai chỉ ra được chỗ nào sai — chỉ thấy "trông hơi lệch".

---

## 6. Bốn họ token, không chỉ màu

F1 hay bị thu hẹp thành "đổ bảng màu vào code". Ba họ còn lại quan trọng không kém và bị bỏ quên nhiều hơn.

| Họ | Phải khai gì | Bỏ quên thì sao |
| --- | --- | --- |
| **Màu** | Token ngữ nghĩa cho nền, chữ, viền, thương hiệu, trạng thái | Đã bàn ở §2 |
| **Chữ** | Họ chữ, **thang cỡ rời rạc**, độ đậm, **chiều cao dòng** | Cỡ chữ tuỳ ý ở mỗi màn; và chiều cao dòng — thứ quyết định cảm giác dày/thưa của cả trang — bị mỗi component tự chọn |
| **Khoảng cách** | Thang nhân từ một đơn vị gốc | Khoảng cách lẻ trộn vào thang làm thang mất nghĩa; giao diện "trông hơi lệch" ở mọi chỗ |
| **Bóng và bo góc** | Lớp độ cao đặt tên theo vai trò | Bóng đổ không đổi theo chế độ tối; bo góc mỗi nơi một giá trị |

**Chiều cao dòng và thang khoảng cách là hai thứ bị bỏ quên nhiều nhất**, và cũng là hai thứ mà thiếu chúng thì không ai chỉ ra được chỗ nào sai — chỉ có cảm giác chung là giao diện chưa chỉn chu.

Quy tắc đặt tên cho cả bốn họ giống nhau: **theo vai trò, không theo giá trị, không theo vị trí duy nhất** ([`../04-design-token-system.md`](../04-design-token-system.md) §3.1).

---

## 7. Nghiệm thu F1

- [ ] Mọi token trong `Design/` có mặt trong stylesheet toàn cục, **đúng tên**
- [ ] Không token nào được thêm mà chưa có tên trong `Design/`
- [ ] Tìm hex trong SCSS của `src/app` → **không kết quả** (luật F6)
- [ ] Tìm `rgb(`/`rgba(` trong SCSS của `core/`, `shared/`, `platform/` → chỉ còn dạng đọc token pha alpha (luật F7)
- [ ] Đổi một token màu thương hiệu → **cả** CSS của mình lẫn component thư viện đổi theo
- [ ] Mở lần đầu, chưa từng chọn theme, trên máy đang để chế độ tối → giao diện **sáng**
- [ ] Chọn "theo hệ điều hành" trên máy đang để chế độ tối → không mảng nào còn nền sáng
- [ ] Tìm mã màu trong `core/theme/` → không kết quả; preset chỉ trỏ `var(--color-*)`
- [ ] Chọn "sáng" tường minh khi hệ điều hành đang tối → giao diện sáng thật
- [ ] Tải lại trang ở chế độ tối → **không có nháy trắng**
- [ ] `font-family` đã tính toán phân giải ra đúng font đã khai
- [ ] Tab bằng bàn phím qua một màn mẫu → thấy rõ trạng thái focus ở mọi control
- [ ] Mọi cặp màu token đạt ngưỡng tương phản AA ở **cả hai** chế độ
- [ ] Một nút và một bảng của thư viện UI render đúng màu token

---

## 8. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Cơ chế token đầy đủ | [`../04-design-token-system.md`](../04-design-token-system.md) |
| Giá trị token | [`../../../Design/DESIGN.md`](../../../Design/DESIGN.md) |
| Bọc thư viện UI | [`../05-component-library.md`](../05-component-library.md) §2 |
| Tương phản và tiếp cận | [`../15-accessibility.md`](../15-accessibility.md) |
| Cổng F6, F7 | [`05-gate.md`](05-gate.md) |
| Pha kế tiếp | [`03-f2-auth-routing.md`](03-f2-auth-routing.md) |
