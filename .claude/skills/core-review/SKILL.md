---
name: "core-review"
description: "Giao một lượt core-reviewer đúng khuôn ba trường — phạm vi BE hoặc FE, phần đổi hay toàn bộ, vòng n/2 — rồi xử lý báo cáo: 🔴 giao sửa và review vòng sau, 🟠 trở xuống chỉ nằm trong báo cáo, hết vòng 2 còn 🔴 thì hỏi người dùng. Dùng khi một báo cáo kết thúc bằng dòng CẦN CORE-REVIEW, khi hook Stop chặn vì phiên chạm Core chưa review, hoặc khi người dùng yêu cầu soát Core."
argument-hint: "<BE|FE> [toàn bộ] - vd 'BE' hoặc 'FE toàn bộ'"
---

Phạm vi: `$ARGUMENTS` — không nêu `BE` hay `FE` thì hỏi, không tự chọn. Cần cả hai phía → hai lượt riêng, mỗi lượt một lần gọi `Agent`.

> 📖 Ba lớp chặn, phần đổi hay toàn bộ, giới hạn vòng: đọc `.claude/CLAUDE.md` §8, §11
> 📖 Mức finding và khuôn báo cáo: đọc `docs/quy-uoc/tieu-chi-review.md` §2, §6

## 1. Giao việc

Gọi `core-reviewer` qua `Agent` ngay trong phiên chính — skill này không chạy `context: fork`, vì phiên chính phải nhận báo cáo để giao sửa. Lời giao theo khuôn ba trường ở `.claude/CLAUDE.md` §8 lớp 1; điền sẵn, ví dụ: `Phạm vi: BE, phần đổi. Vòng: 1/2.`

- `toàn bộ` chỉ khi `.claude/CLAUDE.md` §11 cho; còn lại `phần đổi`.
- Vòng 1 là lượt đầu sau đợt thi công; lượt sau khi sửa 🔴 của vòng 1 là vòng 2.
- 🛑 Ngoài ba trường không kèm gì — kể cả danh sách tệp hay chủ đề cần soát: agent tự xác định phần đổi và tự chọn tệp theo bảng định tuyến của nó.

Từ lúc giao tới lúc nhận báo cáo, không sửa tệp trong phạm vi đang soát. Việc gấp phải sửa → sửa xong giao lại, giữ số vòng.

## 2. Sau báo cáo

Đọc mục *Lỗ mù* trước, rồi xử lý theo mức:

- **🔴** → giao sửa bằng mã finding kèm nguyên văn đúng mục finding đó; không dán cả báo cáo, không tóm tắt. Code → `backend-expert`, `frontend-expert` hoặc `test-engineer` theo tệp; nội dung luật hay quy ước, hoặc finding đòi một quyết định mới → `architect`; sửa cơ học (đường dẫn, số §, neo, nhãn) → phiên chính — `docs/adr/0111-git-danh-sach-cho-phep-cong-docs-chan-mot-lan-architect-truoc-thi-cong-theo-dau-hieu.md` quyết định 1. Sửa xong → giao vòng kế.
- **🟠 trở xuống** → chỉ nằm trong báo cáo: phiên chính kể lại cho người dùng, không ghi vào `docs/`, không mở vòng mới, không hỏi "làm ngay". Người dùng muốn giữ một finding thì phiên chính soạn nội dung issue cho người dùng tạo ([ADR-0114](../../../docs/adr/0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)).
- **Dòng 🔴 ở mục *Ngoài phạm vi — chỉ 🔴*** → báo người dùng, không tính vào vòng; người dùng chọn có thành issue không.
- **Hết vòng 2 còn 🔴** → 🛑 dừng, hỏi người dùng: sửa tiếp, chuyển thành issue hay chấp nhận.

`core-reviewer` không sửa gì. Người dùng muốn nó sửa → nói rõ việc sửa thuộc người nhận nêu trên.

## Dừng lại và hỏi người dùng

- Không nêu `BE` hay `FE`.
- Phạm vi sắp soát còn thay đổi đang dở.
- Hết vòng 2 còn 🔴.
- Cần lệnh git ghi (`.claude/CLAUDE.md` §1).

## Báo lại

Một dòng: phạm vi, vòng, số finding theo mức, mã finding nào giao ai.
