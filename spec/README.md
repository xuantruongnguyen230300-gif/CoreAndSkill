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
| Vòng đời | Theo khoá `scope` của từng tệp — [`../docs/quy-uoc/repo-artifact.md`](../docs/quy-uoc/repo-artifact.md) §15.5 | Chết cùng dự án |
| Ai sở hữu | `architect`, `design-expert` (`docs/Design/`) | `ba-analyst`, người dùng nghiệp vụ |
| Khi mang Core sang dự án khác | **Mang theo** tệp `scope: core` | **Không mang** |

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
   ├─ ui-spec.md          chỉ khi feature có màn hình mới
   └─ ban-giao/           tài liệu bàn giao — tech-writer ghi
      ├─ TechDoc.md
      ├─ UserGuide.md
      └─ UnitTest.md
```

📐 Khuôn của ba tệp bàn giao sẽ vào `_template/`, trong một thư mục con cùng tên, sau khi người dùng duyệt bản ở lần chạy đầu tiên.

## Luật

1. **Feature nghiệp vụ không có `business-rules.md` thì không được bắt đầu code.** `backend-expert` và `frontend-expert` sẽ dừng lại và hỏi — chúng không tự suy diễn nghiệp vụ, không tự bịa spec để có cái mà chạy tiếp.
2. **Feature thuộc Core không cần `spec/`.** Việc chạm Core qua `architect` **trước** thi công chỉ khi mang ít nhất một dấu hiệu ở [`../docs/adr/README.md`](../docs/adr/README.md) §2 hoặc đưa code mới vào Core; việc khác thi công thẳng rồi qua `core-reviewer`, và việc không phải chọn gì thì không cần dòng nhật ký. Ngưỡng và ngoại lệ: [`../docs/kien-truc-core-module.md`](../docs/kien-truc-core-module.md) §4.2, dòng *Cơ chế chống*.
3. **Không chắc một feature thuộc Core hay Module** thì dừng lại và hỏi người dùng — đó là quyết định ranh giới kiến trúc, tiêu chí ở [`../docs/kien-truc-core-module.md`](../docs/kien-truc-core-module.md) §4.
4. Mọi file trong `spec/` khai đủ ba khoá frontmatter như file `docs/` — cổng kiểm cả khu này: luật **D1** ở [`../docs/RULES-DOCS.md`](../docs/RULES-DOCS.md).

## Bắt đầu một feature mới

```bash
mkdir -p spec/<ten-feature>
cp spec/_template/business-rules.md spec/<ten-feature>/
cp spec/_template/ui-spec.md spec/<ten-feature>/   # chỉ khi feature có màn mới
```

Chỉ chép tệp spec ở trên — không chép thư mục khuôn bàn giao; `tech-writer` tự dùng khuôn đó khi tới lượt.

Rồi gọi skill `/ba-story` để `ba-analyst` điền nội dung, hoặc tự viết theo khuôn.

## Trạng thái

Feature nào đã có spec — đọc bằng lệnh, không chép danh sách:

```bash
ls spec/
```

Lệnh chỉ in `README.md` và `_template` thì chưa có feature nghiệp vụ nào. Hiện trạng `src/` và giai đoạn của repo: [`../docs/README.md`](../docs/README.md) mục *Trạng thái repo*.
