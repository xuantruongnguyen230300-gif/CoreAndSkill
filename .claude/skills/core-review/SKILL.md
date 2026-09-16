---
name: "core-review"
description: "Gọi agent core-reviewer cho một lượt audit độc lập phần Core — xác định phạm vi BE hoặc FE (không bao giờ cả hai), giao đúng phạm vi mà không kèm tóm tắt, rồi chuyển báo cáo cho agent phù hợp nếu cần sửa."
argument-hint: "<BE|FE> [phạm vi hẹp] - vd 'BE - envelope và ánh xạ lỗi sang HTTP'"
metadata:
  author: "core-team"
  source: "custom"
user-invocable: true
disable-model-invocation: false
---

## User Input

```text
$ARGUMENTS
```

Bạn **BẮT BUỘC** phải xem xét user input trước khi tiếp tục (nếu không rỗng).

## Mục tiêu

Wrapper mỏng để gọi `core-reviewer` đúng cách. Vai trò đó chỉ có giá trị khi ba ràng buộc của nó không bị nới — và cả ba đều bị nới rất dễ, một cách vô tình, ngay ở khâu giao việc.

| Ràng buộc | Nới ra thì mất gì |
| --- | --- |
| Người kiểm **không sửa** thứ mình kiểm | Người vừa vá xong sẽ chấm bản vá của chính mình là đạt |
| Người kiểm **tự đọc**, không nhận tóm tắt | Nhận tóm tắt thì nó chỉ xác nhận lại thiên kiến của agent viết code — đúng thứ vai trò này sinh ra để chống |
| Một lượt = **một phạm vi** | Corpus hai phía đủ lớn để giết lượt review trước khi nó kết luận được gì |

Ràng buộc thứ ba đã trả giá thật: ở dự án tiền nhiệm, corpus bắt buộc từng lên tới gần một megabyte, ba lượt review liên tiếp chết giữa chừng, và một lượt còn để lại lỗi cố ý trong code mà không phát hiện ra.

Chi tiết vai trò, bảng định tuyến và khuôn báo cáo nằm ở [`../../agents/core-reviewer.md`](../../agents/core-reviewer.md) — skill này không chép lại.

## Các bước thực hiện

### 1. Xác định phạm vi — BE **hoặc** FE, không bao giờ cả hai

`$ARGUMENTS` phải nêu được một phía. Không nêu → hỏi người dùng, đừng tự chọn.

Người dùng yêu cầu review **cả hai** → **không gộp**. Nói rõ vì sao, rồi đề nghị chạy **hai lượt riêng**, mỗi lượt một `Agent` riêng. Hai lượt tuần tự thu được nhiều hơn một lượt gộp bị cụt giữa chừng.

Thu hẹp thêm nếu có thể: một chủ đề cụ thể (envelope, validator, guard, token) tốt hơn "toàn bộ Core". Phạm vi càng hẹp, lượt review càng đọc kỹ được.

### 2. Kiểm giai đoạn repo

```bash
test -d src && echo "CO src" || echo "CHUA CO src"
```

**Chưa có `src/`** → không có code để review. Nói thẳng điều đó, rồi đưa hai lựa chọn:

- Vẫn chạy `core-reviewer` ở **chế độ tài liệu** — phạm vi khi đó là chính `docs/`: mâu thuẫn giữa các file, quy ước nói ngược nhau, luật khai trong bảng luật mà không file nào mô tả cách thi công. Agent đã khai sẵn chế độ này.
- Dùng skill `arch-check` (soát hai chiều lệch giữa luật và quy ước) hoặc `doc-sync` (soát lệch trong toàn kho tài liệu) — hai skill đó chuyên cho việc này hơn.

Đừng để lượt chạy tới lúc agent mở thư mục không tồn tại rồi thất bại khó hiểu.

### 3. Gọi agent

Gọi `core-reviewer` qua `Agent`. Prompt chứa **đúng ba thứ**:

1. Phía nào — BE hoặc FE.
2. Phạm vi hẹp — chủ đề hoặc vùng cần soát.
3. Giai đoạn repo — có `src/` hay chưa, tức chế độ code hay chế độ tài liệu.

🛑 **KHÔNG gửi tóm tắt việc vừa làm.** Không "tôi vừa sửa X theo cách Y", không danh sách file đã đổi, không lý do thiết kế, không kết luận sơ bộ. Mọi câu như thế đều là mồi neo: agent sẽ đi kiểm **câu chuyện được kể** thay vì đi đọc **thứ có thật**.

Cũng không gửi kèm nội dung file. Agent tự resolve đường dẫn và tự chọn file theo bảng định tuyến của nó — đó là cơ chế giữ corpus ở mức không giết lượt review.

### 4. Không sửa gì trong lúc review chạy

Từ lúc gọi tới lúc nhận báo cáo: **không sửa code, không sửa tài liệu trong phạm vi đang review**. File đổi giữa chừng làm báo cáo neo vào một trạng thái không còn tồn tại, và finding sẽ bị bác oan vì "chỗ đó sửa rồi".

Có việc gấp phải sửa → dừng lượt review, sửa, rồi chạy lại từ đầu.

### 5. Nhận báo cáo và chuyển tiếp

Báo cáo có mục Finding, mục "không phải finding", và mục **Lỗ mù**. Đọc mục Lỗ mù trước — nó nói lượt này **không** phủ cái gì, và một báo cáo không có mục đó sẽ được đọc như thể nó phủ hết.

Chuyển finding cho đúng người sửa:

| Finding thuộc | Chuyển cho |
| --- | --- |
| Code backend | `backend-expert` |
| Code frontend | `frontend-expert` |
| Một quy ước trong `docs/` sai hoặc thiếu | `tech-writer`, hoặc `architect` nếu là quyết định kiến trúc |
| Một luật thiếu cổng | Người dùng — thêm dòng vào bảng luật hoặc vào danh sách nợ |

Chuyển bằng **đường dẫn tới báo cáo và mã finding**, không paste lại toàn văn.

🛑 `core-reviewer` **không sửa**. Người dùng yêu cầu nó sửa → nói rõ việc sửa thuộc hai agent thi công, và vì sao ranh giới đó không được nới.

## Dừng lại và hỏi khi

1. **`$ARGUMENTS` không nêu được BE hay FE.** Hỏi, đừng tự chọn phía.
2. **Người dùng yêu cầu một lượt gộp cả BE lẫn FE.** Không gộp; đề nghị hai lượt riêng và nói rõ lý do.
3. **Chưa có `src/`.** Hỏi người dùng chọn chế độ tài liệu hay chuyển sang skill khác.
4. **Có thay đổi chưa lưu trong phạm vi sắp review.** Hỏi trước — review một working tree đang dở dang cho ra finding về thứ người dùng đã biết là chưa xong.
5. **Finding đòi một quyết định kiến trúc mới.** Chuyển cho `architect`, đừng để agent thi công tự quyết.
6. **Cần lệnh git ghi** — xem [`../../CLAUDE.md`](../../CLAUDE.md) §1.

## Đầu ra

- Báo cáo của `core-reviewer` theo đúng khuôn khai trong file agent của nó — skill này không đổi khuôn đó.
- Một dòng tổng hợp trong hội thoại: phạm vi lượt này, chế độ (code hay tài liệu), số finding theo mức, và finding nào đã chuyển cho ai.

## Trước khi coi là xong

1. Lượt review có **đúng một** phạm vi.
2. Prompt gửi cho agent **không** chứa tóm tắt việc vừa làm và không chứa nội dung file.
3. Không có file nào bị sửa trong lúc review chạy.
4. Báo cáo có mục **Lỗ mù**; không có thì hỏi lại agent.
5. Mỗi finding đã có người nhận, hoặc đã được ghi là chờ người dùng quyết.
