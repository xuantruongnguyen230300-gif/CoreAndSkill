---
kind: tham-chieu
scope: core
verified: khong-ap-dung
---

# Sổ nợ đã bỏ — tệp chuyển hướng

> **Tệp này không giữ luật, không giữ việc.** Nó ở lại để các ADR, [nhật ký quyết định](adr/nhat-ky-quyet-dinh.md) và chú thích mã nguồn trỏ tới nó còn tới được đích — [ADR-0114](adr/0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md) quyết định 2, 15. Đừng ghi thêm gì vào đây.

## Ba điều thay cho sổ nợ

1. **Sổ nợ đã bỏ.** Không tệp nào dưới `docs/` giữ sổ nợ hay danh sách việc tồn — ADR-0114 quyết định 1.
2. **Luật chưa có cổng — luật chỉ review canh — đọc ở cột trạng thái của các tệp `RULES-*.md`** (mục lục và chú giải ký hiệu: [`RULES.md`](RULES.md)). Dòng mang `🕳️`, hoặc `📐` khi tên cổng đã chốt; ô *Ép bằng gì* nêu ai canh hôm nay. Lệnh liệt kê nằm ở đầu `RULES.md`.
3. **Việc tồn — code hay test lệch luật — ở GitHub Issues của repo.** Tiêu đề issue mở đầu bằng mã trong ngoặc vuông, nên tra theo mã. Ý tưởng dựng cổng không thành issue: cổng dựng khi vi phạm thật lặp lại — ADR-0114 quyết định 3.

## Tra một mã từng nằm ở đây

Một mã có dòng ở `RULES-*.md` thì dòng đó là chủ — ví dụ B22:

```bash
grep -nE '^\| B22 ' docs/RULES-*.md
```

Không ra dòng nào thì tra bảng *Phân loại* của ADR-0114: mã của luật thuộc ADR đang tạm hoãn, mã việc đã ngừng dùng, hay mã giữ cho một đề xuất chờ quyết định. Lý do chưa có cổng và ý tưởng cổng của từng mã không chép sang đâu — lịch sử git của tệp này giữ chúng:

```bash
git log --oneline -- docs/DEBT.md
```

Các hàng mục *Nợ review* đã chuyển thành issue — bản nháp chờ người dùng tạo (2026-10-06).
