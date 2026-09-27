---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0061 — `primeicons` là phụ thuộc tường minh của FE: ghim chính xác trong `package.json`, nạp stylesheet đúng một lần

> **Trạng thái:** Đã chấp nhận (2026-09-22)

## Bối cảnh

[`../Design/Icons.md`](../Design/Icons.md) §1 chốt PrimeIcons là bộ icon duy nhất của Core, với lý do đầu tiên là *"đã có sẵn — là dependency của PrimeNG, không thêm gói mới"*. Kiến trúc sư đối chiếu ngày 2026-09-22:

| Đo gì | Kết quả |
| --- | --- |
| Gói PrimeNG đang cài có kéo `primeicons` theo không | **Không.** `dependencies` của gói `primeng` đã cài chỉ gồm các gói `@primeuix/*` và `tslib` — đọc bằng `node -p "require('./src/FE/node_modules/primeng/package.json').dependencies"` |
| `src/FE/package.json` | Không có `primeicons` |
| `src/FE/src/styles/styles.scss` và mảng `styles` của `src/FE/angular.json` | Không nạp stylesheet nào của PrimeIcons |
| Mã FE | Mọi màn đã dựng gọi icon bằng lớp `pi pi-*` theo bảng Icons.md §5; các phần tử `<i class="pi …">` hiện **không vẽ glyph nào** — bảng trạng thái đầu Icons.md đã ghi nhận cùng ngày |
| Icon PrimeNG tự vẽ bên trong component bọc | SVG nội tuyến (`data-p-icon`), không phụ thuộc gói này |

Tiền đề *"không thêm gói"* sai với bản PrimeNG đang dùng. Hệ quả: bộ icon đã được chọn và đã được dùng khắp mã, nhưng chưa từng được cài. Thêm gói là một phụ thuộc ngoài mới, nên kiến trúc sư trình bày và người dùng chốt — người dùng đồng ý ngày 2026-09-22.

## Quyết định

Kiến trúc sư đề xuất, người dùng chốt:

1. **`primeicons` là phụ thuộc tường minh** trong `dependencies` của `src/FE/package.json`, **ghim chính xác** — không `^`, không `~` — theo quyết định 4 của [`0028-toolchain-fe-va-ke-hoach-nang-cap.md`](0028-toolchain-fe-va-ke-hoach-nang-cap.md): `package.json` là nguồn phiên bản duy nhất, tài liệu không chép số.
2. **Quy tắc chọn bản lúc cài:** bản ổn định mới nhất tại thời điểm cài mà **có đủ mọi tên icon** ở Icons.md §5. Gói không khai ràng buộc peer với PrimeNG, và icon PrimeNG tự vẽ là SVG, nên hai gói không cần khớp số với nhau — "tương thích" ở đây có đúng một nghĩa kiểm được: tên icon. ADR này không ghi số; `frontend-expert` chọn lúc cài.
3. **Nạp stylesheet đúng một lần, ở điểm vào style toàn cục** ([`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §4.1) — không ở `styleUrl` của component nào, không ở tệp override `_thu-vien.scss` (đó là chỗ đè, không phải chỗ nạp). Chọn mảng `styles` của `angular.json` hay `@use` trong `styles.scss` là việc thi công; §4.1 của file luật ghi lại chỗ đã chọn.
4. **Nghiệm thu, cùng lượt cài:** (a) mọi tên `pi-*` trong bảng Icons.md §5 có trong stylesheet của gói đã cài — lệnh dưới; (b) `ng build` production xanh và số đo `initial` ghi vào [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §8.11 — stylesheet của icon vào `styles.css`, tức vào ngân sách `initial` của luật F14. Chiều ngược — mọi `pi-*` trong mã có trong bảng §5 — là việc của bảng §5 và `design-expert`, đang giao song song.

Lệnh nghiệm thu (a), chạy từ gốc repo, PASS khi không in dòng nào và số ở dòng `da xet` lớn hơn 0:

```bash
CSS=src/FE/node_modules/primeicons/primeicons.css
[ -f "$CSS" ] || { echo "chưa cài primeicons"; exit 1; }
TEN=$(awk '/^## 5\./,/^## 6\./' docs/Design/Icons.md | grep -oE '`pi-[a-z0-9-]+`' | tr -d '`' | sort -u)
for icon in $TEN; do grep -q "\.$icon:before" "$CSS" || echo "THIEU: $icon"; done
echo "da xet: $(printf '%s\n' "$TEN" | grep -c .) ten icon"
```

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Dùng bộ icon SVG `primeng/icons` có sẵn trong gói PrimeNG, không thêm gói

**Được:** không thêm phụ thuộc; SVG không mang ba cái giá của icon font mà Icons.md §1 đã nêu.

**Mất:** bộ đó chỉ gồm icon mà chính PrimeNG dùng bên trong component, không phải bộ đầy đủ — nhiều dòng của bảng §5 (đơn vị, khoá, la bàn, hộp thư…) không có ở đó, nên phải trộn bộ, đúng điều §1 cấm. Mỗi icon là một component Angular import từ `primeng/icons`, tức import `primeng/*` ở mọi nơi dùng icon — vi phạm luật F5 trừ khi bọc lại từng icon một. Và mọi template đã dựng dùng lớp `pi pi-*`: đổi là viết lại cả bảng §5 lẫn mọi màn.

**Vì sao loại:** nó đổi bộ icon, không phải cài bộ đã chọn; đó là một ADR khác, lật Icons.md §1, và hôm nay không có lý do để lật.

### Phương án B — Chép tệp font và CSS của PrimeIcons vào `public/`, không qua npm

**Vì sao loại:** một nguồn phiên bản thứ hai ngoài `package.json`, trái quyết định 4 của ADR-0028; không có đường nâng cấp; tệp nhị phân và giấy phép của bên thứ ba vào git.

### Phương án C — Nạp từ CDN

**Vì sao loại:** phụ thuộc mạng ngoài lúc chạy, trong khi sản phẩm cài trong mạng nội bộ của từng cơ quan ([`0013-multi-tenant.md`](0013-multi-tenant.md), [`0014-mot-instance-key-ring-postgres.md`](0014-mot-instance-key-ring-postgres.md)); không ghim được bản; thêm một nguồn cho CSP.

### Phương án D — Gom subset chỉ chứa icon dùng thật (câu hỏi mở 1 của Icons.md §8)

**Vì sao chưa chọn:** thêm một bước build và một việc phải làm mỗi khi thêm icon, đổi lấy một khoản băng thông chưa đo. Đo trước bằng nghiệm thu (b), rồi xét ở điều kiện lật 2.

## Hệ quả

### Tích cực

- Icon hiện ra; mọi màn đã dựng theo bảng §5 bắt đầu đúng như spec.
- Một nguồn phiên bản, một chỗ nạp. Thêm icon là thêm một dòng ở bảng §5, không đụng build.

### Tiêu cực

- **Thêm một gói phải theo dõi bản vá và nâng cấp**, theo lịch nâng của FE ở [`../wiki-core/fe/16-nen-tang-va-nang-cap.md`](../wiki-core/fe/16-nen-tang-va-nang-cap.md). Người giữ: người giữ toolchain FE — không có ai khác.
- **Cả bộ icon vào tải khởi động** dù chỉ dùng vài chục — ăn vào dư địa ngân sách `initial` của F14; số đo phải ghi lại sau khi cài, không đoán.
- **Ba cái giá của icon font** ở Icons.md §1 (nhấp nháy khi font tải chậm, không tô hai màu, lệch đường cơ sở chữ) nay là thật, không còn là lý thuyết.
- **Nghiệm thu (a) là lệnh chạy tay, không phải cổng thường trực.** Một dòng §5 gõ sai tên, hay một lần nâng gói bỏ mất icon, chỉ lộ ra bằng mắt. Nếu tái diễn, đưa lệnh trên thành một section của `scripts/fe-gate.sh` kèm mã luật ở [`../RULES.md`](../RULES.md) §7.
- Tiền đề *"không thêm gói"* ở Icons.md §1 phải được viết lại — việc của `design-expert`.

### Điều kiện lật

1. PrimeNG phát hành bộ SVG đầy đủ cùng phong cách, hoặc thôi dùng chung phong cách với PrimeIcons.
2. Số đo `initial` sau khi cài vượt ngưỡng cảnh báo của F14 → xét phương án D.
3. Nhu cầu icon ngoài PrimeIcons lặp lại nhiều lần (Icons.md §6 bước 4) → xét lại bộ icon bằng ADR mới.

### Rút lui nếu sai

Gỡ gói khỏi `package.json`, gỡ dòng nạp stylesheet; template không đổi vì chỉ dùng lớp `pi-*`. Chi phí một buổi. Chọn bộ khác thì là ADR mới, lật Icons.md §1.

## Liên quan

- [`0028-toolchain-fe-va-ke-hoach-nang-cap.md`](0028-toolchain-fe-va-ke-hoach-nang-cap.md) quyết định 4 — nguồn phiên bản.
- [`../Design/Icons.md`](../Design/Icons.md) §1, §5, §8.
- [`../RULES.md`](../RULES.md) §7 F5, F14.
