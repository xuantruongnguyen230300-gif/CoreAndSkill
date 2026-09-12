---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `V3` — Ngưng và bật lại hoạt động của một đơn vị

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Đơn vị **không bị xoá, chỉ bị ngưng hoạt động**. Lý do ở §3.

---

## 1. Ai bắt đầu, ở đâu

Tài khoản vận hành hệ thống, từ danh sách đơn vị trong khu quản trị hệ thống.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Tài khoản mang `is_system_operator` | |
| Đơn vị đích **không phải** đơn vị hệ thống | Ngưng đơn vị hệ thống là tự khoá mình ra ngoài |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Vận hành | `PUT /system/tenants/{id}/active` với trạng thái mong muốn | [`../contracts/tenants.md`](../contracts/tenants.md) §3 |
| 2 | BE | Kiểm đơn vị đích không mang cờ đơn vị hệ thống | [`../database/schema-core.md`](../database/schema-core.md) §1.3 |
| 3 | BE | Đổi cờ hoạt động của đơn vị | cùng trên |

### Vì sao ngưng chứ không xoá

| Nếu xoá | Hệ quả |
| --- | --- |
| Dữ liệu nghiệp vụ của đơn vị | Mất, không lấy lại |
| Nhật ký kiểm toán | Khoá ngoại tới bảng đơn vị ở dạng `ON DELETE RESTRICT` **chặn thẳng** — xem luồng `N6`. Nghĩa là xoá một đơn vị **không thực hiện được** khi nhật ký của nó còn, và đó là chủ đích |
| Điều tra về sau | Không còn gì để điều tra |

Hướng **xoá đơn vị** đã bị loại và khoá, khai ở [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10. Giải phóng dung lượng đi qua một thao tác lưu trữ có kiểm soát, không qua lệnh xoá.

## 4. Hỏng ở đâu — và ai thấy gì

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Không có đơn vị đó | `CORE.TENANT.NOT_FOUND` (404) | |
| Ngưng đơn vị hệ thống | `CORE.TENANT.SYSTEM_IMMUTABLE` (422) | Chặn thẳng. **Phép kiểm này dựa vào cột `is_system`** — trước khi cột đó tồn tại, mã lỗi này không ai viết nổi |
| Đơn vị vừa bị ngưng | — | Mọi tài khoản của nó **không đăng nhập được nữa**, và nhận `CORE.AUTH.INVALID_CREDENTIALS` — cùng câu với "sai mật khẩu", cố ý không phân biệt |

## 5. Quan hệ với đơn vị

**Đối tượng là chính một đơn vị.** Người thao tác thuộc đơn vị hệ thống, như luồng `V2`.

Cột `is_system` là thứ phân biệt "đơn vị được phép ngưng" với "đơn vị không được phép". Nó **không** dựa vào mã đơn vị — mã do người gõ, và một đơn vị thật tình cờ trùng mã sẽ làm sập phép kiểm ([`../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)).

## 6. Câu chưa trả lời được

> 🛑 **Người đang có phiên mở khi đơn vị bị ngưng thì ai đá ra?**

[`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §6.3 chặn ở hai chỗ, nhưng **cả hai đều nằm trên đường đăng nhập**. Một người đã đăng nhập từ trước, giữ phiếu còn hạn, không đi qua chỗ nào trong hai chỗ đó.

Đây là cùng câu hỏi chưa trả lời của luồng `D1` §6, nhìn từ phía người ra lệnh ngưng. Ngưng một đơn vị mà người của nó vẫn đang thao tác được là một lệnh **có vẻ đã thi hành nhưng chưa**.

Hai câu còn lại:

- **Bật lại một đơn vị có cần seed lại gì không?** Nếu đơn vị bị ngưng vì seed hỏng giữa chừng (luồng `V2` §4), bật lại nó sẽ cho ra một đơn vị vẫn thiếu menu.
- **Nhật ký kiểm toán của thao tác này ghi vào đơn vị nào** — của người vận hành hay của đơn vị bị ngưng? Cùng câu hỏi với `N6` §6 và `V2` §6.
