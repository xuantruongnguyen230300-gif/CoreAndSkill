---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 09. Bảo mật ngoài phạm vi đăng nhập

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Đăng nhập, phiên và phân quyền ở [`02-identity-auth.md`](02-identity-auth.md). File này lo phần còn lại. Phía FE: [`../fe/14-security.md`](../fe/14-security.md).

---

## 1. Nguyên tắc: nhiều lớp, không lớp nào là duy nhất

Mọi biện pháp dưới đây đều có cách vượt qua. Chúng có giá trị vì **cộng lại**: kẻ tấn công phải vượt tất cả, còn người phòng thủ chỉ cần một lớp còn hoạt động để có thời gian phát hiện.

Hệ quả thực tế cho việc review: một biện pháp bị tắt "vì đã có biện pháp khác rồi" là một finding, không phải một tối ưu.

---

## 2. CSRF, CORS và giới hạn tần suất

> 📖 **Cả ba chủ đề này có file chủ chung: [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §6 (giới hạn tần suất) và §7 (CSRF + CORS + cookie phiên).**

Ở đó có: chuỗi ràng buộc *cùng tên miền gốc ⇒ cookie `SameSite=Lax` ⇒ phải cùng scheme ⇒ HTTPS mọi môi trường*, hai lớp chống CSRF và phép thử chứng minh lớp thứ nhất đang chạy, bảng endpoint nào cần antiforgery, luật đặt tên cookie, allowlist CORS và giới hạn của chính CORS, sáu ràng buộc của giới hạn tần suất.

**Phần thuộc riêng khu này** — nguyên tắc nhiều lớp ở §1, và các mặt an toàn *không* đi qua tầng HTTP ở §3 trở đi.

Vì sao tách như vậy: một đoạn mẫu về giới hạn tần suất ở dự án tiền nhiệm dùng sai cách gọi kèm lý do sai, và code chép y theo nên mang nguyên lỗi. Cách gọi cụ thể phải nằm ở đúng một chỗ.

---

## 3. Chống dò tài khoản

Kẻ tấn công muốn biết **địa chỉ email nào có tài khoản** — đó là bước đầu của mọi chiến dịch dò mật khẩu và lừa đảo.

Hệ thống rò rỉ thông tin đó qua ba đường, và phải bịt cả ba:

| Đường rò | Biểu hiện | Cách bịt |
| --- | --- | --- |
| **Thông điệp lỗi** | "Tài khoản không tồn tại" khác "Sai mật khẩu" | Dùng **một** mã lỗi chung cho cả hai |
| **Thời gian phản hồi** | Tài khoản không tồn tại thì trả lời nhanh hơn, vì không phải kiểm mật khẩu | Vẫn thực hiện một phép băm giả khi tài khoản không tồn tại, để thời gian tương đương |
| **Mã trạng thái hoặc hình dạng phản hồi** | 404 cho tài khoản không có, 401 cho sai mật khẩu | Cùng một mã, cùng một hình dạng |

Đường thứ hai là đường hay bị bỏ sót nhất, vì nó không nhìn thấy được khi thử thủ công — chỉ lộ ra khi có ai đó đo hàng nghìn lần.

Các luồng khác cũng phải theo nguyên tắc này:

- **Khôi phục mật khẩu quản trị đơn vị** qua khu hệ thống: ca "không có tài khoản đó" và ca "có nhưng không đủ điều kiện" trả về **cùng một** mã lỗi — [`../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md).
- **Đăng ký** (nếu có): không nói thẳng "email đã tồn tại" ở màn hình công khai.

**Đánh đổi phải nói rõ:** cách này làm trải nghiệm kém đi — người dùng gõ nhầm email sẽ không biết. Đây là đánh đổi có ý thức, không phải sơ suất, và người thiết kế giao diện cần biết để viết câu chữ cho hợp.

---

## 4. Kiểm tra đầu vào

| Nguyên tắc | Chi tiết |
| --- | --- |
| **Danh sách cho phép, không danh sách chặn** | Danh sách chặn luôn thiếu một biến thể nào đó. Khai *cái gì được phép* thì phần còn lại tự động bị từ chối |
| **Kiểm ở server, luôn luôn** | Kiểm ở FE là trải nghiệm, không phải bảo mật. Mọi request đều có thể được gửi trực tiếp |
| **Giới hạn kích thước ở mọi chiều** | Độ dài chuỗi, số phần tử mảng, độ sâu JSON, kích thước toàn bộ nội dung. Thiếu một chiều là một đường tấn công cạn tài nguyên |
| **Kiểm ở biên, không rải trong nghiệp vụ** | Handler nhận dữ liệu đã hợp lệ. Xem [`04-testing-strategy.md`](04-testing-strategy.md) và luật A11 ở [`../../RULES.md`](../../RULES.md) |

Chi tiết dữ liệu **không đáng tin** kể cả sau khi qua kiểm tra: mọi giá trị vẫn phải được xử lý đúng cách ở nơi nó được dùng (tham số hoá khi vào SQL, mã hoá khi ra HTML). Kiểm tra đầu vào không thay thế được điều đó.

---

## 5. Gán tràn thuộc tính

**Vấn đề:** một endpoint nhận thẳng entity làm kiểu đầu vào. Client gửi thêm trường không có trên form — chẳng hạn cờ toàn quyền ở [`02-identity-auth.md`](02-identity-auth.md) §3.6 — và bộ chuyển đổi gán nó vào.

Lỗ hổng này **không nhìn thấy được** khi đọc code, vì không có dòng nào gán trường đó cả. Nó đến từ hành vi mặc định.

Cách chặn, cộng dồn:

| Lớp | Cách |
| --- | --- |
| Entity nghiệp vụ **không có setter công khai** | Luật E1 ở [`../../RULES.md`](../../RULES.md) — bộ chuyển đổi không gán được vào thứ không có setter |
| Đầu vào là **kiểu riêng cho từng lệnh**, không phải entity | Kiểu đầu vào chỉ khai đúng các trường endpoint đó nhận |
| Cập nhật một phần khai **danh sách trường được sửa** tường minh | Không lặp qua mọi thuộc tính có trong nội dung request |

Lớp thứ nhất là lớp mạnh nhất, vì nó được ép bằng test kiến trúc chứ không bằng kỷ luật.

---

## 6. Header bảo mật

| Header | Chống gì | Ghi chú cho API |
| --- | --- | --- |
| `Strict-Transport-Security` | Hạ cấp xuống HTTP | Bật ở môi trường thật |
| `X-Content-Type-Options: nosniff` | Trình duyệt tự đoán kiểu nội dung | Quan trọng với endpoint tải file |
| `Content-Security-Policy` | Chèn script | Chủ yếu cho trang HTML; với API, đặt chính sách chặn hết |
| `X-Frame-Options` / chỉ thị khung trong CSP | Bị nhúng trong khung để lừa bấm | |
| `Referrer-Policy` | Rò đường dẫn nội bộ sang site khác | |
| `Cache-Control: no-store` cho phản hồi có dữ liệu riêng tư | Dữ liệu người này còn trong bộ đệm khi người khác dùng chung máy | Hay bị bỏ sót |

Header là biện pháp rẻ nhất trong toàn bộ file này: đặt một lần ở middleware, không ảnh hưởng nghiệp vụ.

---

## 7. Quản lý bí mật

| Quy tắc | Chi tiết |
| --- | --- |
| **Không bao giờ trong source** | Luật S6 ở [`../../RULES.md`](../../RULES.md), canh bằng công cụ quét bí mật trong CI |
| Máy lập trình viên | Cơ chế lưu bí mật ngoài thư mục repo của nền tảng phát triển |
| Môi trường thật | Biến môi trường hoặc kho bí mật; **không** nằm trong file cấu hình được commit |
| Xoay vòng | Phải làm được **mà không cần build lại**. Không đạt được thì trên thực tế sẽ không ai xoay |
| Rò rỉ rồi | Coi như đã lộ vĩnh viễn: **thu hồi và thay**, không chỉ xoá khỏi lịch sử git |

**Bẫy:** đặt bí mật vào file cấu hình rồi thêm file đó vào danh sách bỏ qua của git. Nó vẫn nằm trên đĩa mọi máy, vẫn bị chép vào bản sao lưu, và người mới sẽ hỏi xin qua kênh chat.

---

## 8. Tiêm SQL với EF Core — và chỗ vẫn rò

Truy vấn viết bằng LINQ được tham số hoá; đó là lý do tiêm SQL hiếm khi xuất hiện ở dự án dùng EF Core. Nhưng **hiếm không phải không có** — bốn chỗ vẫn rò:

| Chỗ rò | Vì sao |
| --- | --- |
| **SQL thô ghép chuỗi** | Ghép giá trị người dùng vào câu lệnh là tiêm SQL kinh điển. Dạng nội suy chuỗi của EF Core thì an toàn vì nó tự tham số hoá — nhưng chỉ khi dùng đúng kiểu tham số; ghép trước rồi truyền vào thì không cứu được |
| **Tên cột hoặc chiều sắp xếp lấy từ tham số truy vấn** | Tên định danh **không tham số hoá được**. Đây là chỗ rò phổ biến nhất trong thực tế: một lưới cho phép sắp xếp theo cột, và tên cột đi thẳng vào câu lệnh | 
| **Bộ lọc động ghép chuỗi** | Cùng lý do |
| **Hàm và thủ tục trong DB** | Bên trong chúng có thể ghép chuỗi mà EF Core không biết |

**Cách xử lý tên cột:** ánh xạ từ giá trị client gửi lên sang một danh sách cột **cho phép** đã khai sẵn. Giá trị không nằm trong danh sách thì từ chối. Không bao giờ đưa chuỗi client gửi vào câu lệnh, kể cả sau khi "làm sạch".

Cùng nguyên tắc áp cho chiều sắp xếp và cho tên bảng.

---

## 9. Tải file lên

Đây là bề mặt tấn công lớn nhất trong nhóm chức năng thông thường, vì nó nhận dữ liệu tuỳ ý **và** lưu lại.

| Biện pháp | Chi tiết |
| --- | --- |
| **Giới hạn kích thước** | Ở cả tầng máy chủ web và tầng ứng dụng. Thiếu tầng đầu thì nội dung lớn đã được nhận xong mới bị từ chối |
| **Danh sách phần mở rộng cho phép** | Không dùng danh sách chặn |
| **Kiểm nội dung thật, không tin phần mở rộng** | Đọc chữ ký đầu file. Một file thực thi đổi đuôi vẫn là file thực thi |
| **Không tin tên file client gửi** | Sinh tên mới. Tên client gửi có thể chứa đường dẫn tương đối để thoát ra thư mục khác, hoặc ký tự đặc biệt của hệ điều hành |
| **Lưu ngoài thư mục được phục vụ tĩnh** | File tải lên không được nằm ở nơi máy chủ web có thể trả về trực tiếp |
| **Phục vụ qua endpoint có kiểm quyền** | Kèm header buộc tải xuống thay vì hiển thị, và kiểu nội dung xác định — không lấy từ file |
| **Quét mã độc** | Bắt buộc khi file được chia sẻ giữa người dùng |

Chi tiết về lưu trữ, dọn file mồ côi và file tạm: [`14-file-storage.md`](14-file-storage.md).

**Bẫy đặc thù của tệp bảng tính:** một tệp CSV có ô bắt đầu bằng dấu bằng có thể được phần mềm bảng tính diễn giải như công thức khi người khác mở. Đây là lỗ hổng của phía **xuất** dữ liệu, không phải phía nhập — xem [`15-import-export.md`](15-import-export.md).

---

## 10. Quyền của chính ứng dụng

Bảo mật không chỉ là chặn người dùng — nó còn là giới hạn thiệt hại khi ứng dụng bị chiếm quyền.

| Chỗ | Nguyên tắc quyền tối thiểu |
| --- | --- |
| **Tài khoản DB của ứng dụng** | Không dùng tài khoản chủ sở hữu. Ứng dụng cần đọc/ghi dữ liệu, **không** cần quyền sửa lược đồ ở môi trường thật — vì migration chạy tay bằng một tài khoản khác. Xem [`13-core-data-migration.md`](13-core-data-migration.md) §3 |
| **Bảng nhật ký kiểm toán** | Ứng dụng chỉ cần quyền ghi thêm. Không cần quyền sửa hay xoá — đây là cách ép tính bất biến mạnh nhất, xem [`10-data-retention.md`](10-data-retention.md). Luật M13 |
| **Bảng danh mục quyền** | Ứng dụng chỉ đọc — dòng danh mục vào DB bằng migration, xem [`13-core-data-migration.md`](13-core-data-migration.md) §5.1. Luật M13 |
| **Thư mục file** | Quyền ghi đúng thư mục cần, không phải cả ổ đĩa |
| **Kết nối ra ngoài** | Chỉ tới các đích cần thiết, nếu hạ tầng cho phép giới hạn |

Dòng đầu và dòng thứ hai là hai chỗ hiếm khi được làm, và cả hai đều gần như miễn phí — chúng chỉ là cấu hình quyền trên DB, không phải code.

---

## 11. Phụ thuộc bên thứ ba

Phần lớn mã chạy trên môi trường thật không phải mã của nhóm viết ra. Một lỗ hổng trong một thư viện là lỗ hổng của hệ thống.

| Việc | Chi tiết |
| --- | --- |
| **Quét lỗ hổng đã biết trong CI** | Cho cả gói backend và gói frontend. Cổng chặn khi có lỗ hổng mức cao |
| **Ghim phiên bản** | Bản dựng phải tái lập được. Phiên bản trôi tự do nghĩa là bản dựng hôm nay và hôm qua có thể khác nhau mà không ai biết |
| **Nhịp nâng cấp đều đặn** | Nâng cấp gộp một lần sau hai năm là việc không ai dám làm. Nâng đều thì mỗi lần nhỏ |
| **Cân nhắc trước khi thêm một gói mới** | Một gói làm một việc nhỏ mà kéo theo cả cây phụ thuộc là một đánh đổi tệ |

Việc quét lỗ hổng có một cách hỏng đặc trưng: cảnh báo nhiều quá thì mọi người bỏ qua tất cả. Cần phân mức và cần một chính sách rõ về việc chấp nhận có thời hạn cho những cảnh báo không áp dụng được — chấp nhận **có ghi chép và có hạn**, không phải tắt cảnh báo.

---

## 12. Khi có sự cố

Chuẩn bị trước ba thứ, vì lúc xảy ra thì không có thời gian nghĩ:

1. **Ai được báo và bằng đường nào.** Không có danh sách thì thông tin đi lòng vòng.
2. **Cách thu hồi hàng loạt.** Đẩy toàn bộ phiên ra, buộc đổi mật khẩu, thu hồi khoá — phải làm được, và phải từng thử.
3. **Log đủ để trả lời "chuyện gì đã xảy ra".** Đây là lúc nhật ký kiểm toán ở [`10-data-retention.md`](10-data-retention.md) và mã lần gọi ở [`07-observability.md`](07-observability.md) trả cổ tức.

Và sau sự cố: một mục ở [`../../audit/`](../../audit/) ghi lại **cổng nào lẽ ra phải bắt được**. Không có bước đó, cùng một sự cố sẽ lặp lại.

---

## 13. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Antiforgery hai lớp | ✅ sẽ có | Bắt buộc vì phiên đi bằng cookie |
| Tên cookie khai một chỗ + test chống trùng tên | ✅ sẽ có | Luật ở [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §7.5 |
| CORS allowlist theo môi trường, kiểm lúc khởi động | ✅ sẽ có | |
| Giới hạn tần suất theo IP **và** theo tài khoản đăng nhập (mã đơn vị + tên đăng nhập) | ✅ sẽ có | [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §6 |
| Chống dò tài khoản: thông điệp, thời gian, mã trạng thái | ✅ sẽ có | Cả ba đường |
| Kiểm đầu vào ở biên, giới hạn kích thước mọi chiều | ✅ sẽ có | |
| Chống gán tràn: entity không setter công khai, kiểu đầu vào riêng | ✅ sẽ có | Luật E1 |
| Header bảo mật | ✅ sẽ có | Middleware, một chỗ |
| Quét bí mật trong CI | ✅ sẽ có | Luật S6 |
| Danh sách cột cho phép khi sắp xếp và lọc | ✅ sẽ có | §8 |
| Kiểm chữ ký file khi tải lên | ✅ sẽ có | |
| **Quét mã độc cho file tải lên** | ❌ chưa ở v1 | Cần một dịch vụ quét. Bắt buộc trước khi file được chia sẻ giữa người dùng |
| **Giới hạn tần suất chia sẻ giữa nhiều instance** | ❌ chưa | Điều kiện bắt buộc phải giải trước khi chạy instance thứ hai |
| **Tường lửa ứng dụng web** | ❌ chưa | Thuộc hạ tầng, không thuộc Core |
| **Kiểm thử xâm nhập** | ❌ chưa | Nên làm một lần trước khi mở ra ngoài mạng nội bộ |
