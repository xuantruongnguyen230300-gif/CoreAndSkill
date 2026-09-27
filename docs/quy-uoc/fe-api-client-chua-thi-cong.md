---
kind: luat
scope: core
verified: chua-doi-chieu
---

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Hai mục dưới đây là một phần của [`fe-api-client.md`](fe-api-client.md) §6 nhưng **chưa có tệp thật nào trong `src/FE`** hôm nay (đối chiếu 2026-09-27 — `core/http/retry.ts` không tồn tại, `thuLaiKhiLoiMang` không xuất hiện ở đâu dưới `src/FE`; `TIMEOUT_MAC_DINH`/`TIMEOUT_XUAT_FILE` không xuất hiện ở đâu dưới `src/FE`; nợ [F42](../DEBT.md) đang canh phần timeout). Khối dưới là **khuôn để dựng khi có service đầu tiên cần nó**, không phải mã đang có sẵn để `import`.

# Huỷ, retry, timeout — phần chưa thi công

---

## 6.2 Retry — hẹp, có điều kiện

Retry chỉ áp cho **request đọc** (`GET`) và **chỉ khi** lỗi là lỗi mạng hoặc 5xx. Không bao giờ retry `POST`/`PUT`/`DELETE`.

```typescript
// core/http/retry.ts — CHƯA TỒN TẠI, xem banner đầu file
export function thuLaiKhiLoiMang<T>(soLan = 2) {
  return retry<T>({
    count: soLan,
    delay: (err: HttpErrorResponse, lan) =>
      err.status === 0 || err.status >= 500
        ? timer(300 * 2 ** lan)  // lùi theo cấp số nhân
        : throwError(() => err), // lỗi 4xx: hỏng do request, thử lại vô nghĩa
  });
}
```

Áp ở **service của feature**, không ở interceptor.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §6.2

---

## 6.3 Timeout

```typescript
export const TIMEOUT_MAC_DINH = 30_000;
export const TIMEOUT_XUAT_FILE = 120_000;
```

Đặt ở service, khai tường minh cho từng nhóm endpoint. Endpoint xuất báo cáo được phép lâu hơn — nhưng phải **khai bằng một hằng số có tên**, không phải bằng cách bỏ timeout đi. Luật ép bằng máy: [F42](../DEBT.md).

📐 Thao tác dài hơn ngưỡng trên (kết xuất lớn, nhập hàng loạt): BE trả mã việc, FE hỏi trạng thái theo chu kỳ — không nâng timeout. Khuôn chốt ở pha B4 ([`../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md`](../wiki-core/be/trien-khai/05-b4-tep-nhap-xuat-thong-bao.md)).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`fe-api-client.md`](../wiki-core/fe/ly-do/fe-api-client.md) §6.3
