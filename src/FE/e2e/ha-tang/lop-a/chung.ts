// Khuôn chung của phép đo lớp A (ADR-0117 QC3): mỗi phép đo là MỘT hàm, mang tên và danh sách dòng
// spec nguồn ở `docs/Design/` (tệp:dòng). Hàm đo trả danh sách vi phạm (dữ liệu) để canary kiểm chính nó;
// `kiem` biến vi phạm thành đỏ và in nguồn — người gặp đỏ mở đúng dòng spec, không đọc code đo.
// Giá trị token KHÔNG chép vào đây (luật D30): đọc từ trang lúc chạy qua `docToken`.
import { errors, test } from '@playwright/test';
import type { Page } from '@playwright/test';

export interface KetQuaDo {
  /** Mỗi phần tử là một câu nói rõ phần tử nào, trạng thái nào, đo được gì, spec đòi gì. Rỗng là sạch. */
  readonly viPham: readonly string[];
  /** Điều kiện tiên quyết không thoả nên phép đo không có gì để kết luận — hiện thành chú thích của test, không im lặng. */
  readonly ghiChu?: string;
}

export interface ThongTinPhepDo {
  readonly ma: string;
  readonly ten: string;
  /** Mỗi mục: `docs/Design/...:dòng` kèm mục — nguồn duy nhất của giá trị phép đo đòi. */
  readonly nguon: readonly string[];
}

export function phepDo<A extends unknown[]>(
  thongTin: ThongTinPhepDo,
  ham: (page: Page, ...doi: A) => Promise<KetQuaDo>,
) {
  return {
    thongTin,
    do: ham,
    /** Chạy phép đo; có vi phạm thì ném lỗi đỏ nêu mã, tên, nguồn spec và từng vi phạm. */
    kiem: async (page: Page, ...doi: A): Promise<void> => {
      const kq = await ham(page, ...doi);
      if (kq.ghiChu)
        test.info().annotations.push({ type: `${thongTin.ma}-ghi-chu`, description: kq.ghiChu });
      if (kq.viPham.length) throw new Error(dinhDangViPham(thongTin, kq.viPham));
    },
  };
}

export function dinhDangViPham(t: ThongTinPhepDo, viPham: readonly string[]): string {
  return [
    `[${t.ma}] ${t.ten} — vi phạm spec Design`,
    ...t.nguon.map((n) => `  nguồn: ${n}`),
    ...viPham.map((v) => `  - ${v}`),
  ].join('\n');
}

/** Dung sai của phép đo bố cục: nửa điểm ảnh CSS để chịu phần làm tròn (ADR-0117 QC4) — của phép đo, không phải nới luật. */
export const DUNG_SAI_PX = 0.5;

/**
 * Đổi một biểu thức CSS dùng token (`var(--sp-6)`) thành giá trị đã resolve trên TRANG ĐANG CHẠY — theme nào
 * đang áp thì token theo theme đó. `chieu`: `width` cho độ dài (trả px), `color` cho màu (trả `rgb(...)`).
 */
export async function docToken(
  page: Page,
  chieu: 'width' | 'color',
  bieuThuc: string,
): Promise<string> {
  return page.evaluate(
    ({ chieu, bieuThuc }) => {
      const e = document.createElement('div');
      e.style.position = 'absolute';
      e.style.visibility = 'hidden';
      e.style.setProperty(chieu, bieuThuc);
      document.body.appendChild(e);
      const kq = getComputedStyle(e).getPropertyValue(chieu);
      e.remove();
      return kq;
    },
    { chieu, bieuThuc },
  );
}

export async function docDo(page: Page, bieuThuc: string): Promise<number> {
  const v = parseFloat(await docToken(page, 'width', bieuThuc));
  if (Number.isNaN(v)) throw new Error(`Không resolve được token độ dài: ${bieuThuc}`);
  return v;
}

/**
 * Chờ trang yên: request xong, font đã nạp, mọi transition/animation đã dừng — đo sớm hơn thì thấy trạng thái
 * chuyển tiếp. Hết hạn chờ `networkidle` KHÔNG phải lỗi: trạng thái "đang tải" cố ý giữ một request treo (spec
 * dựng nó bằng `cho` của ca giả). Request chưa khai báo vẫn đỏ ở H2 cuối test, không phụ thuộc bước này.
 */
export async function choTrangYen(page: Page): Promise<void> {
  await page.waitForLoadState('networkidle', { timeout: 1500 }).catch((e: unknown) => {
    if (!(e instanceof errors.TimeoutError)) throw e;
  });
  await page.evaluate(() =>
    document.fonts.ready
      .then(() =>
        Promise.all(
          document
            .getAnimations()
            .filter((a) => a.effect?.getComputedTiming().iterations !== Infinity)
            .map((a) => a.finished.catch(() => undefined)),
        ),
      )
      .then(() => undefined),
  );
}
