---
name: "review-doc"
description: "Review một thay đổi tài liệu đang nằm trong working tree: đối chiếu diff với luật repo, kiểm nhãn trạng thái, kiểm việc chép nội dung sang .claude/, kiểm số đếm được chép cứng, rồi chạy cổng. Chỉ báo cáo, không sửa."
argument-hint: "[phạm vi] - vd 'docs/quy-uoc' hoặc để trống để review toàn bộ thay đổi chưa commit"
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

Cổng `check-docs.sh` là lưới **chặn hồi quy**, không phải chứng nhận chất lượng. Nó không đọc hiểu nội dung. Skill này là lượt đọc hiểu chạy trên đúng phần **vừa thay đổi** — nhỏ hơn, nên đọc kỹ được.

Khác với skill `doc-sync` (soát toàn kho, định kỳ), skill này chỉ soát **diff**: cái gì vừa được thêm hoặc sửa.

🛑 **Chỉ đọc.** Không sửa file nào, kể cả finding nhìn có vẻ hiển nhiên. Người review không sửa thứ mình review — thấy lỗi thì báo, không vá.

## Các bước thực hiện

### 1. Lấy diff — chỉ đọc, không đụng git ghi

Ba lệnh đọc được phép chạy tự do theo [`../../CLAUDE.md`](../../CLAUDE.md) §1:

```bash
git status
git diff -- docs .claude spec
git diff --stat -- docs .claude spec
```

`$ARGUMENTS` có nêu phạm vi → giới hạn đường dẫn theo đó. Rỗng → lấy toàn bộ thay đổi chưa commit.

Diff rỗng → hỏi người dùng: muốn review commit gần nhất thay vì working tree không? Nếu có, dùng lệnh đọc:

```bash
git show --stat
git show -- docs .claude spec
```

🛑 Mọi lệnh git **ghi** đều cấm. Cần một lệnh ghi để đi tiếp thì **nói rõ cần chạy lệnh gì**, người dùng tự chạy.

### 2. Đọc tiêu chí chấm

Mở [`../../../docs/quy-uoc/tieu-chi-review.md`](../../../docs/quy-uoc/tieu-chi-review.md) — file này quyết định **cái gì là finding, cái gì không**. Chấm theo nó, không chấm theo cảm giác.

Đối chiếu luật với [`../../../docs/RULES.md`](../../../docs/RULES.md): mỗi finding phải nêu được **luật số mấy** bị vi phạm. Không nêu được thì đó là góp ý, không phải finding — và phải ghi vào mục riêng, đừng trộn lẫn.

### 3. Kiểm nhãn trạng thái

Ba nhãn hợp lệ theo [`../../CLAUDE.md`](../../CLAUDE.md) §4. Repo chưa có `src/` — xác nhận bằng lệnh:

```bash
test -d src && echo "CO src" || echo "CHUA CO src"
```

Chưa có `src/` → mô tả kiến trúc mới thêm vào phải mang nhãn **đích đến, chưa thi công**. Bốn dạng vi phạm cần soi trong diff:

| Dạng | Vì sao là lỗi |
| --- | --- |
| Tuyên bố hoàn thành cho thứ chưa có code | Nhãn "đã xong" được thiết kế để **không ai kiểm lại** |
| Nhãn có ngày nhưng nội dung sai | Cổng kiểm ngày, không kiểm nội dung — đây là lỗ hổng của cổng |
| Nhãn chép vào mục lục thay vì đặt ở đầu chính file | Trạng thái ở chỗ thứ hai không bao giờ được sửa cùng lúc |
| Đóng một việc tồn đọng bằng cách sửa mô tả cho khớp mong muốn | Dạng sai đắt nhất: không lỗi biên dịch, không test nào bắt |

Kiểm thêm khoá `verified` của file vừa sửa: đóng dấu ngày cho một file chưa ai mở source ra so là đúng khuôn sai mà §4 cấm. Giai đoạn 1 thì `chua-doi-chieu` mới là giá trị trung thực.

### 4. Kiểm chép nội dung sang `.claude/`

Diff có chạm `.claude/` → đây là phần cần soi kỹ nhất, vì cổng chỉ bắt được một phần của nó.

**Phép thử một câu** ([`../../CLAUDE.md`](../../CLAUDE.md) §2): câu vừa thêm vào `.claude/` có thể trở thành **sai khi code thay đổi** không? Có → nó thuộc `docs/`.

| Câu vừa thêm | Kết luận |
| --- | --- |
| "Xong việc chạm Core thì gọi `core-reviewer`" | Quy trình — thuộc `.claude/` |
| "Sửa envelope thì đọc file quy ước controller trước" | Trỏ đường — thuộc `.claude/` |
| Một quy tắc về kiểu trả về của handler | Tri thức — **finding**, phải nằm ở `docs/` |
| Một con số về số lượng project | Tri thức — **finding** |

Ba dạng vi phạm cụ thể:

- **Code block ngôn ngữ lập trình trong `.claude/`.** Cổng bắt được dạng này, nhưng vẫn liệt kê trong báo cáo nếu có.
- **Văn xuôi chép lại nội dung `docs/`.** Cổng **không** bắt được. Soi mọi đoạn mới dài hơn một dòng trong `.claude/`: nó đang mô tả *quy trình* hay đang giải thích *nội dung* của một file `docs/`?
- **Dòng trỏ đường kèm tóm tắt.** Dạng chuẩn là **một dòng, không tóm tắt kèm** ([`../../CLAUDE.md`](../../CLAUDE.md) §3). Một dòng trỏ đường kèm hai câu giải thích chính là bản sao thứ hai đang được sinh ra.

Cũng kiểm chiều cập nhật: bảng ở §3 của file luật repo nói rõ ô nào cho phép `.claude/` đổi. **Không ô nào cho phép chép nội dung.** Nếu diff sửa `docs/` **và** thêm nội dung vào `.claude/` cùng lúc, hỏi người dùng vì sao.

### 5. Kiểm số đếm được bằng lệnh

Luật ở [`../../CLAUDE.md`](../../CLAUDE.md) §6: không chép vào tài liệu thứ đếm được bằng lệnh. Bảng liệt kê tay sẽ luôn mục ruỗng.

Soi trong diff các dạng: số lượng file, số lượng test, số lượng cổng, số lượng component, danh sách "còn N chỗ" hay danh sách file vi phạm liệt kê tay.

```bash
git diff -- docs .claude | grep -nE '^\+.*[0-9]+ (file|project|test|cổng|mục|component|chỗ)'
```

Thay đúng nếu bắt được: một **lệnh** cộng **tiêu chí PASS**, không phải một con số.

Ngoại lệ hợp lệ: con số là một **quyết định đích đến** (ví dụ layout Core được chốt gồm bao nhiêu project) chứ không phải một phép đếm hiện trạng. Phân biệt bằng câu hỏi: con số này sẽ sai khi ai đó thêm file, hay chỉ sai khi có người đổi quyết định?

### 6. Chạy cổng

```bash
bash .claude/check-docs.sh
```

Ghi kết quả vào báo cáo. **PASS không có nghĩa là tài liệu đúng** — ba loại lỗi cổng không bao giờ bắt được nêu ở [`../../CLAUDE.md`](../../CLAUDE.md) §8. Nói lại điều đó trong mọi báo cáo.

Cổng FAIL → liệt kê từng mục fail; đó là finding mức chặn, không cần bàn thêm.

### 7. Viết báo cáo

Theo khuôn ở mục Đầu ra. Mỗi finding neo bằng **đường dẫn kèm dòng**. Không neo được thì không phải finding — nó là cảm giác.

## Dừng lại và hỏi khi

1. **Diff rỗng.** Hỏi người dùng muốn review gì — working tree hay commit gần nhất.
2. **Diff chạm cả `docs/` lẫn `.claude/` với nội dung tương tự nhau ở hai bên.** Đây là dấu hiệu bản sao đang sinh ra — hỏi, đừng tự kết luận bên nào là bản gốc.
3. **Một finding đòi phải chọn file chủ giữa hai file.** Đó là quyết định của người dùng, chuyển sang skill `doc-sync`.
4. **Người dùng yêu cầu sửa các finding ngay trong lượt này.** Nói rõ lượt review không sửa; việc sửa thuộc `tech-writer` hoặc chính người dùng.
5. **Cần lệnh git ghi** — xem [`../../CLAUDE.md`](../../CLAUDE.md) §1.

## Đầu ra

```markdown
## Phạm vi lượt này
<đường dẫn đã soát>
Nguồn diff: working tree / commit gần nhất
File đã đọc: <danh sách>
Cổng check-docs.sh: PASS / FAIL

## Kết quả theo nhóm kiểm

| # | Nhóm | Kết luận | Bằng chứng |
|---|------|----------|------------|
| 1 | Nhãn trạng thái | PASS / PARTIAL / FAIL | <đường dẫn:dòng> |
| 2 | Ranh giới .claude ↔ docs | PASS / PARTIAL / FAIL | <đường dẫn:dòng> |
| 3 | Số đếm được chép cứng | PASS / PARTIAL / FAIL | <đường dẫn:dòng> |
| 4 | Cổng máy | PASS / FAIL | <mục fail> |

## Finding

### F1 — <tiêu đề ngắn> — <Nghiêm trọng / Trung bình / Nhẹ>
**Vi phạm:** <luật số mấy, ở file luật nào>
**Ở đâu:** <đường dẫn:dòng>
**Vì sao là lỗi:** <kịch bản hỏng cụ thể>
**Đề xuất:** <hướng sửa — KHÔNG tự sửa>

## Góp ý không phải finding
- <không nêu được luật bị vi phạm, nhưng đáng nói>

## Đã cân nhắc và loại
- <thứ trông có vẻ sai nhưng có lý do chính đáng, và lý do đó là gì>

## Lỗ mù của lượt này
- <thứ không soát được và vì sao>
```

## Trước khi coi là xong

1. Diff đã lấy bằng lệnh git **chỉ đọc**; không chạy lệnh git ghi nào.
2. Mỗi finding nêu được luật bị vi phạm; không nêu được thì đã chuyển sang mục góp ý.
3. Đã chạy `bash .claude/check-docs.sh` và ghi kết quả.
4. Báo cáo có mục **Lỗ mù**.
5. Không sửa bất kỳ file nào.
