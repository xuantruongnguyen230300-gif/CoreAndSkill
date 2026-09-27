---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0040 — A17 (`unsavedChangesGuard`) kéo sớm vào F2, không dời sang F3

> **Trạng thái:** Đã chấp nhận (2026-09-17)

## Bối cảnh

`frontend-expert` rà lộ trình FE và phát hiện một lỗ hổng cùng khuôn với lỗ hổng mà ADR-0037 đã xử lý, nhưng ở một mảng khác.

`docs/wiki-core/fe/01-core-components.md` §2 khai A17 — `unsavedChangesGuard` — thuộc "Nhóm A — bắt buộc, thiếu là Core không dùng được". Lý do ghi ngay trong bảng: "Form nghiệp vụ hành chính thường dài. Mỗi màn tự lo thì hành vi không nhất quán và màn nào quên là người dùng mất dữ liệu." Nhưng bảng lộ trình ở §4.1 của cùng file — bảng duy nhất gán từng mục Nhóm A cho một pha cụ thể — không có dòng nào cho A17. `docs/wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md` và `docs/wiki-core/fe/trien-khai/03-f2-auth-routing.md` §7 (Nghiệm thu F2) cũng không nhắc tới nó ở đâu.

Trong khi đó, `docs/quy-uoc/fe-routing-guard.md` §4.1 đã có đặc tả đầy đủ cho guard này: luật ("chỉ hỏi khi giá trị thật sự đổi", "xoá cờ sau khi lưu", "áp cho cả điều hướng trong app lẫn đóng tab"), và cả khoá i18n của hộp hỏi. Nghĩa là câu hỏi "guard này làm gì" đã được quyết — chỉ còn thiếu quyết định "khi nào build".

Hai screen spec đã dựng thật ở F2 — `docs/Design/Screens/00-khung-ung-dung.md` và `docs/Design/Screens/03-ho-so-ca-nhan.md` — đã viết hành vi của guard này vào mục "Trạng thái"/"Thao tác xong" như một quyết định đã chốt, không nằm trong mục "Cần chốt". Cả hai file này đã đối chiếu với `src/FE` thật ngày 2026-09-17 (bảng "Có thật hôm nay → sẽ thành" ở đầu mỗi file, theo cơ chế ADR-0037) và cùng ghi nhận: **"Chưa có" — không tìm thấy guard hay `canDeactivate` nào tên này trong `src/FE/src` (đã grep toàn repo)**. Đối chiếu trực tiếp `src/FE/src/app/core/guards/` xác nhận điều đó: chỉ có `auth.guard.ts`, `must-change-password.guard.ts`, `permission.guard.ts` — không có guard chặn rời trang.

Nếu để nguyên hiện trạng, F2 có thể được coi là "xong" theo đúng checklist nghiệm thu ở `03-f2-auth-routing.md` §7 — vì checklist đó không có mục nào kiểm hành vi "hỏi trước khi rời trang" — trong khi hai màn F2 đang chạy thật (`shell.component.ts`, `ho-so.page.ts`) lại thiếu đúng cơ chế mà câu chữ trong đặc tả của chính chúng mô tả là đang hoạt động ("Còn thay đổi chưa lưu ở màn con — hỏi TRƯỚC khi gửi request, bằng đúng hộp của `unsavedChangesGuard`"). Đây là khuôn rủi ro giống hệt cái ADR-0037 đã xử lý cho `Sidebar`/`Topbar`/`PageHeader`/`Card`/`ConfirmDialog`/`SkeletonLoader`: đặc tả nói có, code không có, và không có cổng nào — kể cả nghiệm thu tay — hỏi tới sự khác biệt đó trước khi đóng pha. Khác biệt duy nhất: ở ADR-0037 code đã **lệch** khỏi đặc tả (dựng tay thay component), còn ở đây code **vắng mặt hoàn toàn** — một guard chưa từng được lên lịch, không phải một guard bị thay bằng bản viết tay.

ADR-0037 đã áp đúng ngưỡng "hai nơi cần trở lên" (`docs/kien-truc-core-module.md` §4.2, dịch sang FE ở `01-core-components.md` §1) để kéo sớm bốn component khác vào F2. A17 thoả đúng ngưỡng đó nhưng bị bỏ sót khỏi rà soát của ADR-0037, vì rà soát đó tìm "component bị dựng tay thay spec", không tìm "mục Nhóm A chưa có dòng trong bảng lộ trình".

## Quyết định

1. A17 (`unsavedChangesGuard`) kéo sớm vào pha **F2** — cùng ngưỡng đã áp cho ADR-0037: cả `00-khung-ung-dung.md` (hỏi trước khi đăng xuất hoặc đổi ngôn ngữ khi màn con còn thay đổi chưa lưu) và `03-ho-so-ca-nhan.md` (hỏi trước khi rời màn hồ sơ khi còn thay đổi chưa lưu) là hai màn **F2 thật, đang chạy**, không phải dự đoán nhu cầu tương lai — cùng đòi đúng hành vi này, và cùng đã viết vào đặc tả của mình rằng hành vi đó tồn tại.
2. Bảng lộ trình §4.1 của `01-core-components.md` phải thêm một dòng A17, gán pha F2. Việc sửa nội dung file này **không** thuộc phạm vi ghi của `architect` — cùng giới hạn mà ADR-0037 đã tự ghi nhận cho chính nó (mục Hệ quả tiêu cực, ý #3) — nên ADR này không tự áp thay đổi vào file đó; xem mục Hệ quả tiêu cực dưới đây.
3. Nghiệm thu F2 (`03-f2-auth-routing.md` §7) phải thêm tối thiểu các mục: rời màn hồ sơ khi còn thay đổi chưa lưu → hỏi đúng câu "Rời trang?" của `unsavedChangesGuard`; chọn "Rời đi" → mất thay đổi, điều hướng tiếp; chọn "Ở lại" → đóng hộp, không gửi request, thay đổi giữ nguyên; lưu thành công → cờ thay đổi bị xoá, rời trang ngay sau đó **không** bị hỏi lại; đăng xuất hoặc đổi ngôn ngữ ở khung ứng dụng khi màn con còn thay đổi chưa lưu → cùng đi qua đúng một hộp, không hỏi hai lần. Việc thêm các mục này vào file đó cùng chịu giới hạn ở ý #2.
4. Một luật mới — F30 — vào `docs/RULES.md`: cấm một trang tự cài cơ chế hỏi rời trang cục bộ (tự lắng nghe `window:beforeunload`, tự gọi hộp xác nhận) thay cho `unsavedChangesGuard` dùng chung, khi chính screen spec của trang đó đã ghi hành vi này phải đi qua guard. Ép bằng review, chưa có cổng máy — vào danh sách nợ §10 (xem Hệ quả tiêu cực, ý #4).

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Dời sang F3, ghi nợ "guard chưa build" vào danh sách nợ hoặc vào nghiệm thu F3

**Được:** F2 đóng sớm hơn, không tốn công dựng guard trong pha này.
**Mất:** Hai màn F2 **thật, đang chạy** (không phải màn giả định) sẽ vận hành với đúng hành vi "mất dữ liệu im lặng khi rời trang" — trong khi câu chữ đặc tả của chính chúng ("hỏi TRƯỚC khi gửi request") nói ngược lại điều đó đang xảy ra. Nếu mỗi màn tự vá tạm bằng cơ chế cục bộ cho tới F3 thì phải viết lại khi guard thật ra đời — đúng "chi phí kép" mà ADR-0037 đã dùng để loại phương án tương tự của nó. Nếu không vá tạm, có một cửa sổ thời gian F2 được coi là "xong" mà không ai biết đúng bug mất dữ liệu này còn tồn tại, vì nghiệm thu hiện tại không hỏi tới nó.
**Vì sao loại:** cùng lý do ADR-0037 đã loại phương án song song của nó — một khoảng trống không có cổng nào canh (kể cả nghiệm thu tay) sẽ không tự lộ ra, và ở đây nghiệm thu chính là cổng duy nhất bảo vệ hành vi này.

### Phương án B — Hạ A17 khỏi Nhóm A, coi là "nên có" (Nhóm B) thay vì "bắt buộc"

**Được:** giải quyết mâu thuẫn giữa §2 và §4.1 bằng cách làm cho không còn gì bắt buộc phải lên lịch — không còn gì để dời.
**Mất:** xoá đúng lý do A17 tồn tại, đã ghi ngay trong dòng của nó: "Mỗi màn tự lo thì hành vi không nhất quán và màn nào quên là người dùng mất dữ liệu." Hạ nó xuống Nhóm B là chấp nhận đúng thứ luật này sinh ra để cấm, và không có gì trong bối cảnh hôm nay làm lý do ban đầu đó sai — hai màn F2 thật đang chứng minh điều ngược lại: nhu cầu đã có, ngay hôm nay.
**Vì sao loại:** xử lý vấn đề bằng cách xoá luật thay vì xử lý ca cụ thể — cùng lỗi mà ADR-0037 đã dùng để loại Phương án B của nó.

### Phương án C (được chọn) — Kéo sớm vào F2

Xem mục Quyết định. Chi phí thật (thêm công dựng guard, wiring hai đường kỹ thuật, và một lỗ hổng cổng máy mới ở F30) được nêu ở Hệ quả tiêu cực, không giấu.

## Hệ quả

### Tích cực

- Đóng đúng lỗ hổng trước khi F2 có cơ hội bị coi là "xong" với một bug mất dữ liệu mà không nghiệm thu nào từng hỏi tới.
- Nghiệm thu F2 có mục kiểm cụ thể cho hành vi này — không còn khoảng trống giữa "đặc tả nói có" và "checklist không hỏi", đúng nguyên tắc ở `00-lo-trinh-tong-the.md` §8 ("không đóng một pha bằng cách dời mục chưa xong sang pha sau" một cách im lặng).
- Chi phí xây guard thấp hơn nhiều so với lúc ADR-0037 quyết định cho sáu component khác, vì `ConfirmDialog` — thứ guard này cần dùng để hỏi — đã tồn tại thật trong `src/FE` từ chính ADR-0037.
- F30 đặt tên đúng loại vi phạm cho lần tái diễn kế tiếp nếu có, cùng khuôn với F28 mà ADR-0037 đã tạo.

### Tiêu cực

- F2 tốn thêm công thật — chưa đo bằng số cụ thể — để dựng `core/guards/unsaved-changes.guard.ts`, wiring `canDeactivate` cho route của màn hồ sơ và các điều hướng nội bộ ở khung ứng dụng, cộng thao tác xoá cờ "có thay đổi" sau khi lưu thành công ở từng màn dùng guard.
- Guard phải đúng ở **hai đường khác nhau về kỹ thuật** — điều hướng trong app qua `CanDeactivateFn`, và đóng tab/tải lại qua `beforeunload` — theo đúng luật cuối của `fe-routing-guard.md` §4.1: "thiếu một đường là thủng một nửa". Đây là rủi ro thi công thật của quyết định này, không phải rủi ro giả định.
- Quyết định này, giống ADR-0037, **không tự sửa** `01-core-components.md` §4.1 và `03-f2-auth-routing.md` §7 — nằm ngoài phạm vi ghi của `architect`. Nếu không có ai áp đúng nội dung mục Quyết định #2, #3 vào hai file đó, tài liệu lộ trình và ADR này sẽ lệch nhau ngay từ ngày viết — đúng cảnh báo ở `.claude/CLAUDE.md` §3, và đúng rủi ro mà ADR-0037 đã tự ghi nhận cho chính nó mà tới hôm nay lỗ hổng A17 mới lộ ra là một phần hệ quả của việc đó.
- F30 ở `RULES.md` thuộc §10 — chưa có cổng máy, chỉ ép bằng review của `core-reviewer`, cùng giới hạn với F28: một trang tự vá bằng cơ chế cục bộ vẫn có thể lọt qua nếu không ai đối chiếu đúng screen spec của trang đó khi trang được dựng.

## Liên quan

- [`../quy-uoc/fe-routing-guard.md`](../quy-uoc/fe-routing-guard.md) §4.1, §4 — đặc tả đầy đủ của guard mà quyết định này thi hành, không sửa.
- [`../wiki-core/fe/01-core-components.md`](../wiki-core/fe/01-core-components.md) §2, §4.1 — bảng lộ trình cần một lượt sửa riêng theo mục Quyết định #2, ngoài phạm vi ghi của `architect`.
- [`../wiki-core/fe/trien-khai/03-f2-auth-routing.md`](../wiki-core/fe/trien-khai/03-f2-auth-routing.md) §7 — cùng lý do, cần thêm mục nghiệm thu theo mục Quyết định #3.
- [`../Design/Screens/00-khung-ung-dung.md`](../Design/Screens/00-khung-ung-dung.md), [`../Design/Screens/03-ho-so-ca-nhan.md`](../Design/Screens/03-ho-so-ca-nhan.md) — hai màn F2 thật đã xác nhận nhu cầu và đã ghi nợ "Chưa có" trong bảng "Có thật hôm nay → sẽ thành" của mình.
- [`0037-f2-dung-du-component-core-khong-hoan-ngam.md`](0037-f2-dung-du-component-core-khong-hoan-ngam.md) — tiền lệ trực tiếp cho cách kéo sớm một mảng Core vào F2 khi có nhu cầu thật, và cho giới hạn phạm vi ghi của `architect` lên `wiki-core/fe/`.
- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 — ngưỡng "hai nơi cần trở lên" áp dụng cho quyết định #1.
- [`../RULES.md`](../RULES.md) §7, §10 — luật F30 sinh ra từ quyết định này.
