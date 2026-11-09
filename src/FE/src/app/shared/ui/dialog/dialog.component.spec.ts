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
import { DialogComponent } from './dialog.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

@Component({
  standalone: true,
  imports: [DialogComponent],
  template: `
    <app-dialog [open]="open()" title="Tiêu đề">
      @if (coO()) {
        <input id="o-1" />
        <input id="o-2" />
      } @else {
        <p>Chỉ có chữ</p>
      }
    </app-dialog>
  `,
})
class HostComponent {
  readonly open = signal(false);
  readonly coO = signal(true);
}

/** Dialog.md §Accessibility, *Focus lúc mở*; fe-ui-conventions.md §6.5. */
describe('DialogComponent — focus lúc mở', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

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
    fixture.detectChanges();
  });

  async function mo(): Promise<void> {
    host.open.set(true);
    fixture.detectChanges();
    await fixture.whenStable();
    await new Promise((r) => setTimeout(r, 400)); // quá cả setTimeout 5ms/200ms của thư viện
    fixture.detectChanges();
  }

  it('có ô nhập: focus vào ô ĐẦU TIÊN, mang cờ ô tự nhận focus', async () => {
    await mo();
    const o1 = document.getElementById('o-1')!;
    expect(document.activeElement).toBe(o1);
    expect(o1.hasAttribute('data-focus-luc-mo')).toBeTrue();
    expect(document.getElementById('o-2')!.hasAttribute('data-focus-luc-mo')).toBeFalse();
  });

  it('không có ô nhập: không bao giờ vào nút đóng', async () => {
    host.coO.set(false);
    fixture.detectChanges();
    await mo();
    const dang = document.activeElement as HTMLElement;
    expect(dang.closest('.p-dialog-header-actions')).toBeNull();
    expect(dang.closest('.p-dialog')).not.toBeNull();
  });
});

/**
 * Luật F62 / ADR-0123 quyết định 5: thời lượng và đường cong chuyển động của hộp đi bằng token QUA
 * CSS. Spec mở hộp thật và đo phần tử đang mang lớp pha — bắt được ba ca mà không cổng script nào
 * thấy: thư viện đổi tên lớp pha ở một lượt nâng (khối đè ở `_thu-vien.scss` thôi khớp), một lần
 * truyền `duration` bằng số (thư viện ghi style nội tuyến, token bị bỏ qua), và một `transition`
 * dài hơn `animation` trên chính phần tử (thư viện định thời theo giá trị lớn nhất).
 *
 * Đo ngay trong lúc lớp pha còn trên phần tử — `MutationObserver` đọc style ở microtask ngay sau
 * khi lớp được gắn, không qua `setTimeout` đoán giờ — nên không có cửa sổ thời gian để chập chờn.
 * Bật `prefers-reduced-motion` thì thư viện bỏ qua pha (`safe`), spec không có gì để đo.
 */
describe('DialogComponent — chuyển động đi bằng token qua CSS (F62)', () => {
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

  it('pha vào: --dur-base, --ease-decelerate', async () => {
    const pha = batPha('.app-dialog.p-dialog-enter-active');
    await datOpen(true);
    kiemMau(await pha, {
      ten: 'p-animate-dialog-enter',
      thoiLuong: '--dur-base',
      duongCong: '--ease-decelerate',
    });
  });

  it('pha ra: --dur-base, --ease-accelerate', async () => {
    await datOpen(true);
    await choHetPha('.app-dialog.p-dialog-enter-active');
    const pha = batPha('.app-dialog.p-dialog-leave-active');
    await datOpen(false);
    kiemMau(await pha, {
      ten: 'p-animate-dialog-leave',
      thoiLuong: '--dur-base',
      duongCong: '--ease-accelerate',
    });
  });
});

@Component({
  standalone: true,
  imports: [DialogComponent],
  template: `
    <app-dialog
      [open]="open()"
      title="Tiêu đề"
      (closed)="soLanDong = soLanDong + 1; open.set(false)"
    >
      <input id="o-1" />
    </app-dialog>
  `,
})
class HostDongComponent {
  readonly open = signal(false);
  soLanDong = 0;
}

/**
 * `open()` là nguồn sự thật, `hienThi` (hai chiều với PrimeNG) đi theo nó: trang cha đổi `open` thì
 * hộp theo ngay; người dùng đóng bằng nút X thì hộp đóng, phát `closed`, và lần `open = true` kế tiếp
 * mở lại được.
 */
describe('DialogComponent — hộp đi theo open', () => {
  let fixture: ComponentFixture<HostDongComponent>;
  let host: HostDongComponent;

  const hop = (): HTMLElement | null => document.body.querySelector<HTMLElement>('.app-dialog');

  async function choDen(dieuKien: () => boolean, toiDaMs = 2000): Promise<void> {
    const batDau = Date.now();
    while (!dieuKien() && Date.now() - batDau < toiDaMs) {
      await new Promise((xong) => setTimeout(xong, 20));
      fixture.detectChanges();
    }
  }

  async function datOpen(mo: boolean): Promise<void> {
    host.open.set(mo);
    fixture.detectChanges();
    await fixture.whenStable();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostDongComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HostDongComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('trang cha mở rồi đóng: hộp xuất hiện rồi biến mất, không phát closed', async () => {
    expect(hop()).toBeNull();
    await datOpen(true);
    await choDen(() => hop() !== null);
    expect(hop()).not.toBeNull();

    await datOpen(false);
    await choDen(() => hop() === null);
    expect(hop()).toBeNull();
    expect(host.soLanDong).toBe(0);
  });

  it('người dùng bấm nút X: hộp đóng, phát closed đúng một lần, và mở lại được', async () => {
    await datOpen(true);
    await choDen(() => hop() !== null);
    document.body.querySelector<HTMLButtonElement>('.p-dialog-close-button')!.click();
    await choDen(() => hop() === null);
    expect(hop()).toBeNull();
    expect(host.soLanDong).toBe(1);
    expect(host.open()).toBeFalse();

    await datOpen(true);
    await choDen(() => hop() !== null);
    expect(hop()).not.toBeNull();
  });
});
