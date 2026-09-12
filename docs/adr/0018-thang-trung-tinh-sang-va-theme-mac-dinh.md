---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0018 — Thang trung tính sáng nhạt đi một bậc, và theme mặc định là sáng

> **Trạng thái:** Đã chấp nhận (2026-09-11)

## Bối cảnh

[`../Design/DESIGN.md`](../Design/DESIGN.md) chốt bảng màu bằng cách tính từ công thức tương phản WCAG chứ không chọn bằng mắt, và mọi cặp chữ/nền đều đạt AA ở cả hai theme. Bảng đó đúng về số đo.

Nhưng một bản dựng thử giao diện — khung ứng dụng thật với sidebar, bảng dữ liệu, thanh công cụ, dựng bằng đúng bộ token đó — cho thấy hai vấn đề mà bảng số không nói ra:

1. **Giao diện đọc ra "nặng".** Nền trang `#eef1f6`, nền `th` `#e2e8f0` và viền `#7c8ba3` cộng lại làm mỗi bảng thành một khối xám có khung đậm. Không con số nào sai; tổng thể vẫn mệt mắt.
2. **Người mở lần đầu nhận nền tối.** Cơ chế theme khai ba trạng thái, trong đó *không có thuộc tính* nghĩa là đi theo `prefers-color-scheme` của hệ điều hành. Máy để chế độ tối thì ứng dụng ra nền tối ngay lần mở đầu tiên, kể cả với người chưa bao giờ chọn gì.

Đây là quyết định **đổi giá trị đã kiểm** trong file chủ của hệ màu, nên theo [`../Design/CLAUDE.md`](../Design/CLAUDE.md) §5 nó phải đi kèm một ADR chứ không sửa lặng lẽ.

## Quyết định

> **Nhấc cả thang trung tính sáng lên một bậc, và đổi mặc định theme từ "theo hệ điều hành" sang `light`.**

### Thang trung tính

| Token | Cũ | Mới |
| --- | --- | --- |
| `--color-bg` | `#eef1f6` | `#f4f6fa` |
| `--color-surface-2` | `#e2e8f0` | `#eef1f6` |
| `--color-surface-3` | `#d5dde8` | `#e2e8f0` |
| `--color-border-subtle` | `#dbe1ea` | `#e4e9f0` |
| `--color-border` | `#7c8ba3` | `#7f8da3` |
| `--color-border-strong` | `#5a6779` | `#5f6d80` |

`--color-surface` giữ `#ffffff`. Theme tối **không đổi** — vấn đề không nằm ở đó.

Điểm đáng chú ý của bảng trên: giá trị cũ của `bg` trở thành giá trị mới của `surface-2`, và giá trị cũ của `surface-2` trở thành giá trị mới của `surface-3`. Cả thang **trượt một bậc**, không phải sáu lần chỉnh lẻ. Đó là điều giữ cho quan hệ giữa các bậc không đổi.

### Viền chỉ nhạt được tới một mức, và mức đó do phép đo đặt

`--color-border` **không** trượt cùng nhịp với ba bậc nền. Lý do là một ràng buộc cứng phát hiện trong lúc tính:

> **Ngưỡng chặn là `border` trên `bg`, không phải `border` trên `surface`.**

Nền trang tối hơn bề mặt, nên mọi lần làm nhạt nền trang đều kéo con số này xuống trước tiên — và `Card` thì nằm thẳng trên nền trang. Đo thử:

| Ứng viên viền | trên `surface` | **trên `bg`** |
| --- | --- | --- |
| `#8593a8` | 3.12 | **2.88 ❌** |
| `#8290a6` | 3.24 | **2.99 ❌** |
| `#7f8da3` | 3.36 | **3.11 ✅** |

Ứng viên đầu tiên là thứ trực giác chọn — nhạt rõ rệt, vẫn qua 3:1 trên nền trắng. Nó **hỏng**, và hỏng ở đúng chỗ không ai nhìn vào: viền của một thẻ đặt trên nền trang. `#7f8da3` là bậc nhạt nhất còn qua được cả hai.

Hệ quả phải nói thẳng: **phần nhạt đi mà mắt cảm nhận được đến gần như toàn bộ từ ba bậc nền, không từ viền.** Viền chỉ nhạt được một chút, và đó là trần.

### Theme mặc định

Mặc định là `light`. Ứng dụng **không** đọc `prefers-color-scheme` để quyết hộ ở lần mở đầu. Giá trị "theo hệ điều hành" vẫn còn trong cơ chế và người dùng chọn được; nó chỉ không còn là điểm xuất phát.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — giữ nguyên bảng màu, chỉ đổi mặc định theme sang sáng

**Được:** không đụng một giá trị đã kiểm nào, không phải tính lại bảng tương phản, rủi ro bằng không.

**Vì sao loại:** nó chỉ giải một nửa. Người dùng nhận nền sáng, nhưng nền sáng đó vẫn là nền sáng bị chê. Hai vấn đề trong Bối cảnh độc lập với nhau, và sửa một cái không làm cái kia biến mất.

### Phương án B — chỉ làm nhạt `--color-bg`, giữ nguyên `surface-2` và viền

**Được:** một dòng đổi, dễ đảo ngược.

**Vì sao loại:** đây là phương án sai vì nó nhắm sai chỗ. Thứ tạo cảm giác nặng nhất là **dải nền `th` của bảng và đường viền quanh mọi khối**, không phải nền trang. Làm nhạt riêng nền trang còn khiến `surface-2` bật lên rõ hơn — bảng trông **nặng hơn** trước. Một thay đổi làm vấn đề tệ đi trong khi trông như đang sửa nó.

### Phương án C — bỏ viền `Card`, dựa hẳn vào bóng để tách khối

**Được:** đây là cách các hệ thiết kế hiện đại hay dùng, và cho ra giao diện nhẹ nhất trong ba phương án.

**Vì sao loại:** [`../Design/DESIGN.md`](../Design/DESIGN.md) §5.2 đã chốt **bóng không bao giờ là ranh giới** — người dùng chế độ tương phản cao và người dùng màn hình rẻ không thấy bóng nào cả. Với `surface` chênh `bg` chỉ 1.08:1, bỏ viền nghĩa là thẻ **biến mất** với nhóm đó. Đây là hồi quy accessibility đội lốt lựa chọn thẩm mỹ, và nó đúng là thứ [`../Design/Components/Card.md`](../Design/Components/Card.md) ghi thẳng là cấm.

### Phương án D — hỏi hệ điều hành nhưng mặc định sáng khi không rõ

**Vì sao loại:** `prefers-color-scheme` không có trạng thái "không rõ". Nó trả về `light` hoặc `dark`, và rất nhiều máy trả `dark` vì cài đặt mặc định của nhà sản xuất chứ không vì người dùng chọn. Suy diễn ý muốn từ tín hiệu đó là suy diễn từ một tín hiệu yếu, và phương án này không làm nó mạnh lên — nó chỉ mô tả lại phương án đang bị thay.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| Mười bốn con số tương phản trong `DESIGN.md` phải tính lại | Và hai file khác đang chép lại con số cũ ([`../Design/Components/Card.md`](../Design/Components/Card.md), [`../Design/Components/Badge.md`](../Design/Components/Badge.md)) phải sửa theo. Đây đúng là cái giá mà luật "một nội dung một nguồn" tồn tại để giảm, và lần này nó vẫn lộ ra hai bản sao hợp lệ — con số trích dẫn kèm lời giải thích |
| Người thật sự cần nền tối phải tự đổi một lần | Chấp nhận được **chỉ vì** nút đổi theme nằm ngay trên `Topbar`, không giấu trong trang cấu hình, và lựa chọn được nhớ lại. Nếu nút đó bị đẩy vào trang cấu hình thì quyết định này phải xem lại |
| `border` trên `surface-2` tụt từ 2.80 xuống 2.97 — vẫn dưới 3:1 | Luật sẵn có *"component có viền đặt trên `--color-surface-2` phải dùng `--color-border-strong`"* vẫn đúng và vẫn cần. Quyết định này **không** gỡ được nó |
| Phần dư của `border` trên `bg` chỉ còn 0.11 | Bất kỳ lần làm nhạt nền trang nào sau này đều sẽ phá ngưỡng. Con số này phải được kiểm lại mỗi lần ai đó định đụng vào `--color-bg` |

### Tích cực

- Dải nền `th` nhạt đi một bậc rõ rệt, nên bảng dữ liệu — thứ chiếm phần lớn diện tích của một ứng dụng quản trị — nhẹ hẳn.
- Quan hệ giữa các bậc không đổi, nên không spec component nào phải sửa cách dùng token.
- Người mở lần đầu nhận đúng thứ ứng dụng được thiết kế cho.
- Ngưỡng chặn thật của thang trung tính (`border` trên `bg`) nay được ghi ra và có số, thay vì nằm ẩn.

### Điều kiện lật quyết định

Lật phần **theme mặc định** nếu có số đo thật cho thấy phần lớn người dùng đổi sang tối ngay sau lần đăng nhập đầu. Lúc đó mặc định sai, và số liệu sẽ nói điều đó rõ hơn mọi lập luận ở đây.

Lật phần **thang trung tính** thì khó hơn nhiều và cần một ADR mới: nó kéo theo tính lại toàn bộ bảng tương phản một lần nữa. Đừng lật vì cảm giác — chỉ lật khi có một ràng buộc mới đo được, ví dụ yêu cầu hỗ trợ chế độ tương phản cao ([`../Design/DESIGN.md`](../Design/DESIGN.md) §10 câu 2 còn để ngỏ).

## Liên quan

- [`../Design/DESIGN.md`](../Design/DESIGN.md) §2.1, §2.2, §2.3 — giá trị và số đo mới; §8 — mặc định theme
- [`../Design/CLAUDE.md`](../Design/CLAUDE.md) §5 — luật buộc quyết định này phải có ADR
- [`../Design/Components/Card.md`](../Design/Components/Card.md) — nơi luật "viền không bỏ được" sống
- [`../audit/2026-09-11-lenh-kiem-tu-dem-chinh-no.md`](../audit/2026-09-11-lenh-kiem-tu-dem-chinh-no.md) — cùng đợt làm việc, một lỗi khác được tìm ra
