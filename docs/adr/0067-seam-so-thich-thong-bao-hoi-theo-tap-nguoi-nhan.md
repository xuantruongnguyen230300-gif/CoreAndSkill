---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0067 — Seam `INotificationPreferences` hỏi theo TẬP người nhận (`FilterEnabledAsync`), không theo từng người

> **Trạng thái:** Đã chấp nhận (2026-09-23)

## Bối cảnh

`INotificationPreferences` là **seam công khai** của `Core.Application`: Core gọi, dự án hạ nguồn cài ([`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1, hàng ba seam của mảng thông báo). Chỗ gọi duy nhất là `NotificationPublisher` — **internal, nằm trong Core**. Dự án hạ nguồn lấy Core bằng clone và **không sửa tệp thuộc `Core/`** ([ADR-0016](0016-phan-phoi-core-bang-clone.md)), nên nó cài được hiện thực nhưng **không** đổi được cách Core hỏi.

Hình dạng lúc thi công là `IsEnabledAsync(userId, code, channel, ct)` — publisher hỏi một lần cho **mỗi người nhận × mỗi kênh**.

Ràng buộc làm quyết định này khó hơn nó trông: **Core không đo được vấn đề này.** Hiện thực mặc định của Core chạy trong bộ nhớ (`AlwaysEnabledNotificationPreferences` trả nguyên tập), nên hỏi kiểu nào cũng như nhau — không có số đo nào để trình ra. Cái giá chỉ hiện ra ở bản cài của dự án: [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §3.3 khai mức tối thiểu là **một bảng ánh xạ người dùng × loại thông báo × kênh**, tức một bảng trong database. Với bảng đó, mỗi lời gọi là một truy vấn: 200 người nhận trên 2 kênh thành 400 truy vấn tuần tự. Và `NotificationDraft.RecipientUserIds` không có trần nào.

Cửa sổ đổi rẻ đang mở và sắp đóng: `git ls-files src/` trả **0 tệp** — `src/` chưa vào git, nên chưa bản Core nào được clone kèm mã nguồn theo ADR-0016, và chưa dự án nào cài hiện thực theo chữ ký cũ.

Đây là **thay đổi phá vỡ bề mặt công khai của Core** — nhỏ hơn [ADR-0064](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md) nhưng cùng loại, nên cùng đòi một bản ghi.

## Quyết định

Kiến trúc sư chốt:

1. **`INotificationPreferences` khai đúng một method: `FilterEnabledAsync(IReadOnlyCollection<Guid> userIds, string code, NotificationChannels channel, CancellationToken ct)`**, trả về **tập con** của `userIds`.
2. **Hợp đồng của mọi hiện thực:** trả tập con của đầu vào, giữ thứ tự đầu vào, không thêm ai không được hỏi. Hiện thực mặc định của Core trả nguyên tập.
3. **`NotificationPublisher` hỏi đúng một lần cho mỗi kênh**, không một lần cho mỗi người nhận.
4. Hình dạng này chốt **trước** lần phát hành đầu tiên, vì chỗ gọi nằm trong Core và dự án hạ nguồn không sửa được nó.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ `IsEnabledAsync(userId, …)`, để dự án tự lo bằng cache trong hiện thực của nó

**Được:** chữ ký đơn giản nhất và **khó cài sai nhất** — một hàm trả `bool` cho một người thì không có cách nào trả nhầm ai. Không phải sửa gì.

**Mất:** đẩy một bài toán **theo lô** vào một hiện thực chỉ nhìn thấy **một** phần tử. Dự án muốn gộp truy vấn phải tự dựng cache theo request và tự đoán khi nào một lô kết thúc — thông tin mà hình dạng seam không hề cấp cho nó. Cái giá đó trả ở **mọi** dự án hạ nguồn, mãi mãi, vì `NotificationPublisher` là internal.

**Vì sao loại:** hình dạng sai ở đây là hình dạng sai **vĩnh viễn** với mọi dự án hạ nguồn. Đây không phải một tối ưu hoá sớm; đây là một cánh cửa đóng lại.

### Phương án B — Giữ hai method: một cho một người, một cho tập

**Được:** chỗ gọi cũ không phải sửa; dự án chọn cài cái nào tiện hơn.

**Mất:** hai đường cho cùng một câu hỏi, và hai hiện thực có thể trả lời khác nhau mà không gì bắt được. Dự án cài một cái và quên cái kia thì lỗi hiện ra ở đúng nhánh ít chạy nhất.

**Vì sao loại:** đúng khuôn *hai nguồn sẽ lệch* mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5 cấm. Và method thứ hai không thêm khả năng nào — tập một phần tử đã là ca riêng của tập.

### Phương án C — Hoãn tới khi có dự án hạ nguồn thật đo được

**Được:** không đổi bề mặt khi chưa có số đo — đúng kỷ luật *đo trước khi tối ưu* của [`../quy-uoc/be-performance.md`](../quy-uoc/be-performance.md) §1.

**Mất:** lúc có số đo thì Core đã phát hành, chỗ gọi vẫn nằm trong Core, và mỗi dự án đã cài một hiện thực theo chữ ký cũ. Đổi lúc đó là đổi bề mặt công khai của Core **sau khi đã có người dùng** — tức mỗi dự án phải sửa một lớp, vì một quyết định họ không tham gia.

**Vì sao loại:** kỷ luật *đo trước* áp cho việc **thêm cơ chế** (cache, hàng đợi, chỉ mục) — thứ có chi phí vận hành. Quyết định này không thêm cơ chế nào; nó chọn **hình dạng của một chữ ký chưa ai dùng**, và chi phí của việc chọn sai không đối xứng: chọn theo tập mà không cần thì thừa một tham số, chọn theo người mà cần thì hỏng ở mọi dự án.

## Hệ quả

### Tích cực

- Dự án cài bản đọc database trả lời được bằng **một** truy vấn theo tập, không phải một truy vấn mỗi người.
- Số lời gọi seam **không tăng theo số người nhận** — ghim bằng test đếm lời gọi `Publish_AsksThePreferencesSeam_OncePerChannel_NotOncePerRecipient` (`src/BE/Tests/CoreAndSkill.Core.UnitTests/Notifications/NotificationPublisherTests.cs`); cả tệp 19/19 xanh 2026-09-23.
- Hiện thực mặc định của Core rút còn một dòng, nên bản mẫu mà dự án chép theo cũng đúng hình dạng.

### Tiêu cực

- **Chữ ký mới khó cài đúng hơn hẳn chữ ký cũ.** Một hiện thực trả về tập **không phải** tập con của đầu vào — thêm người, hoặc quên lọc và trả nguyên tập — làm thông báo tới cả người đã tắt. Hợp đồng đó hôm nay chỉ nằm trong chú thích: **không cổng nào ép**. Với `bool` thì lỗi loại này không tồn tại được. Ghi thành nợ **B18** ở [`../RULES.md`](../RULES.md) §10.
- **Hiện thực của dự án nay nhận một danh sách dài tuỳ ý.** Core không khai trần nào cho `NotificationDraft.RecipientUserIds`, nên dự án phải tự lo câu truy vấn theo tập quá dài — một vấn đề mà chữ ký cũ không bao giờ tạo ra. Core đẩy vấn đề này sang dự án mà không nói.
- **Đây là lần thứ hai trong cùng một tuần một seam công khai của Core đổi hình dạng** ([ADR-0064](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md) là lần trước). Hôm nay không phá gì vì chưa ai dùng, nhưng mỗi lần như vậy tiêu một phần niềm tin rằng bề mặt Core đã ổn định — và niềm tin đó là thứ ADR-0016 bán cho dự án hạ nguồn.

### Rút lui nếu sai

Đảo lại hôm nay là một lượt sửa **ba chỗ**: chữ ký interface, hiện thực mặc định, và chỗ gọi trong `NotificationPublisher`. Nửa buổi; không dữ liệu nào đổi, không lược đồ nào đổi, không migration nào.

Điều kiện làm việc đảo trở nên **đắt**: Core đã phát hành theo ADR-0016 và có dự án cài hiện thực riêng — khi đó mỗi dự án phải sửa một lớp của mình. Cửa sổ đảo rẻ vì vậy đóng ở lần phát hành đầu tiên, và đó chính là lý do quyết định này chốt hôm nay chứ không hoãn.

Dấu hiệu quyết định này bắt đầu sai: một hiện thực hạ nguồn trả về tập **không phải tập con** và không ai phát hiện cho tới khi có người nhận thư đã tắt. Khi đó thứ cần thêm là một **bộ test hợp đồng** cho mọi hiện thực (nợ B18), **không** phải đổi lại chữ ký.

## Liên quan

- [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md) — vì sao dự án hạ nguồn không sửa được chỗ gọi.
- [`0064-nhanh-tra-tep-di-qua-apicontrollerbase.md`](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md) — cùng loại: đổi bề mặt Core để module hạ nguồn dùng được.
- [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §3.3 · [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1 · [`../RULES.md`](../RULES.md) §10 dòng **B18**.
