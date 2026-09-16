---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0022 — Seed dev không có đường code riêng: `.sql` lo dữ liệu, lệnh bootstrap lo tài khoản

> **Trạng thái:** Đã chấp nhận (2026-09-12) · Sửa một phần bởi ADR-0023 (2026-09-14)
>
> Sửa một phần bởi [`0023-dich-vu-tao-don-vi-dung-chung.md`](0023-dich-vu-tao-don-vi-dung-chung.md).
>
> **Lật [`0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md`](0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md).** ADR đó chọn một lệnh seed dev riêng, canh bằng ba điều kiện đồng thời và để lại dấu nhận dạng cố định. Quyết định này bỏ toàn bộ cơ chế đó.

---

## Bối cảnh

Nhu cầu không đổi so với ADR-0020: từ [`0013-multi-tenant.md`](0013-multi-tenant.md) và [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md), một bản cài dùng được cần **hai** tài khoản ở **hai** đơn vị, và tài khoản thứ hai chỉ ra đời sau khi tài khoản thứ nhất đăng nhập được. Dựng tay chuỗi đó mỗi lần tạo lại database dev là ma sát thật, lặp lại mỗi ngày.

ADR-0020 giải bằng cách thêm một **đường code seed dev** có mật khẩu biết trước, rồi dựng ba hàng rào quanh nó. Chính ADR đó thừa nhận một ca nó không đóng được: máy chạy thật + biến môi trường bị đặt nhầm `Development` + database cùng máy + cài mới. Lý do là cả ba hàng rào đều khoá theo **tên môi trường**, nên chúng sai **cùng lúc** — một tương quan, không phải ba lỗi độc lập.

## Quyết định

> **Không có đường code nào chứa sẵn thông tin tài khoản.** Việc dựng một môi trường dev tách làm hai phần, theo đúng thứ mỗi công cụ làm được:
>
> | Phần | Nội dung | Công cụ |
> | --- | --- | --- |
> | **Dữ liệu** | Đơn vị hệ thống, đơn vị `DEV`, bộ vai trò, ánh xạ vai trò→quyền, menu | Một tệp `.sql` idempotent, nằm trong `src/BE/` |
> | **Tài khoản** | `admin` (quản trị đơn vị `DEV`) và `superadmin` (vận hành hệ thống) | **Lệnh bootstrap đã có**, đọc mật khẩu từ `user-secrets` của máy đang chạy |
> | **Tiện lợi** | Gọi hai phần trên theo thứ tự | Một script bọc, **không chứa bí mật nào** |

Người viết code vẫn gõ đúng một lệnh. Khác biệt duy nhất: mật khẩu nằm trong `user-secrets` trên máy họ, **không nằm trong repo**.

### Vì sao bỏ hẳn cơ chế của ADR-0020

**Một đường code không tồn tại thì không cần hàng rào.** Ba điều kiện đồng thời, dấu nhận dạng cố định, và hàng rào từ chối khởi động của ADR-0020 đều tồn tại để canh **một đoạn code có mật khẩu biết trước**. Bỏ đoạn code đó thì cả ba mất đối tượng — kể cả ca mà ADR-0020 thừa nhận không đóng được.

Đây là khác biệt về **loại**, không phải về mức độ: ADR-0020 làm rủi ro **nhỏ đi**; quyết định này làm nó **không có chỗ phát sinh**.

### Vì sao `.sql` không tạo tài khoản

`PasswordHasher<TUser>` dùng PBKDF2 với salt ngẫu nhiên mỗi lần — không hàm SQL nào sinh được chuỗi băm hợp lệ ([`../database/schema-core.md`](../database/schema-core.md) §4.1).

Cách duy nhất để một tệp `.sql` tạo được tài khoản đăng nhập được là **dán sẵn một chuỗi băm đã tính trước**. Phương án đó bị loại, ba lý do:

| # | Lý do |
| --- | --- |
| 1 | **Chuỗi băm chính là mật khẩu.** Ai có tệp thì đăng nhập được, và một mật khẩu dev dễ nhớ thì bẻ được kể cả không có tệp |
| 2 | **Nó vào git vĩnh viễn.** Xoá tệp về sau không xoá khỏi lịch sử. Khác hẳn `user-secrets` — thứ không bao giờ rời khỏi một máy |
| 3 | **Phải tự đặt đúng `normalized_user_name`, `security_stamp`, `concurrency_stamp`.** Sai thì Identity hỏng theo kiểu khó chẩn đoán nhất: tra cứu đăng nhập trượt mà không báo lỗi, hoặc đổi mật khẩu xong phiên cũ vẫn sống |

### Chỗ đặt tệp `.sql` — một chi tiết không phải tuỳ chọn

Tệp `.sql` dev **phải nằm ngoài** `database/scripts/`. Lệnh cài đặt ở [`../database/script-runbook.md`](../database/script-runbook.md) §3.3 chạy **glob** `database/scripts/core/*.sql`, **không có allowlist** — bất kỳ tệp nào rơi vào đó sẽ tự chạy trên mọi bản cài, kể cả bản chạy thật.

## Phương án đã cân và loại

| Phương án | Vì sao loại |
| --- | --- |
| **Giữ nguyên ADR-0020** | Dựng ba hàng rào để canh một rủi ro mà ta có thể không tạo ra ngay từ đầu. Và ba hàng rào đó sai cùng lúc trong đúng ca xấu nhất |
| **Một tệp `.sql` duy nhất, có dán sẵn chuỗi băm** | Ba lý do ở trên. Đổi lấy: bớt đúng một lệnh |
| **Nới chính sách mật khẩu ở dev để dùng chuỗi ngắn** | Nhánh từ chối mật khẩu yếu sẽ **không bao giờ chạy trên máy lập trình viên**, nên lỗi ở nhánh đó chỉ lộ lần đầu trên bản chạy thật. Đã thành luật **S9** |
| **Không seed gì cả, dựng tay mỗi lần** | Ma sát thật và lặp lại. Nó cũng khiến mỗi người dựng ra một môi trường dev khác nhau |

## Hệ quả

### Tích cực

- **Không còn thứ gì để rò.** Không mật khẩu trong repo, không dấu nhận dạng phải đồng bộ hai đầu, không danh sách định danh dev cần người sở hữu — ADR-0020 nêu chính khoản cuối là câu yếu nhất của nó.
- **Bớt ba luật phải thi công.** S7 và S8 mất đối tượng và được rút; hàng rào từ chối khởi động không phải viết.
- **Đường cài đặt thật và đường dev dùng CHUNG một lệnh tạo tài khoản.** Nghĩa là đường đó được chạy mỗi ngày trên máy dev, thay vì chỉ chạy một lần trên bản cài thật rồi không ai biết nó còn đúng không.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Mỗi máy dev phải đặt `user-secrets` một lần** | Một bước thủ công thêm khi có người mới vào dự án. Tài liệu hướng dẫn phải nêu, và **không** nêu giá trị mật khẩu cụ thể — người mới tự đặt |
| **Script bọc là thứ thứ ba phải bảo trì** | Nó gọi `psql` và `dotnet`; đổi đường dẫn project hay tên tệp `.sql` là phải sửa nó. Không có cổng nào canh |
| **Tệp `.sql` dev nằm ngoài `database/scripts/` nên không được cổng checksum của runbook §3.4 canh** | Nó lệch khỏi schema thật mà không ai báo. Chấp nhận: nó chỉ dựng dữ liệu dev |

## Điều kiện lật quyết định

1. **Có người dựng một đường code seed có mật khẩu biết trước** ở bất kỳ đâu trong `src/` — kể cả có hàng rào. Lúc đó tiền đề *"không tạo ra rủi ro ngay từ đầu"* đã hỏng, và ADR-0020 nên được xem lại nghiêm túc thay vì để một bản vá tự phát tồn tại.
2. **Việc đặt `user-secrets` trở thành rào cản thật** — đo được bằng: có người mới không dựng nổi môi trường dev trong ngày đầu vì bước này. Lúc đó câu trả lời là **tài liệu hướng dẫn tốt hơn hoặc một script hỏi tương tác**, không phải nhúng mật khẩu vào repo.

## Luật bị rút

| Luật | Vì sao rút |
| --- | --- |
| **S7** — lệnh seed dev từ chối cứng khi thiếu một trong ba điều kiện | Không còn lệnh seed dev nào để canh |
| **S8** — môi trường không phải Development từ chối khởi động khi thấy định danh seed dev | Không còn định danh seed dev nào tồn tại |

**S9** (chính sách mật khẩu giống nhau ở mọi môi trường), **S10** (không mã nào rẽ nhánh theo tên đăng nhập), **M10**–**M12** đều **giữ nguyên** — chúng độc lập với cơ chế seed.

## Liên quan

- [`0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md`](0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md) — bị lật bởi quyết định này
- [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) — hai cờ đặc quyền, không bị ảnh hưởng
- [`../wiki-core/be/13-core-data-migration.md`](../wiki-core/be/13-core-data-migration.md) §5.2 — dữ liệu bootstrap
- [`../database/script-runbook.md`](../database/script-runbook.md) §3.3 — đường dựng database
