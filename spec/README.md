---
kind: luat
scope: du-an
verified: chua-doi-chieu
---

# `spec/` — nghiệp vụ theo từng feature

> **`docs/` giữ QUY TẮC. `spec/` giữ NGHIỆP VỤ.**
>
> Quy tắc kiến trúc và quy ước code luôn ở `docs/`, và agent phải tuân thủ chúng **kể cả khi đang làm việc thuộc `spec/`**. Luật đầy đủ ở [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §2.

## Vì sao đặt ngoài `docs/`

Ranh giới này có chủ đích, không phải tuỳ tiện:

| | `docs/` | `spec/` |
| --- | --- | --- |
| Trả lời câu hỏi | *"Làm thế nào cho đúng?"* | *"Nghiệp vụ này quy định gì?"* |
| Vòng đời | Đi theo Core sang mọi dự án | Chết cùng dự án |
| Ai sở hữu | `architect`, `core-reviewer` | `ba-analyst`, người dùng nghiệp vụ |
| Khi mang Core sang dự án khác | **Mang theo** | **Không mang** |

Trộn hai thứ này là cách nhanh nhất để một bộ khung dùng chung nhiễm nghiệp vụ của dự án đầu tiên — và một Core chứa nghiệp vụ là một Core không mang đi đâu được.

## Cấu trúc một feature

```
spec/
├─ README.md              file này
├─ _template/             khuôn để sao chép khi bắt đầu feature mới
│  ├─ business-rules.md
│  └─ ui-spec.md
└─ <ten-feature>/
   ├─ business-rules.md   BẮT BUỘC — luật nghiệp vụ, US, AC, BR
   └─ ui-spec.md          chỉ khi feature có màn hình mới
```

## Luật

1. **Feature nghiệp vụ không có `business-rules.md` thì không được bắt đầu code.** `backend-expert` và `frontend-expert` sẽ dừng lại và hỏi — chúng không tự suy diễn nghiệp vụ, không tự bịa spec để có cái mà chạy tiếp.
2. **Feature thuộc Core không cần `spec/`**, nhưng thay đổi kiến trúc phải qua `architect` và phải có ADR trong [`../docs/adr/`](../docs/adr/).
3. **Không chắc một feature thuộc Core hay Module** thì dừng lại và hỏi người dùng — đó là quyết định ranh giới kiến trúc, tiêu chí ở [`../docs/kien-truc-core-module.md`](../docs/kien-truc-core-module.md) §4.
4. Mọi file trong `spec/` khai đủ ba khoá frontmatter như file `docs/` — cổng kiểm cả khu này.

## Bắt đầu một feature mới

```bash
cp -r spec/_template spec/<ten-feature>
```

Rồi gọi skill `/ba-story` để `ba-analyst` điền nội dung, hoặc tự viết theo khuôn.

## Trạng thái

📐 **Khu này hiện chỉ có khuôn.** Repo đang ở giai đoạn 1 — chưa có `src/`, chưa có feature nghiệp vụ nào. Xem [`../docs/README.md`](../docs/README.md) §Trạng thái repo.
