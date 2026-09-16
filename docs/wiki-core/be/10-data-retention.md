---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 10. Vòng đời dữ liệu — xoá mềm, lưu giữ, kiểm toán

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Cách khai xoá mềm trên entity là thi công: [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md). File này lo **hệ quả và chính sách**.

---

## 1. Ba khái niệm hay bị gộp làm một

| Khái niệm | Nghĩa | Dữ liệu ở đâu |
| --- | --- | --- |
| **Xoá mềm** | Đánh dấu đã xoá, bản ghi vẫn nằm nguyên chỗ cũ | Bảng chính |
| **Lưu trữ** | Chuyển dữ liệu cũ sang nơi khác để bảng chính nhẹ đi | Bảng hoặc kho khác |
| **Xoá cứng** | Xoá thật, không lấy lại được | Không còn |

> **Xoá mềm không phải lưu trữ.** Nó không làm bảng nhẹ đi — trái lại, bảng chỉ phình thêm, và mọi truy vấn phải mang thêm một điều kiện lọc.

Nhầm hai khái niệm này dẫn tới một kết luận sai rất phổ biến: *"đã có xoá mềm nên không cần chính sách lưu giữ"*. Thực tế ngược lại — có xoá mềm thì **càng cần** chính sách lưu giữ, vì không có gì tự rời khỏi bảng nữa.

---

## 2. Xoá mềm — cái giá thật

Xoá mềm đáng dùng: nó cho phép khôi phục nhầm lẫn, giữ được tham chiếu lịch sử, và tránh việc xoá một bản ghi làm gãy dữ liệu liên quan. Nhưng nó kéo theo năm hệ quả, và mỗi hệ quả đều có một cách hỏng đặc trưng.

### 2.1 Bộ lọc mặc định — và chỗ nó không có tác dụng

Bộ lọc truy vấn tự động thêm điều kiện *"chưa bị xoá"* vào mọi truy vấn qua tầng ORM. Nó **không** áp cho:

| Chỗ | Hậu quả |
| --- | --- |
| SQL thô | Bản ghi đã xoá xuất hiện trở lại trên một báo cáo |
| Câu lệnh cập nhật hoặc xoá hàng loạt | Ghi trúng cả bản ghi đã xoá |
| Truy vấn từ công cụ ngoài, từ bảng điều khiển BI | Số liệu không khớp với màn hình ứng dụng |

**Cách canh:** luật E3 ở [`../../RULES.md`](../../RULES.md) bắt mọi entity phải có bộ lọc **đặt tên** — đặt tên để có thể tắt đúng bộ lọc cần tắt, thay vì tắt toàn bộ. Với SQL thô, không có cách nào ngoài kỷ luật và review.

### 2.2 Chỉ mục duy nhất — bẫy phổ biến nhất

Bảng người dùng có ràng buộc duy nhất trên email. Người dùng `a@x.com` bị xoá mềm. Ai đó tạo lại tài khoản với chính email đó → **vi phạm ràng buộc duy nhất**, dù trên giao diện chẳng có tài khoản nào dùng email đó.

Người dùng thấy một thông báo lỗi vô nghĩa về một bản ghi họ không nhìn thấy.

Trên PostgreSQL, cách xử lý là **chỉ mục duy nhất một phần** — chỉ áp cho các dòng chưa bị xoá:

```sql
CREATE UNIQUE INDEX ux_users_email_active
    ON core.users (email)
    WHERE deleted_at IS NULL;
```

Đánh đổi phải biết trước: sau khi làm vậy, **có thể tồn tại nhiều bản ghi đã xoá cùng email**. Mọi truy vấn tra ngược theo email phải xử lý được việc trả về nhiều dòng.

**Luật:** mỗi khi thêm ràng buộc duy nhất cho một bảng có xoá mềm, phải quyết định tường minh — duy nhất trong phạm vi *chưa xoá*, hay duy nhất tuyệt đối. Không có mặc định đúng cho mọi ca; bỏ qua câu hỏi này là cách sinh ra lỗi ở §2.2 này.

### 2.3 Đếm và tổng hợp

Mọi con số hiển thị cho người dùng phải nói rõ nó đếm gì. Một bảng điều khiển đếm "tổng số người dùng" mà quên lọc sẽ lệch dần theo thời gian, và không ai phát hiện vì không có mốc đúng để so.

### 2.4 Bản ghi liên quan

Xoá mềm một bản ghi cha thì các bản ghi con thế nào? Ba lựa chọn, và phải chọn tường minh cho từng quan hệ:

| Lựa chọn | Khi nào |
| --- | --- |
| Xoá mềm theo | Con không có nghĩa nếu không có cha |
| Giữ nguyên | Con là dữ liệu lịch sử độc lập |
| Chặn xoá cha | Còn con đang hoạt động thì không cho xoá |

Không chọn tường minh nghĩa là mặc định "giữ nguyên", và đó thường là lựa chọn sai — kết quả là dữ liệu con mồ côi, hiện ra ở những chỗ không ai ngờ.

### 2.5 Hiệu năng

Bảng chỉ tăng, không giảm. Chỉ mục lớn dần theo cả phần dữ liệu chưa bao giờ được truy vấn. Với bảng có tỉ lệ xoá cao, cân nhắc chỉ mục một phần cho các truy vấn nóng — xem [`11-performance-caching.md`](11-performance-caching.md).

### 2.6 Khi nào KHÔNG dùng xoá mềm

| Loại bảng | Vì sao không |
| --- | --- |
| Bảng nối nhiều-nhiều (gán quyền, gán vai trò) | Việc "bỏ gán" không cần lịch sử ở đây; lịch sử thuộc về nhật ký kiểm toán. Xoá mềm ở bảng nối làm mọi truy vấn phức tạp hơn mà không đổi lấy gì |
| Bảng chỉ ghi thêm (nhật ký, outbox) | Không có khái niệm xoá |
| Dữ liệu tạm, có hạn dùng | Xoá cứng theo hạn là đúng |
| Dữ liệu buộc phải xoá theo yêu cầu pháp lý | Xoá mềm **không phải là xoá**. Xem §5 |

Xoá mềm nên là quyết định cho **từng bảng**, không phải mặc định cho mọi bảng.

---

## 3. Lưu bao lâu

Chính sách lưu giữ phải được viết ra **trước khi** dữ liệu tích tụ. Viết sau nghĩa là phải quyết định trên dữ liệu thật, dưới áp lực, và thường là sau một sự cố.

| Loại dữ liệu | Gợi ý cho hệ tầm trung | Quyết bởi |
| --- | --- | --- |
| Bản ghi nghiệp vụ đang hoạt động | Không giới hạn | Nghiệp vụ |
| Bản ghi đã xoá mềm | Xoá cứng sau một khoảng đã khai | Nghiệp vụ + pháp lý |
| Nhật ký kiểm toán | Dài hơn hẳn dữ liệu nghiệp vụ | Yêu cầu tuân thủ |
| Log vận hành | Ngắn — vài tuần | Vận hành |
| Phiên đăng nhập hết hạn | Dọn thường xuyên | Kỹ thuật |
| File tạm | Rất ngắn, xem [`14-file-storage.md`](14-file-storage.md) | Kỹ thuật |
| Bản ghi outbox đã phát | Ngắn | Kỹ thuật |

**Nguyên tắc:** mỗi bảng có dữ liệu tích tụ theo thời gian phải trả lời được câu *"dòng cũ nhất trong bảng này bao nhiêu tuổi, và vì sao nó còn ở đó"*. Không trả lời được nghĩa là chưa có chính sách.

---

## 4. Xoá cứng — làm thế nào cho an toàn

| Nguyên tắc | Chi tiết |
| --- | --- |
| **Chạy theo lô** | Xoá hàng triệu dòng trong một transaction khoá bảng và có thể làm đầy nhật ký ghi của DB |
| **Có bản sao lưu kiểm chứng được trước khi chạy lần đầu** | Xoá cứng không hoàn tác được |
| **Chạy được ở chế độ thử** | Đếm số dòng sẽ xoá, báo cáo, không xoá. Luôn chạy thử trước |
| **Ghi nhật ký kết quả** | Xoá bao nhiêu, bảng nào, tiêu chí nào |
| **Đúng thứ tự phụ thuộc** | Con trước, cha sau — kể cả khi không có khoá ngoại vật lý giữa các schema |
| **Không xoá thứ nhật ký kiểm toán đang tham chiếu** | Xem §6 |

Dòng cuối là chỗ xung đột thật giữa hai chính sách, và phải giải quyết bằng thiết kế chứ không bằng ngoại lệ: nhật ký kiểm toán **tự chứa** thông tin cần thiết (lưu tên hiển thị tại thời điểm ghi, không chỉ lưu định danh), để việc xoá bản ghi gốc không làm nhật ký mất nghĩa.

---

## 5. Nhật ký kiểm toán

### 5.1 Khác log vận hành

| | Log vận hành | Nhật ký kiểm toán |
| --- | --- | --- |
| Mục đích | Chẩn đoán sự cố | Trả lời "ai đã làm gì" |
| Nơi lưu | Hệ thu gom log | **Bảng trong DB** |
| Mất một phần | Chấp nhận được | **Không chấp nhận được** |
| Ai đọc | Kỹ thuật | Kiểm toán, quản trị, đôi khi cả pháp lý |
| Giữ bao lâu | Vài tuần | Theo yêu cầu tuân thủ |

Trộn hai thứ là cách mất bằng chứng đúng lúc cần nhất — log xoay vòng theo dung lượng, và thứ bị xoá đầu tiên luôn là thứ cũ nhất, tức là thứ đang cần cho một cuộc điều tra.

### 5.2 Ghi gì

| Trường | Ghi chú |
| --- | --- |
| Ai | Định danh **và** tên hiển thị tại thời điểm đó |
| Lúc nào | Thời điểm có múi giờ |
| Làm gì | Loại hành động, dạng mã |
| Trên đối tượng nào | Loại và định danh, **và** nhãn hiển thị tại thời điểm đó |
| Giá trị trước và sau | Với thay đổi quan trọng |
| Từ đâu | Địa chỉ IP, mã lần gọi |

Việc lưu **tên hiển thị tại thời điểm đó** là có chủ đích: bản ghi gốc có thể bị đổi tên hoặc bị xoá, và một dòng nhật ký chỉ có định danh sẽ trở thành vô nghĩa đúng lúc cần đọc.

> 📖 Cột cụ thể của bảng: [`../../database/schema-core.md`](../../database/schema-core.md) §9.4. Mục này nói *ghi gì và vì sao*, không khai lại cột.

### 5.3 Bất biến

Nhật ký kiểm toán **chỉ được ghi thêm**. Không sửa, không xoá qua đường ứng dụng. Ứng dụng không cần quyền cập nhật hay xoá trên bảng đó — và đó là cách ép mạnh nhất, mạnh hơn mọi quy ước.

### 5.4 Ghi gì vào nhật ký kiểm toán

Không phải mọi thứ. Ghi hết thì bảng phình và không ai đọc nổi. Ghi:

- Thay đổi phân quyền và vai trò.
- Tạo, khoá, mở khoá tài khoản; đổi mật khẩu.
- Xuất dữ liệu: ai xuất, bộ lọc nào, bao nhiêu dòng ([`15-import-export.md`](15-import-export.md) §5.5).
- Phát lại bản ghi outbox chết bằng lệnh `core outbox-replay` — mỗi dòng phát lại một dòng nhật ký ([`../../database/script-runbook.md`](../../database/script-runbook.md) §10).
- Xoá bản ghi nghiệp vụ (mềm hoặc cứng).
- Thay đổi cấu hình hệ thống.
- Truy cập dữ liệu nhạy cảm, nếu có yêu cầu tuân thủ.

---

## 6. Quyền được quên

Khi có yêu cầu xoá dữ liệu cá nhân, ba việc xung đột nhau: nghĩa vụ xoá, nghĩa vụ giữ nhật ký kiểm toán, và nhu cầu giữ toàn vẹn dữ liệu nghiệp vụ.

Cách giải thường dùng là **ẩn danh hoá thay vì xoá**:

| Dữ liệu | Xử lý |
| --- | --- |
| Tên, email, điện thoại, địa chỉ | Thay bằng giá trị vô danh, không hoàn nguyên được |
| Bản ghi nghiệp vụ (đơn từ, giao dịch) | **Giữ**, nhưng không còn liên hệ tới người thật |
| Nhật ký kiểm toán | Giữ; thay phần định danh cá nhân bằng giá trị vô danh |
| File tải lên | Xoá thật |

Bốn điểm phải xử lý, và đây là chỗ hay bị bỏ sót:

1. **Bản sao lưu** vẫn chứa dữ liệu cũ. Chính sách phải nói rõ điều đó và nêu thời hạn sao lưu bị xoay vòng.
2. **Log vận hành** có thể chứa dữ liệu cá nhân dù đã lọc — xem [`07-observability.md`](07-observability.md) §5. Thời hạn giữ log ngắn là biện pháp giảm nhẹ chính.
3. **Ẩn danh hoá phải không hoàn nguyên được.** Thay tên bằng một giá trị vẫn tra ngược được về người thật thì chưa phải ẩn danh hoá.
4. **Việc ẩn danh hoá phải để lại một dòng trong nhật ký kiểm toán** — ghi rằng đã xử lý, không ghi dữ liệu đã xoá.

---

## 7. Sao lưu

| Câu hỏi | Ghi chú |
| --- | --- |
| Tần suất | Theo mức dữ liệu chấp nhận mất được |
| Giữ bao nhiêu bản | Đủ để phát hiện hỏng dữ liệu — hỏng có thể âm thầm nhiều ngày trước khi ai đó nhận ra |
| **Đã thử phục hồi chưa** | Đây là câu hỏi duy nhất thật sự quan trọng |
| Sao lưu có được mã hoá không | Bản sao lưu chứa mọi dữ liệu cá nhân của hệ thống |

> **Một bản sao lưu chưa từng được phục hồi thử thì chưa phải là bản sao lưu — nó là một file mà mọi người cùng hy vọng.**

Việc thử phục hồi phải định kỳ, phải ghi nhận kết quả, và phải đo **thời gian** phục hồi — vì thời gian đó chính là thời gian ngừng hoạt động khi sự cố thật xảy ra.

Sao lưu thuộc **vận hành**, không thuộc Core. Nhưng thiết kế của Core ảnh hưởng tới nó: một hệ có file lưu ngoài DB thì sao lưu DB **không đủ**, và hai thứ đó phải phục hồi về cùng một mốc thời gian mới nhất quán.

### 7.1 Bảng khoá bảo vệ dữ liệu — ràng buộc do kiến trúc Core sinh ra

Khoá ký phiếu xác thực và token antiforgery nằm **trong DB** ([`../../adr/0014-mot-instance-key-ring-postgres.md`](../../adr/0014-mot-instance-key-ring-postgres.md), bảng ở [`../../database/schema-core.md`](../../database/schema-core.md) §9.3). Vì chính Core đặt nó ở đó, ba luật dưới đây thuộc Core, không phó mặc cho vận hành:

| Luật | Vì sao |
| --- | --- |
| Bảng khoá **luôn nằm trong** bản sao lưu — không bao giờ bị loại vì "không phải dữ liệu nghiệp vụ" | Phục hồi thiếu nó thì **mọi** cookie phiên và token antiforgery đang lưu hành vô hiệu: toàn bộ người dùng bị đăng xuất, request ghi đầu tiên của mỗi người bị từ chối |
| Phục hồi về một mốc cũ thì **báo trước** rằng người dùng phải đăng nhập lại | Khoá sinh sau mốc đó mất theo — hệ quả đúng, nhưng không ai được bất ngờ |
| **Không** mang bản sao lưu của môi trường thật sang môi trường khác mà giữ nguyên bảng khoá | Môi trường đích khi đó giải mã được cookie của môi trường thật. Mang dữ liệu sang thì **xoá bảng khoá ở đích** để nó tự sinh bộ mới |

---

## 8. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Xoá mềm với bộ lọc đặt tên | ✅ sẽ có | Luật E3 |
| Chỉ mục duy nhất một phần khi bảng có xoá mềm | ✅ sẽ có | Quyết định tường minh cho từng ràng buộc |
| Xoá mềm là quyết định theo từng bảng | ✅ sẽ có | Không bật mặc định cho bảng nối |
| Nhật ký kiểm toán trong schema `core` | ✅ sẽ có | Chỉ ghi thêm; lưu cả nhãn hiển thị tại thời điểm ghi — bảng ở [`../../database/schema-core.md`](../../database/schema-core.md) §9.4 |
| Dọn bản ghi outbox đã phát | ✅ sẽ có | |
| **Job xoá cứng theo chính sách lưu giữ** | ❌ chưa | Cần chính sách nghiệp vụ trước. Khi làm: chạy theo lô, có chế độ thử |
| **Ẩn danh hoá theo yêu cầu** | ❌ chưa | Thiết kế sẵn chỗ (nhật ký kiểm toán tự chứa nhãn) để sau này làm được mà không phải sửa lược đồ |
| **Lưu trữ sang kho khác** | ❌ chưa | Chỉ cần khi bảng đủ lớn để ảnh hưởng truy vấn. Đo trước |
| **Sao lưu và thử phục hồi** | ❌ ngoài phạm vi Core | Thuộc vận hành — **trừ** ba luật về bảng khoá bảo vệ dữ liệu ở §7.1, thứ do chính Core sinh ra. Có file ngoài DB thì sao lưu DB không đủ |
