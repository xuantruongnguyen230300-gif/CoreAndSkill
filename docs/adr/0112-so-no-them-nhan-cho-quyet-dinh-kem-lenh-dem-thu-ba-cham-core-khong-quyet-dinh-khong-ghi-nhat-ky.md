---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0112 — Sổ nợ nhận giá trị thứ tư `⏳ chờ quyết định — <ai>` kèm lệnh đếm thứ ba; việc chạm Core không mang quyết định thì không ghi nhật ký; quyết định 5 của ADR-0111 giữ nguyên

> **Trạng thái:** Đã chấp nhận (2026-09-29) · Sửa một phần bởi [ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md) (2026-10-05)

## Bối cảnh

[ADR-0111](0111-git-danh-sach-cho-phep-cong-docs-chan-mot-lan-architect-truoc-thi-cong-theo-dau-hieu.md) được chấp nhận ngày 2026-09-29 với ba chỗ còn hở. Người dùng trả lời cả ba trong ngày.

**Một — giá trị thứ tư của cột *Chặn bởi*.** Quyết định 10 của ADR-0111 chọn giá trị `⏳ chờ quyết định` cho [`../DEBT.md`](../DEBT.md) nhưng hoãn thi công, vì nó mâu thuẫn quyết định 4 của [ADR-0045](0045-cho-src-sua-dinh-nghia-khong-them-ky-hieu-thu-tu.md): *"Ký hiệu thứ tư bị loại"*. ADR-0045 loại vì hai lý do đã đo:

1. giá trị không chứa `src/` nằm ngoài cả hai lệnh đếm ở đầu bảng, thành vùng mù thứ hai;
2. tách vài hàng ra làm các hàng còn lại trông như đã soát.

Hàng chờ ai đó quyết tới nay ghi `🔧 làm được ngay — sau khi người dùng chọn hướng` và biến thể. Chúng nằm trong lệnh đếm `🔧`, nên số của lệnh đó trộn hai loại: cổng dựng được ngay, và cổng chưa dựng được vì chưa ai quyết dựng theo hướng nào.

**Hai — việc chạm Core không mang quyết định.** Quyết định 2 của ADR-0111 bỏ bước `architect` trước thi công cho việc chạm Core không mang dấu hiệu nào ở [`README.md`](README.md) §2 và không đưa code mới vào Core. Nó không nói việc đó còn phải để lại ghi nhận không. `.claude/agents/architect.md` (chuỗi `và phải có ghi nhận quyết định`) đòi ADR hoặc một dòng nhật ký cho **mọi** thay đổi chạm Core. [`README.md`](README.md) §2 chỉ đòi dòng nhật ký cho **quyết định** nhỏ.

**Ba — quyết định 5 của ADR-0111 dùng lại phương án ADR-0108 đã loại.** Bảng phương án của [ADR-0108](0108-hook-stop-hoan-khi-cho-agent-nen-docs-khong-keo-review.md) ghi *"Chặn một lần rồi thả, hoặc chỉ nhắc"* bị loại vì *"Bỏ chốt bằng máy. Người dùng chọn giữ chốt"*. Sau khi ADR-0111 được chấp nhận, người dùng được nêu rõ điều này và được hỏi lại.

## Quyết định

Người dùng chốt ngày 2026-09-29.

| # | Quyết định |
| --- | --- |
| 1 | Cột *Chặn bởi* của [`../DEBT.md`](../DEBT.md) nhận giá trị thứ tư `⏳ chờ quyết định — <ai>`, trong đó `<ai>` là người phải quyết. Kèm một lệnh đếm thứ ba, neo vào hình dạng hàng 5 cột như hai lệnh cũ, đếm hàng mà ô cuối chứa chuỗi `⏳ chờ quyết định` **ở bất kỳ vị trí nào** — để hàng tách đôi, như hàng chỉ một vế chờ quyết, cũng được đếm. Lý do thứ hai của ADR-0045 do lượt soát lại toàn bảng bịt — lượt soát của quyết định 10 ADR-0111, phủ mọi hàng của cột chứ không riêng hàng ⏳, vì lượt chuyển nhãn tách hàng khỏi 🔧. Tới khi lượt đó xong, đoạn cảnh báo *chưa soát* ở `DEBT.md` giữ nguyên |
| 2 | Việc chạm Core không mang dấu hiệu nào ở [`README.md`](README.md) §2 và không đưa code mới vào Core thì **không** cần dòng nhật ký. Hai ngoại lệ, đều là luật đã có: trong lúc làm phải chọn giữa hai cách hay chốt một giá trị thì ghi một dòng ở [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md) theo §2; thay đổi Core mà dự án hạ nguồn phải làm theo thì ghi `CHANGELOG.md` theo quyết định 5 của [ADR-0100](0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md). `.claude/agents/architect.md` bỏ câu đòi ghi nhận cho mọi thay đổi chạm Core |
| 3 | Quyết định 5 của ADR-0111 giữ nguyên. Người dùng xác nhận sau khi được nêu rằng ADR-0108 từng loại chính phương án đó. Không tệp nào đổi |

Phần của kiến trúc sư trong quyết định 1: lệnh đếm thứ ba khớp chuỗi ở mọi vị trí trong ô, và ô mang giá trị mới không chứa chuỗi `src/`, trừ hàng tách đôi thật. Phần còn lại do người dùng chọn.

Quan hệ với ADR cũ, theo [`README.md`](README.md) §5.1:

- **ADR-0045 — sửa một phần.** Hết hiệu lực: quyết định 4. Còn hiệu lực: quyết định 1 — ⏳ chờ `src/` giữ glyph và chuỗi, hai lệnh đếm cũ giữ nguyên đầu vào; quyết định 2, 3, 5. Kết quả của lệnh đếm `🔧` đổi, vì hàng chờ quyết định rời khỏi nó; lệnh không đổi.
- **ADR-0111 — bổ sung.** Quyết định 1 trả lời *Câu hỏi còn mở* 1 và gỡ điều kiện *"chưa thi công"* của quyết định 10. Quyết định 2 thêm vào quyết định 2 của nó. Quyết định 3 xác nhận quyết định 5 của nó. Không phần nào hết hiệu lực.

## Phương án đã cân nhắc và vì sao loại

| # | Phương án bị loại | Vì sao loại |
| --- | --- | --- |
| 1 | Hướng (b) ở *Câu hỏi còn mở* 1 của ADR-0111: không thêm giá trị, hàng chờ quyết định ghi `🔧 làm được ngay — chờ <ai> quyết` | Lệnh đếm `🔧` vẫn trộn hàng dựng được ngay với hàng chưa ai quyết; muốn biết ai đang phải quyết gì thì đọc từng ô. Người dùng chọn (a) |
| 1 | Thêm giá trị thứ tư, không thêm lệnh đếm | Đúng lý do thứ nhất ADR-0045 đã đo: nhóm mới nằm ngoài mọi lệnh đếm |
| 1 | Lệnh đếm thứ ba chỉ khớp ô **mở đầu** bằng giá trị mới — cùng khuôn lệnh `🔧` | Hàng tách đôi mở đầu bằng giá trị khác, như `🔧 làm được ngay — riêng … chờ quyết`, rơi khỏi lệnh; vế chờ quyết thành vùng mù bên trong ô. Kiến trúc sư loại |
| 2 | Giữ câu ở `.claude/agents/architect.md`: mọi thay đổi chạm Core có ADR hoặc một dòng nhật ký | Mỗi lần sửa lỗi Core thêm một dòng không mang quyết định nào; nhật ký *quyết định nhỏ* thành nhật ký thay đổi. Vết của thay đổi đã có ở lịch sử git và lượt `core-reviewer` |
| 2 | Đòi ADR cho mọi thay đổi chạm Core — dòng *Cơ chế chống* ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 | Ngược quyết định 6 của ADR-0109 và quyết định 2 của ADR-0111 |
| 3 | Lật quyết định 5, trở về chặn tới khi cổng xanh như ADR-0108 | Vòng chặn không đáy mà *Bối cảnh* của ADR-0111 nêu. Người dùng giữ quyết định 5 |

## Hệ quả

### Tích cực

- Hàng chờ quyết định có lệnh đếm riêng và nêu tên người phải quyết.
- Không thêm vùng mù: ba lệnh phủ mọi hàng trừ hàng `🚫`, như trước.
- Khi lượt soát lại xong, lệnh đếm `🔧` chỉ còn đếm hàng dựng được ngay.
- Nhật ký quyết định chỉ còn chứa quyết định.

### Tiêu cực

- **Lý do thứ hai của ADR-0045 còn sống tới khi lượt soát lại xong.** Lượt chuyển nhãn đầu tiên tìm hàng bằng cụm chữ. Hàng chờ quyết định mà không dùng các cụm đó vẫn mang `🔧`, và cột trông như đã phân loại.
- **Hai giá trị dùng chung glyph `⏳`.** Người lọc bằng mắt theo glyph gộp *chờ đối tượng thật* với *chờ người quyết*; phải đọc chữ sau glyph.
- **Ô mang giá trị mới mà chứa chuỗi `src/` bị lệnh đếm thứ nhất đếm nhầm.** Không cổng nào canh; ba lệnh là lệnh chạy tay.
- **Hàng mắc kẹt khi quyết định đã có mà không ai lật ô.** Điều kiện kích hoạt là một quyết định, không phải sự kiện máy nhìn thấy. Ca đã xảy ra trước ADR này: bảy hàng B26, B27, B28, F44, F45, F47, R8 còn chữ *"chờ người dùng quyết"* sau khi người dùng đã chốt ([`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md), 2026-09-29).
- **Ô *Chặn bởi* dài thêm.** Bốn giá trị, cộng người sửa và điều kiện kích hoạt ghi trong cùng ô theo quyết định 10 của ADR-0111.
- **Quyết định 2 dựa vào phán đoán của người thi công.** Người thi công cho rằng *"không có gì phải chọn"* trong khi thật ra đã chọn thì quyết định trôi mất, không dòng nào ghi. Lớp bắt còn lại là `core-reviewer` hỏi *vì sao*.
- Quyết định 3 giữ nguyên cái giá ADR-0111 đã ghi cho quyết định 5.

### Rút lui nếu sai

Không quyết định nào chạm `src/` hay dữ liệu.

1. Quyết định 1: chạy lệnh đếm thứ ba để liệt kê hàng mang giá trị mới; đổi từng hàng về `🔧 làm được ngay — chờ <ai> quyết`; gỡ lệnh đếm thứ ba và dòng chú giải. Viết ADR mới; dòng trạng thái của ADR-0045 thêm đoạn quan hệ theo [`README.md`](README.md) §5.1.
2. Quyết định 2: thêm lại câu vào `.claude/agents/architect.md` — phiên chính sửa — kèm ADR mới.
3. Quyết định 3: như bước 3 mục *Rút lui nếu sai* của ADR-0111.

### Dấu hiệu quyết định này bắt đầu sai

- Lệnh đếm thứ ba không giảm qua nhiều lượt: người phải quyết không biết mình đang được chờ.
- Một hàng mang `⏳ chờ quyết định` mà dòng nhật ký hay ADR của chính quyết định đó đã có.
- Có người đọc nhầm ⏳ chờ `src/` với `⏳ chờ quyết định`.
- `core-reviewer` hay người đọc hỏi *"vì sao Core đổi thế này"* về một thay đổi đã bỏ qua nhật ký, và không dòng nào trả lời.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Mâu thuẫn giữa ADR-0111 và ADR-0045 đọc được trên giấy. Hàng chờ quyết định lẫn trong lệnh `🔧`: đo bằng `grep` các cụm *"chờ người dùng"*, *"sau khi người dùng"* ở `DEBT.md`, không chép số. Bảy hàng kẹt chữ cũ: nêu tên ở *Tiêu cực*. Quyết định 2: mâu thuẫn giữa hai tệp, neo ở *Bối cảnh* |
| 2 | Phương án đơn giản hơn | Hướng (b) và *giữ câu cũ*; bảng *Phương án* nêu chỗ chúng thiếu |
| 3 | Chi phí vận hành thêm | Một lệnh đếm chạy tay; người ghi một quyết định phải lật ô *Chặn bởi* của hàng liên quan trong cùng lượt |
| 4 | Ai bảo trì | `DEBT.md`: `architect`. `.claude/agents/architect.md`: phiên chính |
| 5 | Rút lui thế nào | Mục *Rút lui nếu sai*: lệnh đếm thứ ba liệt kê đúng các hàng phải đổi về; không dữ liệu, không `src/` |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Không quyết định nào chạm `src/`; quyết định 2 không đổi câu hỏi ngưỡng cho code mới vào Core |

## Thi công

| # | Tệp phải sửa | Ai |
| --- | --- | --- |
| 1 | [`../DEBT.md`](../DEBT.md): bảng chú giải, ba lệnh đếm, đoạn *đừng cộng*, đoạn cảnh báo, chuyển các hàng chờ quyết định sang giá trị mới. [`../RULES.md`](../RULES.md) §10: số lệnh đếm. Phần cấu trúc làm cùng lượt ghi ADR này; lượt soát lại toàn bảng là lượt sau | `architect` |
| 2 | `.claude/agents/architect.md`, chuỗi `và phải có ghi nhận quyết định`. Dòng *Cơ chế chống* ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 còn đòi ADR cho mọi thay đổi chạm Core — đã có ở hàng 2 mục *Thi công* của ADR-0111 | Phiên chính; `architect` |
| 3 | Không | — |

## Liên quan

- [ADR-0111](0111-git-danh-sach-cho-phep-cong-docs-chan-mot-lan-architect-truoc-thi-cong-theo-dau-hieu.md) — bổ sung bởi quyết định 1, 2, 3.
- [ADR-0045](0045-cho-src-sua-dinh-nghia-khong-them-ky-hieu-thu-tu.md) — sửa một phần bởi quyết định 1.
- [ADR-0108](0108-hook-stop-hoan-khi-cho-agent-nen-docs-khong-keo-review.md) — phương án bị loại mà quyết định 3 xác nhận dùng lại; dòng trạng thái của nó đã ghi quan hệ với ADR-0111.
- [ADR-0100](0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md) quyết định 5 — ngoại lệ `CHANGELOG.md` của quyết định 2.
