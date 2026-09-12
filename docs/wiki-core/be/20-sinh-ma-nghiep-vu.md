---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 20. Sinh mã nghiệp vụ

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> **Thành phần Nhóm B** ([`01-core-components.md`](01-core-components.md) §2) — ngưỡng bật: *nghiệp vụ đầu tiên cần mã do hệ thống sinh*. Bảng dữ liệu chỉ tạo khi thành phần được bật.

---

## 1. Bài toán, và cách làm sai kinh điển

Số phiếu, số văn bản, mã hồ sơ — thứ người dùng đọc và tra cứu bằng nó.

Cách sai phổ biến nhất: lấy giá trị lớn nhất đang có rồi cộng một. Nó chạy đúng suốt lúc thử một người, rồi **trùng số** ngay lần đầu hai người bấm lưu cùng lúc. Lỗi này không lộ trong test một luồng, và khi lộ thì đã có hai hồ sơ cùng số nằm trong dữ liệu thật.

---

## 2. Khuôn sinh mã nghiệp vụ — định nghĩa gốc

| Quyết định | Chốt | Vì sao |
| --- | --- | --- |
| Nguồn số | **Bảng đếm, cập nhật có khoá dòng và trả về giá trị mới trong cùng một câu lệnh** | Nghiệp vụ hành chính yêu cầu số **liên tục, không nhảy**. Bộ đếm sẵn có của cơ sở dữ liệu nhanh hơn nhưng để lại lỗ số khi một giao dịch bị huỷ |
| Khoá của bộ đếm | **(đơn vị, loại mã, kỳ)** | Mỗi đơn vị đếm riêng — đây là hệ nhiều đơn vị. Kỳ thường là năm, để số quay về 1 mỗi năm |
| Khuôn mã | Mẫu khai được, gồm tiền tố, kỳ và số thứ tự có độ dài cố định | Đổi khuôn là đổi cấu hình, không phải sửa code |
| Thời điểm sinh | **Trong cùng giao dịch** với bản ghi dùng mã đó | Sinh trước rồi mới hỏi người dùng xác nhận là cách chắc chắn tạo ra lỗ số |

---

## 3. Ba bẫy

| Bẫy | Hậu quả |
| --- | --- |
| **Sinh mã rồi mới mở hộp thoại xác nhận** | Người dùng bấm huỷ thì số đã tiêu. Sổ có lỗ, và người kiểm tra sẽ hỏi lỗ đó là gì |
| **Đọc bộ đếm rồi ghi lại bằng hai câu lệnh** | Hai phiên đọc cùng giá trị và ghi cùng giá trị. Phải là **một** câu lệnh cập nhật-và-trả-về, hoặc khoá dòng tường minh |
| **Dựng lại mã từ dữ liệu khi bộ đếm lệch** | Che lỗi thật. Bộ đếm lệch là sự cố cần ghi lại, không phải thứ tự chữa bằng cách đoán |

Ràng buộc kèm theo ở tầng dữ liệu: **mã phải có ràng buộc duy nhất theo đơn vị**, và ràng buộc đó là hàng rào cuối. Không có nó thì mọi lỗi ở trên đều im lặng.

---

## 4. Cái giá phải chấp nhận

Khoá dòng nghĩa là hai người tạo cùng loại phiếu, cùng đơn vị, cùng kỳ sẽ **xếp hàng** trong khoảnh khắc sinh mã. Ở quy mô một cơ quan thì không ai nhận ra; nếu một ngày nó thành điểm nghẽn đo được, lúc đó mới đổi sang bộ đếm của cơ sở dữ liệu và **chấp nhận số nhảy** — nhưng đổi là một quyết định nghiệp vụ, không phải một lần tối ưu kỹ thuật.

---

## 5. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Bảng đếm khoá theo (đơn vị, loại mã, kỳ) | ✅ khi bật | [`../../database/schema-core.md`](../../database/schema-core.md) §9.6 |
| Cập nhật-và-trả-về trong một câu lệnh | 📐 **bắt buộc** | §3 |
| Sinh mã trong cùng giao dịch với bản ghi | 📐 **bắt buộc** | §2 |
| Ràng buộc duy nhất theo đơn vị trên cột mã | 📐 **bắt buộc** | §3 |
| Khuôn mã khai bằng cấu hình | ✅ khi bật | [`19-cau-hinh-theo-don-vi.md`](19-cau-hinh-theo-don-vi.md) |
| **Đánh số lại hàng loạt** | ❌ loại | Mã đã phát ra ngoài thì không đổi. Sai thì huỷ bản ghi và ghi lý do |
