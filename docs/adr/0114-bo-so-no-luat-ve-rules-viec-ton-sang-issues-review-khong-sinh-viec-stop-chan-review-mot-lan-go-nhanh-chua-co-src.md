---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0114 — Bỏ sổ nợ: luật chưa có cổng về `RULES-*` với trạng thái thật, việc tồn sang GitHub Issues, `DEBT.md` chỉ còn chuyển hướng; review không tự sinh việc; hook `Stop` chặn review Core một lần mỗi tập tệp; gỡ nhánh *chưa có `src/`*

> **Trạng thái:** Đã chấp nhận (2026-10-05)

## Bối cảnh

Ngày 2026-10-04 và 2026-10-05 người dùng trả lời từng câu một loạt câu hỏi về sổ nợ và vòng review, với mục tiêu: `.claude/` và `docs/` sạch, `docs/` chỉ giữ luật và nhãn trạng thái thật. ADR này ghi các câu trả lời đó — kể cả hai câu trả lời cuối ngày 2026-10-05 về lỗi ngoài phần đổi và phạm vi issue, nằm ở quyết định 3 và 5 — cùng phần kiến trúc sư phải chốt để thi công được. Số đo lấy trên cây làm việc ngày 2026-10-05, trừ khi ghi khác. Một lượt song song đang sửa `docs/` trong cùng ngày, nên số có thể lệch vài hàng; lệnh đo lại nằm ở mục *Lệnh*.

**Một — sổ nợ lớn dần, việc nó giữ không ai làm.**

- [`../DEBT.md`](../DEBT.md) nặng 126 288 byte. Lúc tách khỏi `RULES.md` ngày 2026-09-23, phần đó nặng 47,9 KB ([ADR-0069](0069-tach-so-no-khoi-rules-md.md)).
- Ba lệnh đếm ở đầu tệp trả 31 (ô chứa `src/`), 60 (ô mở đầu 🔧), 5 (chờ quyết định); 17 hàng chỉ mang 🚫.
- Đoạn cảnh báo *cột Chặn bởi chưa được soát lại*, có từ [ADR-0045](0045-cho-src-sua-dinh-nghia-khong-them-ky-hieu-thu-tu.md) ngày 2026-09-20, vẫn đứng. Lượt soát lại toàn bảng mà quyết định 10 của [ADR-0111](0111-git-danh-sach-cho-phep-cong-docs-chan-mot-lan-architect-truoc-thi-cong-theo-dau-hieu.md) và quyết định 1 của [ADR-0112](0112-so-no-them-nhan-cho-quyet-dinh-kem-lenh-dem-thu-ba-cham-core-khong-quyet-dinh-khong-ghi-nhat-ky.md) đòi chưa diễn ra.
- Vòng soát `/doc-audit` ngày 2026-10-04 đếm 61 hàng, mang 60 mã, ở mục *Luật chưa có cổng* mà không mã nào có dòng ở `RULES-*.md`. Lệnh (4) tái lập con số đó.

**Hai — luật chỉ review canh nằm ngoài bộ luật luôn đọc của người review.** `DEBT.md` ở mục *Tra cứu* của `core-reviewer` (`.claude/agents/core-reviewer.md`, chuỗi `Luật chỉ có review canh — mã nợ`), theo quyết định 4 của ADR-0069. Hàng 🚫 ghi lớp bắt được là `core-reviewer`. Nhưng `core-reviewer` chỉ mở tệp đó khi một finding đã chạm luật chưa có cổng, tức khi nó đã biết luật đó rồi. Mục §23 của cổng đo bộ luật luôn đọc: `core-reviewer` 102 KB, `backend-expert` 90 KB, trần 120 KB.

**Ba — review sinh việc.** Khuôn báo cáo của `core-reviewer` có mục *Ngoài phạm vi — đề xuất ghi nợ*. `.claude/CLAUDE.md` §11 và `.claude/skills/core-review/SKILL.md` giao phiên chính chép mọi finding từ 🟠 trở xuống thành hàng nợ — quyết định 8 của ADR-0111, quyết định 2 của [ADR-0113](0113-ban-giao-qua-phien-chinh-no-review-phien-chinh-ghi-tai-lieu-ban-giao-o-spec-go-muc-11-quet-shell-theo-loi-goi.md). Người dùng quan sát chuỗi *finding → ghi nợ → hỏi có làm ngay không* kéo đợt việc dài ra. Chưa có số đo.

**Bốn — hook `Stop` mâu thuẫn giới hạn hai vòng.** Lớp chặn `core-reviewer` chặn mỗi lần phiên dừng cho tới khi có dấu review mới hơn mọi tệp Core đã sửa (`.claude/hooks/on-stop.sh`, chuỗi `Hook chặn mỗi lần phiên kết thúc lượt`). `.claude/CLAUDE.md` §11 giới hạn hai vòng review mỗi phạm vi, hết vòng 2 thì hỏi người dùng. Sau lượt sửa của vòng 2, hook vẫn chặn và đòi vòng ba. Lối thoát duy nhất là người dùng xoá nhật ký chạm Core.

**Năm — nhánh *chưa có `src/`*.** `src/` vào git từ commit `d8efddc` (*Bối cảnh* bốn của ADR-0113). Nhánh cho trạng thái không còn xảy ra vẫn nằm ở bảy agent, `.claude/CLAUDE.md` §8, `.claude/README.md`, hai hook (`[ -d src ]`), mục §10 và §27 của cổng, và bộ test hook có ca riêng cho nó.

**Sáu — ràng buộc của việc bỏ `DEBT.md`.**

- 22 ADR có link markdown tới `DEBT.md`, 23 ADR nhắc tên nó. Mục §3 của cổng kiểm link trong ADR, và nội dung ADR không sửa được (D45).
- 13 tệp dưới `src/` có chú thích trỏ `docs/DEBT.md`; 4 trong số đó thuộc khối `core-paths`. Mục §10 của cổng FAIL khi chú thích trỏ tệp không tồn tại. Các chú thích đó đã cũ sẵn: chúng nhắc hàng E17, T11, B21, B26, nay không còn trong sổ.
- 39 tệp sống dưới `docs/`, ngoài ADR có số và `audit/`, nhắc tên `DEBT.md`.
- Máy chưa cài `gh`.

## Quyết định

Người dùng chốt ngày 2026-10-04 và 2026-10-05, từng câu:

| # | Quyết định |
| --- | --- |
| 1 | `docs/` chỉ giữ luật và nhãn trạng thái thật. Không tệp nào dưới `docs/` giữ sổ nợ hay danh sách việc tồn |
| 2 | `docs/DEBT.md` còn lại làm tệp chuyển hướng ngắn, `kind: tham-chieu`, không mang hàng bảng nào. Nó nói ba điều: sổ nợ đã bỏ theo ADR này; luật chỉ review canh đọc ở cột trạng thái của `RULES-*.md`; việc tồn ở GitHub Issues. Tệp không bị xoá, vì ADR, nhật ký quyết định và chú thích mã nguồn trỏ tới nó |
| 3 | Chỉ lỗi thật thành GitHub Issues: hàng mục *Nợ review* — code hay test lệch luật — và finding về code của vòng soát 2026-10-04, gộp theo người sửa. Ý tưởng dựng cổng không thành issue: hàng 🔧, ⏳ của mục *Luật chưa có cổng* chỉ còn trạng thái ở dòng luật — chỉ review canh, chưa có cổng, điều kiện kích hoạt. Cổng dựng khi vi phạm thật lặp lại. Hàng ⏳ chờ quyết định không thành issue; phần chờ mang cụm *chưa chốt* ở dòng luật của nó. Phiên chính soạn danh sách issue ngoài repo; người dùng duyệt, tự tạo, rồi xoá bản nháp. `docs/` không chờ số issue mới sạch được |
| 4 | Đợt này không chạm `src/`. Việc code thành issue |
| 5 | Review không tự sinh việc. Khuôn báo cáo của `core-reviewer` bỏ mục *Ngoài phạm vi — đề xuất ghi nợ*. Lỗi 🔴 ngoài phần đổi chiếm **một dòng** trong báo cáo: không tính vào kết luận, không mở vòng review, không tự thành việc; phiên chính chuyển nguyên văn cho người dùng, người dùng chọn đưa vào danh sách issue hay không. Ngoài phần đổi, từ 🟠 trở xuống thì không báo. Trong phần đổi, finding từ 🟠 trở xuống chỉ nằm trong báo cáo; phiên chính đưa một finding vào danh sách issue chỉ khi người dùng chọn. Hết vòng 2 còn 🔴 thì người dùng chọn: sửa tiếp, đưa vào danh sách issue, hay chấp nhận |
| 6 | Hook `Stop` chặn review Core **một lần** cho mỗi tập tệp Core chưa review, mỗi phiên, bằng dấu vân tay cùng khuôn với cổng tài liệu ở quyết định 5 của ADR-0111. Phiên dừng lại với đúng tập đó thì hook chỉ báo người dùng, không đòi vòng review vượt giới hạn hai vòng |
| 7 | Gỡ mọi nhánh *chưa có `src/`* trong agent, `.claude/CLAUDE.md`, hook và cổng |

Kiến trúc sư chốt phần thi công:

| # | Quyết định |
| --- | --- |
| 8 | Mỗi mã chỉ sống ở `DEBT.md` đi theo đúng một loại ở bảng *Phân loại* dưới đây. **Luật** — có dòng ở `RULES-*.md`, giữ mã. **Luật của ADR đang tạm hoãn** — không có dòng, giữ mã; ADR lật phần hoãn thêm dòng cùng lượt. **Việc**, kể cả việc chờ quyết định — không dòng, không issue; mã ngừng dùng và không cấp lại. **Đề xuất chờ quyết định** — không dòng, không issue; mã giữ cho đúng đề xuất đó. **Luật đã có cổng** (D42) — dòng ✅ |
| 9 | Dòng luật chưa có cổng mang 🕳️, hoặc 📐 khi ô *Ép bằng gì* nêu được tên cổng đã chốt. Ô *Ép bằng gì* nêu ai canh hôm nay, kèm *chưa có cổng* hoặc *không ép được bằng máy*. Điều kiện đang chờ — module đầu tiên, dự án hạ nguồn đầu tiên — ghi sau cụm `kích hoạt:` trong cùng ô; phần của luật chờ người dùng quyết ghi sau cụm `chưa chốt:`. Dòng mới chỉ mang câu luật ngắn và trỏ tệp nguồn của luật. Lý do chưa có cổng và ý tưởng cổng không chép sang đâu: lịch sử git của `DEBT.md` giữ chúng. Không thêm ký hiệu trạng thái mới |
| 10 | Hàng nợ mang mã đã có dòng ở `RULES-*.md` — hàng *phần còn lại*, *lỗ cổng*, *(nợ)*, và hàng mục *Nợ review* — thì dòng `RULES-*` cùng mã tự nêu phần chưa phủ bằng một cụm, không trỏ `DEBT.md`. Bản sửa của hàng mục *Nợ review* vào issue; kế hoạch cổng của mọi hàng không vào issue |
| 11 | Ba định nghĩa chỉ có ở `DEBT.md` dời về chủ mới. **D14** — ngoại lệ của `.claude/CLAUDE.md` §6 — thành dòng D14 ở `RULES-DOCS.md`; `.claude/CLAUDE.md` §6 và skill `review-doc` trỏ tới đó. **C4** — danh sách đường hook không thấy chuyển nguyên văn vào chú thích đầu `.claude/hooks/on-pretool.sh`, nguồn duy nhất; `.claude/CLAUDE.md` §1 và `.claude/README.md` trỏ tới đó; lớp harness của C4 thành một cụm *không ép được bằng máy* trong ô của dòng C4. **D42** — dòng ✅ ở `RULES-DOCS.md` §1, ép bằng `check-docs.sh` §27; câu luật viết nhãn bằng glyph `📐`, không chép chuỗi nhãn đầy đủ, để lệnh tầng B của [ADR-0043](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md) — chỉ loại trừ `RULES.md` — không in `RULES-DOCS.md` |
| 12 | Luật *mã luật không bao giờ dùng lại* chuyển vào `RULES.md`, kèm cách tra: lệnh (3). Ra dòng nào thì mã đã dùng. Bảng *Phân loại* của ADR này là bản ghi các mã ngừng dùng trong đợt. Lệnh (2) — liệt kê dòng mang 🕳️ hay 📐 ở `RULES-*.md` — và lệnh (5) — dòng chờ điều kiện kích hoạt hay chờ quyết định — thay ba lệnh đếm của sổ nợ |
| 13 | Dấu vân tay của quyết định 6 băm tập **đường dẫn** tệp Core chưa review, sắp theo byte, giữ trong thư mục của phiên. Dấu chỉ còn giá trị khi mới hơn hẳn dấu `core-reviewed`. Sửa thêm một tệp đã có trong tập không làm tập mới; thêm một tệp là tập mới; một lượt review mới làm mọi dấu cũ hết giá trị. Lời chặn nêu: phạm vi đã đủ hai vòng thì không gọi vòng ba, hỏi người dùng rồi dừng lại. Nhánh chỉ nhắc một lần khi chưa gắn `SubagentStop` giữ nguyên |
| 14 | Gỡ nhánh trong cổng nghĩa là bỏ lối `NOTE` *không có `src/`*: thiếu thư mục mã nguồn thì mục cổng FAIL theo T6, vì repo đi theo đường phân phối của [ADR-0016](0016-phan-phoi-core-bang-clone.md) luôn có `src/`. Trong hook, bỏ điều kiện `[ -d src ]` |
| 15 | ADR chỉ nói *luật X nằm ở `DEBT.md`* — ADR-0092, 0096, 0097, 0098, 0099, 0100, 0103, 0104, 0110 — không đổi quan hệ. Tệp chuyển hướng giữ đường tới, như quyết định 2 của ADR-0069 đã làm với `RULES.md` §10 |

### Phân loại 60 mã chỉ sống ở `DEBT.md`

Mỗi mã xuất hiện đúng một lần. Không mã nào trong bảng thành issue — quyết định 3.

| Mã | Loại | Chỗ ở mới | Trạng thái, ghi chú |
| --- | --- | --- | --- |
| D14, D15, D18, D20, D21, D34, D40, D41, D43 | Luật | `RULES-DOCS.md` §1 — dòng D14 giữ định nghĩa ngoại lệ của §6 | 🕳️ chưa có cổng |
| D19 | Luật | `RULES-DOCS.md` §1 | 🕳️ — nửa mã màu đã là D30 |
| D27 | Luật | `RULES-DOCS.md` §1 | 🕳️ — lát cắt D28 có cổng; bộ đếm rò từ vựng của ADR-0019 ở `audit/` giữ nguyên |
| D31, D35, D44 | Luật | `RULES-DOCS.md` §1 | 🕳️ không ép được bằng máy |
| D42 | Luật đã có cổng | `RULES-DOCS.md` §1 | ✅ `check-docs.sh` §27 |
| C5 | Luật | `RULES-DOCS.md` §2 | 🕳️ — hở của hook khi xoá, đổi tên, chạy nền |
| D25, D26 | Luật | `RULES-FE.md` §7, giữ chữ D | 🕳️ chưa có cổng |
| F15, F16, F40 | Luật | `RULES-FE.md` §7 | 🕳️ chưa có cổng |
| F43 | Luật | `RULES-FE.md` §7 | 🕳️ không ép được bằng máy |
| F46 | Luật | `RULES-FE.md` §7 | 🕳️ không ép được bằng máy — `chưa chốt:` ca theo từng trang, phân biệt lượt đọc với lệnh ghi |
| F17 | Luật có cổng một phần | `RULES-FE.md` §7 | ✅ một phần — `ng lint`; phần cần render không ép được bằng máy |
| F41 | Luật có cổng một phần | `RULES-FE.md` §7 | ✅ một phần — spec; nửa màn tự dựng câu chưa có cổng |
| B11, B17, B18, B24, B25, A20, M15, E20 | Luật | `RULES-BE.md` | 🕳️ chưa có cổng |
| B34 | Luật | `RULES-BE.md` | 🕳️ — điều kiện 1, 2 không ép được bằng máy |
| B9, B10 | Luật | `RULES-BE.md` | 🕳️ — kích hoạt: module đầu tiên |
| B22 | Luật | `RULES-BE.md` | 🕳️ — kích hoạt: module đầu tiên có `DbContext` riêng; khi đó cần một ADR, trả lời cả đường vào outbox của module |
| B12, B13, B14, A21 | Luật | `RULES-BE.md` | 🕳️ — kích hoạt: đối tượng đầu tiên mà hàng nợ nêu |
| A17, A18, A19 | Luật | `RULES-BE.md` | 🕳️ — kích hoạt: dự án hạ nguồn đầu tiên |
| E19 | Luật | `RULES-BE.md` | 🕳️ — đang vi phạm có chủ đích tới điều kiện của ADR-0092 |
| S28 | Luật | `RULES-BE.md` | 🕳️ không ép được bằng máy |
| B27 | Luật có cổng một phần | `RULES-BE.md` | ✅ một phần — hai test đã có; vế *dự án thay được* chưa có cổng |
| S24, B20, T13 | Luật của ADR đang hoãn | ADR-0097, ADR-0098, ADR-0099 — hoãn cùng DB dev chung ([ADR-0110](0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md)) | Không dòng. Biến thể cục bộ của T13 đã ở mục *Tiêu cực* của ADR-0110 |
| S25, S26, S27, E21 | Luật của ADR đang hoãn | ADR-0098, ADR-0099, ADR-0104, ADR-0105 | Không dòng |
| S12, B15, B19 | Việc | Mã ngừng dùng | S12 là lỗ của cổng S6 — ô *Ép bằng gì* của S6 nêu chỗ hở. B15 không có câu luật nguồn, và §8.3 của `quy-uoc/be-performance.md` cấm tối ưu khi chưa đo. B19 hỏng ồn: module đầu tiên có `DbContext` làm host test từ chối khởi động; mục *Tiêu cực* của ADR-0102 đã ghi |
| F48, F49 | Việc chờ quyết định | Mã ngừng dùng; cụm *chưa chốt* ở hàng tương ứng của Screen 12, Screen 03 dưới `Design/Screens/` | Câu hỏi spec màn cho người dùng, không phải luật |
| B29 | Đề xuất chờ quyết định | Không dòng | Mã giữ cho đúng đề xuất này; người dùng nhận thành luật thì dòng mang B29 |

Hàng ⏳ chờ quyết định của sổ nợ mà mã đã có dòng ở `RULES-*` theo quyết định 10 — A7 *phần còn lại*, vế `FallbackPolicy` — mang cụm `chưa chốt:` ở dòng A7.

Quan hệ với ADR cũ, theo [`README.md`](README.md) §5.1:

- **ADR-0045 — thay thế.** Mọi quyết định của nó nói về giá trị ⏳ chờ `src/` và lệnh đếm của sổ nợ; quyết định 4 đã hết hiệu lực từ ADR-0112. Không phần nào còn hiệu lực.
- **ADR-0069 — sửa một phần.** Hết hiệu lực: quyết định 1 vế *toàn bộ §10 chuyển sang `DEBT.md`*, quyết định 3, 4, 5. Còn hiệu lực: quyết định 1 vế *một dãy mã chung, không mã nào dùng lại* — nay `RULES.md` giữ nó; quyết định 2 — tiêu đề `RULES.md` §10 ở lại làm bản chuyển hướng; quyết định 6, nay không đổi kết quả vì `DEBT.md` không còn mang chuỗi nhãn.
- **ADR-0108 — sửa một phần.** Hết hiệu lực: cụm *hết agent nền thì chặn như trước* của quyết định 1 với lớp chặn `core-reviewer`. Còn hiệu lực: hoãn khi còn agent nền, quyết định 2–5, vế sau của quyết định 6.
- **ADR-0111 — sửa một phần.** Hết hiệu lực: câu *lớp chặn `core-reviewer` không đổi* của quyết định 5; vế *từ 🟠 trở xuống thì ghi `DEBT.md` kèm người sửa* của quyết định 8; cụm *hàng nợ* của quyết định 6 vế (b); toàn bộ quyết định 10. Còn hiệu lực: quyết định 1–4, phần cổng tài liệu của quyết định 5, quyết định 6 trừ cụm trên, 7, phần còn lại của 8, quyết định 9.
- **ADR-0112 — sửa một phần.** Hết hiệu lực: quyết định 1. Còn hiệu lực: quyết định 2 và 3 — quyết định 3 xác nhận phần cổng tài liệu, phần đó không đổi.
- **ADR-0113 — sửa một phần.** Hết hiệu lực: quyết định 2. Còn hiệu lực: quyết định 1, 3, 4, 5.

## Phương án đã cân nhắc và vì sao loại

Ba phương án đầu do lời giao nêu; các dòng còn lại kiến trúc sư ghi.

| Quyết định | Phương án bị loại | Vì sao loại |
| --- | --- | --- |
| 2 | Xoá hẳn `DEBT.md` | 22 link trong ADR gãy, mục §3 đỏ. Sửa ADR bị D45 cấm; thêm miễn trừ cho §3 là nới cổng cho xanh. 13 chú thích dưới `src/` làm §10 đỏ, mà sửa chúng là chạm `src/`, 4 tệp trong `core-paths` |
| 1–3 | Một tệp backlog trong `docs/` thay sổ nợ | Đổi tên sổ, giữ nguyên đường sinh việc: review vẫn có chỗ ghi, tệp vẫn lớn dần, `docs/` vẫn chứa thứ không phải luật. Cái được là agent đọc được backlog mà không cần `gh` |
| 1–3 | Giữ sổ, chỉ dọn — soát lại cột *Chặn bởi*, kéo 60 mã về `RULES-*` | Lượt soát lại đã được đòi hai lần từ 2026-09-20 mà chưa diễn ra; đường review → hàng nợ vẫn mở, sổ lớn lại |
| 1–3 | Giữ mục *Luật chưa có cổng*, chỉ bỏ mục *Nợ review* | Luật chỉ review canh vẫn ngoài bộ luật luôn đọc của `core-reviewer`; kế hoạch cổng của mọi hàng 🔧 vẫn là backlog nằm trong `docs/` |
| 3 | `docs/` trỏ số issue | `docs/` phải chờ người dùng tạo issue; agent không mở được issue khi máy chưa có `gh`; link một chiều — tiêu đề issue mang mã — đủ để tra |
| 3 | Issue cho cả kế hoạch dựng cổng của hàng 🔧 và cho hàng ⏳ | Người dùng chỉ dựng cổng khi vi phạm thật lặp lại. Issue cho ý tưởng cổng là sổ nợ dời ra ngoài repo: vẫn lớn dần, vẫn kéo câu hỏi *có làm ngay không* |
| 3 | Mỗi hàng một issue | Người dùng tạo tay nhiều nhất; hàng cùng người sửa được làm trong cùng một lượt |
| 5 | Giữ mục *Ngoài phạm vi* trong báo cáo | Đúng đường sinh việc mà người dùng quan sát |
| 5 | Bỏ hẳn lỗi ngoài phần đổi khỏi báo cáo | Mức 🔴 ở [`../quy-uoc/tieu-chi-review.md`](../quy-uoc/tieu-chi-review.md) §2 là mức làm hỏng thứ thật; bỏ nó là đổi một lần leo thang lấy một lỗ thủng im lặng |
| 6 | Giữ chặn tới khi có review | Mâu thuẫn giới hạn hai vòng ở *Bối cảnh* bốn |
| 6 | Chỉ nhắc, không chặn | Bỏ chốt bằng máy — lý do [ADR-0108](0108-hook-stop-hoan-khi-cho-agent-nen-docs-khong-keo-review.md) loại phương án này. Chặn một lần giữ được một lần đòi review chắc chắn |
| 13 | Vân tay gồm cả mtime của tệp | Mỗi lần sửa lại chặn lại — gần như hành vi cũ, vòng chặn quay về |
| 13 | Dấu không mất giá trị khi có review mới | Sau lượt review vòng 1, lượt sửa giữ nguyên tập đường dẫn: hook không đòi vòng 2 dù luật ở §11 đòi |
| 7, 14 | Giữ nhánh | Đường mã không chạy, ca test cho trạng thái không còn xảy ra, và mục cổng có lối xanh mà không xét gì |
| 8 | Luật của ADR đang hoãn vào `RULES-*` kèm ghi chú hoãn | Bảng luật liệt kê luật đang áp; thêm khoảng 2 KB vào bộ luật luôn đọc cho luật không áp, và cần một giá trị trạng thái mới |
| 8 | Đưa cả 60 mã vào `RULES-*` | Mã việc không phải luật — đúng loại hàng làm bảng luật trông dày mà không ép gì |
| 8 | Một tệp luật riêng cho luật chưa có cổng, đặt ở *Tra cứu* | Là `DEBT.md` đổi tên: luật chỉ review canh vẫn ngoài bộ luật luôn đọc của người review |
| 9 | Giữ một ký hiệu cho *cổng chờ đối tượng thật* | Thêm một giá trị cột phải giữ định nghĩa và lệnh đếm riêng; điều kiện chờ ghi bằng chữ sau `kích hoạt:` đủ cho người đọc, và lệnh (5) lọc theo cụm đó |
| 12 | Sổ mã đã dùng trong `docs/` | Một danh sách nữa phải giữ đồng bộ tay; lịch sử git đã có mọi mã từng ghi |

## Ép bằng gì

| # | Cơ chế |
| --- | --- |
| 1, 2 | Chưa có cổng. Lệnh (1), chạy tay; người kiểm: người dùng đọc diff |
| 3 | Không ép được bằng máy. Người kiểm: người dùng |
| 4 | Không áp — ràng buộc của một đợt |
| 5 | Không ép được bằng máy. Khuôn báo cáo của `core-reviewer` thay mục *Ngoài phạm vi — đề xuất ghi nợ* bằng mục chỉ nhận 🔴; người kiểm: người dùng đọc báo cáo phiên chính chuyển lại |
| 6, 13 | Bộ test hook, nhóm `on-stop` — ca mới phiên chính viết, liệt kê ở mục *Thi công* |
| 7, 14 | Hook: bộ test hook, ca *không src* gỡ. Cổng: chưa có bộ test riêng cho `check-docs.sh`; người kiểm: phiên chính chạy cổng một lần trên bản sao không có `src/` trong thư mục nháp lúc thi công, mục §10 và §27 phải đỏ |
| 8–10 | `check-docs.sh` §14 ép ô *Ép bằng gì* không rỗng; lệnh rà D39 ở `RULES.md` đối chiếu cột trạng thái — chạy tay; lệnh (5) liệt kê phần chờ. Nội dung câu luật: review, người dùng đọc diff |
| 11 | D42: §27. D14, C4: review |
| 12 | Chưa có cổng. Người cấp mã chạy lệnh (3) |

## Lệnh

```bash
# (1) DEBT.md chỉ còn chuyển hướng. PASS khi in 0.
grep -c '^|' docs/DEBT.md

# (2) Luật chưa có cổng: dòng mang 🕳️ hay 📐 ở cột cuối.
grep -hE '^\| [A-Z][0-9]+ .*\| *(🕳️|📐)[^|]*\|$' docs/RULES-*.md

# (3) Mã đã từng dùng — ví dụ B36. Ra dòng nào thì đừng cấp mã đó.
git log -S'B36' --oneline -- docs/
grep -rn 'B36' docs/

# (4) Mã chỉ sống ở mục "Luật chưa có cổng" — chạy trên DEBT.md trước khi viết lại.
comm -23 \
  <(awk '/^## Luật chưa có cổng/{s=1;next} /^## /{s=0} s' docs/DEBT.md | grep -oE '^\| [A-Z][0-9]+' | sed 's/^| //' | sort -u) \
  <(grep -ohE '^\| [A-Z][0-9]+' docs/RULES-*.md | sed 's/^| //' | sort -u)

# (5) Dòng luật còn chờ: điều kiện kích hoạt, hoặc phần chờ người dùng quyết.
grep -hE '^\| [A-Z][0-9]+ .*(kích hoạt:|chưa chốt:)' docs/RULES-*.md
```

## Hệ quả

### Tích cực

- Luật chỉ review canh vào bộ luật luôn đọc của `core-reviewer`, `backend-expert`, `frontend-expert`. Lớp duy nhất bắt được chúng nay luôn có chúng trước mặt.
- Mỗi mã sống ở đúng một bảng. Cái giá *một dãy mã, hai tệp* mà ADR-0069 ghi ở *Tiêu cực* hết.
- `docs/` bớt một tệp 126 KB không phải luật, và không còn đường nào làm nó lớn lại.
- Báo cáo review không còn tự đẻ hàng việc; danh sách việc do người dùng chọn.
- Phiên không còn bị hook chặn không đáy vì Core chưa review; hook và giới hạn hai vòng hết mâu thuẫn.
- Hook, cổng và agent bớt nhánh chết cùng các ca test của nó.

### Tiêu cực

- **Bộ luật luôn đọc lớn lên, sát trần.** Ước theo cỡ cột *Luật* của các hàng sẽ dời: `RULES-BE.md` thêm khoảng 10–13 KB, đưa `core-reviewer` lên khoảng 112–115 KB và `backend-expert` khoảng 100–103 KB, trên trần 120 KB. Dòng luật mới sau đợt này chạm trần sớm hơn, và khi đó thứ tự của [ADR-0041](0041-cham-tran-corpus-reviewer-thi-rut-file-theo-tieu-chi.md) áp: phân loại lại trước, tách tệp sau, không nới trần. Đây là đảo một phần lý do của ADR-0069.
- **Ý tưởng cổng chỉ còn trong lịch sử git.** Cột *Ý tưởng cổng tương lai* của sổ nợ giữ những thiết kế đã nghĩ kỹ — hai nửa của D41, canary của D42, dấu hiệu miễn trừ của D43 — cùng các báo sai đã gặp khi chạy thử. Không issue, không tệp nào mang chúng sang. Lúc vi phạm thật lặp lại, người dựng cổng phải đào `git log` của `DEBT.md` hoặc nghĩ lại từ đầu, và có thể vấp lại đúng báo sai đã ghi.
- **Agent không đọc được issue.** Máy chưa có `gh`; phiên chính phải dán nội dung issue vào lời giao. `test-engineer` mất chỗ tự tra *nguồn việc*.
- **Lỗi thật không theo Core sang dự án hạ nguồn.** Bản clone ([ADR-0016](0016-phan-phoi-core-bang-clone.md)) mang theo `docs/`, không mang issue. Dự án hạ nguồn thấy luật nào chưa có cổng qua cột trạng thái, nhưng không thấy lỗi đã biết trong Core nó vừa kéo về.
- **Mất phép đếm theo loại chờ.** Ba lệnh đếm đi cùng sổ. Lệnh (5) chỉ liệt kê được dòng mang đúng hai cụm `kích hoạt:`, `chưa chốt:` — dòng viết điều kiện bằng chữ khác thì rơi khỏi mọi lệnh.
- **Câu hỏi chờ người dùng không có ai nhắc.** Hàng ⏳ chờ quyết định không thành issue. Câu hỏi của A7, F46, F48, F49 chỉ còn là cụm *chưa chốt* trong một ô; đề xuất B29 không còn dòng nào, chỉ còn bảng *Phân loại* này và lịch sử git. Không gì đưa chúng tới người dùng.
- **Người dùng tạo tay issue.** Từ lúc `DEBT.md` được viết lại tới lúc issue được tạo, lỗi thật chỉ nằm ở bản nháp ngoài repo và lịch sử git.
- **Luật của ADR đang hoãn không hiện ở bảng luật.** Người viết ADR hết hoãn phải nhớ thêm dòng; không cổng nào canh.
- **Luật *mã không dùng lại* dựa vào lịch sử git.** Clone nông hay repo dự án hạ nguồn mới dựng không thấy mã đã ngừng; bảng *Phân loại* này chỉ ghi các mã của đợt.
- **Phiên có thể kết thúc khi Core chưa review**, sau một lần chặn. Sửa tiếp chính các tệp đã trong tập không chặn lại. Lớp bắt còn lại là người dùng đọc lời báo, và nhật ký chạm Core giữ nguyên nên phiên kế tiếp chạm Core gặp lại tập đó.
- **Repo dùng bộ khung trước khi có `src/` không còn chạy được.** Cổng đỏ vì thiếu thư mục mã nguồn; sửa `docs/kien-truc-core-module.md` — tệp trong `core-paths` — đòi `core-reviewer`. Muốn dùng lại cho repo như vậy thì phải dựng lại nhánh.
- **Chú thích dưới `src/` trỏ một tệp chuyển hướng.** Người đọc chú thích *nợ E17 ở docs/DEBT.md* phải đi thêm một chặng tới `RULES-*` hay tới issue. Chúng đã cũ trước ADR này; sửa là việc của đợt code.
- **Bảng luật trộn dòng có cổng với dòng chỉ review canh nhiều hơn.** Nguyên tắc *không trộn gợi ý vào bảng luật* ở đầu `RULES.md` đảo lại: người đọc lướt mà bỏ qua cột trạng thái sẽ nghĩ mọi dòng đều có cổng. Cột trạng thái và lệnh (2) là lớp chặn.
- **Lỗi 🟠 trở xuống ngoài phần đổi mất cùng lượt review.** Người soát thấy mà không được báo. Lỗi 🔴 ngoài phần đổi chỉ tới người dùng khi phiên chính chuyển nguyên văn dòng đó; phiên chính bỏ sót thì không lớp nào khác giữ nó.

### Rút lui nếu sai

Không quyết định nào chạm `src/` hay dữ liệu. Người dùng chạy mọi lệnh git.

1. Quyết định 1–3, 8–12: lấy lại `docs/DEBT.md` từ commit trước lượt viết lại; trả ba lệnh đếm, chú giải 🕳️, các hàng *Tra cứu* và trỏ đường về bản cũ từ lịch sử git. Dòng mới ở `RULES-*` giữ được — luật có mã ở bảng luật đúng ở cả hai cách. Issue đã tạo không phải gỡ. Chừng nửa ngày.
2. Quyết định 5: lấy lại khuôn báo cáo của `core-reviewer`, `.claude/CLAUDE.md` §11 và skill `core-review` từ lịch sử git.
3. Quyết định 6, 13: lấy lại bước 3 của `on-stop.sh` và ca test của nó; chạy `bash .claude/hooks/tests/run-tests.sh` tới khi xanh. Dấu vân tay của phiên hết theo lượt dọn phiên cũ của hook `Stop`.
4. Quyết định 7, 14: lấy lại các nhánh và ca test từ lịch sử git; chạy bộ test hook và cổng tài liệu.
5. Viết ADR mới; dòng trạng thái của ADR này thêm đoạn quan hệ theo [`README.md`](README.md) §5.1.

### Dấu hiệu quyết định này bắt đầu sai

- Mục §23 của cổng đỏ cho `core-reviewer` hay `backend-expert` ngay sau lượt thêm dòng, hoặc sau vài dòng luật mới.
- Lệnh (1) in khác 0, hoặc một tệp mới dưới `docs/` bắt đầu giữ danh sách việc.
- Một mã bị cấp lại cho nghĩa khác.
- Phiên chính phải dán nội dung issue vào lời giao gần như mọi lượt, hoặc agent làm lại một việc đã có issue vì không thấy nó.
- Danh sách issue không được tạo, hoặc số issue mở chỉ tăng.
- Một PR mang thay đổi Core chưa qua review, sau những phiên đã dừng với lời báo *đã chặn một lần*.
- Một lỗi 🔴 ngoài phần đổi bị người soát ghi mà không đến tai người dùng; hay một sự cố truy ngược về lỗi 🟠 mà lượt review trước đã thấy ngoài phần đổi.
- Một luật 🕳️ bị vi phạm thật lần thứ hai mà chưa ai dựng cổng; hay người dựng cổng phải đào lịch sử git của `DEBT.md` mới dựng được.
- Một cụm *chưa chốt* đứng qua nhiều đợt việc mà người dùng chưa được hỏi.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Cỡ `DEBT.md`, ba lệnh đếm, 60 mã, bộ luật luôn đọc: đo ở *Bối cảnh*. Hook mâu thuẫn hai vòng: đọc được trên giấy, neo ở *Bối cảnh* bốn. Leo thang do review sinh việc: người dùng quan sát, **chưa đo** bằng số |
| 2 | Phương án đơn giản hơn | *Giữ sổ, chỉ dọn* và *chỉ bỏ mục Nợ review*; bảng *Phương án* nêu chỗ chúng thiếu |
| 3 | Chi phí vận hành thêm | Người dùng giữ GitHub Issues và tạo tay; phiên chính dán issue khi giao việc; bộ test hook giữ thêm ca cho dấu vân tay; bộ luật luôn đọc sát trần, mỗi dòng luật mới phải cân |
| 4 | Ai bảo trì | Bảng `RULES-*`: `architect`. Issue: người dùng. `.claude/`, hook, `check-docs.sh`, bản nháp danh sách issue, tệp chuyển hướng: phiên chính |
| 5 | Rút lui thế nào | Mục *Rút lui nếu sai*: mọi tệp lấy lại được từ lịch sử git; không dữ liệu, không `src/` |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Không quyết định nào chạm `src/`; dòng luật mới mang luật Core sẵn có |

## Thi công

Chưa quyết định nào được thi công ở lượt ghi ADR này. Thứ tự bắt buộc: bước 1 và bước 2 đọc `DEBT.md` bản hiện tại, nên bước 3 chỉ chạy sau cả hai.

| Bước | Tệp | Việc | Ai |
| --- | --- | --- | --- |
| 1 | `docs/RULES-DOCS.md`, `docs/RULES-BE.md`, `docs/RULES-FE.md`, `docs/RULES-CHUNG.md` | Thêm dòng theo bảng *Phân loại* và quyết định 9, 11. Dòng có hàng nợ cùng mã: tự nêu phần chưa phủ, bỏ trỏ `DEBT.md` (quyết định 10); dòng S6 nêu chỗ hở của S12; dòng A7, F46 mang cụm `chưa chốt:`. Ở `RULES-DOCS.md`: gỡ đoạn *D14 và D15 cố ý KHÔNG nằm ở bảng này*; viết lại đoạn *Mã `B*` là nợ của Backend* thành câu chung: mã giữ số ở mọi trạng thái. Dòng ✅ không nêu tên test chưa viết — lệnh rà D39 bắt | `architect` |
| 1 | `docs/RULES.md` | Chú giải 🕳️; lệnh (2); luật mã không dùng lại kèm lệnh (3); bảng *Tệp nào chứa phạm vi nào* bỏ hàng nợ, khai D25, D26 ở `RULES-FE.md`; câu đầu tệp *phải nằm ở danh sách nợ*; hai đoạn dưới lệnh rà D39 nhắc nợ; §10 giữ tiêu đề, câu chuyển hướng mới | `architect` |
| 1 | `docs/adr/README.md` §2, §7, §8, §9 | Chỗ ghi luật chưa có cổng | `architect` |
| 1 | `docs/quy-uoc/README.md`, `docs/quy-uoc/tieu-chi-review.md`, `docs/OWNERSHIP.md`, `docs/README.md` | Câu đổi nghĩa: chỗ ghi luật chưa có cổng, *đề nghị ghi vào `DEBT.md`*, hàng định tuyến sổ nợ | `architect` |
| 1 | `docs/Design/Screens/` — Screen 12, Screen 03 | Cụm *chưa chốt* ở hàng mà câu hỏi F48, F49 chạm, đọc từ `DEBT.md` bản hiện tại | `design-expert` |
| 1 | — | Chạy `bash .claude/check-docs.sh`, đọc §23. Bộ luật agent nào vượt trần thì dừng, báo người dùng; không nới trần, không bỏ dòng | `architect` |
| 2 | Bản nháp ngoài repo | Danh sách issue theo quyết định 3: các hàng mục *Nợ review* và finding về code của vòng soát 2026-10-04, gộp theo người sửa; tiêu đề mở đầu bằng mã trong ngoặc vuông, kèm trỏ dòng `RULES-*` cùng mã. Chú thích trỏ `DEBT.md` dưới `src/` và `scripts/tests/` vào cùng danh sách | Phiên chính |
| 2 | GitHub | Duyệt, tạo issue | Người dùng |
| 3 | `docs/DEBT.md` | Viết lại thành tệp chuyển hướng theo quyết định 2: `kind: tham-chieu`, `verified: khong-ap-dung`, không bảng; trỏ ADR này, `RULES.md` và GitHub Issues | Phiên chính |
| 3 | Tệp sống còn nhắc `DEBT.md` — lệnh `grep -rl` dưới `docs/` và `spec/`, trừ `docs/adr/[0-9]*`, `docs/audit/`, nhật ký quyết định, mục lục ADR | Trỏ đường sang dòng `RULES-*` cùng mã: cơ học. Câu nhắc mã việc hay mã của ADR đang hoãn, hay câu đổi nghĩa: `architect`; câu đổi nghĩa dưới `docs/Design/`: `design-expert`. `docs/wiki-core/fe/trien-khai/05-gate.md` chỉ sửa sau khi lượt song song ngày 2026-10-05 xong | Phiên chính; `architect`; `design-expert` |
| 4 | `.claude/CLAUDE.md` | §1: trỏ đường vòng C4 sang chú thích đầu `.claude/hooks/on-pretool.sh`. §4: bỏ chữ *nợ*. §6: trỏ D14 sang `docs/RULES-DOCS.md`. §8: lớp 3 theo quyết định 6, bỏ câu lớp chặn bật theo sự tồn tại của `src/`. §11: quyết định 5. §9 không đổi. Cỡ tệp không tăng | Phiên chính |
| 4 | `.claude/README.md` | Đoạn *Khi chưa có `src/`*; hàng hook `Stop` của bảng cơ chế và các dòng tả lớp 3; hai chỗ trỏ nợ C4 | Phiên chính |
| 4 | `.claude/agents/core-reviewer.md` | Thay mục *Ngoài phạm vi — đề xuất ghi nợ* của khuôn và câu dẫn tới nó bằng mục chỉ nhận 🔴, theo quyết định 5; bỏ hàng *Tra cứu* trỏ `DEBT.md`; bỏ câu *phạm vi chưa có source* | Phiên chính |
| 4 | `.claude/agents/architect.md` | Mục *Khi chưa có `src/`*; hàng *Tra cứu* trỏ `DEBT.md`; mục *luật mới phải khai được ép bằng gì* theo quyết định 9; phạm vi ghi bỏ `docs/DEBT.md`; mục 6 của checklist | Phiên chính |
| 4 | `.claude/agents/test-engineer.md`, `backend-expert.md`, `frontend-expert.md`, `design-expert.md`, `tech-writer.md` | Mục *Khi chưa có `src/…`*; câu `verified` theo giai đoạn của `design-expert`; hàng *Tra cứu* trỏ `DEBT.md` của `test-engineer` | Phiên chính |
| 4 | `.claude/skills/core-review/SKILL.md`, `.claude/skills/feature-kickoff/SKILL.md` | Quyết định 5. `core-review`: mô tả skill, dòng trỏ đường đầu tệp, bước xử lý 🟠, hết vòng 2, dòng tóm tắt cuối. `feature-kickoff`: bước gọi `core-reviewer` và mục cuối nhắc *ghi nợ*, *hàng nợ* | Phiên chính |
| 4 | `.claude/skills/adr-new/SKILL.md`, `audit-log/SKILL.md`, `doc-audit/SKILL.md`, `review-doc/SKILL.md` | Luật chưa có cổng thành dòng 🕳️ ở `RULES-*`; đề xuất cổng nằm trong báo cáo; D14 trỏ `RULES-DOCS.md` | Phiên chính |
| 4 | `.claude/hooks/on-pretool.sh` | Chú thích đầu tệp nhận nguyên văn danh sách đường hook không thấy của hàng C4 trước khi `DEBT.md` được viết lại | Phiên chính |
| 4 | `.claude/hooks/on-stop.sh`, `.claude/hooks/on-edit.sh`, `.claude/hooks/tests/run-tests.sh` | Quyết định 6, 13 — ca: chặn lần đầu; dừng lại cùng tập thì không chặn, lời báo tới người dùng; thêm một tệp thì chặn lại; review mới hơn rồi sửa thì chặn lại; nhánh chưa gắn `SubagentStop` không đổi. Quyết định 7, 14 — bỏ `[ -d src ]`, gỡ ca *không src* | Phiên chính |
| 4 | `.claude/check-docs.sh` | §14: chú thích và lời FAIL trỏ trạng thái 🕳️ thay `DEBT.md`. §18: chú thích. §10, §27: bỏ lối NOTE thiếu `src/` theo quyết định 14. Chú thích nhắc *giai đoạn 1* | Phiên chính |
| 4 | — | `bash .claude/hooks/tests/run-tests.sh` và `bash .claude/check-docs.sh` xanh | Phiên chính |
| 5 | git | Commit | Người dùng |

Lượt ghi ADR này sửa sẵn: dòng trạng thái của ADR-0045, 0069, 0108, 0111, 0112, 0113, và dòng của ADR này ở [`muc-luc.md`](muc-luc.md).

## Liên quan

- [ADR-0045](0045-cho-src-sua-dinh-nghia-khong-them-ky-hieu-thu-tu.md) — thay thế.
- [ADR-0069](0069-tach-so-no-khoi-rules-md.md), [ADR-0108](0108-hook-stop-hoan-khi-cho-agent-nen-docs-khong-keo-review.md), [ADR-0111](0111-git-danh-sach-cho-phep-cong-docs-chan-mot-lan-architect-truoc-thi-cong-theo-dau-hieu.md), [ADR-0112](0112-so-no-them-nhan-cho-quyet-dinh-kem-lenh-dem-thu-ba-cham-core-khong-quyet-dinh-khong-ghi-nhat-ky.md), [ADR-0113](0113-ban-giao-qua-phien-chinh-no-review-phien-chinh-ghi-tai-lieu-ban-giao-o-spec-go-muc-11-quet-shell-theo-loi-goi.md) — sửa một phần.
- [ADR-0041](0041-cham-tran-corpus-reviewer-thi-rut-file-theo-tieu-chi.md) — thứ tự khi bộ luật chạm trần; *Tiêu cực* thứ nhất dựa vào nó.
- [ADR-0043](0043-lenh-do-cho-phai-lat-doi-sang-nhan-dich-den.md) — lệnh tầng B và danh sách loại trừ; quyết định 11 tránh làm nó in `RULES-DOCS.md`.
- [ADR-0016](0016-phan-phoi-core-bang-clone.md) — lý do repo luôn có `src/` ở quyết định 14, và lý do việc tồn không theo Core.
- [ADR-0110](0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md) — phần hoãn mà loại *luật của ADR đang hoãn* dựa vào.
