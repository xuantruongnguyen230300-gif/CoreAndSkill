---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0021 — Hai đặc quyền trên tài khoản là hai cột boolean loại trừ nhau; đơn vị hệ thống nhận ra bằng cột, không bằng định danh cố định

> **Trạng thái:** Đã chấp nhận (2026-09-12)

## Bối cảnh

Ba tài liệu đã chốt đang mô tả những thứ mà lược đồ không có chỗ để chứa.

[`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §3.6 chọn *"một cờ trên bản ghi người dùng, được bộ kiểm quyền hiểu là bỏ qua kiểm"* làm lời giải cho câu hỏi *"cài mới, chưa có vai trò nào, ai đăng nhập được để tạo vai trò đầu tiên"*. [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.3 thu hẹp phạm vi của cờ đó về **trong một đơn vị**, và §11.4 nói tài khoản quản trị đầu tiên của **mỗi** đơn vị đều mang nó. Nhưng [`../database/schema-core.md`](../database/schema-core.md) §4.1 không có cột nào cho cờ đó. Cột duy nhất trông giống là `is_system_operator`, và nó là vai **ngược lại**: [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) chốt tài khoản vận hành chỉ thấy **danh sách đơn vị**, không thấy dữ liệu nghiệp vụ của đơn vị nào.

Cùng khuôn thiếu chỗ chứa, ở bảng đơn vị: [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10 yêu cầu *"mọi chỗ liệt kê đơn vị cho người dùng chọn phải loại nó ra"*, [`../contracts/tenants.md`](../contracts/tenants.md) §1 yêu cầu đơn vị hệ thống không nằm trong kết quả, và §3 khai một mã lỗi cho ca *"không được ngưng hoạt động đơn vị hệ thống"*. Cả ba đều giả định code **nhận ra được** đơn vị hệ thống. [`../database/schema-core.md`](../database/schema-core.md) §1.3 không cho nó cách nào để nhận ra.

Đây không phải ba thiếu sót rời nhau. Chúng là cùng một câu hỏi chưa được trả lời: **Core nhận ra một dòng đặc biệt bằng cái gì.**

Ràng buộc lúc quyết định: chưa có `src/`, chưa có dữ liệu ở bất kỳ đâu, nên mọi thay đổi lược đồ ở đây có chi phí gần bằng không — và sẽ không bao giờ rẻ như vậy nữa.

## Quyết định

1. Thêm cột `has_permission_bypass` vào bảng người dùng của Core. Ngữ nghĩa: **bộ kiểm quyền trả lời có cho mọi câu hỏi quyền của tài khoản này**. Nó **không** đụng tới bộ lọc đơn vị — tài khoản mang cờ vẫn chỉ thấy dữ liệu đơn vị mình, đúng như [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.3 đã chốt.
2. `has_permission_bypass` và `is_system_operator` là **hai cột riêng, loại trừ nhau**. Tính loại trừ được ép bằng một ràng buộc kiểm tra ở chính database, không bằng một luật viết trên giấy.
3. Thêm cột `is_system` vào bảng đơn vị của Core, kèm một index duy nhất **một phần** cho phép **nhiều nhất một** dòng mang giá trị đúng.
4. Không endpoint nào được đặt `has_permission_bypass` lên một tài khoản **đã tồn tại**. Cờ chỉ được đặt tại thời điểm **tạo mới** tài khoản, bởi đúng hai đường: lệnh bootstrap chạy tay lúc cài đặt, và endpoint tạo đơn vị ở khu quản trị hệ thống.

Cả hai tên cột theo quy ước tiền tố boolean ở [`../database/schema-core.md`](../database/schema-core.md) §2.1.

## Hai cờ khác nhau ở chỗ nào

Đây là bảng mà người thi công cần, và là thứ hiện không tồn tại ở bất kỳ đâu:

| | `has_permission_bypass` | `is_system_operator` |
| --- | --- | --- |
| Trả lời câu hỏi | Bộ kiểm quyền có trả lời **có** cho mọi câu hỏi không | Tài khoản có vào được **khu quản trị hệ thống** không |
| Đặt ở đâu trong luồng | Trong bộ kiểm quyền, **đúng một nhánh** | Ở biên của riêng nhóm endpoint khu hệ thống |
| Đơn vị của tài khoản | Một đơn vị **nghiệp vụ** | Đơn vị **hệ thống** |
| Thấy dữ liệu nghiệp vụ | Có — của đúng đơn vị mình | **Không, của bất kỳ đơn vị nào** |
| Thấy danh sách đơn vị | Không | Có |
| Có bao nhiêu tài khoản | Một cho mỗi đơn vị nghiệp vụ | Ít, và nên giới hạn ([`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md)) |
| Vai trò nghiệp vụ | Được gán bình thường | **Cấm** — luật M9 |

Hai dòng cuối cùng của bảng trên là lý do gộp chúng lại thành một khái niệm *"tài khoản quản trị"* sẽ hỏng: một cái là quyền lực **bên trong** một đơn vị, cái kia là quyền lực **về** các đơn vị, và chúng loại trừ nhau đúng theo nghĩa mà [`0013-multi-tenant.md`](0013-multi-tenant.md) dựng lên.

## Giải một mâu thuẫn đang tồn tại

[`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §3.6 khai *"cờ này không có endpoint nào đặt được"*, và liệt kê chính câu đó làm cách canh rủi ro *"cờ trở thành cửa sau ai cũng dùng"*. [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) sinh ra một endpoint tạo đơn vị, và [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4 nói đơn vị mới có một tài khoản quản trị **mang cờ bootstrap**. Hai câu không thể cùng đúng.

Quyết định giữ **cái mới**, vì §11.3 đã nói thẳng nó đang sửa phạm vi của §3.6, và vì ADR-0017 là quyết định sau. Nhưng câu thay thế không phải *"bỏ luôn ràng buộc cũ"* — nó là một bất biến hẹp hơn, và hẹp đúng chỗ cần hẹp:

> **Không có đường nào đặt cờ lên một tài khoản ĐÃ TỒN TẠI.** Cờ chỉ sinh ra cùng lúc với chính tài khoản mang nó.

Bất biến mới mạnh gần bằng bất biến cũ ở đúng thứ đáng lo — leo thang đặc quyền cho một tài khoản sẵn có — và nó **kiểm được**: nó nói về vị trí của một phép gán trong code, không nói về ý định. Bất biến cũ thì không còn đúng và sẽ khiến người thi công đọc §3.6 bỏ qua §11.4, tạo ra một đơn vị mới mà người quản trị của nó đăng nhập vào **không làm được gì**.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Không thêm cột nào, dùng lại `is_system_operator` cho cả hai vai

**Được:** đơn giản nhất có thể; không đụng lược đồ; là phương án mà người đọc lướt tài liệu sẽ tưởng là đang có sẵn.

**Vì sao loại:** nó xoá đúng ranh giới mà ADR-0017 dựng lên. Tài khoản quản trị của một đơn vị sẽ mang cờ vận hành, nên nó **thấy danh sách mọi đơn vị**; và luật M9 cấm tài khoản mang cờ vận hành giữ vai trò nghiệp vụ, nên nó lại **không làm được việc của chính đơn vị mình**. Phương án này hỏng ở cả hai đầu cùng lúc.

### Phương án B — Một cột enum `privilege_kind` thay cho hai boolean

**Được:** tính loại trừ có sẵn trong kiểu dữ liệu, không cần ràng buộc kiểm tra. Một chỗ để đọc. Đây là phương án mà một người tỉnh táo sẽ chọn, và nó suýt thắng.

**Mất — và đây là lý do loại:** nó **mã hoá một chính sách thành một kiểu dữ liệu**. Việc hai đặc quyền hôm nay loại trừ nhau là một lựa chọn của ta, không phải một sự thật của bài toán: chúng trả lời hai câu hỏi ở hai tầng khác nhau (bộ kiểm quyền, và biên của một nhóm endpoint). Khai bằng enum thì ngày muốn nới — ví dụ một tài khoản vận hành cần bỏ qua kiểm quyền cho chính các endpoint khu hệ thống — phải **chuyển đổi dữ liệu**. Khai bằng hai cột cộng một ràng buộc thì nới là **gỡ một ràng buộc**, và ràng buộc bị gỡ để lại vết trong lược đồ.

Thêm một điểm nhỏ hơn nhưng có thật: enum biến trạng thái thường gặp nhất — *"không có đặc quyền gì"* — thành một **giá trị phải nhớ tên**, và mọi truy vấn phải so với nó. Hai boolean mặc định sai không cần ai nhớ gì.

### Phương án C — Một bảng riêng cho đặc quyền của tài khoản

**Được:** mở rộng thoải mái; lịch sử cấp và thu hồi đặc quyền ghi được.

**Vì sao loại:** hai đặc quyền không cần lịch sử riêng — nhật ký kiểm toán đã ghi việc đặt cờ ([`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §3.6). Đổi lại, nó thêm một phép nối vào **mọi** lần kiểm quyền, tức vào đường nóng nhất của hệ. Và một bảng đặc quyền là một mời gọi rất rõ ràng để dựng đường phân quyền **thứ ba** — đúng thứ mà [`0005-permission-based.md`](0005-permission-based.md) và ADR-0017 đang cố giữ cho ít.

### Phương án D — Nhận ra đơn vị hệ thống bằng một định danh cố định thay vì một cột

**Được:** không thêm cột; không thêm index; là cách ADR-0020 dùng cho dấu seed dev.

**Vì sao loại — và vì sao nó KHÁC ca của ADR-0020:** đơn vị hệ thống là một dòng mà hệ thống phải **duy trì và truy vấn suốt đời**; dấu seed dev là một dòng mà môi trường thật phải **từ chối**. Với dòng phải duy trì, một định danh cố định biến mọi truy vấn thành một phép so với hằng số nằm trong code, và ngày khôi phục dữ liệu từ một bản cài khác thì hằng số đó trỏ vào hư không — im lặng, vì truy vấn vẫn hợp lệ và chỉ trả về rỗng. Với dòng phải từ chối thì ngược lại: một cột dành riêng cho nó là một cột **sai với 100% dữ liệu thật, mãi mãi**, và một cột ghi được mà lẽ ra luôn sai là một cột sẽ có ngày bị đặt thành đúng.

Nguyên tắc rút ra, và nó là phần đáng nhớ nhất của ADR này: **cột cho trạng thái hệ thống phải duy trì; hằng số định danh cho trạng thái hệ thống phải từ chối.**

### Phương án E — Suy ra đơn vị hệ thống từ dữ liệu, không khai tường minh

Chẳng hạn *"đơn vị hệ thống là đơn vị của các tài khoản vận hành"*.

**Vì sao loại:** nó vòng tròn ở đúng lúc cần nhất — lần cài đặt đầu tiên, khi chưa có tài khoản vận hành nào. Và nó biến một sự thật ổn định thành một phép suy chạy lại mỗi lần, để rồi hỏng lặng lẽ vào ngày tài khoản vận hành cuối cùng bị vô hiệu hoá.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| Core có **hai** đường phân quyền song song, nay thành ba cùng ma trận quyền | ADR-0017 đã ghi cái giá này cho cờ vận hành; ADR này thừa nhận cờ thứ hai và **không** giả vờ rằng nó rẻ hơn. Ba đường thì mọi câu hỏi *"vì sao người này vào được màn này"* phải kiểm ba chỗ, và màn hình trả lời câu hỏi đó phải nói ra cả ba |
| Tính loại trừ **không miễn phí** | Với enum nó có sẵn trong kiểu dữ liệu; ở đây nó là một ràng buộc phải viết. Quên viết thì tồn tại được một dòng mang cả hai cờ, và dòng đó vừa bỏ qua kiểm quyền vừa vào được khu hệ thống. Đây là cái giá trực tiếp của việc chọn B thì mất linh hoạt, chọn hai cột thì phải tự canh |
| Một index duy nhất một phần trên bảng đơn vị | Ràng buộc *"nhiều nhất một đơn vị hệ thống"* là **nhiều nhất**, không phải **đúng một** — database không ép được *"đúng một"* cho tới khi dòng đó tồn tại. Khoảng hở: một bản cài chưa chạy bootstrap có **không** đơn vị hệ thống nào, và trạng thái đó hợp lệ với ràng buộc nhưng vô dụng với ứng dụng. Phải có một phép kiểm khác bắt ca đó |
| Số tài khoản mang `has_permission_bypass` **tăng theo số đơn vị** | Ngưỡng cảnh báo ở [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §3.6 không còn khai được bằng một hằng số. Xem [`0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md`](0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md) |
| Khuyến nghị *"tắt cờ sau khi dựng xong bộ vai trò"* nay áp cho **mỗi** đơn vị | §3.6 gọi đó là khuyến nghị vận hành, không ép được bằng code. Nhân nó lên theo số đơn vị thì tỉ lệ bị quên tiến về 100%. Không có lời giải trong ADR này — chỉ có phép đếm, và phép đếm chỉ nói *"nhiều quá"* chứ không nói *"đơn vị nào quên"* |
| Bất biến cũ của §3.6 bị thay bằng một bất biến hẹp hơn | Trước: không endpoint nào đặt được cờ. Sau: không endpoint nào đặt cờ **lên tài khoản đã tồn tại**. Bề mặt tấn công rộng ra đúng một endpoint, và endpoint đó nằm sau cờ vận hành |

### Tích cực

- Hai tài liệu đã chốt nhưng không thi công được nay thi công được, và người thi công không phải đoán.
- Tính loại trừ và tính "nhiều nhất một" do **database** ép, không do một test ép — chúng đúng kể cả với dữ liệu ghi bằng SQL tay.
- Mã lỗi *"không được ngưng hoạt động đơn vị hệ thống"* ở [`../contracts/tenants.md`](../contracts/tenants.md) §3 nay có cách để kiểm; trước đó nó là một mã lỗi không ai viết nổi.
- Ngữ nghĩa hai cờ nằm cạnh nhau trong một bảng, nên câu hỏi *"tại sao không gộp"* có câu trả lời tại chỗ thay vì bị đề xuất lại mỗi sáu tháng.

### Điều gì là dấu hiệu quyết định này bắt đầu sai

- `has_permission_bypass` bị đọc ở nhiều hơn **một** chỗ trong bộ kiểm quyền. §3.6 đã cảnh báo đúng ca này; nó là bước đầu của việc cờ biến thành vai trò trá hình.
- Có đề xuất bỏ ràng buộc loại trừ vì "tiện". Nới thì phải là một ADR trích ADR này, không phải một dòng trong migration.
- Xuất hiện cột đặc quyền thứ ba. Hai cột là một ranh giới; ba cột là một bảng đặc quyền đang mọc ra mà không ai gọi tên nó — lúc đó phương án C đáng xét lại.
- Có người hỏi *"làm sao tài khoản vận hành sửa được hồ sơ của chính nó"* và câu trả lời đòi nới M9. Xem phần dưới.

## Một câu hỏi ADR này KHÔNG trả lời

Tài khoản vận hành không mang `has_permission_bypass` và bị luật M9 cấm giữ vai trò nghiệp vụ. Vậy nó lấy quyền ở đâu để gọi các endpoint **của chính nó** — xem hồ sơ, đổi mật khẩu, đăng xuất? Nếu những endpoint đó đòi một khoá quyền, tài khoản vận hành nhận 403 trên chính hồ sơ mình.

Có hai lối ra — coi nhóm endpoint đó là *"của bản thân"* và không đòi quyền, hoặc đọc M9 là *"cấm vai trò nghiệp vụ, không cấm vai trò Core"* — và chúng dẫn tới hai mô hình khác nhau. ADR này **không chọn**, vì chọn ở đây là sửa phạm vi của luật M9, tức chạm vào ADR-0017. Ghi ra để nó không bị phát hiện lần đầu bởi một lập trình viên đang gấp.

## Liên quan

- [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) — nguồn của `is_system_operator` và luật M9
- [`0013-multi-tenant.md`](0013-multi-tenant.md) · [`0005-permission-based.md`](0005-permission-based.md) — hai ranh giới mà ADR này phải không phá
- [`0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md`](0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md) — ca ngược lại của phương án D
- [`../RULES.md`](../RULES.md) §9 — luật M10, M11, M12
