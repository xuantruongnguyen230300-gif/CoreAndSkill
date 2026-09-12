---
kind: luat
scope: core
verified: chua-doi-chieu
---

# F2 — Xác thực, routing và menu theo quyền

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**
>
> **Định nghĩa hoàn thành:** đăng nhập qua form thật bằng **cookie phiên**; guard chặn đúng khi chưa đăng nhập **và** điều hướng kèm đường dẫn quay lại; tài khoản buộc đổi mật khẩu bị ép sang màn đổi mật khẩu và **không vào được route nào khác**; đăng xuất khiến route cũ bị chặn **ngay**, không cần tải lại trang; menu dựng theo **permission** trả về từ server.

---

## 1. Hợp đồng đã chốt — không phải đoán

> **Phạm vi F2 gồm cả luồng quên mật khẩu**: màn nhập email, màn đặt lại mật khẩu theo liên kết, và màn thông báo kết quả. Hợp đồng đã có ở [`../../../contracts/auth.md`](../../../contracts/auth.md) — ba màn này hay bị bỏ quên vì chúng nằm **ngoài** guard, tức không màn nào trong app dẫn tới chúng.

Hình dạng request và response nằm ở [`../../../contracts/auth.md`](../../../contracts/auth.md): đăng nhập, đăng xuất, "tôi là ai", đổi mật khẩu, và endpoint lấy token XSRF.

Vì hình dạng đã cố định, FE **dựng được ngay** trên dữ liệu giả đúng hợp đồng, không phải đợi BE. Nhưng **đóng** F2 thì cần endpoint thật chạy — vì đúng những thứ dữ liệu giả không mô phỏng được (cookie, XSRF, hình dạng lỗi thật) là những thứ hay hỏng nhất.

---

## 2. Thứ tự viết

```
1. Interceptor cookie + XSRF (nếu chưa xong ở F0)
        │
        ▼
2. AuthService — đăng nhập, đăng xuất, "tôi là ai" + ánh xạ DTO → model
        │
        ▼
3. SessionService — state dạng signal, nạp MỘT LẦN lúc khởi động app
        │
        ▼
4. PermissionService + directive hiển thị theo quyền
        │
        ▼
5. Ba guard theo đúng thứ tự + khai route từng feature
        │
        ▼
6. Màn đăng nhập và màn đổi mật khẩu (bố cục không có shell)
        │
        ▼
7. Layout shell + menu động
```

**Bước 3 nạp một lần lúc khởi động, không nạp lại mỗi lần đổi route.** Gọi trong guard nghĩa là mỗi lần điều hướng là một request nữa, và mỗi lần đó là một cơ hội để giao diện chớp nháy hoặc để hai request đua nhau.

> 📖 Cơ chế đầy đủ, mười bẫy và cách kiểm: [`../07-auth-identity.md`](../07-auth-identity.md).
> 📖 Quy ước guard, thứ tự guard, cờ tắt shell: [`../../../quy-uoc/fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md). Đó là nguồn duy nhất — không viết lại guard theo trí nhớ.

---

## 3. Permission, KHÔNG phải role

Đây là quyết định nền của repo ([`../../../adr/0005-permission-based.md`](../../../adr/0005-permission-based.md)) và F2 là pha nó thành hình.

| Làm | Không làm |
| --- | --- |
| Guard nhận **mã permission** từ dữ liệu của route | Guard so sánh tên vai trò |
| Directive hỏi *"có permission X không"* | Điều kiện hiển thị so chuỗi vai trò |
| Menu do **server** lọc sẵn theo quyền | Danh sách menu khai cứng ở FE rồi lọc |

**Không có hằng số vai trò nào trong `core/`.** FE không biết tập permission nào tồn tại — chuỗi do nơi gọi truyền vào và do dữ liệu quyết định.

Ở dự án tiền nhiệm, ba tên vai trò được khai cứng và mọi màn quản trị phải biết ba cái tên đó. Đó là lý do phần phân quyền không dùng lại được ở sản phẩm thứ hai.

---

## 4. Sáu chỗ hỏng im lặng — kiểm riêng từng chỗ

Cả sáu đều **không** gây lỗi biên dịch và **không** bị test mặc định nào bắt.

### 4.1 Guard buộc đổi mật khẩu bị bỏ quên

Người bị buộc đổi mật khẩu **đã đăng nhập thành công**, nên guard xác thực cho qua. Thiếu guard riêng thì họ vào được toàn bộ app với mật khẩu tạm.

Phải kiểm bằng **một tài khoản thật** ở trạng thái đó, không suy luận từ code.

### 4.2 Vòng lặp điều hướng vô hạn

Guard ép sang màn đổi mật khẩu mà không loại trừ chính màn đó → điều hướng lặp vô hạn, trình duyệt treo. Tệ hơn nhiều so với chặn sai.

### 4.3 Thứ tự guard đảo

Kiểm permission trước khi kiểm "buộc đổi mật khẩu" → người buộc đổi mật khẩu nhận thông báo "không đủ quyền" thay vì được đưa tới màn đổi mật khẩu. Thông báo sai và gây hoang mang.

### 4.4 Nhiều 401 cùng lúc

Một màn gọi bốn API song song, phiên hết hạn, cả bốn trả 401 → bốn lần điều hướng và bốn thông báo. Phải chặn: chỉ xử lý lần đầu.

### 4.5 401 của chính lời gọi đăng nhập bị coi là hết phiên

Sai mật khẩu trả 401. Không loại trừ endpoint đăng nhập thì người nhập sai mật khẩu bị "đăng xuất" và điều hướng lung tung thay vì thấy thông báo sai mật khẩu.

### 4.6 Token XSRF cũ sau khi đăng nhập

Đăng nhập thường xoay vòng phiên; token cũ hết hiệu lực. Không lấy lại thì **mọi thao tác ghi đầu tiên sau đăng nhập** đều hỏng — trong khi mọi thao tác đọc vẫn bình thường. Hình dạng triệu chứng này là dấu hiệu nhận biết: ghi hỏng, đọc chạy.

---

## 5. Bật cổng chữ tiếng Việt trong template

F2 là pha i18n có mặt, nên đây là lúc luật F8 bắt đầu có nghĩa.

Bật ngay khi hạ tầng i18n chạy, **trước** khi dựng hàng loạt màn — bọc chuỗi trên một codebase đã lớn là một đợt sửa chạm mọi file.

Cách dò và ba giới hạn phải biết: [`../08-i18n.md`](../08-i18n.md) §10.

---

## 6. Điều kiện phía BE hay bị nghi oan cho FE

Ở mọi môi trường, FE và API ở hai origin khác nhau ([`../17-phuc-vu-va-trien-khai.md`](../17-phuc-vu-va-trien-khai.md) §4). Dev cũng bật CORS và chạy HTTPS như thật — đừng dùng proxy của máy chủ dev để giấu ranh giới đi.

Triệu chứng khi cấu hình sai: **luôn nhận 401 dù vừa đăng nhập thành công.** Gặp hình dạng này thì kiểm theo thứ tự — cookie có được gửi kèm request không, origin có nằm trong allowlist không, FE và API có cùng scheme không — lệch scheme là khác site, cookie không đi — trước khi nghi ngờ code FE.

---

## 7. Nghiệm thu F2

- [ ] Gọi API cần đăng nhập khi chưa đăng nhập → điều hướng kèm đường dẫn quay lại, không phải màn trắng hay lỗi trong console
- [ ] Đăng nhập xong quay **đúng** về đường dẫn đó, không phải luôn về trang đích
- [ ] Đường dẫn quay lại trỏ ra miền ngoài → **bị từ chối**
- [ ] Tài khoản buộc đổi mật khẩu: gõ URL bất kỳ đều bị đưa về màn đổi mật khẩu; riêng màn đó **không lặp**
- [ ] Đổi mật khẩu xong đi thẳng vào app — **không** bắt đăng nhập lại
- [ ] Thao tác ghi **ngay sau** đăng nhập thành công chạy được (§4.6)
- [ ] Đăng xuất → gọi lại API cần auth → chặn ngay, không cần tải lại trang
- [ ] Bốn request song song cùng nhận 401 → **một** lần điều hướng, **một** thông báo
- [ ] Nhập sai mật khẩu → thấy thông báo sai mật khẩu, không bị coi là hết phiên
- [ ] Đăng xuất ở một tab → tab khác tự dọn
- [ ] Tài khoản thiếu permission gõ thẳng URL → bị chặn, **không** thấy nội dung màn dù chỉ chớp nhoáng
- [ ] Menu chỉ hiện mục mà tài khoản có quyền; ẩn mục **không** thay được guard
- [ ] Đổi ngôn ngữ → menu đổi theo (cache menu có khoá gồm cả người dùng lẫn ngôn ngữ)
- [ ] Không tệp `.html` nào chứa chữ tiếng Việt (luật F8)
- [ ] Không hằng số vai trò nào trong `core/`
- [ ] Xem tab Network: cookie thật sự được gửi kèm request

---

## 8. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Cơ chế xác thực đầy đủ | [`../07-auth-identity.md`](../07-auth-identity.md) |
| Guard và routing | [`../../../quy-uoc/fe-routing-guard.md`](../../../quy-uoc/fe-routing-guard.md) |
| Hợp đồng endpoint | [`../../../contracts/auth.md`](../../../contracts/auth.md) · [`../../../contracts/meta-menu.md`](../../../contracts/meta-menu.md) |
| i18n và cổng F8 | [`../08-i18n.md`](../08-i18n.md) |
| Vì sao permission chứ không phải role | [`../../../adr/0005-permission-based.md`](../../../adr/0005-permission-based.md) |
| Pha kế tiếp | [`04-f3-man-quan-tri.md`](04-f3-man-quan-tri.md) |
