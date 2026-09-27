---
kind: luat
scope: core
verified: chua-doi-chieu
---

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

# `be-api-controller.md` — phần chưa thi công

> File này giữ phần của [`be-api-controller.md`](be-api-controller.md) mô tả một đích đến **chưa có
> trong `src/BE`** hôm nay. Số mục dưới đây trùng số mục của file luật; khi phần nào về code thì lật
> nhãn ở đúng mục đó và chuyển nội dung ngược lại file luật, không đổi số mục.

---

## 8.2 Swagger / OpenAPI

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Đối chiếu 2026-09-22: `src/BE` chưa tham chiếu gói OpenAPI nào và chưa
> action nào khai `[ProducesResponseType]`; chỗ chờ duy nhất là chú thích `Swagger theo môi trường` trong
> `CoreWebServiceCollectionExtensions.cs`.

- Bật ở Development; ở Production **chỉ** bật sau xác thực, hoặc tắt hẳn.
- Mọi action khai `[ProducesResponseType]` cho status thành công và ít nhất một nhánh lỗi.
- Kiểu trả về trong tài liệu là `ApiEnvelope<T>`, không phải `T`.
- OpenAPI là tài liệu **sinh ra**, không phải hợp đồng — hợp đồng nằm ở [`../contracts/`](../contracts/).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §8.2.
