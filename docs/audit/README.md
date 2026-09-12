---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `audit/` — postmortem sự cố và kết quả audit

> **Khu này giữ bài học.** Không phải để tưởng niệm, mà vì mỗi mục ở đây **sinh ra một mục cổng mới**.
>
> Một sự cố không có bản ghi sẽ lặp lại. Một bản ghi không nói được *cổng nào lẽ ra phải bắt* thì chỉ là chuyện kể.

---

## 1. Vì sao khu này tồn tại

Nó là **hệ quả trực tiếp của [`../adr/0010-comment-toi-thieu.md`](../adr/0010-comment-toi-thieu.md)**.

Ở dự án tiền nhiệm, lịch sử sự cố được kể lại ngay trong comment của file code — có file mà comment chiếm hơn sáu mươi phần trăm nội dung. Cách đó giữ được bài học rất tốt, nhưng trả bằng chi phí đọc trên mọi lượt đọc file, cho cả người lẫn agent.

CoreAndSkill chọn comment tối thiểu. Đổi lại, **bài học phải có chỗ khác để sống** — nếu không thì đã xoá comment mà không thay bằng gì, và đó là phương án tệ nhất.

Khu này là chỗ đó. Nó chỉ có giá trị nếu được viết đều: **một sự cố không được ghi lại là một comment đã bị xoá không thay thế.**

## 2. Cái gì thuộc khu này, cái gì không

| Thuộc `audit/` | Thuộc chỗ khác |
| --- | --- |
| Sự cố đã xảy ra trên môi trường thật | Rủi ro dự đoán, chưa xảy ra → viết ở [`../RULES.md`](../RULES.md) hoặc `quy-uoc/` |
| Kết quả rà soát toàn cục có phát hiện cụ thể | Ghi chú review của một PR đơn lẻ |
| Một cổng phát hiện đang không hoạt động | Đề xuất thêm cổng mới → [`../RULES.md`](../RULES.md) |
| Một quyết định cũ đã gây hậu quả đo được | Lý do chọn một quyết định → [`../adr/`](../adr/) |

**Phép thử:** nếu không viết được phần *"cổng nào lẽ ra phải bắt"*, hãy hỏi lại xem đây có phải sự cố không. Nhiều thứ tưởng là sự cố thật ra là một quyết định đã cân nhắc và chấp nhận rủi ro — thứ đó thuộc `adr/`.

## 3. Khuôn bắt buộc — bốn phần, không thiếu phần nào

| # | Phần | Phải trả lời | Bẫy |
| --- | --- | --- | --- |
| 1 | **Hiện tượng** | Người dùng hoặc người vận hành **nhìn thấy gì**? Ở đâu, lúc nào? | Viết ngay nguyên nhân vào đây, làm mất bước quan sát |
| 2 | **Nguyên nhân gốc** | Cơ chế nào dẫn tới hiện tượng đó? Vì sao nó không bị phát hiện sớm hơn? | Dừng ở nguyên nhân gần nhất ("thiếu kiểm null") thay vì đi tới cơ chế |
| 3 | **Cách vá** | Đã làm gì để dừng chảy máu? Vá triệu chứng hay vá gốc? | Ghi cách vá như thể đã xong chuyện, trong khi mới chỉ chặn triệu chứng |
| 4 | **Cổng nào lẽ ra phải bắt** | Cơ chế nào — có sẵn hoặc phải dựng — sẽ chặn lớp lỗi này lần sau? | Viết "cần cẩn thận hơn". Đó không phải cổng, đó là niềm tin |

**Phần 4 là phần có giá trị nhất.** Ba phần đầu kể lại quá khứ; phần 4 đổi tương lai. Mỗi mục audit nên kết thúc bằng một dòng cụ thể đủ để thêm vào bảng luật ở [`../RULES.md`](../RULES.md), kèm cột *"Ép bằng gì"*.

Nếu phần 4 kết luận rằng **chưa có cổng nào khả thi**, hãy viết đúng như vậy và đưa nó vào danh sách nợ ở §10 của file đó. Nợ nhìn thấy được thì còn trả được.

Nên có thêm một mục cuối: **bài học tổng quát** — phát biểu lớp lỗi ở mức trừu tượng hơn một bậc, để nó dùng được cho tình huống khác chứ không chỉ cho đúng file đã hỏng.

## 4. Đặt tên file

```text
YYYY-MM-DD-<slug>.md
```

| Quy tắc | Chi tiết |
| --- | --- |
| Ngày | Ngày **sự cố được phát hiện**, không phải ngày viết bản ghi |
| Slug | Chữ thường, không dấu, nối bằng gạch ngang; mô tả **cơ chế hỏng**, không mô tả triệu chứng |
| Đổi tên | Không — code và tài liệu đã trỏ tới |

Slug tốt: `2026-09-05-reflection-envelope` — nói cơ chế. Slug tệ: `2026-09-05-loi-500` — nói triệu chứng, mà triệu chứng thì trùng nhau giữa các sự cố hoàn toàn khác nhau.

```bash
ls docs/audit/2*.md                       # các mục hiện có, theo thứ tự thời gian
grep -l 'Cổng nào lẽ ra phải bắt' docs/audit/*.md
```

## 5. Luật: code chỉ để lại MỘT DÒNG

Đây là mặt còn lại của [`../adr/0010-comment-toi-thieu.md`](../adr/0010-comment-toi-thieu.md).

Ở nơi code có một dòng phòng thủ tồn tại vì một sự cố, code được để lại **đúng một dòng trỏ tới file audit**, và:

- **không** kể lại hiện tượng,
- **không** chép nguyên nhân,
- **không** giải thích cách vá.

Vì sao chặt thế: hai bản của cùng một câu chuyện sẽ lệch nhau, và bản nằm trong code là bản không ai sửa. Một dòng trỏ đường thì không thể lệch — nó hoặc đúng, hoặc chết, và cổng tài liệu phát hiện được cái chết đó.

Kèm theo: dòng trỏ đường **không được trỏ vào file mang nhãn lịch sử** (luật D10 ở [`../RULES.md`](../RULES.md)). Ở dự án tiền nhiệm từng có một chú thích trỏ đúng vào một tài liệu đã chết — đường dẫn tồn tại nên cổng xanh, mà nội dung thì đã hết hiệu lực từ lâu.

## 6. Văn hoá không đổ lỗi

**Viết về hệ thống và cơ chế, không viết về người.**

| Đừng viết | Viết |
| --- | --- |
| "X quên chạy cổng" | "Cổng chạy tay, không có cơ chế nào ghi lại rằng nó đã chạy" |
| "Ai đó xoá nhầm script" | "Không có gì phát hiện được khi một script cổng biến mất khỏi đĩa" |
| "Thiếu cẩn thận khi đổi chữ ký" | "Ràng buộc giữa hai file được giữ bằng phản chiếu, nên compiler không nhắc khi một bên đổi" |

Lý do không phải lịch sự. Lý do là **hiệu quả**: một bản ghi chỉ ra người sẽ dừng lại ở kết luận "lần sau cẩn thận hơn" — và đó là kết luận không dùng được, vì nó không đổi gì trong hệ thống. Một bản ghi chỉ ra cơ chế sẽ dẫn thẳng tới một cổng cụ thể.

Hệ quả kèm theo: **không nêu tên người trong file audit.** Kể cả khen. Nếu cần dẫn nguồn phát hiện, viết vai trò chứ không viết tên.

Và: **kể lại bằng văn xuôi.** Không trích dẫn dạng đường-dẫn-kèm-số-dòng trỏ vào repo khác, vì số dòng ở repo khác không ai kiểm được và nó sẽ trôi ngay lần sửa kế tiếp.

## 7. Quy trình ghi một mục audit

| Bước | Việc | Ai | Khi nào |
| --- | --- | --- | --- |
| 1 | Dừng chảy máu | người xử lý sự cố | ngay |
| 2 | Ghi nháp phần **Hiện tượng** khi ký ức còn tươi | người phát hiện | trong ngày |
| 3 | Điều tra tới **nguyên nhân gốc**, không dừng ở nguyên nhân gần nhất | người xử lý | trong vài ngày |
| 4 | Viết phần **Cổng nào lẽ ra phải bắt**, và đề xuất luật kèm cột "Ép bằng gì" | người xử lý + người review | trước khi coi sự cố là đóng |
| 5 | Thêm luật vào [`../RULES.md`](../RULES.md), hoặc đưa vào danh sách nợ nếu chưa có cổng khả thi | người review | cùng lúc bước 4 |
| 6 | Đặt **một dòng** trỏ tới file audit ở chỗ code liên quan, nếu có | người sửa code | khi sửa code |

Hai điều dễ bị bỏ:

- **Bước 4 không được hoãn.** Hoãn tới khi rảnh nghĩa là không bao giờ, và khi đó bản ghi tụt xuống thành chuyện kể. Một mục audit thiếu phần 4 là một mục chưa xong.
- **Sự cố "đã tự hết" vẫn phải ghi.** Một lỗi biến mất sau khi khởi động lại là một lỗi chưa hiểu, không phải một lỗi đã sửa.

## 8. Phần 4 sinh ra cổng như thế nào — hai ví dụ có thật

Đây là chỗ chứng minh khu này không chỉ là nơi kể chuyện. Hai mục dưới đây đã đi hết đường từ hiện tượng tới một dòng trong bảng luật — mục lục đầy đủ ở §9:

| Sự cố | Nguyên nhân gốc ở mức cơ chế | Luật sinh ra |
| --- | --- | --- |
| Reflection ở đường xử lý lỗi | Ràng buộc giữa hai file được giữ bằng phản chiếu thay vì bằng kiểu, nên compiler không nhắc khi một bên đổi | **R5** — cấm phản chiếu ở đường ánh xạ `Result` → HTTP, ép bằng test kiến trúc |
| Cổng không tồn tại | Không có gì chứng minh một cổng đang chạy; và chuỗi lệnh không dừng khi một lệnh gãy | **T1** — mọi detector phải có test kiểm chính nó; cộng quyết định có CI |

Chú ý cột giữa: **nguyên nhân gốc phải được phát biểu ở mức cơ chế, không ở mức sự việc.** "Phép tra trả null" là sự việc — nó chỉ sinh ra một bản vá. "Ràng buộc giữ bằng phản chiếu nên compiler không kiểm được" là cơ chế — nó sinh ra một luật áp cho mọi chỗ khác có cùng hình dạng.

Phép thử cho phần 4: **luật đề xuất có bắt được sự cố này nếu quay ngược thời gian không, và nó có bắt được một sự cố khác cùng lớp không?** Chỉ trả lời được câu đầu thì đó là bản vá, chưa phải cổng.

## 9. Mục lục

| Mục | Cơ chế hỏng | Cổng nó sinh ra |
| --- | --- | --- |
| [2026-09-08 — audit PlatformManager](2026-09-08-audit-platformmanager.md) | Rà soát repo tiền nhiệm: giữ gì, bỏ gì. **`kind: tham-chieu`** — ảnh chụp một dự án KHÁC, mọi đường dẫn trong đó thuộc repo đó | Nguồn của phần lớn luật ở [`../RULES.md`](../RULES.md) và các ADR đầu tiên |
| [2026-09-09 — cổng no-op và hai mô hình nguồn](2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md) | Sáu mục cổng in "không có vi phạm" sau khi xét tập rỗng; hook ghi dấu bỏ sót mọi sửa file không đi qua công cụ sửa file; hai mô hình nguồn FE↔API cùng mang `kind: luat` và nói ngược nhau | Luật T6 — bộ dò phải khẳng định tập đầu vào khác rỗng; T5 và T7; bốn dòng nợ mới ở [`../RULES.md`](../RULES.md) §10 |
| [2026-09-10 — ghi chú lịch sử trong file nội dung](2026-09-10-ghi-chu-lich-su-trong-file-noi-dung.md) | Mỗi lượt vá để lại câu kể bản sai ngay tại chỗ vá; không luật nào cấm và không cổng nào dò | Luật D24 — `check-docs.sh` §17 dò dấu hiệu kể lịch sử |
| [2026-09-05 — reflection ở đường xử lý lỗi](2026-09-05-reflection-envelope.md) | Cầu nối exception → envelope dựng bằng phản chiếu; phép tra trả null khi chữ ký đổi, biến mọi lỗi nghiệp vụ thành lỗi con trỏ rỗng | Luật R5 — cấm phản chiếu ở đường ánh xạ `Result` → HTTP |
| [2026-08-23 — cổng không tồn tại](2026-08-23-cong-khong-ton-tai.md) | Script cổng được tài liệu hướng dẫn chạy không có trên đĩa; lệnh gãy mà chuỗi vẫn chạy tiếp | Luật T1 — mọi detector phải có test kiểm chính nó; và [`../adr/0011-ci-github-actions.md`](../adr/0011-ci-github-actions.md) |

Rà bằng lệnh thay vì đếm tay:

```bash
ls docs/audit/2*.md | wc -l
grep -rL '^kind:' docs/audit --include='*.md'
```

## 10. Vì sao file ở khu này mang `kind: luat`, không mang `kind: lich-su`

Câu hỏi hợp lý: một postmortem kể chuyện đã qua, sao không phải là lịch sử?

Vì hai lý do, và cả hai đều thực dụng:

1. **Phần 4 vẫn còn hiệu lực.** Một mục audit không chỉ kể chuyện — nó khai một lớp lỗi và cơ chế chặn lớp lỗi đó. Chừng nào cơ chế ấy còn phải được giữ, nội dung file còn là thứ code phải tuân.
2. **Nhãn lịch sử là nhãn cấm trích dẫn.** Luật D10 ở [`../RULES.md`](../RULES.md) cấm mọi trích dẫn trỏ vào file mang nhãn đó. Mà [`../adr/0010-comment-toi-thieu.md`](../adr/0010-comment-toi-thieu.md) lại yêu cầu code để lại một dòng trỏ **vào đây**. Gắn nhãn lịch sử cho khu này là tự mâu thuẫn: dòng trỏ đường sẽ bị chính cổng của repo bắt lỗi.

`kind: lich-su` dành cho **tài liệu đã chết** — kế hoạch bị thay thế, thiết kế bị bỏ. Một bài học vẫn đang được ép bằng cổng thì chưa chết.

## 11. Liên quan

- [`../adr/0010-comment-toi-thieu.md`](../adr/0010-comment-toi-thieu.md) — quyết định sinh ra khu này
- [`../RULES.md`](../RULES.md) — nơi phần 4 của mỗi mục audit kết thúc thành một luật có cổng
- [`../quy-uoc/tieu-chi-review.md`](../quy-uoc/tieu-chi-review.md) — cái gì là finding, cái gì không
