---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0017 — Khu quản trị hệ thống và tài khoản vận hành

> **Trạng thái:** Đã chấp nhận (2026-09-10)

## Bối cảnh

[`0013-multi-tenant.md`](0013-multi-tenant.md) chốt **cách ly tuyệt đối**: một tài khoản thuộc đúng một đơn vị, và không tồn tại tài khoản nhìn được dữ liệu của đơn vị khác.

Nhưng phải có ai đó **tạo đơn vị thứ hai**, và ngưng hoạt động một đơn vị khi hợp đồng kết thúc. Danh sách đơn vị bản thân nó là dữ liệu của mọi đơn vị, nên màn hình quản trị đơn vị **không đặt được** bên trong một đơn vị nào.

Trước quyết định này, việc đó chỉ làm được bằng một lệnh chạy tay lúc cài đặt — tức người vận hành phải là người có quyền truy cập máy chủ.

## Quyết định

> **Có một khu quản trị hệ thống, dùng bởi TÀI KHOẢN VẬN HÀNH. Tài khoản vận hành chỉ thấy danh sách đơn vị và trạng thái của chúng; nó KHÔNG thấy dữ liệu nghiệp vụ của bất kỳ đơn vị nào.**

Ba cơ chế giữ ranh giới đó, và điểm mạnh của thiết kế là **cả ba đều là cơ chế sẵn có**, không phải luật viết trên giấy:

| # | Cơ chế | Ép bằng |
| --- | --- | --- |
| 1 | Tài khoản vận hành thuộc một **đơn vị hệ thống dành riêng**, không thuộc đơn vị nghiệp vụ nào | Bộ lọc truy vấn theo đơn vị đang có. Mọi truy vấn dữ liệu nghiệp vụ của tài khoản này trả về **rỗng** — không cần thêm một lớp chặn nào |
| 2 | Quyền vào khu hệ thống đi bằng **một cờ trên tài khoản**, không đi qua ma trận quyền theo đơn vị | Ma trận quyền không có đường nào cấp quyền hệ thống; và ngược lại, tài khoản vận hành không nhận được quyền nghiệp vụ |
| 3 | Bảng `core.tenant` vốn **không mang cột đơn vị** ([`../database/schema-core.md`](../database/schema-core.md) §1.3) | Đọc được danh sách đơn vị không phải là một ngoại lệ của bộ lọc — bảng đó chưa bao giờ nằm trong phạm vi bộ lọc |

Điểm 1 là điểm quan trọng nhất: **ranh giới không do một câu lệnh kiểm quyền giữ, mà do chính bộ lọc đã có giữ.** Một lỗi lập trình ở khu hệ thống không mở được dữ liệu nghiệp vụ ra, vì tài khoản đó không có đơn vị nghiệp vụ nào để mà thấy.

## Quan hệ với ADR-0013

Quyết định này **lật một phần** [`0013-multi-tenant.md`](0013-multi-tenant.md): câu *"không tồn tại tài khoản nhìn được mọi đơn vị"* nay đúng với **dữ liệu nghiệp vụ**, không đúng với **danh sách đơn vị**.

Ba điều của ADR-0013 **không** bị lật, và không được lật kèm:

- Không có báo cáo tổng hợp xuyên đơn vị.
- Một tài khoản vẫn thuộc đúng một đơn vị.
- Không có tài khoản nào đọc được dữ liệu nghiệp vụ của đơn vị khác.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Chỉ dùng lệnh chạy tay, không có màn hình

**Được:** không lật gì cả, không thêm bề mặt tấn công, không thêm code.

**Vì sao loại:** nó buộc người tạo đơn vị phải có quyền vào máy chủ. Ở mô hình nhiều cơ quan dùng chung một bản cài, việc thêm một cơ quan là **việc thường xuyên**, không phải việc của một lần cài đặt.

### Phương án B — Một tài khoản quản trị nhìn và sửa được mọi đơn vị

**Vì sao loại:** đây là phương án tệ nhất. Một tài khoản bị chiếm là mất dữ liệu của **mọi** cơ quan cùng lúc, và nó xoá bỏ chính tính chất mà ADR-0013 dựng lên. Cái tiện mà nó đổi lấy — sửa hộ dữ liệu cho một đơn vị — nên giải bằng quy trình hỗ trợ có ghi vết, không bằng một tài khoản vạn năng.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| Thêm một bề mặt đăng nhập có giá trị cao | Tài khoản vận hành phải chịu ràng buộc chặt hơn: bắt buộc đổi mật khẩu lần đầu, ghi nhật ký kiểm toán mọi thao tác, và nên giới hạn số lượng |
| Một cờ trên tài khoản là một đường phân quyền **thứ hai** | Hai đường phân quyền song song luôn có nguy cơ lệch. Bù lại bằng luật M9: tài khoản mang cờ vận hành **không được** gán vai trò nghiệp vụ nào, ép bằng test |
| Đơn vị hệ thống là một dòng dữ liệu đặc biệt | Mọi chỗ liệt kê đơn vị cho người dùng chọn phải loại nó ra. Quên là lộ một đơn vị không có thật với người dùng |

### Tích cực

- Thêm một cơ quan không còn cần quyền vào máy chủ.
- Ranh giới dữ liệu do bộ lọc sẵn có giữ, không do một lớp kiểm quyền mới.
- Đường tạo đơn vị có nhật ký kiểm toán, thay vì một lệnh chạy tay không để lại vết trong ứng dụng.

### Điều kiện lật quyết định

Nếu xuất hiện nhu cầu **tài khoản vận hành xem được dữ liệu nghiệp vụ** để hỗ trợ người dùng, thì đó là một quyết định khác hẳn và phải có ADR mới — kèm cơ chế ghi vết và giới hạn thời gian, không phải một cờ nữa.

## Liên quan

- [`0013-multi-tenant.md`](0013-multi-tenant.md) — quyết định bị lật một phần
- [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10, §11.4 — vòng đời đơn vị và seed
- [`../contracts/tenants.md`](../contracts/tenants.md) — hợp đồng của khu quản trị hệ thống
- [`../RULES.md`](../RULES.md) §9 — luật M9
