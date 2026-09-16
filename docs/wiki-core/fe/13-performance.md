---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 13. Hiệu năng Frontend

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`, nên **chưa có số đo nào**. Mọi ngưỡng trong file này được mô tả bằng *cách chọn ngưỡng*, không bằng con số — xem §4.
>
> Đo và tối ưu phía BE: [`../../quy-uoc/be-performance.md`](../../quy-uoc/be-performance.md). Số đo trải nghiệm: [`10-observability.md`](10-observability.md) §6.

---

## 1. Nguyên tắc: không tối ưu khi chưa đo

Tối ưu sớm ở FE đặc biệt tốn, vì phần lớn "tối ưu" làm code khó đọc mà không đổi được gì người dùng cảm nhận được.

Nhưng có một nhóm ngoại lệ quan trọng: **những quyết định đắt để đảo ngược**. Chúng phải làm đúng từ đầu dù chưa đo được gì.

| Nhóm | Ví dụ | Làm khi nào |
| --- | --- | --- |
| **Đắt để đảo ngược** | Lazy-load theo route, chiến lược phát hiện thay đổi, phân trang ở server | **Ngay từ đầu** |
| Rẻ để làm sau | Tối ưu một danh sách cụ thể, hoãn tải một ảnh, nhớ kết quả một phép tính | Sau khi đo |

Ba thứ ở nhóm đầu có chung tính chất: chúng là **quyết định kiến trúc đội lốt tối ưu hoá**. Đổi chúng sau nghĩa là viết lại màn hình.

---

## 2. Zoneless, `OnPush` và signal

### 2.1 Bật zoneless ngay từ đầu

Phiên bản Angular đã chốt ([`../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md)) chạy được không cần thư viện vá bất đồng bộ. Bật ngay ở lần cấu hình đầu tiên, **không** để mặc định rồi gỡ sau.

Vì sao không gỡ sau: mọi component viết trong lúc còn chạy chế độ cũ đều **chưa được kiểm chứng** dưới chế độ mới. Khi gỡ, lỗi lộ ra rải rác ở nhiều màn cùng lúc thay vì tập trung — và mỗi lỗi trông như một lỗi riêng biệt.

Bật ngay từ đầu thì mỗi component được kiểm ngay khi viết, và lỗi lộ ra từng cái một, ngay tại chỗ.

### 2.2 `OnPush` là mặc định, không phải tối ưu hoá

Mọi component khai `ChangeDetectionStrategy.OnPush`. Đây là mặc định của repo, không phải thứ thêm vào khi thấy chậm.

Với zoneless và signal, `OnPush` gần như là hệ quả tự nhiên: signal tự báo cho Angular biết chỗ nào cần vẽ lại, nên phát hiện thay đổi rộng khắp không còn tác dụng gì ngoài tốn công.

### 2.3 Bẫy lớn nhất: sửa mảng hoặc object tại chỗ

```typescript
this.items().push(newItem);          // ❌ signal không phát hiện gì — màn hình đứng im
this.items.update(xs => [...xs, newItem]);   // ✅
```

Đây là lỗi hay gặp nhất khi chuyển sang signal, và triệu chứng của nó là thứ khó tin nhất: **không có lỗi nào cả**, dữ liệu đúng trong bộ nhớ, chỉ có màn hình không đổi. Người ta thường nghi ngờ mọi thứ khác trước khi nghi ngờ dòng này.

Quy tắc: **signal giữ giá trị bất biến.** Muốn đổi thì tạo giá trị mới.

### 2.4 `computed()` chứ không phải tính trong template

Một biểu thức trong template chạy lại mỗi lần component được kiểm. Một `computed()` chỉ tính lại khi phụ thuộc của nó đổi, và nhớ kết quả.

Chi phí thật không nằm ở một biểu thức đơn giản mà ở **hàm gọi trong template**: một hàm lọc danh sách gọi từ template sẽ chạy lại rất nhiều lần, và nếu nó tạo mảng mới mỗi lần thì nó cũng làm mọi thứ phía dưới vẽ lại theo.

---

## 3. Lazy-load theo route

### 3.1 Luật

**Mọi route ngoài shell và màn đăng nhập đều lazy.** Không có ngoại lệ "màn này nhỏ nên để chung".

Vì sao không có ngoại lệ: bundle khởi động là thứ **mọi** người dùng tải, kể cả người không bao giờ vào màn đó. Một màn nhỏ hôm nay là một màn to sáu tháng nữa, và lúc đó không ai đi tách lại.

### 3.2 Lazy cả nhánh, không lazy từng màn

Nhóm theo nhánh chức năng, không tách từng màn thành một chunk riêng. Quá nhiều chunk nhỏ đổi một vấn đề (bundle to) lấy một vấn đề khác (quá nhiều request nhỏ, và một khoảng trắng mỗi lần chuyển màn).

Kinh nghiệm thực dụng: một chunk cho một nhánh chức năng mà người dùng thường ở lại trong đó.

### 3.3 Tải trước có chọn lọc

Sau khi shell đã hiển thị và người dùng đang đọc, tải trước nhánh **có khả năng cao** được vào (thường là nhánh mặc định sau đăng nhập). Đừng tải trước tất cả — làm thế là quay về không lazy, chỉ chậm hơn một nhịp.

### 3.4 Chỗ rò rỉ hay gặp

Một import ở tầng chung kéo một thư viện nặng vào bundle khởi động, dù chỉ một màn lazy dùng nó. Ví dụ điển hình: một `index.ts` gom xuất mọi thứ trong `shared/`, khiến import một component kéo theo cả nhóm.

**Cách phát hiện:** phân tích thành phần bundle sau mỗi lần thêm phụ thuộc. Cách phòng: không dùng tệp gom xuất cho `shared/` — import thẳng từ đường dẫn của component.

---

## 4. Ngân sách bundle — chọn ngưỡng theo số đo, không giữ mặc định

### 4.1 Vì sao ngưỡng mặc định là vô nghĩa

Công cụ tạo dự án đặt sẵn một ngưỡng. Con số đó không biết gì về ứng dụng này. Giữ nguyên nó có hai kết cục, và cả hai đều xấu:

- **Quá chặt** → cảnh báo vàng ở mọi lần build. Người ta học cách bỏ qua, và cổng chết trong im lặng.
- **Quá lỏng** → không bao giờ báo. Bundle phình dần và không ai biết cho tới khi nó đã to.

Ở dự án tiền nhiệm, ngưỡng mặc định được giữ nguyên trong khi chính tài liệu yêu cầu chỉnh theo số đo. Khi cuối cùng có người đo, dư địa còn lại chỉ vài kilobyte — ngưỡng đó đã mất tác dụng cảnh báo từ rất lâu mà không ai nhận ra.

### 4.2 Quy trình chọn ngưỡng

1. **Đo** kích thước hiện tại ở cuối pha F1 và cuối pha F3.
2. **Đặt ngưỡng cảnh báo có dư địa thật** so với số đo — đủ cho vài lượt phát triển nữa.
3. **Đặt ngưỡng lỗi ở mốc thật sự không chấp nhận được**, không phải ở ngay trên ngưỡng cảnh báo.
4. **Ghi lại số đo và lý do** ngay cạnh chỗ khai ngưỡng.
5. **Xem lại khi vượt**, và mỗi lần nâng ngưỡng phải kèm lý do.

**Vì sao không đặt ngưỡng sát số đo hiện tại:** ngưỡng sát thì lần thêm thư viện kế tiếp lại vàng, và người ta lại nâng — ngưỡng trở thành thứ **chạy theo** bundle thay vì **ràng buộc** nó.

### 4.3 Lỗ mù phải biết trước

Ngân sách thường chỉ khai cho phần khởi động. **Lazy chunk không bị ràng buộc gì** — một chunk lazy có thể lớn hơn cả bundle khởi động và đi qua cổng im lặng.

Điều này **đã xảy ra thật** ở dự án tiền nhiệm: một nhánh quản trị nặng hơn bundle khởi động vì một component bảng của thư viện UI chỉ được dùng ở đó.

Cách xử lý ở repo này: khai thêm ngân sách cho lazy chunk ngay khi có lazy chunk đầu tiên. Không khai được thì phải ghi lỗ mù này ra trong [`trien-khai/05-gate.md`](trien-khai/05-gate.md), để không ai tưởng cổng phủ hết.

### 4.4 Ngân sách cho style của component

Cũng là một ngưỡng, cũng phải chọn theo số đo. Một điều phải biết trước: **ngưỡng này áp chung cho mọi tệp style component**, không đặt riêng cho từng tệp được. Nên khi một component phức tạp vượt ngưỡng, lựa chọn là nhị phân: nới cho tất cả, hoặc chia tệp đó ra.

Chia một component thành hai tệp style chỉ để lách ngưỡng thường là lựa chọn tệ hơn: người sửa component đó phải nhớ mở cả hai tệp. Nếu tệp lớn vì component thật sự phức tạp (nhiều trạng thái, nhiều điểm ngắt) thì nới ngưỡng là câu trả lời đúng — kèm ghi lại lý do.

---

## 5. Trạng thái tải và dịch chuyển bố cục

Nội dung nhảy khi dữ liệu về là vấn đề **cảm nhận** rõ hơn nhiều so với vài trăm mili giây tải.

| Việc | Vì sao |
| --- | --- |
| Skeleton giữ **đúng** kích thước nội dung sẽ thay thế nó | Skeleton sai kích thước còn gây nhảy nhiều hơn không có skeleton |
| Ảnh khai sẵn tỉ lệ hoặc kích thước | Ảnh không khai kích thước làm cả trang nhảy khi nó tải xong |
| Vùng thông báo có chỗ dành sẵn | Thông báo chèn vào giữa luồng đẩy nội dung xuống đúng lúc người dùng sắp bấm |
| Tải lại dữ liệu thì giữ nội dung cũ mờ đi | Thay bằng skeleton ở mỗi lần đổi trang gây nhấp nháy — [`11-grid-and-metadata.md`](11-grid-and-metadata.md) §7 |

---

## 6. Ảnh và tài nguyên tĩnh

| Việc | Ghi chú |
| --- | --- |
| Hoãn tải ảnh ngoài màn hình đầu | Ảnh ở màn hình đầu thì **không** hoãn — hoãn nó là làm chậm đúng thứ được đo |
| Khai kích thước cho mọi ảnh | Chống dịch chuyển bố cục |
| Dùng định dạng ảnh hiện đại | Có dự phòng cho trình duyệt trong phạm vi đã khai ([`16-nen-tang-va-nang-cap.md`](16-nen-tang-va-nang-cap.md) §5) |
| Biểu tượng dùng SVG hoặc bộ icon của thư viện | Không nhúng ảnh bitmap cho biểu tượng |
| Font tự phục vụ, có hiển thị dự phòng trong lúc tải | Chặn hiển thị chờ font là một khoảng trắng người dùng thấy được |

---

## 7. Virtual scroll — chỉ khi thật sự cần

Virtual scroll chỉ vẽ những dòng đang trong tầm nhìn. Nó giải một bài toán có thật, và tạo ra ba bài toán mới: tìm kiếm trong trang của trình duyệt không thấy nội dung chưa vẽ; chiều cao dòng thay đổi làm thanh cuộn nhảy; và in ấn chỉ ra phần đang hiển thị.

**Ngưỡng:** chỉ dùng khi thật sự phải hiển thị hàng nghìn dòng **cùng lúc** và **không phân trang được** (ví dụ một danh sách cần cuộn liên tục theo bản chất).

Với hệ quản trị, câu trả lời gần như luôn là **phân trang ở server** ([`11-grid-and-metadata.md`](11-grid-and-metadata.md) §1), không phải virtual scroll. Virtual scroll giải bài toán vẽ; nó **không** giải bài toán tải hàng nghìn bản ghi về trình duyệt.

---

## 8. Đo bằng gì trước khi tối ưu

Bốn công cụ, mỗi cái trả lời một câu hỏi khác nhau. Dùng nhầm công cụ là lý do phổ biến khiến người ta tối ưu sai chỗ.

| Câu hỏi | Công cụ | Bẫy khi đọc kết quả |
| --- | --- | --- |
| "Cái gì làm bundle to?" | Phân tích thành phần bundle sau build | Nhìn kích thước **sau nén**, không nhìn kích thước thô — chênh lệch rất lớn và gây kết luận sai |
| "Vì sao thao tác này giật?" | Bộ ghi hiệu năng của trình duyệt | Đo trên máy dev mạnh là đo trong điều kiện tốt nhất; giả lập CPU chậm mới thấy vấn đề thật |
| "Component nào vẽ lại nhiều?" | Công cụ dành cho Angular | Vẽ lại nhiều không tự nó là vấn đề — vấn đề là vẽ lại **tốn** |
| "Người dùng thật thấy thế nào?" | Số đo trải nghiệm — [`10-observability.md`](10-observability.md) §6 | Số trung bình che mất phần đuôi; nhóm người dùng chậm nhất mới là nhóm cần quan tâm |

### 8.1 Ba luật khi đo

1. **Đo trước, đo sau, cùng điều kiện.** Không có số trước thì không chứng minh được gì đã cải thiện, và một "tối ưu" làm chậm đi sẽ không bị phát hiện.
2. **Đo với dữ liệu thật về khối lượng.** Một bảng nhanh với 20 dòng nói lên rất ít về hành vi của nó với 2.000 dòng.
3. **Ghi lại số đo cùng lý do**, ngay cạnh chỗ đặt ngưỡng. Một con số không có bối cảnh sẽ bị đổi tuỳ tiện ở lần sau.

---

## 9. Kiểm chứng

- [ ] Ứng dụng chạy zoneless, bật từ lần cấu hình đầu
- [ ] Mọi component dùng `OnPush`
- [ ] Không chỗ nào sửa mảng hoặc object của signal tại chỗ
- [ ] Không hàm lọc/sắp xếp nào được gọi thẳng trong template
- [ ] Mọi route ngoài shell và đăng nhập đều lazy
- [ ] Phân tích bundle: không thư viện nặng nào lọt vào bundle khởi động
- [ ] Ngưỡng ngân sách được đặt theo số đo, có ghi lại số đo và lý do
- [ ] Có ngân sách cho lazy chunk, hoặc lỗ mù được ghi ra ở tài liệu cổng
- [ ] Mọi ảnh khai kích thước; skeleton giữ đúng chiều cao nội dung
- [ ] Đổi trang trong bảng không làm bố cục nhảy

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Zoneless và `OnPush` là mặc định | ✅ sẽ có | §2 — bật từ lần cấu hình đầu tiên, không gỡ sau |
| Lazy-load theo nhánh chức năng | ✅ sẽ có | §3 |
| Ngân sách bundle chọn theo số đo | ✅ sẽ có | Luật F14, bật ở pha F3 (§4.2) |
| Ngân sách cho lazy chunk | ✅ sẽ có | §4.3 — khai ngay khi có lazy chunk đầu tiên, hoặc ghi lỗ mù ra ở [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
| Skeleton đúng kích thước, ảnh khai kích thước | ✅ sẽ có | §5, §6 |
| Tải trước có chọn lọc | ❌ chưa | Điều kiện: đã đo và biết nhánh nào được vào nhiều nhất (§3.3) |
| Virtual scroll | ❌ chưa | Điều kiện: phải hiển thị hàng nghìn dòng cùng lúc và không phân trang được (§7) |
| Đẩy tính toán nặng sang web worker | ❌ chưa | Điều kiện: có số đo cho thấy một tính toán làm nghẽn tương tác |
| Giữ nguyên ngưỡng ngân sách mặc định | ❌ loại, không hoãn `K37` | §4.1 — con số đó không biết gì về ứng dụng này |
| Tệp gom xuất cho `shared/` | ❌ loại, không hoãn `K38` | §3.4 — kéo cả nhóm vào bundle khởi động |
| SSR để cải thiện thời gian tải | ❌ loại, không hoãn `K39` | [`01-core-components.md`](01-core-components.md) §4.2 — muốn lật thì viết ADR |

> Cách đọc ba ký hiệu của bảng trên — và khi nào *"FE thiếu X"* là finding: [`../README.md`](../README.md) §9.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Đo trải nghiệm người dùng thật | [`10-observability.md`](10-observability.md) §6 |
| Phân trang ở server | [`11-grid-and-metadata.md`](11-grid-and-metadata.md) |
| Hiệu năng phía BE | [`../../quy-uoc/be-performance.md`](../../quy-uoc/be-performance.md) |
| Phạm vi trình duyệt ảnh hưởng bundle thế nào | [`16-nen-tang-va-nang-cap.md`](16-nen-tang-va-nang-cap.md) §5 |
| Cache header cho tài nguyên tĩnh | [`17-phuc-vu-va-trien-khai.md`](17-phuc-vu-va-trien-khai.md) §3 |
| Cổng F14 (ngân sách) | [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
