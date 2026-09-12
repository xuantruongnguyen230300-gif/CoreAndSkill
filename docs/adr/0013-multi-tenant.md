---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0013 — Multi-tenant bằng cột phân biệt, cách ly tuyệt đối, một tài khoản một tenant

> **Trạng thái:** Đã chấp nhận (2026-09-08)
>
> Thay thế mục "chưa làm multi-tenant ở v1" ở [`0001-modular-monolith.md`](0001-modular-monolith.md) và [`../wiki-core/be/01-core-components.md`](../wiki-core/be/01-core-components.md) §Áp dụng. Multi-tenant nay **nằm trong phạm vi v1**.

## Bối cảnh

Core này phục vụ các hệ thống dùng chung cho **nhiều đơn vị hành chính hoặc tổ chức độc lập** — nhiều sở, nhiều huyện, nhiều trường cùng chạy trên một hệ.

Ba đặc điểm nghiệp vụ đã được xác nhận, và chính chúng quyết định thiết kế:

| Câu hỏi | Trả lời | Hệ quả kiến trúc |
| --- | --- | --- |
| Tenant là gì | Đơn vị hành chính / tổ chức độc lập | Tenant là một thực thể có danh tính, không phải một thuộc tính suy ra được |
| Có bao giờ cần dữ liệu xuyên tenant | **Không bao giờ** | Cách ly tuyệt đối. Không cần mô hình phân cấp, không cần báo cáo hợp nhất |
| Một tài khoản thuộc mấy tenant | **Đúng một** | Tenant xác định ngay lúc đăng nhập, không cần màn chọn tenant, không cần đổi tenant giữa phiên |

Ba câu trả lời này là **dạng multi-tenant đơn giản nhất tồn tại**. Nếu bất kỳ câu nào đổi — đặc biệt là câu thứ hai — thì ADR này không còn đúng và phải viết ADR mới thay thế, không sửa file này.

## Vì sao phải phân biệt multi-tenant với giới hạn dữ liệu theo phạm vi

Đây là chỗ nhầm phổ biến nhất, và nhầm theo hướng đắt: nhiều đội dựng cả bộ máy multi-tenant cho một bài toán chỉ cần lọc dữ liệu theo phòng ban.

**Phép thử, một câu:** *có bao giờ cần một báo cáo tổng hợp xuyên qua các đơn vị không?*

- **Có** → không phải multi-tenant. Đó là **giới hạn dữ liệu theo phạm vi**: các đơn vị nằm trong cùng một tổ chức, chỉ khác quyền nhìn. Giải bằng permission cộng bộ lọc, rẻ hơn nhiều và không mang rủi ro rò dữ liệu.
- **Không, và ai hỏi câu đó là đang hiểu sai nghiệp vụ** → đúng là multi-tenant.

Ở đây câu trả lời là vế thứ hai. Ghi lại phép thử này vì nó là thứ người sau cần khi có ai đề nghị "thêm báo cáo tổng hợp toàn tỉnh" — đề nghị đó không phải một tính năng, nó là một thay đổi kiến trúc.

## Quyết định

**Mô hình: một database, một schema, cột phân biệt `TenantId` cộng bộ lọc truy vấn toàn cục.**

1. Mọi entity thuộc dữ liệu tenant khai `ITenantScoped` (mang `TenantId`).
2. `DbContext` gắn **bộ lọc truy vấn toàn cục** theo `TenantId` cho mọi entity đó — lập trình viên không phải nhớ lọc, và không có cách nào quên.
3. `TenantId` lấy từ **claim trong phiếu xác thực**, gán lúc đăng nhập. **Không bao giờ** lấy từ tham số client gửi lên.
4. Mọi **index duy nhất** phải gồm `TenantId` ở vị trí đầu.
5. Mọi lần gọi bỏ bộ lọc phải nằm trong **allowlist khai tường minh**, có test canh.
6. Tenant là một bảng thật trong schema `core`, có vòng đời: tạo, ngưng hoạt động. Ngưng hoạt động thì mọi tài khoản của nó không đăng nhập được.

## Phương án đã cân nhắc và vì sao loại

### Schema riêng cho mỗi tenant — loại

Cách ly tốt hơn: dữ liệu hai tenant nằm ở hai schema, một truy vấn quên lọc cũng không lọt sang được.

Loại vì **chi phí migration nhân lên theo số tenant**. Mỗi lần đổi schema là N lần chạy, và một lần chạy hỏng giữa chừng để lại các tenant ở các phiên bản schema khác nhau — trạng thái không có công cụ nào quản lý được. Với đơn vị hành chính, số tenant có xu hướng tăng dần và không được kiểm soát bởi đội phát triển.

Thêm một điểm: chính sách của repo này là **áp schema chạy tay** ([`0009-ap-schema-chay-tay.md`](0009-ap-schema-chay-tay.md)). Chạy tay N schema là công việc không ai làm đúng được quá vài lần.

### Database riêng cho mỗi tenant — loại

Cách ly tuyệt đối, và có một lợi ích thật: khôi phục dữ liệu cho một tenant không đụng tenant khác.

Loại vì chi phí vận hành không tương xứng ở quy mô này: N chuỗi kết nối, N lịch sao lưu, N lần nâng cấp, và bài toán "một tenant mới cần bao lâu để dựng" trở thành một quy trình thay vì một dòng dữ liệu.

Đây là mô hình đúng khi tenant **ít và rất lớn**, hoặc khi có yêu cầu pháp lý bắt buộc dữ liệu nằm ở nơi tách biệt. Không phải trường hợp này.

### Không làm multi-tenant, dùng giới hạn dữ liệu theo phạm vi — loại

Rẻ hơn và không mang rủi ro rò dữ liệu. Loại vì phép thử ở trên cho kết quả rõ: các đơn vị **độc lập**, không bao giờ tổng hợp xuyên đơn vị. Dùng phạm vi cho một bài toán cách ly là để ngỏ khả năng một truy vấn quên lọc làm lộ dữ liệu của đơn vị khác — mà đó chính là rủi ro lớn nhất ở đây.

### Hoãn sang sau — loại

Đây là phương án ADR bản đầu chọn, và nó **đã bị lật**.

Chi phí làm ngay: khoảng ba đến năm ngày. Chi phí thêm sau: ba đến bốn tuần, cộng một rủi ro không định lượng được — mọi truy vấn đã viết trước đó đều phải rà lại xem có thiếu lọc không, trên một cơ sở dữ liệu đã có dữ liệu thật của nhiều đơn vị.

Chênh lệch đó đủ lớn để quyết định làm ngay, khi chưa có dòng code nào phải sửa.

## Hệ quả

### Tích cực

- Bộ lọc toàn cục nghĩa là **lập trình viên viết truy vấn bình thường** và vẫn đúng — cách ly không phụ thuộc kỷ luật của từng người.
- Một database, một lần migration, một lịch sao lưu.
- Thêm một tenant là thêm một dòng dữ liệu, không phải một quy trình vận hành.
- Mô hình một-tài-khoản-một-tenant bỏ được cả nhóm phức tạp: không màn chọn tenant, không đổi tenant giữa phiên, không phải tính lại quyền khi đổi tenant.

### Tiêu cực — nói thẳng, đây là cái giá thật

- **Cách ly là cách ly LOGIC, không phải vật lý.** Dữ liệu hai đơn vị nằm cùng một bảng. Một lỗi lập trình đủ nặng — hoặc một lần bỏ bộ lọc đặt sai chỗ — làm lộ dữ liệu đơn vị khác. Đây là **rủi ro nghiêm trọng nhất của toàn hệ**, và nó không biến mất, chỉ được canh.
- **Một lần gọi bỏ bộ lọc đặt sai chỗ là một lỗ rò im lặng.** Không có lỗi biên dịch, không có ngoại lệ, truy vấn chạy bình thường và trả về nhiều dữ liệu hơn đáng ra được thấy. Đây là lý do luật M5 tồn tại và vì sao nó cần allowlist chứ không phải lời khuyên.
- **Truy vấn thô (`FromSqlRaw`) không đi qua bộ lọc toàn cục.** Bất kỳ SQL viết tay nào cũng phải tự thêm điều kiện tenant. Trừu tượng hoá rò ra đúng ở chỗ nguy hiểm nhất.
- **Index duy nhất thiếu `TenantId` là một lỗi khó thấy**: nó không gây rò dữ liệu, nó gây từ chối sai — đơn vị B không tạo được bản ghi mã `X` chỉ vì đơn vị A đã dùng mã đó. Người dùng báo "hệ thống nói mã trùng nhưng tôi tìm không thấy".
- Khôi phục dữ liệu cho một tenant khó hơn hẳn mô hình database riêng — phải lọc theo `TenantId` khi phục hồi.
- Email không còn là định danh duy nhất toàn hệ: hai đơn vị có thể có cùng một địa chỉ email. Duy nhất phải tính theo cặp `(TenantId, Email)`, và điều đó ảnh hưởng tới luồng quên mật khẩu.

### Điều kiện để lật quyết định này

Viết ADR mới nếu **bất kỳ** điều nào sau xảy ra:

- Xuất hiện yêu cầu báo cáo tổng hợp xuyên tenant → mô hình phải đổi sang phân cấp, không phải thêm một ngoại lệ.
- Một tài khoản cần thuộc nhiều tenant.
- Có yêu cầu pháp lý bắt dữ liệu một đơn vị nằm ở hạ tầng tách biệt.
- Một tenant lớn tới mức làm chậm truy vấn của tenant khác.

## Đã lật một phần

[`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) mở một ngoại lệ **hẹp**: tài khoản vận hành thấy được **danh sách đơn vị**. Cách ly **dữ liệu nghiệp vụ** ở đây không đổi.

## Luật đi kèm

Các luật `M*` ở [`../RULES.md`](../RULES.md) §9 ép quyết định này. Chi tiết thi công: [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md); schema: [`../database/schema-core.md`](../database/schema-core.md).
