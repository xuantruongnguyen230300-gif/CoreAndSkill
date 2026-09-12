---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 07. Quan sát hệ thống — log, metric, health check, trace

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Phía FE: [`../fe/10-observability.md`](../fe/10-observability.md).

---

## 1. Bài toán thật

Không phải "hệ thống có đang chạy không" — cái đó nhìn màn hình là biết. Bài toán thật là:

> **10h07 sáng, một người dùng báo "bấm lưu thì báo lỗi". Trong hàng chục nghìn dòng log của buổi sáng, tìm đúng chuỗi dòng của lần bấm đó, xuyên qua mọi tầng, trong vòng vài phút.**

Mọi thứ trong file này tồn tại để trả lời câu đó. Một cơ chế quan sát không rút ngắn được thời gian trả lời câu đó thì chỉ đang tốn đĩa.

---

## 2. Log có cấu trúc

### 2.1 Khác gì log chuỗi

| | Log chuỗi | Log có cấu trúc |
| --- | --- | --- |
| Dòng log | Một câu đã ghép sẵn | Một khuôn + các trường có tên |
| Tìm kiếm | Khớp văn bản, dễ trượt | Lọc theo trường |
| Đếm, gộp nhóm | Gần như không làm được | Làm được |

Điểm mấu chốt: **truyền tham số, đừng nội suy vào chuỗi trước khi log.**

```csharp
// ✅ đúng — UserId là một trường có tên, lọc được
logger.LogInformation("Người dùng {UserId} đổi mật khẩu thành công", userId);

// ❌ sai — đã thành một chuỗi, chỉ còn cách tìm theo văn bản
logger.LogInformation($"Người dùng {userId} đổi mật khẩu thành công");
```

Cách sai còn có một hệ quả kín: mọi dòng log trở thành một khuôn khác nhau, nên không nhóm được *"lỗi này xảy ra bao nhiêu lần"*.

### 2.2 Khuôn chung cho mọi dòng log

Mọi dòng nên mang được, không cần lặp lại ở từng lời gọi:

| Trường | Nguồn |
| --- | --- |
| Thời điểm, mức, tên nguồn ghi | Thư viện log tự thêm |
| **Mã lần gọi** | Middleware đặt vào phạm vi log, xem §3 |
| Định danh người dùng | Middleware, khi đã xác thực |
| Tên môi trường, tên phiên bản | Cấu hình lúc khởi động |
| Đường dẫn và động từ HTTP | Middleware |

Đặt những trường này ở **phạm vi** thay vì truyền tay ở mỗi lời gọi. Truyền tay thì sẽ có chỗ quên, và chỗ quên là chỗ điều tra bị đứt.

---

## 3. Mã lần gọi xuyên suốt

### 3.1 Vòng đời

```text
FE sinh (hoặc nhận từ hạ tầng) ──> gửi kèm header
                                       │
                        Middleware nhận / sinh nếu chưa có
                                       │
                        Đặt vào phạm vi log của cả request
                                       │
              ┌────────────────────────┼────────────────────────┐
              ▼                        ▼                        ▼
        mọi dòng log            bản ghi outbox            envelope trả về
                                                                │
                                                    FE hiển thị khi có lỗi
```

### 3.2 Bốn yêu cầu

1. **Sinh sớm nhất có thể** — middleware đầu chuỗi, trước cả xác thực. Lỗi khi xác thực cũng phải truy được.
2. **Nhận giá trị từ client nếu có, sinh mới nếu không.** FE gửi lên thì hai bên nối được với nhau.
3. **Trả về trong envelope lỗi.** Người dùng đọc được mã đó cho bộ phận hỗ trợ. Đây là thứ rút ngắn việc điều tra từ hàng chục phút xuống dưới một phút.
4. **Đi theo cả việc chạy nền.** Bản ghi outbox mang mã của request đã sinh ra nó — nếu không, mọi thứ chạy nền sẽ là một vùng tối không nối được với nguyên nhân.

**Bẫy:** chỉ ghi mã đó ở middleware rồi thôi. Nó phải nằm trong **phạm vi** để mọi dòng log bên trong request đều mang, kể cả dòng do thư viện bên thứ ba ghi.

---

## 4. Dùng đúng mức log

| Mức | Nghĩa | Ai đọc | Bật ở môi trường thật? |
| --- | --- | --- | --- |
| **Trace** | Chi tiết từng bước | Người đang gỡ lỗi | Không |
| **Debug** | Thông tin cho lập trình viên | Người đang gỡ lỗi | Không |
| **Information** | Sự kiện nghiệp vụ đáng ghi nhận | Người vận hành | Có |
| **Warning** | Bất thường **đã tự xử lý được** | Người vận hành | Có |
| **Error** | Một request thất bại, cần người xem | Người trực | Có |
| **Fatal** | Ứng dụng không tiếp tục được | Người trực, ngay | Có |

Hai lỗi dùng mức hay gặp nhất, và cả hai đều làm log mất giá trị:

| Lỗi | Hệ quả |
| --- | --- |
| **Log lỗi nghiệp vụ ở mức Error** | Nhập sai email là chuyện bình thường. Ghi nó ở Error khiến bảng theo dõi lỗi đầy nhiễu, và cảnh báo thật chìm nghỉm. Lỗi nghiệp vụ thuộc **Information**, hoặc không log gì |
| **Log lỗi hệ thống ở mức Warning** | Ngược lại: một ngoại lệ chưa xử lý bị ghi Warning sẽ không ai thấy |

**Phép thử:** dòng log này có cần **người** làm gì không? Có → Error. Không → thấp hơn.

---

## 5. Không được log cái gì

Đây là mục có hậu quả pháp lý, không chỉ kỹ thuật. Log thường được lưu lâu hơn dữ liệu, được sao chép sang hệ thống phân tích, và có nhiều người đọc được hơn DB.

| Tuyệt đối không log | Kể cả ở mức Debug |
| --- | --- |
| Mật khẩu, ở mọi dạng | ✅ kể cả |
| Token, khoá API, chuỗi kết nối, bí mật ký | ✅ |
| Giá trị cookie phiên | ✅ |
| Toàn bộ nội dung request của endpoint đăng nhập / đổi mật khẩu | ✅ |
| Số giấy tờ tuỳ thân, số thẻ, dữ liệu sức khoẻ | ✅ |
| Nội dung file người dùng tải lên | ✅ |

| Log có kiểm soát | Cách |
| --- | --- |
| Email, số điện thoại | Che một phần, hoặc chỉ ghi định danh người dùng và tra ngược khi cần |
| Nội dung request thông thường | Chỉ khi gỡ lỗi, có thời hạn, và đi qua bộ lọc trường nhạy cảm |
| Tên người dùng khi đăng nhập thất bại | Được — cần cho điều tra tấn công. Nhưng **không** kèm mật khẩu đã nhập |

### Ba đường rò hay gặp

| Đường rò | Vì sao xảy ra | Cách chặn |
| --- | --- | --- |
| **Log cả một đối tượng** | `logger.LogInformation("{@Request}", request)` ghi **mọi** thuộc tính — kể cả thuộc tính mật khẩu thêm vào ba tháng sau | Cấm log nguyên đối tượng đầu vào. Log các trường đã chọn |
| **Log chi tiết ngoại lệ chứa câu lệnh SQL kèm tham số** | Thư viện dữ liệu ghi câu lệnh khi bật mức chi tiết | Không bật ghi tham số câu lệnh ở môi trường thật |
| **Thông điệp lỗi của thư viện ngoài** | Một số thư viện đưa cả giá trị đầu vào vào thông điệp lỗi | Không chuyển thẳng thông điệp lỗi của thư viện ngoài vào log ở mức có lưu trữ |

Cách canh: một bộ lọc trường nhạy cảm ở tầng cấu hình log — che theo **danh sách tên trường** — cộng một test khẳng định bộ lọc đang hoạt động. Bộ lọc không có test kiểm chính nó thuộc đúng loại "cổng hỏng âm thầm" ở [`04-testing-strategy.md`](04-testing-strategy.md) §3.

---

## 6. Nên log cái gì

| Sự kiện | Mức | Vì sao |
| --- | --- | --- |
| Ứng dụng khởi động: phiên bản, môi trường, migration đã áp | Information | Dòng đầu tiên mọi cuộc điều tra cần |
| Đăng nhập thành công / thất bại | Information | Điều tra bảo mật |
| Khoá tài khoản | Warning | Có thể là tấn công |
| Thay đổi phân quyền | Information | Ai đổi, đổi gì |
| Bắt đầu / kết thúc một request | Information | Kèm thời gian chạy và mã trạng thái |
| Lỗi nghiệp vụ | Information | Kèm mã lỗi, để đếm được |
| Ngoại lệ chưa xử lý | Error | Kèm đầy đủ ngăn xếp |
| Sự kiện outbox thất bại quá N lần | Error | Cần người can thiệp |
| Job nền: bắt đầu, kết thúc, số bản ghi xử lý | Information | Không có thì job nền là vùng tối |

Điểm chung: log **sự kiện có ý nghĩa**, không log **dòng chảy code**. `"Đã vào hàm X"` không giúp gì mà làm loãng phần có ích.

---

### 6.1 Khi lưu lượng lớn — lấy mẫu

Log mọi request ở mức chi tiết sẽ tốn đĩa và làm chậm chính ứng dụng khi lưu lượng tăng. Chính sách nên có sẵn từ đầu, kể cả khi chưa cần bật:

| Loại | Lấy mẫu |
| --- | --- |
| Request thất bại, ngoại lệ | **Luôn giữ toàn bộ.** Đây là thứ đáng giá nhất, và cũng là thứ ít nhất về số lượng |
| Request thành công | Lấy mẫu theo tỉ lệ khi lưu lượng vượt ngưỡng |
| Request chậm bất thường | Luôn giữ |

Nguyên tắc: **lấy mẫu phần bình thường, giữ trọn phần bất thường.** Lấy mẫu đều tay trên mọi request là cách làm mất đúng những dòng cần tìm.

---

## 7. Metric nên có

Log trả lời *"chuyện gì đã xảy ra với request này"*. Metric trả lời *"hệ thống đang thế nào"*. Hai câu hỏi khác nhau, và log không thay được metric ở quy mô nào.

| Nhóm | Chỉ số | Vì sao |
| --- | --- | --- |
| **Lưu lượng** | Số request mỗi giây, chia theo endpoint | Nền để đọc mọi chỉ số khác |
| **Lỗi** | Tỉ lệ phản hồi 5xx; tỉ lệ 4xx tách riêng | 4xx tăng thường là lỗi client hoặc tấn công, không phải lỗi server |
| **Độ trễ** | Phân vị 50 / 95 / 99 | **Không dùng trung bình** — trung bình giấu đúng nhóm người dùng đang khổ |
| **Tài nguyên** | CPU, bộ nhớ, số kết nối DB đang dùng trên tổng | Cạn kết nối DB là nguyên nhân sự cố rất hay gặp và rất khó đoán từ log |
| **Hàng đợi nội bộ** | Số bản ghi outbox chưa phát, tuổi bản ghi cũ nhất | **Chỉ số cảnh báo sớm tốt nhất** cho toàn bộ mảng sự kiện |
| **Nghiệp vụ** | Số đăng nhập thất bại, số job chạy lỗi | Vài chỉ số, chọn lọc |

**Tuổi bản ghi outbox cũ nhất** đáng được nhắc riêng: nó tăng đều là dấu hiệu tiến trình phát đã chết hoặc đang kẹt, và nó phát hiện sự cố đó **trước khi** người dùng nhận ra thông báo không tới.

---

## 8. Health check — sống và sẵn sàng là hai câu hỏi khác nhau

| | Liveness (còn sống) | Readiness (sẵn sàng) |
| --- | --- | --- |
| Trả lời | Tiến trình có cần khởi động lại không | Có nên gửi request tới đây không |
| Kiểm gì | **Chỉ bản thân tiến trình.** Không gọi DB, không gọi hệ ngoài | DB kết nối được, migration đã áp, cấu hình hợp lệ |
| Sai thì sao | Khởi động lại vô ích, hoặc lặp vô hạn | Nhận request khi chưa sẵn sàng, hoặc bị loại khỏi tải cân bằng oan |

### Bẫy kinh điển

**Cho liveness kiểm DB.** DB chậm 30 giây → liveness thất bại → bộ điều phối khởi động lại app → app khởi động lại càng làm DB tải nặng → liveness lại thất bại. Một sự cố nhỏ ở DB biến thành vòng lặp khởi động lại toàn hệ.

> **Liveness chỉ được trả lời câu hỏi: tiến trình này có bị treo tới mức chỉ còn cách giết đi không?**

Hai nguyên tắc kèm theo:

- Endpoint health check **không đòi xác thực**, nhưng **không được tiết lộ chi tiết** (chuỗi kết nối, phiên bản thư viện, thông điệp lỗi nội bộ) — nó là endpoint công khai.
- Readiness phải trả về **thất bại khi còn migration chưa áp**. Đây là mặt còn lại của luật E8 ở [`../../RULES.md`](../../RULES.md).

---

## 9. OpenTelemetry — khi nào cần

OpenTelemetry là chuẩn chung để mô tả log, metric và trace, cộng với bộ thư viện thu thập.

| Ở quy mô một tiến trình | Ở nhiều tiến trình |
| --- | --- |
| Giá trị chính là **không bị khoá vào một nhà cung cấp**: đổi hệ thống thu thập không phải sửa code | Giá trị chính là **trace phân tán**: nhìn được một request đi qua mấy dịch vụ, mỗi chặng tốn bao lâu |

Khuyến nghị cho Core này:

- **Dùng chuẩn ngay từ đầu** cho phần đặt tên và ngữ nghĩa thuộc tính — chi phí gần bằng không, và nó tránh phải đổi tên hàng loạt sau này.
- **Chưa dựng hạ tầng thu thập** khi vẫn còn một tiến trình. Trace phân tán trong một tiến trình không cho thêm thông tin nào mà log có phạm vi chưa cho.
- **Ngưỡng dựng:** có từ hai tiến trình trở lên gọi nhau, hoặc bắt đầu gọi hệ thống bên ngoài mà độ trễ của chúng ảnh hưởng người dùng.

---

## 10. Lưu log ở đâu, giữ bao lâu

| Câu hỏi | Khuyến nghị cho hệ tầm trung |
| --- | --- |
| Ghi ra đâu | Ghi ra đầu ra chuẩn của tiến trình; hạ tầng thu gom. Ghi thẳng ra file trên máy chủ khiến log biến mất cùng máy chủ |
| Giữ bao lâu | Log vận hành: vài tuần. Nhật ký kiểm toán: theo yêu cầu tuân thủ, xem [`10-data-retention.md`](10-data-retention.md) |
| Ai đọc được | Log có thể chứa dữ liệu cá nhân dù đã lọc — quyền đọc log là quyền cần kiểm soát |

**Nhật ký kiểm toán không phải log.** Log để chẩn đoán, có thể mất, xoay vòng theo thời gian. Nhật ký kiểm toán là dữ liệu nghiệp vụ, nằm trong DB, không được mất. Trộn hai thứ là cách mất bằng chứng khi cần tới. Xem [`10-data-retention.md`](10-data-retention.md).

---

## 11. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Log có cấu trúc (Serilog) | ✅ sẽ có | Ghi ra đầu ra chuẩn |
| Mã lần gọi trong phạm vi log | ✅ sẽ có | Sinh ở middleware đầu chuỗi |
| Mã lần gọi trong envelope lỗi | ✅ sẽ có | Hình dạng envelope ở [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) |
| Bộ lọc trường nhạy cảm + test kiểm bộ lọc | ✅ sẽ có | Bộ lọc không có test là bộ lọc không tin được |
| Liveness và readiness tách riêng | ✅ sẽ có | Liveness **không** chạm DB |
| Readiness thất bại khi còn migration chưa áp | ✅ sẽ có | Mặt còn lại của luật E8 |
| Đặt tên thuộc tính theo chuẩn OpenTelemetry | ✅ sẽ có | Chi phí gần bằng không |
| **Hạ tầng thu thập trace** | ❌ chưa | Một tiến trình thì trace phân tán không thêm thông tin. Ngưỡng ở §9 |
| **Metric xuất ra hệ ngoài** | ❌ chưa ở v1 | Khai chỉ số trong code trước; đấu nối khi có nơi nhận |
| **Cảnh báo tự động** | ❌ chưa | Cần có nơi nhận metric trước. Khi làm, ưu tiên hai chỉ số: tỉ lệ 5xx và tuổi bản ghi outbox cũ nhất |
| **Ghi log ra file trên máy chủ** | ❌ không làm | Log biến mất cùng máy chủ, và không gộp được khi chạy nhiều instance |
