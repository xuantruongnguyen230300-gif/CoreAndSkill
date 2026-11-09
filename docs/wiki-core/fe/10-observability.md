---
kind: luat
scope: core
verified: 2026-11-06
---

# 10. Quan sát được — lỗi runtime và số đo phía FE

> ✅ **CÓ THẬT** (đối chiếu cả tệp 2026-11-06). Hàng đã có code mang neo ngay ở §10; khẳng định hiện trạng ở §3, §4, §5 mang neo tại chỗ. Một hạng mục v1 còn thiếu và **chưa chốt**: lỗi runtime hiện gì cho người dùng, kèm `traceId` (§5.1) — `docs/Design/` chưa có spec cho nó; hàng thứ ba của §10.
>
> Đích nhận lỗi phía BE có code nhưng card [`../../contracts/client-errors.md`](../../contracts/client-errors.md) còn `DRAFT`: chưa ai gọi thử thật giữa FE và BE.
>
> Hiện trạng repo: [`../../README.md`](../../README.md) mục *Trạng thái repo*. Phía BE: [`../be/07-observability.md`](../be/07-observability.md). Hai phía nối với nhau bằng `traceId` — §5 là mục quan trọng nhất của file này.

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
// core/observability/global-error-handler.ts — rút gọn
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly reporter = inject(ClientErrorReporter);

  handleError(loi: unknown): void {
    try {
      // Lỗi HTTP interceptor đã xử lý — HttpErrorResponse, hay ApiFailureError dịch ra từ nó — không đếm hai lần.
      if (!(loi instanceof HttpErrorResponse || loi instanceof ApiFailureError)) {
        this.reporter.capture(loi);
      }
      console.error(loi);   // MỌI lỗi, kể cả lỗi HTTP
    } catch {
      // Bọc kín (§4.1): ném từ ErrorHandler là vòng lặp.
    }
  }
}
```

| Luật của bộ xử lý | Vì sao |
| --- | --- |
| Lỗi HTTP — `HttpErrorResponse`, và `ApiFailureError` mà `errorInterceptor` dịch ra từ nó — **không** vào bộ thu lỗi | Interceptor đã xử lý chúng: toast, lỗi ô, hết phiên. Đếm thêm ở đây là đếm hai lần (§2, §7) |
| **Mọi** lỗi, kể cả lỗi HTTP, vẫn `console.error` | Bộ xử lý này thay bản mặc định của Angular — bản chỉ làm đúng một việc là in ra `console` — và bộ nghe cửa sổ của Angular gọi `preventDefault()` nên trình duyệt không tự in nữa. Bỏ dòng này là lỗi biến mất khỏi `console` lúc phát triển (§8) |

✅ **CÓ THẬT** (đối chiếu 2026-11-06): `src/FE/src/app/core/observability/global-error-handler.ts`, chuỗi `return loi instanceof HttpErrorResponse || loi instanceof ApiFailureError;`; ca kiểm `src/FE/src/app/core/observability/global-error-handler.spec.ts`, chuỗi `lỗi HTTP đã qua interceptor → KHÔNG đếm hai lần (ApiFailureError)`.

**Ba nguồn lỗi nằm ngoài đường Angular tự gọi `ErrorHandler`** — Angular tự gọi nó cho lỗi trong template, handler gắn qua template, vòng đời component. Mỗi nguồn nối một cách:

| Nguồn | Nối bằng |
| --- | --- |
| Promise bị từ chối mà không ai bắt | Sự kiện `unhandledrejection` của cửa sổ — `provideBrowserGlobalErrorListeners()` của Angular đưa nó vào `ErrorHandler` |
| Lỗi ngoài vùng Angular (thư viện bên thứ ba, callback thuần) | Sự kiện `error` của cửa sổ — cùng hàm trên |
| Tài nguyên tải hỏng (ảnh, script, stylesheet) | Bộ nghe `error` riêng ở **giai đoạn capture**, chỉ nhận sự kiện có đích là phần tử |

Hai nguồn đầu đi qua hàm của Angular, **không** tự nghe lại `error` hay `unhandledrejection`: nghe hai lần là mỗi lỗi vào `ErrorHandler` hai lần. Nguồn thứ ba phải nghe riêng: sự kiện `error` của phần tử không nổi bọt, và bộ nghe của Angular gắn trên cửa sổ ở giai đoạn nổi bọt nên không thấy nó (đối chiếu `@angular/core` 21.2, hàm `provideBrowserGlobalErrorListeners`, 2026-11-06). Bộ nghe capture bỏ qua sự kiện có đích không phải phần tử — lỗi script thường đã tới bằng đường của Angular.

Composition root gọi một hàm, `provideCoreObservability()`, nối `ErrorHandler` cùng cả ba nguồn.

✅ **CÓ THẬT** (đối chiếu 2026-11-06): `src/FE/src/app/core/observability/provide-core-observability.ts`, chuỗi `provideBrowserGlobalErrorListeners(),` và chuỗi `provideEnvironmentInitializer(nghePhanTuTaiHong),`; `src/FE/src/app/core/observability/resource-error-listener.ts`, chuỗi `cuaSo.addEventListener('error', nghe, true);`; `src/FE/src/app/app.config.ts`, chuỗi `provideCoreObservability(),`. Ca kiểm đi hết đường tới đầu ra: `src/FE/src/app/core/observability/provide-core-observability.spec.ts`, chuỗi `promise bị từ chối mà không ai bắt (sự kiện unhandledrejection) → tới đầu ra`.

Nguồn thứ nhất là nguồn hay bị bỏ sót nhất, và nó chính là nơi mọi lỗi `async/await` không bắt rơi vào.

---

## 4. Gửi đi đâu

| Phương án | Được | Mất |
| --- | --- | --- |
| **Chỉ `console`** | Không tốn gì | Không thu được gì từ người dùng thật. Chỉ đúng cho môi trường phát triển |
| **Gửi về BE của chính mình** | Không thêm nhà cung cấp, dữ liệu không rời hệ thống, dễ nối `traceId` | Phải viết endpoint, phải chống lụt, phải quyết định lưu bao lâu |
| **Dịch vụ thu lỗi bên ngoài** | Gộp nhóm lỗi, cảnh báo, xem lại phiên — tất cả có sẵn | Dữ liệu ra ngoài; thêm một phụ thuộc trong bundle; chi phí |

**Quyết định cho v1 (chốt 2026-09-10): gửi về BE của chính mình.** `ClientErrorReporter` giữ đầu ra cắm được — môi trường phát triển ra `console`, môi trường thật ra endpoint khai ở [`../../contracts/client-errors.md`](../../contracts/client-errors.md).

**Bản dựng `production` gửi về BE, bản dựng `development` giữ `console`** — người dùng chốt 2026-11-06, không chờ có môi trường staging (dòng 2026-11-06 ở [`../../adr/nhat-ky-quyet-dinh.md`](../../adr/nhat-ky-quyet-dinh.md)). Chọn theo cờ `production` của tệp môi trường; bản dựng `e2e` của lưới giao diện mang cờ đó nên cũng cắm đầu ra HTTP, tới API giả.

✅ **CÓ THẬT** (đối chiếu 2026-11-06): `src/FE/src/app/app.config.ts`, chuỗi `return production ? [provideClientErrorOutput(HttpClientErrorOutput)] : [];` và chuỗi `...provideDauRaLoiTheoMoiTruong(environment.production),`; đầu ra mặc định khi không cắm gì: `src/FE/src/app/core/observability/client-error-output.ts`, chuỗi `factory: () => new ConsoleClientErrorOutput(),`; ca kiểm hai nhánh: `src/FE/src/app/app.config.spec.ts`, chuỗi `production: đầu ra là HttpClientErrorOutput`.

Không dùng dịch vụ thu lỗi bên ngoài: dữ liệu của cơ quan không rời hệ thống, và lỗi FE nối được với log BE qua `traceId` (§5) chỉ khi hai bên ở cùng một chỗ.

Vì sao vẫn giữ lớp trừu tượng thay vì gọi thẳng endpoint: chi phí của nó gần bằng 0 (một interface và một hiện thực tầm thường), còn gọi thẳng thì môi trường phát triển cũng bắn báo cáo về server, và đổi đích sau này thành một đợt sửa rải khắp nơi.

Hợp đồng của endpoint: [`../../contracts/client-errors.md`](../../contracts/client-errors.md). Ba ràng buộc ở §4.1 áp cho **mọi** đầu ra, kể cả `console`.

### 4.1 Ba ràng buộc bắt buộc, dù chọn đích nào

| Ràng buộc | Vì sao |
| --- | --- |
| **Không gửi dữ liệu cá nhân hay nội dung form.** Báo cáo mang đúng sáu field của [hợp đồng](../../contracts/client-errors.md) §1, không hơn. `message` và `stack` chỉ lấy văn bản **nằm sẵn** trong giá trị bị ném — `Error.message`, `Error.stack`, chuỗi bị ném, trường `message` kiểu chuỗi của object; **không** `String()` hay `JSON.stringify` một object, **không** đọc `cause`, `error`, `body`. URL trong `message`, `stack` và đường dẫn màn bỏ query, fragment | Một báo cáo lỗi kèm nội dung ô nhập có thể chứa mật khẩu người dùng gõ nhầm ô; một object bị ném có thể chính là gói dữ liệu form. BE lọc lần hai bằng `SensitiveTextRedactor` — lớp thứ hai, không thay lớp này |
| **Có giới hạn tần suất.** Mỗi tab tối đa **5 báo cáo trong mọi khoảng 60 giây**; cùng một lỗi — cùng loại, cùng câu, cùng đầu `stack` — lặp lại trong 60 giây chỉ báo một lần và không tính vào hạn mức (`architect` duyệt 2026-11-06, dòng cùng ngày ở [nhật ký](../../adr/nhat-ky-quyet-dinh.md)). Hạn mức mỗi tab phải nhỏ hơn hạn mức BE cho mỗi IP — mặc định đọc ở `CoreRateLimitOptions` | Một lỗi trong vòng lặp vẽ lại có thể sinh hàng nghìn báo cáo mỗi phút và tự làm sập chính hệ thống thu. Thấp hơn BE để vài tab sau cùng một IP không tự chặn nhau |
| **Thất bại của việc gửi không được làm hỏng app** | Bọc kín, không để một lỗi trong bộ thu lỗi lại sinh thêm lỗi — đây là vòng lặp cổ điển |

Hệ quả chấp nhận của ràng buộc thứ nhất: code tự ghép giá trị người dùng nhập vào câu của một `Error` thì giá trị đó đi lên cùng `message` — đừng ghép. Bộ lọc phía BE nhận ra theo tên trường, không nhận ra một giá trị trần. Của ràng buộc thứ hai: người dùng sau cùng một IP — NAT của cơ quan — chia chung hạn mức BE; vượt thì BE trả 429 và đầu ra HTTP ngừng gửi tới hết `Retry-After` (§4.2), báo cáo trong lúc đó bị bỏ.

✅ **CÓ THẬT** (đối chiếu 2026-11-06), cả ba ràng buộc thi hành trong `ClientErrorReporter`, **trước** khi báo cáo tới đầu ra, nên áp cho mọi đầu ra:

| Ràng buộc | Code | Ca kiểm |
| --- | --- | --- |
| Không dữ liệu cá nhân | `src/FE/src/app/core/observability/client-error-report.ts`, chuỗi `if (laObject(loi) && typeof loi['message'] === 'string') {` | `client-error-report.spec.ts`, chuỗi `Error kèm cause và thuộc tính phụ mang nội dung form → không field nào mang theo` |
| Giới hạn tần suất | `src/FE/src/app/core/observability/client-error-reporter.ts`, chuỗi `const TOI_DA_BAO_CAO_MOI_CUA_SO = 5;` và chuỗi `const CUA_SO_GIOI_HAN_MS = 60_000;` | `client-error-reporter.spec.ts`, chuỗi `lỗi nghìn lần KHÔNG tốn thêm hạn mức` |
| Bọc kín | `src/FE/src/app/core/observability/client-error-reporter.ts`, chuỗi `if (this.dangBao) {` | `client-error-reporter.spec.ts`, chuỗi `việc báo lỗi tự sinh lỗi rồi quay lại capture → lần vào lại bị bỏ, không vòng lặp` |

### 4.2 Đầu ra HTTP

`HttpClientErrorOutput` gửi `POST` tới đích của hợp đồng và làm đúng bốn điều, vì endpoint là best-effort:

| Điều | Vì sao |
| --- | --- |
| **Đi vòng chuỗi interceptor** — `HttpClient` dựng từ `HttpBackend` — và tự ghép `API_BASE_URL`, tự gắn `withCredentials: true` cùng `X-XSRF-TOKEN`. Đây là ngoại lệ có tên của luật F59 ([`../../RULES-FE.md`](../../RULES-FE.md)) | Lỗi của chính việc gửi không được đi qua `errorInterceptor` — không toast, không xử lý hết phiên, không lấy token rồi gửi lại — và không quay về `ErrorHandler`. Đi qua chuỗi thì mỗi lần gửi hỏng có thể sinh một báo cáo mới |
| **Chưa có token CSRF thì bỏ báo cáo**, không xin token | Hợp đồng cấm xin token chỉ để báo lỗi ([`../../contracts/client-errors.md`](../../contracts/client-errors.md), *Ghi chú*) |
| **Không thử lại**; mọi thất bại nuốt bằng một hàm xử lý lỗi tường minh | Không có hàm đó thì RxJS chuyển thất bại thành lỗi chưa bắt — quay lại `ErrorHandler` |
| **429 thì ngừng gửi** tới hết `Retry-After`, có trần; đọc không được thì nghỉ một cửa sổ | Thử lại ngay biến hạn mức thành vòng lặp |

Hệ quả của điều thứ hai: lỗi xảy ra trước khi lần lấy token lúc khởi động về thì không tới BE, chỉ còn `console`. Lỗi nạp tệp dịch lúc khởi động ([`08-i18n.md`](08-i18n.md) §2.3) rơi vào ca này mỗi khi tệp dịch hỏng về trước token — hai việc chạy song song, chưa đo bên nào thường về trước.

✅ **CÓ THẬT** (đối chiếu 2026-11-06): `src/FE/src/app/core/observability/http-client-error-output.ts`, chuỗi `private readonly http = new HttpClient(inject(HttpBackend));` và chuỗi `headers: { 'X-XSRF-TOKEN': token },`; ca kiểm `src/FE/src/app/core/observability/http-client-error-output.spec.ts`, chuỗi `request không qua interceptor nào của app` và chuỗi `chưa có token CSRF → bỏ báo cáo, KHÔNG xin token và không gửi`.

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

`ClientErrorReporter` giữ `traceId` của **request hoàn tất cuối cùng** và một số ít request trước đó — phản hồi thành công lẫn thất bại; `errorInterceptor` đưa thân phản hồi cho nó. Khi một lỗi runtime xảy ra, gần như luôn có một request vừa hoàn tất liên quan tới nó.

Đây là suy luận xác suất, không phải quan hệ nhân quả chắc chắn — người đọc log phải biết vậy để không kết luận sai. Nhưng nó rẻ và trong thực tế đúng phần lớn thời gian. Hai đầu ra nói điều đó theo hai cách:

| Đầu ra | Mang gì | Chỗ nói rõ đó là suy luận |
| --- | --- | --- |
| BE | **Đúng một** `traceId` — của request hoàn tất cuối cùng — ở field `traceId` của hợp đồng | Nghĩa của field ở [`../../contracts/client-errors.md`](../../contracts/client-errors.md) §1. BE ghi nó dưới tên riêng `ClientTraceId`, không lẫn với `TraceId` của chính lần gửi báo cáo |
| `console` | `traceId` cuối cùng trong báo cáo, cộng các `traceId` trước đó in kèm | Câu in cùng các `traceId` trước: chỉ là suy luận, không chắc là nguyên nhân |

Các `traceId` trước **không** lên BE: hợp đồng có một field, thêm field là đổi hợp đồng API — một ADR ([`../../adr/README.md`](../../adr/README.md) §2) — cho một nhu cầu chưa ai có.

✅ **CÓ THẬT** (đối chiếu 2026-11-06): `src/FE/src/app/core/observability/client-error-reporter.ts`, chuỗi `traceId: this.cacTraceId.at(-1) ?? null,`; `src/FE/src/app/core/interceptors/error.interceptor.ts`, chuỗi `boThuLoi.ghiNhanTuPhanHoi(`; `src/FE/src/app/core/observability/client-error-output.ts`, chuỗi `chỉ là suy luận từ các request hoàn tất`; `src/BE/Core/CoreAndSkill.Core.Application/ClientErrors/ReportClientErrorCommandHandler.cs`, chuỗi `trace {ClientTraceId}`. Ca kiểm: `src/FE/src/app/core/observability/client-error-reporter.spec.ts`, chuỗi `báo cáo mang traceId của phản hồi cuối cùng; các traceId trước đi riêng`.

### 5.3 Điều kiện phía BE

`traceId` chỉ có giá trị nếu BE **thật sự ghi nó vào log** cho mọi request, kể cả request thành công. Nếu BE chỉ ghi `traceId` ở nhánh lỗi thì một nửa công dụng biến mất. Xem [`../be/07-observability.md`](../be/07-observability.md).

✅ **CÓ THẬT** (đối chiếu 2026-11-06): mỗi request trừ đường health check có một dòng log mang `traceId` — `src/BE/Core/CoreAndSkill.Core.Web/Middleware/RequestLogMiddleware.cs`, chuỗi `responded {StatusCode} in {ElapsedMs} ms (trace {TraceId})`.

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

## 8. Log ở FE — nói ngắn về thứ dễ lạm dụng

| Quy tắc | Vì sao |
| --- | --- |
| Không để lại `console.log` trong code đã hoàn thành | Nó là nhiễu, và đôi khi là rò rỉ dữ liệu — log một object người dùng là log cả những trường không nên hiện |
| `console.error` giữ lại ở đường xử lý lỗi chung | Đây là chỗ nó có ích thật |
| Không tự dựng một tầng logger có mức độ ở FE | Ở BE thì đáng; ở FE thì gần như luôn thành một lớp bọc quanh `console` mà không ai đọc đầu ra |

---

## 9. Kiểm chứng

- [ ] Một lỗi runtime cố ý → được `ErrorHandler` bắt, không chỉ dừng ở log trình duyệt
- [ ] Một promise bị từ chối không ai bắt → cũng được ghi nhận
- [ ] Lỗi HTTP thường **không** sinh báo cáo lỗi runtime (không đếm hai lần)
- [ ] Màn lỗi chung hiển thị `traceId` đọc được
- [ ] `traceId` đó tìm ra được dòng log tương ứng ở BE
- [ ] Bộ thu lỗi có giới hạn tần suất: một lỗi lặp nhanh không sinh vô hạn báo cáo
- [ ] Báo cáo lỗi không chứa nội dung ô nhập của người dùng
- [ ] Lỗi bên trong chính bộ thu lỗi không làm hỏng app

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> Đối chiếu `src/` ngày 2026-11-06: hàng `✅ CÓ THẬT` mang neo ngay trong ô; hàng còn `✅ sẽ có` chưa có code. Ý nghĩa các ký hiệu: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| `ErrorHandler` tùy biến + ba nguồn lỗi ngoài Angular | ✅ CÓ THẬT (đối chiếu 2026-11-06) — `src/FE/src/app/core/observability/provide-core-observability.ts`, chuỗi `{ provide: ErrorHandler, useClass: GlobalErrorHandler },`; neo từng nguồn ở §3 | §3 |
| `ClientErrorReporter` có đầu ra cắm được | ✅ CÓ THẬT (đối chiếu 2026-11-06) — `src/FE/src/app/core/observability/client-error-output.ts`, chuỗi `export function provideClientErrorOutput(`; ca kiểm `src/FE/src/app/core/observability/provide-core-observability.spec.ts`, chuỗi `provideClientErrorOutput thay được đầu ra` | Đầu ra mặc định là `console` — §4 |
| Hiển thị `traceId` ở màn lỗi người dùng đọc được | ✅ CÓ THẬT (đối chiếu 2026-11-05) cho lỗi HTTP — toast hiện nó: `src/FE/src/app/shared/ui/toast/toast.component.ts`, chuỗi `detail: message.traceId ?`; interceptor truyền: `src/FE/src/app/core/interceptors/error.interceptor.ts`, chuỗi `body?.traceId ?? null`. ✅ sẽ có cho lỗi runtime — `GlobalErrorHandler` chỉ báo và in ra `console`, không hiện gì; **chưa chốt** lỗi runtime hiện gì cho người dùng: `docs/Design/` chưa có spec | §5.1 |
| Giới hạn tần suất và bọc kín bộ thu lỗi | ✅ CÓ THẬT (đối chiếu 2026-11-06) — neo ở §4.1 | §4.1 |
| Đích nhận báo cáo lỗi | ✅ CÓ THẬT (đối chiếu 2026-11-06) — bản `production` gửi về BE, bản `development` giữ `console` (người dùng chốt 2026-11-06): `src/FE/src/app/app.config.ts`, chuỗi `...provideDauRaLoiTheoMoiTruong(environment.production),`; đầu ra HTTP: `src/FE/src/app/core/observability/http-client-error-output.ts`, chuỗi `const DUONG_DAN_BAO_LOI = '/core/client-errors';`. Card hợp đồng còn `DRAFT`, chưa gọi thử thật | §4, §4.2 |
| Đo Web Vitals trên máy người dùng thật | ❌ chưa | Điều kiện: có nơi nhận số và có ngưỡng để so (§6.2, §6.3). Trước đó đo tay bằng công cụ trình duyệt |
| Nối `traceId` FE ↔ log BE thành một truy vấn | ❌ chưa | Điều kiện: BE có đường log tập trung — [`../be/07-observability.md`](../be/07-observability.md) |
| Ghi lại phiên thao tác của người dùng | ❌ chưa | Điều kiện: có loại lỗi không tái hiện được bằng mô tả; kèm bài toán che dữ liệu nhạy cảm |
| Gửi nội dung form trong báo cáo lỗi | ❌ loại, không hoãn `K30` | §4.1 — có thể chứa mật khẩu người dùng gõ nhầm ô |
| Tầng logger có mức độ riêng ở FE | ❌ loại, không hoãn `K31` | §8 — gần như luôn thành lớp bọc quanh console |

> Cách đọc ba ký hiệu của bảng trên — và khi nào *"FE thiếu X"* là finding: [`../README.md`](../README.md) §9.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| `traceId` sinh ra và gắn vào envelope thế nào ở BE | [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §3.1 (`TraceIdMiddleware`) |
| Envelope mang `traceId` | [`02-http-envelope.md`](02-http-envelope.md) |
| Ngân sách bundle và số đo | [`13-performance.md`](13-performance.md) §4 |
| Không rò rỉ dữ liệu nhạy cảm | [`14-security.md`](14-security.md) |
| Nợ hợp đồng cho endpoint nhận lỗi | [`../../contracts/README.md`](../../contracts/README.md) |
| Request API nào được đi vòng chuỗi interceptor | Hàng F59 ở [`../../RULES-FE.md`](../../RULES-FE.md) |
