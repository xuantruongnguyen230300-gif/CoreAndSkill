---
kind: luat
scope: core
verified: chua-doi-chieu
---

# RULES-CHUNG — luật kiểm thử chung cho Backend và Frontend

> Mục lục, ký hiệu cột *Trạng thái* và ba mức: [`RULES.md`](RULES.md). Các luật T chỉ thuộc Backend: [`RULES-BE.md`](RULES-BE.md) §8.

## 8. Luật kiểm thử

📖 Chi tiết: [`wiki-core/be/04-testing-strategy.md`](wiki-core/be/04-testing-strategy.md) · [`wiki-core/fe/06-testing-strategy.md`](wiki-core/fe/06-testing-strategy.md)

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| T6 | **Mọi bộ dò phải khẳng định tập đầu vào khác rỗng trước khi khẳng định "không vi phạm"** | MUST | `check-docs.sh` tự áp cho chính nó — đếm bằng lệnh, không chép danh sách (xem dưới bảng); phía BE xem [nợ](DEBT.md) | ✅ một phần |
| T12 | **Mọi ca kỳ vọng XANH trong canary của một cổng đi kèm ít nhất một ca kỳ vọng ĐỎ đi qua cùng cơ chế dò** — cùng section, cùng thư mục đầu vào, khác đúng một vi phạm được cắm. *Canary* là test chạy một bộ dò trên đầu vào dựng sẵn rồi khẳng định phán quyết của nó. Ca xanh đứng một mình không phân biệt được *xanh vì được phép* với *xanh vì không kiểm gì* — T6 bắt ca đó ở mức tập đầu vào, luật này ở mức từng khẳng định. Áp **ngay** cho canary viết mới hoặc đang sửa; canary cũ rà dần | MUST | Review. Canary dưới `core-paths` (`scripts/tests/`, `src/BE/Tests/CoreAndSkill.ArchTests/`) đổi thì buộc một lượt `core-reviewer` soát cặp xanh–đỏ; canary ngoài `core-paths` (`.claude/hooks/tests/`, canary nội tại của `.claude/check-docs.sh`) chỉ được soát khi có người đọc. Không cổng máy nào ghép được hai ca — xem [nợ](DEBT.md) | 🕳️ nợ |

> **Kiểm mục nào của cổng đang tự áp T6** — đừng chép danh sách vào đây (§6 của [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md)):
>
> ```bash
> awk '/^[[:space:]]*(if|elif)[[:space:]]+!/ { armed=3; guards++ }
>      /^[[:space:]]*(if|elif)[[:space:]].*(-eq[ ]+0|-lt[ ]|-le[ ]+0|-z[ ]|!=[ ]+"?[0-9]|[[]![ ]+-[edf][ ]).*then/ { armed=3; guards++ }
>      /^[[:space:]]*0[)]/ { armed=1; guards++ }
>      /^section "/ { if (n!="" && !g) print "KHONG CO CHOT: " n; n=$0; sub(/^section "/,"",n); sub(/".*/,"",n); g=0; armed=0; next }
>      armed>0 && /(^|[^a-zA-Z_])(bad|warn)[ ]+"/ { if (n!="") g=1 }
>      { if (armed>0) armed-- }
>      END { if (n!="" && !g) print "KHONG CO CHOT: " n
>            if (guards==0) print "BO DO HONG: khong khop duoc dieu kien rong/bang-0 nao trong ca script" }' .claude/check-docs.sh
> ```
>
> **PASS khi lệnh này không in dòng nào.** Mỗi mục cổng phải làm được một trong ba việc: đếm đầu vào và FAIL khi bằng 0 · chạy canary chứng minh phép dò còn sống · khai báo tường minh bằng `NOTE` rằng nó không kiểm gì.
>
> **Lệnh neo vào HÀNH VI, không vào chữ của thông điệp:** mỗi mục phải có lời gọi `bad`/`warn` đến được từ một `if`/`elif` đúng khi đầu vào rỗng — cú pháp điều kiện shell là **tập đóng** (`-eq 0` · `-lt` · `-z` · `[ ! -e/-d/-f ]` · `if !` · nhánh `case` bằng 0), câu chữ thì không. Lệnh so **đúng tập** mục, không so số lần khớp với số mục ([`audit/2026-09-12-cong-neo-vao-muc-thay-vi-vai-tro-cua-chuoi.md`](audit/2026-09-12-cong-neo-vao-muc-thay-vi-vai-tro-cua-chuoi.md)).
>
> **Giới hạn đã biết.** (1) Lời gọi `bad`/`warn` phải trong **ba dòng** sau điều kiện, xa hơn thì bị báo thiếu (đỏ sai). (2) Lệnh kiểm chốt **có mặt**, không kiểm chốt **đúng**. (3) Dòng `BO DO HONG` là canary của chính lệnh: nó in ra thì mọi dòng `KHONG CO CHOT` là giả.
