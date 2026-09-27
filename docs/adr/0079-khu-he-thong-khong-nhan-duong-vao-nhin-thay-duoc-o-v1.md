---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0079 — Khu `/he-thong` không nhận đường vào nhìn thấy được ở v1; đường vào là URL gõ thẳng và cổng chặn mục menu giữ nguyên

> **Trạng thái:** Đã chấp nhận (2026-09-24)
>
> **Bổ sung [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md).** ADR-0017 chốt *khu hệ thống tồn tại và ai vào được*; nó không nói **người vận hành tới đó bằng cách nào**. ADR này trả lời đúng câu đó và không chạm điều nào của ADR-0017.
>
> Đây là một ADR thuộc nhóm **quyết định KHÔNG làm**, loại **hoãn** — [`../wiki-core/be/08-adr-practice.md`](../wiki-core/be/08-adr-practice.md) §3.1. Mục *Điều kiện kích hoạt* ở cuối là mục bắt buộc của nhóm này.

## Bối cảnh

Khu quản trị hệ thống đã có màn hình thật và đã có guard. Thứ chưa có là **đường vào**: hôm nay người vận hành tới `/he-thong/don-vi` bằng cách gõ URL.

`design-expert` đã trả lời phần thuộc về thiết kế trong lượt 2026-09-23 và ghi thành quyết định tường minh ở [`../Design/Screens/00-khung-ung-dung.md`](../Design/Screens/00-khung-ung-dung.md) (bảng *Quyết định bố cục*) và [`../Design/Screens/20-don-vi.md`](../Design/Screens/20-don-vi.md): **màn đơn vị không nằm trong `Sidebar`**, và căn cứ là thiết kế chứ không phải một giới hạn kỹ thuật — `Sidebar` trả lời *"trong đơn vị của tôi, tôi làm được gì"*, còn `/he-thong` trả lời *"có những đơn vị nào"*; khác người dùng, khác phạm vi. Câu còn mở mà `design-expert` giao sang đây là câu về **cơ chế**: nếu một ngày cần một đường vào nhìn thấy được thì nó đi bằng cái gì.

Bốn phép đo lấy ngày 2026-09-24, trên mã nguồn và dữ liệu seed hôm nay:

- **Mô hình dữ liệu menu không diễn đạt được cờ.** `database/scripts/core/0001__core__initial.sql` — khối `CREATE TABLE core.menu_item (` — mang `required_permission_id uuid` và một bảng nối `CREATE TABLE core.menu_item_role (`. Không cột nào nhắc `is_system_operator`. Ba bước quyết định một mục có hiện hay không ([`../database/schema-core.md`](../database/schema-core.md) §6.3) vì thế chỉ đọc được **quyền**, **vai trò**, hoặc *"mọi người đã đăng nhập"*.
- **Nguồn seed của Core không có mục nào trỏ vào khu đó.** `src/BE/Core/CoreAndSkill.Core.Infrastructure/Tenants/CoreTenantSeedSource.cs` — hàm `GetMenuItems()` — không trả mục nào có `route` bắt đầu bằng `/he-thong`. Nhóm `"menu": {` của `src/FE/public/i18n/vi.json` cũng không có khoá nhãn nào cho khu hệ thống.
- **Đã có một cổng chặn đường rò, và nó là cổng chặn chứ không phải lời khai.** `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Tenants/CoreTenantSeedMenuTests.cs` — chuỗi `CoreSeedMenu_HasNoItemPointingIntoTheSystemOperatorArea` — làm đỏ ngay khi một mục seed trỏ vào `/he-thong`. Chú thích ngay trên nó nói rõ nó chờ một ADR, và **đừng gỡ nó** để một mục lọt qua.
- **Cờ đi bằng một đường riêng, đã có tên.** `src/FE/src/app/core/guards/system-operator.guard.ts` — chuỗi `laVanHanhHeThong()` — là lớp chặn duy nhất của khu này ở FE; phía BE là `[RequireSystemOperator]` ([`0024-ba-muc-khai-bao-phan-quyen-endpoint.md`](0024-ba-muc-khai-bao-phan-quyen-endpoint.md)). Cờ là một cột boolean, không phải một khoá quyền ([`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)).

Ràng buộc có thật, và nó giải thích vì sao câu hỏi này không tự tan: **ba tệp đang mang một dòng "chờ `architect`"** — bảng *Cần chốt* của `00-khung-ung-dung.md`, banner của [`../contracts/meta-menu.md`](../contracts/meta-menu.md) §1, và chú thích của chính test trên. Một câu hỏi mở nằm ở ba chỗ là một lời mời: người gặp nó lần sau, dưới áp lực của một việc khác, sẽ thêm một mục menu theo đường hiện có — và đường hiện có chỉ có hai lối, cả hai đều sai. Không gắn quyền thì mục hiện cho **mọi người dùng của mọi đơn vị**; gắn một quyền thì dựng đúng đường phân quyền thứ hai mà cơ chế 2 của ADR-0017 và luật **M9** ([`../RULES.md`](../RULES.md) §9) tồn tại để chặn.

Cái chưa ai đo được, và nó quyết định hình dạng của quyết định này: **chưa có phép đo nào cho thấy việc gõ URL đang gây thiệt hại.** Không có báo cáo người vận hành lạc đường, không có số lần thao tác hỏng vì gõ sai. Khu `/he-thong` hôm nay có **đúng một** màn.

## Quyết định

`architect` chốt ba điều:

1. **Khu `/he-thong` không nhận đường vào nhìn thấy được ở v1.** Đường vào là **URL gõ thẳng**, và `systemOperatorGuard` là lớp chặn duy nhất. Đây là trạng thái **đã quyết**, không phải chỗ còn sót — mọi tệp mô tả nó phải nói như vậy.
2. **Không cơ chế nào được dựng cho mục đích này ở v1.** Không thêm cột vào `core.menu_item`, không mở `ITenantSeedSource` theo hướng nhắm riêng đơn vị hệ thống, không cho khung hay router rẽ nhánh theo cờ `isSystemOperator`. Ba phương án đó được xét ở mục dưới và **hoãn**, không bị bác về nguyên tắc.
3. **Cổng `CoreSeedMenu_HasNoItemPointingIntoTheSystemOperatorArea` giữ nguyên, và nó đổi nghĩa.** Từ hôm nay nó không còn là *"đang chờ một quyết định"* mà là *"thi hành quyết định 1"*. Ngày nào một điều kiện kích hoạt ở cuối ADR này xảy ra và có ADR mới chốt cơ chế, cổng đó được **sửa cùng lượt** với cơ chế; gỡ nó để một mục lọt qua là vi phạm ADR này.

Ba dòng "chờ `architect`" nêu ở §Bối cảnh trỏ về đây thay vì trỏ vào một khoảng trống. Việc sửa dòng ở hai tệp `../Design/Screens/` thuộc `design-expert`; dòng ở [`../contracts/meta-menu.md`](../contracts/meta-menu.md) sửa trong lượt này.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Thêm một cột trên `core.menu_item` và một bước lọc thứ tư

Cột kiểu `requires_system_operator boolean`, và một bước đọc nó chen vào thứ tự ở [`../database/schema-core.md`](../database/schema-core.md) §6.3.

**Được:** giải đúng vấn đề ở đúng tầng. Menu vẫn là dữ liệu, FE vẫn không biết gì về cờ, và mọi màn tương lai của khu hệ thống dùng lại cùng cơ chế mà không thêm gì.

**Vì sao hoãn:** ba lý do, xếp theo sức nặng.

Thứ nhất, nó **đổi lược đồ của một bảng Core**. Cái giá không nằm ở dòng `ALTER TABLE` mà nằm ở chỗ mọi dự án hạ nguồn đã cài đều phải chạy một bước di trú cho một tính năng họ có thể không bao giờ dùng — khu hệ thống có đúng một màn và một nhóm người dùng rất nhỏ.

Thứ hai, nó làm **thứ tự lọc menu dài thêm một bước**, và thứ tự đó là thứ `schema-core.md` §6.3 cố ý mô tả bằng câu *"đúng một thứ tự, không có nhánh nào khác"*. Bước thứ tư không đắt hôm nay; cái đắt là nó mở tiền lệ cho bước thứ năm, và một bộ lọc bốn nhánh không còn kiểm được bằng mắt.

Thứ ba, và đây là lý do khiến nó là *hoãn* chứ không phải *loại*: nó **rẻ hơn hẳn nếu một ngày `core.menu_item` phải nhận thêm một chiều diễn đạt vì lý do khác**. Lúc đó cột này đi ghép vào một lượt di trú đã có, và hai lý do đầu biến mất. Trả cái giá đó hôm nay, cho một màn duy nhất mà một người biết URL là đủ dùng, là mua sớm.

### Phương án B — Seed mục menu **chỉ vào đơn vị hệ thống**

`core.menu_item` có `tenant_id`, và đơn vị hệ thống là một đơn vị riêng nhận ra bằng cột `is_system` ([`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)). Một mục trỏ vào `/he-thong` gieo **chỉ** vào đơn vị đó, không gắn quyền, sẽ rơi vào bước 3 và hiện cho mọi người dùng **của riêng đơn vị đó**.

**Được:** đây là phương án rẻ nhất trong bốn phương án, và cái rẻ của nó là thật — **không** đổi lược đồ, **không** thêm bước lọc, **không** đẻ khoá quyền, **không** cho FE biết về cờ. Nó dùng đúng chiều phân tách mà ADR-0017 cơ chế 1 đã dựng: ranh giới do đơn vị giữ, không do một lớp kiểm quyền mới.

**Vì sao hoãn:** nó đúng **chỉ khi** một bất biến được giữ, và bất biến đó hôm nay **chưa được phát biểu, chưa được ép, và chưa chắc đúng**:

> Mọi tài khoản thuộc đơn vị hệ thống đều mang cờ `is_system_operator`.

Luật **M9** ([`../RULES.md`](../RULES.md) §9) nói chiều ngược lại — tài khoản **mang cờ** thì không được gán vai trò nghiệp vụ. Nó không nói gì về một tài khoản **nằm trong đơn vị hệ thống mà không mang cờ**. Nếu một tài khoản như vậy tồn tại được, mục menu này hiện cho nó, và nó bấm vào rồi bị `systemOperatorGuard` đá ra màn 403 — một mục menu dẫn tới màn từ chối là lỗi giao diện, không phải lỗ rò dữ liệu, nhưng nó biến *"menu là thứ bạn làm được"* thành *"menu là thứ có thể bạn làm được"*, và tính chất đó thì mọi mục menu khác trong hệ thống đang giữ.

Chuyển phương án này thành khả thi vì vậy **không** phải là viết một dòng seed — nó là chốt thêm một bất biến về đơn vị hệ thống và dựng cổng ép bất biến đó. Đó là một quyết định riêng, đáng một ADR riêng, và nó đáng làm **vào ngày có lý do làm**, không phải hôm nay.

### Phương án C — Không đụng menu; cho tài khoản mang cờ đáp xuống `/he-thong/don-vi` sau khi đăng nhập

Phương án đơn giản nhất và là phương án phải nêu ra trước khi bàn hai phương án trên: không dữ liệu nào đổi, không lược đồ nào đổi, chỉ một nhánh chọn tuyến đích sau đăng nhập.

**Được:** người vận hành không bao giờ phải gõ URL, và chi phí gần bằng không.

**Vì sao loại — và đây là *loại*, không phải *hoãn*:** nó không giải vấn đề mà nó có vẻ giải. Đáp xuống đúng chỗ chỉ đúng **một lần**, ở đầu phiên; người vận hành điều hướng đi chỗ khác rồi thì vẫn không có đường quay lại nào ngoài URL. Một cơ chế chỉ hoạt động ở lần đầu tiên của phiên là một cơ chế người dùng sẽ tin rồi mất, và cái mất đó tệ hơn việc chưa bao giờ có. Nó còn đặt cờ `isSystemOperator` vào một đường rẽ nhánh thứ hai ở FE, trong khi [`../Design/Screens/00-khung-ung-dung.md`](../Design/Screens/00-khung-ung-dung.md) đã chốt khung **không** rẽ nhánh theo cờ — và *nơi người dùng đáp xuống* là câu hỏi thuộc `design-expert`, không thuộc ADR này.

### Phương án D — Làm ngay, chọn A hoặc B, để đóng ba dòng "chờ `architect`"

**Được:** ba tệp hết dòng chờ, và câu hỏi không quay lại.

**Vì sao loại:** đây là lý do tệ nhất để ra một quyết định kiến trúc — nó lấy *sự khó chịu vì có một câu hỏi mở* làm căn cứ, trong khi câu hỏi mở đó **không** gây thiệt hại nào đo được. ADR này đóng ba dòng chờ bằng một câu trả lời thật (*"đã quyết: không làm, và đây là điều kiện xét lại"*), tốn đúng một tệp, và không nợ dự án hạ nguồn nào một bước di trú.

## Hệ quả

### Tích cực

- Ba dòng "chờ `architect`" đổi từ **câu hỏi mở** thành **quyết định có điều kiện xét lại**. Người gặp chúng lần sau đọc được vì sao chưa làm, thay vì đọc được rằng chưa ai nghĩ tới.
- Cổng `CoreSeedMenu_HasNoItemPointingIntoTheSystemOperatorArea` có một lý do nêu được thành câu, nên nó thôi là một cổng "tạm" mà ai đó thấy phiền sẽ gỡ.
- Không dự án hạ nguồn nào phải chạy di trú cho một tính năng chưa ai cần.
- Phép đo về bất biến ở phương án B được ghi lại. Ngày có người quay lại câu hỏi này, họ bắt đầu từ chỗ đã biết *điều kiện để phương án rẻ nhất trở thành đúng*, thay vì phát hiện lại nó.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| Người vận hành phải **nhớ một URL** | Tri thức vận hành sống trong đầu người, không trong sản phẩm. Người vận hành thứ hai phải được ai đó chỉ cho. Đây là cùng loại với thứ mà [ADR-0017](0017-khu-quan-tri-he-thong.md) §Phương án A đã từ chối một lần — ở đó là *"phải có quyền vào máy chủ"*, ở đây nhẹ hơn nhiều nhưng cùng hình dạng |
| Một đường vào không nhìn thấy được là một đường **ít được đi qua** | Khu `/he-thong` hỏng sẽ không ai biết cho tới lần có người cần dùng, và lần đó thường là lúc gấp |
| Cổng chỉ quét **nguồn seed của Core**, không quét dữ liệu đang chạy | Test đọc `CoreTenantSeedSource`; một hàng `core.menu_item` chèn bằng SQL tay hoặc do một dự án hạ nguồn tự gieo **không** bị nó bắt. Lớp chặn thật cho ca đó vẫn chỉ là `systemOperatorGuard` phía FE và `[RequireSystemOperator]` phía BE — mục menu sai chỉ dẫn tới 403, không dẫn tới dữ liệu |
| Quyết định này đóng vòng lặp **bằng giấy**, không bằng cơ chế | Không gì ngăn một lượt sau thêm một mục menu ngoài một test và ADR này. Nếu ca đó xảy ra, đó là bằng chứng giấy không đủ, và là một điều kiện kích hoạt |

### Điều kiện kích hoạt — xét lại quyết định này khi

Đây là quyết định **hoãn**, nên nó phải nói ra thứ làm nó hết hạn. Một trong các sự kiện sau, quan sát được, là đủ:

1. **Khu `/he-thong` có màn thứ hai.** Lúc đó "gõ URL" nhân lên theo số màn, và một trang đích của khu trở thành thứ phải có chứ không phải thứ tiện.
2. **Có người vận hành thứ hai không phải người cài đặt hệ thống** — tức tri thức "URL là gì" phải truyền tay lần đầu tiên.
3. **Một phép đo về thiệt hại thật:** một người vận hành báo không tìm ra đường vào, hoặc một thao tác bị chậm hay hỏng vì gõ sai URL. Một lần là đủ để mở lại; đây là phép đo mà §Bối cảnh ghi nhận là đang thiếu.
4. **`core.menu_item` phải nhận thêm một chiều diễn đạt vì một lý do khác.** Phương án A đi ghép vào lượt đó và mất gần hết cái giá của nó.
5. **Bất biến "mọi tài khoản của đơn vị hệ thống đều mang cờ `is_system_operator`" được chốt và có cổng ép** — vì lý do nào cũng được. Phương án B khi đó trở thành đúng và gần như miễn phí.
6. **Có người thêm một mục menu trỏ vào `/he-thong` bất chấp cổng** — tức bằng chứng rằng một ADR và một test không đủ giữ ranh giới này.

Sự kiện nào xảy ra thì viết **ADR mới**, và sửa cổng ở quyết định 3 **trong cùng lượt** với cơ chế.

## Liên quan

- [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) — khu hệ thống tồn tại vì sao, và ba cơ chế giữ ranh giới dữ liệu.
- [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) — `is_system_operator` là cột boolean; đơn vị hệ thống nhận ra bằng `is_system`.
- [`0024-ba-muc-khai-bao-phan-quyen-endpoint.md`](0024-ba-muc-khai-bao-phan-quyen-endpoint.md) — `[RequireSystemOperator]` là mức phân quyền riêng của khu này.
- [`../database/schema-core.md`](../database/schema-core.md) §6.3 — ba bước quyết định một mục menu có hiện hay không.
- [`../contracts/meta-menu.md`](../contracts/meta-menu.md) §1 — hợp đồng của endpoint trả cây menu.
- [`../Design/Screens/00-khung-ung-dung.md`](../Design/Screens/00-khung-ung-dung.md) — quyết định bố cục: `Sidebar` không có mục nào trỏ vào khu hệ thống.
- [`../RULES.md`](../RULES.md) §9 — luật M9.
