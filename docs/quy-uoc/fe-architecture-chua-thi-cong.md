---
kind: luat
scope: core
verified: chua-doi-chieu
---

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG**

# Phần chưa thi công — `fe-architecture.md`

> Nội dung dưới đây dời từ [`fe-architecture.md`](fe-architecture.md) §2.7 ngày 2026-09-27. Luật đã thi công của seam `CORE_SCREEN_EXT` nằm ở đó; mục này chỉ giữ phần **chưa** có điểm mở rộng thật để đối chiếu, nên không đứng lẫn với luật đang ép bằng cổng.

## §2.7 Seam cho MÀN Core — mở rộng `Toolbar`

Hành động dòng, trường lọc và nút riêng trên `Toolbar` **chưa** có điểm mở rộng: nút và hành động cần một dạng callback chưa ai dùng thật để đối chiếu, trường lọc cần `FilterPanel` chưa dựng. Thêm bằng ADR mới khi dự án hạ nguồn đầu tiên cần — [`../adr/0057-seam-fe-chi-mang-gia-tri-dung-duoc-o-composition-root.md`](../adr/0057-seam-fe-chi-mang-gia-tri-dung-duoc-o-composition-root.md).
