---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0023 — Một service C# tạo đơn vị dùng chung cho lệnh bootstrap và khu quản trị hệ thống; dev và bản cài thật chạy cùng một lệnh

> **Trạng thái:** Đã chấp nhận (2026-09-14)
>
> **Sửa một phần [`0022-seed-dev-khong-co-duong-code-rieng.md`](0022-seed-dev-khong-co-duong-code-rieng.md).** Bỏ tệp `.sql` dựng dữ liệu dev và script bọc. Giữ nguyên câu trung tâm của ADR đó: **không có đường code nào chứa sẵn thông tin tài khoản.**

## Bối cảnh

Ba tài liệu đang tả ba cách khác nhau cho cùng một việc — dựng một bản cài từ database trống tới lúc có người đăng nhập được. Số dòng dưới đây là số dòng ngày 2026-09-14.

| Tài liệu | Ai tạo đơn vị nghiệp vụ đầu tiên | Ai dựng vai trò, ánh xạ quyền, menu |
| --- | --- | --- |
| `docs/database/script-runbook.md:204` — §3.3, bản cài thật | Endpoint tạo đơn vị của khu quản trị hệ thống, sau khi tài khoản vận hành đăng nhập; lệnh dòng lệnh *"cố ý không có"* (`docs/database/script-runbook.md:240`) | Endpoint đó, bằng C# |
| `docs/database/script-runbook.md:624` — §6, dev · `docs/luong/V1-cai-dat-lan-dau.md:38` | Tệp `.sql` trong `src/BE/` | Tệp `.sql` đó |
| `docs/wiki-core/be/17-multi-tenant.md:289` | Lệnh bootstrap chạy tay | Không nói |

Hai trong ba cách dựng vai trò và menu bằng **hai công cụ khác nhau**. Chính runbook đã gọi tên hệ quả: hai đường cho cùng một việc lệch nhau ngay lần đầu có người sửa một bên (`docs/wiki-core/be/ly-do/script-runbook.md:114`). Và vì đường SQL là đường **dev**, thứ chạy mỗi ngày trên máy lập trình viên không phải thứ chạy trên bản cài thật.

Ba lỗ khác treo cùng chỗ:

1. **Bộ vai trò mặc định thuộc dự án** (`docs/wiki-core/be/17-multi-tenant.md:349`), nhưng không có seam nào cho dự án khai nó. Luồng tạo đơn vị ghi thẳng: *"một seam chưa có tên"* (`docs/luong/V2-tao-don-vi-moi.md:68`).
2. **Tài khoản vận hành gõ gì vào ô mã đơn vị.** Form đăng nhập luôn đòi mã đơn vị, còn đơn vị hệ thống nhận ra bằng cột `is_system` chứ không bằng mã (`docs/luong/D1-dang-nhap.md:21`). Cùng lúc, một tài liệu đòi loại đơn vị hệ thống khỏi mọi chỗ liệt kê, *"kể cả form đăng nhập"* (`docs/wiki-core/be/17-multi-tenant.md:294`).
3. **Tài khoản vận hành quên mật khẩu thì không có đường nào.** Khu quản trị hệ thống cần chính tài khoản đó để vào, và mật khẩu băm không dựng được bằng SQL (`docs/luong/V1-cai-dat-lan-dau.md:63`).

Ràng buộc có thật lúc quyết định: chưa có `src/`; tiến trình API phục vụ thật **không seed gì** lúc khởi động (`docs/database/script-runbook.md:264`); mật khẩu không nằm trong repo ([`0022-seed-dev-khong-co-duong-code-rieng.md`](0022-seed-dev-khong-co-duong-code-rieng.md)); danh mục khoá quyền đã có một seam gộp nhiều nguồn và kiểm lúc khởi động (`docs/quy-uoc/be-architecture.md:121`, `docs/quy-uoc/be-architecture.md:131`) — khuôn đó dùng lại được.

## Quyết định

> **Core có đúng một service tạo đơn vị, `ITenantProvisioningService`. Lệnh bootstrap và endpoint tạo đơn vị của khu quản trị hệ thống cùng gọi nó. Dev và bản cài thật dựng môi trường bằng cùng một lệnh; không có tệp `.sql` dữ liệu dev.**

### 1. Service và chỗ đặt

| Thành phần | Nằm ở | Vai |
| --- | --- | --- |
| `ITenantProvisioningService` | Interface ở `Core.Application`, hiện thực ở `Core.Infrastructure` | **Một** hiện thực cho mọi lối vào — lệnh bootstrap và endpoint của khu quản trị hệ thống gọi cùng nó, nên một đơn vị dựng từ dòng lệnh và một đơn vị dựng từ màn hình không thể khác nhau. Danh sách thao tác của service sống ở [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1; ADR này không chép, và [`0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) §3, §5 là nơi hai thao tác không-phải-tạo được quyết định |
| Runner lệnh vận hành `RunCoreCommandAsync` | `Core.Web` | Nhận tham số dòng lệnh, chạy lệnh rồi thoát. **Không bao giờ mở cổng** |
| `Program.cs` của host | `CoreAndSkill.Api` | Đúng một dòng cho việc này: `if (await app.RunCoreCommandAsync(args)) return;` |

Ngoài service này, **không đường code nào** thêm dòng vào bảng đơn vị, đặt `is_system`, hay tạo tài khoản mang `has_permission_bypass` hoặc `is_system_operator`. Service nằm trong allowlist mở phạm vi ngữ cảnh thực thi (luật A12) và allowlist vị trí gán cờ bypass (luật M12) với **một** mục cho cả service, thay vì một mục cho mỗi lối vào hay mỗi thao tác.

**Mỗi thao tác của service chạy trong một transaction.** Với việc tạo đơn vị: đơn vị, tài khoản, vai trò, ánh xạ quyền, menu và các dòng nhật ký kiểm toán của lần tạo đó cùng commit, hoặc không dòng nào được ghi. Hỏng ở bất kỳ bước nào thì không còn dòng nào trong database, và mã đơn vị chưa bị chiếm. `core bootstrap` gọi hai thao tác tạo nên là hai transaction: đơn vị hệ thống đã commit mà đơn vị nghiệp vụ hỏng thì chạy lại lệnh — quy tắc 3 ở §3 bỏ qua phần đã có.

### 2. Các lệnh

Runner chỉ nhận lệnh khi tham số đầu tiên là `core`. Không phải thì nó trả `false` và host phục vụ HTTP như thường. `core` kèm một động từ không biết → báo lỗi, thoát mã khác 0, không mở cổng.

Danh sách lệnh và tham số: [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3, mục *Lệnh của runner* (chủ). ADR này chốt hai lệnh đầu — `core bootstrap` (đơn vị hệ thống + tài khoản vận hành, rồi đơn vị nghiệp vụ đầu tiên + tài khoản quản trị) và `core reset-operator-password` (§5); lệnh chạy lại seed cho mọi đơn vị khi lắp module mới cũng đi qua cùng runner.

Cách gọi đầy đủ và tên khoá cấu hình thuộc [`../database/script-runbook.md`](../database/script-runbook.md).

### 3. `core bootstrap` tạo gì

| Tạo | Cờ | Thuộc |
| --- | --- | --- |
| Đơn vị hệ thống | `is_system` | — |
| Tài khoản vận hành (ví dụ tên `superadmin`) | `is_system_operator` · `must_change_password` | Đơn vị hệ thống |
| Đơn vị nghiệp vụ đầu tiên | — | — |
| Tài khoản quản trị đơn vị (ví dụ tên `admin`) | `has_permission_bypass` · `must_change_password` | Đơn vị nghiệp vụ đầu tiên |

`must_change_password` trên tài khoản vận hành là ràng buộc mà [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) đặt cho loại tài khoản này; ADR này không đổi nó. Lý do thêm, riêng cho lệnh này: mật khẩu ban đầu nằm dạng chữ thường trong kho cấu hình của máy — buộc đổi ở lần đăng nhập đầu làm giá trị đó hết dùng được.

Năm quy tắc của lệnh:

1. **Mọi giá trị đọc qua hệ cấu hình**: mã và tên hai đơn vị, tên đăng nhập và mật khẩu hai tài khoản. Trên máy dev, giá trị bí mật nằm ở `user-secrets`. Tên đăng nhập là **dữ liệu**, không phải vai trò (luật S10).
2. **Kiểm đủ đầu vào trước dòng ghi đầu tiên.** Thiếu một giá trị → dừng, **không ghi dòng nào**, thoát mã khác 0.
3. **Chạy lại không nhân đôi.** Đơn vị hệ thống đã có (nhận ra bằng `is_system`) → bỏ qua; đơn vị mang mã đó đã có → bỏ qua; tài khoản đã có → bỏ qua. **Không bao giờ ghi đè mật khẩu hay cờ của tài khoản đã tồn tại.**
4. **Không ghi danh mục khoá quyền.** Danh mục đi bằng migration; tài khoản database mà lệnh chạy bằng chỉ đọc được hai bảng danh mục.
5. **Ghi nhật ký kiểm toán** cho mỗi đơn vị và mỗi tài khoản tạo ra, người thực hiện là hệ thống — lệnh không chạy dưới tài khoản nào.

### 4. Seam `ITenantSeedSource`

Phần thuộc **quyết định**: dữ liệu mặc định của một đơn vị mới không nằm trong Core, mà đi qua một seam **nhiều nguồn** dựng theo đúng khuôn `IPermissionCatalogSource` — cùng cơ chế, cùng chỗ kiểm, để không có khuôn thứ hai phải học. Ràng buộc buộc phải chọn như vậy là luật A3: Core dựng đơn vị nhưng **không được biết module nào tồn tại**, nên thứ Core không biết phải do chính module khai. Hệ quả đi kèm là luật S1 — nguồn của Core không khai vai trò nào.

> 📖 Bảng luật của seam (ai đăng ký, Core gộp thế nào, kiểm vào lúc nào, ca không nguồn nào khai vai trò) và chữ ký kiểu: đọc [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1, mục *Nguồn seed cho đơn vị mới*. ADR này không chép.

Seed hỏng giữa chừng thì cả lần tạo đơn vị rollback — quy tắc transaction ở §1. Không có đơn vị nào ở trạng thái *"đã tạo nhưng thiếu seed"*.

### 5. `core reset-operator-password` — khôi phục chính tài khoản vận hành

Lệnh đọc tên đăng nhập và mật khẩu mới qua hệ cấu hình. Nó chỉ nhận một tài khoản **mang `is_system_operator`, thuộc đơn vị hệ thống**; không thấy tài khoản, hoặc tài khoản không mang cờ đó → dừng, không ghi gì, thoát mã khác 0. Thành công thì: đặt mật khẩu qua `UserManager`, bật `must_change_password`, đổi security stamp để mọi phiên đang mở trượt, ghi một dòng nhật ký kiểm toán ở đơn vị hệ thống.

Lệnh này là đường **duy nhất** đặt lại mật khẩu của tài khoản vận hành; không endpoint nào làm được việc đó. Lý do giống lệnh cài đặt (`docs/luong/V1-cai-dat-lan-dau.md:21`): ai chạy được lệnh trên máy chủ thì vốn đã có toàn quyền, nên lệnh không trao thêm quyền cho ai — còn một endpoint làm được việc đó là một đường leo thang mở ra mạng.

Khôi phục tài khoản quản trị **của một đơn vị nghiệp vụ** là việc khác, đi qua khu quản trị hệ thống: [`0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md).

### 6. Mã đơn vị hệ thống

Người vận hành **đặt mã đơn vị hệ thống qua cấu hình** lúc chạy `core bootstrap`. Tài khoản vận hành gõ mã đó vào ô mã đơn vị trên form đăng nhập — cùng một luồng đăng nhập với mọi người, không có nhánh riêng. Code vẫn nhận ra đơn vị hệ thống bằng cột `is_system`, không bằng mã ([`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)).

Yêu cầu *"loại đơn vị hệ thống khỏi mọi chỗ liệt kê đơn vị cho người dùng chọn"* giữ nguyên. Ô mã đơn vị là ô gõ, không phải danh sách chọn, nên nó không phải một chỗ liệt kê.

### 7. Phần của ADR-0022 bị sửa

| ADR-0022 | Theo ADR này |
| --- | --- |
| Tệp `.sql` idempotent trong `src/BE/` dựng dữ liệu dev | **Bỏ.** Dữ liệu đơn vị đi bằng `ITenantProvisioningService` và `ITenantSeedSource` |
| Script bọc gọi `psql` rồi lệnh bootstrap | **Bỏ.** Không còn hai bước khác công cụ để bọc |
| Lệnh bootstrap tạo tài khoản, mật khẩu từ `user-secrets` | **Giữ**; lệnh tạo thêm hai đơn vị |
| Không đường code nào chứa sẵn thông tin tài khoản | **Giữ nguyên** |
| Không nới chính sách mật khẩu ở dev (luật S9) | **Giữ nguyên** |

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Đường theo runbook §3.3: bootstrap chỉ dựng đơn vị hệ thống; đơn vị nghiệp vụ đầu tiên đi qua endpoint

**Được:** chỉ **một** lối vào tạo đơn vị nghiệp vụ, và lối đó là HTTP có nhật ký kiểm toán. Lệnh bootstrap nhỏ nhất có thể. Đây là phương án có lập luận tốt nhất trong ba bản đang tồn tại.

**Mất:** muốn có một đơn vị dùng được thì phải chạy host, mở FE, đăng nhập tài khoản vận hành, đổi mật khẩu, rồi thao tác màn hình. Với môi trường dev dựng lại mỗi ngày và với bộ integration test cần sẵn một đơn vị nghiệp vụ, đó là một chuỗi thao tác qua giao diện mỗi lần.

**Vì sao loại:** lập luận mạnh nhất của nó — *"hai đường sẽ lệch nhau"* — là lập luận chống **hai hiện thực**, không chống **hai lối vào**. Service dùng chung giữ được lợi ích đó (một hiện thực) mà bỏ được ma sát.

### Phương án B — Đường theo V1 và ADR-0022: `.sql` dựng dữ liệu, bootstrap dựng tài khoản, script bọc gọi cả hai

**Được:** dev dựng lại bằng một lệnh; tệp `.sql` seed được bất kỳ dữ liệu mẫu nào, kể cả dữ liệu nghiệp vụ; không cần host chạy mới có dữ liệu.

**Vì sao loại:** vai trò, ánh xạ quyền và menu được dựng bằng SQL ở dev và bằng C# sau endpoint ở bản thật — hai hiện thực của cùng một việc, và hiện thực chạy mỗi ngày không phải hiện thực đi lên máy chủ. Kèm hai cái giá chính ADR-0022 đã ghi: tệp `.sql` nằm ngoài cổng checksum của runbook, và script bọc là thứ thứ ba phải bảo trì mà không cổng nào canh.

### Phương án C — Tiến trình API tự seed lúc khởi động khi thấy database trống

**Được:** không lệnh nào phải nhớ; cài xong là chạy.

**Vì sao loại:** trái nguyên tắc *seed là thao tác của người vận hành, không phải tác dụng phụ của việc khởi động* (`docs/wiki-core/be/ly-do/script-runbook.md:111`). Tiến trình phục vụ thật cần quyền ghi dữ liệu nền; nhiều instance khởi động cùng lúc đua nhau seed; và mật khẩu ban đầu phải có mặt trong cấu hình của **mọi** lần khởi động thay vì của đúng một lần chạy lệnh.

### Phương án D — Một project console riêng cho lệnh vận hành

**Được:** host web không có nhánh dòng lệnh nào; `Core.Web` giữ đúng vai tầng HTTP.

**Vì sao loại:** nguồn seed và nguồn danh mục quyền đến từ **composition root** — mỗi module đăng ký chúng khi được lắp vào host. Một project console phải lặp lại toàn bộ phần lắp module của host. Hai composition root cho cùng một dự án là hai danh sách module sẽ lệch nhau, và lệch theo kiểu tệ nhất: lệnh bootstrap dựng đơn vị thiếu vai trò của một module mà host thật có.

### Phương án E — Mã đơn vị hệ thống cố định trong code, hoặc tài khoản vận hành đăng nhập không cần mã đơn vị

**Được:** người vận hành không phải nhớ mã; bớt một khoá cấu hình.

**Vì sao loại:** mã cố định là một hằng số định danh cho một dòng hệ thống phải duy trì — đúng khuôn phương án D của [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) đã loại, cộng rủi ro trùng mã với một cơ quan thật. Đăng nhập không mã thì luồng đăng nhập — luồng nhạy cảm nhất hệ — có một nhánh riêng, kích hoạt bằng việc **bỏ trống** một ô.

### Phương án F — Khôi phục tài khoản vận hành bằng cách chạy lại `core bootstrap`, hoặc bằng một endpoint

**Được:** không thêm lệnh.

**Vì sao loại:** chạy lại mà ghi đè mật khẩu biến một lệnh idempotent thành một lệnh có tác dụng phụ ẩn — người chạy lại "cho chắc" sẽ đặt lại mật khẩu mà không biết. Endpoint thì là đường leo thang mở ra mạng (§5).

### Phương án G — Seed hỏng giữa chừng thì giữ đơn vị, để ở trạng thái ngưng hoạt động

**Được:** transaction không phải bao toàn bộ seed của mọi nguồn. Đơn vị hỏng hiện ra trong danh sách, người vận hành thấy nó tồn tại; đơn vị *"hoạt động nhưng thiếu menu"* — thứ không ai chẩn đoán được từ giao diện — cũng không xảy ra.

**Vì sao loại:** đơn vị ngưng mà thiếu seed là trạng thái cần một đường riêng để thoát ra — seed tiếp hoặc dọn đi — tức đúng lối *"sửa đơn vị lỗi"* mà mục *Dấu hiệu* gọi là cửa sau. Mã đơn vị bị một dòng không dùng được chiếm giữ, và hệ không xoá đơn vị ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10). Tệ nhất: chạy lại với cùng mã thì quy tắc 3 ở §3 thấy đơn vị đã có và **bỏ qua** — lần chạy lại xanh, đơn vị vẫn thiếu seed.

## Hệ quả

### Tích cực

- **Một hiện thực tạo đơn vị**, chạy mỗi ngày trên máy dev và mỗi lần cài thật. ADR-0022 đạt được điều đó cho tài khoản; ADR này mở rộng nó cho dữ liệu đơn vị.
- **Ba câu hỏi mở được đóng**: seam cho bộ vai trò mặc định, mã đơn vị của tài khoản vận hành, đường khôi phục tài khoản vận hành.
- **Allowlist của luật A12 và M12 có một mục cho việc tạo đơn vị**, không phải hai.
- **Không còn tệp dựng dữ liệu nào nằm ngoài cổng checksum**, và không còn script bọc.
- **Tạo đơn vị hỏng không để lại đơn vị nửa vời.** Chạy lại với cùng giá trị là đủ; không cần thao tác dọn nào.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Dev mất đường seed dữ liệu mẫu tuỳ ý** | Tệp `.sql` seed được cả dữ liệu nghiệp vụ mẫu; `ITenantSeedSource` chỉ khai vai trò, ánh xạ quyền, menu. Môi trường dev dựng lại là một đơn vị **không có dữ liệu nghiệp vụ**. ADR này không đưa ra đường thay thế |
| **Mọi `Program.cs` của dự án hạ nguồn phải có dòng `if`** | Thiếu dòng đó thì lệnh `core bootstrap` **khởi động web server bình thường** — người vận hành thấy log khởi động, không thấy lỗi nào, và database vẫn trống. Hỏng im lặng ở đúng bước cài đặt. Hôm nay không cổng nào canh |
| **`Core.Web` gánh thêm vai dòng lệnh** | Câu *"`Core.Web` là tầng HTTP"* không còn tuyệt đối. Bảng ranh giới project ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2 phải nói ra điều đó, nếu không người đọc sẽ coi runner là vi phạm |
| **Service là một điểm tập trung rủi ro** | Nó vừa mở phạm vi của đơn vị khác, vừa gán hai cờ đặc quyền. Một lỗi ở đây sai cho **mọi** đơn vị tạo sau đó, qua cả lệnh lẫn endpoint |
| **Người vận hành đặt nhiều giá trị hơn trước lần cài** | Hai mã, hai tên đơn vị, hai cặp tên đăng nhập–mật khẩu. Thiếu một giá trị là dừng — đúng chủ đích, nhưng là thêm một cách để lần cài đầu hỏng |
| **Bí mật ở máy chạy thật đi đường khác máy dev** | `user-secrets` là cơ chế của máy dev; bản thật đọc cùng khoá qua biến môi trường do Docker Compose cấp từ tệp `.env` ngoài repo — [`../wiki-core/be/18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §7. Hai đường nạp, một tập khoá — lệch tên khoá giữa hai đường là lỗi chỉ lộ ở lần cài thật |
| **Dựng lại database dev chậm hơn một lệnh `psql`** | Lệnh phải build và dựng DI của host mới chạy được. Chấp nhận |
| **Lần tạo đơn vị hỏng không để lại dấu vết nào trong database** | Dòng nhật ký kiểm toán rollback cùng dữ liệu. Chẩn đoán một lần tạo hỏng chỉ còn log ứng dụng — người vận hành nhận lỗi, còn nhật ký kiểm toán không cho thấy đã có một lần thử |
| **Transaction dài lên theo số nguồn seed** | Mỗi module khai thêm vai trò, ánh xạ quyền, menu là thêm dòng ghi trong cùng transaction; kết nối bị giữ cho tới khi seed của mọi nguồn xong. Chấp nhận cho một thao tác hiếm; đo lại khi số module tăng |

## Điều kiện lật quyết định

1. **Có một đường thứ hai** thêm dòng vào bảng đơn vị, đặt `is_system`, hoặc tạo tài khoản mang cờ đặc quyền, ngoài `ITenantProvisioningService`. Tiền đề *một hiện thực* đã hỏng: hoặc gộp đường đó về service, hoặc viết ADR mới giải thích vì sao cần hai.
2. **Có dự án phải cài trên nền tảng không cho chạy lệnh trên máy chủ** — không có shell, không chạy được một tiến trình một lần rồi thoát. Cả lệnh bootstrap lẫn lệnh khôi phục tài khoản vận hành đều dựa trên tiền đề đó.
3. **Việc thiếu dữ liệu mẫu ở dev thành rào cản đo được** — lập trình viên dựng lại dữ liệu mẫu bằng tay nhiều lần mỗi tuần, hoặc tự viết lại tệp `.sql` ngoài luồng. Câu trả lời cần xét là một đường nạp dữ liệu mẫu **tách khỏi** việc tạo đơn vị, không phải đưa tệp `.sql` trở lại làm nguồn của vai trò và menu.

### Dấu hiệu quyết định này bắt đầu sai

- `core bootstrap` bắt đầu nhận tham số để sửa trạng thái đã có — đặt lại mật khẩu, bật cờ, "sửa đơn vị lỗi". Nó đang thành cửa sau.
- Một handler ngoài service gọi thẳng `UserManager` để tạo tài khoản mang cờ đặc quyền "cho nhanh".
- Một nguồn `ITenantSeedSource` đọc dữ liệu từ database hay tệp lúc chạy. Seam này khai dữ liệu tĩnh kiểm được lúc khởi động; đọc động thì phép kiểm lúc khởi động mất nghĩa.

## Liên quan

- [`0022-seed-dev-khong-co-duong-code-rieng.md`](0022-seed-dev-khong-co-duong-code-rieng.md) — bị sửa một phần
- [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) — tài khoản vận hành và ràng buộc của nó
- [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) — hai cờ đặc quyền, cột `is_system`
- [`0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) — khôi phục tài khoản quản trị của đơn vị nghiệp vụ
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1 — khuôn seam gộp nhiều nguồn, allowlist mở phạm vi
- [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10, §11.4 — vòng đời và seed của đơn vị
- [`../database/script-runbook.md`](../database/script-runbook.md) §3.3, §6 — cách gọi lệnh
- [`../RULES.md`](../RULES.md) — luật A3, A12, M12, S1, S9, S10
