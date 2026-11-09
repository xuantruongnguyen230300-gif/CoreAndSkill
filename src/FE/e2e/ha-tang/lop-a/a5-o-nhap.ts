import { errors } from '@playwright/test';
import type { Locator, Page } from '@playwright/test';

import { DUNG_SAI_PX, choTrangYen, docDo, docToken, phepDo } from './chung';

/** Đường biên đo được của một ô nhập, từ computed style — không phụ thuộc cách CSS vẽ ra nó. */
interface DoBien {
  readonly dayBien: number; // viền thật + phần dày vẽ vào phía trong (bóng inset không nhoè)
  readonly mauVien: string;
  readonly mauPhanDay: string | null;
  readonly vongNgoai: string | null; // outline hoặc bóng ngoài — spec cấm
  readonly rong: number;
  readonly cao: number;
  readonly lechChu: number; // viền trái + đệm trái: vị trí chữ trong ô
  readonly trungFocusVisible: boolean;
}

type TrangThai = 'nghỉ' | 'hover' | 'focus' | 'focus+hover';

const docBienTrongTrang = (el: HTMLElement): DoBien => {
  const s = getComputedStyle(el);
  const px = (v: string) => parseFloat(v) || 0;
  const bong = s.boxShadow === 'none' ? [] : s.boxShadow.split(/,(?![^(]*\))/).map((b) => b.trim());
  let phanDay = 0;
  let mauPhanDay: string | null = null;
  let ngoai: string | null = null;
  for (const b of bong) {
    const mau = /(rgba?\([^)]*\))/.exec(b)?.[1] ?? '';
    const so = (b.match(/-?[\d.]+px/g) ?? []).map(px);
    const [x = 0, y = 0, nhoe = 0, lan = 0] = so;
    if (/\binset\b/.test(b)) {
      if (lan > phanDay && nhoe === 0 && x === 0 && y === 0) {
        phanDay = lan;
        mauPhanDay = mau;
      }
    } else if (x || y || nhoe || lan) {
      ngoai = `box-shadow ngoài "${b}"`;
    }
  }
  if (s.outlineStyle !== 'none' && px(s.outlineWidth) > 0) {
    ngoai = `outline ${s.outlineWidth} ${s.outlineStyle} ${s.outlineColor}`;
  }
  const r = el.getBoundingClientRect();
  return {
    dayBien: px(s.borderTopWidth) + phanDay,
    mauVien: s.borderTopColor,
    mauPhanDay,
    vongNgoai: ngoai,
    rong: r.width,
    cao: r.height,
    lechChu: px(s.borderLeftWidth) + px(s.paddingLeft),
    trungFocusVisible: el.matches(':focus-visible'),
  };
};

interface Mong {
  readonly day: number;
  readonly mau: string;
}

async function mongDoi(page: Page) {
  const w = (t: string) => docDo(page, `var(${t})`);
  const c = (t: string) => docToken(page, 'color', `var(${t})`);
  const day = {
    thuong: await w('--border-w'),
    dam: await w('--border-w-strong'),
    nhan: await w('--border-w-accent'),
  };
  const mau = {
    vien: await c('--color-border-strong'),
    chu: await c('--color-text'),
    loi: await c('--color-danger-border'),
  };
  return {
    hopLeNghi: { day: day.thuong, mau: mau.vien } satisfies Mong,
    hopLeFocus: { day: day.dam, mau: mau.chu } satisfies Mong,
    loiNghi: { day: day.dam, mau: mau.loi } satisfies Mong,
    loiFocus: { day: day.nhan, mau: mau.loi } satisfies Mong,
  };
}

async function datTrangThai(page: Page, o: Locator, tt: TrangThai): Promise<void> {
  await page.evaluate(() => (document.activeElement as HTMLElement | null)?.blur());
  await page.mouse.move(0, 0);
  if (tt === 'focus' || tt === 'focus+hover') await o.focus();
  if (tt === 'hover' || tt === 'focus+hover') await o.hover();
}

/**
 * A5 — ô nhập chữ có đúng MỘT đường biên ở mọi trạng thái, không vòng ngoài; màu nói hợp lệ/lỗi, độ dày
 * nói có focus hay không; ô lỗi giữ màu lỗi khi focus và khi hover; hover không đổi viền; kích thước ô và
 * vị trí chữ không đổi. Lái chính form: điền hợp lệ → đo bốn trạng thái; xoá rỗng + bấm gửi → đo bốn
 * trạng thái của ô lỗi. `nutGui` là nút submit của form (không gọi API vì ô rỗng bị chặn ở FE).
 */
export const a5ONhap = phepDo(
  {
    ma: 'A5',
    ten: 'ô nhập chữ: một đường biên theo hai trục hợp lệ/lỗi × focus, hover không đổi viền',
    nguon: [
      'docs/Design/DESIGN.md:235 (§2.6 Ô nhập chữ — Luật: đúng một đường biên, không vòng ngoài, không --color-focus)',
      'docs/Design/DESIGN.md:237-240 (bảng hai trục: hợp lệ nghỉ/focus, lỗi nghỉ/focus — độ dày và màu)',
      'docs/Design/DESIGN.md:244 (dòng Kích thước — kích thước ô và vị trí chữ không đổi)',
      'docs/Design/DESIGN.md:246 (dòng hover — ô hợp lệ giữ viền nghỉ; ô lỗi không đổi gì)',
      'docs/Design/DESIGN.md:247 (dòng Ô đang lỗi — màu lỗi ở lại khi focus)',
    ],
  },
  async (page: Page, nutGui: Locator, vung: string = 'main form', boChonO?: string) => {
    await choTrangYen(page);
    // `boChonO` thu hẹp tập ô cần đo khi form có ô không sửa được (readonly) hoặc ô tuỳ chọn không bao giờ báo lỗi.
    const o = page.locator(
      boChonO ?? `${vung} input:not([type=hidden]):not([type=checkbox]):not([type=radio])`,
    );
    const n = await o.count();
    if (n === 0)
      return { viPham: [`không có ô nhập nào trong "${vung}" — phép đo không có gì để đo`] };
    const mong = await mongDoi(page);
    const viPham: string[] = [];
    // Đo SAU khi mọi transition của ô đã xong: `transition: border-color` khiến đo ngay sau focus đọc màu giữa chừng.
    const doO = async (i: number) => {
      await o
        .nth(i)
        .evaluate((e) =>
          Promise.all(e.getAnimations().map((a) => a.finished.catch(() => undefined))),
        );
      return o.nth(i).evaluate(docBienTrongTrang);
    };
    const idO = (i: number) =>
      o.nth(i).evaluate((e) => e.id || e.getAttribute('name') || e.tagName);

    const kiem = (ten: string, id: string, d: DoBien, m: Mong, goc: DoBien) => {
      if (d.vongNgoai)
        viPham.push(`ô #${id} · ${ten}: có vòng ngoài (${d.vongNgoai}), spec đòi không vòng ngoài`);
      if (Math.abs(d.dayBien - m.day) > DUNG_SAI_PX)
        viPham.push(`ô #${id} · ${ten}: đường biên dày ${d.dayBien}px, spec đòi ${m.day}px`);
      if (d.mauVien !== m.mau)
        viPham.push(`ô #${id} · ${ten}: viền màu ${d.mauVien}, spec đòi ${m.mau}`);
      if (d.mauPhanDay !== null && d.mauPhanDay !== m.mau)
        viPham.push(`ô #${id} · ${ten}: phần dày thêm màu ${d.mauPhanDay}, spec đòi ${m.mau}`);
      if (Math.abs(d.rong - goc.rong) > DUNG_SAI_PX || Math.abs(d.cao - goc.cao) > DUNG_SAI_PX) {
        viPham.push(
          `ô #${id} · ${ten}: kích thước ${d.rong}×${d.cao} khác lúc nghỉ ${goc.rong}×${goc.cao}`,
        );
      }
      if (Math.abs(d.lechChu - goc.lechChu) > DUNG_SAI_PX) {
        viPham.push(
          `ô #${id} · ${ten}: chữ lệch ${d.lechChu}px từ mép ô, lúc nghỉ ${goc.lechChu}px — chữ bị đẩy khi đổi trạng thái`,
        );
      }
    };

    // --- Hợp lệ: điền giá trị cho mọi ô ---
    for (let i = 0; i < n; i++) await o.nth(i).fill('gia-tri-hop-le');
    const gocHopLe: DoBien[] = [];
    for (let i = 0; i < n; i++) {
      const id = await idO(i);
      await datTrangThai(page, o.nth(i), 'nghỉ');
      const nghi = await doO(i);
      gocHopLe.push(nghi);
      kiem('hợp lệ · nghỉ', id, nghi, mong.hopLeNghi, nghi);
      await datTrangThai(page, o.nth(i), 'hover');
      kiem('hợp lệ · hover (không focus)', id, await doO(i), mong.hopLeNghi, nghi);
      await datTrangThai(page, o.nth(i), 'focus');
      const f = await doO(i);
      if (!f.trungFocusVisible)
        viPham.push(
          `ô #${id}: nhận focus nhưng không khớp :focus-visible — không đo được dấu hiệu focus`,
        );
      kiem('hợp lệ · focus', id, f, mong.hopLeFocus, nghi);
      await datTrangThai(page, o.nth(i), 'focus+hover');
      kiem('hợp lệ · focus+hover', id, await doO(i), mong.hopLeFocus, nghi);
    }

    // --- Lỗi: xoá rỗng rồi bấm gửi → mọi ô bắt buộc báo lỗi ---
    for (let i = 0; i < n; i++) await o.nth(i).fill('');
    await nutGui.click();
    await page.waitForLoadState('networkidle');
    // Ứng dụng đưa focus về ô lỗi đầu SAU lần render kế tiếp (`afterNextRender`). Đo sớm hơn thì focus ấy rơi vào giữa
    // phép đo và ô "nghỉ" bị đọc ở trạng thái focus (đã gặp một lần trong lượt chạy đầy đủ 2026-10-06) — chờ tới khi focus nằm ở một ô lỗi.
    // Không chờ được (ứng dụng không chuyển focus) không phải lỗi của phép đo này — A12 canh việc đó.
    await page
      .waitForFunction(
        () => document.activeElement?.getAttribute('aria-invalid') === 'true',
        undefined,
        { timeout: 1000 },
      )
      .catch((e: unknown) => {
        if (!(e instanceof errors.TimeoutError)) throw e;
      });
    for (let i = 0; i < n; i++) {
      const id = await idO(i);
      const coLoi = (await o.nth(i).getAttribute('aria-invalid')) === 'true';
      if (!coLoi) {
        viPham.push(
          `ô #${id}: bỏ trống rồi bấm gửi mà không có aria-invalid="true" — không dựng được trạng thái lỗi để đo`,
        );
        continue;
      }
      await datTrangThai(page, o.nth(i), 'nghỉ');
      kiem('lỗi · nghỉ', id, await doO(i), mong.loiNghi, gocHopLe[i]!);
      await datTrangThai(page, o.nth(i), 'hover');
      kiem('lỗi · hover (không focus)', id, await doO(i), mong.loiNghi, gocHopLe[i]!);
      await datTrangThai(page, o.nth(i), 'focus');
      kiem('lỗi · focus', id, await doO(i), mong.loiFocus, gocHopLe[i]!);
      await datTrangThai(page, o.nth(i), 'focus+hover');
      kiem('lỗi · focus+hover', id, await doO(i), mong.loiFocus, gocHopLe[i]!);
    }
    return { viPham };
  },
);
