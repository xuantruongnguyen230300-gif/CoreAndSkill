---
kind: tham-chieu
scope: core
verified: khong-ap-dung
---

# Tra cứu file / class — Backend

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**
>
> **Bảng dưới đây RỖNG có chủ đích.** Repo đang ở giai đoạn 1: chưa có `src/`, nên chưa có file nào và chưa có class nào để tra. Điền vào đây bất cứ tên nào lúc này là bịa hiện trạng — đúng khuôn sai mà [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §4 cấm.

## Vì sao ba khoá phân loại của file này khác các file khác trong khu

| Khoá | Giá trị | Lý do |
| --- | --- | --- |
| `kind` | `tham-chieu` | Đây là **bảng tra**, không phải luật. Code không tuân theo nó; nó chỉ chỉ đường |
| `scope` | `core` | Nó đi theo Core sang dự án khác — nhưng nội dung được dựng lại ở mỗi repo |
| `verified` | `khong-ap-dung` | Không có `src/` để đối chiếu. Đây **không** phải một cách né việc: cổng chỉ chấp nhận giá trị này khi file mang `kind: tham-chieu` hoặc `kind: lich-su`, và file này thoả điều kiện đó một cách thật sự |

## Bảng tra — điền ở giai đoạn 2

| Thành phần | Project | Đường dẫn | Vai |
| --- | --- | --- | --- |
| *(chưa có `src/`)* | | | |

## Điền bảng này thế nào khi `src/` tồn tại

Không chép tay. Dựng bằng lệnh, rồi mới biên tập cột *Vai*:

```bash
find src/BE -name '*.csproj' -not -path '*/obj/*' -not -path '*/bin/*' | sort
grep -rn 'public interface I' src/BE --include='*.cs' | sort
grep -rn 'public .*class ' src/BE --include='*.cs' -l | sort
```

Ba lệnh trên trả lời ba câu hỏi khác nhau: có những project nào, Core lộ ra những interface nào, và class nằm ở file nào.

## Ba luật khi điền

| Luật | Vì sao |
| --- | --- |
| **Mỗi tên phải được xác minh bằng cách mở chính file nguồn**, không suy từ tên file | Ở dự án tiền nhiệm, một bản của bảng này từng liệt kê hàng loạt tên **của một dự án khác**, và không tên nào tồn tại trong repo đó |
| **Không chép số đếm được** — không viết "có N interface" | [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6. Dùng lệnh ở trên |
| **Đổi `verified` sang ngày đối chiếu** ngay khi bảng có nội dung | Giữ `khong-ap-dung` cho một bảng đã có nội dung là tự miễn trừ sai |

## Trong lúc chờ

Cần biết một thành phần **nên** tồn tại và **nên** nằm ở đâu, đọc:

| Câu hỏi | File |
| --- | --- |
| Project nào chứa gì | [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §2 |
| Core cần có thành phần nào | [`01-core-components.md`](01-core-components.md) |
| Một thành phần được viết ra sao | [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) |
