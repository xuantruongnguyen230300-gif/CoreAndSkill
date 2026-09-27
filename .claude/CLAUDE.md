# CLAUDE.md — Luật toàn repo CoreAndSkill

## 1. Git — đọc được, GHI thì không

Lệnh git ghi là việc của người dùng, với mọi skill, subagent.

| | Lệnh | Ai chạy |
| --- | --- | --- |
| ✅ Được phép | `git status`, `git diff`, `git log`, `git show`, `git blame` | Agent |
| 🛑 **CẤM** | `add`, `commit`, `push`, `checkout`, `switch`, `restore`, `merge`, `rebase`, `reset`, `stash`, `branch`, `clean`, `cherry-pick`, `revert`, `rm`, `mv`, `am`, `apply`, `pull`, `tag`, `config`, `worktree`, `submodule`, `remote` | **Chỉ người dùng** |

`deny` trong [`settings.json`](settings.json) chặn các lệnh trên, và chặn thêm `dotnet ef database update`, `dotnet ef database drop`, `dotnet ef migrations remove`, `npm publish`, `pnpm publish`, `dotnet nuget push`, `docker push`.

Các dạng ghi của `remote` bị chặn riêng: `git remote add`, `git remote rm`, `git remote remove`, `git remote set-url`, `git remote rename`, `git remote prune`, `git remote set-head`, `git remote set-branches`, `git remote update`; `git remote -v` được chạy.

> Bảng cấm và hai dòng trên là đầu vào cổng §1.

- Mục cấm lệnh ở `deny` đủ ba dạng `Bash(<lệnh>:*)`, `Bash(rtk <lệnh>:*)`, `PowerShell(<lệnh>:*)`; mục chặn công cụ ghi vào thư mục trạng thái khai theo tên công cụ.
- Lớp hai: hook `PreToolUse` khớp từng đoạn lệnh với chính `deny`; bị chặn thì không viết lại lệnh cho lọt.
- Cần lệnh git ghi → dừng, nói rõ lệnh cho người dùng tự chạy; không "xin phép rồi tự chạy".

> 📖 Đường vòng lọt cả hai lớp: nợ C4 ở `docs/DEBT.md`

## 2. Ranh giới `.claude` ↔ `docs` — phép thử kiểm được bằng máy

`.claude/`: quy trình, ràng buộc, cấu hình harness (không tri thức, code mẫu). `docs/`: quy tắc (không nghiệp vụ feature). `spec/<feature>/`: nghiệp vụ (không quy tắc). Agent, skill luôn tuân `docs/`, cả khi làm `spec/`.

### Phép thử — trả lời được bằng có/không

`.claude/` không được chứa câu nào có thể thành SAI khi code đổi, nên cấm code block ngôn ngữ lập trình. Được phép:

```bash
grep -n 'case "$lang" in' .claude/check-docs.sh
```

### Ngoại lệ duy nhất — tài liệu VỀ chính hệ thống agent

Tài liệu về chính bộ agent/skill thuộc `.claude/`: [`README.md`](README.md).

## 3. Chiều cập nhật — nội dung vào `docs`, CHỈ đường dẫn vào `.claude`

Tra trước khi mở file:

- Chốt kiến trúc; sửa quy ước, code mẫu, token; ghi hiện trạng; đóng việc tồn đọng → chỉ sửa `docs/`.
- Sửa luật viết sai → `docs/` chỉ ghi bản mới, bài học có cơ chế hỏng vào `docs/audit/`; không kể lại bản trước (D24).
- File `docs/` đổi tên, chỗ, bị xoá, tách → `.claude/` chỉ sửa đường dẫn.
- Thêm file chủ ở `docs/` → `.claude/` chỉ thêm MỘT dòng đường dẫn vào agent cần nó, đúng phần *Bộ luật*/*Tra cứu* (D36); tới được qua mục lục thì thôi.
- Thêm/bỏ agent, skill; đổi quy trình, bàn giao, ràng buộc thi hành → sửa `.claude/`.
- Không chép nội dung `docs/` sang `.claude/`; trỏ đường: một dòng `> 📖 <chủ đề>: đọc <đường dẫn>`, không tóm tắt.

### Vì sao — bốn lý do đã trả giá thật ở dự án tiền nhiệm

> 📖 Lý do của mọi luật: `.claude/README.md` mục *Vì sao*

## 4. Tài liệu phải mô tả thứ CÓ THẬT — và phải dán nhãn trạng thái

Tuyên bố hiện trạng trong `docs/` phải mang một trong ba nhãn (không nhãn = chưa xác minh):

- `✅ CÓ THẬT`: đã đối chiếu source; kèm ngày + neo (§9).
- `🚧 ĐÃ CHỐT — ĐANG THI CÔNG`: đã quyết, code chưa về; kèm bảng *"có thật hôm nay → sẽ thành"* có neo (§9); chỉ khi chưa mục nào đối chiếu được mới thay bằng một câu khai thẳng.
- `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG`: mới dự kiến.

Nhãn, `verified` chỉ lật khi có người mở đúng file đó ra đối chiếu, từng file một. `chua-doi-chieu`, `📐` là giá trị trung thực, không phải nợ. Cấm tuyệt đối sửa mô tả cho khớp mong muốn rồi đánh dấu xong.

> 📖 Hiện trạng `src/`, job CI nào đã chạy: `docs/README.md` mục *Trạng thái repo*. Chuyển giai đoạn 2, lật nhãn: `docs/adr/0030-dieu-kien-chuyen-giai-doan-2.md`

## 5. Một nội dung — một nguồn đối chiếu duy nhất

- Mỗi nội dung một file giữ định nghĩa, file khác chỉ trỏ tới; hai file cùng tả một thứ → một làm chủ, file kia còn một dòng trỏ đường. Không "giữ cả hai cho chắc".
- 🛑 **Sửa bằng cách gỡ một bản, KHÔNG bằng cách đồng bộ hai bản.**
- Định nghĩa mới: tra sổ `docs/OWNERSHIP.md` trước; có chủ thì trỏ đường, chưa thì viết rồi thêm dòng vào sổ.
- Tài liệu chết cần giữ: không xoá, không để nguyên; đầu file dán banner "TÀI LIỆU LỊCH SỬ" + bảng *"Trong file này → Thực tế hiện nay"* + trỏ nguồn sống, `kind: lich-su`.

## 6. Không chép vào tài liệu thứ đếm được bằng lệnh

Cấm chép danh sách file vi phạm, số lượng test/gate/thành phần/project, danh sách "còn N chỗ hardcode"; thay bằng lệnh + tiêu chí PASS.

## 7. Giao diện người dùng: `docs/Design/` là nguồn DUY NHẤT

Mọi tham chiếu giao diện lấy từ `docs/Design/`; không prototype HTML song song, không nguồn UI thứ hai. Luật riêng: `docs/Design/CLAUDE.md`.

## 8. Trước khi coi một việc là xong

- Sửa `.md` ở `docs/`, `.claude/`, `spec/` → `bash .claude/check-docs.sh`; sửa `.claude/hooks/` (không `grep -P`, tự ép locale) hay khối `hooks` → tự chạy `bash .claude/hooks/tests/run-tests.sh`. Chạy cổng qua công cụ Bash; lệnh cổng thiếu trong `allow` thì thêm dạng `Bash(…)`.
- Job CI cổng BE/FE chưa từng chạy (📖 §4) thì đừng coi là đang canh, đừng trích kết quả.

### Cổng chạy tự động — không phụ thuộc ai nhớ

Hook `Stop` bỏ qua cả lượt kiểm khi còn agent nền; không còn thì chạy cổng tài liệu nếu có tệp mới hơn lần xanh cuối, đỏ → chặn tới khi xanh. Sửa `src/`: chỉ nhắc build/test.

> 📖 Chạm Core là gì: khối `core-paths` ở `docs/kien-truc-core-module.md`

### `core-reviewer` sau khi chạm Core — ba lớp

1. Agent thi công chạm Core ghi cuối báo cáo `CẦN CORE-REVIEW: BE` và/hoặc `CẦN CORE-REVIEW: FE`, không tự gọi; phiên chính hoặc `/feature-kickoff` gọi `core-reviewer`, mỗi lượt một phạm vi, chỉ truyền phạm vi.
2. Hook `SubagentStop` ghi dấu `core-reviewed` khi lượt `core-reviewer` kết thúc.
3. `Stop` chặn mỗi lượt khi có `src/`, phiên đã chạm Core (cả qua shell) và file Core sửa không cũ hơn hẳn dấu review (hoặc chưa có dấu); chỉ chặn cứng khi có gắn `SubagentStop`, thiếu thì nhắc một lần.

- Lối thoát duy nhất: lượt `core-reviewer` mới hơn file đã sửa; bỏ review là việc người dùng quyết (xoá dấu `core-touched`). Thư mục `.state` của `.claude` là trạng thái chạy, không phải bằng chứng; agent không tạo, sửa, xoá gì trong đó.
- 🛑 **Lớp chặn `core-reviewer` bật theo sự tồn tại của `src/`, hook kiểm lúc chạy** (không phải đang tắt). Cổng tài liệu không phụ thuộc giai đoạn.
- Không đi vòng cổng: báo sai → sửa phép dò bằng dấu hiệu máy đọc được, ghi lý do; không nới luật cho cổng xanh.

Đếm số mục cổng (§6):

```bash
grep -c '^section ' .claude/check-docs.sh
```

### ⚠️ PASS không có nghĩa là tài liệu ĐÚNG

Cổng không đọc hiểu: văn xuôi tả thứ không có, sơ đồ chép sai, ngày đúng nội dung sai đều lọt; đối chiếu nội dung là việc của `core-reviewer`, người đọc.

## 9. Ba khoá phân loại bắt buộc cho mọi file `docs/` và `spec/`

- `kind` (`luat`, `tham-chieu`, `quyet-dinh`, `lich-su`): code có phải tuân file không? `tham-chieu` quan trọng nhất, dễ sót nhất.
- `scope` (`core`, `du-an`): file có theo Core sang dự án khác không?
- `verified` (`YYYY-MM-DD`, `chua-doi-chieu`, `khong-ap-dung`): lần cuối đối chiếu cả file với source; lật theo §4.
- 🛑 **Không tự khai được `khong-ap-dung`**: cổng chỉ nhận khi file mang `kind: tham-chieu`, `kind: lich-su` hoặc `status:` chứa `not built`.
- Khẳng định hiện trạng phải neo: tới `docs/` bằng `file:dòng`; tới `src/` bằng đường dẫn + chuỗi tìm được trong tệp, không số dòng.

> 📖 Vì sao hai dạng neo: `docs/adr/0046-neo-trich-dan-vao-src-bang-chuoi-tim-duoc.md`

## 10. Ngôn ngữ

`docs/`, `.claude/` viết tiếng Việt; định danh kỹ thuật giữ tiếng Anh.

## 11. Điều phối agent

- Mỗi lượt giao một việc nhỏ (30–40 phút), không cả một pha; việc lớn thì chia.
- Chờ agent nền: kết thúc lượt, đợi thông báo; không ngủ chờ, không hỏi thăm theo vòng.
- `core-reviewer` mặc định soát phần đổi từ lượt review trước + code nó gọi trực tiếp; toàn bộ khi người dùng yêu cầu hoặc trước khi chốt giai đoạn (gửi `Phạm vi: BE, toàn bộ.` hoặc FE).
- Mỗi phạm vi tối đa 2 vòng review → sửa: 🔴 sửa rồi review lại; 🟠 trở xuống ghi nợ `docs/DEBT.md` kèm người sửa, không mở vòng mới; hết vòng 2 còn 🔴 → dừng, hỏi người dùng sửa tiếp, ghi nợ hay chấp nhận.
- Model ghim ở dòng `model:` của agent: soát/viết luật → Opus; code, test, spec, tài liệu bàn giao → Sonnet. Lượt khó: phiên chính nâng model riêng lượt đó qua tham số `model`.
