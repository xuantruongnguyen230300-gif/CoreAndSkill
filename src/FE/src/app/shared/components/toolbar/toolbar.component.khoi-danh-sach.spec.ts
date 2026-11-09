import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { ToolbarChip, ToolbarComponent } from './toolbar.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

@Component({
  standalone: true,
  imports: [ToolbarComponent],
  template: `
    <app-toolbar
      [variant]="variant()"
      [placement]="placement()"
      searchLabel="Tìm kiếm"
      filterLabel="Bộ lọc"
      [filterAriaLabel]="
        soDieuKien() > 0 ? 'Bộ lọc, ' + soDieuKien() + ' điều kiện đang bật' : null
      "
      [activeFilterCount]="soDieuKien()"
      [filterPanelOpen]="mo()"
      filterPanelId="panel-loc"
      [chips]="chips()"
      [disabled]="khoa()"
      (filterPanelToggled)="daPhat.push($event)"
    >
      <button toolbarActions type="button" class="hd-phu">Phụ</button>
      <button toolbarActions type="button" class="hd-chinh">Chính</button>
    </app-toolbar>
    <app-toolbar searchLabel="Tìm kiếm thứ hai" variant="search" />
  `,
})
class HostComponent {
  readonly variant = signal<'full' | 'search'>('full');
  readonly placement = signal<'standalone' | 'attached'>('standalone');
  readonly soDieuKien = signal(0);
  readonly mo = signal(false);
  readonly khoa = signal(false);
  readonly chips = signal<readonly ToolbarChip[]>([]);
  readonly daPhat: boolean[] = [];
}

/** Toolbar.md §"Nút lọc — đặc tả thi công", §"Cách đặt", §API `searchLabel`, §Accessibility (thứ tự Tab). */
describe('ToolbarComponent — nút lọc, cách đặt, id ô tìm, thứ tự Tab', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let el: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    el = fixture.nativeElement as HTMLElement;
    document.body.appendChild(el);
    await ve();
  });

  afterEach(() => el.remove());

  async function ve(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  const nutLoc = (): HTMLButtonElement => el.querySelector('.toolbar__nut-loc button')!;

  describe('nút lọc', () => {
    it('variant full: <button> thật, aria-haspopup="dialog", aria-expanded theo panel, aria-controls trỏ id panel', async () => {
      expect(nutLoc().type).toBe('button');
      expect(nutLoc().getAttribute('aria-haspopup')).toBe('dialog');
      expect(nutLoc().getAttribute('aria-expanded')).toBe('false');
      expect(nutLoc().getAttribute('aria-controls')).toBe('panel-loc');
      host.mo.set(true);
      await ve();
      expect(nutLoc().getAttribute('aria-expanded')).toBe('true');
    });

    it('0 điều kiện: không badge, không aria-label — tên đọc lên là chữ nhìn thấy', () => {
      expect(el.querySelector('.toolbar__nut-loc app-badge')).toBeNull();
      expect(nutLoc().hasAttribute('aria-label')).toBeFalse();
      expect(nutLoc().textContent).toContain('Bộ lọc');
    });

    it('3 điều kiện: badge neutral sm aria-hidden mang con số; aria-label mở đầu bằng đúng chữ nhìn thấy', async () => {
      host.soDieuKien.set(3);
      await ve();
      const badge = el.querySelector<HTMLElement>('.toolbar__nut-loc app-badge')!;
      expect(badge.getAttribute('aria-hidden')).toBe('true');
      expect(badge.textContent?.trim()).toBe('3');
      expect(badge.querySelector('.badge--sm')).not.toBeNull();
      const nhan = nutLoc().getAttribute('aria-label')!;
      expect(nhan).toBe('Bộ lọc, 3 điều kiện đang bật');
      expect(nhan.startsWith('Bộ lọc')).withContext('WCAG 2.5.3 — Label in Name').toBeTrue();
    });

    it('bấm phát filterPanelToggled với !filterPanelOpen', async () => {
      nutLoc().click();
      host.mo.set(true);
      await ve();
      nutLoc().click();
      expect(host.daPhat).toEqual([true, false]);
    });

    it('disabled của dải → nút lọc disabled thật; variant search → không có nút lọc', async () => {
      host.khoa.set(true);
      await ve();
      expect(nutLoc().disabled).toBeTrue();
      host.variant.set('search');
      await ve();
      expect(el.querySelector('.toolbar__nut-loc')).toBeNull();
    });
  });

  describe('cách đặt', () => {
    const dai = (): HTMLElement => el.querySelector('app-toolbar .toolbar')!;

    it('standalone (mặc định): viền, bo góc, bóng riêng', () => {
      const cs = getComputedStyle(dai());
      expect(cs.borderTopWidth).toBe('1px');
      expect(cs.borderTopLeftRadius).not.toBe('0px');
      expect(cs.boxShadow).not.toBe('none');
    });

    it('attached: không viền, không bo, không bóng; nền đặc', async () => {
      host.placement.set('attached');
      await ve();
      const cs = getComputedStyle(dai());
      expect(cs.borderTopWidth).toBe('0px');
      expect(cs.borderTopLeftRadius).toBe('0px');
      expect(cs.boxShadow).toBe('none');
      expect(cs.backgroundColor).not.toBe('rgba(0, 0, 0, 0)');
    });
  });

  describe('ô tìm', () => {
    it('mỗi thể hiện một id duy nhất; <label for> trỏ đúng ô của chính nó', () => {
      const oTim = Array.from(el.querySelectorAll<HTMLInputElement>('.toolbar__tim-input'));
      expect(oTim.length).toBe(2);
      expect(oTim[0].id).not.toBe(oTim[1].id);
      Array.from(el.querySelectorAll('app-toolbar')).forEach((tb) => {
        const input = tb.querySelector<HTMLInputElement>('input')!;
        expect(tb.querySelector('label')?.getAttribute('for')).toBe(input.id);
      });
    });

    it('trần --toolbar-search-w-max (360px) từ $bp-md; dưới ngưỡng trải hết hàng, không trần', () => {
      const tim = el.querySelector<HTMLElement>('.toolbar__tim')!;
      const tran = document.createElement('div');
      tran.style.width = 'var(--toolbar-search-w-max)';
      document.body.appendChild(tran);
      const px = getComputedStyle(tran).width;
      tran.remove();
      expect(px).toBe('360px');
      const tuMd = matchMedia('(min-width: 900px)').matches;
      expect(getComputedStyle(tim).maxWidth).toBe(tuMd ? '360px' : 'none');
    });
  });

  it('thứ tự Tab (thứ tự DOM): ô tìm → nút lọc → chip → nhóm hành động', async () => {
    host.chips.set([{ key: 'a', label: 'Trạng thái', value: 'Hoạt động', removable: true }]);
    await ve();
    const dai = el.querySelector('app-toolbar')!;
    const tabbable = Array.from(dai.querySelectorAll<HTMLElement>('input, button')).filter(
      (e) => e.tabIndex >= 0,
    );
    const thuTu = tabbable.map((e) => {
      if (e.matches('input')) return 'tim';
      if (e.closest('.toolbar__nut-loc')) return 'loc';
      if (e.closest('.toolbar__chips')) return 'chip';
      if (e.closest('.toolbar__hanh-dong')) return 'hanh-dong';
      return '?';
    });
    expect(thuTu).toEqual(['tim', 'loc', 'chip', 'hanh-dong', 'hanh-dong']);
  });

  it('nút chính đặt cuối nhóm hành động theo thứ tự nội dung chiếu vào', () => {
    const nut = Array.from(el.querySelectorAll('.toolbar__hanh-dong button')).map(
      (b) => b.className,
    );
    expect(nut).toEqual(['hd-phu', 'hd-chinh']);
  });
});
