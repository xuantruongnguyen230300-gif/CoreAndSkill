---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 02. Danh tính, phiên và phân quyền

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> File này giữ **mô hình và lý do**. Cách thi công cụ thể (chữ ký, đăng ký DI, thuộc tính trên controller) ở [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md). Hợp đồng endpoint ở [`../../contracts/auth.md`](../../contracts/auth.md) và [`../../contracts/permissions.md`](../../contracts/permissions.md).

---

## 1. Mô hình phiên — cookie session, KHÔNG JWT

### 1.1 Nó hoạt động thế nào

Đăng nhập thành công → server tạo một phiếu xác thực, ký và mã hoá, đặt vào cookie `HttpOnly`. Mọi request sau đó trình duyệt tự gửi kèm cookie; server giải mã phiếu và biết ai đang gọi. Trạng thái "đang đăng nhập" **thuộc về server**, không thuộc về client.

Đó là điểm khác biệt gốc so với JWT, và mọi ưu nhược điểm dưới đây đều chảy ra từ nó.

### 1.2 Bảng đánh đổi — đọc cả hai cột

| Tiêu chí | Cookie session | JWT (bearer trong header) |
| --- | --- | --- |
| **Kịch bản XSS trộm được token** | Cookie `HttpOnly` **JavaScript không đọc được**. Script chèn được vẫn gọi API thay người dùng, nhưng **không mang token đi nơi khác** | Token phải nằm ở chỗ JS đọc được để gắn vào header. Trộm được là dùng được ở bất kỳ đâu, cho tới khi hết hạn |
| **Thu hồi phiên** | **Tức thì.** Xoá phiếu ở server, request kế tiếp trượt | Token đã phát thì hợp lệ tới lúc hết hạn. Muốn thu hồi phải có danh sách đen — tức là quay lại trạng thái ở server, tức là mất đúng lý do chọn JWT |
| **CSRF** | **Phải xử lý.** Trình duyệt tự gửi cookie kể cả khi request đến từ trang khác | Không bị, vì header `Authorization` không tự động gắn |
| **FE và BE khác origin** | **Cùng tên miền gốc:** `SameSite=Lax` là đủ — vẫn là cùng site. **Khác tên miền gốc:** phải `SameSite=None` + `Secure`, và trình duyệt có thể chặn cookie bên thứ ba | Không vướng, chỉ cần CORS cho phép header |
| **Client không phải trình duyệt** (app di động, dịch vụ gọi dịch vụ) | Kém — phải tự quản lý cookie jar, không có cơ chế làm mới chuẩn | Tốt, đây là chỗ JWT thắng rõ |
| **Chạy nhiều instance** | Cần **kho khoá dùng chung** để mọi instance giải mã được phiếu do instance khác ký | Không cần chia sẻ gì, chỉ cần cùng khoá ký |
| **Kích thước mỗi request** | Nhỏ — cookie chỉ mang phiếu | Lớn dần theo số claim nhét vào token |
| **Đọc được thông tin người dùng mà không hỏi server** | Không | Có — và đó vừa là ưu điểm vừa là cái bẫy: thông tin trong token là **ảnh chụp lúc phát**, quyền đã bị thu hồi vẫn còn nguyên trong token |

### 1.3 Vì sao chọn cookie cho Core này

Ba lý do, xếp theo trọng số:

1. **Thu hồi tức thì là yêu cầu thật ở hệ quản trị nội bộ.** Khoá một tài khoản phải có hiệu lực ngay, không phải "sau 15 phút nữa". Với JWT, để đạt điều đó phải kiểm danh sách đen ở mỗi request — và khi đã kiểm ở mỗi request thì JWT không còn phi trạng thái nữa, chỉ còn lại nhược điểm của nó.
2. **Client chính là trình duyệt.** Đây là ứng dụng quản trị, không phải API công khai cho bên thứ ba. Điểm mạnh nhất của JWT (client đa dạng, dịch vụ gọi dịch vụ) không được dùng tới.
3. **`HttpOnly` là hàng rào XSS không mất tiền.** Không phải hàng rào tuyệt đối — script chèn được vẫn thao tác thay người dùng — nhưng nó chặn đúng kịch bản tệ nhất: token bị mang ra khỏi trình duyệt và dùng lại từ nơi khác, lâu dài.

### 1.4 Cái giá phải trả — ghi thẳng, không giấu

| Cái giá | Xử lý ở đâu |
| --- | --- |
| Phải có antiforgery cho mọi endpoint ghi | [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §7.2 |
| FE và API phải cùng tên miền gốc **và cùng scheme** để cookie còn là cùng site → **HTTPS ở mọi môi trường**, kể cả máy lập trình viên | [`../../adr/0015-fe-va-api-khac-nguon.md`](../../adr/0015-fe-va-api-khac-nguon.md) |
| Chạy nhiều instance cần kho khoá dùng chung | §1.5 dưới đây |
| App di động sau này sẽ vướng | §1.6 |

### 1.5 Chạy nhiều instance — hai thứ khác nhau, đừng lẫn

| Thứ | Vấn đề nếu thiếu | Giải pháp |
| --- | --- | --- |
| **Kho khoá bảo vệ dữ liệu** (khoá dùng để ký và mã hoá phiếu) | Mỗi instance sinh khoá riêng trong bộ nhớ → phiếu do instance A ký thì instance B không giải được → người dùng bị đăng xuất ngẫu nhiên khi tải cân bằng đổi instance | Trỏ kho khoá vào một nơi dùng chung (thư mục mạng, hoặc bảng trong DB). **Bắt buộc trước khi chạy instance thứ hai** |
| **Kho phiếu phía server** (lưu trạng thái phiên ở server thay vì nhét vào cookie) | Không có nó, phiên vẫn chạy được — nhưng thu hồi tức thì chỉ đạt được ở mức "đánh dấu người dùng cần xác thực lại", không phải "xoá đúng phiên này" | Chỉ thêm khi cần quản lý phiên ở mức từng thiết bị |

**Bẫy đã thấy ở nhiều dự án:** người ta chạy hai instance, thấy người dùng bị đăng xuất ngẫu nhiên, rồi đổ lỗi cho cookie hoặc cho tải cân bằng. Nguyên nhân gần như luôn là kho khoá chưa dùng chung. Triệu chứng: lỗi giải mã phiếu trong log, và tần suất tỉ lệ thuận với số instance.

### 1.6 Khi nào nên đổi sang JWT — ngưỡng cụ thể

Đổi khi thoả **ít nhất một**:

- Có client **không phải trình duyệt** cần gọi API: app di động, hệ thống bên ngoài, tích hợp máy-với-máy.
- Cần **uỷ quyền cho bên thứ ba** — người dùng cho phép một ứng dụng khác truy cập dữ liệu của họ. Đây là bài toán OAuth2, và khi tới đó thì **dùng một máy chủ uỷ quyền có sẵn**, đừng tự phát token. Tự chế token là chỗ dễ sai nhất trong toàn bộ mảng bảo mật.
- Kiến trúc tách thành nhiều dịch vụ cần truyền danh tính qua lại mà không gọi ngược về một dịch vụ trung tâm.

**Không** đổi vì: "JWT hiện đại hơn", "phi trạng thái nghe scale tốt hơn", hoặc "cookie phiền vì CSRF". Ba lý do đó đều không đứng vững ở quy mô này.

Nếu đổi: đó là quyết định cần ADR — nó chạm phiên, CORS, FE, và mọi endpoint.

---

## 2. ASP.NET Core Identity — dùng gì, khoanh vùng ra sao

### 2.1 Vì sao giữ Identity thay vì tự viết

Identity giải quyết một nhóm việc mà **sai một chi tiết là lỗ hổng**, và không nhóm nào đủ rảnh để làm lại cho đúng:

| Identity lo | Nếu tự viết thì sai ở đâu |
| --- | --- |
| Băm mật khẩu (thuật toán chậm có muối, có số vòng lặp, có phiên bản để nâng cấp) | Chọn thuật toán nhanh, quên muối, hoặc không có đường nâng cấp khi thuật toán cũ đi |
| So sánh chống rò thời gian | So sánh chuỗi thông thường → rò thông tin qua thời gian phản hồi |
| Khoá tài khoản sau nhiều lần sai | Đếm sai chỗ, hoặc quên reset bộ đếm |
| Token đặt lại mật khẩu / xác nhận email | Token đoán được, không hết hạn, hoặc dùng lại được |
| Chuẩn hoá tên đăng nhập và email | Hai tài khoản khác nhau chỉ khác hoa thường |

📖 [`../../adr/0004-giu-aspnet-identity.md`](../../adr/0004-giu-aspnet-identity.md)

### 2.2 Dùng phần nào, bỏ phần nào

| Phần của Identity | Dùng? | Ghi chú |
| --- | --- | --- |
| Băm và kiểm mật khẩu | ✅ | Lý do chính để giữ Identity |
| Khoá tài khoản, đếm lần sai | ✅ | |
| Sinh và kiểm token (đặt lại mật khẩu, xác nhận email) | ✅ | |
| Chuẩn hoá tên đăng nhập / email, ràng buộc duy nhất | ✅ | |
| Dấu đồng thời trên bản ghi người dùng | ✅ | Dùng làm token chống ghi đè cho chính bảng người dùng — xem [`06-concurrency-control.md`](06-concurrency-control.md) |
| Phiên cookie | ✅ | Identity lõi ở `Core.Infrastructure`; cookie scheme và đăng nhập/đăng xuất ở `Core.Web` — [`../../adr/0026-ranh-gioi-identity-va-cookie.md`](../../adr/0026-ranh-gioi-identity-va-cookie.md) |
| **Vai trò của Identity dùng làm cơ chế phân quyền** | ❌ | Bảng vai trò của Identity được dùng như **kho dữ liệu vai trò**, nhưng việc kiểm quyền **không** kiểm tên vai trò — xem §3 |
| Giao diện dựng sẵn của Identity | ❌ | FE tự làm màn hình, xem [`../fe/07-auth-identity.md`](../fe/07-auth-identity.md) |
| Đăng nhập qua nhà cung cấp ngoài | ❌ ở v1 | Thêm khi có yêu cầu thật |
| Xác thực nhiều yếu tố | ❌ ở v1 | Identity có sẵn cơ chế; bật khi có yêu cầu |

### 2.3 Khoanh vùng — kiểu Identity không được rò ra ngoài hạ tầng

**Luật:** các kiểu người dùng và vai trò của Identity chỉ xuất hiện trong `Core.Infrastructure`. Luật S5 ở [`../../RULES.md`](../../RULES.md) canh việc này bằng ArchTest `IdentityTypes_MustNotLeak_OutsideInfrastructure`.

**Vì sao khoanh vùng mà không phải bỏ hẳn:**

- Bỏ hẳn = tự viết lại §2.1. Không.
- Không khoanh vùng = tầng Application phụ thuộc một thư viện hạ tầng. Từ đó handler không unit-test được nếu không dựng cả bộ Identity, và luật A2 ở [`../../RULES.md`](../../RULES.md) bị vi phạm.

**Hình dạng seam.** Application chỉ thấy interface — một dịch vụ danh tính với các thao tác nghiệp vụ cần dùng.

> 📖 Chữ ký seam danh tính: đọc [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §7. Ranh giới giữa Identity lõi và cookie: [`../../adr/0026-ranh-gioi-identity-va-cookie.md`](../../adr/0026-ranh-gioi-identity-va-cookie.md).

Điểm cần chú ý: thao tác của seam trả `Result`, **không** ném exception và **không** trả kiểu kết quả của Identity. Nếu kiểu kết quả của Identity đi ra tới Application thì seam đã thủng — đúng loại vi phạm mà nhìn code thì thấy vẫn "có interface".

Mã lỗi do Identity sinh (mật khẩu yếu, email trùng) phải được **dịch sang mã lỗi của catalog** trước khi rời `Core.Infrastructure`, và những lỗi thuộc về một ô nhập cụ thể thì đi ra dưới dạng lỗi-theo-ô. Xem [`16-i18n-va-ma-loi.md`](16-i18n-va-ma-loi.md).

---

## 3. Phân quyền theo permission — không có hằng số vai trò trong Core

> 📖 **Luật gốc *"trong `Core.*` không tồn tại hằng số role nào"* và bốn hệ quả của việc vi phạm nó: đọc [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §4.1.**

### 3.1 Mô hình

```text
người dùng ──(N-N)── vai trò ──(N-N)── quyền
                                          │
                                    khoá dạng "tài-nguyên.hành-động"
```

- **Vai trò là dữ liệu.** Tạo, sửa, xoá bằng màn hình quản trị. Core không biết tên nào tồn tại.
- **Quyền là danh mục.** Mỗi quyền có một khoá ổn định, không đổi theo thời gian, vì nó xuất hiện trong dữ liệu phân quyền.
- **Người dùng không được gán quyền trực tiếp** ở v1. Gán trực tiếp tạo ra một đường thứ hai để có quyền, và mọi câu hỏi "vì sao người này vào được màn này" phải kiểm hai chỗ. Thêm khi có nhu cầu thật, và khi thêm thì phải có màn hình trả lời được câu hỏi *"quyền này người dùng có từ đâu"*.

### 3.2 Khuôn khoá quyền

> 📖 **Danh mục khoá của Core — tập đầy đủ, kèm `resource_key`, `action` và ý nghĩa từng
> khoá: đọc [`../../database/schema-core.md`](../../database/schema-core.md) §5.2.**
>
> Mục này **cố ý không liệt kê lại.** Một bộ khoá thứ hai lệch khỏi bộ seed thì code chặn theo
> bộ này, dữ liệu nạp bộ kia, và vì phân quyền là deny-by-default, kết quả là **403 cho mọi
> người, kể cả tài khoản đủ quyền**. Xem
> [`../../OWNERSHIP.md`](../../OWNERSHIP.md) §1.

Quy tắc:

| Quy tắc | Vì sao |
| --- | --- |
| Khoá **không mang tên vai trò** | Vai trò là dữ liệu; khoá là danh mục cố định |
| Khoá **không mang tên nghiệp vụ của một dự án** trong phần Core | Vi phạm luật A5 ở [`../../RULES.md`](../../RULES.md) — Core biết tên nghiệp vụ là Core không mang đi được |
| Một khoá đã phát hành thì **không đổi tên** | Đổi tên khoá làm dữ liệu phân quyền cũ trỏ vào hư không, và hỏng **im lặng**: người dùng chỉ mất quyền chứ không có lỗi nào |
| Module tự khai khoá của mình | Module đăng ký danh mục quyền của nó khi lắp vào Host |

### 3.3 Ma trận quyền theo tài nguyên

Màn hình phân quyền hiển thị một ma trận: hàng là tài nguyên, cột là hành động, ô là dấu tích cho một vai trò đang chọn.

Ba điều làm ma trận này không vỡ khi hệ lớn lên:

1. **Danh mục quyền do server cấp**, FE không hardcode. FE hardcode thì thêm quyền ở module mới là phải sửa FE.
2. **Ghi cả tập, không ghi từng ô.** Người dùng tích nhiều ô rồi bấm lưu một lần; server nhận toàn bộ tập quyền của vai trò đó và thay thế. Ghi từng ô tạo ra trạng thái nửa vời khi mạng rớt giữa chừng.
3. **Có token phiên bản ở cấp tập hợp** để chống ghi đè khi hai người cùng mở màn phân quyền. Xem [`06-concurrency-control.md`](06-concurrency-control.md).

### 3.4 Kiểm quyền ở đâu

| Tầng | Kiểm gì | Ghi chú |
| --- | --- | --- |
| **Biên HTTP** | Endpoint này đòi quyền nào | Khai bằng thuộc tính trên action; cách viết ở [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) |
| **Handler** | Quyền phụ thuộc dữ liệu ("chỉ sửa được bản ghi của đơn vị mình") | Không khai được ở biên vì phải đọc dữ liệu mới biết |
| **Menu** | Lọc mục menu theo quyền | Chỉ để hiển thị. **Không phải hàng rào** — ẩn menu không ngăn ai gọi thẳng endpoint |

**Điểm dễ sai nhất:** coi việc ẩn menu là đã phân quyền xong. Menu là trải nghiệm, endpoint mới là hàng rào.

### 3.5 Seed vai trò và quyền cho một dự án mới

Chia đôi trách nhiệm:

| Ai | Seed gì |
| --- | --- |
| **Core** | Danh mục quyền của chính Core (quản trị người dùng, vai trò, phân quyền, menu). Khoá khai trong code qua `IPermissionCatalogSource`; dòng vào DB bằng migration của schema `core`. Tiến trình ứng dụng **không** ghi danh mục — [`13-core-data-migration.md`](13-core-data-migration.md) §5.1 |
| **Dự án** | Bộ vai trò mặc định của dự án đó và ánh xạ vai trò → quyền. Đây là dữ liệu **của dự án**, không thuộc Core |

Nguyên tắc cho việc seed danh mục quyền: **thêm được, không xoá tự động**. Một khoá quyền biến mất khỏi danh mục thì mọi dòng phân quyền trỏ tới nó thành rác âm thầm. Khi cần bỏ một quyền, đánh dấu ngừng dùng và dọn bằng một bước có chủ đích. Chi tiết ở [`13-core-data-migration.md`](13-core-data-migration.md).

### 3.6 Tài khoản bootstrap lấy toàn quyền bằng cách nào

Đây là câu hỏi thật mà mô hình "không có vai trò cứng" bắt buộc phải trả lời: **cài mới, DB trống, chưa có vai trò nào, chưa ai được gán quyền — vậy ai đăng nhập được để tạo vai trò đầu tiên?**

Ba phương án, và vì sao chọn phương án thứ ba:

| Phương án | Vì sao loại / chọn |
| --- | --- |
| **Seed một vai trò tên "quản trị" trong Core** | ❌ Loại. Đó chính là hằng số vai trò, chỉ đổi chỗ từ code sang dữ liệu của Core. Luật S1 cấm |
| **Seed một vai trò được gán sẵn toàn bộ quyền hiện có** | ❌ Loại. "Toàn bộ quyền hiện có" là ảnh chụp **tại lúc seed**. Module lắp vào sau mang theo quyền mới, và vai trò bootstrap không có chúng — người quản trị bị khoá khỏi chính màn hình mình vừa cài |
| **Một cờ trên bản ghi người dùng, được bộ kiểm quyền hiểu là "bỏ qua kiểm"** | ✅ Chọn. Không mang tên nghiệp vụ, không phải vai trò, và **không phụ thuộc danh mục quyền tại thời điểm nào** |

**Hình dạng phương án đã chọn:**

- Bảng người dùng của Core có một cột cờ. Bộ kiểm quyền có đúng **một** nhánh: cờ bật thì **tập quyền hiệu lực** là toàn bộ danh mục quyền hiện có, nên mọi câu hỏi quyền trả lời có.
- Cột mang cờ là `core.app_user.has_permission_bypass` ([`../../database/schema-core.md`](../../database/schema-core.md) §4.1). Nó **không phải** `is_system_operator` — hai cột đó là hai vai ngược nhau và loại trừ nhau bằng ràng buộc ở database (luật M11).
- **Không đường nào đặt cờ lên một tài khoản ĐÃ TỒN TẠI.** Cờ chỉ sinh ra **cùng lúc** với chính tài khoản mang nó — bởi lệnh bootstrap lúc cài đặt, bởi `POST /system/tenants` khi tạo một đơn vị mới, hoặc bởi `POST /system/tenants/{id}/admins` khi tạo thêm tài khoản quản trị ([`../../contracts/tenants.md`](../../contracts/tenants.md) §2, §6); cả ba đi qua service tạo đơn vị dùng chung ([`../../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../../adr/0023-dich-vu-tao-don-vi-dung-chung.md)). Luật **M12**.
- Lệnh bootstrap tạo đơn vị hệ thống, đơn vị nghiệp vụ đầu tiên và hai tài khoản: `superadmin` mang `is_system_operator` **và** cờ bắt buộc đổi mật khẩu ở đơn vị hệ thống; `admin` mang cờ này cùng cờ **bắt buộc đổi mật khẩu ở lần đăng nhập đầu** ở đơn vị nghiệp vụ đầu tiên ([`../../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../../adr/0023-dich-vu-tao-don-vi-dung-chung.md)). Việc tạo được ghi vào nhật ký kiểm toán. Máy dev và bản thật dùng cùng lệnh đó — [`17-multi-tenant.md`](17-multi-tenant.md) §10.
- Phản hồi phiên lấy `permissions` từ tập quyền hiệu lực mà **chính bộ kiểm quyền** cấp — nên tài khoản mang cờ nhận toàn bộ danh mục mà handler phiên không đọc cờ, và luật *"một chỗ"* ở bảng rủi ro dưới vẫn giữ; FE không có nhánh riêng cho cờ — [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §4.3 · [`../../contracts/auth.md`](../../contracts/auth.md) §5.
- Sau khi dựng xong bộ vai trò thật, tài khoản mang cờ **tự từ bỏ** cờ qua endpoint từ bỏ ở [`../../contracts/profile.md`](../../contracts/profile.md): một chiều, và bị từ chối khi đơn vị chưa có tài khoản nào khác giữ `core.permission.write` qua vai trò — chống tự khoá khỏi màn phân quyền. Luật M12 cấm **bật** cờ trên tài khoản đã tồn tại, không cấm tắt.

**Rủi ro của phương án này và cách canh:**

| Rủi ro | Cách canh |
| --- | --- |
| Cờ trở thành cửa sau ai cũng dùng | Không có endpoint đặt cờ lên tài khoản đã có. Mọi lần cờ được đặt đều vào nhật ký kiểm toán |
| Cờ bị nhân bản thành nhiều điều kiện `if` rải rác | Chỉ được đọc ở **đúng một chỗ** trong bộ kiểm quyền. Ngoài chỗ đó, việc đọc cờ là finding |
| Cờ trên thực tế là "vai trò trá hình" | Phép thử: nó có mang **tên** không? Có phải dữ liệu người dùng tạo được không? Không và không → nó là thuộc tính của cơ chế phân quyền, không phải vai trò |
| Quên tắt cờ sau khi cài xong | Màn hồ sơ hiện `NoticeBanner` nhắc từ bỏ cờ chừng nào tài khoản còn mang nó ([`../../Design/Screens/03-ho-so-ca-nhan.md`](../../Design/Screens/03-ho-so-ca-nhan.md)) |

---

## 4. Mật khẩu, khoá tài khoản, đổi mật khẩu lần đầu

### 4.1 Chính sách mật khẩu

Chính sách phải nằm ở **cấu hình**, không rải trong code — vì mỗi dự án có yêu cầu tuân thủ khác nhau, và vì FE cần hiển thị đúng yêu cầu đó cho người dùng.

| Chiều | Khuyến nghị cho hệ tầm trung | Ghi chú |
| --- | --- | --- |
| Độ dài tối thiểu | Ưu tiên **độ dài** hơn độ phức tạp | Bắt buộc đủ loại ký tự đẩy người dùng tới các biến thể đoán được |
| Chặn mật khẩu phổ biến | Nên có | Hiệu quả cao hơn mọi quy tắc về ký tự đặc biệt |
| Bắt đổi định kỳ | **Không**, trừ khi quy định bắt buộc | Ép đổi định kỳ làm mật khẩu yếu đi theo thời gian |
| Hiển thị yêu cầu ngay khi nhập | Có | Kiểm ở cả FE và BE; **BE là nơi quyết định**, FE chỉ hiển thị |

**Quan trọng:** BE trả về **mã lỗi**, không trả câu tiếng Việt. Câu chữ do FE ghép. Xem [`16-i18n-va-ma-loi.md`](16-i18n-va-ma-loi.md) — luật R8 ở [`../../RULES.md`](../../RULES.md) canh việc này.

**Chính sách giống nhau ở mọi môi trường — luật S9.** Section cấu hình `Core:Identity:Password` chỉ nằm ở `appsettings.json`. Một test quét mọi tệp `appsettings.*.json` và đỏ khi section đó xuất hiện ở tệp theo môi trường; tên test ở cột *Ép bằng gì* của [`../../RULES.md`](../../RULES.md).

### 4.2 Khoá tài khoản — định nghĩa gốc

Khoá sau một số lần sai mật khẩu **liên tiếp**, tự mở sau một khoảng thời gian.

| Tham số | Mặc định | Khoá cấu hình |
| --- | --- | --- |
| Số lần sai mật khẩu liên tiếp thì khoá | **5** | `Core:Identity:Lockout:MaxFailedAttempts` |
| Thời gian khoá | **15 phút** | `Core:Identity:Lockout:DurationMinutes` |

Hai khoá nạp vào tuỳ chọn khoá tài khoản của Identity (`LockoutOptions.MaxFailedAccessAttempts`,
`LockoutOptions.DefaultLockoutTimeSpan`). File khác cần hai giá trị này thì trỏ về đây, không chép.

**Thứ tự kiểm khi đăng nhập.** Chạy sau khi đơn vị đã nạp (§6.2) và lượt thử đã tính vào hạn mức theo
tài khoản ([`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §6.5):

| # | Tình huống | Làm gì | Trả |
| --- | --- | --- | --- |
| 1 | Sai mật khẩu, tài khoản **không** đang khoá | Tính một lần sai (`UserManager.AccessFailedAsync`); đủ ngưỡng thì Identity đặt mốc khoá | `CORE.AUTH.INVALID_CREDENTIALS` |
| 2 | Sai mật khẩu, tài khoản **đang** khoá | **Không** tính lần sai | `CORE.AUTH.INVALID_CREDENTIALS` — cùng mã với ca 1 |
| 3 | Đúng mật khẩu, tài khoản đang khoá | — | `CORE.AUTH.LOCKED_OUT` |
| 4 | Đúng mật khẩu, không khoá | Xoá bộ đếm lần sai (`UserManager.ResetAccessFailedCountAsync`) | Đi tiếp — seam trả `CredentialCheck` ([`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §7.1); cờ đổi mật khẩu ở §4.3 |

Ba điểm của bảng không đổi được:

1. **Sai mật khẩu luôn ra `CORE.AUTH.INVALID_CREDENTIALS`, kể cả khi đang khoá.** `CORE.AUTH.LOCKED_OUT`
   chỉ trả cho người đã gõ **đúng** mật khẩu — người không biết mật khẩu không phân biệt được tài khoản
   bị khoá với tài khoản không tồn tại.
2. **Ca 2 không tính lần sai.** Đủ ngưỡng thì `AccessFailedAsync` **ghi đè** mốc khoá bằng *thời điểm
   hiện tại + thời gian khoá*. Tài khoản do quản trị khoá mang mốc khoá ở xa trong tương lai
   ([`../../database/schema-core.md`](../../database/schema-core.md) §4.1); tính lần sai trên nó thì vài lần gõ
   sai của bất kỳ ai **rút lệnh khoá của quản trị xuống bằng thời gian khoá tự động**.
3. **Ca 4 phải xoá bộ đếm.** Thiếu bước này thì "liên tiếp" thành "cộng dồn": người gõ sai lác đác qua
   nhiều ngày vẫn bị khoá.

**Cái giá của điểm 1, ghi thẳng:** trong lúc khoá, phản hồi vẫn cho biết mật khẩu vừa thử đúng hay sai.
Khoá tài khoản vì vậy **không** làm chậm việc dò mật khẩu — nó chỉ chặn việc **dùng** mật khẩu dò được
cho tới khi hết khoá. Việc làm chậm dò thuộc hạn mức theo tài khoản và các hàng rào theo IP
([`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §6.1).

Ba điểm dễ sai ở tầng thiết kế:

1. **Khoá theo tài khoản là chưa đủ.** Kẻ tấn công thử một mật khẩu phổ biến trên hàng nghìn tài khoản thì không tài khoản nào chạm ngưỡng. Cần thêm giới hạn tần suất theo IP — xem [`09-security-beyond-auth.md`](09-security-beyond-auth.md).
2. **Khoá theo tài khoản mở đường cho tấn công từ chối dịch vụ nhắm vào một người.** Biết tên đăng nhập là khoá được người đó. Giảm nhẹ bằng cách mở khoá tự động sau một khoảng, không khoá vĩnh viễn.
3. **Thông điệp trả về không được tiết lộ tài khoản có tồn tại hay không.** Xem [`09-security-beyond-auth.md`](09-security-beyond-auth.md) §chống dò tài khoản.

### 4.3 Bắt buộc đổi mật khẩu lần đầu

Tài khoản do quản trị viên tạo mang mật khẩu tạm. Cần một cờ *"phải đổi mật khẩu"*:

- Đăng nhập vẫn thành công (nếu không thì người dùng không đổi được).
- Nhưng **mọi endpoint khác** trả về lỗi có mã riêng cho tới khi mật khẩu được đổi.

> 📖 Allowlist đường đi qua được trong trạng thái này: đọc [`../../contracts/auth.md`](../../contracts/auth.md) §1.2
- FE nhận mã đó thì điều hướng sang màn đổi mật khẩu và không cho thoát ra.

**Bẫy:** làm việc này chỉ bằng cách điều hướng ở FE. Người dùng gọi thẳng endpoint là bỏ qua được. Cờ phải được kiểm ở server.

### 4.4 Quên mật khẩu, đặt lại hộ, khôi phục

| Tình huống | Đường | Nguồn |
| --- | --- | --- |
| Người dùng quên mật khẩu | **Không có luồng tự phục vụ ở v1.** Người dùng liên hệ quản trị đơn vị của mình | [`../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) |
| Quản trị đơn vị đặt lại hộ | Chỉ cho người trong đơn vị mình; quản trị tự gõ mật khẩu tạm | [`../../contracts/users.md`](../../contracts/users.md) |
| Quản trị của một đơn vị mất quyền truy cập | Tài khoản vận hành khôi phục mật khẩu, **chỉ** cho tài khoản mang cờ bypass hoặc đang giữ một vai trò `is_system` của đơn vị đó, không trả dữ liệu nào | [`../../contracts/tenants.md`](../../contracts/tenants.md) · ADR-0029 |
| Chính tài khoản vận hành mất quyền truy cập | Lệnh chạy tay trên máy chủ | [`../../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../../adr/0023-dich-vu-tao-don-vi-dung-chung.md) |

---

## 5. Đăng xuất và thu hồi phiên

| Tình huống | Phải xảy ra gì |
| --- | --- |
| Người dùng bấm đăng xuất | Xoá cookie phiên; **và** làm mất hiệu lực phiếu ở server nếu có kho phiếu |
| Quản trị viên khoá một tài khoản | Phiên đang mở của tài khoản đó phải trượt ở request kế tiếp, không đợi hết hạn |
| Quản trị đặt lại mật khẩu hộ, hoặc tài khoản vận hành khôi phục mật khẩu quản trị đơn vị | Phiên đang mở của tài khoản đích phải trượt ở request kế tiếp, không đợi hết hạn |
| Người dùng đổi mật khẩu | Mọi phiên khác của chính người đó nên bị đẩy ra |
| Quyền của một vai trò thay đổi | Quyền phải có hiệu lực ở request kế tiếp |

**Khoá, đặt lại mật khẩu và đổi mật khẩu đạt được mà không cần kho phiếu:** phiếu mang theo một dấu đồng thời của bản ghi người dùng — security stamp; mỗi thao tác trên đổi dấu đó, và mỗi request kiểm dấu còn khớp DB không. Đây là một lần đọc thêm mỗi request — chi phí thật, nhưng nó là cái giá của "thu hồi tức thì", tức là chính lý do chọn cookie ở §1.3. Thao tác nào đổi dấu và cách đổi: [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §7.4.

**Dòng về quyền không đi qua dấu đó:** kiểm quyền đọc tập quyền hiệu lực từ DB ở mỗi request, không từ phiếu — [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §4.3.

Nếu lần đọc đó trở thành điểm nghẽn (đã **đo**, không phải đoán), đó là ca dùng cache đầu tiên đáng cân nhắc — xem [`11-performance-caching.md`](11-performance-caching.md).

---

## 6. Tenant trong luồng xác thực

> Nhiều đơn vị hành chính độc lập dùng chung một bản cài
> ([`../../adr/0013-multi-tenant.md`](../../adr/0013-multi-tenant.md)). File chủ về thi công:
> [`17-multi-tenant.md`](17-multi-tenant.md). Mục này chỉ ghi phần **chạm tới xác thực**.

### 6.1 `TenantId` vào phiếu xác thực lúc đăng nhập

Đăng nhập thành công → phiếu mang thêm một claim `TenantId`, cạnh danh tính người dùng. Từ đó
`ITenantContext.TenantId` đọc được ở mọi request, và bộ lọc truy vấn toàn cục dùng chính giá trị
đó. Chữ ký hai seam `ICurrentUser` và `ITenantContext`:
[`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §1.1. **Không** có đường nào khác: `TenantId` không bao giờ đến từ route, query, body hay header —
client tự khai tenant là client tự cấp quyền (luật M2, [`../../RULES.md`](../../RULES.md) §9).

Vì tenant nằm trong phiếu và **một tài khoản thuộc đúng một tenant**, mô hình này không có màn
chọn tenant và không có đường đổi tenant giữa phiên. Đổi tenant = đăng xuất và đăng nhập bằng
tài khoản khác.

### 6.2 Tìm người dùng nào khi email trùng giữa hai đơn vị

Đây là hệ quả thi công nặng nhất của multi-tenant lên Identity: **tên đăng nhập và email không
còn duy nhất toàn hệ**, mà duy nhất theo cặp `(TenantId, giá trị chuẩn hoá)`. Hai cơ quan hoàn
toàn có thể cùng có `admin`, cùng có `vanthu@…`. `UserManager.FindByNameAsync` tra theo tên
chuẩn hoá, nên **một mình nó không còn định danh được một người**.

Cách giải đã chốt: **một ô "mã đơn vị" trên chính form đăng nhập**, cùng cấp với ô tên đăng
nhập. Đó không phải màn chọn tenant — nó điền **trước** khi xác thực, không phải chọn **sau**
khi xác thực. Hai phương án khác (tên đăng nhập duy nhất toàn hệ; suy tenant từ tên miền) đã
được cân nhắc và loại — lý do ở [`17-multi-tenant.md`](17-multi-tenant.md) §11.

**Thứ tự bắt buộc, và bước 3 là bước dễ bỏ sót nhất:**

| # | Bước | Nếu làm sai |
| --- | --- | --- |
| 1 | Nhận `(mã đơn vị, tên đăng nhập, mật khẩu)` | — |
| 2 | Tra tenant theo mã đơn vị | — |
| 3 | **Nạp `TenantId` vừa tra được vào ngữ cảnh của request này** | Bỏ qua bước này thì `UserManager` chạy khi ngữ cảnh tenant còn rỗng: hoặc mọi người đều "sai mật khẩu", hoặc — tệ hơn nhiều — truy vấn chạy như thể không có bộ lọc |
| 4 | Gọi `UserManager`: kiểm mật khẩu rồi khoá tài khoản theo thứ tự ở §4.2, rồi cờ đổi mật khẩu lần đầu (§4.3) | Đảo mật khẩu và khoá thì tài khoản bị khoá lộ ra với người không biết mật khẩu |
| 5 | Phát phiếu mang claim `TenantId` | — |

### 6.3 Đơn vị ngưng hoạt động thì chặn ở bước nào

**Chặn ở hai chỗ, không phải một** — và thiếu chỗ nào cũng hỏng theo một kiểu riêng:

| Chặn ở | Bắt ca nào | Thiếu nó thì |
| --- | --- | --- |
| **Bước 2 của luồng đăng nhập** (ngay sau khi tra tenant) | Người chưa đăng nhập | Màn đăng nhập trả một lỗi chung chung thay vì một câu trả lời rõ ràng |
| **Bước dựng danh tính cho mỗi request** | Người **đang** có phiên mở | Phiên đang chạy sống tiếp tới khi cookie hết hạn — mất đúng thứ "thu hồi tức thì" là lý do chọn cookie ở §1.3 |

Chỗ chặn thứ hai nằm cùng đường với việc kiểm dấu đồng thời của bản ghi người dùng ở §5, nên nó
không thêm một lần đọc mới — nó thêm một điều kiện vào lần đọc đã có.

> **Không phân biệt thông điệp.** Mã đơn vị không tồn tại, đơn vị đã ngưng hoạt động, sai tên
> đăng nhập, sai mật khẩu — bốn ca trả về **cùng một** câu. Phân biệt chúng là mở đường dò: một
> người ngoài thử lần lượt các mã đơn vị và biết được cơ quan nào đang dùng hệ thống. Cùng họ
> với chống dò tài khoản ở [`09-security-beyond-auth.md`](09-security-beyond-auth.md).

### 6.4 Cờ bootstrap KHÔNG vượt qua ranh giới tenant

Cờ ở §3.6 giữ nguyên cơ chế nhưng phạm vi hẹp hơn trực giác: nó cho qua mọi câu hỏi **quyền**,
nó **không** gỡ bộ lọc tenant. Một tài khoản mang cờ vẫn chỉ thấy dữ liệu của đơn vị mình.

Trong mô hình này **không tồn tại** tài khoản nhìn được dữ liệu của mọi đơn vị. Việc quản trị các
tenant — tạo, ngưng hoạt động — đi qua khu quản trị hệ thống, bằng tài khoản vận hành thuộc đơn vị
hệ thống: nó thấy danh sách đơn vị, không thấy dữ liệu bên trong đơn vị nào —
[`17-multi-tenant.md`](17-multi-tenant.md) §11.3.

---

## 7. §Áp dụng — Core này làm gì, cố ý chưa làm gì

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Phiên cookie, `HttpOnly` | ✅ sẽ có | |
| Antiforgery hai lớp | ✅ sẽ có | [`09-security-beyond-auth.md`](09-security-beyond-auth.md) |
| Kho khoá dùng chung | ✅ sẽ có | Bắt buộc trước instance thứ hai |
| Phân quyền theo permission | ✅ sẽ có | Không hằng số vai trò |
| Cờ bootstrap | ✅ sẽ có | §3.6. **Không** gỡ bộ lọc tenant — §6.4 |
| Bắt đổi mật khẩu lần đầu | ✅ sẽ có | Kiểm ở server |
| Claim `TenantId` trong phiếu xác thực | ✅ sẽ có | §6.1. Nguồn duy nhất của tenant ở mọi request |
| Ô mã đơn vị trên form đăng nhập | ✅ sẽ có | §6.2. **Không** phải màn chọn tenant |
| Đặt lại mật khẩu hộ trong đơn vị; khôi phục quản trị đơn vị qua khu hệ thống | ✅ sẽ có | §4.4 |
| Từ bỏ cờ bypass, một chiều, chống tự khoá | ✅ sẽ có | §3.6 |
| **Quên mật khẩu tự phục vụ** | ❌ chưa | §4.4. Ngoài v1 — [`../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) |
| Tên đăng nhập / email duy nhất theo cặp với tenant | ✅ sẽ có | §6.2 · [`../../database/schema-core.md`](../../database/schema-core.md) §4.1 |
| Chặn đơn vị ngưng hoạt động ở **hai** chỗ | ✅ sẽ có | §6.3 |
| **Kho phiếu phía server** | ❌ chưa | Thu hồi đạt được qua dấu đồng thời; kho phiếu chỉ cần khi muốn quản lý phiên theo từng thiết bị |
| **Xác thực nhiều yếu tố** | ❌ chưa | Identity có sẵn cơ chế. Bật khi có yêu cầu tuân thủ |
| **Đăng nhập qua nhà cung cấp ngoài / SSO** | ❌ chưa | Khi cần, dùng thư viện chuẩn — không tự phát token |
| **Gán quyền trực tiếp cho người dùng** | ❌ chưa | Tạo đường thứ hai để có quyền. Chỉ thêm kèm màn hình truy vết nguồn quyền |
| **JWT** | ❌ chưa | Ngưỡng đổi ở §1.6. Đổi thì phải có ADR |
| **Màn chọn tenant / đổi tenant giữa phiên** | ❌ loại, không hoãn `K01` | Một tài khoản thuộc đúng một tenant. Muốn lật thì viết ADR mới lật [`../../adr/0013-multi-tenant.md`](../../adr/0013-multi-tenant.md) |
| **Tài khoản nhìn được mọi tenant** | ❌ loại, không hoãn `K02` | §6.4 |
| **Nới chính sách mật khẩu theo môi trường** | ❌ **loại, không hoãn** `K51` | Mọi hình thức, kể cả chỉ hạ độ dài tối thiểu ở dev. Nhánh từ chối mật khẩu yếu sẽ **không bao giờ chạy trên máy lập trình viên**, nên lỗi ở nhánh đó chỉ lộ lần đầu trên bản chạy thật. Luật **S9** |

Một finding dạng *"nên dùng JWT"* hoặc *"thiếu MFA"* chỉ hợp lệ khi kèm bằng chứng rằng điều kiện ở cột ghi chú đã xảy ra.
