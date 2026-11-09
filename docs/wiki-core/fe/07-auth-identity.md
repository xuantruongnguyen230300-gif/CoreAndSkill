---
kind: luat
scope: core
verified: 2026-11-05
---

# 07. Xác thực và danh tính phía FE

> ✅ **CÓ THẬT** (đối chiếu 2026-11-05) — mọi hàng `✅` ở §10, neo vào `src/FE` ngay trong ô; các hàng `❌` cũng đã đối chiếu là chưa có trong code; thân bài §1–§8 đối chiếu cùng ngày. Hiện trạng repo: [`../../README.md`](../../README.md) mục *Trạng thái repo*.
>
> Hợp đồng endpoint: [`../../contracts/auth.md`](../../contracts/auth.md). Cơ chế phía BE: [`../be/02-identity-auth.md`](../be/02-identity-auth.md). Quy ước guard và route: [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md).

---

## 1. Phiên bằng cookie, không phải JWT trong bộ nhớ FE

Quyết định đã chốt: **cookie phiên `HttpOnly`**, do BE cấp và thu hồi. FE **không** giữ token ở đâu cả.

### 1.1 Vì sao cookie chứ không phải JWT

| | Cookie `HttpOnly` | JWT do FE giữ |
| --- | --- | --- |
| JavaScript đọc được token? | **Không** | Có — nên một lỗ XSS là mất token |
| Thu hồi phiên ngay lập tức | Được — server xoá phiên | Khó: token còn hạn thì còn dùng được |
| FE phải viết gì | Gần như không | Lưu trữ, làm mới, chống đua khi làm mới, dọn khi đăng xuất |
| Rủi ro CSRF | **Có** — phải xử lý, xem §3 | Thấp hơn nếu token nằm ở header |

Đánh đổi thật nằm ở hàng cuối: cookie đổi rủi ro XSS lấy rủi ro CSRF. Đây là đổi có lợi, vì **CSRF có biện pháp đối phó chuẩn, đầy đủ và kiểm được** (§3), còn hậu quả của XSS khi token nằm trong tầm với của JavaScript thì không có cách nào giới hạn.

Với một hệ quản trị nội bộ — FE và BE cùng miền, không có ứng dụng di động dùng chung API — cookie còn xoá luôn một tầng code phải viết và phải test.

### 1.2 Hệ quả cho FE

- Mọi request phải bật gửi cookie (`withCredentials`), làm một lần trong interceptor.
- FE **không biết** phiên còn sống hay không nếu chỉ nhìn bộ nhớ của mình. Nguồn sự thật là server; FE giữ một bản sao và bản sao đó có thể sai (§7).
- Không có logic làm mới token, và v1 không có bộ đếm phiên nào phía FE (§7.5).

---

## 2. Luồng đăng nhập

```
1. Người dùng mở app
        │
        ▼
2. Gọi endpoint "tôi là ai"  ─── 401 ──►  chưa đăng nhập → màn đăng nhập
        │ 200
        ▼
3. Có thông tin người dùng + tập permission  → nạp vào signal, dựng menu
        │
        ▼
4. mustChangePassword = true ? ── có ──►  ép sang màn đổi mật khẩu
        │ không
        ▼
5. Vào đường dẫn đích (hoặc trang chủ)
```

### 2.1 Nạp một lần lúc khởi động, không nạp lại mỗi lần đổi route

Gọi "tôi là ai" ở bước khởi động app, không gọi trong guard. Nếu gọi trong guard, mỗi lần điều hướng là một request nữa, và mỗi lần đó là một cơ hội để giao diện chớp nháy hoặc để hai request đua nhau.

**Bẫy đi kèm:** bước khởi động nhận 401 là **trạng thái bình thường** (chưa đăng nhập), không phải lỗi. Nếu để 401 này đi vào bộ xử lý hết phiên, người chưa từng đăng nhập bị báo hết phiên ngay khi mở app. Request này mang cờ `BO_QUA_HET_PHIEN` để bỏ qua bộ xử lý đó (§7.3); 401 không toast ở mọi request nên không cần cờ nào khác — xem [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.2 và §2.5.

### 2.2 Đường dẫn quay lại

Khi guard chặn, nó phải mang theo đường dẫn người dùng định vào, và sau đăng nhập phải quay đúng về đó. Luôn đưa về trang chủ là một lỗi trải nghiệm nhỏ nhưng gây khó chịu lớn: người dùng mở một đường link được gửi cho mình, đăng nhập, rồi lạc ở một màn khác.

> 🛑 **Đường dẫn quay lại là dữ liệu do người ngoài kiểm soát.** Chỉ chấp nhận đường dẫn nội bộ (bắt đầu bằng một dấu gạch chéo, không phải hai). Không kiểm là mở đường cho chuyển hướng mở — kẻ tấn công gửi một link đăng nhập của chính hệ thống mà sau khi đăng nhập lại nhảy sang miền của họ.

---

## 3. XSRF — token gửi lại qua header

### 3.1 Cơ chế

BE phát XSRF token trong **thân phản hồi** của một endpoint riêng. FE giữ token đó **trong bộ nhớ** và **gửi lại** trong một header ở mọi request làm thay đổi dữ liệu. BE so token với nửa còn lại mà nó giữ trong một cookie `HttpOnly`.

Vì sao cơ chế này chặn được CSRF: một trang của kẻ tấn công có thể khiến trình duyệt **gửi** cookie của bạn, nhưng không **đọc** được phản hồi của API — CORS không cho origin lạ đọc. Không có token thì không đặt được header, và request thiếu header bị BE từ chối.

### 3.2 Ba điều FE phải làm đúng

| Việc | Bẫy nếu làm sai |
| --- | --- |
| Chỉ gắn header cho lệnh ghi — ngoại lệ có tên duy nhất: `GET` mang dấu `CO_TAC_DUNG_PHU` (tải tệp xuất, BE ghi nhật ký kiểm toán nên kiểm token như lệnh ghi — [`../../adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md`](../../adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md)) | Gắn cho mọi `GET` là thừa; một `GET` khác làm thay đổi dữ liệu là lỗi thiết kế cần sửa ở BE |
| Chỉ gắn cho request đi tới **API của mình** | Gửi token của mình sang miền thứ ba là rò rỉ token |
| Lấy lại token sau khi đăng nhập và sau khi đăng xuất | Token gắn với danh tính lúc phát; token cũ làm thao tác ghi đầu tiên sau đổi danh tính bị từ chối |

Cách gắn và các thời điểm lấy token: [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.1 — đó là file chủ. Token **chỉ** nằm trong bộ nhớ, không `localStorage`/`sessionStorage`.

### 3.3 Triệu chứng khi hỏng

Mọi thao tác **ghi** trả 403 trong khi mọi thao tác **đọc** vẫn bình thường. Gặp đúng hình dạng này thì kiểm XSRF trước, đừng nghi phân quyền — phân quyền hỏng sẽ chặn cả đọc lẫn ghi ở cùng một tài nguyên.

---

## 4. Buộc đổi mật khẩu lần đầu

### 4.1 Vì sao cần một guard riêng

Người bị buộc đổi mật khẩu **đã đăng nhập thành công**. Guard xác thực chỉ hỏi *"đã đăng nhập chưa"*, nên nó trả lời "rồi" và cho qua. Thiếu guard riêng thì người đó vào được toàn bộ app với mật khẩu tạm — đúng thứ mà cơ chế này sinh ra để ngăn.

Không có lỗi biên dịch nào báo, và không test mặc định nào bắt. Phải kiểm bằng một tài khoản thật ở trạng thái đó.

### 4.2 Bốn ràng buộc

| Ràng buộc | Vì sao |
| --- | --- |
| Ép từ **mọi** route, kể cả gõ thẳng URL | Ẩn menu không thay được guard |
| Chính màn đổi mật khẩu **không** bị ép lần nữa | Thiếu điều kiện loại trừ này là vòng lặp điều hướng vô hạn — trình duyệt treo |
| Đổi xong đi thẳng vào app, **không** bắt đăng nhập lại | Bắt đăng nhập lại là trải nghiệm tệ và thường do FE không cập nhật lại trạng thái sau khi đổi |
| Màn này dùng bố cục không có shell | Sidebar hiện ra ở màn mà người dùng không được đi đâu cả là mâu thuẫn thị giác |

### 4.3 Thứ tự guard

Thứ tự là ràng buộc, không phải chi tiết:

```
1. Đã đăng nhập chưa?          → chưa: đi đăng nhập kèm đường dẫn quay lại
2. Có buộc đổi mật khẩu không? → có: ép sang màn đổi mật khẩu
3. Có permission cho route này không? → không: đi tới đích đã định
```

Đảo bước 2 và 3 thì người buộc đổi mật khẩu sẽ nhận thông báo "không đủ quyền" thay vì được đưa tới màn đổi mật khẩu — một thông báo sai và gây hoang mang.

> 📖 Cờ bật giữa phiên (403 `CORE.AUTH.PASSWORD_CHANGE_REQUIRED`): đọc [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §5.4.

---

## 5. Hiển thị theo permission, KHÔNG theo role

Đây là một trong những quyết định nền của repo ([`../../adr/0005-permission-based.md`](../../adr/0005-permission-based.md)) và nó áp cho FE y như BE.

### 5.1 Vì sao không dùng tên role

| Vấn đề với `if (user.role === 'Admin')` | Hệ quả |
| --- | --- |
| Core biết tên role | Sản phẩm thứ hai có bộ role khác — Core không mang đi được |
| Không thêm được role mới mà không sửa code | Thêm một role nghĩa là tìm mọi chỗ so sánh chuỗi và thêm nhánh |
| Kiểm tra rải rác, mỗi chỗ một kiểu | Chỗ dùng `===`, chỗ dùng danh sách, chỗ quên phân biệt hoa thường |
| Chuỗi role gõ sai không có gì báo | So sánh trả `false` im lặng và nút biến mất với đúng người cần nó |

Ở dự án tiền nhiệm, ba tên role được khai cứng trong Core, và mọi màn quản trị phải biết ba cái tên đó. Đó là lý do sản phẩm thứ hai không dùng lại được phần phân quyền.

### 5.2 Cách đúng

FE nhận **tập permission** của người đang đăng nhập từ endpoint "tôi là ai", và mọi kiểm tra là một câu hỏi *"có permission X không"*. FE **không biết** tập permission nào tồn tại — chuỗi do nơi gọi truyền vào và do dữ liệu quyết định.

**Tài khoản mang `has_permission_bypass` đi đúng đường này, không có đường riêng.** Phản hồi phiên của nó trả `permissions` là **toàn bộ danh mục quyền hiện có** ([`../../contracts/auth.md`](../../contracts/auth.md) §3), nên FE **không** có nhánh nào theo cờ đó — ở guard, directive hay menu. Một nhánh riêng là một đường kiểm quyền thứ hai, và hai đường thì sẽ có lúc cho hai câu trả lời khác nhau.

Câu hỏi *"có permission X không"* là một phương thức của **`AuthService`** ở `core/auth`, định nghĩa ở [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §3.3. Guard, directive và menu cùng hỏi đúng chỗ đó; không có service quyền thứ hai.

Ba nơi dùng:

| Nơi | Cách |
| --- | --- |
| Template | Directive `*appHasPermission` — [`05-component-library.md`](05-component-library.md) §4 |
| Route | Guard permission dạng factory, mã truyền làm đối số: `permissionGuard('core.user.read')` ([`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §3.3) |
| Menu | Server đã lọc sẵn — xem §6 |

### 5.3 Ranh giới phải nhắc lại

> 🛑 **Kiểm quyền ở FE là trải nghiệm, không phải bảo mật.** Nó tránh cho người dùng bấm vào thứ sẽ bị từ chối. Nó **không** ngăn được ai cả: một người dùng có thể gọi thẳng API. Mọi endpoint phải tự kiểm quyền, độc lập với FE ([`../../RULES.md`](../../RULES.md) S2).
>
> Câu hỏi để tự kiểm: *"nếu ai đó xoá đoạn kiểm tra này khỏi FE thì họ làm được gì thêm?"* — trả lời "không gì cả" thì đúng; trả lời "họ xoá được bản ghi" thì BE đang thiếu kiểm tra.

### 5.4 Tập quyền đổi giữa phiên — làm mới khi gặp 403, khi tab hiện lại, sau khi lưu ma trận

Đổi vai trò của một người, hay đổi ma trận quyền, có hiệu lực ở BE **từ request kế tiếp** của người đó ([`../../contracts/users.md`](../../contracts/users.md) §7). Tập quyền FE nạp lúc khởi động (§2.1) thì không tự biết: nút vẫn hiện, bấm vào nhận 403 — còn quyền vừa **được cấp thêm** thì không có 403 nào báo, nút của nó chỉ hiện khi tập quyền được nạp lại.

FE làm mới tập quyền và nạp lại menu trong cùng bước ở ba lúc (người dùng chốt 2026-10-06):

- **Gặp 403 `CORE.AUTH.FORBIDDEN`** — quyền vừa bị thu. 403 mang mã nghiệp vụ không kéo theo việc làm mới — nó nói về dữ liệu của thao tác, không nói tập quyền đã cũ.
- **Tab hiện lại** — người dùng quay về tab sau khi làm việc khác, thường là lúc quyền vừa được cấp ở phiên của quản trị viên. Chặn dồn để chuyển tab liên tục không thành chuỗi lời gọi `me`.
- **Màn ma trận phân quyền lưu thành công** — người lưu có thể vừa đổi quyền của chính mình.

Không nạp lại theo lịch, không nạp lại mỗi lần đổi route. Directive và guard hỏi cùng `AuthService` nên tự đổi theo; menu do server lọc (§6) nên phải nạp lại mới khớp; người dùng ở lại màn đang làm.

> 📖 Hiện thực — nhánh 403 của `errorInterceptor`: [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.2; ba lúc làm mới, khoảng chặn dồn, `AuthService.lamMoiQuyen()` và `lamMoiPhien()` (gọi lại `me`; nơi gọi khai ở đó): [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §3.3. Tầng nào xử lý 401/403: [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §8.

---

## 6. Menu động theo permission

Menu phải do **server** trả về, đã lọc theo quyền của người đang đăng nhập.

**Vì sao không để FE tự lọc từ một danh sách khai sẵn:** danh sách khai sẵn nghĩa là Core biết tên mọi màn của mọi sản phẩm dựng trên nó — vi phạm ranh giới Core ↔ Module ([`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §4). Nó cũng có nghĩa là thêm một màn nghiệp vụ phải sửa code Core.

Hợp đồng: [`../../contracts/meta-menu.md`](../../contracts/meta-menu.md).

**Ba điều FE phải xử lý:**

1. **Menu là dữ liệu, không phải cấu trúc code.** Không được có `switch` theo mã menu trong FE.
2. **Cache menu phải có khoá gồm cả người dùng lẫn ngôn ngữ** — [`03-state-management.md`](03-state-management.md) §5.1. Thiếu `userId` trong khoá là người sau thấy menu của người trước.
3. **Menu ẩn một mục không thay được guard.** Người gõ thẳng URL vẫn phải bị chặn.

---

## 7. Hết phiên và đăng xuất

### 7.1 Ba đường phiên kết thúc

| Đường | FE biết bằng cách nào | Phải làm gì |
| --- | --- | --- |
| Người dùng bấm đăng xuất | Chủ động | Gọi endpoint đăng xuất, dọn state, điều hướng |
| Phiên hết hạn ở server | Nhận 401 ở một request bất kỳ | Dọn state, điều hướng, giữ đường dẫn để quay lại |
| Quản trị viên khoá tài khoản | Nhận 401 ở request kế tiếp | Như trên |

Ba đường phải quy về **một** chỗ dọn, ở `core/auth`. Với hai đường nhận 401, chỗ đó là **`SessionExpiryHandler`**, do nhánh 401 của `errorInterceptor` gọi — hành vi chi tiết ở file chủ [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.5. Nếu mỗi nơi tự dọn, sẽ có nơi quên dọn một thứ — và thứ bị quên thường là cache.

### 7.2 Dọn cái gì

| Phải dọn | Không dọn |
| --- | --- |
| Thông tin người dùng và tập permission | Chế độ sáng/tối |
| Menu và mọi cache có nội dung theo người dùng | Ngôn ngữ đã chọn |
| State của mọi màn đã mở | |

Ranh giới: dọn thứ **thuộc về người dùng**, giữ thứ **thuộc về máy**. Nhầm chiều này thì hoặc là rò rỉ dữ liệu giữa hai người dùng chung máy, hoặc là mỗi lần đăng nhập lại phải chọn lại chế độ hiển thị.

### 7.3 Ba bẫy

**(1) Nhiều 401 cùng lúc gây nhiều lần điều hướng.** Một màn gọi bốn API song song, phiên hết hạn, cả bốn trả 401 và cả bốn cùng gọi xử lý hết phiên. Kết quả là bốn lần điều hướng và có thể bốn thông báo. Phải chặn: `SessionExpiryHandler` chạy **một lần**, cho tới lần đăng nhập kế tiếp.

**(2) Hai request mà 401 là chuyện bình thường.** `me` lúc khởi động (chưa đăng nhập, §2.1) và `logout` (phiên có thể đã hết trước khi bấm). Hai request này mang cờ `HttpContextToken` để nhánh 401 **bỏ qua** `SessionExpiryHandler`. Thiếu cờ thì người chưa từng đăng nhập bị báo "hết phiên" ngay khi mở app.

Sai mật khẩu **không** thuộc nhóm này: đăng nhập sai trả **422** `CORE.AUTH.INVALID_CREDENTIALS` ([`../../contracts/auth.md`](../../contracts/auth.md) §3) và đi đường lỗi của form. Đưa nó vào đường xử lý hết phiên là coi người gõ sai mật khẩu như người hết phiên.

**(3) Đăng xuất ở một tab không tự đóng các tab khác.** Cookie dùng chung, nên tab còn mở trở thành một giao diện chết — trông vẫn đầy dữ liệu nhưng mọi thao tác trả 401. Cách rẻ nhất: khi xử lý hết phiên, ghi một dấu hiệu vào `localStorage`; các tab khác nghe sự kiện thay đổi của `localStorage` và tự dọn theo. Đây là ngoại lệ có lý do của quyết định "không đồng bộ đa tab" ở [`03-state-management.md`](03-state-management.md) §7.

### 7.4 Khoá tài khoản chấm dứt phiên ở request kế tiếp

Khoá tài khoản và đặt lại mật khẩu hộ đều đổi security stamp; phiên của người bị tác động chấm dứt ở **request kế tiếp của chính họ**, không phải ngay lúc quản trị bấm nút. **Câu chữ trên giao diện phải phản ánh đúng nhịp đó** — viết "Đã đăng xuất người dùng" trong khi người kia vẫn đang nhìn một màn đầy dữ liệu tới lần thao tác sau khiến quản trị viên tưởng thao tác hỏng. Nhịp chấm dứt khai ở [`../../contracts/users.md`](../../contracts/users.md) §8.

### 7.5 Cảnh báo trước khi hết phiên

**v1 không có cảnh báo sớm** — hàng *Đếm ngược hết phiên trên giao diện* ở §10 (`❌ chưa`). Hết phiên xử lý khi gặp 401, bởi `SessionExpiryHandler` ([`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.5); dữ liệu chưa lưu mất — đánh đổi ở [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §8. `sessionMinutes` của DTO phiên ([`../../contracts/auth.md`](../../contracts/auth.md) §3) chưa có nơi tiêu thụ ở FE v1.

---

## 8. CORS — FE và API khác nguồn ở mọi môi trường

FE và API ở **hai origin khác nhau** ở mọi môi trường ([`17-phuc-vu-va-trien-khai.md`](17-phuc-vu-va-trien-khai.md) §4). Nghĩa là CORS và cookie chéo nguồn là chuyện của **cả dev lẫn thật**, không phải chuyện riêng của một môi trường.

> 📖 Allowlist CORS, cookie `SameSite=Lax`, antiforgery hai lớp: đọc [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §7.

**Triệu chứng khi cấu hình sai:** luôn nhận 401 dù vừa đăng nhập thành công. Kiểm theo thứ tự: cookie có được gửi kèm request không (xem tab Network) → FE và API có cùng scheme và cùng tên miền gốc không → BE có cho phép gửi thông tin xác thực không.

---

## 9. Kiểm chứng

Đi **bằng tay** trên ứng dụng chạy thật — phần lớn mục dưới không test tự động nào bắt.

- [ ] Gọi API cần đăng nhập khi chưa đăng nhập → điều hướng kèm đường dẫn quay lại, không phải màn trắng hay lỗi trong console
- [ ] Đăng nhập xong quay **đúng** về đường dẫn đó, không phải luôn về trang đích
- [ ] Đường dẫn quay lại trỏ ra miền ngoài → **bị từ chối**
- [ ] Tài khoản buộc đổi mật khẩu: gõ URL bất kỳ đều bị đưa về màn đổi mật khẩu; riêng màn đó **không lặp**
- [ ] Đổi mật khẩu xong đi thẳng vào app — **không** bắt đăng nhập lại
- [ ] Thao tác ghi **ngay sau** đăng nhập thành công chạy được (§3.2)
- [ ] Đăng xuất → gọi lại API cần auth → chặn ngay, không cần tải lại trang
- [ ] Bốn request song song cùng nhận 401 → **một** lần điều hướng, **một** thông báo
- [ ] Nhập sai mật khẩu → thấy thông báo sai mật khẩu, không bị coi là hết phiên
- [ ] Mở app khi chưa đăng nhập → vào màn đăng nhập, **không** có thông báo hết phiên
- [ ] Tài khoản mang `has_permission_bypass` → mọi chức năng theo quyền hiện ra, không qua nhánh FE riêng nào
- [ ] Form đăng nhập không có ô ghi nhớ đăng nhập
- [ ] Màn hồ sơ cá nhân đọc và lưu được qua HTTP thật
- [ ] Lưu hồ sơ với họ tên mới → Topbar hiện tên mới ngay, không tải lại trang
- [ ] Chọn một ngôn ngữ khác ở màn đổi mật khẩu bắt buộc → đổi xong, giao diện giữ ngôn ngữ đó và hồ sơ mang ngôn ngữ đó ([`08-i18n.md`](08-i18n.md) §7) — chỉ khi `CORE_I18N.languages` có từ hai mục; v1 bỏ qua
- [ ] Thu một quyền của tài khoản đang mở app → thao tác kế tiếp nhận 403 → nút **và** mục menu của quyền đó biến mất, người dùng ở lại màn
- [ ] Cấp thêm một quyền cho tài khoản đang mở app ở tab khác → chuyển sang tab đó → nút **và** mục menu của quyền mới hiện ra, không tải lại trang
- [ ] Đã đăng nhập, gõ URL không tồn tại → trang 404 **trong** khung app; chưa đăng nhập → về màn đăng nhập kèm đường dẫn quay lại
- [ ] Một request chạm hạn mức ở màn không tắt toast → toast hiện số giây chờ khớp header `Retry-After` của chính response đó
- [ ] Mở hồ sơ ở hai tab, lưu ở tab thứ nhất rồi lưu ở tab thứ hai → tab thứ hai nhận 409 kèm cảnh báo xung đột (toast theo mặc định của interceptor, hoặc cách hiển thị khác nếu màn tự tắt toast — [`../../Design/Screens/03-ho-so-ca-nhan.md`](../../Design/Screens/03-ho-so-ca-nhan.md) dùng `NoticeBanner` cho cả ba endpoint hồ sơ), dữ liệu vừa nhập còn nguyên trên form, tab Network **không** có request gửi lại tự động ([`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.2)
- [ ] Đăng xuất ở một tab → tab khác tự dọn
- [ ] Tài khoản thiếu permission gõ thẳng URL → bị chặn, **không** thấy nội dung màn dù chỉ chớp nhoáng
- [ ] Menu chỉ hiện mục mà tài khoản có quyền; ẩn mục **không** thay được guard
- [ ] Đổi ngôn ngữ → menu đổi theo (cache menu có khoá gồm cả người dùng lẫn ngôn ngữ) — chỉ khi `CORE_I18N.languages` có từ hai mục; v1 bỏ qua
- [ ] Không tệp `.html` nào chứa chữ tiếng Việt (luật F8)
- [ ] Không hằng số vai trò nào trong `core/`
- [ ] Xem tab Network: cookie thật sự được gửi kèm request
- [ ] Rời màn hồ sơ khi còn thay đổi chưa lưu → hỏi đúng câu "Rời trang?" của `unsavedChangesGuard` ([`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §4.1)
- [ ] Chọn "Rời đi" → mất thay đổi, điều hướng tiếp; chọn "Ở lại" → đóng hộp, không gửi request, thay đổi giữ nguyên
- [ ] Lưu thành công → cờ thay đổi bị xoá; rời trang ngay sau đó không bị hỏi lại
- [ ] Đăng xuất hoặc đổi ngôn ngữ ở khung ứng dụng khi màn con còn thay đổi chưa lưu → cùng đi qua đúng một hộp, không hỏi hai lần
- [ ] Đóng tab/tải lại trang khi còn thay đổi chưa lưu → trình duyệt cảnh báo (đường `beforeunload`, không chỉ đường điều hướng trong app)

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> Đối chiếu `src/FE` ngày 2026-11-05: hàng `✅ CÓ THẬT` mang neo ngay trong ô; hàng `❌` chưa có trong code. Ý nghĩa các ký hiệu: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Phiên bằng cookie `HttpOnly` | ✅ CÓ THẬT (đối chiếu 2026-11-05) — `src/FE/src/app/core/interceptors/auth.interceptor.ts`, chuỗi `withCredentials: true` | §1 — [`../../adr/0004-giu-aspnet-identity.md`](../../adr/0004-giu-aspnet-identity.md) |
| XSRF token gửi lại qua header | ✅ CÓ THẬT (đối chiếu 2026-11-05) — token ở bộ nhớ: `src/FE/src/app/core/auth/xsrf-token.store.ts`, chuỗi `readonly token = this._token.asReadonly();`; gắn header: `auth.interceptor.ts` cùng chỗ trên, chuỗi `'X-XSRF-TOKEN': token`; lấy lại sau đăng nhập: `src/FE/src/app/core/auth/auth.service.ts`, chuỗi `switchMap(() => this.xsrf.lamMoi({ boQuaToastLoi: true }))` | §3 |
| Nạp "tôi là ai" một lần lúc khởi động | ✅ CÓ THẬT (đối chiếu 2026-11-05) — `src/FE/src/app/app.config.ts`, chuỗi `auth.napPhienKhoiDong().pipe(catchError(() => of(null)))`; guard chỉ đọc signal: `src/FE/src/app/core/guards/auth.guard.ts`, chuỗi `if (auth.daDangNhap()) {` | §2.1 |
| Ba guard đúng thứ tự | ✅ CÓ THẬT (đối chiếu 2026-11-05) — `src/FE/src/app/app.routes.ts`, chuỗi `canActivate: [authGuard, mustChangePasswordGuard]`; `permissionGuard` ở route con, ví dụ `src/FE/src/app/platform/quan-tri/nguoi-dung/nguoi-dung.routes.ts`, chuỗi `canActivate: [permissionGuard('core.user.read')]` | §4.3 |
| Làm mới tập quyền và menu khi gặp 403 thiếu quyền, khi tab hiện lại, sau khi lưu ma trận quyền | ✅ CÓ THẬT (đối chiếu 2026-11-05) — 403: `src/FE/src/app/core/interceptors/error.interceptor.ts`, chuỗi `auth.lamMoiQuyen();`; tab hiện lại: `src/FE/src/app/core/auth/auth.service.ts`, chuỗi `const KHOANG_CHAN_LAM_MOI_MS = 30_000;`; ma trận: `src/FE/src/app/platform/quan-tri/phan-quyen/pages/ma-tran/ma-tran-phan-quyen.page.ts`, chuỗi `void this.auth.lamMoiPhien({ napLaiMenu: true });` | §5.4 |
| Menu do server lọc theo quyền | ✅ CÓ THẬT (đối chiếu 2026-11-05) — `src/FE/src/app/core/menu/menu.service.ts`, chuỗi `'/core/meta/menu'`; khoá cache gồm người dùng và ngôn ngữ: `src/FE/src/app/core/menu/menu.store.ts`, chuỗi `private khoaCache(userId: string): string {` | §6 — [`../../contracts/meta-menu.md`](../../contracts/meta-menu.md) |
| Đồng bộ đăng xuất giữa các tab | ✅ CÓ THẬT (đối chiếu 2026-11-05) — ghi dấu: `src/FE/src/app/core/auth/tab-session-broadcast.service.ts`, chuỗi `localStorage.setItem(KHOA_BAO_TAB_PHIEN_KET_THUC, String(Date.now()));`; tab khác nghe: `src/FE/src/app/core/auth/session-expiry.handler.ts`, chuỗi `window.addEventListener('storage', nghe);` | §7.3 — ngoại lệ có lý do của [`03-state-management.md`](03-state-management.md) §7 |
| Ghi nhớ đăng nhập / phiên dài | ❌ chưa | Điều kiện: có yêu cầu thật và BE hỗ trợ trước. Ở v1 mọi phiên cùng một thời hạn không thao tác ([`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §7); form đăng nhập không có ô ghi nhớ |
| Đăng nhập một lần (SSO) | ❌ chưa | Điều kiện: tổ chức có nhà cung cấp danh tính dùng chung |
| Xác thực hai yếu tố phía giao diện | ❌ chưa | Điều kiện: BE hỗ trợ trước; FE chỉ là màn nhập mã |
| Đếm ngược hết phiên trên giao diện | ❌ chưa | Điều kiện: phiên ngắn tới mức người dùng mất dữ liệu form đang nhập |
| JWT do FE tự giữ | ❌ loại, không hoãn `K23` | §1.1 — một lỗ XSS là mất token |
| Kiểm quyền theo tên vai trò | ❌ loại, không hoãn `K24` | §5.1 — [`../../adr/0005-permission-based.md`](../../adr/0005-permission-based.md) |

> Cách đọc ba ký hiệu của bảng trên — và khi nào *"FE thiếu X"* là finding: [`../README.md`](../README.md) §9.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Hợp đồng endpoint xác thực | [`../../contracts/auth.md`](../../contracts/auth.md) |
| Guard, thứ tự guard, cờ tắt shell | [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) |
| Cơ chế phía BE | [`../be/02-identity-auth.md`](../be/02-identity-auth.md) |
| Vì sao permission chứ không phải role | [`../../adr/0005-permission-based.md`](../../adr/0005-permission-based.md) |
| Menu động | [`../../contracts/meta-menu.md`](../../contracts/meta-menu.md) |
| Bảo mật FE ngoài xác thực | [`14-security.md`](14-security.md) |
