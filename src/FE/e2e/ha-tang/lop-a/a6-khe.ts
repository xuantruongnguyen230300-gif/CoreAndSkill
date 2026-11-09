import type { Page } from '@playwright/test';

import { DUNG_SAI_PX, choTrangYen, docDo, phepDo } from './chung';

interface Hop {
  readonly tren: number;
  readonly duoi: number;
}

/** Khe dọc đo được giữa hai phần tử liền kề, và hai đầu của khe — để thông điệp đỏ nói rõ đo từ đâu tới đâu. */
interface Khe {
  readonly ten: string;
  readonly do: number;
}

/** Đo trong trang: mọi khe dọc của form xác thực và của các vùng trong khung. */
const doKheTrongTrang = () => {
  const h = (e: Element): Hop => {
    const r = e.getBoundingClientRect();
    return { tren: r.top, duoi: r.bottom };
  };
  const hien = (e: Element | null): e is HTMLElement =>
    !!e && (e as HTMLElement).checkVisibility({ checkVisibilityCSS: true });
  const ten = (e: Element) => `<${e.tagName.toLowerCase()}${e.id ? '#' + e.id : ''}>`;
  const h1 = document.querySelector('main h1');
  const form = document.querySelector('main form');
  const kq = {
    nhanDenO: [] as Khe[],
    oDenLoi: [] as Khe[],
    giuaTruong: [] as Khe[],
    truongCuoiDenNut: null as Khe | null,
    tieuDeDenMoTa: null as Khe | null,
    giuaVung: [] as Khe[],
  };
  if (form) {
    const oNhap = Array.from(form.querySelectorAll('input:not([type=hidden])')).filter(hien);
    const phanTuCuoi = (o: Element) => {
      const ids = (o.getAttribute('aria-describedby') ?? '').split(/\s+/).filter(Boolean);
      const dong = ids.map((i) => document.getElementById(i)).filter(hien);
      return { dong: dong[dong.length - 1] ?? null, goc: dong[dong.length - 1] ?? o };
    };
    oNhap.forEach((o, i) => {
      const nhan = o.id ? document.querySelector(`label[for="${CSS.escape(o.id)}"]`) : null;
      if (nhan) kq.nhanDenO.push({ ten: `nhãn → ô #${o.id}`, do: h(o).tren - h(nhan).duoi });
      const { dong, goc } = phanTuCuoi(o);
      if (dong) kq.oDenLoi.push({ ten: `ô #${o.id} → ${ten(dong)}`, do: h(dong).tren - h(o).duoi });
      const ke = oNhap[i + 1];
      if (ke) {
        const nhanKe = ke.id ? document.querySelector(`label[for="${CSS.escape(ke.id)}"]`) : null;
        if (nhanKe)
          kq.giuaTruong.push({
            ten: `${ten(goc)} (cuối trường #${o.id}) → nhãn trường #${ke.id}`,
            do: h(nhanKe).tren - h(goc).duoi,
          });
      } else {
        const nut = form.querySelector('button[type=submit]');
        if (hien(nut))
          kq.truongCuoiDenNut = {
            ten: `${ten(goc)} (cuối trường #${o.id}) → nút chính`,
            do: h(nut).tren - h(goc).duoi,
          };
      }
    });
  }
  if (h1) {
    const anh = Array.from(h1.parentElement?.children ?? []).filter(hien);
    const vt = anh.indexOf(h1 as HTMLElement);
    const moTa = anh[vt + 1]?.tagName === 'P' ? anh[vt + 1]! : null;
    if (moTa) kq.tieuDeDenMoTa = { ten: 'tiêu đề <h1> → mô tả', do: h(moTa).tren - h(h1).duoi };
    const dauVung = moTa ? vt + 1 : vt;
    for (let i = dauVung; i < anh.length - 1; i++) {
      kq.giuaVung.push({
        ten: `${ten(anh[i]!)} → ${ten(anh[i + 1]!)}`,
        do: h(anh[i + 1]!).tren - h(anh[i]!).duoi,
      });
    }
  }
  return kq;
};

async function mong(page: Page) {
  return {
    sp3: await docDo(page, 'var(--sp-3)'),
    sp4: await docDo(page, 'var(--sp-4)'),
    sp6: await docDo(page, 'var(--sp-6)'),
    sp7: await docDo(page, 'var(--sp-7)'),
    sp8: await docDo(page, 'var(--sp-8)'),
  };
}

function soSanh(khe: readonly Khe[], dung: number, tenBac: string, viPham: string[]) {
  for (const k of khe) {
    if (Math.abs(k.do - dung) > DUNG_SAI_PX)
      viPham.push(`${k.ten}: khe ${k.do.toFixed(1)}px, spec đòi ${dung}px (${tenBac})`);
  }
}

/**
 * A6a — khe dọc TRONG form xác thực. Phần tử liền kề không chồng nhau (khe âm hay bằng 0 là vi phạm) và khe
 * bằng bậc spec khai: nhãn → ô `--sp-4`, ô → dòng lỗi/gợi ý `--sp-3`, trường → trường `--sp-6` (đo từ phần tử
 * cuối của trường trên tới nhãn trường dưới), trường cuối → nút chính `--sp-7`.
 */
export const a6KheTrongForm = phepDo(
  {
    ma: 'A6a',
    ten: 'khe dọc trong form: nhãn→ô, ô→dòng phụ, trường→trường, trường cuối→nút chính',
    nguon: [
      'docs/Design/Components/AuthCard.md:87 (§Kích thước — giữa hai trường --sp-6, đo từ phần tử cuối của trường trên)',
      'docs/Design/Components/AuthCard.md:88 (§Kích thước — trường cuối → nút chính --sp-7)',
      'docs/Design/Components/AuthCard.md:90 (khe trong form là của khung — một màn quên khai không được làm khe về 0)',
      'docs/Design/Components/AuthField.md:111 (§Responsive — nhãn nằm trên ô, khe --sp-4)',
      'docs/Design/Components/AuthField.md:94 (§Trạng thái error — dòng lỗi cách ô --sp-3)',
      'docs/Design/DESIGN.md:432,434,435 (§4 — --sp-4 khe nhãn → ô; --sp-6 khe giữa hai trường; --sp-7 khe giữa hai nhóm)',
    ],
  },
  async (page: Page) => {
    await choTrangYen(page);
    const d = await page.evaluate(doKheTrongTrang);
    const m = await mong(page);
    const viPham: string[] = [];
    if (d.nhanDenO.length === 0)
      return {
        viPham: [
          'không tìm thấy ô nhập có nhãn <label for> trong form — phép đo không có gì để đo',
        ],
      };
    soSanh(d.nhanDenO, m.sp4, '--sp-4', viPham);
    soSanh(d.oDenLoi, m.sp3, '--sp-3', viPham);
    soSanh(d.giuaTruong, m.sp6, '--sp-6', viPham);
    if (d.truongCuoiDenNut) soSanh([d.truongCuoiDenNut], m.sp7, '--sp-7', viPham);
    else
      viPham.push(
        'không tìm thấy nút chính type=submit trong form — không đo được khe trường cuối → nút',
      );
    return { viPham };
  },
);

/** A6b — khe giữa các vùng của khung (khối tiêu đề · khu lỗi · form · liên kết phụ) bằng `--sp-8`. */
export const a6KheGiuaVung = phepDo(
  {
    ma: 'A6b',
    ten: 'khe giữa các vùng trong khung xác thực',
    nguon: [
      'docs/Design/Components/AuthCard.md:85 (§Kích thước — giữa các vùng: logo · khối tiêu đề · khu lỗi · form · liên kết phụ · LanguageSwitcher: --sp-8)',
    ],
  },
  async (page: Page) => {
    await choTrangYen(page);
    const d = await page.evaluate(doKheTrongTrang);
    const m = await mong(page);
    const viPham: string[] = [];
    if (d.giuaVung.length === 0)
      return {
        viPham: ['không tìm thấy hai vùng liền kề nào sau tiêu đề — phép đo không có gì để đo'],
      };
    soSanh(d.giuaVung, m.sp8, '--sp-8', viPham);
    return { viPham };
  },
);

/**
 * A6c — khe tiêu đề → mô tả bằng `--sp-3`. Chỉ áp cho màn CÓ mô tả (đăng nhập chế độ vận hành, đổi mật khẩu
 * bắt buộc); màn không có mô tả thì phép đo báo vi phạm "không có mô tả" — người gọi chỉ gọi khi spec màn có.
 */
export const a6KheTieuDeMoTa = phepDo(
  {
    ma: 'A6c',
    ten: 'khe tiêu đề → mô tả',
    nguon: ['docs/Design/Components/AuthCard.md:86 (§Kích thước — tiêu đề → mô tả --sp-3)'],
  },
  async (page: Page) => {
    await choTrangYen(page);
    const d = await page.evaluate(doKheTrongTrang);
    const m = await mong(page);
    if (!d.tieuDeDenMoTa)
      return {
        viPham: ['màn này phải có mô tả ngay dưới <h1> nhưng không tìm thấy — không đo được khe'],
      };
    const viPham: string[] = [];
    soSanh([d.tieuDeDenMoTa], m.sp3, '--sp-3', viPham);
    return { viPham };
  },
);
