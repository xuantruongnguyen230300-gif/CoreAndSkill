---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0025 — Dữ liệu module lưu bằng DbContext riêng không mang Identity, nhưng commit trong MỘT transaction trên MỘT kết nối cùng outbox và nhật ký của Core

> **Trạng thái:** Đã chấp nhận (2026-09-14)

## Bối cảnh

Ba mô tả đã chốt, đặt cạnh nhau, không thi công được một command của module:

| # | Tài liệu nói (lúc ghi ADR) | Vì sao nó hỏng khi có module |
| --- | --- | --- |
| 1 | [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §5 (dòng 169): *mỗi module một `DbContext` riêng — kế thừa base của Core* | Base duy nhất đã khai là `CoreDbContext`, và nó kế thừa `IdentityDbContext<AppUser, AppRole, Guid>` ([`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §5.1, dòng 357–360). Kế thừa nó thì model của module mang theo bảng Identity; snapshot thuộc về từng `DbContext` ([`../database/migration-policy.md`](../database/migration-policy.md) §1.2, dòng 59–67), nên migration đầu tiên của module sinh lệnh tạo bảng Identity — đúng triệu chứng *"cấu hình `DbContext` đang gom cả entity của bên kia"* mà cùng file đó (dòng 187–188) liệt kê là sai. Module Infrastructure còn phải thấy `AppUser`, trái luật S5 |
| 2 | [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §4 (dòng 348): `UnitOfWork` nhận **một** `CoreDbContext` | Transaction chỉ phủ `CoreDbContext`. Handler module ghi vào `DbContext` của module; outbox ([`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §10.3, dòng 999–1015) và nhật ký kiểm toán ở schema `core` phải *"cùng transaction"* với dữ liệu nghiệp vụ. Hai `DbContext` trên hai kết nối là hai transaction — không có "cùng" nào cả |
| 3 | [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §5.1 (dòng 398–405): quét handler và validator theo assembly chứa `ICommandBase` | Chỉ assembly `Core.Application` được quét; handler và validator của module không được đăng ký. Module lại bị cấm tự đăng ký behavior (cùng file dòng 413–416; [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §7 dòng 216). Đường gần nhất trong tài liệu để module đăng ký handler là **chép khối đăng ký** — và khối đó mang theo hai `AddOpenBehavior`, đúng ca luật A10 bắt |

Ràng buộc có thật: mọi schema nằm trong **một** database PostgreSQL, không khoá ngoại xuyên schema ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §5); mỗi bên sở hữu chuỗi migration của mình ([`0008-core-so-huu-migration.md`](0008-core-so-huu-migration.md)); và [`0001-modular-monolith.md`](0001-modular-monolith.md) đã hứa *"một transaction thật"* cho thao tác chạm nhiều aggregate.

## Quyết định

> 1. **Bộ lọc đơn vị và bộ lọc xoá mềm dùng chung được khai qua một base hoặc extension KHÔNG kế thừa `IdentityDbContext`.** `CoreDbContext` giữ `IdentityDbContext` và dùng cùng cơ chế lọc đó; `DbContext` của module dùng nó mà không mang Identity.
> 2. **`IUnitOfWork` điều phối mọi `DbContext` đã đăng ký trên MỘT `DbConnection` và MỘT transaction.** Outbox và nhật ký kiểm toán ở schema `core` commit cùng dữ liệu module, hoặc cùng không.
> 3. **Mỗi `AddXModule()` tự đăng ký assembly của module đó với Core; `AddCore`/`AddCoreApplication` gom mọi assembly đã đăng ký rồi quét handler và validator của Core và của chúng.** Host giữ mỗi module đúng một dòng. Pipeline behavior đăng ký **một lần**, bên trong `AddCoreApplication` (luật A10).

Tên kiểu, chữ ký, cách module đăng ký `DbContext` với đơn vị công việc: [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §4–§5 và [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §5. Cách module đăng ký assembly và cách Core gom: [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3, §5.1. ADR này không chép.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — `DbContext` của module kế thừa `CoreDbContext`

**Được:** hai bộ lọc có sẵn, không viết thêm gì; đúng câu chữ đang có ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §5.

**Vì sao loại:** ba lý do ở dòng 1 của bảng *Bối cảnh* — bảng Identity lọt vào model và migration của module, quyền sở hữu schema của ADR-0008 vỡ, và kiểu Identity rò ra ngoài `Core.Infrastructure` (S5). Cả ba đều là lỗi cấu trúc, không vá được bằng cấu hình.

### Phương án B — Một `DbContext` cho mọi schema: module nạp cấu hình entity vào `CoreDbContext`

**Được:** một kết nối, một transaction, một change tracker — bài toán điều phối biến mất.

**Vì sao loại:** Core phải biết assembly của module lúc dựng model — dạng R1 không cần ProjectReference cũng vi phạm. Một snapshot cho mọi schema nghĩa là migration của module nằm trong chuỗi của Core — lật ADR-0008. Và đây chính là mô hình dự án tiền nhiệm đã giữ ([`../database/migration-policy.md`](../database/migration-policy.md) §1.2, dòng 73), với đúng cái giá mà ADR-0008 ghi.

### Phương án C — Mỗi `DbContext` một transaction, commit lần lượt

**Được:** không cần kết nối chung; mỗi context tự quản như mẫu hiện có.

**Vì sao loại:** commit thứ nhất xong, commit thứ hai hỏng → dữ liệu ghi dở giữa hai schema — đúng lớp lỗi [`0006-pipeline-behavior.md`](0006-pipeline-behavior.md) sinh ra để loại. Với outbox thì hỏng theo hai chiều, cả hai đều im lặng: sự kiện phát cho thay đổi không tồn tại, hoặc thay đổi tồn tại mà sự kiện mất.

### Phương án D — Giao dịch môi trường (`TransactionScope`) bao nhiều kết nối

**Được:** không đổi chữ ký `UnitOfWork`; mỗi context giữ kết nối riêng.

**Vì sao loại:** hai kết nối cùng tham gia một giao dịch môi trường thì leo thang thành giao dịch phân tán (commit hai pha) — đúng loại chi phí điều phối mà ADR-0001 chọn Modular Monolith để khỏi phải trả. Còn nếu giữ mọi context trên **một** kết nối để khỏi leo thang, thì đó chính là phương án đã chọn, chỉ thêm một tầng giao dịch môi trường không cần thiết.

### Phương án E — Mỗi module outbox và nhật ký riêng, trong schema của mình

**Được:** module không cần ghi vào schema `core` trong transaction của mình, nên nhu cầu giao dịch xuyên `DbContext` cho outbox biến mất.

**Vì sao loại:** mỗi module một bảng outbox, một bộ phát, một bảng nhật ký — nhân bản đúng hạ tầng Core giữ **một** bản (outbox trong schema `core`: [`../wiki-core/be/05-cross-module-consistency.md`](../wiki-core/be/05-cross-module-consistency.md), dòng 203 lúc ghi). Màn nhật ký kiểm toán phải gộp N bảng. Và vẫn không giải được ca một command ghi cả dữ liệu Core lẫn dữ liệu module.

### Phương án F — Module tự gọi đăng ký mediator cho assembly của mình

**Được:** Core không phải gom gì; mỗi module tự đủ.

**Vì sao loại:** khối đăng ký là nơi behavior được khai, và chép nó là đăng ký behavior lần hai — mỗi request chạy behavior hai lượt (A10). Tách khối đăng ký handler khỏi khối đăng ký behavior thì module vẫn phải biết đúng hàm nào của thư viện mediator được gọi mà không kéo theo behavior — một luật phải nhớ ở mọi module, mãi mãi. Quyết định 3 giữ phần *module tự khai assembly của mình* và để việc gọi thư viện mediator ở đúng một chỗ trong Core.

### Phương án G — Host truyền assembly của module vào `AddCoreApplication(params Assembly[])`

**Được:** Core không cần chỗ gom assembly; danh sách assembly hiện ra tường minh ở `Program.cs`.

**Vì sao loại:** lắp một module thành **hai** dòng ở host — `AddXModule()` và một phần tử trong danh sách assembly — trong khi host giữ nguyên tắc mỗi module đúng một dòng ([`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3). Dòng thứ hai là dòng hay quên, và quên nó thì handler của module không được đăng ký mà app vẫn khởi động bình thường.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Việc tách một module ra database riêng không còn là việc cơ học** | [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §1 (dòng 42) nói tách module thành service là cơ học vì ranh giới đã sẵn. Sau ADR này, tính nguyên tử giữa dữ liệu module và outbox của Core mất ngay khi tách database, và module phải chuyển sang outbox riêng trước. Đây là cái giá có ý thức: đổi tính nguyên tử hôm nay lấy việc tách đắt hơn về sau |
| **Một kết nối cho mọi `DbContext` trong một scope** | Không chạy truy vấn song song giữa các context trong cùng một request — một kết nối không dùng đồng thời được. Kết nối bị giữ từ lúc mở transaction tới lúc commit, cho **mọi** context |
| **Thử lại phải chạy lại toàn bộ đơn vị công việc** | Hai bẫy đã ghi ở [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §4 (dòng 384–388) nhân lên theo số context: dọn change tracker ở đầu mỗi lượt thử phải làm cho **mọi** context đã đăng ký. Sót một context thì lượt thử lại ghi instance sửa dở của lượt trước |
| **Một điểm đăng ký mới mà module phải nhớ gọi** | Module quên đăng ký `DbContext` với đơn vị công việc thì dữ liệu module ghi **ngoài** transaction hoặc không được lưu — hỏng im lặng. Lúc chốt, **chưa có luật nào mang mã** canh ca này |
| **Module phải tự đăng ký assembly trong `AddXModule()`** | Module quên đăng ký thì handler của nó không được đăng ký, và lỗi chỉ lộ **lúc gửi request**, không lúc khởi động. Lớp canh chuyển từ host sang người viết module |
| **Bước gom chỉ thấy assembly đã được đăng ký với nó** | Gom sớm hơn lời gọi `AddXModule()` nào thì assembly đó bị bỏ sót. Vì vậy bước gom niêm danh sách, và một lần ghi nhận sau đó làm host **dừng ngay lúc khởi động** — thứ tự sai không bao giờ thành một app chạy thiếu handler. Cái giá: thứ tự lời gọi ở host thành một ràng buộc cứng mà người lắp module phải biết. Thời điểm gom, cơ chế niêm và thứ tự lời gọi ở host thuộc [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3, §5.1 |
| **Core biết "có những `DbContext` khác" và "có những assembly module khác"** | Dưới dạng hai danh sách không tên — nhưng vẫn là hai seam mới phải giữ đúng trên mọi dự án hạ nguồn |
| **Repo Core chưa có module để kiểm** | Theo [`0032-module-mau-o-du-an-ha-nguon.md`](0032-module-mau-o-du-an-ha-nguon.md), phần điều phối nhiều `DbContext` trong repo Core chỉ kiểm được bằng một `DbContext` dựng riêng trong project test |

### Tích cực

- Lời hứa *"một transaction thật"* của ADR-0001 và *"outbox cùng transaction"* của khu thông báo **thành thi công được** cho cả dữ liệu module.
- Model và chuỗi migration của module **không** chứa bảng Identity; S5 và ADR-0008 giữ nguyên.
- Handler của module viết **giống hệt** handler Core: không tự mở transaction, không tự lưu.
- A10 giữ được, vì chỉ còn đúng một chỗ đăng ký behavior.
- Host giữ mỗi module đúng một dòng; lắp module không thêm bước nào ở `Program.cs`.

### Điều kiện lật quyết định

1. **Một module cần database riêng** vì lý do đo được — khối lượng, yêu cầu cách ly vật lý. Lúc đó phương án E đáng xét lại cho riêng module đó.
2. **Thời gian giữ kết nối hoặc tỉ lệ thử lại tăng đo được** do transaction bao nhiều context.
3. **Xuất hiện `DbContext` không thuộc đơn vị công việc** — ví dụ một context chỉ đọc cho báo cáo. ADR này không phân biệt context *tham gia* với context *chỉ đọc*; cần một quyết định riêng.

## Liên quan

- [`0001-modular-monolith.md`](0001-modular-monolith.md) — lời hứa một transaction thật
- [`0006-pipeline-behavior.md`](0006-pipeline-behavior.md) — `TransactionBehavior`, luật A10
- [`0008-core-so-huu-migration.md`](0008-core-so-huu-migration.md) — quyền sở hữu migration theo schema
- [`0032-module-mau-o-du-an-ha-nguon.md`](0032-module-mau-o-du-an-ha-nguon.md) — vì sao repo Core chưa có module để kiểm
- [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §4, §5 — hợp đồng đơn vị công việc và đăng ký
- [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §5 — bộ lọc toàn cục
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3, §5.1 — host mỏng, đăng ký theo nhóm
- [`../database/migration-policy.md`](../database/migration-policy.md) §1.2 — mỗi bên một `DbContext`
