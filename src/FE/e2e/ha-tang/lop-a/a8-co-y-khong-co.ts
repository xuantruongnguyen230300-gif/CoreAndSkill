import type { Page } from '@playwright/test';

import { choTrangYen, phepDo } from './chung';

/** Một dòng của bảng *Cố ý không có* mà gọi tên được phần tử — dòng chỉ tả một trạng thái thì không có ở đây. */
export interface PhanTuCoYKhongCo {
  /** Tên cho thông điệp đỏ. */
  readonly ten: string;
  /** Bộ chọn CSS của phần tử không được có mặt. */
  readonly boChon: string;
  /** Dòng spec, dạng `docs/Design/Screens/...:dòng`. */
  readonly nguon: string;
  /** Số phần tử TỐI ĐA được phép (mặc định 0) — dùng cho "chỉ có đúng liên kết chuyển chế độ". */
  readonly toiDa?: number;
}

/**
 * Bảng *Cố ý không có* của `Screens/01-dang-nhap.md` (dòng 122-130) — phần gọi tên được phần tử, cho chế độ
 * THƯỜNG. Bốn dòng không đo: *thiếu mã cấu hình* (trạng thái, không phải phần tử), *tên hoặc logo của đơn vị*
 * (không có phần tử nào gọi tên được trước khi đăng nhập), đúng như quy định của 06-testing-strategy.md §6.1.
 */
export const KHONG_CO_DANG_NHAP_THUONG: readonly PhanTuCoYKhongCo[] = [
  {
    ten: 'ô mã đơn vị ở chế độ thường',
    boChon: 'input[autocomplete="organization"]',
    nguon: 'docs/Design/Screens/01-dang-nhap.md:124',
  },
  {
    ten: 'ảnh logo trong khung',
    boChon: 'main img, main [role="img"]',
    nguon: 'docs/Design/Screens/01-dang-nhap.md:127',
  },
  {
    ten: 'ô ghi nhớ đăng nhập',
    boChon: 'main input[type="checkbox"], main [role="checkbox"], main [role="switch"]',
    nguon: 'docs/Design/Screens/01-dang-nhap.md:128',
  },
  {
    // Spec: chỉ có MỘT liên kết trong khung — liên kết chuyển chế độ; liên kết quên mật khẩu bị cấm.
    ten: 'liên kết quên mật khẩu (khung chỉ được có đúng liên kết chuyển chế độ)',
    boChon: 'main a',
    nguon: 'docs/Design/Screens/01-dang-nhap.md:129',
    toiDa: 1,
  },
  {
    ten: 'LanguageSwitcher khi CORE_I18N.languages chỉ có một mục',
    boChon: 'app-language-switcher',
    nguon: 'docs/Design/Screens/01-dang-nhap.md:130',
  },
];

/** Như trên, cho chế độ vận hành: ô mã đơn vị CÓ mặt nên không nằm trong danh sách. */
export const KHONG_CO_DANG_NHAP_VAN_HANH: readonly PhanTuCoYKhongCo[] =
  KHONG_CO_DANG_NHAP_THUONG.filter((p) => p.boChon !== 'input[autocomplete="organization"]');

/**
 * A8 — phần tử ghi ở bảng *Cố ý không có* của spec màn thì không có trong DOM, ở mọi trạng thái được kiểm.
 * Người gọi truyền danh sách của màn (đọc từ bảng của spec màn) và gọi phép đo ở MỌI trạng thái của màn.
 */
export const a8CoYKhongCo = phepDo(
  {
    ma: 'A8',
    ten: 'phần tử ghi ở bảng "Cố ý không có" không có trong DOM',
    nguon: [
      'docs/Design/Screens/01-dang-nhap.md:122-130 (bảng Cố ý không có — căn cứ từng dòng ở thông điệp)',
    ],
  },
  async (page: Page, danhSach: readonly PhanTuCoYKhongCo[]) => {
    await choTrangYen(page);
    const viPham: string[] = [];
    for (const p of danhSach) {
      const so = await page.locator(p.boChon).count();
      if (so > (p.toiDa ?? 0))
        viPham.push(
          `${p.ten}: tìm thấy ${so} phần tử khớp "${p.boChon}", tối đa ${p.toiDa ?? 0} — ${p.nguon}`,
        );
    }
    return { viPham };
  },
);
