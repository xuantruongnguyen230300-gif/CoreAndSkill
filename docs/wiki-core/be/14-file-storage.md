---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 14. Lưu trữ file

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Phần bảo mật khi nhận file tải lên ở [`09-security-beyond-auth.md`](09-security-beyond-auth.md) §9. File này lo **cơ chế lưu, vòng đời file và các cách hỏng đặc trưng**.

---

## 1. Vì sao cần một lớp trừu tượng

File có thể nằm ở đĩa của máy chủ, ở một ổ đĩa mạng, hoặc ở một dịch vụ lưu trữ đối tượng. Ba nơi này có API khác nhau, mô hình lỗi khác nhau, và mô hình quyền khác nhau.

Nếu nghiệp vụ gọi thẳng API của hệ thống tệp:

| Hệ quả | Chi tiết |
| --- | --- |
| Đổi nơi lưu là sửa nghiệp vụ | Và "đổi nơi lưu" xảy ra sớm hơn người ta tưởng — ngay khi mở instance thứ hai, vì đĩa cục bộ không dùng chung được |
| Không test được | Test phải chạm đĩa thật, tạo file thật, dọn file thật |
| Đường dẫn rải khắp nơi | Mỗi chỗ ghép đường dẫn một kiểu |

**Nhưng đừng trừu tượng quá tay.** Interface chỉ nên có các thao tác thật sự dùng. Một interface cố mô phỏng đầy đủ khả năng của cả hệ thống tệp lẫn dịch vụ lưu trữ đối tượng sẽ có những thao tác không cài đặt được ở nơi này, và những thao tác chỉ có nghĩa ở nơi kia.

Thao tác tối thiểu: lưu một luồng dữ liệu và nhận về một khoá, mở một luồng đọc theo khoá, xoá theo khoá, kiểm tồn tại. Bốn thao tác đó phủ gần hết nhu cầu thật.

```csharp
// Core.Application — interface, không biết file nằm ở đâu
public interface IFileStorage
{
    Task<Result<string>> SaveAsync(Stream content, string logicalName, CancellationToken ct);
    Task<Result<Stream>> OpenAsync(string key, CancellationToken ct);
    Task<Result> DeleteAsync(string key, CancellationToken ct);
}
```

Điểm cần chú ý: các thao tác trả `Result` chứ không ném exception cho lỗi dự kiến được (không tìm thấy file, không đủ quyền ghi) — đúng luật R1 ở [`../../RULES.md`](../../RULES.md).

---

## 2. Đường dẫn qua cấu hình, không hardcode

| Quy tắc | Vì sao |
| --- | --- |
| Thư mục gốc lấy từ cấu hình | Mỗi môi trường một chỗ khác nhau |
| Cấu hình được **kiểm lúc khởi động** | Thư mục không tồn tại hoặc không ghi được phải làm app không khởi động, chứ không phải hỏng ở lần tải file đầu tiên. Luật A8 ở [`../../RULES.md`](../../RULES.md) |
| **Không** đặt thư mục file bên trong thư mục ứng dụng | Triển khai lại là mất sạch file |
| **Không** đặt trong thư mục được phục vụ tĩnh | Xem §4 |
| Ghép đường dẫn bằng API của nền tảng | Ghép chuỗi bằng tay sẽ sai dấu phân tách khi đổi hệ điều hành |

### Bố cục khoá file

Đặt tất cả file vào một thư mục là cách chắc chắn gặp vấn đề khi số file lớn — cả về hiệu năng liệt kê lẫn về giới hạn của hệ thống tệp.

Bố cục nên có: phân theo mục đích, rồi phân theo thời gian.

```text
<gốc>/<mục đích>/<năm>/<tháng>/<khoá sinh ra>.<đuôi>
```

Phân theo thời gian có một lợi ích thêm: chính sách dọn dẹp theo tuổi trở thành thao tác trên thư mục, không cần quét toàn bộ.

**Tên file lưu trên đĩa phải do hệ thống sinh**, không dùng tên client gửi. Tên client gửi có thể chứa đường dẫn tương đối để thoát ra thư mục khác, ký tự đặc biệt, hoặc trùng với file đã có. Tên gốc lưu ở DB làm siêu dữ liệu và dùng khi trả file về cho người dùng.

---

## 3. Siêu dữ liệu trong DB

Mỗi file có một bản ghi trong DB: khoá lưu trữ, tên gốc, kiểu nội dung, kích thước, ai tải lên, lúc nào, và tham chiếu tới bản ghi nghiệp vụ nếu có.

> **Từ đây có hai nguồn sự thật — DB và nơi lưu file — và chúng sẽ lệch nhau.** Toàn bộ §5 nói về việc đó.

---

### 3.1 Gắn file vào bản ghi nghiệp vụ — ba mức, chọn tường minh

Người dùng thường tải file lên **trước khi** bản ghi nghiệp vụ tồn tại (họ đang điền form). Ba cách xử lý:

| Cách | Luồng | Đánh đổi |
| --- | --- | --- |
| **Tải sau khi lưu** | Lưu bản ghi trước, rồi mới cho đính kèm | Đơn giản nhất, không có file mồ côi. Trải nghiệm kém với form dài |
| **Tải lên vùng tạm, gắn khi lưu** | File vào vùng tạm kèm khoá phiên; khi lưu bản ghi thì chuyển sang vùng chính | Trải nghiệm tốt. Cần job dọn vùng tạm — xem §6 |
| **Tải thẳng vào vùng chính, gắn sau** | File vào vùng chính ngay, tham chiếu điền sau | Sinh nhiều file mồ côi nhất; cần job đối soát chạy đều |

Cách thứ hai là cân bằng tốt nhất cho ứng dụng quản trị. Điều quan trọng là **chọn tường minh cho từng luồng** — không chọn thì mỗi màn hình sẽ tự làm một kiểu, và job dọn dẹp không biết được file nào là rác, file nào đang chờ gắn.

---

## 4. Bảo mật khi phục vụ file

| Quy tắc | Vì sao |
| --- | --- |
| File **không** nằm trong thư mục máy chủ web phục vụ trực tiếp | Nếu nằm trong đó, ai đoán được đường dẫn là tải được, không qua kiểm quyền nào |
| Phục vụ qua một endpoint **có kiểm quyền** | Quyền xem file thường theo bản ghi nghiệp vụ chứa nó, không phải theo bản thân file |
| Kiểu nội dung do **hệ thống** quyết định, không lấy từ file | Kiểu nội dung sai làm trình duyệt diễn giải file theo cách không mong muốn |
| Đặt header buộc tải xuống cho file người dùng tải lên | Tránh nội dung được thực thi trong ngữ cảnh trang |
| Đặt chỉ thị không lưu bộ đệm cho file riêng tư | Tránh file người này còn trong bộ đệm khi người khác dùng chung máy |

Nếu cần cho phép tải mà không qua ứng dụng (ví dụ file lớn), dùng **liên kết có chữ ký và có hạn dùng** — không mở thư mục ra công khai.

---

## 5. File mồ côi — hai chiều, và cả hai đều xảy ra

| Chiều | Nguyên nhân | Hậu quả |
| --- | --- | --- |
| **File trên đĩa, không có bản ghi DB** | Tải lên xong thì transaction thất bại; hoặc bản ghi bị xoá mà file không bị xoá | Đĩa đầy dần bởi thứ không ai dùng |
| **Bản ghi DB, không có file** | File bị xoá bằng tay; hoặc phục hồi DB về mốc thời gian khác với mốc của kho file | Người dùng bấm tải và nhận lỗi |

### 5.1 Thứ tự thao tác — chỗ quyết định

Vấn đề gốc: **việc ghi file không nằm trong transaction của DB.** Không có cách nào làm hai việc đó nguyên tử.

| Thứ tự | Rủi ro |
| --- | --- |
| Ghi file trước, rồi ghi DB | Transaction thất bại → file mồ côi. **Rác, nhưng vô hại** |
| Ghi DB trước, rồi ghi file | Ghi file thất bại → bản ghi trỏ vào hư không. **Hỏng, người dùng nhìn thấy** |

Chọn thứ tự thứ nhất: **rác dễ dọn hơn là hỏng**.

Với việc xoá, nguyên tắc ngược lại nhưng cùng logic: **xoá bản ghi DB trước (commit xong), rồi mới xoá file**. Xoá file trước rồi transaction quay lại thì bản ghi còn nguyên mà file đã mất.

**Quy tắc chung:** thao tác trên kho file luôn nghiêng về phía để lại rác, không nghiêng về phía để lại tham chiếu gãy.

### 5.2 Dọn dẹp

Một job định kỳ đối soát hai chiều:

| Chiều | Việc |
| --- | --- |
| File không có bản ghi, **và cũ hơn một khoảng an toàn** | Xoá |
| Bản ghi không có file | **Báo cáo, không tự xoá** — đây là dấu hiệu có gì đó sai, cần người xem |

Khoảng an toàn ở dòng đầu là bắt buộc: nếu không, job sẽ xoá đúng file vừa được tải lên trong lúc transaction chưa kịp commit.

Chênh lệch bất thường giữa hai bên là chỉ số đáng theo dõi — nó tăng đột ngột nghĩa là có một luồng đang hỏng.

---

## 6. File tạm

File tạm sinh ra ở nhiều chỗ: file xuất dữ liệu chờ tải, file tải lên chờ xử lý, file trung gian khi chuyển đổi định dạng.

| Quy tắc | Vì sao |
| --- | --- |
| Thư mục tạm **riêng**, tách khỏi file lâu dài | Để dọn được mà không sợ chạm nhầm |
| Mọi file tạm có **hạn dùng** | Không có hạn thì nó thành file vĩnh viễn không ai dám xoá |
| Dọn bằng job định kỳ, **không** chỉ dựa vào khối dọn dẹp trong code | Tiến trình chết giữa chừng thì khối dọn dẹp không chạy — và đó chính là lúc file tạm bị bỏ lại |
| Không đặt file tạm chứa dữ liệu người dùng ở thư mục tạm dùng chung của hệ điều hành | Quyền truy cập ở đó thường rộng hơn cần thiết |

Dòng thứ ba là dòng hay bị bỏ qua: mọi người viết khối dọn dẹp trong code và coi như xong. Nhưng file tạm còn lại luôn đến từ ca **tiến trình chết**, tức là đúng ca khối dọn dẹp không chạy.

---

## 7. Kích thước và giới hạn

| Giới hạn | Đặt ở đâu |
| --- | --- |
| Kích thước một file | Cả tầng máy chủ web và tầng ứng dụng |
| Tổng kích thước một request nhiều file | Tầng ứng dụng |
| Hạn mức theo người dùng hoặc theo bản ghi | Nghiệp vụ, nếu cần |

Đặt ở tầng máy chủ web là quan trọng: thiếu nó thì toàn bộ nội dung đã được nhận và ghi vào bộ nhớ hoặc đĩa **trước khi** ứng dụng có cơ hội từ chối.

**Với file lớn, xử lý theo luồng, không nạp hết vào bộ nhớ.** Một request nạp cả file vào bộ nhớ, nhân với số người dùng đồng thời, là cách làm cạn bộ nhớ nhanh nhất.

---

## 8. Nhiều instance và sao lưu

| Vấn đề | Ghi chú |
| --- | --- |
| **Đĩa cục bộ không dùng chung được** | Instance A lưu file, instance B không đọc được. Đây là lý do phổ biến nhất buộc phải đổi nơi lưu — và là lý do lớp trừu tượng ở §1 tồn tại |
| **Sao lưu DB không đủ** | Phải sao lưu cả kho file |
| **Phục hồi phải về cùng mốc thời gian** | Phục hồi DB về hôm qua và giữ kho file của hôm nay tạo ra cả hai chiều mồ côi ở §5 cùng lúc |

Dòng cuối là chỗ hay bị phát hiện muộn — thường là trong chính lúc đang xử lý sự cố.

---

## 9. §Áp dụng

> **Thuộc phạm vi v1** (chốt 2026-09-10) — [`01-core-components.md`](01-core-components.md) §5.1, mục A16.
> Thi công ở pha **B4** ([`trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)); hợp đồng endpoint ở [`../../contracts/files.md`](../../contracts/files.md).

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Interface lưu trữ ở `Core.Application` | ✅ sẽ có | Bốn thao tác tối thiểu, trả `Result` |
| Cài đặt trên hệ thống tệp | ✅ sẽ có | Đủ cho v1 một instance |
| Đường dẫn gốc từ cấu hình, kiểm lúc khởi động | ✅ sẽ có | Luật A8 |
| Tên file do hệ thống sinh; tên gốc lưu ở DB | ✅ sẽ có | |
| Bố cục theo mục đích rồi theo thời gian | ✅ sẽ có | |
| Phục vụ qua endpoint có kiểm quyền | ✅ sẽ có | Không phục vụ tĩnh |
| Thứ tự thao tác nghiêng về phía để lại rác | 📐 luật | §5.1 |
| Job đối soát file mồ côi hai chiều | ✅ sẽ có | Chiều "bản ghi không có file" chỉ báo cáo |
| Thư mục tạm riêng + job dọn theo hạn | ✅ sẽ có | Không chỉ dựa vào khối dọn dẹp trong code |
| Giới hạn kích thước ở cả hai tầng | ✅ sẽ có | |
| **Quét mã độc** | ❌ chưa ở v1 | Bắt buộc trước khi file được chia sẻ giữa người dùng — [`09-security-beyond-auth.md`](09-security-beyond-auth.md) |
| **Cài đặt trên dịch vụ lưu trữ đối tượng** | ❌ chưa | Cần khi mở instance thứ hai. Lớp trừu tượng tồn tại để lúc đó chỉ phải thêm một cài đặt |
| **Liên kết có chữ ký, có hạn dùng** | ❌ chưa | Cần khi có file lớn hoặc lượng tải cao |
| **Chống trùng lặp theo nội dung, đánh phiên bản file** | ❌ chưa | Chưa có nhu cầu |
