---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 03. Thiết kế theo metadata — và chỗ nó trở thành cái bẫy

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`.
>
> Hợp đồng endpoint menu: [`../../contracts/meta-menu.md`](../../contracts/meta-menu.md). Phía FE tiêu thụ metadata: [`../fe/11-grid-and-metadata.md`](../fe/11-grid-and-metadata.md).

---

## 1. Metadata giải quyết gì

Metadata là **dữ liệu mô tả giao diện hoặc hành vi**, thay vì viết chúng thành code. Đổi metadata là thao tác dữ liệu; đổi code là sửa, build, triển khai.

Nó đáng dùng khi thoả cả hai:

1. Thứ cần đổi **thật sự đổi thường xuyên** — không phải "biết đâu sau này".
2. Người đổi **không phải lập trình viên** — hoặc là lập trình viên nhưng không muốn triển khai lại chỉ vì một dòng cấu hình.

Thiếu vế thứ nhất thì metadata là chi phí không đổi lấy gì. Thiếu vế thứ hai thì một hằng số trong code đã đủ và rõ hơn nhiều.

### Ba mức, không phải một

| Mức | Metadata mô tả gì | Nhóm | Rủi ro |
| --- | --- | --- | --- |
| **1. Điều hướng** | Menu nào tồn tại, ai thấy được | A — bắt buộc | Thấp |
| **2. Trình bày** | Lưới hiển thị cột nào, rộng bao nhiêu, sắp xếp mặc định | B — khi có nhu cầu | Trung bình |
| **3. Cấu trúc và hành vi** | Form gồm ô nào, kiểu gì, ràng buộc gì, luật hiển thị có điều kiện | B — **rất thận trọng** | Cao |

Rủi ro tăng theo mức vì mức càng cao thì metadata càng **mô tả logic** thay vì mô tả dữ liệu. Chi tiết ở §5.

---

## 2. Mức 1 — menu động theo quyền

### 2.1 Vì sao đây là Nhóm A

Menu hardcode ở FE có ba hệ quả, và cả ba đều xảy ra sớm:

- Thêm một màn hình là sửa FE và triển khai lại FE, kể cả khi BE đã sẵn sàng.
- Việc lọc menu theo quyền phải viết tay ở FE, và nó sẽ lệch với việc kiểm quyền thật ở BE.
- Một module lắp thêm vào không tự khai được mục menu của nó — Host phải biết trước.

### 2.2 Mô hình dữ liệu tối thiểu

| Thuộc tính | Vai |
| --- | --- |
| Khoá ổn định | Định danh mục menu; **không đổi** sau khi phát hành |
| Mục cha | Dựng cây |
| Thứ tự | Sắp xếp trong cùng cấp |
| Đường dẫn điều hướng | FE dùng để chuyển trang |
| Khoá biểu tượng | FE tra ra icon; **BE không giữ đường dẫn ảnh** |
| Khoá quyền yêu cầu | Mục chỉ hiện với người có quyền đó |
| Khoá i18n cho nhãn | **Không lưu câu tiếng Việt trong DB** |

Hai dòng cuối là hai bẫy đã thấy ở nhiều dự án:

- **Lưu nhãn tiếng Việt trong bảng menu.** Ngày thêm ngôn ngữ thứ hai, mọi bản ghi phải nhân đôi hoặc thêm cột — và bảng menu trở thành một bảng dịch tạm bợ. Lưu **khoá**, để FE dịch. Xem [`16-i18n-va-ma-loi.md`](16-i18n-va-ma-loi.md).
- **Lưu đường dẫn file icon.** Đổi bộ icon là chạy `UPDATE` trên dữ liệu thật. Lưu **khoá**, FE ánh xạ.

### 2.3 Menu là trải nghiệm, không phải hàng rào

Endpoint trả menu đã lọc theo quyền của người đang đăng nhập. Nhưng:

> **Ẩn một mục menu không ngăn được ai gọi thẳng endpoint đằng sau nó.**

Phân quyền thật nằm ở biên HTTP và ở handler — xem [`02-identity-auth.md`](02-identity-auth.md) §3.4. Coi menu là hàng rào là một trong những nhầm lẫn tốn kém nhất trong mảng này, vì nó **trông có vẻ đúng** trên mọi thử nghiệm thủ công.

### 2.4 Quyền đổi thì menu phải đổi ngay

Menu được tính theo quyền hiện tại, không lấy từ bản chụp lúc đăng nhập. Nếu có cache, nó phải bị vô hiệu hoá khi phân quyền thay đổi — và đó chính là loại vô hiệu hoá cache khó làm đúng. Ở v1 chưa cache; lý do ở [`11-performance-caching.md`](11-performance-caching.md).

---

## 3. Mức 2 — cấu hình cột lưới

### 3.1 Chia đôi trách nhiệm — chỗ hay bị làm sai

| Loại cấu hình | Ai giữ | Vì sao |
| --- | --- | --- |
| Cột nào **tồn tại**, kiểu dữ liệu, có sắp xếp được không, có lọc được không | **BE** | Đây là hợp đồng dữ liệu. FE tự bịa một cột thì không có gì đằng sau nó |
| Người dùng **ẩn/hiện** cột nào, thứ tự, độ rộng | **Người dùng**, lưu theo từng người | Đây là sở thích, không phải hợp đồng |
| Nhãn hiển thị của cột | **Khoá i18n** từ BE, câu chữ ở FE | Cùng lý do §2.2 |
| Định dạng hiển thị (số lẻ, kiểu ngày) | BE khai **ý định** (tiền tệ / số nguyên / ngày), FE quyết định hiển thị | BE không được ghép sẵn chuỗi đã định dạng — xem [`16-i18n-va-ma-loi.md`](16-i18n-va-ma-loi.md) |

**Bẫy phổ biến:** để BE trả về chuỗi đã định dạng sẵn (`"1.234,50 ₫"`). Nó tiện đúng một lần, rồi hỏng ở mọi thứ đến sau: sắp xếp phía client, xuất Excel ra ô văn bản thay vì ô số, và đổi ngôn ngữ.

### 3.2 Ngưỡng để làm

Cấu hình cột đáng metadata hoá khi có **từ ba màn hình lưới trở lên** cùng cần tuỳ biến. Dưới ngưỡng đó, khai cột ngay tại component là rõ hơn và sửa nhanh hơn.

---

## 4. Mức 3 — form sinh từ metadata

Đây là mức có tỉ lệ hối tiếc cao nhất. Nó hấp dẫn vì lời hứa *"thêm trường mới không cần lập trình viên"*, và lời hứa đó đúng — cho tới ô nhập thứ hai cần một hành vi mà metadata chưa mô tả được.

### 4.1 Đường trượt điển hình

```text
Bước 1: metadata mô tả ô nhập      → tên, kiểu, bắt buộc hay không.  Ổn.
Bước 2: cần ẩn ô B khi ô A = X     → thêm trường "điều kiện hiển thị".
Bước 3: điều kiện cần "hoặc"       → thêm cú pháp biểu thức.
Bước 4: cần gọi API khi ô đổi      → thêm "hành động".
Bước 5: hành động cần rẽ nhánh     → thêm điều kiện vào hành động.
Bước 6: có người hỏi vì sao form   → không ai trả lời được.
        không chạy đúng
```

Tới bước 4, metadata **đã là một ngôn ngữ lập trình**. Một ngôn ngữ không có compiler, không có gợi ý cú pháp, không có debugger, không có test, không có `git blame` ở mức dòng, và chỉ một người trong nhóm hiểu.

### 4.2 Ranh giới cụ thể

Metadata cho form được phép mô tả **cấu trúc và ràng buộc tĩnh**. Nó **không** được mô tả **luồng điều khiển**.

| Được | Không được |
| --- | --- |
| Ô này tên gì, kiểu gì | Biểu thức điều kiện tự do |
| Bắt buộc hay không | Vòng lặp, rẽ nhánh |
| Giới hạn độ dài, khoảng giá trị | Gọi API khi giá trị đổi |
| Nguồn dữ liệu cho danh sách chọn (theo **khoá danh mục**, không theo câu SQL) | Câu SQL hoặc tên bảng nằm trong metadata |
| Thứ tự và nhóm ô | Luật nghiệp vụ |

**Phép thử một câu:** nếu để thêm một tính năng vào form, người ta phải **viết một biểu thức** vào metadata thay vì **chọn một giá trị** — đã vượt ranh giới.

### 4.3 Điều kiện tối thiểu nếu vẫn làm

Nếu vẫn quyết định làm form theo metadata, ba thứ này là bắt buộc, không phải tuỳ chọn:

1. **Metadata có schema và được kiểm lúc lưu**, không phải lúc render. Metadata sai phải bị từ chối tại chỗ, không được biến thành một màn hình trắng ở môi trường thật.
2. **Có kiểm tra tham chiếu**: mọi trường trong metadata phải khớp một trường thật trong mô hình dữ liệu. Không có bước này, gõ sai một tên trường là một ô nhập im lặng không lưu được gì.
3. **Có bản ghi lịch sử thay đổi metadata.** Metadata là code; code phải truy được ai đổi, lúc nào, đổi từ gì.

Không đủ ba thứ trên thì đừng làm — hãy viết form bằng code.

---

### 4.4 Ba bậc trung gian — cân nhắc trước khi nhảy sang mức 3

Giữa "viết form bằng code" và "sinh form từ metadata" có ba bậc rẻ hơn nhiều, và phần lớn nhu cầu thật dừng lại ở đây:

| Bậc | Cách làm | Đổi được gì mà không sửa code |
| --- | --- | --- |
| **Component tái sử dụng** | Form viết bằng code nhưng ghép từ các khối dùng chung | Không gì — nhưng chi phí viết một form mới giảm rất mạnh |
| **Cấu hình chọn từ tập đóng** | Metadata chỉ chứa các giá trị chọn từ một danh sách đã khai trong code | Bật/tắt ô, đổi nhãn, đổi thứ tự |
| **Bố cục theo metadata, ô nhập theo code** | Code khai các ô và hành vi; metadata chỉ quyết định sắp xếp và nhóm | Cách trình bày |

Bậc thứ hai đáng nhấn: **tập đóng** nghĩa là mọi giá trị hợp lệ đều được khai trong code, nên metadata sai bị từ chối ngay, và không có chỗ nào để một biểu thức len vào. Đó là ranh giới ở §4.2 được ép bằng kiểu dữ liệu thay vì bằng kỷ luật.

Chỉ khi cả ba bậc đều không đủ thì mức 3 mới đáng bàn — và lúc đó phải trả lời được §4.3 và §6.

---

## 5. Khi metadata thành bẫy — bốn triệu chứng

| Triệu chứng | Nghĩa là gì |
| --- | --- |
| Có người phải **đọc metadata để hiểu code làm gì** | Logic đã chuyển từ code sang dữ liệu, nhưng công cụ thì vẫn nằm ở phía code |
| Metadata sai làm hỏng **âm thầm** — không lỗi, chỉ là màn hình thiếu thứ gì đó | Không có tầng kiểm hợp lệ. Đây là dạng hỏng đắt nhất vì không ai biết để sửa |
| Có một tài liệu riêng **giải thích cú pháp** của metadata | Đã có một ngôn ngữ. Ngôn ngữ thì cần đặc tả, công cụ, và người bảo trì |
| Sửa một tính năng phải **đổi cả code và metadata** cho khớp | Metadata không hề tách được khỏi code — nó chỉ tách được *hình thức*, còn ràng buộc thì vẫn nguyên, và nay nằm ở hai chỗ |

Triệu chứng thứ tư là nghiêm trọng nhất: nó nghĩa là metadata **không đạt được mục tiêu ban đầu** (đổi mà không cần lập trình viên) nhưng vẫn thu đủ chi phí.

### Đường lùi

Khi đã lỡ vượt ranh giới, cách gỡ rẻ nhất là **đóng băng metadata ở mức đang có** (không thêm khả năng mới) và chuyển các ca mới sang code, thay vì viết lại toàn bộ. Metadata đã sinh ra dữ liệu thật; xoá cơ chế là mất dữ liệu đó.

---

## 6. Bốn câu hỏi trước khi metadata hoá bất cứ thứ gì

Trả lời được cả bốn thì làm. Vướng một câu thì viết bằng code.

1. **Ai đổi thứ này, và bao lâu một lần?** Nếu câu trả lời là "lập trình viên, vài tháng một lần" — dùng code.
2. **Metadata sai thì hệ thống báo lỗi ở đâu?** Không trả lời được nghĩa là nó sẽ hỏng âm thầm.
3. **Có cần biểu thức không?** Cần thì đã vượt ranh giới §4.2.
4. **Ai truy vết được một thay đổi metadata sau sáu tháng?** Không có lịch sử thay đổi thì không.

---

## 7. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| **Menu động theo quyền** | ✅ sẽ có — Nhóm A | Lưu khoá i18n và khoá icon, không lưu câu chữ và đường dẫn ảnh |
| **Danh mục cột do BE khai** | ✅ sẽ có ở mức tối thiểu | BE khai cột tồn tại và kiểu; FE quyết hiển thị |
| **Sở thích cột theo người dùng** | ❌ chưa | Thêm khi có màn hình thứ ba cần tuỳ biến cột |
| **Form sinh từ metadata** | ❌ **không làm ở v1** | Chưa đạt điều kiện §4.3, và chưa có nhu cầu thật. Muốn làm phải có ADR nêu rõ ba điều kiện đó được đáp ứng thế nào |
| **Biểu thức điều kiện trong metadata** | ❌ **loại, không hoãn** `K03` | Vượt ranh giới §4.2. Đây là quyết định về hướng, không phải về thứ tự ưu tiên |

Một đề xuất "làm engine form cho linh hoạt" cần kèm câu trả lời cho cả bốn câu hỏi ở §6. Thiếu câu trả lời thì đề xuất chưa đủ chín.
