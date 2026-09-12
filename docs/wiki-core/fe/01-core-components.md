---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 01. Một Core FE gồm những gì

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo đang ở giai đoạn 1, chưa có `src/`. Toàn bộ file này mô tả thứ `src/FE` **phải trở thành**, không phải mô tả hiện trạng.
>
> **File chủ của câu hỏi "Core FE đủ chưa, còn thiếu mảng nào".** Ai muốn biết *thi công thế nào* thì đọc [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md); file này chỉ trả lời *cần có gì và vì sao*.

---

## 1. Phép thử: cái gì thuộc Core FE

Dùng đúng phép thử của [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §4, dịch sang phía trình duyệt:

> **Một thứ thuộc Core FE khi mọi sản phẩm dựng trên nền tảng này đều cần nó, và khi gỡ nó ra thì màn hình đầu tiên của sản phẩm mới không dựng được.**

Bốn câu hỏi phụ giúp phân loại nhanh:

| Câu hỏi | Trả lời "có" nghĩa là |
| --- | --- |
| Màn đăng nhập của một sản phẩm hoàn toàn khác có cần nó không? | Thuộc `core/` |
| Nó là *cách hiển thị* một thứ, dùng lại được ở nhiều màn, và không biết gì về API? | Thuộc `shared/` |
| Nó là màn hình quản trị chung của nền tảng (người dùng, quyền, đổi mật khẩu)? | Thuộc `platform/` |
| Nó chỉ có nghĩa với một nghiệp vụ cụ thể? | Thuộc `modules/` — **không** phải Core |

Ngưỡng định lượng giữ nguyên như BE: **code chỉ được nâng lên Core khi từ hai nơi trở lên cần nó.** Một màn cần thì để ở màn đó.

---

## 2. Nhóm A — bắt buộc, thiếu là Core không dùng được

Đây là những mảng mà **không có chúng thì màn hình đầu tiên của sản phẩm thứ hai không chạy nổi**, hoặc chạy nhưng mỗi dự án phải tự viết lại theo cách riêng — tức đã mất lý do tồn tại của Core.

| # | Thành phần | Tầng | Vì sao bắt buộc | Chi tiết |
| --- | --- | --- | --- | --- |
| A1 | **Envelope HTTP + chuỗi interceptor** | `core/http` (kiểu, unwrap) + `core/interceptors` (chuỗi) | Mọi lời gọi API đi qua đây. Sai ở đây là sai lan ra toàn app; đúng ở đây thì mọi service sau này không phải bắt lỗi thủ công | [`02-http-envelope.md`](02-http-envelope.md) · [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2 |
| A2 | **Xử lý lỗi tập trung** | `core/http` | Nếu mỗi service tự hiển thị lỗi theo cách riêng, câu chữ và hành vi sẽ khác nhau giữa các màn — người dùng thấy một app hai tính cách | [`02-http-envelope.md`](02-http-envelope.md) §4 |
| A3 | **Phiên và danh tính** | `core/auth` | Cookie phiên, XSRF, người dùng hiện tại dạng signal, xử lý hết phiên | [`07-auth-identity.md`](07-auth-identity.md) |
| A4 | **Guard và routing** | `core/guards` | Chặn theo phiên, ép đổi mật khẩu lần đầu, chặn theo **permission** | [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) |
| A5 | **Menu động theo quyền** | `core/menu` | Sản phẩm khác có tập màn khác. Menu hardcode nghĩa là Core biết tên nghiệp vụ — vi phạm ranh giới | [`07-auth-identity.md`](07-auth-identity.md) §6 |
| A6 | **Hệ design token** | `core/theme` + stylesheet toàn cục | Không có token thì mỗi màn tự chọn màu, và đổi thương hiệu cho sản phẩm thứ hai là sửa hàng trăm chỗ | [`04-design-token-system.md`](04-design-token-system.md) |
| A7 | **Lớp bọc thư viện UI** | `shared/ui` | Đổi hoặc nâng thư viện UI mà không phải sửa mọi màn nghiệp vụ | [`05-component-library.md`](05-component-library.md) |
| A8 | **Bộ component dùng chung** | `shared/components` | Bảng dữ liệu, form field, modal, confirm, toast, empty state, skeleton, page header | [`05-component-library.md`](05-component-library.md) §3 |
| A9 | **Hạ tầng form** | `shared/forms` (có trong sơ đồ `shared/` ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2) | Typed reactive form, hiển thị lỗi theo ô, bind `fieldErrors` từ BE | [`09-forms-validation.md`](09-forms-validation.md) |
| A10 | **Bảng dữ liệu server-side** | `shared/components/data-grid` | Phân trang, sắp xếp, lọc ở server là mặc định của hệ quản trị; làm sai chỗ này thì mọi màn danh sách đều chậm | [`11-grid-and-metadata.md`](11-grid-and-metadata.md) |
| A11 | **Đa ngôn ngữ** | `core/i18n` | Không phải vì hôm nay cần hai thứ tiếng, mà vì câu chữ phải nằm ngoài template — xem §5.1 | [`08-i18n.md`](08-i18n.md) |
| A12 | **Layout shell** | `platform/shell` | Topbar, sidebar, breadcrumb, vùng nội dung; cờ tắt shell cho màn đăng nhập | [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) |
| A13 | **Màn quản trị Core** | `platform/*` | Danh sách đầy đủ và nguồn duy nhất của nó: [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.3. Gồm cả **hồ sơ cá nhân**, **quản trị vai trò** và **khu quản trị đơn vị** cho tài khoản vận hành ([`../../contracts/tenants.md`](../../contracts/tenants.md)), tách hẳn khỏi các màn nghiệp vụ | [`trien-khai/04-f3-man-quan-tri.md`](trien-khai/04-f3-man-quan-tri.md) |
| A14 | **Ranh giới tầng có máy kiểm** | cấu hình ESLint + cổng | Không có compiler ép layering như BE. ESLint là hàng rào duy nhất | [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
| A17 | **Guard chặn rời trang khi chưa lưu** | `core/guards` | Form nghiệp vụ hành chính thường dài. Mỗi màn tự lo thì hành vi không nhất quán và màn nào quên là người dùng mất dữ liệu | [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §4.1 |
| A16 | **Khu thông báo trong ứng dụng** | `platform/notifications` | BE đã có endpoint ở v1 ([`../../contracts/notifications.md`](../../contracts/notifications.md)). Chuông đếm số chưa đọc, danh sách, đánh dấu đã đọc. Câu chữ dựng từ khoá và tham số, **không** nhận câu ghép sẵn từ server | [`../be/12-notifications.md`](../be/12-notifications.md) §5 |
| A15 | **Bộ test và ngưỡng** | `*.spec.ts` cạnh file | Mọi service có spec cạnh nó, nếu không thì "đã test" chỉ là một câu nói | [`06-testing-strategy.md`](06-testing-strategy.md) |

**A14 là mảng dễ bị coi thường nhất.** Ở BE, `Core.Domain` không tham chiếu `Core.Infrastructure` thì code trong Domain **không thể** gọi `DbContext` — compiler ép, chi phí bằng 0, không bao giờ hỏng. FE không có cơ chế tương đương: một thư mục là một thư mục, và một import đi ngược tầng vẫn biên dịch bình thường. Ranh giới FE chỉ tồn tại khi có người viết lint rule cho nó, và chỉ **còn** tồn tại khi có cổng cấm gỡ lint rule đó bằng một dòng comment ([`../../RULES.md`](../../RULES.md) F1–F3).

---

## 3. Nhóm B — nên có, nhưng đúng lúc

Nhóm B khác Nhóm A ở một điểm: **thiếu chúng thì app vẫn chạy và vẫn dùng được**, chỉ là thiếu một lớp bảo vệ hoặc một tiện nghi. Cài trước khi có nhu cầu là thêm phụ thuộc để đi tìm bài toán.

| # | Thành phần | Ngưỡng nên làm | Chi tiết |
| --- | --- | --- | --- |
| B1 | **Thu lỗi runtime FE** | Khi có môi trường thật và người dùng thật báo lỗi mà không tái hiện được | [`10-observability.md`](10-observability.md) |
| B2 | **Đo Core Web Vitals** | Khi có phàn nàn về tốc độ, hoặc khi bundle vượt ngân sách hai lần liên tiếp | [`13-performance.md`](13-performance.md) |
| B3 | **Thư viện biểu đồ** | Khi màn đầu tiên dùng biến thể `line` của `Chart`. Component biểu đồ **thuộc Core** ([`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md)); riêng **thư viện** vẫn nạp theo yêu cầu, không vào bundle khởi động | [`12-charting.md`](12-charting.md) |
| B4 | **Tải tệp lên** | Khi màn nghiệp vụ đầu tiên cần đính kèm. **Phía BE đã thuộc v1** ([`../../contracts/files.md`](../../contracts/files.md)), nên phần còn thiếu chỉ là component tải lên và hiển thị danh sách tệp | [`05-component-library.md`](05-component-library.md) §7 |
| B5 | **Virtual scroll** | Khi một bảng thật sự phải hiển thị hàng nghìn dòng cùng lúc mà không phân trang được | [`13-performance.md`](13-performance.md) §6 |
| B6 | **Đồng bộ giữa nhiều tab** | Khi có luồng thật mở hai tab song song và dữ liệu lệch gây hậu quả | [`03-state-management.md`](03-state-management.md) §7 |
| B7 | **Feature flag** | Khi cần bật/tắt tính năng theo môi trường mà không deploy lại | chưa chốt |
| B8 | **Store chuyên dụng (NgRx)** | Khi thoả ngưỡng khai ở [`03-state-management.md`](03-state-management.md) §3 | [`03-state-management.md`](03-state-management.md) |
| B9 | **Xuất dữ liệu phía client** | Khi cần xuất thứ server không dựng được (ví dụ đúng cột đang hiển thị) | [`11-grid-and-metadata.md`](11-grid-and-metadata.md) §8 |
| B10 | **PWA / hoạt động ngoại tuyến** | Khi có người dùng thật làm việc ở nơi mất mạng | chưa chốt |

---

## 4. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới đây là **cam kết phạm vi** của giai đoạn 2, không phải mô tả hiện trạng. Cột "Pha" trỏ tới [`trien-khai/00-lo-trinh-tong-the.md`](trien-khai/00-lo-trinh-tong-the.md); ý nghĩa ba ký hiệu trạng thái ở [`../README.md`](../README.md) §9.

### 4.1 Sẽ làm

| Thành phần | Trạng thái | Pha | Ghi chú phạm vi |
| --- | --- | --- | --- |
| A1, A2 — envelope + interceptor | ✅ sẽ có | F0 | Chuỗi interceptor có **thứ tự** là ràng buộc thật, không phải chi tiết |
| A14 — ranh giới ESLint + cổng | ✅ sẽ có | F0 | Bật từ ngày đầu, không đợi tới khi có vi phạm |
| A15 — bộ test | ✅ sẽ có | F0 trở đi | Spec đi cùng file, không gom về cuối dự án |
| A6 — design token | ✅ sẽ có | F1 | Nguồn là [`../../Design/DESIGN.md`](../../Design/DESIGN.md), code đuổi theo |
| A7 — lớp bọc UI | ✅ sẽ có | F1 | Bọc trước khi có màn nghiệp vụ nào, vì bọc sau là sửa mọi màn |
| A3, A4, A5 — phiên, guard, menu | ✅ sẽ có | F2 | Permission-based, **không** role-based |
| A11 — i18n | ✅ sẽ có | F2 | Hạ tầng bật từ F2; bản dịch thứ hai có thể về sau |
| A12 — layout shell | ✅ sẽ có | F2 | Cùng lúc với màn đăng nhập, vì màn đó cần cờ tắt shell |
| A8, A9, A10 — component, form, grid | ✅ sẽ có | F3 | Sinh ra từ nhu cầu thật của hai màn quản trị |
| A13 — màn quản trị Core | ✅ sẽ có | F3 | Người dùng + phân quyền |

### 4.2 Cố ý chưa làm ở v1 — kèm lý do và điều kiện mở lại

| Hạng mục | Trạng thái | Vì sao | Điều kiện mở lại |
| --- | --- | --- | --- |
| **NgRx / store chuyên dụng** (B8) | ❌ chưa | Signals đã là cơ chế reactivity gốc của Angular. Thêm một tầng store nữa nghĩa là mỗi dev phải học hai mô hình, và mỗi thay đổi state đi qua ba file thay vì một | Thoả ngưỡng khai ở [`03-state-management.md`](03-state-management.md) §3 |
| **Thư viện biểu đồ** (B3) | ❌ chưa | Mọi biến thể của `Chart` trừ `line` tự dựng bằng HTML/CSS, không cần thư viện nào. Cài sẵn là gánh bundle cho phần lớn dự án không chạm tới biến thể `line` | Màn đầu tiên dùng biến thể `line`, và khi đó **nạp theo yêu cầu** — [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md) ràng buộc 1 |
| **Sink lỗi runtime bên ngoài** (B1) | ❌ chưa | Chưa có môi trường thật, chưa có người dùng thật. Gắn sớm là gắn vào một hộp không ai mở | Có môi trường staging dùng thật, hoặc lỗi đầu tiên không tái hiện được |
| **Đo Web Vitals tự động** (B2) | ❌ chưa | Đo được thì phải có người đọc số. Chưa có ai đọc và chưa có ngưỡng để so | Có ngân sách bundle đã chốt bằng số đo thật ([`13-performance.md`](13-performance.md) §4) |
| **Cache phía client theo thời gian sống** | ❌ chưa | Cùng lý do với quyết định hoãn Redis phía BE: chưa có số đo nào cho thấy đang chậm. Cache sai là dữ liệu cũ hiển thị như dữ liệu mới — sai kiểu khó phát hiện nhất | Có số đo cho thấy một endpoint bị gọi lặp và tốn |
| **Angular workspace / Nx** | ❌ loại, không hoãn `K10` | Ranh giới bằng thư mục + ESLint đủ cho một app. Workspace đổi lấy ranh giới cứng hơn bằng chi phí build và thêm một lớp cấu hình phải bảo trì | [`../../adr/0007-fe-giu-cau-truc-thu-muc.md`](../../adr/0007-fe-giu-cau-truc-thu-muc.md) ghi điều kiện lật |
| **SSR / hydration** | ❌ loại, không hoãn `K11` | Đây là hệ quản trị nội bộ, mọi màn nằm sau đăng nhập. SSR không cải thiện SEO cho màn không được index, và thêm một runtime Node phải vận hành và vá | Có màn công khai cần SEO — nhưng đó là đổi bản chất sản phẩm, nên phải đi qua ADR |

### 4.3 Cách dùng bảng 4.2 khi review

**Cách đọc bảng 4.2 cho đúng:** đây **không** phải danh sách việc tồn đọng. Mỗi dòng là một quyết định đã cân nhắc, và cột "Điều kiện mở lại" là điều kiện kiểm được. Ai muốn thêm một trong số đó vào Core phải chỉ ra điều kiện đã thoả — không phải chỉ ra rằng "dự án khác cũng có".

Một thành phần Nhóm A vắng mặt thì **luôn** là finding, trừ khi có ADR nói khác. Một dòng **❌ loại, không hoãn** chỉ đổi được bằng một ADR mới — không đổi được bằng một finding.

---

## 5. Ba mảng hay bị bỏ sót vì trông không giống "tính năng"

### 5.1 i18n không phải để dịch — mà để câu chữ ra khỏi template

Lý do thật của A11 không phải "ngày mai có tiếng Anh". Lý do là: khi câu chữ nằm rải trong template, **không ai biết app đang nói những gì**. Sửa một thông báo phải tìm khắp repo; và câu tiếng Việt viết cho Core sẽ đi thẳng sang sản phẩm thứ hai có cách xưng hô hoàn toàn khác.

Đây cũng là lý do luật F8 ([`../../RULES.md`](../../RULES.md)) cấm chữ tiếng Việt trong template: nó biến một quy ước mềm thành thứ máy kiểm được.

### 5.2 Lớp bọc UI phải có TRƯỚC màn hình đầu tiên

Bọc thư viện UI là việc rẻ khi làm trước và đắt khi làm sau. Trước: viết một nhúm file trong `shared/ui/`. Sau: sửa mọi màn đã viết, và mỗi lần sửa là một cơ hội làm hỏng một màn đang chạy.

Ở dự án tiền nhiệm, một component bảng của thư viện UI được import ở đúng một chỗ trong một nhánh lazy, và điều đó kéo cả thư viện bảng vào một chunk lớn hơn cả bundle khởi động. Không cổng nào bắt, vì ngân sách chỉ khai cho phần khởi động. Bọc sẵn ở `shared/ui/` không tự giải quyết bài toán bundle, nhưng nó cho **một chỗ duy nhất** để giải quyết.

### 5.3 Cổng phải sinh ra cùng lúc với luật

Luật không có cổng là gợi ý ([`../../RULES.md`](../../RULES.md) §10). Điều này đúng gấp đôi ở FE, vì FE không có compiler canh ranh giới.

Ở dự án tiền nhiệm, một script cổng FE **không tồn tại trên đĩa** trong khi tài liệu vẫn hướng dẫn chạy nó. Người chạy thấy `No such file or directory` ở lệnh đầu, ba lệnh sau chạy bình thường, và kết luận cổng đã xanh. Ba mục cổng không có gì canh suốt thời gian dài. Xem [`../../audit/2026-08-23-cong-khong-ton-tai.md`](../../audit/2026-08-23-cong-khong-ton-tai.md).

---

## 6. Rủi ro lớn nhất không phải thiếu — mà là phình

Một Core FE thiếu tính năng thì dự án tự bù được. Một Core FE **chứa nghiệp vụ** thì không mang đi đâu được, và đó là thất bại ở đúng lý do nó tồn tại.

Ba dấu hiệu phình, phát hiện được bằng cách đọc:

| Dấu hiệu | Ví dụ | Xử lý |
| --- | --- | --- |
| Core biết **tên** nghiệp vụ | Một khoá i18n cho một mục menu nghiệp vụ, một route nghiệp vụ khai cứng trong `core/` | Đưa xuống `modules/`; menu phải đến từ server |
| Component `shared/` có thuộc tính mang nghĩa nghiệp vụ | Một badge nhận vào đúng danh sách trạng thái của một quy trình cụ thể | Đổi thành `variant` trung tính, nghĩa do nơi gọi quyết định |
| `core/` giữ hằng số danh sách | Danh sách trạng thái đơn, danh sách vai trò | Dữ liệu thuộc DB hoặc thuộc module. Xem [`../../adr/0005-permission-based.md`](../../adr/0005-permission-based.md) |

**Cơ chế chống:** mọi thay đổi chạm `core/` hoặc `shared/` phải qua agent `architect` và phải có ADR — cùng cơ chế với phía BE ([`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §4.2).

---

## 7. Đếm bằng lệnh, đừng chép số

Theo [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6, không chép số lượng component, service hay interceptor vào tài liệu. Khi `src/FE` tồn tại:

```bash
ls src/FE/src/app                                        # các tầng thật sự có
find src/FE/src/app/shared -name '*.ts' | wc -l          # kích thước lớp dùng chung
grep -rn 'withInterceptors' src/FE/src/app/app.config.ts # chuỗi interceptor — đọc mảng tại chỗ
find src/FE/src/app -name '*.service.ts' | wc -l
find src/FE/src/app -name '*.service.spec.ts' | wc -l    # hai số này phải bằng nhau — luật F12
```

Hai lệnh cuối cho một phép thử rẻ và không bao giờ sai: **số service phải bằng số spec.** Lệch một là có một service không ai kiểm.

---

## 8. Đối xứng với Core BE — chỗ giống và chỗ không

Hai phía cùng một nền tảng nên nhiều khái niệm khớp nhau, nhưng có ba chỗ **cố ý không đối xứng**. Nhầm chúng là nguồn của những tranh luận không có hồi kết trong review.

| Khái niệm | Phía BE | Phía FE | Có đối xứng không |
| --- | --- | --- | --- |
| Ranh giới tầng | ProjectReference, compiler ép | Thư mục, ESLint ép | **Không** — FE yếu hơn, nên cần thêm luật cấm `eslint-disable` |
| Lỗi nghiệp vụ | `Result<T>`, không ném exception | Envelope trong body, interceptor dịch một lần | Có, cùng tinh thần: lỗi là **dữ liệu**, không phải luồng ngoại lệ |
| Phân quyền | Kiểm bằng permission ở handler/endpoint | Ẩn/hiện + guard bằng permission | Có — nhưng FE **không phải** lớp bảo vệ, xem [`14-security.md`](14-security.md) §2 |
| Câu hiển thị | Không hardcode trong BE, chỉ mã + tham số | Dịch mã thành câu bằng i18n | Có, và đây là cặp phải khớp từng tên tham số |
| Cấu hình | `Options` + `ValidateOnStart` | Không có tầng cấu hình runtime ở v1 | **Không** — xem [`17-phuc-vu-va-trien-khai.md`](17-phuc-vu-va-trien-khai.md) §5 |
| Migration / phiên bản dữ liệu | Core sở hữu schema `core` | Không có khái niệm tương đương | **Không** |

**Chỗ lệch quan trọng nhất là dòng phân quyền.** Phía BE, một kiểm tra permission thiếu là một lỗ hổng. Phía FE, một kiểm tra permission thiếu là một **lỗi giao diện** — người dùng thấy một nút mà bấm vào sẽ nhận 403. Ngược lại, một kiểm tra permission *có* ở FE không bao giờ thay thế được kiểm tra ở BE. Ai đọc bảng này rồi kết luận "FE đã chặn nên BE khỏi chặn" là đã hiểu ngược.

---

## 9. Bốn thứ Core FE cố tình KHÔNG cung cấp

Danh sách này tồn tại để module không đi tìm thứ không có, và để không ai "bổ sung cho đủ".

| Không cung cấp | Vì sao | Module tự làm thế nào |
| --- | --- | --- |
| **Base class cho component** | Kế thừa component là cách nhanh nhất để một thay đổi ở Core làm vỡ mọi màn nghiệp vụ, mà không chỗ nào trong module thay đổi cả. Angular hiện đại dùng composition (`inject()`, directive, signal) thay kế thừa | Inject service dùng chung; dùng directive cho hành vi lặp lại |
| **Base class cho service** | Cùng lý do. Thêm nữa: một `BaseService<T>` luôn tiến hoá thành nơi chứa mọi thứ ai đó thấy "chung chung" | Gọi thẳng `HttpClient`, hoặc gọi **hàm** CRUD dùng chung ở [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §5 — composition, không kế thừa |
| **Store toàn cục dùng chung cho mọi feature** | Một store toàn cục là "god component" ở dạng state. Feature A đọc state feature B nghĩa là hai feature không tách được nữa | Mỗi feature một store riêng, xem [`03-state-management.md`](03-state-management.md) |
| **Route khai sẵn cho màn nghiệp vụ** | Core khai route nghiệp vụ nghĩa là Core biết tên nghiệp vụ — vi phạm đúng luật ở §6 | Module tự khai `*.routes.ts` và tự đăng ký lazy |

> **"Không base class cho service" ≠ "không có gì dùng chung cho service".** CRUD và phân trang lặp ở mọi feature, và Core **có** cung cấp chúng — dưới dạng **hàm thuần** nhận mapper làm tham số ([`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §5), không dưới dạng lớp để kế thừa. Khác biệt không phải chuyện phong cách:
>
> - Kế thừa buộc mọi service mang theo **toàn bộ** bề mặt của lớp cha, kể cả phần nó không dùng — đó là đường mà "nơi chứa mọi thứ" hình thành.
> - Hàm thì service chỉ nhận đúng thứ nó gọi, và **mapper là tham số bắt buộc**, nên không có đường nào lấy DTO ra khỏi tầng HTTP mà bỏ qua mapper (luật F10). Cơ chế này thay cho việc để phương thức của base ở mức `protected` — mạnh hơn, vì quên mapper là lỗi biên dịch chứ không phải lỗi phạm vi truy cập.
>
> Chốt: **giữ dòng ở bảng trên** — không có base class, thứ dùng chung đi bằng hàm.

---

## 10. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Thi công một feature FE thế nào | [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) |
| Luật FE nào được ép bằng gì | [`../../RULES.md`](../../RULES.md) §7 |
| Làm theo thứ tự nào | [`trien-khai/00-lo-trinh-tong-the.md`](trien-khai/00-lo-trinh-tong-the.md) |
| Ranh giới Core ↔ Module | [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §6 |
| Core BE cần gì (để đối chiếu hai phía) | [`../be/01-core-components.md`](../be/01-core-components.md) |
| Mục lục khu kiến thức nền | [`../README.md`](../README.md) |
