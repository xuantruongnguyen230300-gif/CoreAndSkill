---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Lộ trình thi công Core BE — tổng thể

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo đang ở giai đoạn 1, chưa có `src/`. Toàn bộ lộ trình dưới đây là **kế hoạch cho giai đoạn 2**, không phải mô tả tiến độ.
>
> Các file `be/01-…18-…` trả lời *"một Core BE tốt gồm gì và vì sao"*. Thư mục này trả lời *"làm theo thứ tự nào, và mỗi bước xong thì có gì chạy được"*. Bản đối xứng phía FE: [`../../fe/trien-khai/00-lo-trinh-tong-the.md`](../../fe/trien-khai/00-lo-trinh-tong-the.md).

---

## 1. Năm pha

| Pha | Tên | Xong thì có gì **chạy được** |
| --- | --- | --- |
| **B0** | Nền móng | Năm project dựng xong với đúng chiều tham chiếu; một endpoint thử trả envelope đúng khuôn cho cả nhánh thành công lẫn nhánh lỗi; thiếu một giá trị cấu hình bắt buộc thì app **không khởi động**; ArchTest đã chạy và đã từng đỏ |
| **B1** | Dữ liệu, đơn vị, danh tính | Database dựng từ trống bằng runbook; đăng nhập bằng cookie chạy thật qua HTTP; mọi bảng dữ liệu mang cột đơn vị và bộ lọc đơn vị đã bật; app từ chối khởi động khi DB lệch model |
| **B2** | Phân quyền, menu, bảo mật biên | Bốn card hợp đồng hiện có chạy đúng như đã khai; 403 bắn đúng chỗ; CSRF và rate limit chặn thật, chứng minh bằng test |
| **B3** | Vận hành | Nhật ký kiểm toán ghi được; chỉ số và health check phản ánh đúng trạng thái; triển khai và quay lui đi theo đúng thứ tự ở [`../18-trien-khai-va-van-hanh.md`](../18-trien-khai-va-van-hanh.md); tạo một đơn vị mới chạy lại được nhiều lần |
| **B4** | Tệp, nhập/xuất, thông báo | Đính kèm tệp và tải lại được có kiểm quyền; xuất danh sách theo bộ lọc đang xem; một sự kiện nghiệp vụ sinh thông báo tới đúng người nhận qua Outbox |

Mỗi pha một file: [`01-b0-nen-mong.md`](01-b0-nen-mong.md) · [`02-b1-du-lieu-don-vi-danh-tinh.md`](02-b1-du-lieu-don-vi-danh-tinh.md) · [`03-b2-phan-quyen-va-bien.md`](03-b2-phan-quyen-va-bien.md) · [`04-b3-van-hanh.md`](04-b3-van-hanh.md) · [`05-b4-tep-nhap-xuat-thong-bao.md`](05-b4-tep-nhap-xuat-thong-bao.md)

---

## 2. Thứ tự này đến từ đâu

Không phải thứ tự ưu tiên mà là **thứ tự phụ thuộc**, lấy từ đồ thị ở [`../01-core-components.md`](../01-core-components.md) §4. Năm pha chỉ là cách cắt đồ thị đó thành những lát mà mỗi lát kết thúc bằng một thứ chạy được.

Ba quyết định về thứ tự, mỗi cái đều có thể làm khác và đều tốn giá nếu làm khác:

| Quyết định | Vì sao |
| --- | --- |
| **ArchTest bật từ B0, không để cuối** | Bộ test viết sau khi code xong sẽ được viết cho khớp code hiện có — nó xác nhận hiện trạng thay vì ép luật. [`../01-core-components.md`](../01-core-components.md) §4 nói thẳng điều này |
| **Multi-tenant nằm ở B1, không phải pha cuối** | Cột đơn vị áp cho **mọi** bảng dữ liệu và bộ lọc gắn một lần khi dựng model ([`../../../database/schema-core.md`](../../../database/schema-core.md) §3.7, [`../17-multi-tenant.md`](../17-multi-tenant.md) §6). Thêm sau nghĩa là viết lại schema và rà lại từng truy vấn đã viết |
| **Phân quyền sau danh tính, trước menu** | Menu động lọc theo quyền, nên nó chỉ kiểm chứng được khi đã có quyền thật ([`../01-core-components.md`](../01-core-components.md) §4) |

```text
B0 ──► B1 ──► B2 ──► B3 ──► B4
 └──────┴──────┴──────┴──────┴──► ArchTest + cổng tài liệu (chạy từ B0)
```

---

## 3. Nguyên tắc chi phối cả năm pha

| Nguyên tắc | Nghĩa cụ thể |
| --- | --- |
| **Mỗi pha kết thúc bằng thứ chạy được** | Không phải "một solution đầy project". Định nghĩa hoàn thành của mỗi pha là một hành vi kiểm được qua HTTP hoặc qua một lệnh |
| **Luật có cổng cùng lúc với luật** | Luật chưa có cổng nằm ở [`../../../RULES.md`](../../../RULES.md) §10, không nằm trong đầu người viết |
| **Chỉ dựng thứ thuộc Nhóm A** | Thành phần Nhóm B ([`../01-core-components.md`](../01-core-components.md) §2) chỉ dựng khi ngưỡng chạm. Năm pha này **không** bao gồm chúng |
| **Hợp đồng trước code** | Endpoint nào có người dùng thật thì card ở [`../../../contracts/`](../../../contracts/) phải xong trước — FE làm song song theo card đó |

---

## 4. Quan hệ với lộ trình FE

Hai lộ trình chạy song song được, với đúng một ràng buộc: **F2 của FE cần B2 của BE đã xong** (đăng nhập, phân quyền, menu). Trước mốc đó FE dùng dữ liệu giả theo card hợp đồng.

Ràng buộc thứ hai, nhẹ hơn: F3 (hai màn quản trị) cần các endpoint người dùng và phân quyền của B2 chạy thật.

---

## 5. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Core gồm những thành phần nào, cái nào Nhóm A cái nào Nhóm B | [`../01-core-components.md`](../01-core-components.md) |
| Năm project và chiều tham chiếu | [`../../../kien-truc-core-module.md`](../../../kien-truc-core-module.md) §2 |
| Toàn bộ luật kèm cột ép bằng gì | [`../../../RULES.md`](../../../RULES.md) |
| Lộ trình FE | [`../../fe/trien-khai/00-lo-trinh-tong-the.md`](../../fe/trien-khai/00-lo-trinh-tong-the.md) |
