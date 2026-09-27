---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0095 — Khoá duy nhất của `core.setting` khai `NULLS NOT DISTINCT`, và Core đòi PostgreSQL 15 trở lên

> **Trạng thái:** Đã chấp nhận (2026-09-25)

## Bối cảnh

[`../database/schema-core.md`](../database/schema-core.md) §9.5 khai khoá duy nhất của `core.setting` trên `(tenant_id, scope, scope_id, key)`, kèm mệnh đề xoá mềm theo §3.3 cùng tệp. Hai trong bốn cột được phép rỗng:

| Phạm vi | `tenant_id` | `scope_id` |
| --- | --- | --- |
| `global` | rỗng | rỗng |
| `tenant` | có | rỗng |
| `user` | có | có |

Mặc định, unique index của PostgreSQL coi hai giá trị `NULL` là **khác nhau**. Hai dòng `global` cùng `key` vì thế cùng lọt qua. Hai dòng `tenant` cùng đơn vị, cùng `key` cũng lọt. Khoá như đang khai chỉ chặn trùng ở **một** trong ba phạm vi. Ở hai phạm vi kia, thứ tự ưu tiên của [`../wiki-core/be/19-cau-hinh-theo-don-vi.md`](../wiki-core/be/19-cau-hinh-theo-don-vi.md) §2 đứng trước hai giá trị cho cùng một ô, và không có luật nào nói giá trị nào thắng.

Bảng **chưa thi công**. Kiến trúc sư dò ngày 2026-09-25: `src/BE` và `database/scripts/core` không có gì khớp `core.setting`. `docker-compose.yml` dùng image `postgres:18`. Chưa tài liệu nào trong `docs/` khai phiên bản PostgreSQL tối thiểu.

[ADR-0093](0093-pham-vi-cau-hinh-dung-chung-ten-la-global.md), cùng ngày, đổi tên phạm vi rộng nhất thành `global`. Phương án B của ADR đó đã ghi rằng việc đổi khoá duy nhất nằm ngoài câu hỏi nó trả lời.

## Quyết định

Người dùng chốt ngày 2026-09-25: unique index của `core.setting` trên `(tenant_id, scope, scope_id, key)` khai **`NULLS NOT DISTINCT`**, và giữ mệnh đề `WHERE is_deleted = false` theo §3.3.

Kiến trúc sư ghi hệ quả đi kèm. `NULLS NOT DISTINCT` có từ PostgreSQL 15, nên **Core đòi PostgreSQL 15 trở lên**. Sàn phiên bản được khai ở `schema-core.md` §1.

Người thi công phải đọc lại script sinh từ model để thấy mệnh đề `NULLS NOT DISTINCT` thật sự có trong câu `CREATE UNIQUE INDEX`. Phía EF, API dự kiến của Npgsql là `.AreNullsDistinct(false)` trên `HasIndex`. Người thi công xác nhận API đó khi dựng bảng.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Ba unique index một phần, mỗi phạm vi một cái

Ví dụ: `(key) WHERE scope = 'global'`, `(tenant_id, key) WHERE scope = 'tenant'` và `(tenant_id, scope_id, key) WHERE scope = 'user'`, cái nào cũng kèm `AND is_deleted = false`.

**Được:** chạy trên mọi phiên bản PostgreSQL. Không index nào chứa cột rỗng.

**Mất:** ba index cho một bảng. Mỗi câu `ON CONFLICT` phải lặp nguyên văn vị từ của đúng phạm vi mình ghi; sai là `42P10` (§3.3, hệ quả 2). Model EF cần ba `HasIndex` có bộ lọc.

**Vì sao loại:** trả độ phức tạp gấp ba để chạy được trên PostgreSQL 14 trở xuống. Không dự án nào đang cần điều đó: image dùng bản 18.

### Phương án B — Index biểu thức, `COALESCE` cột rỗng về một UUID gác

**Được:** một index, chạy trên mọi phiên bản.

**Mất:** EF không mô hình hoá gọn index biểu thức, nên phải viết SQL tay trong migration. `ON CONFLICT` phải lặp nguyên văn biểu thức. UUID gác là một giá trị không trỏ vào đâu nhưng có hình dạng giống một id thật.

**Vì sao loại:** đổi một mệnh đề chuẩn lấy SQL tay và một giá trị ma.

### Phương án C — Không để cột rỗng: dòng `global` gán vào đơn vị hệ thống

**Vì sao loại:** ADR-0093 đã nêu đúng ý này làm dấu hiệu sai. Cấu hình toàn cục không thuộc đơn vị hệ thống.

### Phương án D — Giữ nguyên, chặn trùng ở tầng ứng dụng

**Được:** không đổi DDL.

**Mất:** kiểm-rồi-ghi ở ứng dụng để lọt hai lượt ghi đồng thời.

**Vì sao loại:** [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §7 ưu tiên ràng buộc duy nhất của DB khi làm được. Ở đây làm được.

## Hệ quả

### Tích cực

- Một index chặn trùng đúng ở cả ba phạm vi.
- `ON CONFLICT` có một dạng duy nhất cho cả bảng.
- Chi phí đổi bằng không, vì chưa có code và chưa có dữ liệu.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Core đòi PostgreSQL 15 trở lên** | Dự án mang Core sang mà đang chạy bản 14 trở xuống thì không áp được script. Họ phải nâng PostgreSQL trước |
| **Sàn 15 là do quyết định này đặt, chưa kiểm cho cả repo** | Chưa ai rà xem script khác, hay bản Npgsql đang dùng, có đòi phiên bản cao hơn không. Con số 15 là sàn tối thiểu, không phải sàn đã kiểm đủ |
| **Mệnh đề ít người biết** | Người đọc DDL theo thói quen có thể coi `NULLS NOT DISTINCT` là thừa và gỡ đi. Không cổng nào canh việc đó cho tới khi có integration test chạy trên Postgres thật (luật T2): hai dòng `global` cùng `key` và chưa xoá phải bị `23505` |

### Rút lui nếu sai

Khi bảng chưa thi công: đổi thiết kế §9.5 sang phương án A, gỡ dòng sàn phiên bản ở §1, viết ADR mới.

Khi bảng đã có dữ liệu: thêm một script mới. Script tạo ba index một phần của phương án A, rồi xoá index cũ. Dữ liệu không phải sửa: mọi tập dòng thoả `NULLS NOT DISTINCT` đều thoả ba index kia, vì ràng buộc cũ chặt hơn. Code phải sửa mọi câu `ON CONFLICT` trên bảng để khớp vị từ của từng phạm vi.

### Dấu hiệu quyết định này bắt đầu sai

- Một dự án bắt buộc chạy trên dịch vụ PostgreSQL chỉ có bản 14 trở xuống.
- Khoá cần thêm một cột được phép rỗng, mà hai giá trị rỗng của cột đó phải được coi là khác nhau.

## Liên quan

- [`../database/schema-core.md`](../database/schema-core.md) §1, §3.3, §9.5
- [`0093-pham-vi-cau-hinh-dung-chung-ten-la-global.md`](0093-pham-vi-cau-hinh-dung-chung-ten-la-global.md): bổ sung cho ADR đó
- [`../wiki-core/be/19-cau-hinh-theo-don-vi.md`](../wiki-core/be/19-cau-hinh-theo-don-vi.md) §2
