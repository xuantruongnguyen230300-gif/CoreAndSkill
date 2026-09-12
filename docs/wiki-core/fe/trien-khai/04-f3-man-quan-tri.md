---
kind: luat
scope: core
verified: chua-doi-chieu
---

# F3 — Hai màn quản trị Core

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**
>
> **Phạm vi F3 gồm bốn màn**: quản trị người dùng, phân quyền, **quản trị vai trò** ([`../../../contracts/roles.md`](../../../contracts/roles.md)) và **hồ sơ cá nhân** ([`../../../contracts/profile.md`](../../../contracts/profile.md)). Hai màn sau nhỏ hơn nhiều và dùng lại đúng khuôn lưới/form của hai màn đầu.
>
> **Định nghĩa hoàn thành:** màn quản trị người dùng chạy đủ tạo / sửa / khoá / mở khoá / phân trang / tìm kiếm qua HTTP thật; màn phân quyền đọc và lưu được ma trận quyền; tài khoản **thiếu permission quản trị quyền** bị chặn khỏi màn phân quyền kể cả khi gõ thẳng URL; mọi lỗi validation từ BE bind **đúng từng ô nhập**.

Đây là hai màn Core cuối cùng. Xong F3 là `platform/` đủ bộ màn Core, và nền tảng dùng lại được cho sản phẩm khác.

---

## 1. Phạm vi

| Màn | Quyền yêu cầu | Hợp đồng |
| --- | --- | --- |
| Quản trị người dùng | Permission quản trị người dùng | [`../../../contracts/users.md`](../../../contracts/users.md) |
| Phân quyền | Permission quản trị **quyền** — chặt hơn | [`../../../contracts/permissions.md`](../../../contracts/permissions.md) |

> 📖 **Bố cục bắt buộc của màn danh sách**: [`../../../Design/Templates/ListScreen.md`](../../../Design/Templates/ListScreen.md) · Component ghép nên màn: [`../../../Design/COMPONENTS.md`](../../../Design/COMPONENTS.md) · Giá trị token: [`../../../Design/DESIGN.md`](../../../Design/DESIGN.md).

**Hai màn này là nơi bốn hạ tầng dùng chung ra đời**, sinh ra từ nhu cầu thật thay vì từ suy đoán: bảng dữ liệu server-side, hộp thoại form, xác nhận thao tác, và hạ tầng bind lỗi theo ô.

---

## 2. Thứ tự viết

```
1. Data grid dùng chung (shared/components/data-grid)
        │
        ▼
2. Màn danh sách người dùng — chỉ đọc trước: phân trang, sắp xếp, tìm kiếm, state trên URL
        │
        ▼
3. Hộp thoại form + hạ tầng bind fieldErrors
        │
        ▼
4. Tạo / sửa / khoá / mở khoá
        │
        ▼
5. Màn phân quyền (ma trận)
        │
        ▼
6. Bật ngân sách bundle theo số đo thật
```

**Bước 1 và 2 làm phần đọc trước phần ghi.** Phân trang, sắp xếp, lọc và trạng thái trên URL là phần dễ sai nhất và ảnh hưởng tới mọi màn danh sách sau này; làm xong và kiểm kỹ trước khi thêm phần ghi thì lỗi không lẫn vào nhau.

> 📖 Bảng dữ liệu: [`../11-grid-and-metadata.md`](../11-grid-and-metadata.md) · Form và lỗi theo ô: [`../09-forms-validation.md`](../09-forms-validation.md) · Component dùng chung: [`../05-component-library.md`](../05-component-library.md).

---

## 3. Năm thứ dễ làm sai

### 3.1 Chặn màn phân quyền bằng UI thay vì bằng guard

Ẩn mục khỏi menu **không** thay được guard: người gõ thẳng URL vẫn phải bị chặn. Và guard ở FE **không** thay được kiểm tra ở BE — endpoint phân quyền phải tự chặn độc lập ([`../14-security.md`](../14-security.md) §2).

Ba lớp này bổ sung nhau, không thay thế nhau. Bỏ lớp nào cũng để lại một đường đi.

### 3.2 Khoá tài khoản không có hiệu lực tức thì

Nếu phiên có thời gian sống, tài khoản bị khoá vẫn dùng được tới khi phiên được kiểm lại. **Câu chữ phải phản ánh đúng độ trễ đó.**

Viết "Đã đăng xuất người dùng" khi thực tế là "sẽ chấm dứt trong ít phút" khiến quản trị viên tưởng thao tác hỏng và bấm lại nhiều lần. Độ trễ thật khai ở hợp đồng người dùng.

### 3.3 Chờ một trường không tồn tại trong kết quả phân trang

Server trả tổng số bản ghi; số trang do FE tự tính ([`../11-grid-and-metadata.md`](../11-grid-and-metadata.md) §2.2). Chờ một trường số trang có sẵn cho `undefined`, phép tính ra một giá trị không phải số, và thanh phân trang hiển thị trống — **không** báo lỗi gì.

### 3.4 Khoá `fieldErrors` không khớp tên control

Thông báo lỗi biến mất hoàn toàn: không vào ô nào, không vào thông báo chung. Người dùng bấm Lưu, không có gì xảy ra.

Bắt buộc: khoá không khớp phải được gộp vào thông báo chung và ghi log ở môi trường phát triển ([`../09-forms-validation.md`](../09-forms-validation.md) §4.3).

### 3.5 Ma trận quyền lưu nửa vời

Màn phân quyền thường là một lưới nhiều ô tích. Ba câu hỏi phải trả lời trước khi viết, vì mỗi câu dẫn tới một thiết kế khác:

| Câu hỏi | Nếu chọn sai |
| --- | --- |
| Lưu **toàn bộ** ma trận hay chỉ phần đã đổi? | Gửi toàn bộ thì hai người sửa cùng lúc sẽ ghi đè nhau im lặng |
| Có xác nhận trước khi lưu không? | Một cú bấm nhầm trên lưới quyền có hậu quả rộng |
| Sau khi lưu có tải lại từ server không? | Không tải lại thì màn hình hiển thị thứ người dùng vừa chọn, chưa chắc là thứ server đã ghi |

Ở repo này: **tải lại từ server sau khi lưu**, và **xác nhận** nếu thao tác gỡ quyền. Đây là quyết định mặc định, dự án có thể lệch nhưng phải nêu lý do.

---

## 4. Bật ngân sách bundle — theo số đo, không giữ mặc định

F3 là lúc app đủ lớn để một con số ngân sách có nghĩa.

Quy trình bắt buộc ([`../13-performance.md`](../13-performance.md) §4.2): **đo trước**, đặt ngưỡng cảnh báo có dư địa thật, đặt ngưỡng lỗi ở mốc thật sự không chấp nhận được, và **ghi lại số đo cùng lý do ngay cạnh chỗ khai ngưỡng**.

Đừng giữ nguyên ngưỡng mặc định của công cụ tạo dự án. Con số đó không biết gì về ứng dụng này, và ở dự án tiền nhiệm nó đã mất tác dụng cảnh báo từ rất lâu trước khi có người phát hiện.

**Lỗ mù phải xử lý ngay ở F3:** ngân sách thường chỉ khai cho phần khởi động, **lazy chunk không bị ràng buộc gì**. F3 tạo ra lazy chunk đầu tiên, nên đây đúng là lúc khai ngân sách cho chúng — hoặc ghi lỗ mù ra ở [`05-gate.md`](05-gate.md) nếu không khai được.

---

## 5. Nghiệm thu F3

**Phân quyền và chặn**

- [ ] Tài khoản thiếu permission quản trị quyền gõ thẳng URL màn phân quyền → bị chặn, **không** thấy nội dung dù chớp nhoáng
- [ ] Gọi thẳng endpoint phân quyền bằng công cụ ngoài với tài khoản đó → BE từ chối (kiểm rằng FE không phải lớp bảo vệ duy nhất)

**Danh sách**

- [ ] Sang trang 2 rồi đổi bộ lọc → **về trang 1**
- [ ] Đổi trang, sắp xếp, lọc → URL đổi; mở URL đó ở tab khác cho **đúng** màn hình đó
- [ ] Bấm Quay lại của trình duyệt → về đúng trạng thái trước, không mất bộ lọc
- [ ] Gõ nhanh vào ô tìm kiếm → chỉ kết quả cuối cùng có hiệu lực (không về sai thứ tự)
- [ ] Trạng thái rỗng do chưa có dữ liệu và rỗng do bộ lọc dùng **hai câu khác nhau**
- [ ] Đổi trang → dữ liệu cũ mờ đi, bảng **không nhảy**

**Form**

- [ ] Tạo người dùng với dữ liệu sai → lỗi hiện **trên từng ô**, không chỉ một thông báo chung
- [ ] Một khoá lỗi không khớp control → vẫn hiện ở thông báo chung, **không biến mất**
- [ ] Sửa ô đang lỗi → lỗi server biến mất, lỗi client còn nguyên nếu vẫn sai
- [ ] Bấm Lưu hai lần nhanh → **một** bản ghi được tạo
- [ ] Sửa form rồi bấm menu khác → có hỏi; sau khi lưu rồi rời đi → **không** hỏi

**Thao tác và câu chữ**

- [ ] Câu chữ thao tác khoá tài khoản **không** hứa hiệu lực tức thì
- [ ] Thao tác hàng loạt hỏi xác nhận có **số lượng** trong câu
- [ ] Lưu ma trận quyền → tải lại trang → giá trị đúng như vừa lưu

**Cổng**

- [ ] Ngân sách bundle đặt theo số đo, có ghi lại số đo và lý do
- [ ] Có ngân sách cho lazy chunk, hoặc lỗ mù được ghi ra ở [`05-gate.md`](05-gate.md)
- [ ] Toàn bộ cổng FE chạy xanh — xem [`05-gate.md`](05-gate.md), **không** chỉ chạy script

---

## 6. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Bảng dữ liệu server-side | [`../11-grid-and-metadata.md`](../11-grid-and-metadata.md) |
| Form và bind lỗi theo ô | [`../09-forms-validation.md`](../09-forms-validation.md) |
| Component dùng chung | [`../05-component-library.md`](../05-component-library.md) |
| Hợp đồng endpoint | [`../../../contracts/users.md`](../../../contracts/users.md) · [`../../../contracts/permissions.md`](../../../contracts/permissions.md) |
| Ngân sách bundle | [`../13-performance.md`](../13-performance.md) §4 |
| Toàn bộ cổng | [`05-gate.md`](05-gate.md) |
