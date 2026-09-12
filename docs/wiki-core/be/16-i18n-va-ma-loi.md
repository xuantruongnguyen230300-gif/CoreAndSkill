---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 16. Mã lỗi và đa ngôn ngữ phía Backend

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Hình dạng envelope và ánh xạ sang HTTP là thi công: [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md). Phía FE ghép câu: [`../fe/08-i18n.md`](../fe/08-i18n.md) và [`../fe/09-forms-validation.md`](../fe/09-forms-validation.md).

---

## 1. Luật gốc: BE sở hữu MÃ, không sở hữu CÂU CHỮ

> **Backend trả về mã lỗi và tham số đặt tên. Backend không bao giờ trả về một câu đã ghép sẵn để hiển thị.**

Luật R8 ở [`../../RULES.md`](../../RULES.md), canh bằng ArchTest `NoUserFacingVietnameseString_InBackend`.

### 1.1 Vì sao

| Lý do | Chi tiết |
| --- | --- |
| **Đổi ngôn ngữ không phải sửa BE** | Thêm ngôn ngữ thứ hai là thêm một tệp dịch ở FE. Nếu câu nằm ở BE thì phải sửa BE, triển khai lại BE, và BE phải biết ngôn ngữ của người đang xem |
| **Sửa câu chữ không phải triển khai lại BE** | Câu chữ đổi nhiều hơn logic. Một lỗi chính tả không đáng phải triển khai lại backend |
| **Một nơi giữ giọng văn** | Câu chữ nằm cùng chỗ với mọi câu chữ khác của giao diện, nên thống nhất được về cách xưng hô, độ dài, mức trang trọng |
| **Client tự quyết cách hiển thị** | Cùng một lỗi có thể hiện dưới ô nhập, trong hộp thoại, hoặc trong một dòng tóm tắt — với độ dài khác nhau |
| **Máy đọc được** | Mã lỗi đếm được, gộp nhóm được, cảnh báo được. Một câu tiếng Việt thì không |

### 1.2 Ranh giới sở hữu

| Thứ | Ai sở hữu |
| --- | --- |
| Mã lỗi | **BE** |
| Tham số của thông điệp (tên và giá trị) | **BE** |
| Hằng số chính sách xuất hiện trong câu (độ dài mật khẩu tối thiểu, số lần thử tối đa) | **BE** — vì BE là nơi thực thi chính sách đó |
| Câu chữ hiển thị | **FE** |
| Thứ tự từ, dấu câu, cách xưng hô | **FE** |
| Cách định dạng ngày, số, tiền theo văn hoá | **FE** — xem §6 |

Dòng thứ ba đáng chú ý: nếu BE ép mật khẩu tối thiểu N ký tự và FE hiển thị một số khác được viết cứng trong bản dịch, hai con số sẽ lệch nhau khi chính sách đổi. Con số phải đi ra từ BE **dưới dạng tham số**, và câu ở FE chừa chỗ cho nó.

### 1.3 Ngoại lệ: email

Email được **server** gửi đi, nên phải có ai đó ghép câu ở phía server. Cách giải là tách cơ chế khỏi nội dung: Core cấp cơ chế kết xuất mẫu, dự án cấp mẫu và bản dịch. Core **không** đi kèm mẫu tiếng Việt mặc định. Chi tiết ở [`12-notifications.md`](12-notifications.md) §4.

---

## 2. Tham số theo TÊN, không theo thứ tự

Luật R7 ở [`../../RULES.md`](../../RULES.md), canh bằng ArchTest `MessageParams_AreNamed_NotPositional`.

> 📖 **Vì sao tham số phải theo TÊN chứ không theo thứ tự — kèm hệ quả khi hai field cùng vi phạm một luật: đọc [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) §8.2.**

Phần thuộc riêng khu này nằm ở mục dưới.

### 2.1 Giá trị tham số phải là dữ liệu thô

| Đúng | Sai |
| --- | --- |
| `"MinLength": "8"` | `"MinLength": "8 ký tự"` |
| `"Deadline": "2026-03-01T00:00:00Z"` | `"Deadline": "ngày 01/03/2026"` |
| `"Amount": "1234.5"` | `"Amount": "1.234,50 ₫"` |

Cột phải là câu chữ và định dạng theo văn hoá — hai thứ thuộc về FE. Gửi giá trị đã định dạng nghĩa là BE đã quyết định thay FE, và quyết định đó sẽ sai với ngôn ngữ khác.

> 📖 **Kiểu của từ điển tham số và cách đặt tên khoá: đọc [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) §2** (chữ ký `Error`). Giá trị là **chuỗi chưa định dạng**, không phải số — "thô" nói về *chưa định dạng*, không nói về kiểu JSON.

---

## 3. Catalog mã lỗi

### 3.1 Vì sao phải tập trung

Mã lỗi rải rác dưới dạng chuỗi ở nơi ném ra dẫn tới: mã trùng, mã sai chính tả, mã không ai còn dùng, và không có cách nào liệt kê "hệ thống này có những mã lỗi nào" để đưa cho người dịch.

Luật R2 ở [`../../RULES.md`](../../RULES.md) cấm dựng mã lỗi từ chuỗi literal ngoài catalog.

### 3.2 Một mục catalog gồm gì

| Thành phần | Vai |
| --- | --- |
| **Mã** | Định danh ổn định |
| **Loại lỗi** | Quyết định ánh xạ sang HTTP — xem §3.5 |
| **Tên các tham số** | Hợp đồng với người dịch |
| **Câu dự phòng** | Xem §4 |

> 📖 **Chữ ký `Error`, hình dạng catalog và cách gắn tham số: đọc [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) §7.1.**

Bảng trên nói một mục catalog *gồm gì*. Nó **không** khai hình dạng code — hình dạng đó có file chủ, và một mẫu thứ hai ở đây sẽ lệch khỏi mẫu chủ mà không gì báo.

### 3.3 Khuôn mã

> 📖 **Khuôn mã lỗi, bảng ba thành phần và biểu thức kiểm: đọc [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) §7.3.**

Một luật thuộc riêng khu này, không có ở file chủ: **phần Core không mang tên nghiệp vụ của một dự án cụ thể trong mã lỗi** — luật A5 ở [`../../RULES.md`](../../RULES.md). Core biết tên nghiệp vụ là Core không mang đi được.

### 3.4 Loại lỗi và ánh xạ HTTP

> 📖 **Bảng `ErrorType` → mã HTTP là định nghĩa gốc, nằm ở [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §1.1** ([`../../OWNERSHIP.md`](../../OWNERSHIP.md) §3). Mở đúng bảng đó, đừng nhớ theo trí nhớ.

Điều thuộc riêng khu này: **loại lỗi được chọn ở catalog, không ở controller.** Controller chỉ ánh xạ; chọn sai loại ở catalog thì mọi endpoint dùng mã đó cùng trả sai mã HTTP.

## 4. Câu dự phòng và bản dịch — một nguồn, hai đầu ra

### 4.1 Vì sao vẫn cần một câu ở BE

Dù FE ghép câu, BE vẫn cần một câu tiếng Việt tối giản cho mỗi mã, vì ba nơi:

| Nơi | Vì sao |
| --- | --- |
| **Log và bảng theo dõi lỗi** | Người trực đọc log không có bảng dịch của FE trong tay |
| **Client không phải FE của mình** | Công cụ thử API, tích hợp bên thứ ba |
| **FE chưa có bản dịch cho mã mới** | Hiển thị câu dự phòng còn hơn hiển thị chính mã lỗi cho người dùng |

**Câu này là dự phòng, không phải nguồn hiển thị chính.** FE có bản dịch thì luôn dùng bản dịch. Nhầm chỗ này dẫn tới việc FE hiển thị thẳng câu của BE, và toàn bộ mục §1 mất tác dụng.

### 4.2 Một nguồn, hai đầu ra

Vấn đề: nếu câu dự phòng ở BE và bản dịch ở FE là hai danh sách được duy trì độc lập, chúng sẽ lệch — mã mới thêm ở BE mà FE không biết, mã bỏ đi ở BE mà FE còn giữ.

Cách giải:

```text
        Catalog mã lỗi (BE)  ← NGUỒN DUY NHẤT
                 │
       ┌─────────┴──────────┐
       ▼                    ▼
 Câu dự phòng        Tệp khoá dịch sinh ra
 dùng trong log      cho FE (khoá + tên tham số)
```

Tệp khoá dịch **sinh ra bằng lệnh**, không chép tay. Người dịch điền câu vào tệp đó. Hai điều kiểm được từ đây:

1. Mã có trong catalog mà thiếu bản dịch → cổng báo.
2. Bản dịch có khoá không tồn tại trong catalog → cổng báo (khoá chết).

Không có bước sinh này, hai danh sách sẽ lệch — không phải nếu, mà là khi nào.

---

## 5. Lỗi thuộc về từng ô nhập

### 5.1 Vì sao cần tách riêng

Một lỗi cấp thao tác (*"không đủ quyền"*) và một lỗi cấp trường (*"email sai định dạng"*) được hiển thị ở hai chỗ khác nhau: một cái ở đầu form hoặc trong hộp thoại, một cái ngay dưới ô nhập.

Nếu cả hai đi ra cùng một chỗ, FE không phân biệt được, và người dùng phải tự tìm xem ô nào sai.

### 5.2 Hình dạng

> 📖 **Hình dạng `fieldErrors` trong envelope là định nghĩa gốc, nằm ở [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §2.1–2.3** ([`../../OWNERSHIP.md`](../../OWNERSHIP.md) §3).

Ba điểm thuộc riêng khu này, đọc kèm bảng ở file chủ:

- **Một trường có thể có nhiều lỗi.** Vì vậy giá trị là một **danh sách**, không phải một lỗi.
- Tên trường phải ánh xạ được về đúng ô nhập trên form — quy tắc casing chính xác ở file chủ §2.3, và nó là chỗ *"sửa cho nhất quán" sẽ phá im lặng*.
- Mỗi phần tử vẫn mang **mã và tham số**, không mang câu. Đây là chỗ luật §1 của file này áp vào cấp trường.

### 5.3 ⚠️ KHÔNG nối danh sách mã vào một chỗ giữ trong câu

Cám dỗ: khi một trường có nhiều lỗi, ghép chúng thành một chuỗi rồi nhét vào một chỗ giữ của một câu duy nhất.

```json
// ❌ SAI
{ "code": "CORE.USER.PASSWORD_INVALID",
  "messageParams": { "problems": "tooShort, needsDigit" } }
```

Bốn thứ vỡ, và không thứ nào lộ ra ngay:

| Vỡ ở đâu | Chi tiết |
| --- | --- |
| **Dấu phân tách** | Dấu phẩy, dấu chấm phẩy, hay chữ "và"? Khác nhau giữa các ngôn ngữ, và nó đã bị đóng cứng ở BE |
| **Liên từ cuối cùng** | Nhiều ngôn ngữ dùng liên từ trước phần tử cuối. Nối sẵn thì không làm được |
| **Từng mã không còn dịch được** | Cả cụm đã thành một chuỗi. FE chỉ có thể hiển thị nguyên si — tức là hiển thị mã lỗi thô cho người dùng |
| **Không hiển thị theo cách khác được** | FE không thể biến nó thành một danh sách gạch đầu dòng, không thể tô đậm từng mục |

> **Danh sách phải đi ra dưới dạng danh sách.** Ghép sẵn là thứ vỡ ngay khi đổi ngôn ngữ — và cho tới lúc đó thì nó trông vẫn ổn, nên không ai sửa.

Cùng nguyên tắc áp cho mọi tập hợp trong thông điệp: danh sách trường bị trùng, danh sách dòng lỗi khi nhập liệu, danh sách quyền còn thiếu. Tất cả đi ra dưới dạng mảng.

### 5.4 Lỗi từ thư viện xác thực và từ Identity

Bộ kiểm hợp lệ và bộ quản lý danh tính đều sinh ra mã lỗi riêng của chúng. Hai việc phải làm:

1. **Chuyển sang mã của catalog** trước khi rời `Core.Infrastructure`. Mã nội bộ của một thư viện là hợp đồng của thư viện đó, không phải hợp đồng của hệ thống — nó có thể đổi khi nâng cấp thư viện.
2. **Chuyển tham số theo tên**, và chỉ chuyển các khoá nằm trong danh sách cho phép. Chuyển nguyên cả từ điển tham số của thư viện ra ngoài là rò rỉ chi tiết cài đặt, và có thể rò cả giá trị nhạy cảm.

#### Ánh xạ mã sang ô nhập phải có HAI tầng, không phải một

Khi một lỗi của bộ quản lý danh tính cần hiện dưới đúng một ô của biểu mẫu, phải trả lời hai câu tách rời nhau:

| Câu hỏi | Phụ thuộc gì | Ví dụ |
| --- | --- | --- |
| Mã này **nghĩa là gì** | Chỉ thư viện | "mật khẩu không đạt yêu cầu độ mạnh" |
| Nó thuộc **ô nào** | Từng biểu mẫu | Ở màn đổi mật khẩu là ô *mật khẩu mới*; ở màn quản trị đặt lại mật khẩu là ô *mật khẩu tạm* |

Gộp hai câu thành một bảng duy nhất buộc **mỗi** biểu mẫu chép lại toàn bộ danh mục mã chỉ để đổi tên ô. Ba biểu mẫu là ba bản chép, và chúng sẽ lệch — đúng khuôn hỏng mà [`../../OWNERSHIP.md`](../../OWNERSHIP.md) §1 mô tả.

**Luật:** tầng một (mã → ý nghĩa) khai **một lần** cho toàn hệ. Tầng hai (ý nghĩa → tên ô) khai **theo từng biểu mẫu**, và chỉ khai phần khác với mặc định.

---

## 6. Ngày giờ, số và tiền tệ

### 6.1 Nguyên tắc: BE truyền giá trị, FE định dạng

| Kiểu | BE gửi | FE hiển thị |
| --- | --- | --- |
| Thời điểm | Chuỗi theo chuẩn quốc tế, **có múi giờ**, ưu tiên UTC | Theo múi giờ và văn hoá của người xem |
| Ngày không có giờ (ngày sinh, ngày hiệu lực) | Chuỗi ngày thuần, **không** kèm giờ và múi giờ | Theo định dạng của văn hoá |
| Số | Số, không phải chuỗi | Theo quy ước dấu ngăn cách của văn hoá |
| Tiền | Số **kèm mã tiền tệ**, tách riêng | Theo quy ước của văn hoá |

### 6.2 Ba bẫy

| Bẫy | Chi tiết |
| --- | --- |
| **Trộn "thời điểm" với "ngày"** | Một ngày sinh lưu dạng thời điểm sẽ lệch một ngày khi đổi múi giờ. Ngày sinh **không có** múi giờ — nó không phải một thời điểm |
| **Lưu thời gian địa phương** | Lưu UTC, đổi sang giờ địa phương khi hiển thị. Lưu giờ địa phương thì mọi so sánh đều sai khi có nhiều múi giờ, và sai hai lần mỗi năm nếu vùng đó đổi giờ |
| **Số tiền dùng kiểu dấu phẩy động** | Sai số tích luỹ. Dùng kiểu thập phân chính xác, và lưu **cả mã tiền tệ** — một con số không có đơn vị là một con số vô nghĩa |

### 6.3 Sắp xếp chuỗi tiếng Việt

Sắp xếp chuỗi theo thứ tự byte cho kết quả sai với tiếng Việt — chữ có dấu bị xếp sau toàn bộ chữ không dấu. Sắp xếp phải theo quy tắc đối chiếu của ngôn ngữ, khai ở tầng DB. Đây là một quyết định về lược đồ, nên phải chốt **trước** khi có dữ liệu thật: xem [`../../database/schema-core.md`](../../database/schema-core.md).

---

## 7. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| BE trả mã + tham số, không trả câu | 📐 luật | R8 |
| Tham số theo tên | 📐 luật | R7 |
| Catalog mã lỗi tập trung | ✅ sẽ có | R2 — hình dạng khai ở [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) §7.1, **không** khai ở đây |
| Khuôn mã và tính duy nhất | ✅ sẽ có | R3 |
| Ánh xạ loại lỗi → HTTP tại một chỗ, không reflection | ✅ sẽ có | R4, R5 |
| Câu dự phòng cho log và client ngoài | ✅ sẽ có | Không phải nguồn hiển thị chính |
| Tệp khoá dịch **sinh từ catalog** | ✅ sẽ có | Kèm cổng báo khoá thiếu và khoá chết |
| Lỗi theo từng ô nhập, mỗi ô là một **danh sách** | ✅ sẽ có | §5.2 |
| Không nối danh sách mã vào một chỗ giữ | 📐 **luật** | §5.3 |
| Chuyển mã của thư viện sang mã catalog, lọc tham số theo danh sách cho phép | ✅ sẽ có | §5.4 |
| Thời điểm lưu UTC; ngày thuần không có múi giờ | ✅ sẽ có | §6.2 |
| Tiền: kiểu thập phân + mã tiền tệ | ✅ sẽ có | |
| Quy tắc đối chiếu tiếng Việt ở tầng DB | ✅ sẽ có | Chốt trước khi có dữ liệu thật |
| **Đa ngôn ngữ thật (từ hai ngôn ngữ trở lên)** | ❌ chưa ở v1 | Nhưng **mọi ràng buộc ở trên vẫn áp ngay từ đầu**. Chúng gần như miễn phí lúc này và rất đắt khi thêm sau |
| **Chọn ngôn ngữ theo header của request** | ❌ chưa | BE không ghép câu nên không cần. Ngoại lệ là email — lấy ngôn ngữ từ hồ sơ người nhận, xem [`12-notifications.md`](12-notifications.md) |
