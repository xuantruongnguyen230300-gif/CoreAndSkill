---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0016 — Dự án hạ nguồn lấy Core bằng CLONE, phiên bản bằng TAG

> **Trạng thái:** Đã chấp nhận (2026-09-10)

## Bối cảnh

Mục tiêu tồn tại của repo này là **một Core dùng lại được cho nhiều dự án**. Nhưng chưa quyết định nào nói dự án thứ hai *lấy Core về bằng cách nào*, và *làm sao biết mình đang chạy bản Core nào*.

Khoảng trống đó không vô hại: khi không có đường chính thức, cách rẻ nhất luôn là **chép thư mục**. Chép thư mục thì mỗi dự án thành một Core riêng sau vài tháng, và một bản vá bảo mật phải sửa lại ở từng nơi — đúng bệnh mà [`0002-core-5-project.md`](0002-core-5-project.md) trích dẫn để bác bỏ.

## Quyết định

> **Mỗi dự án là một bản `clone` đầy đủ của repo Core, phát triển tiếp trên chính bản đó. Phiên bản Core đánh dấu bằng `tag` trên repo Core; dự án ghi tag mình đang dùng vào một tệp ở gốc repo.**

Bốn luật đi kèm, và luật đầu là luật giữ cho ba luật kia có nghĩa:

| # | Luật | Vì sao |
| --- | --- | --- |
| 1 | **Dự án KHÔNG sửa tệp thuộc `Core/`** | Đây là điều kiện sống còn của mô hình. Sửa Core trong bản clone thì lần kéo bản vá kế tiếp là xung đột hàng chục tệp, và người ta sẽ bỏ luôn việc kéo. Cần đổi hành vi thì đi qua seam đã có; seam chưa có thì nâng thay đổi lên Core rồi kéo về |
| 2 | **Tệp `CORE_VERSION` ở gốc repo dự án** ghi tag và mã commit của Core đang dùng | Không có nó thì câu hỏi *"dự án này thiếu bản vá nào"* không ai trả lời được |
| 3 | **Kéo bản vá bằng `fetch` + `merge` theo tag**, không chép tệp | Chép tệp làm mất lịch sử, nên lần sau vẫn phải chép tiếp |
| 4 | **Repo Core có `CHANGELOG.md`**, mỗi thay đổi một dòng, thay đổi phá vỡ kèm mục *"dự án phải làm gì"* | Người kéo bản mới về cần biết phải sửa gì, trước khi merge chứ không phải sau |

Quy ước thao tác và khuôn `CORE_VERSION`: [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Đóng gói Core thành package (NuGet cho BE, npm cho FE)

**Được:** ranh giới cứng do trình quản lý gói ép; nâng cấp là đổi một dòng phiên bản; không có chuyện dự án sửa Core vì Core nằm trong thư mục gói.

**Vì sao loại:** nó đòi một máy chủ gói nội bộ phải cài, cấu hình, sao lưu và cấp quyền — một hạ tầng nữa cho một đội đang có một sản phẩm. Và phần FE của Core không đóng gói gọn được: nó gồm cả `Design/`, tệp dịch, cấu hình build. Đây là quyết định **có thể lật về sau** khi số dự án đủ nhiều; lúc đó cái giá hạ tầng mới tương xứng.

### Phương án B — Submodule git

**Vì sao loại:** submodule chuyển bài toán phiên bản thành một con trỏ commit mà công cụ nào cũng xử lý hơi khác nhau, và người mới clone quên `--recursive` là nhận về một thư mục rỗng — hỏng theo kiểu im lặng. Nó cũng không giải quyết được việc dự án cần **sửa** một chỗ trong Core, vì submodule là chỉ đọc trên thực tế.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| Luật "không sửa `Core/`" **không có cổng ép** ở giai đoạn 1 | Nó dựa vào kỷ luật và review. Cổng khả thi khi có `src/`: so thư mục `Core/` của dự án với tag đã ghi trong `CORE_VERSION`, khác là đỏ |
| Mỗi dự án gánh **toàn bộ** lịch sử và tài liệu của Core | Chấp nhận: đổi lại là dự án luôn có đủ luật và ADR ngay tại chỗ, không phải tra ở repo khác |
| Bản vá Core lan chậm | Nó tới dự án khi có người kéo, không tự động. `CHANGELOG.md` là thứ duy nhất báo rằng có gì để kéo |

### Tích cực

- Không cần hạ tầng nào ngoài git.
- Dự án sửa được Core khi thật sự cần — nhưng phải làm ở repo Core rồi kéo về, nên thay đổi đó tới được mọi dự án khác.
- Tài liệu và code đi cùng nhau, nên luật không bao giờ nằm ở một repo mà code nằm ở repo khác.

### Điều kiện lật quyết định

Từ **bốn dự án** trở lên dùng chung Core, hoặc khi việc kéo bản vá thủ công bắt đầu bị bỏ qua thấy rõ. Khi đó đóng gói (phương án A) mới đáng giá hạ tầng của nó.

## Liên quan

- [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15 — thao tác cụ thể và khuôn `CORE_VERSION`
- [`0002-core-5-project.md`](0002-core-5-project.md) — vì sao Core tự đứng được là điều kiện của mô hình này
- [`0012-giai-doan-1-chi-docs.md`](0012-giai-doan-1-chi-docs.md) — vì sao giai đoạn 1 chưa có `src/`
