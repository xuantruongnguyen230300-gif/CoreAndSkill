---
name: "arch-check"
description: "Kiểm tuân thủ kiến trúc. Ở giai đoạn chưa có src/ thì chạy ở chế độ tài liệu: đối chiếu RULES.md với các file quy ước để tìm luật không ai mô tả cách thi công và quy ước không luật nào ép. Khi có src/ sẽ mở rộng sang chạy ArchTests và cổng FE."
argument-hint: "[phạm vi] - 'BE' hoặc 'FE' hoặc để trống để soát cả hai chiều tài liệu"
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

Một luật kiến trúc chỉ có tác dụng khi **cả hai** đầu đều có mặt:

| Đầu | Ở đâu | Trả lời |
| --- | --- | --- |
| **Luật + cổng** | [`../../../docs/RULES.md`](../../../docs/RULES.md) | Vi phạm thì cái gì bắt được |
| **Cách thi công** | Các file trong khu quy ước | Người viết code phải làm thế nào |

Thiếu đầu thứ hai: luật tồn tại nhưng không ai biết làm thế nào cho đúng, nên nó bị vi phạm một cách thiện chí. Thiếu đầu thứ nhất: quy ước tồn tại nhưng không cổng nào canh, nên nó trôi trong vài tháng và không ai biết lúc nào nó bắt đầu trôi.

Skill này soát **hai chiều lệch** đó.

## ⚠️ Xác định chế độ chạy — bước bắt buộc đầu tiên

```bash
test -d src && echo "CO src" || echo "CHUA CO src"
```

| Kết quả | Chế độ | Chạy mục nào |
| --- | --- | --- |
| **CHƯA CÓ `src/`** | Chế độ tài liệu | Bước 1–5 |
| **CÓ `src/`** | Chế độ đầy đủ | Bước 1–5, cộng bước 6 |

Repo đang ở giai đoạn 1 — trạng thái đọc ở [`../../../docs/README.md`](../../../docs/README.md), mục trạng thái đầu file. **Nói rõ chế độ đang chạy ngay đầu báo cáo**, để không ai đọc một báo cáo chế độ tài liệu như thể nó đã kiểm code.

## Các bước thực hiện

### 1. Đọc bảng luật

Mở [`../../../docs/RULES.md`](../../../docs/RULES.md). Ba cột cần cho lượt này: luật, cột "Ép bằng gì", cột trạng thái.

Cột trạng thái phân biệt cổng đang chạy được với cổng phải chờ tới khi có `src/`. Ở chế độ tài liệu, **luật phải chờ `src/` không phải finding** — nó đang đúng trạng thái của nó. Báo finding cho những luật đó là lặp lại đúng lỗi đã có tên: chấm một thứ đang cố ý dở dang.

`$ARGUMENTS` nêu `BE` hoặc `FE` → chỉ soát các bảng luật thuộc phía đó.

### 2. Chiều A — luật khai trong bảng nhưng không file nào mô tả cách thi công

Với mỗi dòng luật, tìm xem có file quy ước nào nói **làm thế nào** cho đúng luật đó không. Bảng tra chủ đề ở [`../../../docs/README.md`](../../../docs/README.md) là điểm bắt đầu; mở **đúng một** file cho mỗi chủ đề, đừng đọc cả thư mục.

```bash
grep -rn '<tu-khoa-cua-luat>' docs/quy-uoc docs/wiki-core --include='*.md'
```

Ba mức kết luận:

| Tình huống | Kết luận |
| --- | --- |
| Có file mô tả đủ để code theo | PASS |
| Có nhắc tới nhưng chỉ một câu, không đủ để làm theo | PARTIAL — ghi rõ thiếu gì |
| Không file nào nhắc tới | **MISSING** — đây là luật treo |

Luật treo nguy hiểm vì nó **có cổng**: cổng sẽ đỏ, người viết code sẽ không biết phải sửa thế nào, và cách xử lý nhanh nhất họ tìm ra sẽ là vô hiệu hoá cổng.

### 3. Chiều B — quy ước mô tả nhưng không luật nào ép

Đọc các file quy ước, tìm những câu ở dạng mệnh lệnh — "phải", "không được", "luôn", "cấm". Với mỗi câu như thế, tìm ngược trong bảng luật xem có dòng nào tương ứng không.

Không có → một trong ba khả năng, phải phân biệt:

- **Đúng là luật, thiếu dòng trong bảng.** Finding: đề xuất thêm dòng, kèm cột "Ép bằng gì".
- **Là gợi ý chứ không phải luật.** Không phải finding, nhưng nên viết lại cho bớt giọng mệnh lệnh — một gợi ý mặc áo luật làm người đọc mất khả năng phân biệt cái nào bắt buộc.
- **Là luật nhưng chưa có cổng nào ép được.** Nó phải nằm ở mục Danh sách nợ cuối file luật, nhìn thấy được. Không có ở đó → finding.

Quy tắc bổ sung luật đã khai ngay trong file luật: thêm một dòng vào bảng nào thì **phải** khai cột "Ép bằng gì"; chưa có cổng thì dòng đó thuộc danh sách nợ, không được để trống cột.

### 4. Chiều C — cổng khai trong bảng nhưng không tồn tại

Đây là dạng hỏng đắt nhất: một cổng hỏng âm thầm còn tệ hơn không có cổng, vì nó tạo cảm giác được bảo vệ.

Với mỗi cổng khai ở cột "Ép bằng gì" và mang trạng thái đang chạy được:

```bash
ls -la .claude/check-docs.sh
grep -c '^section ' .claude/check-docs.sh
bash .claude/check-docs.sh
```

Script không tồn tại, hoặc chạy nhưng không có mục nào tương ứng với luật đang xét → **finding mức chặn**.

Kiểm thêm một dạng hỏng không lộ ra khi chạy tay: dòng lệnh nào của cổng còn bị bỏ sót trong khối cho phép của cấu hình harness thì trên thực tế nó bị prompt chặn, và **một cổng bị prompt chặn là một cổng không ai chạy** ([`../../CLAUDE.md`](../../CLAUDE.md) §8).

```bash
grep -n 'permissions' .claude/settings.json
```

### 5. Chiều D — ranh giới Core ↔ Module trong tài liệu

Mở [`../../../docs/kien-truc-core-module.md`](../../../docs/kien-truc-core-module.md). Ba luật ranh giới và bốn tiêu chí phân loại nằm ở đó.

Ở chế độ tài liệu, soát được hai thứ:

- Các file quy ước có mô tả nào **mâu thuẫn** với ranh giới đã chốt không — ví dụ một quy ước cho phép tầng dưới biết về tầng trên.
- Mỗi luật ranh giới có tên ArchTest tương ứng khai ở cột "Ép bằng gì" không. Không có tên → luật ranh giới đang chỉ được ép bằng niềm tin.

🛑 Không kiểm được ở chế độ tài liệu: bản thân code có tuân ranh giới không. Ghi việc đó vào mục **Lỗ mù**, đừng để trống rồi để người đọc tự hiểu là đã kiểm.

### 6. Chế độ đầy đủ — 📐 CHƯA DÙNG ĐƯỢC ở giai đoạn hiện tại

> **Mục này mô tả sẵn phần sẽ chạy khi `src/` tồn tại. Ở giai đoạn 1 nó KHÔNG chạy được — bỏ qua và ghi vào mục Lỗ mù.**

Khi có `src/`, bước 6 bổ sung ba việc, chạy **sau** bước 1–5 chứ không thay thế:

- Chạy bộ ArchTests bằng lệnh test của .NET, đối chiếu từng tên test với cột "Ép bằng gì" trong bảng luật. Test khai trong bảng mà không tồn tại trong solution là finding mức chặn — đó đúng là khuôn "cổng không tồn tại".
- Chạy cổng FE và đối chiếu tương tự.
- Với mỗi luật mang trạng thái chờ giai đoạn 2, kiểm xem cổng của nó đã bật chưa; chưa bật thì trạng thái trong bảng luật phải được cập nhật.

Quy trình chi tiết của hai cổng đó thuộc `backend-expert` và `frontend-expert`; skill này chỉ hợp nhất kết quả.

## Dừng lại và hỏi khi

1. **Một câu mệnh lệnh trong quy ước không rõ là luật hay gợi ý.** Đây là quyết định về mức ràng buộc — hỏi, đừng tự xếp loại.
2. **Hai file quy ước mô tả cùng một luật theo hai cách khác nhau.** Báo mâu thuẫn, đừng tự chọn bên nào đúng; việc chọn file chủ thuộc skill `doc-sync`.
3. **Một cổng khai trong bảng nhưng script không tồn tại.** Dừng, báo ngay — mọi kết luận khác của lượt này đang đứng trên một nền không có thật.
4. **Người dùng yêu cầu sửa các finding.** Skill này chỉ báo cáo; sửa `docs/` thuộc `tech-writer`, sửa code thuộc hai agent thi công.
5. **Cần lệnh git ghi** — xem [`../../CLAUDE.md`](../../CLAUDE.md) §1. Nói rõ cần chạy lệnh gì, người dùng tự chạy.

## Đầu ra

```markdown
## Chế độ lượt này
Tài liệu (chưa có src/) / Đầy đủ
Phạm vi: BE / FE / cả hai chiều tài liệu

## Kết quả

| # | Luật | Có file mô tả cách thi công | Có cổng | Kết luận |
|---|------|------------------------------|---------|----------|

## Finding

### F1 — <tiêu đề ngắn> — <Nghiêm trọng / Trung bình / Nhẹ>
**Chiều lệch:** A (luật treo) / B (quy ước không luật) / C (cổng không tồn tại) / D (mâu thuẫn ranh giới)
**Ở đâu:** <đường dẫn:dòng>
**Vì sao là lỗi:** <kịch bản hỏng cụ thể>
**Đề xuất:** <hướng sửa — KHÔNG tự sửa>

## Không phải finding — luật đang chờ giai đoạn 2
- <liệt kê, để người đọc biết chúng đã được cân nhắc>

## Lỗ mù của lượt này
- Chế độ tài liệu không kiểm được code có tuân ranh giới hay không.
- <thứ khác không soát được và vì sao>
```

## Trước khi coi là xong

1. Đã xác định chế độ bằng lệnh và ghi vào đầu báo cáo.
2. Bốn chiều A–D đều có kết luận, hoặc chiều nào bỏ qua thì đã ghi vào **Lỗ mù**.
3. Không báo finding cho luật đang mang trạng thái chờ giai đoạn 2.
4. Mọi finding neo bằng đường dẫn.
5. Chạy `bash .claude/check-docs.sh` nếu lượt này có chạm tài liệu.
6. Không sửa file nào.
