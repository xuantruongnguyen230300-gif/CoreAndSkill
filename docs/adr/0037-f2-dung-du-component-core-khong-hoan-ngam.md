---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0037 — F2 dựng đủ Sidebar/Topbar/PageHeader/Card/ConfirmDialog/SkeletonLoader, không hoãn ngầm sang F3

> **Trạng thái:** Đã chấp nhận (2026-09-17)

## Bối cảnh

`core-reviewer` rà phạm vi FE pha F2 và phát hiện hai màn đã lệch khỏi `docs/Design/` mà không qua quy trình nào ghi lại quyết định đó.

`docs/quy-uoc/fe-architecture.md` §2.3 đã chốt: `platform/shell/` là tầng smart, phải truyền dữ liệu xuống hai component dumb `Sidebar` và `Topbar` dựng riêng ở `shared/components/`, "áp cho chúng như mọi component khác, không ngoại lệ". Thực tế `shell.component.html` dựng thẳng `<nav class="sidebar">` và `<header class="topbar">` bằng markup tay trong template của `ShellComponent`. Lý do duy nhất được ghi lại là một đoạn comment trong `shell.component.ts`, tự nhận là "đơn giản hoá có ghi nhận so với spec đầy đủ".

Cùng lúc, `docs/Design/Screens/03-ho-so-ca-nhan.md` — screen spec của màn hồ sơ cá nhân, cũng thuộc F2 theo cam kết A13 ở `docs/wiki-core/fe/01-core-components.md` §4.1 ("Hồ sơ cá nhân ở F2") — đòi bốn component `PageHeader`, `Card`, `ConfirmDialog`, `SkeletonLoader`. Trang `ho-so.page.html` thực tế dựng tay `<h1>`, `<section class="the">`, và một `<div role="alertdialog">` thay cho `ConfirmDialog`, không gọi bất kỳ component nào trong bốn cái trên.

Khi mở rộng tra cứu để hiểu vì sao, lộ ra một mâu thuẫn thật trong chính lộ trình đã chốt: cũng ở `01-core-components.md` §4.1, dòng "A8, A9, A10 — component, form, grid" (nhóm chứa cả bốn component trên) ghi "✅ sẽ có | F3 | Sinh ra từ nhu cầu thật của các màn quản trị F3" — tức lộ trình tự giả định màn hồ sơ cá nhân không cần chúng, trong khi chính screen spec của màn đó (nguồn UI duy nhất theo `.claude/CLAUDE.md` §7) lại đòi đúng bốn cái đó. A12 "Layout shell" — nhóm chứa Sidebar/Topbar — thì không có mâu thuẫn: roadmap đã ghi rõ "✅ sẽ có | F2".

`docs/Design/CLAUDE.md` §5 quy định chiều cập nhật duy nhất hợp lệ khi code cần lệch spec: (1) sửa spec, (2) ghi lại quyết định đổi — một dòng trong mục "Lịch sử quyết định" của spec, hoặc một ADR nếu là thay đổi lớn, (3) áp vào code. Không có bước nào trong ba bước đó đã xảy ra trước khi code lệch; chỉ có một comment trong code đóng vai một quyết định kiến trúc mà không ai canh cổng.

## Quyết định

1. `platform/shell/` dựng `Sidebar` và `Topbar` là hai component dumb riêng ở `shared/components/`, nhận dữ liệu qua `input()`/phát `output()`, đúng nguyên văn `fe-architecture.md` §2.3 — không có ngoại lệ cho F2. Đoạn comment hiện tại trong `shell.component.ts` không phải một quyết định hợp lệ và phải được gỡ khi component thật được dựng.
2. Bốn component `PageHeader`, `Card`, `ConfirmDialog`, `SkeletonLoader` — thuộc nhóm A8 — được kéo sớm vào phạm vi F2, đúng phiên bản tối thiểu mà màn hồ sơ cá nhân cần theo `Design/Screens/03-ho-so-ca-nhan.md`. Lý do kéo sớm không phải dự đoán nhu cầu tương lai — đây là nhu cầu **đã có thật** ngay trong F2, và `ConfirmDialog`/`Card` chắc chắn còn được F3 dùng lại, nên ngưỡng "từ hai nơi cần trở lên" của `kien-truc-core-module.md` §4.2 (áp dịch sang FE ở `01-core-components.md` §1) đã thoả. Phần còn lại của A8 (modal tổng quát ngoài `ConfirmDialog`, `EmptyState`, hạ tầng form) và toàn bộ A9/A10 (`shared/forms`, `DataTable`) vẫn ở F3 như lộ trình đã định — quyết định này không kéo cả nhóm A8, chỉ kéo bốn component có nhu cầu thật.
3. Biến thể chưa cần ngay (Sidebar dạng collapsed/drawer, flyout menu tài khoản, `LanguageSwitcher` — v1 chỉ một ngôn ngữ) được hoãn hợp lệ qua cơ chế trạng thái `🚧 đang dựng` đã có sẵn ở `docs/Design/CLAUDE.md` §4, với bảng "đã có → còn thiếu" ghi trong spec component tương ứng. Đây không phải ngoại lệ mới — đây là cơ chế đã tồn tại cho đúng tình huống "dựng chưa đủ so với spec".
4. Một quyết định lệch khỏi luật đã chốt không được ghi bằng comment trong code. Quy trình bắt buộc là ba bước ở `Design/CLAUDE.md` §5, hoặc một ADR khi thay đổi đủ lớn để chạm ranh giới hoặc đắt để đảo — như quyết định này.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Chấp nhận nợ tới F3, ghi vào `RULES.md` §10

**Được:** F2 đóng nhanh hơn, không tốn công dựng sáu component ngay trong pha này.
**Mất:** Hai màn (`shell`, `ho-so`) phải viết lại toàn bộ phần markup khi F3 tới — chi phí kép, không chi phí một lần. Không có cổng nào chặn pattern "dựng tay thay component có sẵn" lan sang các màn khác của F2/F3 trước khi bị phát hiện — đúng khuôn rủi ro mà nợ hardcode màu ở dự án tiền nhiệm đã minh chứng: nợ không có cổng tái sinh nhiều lần. Quan trọng hơn: chấp nhận nợ ở đây tương đương âm thầm mở một ngoại lệ cho đúng câu `fe-architecture.md` §2.3 đã chốt "không ngoại lệ", mà không qua ADR nào — làm câu đó mất giá trị cho mọi trường hợp tương tự sau này.
**Vì sao loại:** cái giá về niềm tin vào một luật "không ngoại lệ" đã chốt lớn hơn công tiết kiệm được ở một pha.

### Phương án B — Sửa `fe-architecture.md` §2.3, bỏ "không ngoại lệ"

**Được:** khớp đúng thực tế đang chạy, không đòi hỏi F2 tốn thêm công.
**Mất:** xoá luôn lý do A12 tồn tại — đảm bảo `Sidebar`/`Topbar` mang đi được sang sản phẩm thứ hai không phải viết lại. Mở khoá này thì bất kỳ pha sau nào cũng có thể lập luận "màn tôi cũng chưa có component nên tự dựng tay", và Core dần đầy những màn viết tay không dùng lại được — đúng nguy cơ "Core phình to/mục ruỗng" mà repo tồn tại để chặn.
**Vì sao loại:** phương án này xử lý bằng cách xoá luật thay vì xử lý ca cụ thể; nó giải quyết đúng vấn đề bằng cách làm vấn đề không còn tồn tại trên giấy.

### Phương án C (được chọn) — Dựng đủ sáu component tối thiểu ngay tại F2

Xem mục Quyết định. Chi phí thật (thêm công dựng sáu component trước khi đóng pha) được nêu rõ ở Hệ quả tiêu cực, không giấu.

## Hệ quả

### Tích cực

- Đóng đúng lỗ hổng quy trình trước khi nó lan: F27/F28 ([`../RULES.md`](../RULES.md) §10) đặt tên cho đúng loại vi phạm, nên lần tái diễn kế tiếp (nếu có) được gọi đúng tên ngay từ review đầu, không phải tranh luận lại "được tự dựng tay hay không" từ đầu.
- F2 khi đóng thực sự khớp `docs/Design/` — đúng vai của khu Design là nguồn UI duy nhất, không phải nguồn UI cho tới khi người thi công thấy bất tiện.
- `core-reviewer` các pha sau có một tiền lệ rõ để đối chiếu, thay vì phải tự suy luận lại ranh giới "component nào đủ quan trọng để không được viết tay".

### Tiêu cực

- F2 tốn thêm công thật — chưa đo bằng số cụ thể — để dựng sáu component (`Sidebar`, `Topbar`, `PageHeader`, `Card`, `ConfirmDialog`, `SkeletonLoader`) trước khi coi pha này xong. Đây là cái giá thật của quyết định, không phải chi phí danh nghĩa.
- Rủi ro dựng vội: nếu sáu component này được dựng chỉ đủ cho đúng hai màn F2 mà không nghĩ trước API sẽ được F3 dùng lại thế nào, F3 có thể phải đổi input/output của chúng — tốn thêm một vòng sửa. Người dựng phải đọc đúng spec Design của từng component (không chỉ phần F2 cần) khi viết chữ ký API, dù chưa cần dựng hết mọi biến thể.
- Quyết định này kéo sớm một phần của A8 mà không viết lại toàn bộ bảng lộ trình cùng lúc — `docs/wiki-core/fe/01-core-components.md` §4.1 và `docs/wiki-core/fe/trien-khai/03-f2-auth-routing.md` vẫn còn nguyên câu "A8 sẽ có ở F3" tại thời điểm ADR này được chấp nhận. Nếu không có ai sửa hai file đó theo đúng nội dung mục Quyết định, tài liệu lộ trình và ADR này sẽ lệch nhau — đúng lỗi mà `.claude/CLAUDE.md` §3 cảnh báo. ADR này **không tự sửa** hai file đó (ngoài phạm vi ghi của `architect`); việc sửa thuộc người đang giữ nội dung `wiki-core/fe/` — cần một lượt sửa riêng trỏ về ADR này.

## Liên quan

- [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.3 — luật A12 mà quyết định này thi hành, không sửa.
- [`../Design/CLAUDE.md`](../Design/CLAUDE.md) §5 — quy trình ba bước khi code cần lệch spec; quyết định này là ví dụ của bước "viết ADR khi thay đổi lớn".
- [`../wiki-core/fe/01-core-components.md`](../wiki-core/fe/01-core-components.md) §4.1 — bảng lộ trình A8/A12/A13 cần một lượt sửa theo mục Hệ quả tiêu cực ở trên.
- [`../wiki-core/fe/trien-khai/03-f2-auth-routing.md`](../wiki-core/fe/trien-khai/03-f2-auth-routing.md) — cùng lý do.
- [`../RULES.md`](../RULES.md) §7, §10 — luật F27, F28 sinh ra từ quyết định này.
- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 — ngưỡng "từ hai module/màn cần trở lên" áp dụng cho việc kéo sớm bốn component ở mục Quyết định #2.
