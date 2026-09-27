---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0038 — Preset PrimeNG dựng từ sub-preset theo phần dùng, không import object `Aura` gộp

> **Trạng thái:** Đã chấp nhận (2026-09-17) · Sửa một phần bởi [ADR-0039](0039-sua-mot-phan-0038-hai-sub-preset-khong-phai-bon.md) (2026-09-17) · Sửa một phần bởi [ADR-0056](0056-f29-tinh-component-con-duoc-dung-ben-trong.md) (2026-09-22)

## Bối cảnh

`frontend-expert` chạy `ng build` production của `src/FE` và thấy bundle initial **506.93 kB**, vượt `maximumWarning: 500kB` khai ở `src/FE/angular.json` (luật F14, `docs/RULES.md`). Build chưa đỏ — `maximumError` vẫn `1MB`.

Điều tra đã loại trừ nguyên nhân "thiếu lazy-split route": mọi route F2 đã đúng `loadChildren`/`loadComponent`. Code tự viết trong bundle khởi động chỉ khoảng 6.9 kB; phần lớn còn lại là hạ tầng buộc phải eager theo đúng đặc tả đã chốt (interceptor chain, guard đồng bộ trên route gốc, `ToastComponent` toàn app, `provideAppInitializer` gọi `xsrf.lamMoi()`/`auth.napPhienKhoiDong()`).

Nguyên nhân đo được: `src/FE/src/app/core/theme/prime-preset.ts` import `Aura` — object default-export duy nhất của `@primeuix/themes/aura`, gộp tĩnh khoảng 90 sub-preset (mỗi loại control PrimeNG một sub-preset: `button`, `toast`, `message`, `datatable`, `select`, `datepicker`, `treetable`, `autocomplete`...). Package này tách sẵn export theo sub-path cho từng component của từng theme (`@primeuix/themes/aura/<tên>` — xác nhận có `base`, `button`, `toast`, `message` và khoảng 90 thư mục khác dưới `node_modules/@primeuix/themes/dist/aura/`), nên chọn lọc import là khả thi kỹ thuật, không cần công cụ tree-shaking đặc biệt nào.

Ứng dụng hôm nay chỉ thực dùng **một** component PrimeNG trực tiếp: `Toast` (qua `primeng/toast`) và `MessageService` (`primeng/api`) trong `shared/ui/toast/` — nơi duy nhất được phép import `primeng/*` ngoài `core/theme` (luật F5). `app-button` (`shared/ui/button/`) là component tự dựng, **không** bọc PrimeNG `Button`. Sub-preset thật đang cần chỉ khoảng 4 (`base` + `button` + `toast` + `message`, tổng ~30.9 kB đo được qua build stats); phần còn lại — khoảng **76.36 kB**, hơn 10 lần khoản vượt ngưỡng 6.93 kB — là ~86 sub-preset của những control chưa màn nào trong Core dùng tới.

`docs/wiki-core/fe/04-design-token-system.md` §7 đã cố ý để ngỏ cú pháp import cụ thể: *"Cú pháp API PrimeNG của phiên bản đã chốt chưa được xác minh trong repo... Cú pháp chốt khi thi công F1"*. Việc chốt cú pháp đó chính là việc ADR này quyết định.

Repo đã có một tiền lệ đúng dạng vấn đề này: ADR-0019 (biểu đồ, lưới nhập liệu, chọn khoảng ngày thuộc Core) lập nguyên tắc *"một dự án không dùng thì không trả giá gì"* cho một loại chi phí khác (thư viện biểu đồ), và ghi nhận rõ *"F14 là một trần kích thước, không phải allowlist nội dung — nó không biết cái gì nằm trong bundle"* (khai thành nợ D25/D26 ở `RULES.md`). Nguyên tắc đó bắt nguồn từ ADR-0016: Core phân phối bằng clone, nên chi phí thừa trong Core bị nhân theo số dự án hạ nguồn, không phải trả một lần.

## Quyết định

1. `prime-preset.ts` dựng preset bằng cách import **đúng** các sub-preset Aura tương ứng những component PrimeNG mà `src/FE` **thực sự** dùng trực tiếp (hôm nay: `base`, `button`, `toast`, `message`) — **không** import object `Aura` gộp của `@primeuix/themes/aura`.
2. Thêm luật **F29** vào `docs/RULES.md`: mọi module `primeng/<x>` được import trong `src/FE` mà `<x>` là một component (loại trừ `primeng/api`, `primeng/config` — không phải component) phải có sub-preset `<x>` tương ứng đã import trong `prime-preset.ts`; và `prime-preset.ts` không được import object `Aura` gộp.
3. `angular.json` giữ nguyên `budgets.initial.maximumWarning: 500kB` — **không** nâng ngưỡng để né cảnh báo.
4. `docs/wiki-core/fe/04-design-token-system.md` §7 cần một lượt sửa riêng để khai cụ thể cú pháp import đã chốt ở mục 1 — việc này nằm ngoài phạm vi ghi của `architect` (giữ đúng tiền lệ đã nói rõ ở ADR-0037), thuộc người đang giữ nội dung `wiki-core/fe/`.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Nâng `maximumWarning` (và/hoặc `maximumError`) ở `angular.json`

**Được:** rẻ nhất, một dòng cấu hình, không đổi code, không tạo rủi ro bảo trì mới.

**Mất:** Không sửa nguyên nhân — mọi dự án hạ nguồn (Core phân phối bằng clone, ADR-0016) tiếp tục trả 76.36 kB cho ~86 sub-preset PrimeNG mà dự án đó chưa chắc bao giờ dùng, mãi mãi — đúng ngược nguyên tắc ADR-0019 đã lập cho một loại chi phí tương tự. Ngưỡng "đúng" cho một Core **chưa có module nghiệp vụ nào** là một số không ai biết trước; nới ngay ở lần cảnh báo đầu tiên, khi Core còn trống, là tiền lệ tệ nhất có thể đặt — mọi lần cảnh báo sau sẽ noi theo, và ngưỡng mất hết ý nghĩa canh gác. Đây là bản áp cho một con số của đúng hành vi mà `.claude/CLAUDE.md` §4 cấm cho văn xuôi: sửa mô tả cho khớp thực tế đang có rồi coi là xong.

**Vì sao loại:** cái mất (che một chi phí thật, nhân bản qua mọi dự án hạ nguồn, đặt tiền lệ nới ngưỡng ngay từ vạch xuất phát) lớn hơn cái được (rẻ, không rủi ro).

### Phương án B — Giữ import `Aura` gộp, chấp nhận 506.93 kB là chi phí hạ tầng phải trả

**Được:** không đổi gì; đây cũng là cách phổ biến trong tài liệu/ví dụ chính thức của PrimeNG.

**Vì sao loại:** tiền đề sai. Đã đo được rằng chỉ 30.9/107.26 kB của preset là thứ đang dùng thật; phần còn lại là suy đoán "để sau dùng tới" — đúng khuôn dự đoán nhu cầu tương lai mà nguyên tắc "ngưỡng hai module" của Core cấm áp cho phụ thuộc/hiện thực (`kien-truc-core-module.md` §4.2). Khác với ADR-0019 — nơi lập luận "mọi dự án trong lớp này đều cần" được chứng minh bằng luận cứ nghiệp vụ cụ thể cho từng component — ở đây không có luận cứ nào cho việc nạp trước 86 sub-preset chưa ai cần.

### Phương án C (được chọn) — Chọn lọc sub-preset theo phần dùng

Xem mục Quyết định. Chi phí thật nêu ở Hệ quả, không giấu.

## Hệ quả

### Tích cực

- Bundle initial giảm ước tính về ~430 kB (506.93 − 76.36 kB), dưới ngưỡng cảnh báo 500kB mà không cần đổi `angular.json`.
- Mọi dự án hạ nguồn clone Core chỉ trả chi phí bundle cho đúng những control PrimeNG Core thật dùng — mở rộng đúng nguyên tắc ADR-0019 đã lập cho `Chart`/`EditableGrid`/`DatePicker` sang chính hạ tầng theme.
- Ngưỡng cảnh báo F14 giữ nguyên ý nghĩa canh gác — không bị nới ngay từ lần cảnh báo đầu tiên khi Core còn chưa có module nghiệp vụ nào.

### Tiêu cực — cái giá thật

- **Rủi ro lỗi im lặng mới, không có ở phương án A/B.** Mỗi khi một module nghiệp vụ (hoặc Core ở pha F3+) dùng thêm một loại control PrimeNG mà quên thêm sub-preset tương ứng vào `prime-preset.ts`, component đó vẫn chạy nhưng **sai theme** (dùng token mặc định của PrimeNG thay vì `var(--color-*)` của Core) — không lỗi biên dịch, không test nào bắt cho tới khi F29 có cổng thật.
- Cho tới khi F29 có cổng (hiện ở danh sách nợ `RULES.md` §10, "🔧 làm được ngay" nhưng chưa ai viết), lớp bắt duy nhất là review bằng mắt — đúng khuôn rủi ro mà D27 (từ vựng nghiệp vụ rò vào ba component Core của ADR-0019) đã minh chứng: đã rò hai lần, cả hai do người đọc bắt, không do cổng.
- **Trái thói quen phổ biến của cộng đồng PrimeNG** — import `Aura` nguyên khối là cách hầu hết tài liệu/ví dụ chính thức hướng dẫn. Người đọc `prime-preset.ts` sau này có thể tưởng import rời rạc là thiếu sót rồi "sửa lại cho gọn" bằng cách gộp về `Aura` — hoàn tác quyết định này mà không biết lý do. Đây là lý do chính quyết định này cần một ADR, không chỉ một dòng quy ước.
- `docs/wiki-core/fe/04-design-token-system.md` §7 cần một lượt sửa riêng (ngoài phạm vi ghi của `architect`) để khai cú pháp cụ thể. Không ai sửa thì ADR này và tài liệu quy ước sẽ lệch nhau — đúng lỗi `.claude/CLAUDE.md` §3 cảnh báo.

### Điều kiện lật quyết định

Lật ADR này khi số sub-preset thật cần dùng tăng lên gần hết danh sách (~90) — lúc đó lợi ích chọn lọc gần như biến mất trong khi rủi ro "quên thêm sub-preset" vẫn còn. Rút lui rẻ: quay lại import `Aura` gộp là một dòng code, không có dữ liệu nào sinh ra cần dọn, không có migration.

## Liên quan

- [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md) — lý do chi phí thừa trong Core bị nhân theo số dự án hạ nguồn.
- [`0019-ba-component-nang-thuoc-core.md`](0019-ba-component-nang-thuoc-core.md) — nguyên tắc "không dùng thì không trả giá" lập lần đầu cho `Chart`/`EditableGrid`/`DatePicker`; ADR này áp lại nguyên tắc đó cho theme preset. §Ràng buộc 1–2 và nợ D25/D26 cùng nhận định "F14 là trần kích thước, không phải allowlist nội dung" — lộ rõ nhất ở chính vụ việc này.
- [`0037-f2-dung-du-component-core-khong-hoan-ngam.md`](0037-f2-dung-du-component-core-khong-hoan-ngam.md) — tiền lệ về phạm vi ghi của `architect` không gồm `wiki-core/fe/`.
- [`../RULES.md`](../RULES.md) §7 F14, F29 và §10 (nợ F29) — cổng liên quan.
- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 — ngưỡng "hai module cần" mà Phương án B áp nhầm cho phụ thuộc/hiện thực.
- [`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md) §7 — cần cập nhật cú pháp cụ thể (ngoài phạm vi ghi của `architect`).
