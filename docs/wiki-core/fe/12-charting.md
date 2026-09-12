---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 12. Biểu đồ

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG**, và với riêng chủ đề này còn thêm một tầng nữa: **Core cấp component biểu đồ, nhưng KHÔNG gắn sẵn thư viện biểu đồ nào vào bundle khởi động**. Xem §2.
>
> 📖 Component: [`../../Design/Components/Chart.md`](../../Design/Components/Chart.md) · Giá trị màu: [`../../Design/DESIGN.md`](../../Design/DESIGN.md) §2.8 · Quyết định và ba ràng buộc: [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md).
>
> File này tồn tại để trả lời trước một câu hỏi chắc chắn sẽ được hỏi — *"vẽ biểu đồ bằng gì"* — và để câu trả lời không phải là một quyết định vội vàng lúc đang gấp.

---

## 1. Khi nào thật sự cần biểu đồ

Biểu đồ đáng có khi câu hỏi của người dùng là về **hình dạng** của dữ liệu, không về **giá trị** của nó.

| Câu hỏi của người dùng | Trả lời tốt nhất bằng |
| --- | --- |
| "Tháng này bao nhiêu đơn?" | Một con số lớn |
| "Ai chưa hoàn thành?" | Một bảng lọc được |
| "Xu hướng ba tháng qua đi lên hay xuống?" | Biểu đồ |
| "Ba nhóm này chênh nhau nhiều không?" | Biểu đồ |
| "Bản ghi số 4712 có gì?" | Một màn chi tiết |

**Ba dòng đầu và dòng cuối chiếm phần lớn nhu cầu của một hệ quản trị.** Đó là lý do một Core cho hệ quản trị không cần biểu đồ, trong khi một sản phẩm phân tích thì cần ngay từ đầu.

Cạm bẫy thường gặp: đưa biểu đồ vào một màn tổng quan vì màn tổng quan "trông phải có biểu đồ". Một biểu đồ không trả lời câu hỏi nào là chỗ chiếm màn hình và tốn bảo trì.

---

## 2. Vì sao Core này CHƯA gắn thư viện biểu đồ

### 2.1 Không màn Core nào cần

Các màn Core là: đăng nhập, đổi mật khẩu, danh sách người dùng, ma trận phân quyền, và một trang đích. Không màn nào trong số đó có câu hỏi về hình dạng dữ liệu.

### 2.2 THƯ VIỆN biểu đồ không vào bundle khởi động

> 🔄 **Mục này đã đổi phạm vi.** Nó từng kết luận cả *nhu cầu* biểu đồ thuộc module. [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md) lật vế đó: **component** biểu đồ thuộc Core. Hai lập luận dưới đây vẫn đứng vững, nhưng nay chúng chỉ áp cho **thư viện**, không áp cho component.

Đây là lập luận quyết định, không phải "để sau cho gọn".

Một biểu đồ luôn gắn với một câu hỏi nghiệp vụ cụ thể: xu hướng của *cái gì*, phân bố theo *tiêu chí nào*. Đó là kiến thức của module. Nếu Core **cài sẵn thư viện biểu đồ vào bundle khởi động**, hai chuyện xảy ra:

1. **Core gánh một phụ thuộc cho một tính năng nó không dùng** — mỗi lần nâng Angular lại phải kiểm tính tương thích của một thư viện không màn Core nào chạm tới ([`16-nen-tang-va-nang-cap.md`](16-nen-tang-va-nang-cap.md) §3).
2. **Mọi dự án trả giá bundle cho một tính năng phần lớn trong số đó không bật.** Mọi biến thể của [`../../Design/Components/Chart.md`](../../Design/Components/Chart.md) trừ `line` đều tự dựng bằng HTML/CSS và không cần thư viện nào.

**Lập luận thứ hai trước đây còn có một vế nữa** — rằng component biểu đồ dùng chung sẽ tiến hoá theo nhu cầu của module đầu tiên, mang tên gọi và đơn vị của module đó. Vế đó vẫn là rủi ro thật, nhưng nay nó được chặn bằng một ràng buộc thay vì bằng việc không có component: [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md) ràng buộc 3 cấm từ vựng nghiệp vụ trong ba component đó, và *Điều kiện lật* của ADR coi lần rò thứ ba là dấu hiệu phải xem lại toàn bộ quyết định.

Ở dự án tiền nhiệm, một thư viện biểu đồ được cài cho đúng một component của đúng một module. Module đó bị gỡ, và thư viện vẫn nằm trong danh sách phụ thuộc thêm một thời gian nữa — không ai dùng, vẫn phải nâng cấp.

### 2.3 Vì sao KHÔNG "cài sẵn cho tiện"

Lập luận thường gặp là: cài sẵn thì module đầu tiên khỏi phải quyết định gì. Nó sai ở ba chỗ:

| Lập luận | Vì sao không đúng |
| --- | --- |
| "Cài sẵn thì module khỏi chọn" | Chọn thư viện biểu đồ là quyết định **nên** thuộc về người biết dữ liệu sẽ vẽ. Chọn hộ từ trước là chọn mù |
| "Chỉ vài chục kilobyte" | Chi phí thật không phải bundle mà là **nâng cấp**: một phụ thuộc nữa phải kiểm tương thích ở mỗi lần nâng framework, mãi mãi |
| "Sau này chắc chắn cần" | "Chắc chắn cần" không phải là "đang cần". Ở dự án tiền nhiệm, thứ chắc chắn cần đó đã bị gỡ đi sau khi module dùng nó biến mất |

### 2.4 Điều kiện để cài THƯ VIỆN

> 🔄 **Mục này đã đổi phạm vi cùng §2.2.** Nó từng nói cả chỗ đặt *component*; [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md) lật vế đó — component ở Core, tại [`../../Design/Components/Chart.md`](../../Design/Components/Chart.md). Điều kiện dưới đây chỉ còn áp cho **thư viện**.

Cài khi màn **đầu tiên** dùng biến thể `line` thật sự cần, và khi đó:

- Phụ thuộc được cài kèm lý do ghi ngay cạnh chỗ khai.
- **Nạp theo yêu cầu, không vào bundle khởi động** — ADR-0019 ràng buộc 1. Dự án không vẽ đường không tải một byte nào của nó.
- Mọi biến thể `Chart` trừ `line` tự dựng bằng HTML/CSS và **không** chạm tới thư viện này.

---

## 3. Chọn thư viện thế nào — tiêu chí, không phải tên

Khi tới lúc phải chọn, hỏi theo thứ tự này. Ba câu đầu thường đã loại được phần lớn ứng viên.

| # | Câu hỏi | Vì sao hỏi trước |
| --- | --- | --- |
| 1 | Thư viện UI đang dùng đã có component biểu đồ chưa? | Nếu có, dùng nó là tránh được một phụ thuộc nữa và một hệ theme nữa |
| 2 | Nó có bọc được sau `shared/ui` không? | Nếu không bọc được thì luật F5 bị vi phạm ngay từ đầu |
| 3 | Nó nặng bao nhiêu, và có tải theo nhu cầu được không? | Biểu đồ hiếm khi ở màn đầu tiên; nó phải nằm trong lazy chunk |
| 4 | Nó nhận màu từ biến CSS được không? | Nếu phải truyền mã màu cứng thì hệ token bị chọc thủng ở đúng chỗ nhiều màu nhất |
| 5 | Vẽ bằng canvas hay SVG? | Quyết định thẳng khả năng tiếp cận — §4 |
| 6 | Nó có API kiểu TypeScript thật không? | Cấu hình biểu đồ là những object lồng nhau sâu; không có kiểu thì mọi sai sót chỉ lộ lúc chạy |

**Đừng chọn theo số ngôi sao hay theo danh sách tính năng.** Một thư viện có mọi loại biểu đồ nhưng không đọc được biến CSS sẽ khiến mọi biểu đồ trong app nằm ngoài hệ token — và đó là một khoản nợ lan ra mọi màn có biểu đồ.

---

## 4. Khả năng đọc và tiếp cận

> Đây là phần đáng đọc nhất của file này, và là phần bị bỏ qua nhiều nhất khi thêm biểu đồ.

### 4.1 Canvas không đọc được bằng trình đọc màn hình

Biểu đồ vẽ trên canvas **không có cấu trúc DOM nào** để trình đọc màn hình bám vào. Với người dùng trình đọc màn hình, một canvas là một vùng trống tuyệt đối: không đọc được trục, không đọc được điểm dữ liệu, không đọc được xu hướng.

Đây **không** phải lỗi cấu hình — đó là bản chất của canvas, và không sửa được bằng cách thêm thuộc tính lên chính phần tử canvas.

Hai lớp bù, áp cho biểu đồ mang thông tin nghiệp vụ (không cần cho biểu đồ trang trí):

**(1) Một câu tóm tắt kết luận**, không phải mô tả hình dạng. "Đường màu xanh đi lên" là vô dụng; "Doanh số tăng đều từ 62 lên 88 trong sáu tuần" là thứ người ta cần. Câu này phải được **tính từ chính dữ liệu** đang vẽ, không phải chuỗi tĩnh viết tay — hai bên lệch nhau còn tệ hơn không có gì.

**(2) Một bảng dữ liệu thay thế**, ẩn khỏi thị giác nhưng **giữ lại cho công nghệ hỗ trợ**. Phải dùng kỹ thuật ẩn đúng: ẩn bằng cách gỡ khỏi bố cục hoặc gỡ khỏi DOM thì trình đọc màn hình cũng bỏ qua, tức không bù được gì.

SVG khá hơn canvas ở điểm này (có DOM để gắn nhãn), nhưng vẫn không tự nhiên là biểu đồ đọc được — vẫn cần hai lớp bù trên.

### 4.2 Màu không được là kênh thông tin duy nhất

Khoảng một phần hai mươi nam giới có khó khăn phân biệt màu. Một biểu đồ phân biệt các đường **chỉ bằng màu** là một biểu đồ nhóm người đó không đọc được.

Bù bằng: kiểu nét khác nhau, ký hiệu điểm khác nhau, hoặc nhãn đặt trực tiếp cạnh đường thay vì chú giải ở xa. Nhãn trực tiếp thường là giải pháp tốt nhất — nó cũng làm biểu đồ dễ đọc hơn cho tất cả mọi người.

### 4.3 Bảng màu biểu đồ thuộc hệ token

Màu của biểu đồ là quyết định thiết kế như mọi màu khác, và thuộc [`../../Design/DESIGN.md`](../../Design/DESIGN.md). Chuỗi màu mặc định của thư viện gần như chắc chắn không khớp bảng màu sản phẩm, và tệ hơn: nó không đổi theo chế độ tối.

---

## 5. Bốn thứ phải làm đúng khi thật sự vẽ biểu đồ

| Việc | Sai thì sao |
| --- | --- |
| Component biểu đồ là **dumb** — nhận dữ liệu đã ánh xạ qua `input()` | Component biết `HttpClient` là component không test được và không dùng lại được ([`05-component-library.md`](05-component-library.md) §5) |
| Cập nhật bằng **đối tượng dữ liệu mới**, không sửa tại chỗ | Sửa mảng tại chỗ thì signal không phát hiện đổi và biểu đồ đứng im — không có lỗi nào được ném ra |
| **Không** bọc biểu đồ trong điều kiện chỉ để ép vẽ lại | Nó huỷ và dựng lại toàn bộ, tốn và giật. Chi phí tăng theo tần suất làm mới |
| Biểu đồ co nhỏ thì **đổi cách trình bày**, không thu nhỏ mù | Mười hai nhãn trục nhét vào màn hình điện thoại thành một dải chữ chồng lên nhau |

Về dòng cuối, ba cách xử lý theo thứ tự ưu tiên: giảm số điểm hiển thị trên màn hẹp; đổi loại biểu đồ hoặc gộp kỳ lớn hơn; và chỉ khi hai cách trên không đủ mới thêm khả năng kéo/thu phóng — vì nó thêm một phụ thuộc và thêm một thao tác người dùng phải học.

---

## 6. Chọn loại biểu đồ theo câu hỏi, không theo thẩm mỹ

Sai lầm phổ biến là chọn loại biểu đồ trước rồi nhét dữ liệu vào. Ngược lại mới đúng: câu hỏi quyết định loại.

| Câu hỏi | Loại | Bẫy |
| --- | --- | --- |
| Thay đổi theo thời gian | Đường | Nối các điểm không đều nhau về thời gian bằng đường thẳng là ngụ ý một xu hướng không có thật |
| So sánh các nhóm rời rạc | Cột | **Trục giá trị phải bắt đầu từ 0.** Cắt trục làm chênh lệch 3% trông như chênh lệch gấp đôi |
| Tỷ trọng của một tổng thể | Cột chồng hoặc thanh 100% | Biểu đồ tròn chỉ đọc được với rất ít phần; quá số đó thì không ai so được các lát bằng mắt |
| Phân bố của một tập giá trị | Cột tần suất | Chọn độ rộng nhóm khác nhau cho ra hình dạng khác hẳn — phải nói rõ đang nhóm theo gì |
| Quan hệ giữa hai đại lượng | Điểm phân tán | Không suy ra nhân quả từ hình dạng |

**Ba điều luôn phải có, bất kể loại nào:** đơn vị (phần trăm hay số tuyệt đối), phạm vi thời gian, và **thời điểm dữ liệu được cập nhật lần cuối**. Thiếu điều thứ ba, người dùng không biết mình đang nhìn số của hôm nay hay của tuần trước — và họ sẽ mặc định là hôm nay.

**Trục giá trị cắt gốc là cách bóp méo dữ liệu phổ biến nhất và ít bị nhận ra nhất.** Nếu buộc phải cắt (dữ liệu dao động trong một dải hẹp ở giá trị cao), phải đánh dấu rõ ràng rằng trục bị cắt.

---

## 7. Định dạng số trên biểu đồ

Số trên biểu đồ chịu cùng luật với số ở mọi nơi khác: **định dạng theo văn hoá đang chọn** ([`08-i18n.md`](08-i18n.md) §6). Điều này hay bị quên vì cấu hình biểu đồ nằm trong một object riêng, xa các pipe của Angular.

| Việc | Cụ thể |
| --- | --- |
| Nhãn trục và chú thích công cụ | Dùng cùng cơ chế định dạng với phần còn lại của app |
| Rút gọn số lớn | Rút gọn ở nhãn trục cho gọn, nhưng **chú thích công cụ phải hiện số đầy đủ** |
| Nhãn trục thời gian | Theo locale; và phải đổi khi người dùng đổi ngôn ngữ lúc chạy |
| Giá trị thiếu | Phân biệt "bằng 0" với "không có dữ liệu". Vẽ 0 cho một kỳ chưa có dữ liệu là báo cáo sai |

Dòng cuối là lỗi dữ liệu, không phải lỗi hiển thị — nhưng biểu đồ là nơi nó lộ ra rõ nhất, và cũng là nơi nó gây hiểu nhầm nặng nhất.

---

## 8. Ô số liệu — thứ thường được cần thay vì biểu đồ

Trước khi thêm một thư viện biểu đồ, hỏi xem thứ đang cần có phải chỉ là **một con số lớn** không. Rất thường là vậy.

Một ô số liệu gồm: nhãn, giá trị, và (tuỳ chọn) mức thay đổi so với kỳ trước. Nó dựng bằng HTML và token màu, **không cần thư viện nào**, không cần canvas, và trình đọc màn hình đọc được tự nhiên.

| Ưu điểm so với biểu đồ | Cụ thể |
| --- | --- |
| Không phụ thuộc | Không thêm gì vào bundle |
| Đọc được ngay | Người dùng không phải giải mã hình |
| Tiếp cận sẵn | Chỉ là chữ — không cần lớp bù nào ở §4.1 |
| In ra vẫn đúng | Không phụ thuộc màu |

Ba ràng buộc nếu có mức thay đổi: nói rõ **so với kỳ nào**; **không dùng riêng màu** để chỉ tăng/giảm (thêm dấu hoặc mũi tên); và cẩn thận với phần trăm thay đổi khi giá trị gốc rất nhỏ — tăng từ 1 lên 3 là "tăng 200%", một con số đúng về toán và gây hiểu nhầm về nghĩa.

Đường phát triển đúng thường là: **con số → bảng → biểu đồ**, thêm bậc sau khi bậc trước không đủ. Nhảy thẳng tới bậc cuối là cách thêm chi phí trước khi biết có cần không.

---

## 9. Kiểm chứng — dùng khi biểu đồ đầu tiên ra đời

- [ ] Thư viện biểu đồ được khai trong phụ thuộc của **module**, có ghi lý do; `shared/` và `core/` không nhắc tới nó
- [ ] Biểu đồ nằm trong lazy chunk, không trong bundle khởi động
- [ ] Màu biểu đồ đến từ token, đổi theo chế độ sáng/tối
- [ ] Có câu tóm tắt kết luận, tính từ chính dữ liệu đang vẽ
- [ ] Có bảng dữ liệu thay thế, ẩn thị giác nhưng trình đọc màn hình đọc được
- [ ] Các chuỗi dữ liệu phân biệt được khi in đen trắng
- [ ] Component biểu đồ không inject service lấy dữ liệu
- [ ] Làm mới dữ liệu liên tục không làm biểu đồ dựng lại từ đầu

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Ô số liệu dựng bằng HTML và token | ✅ sẽ có | §8 — thứ thường được cần thay vì biểu đồ |
| Thư viện biểu đồ trong một **module** nghiệp vụ | ❌ chưa | Điều kiện: màn nghiệp vụ đầu tiên cần. Tiêu chí chọn ở §3 |
| Component biểu đồ **thuộc Core** | ✅ đã có | [`../../Design/Components/Chart.md`](../../Design/Components/Chart.md). Điều kiện cấp bởi [`../../adr/0019-ba-component-nang-thuoc-core.md`](../../adr/0019-ba-component-nang-thuoc-core.md), **đi trước** ngưỡng hai module — cái giá ghi ở mục *Hệ quả tiêu cực* của ADR |
| Hai lớp bù tiếp cận cho biểu đồ | ✅ đã có | `Chart.md` bắt buộc bảng số liệu tương đương ngay ở mục *Khi nào dùng*, và bắt nhãn số hiện rõ cho ba màu dưới ngưỡng tương phản (§4.1) |
| Bảng màu biểu đồ trong hệ token | ✅ đã có | [`../../Design/DESIGN.md`](../../Design/DESIGN.md) §2.8 — `--chart-1`…`--chart-8`, thang mức độ, dải hai chiều, đã chạy qua phép kiểm bằng máy ở cả hai theme (§4.3) |
| Kéo và thu phóng biểu đồ | ❌ chưa | Điều kiện: giảm số điểm và đổi loại biểu đồ đều không đủ (§5) |
| Thư viện biểu đồ trong **bundle khởi động** | ❌ loại, không hoãn `K33` | §2.2, §2.3 — mọi biến thể trừ `line` tự dựng, nên cài sẵn là bắt mọi dự án trả giá cho thứ phần lớn không bật. ADR-0019 ràng buộc 1 |
| Biểu đồ không trả lời câu hỏi nào | ❌ loại, không hoãn `K34` | §1 — chiếm màn hình và tốn bảo trì |
| Trục giá trị cắt gốc mà không đánh dấu | ❌ loại, không hoãn `K35` | §6 — bóp méo dữ liệu |
| Màu là kênh thông tin duy nhất | ❌ loại, không hoãn `K36` | §4.2 |

Một finding dạng *"FE thiếu X"* chỉ hợp lệ khi X mang trạng thái **✅ sẽ có** mà vắng mặt, hoặc khi điều kiện ở cột ghi chú của một dòng **❌ chưa** đã xảy ra. Dòng **❌ loại, không hoãn** chỉ đổi được bằng một ADR mới, không đổi được bằng một finding.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Ngưỡng Nhóm A / Nhóm B | [`01-core-components.md`](01-core-components.md) §3 |
| Ranh giới Core ↔ Module | [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §4 |
| Bọc thư viện UI, luật F5 | [`05-component-library.md`](05-component-library.md) §2 |
| Bảng màu và token | [`04-design-token-system.md`](04-design-token-system.md) |
| Tiếp cận nói chung | [`15-accessibility.md`](15-accessibility.md) |
| Chi phí phụ thuộc khi nâng cấp | [`16-nen-tang-va-nang-cap.md`](16-nen-tang-va-nang-cap.md) §3 |
