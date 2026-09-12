---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 11. Hiệu năng và cache

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Quy ước viết truy vấn, repository, chỉ mục cụ thể: [`../../quy-uoc/be-performance.md`](../../quy-uoc/be-performance.md). File này lo **thứ tự ưu tiên và lý do**.

---

## 1. Đo trước khi tối ưu

Trực giác về hiệu năng sai gần như luôn luôn. Một vòng lặp trông đáng ngờ thường tốn vài micro giây; một dòng gọi thuộc tính điều hướng trông vô hại có thể sinh ra hàng nghìn lượt gọi DB.

**Ba câu hỏi phải trả lời được trước khi sửa một dòng code:**

1. **Chậm bao nhiêu, đo ở đâu?** "Người dùng kêu chậm" chưa phải số đo. Cần: endpoint nào, phân vị 95 là bao nhiêu, với bộ dữ liệu cỡ nào.
2. **Thời gian đi đâu?** DB, ứng dụng, mạng, hay chờ một hệ ngoài? Bốn nguyên nhân này có bốn cách chữa hoàn toàn khác nhau.
3. **Ngưỡng chấp nhận được là bao nhiêu?** Không có ngưỡng thì tối ưu không có điểm dừng.

**Dùng phân vị, không dùng trung bình.** Trung bình bị kéo xuống bởi số đông request nhanh, và giấu đúng nhóm người dùng đang khổ. Xem [`07-observability.md`](07-observability.md) §7.

---

## 2. N+1, chỉ mục, phân trang, cache

> 📖 **Bốn chủ đề này có file chủ chung: [`../../quy-uoc/be-performance.md`](../../quy-uoc/be-performance.md).** N+1 ở §4 · chỉ mục §5 · phân trang offset và keyset §6 · cache §8.

Ở đó có phần khu này **không** giữ: ba khuôn sinh ra N+1 và cách phát hiện, bảy quy tắc đặt chỉ mục, ngưỡng chuyển từ offset sang keyset, ba điều kiện bắt buộc trước khi thêm bất kỳ cache nào.

Thứ tự bắt buộc *query pattern → index → đo lại → thuật toán → đo lại → cache* cũng khai ở đó (§1). Nhảy thẳng vào cache là sai lầm phổ biến nhất của mảng này: nó **che** triệu chứng, giữ nguyên nguyên nhân, và thêm một lớp có thể trả dữ liệu cũ.

---

## 3. Vài ngưỡng thực tế cho hệ tầm trung

Các con số dưới đây là **mốc để bắt đầu nghi ngờ**, không phải chuẩn:

| Quan sát | Nghĩa |
| --- | --- |
| Một endpoint đọc mất hơn vài trăm mili giây ở phân vị 95 | Đáng xem truy vấn |
| Một request phát ra hàng chục câu lệnh DB | Gần như chắc chắn có N+1 |
| Bảng vượt vài triệu dòng | Bắt đầu cần nghĩ về chỉ mục một phần và phân trang keyset |
| Số kết nối DB đang dùng chạm trần thường xuyên | Vấn đề ở giữ kết nối quá lâu, không phải ở kích thước bể kết nối |
| Bộ nhớ tăng đều không giảm | Rò rỉ, thường do cache trong bộ nhớ không có giới hạn |

Dòng áp chót đáng nhấn: tăng kích thước bể kết nối gần như luôn là cách chữa sai. Nguyên nhân thật thường là một truy vấn chậm hoặc một transaction mở quá lâu, và tăng bể chỉ làm DB nhận nhiều việc chậm hơn cùng lúc.

---

## 4. Kết nối và transaction — nguồn nghẽn hay bị bỏ qua

Ba cách hỏng dưới đây không nằm ở truy vấn nào cả, nên chúng không xuất hiện khi soi từng truy vấn một:

| Cách hỏng | Biểu hiện | Chữa |
| --- | --- | --- |
| **Transaction mở quá lâu** | Giữ kết nối, giữ khoá dòng, chặn người khác. Thường do gọi một hệ ngoài khi đang trong transaction | Không bao giờ gọi hệ ngoài trong transaction. Đây là lý do Outbox tồn tại — xem [`05-cross-module-consistency.md`](05-cross-module-consistency.md) |
| **Kết nối không được trả về** | Số kết nối đang dùng tăng đều, không giảm | Luôn giải phóng đúng cách; kiểm bằng chỉ số ở [`07-observability.md`](07-observability.md) |
| **Việc nặng chạy trong request** | Một request tốn hàng chục giây | Chuyển sang job nền — xem [`15-import-export.md`](15-import-export.md) §6 |

Dòng đầu là dòng đắt nhất, vì hậu quả của nó rơi lên **người khác**: người mở transaction lâu thì thấy bình thường, những người bị chặn mới thấy hệ thống chậm. Nên nguyên nhân và triệu chứng nằm ở hai chỗ khác nhau.

---

## 5. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Truy vấn đọc không theo dõi thay đổi, chỉ lấy cột cần | ✅ sẽ có | Quy ước ở [`../../quy-uoc/be-performance.md`](../../quy-uoc/be-performance.md) |
| Test đếm số câu lệnh cho endpoint danh sách | ✅ sẽ có | Biến N+1 thành lỗi hỏng build |
| Chỉ mục cho cột tham chiếu bằng định danh | ✅ sẽ có | PostgreSQL không tự tạo |
| Chỉ mục một phần cho bảng có xoá mềm | ✅ sẽ có | Xem [`10-data-retention.md`](10-data-retention.md) |
| Phân trang offset cho lưới quản trị | ✅ sẽ có | Đủ dùng, đơn giản hơn |
| Phân trang keyset cho xuất dữ liệu theo lô | ✅ sẽ có | |
| **Cache phân tán (Redis)** | ❌ chưa ở v1 | Điều kiện ở [`../../quy-uoc/be-performance.md`](../../quy-uoc/be-performance.md) §8.3 |
| **Cache trong bộ nhớ tiến trình** | ❌ chưa ở v1 | Sẽ tạo lệch giữa các instance ngay khi mở instance thứ hai |
| **Pipeline behavior cho cache** | ❌ **loại, không hoãn** `K04` | [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) §5.5 |
| **Kiểm thử tải tự động** | ❌ chưa | Đo thủ công trước; tự động hoá khi có ngưỡng cần bảo vệ |
| **Bản sao chỉ đọc của DB** | ❌ chưa | Chỉ cần khi tải đọc vượt khả năng một máy chủ. Xa hơn nhiều so với quy mô hiện tại |
