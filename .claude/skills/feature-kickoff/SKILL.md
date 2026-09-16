---
name: "feature-kickoff"
description: "Điểm vào duy nhất khi bắt đầu một feature: phân loại Core vs Module, gate spec nghiệp vụ, gọi design-expert nếu cần màn hình mới, spawn backend-expert và frontend-expert song song, rồi nối test-engineer, core-reviewer và tech-writer. Dùng thay vì tự nhớ trình tự gọi từng agent."
argument-hint: "<tên feature> [mô tả ngắn] - vd 'DonNghiPhep - nhân viên xin nghỉ, quản lý duyệt'"
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

Một feature đi qua nhiều agent theo một thứ tự nhất định, và **thứ tự đó là phần dễ quên nhất**. Bỏ một bước không làm việc hỏng ngay — nó làm việc hỏng im lặng: thiếu spec thì hai agent suy diễn hai nghiệp vụ khác nhau; bỏ `core-reviewer` thì một vi phạm ranh giới Core sống thêm vài tháng trước khi ai đó phát hiện.

Skill này giữ thứ tự đó và tự nối các agent lại. Người dùng gọi một lệnh, không phải gọi từng agent.

## ⚠️ Giai đoạn 1 — kiểm TRƯỚC KHI làm gì khác

Repo có thể đang ở giai đoạn chỉ có `docs/` và `.claude/`, **chưa có `src/`**. Xác nhận bằng lệnh, đừng tin trí nhớ:

```bash
test -d src && echo "CO src" || echo "CHUA CO src"
```

**Chưa có `src/`** → bước 5 trở đi **không chạy được**, vì `backend-expert` và `frontend-expert` không có gì để sửa. Khi đó:

1. **Nói thẳng với người dùng ngay từ đầu**, đừng để chuỗi chạy tới bước 5 rồi thất bại khó hiểu.
2. Vẫn chạy được bước 1–4: phân loại Core/Module, viết spec nghiệp vụ, dựng screen spec. Đó là việc có giá trị thật ở giai đoạn này.
3. Hỏi người dùng muốn dừng sau bước 4, hay muốn khởi tạo `src/` trước — **không tự khởi tạo solution khi không được yêu cầu**.

Trạng thái repo đọc ở [`../../../docs/README.md`](../../../docs/README.md), mục trạng thái đầu file.

## Các bước thực hiện

### 1. Resolve feature

`$ARGUMENTS` rỗng → hỏi người dùng tên feature và mô tả ngắn. **Không tự đoán** feature nào đang cần làm.

Tên feature dùng thống nhất cho cả thư mục `spec/<feature>/` lẫn tên module, viết không dấu.

### 2. Phân loại Core vs Module — quyết định ranh giới kiến trúc

Tiêu chí ở [`../../../docs/kien-truc-core-module.md`](../../../docs/kien-truc-core-module.md) §4. Mở đúng mục đó và đối chiếu, **đừng suy ra từ tên feature**.

| Kết luận | Đi tiếp thế nào |
| --- | --- |
| **Module** — nghiệp vụ riêng của một domain | Bước 3 áp dụng: bắt buộc có spec |
| **Core** — có ý nghĩa với **mọi** sản phẩm dựng trên nền tảng | Bỏ qua bước 3. **Luôn** chuyển cho `architect` trước bước 5 — kể cả khi trông không giống thay đổi kiến trúc; `architect` là người nói nó có phải hay không |
| **Không chắc** | 🛑 **Dừng lại, hỏi người dùng** |

Vì sao không được đoán: một thứ bị đưa nhầm vào Core sẽ được mọi module kế thừa, và gỡ ra sau đó tốn tuần chứ không tốn giờ. Ngưỡng chống Core phình to nằm ở mục 4.2 của cùng file — **mở ra đọc, đừng trả lời từ trí nhớ**.

### 3. Gate spec — chỉ áp cho feature Module

```bash
ls spec/<feature>/ 2>/dev/null
```

**Không có `spec/<feature>/business-rules.md`** → hai lựa chọn, hỏi người dùng chọn:

- Chạy skill `ba-story` (hoặc gọi thẳng `ba-analyst`) để ghi nghiệp vụ thành file trước.
- Người dùng tự viết file đó rồi quay lại.

🛑 **Không tự bịa spec để có cái mà chạy tiếp.** Đây là cách hỏng thường gặp nhất của một chuỗi tự động: agent thiếu dữ liệu, tự điền một phương án hợp lý, và không ai biết nghiệp vụ đang chạy là do người hay do máy nghĩ ra.

**Có spec** → đọc để hiểu, rồi mang theo **đường dẫn** khi giao việc ở bước 5. Không paste nguyên văn — paste là chép nội dung, và bản chép sẽ lệch với bản gốc ngay lần sửa đầu tiên.

Ghi lại các **câu hỏi còn mở** trong spec. Câu nào đang chặn thì nói rõ với hai agent ở bước 5.

### 4. Cần màn hình mới không

Nguồn giao diện **duy nhất** là khu Design ([`../../CLAUDE.md`](../../CLAUDE.md) §7). Không dựng prototype song song, không giữ nguồn UI thứ hai.

| Tình huống | Làm gì |
| --- | --- |
| Cần UI mới, chưa có screen spec | Gọi `design-expert` qua `Agent` trước khi sang bước 5 |
| Đã có screen spec khớp yêu cầu | Bỏ qua, mang đường dẫn screen spec sang bước 5 |
| Feature không có UI — thuần API hoặc background job | Bỏ qua |
| Không chắc có cần màn hình mới không | Hỏi người dùng, đừng tự quyết |

### 5. Spawn `backend-expert` + `frontend-expert` song song

🛑 Bước này **cần `src/`**. Chưa có thì dừng ở đây và báo, theo mục cảnh báo giai đoạn 1 ở trên.

Gọi cả hai như teammate nền trong **cùng một lượt**, không tuần tự — hai phía cần chạy song song để phía FE không phải chờ BE xong mới bắt đầu.

Prompt cho mỗi agent gồm đúng bốn thứ:

1. Tên feature và mô tả ngắn.
2. Kết luận phân loại ở bước 2 — Core hay Module.
3. **Đường dẫn** `spec/<feature>/business-rules.md` (nếu bước 3 áp dụng) và đường dẫn screen spec (nếu bước 4 áp dụng). Chỉ đường dẫn.
4. Nhắc rằng phía kia cũng đang chạy song song, và hai agent tự trao đổi hợp đồng API qua `SendMessage` theo cơ chế đã khai trong file agent của chúng — skill này không làm trung gian.

Chỉ một phía có việc thật → chỉ spawn đúng một agent. Không ép agent còn lại chạy để cho đủ bộ; một lượt agent không có việc vẫn tiêu context và vẫn có thể sinh ra thay đổi ngoài ý muốn.

### 6. Gọi `test-engineer` tìm ca biên

Người viết code không nhìn thấy được ca mình đã bỏ sót. Gọi `test-engineer` qua `Agent` khi feature chạm Core hoặc chạm luồng bảo mật, và nói chung là khi có Business Rule ở dạng luật cấm hoặc luật chuyển trạng thái.

Mang theo **đường dẫn** spec, không paste. Yêu cầu cụ thể: tìm ca biên mà AC hiện có **không** phủ — trùng, hết hạn, thiếu quyền, dữ liệu rỗng, đồng thời hai người cùng thao tác.

### 7. Việc chạm Core → `core-reviewer`

> 📖 Đường dẫn nào tính là chạm Core: khối `core-paths` trong `docs/kien-truc-core-module.md`

`backend-expert`, `frontend-expert` và `test-engineer` **không tự gọi** `core-reviewer`. Việc của chúng chạm Core thì báo cáo kết thúc bằng dòng `CẦN CORE-REVIEW: BE` và/hoặc `CẦN CORE-REVIEW: FE`.

Skill này đọc dòng cuối báo cáo của từng agent ở bước 5 và 6. Có dòng đó thì gọi `core-reviewer` qua skill `core-review` (hoặc thẳng qua `Agent`), và tuân hai ràng buộc:

- **Chỉ truyền phạm vi.** Không gửi tóm tắt việc vừa làm, không danh sách file đã đổi, không chép lại báo cáo của agent thi công. Người kiểm nhận tóm tắt của người viết thì nó chỉ xác nhận lại thiên kiến của người viết — và đó đúng là thứ vai trò này sinh ra để chống.
- **Một lượt = một phạm vi.** Có cả dòng BE lẫn dòng FE → hai lượt riêng, không bao giờ gộp.

Không sửa code trong lúc `core-reviewer` đang chạy.

### 8. Tài liệu bàn giao → `tech-writer`

Feature sinh ra thay đổi mà người khác cần biết — hợp đồng API mới, quy ước mới, một luật mới cần vào file luật — thì gọi `tech-writer`.

Nhắc lại chiều cập nhật ([`../../CLAUDE.md`](../../CLAUDE.md) §3): **nội dung vào `docs/`**, `.claude/` chỉ đổi khi **đường dẫn** đổi. Một feature mới hầu như không bao giờ là lý do chính đáng để thêm nội dung vào `.claude/`.

### 9. Tổng hợp

Sau khi các agent báo cáo xong, tổng hợp cho người dùng theo khuôn ở mục Đầu ra. Không tự làm lại việc của agent nào, và không tóm tắt sai lệch những gì từng agent đã báo — nếu một agent nói "chưa kiểm được phần X" thì câu đó phải sống sót qua bản tổng hợp.

Ba thứ **bắt buộc** xuất hiện trong bản tổng hợp, vì chúng là thứ dễ rơi mất nhất giữa nhiều lượt agent:

- **Câu hỏi còn mở** trong spec, và câu nào đang chặn.
- **Finding từ `core-reviewer`** chưa có người nhận.
- **Bước nào đã bị bỏ qua và vì sao** — kể cả khi lý do chính đáng. Một bước bị bỏ im lặng sẽ được đọc như một bước đã chạy.

## Dừng lại và hỏi khi

1. **Không chắc feature thuộc Core hay Module** (bước 2). Đây là quyết định ranh giới kiến trúc, không suy đoán được từ tên.
2. **Feature Module thiếu spec** (bước 3). Hỏi người dùng chọn một trong hai lối, đừng tự bịa nghiệp vụ.
3. **Chưa có `src/` mà người dùng muốn đi tới bước 5.** Nói rõ bước đó không chạy được và vì sao; hỏi có muốn khởi tạo `src/` không, đừng tự khởi tạo.
4. **Không chắc feature có cần màn hình mới hay không** (bước 4).
5. **Feature đòi một quyết định kiến trúc mới** — thêm project, đổi ranh giới tầng, thêm phụ thuộc ngoài, đổi cách xử lý lỗi. Chuyển cho `architect`, đừng để hai agent thi công tự quyết.
6. **Spec mâu thuẫn với một quy tắc trong `docs/`.** Quy tắc trong `docs/` thắng. Báo mâu thuẫn, đừng tự chọn một bên.
7. **Cần lệnh git ghi** — xem [`../../CLAUDE.md`](../../CLAUDE.md) §1.

## Đầu ra

Một bản tổng hợp trong hội thoại:

```markdown
## Feature
<tên> — phân loại: Core / Module
Giai đoạn repo: có src/ · chưa có src/

## Bước đã chạy
| # | Bước | Kết quả |
|---|------|---------|
| 2 | Phân loại | Core / Module — theo tiêu chí nào |
| 3 | Gate spec | có / vừa tạo / không áp dụng |
| 4 | Screen spec | có / vừa tạo / không cần |
| 5 | BE + FE | chạy / bỏ qua vì chưa có src/ |
| 6 | test-engineer | ca biên tìm thêm được |
| 7 | core-reviewer | đã chạy BE / đã chạy FE / không báo cáo nào có dòng CẦN CORE-REVIEW |
| 8 | tech-writer | tài liệu cần cập nhật |

## File đã tạo hoặc sửa
- <đường dẫn> — <ai tạo>

## Cần người quyết
- <câu hỏi còn mở trong spec, finding từ core-reviewer, mâu thuẫn chưa giải>
```

## Trước khi coi là xong

1. Đã kiểm sự tồn tại của `src/` **trước** khi chạy bước nào, và đã báo nếu chưa có.
2. Phân loại Core/Module đã dứt khoát, hoặc đã dừng lại hỏi.
3. Feature Module có spec thật — không có chỗ nào nghiệp vụ do agent tự nghĩ ra.
4. Mọi prompt giao cho agent con mang **đường dẫn**, không paste nguyên văn spec.
5. Sửa tài liệu nào thì chạy `bash .claude/check-docs.sh`.
6. Cổng nào của khu vừa sửa mà bị bỏ qua thì **nói ra**, đừng im lặng.
7. Kết luận Core ở bước 2 đã qua `architect` trước bước 5.
8. Mỗi dòng `CẦN CORE-REVIEW` trong báo cáo agent đã có đúng một lượt `core-reviewer` cho phạm vi đó — hoặc đã báo người dùng là lượt đó chưa chạy.
