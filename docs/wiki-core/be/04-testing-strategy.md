---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 04. Chiến lược kiểm thử — test là một phần của kiến trúc

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`, nên chưa có test nào tồn tại. File này mô tả bộ test mà giai đoạn 2 phải dựng.
>
> Danh sách luật và cột "ép bằng gì" là [`../../RULES.md`](../../RULES.md). File này giải thích **vì sao từng loại test tồn tại** và **cách để bộ test không tự lừa mình**.

---

## 1. Kim tự tháp — bốn tầng, mỗi tầng một mục đích

| Tầng | Trả lời câu hỏi | Chạm gì | Tỉ lệ mong muốn | Thời gian một lượt |
| --- | --- | --- | --- | --- |
| **Unit** | Một quy tắc nghiệp vụ có đúng không | Chỉ đối tượng đang test | Nhiều nhất | Mili giây |
| **Architecture** | Ranh giới có bị vi phạm không | Đồ thị assembly, mô hình EF, mã nguồn | Ít, nhưng phủ **mọi** luật ở [`../../RULES.md`](../../RULES.md) | Giây |
| **Integration** | Ghép lại có chạy không | DB thật, pipeline thật | Vừa | Chục giây |
| **E2E** | Người dùng làm được việc không | Cả hệ, qua trình duyệt | Rất ít | Phút |

**Tỉ lệ là hệ quả, không phải mục tiêu.** Đừng đếm test rồi chỉnh cho ra hình kim tự tháp. Cách đúng để đạt hình dạng đó là viết mỗi loại test cho đúng câu hỏi của nó.

### 1.1 Phạm vi từng tầng — ranh giới hay bị nhoè

| Tầng | Test cái này | KHÔNG test cái này |
| --- | --- | --- |
| Unit | Luật trong entity, hàm tính toán, validator, ánh xạ | Handler cần DB. Đó là integration |
| Architecture | Luật kiến trúc và quy ước đặt tên | Hành vi. ArchTest không biết code chạy đúng hay sai |
| Integration | Một command từ đầu tới cuối, qua pipeline, ghi DB thật, đọc lại | Giao diện |
| E2E | Vài luồng sống còn: đăng nhập, tạo bản ghi, phân quyền | Mọi màn hình. E2E phủ rộng là bộ test không ai dám sửa |

**Cạm bẫy hay gặp:** viết integration test cho những thứ unit test đã đủ, vì integration test "chắc chắn hơn". Kết quả là bộ test chậm gấp nhiều lần mà không phát hiện thêm lỗi nào — và bộ test chậm thì người ta ngừng chạy.

---

## 2. ArchTest — mục quan trọng nhất của file này

### 2.1 Vì sao ArchTest đáng giá hơn mọi loại test khác ở một Core

Một Core sống nhiều năm và qua nhiều người. Luật kiến trúc viết trong tài liệu sẽ bị vi phạm — không phải vì ai đó cố ý, mà vì người mới không biết luật tồn tại, và vì lúc gấp thì đường ngắn nhất luôn hấp dẫn.

ArchTest biến luật thành thứ **hỏng build**. Đó là khác biệt giữa một quy ước và một ràng buộc.

**Một điểm cần hiểu rõ:** ArchTest không kiểm code chạy đúng. Nó kiểm code có **hình dạng** đúng. Hai việc khác nhau, và cả hai đều cần.

### 2.2 Danh sách luật cần canh
> 📖 **Tên từng ArchTest nằm ở cột "Ép bằng gì" của [`../../RULES.md`](../../RULES.md) — nguồn duy nhất.** Bảng dưới nói *mỗi luật cần canh điều gì*; mở `RULES.md` để lấy **tên** khi thi công.

Chép tên test sang đây là dựng nguồn thứ hai: đổi tên một test thì sửa một chỗ không chạm chỗ kia, và tên test chính là thứ người sau `grep` để nối luật với cổng.
**Kiến trúc và tầng** (RULES §3):

| Luật | Canh điều gì |
| --- | --- |
| **A1** | Domain không kéo theo bất kỳ package nào |
| **A2** | Application không thấy EF Core, ASP.NET Core |
| **A3** | Core không biết module nào tồn tại |
| **A4** | Module chỉ nói chuyện qua `Core.Contracts` |
| **A5** | Core không biết **tên** nghiệp vụ, kể cả dưới dạng chuỗi |
| **A6** | Không có project nằm ngoài solution — thứ không ai build và không ai test |
| **A7** | Host chỉ là điểm lắp ráp |
| **A8** | Cấu hình thiếu thì app không khởi động được |
| **A9** | Middleware khai rồi mà quên nối — hỏng im lặng |
| **A10** | Đăng ký hai lần thì mỗi request chạy hai lượt |
| **A11** | Validator quên đăng ký thì nó **không chạy**, và không ai biết |
| **A12** | Không ai ngoài các nơi đã khai tự chọn đơn vị và danh tính cho mã đang chạy |

**Domain và dữ liệu** (RULES §4):

| Luật | Canh điều gì |
| --- | --- |
| **E1** | Trạng thái entity chỉ đổi qua hành vi |
| **E2** | Định danh không đổi sau khi tạo |
| **E3** | Không có entity nào lọt lưới lọc xoá mềm |
| **E4** | Entity Core ở schema `core`, entity module ở schema của module |
| **E5** | Không có sợi dây trói vĩnh viễn giữa hai schema |
| **E6** | Migration nằm ở project sở hữu schema |
| **E7** | Interceptor khai rồi mà quên nối |

Riêng `Startup_Fails_When_PendingMigrationsExist` là **integration test**, không phải ArchTest — nó cần dựng app thật.

**Lỗi và envelope** (RULES §5):

| Luật | Canh điều gì |
| --- | --- |
| **R1** | Lỗi nghiệp vụ đi bằng `Result`, không bằng exception |
| **R2** | Mã lỗi không mọc rải rác |
| **R3** | Mã lỗi đúng khuôn và không trùng trong toàn hệ |
| **R4** | Không có loại lỗi nào rơi vào khoảng trống ánh xạ |
| **R5** | Xem [`../../audit/2026-09-05-reflection-envelope.md`](../../audit/2026-09-05-reflection-envelope.md) |
| **R6** | Envelope dựng tay vẫn phải có mã |
| **R7** | Tham số theo tên — xem [`16-i18n-va-ma-loi.md`](16-i18n-va-ma-loi.md) |
| **R8** | Câu chữ không nằm ở BE |

**Bảo mật và phân quyền** (RULES §6):

| Luật | Canh điều gì |
| --- | --- |
| **S1** | Không có hằng số vai trò trong Core |
| **S2** | Kiểm quyền, không kiểm tên vai trò |
| **S3** | Không có controller đứng ngoài envelope và quy ước chung |
| **S4** | Mỗi endpoint công khai là một quyết định, không phải một lần quên |
| **S5** | Seam Identity không thủng |

Luật S6 (không có secret trong source) được canh bằng công cụ quét bí mật trong CI, không phải ArchTest.

**Cách ly giữa các đơn vị** (RULES §9):

| Luật | Canh điều gì |
| --- | --- |
| **M1** | không entity nào mang `ITenantScoped` mà lọt lưới bộ lọc theo đơn vị |
| **M2** | không DTO nào, không tham số action nào để model binder gán `TenantId` từ request |
| **M3** | một unique thiếu `TenantId` biến giá trị của đơn vị A thành ràng buộc lên đơn vị B |
| **M4** | entity mới quên khai `ITenantScoped` mà không có tên trong danh sách miễn trừ |
| **M5** | mỗi lần bỏ bộ lọc là một quyết định phải giải trình, không phải một lần gõ nhanh |
| **M6** | SQL thô không đi qua query filter, nên nó phải tự mang điều kiện |
| **M8** | Dữ liệu đơn vị không bao giờ được ghi khi chưa biết đơn vị — ghi vào `Guid.Empty` là dữ liệu vô hình với mọi người |

Riêng `CrossTenantAccess_Returns_NotFound` (M7) là **integration test**, không phải ArchTest — cùng lý do với `Startup_Fails_When_PendingMigrationsExist` (E8): nó cần một app thật, hai đơn vị thật, và một lượt gọi HTTP thật. Không có cách nào đọc hình dạng code mà biết được endpoint trả 404 hay 403.

> 🚨 **Nhóm này từng vắng mặt hoàn toàn khỏi mục kiểm kê trên, và đó là lỗ nguy hiểm nhất trong cả file.** Đây là file duy nhất mà `test-engineer` được định tuyến tới; một luật không có tên ở đây là một luật không ai viết test. Mà M1–M7 canh đúng thứ rủi ro nhất của toàn hệ: **rò dữ liệu giữa hai đơn vị** — hỏng mà không có lỗi, không có ngoại lệ, chỉ là truy vấn trả về nhiều hơn đáng ra được thấy ([`../../RULES.md`](../../RULES.md) §9).
>
> M7 đáng một câu riêng: nó là ranh giới giữa *"không tìm thấy"* và *"cấm truy cập"*. Trả **cấm truy cập** là xác nhận bản ghi **tồn tại**, nên người ngoài dò được danh sách mã hồ sơ của đơn vị khác chỉ bằng cách thử. Test phải khẳng định đúng con số **404**, và nó phải chạy cho **cả hai** ca: bản ghi không tồn tại, và bản ghi tồn tại nhưng thuộc đơn vị khác — hai ca đó phải cho ra **cùng một** response, kể cả `code`.
>
> Thi công: [`17-multi-tenant.md`](17-multi-tenant.md).

### 2.3 Test seam động — chứng minh lớp ĐÃ ĐƯỢC NỐI, không chỉ ĐÃ ĐÚNG — luật T7

ArchTest và unit test trả lời câu *"logic này có đúng không"*. Chúng **không** trả lời câu *"logic này có được ai gọi không"*.

Hai câu đó hỏng độc lập nhau, và câu thứ hai hỏng im lặng hơn nhiều:

| Hỏng ở đâu | Test nào bắt được | Triệu chứng |
| --- | --- | --- |
| Validator viết sai luật | Unit test validator | Đỏ ngay |
| Validator viết đúng nhưng **không ai gọi** | **Không test nào ở trên** | Request sai định dạng đi thẳng vào handler; đây là một lỗ hổng, không phải một bug hiển thị |

Ở dự án tiền nhiệm, bản hỏng nằm đúng ở ô thứ hai — validator có unit test xanh, và pipeline không gọi nó.

**Luật:** mỗi lớp cắt ngang mạch request — pipeline behavior, middleware, filter phân quyền — phải có **một** test gửi request thật qua toàn bộ mạch và khẳng định lớp đó **đã can thiệp**. Một test cho mỗi lớp là đủ; test này chứng minh sự tồn tại của mối nối, không chứng minh logic bên trong.

Phân biệt với luật A10: A10 canh *"behavior có được đăng ký đúng một lần không"* — một khẳng định **tĩnh** trên `ServiceCollection`. T7 canh *"đăng ký đó có thật sự nằm trên đường đi của request không"*. Đăng ký đúng vào một pipeline không được dùng vẫn qua được A10.

### 2.4 Ba cách viết detector — chọn đúng cách

| Cách | Dùng khi | Rủi ro |
| --- | --- | --- |
| **Đồ thị assembly** (đọc metadata của assembly đã build) | Luật về tham chiếu, kế thừa, chữ ký | Ít nhất. Ưu tiên cách này |
| **Phân tích cú pháp mã nguồn (AST)** | Luật về hình dạng code: có ném exception không, có dùng reflection không | Trung bình. Cần hiểu đúng cú pháp |
| **Quét văn bản nguồn** | Chỉ khi hai cách trên không làm được | **Cao** — xem cảnh báo dưới |

> ⚠️ **Bài học từ dự án tiền nhiệm.** Detector cho luật "Core không chứa tên nghiệp vụ" ở đó quét **văn bản nguồn** thay vì AST. Hệ quả: code phải viết vòng để né detector — một chuỗi nội suy hoàn toàn hợp lệ vẫn bị bắt, nên người viết phải tách biến chỉ để tạo ra một chuỗi "sạch". Đuôi vẫy chó: luật được thoả mãn, code thì xấu đi.
>
> Khi buộc phải quét văn bản, detector **phải** có test chứng minh nó bỏ qua comment và định danh — xem §3.

**Công cụ cho từng cách — chốt 2026-09-10:**

| Cách | Công cụ | Ghi chú |
| --- | --- | --- |
| Đồ thị assembly | **ArchUnitNET** | Lớp chính, dùng cho phần lớn detector. Chọn nó thay vì thư viện tương đương vì API rộng hơn khi luật phức tạp dần |
| Phân tích cú pháp | **Roslyn syntax API**, gọi trong project ArchTest để quét AST, chỉ cho vài luật hình dạng code | Đắt hơn đồ thị assembly. Dành cho luật mà vi phạm gây hỏng im lặng — điển hình là cấm phản chiếu ở đường ánh xạ lỗi (luật R5, ép bằng ArchTest) |
| Quét văn bản | `grep` trong cổng | Chỉ khi hai cách trên không làm được, và luôn kèm meta-test |

> **Trước khi cam kết, viết thử ba detector KHÓ NHẤT bằng ArchUnitNET.** Ba cái đó chạy được thì phần còn lại chắc chắn chạy được; ngược lại thì đổi công cụ lúc mới có ba test rẻ hơn nhiều so với lúc đã có bốn mươi.

---

## 3. Meta-test — mọi detector phải có test kiểm chính detector đó

### 3.1 Vì sao đây là practice đắt giá nhất trong file này

Một cổng hỏng âm thầm **tệ hơn không có cổng**.

Không có cổng thì mọi người biết là không có, và họ tự cẩn thận. Có một cổng luôn xanh vì nó không kiểm gì cả thì mọi người **tin là mình được bảo vệ** và ngừng cẩn thận. Đó là trạng thái tệ nhất trong hai.

Ở dự án tiền nhiệm, một script cổng **không tồn tại trên đĩa** khiến vài mục cổng phía FE không chạy suốt một thời gian dài, trong khi tài liệu vẫn ghi bình thường và mọi lượt chạy đều báo xanh. Xem [`../../audit/2026-08-23-cong-khong-ton-tai.md`](../../audit/2026-08-23-cong-khong-ton-tai.md).

**Luật T1** ở [`../../RULES.md`](../../RULES.md) ép điều này: mọi detector trong ArchTest phải có test kiểm chính detector đó.

### 3.2 Quy ước đặt tên

```text
Detector_<TênLuật>_<HànhVi>
```

Mỗi detector cần **tối thiểu hai** meta-test:

| Meta-test | Chứng minh điều gì |
| --- | --- |
| `Detector_<Luật>_Catches_RealViolation` | Đưa vào một vi phạm thật → detector **phải** báo đỏ |
| `Detector_<Luật>_Ignores_<CaHợpLệ>` | Đưa vào một ca trông giống vi phạm nhưng hợp lệ → detector **phải** bỏ qua |

Thiếu vế thứ nhất: detector có thể luôn xanh vì nó không bao giờ tìm thấy gì. Thiếu vế thứ hai: detector báo đỏ oan, và người ta sẽ vô hiệu hoá nó — kết quả cuối cùng giống hệt việc không có cổng.

### 3.3 Ví dụ cụ thể — detector quét văn bản

Giả sử detector canh luật "Core không được chứa chuỗi literal đặt tên tầng nghiệp vụ". Bộ meta-test tối thiểu:

```csharp
[Fact] // Bắt được vi phạm thật
public void Detector_BusinessNameLiteral_Catches_RealViolation()
{
    var src = """
        public sealed class Handler
        {
            private const string Target = "DonHang";   // vi phạm thật
        }
        """;
    Assert.NotEmpty(Detector.Scan(src));
}

[Fact] // Bỏ qua comment — chữ nằm trong comment không phải chuỗi
public void Detector_BusinessNameLiteral_Ignores_Comment()
{
    var src = "// ví dụ: một module DonHang sẽ khai ở đây\npublic sealed class Handler { }";
    Assert.Empty(Detector.Scan(src));
}

[Fact] // Bỏ qua định danh — tên biến/class không phải chuỗi literal
public void Detector_BusinessNameLiteral_Ignores_Identifier()
{
    var src = "public sealed class DonHangHandler { }";
    Assert.Empty(Detector.Scan(src));
}
```

Ba test này là thứ phân biệt một detector **thật sự canh luật** với một biểu thức chính quy tình cờ chưa khớp gì.

### 3.4 Chỗ dễ bỏ sót nhất

| Loại detector | Meta-test dễ quên |
| --- | --- |
| Quét văn bản | Comment, định danh, chuỗi nội suy, chuỗi nhiều dòng |
| Kiểm đồ thị assembly | Ca **rỗng** — nếu không tìm thấy assembly nào thì test vẫn xanh. Phải có meta-test khẳng định detector tìm thấy đúng tập assembly mong đợi |
| Kiểm mô hình EF | Detector chạy trên một mô hình rỗng cũng xanh. Phải kiểm số lượng entity quét được lớn hơn 0 |
| Kiểm cấu hình khởi động | Detector chạy trên một `ServiceCollection` chưa đăng ký gì cũng xanh |

**Khuôn chung của bốn dòng trên:** detector duyệt một tập rỗng thì luôn PASS. Mọi detector duyệt tập hợp đều cần một meta-test khẳng định **tập hợp không rỗng**.

---

## 4. Integration test — PostgreSQL thật, không mock DB

### 4.1 Vì sao không dùng DB trong bộ nhớ

DB trong bộ nhớ không phải PostgreSQL. Nó khác ở đúng những chỗ integration test sinh ra để kiểm:

| Khác ở đâu | Hệ quả |
| --- | --- |
| Không có ràng buộc duy nhất thật, không có khoá ngoại thật | Test xanh, môi trường thật đỏ vì vi phạm ràng buộc |
| Không dịch được truy vấn đặc thù của provider | Truy vấn chạy trong test, ném lỗi khi chạy thật |
| Không có token đồng thời của PostgreSQL | Toàn bộ mảng chống ghi đè **không kiểm được** — xem [`06-concurrency-control.md`](06-concurrency-control.md) |
| Không có transaction và mức cô lập thật | Bug về đồng thời không bao giờ lộ ra trong test |
| Không có schema | Luật "mỗi module một schema" không kiểm được |

Với Core này — dùng PostgreSQL, có schema riêng theo module, có token đồng thời — DB trong bộ nhớ kiểm được rất ít thứ đáng kiểm.

**Luật T2** ở [`../../RULES.md`](../../RULES.md): integration test chạy trên PostgreSQL thật.

### 4.2 Dùng lại container — chỗ quyết định thời gian chạy

Khởi động một container PostgreSQL tốn vài giây. Nếu mỗi test class khởi động một container, bộ test sẽ chậm tới mức không ai chạy trước khi đẩy code.

| Chiến lược | Container | Cô lập dữ liệu bằng |
| --- | --- | --- |
| ❌ Container mỗi test | Rất chậm | — |
| ❌ Container mỗi class | Chậm | — |
| ✅ **Một container cho cả lượt chạy** | Khởi động một lần | Xem §4.3 |

Cách làm: một fixture ở phạm vi toàn bộ assembly test khởi động container, áp schema một lần, rồi mọi test class dùng chung.

### 4.3 Cô lập dữ liệu giữa các test — ba cách

| Cách | Ưu | Nhược |
| --- | --- | --- |
| **Transaction rồi rollback** | Nhanh nhất, sạch tuyệt đối | Không dùng được khi chính code đang test mở transaction — mà pipeline Transaction thì luôn mở |
| **Xoá dữ liệu giữa các test** | Đơn giản, hợp với mọi ca | Phải biết thứ tự xoá theo khoá ngoại; chậm dần khi số bảng tăng |
| **Mỗi test một schema hoặc một DB** | Cô lập tuyệt đối, chạy song song được | Tốn thời gian dựng schema cho mỗi test |

Với Core có pipeline Transaction, cách thứ nhất bị loại ngay. Khuyến nghị: **cách thứ hai** ở v1, chuyển sang cách thứ ba khi cần chạy song song.

**Bẫy:** để test phụ thuộc dữ liệu do test khác tạo ra. Nó xanh khi chạy cả bộ, đỏ khi chạy một mình, và người ta sẽ "sửa" bằng cách ép thứ tự chạy. Từ đó bộ test không còn nói lên điều gì.

### 4.4 Schema cho test lấy từ đâu

Lấy từ **chính migration của repo**, không dựng bằng cơ chế tạo schema tự động từ mô hình.

Lý do: nếu test dựng schema từ mô hình, thì mọi sai lệch giữa mô hình và migration đều **vô hình** trong test — mà đó chính là loại lỗi làm hỏng lần triển khai. Chạy migration trong test là cách rẻ nhất để canh migration.

Xem [`../../database/migration-policy.md`](../../database/migration-policy.md).

---

## 5. Coverage — ngưỡng và vì sao không đặt 100%

**Luật T3** ở [`../../RULES.md`](../../RULES.md) đặt ngưỡng cho `Core.Domain` và `Core.Application` cao hơn ngưỡng cho module. Lý do: Core được dùng lại ở mọi dự án, nên một lỗi ở Core nhân lên theo số dự án.

**Công cụ ép:** `coverlet.msbuild`, khai `Threshold` theo **dòng và nhánh** — dưới ngưỡng thì `dotnet test` thất bại. Con số ngưỡng chỉ nằm ở luật T3, không chép sang cấu hình tài liệu nào khác.

### Vì sao không 100%

| Lý do | Giải thích |
| --- | --- |
| **Tạo test rác** | 100% buộc phải viết test cho getter, cho ánh xạ tầm thường, cho nhánh không bao giờ xảy ra. Những test đó không bắt được lỗi nào, nhưng vẫn phải sửa mỗi lần refactor |
| **Coverage đo dòng chạy qua, không đo điều được khẳng định** | Một test gọi hàm rồi không assert gì vẫn cho 100% coverage |
| **Nó dời trọng tâm** | Khi coverage là mục tiêu, người ta tối ưu coverage chứ không tối ưu độ tin cậy |

**Cách dùng coverage đúng:** dùng nó để **tìm vùng trống**, không dùng nó làm điểm số. Một sụt giảm đột ngột đáng xem; con số tuyệt đối thì ít nói lên điều gì.

Vùng phải phủ dù coverage nói gì:

- Mọi nhánh lỗi nghiệp vụ — đây là nhánh ít được chạy thủ công nhất, nên là nơi lỗi sống lâu nhất.
- Mọi ánh xạ `Result` → HTTP.
- Mọi validator.
- Mọi detector (xem §3).

---

## 6. Đặt tên test và cấu trúc

### 6.1 Tên nói lên luật, không nói lên hàm được gọi

```text
<ĐốiTượng>_<ĐiềuKiện>_<KếtQuảMongĐợi>
```

| Tên tốt | Tên kém |
| --- | --- |
| `CreateUser_WhenEmailAlreadyExists_ReturnsConflictError` | `TestCreateUser1` |
| `Startup_Fails_When_PendingMigrationsExist` | `MigrationTest` |
| `Detector_RoleConstant_Catches_RealViolation` | `RoleTest` |

Phép thử: đọc **riêng tên test** trong báo cáo lỗi của CI, có biết luật nào vừa vỡ không? Không biết thì tên chưa đạt.

### 6.2 Arrange / Act / Assert

Ba khối, tách bằng dòng trống, không trộn:

```csharp
[Fact]
public async Task ChangePassword_WhenCurrentPasswordWrong_ReturnsChangePasswordFailed_WithPasswordMismatchOnCurrentPassword()
{
    // Arrange
    var user = await Fixture.CreateUserAsync(password: "dung-mat-khau");

    // Act
    var result = await Sut.ChangePasswordAsync(user.Id, "sai-mat-khau", "Moi-mat-khau-1", clearMustChangePassword: false, CancellationToken.None);

    // Assert — hợp đồng ở contracts/auth.md §6: mã gốc + lý do ở fieldErrors, ô mật khẩu hiện tại
    result.IsFailure.ShouldBeTrue();
    result.Error!.Code.ShouldBe(AuthErrors.ChangePasswordFailed.Code);
    result.Error.FieldErrors["CurrentPassword"].ShouldContain(f => f.Code == AuthErrors.PasswordMismatch.Code);
}
```

Hai quy tắc kèm theo:

- **Một Act cho mỗi test.** Hai Act là hai test bị ghép, và khi đỏ thì không biết cái nào hỏng.
- **Assert vào mã lỗi, không vào câu chữ.** Câu chữ không nằm ở BE (luật R8), nên assert vào chuỗi tiếng Việt là assert vào thứ không tồn tại. Mã lấy từ catalog (`XxxErrors.Yyy`, [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) §7.1), không gõ literal trong test.

### 6.3 Khẳng định ĐÚNG con số, không khẳng định "khác 200" — luật T5

> Một test nhánh lỗi khẳng định `!IsSuccessStatusCode` là một test **xanh vì lý do sai**.

Chuỗi lập luận, đọc theo thứ tự:

1. Ca đúng trả **400**. Ca hỏng trả **500**.
2. Cả hai đều "khác 200".
3. Nên test đòi `!IsSuccessStatusCode` **xanh nguyên trong lúc bug còn nguyên**.

Đây không phải giả định. Ở dự án tiền nhiệm, hai bộ test nhánh lỗi mang đúng khuôn này, và chú thích tại chỗ ghi rõ rằng bản hỏng cũ trả 500 chứ không phải 400 — tức là test đã chạy qua bug mà không thấy gì.

| Khẳng định | Đánh giá |
| --- | --- |
| `Assert.False(response.IsSuccessStatusCode)` | 🛑 Không phân biệt được "validate bắt được" với "server sập" |
| `Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode)` | ✅ |
| `Assert.Equal(HttpStatusCode.BadRequest, …)` **+** khẳng định mã lỗi trong envelope | ✅ chặt nhất — mã trạng thái đúng vẫn có thể đi kèm mã lỗi sai |

**Cách kiểm chính test này có chặt không:** tạm gỡ luật khỏi validator rồi chạy lại. Test phải chuyển đỏ kèm thông điệp *"Expected 400, Actual 500"*. Nếu nó đỏ vì một lý do khác, hoặc vẫn xanh, thì assertion chưa đủ chặt. Đây là cùng một phép thử mà [§3](#3-meta-test--mọi-detector-phải-có-test-kiểm-chính-detector-đó) áp cho detector, chỉ khác chỗ áp.

### 6.4 Dữ liệu test

Dùng builder có giá trị mặc định hợp lệ, test chỉ khai **những gì liên quan tới luật đang kiểm**. Rải `new Entity(...)` với đủ tham số ở mọi test làm mỗi lần thêm một trường bắt buộc là sửa hàng loạt test — và người ta sẽ sửa cho hết đỏ, không sửa cho đúng.

---

## 7. Công cụ — chốt 2026-09-10

| Vai | Công cụ | Vì sao |
| --- | --- | --- |
| Bộ chạy test | **xUnit** | Mặc định của hệ sinh thái .NET hiện nay; `ArchUnitNET` và `Testcontainers` đều chạy trên nó không cần lớp đệm |
| Khẳng định | **Shouldly** | Câu khẳng định đọc xuôi và **giấy phép mở**. Xem cảnh báo dưới |
| Giả lập | **NSubstitute** | Cú pháp gọn hơn hẳn khi giả lập interface, và interface là thứ duy nhất được phép giả lập ở đây (luật T8) |
| Kiến trúc | **ArchUnitNET** | §2.4 |
| Database thật | **Testcontainers** | §4 |

> ⚠️ **Kiểm giấy phép trước khi thêm một thư viện test.** Một thư viện khẳng định phổ biến trong hệ .NET đã đổi sang giấy phép **thu phí cho dùng thương mại** kể từ một phiên bản chính; nâng cấp qua mốc đó biến một phụ thuộc miễn phí thành một khoản phải trả, và điều đó **không** hiện ra trong bất kỳ cổng nào. Đây là lý do chọn Shouldly, không phải vì cú pháp đẹp hơn.

### 7.1 Luật T8 — mock chỉ ở biên hạ tầng

> **Chỉ giả lập thứ nằm ở BIÊN: cổng ra hạ tầng và hệ ngoài. Không bao giờ giả lập kiểu của chính miền nghiệp vụ.**

| Được giả lập | Không được giả lập |
| --- | --- |
| Seam ra hạ tầng: lưu trữ tệp, gửi thư, đồng hồ, sinh khoá | Entity, value object, `Result<T>` |
| Hệ ngoài | Handler, validator của chính mình |
| | `DbContext` — dùng PostgreSQL thật, §4 |

**Vì sao.** Giả lập chính miền nghiệp vụ nghĩa là test khẳng định *"code gọi đúng thứ tôi nghĩ nó gọi"*, chứ không khẳng định *"code cho ra kết quả đúng"*. Nó xanh ngay cả khi logic sai, và nó **đỏ** mỗi lần refactor dù hành vi không đổi — tức là vừa không bắt được lỗi, vừa cản việc sửa code. Đây là loại test tốn tiền bảo trì mà không mua được gì.

Hệ quả kèm theo: nếu một handler khó test vì phải giả lập quá nhiều thứ của chính mình, đó là **dấu hiệu thiết kế**, không phải lý do để thêm mock.

---

## 8. Test không ổn định

Một test lúc xanh lúc đỏ **phá giá trị của cả bộ test**, vì nó dạy mọi người rằng đỏ không có nghĩa là hỏng.

Chính sách: test không ổn định phải được **đánh dấu và có người nhận trong một khoảng thời gian đã khai**, không được "chạy lại cho qua" như thói quen. Hết khoảng đó mà chưa sửa thì xoá — một test không tin được thì không có giá trị nào để giữ.

Nguyên nhân phổ biến, theo thứ tự hay gặp: phụ thuộc thời gian thật, phụ thuộc thứ tự chạy, dùng chung dữ liệu giữa các test, và phụ thuộc thứ tự trả về của một truy vấn không có mệnh đề sắp xếp.

---

---

## 9. Danh mục ca biên — danh sách phải rà

Đây là **danh sách phải rà, không phải danh sách phải viết đủ**. Với mỗi mục, hỏi *"ở chỗ này nó có nghĩa gì không"* — có thì viết test, không thì bỏ qua.

Danh mục tồn tại vì một lý do cụ thể: **người viết code không nhìn thấy ca mình đã bỏ sót.** Test do chính tác giả viết có xu hướng kiểm đúng những gì tác giả đã nghĩ tới lúc viết code — tức là kiểm lại chính giả định đã sinh ra lỗi. Một danh sách bên ngoài phá được vòng lặp đó.

| Nhóm | Cân nhắc gì |
| --- | --- |
| Rỗng và null | Chuỗi rỗng, danh sách rỗng, giá trị null, trường tuỳ chọn không truyền |
| Một phần tử | Danh sách đúng một phần tử — chỗ hay lộ lỗi phân trang và lỗi nối chuỗi |
| Biên trên và biên dưới | Đúng ngưỡng, dưới ngưỡng một đơn vị, trên ngưỡng một đơn vị |
| Trùng lặp | Gửi hai lần cùng dữ liệu, tạo trùng khoá, thêm trùng phần tử vào tập |
| Unicode và dấu tiếng Việt | Sắp xếp, so sánh, tìm kiếm, cắt chuỗi theo độ dài, chữ hoa chữ thường có dấu |
| Số âm và số 0 | Số lượng bằng 0, số âm ở nơi chỉ chấp nhận dương, phép chia cho 0 |
| Thời gian và múi giờ | Biên ngày, giờ lưu và giờ hiển thị, khoảng thời gian ngược, hạn hiệu lực |
| Đồng thời | Hai người sửa cùng bản ghi, hai yêu cầu cùng tạo một thứ, khoá lạc quan |
| Quyền thiếu | Không đăng nhập, đăng nhập nhưng thiếu quyền, có quyền xem nhưng không có quyền sửa |
| Dữ liệu đã xoá mềm | Truy vấn có lọt bản ghi đã xoá không, tạo lại thứ đã xoá, tham chiếu tới bản ghi đã xoá |
| Transaction rollback | Lỗi giữa chừng có để lại dữ liệu dở dang không, hiệu ứng phụ ngoài DB có bị bỏ lại không |

**Một ca biên đã cân nhắc và cố ý bỏ qua vẫn phải nói ra trong báo cáo** — kèm lý do. Im lặng bỏ qua và cân nhắc rồi bỏ qua trông giống hệt nhau từ bên ngoài, nhưng chỉ một trong hai là công việc.

### Ba nhóm đáng chú ý riêng ở Core này

- **Dữ liệu đã xoá mềm** — mọi entity đều mang cờ xoá mềm và có bộ lọc truy vấn tự động, nên lỗi ở đây là lỗi *im lặng*: truy vấn trả ít hơn dự kiến mà không báo gì. Đặc biệt kiểm ca **tạo lại một bản ghi đã xoá mềm** khi có ràng buộc duy nhất — xem `../../database/schema-core.md`.
- **Đồng thời** — cơ chế khoá lạc quan chỉ chạy khi được cấu hình đúng cho provider, và nó hỏng *im lặng* nếu cấu hình sai. Xem `06-concurrency-control.md`.
- **Transaction rollback** — pipeline bọc command trong transaction, nhưng hiệu ứng phụ ngoài cơ sở dữ liệu (gửi mail, ghi file) **không** được rollback theo. Ca biên phải kiểm là: lệnh thất bại sau khi hiệu ứng phụ đã xảy ra.

## 10. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Unit test cho Domain và Application | ✅ sẽ có | |
| ArchTest phủ mọi luật §3–§6 **và §9** của [`../../RULES.md`](../../RULES.md) | ✅ sẽ có | Viết **song song** với code, không để cuối. §9 là nhóm multi-tenant — nó từng vắng mặt khỏi mục kiểm kê của chính file này (xem 🚨 ở §2.2), nên nó được nêu riêng ở đây |
| Meta-test `Detector_*` | ✅ sẽ có | Luật T1 — không có ngoại lệ |
| Integration test trên PostgreSQL thật | ✅ sẽ có | Một container cho cả lượt chạy |
| Cổng coverage trong CI | ✅ sẽ có | `coverlet.msbuild`, ngưỡng dòng và nhánh (§5). Con số ở [`../../RULES.md`](../../RULES.md) T3 |
| Build không warning | ✅ sẽ có | Luật T4 |
| **E2E** | ❌ chưa ở v1 | Thêm khi có luồng nghiệp vụ đủ ổn định để không phải sửa test mỗi tuần. Trước đó, E2E là chi phí bảo trì thuần |
| **Test hiệu năng tự động** | ❌ chưa | Đo thủ công trước, xem [`11-performance-caching.md`](11-performance-caching.md) |
| **Test đột biến** | ❌ chưa | Đáng cân nhắc khi bộ test đã ổn định và có nghi ngờ về chất lượng assert |

Phía FE: [`../fe/06-testing-strategy.md`](../fe/06-testing-strategy.md).
