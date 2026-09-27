import { EnvironmentProviders } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

/** Phần của `window.matchMedia` mà việc chọn hoạt ảnh cần tới — chỉ đọc `matches`. */
export type TruyVanMedia = (query: string) => { readonly matches: boolean };

/**
 * Nửa provider của nhánh giảm chuyển động: hệ điều hành bật "giảm chuyển động" thì bộ chạy rỗng
 * `provideAnimationsAsync('noop')`, không thì bộ chạy thật `provideAnimationsAsync()`.
 *
 * 🛑 Nhánh `'noop'` ở đây KHÔNG phải cấu hình test lạc vào production — gỡ nó là trả hoạt ảnh về
 * cho đúng những người đã xin tắt. Khối `@media (prefers-reduced-motion: reduce)` toàn cục ở
 * `src/styles/styles.scss` chỉ ép thời lượng của animation và transition CSS. Hoạt ảnh Angular
 * không đi qua hai thuộc tính đó: bộ chạy trao thời lượng cho `element.animate(keyframes,
 * { duration })` bằng đối số JS, nên khối CSS đi qua mà không đổi gì. Toast, hộp thoại, menu bật
 * lên và các lớp phủ của thư viện UI vào/ra bằng bộ chạy này. Thay nó bằng bộ chạy rỗng lúc khởi
 * động là nửa còn lại của nhánh giảm chuyển động — khối CSS là nửa kia. Hai nửa và ba loại chuyển
 * động: docs/Design/DESIGN.md §7, mục "Giảm chuyển động".
 *
 * Vì sao `provideAnimationsAsync('noop')` chứ KHÔNG `provideNoopAnimations` — dù spec vẫn dùng
 * cái sau: `provideNoopAnimations` là import tĩnh, kéo cả bộ máy hoạt ảnh vào bundle khởi động
 * của MỌI người dùng, kể cả người không bật cờ. Đo 2026-09-24: +64 kB thô, đẩy bundle initial qua
 * ngưỡng cảnh báo 600 kB của ngân sách F14. Bản `'noop'` cho cùng bộ chạy rỗng
 * (`ANIMATION_MODULE_TYPE` là `'NoopAnimations'`) mà bộ máy vẫn nạp lười. "Sửa cho gọn" về
 * `provideNoopAnimations` là trả 64 kB đó lại cho tất cả. (Tên API bị loại cố ý viết không kèm
 * dấu ngoặc gọi hàm: cổng F39 quét văn bản của tệp không phải spec, kể cả chú thích.)
 *
 * Hỏi MỘT lần, lúc app khởi động. Đổi cài đặt hệ điều hành giữa phiên thì phải tải lại trang mới
 * có hiệu lực — cái giá đã chọn có ý thức, không phải lỗi chờ sửa bằng một bộ lắng nghe thứ hai.
 *
 * `matchMedia` vắng (SSR, môi trường test không cài nó) ⇒ TẮT hoạt ảnh. Vắng `matchMedia` là
 * không có trình duyệt nào để hỏi, và ở đó không ai nhìn hoạt ảnh: chính `provideAnimationsAsync`
 * cũng tự chuyển sang `'noop'` khi chạy trên server. Nếu lỡ là một trình duyệt thật không trả lời
 * được thì hai cách đoán sai không ngang giá — tắt nhầm chỉ mất một hiệu ứng, bật nhầm là đưa
 * chuyển động tới người đã xin giảm, điều DESIGN.md §7 khai là bắt buộc tránh.
 *
 * Không đọc biến toàn cục nào: `matchMedia` đi vào qua tham số, nên hàm kiểm được bằng unit test
 * thường. Tham số cố ý không có mặc định — một lời gọi quên truyền nó sẽ lặng lẽ rơi vào nhánh
 * vắng ở trên và tắt hoạt ảnh của mọi người.
 */
export function chonProviderHoatAnh(matchMedia: TruyVanMedia | undefined): EnvironmentProviders {
  if (typeof matchMedia !== 'function') {
    return provideAnimationsAsync('noop');
  }
  return matchMedia('(prefers-reduced-motion: reduce)').matches
    ? provideAnimationsAsync('noop')
    : provideAnimationsAsync();
}

/**
 * Provider hoạt ảnh DUY NHẤT của app. Composition root gọi hàm này và không khai provider hoạt ảnh
 * nào khác (luật F39, ADR-0080).
 *
 * Vì sao thân hàm sống ở `core/` chứ không ở `app.config.ts`: nửa provider là nghĩa vụ trợ năng
 * của chính các component Core — `Dialog`, `Toast`, `Menu` của Core là thứ chuyển động. Tệp cấu
 * hình app là tệp của dự án, nằm ngoài vùng Core canh: dự án hạ nguồn tự viết composition root
 * riêng sẽ không thừa hưởng một thân hàm để ở đó, và một lượt rút ruột nó không buộc lượt soát Core
 * nào. Cùng lý do mã locale đi qua `provideCoreI18n` (ADR-0063). Khi Angular gỡ API hoạt ảnh đang
 * dùng, lượt di trú cũng chỉ xảy ra một lần — ở đây.
 *
 * Không nhận tham số: không giá trị nào ở đây đổi khi dựng sản phẩm khác trên cùng nền tảng, nên
 * đây không phải seam (fe-architecture.md §2.5). Đây là chỗ DUY NHẤT đọc `globalThis.matchMedia`;
 * truyền nó tách rời khỏi `window` vẫn gọi được, và spec khoá đúng lời gọi đó.
 */
export function provideCoreAnimations(): EnvironmentProviders {
  return chonProviderHoatAnh(globalThis.matchMedia);
}
