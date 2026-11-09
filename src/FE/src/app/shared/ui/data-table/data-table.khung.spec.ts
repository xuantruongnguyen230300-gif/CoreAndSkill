import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { DataTableComponent } from './data-table.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const PROVIDERS = [
  provideZonelessChangeDetection(),
  provideTranslateService({
    lang: 'vi',
    fallbackLang: 'vi',
    loader: provideTranslateLoader(FakeTranslateLoader),
  }),
];

interface Hang {
  readonly id: number;
  readonly ten: string;
}

function nhieuDong(n: number): readonly Hang[] {
  return Array.from({ length: n }, (_, i) => ({ id: i + 1, ten: `dong-${i + 1}` }));
}

/**
 * Vùng chứa có chiều cao cố định, là cột flex — đúng chuỗi flex mà trang danh sách cấp cho khối
 * (Design/Templates/ListScreen.md §4). Cột hành động có hai nút, nút ⋯ mang `aria-expanded`.
 */
@Component({
  standalone: true,
  imports: [DataTableComponent],
  template: `
    <div
      class="vung-chua"
      [style.height.px]="cao()"
      [style.--data-table-scroll-h-floor]="lapChieuCao() ? 'var(--data-table-scroll-h-min)' : null"
      style="display: flex; flex-direction: column"
    >
      <ng-template #oTen let-hang
        ><a href="#ten" class="lien-ten">{{ hang.ten }}</a></ng-template
      >
      <ng-template #oHanhDong let-hang let-compact="compact">
        <button type="button" class="nut-sua" [attr.data-compact]="compact">
          Sửa {{ hang.ten }}
        </button>
        <button
          type="button"
          class="nut-menu"
          aria-haspopup="menu"
          [attr.aria-expanded]="moId() === hang.id ? 'true' : 'false'"
        >
          ⋯
        </button>
      </ng-template>
      <app-data-table
        [rows]="rows()"
        [columns]="[{ key: 'ten', header: 'Tên', cell: oTen }]"
        caption="Bảng thử"
        rowKey="id"
        [state]="trangThai()"
        paginationAriaLabel="Phân trang"
        [totalRecords]="rows().length"
        [rowActionsTemplate]="coHanhDong() ? oHanhDong : null"
        [rowActionsLabel]="nhanHanhDong()"
      >
        @if (coDauKhung()) {
          <div dataTableTop class="dau-thu">Đầu khung</div>
        }
      </app-data-table>
    </div>
  `,
})
class HostComponent {
  readonly cao = signal(400);
  // Mô phỏng mixin `man-danh-sach`, thứ bật sàn vùng cuộn (styles/_spacing.scss).
  readonly lapChieuCao = signal(true);
  readonly trangThai = signal<'idle' | 'loading'>('idle');
  readonly rows = signal<readonly Hang[]>(nhieuDong(40));
  readonly coHanhDong = signal(true);
  readonly nhanHanhDong = signal('Hành động');
  readonly coDauKhung = signal(true);
  readonly moId = signal<number | null>(null);
}

describe('DataTableComponent — khung một viền, vùng cuộn duy nhất', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let el: HTMLElement;

  async function dung(): Promise<void> {
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    el = fixture.nativeElement as HTMLElement;
    document.body.appendChild(el); // cần nằm trong tài liệu để `:hover`/`:focus-within` tính được
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function ve(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  const q = <E extends Element = HTMLElement>(s: string): E => {
    const e = el.querySelector<E>(s);
    if (e === null) throw new Error(`không thấy ${s}`);
    return e;
  };
  const cs = (e: Element): CSSStyleDeclaration => getComputedStyle(e);
  const bien = (token: string): string => {
    const m = document.createElement('div');
    m.style.height = `var(${token})`;
    document.body.appendChild(m);
    const ra = getComputedStyle(m).height;
    m.remove();
    return ra;
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: PROVIDERS,
    });
  });

  afterEach(() => el.remove());

  it('khung lấp chiều cao vùng chứa; CHỈ vùng cuộn cuộn dọc (trang không tràn)', async () => {
    await dung();
    const khung = q('.data-table__khung');
    const goc = q('.data-table');
    expect(goc.getBoundingClientRect().height).withContext('khung = vùng chứa').toBeCloseTo(400, 0);
    expect(khung.scrollHeight)
      .withContext('bảng 40 dòng cao hơn vùng cuộn')
      .toBeGreaterThan(khung.clientHeight);
    expect(cs(khung).overflowY).toBe('auto');
    expect(q('.vung-chua').scrollHeight)
      .withContext('vùng chứa không tràn')
      .toBeLessThanOrEqual(400);
  });

  it('một viền bao cả dải đầu, vùng cuộn và dải phân trang; các dải trong không tự vẽ viền ngoài', async () => {
    await dung();
    const goc = q('.data-table');
    expect(cs(goc).borderTopWidth).toBe(bien('--border-w'));
    expect(goc.contains(q('.dau-thu'))).toBeTrue();
    expect(goc.contains(q('.data-table__khung'))).toBeTrue();
    expect(goc.contains(q('app-pagination'))).toBeTrue();
    const phanTrang = q('.data-table__phan-trang');
    expect(cs(phanTrang).borderLeftWidth).toBe('0px');
    expect(cs(phanTrang).borderTopWidth).withContext('chỉ có vạch ngăn phía trên').not.toBe('0px');
    // Dải phân trang nằm DƯỚI vùng cuộn.
    expect(phanTrang.getBoundingClientRect().top).toBeGreaterThanOrEqual(
      q('.data-table__khung').getBoundingClientRect().bottom - 1,
    );
  });

  it('slot [dataTableTop] có nội dung → dải đầu + vạch ngăn; trống → không vẽ dải lẫn vạch', async () => {
    await dung();
    const dau = q('.data-table__dau');
    expect(cs(dau).display).not.toBe('none');
    expect(cs(dau).borderBottomWidth).not.toBe('0px');
    host.coDauKhung.set(false);
    await ve();
    expect(cs(q('.data-table__dau')).display).toBe('none');
  });

  it('trang KHÔNG cấp chiều cao (không có sàn) → khung cao theo nội dung, không để trống tới 280px', async () => {
    await dung();
    host.lapChieuCao.set(false);
    host.cao.set(0);
    host.rows.set(nhieuDong(3));
    await ve();
    q<HTMLElement>('.vung-chua').style.height = 'auto';
    await ve();
    const than = q('.data-table__than').getBoundingClientRect().height;
    const bang = q('.data-table__khung table').getBoundingClientRect().height;
    expect(than)
      .withContext('thân bảng ôm nội dung')
      .toBeLessThan(bang + 4);
    expect(than).toBeLessThan(parseFloat(bien('--data-table-scroll-h-min')));
  });

  it('Tab tới phần tử bất kỳ trong dòng (ô đầu) → nút hành động của dòng hiện', async () => {
    await dung();
    const hang = q('.data-table__hang');
    const vung = hang.querySelector<HTMLElement>('.data-table__hanh-dong')!;
    // Máy kiểm có thể có thiết bị cảm ứng (`hover: none` → luôn hiện): chỉ khẳng định khi nghỉ = 0.
    if (cs(vung).opacity === '1') return;
    hang.querySelector<HTMLElement>('.lien-ten')!.focus();
    expect(cs(vung).opacity).toBe('1');
  });

  it('đang tải lại: lớp phủ có spinner, chặn click xuống nút của dữ liệu cũ, không bẫy focus', async () => {
    await dung();
    host.trangThai.set('loading');
    await ve();
    const than = q('.data-table__than');
    const phu = q('.data-table__tai-lai');
    expect(phu.querySelector('.pi-spinner.pi-spin')).not.toBeNull();
    // Không dùng `elementFromPoint`: lớp phủ của bộ test khác còn sót có thể nằm trên cùng.
    const h = than.getBoundingClientRect();
    const r = phu.getBoundingClientRect();
    expect([r.left, r.top, r.width, r.height]).toEqual([h.left, h.top, h.width, h.height]);
    expect(cs(phu).pointerEvents).withContext('lớp phủ chặn click').not.toBe('none');
    expect(parseInt(cs(phu).zIndex, 10)).toBeGreaterThan(
      parseInt(cs(q('thead th.data-table__o-hanh-dong')).zIndex, 10),
    );
    // Vùng cuộn vẫn nhận focus và cuộn được bằng bàn phím: lớp phủ không inert, không focus-trap.
    const khung = q('.data-table__khung');
    expect(phu.querySelector('[tabindex], button, a')).toBeNull();
    khung.focus();
    expect(document.activeElement).toBe(khung);
    host.trangThai.set('idle');
    await ve();
    expect(el.querySelector('.data-table__tai-lai')).toBeNull();
  });

  it('vùng chứa thấp hơn sàn → vùng cuộn giữ sàn --data-table-scroll-h-min, không co tiếp', async () => {
    await dung();
    host.cao.set(120);
    await ve();
    const san = parseFloat(bien('--data-table-scroll-h-min'));
    expect(san).toBe(280);
    expect(q('.data-table__than').getBoundingClientRect().height).toBeGreaterThanOrEqual(san);
  });

  it('tiêu đề cột dính đỉnh vùng cuộn: cuộn dọc thì th vẫn ở đỉnh, lớp --z-sticky', async () => {
    await dung();
    const khung = q('.data-table__khung');
    const th = q('thead th');
    expect(cs(th).position).toBe('sticky');
    const m = document.createElement('div');
    m.style.zIndex = 'var(--z-sticky)';
    document.body.appendChild(m);
    const zSticky = parseInt(getComputedStyle(m).zIndex, 10);
    m.remove();
    expect(parseInt(cs(th).zIndex, 10)).toBe(zSticky);

    khung.scrollTop = 300;
    await ve();
    expect(khung.scrollTop).withContext('tiền đề: đã cuộn').toBeGreaterThan(0);
    expect(th.getBoundingClientRect().top).toBeCloseTo(khung.getBoundingClientRect().top, 0);
  });

  it('dòng thân bảng có quy tắc đổi nền khi rê chuột (bọc @media (hover: hover)) bằng --color-surface-2', async () => {
    await dung();
    // `:hover` không giả lập được bằng DOM; Playwright (e2e/man) hover thật. Ở đây khoá quy tắc.
    const luat: string[] = [];
    const duyet = (rules: CSSRuleList, trongHover: boolean): void => {
      for (const r of Array.from(rules)) {
        if (r instanceof CSSMediaRule)
          duyet(r.cssRules, trongHover || /hover:\s*hover/.test(r.conditionText));
        else if (
          r instanceof CSSStyleRule &&
          trongHover &&
          /\.data-table__hang\S*:hover/.test(r.selectorText)
        ) {
          luat.push(r.cssText);
        }
      }
    };
    for (const ss of Array.from(document.styleSheets)) {
      try {
        duyet(ss.cssRules, false);
      } catch {
        /* stylesheet khác nguồn */
      }
    }
    expect(luat.length).toBeGreaterThan(0);
    expect(luat.join('\n')).toContain('--color-surface-2');
  });
});

describe('DataTableComponent — cột hành động dùng chung (rowActionsTemplate)', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let el: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: PROVIDERS,
    });
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    host.rows.set(nhieuDong(5));
    el = fixture.nativeElement as HTMLElement;
    document.body.appendChild(el);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  afterEach(() => el.remove());

  const cs = (e: Element): CSSStyleDeclaration => getComputedStyle(e);
  const o = (i = 0): HTMLElement =>
    el.querySelectorAll<HTMLElement>('tbody td.data-table__o-hanh-dong')[i];
  const vung = (i = 0): HTMLElement => o(i).querySelector<HTMLElement>('.data-table__hanh-dong')!;

  async function ve(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('luôn là cột cuối, mỗi dòng một ô; th scope="col" mang tên cột ẩn thị giác', () => {
    const cacTh = Array.from(el.querySelectorAll('thead th'));
    expect(cacTh.length).toBe(2);
    const thHd = cacTh[1];
    expect(thHd.classList).toContain('data-table__o-hanh-dong');
    expect(thHd.getAttribute('scope')).toBe('col');
    const ten = thHd.querySelector('.sr-only');
    expect(ten?.textContent?.trim()).toBe('Hành động');
    expect(el.querySelectorAll('tbody td.data-table__o-hanh-dong').length).toBe(5);
    const hang = o(0).parentElement!;
    expect(hang.lastElementChild).toBe(o(0));
  });

  it('rowActionsTemplate = null → không có cột, kể cả th', async () => {
    host.coHanhDong.set(false);
    await ve();
    expect(el.querySelectorAll('.data-table__o-hanh-dong').length).toBe(0);
    expect(el.querySelectorAll('thead th').length).toBe(1);
  });

  it('có template mà thiếu rowActionsLabel → ném lỗi, không vẽ cột không tên', async () => {
    host.nhanHanhDong.set('');
    expect(() => fixture.detectChanges()).toThrowError(/rowActionsLabel/);
    host.nhanHanhDong.set('Hành động');
  });

  it('ghim mép phải (sticky right: 0), nền đặc kế thừa nền dòng, lớp trên th của thân bảng nhưng dưới th tiêu đề', () => {
    const td = o(0);
    expect(cs(td).position).toBe('sticky');
    expect(cs(td).right).toBe('0px');
    expect(cs(td).backgroundColor).withContext('không trong suốt').not.toBe('rgba(0, 0, 0, 0)');
    const thHd = el.querySelector<HTMLElement>('thead th.data-table__o-hanh-dong')!;
    expect(parseInt(cs(thHd).zIndex, 10)).toBeGreaterThan(parseInt(cs(td).zIndex, 10));
    expect(parseInt(cs(td).zIndex, 10)).toBeGreaterThan(0);
  });

  it('nghỉ: ẩn bằng opacity — nút KHÔNG display:none / visibility:hidden, vẫn nằm trong thứ tự Tab', () => {
    const v = vung(0);
    if (matchMedia('(hover: none)').matches) return; // thiết bị không rê chuột: luôn hiện — ca bên dưới
    expect(cs(v).opacity).toBe('0');
    const nut = v.querySelector<HTMLButtonElement>('.nut-sua')!;
    expect(cs(nut).display).not.toBe('none');
    expect(cs(nut).visibility).toBe('visible');
    expect(nut.tabIndex).toBe(0);
    expect(nut.getClientRects().length)
      .withContext('vẫn có hộp, chỉ trong suốt')
      .toBeGreaterThan(0);
  });

  it('không transition: hiện/ẩn tức thì', () => {
    expect(cs(vung(0)).transitionDuration).toMatch(/^0s(, 0s)*$/);
  });

  it('Tab/focus vào một nút trong dòng → :focus-within hiện nút (không bao giờ focus vào nút vô hình)', async () => {
    const v = vung(2);
    v.querySelector<HTMLButtonElement>('.nut-sua')!.focus();
    await ve();
    expect(document.activeElement).toBe(v.querySelector('.nut-sua'));
    expect(cs(v).opacity).toBe('1');
    expect(cs(vung(0)).opacity)
      .withContext('dòng khác vẫn nghỉ')
      .toBe(matchMedia('(hover: none)').matches ? '1' : '0');
  });

  it('một nút trong ô có aria-expanded="true" → ô hiện dù focus đã rời dòng (Menu dựng qua portal)', async () => {
    if (matchMedia('(hover: none)').matches) return;
    expect(cs(vung(1)).opacity).toBe('0');
    host.moId.set(2); // dòng thứ hai
    await ve();
    expect(cs(vung(1)).opacity).toBe('1');
    expect(cs(vung(0)).opacity).toBe('0');
  });

  it('ngữ cảnh template: compact=false ở khổ rộng; true khi truy vấn dưới $bp-xs khớp', async () => {
    expect(o(0).querySelector('.nut-sua')?.getAttribute('data-compact')).toBe('false');

    const dangKy: ((e: { matches: boolean }) => void)[] = [];
    spyOn(window, 'matchMedia').and.callFake(
      () =>
        ({
          matches: true,
          addEventListener: (_: string, f: (e: { matches: boolean }) => void) => dangKy.push(f),
          removeEventListener: () => undefined,
        }) as unknown as MediaQueryList,
    );
    const f2 = TestBed.createComponent(HostComponent);
    f2.componentInstance.rows.set(nhieuDong(2));
    f2.detectChanges();
    await f2.whenStable();
    f2.detectChanges();
    const nut = (f2.nativeElement as HTMLElement).querySelector('.nut-sua');
    expect(nut?.getAttribute('data-compact')).toBe('true');

    dangKy.forEach((f) => f({ matches: false })); // co giãn lên khổ rộng
    f2.detectChanges();
    await f2.whenStable();
    f2.detectChanges();
    expect(
      (f2.nativeElement as HTMLElement).querySelector('.nut-sua')?.getAttribute('data-compact'),
    ).toBe('false');
  });
});

describe('DataTableComponent — cột không sắp xếp được', () => {
  interface SapXepTest {
    ariaSort(cot: { key: string; header: string; sortable?: boolean }): string;
    sapXep(cot: { key: string; header: string; sortable?: boolean }): void;
    sortChange: { emit(v: unknown): void };
  }

  it('cột không `sortable`: aria-sort "none" và bấm sắp xếp không phát sortChange', () => {
    TestBed.configureTestingModule({ providers: PROVIDERS });
    const f = TestBed.createComponent(HostComponent);
    f.detectChanges();
    const bang = f.debugElement.query(By.directive(DataTableComponent))
      .componentInstance as unknown as SapXepTest;
    const phat = spyOn(bang.sortChange, 'emit');
    const cot = { key: 'ten', header: 'Tên' };
    expect(bang.ariaSort(cot)).toBe('none');
    bang.sapXep(cot);
    expect(phat).not.toHaveBeenCalled();
    expect(bang.ariaSort({ ...cot, sortable: true })).toBe('none'); // sortable nhưng chưa là cột đang sắp xếp
  });
});
