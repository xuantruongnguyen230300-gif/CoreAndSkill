---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N2` — Đính kèm tệp: tải lên, lưu, tải về, xoá

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Quyền đọc tệp đi theo **bản ghi chủ**, còn ranh giới **đơn vị** đi theo bộ lọc chung của bảng `core.file` — hai lớp khác nhau, §5 và §6.

---

## 1. Ai bắt đầu, ở đâu

Người dùng đã đăng nhập, từ một màn nghiệp vụ có ô đính kèm.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Phiên hợp lệ, ngữ cảnh đơn vị đã có | Thiết lập ở luồng `D1` bước 4 |
| `purpose` của tệp đã khai | Mỗi `purpose` có giới hạn dung lượng và danh sách kiểu tệp riêng |
| Quyền trên **bản ghi nghiệp vụ** chứa tệp | Quyền xem tệp đi theo bản ghi, **không** theo bản thân tệp — tệp không có khoá quyền riêng ([`../contracts/files.md`](../contracts/files.md) §4) |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | FE | `POST /api/v1/core/files` kèm `purpose` | [`../contracts/files.md`](../contracts/files.md) §1 |
| 2 | BE | Kiểm dung lượng và kiểu tệp **theo `purpose` đó**, không theo một ngưỡng chung | cùng trên |
| 3 | BE | Ghi tệp ra nơi lưu trữ — **ngoài** thư mục máy chủ web phục vụ trực tiếp | [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §5 |
| 4 | BE | Ghi bản ghi tệp, trả về định danh | [`../contracts/files.md`](../contracts/files.md) §1 |
| 5 | FE | Gắn định danh đó vào bản ghi nghiệp vụ đang soạn | thuộc `spec/<feature>/` |
| 6 | Người dùng | `GET /api/v1/core/files/{id}` để tải về | [`../contracts/files.md`](../contracts/files.md) §2 |
| 7 | BE | Kiểm quyền **rồi mới** phát tệp — không phục vụ tĩnh | [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §5 |
| 8 | Người dùng | `DELETE /api/v1/core/files/{id}` | [`../contracts/files.md`](../contracts/files.md) §3 |

### Vì sao không phục vụ tĩnh

Tệp nằm trong thư mục web phục vụ trực tiếp thì **ai đoán được đường dẫn là tải được**, không qua lớp kiểm nào. Đường dẫn khó đoán **không phải** một lớp bảo vệ.

## 4. Hỏng ở đâu — và người dùng thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/files.md`](../contracts/files.md) §1–§3. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Tệp quá lớn cho `purpose` đó | `CORE.FILE.TOO_LARGE` | Nêu đúng giới hạn của `purpose` đang dùng, không nêu một con số chung |
| Kiểu tệp không nằm trong danh sách | `CORE.FILE.TYPE_NOT_ALLOWED` | |
| Không có tệp đó **hoặc** không có quyền đọc bản ghi chứa nó | `CORE.FILE.NOT_FOUND` | **Gộp hai ca là cố ý** — phân biệt được chúng là cho người gọi biết một định danh có tồn tại hay không |
| Lỗi dự kiến được (không tìm thấy, không đọc được) | trả `Result`, **không ném exception** | [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §3 |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị** — và đây là chỗ [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) **im lặng**.

Bản ghi tệp mang `tenant_id` như mọi bảng khác, nên bộ lọc đơn vị áp được. Nhưng file chủ của chủ đề lưu tệp nhắc chữ "đơn vị" đúng **0 lần**, và câu bảo vệ duy nhất nó đưa ra là *"phục vụ qua một endpoint **có kiểm quyền**"*.

**Quyền không phải đơn vị.** Tệp không có khoá quyền riêng — quyền đọc tệp là quyền đọc **bản ghi chủ** ([`../contracts/files.md`](../contracts/files.md) §4). Hai người ở hai đơn vị khác nhau đều có thể mang quyền đọc cùng một loại bản ghi một cách hợp lệ.

## 6. Câu chưa trả lời được

Bảng tệp là `core.file` ([`../database/schema-core.md`](../database/schema-core.md) §9.7): `tenant_id NOT NULL` theo §3.7, nên bước 7 tra tệp qua bộ lọc đơn vị chung như mọi bảng — tệp của đơn vị khác **không tồn tại** với người gọi, trả `CORE.FILE.NOT_FOUND`.

Hai câu cần chốt **ở đầu pha B4**, thuộc [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md):

1. Nơi lưu trữ vật lý có tách theo đơn vị không, hay mọi đơn vị chung một thư mục và chỉ tách bằng `storage_key`?
2. Một tệp đã tải lên nhưng **chưa gắn vào bản ghi nghiệp vụ nào** (`owner_id` rỗng) thì quyền của nó đi theo cái gì? Giữa bước 4 và bước 5 có một khoảng mà "quyền theo bản ghi chứa nó" chưa có nghĩa.
