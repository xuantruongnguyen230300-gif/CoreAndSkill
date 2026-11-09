---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 07. Quan sát hệ thống — log, metric, health check, trace

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu **theo từng mục** ở bảng dưới). Mục nào bảng không nhắc
> thì **chưa ai mở `src/` ra đối chiếu**, nên đừng đọc file này như bảng tiến độ — và cũng đừng đọc nó như
> "chưa ai bắt đầu".
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | Bộ lọc trường nhạy cảm của §5 đã có thật: `src/BE/Core/CoreAndSkill.Core.Application/Common/Logging/SensitiveTextRedactor.cs`, kèm test kiểm chính bộ lọc và test đọc dòng log thật của đường `client-errors` | Bộ lọc gắn ở **tầng cấu hình log** thay vì được gọi tay ở từng chỗ ghi — luật **S13** và **B11** ở [`../../RULES-BE.md`](../../RULES-BE.md); B11 chưa có cổng |
> | §2.3 — đầu ra: ngoài Development console dùng formatter JSON với đủ bốn ràng buộc, Development giữ console văn bản — neo ở cuối §2.3 (đối chiếu 2026-10-06) | Một dòng JSON thật đọc ra từ đầu ra chuẩn của bản chạy ngoài Development — **chưa ai đo**; test hôm nay chỉ kiểm cấu hình formatter |
> | §4.1 — mức log máy dev khớp bảng: `src/BE/CoreAndSkill.Api/appsettings.Development.json`, chuỗi `"Microsoft.EntityFrameworkCore": "Warning"` (đối chiếu 2026-10-06); bản cài thật cùng mức — neo ngay dưới bảng §4.1 | — |
> | §6 — một dòng log mỗi request do middleware của Core ghi, trừ request tới `/health/*` — neo ở hàng của nó trong §6 (đối chiếu 2026-10-06) | — |
> | Log có cấu trúc trong code, mã lần gọi, cách dùng mức log ở §4, chỉ số, health check: **chưa ai đối chiếu với code** | Đối chiếu từng mục, rồi mới lật `verified:` của file này |
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

Cách sai còn có một hệ quả kín: mọi dòng log trở thành một khuôn khác nhau, nên không nhóm được *"lỗi này xảy ra bao nhiêu lần"*. Lọc theo trường cũng chỉ đúng khi tên trường đáng tin: **một tên trường log mang một nghĩa trong toàn Core; mã của một bản ghi đã lưu mang tên theo bản ghi đó** — `{JobId}` luôn là `core.job.id`, mã của thứ khác mang tên khác. Một tên chở hai giá trị thì lọc theo nó ra hai nhóm dòng không nối được với nhau, và không gì báo chỗ đứt — luật B35 ở [`../../RULES-BE.md`](../../RULES-BE.md) §6.

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

### 2.3 Đầu ra theo môi trường

| Môi trường | Định dạng | Vì sao |
| --- | --- | --- |
| Development | Văn bản của console mặc định ASP.NET Core | Người đọc là người, ngay ở cửa sổ đang chạy |
| Mọi môi trường khác | **JSON một dòng mỗi bản ghi**, ra đầu ra chuẩn, bằng formatter JSON có sẵn của `Microsoft.Extensions.Logging.Console` (`AddJsonConsole`) — không thêm gói | Hạ tầng thu gom (§10) lọc theo trường; Serilog cho thêm nhiều đích ghi, mà Core chỉ ghi một đích |

Bốn ràng buộc khi nối — ba thiết lập cuối là chỗ formatter mặc định làm hỏng log mà không báo, kiến trúc sư đã chạy thử formatter của runtime .NET 10 ngày 2026-10-06:

1. **Nối trong Core, trên đường `AddCore`** — không ở host ([`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §3).
2. **`IncludeScopes = true`.** Mã lần gọi nằm ở phạm vi log (§3); formatter không xuất phạm vi thì mã đó mất khỏi mọi dòng.
3. **`TimestampFormat` dạng ISO 8601 kèm `UseUtcTimestamp = true`.** Để trống `TimestampFormat` thì dòng JSON không có trường thời điểm nào.
4. **`JsonWriterOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping`.** Bộ mã hoá mặc định thoát mọi ký tự ngoài ASCII thành `\uXXXX` — thông điệp tiếng Việt không đọc được bằng mắt qua `docker logs`. Bộ mã hoá nới vẫn thoát nháy kép, gạch chéo ngược và ký tự điều khiển, nên mỗi dòng vẫn là một JSON hợp lệ không xuống dòng; nó chỉ không an toàn để nhúng vào HTML, mà log không đi vào HTML.

**Nối định dạng JSON chưa thoả luật B11.** Formatter JSON có sẵn là lớp `internal sealed`, không có chỗ chen bộ lọc trường nhạy cảm của §5. Bộ lọc ở tầng cấu hình log cần một formatter hay một lớp bọc provider của chính Core — chọn cách nào là quyết định riêng lúc thi công B11. Cho tới lúc đó, ngoại lệ do framework tự ghi đi vào trường `Exception` của dòng JSON, chưa lọc — log do framework ghi ở §5, mục *Điểm mù*.

✅ **CÓ THẬT** (đối chiếu 2026-10-06):

| Ràng buộc | Neo |
| --- | --- |
| Nối trong Core, trên đường `AddCore` — lời đăng ký dịch vụ đầu tiên | `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreServiceCollectionExtensions.cs`, chuỗi `services.AddCoreLogging(environment);` |
| Development trả về không đổi gì; môi trường khác gọi `AddJsonConsole` với ba thiết lập 2–4 | `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreLoggingExtensions.cs`, chuỗi `if (environment.IsDevelopment())` và chuỗi `logging.AddJsonConsole(` |
| Test: `Production`, `Staging` ra formatter JSON kèm ba thiết lập; `Development` không ra JSON — chạy xanh trên máy dev Windows 2026-10-06 | `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Web/ConsoleLogFormatTests.cs`, chuỗi `OutsideDevelopment_ConsoleFormatter_IsJson_WithTheThreeSettings` |

Hai chỗ test **chưa** chứng minh: *một dòng mỗi bản ghi* chỉ kiểm qua cờ `JsonWriterOptions.Indented = false`, chưa ai đọc một dòng JSON thật ra đầu ra chuẩn; ràng buộc 3 chỉ kiểm `TimestampFormat` không rỗng, không kiểm dạng ISO 8601 — dạng đó nằm ở code, chuỗi `"yyyy-MM-ddTHH:mm:ss.fffZ"`.

---

## 3. Mã lần gọi xuyên suốt

### 3.1 Vòng đời

```text
FE / hạ tầng gửi header traceparent (nếu có)
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
2. **Theo chuẩn W3C Trace Context: nhận header `traceparent` nếu có, sinh mới nếu không.** FE gửi lên thì hai bên nối được với nhau. Mã lần gọi lấy từ đâu và gán vào chỗ nào: đọc [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §3.
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

### 4.1 Mức log ở máy dev

| Nhóm nguồn ghi | Mức | Vì sao |
| --- | --- | --- |
| Mặc định | Information | Thấy được mọi sự kiện đáng ghi ở §6 |
| `Microsoft.AspNetCore` | Warning | Mỗi request sinh vài dòng Information của framework — che mất dòng của Core |
| `Microsoft.EntityFrameworkCore` | Warning | Ở Information, EF ghi một dòng cho mỗi câu SQL |

Khai ở `appsettings.Development.json` của host. Cần xem SQL — ví dụ khi dò N+1 ([`../../quy-uoc/be-performance.md`](../../quy-uoc/be-performance.md) §4.3) — thì bật riêng nhóm `Microsoft.EntityFrameworkCore.Database.Command` ở Information cho máy mình, bằng `user-secrets` với khoá `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command`, rồi gỡ khoá đó khi xong. Không commit mức Debug hay mức bật SQL vào `appsettings.Development.json`: tệp đó dùng chung cho mọi máy dev.

Bản cài thật khai cùng ba mức ở `appsettings.json` của host — `Microsoft.EntityFrameworkCore` ở Warning, vì một dòng cho mỗi câu SQL ở môi trường thật tăng theo lưu lượng và chìm mất dòng của Core. ✅ Đối chiếu 2026-10-06: `src/BE/CoreAndSkill.Api/appsettings.json`, chuỗi `"Microsoft.EntityFrameworkCore": "Warning"`.

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
| **Thông điệp lỗi của thư viện ngoài** | Một số thư viện đưa cả giá trị đầu vào vào thông điệp lỗi | Thông điệp của **mọi** tầng ngoại lệ đi qua bộ lọc trước khi vào log; Core không trao đối tượng ngoại lệ cho logger — mục *Ghi một ngoại lệ vào log* ngay dưới |

Cách canh: một bộ lọc trường nhạy cảm ở tầng cấu hình log — che theo **danh sách tên trường** — cộng một test khẳng định bộ lọc đang hoạt động. Bộ lọc không có test kiểm chính nó thuộc đúng loại "cổng hỏng âm thầm" ở [`04-testing-strategy.md`](04-testing-strategy.md) §3.

### Ghi một ngoại lệ vào log — định nghĩa gốc

Luật **B28** ([`../../RULES-BE.md`](../../RULES-BE.md) §6), người dùng chốt 2026-09-29 ([`../../adr/nhat-ky-quyet-dinh.md`](../../adr/nhat-ky-quyet-dinh.md)): dòng log lỗi **giữ ngăn xếp**, và thông điệp của **mọi** tầng ngoại lệ đi qua bộ lọc trường nhạy cảm trước khi vào log. Điều §6 đòi — đủ ngăn xếp — và điều bảng trên cấm — thông điệp thư viện ngoài chưa lọc — cùng đúng nhờ một hàm dựng chuỗi, không nhờ đối tượng ngoại lệ.

> ✅ **CÓ THẬT** (đối chiếu 2026-10-01 — code từ lượt `backend-expert` 2026-09-30, ArchTest từ lượt `test-engineer`
> 2026-10-01).
>
> `FailureText.DescribeWithStack(Exception exception)` có, cùng lớp với `Describe`: `src/BE/Core/CoreAndSkill.Core.Application/Common/Logging/FailureText.cs`, kèm ba unit test logic của hàm —
> `DescribeWithStack_FiltersTheMessage_OfEveryLayer`, `DescribeWithStack_KeepsTheStackFrames_OfEveryLayer`,
> `DescribeWithStack_WalksEveryBranch_OfAnAggregateException` (`src/BE/Tests/CoreAndSkill.Core.UnitTests/Common/B4SupportTests.cs`).
> Không lời gọi `ILogger` nào của Core còn trao nguyên đối tượng ngoại lệ, không health check nào còn trao ngoại lệ
> cho `HealthCheckResult`, không dòng log mức Warning trở lên nào còn dùng `FailureText.Describe` (hai health check, ba chỗ gọi,
> nối câu cố định với `FailureText.Describe(ex)` trong mô tả thay vì trao `ex` — hợp lệ, vì đó là mô tả health
> check, không phải log). Ba ArchTest giữ nguyên trạng này: `CoreSource_MustNotPass_AnExceptionObject_ToILogger`,
> `CoreSource_MustNotPass_AnException_ToHealthCheckResult`, `CoreSource_WarningOrHigherLog_MustNotUse_FailureTextDescribe`
> (`src/BE/Tests/CoreAndSkill.ArchTests/LoggerExceptionArgumentTests.cs`), chạy xanh 2026-10-04 (lượt `test-engineer`) với bộ dò nhận lời gọi qua `?.` như qua `.` và coi mọi `.Log(` có receiver là đỏ — chuỗi `MemberBindingExpressionSyntax` trong `TryGetInvokedMethodName` và chuỗi `name == "Log" && arguments.Count > 0` — cùng phép dò vế 1 lần theo tham số kiểu ngoại lệ — chuỗi `ExceptionParameterNamesEnclosing` và `IsInsideFailureTextCall` — đều trong `src/BE/Tests/CoreAndSkill.ArchTests/Support/LoggerExceptionArgumentScanner.cs`, đối chiếu 2026-10-04 với cột *Đỏ khi* bên dưới. Phần đối số tên của vế 1 — template tìm theo `message:`, chuỗi `FindMessageArgument`; `exception:` khác `null`, chuỗi `{ Name.Identifier.Text: "exception" } && !IsNullLiteral` — kiến trúc sư đọc đối chiếu cùng ngày, chưa ghi lượt chạy. Luật **B28** ở
> [`../../RULES-BE.md`](../../RULES-BE.md).

**Ba vế, không danh sách miễn trừ:**

1. **Không trao cho `ILogger` đối tượng ngoại lệ, hay bất cứ thứ gì lấy từ nó, trừ qua một hàm của `FailureText`** — không overload nhận `Exception`, dù trao theo vị trí (`LogError(ex, …)`) hay qua đối số tên (`LogError(exception: ex, …)`), không overload tổng quát `Log(<mức>, …)` dù mức viết bằng `LogLevel.…` hay bằng một biến, dù có trao ngoại lệ hay không: Core chỉ gọi `Log<Mức>(`; không giá trị tham số nào, kể cả lỗ `{…}` của chuỗi nội suy, nhắc tới một biến giữ ngoại lệ ở bất cứ đâu trong biểu thức — chính biến đó, `.Message`, `.ToString()`, tầng trong (`.InnerException`, `.GetBaseException()`), phép ép kiểu `(object)ex`, `ex!`, hay một thuộc tính bất kỳ như `ex.GetType().Name` — trừ khi biến nằm trong đối số của một lời gọi `FailureText.<hàm>(…)`. Biến giữ ngoại lệ là mọi biến mang một ngoại lệ, không riêng biến khai ở `catch`: tham số kiểu ngoại lệ của phương thức, hàm dựng hay lambda cũng là nó, biến trung gian cũng vậy — phần cổng nhận ra nằm ở cột *Đỏ khi* bên dưới, phần còn lại ở mục *Điểm mù*. Cả tên kiểu cũng đi qua `FailureText`: chuỗi của `DescribeWithStack` đã mở mỗi tầng bằng tên kiểu (bảng dưới). Provider log gọi `ToString()` của giá trị nhận được — với một tầng ngoại lệ, đó là thông điệp chưa lọc kèm ngăn xếp của tầng đó và mọi tầng bên trong, ghi **nguyên văn**; bộ lọc không có chỗ chen vào giữa.
2. **Không trao ngoại lệ cho `HealthCheckResult`** — `Unhealthy(…, ex)`, `Degraded(…, ex)`, `new HealthCheckResult(…, exception: …)`. Bộ chạy health check của framework ghi ngoại lệ của một kết quả không lành vào log mức Error (theo mã nguồn framework — **chưa đo** trên host này): một đường rò nguyên ngoại lệ gián tiếp. Nguyên nhân đi vào mô tả — câu cố định nối `FailureText.Describe(ex)`. Mô tả chỉ vào log: bộ ghi phản hồi của `/health/*` giữ mặc định, chỉ ghi trạng thái (§8).
3. **Dòng log mức Warning trở lên mô tả một ngoại lệ bằng `FailureText.DescribeWithStack(ex)`**, truyền vào tham số tên `{Failure}`. `FailureText.Describe(ex)` — một tầng, không ngăn xếp, cắt ở `FailureText.MaxLength` — chỉ còn cho cột lưu trong DB (`core.outbox_message.last_error`), cho mô tả health check, và cho log mức Debug hay Information.

**Hàm `FailureText.DescribeWithStack(Exception exception)`** — `public static string`, cùng lớp với `Describe`:

| Phần | Cách dựng |
| --- | --- |
| Mỗi tầng | Một dòng `<tên kiểu đầy đủ>: <thông điệp đã qua SensitiveTextRedactor.Redact, cắt ở MaxLength>`, tiếp theo là ngăn xếp của **chính tầng đó** (`StackTrace`) nguyên văn; tầng không có ngăn xếp thì bỏ phần này |
| Thứ tự | Từ ngoài vào trong theo `InnerException`; mỗi tầng trong mở đầu bằng ` ---> `, như `Exception.ToString()` |
| `AggregateException` | Duyệt **mọi** phần tử của `InnerExceptions` theo thứ tự, mỗi phần tử duyệt tiếp như một chuỗi riêng; không đi `InnerException` của chính nó — đó là phần tử đầu, đã duyệt |
| Không bao giờ có | `Data`, thuộc tính riêng của lớp ngoại lệ, thông điệp chưa lọc |
| Không cắt | Tổng độ dài — log không phải cột DB; chỉ từng thông điệp bị cắt |

Ngăn xếp không qua bộ lọc: khung gọi là siêu dữ liệu biên dịch — tên phương thức, kiểu và tên tham số, tệp và dòng — không mang giá trị lúc chạy.

**Hai lệnh liệt kê chỗ còn lệch vế 1 và vế 2** — PASS khi cả hai không in dòng nào. Lệnh thứ nhất đọc dòng mở lời gọi log và dòng ngay sau, coi biến ngoại lệ là tên `ex` hay `exception`, và bỏ qua cả dòng có `FailureText.<hàm>(ex…)` — dòng vừa có lời gọi đó vừa có một tham chiếu khác thì lọt. Phép đo chính thức là ArchTest, lệnh chỉ để tra nhanh:

```bash
grep -rnE -A1 '\.Log(Trace|Debug|Information|Warning|Error|Critical)?\(' src/BE/Core src/BE/CoreAndSkill.Api --include=*.cs \
  | grep -E '\b(ex|exception)\b' | grep -vE 'FailureText\.[A-Za-z]+\((ex|exception)\b'
grep -rnE 'HealthCheckResult\.(Unhealthy|Degraded)\([^)]*,\s*(ex|exception)\)' src/BE/Core src/BE/CoreAndSkill.Api --include=*.cs
```

**Cổng — ArchTest quét cú pháp**, tệp `LoggerExceptionArgumentTests.cs` và bộ dò `Support/LoggerExceptionArgumentScanner.cs` dưới `src/BE/Tests/CoreAndSkill.ArchTests/`, tầm `ProductSourceFiles.Core()` — năm project Core cùng host, như mọi cổng quét cú pháp khác:

| Vế | Test | Đỏ khi |
| --- | --- | --- |
| 1 | `CoreSource_MustNotPass_AnExceptionObject_ToILogger` | Lời gọi `Log<Mức>(` có đối số ở vị trí thông điệp **không** phải biểu thức chuỗi — literal, chuỗi nội suy, phép nối `+` của chúng; vị trí thông điệp là đối số tên `message:` khi có, không thì đối số đầu không mang tên, nên `LogError(exception: null, message: "…")` xanh; lời gọi `Log<Mức>(` có đối số tên `exception:` khác literal `null`, bất kể giá trị — `exception: ex`, `exception: attempt.Exception` — còn `exception: null` tường minh thì xanh, cùng khuôn vế 2; mọi lời gọi `.Log(` có receiver và ít nhất một đối số, bất kể đối số đầu — mức log viết `LogLevel.*` hay là biến (`logger.Log(level, ex, "…")`) đều đỏ, và vì bộ dò không có semantic model, `.Log(` trên một receiver không phải logger cũng đỏ; một đối số mà cây con — kể cả lỗ `{…}` của chuỗi nội suy — có tham chiếu tới một biến được theo dõi, trừ tham chiếu nằm trong đối số của một lời gọi `FailureText.<hàm>(…)`. Biến được theo dõi: biến khai ở mệnh đề `catch (… tên)` bao quanh; tham số có kiểu khai báo mà định danh cuối kết thúc bằng `Exception` — kể cả `Exception?`, `System.Exception`, `DbUpdateException` — của phương thức, hàm dựng, toán tử, hàm cục bộ, lambda và anonymous method có kiểu tường minh, primary constructor, khi lời gọi log nằm trong thân chính thành viên đó (primary constructor: cả thân kiểu). Đỏ: `ex`, `ex.Message`, `ex.InnerException`, `ex.GetBaseException()`, `(object)ex`, `ex!`, `ex.GetType().Name`, `$"…{ex.InnerException}…"` — với `ex` khai ở `catch`, hay là tham số `Exception ex`, `DbUpdateException? ex`, `(Exception ex) => …`. Xanh: `FailureText.DescribeWithStack(ex.InnerException!)`. Lời gọi qua `?.` — `logger?.LogError(…)`, `logger?.Log(…)` — được nhận như qua `.` ở mọi dạng trên |
| 2 | `CoreSource_MustNotPass_AnException_ToHealthCheckResult` | `HealthCheckResult.Unhealthy(`/`Degraded(` có đối số vị trí thứ hai hay đối số tên `exception:` khác literal `null`; `new HealthCheckResult(` có đối số vị trí thứ ba hay `exception:` khác `null` |
| 3 | `CoreSource_WarningOrHigherLog_MustNotUse_FailureTextDescribe` | `LogWarning(`, `LogError(` hay `LogCritical(` — qua `.` hay qua `?.`, như `logger?.LogWarning(` — có một đối số mà cây con chứa lời gọi `FailureText.Describe(`, kể cả `FailureText.Describe(ex) + "…"`. Dạng `.Log(<mức>, …)` đã đỏ ở vế 1 |
| T6 | `CoreSource_MustNotPass_AnExceptionObject_ToILogger_ScansRealLoggerCalls` | Bộ dò thấy 0 lời gọi log — `Log<Mức>(` hay `.Log(`, tính cả lời gọi qua `?.` — hoặc 0 lời gọi `HealthCheckResult.*`, trên source thật |

Nhóm `Detector_B28_*` (T1) chạy trên source dựng sẵn: mỗi dạng ở cột *Đỏ khi* một ca đỏ, kèm ca xanh sinh đôi — `DescribeWithStack(ex)` làm giá trị `{Failure}`; `Describe(ex)` trong `LogInformation`; `Unhealthy("…")` chỉ có mô tả. Logic của hàm có unit test riêng: `DescribeWithStack_FiltersTheMessage_OfEveryLayer` — tầng trong mang `password: …` thì chuỗi ra không còn giá trị đó; `DescribeWithStack_KeepsTheStackFrames_OfEveryLayer`; `DescribeWithStack_WalksEveryBranch_OfAnAggregateException`.

**Điểm mù — nói ra để không ai tin cổng quá:**

- Ngoại lệ, hay thứ lấy từ nó, đi qua một biến trung gian rồi mới vào log — `var e2 = ex;`, `var inner = ex.InnerException;` — hay qua biến mẫu `is XException x`: vế 1 chỉ lần theo tên biến khai ở mệnh đề `catch` và tên tham số có kiểu khai báo kết thúc bằng `Exception`. Ngoại lệ đứng ở vị trí thông điệp thì vẫn bị bắt ở mọi dạng, vì vị trí đó phải là biểu thức chuỗi; trao qua đối số tên `exception:` cũng bị bắt, bất kể giá trị. Còn lọt một dạng: template đi bằng đối số tên `message:` mà ngoại lệ đứng **trước** nó theo vị trí — `LogError(attempt.Exception, message: "…")`: phép dò thông điệp xét đối số `message:`, phép dò đối số tên chỉ xét `exception:`, phép dò biến chỉ thấy biến được theo dõi. Lớp bắt hôm nay là review; bản sửa phép dò cho dạng này theo dõi ở GitHub Issues, mã B28.
- Tham số lambda không khai kiểu — `e => …`, `(e) => …`: kiểu suy ra từ ngữ cảnh, mà bộ dò chỉ đọc cú pháp, không có semantic model, nên không biết `e` là ngoại lệ. Ca `Detector_B28_Ignores_UntypedLambdaParameter_KnownBlindSpot` khoá đúng điểm mù này — mở rộng bộ dò làm ca đó đỏ, và mục này phải sửa cùng lượt. Viết `(Exception e) => …` thì vào tầm. Lớp bắt là review.
- Tham số mang ngoại lệ nhưng kiểu khai báo không kết thúc bằng `Exception` — bí danh `using E = System.Exception;`, kiểu cơ sở `object`, tham số kiểu generic ràng buộc `where T : Exception` — và mảng `params Exception[]`: không được theo dõi. Lớp bắt là review.
- Giá trị `FailureText.Describe(ex)` gán vào một field, property hay biến rồi mới vào dòng log mức Warning trở lên: vế 3 chỉ đọc cú pháp tại chính chỗ gọi log, không theo dữ liệu qua biến trung gian. Lớp bắt là review.
- Chỗ trừ `FailureText.<hàm>(…)` tin mọi hàm của lớp đó đã lọc thông điệp: một hàm của `FailureText` trả thông điệp chưa lọc thì cổng vẫn xanh. Lớp bắt là unit test của chính hàm đó và review.
- Log đi qua `LoggerMessage.Define` hay phương thức sinh từ `[LoggerMessage]` nhận tham số `Exception`, hay lời gọi không receiver — `Log(…)` gọi trong chính một lớp cài `ILogger`: bộ dò chỉ biết lời gọi `Log<Mức>(` và `.Log(` có receiver (kể cả qua `?.`). Core hôm nay không dùng các cách này; dùng thì mở rộng bộ dò **trước**.
- Log do **framework** tự ghi — middleware xử lý ngoại lệ, bộ chạy health check, host khi một `BackgroundService` ném ra ngoài, EF Core — nằm ngoài tầm vì cổng chỉ đọc source của Core. Chỉ bộ lọc ở tầng cấu hình log — luật **B11**, chưa có cổng — mới phủ được đường này. Một đường cùng loại mà bộ lọc đó cũng **không** che: ngoại lệ thoát khỏi `Program.cs` thì runtime .NET in `ToString()` của nó ra stderr — thông điệp chưa lọc kèm ngăn xếp của mọi tầng — không qua `ILogger`, nên không qua bộ lọc nào gắn ở tầng cấu hình log. Ngoại lệ Core cố ý ném để từ chối khởi động vì thế không gắn ngoại lệ gốc làm `InnerException`; chẩn đoán nằm ở dòng `LogError` kèm `DescribeWithStack` ngay trước lệnh ném (người dùng chốt 2026-10-04, [`../../adr/nhat-ky-quyet-dinh.md`](../../adr/nhat-ky-quyet-dinh.md); không thêm vế vào B28). ✅ Đối chiếu 2026-10-04: `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/DatabaseSchemaStartupCheck.cs`, chuỗi `CannotReachDatabase(int attempts, TimeSpan elapsed, string reason)`; test `NonTransientFailure_ThrownExceptionToString_DoesNotContainTheOriginalMessage`, `WaitExpired_ThrownExceptionToString_DoesNotContainTheOriginalMessage` (`src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Web/DatabaseSchemaStartupCheckTests.cs`). Ngoại lệ **không** do Core ném — lỗi lập trình, lỗi của thư viện thoát ra ngoài — vẫn đi đường này; lớp bắt là review.
- Giá trị nhạy cảm gõ thẳng vào **template** vẫn lọt — việc của luật S13 và review.

**Cái giá.** Provider log nhận chuỗi, không nhận đối tượng: các trường có cấu trúc của ngoại lệ — kiểu, ngăn xếp tách riêng theo quy ước OpenTelemetry — mất, nên nơi thu log muốn gom lỗi theo kiểu phải đọc dòng đầu của `{Failure}`. Dấu hiệu luật này cần xét lại: dựng hạ tầng thu log mà việc gom lỗi dựa vào trường ngoại lệ có cấu trúc. Lúc đó hướng đúng là thi công bộ lọc ở tầng cấu hình log (luật B11) rồi mới nới vế 1 — không phải thêm miễn trừ.

---

## 6. Nên log cái gì

| Sự kiện | Mức | Vì sao |
| --- | --- | --- |
| Ứng dụng khởi động: phiên bản, môi trường, migration đã áp | Information | Dòng đầu tiên mọi cuộc điều tra cần |
| Đăng nhập thành công / thất bại | Information | Điều tra bảo mật |
| Khoá tài khoản | Warning | Có thể là tấn công |
| Thay đổi phân quyền | Information | Ai đổi, đổi gì |
| Mỗi request — **đúng một dòng**, ghi khi request kết thúc: phương thức, đường dẫn không kèm query string (§5), mã trạng thái, thời gian xử lý, mã lần gọi | Information | Đếm và đo được từng request mà không đọc hai dòng rời. Middleware của Core ghi, đứng sau middleware mã lần gọi và trước bộ xử lý ngoại lệ — nên thấy cả mã 500 do bộ xử lý ngoại lệ dựng. Thay cho hai dòng request của framework, vốn tắt vì `Microsoft.AspNetCore` ở Warning (§4.1). **Request tới `/health/*` không ghi dòng** — bộ điều phối và cân bằng tải thăm dò liên tục, mỗi lần một dòng thì ngập log (người dùng chốt 2026-10-06, [nhật ký quyết định](../../adr/nhat-ky-quyet-dinh.md)). ✅ Đối chiếu 2026-10-06: `src/BE/Core/CoreAndSkill.Core.Web/Middleware/RequestLogMiddleware.cs`, chuỗi `responded {StatusCode}` và chuỗi `StartsWithSegments(HealthPaths.Root)`; tiền tố ở `HealthPaths.cs` dưới `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/`, chuỗi `Root = "/health"`; thứ tự nối ở `CoreApplicationBuilderExtensions.cs` cùng thư mục, chuỗi `app.UseMiddleware<RequestLogMiddleware>()`. Test `HealthCheckRequests_WriteNoLine_WhileOrdinaryRequestsStillWriteOne` ở `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Web/RequestLogMiddlewareTests.cs` — chưa chạy trong lượt đối chiếu này |
| Lỗi nghiệp vụ | Information | Kèm mã lỗi, để đếm được |
| Ngoại lệ chưa xử lý — lọt tới một `catch` bắt tất cả của request, việc nền hay nhịp định kỳ | Error | Kèm đầy đủ ngăn xếp, dựng bằng `FailureText.DescribeWithStack` — thông điệp của mọi tầng đã lọc (§5, luật B28) |
| Sự kiện outbox thất bại quá N lần | Error | Cần người can thiệp |
| Job nền: bắt đầu, kết thúc, số bản ghi xử lý | Information | Không có thì job nền là vùng tối. `{JobId}` là `core.job.id`; hàng đợi việc nền ghi mã nội bộ của nó dưới `{QueueItemId}`, và nơi đưa việc vào hàng đợi ghi một dòng mang cả hai (§2.1). ✅ Đối chiếu 2026-10-04: `src/BE/Core/CoreAndSkill.Core.Infrastructure/Jobs/BackgroundJobQueueHostedService.cs`, chuỗi `{QueueItemId}`; dòng nối ở [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) §10.2 |

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
- Readiness trả **`Degraded`** (không phải thất bại) khi Outbox có bản ghi `dead` hoặc bản ghi chưa phát cũ nhất quá ngưỡng — ngưỡng và định nghĩa `dead`: [`12-notifications.md`](12-notifications.md) §2.5.

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
| Log có cấu trúc — JSON ra đầu ra chuẩn ngoài Development, formatter có sẵn của .NET | ✅ có (đối chiếu 2026-10-06) | §2.3 — neo cuối mục; chưa đo dòng JSON thật ra console. Serilog bị loại 2026-10-06 — [`../../adr/nhat-ky-quyet-dinh.md`](../../adr/nhat-ky-quyet-dinh.md) |
| Mức log máy dev và bản cài thật: Information, `Microsoft.AspNetCore` và `Microsoft.EntityFrameworkCore` ở Warning | ✅ có (đối chiếu 2026-10-06) | §4.1 |
| Một dòng log mỗi request, do middleware của Core ghi; `/health/*` không ghi | ✅ có (đối chiếu 2026-10-06) | §6 |
| Mã lần gọi trong phạm vi log | ✅ sẽ có | Sinh ở middleware đầu chuỗi |
| Mã lần gọi trong envelope lỗi | ✅ sẽ có | Hình dạng envelope ở [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) |
| Bộ lọc trường nhạy cảm + test kiểm bộ lọc, **đường `client-errors`** | ✅ có (đối chiếu 2026-09-20) | Bộ lọc không có test là bộ lọc không tin được. Luật **S13** ở [`../../RULES.md`](../../RULES.md) §6 |
| Log lỗi giữ ngăn xếp, thông điệp mọi tầng qua bộ lọc; không trao đối tượng ngoại lệ cho logger | ✅ có (đối chiếu 2026-10-01) | §5 mục *Ghi một ngoại lệ vào log*. Luật **B28** ở [`../../RULES-BE.md`](../../RULES-BE.md) §6 |
| Bộ lọc gắn ở **tầng cấu hình log**, phủ mọi đường ghi | ❌ chưa | Hôm nay bộ lọc được gọi tay ở đúng một handler, nên một đường log mới bỏ qua nó mà không gì báo. Luật **B11** ở [`../../RULES-BE.md`](../../RULES-BE.md) |
| Liveness và readiness tách riêng | ✅ sẽ có | Liveness **không** chạm DB |
| Readiness thất bại khi còn migration chưa áp | ✅ sẽ có | Mặt còn lại của luật E8 |
| Đặt tên thuộc tính theo chuẩn OpenTelemetry | ✅ sẽ có | Chi phí gần bằng không |
| **Hạ tầng thu thập trace** | ❌ chưa | Một tiến trình thì trace phân tán không thêm thông tin. Ngưỡng ở §9 |
| **Metric xuất ra hệ ngoài** | ❌ chưa ở v1 | Khai chỉ số trong code trước; đấu nối khi có nơi nhận |
| **Cảnh báo tự động** | ❌ chưa | Cần có nơi nhận metric trước. Khi làm, ưu tiên hai chỉ số: tỉ lệ 5xx và tuổi bản ghi outbox cũ nhất |
| **Ghi log ra file trên máy chủ** | ❌ không làm | Log biến mất cùng máy chủ, và không gộp được khi chạy nhiều instance |
