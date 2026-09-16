---
kind: luat
scope: core
verified: chua-doi-chieu
---

# B2 — Phân quyền, menu, bảo mật biên

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**
>
> **Định nghĩa hoàn thành:** card `users.md`, `roles.md`, `permissions.md`, `meta-menu.md` và `auth.md` §10 chạy đúng như đã khai; 403 bắn đúng chỗ và không nhầm với 401; CSRF, CORS và rate limit chặn thật, mỗi cái có một test chứng minh.

---

## 1. Dựng gì ở pha này

| Thứ | File chủ |
| --- | --- |
| Phân quyền theo permission, danh mục quyền do module cấp | [`../02-identity-auth.md`](../02-identity-auth.md) §3 · [`../../../database/schema-core.md`](../../../database/schema-core.md) §5 |
| Menu động lọc theo quyền | [`../../../contracts/meta-menu.md`](../../../contracts/meta-menu.md) |
| CSRF, CORS, cookie, rate limit | [`../../../quy-uoc/be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §6, §7 |
| Header bảo mật và các lớp còn lại | [`../09-security-beyond-auth.md`](../09-security-beyond-auth.md) |
| Card hợp đồng của pha | [`../../../contracts/users.md`](../../../contracts/users.md) · [`../../../contracts/roles.md`](../../../contracts/roles.md) · [`../../../contracts/permissions.md`](../../../contracts/permissions.md) · [`../../../contracts/meta-menu.md`](../../../contracts/meta-menu.md) · [`../../../contracts/auth.md`](../../../contracts/auth.md) §10 |

---

## 2. Thứ tự viết

1. **Danh mục quyền** và cơ chế module tự khai quyền của mình.
2. **Kiểm quyền ở biên**, mặc định **từ chối** khi chưa khai.
3. **Các endpoint của card thuộc pha này** (bảng §1), viết theo đúng card — card là hợp đồng, không phải gợi ý.
4. **Menu động**, lọc theo quyền, giữ mục cha khi có con hiện.
5. **Lớp biên**: CORS allowlist, antiforgery, rate limit theo từng chính sách.

---

## 3. Ba thứ hay bị làm sai ở B2

| Sai | Hậu quả |
| --- | --- |
| **Phân nhánh theo tên vai trò** | Luật S2 cấm. Tên vai trò là dữ liệu của dự án, đổi lúc nào không ai biết |
| **Chuỗi đường dẫn của rate limit gõ tay ở hai chỗ** | Lệch một đoạn là hàng rào chạy mỗi request mà **không chặn gì** — [`../../../quy-uoc/be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §8.1 |
| **Trả 302 thay vì 401 khi chưa đăng nhập** | FE là SPA, nó đi theo chuyển hướng và nhận về một trang HTML với mã 200 — [`../../../contracts/auth.md`](../../../contracts/auth.md) §5 |

---

## 4. Nghiệm thu B2

- [ ] Tài khoản thiếu quyền gọi endpoint → 403 kèm mã lỗi đúng; **không** điều hướng.
- [ ] Chưa đăng nhập gọi endpoint → 401 JSON sạch.
- [ ] Endpoint mới quên khai quyền → **bị từ chối**, không mở mặc định.
- [ ] Gửi một request ghi thiếu header chống CSRF → 403, kèm đúng mã lỗi ở card.
- [ ] Gửi một request ghi mang `Origin` ngoài allowlist **kèm token hợp lệ** → 403 `CORE.AUTH.ORIGIN_REJECTED`.
- [ ] Phiên đã hết hạn, gửi request ghi kèm token cũ tới endpoint đòi đăng nhập → 401 `CORE.AUTH.NOT_AUTHENTICATED`, **không** 403 `CORE.AUTH.CSRF_REJECTED`.
- [ ] Request mang phiên hợp lệ → response có `Set-Cookie` cấp lại cookie phiên với mốc hết hạn tính từ chính request đó.
- [ ] Gọi từ một nguồn ngoài allowlist → bị CORS chặn; nguồn trong allowlist thì gửi kèm cookie được.
- [ ] Vượt ngưỡng rate limit → 429 kèm `Retry-After`, và ngưỡng **không** lộ ra trong thông báo.
- [ ] Mỗi phản hồi của card thuộc pha này khớp đúng khuôn đã khai, gồm cả bảng lỗi.

---

## 5. Đọc tiếp

Pha kế: [`04-b3-van-hanh.md`](04-b3-van-hanh.md).
