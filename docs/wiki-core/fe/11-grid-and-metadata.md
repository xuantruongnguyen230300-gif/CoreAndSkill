---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 11. Bảng dữ liệu server-side

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`; đây là component `shared/components/data-grid` phải dựng ở pha F3.
>
> Hợp đồng phân trang phía BE: [`../../contracts/README.md`](../../contracts/README.md). Truy vấn và chỉ mục: [`../../quy-uoc/be-performance.md`](../../quy-uoc/be-performance.md).

---

## 1. Server-side là mặc định, không phải tối ưu hoá

Tải toàn bộ dữ liệu về rồi lọc ở trình duyệt là cách viết nhanh nhất và cũng là cách hỏng chắc chắn nhất — chỉ là hỏng muộn.

| | Client-side | Server-side |
| --- | --- | --- |
| 50 dòng | Chạy tốt | Chạy tốt |
| 5.000 dòng | Chậm rõ, tốn bộ nhớ | Chạy tốt |
| 500.000 dòng | Trình duyệt treo | Chạy tốt |
| Ai thấy vấn đề trước | **Người dùng ở môi trường thật** | — |

Điều làm client-side nguy hiểm: **nó chạy hoàn hảo với dữ liệu thử.** Máy dev có 20 bản ghi, mọi thứ mượt. Vấn đề chỉ xuất hiện sau vài tháng dùng thật, và lúc đó sửa là viết lại màn hình.

Vì thế: **mọi bảng dùng đường server-side ngay từ dòng code đầu tiên**, kể cả khi biết chắc bảng đó ít dữ liệu. Chi phí thêm gần bằng 0 khi đã có component sẵn.

**Ngoại lệ duy nhất:** danh sách tra cứu ngắn có trần cứng theo bản chất (danh sách vai trò, danh sách trạng thái). Chúng không phải bảng dữ liệu; chúng là danh mục.

---

## 2. Hợp đồng truy vấn và kết quả

> 📖 **Chữ ký `GridQuery` — định nghĩa ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8**, cùng chỗ với `ListStateStore` là tầng giữ nó. Không khai lại ở đây.

Khu này giữ **lý lẽ**, không giữ chữ ký: vì sao trang đếm từ 1 (§2.1), vì sao không có `totalPages` (§2.2), vì sao mọi bảng đi đường server-side (§1).

Kết quả trả về **không** khai lại ở đây: bảng dữ liệu tiêu thụ đúng kiểu `PagedList<T>` của tầng gọi API.

> 📖 **Kiểu `PagedList<T>` phía FE: [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §5.1 · hợp đồng phân trang trên dây (tên tham số, khoảng hợp lệ, mặc định): [`../../contracts/README.md`](../../contracts/README.md) §8.**

`GridQuery` ở trên là **trạng thái của bảng trên màn hình**, không phải kiểu gửi lên dây: nó mang thêm `filters` và dùng tên theo ngôn ngữ của giao diện. Bước chuyển sang tham số truy vấn thật là một bước **tường minh**, khai ở đúng một chỗ ([`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §7.2). Đưa thẳng một object trạng thái vào hàm dựng query string là cách một tham số sai tên đi ra khỏi trình duyệt mà không ai thấy — BE không nhận ra tham số, **áp mặc định**, và người dùng bấm trang 7 nhận về trang 1.

### 2.1 Trang bắt đầu từ 1

Đây là một quyết định tuỳ ý, và giá trị của nó nằm ở chỗ **được chốt một lần cho toàn hệ**. Lỗi lệch một đơn vị giữa FE và BE cho ra một triệu chứng đặc trưng: trang đầu hiển thị đúng, mọi trang sau lệch đúng một trang, và không có lỗi nào được ném ra.

Chốt: **1 là trang đầu**, ở cả hợp đồng API lẫn hiển thị. Nếu thư viện bảng đếm từ 0 ở nội bộ, việc chuyển đổi nằm gọn trong lớp bọc — không rò ra nơi gọi.

### 2.2 Không có `totalPages`

Server trả `totalCount`; số trang do FE tự tính. Lý do: `totalPages` là dữ liệu **phái sinh**, và dữ liệu phái sinh gửi qua dây là hai nguồn sự thật cho cùng một con số. Khi `pageSize` đổi ở client mà `totalPages` cũ vẫn còn, phân trang hỏng im lặng. Hợp đồng ở [`../../contracts/README.md`](../../contracts/README.md) §8 không có trường đó, và kiểu phía FE cũng không được thêm vào.

**Bẫy thi công:** chờ một trường không tồn tại sẽ cho `undefined`, và phép tính ra `NaN` — thanh phân trang hiển thị trống chứ không báo lỗi.

### 2.3 Tổng số bản ghi khi bảng rất lớn

Đếm chính xác trên bảng lớn có thể tốn hơn cả việc lấy dữ liệu. Nếu BE chuyển sang đếm gần đúng hoặc bỏ đếm, **FE phải biết** — giao diện phân trang "trang 5/∞" khác hẳn "trang 5/120". Đây là điều phải khai ở hợp đồng ngay từ đầu, không phải phát hiện lúc đã chậm.

---

## 3. Trạng thái bảng đặt trên URL

> **Trang, sắp xếp, bộ lọc, từ khoá là UI state cần chia sẻ được — chúng thuộc URL, không thuộc store.** ([`03-state-management.md`](03-state-management.md) §1)

### 3.1 Bốn thứ được lợi ngay

| Lợi ích | Nếu không đặt trên URL |
| --- | --- |
| Gửi link cho đồng nghiệp | Họ mở ra thấy trang 1 không lọc, rồi phải mô tả bằng lời "lọc theo X rồi sang trang 3" |
| Nút Quay lại của trình duyệt hoạt động đúng | Bấm Quay lại từ màn chi tiết là mất sạch bộ lọc — lỗi trải nghiệm bị phàn nàn nhiều nhất ở màn danh sách |
| Tải lại trang giữ nguyên chỗ đang xem | Mất chỗ |
| Lưu dấu trang | Không lưu được |

### 3.2 Ba quy tắc

| Quy tắc | Vì sao |
| --- | --- |
| Đổi bộ lọc hay từ khoá → **về trang 1** | Giữ trang cũ với kết quả mới thường cho một trang rỗng, và người dùng tưởng không có dữ liệu |
| Giá trị mặc định **không** ghi lên URL | URL đầy tham số mặc định thì dài, khó đọc, và mọi thay đổi mặc định sau này lại phải xử lý URL cũ |
| Đọc URL là nguồn khởi tạo duy nhất | Nếu component tự đặt trạng thái ban đầu rồi mới đọc URL, sẽ có hai lần tải: một lần sai rồi một lần đúng |

### 3.3 Bẫy vòng lặp

URL đổi → tải dữ liệu → cập nhật state → ghi lại URL → URL đổi → …

Vòng lặp này xảy ra khi cùng một dòng dữ liệu đi cả hai chiều. Cách chống: **chỉ một chiều là nguồn.** URL là nguồn; component chỉ đọc từ URL và chỉ ghi lên URL khi **người dùng** thao tác, không ghi lại URL sau khi tải xong.

---

## 4. Chống gọi API dồn dập khi gõ tìm kiếm

Ba thứ phải làm, và mỗi thứ chống một vấn đề khác nhau:

| Cơ chế | Chống cái gì |
| --- | --- |
| Trì hoãn theo thời gian gõ | Mỗi ký tự một request |
| Bỏ qua giá trị trùng liên tiếp | Gõ rồi xoá về đúng giá trị cũ vẫn gọi lại |
| Huỷ request cũ khi có request mới | **Kết quả về sai thứ tự** |

Cơ chế thứ ba là cơ chế hay bị quên nhất và gây lỗi khó hiểu nhất: người dùng gõ "ab" rồi "abc"; request của "ab" chậm hơn và về sau, ghi đè kết quả của "abc". Người dùng thấy ô tìm kiếm ghi "abc" nhưng danh sách là kết quả của "ab" — và bấm tìm lại thì đúng, nên lỗi này rất khó tin là có thật.

---

## 5. Chọn nhiều dòng

| Quyết định | Chọn | Vì sao |
| --- | --- | --- |
| Ô chọn tất cả chọn phạm vi nào | **Chỉ trang hiện tại** | "Chọn tất cả 12.400 dòng" là một thao tác nguy hiểm ẩn sau một ô tích. Muốn thao tác toàn bộ thì phải có một hành động riêng, nói rõ số lượng |
| Chọn có sống qua khi đổi trang không | **Có**, nhưng phải hiển thị số đã chọn | Mất lựa chọn khi lật trang là bực bội; giữ mà không hiện thì người dùng thao tác lên những dòng họ đã quên |
| Đổi bộ lọc thì sao | **Xoá lựa chọn** | Lựa chọn với một tập kết quả khác là vô nghĩa và nguy hiểm |
| Thao tác hàng loạt | Xác nhận có **số lượng** trong câu hỏi | "Xoá 37 người dùng?" khác hẳn "Bạn có chắc không?" |

Thanh thao tác hàng loạt chỉ hiện khi có ít nhất một dòng được chọn, và **luôn** cho biết đang chọn bao nhiêu.

---

## 6. Cột cấu hình được

Ba mức, đi từ rẻ tới đắt. Đừng nhảy thẳng lên mức 3.

| Mức | Nội dung | Lưu ở đâu |
| --- | --- | --- |
| 1 | Ẩn/hiện cột | `localStorage` theo máy |
| 2 | Thêm đổi thứ tự và độ rộng cột | `localStorage` |
| 3 | Lưu theo tài khoản, đồng bộ giữa máy | Server — cần endpoint và hợp đồng |

**Mức 1–2 dùng `localStorage`** vì đây là tuỳ chọn hiển thị của một máy, không phải dữ liệu tài khoản ([`14-security.md`](14-security.md) §6). Mức 3 chỉ làm khi có yêu cầu thật, vì nó kéo theo một endpoint, một bảng, và một bài toán di trú khi tập cột đổi.

**Bẫy chung cho cả ba mức:** cấu hình đã lưu tham chiếu tới **tập cột của phiên bản cũ**. Khi một cột bị bỏ đi hoặc đổi khoá, cấu hình cũ trỏ vào hư không. Bảng phải bỏ qua khoá không nhận ra và vẫn hiển thị bình thường — không được vỡ, và cũng không được im lặng hiện bảng trống.

---

## 7. Bốn trạng thái bắt buộc của một bảng

| Trạng thái | Hiển thị | Bỏ qua thì sao |
| --- | --- | --- |
| Đang tải lần đầu | Skeleton giữ đúng chiều cao hàng | Giao diện nhảy khi dữ liệu về |
| Đang tải lại (đổi trang, đổi lọc) | Giữ dữ liệu cũ mờ đi, **không** thay bằng skeleton | Nhấp nháy toàn bảng ở mỗi thao tác |
| Rỗng | Câu giải thích + hành động gợi ý | Bảng trắng — người dùng không biết là chưa có dữ liệu hay là lỗi |
| Lỗi | Thông báo + nút thử lại | Bảng trắng, giống hệt trạng thái rỗng |

Hai dòng cuối phân biệt được là điều kiện tối thiểu. Hai trạng thái khác nhau về bản chất mà trông giống nhau là một lỗi giao diện thường gặp và dễ sửa.

**Trạng thái rỗng có hai loại, và câu chữ phải khác nhau:** "chưa có dữ liệu nào" (gợi ý tạo mới) khác "không có kết quả khớp bộ lọc" (gợi ý xoá bộ lọc). Dùng chung một câu cho cả hai là bảo người dùng tạo mới trong khi thứ họ cần là bỏ bộ lọc.

---

## 8. Xuất dữ liệu theo bộ lọc đang xem

### 8.1 Nguyên tắc: xuất theo bộ lọc, không xuất theo trang

Người dùng bấm Xuất khi đang xem trang 3 mong nhận **toàn bộ kết quả khớp bộ lọc**, không phải 20 dòng của trang 3. Đây là kỳ vọng gần như phổ quát và hiếm khi được nói ra.

Hệ quả: yêu cầu xuất gửi lên **cùng bộ lọc**, nhưng không gửi tham số phân trang.

### 8.2 Xuất ở server, không ở client

| | Server dựng tệp | Client dựng tệp |
| --- | --- | --- |
| Giới hạn dữ liệu | Không bị ràng buộc bộ nhớ trình duyệt | Treo tab với vài chục nghìn dòng |
| Định dạng | Kiểm soát được, thống nhất | Mỗi màn một kiểu |
| Số dòng | Đúng tập server có | Chỉ những gì FE đã tải về |

Ngoại lệ hợp lý duy nhất: xuất **đúng thứ đang hiển thị** (đúng cột đã chọn, đúng thứ tự đang xem) trên một tập nhỏ. Đó là B9 ở [`01-core-components.md`](01-core-components.md) §3 — làm khi có nhu cầu thật.

### 8.3 Ba điều hay quên

1. **Xuất là thao tác chậm.** Phải có phản hồi ngay (trạng thái đang xử lý), và với tập lớn thì nên là tác vụ nền có thông báo khi xong — xem [`../be/15-import-export.md`](../be/15-import-export.md).
2. **Xuất phải kiểm quyền như đọc dữ liệu.** Một endpoint xuất không kiểm quyền là một đường vòng qua mọi kiểm tra của màn danh sách.
3. **Tên tệp nên mang bộ lọc và thời điểm.** Ba tệp cùng tên trong thư mục tải về là ba tệp không phân biệt được.

---

## 9. Bảng và tiếp cận

Bảng là nơi hay bị bỏ qua nhất về tiếp cận vì nó "chỉ là dữ liệu".

| Yêu cầu | Vì sao |
| --- | --- |
| Dùng thẻ bảng thật với ô tiêu đề đúng | Trình đọc màn hình đọc được tên cột khi di chuyển giữa các ô. Bảng dựng bằng thẻ chia khối thì không |
| Nút sắp xếp cho biết chiều hiện tại bằng thuộc tính ARIA | Người dùng bàn phím không thấy mũi tên nhỏ |
| Thao tác trong dòng phải tới được bằng phím Tab | Menu ba chấm chỉ mở bằng chuột là không dùng được |
| Bảng cuộn ngang phải hội thoại được bằng bàn phím | Vùng cuộn không focus được thì không cuộn được bằng phím |

Chi tiết: [`15-accessibility.md`](15-accessibility.md).

---

## 10. Kiểm chứng

- [ ] Sang trang 2 rồi đổi bộ lọc → về trang 1
- [ ] Đổi trang, sắp xếp, lọc → URL đổi theo; sao chép URL mở ở tab khác cho **đúng** màn hình đó
- [ ] Bấm Quay lại của trình duyệt → về đúng trạng thái trước, không mất bộ lọc
- [ ] Gõ nhanh vào ô tìm kiếm → chỉ một request cuối cùng có hiệu lực; kết quả không về sai thứ tự
- [ ] Chọn vài dòng rồi đổi trang → lựa chọn còn, và số lượng hiển thị rõ
- [ ] Chọn vài dòng rồi đổi bộ lọc → lựa chọn bị xoá
- [ ] Thao tác hàng loạt hỏi xác nhận có **số lượng** trong câu
- [ ] Trạng thái rỗng do chưa có dữ liệu và rỗng do bộ lọc dùng hai câu khác nhau
- [ ] Đổi trang → dữ liệu cũ mờ đi, bảng không nhảy
- [ ] Xuất khi đang ở trang 3 có bộ lọc → tệp chứa **toàn bộ** kết quả khớp bộ lọc
- [ ] Cấu hình cột đã lưu tham chiếu một cột đã bị bỏ → bảng vẫn hiển thị bình thường

---

## 11. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Data grid server-side dùng chung | ✅ sẽ có | §1 — mặc định cho mọi bảng, kể cả bảng ít dữ liệu |
| Trang bắt đầu từ 1; không gửi số trang qua dây | ✅ sẽ có | §2.1, §2.2 |
| Trạng thái bảng đặt trên URL | ✅ sẽ có | §3 |
| Ba cơ chế chống gọi dồn dập khi tìm kiếm | ✅ sẽ có | §4 — cơ chế huỷ request cũ là cái hay quên nhất |
| Bốn trạng thái bắt buộc của bảng | ✅ sẽ có | §7 |
| Chọn nhiều dòng, phạm vi trang hiện tại | ✅ sẽ có | §5 |
| Ẩn / hiện cột, lưu theo máy | ✅ sẽ có | Mức 1 ở §6 |
| Đổi thứ tự và độ rộng cột | ❌ chưa | Điều kiện: có yêu cầu thật (mức 2 ở §6) |
| Cấu hình cột theo tài khoản, đồng bộ giữa máy | ❌ chưa | Điều kiện: có endpoint và hợp đồng (mức 3 ở §6) |
| Xuất dữ liệu theo bộ lọc đang xem | ❌ chưa | Điều kiện: nghiệp vụ đầu tiên cần. Dựng tệp ở server — [`../be/15-import-export.md`](../be/15-import-export.md) |
| Bảng chỉnh sửa tại chỗ | ✅ sẽ có | Component riêng [`../../Design/Components/EditableGrid.md`](../../Design/Components/EditableGrid.md), **không** phải một chế độ của lưới đọc — hai thứ có nguồn sự thật ngược nhau. Điều kiện cấp bởi [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md). Bài toán lưu từng ô và xung đột vẫn để ngỏ, ghi ở `Cần chốt` của chính spec đó |
| Lọc và phân trang ở client | ❌ loại, không hoãn `K32` | §1 — chạy hoàn hảo với dữ liệu thử, hỏng với dữ liệu thật |

Một finding dạng *"FE thiếu X"* chỉ hợp lệ khi X mang trạng thái **✅ sẽ có** mà vắng mặt, hoặc khi điều kiện ở cột ghi chú của một dòng **❌ chưa** đã xảy ra. Dòng **❌ loại, không hoãn** chỉ đổi được bằng một ADR mới, không đổi được bằng một finding.

---

## 12. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Hợp đồng phân trang | [`../../contracts/README.md`](../../contracts/README.md) |
| Truy vấn và chỉ mục phía BE | [`../../quy-uoc/be-performance.md`](../../quy-uoc/be-performance.md) |
| Xuất dữ liệu phía BE | [`../be/15-import-export.md`](../be/15-import-export.md) |
| State trên URL | [`03-state-management.md`](03-state-management.md) §1 |
| Bọc thư viện bảng | [`05-component-library.md`](05-component-library.md) §2 |
| Virtual scroll khi nào cần | [`13-performance.md`](13-performance.md) §6 |
