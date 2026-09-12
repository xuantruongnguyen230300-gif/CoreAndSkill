# CHANGELOG — Core

Mỗi mục là **một** thay đổi của Core mà dự án hạ nguồn cần biết. Quyết định nền:
`docs/adr/0016-phan-phoi-core-bang-clone.md`; thao tác kéo bản mới: `docs/quy-uoc/repo-artifact.md` §15.

Khuôn một mục:

```text
## core-vX.Y.Z — YYYY-MM-DD
- Thêm: ...
- Sửa: ...
- ⚠️ Phá vỡ: ...
  Dự án phải làm gì: ...
```

Luật: mục **Phá vỡ** bắt buộc kèm dòng *"Dự án phải làm gì"*. Thiếu dòng đó thì người kéo bản mới
về chỉ biết là có gì đó vỡ, không biết vỡ ở đâu.

---

## Chưa có bản phát hành nào

Repo đang ở giai đoạn 1 — chưa có `src/`, nên chưa có tag Core nào được đánh
(`docs/adr/0012-giai-doan-1-chi-docs.md`).
