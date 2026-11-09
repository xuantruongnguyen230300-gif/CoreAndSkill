---
name: "feature-kickoff"
description: "Dùng khi bắt đầu một feature nghiệp vụ Module mới: checklist thứ tự gọi agent — phân loại Core/Module, spec qua ba-analyst, design-expert nếu cần màn mới, backend-expert và frontend-expert chia việc nhỏ, test-engineer, core-reviewer khi chạm Core, người dùng chốt bàn giao, tech-writer."
argument-hint: "<tên feature> [mô tả ngắn] - vd 'DonNghiPhep - nhân viên xin nghỉ, quản lý duyệt'"
---

Feature: `$ARGUMENTS` — rỗng thì hỏi tên và mô tả ngắn, không đoán. Tên viết không dấu, dùng chung cho thư mục `spec/<feature>/` và tên module.

> 📖 Luồng chi tiết, điểm dừng và luồng rút gọn: đọc `.claude/README.md` §3

## Checklist — xong bước này mới sang bước sau

1. **Phân loại Core/Module** — mở `docs/kien-truc-core-module.md` §4 đối chiếu, không suy từ tên. Không chắc → 🛑 hỏi. Kết luận Core → dừng checklist, đi luồng Core ở `.claude/README.md` §3.
2. **Spec** — cần `spec/<feature>/business-rules.md`. Chưa có → 🛑 hỏi người dùng: giao `ba-analyst` (skill `ba-story`) hay tự viết. Không bịa nghiệp vụ.
3. **Màn hình** — cần màn hay component chưa có spec trong `docs/Design/` → `design-expert` trước bước 4. Không chắc có cần → 🛑 hỏi.
4. **Thi công** — `architect` đứng trước khi việc mang dấu hiệu ở `docs/adr/README.md` §2 hoặc đưa code mới vào Core. Chia việc 30–40 phút (`.claude/CLAUDE.md` §11). `backend-expert` và `frontend-expert` gọi cùng một lượt khi cả hai phía có việc; một phía có việc thì chỉ gọi phía đó. Lời giao: tên feature, kết luận Module, **đường dẫn** spec và spec màn — không dán nội dung.
5. **`test-engineer`** — tìm ca biên mà AC chưa phủ; giao đường dẫn spec, không kèm tóm tắt việc vừa làm.
6. **`core-reviewer`** — khi báo cáo nào kết thúc bằng dòng `CẦN CORE-REVIEW`; gọi qua skill `core-review`, mỗi lượt một phạm vi. Vòng review và mức: `.claude/CLAUDE.md` §8, §11.
7. 🙋 **Người dùng chốt bàn giao.**
8. **`tech-writer`** — TechDoc, UserGuide, tài liệu UnitTest cho User Story đã chốt, ghi vào `spec/<feature>/ban-giao/`. Lời giao kèm **nguyên văn** mục ca biên cố ý bỏ qua trong báo cáo `test-engineer`.

Giữa các bước, agent không gọi nhau: đọc các dòng `BÀN GIAO:` và `CẦN CORE-REVIEW:` cuối mỗi báo cáo để biết gọi ai tiếp. Kênh gửi thẳng duy nhất là Contract Card qua `SendMessage` giữa `backend-expert` và `frontend-expert` đang chạy song song ở bước 4 — `.claude/README.md` §5.

Finding đòi sửa **nội dung** luật hay quy ước → giao `architect` kèm mã finding; sửa cơ học → phiên chính. Không giao `tech-writer`. Nguồn: `docs/adr/0111-git-danh-sach-cho-phep-cong-docs-chan-mot-lan-architect-truoc-thi-cong-theo-dau-hieu.md` quyết định 1.

## Dừng lại và hỏi người dùng

- Không chắc feature thuộc Core hay Module.
- Chưa có spec, hoặc câu hỏi nghiệp vụ còn mở trong spec đang chặn thi công.
- Không chắc có cần màn hình mới.
- Spec nói ngược một luật trong `docs/` — luật thắng; báo mâu thuẫn, không chọn bên.
- Hết vòng review thứ hai mà còn 🔴.
- Trước bước 8: chốt bàn giao.
- Bước 8, lượt đầu khi chưa có khuôn ở thư mục con `ban-giao/` của `spec/_template/`: `tech-writer` đề xuất khuôn rồi dừng — người dùng duyệt khuôn một lần, rồi giao lại lượt viết tài liệu.
- Cần lệnh git ghi (`.claude/CLAUDE.md` §1).

## Bản tổng hợp cuối

Không làm lại việc của agent nào; câu *"chưa kiểm X"* trong báo cáo của agent phải còn nguyên. Ba thứ bắt buộc có:

- Câu hỏi còn mở trong spec, và câu nào đang chặn.
- Finding 🟠 trở xuống và dòng *Ngoài phạm vi — chỉ 🔴* của `core-reviewer`: kể lại; người dùng chọn có thành issue không.
- Bước nào bị bỏ qua và vì sao — bước bỏ mà không nói sẽ bị đọc như đã chạy.
