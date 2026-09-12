---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N7` — Màn hình sinh từ metadata

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> 🛑 **Luồng này NGOÀI PHẠM VI v1.** Nó được viết ra để nói rõ ranh giới, không phải để thi công. Đọc §6 trước khi dùng bất cứ gì ở đây.

---

## 1. Ai bắt đầu, ở đâu

FE, khi mở một màn được khai là "sinh từ metadata" thay vì dựng tay.

## 2. Điều kiện trước

| Cần có | Trạng thái hôm nay |
| --- | --- |
| Bản ghi metadata mô tả lưới và form | 📐 chưa có |
| Endpoint trả metadata đó | 📐 khai trong hợp đồng, **ngoài phạm vi v1** |
| FE biết dựng giao diện từ metadata | 📐 chưa có |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | FE | `GET /meta/grid/{key}` để lấy mô tả lưới | [`../contracts/meta-menu.md`](../contracts/meta-menu.md) §3 |
| 2 | FE | `GET /meta/form/{key}` để lấy mô tả form | cùng trên |
| 3 | FE | Dựng lưới và form từ mô tả đó | [`../Design/COMPONENTS.md`](../Design/COMPONENTS.md) |

### Ranh giới đã chốt, và nó hẹp hơn người ta tưởng

[`../wiki-core/be/03-metadata-driven-design.md`](../wiki-core/be/03-metadata-driven-design.md) §4.2 chốt một ranh giới, và hai hướng nằm **ngoài** nó đã bị loại và khoá:

| Hướng | Trạng thái |
| --- | --- |
| Mô tả **cột lưới** và **trường form** bằng dữ liệu | Trong ranh giới |
| **Biểu thức điều kiện** trong metadata | ❌ đã loại và khoá — lật thì phải có ADR |
| Một **engine form** sinh toàn bộ màn từ metadata | ❌ đã loại và khoá |

Lý do loại: metadata mang biểu thức là một **ngôn ngữ lập trình không có trình gỡ lỗi**. Khi màn hiện sai, không ai đặt được điểm dừng vào một dòng dữ liệu.

## 4. Hỏng ở đâu — và ai thấy gì

Chưa thi công nên chưa có ca hỏng thật. Hai ca **dự kiến được** và đáng ghi trước:

| Ca | Biểu hiện dự kiến |
| --- | --- |
| Metadata mô tả một cột không còn tồn tại trong dữ liệu | Lưới hiện một cột rỗng, **không lỗi nào bắn ra** |
| Metadata và màn dựng tay cùng mô tả một màn | Hai nguồn cho một giao diện — đúng thứ [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5 cấm, chỉ khác là bản sao nằm trong dữ liệu chứ không trong tài liệu |

## 5. Quan hệ với đơn vị

**Chưa quyết định được, và đây là câu quan trọng nhất của luồng.**

Nếu metadata **thuộc đơn vị** thì mỗi đơn vị sửa được lưới của mình, và mỗi bản cài có N biến thể của cùng một màn — chi phí hỗ trợ tăng theo số đơn vị.

Nếu metadata **dùng chung toàn hệ** thì nó giống danh mục quyền: là hợp đồng với code, không phải dữ liệu người dùng.

Hai lựa chọn dẫn tới hai sản phẩm khác nhau, và chọn sai thì sửa rất đắt. Không file nào đang trả lời câu này.

## 6. Câu chưa trả lời được

> 🛑 **Luồng này ngoài phạm vi v1** ([`../contracts/meta-menu.md`](../contracts/meta-menu.md) §3). File này tồn tại để ranh giới đó **nhìn thấy được**, không phải để ai đó bắt đầu thi công.

- **Phạm vi metadata theo đơn vị hay toàn hệ** — xem §5.
- **Quan hệ với `Design/`**: [`../Design/`](../Design/) là nguồn giao diện duy nhất. Một màn sinh từ metadata thì spec màn hình của nó nằm ở đâu — vẫn ở `Design/`, hay trong dữ liệu? Nếu trong dữ liệu thì luật *"`Design/` là nguồn UI duy nhất"* có một ngoại lệ chưa ai khai.
- **Ai sửa metadata?** Chưa có endpoint quản trị, cùng tình trạng với menu ở luồng `P3` §6.
