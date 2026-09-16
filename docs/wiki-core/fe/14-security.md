---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 14. Bảo mật phía Frontend

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Xác thực và phiên: [`07-auth-identity.md`](07-auth-identity.md). Bảo mật phía BE: [`../be/09-security-beyond-auth.md`](../be/09-security-beyond-auth.md). Header bảo mật đặt ở đâu: [`17-phuc-vu-va-trien-khai.md`](17-phuc-vu-va-trien-khai.md) §5.

---

## 1. Câu đầu tiên phải nói rõ: FE không phải lớp bảo vệ

Mọi thứ trong bundle FE đều **công khai**: mã nguồn đọc được, mọi kiểm tra bỏ qua được, mọi giá trị nhúng vào lấy ra được.

Vì vậy vai của FE trong bảo mật gồm đúng ba việc:

| Vai | Cụ thể |
| --- | --- |
| **Không tự tạo ra lỗ hổng** | XSS qua render nội dung không tin cậy, chuyển hướng mở, rò rỉ dữ liệu qua lưu trữ trình duyệt |
| **Không rò rỉ thứ không nên có ở đây** | Secret trong bundle, dữ liệu nhạy cảm để lại trong bộ nhớ trình duyệt |
| **Không cản trở lớp bảo vệ thật** | Kiểm quyền, kiểm dữ liệu, giới hạn tần suất — tất cả ở BE |

Phép thử để phân biệt việc của FE với việc của BE:

> **"Nếu ai đó xoá đoạn code này khỏi bundle thì họ làm được gì thêm?"**
>
> Trả lời "không gì cả" → đây là việc của FE (trải nghiệm). Trả lời "họ đọc/sửa được dữ liệu" → **BE đang thiếu một kiểm tra**, và đó là lỗi phải sửa ở BE.

---

## 2. Kiểm quyền ở FE là giao diện, không phải bảo vệ

Nhắc lại từ [`07-auth-identity.md`](07-auth-identity.md) §5.3 vì đây là chỗ hay hiểu sai nhất.

Ẩn một nút, chặn một route, lọc một menu — cả ba đều **chỉ** để người dùng không bấm vào thứ sẽ bị từ chối. Không cái nào ngăn được ai gọi thẳng API.

Hệ quả cụ thể khi thi công:

| Đừng | Vì |
| --- | --- |
| Tải toàn bộ dữ liệu rồi lọc ở client theo quyền | Dữ liệu đã ở trong trình duyệt là đã lộ, dù không hiển thị |
| Trả về bản ghi đầy đủ rồi FE ẩn vài trường nhạy cảm | Mở tab Network là thấy hết |
| Giao cho FE quyết định thao tác nào được phép | Quyết định thuộc BE; FE chỉ hỏi |

---

## 3. Render nội dung không tin cậy — XSS

### 3.1 Angular bảo vệ mặc định, và chỗ bảo vệ bị tắt

Angular tự escape mọi giá trị nội suy vào template. Một chuỗi chứa thẻ HTML sẽ hiển thị **thành chữ**, không thành thẻ. Đây là mặc định đúng và không nên nghĩ tới nữa.

Bảo vệ chỉ mất khi có người tắt nó. Ba đường tắt:

| Đường | Rủi ro |
| --- | --- |
| Gán HTML thô vào một phần tử | Chuỗi có mã sẽ chạy |
| Đánh dấu một giá trị là "tin cậy" bằng API bỏ qua kiểm tra | Đúng chỗ tên API đã cảnh báo, và vẫn có người dùng nó cho tiện |
| Thao tác DOM trực tiếp | Đi vòng qua toàn bộ cơ chế của Angular |

### 3.2 Luật

> **Không dùng API bỏ qua kiểm tra bảo mật.** Cần một ngoại lệ thì nó phải có tên, có lý do, và có làm sạch nội dung trước.

Cần hiển thị HTML do người dùng nhập (nội dung từ trình soạn thảo, mô tả có định dạng) thì làm sạch phía **server** trước khi lưu, theo một danh sách thẻ cho phép. Làm sạch ở client là làm sạch ở nơi kẻ tấn công kiểm soát — vô nghĩa.

### 3.3 Ba nguồn nội dung không tin cậy hay bị quên

| Nguồn | Vì sao bị quên |
| --- | --- |
| **Tên tệp do người dùng tải lên** | Trông như dữ liệu hệ thống, thật ra do người dùng đặt |
| **Thông báo lỗi từ server có chứa dữ liệu người dùng** | "Email `<script>` đã tồn tại" — dữ liệu đi vòng qua BE rồi quay lại |
| **Tham số trên URL** | Hiển thị lại từ khoá tìm kiếm là chỗ XSS phản xạ cổ điển |

Cả ba đều an toàn nếu đi qua nội suy bình thường của Angular. Chúng chỉ nguy hiểm khi ai đó quyết định render chúng dưới dạng HTML.

### 3.4 Liên kết và chuyển hướng

| Việc | Luật |
| --- | --- |
| Đường dẫn quay lại sau đăng nhập | Chỉ chấp nhận đường dẫn nội bộ — [`07-auth-identity.md`](07-auth-identity.md) §2.2 |
| Liên kết ra ngoài mở tab mới | Phải khai thuộc tính chống truy cập ngược cửa sổ mở |
| Địa chỉ liên kết đến từ dữ liệu | Chỉ cho phép giao thức an toàn; chặn giao thức thực thi mã |

---

## 4. Content Security Policy

CSP là lớp phòng thủ **thứ hai**: khi có một lỗ XSS, CSP hạn chế thiệt hại bằng cách cấm trình duyệt chạy mã không nằm trong nguồn cho phép.

### 4.1 Đặt ở đâu

**Ở header HTTP, do máy chủ phục vụ đặt** — không đặt bằng thẻ trong tài liệu HTML. Thẻ meta không hỗ trợ đầy đủ mọi chỉ thị và không có tác dụng ở một số chế độ. Chi tiết: [`17-phuc-vu-va-trien-khai.md`](17-phuc-vu-va-trien-khai.md) §6.

### 4.2 Ba điều phải biết trước khi bật

| Điều | Cụ thể |
| --- | --- |
| **Angular sinh style nội tuyến** | Một chính sách cấm style nội tuyến sẽ làm vỡ giao diện. Phải xử lý đúng cách, không phải nới chính sách tới mức vô nghĩa |
| **Bật ở chế độ chỉ báo cáo trước** | Bật thẳng chế độ chặn trên môi trường thật là cách làm trắng ứng dụng cho toàn bộ người dùng cùng lúc |
| **Chính sách phải kiểm được** | Một chính sách cho phép mọi nguồn là một chính sách không bảo vệ gì; nó chỉ tạo cảm giác an toàn |
| **Phải khai origin của API** | FE và API ở **hai origin khác nhau** ([`../../adr/0015-fe-va-api-khac-nguon.md`](../../adr/0015-fe-va-api-khac-nguon.md)), nên chỉ thị điều khiển đích kết nối phải liệt kê origin của API. Quên nó thì **mọi lời gọi API bị trình duyệt chặn** ngay khi CSP chuyển sang chế độ chặn — và triệu chứng là app trắng, không phải một lỗi mạng dễ đọc |
| **Script nội tuyến duy nhất được phép là script theme** | `index.html` có đúng một script nội tuyến — đặt `data-theme` trước paint; `script-src` cho phép nó qua **hash** `'sha256-…'`, **không** `'unsafe-inline'`. Nội dung script và luật đi kèm: [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §3.5 |

### 4.3 Ba header đi kèm

Cùng đặt ở tầng phục vụ: chặn trình duyệt tự đoán kiểu nội dung; kiểm soát việc nhúng ứng dụng vào khung của trang khác; và kiểm soát thông tin nguồn gửi kèm khi đi ra ngoài.

---

## 5. Không có secret trong bundle

### 5.1 Luật

> **Mọi thứ nhúng vào bundle là công khai.** Không có "biến môi trường bí mật" ở FE.

Cái bẫy là từ "biến môi trường": ở BE nó có thể là secret; ở FE nó chỉ là **một giá trị được nhúng vào tệp JavaScript lúc build**. Người ta mang thói quen từ BE sang và nhúng nhầm.

### 5.2 Cái gì được và không được

| Được nhúng | Không được nhúng |
| --- | --- |
| Đường dẫn API, cờ bật/tắt tính năng | Chuỗi kết nối, mật khẩu, khoá riêng |
| Khoá công khai đã thiết kế để công khai | Khoá API của dịch vụ bên thứ ba tính phí theo lượt gọi |
| Phiên bản, mã build | Bất kỳ thứ gì mà biết nó thì làm được gì đó |

Với dòng thứ hai bên phải: cần gọi dịch vụ bên thứ ba có tính phí thì **BE gọi hộ**, FE gọi BE. Đây không phải sự cẩn thận thừa — một khoá API bị lộ trong bundle là một hoá đơn.

### 5.3 Ép bằng máy

Luật S6 của [`../../RULES.md`](../../RULES.md) quét secret ở CI, và phạm vi quét phải bao gồm **thư mục build ra**, không chỉ mã nguồn. Một giá trị chỉ xuất hiện sau khi build (do nội suy lúc build) sẽ không bị quét mã nguồn phát hiện.

**Lịch sử Git cũng là bề mặt lộ.** Một secret đã commit rồi xoá vẫn nằm trong lịch sử. Cách xử lý duy nhất đúng là **thu hồi và cấp lại** secret đó, không phải xoá dòng code.

---

## 6. `localStorage` — dùng cho gì và không dùng cho gì

### 6.1 Bản chất

`localStorage` là bộ nhớ **bất kỳ JavaScript nào chạy trên trang đều đọc được**, tồn tại vô hạn, không hết hạn, không xoá khi đóng trình duyệt, và không xoá khi đăng xuất trừ khi có ai đó viết code xoá.

### 6.2 Bảng quyết định

| Dùng cho | Không dùng cho | Vì sao không |
| --- | --- | --- |
| Chế độ sáng/tối | Token, khoá phiên | Một lỗ XSS đọc được hết. Đây là lý do chọn cookie `HttpOnly` ở [`07-auth-identity.md`](07-auth-identity.md) §1 |
| Ngôn ngữ đã chọn | Dữ liệu cá nhân, hồ sơ người dùng | Máy dùng chung: người sau đọc được dữ liệu người trước |
| Cột đang ẩn, độ rộng cột | Nội dung nghiệp vụ tải về | Bản sao dữ liệu nằm ngoài mọi kiểm soát truy cập |
| Trạng thái thu gọn của sidebar | Bất cứ thứ gì cần hết hạn | Không có cơ chế hết hạn |
| Cờ đồng bộ đăng xuất giữa các tab | | Đây là ngoại lệ có lý do — [`07-auth-identity.md`](07-auth-identity.md) §7.3 |

**Quy tắc rút gọn:** lưu **tuỳ chọn hiển thị của một máy**, không lưu **dữ liệu của một người**.

### 6.3 Ba luật kỹ thuật

1. **Mọi lần đọc/ghi phải bọc chống lỗi.** Ở chế độ riêng tư hoặc khi người dùng chặn lưu trữ, chính lời gọi có thể ném lỗi. Không bọc thì app trắng trang vì một tuỳ chọn hiển thị.
2. **Không tin dữ liệu đọc ra.** Người dùng sửa được `localStorage`. Một giá trị lạ phải suy giảm êm về mặc định, không làm vỡ màn hình.
3. **Đăng xuất phải dọn phần thuộc về người dùng**, giữ phần thuộc về máy — [`07-auth-identity.md`](07-auth-identity.md) §7.2.

### 6.4 Dữ liệu nhạy cảm trong bộ nhớ

Ngay cả biến trong bộ nhớ cũng cần chú ý ở vài chỗ:

| Việc | Vì sao |
| --- | --- |
| Không log object chứa dữ liệu người dùng | Log ở lại trong console, và có thể đi vào báo cáo lỗi — [`10-observability.md`](10-observability.md) §4.1 |
| Xoá mật khẩu khỏi state ngay sau khi gửi | Giữ lại không có ích gì và nằm trong ảnh chụp bộ nhớ |
| Không đưa dữ liệu nhạy cảm lên URL | URL đi vào lịch sử trình duyệt, log của proxy, và tiêu đề trang |

Dòng cuối đáng nhấn mạnh vì nó mâu thuẫn biểu kiến với [`11-grid-and-metadata.md`](11-grid-and-metadata.md) §3 (đặt trạng thái bảng lên URL). Không mâu thuẫn thật: **trang, sắp xếp, bộ lọc là siêu dữ liệu; giá trị nhạy cảm thì không được lên URL.** Một bộ lọc theo số căn cước thuộc loại thứ hai.

---

## 7. Phụ thuộc

| Việc | Ghi chú |
| --- | --- |
| Quét lỗ hổng phụ thuộc định kỳ | Một lệnh; chạy trong CI |
| Xử lý mức nghiêm trọng cao trước | Không phải mọi cảnh báo đều đáng gấp: lỗ hổng ở công cụ build khác với lỗ hổng trong mã chạy trên trình duyệt |
| Không thêm phụ thuộc cho việc nhỏ | Mỗi phụ thuộc là một bề mặt tấn công và một khoản nợ nâng cấp |
| Ghim phiên bản, commit tệp khoá | Không có tệp khoá thì hai lần build cho ra hai cây phụ thuộc khác nhau |

Khi bản vá đòi nâng major mà thư viện UI chưa sẵn sàng: xử lý bằng cách ghi đè phiên bản cho đúng gói bắc cầu bị ảnh hưởng, **không** nâng cả framework. Ghi lý do ngay cạnh chỗ ghi đè — thứ đó phải được gỡ khi nâng thật, và không ai nhớ nếu không viết. Xem [`16-nen-tang-va-nang-cap.md`](16-nen-tang-va-nang-cap.md) §3.

---

## 8. Kiểm chứng

- [ ] Không chỗ nào trong `src/app` dùng API bỏ qua kiểm tra bảo mật của Angular, hoặc mỗi chỗ dùng đều có lý do và có làm sạch
- [ ] Không chỗ nào gán HTML thô từ dữ liệu server hay từ URL
- [ ] Đường dẫn quay lại trỏ ra miền ngoài bị từ chối
- [ ] Liên kết ra ngoài có thuộc tính chống truy cập ngược cửa sổ
- [ ] CSP đặt ở header, đã chạy qua chế độ chỉ báo cáo trước khi chặn
- [ ] Quét secret trên **thư mục build ra**, không chỉ mã nguồn — không kết quả
- [ ] `localStorage` chỉ chứa tuỳ chọn hiển thị; không token, không dữ liệu người dùng
- [ ] Mọi truy cập `localStorage` được bọc chống lỗi và chịu được giá trị hỏng
- [ ] Đăng xuất dọn dữ liệu người dùng, giữ tuỳ chọn của máy
- [ ] Không log nào chứa nội dung ô nhập hay object người dùng

---

## 9. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Không dùng API bỏ qua kiểm tra bảo mật của Angular | ✅ sẽ có | §3.2 |
| Kiểm đường dẫn quay lại sau đăng nhập | ✅ sẽ có | §3.4 — chống chuyển hướng mở |
| Bốn header bảo mật đặt ở proxy | ✅ sẽ có | [`17-phuc-vu-va-trien-khai.md`](17-phuc-vu-va-trien-khai.md) §6 |
| Quét secret trên **thư mục build ra** | ✅ sẽ có | Luật S6 — §5.3 |
| `localStorage` chỉ giữ tuỳ chọn hiển thị, bọc chống lỗi | ✅ sẽ có | §6 |
| Quét lỗ hổng phụ thuộc trong CI | ✅ sẽ có | §7 |
| CSP ở chế độ chặn | ❌ chưa | Điều kiện: đã chạy chế độ chỉ báo cáo trên môi trường thật và xử hết báo cáo (§4.2) |
| Làm sạch HTML do người dùng nhập | ❌ chưa | Điều kiện: có trình soạn thảo đa dạng thức — và làm ở **BE**, không ở client (§3.2) |
| Ghim băm cho tài nguyên bên thứ ba | ❌ chưa | Điều kiện: có tài nguyên nạp từ nguồn ngoài |
| Giữ token hoặc dữ liệu người dùng trong `localStorage` | ❌ loại, không hoãn `K40` | §6.2 |
| Tải dữ liệu đầy đủ rồi lọc theo quyền ở client | ❌ loại, không hoãn `K41` | §2 — dữ liệu đã ở trong trình duyệt là đã lộ |
| Coi kiểm quyền ở FE là lớp bảo vệ | ❌ loại, không hoãn `K42` | §1 — phép thử ở cuối §1 |

> Cách đọc ba ký hiệu của bảng trên — và khi nào *"FE thiếu X"* là finding: [`../README.md`](../README.md) §9.

---

## 10. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Phiên, XSRF, hết phiên | [`07-auth-identity.md`](07-auth-identity.md) |
| Bảo mật phía BE | [`../be/09-security-beyond-auth.md`](../be/09-security-beyond-auth.md) |
| Header bảo mật và reverse proxy | [`17-phuc-vu-va-trien-khai.md`](17-phuc-vu-va-trien-khai.md) |
| Không rò rỉ qua báo cáo lỗi | [`10-observability.md`](10-observability.md) §4.1 |
| Cái gì được commit | [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) |
| Luật S6 | [`../../RULES.md`](../../RULES.md) §6 |
