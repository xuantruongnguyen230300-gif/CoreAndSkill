---
kind: luat
scope: core
verified: chua-doi-chieu
---

# B1 — Dữ liệu, đơn vị, danh tính

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**
>
> **Định nghĩa hoàn thành:** dựng database từ trống bằng đúng runbook; lệnh bootstrap dựng hai đơn vị và hai tài khoản đầu tiên; đăng nhập bằng cookie chạy thật qua HTTP; card `auth.md` (trừ §10) và `profile.md` chạy đúng như đã khai; mọi bảng dữ liệu của đơn vị mang cột đơn vị và bộ lọc đã bật, chứng minh bằng test hai đơn vị; app **từ chối khởi động** khi database lệch model.

---

## 1. Dựng gì ở pha này

| Thứ | File chủ |
| --- | --- |
| Entity nền, năm cột audit, xoá mềm | [`../../../quy-uoc/be-entity-domain.md`](../../../quy-uoc/be-entity-domain.md) · [`../../../database/schema-core.md`](../../../database/schema-core.md) §3 |
| Cột đơn vị và bộ lọc truy vấn toàn cục | [`../17-multi-tenant.md`](../17-multi-tenant.md) §6 · [`../../../database/schema-core.md`](../../../database/schema-core.md) §3.7 |
| Biên transaction và validation ở tầng request | [`../../../quy-uoc/be-cqrs-handler.md`](../../../quy-uoc/be-cqrs-handler.md) · [`../../../adr/0006-pipeline-behavior.md`](../../../adr/0006-pipeline-behavior.md) |
| Migration do Core sở hữu, runbook áp schema | [`../13-core-data-migration.md`](../13-core-data-migration.md) · [`../../../database/script-runbook.md`](../../../database/script-runbook.md) |
| Danh tính, phiên cookie, người dùng hiện tại | [`../02-identity-auth.md`](../02-identity-auth.md) · [`../../../quy-uoc/be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §7 |
| Ngữ cảnh thực thi khi không có request | [`../../../quy-uoc/be-architecture.md`](../../../quy-uoc/be-architecture.md) §1.1 |
| Service tạo đơn vị dùng chung và lệnh bootstrap gọi nó (chưa có endpoint tạo đơn vị ở pha này) | [`../../../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../../../adr/0023-dich-vu-tao-don-vi-dung-chung.md) · [`../17-multi-tenant.md`](../17-multi-tenant.md) §10, §11.4 |
| Card hợp đồng của pha | [`../../../contracts/auth.md`](../../../contracts/auth.md) (trừ §10) · [`../../../contracts/profile.md`](../../../contracts/profile.md) |
| Đồng thời và khoá lạc quan | [`../06-concurrency-control.md`](../06-concurrency-control.md) |

---

## 2. Thứ tự viết

1. **Entity nền và quy ước cột**, chưa có bảng nghiệp vụ nào.
2. **Cột đơn vị và bộ lọc**, gắn một lần khi dựng model. Làm ngay bước này, không để pha sau.
3. **`DbContext` của Core**, bộ chặn ghi kiểm ràng buộc đơn vị (luật M8).
4. **Hai pipeline behavior**: transaction và validation. Không thêm behavior thứ ba.
5. **Migration đầu tiên và runbook**, dựng database từ trống, kèm cơ chế phát hiện DB lệch model.
6. **Danh tính**: Identity, phiên cookie, `ICurrentUser`, `ITenantContext`, service tạo đơn vị dùng chung và lệnh bootstrap gọi nó, rồi các endpoint của card `auth.md` (trừ §10) và `profile.md`.

Bước 2 là bước đắt nhất nếu làm sai thứ tự: gắn cột đơn vị sau khi đã có bảng và truy vấn nghĩa là sửa cả hai.

---

## 3. Ba thứ hay bị làm sai ở B1

| Sai | Hậu quả |
| --- | --- |
| **Gắn bộ lọc đơn vị lẻ ở từng cấu hình entity** | Entity mới quên khai thì mất bộ lọc, và **không có gì báo**. Luật ở [`../17-multi-tenant.md`](../17-multi-tenant.md) §6 |
| **Unique index không kèm mệnh đề xoá mềm** | Xoá mềm một bản ghi rồi tạo lại bản ghi cùng khoá sẽ vỡ — [`../../../database/schema-core.md`](../../../database/schema-core.md) §3.3 |
| **Chạy migration tự động lúc khởi động** | Trái [`../../../adr/0009-ap-schema-chay-tay.md`](../../../adr/0009-ap-schema-chay-tay.md): một lần triển khai hỏng giữa chừng để lại database ở trạng thái không ai biết |

---

## 4. Nghiệm thu B1

- [ ] Dựng database từ trống chỉ bằng các bước trong runbook, không thao tác tay ngoài tài liệu.
- [ ] Sửa model mà chưa sinh migration → app **không khởi động**.
- [ ] Đăng nhập qua HTTP thật, cookie phiên đúng thuộc tính đã khai.
- [ ] Tài khoản đang khoá: sai mật khẩu → `CORE.AUTH.INVALID_CREDENTIALS`; đúng mật khẩu → `CORE.AUTH.LOCKED_OUT` — thứ tự ở [`../02-identity-auth.md`](../02-identity-auth.md) §4.2.
- [ ] Tài khoản do quản trị khoá, gõ sai mật khẩu vượt ngưỡng khoá tự động → mốc khoá của quản trị **không đổi**.
- [ ] Test hai đơn vị: đơn vị A không đọc được dòng của đơn vị B, kể cả qua truy vấn viết tay.
- [ ] Ghi một bản ghi thiếu đơn vị → **bị từ chối**, không âm thầm ghi giá trị rỗng (luật M8).
- [ ] Lệnh bootstrap chạy **hai lần** không nhân đôi đơn vị hay tài khoản.
- [ ] Xoá một giá trị cấu hình mà lệnh bootstrap cần → lệnh dừng, **không ghi dòng nào**.
- [ ] Mỗi phản hồi của card `auth.md` (trừ §10) và `profile.md` khớp đúng khuôn đã khai, gồm cả bảng lỗi.
- [ ] ArchTest vẫn xanh; không project nào lọt tham chiếu EF Core lên tầng ứng dụng.

---

## 5. Đọc tiếp

Pha kế: [`03-b2-phan-quyen-va-bien.md`](03-b2-phan-quyen-va-bien.md).
