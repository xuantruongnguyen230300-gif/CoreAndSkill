---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 10. Quan sát được — lỗi runtime và số đo phía FE

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Phía BE: [`../be/07-observability.md`](../be/07-observability.md). Hai phía nối với nhau bằng `traceId` — §5 là mục quan trọng nhất của file này.

---

## 1. Vấn đề: lỗi FE là loại lỗi không ai nhìn thấy

Lỗi BE để lại dấu vết: log, mã trạng thái, số đo. Lỗi FE thì xảy ra **trên máy người khác**, trong một trình duyệt mình không có, ở một trạng thái mình không dựng lại được.

Ba câu hỏi thường không trả lời được nếu không chuẩn bị trước:

| Câu hỏi | Không có gì thì | Có gì thì |
| --- | --- | --- |
| "Hôm qua tôi bấm Lưu mà không được" | Không tái hiện được, đóng lại là "không rõ" | Tìm theo `traceId` hoặc theo thời điểm, thấy lỗi thật |
| Một màn hỏng với 5% người dùng | Không ai báo, hoặc báo sau vài tuần | Thấy trong đợt lỗi đầu tiên |
| App chậm với ai, ở đâu | Chỉ có cảm nhận | Có số đo |

---

## 2. Ba loại tín hiệu, ba cách xử lý

| Loại | Ví dụ | Ai xử lý |
| --- | --- | --- |
| **Lỗi HTTP** — đã có envelope, đã có `traceId` | 409 khi lưu trùng | Interceptor lỗi — [`02-http-envelope.md`](02-http-envelope.md) §4 |
| **Lỗi runtime JavaScript** — ngoài dự kiến | Đọc thuộc tính của `undefined`, lỗi khi vẽ lại | `ErrorHandler` tuỳ biến — §3 |
| **Số đo trải nghiệm** | Thời gian tới khung hình đầu, độ trễ tương tác | §6 |

Ba loại này khác nhau ở chỗ **lỗi HTTP đã được xử lý ở nơi khác**. Đưa cả chúng vào bộ thu lỗi runtime nghĩa là mỗi lần server trả 400 hợp lệ lại sinh một "lỗi FE" — nhiễu tới mức không ai đọc nữa.

---

## 3. Bắt lỗi runtime

```typescript
// core/observability/global-error-handler.ts
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly reporter = inject(ClientErrorReporter);

  handleError(error: unknown): void {
    // Lỗi HTTP đã đi qua interceptor rồi — không đếm hai lần.
    if (error instanceof HttpErrorResponse) return;

    this.reporter.capture(error);
    console.error(error);   // vẫn giữ, để môi trường phát triển không mất thông tin
  }
}
```

**Ba nguồn lỗi mà `ErrorHandler` của Angular KHÔNG bắt**, phải nối riêng nếu muốn phủ hết:

| Nguồn | Nối bằng |
| --- | --- |
| Promise bị từ chối mà không ai bắt | Sự kiện `unhandledrejection` của cửa sổ |
| Lỗi ngoài vùng Angular (thư viện bên thứ ba, callback thuần) | Sự kiện `error` của cửa sổ |
| Tài nguyên tải hỏng (ảnh, script) | Sự kiện `error` ở giai đoạn capture |

Nguồn thứ nhất là nguồn hay bị bỏ sót nhất, và nó chính là nơi mọi lỗi `async/await` không bắt rơi vào.

---

## 4. Gửi đi đâu

| Phương án | Được | Mất |
| --- | --- | --- |
| **Chỉ `console`** | Không tốn gì | Không thu được gì từ người dùng thật. Chỉ đúng cho môi trường phát triển |
| **Gửi về BE của chính mình** | Không thêm nhà cung cấp, dữ liệu không rời hệ thống, dễ nối `traceId` | Phải viết endpoint, phải chống lụt, phải quyết định lưu bao lâu |
| **Dịch vụ thu lỗi bên ngoài** | Gộp nhóm lỗi, cảnh báo, xem lại phiên — tất cả có sẵn | Dữ liệu ra ngoài; thêm một phụ thuộc trong bundle; chi phí |

**Quyết định cho v1 (chốt 2026-09-10): gửi về BE của chính mình.** `ClientErrorReporter` giữ đầu ra cắm được — môi trường phát triển ra `console`, môi trường thật ra endpoint khai ở [`../../contracts/client-errors.md`](../../contracts/client-errors.md).

Không dùng dịch vụ thu lỗi bên ngoài: dữ liệu của cơ quan không rời hệ thống, và lỗi FE nối được với log BE qua `traceId` (§5) chỉ khi hai bên ở cùng một chỗ.

Vì sao vẫn giữ lớp trừu tượng thay vì gọi thẳng endpoint: chi phí của nó gần bằng 0 (một interface và một hiện thực tầm thường), còn gọi thẳng thì môi trường phát triển cũng bắn báo cáo về server, và đổi đích sau này thành một đợt sửa rải khắp nơi.

Hợp đồng của endpoint: [`../../contracts/client-errors.md`](../../contracts/client-errors.md). Ba ràng buộc ở §4.1 áp cho **mọi** đầu ra, kể cả `console`.

### 4.1 Ba ràng buộc bắt buộc, dù chọn đích nào

| Ràng buộc | Vì sao |
| --- | --- |
| **Không gửi dữ liệu cá nhân hay nội dung form** | Một báo cáo lỗi kèm nội dung ô nhập có thể chứa mật khẩu người dùng gõ nhầm ô. Chỉ gửi loại lỗi, vị trí, `traceId` |
| **Có giới hạn tần suất** | Một lỗi trong vòng lặp vẽ lại có thể sinh hàng nghìn báo cáo mỗi phút và tự làm sập chính hệ thống thu |
| **Thất bại của việc gửi không được làm hỏng app** | Bọc kín, không để một lỗi trong bộ thu lỗi lại sinh thêm lỗi — đây là vòng lặp cổ điển |

---

## 5. `traceId` — nối hai phía

Đây là lý do envelope mang `traceId` **kể cả khi thành công** ([`02-http-envelope.md`](02-http-envelope.md) §2.1).

### 5.1 Ba nơi `traceId` phải xuất hiện

| Nơi | Để làm gì |
| --- | --- |
| Trong báo cáo lỗi runtime | Nối một lỗi FE với chuỗi request đã dẫn tới nó |
| **Trên màn hình lỗi mà người dùng nhìn thấy** | Người dùng đọc mã đó cho người hỗ trợ; đó là cách rẻ nhất để nối một lời phàn nàn với một dòng log |
| Trong log của trình duyệt lúc phát triển | Truy nhanh khi đang làm |

Cách hiển thị: một dòng nhỏ, chữ nhạt, ở cuối thông báo lỗi — đủ để đọc cho người hỗ trợ, không tranh chỗ với câu giải thích. **Đừng ẩn hoàn toàn**: một `traceId` chỉ nằm trong log của trình duyệt là một `traceId` không ai lấy ra được từ người dùng thật.

### 5.2 Giữ `traceId` gần nhất

`ClientErrorReporter` giữ `traceId` của **request cuối cùng** (và một số ít request trước đó). Khi một lỗi runtime xảy ra, gần như luôn có một request vừa hoàn tất liên quan tới nó.

Đây là suy luận xác suất, không phải quan hệ nhân quả chắc chắn — và phải ghi rõ như vậy trong báo cáo, để người đọc log không kết luận sai. Nhưng nó rẻ và trong thực tế đúng phần lớn thời gian.

### 5.3 Điều kiện phía BE

`traceId` chỉ có giá trị nếu BE **thật sự ghi nó vào log** cho mọi request, kể cả request thành công. Nếu BE chỉ ghi `traceId` ở nhánh lỗi thì một nửa công dụng biến mất. Xem [`../be/07-observability.md`](../be/07-observability.md).

---

## 6. Core Web Vitals

### 6.1 Ba chỉ số và ý nghĩa thực dụng

| Chỉ số | Đo cái gì | Với hệ quản trị nội bộ |
| --- | --- | --- |
| **LCP** — khối nội dung lớn nhất hiện ra | Cảm giác "trang đã tải xong" | Quan trọng: đây là ấn tượng đầu mỗi lần mở app |
| **INP** — độ trễ phản hồi tương tác | Bấm xong bao lâu thì thấy gì đó xảy ra | **Quan trọng nhất**: hệ quản trị là chuỗi thao tác liên tục, độ trễ tích lại thành mệt mỏi |
| **CLS** — dịch chuyển bố cục | Nội dung nhảy khi đang đọc | Quan trọng vừa; hay xảy ra ở màn có bảng tải chậm |

**INP là chỉ số đáng nhìn nhất ở loại ứng dụng này**, ngược với trang công khai nơi LCP được chú ý nhất. Lý do: người dùng nội bộ mở app một lần rồi làm việc hàng giờ trong đó — họ trả giá LCP một lần và trả giá INP hàng nghìn lần.

### 6.2 Đo bằng gì — và vì sao chưa cài thư viện

Hai cách:

| Cách | Được | Mất |
| --- | --- | --- |
| Công cụ kiểm tra trong trình duyệt, chạy tay | Không thêm phụ thuộc, dùng được ngay | Đo trên máy dev, mạng dev — không phải điều kiện thật |
| Thư viện đo trên máy người dùng thật | Số đo thật | Thêm phụ thuộc, và **phải có nơi nhận số đo** |

**Quyết định v1: đo tay bằng công cụ trình duyệt, không cài thư viện** ([`01-core-components.md`](01-core-components.md) §4.2, B2).

Lý do là điều kiện, không phải sự lười: một thư viện đo mà không có nơi nhận số và không có ai đọc số thì chỉ thêm byte vào bundle. Mở lại khi có nơi nhận (§4) — cùng một hạ tầng phục vụ cả lỗi lẫn số đo.

### 6.3 Số đo chỉ có nghĩa khi có ngưỡng

Một con số không có ngưỡng thì không dẫn tới hành động nào. Trước khi đo tự động, phải có: giá trị hiện tại (đo được), ngưỡng chấp nhận, và **hành động khi vượt ngưỡng**. Thiếu vế thứ ba thì việc đo chỉ tạo ra một biểu đồ không ai nhìn.

Ngân sách bundle ở [`13-performance.md`](13-performance.md) §4 là ví dụ của một số đo **có** đủ ba vế — và đó là lý do nó được chọn làm cổng trước.

---

## 7. Phân biệt lỗi của app với lỗi của người dùng

Một bộ thu lỗi trộn hai loại này sẽ nhanh chóng trở thành một danh sách không ai đọc.

| Loại | Ví dụ | Có phải lỗi FE không |
| --- | --- | --- |
| Người dùng nhập sai | 400 với `fieldErrors` | **Không** — đây là hệ thống hoạt động đúng |
| Người dùng thiếu quyền | 403 | **Không**, trừ khi giao diện đã hiện nút mà lẽ ra phải ẩn |
| Phiên hết hạn | 401 | **Không** |
| Server lỗi | 500 | Không phải lỗi FE, nhưng **đáng đếm** ở FE để biết người dùng gặp bao nhiêu lần |
| Mất mạng / hết thời gian chờ | Không có phản hồi | Đáng đếm, không đáng báo động từng lần |
| Ngoại lệ JavaScript | Đọc thuộc tính của `undefined` | **Có** — luôn là lỗi cần sửa |

Dòng thứ hai đáng dừng lại: **403 lặp lại nhiều lần ở cùng một chỗ là dấu hiệu giao diện đang hiện thứ không nên hiện.** Nó không phải lỗi bảo mật (BE đã chặn đúng), nhưng nó là một lỗi giao diện thật và chỉ phát hiện được nếu có ai đó nhìn số liệu. Đây là ví dụ điển hình cho việc quan sát không chỉ để tìm lỗi kỹ thuật.

---

## 8. Ba việc còn nợ, ghi ra để nhìn thấy được

> Theo [`../../RULES.md`](../../RULES.md) §10, nợ phải nhìn thấy được chứ không được hợp thức hoá bằng im lặng.

| Nợ | Vì sao chưa làm | Điều kiện làm |
| --- | --- | --- |
| Đích nhận báo cáo lỗi | Chưa có môi trường thật, chưa có hợp đồng endpoint | Có môi trường staging dùng thật |
| Đo Web Vitals trên máy người dùng | Chưa có nơi nhận số và chưa có ngưỡng | Cùng lúc với dòng trên — chung hạ tầng |
| Nối `traceId` FE ↔ log BE thành một truy vấn | Cần cả hai phía có `src/` và một nơi tập trung log | Khi BE có đường log tập trung |

Cả ba đều **không** phải điều kiện để đóng pha F0–F3. Chúng là việc của giai đoạn vận hành, và chúng nằm ở đây để không ai tưởng chúng đã có.

---

## 9. Log ở FE — nói ngắn về thứ dễ lạm dụng

| Quy tắc | Vì sao |
| --- | --- |
| Không để lại `console.log` trong code đã hoàn thành | Nó là nhiễu, và đôi khi là rò rỉ dữ liệu — log một object người dùng là log cả những trường không nên hiện |
| `console.error` giữ lại ở đường xử lý lỗi chung | Đây là chỗ nó có ích thật |
| Không tự dựng một tầng logger có mức độ ở FE | Ở BE thì đáng; ở FE thì gần như luôn thành một lớp bọc quanh `console` mà không ai đọc đầu ra |

---

## 10. Kiểm chứng

- [ ] Một lỗi runtime cố ý → được `ErrorHandler` bắt, không chỉ dừng ở log trình duyệt
- [ ] Một promise bị từ chối không ai bắt → cũng được ghi nhận
- [ ] Lỗi HTTP thường **không** sinh báo cáo lỗi runtime (không đếm hai lần)
- [ ] Màn lỗi chung hiển thị `traceId` đọc được
- [ ] `traceId` đó tìm ra được dòng log tương ứng ở BE
- [ ] Bộ thu lỗi có giới hạn tần suất: một lỗi lặp nhanh không sinh vô hạn báo cáo
- [ ] Báo cáo lỗi không chứa nội dung ô nhập của người dùng
- [ ] Lỗi bên trong chính bộ thu lỗi không làm hỏng app

---

## 11. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| `ErrorHandler` tùy biến + ba nguồn lỗi ngoài Angular | ✅ sẽ có | §3 |
| `ClientErrorReporter` có đầu ra cắm được | ✅ sẽ có | Đầu ra mặc định là console — §4 |
| Hiển thị `traceId` ở màn lỗi người dùng đọc được | ✅ sẽ có | §5.1 |
| Giới hạn tần suất và bọc kín bộ thu lỗi | ✅ sẽ có | §4.1 |
| Đích nhận báo cáo lỗi | ❌ chưa | Điều kiện: có môi trường staging dùng thật. Cần hợp đồng endpoint trước nếu chọn gửi về BE (§4) |
| Đo Web Vitals trên máy người dùng thật | ❌ chưa | Điều kiện: có nơi nhận số và có ngưỡng để so (§6.2, §6.3). Trước đó đo tay bằng công cụ trình duyệt |
| Nối `traceId` FE ↔ log BE thành một truy vấn | ❌ chưa | Điều kiện: BE có đường log tập trung — [`../be/07-observability.md`](../be/07-observability.md) |
| Ghi lại phiên thao tác của người dùng | ❌ chưa | Điều kiện: có loại lỗi không tái hiện được bằng mô tả; kèm bài toán che dữ liệu nhạy cảm |
| Gửi nội dung form trong báo cáo lỗi | ❌ loại, không hoãn `K30` | §4.1 — có thể chứa mật khẩu người dùng gõ nhầm ô |
| Tầng logger có mức độ riêng ở FE | ❌ loại, không hoãn `K31` | §9 — gần như luôn thành lớp bọc quanh console |

Một finding dạng *"FE thiếu X"* chỉ hợp lệ khi X mang trạng thái **✅ sẽ có** mà vắng mặt, hoặc khi điều kiện ở cột ghi chú của một dòng **❌ chưa** đã xảy ra. Dòng **❌ loại, không hoãn** chỉ đổi được bằng một ADR mới, không đổi được bằng một finding.

---

## 12. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| `traceId` sinh ra và gắn vào envelope thế nào ở BE | [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §3.1 (`TraceIdMiddleware`) |
| Envelope mang `traceId` | [`02-http-envelope.md`](02-http-envelope.md) |
| Ngân sách bundle và số đo | [`13-performance.md`](13-performance.md) §4 |
| Không rò rỉ dữ liệu nhạy cảm | [`14-security.md`](14-security.md) |
| Nợ hợp đồng cho endpoint nhận lỗi | [`../../contracts/README.md`](../../contracts/README.md) |
