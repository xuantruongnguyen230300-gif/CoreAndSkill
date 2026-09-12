---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0015 — FE và API phục vụ ở HAI origin khác nhau

> **Trạng thái:** Đã chấp nhận (2026-09-10)

## Bối cảnh

Tài liệu của repo mang **hai mô hình phục vụ mâu thuẫn nhau**, cả hai cùng `kind: luat`:

| Phía | Khai gì |
| --- | --- |
| `quy-uoc/be-api-controller.md` §7 | FE ở **origin khác** BE ⇒ CORS allowlist + antiforgery hai lớp |
| `wiki-core/fe/17-phuc-vu-va-trien-khai.md` §4 | FE và API **cùng một nguồn** qua reverse proxy ⇒ không có CORS |

Giả định "khác nguồn" có mặt ở bốn file; giả định "cùng nguồn" ở ba file. Không bên nào có ADR, nên không có chỗ nào nói bên nào là quyết định.

Đây là mâu thuẫn nặng nhất tìm được trong lượt rà soát ngày 2026-09-09 ([`../audit/2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md`](../audit/2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md)): người thi công đọc file chủ của phía mình, tin nó, và không có lý do gì mở file của phía kia.

## Quyết định

> **FE và API phục vụ ở hai origin khác nhau, ở MỌI môi trường.** Dev là hai cổng; prod là hai subdomain **của cùng một tên miền gốc** — tức **cùng site**, nên cookie phiên vẫn là `SameSite=Lax`. Dev bật CORS và chạy HTTPS như thật, **không** dùng proxy của máy chủ dev để giấu ranh giới đi.

File chủ của chuỗi ràng buộc kéo theo là [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7. Mọi file khác trỏ về đó.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Cùng nguồn qua reverse proxy

Xoá bỏ cả một nhóm bài toán thay vì giải chúng: không CORS, cookie first-party, không cần biết đường dẫn API, CSP đơn giản hơn.

**Vì sao loại:** nó đòi một reverse proxy ở **mọi** môi trường, kể cả máy của từng lập trình viên. Cái giá đó lặp lại với mỗi người mới vào dự án, và nó dồn một thành phần hạ tầng vào giữa FE và BE ngay cả khi chỉ chạy thử một endpoint. Với một Core dựng cho nhiều dự án, ràng buộc "phải có proxy" là ràng buộc áp lên mọi dự án hạ nguồn.

### Phương án B — Cùng nguồn ở thật, proxy dev ở máy lập trình viên

**Vì sao loại:** đây là phương án tệ nhất trong ba. Nó vẫn cần proxy ở prod, và ở dev nó **giấu** đúng nhóm lỗi mà ta cần thấy sớm. Mọi bẫy của cookie chéo nguồn chỉ lộ ra ở lần triển khai đầu tiên.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| CORS phải đúng ở mọi môi trường | Sai một origin là hỏng đăng nhập, và triệu chứng (401 sau khi đăng nhập thành công) không nói ra nguyên nhân |
| FE và API phải **cùng scheme** để còn là cùng site | **HTTPS ở mọi môi trường, kể cả máy lập trình viên** — FE `http` mà API `https` là khác site, cookie phiên không được gửi. Cái giá lặp lại với mỗi người mới |
| `SameSite=Lax` chỉ chặn request từ **site khác** | Một subdomain khác của cùng tên miền gốc vẫn là cùng site — nên kiểm `Origin` và antiforgery vẫn **bắt buộc** |
| FE phải biết đường dẫn API | Không suy ra được từ nguồn của chính nó — xem [`../wiki-core/fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §5.2 |

### Tích cực

- Không cần reverse proxy ở môi trường phát triển.
- FE và BE triển khai độc lập nhau.
- Dev chạy đúng hình dạng của thật, nên nhóm lỗi cookie chéo nguồn lộ ra ở máy lập trình viên chứ không ở lần triển khai đầu tiên.

### Điều kiện của quyết định

FE và API **cùng tên miền gốc và cùng scheme**. Khác tên miền gốc thì cookie thành bên thứ ba — phải `SameSite=None` và trình duyệt có thể chặn hẳn. Khi đó phải lật ADR này.

### Cấu hình đường dẫn API cho FE

Nhúng lúc build (chốt 2026-09-10) — [`../wiki-core/fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §5.2.

## Luật đi kèm

Quyết định này **không sinh luật mới** — nó chốt lại tiền đề cho các luật đã có ở [`../RULES.md`](../RULES.md) §6 (bảo mật & phân quyền). Điều nó đổi là: bản mô tả "cùng nguồn" ở khu FE đã được gỡ, nên chỉ còn một tiền đề trong toàn repo.
