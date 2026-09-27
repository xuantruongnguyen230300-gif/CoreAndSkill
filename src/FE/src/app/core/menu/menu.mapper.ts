import { MenuItemDto } from './menu.dto';
import { MenuNode } from './menu.model';

/**
 * DTO phẳng → cây model, **giữ nguyên thứ tự máy chủ trả**. Hàm thuần, không inject gì
 * (fe-api-client.md §4.2 — mapper cho kiểu của `core/` nằm cạnh DTO và model của chính mảng đó).
 *
 * 🛑 KHÔNG tự sắp lại. Thứ tự mảng là một phần hợp đồng và đã là thứ tự TOÀN PHẦN do máy chủ
 * quyết (contracts/meta-menu.md §1: gốc trước con, rồi `displayOrder`, rồi `code`). Sắp lại ở FE
 * theo một mình `displayOrder` là dựng nguồn thứ hai cho cùng một thứ tự — ngày máy chủ đổi tiêu
 * chí, FE âm thầm nói khác. Gom theo cha dưới đây giữ nguyên thứ tự duyệt, nên thứ tự anh em
 * chính là thứ tự nhận được.
 *
 * Mục có `parentId` không trỏ tới mục nào đang có trong danh sách bị coi là mồ côi và rơi xuống
 * làm gốc — hiện tượng đã cảnh báo ở meta-menu.md §1.6 (module đã gỡ); BE lọc đúng thì trường hợp
 * này không xảy ra, nhưng FE không được im lặng làm mất mục.
 */
export function dungCayMenu(phang: readonly MenuItemDto[]): readonly MenuNode[] {
  const theoId = new Map(phang.map((m) => [m.id, m] as const));
  const conCua = new Map<string | null, MenuItemDto[]>();
  for (const muc of phang) {
    const khoaCha = muc.parentId !== null && theoId.has(muc.parentId) ? muc.parentId : null;
    const ds = conCua.get(khoaCha) ?? [];
    ds.push(muc);
    conCua.set(khoaCha, ds);
  }

  // Rút hình dạng dây về đúng thứ màn cần: bỏ `parentId`, `displayOrder` — `displayOrder` là
  // tiêu chí máy chủ đã dùng để sắp, FE không đọc lại nó.
  const dung = (khoaCha: string | null): MenuNode[] =>
    (conCua.get(khoaCha) ?? []).map((muc) => ({
      id: muc.id,
      code: muc.code,
      labelKey: muc.labelKey,
      icon: muc.icon,
      route: muc.route,
      children: dung(muc.id),
    }));

  return dung(null);
}
