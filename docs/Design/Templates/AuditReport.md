---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khuôn — báo cáo audit giao diện

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Một lượt audit trả lời đúng một câu: **khu Design này có còn nói thật không.**
>
> 🛑 **Ba khoá frontmatter ĐỔI khi dùng:** khuôn mang `kind: luat` / `scope: core`; báo cáo của một lượt audit mang **`kind: tham-chieu`** và **`scope: du-an`** — nó là ảnh chụp một thời điểm, không phải luật code phải tuân. Đặt nhầm `kind: luat` sẽ khiến agent coi một phát hiện đã sửa xong là quy tắc phải giữ, và báo *"doc yêu cầu X, code không có X"* cho những X chưa bao giờ là luật.

---

## 1. Audit kiểm gì

Bốn nhóm, theo thứ tự mức nghiêm trọng giảm dần:

| # | Nhóm | Câu hỏi | Kiểm bằng |
| --- | --- | --- | --- |
| 1 | **Trung thực** | Có spec nào khẳng định hiện trạng mà không neo được bằng chứng không? Có trích dẫn nào trỏ vào file không tồn tại không? | Đọc + cổng |
| 2 | **Nhãn** | Nhãn fidelity và trạng thái component có đúng thực tế không? | Đọc + đối chiếu source |
| 3 | **Toàn vẹn** | Có spec nào thiếu mục bắt buộc không? Có giá trị thô nào không có token đằng sau không? | Lệnh + đọc |
| 4 | **Nhất quán** | Có hai chỗ nói ngược nhau không? Có component nào trùng vai không? | Đọc |

**Nhóm 1 luôn là chặn.** Một tài liệu mô tả thứ không tồn tại còn tệ hơn không có tài liệu: người đọc tin nó và đi làm theo.

---

## 2. Dàn bài — chép từ đây xuống

### `# Audit giao diện — <tên dự án> — <ngày>`

Ngày nằm trong tiêu đề vì báo cáo là ảnh chụp một thời điểm.

### `## Kết luận`

**Một đoạn.** `ĐẠT` hoặc `CHẶN`, và vì sao. `CHẶN` phải gọi tên số hiệu phát hiện gây chặn.

`ĐẠT` đòi **không có phát hiện mức chặn nào** và cổng tài liệu xanh. Cảnh báo được ghi lại, không gây chặn.

### `## Phạm vi`

Lượt audit này đọc những file nào, **và cố ý bỏ qua những file nào**. Vế thứ hai quan trọng ngang vế thứ nhất: một báo cáo không nói mình bỏ qua cái gì sẽ bị đọc như thể nó đã kiểm hết.

### `## Phát hiện`

Bảng. Mức chặn xếp trước.

### `## Việc phải làm`

Lệnh hoặc hành động cụ thể cho từng phát hiện mức chặn.

### `## Lần audit tiếp theo nên xem gì`

---

## 3. Bảng phát hiện — sáu cột

| # | Nhóm | Mức | Ở đâu | Vấn đề | Sửa thế nào |
| --- | --- | --- | --- | --- | --- |
| 1 | trung thực | **chặn** | `Components/Toast.md` mục Trạng thái | Khẳng định "hiện dùng `aria-live="polite"`" nhưng repo chưa có `src/` | Đổi câu về thì tương lai, hoặc đổi nhãn file |
| 2 | toàn vẹn | cảnh báo | `Components/Badge.md` | Thiếu dòng `loading` trong bảng trạng thái | Thêm dòng, ghi `Không áp dụng.` kèm lý do |

Ba mức, không hơn:

| Mức | Nghĩa | Ảnh hưởng kết luận |
| --- | --- | --- |
| **chặn** | Tài liệu đang nói sai, hoặc thiếu thứ khiến không dựng được | `CHẶN` |
| **cảnh báo** | Thiếu sót thật nhưng không dẫn tới hiểu sai | Ghi lại, vẫn `ĐẠT` |
| **ghi nhận** | Quan sát, chưa phải lỗi | Ghi lại |

**Không có mức thứ tư.** Thang mức càng nhiều bậc thì càng nhiều thứ được đẩy xuống bậc thấp nhất để bảng trông đẹp.

---

## 4. Ba điều một báo cáo audit không được làm

| Không được | Vì sao |
| --- | --- |
| 🛑 **Tự sửa spec rồi ghi là đã sửa trong cùng một lượt** | Audit và sửa là hai việc. Trộn lại thì không ai kiểm được lượt sửa đó. Báo cáo nêu vấn đề; lượt sau sửa |
| 🛑 **Ghi `✅ đã sửa` cho thứ chưa sửa** | Đây là khuôn hỏng mà [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §4 cấm. Nhãn "đã xong" được thiết kế để không ai kiểm lại |
| 🛑 **Chép danh sách file vi phạm thành bảng cứng** | Danh sách đó mục ruỗng ngay lượt sau. Ghi **lệnh** tìm ra chúng và tiêu chí PASS ([`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6) |

Ví dụ đúng cho điều thứ ba:

```bash
grep -Ln 'Cần chốt' docs/Design/Components/*.md
grep -rn 'src/' docs/Design --include='*.md'
```

PASS = lệnh đầu không in file nào; lệnh sau không in dòng nào ngoài các dòng đang nói về luật cấm.

---

## 5. Ví dụ điền — mục Kết luận

Viết đúng:

```text
## Kết luận

CHẶN. Ba phát hiện mức chặn: #1, #4 và #7.

Cả ba cùng một khuôn — spec khẳng định hiện trạng trong khi repo chưa có src/
để đối chiếu. Đây không phải ba lỗi rời rạc mà là một hiểu nhầm về nhãn fidelity
(docs/Design/CLAUDE.md §2), nên sửa nên làm một lượt cho cả ba.

Cổng tài liệu xanh — nhưng điều đó không nói gì về ba phát hiện này: cổng không
đọc hiểu nội dung.
```

Viết **sai** — kết luận không nói được gì:

```text
## Kết luận

Nhìn chung tốt, còn một vài chỗ cần cân nhắc thêm.
```

---

## 6. Bắt buộc và tuỳ chọn

| Phần | Bắt buộc? | Ghi chú |
| --- | --- | --- |
| Ba khoá frontmatter, `kind: tham-chieu` | ✅ **Bắt buộc** | |
| Ngày trong tiêu đề | ✅ **Bắt buộc** | |
| `## Kết luận` — một đoạn, ĐẠT/CHẶN | ✅ **Bắt buộc** | `CHẶN` phải gọi tên số hiệu phát hiện |
| `## Phạm vi` — kể cả phần cố ý bỏ qua | ✅ **Bắt buộc** | |
| Bảng phát hiện sáu cột | ✅ **Bắt buộc** | Không phát hiện nào thì ghi `Không có` |
| Cột "Sửa thế nào" có nội dung cụ thể | ✅ **Bắt buộc** | "Cần xem lại" không phải cách sửa |
| Lệnh + tiêu chí PASS thay cho danh sách cứng | ✅ **Bắt buộc** | |
| `## Lần audit tiếp theo nên xem gì` | ⬜ Tuỳ chọn | Rất nên có |
| Đo tương phản lại toàn bộ bảng màu | ⬜ Tuỳ chọn | Cần khi lượt trước có đổi màu |
| Tự sửa trong cùng lượt | 🛑 **Cấm** | |

---

## 7. Danh sách kiểm

- [ ] Kết luận là `ĐẠT` hoặc `CHẶN`, không phải một câu chung chung.
- [ ] `CHẶN` gọi tên số hiệu phát hiện gây chặn.
- [ ] Phạm vi nói rõ cả phần cố ý bỏ qua.
- [ ] Mỗi phát hiện có cách sửa cụ thể.
- [ ] Không danh sách file cứng — chỉ lệnh và tiêu chí PASS.
- [ ] Không tự sửa spec trong lượt này.
- [ ] `kind: tham-chieu`, không phải `kind: luat`.
- [ ] `bash .claude/check-docs.sh` xanh.
