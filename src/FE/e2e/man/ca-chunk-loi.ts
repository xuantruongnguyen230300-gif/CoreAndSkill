// Dựng lỗi "không tải được phần mã" cho docs/Design/Screens/02-trang-loi.md (mục *Không tải được phần ứng dụng*
// và *Điều hướng hỏng khi đã có màn đang hiện*) bằng `page.route`. Không đoán tên chunk (băm theo bản build).
// Hai cách chọn tệp bị huỷ:
// - `chanChunkLazy`: huỷ mọi .js chưa tải trước lúc `bat()` — dùng khi ĐÃ có màn (bootstrap xong, chunk của nó đã tải).
// - `chanChunkTuyen`: huỷ đúng phần mã của MỘT tuyến, tìm bằng hai lần tải thử không chặn (tuyến đích trừ một URL lạ
//   cùng khung) — dùng khi chưa có màn nào, vì lúc đó bootstrap còn tự nạp chunk (hoạt ảnh...) mà không được chặn.
import type { Page } from '@playwright/test';

import type { ApiGia } from '../ha-tang/api-gia';

export interface ChanChunk {
  bat(): void;
  tat(): void;
  /** URL các tệp bị huỷ. */
  readonly daHuy: readonly string[];
}

const tenTep = (url: string): string => new URL(url).pathname.split('/').pop() ?? '';
const LOI_ANGULAR_GHI_RA = 'Failed to fetch dynamically imported module';

async function dangKy(
  page: Page,
  apiGia: ApiGia,
  batNgay: boolean,
  huy: (ten: string) => boolean,
  ghiNhanKhiTat: (ten: string) => void,
): Promise<ChanChunk> {
  const daHuy: string[] = [];
  let dangChan = batNgay;
  await page.route(/\.js(\?.*)?$/, (route) => {
    const url = route.request().url();
    const ten = tenTep(url);
    if (dangChan && huy(ten)) {
      daHuy.push(url);
      // Chromium ghi một dòng console.error cho mỗi tệp bị huỷ; Angular ghi thêm `ERROR ...` cho lần điều hướng hỏng.
      apiGia.mienLoiTaiTep(url);
      apiGia.mienLoiConsoleChua(LOI_ANGULAR_GHI_RA, 1);
      return route.abort();
    }
    if (!dangChan) ghiNhanKhiTat(ten);
    return route.fallback();
  });
  return {
    bat: () => {
      dangChan = true;
    },
    tat: () => {
      dangChan = false;
    },
    daHuy,
  };
}

/** Huỷ mọi .js chưa tải trước lúc `bat()` (trừ gói khởi động mà `index.html` nêu). */
export async function chanChunkLazy(page: Page, apiGia: ApiGia): Promise<ChanChunk> {
  const html = await (await page.request.get('/')).text();
  const khoiDong = new Set([...html.matchAll(/(?:href|src)="([^"]+\.js)"/g)].map((m) => m[1]!));
  const daTai = new Set<string>();
  return dangKy(
    page,
    apiGia,
    false,
    (ten) => !khoiDong.has(ten) && !daTai.has(ten),
    (ten) => daTai.add(ten),
  );
}

/**
 * Huỷ đúng phần mã riêng của tuyến `urlDich`, chặn đang BẬT sẵn. `urlLa` là một URL không khớp tuyến nào: nó cũng đi qua
 * khung nên phần chung (khung, hoạt ảnh...) rơi ra khỏi tập cần huỷ. Tập rỗng nghĩa là phép dò hỏng — ném lỗi, không im.
 * Hàm tải `urlDich` hai lần trên chính `page` (không chặn) — gọi TRƯỚC khi test mở màn thật.
 */
export async function chanChunkTuyen(
  page: Page,
  apiGia: ApiGia,
  urlDich: string,
  urlLa = '/duong-dan-khong-ton-tai',
): Promise<ChanChunk> {
  const ghi = async (url: string): Promise<Set<string>> => {
    const gap = new Set<string>();
    const ghiNhan = (req: { url(): string }) => {
      if (/\.js(\?.*)?$/.test(req.url())) gap.add(tenTep(req.url()));
    };
    page.on('requestfinished', ghiNhan);
    await page.goto(url);
    await page.waitForLoadState('networkidle');
    page.off('requestfinished', ghiNhan);
    return gap;
  };
  const cuaDich = await ghi(urlDich);
  const cuaLa = await ghi(urlLa);
  const rieng = new Set([...cuaDich].filter((t) => !cuaLa.has(t)));
  if (rieng.size === 0) {
    throw new Error(
      `chanChunkTuyen: không tìm được phần mã riêng của ${urlDich} (tải ${cuaDich.size} tệp, trùng hết với ${urlLa})`,
    );
  }
  return dangKy(
    page,
    apiGia,
    true,
    (ten) => rieng.has(ten),
    () => undefined,
  );
}
