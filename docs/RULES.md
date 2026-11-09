---
kind: luat
scope: core
verified: chua-doi-chieu
---

# RULES — mục lục các bảng luật, kèm cột "ép bằng gì"

> **Lý do các bảng luật tồn tại nằm ở cột "Ép bằng gì".**
>
> Mọi luật trả lời câu *"luật này được ép bằng cái gì?"*: một cổng cụ thể, hoặc ai canh hôm nay kèm *chưa có cổng* hay *không ép được bằng máy*. Luật chưa có cổng vẫn nằm trong bảng, mang `🕳️` — chỗ hở phải nhìn thấy được, không phải để trông như đã kín cổng.
>
> Ba mức: **MUST** (vi phạm là lỗi) · **MUST NOT** (cấm) · **SHOULD** (nên, lệch thì phải nêu lý do trong PR).

**Cột "Trạng thái"** cho biết cổng đã chạy được chưa:
`✅` = cổng đang chạy · `✅ một phần` = cổng đang chạy nhưng chỉ phủ **một phần** luật — ô *Ép bằng gì* nêu phần máy ép và ai canh phần còn lại · `✅⌀` = cổng chạy nhưng **tập đầu vào hiện rỗng** · `📐` = cổng **đã thiết kế, chưa viết** — cột *Ép bằng gì* đã chốt tên, chưa có file nào mang tên đó · `⏸️` = cổng **đã viết thật, chưa chạy lần nào** — thứ còn thiếu là môi trường, không phải code · `🕳️` = **không có cổng**: ô *Ép bằng gì* nêu ai canh hôm nay, kèm *chưa có cổng* hoặc *không ép được bằng máy*; điều kiện đang chờ ghi sau cụm `kích hoạt:`, phần chờ người dùng quyết ghi sau cụm `chưa chốt:` ([ADR-0114](adr/0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)) · `Hết áp dụng (<ngày>)` = điều kiện của luật không còn đúng ở repo nào — dòng giữ mã để mã không bị dùng lại, ô *Ép bằng gì* nêu vì sao hết áp dụng.

Liệt kê bằng lệnh, đừng chép số:

```bash
# Dòng chưa có cổng: mang 🕳️ hay 📐 ở cột cuối.
LC_ALL=C grep -hE '^\| [A-Z][0-9]+ .*\| *(🕳️|📐)[^|]*\|$' docs/RULES-*.md
# Dòng còn chờ: điều kiện kích hoạt, hoặc phần chờ người dùng quyết.
LC_ALL=C grep -hE '^\| [A-Z][0-9]+ .*(kích hoạt:|chưa chốt:)' docs/RULES-*.md
```

**Mã luật mang đúng một nghĩa ở mọi tệp và không bao giờ dùng lại**, kể cả mã đã ngừng dùng. Trước khi cấp mã mới, tra lịch sử — ra dòng nào thì mã đã dùng. Ép bằng: chưa có cổng; người cấp mã chạy lệnh dưới — trên clone nông, lệnh đầu có thể sót mã.

```bash
git log -S'B36' --oneline -- docs/
grep -rn 'B36' docs/
```

## Tệp nào chứa phạm vi nào

Mỗi mã luật nằm ở đúng một tệp. Tệp con giữ nguyên số mục §N, nên một trích dẫn *"`RULES.md` §N"* đi qua mục cùng số ở dưới rồi tới tệp chứa nó.

| Phạm vi | Mục | Mã | Tệp |
| --- | --- | --- | --- |
| Tài liệu, ranh giới `.claude` ↔ `docs` | §1, §2 | `D*` trừ D25, D26; `C*` | [`RULES-DOCS.md`](RULES-DOCS.md) |
| Backend | §3, §4, §5, §6, §8 (trừ T1, T6, T12, T14), §9 | `A*`, `B*`, `E*`, `R*`, `S*`, `M*`, `T*` còn lại | [`RULES-BE.md`](RULES-BE.md) |
| Frontend | §7 | `F*`, D25, D26 | [`RULES-FE.md`](RULES-FE.md) |
| Kiểm thử chung Backend + Frontend; cụm cấu hình máy dev BE ↔ FE | §8: T1, T6, T12, T14 | `T1`, `T6`, `T12`, `T14` | [`RULES-CHUNG.md`](RULES-CHUNG.md) |

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
> **Hai bộ lọc hàng gánh hai việc khác nhau — đừng gộp.** Lệnh quét bốn tệp `RULES-*.md`, **không** quét tệp này. `grep -vE '^\| F[0-9]+ \|'` loại luật FE: chúng ép bằng `scripts/fe-gate.sh`/ESLint nên không bao giờ có tên test trong `src/BE`, mà F4/F21 lại mang token khớp mẫu định danh (`BUSINESS_MODULES`, `CORE_ROUTES`) — lọt vào tầm quét là ba dòng báo sai ngay.
>
> Lệnh chỉ trả lời *test có tồn tại không* — nó không biết test đã chạy xanh chưa, và nó mù với cổng không phải test (bước CI, `TreatWarningsAsErrors`, `scripts/fe-gate.sh`). Hai câu hỏi đó vẫn phải trả lời bằng cách chạy thật rồi đọc output; lệnh này chỉ chặn **hồi quy** của đúng kiểu lệch mà nó đo được.
>
> **Vì sao tách `✅⌀` khỏi `✅`:** một mục cổng xét 0 mục vẫn "chạy", nhưng chưa từng chặn được gì — gộp chung ký hiệu làm bảng trả lời sai đúng câu nó tồn tại để trả lời: *luật nào đang thật sự được máy ép?* **Vì sao tách `⏸️` khỏi `📐`:** `📐` cần **người viết cổng**, `⏸️` cần **môi trường** (Docker trên máy chạy, một lượt job CI chạy tới bước đó); gộp lại thì người nhận việc đi viết lại một cổng đã tồn tại — tức tạo nguồn thứ hai, đúng thứ §5 của [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) cấm.
>
> ⚠️ Lệnh rà bên trên **chưa** biết `⏸️`: nó chỉ có nhánh cho `📐` và cho `✅`, nên dòng mang `⏸️` đi qua im lặng. Nhánh còn thiếu là *khai `⏸️` mà không có file cổng nào mang tên đó* — ghi ở dòng **D39** của [`RULES-DOCS.md`](RULES-DOCS.md).

## Đối chiếu T1 — detector với test kiểm nó

> Định nghĩa *detector* và tên test: dòng **T1** của [`RULES-CHUNG.md`](RULES-CHUNG.md); vì sao có luật: [`wiki-core/be/04-testing-strategy.md`](wiki-core/be/04-testing-strategy.md) §3.1. Đừng chép kết quả vào đây.
>
> ```bash
> n=0
> for m in $(grep -oE '^section "F[0-9]+' scripts/fe-gate.sh | cut -c10-); do
>   n=$((n+1))
>   [ -f "scripts/tests/fe-gate-$(printf '%s' "$m" | tr F f).test.sh" ] || echo "THIEU CANARY: $m"
> done
> echo "da xet: $n section"; [ "$n" -gt 0 ] || echo "FAIL: tap dau vao RONG"
> n=0
> for m in $(LC_ALL=C awk -F'|' '/^\| [A-Z][0-9]+ \|/ && !/^\| F[0-9]+ \|/ && /ArchTest/ && /`[A-Z][A-Za-z0-9]*_[A-Za-z0-9_]+`/ && $(NF-1) ~ /✅/ { gsub(/ /,"",$2); print $2 }' docs/RULES-*.md); do
>   n=$((n+1))
>   for v in Catches Ignores; do
>     grep -rqs "void Detector_${m}_${v}" src/BE/Tests --include=*.cs || echo "THIEU Detector_${m}_${v}"
>   done
> done
> echo "da xet: $n ma luat"; [ "$n" -gt 0 ] || echo "FAIL: tap dau vao RONG"
> ```
>
> **PASS: không dòng `THIEU`, `FAIL`.** Phần đầu đối chiếu section của `scripts/fe-gate.sh` với tệp canary cùng mã; phần sau đối chiếu mã luật ép bằng ArchTest có tên, trạng thái `✅`, với cặp `Detector_<mã>_Catches` / `_Ignores`. `da xet: 0` nghĩa là bộ lọc hỏng. Lệnh kiểm test **có mặt** theo tên, không kiểm test **đúng** — phần đó của T12 và review; mã chỉ tả ArchTest bằng lời, không viết tên test ở ô *Ép bằng gì*, thì lọt. Test viết trước 2026-10-06 mang tên ngoài khuôn mã còn hiện ra cho tới khi tệp của nó được sửa tới.

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

→ T1, T6, T12, T14: [`RULES-CHUNG.md`](RULES-CHUNG.md) §8 · T2–T5, T7–T11: [`RULES-BE.md`](RULES-BE.md) §8

## 9. Luật multi-tenant

→ [`RULES-BE.md`](RULES-BE.md) §9

## 10. Danh sách nợ — luật CHƯA có cổng

> Không còn sổ nợ: luật chưa có cổng là dòng `🕳️` trong các bảng trên, liệt kê bằng lệnh ở đầu tệp ([ADR-0114](adr/0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)).
>
> Tiêu đề này giữ lại để mọi trích dẫn *"`RULES.md` §10"* đã viết còn tới được đích — nó không giữ nội dung nào.
