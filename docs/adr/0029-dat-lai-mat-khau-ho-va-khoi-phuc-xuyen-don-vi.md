---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0029 — Quên mật khẩu tự phục vụ ngoài v1; đặt lại mật khẩu hộ chỉ trong đơn vị; người vận hành chỉ khôi phục được tài khoản quản trị của một đơn vị; thao tác xuyên đơn vị ghi hai dòng nhật ký

> **Trạng thái:** Đã chấp nhận (2026-09-14)
>
> **Bổ sung [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md).** Ranh giới *"tài khoản vận hành không thấy dữ liệu nghiệp vụ"* giữ nguyên. ADR này thêm **một** thao tác hẹp mà tài khoản vận hành làm được lên một đơn vị, và chốt chỗ ghi nhật ký của thao tác xuyên đơn vị.

## Bối cảnh

Bốn câu hỏi dưới đây gắn với nhau, vì cùng trả lời *"người mất quyền truy cập lấy lại bằng đường nào, và ai thấy việc đó"*. Số dòng là số dòng ngày 2026-09-14.

**Tự phục vụ chưa thi công được.** Luồng quên mật khẩu cần một kênh gửi mà không tài liệu nào khai (`docs/luong/D3-quen-mat-khau.md:71`), thời hạn token chưa khai (`docs/luong/D3-quen-mat-khau.md:72`), và chưa nói có đá phiên đang mở không (`docs/luong/D3-quen-mat-khau.md:73`). Kênh gửi chết thì không ai thấy gì (`docs/luong/D3-quen-mat-khau.md:59`). Hai endpoint của luồng nằm trong allowlist ẩn danh (`docs/quy-uoc/be-api-controller.md:403`, `docs/quy-uoc/be-api-controller.md:405`) — hai cửa không cần đăng nhập, phải chống dò tài khoản và chống gửi thư hàng loạt. Với một hệ nội bộ cơ quan, *"có máy chủ thư"* là một giả định lớn và là một hệ thống ngoài phải vận hành.

**Đặt lại hộ chưa có luật cho đích.** Đặt lại được mật khẩu của người khác là đặt lại được mật khẩu của **quản trị khác** (`docs/luong/D4-quan-tri-dat-lai-mat-khau.md:49`). Các luật bảo vệ tài khoản quản trị (`docs/contracts/users.md:30`) canh gán vai trò và khoá tài khoản, không canh đích của thao tác đặt lại. Luồng đặt lại hộ còn trỏ tới luật *"quản trị cuối cùng"* (`docs/luong/D4-quan-tri-dat-lai-mat-khau.md:55`) — luật đó đã bị loại (`docs/contracts/users.md:146`).

**Một đơn vị có thể tự khoá mình ra ngoài.** Tài khoản mang `has_permission_bypass` của một đơn vị quên mật khẩu, và đơn vị chưa có quản trị nào khác. Không ai trong đơn vị đặt lại được; tài khoản vận hành chỉ thấy danh sách đơn vị (`docs/adr/0017-khu-quan-tri-he-thong.md:23`); sửa database bằng tay không dựng được mật khẩu băm (`docs/luong/V1-cai-dat-lan-dau.md:63`). Đường còn lại là ai đó viết code tạm chạy trên máy chủ — không nhật ký, không ai kiểm.

**Nhật ký của thao tác xuyên đơn vị chưa có chỗ ghi.** Người thao tác thuộc đơn vị hệ thống, đối tượng thuộc đơn vị khác; ghi vào đơn vị nào thì chưa file nào chốt (`docs/luong/N6-nhat-ky-kiem-toan.md:68`, `docs/luong/V2-tao-don-vi-moi.md:67`). Lược đồ nhật ký cho phép người thực hiện thuộc đơn vị khác, vì nó không có khoá ngoại tới bảng người dùng (`docs/database/schema-core.md:930`).

## Quyết định

### 1. Quên mật khẩu tự phục vụ nằm ngoài v1

Luồng `D3` và hai endpoint quên mật khẩu / đặt lại bằng token ở [`../contracts/auth.md`](../contracts/auth.md) §8–§9 **giữ lại, đánh dấu ngoài v1** — không xoá. Ở v1 hai endpoint đó không được thi công và không nằm trong allowlist ẩn danh. Người quên mật khẩu liên hệ quản trị đơn vị bằng kênh ngoài hệ thống; quản trị đặt lại hộ.

Đây là quyết định **hoãn**, không phải **loại** — điều kiện kích hoạt ở cuối ADR.

### 2. Quản trị đơn vị đặt lại hộ — chỉ trong đơn vị mình, thêm Luật 5

Đường giữ nguyên: luồng `D4`, quyền `core.user.reset-password`, người gọi và đích cùng đơn vị. Card [`../contracts/users.md`](../contracts/users.md) §2 thêm **Luật 5** cho thao tác đặt lại mật khẩu:

| Ca | `type` | HTTP | Vì sao |
| --- | --- | ---: | --- |
| Tập quyền hiệu lực của đích **vượt** tập quyền của người gọi | `Forbidden` | 403 | Đặt lại mật khẩu là chiếm được phiên của đích, tức chiếm được quyền của đích. Cùng nguyên tắc với Luật 1 — không leo thang đặc quyền |
| Đích mang `has_permission_bypass` | `Forbidden` | 403 | Tài khoản đó có mọi quyền; không người gọi nào khác trong đơn vị bao trùm được nó |
| Đích là chính người gọi | `BusinessRule` | 422 | Người gọi đủ tư cách, thao tác thì không hợp lệ: đổi mật khẩu của chính mình đi đường hồ sơ hoặc `D2` — đường đòi mật khẩu hiện tại |

Mã lỗi cụ thể do card khai. Luật *"quản trị cuối cùng"* không quay lại.

### 3. Người vận hành khôi phục tài khoản quản trị của một đơn vị — và chỉ việc đó

Tài khoản vận hành **không** có đường đặt lại mật khẩu cho người dùng bất kỳ. Nó có đúng một thao tác:

`POST /api/v1/core/system/tenants/{id}/recovery-reset-password`, thân `{ userName, tempPassword }`

| Quy tắc | Vì sao |
| --- | --- |
| Chỉ nhận đích **thuộc đơn vị `{id}`** và **mang `has_permission_bypass` hoặc đang giữ một vai trò `is_system` của đơn vị đó** | Đó là hai đường quản trị mà một đơn vị có từ lúc được tạo: cờ của tài khoản quản trị đầu tiên (luật M12), và vai trò hệ thống mà nguồn seed khai — vai trò không xoá được, không bị thu hồi `core.permission.write` ([`../database/schema-core.md`](../database/schema-core.md) §4.2). Khôi phục một trong hai là trả lại lối vào quản trị cho đơn vị, không phải chạm vào một người dùng nghiệp vụ |
| **Không trả dữ liệu** trong phản hồi thành công | Tài khoản vận hành không được biết gì về người dùng của đơn vị |
| *"Không có tài khoản đó"* và *"có nhưng không đủ điều kiện"* gộp vào **một mã 422** | Tách hai ca thì endpoint thành công cụ dò tên đăng nhập trong một đơn vị, mở cho đúng người lẽ ra không thấy dữ liệu đó. 422 vì `userName` là tham chiếu trong payload; đơn vị `{id}` không tồn tại mới là 404 (`docs/quy-uoc/be-api-controller.md:27`, `docs/quy-uoc/be-api-controller.md:29`) |
| Bật `must_change_password`; đổi security stamp để mọi phiên đang mở trượt | Cùng lý do đá phiên ở `D4`: lý do khôi phục thường là nghi tài khoản bị chiếm |
| Ghi nhật ký theo §4 | — |

**Thao tác chạy bên trong `ITenantProvisioningService`** ([`0023-dich-vu-tao-don-vi-dung-chung.md`](0023-dich-vu-tao-don-vi-dung-chung.md) §1). Service mở phạm vi ngữ cảnh của đơn vị `{id}` để tra tài khoản đích và ghi thay đổi; service đã nằm trong allowlist của luật A12, nên thao tác này **không** thêm mục allowlist nào.

**Lối cuối khi không còn đích đủ điều kiện** (đơn vị đã từ bỏ cờ và không còn ai giữ vai trò `is_system`): `POST /api/v1/core/system/tenants/{id}/admins` tạo **một tài khoản quản trị mới** mang `has_permission_bypass` + `must_change_password` cho đơn vị đó. Đây là đường thứ ba được sinh cờ tại lúc **tạo mới** tài khoản (cùng luật M12 với lệnh bootstrap và endpoint tạo đơn vị), ghi nhật ký theo §4. Card: [`../contracts/tenants.md`](../contracts/tenants.md) §6.

Endpoint thuộc card [`../contracts/tenants.md`](../contracts/tenants.md), pha B3; mức phân quyền khai theo [`0024-ba-muc-khai-bao-phan-quyen-endpoint.md`](0024-ba-muc-khai-bao-phan-quyen-endpoint.md). Nếu khu luồng cần một luồng riêng cho thao tác này, tên đã dành là `V4-khoi-phuc-quan-tri-don-vi.md`.

Khôi phục **chính tài khoản vận hành** không đi qua HTTP: lệnh `core reset-operator-password` ở [`0023-dich-vu-tao-don-vi-dung-chung.md`](0023-dich-vu-tao-don-vi-dung-chung.md).

### 4. Thao tác xuyên đơn vị ghi hai dòng nhật ký

Áp cho **tạo đơn vị** (luồng `V2`), **ngưng và bật lại đơn vị** (luồng `V3`) và **khôi phục tài khoản quản trị** (§3):

| Dòng | Ghi ở đơn vị | Người thực hiện | Để làm gì |
| --- | --- | --- | --- |
| 1 | Đơn vị hệ thống | Tài khoản vận hành | Nhật ký của người vận hành đọc được ở một chỗ — ADR-0017 đòi ghi mọi thao tác của loại tài khoản này |
| 2 | Đơn vị bị tác động | Tài khoản vận hành, **kèm dấu hiệu máy đọc được rằng người thực hiện thuộc đơn vị khác** | Đơn vị thấy trong chính nhật ký của mình rằng một người ngoài đã tác động lên nó |

Hai dòng không tách rời nhau: cả hai cùng được ghi với bước ghi mà chúng mô tả, hoặc không dòng nào được ghi. Mỗi dòng ghi trong phạm vi ngữ cảnh của chính đơn vị nó thuộc — không dòng nào tự khai `tenant_id` (luật M2, M8). Hình dạng cột của dấu hiệu *"người thực hiện thuộc đơn vị khác"* thuộc [`../database/schema-core.md`](../database/schema-core.md) §9.4.

### 5. Ngưng và bật lại đơn vị chạy bên trong `ITenantProvisioningService`

Đổi trạng thái hoạt động của một đơn vị nghiệp vụ (luồng `V3`, `PUT /api/v1/core/system/tenants/{id}/active`) là một thao tác của service, cùng chỗ với khôi phục ở §3. Dòng nhật ký thứ hai của §4 ghi trong phạm vi của đơn vị bị tác động, trong request của một tài khoản thuộc đơn vị hệ thống; service mở phạm vi đó, và allowlist của luật A12 **không** thêm mục nào.

Lý do chọn chỗ này là lý do của §3: hai chỗ mở phạm vi xuyên đơn vị từ tài khoản vận hành là hai chỗ phải giữ đúng cùng một luật — phương án I.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ quên mật khẩu tự phục vụ trong v1

**Được:** người dùng tự lấy lại quyền truy cập bất kỳ lúc nào mà không làm phiền ai; tải của quản trị gần bằng không.

**Vì sao hoãn:** nó kéo theo một hệ thống ngoài phải chọn, cấu hình, theo dõi và khôi phục khi hỏng — và khi kênh gửi chết thì không ai thấy gì. Cộng thêm hai cửa ẩn danh phải canh, và ba câu hỏi chưa có lời đáp. Giá trị của nó phụ thuộc số người quên mật khẩu, thứ chưa đo được trước khi có người dùng thật.

### Phương án B — Người vận hành đặt lại được mật khẩu cho mọi người dùng

**Được:** một đường hỗ trợ cho mọi ca, kể cả đơn vị không còn quản trị nào hoạt động.

**Vì sao loại:** người đặt mật khẩu tạm là người biết mật khẩu tạm. Tài khoản vận hành đăng nhập được bằng mật khẩu đó vào **bất kỳ** tài khoản nào và thấy dữ liệu nghiệp vụ của đơn vị đó — đúng phương án *"một tài khoản nhìn và sửa được mọi đơn vị"* mà ADR-0017 đã loại, chỉ đi bằng cửa sau. Kèm theo, nó cho người vận hành biết ai tồn tại trong từng đơn vị.

### Phương án C — Khu hệ thống không có đường khôi phục nào

**Được:** ranh giới của ADR-0017 nguyên vẹn tuyệt đối.

**Vì sao loại:** đơn vị mất mọi lối vào quản trị thì chỉ còn đường sửa máy chủ bằng tay, và đường đó không để lại nhật ký nào. Phương án này không bỏ được thao tác khôi phục — nó chỉ đẩy thao tác đó ra khỏi tầm nhìn.

### Phương án D — Người vận hành tạo một tài khoản quản trị mới mang cờ bypass thay vì đặt lại

**Được:** không chạm tài khoản cũ; tài khoản cũ còn nguyên để điều tra.

**Vì sao không làm đường chính:** mỗi lần khôi phục để lại thêm một tài khoản toàn quyền trong đơn vị. **Giữ làm lối cuối** (§3): chỉ dùng khi không còn đích đủ điều kiện cho `recovery-reset-password`; cờ vẫn sinh tại lúc tạo mới, không trái M12.

### Phương án E — Endpoint khôi phục trả 404 cho "không có" và 422 cho "không đủ điều kiện"

**Được:** người vận hành biết mình gõ sai tên hay nhắm sai tài khoản.

**Vì sao loại:** đó là một phép dò tên đăng nhập trong đơn vị, mở cho người mà ADR-0017 chốt là không được thấy dữ liệu bên trong đơn vị. Cái giá — người vận hành phải hỏi lại đơn vị khi nhận 422 — nằm đúng chỗ.

### Phương án F — Nhật ký thao tác xuyên đơn vị ghi một dòng

**Chỉ ở đơn vị hệ thống — vì sao loại:** việc tài khoản quản trị của một đơn vị bị người ngoài đặt lại mật khẩu, hay việc chính đơn vị bị ngưng, **không hiện** trong nhật ký của đơn vị đó.

**Chỉ ở đơn vị bị tác động — vì sao loại:** nhật ký của người vận hành rải trên mọi đơn vị; muốn biết một tài khoản vận hành đã làm gì phải đọc qua toàn bộ các đơn vị.

### Phương án G — Cho quản trị tự đặt lại mật khẩu của chính mình qua `D4`

**Vì sao loại:** `D4` không đòi mật khẩu hiện tại. Một phiên bị chiếm dùng được nó để đổi mật khẩu và khoá chủ thật ra ngoài mà không cần biết mật khẩu cũ — đúng thứ đường đổi mật khẩu của chính mình tồn tại để chặn.

### Phương án H — Endpoint khôi phục chỉ nhận đích mang `has_permission_bypass`

**Được:** bề mặt nhỏ nhất — mỗi đơn vị có nhiều nhất một loại tài khoản mà người vận hành đặt được mật khẩu.

**Vì sao loại:** đơn vị làm đúng khuyến nghị vận hành — tắt cờ bypass sau khi đã có quản trị qua vai trò hệ thống — mất lưới an toàn: quản trị đó quên mật khẩu và không còn ai khác thì không có đường khôi phục nào qua khu hệ thống. Làm đúng khuyến nghị không được phép là cách tự khoá mình ra ngoài.

### Phương án I — Thao tác khôi phục, và thao tác ngưng hoặc bật lại đơn vị, mở phạm vi đơn vị đích bằng mục allowlist riêng cho từng handler

**Được:** `ITenantProvisioningService` giữ đúng việc *tạo*; handler khôi phục và handler đổi trạng thái hoạt động đứng riêng, đọc thấy ngay ở khu hệ thống.

**Vì sao loại:** allowlist của luật A12 dài thêm một mục cho mỗi thao tác cùng loại với thao tác service đã làm — mở phạm vi của một đơn vị khác, từ tài khoản vận hành. Hai chỗ mở phạm vi xuyên đơn vị là hai chỗ phải giữ đúng cùng một luật; một chỗ thì giữ ở một nơi.

## Hệ quả

### Tích cực

- Không hệ thống ngoài nào phải vận hành ở v1; không cửa ẩn danh nào cho việc đặt lại mật khẩu.
- Một đơn vị mất lối vào quản trị có đường lấy lại **có nhật ký**, thay vì một thao tác tay trên máy chủ — kể cả khi đơn vị đã tắt cờ bypass.
- Đặt lại hộ không còn là đường leo thang: quản trị chỉ đặt lại được người mình bao trùm quyền.
- *"Nhật ký xuyên đơn vị ghi vào đâu"* có lời đáp cho cả ba thao tác của tài khoản vận hành lên một đơn vị.
- Thao tác khôi phục và thao tác ngưng, bật lại đơn vị không thêm mục nào vào allowlist mở phạm vi.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Người vận hành biết mật khẩu tạm của tài khoản quản trị được khôi phục** | Về kỹ thuật, họ đăng nhập được vào tài khoản đó **trước** chủ của nó và thấy dữ liệu nghiệp vụ của đơn vị. Với thao tác này, ranh giới của ADR-0017 **không còn do bộ lọc đơn vị giữ** mà do nhật ký giữ — phát hiện được sau, không ngăn được trước. Cùng lỗ đó đã có ở luồng tạo đơn vị, nơi người vận hành gõ mật khẩu tạm của quản trị đầu tiên |
| **Mỗi lần quên mật khẩu tốn công một quản trị** | Đơn vị đông người thì quản trị thành bàn trợ giúp mật khẩu |
| **Mật khẩu tạm đi qua kênh ngoài hệ thống** | Tin nhắn, lời nói — không kiểm soát được (`docs/luong/D4-quan-tri-dat-lai-mat-khau.md:74`). Cờ buộc đổi mật khẩu là hàng rào duy nhất |
| **Số tài khoản người vận hành đặt được mật khẩu tăng theo số người giữ vai trò hệ thống** | Mỗi người giữ một vai trò `is_system` của đơn vị là một đích hợp lệ. Đơn vị gán vai trò hệ thống rộng rãi thì cái giá ở dòng đầu bảng này phủ lên nhiều tài khoản hơn |
| **Đơn vị hết đích đủ điều kiện chỉ còn lối cuối** | Đơn vị không còn tài khoản mang cờ bypass **và** không ai giữ vai trò `is_system` — ví dụ dự án không khai vai trò hệ thống nào rồi đơn vị tắt cờ bypass — thì `recovery-reset-password` không còn đích; đường còn lại là `/admins` (§3), mỗi lần dùng để lại thêm một tài khoản toàn quyền trong đơn vị |
| **Service tạo đơn vị gánh thêm những thao tác không phải tạo** | `ITenantProvisioningService` là điểm tập trung rủi ro mà [`0023-dich-vu-tao-don-vi-dung-chung.md`](0023-dich-vu-tao-don-vi-dung-chung.md) đã ghi; khôi phục, ngưng và bật lại đơn vị làm điểm đó lớn thêm, và tên service không còn tả hết việc của nó |
| **Luật 5 tính tập quyền hiệu lực của đích mỗi lần gọi** | Một truy vấn gộp vai trò của đích, trên một thao tác không thường xuyên. Chấp nhận |
| **Một thao tác ghi hai dòng nhật ký** | Đếm thao tác theo nhật ký phải khử trùng; hai dòng cùng `trace_id` là cách nối chúng |

## Điều kiện kích hoạt — xem lại quên mật khẩu tự phục vụ

1. **Hệ đã có một kênh gửi đang vận hành** cho mục đích khác — ví dụ thông báo qua thư điện tử. Chi phí biên của luồng `D3` khi đó chủ yếu là code.
2. **Tải đặt lại hộ đo được là quá sức** — số lần đặt lại hộ mỗi tuần ở một đơn vị vượt ngưỡng mà người dùng đặt ra khi đã có số liệu thật.

## Điều kiện lật quyết định

1. **Có yêu cầu người vận hành hỗ trợ tài khoản không mang cờ bypass và không giữ vai trò `is_system`.** Đó là mở rộng quyền của tài khoản vận hành vào dữ liệu người dùng nghiệp vụ — ADR mới, theo đúng điều kiện lật của ADR-0017.
2. **Có một cơ chế khôi phục mà người vận hành không biết mật khẩu** — ví dụ liên kết dùng một lần gửi thẳng tới chủ tài khoản. Cái giá lớn nhất ở mục Tiêu cực khi đó biến mất, và endpoint khôi phục nên đổi theo.

### Dấu hiệu quyết định này bắt đầu sai

- Nhật ký cho thấy khôi phục lặp lại nhiều lần trên cùng một đơn vị — người vận hành đang làm việc của quản trị đơn vị.
- Có đề xuất tách mã 422 của endpoint khôi phục "cho dễ hỗ trợ".
- Người dùng ở một đơn vị dùng chung mật khẩu vì đặt lại hộ quá phiền.

## Liên quan

- [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) — quyết định được bổ sung
- [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) — cờ `has_permission_bypass`, luật M12
- [`0023-dich-vu-tao-don-vi-dung-chung.md`](0023-dich-vu-tao-don-vi-dung-chung.md) — khôi phục tài khoản vận hành bằng lệnh
- [`0024-ba-muc-khai-bao-phan-quyen-endpoint.md`](0024-ba-muc-khai-bao-phan-quyen-endpoint.md) — mức phân quyền của endpoint khu hệ thống
- [`0013-multi-tenant.md`](0013-multi-tenant.md) — cách ly đơn vị
- [`../contracts/users.md`](../contracts/users.md) §2, §9 · [`../contracts/tenants.md`](../contracts/tenants.md) · [`../contracts/auth.md`](../contracts/auth.md) §8–§9
- [`../luong/D3-quen-mat-khau.md`](../luong/D3-quen-mat-khau.md) · [`../luong/D4-quan-tri-dat-lai-mat-khau.md`](../luong/D4-quan-tri-dat-lai-mat-khau.md) · [`../luong/N6-nhat-ky-kiem-toan.md`](../luong/N6-nhat-ky-kiem-toan.md)
- [`../database/schema-core.md`](../database/schema-core.md) §9.4 — cột của bảng nhật ký
