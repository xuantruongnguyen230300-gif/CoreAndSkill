---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0031 — Khoá MediatR ở dòng 12.5.x

> **Trạng thái:** Đã chấp nhận (2026-09-14)

## Bối cảnh

[`../quy-uoc/README.md`](../quy-uoc/README.md) §3 (dòng 71 lúc ghi) chốt MediatR làm mediator, **không kèm phiên bản**. Toàn bộ mẫu thi công phía BE dựa vào API của nó: đăng ký handler và behavior ([`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §5.1, dòng 398–403), hai pipeline behavior ([`0006-pipeline-behavior.md`](0006-pipeline-behavior.md), [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §5.2–§5.3), và controller nhận `ISender` ([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §3.1, dòng 304).

Dự án MediatR chuyển sang giấy phép thương mại từ bản lớn 13. Dòng **12.5.x** là bản cuối trước thay đổi đó. Repo này không chứa nguồn nào cho điều khoản chi tiết của giấy phép mới — ADR này **không** nêu điều khoản; điều khoản chi tiết đối chiếu tại nguồn chính thức của dự án MediatR mỗi khi xem lại quyết định.

Ba ràng buộc làm việc này không thể để trống:

| Ràng buộc | Vì sao nó quan trọng ở đây |
| --- | --- |
| Core phân phối bằng clone ([`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md)) | Phụ thuộc Core chọn thì **mọi** dự án hạ nguồn mang theo — kể cả điều khoản giấy phép của nó |
| Phiên bản khai tập trung, không khai trong `.csproj` ([`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md), dòng 62; luật A14) | Có đúng một chỗ ghim — tốt; nhưng chỗ đó phải được điền bằng một con số **có người quyết** |
| Lệnh thêm gói mặc định lấy bản mới nhất | Không chốt trước thì lần dựng đầu tiên ở B0 kéo về bản mang giấy phép thương mại, và **không ai quyết định việc đó** |

## Quyết định

> **Core dùng MediatR dòng 12.5.x, ghim chính xác một bản thuộc dòng đó ở tệp khai phiên bản tập trung. Không nâng lên 13 trở lên khi chưa có ADR mới.**

Tài liệu sống **không** chép số phiên bản; chỗ cần nói phiên bản trỏ về ADR này.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — MediatR 13 trở lên, theo giấy phép thương mại hoặc giấy phép dành cho cộng đồng

**Được:** bản vá tiếp tục; hỗ trợ runtime mới; tài liệu và mẫu hiện hành trên mạng khớp với code.

**Mất:** điều khoản giấy phép đi theo Core vào mọi dự án hạ nguồn. Mỗi dự án phải tự thẩm định mình thuộc diện nào, và thẩm định lại khi điều khoản đổi — một nghĩa vụ không cổng nào canh, và người chịu nó không phải người chọn nó.

**Vì sao loại:** chi phí đó trả **mãi mãi** và **nhân theo số dự án**, trong khi hôm nay không có nhu cầu đo được nào mà chỉ bản 13 trở lên mới đáp ứng.

### Phương án B — Tự viết một mediator mỏng ngay bây giờ

**Được:** không phụ thuộc giấy phép nào; bề mặt nhỏ, chỉ đúng thứ Core dùng. Đây là phương án một người tỉnh táo sẽ chọn.

**Mất:** Core tự sở hữu phần phân giải handler, pipeline open-generic, và cơ chế loại query khỏi `TransactionBehavior` bằng ràng buộc generic ([`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §5.3). Một lỗi ở đó là lỗi của **mọi** request ở **mọi** dự án — cùng rủi ro ADR-0006 đã ghi cho pipeline, nay cộng thêm việc tự viết chính cái pipeline.

**Vì sao loại:** trả chi phí viết và bảo trì vĩnh viễn cho một vấn đề **chưa xảy ra** — dòng 12.5.x chạy được. Giữ làm lối thoát ở mục *Điều kiện lật*.

### Phương án C — Đổi sang một thư viện mediator mã nguồn mở khác

**Được:** giấy phép mở; có bản vá.

**Vì sao loại:** toàn bộ mẫu trong khu quy ước BE phải viết lại theo một API chưa ai trong repo dùng, và chọn thư viện thay thế lúc này là chọn không có tiêu chí đo. Đáng xét khi điều kiện lật xảy ra — và lúc đó phải so với phương án B.

### Phương án D — Bỏ mediator, controller gọi handler thẳng qua DI

**Vì sao loại:** hai pipeline behavior của ADR-0006 mất chỗ bám; muốn giữ chúng thì phải tự dựng decorator bao handler — tức phương án B mà không gọi tên nó.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Không nhận bản vá mới cho dòng 12** | Một lỗ hổng công bố về sau trong 12.5.x chỉ còn hai đường: tự vá trên bản fork, hoặc thoát khỏi dòng này. Cả hai đều là việc gấp, không lên lịch trước được |
| **Tương thích với bản lớn .NET sau không được bảo đảm** | Mỗi lần nâng runtime phải kiểm lại MediatR, với chuẩn build không cảnh báo (luật T4) |
| **Công cụ quét phụ thuộc lỗi thời báo MediatR mãi** | Một cảnh báo cố định phải được đánh dấu có lý do — nếu không, nó dạy người đọc bỏ qua cả cảnh báo thật nằm cạnh |
| **Tài liệu và mẫu trên mạng dần viết cho bản mới** | Người mới chép một mẫu hiện hành có thể gặp API không có ở 12.5 |
| **Kiểu của MediatR lộ ra ngoài `Core.Application`** | Controller của Core và của module nhận `ISender`. Lối thoát B hoặc C phải sửa **mọi** controller ở **mọi** dự án, không chỉ `Core.Application`. ADR này không gói kiểu đó lại |
| **Mọi dự án hạ nguồn thừa hưởng khoá này** | Một dự án tự nâng riêng là lệch khỏi mẫu Core mà nó đang thừa hưởng, và lần kéo bản vá Core kế tiếp phải hoà giải phần lệch đó |

### Tích cực

- Không nghĩa vụ giấy phép mới nào cho bất kỳ dự án hạ nguồn nào.
- Mẫu hiện có trong khu quy ước BE thi công được nguyên trạng.
- Phiên bản mediator là một quyết định có tên và có chỗ tra, không phải thứ xảy ra ngầm bởi một lệnh thêm gói.

### Điều kiện lật quyết định — kèm lộ trình thoát

| Dấu hiệu | Lối ra phải cân trong ADR mới |
| --- | --- |
| **Lỗ hổng bảo mật công bố ảnh hưởng 12.5.x**, không có bản vá trong dòng 12 | Tự viết mediator mỏng giữ gần chữ ký đang dùng (phương án B) · hoặc mua giấy phép bản hiện hành (phương án A) |
| **Bản .NET đang dùng hoặc sắp nâng không build sạch hay không chạy với 12.5.x** | Như trên |
| **Nhu cầu đo được mà chỉ bản 13 trở lên đáp ứng** | Phương án A — kèm thẩm định giấy phép cho từng dự án hạ nguồn |
| **Điều khoản giấy phép của MediatR thay đổi** | Đối chiếu tại nguồn chính thức, xét lại phương án A |

## Liên quan

- [`0006-pipeline-behavior.md`](0006-pipeline-behavior.md) — hai pipeline behavior dựa trên mediator
- [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md) — vì sao phụ thuộc của Core đi theo mọi dự án
- [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §1, §5 — mẫu dùng MediatR
- [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) — khai phiên bản tập trung
- [`../RULES.md`](../RULES.md) — luật A14, T4
