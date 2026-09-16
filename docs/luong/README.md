---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `luong/` — luồng hoạt động của Core, từ đầu tới cuối

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> **Khu này trả lời đúng một câu hỏi:**
>
> > *Khi một người làm việc X, hệ thống đi qua những đâu — theo thứ tự nào, và hỏng ở đâu?*

---

## 1. Vì sao khu này tồn tại

Mọi khu khác trong `docs/` được viết **theo chủ đề**: một file cho danh tính, một file cho phân quyền, một file cho lưu tệp. Cách đó đúng cho việc tra cứu, và sai cho một loại lỗi.

**Lỗi loại đó nằm ở mối nối giữa hai file, nên không file nào chịu trách nhiệm.** Ba ca đã tìm ra ở repo này, cả ba đều cùng khuôn:

| Ca | Mối nối bị hở |
| --- | --- |
| Cờ bỏ qua kiểm quyền được mô tả bằng văn xuôi ở một file, còn bảng cột ở file khác thì không có cột nào tương ứng | cơ chế ↔ lược đồ |
| `contracts/tenants.md` nói admin đơn vị mới mang cờ buộc đổi mật khẩu, nhưng không nói nó còn phải mang cờ thứ hai — thiếu cờ đó thì đăng nhập được mà không làm được gì | hợp đồng ↔ cơ chế |
| [`../database/script-runbook.md`](../database/script-runbook.md) mô tả cách dựng database mà không biết multi-tenant tồn tại | vận hành ↔ quyết định kiến trúc |

Không cổng nào bắt được cả ba, vì **không có gì mâu thuẫn cả** — chỉ có im lặng. Một luồng đi hết từ đầu tới cuối là thứ duy nhất làm im lặng đó hiện ra.

## 2. Luật của khu này — đọc trước khi viết file đầu tiên

> 🛑 **Một file luồng KHÔNG được chứa định nghĩa nào.** Nó sở hữu **thứ tự** và **mối nối**; mọi chi tiết trỏ về file chủ.

Cụ thể:

| Được viết | Không được viết |
| --- | --- |
| Bước 1, 2, 3… ai làm, làm gì | Chữ ký kiểu, tên cột, giá trị token |
| `code` của một lỗi, **kèm link tới card hợp đồng chủ** | HTTP status hay `ErrorType` của mã đó — card giữ ánh xạ, luồng chỉ trỏ tới |
| *"Bước này hỏng thì thấy gì"* | Bảng đặc tả một bảng dữ liệu |
| Chỗ hai file phải khớp nhau, và khớp ở điểm nào | Luật mới — luật vào [`../RULES.md`](../RULES.md) |
| Câu hỏi chưa ai trả lời | Câu trả lời tự nghĩ ra |

Lý do luật này cứng: một file luồng nhắc lại chi tiết sẽ thành **nguồn thứ hai**, và nguồn thứ hai luôn lệch ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5). Khu này sinh ra để **đóng** loại lỗi đó, không phải để nhân nó lên.

## 3. Khuôn bắt buộc — sáu mục

| # | Mục | Phải trả lời |
| --- | --- | --- |
| 1 | **Ai bắt đầu, ở đâu** | Vai nào, từ màn nào hoặc từ dòng lệnh nào |
| 2 | **Điều kiện trước** | Cái gì phải có sẵn thì luồng này mới chạy được |
| 3 | **Các bước** | Bảng: *bước · ai làm · hệ thống làm gì · file chủ của chi tiết* |
| 4 | **Hỏng ở đâu** | Mỗi bước hỏng thì người dùng **thấy gì**. Không viết "trả lỗi 500" |
| 5 | **Quan hệ với đơn vị** | *thuộc đơn vị* · *dùng chung toàn hệ* · *không áp dụng, vì …* — **không được để trống** |
| 6 | **Câu chưa trả lời được** | Chỗ docs còn hở. Trống thì viết "Không còn" |

Mục 5 là mục đắt nhất và là lý do khu này ra đời: [`../RULES.md`](../RULES.md) §9 xếp rò dữ liệu giữa hai đơn vị là **rủi ro nghiêm trọng nhất của toàn hệ**, và nó *"không gây lỗi, không gây ngoại lệ — truy vấn chạy bình thường, chỉ trả về nhiều hơn đáng ra được thấy"*. Bắt mỗi luồng tự trả lời câu đó biến một khoảng im lặng thành một câu kiểm được.

Mục 6 theo đúng khuôn *"nợ nhìn thấy được"*: một luồng có chỗ hở mà nói ra thì còn sửa được; một luồng trông trọn vẹn thì không ai kiểm lại.

## 4. Mục lục

Trạng thái: ✅ đã viết · 🚧 đang viết · ⬜ chưa viết. **Hiện không còn dòng ⬜ nào.**

### Vận hành và cài đặt

| Mã | Luồng | Trạng thái |
| --- | --- | --- |
| `V1` | [Cài đặt lần đầu — từ database trống tới hai tài khoản đăng nhập được](V1-cai-dat-lan-dau.md) | ✅ |
| `V2` | [Tạo một đơn vị mới](V2-tao-don-vi-moi.md) | ✅ |
| `V3` | [Ngưng và bật lại hoạt động của một đơn vị](V3-ngung-va-bat-lai-don-vi.md) | ✅ |
| `V4` | [Khôi phục quản trị đơn vị — vận hành đặt lại mật khẩu cho người giữ cửa quản trị của đơn vị](V4-khoi-phuc-quan-tri-don-vi.md) | ✅ |

### Danh tính

| Mã | Luồng | Trạng thái |
| --- | --- | --- |
| `D1` | [Đăng nhập](D1-dang-nhap.md) | ✅ |
| `D2` | [Đổi mật khẩu bắt buộc ở lần đăng nhập đầu](D2-doi-mat-khau-lan-dau.md) | ✅ |
| `D3` | [Quên mật khẩu và tự đặt lại](D3-quen-mat-khau.md) · **ngoài phạm vi v1** | ✅ |
| `D4` | [Quản trị đặt lại mật khẩu cho người khác](D4-quan-tri-dat-lai-mat-khau.md) | ✅ |
| `D5` | [Khoá và mở khoá một tài khoản](D5-khoa-va-mo-khoa-tai-khoan.md) | ✅ |
| `D6` | [Đăng xuất](D6-dang-xuat.md) | ✅ |

### Phân quyền

| Mã | Luồng | Trạng thái |
| --- | --- | --- |
| `P1` | [Tạo vai trò và gán quyền cho nó](P1-tao-vai-tro-va-gan-quyen.md) | ✅ |
| `P2` | [Gán vai trò cho một người dùng](P2-gan-vai-tro-cho-nguoi-dung.md) | ✅ |
| `P3` | [Menu hiện ra theo quyền của người đang đăng nhập](P3-menu-theo-quyen.md) | ✅ |

### Nghiệp vụ nền của Core

| Mã | Luồng | Trạng thái |
| --- | --- | --- |
| `N1` | [Tạo một người dùng mới](N1-tao-nguoi-dung-moi.md) | ✅ |
| `N2` | [Đính kèm tệp — tải lên, lưu, tải về, xoá](N2-dinh-kem-tep.md) | ✅ |
| `N3` | [Xuất dữ liệu theo bộ lọc đang xem](N3-xuat-du-lieu.md) | ✅ |
| `N4` | [Nhập dữ liệu từ tệp](N4-nhap-du-lieu-tu-tep.md) | ✅ |
| `N5` | [Thông báo đi qua Outbox tới đúng người nhận](N5-thong-bao-outbox.md) | ✅ |
| `N6` | [Ghi nhật ký kiểm toán](N6-nhat-ky-kiem-toan.md) | ✅ |
| `N7` | [Màn hình sinh từ metadata](N7-man-hinh-tu-metadata.md) · **ngoài phạm vi v1** | ✅ |
| `N8` | [FE báo lỗi về server](N8-fe-bao-loi-ve-server.md) | ✅ |
| `N9` | [Người dùng sửa hồ sơ của chính mình](N9-sua-ho-so-ca-nhan.md) | ✅ |

**Phép kiểm mục lục không lệch khỏi thư mục** — hai số phải bằng nhau:

```bash
ls docs/luong/*.md | grep -v README | wc -l
grep -cE '^\| `[A-Z][0-9]+` \| \[' docs/luong/README.md
```

## 5. Câu hỏi mở chặn pha nào

Mỗi luồng kết thúc bằng mục *"Câu chưa trả lời được"*. Bảng này **không chép lại** các câu đó — nó chỉ nói **khi nào phải trả lời**, để chúng trở thành một lịch thay vì một đống.

Tên pha theo [`../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md) §1.

| Pha | Xong thì có gì chạy được | Luồng phải giải xong mục §6 |
| --- | --- | --- |
| **B0** Nền móng | Project đúng chiều tham chiếu · envelope cả hai nhánh · thiếu cấu hình thì không khởi động · ArchTest đã từng đỏ | **Không luồng nào.** Bốn thứ này không chạm đơn vị, không chạm phân quyền |
| **B1** Dữ liệu, đơn vị, danh tính | Database dựng từ runbook · lệnh bootstrap dựng hai đơn vị và hai tài khoản qua service tạo đơn vị · card `auth.md` (trừ §10) và `profile.md` chạy đúng · mọi bảng mang cột đơn vị và bộ lọc đã bật | `V1` · `D1` · `D2` · `D6` · `N9` |
| **B2** Phân quyền, menu, bảo mật biên | Card `users.md` · `roles.md` · `permissions.md` · `meta-menu.md` · `auth.md` §10 chạy đúng · 403 bắn đúng chỗ · CSRF và rate limit chặn thật | `P1` · `P2` · `P3` · `N1` · `D4` · `D5` |
| **B3** Vận hành | Card `tenants.md` (gồm endpoint khôi phục quản trị đơn vị) · `client-errors.md` chạy đúng · nhật ký kiểm toán ghi được · chỉ số và health check đúng · tạo một đơn vị mới chạy lại được nhiều lần | `V2` · `V3` · `V4` · `N6` · `N8` |
| **B4** Tệp, nhập/xuất, thông báo | Card `files.md` · `exports.md` · `jobs.md` · `notifications.md` · đính kèm và tải lại có kiểm quyền · xuất theo bộ lọc đang xem · sự kiện sinh thông báo qua Outbox | `N2` · `N3` · `N4` · `N5` |
| — | ngoài phạm vi v1 | `D3` · `N7` |

🛑 **B0 không bị luồng nào chặn.** Đó là kết luận đáng giá nhất của bảng này: phần lớn câu hỏi mở là **quyết định mà ràng buộc thật mới ép ra được**, và chúng rơi vào B1 trở đi. Trả lời chúng trên giấy trước khi có code là lặp lại khuôn đã làm [`../adr/0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md`](../adr/0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md) bị lật trong vài giờ.

**Cách dùng bảng này:** bắt đầu một pha thì mở mục §6 của đúng nhóm luồng ở cột phải, quyết từng câu, rồi sửa **file chủ** — không sửa file luồng, vì luồng không giữ định nghĩa nào.

Đếm số câu đang mở, đừng chép số:

```bash
grep -c '^- \*\*' docs/luong/[A-Z][0-9]*.md | awk -F: '{s+=$2} END {print s}'
```

## 6. Khu này KHÔNG chứa gì

- **Luồng của một feature nghiệp vụ cụ thể** — thứ đó thuộc `spec/<feature>/`. Khu này chỉ chứa luồng của **Core**.
- **Luồng giao diện** — thứ tự màn hình, chuyển màn, câu chữ thuộc [`../Design/`](../Design/).
- **Hướng dẫn người dùng cuối** — khu này viết cho người **dựng** hệ thống, không cho người dùng nó.
