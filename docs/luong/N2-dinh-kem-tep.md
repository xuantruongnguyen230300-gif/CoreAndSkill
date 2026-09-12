---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N2` — Đính kèm tệp: tải lên, lưu, tải về, xoá

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng này có một **lỗ hở đã xác minh**, ghi ở §6. Đọc mục đó trước khi thi công.

---

## 1. Ai bắt đầu, ở đâu

Người dùng đã đăng nhập, từ một màn nghiệp vụ có ô đính kèm.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Phiên hợp lệ, ngữ cảnh đơn vị đã có | Thiết lập ở luồng `D1` bước 4 |
| `purpose` của tệp đã khai | Mỗi `purpose` có giới hạn dung lượng và danh sách kiểu tệp riêng |
| Quyền trên **bản ghi nghiệp vụ** chứa tệp | Quyền xem tệp đi theo bản ghi, **không** theo bản thân tệp |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | FE | `POST /files` kèm `purpose` | [`../contracts/files.md`](../contracts/files.md) §1 |
| 2 | BE | Kiểm dung lượng và kiểu tệp **theo `purpose` đó**, không theo một ngưỡng chung | cùng trên |
| 3 | BE | Ghi tệp ra nơi lưu trữ — **ngoài** thư mục máy chủ web phục vụ trực tiếp | [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §5 |
| 4 | BE | Ghi bản ghi tệp, trả về định danh | [`../contracts/files.md`](../contracts/files.md) §1 |
| 5 | FE | Gắn định danh đó vào bản ghi nghiệp vụ đang soạn | thuộc `spec/<feature>/` |
| 6 | Người dùng | `GET /files/{id}` để tải về | [`../contracts/files.md`](../contracts/files.md) §2 |
| 7 | BE | Kiểm quyền **rồi mới** phát tệp — không phục vụ tĩnh | [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §5 |
| 8 | Người dùng | `DELETE /files/{id}` | [`../contracts/files.md`](../contracts/files.md) §3 |

### Vì sao không phục vụ tĩnh

Tệp nằm trong thư mục web phục vụ trực tiếp thì **ai đoán được đường dẫn là tải được**, không qua lớp kiểm nào. Đường dẫn khó đoán **không phải** một lớp bảo vệ.

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Tệp quá lớn cho `purpose` đó | `CORE.FILE.TOO_LARGE` (400) | Nêu đúng giới hạn của `purpose` đang dùng, không nêu một con số chung |
| Kiểu tệp không nằm trong danh sách | `CORE.FILE.TYPE_NOT_ALLOWED` (400) | |
| Không có tệp đó **hoặc** không có quyền đọc bản ghi chứa nó | `CORE.FILE.NOT_FOUND` (404) | **Gộp hai ca là cố ý** — phân biệt được chúng là cho người gọi biết một định danh có tồn tại hay không |
| Lỗi dự kiến được (không tìm thấy, không đọc được) | trả `Result`, **không ném exception** | [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §3 |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị** — và đây là chỗ [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) **im lặng**.

Bản ghi tệp mang `tenant_id` như mọi bảng khác, nên bộ lọc đơn vị áp được. Nhưng file chủ của chủ đề lưu tệp nhắc chữ "đơn vị" đúng **0 lần**, và câu bảo vệ duy nhất nó đưa ra là *"phục vụ qua một endpoint **có kiểm quyền**"*.

**Quyền không phải đơn vị.** Hai người ở hai đơn vị khác nhau đều có thể mang quyền `core.file.read` một cách hợp lệ.

## 6. Câu chưa trả lời được

> 🛑 **Lỗ hở đã xác minh: không file nào nói bước 7 phải kiểm ĐƠN VỊ, ngoài việc kiểm QUYỀN.**

Ca hỏng cụ thể: người dùng của đơn vị A, có quyền `core.file.read` hợp lệ, gọi `GET /files/{id}` với định danh của một tệp thuộc đơn vị B. Nếu bước 7 chỉ hỏi *"người này có quyền đọc tệp không"* mà không hỏi *"tệp này có thuộc đơn vị của người này không"*, tệp được phát.

Ca này **không sinh lỗi, không sinh exception**, và nó đúng nguyên văn thứ [`../RULES.md`](../RULES.md) §9 mô tả là rủi ro nghiêm trọng nhất của toàn hệ: *"truy vấn chạy bình thường, chỉ trả về nhiều hơn đáng ra được thấy"*.

Ba câu cần chốt, và cả ba thuộc [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md):

1. Bước 7 kiểm đơn vị bằng bộ lọc chung của interceptor, hay bằng một phép kiểm tường minh?
2. Nơi lưu trữ vật lý có tách theo đơn vị không, hay mọi đơn vị chung một thư mục và chỉ tách bằng bản ghi?
3. Một tệp đã tải lên nhưng **chưa gắn vào bản ghi nghiệp vụ nào** thì quyền của nó đi theo cái gì? Giữa bước 4 và bước 5 có một khoảng mà "quyền theo bản ghi chứa nó" chưa có nghĩa.
