---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0041 — Chạm trần corpus của `core-reviewer` thì rút file khỏi *Bộ luật* theo tiêu chí D36, không nới trần và không tách file cho vừa

> **Trạng thái:** Đã chấp nhận (2026-09-19)

## Bối cảnh

Mục §23 của cổng tài liệu đỏ lần đầu tiên vào 2026-09-19: bộ luật của `core-reviewer` ở phạm vi BE vượt trần 400 KB. Đây là lần đầu trần đó **thật sự chặn** một lượt, chứ không còn là một con số nằm trong [`../RULES.md`](../RULES.md) chờ ai đó chạm tới.

Ba sự thật đo được tại thời điểm quyết định, đọc bằng chính phép đo của §23 (`bash .claude/check-docs.sh`, mục §23 in cỡ bộ luật của từng agent):

1. **Không dòng nào vừa được thêm vào bảng định tuyến của `core-reviewer`.** Corpus vượt trần vì các file luật nó trỏ tới phình dần trong suốt pha thi công, cộng thêm phần văn bản mới ghi vào [`../RULES.md`](../RULES.md) trong chính lượt này. Không có một thay đổi nào để đổ lỗi — đó là hình dạng bình thường của loại sự cố này, và là lý do trần phải là một cổng chứ không phải một lời nhắc.
2. **`docs/database/script-runbook.md` không nằm trong bộ luật của `core-reviewer`.** Nó phình +6952 B trong pha này, nhưng phần phình đó **không** đóng góp một byte nào vào con số của §23. Giả thuyết ban đầu — rằng hai file vừa vượt 50 KB của luật D38 là nguyên nhân — đúng một nửa.
3. Trong *Bộ luật — phạm vi BE*, file lớn nhất là `docs/database/schema-core.md` — và nó là **bảng định nghĩa** (bảng, cột, index, khoá quyền), không phải văn xuôi luật. Chính luật D38 ở [`../RULES.md`](../RULES.md) đã phân loại nó như vậy từ trước, khi miễn trừ nó khỏi ngưỡng 50 KB của file luật. Việc phân loại đó có trước sự cố này và không phải do sự cố này sinh ra.

Ràng buộc cứng: thông điệp của chính cổng khai *"không nới ngưỡng"*, và con số 400 KB không phải ước lượng — nó là kết quả đo từ sự cố corpus ~780 KB ở dự án tiền nhiệm, nơi ba lượt review liên tiếp chết giữa chừng và một lượt để lại lỗi cố ý trong code mà không phát hiện ra.

## Quyết định

Chuyển `docs/database/schema-core.md` từ mục *Bộ luật — phạm vi BE* xuống mục *Tra cứu* của [`../../.claude/agents/core-reviewer.md`](../../.claude/agents/core-reviewer.md), giữ nguyên câu mô tả chủ đề để dòng đó vẫn nêu đúng lúc nào phải mở file.

Khi trần corpus của một agent bị chạm, thứ tự biện pháp là: **(1)** phân loại lại theo đúng tiêu chí D36 — *Bộ luật* là file agent phải tuân ở **mọi** việc, *Tra cứu* là file chỉ mở khi chủ đề chạm tới; **(2)** nếu không còn hàng nào sai chỗ thì làm nhẹ chính file luật theo D38; **(3)** không bao giờ nới trần.

Phép thử để xếp một file vào *Tra cứu*: **một lượt review điển hình của phạm vi đó có thể kết luận đúng mà không mở file này hay không.** Một danh mục tra cứu — bảng cột, bảng mã, bảng ánh xạ — gần như luôn trả lời "có"; một file quy ước tầng thì gần như luôn trả lời "không".

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Nới trần từ 400 KB lên 450 KB
**Được:** một dòng sửa, cổng xanh ngay, không ai phải đọc ít đi.
**Mất:** trần mất hết nghĩa. Một ngưỡng bị nới ở đúng lần đầu nó chặn là một ngưỡng đã tuyên bố rằng nó sẽ nới ở mọi lần sau.
**Vì sao loại:** con số 400 KB đến từ một sự cố đã trả giá, không từ cảm tính. Và thứ trần này bảo vệ không phải dung lượng đĩa mà là **khả năng một lượt review chạy hết**; nới trần không làm lượt review sống lâu hơn, nó chỉ làm cổng thôi báo.

### Phương án B — Tách `be-api-controller.md` và `script-runbook.md` theo D38 ngay trong lượt này
**Được:** giải cả cảnh báo mềm D38 lẫn cổng cứng §23 trong một mạch; đúng hướng dài hạn.
**Mất:** `script-runbook.md` **không** nằm trong bộ luật của `core-reviewer`, nên tách nó giảm 0 byte cho §23 — nửa việc này không giải quyết vấn đề đang chặn. `be-api-controller.md` thì giảm thật, nhưng nó là file reviewer phải tuân ở mọi lượt BE: cắt phần *vì sao / bẫy* ra không đổi việc file vẫn phải đọc, và phần luật còn lại vẫn ở mức vài chục KB.
**Vì sao loại:** nó là phẫu thuật nội dung trên hai file luật lớn, thực hiện giữa lúc một agent khác đang sửa đúng một trong hai file — rủi ro mất thay đổi là thật. Và nó trộn hai việc có tiêu chí khác nhau: D38 hỏi *"file này có quá dài để đọc không"*, §23 hỏi *"agent này có phải đọc file này ở mọi việc không"*. Trả lời câu thứ hai bằng công cụ của câu thứ nhất là cách bỏ sót đúng hàng sai chỗ. Việc tách vẫn cần làm, nhưng là một việc riêng, có phạm vi riêng.

### Phương án C — Rút `docs/quy-uoc/be-performance.md` (file nhỏ, vừa đủ qua vạch)
**Được:** động chạm ít nhất, một dòng.
**Mất:** biên còn lại mỏng tới mức lần sửa tài liệu kế tiếp đẩy cổng đỏ lại.
**Vì sao loại:** chọn theo **cỡ vừa đủ** chứ không theo tiêu chí. Cắt đúng bằng phần vượt là cách hợp thức hoá việc nới trần mà vẫn giữ được con số trên giấy — và nó bỏ lại hàng thật sự sai chỗ đúng nơi cũ.

### Phương án D — Rút `docs/database/migration-policy.md` thay cho `schema-core.md`
**Được:** cũng hợp tiêu chí, cũng là chủ đề chỉ chạm khi có migration trong diff.
**Mất:** chính sách migration ràng buộc **mọi** thay đổi chạm dữ liệu, kể cả thay đổi không thêm bảng nào; còn một danh mục cột thì chỉ cần khi đụng đúng cột đó.
**Vì sao loại:** giữa hai hàng cùng hợp tiêu chí, hàng có tính **danh mục** thuần tuý là hàng đúng để rút trước. Hàng còn lại vẫn là ứng viên cho lần chạm trần sau.

## Hệ quả

### Tích cực

- Cổng §23 xanh trở lại bằng đúng cơ chế nó đề nghị, không bằng một con số bị sửa.
- Biên còn lại đủ rộng để vài đợt sửa tài liệu nữa không chạm trần — không phải cắt sát vạch rồi phải quay lại.
- Bảng định tuyến trung thực hơn trước: một danh mục tra cứu nay nằm ở mục dành cho tra cứu, thay vì ở mục "phải tuân ở mọi việc".
- Thứ tự biện pháp được ghi ra, nên lần chạm trần sau không phải tranh luận lại từ đầu.

### Tiêu cực

- **`core-reviewer` không còn mở danh mục schema theo mặc định.** Một lượt review chạm entity mà reviewer không theo dòng *Tra cứu* sẽ bỏ sót luật schema. Đây là mất mát thật và không lấy lại được bằng nỗ lực: thứ duy nhất bù vào là một dòng trong bảng, tức là phụ thuộc vào việc agent đọc bảng đúng cách.
- **Mỗi lần rút một hàng, "vốn hiểu biết mặc định" của reviewer giảm đi, và không cổng nào canh mức giảm đó.** §23 đo byte, không đo chất lượng review. Nghĩa là biện pháp này có đáy, và đáy đó vô hình.
- Biên vừa giành lại được sẽ bị ăn mòn tiếp bởi chính đà phình đã gây ra lần này. Lần sau nhiều khả năng không còn ứng viên sạch như một danh mục thuần tuý, và lúc đó chi phí là phẫu thuật nội dung thật — tức phương án B, chỉ là đắt hơn vì làm muộn hơn.
- Quyết định này không đụng tới nguyên nhân gốc: tài liệu luật vẫn đang phình. Nó mua thời gian, không chữa bệnh.

### Dấu hiệu quyết định này bắt đầu sai

- `core-reviewer` bỏ sót từ **hai** finding trở lên thuộc luật schema vì không mở `schema-core.md`. Khi đó dòng *Tra cứu* không đủ, và câu trả lời là tách phần **ràng buộc** của `schema-core.md` ra một file nhỏ để đưa lại vào *Bộ luật*, **không** phải kéo nguyên file 70 KB về chỗ cũ.
- Lần chạm trần kế tiếp không tìm được hàng nào sai chỗ theo tiêu chí D36. Khi đó vấn đề đã chuyển hẳn sang "file luật quá to" và biện pháp đúng là D38, không phải bảng định tuyến.

## Liên quan

| Đọc gì | Vì sao |
| --- | --- |
| [`../RULES.md`](../RULES.md) | Luật D36 (ngưỡng cỡ bộ luật) và D38 (file luật vượt 50 KB) — hai luật sinh ra hai biện pháp ở trên |
| [`../../.claude/agents/core-reviewer.md`](../../.claude/agents/core-reviewer.md) | Bảng định tuyến bị sửa bởi quyết định này |
| [`../kien-truc-core-module.md`](../kien-truc-core-module.md) | Hình dạng kiến trúc mà các file luật trên mô tả |
