import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { providePrimeNG } from 'primeng/config';
import { Observable, of } from 'rxjs';

import { CORE_PRIME_PRESET } from '../../../core/theme/prime-preset';
import { DrawerComponent } from './drawer.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

@Component({
  standalone: true,
  imports: [DrawerComponent],
  template: `
    <main>
      <span hidden>{{ nhip() }}</span>
      @if (coNutMo()) {
        <button id="nut-mo" type="button" (click)="mo()">Mở</button>
      }
    </main>
    <app-drawer
      [open]="open()"
      [title]="'Lọc người dùng'"
      [panelId]="'panel-thu'"
      [loading]="dangTai()"
      [position]="viTri()"
      (closed)="soLanDong = soLanDong + 1; open.set(false)"
    >
      @if (coONhap()) {
        <input id="o-1" type="text" />
        <input id="o-2" type="text" />
      }
      <button slot="footer" id="nut-chan" type="button">Áp dụng</button>
    </app-drawer>
  `,
})
class HostComponent {
  readonly open = signal(false);
  readonly coNutMo = signal(true);
  readonly coONhap = signal(true);
  readonly dangTai = signal(false);
  /** Nhịp vẽ lại: một thay đổi signal để fixture zoneless có lần vẽ lại (xem `choGoLopPhu`). */
  readonly nhip = signal(0);
  readonly viTri = signal<'right' | 'left' | 'bottom'>('right');
  soLanDong = 0;
  mo(): void {
    this.open.set(true);
  }
}

describe('DrawerComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  async function onDinh(): Promise<void> {
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
    await new Promise((r) => setTimeout(r));
  }

  /**
   * Chờ điều kiện đúng. PrimeNG 21 thoát phần tử bằng hoạt ảnh CSS thật (lớp `p-leave-to`, gỡ khỏi
   * DOM khi `transitionend`), không còn gỡ đồng bộ như bản 20 — nên không thể đo ngay sau `onDinh`.
   */
  async function choDen(dieuKien: () => boolean, toiDaMs = 2000): Promise<void> {
    const batDau = Date.now();
    while (!dieuKien() && Date.now() - batDau < toiDaMs) {
      await new Promise((xong) => setTimeout(xong, 20));
      fixture.detectChanges();
    }
  }

  /**
   * Chờ lớp phủ được gỡ khỏi <body>. Thư viện gỡ nó khi pha ra kết thúc (lớp CSS theo pha, ADR-0123);
   * trong fixture zoneless không view nào bẩn thì không có lần vẽ lại nào tự tới — nên mỗi nhịp chờ
   * bơm một thay đổi signal rồi vẽ lại, không phụ thuộc việc thư viện gỡ phần tử ở nhịp nào.
   */
  async function choGoLopPhu(toiDaMs = 2000): Promise<void> {
    const batDau = Date.now();
    while (document.body.querySelector('.p-drawer-mask') && Date.now() - batDau < toiDaMs) {
      host.nhip.update((n) => n + 1);
      await new Promise((xong) => setTimeout(xong, 20));
      fixture.detectChanges();
    }
  }

  function goc(): HTMLElement | null {
    return document.body.querySelector<HTMLElement>('#panel-thu');
  }

  function nutMo(): HTMLButtonElement {
    return document.getElementById('nut-mo') as HTMLButtonElement;
  }

  /** Mở bằng đúng đường người dùng: bấm nút (nút giữ focus) rồi chờ drawer dựng xong. */
  async function moBangNut(): Promise<void> {
    nutMo().focus();
    nutMo().click();
    await onDinh();
  }

  function nhanEscape(): void {
    // `p-drawer` nghe `event.which == 27` ở document; `which` của KeyboardEvent tự dựng luôn là 0.
    const su = new KeyboardEvent('keydown', { key: 'Escape', code: 'Escape', bubbles: true });
    Object.defineProperty(su, 'which', { value: 27 });
    document.dispatchEvent(su);
  }

  beforeEach(async () => {
    // Khoá cuộn nền là một lớp trên <body>: ca của spec khác để sót thì hai ca "khoá cuộn" dưới đo sai.
    document.body.classList.remove('p-overflow-hidden');
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
    fixture.detectChanges();
    await onDinh();
  });

  afterEach(() => {
    fixture.destroy();
    // Lớp phủ chỉ gỡ khi hoạt ảnh CSS kết thúc; fixture bị huỷ trước đó thì nó rò sang ca sau.
    document.querySelectorAll('.p-drawer-mask').forEach((m) => m.remove());
  });

  it('đóng thì không dựng gì vào <body>', () => {
    expect(goc()).toBeNull();
    expect(document.body.querySelector('.p-drawer-mask')).toBeNull();
  });

  it('mở: phần tử gốc mang role="dialog", aria-modal, aria-labelledby trỏ vào tiêu đề và id = panelId', async () => {
    await moBangNut();
    const g = goc()!;
    expect(g).not.toBeNull();
    expect(g.getAttribute('role')).toBe('dialog');
    expect(g.getAttribute('aria-modal')).toBe('true');
    const tieuDe = document.getElementById(g.getAttribute('aria-labelledby')!);
    expect(tieuDe?.textContent).toContain('Lọc người dùng');
    expect(g.classList).toContain('app-drawer--md');
    expect(document.body.querySelector('.p-drawer-mask')).not.toBeNull();
  });

  it('mở: focus vào ô nhập ĐẦU TIÊN của thân, không vào nút đóng', async () => {
    await moBangNut();
    expect(document.activeElement?.id).toBe('o-1');
  });

  it('mở: thân không có ô nhập → focus vào nút đóng', async () => {
    host.coONhap.set(false);
    await onDinh();
    await moBangNut();
    expect(document.activeElement?.closest('.p-drawer-close-button')).not.toBeNull();
  });

  it('bẫy focus: Tab ra khỏi phần tử cuối vòng về phần tử đầu, không thoát ra nền', async () => {
    await moBangNut();
    const g = goc()!;
    const sentinels = g.querySelectorAll<HTMLElement>('[data-p-hidden-focusable="true"]');
    expect(sentinels.length).toBe(2);
    // Đang ở phần tử focus được cuối (nút ở chân), Tab chạm sentinel cuối.
    document.getElementById('nut-chan')!.focus();
    sentinels[1].focus();
    expect(g.contains(document.activeElement)).toBeTrue();
    expect(document.activeElement).not.toBe(sentinels[1]);
    expect(document.activeElement?.closest('.p-drawer-close-button')).not.toBeNull();

    // Shift+Tab từ phần tử đầu chạm sentinel đầu → sang phần tử cuối.
    sentinels[0].focus();
    expect(g.contains(document.activeElement)).toBeTrue();
    expect(document.activeElement?.id).toBe('nut-chan');
  });

  it('Escape: phát closed, đóng drawer và trả focus về nút đã mở', async () => {
    await moBangNut();
    expect(goc()).not.toBeNull();
    nhanEscape();
    await onDinh();
    await choDen(() => goc() === null);
    expect(host.soLanDong).toBe(1);
    expect(goc()).toBeNull();
    expect(document.activeElement).toBe(nutMo());
  });

  it('nút đóng: phát closed và trả focus về nút đã mở', async () => {
    await moBangNut();
    document.body.querySelector<HTMLButtonElement>('.p-drawer-close-button button')!.click();
    await onDinh();
    await choDen(() => goc() === null);
    expect(host.soLanDong).toBe(1);
    expect(goc()).toBeNull();
    expect(document.activeElement).toBe(nutMo());
  });

  it('bấm lớp phủ: phát closed và trả focus về nút đã mở', async () => {
    await moBangNut();
    document.body.querySelector<HTMLElement>('.p-drawer-mask')!.click();
    await onDinh();
    expect(host.soLanDong).toBe(1);
    expect(document.activeElement).toBe(nutMo());
  });

  it('trang cha đặt open = false (bấm Áp dụng): KHÔNG phát closed nhưng vẫn trả focus về nút đã mở', async () => {
    await moBangNut();
    host.open.set(false);
    await onDinh();
    await choDen(() => goc() === null);
    expect(host.soLanDong).toBe(0);
    expect(goc()).toBeNull();
    expect(document.activeElement).toBe(nutMo());
  });

  /**
   * Lớp phủ do thư viện dựng thẳng vào <body>, NGOÀI cây của component, và chỉ thư viện gỡ nó: khi
   * `animationend` bắn trên lớp phủ (pha ra). Hai ca dưới giữ hai đầu của hợp đồng đó — đóng bình
   * thường thì gỡ, và component bị huỷ GIỮA pha ra thì cũng không để lại lớp phủ chặn cả màn.
   */
  it('đóng xong hoạt ảnh ra: lớp phủ được gỡ khỏi <body> và khoá cuộn nền được nhả', async () => {
    await moBangNut();
    expect(document.body.classList).toContain('p-overflow-hidden');
    host.open.set(false);
    await onDinh();
    await choGoLopPhu();
    expect(document.body.querySelector('.p-drawer-mask')).toBeNull();
    expect(document.body.classList).not.toContain('p-overflow-hidden');
  });

  it('huỷ component giữa lúc lớp phủ đang ra: không để lại lớp phủ và khoá cuộn nền', async () => {
    await moBangNut();
    host.open.set(false);
    await choDen(
      () => document.body.querySelector('.p-drawer-mask.p-overlay-mask-leave-active') !== null,
    );
    expect(document.body.querySelector('.p-drawer-mask.p-overlay-mask-leave-active'))
      .withContext('phải đang ở giữa pha ra của lớp phủ')
      .not.toBeNull();
    fixture.destroy();
    expect(document.body.querySelector('.p-drawer-mask')).toBeNull();
    expect(document.body.classList).not.toContain('p-overflow-hidden');
  });

  it('phần tử đã mở không còn trong DOM → focus về vùng <main>', async () => {
    await moBangNut();
    host.coNutMo.set(false);
    await onDinh();
    expect(document.getElementById('nut-mo')).toBeNull();
    host.open.set(false);
    await onDinh();
    expect(document.activeElement).toBe(document.querySelector('main'));
  });

  it('loading → aria-busy="true" trên thân, đầu và chân vẫn có mặt', async () => {
    host.dangTai.set(true);
    await moBangNut();
    const g = goc()!;
    expect(g.querySelector('.app-drawer__than')?.getAttribute('aria-busy')).toBe('true');
    expect(g.querySelector('.p-drawer-header')).not.toBeNull();
    expect(g.querySelector('#nut-chan')).not.toBeNull();
  });

  it('chân nằm ngoài thân cuộn (chỉ thân cuộn)', async () => {
    await moBangNut();
    const g = goc()!;
    expect(g.querySelector('.p-drawer-content #nut-chan')).toBeNull();
    expect(g.querySelector('.p-drawer-footer #nut-chan')).not.toBeNull();
  });

  it('dưới $bp-sm: neo đáy; position="left" giữ neo trái ở mọi ngưỡng', async () => {
    fixture.destroy();
    spyOn(window, 'matchMedia').and.returnValue({
      matches: true,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
    } as unknown as MediaQueryList);
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    await onDinh();
    await moBangNut();
    expect(goc()!.classList).toContain('app-drawer--bottom');
    host.open.set(false);
    host.viTri.set('left');
    await onDinh();
    await moBangNut();
    expect(goc()!.classList).toContain('app-drawer--left');
  });
});

/**
 * Luật F62 / ADR-0123 quyết định 5: thời lượng và đường cong chuyển động của tấm drawer và lớp phủ đi
 * bằng token QUA CSS. Spec mở drawer thật và đo phần tử đang mang lớp pha — bắt được ba ca mà không
 * cổng script nào thấy: thư viện đổi tên lớp pha ở một lượt nâng (đã xảy ra: khối lớp phủ cũ trỏ
 * `p-overlay-mask-enter` chết sau lượt nâng 20 → 21), một lần truyền `duration` bằng số (thư viện ghi
 * style nội tuyến, token bị bỏ qua), và một `transition` dài hơn `animation` trên chính tấm (thư viện
 * định thời theo giá trị lớn nhất; tấm nhảy về chỗ cũ trước khi ẩn).
 *
 * Đo ngay trong lúc lớp pha còn trên phần tử — `MutationObserver` đọc style ở microtask ngay sau khi
 * lớp được gắn, không qua `setTimeout` đoán giờ — nên không có cửa sổ thời gian để chập chờn. Bật
 * `prefers-reduced-motion` thì thư viện bỏ qua pha (`safe`), spec không có gì để đo.
 */
describe('DrawerComponent — chuyển động đi bằng token qua CSS (F62)', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  interface MauPha {
    ten: string;
    thoiLuong: string;
    duongCong: string;
    transitionDuration: string;
    transitionDelay: string;
    noiTuyen: string;
  }

  /** Chờ phần tử khớp `chon`, rồi đọc style đã tính NGAY lúc đó. */
  function batPha(chon: string): Promise<MauPha> {
    return new Promise((resolve, reject) => {
      const doc = (): boolean => {
        const el = document.querySelector<HTMLElement>(chon);
        if (!el) {
          return false;
        }
        const t = getComputedStyle(el);
        resolve({
          ten: t.animationName,
          thoiLuong: t.animationDuration,
          duongCong: t.animationTimingFunction,
          transitionDuration: t.transitionDuration,
          transitionDelay: t.transitionDelay,
          noiTuyen: el.style.animationDuration + el.style.transitionDuration,
        });
        return true;
      };
      if (doc()) {
        return;
      }
      const quanSat = new MutationObserver(() => {
        if (doc()) {
          quanSat.disconnect();
          clearTimeout(hen);
        }
      });
      quanSat.observe(document.body, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeFilter: ['class'],
      });
      const hen = setTimeout(() => {
        quanSat.disconnect();
        reject(new Error(`Không thấy phần tử "${chon}" trong 2 giây — lớp pha đổi tên?`));
      }, 2000);
    });
  }

  /** Giá trị đã tính của một token, qua cùng bộ máy tính với phần tử đo (không chép số vào spec). */
  function giaTriToken(thuocTinh: string, token: string): string {
    expect(getComputedStyle(document.documentElement).getPropertyValue(token).trim())
      .withContext(`token ${token} chưa khai`)
      .not.toBe('');
    const dau = document.createElement('div');
    dau.style.setProperty(thuocTinh, `var(${token})`);
    document.body.appendChild(dau);
    const v = getComputedStyle(dau).getPropertyValue(thuocTinh);
    dau.remove();
    return v;
  }

  function mili(dsach: string): number[] {
    return dsach.split(',').map((x) => {
      const so = parseFloat(x);
      return x.trim().endsWith('ms') ? so : so * 1000;
    });
  }

  async function choHetPha(chon: string): Promise<void> {
    const batDau = Date.now();
    while (document.querySelector(chon) && Date.now() - batDau < 2000) {
      await new Promise((r) => setTimeout(r, 20));
    }
  }

  function kiemMau(
    mau: MauPha,
    token: { ten: string; thoiLuong: string; duongCong: string },
  ): void {
    expect(mau.ten).withContext('keyframes của thư viện phải còn nguyên').toBe(token.ten);
    expect(mau.thoiLuong).toBe(giaTriToken('animation-duration', token.thoiLuong));
    expect(mau.duongCong).toBe(giaTriToken('animation-timing-function', token.duongCong));
    expect(mau.noiTuyen)
      .withContext('thư viện ghi thời lượng nội tuyến — có ai truyền `duration` bằng số?')
      .toBe('');
    // Thư viện kết thúc pha theo thời lượng LỚN NHẤT: một transition dài hơn animation giành quyền định thời.
    const animation = Math.max(...mili(mau.thoiLuong));
    const transition = Math.max(
      ...mili(mau.transitionDuration).map((d, i) => d + (mili(mau.transitionDelay)[i] ?? 0)),
    );
    expect(transition)
      .withContext('có transition dài hơn animation trên chính phần tử chuyển động')
      .toBeLessThanOrEqual(animation);
  }

  beforeEach(async () => {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      pending('prefers-reduced-motion bật — thư viện bỏ qua pha, không có gì để đo');
    }
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideZonelessChangeDetection(),
        providePrimeNG({
          theme: {
            preset: CORE_PRIME_PRESET,
            options: { darkModeSelector: false, cssLayer: true },
          },
        }),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  async function datOpen(mo: boolean): Promise<void> {
    host.open.set(mo);
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
  }

  it('tấm — pha vào: --dur-base, --ease-decelerate', async () => {
    const pha = batPha('.app-drawer.p-drawer-enter-right');
    await datOpen(true);
    kiemMau(await pha, {
      ten: 'p-animate-drawer-enter-right',
      thoiLuong: '--dur-base',
      duongCong: '--ease-decelerate',
    });
  });

  it('tấm — pha ra: --dur-base, --ease-accelerate', async () => {
    await datOpen(true);
    await choHetPha('.app-drawer.p-drawer-enter-right');
    const pha = batPha('.app-drawer.p-drawer-leave-right');
    await datOpen(false);
    kiemMau(await pha, {
      ten: 'p-animate-drawer-leave-right',
      thoiLuong: '--dur-base',
      duongCong: '--ease-accelerate',
    });
  });

  it('lớp phủ — cả hai pha: --dur-base, --ease-standard, keyframes riêng của app', async () => {
    const vao = batPha('.p-drawer-mask.p-overlay-mask-enter-active');
    await datOpen(true);
    const mauVao = await vao;
    expect(mauVao.ten).toBe('app-drawer-mask-vao');
    expect(mauVao.thoiLuong).toBe(giaTriToken('animation-duration', '--dur-base'));
    expect(mauVao.duongCong).toBe(giaTriToken('animation-timing-function', '--ease-standard'));

    // Lớp `p-overlay-mask-enter-active` ở lại suốt lúc drawer mở (chỉ pha ra mới gỡ nó) — chờ nó biến mất là
    // chờ hết 2 giây. Chờ chính hoạt ảnh vào kết thúc.
    await Promise.all(
      (document.querySelector('.p-drawer-mask') as HTMLElement)
        .getAnimations()
        .map((a) => a.finished),
    );
    const ra = batPha('.p-drawer-mask.p-overlay-mask-leave-active');
    await datOpen(false);
    const mauRa = await ra;
    expect(mauRa.ten).toBe('app-drawer-mask-ra');
    expect(mauRa.thoiLuong).toBe(giaTriToken('animation-duration', '--dur-base'));
    expect(mauRa.duongCong).toBe(giaTriToken('animation-timing-function', '--ease-standard'));
  });
});
