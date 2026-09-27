---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0070 — Mã test dùng chung là một **tệp nguồn** ở `src/BE/Tests/Shared/` nối vào từng project test, không phải một project test-support

> **Trạng thái:** Đã chấp nhận (2026-09-23)

## Bối cảnh

Một loạt test giật đã được sửa ngày 2026-09-23. Nguyên nhân: `CoreMetrics` là một `Meter` **tĩnh toàn tiến trình**, `MeterListener` nghe **toàn tiến trình**, còn xUnit chạy các lớp test song song trong **cùng** tiến trình — nên một listener mở trong lớp test này đếm luôn phép đo của lớp test kia.

Cách cô lập đã chốt: lọc phép đo theo **luồng logic** của chính test, nhận diện bằng `AsyncLocal`. Ý tưởng đó hôm nay nằm ở **hai** nơi:

| Nơi | Hình dạng |
| --- | --- |
| `src/BE/Tests/CoreAndSkill.Core.UnitTests/Support/MeterProbe.cs` | Lớp `MeterProbe` đầy đủ: lọc theo tham chiếu `Instrument`, lọc theo `AsyncLocal`, khôi phục giá trị `AsyncLocal` cũ khi `Dispose`, có `MeterProbeTests.cs` kiểm chính nó |
| `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Jobs/B4BackgroundFailureVisibilityTests.cs` | Một `private static` trong chính lớp test: `CountFailedJobMetricDuring`, cùng kỹ thuật `AsyncLocal`, **không** khôi phục giá trị cũ, chỉ đếm được một `Instrument` |

Hai project test không dùng chung một dòng mã nào. Bản thứ hai **đã lệch** khỏi bản thứ nhất ngay từ lúc sinh ra — nó thiếu đúng hai tính chất mà bản thứ nhất phải thêm vào sau khi gặp ca hỏng thật. Đó là khuôn *"hai bản sẽ lệch nhau"* mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5 cấm, và §5 cũng nói sẵn cách sửa: **gỡ một bản, không đồng bộ hai bản**.

Ngưỡng của [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 — *"từ HAI nơi cần thì mới nâng lên chỗ dùng chung"* — đã đạt: đúng hai project test cần, và `CoreAndSkill.ArchTests` thì không (nó quét cú pháp, không đo chỉ số). Đạt ngưỡng, không vượt: đây là ca **vừa đủ**, không phải ca dư dả.

## Quyết định

Kiến trúc sư chốt:

1. **Mã test dùng chung sống ở `src/BE/Tests/Shared/`, là tệp nguồn trần, không phải project.** Mỗi project test cần nó khai một mục `<Compile Include>` tường minh trỏ tới đúng tệp đó, kèm `Link` để nó hiện đúng chỗ trong cây của project.
2. **Khai từng tệp một, không dùng ký tự đại diện.** Một tệp bỏ vào `Shared/` mà tự động lọt vào mọi project test là một quyết định chia sẻ không ai ký; khai tay buộc người thêm phải nói ra nó chia sẻ cho ai.
3. **`MeterProbe.cs` là tệp đầu tiên chuyển vào đó**, kèm `MeterProbeTests.cs` — bộ test kiểm chính dụng cụ này chỉ cần biên dịch ở **một** project (T1 đòi detector có test kiểm nó, không đòi mỗi assembly một bản).
4. **`CountFailedJobMetricDuring` trong `B4BackgroundFailureVisibilityTests` bị gỡ**, chỗ gọi đổi sang `MeterProbe`. Không giữ lại làm "bản cho integration".
5. **Không dựng project `CoreAndSkill.TestSupport`.** Lý do ở phương án A.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Một project class library `CoreAndSkill.TestSupport`, ba project test tham chiếu tới

**Được:** hình dạng quen thuộc nhất, ai đọc solution cũng hiểu ngay. `MeterProbe` có một chủ rõ ràng. Không lo coverage — mục `Include` của [T3](../RULES.md) lọc theo **tên assembly** (`[CoreAndSkill.Core.Domain]*,[CoreAndSkill.Core.Application]*`), nên một assembly mới không vào mẫu số.

**Mất:** `MeterProbe.cs` hôm nay chứa **hai** thứ — lớp `MeterProbe` và `OutboxSnapshotCollection` mang `[CollectionDefinition]`. Với xUnit 2 (bản ghim ở `Directory.Packages.props`), định nghĩa collection được khám phá **theo từng assembly test**: một `[CollectionDefinition]` nằm trong một class library không phải assembly test thì không assembly nào thấy, và `[Collection("…")]` ở các lớp test trỏ vào một collection không tồn tại — xUnit **không báo lỗi** cho ca đó, nó chỉ thôi tuần tự hoá. Tức cổng **T10** vẫn xanh trong khi thứ nó canh đã hỏng. Muốn dùng project thì phải xẻ tệp làm hai và nhân bản `OutboxSnapshotCollection` ra từng assembly — quay lại đúng hai bản.

**Vì sao loại:** nó đổi một bản sao **nhìn thấy được** (hai đoạn `AsyncLocal`) lấy một bản sao **bắt buộc và im lặng** (định nghĩa collection mỗi assembly một bản), cộng thêm một chế độ hỏng mà không cổng nào bắt.

> Phép bác bỏ nếu tôi sai: cho `[CollectionDefinition]` vào một class library, để hai lớp test ở hai assembly cùng ghi ảnh chụp outbox, chạy lại và xem chúng có còn tuần tự với nhau không. Xanh thật thì phương án A sống lại và ADR này phải viết lại.

### Phương án B — `MeterProbe` thành `public`, `IntegrationTests` tham chiếu thẳng project `UnitTests`

**Được:** không tệp mới, không cấu hình MSBuild nào.

**Vì sao loại:** một project test tham chiếu một project test kéo theo toàn bộ lớp test và fixture của bên kia vào ngữ cảnh biên dịch của bên này, và khiến `UnitTests` — vốn **chỉ** tham chiếu `Domain` và `Application` để sàn coverage T3 đo đúng hai tầng đó — thành phụ thuộc của một project tham chiếu cả `Infrastructure` lẫn `Web`. Ranh giới ấy là thứ [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §9.1 dựng có chủ đích.

### Phương án C — Chấp nhận hai bản, ghi một dòng nợ

**Được:** không sửa gì hôm nay. Hai đoạn mã đều ngắn.

**Vì sao loại:** bản thứ hai **đã** lệch rồi, trước khi ai kịp ghi nợ. Một dòng nợ ở [`../DEBT.md`](../DEBT.md) làm chỗ lệch nhìn thấy được nhưng không làm nó hết lệch, và lần cải tiến tiếp theo của `MeterProbe` vẫn sẽ không tới bản kia. §5 nói thẳng: đồng bộ là hoãn vấn đề.

### Phương án D — Bỏ hẳn `MeterProbe`, để `CoreMetrics` nhận một `Meter` tiêm vào thay vì `Meter` tĩnh

**Được:** giải tận gốc — không còn trạng thái toàn tiến trình thì không cần cô lập, và `OutboxSnapshotCollection` cũng không cần luôn.

**Vì sao loại:** nó đổi **mã sản phẩm** của Core để cho test dễ hơn, và đổi ở một chỗ mà hình dạng tĩnh đang là lựa chọn đúng cho mã gọi (`CoreMetrics.X.Add(1)` không phải xin ai). Đây có thể là quyết định đúng về sau, nhưng nó là một quyết định về **thiết kế đo lường của Core**, không phải về chỗ đặt mã test — trộn hai thứ vào một lượt là cách một thay đổi hạ tầng test lặng lẽ trở thành một thay đổi API của Core. Để riêng; mở lại khi có lý do từ phía sản phẩm.

## Hệ quả

### Tích cực

- Một nguồn duy nhất cho phép lọc theo luồng; lần sửa tiếp theo của `MeterProbe` tới cả hai project mà không ai phải nhớ.
- `OutboxSnapshotCollection` ở lại **trong** assembly test, đúng chỗ xUnit tìm nó — cổng T10 canh đúng thứ nó tưởng.
- Không project mới: không thêm `csproj` phải giữ đồng bộ phiên bản gói, không thêm mục trong `.slnx`, không thêm assembly vào bước build.
- Bản integration mạnh lên miễn phí: nó nhận luôn phần khôi phục `AsyncLocal` và phần lọc nhiều `Instrument` mà bản chép tay không có.

### Tiêu cực

- **Tệp nguồn liên kết là thứ vô hình trong cây thư mục.** Mở `CoreAndSkill.Core.IntegrationTests/` trên đĩa sẽ **không** thấy `MeterProbe.cs`; nó chỉ hiện trong IDE và trong `csproj`. Người sửa nó dễ tưởng mình đang sửa mã của một project, trong khi đang sửa mã của hai — và trình biên dịch chỉ báo khi build cả hai.
- **Hai assembly có hai kiểu `MeterProbe` khác nhau** dù cùng một tệp nguồn. Không ai truyền được một probe qua ranh giới assembly, và thông điệp lỗi kiểu sẽ nói về hai kiểu trùng tên. Hôm nay không ai cần truyền; nếu ngày nào cần thì phương án A quay lại bàn.
- **Không cổng nào canh việc này.** Một người viết một `CountXDuring` mới ngay trong lớp test của mình — đúng thứ vừa bị gỡ — sẽ không làm gì đỏ. Luật *"kỹ thuật cô lập phép đo chỉ có một bản"* chưa có hình dạng máy nhận ra; nó ghi ở [`../DEBT.md`](../DEBT.md) thay vì được khai là đã ép.
- **Thư mục `Shared/` là chỗ mọi thứ sẽ trôi vào.** Quy tắc *khai từng tệp một* làm nó chậm lại nhưng không chặn được. Dấu hiệu cần đọc lại ADR này: `Shared/` có tệp thứ tư, hoặc một tệp ở đó bắt đầu cần `PackageReference` riêng.

### Rút lui nếu sai

Dựng `CoreAndSkill.TestSupport` (phương án A), chuyển lớp `MeterProbe` sang đó, để `OutboxSnapshotCollection` lại ở từng assembly test. Nửa buổi, và phần khó — quyết định tách đôi tệp — đã có sẵn câu trả lời ở ngay mục Phương án A.

## Liên quan

- [`../RULES.md`](../RULES.md) §8 — luật **T10** (lớp test ghi trạng thái chỉ số toàn cục phải chung một collection) và **T1**, **T3**, **T6**.
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §9.1 — ranh giới tham chiếu của ba project test.
- [`../wiki-core/be/04-testing-strategy.md`](../wiki-core/be/04-testing-strategy.md) §8 — test không ổn định phải được sửa, không chạy lại cho qua.
- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 — ngưỡng hai nơi cần.
