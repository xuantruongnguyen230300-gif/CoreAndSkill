---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0020 — Seed dev đi bằng lệnh riêng, chạy sau ba điều kiện đồng thời, và để lại dấu nhận dạng cố định

> **Trạng thái:** Đã thay thế bởi ADR-0022 (2026-09-12)
>
> Thay thế bởi [`0022-seed-dev-khong-co-duong-code-rieng.md`](0022-seed-dev-khong-co-duong-code-rieng.md).

## Bối cảnh

[`../wiki-core/be/13-core-data-migration.md`](../wiki-core/be/13-core-data-migration.md) §5.2 cho phép seed sẵn tài khoản ở môi trường phát triển, kèm đúng một ràng buộc: *"phân biệt bằng môi trường, không bằng cờ trong code"*. Câu đó nói **không được làm gì** — không hằng số biên dịch. Nó không nói **bao nhiêu hàng rào thì đủ**.

Cùng lúc, [`../database/script-runbook.md`](../database/script-runbook.md) §6 chốt chiều ngược lại cho đường cài đặt thật: thiếu secret thì lệnh seed dừng và không ghi dòng nào, vì *"sinh một mật khẩu mặc định để cho tiện là cách nhanh nhất để một mật khẩu mặc định đi thẳng lên production"*. Hai câu này chưa bao giờ được đặt cạnh nhau, nên ranh giới giữa chúng bỏ trống.

Nhu cầu thì có thật, và nó lớn hơn trước. Từ [`0013-multi-tenant.md`](0013-multi-tenant.md) và [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md), một môi trường dev **dùng được** cần tối thiểu hai tài khoản thuộc hai vai khác hẳn nhau: một tài khoản vận hành hệ thống để tạo đơn vị, và một quản trị của một đơn vị nghiệp vụ để làm phần còn lại. Không có seed, tài khoản thứ hai chỉ ra đời **sau khi** tài khoản thứ nhất đã đăng nhập được, và mỗi người phải tự đặt rồi tự nhớ hai mật khẩu trên từng máy.

Ràng buộc có thật lúc quyết định: chưa có `src/`, chưa có dữ liệu ở bất kỳ đâu, và biến môi trường của nền tảng hiện là **thứ duy nhất** phân biệt môi trường trong toàn bộ tài liệu.

## Quyết định

1. Seed dev đi bằng **một lệnh mang tên riêng**, không phải một tham số thêm vào lệnh cài đặt thật. Tên lệnh cài đặt thật không được là một dạng gõ thiếu của tên lệnh seed dev.
2. Lệnh seed dev **từ chối cứng** — thoát với mã khác 0, không ghi dòng nào — trừ khi **cả ba** điều kiện cùng đúng:
   - môi trường đang chạy là Development;
   - máy chủ database trong chuỗi kết nối là chính máy đang chạy lệnh;
   - bảng người dùng của Core **không có dòng nào**.
3. Mọi hàng dữ liệu do lệnh đó tạo mang **định danh cố định**, khai ở đúng một chỗ. Ở mọi môi trường không phải Development, tiến trình **từ chối khởi động** khi thấy bất kỳ định danh nào trong danh sách đó, và endpoint sẵn sàng trả đỏ khi dấu xuất hiện lúc đang chạy.
4. Chính sách mật khẩu **giống hệt nhau ở mọi môi trường**. Mật khẩu dev là một chuỗi thoả chính sách thật, **công bố** trong runbook — không cất trong kho bí mật, vì giả vờ nó là bí mật còn tệ hơn thừa nhận nó công khai.

## Vì sao ba điều kiện chứ không phải một

Mỗi điều kiện bắt một ca hỏng khác nhau. Bảng dưới là lý do cả ba cùng phải có — và cũng là lý do không thêm điều kiện thứ tư.

| Ca hỏng | Đk 1 — môi trường | Đk 2 — database cùng máy | Đk 3 — bảng người dùng rỗng |
| --- | --- | --- | --- |
| Chạy trên máy chạy thật, biến môi trường khai đúng | **chặn** | không chặn | không chặn nếu là bản cài mới |
| Chạy trên máy lập trình viên, chuỗi kết nối trỏ vào database thật | không chặn | **chặn** | **chặn** |
| Chạy nhầm trên database dev đã có dữ liệu | không chặn | không chặn | **chặn** |
| Máy chạy thật, biến môi trường bị đặt nhầm thành Development, database cùng máy, bản cài mới | không chặn | không chặn | không chặn |

**Hàng cuối là lỗ còn lại, và nó không đóng được bằng cách thêm một điều kiện đọc từ tiến trình.** Lý do: mọi hàng rào khoá theo tên môi trường đều sai **cùng lúc** khi tên môi trường sai — kể cả hàng rào khởi động ở điểm 3, vì nó cũng tự tắt khi môi trường là Development. Đây là một tương quan, không phải ba lỗi độc lập. Nói rõ nó ra là điều kiện để sau này có người đóng được nó — xem phương án D.

Cái đỡ lại, và cần biết rõ rằng nó **không phải cổng**: một máy chạy thật bị đặt nhầm thành Development đồng thời bật trang lỗi chi tiết, bật tài liệu API, và tắt kiểm tra cấu hình lúc khởi động ([`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §4.3). Đó là bốn triệu chứng dễ thấy chứ không phải một hàng rào — người ta phải **nhìn** thì nó mới có tác dụng.

## Dấu nhận dạng là định danh, không phải mật khẩu

Hàng rào ở điểm 3 hỏi *"có dòng nào mang định danh này không"*, không hỏi *"có ai đang dùng mật khẩu này không"*. Ba lý do:

| Lý do | Chi tiết |
| --- | --- |
| Không kéo mật khẩu dev vào đường kiểm | Kiểm theo mật khẩu thì phải băm thử chuỗi đó với salt của từng dòng — vừa đắt, vừa buộc chuỗi đó phải nằm trong đường xử lý của môi trường thật |
| Không báo sai | Một mã đơn vị do người gõ có thể trùng một chuỗi quy ước; một định danh cố định thì không ai gõ trúng. Khoá phép dò vào mã đơn vị là tự đặt một quả mìn hẹn giờ dưới chân khu quản trị đơn vị |
| Đúng luật seed đã có | Dữ liệu seed dùng định danh cố định chứ không sinh ngẫu nhiên — [`../wiki-core/be/13-core-data-migration.md`](../wiki-core/be/13-core-data-migration.md) §5.1 |

Hàng rào này **cộng thêm** vào phép đếm sẵn có ở [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §3.6 — *"health check cảnh báo khi số tài khoản mang cờ vượt ngưỡng đã khai"* — chứ không thay nó. Hai phép hỏi hai câu khác nhau: phép đếm hỏi *"có quá nhiều tài khoản đặc quyền không"*, phép dò định danh hỏi *"có đúng những tài khoản này không"*. Một tài khoản dev lọt lên qua bản sao lưu làm cả hai cùng đỏ; một tài khoản đặc quyền do người vận hành tự tạo chỉ làm phép đếm đỏ.

> **Ngưỡng của phép đếm không còn là một con số cố định.** Từ [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md), mỗi đơn vị nghiệp vụ sinh ra một tài khoản quản trị mang cờ bỏ qua kiểm quyền, nên số tài khoản mang cờ tăng theo số đơn vị. Ngưỡng phải khai theo quan hệ đó, không theo một hằng số — khai hằng số thì nó đỏ oan ở đơn vị thứ N, và sẽ bị ai đó nâng lên cho hết đỏ.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Chỉ dựa vào biến môi trường, không thêm gì

**Được:** không thêm dòng nào; đúng nguyên văn câu ở 13 §5.2; không có chi phí vận hành mới.

**Mất:** một biến chuỗi do chính người chạy lệnh đặt là **một** hàng rào, và nó nằm sai chỗ so với hai ca hỏng có thật nhất — chuỗi kết nối trỏ nhầm, và chạy lại trên database đã có dữ liệu. Cả hai ca đó xảy ra trên một máy lập trình viên, nơi biến môi trường **đang** là Development một cách hoàn toàn hợp lệ.

**Vì sao loại:** phương án này đúng với câu chữ của luật cũ và trượt khỏi mục đích của nó. Câu *"phân biệt bằng môi trường"* được viết để cấm hằng số biên dịch, không phải để tuyên bố một biến chuỗi là đủ.

### Phương án B — Nới chính sách mật khẩu ở Development để dùng đúng `admin` làm mật khẩu

**Được:** đúng thứ người dùng xin; không phải nhớ chuỗi nào khác.

**Mất:** ba thứ, và cả ba đều hỏng im lặng.

1. **Chính sách mật khẩu là thứ đang được kiểm thử, không phải phông nền.** [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.1 chốt rằng phía quyết định là BE còn FE chỉ hiển thị lại yêu cầu. Nới ở dev nghĩa là nhánh từ chối mật khẩu yếu **không bao giờ chạy** trên máy lập trình viên, và câu chữ FE hiển thị được đối chiếu với một bộ luật khác bộ luật thật.
2. **Khoá cấu hình nới lỏng là thứ được chép đi.** Dựng một môi trường mới bằng cách chép tệp cấu hình của môi trường phát triển là việc người ta làm thật. Khi đó cái được chép không gây lỗi, không gây cảnh báo — hệ thống chỉ lặng lẽ nhận mật khẩu yếu, mãi mãi.
3. **Nó mở một trục khác biệt giữa các môi trường** đúng ở chỗ đắt nhất khi lệch: người dùng thật không đặt nổi mật khẩu mà lập trình viên vừa thử xong.

**Vì sao loại:** cái tiện đổi lấy là ghi nhớ **một chuỗi**, một lần, trên mỗi máy. Cái giá là một trục lệch môi trường nằm ngay trong đường xác thực. Đây là tỉ giá tệ nhất trong cả ADR này.

> Phần người dùng thật sự quan tâm — gõ `admin` ở **ô tên đăng nhập** — không bị đụng tới. Tên đăng nhập không đi qua chính sách mật khẩu.

### Phương án C — Tách seed dev sang một artifact riêng, không nằm trong gói triển khai

**Được:** hàng rào mạnh nhất có thể có — không chạy được thứ không có trên đĩa. Chuỗi tên đăng nhập và mật khẩu dev cũng không còn nằm trong binary chạy thật.

**Mất:** nhân đôi phần dựng ngữ cảnh (kết nối database, Identity, băm mật khẩu); thêm một artifact phải build; và toàn bộ sức mạnh của nó đặt lên một việc **phải nhớ** — loại artifact đó khỏi gói triển khai. Ở giai đoạn 1 không có cổng nào canh việc nhớ đó.

**Vì sao loại:** nó đổi một rủi ro có ba hàng rào máy kiểm được lấy một rủi ro có một hàng rào là trí nhớ con người. **Điều kiện xét lại:** khi có cổng đọc được nội dung gói triển khai — cùng loại cổng mà D25 và D26 ở [`../RULES.md`](../RULES.md) §10 đang chờ — phương án này rẻ hơn hẳn và đáng cân nhắc lại.

### Phương án D — Đánh dấu môi trường trong chính database (**chưa chọn**, có điều kiện kích hoạt)

**Được:** phá được đúng cái tương quan ở hàng cuối bảng trên. Dấu đi **theo dữ liệu**: khôi phục bản sao lưu của môi trường thật sang máy dev thì dấu đi theo; trỏ tiến trình dev vào database thật thì gặp dấu thật. Không phụ thuộc biến môi trường nào.

**Mất:** một cột hoặc một bảng mới trong `core` mà người dùng duy nhất hôm nay là chính hàng rào này; thêm một bước bắt buộc lúc cài đặt, và một bước lúc cài đặt là một bước có thể làm sai; cộng câu hỏi *"ai sửa được dấu đó"*, mà bản thân câu đó lại cần một hàng rào nữa.

**Vì sao chưa chọn:** hôm nay nó có đúng một chỗ dùng. Đổi ý thì **rẻ** — thêm một cột khi chưa có dữ liệu thật gần như không mất gì. **Điều kiện chọn:** xảy ra lần đầu một ca biến môi trường sai trên máy không phải máy lập trình viên; **hoặc** số môi trường vượt quá ba; **hoặc** có môi trường thứ hai chứa dữ liệu thật, ví dụ môi trường thử nạp bản sao dữ liệu thật.

### Phương án E — Cảnh báo thay vì từ chối

**Vì sao loại:** một cảnh báo trên lệnh mà người ta chạy hàng chục lần là vô hình từ lần thứ hai. Đây đúng khuôn mà runbook §6 đã bác một lần khi chọn *"cố ý dừng và không ghi dòng nào"*.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| Tên đăng nhập và mật khẩu dev là **hằng số nằm trong artifact chạy ở môi trường thật** | Ai đọc được binary thì biết chúng. Chấp nhận được **chỉ vì** ba điều kiện và hàng rào khởi động làm hai tài khoản đó không tồn tại nổi ở môi trường thật — **không** vì chúng bí mật. Ngày nào ba điều kiện bị nới, câu này thành một cửa sau |
| Lệnh seed dev **không chạy lại được** | Muốn seed lại thì xoá database rồi dựng lại. Rẻ ở dev, và runbook §6 đã nói vậy — nhưng nó ngược với thói quen *"seed phải chạy lại được"* ở 13 §5.1, và chỗ ngược đó là **chủ đích**, không phải sót |
| Không seed dev được vào database nằm trên máy khác | Nhóm dùng chung một Postgres đặt ở máy khác phải đi đường cài đặt thật và tự đặt mật khẩu. Nếu sau này chạy app trong compose với database ở một hostname dịch vụ, điều kiện 2 chặn — lúc đó phải mở **có chủ đích**, không nới bằng một biến môi trường |
| Hai tài khoản dev **không** mang cờ buộc đổi mật khẩu lần đầu | Mang cờ đó thì mật khẩu công bố hết tác dụng ngay lần đăng nhập đầu, và cả ADR này vô nghĩa. Giá phải trả: luồng đổi mật khẩu lần đầu ([`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.3) không còn bị chạm tới trong thao tác hằng ngày, nên nó **phải** được canh bằng integration test — không bằng "rồi sẽ có người thấy" |
| Danh sách định danh cố định phải đồng bộ hai đầu | Trình seed và hàng rào khởi động đọc **cùng một** danh sách. Tách làm hai bản là dựng lại đúng lớp lỗi mà [`../OWNERSHIP.md`](../OWNERSHIP.md) tồn tại để chặn |
| Hàng rào khởi động biến một sự cố **bảo mật** thành một sự cố **sẵn sàng** | Một bản cài thật dính dấu dev không phục vụ được cho tới khi có người xoá dòng đó. Với một instance ([`0014-mot-instance-key-ring-postgres.md`](0014-mot-instance-key-ring-postgres.md)) đó là gián đoạn toàn phần. Đổi có chủ đích: một tài khoản mật khẩu công khai ở môi trường thật là mất dữ liệu của **mọi** đơn vị, và mất không lấy lại được. Hệ quả bắt buộc: thông điệp từ chối phải nêu **đúng định danh cần xoá**, nếu không người vận hành bị khoá ngoài mà không biết gỡ kiểu gì |
| Lỗ tương quan biến môi trường **vẫn còn mở** | Ghi ở bảng trên. Không phương án nào được chọn ở đây đóng nó; phương án D đóng được và đang chờ điều kiện |

### Tích cực

- Hai ca hỏng có thật nhất — chuỗi kết nối trỏ nhầm, chạy lại trên database đã có dữ liệu — bị chặn bởi hai điều kiện không dính gì tới tên môi trường.
- Điều kiện 3 là hàng rào duy nhất **không đọc bất kỳ chuỗi nào mà người chạy lệnh đặt được**.
- Một bản cài thật dính dấu dev không im lặng chạy tiếp; nó dừng và nói ra.
- Chính sách mật khẩu còn đúng một bộ cho mọi môi trường, nên nhánh từ chối mật khẩu yếu được chạy mỗi ngày trên máy lập trình viên.

### Hai hướng bị khoá, không phải hoãn

Hai câu dưới là **hướng đi đã bị bác**, không phải việc xếp sau. Muốn lật thì viết ADR trích mã, theo [`../wiki-core/README.md`](../wiki-core/README.md) §9.1:

1. **Nới chính sách mật khẩu theo môi trường** — dưới mọi hình thức, kể cả "chỉ hạ độ dài tối thiểu".
2. **Seed dev vào một database không nằm trên máy đang chạy lệnh.**

Hai dòng khai mã cho hai hướng này thuộc bảng §Áp dụng của [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) và [`../wiki-core/be/13-core-data-migration.md`](../wiki-core/be/13-core-data-migration.md), và **chưa được thêm**. Mã cấp bằng lệnh ở §9.1, không đoán.

### Điều gì là dấu hiệu quyết định này bắt đầu sai

- Có người xin thêm điều kiện thứ tư, hoặc xin **bỏ** một trong ba. Xin bỏ điều kiện 3 là dấu hiệu mạnh nhất: nó nghĩa là ai đó đang muốn seed dev đè lên dữ liệu đã có.
- Danh sách định danh cố định dài quá ba dòng — nghĩa là seed dev đang phình thành một bộ dữ liệu mẫu, và bộ dữ liệu mẫu là chuyện khác hẳn, cần quyết định riêng.
- Xuất hiện một khoá cấu hình nào đó thuộc chính sách mật khẩu trong một tệp cấu hình theo môi trường.

## Liên quan

- [`0013-multi-tenant.md`](0013-multi-tenant.md) · [`0017-khu-quan-tri-he-thong.md`](0017-khu-quan-tri-he-thong.md) — hai vai mà seed dev phải dựng
- [`0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) — hai cờ mà hai tài khoản dev mang
- [`../wiki-core/be/13-core-data-migration.md`](../wiki-core/be/13-core-data-migration.md) §5.2 · [`../database/script-runbook.md`](../database/script-runbook.md) §6 — hai câu mà ADR này nối lại
- [`../RULES.md`](../RULES.md) §6 — luật S7, S8, S9, S10
