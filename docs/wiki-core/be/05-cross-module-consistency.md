---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 05. Nhất quán dữ liệu giữa các module

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Ba khái niệm hay bị lẫn — domain event, integration event, notification — được tách bạch ở [`12-notifications.md`](12-notifications.md) §1. File này lo phần **nhất quán dữ liệu**; file kia lo phần **báo cho người dùng**.

---

## 1. Vấn đề

Modular monolith: một tiến trình, một DB, nhiều schema, và **cấm khoá ngoại xuyên schema** ([`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §5).

Cấm khoá ngoại xuyên schema là quyết định đúng — nó là điều kiện tiên quyết để sau này tách được microservice. Nhưng nó đẩy một trách nhiệm về phía ứng dụng: **DB không còn giữ hộ tính toàn vẹn giữa hai module nữa**.

Ba câu hỏi phát sinh:

1. Module A đổi dữ liệu, module B cần biết — bằng cách nào?
2. Nếu B xử lý thất bại, A có phải quay lại không?
3. Trong khoảng thời gian A đã đổi mà B chưa xử lý xong, hệ thống ở trạng thái nào, và nói gì với người dùng?

---

## 2. Ba mức nhất quán — chọn đúng mức cho đúng việc

| Mức | Cơ chế | Đảm bảo | Dùng khi |
| --- | --- | --- | --- |
| **Cùng transaction** | Domain event xử lý trong process, trước khi commit | Hoặc tất cả, hoặc không gì | Hai bên **cùng một module**, hoặc cùng một biên nhất quán |
| **Outbox** | Ghi dữ liệu và ghi sự kiện trong **cùng** transaction; phát sau | Sự kiện chắc chắn được phát, ít nhất một lần | Hai bên **khác module** |
| **Eventual** | Chấp nhận độ trễ, tự chữa bằng đối soát định kỳ | Cuối cùng sẽ đúng | Việc phụ, không chặn nghiệp vụ chính |

**Quy tắc chọn:** đi từ trên xuống, chọn mức đầu tiên đủ dùng. Chọn mức thấp hơn mức cần thì hỏng dữ liệu; chọn mức cao hơn mức cần thì trói hai module vào nhau và mất đúng thứ modular monolith sinh ra để giữ.

---

## 3. Domain event — trong process, cùng transaction

### 3.1 Nó là gì

Một sự kiện **trong lòng một biên nhất quán**: entity ghi nhận "chuyện này vừa xảy ra", và các handler trong cùng transaction phản ứng. Nếu bất kỳ handler nào thất bại, toàn bộ transaction quay lại — kể cả thay đổi đã gây ra sự kiện.

### 3.2 Phát ở đâu, xử lý lúc nào

- **Phát**: entity tự thêm sự kiện vào danh sách của nó khi trạng thái đổi. Entity không tự gọi handler — nó chỉ ghi nhận.
- **Xử lý**: ngay **trước** khi lưu, trong cùng transaction.

Chỗ này có một quyết định thật phải chốt:

| Thời điểm phát tán | Ưu | Nhược |
| --- | --- | --- |
| **Trước khi lưu** | Handler được sửa tiếp dữ liệu và mọi thứ lưu chung một lần | Handler chưa thấy giá trị do DB sinh (ví dụ số tự tăng) |
| Sau khi lưu, cùng transaction | Handler thấy dữ liệu đã lưu | Thay đổi của handler cần một lần lưu nữa; dễ quên |

Khuyến nghị: **trước khi lưu**, và tránh phụ thuộc giá trị do DB sinh trong handler domain event.

### 3.3 Giới hạn — nói thẳng

Domain event **không** dùng để nói chuyện giữa hai module. Nếu handler của module B chạy trong transaction của module A thì hai module đã dính vào nhau: B chậm thì A chậm, B lỗi thì A quay lại. Đó đúng là thứ ranh giới module sinh ra để tránh.

Ranh giới: domain event **không rời khỏi module đã phát ra nó**.

---

## 4. Integration event qua Outbox

### 4.1 Vấn đề mà Outbox giải

Cần làm hai việc: ghi dữ liệu vào DB, và báo cho bên khác. Không có Outbox thì luôn có một khe hở:

| Thứ tự | Khe hở |
| --- | --- |
| Ghi DB → phát sự kiện | Ghi xong, tiến trình chết trước khi phát → **sự kiện mất**, dữ liệu đã đổi mà không ai biết |
| Phát sự kiện → ghi DB | Phát xong, ghi thất bại → **sự kiện ma**, bên nhận phản ứng với thứ không xảy ra |

Cả hai đều hỏng, và cả hai đều hiếm — nên chúng sống rất lâu trước khi ai đó tìm ra.

**Outbox đóng khe hở bằng cách biến hai việc thành một:** ghi dữ liệu và ghi bản ghi sự kiện vào **cùng một transaction**, trên cùng một DB. Transaction thành công thì cả hai cùng có; thất bại thì cả hai cùng không. Việc phát sự kiện đi được tách ra một tiến trình riêng, đọc bảng outbox và phát.

Module có `DbContext` riêng, còn outbox nằm ở schema `core` — nên *"cùng một transaction"* chỉ đúng khi mọi `DbContext` tham gia chạy trên **một** kết nối và **một** transaction do đơn vị công việc điều phối: [`../../adr/0025-luu-du-lieu-module-mot-transaction.md`](../../adr/0025-luu-du-lieu-module-mot-transaction.md).

### 4.2 Hình dạng bảng outbox

| Cột | Vai |
| --- | --- |
| Định danh | Khoá; **bên nhận dùng nó để chống xử lý trùng** |
| Loại sự kiện | Tên hợp đồng, để chọn bộ xử lý |
| Nội dung | Dữ liệu sự kiện đã tuần tự hoá |
| Thời điểm tạo | |
| Thời điểm phát thành công | Rỗng nghĩa là chưa phát |
| Số lần thử | Để lùi dần và để phát hiện bản ghi kẹt |
| Lỗi lần cuối | Để chẩn đoán |
| Mã lần gọi | Nối sự kiện với request đã sinh ra nó — xem [`07-observability.md`](07-observability.md) |

Bảng này thuộc schema `core`. Chi tiết vận hành ở [`12-notifications.md`](12-notifications.md) §Outbox.

### 4.3 Đảm bảo là "ít nhất một lần", không phải "đúng một lần"

Tiến trình phát có thể phát thành công rồi chết trước khi kịp đánh dấu đã phát. Lần chạy sau nó phát lại. Đây **không phải lỗi cần sửa** — "đúng một lần" là thứ không đạt được ở hệ phân tán mà không trả giá rất đắt.

Hệ quả bắt buộc: **mọi bên nhận phải chịu được xử lý trùng.** Xem §5.

### 4.4 Tiến trình phát — vài điểm dễ sai

| Điểm | Cách làm đúng |
| --- | --- |
| Lấy bản ghi chưa phát | Khoá bản ghi đang xử lý để hai instance không cùng phát một sự kiện |
| Thứ tự | **Không hứa hẹn thứ tự** giữa các sự kiện khác nhau. Bên nhận cần thứ tự thì phải tự xử lý bằng số phiên bản trong nội dung sự kiện |
| Sự kiện kẹt | Sau N lần thử, chuyển sang trạng thái cần can thiệp và **báo động** — không im lặng thử mãi |
| Dọn bảng | Xoá bản ghi đã phát sau một khoảng đã khai; không dọn thì bảng phình mãi và tiến trình chậm dần |
| Nhịp quét | Vài giây một lần là đủ ở quy mô này. Nhanh hơn thì tốn kết nối DB mà không đổi lấy gì |

---

## 5. Idempotency — điều kiện bắt buộc, không phải tuỳ chọn

"Ít nhất một lần" nghĩa là bên nhận **sẽ** gặp sự kiện trùng. Nếu xử lý trùng gây hậu quả (cộng tiền hai lần, gửi hai email), hệ thống sai.

Ba cách, xếp theo độ tin cậy:

| Cách | Làm thế nào | Ghi chú |
| --- | --- | --- |
| **Bảng đã-xử-lý** | Bên nhận ghi định danh sự kiện vào một bảng với ràng buộc duy nhất, **trong cùng transaction** với việc xử lý. Trùng thì vi phạm ràng buộc và bỏ qua | Tin cậy nhất. Chi phí: một bảng và một lần ghi |
| **Thao tác tự nhiên bất biến** | Thiết kế để xử lý lại không đổi kết quả: gán giá trị thay vì cộng dồn, `INSERT … ON CONFLICT DO NOTHING` | Tốt nhất khi làm được, nhưng không phải ca nào cũng làm được |
| **Kiểm trạng thái trước khi làm** | "Nếu đã ở trạng thái đích thì thôi" | Có kẽ hở giữa lúc kiểm và lúc ghi. Chỉ dùng khi hậu quả trùng là nhẹ |

**Bẫy:** ghi bảng đã-xử-lý ở một transaction **khác** transaction xử lý. Khi đó vẫn có khe hở, chỉ là hẹp hơn — và khe hở hẹp là khe hở khó tái hiện nhất.

Cùng nguyên tắc áp cho endpoint HTTP mà client có thể gửi lại: nhận một khoá do client sinh, ghi kèm ràng buộc duy nhất.

---

## 6. Eventual consistency — và cách nói cho người dùng

Có một khoảng thời gian, thường vài giây, mà dữ liệu ở hai module chưa khớp. Kỹ thuật thì bình thường; vấn đề nằm ở chỗ **người dùng không biết điều đó**.

Cách xử lý, theo thứ tự ưu tiên:

| Cách | Khi nào | Ví dụ giao diện |
| --- | --- | --- |
| **Nói rõ trạng thái trung gian** | Việc đủ quan trọng để người dùng chờ kết quả | Bản ghi hiện trạng thái *"đang xử lý"*, tự cập nhật khi xong |
| **Không nói gì** | Việc phụ, người dùng không quan sát | Ghi nhật ký truy cập |
| ❌ Báo "thành công" rồi im lặng thất bại | Không bao giờ | Đây là cách làm mất niềm tin nhanh nhất |

**Quy tắc:** nếu một việc có thể thất bại **sau khi** người dùng đã rời màn hình, phải có đường để họ biết. Tối thiểu là một trạng thái xem lại được; tốt hơn là một thông báo — xem [`12-notifications.md`](12-notifications.md).

Và cần một **cơ chế đối soát**: một job định kỳ so hai bên, phát hiện lệch, báo cáo. Không có nó, một sự kiện mất sẽ không bao giờ bị phát hiện — nó chỉ thể hiện thành "dữ liệu sai" nhiều tháng sau.

---

## 7. Vì sao KHÔNG dùng message broker ở v1

### 7.1 Broker cho gì và Outbox đã cho được bao nhiêu

| Broker cho | Ở quy mô này, Outbox + tiến trình phát nền cho | Còn thiếu gì |
| --- | --- | --- |
| Bền bỉ khi bên nhận chết | ✅ Bảng DB bền hơn hầu hết cấu hình broker mặc định | — |
| Thử lại và lùi dần | ✅ Số lần thử nằm ngay trong bảng | — |
| Thư chết | ✅ Một cột trạng thái | Không có giao diện quản lý sẵn |
| Tách nhịp giữa các process | ➖ Không cần — mọi module cùng một process | Cần khi tách process |
| Fan-out ra hệ ngoài | ❌ | Cần khi có hệ ngoài |
| Chịu tải đỉnh bằng hàng đợi | ➖ Đủ ở quy mô này | Cần khi tải vượt khả năng DB |

### 7.2 Broker tốn gì

- **Một hạ tầng nữa phải cài, giám sát, sao lưu, nâng cấp.**
- **Một nguồn sự thật nữa** — và câu hỏi "sự kiện này đã tới chưa" giờ phải hỏi ở hai nơi.
- **Mất tính nguyên tử với DB.** Chính vì broker không nằm trong transaction của DB nên vẫn **phải có Outbox** để ghi và phát cùng số phận. Tức là thêm broker **không** bỏ được Outbox; nó chỉ thay chỗ đến của tiến trình phát.

Điểm cuối là điểm hay bị hiểu nhầm nhất: người ta thêm broker và tưởng đã giải quyết bài toán nhất quán. Không — bài toán nhất quán được giải bằng Outbox, và Outbox nằm ở DB.

### 7.3 Điều kiện để thêm broker

Thêm khi thoả **ít nhất một**, và kèm ADR:

1. Một module được tách thành **process riêng** — lúc đó "trong cùng tiến trình" không còn đúng.
2. Cần phát sự kiện ra **hệ thống bên ngoài** không truy cập được DB này.
3. Tiến trình phát **đã đo được** là không theo kịp tải, và tối ưu truy vấn không đủ.
4. Cần các mẫu định tuyến mà bảng không làm được: nhiều bên nhận độc lập với tiến độ riêng, phát tán theo chủ đề.

"Broker chuẩn hơn" hoặc "sau này chắc cần" không nằm trong danh sách.

---

## 8. Bốn câu hỏi khi thiết kế một luồng xuyên module

1. **Nếu bên nhận thất bại, nghiệp vụ chính có phải quay lại không?** Có → hai bên thuộc cùng một biên nhất quán, và có lẽ chúng không nên là hai module.
2. **Bên nhận xử lý cùng một sự kiện hai lần thì sao?** Không trả lời được → chưa xong phần idempotency ở §5.
3. **Sự kiện mất thì bao lâu ai đó phát hiện?** Không có câu trả lời → thiếu job đối soát ở §6.
4. **Người dùng có thấy trạng thái trung gian không, và họ hiểu nó không?** Không → thiếu thiết kế giao diện cho eventual consistency.

Ba câu đầu là kỹ thuật, câu thứ tư là thứ hay bị bỏ quên — và nó lại là câu duy nhất người dùng cảm nhận được.

---

## 9. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Domain event trong process | ✅ sẽ có | Không rời khỏi module đã phát |
| Outbox trong schema `core` | ✅ sẽ có | Ghi cùng transaction với dữ liệu |
| Tiến trình phát nền | ✅ sẽ có | Hosted service, quét theo nhịp |
| Bảng đã-xử-lý cho bên nhận | ✅ sẽ có | Ràng buộc duy nhất trên định danh sự kiện |
| Job đối soát định kỳ | 📐 nên có | Không có nó thì sự kiện mất sẽ không bao giờ lộ ra |
| **Message broker** | ❌ chưa ở v1 | Điều kiện ở §7.3. Thêm broker **không** bỏ được Outbox |
| **Saga / bù trừ nhiều bước** | ❌ chưa | Chỉ cần khi một nghiệp vụ trải qua nhiều module và phải quay lại từng bước. Chưa có ca nào |
| **Hứa hẹn thứ tự sự kiện** | ❌ không làm | Bên nhận cần thứ tự thì tự xử lý bằng số phiên bản trong nội dung sự kiện |
