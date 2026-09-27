---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0101 — Hợp đồng giữa hai module nằm ở project `CoreAndSkill.Modules.<X>.Contracts` của module phát, chủ yếu là sự kiện outbox; `Core.Contracts` chỉ chứa thứ Core phát ra

> **Trạng thái:** Đã chấp nhận (2026-09-25) · Bổ sung bởi [ADR-0103](0103-giua-module-doc-dong-bo-qua-contracts-ghi-qua-su-kien.md) (2026-09-25)

## Bối cảnh

Hôm nay tài liệu nói: module A không tham chiếu module B, và hai module giao tiếp qua `Core.Contracts`. Câu đó có ở ba chỗ:

- [`0001-modular-monolith.md`](0001-modular-monolith.md): *"giao tiếp đi qua `Core.Contracts`"*.
- [`0002-core-5-project.md`](0002-core-5-project.md): `Core.Contracts` chứa *"integration event, DTO dùng chung giữa các module"*, và chỉ chứa thứ từ hai module trở lên cùng cần.
- Luật R2 ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §3 và luật A4 ở [`../RULES.md`](../RULES.md) §3.

Cách đó mâu thuẫn với hai quyết định khác, và mâu thuẫn chứng minh được trên giấy:

1. **Với luật 1 của [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md)** (dự án không sửa vùng Core, nay đọc theo [`0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md`](0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md)). Module *Bán hàng* của một dự án hạ nguồn muốn phát sự kiện cho module *Công nợ*. Sự kiện đó phải nằm trong `Core.Contracts`, tức phải sửa vùng Core. Dự án không làm được việc đó mà không vi phạm ADR-0016, hoặc phải đẩy một sự kiện nghiệp vụ lên repo Core.
2. **Với ngưỡng Core** ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4. Sự kiện *"đơn hàng đã xác nhận"* đẩy lên repo Core là từ vựng nghiệp vụ nằm trong Core — dạng vi phạm R1 tinh vi nhất. Mọi dự án khác kéo bản Core mới về sẽ mang theo nó.

Đối chiếu ngày 2026-09-25: `src/BE/Core/CoreAndSkill.Core.Contracts/` chỉ có `AssemblyMarker.cs` và tệp project. Chưa module nào tồn tại. Thay đổi hôm nay không đụng dòng mã nào.

Outbox lưu loại sự kiện bằng một khoá hợp đồng ổn định có phiên bản — `src/BE/Core/CoreAndSkill.Core.Domain/Outbox/OutboxMessage.cs`, chú thích `Khoá hợp đồng ổn định kèm phiên bản`. Nó không lưu tên kiểu CLR. Nhờ vậy dời một kiểu sự kiện sang project khác không làm hỏng các dòng outbox đã lưu.

Một báo cáo đánh giá độc lập ngày 2026-09-25 nêu vấn đề này là P2. Người dùng chọn *"cách thực tế nhiều senior dùng"*.

## Quyết định

Người dùng chốt ngày 2026-09-25: **mỗi module có hợp đồng công khai riêng, ở project `CoreAndSkill.Modules.<X>.Contracts`; module khác chỉ tham chiếu project đó; giao tiếp chủ yếu qua sự kiện outbox; `Core.Contracts` giữ lại nhưng thu hẹp.** Kiến trúc sư ghi phần đi kèm.

1. **`CoreAndSkill.Modules.<X>.Contracts`** chứa thứ module X công bố cho module khác. Trước hết là integration event X phát ra; sau đó là DTO đi kèm các sự kiện ấy.
   - Chỉ phụ thuộc BCL: không package, không project nào khác, kể cả `Core.Contracts`.
   - **Chỉ tạo khi có một module khác thật sự cần nó** — cùng tinh thần ngưỡng Core ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2. Module chưa ai nghe thì không có project này.
2. **Module B chỉ được tham chiếu `Modules.<A>.Contracts` của module A**, không tham chiếu project nào khác của A.
3. **Giao tiếp chủ yếu qua sự kiện outbox.** A ghi sự kiện trong cùng giao dịch với thay đổi của nó; B đăng ký bên nhận qua seam `IOutboxEventHandler`. Khi B cần dữ liệu của A ngay trong cùng request, A khai một interface truy vấn trong `Modules.<A>.Contracts` và hiện thực nó ở Infrastructure của A. Đó là đường phụ, không phải đường chính.
4. **`Core.Contracts` giữ lại, chỉ chứa thứ Core phát ra** cho module, như sự kiện Core công bố. Nó không còn là nơi đặt hợp đồng giữa hai module. Không dòng mã nào đổi.
5. **Luật sửa theo:**
   - R2 ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §3.
   - A4 ở [`../RULES.md`](../RULES.md) §3 — phủ cả vế *"mọi project `*.Contracts` chỉ phụ thuộc BCL"*, vốn chưa có ArchTest nào cho `Core.Contracts`.
   - Hàng `Core.Contracts` ở §2 và bảng hợp đồng lắp ghép ở §7 của cùng tệp.
   - Sơ đồ ở [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.2.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ `Core.Contracts` làm nơi chung cho mọi hợp đồng giữa module

**Được:** không đổi gì. Một chỗ duy nhất để tìm mọi hợp đồng.

**Mất:** hai mâu thuẫn ở *Bối cảnh*. Dự án hạ nguồn phải sửa vùng Core, hoặc đẩy từ vựng nghiệp vụ lên repo Core.

**Vì sao loại:** nó không dùng được ở đúng nơi nó phải dùng, tức dự án hạ nguồn.

### Phương án B — Một project hợp đồng chung cho cả dự án, như `<Dự án>.Contracts`

**Được:**

- Một chỗ, nằm trong vùng dự án, nên không đụng ADR-0016.
- Ít project hơn phương án đã chọn.

**Mất:**

- Không ai sở hữu từng kiểu trong đó. Project chung thành ngăn kéo rác — đúng rủi ro ADR-0002 ghi cho `Core.Contracts`.
- Mọi module phụ thuộc một project, nên đổi một sự kiện thì build lại mọi module.
- Đồ thị phụ thuộc không nói được module nào nghe module nào.

**Vì sao loại:** mất quyền sở hữu, mà quyền sở hữu là thứ phương án đã chọn mua được.

### Phương án C — Không tham chiếu project nào; mỗi bên nhận tự khai lại kiểu sự kiện và đọc theo khoá hợp đồng

**Được:** không phụ thuộc lúc biên dịch giữa hai module.

**Mất:** không trình biên dịch nào kiểm hai bản khai. Bên phát đổi một trường thì bên nhận vỡ lúc chạy, trong một giao dịch outbox, không ở lúc build.

**Vì sao loại:** đổi lỗi lúc build lấy lỗi lúc chạy. [`0001-modular-monolith.md`](0001-modular-monolith.md) chọn monolith một phần vì *"refactor xuyên module vẫn được compiler kiểm"*.

## Hệ quả

### Tích cực

- Dự án hạ nguồn thêm hợp đồng giữa hai module của mình mà không chạm vùng Core.
- Core không bao giờ mang từ vựng nghiệp vụ vì lý do giao tiếp giữa module.
- Hợp đồng có chủ: module phát sở hữu nó. Đồ thị tham chiếu project cho thấy ai nghe ai.
- Không dòng mã nào đổi hôm nay.

### Tiêu cực

- **Số project tăng**: tối đa thêm một project cho mỗi module có người nghe. Solution dài hơn, Dockerfile của host thêm một dòng `COPY` mỗi lần.
- **Luật *"chỉ tạo khi có người cần"* dễ bị bỏ**: tạo sẵn cho đủ bộ là thói quen. Cổng A4 có một vế bắt project `*.Contracts` không ai tham chiếu, nhưng vế đó còn `📐`.
- **Đường truy vấn đồng bộ** là cửa để hai module dính chặt lại, nếu dùng thay cho sự kiện. Quyết định 3 chỉ gọi nó là đường phụ, không đặt ngưỡng nào.
- **`Core.Contracts` trống**, và có thể trống lâu. Một project trống trong Core là một thứ phải giải thích cho người mới.

### Rút lui nếu sai

1. Dời kiểu từ `Modules.<X>.Contracts` sang chỗ mới — một đợt refactor không tên miền, trình biên dịch dẫn đường.
2. Sửa A4 và R2 bằng ADR mới.

Dòng outbox đã lưu không bị ảnh hưởng, vì outbox lưu khoá hợp đồng chứ không lưu tên kiểu CLR (neo ở *Bối cảnh*). Cỡ việc tỉ lệ với số hợp đồng. Hôm nay con số đó bằng không.

### Dấu hiệu quyết định này bắt đầu sai

- Một `Modules.<X>.Contracts` chứa logic hay tham chiếu package.
- Hai module gọi nhau qua interface truy vấn nhiều hơn qua sự kiện.
- Một project `*.Contracts` tồn tại mà không module nào tham chiếu.
- Có đề xuất đưa một sự kiện của module lên `Core.Contracts` *"vì hai dự án cùng cần"*. Đó là câu hỏi ngưỡng Core, không phải câu hỏi hợp đồng giữa module.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Mâu thuẫn chứng minh được trên giấy (*Bối cảnh*). **Chưa có ca thật**: chưa module nào tồn tại |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án B, một project chung của dự án, đơn giản hơn. Nó mất quyền sở hữu và biến thành ngăn kéo rác |
| 3 | Chi phí vận hành thêm | Một project cho mỗi module có người nghe. Không hạ tầng mới |
| 4 | Ai bảo trì | Module phát sở hữu project `.Contracts` của mình. Cổng A4 thuộc ArchTests của Core, `test-engineer` giữ |
| 5 | Rút lui thế nào | Refactor dời kiểu; không dữ liệu nào phải chuyển |
| 6 | Có buộc Core biết nghiệp vụ không | Ngược lại: nó rút lối duy nhất mà nghiệp vụ từng có thể lọt vào `Core.Contracts` |

## Thi công và kiểm

| Ai | Việc | Khi nào |
| --- | --- | --- |
| `test-engineer` | ArchTest A4, ba vế. (1) Project của module A chỉ tham chiếu `Modules.<B>.Contracts` của module B. (2) Mọi project `*.Contracts`, kể cả `Core.Contracts`, chỉ phụ thuộc BCL. (3) Mọi `Modules.<X>.Contracts` được ít nhất một module khác tham chiếu. Tập module xác định theo thư mục `src/BE/Modules/`, không theo tiền tố tên ([`0102-module-o-src-be-modules-test-canh-module.md`](0102-module-o-src-be-modules-test-canh-module.md)). Canary module giả cho từng vế (T1) | Cùng lượt mở rộng ArchTests ra module của ADR-0102; vế (2) cho `Core.Contracts` dựng được ngay |
| `backend-expert` | Không có việc mã hôm nay | — |

## Liên quan

- [`0001-modular-monolith.md`](0001-modular-monolith.md), [`0002-core-5-project.md`](0002-core-5-project.md) — được ADR này sửa một phần: nơi đặt hợp đồng giữa module, vai của `Core.Contracts`
- [`0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md`](0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md) — vùng dự án
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1 — seam `IOutboxEventHandler`
