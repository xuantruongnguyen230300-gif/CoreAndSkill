---
kind: tham-chieu
scope: core
verified: khong-ap-dung
---

# Tra cứu file / class — Backend

> **File này không giữ bảng file ↔ class, có chủ đích.** Tên class và đường dẫn file đổi mỗi lần có người thêm, đổi tên hay dời một file; một bảng chép tay từ `src/` là đúng loại danh sách mà [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6 cấm, vì nó mục ruỗng mà không cổng nào báo. Tra bằng lệnh ở dưới — kết quả lệnh là nguồn, không phải file này.

## Vì sao ba khoá phân loại của file này khác các file khác trong khu

| Khoá | Giá trị | Lý do |
| --- | --- | --- |
| `kind` | `tham-chieu` | Đây là **cách tra**, không phải luật. Code không tuân theo nó; nó chỉ chỉ đường |
| `scope` | `core` | Nó đi theo Core sang dự án khác — lệnh dùng lại nguyên ở repo nào có `src/BE` |
| `verified` | `khong-ap-dung` | File không khẳng định điều gì về nội dung `src/`, nên không có gì để đối chiếu. Đây **không** phải một cách né việc: cổng chỉ chấp nhận giá trị này khi file mang `kind: tham-chieu` hoặc `kind: lich-su`, và file này thoả điều kiện đó một cách thật sự. Ngày file này chép vào một tên lấy từ `src/`, giá trị này thành tự miễn trừ sai |

## Tra bằng lệnh — chạy từ gốc repo

```bash
# Kiểu <Ten> (class, record, interface, struct, enum) khai ở file nào
grep -rlE --include='*.cs' --exclude-dir=obj --exclude-dir=bin '(class|record|interface|struct|enum) +<Ten>\b' src/BE

# Core lộ ra những interface công khai nào, ở file nào
grep -rnE --include='*.cs' --exclude-dir=obj --exclude-dir=bin 'public +([a-z]+ +)*interface +I' src/BE/Core | sort
```

Có những project nào: danh sách gốc ở [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §2, lệnh đếm ở §8 cùng file.

## Tiêu chí đọc kết quả tra một kiểu

| Lệnh trả về | Nghĩa | Làm gì |
| --- | --- | --- |
| **Đúng một file** | PASS — đó là chỗ khai | Mở file ra đọc trước khi trích; tên file không bảo đảm nội dung |
| **Không file nào** | Kiểu chưa có trong `src/BE`, hoặc gõ sai tên | Đừng kết luận "thiếu" ngay: mở file luật của chủ đề xem kiểu đó là thứ đã thi công hay mới là đích |
| **Nhiều file** | `partial`, hoặc hai project cùng khai một tên | Mở từng file; đừng chọn đại |

## Trích kết quả sang tài liệu khác

Neo theo [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §9: đường dẫn + một chuỗi tìm được trong tệp, không số dòng. Không chép số đếm ("có N interface") — cần con số thì chạy lại lệnh. Ở dự án tiền nhiệm, một bảng tra cùng loại từng liệt kê hàng loạt tên **của một dự án khác**, và không tên nào tồn tại trong repo đó.

## Lệnh trả lời cái đang có — cái nên có thì đọc

| Câu hỏi | File |
| --- | --- |
| Project nào chứa gì | [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §2 |
| Core cần có thành phần nào | [`01-core-components.md`](01-core-components.md) |
| Một thành phần được viết ra sao | [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) |
