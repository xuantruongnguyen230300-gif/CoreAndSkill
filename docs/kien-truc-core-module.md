---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Kiến trúc Core ↔ Module — ranh giới tái sử dụng cho BE và FE

> **File chủ về ranh giới.** Agent đọc file này để biết cái gì thuộc **Core** (mang đi được sang mọi dự án dựng trên nền tảng này) và cái gì thuộc **Module** (nghiệp vụ riêng của một dự án).
>
> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo đang ở giai đoạn 1, chưa có `src/`. Toàn bộ layout dưới đây là thứ `src/` phải trở thành, không phải mô tả hiện trạng.

---

## 1. Mô hình: Modular Monolith

Một solution, một process, N module. Core là **framework nội bộ**; module là nghiệp vụ lắp lên Core.

```
                    ┌─────────────────────────┐
                    │        Host (Api)        │  ← composition root duy nhất, < 50 dòng
                    └────────────┬─────────────┘
                                 │ đăng ký module
          ┌──────────────┬───────┴───────┬──────────────┐
          ▼              ▼               ▼              ▼
   ┌────────────┐ ┌────────────┐  ┌────────────┐ ┌────────────┐
   │  Module A  │ │  Module B  │  │  Module C  │ │    ...     │
   └─────┬──────┘ └─────┬──────┘  └─────┬──────┘ └─────┬──────┘
         │              │               │              │
         └──────────────┴───────┬───────┴──────────────┘
                                ▼
                    ┌─────────────────────────┐
                    │          CORE            │  ← không biết module nào tồn tại
                    │ Domain·App·Infra·Web·Ctr │
                    └─────────────────────────┘
```

### Vì sao Modular Monolith, không phải microservices

Với hệ tầm trung, mô hình này cho **ranh giới** của microservices (dễ tách sau) mà không phải trả **chi phí vận hành** của microservices: distributed transaction, tracing phân tán, N pipeline deploy, N môi trường phải đồng bộ.

Khi một module thật sự cần scale riêng, việc tách nó thành service độc lập là **cơ học** — vì ranh giới đã sẵn: schema riêng, không FK xuyên schema, giao tiếp qua `Core.Contracts`.

📖 Lý do đầy đủ và các phương án đã loại: [`adr/0001-modular-monolith.md`](adr/0001-modular-monolith.md)

---

## 2. Backend — Năm project Core — định nghĩa gốc

Bảng dưới đây là **nguồn duy nhất** của danh sách project Core và ranh giới từng project.
File thi công ([`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md)) trỏ về đây thay vì
chép lại ([`OWNERSHIP.md`](OWNERSHIP.md) §2) — hai bản của bảng này là đúng cách repo tiền
nhiệm có bốn sơ đồ đặt tên project nói ngược nhau.

| Project | Chứa | **Cấm** chứa | Tham chiếu được |
| --- | --- | --- | --- |
| `Core.Domain` | Entity gốc, Value Object, `Result<T>`, `Error`, `ErrorType`, guard clause, catalog lỗi của invariant Domain, luật nghiệp vụ thuần | **Bất kỳ package nào** (zero package reference) — không EF Core, không MediatR, không FluentValidation, không `Microsoft.Extensions.*` | — (chỉ BCL) |
| `Core.Application` | `ICommand<T>`/`IQuery<T>`, handler, validator, pipeline behavior, DTO, catalog lỗi cấp use case, **mọi seam ra ngoài** (danh sách đầy đủ: [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §1.1) | EF Core (`DbContext`, `Include`, `AsQueryable`), ASP.NET Core (`HttpContext`, `IActionResult`), `IConfiguration` trực tiếp, mọi kiểu của Identity, mọi project Infrastructure/Web | `Domain` |
| `Core.Infrastructure` | `CoreDbContext`, repository, `IEntityTypeConfiguration<T>`, interceptor, **migration schema `core`**, ASP.NET Core Identity (`AppUser`/`AppRole`), cache, gửi mail, lưu file, job nền, implementation của mọi seam khai ở Application | Controller, middleware, **bất cứ thứ gì phụ thuộc `HttpContext`** | `Domain`, `Application` |
| `Core.Web` | `ApiControllerBase`, middleware, `HttpContextCurrentUser`, `ModelBindingProblemFactory`, ánh xạ `Result` → HTTP, controller Core, `AddCore()` / `UseCore()` | Nghiệp vụ của bất kỳ dự án nào; `DbContext` gọi thẳng | `Domain`, `Application`, `Infrastructure` |
| `Core.Contracts` | Integration event, DTO dùng chung **giữa các module** | Bất cứ thứ gì phụ thuộc project Core khác — nó phải tự đứng một mình | — (chỉ BCL) |

Host là `CoreAndSkill.Api`; module là `CoreAndSkill.Modules.<X>.*`. Sơ đồ chiều phụ thuộc và
danh sách ArchTest canh từng dòng: [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md)
§1.2–§1.3.

> **Dòng `Core.Infrastructure` cấm `HttpContext` là dòng hay bị vi phạm nhất**, vì lớp đọc danh
> tính người dùng nhìn có vẻ thuộc về hạ tầng. Nó không: `HttpContextCurrentUser` sống ở
> `Core.Web`, và `Core.Infrastructure` chỉ thấy seam `ICurrentUser` / `ITenantContext`.

### 2.1 Vì sao tách 5 project chứ không gộp 1

Cám dỗ lớn là gom hết vào một `Core.dll` cho gọn. Đừng.

**Tách project là cách duy nhất để compiler ép luật layering.** `Core.Domain` không tham chiếu `Core.Infrastructure` thì code trong Domain **không thể** gọi `DbContext` — dù người viết có muốn. Đây là hàng rào chi phí bằng 0, không bao giờ hỏng, và không phụ thuộc ai nhớ luật.

Luật viết trong tài liệu thì người ta quên. Luật viết trong đồ thị tham chiếu thì compiler nhắc.

### 2.2 Vì sao có `Core.Web` — bệnh nặng nhất cần trị

Ở dự án tiền nhiệm, toàn bộ hạ tầng web nằm trong project host: `ApiControllerBase`, sáu middleware, `GlobalExceptionHandler`, `HttpContextCurrentUser`, chính sách CORS, cùng bốn controller của Core (auth, user, permission, menu).

Hệ quả đo được và các hậu quả kéo theo: [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §2.2.

`Core.Web` tồn tại để **Core tự đứng được**. Sau khi có nó, `Program.cs` của một dự án mới chỉ còn gọi `AddCore()`, `UseCore()`, rồi đăng ký module của mình.

### 2.3 Vì sao KHÔNG có `Core.Common` và `Core.Persistence`

Hai project này từng nằm trong phương án 6-project. Đã loại, có lý do:

| Project bị loại | Vì sao |
| --- | --- |
| `Core.Common` | Tên này gần như luôn biến thành ngăn kéo rác. Một project tên "Common" **không có tiêu chí nào để từ chối** thứ gì cả — và cái gì cũng có thể lý luận là "dùng chung". Guard clause và extension method thuộc về `Domain`. |
| `Core.Persistence` tách khỏi `Core.Infrastructure` | Lợi ích được viện dẫn là "đổi provider DB không đụng hạ tầng khác". Trong hệ tầm trung điều đó gần như không bao giờ xảy ra. Chi phí thì vĩnh viễn: thêm một project và thêm câu hỏi *"file này bỏ vào đâu"* cho mọi file mới. |

📖 [`adr/0002-core-5-project.md`](adr/0002-core-5-project.md)

### 2.4 Host — composition root duy nhất

Project host (`Api`) **chỉ được** chứa:

- `Program.cs` — gọi `AddCore()`, `UseCore()`, đăng ký module
- `appsettings*.json`
- Đăng ký module của dự án

**Cấm** chứa: middleware, controller, entity, handler, migration. Luật A7 ở [`RULES.md`](RULES.md) ép điều này.

Ngưỡng cụ thể để tự kiểm: **`Program.cs` dưới 50 dòng.** Vượt ngưỡng nghĩa là có thứ gì đó lẽ ra thuộc `Core.Web` đang nằm sai chỗ.

---

## 3. Ba luật ranh giới — ép bằng test, không bằng niềm tin

| # | Luật | Ép bằng |
| --- | --- | --- |
| **R1** | Core **không bao giờ** tham chiếu Module | Không có ProjectReference + ArchTest A3 |
| **R2** | Module A **không bao giờ** tham chiếu trực tiếp Module B — chỉ qua `Core.Contracts` | ArchTest A4 |
| **R3** | Tầng trong không biết tầng ngoài: Domain ⊄ Application ⊄ Infrastructure/Web | Đồ thị ProjectReference + ArchTest A1, A2 |

**R1 có một dạng vi phạm tinh vi hơn ProjectReference:** Core biết *tên* của nghiệp vụ. Một chuỗi `"DonHang"` nằm trong `Core.Application` là vi phạm R1 kể cả khi không có tham chiếu assembly nào — vì nó khiến Core không mang đi được sang dự án không có khái niệm đó. Luật A5 bắt dạng này.

> ⚠️ **Bài học khi thi công A5:** ở dự án tiền nhiệm, detector cho luật này quét **văn bản nguồn** thay vì AST. Hệ quả là code phải viết vòng để né detector — một chuỗi nội suy hợp lệ vẫn bị bắt, nên người viết phải tách biến chỉ để làm chuỗi "sạch". Đuôi vẫy chó. Khi thi công ở giai đoạn 2, **ưu tiên phân tích AST**; nếu buộc phải quét văn bản thì detector phải có test chứng minh nó bỏ qua comment và định danh (luật T1).

---

## 4. Tiêu chí: cái gì thuộc Core, cái gì thuộc Module

### 4.1 Phép thử

> **Một thứ thuộc Core khi nó có ý nghĩa với MỌI sản phẩm dựng trên nền tảng này.**

| Ví dụ | Thuộc | Vì sao |
| --- | --- | --- |
| Đăng nhập, phiên, đổi mật khẩu | Core | Mọi sản phẩm đều cần |
| Người dùng, vai trò, quyền | Core | Mọi sản phẩm đều cần |
| Menu động theo quyền | Core | Mọi sản phẩm đều cần |
| Thông báo (in-app, email) | Core | Cơ chế chung, nội dung do module cung cấp |
| Lưu file, import/export | Core | Cơ chế chung |
| Quản lý đơn hàng | Module | Chỉ có nghĩa với sản phẩm bán hàng |
| Báo cáo tuần theo tiêu chí X | Module | Nghiệp vụ riêng |

### 4.2 Ngưỡng định lượng — chống Core phình to

> **Code chỉ được vào Core khi từ HAI module trở lên cần nó.**

Một module cần thì để ở module đó. Module thứ hai cần thì mới nâng lên Core — và lúc nâng, phải gỡ sạch mọi dấu vết của module đầu tiên khỏi thiết kế.

**Vì sao ngưỡng này quan trọng:** rủi ro lớn nhất của một repo Core không phải thiếu tính năng, mà là **Core phình to** vì cái gì cũng "để đó cho tiện". Một Core chứa nghiệp vụ là một Core không mang đi đâu được — tức là nó đã thất bại ở đúng lý do nó tồn tại.

**Cơ chế chống:** mọi thay đổi chạm `Core/` phải qua agent `architect` và phải có ADR. Xem [`.claude/agents/architect.md`](../.claude/agents/architect.md).

### 4.3 Khi nào tách một module mới

Tách khi thoả **ít nhất hai** trong ba:

1. Có ranh giới nghiệp vụ rõ — người dùng nói về nó như một thứ riêng
2. Có tập bảng riêng, gần như không join sang bảng của module khác
3. Có thể thay đổi độc lập — sửa nó không buộc phải sửa module khác

Không thoả thì để chung; tách quá sớm tạo ra chi phí phối hợp mà không đổi lấy gì.

---

## 5. Ranh giới dữ liệu

| Luật | Chi tiết |
| --- | --- |
| Mỗi module một **schema PostgreSQL riêng** | Core dùng schema `core`; module dùng schema tên module |
| Mỗi module một `DbContext` riêng | Kế thừa base của Core |
| **Cấm foreign key vật lý xuyên schema** | Tham chiếu bằng ID (ví dụ một cột `Guid` giữ id người tạo), không bằng navigation property |
| Migration độc lập theo module | Core sở hữu migration schema `core`; module sở hữu migration của mình |

> **Cấm FK xuyên schema là điều kiện tiên quyết để sau này tách microservice.** Một FK xuyên schema là một sợi dây trói vĩnh viễn — không tách được mà không sửa schema, và sửa schema trên dữ liệu thật là việc đắt nhất trong vòng đời một hệ thống.

📖 Chi tiết: [`database/migration-policy.md`](database/migration-policy.md)

---

## 6. Frontend — bốn tầng

> 📖 **Sơ đồ bốn tầng FE và chiều phụ thuộc: đọc [`quy-uoc/fe-architecture.md`](quy-uoc/fe-architecture.md) §1** — đó là file chủ.

**Chiều phụ thuộc chỉ đi xuống.** `core/` là tầng đáy — nó không được import bất cứ thứ gì từ ba tầng trên.

### 6.1 Ranh giới FE được ép bằng gì

Khác với BE (compiler ép qua ProjectReference), FE **giữ cấu trúc thư mục** trong một app Angular duy nhất. Ranh giới do ESLint canh.

Điều đó có một hệ quả phải xử lý: **ESLint bỏ qua được bằng một dòng comment.** Vì vậy luật F3 ở [`RULES.md`](RULES.md) cấm `eslint-disable` cho đúng danh sách rule ranh giới, và cổng FE kiểm điều đó.

Không có F3 thì F1 và F2 chỉ là gợi ý.

📖 [`adr/0007-fe-giu-cau-truc-thu-muc.md`](adr/0007-fe-giu-cau-truc-thu-muc.md) · [`quy-uoc/fe-architecture.md`](quy-uoc/fe-architecture.md)

### 6.2 Ngoại lệ có chủ đích: `Design/` phủ cả Core lẫn nghiệp vụ

[`Design/`](Design/) là nguồn giao diện duy nhất và chứa **cả** màn hình Core lẫn màn hình nghiệp vụ, **cả** component dùng chung lẫn component riêng sản phẩm.

Vì vậy **không** áp luật "gỡ nghiệp vụ khỏi Core" cho khu Design: một spec component được phép trích dẫn màn hình nghiệp vụ làm nơi nó xuất hiện. Chia đôi khu này sẽ phá đúng thứ nó sinh ra để làm — trả lời một câu hỏi *"giao diện chỗ này trông ra sao"* ở **một** chỗ.

---

## 7. Một module gồm những gì — hợp đồng lắp ghép

Để lắp được vào Host, một module phải cung cấp:

| Thành phần | Vai |
| --- | --- |
| `Modules.<X>.Domain` | Entity, VO, luật nghiệp vụ |
| `Modules.<X>.Application` | Command/Query, handler, validator, DTO |
| `Modules.<X>.Infrastructure` | `DbContext` riêng, EF configuration, **migration schema của mình** |
| `Modules.<X>.Endpoints` | Controller kế thừa `ApiControllerBase` của Core |
| Một nguồn danh mục khoá quyền | Khoá của module qua `IPermissionCatalogSource`, Core kiểm lúc khởi động — [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §1.1 |
| Một extension `AddXModule()` | Điểm đăng ký duy nhất — Host chỉ gọi đúng dòng này |

**Module KHÔNG được đăng ký lại pipeline behavior** — behavior sống ở `Core.Application` và là open-generic, tự áp cho mọi request bất kể assembly nào khai handler. Đăng ký hai lần khiến mỗi request chạy behavior hai lượt; luật A10 bắt điều này.

---

## 8. Số liệu — đếm bằng lệnh, đừng chép

Theo [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §6, **không chép số lượng project vào tài liệu**. Bảng liệt kê tay sẽ mục ruỗng.

Khi `src/` tồn tại (giai đoạn 2), đếm bằng:

```bash
find src/BE -iname '*.csproj' | wc -l          # project trên đĩa
grep -c '<Project' src/BE/*.slnx                # project thật sự trong solution
```

Hai con số này **có thể khác nhau** — một project trên đĩa mà không nằm trong solution thì không được build, không được test, và không ai biết. Luật A6 ở [`RULES.md`](RULES.md) bắt trường hợp đó.

> Ở dự án tiền nhiệm, một bảng trong tài liệu chép cứng "8 project" và sai suốt từ đó, qua ít nhất hai lần con số thật thay đổi.

---

## 9. Bảng chuyển trạng thái

Khi `src/` bắt đầu được xây ở giai đoạn 2, thay bảng này bằng bảng *"có thật hôm nay → sẽ thành"* theo [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §4.

| Hôm nay (giai đoạn 1) | Sẽ thành (giai đoạn 2) |
| --- | --- |
| Chưa có `src/` | `src/BE` 5 project Core + host + tests; `src/FE` một app Angular bốn tầng |
| Chưa có cổng BE/FE | `dotnet test` (gồm ArchTests) + `fe-gate.sh` + bộ lệnh `ng` |
| Mọi luật `📐` ở [`RULES.md`](RULES.md) | Chuyển dần sang `✅` khi cổng tương ứng được viết |
