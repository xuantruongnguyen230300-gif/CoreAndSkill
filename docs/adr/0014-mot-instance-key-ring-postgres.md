---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0014 — Chạy một instance; bộ khoá bảo vệ dữ liệu để trong PostgreSQL; chưa dùng Redis

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

**Instance** ở đây nghĩa là một tiến trình ứng dụng đang chạy. Chạy ba bản sao sau một bộ cân bằng tải là ba instance của cùng ứng dụng.

Con số đó không phải chi tiết vận hành — nó đổi thiết kế, vì **mỗi instance có bộ nhớ riêng**. Cái gì nằm trong RAM của instance A thì instance B không thấy.

Hệ này xác thực bằng cookie với ASP.NET Core Identity ([`0004-giu-aspnet-identity.md`](0004-giu-aspnet-identity.md)). Cookie xác thực được mã hoá bằng một **bộ khoá bảo vệ dữ liệu** (data protection key ring). Mặc định bộ khoá đó nằm trong thư mục cục bộ của chính instance sinh ra nó.

Hệ quả nếu chạy nhiều instance mà không xử lý:

> Instance A mã hoá cookie bằng khoá của nó. Request tiếp theo của cùng người dùng đi vào instance B. B không có khoá đó, không giải mã được, coi như chưa đăng nhập.
>
> Triệu chứng người dùng thấy: **"thỉnh thoảng tự nhiên bị đăng xuất"**. Không có bản ghi lỗi nào. Không tái hiện được trên máy phát triển, vì máy phát triển chỉ chạy một instance.

Hai chỗ hỏng cùng họ: bộ đếm giới hạn tần suất mặc định nằm trong bộ nhớ nên N instance thành N bộ đếm riêng và ngưỡng thật bị nhân lên N lần; bộ nhớ đệm trong tiến trình thì mỗi instance giữ một bản dữ liệu lệch nhau.

## Quyết định

Ba phần, tách riêng vì chúng có vòng đời khác nhau.

### 1. Vận hành bằng MỘT instance ở v1

Một tiến trình .NET phục vụ thoải mái một ứng dụng quản trị nội bộ ở quy mô đơn vị hành chính. Trần thực tế sẽ chạm ở thiết kế truy vấn trước khi chạm ở số tiến trình.

Và khi chỉ có một instance thì **toàn bộ nhóm vấn đề trên không tồn tại**.

### 2. NHƯNG bộ khoá bảo vệ dữ liệu để trong PostgreSQL ngay từ đầu

Đây là khoản đầu tư duy nhất trả trước, và nó rẻ: khoảng hai chục dòng cấu hình cộng một bảng.

Lý do làm ngay dù chưa cần:

- Ngày thêm instance thứ hai **không phải sửa gì** — đó là một quyết định vận hành, thường được đưa ra dưới áp lực, và không nên kèm theo một thay đổi mã nguồn.
- Bộ khoá lưu trên đĩa cục bộ **mất khi container được tạo lại**. Ngay cả một instance, chạy trong container không có ổ đĩa bền, cũng đá toàn bộ người dùng ra mỗi lần triển khai lại.
- Loại lỗi này chỉ lộ ra ở môi trường thật và mang triệu chứng gây hiểu nhầm. Chi phí phòng bằng một phần nhỏ chi phí chẩn đoán.

### 3. CHƯA dùng Redis

Không phải vì Redis dở. Nó nhẹ, ổn định, chi phí thấp, và hoàn toàn phù hợp hệ tầm trung.

Mà vì thêm một dịch vụ nghĩa là thêm một thứ phải cài, cấu hình, giám sát, sao lưu, nâng cấp, và xử lý khi nó chết — **chi phí trả mãi mãi**, trong khi lợi ích hiện chưa đo được.

Năm việc người ta dùng Redis, và cái nào PostgreSQL làm thay được:

| Việc | PostgreSQL thay được | Ghi chú |
| --- | --- | --- |
| Bộ khoá bảo vệ dữ liệu dùng chung | **Được** | Đã chọn cách này |
| Bộ đếm giới hạn tần suất dùng chung | **Được** | Chậm hơn, không đáng kể ở quy mô này |
| Khoá phân tán cho job nền | **Được** | Dùng khoá tư vấn của PostgreSQL |
| Bộ nhớ đệm dữ liệu nóng | Kém hơn rõ rệt | Đây mới là lý do thật để thêm Redis |
| Phát tán sự kiện giữa các instance | Được nhưng vụng | Chưa có nhu cầu |

Ba việc đầu — những việc sẽ cần trước — PostgreSQL làm được. Việc thứ tư là lý do chính đáng duy nhất, và nó chỉ chính đáng **sau khi đo được** một truy vấn nóng thật.

Core vẫn khai `ICacheStore` với bản cài đặt trong bộ nhớ. Đổi sang Redis sau là đổi **một dòng đăng ký phụ thuộc**, không sửa chỗ gọi.

## Phương án đã cân nhắc và vì sao loại

### Nhiều instance ngay từ đầu — loại

Được: sẵn sàng chịu tải, không chết cả hệ khi một máy hỏng, cập nhật không phải dừng.

Loại vì trả chi phí phức tạp cho một nhu cầu **chưa tồn tại**: cần bộ cân bằng tải, cần mọi trạng thái ra khỏi tiến trình, cần theo dõi phân tán để lần một request đi qua máy nào. Ở quy mô này, thời gian đó dùng cho việc khác có ích hơn.

### Thêm Redis ngay để "sau này khỏi sửa" — loại

Đây đúng khuôn lập luận mà [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 bác: dự đoán nhu cầu tương lai. Và nó tự mâu thuẫn — nếu Core đã khai `ICacheStore` thì việc "sau này phải sửa" vốn đã được giải, nên lý do thêm sớm không còn.

### Bộ khoá để trên đĩa cục bộ (mặc định) — loại

Rẻ nhất, không cấu hình gì. Loại vì hai lý do ở §2: mất khi container tạo lại, và biến việc thêm instance thành một thay đổi mã nguồn.

### Bộ khoá trên thư mục chia sẻ mạng — loại

Hoạt động được và không cần thêm dịch vụ. Loại vì kéo theo một phụ thuộc hạ tầng khó thấy: quyền truy cập tệp, độ trễ mạng, hành vi khi thư mục không truy cập được. PostgreSQL đã có sẵn, đã được sao lưu, đã được giám sát.

## Hệ quả

### Tích cực

- Số dịch vụ phải vận hành ở v1: **hai** — ứng dụng và PostgreSQL. Dựng môi trường mới nhanh, người mới hiểu được toàn bộ.
- Thêm instance thứ hai sau này là quyết định thuần vận hành.
- Không có bộ nhớ đệm nghĩa là **không có bài toán vô hiệu hoá bộ nhớ đệm** — nhóm lỗi khó nhất trong nhóm này, và nó chưa tồn tại.

### Tiêu cực

- **Một instance là một điểm hỏng duy nhất.** Tiến trình chết thì hệ ngừng. Chấp nhận được cho ứng dụng nội bộ có cửa sổ bảo trì; **không** chấp nhận được nếu có cam kết mức độ sẵn sàng.
- **Cập nhật phiên bản có thời gian gián đoạn.** Ngắn, nhưng có thật.
- Bộ khoá trong PostgreSQL nghĩa là **cơ sở dữ liệu chết thì không ai đăng nhập được** — kể cả trang không cần dữ liệu. Đây là một phụ thuộc mới, có thật, và đáng nêu.
- **Không có bộ nhớ đệm nghĩa là mọi truy vấn đều chạm cơ sở dữ liệu.** Với dữ liệu đọc rất nhiều như ma trận quyền và menu, đây là chỗ sẽ chậm trước tiên. Cần đo, không đoán.

### Điều kiện xem lại

- Có cam kết mức độ sẵn sàng, hoặc không chấp nhận gián đoạn khi cập nhật → thêm instance. Bộ khoá đã sẵn sàng, chỉ cần chuyển bộ đếm giới hạn tần suất sang dùng chung.
- Đo được một truy vấn nóng thật mà chỉ mục không giải quyết được → thêm Redis làm bộ nhớ đệm.
- Cần đẩy sự kiện thời gian thực tới nhiều instance → thêm Redis.

**Đo trước, đừng đoán.** Xem [`../wiki-core/be/11-performance-caching.md`](../wiki-core/be/11-performance-caching.md).
