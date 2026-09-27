---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0105 — Dev cần thử khu quản trị hệ thống thì dựng database cục bộ và dùng tài khoản vận hành của chính mình; không thêm lệnh tạo tài khoản vận hành, không dùng chung tài khoản vận hành trên DB dev chung

> **Trạng thái:** Đã chấp nhận (2026-09-25)

## Bối cảnh

[`0104-tai-khoan-ung-dung-cua-dev-tren-db-chung-tao-qua-man-quan-tri.md`](0104-tai-khoan-ung-dung-cua-dev-tren-db-chung-tao-qua-man-quan-tri.md) cho mỗi dev một tài khoản ứng dụng trong đơn vị nghiệp vụ trên DB dev chung. Mục *Tiêu cực* của ADR đó để ngỏ một chỗ.

Lệnh bootstrap tạo **đúng một** tài khoản vận hành hệ thống, và không đường nào khác tạo thêm. Dev cần thử khu quản trị hệ thống thì có hai lối, cả hai chưa được quyết:

- mượn tài khoản vận hành của người vận hành — trái mục 4 của ADR-0104;
- dùng database cục bộ.

## Quyết định

Người dùng chốt ngày 2026-09-25:

1. **Dev cần thử khu quản trị hệ thống thì dựng database cục bộ** theo [`../database/script-runbook.md`](../database/script-runbook.md) §6 — áp script của schema `core`, rồi chạy lệnh bootstrap — và đăng nhập bằng tài khoản vận hành mà chính lệnh đó tạo, với mật khẩu từ `user-secrets` của máy mình.
2. **Không thêm lệnh hay endpoint tạo tài khoản vận hành.**
3. **Không dùng chung tài khoản vận hành trên DB dev chung.** Tài khoản đó chỉ người vận hành DB dev dùng — mục 4 của ADR-0104 giữ nguyên.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Thêm lệnh runner tạo thêm tài khoản vận hành

**Được:** mỗi dev có tài khoản vận hành riêng trên DB chung, truy vết theo người.

**Mất:**

- Một đường mới tạo tài khoản mang cờ đặc quyền cao nhất. Đó là bề mặt tấn công mới, cần luật riêng, nhật ký riêng, test riêng.
- Nhu cầu chỉ có ở môi trường dev.

**Vì sao loại:** người dùng chốt không làm.

### Phương án B — Người vận hành cho mượn tài khoản vận hành trên DB chung

**Được:** không bước nào thêm.

**Mất:** mọi dòng nhật ký của khu hệ thống trên DB chung mang một tên; ai từng mượn thì còn biết mật khẩu.

**Vì sao loại:** trái mục 4 của ADR-0104, cùng lý do mất truy vết.

## Hệ quả

### Tích cực

- Không dòng mã nào đổi. Không đường mới nào tạo tài khoản đặc quyền.
- Người thử khu hệ thống tự do dựng lại dữ liệu của mình mà không ảnh hưởng người khác.

### Tiêu cực

- **Dev phải giữ được một Postgres cục bộ.** Cùng điều kiện với nhánh mang script mới và với việc gỡ lỗi việc nền ([`0099-luat-van-hanh-db-dev-chung.md`](0099-luat-van-hanh-db-dev-chung.md)).
- **Khu hệ thống không được thử trên dữ liệu chung.** Lỗi chỉ lộ ra khi có nhiều đơn vị do nhiều người tạo thì khó thấy trên database cục bộ.

### Rút lui nếu sai

Viết ADR mới cho phương án A. Không dữ liệu nào phụ thuộc quyết định này.

### Dấu hiệu quyết định này bắt đầu sai

- Tài khoản vận hành trên DB chung bị dùng bởi người khác người vận hành.
- Nhiều dev không dựng được database cục bộ.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Khoảng trống ở mục *Tiêu cực* của ADR-0104. Không có gì để đo |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án B đơn giản hơn. Nó mất truy vết |
| 3 | Chi phí vận hành thêm | Mỗi dev thử khu hệ thống giữ một database cục bộ |
| 4 | Ai bảo trì | Từng dev, với database của mình |
| 5 | Rút lui thế nào | ADR mới; không dữ liệu phải chuyển |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Không dòng mã nào đổi |

## Liên quan

- [`0104-tai-khoan-ung-dung-cua-dev-tren-db-chung-tao-qua-man-quan-tri.md`](0104-tai-khoan-ung-dung-cua-dev-tren-db-chung-tao-qua-man-quan-tri.md) — được ADR này bổ sung
- [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) — khu quản trị hệ thống
- [`../database/script-runbook.md`](../database/script-runbook.md) §3.3, §6
