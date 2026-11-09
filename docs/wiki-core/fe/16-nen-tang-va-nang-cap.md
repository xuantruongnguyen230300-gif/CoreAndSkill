---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 16. Nền tảng chạy và chính sách nâng cấp

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu cả tệp 2026-11-06 bằng đọc tệp và `npm view`; kết quả build, test là của lượt nâng bản lớn cùng ngày, lượt đối chiếu không chạy lại). Hạng mục có thật và hạng mục chưa: bảng ở §11, bảng *có thật hôm nay → sẽ thành* ngay dưới nó. `verified:` giữ `chua-doi-chieu` vì lý do đó.
>
> Hai câu hỏi mà không ai đặt ra cho tới lúc đã quá muộn để trả lời rẻ: **"app này chạy được trên trình duyệt nào"** và **"bao lâu nâng framework một lần"**. File này trả lời trước cả hai.
>
> 📖 Phiên bản đã chốt của framework, thư viện UI, thư viện dịch và toolchain đi kèm, cùng **kế hoạch nâng bản lớn kế tiếp trước khi bản đang dùng hết hỗ trợ**: [`../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md). File này giữ **chính sách**; con số và lịch nằm ở ADR đó và ở `package.json` + `.nvmrc`.
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

### 4.1 Cơ chế vỡ không báo lỗi — rà trước khi tin build xanh

Mỗi dòng là một cơ chế đã đổi hành vi của Core khi nâng bản lớn của framework và thư viện UI mà không lỗi build nào báo. Ví dụ ghi bản lớn mà thay đổi xuất hiện — đó là sự kiện của bản phát hành, không phải bản đang ghim (`K47`).

| Cơ chế | Vì sao không báo lỗi | Chỗ bắt hôm nay |
| --- | --- | --- |
| Mã Core chọn phần tử bên trong component thư viện theo thuộc tính nội bộ (`data-pc-section`) | Thư viện đổi giá trị thì selector khớp không phần tử nào: không ngoại lệ, chỉ mất hành vi. Ví dụ: Menu của PrimeNG 21 đổi `data-pc-section="menuitem"` thành `"item"`, lớp vá của `app-menu` mất `menuitemradio` và `aria-checked` | Chú thích *Hỏng khi nào* cạnh selector — `src/FE/src/app/shared/ui/menu/menu.component.ts`, chuỗi `Hỏng khi nào: PrimeNG đổi`. Ca kiểm của `menu.component.spec.ts` tự dựng DOM mang sẵn giá trị thuộc tính, nên **không** bắt được lần đổi sau; lưới giao diện chưa có ca nào kiểm `menuitemradio` |
| Trường công khai của component thư viện đổi thành signal | Mã cũ đọc trường như giá trị và nhận về một hàm. Ví dụ: `container`, `containerViewChild` của PrimeNG 21 | `tsc` chặt bắt khi kiểu được khai; qua `any` hay ép kiểu thì lọt |
| Phần tử rời DOM bằng hoạt ảnh CSS thật thay vì gỡ đồng bộ | Phần tử còn trong DOM tới khi hoạt ảnh xong; ca kiểm đo ngay sau thao tác đóng thì đỏ. Ví dụ: lớp `p-leave-to` của PrimeNG 21 | Ca kiểm chờ điều kiện, không đo ngay — `src/FE/src/app/shared/ui/drawer/drawer.component.spec.ts`, chuỗi `Chờ điều kiện đúng.` |
| Dữ liệu locale CLDR đóng gói cùng framework | Tên khuôn (`'short'`, `'shortDate'`) đổi kết quả theo bản cài; build, test xanh. Ví dụ: `shortDate` của `vi` thành `d/M/yy` ở Angular 21 | Khuôn ngày giờ đi qua seam `CORE_I18N` — [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §5.5, luật F36 |
| Sàn trình duyệt mặc định của framework | Xê dịch theo bản cài, không cảnh báo | Khai tường minh — §5 |
| Lớp nổi neo để ở nơi gắn mặc định của thư viện | Lớp nổi vẫn dựng, vẫn hiện; hỏng chỉ lộ khi nút neo nằm trong một khung cuộn của trang và việc mở lớp nổi làm khung đó cuộn: hộp định vị lệch theo cha định vị (ô `sticky` của bảng), lời gọi focus vào hộp cuộn khung, rồi bộ nghe cuộn mà thư viện gắn lúc hiện đóng hộp. Dưới giảm chuyển động hay khi khung nhìn thấp, hộp đóng gần như ngay khi mở — trông như lỗi chuyển động. Ví dụ: PrimeNG 21 mặc định `'self'` (gói `primeng` đã cài, tệp `primeng-config.mjs`, chuỗi `overlayAppendTo = signal('self'`) — menu hàng của bảng mở xong đóng ngay, bảng gợi ý `Autocomplete` và ô số dòng `Paginator` đóng ngay. Mặc định đó không mới ở bản 21: PrimeNG 20 cũng mặc định `'self'` và gắn bộ nghe cuộn lúc hiện (đọc gói tải về ngoài repo, 2026-11-07). Lỗi được thấy lần đầu ở lần kiểm dưới cờ giảm chuyển động sau lượt nâng lên 21; thay đổi nào của bản 21 làm nó lộ ra thì chưa xác định | Luật F65 — `appendTo="body"` trên từng thẻ, ví dụ `src/FE/src/app/shared/ui/menu/menu.component.html`, chuỗi `appendTo="body"`. Thư viện đổi cách gắn hộp ở một lượt nâng sau thì hai ca Karma *lớp nổi gắn vào body* của `Menu`, `Autocomplete` đỏ; `Pagination` chỉ có ca lưới giao diện, chạy tay. Neo đầy đủ: ô *Ép bằng gì* của F65 ở [`../../RULES-FE.md`](../../RULES-FE.md) |

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
grep -cvE '^[[:space:]]*(#|$)' src/FE/.browserslistrc     # số dòng khai trình duyệt; PASS khi ≥ 1
```

---

## 9. Môi trường build cũng là một phiên bản phải chốt

Phiên bản của môi trường chạy công cụ build ít được nhắc, và là nguồn của loại lỗi khó chịu nhất: **"chạy được trên máy tôi"**.

| Việc | Vì sao |
| --- | --- |
| Ghim phiên bản môi trường build ở `.nvmrc` và trường `engines` của `package.json` — từ F0, hai tệp này cùng `package.json` là **nguồn phiên bản duy nhất** | Người mới vào dự án biết cần cài gì; công cụ cảnh báo khi lệch |
| Dùng lệnh cài đặt **theo tệp khoá**, không phải lệnh cài thông thường | Lệnh thông thường có thể sửa tệp khoá và cho ra cây phụ thuộc khác với cây đã kiểm |
| CI và máy dev dùng cùng phạm vi phiên bản | Lệch phiên bản môi trường build cho ra bundle khác nhau, và chỉ một trong hai được test |

Dòng thứ hai là dòng hay bị bỏ qua nhất và có hậu quả rõ nhất: một lần cài đặt "vô hại" trên CI có thể kéo về một bản vá mới của một phụ thuộc bắc cầu, và bản build ra khác với bản đã kiểm ở máy dev.

**Đây là lý do tệp khoá phải được commit** ([`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md)): nó là thứ duy nhất làm cho hai lần build ở hai chỗ cho ra cùng một kết quả.

**Chế độ biên dịch nghiêm ngặt cũng chốt một lần, và không nới** — `strict` của TypeScript cùng các cờ `strict…` của trình biên dịch Angular trong `tsconfig.json`. Nới thì dễ; siết lại trên một codebase đã lớn là một đợt sửa chạm mọi tệp, nên gần như không ai làm.

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

> Đối chiếu với `src/` ngày 2026-11-06 bằng đọc tệp và `npm view`; kết quả build, test là của lượt nâng bản lớn cùng ngày: hàng ghi ✅ CÓ THẬT có neo ở cột *Ghi chú*; hàng ghi 🚧 có bảng *có thật hôm nay → sẽ thành* ngay dưới bảng. Ý nghĩa ký hiệu: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Khai phạm vi trình duyệt tường minh | ✅ CÓ THẬT 2026-11-06 | §5 — từ pha F0, kèm ghi chú căn cứ; giá trị theo sàn mặc định của bản Angular đang ghim, người dùng chốt 2026-11-06. Neo: `src/FE/.browserslistrc` (chuỗi `Khai tường minh dù giá trị bằng đúng mặc định của Angular`, chuỗi `Điều kiện siết lại: có số liệu người dùng thật`) |
| Tệp khoá phụ thuộc được commit, phiên bản ghim | ✅ CÓ THẬT 2026-11-06 | §7, §9 — [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md). Neo: `src/FE/package-lock.json` (chuỗi `"lockfileVersion"`), `git ls-files src/FE/package-lock.json` in ra tệp đó; `grep -nE '"[~^]' src/FE/package.json` chỉ in dòng của `engines` — không phụ thuộc nào dùng dải |
| Không để tụt quá một bản lớn | ✅ CÓ THẬT 2026-11-06 | §2.1 — ba mốc ở §2.2. Lệnh §8 và `npm view @angular/core dist-tags`: bản lớn ghim ở `src/FE/package.json` (chuỗi `"@angular/core":`) kém nhãn `latest` một bậc — dưới mốc thứ ba. Mốc thứ nhất đối chiếu lịch hỗ trợ ở angular.dev, lượt này chưa đối chiếu. Số phiên bản không chép vào đây (`K47`) |
| Kiểm thư viện UI trước khi nâng | ✅ CÓ THẬT 2026-11-06 | §3 — lần nâng bản lớn 2026-11-06 không nâng framework trước thư viện UI: `npm view primeng@<bản ghim> peerDependencies` nhận đúng bản lớn `@angular/core` đang ghim; `@ngx-translate/core` khai dải mở phía trên (`>=`), không chặn. Bản ghim đọc ở `src/FE/package.json`, chuỗi `"primeng":`, chuỗi `"@ngx-translate/core":` |
| Checklist bốn mục sau mỗi lần nâng | 🚧 đang thi công | §4 — lần nâng 2026-11-06 có bằng chứng cho mục 2, 3, 4; mục 1 chưa. Bảng dưới |
| Ghim phiên bản môi trường build (`.nvmrc` + `engines`) | ✅ CÓ THẬT 2026-11-06 | §9 — từ pha F0. Neo: tệp `src/FE/.nvmrc`; `src/FE/package.json` (chuỗi `"engines": {`) — bản ở `.nvmrc` nằm trong dải `engines`; CI đọc đúng tệp đó và cài theo tệp khoá — `.github/workflows/docs-gate.yml` (chuỗi `node-version-file: src/FE/.nvmrc`, chuỗi `run: npm ci`) |
| Kế hoạch nâng bản lớn kế tiếp trước khi bản đang dùng hết hỗ trợ | ❌ chưa | Kế hoạch của lần nâng trước — quyết định 2 ở [`0028-toolchain-fe-va-ke-hoach-nang-cap.md:30`](../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md) — đã thi công 2026-11-06. Điều kiện: lập trước khi bản đang ghim chạm một mốc ở §2.2 — đọc bằng lệnh §8 và lịch hỗ trợ ở angular.dev. Kế hoạch nâng lên 22 có một điều kiện vào: việc chuyển bộ chạy test sang Vitest đang hoãn phải xong trước — quyết định 3 ở [`0126-hoan-chuyen-vitest-sang-pr-rieng-ngay-sau-pr-nang-angular-21-muon-nhat-truoc-angular-22.md`](../../adr/0126-hoan-chuyen-vitest-sang-pr-rieng-ngay-sau-pr-nang-angular-21-muon-nhat-truoc-angular-22.md) |
| Nâng theo lịch cố định | ❌ chưa | Điều kiện: hiện nâng theo ba mốc ở §2.2. Chuyển sang nhịp cố định khi đội đủ lớn để việc nâng có người chịu trách nhiệm |
| Cập nhật phụ thuộc tự động | ❌ chưa | Điều kiện: CI đủ tin cậy để một PR tự động xanh có nghĩa |
| Siết phạm vi trình duyệt | ❌ chưa | Điều kiện: có số liệu người dùng thật (§5.5) |
| Nâng framework trước khi thư viện UI sẵn sàng | ❌ loại, không hoãn `K46` | §3 — dẫn tới ép cài cho qua rồi vỡ ở chỗ phức tạp nhất |
| Chép số phiên bản vào tài liệu | ❌ loại, không hoãn `K47` | §8 — đọc bằng lệnh |

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** — hàng *Checklist bốn mục sau mỗi lần nâng* (đối chiếu 2026-11-06; kết quả chạy là của lượt nâng bản lớn cùng ngày, lượt đối chiếu không chạy lại):
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | Mục 2: `src/FE/src/app/app.config.ts` có `provideZonelessChangeDetection(),`, `grep -c 'zone.js' src/FE/package.json` in `0`, `ng test` xanh. Mục 3: `ng build` dưới ngân sách `initial` ở `src/FE/angular.json`, chuỗi `"type": "initial",`. Mục 4: `ng test` (bộ chạy ở `src/FE/angular.json`, chuỗi `"builder": "@angular/build:karma"`) và `npm run e2e` xanh. Mục 1: chỉ có lưới giao diện đo DOM theo spec `docs/Design/` và quét axe — không có bằng chứng so ảnh trước và sau đợt nâng | Mục 1 có bằng chứng so ảnh trước và sau đợt nâng — lưới không tự so, cái giá ghi ở [`0117-luoi-giao-dien-playwright-do-dom-theo-spec-design-axe-aa-api-gia-anh-chi-tham-khao.md:260`](../../adr/0117-luoi-giao-dien-playwright-do-dom-theo-spec-design-axe-aa-api-gia-anh-chi-tham-khao.md) |

> Cách đọc ba ký hiệu của bảng trên — và khi nào *"FE thiếu X"* là finding: [`../README.md`](../README.md) §9.

---

## 12. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Phiên bản đã chốt, toolchain, kế hoạch nâng cấp | [`../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md`](../../adr/0028-toolchain-fe-va-ke-hoach-nang-cap.md) |
| Ngân sách bundle | [`13-performance.md`](13-performance.md) §4 |
| Quét lỗ hổng phụ thuộc | [`14-security.md`](14-security.md) §7 |
| Cái gì được commit | [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) |
| Vì sao giữ cấu trúc thư mục thay vì workspace | [`../../adr/0007-fe-giu-cau-truc-thu-muc.md`](../../adr/0007-fe-giu-cau-truc-thu-muc.md) |
| Bọc thư viện UI để giảm chi phí nâng cấp | [`05-component-library.md`](05-component-library.md) §2 |
| Cổng F9 | [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
