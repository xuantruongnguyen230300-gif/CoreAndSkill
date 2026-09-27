---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# Nhật ký quyết định nhỏ

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Chưa dòng nào trong tệp này được đối chiếu với source.

Quyết định không mang dấu hiệu nào ở [`README.md`](README.md) §2 thì không viết ADR: ghi **một dòng** ở đây, mới nhất ở trên, rồi sửa thẳng tệp chủ của nội dung. Dòng chỉ ghi *đã chốt gì, ai chốt, sửa ở đâu* — lý do dài thì không phải quyết định nhỏ.

| Ngày | Quyết định | Ai chốt | Sửa ở |
| --- | --- | --- | --- |
| 2026-09-27 | Giai đoạn phát triển vẫn chạy nhóm test `RequiresDocker` trên CI, không bỏ tới khi Core xong. Bước test BE chia hai: không Docker (đúng lệnh máy dev) và nhóm `RequiresDocker`; hai bước test và E10 chạy khi build xanh, kể cả khi bước trước đỏ | Người dùng | `.github/workflows/docs-gate.yml` (job `backend-gate`), [`../wiki-core/be/04-testing-strategy.md`](../wiki-core/be/04-testing-strategy.md) §4.2 |
