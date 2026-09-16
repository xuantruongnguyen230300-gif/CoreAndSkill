---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng — `be-entity-domain.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`../../../quy-uoc/be-entity-domain.md`](../../../quy-uoc/be-entity-domain.md);
> luật ở đó. Số mục ở đây **trùng số §** của file luật; mục không có gì dời thì ghi "—".
>
> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi khối code là khuôn cho `src/BE` sẽ xây ở giai đoạn 2.

---

## 1. `BaseEntity`

### 1.1 `Id` là `init`, sinh bằng UUID v7

Hai quyết định gộp trong một dòng.

**`init` chứ không `set`:** cách làm ngược lại — để interceptor sinh Id lúc `SaveChangesAsync` — khiến
entity mang `Guid.Empty` trong suốt khoảng từ khi tạo tới khi lưu, và **mọi thứ cần Id trước đó đều
vỡ**: gắn quan hệ giữa hai entity mới, phát domain event, trả Id về cho caller trong cùng một handler.

**UUID v7 chứ không v4 (`Guid.NewGuid()`):** v7 mang timestamp ở phần đầu nên các giá trị sinh gần
nhau về thời gian thì gần nhau về thứ tự. Hệ quả trên PostgreSQL:

| | UUID v4 (ngẫu nhiên) | UUID v7 (tuần tự theo thời gian) |
| --- | --- | --- |
| Vị trí chèn trong B-tree của khoá chính | Rải khắp cây | Luôn ở mép phải |
| Page split | Xảy ra liên tục ở các trang giữa | Gần như chỉ ở trang cuối |
| Phân mảnh index | Tăng dần theo số bản ghi | Thấp và ổn định |
| Cache hit của trang index nóng | Kém — trang nóng trải rộng | Tốt — trang nóng là vài trang cuối |

Đây là vấn đề thật ở bảng ghi nhiều, không phải tối ưu sớm: nó không sửa được sau khi đã có dữ liệu
mà không đổi khoá chính.

**Điểm sinh Id là `EntityId.New()`:** một seam một dòng cho phép đổi cách sinh Id ở đúng một chỗ, và
cho phép các kiểu **không** kế thừa `BaseEntity` (như `AppUser`) dùng chung cùng quy tắc.

### 1.2 Năm field audit có public setter — CÓ CHỦ ĐÍCH, và có giới hạn

Đây **không** phải sơ suất encapsulation. `AuditInterceptor` phải **ghi** được năm field này từ bên
ngoài entity. Nếu để `protected`/`private`, interceptor buộc phải dùng reflection hoặc shadow property —
cả hai đều phức tạp hơn nhiều so với cái chúng bảo vệ, và reflection ở đường ghi nóng là thứ ta đang cố
loại khỏi codebase này.

Năm field kể trên nằm ở lớp cơ sở nên nằm ngoài phạm vi detector của luật
`BaseEntity_Descendants_MustNotHave_PublicSetter` — đó chính là cách phân biệt được viết thành cổng.

### 1.3 `AuditInterceptor` ghi gì ở mỗi trạng thái

**Vì sao ghi luôn `Updated*` lúc tạo:** để hai cột đó **không null** từ lúc bản ghi ra đời. Nếu để
null, mọi nơi hiển thị "sửa lần cuối" phải tự viết `UpdatedAt ?? CreatedAt` — ở từng màn danh sách,
từng báo cáo, từng câu `ORDER BY`. Chỉ cần một chỗ quên là bản ghi **chưa sửa lần nào** rơi xuống cuối
danh sách sắp theo thời gian sửa: sai im lặng, không lỗi, không test nào bắt.

**Cái giá đã cân và chấp nhận:** mất khả năng đọc `UpdatedAt is null` để biết "bản ghi này chưa từng bị
sửa". Câu hỏi đó hiếm khi được hỏi.

Cả bốn field lấy chung một biến `now` để bản ghi mới có `CreatedAt` **bằng đúng** `UpdatedAt`, không
lệch vài mili giây. Một interceptor không được đăng ký là một cơ chế tồn tại trên giấy — vì thế có luật
`EveryInterceptor_IsWiredInto_DbContextOptions`.

### 1.4 `IAuditableEntity` cho kiểu không kế thừa `BaseEntity`

Chọn entity theo `BaseEntity` thay vì theo interface thì mọi kiểu nằm ngoài cây kế thừa phải ghi tay
bốn cột ở **từng** điểm tạo và sửa — và chỗ quên không lỗi, không cảnh báo, chỉ để lại cột rỗng.

`UserManager` lưu `AppUser` bằng `SaveChangesAsync` cả cho thao tác sổ sách của nó, nên
`UpdatedAt`/`UpdatedBy` của một tài khoản đổi theo cả những thao tác đó, không chỉ khi hồ sơ được sửa.

---

## 2. Encapsulation — field nghiệp vụ `private set`

Hình dạng tối thiểu của một entity đóng gói đúng là khối `Tenant` ở [`../../../quy-uoc/be-entity-domain.md`](../../../quy-uoc/be-entity-domain.md) §3.2 — không chép lại ở đây.

**Vì sao:** invariant chỉ là luật khi nó **không thể** bị vượt qua. Nếu ai đó gán được `tenant.IsActive`
của đơn vị hệ thống từ bên ngoài, luật "đơn vị hệ thống không bị ngưng hoạt động" chỉ còn là quy ước
bằng lời — và quy ước bằng lời thì lần thứ một trăm sẽ có người quên.

Tên method cũng là tài liệu: `Deactivate()` nói được lý do nghiệp vụ mà `IsActive = false` không nói
được — nó chặn hay chỉ ẩn? có ghi vết không? có kéo theo gì không? Câu trả lời nằm trong thân method,
đúng một chỗ.

---

## 3. Factory & mutation method trả `Result<T>` — KHÔNG ném exception

Đây là điểm lệch lớn nhất khỏi dự án tiền nhiệm. Đọc kỹ.

### 3.1 Cách cũ — và vì sao bỏ

```csharp
// ❌ KHÔNG dùng ở repo này
public static Criteria Create(string code, decimal maxScore)
{
    if (string.IsNullOrWhiteSpace(code))
        throw new DomainException(CriteriaErrors.CodeRequired);
    if (maxScore <= 0)
        throw new DomainException(CriteriaErrors.MaxScoreInvalid);
    return new Criteria { Code = code, MaxScore = maxScore };
}
```

Bốn vấn đề, xếp theo mức độ đắt:

1. **Lỗi nghiệp vụ là kết quả mong đợi, không phải sự cố.** "Mã bị trùng" xảy ra hàng ngày trong vận
   hành bình thường. Dùng exception cho nó là dùng cơ chế thoát hiểm cho luồng chính.
2. **Chữ ký nói dối.** `Criteria Create(...)` hứa trả về một `Criteria`. Người gọi không thấy trong kiểu
   bất kỳ dấu hiệu nào rằng lời gọi này có thể thất bại, nên rất dễ quên bọc.
3. **Cần một cầu nối exception → HTTP.** Cầu nối đó ở dự án tiền nhiệm dựng bằng reflection và **đã nổ
   trên Production** — xem
   [`2026-09-05-reflection-envelope.md`](../../../audit/2026-09-05-reflection-envelope.md). Bỏ exception
   là bỏ luôn lý do tồn tại của cầu nối đó.
4. **Chỉ báo được một lỗi mỗi lần.** `throw` đầu tiên kết thúc hàm, nên người dùng sửa một lỗi rồi lại
   thấy lỗi tiếp theo. `Result` gom được nhiều lỗi trong một lượt.

### 3.2 Cách của repo này

Đặt catalog `TenantErrors` ở `Core.Application` thì khối `Tenant` **không biên dịch được** — và một mẫu
không biên dịch được là một mẫu sẽ được chép nguyên xi rồi mới phát hiện.

Vì sao không gán `Id` trong factory: gán lại là sinh GUID thứ hai rồi ghi đè cái thứ nhất — không sai
kết quả, nhưng làm mờ chỗ nào thật sự sở hữu Id. Vì sao không gán `CreatedAt`/`UpdatedAt`: gán tay là
dạy đúng thứ interceptor sinh ra để khỏi phải làm, và tạo nguồn thứ hai lệch được với nguồn thứ nhất.

### 3.3 `Error` sống ở đâu

Quy tắc một dòng: **catalog nằm cùng project với code ném ra nó.** Đừng đọc nó thành "catalog thuộc
`Application`" — đặt sai chỗ thì hỏng lúc biên dịch `Core.Domain`.

### 3.4 Khi nào vẫn còn được ném exception

—

---

## 4. Value Object

Bọc VO cho một `decimal`/`string` đơn lẻ không có luật gì là thêm một tầng để không đổi lấy gì.
`record` cho sẵn so sánh theo giá trị — đúng bản chất của Value Object: hai địa chỉ email cùng chuỗi là
**một**, không phải hai thứ giống nhau.

### 4.1 Ánh xạ EF Core

`.Value` ném trong nhánh đọc là hành vi đúng: dữ liệu đã lưu mà không dựng lại được VO là lỗi ngoài dự
kiến, không phải lỗi nghiệp vụ.

---

## 5. Soft delete và tenant — hai vòng lặp, không khai lẻ

### 5.1 Chữ ký CoreDbContext

**Vì sao module không kế thừa `IdentityDbContext`:** lớp đó khai bảng Identity của schema `core`; kế
thừa nó thì model của mỗi module mang thêm một bản bảng Identity, và migration của module sinh ra chúng.

**`CurrentTenantId` rỗng ⇒ truy vấn trả rỗng:** thiếu đơn vị làm hệ thống đóng, không làm nó mở.
`DbContext` nhận `ITenantContext` qua constructor vì `Core.Infrastructure` bị **cấm** phụ thuộc
`HttpContext`.

**Hai vòng lặp, hai điều kiện:** `typeof(BaseEntity).IsAssignableFrom(...)` cho vòng soft delete,
`typeof(ITenantScoped)…` cho vòng tenant. Chép điều kiện của vòng này sang vòng kia là cách làm `AppUser`
mất filter tenant mà không có gì báo.

**Vì sao không gộp hai điều kiện:** filter **không tên** thì lần khai sau ghi đè lần khai trước, nên một
filter gộp không tên sẽ **xoá** filter kia — không lỗi biên dịch, không cảnh báo, không ngoại lệ. Nếu thứ
bị xoá là filter tenant thì hậu quả là **đơn vị A nhìn thấy dữ liệu đơn vị B**. Gộp cũng làm hai luật
(E3 và M1) mất khả năng kiểm độc lập.

**Vì sao không chụp `TenantId` vào biến cục bộ:** model được cache theo kiểu `DbContext`, nên giá trị
của request đầu tiên sẽ được dùng lại cho **mọi tenant** sau đó. Đây là lỗi rò dữ liệu nặng nhất của
cả mô hình — [`17-multi-tenant.md`](../17-multi-tenant.md) §4.

### 5.2 Ba ràng buộc, cả ba hỏng im lặng nếu bỏ

1. *Thứ tự* — đảo thứ tự thì các entity chỉ được đăng ký vào model bởi configuration sẽ chưa có mặt
   lúc vòng lặp chạy, và **mọi** entity đó mất filter — không lỗi, không cảnh báo.
2. *Filter phải có tên* — EF Core cho phép nhiều query filter đặt tên trên cùng một entity. Đặt tên là
   cách để một module thêm filter riêng mà không xoá mất filter soft delete của Core.
3. *Khai lẻ là sai* — đây là dạng lỗi rò rỉ dữ liệu đã xoá ra API mà chỉ người dùng phát hiện.

Ràng buộc số 2 ở vòng lặp tenant nặng hơn hẳn: mất filter soft delete làm dữ liệu đã xoá hiện lại —
phiền, người dùng báo ngay. Mất filter tenant làm **đơn vị này nhìn thấy dữ liệu đơn vị khác** — và có
thể không ai báo trong nhiều tháng.

### 5.3 Cái bẫy đi kèm — index unique phải gồm BA thứ

Xoá mềm nghĩa là dòng cũ **vẫn nằm trong bảng**. Một index unique không lọc sẽ chặn việc chèn lại đúng
cặp giá trị đã xoá. Và trên một entity `ITenantScoped`, một index unique không gồm `TenantId` sẽ chặn
đơn vị B dùng một mã mà đơn vị A đã dùng. `TenantId` đứng đầu vì mọi truy vấn đều lọc theo tenant
trước, nên index dẫn đầu bằng cột được lọc trước thì seek được.

Hai cách hỏng, hai triệu chứng khác hẳn nhau — và cả hai đều không có lỗi biên dịch:

| Thiếu | Triệu chứng người dùng thấy |
| --- | --- |
| Mệnh đề `is_deleted` | *"Tôi vừa xoá nó xong mà, sao bảo trùng?"* — dòng chiếm khoá là một dòng đã xoá, không màn hình nào hiển thị |
| `TenantId` | *"Hệ thống nói mã trùng nhưng tôi tìm không thấy"* — dòng chiếm khoá thuộc **đơn vị khác**, và bộ lọc tenant đã giấu nó đi |

Ca thứ nhất là bẫy đã dính thật ở dự án tiền nhiệm, và cả hai chỉ lộ ra khi có người thử tạo lại một
bản ghi — thường là vài tháng sau khi tính năng lên.

**Trần 63 byte của tên index:** thêm `_tenant` vào mọi tên đẩy một số tên chạm trần, và PostgreSQL cắt
**âm thầm** — không lỗi, không cảnh báo. Hai index rồi va nhau với thông báo *"relation already exists"*
trỏ vào một cái tên chưa ai từng gõ.

**`HasFilter` là SQL thô:** đây là chỗ trừu tượng hoá của EF Core **rò ra** — mọi nơi khác bạn viết tên
property và quy ước đặt tên tự dịch sang tên cột, nhưng chuỗi SQL thô thì không ai dịch hộ. Viết sai tên
ở đây **không gây lỗi biên dịch** — nó gây lỗi lúc chạy migration, hoặc tệ hơn, tạo ra một index lọc
theo một cột không tồn tại nếu cơ sở dữ liệu chấp nhận.

### 5.4 Khi nào bỏ qua filter

Một lời gọi `IgnoreQueryFilters` không nêu được thuộc nhóm nào là dấu hiệu đang vá một query sai chỗ
khác. Dạng không tham số gỡ cả filter tenant: ở một màn hình khôi phục dữ liệu, đó là một lỗ rò toàn
phần cho một nhu cầu chỉ cần gỡ một nửa.

---

## 6. Concurrency — dùng `xmin` của PostgreSQL

Vì sao cấm tự thử lại khi xung đột: tự thử lại nghĩa là ghi đè thay đổi của người khác một cách tự
động, tức mất trắng giá trị của cả cơ chế, trong khi mọi thứ nhìn vẫn như đã làm đúng.

### 6.1 Entity nào cần token — và entity nào không

Khuôn nhận diện điển hình: một entity vừa nhận **ghi hàng loạt** (import ghi đè toàn bộ field) vừa nhận
**sửa tay từng field** (đọc-rồi-ghi). Không có gì phát hiện khi hai luồng ghi đè lên nhau; người sửa sau
âm thầm mất thay đổi của người trước.

> ⚠️ **Cẩn thận với suy luận "chỉ có một luồng ghi".** Ở dự án tiền nhiệm, một tài liệu khẳng định vài
> bảng chỉ được ghi bởi tiến trình seed — trong khi màn hình phân quyền cũng ghi chúng qua một endpoint
> khác. Giới hạn **ai** ghi được không giới hạn **bao nhiêu người** ghi cùng lúc.

### 6.2 Recipe concurrency token cho Npgsql

—

### 6.3 Kiểu CLR nào hợp lệ cho concurrency token

Đây là bẫy đã dính thật, và là mục chở luật chống lỗi im lặng của cả chủ đề này. Nhánh `byte[]` trên
Npgsql không có lỗi biên dịch, không có lỗi lúc chạy, không có cảnh báo. Ghi đè vẫn xảy ra đúng như khi
chưa làm gì cả. Đây chính là hình dạng của sự cố ở dự án tiền nhiệm: một recipe dùng API của SQL Server
tồn tại **song song ở nhiều file tài liệu** trong một dự án chạy Npgsql, và sửa một nơi không chạm nơi
kia — vì thế file luật là file chủ duy nhất của recipe.

`UseXminAsConcurrencyToken()` đã bị Npgsql đánh obsolete rồi gỡ hẳn; ai còn thấy nó trong code chép từ
đâu đó thì đó là dấu hiệu code chưa từng build được với package version hiện tại, không phải một lựa
chọn thiết kế.

### 6.4 Không áp cho `AppUser`/`AppRole`

Thêm cột thứ hai chồng lên `ConcurrencyStamp` là hai nguồn cho cùng một sự thật, và chúng lệch được mà
vẫn biên dịch.

---

## 7. Ranh giới Identity — `AppUser` không rời khỏi Infrastructure

### 7.1 Luật

Ba interface thay vì một là áp dụng ISP: một handler chỉ cần tra cứu người dùng không phải phụ thuộc
vào bề mặt cho phép khoá tài khoản.

`ITenantLookup` đứng riêng, không ghép vào `IIdentityService`: nó chạy **trước** khi có phạm vi đơn vị và
không chạm kiểu Identity nào — ghép vào là buộc seam Identity có một thao tác chạy ngoài phạm vi.

### 7.2 Hệ quả bắt buộc

Trả `AppUser` ra ngoài là rò rỉ toàn bộ bề mặt Identity — gồm cả `PasswordHash` và `SecurityStamp`.
`AppUser` không kế thừa `BaseEntity` vì C# không cho kế thừa hai lớp cơ sở, và `IdentityUser<Guid>` là
bắt buộc.

### 7.3 Đây là đánh đổi có ý thức

**Lợi:** dùng ngay được toàn bộ ASP.NET Core Identity — băm mật khẩu đúng chuẩn, lockout, `SecurityStamp`
(thu hồi phiên được), xác nhận email, two-factor. Tự viết lại những thứ này là công việc nhiều tháng và
là chỗ dễ sai nhất trong một hệ thống.

**Giá phải trả, nói thẳng:** đổi sang SSO/LDAP/OIDC sau này **phải sửa `Core.Infrastructure`** và cấu
hình scheme ở `Core.Web`. Không đổi được bằng cấu hình.

Cụ thể phần nào phải viết lại: implementation của ba interface và của seam dựng `ClaimsPrincipal`,
authentication scheme ở `Core.Web`, và cách ánh xạ nhóm/vai trò từ nguồn ngoài về bảng vai trò nội bộ.
Cụ thể phần nào **không** phải sửa: `Core.Application`, `Core.Domain`, mọi handler, mọi controller — vì
chúng chỉ thấy các interface.

Đó chính là giá trị của ranh giới này: nó không làm việc đổi trở nên **rẻ**, nó làm việc đổi trở nên
**có phạm vi biết trước**.

---

## 8. FK và ranh giới schema

Cấm FK vật lý xuyên schema là **điều kiện tiên quyết để sau này tách microservice**: một FK xuyên schema
là một sợi dây trói vĩnh viễn — không tách được mà không sửa schema, và sửa schema trên dữ liệu thật là
việc đắt nhất trong vòng đời một hệ thống.

---

## 9. ArchTest canh gì trong file này

—

## 10. Khi thêm một entity mới — thứ tự

—
