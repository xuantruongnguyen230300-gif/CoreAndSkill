---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 2026-09-09 — Cổng tự khai "OK" trên tập rỗng, và hai mô hình nguồn FE↔API cùng mang `kind: luat`

> Kết quả một lượt rà soát toàn cục `.claude/` và `docs/`, chạy bằng hai lượt kiểm độc lập không tiếp xúc nhau: một lượt soi tính nhất quán nội bộ, một lượt đối chiếu với repo sản phẩm tiền nhiệm.
>
> Viết theo khuôn bốn phần ở [`README.md`](README.md).

---

## 1. Hiện tượng

Cổng [`../../.claude/check-docs.sh`](../../.claude/check-docs.sh) báo **PASS** ở mọi mục. Không mục nào đỏ, không cảnh báo nào. Trong lúc đó, ba nhóm vấn đề dưới đây đang tồn tại và không mục nào chạm tới.

### 1.1 Sáu mục của cổng không tự chứng minh được đầu vào

Một mục in nguyên văn `OK … (0 trích dẫn)` — tức là nó khẳng định "không có vi phạm" sau khi xét **đúng không mục nào**. Một mục khác in `(0 dòng đã xét)` trong khi thực tế nó có xét: biến đếm của nó tăng **ngay trước** dòng báo vi phạm, nên nó đếm vi phạm chứ không đếm đầu vào. Con số in ra vì vậy luôn bằng 0 khi mọi thứ đúng, và cũng bằng 0 khi phép dò đã hỏng — hai trạng thái ngược nhau, cùng một thông điệp.

### 1.2 Hai mô hình phục vụ FE↔API mâu thuẫn, cả hai đều là luật

Phía FE khai FE và API phục vụ **trên cùng một nguồn** qua reverse proxy, và liệt kê bốn thứ có được từ quyết định đó — dẫn đầu là "không có CORS". Phía BE, ở đúng file chủ của quy ước controller, khai FE chạy ở **origin khác** BE ở cả môi trường phát triển lẫn môi trường thật, rồi dựng nguyên một chuỗi ràng buộc trên giả định đó: cookie chéo nguồn ⇒ `SameSite=None` ⇒ `Secure` bắt buộc ⇒ HTTPS trên **mọi** máy lập trình viên.

Giả định "khác nguồn" còn có mặt ở ba file khác. Tức là **bốn** bản, không phải hai.

### 1.3 Đường dẫn tồn tại nhưng trỏ vào thứ không có

Hai agent được trỏ tới một thư mục khuôn để lấy mẫu cho ba loại tài liệu bàn giao. Thư mục đó tồn tại và chứa mười khuôn — **không khuôn nào** trong ba loại được hứa. Cổng xanh vì nó kiểm *đường dẫn có tồn tại không*, không kiểm *đường dẫn có chứa thứ được hứa không*.

Cùng nhóm: một script cổng được nêu như lệnh bắt buộc chạy trong hai file agent, không kèm ghi chú nào, trong khi chính [`../RULES.md`](../RULES.md) khai rõ script đó thuộc giai đoạn 2 và chưa tồn tại.

---

## 2. Nguyên nhân gốc

Ba hiện tượng trên là ba biểu hiện của **một** cơ chế: **cổng chỉ kiểm được thứ nó được viết để kiểm, và mỗi mục cổng tự quyết định thế nào là "không có vi phạm".**

### 2.1 "Không tìm thấy vi phạm" và "không nhìn gì cả" là hai kết quả khác nhau in ra cùng một dòng

§0 của chính script đã phát biểu luật này — *"MỖI MỤC PHẢI TỰ CHỨNG MINH NÓ CÓ DỮ LIỆU ĐẦU VÀO"* — và sáu mục không tuân theo. Đây không phải sơ suất ngẫu nhiên: chốt tự chứng minh phải được **viết riêng cho từng mục**, nên nó là thứ bị bỏ quên đúng ở những mục được thêm sau.

Hệ quả nặng hơn con số 0: khi một phép dò hỏng — pattern lệch một ký tự, một hàm ném lỗi, một biến đổi tên — mục đó chuyển từ "canh" sang "không canh" mà **thông điệp in ra không đổi**. Không có gì để so sánh, nên không ai phát hiện.

### 2.2 Mâu thuẫn FE↔BE tồn tại được vì cổng không đọc hiểu, và vì quyết định không có ADR

Quyết định "cùng nguồn" **xoá bỏ cả một nhóm bài toán** thay vì giải chúng. Nó đắt để đảo ngược và ảnh hưởng tới cả hai phía. Nhưng nó không được ghi ở [`../adr/`](../adr/), và không có dòng nào trong [`../OWNERSHIP.md`](../OWNERSHIP.md).

Không có ADR nghĩa là không có chỗ nào nói *"đây là quyết định, đã cân nhắc"*. Không có dòng chủ quyền nghĩa là cổng §15 không canh. Kết quả: bản cũ ở phía BE không bị gỡ, và nó **vẫn là luật** với người đọc — vì nó nằm ở file chủ, mang `kind: luat`, và trình bày một chuỗi lập luận chặt chẽ trên một tiền đề đã bị thay.

Đây đúng khuôn hỏng mà [`../OWNERSHIP.md`](../OWNERSHIP.md) §1 mô tả: hai file cùng `kind: luat` định nghĩa cùng một thứ và nói khác nhau. Người thi công đọc file chủ của phía mình, tin nó, và không có lý do gì mở file của phía kia.

### 2.3 Một đường dẫn đúng cú pháp là bằng chứng yếu hơn nó trông có vẻ

Kiểm "đường dẫn tồn tại" bắt được ca file bị di chuyển hoặc đổi tên. Nó **không** bắt được ca đường dẫn trỏ tới một thư mục thật nhưng nội dung bên trong không phải thứ được hứa — và đó chính là ca xảy ra ở đây. Với một agent, hai ca đó dẫn tới hai hành vi khác hẳn nhau: đường dẫn chết làm nó dừng lại và hỏi; đường dẫn sống trỏ nhầm làm nó **tự tin dùng nhầm khuôn**.

---

## 3. Cách vá

Ba nhóm dưới đây đã làm xong trong cùng lượt. Nhóm thứ tư **chưa** — nó chờ một quyết định của người dùng, và ghi ra đây để nợ nhìn thấy được.

### 3.1 Vá gốc — cổng phải tự chứng minh đầu vào

| Mục | Trước | Sau |
| --- | --- | --- |
| §2 code block trong `.claude/` | Chỉ bắt fence ở cột 0; không chốt 0 mục | Bắt cả fence thụt lề; xét 0 khối ⇒ FAIL |
| §6 tuyên bố hoàn thành | Biến đếm tăng ngay trước dòng báo vi phạm ⇒ luôn in 0 | Tách hai số: số dòng **trích được** và số dòng **còn lại sau miễn trừ**; trích 0 dòng ⇒ FAIL |
| §7 trích dẫn `file:dòng` | In `OK … (0 trích dẫn)` | Xét 0 mục ⇒ in **NOTE khai báo tường minh**, không phải OK |
| §8 bảng định tuyến | Không chốt 0 mục | Xét 0 định danh ⇒ FAIL |
| §9 tên file trong `.claude/` | Không chốt 0 mục | Xét 0 tên ⇒ FAIL |

> **§7 cố ý KHÔNG chuyển thành FAIL.** Repo hiện có đúng 0 trích dẫn `file:dòng`, và ở giai đoạn 1 chưa có `src/` để neo thì con số đó **hợp lệ**. Biến một trạng thái hợp lệ thành đỏ là cách nhanh nhất để người ta tắt mục đi. Cái sai cần sửa không phải con số, mà là việc nó được in ra dưới nhãn OK — nên nó dùng đúng khuôn NOTE mà §10 đã dùng cho ca chưa có `src/`.

### 3.2 Vá gốc — hai lỗ hổng của chính hạ tầng cổng

**Hook ghi dấu bỏ sót mọi thay đổi không đi qua công cụ sửa file.** Hook `PostToolUse` chỉ khớp ba công cụ sửa file. Một lượt sửa tài liệu bằng lệnh shell — thay thế tại chỗ, heredoc, script — không ghi dấu nào, nên hook `Stop` thoát sớm và **cổng không chạy**. Câu *"không thể kết thúc một lượt với cổng đang đỏ"* ở [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8 vì vậy đúng với một cách sửa file và sai với cách kia.

Đã vá: matcher nhận thêm công cụ chạy lệnh, và hook ghi dấu **không điều kiện** cho mọi lệnh — kể cả lệnh chỉ đọc. Đây là lựa chọn có chủ đích, không phải sự cẩu thả: phân biệt lệnh đọc với lệnh ghi cần phân tích dòng lệnh, và phân tích sai theo chiều *"đây không phải lệnh ghi"* làm cổng biến mất im lặng. Đánh dấu thừa tốn khoảng hai giây ở cuối lượt; đánh dấu thiếu tốn cả cổng.

Vá kèm: công cụ sửa notebook khai đường dẫn dưới một khoá khác, nên nhánh đó của matcher **chưa bao giờ** ghi được dấu.

**Cổng có một miễn trừ cứng không khai ở đâu cả.** §1 bỏ qua một lệnh git nhất định ngay trong mã script, vì cách khai nó trong danh sách chặn có dạng khác. Đã gỡ miễn trừ và sửa **phép dò** để nhận cả hai dạng khai. Ngoài ra §1 trước đây chỉ đối chiếu phần lệnh git; phần lệnh phá huỷ không phải git — và cả năm dạng ghi của lệnh vừa nói — nay cũng được đối chiếu, đọc trực tiếp từ văn bản của [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §1.

**`README.md` ở gốc repo nằm ngoài tầm quét.** Nó là cửa vào đầu tiên của repo. Đã đưa vào tầm quét của các mục kiểm link và kiểm đường dẫn — và **không** đưa vào mục kiểm ba khoá phân loại, vì nó không phải file `docs/`. Việc đưa vào lập tức lộ ra một bẫy thứ hai: mục kiểm link tính thư mục chứa file bằng cách cắt chuỗi ở dấu gạch chéo, nên với file ở gốc — không có gạch chéo nào — nó tính ra chính tên file, và **mọi** link tương đối trong đó bị báo chết oan.

### 3.3 Vá gốc — một nội dung một nguồn

| Chỗ lệch | Xử lý |
| --- | --- |
| Khuôn mã lỗi định nghĩa ở hai file | Rút bản thứ hai còn một dòng trỏ đường; đăng ký chủ quyền vào [`../OWNERSHIP.md`](../OWNERSHIP.md) §3 để cổng §15 canh từ nay |
| Danh mục khoá phân quyền bị chép lại lần thứ hai trong khu hợp đồng | Rút còn dòng trỏ đường. Kèm theo, gỡ một số đếm chép cứng |
| Ngưỡng đưa code vào Core bị chép vào ba file `.claude/` | Gỡ giá trị, giữ đường dẫn — đúng khuôn mà file agent kiến trúc đã tự khai |
| Chú thích cây thư mục liệt kê chuỗi interceptor sai thứ tự so với file chủ | Thay bằng dòng trỏ về file chủ |
| Một tiêu đề mục khai "hai bảng" nhưng chứa ba mục con | Sửa tiêu đề |
| Thông điệp FAIL của chính cổng chỉ người đọc tới sai số mục của danh sách nợ | Sửa ở cả ba nguồn nhắc tới nó |
| Sáu khuôn thiết kế không được gọi tên ở đâu; hai agent được trỏ tới ba khuôn bàn giao không tồn tại | Thêm bảng "khuôn nào dùng cho việc nào"; hai agent nay được trỏ tới bảng đó, kèm dòng nói rõ ba khuôn bàn giao **chưa có** |
| `16-i18n-va-ma-loi.md` mang **bốn** định nghĩa mâu thuẫn với file chủ: khuôn mã chữ thường (chủ dùng CHỮ HOA) · bảng `ErrorType`→HTTP thiếu dòng **422** · `fieldErrors` khoá chữ thường (phá thẳng nợ B1) · mẫu catalog không biên dịch được với chữ ký `Error` | Rút cả bốn còn dòng trỏ đường |
| `06-concurrency-control.md` dạy `UseXminAsConcurrencyToken()` — API Npgsql **đã gỡ hẳn**, và file chủ nói thẳng điều đó. Cùng khối cảnh báo lại tự nhận *"file chủ là file bạn đang đọc"* rồi trỏ recipe sang file khác | Gỡ recipe chết; bảng *entity nào cần token* chuyển **vào** file chủ; đăng ký chủ quyền |
| Danh mục tên ArchTest tồn tại ở **bốn** chỗ, trong đó `be-architecture.md` tự nhân bản ở hai mục của chính nó | Gỡ hai bản sao thuần; còn `RULES.md` (cột ép) và `04-testing-strategy.md` (có cột giải thích) — đã đối chiếu, **khớp chính xác** |
| Ba hàng định tuyến trỏ tới file **không nhắc** định danh chúng hứa: `traceId` · `CurrentUser` · `CORE_I18N` | Sửa đích, hoặc gỡ định danh chưa ai định nghĩa. Lộ ra khi §8 được nới để đọc đường dẫn tương đối |

> **Bài học về cách vá, không phải về nội dung được vá:** mười khối *"bản trước của mục này sai vì…"* đã bị nhét thẳng vào file nội dung trong lúc sửa. Đó là **audit viết vào file nội dung** — đúng thứ [`README.md`](README.md) §2 phân định là không thuộc về đó. Chúng đã được gỡ và gom về bảng này. Ghi lại vì khuôn hỏng ở đây không phải một câu sai, mà là một **thói quen** làm tài liệu phình lên trên mỗi lần vá.

### 3.4 CHƯA vá — chờ quyết định

**Mô hình nguồn FE↔API vẫn đang mâu thuẫn.** Không sửa trong lượt này, có chủ đích: chọn một bên là một quyết định kiến trúc có đánh đổi thật — mô hình cùng nguồn đòi reverse proxy ở **mọi** môi trường kể cả máy lập trình viên — chứ không phải một lỗi chính tả để người rà soát tự chọn.

Khi bên nào được chốt, việc phải làm gồm ba phần, không được làm thiếu phần nào:

1. Sửa file chủ của bên bị loại.
2. Gỡ **cả ba** bản sao còn lại. Sửa file chủ mà để bản sao lại chính là hành vi *"đồng bộ hai bản"* mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5 cấm.
3. Viết ADR cho quyết định, và thêm một dòng vào [`../OWNERSHIP.md`](../OWNERSHIP.md) §3 để lần sau cổng canh giúp.

---

## 4. Cổng nào lẽ ra phải bắt

### 4.1 Cổng đã có, nay được vá — kèm phép thử ngược

Năm mục cổng nêu ở §3.1 và ba lỗ hổng hạ tầng ở §3.2 đều đã được kiểm bằng **phép thử ngược**: cố ý gây ra đúng vi phạm trên một bản sao của repo rồi xác nhận cổng chuyển đỏ. Bảy phép thử, bảy lần đỏ đúng chỗ:

| Vi phạm cố ý gây ra | Mục bắt được |
| --- | --- |
| Code block ngôn ngữ lập trình **thụt lề** trong `.claude/` | §2 |
| Gỡ một lệnh không phải git khỏi danh sách chặn | §1 |
| Gỡ một dạng ghi của lệnh từng được miễn trừ cứng | §1 |
| Gỡ một lệnh git khỏi danh sách chặn | §1 |
| Làm hỏng pattern của mục kiểm bảng định tuyến | §8 |
| Làm hỏng pattern của mục kiểm tuyên bố hoàn thành | §6 |
| Link chết trong `README.md` ở gốc repo | §3 |

Hook ghi dấu được kiểm riêng bằng bảy ca payload: sửa file bằng lệnh shell, lệnh chỉ đọc, lệnh không liên quan, sửa file bằng công cụ với đường dẫn tuyệt đối và tương đối, sửa file ngoài khu cần canh, và sửa notebook.

> **Phép thử ngược là phần bắt buộc, không phải phần thêm.** Ba trong bảy ca trên trước lượt vá này **không** làm cổng đỏ. Nếu chỉ chạy cổng sau khi sửa và thấy xanh rồi kết luận là xong, thì kết luận đó đúng bằng đúng cái nó đo: rằng repo hiện tại không vi phạm — chứ không phải rằng cổng bắt được vi phạm.

### 4.2 Cổng chưa có — nợ đã ghi

Bốn luật ở [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) trước lượt này **không có mã luật nào** trong [`../RULES.md`](../RULES.md), kể cả trong danh sách nợ — nghĩa là chúng không nằm ở bảng luật, cũng không nằm ở bảng nợ, tức là biến mất khỏi mọi chỗ đếm được. Trong khi chính `RULES.md` tự khai là *"mọi luật của repo"*.

Đã thêm vào §10 kèm ý tưởng cổng: nguồn giao diện duy nhất · ngôn ngữ tiếng Việt · nhãn đang-thi-công phải kèm bảng đối chiếu · hợp đồng API phải mô tả route có thật.

Kèm ba luật kiểm thử mới ở §8, rút từ vết thương đo được ở dự án tiền nhiệm — chi tiết ở [`../wiki-core/be/04-testing-strategy.md`](../wiki-core/be/04-testing-strategy.md).

---

## 5. Bài học tổng quát

> **Một cổng có ba trạng thái, không phải hai: bắt được vi phạm · không có vi phạm để bắt · không nhìn gì cả. Trạng thái thứ ba đội lốt trạng thái thứ hai, và nó đội lốt hoàn hảo.**

Ba hệ quả dùng được ngoài phạm vi lượt này:

1. **Mọi phép kiểm phải in ra số mục nó đã xét, và phải coi số 0 là một kết quả cần giải thích.** Giải thích có thể là FAIL, có thể là một khai báo tường minh — nhưng không bao giờ được là im lặng dưới nhãn OK. Đây là cùng một luật mà khu kiểm thử áp cho detector, chỉ khác chỗ áp.

2. **Phép thử ngược là cách duy nhất phân biệt "cổng đang canh" với "cổng trông như đang canh".** Chạy cổng và thấy xanh chỉ chứng minh repo hiện tại không vi phạm. Muốn biết cổng còn sống thì phải gây ra vi phạm và xem nó có đỏ không.

3. **Một tài liệu sai không nằm yên.** Bản mô tả mô hình khác nguồn không gây lỗi nào ở giai đoạn tài liệu. Nó chỉ trở thành thiệt hại khi có người thi công theo — và lúc đó thứ họ dựng là hạ tầng thật, tốn thật, cho một bài toán đã bị xoá bằng một quyết định ở file khác. Chi phí của một mâu thuẫn tài liệu **không** trả lúc nó được viết ra; nó trả lúc có người tin nó.
