---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 2026-09-05 — Phản chiếu ở đường xử lý lỗi biến mọi lỗi nghiệp vụ thành lỗi con trỏ rỗng

> Sự cố có thật ở **dự án tiền nhiệm**. Ghi lại ở đây vì nó là lý do tồn tại của hai luật trong [`../RULES.md`](../RULES.md) và của [`../adr/0003-result-thuan.md`](../adr/0003-result-thuan.md).
>
> Viết theo khuôn bốn phần ở [`README.md`](README.md).

---

## 1. Hiện tượng

Trên môi trường Production, **mọi lỗi nghiệp vụ trở thành `NullReferenceException`.**

Nghĩa là: người dùng thao tác sai — nhập trùng mã, sửa bản ghi đã bị xoá, vi phạm một luật nghiệp vụ — và thay vì nhận về một thông điệp giải thích kèm mã lỗi, họ nhận một lỗi máy chủ chung chung. Phía frontend không có mã nào để dịch, nên chỉ hiển thị được thông báo mặc định.

Ba đặc điểm khiến nó khó phát hiện:

- **Đường thành công hoàn toàn bình thường.** Mọi thao tác đúng đều chạy đúng. Chỉ nhánh lỗi hỏng.
- **Lỗi trong log không trỏ về nguyên nhân.** Vết ngăn xếp dừng ở lớp xử lý lỗi, không nhắc gì tới lỗi nghiệp vụ ban đầu — thông tin về lỗi thật đã mất trước khi được ghi.
- **Không tái hiện được bằng cách đọc code một cách hời hợt**, vì đoạn code liên quan trông hoàn toàn hợp lý.

## 2. Nguyên nhân gốc

Kiến trúc lúc đó có **hai cơ chế lỗi song song**: tầng Domain ném exception nghiệp vụ, tầng Application trả về một envelope kết quả. Giữa chúng phải có một **cầu nối**, và cầu nối đó là một pipeline behavior bắt exception rồi dựng envelope tương ứng.

Cầu nối được dựng bằng **phản chiếu**:

- tra một phương thức factory theo **tên** trên kiểu envelope,
- kèm một **danh sách kiểu tham số hardcode**, và danh sách đó nằm ở **một file khác** với file khai phương thức,
- rồi **gọi** phương thức tìm được.

Chuỗi sự kiện dẫn tới sự cố:

1. Chữ ký của phương thức factory được **thêm một tham số**, tham số đó có giá trị mặc định. Với người viết, đây là một thay đổi tương thích ngược — mọi lời gọi cũ trong C# vẫn biên dịch bình thường.
2. Nhưng phép tra theo phản chiếu so khớp **chữ ký đầy đủ**. Một tham số có giá trị mặc định **không** làm phương thức khớp với một danh sách kiểu ngắn hơn. Phép tra trả về `null`.
3. Kết quả tra được đưa qua toán tử **khẳng định không null**. Toán tử này không kiểm gì cả — nó chỉ tắt cảnh báo của compiler. Khi giá trị thật sự là null, dòng gọi kế tiếp ném `NullReferenceException`.
4. Nó ném **ngay bên trong nhánh xử lý lỗi**. Nên mọi lỗi nghiệp vụ, không phân biệt loại, đều bị thay bằng lỗi con trỏ rỗng.

Nguyên nhân gốc, phát biểu ở mức cơ chế: **một ràng buộc giữa hai file được giữ bằng phản chiếu thay vì bằng kiểu.** Compiler không biết hai file đó phải khớp nhau, nên khi một bên đổi, không có gì nhắc bên kia. Toán tử khẳng định không null xoá nốt tín hiệu cảnh báo cuối cùng.

### Vì sao không có gì bắt được

Đây là phần đáng học nhất, vì nó cho thấy ba tầng phòng thủ đều trượt cùng lúc:

| Tầng | Vì sao trượt |
| --- | --- |
| **Compiler** | Phản chiếu là chuyện lúc chạy. Tên phương thức và danh sách kiểu là dữ liệu, không phải mã được kiểm kiểu. Đổi chữ ký không sinh lỗi biên dịch nào |
| **Test kiến trúc** | Bộ test kiến trúc của họ rất mạnh, nhưng không luật nào chạm tới đường này. Nó kiểm ranh giới, phụ thuộc, hình dạng entity — không kiểm rằng một lời gọi phản chiếu có tìm thấy đích hay không |
| **Test thủ công và test tự động** | Hỏng nằm ở **nhánh xử lý lỗi**. Đó là nhánh ít được chạy nhất: người kiểm thử đi đường thành công, và test tự động lúc đó chưa phủ luồng lỗi qua behavior này |

Cộng lại: một thay đổi trông vô hại, không lỗi biên dịch, không cổng nào chạm tới, hỏng ở nhánh ít chạy nhất. Nó ra tới Production là chuyện gần như tất yếu.

### Hình dạng của lỗi, tóm trong bốn bước

| Bước | Điều xảy ra | Ai lẽ ra phải phản đối |
| --- | --- | --- |
| 1 | Thêm một tham số có giá trị mặc định vào phương thức factory | Không ai — trong C# đây là thay đổi tương thích ngược, mọi lời gọi cũ vẫn biên dịch |
| 2 | Phép tra theo phản chiếu so khớp chữ ký đầy đủ và trả `null` | Compiler — nhưng nó không thấy gì, vì tên phương thức và danh sách kiểu chỉ là dữ liệu |
| 3 | Toán tử khẳng định không null cho `null` đi qua | Toán tử này không kiểm gì; nó chỉ tắt cảnh báo |
| 4 | Lời gọi kế tiếp ném lỗi con trỏ rỗng, ngay trong nhánh xử lý lỗi | Test — nhưng nhánh lỗi khi đó chưa có test đi qua |

Điều đáng chú ý ở bảng này: **không bước nào là một sai lầm rõ ràng.** Bước 1 là thay đổi đúng chuẩn. Bước 2 là hành vi đúng của thư viện phản chiếu. Bước 3 là một thói quen phổ biến. Bước 4 chỉ là hệ quả. Sự cố sinh ra từ **cách bốn thứ đúng ghép lại**, và đó là lý do việc đọc code từng chỗ không phát hiện được nó.

## 3. Cách vá

Cách vá tại chỗ của dự án tiền nhiệm gồm hai việc:

1. **Thay toán tử khẳng định không null bằng một lệnh ném lỗi có thông điệp giải thích.** Kết quả: khi phép tra thất bại, hệ ném một lỗi nói rõ nó không tìm thấy phương thức nào và vì sao điều đó quan trọng — thay vì một lỗi con trỏ rỗng không manh mối. Đây là vá **chẩn đoán**: sự cố vẫn xảy ra, nhưng lần sau người ta biết ngay chuyện gì.
2. **Thêm một unit test cho luồng đó**, để phép tra được thực thi ít nhất một lần trong bộ test.

Họ cũng ghi lại nguyên sự cố vào comment ngay tại file — cách làm giữ được bài học, nhưng chính là lớp vấn đề mà [`../adr/0010-comment-toi-thieu.md`](../adr/0010-comment-toi-thieu.md) xử lý theo hướng khác.

**Nguyên nhân gốc vẫn còn.** Mỗi lần đổi chữ ký phương thức factory vẫn là một lần phải nhớ sửa danh sách kiểu ở file khác, và compiler vẫn không nhắc. Cách vá đã đổi *cách hỏng* từ im lặng sang ồn ào — một cải thiện thật — nhưng chưa xoá được khả năng hỏng.

## 4. Cổng nào lẽ ra phải bắt — và CoreAndSkill chặn thế nào

Hai lớp, một chặn triệu chứng, một xoá nguyên nhân.

### Lớp 1 — Luật R5: cấm phản chiếu ở đường ánh xạ `Result` → HTTP

Luật R5 trong [`../RULES.md`](../RULES.md) cấm phản chiếu trên đúng đoạn đường này, và được ép bằng một test kiến trúc.

Thay vào đó, ánh xạ từ loại lỗi sang mã trạng thái HTTP viết bằng **khớp mẫu** trên một tập `ErrorType` đóng. Điều này đổi bản chất của lớp lỗi: thêm một `ErrorType` mà quên xử lý trở thành thứ compiler phàn nàn được, ngay lúc biên dịch, thay vì một null lộ ra ở Production.

Nguyên tắc phát biểu tổng quát: **ở nhánh xử lý lỗi, ưu tiên cấu trúc mà compiler kiểm được hơn cấu trúc linh hoạt.** Linh hoạt ở đây không mua được gì — tập loại lỗi vốn đã đóng.

### Lớp 2 — Result thuần: xoá luôn cầu nối

[`../adr/0003-result-thuan.md`](../adr/0003-result-thuan.md) bỏ hẳn một trong hai cơ chế lỗi: Domain và Application đều trả `Result<T>`, không ném exception cho lỗi nghiệp vụ.

Không còn hai cơ chế thì **không còn cầu nối nào để hỏng**. Đây là dạng vá tốt nhất — không phải chặn lỗi, mà làm cho nơi sinh ra lỗi biến mất.

### Lớp 3 — Test phải chạm nhánh lỗi

Bổ sung, thuộc [`../wiki-core/be/04-testing-strategy.md`](../wiki-core/be/04-testing-strategy.md): với mỗi loại lỗi khai trong catalog, phải có ít nhất một test đi qua đường ánh xạ sang HTTP. Không có test nào chạm nhánh lỗi thì nhánh lỗi trên thực tế chưa từng chạy.

### Dấu hiệu nhận ra cùng lớp lỗi ở chỗ khác

Bốn câu hỏi để soi một đoạn code bất kỳ; trả lời "có" cho từ hai câu trở lên thì đoạn đó cùng lớp với sự cố này:

1. Có hai file phải khớp nhau mà **không có kiểu nào ràng buộc** chúng — tên phương thức dạng chuỗi, danh sách kiểu tham số, khoá cấu hình đối chiếu bằng tên?
2. Ràng buộc đó có được **giải quyết lúc chạy** thay vì lúc biên dịch?
3. Đoạn code đó có nằm ở **nhánh hiếm chạy** — xử lý lỗi, dọn dẹp khi thất bại, đường di trú dữ liệu, luồng khôi phục?
4. Có toán tử hoặc thao tác nào **tắt cảnh báo** thay vì xử lý trường hợp xấu?

Cách xử lý theo thứ tự ưu tiên: xoá ràng buộc lỏng (thay bằng kiểu) → nếu không xoá được thì thêm test đi qua đúng đường đó → nếu vẫn không được thì ít nhất làm nó **hỏng ồn ào** với thông điệp giải thích, đừng để nó hỏng thành một lỗi vô nghĩa.

### Điều CoreAndSkill cố ý KHÔNG làm

Luật R5 **không cấm phản chiếu trong toàn hệ**. Nó chỉ cấm trên đường ánh xạ `Result` sang HTTP.

Lý do phải nói rõ, để người sau không mở rộng luật này thành một lệnh cấm chung:

- Phản chiếu là công cụ hợp lệ ở nhiều chỗ — ánh xạ đối tượng, dựng dịch vụ khi khởi động, dò kiểu để đăng ký hàng loạt. Ở những chỗ đó nó chạy **một lần lúc khởi động**, và nếu hỏng thì ứng dụng không lên được: hỏng ồn ào, phát hiện ngay.
- Điều làm sự cố này nghiêm trọng không phải bản thân phản chiếu, mà là **vị trí** của nó: một đường chạy hiếm, ở giữa luồng phục vụ người dùng, nơi hỏng không làm ứng dụng dừng mà chỉ làm mọi lỗi nghiệp vụ mất nghĩa.

Cấm rộng hơn mức cần thiết sẽ khiến luật bị coi là phiền và bị xin ngoại lệ — và một luật đã có ngoại lệ đầu tiên thì sẽ có ngoại lệ thứ hai. Luật hẹp và có lý do rõ thì giữ được.

## 5. Bài học tổng quát

> **Nhánh xử lý lỗi là nhánh ít được test nhất, và là nơi phản chiếu gây thiệt hại lớn nhất.**

Ba điều rút ra, dùng được cho tình huống khác:

1. **Code chạy hiếm cần được kiểm nghiêm hơn code chạy thường, không phải lỏng hơn.** Code chạy mỗi phút thì lỗi lộ ra trong ngày. Code chạy chỉ khi có lỗi có thể hỏng hàng tháng mà không ai biết — và nó hỏng đúng lúc người dùng đang cần nó nhất.
2. **Một ràng buộc mà compiler không thấy sẽ trôi.** Phản chiếu, chuỗi định danh, cấu hình đối chiếu bằng tên: tất cả đều tạo ra cặp file phải khớp nhau mà không ai ép. Ở nơi có thể thay bằng kiểu, hãy thay.
3. **Toán tử khẳng định không null là một cách tắt cảnh báo, không phải một cách kiểm.** Dùng nó ở nhánh xử lý lỗi là bỏ đi tín hiệu cuối cùng, đúng ở chỗ tín hiệu quý nhất.
4. **Vá chẩn đoán vẫn đáng làm, kể cả khi chưa vá được gốc.** Đổi một lỗi vô nghĩa thành một lỗi có thông điệp giải thích không ngăn được sự cố, nhưng nó rút thời gian điều tra lần sau từ hàng giờ xuống hàng phút. Khi chưa xoá được nguyên nhân, hãy ít nhất làm cho cách hỏng nói được nó là gì.

Một hệ quả cho cách viết code nói chung: **ở mọi chỗ mà một tra cứu có thể không tìm thấy, hãy quyết định trước điều gì xảy ra khi không tìm thấy.** Bỏ trống quyết định đó không làm trường hợp xấu biến mất — nó chỉ dời thời điểm phát hiện sang lúc bất tiện nhất.

## 6. Ảnh hưởng tới phía frontend — vì sao nó làm sự cố nặng thêm

Đáng ghi riêng, vì nó cho thấy một lỗi backend có thể lan qua ranh giới hợp đồng.

Thiết kế hợp đồng API của hệ này tách **mã lỗi** khỏi **câu hiển thị**: backend trả mã cùng tham số, frontend tra bảng ngôn ngữ để dựng câu cho người dùng. Đó là thiết kế đúng và CoreAndSkill giữ nguyên — xem [`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md).

Nhưng khi cầu nối hỏng, envelope trả về **không còn mã nghiệp vụ nào**. Frontend không có gì để tra, nên rơi về thông báo mặc định. Hệ quả là hai thông tin cùng mất một lúc:

- người dùng không biết mình sai ở đâu,
- người hỗ trợ không có mã để tra cứu khi nhận báo lỗi.

Bài học kèm theo: **một hợp đồng dựa trên mã lỗi chỉ mạnh bằng chỗ yếu nhất trên đường sinh ra mã đó.** Vì vậy luật R6 ở [`../RULES.md`](../RULES.md) yêu cầu mọi envelope lỗi dựng tay đều mang mã nghiệp vụ — envelope không mã là một envelope đã mất thông tin trước khi rời khỏi máy chủ.

## 7. Liên quan

- [`../adr/0003-result-thuan.md`](../adr/0003-result-thuan.md) — quyết định xoá cầu nối
- [`../RULES.md`](../RULES.md) — luật R4, R5
- [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) — ánh xạ `Result` → HTTP, đúng một chỗ
- [`README.md`](README.md) — khuôn và luật của khu này
