---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 06. Kiểm soát ghi đè khi nhiều người sửa cùng lúc

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> File này giữ **mô hình và lý do**. Cách khai trên entity (chữ ký, cấu hình EF) là thi công — ở [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md).

---

## 1. Vấn đề: mất bản ghi cập nhật

Hai người cùng mở một bản ghi lúc 9h00. Người thứ nhất sửa số điện thoại, lưu lúc 9h02. Người thứ hai sửa địa chỉ, lưu lúc 9h03 — và bản ghi của họ mang **số điện thoại cũ**, vì họ đọc dữ liệu trước khi người kia lưu.

Kết quả: thay đổi của người thứ nhất biến mất. Và điều tệ nhất là **không ai biết**: giao diện báo lưu thành công cho cả hai.

Đây là loại lỗi không có ngoại lệ, không có log, không tái hiện được theo yêu cầu, và chỉ lộ ra khi có người nhớ rõ mình đã nhập gì.

---

## 2. Hai họ giải pháp

| | Optimistic | Pessimistic |
| --- | --- | --- |
| Giả định | Xung đột hiếm | Xung đột thường xuyên |
| Cách làm | Cho cả hai cùng sửa, phát hiện xung đột **lúc ghi** | Khoá bản ghi, người thứ hai phải chờ |
| Chi phí | Gần bằng không khi không có xung đột | Giữ khoá, có nguy cơ chờ và khoá chết |
| Trải nghiệm | Người thứ hai bị từ chối và phải tải lại | Người thứ hai bị chặn ngay từ đầu |
| Dùng cho | **Gần như mọi màn hình CRUD** | Ca đặc biệt, xem §7 |

Mặc định là **optimistic**. Pessimistic chỉ dùng khi có lý do cụ thể — nó đắt hơn và kéo theo cả một lớp vấn đề mới.

---

## 3. Optimistic trên PostgreSQL — dùng `xmin`

### 3.1 Nguyên lý

Mỗi bản ghi mang một **token phiên bản** đổi giá trị ở mọi lần cập nhật. Khi ghi, câu lệnh kèm điều kiện *"token vẫn bằng giá trị lúc tôi đọc"*. Nếu người khác đã ghi trước, token đã đổi, điều kiện không khớp, câu lệnh cập nhật **0 dòng** — và tầng dữ liệu báo xung đột thay vì âm thầm ghi đè.

### 3.2 PostgreSQL đã có sẵn token đó

PostgreSQL gán cho mỗi phiên bản dòng một cột hệ thống `xmin` — định danh transaction đã tạo ra phiên bản dòng ấy. **Mọi transaction ghi đều đổi `xmin`**, do chính DB làm, không cần ứng dụng can thiệp.

> ⚠️ `xmin` mang **định danh transaction**, không phải bộ đếm phiên bản dòng. Hai lần `UPDATE` trong **cùng một** transaction cho ra cùng một `xmin`. Đó là lý do test ở §3.4 phải dùng **hai `DbContext` độc lập** — viết bằng một context thì ngoại lệ đồng thời không bao giờ đến, và người viết sẽ kết luận nhầm là cơ chế hỏng.

Nghĩa là: **không cần thêm cột, không cần trigger, không cần code nào tăng số phiên bản.**

> 📖 **Cách khai với Npgsql — kiểu CLR nào, API nào: đọc [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §6.2.**

### 3.3 ⚠️ Bẫy: dùng KIỂU `byte[]`

> Cái sai **không phải** API `IsRowVersion()` — API đó đúng trên Npgsql. Cái sai là **kiểu CLR `byte[]`**. Ranh giới chính xác ở [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §6.3.

Trên SQL Server, cách làm là khai một thuộc tính **`byte[]`** rồi đánh dấu bằng `IsRowVersion()`. Cách đó **lan truyền rất rộng** trong tài liệu và ví dụ trên mạng, vì SQL Server là provider phổ biến nhất.

Áp nguyên **kiểu `byte[]`** đó lên PostgreSQL thì:

1. Npgsql tạo ra một cột `bytea` bình thường trong bảng.
2. **Không có gì cập nhật cột đó** — không phải cột hệ thống, không có trigger, không có code nào tăng.
3. Điều kiện `WHERE` so cột đó với giá trị cũ, và nó **luôn khớp** (cả hai đều rỗng, hoặc cả hai đều bằng giá trị đã ghi lần đầu).
4. Mọi lần ghi đều thành công. **Kiểm tra đồng thời bị vô hiệu hoá hoàn toàn, im lặng.**

Không có lỗi biên dịch. Không có cảnh báo lúc chạy. Cấu hình trông đúng, code trông đúng, test kiểu "sửa rồi lưu" vẫn xanh. Thứ duy nhất khác là: cơ chế bảo vệ không tồn tại.

> **Bài học từ dự án tiền nhiệm:** ở đó, một recipe concurrency **sai provider** tồn tại **song song ở hai chỗ** trong tài liệu. Sửa một chỗ không chạm chỗ kia. Đây là ví dụ điển hình cho luật một-chủ-đề-một-file-chủ ([`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §5): hai bản sao thì chúng sẽ lệch nhau, và agent đọc trúng bản sai sẽ tự tin sinh ra code sai.
>
> **File chủ về recipe concurrency là [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §6.** File bạn đang đọc giữ *vì sao* — bối cảnh, đánh đổi, cách xử lý xung đột — và **không** giữ hình dạng code.

### 3.4 Cách canh cái bẫy đó

Câu văn cảnh báo không đủ — người đọc tài liệu trên mạng đông hơn người đọc file này. Cần một test **chứng minh cơ chế thật sự hoạt động**:

```csharp
[Fact]
public async Task Update_WhenRowChangedByAnotherContext_ThrowsConcurrency()
{
    // Arrange — hai DbContext độc lập, cùng đọc một bản ghi
    var a = await CtxA.Items.SingleAsync(x => x.Id == id);
    var b = await CtxB.Items.SingleAsync(x => x.Id == id);

    // Act — A ghi trước
    a.Rename("A");
    await CtxA.SaveChangesAsync();

    // Assert — B ghi sau phải THẤT BẠI, không được âm thầm ghi đè
    b.Rename("B");
    await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => CtxB.SaveChangesAsync());
}
```

Test này phải chạy trên **PostgreSQL thật** (luật T2 ở [`../../RULES.md`](../../RULES.md)) — trên DB trong bộ nhớ nó vô nghĩa, vì ở đó không có `xmin`. Xem [`04-testing-strategy.md`](04-testing-strategy.md) §4.

Đây là ví dụ điển hình cho *"cổng hỏng âm thầm tệ hơn không có cổng"*: khai token sai provider tạo ra **cảm giác** đã chống ghi đè.

---

## 4. Entity nào cần token

> 📖 **Bảng quyết định *entity nào cần / không cần token*, kèm bẫy "chỉ có một luồng ghi": đọc [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §6.1.**

Bảng đó là một **quy tắc thi công** — người viết entity mới tra nó để quyết định — nên nó thuộc khu `quy-uoc/`. Khu này giữ phần *vì sao cần token nói chung* (§1, §2) và phần *xử lý khi xung đột xảy ra* (§6).

## 5. Ghi cả một tập hợp — token ở cấp tập, không cấp dòng

Một số màn hình không sửa một bản ghi mà **thay thế cả một tập**: ma trận phân quyền là ví dụ rõ nhất — người dùng tích nhiều ô rồi lưu một lần, và server thay toàn bộ tập quyền của vai trò đó.

Token cấp dòng không giải quyết được ca này: các dòng bị xoá đi và tạo mới, nên chẳng có dòng nào để so token.

Cách làm: **một token cho cả tập**.

1. Khi trả dữ liệu ra màn hình, server tính một giá trị băm trên toàn bộ tập hiện tại và gửi kèm.
2. Khi lưu, client gửi lại giá trị đó.
3. Server tính lại băm trên tập hiện tại trong DB; khác nghĩa là có người đã đổi → từ chối, báo xung đột.

Điều kiện để băm ổn định: tập phải được **sắp xếp xác định** trước khi băm. Không có bước sắp xếp, băm đổi ngẫu nhiên theo thứ tự DB trả về, và người dùng gặp lỗi xung đột không tồn tại.

---

## 6. Xử lý xung đột — và nói gì với người dùng

### 6.1 Ở tầng server

| Bước | Việc |
| --- | --- |
| Bắt ngoại lệ đồng thời | Ở đúng một chỗ, không rải trong từng handler |
| Chuyển thành lỗi nghiệp vụ | Đây **không** phải lỗi hệ thống — nó nằm trong dự kiến. Trả `Result` thất bại với mã lỗi riêng, đúng luật R1 ở [`../../RULES.md`](../../RULES.md) |
| Ánh xạ sang HTTP | 409 |
| Không tự thử lại | Thử lại tự động nghĩa là ghi đè thay đổi của người kia — đúng thứ đang muốn tránh |

### 6.2 Ở tầng giao diện

Thông điệp phải trả lời được ba câu, nếu không người dùng sẽ chỉ bấm lưu lại:

1. **Chuyện gì xảy ra** — người khác đã sửa bản ghi này sau khi bạn mở nó.
2. **Dữ liệu của tôi còn không** — có, chưa mất; nó chỉ chưa được lưu.
3. **Giờ làm gì** — tải lại để xem bản mới nhất rồi nhập lại.

Mức tốt hơn, khi màn hình đáng đầu tư: hiển thị **những trường đã bị người khác đổi**, để người dùng không phải nhập lại toàn bộ. Đắt hơn nhiều, nên chỉ làm cho form dài.

**Như mọi thông điệp khác:** BE trả **mã lỗi + tham số đặt tên**, FE ghép câu. Xem [`16-i18n-va-ma-loi.md`](16-i18n-va-ma-loi.md).

---

## 7. Khi nào cần khoá bi quan

Optimistic không đủ khi việc **kiểm rồi ghi** phải nguyên tử và không được để lọt:

| Tình huống | Vì sao optimistic không đủ |
| --- | --- |
| Cấp một số thứ tự không trùng từ một bộ đếm | Hai luồng cùng đọc bộ đếm, cùng thấy giá trị cũ. Token trên bản ghi bộ đếm giải quyết được, nhưng chi phí thử lại cao khi tần suất lớn |
| Trừ tồn kho, trừ hạn mức | Kiểm "còn đủ không" rồi trừ phải nguyên tử |
| Một job nền chỉ được một instance chạy | Không phải bài toán bản ghi mà là bài toán khoá toàn cục |

Ba công cụ, chọn theo bài toán:

| Công cụ | Dùng cho | Rủi ro |
| --- | --- | --- |
| Khoá dòng khi đọc (`SELECT … FOR UPDATE`) | Trừ tồn kho, trừ hạn mức | Khoá chết nếu hai transaction khoá nhiều dòng theo thứ tự khác nhau |
| Khoá tư vấn của PostgreSQL | Khoá theo một khoá logic, không gắn với dòng nào | Quên nhả khoá; khoá sống theo phiên |
| Ràng buộc duy nhất trong DB | Chống trùng | **Ưu tiên cách này khi làm được** — DB tự đảm bảo, không cần khoá |

**Ba quy tắc bắt buộc khi dùng khoá:**

1. Luôn khoá theo **cùng một thứ tự** ở mọi nơi. Thứ tự khác nhau là công thức tạo khoá chết.
2. Giữ khoá **ngắn nhất có thể**. Tuyệt đối không gọi hệ thống ngoài khi đang giữ khoá.
3. Luôn có **thời hạn chờ**. Chờ vô hạn biến một xung đột thành một sự cố toàn hệ.

---

## 8. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Token đồng thời bằng `xmin` | ✅ sẽ có | Cấu hình dành riêng cho Npgsql, **không** dùng kiểu `byte[]` |
| Test chứng minh cơ chế hoạt động | ✅ sẽ có | Chạy trên PostgreSQL thật; là thứ duy nhất phân biệt "có bảo vệ" với "tưởng có bảo vệ" |
| Token cấp tập cho ma trận phân quyền | ✅ sẽ có | Băm trên tập đã sắp xếp xác định |
| Bảng người dùng | 📐 dùng dấu đồng thời sẵn có của Identity | Không thêm cột, không thêm migration |
| Ánh xạ xung đột → HTTP 409 tại một chỗ | ✅ sẽ có | |
| **Khoá bi quan** | ❌ chưa | Chưa có ca nào cần. Khi cần, ưu tiên ràng buộc duy nhất trước khi nghĩ tới khoá |
| **Hợp nhất thay đổi ở mức trường** | ❌ chưa | Đắt. Chỉ cân nhắc cho form dài, và cần ADR |
| **Tự thử lại khi xung đột** | ❌ không làm | Thử lại là ghi đè thay đổi của người khác — đúng thứ cơ chế này sinh ra để chặn |
