---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 17. Phục vụ và triển khai Frontend

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`, chưa có cấu hình triển khai nào.
>
> Cái gì được commit và cái gì là artifact build: [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md). Bảo mật liên quan: [`14-security.md`](14-security.md).

---

## 1. Bản build FE là gì

Kết quả build là một thư mục **tệp tĩnh**: một tài liệu HTML gốc, các tệp JavaScript và CSS có mã băm trong tên, và tài nguyên (font, ảnh, tệp dịch).

Hai hệ quả quan trọng:

| Hệ quả | Nghĩa là |
| --- | --- |
| Không cần runtime để phục vụ | Bất kỳ máy chủ tệp tĩnh nào cũng phục vụ được. Không cần Node ở môi trường chạy |
| **Không có tầng nào để giấu bí mật** | Mọi thứ trong thư mục đó công khai — [`14-security.md`](14-security.md) §5 |

---

## 2. SPA fallback — điều kiện tối thiểu để routing hoạt động

### 2.1 Vấn đề

Ứng dụng một trang định tuyến ở phía trình duyệt. Máy chủ chỉ có một tài liệu HTML gốc; nó **không** có tệp nào tương ứng với một đường dẫn của ứng dụng.

Điều đó không gây vấn đề khi người dùng đi lại trong app (router xử lý, không có request nào tới máy chủ). Nó gây vấn đề ở đúng ba tình huống:

1. Tải lại trang khi đang ở một màn bất kỳ
2. Mở một đường link được gửi cho mình
3. Mở một dấu trang đã lưu

Cả ba đều gửi một request thật tới máy chủ cho một đường dẫn không tồn tại → 404.

### 2.2 Luật

> **Mọi đường dẫn không khớp tệp thật đều trả về tài liệu HTML gốc, với mã trạng thái 200.**

### 2.3 Ba bẫy

| Bẫy | Hậu quả |
| --- | --- |
| Fallback bắt cả tệp tĩnh không tồn tại | Một tệp thiếu trả về HTML với mã 200, trình duyệt cố dùng nó làm ảnh hoặc script, và lỗi thật bị che |
| Trả fallback với mã 404 thay vì 200 | Một số công cụ và bộ nhớ đệm xử lý khác đi; và bản thân việc này là nói dối về trạng thái |

Thứ tự đúng của quy tắc phục vụ: (1) tệp tĩnh tồn tại → trả tệp; (2) còn lại → trả HTML gốc.

> Máy chủ FE **không** có tiền tố API nào để chuyển tiếp — API ở origin riêng (§4.1). Bẫy *"fallback bắt luôn tiền tố API"* chỉ tồn tại ở mô hình cùng nguồn, tức phương án đã loại ở [`../../adr/0015-fe-va-api-khac-nguon.md`](../../adr/0015-fe-va-api-khac-nguon.md).

---

## 3. Cache header — hai loại tệp, hai chính sách ngược nhau

Đây là chỗ **một cấu hình sai gây ra loại lỗi tệ nhất**: người dùng chạy phiên bản cũ mà không biết, và tải lại trang cũng không sửa được.

| Loại tệp | Chính sách | Vì sao an toàn / cần thiết |
| --- | --- | --- |
| Tệp có **mã băm trong tên** (JS, CSS, font) | Cache rất lâu, đánh dấu bất biến | Nội dung đổi → tên đổi → URL khác. Không bao giờ có chuyện tệp cũ mang tên mới |
| **Tài liệu HTML gốc** | **Không cache**, luôn hỏi lại máy chủ | Nó là tệp chứa danh sách tên các tệp băm. Cache nó là ghim toàn bộ phiên bản cũ |
| Tệp dịch, tệp cấu hình chạy | Cache ngắn hoặc kiểm lại mỗi lần | Chúng đổi mà **không** đổi tên |

### 3.1 Vì sao HTML gốc là điểm mấu chốt

Chuỗi phụ thuộc là: HTML gốc → tên tệp băm → nội dung. Trình duyệt chỉ biết phiên bản mới tồn tại khi nó lấy được HTML gốc mới.

Cache HTML gốc, dù chỉ vài giờ, tạo ra tình huống: đã triển khai bản mới, một phần người dùng vẫn chạy bản cũ, và **tải lại trang không giúp gì** vì trình duyệt lấy HTML từ bộ nhớ đệm. Cách sửa duy nhất từ phía người dùng là xoá dữ liệu duyệt web — điều không thể hướng dẫn hàng loạt.

### 3.2 Tệp dịch là ngoại lệ hay bị quên

Tệp dịch được tải lúc chạy và tên **không** có mã băm. Cache dài thì sửa một câu chữ không có tác dụng cho người đã truy cập. Cache header cho chúng phải theo nhóm thứ ba, không theo nhóm thứ nhất.

### 3.3 Triển khai giữa chừng

Người dùng đang mở app cũ, một bản mới được triển khai, các tệp băm cũ có thể không còn trên máy chủ. Khi họ điều hướng tới một nhánh lazy chưa tải, request tệp đó trả 404 và **màn hình không mở được**.

**Cách giảm nhẹ đã chốt cho v1: giữ lại tệp của bản trước một thời gian sau khi triển khai.** Nó là việc của tầng phục vụ, không cần dòng code FE nào.

**Bắt lỗi tải chunk để hiện banner mời tải lại trang — chốt 2026-09-11 là KHÔNG làm ở v1.** Người dùng gặp thì tự tải lại trang. Cái giá phải chấp nhận: người xui nhất thấy một thao tác không phản ứng hoặc một màn trắng, và không có gì giải thích cho họ. Điều kiện xem lại: khi nhịp triển khai đủ dày để chuyện này chạm tới người dùng thật.

---

## 4. Chạy sau reverse proxy

### 4.1 FE và API ở HAI nguồn khác nhau

> **FE và API phục vụ ở hai origin khác nhau** — dev là hai cổng, prod là hai subdomain **của cùng một tên miền gốc**. Reverse proxy phục vụ tệp tĩnh của FE; API có origin riêng.
>
> 📖 Chuỗi ràng buộc kéo theo — CORS allowlist, cookie `SameSite=Lax`, antiforgery hai lớp: đọc [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §7. Đó là file chủ; mục này **không** chép lại.

Bốn cái giá phải trả, ghi ra để không ai tưởng chúng là chi tiết vặt:

| Cái giá | Nghĩa là |
| --- | --- |
| Phải cấu hình CORS đúng ở **mọi** môi trường | Sai một chỗ là hỏng đăng nhập, và triệu chứng không nói ra nguyên nhân |
| FE và API phải cùng tên miền gốc **và cùng scheme** | Để cookie phiên còn là cùng site. FE `http` mà API `https` là khác site — cookie không được gửi ⇒ **HTTPS ở mọi môi trường, kể cả máy lập trình viên** |
| Phải có cơ chế biết đường dẫn API | Xem §5 — quyết định ở đó phụ thuộc trực tiếp vào mục này |
| CSP phải khai thêm nguồn cho phép | Một dòng nữa phải đúng, và nó chỉ sai lúc chạy thật |

**Đổi lại:** không cần dựng reverse proxy ở mọi môi trường, và FE triển khai độc lập với BE.

### 4.2 Môi trường phát triển phải giống

Ở máy dev, máy chủ dev và API ở hai cổng khác nhau — **đúng hình dạng của môi trường thật** (§4.1). Vì vậy dev **bật CORS và chạy HTTPS**, không dùng proxy của máy chủ dev để giấu ranh giới đi.

Vì sao: mọi bẫy của cookie giữa hai origin — lệch scheme thành khác site, allowlist thiếu một origin, antiforgery không qua được — chỉ lộ ra khi dev chạy đúng cấu hình của thật. Dùng proxy dev để né chúng nghĩa là dời toàn bộ nhóm lỗi đó sang lần triển khai đầu tiên.

### 4.3 Ứng dụng không nằm ở gốc miền

Nếu app phục vụ dưới một đường dẫn con, phải khai đường dẫn cơ sở lúc build — nếu không mọi tài nguyên sẽ được yêu cầu từ gốc miền và không tìm thấy.

Đây là một quyết định phải chốt **trước** khi build, không sửa được sau khi đã có artifact. Mặc định của repo này: **phục vụ ở gốc miền**; nếu một dự án cần khác, đó là một biến thể phải khai tường minh.

### 4.4 Nén

Nén tệp văn bản ở tầng proxy. Nén trước lúc build và để proxy phục vụ tệp đã nén là tốt hơn nữa (nén một lần thay vì nén mỗi request). Ảnh và font đã nén sẵn thì không nén lại — tốn CPU mà không giảm được gì.

---

## 5. Cấu hình: lúc build hay lúc chạy

### 5.1 Hai mô hình

| | Nhúng lúc build | Đọc lúc chạy |
| --- | --- | --- |
| Cách làm | Giá trị nội suy vào bundle | Một tệp cấu hình được tải trước khi app khởi động |
| Số artifact | **Một artifact cho mỗi môi trường** | **Một artifact cho mọi môi trường** |
| Đổi giá trị | Build lại và triển khai lại | Sửa một tệp, tải lại trang |
| Rủi ro | Build cho môi trường A đem triển khai lên B là sai âm thầm | Thêm một request chặn lúc khởi động |

### 5.2 Quyết định cho v1

> **Đường dẫn API là giá trị BẮT BUỘC phải cấu hình.** Vì FE và API khác nguồn (§4), FE không suy ra được đường dẫn API từ nguồn của chính nó.

Đây là hệ quả trực tiếp của §4, không phải một lựa chọn riêng: chọn khác nguồn là chọn luôn việc phải có chỗ khai đường dẫn API.

Những gì còn lại (chế độ build, phiên bản hiển thị) nhúng lúc build là đủ.

> **Chốt cho v1: nhúng lúc build.** `apiBaseUrl` nằm ở `environments/*.ts` ([`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md)), mỗi môi trường một artifact. Hình dạng giá trị: [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.1.
>
> Cái giá phải canh: build cho môi trường A đem triển khai lên B là sai **âm thầm** — FE gọi nhầm API. Artifact đặt tên theo môi trường, và bước kiểm sau triển khai xác nhận trang vừa lên gọi đúng API.

**Vì sao ghi quyết định này ra dù nó là "không làm gì":** đây chính xác là loại quyết định mà sáu tháng sau có người sẽ hỏi *"sao không có config runtime?"* và tự thêm vào. Ghi ra để câu trả lời có sẵn, kèm điều kiện mở lại.

### 5.3 Điều kiện mở lại

Cần cấu hình runtime khi thoả **một** trong hai:

1. Cần đổi đường dẫn API mà không build lại — ví dụ một artifact dùng cho nhiều môi trường.
2. Cần đổi một giá trị mà không được build lại (cờ bật/tắt tính năng theo môi trường).

> Điều kiện *"FE và API ở hai nguồn khác nhau"* **không còn là điều kiện** — nó luôn đúng (§4.1). Thứ còn phải chọn là **nhúng lúc build hay đọc lúc chạy**, không phải *có cần cấu hình hay không*.

Khi làm, hai ràng buộc: tệp cấu hình **không cache** (§3), và app phải xử lý được trường hợp tải cấu hình thất bại — bằng một màn lỗi có nghĩa, không phải màn trắng.

---

## 6. Header bảo mật đặt ở tầng phục vụ

Bốn header đặt ở proxy, không đặt trong ứng dụng:

| Header | Việc |
| --- | --- |
| Chính sách nội dung (CSP) | Giới hạn nguồn được phép chạy mã — [`14-security.md`](14-security.md) §4 |
| Chặn đoán kiểu nội dung | Không để trình duyệt tự suy diễn kiểu tệp |
| Kiểm soát nhúng vào khung | Chống bị nhúng vào trang khác để lừa thao tác |
| Kiểm soát thông tin nguồn gửi kèm | Không rò rỉ đường dẫn nội bộ khi người dùng đi ra ngoài |

**Vì sao ở proxy chứ không trong ứng dụng:** ứng dụng tĩnh không có chỗ nào để đặt header. Thẻ trong tài liệu HTML chỉ hỗ trợ một phần và không thay được header thật.

Hệ quả tổ chức phải nói rõ: **cấu hình proxy là một phần của sản phẩm**, không phải việc riêng của người vận hành. Nó phải nằm trong kho mã, được review, và đi cùng phiên bản.

---

## 7. Kiểm tra sức khoẻ và chẩn đoán

| Việc | Ghi chú |
| --- | --- |
| Một endpoint kiểm tra sức khoẻ tĩnh | Để hệ thống giám sát biết tầng tệp tĩnh còn sống |
| Hiển thị phiên bản build ở đâu đó trong app | Khi người dùng báo lỗi, câu đầu tiên là "bạn đang chạy bản nào" |
| Không bật bản đồ mã nguồn công khai ở môi trường thật | Nó phơi toàn bộ mã nguồn có cấu trúc. Sinh ra và giữ riêng cho việc chẩn đoán thì được |

Dòng thứ hai rẻ và có ích bất ngờ: nó phân biệt được *"lỗi đã sửa nhưng người dùng chưa nhận bản mới"* (vấn đề cache, §3) với *"lỗi chưa sửa"* — hai tình huống trông giống hệt nhau từ phía người báo lỗi.

---

## 8. Quy trình triển khai

Thứ tự đúng, và lý do của thứ tự:

```
1. Chạy cổng FE  ────► đỏ thì dừng, chưa build gì cả
2. Build         ────► artifact có mã băm
3. Đưa tệp tĩnh lên trước
4. Chuyển proxy sang bản mới
5. Kiểm nhanh: mở app, tải lại ở một màn sâu, đăng nhập
```

Bước 3 trước bước 4 là để tránh khoảng thời gian mà HTML gốc mới đã phục vụ nhưng tệp băm mới chưa có.

**Hai thứ phải kiểm ở bước 5, vì chúng chỉ hỏng ở môi trường thật:** tải lại trang ở một màn sâu (kiểm SPA fallback, §2) và đăng nhập (kiểm cookie và XSRF sau proxy, §4).

**Quay lui phải làm được.** Giữ artifact của bản trước để quay lui là đổi proxy về, không phải build lại từ mã nguồn.

---

## 9. Kiểm chứng

- [ ] Tải lại trang ở một màn sâu → hiển thị đúng màn đó, không phải 404
- [ ] Gọi một endpoint API không tồn tại → nhận 404 JSON, **không** phải HTML gốc
- [ ] Một tệp tĩnh không tồn tại → 404, không phải HTML gốc với mã 200
- [ ] Header cache: tệp băm cache lâu; **HTML gốc không cache**; tệp dịch cache ngắn
- [ ] Triển khai một bản mới → người dùng đang mở app nhận bản mới sau khi tải lại
- [ ] Đăng nhập chạy được sau proxy; cookie được gửi kèm request
- [ ] Bốn header bảo mật có mặt trong phản hồi
- [ ] Không có bản đồ mã nguồn công khai ở môi trường thật
- [ ] Phiên bản build hiển thị được trong app
- [ ] Quay lui về bản trước làm được mà không cần build lại

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| SPA fallback theo đúng thứ tự ba quy tắc | ✅ sẽ có | §2.3 |
| Cache header cho ba nhóm tệp | ✅ sẽ có | §3 — tài liệu HTML gốc **không** cache |
| FE và API ở hai origin, CORS allowlist khai đúng ở mọi môi trường | ✅ sẽ có | §4.1 — ràng buộc kéo theo ở file chủ BE |
| HTTPS ở mọi môi trường, kể cả máy lập trình viên | ✅ sẽ có | §4.1 — lệch scheme là khác site |
| Đã chọn và khai cơ chế cấu hình đường dẫn API | ✅ đã chốt — nhúng lúc build | §5.2 |
| Cấu hình proxy nằm trong kho mã, được review | ✅ sẽ có | §6 |
| Hiển thị phiên bản build trong app | ✅ sẽ có | §7 — phân biệt "chưa nhận bản mới" với "lỗi chưa sửa" |
| Giữ tệp của bản trước sau khi triển khai | ✅ sẽ có | §3.3 |
| **Bắt lỗi tải chunk + banner "có bản mới"** | ❌ chưa ở v1 — chốt 2026-09-11 | §3.3. Người dùng tự tải lại trang khi gặp. Điều kiện làm: triển khai đủ thường xuyên để việc này xảy ra với người dùng thật |
| Tầng cấu hình lúc chạy | ❌ chưa | Điều kiện: một trong hai tình huống ở §5.3 |
| Endpoint kiểm tra sức khoẻ tĩnh | ❌ chưa | Điều kiện: có hệ thống giám sát thật |
| Phục vụ dưới một đường dẫn con | ❌ chưa | Điều kiện: dự án cụ thể cần — phải khai tường minh **trước** khi build (§4.3) |
| Một bản build cho mỗi môi trường | ❌ loại, không hoãn `K48` | §5.1 — bản build cho môi trường A đem triển khai lên B là sai âm thầm |
| Bản đồ mã nguồn công khai ở môi trường thật | ❌ loại, không hoãn `K49` | §7 |

Một finding dạng *"FE thiếu X"* chỉ hợp lệ khi X mang trạng thái **✅ sẽ có** mà vắng mặt, hoặc khi điều kiện ở cột ghi chú của một dòng **❌ chưa** đã xảy ra. Dòng **❌ loại, không hoãn** chỉ đổi được bằng một ADR mới, không đổi được bằng một finding.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Cái gì được commit, cái gì là artifact | [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) |
| CSP và header bảo mật | [`14-security.md`](14-security.md) §4 |
| Cookie và CORS ở môi trường dev | [`07-auth-identity.md`](07-auth-identity.md) §8 |
| Cache tệp dịch | [`08-i18n.md`](08-i18n.md) §2 |
| Ngân sách bundle | [`13-performance.md`](13-performance.md) §4 |
| Cổng phải chạy trước khi build | [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
