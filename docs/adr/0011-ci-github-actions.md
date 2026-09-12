---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0011 — Có CI chạy đủ cổng trên mỗi PR

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

Dự án tiền nhiệm **cố ý không có CI**. Luật repo của họ ghi thẳng điều đó, kèm hệ quả được nêu ngay cạnh: *không còn máy nào chạy hộ*.

Hệ quả đó đã xảy ra thật. Ngày 2026-08-23, người ta phát hiện **script cổng frontend không tồn tại trên đĩa** — trong khi tài liệu vẫn hướng dẫn chạy nó và vẫn mô tả các mục cổng như đang hoạt động. Ba mục cổng đã không có gì canh suốt một thời gian dài, và không ai biết.

Chi tiết ở [`../audit/2026-08-23-cong-khong-ton-tai.md`](../audit/2026-08-23-cong-khong-ton-tai.md).

Lựa chọn không CI của họ có lý trong bối cảnh của họ: một repo cá nhân, một người làm, mọi lệnh chạy trên máy mình. Nhưng CoreAndSkill có bối cảnh khác — nó là **bộ khung nhiều dự án dùng chung**, nghĩa là một lỗi lọt qua cổng ở đây sẽ được nhân bản sang mọi dự án dựng trên nó.

## Quyết định

Mỗi PR phải chạy qua CI, và CI chạy **đủ** các cổng, không phải một phần:

- cổng tài liệu,
- cổng frontend cùng các lệnh kiểm và build của Angular,
- toàn bộ test backend, bao gồm test kiến trúc và integration test trên PostgreSQL thật.

Ba ràng buộc kèm theo, mỗi cái sinh ra từ một cách hỏng đã biết:

| Ràng buộc | Chống cách hỏng nào |
| --- | --- |
| **Fail-fast:** một lệnh trong chuỗi gãy thì cả chuỗi dừng và báo đỏ | Ở dự án tiền nhiệm, lệnh gãy ngay từ đầu nhưng các lệnh sau vẫn chạy, nên người chạy tưởng đã qua |
| **Mọi lệnh của cổng nằm trong danh sách cho phép của harness** | Một cổng bị hỏi lại giữa chừng là cổng trên thực tế không ai chạy |
| **Mọi detector phải có test kiểm chính nó** — luật T1 ở [`../RULES.md`](../RULES.md) | Cổng chạy nhưng không phát hiện được gì cũng là cổng không tồn tại, chỉ khó thấy hơn |

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Không CI, chạy cổng bằng tay như dự án tiền nhiệm

**Được:** không phải bảo trì cấu hình pipeline; không phụ thuộc dịch vụ ngoài; không tốn phút chạy; phản hồi ngay trên máy, không phải chờ hàng đợi.

**Vì sao loại:** cổng chạy tay là cổng **có thể bị bỏ** — và việc bỏ nó không làm hỏng gì ngay, nên không có tín hiệu nào báo. Với repo cá nhân, rủi ro đó chấp nhận được vì phạm vi thiệt hại nhỏ. Với bộ khung dùng chung thì không: một luật ranh giới bị vi phạm ở đây sẽ đi theo mọi dự án sinh ra từ nó.

Còn một điểm nữa, nặng hơn: không CI nghĩa là **không có log nào để đối chiếu**. Câu hỏi *"cổng này lần cuối chạy là bao giờ, và nó nói gì"* không trả lời được. Sự cố ngày 2026-08-23 tồn tại lâu như vậy chính vì không ai trả lời được câu đó.

### Phương án B — Hook cục bộ trước khi commit

**Được:** phản hồi nhanh nhất; chặn ngay trước khi code vào lịch sử.

**Vì sao loại:** hook cục bộ bỏ qua được bằng một cờ dòng lệnh, và nó chỉ chạy trên máy có cài. Nó là **bổ sung tốt** cho CI vì rút ngắn vòng phản hồi, nhưng không thay thế được, vì nó không cho ai ngoài người chạy thấy kết quả.

### Phương án C — CI chỉ chạy một phần cổng cho nhanh

**Được:** thời gian chờ ngắn hơn, tốn ít phút chạy hơn.

**Vì sao loại:** đúng hình dạng của sự cố cần chống. Nếu một phần cổng không chạy trên CI, phần đó quay lại trạng thái "chạy tay" — nghĩa là quay lại trạng thái có thể bị bỏ, chỉ khác là nay còn nguy hiểm hơn vì màu xanh của CI tạo cảm giác mọi thứ đã được kiểm.

## Hệ quả

### Tích cực

- **Cổng chạy trên máy không phải của người viết**, nên "chạy được trên máy tôi" không còn là kết luận.
- **Có log để đối chiếu.** Câu hỏi *"mục cổng này thật sự đang chạy chứ?"* trả lời được bằng cách mở lần chạy gần nhất — chứ không phải bằng cách tin vào tài liệu.
- Một script cổng biến mất khỏi đĩa sẽ làm CI đỏ ngay lần chạy kế tiếp, thay vì im lặng hàng tháng.
- Đây là điều kiện để các luật ranh giới ở [`0007-fe-giu-cau-truc-thu-muc.md`](0007-fe-giu-cau-truc-thu-muc.md) có tác dụng thật, vì chúng chỉ được ép bởi cổng.

### Tiêu cực — cái giá thật

- **Cấu hình pipeline là code phải bảo trì**, và nó hỏng theo những kiểu riêng: phiên bản runtime của máy chạy đổi, phụ thuộc bên ngoài không tải được, dịch vụ CI có sự cố. Có những ngày CI đỏ mà code hoàn toàn đúng.
- **Vòng phản hồi dài hơn.** Chờ vài phút cho một lỗi mà chạy dưới máy biết ngay là chi phí trả trên mỗi lần đẩy code.
- **Phụ thuộc một dịch vụ bên ngoài.** Dịch vụ đó hỏng thì việc merge dừng lại — trừ khi có đường vượt cổng, mà đường vượt cổng thì lại xoá đúng thứ vừa xây.
- **Cám dỗ nới cổng khi nó phiền.** Một mục cổng hay đỏ vì lý do vặt sẽ bị đề nghị tắt tạm, và cái tắt tạm hiếm khi được bật lại. Cần coi việc tắt một mục cổng là thay đổi phải có lý do ghi lại, không phải thao tác kỹ thuật.
- **Integration test chạy cơ sở dữ liệu thật nên lần chạy tốn thời gian và tài nguyên**, và nó sẽ chậm dần theo số lượng test. Đây là chi phí chấp nhận vì luật T2 không cho mock cơ sở dữ liệu.

## Liên quan

- [`../audit/2026-08-23-cong-khong-ton-tai.md`](../audit/2026-08-23-cong-khong-ton-tai.md) — sự cố sinh ra quyết định này
- [`../RULES.md`](../RULES.md) — luật T1, T2, T3, T4, S6
- [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) — cổng FE gồm những mục nào
