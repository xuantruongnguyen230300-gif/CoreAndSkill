---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng của `kien-truc-core-module.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`docs/kien-truc-core-module.md`](../../../kien-truc-core-module.md); luật ở đó. Số mục dưới đây trùng số mục của file luật — mục không có gì dời thì ghi "—".

---

## 1. Mô hình: Modular Monolith

Sơ đồ lắp ghép:

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

## 2. Backend — Năm project Core

Vì sao bảng project chỉ được có **một** bản, và file thi công ([`quy-uoc/be-architecture.md`](../../../quy-uoc/be-architecture.md)) phải trỏ về thay vì chép lại: hai bản của bảng này là đúng cách repo tiền nhiệm có bốn sơ đồ đặt tên project nói ngược nhau.

Cột *Tham chiếu được* của bảng nói chiều phụ thuộc **giữa các project Core**. Vì sao module có thêm cạnh `Modules.<X>.Infrastructure` → `Core.Infrastructure`: `DbContext` của module phải áp **cùng** bộ lọc truy vấn với Core. Lý do và hệ quả: [`quy-uoc/be-architecture.md`](../../../quy-uoc/be-architecture.md) §1.2.
> **Dòng `Core.Infrastructure` cấm `HttpContext` là dòng hay bị vi phạm nhất**, vì lớp đọc danh
> tính người dùng nhìn có vẻ thuộc về hạ tầng. Nó không: `HttpContextCurrentUser` sống ở
> `Core.Web`, và `Core.Infrastructure` chỉ thấy seam `ICurrentUser` / `ITenantContext`.

### 2.1 Vì sao tách 5 project chứ không gộp 1

Cám dỗ lớn là gom hết vào một `Core.dll` cho gọn. Đừng.

**Tách project là cách duy nhất để compiler ép luật layering.** `Core.Domain` không tham chiếu `Core.Infrastructure` thì code trong Domain **không thể** gọi `DbContext` — dù người viết có muốn. Đây là hàng rào chi phí bằng 0, không bao giờ hỏng, và không phụ thuộc ai nhớ luật.

Luật viết trong tài liệu thì người ta quên. Luật viết trong đồ thị tham chiếu thì compiler nhắc.

### 2.2 Vì sao có `Core.Web` — bệnh nặng nhất cần trị

Ở dự án tiền nhiệm, toàn bộ hạ tầng web nằm trong project host: `ApiControllerBase`, sáu middleware, `GlobalExceptionHandler`, `HttpContextCurrentUser`, chính sách CORS, cùng bốn controller của Core (auth, user, permission, menu).

Hệ quả đo được và các hậu quả kéo theo: [`quy-uoc/be-architecture.md`](../../../quy-uoc/be-architecture.md) §2.2.

`Core.Web` tồn tại để **Core tự đứng được**. Sau khi có nó, `Program.cs` của một dự án mới chỉ còn gọi `AddCore()`, `UseCore()`, rồi đăng ký module của mình.

### 2.3 Vì sao KHÔNG có `Core.Common` và `Core.Persistence`

Hai project này từng nằm trong phương án 6-project. Đã loại, có lý do:

| Project bị loại | Vì sao |
| --- | --- |
| `Core.Common` | Tên này gần như luôn biến thành ngăn kéo rác. Một project tên "Common" **không có tiêu chí nào để từ chối** thứ gì cả — và cái gì cũng có thể lý luận là "dùng chung". Guard clause và extension method thuộc về `Domain`. |
| `Core.Persistence` tách khỏi `Core.Infrastructure` | Lợi ích được viện dẫn là "đổi provider DB không đụng hạ tầng khác". Trong hệ tầm trung điều đó gần như không bao giờ xảy ra. Chi phí thì vĩnh viễn: thêm một project và thêm câu hỏi *"file này bỏ vào đâu"* cho mọi file mới. |

### 2.4 Host — composition root duy nhất

Vì sao có ngưỡng 50 dòng cho `Program.cs`: vượt ngưỡng nghĩa là có thứ gì đó lẽ ra thuộc `Core.Web` đang nằm sai chỗ.

## 3. Ba luật ranh giới — ép bằng test, không bằng niềm tin

Vì sao một chuỗi tên nghiệp vụ trong `Core.Application` là vi phạm R1: nó khiến Core không mang đi được sang dự án không có khái niệm đó.

> ⚠️ **Bài học khi thi công A5:** ở dự án tiền nhiệm, detector cho luật này quét **văn bản nguồn** thay vì AST. Hệ quả là code phải viết vòng để né detector — một chuỗi nội suy hợp lệ vẫn bị bắt, nên người viết phải tách biến chỉ để làm chuỗi "sạch". Đuôi vẫy chó. Đó là lý do file luật yêu cầu ưu tiên phân tích AST, và detector quét văn bản phải có test chứng minh nó bỏ qua comment và định danh (luật T1).

## 4. Tiêu chí: cái gì thuộc Core, cái gì thuộc Module

### 4.1 Phép thử

—

### 4.2 Ngưỡng định lượng — chống Core phình to

**Vì sao ngưỡng "hai module trở lên" quan trọng:** rủi ro lớn nhất của một repo Core không phải thiếu tính năng, mà là **Core phình to** vì cái gì cũng "để đó cho tiện". Một Core chứa nghiệp vụ là một Core không mang đi đâu được — tức là nó đã thất bại ở đúng lý do nó tồn tại.

### 4.3 Khi nào tách một module mới

Tách quá sớm tạo ra chi phí phối hợp mà không đổi lấy gì.

## 5. Ranh giới dữ liệu

Vì sao một lần ghi phải là một transaction trên mọi `DbContext`: nhờ đó outbox và nhật ký ở schema `core` commit cùng dữ liệu module.

> **Cấm FK xuyên schema là điều kiện tiên quyết để sau này tách microservice.** Một FK xuyên schema là một sợi dây trói vĩnh viễn — không tách được mà không sửa schema, và sửa schema trên dữ liệu thật là việc đắt nhất trong vòng đời một hệ thống.

## 6. Frontend — bốn tầng

### 6.1 Ranh giới FE được ép bằng gì

Khác với BE (compiler ép qua ProjectReference), FE giữ cấu trúc thư mục trong một app Angular duy nhất, nên ranh giới chỉ do ESLint canh — và **ESLint bỏ qua được bằng một dòng comment**. Đó là lý do luật F3 tồn tại: không có F3 thì F1 và F2 chỉ là gợi ý.

### 6.2 Ngoại lệ có chủ đích: `Design/` phủ cả Core lẫn nghiệp vụ

Chia đôi khu Design sẽ phá đúng thứ nó sinh ra để làm — trả lời một câu hỏi *"giao diện chỗ này trông ra sao"* ở **một** chỗ.

## 7. Một module gồm những gì — hợp đồng lắp ghép

Vì sao module không được đăng ký lại pipeline behavior: behavior là open-generic, tự áp cho mọi request bất kể assembly nào khai handler; đăng ký hai lần khiến mỗi request chạy behavior hai lượt.

Cơ chế "cùng một lời quét": `AddXModule()` ghi nhận assembly của module, `AddCoreApplication` gom rồi quét một lần — nên dòng của module ở host đứng trước `AddCore`.

## 8. Số liệu — đếm bằng lệnh, đừng chép

Một project trên đĩa mà không nằm trong solution thì không được build, không được test, và không ai biết — đó là ca luật A6 bắt. Ở dự án tiền nhiệm, một bảng trong tài liệu chép cứng "8 project" và sai suốt từ đó, qua ít nhất hai lần con số thật thay đổi. Bảng liệt kê tay sẽ mục ruỗng.

## 9. Bảng chuyển trạng thái

—

## 10. Đường dẫn chạm Core

Vì sao khối máy đọc có bốn luật định dạng:

| Luật định dạng | Vì sao |
| --- | --- |
| Khối nằm giữa hai dòng chú thích HTML đánh dấu mở và đóng; bên trong là **một** khối rào không gắn ngôn ngữ | Parser tìm hai dòng đánh dấu, không đoán theo tiêu đề — đổi câu chữ của mục này không làm hỏng phép đọc |
| Mỗi dòng một đường dẫn tính từ gốc repo; thư mục kết thúc bằng `/`, tệp ghi đúng tên | Dấu `/` cuối là thứ phân biệt "mọi thứ bên trong" với "đúng tệp này" |
| Dòng trống và dòng bắt đầu bằng `#` bị bỏ qua | Chú thích nhóm nằm ngay trong khối mà không thành một đường dẫn giả |
| Không liệt kê tệp bị `.gitignore` loại | Hook tự loại chúng bằng `git check-ignore`; liệt kê ra là để một thư mục output build kích hoạt review |

**Vì sao host và ArchTests nằm trong khối:** host là nơi Core được lắp vào một sản phẩm, và ArchTests
là hàng rào của mọi luật ở [`kien-truc-core-module.md`](../../../kien-truc-core-module.md) §3 — sửa một test là nới một luật mà không đụng dòng luật nào. **Vì sao
tài liệu quy ước nằm trong khối:** sửa một quy ước là đổi thứ code Core phải tuân, nên nó cần cùng một
lượt review như sửa chính code đó.
