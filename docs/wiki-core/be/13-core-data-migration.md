---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 13. Migration và dữ liệu Core

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`, chưa có migration nào.
>
> Chính sách sở hữu migration ở dạng luật: [`../../database/migration-policy.md`](../../database/migration-policy.md). Thứ tự chạy script và cách phát hiện DB lệch model: [`../../database/script-runbook.md`](../../database/script-runbook.md). Lược đồ bảng `core`: [`../../database/schema-core.md`](../../database/schema-core.md).
>
> File này giữ **lý do và các đánh đổi**.

---

## 1. Ai sở hữu migration — quyết định đảo ngược

### 1.1 Quyết định

> 📖 **Luật sở hữu migration: đọc [`../../database/migration-policy.md`](../../database/migration-policy.md) §1** — đó là file chủ. Quyết định gốc: [`../../adr/0008-core-so-huu-migration.md`](../../adr/0008-core-so-huu-migration.md).

Đây là **đảo ngược** so với dự án tiền nhiệm, nơi project host sở hữu toàn bộ migration của mọi phần.

### 1.2 Vì sao đảo

Mục tiêu tồn tại của repo này là: **mang Core sang một dự án mới và dùng được ngay**.

Với mô hình cũ (host sở hữu tất cả), một dự án mới phải:

1. Tự sinh lại toàn bộ migration cho các bảng của Core — người dùng, vai trò, quyền, menu, outbox, nhật ký kiểm toán.
2. Tự giữ cho lược đồ đó **khớp** với mô hình dữ liệu mà Core kỳ vọng.
3. Và làm lại việc đó ở **mỗi** dự án.

Ba bước trên đảm bảo một điều: các dự án sẽ lệch nhau. Không phải vì ai làm ẩu, mà vì việc sinh lại một lược đồ bằng tay, nhiều lần, ở nhiều nơi, không thể ra kết quả giống nhau.

Và khi Core nâng cấp — thêm một cột, đổi một chỉ mục — không có đường nào để thay đổi đó tới các dự án. Mỗi dự án phải tự phát hiện và tự làm lại.

Với mô hình mới, migration của `core` **đi cùng Core**. Dự án mới tham chiếu Core, chạy migration của Core, và có đúng lược đồ mà Core cần. Nâng cấp Core mang theo migration mới.

### 1.3 Cái giá — ghi thẳng, không giấu

> **`Core.Infrastructure` từ nay biết provider là PostgreSQL.**

Migration là mã sinh ra cho **một** provider cụ thể: kiểu cột, cú pháp chỉ mục, hàm mặc định đều khác nhau giữa các hệ quản trị dữ liệu. Chứa migration nghĩa là chứa tri thức về provider.

| Cái mất | Cái được |
| --- | --- |
| Core không còn trung lập về provider. Đổi sang hệ quản trị dữ liệu khác là viết lại toàn bộ migration của `core` | Dự án mới thừa kế được bảng Core, không phải sinh lại |
| `Core.Infrastructure` kéo theo gói thư viện của provider | Nâng cấp Core mang theo thay đổi lược đồ |
| Test của Core cần một PostgreSQL thật | Không còn khả năng các dự án lệch lược đồ |

**Đánh giá cái mất:** khả năng đổi provider là thứ **gần như không bao giờ được dùng** ở hệ tầm trung, và khi nó xảy ra thì migration cũng không phải phần đắt nhất — mọi truy vấn đặc thù, mọi kiểu dữ liệu, mọi tối ưu đều phải xem lại. Đổi một khả năng gần như lý thuyết lấy một lợi ích xảy ra ở **mỗi** dự án là đổi đúng.

**Điều này không nới lỏng luật A2** ([`../../RULES.md`](../../RULES.md)): `Core.Application` vẫn không được biết EF Core hay provider nào. Tri thức về provider dừng lại ở `Core.Infrastructure`.

📖 [`../../adr/0008-core-so-huu-migration.md`](../../adr/0008-core-so-huu-migration.md)

---

## 2. Schema theo module

### 2.1 Mô hình

| Phía | Schema | Migration nằm ở |
| --- | --- | --- |
| Core | `core` | `Core.Infrastructure` |
| Module X | tên module | `Modules.<X>.Infrastructure` |

Mỗi phía có ngữ cảnh dữ liệu riêng và lịch sử migration riêng. Hai lịch sử **không** trộn vào một bảng lịch sử chung — trộn thì thứ tự áp phụ thuộc thứ tự sinh, và hai module phát triển song song sẽ chèn migration vào giữa lịch sử của nhau.

### 2.2 Cấm khoá ngoại vật lý xuyên schema

Luật E5 ở [`../../RULES.md`](../../RULES.md). Lý do đầy đủ ở [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §5; phần liên quan tới migration là:

| Nếu có khoá ngoại xuyên schema | Hệ quả |
| --- | --- |
| Migration của module phụ thuộc **thứ tự** với migration của Core | Không áp độc lập được nữa |
| Tách module thành dịch vụ riêng | Không tách được mà không sửa lược đồ trên dữ liệu thật |
| Xoá một bản ghi Core | Bị chặn bởi dữ liệu của một module mà Core không biết tồn tại |

**Thay thế:** tham chiếu bằng định danh, và tính toàn vẹn do ứng dụng giữ. Đây là cái mất thật — DB không còn canh hộ — và phải bù bằng hai thứ:

1. Kiểm tra tồn tại ở tầng ứng dụng khi tạo tham chiếu.
2. Một job đối soát định kỳ tìm tham chiếu trỏ vào hư không. Không có job này thì dữ liệu mồ côi tích tụ âm thầm.

Và **luôn đánh chỉ mục cho cột tham chiếu** — PostgreSQL không tự tạo chỉ mục cho chúng, xem [`../../quy-uoc/be-performance.md`](../../quy-uoc/be-performance.md) §5.

---

## 3. Áp lược đồ — chạy tay, nhưng có kỷ luật

Quyết định của repo: **không tự động áp migration lúc khởi động**.

### 3.1 Vì sao không tự động áp

| Lý do | Chi tiết |
| --- | --- |
| Nhiều instance cùng khởi động | Cùng chạy migration một lúc → tranh chấp, hoặc lược đồ ở trạng thái nửa vời |
| Migration hỏng lúc khởi động | App không lên được, và lược đồ đã thay đổi một phần — trạng thái tệ nhất để xử lý |
| Không có cơ hội xem trước | Không ai thấy câu lệnh sẽ chạy trước khi nó chạy trên dữ liệu thật |
| Không quay lui được | Việc quay lui phải là một quyết định của con người |

### 3.2 Nhưng "chạy tay" không có nghĩa là tuỳ tiện

Ba thứ bắt buộc:

1. **Đường dẫn cố định và thứ tự xác định** cho các script — ai chạy cũng ra cùng kết quả. Xem [`../../database/script-runbook.md`](../../database/script-runbook.md).
2. **Cơ chế phát hiện DB lệch model.** App phải **từ chối khởi động** khi còn migration chưa áp. Luật E8 ở [`../../RULES.md`](../../RULES.md), kèm test `Startup_Fails_When_PendingMigrationsExist`.
3. **Readiness thất bại** trong cùng tình huống, để bộ điều phối không gửi request tới. Xem [`07-observability.md`](07-observability.md) §8.

Điểm 2 là thứ biến "chạy tay" từ một rủi ro thành một quy trình: quên chạy thì app không lên, thay vì app lên rồi hỏng ở truy vấn đầu tiên chạm cột chưa tồn tại — lỗi xảy ra muộn hơn nhiều, ở một chỗ không liên quan, với một thông điệp không gợi ý gì về nguyên nhân.

📖 [`../../adr/0009-ap-schema-chay-tay.md`](../../adr/0009-ap-schema-chay-tay.md)

---

## 4. Thay đổi lược đồ an toàn — mở rộng trước, thu hẹp sau

Khi hệ thống đã có dữ liệu và người dùng thật, một thay đổi lược đồ phá vỡ tương thích sẽ làm hỏng phiên bản ứng dụng đang chạy trong khoảng thời gian giữa lúc áp lược đồ và lúc mã mới lên.

Cách chuẩn là chia thành các bước, mỗi bước tương thích với cả mã cũ lẫn mã mới.

### Ví dụ: đổi tên một cột

| Bước | Việc | Mã cũ còn chạy? |
| --- | --- | --- |
| 1 | Thêm cột mới, cho phép rỗng | ✅ |
| 2 | Mã mới ghi **cả hai** cột, đọc cột mới | ✅ |
| 3 | Chuyển dữ liệu cũ sang cột mới | ✅ |
| 4 | Mã ngừng ghi cột cũ | ✅ |
| 5 | Xoá cột cũ | — |

Chậm hơn "đổi tên cột" một dòng, nhưng không có khoảng thời gian nào hệ thống hỏng.

### Bảng tra nhanh

| Thao tác | An toàn? | Ghi chú |
| --- | --- | --- |
| Thêm cột cho phép rỗng | ✅ | |
| Thêm cột **không rỗng** có giá trị mặc định | ⚠️ | Trên bảng lớn có thể viết lại toàn bộ bảng và khoá lâu; kiểm tra hành vi của phiên bản DB đang dùng |
| Thêm chỉ mục | ⚠️ | Dùng chế độ tạo không khoá bảng trên môi trường thật |
| Xoá cột | ❌ | Chỉ sau khi chắc chắn không mã nào còn đọc |
| Đổi tên cột / bảng | ❌ | Dùng quy trình năm bước ở trên |
| Thu hẹp kiểu dữ liệu | ❌ | Có thể mất dữ liệu |
| Thêm ràng buộc không rỗng | ❌ | Chỉ sau khi đã lấp hết giá trị rỗng |

---

## 5. Migration cho dữ liệu, không chỉ cho lược đồ

Có ba loại thay đổi dữ liệu, và chúng phải được đối xử khác nhau.

### 5.1 Dữ liệu tham chiếu (seed)

Dữ liệu mà **hệ thống không chạy được nếu thiếu**: danh mục quyền của Core, cây menu của Core.

| Quy tắc | Vì sao |
| --- | --- |
| Đi cùng migration | Nó là một phần của định nghĩa hệ thống, không phải dữ liệu người dùng |
| **Ghi có điều kiện** — có rồi thì bỏ qua, chưa có thì thêm | Migration có thể chạy trên DB đã có dữ liệu |
| **Không xoá tự động** những mục không còn trong danh sách | Một khoá quyền biến mất làm mọi dòng phân quyền trỏ tới nó thành rác âm thầm. Đánh dấu ngừng dùng, dọn bằng một bước có chủ đích |
| Định danh **cố định**, không sinh ngẫu nhiên | Sinh ngẫu nhiên thì mỗi môi trường một giá trị, và không tham chiếu chéo được |

Dòng cuối rẻ khi làm từ đầu và rất đắt khi sửa sau — lúc đó dữ liệu thật đã trỏ vào các định danh khác nhau ở mỗi môi trường.

### 5.2 Dữ liệu bootstrap

Tài khoản đầu tiên, để có người đăng nhập được vào một hệ thống vừa cài. Xem [`02-identity-auth.md`](02-identity-auth.md) §3.6.

| Quy tắc | Vì sao |
| --- | --- |
| **Không** nằm trong migration chạy ở môi trường thật | Một migration tạo tài khoản có mật khẩu biết trước là một cửa sau đi theo mọi lần triển khai |
| Là một **lệnh riêng**, chạy có chủ đích khi cài đặt | Người vận hành quyết định thời điểm và mật khẩu |
| Bắt buộc đổi mật khẩu ở lần đăng nhập đầu | |
| Ghi vào nhật ký kiểm toán | |
| Ở môi trường phát triển, **dữ liệu** được phép seed sẵn cho tiện; **tài khoản thì không** | Dữ liệu đi bằng một tệp `.sql`, tài khoản đi bằng chính lệnh bootstrap với mật khẩu từ `user-secrets` — [`../../adr/0022-seed-dev-khong-co-duong-code-rieng.md`](../../adr/0022-seed-dev-khong-co-duong-code-rieng.md). Không có đường code nào chứa sẵn mật khẩu, nên không cần rào theo môi trường |

### 5.3 Lấp dữ liệu cũ (backfill)

Điền giá trị cho một cột mới trên dữ liệu đã có.

> **Backfill lớn KHÔNG được nằm trong migration.**

| Vì sao | Chi tiết |
| --- | --- |
| **Migration chạy trong một transaction** | Cập nhật hàng triệu dòng trong một transaction khoá bảng, làm phình nhật ký ghi của DB, và có thể hết thời gian chờ |
| **Không có tiến độ, không dừng được** | Chạy 40 phút rồi thất bại là mất trắng, phải chạy lại từ đầu |
| **Chặn việc triển khai** | Không ai dám triển khai khi bước áp lược đồ mất hàng chục phút |
| **Không quay lui được** | Quay lui một thay đổi lược đồ thì dễ; quay lui một thao tác dữ liệu đã chạy nửa chừng thì không |

**Cách đúng:**

1. Migration chỉ thêm cột, cho phép rỗng. Nhanh, an toàn.
2. Backfill là một **script hoặc job riêng**: chạy theo lô, có tiến độ, dừng và chạy tiếp được, chạy lại được mà không hỏng.
3. Khi backfill xong và đã kiểm chứng, một migration sau mới thêm ràng buộc không rỗng.

**Ngưỡng "lớn":** không phải một con số cố định. Phép thử là — *chạy trên bản sao dữ liệu thật thì mất bao lâu?* Vượt quá thời gian chấp nhận được cho một lần triển khai thì tách ra.

Và luôn: **thử trên bản sao dữ liệu thật trước.** Backfill chạy đúng trên vài trăm dòng thử nghiệm không nói lên gì về hàng triệu dòng.

---

## 6. Quay lui

| Loại | Quay lui được? | Cách |
| --- | --- | --- |
| Thêm cột cho phép rỗng | ✅ Dễ | Xoá cột |
| Thêm chỉ mục | ✅ Dễ | Xoá chỉ mục |
| Xoá cột | ❌ | Dữ liệu đã mất. Chỉ phục hồi được từ bản sao lưu |
| Backfill | ❌ | Giá trị cũ không còn |

Vì phần lớn thao tác không quay lui được, **cách phòng thủ thật không phải là kịch bản quay lui, mà là quy trình mở rộng trước / thu hẹp sau ở §4** — nó đảm bảo mỗi bước đều tương thích ngược, nên hiếm khi cần quay lui.

Với thay đổi có rủi ro, thứ tự đúng là: sao lưu → thử trên bản sao dữ liệu thật → áp trong khung giờ ít người dùng → theo dõi.

---

## 6.1 Review một migration — sáu câu hỏi

Migration là loại thay đổi khó sửa nhất sau khi đã chạy, nên nó xứng đáng được xem kỹ hơn code thường:

1. **Nó có khoá bảng lâu không?** Trên bảng lớn, một thao tác trông vô hại có thể khoá hàng phút.
2. **Nó có xoá gì không?** Cột, bảng, ràng buộc — mọi thứ xoá đều không lấy lại được.
3. **Mã của phiên bản đang chạy còn hoạt động sau khi áp không?** Nếu không, cần chia bước theo §4.
4. **Nó có chạy thao tác dữ liệu lớn không?** Nếu có, tách ra theo §5.3.
5. **Nó có tạo khoá ngoại xuyên schema không?** Luật E5 cấm — và ArchTest bắt, nhưng bắt sau khi đã viết.
6. **Chỉ mục mới có được tạo ở chế độ không khoá bảng không?**

Sáu câu này nên nằm trong danh mục kiểm khi review, không phụ thuộc trí nhớ của người xem.

### Đặt tên migration

Tên migration là thứ người ta đọc khi tìm *"thay đổi này đến từ đâu"*, thường là vào lúc đang xử lý sự cố. Tên phải mô tả **thay đổi**, không mô tả **lý do** và không mô tả **mã ticket**:

| Tên tốt | Tên kém |
| --- | --- |
| `AddLockoutColumnsToUsers` | `Fix123` |
| `CreatePermissionCatalogTable` | `Update` |
| `MakeUserEmailUniqueAmongActive` | `NewChanges2` |

---

## 6.2 Nhiều môi trường

Thứ tự áp là cố định và không có ngoại lệ: môi trường phát triển → môi trường thử → môi trường thật. Một migration chưa từng chạy ở môi trường thử thì không được chạy ở môi trường thật.

| Yêu cầu | Vì sao |
| --- | --- |
| Môi trường thử có **cấu trúc dữ liệu giống** môi trường thật | Migration chạy đúng trên DB rỗng không nói lên gì |
| Đo thời gian chạy ở môi trường thử, trên khối lượng dữ liệu tương đương | Đây là con số quyết định có phải tách backfill ra không |
| Ghi lại phiên bản lược đồ của từng môi trường | Câu hỏi *"môi trường này đang ở đâu"* phải trả lời được trong vài giây |

**Không bao giờ sửa một migration đã áp ở bất kỳ môi trường nào.** Sửa nó thì các môi trường đã áp bản cũ sẽ không bao giờ nhận được thay đổi, và lịch sử migration của chúng vĩnh viễn khác nhau. Cần sửa thì viết một migration mới.

---

## 7. Cửa thoát hiểm

Có những tình huống phải chạm thẳng vào DB môi trường thật: dữ liệu hỏng do một lỗi đã sửa, một bản ghi kẹt chặn cả hệ, một tài khoản quản trị bị khoá hết.

Đây là **lưới an toàn cuối**, không phải quy trình vận hành. Điều kiện:

| Điều kiện | Vì sao |
| --- | --- |
| Có người thứ hai xem lại câu lệnh trước khi chạy | Câu lệnh cập nhật thiếu mệnh đề điều kiện là tai nạn kinh điển |
| Chạy trong transaction, kiểm số dòng ảnh hưởng **trước khi** kết thúc | Số dòng khác dự kiến thì huỷ |
| Ghi lại: ai, khi nào, câu lệnh gì, vì sao | |
| Có mục tương ứng ở [`../../audit/`](../../audit/) nếu đây là hậu quả của một sự cố | Để lần sau có cổng chặn |

**Dùng cửa thoát hiểm quá một lần cho cùng một nguyên nhân nghĩa là thiếu một tính năng**, không phải thiếu kỷ luật vận hành.

---

## 8. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Core sở hữu migration schema `core` | 📐 quyết định đã chốt | [`../../adr/0008-core-so-huu-migration.md`](../../adr/0008-core-so-huu-migration.md) |
| Mỗi module sở hữu migration của mình | ✅ sẽ có | Luật E6 |
| Lịch sử migration tách riêng theo phía | ✅ sẽ có | Không trộn vào một bảng lịch sử chung |
| Cấm khoá ngoại vật lý xuyên schema | 📐 luật | Luật E5 |
| App từ chối khởi động khi còn migration chưa áp | ✅ sẽ có | Luật E8 |
| Áp lược đồ chạy tay, có đường dẫn và thứ tự cố định | ✅ sẽ có | [`../../database/script-runbook.md`](../../database/script-runbook.md) |
| Seed danh mục quyền và menu của Core | ✅ sẽ có | Định danh cố định, ghi có điều kiện, không xoá tự động |
| Lệnh bootstrap riêng cho tài khoản đầu tiên | ✅ sẽ có | Không nằm trong migration của môi trường thật |
| **Job đối soát tham chiếu xuyên schema** | ❌ chưa | Cần khi có module thứ hai |
| **Tự động áp migration lúc khởi động** | ❌ **không làm** | Lý do ở §3.1 — đây là quyết định về hướng |
| **Backfill trong migration** | ❌ **không làm** | Backfill lớn là script riêng, chạy theo lô |
| **Công cụ so sánh lược đồ giữa các môi trường** | ❌ chưa | Đáng có khi số môi trường tăng |
| **Tệp seed chứa sẵn chuỗi băm mật khẩu của một tài khoản** | ❌ **loại, không hoãn** `K50` | Chuỗi băm chính là mật khẩu, và nó vào git vĩnh viễn — xoá tệp về sau không xoá khỏi lịch sử. [`../../adr/0022-seed-dev-khong-co-duong-code-rieng.md`](../../adr/0022-seed-dev-khong-co-duong-code-rieng.md) |
