---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 2026-09-21 — Mẫu mã trong tài liệu luật mang lỗi mà không ai chạy nó

> Phát hiện bởi một lượt `core-reviewer` phạm vi FE sau đợt tái cấu trúc theo [`../adr/0049-trang-thai-quy-trinh-hop-thoai-o-state-khong-o-services.md`](../adr/0049-trang-thai-quy-trinh-hop-thoai-o-state-khong-o-services.md). Đối tượng: mẫu `errorInterceptor` ở mục §2.2 của [`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md).
>
> Viết theo khuôn bốn phần ở [`README.md`](README.md).

---

## 1. Hiện tượng

Tài liệu luật mang một khối mã mẫu cho `errorInterceptor`. Người thi công sau được yêu cầu theo mẫu đó. Hai điều sai nằm trong mẫu:

1. **Gắn cờ "đã thử lại" bằng `req.context.set(...)` trên context của request gốc.** Bên gọi thật của interceptor là các service giữ **một** `HttpContext` dùng chung cho mọi lệnh ghi. Cờ gắn vào instance đó nằm lại vĩnh viễn, nên từ lần CSRF hết hạn thứ hai trở đi, request không còn được gửi lại và lỗi CSRF rơi thẳng về màn.
2. **Nhánh gửi lại trả thẳng `xsrf.lamMoi().pipe(switchMap(… next(…)))` từ callback của `catchError`.** Lỗi của lần gửi lại thoát ra khỏi interceptor dưới dạng lỗi HTTP thô: không thành `ApiFailureError`, không toast, không nhánh 401 hay 429 nào chạy.

**Ca thứ ba, cùng ngày, cùng cơ chế:** sau khi hai điều trên được vá, code thêm hằng `LENH_AN_TOAN` để nhánh CSRF không nhận GET/HEAD/OPTIONS — vì `XsrfTokenStore.lamMoi()` là một GET đi qua chính interceptor mà không mang cờ `DA_THU_LAI_XSRF`, GET token bị `CSRF_REJECTED` sẽ tự gọi lại mãi. Mẫu trong tài liệu vẫn chỉ kiểm cờ; một `core-reviewer` bắt được sau khi code đổi. Chưa quan sát ở môi trường thật (BE bỏ qua kiểm CSRF cho phương thức an toàn nên hiếm) — suy từ cơ chế. Đã sửa mẫu, kiểm cả `docs/wiki-core/fe/ly-do/fe-api-client.md`: file đó không chứa mẫu mã của nhánh này, chỉ có lời giải thích, và đã thêm lý do.

Cả hai điều đầu **chưa được quan sát ở môi trường thật** — phát hiện bằng đọc code đối chiếu tài liệu, hệ quả nêu trên suy từ cơ chế và tái hiện được bằng spec của interceptor.

Điều sai thứ nhất (và ca thứ ba) đã được người thi công phát hiện và **sửa trong code** — kèm một dòng chú thích nêu lý do — nhưng **không ai sửa mẫu trong tài liệu**. Từ lúc đó, tài liệu và code nói ngược nhau, và tài liệu là bản mà người thi công kế tiếp sẽ đọc. Điều sai thứ hai có mặt ở cả mẫu lẫn code; cả hai đã được sửa cùng ngày (mục 3).

Ghi chú thời điểm: vị trí của hai hàm `dichLoi` và `thamSoRetryAfter` cũng đã lệch cùng cách — code dời sang `core/http/`, mẫu vẫn khai chúng nằm trong file interceptor. Lỗi này không gây sai hành vi, chỉ gây sai đường đi của người đọc.

## 2. Nguyên nhân gốc

**Mẫu mã trong tài liệu luật là một bản sao thứ hai của code, và không có gì buộc bản sao đó chạy, biên dịch hay bị đối chiếu.**

- Mẫu không được rút ra từ việc chạy nó. `HttpContext.set()` sửa tại chỗ là một hành vi chỉ lộ ra khi **hai lần gọi dùng chung một instance** — hình dạng mà chính mẫu không có, vì mẫu chỉ có một request.
- Khi code được sửa vì một lý do có thật, người sửa mở file code, không mở tài liệu. Hai bản lệch nhau và không có tín hiệu nào báo.
- Cổng tài liệu kiểm được **đường dẫn có tồn tại không, tuyên bố hiện trạng có nhãn không** — không kiểm mẫu mã có đúng không. `check-docs.sh` xanh suốt thời gian lệch.
- Điều sai thứ hai khó hơn: nó là **lỗi ngữ nghĩa của RxJS** (`catchError` không bắt lỗi của observable do chính callback nó trả về), nên compiler không báo và một test chỉ có ca "lần gửi lại thành công" cũng không bắt.

Đây là đúng khuôn mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §3 nêu ở lý do thứ nhất: rule sai không nằm yên, nó sinh ra code sai. Ở đây code được sửa một nửa; nửa còn lại, code và mẫu mang cùng một cấu trúc sai.

## 3. Cách vá

| Phần | Làm gì | Vá gốc hay triệu chứng |
| --- | --- | --- |
| Mẫu §2.2 | Dựng bản sao context trước khi gắn cờ; chuyển hai hàm dịch lỗi về đúng file; mô tả cấu trúc cuối của nhánh gửi lại (xem dòng dưới) và giữ đoạn văn nêu *lý do* của nó | **Triệu chứng.** Sửa mẫu, chưa sửa lý do mẫu có thể lệch code lần sau |
| Cấu trúc nhánh gửi lại | Người thi công tách phần xử lý lỗi thành hàm `xuLyLoi(yeuCau)` dựng theo từng request; lần gửi lại đi qua `next(guiLai).pipe(catchError(xuLyLoi(guiLai)))`, cờ `DA_THU_LAI_XSRF` nằm trên context của bản clone nên không có lần gửi thứ ba | Vá gốc của điều sai thứ hai. Mẫu trong tài liệu đã khớp sau khi code xong — đối chiếu lại bằng chuỗi `xuLyLoi` và `ctxDaThuLaiXsrf` trong `error.interceptor.ts` |
| Test hồi quy | Spec của interceptor có ca context dùng chung qua hai lần CSRF liên tiếp, và ba ca lần gửi lại thất bại: 422 kèm `fieldErrors` → `ApiFailureError`; 401 → `SessionExpiryHandler`; CSRF lần hai → đúng một toast, không request treo | Canh **code**, không canh **mẫu trong tài liệu** |

Bản vá **chưa** chạm nguyên nhân: mẫu vẫn là bản sao thứ hai, và lần sau code đổi thì mẫu vẫn có thể lệch.

## 4. Cổng nào lẽ ra phải bắt

**Không có cổng máy khả thi cho lớp lỗi "mẫu mã sai ngữ nghĩa".** Máy kiểm được mẫu có biên dịch không nếu trích được khối mã ra một dự án thử — nhưng khối mẫu trong tài liệu là đoạn cắt, thiếu import và ngữ cảnh, và dựng dự án thử cho chúng là một khoản đầu tư lớn hơn giá trị nó đem lại. Kể cả biên dịch được, lỗi ở đây (thay đổi tại chỗ, `catchError`) vẫn biên dịch sạch.

Lớp đã bắt được sự cố này là **`core-reviewer` đối chiếu mẫu với code** — một lớp đọc hiểu, không phải cổng. Vì vậy luật đề xuất được khai đúng như thế, và vào danh sách nợ:

> **D44**: khối mã mẫu trong `docs/quy-uoc/` không mang lỗi mà code thật đã sửa; khi code sửa một thứ mà một mẫu trích, mẫu được sửa trong cùng lượt. Ép bằng gì: không ép được bằng máy; người kiểm là `core-reviewer`. Xem [`../RULES.md`](../RULES.md) §10.

Áp phép thử ở [`README.md`](README.md) §8:

- **Quay ngược thời gian thì luật đó có bắt được sự cố này không?** Có nếu người sửa code nhớ luật; không có gì nhắc họ. Vì vậy luật chỉ là một yêu cầu với người, và dòng nợ ghi đúng như thế.
- **Nó có bắt được một sự cố khác cùng lớp không?** Cùng một lớp lỗi ở mọi mẫu khác trong `quy-uoc/` — chỗ nào code có thêm một dòng phòng thủ mà mẫu không có. Chỉ khi có người đọc.

Phần máy làm được và **đã có**: spec hồi quy của code (mục 3). Nó bảo vệ hành vi, không bảo vệ tài liệu.

## 5. Bài học tổng quát

**Một mẫu mã trong tài liệu luật là code chưa được chạy, và người đọc tin nó hơn code thật vì nó nằm trong file mang chữ "luật".** Chỗ nguy hiểm nhất của nó không phải lúc viết mà lúc code đổi: code có test và có người va vào lỗi; mẫu không có ai.

Hai hệ quả khi viết mẫu:

1. Mẫu chỉ nên chứa thứ **cần** để dạy một ràng buộc. Mỗi dòng thừa là một dòng có thể lệch.
2. Ở chỗ mẫu chưa chắc đúng, **nói thẳng là chưa chốt** thay vì viết một mẫu trông hoàn chỉnh. Một mẫu hoàn chỉnh mà sai tệ hơn một chỗ trống có ghi chú — cấu trúc nhánh gửi lại đã được để ngỏ như vậy cho tới khi code xong, rồi mới điền.

Cùng họ với [`2026-09-11-lenh-kiem-tu-dem-chinh-no.md`](2026-09-11-lenh-kiem-tu-dem-chinh-no.md): thứ nằm trong tài liệu được tin như thể đã được chạy, trong khi không cơ chế nào chạy nó.
