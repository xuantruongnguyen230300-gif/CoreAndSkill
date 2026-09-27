---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0076 — Prettier **không** phải cổng của repo này; khối cấu hình còn sót của bộ sinh khung bị gỡ

> **Trạng thái:** Đã chấp nhận (2026-09-23)

## Bối cảnh

Một phiên điều phối đưa `npx prettier --check` vào danh sách cổng bắt buộc cho các agent FE, rồi tự phát hiện ra mình không có căn cứ nào để làm thế và mang câu hỏi lên bàn kiến trúc sư. Hai lượt agent khác nhau đã báo hai kết quả trái ngược — một xanh, một đỏ — và điều đó là thứ đáng chú ý hơn cả câu hỏi ban đầu: **một lệnh cho hai câu trả lời thì nó không phải cổng, bất kể nó kiểm cái gì.**

Hai giả thuyết được đưa ra trước khi đo, và **cả hai đều sai**. Ghi lại ở đây vì chúng là hai cách hỏng rất dễ tin:

| Giả thuyết | Đo được gì | Kết luận |
| --- | --- | --- |
| *"Repo không có tệp cấu hình prettier nào, nên nó chạy mặc định `printWidth: 80` trong khi mã viết ~100 cột"* | `src/FE/package.json` **có** khoá `prettier`, khai `printWidth: 100`, `singleQuote: true`, và một override dùng parser `angular` cho `*.html`. Khoá trong `package.json` là một chỗ khai cấu hình đầy đủ tư cách, prettier đọc nó bình thường | Sai. Cấu hình có, và nó đúng với lối viết của mã |
| *"Cấu hình chỉ có tác dụng khi đứng đúng thư mục — chạy từ gốc repo thì không đọc được `package.json` của `src/FE`"* | `--find-config-path` trên một tệp dưới `src/FE`, **gọi từ gốc repo**, trả về đúng `src/FE/package.json`. Prettier phân giải cấu hình **theo từng tệp**, đi ngược lên từ thư mục chứa tệp, nên chỗ đứng lúc gõ lệnh không đổi cấu hình nào được áp | Sai. Và kiểm được bằng một lệnh |

Đo tiếp, cùng ngày, cùng bộ mã:

| Đo gì | Kết quả |
| --- | --- |
| `--check` trên toàn bộ `src/FE/src`, chạy **từ gốc repo** | 27 tệp lệch |
| `--check` trên cùng tập tệp đó, chạy **từ `src/FE`** | 27 tệp lệch — **cùng con số, cùng danh sách** |
| Prettier có trong `devDependencies` của `src/FE/package.json` không | **Không** |
| Có bản cài nào trên máy không — cục bộ, hoặc toàn cục | **Không**. Mỗi lần `npx prettier` là một lần **tải về qua mạng**, phiên bản **không ghim** |
| `scripts/fe-gate.sh` và `.github/workflows/docs-gate.yml` có gọi prettier không | **Không dòng nào.** Tên nó chỉ xuất hiện trong chú thích của vài phép dò — chúng khai rằng mẫu tìm phải chịu được việc một dòng **bị bẻ**, tức chúng chống đỡ prettier chứ không phụ thuộc nó |

Nên nguyên nhân thật của hai kết quả trái ngược không phải cấu hình và không phải chỗ đứng: nó là **hai tập tệp khác nhau**. Một lượt kiểm sáu tệp nó vừa sửa — sáu tệp ấy sạch — và báo xanh; một lượt kiểm cả cây và báo đỏ. Cả hai lượt đều đúng với câu hỏi mình hỏi, và hai câu hỏi đó không phải một.

Cộng lại thành một bức tranh rõ: một công cụ **không được cài**, **không được ghim**, **không được lệnh cổng nào gọi**, để lại **một khối cấu hình** do bộ sinh khung Angular CLI sinh ra, và khối ấy làm mọi người đọc nó tin rằng công cụ đó đã được nhận vào. [ADR-0028](0028-toolchain-fe-va-ke-hoach-nang-cap.md) chốt toolchain FE và **không** có prettier trong đó — nghĩa là nó chưa bao giờ được nhận; nó chỉ chưa bao giờ bị tiễn.

## Quyết định

Kiến trúc sư chốt:

1. **Prettier không phải cổng của repo này.** Không agent nào được yêu cầu chạy `prettier --check` như một điều kiện để coi việc là xong, và kết quả của nó không phải căn cứ cho bất kỳ phán quyết nào.
2. **Khối `prettier` trong `src/FE/package.json` bị gỡ.** Một khối cấu hình cho một công cụ không được cài và không được gọi là một cái bẫy: nó làm `npx prettier --write` trông như đã được phê duyệt, và **một** lần chạy như thế viết lại 27 tệp trong một lượt. Việc gỡ giao `frontend-expert`.
3. **Sự thật này được viết ở [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md)** — tệp chủ về "cổng FE là những cái nào" — dưới dạng một mục nói rõ prettier **không** nằm trong hai bảng cổng và vì sao. Người sau đi tìm câu trả lời sẽ mở đúng tệp đó.
4. **Không thêm luật mã `F` nào, và không thêm dòng nợ nào.** Đây không phải một luật chưa có cổng; đây là một công cụ không được nhận. Danh sách cổng FE đã có **hai nguồn máy đọc được** — các section của `scripts/fe-gate.sh` và các bước trong workflow CI — và prettier không có mặt ở cả hai. Không có mặt ở nơi liệt kê chính là hình thức ép: thứ không được liệt kê thì không được chạy như cổng.
5. **`npx prettier` rút khỏi `permissions.allow` và khỏi dòng lệnh của `frontend-expert`.** Cả hai tệp thuộc `.claude/`, ngoài phạm vi ghi của `architect` — giao phiên chính. Lý do không chỉ là gọn gàng: một mục `allow` cho một lệnh **tải gói không ghim từ mạng về rồi chạy** là một bề mặt mà không quyết định nào của repo này từng mở ra.

Muốn nhận prettier vào sau này thì đường đi là: một ADR mới, một mục ghim **chính xác** trong `devDependencies` theo [ADR-0028](0028-toolchain-fe-va-ke-hoach-nang-cap.md) quyết định 4, một section trong `scripts/fe-gate.sh`, một bước CI, một dòng ở [`../RULES.md`](../RULES.md) §7 kèm cột *Ép bằng gì* — và một lượt định dạng lại 27 tệp, làm gọn một lần, tách hẳn khỏi mọi thay đổi khác.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Nhận prettier thành cổng thật

**Được:** đây là phương án cho kết quả **tốt nhất về đích đến**, và phải nói rõ. Định dạng thôi là chuyện phải bàn; các phép dò của `scripts/fe-gate.sh` thôi phải chống đỡ việc một dòng bị bẻ ở chỗ nào — hôm nay ít nhất năm phép dò khai chính điều đó trong chú thích, và vài tệp canary có ca riêng cho nó. Một bộ định dạng cố định làm hình dạng mã trở thành thứ đoán trước được, tức làm mọi phép dò văn bản dễ viết hơn và ít lỗ hơn.

**Mất:** ba khoản, và khoản thứ ba là khoản chặn.

Một, chi phí vận hành vĩnh viễn: một phụ thuộc nữa phải ghim, phải nâng, phải chờ khi CI chạy; và mỗi lần prettier đổi mặc định giữa hai bản lớn thì cả cây mã đổi theo.

Hai, một lượt định dạng lại 27 tệp — số đo thật, không phải ước lượng — rơi vào **đúng lúc** `frontend-expert` đang làm việc trong `src/FE/`. Một commit chạm mọi tệp FE trong khi có người đang sửa dở là cách rẻ nhất để tạo ra xung đột ở mọi nhánh đang mở.

Ba, và đây là chỗ quyết: nó **khó đảo nhất** trong cả bốn phương án. Một lượt định dạng lại toàn cây ghi đè `git blame` của mọi tệp FE. Muốn bỏ prettier sau đó, ta gỡ được công cụ nhưng không lấy lại được lịch sử — và lịch sử dòng là thứ người ta dùng khi đi tìm *vì sao dòng này thế này*.

**Vì sao loại — và loại ở mức nào:** loại **hôm nay**, không loại khỏi bàn; quyết định 4 ghi sẵn đường quay lại. Không ai yêu cầu prettier, không ai sở hữu nó, và không phép đo nào cho thấy định dạng đang gây ra vấn đề gì. Trả một cái giá không đảo được cho một lợi ích chưa ai đo là đúng thứ mà sáu câu hỏi trước một đề xuất kiến trúc tồn tại để chặn.

### Phương án B — Giữ nguyên trạng, chỉ thôi bảo agent chạy nó

**Được:** rẻ nhất. Không chạm `src/FE/` lúc đang có người làm ở đó.

**Vì sao loại:** nguyên trạng **chính là** thứ đã sinh ra lượt này. Khối cấu hình ở lại thì người sau mở `package.json`, thấy prettier được cấu hình tử tế, và suy ra đúng cái điều mà phiên điều phối vừa suy ra. Một quyết định mà bằng chứng phản lại nó vẫn nằm trong repo là một quyết định sẽ bị lật lại bởi chính người không biết nó tồn tại.

### Phương án C — Giữ khối cấu hình, ghi một dòng chú thích *"không phải cổng"* cạnh nó

**Được:** không mất cấu hình, mà vẫn cảnh báo đúng chỗ người đọc gặp nó.

**Vì sao loại:** một chú thích cạnh một cấu hình còn dùng được không ngăn ai chạy công cụ. Và nó dựng nguồn thứ hai cho câu *"prettier có phải cổng không"* — một ở `package.json`, một ở tệp cổng FE — đúng thứ mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5 cấm, với cách sửa mà §5 chỉ ra là gỡ một bản chứ không chú thích cả hai.

### Phương án D — Thêm một cổng kiểm rằng repo **không** có cấu hình prettier

**Được:** phòng đúng ca tái phát: một tệp `.prettierrc` xuất hiện trở lại và không ai nhận ra ý nghĩa của nó.

**Vì sao loại:** nó biến một quyết định *"chưa nhận"* thành một lệnh cấm, và chặn luôn cả đường đi hợp lệ mà quyết định 4 vừa mở — người viết ADR nhận prettier vào sẽ thấy cổng đỏ trước khi kịp viết cổng mới. Ngưỡng cho một cổng mới là *luật này bị vi phạm mà không ai biết*; ở đây thì ngược lại, một tệp cấu hình mới là thứ hiện ra ngay trong diff.

## Hệ quả

### Tích cực

- Danh sách cổng FE trở lại đúng **một** cách đọc: thứ có section trong `scripts/fe-gate.sh` hoặc một bước trong workflow CI. Không còn cổng thứ tư sống bằng lời truyền miệng.
- Lượt agent thôi tiêu thời gian vào một lệnh có thể cho hai kết quả, và thôi sinh ra những báo cáo "đỏ" về 27 tệp không ai đang đụng tới.
- Một mục `permissions.allow` cho phép **tải một gói không ghim từ mạng về rồi chạy** được đóng lại. Đó là khoản lợi mà không bên nào trong lượt này đi tìm, và nó có lẽ lớn hơn cả câu hỏi ban đầu.
- Hai giả thuyết sai được ghi lại kèm phép đo bác chúng, nên lần sau chúng không phải đi lại từ đầu.

### Tiêu cực

- **Định dạng mã FE từ nay không có gì canh — và nó sẽ trôi.** Đây là cái giá thật, không phải một lợi ích trá hình: 27 tệp lệch hôm nay, và con số đó sẽ chỉ tăng. Xung đột hợp nhất vì khác cách xuống dòng là chuyện sẽ xảy ra, và sẽ tốn thời gian của người thật.
- **Các phép dò văn bản của `scripts/fe-gate.sh` ở lại trong thế phải chống đỡ mọi cách bẻ dòng.** Chúng làm được — có canary chứng minh — nhưng mỗi phép dò mới phải trả lại cái giá đó, và một phép dò quên ca bẻ dòng sẽ hỏng theo chiều **xanh giả**.
- **Quyết định 2 chạm `src/FE/` trong khi `frontend-expert` đang làm ở đó.** Nhỏ — một khoá trong `package.json` — nhưng nó vẫn là một thay đổi đến từ ngoài luồng việc của người đang cầm tệp đó.
- **Con số 27 sẽ mục ruỗng ngay khi có ai sửa một tệp FE.** Nó là phép đo của một ngày, dùng để so hai phương án, không phải một hiện trạng để trích dẫn về sau. Ai cần con số hôm nay thì chạy lại lệnh; ADR này ghi nó vì nó là **căn cứ của quyết định**, không vì nó là sự thật lâu dài.

### Rút lui nếu sai

Rẻ và sạch — đây là phương án đảo được nhất trong bốn phương án, và đó là một phần lý do nó thắng. Nhận prettier vào sau này không phải hoàn tác gì cả: khối cấu hình đã gỡ được viết lại trong một phút, và mọi thứ còn lại (ghim phiên bản, section cổng, bước CI, dòng luật) là việc phải làm **dù có gỡ hôm nay hay không**. Thứ duy nhất lượt này lấy đi là một khối cấu hình không ai dùng; thứ nó giữ lại là quyền quyết định chưa bị tiêu mất bằng một lượt định dạng toàn cây.

## Việc thi công

**`frontend-expert` — `src/FE/`:** gỡ khoá `prettier` khỏi `package.json`. Không chạy `prettier --write` trên bất cứ tệp nào trong cùng lượt — mục đích của quyết định 2 là ngăn đúng lượt chạy đó.

**Phiên chính — `.claude/`:** gỡ `npx prettier` khỏi dòng *Lệnh & công cụ* của `agents/frontend-expert.md`, và gỡ hai mục `prettier` khỏi `permissions.allow` của `settings.json`. Ngoài phạm vi ghi của `architect`.

**`architect` — đã làm trong lượt này:** mục mới ở [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md).

## Liên quan

- [`0028-toolchain-fe-va-ke-hoach-nang-cap.md`](0028-toolchain-fe-va-ke-hoach-nang-cap.md) — toolchain FE đã chốt, nơi prettier **không** có mặt; quyết định 4 của nó là luật ghim chính xác.
- [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) — tệp chủ về tập cổng FE.
- [`0011-ci-github-actions.md`](0011-ci-github-actions.md) — *cổng chạy tay là cổng có thể bị bỏ*; một cổng không có trong CI thì không phải cổng.
