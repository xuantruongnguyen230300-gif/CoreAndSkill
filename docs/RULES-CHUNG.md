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
| T1 | **Mọi detector phải có test kiểm chính nó** — một ca bắt vi phạm thật, một ca bỏ qua ca hợp lệ trông giống vi phạm. *Detector* nhận theo **mã luật**: bộ dò máy chạy cho một mã — ArchTest, section `scripts/fe-gate.sh`, rule ESLint mà ô *Ép bằng gì* của mã đó nêu; lớp dưới `Support/` không tự là detector. Test kiểm nó: `Detector_<mã>_Catches_*` cùng `Detector_<mã>_Ignores_*`; canary `scripts/tests/fe-gate-<mã viết thường>.test.sh`; rule ESLint: canary nêu ở ô *Ép bằng gì* (người dùng chốt 2026-10-06) | MUST | Lệnh ở [`RULES.md`](RULES.md) mục *Đối chiếu T1*, chạy tay — chưa có cổng; test cũ mang tên ngoài khuôn mã đổi tên khi tệp được sửa tới. Rule ESLint: review | 🕳️ |
| T6 | **Mọi bộ dò phải khẳng định tập đầu vào khác rỗng trước khi khẳng định "không vi phạm"**. Bộ dò đọc **đúng một tệp nêu tên cố định** và đỏ khi tệp vắng (lời đọc ném lỗi) — như A1, R5 — là đã thoả (người dùng chốt 2026-10-06) | MUST | `check-docs.sh` tự áp cho chính nó — đếm bằng lệnh, không chép danh sách (xem dưới bảng). Phía BE: mỗi ArchTest duyệt một tập hợp mang meta-test T6 của nó ([`wiki-core/be/04-testing-strategy.md`](wiki-core/be/04-testing-strategy.md) §3.4); chưa cổng nào kiểm mọi detector đều có — review | ✅ một phần |
| T12 | **Mọi ca kỳ vọng XANH trong canary của một cổng đi kèm ít nhất một ca kỳ vọng ĐỎ đi qua cùng cơ chế dò** — cùng section, cùng thư mục đầu vào, khác đúng một vi phạm được cắm. *Canary* là test chạy một bộ dò trên đầu vào dựng sẵn rồi khẳng định phán quyết của nó. Ca xanh đứng một mình không phân biệt được *xanh vì được phép* với *xanh vì không kiểm gì* — T6 bắt ca đó ở mức tập đầu vào, luật này ở mức từng khẳng định. Áp **ngay** cho canary viết mới hoặc đang sửa; canary cũ rà dần | MUST | Review. Canary dưới `core-paths` (`scripts/tests/`, `src/BE/Tests/CoreAndSkill.ArchTests/`) đổi thì buộc một lượt `core-reviewer` soát cặp xanh–đỏ; canary ngoài `core-paths` (`.claude/hooks/tests/`, canary nội tại của `.claude/check-docs.sh`) chỉ được soát khi có người đọc. Không cổng máy nào ghép được hai ca. Canary viết trước 2026-09-24 rà dần: tệp rà xong mang dấu `T12:cap-xanh-do` trong chú thích — lời tự khai, `core-reviewer` kiểm khi tệp được sửa; phần còn lại: `grep -L T12:cap-xanh-do scripts/tests/*.test.sh .claude/hooks/tests/*.sh .claude/check-docs.sh; grep -rl "void Detector_" src/BE/Tests --include=*.cs \| xargs grep -L T12:cap-xanh-do` | 🕳️ |
| T14 | **Cụm cấu hình máy dev của BE và FE khớp nhau và khớp giá trị ghim**: cổng API ↔ `apiBaseUrl`, `tenantCode` ↔ `Core:Bootstrap:FirstTenantCode`; origin mặc định của `ng serve` — `ssl` bật, cổng ghim — không đối chiếu với `Core:Auth:AllowedOrigins`, vì BE dev nhận mọi cổng `https://localhost` ([ADR-0129](adr/0129-development-nhan-moi-cong-https-localhost-mot-ham-origin-chung-cho-cors-va-lop-1-go-kiem-cong-prestart.md)). Đổi cổng có chủ ý thì đổi luôn hằng ghim của cổng trong cùng thay đổi ([`quy-uoc/chay-may-dev.md`](quy-uoc/chay-may-dev.md) §5; người dùng chốt 2026-10-06, Q41) | MUST | `scripts/dev-config-check.sh` — job CI `dev-config`; canary `scripts/tests/dev-config-check.test.sh`. Cổng chỉ đọc tệp: cổng mà `ng serve` thật sự nghe lúc chạy thì không thấy — sau ADR-0129 không cần thấy. Cổng không đọc `AllowedOrigins`; canary có ca xanh canh việc thêm lại phép đối chiếu đó | ⏸️ — chạy tay trên máy dev 2026-11-09, sau ADR-0129: cổng và canary xanh; job `dev-config` chưa chạy trên CI lần nào: thay đổi chưa push |

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
> **Giới hạn đã biết.** (1) Lời gọi `bad`/`warn` phải trong **ba dòng** sau điều kiện, xa hơn thì bị báo thiếu (đỏ sai). (2) Lệnh kiểm chốt **có mặt**, không kiểm chốt **đúng**. (3) Dòng `BO DO HONG` là canary của chính lệnh: nó in ra thì mọi dòng `KHONG CO CHOT` là giả. (4) Mẫu `[[]![ ]+-[edf][ ]` không khớp `if [ ! -f x ]` — có dấu cách sau `[` — nên báo thiếu chốt oan, theo chiều an toàn.
