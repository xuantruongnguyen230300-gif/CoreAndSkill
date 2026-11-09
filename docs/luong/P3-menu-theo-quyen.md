---
kind: luat
scope: core
verified: 2026-10-06
---

# `P3` — Menu hiện ra theo quyền của người đang đăng nhập

✅ **CÓ THẬT** — đối chiếu trọn tệp với `src/` ngày 2026-10-06, chỉ bằng đọc mã, không chạy ứng dụng. Neo từng bước ở mục *Đối chiếu với `src/`* cuối tệp.

> Luồng này là chỗ **giao diện và bảo mật dễ bị nhầm làm một**. Menu ẩn một mục **không** phải một lớp bảo vệ.

---

## 1. Ai bắt đầu, ở đâu

FE, khi dựng khung ứng dụng — tức sau đăng nhập (luồng `D1`), và nếu tài khoản mang cờ buộc đổi mật khẩu thì sau khi đổi xong (luồng `D2`). Không sớm hơn: `GET /api/v1/core/meta/menu` không thuộc bốn cửa mở của `D2`, nên gọi khi cờ còn bật chỉ nhận `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` ([`D2-doi-mat-khau-lan-dau.md`](D2-doi-mat-khau-lan-dau.md) §3 *Bốn cửa mở trong lúc cờ còn bật*).

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Phiên hợp lệ | |
| Bản ghi menu của đơn vị đã seed | Luồng `V1` bước 5 hoặc `V2` bước 4 — menu lấy từ các nguồn seed đã đăng ký |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | FE | `GET /api/v1/core/meta/menu` | [`../contracts/meta-menu.md`](../contracts/meta-menu.md) §1 |
| 2 | BE | Lấy cây menu **của đơn vị này** | [`../database/schema-core.md`](../database/schema-core.md) §6.3 |
| 3 | BE | Lọc từng mục theo **ba bước** của thứ tự quyết định, rồi kéo mục cha vào khi có con hiện | cùng trên · [`../contracts/meta-menu.md`](../contracts/meta-menu.md) §1.4, §1.5 |
| 4 | FE | Dựng thanh điều hướng từ cây đã lọc | [`../Design/Components/Sidebar.md`](../Design/Components/Sidebar.md) |

### Mối nối cần nhớ ở bước 3

Bước cuối của thứ tự quyết định cho **mọi** người đã đăng nhập thấy một mục không khai quyền và không gán vai trò nào. Mục như vậy không bao giờ bị lọc, kể cả với người chưa có vai trò nào — đó là lý do triệu chứng ở §4 là *"chỉ còn vài mục"*, không phải *"menu rỗng"*.

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Biểu hiện |
| --- | --- |
| Mục menu trỏ tới màn mà người dùng không có quyền | Bấm vào nhận `CORE.AUTH.FORBIDDEN` ([`../contracts/auth.md`](../contracts/auth.md) §11). Khó chịu nhưng **không phải lỗ hổng** |
| Màn được bảo vệ **chỉ bằng** việc ẩn mục menu | 🛑 **Là lỗ hổng.** Người dùng gõ thẳng đường dẫn là vào được. Menu là gợi ý điều hướng, không phải hàng rào — hàng rào nằm ở kiểm quyền phía server |
| Người dùng không có quyền nào | Với menu Core seed, chỉ thấy **Trang chủ**. BE trả cả nhóm **Quản trị** — hai mục đều không khai quyền, hiện cho mọi người (mối nối ở §3) — nhưng không kèm mục con nào, và thanh điều hướng ẩn nhóm không còn mục con ([`../Design/Components/Sidebar.md`](../Design/Components/Sidebar.md)). Không lỗi nào giải thích vì sao thiếu phần còn lại. Thường là vai trò rỗng quyền (`P1` bước 5) hoặc chưa gán vai trò (`P2`) |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Bản ghi menu mang `tenant_id` — mỗi đơn vị chỉnh menu của mình mà không đụng đơn vị khác.

Danh mục quyền thì **dùng chung toàn hệ**, nên mục menu của đơn vị A trỏ tới một khoá quyền cũng tồn tại ở đơn vị B. Đó là đúng: khoá là hợp đồng với code, còn việc ai có khoá đó mới là chuyện của từng đơn vị.

## 6. Câu chưa trả lời được

- **Chưa có endpoint quản trị menu.** [`../contracts/meta-menu.md`](../contracts/meta-menu.md) §2 khai rõ điều này. Nghĩa là hôm nay menu chỉ đổi được bằng cách seed hoặc sửa database — và mục "Sửa cấu hình menu" trong danh mục quyền chưa có đường nào dùng tới.

Menu của module lắp thêm vào bản cài đã có đơn vị: lệnh `core seed-tenant-defaults` ([`../contracts/meta-menu.md`](../contracts/meta-menu.md) §2); mục trỏ tới module chưa lắp bị lọc lúc chạy (cùng file, §1.6).

> Câu *"người dùng mang cờ `has_permission_bypass` thấy menu gì"* **đã có lời đáp**: cờ đó chỉ làm bộ kiểm quyền trả lời "có" cho mọi câu hỏi quyền ([`../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)), nên nó chỉ tác động lên bước 1 của thứ tự quyết định ở [`../database/schema-core.md`](../database/schema-core.md) §6.3. Hai bước còn lại không đọc cờ.

---

## Đối chiếu với `src/`

Neo của `P3`, đối chiếu 2026-10-06. Đường dẫn BE tính từ `src/BE/Core/`, FE từ `src/FE/src/app/`.

| Bước, ca | Neo bằng chuỗi |
| --- | --- |
| Bước 1 — endpoint mức chỉ cần đăng nhập, ngoài bốn cửa của `D2` | `CoreAndSkill.Core.Web/Controllers/MetaController.cs`: `[HttpGet("menu")]`, `[AuthenticatedOnly(`; bốn cửa ở `CoreAndSkill.Core.Web/Middleware/PasswordChangeRequiredMiddleware.cs` không có đường menu |
| Bước 2–3 — cây của đơn vị, ba bước, kéo mục cha, lọc module chưa lắp | `CoreAndSkill.Core.Infrastructure/Menu/MenuQueryService.cs`: `.Where(m => m.ModuleKey == null \|\| installedModuleKeys.Contains(m.ModuleKey))`, `// Bước 1 — thắng tuyệt đối`, `shown = gatedRoles.Overlaps(userRoleIdSet);`, `shown = true; // mọi người dùng đã đăng nhập`, `shownIds.Add(parentId);` |
| Menu Core seed — Trang chủ và nhóm Quản trị không khai quyền | `CoreAndSkill.Core.Infrastructure/Tenants/CoreTenantSeedSource.cs`: `new("trang-chu", "menu.trang-chu", "pi-home", "/trang-chu", null, 10, null, null),`, `new("quan-tri", "menu.quan-tri", "pi-cog", null, null, 90, null, null),` |
| §1 — FE nạp menu khi khung dựng, sau guard | `platform/shell/shell.component.ts`: `this.menu.lamMoi(id);` trong constructor; khung sau `app.routes.ts`: `canActivate: [authGuard, mustChangePasswordGuard]` |
| Bước 4 — thanh điều hướng ẩn nhóm không còn mục con | `core/menu/menu.mapper.ts`: `.filter((nut) => nut.route !== null \|\| nut.children.length > 0);` |
