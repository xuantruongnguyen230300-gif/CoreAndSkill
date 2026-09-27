---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0104 — Trên DB dev chung, người vận hành chạy bootstrap một lần, rồi dùng `admin` tạo tài khoản ứng dụng cho từng dev qua màn quản trị người dùng; không mã mới

> **Trạng thái:** Đã chấp nhận (2026-09-25) · Bổ sung bởi [ADR-0105](0105-thu-khu-quan-tri-he-thong-tren-db-cuc-bo.md) (2026-09-25)

## Bối cảnh

[`0099-luat-van-hanh-db-dev-chung.md`](0099-luat-van-hanh-db-dev-chung.md) nói người vận hành DB dev áp script lên database chung. ADR đó không nói dev **đăng nhập ứng dụng** bằng tài khoản nào. Kiến trúc sư nêu khoảng trống đó thành câu hỏi. Người dùng trả lời cùng câu hỏi về cách ghi tên người vận hành.

Trên database cục bộ, mỗi dev tự chạy lệnh bootstrap với mật khẩu từ `user-secrets` của máy mình ([`../database/script-runbook.md`](../database/script-runbook.md) §3.3, bước 5). Lệnh đó tạo đơn vị hệ thống kèm một tài khoản vận hành, và đơn vị nghiệp vụ đầu tiên kèm một tài khoản quản trị. Trên database chung, lệnh chỉ có tác dụng **một lần**: chạy lại thì bỏ qua đơn vị và tài khoản đã có, và không ghi đè mật khẩu ([`0023-dich-vu-tao-don-vi-dung-chung.md`](0023-dich-vu-tao-don-vi-dung-chung.md), mục *3. `core bootstrap` tạo gì*). Dev thứ hai chạy lệnh với mật khẩu của mình thì không có tài khoản nào mang mật khẩu đó.

Hai đường tạo tài khoản đã có, đọc tài liệu ngày 2026-09-25:

- **Tài khoản trong một đơn vị nghiệp vụ:** `POST /api/v1/core/users` ([`../contracts/users.md`](../contracts/users.md) §5), màn [`../Design/Screens/10-nguoi-dung.md`](../Design/Screens/10-nguoi-dung.md). Người gọi gõ một mật khẩu tạm. Tài khoản tạo ra mang `mustChangePassword = true`.
- **Tài khoản vận hành hệ thống:** không có đường thứ hai ngoài lệnh bootstrap. Không card nào trong [`../contracts/`](../contracts/) tạo thêm một tài khoản mang cờ vận hành hệ thống.

## Quyết định

Người dùng chốt ngày 2026-09-25:

1. **Người vận hành DB dev chạy `core bootstrap` một lần** trên database chung, với mật khẩu từ `user-secrets` của máy người vận hành.
2. **Người vận hành đăng nhập bằng `admin`, rồi tạo cho mỗi dev một tài khoản ứng dụng** trong đơn vị nghiệp vụ đầu tiên, qua màn quản trị người dùng. Dev đăng nhập bằng mật khẩu tạm và đổi mật khẩu ở lần đầu, theo luồng đã có. **Không có dòng mã mới, seed mới hay script mới.**
3. **Tài liệu chỉ ghi vai trò *người vận hành DB dev*.** Tên người để ngoài repo, vì `docs/` đi theo Core sang dự án khác. ADR-0099 đã theo đúng cách này.

Kiến trúc sư ghi phần đi kèm:

4. **Hai tài khoản do bootstrap tạo chỉ người vận hành dùng.** Dev không dùng chung `admin` hay tài khoản vận hành. Lý do: nhật ký kiểm toán ghi theo người thực hiện; một tài khoản dùng chung làm mọi dòng nhật ký trên DB chung mất nghĩa.
5. **Người rời nhóm: khoá tài khoản ứng dụng của người đó** qua cùng màn quản trị. Việc này đi kèm các bước ở ADR-0098 (đổi mật khẩu DB) và ADR-0099 (xoá tài khoản Postgres cá nhân).
6. **Luật S27** ở [`../DEBT.md`](../DEBT.md): mỗi dev đăng nhập DB chung bằng tài khoản ứng dụng của riêng mình. Không ép được bằng máy.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Cả nhóm dùng chung tài khoản `admin`

**Được:** không bước nào ngoài bootstrap.

**Mất:**

- Nhật ký kiểm toán không phân biệt người.
- Một người đổi mật khẩu `admin` thì cả nhóm mất đăng nhập.
- Người rời nhóm vẫn biết mật khẩu.

**Vì sao loại:** mất truy vết — cùng lý do ADR-0099 loại tài khoản Postgres dùng chung.

### Phương án B — Seed tài khoản dev bằng một script hay một lệnh runner mới

**Được:** dựng lại DB chung thì tài khoản tự có lại.

**Mất:**

- Mã mới trong Core cho một nhu cầu chỉ có ở dev.
- Danh sách người trong nhóm nằm trong repo, trái mục 3.
- [`0022-seed-dev-khong-co-duong-code-rieng.md`](0022-seed-dev-khong-co-duong-code-rieng.md) đã loại *"đường code chứa sẵn thông tin tài khoản"*.

**Vì sao loại:** trái hai quyết định đã có.

## Hệ quả

### Tích cực

- Không dòng mã nào đổi. Dùng đúng luồng mà người quản trị thật dùng, nên luồng đó được dùng thử hằng ngày.
- Mỗi dòng nhật ký kiểm toán trên DB chung mang tên một người.

### Tiêu cực

- **Người vận hành là điểm nghẽn** khi có dev mới, cũng như khi áp script (ADR-0099).
- **Không có đường tạo thêm tài khoản vận hành hệ thống.** Dev cần thử khu quản trị hệ thống thì hoặc mượn tài khoản vận hành từ người vận hành — trái mục 4 — hoặc dùng database cục bộ. Hôm nay chưa quyết đường nào.
- **Dựng lại DB chung thì phải tạo lại mọi tài khoản bằng tay.**
- **Dev thử luồng quản trị trong đơn vị cần quyền quản trị.** Người vận hành phải gán vai trò cho tài khoản của họ, và có thể nhiều dev cùng giữ quyền quản trị đơn vị.

### Rút lui nếu sai

Quay về tài khoản dùng chung, hoặc viết ADR mới cho một đường seed. Không dữ liệu nào phụ thuộc quyết định này ngoài chính các tài khoản dev trên DB chung.

### Dấu hiệu quyết định này bắt đầu sai

- Người vận hành thường xuyên bị gọi chỉ để tạo tài khoản.
- Dòng nhật ký trên DB chung mang tên `admin` cho thao tác của một dev.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Khoảng trống của ADR-0099: chưa ai nói dev đăng nhập bằng gì. Không có gì để đo |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án A, dùng chung `admin`, đơn giản hơn. Nó mất truy vết |
| 3 | Chi phí vận hành thêm | Một thao tác tạo tài khoản cho mỗi dev mới, một thao tác khoá cho mỗi người rời nhóm |
| 4 | Ai bảo trì | Người vận hành DB dev chung |
| 5 | Rút lui thế nào | Mục *Rút lui nếu sai* |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Không dòng mã nào đổi |

## Liên quan

- [`0099-luat-van-hanh-db-dev-chung.md`](0099-luat-van-hanh-db-dev-chung.md) — được ADR này bổ sung
- [`../contracts/users.md`](../contracts/users.md) §5 — tạo tài khoản, mật khẩu tạm
- [`../database/script-runbook.md`](../database/script-runbook.md) §3.3, §6
