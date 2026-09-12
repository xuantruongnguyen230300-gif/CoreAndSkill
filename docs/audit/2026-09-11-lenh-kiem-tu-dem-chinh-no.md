---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 2026-09-11 — Một lệnh kiểm đếm cả chính nó, nên không bao giờ PASS được

> Phát hiện trong lúc bổ sung mười một spec component mới vào [`../Design/COMPONENTS.md`](../Design/COMPONENTS.md).
>
> Viết theo khuôn bốn phần ở [`README.md`](README.md).

---

## 1. Hiện tượng

[`../Design/COMPONENTS.md`](../Design/COMPONENTS.md) kê một phép kiểm để người viết tự xác nhận mục lục không lệch khỏi thư mục spec, kèm tiêu chí *"PASS = số file spec bằng số dòng trong bảng"*. Lệnh đếm dòng bảng là một lệnh `grep -c` theo **chuỗi trạng thái** `spec xong, chưa dựng`.

Chạy thật, hai con số ra **39** và **41**. Chênh đúng hai, và chênh ngay cả khi mục lục hoàn toàn khớp thư mục.

Hai dòng thừa:

1. Dòng nhãn fidelity ở đầu file — nó nhắc tên trạng thái để nói mọi dòng bên dưới mang trạng thái đó.
2. **Chính khối lệnh kiểm.** Lệnh chứa chuỗi mà nó đi tìm, nên nó luôn tự đếm mình.

Phép kiểm này nằm trong tài liệu từ lúc file được viết. Không ai phát hiện, vì tiêu chí PASS của nó chỉ được chạy khi có người thêm component mới — và cho tới lượt này thì chưa có ai thêm.

---

## 2. Nguyên nhân gốc

**Phép kiểm neo vào một chuỗi văn bản thay vì neo vào cấu trúc mà nó muốn đếm.**

Chuỗi `spec xong, chưa dựng` là một **giá trị** trong bảng. Nhưng cùng chuỗi đó cũng xuất hiện hợp lệ ở hai chỗ khác trong cùng file: khi tài liệu **nói về** trạng thái đó, và khi tài liệu **kê lệnh tìm** trạng thái đó. Ba lần xuất hiện, ba vai khác nhau, cùng một chuỗi — `grep` không phân biệt được vai.

Đây là cùng một cơ chế với thứ [`2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md`](2026-09-09-cong-no-op-va-hai-mo-hinh-nguon.md) đã ghi, nhìn từ hướng ngược lại: ở đó một mục cổng báo OK trên tập rỗng vì nó không tự chứng minh được đầu vào; ở đây một phép kiểm báo FAIL trên tập đúng vì nó không tự loại được chính mình ra khỏi đầu vào. Cả hai đều là **phép đếm không biết mình đang đếm gì**.

Có một tầng nguyên nhân nữa, đáng ghi riêng: phép kiểm này **không thuộc cổng tự động**. Nó là một lệnh kê trong văn xuôi để người viết tự chạy. Không có gì bắt nó phải đúng, và không có gì báo khi nó sai — khác hẳn các mục trong [`../../.claude/check-docs.sh`](../../.claude/check-docs.sh), nơi một mục hỏng sẽ tự lộ ra ở lần chạy kế tiếp.

---

## 3. Cách vá

Đổi lệnh sang neo vào **hình dạng của một dòng bảng có link spec** thay vì neo vào chuỗi trạng thái:

```bash
grep -cE '^\| `[A-Za-z]+` \|.*\]\(\./Components/' docs/Design/COMPONENTS.md
```

Biểu thức này đòi ba thứ cùng lúc trên một dòng: mở đầu bằng ô bảng chứa tên component trong dấu nháy ngược, và có một link trỏ vào `./Components/`. Dòng văn xuôi không khớp. Khối lệnh không khớp — nó không phải một dòng bảng. Sau khi sửa, hai con số cùng ra **39**.

Lần vá đầu tiên còn kèm một đoạn văn kể lại lệnh cũ sai thế nào. Cổng `check-docs.sh` §17 chặn ngay: **luật D24 cấm file nội dung kể lại bản trước của chính nó.** Đoạn kể đó được chuyển sang chính file bạn đang đọc, và `COMPONENTS.md` chỉ giữ một dòng trỏ tới đây. Đó là hành vi đúng của cổng, và đáng ghi lại: cổng bắt được vi phạm do chính lần vá sinh ra.

---

## 4. Cổng nào lẽ ra phải bắt

**Không cổng nào, và đó mới là điều đáng nói.**

| Mục cổng | Vì sao không bắt được |
| --- | --- |
| §4 — đường dẫn trích dẫn phải tồn tại | Lệnh này không trích dẫn đường dẫn nào sai. `docs/Design/COMPONENTS.md` có thật |
| §8 — bảng định tuyến trỏ đúng chủ đề | Đây không phải bảng định tuyến |
| §15, §16 — sổ chủ quyền | Đây không phải một định nghĩa có mốc |

Cổng kiểm **tài liệu**, không kiểm **những lệnh mà tài liệu bảo người ta chạy**. Một lệnh sai trong văn xuôi là một lệnh im lặng hỏng — nó không làm đỏ gì cả, nó chỉ khiến người chạy nó bối rối rồi bỏ qua.

### Nợ để lại, nói rõ thay vì vá vội

Repo có **nhiều** phép kiểm kiểu này nằm rải trong văn xuôi — [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6 còn khuyến khích chúng, vì thay một bảng đếm tay bằng một lệnh là đúng hướng. Nhưng không lệnh nào trong số đó được ai kiểm.

Một mục cổng mới — *"mọi khối lệnh kê trong tài liệu phải chạy được và phải trả về thứ nó hứa"* — là việc lớn hơn nhiều so với vẻ ngoài: nó cần biết lệnh nào an toàn để chạy, cần một cách khai tiêu chí PASS máy đọc được, và cần chạy được trên máy không có `src/`. Ghi vào đây như **nợ nhìn thấy được**, cùng khuôn với [`../RULES.md`](../RULES.md) §10, thay vì dựng vội một mục cổng nửa vời — một mục cổng chỉ kiểm được vài lệnh dễ sẽ tạo ra đúng cảm giác an toàn sai mà audit ngày 2026-09-09 đã ghi.

---

## 5. Bài học một câu

> **Một phép kiểm tìm theo chuỗi văn bản sẽ tìm thấy chính nó.** Neo phép đếm vào cấu trúc — hình dạng một dòng bảng, một thẻ, một khoá — chứ đừng neo vào một chuỗi mà tài liệu còn dùng để nói về chuỗi đó.
