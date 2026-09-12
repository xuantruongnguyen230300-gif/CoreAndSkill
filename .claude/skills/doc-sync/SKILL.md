---
name: "doc-sync"
description: "Dò lệch trong docs/ — thứ cổng check-docs.sh không bắt được vì cần đọc hiểu: thiếu khoá phân loại, hai file cùng làm chủ một chủ đề, bảng định tuyến trỏ sai, file mồ côi, tuyên bố hiện trạng sai, luật thiếu cột ép. Chỉ báo cáo, không tự sửa."
argument-hint: "[phạm vi] - vd 'docs/quy-uoc' hoặc 'toàn repo' hoặc để trống"
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

Cổng máy chỉ bắt được thứ **máy kiểm được** — đường dẫn có tồn tại không, link có resolve không, tuyên bố có kèm ngày không. Ba loại lỗi nó không bao giờ bắt được đã liệt kê ở [`../../CLAUDE.md`](../../CLAUDE.md) §8.

Skill này lấp đúng khoảng trống đó: sáu phép kiểm **cần đọc hiểu nội dung**, chạy sau khi cổng đã xanh.

🛑 **Skill này KHÔNG sửa file.** Nó đề xuất, người dùng quyết. Hai trong sáu phép kiểm — trùng chủ đề và mồ côi — không có đáp án đúng duy nhất; tự gộp file là cách nhanh nhất để xoá mất một nội dung mà không ai đọc lại.

## Các bước thực hiện

### 0. Chạy cổng máy trước — nếu đỏ thì dừng

```bash
bash .claude/check-docs.sh
```

Cổng đỏ → **dừng, báo người dùng sửa cổng trước**. Skill này soát tầng trên cổng; chạy nó khi tầng dưới còn hỏng chỉ tạo ra một danh sách nhiễu.

### 1. Xác định phạm vi

`$ARGUMENTS` là một đường dẫn thư mục → chỉ soát trong đó. Rỗng hoặc "toàn repo" → soát cả `docs/` và các bảng định tuyến trong `.claude/`.

Liệt kê file bằng lệnh, không nhớ bằng đầu:

```bash
find docs -name '*.md' | sort
find .claude -name '*.md' | sort
```

### 2. Kiểm A — thiếu frontmatter hoặc thiếu một trong ba khoá

Ba khoá bắt buộc theo [`../../CLAUDE.md`](../../CLAUDE.md) §9: `kind`, `scope`, `verified`.

```bash
grep -rL '^kind:'     docs --include='*.md'
grep -rL '^scope:'    docs --include='*.md'
grep -rL '^verified:' docs --include='*.md'
```

Kiểm thêm phần máy khó bắt: giá trị `khong-ap-dung` của khoá `verified` chỉ hợp lệ khi file mang `kind: tham-chieu`, `kind: lich-su`, hoặc khai `status:` chứa `not built`. Mọi ca khác là **lạm dụng** — dán nó lên một file khó đối chiếu là cách nhanh nhất để làm con số đẹp lên mà không kiểm gì cả.

```bash
grep -rl 'verified: khong-ap-dung' docs --include='*.md'
```

Với mỗi file trong kết quả, mở frontmatter ra đọc và xác nhận lý do miễn trừ có thật.

### 3. Kiểm B — hai file cùng làm chủ một chủ đề

Đây là phép kiểm **cần đọc hiểu**, và là lý do skill này tồn tại. Luật một chủ đề một file chủ ở [`../../CLAUDE.md`](../../CLAUDE.md) §5.

Cách làm:

```bash
grep -rn '^# '  docs --include='*.md'
grep -rn '^## ' docs --include='*.md'
```

So **tiêu đề cấp 1 và cấp 2** giữa các file. Hai file nghi trùng khi:

- Tiêu đề cấp 1 mô tả cùng một chủ đề bằng từ khác — ví dụ một file nói "ranh giới tầng FE", file kia nói "cấu trúc thư mục Angular".
- Cùng một định danh kỹ thuật xuất hiện ở **mục cấp 2 của cả hai file** — tức cả hai đang giữ nội dung, không phải một file trỏ tới file kia.
- Một file có mục dài về chủ đề mà file kia tuyên bố là chủ.

Với mỗi cặp nghi ngờ: mở cả hai, đọc mục nghi trùng, rồi báo cáo dạng *"file X mục N và file Y mục M cùng giữ nội dung về Z"*.

🛑 **Không tự chọn file chủ, không tự rút file kia thành dòng trỏ đường.** Việc chọn file chủ đổi thói quen đọc của mọi agent — người dùng quyết.

### 4. Kiểm C — bảng định tuyến trỏ sai

Bảng định tuyến sống trong các file agent dưới `.claude/agents/` và trong các skill. Hai dạng hỏng:

**C1 — trỏ tới file không còn tồn tại.** Cổng bắt được dạng này, nên nếu bước 0 xanh thì bỏ qua. Nếu bước 0 bị bỏ vì lý do nào đó, tự dò:

```bash
grep -rhoE '(docs|spec)/[A-Za-z0-9._/-]+\.md' .claude --include='*.md' | sort -u
```

Kiểm từng đường dẫn có tồn tại không.

**C2 — trỏ tới file mang `kind: lich-su`.** Đây là dạng nguy hiểm hơn: file tồn tại nên cổng vẫn xanh, nhưng nội dung đã chết và agent vẫn đọc nó như luật sống.

```bash
grep -rl 'kind: lich-su' docs --include='*.md'
```

Với mỗi file lịch sử tìm được, tìm ngược xem còn ai trỏ tới nó không — thay phần trong ngoặc nhọn bằng tên file thật:

```bash
grep -rn '<ten-file-lich-su>' .claude docs --include='*.md'
```

### 5. Kiểm D — file mồ côi

File `docs/` không được **bất kỳ** mục lục hay bảng định tuyến nào trỏ tới thì agent sẽ không bao giờ mở nó. Nó tồn tại nhưng vô hiệu — và tệ hơn file thiếu, vì nó tạo cảm giác chủ đề đó đã được phủ.

```bash
find docs -name '*.md' | while read -r f; do
  hits=$(grep -rl "$(basename "$f")" docs .claude --include='*.md' | grep -cv "^$f$")
  [ "$hits" -eq 0 ] && echo "MO_COI: $f"
done
```

Hai mục lục là nơi một file **phải** tới được: [`../../../docs/README.md`](../../../docs/README.md) và [`../../../docs/wiki-core/README.md`](../../../docs/wiki-core/README.md); ngoài ra là bảng định tuyến của đúng agent cần nó.

Ngoại lệ hợp lệ — ghi nhận, không phải lỗi: file mục lục của chính một khu, và file mang `kind: lich-su` đã cố ý gỡ khỏi mọi bảng.

### 6. Kiểm E — tuyên bố hiện trạng khi repo chưa có `src/`

Xác nhận giai đoạn bằng lệnh, đừng tin trí nhớ:

```bash
test -d src && echo "CO src" || echo "CHUA CO src"
```

Chưa có `src/` → mọi tuyên bố hoàn thành về thành phần backend hay frontend đều là **bịa hiện trạng** ([`../../CLAUDE.md`](../../CLAUDE.md) §4).

```bash
grep -rn "ĐÃ CÓ\|✅ Xong\|FIXED\|Đã bật\|CÓ THẬT" docs --include='*.md'
```

Đọc từng dòng kết quả. Cổng chỉ bắt được ca **thiếu ngày**; nó không bắt được ca ngày đúng mà nội dung sai. Phân ba ca:

| Dòng đang nói về | Kết luận |
| --- | --- |
| Một file `docs/` đã viết xong | Hợp lệ, không phải finding |
| Một cổng đang chạy được ở giai đoạn 1 | Hợp lệ **nếu** cổng thật sự chạy — kiểm bằng lệnh ở bước 0 |
| Code, project, component, endpoint, migration | **Chặn** — chưa có `src/` thì không có gì để xác nhận |

### 7. Kiểm F — luật thiếu cột "ép bằng gì"

[`../../../docs/RULES.md`](../../../docs/RULES.md) tồn tại vì cột đó. Một dòng luật để trống cột này là một gợi ý được mặc áo luật.

Đọc từng bảng trong file, tìm dòng có ô "Ép bằng gì" rỗng, chứa dấu gạch ngang trơn, hoặc chứa chữ mang nghĩa "chưa có" mà **không** kèm dòng tương ứng ở mục Danh sách nợ cuối file.

Luật chưa có cổng thì **phải** xuất hiện ở danh sách nợ — không được để trống cột rồi thôi. Đó là chỗ duy nhất khiến nợ nhìn thấy được.

### 8. Viết báo cáo

Phân ba mức. Mỗi mục neo bằng đường dẫn; không neo được thì không phải finding.

| Mức | Nghĩa |
| --- | --- |
| 🔴 **Chặn** | Tài liệu đang nói sai sự thật, hoặc agent đang được dẫn tới nội dung chết |
| 🟠 **Cần sửa** | Đúng nội dung nhưng sai khuôn — thiếu khoá, thiếu cột, mồ côi |
| 🟡 **Cần người quyết** | Nghi trùng chủ đề. Không có đáp án đúng duy nhất |

## Dừng lại và hỏi khi

1. **Hai file cùng chủ đề nhưng cả hai đều đang được trỏ tới từ bảng định tuyến khác nhau.** Chọn file chủ là quyết định về thói quen đọc của agent — hỏi, đừng chọn hộ.
2. **Một file mồ côi có nội dung dày.** Có thể là file bị bỏ quên, cũng có thể là file cố ý chưa nối vào. Hỏi trước khi đề xuất xoá hay đề xuất nối.
3. **Cổng ở bước 0 đỏ.** Dừng hẳn, không chạy tiếp.
4. **Người dùng yêu cầu skill tự sửa các finding.** Nói rõ skill này chỉ báo cáo; việc sửa `docs/` thuộc `tech-writer` hoặc chính người dùng.
5. **Cần lệnh git ghi** — xem [`../../CLAUDE.md`](../../CLAUDE.md) §1. Nói rõ cần chạy lệnh gì, người dùng tự chạy.

## Đầu ra

Một báo cáo trong hội thoại, không ghi file:

```markdown
## Phạm vi đã soát
<thư mục — kèm kết quả lệnh đếm, không chép số bằng tay>
Cổng check-docs.sh: PASS / FAIL

## 🔴 Chặn
### 1. <tiêu đề ngắn>
**Ở đâu:** <đường dẫn:dòng>
**Vì sao sai:** <luật nào bị vi phạm, ở file nào>
**Đề xuất:** <hướng sửa — KHÔNG tự sửa>

## 🟠 Cần sửa
| # | Ở đâu | Vấn đề | Đề xuất |
|---|-------|--------|---------|

## 🟡 Cần người quyết — nghi trùng chủ đề
| # | File A | File B | Chủ đề trùng | Câu hỏi cho người dùng |
|---|--------|--------|--------------|------------------------|

## Đã cân nhắc và loại
- <thứ trông có vẻ sai nhưng có lý do chính đáng, và lý do đó là gì>

## Lỗ mù của lượt này
- <phép kiểm nào không chạy được và vì sao>
```

Mục **Lỗ mù** là bắt buộc. Một báo cáo không nói mình đã bỏ sót gì sẽ được đọc như thể nó phủ hết — và đó là cách một lượt soát tạo ra cảm giác an toàn giả.

## Trước khi coi là xong

1. Đã chạy `bash .claude/check-docs.sh` ở bước 0 và ghi kết quả vào báo cáo.
2. Đủ sáu phép kiểm A–F, hoặc phép kiểm nào bỏ qua thì đã ghi vào mục **Lỗ mù**.
3. Mọi finding có đường dẫn. Không có finding nào chỉ dựa trên cảm giác.
4. Không sửa bất kỳ file nào.
