/**
 * Lớp cộng tác thuần của `TooltipComponent` — **phán xét nội dung chiếu vào**: tìm phần tử neo,
 * nói nó có hợp lệ không, nó thuộc loại nào, nơi gọi đã nối lời chú chưa; và lúc phát triển thì
 * kêu lên khi nơi gọi làm sai.
 *
 * Tách khỏi component vì đây là một câu hỏi khác hẳn câu hỏi của lớp bọc. Lớp bọc trả lời *khi nào
 * được hiện* và *hiện cái gì*; chỗ này trả lời *nơi gọi đã đưa cho ta cái gì*. Câu hỏi thứ hai
 * thuần DOM: không signal, không DI, không Angular — nên nó kiểm được bằng một `<div>` dựng tay,
 * không cần `TestBed` (luật **F26**, docs/RULES.md §7 — lớp cộng tác thuần có `.spec.ts` RIÊNG,
 * test tích hợp của component không được tính là đủ).
 *
 * Nguồn: Design/Components/Tooltip.md §Hình dạng (ràng buộc 1) và §Accessibility (khoản 1 và 4).
 */

/** Đánh dấu phần tử mô tả do chính lớp bọc vẽ, để tách nó khỏi nội dung chiếu vào. */
export const ATTR_MO_TA = 'data-app-tooltip-mo-ta';

/** Kết quả đọc nội dung chiếu vào. `hopLe` sai ⇒ không dựng tooltip nào. */
export interface NoiDungChieuVao {
  /** Phần tử neo — đích của `aria-describedby`. `null` khi nội dung chiếu vào không hợp lệ. */
  readonly neo: HTMLElement | null;
  readonly hopLe: boolean;
}

/**
 * Đọc nội dung chiếu vào của host và chốt phần tử neo.
 *
 * Hợp lệ khi và chỉ khi có **đúng một** phần tử con (không kể phần tử mô tả do lớp bọc tự vẽ) và
 * **không** có chữ trần nào — §Hình dạng, ràng buộc 1. Chữ trần bị loại vì `aria-describedby` cần
 * một phần tử để ghi lên, còn nhiều phần tử anh em thì không có cái nào là "phần tử neo".
 *
 * Thuần: không đọc gì ngoài `host`, không ghi gì cả. Người gọi quyết định làm gì với kết quả.
 */
export function doNoiDungChieuVao(host: HTMLElement): NoiDungChieuVao {
  const con = Array.from(host.children).filter((e) => !e.hasAttribute(ATTR_MO_TA));
  const coChuTran = Array.from(host.childNodes).some(
    (n) => n.nodeType === Node.TEXT_NODE && (n.textContent ?? '').trim() !== '',
  );

  if (con.length === 1 && !coChuTran && con[0] instanceof HTMLElement) {
    return { neo: con[0], hopLe: true };
  }
  return { neo: null, hopLe: false };
}

/**
 * Phần tử neo có phải một component tự dựng phần tử nhận focus BÊN TRONG hay không — ca mà
 * §Accessibility ("Phần tử neo là một component Core") cấm lớp bọc tự ghi `aria-describedby`.
 *
 * Dấu hiệu là dấu gạch ngang trong tên thẻ, và nó máy đọc được chứ không phải ước lượng: tên thẻ
 * HTML gốc KHÔNG BAO GIỜ chứa gạch ngang, còn selector của một component thì luôn chứa. Nói cách
 * khác `<button>` / `<span tabindex="0">` đi nhánh cũ, `<app-check>` / `<app-input>` /
 * `<app-autocomplete>` đi nhánh mới.
 *
 * Vì sao nhánh mới tồn tại: ghi `aria-describedby` lên host của một component như vậy thì thuộc
 * tính rơi lên host trong khi focus nằm ở `<input>` bên trong, và trình đọc màn hình bỏ qua lời
 * chú. Hai nơi cùng ghi một thuộc tính là hai nơi sẽ giẫm lên nhau, bên thua không cố định — nên
 * lớp bọc THÔI ghi và `id` đi ra ngoài qua `idMoTa`.
 */
export function laComponent(neo: HTMLElement): boolean {
  return neo.tagName.includes('-');
}

/**
 * Nơi gọi đã nối `id` lời chú vào phần tử nhận focus THẬT bên trong component neo chưa.
 *
 * Nối đúng thì `id` nằm trên phần tử bên trong, không phải trên host — nên tìm bằng `querySelector`
 * chứ không đọc thuộc tính của chính `neo`. Dùng `~=` vì `aria-describedby` nhận danh sách id.
 */
export function daNoiMoTa(neo: HTMLElement, idMoTa: string): boolean {
  return neo.querySelector(`[aria-describedby~="${idMoTa}"]`) !== null;
}

/**
 * Kêu lên khi nội dung chiếu vào không hợp lệ. Im lặng bỏ qua là cách một tooltip biến mất mà
 * không ai biết (§Hình dạng, ràng buộc 1).
 *
 * Người gọi tự canh `isDevMode()`.
 */
export function canhBaoNoiDungSai(host: HTMLElement): void {
  console.error(
    '<app-tooltip> cần ĐÚNG MỘT phần tử chiếu vào, không phải chữ trần hay nhiều phần tử ' +
      'anh em (Design/Components/Tooltip.md §Hình dạng). Không dựng tooltip nào.',
    host,
  );
}

/**
 * Kêu lên khi phần tử neo là một component tự dựng control bên trong mà nơi gọi QUÊN nối `idMoTa`
 * — khoản 4 của §Accessibility. Đã nối đúng thì không làm gì.
 *
 * Phải báo lúc phát triển, không im lặng: một lời chú rơi khỏi trình đọc màn hình là thứ không ai
 * nhìn thấy, kể cả người vừa dựng xong màn và bấm thử.
 *
 * Người gọi tự canh `isDevMode()` và tự chọn thời điểm — phải là SAU khi cả lượt render xong, lúc
 * component neo đã kịp ghi `describedBy` mà nơi gọi truyền vào.
 */
export function canhBaoChuaNoiMoTa(neo: HTMLElement, idMoTa: string): void {
  if (daNoiMoTa(neo, idMoTa)) {
    return;
  }
  const ten = neo.tagName.toLowerCase();
  console.warn(
    `[app-tooltip] <${ten}> là component tự dựng phần tử nhận focus ` +
      'bên trong, nên lớp bọc KHÔNG tự ghi aria-describedby. Nơi gọi phải nối: ' +
      '<app-tooltip #tip="appTooltip" …><' +
      ten +
      ' [describedBy]="tip.idMoTa" … /></app-tooltip> ' +
      '(Design/Components/Tooltip.md §Accessibility, "Phần tử neo là một component Core"). ' +
      'Chưa nối thì trình đọc màn hình không đọc lời chú này.',
    neo,
  );
}
