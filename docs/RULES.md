---
kind: luat
scope: core
verified: chua-doi-chieu
---

# RULES — mục lục các bảng luật, kèm cột "ép bằng gì"

> **Lý do các bảng luật tồn tại nằm ở cột "Ép bằng gì".**
>
> Một luật trả lời được câu *"luật này được ép bằng cái gì?"* bằng một cổng cụ thể thì nó là **luật**. Trả lời "bằng niềm tin" thì nó là **gợi ý** — và phải nằm ở [danh sách nợ](DEBT.md), nhìn thấy được, chứ không trộn lẫn vào các bảng luật.
>
> Ba mức: **MUST** (vi phạm là lỗi) · **MUST NOT** (cấm) · **SHOULD** (nên, lệch thì phải nêu lý do trong PR).

**Cột "Trạng thái"** cho biết cổng đã chạy được chưa:
`✅` = đang chạy ở giai đoạn 1 · `✅⌀` = cổng chạy nhưng **tập đầu vào hiện rỗng** · `📐` = cổng **đã thiết kế, chưa viết** — cột *Ép bằng gì* đã chốt tên, chưa có file nào mang tên đó · `⏸️` = cổng **đã viết thật, chưa chạy lần nào** — thứ còn thiếu là môi trường, không phải code · `🕳️` = **không có cổng**, chỉ review canh, xem [`DEBT.md`](DEBT.md).

## Tệp nào chứa phạm vi nào

Mỗi mã luật nằm ở đúng một tệp. Tệp con giữ nguyên số mục §N, nên một trích dẫn *"`RULES.md` §N"* đi qua mục cùng số ở dưới rồi tới tệp chứa nó.

| Phạm vi | Mục | Mã | Tệp |
| --- | --- | --- | --- |
| Tài liệu, ranh giới `.claude` ↔ `docs` | §1, §2 | `D*`, `C*` | [`RULES-DOCS.md`](RULES-DOCS.md) |
| Backend | §3, §4, §5, §6, §8 (trừ T6, T12), §9 | `A*`, `B*`, `E*`, `R*`, `S*`, `M*`, `T*` còn lại | [`RULES-BE.md`](RULES-BE.md) |
| Frontend | §7 | `F*` | [`RULES-FE.md`](RULES-FE.md) |
| Kiểm thử chung Backend + Frontend | §8: T6, T12 | `T6`, `T12` | [`RULES-CHUNG.md`](RULES-CHUNG.md) |
| Nợ — luật chưa có cổng | §10 | mã nợ | [`DEBT.md`](DEBT.md) |

## Tự rà cột "Trạng thái" — luật D39

> **Cột này lệch dần nếu chỉ sửa từng dòng một.** Rà **cả bốn bảng cùng lúc**, theo đúng một tiêu chí — *tên test ở cột "Ép bằng gì" có trong `src/BE/Tests/` hay không* — rồi đối chiếu với ký hiệu đang khai. Đừng chép kết quả vào đây ([`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §6); chạy lại lệnh. Lần rà toàn bảng gần nhất: **2026-09-27**.
>
> ```bash
> scan() {
>   awk '/^\| [A-Z][0-9]+ \|/ { print }' docs/RULES-*.md \
>     | grep -vE '^\| F[0-9]+ \|' | grep -iE 'test'
> }
> rows=$(scan | grep -c .)
> toks=$(scan | grep -oE '`[A-Z][A-Za-z0-9]*_[A-Za-z0-9_]+`' | grep -c .)
> scan | while IFS= read -r row; do
>   st=$(printf '%s' "$row" | sed 's/.*| *\([^|]*\) *|$/\1/')
>   for t in $(printf '%s' "$row" | grep -oE '`[A-Z][A-Za-z0-9]*_[A-Za-z0-9_]+`' | tr -d '`'); do
>     if grep -rqs "$t" src/BE --include=*.cs; then
>       case "$st" in *📐*) echo "KHAI 📐 NHUNG TEST DA CO: $t";; esac
>     else
>       case "$st" in *✅*) echo "KHAI ✅ NHUNG KHONG CO TEST: $t";; esac
>     fi
>   done
> done
> echo "da xet: $rows hang, $toks ten test"
> [ "$rows" -gt 0 ] && [ "$toks" -gt 0 ] || echo "FAIL: tap dau vao RONG"
> ```
>
> **PASS: không in dòng nào mở đầu bằng `KHAI` hay `FAIL`.** Dòng `da xet:` là một phần của tiêu chí — hai số về `0` nghĩa là bộ lọc đã ngừng bắt hàng nào và lệnh xanh vì tập rỗng ([`audit/2026-09-11-lenh-kiem-tu-dem-chinh-no.md`](audit/2026-09-11-lenh-kiem-tu-dem-chinh-no.md)), nên `0` là FAIL.
>
> **Hai bộ lọc hàng gánh hai việc khác nhau — đừng gộp.** Lệnh quét bốn tệp `RULES-*.md`, **không** quét tệp này: §10 ở đây trỏ sang [`DEBT.md`](DEBT.md), nơi hàng nợ có cột cuối *Chặn bởi* chứ không phải *Trạng thái*. `grep -vE '^\| F[0-9]+ \|'` loại luật FE: chúng ép bằng `scripts/fe-gate.sh`/ESLint nên không bao giờ có tên test trong `src/BE`, mà F4/F21 lại mang token khớp mẫu định danh (`BUSINESS_MODULES`, `CORE_ROUTES`) — lọt vào tầm quét là ba dòng báo sai ngay.
>
> Lệnh chỉ trả lời *test có tồn tại không* — nó không biết test đã chạy xanh chưa, và nó mù với cổng không phải test (bước CI, `TreatWarningsAsErrors`, `scripts/fe-gate.sh`). Hai câu hỏi đó vẫn phải trả lời bằng cách chạy thật rồi đọc output; lệnh này chỉ chặn **hồi quy** của đúng kiểu lệch mà nó đo được.
>
> **Vì sao tách `✅⌀` khỏi `✅`:** một mục cổng xét 0 mục vẫn "chạy", nhưng chưa từng chặn được gì — gộp chung ký hiệu làm bảng trả lời sai đúng câu nó tồn tại để trả lời: *luật nào đang thật sự được máy ép?* **Vì sao tách `⏸️` khỏi `📐`:** `📐` cần **người viết cổng**, `⏸️` cần **môi trường** (Docker trên máy chạy, `src/` vào git để job CI kích hoạt); gộp lại thì người nhận việc đi viết lại một cổng đã tồn tại — tức tạo nguồn thứ hai, đúng thứ §5 của [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) cấm.
>
> ⚠️ Lệnh rà bên trên **chưa** biết `⏸️`: nó chỉ có nhánh cho `📐` và cho `✅`, nên dòng mang `⏸️` đi qua im lặng. Nhánh còn thiếu là *khai `⏸️` mà không có file cổng nào mang tên đó* — ghi ở dòng **D39** của [nợ](DEBT.md).

---

## 1. Luật tài liệu — hiệu lực NGAY

→ [`RULES-DOCS.md`](RULES-DOCS.md) §1

## 2. Luật ranh giới `.claude` ↔ `docs`

→ [`RULES-DOCS.md`](RULES-DOCS.md) §2

## 3. Luật kiến trúc Backend

→ [`RULES-BE.md`](RULES-BE.md) §3

## 4. Luật Domain & dữ liệu

→ [`RULES-BE.md`](RULES-BE.md) §4

## 5. Luật lỗi & envelope

→ [`RULES-BE.md`](RULES-BE.md) §5

## 6. Luật bảo mật & phân quyền

→ [`RULES-BE.md`](RULES-BE.md) §6

## 7. Luật Frontend

→ [`RULES-FE.md`](RULES-FE.md) §7

## 8. Luật kiểm thử

→ T6, T12: [`RULES-CHUNG.md`](RULES-CHUNG.md) §8 · T1–T5, T7–T11: [`RULES-BE.md`](RULES-BE.md) §8

## 9. Luật multi-tenant

→ [`RULES-BE.md`](RULES-BE.md) §9

## 10. Danh sách nợ — luật CHƯA có cổng

> 📖 Đã tách ra file riêng: [`DEBT.md`](DEBT.md). Mã nợ, cột *Chặn bởi* và hai lệnh đếm nằm ở đó.
>
> Tiêu đề này giữ lại để mọi trích dẫn *"`RULES.md` §10"* đã viết còn tới được đích — nó không giữ nội dung nào.
