---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 15. Nhập và xuất dữ liệu

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Lưu trữ file vật lý: [`14-file-storage.md`](14-file-storage.md). Bảo mật khi nhận file: [`09-security-beyond-auth.md`](09-security-beyond-auth.md) §9.

---

## 1. Hai bài toán khác nhau

| | Nhập | Xuất |
| --- | --- | --- |
| Rủi ro chính | **Dữ liệu bẩn vào hệ thống** | Dữ liệu ra ngoài tầm kiểm soát |
| Khó nhất | Xử lý lỗi từng dòng | Kích thước và bộ lọc |
| Người dùng cần gì | Biết dòng nào sai và sai gì | Đúng tập dữ liệu họ đang nhìn |

Chúng dùng chung một vài thành phần đọc/ghi, nhưng chính sách hoàn toàn khác nhau. Đừng thiết kế chung.

---

## 2. Định dạng — CSV và Excel

| | CSV | Excel |
| --- | --- | --- |
| Đọc/ghi | Rất nhẹ, xử lý theo luồng tự nhiên | Nặng hơn; nhiều thư viện nạp cả tệp vào bộ nhớ |
| Kiểu dữ liệu | Không có — mọi thứ là văn bản | Có kiểu |
| Nhiều bảng trong một tệp | Không | Có |
| Người dùng quen | Người dùng nghiệp vụ quen Excel hơn hẳn | |

### 2.1 Bẫy của CSV

| Bẫy | Biểu hiện | Xử lý |
| --- | --- | --- |
| **Bảng mã** | Tiếng Việt thành ký tự lạ | Đọc: nhận diện dấu hiệu đầu tệp, mặc định UTF-8. Ghi: **kèm dấu hiệu đầu tệp**, nếu không phần mềm bảng tính phổ biến sẽ mở sai bảng mã |
| **Dấu phân tách theo vùng** | Một số vùng dùng dấu chấm phẩy | Cho phép chọn, hoặc tự dò từ dòng tiêu đề |
| **Dấu ngăn cách phần thập phân** | `1,5` là một số hay hai ô? | Quy ước rõ trong tệp mẫu và tài liệu |
| **Ô chứa dấu phân tách hoặc xuống dòng** | Lệch cột từ đó trở đi | Dùng thư viện đúng chuẩn, không tự tách chuỗi |

Dòng cuối đáng nhấn: **không bao giờ tự tách CSV bằng cách cắt chuỗi theo dấu phẩy.** Nó chạy đúng trên tệp mẫu và sai trên tệp thật, ở đúng những dòng có ghi chú dài.

### 2.2 Bẫy của Excel

| Bẫy | Biểu hiện | Xử lý |
| --- | --- | --- |
| **Ô có công thức** | Đọc ra công thức thay vì giá trị | Đọc giá trị đã tính; nếu tệp chưa từng mở thì có thể chưa có giá trị lưu sẵn |
| **Ngày là số** | Ngày lưu dạng số thứ tự | Đọc theo kiểu ô, không đọc theo chuỗi hiển thị |
| **Số dài bị mất chữ số** | Mã số dài bị chuyển thành dạng số và mất chữ số cuối, hoặc hiển thị rút gọn | Trong tệp mẫu, định dạng cột đó là **văn bản** |
| **Số 0 ở đầu biến mất** | Mã bắt đầu bằng 0 | Cùng cách xử lý |
| Ô trống và ô chứa khoảng trắng | Hai thứ khác nhau | Cắt khoảng trắng, coi chuỗi rỗng là trống |

Hai dòng giữa là nguyên nhân phổ biến nhất của các báo lỗi kiểu *"dữ liệu nhập vào bị sai"* mà thật ra tệp đầu vào đã sai từ trước khi hệ thống nhìn thấy.

---

## 3. Kiến trúc nhập liệu

### 3.1 Đọc theo dòng, không nạp cả tệp

Interface đọc trả về một chuỗi dòng, mỗi dòng là một ánh xạ tên cột → giá trị chuỗi. Việc chuyển đổi kiểu nằm ở tầng trên, để lỗi chuyển đổi trở thành **lỗi của dòng**, không phải lỗi của cả tệp.

Nạp cả tệp vào bộ nhớ hỏng ở đúng lúc quan trọng nhất: khi người dùng nhập một tệp thật sự lớn.

### 3.2 Ba giai đoạn

```text
1. Đọc và ánh xạ    → dòng thành đối tượng đầu vào
2. Kiểm hợp lệ      → từng dòng, cộng các luật liên dòng (trùng lặp trong tệp)
3. Ghi              → theo lô
```

Tách ba giai đoạn cho phép **kiểm trước, ghi sau**: người dùng thấy toàn bộ lỗi trước khi có bất kỳ thay đổi nào — hữu ích với tệp mà họ định sửa rồi nhập lại.

---

## 4. Lỗi từng dòng

### 4.1 Chọn chính sách, và nói rõ cho người dùng

| Chính sách | Nghĩa | Hợp với |
| --- | --- | --- |
| **Tất-cả-hoặc-không** | Một dòng lỗi thì không ghi gì | Dữ liệu phải nhất quán theo lô |
| **Ghi phần đúng** | Dòng đúng được ghi, dòng lỗi báo lại | Danh mục, dữ liệu độc lập theo dòng |

Cả hai đều hợp lệ. Cái không hợp lệ là **không chọn** — hoặc chọn mà không nói cho người dùng biết, để họ không biết nhập lại tệp đã sửa thì có tạo bản trùng không.

### 4.2 ⚠️ Rò entity chưa lưu từ dòng lỗi sang dòng sau

Đây là bẫy đặc trưng nhất của nhập liệu, và nó **âm thầm**.

**Cơ chế:**

1. Xử lý dòng 5: tạo entity, thêm vào ngữ cảnh dữ liệu. Bộ theo dõi thay đổi của ORM giữ nó ở trạng thái *chờ thêm mới*.
2. Sau đó phát hiện dòng 5 sai (một luật nghiệp vụ, một tham chiếu không tồn tại). Ghi nhận lỗi, `continue` sang dòng 6.
3. **Entity của dòng 5 vẫn nằm trong bộ theo dõi.**
4. Xử lý dòng 6 xong, gọi lưu thay đổi → ORM ghi **cả entity của dòng 5**.

Kết quả: dòng đã bị báo lỗi vẫn được ghi vào DB. Báo cáo nói *"dòng 5 lỗi, không nhập"*, còn dữ liệu thì có dòng 5.

Bẫy này khó phát hiện vì:

- Không có ngoại lệ nào.
- Chỉ xảy ra khi có dòng lỗi **và** có dòng đúng phía sau.
- Test với tệp toàn dòng đúng, hoặc tệp toàn dòng lỗi, đều không bắt được.

**Cách chặn:** khi một dòng bị coi là lỗi, **huỷ mọi thay đổi đang được theo dõi của dòng đó** trước khi sang dòng kế.

```csharp
foreach (var row in rows)
{
    var result = await ProcessRowAsync(row, ct);
    if (result.IsFailure)
    {
        report.AddError(row.Number, result.Error);
        DiscardTrackedChanges();   // BẮT BUỘC — nếu không, entity dòng lỗi sẽ được ghi cùng dòng sau
        continue;
    }
}
```

`DiscardTrackedChanges` gỡ khỏi bộ theo dõi mọi entity đang ở trạng thái chờ và trả các entity đã sửa về trạng thái ban đầu.

**Test bắt buộc kèm theo:** một tệp có dòng lỗi **nằm giữa** các dòng đúng, và khẳng định dòng lỗi **không** xuất hiện trong DB. Không có test này thì cơ chế trên có thể bị gỡ ra trong một lần tái cấu trúc mà không ai biết.

### 4.3 Báo cáo kết quả

| Trường | Vì sao |
| --- | --- |
| Tổng số dòng đọc được | |
| Số dòng thành công / thất bại | |
| **Từng dòng lỗi: số dòng, cột, mã lỗi, giá trị đã nhận** | Người dùng phải tìm được đúng ô trong tệp của họ |
| Tệp kết quả tải về được | Với tệp lớn, danh sách lỗi trên màn hình là không đủ |

Số dòng phải là **số dòng trong tệp gốc** như người dùng nhìn thấy, không phải chỉ số nội bộ. Lệch một dòng do dòng tiêu đề là chi tiết nhỏ nhưng làm hỏng toàn bộ giá trị của báo cáo.

Và như mọi thông điệp khác: BE trả **mã lỗi + tham số đặt tên**, FE ghép câu. Xem [`16-i18n-va-ma-loi.md`](16-i18n-va-ma-loi.md).

### 4.4 Nhập lại cùng một tệp

Người dùng **sẽ** nhập lại cùng một tệp — vì mạng rớt, vì họ không chắc lần trước đã xong chưa, hoặc vì họ đã sửa vài dòng lỗi và nhập lại cả tệp.

Phải quyết định tường minh:

| Cách | Ghi chú |
| --- | --- |
| Theo khoá nghiệp vụ: có rồi thì cập nhật, chưa có thì thêm | Cách phổ biến và an toàn nhất |
| Từ chối nếu khoá đã tồn tại | Rõ ràng, nhưng buộc người dùng phải lọc tệp thủ công |
| Luôn tạo mới | Gần như luôn sai — sinh dữ liệu trùng |

Không quyết định nghĩa là mặc định rơi vào cách thứ ba.

---

## 5. Xuất dữ liệu

### 5.1 Xuất theo bộ lọc đang xem, không theo trang đang xem

Người dùng lọc một lưới còn vài trăm dòng, đang xem trang 2, và bấm xuất. Họ mong nhận **toàn bộ kết quả đã lọc**, không phải hai mươi dòng của trang 2.

Hệ quả kỹ thuật: endpoint xuất nhận **cùng bộ tham số lọc và sắp xếp** với endpoint danh sách, chỉ khác ở chỗ không phân trang. Dùng chung một chỗ dựng truy vấn cho cả hai — nếu không, hai bên sẽ lệch nhau, và người dùng nhận một tệp không khớp thứ họ đang nhìn.

### 5.2 Ghi theo luồng

Ghi trực tiếp ra luồng phản hồi, không dựng cả tệp trong bộ nhớ. Với định dạng bảng tính, dùng chế độ ghi theo luồng của thư viện nếu có.

Kèm theo: đọc dữ liệu theo lô, không nạp toàn bộ kết quả rồi mới ghi. Với tập lớn, **phân trang keyset** là cách duyệt phù hợp — xem [`../../quy-uoc/be-performance.md`](../../quy-uoc/be-performance.md) §6.3.

### 5.3 Giới hạn

Cần một giới hạn số dòng cho việc xuất đồng bộ. Vượt giới hạn thì chuyển sang chạy nền (§6), chứ **không** để request chạy hàng phút — nó sẽ hết thời gian chờ ở một tầng nào đó giữa đường, và người dùng nhận một lỗi không giải thích được.

### 5.4 ⚠️ Công thức trong tệp bảng tính

Một ô bắt đầu bằng dấu bằng, dấu cộng, dấu trừ hoặc ký hiệu tab có thể được phần mềm bảng tính diễn giải như **công thức** khi người khác mở tệp — và công thức đó có thể được dùng để tấn công máy người mở.

Nguy hiểm ở chỗ: dữ liệu độc hại được **người dùng khác nhập vào hệ thống**, và hệ thống chỉ là nơi chuyển tiếp. Người bị hại là người mở tệp xuất.

**Cách chặn:** khi ghi, ô có giá trị bắt đầu bằng các ký tự đó phải được vô hiệu hoá — thêm một ký tự phía trước hoặc ép kiểu ô là văn bản. Đây là trách nhiệm của phía **xuất**, không phải phía nhập.

### 5.5 Dữ liệu nhạy cảm

Xuất là đường dữ liệu rời khỏi hệ thống. Ba điều cần có:

- Quyền xuất là **quyền riêng**, không mặc định đi kèm quyền xem.
- Ghi vào nhật ký kiểm toán: ai xuất, bộ lọc nào, bao nhiêu dòng.
- Cân nhắc che bớt cột nhạy cảm trong tệp xuất.

---

## 6. Tệp lớn chạy nền

Ngưỡng: khi thời gian xử lý vượt mức chấp nhận được cho một request đồng bộ.

Luồng:

```text
Người dùng gửi yêu cầu ──> tạo bản ghi công việc, trả về ngay
                                │
                        job nền xử lý theo lô
                                │
                  cập nhật tiến độ vào bản ghi công việc
                                │
                    xong ──> lưu tệp kết quả + thông báo
```

| Yêu cầu | Chi tiết |
| --- | --- |
| Bản ghi công việc có trạng thái và tiến độ | Người dùng cần biết còn bao lâu |
| Kết quả tải về được, có hạn dùng | Tệp kết quả là file tạm — xem [`14-file-storage.md`](14-file-storage.md) §6 |
| Thất bại thì báo, kèm lý do | Im lặng là cách hỏng tệ nhất |
| Thông báo khi xong | Xem [`12-notifications.md`](12-notifications.md) |
| Một người không chạy được nhiều việc nặng cùng lúc | Nếu không, một người dùng có thể làm cạn tài nguyên |

Với nhập liệu chạy nền, tệp gốc phải được lưu lại tới khi công việc kết thúc — không giữ nó trong bộ nhớ giữa request và job.

---

### 6.1 Bộ nhớ và tài nguyên

| Rủi ro | Chặn bằng |
| --- | --- |
| Nhiều người cùng nhập tệp lớn | Giới hạn số công việc nặng chạy đồng thời |
| Một tệp làm cạn bộ nhớ | Đọc và ghi theo luồng; giới hạn kích thước ở tầng máy chủ web |
| Ghi theo lô quá lớn | Lô vừa phải, và **huỷ theo dõi thay đổi sau mỗi lô** — bộ theo dõi giữ mọi entity đã đi qua nó, nên nó sẽ phình theo số dòng đã xử lý, không theo kích thước lô |

Dòng cuối là biến thể của §4.2: cùng một bộ theo dõi thay đổi, cùng một cách hỏng — nó giữ nhiều hơn người viết tưởng.

---

## 7. Tệp mẫu

Mỗi luồng nhập liệu cần một tệp mẫu tải về được, và nó phải **sinh từ chính định nghĩa cột mà bộ đọc dùng**, không phải một tệp tĩnh chép tay.

Tệp mẫu tĩnh sẽ lệch khỏi định nghĩa thật ngay lần đầu ai đó thêm một cột — và triệu chứng là người dùng nhập tệp mẫu do chính hệ thống cấp mà vẫn bị báo lỗi.

Tệp mẫu nên có: dòng tiêu đề đúng tên cột, một dòng ví dụ, định dạng văn bản cho các cột mã, và ghi chú về cột bắt buộc.

---

## 8. §Áp dụng

> **Thuộc phạm vi v1** (chốt 2026-09-10) — [`01-core-components.md`](01-core-components.md) §5.1, mục A17.
> Thi công ở pha **B4** ([`trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)); hợp đồng endpoint ở [`../../contracts/exports.md`](../../contracts/exports.md).

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Interface đọc theo dòng | ✅ sẽ có | Trả chuỗi dòng, không nạp cả tệp |
| Interface ghi dạng bảng | ✅ sẽ có | Dùng chung cho CSV và bảng tính |
| **Huỷ thay đổi đang theo dõi sau mỗi dòng lỗi** | 📐 **bắt buộc** | §4.2 — kèm test có dòng lỗi nằm giữa |
| Báo cáo kết quả theo dòng, kèm mã lỗi | ✅ sẽ có | Số dòng theo tệp gốc |
| Chính sách nhập lại theo khoá nghiệp vụ | ✅ sẽ có | Quyết định tường minh cho từng luồng |
| Xuất theo bộ lọc đang xem, dùng chung chỗ dựng truy vấn | ✅ sẽ có | |
| Ghi theo luồng, đọc theo lô | ✅ sẽ có | |
| Vô hiệu hoá công thức trong tệp xuất | 📐 **bắt buộc** | §5.4 |
| Tệp mẫu sinh từ định nghĩa cột | ✅ sẽ có | Không dùng tệp tĩnh chép tay |
| Quyền xuất là quyền riêng + ghi nhật ký kiểm toán | ✅ sẽ có | |
| **Nhập/xuất chạy nền** | ❌ chưa ở v1 | Đặt sẵn giới hạn số dòng cho đường đồng bộ; vượt thì báo lỗi rõ ràng thay vì treo |
| **Nhập từ nguồn khác tệp** (API, hệ ngoài) | ❌ chưa | Bài toán khác, không dùng chung thiết kế này |
