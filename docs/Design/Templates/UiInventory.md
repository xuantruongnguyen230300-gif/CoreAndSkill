---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khuôn — kiểm kê UI hiện có

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Khuôn này **chưa dùng được ở giai đoạn 1** — xem §1.

> Kiểm kê là bản đếm **cái đang chạy**: có bao nhiêu màn, câu chữ đến từ đâu, tài sản thương hiệu nào được nạp, component nào đã dựng.
>
> 🛑 **Ba khoá frontmatter ĐỔI khi dùng:** khuôn mang `scope: core`; bản kiểm kê của một dự án mang **`scope: du-an`**, `verified: chua-doi-chieu` cho tới khi đối chiếu xong.

---

## 1. Khi nào KHÔNG dùng khuôn này

🛑 **Không dùng khi chưa có `src/`.**

Kiểm kê là tài liệu **hiện trạng** — vai thứ hai trong hai vai ở [`../CLAUDE.md`](../CLAUDE.md) §2. Nó chỉ có nghĩa khi có một ứng dụng đang chạy để đếm. Viết một bản kiểm kê trước khi có app thì mọi dòng của nó là bịa, và tệ hơn: nó **trông giống** một bản đếm thật.

Repo đang ở giai đoạn 1. **Khuôn này để dành.** Nó có mặt ở đây để khi giai đoạn 2 tới, không ai phải nghĩ lại từ đầu.

Dấu hiệu đã đến lúc dùng: có `src/FE/` với ít nhất một tuyến chạy được.

---

## 2. Vì sao kiểm kê phải có trước spec

Thứ tự đúng: **kiểm kê → token → component → màn hình**. Đảo thứ tự sinh ra hai loại hỏng:

| Nếu spec màn hình trước kiểm kê | Hậu quả |
| --- | --- |
| Không biết màn nào đang có | Spec một màn đã bị xoá, hoặc bỏ sót một màn đang chạy |
| Không biết câu chữ đến từ đâu | Ghi "chữ viết thẳng trong template" cho một ứng dụng đã có i18n — **một ô sai mà mọi prompt pack phía sau thừa hưởng** |

Cái thứ hai đã xảy ra thật ở dự án tiền nhiệm: một câu mô tả sai nguồn câu chữ lan xuống từng screen spec, và mỗi bản sao đều trông có thẩm quyền.

---

## 3. Dàn bài — chép từ đây xuống

### `# Kiểm kê UI — <tên dự án>`

Nhãn fidelity — kiểm kê **luôn** là `✅ ĐÃ ĐỐI CHIẾU` kèm ngày, hoặc chưa xong. Nó không có vai "đích đến".

Rồi một dòng: ngày đếm, và **lệnh** dùng để đếm.

### `## Danh sách màn`

Bảng. **Mỗi dòng là một màn tới được trong ứng dụng đang chạy.**

### `## Nguồn câu chữ`

Câu người dùng đọc đến từ đâu: khoá i18n, dữ liệu từ máy chủ, hay viết thẳng trong template. Kiểm bằng lệnh, đừng nhớ.

### `## Tài sản thương hiệu`

Mọi logo, hình minh hoạ, favicon mà ứng dụng thật sự nạp.

### `## Component đã dựng`

Đối chiếu với `COMPONENTS.md`: cái nào đã có thật, cái nào còn là spec.

### `## Chênh lệch phát hiện được`

Chỗ code lệch spec. Theo [`../CLAUDE.md`](../CLAUDE.md) §5 thì **sửa code**, nên mục này là danh sách việc cho phía code, không phải danh sách việc sửa spec.

### `## Cần chốt`

---

## 4. Bảng danh sách màn — sáu cột

| Tuyến | Khung | Component ghép | Nguồn câu chữ | Ảnh | Spec |
| --- | --- | --- | --- | --- | --- |
| `/quan-tri/nguoi-dung` | ứng dụng | PageHeader · Card · Toolbar · DataTable · Pagination | khoá i18n `nguoiDung.*` | chưa chụp | chưa có |

Bốn luật:

1. **Đếm bằng lệnh, không chép số.** Số màn là thứ đếm được, nên [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6 cấm chép. Ghi lệnh đếm ngay đầu file và tiêu chí PASS: *"số dòng trong bảng bằng số tuyến lệnh trả về"*.
2. **Tuyến chuyển hướng và tuyến bắt-tất-cả không phải màn.** Chúng đổ về một trong các tuyến đã đếm.
3. **Cột "Nguồn câu chữ" kiểm bằng lệnh**, không viết từ trí nhớ. Đây là ô đã sai thật ở dự án tiền nhiệm.
4. **Cột "Spec" chỉ ghi đường dẫn tới file đã có.** Chưa có spec thì ghi `chưa có` — không ghi trước tên file, không đánh dấu hoàn thành cho tệp chưa tồn tại. Cùng lý do với ảnh ở §5: một tên file chờ viết trông giống hệt một spec đã xong.

---

## 5. Ảnh màn hình — một cái mặc định, và không hơn

**Mặc định: một ảnh desktop cho mỗi màn.** Đó là thứ trả lời câu *"màn này trông ra sao"*.

Ảnh cho từng trạng thái và từng khổ màn **chỉ chụp khi có người thật sự cần ca đó**.

**Vì sao đặt trần:** ở dự án tiền nhiệm, một lượt lập kế hoạch xếp hàng chục ảnh "chờ chụp" trên vài màn, và **không ảnh nào được chụp**. Một danh sách chờ mà không ai làm thì giá trị bằng không, và nó còn **che mất** những màn thật sự chưa có tham chiếu hình ảnh nào.

🛑 **Không ghi tên file ảnh chưa tồn tại vào bất kỳ spec nào.** Ghi trạng thái `chưa chụp` trong bảng này, kèm hướng dẫn chụp lại được. Spec chỉ trỏ tới ảnh **đã có**.

Hướng dẫn chụp phải đủ để người khác làm lại: lệnh chạy máy chủ, địa chỉ, khổ màn, và cần đăng nhập hay không.

🛑 **Không ghi tài khoản, mật khẩu, khoá vào hướng dẫn chụp.** Ghi *"cần một phiên đã đăng nhập"* và trỏ tới runbook tạo dữ liệu.

---

## 6. Bắt buộc và tuỳ chọn

| Phần | Bắt buộc? | Ghi chú |
| --- | --- | --- |
| Ba khoá frontmatter | ✅ **Bắt buộc** | |
| Nhãn fidelity kèm ngày đếm | ✅ **Bắt buộc** | Kiểm kê không có ngày là kiểm kê vô nghĩa |
| Lệnh đếm + tiêu chí PASS ở đầu file | ✅ **Bắt buộc** | |
| Bảng danh sách màn | ✅ **Bắt buộc** | |
| Cột nguồn câu chữ, kiểm bằng lệnh | ✅ **Bắt buộc** | |
| Bảng tài sản thương hiệu | ✅ **Bắt buộc** có tiêu đề | Không có tài sản nào thì ghi `Không có` |
| Mục chênh lệch phát hiện được | ✅ **Bắt buộc** | Rỗng thì ghi `Không phát hiện` |
| Bảng đối chiếu component đã dựng | ⬜ Tuỳ chọn | Rất nên có — nó là căn cứ để đổi trạng thái ở `COMPONENTS.md` |
| Ghi chú hiệu năng đo được | ⬜ Tuỳ chọn | |
| Chép số lượng màn/component | 🛑 **Cấm** | Đếm bằng lệnh |
| Tên file ảnh chưa tồn tại | 🛑 **Cấm** | |
| Tài khoản, mật khẩu | 🛑 **Cấm** | |

---

## 7. Danh sách kiểm

- [ ] Có `src/` để đếm — nếu không, **dừng lại, chưa dùng khuôn này**.
- [ ] Mỗi số liệu có một lệnh đếm đi kèm và một tiêu chí PASS.
- [ ] Cột nguồn câu chữ kiểm bằng lệnh, không viết từ trí nhớ.
- [ ] Không tên file ảnh nào chưa tồn tại.
- [ ] Không thông tin đăng nhập nào.
- [ ] Mục chênh lệch nêu việc cho **phía code**, không nêu việc sửa spec.
- [ ] `bash .claude/check-docs.sh` xanh.
