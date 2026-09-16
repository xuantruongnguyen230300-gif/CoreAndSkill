---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0030 — Repo chuyển sang giai đoạn 2 khi đủ một danh sách điều kiện kiểm được; `architect` lật nhãn trạng thái trong `docs/`, phiên chính lật `.claude/` và `README.md` gốc

> **Trạng thái:** Đã chấp nhận (2026-09-14)

## Bối cảnh

[`0012-giai-doan-1-chi-docs.md`](0012-giai-doan-1-chi-docs.md) chốt giai đoạn 1 chỉ xây `docs/` và `.claude/`, còn `src/` xây ở giai đoạn 2. Nó không nói **khi nào** chuyển, và **ai** được nói là đã chuyển.

Trong khi đó máy **tự** đổi hành vi theo một tín hiệu duy nhất: thư mục `src/` có tồn tại hay không. Cổng [`../../.claude/check-docs.sh`](../../.claude/check-docs.sh) §11 chỉ xét khi chưa có `src/`; hook `Stop` chỉ bật lời nhắc `core-reviewer` khi `src/` tồn tại ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8). Nghĩa là **lệnh đầu tiên tạo ra `src/` là hành động lật giai đoạn trên thực tế** — bất kể harness, CI hay hợp đồng đã sẵn sàng chưa.

Lúc quyết định, có năm khoảng hở cụ thể sẽ lộ ra đúng lúc bắt đầu dựng:

| Khoảng hở | Bằng chứng (lúc ghi ADR) |
| --- | --- |
| Lớp chặn lệnh cấm có lỗ | Lượt thử ngày 2026-09-14 cho thấy lệnh cấm lọt qua công cụ PowerShell và qua tiền tố `rtk`; `permissions.deny` chỉ khai dạng `Bash(...)` |
| Harness hỏi lại lệnh dựng skeleton | `permissions.allow` trong [`../../.claude/settings.json`](../../.claude/settings.json) không có lệnh tạo solution, project, workspace Angular |
| CI chưa chạy cổng BE/FE | Hai job trong [`../../.github/workflows/docs-gate.yml`](../../.github/workflows/docs-gate.yml) — `backend-gate` và `frontend-gate` — mang `if: false` |
| Hợp đồng chưa chốt, chưa gán pha | Mọi card mang `DRAFT` ở dòng `Status:` của chính card ([`../contracts/README.md`](../contracts/README.md) §3 *Ba trạng thái*); lộ trình BE còn viết *"bốn card hợp đồng hiện có"* ([`../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md) §1 *Năm pha*) trong khi khu hợp đồng có nhiều card hơn |
| Hai định nghĩa chưa biết lúc nào chủ đổi | Ánh xạ mã lỗi → `ErrorType` do card giữ ([`../contracts/README.md`](../contracts/README.md) §2 *Khuôn một card*, mục *Lỗi*); danh sách miễn trừ M4 do [`../database/schema-core.md`](../database/schema-core.md) giữ (mục *Danh sách miễn trừ*). Khi code về, cả hai có một ứng viên chủ mới trong `src/BE` |

## Quyết định

> 1. **Giai đoạn 2 bắt đầu khi MỌI điều kiện ở bảng dưới đạt. Chưa đạt thì không tạo `src/`.**
> 2. **`architect` đối chiếu bảng điều kiện và lật nhãn trạng thái giai đoạn ở các chỗ nằm trong `docs/`; phiên chính lật các chỗ nằm trong `.claude/` và `README.md` gốc** — đúng danh sách ở mục *Chỗ phải lật*, sau khi `architect` báo đủ điều kiện.
> 3. **Hai định nghĩa chuyển chủ sang code theo mục *Chuyển chủ sang code*** — khi tệp đích tồn tại, không cùng lúc lật giai đoạn.

### Điều kiện chuyển giai đoạn

| # | Điều kiện | Cách kiểm — tiêu chí PASS | Ai kiểm |
| --- | --- | --- | --- |
| 1 | **Quyết định đợt 2026-09-14 đã vào tài liệu** | (a) Mười ADR từ 0023 tới 0032 tồn tại và mang `Đã chấp nhận` — lệnh 1 dưới bảng. (b) [`../RULES.md`](../RULES.md) có dòng cho S11, R9, E9, E10, A14, M13, F18–F24 — lệnh 2 in `13`. (c) Không còn mã hướng khoá giữ chỗ — lệnh 3 in rỗng | `architect` |
| 2 | **Lớp chặn lệnh cấm đã vá và đã thử bằng lệnh thật** | (a) `permissions.deny` có đủ ba dạng cho mỗi mục, và mục §1 của cổng tài liệu kiểm điều đó rồi xanh. (b) Hook `PreToolUse` được gắn trong `settings.json`. (c) Một phiên thử gọi một lệnh cấm theo ba đường — tiền tố `rtk`, công cụ PowerShell, và sau `&&` — và **cả ba** bị chặn | Phiên chính thử; **người dùng xác nhận** |
| 3 | **Hook review Core đã gắn và đã thử bằng payload thật** | Hook `SubagentStop` ghi dấu review; hook `Stop` chặn lượt khi có sửa Core mới hơn dấu review. Chặn cứng chỉ gắn **sau** khi đã chạy thử với payload thật và thấy đúng hai nhánh: có dấu → cho qua, không dấu → chặn | Phiên chính thử; **người dùng xác nhận** |
| 4 | **CI chạy cổng BE, cổng FE và quét bí mật** | `grep -n 'if: false' .github/workflows/docs-gate.yml` in rỗng; job BE và FE bật theo điều kiện có `src/BE` / `src/FE`; job gitleaks có mặt và **đã chạy xanh ít nhất một lần** trên một PR | `architect` đọc workflow; **người dùng xác nhận** lượt chạy |
| 5 | **Mọi card hợp đồng đã gán pha** | Lệnh 4 dưới bảng in rỗng. Phép kiểm này **gần đúng**: nó chứng minh tên card được nhắc trong file pha, không chứng minh dòng đó là dòng gán pha — người kiểm mở từng dòng khớp để xác nhận | `architect` |
| 6 | **`auth.md` và `profile.md` ở `AGREED`** | `grep -n '^\*\*Status:\*\*' docs/contracts/auth.md docs/contracts/profile.md` — mọi card thuộc v1 mang `AGREED`. Chuyển trạng thái theo đúng thủ tục ở [`../contracts/README.md`](../contracts/README.md) §3 | `backend-expert` và `frontend-expert` **cùng** soát nội dung; `architect` kiểm dòng `Status:` |
| 7 | **Cổng tài liệu xanh** | `bash .claude/check-docs.sh` thoát `0` | `architect` |
| 8 | **Harness không hỏi lại lệnh dựng skeleton** | `permissions.allow` có mục cho `dotnet new`, `dotnet sln`, `dotnet add`, `dotnet restore`, `ng new`, `ng generate` **ở đúng dạng lệnh sẽ được gọi**; thử một lệnh dựng trong thư mục tạm **ngoài repo** → harness không hỏi | Phiên chính thử; **người dùng xác nhận** |

```bash
# 1 — mười ADR của đợt, kèm dòng trạng thái
grep -H '^> \*\*Trạng thái:\*\*' docs/adr/002[3-9]-*.md docs/adr/003[0-2]-*.md

# 2 — PASS khi in 13
grep -oE '^\| (S11|R9|E9|E10|A14|M13|F18|F19|F20|F21|F22|F23|F24) \|' docs/RULES.md | sort -u | wc -l

# 3 — PASS khi rỗng. Mẫu viết có ngoặc vuông để chính dòng này không tự khớp
grep -rn 'K-M[O]I' docs .claude --include='*.md'

# 4 — PASS khi rỗng
for f in docs/contracts/*.md; do
  b=$(basename "$f"); [ "$b" = README.md ] && continue
  grep -q "$b" docs/wiki-core/be/trien-khai/*.md || echo "$b chưa gán pha"
done
```

### Chỗ phải lật

Chỉ lật khi **cả tám** điều kiện đạt, và cả hai người lật **trong cùng đợt thay đổi** với lần tạo `src/` đầu tiên. Lý do: máy đổi hành vi theo sự tồn tại của `src/`, tài liệu đổi theo nhãn — tách hai việc ra thì trong khoảng giữa, hoặc tài liệu nói sai, hoặc máy chạy sai.

| Chỗ | Lật gì | Ai lật |
| --- | --- | --- |
| [`../README.md`](../README.md) — mục *Trạng thái repo* | Câu *"đang ở giai đoạn 1 … chưa có `src/`"* và hệ quả của nó | `architect` |
| [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §9 | Thay bảng bằng bảng *"có thật hôm nay → sẽ thành"* theo [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 | `architect` |
| [`../../README.md`](../../README.md) — mục *Trạng thái* | Cùng nội dung với `docs/README.md`, và dòng `src/` ở bảng ba tài sản | Phiên chính |
| [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 và §8 | Câu về giai đoạn 1 ở §4; ở §8 là **bảng cổng theo thứ vừa sửa** cùng đoạn ngay dưới nó nói cổng BE và cổng FE thuộc giai đoạn 2 | Phiên chính |
| [`../../.claude/README.md`](../../.claude/README.md) | Dòng luồng rút gọn cho giai đoạn chưa có `src/` ở §3; mục *Bộ agent này chưa chạy trên code thật* ở §8 | Phiên chính |

Các chỗ còn lại nhắc giai đoạn 1 **không** liệt kê tay ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6). Tìm bằng lệnh, mở từng file, quyết lật hay giữ — một câu nói về giai đoạn 1 như một mốc đã qua thì giữ được. Chia người lật theo cùng quy tắc với bảng: file trong `docs/` do `architect`, file trong `.claude/` và `README.md` gốc do phiên chính:

```bash
grep -rln 'giai đoạn 1\|Giai đoạn 1' docs .claude README.md --include='*.md' \
  | grep -v '^docs/adr/\|^docs/audit/\|^docs/00-overview/'
```

**Không lật:**

- [`../RULES.md`](../RULES.md) — mỗi dòng `📐` chuyển trạng thái khi cổng **của chính nó** chạy, không chuyển hàng loạt ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §9).
- [`0012-giai-doan-1-chi-docs.md`](0012-giai-doan-1-chi-docs.md) — nó quyết định giai đoạn 1 **là gì**; giai đoạn 1 kết thúc không lật quyết định đó.
- `verified:` của từng file — chỉ đổi khi có người mở source ra đối chiếu toàn file ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §9).

### Chuyển chủ sang code

| Định nghĩa | Chủ ở giai đoạn 1 | Chủ khi code về | Lúc chuyển | Cách kiểm | Ai |
| --- | --- | --- | --- | --- | --- |
| Danh sách mã lỗi của một feature | Card hợp đồng của endpoint — ánh xạ mã → `ErrorType` | Tệp catalog `*Errors.cs` của feature đó ([`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §7.1) | Khi tệp `*Errors.cs` của feature **lần đầu** vào `src/BE` | Card trỏ về tệp catalog thay vì tự khai là chủ; sổ chủ quyền ghi chủ mới | `backend-expert` viết tệp; `architect` sửa file luật |
| Danh sách miễn trừ M4 | Bảng *Danh sách miễn trừ* ở [`../database/schema-core.md`](../database/schema-core.md) | Dictionary C# trong `CoreAndSkill.ArchTests` — khoá `schema.table` lấy từ EF model, giá trị là lý do | Khi ArchTest `EveryBusinessEntity_IsTenantScoped_OrExempt` **lần đầu** chạy | Bảng trong `schema-core.md` rút còn dòng trỏ đường; ArchTest đọc dictionary, không đọc markdown | `test-engineer` hoặc `backend-expert` viết; `architect` sửa file luật |

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Chuyển giai đoạn khi có người tạo `src/`, không điều kiện

**Được:** không có gì phải kiểm; bắt đầu code ngay khi muốn.

**Vì sao loại:** máy lật trước người. Mỗi khoảng hở ở mục *Bối cảnh* lộ ra đúng lúc đang dựng, và khoảng hở đầu tiên — lệnh cấm lọt qua — là loại gây hại **không hoàn tác được**.

### Phương án B — Chuyển theo một ngày cố định

**Được:** lịch rõ ràng, dễ báo tiến độ.

**Vì sao loại:** ngày không kiểm được thứ gì đã sẵn sàng. Tới ngày mà harness chưa vá thì hoặc lùi ngày — và ngày mất nghĩa — hoặc dựng trên một harness hở.

### Phương án C — Điều kiện viết bằng câu văn, kiểu "khi tài liệu đủ tốt"

**Vì sao loại:** không ai kiểm được, nên nó sẽ được coi là đạt vào đúng lúc có người muốn bắt đầu. Đúng khuôn [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 cấm: đóng một việc bằng một câu tự nhận.

### Phương án D — Bất kỳ agent nào thấy đủ điều kiện đều được lật nhãn

**Vì sao loại:** nhiều người lật thì nhãn lật **từng phần**, ở từng file, vào những lúc khác nhau — và không ai chịu trách nhiệm cho câu *"repo đã sang giai đoạn 2"*. Quyết định 2 giữ một người trả lời câu đó — `architect`, người đối chiếu bảng — và mỗi chỗ phải lật có đúng một người lật, khai sẵn ở bảng.

### Phương án F — Mở rộng phạm vi ghi của `architect` ra `.claude/` và `README.md` gốc để một người lật hết

**Được:** một người, một lượt, không có khoảng giữa hai người lật.

**Vì sao loại:** phạm vi ghi của vai `architect` là `docs/adr/` và file luật, kiến trúc trong `docs/`; `.claude/` là quy trình và cấu hình harness, do phiên chính sửa. Nới ranh giới vai vĩnh viễn để phục vụ một việc xảy ra đúng một lần là đổi một ranh giới đang có lý do lấy một tiện lợi dùng một lần.

### Phương án E — Chuyển chủ hai định nghĩa sang code ngay lúc lật giai đoạn

**Vì sao loại:** lúc lật chưa có tệp C# nào để làm chủ. Chuyển chủ trước khi đích tồn tại là tạo ra một khoảng thời gian định nghĩa **không có chủ nào**.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Giai đoạn 1 kéo dài thêm, và một phần lịch nằm ngoài repo** | Ba điều kiện cần người dùng xác nhận và một điều kiện cần một lượt CI thật. Lịch chuyển giai đoạn phụ thuộc người, không chỉ phụ thuộc tài liệu |
| **Bảng điều kiện là ảnh chụp ngày 2026-09-14** | Điều kiện phát sinh sau đó không tự vào bảng. Thêm điều kiện là viết ADR mới, không sửa bảng này |
| **Hai người lật, nên có thể lật lệch nhau** | `architect` lật xong phần `docs/` mà phiên chính chưa lật `.claude/` — hoặc ngược lại — thì trong khoảng đó `docs/` nói giai đoạn 2 còn `.claude/CLAUDE.md` vẫn tả cổng của giai đoạn 1. Hàng rào duy nhất là quy tắc *cùng đợt thay đổi*; không cổng máy nào so nhãn giữa hai khu |
| **`architect` vẫn là điểm nghẽn đơn cho câu *"đủ điều kiện chưa"*** | Phiên chính không lật trước khi `architect` báo đạt. `architect` không chạy được thì không ai lật |
| **Không cổng máy nào chặn việc tạo `src/` trước khi đủ điều kiện** | Quyết định 1 được giữ bằng người đọc ADR này. Một lệnh dựng skeleton chạy sớm vẫn chạy được |
| **Kiểm điều kiện 5 là gần đúng** | Nhắc tên card trong file pha không đồng nghĩa với gán pha |
| **Chuyển chủ sang code đòi mở rộng cổng chủ quyền** | Cổng §15 chỉ nhận file chủ nằm trong `docs/`. Một chủ nằm trong `src/BE` hôm nay không đăng ký được vào sổ; phần mở rộng đó chưa có |

### Tích cực

- Câu hỏi *"đã sang giai đoạn 2 được chưa"* có câu trả lời bằng lệnh và bằng tên người kiểm.
- Khoảng hở của harness đóng **trước** khi có code, không phải giữa lúc dựng.
- Một người trả lời câu *"đã đủ điều kiện chưa"*, và mỗi chỗ phải lật có đúng một người lật nằm trong phạm vi ghi của chính vai đó.
- Người thi công biết chủ của danh sách mã lỗi và danh sách miễn trừ M4 ở mọi thời điểm.

### Điều kiện lật quyết định

1. **Một điều kiện trong bảng hoá ra không kiểm được như đã ghi** — lệnh không phân biệt được đạt với không đạt. Viết ADR mới thay bảng.
2. **Người dùng chốt bỏ qua một điều kiện để bắt đầu sớm.** Ghi bằng ADR mới nói điều kiện nào bị bỏ, vì sao, và cái giá chấp nhận — không sửa bảng này, không lặng lẽ tạo `src/`.

## Liên quan

- [`0012-giai-doan-1-chi-docs.md`](0012-giai-doan-1-chi-docs.md) — giai đoạn 1 là gì
- [`0011-ci-github-actions.md`](0011-ci-github-actions.md) — vì sao CI phải chạy đủ cổng
- [`0024-ba-muc-khai-bao-phan-quyen-endpoint.md`](0024-ba-muc-khai-bao-phan-quyen-endpoint.md) · [`0027-errortype-unauthorized.md`](0027-errortype-unauthorized.md) — hai luật mới cần `src/` để có cổng
- [`../contracts/README.md`](../contracts/README.md) §3 — thủ tục `DRAFT` → `AGREED`
- [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4, §8 — nhãn trạng thái và cổng theo giai đoạn
