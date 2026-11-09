---
kind: luat
scope: core
verified: 2026-10-05
---

# 2026-10-05 — Nhánh `path: ''` không component nuốt URL gốc

> Phát hiện khi mở ứng dụng trên máy dev (`ng serve`, BE chạy thật), đo bằng Chrome headless qua DevTools Protocol. Đối tượng: mẫu `app.routes.ts` ở [`../quy-uoc/fe-routing-guard.md`](../quy-uoc/fe-routing-guard.md) §2.1 và tệp `src/FE/src/app/app.routes.ts` dựng theo mẫu đó.
>
> Viết theo khuôn bốn phần ở [`README.md`](README.md).

---

## 1. Hiện tượng

Mở địa chỉ gốc của ứng dụng khi chưa đăng nhập: URL đứng yên ở `/`, vùng nội dung trắng — `<router-outlet>` gốc không dựng gì, tiêu đề tab giữ chuỗi tĩnh của `index.html`, console không có lỗi hay ngoại lệ nào. Request đáng chú ý duy nhất là `GET /api/v1/core/auth/me` trả 401, bình thường lúc khởi động khi chưa có phiên.

Cùng lúc đó hai đường khác vẫn đúng: mở `/trang-chu` khi chưa đăng nhập thì sang `/dang-nhap?returnUrl=%2Ftrang-chu`; mở `/dang-nhap` thì hiện form.

Spec điều hướng trên cấu hình route thật — `src/FE/src/app/app.routes.spec.ts`, khối `describe('app.routes — điều hướng URL gốc'`, chạy bằng `ng test` ngày 2026-10-05 — cho thấy hiện tượng rộng hơn quan sát trên trình duyệt: cả ba ca mở `/` (chưa đăng nhập, đã đăng nhập, bị buộc đổi mật khẩu) đều kết thúc ở `/`. Người đã đăng nhập gõ địa chỉ gốc cũng chỉ thấy trang trắng.

## 2. Nguyên nhân gốc

**Cơ chế.** `app.routes.ts` khai hai nhánh `path: ''` liền nhau. Nhánh đầu là nhánh xác thực: không component, `loadChildren` sang đúng hai route con `dang-nhap` và `doi-mat-khau-bat-buoc`. Nhánh sau là nhánh khung, và dòng chuyển hướng `{ path: '', pathMatch: 'full', redirectTo: 'trang-chu' }` nằm trong `children` của nó. Router thử các route anh em theo thứ tự và dừng ở route đầu tiên trả về kết quả khớp.

Với URL `/`, nhánh xác thực khớp `''` mà không tiêu segment nào; router nạp hai route con, không con nào khớp. Ở bước đó `@angular/router` — bản 20.3.31 đang cài, tệp `router2.mjs` trong thư mục `fesm2022` của gói — tách hai ca trong `Recognizer.processSegment`:

- URL **còn** segment chưa tiêu: trả `noMatch$1(segmentGroup)`, nhánh cha trượt, router thử anh em kế tiếp.
- URL **hết** segment: trả `of(new NoLeftoversInUrl())` — điều kiện là hàm `noLeftoversInUrl(segmentGroup, segments, outlet)`, đúng khi `segments.length === 0` — và nhánh cha được nhận là khớp với một cây con rỗng.

Nhánh khung vì thế không bao giờ được xét với URL rỗng: không `authGuard`, không dòng chuyển hướng. Router kết thúc một lần điều hướng **thành công** tới một route không component, nên không có gì để báo lỗi.

URL còn segment — `/trang-chu`, `/dang-nhap`, một URL lạ — đi đúng, vì ở đó nhánh xác thực trượt. Chỉ URL gốc hỏng.

**Vì sao không ai thấy sớm hơn.**

- Mẫu trong tài liệu luật mang đúng cấu trúc này, kèm chú thích nêu một giả định: nhánh xác thực không khai `''` hay `'**'`, nên URL nào nhánh đó không phục vụ sẽ sang nhánh sau. Giả định đúng với mọi URL trừ URL rỗng. Code dựng theo mẫu và chép cả chú thích, nên mẫu và code **khớp nhau** — lượt đối chiếu mẫu với code nào cũng chỉ xác nhận cái sai của bên kia.
- Không test nào điều hướng qua cấu hình route thật. Test của guard gọi thẳng hàm guard; test của route con dựng cấu hình riêng. Cả hai xanh trong khi `/` hỏng.
- Lỗi không để lại tín hiệu nào máy đang canh: biên dịch, lint, `ng build` đều sạch, console sạch.

## 3. Cách vá

| Phần | Làm gì | Vá gốc hay triệu chứng |
| --- | --- | --- |
| Khuôn route | Dòng chuyển hướng URL gốc thành phần tử đầu của `routes`, trước nhánh xác thực; dòng cùng hình dạng trong `children` của nhánh khung bị gỡ vì không còn tới được. Mẫu và các phương án đã loại: [`../quy-uoc/fe-routing-guard.md`](../quy-uoc/fe-routing-guard.md) §2.1, [`../wiki-core/fe/ly-do/fe-routing-guard.md`](../wiki-core/fe/ly-do/fe-routing-guard.md) §2.1 | Vá gốc cho URL gốc. Khuôn chạy thử trên một bản sao của `src/FE` ngày 2026-10-05: bốn ca của spec cùng sáu ca biên — `/dang-nhap` khi chưa và đã đăng nhập, URL lạ khi chưa và đã đăng nhập, `/?a=1`, màn đổi mật khẩu khi bị buộc đổi — đều xanh |
| `src/FE/src/app/app.routes.ts` | `frontend-expert` sửa theo khuôn trên ngày 2026-10-05: chuỗi `// URL gốc: phần tử ĐẦU, trước nhánh xác thực` đứng ngay trên dòng chuyển hướng, phần tử đầu của `routes` | Vá gốc cho URL gốc |
| URL của cả khu | Người dùng chốt ngày 2026-10-05: địa chỉ phải trỏ đúng một màn, còn lại về trang chủ. `app.routes.ts` có `{ path: 'quan-tri', pathMatch: 'full', redirectTo: '/trang-chu' }` và `{ path: 'he-thong', pathMatch: 'full', redirectTo: '/trang-chu' }` đứng trước `loadChildren` cùng path; `quan-tri.routes.ts`, `he-thong.routes.ts` không đổi. Đích tuyệt đối nên query của URL khu bị bỏ. Luật và lý do: §2.1 của hai tệp ở hàng đầu | Vá cho hai khu đang có. Khu thêm sau mà tầng gom không có route `''` vẫn mắc nếu thiếu dòng tương ứng |
| Test hồi quy | `src/FE/src/app/app.routes.spec.ts` điều hướng qua `routes` thật và khẳng định URL cuối theo trạng thái phiên — cho URL gốc, cho URL của cả khu kể cả khi có query, và cho màn con của khu. Đỏ ba ca URL gốc trên code trước bản sửa, đúng hiện tượng mục 1; xanh trên code đã sửa, chạy ngày 2026-10-05 | Canh hành vi của code. Tệp chưa vào git ở ngày ghi, nên job `frontend-gate` chưa chạy nó |

Ca URL của cả khu lộ ra trong cùng lượt điều tra: trên bản sao đã sửa URL gốc, `/quan-tri` và `/he-thong` khi đã đăng nhập dừng ở route `loadChildren` không component của chính khu đó, cây con rỗng — khung dựng, vùng nội dung trắng — thay vì rơi vào `'**'` của khung. Cùng cơ chế, ở tầng `<feature>.routes.ts` (§2.2 của tệp luật). Nó cần người dùng quyết URL khu dẫn tới đâu; quyết định và bản sửa ở hàng *URL của cả khu* trên.

Cả hai bản vá **chưa** chạm lớp lỗi: một route không component — nhánh `''` hay tầng gom `loadChildren` — mà không route con nào khớp vẫn nhận trọn URL đã hết segment, ở bất kỳ chỗ nào sau này có hình dạng đó. Thứ chặn nó lần sau là cổng ở mục 4, không phải hai dòng chuyển hướng.

## 4. Cổng nào lẽ ra phải bắt

**Một spec điều hướng qua cấu hình route thật.** Lớp lỗi là *thứ tự khớp route*: hành vi của nó chỉ có trong router lúc chạy, không có trong từng tệp route nhìn riêng. Không lint, không phép dò văn bản nào thấy được, vì mọi dòng đều hợp lệ. Chỉ chạy router trên đúng mảng `routes` mà ứng dụng dùng mới thấy. Spec ở mục 3 là cổng đó, và nó đỏ ngay ở lần chạy đầu.

Luật đề xuất để giữ cổng này — **chưa cấp mã, chờ người dùng chốt trước khi vào [`../RULES-FE.md`](../RULES-FE.md):**

> Mỗi lối vào cấp một của bản đồ route ([`../quy-uoc/fe-routing-guard.md`](../quy-uoc/fe-routing-guard.md) §1) — URL gốc, hai màn xác thực, một URL lạ, và đoạn đầu của mỗi khu trong khung — có ca điều hướng chạy trên `routes` thật trong `app.routes.spec.ts`, khẳng định đích cuối theo trạng thái phiên. Mức: MUST. Ép bằng gì: `ng test` cho các ca đã có. **Hở:** lối vào mới thêm vào §1 mà không thêm ca thì không gì đỏ — phần đó chỉ review canh.

Áp phép thử ở [`README.md`](README.md) §8:

- **Quay ngược thời gian thì có bắt được sự cố này không?** Có — spec này đỏ ba ca trên chính cấu hình gây ra sự cố.
- **Có bắt được một sự cố khác cùng lớp không?** Có, nếu ca cho đoạn đầu của khu được viết: `/quan-tri` và `/he-thong` ở mục 3 là đúng loại đó. Cũng bắt được nhánh đặt sai thứ tự và `'**'` đứng trước route nó nuốt. Không bắt được lối vào chưa có ca — phần hở nêu trong luật.

Ca này **không** tính là lần tái diễn của dòng nợ **D44** ở [`../DEBT.md`](../DEBT.md): D44 là mẫu lệch khỏi code đã sửa, còn ở đây mẫu và code khớp nhau và cùng sai. Cổng mà D44 dự tính — dấu vết mẫu↔tệp code — cũng không bắt được ca này.

## 5. Bài học tổng quát

**Một câu trong tài liệu luật nói về hành vi của framework là một dự đoán cho tới khi có test chạy framework thật để kiểm nó.** Chú thích *"không khai `''` nên sẽ trượt sang nhánh sau"* đọc như một sự thật về router, nhưng chưa ai chạy router với nó. Đúng ở mọi ca người viết nghĩ tới; sai ở ca biên duy nhất không ai thử — URL rỗng.

Hai hệ quả:

1. Khi mẫu dựa vào cách framework giải một cấu hình — thứ tự khớp, kế thừa tham số, thời điểm guard chạy — thì test của mẫu phải chạy **cấu hình thật qua framework thật**, không gọi riêng từng mảnh. Gọi riêng chỉ kiểm từng mảnh, không kiểm cách framework ghép chúng.
2. Một mẫu được code chép đúng nguyên văn thì lượt đối chiếu mẫu với code không còn là lớp kiểm độc lập: hai bản cùng một nguồn, cùng sai một chỗ.

Cùng họ với [`2026-09-21-mau-ma-trong-luat-khong-ai-chay.md`](2026-09-21-mau-ma-trong-luat-khong-ai-chay.md) — mẫu mã trong luật chưa từng chạy. Khác ở chỗ ca đó lộ ra khi code đã rời mẫu; ca này lộ ra vì code bám mẫu quá sát.
