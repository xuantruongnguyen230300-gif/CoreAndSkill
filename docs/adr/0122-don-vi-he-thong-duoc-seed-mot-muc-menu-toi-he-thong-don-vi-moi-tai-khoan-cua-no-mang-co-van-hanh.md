---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0122 — Đơn vị hệ thống, và chỉ nó, được seed đúng một mục menu tới `/he-thong/don-vi`; mọi tài khoản của đơn vị hệ thống mang cờ vận hành

> **Trạng thái:** Đã chấp nhận (2026-10-08)
>
> **Sửa một phần [`0079-khu-he-thong-khong-nhan-duong-vao-nhin-thay-duoc-o-v1.md`](0079-khu-he-thong-khong-nhan-duong-vao-nhin-thay-duoc-o-v1.md)** và **bổ sung [`0088-seed-don-vi-he-thong-hoan-co-dieu-kien.md`](0088-seed-don-vi-he-thong-hoan-co-dieu-kien.md)**. Phần nào của hai ADR đó còn hiệu lực: mục *Quan hệ với các ADR trước* ở cuối.

## Bối cảnh

ADR-0079 hoãn đường vào nhìn thấy được tới khu `/he-thong` và nêu sáu điều kiện xét lại. Ngày 2026-10-08 điều kiện 3 xảy ra: người dùng — người cài đặt, và là người vận hành duy nhất hôm nay — báo không tìm ra đường vào màn đơn vị. Đó đúng là phép đo thiệt hại mà ADR-0079 ghi là còn thiếu, và chính ADR đó nói một lần là đủ để mở lại. Điểm đáng ghi: người không tìm ra là người có trong tay toàn bộ tài liệu cài đặt.

Người dùng chọn phương án B của ADR-0079 — seed mục menu chỉ vào đơn vị hệ thống — và loại phương án A vì nó đổi lược đồ Core. ADR-0079 nói rõ phương án B chỉ đúng khi một bất biến về đơn vị hệ thống được chốt và có cổng. Phép đo trên mã nguồn ngày 2026-10-08, bằng đọc tệp:

- **Lọc menu không đổi.** Ba bước ở [`../database/schema-core.md`](../database/schema-core.md) §6.3. Một mục không gắn quyền, không có dòng `core.menu_item_role`, rơi vào bước 3 và hiện cho mọi người dùng đã đăng nhập **của đơn vị chứa nó**, vì `core.menu_item` mang `tenant_id` (§6.1 cùng tệp).
- **Seed chỉ chạy lúc tạo đơn vị, và không rẽ nhánh.** `src/BE/Core/CoreAndSkill.Core.Infrastructure/Tenants/TenantProvisioningService.cs` gọi seed dưới `if (tenantWasCreated)`; thân `private async Task ApplySeedAsync(CancellationToken ct)` gộp mọi nguồn `ITenantSeedSource` cho mọi đơn vị. Nguồn của Core là `src/BE/Core/CoreAndSkill.Core.Infrastructure/Tenants/CoreTenantSeedSource.cs`, hàm `GetMenuItems()`.
- **Bất biến *mọi tài khoản của đơn vị hệ thống mang `is_system_operator`* hôm nay đúng nhờ cấu trúc, nhưng không được phát biểu ở đâu và không có cổng.** Có bốn đường sinh hay đổi tài khoản:
  1. Lệnh bootstrap tạo đơn vị hệ thống qua service tạo đơn vị, với `AdminIsSystemOperator: true,` ở `src/BE/Core/CoreAndSkill.Core.Web/Commands/CoreCommandRunner.cs`. Service nhận hai cờ của tài khoản đầu **độc lập** với `IsSystem` — bất biến nằm ở chỗ gọi, không nằm ở service.
  2. Tạo quản trị bổ sung loại đơn vị hệ thống: `TenantProvisioningService.cs`, chuỗi `FindBusinessTenantAsync`.
  3. `POST /users` tạo tài khoản không cờ trong đơn vị của người gọi — `src/BE/Core/CoreAndSkill.Core.Infrastructure/Identity/UserAdminService.cs`, chuỗi `AppUser.NewAccount(input.UserName, input.Email, input.FullName)` — nhưng đòi quyền tạo người dùng. Trong đơn vị hệ thống không tài khoản nào có quyền nào: tài khoản vận hành không mang được cờ bypass (ràng buộc `ck_app_user_privilege_flags_exclusive` ở `database/scripts/core/0001__core__initial.sql`, luật M11), và không ai trong đơn vị đó gán được vai trò cho nó (luật M9).
  4. Cờ không gỡ được khỏi tài khoản đã có: `src/BE/Core/CoreAndSkill.Core.Infrastructure/Identity/AppUser.cs`, chuỗi `public bool IsSystemOperator { get; private set; }` — chỉ gán trong `AppUser.NewAccount(`.

  Ngoài bốn đường đó chỉ còn SQL tay.
- **Cổng hiện có không biết đơn vị.** `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Tenants/CoreTenantSeedMenuTests.cs`, chuỗi `CoreSeedMenu_HasNoItemPointingIntoTheSystemOperatorArea`, đọc nguồn của Core — tức tập mục **mọi** đơn vị nhận. Chạy xanh bằng `dotnet test` ngày 2026-10-08.
- **Bản cài đã bootstrap không tự nhận mục mới.** Seed không chạy lại cho đơn vị đã có; lệnh `core seed-tenant-defaults` chưa thi công ([`../database/script-runbook.md`](../database/script-runbook.md) §9). [`../database/migration-policy.md`](../database/migration-policy.md) §4.1 không cho dữ liệu của một đơn vị đi trong migration. Hôm nay bản cài đã bootstrap duy nhất là máy dev của người dùng: đường cài Compose chưa ai chạy (bảng 🚧 ở script-runbook §3.3).

## Quyết định

Người dùng chốt ngày 2026-10-08: dùng phương án B của ADR-0079 — chỉ seed một mục menu vào đơn vị hệ thống, không đổi lược đồ `core.menu_item`, không thêm bước lọc thứ tư — và bù mục đó cho bản cài đã bootstrap bằng một script dữ liệu do người dùng chạy. `architect` chốt cơ chế theo sáu điểm:

1. **Mục menu.** Đơn vị hệ thống nhận đúng một mục: mã `he-thong-don-vi`, route `/he-thong/don-vi`, không quyền, không dòng `core.menu_item_role`. Đơn vị nghiệp vụ không nhận mục này. Mục hiện cho **mọi tài khoản của đơn vị hệ thống**; nhờ điểm 3, tập đó đúng bằng tập tài khoản vận hành. Mã chốt ở đây chứ không để thiết kế chọn, vì mã menu không đổi sau khi đã dùng (schema-core §6.1, luật 3) và script ở điểm 5 nhận ra dòng bằng mã.
2. **Cơ chế seed.** Mục khai ở `CoreTenantSeedSource` thành một danh sách riêng của đơn vị hệ thống, **ngoài** `ITenantSeedSource`: seam không đổi, nguồn của module không góp được gì vào danh sách này. `TenantProvisioningService` ghép danh sách đó vào tập menu khi đơn vị vừa tạo mang `IsSystem`. `TenantSeedValidator` kiểm đúng tập mà đơn vị hệ thống nhận — mã không trùng với mục của nguồn khác, cây một cấp — để va chạm hiện lúc khởi động chứ không thành lỗi ghi giữa lệnh bootstrap.
3. **Bất biến — luật M16.** Mọi tài khoản thuộc đơn vị hệ thống mang `is_system_operator`. Service tạo đơn vị tự ép, không trông vào chỗ gọi: yêu cầu tạo đơn vị hệ thống mà tài khoản đầu không mang cờ vận hành, hoặc mang cờ bypass, thì ném `InvalidOperationException` — lỗi lập trình, cùng loại với lời gọi ngoài transaction, vì không người dùng nào dựng được yêu cầu đó. Ba đường còn lại ở §Bối cảnh giữ nguyên. Đường sinh tài khoản mới nào chạm đơn vị hệ thống — ví dụ tạo tài khoản vận hành thứ hai — phải giữ M16.
4. **Cổng — luật M17.** `CoreSeedMenu_HasNoItemPointingIntoTheSystemOperatorArea` giữ vế hôm nay: nguồn seed dùng chung của Core không có mục nào trỏ vào `/he-thong`. Cùng lượt thi công, cổng thêm vế đơn vị hệ thống: danh sách riêng có đúng một mục mang đúng các giá trị ở điểm 1, và một integration test PostgreSQL thật xác nhận sau khi tạo một đơn vị hệ thống và một đơn vị nghiệp vụ thì đơn vị hệ thống có đúng một mục `/he-thong/…`, đơn vị nghiệp vụ không có mục nào. Gỡ hay nới một vế để một mục lọt qua vẫn là vi phạm, như quyết định 3 của ADR-0079.
5. **Bản cài đã bootstrap.** Một script bù dữ liệu thêm mục cho đơn vị hệ thống đã có. Tệp đặt ở thư mục mới `bu-du-lieu` dưới `database/`, **ngoài** `database/scripts/`: không vòng áp schema nào nhặt nó, và nó không ghi `core.schema_script_history`. Năm tính chất bắt buộc:
   - Mở bằng khối tự chặn ở script-runbook §3.1, chép nguyên văn, ngay sau `START TRANSACTION;`.
   - Không có đơn vị hệ thống thì dừng bằng lỗi nói rõ *bản cài chưa bootstrap; lệnh bootstrap sẽ seed mục này* — không im lặng chèn không dòng nào.
   - Đơn vị hệ thống đã có mục mã `he-thong-don-vi` chưa xoá thì không ghi gì và báo ra; chưa có thì chèn đúng một dòng. Chạy lại cho cùng kết quả.
   - Giá trị mọi cột của mục lấy nguyên từ danh sách ở điểm 2. `id` sinh bằng `uuidv7()` của PostgreSQL 18 — phiên bản ghim ở `docker-compose.yml`, chuỗi `image: postgres:18`; cụm thiếu hàm đó thì script dừng và transaction không để lại gì. Năm cột audit điền như dòng do lệnh bootstrap ghi, tác nhân `system`.
   - Một integration test PostgreSQL thật chạy chính tệp này hai lần trên một đơn vị hệ thống thiếu mục, rồi so dòng với danh sách ở điểm 2 — để giá trị trong SQL không lệch giá trị trong C# lúc viết.

   **Người dùng chạy script**, bằng tài khoản đóng được vai chủ schema `core` (script-runbook §3.1, *Tài khoản chạy script*), sau khi code của lượt thi công đã vào; rồi đăng nhập lại bằng tài khoản vận hành để thấy mục. Agent chỉ viết.
6. **Giao diện.** Nhãn, khoá dịch, icon, thứ tự và vị trí — cấp một hay con của một mục cha đã có — do `design-expert` chốt ở [`../Design/Screens/00-khung-ung-dung.md`](../Design/Screens/00-khung-ung-dung.md) và [`../Design/Screens/20-don-vi.md`](../Design/Screens/20-don-vi.md). ADR này không đặt giá trị nào trong số đó; thi công lấy từ spec. Vị trí nào đòi thêm một mục cha mới — tức hai mục — thì quay lại `architect`, vì nó đổi điểm 1.

Khung FE vẫn không rẽ nhánh theo cờ `isSystemOperator`: mục đến từ dữ liệu menu như mọi mục khác.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Cột `requires_system_operator` trên `core.menu_item` và bước lọc thứ tư

Phương án A của ADR-0079.

**Được:** giải ở đúng tầng; không cần bất biến nào về đơn vị hệ thống, vì mục tự lọc theo cờ của từng tài khoản; mọi màn sau này của khu dùng lại không thêm gì.

**Mất:** đổi lược đồ một bảng Core, nên mọi bản cài hạ nguồn chạy một bước di trú; thứ tự lọc §6.3 thêm một nhánh.

**Vì sao loại:** người dùng loại vì nó đổi lược đồ Core. Lý do hoãn của ADR-0079 vẫn đúng: khu có đúng một màn, và chưa lý do nào khác buộc `core.menu_item` nhận thêm chiều diễn đạt. Vẫn là **hoãn**, không bác — điều kiện kích hoạt 4 của ADR-0079 còn hiệu lực, và hai dấu hiệu sai đầu tiên ở cuối ADR này trỏ về đây.

### Phương án B' — Mở `ITenantSeedSource` cho nguồn khai mục riêng của đơn vị hệ thống

**Được:** module nào có màn ở khu hệ thống thì tự góp mục; danh sách không phải nằm trong Core.

**Mất:** seam mang một khái niệm chỉ đúng với một đơn vị; mọi nguồn, kể cả nguồn module, thêm một phương thức hầu như luôn trả rỗng.

**Vì sao loại:** khu `/he-thong` là của Core ([ADR-0017](0017-khu-quan-tri-he-thong.md)), và hôm nay không module nào có màn ở đó. Một abstraction có đúng một hiện thực, hiện thực đó nằm trong Core, là dấu hiệu Core phình. Điều cấm này của quyết định 2 ADR-0079 vẫn giữ; module đầu tiên cần đặt màn vào khu hệ thống là lúc xét lại, bằng ADR mới.

### Phương án C — Viết rõ URL trong tài liệu cài đặt, hoặc gợi ý ở trang đăng nhập

Phương án đơn giản nhất: không đổi code, không đổi dữ liệu.

**Được:** gần như miễn phí.

**Vì sao loại:** phép đo ở §Bối cảnh là chính bằng chứng tài liệu không đủ — người không tìm ra có tài liệu trong tay. Gợi ý ở trang đăng nhập chỉ hiện ở đầu phiên, cùng khuyết tật đã làm ADR-0079 **loại** phương án đáp xuống `/he-thong/don-vi` sau khi đăng nhập; phương án đó vẫn loại, lý do không đổi.

### Phương án D — Bù dữ liệu bằng migration EF, sinh thành script đánh số trong `database/scripts/core/`

**Được:** một đường cho mọi môi trường; ứng dụng từ chối khởi động tới khi script được chạy (script-runbook §5.1), nên không ai quên.

**Vì sao loại:** migration-policy §4.1 không cho dữ liệu của một đơn vị đi trong migration, và điều kiện 4 ở đó cấm câu ghi phụ thuộc dữ liệu có sẵn — câu chèn này phụ thuộc đúng dòng đơn vị hệ thống, và trên database trống nó chạy trước bootstrap nên không làm gì. Lật hai điều đó cho một nhu cầu hôm nay chỉ có một máy, rồi để mọi bản cài mới chạy mãi một script không làm gì, là trả giá vĩnh viễn cho việc một lần.

### Phương án E — Cho lệnh `core bootstrap` chạy lại bổ sung mục còn thiếu cho đơn vị hệ thống đã có

**Được:** giá trị chỉ nằm ở một chỗ, trong C#; không SQL viết tay; người dùng chạy một lệnh đã quen và vốn chạy lại được.

**Vì sao loại:** nó biến lệnh bootstrap thành một đường chạy lại seed riêng cho đơn vị hệ thống — đúng điều kiện kích hoạt 2 của ADR-0088, buộc chốt phạm vi của cả hai đường seed ngay trong lượt này. Và nó để lại trong Core vĩnh viễn một nhánh code cho một nhu cầu một lần.

### Phương án F — Dựng luôn lệnh `core seed-tenant-defaults`

**Vì sao loại:** cùng hai lý do với E, cộng thêm phần lớn hơn hẳn: lệnh mang luật E22 — mỗi đơn vị một transaction, chạy lại được trên ba loại dữ liệu.

### Phương án G — Xoá database dev rồi bootstrap lại

**Được:** không viết dòng nào.

**Vì sao loại:** mất dữ liệu đang có trên máy dev, và không dùng được cho bản cài nào có dữ liệu thật — đúng loại bản cài mà bước bù dữ liệu tồn tại để phục vụ.

### Phương án H — Đặt câu SQL bù dữ liệu ngay trong runbook, không thành tệp

**Được:** không thêm thư mục nào dưới `database/`.

**Vì sao loại:** câu nằm trong tài liệu thì không test nào chạy được nó, nên không gì giữ giá trị của nó khớp danh sách C#.

## Hệ quả

### Tích cực

- Người vận hành thấy đường vào màn đơn vị trên `Sidebar`, không phải nhớ URL — giải đúng phép đo ở §Bối cảnh.
- Không đổi lược đồ, không thêm bước lọc, không đẻ khoá quyền, seam không đổi, FE không rẽ nhánh theo cờ. Ranh giới vẫn do đơn vị giữ — cơ chế 1 của ADR-0017.
- Bất biến mà phương án B cần thành luật M16, ép tại service tạo đơn vị thay vì tại từng chỗ gọi.
- Script bù dữ liệu không vào chuỗi script schema: bản cài mới không bao giờ chạy nó.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| Tính đúng của mục menu treo vào M16 | Mục không đọc cờ; nó hiện cho mọi tài khoản của đơn vị hệ thống. Một đường sinh tài khoản **mới** chạm đơn vị hệ thống mà quên cờ, hay một dòng chèn bằng SQL tay, làm mục hiện cho người bấm vào là gặp 403. Không rò dữ liệu, nhưng tính chất *menu là thứ bạn làm được* thôi đúng — và không cổng nào thấy một đường sinh tài khoản chưa tồn tại; chỉ review |
| Đường seed rẽ nhánh theo `IsSystem` | Muốn biết đơn vị hệ thống có gì phải đọc hai danh sách. Ngày `core seed-tenant-defaults` được dựng, nó phải biết danh sách riêng này — thêm một chỗ phải khớp khi chốt phạm vi theo ADR-0088 |
| Giá trị của mục nằm ở hai chỗ | Danh sách C# và script bù dữ liệu. Test giữ hai chỗ khớp lúc viết; sau đó script đứng yên. Đổi nhãn hay icon về sau không tới bản cài đã có — như mọi mục seed khác hôm nay — và muốn tới thì mỗi lần đổi cần thêm một script bù dữ liệu |
| Thêm một loại tệp dưới `database/` không có bảng lịch sử | Database không ghi script bù dữ liệu đã chạy hay chưa; câu trả lời là chính dòng menu, kiểm bằng câu nghiệm thu. Thư mục `bu-du-lieu` mang luật riêng, cạnh `scripts/` và `dev/` |
| Cổng vẫn chỉ đọc nguồn của Core | Nguồn seed của module khai một mục trỏ vào `/he-thong` thì mục vào **mọi** đơn vị mà không cổng nào đỏ — như ADR-0079 đã ghi; ADR này không đóng chỗ đó |
| Hai loại đơn vị có menu khác nhau bằng dữ liệu | Kiểm thử giao diện phải đăng nhập bằng tài khoản vận hành mới thấy mục; lối tắt của trang chủ dựng từ cùng cây menu nên cũng đổi theo |

### Dấu hiệu quyết định này bắt đầu sai

1. Khu `/he-thong` có màn thứ hai và cần nhóm dưới một mục cha, hoặc cần ẩn hiện theo từng tài khoản vận hành — phương án A tới hạn.
2. Có nhu cầu một tài khoản trong đơn vị hệ thống **không** mang cờ vận hành — tài khoản chỉ xem, tài khoản kỹ thuật. M16 phải nới, và mục menu không còn đúng người: quay lại phương án A.
3. Câu nghiệm thu M16 ra khác 0 trên một bản cài nào đó.
4. Module đầu tiên muốn đặt màn vào khu hệ thống — phương án B' tới hạn.
5. Người vận hành vẫn báo không thấy đường vào — lúc đó vấn đề là thiết kế, thuộc `design-expert`, không phải cơ chế.

### Rút lui nếu sai

1. Một lượt BE chạm Core, qua `core-reviewer`: gỡ nhánh `IsSystem` khỏi đường seed và danh sách riêng khỏi `CoreTenantSeedSource`; cổng M17 về lại một vế — không nguồn nào có mục `/he-thong`.
2. Mỗi bản cài đã có mục: người dùng xoá mềm dòng đó bằng một câu `UPDATE` đặt `is_deleted = true` cho mục mã `he-thong-don-vi` của đơn vị hệ thống. Không bảng nào trỏ vào dòng đó — mục không có dòng `core.menu_item_role`. Tệp script bù dữ liệu xoá khỏi repo cùng lượt.
3. M16 giữ nguyên: nó đúng và vô hại khi không còn mục nào dựa vào nó.
4. ADR mới ghi việc rút lui; `design-expert` trả hai spec màn về đường vào bằng URL.

Nếu rút lui là để sang phương án A, bước 2 đổi thành: lượt di trú thêm cột đánh dấu dòng `he-thong-don-vi` mang cờ mới, rồi gỡ nhánh seed. Cả hai hướng không chạm dữ liệu nghiệp vụ nào.

## Sáu câu hỏi

| Câu | Trả lời |
| --- | --- |
| Vấn đề thật, đã đo chưa | Một báo cáo của người dùng ngày 2026-10-08 — điều kiện kích hoạt 3 của ADR-0079, nơi một lần là đủ. Chưa đo tần suất; với một người vận hành, không có gì để đo thêm |
| Phương án đơn giản hơn | Phương án C — viết rõ URL. Không đủ: chính người có tài liệu đã không tìm ra |
| Chi phí vận hành thêm | Một nhánh seed và một danh sách trong Core; một luật phải giữ ở mọi đường sinh tài khoản về sau; một script mỗi bản cài cũ chạy tay một lần; một thư mục mới dưới `database/`. Không hạ tầng, không phụ thuộc ngoài |
| Ai bảo trì | Người giữ Core — hôm nay là người dùng; mỗi lượt chạm qua `core-reviewer`. M16 không có người canh riêng: hai test và review của từng đường sinh tài khoản mới. Đây là chỗ yếu, nói ra |
| Rút lui thế nào | Mục *Rút lui nếu sai* ở trên — một lượt BE và một câu SQL mỗi bản cài |
| Core có phải biết nghiệp vụ không | Không. Khu hệ thống là của Core; mục trỏ vào màn của Core; Core không biết tên module nào. Qua phép thử ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.1; ngưỡng §4.2 không áp vì đây không phải code một module cần, mà là đường vào một màn Core đã có |

## Thi công

| # | Việc | Ai | Điều kiện |
| --- | --- | --- | --- |
| 1 | Danh sách riêng của đơn vị hệ thống, nhánh `IsSystem` ở đường seed, `TenantSeedValidator` kiểm tập đơn vị hệ thống nhận — điểm 2 | `backend-expert` | Sau khi `design-expert` chốt nhãn, khoá dịch, icon, thứ tự, vị trí |
| 2 | Phép ép M16 trong service tạo đơn vị cùng hai test của M16 — điểm 3 | `backend-expert` | Không chờ |
| 3 | Vế đơn vị hệ thống của cổng M17 — điểm 4 | `backend-expert` | Cùng lượt với việc 1 |
| 4 | Script bù dữ liệu và test chạy chính tệp đó — điểm 5 | `backend-expert` | Cùng lượt với việc 1 |
| 5 | Ca biên ngoài các test trên | `test-engineer` | Sau việc 1–4 |
| 6 | Khoá dịch của mục trong `src/FE/public/i18n/vi.json` | `frontend-expert` | Sau spec của `design-expert` |
| 7 | Tài liệu theo code: card [`../contracts/meta-menu.md`](../contracts/meta-menu.md) §1; [`../database/script-runbook.md`](../database/script-runbook.md) — thư mục `bu-du-lieu` ở §1, cách chạy script bù dữ liệu, câu nghiệm thu M16 và M17 ở §3.3; [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4; ô trạng thái M16, M17 ở [`../RULES-BE.md`](../RULES-BE.md) §9 | `architect` | Sau lượt BE |
| 8 | Chạy script bù dữ liệu trên máy dev | Người dùng | Sau khi lượt BE vào |

## Quan hệ với các ADR trước

**ADR-0079 — sửa một phần.**

- Hết hiệu lực: quyết định 1 — khu `/he-thong` không có đường vào nhìn thấy được — đối với `/he-thong/don-vi`; câu mở đầu của quyết định 2 — không cơ chế nào được dựng cho mục đích này.
- Còn hiệu lực: ba điều cấm cụ thể của quyết định 2 — không thêm cột vào `core.menu_item`, không mở `ITenantSeedSource` theo hướng nhắm riêng đơn vị hệ thống, không cho khung hay router rẽ nhánh theo cờ. Quyết định 3 — cổng sửa cùng lượt với cơ chế, đúng như nó dặn. Phương án A vẫn hoãn, phương án C vẫn loại. Điều kiện kích hoạt 1, 2, 4, 6 còn là căn cứ xét lại phương án A. Điều kiện 5 — bất biến được chốt và có cổng — ADR này đáp ứng bằng M16, đủ khi hai test của M16 chạy.

**ADR-0088 — bổ sung.** Đơn vị hệ thống vẫn nhận seed của mọi nguồn như mọi đơn vị mới; ADR này thêm một mục chỉ nó nhận, và không trả lời câu hỏi ADR-0088 hoãn. Khi điều kiện kích hoạt 2 của ADR-0088 xảy ra, phạm vi chốt lúc đó phải tính cả danh sách riêng này.

**ADR-0017 — không đổi.** ADR này dùng đúng cơ chế 1 của nó: ranh giới do đơn vị giữ.

## Liên quan

- [`0079-khu-he-thong-khong-nhan-duong-vao-nhin-thay-duoc-o-v1.md`](0079-khu-he-thong-khong-nhan-duong-vao-nhin-thay-duoc-o-v1.md) — phương án B và bất biến nó cần.
- [`0088-seed-don-vi-he-thong-hoan-co-dieu-kien.md`](0088-seed-don-vi-he-thong-hoan-co-dieu-kien.md) — đơn vị hệ thống nhận seed của mọi nguồn.
- [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) · [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) — khu hệ thống, cờ vận hành, ràng buộc loại trừ.
- [`../database/schema-core.md`](../database/schema-core.md) §6 — bảng menu và ba bước lọc.
- [`../database/migration-policy.md`](../database/migration-policy.md) §4.1 · [`../database/script-runbook.md`](../database/script-runbook.md) §3.1 — vì sao bù dữ liệu không đi trong migration; khối tự chặn.
- [`../RULES-BE.md`](../RULES-BE.md) §9 — luật M9, M11, M16, M17.
