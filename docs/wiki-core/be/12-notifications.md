---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 12. Sự kiện và thông báo

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Phần **nhất quán dữ liệu** giữa các module ở [`05-cross-module-consistency.md`](05-cross-module-consistency.md). File này lo phần **báo cho người dùng** và chi tiết vận hành Outbox.

---

## 1. Ba khái niệm hay bị lẫn — tách trước khi làm gì khác

Đây là mục quan trọng nhất của file. Ba thứ dưới đây thường bị gọi chung là "event" hoặc "notification", và việc gộp chúng lại là nguồn gốc của phần lớn thiết kế sai trong mảng này.

| | Domain event | Integration event | Notification |
| --- | --- | --- | --- |
| **Là gì** | Một chuyện vừa xảy ra **trong** một biên nhất quán | Một chuyện đã xảy ra, công bố **ra ngoài** module | Một **thông điệp gửi tới con người** |
| **Phạm vi** | Trong module, trong process | Giữa các module | Ra khỏi hệ thống, tới người dùng |
| **Transaction** | **Cùng** transaction với thay đổi | **Khác** transaction — ghi vào Outbox, phát sau | Không liên quan transaction |
| **Thất bại thì sao** | Toàn bộ quay lại | Thử lại; dữ liệu đã ghi vẫn giữ nguyên | Thử lại; không ảnh hưởng nghiệp vụ |
| **Ai nhận** | Handler trong cùng module | Module khác | Người dùng |
| **Bản chất** | Cơ chế | Cơ chế | **Người tiêu thụ** của hai cơ chế trên |

### 1.1 Điểm cốt lõi: notification KHÔNG phải một cơ chế

Notification không đứng cùng hàng với hai khái niệm kia. Nó là **một trong những thứ đăng ký nghe** integration event — cùng hàng với "cập nhật một bảng tổng hợp" hay "gọi một hệ thống ngoài".

Hệ quả thiết kế, và đây là chỗ hay làm sai:

| Làm đúng | Làm sai |
| --- | --- |
| Nghiệp vụ phát ra một **sự kiện**: *"tài khoản vừa bị khoá"* | Nghiệp vụ gọi thẳng *"gửi email khoá tài khoản"* |
| Một bên nghe sự kiện đó quyết định có thông báo không, cho ai, qua kênh nào | Handler nghiệp vụ biết địa chỉ email, biết mẫu thư, biết cả cách gửi |

Cách sai trói nghiệp vụ vào kênh gửi. Thêm một kênh là sửa handler nghiệp vụ; tắt thông báo cho một nhóm người dùng cũng là sửa handler nghiệp vụ. Và test cho luật nghiệp vụ phải dựng giả một dịch vụ gửi mail.

### 1.2 Luồng đầy đủ

```text
Handler nghiệp vụ
   │ (1) đổi trạng thái, entity ghi nhận domain event
   ├── domain event ──> handler trong cùng module ── cùng transaction
   │
   └── (2) ghi bản ghi Outbox ──── CÙNG transaction với dữ liệu
                │
        [transaction commit]
                │
   (3) tiến trình phát nền đọc Outbox
                │
        ┌───────┴────────┬──────────────────┐
        ▼                ▼                  ▼
  module khác     bên tạo thông báo    hệ thống ngoài
                        │
                 (4) quyết định: ai nhận, kênh nào, mẫu nào
                        │
                 ┌──────┴──────┐
                 ▼             ▼
              in-app         email
```

Bước (4) là nơi **toàn bộ** tri thức về thông báo tập trung. Không có tri thức nào về thông báo nằm ở bước (1).

---

## 2. Outbox — chi tiết vận hành

Lý do Outbox tồn tại đã nói ở [`05-cross-module-consistency.md`](05-cross-module-consistency.md) §4. Mục này lo phần chạy thật.

### 2.1 Ghi vào Outbox

Bản ghi Outbox được ghi **trong cùng transaction** với thay đổi dữ liệu. Cách tự nhiên nhất: một bộ chặn ở tầng dữ liệu thu các sự kiện mà entity đã ghi nhận, chuyển thành bản ghi Outbox, ngay trước khi lưu.

Làm ở tầng đó có hai cái lợi: handler không phải nhớ ghi Outbox, và **không thể quên**.

### 2.2 Nội dung một bản ghi

| Yêu cầu | Vì sao |
| --- | --- |
| **Tự chứa** | Bên xử lý không được phải đọc lại DB để hiểu sự kiện. Dữ liệu có thể đã đổi từ lúc phát |
| **Không chứa dữ liệu nhạy cảm** | Bảng Outbox sống lâu, được đọc bởi nhiều thứ. Chứa định danh, đừng chứa mật khẩu hay số giấy tờ |
| **Có phiên bản hợp đồng** | Hình dạng sự kiện sẽ đổi. Không có số phiên bản thì lúc đổi phải dừng hệ thống để dọn hàng chờ |
| **Mang mã lần gọi** | Nối việc chạy nền với request đã sinh ra nó — xem [`07-observability.md`](07-observability.md) |

### 2.3 Tiến trình phát

| Chi tiết | Cách làm |
| --- | --- |
| Nhịp quét | Vài giây một lần là đủ ở quy mô này |
| Lấy bản ghi | Khoá dòng đang xử lý để hai instance không cùng phát một sự kiện |
| Kích thước lô | Vừa phải; lô lớn giữ transaction lâu |
| Thứ tự | **Không hứa hẹn.** Bên nhận cần thứ tự thì tự xử lý bằng số phiên bản trong nội dung |
| Đánh dấu đã phát | Sau khi bên nhận xử lý xong |
| Dọn dẹp | Xoá bản ghi đã phát sau một khoảng đã khai; không dọn thì bảng phình và tiến trình chậm dần |

### 2.4 Thử lại và lùi dần

| Loại lỗi | Thử lại? |
| --- | --- |
| Mạng chập chờn, hệ ngoài tạm thời không phản hồi | ✅ Nên |
| Dịch vụ nhận đang quá tải | ✅ Nên, và phải **lùi dần** |
| Dữ liệu sai hình dạng, hợp đồng không khớp | ❌ Thử lại bao nhiêu lần cũng vậy — chuyển thẳng sang trạng thái cần can thiệp |
| Địa chỉ email không tồn tại | ❌ Không thử lại |

Khoảng lùi tăng dần (vài giây → vài chục giây → vài phút) và có trần. Không lùi dần thì một dịch vụ đang quá tải sẽ bị chính cơ chế thử lại đánh sập.

**Kèm một chút ngẫu nhiên vào khoảng lùi.** Không có nó, nhiều bản ghi cùng thất bại một lúc sẽ cùng thử lại một lúc, tạo ra từng đợt sóng đều đặn.

### 2.5 Thư chết

Sau N lần thử, bản ghi chuyển sang trạng thái cần can thiệp và **phát cảnh báo**. Ba yêu cầu:

1. **Không im lặng.** Một bản ghi kẹt mà không ai biết là một thông báo không bao giờ tới, và người dùng chỉ phát hiện khi hậu quả đã xảy ra.
2. **Xem lại được.** Cần một cách liệt kê bản ghi kẹt kèm lỗi cuối, để chẩn đoán.
3. **Phát lại được.** Sau khi sửa nguyên nhân, phải phát lại được — có kiểm soát, không phát lại hàng loạt một cách mù quáng.

**Chỉ số cảnh báo tốt nhất cho cả mảng này:** tuổi của bản ghi Outbox chưa phát cũ nhất. Nó tăng đều nghĩa là tiến trình phát đã chết hoặc đang kẹt, và nó báo trước khi người dùng nhận ra.

---

### 2.6 Hợp đồng sự kiện sẽ đổi — chuẩn bị trước

Một sự kiện là một **hợp đồng** giữa bên phát và bên nhận. Nó sẽ đổi, và lúc đổi thì trong hàng chờ có thể còn bản ghi theo hình dạng cũ.

| Loại thay đổi | An toàn? | Cách làm |
| --- | --- | --- |
| Thêm trường tuỳ chọn | ✅ | Bên nhận cũ bỏ qua trường lạ |
| Thêm trường bắt buộc | ❌ | Thêm dạng tuỳ chọn trước, chuyển bên nhận, rồi mới siết |
| Xoá trường | ❌ | Chỉ sau khi chắc chắn không bên nhận nào đọc |
| Đổi ý nghĩa của một trường | ❌ **Nguy hiểm nhất** | Dùng **trường mới**, đừng đổi nghĩa trường cũ |

Dòng cuối đáng nhấn: đổi ý nghĩa mà giữ nguyên tên là loại thay đổi không công cụ nào phát hiện được — mọi thứ vẫn phân tích được, chỉ là hiểu sai.

**Cách phòng:** mỗi loại sự kiện mang số phiên bản, và bên nhận từ chối rõ ràng phiên bản nó không hiểu, thay vì cố xử lý.

---

## 3. Kênh thông báo

### 3.1 Hai kênh ở v1

| Kênh | Hợp với | Đặc điểm |
| --- | --- | --- |
| **In-app** | Việc liên quan tới thao tác trong hệ thống | Rẻ nhất, không phụ thuộc hệ ngoài, xem được lịch sử. Chỉ tới khi người dùng mở ứng dụng |
| **Email** | Việc cần biết ngay cả khi không mở ứng dụng | Phụ thuộc một dịch vụ ngoài; có thể vào thư rác; không đảm bảo tới |

### 3.2 Trừu tượng hoá đúng mức

Core khai một interface gửi thông báo, có một cài đặt cho mỗi kênh. Điều cần cưỡng lại: xây một cơ chế định tuyến đa kênh với ưu tiên, dự phòng và quy tắc — trước khi có kênh thứ ba.

**Ngưỡng:** hai kênh thì một interface và một bộ chọn kênh đơn giản là đủ. Từ kênh thứ ba, hoặc khi có quy tắc dạng *"thử kênh A, thất bại thì chuyển kênh B"*, mới đáng dựng cơ chế định tuyến.

### 3.3 Sở thích người nhận

Ngay cả ở v1, cần một chỗ trả lời câu *"người này có muốn nhận thông báo loại đó qua kênh đó không"*. Không có nó thì cách duy nhất để tắt bớt thông báo là sửa code.

Mức tối thiểu: một bảng ánh xạ người dùng × loại thông báo × kênh, mặc định là bật. Rẻ, và nó tránh được việc phải sửa hàng loạt handler về sau.

### 3.4 Gộp thông báo

Một thao tác sinh ra hàng trăm sự kiện thì người dùng nhận hàng trăm email. Đây là cách nhanh nhất để mọi người tắt thông báo vĩnh viễn.

Chưa cần làm ở v1, nhưng cần **thiết kế sẵn chỗ**: thông báo mang một khoá nhóm, để sau này gộp được mà không phải sửa lược đồ dữ liệu.

---

## 4. Nội dung thông báo — mẫu và đa ngôn ngữ

### 4.1 Luật: BE không ghép câu hiển thị

Cùng luật với mọi thông điệp khác trong hệ (luật R8 ở [`../../RULES.md`](../../RULES.md)): backend giữ **mã và tham số**, không giữ câu chữ.

Với thông báo, luật này có một khác biệt phải xử lý: **email được gửi đi từ server**, không phải do trình duyệt hiển thị — nên phải có ai đó ghép câu ở phía server.

Cách giải: tách **cơ chế** khỏi **nội dung**.

| Ai | Sở hữu gì |
| --- | --- |
| **Core** | Cơ chế: interface kết xuất mẫu, cơ chế chọn ngôn ngữ, cơ chế gửi |
| **Dự án** | Nội dung: các mẫu thư, các bản dịch |

Core **không** đi kèm mẫu thư mặc định bằng tiếng Việt — làm vậy là đưa câu chữ vào Core, và Core sẽ không mang đi được sang dự án có giọng văn khác hoặc ngôn ngữ khác.

### 4.2 Lấy ngôn ngữ ở đâu

Đây là một câu hỏi thật, và câu trả lời khác với ngôn ngữ của request:

| Ca | Lấy ngôn ngữ từ |
| --- | --- |
| Thông báo do chính người nhận gây ra, gửi ngay | Có thể lấy từ request |
| **Thông báo gửi cho người khác** | **Sở thích của người nhận** — không phải người gây ra |
| Thông báo phát sinh từ job nền | Sở thích của người nhận; không có request nào để lấy |

Vì hai dòng cuối chiếm phần lớn, quy tắc chung là: **ngôn ngữ lấy từ hồ sơ người nhận**, có giá trị mặc định của hệ thống khi thiếu.

Hệ quả: mỗi người dùng cần một thuộc tính ngôn ngữ ưa dùng, và bản ghi Outbox phải mang đủ thông tin để bên xử lý tra ra người nhận.

### 4.3 Mẫu

| Yêu cầu | Vì sao |
| --- | --- |
| Mẫu tách khỏi code | Sửa câu chữ không cần triển khai lại |
| Một mẫu cho mỗi loại × ngôn ngữ | |
| Tham số **theo tên**, không theo thứ tự | Đổi ngôn ngữ thường đổi trật tự từ. Tham số theo thứ tự sẽ ghép sai ở ngôn ngữ khác. Luật R7 ở [`../../RULES.md`](../../RULES.md) |
| Thoát ký tự khi kết xuất HTML | Tên người dùng chứa ký tự đặc biệt không được phá vỡ email, và không được trở thành đường chèn script |
| Có bản văn bản thuần kèm bản HTML | Một số trình đọc thư chỉ hiển thị văn bản thuần |
| Thiếu mẫu thì thất bại **rõ ràng** | Không được gửi đi một email có chỗ trống chưa thay |

Chi tiết về mã, tham số đặt tên và ranh giới sở hữu câu chữ: [`16-i18n-va-ma-loi.md`](16-i18n-va-ma-loi.md).

---

## 5. Thông báo trong ứng dụng

| Khía cạnh | Cách làm |
| --- | --- |
| Lưu trữ | Bảng trong schema `core`: người nhận, loại, tham số, đã đọc chưa, thời điểm |
| **Lưu tham số, không lưu câu đã ghép** | Câu chữ ghép lúc hiển thị. Lưu câu đã ghép thì đổi ngôn ngữ không đổi được thông báo cũ, và sửa lỗi chính tả không sửa được cái đã gửi |
| Đánh dấu đã đọc | Từng cái và tất cả |
| Cập nhật giao diện | Hỏi định kỳ ở v1 |
| Dọn dẹp | Xoá thông báo đã đọc quá hạn — xem [`10-data-retention.md`](10-data-retention.md) |

Dòng thứ hai là quyết định quan trọng nhất của mục này, và nó rẻ khi làm từ đầu, đắt khi sửa sau.

**Cập nhật thời gian thực** (kết nối đẩy từ server) là Nhóm B: nó thêm một hạ tầng kết nối lâu dài, và ở ứng dụng quản trị, độ trễ vài chục giây thường không ai để ý. Ngưỡng: khi có nghiệp vụ mà độ trễ đó gây hậu quả thật.

---

## 6. §Áp dụng

> **Thuộc phạm vi v1** (chốt 2026-09-10) — [`01-core-components.md`](01-core-components.md) §5.1, mục A18.
> Thi công ở pha **B4** ([`trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)); hợp đồng endpoint ở [`../../contracts/notifications.md`](../../contracts/notifications.md).

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Tách ba khái niệm ở §1 | 📐 luật thiết kế | Handler nghiệp vụ **không** biết kênh gửi |
| Outbox trong schema `core` | ✅ sẽ có | Ghi cùng transaction, qua bộ chặn ở tầng dữ liệu |
| Tiến trình phát nền | ✅ sẽ có | Hosted service |
| Thử lại có lùi dần, có ngẫu nhiên, có trần | ✅ sẽ có | |
| Trạng thái thư chết + cảnh báo | ✅ sẽ có | Im lặng là cách hỏng tệ nhất ở đây |
| Chỉ số tuổi bản ghi chưa phát cũ nhất | ✅ sẽ có | Cảnh báo sớm tốt nhất cho cả mảng |
| Kênh in-app | ✅ sẽ có | Lưu tham số, không lưu câu đã ghép |
| Kênh email | ✅ sẽ có | Interface ở Core, cấu hình dịch vụ gửi ở dự án |
| Interface kết xuất mẫu, **không kèm mẫu mặc định** | ✅ sẽ có | Nội dung thuộc dự án |
| Ngôn ngữ lấy từ hồ sơ người nhận | ✅ sẽ có | Không lấy từ request |
| Sở thích nhận thông báo | 📐 mức tối thiểu | Bảng ánh xạ, mặc định bật |
| **Kênh thứ ba (tin nhắn, ứng dụng nhắn tin)** | ❌ chưa | Thêm khi có yêu cầu; lúc đó mới dựng cơ chế định tuyến |
| **Gộp thông báo** | ❌ chưa | Thiết kế sẵn khoá nhóm để sau này làm được |
| **Đẩy thời gian thực** | ❌ chưa | Hỏi định kỳ ở v1 |
| **Message broker** | ❌ chưa | Điều kiện ở [`05-cross-module-consistency.md`](05-cross-module-consistency.md) §7.3 |
