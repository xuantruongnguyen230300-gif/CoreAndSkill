import { Injectable, signal } from '@angular/core';

/** Ba trạng thái người dùng chọn được — DESIGN.md §8. */
export type ThemePreference = 'light' | 'dark' | 'system';

/** CÙNG khoá mà script nội tuyến của index.html đọc trước khi trang vẽ (fe-ui-conventions.md §3.5). */
const KHOA_LUU_THEME = 'theme';

/**
 * Đọc/ghi CÙNG khoá `localStorage` và CÙNG thuộc tính `data-theme` mà script nội tuyến trong
 * `index.html` đã áp trước khung hình đầu tiên. KHÔNG áp lại thuộc tính lúc khởi tạo — script đã
 * áp; áp lại ở đây là dùng một cơ chế phản ứng chậm hơn (đợi Angular khởi động xong), gây đúng
 * cái nháy trắng mà script kia sinh ra để tránh (fe-ui-conventions.md §3.5, DESIGN.md §8).
 *
 * `setPreference` dành cho một `ThemeToggle` sẽ dựng ở pha có `platform/shell` — F1 chỉ cần cơ
 * chế, chưa có control nào gọi tới nó.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly _preference = signal<ThemePreference>(this.docTuLuuTru());

  /** Lựa chọn HIỆN TẠI của người dùng — không phải chế độ đang hiển thị (`system` không lộ ra ở đây). */
  readonly preference = this._preference.asReadonly();

  setPreference(preference: ThemePreference): void {
    this.ghiVaoLuuTru(preference);
    this.apDungThuocTinh(preference);
    this._preference.set(preference);
  }

  /** Giá trị lạ hoặc đọc lỗi (chế độ riêng tư, dung lượng đầy) suy giảm êm về mặc định — DESIGN.md §8: mặc định là sáng. */
  private docTuLuuTru(): ThemePreference {
    let raw: string | null = null;
    try {
      raw = localStorage.getItem(KHOA_LUU_THEME);
    } catch {
      raw = null;
    }
    return raw === 'dark' || raw === 'light' || raw === 'system' ? raw : 'light';
  }

  private ghiVaoLuuTru(preference: ThemePreference): void {
    try {
      localStorage.setItem(KHOA_LUU_THEME, preference);
    } catch {
      // Lưu thất bại — không chặn người dùng đổi giao diện, chỉ mất khả năng nhớ cho lần sau.
    }
  }

  /** `system` gỡ thuộc tính — để `:root:not([data-theme='light'])` của _tokens.scss quyết theo hệ điều hành. */
  private apDungThuocTinh(preference: ThemePreference): void {
    if (preference === 'system') {
      document.documentElement.removeAttribute('data-theme');
      return;
    }
    document.documentElement.setAttribute('data-theme', preference);
  }
}
