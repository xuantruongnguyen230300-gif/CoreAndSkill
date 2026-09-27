---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0075 — `Menu`, `DatePicker`, `Autocomplete` vào **nhóm B**; bậc thang thời lượng chọn theo **VAI trong `DESIGN.md` §7**, không theo con số trùng; lượt rà `120ms` đóng lại

> **Trạng thái:** Đã chấp nhận (2026-09-23)

## Bối cảnh

Hai câu hỏi còn mở sau [ADR-0073](0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) cùng về bàn một lượt. Chúng **khoá vào nhau**, và tách ra quyết định riêng thì mỗi câu trả lời sẽ phá câu kia — đó là lý do ADR này có hai vế thay vì hai ADR.

### Vế 1 — ba component lớp nổi

`design-expert` đo và tìm ra: `Menu`, `DatePicker`, `Autocomplete` mở lớp nổi bằng **hoạt ảnh Angular với hai input rời** (`showTransitionOptions` / `hideTransitionOptions`) — **đúng cùng cơ chế với `Toast`**, thứ mà ADR-0073 quyết định 3 và 6 đã trao quyền đặt số qua hằng số có tên.

Nên câu mà ba spec đang mang — *"spec không đặt được thời lượng cho ba component này"* — là **sai**. Câu đúng: *"spec chưa được giao quyền đặt"*. Hai câu nghe giống nhau và dẫn tới hai việc khác hẳn: câu thứ nhất là một giới hạn của nền tảng, câu thứ hai là một khoảng trống thẩm quyền.

ADR-0073 tự đặt ranh giới cho tiền lệ `Tooltip` bằng một câu dứt khoát: *"spec nhường khi không dựng được"*, không phải *"spec nhường khi khó"*. Theo đúng ranh giới đó, ba component này **dựng được**, nên chúng không thuộc ca `Tooltip`. Quyết định 9 của ADR-0073 xếp chúng vào ca *"spec thiếu, viết một câu nói rõ `.12s` là trùng số"* — đó là phương thuốc đúng cho một chẩn đoán chưa đủ: chẩn đoán đúng là **thiếu chủ**, và một câu văn xuôi không đặt được chủ cho một con số.

### Vế 2 — lượt rà `120ms` mà ADR-0073 đặt hàng

ADR-0073 §Hệ quả tiêu cực gạch 2 để lại một việc: khoá `semantic.transitionDuration` nuôi cả `button`, `select`, `inputtext`, `chip`, `tooltip` và mọi sub-preset thêm sau, *"không ai đã xem `120ms` có đúng cho từng chỗ đó không"*, với rủi ro nêu đích danh — *"một chuyển tiếp vốn dịu ở `200ms` trở nên **giật** ở `120ms`, và không test nào bắt"*.

`design-expert` rà và **không tìm thấy chỗ nào sai**. Lập luận của nó là cấu trúc chứ không phải cảm tính: đọc 13 sub-preset, lần chuỗi token về khoá chung, rồi mở CSS từng cái xem **thuộc tính nào thật sự ăn khoá**. Kết quả: mọi consumer là chuyển tiếp **chỉ-sơn-màu** — `background`, `color`, `border-color`, `outline-color`, `box-shadow`. Không `transform`, không `opacity`, không `width`/`height`, không `animation`. Toàn bộ chuyển động **hình học** nằm ngoài khoá và đi bằng số trần.

Kiến trúc sư đối chiếu kết quả đó với [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 ngày 2026-09-23 và xác nhận một điều mà báo cáo chưa nói thành lời: **tập consumer đo được trùng đúng vai mà §7 giao cho `--dur-fast` — "đổi màu khi hover/focus"**. Không phải "xấp xỉ đúng vai"; đúng vai.

Hai hệ quả:

- Rủi ro mà ADR-0073 nêu **đi ngược chiều thực tế**. Rút ngắn một chuyển tiếp sơn màu làm **giảm** số khung hình phải sơn; nó không thể sinh ra giật. Giật là khung hình bị bỏ, và đây là ít việc hơn chứ không nhiều hơn.
- Rủi ro còn lại là một rủi ro **khác**, chưa ai viết ra: một chuyển tiếp có thể thành **cộc** — đổi màu xong quá gọn so với ý đồ. Cộc là chuyện thẩm mỹ, đo bằng mắt; giật là chuyện hiệu năng, đo bằng máy. Gộp hai thứ vào một chữ là lý do câu hỏi này suýt bị đóng sai.

### Chỗ hai vế khoá vào nhau

`.12s` của ba component ở vế 1 trùng đúng giá trị `--dur-fast`. Cách "tiết kiệm" nhất để đóng vế 1 là treo cả ba lên `--dur-fast`: con số không đổi, không ai thấy gì khác, xong.

Làm vậy thì `--dur-fast` có **vai thứ hai** — mở/đóng lớp nổi, tức một chuyển động `transform`/`opacity`. Và ngay khoảnh khắc đó, lập luận đóng vế 2 **mất hiệu lực**: nó đứng được chính vì tập consumer của khoá là thuần sơn màu. Đóng vế 2 buổi sáng rồi phá nền của nó buổi chiều là cái bẫy mà lượt này phải tránh, và nó chỉ nhìn thấy được khi hai vế nằm trên cùng một bàn.

## Quyết định

Kiến trúc sư chốt:

1. **`Menu`, `DatePicker`, `Autocomplete` vào nhóm B.** Ba spec được quyền đặt thời lượng và đường cong mở/đóng lớp nổi; số sống trong hằng số TypeScript có tên cạnh component bọc, mỗi hằng nêu đích danh token nó phản chiếu — đúng khuôn `Toast`, luật **F37** ([`../RULES.md`](../RULES.md) §7). Việc này **sửa một phần quyết định 9 của [ADR-0073](0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md)**: phần phân loại *"spec thiếu, không phải code lệch"* vẫn đúng; phần phương thuốc *"viết một câu nói rõ đây là trùng số"* hết hiệu lực, vì con số nay có chủ.
2. **Bậc thang chọn theo VAI mà [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 giao cho token, KHÔNG theo con số đang trùng.** Mở/đóng một lớp nổi là vai của `--dur-base` — bậc mà §7 đã giao cho *"mở/đóng `Dialog`, trượt drawer, chuyển tab"*. Ba component vì thế nối về **`--dur-base`**, **không** về `--dur-fast`. Hệ quả nhìn thấy được: nhịp mở dài ra, nhịp đóng dài ra; con số trùng `.12s` biến mất, và biến mất là **đúng** — nó chưa bao giờ là một phép nối.
3. **Một hằng thời lượng dùng cho cả hai chiều; hai hằng đường cong.** Chiều vào lấy vai `--ease-decelerate`, chiều ra lấy vai `--ease-accelerate`, theo đúng phân vai của §7. Bất đối xứng **thời lượng** của thư viện (`.12s` vào, `.1s` ra) không được giữ: nó là mặc định của gói, chưa bao giờ là một ý đồ thiết kế của hệ này, và thang token không có bậc nào ở giá trị đó. Bất đối xứng **đường cong** thì giữ, vì §7 khai đích danh hai vai vào/ra.
4. **Luật chung cho mọi hằng nhóm B: giá trị phải rơi đúng một bậc của thang `--dur-*`.** Không bậc nào vừa thì câu trả lời **không** phải thêm một bậc — đó là lấy bậc gần nhất **theo vai**, và mong muốn một giá trị ngoài thang trở thành một câu hỏi cho §7 của [`../Design/DESIGN.md`](../Design/DESIGN.md), tức một quyết định của hệ thiết kế chứ không của một component. Thêm một bậc để khớp một mặc định của thư viện bên thứ ba là đảo ngược quyền sở hữu mà cả trục này sinh ra để dựng.
5. **Lượt rà `120ms` ĐÓNG — cho rủi ro đã nêu, không cho rủi ro chưa nêu.** Rủi ro *"giật"* của ADR-0073 §Hệ quả tiêu cực gạch 2 được bác bằng cấu trúc: khoá chỉ nuôi chuyển tiếp sơn màu, và rút ngắn một chuyển tiếp sơn màu không sinh ra khung hình bị bỏ. Rủi ro *"cộc"* thì **không** đóng — nó cần mắt người trên ứng dụng đang chạy, và không lượt đọc tĩnh nào thay được. Nó không trở thành một dòng nợ: nó là một **việc của một lượt rà giao diện** sau khi FE chạy được, cùng lúc với mọi phán xét thẩm mỹ khác.
6. **Dòng F37 ở [`../RULES.md`](../RULES.md) §7 thôi liệt kê tên component.** Phạm vi khai bằng **tiêu chí** — hằng thời lượng viết trong TypeScript dưới lớp bọc, vì tham số của thư viện là chuỗi không giải `var()` — đúng bằng thứ cổng đang quét. Danh sách ba cái tên là một chỗ mục ruỗng đã mục ngay lượt này; cổng chưa bao giờ đọc nó.

## Phương án đã cân nhắc và vì sao loại

### Vế 1 — Phương án A: giữ nguyên quyết định 9 của ADR-0073

Đây là **phương án đơn giản nhất**: không sửa gì, ba spec giữ câu cảnh báo *"`.12s` là trùng số, đừng đóng thành đã đạt"*, thư viện giữ quyền quyết.

**Được:** không dòng mã nào thêm, không hằng số nào thêm, và cái giá *"giá trị sống ở hai nơi"* mà ADR-0073 đã nhận không lớn thêm. Ba lượt rà sau này được cảnh báo tử tế.

**Mất:** nó để một con số không có chủ ở giữa một trục vừa tốn cả một ADR để đặt chủ cho mọi con số khác. Và nó tạo ra một chỗ **lệch trong lòng một component**: sau khi thi hành ADR-0073 quyết định 1, nhịp đổi màu của một mục menu chạy theo token, còn nhịp mở của chính hộp menu đó thì không. Đổi token một lần là hai nhịp rời nhau, trong cùng một thứ người dùng nhìn thấy cùng một lúc. Thứ duy nhất canh chỗ đó là một câu văn xuôi trong spec — và văn xuôi là mức ép yếu nhất repo này có.

**Vì sao loại:** ranh giới mà chính ADR-0073 vạch cho tiền lệ `Tooltip` loại nó. Ba component này **dựng được**, nên nhường quyền ở đây là vứt bỏ một quyền chứ không phải thừa nhận một giới hạn — đúng câu mà ADR-0073 dùng để loại phương án A của vế 2 của nó.

### Vế 1 — Phương án B: vào nhóm B nhưng nối về `--dur-fast`, giữ nguyên con số

**Được:** rẻ nhất trong các phương án "có làm gì đó". Con số không đổi nên không ai phải xem lại cảm giác của ba component; `.12s` thành `120ms` và phép nối thành thật. Nhìn qua thì đây là phương án thắng: nó mua quyền sở hữu mà không trả gì.

**Mất:** nó trả bằng một thứ không hiện ra ở chỗ trả. `--dur-fast` nhận vai thứ hai, và vai đó là một chuyển động `transform`/`opacity` — đúng loại mà §7 tách khỏi vai sơn màu. Từ đó: lập luận đóng vế 2 sập; mọi lượt rà `--dur-fast` về sau phải hỏi *"chỗ này là sơn màu hay là hình học"* trước khi kết luận được gì; và một lần chỉnh `--dur-fast` cho hợp với hover sẽ đổi nhịp mở của ba lớp nổi mà không ai nghĩ tới.

**Vì sao loại:** nó khớp **con số** thay vì khớp **vai**, và con số đang trùng là trùng ngẫu nhiên — chính ba spec đã nói thế bằng chữ đậm. Chốt theo nó là lấy đúng cái trùng ngẫu nhiên ấy làm căn cứ kiến trúc.

### Vế 1 — Phương án C: thêm một bậc thang cho giá trị của thư viện

**Được:** giữ được cả bất đối xứng lẫn con số hiện tại; không component nào đổi cảm giác.

**Vì sao loại:** thang thời lượng là một quyết định của hệ thiết kế, khai ở §7 của [`../Design/DESIGN.md`](../Design/DESIGN.md). Thêm một bậc để khớp mặc định của PrimeNG là để một gói bên thứ ba viết vào hệ thiết kế của mình — và bậc đó sẽ không có vai nào để khai ngoài *"vì thư viện làm thế"*. Quyết định 4 đóng hẳn đường này cho cả những lần sau.

### Vế 2 — Phương án A: không đóng, chờ chạy được ứng dụng rồi rà bằng mắt

**Được:** thành thật tuyệt đối với giới hạn mà `design-expert` tự khai — toàn bộ là đọc tĩnh, không chạy ứng dụng.

**Mất:** nó gộp lại hai câu hỏi mà phép đo vừa tách ra được. Câu *"có giật không"* đã có câu trả lời **cấu trúc**, không cần chạy: một chuyển tiếp sơn màu ngắn đi thì sơn ít khung hình hơn. Giữ nó mở nghĩa là mỗi lượt rà sau phải đọc lại 13 sub-preset để tự đi tới đúng kết luận đó — và lần thứ ba thì sẽ có người đóng bừa.

**Vì sao loại:** để mở một câu hỏi **đã** trả lời được không phải là thận trọng, nó là đùn việc. Cái cần giữ mở là câu hỏi thẩm mỹ, và quyết định 5 giữ đúng câu đó mở.

### Vế 2 — Phương án B: đóng cả hai rủi ro, coi lượt rà là xong hẳn

**Được:** dứt điểm, không để lại đuôi.

**Vì sao loại:** `design-expert` không chạy ứng dụng, và nó nói ra điều đó. Đóng phần "cộc" dựa trên một lượt đọc tĩnh là đúng khuôn mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 cấm — dán nhãn xong cho thứ chưa ai mở ra xem. Cái giá của việc đóng bừa ở đây lại đúng bằng cái nó định mua: một lượt rà giao diện sau này.

## Hệ quả

### Tích cực

- `--dur-fast` giữ **đúng một vai**, và vai đó khớp đúng tập consumer đo được. Lượt rà sau trả lời được *"khoá này nuôi cái gì"* bằng một câu, không bằng một cuộc điều tra.
- Sáu component nhóm B đi cùng một khuôn, và khuôn đó có tiêu chí thay vì có danh sách — nên component bọc thứ bảy chạy hoạt ảnh Angular tự thuộc phạm vi mà không ai phải nhớ thêm tên vào đâu.
- Cổng F37 phủ ba component mới **mà không sửa một dòng nào** của cổng: nó vốn quét mọi hằng `*_MS` dưới lớp bọc, và nửa đối chiếu giá trị của nó bắt được ca hai nơi trôi khỏi nhau. Phạm vi thật của cổng và phạm vi viết trong luật từ nay khớp nhau.
- Con số trùng `.12s` biến mất khỏi mã, nên cái bẫy mà ba spec phải cảnh báo bằng chữ đậm cũng biến mất. Bỏ được một cảnh báo tốt hơn viết thêm một cảnh báo tốt.
- Câu hỏi mở ở `DatePicker.md` §Cần chốt 1 đóng cho cả ba tệp bằng một lần.

### Tiêu cực

- **Ba component mở và đóng CHẬM HƠN hôm nay, và người dùng sẽ thấy.** Đây là cái giá chính, và nó là một thay đổi cảm giác không ai đặt hàng — nó đến từ việc áp một thang có sẵn. Nếu nhịp mới thấy nặng tay khi FE chạy được, đường sửa **không** phải khai riêng một khoá cho ba component: đó là xem lại vai của các bậc ở §7 của [`../Design/DESIGN.md`](../Design/DESIGN.md), và nó cần một quyết định của hệ thiết kế.
- **Trục này quyết trên một thang chưa ai nhìn thấy chạy.** Quyết định 2 chọn bậc bằng cách đọc bảng vai; không lượt nào trong đó là một người nhìn một cái menu bung ra. Bảng vai có thể tự nó chưa đúng, và lượt này **không** kiểm được điều đó — nó chỉ đảm bảo mọi thứ nhất quán **với** bảng.
- **Nhóm B lớn gấp đôi, nên cái giá "giá trị sống ở hai nơi" của ADR-0073 cũng lớn gấp đôi.** F37 làm hai nơi truy được về nhau và bắt được chúng trôi khỏi nhau; nó **không** bắt được một hằng nêu **sai bậc** — nối `--dur-slow` vào chỗ đáng lẽ `--dur-base` thì cổng xanh, vì hằng và token vẫn khớp giá trị. Quyết định 4 làm chỗ đó **có thể tranh luận được**; nó không làm chỗ đó được canh.
- **Vế 2 đóng một nửa, và nửa còn lại không có ngày hẹn.** Nó không phải một dòng nợ có điều kiện trả, nó là một việc chờ một điều kiện ngoài tầm lượt này — FE chạy được. Ai đọc quyết định 5 mà nhớ mỗi chữ "đóng" sẽ đọc rộng hơn thực tế.

### Rút lui nếu sai

Vế 1 rẻ: mỗi hằng là một dòng; gỡ ba cặp hằng thì ba component quay về mặc định thư viện, không chạm preset, không chạm token, không chạm spec của ba component kia. Đổi bậc — `--dur-base` sang một bậc khác — là sửa một hằng và một chú thích cho mỗi component, cổng F37 tự bắt nếu sửa nửa vời.

Vế 2 đắt hơn theo một chiều: quyết định 5 **mở lại được** bất cứ lúc nào bằng một lượt rà trên ứng dụng chạy, không cần ADR. Nhưng nếu lượt rà đó kết luận `--dur-fast` phải khác đi cho một nhóm consumer nào đó, thì đấy là một khoá khai riêng đầu tiên, và nó cần ADR vì nó phá tính chất *"một chỗ áp, lá tự thừa hưởng"* mà ADR-0073 quyết định 1 mua được.

## Việc thi công

**`design-expert` — `docs/Design/`:**

1. `Components/Menu.md`, `Components/DatePicker.md`, `Components/Autocomplete.md` — ba spec chuyển từ *"nhận mặc định thư viện, `.12s` là trùng số"* sang *"spec đặt số, nối về vai `--dur-base`"*. Gỡ các khối 🛑 cảnh báo trùng số: sau lượt này không còn cái trùng nào để cảnh báo. §Token dùng của cả ba khai token cho khoản mở/đóng, kèm câu nói rõ giá trị đi vào TypeScript chứ không vào CSS — cùng khuôn ba spec nhóm B cũ, **không chép nguyên văn** sang (luật D37).
2. `Components/DatePicker.md` §Cần chốt — gỡ câu hỏi 1, trỏ về ADR này. Hai tệp kia đang trỏ sang nó nên sửa theo.
3. `DESIGN.md` §7 — vai của `--dur-base` nay gồm cả mở/đóng lớp nổi của ba component này. Đây là tệp chủ của thang token; kiến trúc sư **không** sửa nó.

**`frontend-expert` — `src/FE/`:** cặp hằng cho `shared/ui/menu/`, `shared/ui/date-picker/`, `shared/ui/autocomplete/` khi mỗi component được dựng — một hằng thời lượng, hai hằng đường cong, hai chuỗi ghép, mỗi hằng kèm chú thích nêu **đích danh** token nó phản chiếu (F37). Khuôn có sẵn ở lớp bọc `Toast`; đọc ở đó, đừng dựng khuôn thứ hai.

**`test-engineer`:** không có việc phát sinh. Cổng F37 phủ ba component mới theo hình dạng, không theo danh sách tên.

## Liên quan

- [`0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) — ADR bị sửa một phần; tám quyết định đầu còn hiệu lực nguyên vẹn, quyết định 9 chỉ còn phần phân loại.
- [`../RULES.md`](../RULES.md) §7 — luật **F37** và trạng thái cổng của nó.
- [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 — thang thời lượng và **vai** của từng bậc, căn cứ của quyết định 2.
- [`../Design/Components/Tooltip.md`](../Design/Components/Tooltip.md) — tiền lệ *spec nhường quyền*, và ranh giới *"nhường khi không dựng được"* mà vế 1 dựa vào.
