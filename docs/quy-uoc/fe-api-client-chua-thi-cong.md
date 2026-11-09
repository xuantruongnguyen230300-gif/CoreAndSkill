---
kind: luat
scope: core
verified: chua-doi-chieu
---

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Hai khối dưới là **chữ ký để dựng** cho [`fe-api-client.md`](fe-api-client.md) §6.2 và §6.3; **chưa có tệp thật nào trong `src/FE`** hôm nay (đối chiếu 2026-09-27 — `core/http/retry.ts` không tồn tại, `thuLaiKhiLoiMang` không xuất hiện ở đâu dưới `src/FE`; `TIMEOUT_MAC_DINH`/`TIMEOUT_XUAT_FILE` không xuất hiện ở đâu dưới `src/FE`). Khối dưới là **khuôn để dựng khi có service đầu tiên cần nó**, không phải mã đang có sẵn để `import`.
>
> Tệp này không giữ câu luật nào. Luật áp hai khối — F42, F52, F53 — nằm ở tệp chính và có hiệu lực ngay.

# Huỷ, retry, timeout — phần chưa thi công

---

## 6.2 Retry — hàm dùng chung

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

Áp cho request nào, ở đâu: [`fe-api-client.md`](fe-api-client.md) §6.2, luật F52.

---

## 6.3 Timeout — hai hằng số

```typescript
export const TIMEOUT_MAC_DINH = 30_000;
export const TIMEOUT_XUAT_FILE = 120_000;
```

Luật dùng hai hằng số và luật cho thao tác dài hơn `TIMEOUT_XUAT_FILE`: [`fe-api-client.md`](fe-api-client.md) §6.3, luật F42 và F53.
