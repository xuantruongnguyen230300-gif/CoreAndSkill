---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0051 — `core.permission.write` là khoá gốc của một đơn vị: ghi ma trận không đối chiếu tập quyền của người gọi

> **Trạng thái:** Đã chấp nhận (2026-09-22) · Bổ sung bởi ADR-0052 (2026-09-22)
>
> Câu còn mở về truy vết việc tự gắn khoá đã có lời đáp: [`0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md`](0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md).

## Bối cảnh

Một lượt `core-reviewer` BE báo rằng `PUT /api/v1/core/permissions/matrix` không đối chiếu tập quyền của người gọi. Một người chỉ giữ `core.permission.read` và `core.permission.write` có thể gắn mọi khoá vào vai trò của chính mình, và từ request kế tiếp là có toàn quyền trong đơn vị. Luật 1 ở [`../contracts/users.md`](../contracts/users.md) §2 chỉ chặn leo thang ở đường **gán vai trò cho người dùng**.

Tài liệu đã chốt coi khoá này là khoá quản trị gốc ở ba chỗ, nhưng chưa chỗ nào viết thẳng ra:

- Vai trò `is_system` không bao giờ mất khoá này, và bất biến đó được chọn **thay cho** luật "không hạ quản trị cuối cùng" vì nó giữ lại **đường sửa** ([`../contracts/users.md`](../contracts/users.md) §2 mục *Đã cân nhắc và LOẠI*, [`../database/schema-core.md`](../database/schema-core.md) §5.3).
- Từ bỏ cờ `has_permission_bypass` chỉ được phép khi đơn vị còn một tài khoản khác giữ khoá này qua vai trò ([`../contracts/profile.md`](../contracts/profile.md) §3).
- Khôi phục quản trị đơn vị nhận người giữ vai trò hệ thống vì *"cửa quản trị nằm ở vai trò"* ([`../contracts/tenants.md`](../contracts/tenants.md) §4 "Ghi chú").

Cả ba chỉ đúng nếu người giữ khoá này **dựng lại được bất kỳ ô nào** của ma trận. Câu hỏi là: đó là ý đồ, hay là một lỗ chưa ai thấy?

## Quyết định

Kiến trúc sư chốt: `core.permission.write` **tương đương toàn quyền trong đơn vị**. `PUT /api/v1/core/permissions/matrix` **không** đối chiếu tập quyền của người gọi. Card [`../contracts/permissions.md`](../contracts/permissions.md) §6 khai thẳng điều này. Code hiện tại đúng với quyết định; không có việc thi công nào kèm theo.

## Phương án đã cân nhắc và vì sao loại

### Phương án B — Người gọi chỉ được cấp hoặc thu khoá mình đang giữ (cùng khuôn luật 1)

**Được:** đóng đường leo thang qua ma trận. Tách được một vai trò "quản lý phân quyền hạng hai" chỉ phân phát được những khoá nó có. Nhất quán với cách luật 1 và luật 5 ([`0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)) áp nguyên tắc không leo thang.

**Vì sao loại:** nó phá bất biến "đường sửa". Bất biến ở `schema-core.md` §5.3 chỉ giữ **một** khoá trên vai trò hệ thống. Theo B, người giữ vai trò đó chỉ dựng lại được những khoá họ còn giữ. Một khoá bị gỡ khỏi mọi vai trò, kể cả do người giữ nó tự gỡ ở chính vai trò mình, sẽ thành **khoá mồ côi**: không ai cấp lại được qua API. Đơn vị đã từ bỏ cờ bypass theo khuyến nghị vận hành thì chỉ còn đường `UPDATE` bằng SQL tay. Muốn chặn khoá mồ côi thì phải đếm "còn ai giữ khoá này không" trên mỗi lần ghi. Đó đúng là luật "người cuối cùng" mà `users.md` §2 đã loại, vì nó đắt và vẫn thua khi hai request chạy đồng thời.

### Phương án C — B, cộng ngoại lệ cho người mang vai trò `is_system`

**Được:** giữ được đường sửa cho quản trị gốc, vẫn chặn leo thang cho người giữ `permission.write` qua vai trò thường.

**Vì sao loại:** nó biến `is_system` thành **điều kiện cho qua một endpoint**, trong khi `schema-core.md` §4.2 cấm đúng việc đó (`is_system` chỉ bảo vệ vòng đời của dòng, luật S2). Kết quả là một đường phân quyền thứ tư, bên cạnh ma trận và hai cờ của [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md), trong khi 0021 đã ghi ba đường là cái giá phải canh.

### Phương án D — Giữ nguyên code, không viết gì

**Được:** không tốn gì.

**Vì sao loại:** reviewer đã đọc sự im lặng này thành một lỗ bảo mật. Lượt review sau, hoặc người thi công sau, sẽ "vá" nó theo B mà không biết B phá đường sửa.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Cấp `core.permission.write` là cấp toàn quyền trong đơn vị** | Tách khoá ([`../database/schema-core.md`](../database/schema-core.md) §5.2 ràng buộc 3) không có tác dụng với người giữ khoá này: họ tự thêm cho mình bất kỳ khoá nào ở request sau. Không có vai trò "chỉ quản lý phân quyền cho một nhóm tài nguyên" |
| **Luật 1 và luật 5 của `users.md` §2 không chặn được người giữ khoá này** | Cả hai so với tập quyền **hiện tại** của người gọi, và người giữ khoá tự mở rộng được tập đó trước khi gọi. Hai luật chỉ canh người **không** giữ khoá gốc |
| **Leo thang qua ma trận không để lại dấu riêng** | Không tài liệu nào bắt buộc ghi nhật ký kiểm toán cho việc ghi ma trận. Chưa biết việc tự gắn khoá có truy vết được không. Đây là câu chưa có lời đáp, không phải câu đã được giải |
| Mô hình không nới được sau này mà không đụng vấn đề khoá mồ côi | Ngày cần phân quyền uỷ nhiệm, người làm phải giải bài toán ở phương án B, không chỉ thêm một phép kiểm |

### Tích cực

- Bất biến "đường sửa" giữ nguyên: người giữ vai trò hệ thống luôn dựng lại được mọi ô của ma trận qua API.
- Không thêm đường phân quyền nào, và không thêm phép đếm nào trên đường ghi.
- Câu hỏi reviewer đặt ra có câu trả lời tại chỗ, kèm lý do loại B và C.

### Điều gì là dấu hiệu quyết định này bắt đầu sai

- Một dự án cần cấp khoá này cho người **không** phải quản trị đơn vị, ví dụ trưởng nhóm tự phân quyền cho nhóm mình. Đó là nhu cầu uỷ nhiệm, và mô hình này không có câu trả lời cho nó.
- Có sự cố một người tự gắn khoá vào vai trò mình mà không ai truy ra được. Việc ghi nhật ký cho thao tác ghi ma trận không còn là tuỳ chọn.
- Số tài khoản giữ khoá này qua vai trò trong một đơn vị lớn lên cỡ số tài khoản mang các khoá nghiệp vụ. Nghĩa là người vận hành đang cấp nó như một khoá thường.

### Rút lui nếu sai

Chuyển sang B hoặc một biến thể của nó là việc **code**: thêm một phép kiểm vào đúng một handler (`UpdatePermissionMatrixCommandHandler`), không đổi lược đồ, không chuyển dữ liệu, vì `core.role_permission` không đổi hình. Phần đắt là bài toán khoá mồ côi. Phải chốt nó bằng một ADR mới **trước** khi thêm phép kiểm. Dữ liệu sinh ra trong thời gian quyết định này còn hiệu lực là những ô ma trận mà có thể ai đó đã tự cấp. Đợt rút lui phải kèm một lượt rà vai trò nào đang giữ khoá này trong từng đơn vị.

## Liên quan

- [`../contracts/permissions.md`](../contracts/permissions.md) §6: nơi quyết định được khai
- [`../contracts/users.md`](../contracts/users.md) §2: luật 1, luật 5, luật "người cuối cùng" đã loại
- [`0005-permission-based.md`](0005-permission-based.md) · [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)
