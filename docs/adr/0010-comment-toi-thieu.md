---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0010 — Comment trong code tối thiểu; lý do và lịch sử sự cố sống ở `docs/`

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

Ở dự án tiền nhiệm có những file mà **comment chiếm hơn sáu mươi phần trăm nội dung**, trong đó nhiều đoạn kể lại nguyên vẹn một sự cố: bẫy là gì, ngày nào nổ, hỏng ra sao, vá thế nào.

Đây là một tập quán có giá trị thật, và audit ghi nhận nó là một trong những điểm hiếm: **bài học không biến mất khi người viết rời đi**. Người sửa file đó ba tháng sau đọc được lý do của từng dòng phòng thủ, nên không gỡ nhầm.

Nhưng cùng file đó cũng bị ghi vào danh sách cần xử lý, vì hai lý do đo được:

- **Chi phí đọc cao cho cả người lẫn agent.** Đây chính là lớp vấn đề đã làm cạn context của agent review: một lượt review chỉ phần backend từng tiêu tới bốn trăm nghìn token, và bộ tri thức đầy đủ đã giết ba lượt review liên tiếp trước khi họ chuyển sang cách trỏ đường thay vì chép nội dung.
- **Không cổng nào kiểm comment**, nên chúng tự trôi. Một comment mô tả hành vi đã đổi vẫn nằm nguyên đó, và nó không sai một cách ồn ào — nó sai một cách đáng tin.

Với CoreAndSkill, chi phí đọc còn quan trọng hơn: bộ khung này được thiết kế để **agent làm việc trên nó**, và context là tài nguyên hữu hạn.

## Quyết định

- **Comment trong code tối thiểu.** Chỉ giữ comment trả lời câu hỏi *vì sao dòng này tồn tại* mà tên hàm và tên biến không trả lời được — ví dụ một cách làm vòng vèo để né một giới hạn thật của thư viện.
- **Không kể lịch sử sự cố trong code.** Lịch sử sự cố sống ở [`../audit/`](../audit/); lý do của một quyết định kiến trúc sống ở khu này.
- **Code chỉ được để lại đúng một dòng trỏ tới file audit**, không kể lại nội dung. Đây cũng là hình dạng bắt buộc mô tả ở [`../audit/README.md`](../audit/README.md).
- Chú thích trỏ tới tài liệu phải trỏ vào tài liệu **còn sống**: luật D10 ở [`../RULES.md`](../RULES.md) cấm trỏ vào file mang nhãn lịch sử.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ mật độ comment cao như dự án tiền nhiệm

**Được:** bài học nằm ngay cạnh dòng code nó bảo vệ. Người sửa không cần biết có khu tài liệu nào tồn tại vẫn đọc được lý do. Khoảng cách giữa lý do và code bằng không, nên không có chuyện quên cập nhật một trong hai vì chúng ở cùng chỗ.

**Vì sao loại:** chi phí đọc trải lên **mọi** lượt đọc file, còn lợi ích chỉ hiện ra ở lượt hiếm khi có người định gỡ đúng dòng phòng thủ đó. Với một bộ khung mà agent phải nạp cả file vào context, tỷ lệ đó đổi hẳn cán cân. Và lập luận "ở cùng chỗ nên không lệch" không đứng vững trong thực tế: comment vẫn lệch, chỉ là không ai phát hiện vì không cổng nào kiểm.

### Phương án B — Comment tối thiểu, không có nơi lưu lý do

**Được:** code sạch nhất; không phải bảo trì khu tài liệu nào.

**Vì sao loại:** bài học mất hẳn. Đây là phương án tệ nhất trong ba, vì nó chỉ lấy phần rẻ của quyết định mà bỏ phần có giá trị. Sáu tháng sau sẽ có người gỡ một dòng phòng thủ trông như thừa, và sự cố cũ quay lại — lần này không còn manh mối nào.

Chính vì loại phương án này mà khu [`../audit/`](../audit/) là **điều kiện đi kèm bắt buộc**, không phải phần thêm cho đẹp. Quyết định này chỉ đúng khi khu đó tồn tại và được viết đều.

### Phương án C — Comment dày nhưng có cổng kiểm độ tươi

**Được:** giữ được cả hai mặt trên lý thuyết.

**Vì sao loại:** không có cách máy nào kiểm được một câu văn xuôi còn mô tả đúng hành vi hiện tại hay không. Cổng dựng ra sẽ chỉ kiểm được hình thức — độ dài, có ngày tháng hay không — mà hình thức đúng thì không nói gì về nội dung đúng. Đây đúng loại lỗi mà [`../README.md`](../README.md) cảnh báo: cổng xanh không có nghĩa nội dung đúng.

## Hệ quả

### Tích cực

- **Chi phí đọc một file giảm mạnh** cho cả người lẫn agent, và context còn lại dùng để đọc thêm code thay vì đọc lại lịch sử.
- **Bài học có địa chỉ tra cứu**, tìm được bằng lệnh, và không phụ thuộc ai đó tình cờ mở đúng file.
- Một sự cố chạm nhiều file nay có **một** bản ghi, thay vì bị kể lại từng mảnh ở từng file.

### Tiêu cực — cái giá thật

- **Khoảng cách giữa lý do và code trở thành thật.** Người sửa code phải chủ động mở file audit mới biết vì sao dòng phòng thủ đó tồn tại. Nếu họ không mở, họ ở tình trạng của phương án B — và không có gì bắt buộc họ mở, ngoài dòng trỏ đường.
- **Dòng trỏ đường có thể chết.** File audit đổi tên hoặc bị xoá thì dòng trỏ trong code thành vô nghĩa. Cổng tài liệu kiểm được đường dẫn có tồn tại, nhưng phải nhớ chạy nó.
- **Khu audit phải được viết đều mới có tác dụng.** Nếu một sự cố xảy ra mà không ai ghi lại, quyết định này đã xoá comment và không thay bằng gì. Rủi ro này là thật và không có cổng nào bắt được — không cổng nào biết một sự cố vừa xảy ra.
- **Ranh giới "comment nào được giữ" là phán đoán, không phải luật.** Sẽ có tranh luận, và sẽ có người cắt quá tay, xoá cả comment thật sự cần. Tiêu chí *vì sao chứ không phải cái gì* thu hẹp vùng xám nhưng không xoá được nó.

## Liên quan

- [`../audit/README.md`](../audit/README.md) — khuôn bắt buộc của một mục audit, và luật một dòng trỏ đường
- [`../RULES.md`](../RULES.md) — luật D10
- [`../quy-uoc/tieu-chi-review.md`](../quy-uoc/tieu-chi-review.md) — comment thừa có phải finding không
