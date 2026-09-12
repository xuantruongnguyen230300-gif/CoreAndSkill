---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0019 — Biểu đồ, lưới nhập liệu và chọn khoảng ngày thuộc Core

> **Trạng thái:** Đã chấp nhận (2026-09-11)
>
> **Quyết định này LẬT bốn luật đã có.** Danh sách chính xác ở mục *Cái bị lật*.

## Bối cảnh

[`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §1 khai bốn thứ **cố ý không có** trong thư viện Core: biểu đồ, trình soạn thảo văn bản đa dạng thức, bộ chọn ngày phức tạp có khoảng thời gian, và bảng có thể chỉnh sửa tại chỗ. Lý do viết ra rất rõ: cả bốn đều nặng, và chưa màn Core nào cần.

[`../wiki-core/fe/12-charting.md`](../wiki-core/fe/12-charting.md) §2.2 còn đi xa hơn cho riêng biểu đồ, và tự gọi lập luận của mình là *"lập luận quyết định, không phải 'để sau cho gọn'"*: nhu cầu biểu đồ thuộc module vì một biểu đồ luôn gắn với một câu hỏi nghiệp vụ cụ thể.

Một đợt bổ sung khu `docs/Design/` đã đưa **ba trên bốn** thứ đó vào `Components/`: `Chart`, `EditableGrid`, và `Input` biến thể `daterange`. Đợt đó không có ADR, nên repo rơi vào tình trạng hai file `kind: luat` nói ngược nhau mà không bản nào thắng — đúng dạng lỗi mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §3 mô tả: *agent đọc file nào trước thì tin file đó, và không bao giờ biết có mâu thuẫn*. Cổng `check-docs.sh` không bắt được vì đây là mâu thuẫn ngữ nghĩa.

ADR này tồn tại để chấm dứt tình trạng đó bằng cách chọn **một** bản.

## Quyết định

> **Ba component nói trên thuộc Core, với điều kiện chúng được dựng sao cho một dự án không dùng chúng KHÔNG trả giá gì.**

Vế sau không phải lời hứa suông — nó là ba ràng buộc kỹ thuật, ghi ở mục *Điều kiện kèm theo*.

### Vì sao lật — lập luận, không phải sở thích

Luật cũ dựa trên một câu đúng: *"đừng đưa vào Core thứ chỉ một module cần"*. ADR này không cãi câu đó. Nó cãi **tiền đề** rằng ba thứ này chỉ một module cần.

Core này không phải một thư viện UI đa dụng. Nó là **bộ khung cho phần mềm quản lý dùng trong cơ quan và doanh nghiệp Việt Nam** — [`../kien-truc-core-module.md`](../kien-truc-core-module.md) và toàn bộ khu [`../contracts/`](../contracts/) đều nói điều đó: đơn vị trực thuộc, phân quyền theo đơn vị, chứng từ, nhập xuất tệp, nhật ký kiểm toán. Trong lớp phần mềm đó, ba thứ này không phải tính năng của một module — chúng là **năng lực nền của mọi sản phẩm trong lớp**:

| Thứ | Vì sao mọi dự án trong lớp này đều cần |
| --- | --- |
| Biểu đồ | Mọi phần mềm quản lý đều có màn báo cáo, và mọi màn báo cáo đều có ít nhất một hình so sánh. Không có sản phẩm quản lý nào chỉ có bảng |
| Lưới nhập liệu | Chứng từ có dòng chi tiết, bảng lương có dòng nhân sự, kiểm kê có dòng vật tư. Không ai chịu mở bốn mươi hộp thoại để nhập bốn mươi dòng |
| Chọn khoảng ngày | Mọi màn danh sách nghiệp vụ đều lọc theo thời gian. Đây là thứ phổ biến hơn cả ba biến thể `Input` khác cộng lại |

**Đây là một khẳng định thực nghiệm về lớp bài toán, và nó sai được.** Mục *Điều kiện lật* nói rõ dấu hiệu nào chứng minh nó sai.

### Vì sao lật BÂY GIỜ, trước khi có module thật

Đây là chỗ yếu nhất của quyết định, nên nói thẳng thay vì giấu.

Luật cũ có một phép thử tốt — *"ngưỡng hai module"* ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2: một thứ lên Core khi **module thứ hai** cần nó. Repo hiện có **không** module nào. Theo phép thử đó thì ba component này chưa đủ điều kiện, và ADR này đang đi trước phép thử.

Lý do chấp nhận đi trước, cân với cái giá:

1. **Giai đoạn 1 của repo là giai đoạn viết tài liệu, không phải viết code.** Cái được đưa vào Core hôm nay là **một spec**, không phải một phụ thuộc npm. Chi phí của một spec sai thấp hơn nhiều so với chi phí của một thư viện sai — và nó gỡ ra bằng một lần xoá file, không bằng một lần gỡ phụ thuộc khỏi ba mươi màn.
2. **Cái giá của việc thiếu thì đắt hơn cái giá của việc thừa, ở đúng ba thứ này.** Không có spec chung thì dự án thứ nhất tự dựng lưới nhập liệu của nó, dự án thứ hai dựng lại, và hai bản đó khác nhau về phím tắt, về trạng thái ô chưa lưu, về cách xác thực. Hợp nhất hai bản đã lan ra đắt hơn nhiều so với gỡ một spec chưa ai dựng.
3. **Luật cũ tự để cửa.** Chính [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §1 kết bằng *"Thêm khi có màn thật cần, và cân nhắc đặt ở module thay vì Core nếu chỉ một module cần."* ADR này trả lời vế sau: không phải chỉ một module cần.

## Điều kiện kèm theo — ba ràng buộc, không phải lời hứa

Quyết định chỉ có hiệu lực khi cả ba được giữ. Vi phạm bất kỳ điều nào thì quyết định này **tự mất căn cứ**, và mục *Điều kiện lật* áp dụng.

| # | Ràng buộc | Vì sao khó ép — **trạng thái cổng ở [`../RULES.md`](../RULES.md)**, không ở đây |
| --- | --- | --- |
| 1 | **Không thư viện biểu đồ nào nằm trong bundle khởi động** — hướng khoá `K33`, khai ở [`../wiki-core/fe/12-charting.md`](../wiki-core/fe/12-charting.md) §10. **Mọi biến thể của [`../Design/Components/Chart.md`](../Design/Components/Chart.md) trừ `line`** tự dựng bằng HTML/CSS và không cần thư viện nào. Biến thể `line` cần thư viện thì nạp **theo yêu cầu**, khi màn đầu tiên dùng nó được mở | F14 là một *trần kích thước*, không phải allowlist nội dung — nó không biết *cái gì* nằm trong bundle. Khai thành luật **D25** ở [`../RULES.md`](../RULES.md) |
| 2 | **Ba component đều nằm ở nhánh tải chậm.** Một dự án không có màn nào dùng `EditableGrid` thì không tải một byte nào của nó | Cùng lý do với ràng buộc 1. Khai thành luật **D26** ở [`../RULES.md`](../RULES.md) |
| 3 | **Không component nào trong ba mang từ vựng của một ngành.** Tên biến thể, tên tầng xác thực, tên input phải độc lập nghiệp vụ. Ví dụ minh hoạ thì được phép nhắc chứng từ hay dự toán | Phép thử Core↔nghiệp vụ ở [`../Design/CLAUDE.md`](../Design/CLAUDE.md) §1 là **đọc hiểu**: ranh giới giữa ví dụ minh hoạ và từ vựng ăn vào mô hình là phán đoán. Khai thành luật **D27** ở [`../RULES.md`](../RULES.md) |

Ràng buộc 3 đã phải thi hành ngay khi viết ADR này: `EditableGrid` từng đặt tên một tầng xác thực là *"mức chứng từ"*, và `Input` biến thể `daterange` từng khai luật về kỳ kế toán đã khoá sổ. Cả hai đã sửa. Đó là bằng chứng ràng buộc này cần thiết, không phải trang trí.

## Cái bị lật — danh sách chính xác

| File | Chỗ bị lật | Sau ADR này |
| --- | --- | --- |
| [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md) §1 | *"Bốn thứ cố ý không có"* | Còn **một**: trình soạn thảo văn bản đa dạng thức |
| [`../wiki-core/fe/12-charting.md`](../wiki-core/fe/12-charting.md) §2.2 | *"Nhu cầu biểu đồ thuộc MODULE, không thuộc Core"* | Lật. Nhưng §2.2 giữ nguyên giá trị ở một vế: **thư viện** biểu đồ vẫn không nằm trong bundle khởi động |
| [`../wiki-core/fe/01-core-components.md`](../wiki-core/fe/01-core-components.md) §3 dòng B3 và §6 | *"khi đó nó thuộc module, không thuộc Core"* | Lật |
| [`../wiki-core/fe/11-grid-and-metadata.md`](../wiki-core/fe/11-grid-and-metadata.md) §11 | *"Bảng chỉnh sửa tại chỗ — ❌ chưa"* | Đổi sang ✅, vì điều kiện của dòng đó nay do ADR này cấp |
| [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.3 | *"Thư viện biểu đồ vẫn thuộc nhóm thêm-khi-cần"* | Lật một phần: component thuộc Core, thư viện vẫn nạp theo yêu cầu |

Năm chỗ trên **đã được sửa để trỏ về ADR này**. Không chỗ nào còn giữ bản cũ — đó là điều kiện để repo chỉ có một nguồn.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — rút ba component về module, giữ nguyên bốn luật

**Được:** không lật gì cả, giữ nguyên kỷ luật "ngưỡng hai module", và rẻ nhất hôm nay vì chưa có code.

**Vì sao loại:** nó giải quyết mâu thuẫn tài liệu mà không giải quyết vấn đề thật. Ba thứ này rồi sẽ được dựng — bởi dự án đầu tiên, theo cách riêng của nó, và không ai xem lại. Lúc dự án thứ hai cần, thứ tồn tại không phải một spec Core mà là một hiện thực đã bám vào nghiệp vụ của dự án đầu. Đó chính là kịch bản mà [`../wiki-core/fe/12-charting.md`](../wiki-core/fe/12-charting.md) §2.2 cảnh báo — chỉ khác là nó xảy ra ở module thay vì ở Core, và ở module thì **không ai canh**.

### Phương án B — chờ tới khi module thứ hai cần, đúng ngưỡng

**Được:** tuân thủ tuyệt đối phép thử đã có.

**Vì sao loại:** ngưỡng hai module là phép thử đúng cho **phụ thuộc và hiện thực**, không phải cho **tri thức thiết kế**. Một spec mô tả tám trạng thái của lưới nhập liệu không tốn bundle, không tốn thời gian nâng cấp, và không khoá ai vào gì. Áp ngưỡng của phụ thuộc lên tài liệu là áp nhầm công cụ — và cái giá của việc áp nhầm là hai dự án cùng học lại một bài học.

### Phương án C — giữ ở Core nhưng đánh dấu "thử nghiệm", chưa ràng buộc

**Vì sao loại:** [`../Design/CLAUDE.md`](../Design/CLAUDE.md) §4 chỉ có bốn trạng thái component, và "thử nghiệm" không nằm trong đó. Thêm trạng thái thứ năm để né một quyết định là cách làm bảng trạng thái mất nghĩa. Một spec hoặc là luật, hoặc không nên ở trong `Components/`.

### Phương án D — chỉ rút `Chart`, giữ hai thứ còn lại

**Được:** `Chart` là thứ có lập luận chống lại mạnh nhất — nó kéo theo một thư viện ngoài và cả một bảng màu riêng trong [`../Design/DESIGN.md`](../Design/DESIGN.md) §2.8.

**Vì sao loại:** ràng buộc 1 và 2 ở trên đã vô hiệu hoá đúng hai lý do đó. Mọi biến thể `Chart` trừ `line` đều không cần thư viện nào; bảng màu là **giá trị token**, không phải mã chạy, nên nó không vào bundle của dự án không vẽ biểu đồ. Sau khi hai lý do mạnh nhất bị vô hiệu, phương án này chỉ còn là nửa vời — và nửa vời thì để lại đúng một mâu thuẫn tài liệu nhỏ hơn.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Đi trước phép thử "ngưỡng hai module"** | Đây là khoản đắt nhất và không mua lại được bằng nỗ lực. Nếu khẳng định thực nghiệm ở mục *Vì sao lật* sai, Core mang ba spec không ai dùng, và mỗi lần rà soát kiến trúc lại phải giải thích vì sao chúng ở đó |
| **Lúc chốt, cả ba ràng buộc đều chưa có cổng** | Đây là khoản nguy hiểm nhất sau khoản đầu. Ba ràng buộc là thứ giữ cho quyết định này đúng. Lúc chốt quyết định, cả ba chỉ được ép **bằng review**; chúng đã khai thành D25–D27 ở [`../RULES.md`](../RULES.md) — nhìn thấy được, nhưng nhìn thấy không phải là được canh. **Trạng thái cổng về sau đọc ở `RULES.md`, không ở ADR này** — một ADR ghi lại quyết định tại thời điểm ra quyết định, và con số nào chép vào đây cũng sẽ mục. Từ vựng nghiệp vụ đã rò **hai lần** ngay trong đợt viết chính ba component này |
| **Bề mặt Core rộng thêm ba component nặng** | ADR-0016 phân phối Core bằng clone, nên mọi dự án đều mang theo. Chi phí thật không phải bundle (ràng buộc 1–2 đã chặn) mà là **chi phí bảo trì spec**: ba file nữa phải giữ đúng mỗi lần đổi token hay đổi luật chung |
| **`ProgressBar` nay phụ thuộc bảng màu biểu đồ** | Biến thể `meter` dùng `--chart-seq-1`. Gỡ `Chart` trong tương lai không còn là gỡ một file — phải gỡ cả ràng buộc này trước |

### Tích cực

- Repo trở lại **một nguồn**: năm chỗ mâu thuẫn đã trỏ về đây.
- Dự án thứ hai nhận được ba spec đã cân nhắc thay vì phải phát minh lại — đúng mục tiêu tái sử dụng mà Core tồn tại vì nó.
- Ràng buộc 1 và 2 biến một câu lo mơ hồ ("Core phình to") thành hai điều kiện đo được bằng ngân sách bundle.
- Từ vựng nghiệp vụ trong ba component đã bị gỡ, và việc gỡ đó nay có lý do ghi lại thay vì là ý thích của người sửa.

### Điều kiện lật quyết định

Lật ADR này khi **một trong ba** dấu hiệu sau xuất hiện:

1. **Dự án thứ hai dựng xong mà không dùng tới một trong ba component.** Lúc đó khẳng định "mọi sản phẩm trong lớp này đều cần" đã sai với thứ đó, và nó nên về module.
2. **Ràng buộc 1 hoặc 2 bị vi phạm** — thư viện biểu đồ lọt vào bundle khởi động, hoặc một trong ba component nằm ở nhánh tải ngay. Lúc đó cái giá đã vượt quá thứ ADR này cân.
3. **`docs/audit/` có một mục ghi nhận từ vựng nghiệp vụ rò vào ba component này**, tính từ 2026-09-12.

   Mục audit ghi một lần rò phải mang **đúng chuỗi mốc** `[RO-TU-VUNG-D27]` trong thân file. Không có mốc thì không tính — bộ đếm chỉ đếm thứ nó nhận ra được:

   ```bash
   grep -rl '\[RO-TU-VUNG-D27\]' docs/audit/ | wc -l
   ```

   **PASS = `0`. Ra `1` trở lên nghĩa là ràng buộc 3 không giữ được bằng người đọc, và ADR này lật.**

   🛑 **Ngưỡng là MỘT, không phải ba, và đây là lý do.** Hai lần rò trong đợt viết ADR này (mục *Tiêu cực*) **không được ghi thành mục audit nào**, nên chúng không có trong bộ đếm và không bao giờ vào được nữa. Giữ ngưỡng ba thì thực tế phải rò **năm** lần mới lật — tức điều kiện lật rộng gấp đôi thứ ADR này định. Hạ xuống một giữ đúng ý ban đầu *"lần rò thứ ba thì lật"*, với hai lần đã tiêu.

   Mốc cần đặt ở thân file vì tên file và tiêu đề còn đổi được; và phạm vi đếm là `docs/audit/` nên chỗ **định nghĩa** mốc — file này và [`../RULES.md`](../RULES.md) — không tự đếm chính nó.

   🛑 **Điều kiện 2 hôm nay chưa kiểm được**, vì cổng mà nó dựa vào (D25/D26) chưa tồn tại. Nói ra thay vì để nó trông như một điều kiện đang hoạt động: cho tới khi có cổng allowlist chunk khởi động, vi phạm ràng buộc 1 hoặc 2 chỉ lộ ra khi có người mở build stats.

## Liên quan

- [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md) — vì sao mọi thứ trong Core đi theo mọi dự án
- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 — phép thử "ngưỡng hai module" mà ADR này đi trước
- [`../Design/COMPONENTS.md`](../Design/COMPONENTS.md) §3 — nơi ba component có dòng
- [`../Design/Templates/ListScreen.md`](../Design/Templates/ListScreen.md) — khuôn màn danh sách, cùng đợt làm việc
- [`../RULES.md`](../RULES.md) §7 F14 — ngân sách bundle, cổng ép ràng buộc 1 và 2
