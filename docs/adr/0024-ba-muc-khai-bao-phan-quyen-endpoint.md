---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0024 — Mọi endpoint cần đăng nhập khai đúng một trong ba mức phân quyền: khoá quyền, cờ vận hành, hoặc chỉ cần đăng nhập kèm lý do

> **Trạng thái:** Đã chấp nhận (2026-09-14)
>
> **Đóng câu hỏi mở** ở mục *"Một câu hỏi ADR này KHÔNG trả lời"* của [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md): chọn lối ra thứ nhất — nhóm endpoint *của bản thân* không đòi khoá quyền — và **không** đổi phạm vi luật M9.

## Bối cảnh

Lúc quyết định, tài liệu có ba cách để một endpoint cần đăng nhập nói ai được gọi nó, nhưng chỉ một cách có tên trong code:

| Cách | Khai ở đâu (lúc ghi ADR) | Máy phân biệt được "cố ý" với "quên" không |
| --- | --- | --- |
| Theo khoá quyền | `[RequirePermission("…")]` — [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §4.2 | Có |
| Theo cờ vận hành hệ thống | Chỉ là câu văn *"cờ vận hành hệ thống"* ở dòng `Quyền:` của [`../contracts/tenants.md`](../contracts/tenants.md) (dòng 20, 54, 94 lúc ghi). Không attribute nào mang tên đó | Không |
| Chỉ cần đăng nhập | `ApiControllerBase` mang `[Authorize]` ([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §3, dòng 285 lúc ghi); filter §4.2 (dòng 381–382 lúc ghi) ghi rõ: không khai attribute thì giữ nguyên `[Authorize]`, không chặn thêm | **Không** — "không khai gì" và "cố ý mở cho mọi người đã đăng nhập" trông giống hệt nhau |

Dòng cuối là chỗ hỏng. `[Authorize]` chặn người **chưa** đăng nhập, nhưng mở cho **mọi** người đã đăng nhập. Một action nghiệp vụ quên `[RequirePermission]` mở cho mọi tài khoản của đơn vị: không lỗi biên dịch, không test nào đỏ. Checklist ở [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §7 bước 8 (dòng 554 lúc ghi) chỉ nhắc bằng câu văn. Luật S4 canh `[AllowAnonymous]` bằng allowlist; ca *"đã đăng nhập nhưng không khai gì"* không có luật nào canh.

Cùng lúc, ADR-0021 để lại một câu hỏi mà nó cố ý không chọn: tài khoản vận hành không mang `has_permission_bypass` và bị M9 cấm giữ vai trò nghiệp vụ — vậy nó xem hồ sơ, đổi mật khẩu, đăng xuất bằng quyền gì. Card [`../contracts/auth.md`](../contracts/auth.md) (dòng 236, 263, 310, 388 lúc ghi) và [`../contracts/profile.md`](../contracts/profile.md) (dòng 20, 51 lúc ghi) đang khai `Quyền: [Authorize]` — tức đang dùng đúng cách thứ ba, cách không có tên.

## Quyết định

> **Mọi action không mang `[AllowAnonymous]` khai đúng một mức phân quyền trong ba mức dưới đây.** Luật **S11** ở [`../RULES.md`](../RULES.md) §6, ép bằng ArchTest `EveryAction_DeclaresExactlyOneAuthorizationLevel`.

| Mức | Attribute | Ai qua | Dùng cho |
| --- | --- | --- | --- |
| Theo khoá quyền | `[RequirePermission("<code>")]` | Tài khoản có khoá qua vai trò, hoặc mang `has_permission_bypass` ([`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)) | Mọi endpoint nghiệp vụ và quản trị **trong** một đơn vị |
| Khu hệ thống | `[RequireSystemOperator]` | Chỉ tài khoản mang `is_system_operator` | Khu quản trị hệ thống ([`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md)) |
| Chỉ cần đăng nhập | `[AuthenticatedOnly("<lý do>")]` | Mọi tài khoản đã đăng nhập, **kể cả** tài khoản vận hành | Endpoint **của bản thân**: hồ sơ, đổi mật khẩu, người dùng hiện tại, đăng xuất |

Ba hệ quả đi kèm, và chúng là lý do quyết định này tồn tại:

1. **Không khai là vi phạm.** Mức *"chỉ cần đăng nhập"* phải được nói ra bằng tên, không được suy ra từ việc thiếu attribute.
2. **Lý do trong `[AuthenticatedOnly]` là bắt buộc.** Nó biến *"mở cho mọi người đã đăng nhập"* thành một câu người review đọc được ngay tại action — cùng tinh thần allowlist của S4, nhưng đặt tại chỗ.
3. **Cờ vận hành đi bằng attribute riêng, không đi qua ma trận quyền** — giữ nguyên cơ chế 2 của ADR-0017.

### Luật S11 đếm theo mức, không theo attribute

Phần thuộc **quyết định** này là đơn vị đếm: S11 đếm **mức**, không đếm số attribute. Chọn vậy vì mục đích của luật là *"mức phân quyền của một action phải đọc được, không phải suy ra"* — đếm attribute thì một action khai hai khoá cùng mức sẽ bị báo sai, còn một action chồng hai mức khác nhau lại lọt.

Cách đếm từng ca — nhiều khoá trên một action, mức khai ở cấp controller, action chồng mức, `[AllowAnonymous]` trên action — là chi tiết thi công, và nó chỉ sống ở file quy ước.

> 📖 Bảng đếm từng ca, cách hiện thực filter, nguồn đọc cờ, thứ tự filter: đọc [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §4. ADR này không chép.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ nguyên: `[Authorize]` mặc định, `[RequirePermission]` tuỳ chọn

**Được:** ít attribute nhất; endpoint của bản thân không phải khai gì.

**Vì sao loại:** *quên* và *cố ý* không phân biệt được — với người đọc lẫn với máy. Lỗi hỏng im lặng và hỏng theo chiều **mở rộng** quyền. Mỗi module mới là thêm một lượt cơ hội quên, và không cổng nào đếm được lượt đó.

### Phương án B — Lối ra thứ hai của ADR-0021: cấp khoá quyền Core cho endpoint của bản thân, đọc M9 là "cấm vai trò nghiệp vụ, không cấm vai trò Core"

**Được:** một cơ chế duy nhất — ma trận quyền — cho mọi endpoint; không attribute mới. Đây là phương án một người tỉnh táo sẽ chọn.

**Vì sao loại:** khoá quyền đến qua vai trò, và vai trò là dữ liệu theo đơn vị ([`0005-permission-based.md`](0005-permission-based.md), [`0013-multi-tenant.md`](0013-multi-tenant.md)). Mọi tài khoản — kể cả tài khoản vận hành ở đơn vị hệ thống, kể cả tài khoản vừa tạo chưa ai gán vai trò — phải được gán một vai trò Core mặc định thì mới xem được hồ sơ hay đổi được mật khẩu bắt buộc. Quên gán là **tự khoá ngay ở bước đổi mật khẩu lần đầu**. Và *"một vai trò mà mọi tài khoản đều phải có"* là một hằng số vai trò đội lốt dữ liệu — thứ S1 cấm. Cộng thêm: phải sửa phạm vi M9, tức chạm ADR-0017.

### Phương án C — Hai mức: khu hệ thống cũng dùng `[RequirePermission]` với khoá riêng, cộng `[AuthenticatedOnly]`

**Được:** bớt một attribute.

**Vì sao loại:** đưa quyền khu hệ thống vào ma trận quyền là xoá cơ chế 2 của ADR-0017. Một vai trò trong đơn vị nghiệp vụ gán được khoá hệ thống là leo thang từ **trong** một đơn vị ra **về** mọi đơn vị. Tệ hơn: `has_permission_bypass` trả *có* cho mọi khoá, nên quản trị của **mọi** đơn vị sẽ vào được khu hệ thống — trừ khi bộ kiểm quyền thêm một nhánh ngoại lệ, tức đọc cờ ở chỗ thứ hai. ADR-0021 đã nêu chính việc đó là dấu hiệu cờ biến thành vai trò trá hình.

### Phương án D — Allowlist tập trung cho endpoint "chỉ cần đăng nhập", cùng khuôn S4

**Được:** toàn bộ bề mặt *"mở cho mọi người đã đăng nhập"* đọc được trong một lần nhìn.

**Vì sao loại:** allowlist của S4 nằm ở `Core.Web` và liệt kê route. Endpoint của bản thân sẽ xuất hiện ở module — một module có màn *"việc của tôi"* là chuyện bình thường. Allowlist tập trung buộc mỗi module sửa một tệp của Core, tức Core biết route của module: vi phạm R1 và luật 1 của [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md). Lý do đặt tại chỗ đổi tính *"đọc một lần"* lấy tính *"không chạm Core"*; phần mất bù một phần bằng phép đếm ở mục *Tích cực*.

### Phương án E — Chỉ chặn lúc chạy: filter trả 403 khi action không khai mức nào

**Được:** không phụ thuộc project test; ép được cả những action sinh động mà ArchTest không quét tới.

**Vì sao loại làm cơ chế ép DUY NHẤT:** vi phạm chỉ lộ khi có người gọi đúng endpoint đó — thường là QA hoặc người dùng thật — và lộ ra thành 403 cho mọi người. ArchTest đỏ ở `dotnet test`, trước khi merge. ADR này không cấm thêm một lớp chặn lúc chạy; nó không nhận lớp đó **thay cho** ArchTest.

### Phương án F — Nhiều `[RequirePermission]` trên một action nghĩa là có MỘT trong các khoá

**Được:** khai được endpoint mà hai nhóm người khác quyền cùng gọi — ví dụ người có quyền xem và người có quyền duyệt — không cần thêm khoá mới.

**Vì sao loại:** thêm một dòng attribute làm quyền **rộng ra**. Người đọc action thấy thêm một dòng phân quyền và hiểu là chặt hơn — hiểu sai đúng theo chiều mở rộng quyền, cùng chiều hỏng với phương án A. Với nghĩa *"phải có tất cả"*, thêm một dòng không bao giờ mở thêm cho ai.

### Phương án G — Action được khai mức khác đè lên mức cấp controller

**Được:** một controller mang mức chung, vài action ngoại lệ, không phải khai lặp ở từng action.

**Vì sao loại:** đọc riêng action không biết mức thật, và một dòng ghi đè trông như thay đổi cục bộ. `[AuthenticatedOnly]` đè lên `[RequireSystemOperator]` cấp controller là mở một action của khu hệ thống cho mọi tài khoản đã đăng nhập — bằng một dòng mà người review dễ đọc lướt.

### Phương án H — `[AllowAnonymous]` chồng lên mức cấp controller cũng là vi phạm

**Được:** một luật duy nhất cho mọi chồng lấn — đọc attribute cấp controller là biết mức của mọi action.

**Vì sao loại:** ASP.NET Core đã cho `[AllowAnonymous]` thắng `[Authorize]` ở mọi cấp, nên luật này đi ngược hành vi framework mà không đổi được hành vi đó — chỉ buộc controller có một action công khai phải bỏ khai mức cấp controller hoặc tách controller. Quyết định *"endpoint này mở ẩn danh"* đã có chỗ duyệt riêng là allowlist của luật S4; S11 kiểm action ẩn danh nằm trong allowlist đó thay vì cấm nó.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **`[AuthenticatedOnly]` là cửa dễ lạm dụng nhất trong ba** | ArchTest chỉ biết **có** lý do, không biết lý do **đúng**. Một endpoint nghiệp vụ gắn `[AuthenticatedOnly("tạm")]` qua cổng xanh và mở cho mọi tài khoản của đơn vị. Lớp bắt được là review — `core-reviewer` và người đọc PR. Phần máy làm được chỉ là đếm |
| **Tài khoản vận hành gọi được mọi endpoint `[AuthenticatedOnly]`, kể cả của module** | Ranh giới dữ liệu vẫn do bộ lọc đơn vị giữ (cơ chế 1 của ADR-0017) — tài khoản đó thấy rỗng. Nhưng một endpoint của bản thân có **tác dụng phụ ngoài dữ liệu đơn vị** (gửi thư, gọi dịch vụ ngoài) thì tài khoản vận hành kích hoạt được |
| **Mỗi action thêm một dòng khai báo** | Kể cả những action hiển nhiên. Đổi lấy: không còn action nào mà mức phân quyền phải suy ra |
| **`Core.Web` thêm hai attribute và ít nhất một filter** | Bề mặt phải giữ đúng trên mọi dự án hạ nguồn. Đường phân quyền bằng cờ đã có từ ADR-0017; nay nó có tên trong code, nhưng cũng có thêm một chỗ để hỏng |
| **Lúc chốt, S11 chưa có cổng chạy** | ArchTest cần `src/`. Cho tới lúc đó luật chỉ được giữ bằng người đọc — trạng thái cổng đọc ở [`../RULES.md`](../RULES.md), không ở đây |
| **Attribute không khai được *"một trong hai khoá"*** | Endpoint thật sự cần hai nhóm quyền khác nhau cùng gọi phải có một khoá riêng cho nó, hoặc tách thành hai endpoint. Mỗi lần như vậy danh mục khoá quyền dài thêm |
| **Controller trộn mức mất khai báo cấp controller** | Chỉ cần một action khác mức là cả controller phải khai ở từng action, hoặc tách controller |
| **Đọc attribute cấp controller không đủ để biết action nào mở ẩn danh** | `[AllowAnonymous]` trên action thắng mức cấp controller. Người review phải nhìn cả attribute của từng action — hoặc đọc allowlist ẩn danh, nơi mọi action như vậy bắt buộc có tên |
| **Card đang viết `Quyền: [Authorize]` không mang tên mức** | Với endpoint của bản thân, câu đó không sai về nghĩa, và ADR này không yêu cầu sửa hàng loạt. Người đọc card phải biết nó tương ứng mức *chỉ cần đăng nhập*. Card nào mô tả **sai** mức thì phải sửa |

### Tích cực

- **Quên khai phân quyền trở thành một lần `dotnet test` đỏ**, không còn là một quyền mở rộng im lặng.
- **Câu hỏi mở của ADR-0021 có lời đáp** mà không chạm M9, không chạm ADR-0017.
- **Ba mức ứng với đúng ba câu hỏi đã có sẵn trong mô hình:** quyền **trong** một đơn vị, quyền **về** các đơn vị, và danh tính **của chính mình**.
- **Bề mặt nhạy cảm nhất đếm được bằng lệnh** khi có `src/`:

```bash
grep -rn 'AuthenticatedOnly(' src/BE --include='*.cs'
```

### Dấu hiệu quyết định này bắt đầu sai

- Số action mang `[AuthenticatedOnly]` tăng theo **số endpoint nghiệp vụ**, không theo số màn *"của tôi"*.
- Lý do trong `[AuthenticatedOnly]` lặp lại một chuỗi chung chung trên nhiều action.
- Có đề xuất cho `[RequireSystemOperator]` nhận thêm khoá quyền, hoặc cho `[RequirePermission]` hiểu cờ vận hành — lúc đó ba mức đang nhập làm hai.
- Xuất hiện nhu cầu một **mức thứ tư** ở tầng attribute, ví dụ *"chủ bản ghi"*. Kiểm theo bản ghi thuộc về handler ([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §4.4), không phải một mức mới.

## Liên quan

- [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) — câu hỏi mở được đóng
- [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) — cơ chế 2 và luật M9, không bị đổi
- [`0005-permission-based.md`](0005-permission-based.md) — phân quyền bằng permission
- [`0006-pipeline-behavior.md`](0006-pipeline-behavior.md) — vì sao phân quyền ở tầng HTTP, không ở pipeline
- [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §4, §5 — hiện thực và allowlist ẩn danh
- [`../RULES.md`](../RULES.md) §6 — luật S4, S11
