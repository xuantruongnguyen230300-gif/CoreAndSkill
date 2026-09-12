---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0006 — Đúng hai pipeline behavior ở v1: `Validation` và `Transaction`

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

Ở dự án tiền nhiệm, tầng Application đăng ký đúng hai behavior: kiểm hợp lệ và xử lý ngoại lệ. **Không có behavior transaction.**

Thay vào đó, quy ước ghi rằng handler tự sở hữu việc lưu, đúng một lần, ở cuối. Hệ quả là **không có transaction bao ngoài**: một command chạm hai aggregate, nếu phần sau lỗi sau khi phần trước đã lưu, thì dữ liệu ghi dở dang và không có gì rollback.

Đây không phải rủi ro lý thuyết. Chính họ đã gặp lớp vấn đề này ở luồng nhập liệu theo dòng và phải bù bằng một cơ chế thủ công loại bỏ thay đổi đang theo dõi — tức là tự làm lấy một phần việc của transaction, bằng tay, ở một chỗ.

Behavior xử lý ngoại lệ thì [`0003-result-thuan.md`](0003-result-thuan.md) đã xoá lý do tồn tại: không còn exception nghiệp vụ để bắt.

## Quyết định

`Core.Application` đăng ký **đúng hai** pipeline behavior:

| Behavior | Áp cho | Làm gì |
| --- | --- | --- |
| `ValidationBehavior` | Command và Query | Chạy validator của request; có lỗi thì trả `Result` thất bại kiểu `Validation`, **không gọi handler** |
| `TransactionBehavior` | **Chỉ command** | Mở transaction trước handler, commit khi `Result` thành công, rollback khi thất bại hoặc khi có exception ngoài dự kiến |

Thứ tự cố định: `Validation` chạy trước `Transaction`. Lý do: request sai hình dạng thì không đáng mở transaction, và mở transaction rồi rollback ngay là chi phí vô ích trên mỗi request hỏng.

Hệ quả kèm theo cho quy ước viết handler: **handler không tự quản transaction**, và không gọi lưu nhiều lần với ý định "chốt từng phần".

Query **không** đi qua `TransactionBehavior`. Một truy vấn chỉ đọc không cần transaction, và bọc nó lại chỉ giữ kết nối lâu hơn cần thiết.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Không có `TransactionBehavior`, handler tự lưu một lần ở cuối

Đây là cách dự án tiền nhiệm làm.

**Được:** không có gì bao ngoài, đọc handler là thấy hết; không có transaction dài ngoài tầm nhìn của người viết.

**Vì sao loại:** nó **đúng cho tới khi một command chạm hai aggregate**. Lúc đó quy ước "lưu một lần ở cuối" không còn đủ, vì có những thao tác buộc phải lưu giữa chừng để lấy khoá sinh tự động, hoặc để gọi một dịch vụ cần bản ghi đã tồn tại. Từ đó trở đi, tính toàn vẹn phụ thuộc vào việc người viết nhớ đúng thứ tự — và một cơ chế bù thủ công cho luồng nhập liệu theo dòng đã là bằng chứng rằng cái nhớ đó đã thất bại một lần.

### Phương án B — Transaction mở bằng tay trong từng handler cần

**Được:** transaction chỉ có ở nơi thật sự cần; phạm vi hẹp nhất có thể.

**Vì sao loại:** biến một luật thành một việc phải nhớ, ở mọi handler, mãi mãi. Và cái quên đó không gây lỗi ngay: nó chỉ hiện ra vào ngày một bước giữa chừng thất bại trên Production. Đây đúng là lớp lỗi mà pipeline behavior sinh ra để loại bỏ.

### Phương án C — Bọc cả Query trong transaction cho đồng nhất

**Được:** một luật duy nhất, không phải phân biệt command với query.

**Vì sao loại:** trả chi phí trên mọi lượt đọc để mua một tính chất mà đọc không cần. Ngoài ra nó che mất một tín hiệu hữu ích: nếu một query cần transaction, gần như chắc chắn nó đang ghi thứ gì đó và lẽ ra phải là command.

## Vì sao chưa có Logging, Performance, Caching, Authorization

Ghi rõ ở đây để người sau **không tưởng là bỏ sót**.

| Behavior | Vì sao chưa làm ở v1 |
| --- | --- |
| `Logging` | Chưa đo được vấn đề. Ghi log mỗi request là ghi trùng phần lớn thứ mà tầng HTTP đã ghi; khi có nhu cầu thật thì nó nên đi cùng thiết kế quan sát tổng thể ở [`../wiki-core/be/07-observability.md`](../wiki-core/be/07-observability.md), không phải thêm lẻ |
| `Performance` | Đo thời gian mỗi handler chỉ có ích khi đã có nơi thu và xem số liệu. Chưa có nơi đó thì nó chỉ sinh log không ai đọc |
| `Caching` | Chưa có Redis và chưa có truy vấn nào được chứng minh là nút cổ chai. Thêm cache trước khi biết cache cái gì là tự tạo ra lớp lỗi dữ liệu cũ mà không đổi lại được gì |
| `Authorization` | Phân quyền kiểm ở mức endpoint qua thuộc tính yêu cầu quyền — xem [`0005-permission-based.md`](0005-permission-based.md). Thêm một tầng kiểm thứ hai ở pipeline sẽ tạo hai chỗ quyết định cùng một việc, và khi hai chỗ bất đồng thì rất khó truy |

Nguyên tắc chung: **thêm behavior sau khi đo được vấn đề**, không thêm vì thấy hay. Mỗi behavior chạy trên **mọi** request, nên chi phí và rủi ro của nó cũng trải trên mọi request.

## Hệ quả

### Tích cực

- **Ghi dở dang biến mất như một lớp lỗi.** Một command hoặc thành công trọn vẹn, hoặc không để lại gì.
- Handler viết ngắn hơn: không còn đoạn tự quản transaction, không còn cơ chế bù thủ công.
- Số behavior nhỏ nên **thứ tự chúng còn đọc hiểu được**, và luật A10 ở [`../RULES.md`](../RULES.md) kiểm được rằng mỗi behavior đăng ký đúng một lần.

### Tiêu cực — cái giá thật

- **Transaction sống lâu hơn.** Nó bao trọn handler, kể cả những phần chậm mà lẽ ra không cần nằm trong transaction. Một handler gọi dịch vụ ngoài ở giữa sẽ giữ transaction mở suốt lượt gọi đó — và đó là công thức của khoá kéo dài. Quy ước bù: **không gọi dịch vụ ngoài bên trong command**; việc đó đẩy sang outbox.
- **Handler mất quyền chốt từng phần.** Có luồng thật sự muốn giữ phần đã làm được khi phần sau hỏng — ví dụ nhập một tệp nhiều dòng, dòng lỗi thì bỏ dòng đó thôi. Với transaction bao ngoài, luồng đó phải được thiết kế lại thành nhiều command nhỏ, mỗi dòng một command. Đó là thiết kế đúng hơn, nhưng nó là công phải làm thêm chứ không miễn phí.
- **Một behavior sai là sai toàn hệ.** Đây là mặt trái của việc dùng pipeline: bug nằm ở đó ảnh hưởng mọi use case cùng lúc. Đổi lại, nó chỉ có một chỗ để test kỹ.
- **Không có Logging behavior nghĩa là khi cần truy vết một lỗi hiếm, phải thêm log tại chỗ** thay vì có sẵn dấu vết. Đây là nợ có ý thức, không phải quên.

## Liên quan

- [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) — quy ước viết handler và validator
- [`../RULES.md`](../RULES.md) — luật A10, A11
- [`0003-result-thuan.md`](0003-result-thuan.md) — vì sao không còn behavior xử lý ngoại lệ
