---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 2026-08-23 — Cổng không tồn tại: script được tài liệu hướng dẫn chạy không có trên đĩa

> Sự cố có thật ở **dự án tiền nhiệm**. Ghi lại ở đây vì nó là lý do tồn tại của [`../adr/0011-ci-github-actions.md`](../adr/0011-ci-github-actions.md) và của luật T1 trong [`../RULES.md`](../RULES.md).
>
> Viết theo khuôn bốn phần ở [`README.md`](README.md).

---

## 1. Hiện tượng

Ngày 2026-08-23, người ta phát hiện **ba mục cổng frontend đã không chạy suốt một thời gian dài.**

Không ai biết. Và điều đáng nói là **không có gì trông bất thường**:

- Tài liệu vẫn mô tả các mục cổng đó như đang hoạt động bình thường, kèm hướng dẫn chạy.
- Người chạy cổng vẫn thấy kết quả kết thúc bình thường, không có thông báo lỗi nào nổi bật.
- Không có lần chạy nào bị đỏ, vì không có gì chạy để mà đỏ.

Nói cách khác: **hiện tượng của sự cố này là sự vắng mặt của hiện tượng.** Đó là điều làm nó khác mọi sự cố khác trong khu này — không có gì hỏng, chỉ có một lớp bảo vệ đã ngừng tồn tại trong im lặng.

Khoảng thời gian ba mục cổng đó không có gì canh không xác định được chính xác, vì không có log nào để truy ngược.

## 2. Nguyên nhân gốc

**Script cổng mà tài liệu hướng dẫn chạy không tồn tại trên đĩa.**

Cơ chế biến việc đó thành im lặng gồm hai lớp:

### Lớp 1 — Lệnh gãy ngay từ đầu, chuỗi vẫn chạy tiếp

Quy trình kiểm được mô tả như một chuỗi lệnh chạy nối nhau. Lệnh đầu tiên gọi script không tồn tại nên hỏng ngay lập tức. Nhưng chuỗi **không dừng lại** — các lệnh sau vẫn chạy bình thường và vẫn cho kết quả xanh.

Người chạy nhìn phần cuối màn hình, thấy các lệnh sau báo thành công, và kết luận cổng đã qua. Thông báo lỗi của lệnh đầu nằm ở phía trên, lẫn giữa các dòng đầu ra khác, và trôi khỏi tầm nhìn.

Đây là điểm mấu chốt: **một chuỗi kiểm không fail-fast sẽ biến lỗi thành nhiễu.**

### Lớp 2 — Không có máy nào chạy hộ

Repo cố ý không có CI. Đó là lựa chọn có ý thức, ghi rõ trong luật repo của họ, kèm chính câu nhận định *không còn máy nào chạy hộ*.

Hệ quả khuếch đại của lựa chọn đó, khi ghép với lớp 1:

| Nếu có CI | Vì không có CI |
| --- | --- |
| Script biến mất khỏi đĩa làm lần chạy kế tiếp đỏ | Không có lần chạy nào để mà đỏ |
| Có log của mọi lần chạy, đối chiếu được | Không có gì để trả lời câu *"mục cổng này lần cuối chạy là bao giờ"* |
| Cổng chạy trên máy trung lập, cùng một cách với mọi người | Cổng chạy trên máy của người viết, mỗi người một môi trường |

Nguyên nhân gốc, phát biểu ở mức cơ chế: **một cổng chỉ tồn tại trên giấy khi không có gì chứng minh nó đang chạy.** Tài liệu mô tả cổng không phải bằng chứng cổng tồn tại — tài liệu là thứ dễ đúng nhất và trôi lặng nhất.

### Vì sao không có gì bắt được

- **Không có cổng nào kiểm cổng.** Cổng tài liệu của họ kiểm nhiều thứ, nhưng không kiểm rằng một script được tài liệu nhắc tới có thật sự tồn tại trên đĩa ở dạng thực thi được.
- **Không có gì kiểm rằng một mục cổng thực sự phát hiện được vi phạm.** Kể cả nếu script tồn tại, một mục cổng viết sai có thể luôn xanh — và tình trạng đó cũng không phân biệt được với "không có vi phạm nào".
- **Tài liệu là bên duy nhất khẳng định cổng đang chạy**, và không có gì đối chiếu lời khẳng định đó với thực tế.

## 3. Cách vá

Cách vá tại chỗ khi phát hiện gồm hai phần:

1. **Ghi nhận sự thật vào tài liệu**, thay vì lặng lẽ sửa. Luật repo của họ được cập nhật để nói rõ tình trạng và ngày phát hiện — nhờ đó sự cố này còn truy được, và bản ghi bạn đang đọc mới viết được.
2. **Khôi phục script cổng** và mô tả lại chính xác mục nào đang chạy thật, mục nào chỉ là kế hoạch.

Phần hai đáng chú ý vì nó tạo ra một tập quán tốt: tài liệu cổng của họ về sau **tách bạch mục đang chạy với mục chưa bật**, thay vì liệt kê chung một danh sách. Một mục cổng chưa bật mà nằm chung danh sách với mục đang chạy là một lời hứa giả — người đọc sẽ tin cả danh sách.

**Nguyên nhân gốc vẫn còn ở repo đó:** vẫn không có CI, nên vẫn không có gì chứng minh cổng đang chạy. Cách vá đã sửa một trường hợp cụ thể, không sửa lớp lỗi.

## 4. Cổng nào lẽ ra phải bắt — và CoreAndSkill chặn thế nào

Bốn cơ chế, mỗi cái chặn một lớp của cách hỏng này.

### 4.1 Có CI — [`../adr/0011-ci-github-actions.md`](../adr/0011-ci-github-actions.md)

Mỗi PR chạy đủ cổng trên một máy trung lập. Hai thứ thu được:

- Script biến mất khỏi đĩa làm CI **đỏ ngay lần chạy kế tiếp**, tính bằng giờ chứ không bằng tháng.
- Có **log để đối chiếu**. Câu hỏi *"mục cổng này thật sự đang chạy chứ"* trả lời được bằng cách mở lần chạy gần nhất, chứ không bằng cách tin vào tài liệu.

### 4.2 Luật T1 — mọi detector phải có test kiểm chính nó

Luật T1 trong [`../RULES.md`](../RULES.md) yêu cầu mỗi detector có một test chứng minh nó **bắt được vi phạm thật** và **bỏ qua thứ không phải vi phạm**.

Đây là lớp phòng thủ chống dạng hỏng tinh vi hơn: cổng chạy, nhưng không phát hiện được gì. Không có test kiểm detector thì "luôn xanh vì không có lỗi" và "luôn xanh vì hỏng" là hai trạng thái không phân biệt được từ bên ngoài.

Đáng ghi nhận: chính dự án tiền nhiệm đã có tập quán này ở phần test kiến trúc, và audit xếp nó vào nhóm điểm hiếm. CoreAndSkill nâng nó thành luật áp cho **mọi** detector, không riêng test kiến trúc.

### 4.3 Cổng phải fail-fast

Một lệnh trong chuỗi gãy thì cả chuỗi dừng và báo đỏ. Không chạy tiếp, không để lỗi trôi lên phía trên màn hình rồi kết thúc bằng một dòng xanh.

Nguyên tắc phát biểu tổng quát: **kết quả của một cổng phải là một trạng thái duy nhất ở cuối, không phải một chuỗi dòng để người đọc tự tổng hợp.** Con người đọc phần cuối màn hình.

### 4.4 Mọi lệnh của cổng nằm trong danh sách cho phép của harness

Nếu một lệnh của cổng làm harness dừng lại hỏi ý kiến, thì trên thực tế cổng đó không chạy — nó chỉ chạy khi có người ngồi cạnh bấm đồng ý, và trong phiên tự động thì nó treo hoặc bị bỏ qua.

**Một cổng bị hỏi lại giữa chừng là cổng thực tế không ai chạy.** Danh sách cho phép của harness phải phủ đủ mọi lệnh mà cổng cần.

### 4.5 Danh sách kiểm — một cổng có thật sự tồn tại không

Sáu câu hỏi. Trả lời "không" cho bất kỳ câu nào nghĩa là cổng đó chưa chứng minh được nó tồn tại.

| # | Câu hỏi | Nếu "không" thì sao |
| --- | --- | --- |
| 1 | Script hoặc lệnh của cổng có thật sự nằm trong repo, ở dạng chạy được? | Cổng chỉ tồn tại trong tài liệu |
| 2 | Nó có chạy trên máy trung lập ở mỗi PR? | Cổng chỉ chạy khi có người nhớ chạy |
| 3 | Chuỗi lệnh có dừng ngay khi một lệnh gãy? | Lỗi trở thành nhiễu và trôi khỏi tầm nhìn |
| 4 | Có lần chạy nào **đỏ thật** khi cố tình vi phạm chưa? | Không phân biệt được "xanh vì đúng" với "xanh vì hỏng" |
| 5 | Mọi lệnh nó cần có nằm trong danh sách cho phép của harness? | Cổng treo hoặc bị bỏ qua trong phiên tự động |
| 6 | Có log của lần chạy gần nhất, đối chiếu được? | Không trả lời được câu "nó chạy lần cuối bao giờ" |

Câu 4 là câu hay bị bỏ nhất, và cũng là câu luật T1 sinh ra để trả lời. **Một cổng chưa từng báo đỏ là một cổng chưa từng được chứng minh** — nó có thể đang canh, mà cũng có thể đang ngủ.

### 4.6 Điều CoreAndSkill KHÔNG kết luận từ sự cố này

Sự cố này **không** chứng minh rằng lựa chọn không CI của dự án tiền nhiệm là sai trong bối cảnh của họ.

Bối cảnh đó là: một repo cá nhân, một người làm, phạm vi thiệt hại giới hạn trong một dự án. Với hoàn cảnh ấy, cái giá của việc dựng và bảo trì CI có thể lớn hơn cái giá của rủi ro — và họ đã ghi rõ đó là lựa chọn có chủ đích chứ không phải thiếu sót.

Điều đổi kết luận là **bối cảnh của CoreAndSkill khác**: đây là bộ khung nhiều dự án dùng chung, nên một lỗi lọt qua cổng ở đây sẽ được nhân bản sang mọi dự án dựng trên nó. Phạm vi thiệt hại nhân lên, còn chi phí CI thì không đổi.

Ghi điều này lại vì một lý do cụ thể: **một bản ghi audit dễ bị đọc thành lời kết tội một quyết định.** Nó không phải thế. Nó ghi lại rằng một quyết định hợp lý trong bối cảnh A trở thành rủi ro không chấp nhận được trong bối cảnh B — và đó mới là bài học mang đi được.

## 5. Bài học tổng quát

> **Một cổng hỏng âm thầm tệ hơn không có cổng, vì nó tạo cảm giác được bảo vệ.**

Không có cổng thì người ta biết mình không được bảo vệ và tự cẩn thận. Có một cổng đã chết thì người ta tin vào nó, bỏ bớt sự cẩn thận, và mức bảo vệ thực tế **thấp hơn** trường hợp không có cổng nào.

Ba điều rút ra, dùng được cho tình huống khác:

1. **Mọi cổng phải tự chứng minh nó đang chạy.** Một dấu vết mà người khác đối chiếu được — một lần chạy trên CI có mốc thời gian — chứ không phải một câu trong tài liệu.
2. **Tài liệu mô tả một cơ chế không phải bằng chứng cơ chế đó tồn tại.** Đây là dạng cụ thể của điều mà [`../README.md`](../README.md) đã cảnh báo: cổng xanh không có nghĩa nội dung đúng, và tài liệu đúng hình thức không có nghĩa hệ thống đúng.
3. **Hai trạng thái "xanh vì không có lỗi" và "xanh vì cổng hỏng" phải phân biệt được từ bên ngoài.** Đây chính là việc của luật T1. Một cổng chưa từng báo đỏ là một cổng chưa từng được chứng minh.

Hệ quả cho tập quán viết tài liệu ở repo này: khi mô tả một cổng, luôn **tách mục đang chạy khỏi mục chưa bật**, và không bao giờ suy ra trạng thái của một cổng từ tài liệu — chỉ suy từ lần chạy gần nhất.

## 6. Liên quan

- [`../adr/0011-ci-github-actions.md`](../adr/0011-ci-github-actions.md) — quyết định sinh ra từ sự cố này
- [`../adr/0007-fe-giu-cau-truc-thu-muc.md`](../adr/0007-fe-giu-cau-truc-thu-muc.md) — ranh giới FE chỉ có tác dụng khi cổng thật sự chạy
- [`../RULES.md`](../RULES.md) — luật T1, và cột "Ép bằng gì" của mọi luật
- [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) — cổng FE, mục nào đang chạy thật
- [`README.md`](README.md) — khuôn và luật của khu này
