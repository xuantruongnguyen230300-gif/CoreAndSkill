// Bọc axe-core của lưới giao diện (ADR-0117 QC5). Thẻ WCAG A/AA đúng như ADR chốt — không AAA, không
// best-practice. Ngoại lệ chỉ ở `ngoai-le-axe.ts`, áp bằng cách lọc kết quả theo cặp quy tắc × phần tử.
import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';

import { NGOAI_LE_AXE } from './ngoai-le-axe';
import type { NgoaiLeAxe } from './ngoai-le-axe';
import { choTrangYen } from './lop-a/chung';

export const THE_AXE = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'] as const;

/** Phần của kết quả axe mà lưới dùng — tách ra để canary kiểm bộ lọc ngoại lệ không cần chạy trình duyệt. */
export interface ViPhamAxe {
  readonly id: string;
  readonly impact?: string | null;
  readonly help: string;
  readonly helpUrl: string;
  readonly nodes: readonly {
    readonly target: readonly (string | readonly string[])[];
    readonly html: string;
  }[];
}

const chuoiTarget = (t: readonly (string | readonly string[])[]) =>
  t.map((x) => (typeof x === 'string' ? x : x.join(' '))).join(' ');

/**
 * Lọc kết quả theo ngoại lệ của màn và project đang chạy. Trả về `{ conLai, moCoi }`: `conLai` là vi phạm
 * chưa có ngoại lệ; `moCoi` là mục ngoại lệ áp cho lượt chạy này nhưng không khớp vi phạm nào.
 */
export function apDungNgoaiLe(
  vp: readonly ViPhamAxe[],
  man: string,
  project: string,
  ngoaiLe: readonly NgoaiLeAxe[] = NGOAI_LE_AXE,
): { conLai: ViPhamAxe[]; moCoi: NgoaiLeAxe[] } {
  const ap = ngoaiLe.filter((n) => n.man === man && (!n.project || n.project.includes(project)));
  const daKhop = new Set<NgoaiLeAxe>();
  const conLai: ViPhamAxe[] = [];
  for (const v of vp) {
    const nodes = v.nodes.filter((nut) => {
      const t = chuoiTarget(nut.target);
      const mucKhop = ap.filter((n) => n.quyTac === v.id && t === n.boChon);
      mucKhop.forEach((n) => daKhop.add(n));
      return mucKhop.length === 0;
    });
    if (nodes.length) conLai.push({ ...v, nodes });
  }
  return { conLai, moCoi: ap.filter((n) => !daKhop.has(n)) };
}

export function dinhDangAxe(conLai: readonly ViPhamAxe[], moCoi: readonly NgoaiLeAxe[]): string {
  const dong: string[] = [];
  for (const v of conLai) {
    dong.push(`[axe] ${v.id} (${v.impact ?? '?'}): ${v.help} — ${v.helpUrl}`);
    for (const n of v.nodes) dong.push(`  - ${chuoiTarget(n.target)}  ${n.html.slice(0, 120)}`);
  }
  for (const n of moCoi) {
    dong.push(
      `[axe] ngoại lệ mồ côi: ${n.quyTac} × ${n.boChon} (màn ${n.man}) không khớp vi phạm nào — gỡ mục khỏi ngoai-le-axe.ts (căn cứ cũ: ${n.canCu})`,
    );
  }
  return dong.join('\n');
}

/** Quét trang hiện tại; đỏ nếu còn vi phạm WCAG A/AA chưa có ngoại lệ hoặc có ngoại lệ mồ côi. */
export async function quetAxe(page: Page, man: string, project: string): Promise<void> {
  await choTrangYen(page);
  const kq = await new AxeBuilder({ page }).withTags([...THE_AXE]).analyze();
  const { conLai, moCoi } = apDungNgoaiLe(kq.violations, man, project);
  if (conLai.length || moCoi.length) {
    throw new Error(
      `Lưới giao diện — axe WCAG A/AA không sạch ở màn ${man} (${project}):\n${dinhDangAxe(conLai, moCoi)}`,
    );
  }
}
