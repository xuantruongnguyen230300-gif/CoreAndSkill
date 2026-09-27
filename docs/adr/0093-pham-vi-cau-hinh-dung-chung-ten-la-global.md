---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0093 — Phạm vi cấu hình dùng chung cho mọi đơn vị mang giá trị `global` trong cột `core.setting.scope`, không mang `system`

> **Trạng thái:** Đã chấp nhận (2026-09-25) · Bổ sung bởi [ADR-0095](0095-khoa-duy-nhat-cua-core-setting-dung-nulls-not-distinct.md) (2026-09-25)

## Bối cảnh

[`../database/schema-core.md`](../database/schema-core.md) §9.5 khai cột `core.setting.scope` nhận ba giá trị. Giá trị cho phạm vi rộng nhất, tức một giá trị dùng chung cho mọi đơn vị (`tenant_id` rỗng), từng là `system`. Thành phần cấu hình theo đơn vị **chưa thi công**: nó là thành phần Nhóm B, và dưới `src/BE` chưa có gì khớp `core.setting` (dò 2026-09-24, banner của [`../wiki-core/be/19-cau-hinh-theo-don-vi.md`](../wiki-core/be/19-cau-hinh-theo-don-vi.md)).

Vòng `core-reviewer` ngày 2026-09-25 nêu hai va chạm của giá trị đó:

1. **Luật S22** ([`../RULES.md`](../RULES.md) §6) cấm mã Core gõ tay chữ `"system"`, không phân biệt hoa thường, vì chữ đó là tên tác nhân hệ thống và chỉ có một nguồn là `SystemActor.UserName` ([ADR-0090](0090-ten-dang-nhap-system-la-ten-danh-rieng-chan-o-duong-tao.md)). Ngày thành phần cấu hình được thi công, hằng số cho giá trị `scope` này sẽ làm cổng S22 đỏ.
2. **Chữ "hệ thống" trong repo này đã có chủ.** Nó chỉ *đơn vị hệ thống* (`core.tenant.is_system`), *tài khoản vận hành hệ thống* (`is_system_operator`), và *tác nhân hệ thống* (`SystemActor`). Một dòng cấu hình `scope = system` lại **không** thuộc đơn vị hệ thống: nó có `tenant_id` rỗng và áp cho mọi đơn vị. Người đọc dễ hiểu nhầm nó là *"cấu hình của đơn vị hệ thống"*.

## Quyết định

Người dùng chốt ngày 2026-09-25: giá trị đó đổi thành **`global`**, ngay bây giờ, khi thành phần còn chưa có dòng code nào.

Kiến trúc sư chốt thêm phần văn xuôi đi cùng: tên tiếng Việt của phạm vi này là **"toàn cục"**, không còn là "hệ thống". Tên giá trị và tên gọi trong văn xuôi phải chỉ về cùng một thứ; giữ chữ "hệ thống" trong văn xuôi thì va chạm thứ 2 ở Bối cảnh vẫn còn nguyên.

Ba giá trị hợp lệ của cột nằm ở định nghĩa gốc: [`../database/schema-core.md`](../database/schema-core.md) §9.5. Thứ tự ưu tiên giữa các phạm vi: [`../wiki-core/be/19-cau-hinh-theo-don-vi.md`](../wiki-core/be/19-cau-hinh-theo-don-vi.md) §2.

## Phương án đã cân nhắc và vì sao loại

Kiến trúc sư ghi các phương án dưới đây ngày 2026-09-25, sau khi người dùng đã chốt.

### Phương án A — Giữ `system`, thêm một miễn trừ có tên vào cổng S22

**Được:** không sửa tài liệu nào.

**Mất:**

- Danh sách miễn trừ của S22 thêm một mục, cho một thứ không liên quan gì tới tác nhân hệ thống. Mỗi miễn trừ là một chỗ bộ dò phải nhận ra *"chữ này ở đây mang nghĩa khác"*. Bộ dò làm việc đó bằng tên tệp hoặc tên thành viên, và bị lừa được khi mã dời chỗ.
- Va chạm nghĩa với *đơn vị hệ thống* không mất đi.

**Vì sao loại:** trả chi phí bảo trì cổng mãi mãi, chỉ để giữ một cái tên đang gây nhầm.

### Phương án B — Bỏ cột `scope`, suy phạm vi từ hai cột `tenant_id` và `scope_id`

`tenant_id` rỗng là toàn cục, `scope_id` có giá trị là người dùng, còn lại là đơn vị.

**Được:** không có giá trị chữ nào để va chạm. Bảng bớt một cột có thể lệch với hai cột kia.

**Mất:** đây là thay đổi thiết kế bảng, vượt quá câu hỏi người dùng đã chốt. Nó cũng đổi khoá duy nhất của bảng.

**Vì sao loại:** ngoài phạm vi. Nếu cần xét lại thiết kế bảng, việc đó thuộc lượt thi công thành phần, kèm ADR riêng.

## Hệ quả

### Tích cực

- Ngày thành phần được thi công, cổng S22 không cần miễn trừ nào cho nó.
- Chữ "hệ thống" giữ đúng một nghĩa trong schema `core`: những gì thuộc về đơn vị hệ thống và người vận hành.
- Chi phí đổi bằng không về code và dữ liệu, vì chưa có cả hai.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **`global` là chữ tiếng Anh duy nhất trong ba tên gọi của phạm vi** | Hai giá trị kia là `tenant` và `user`, cũng tiếng Anh, nên cột vẫn nhất quán. Nhưng văn xuôi phải dịch `global` thành "toàn cục", và ai đọc văn xuôi cũ hay tài liệu ngoài sẽ gặp chữ "hệ thống" cho cùng thứ đó |
| **Không cổng nào giữ tên mới** | Chưa có code, nên không có gì để một cổng đối chiếu. Người thi công thành phần đọc §9.5 là lớp bảo vệ duy nhất |

### Rút lui nếu sai

Đổi ô giá trị ở §9.5 của `schema-core.md` về lại `system`, và đổi các câu văn xuôi đã đổi cùng lượt — tìm chữ "toàn cục" trong hai tệp đầu của mục *Liên quan*. Nếu thành phần đã được thi công, rút lui cần thêm một script đổi giá trị trong các dòng đã có của `core.setting`, và một miễn trừ S22. Khi đó cần ADR mới.

### Dấu hiệu quyết định này bắt đầu sai

- Có thêm một phạm vi mới rộng hơn đơn vị nhưng hẹp hơn toàn cục, như một nhóm đơn vị. Lúc đó `global` vẫn đúng nghĩa, nhưng bảng phạm vi cần xét lại toàn bộ.
- Có người đề xuất đặt cấu hình toàn cục vào dòng của đơn vị hệ thống. Đó chính là sự nhầm lẫn mà quyết định này tránh.

## Liên quan

- [`../database/schema-core.md`](../database/schema-core.md) §1.3, §9.5
- [`../wiki-core/be/19-cau-hinh-theo-don-vi.md`](../wiki-core/be/19-cau-hinh-theo-don-vi.md) §2, §3
- [`0090-ten-dang-nhap-system-la-ten-danh-rieng-chan-o-duong-tao.md`](0090-ten-dang-nhap-system-la-ten-danh-rieng-chan-o-duong-tao.md) · [`../RULES.md`](../RULES.md) S22
