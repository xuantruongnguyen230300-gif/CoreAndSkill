---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 16. Nền tảng chạy và chính sách nâng cấp

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`, nên chưa có `package.json` để đối chiếu.
>
> Hai câu hỏi mà không ai đặt ra cho tới lúc đã quá muộn để trả lời rẻ: **"app này chạy được trên trình duyệt nào"** và **"bao lâu nâng framework một lần"**. File này trả lời trước cả hai.
>
> Không thuộc file này: quét lỗ hổng phụ thuộc — [`14-security.md`](14-security.md) §7. Ngân sách bundle — [`13-performance.md`](13-performance.md) §4.

---

## 1. Vì sao đây không phải việc "để sau"

Angular ra bản lớn mỗi sáu tháng, mỗi bản được hỗ trợ khoảng mười tám tháng. Bỏ qua ba kỳ liên tiếp là rơi ra khỏi vùng hỗ trợ — và lúc đó không còn bản vá bảo mật nào.

Cái đắt **không** phải một lần nâng. Cái đắt là nhảy nhiều bản lớn cùng lúc: mỗi bản có đường di trú riêng chạy được tự động; nhảy cóc thì các bước tự động không áp được theo thứ tự, và phải sửa tay toàn bộ thay đổi phá vỡ của cả mấy kỳ trong một lần, trên một codebase đã lớn hơn nhiều.

> Đây là khoản nợ **duy nhất** trong toàn bộ tài liệu FE tự tăng theo thời gian mà không cần ai viết thêm dòng code nào.

---

## 2. Chính sách phiên bản

### 2.1 Nguyên tắc

| Loại | Chính sách |
| --- | --- |
| Bản vá và bản nhỏ | Gộp vào PR thường, không cần quyết định riêng |
| Bản lớn | Cần người quyết tường minh, có kế hoạch, có kiểm sau nâng |
| **Không để tụt quá một bản lớn** | Đây là ranh giới giữa "một bước nâng cấp" và "một dự án riêng" |

### 2.2 Ba mốc buộc phải nâng, không thương lượng

| Mốc | Vì sao không hoãn tiếp được |
| --- | --- |
| Phiên bản đang dùng hết hạn hỗ trợ | Không còn bản vá bảo mật. Mốc cứng |
| Một lỗ hổng nghiêm trọng **chỉ** vá được bằng nâng bản lớn | Lúc đó nâng là bắt buộc và gấp — tình huống tệ nhất để nâng |
| Đã tụt hai bản lớn | Vượt ngưỡng ở §2.1 |

Ghi ba mốc này ra để lần sau không ai phải suy đoán lý do, và để việc hoãn — nếu có — là một quyết định có ý thức chứ không phải một sự trôi đi.

### 2.3 Thứ tự khi nâng

Framework trước, thư viện bên thứ ba sau. **Không gộp một lần.** Gộp thì khi có gì vỡ, không biết vỡ vì cái nào — và phải tách ra để tìm, tức mất chính thời gian mà việc gộp định tiết kiệm.

---

## 3. Ràng buộc thật: thư viện UI quyết định nhịp, không phải framework

Đây là chỗ kế hoạch ở §2 hay vỡ trong thực tế.

Framework nâng được ngay ngày ra bản mới. **Thư viện UI thường sau vài tuần tới vài tháng.** Và thư viện UI là thứ mà toàn bộ bảng, form, hộp thoại của hệ thống này đứng trên.

> **Luật: không nâng bản lớn của framework trước khi thư viện UI có bản hỗ trợ.**

Nâng trước sẽ dẫn tới việc ép cài cho qua, rồi phát hiện vỡ ở đúng những component phức tạp nhất, lúc đã không lùi được nữa.

Kiểm trước khi bắt đầu — hai lệnh, đọc kết quả thay vì đoán:

```bash
cd src/FE
npm outdated
npm info primeng peerDependencies
```

**Khi bản vá bảo mật đòi nâng bản lớn mà thư viện UI chưa sẵn sàng:** ghi đè phiên bản cho đúng gói bắc cầu bị ảnh hưởng, **không** nâng cả framework. Ghi lý do ngay cạnh chỗ ghi đè — thứ đó phải được gỡ khi nâng thật, và sẽ không ai nhớ nếu không viết ra.

Danh sách phụ thuộc phải kiểm khi nâng gồm cả những gói **không** theo nhịp phát hành của framework (thư viện dịch, bộ locale của thư viện UI). Chúng không đổi ba mốc ở §2.2, nhưng chúng có thể vỡ.

---

## 4. Sau mỗi lần nâng — kiểm gì

Build xanh **không** đủ. Bốn thứ hay vỡ âm thầm, xếp theo khả năng vỡ:

| # | Kiểm gì | Vì sao dễ vỡ |
| --- | --- | --- |
| 1 | **Giao diện của thư viện UI** | Cơ chế theme là API đổi thường xuyên giữa các bản lớn. So bằng ảnh chụp màn hình, không nhìn lướt |
| 2 | **Chế độ zoneless** | Thay đổi trong cơ chế phát hiện thay đổi tác động thẳng vào đây — [`13-performance.md`](13-performance.md) §2 |
| 3 | **Kích thước bundle** | So với ngân sách; một bản lớn có thể đổi cách chia chunk |
| 4 | **Bộ test** | Bộ chạy test và API test cũng đổi giữa các bản |

Dòng 1 đáng nhấn mạnh: nó vỡ theo kiểu **trông vẫn chạy**. Không lỗi, không cảnh báo, chỉ là một vài chỗ màu sai hoặc khoảng cách sai. Không so ảnh thì phát hiện bằng người dùng.

---

## 5. Phạm vi trình duyệt — khai TƯỜNG MINH

### 5.1 Luật

> **Khai danh sách trình duyệt hỗ trợ một cách tường minh trong cấu hình dự án, kể cả khi giá trị bằng đúng mặc định của framework.**

### 5.2 Vì sao khai kể cả khi không đổi giá trị

Đây là chỗ dễ hiểu nhầm nhất trong file này. Khai một danh sách bằng đúng mặc định **không đổi hành vi hôm nay**. Thứ nó đổi là:

| Trước khi khai | Sau khi khai |
| --- | --- |
| Phạm vi trình duyệt do framework quyết định | **Do đội quyết định** |
| Nâng framework → danh sách tự trôi, không ai được báo | Nâng framework → nếu mặc định của nó xê dịch, **diff ở tệp cấu hình là chỗ duy nhất thấy được** |
| Không có gì để tranh luận khi cần thu hẹp | Có một dòng cụ thể để sửa, kèm lý do |

Nói cách khác: khai tường minh biến một mặc định vô hình thành một **quyết định có chủ**.

### 5.3 Ghi lại căn cứ ngay cạnh danh sách

Danh sách không có lý do sẽ bị sửa tuỳ tiện. Ghi ngay cạnh: nguồn của giá trị hiện tại, và điều kiện để thay đổi nó (có số liệu người dùng thật).

### 5.4 Cả hai chiều đều xấu

| Quá rộng | Quá hẹp |
| --- | --- |
| Bundle phình vì phải hạ cấp cú pháp và thêm mã đệm cho trình duyệt không ai dùng | Có máy trong cơ quan mở lên là trắng trang |
| Ăn thẳng vào ngân sách ở [`13-performance.md`](13-performance.md) §4 | Lỗi này **không** xuất hiện trên máy dev, nên không ai biết cho tới khi người dùng báo |

Cột phải nguy hiểm hơn: nó là dạng hỏng im lặng, và nó xảy ra với đúng những người dùng ít có khả năng tự chẩn đoán nhất.

### 5.5 Khi nào siết lại

Khi có số liệu người dùng thật. Trước đó, giá trị đúng nhất là **chép đúng hành vi đang chạy** — vì đoán một danh sách khác là đổi hành vi mà không có căn cứ nào, và cũng không sinh cảnh báo về trình duyệt không hỗ trợ.

---

## 6. Phát hiện thay đổi phá vỡ

Ba nguồn, dùng theo thứ tự:

| Nguồn | Bắt được gì |
| --- | --- |
| Ghi chú phát hành và hướng dẫn di trú chính thức | Thay đổi có chủ đích, có đường xử lý |
| Bộ chuyển đổi tự động của công cụ | Phần lớn thay đổi cú pháp; **không** bắt được thay đổi hành vi |
| Cảnh báo ngừng hỗ trợ trong bản hiện tại | Thứ **sẽ** vỡ ở bản sau — đây là nguồn rẻ nhất và bị bỏ qua nhiều nhất |

**Dòng thứ ba là chỗ đáng đầu tư nhất.** Xử lý cảnh báo ngừng hỗ trợ ngay khi thấy, ở bản hiện tại, biến một đợt nâng cấp lớn thành một loạt thay đổi nhỏ. Bỏ qua chúng thì chúng dồn lại và cùng nổ vào ngày nâng cấp.

Hệ quả cho quy ước hằng ngày: **không để cảnh báo tích tụ trong đầu ra build.** Một đầu ra build đầy cảnh báo là một đầu ra không ai đọc, và cảnh báo quan trọng sẽ lẫn vào đó.

---

## 7. Ba luật đi kèm, ép được bằng cổng

| Luật | Ép bằng |
| --- | --- |
| Không còn cú pháp framework đã lỗi thời trong mã nguồn (luật F9) | Cổng — [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
| Tệp khoá phụ thuộc được commit | [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) |
| Phiên bản ghim, không dùng dải phiên bản mở | Cùng trên |

Luật F9 có vai trò kép thường bị bỏ qua: ngoài việc giữ mã nguồn hiện đại, nó **chống trôi ngược**. Cú pháp cũ vẫn chạy được trong nhiều bản, nên không có gì tự nhiên ngăn một người quen tay viết lại kiểu cũ — và mã cũ lẫn vào mã mới làm codebase có hai phong cách.

---

## 8. Đếm bằng lệnh, đừng chép phiên bản

Theo [`../../../.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §6, không chép số phiên bản vào tài liệu — chúng sẽ sai và không ai sửa. Khi `src/FE` tồn tại:

```bash
grep -E '"@angular/core"|"primeng"' src/FE/package.json   # đang ở bản nào
cd src/FE && npm outdated @angular/core primeng           # rỗng = chưa tụt
grep -c 'browserslist' src/FE/package.json                # PASS khi ≥ 1
```

---

## 9. Môi trường build cũng là một phiên bản phải chốt

Phiên bản của môi trường chạy công cụ build ít được nhắc, và là nguồn của loại lỗi khó chịu nhất: **"chạy được trên máy tôi"**.

| Việc | Vì sao |
| --- | --- |
| Khai phạm vi phiên bản môi trường build trong cấu hình dự án | Người mới vào dự án biết cần cài gì; công cụ cảnh báo khi lệch |
| Dùng lệnh cài đặt **theo tệp khoá**, không phải lệnh cài thông thường | Lệnh thông thường có thể sửa tệp khoá và cho ra cây phụ thuộc khác với cây đã kiểm |
| CI và máy dev dùng cùng phạm vi phiên bản | Lệch phiên bản môi trường build cho ra bundle khác nhau, và chỉ một trong hai được test |

Dòng thứ hai là dòng hay bị bỏ qua nhất và có hậu quả rõ nhất: một lần cài đặt "vô hại" trên CI có thể kéo về một bản vá mới của một phụ thuộc bắc cầu, và bản build ra khác với bản đã kiểm ở máy dev.

**Đây là lý do tệp khoá phải được commit** ([`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md)): nó là thứ duy nhất làm cho hai lần build ở hai chỗ cho ra cùng một kết quả.

---

## 10. Kiểm chứng

- [ ] Danh sách trình duyệt khai tường minh trong cấu hình, kèm ghi chú căn cứ
- [ ] Tệp khoá phụ thuộc được commit; không phụ thuộc nào dùng dải phiên bản mở
- [ ] Đầu ra build không có cảnh báo ngừng hỗ trợ nào chưa xử lý
- [ ] Không còn cú pháp framework lỗi thời (luật F9)
- [ ] Trước mỗi lần nâng bản lớn: đã kiểm thư viện UI có bản hỗ trợ
- [ ] Sau mỗi lần nâng: đã so ảnh chụp màn hình, kiểm zoneless, kiểm bundle, chạy test

---

## 11. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Ý nghĩa ba ký hiệu trạng thái: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Khai phạm vi trình duyệt tường minh | ✅ sẽ có | §5 — từ pha F0, kèm ghi chú căn cứ |
| Tệp khoá phụ thuộc được commit, phiên bản ghim | ✅ sẽ có | §7, §9 — [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) |
| Không để tụt quá một bản lớn | ✅ sẽ có | §2.1 — ba mốc bắt buộc ở §2.2 |
| Kiểm thư viện UI trước khi nâng | ✅ sẽ có | §3 — ràng buộc thật quyết định nhịp nâng |
| Checklist bốn mục sau mỗi lần nâng | ✅ sẽ có | §4 |
| Khai phạm vi phiên bản môi trường build | ✅ sẽ có | §9 |
| Nâng theo lịch cố định | ❌ chưa | Điều kiện: hiện nâng theo ba mốc ở §2.2. Chuyển sang nhịp cố định khi đội đủ lớn để việc nâng có người chịu trách nhiệm |
| Cập nhật phụ thuộc tự động | ❌ chưa | Điều kiện: CI đủ tin cậy để một PR tự động xanh có nghĩa |
| Siết phạm vi trình duyệt | ❌ chưa | Điều kiện: có số liệu người dùng thật (§5.5) |
| Nâng framework trước khi thư viện UI sẵn sàng | ❌ loại, không hoãn `K46` | §3 — dẫn tới ép cài cho qua rồi vỡ ở chỗ phức tạp nhất |
| Chép số phiên bản vào tài liệu | ❌ loại, không hoãn `K47` | §8 — đọc bằng lệnh |

Một finding dạng *"FE thiếu X"* chỉ hợp lệ khi X mang trạng thái **✅ sẽ có** mà vắng mặt, hoặc khi điều kiện ở cột ghi chú của một dòng **❌ chưa** đã xảy ra. Dòng **❌ loại, không hoãn** chỉ đổi được bằng một ADR mới, không đổi được bằng một finding.

---

## 12. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Ngân sách bundle | [`13-performance.md`](13-performance.md) §4 |
| Quét lỗ hổng phụ thuộc | [`14-security.md`](14-security.md) §7 |
| Cái gì được commit | [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) |
| Vì sao giữ cấu trúc thư mục thay vì workspace | [`../../adr/0007-fe-giu-cau-truc-thu-muc.md`](../../adr/0007-fe-giu-cau-truc-thu-muc.md) |
| Bọc thư viện UI để giảm chi phí nâng cấp | [`05-component-library.md`](05-component-library.md) §2 |
| Cổng F9 | [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
