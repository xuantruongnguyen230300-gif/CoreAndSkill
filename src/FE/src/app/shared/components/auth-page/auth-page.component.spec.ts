import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AuthCardComponent } from '../auth-card/auth-card.component';
import { FooterComponent } from '../footer/footer.component';
import { AuthPageComponent } from './auth-page.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

@Component({
  selector: 'app-thu-auth-page',
  standalone: true,
  imports: [AuthPageComponent, AuthCardComponent, FooterComponent],
  template: `
    <app-auth-page>
      <app-auth-card title="Đăng nhập">
        <div [style.height.px]="noiDung">nội dung</div>
      </app-auth-card>
      <app-footer variant="auth" size="sm" copyright="© 2026 X" />
    </app-auth-page>
  `,
})
class ThuHost {
  noiDung = 40;
}

/**
 * AuthCard.md §Responsive: cột `min-height: 100dvh` bọc `main` và `Footer`. Karma chạy trong khung
 * nhìn nhỏ và cố định, nên component được dựng trong một iframe cùng nguồn có kích thước tuỳ ý
 * (chép style của trang vào) — `dvh` trong iframe là chiều cao của chính iframe. Karma không nạp
 * style toàn cục (token) nên ca thiếu chỗ dựng bằng nội dung cao hơn khung, không bằng đệm.
 */
describe('AuthPageComponent — Footer trong khung nhìn', () => {
  let iframe: HTMLIFrameElement;

  function dung(rong: number, cao: number, noiDung = 40) {
    iframe = document.createElement('iframe');
    iframe.style.cssText = `width:${rong}px;height:${cao}px;border:0;position:absolute;top:0;left:0`;
    document.body.appendChild(iframe);
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    });
    const fixture = TestBed.createComponent(ThuHost);
    fixture.componentInstance.noiDung = noiDung;
    fixture.detectChanges();
    const d = iframe.contentDocument as Document;
    // Tệp style toàn cục khai đường dẫn tương đối: iframe about:blank cần `<base>` của trang mẹ.
    const base = d.createElement('base');
    base.href = document.baseURI;
    d.head.appendChild(base);
    document.head
      .querySelectorAll('style, link[rel="stylesheet"]')
      .forEach((n) => d.head.appendChild(n.cloneNode(true)));
    // Token mà phép đo responsive cần, khai cố định: tệp style toàn cục clone qua `<link>` nạp bất đồng bộ
    // nên không chắc đã có mặt trong iframe lúc đo.
    const token = d.createElement('style');
    token.textContent =
      ':root{--sp-6:1rem;--sp-8:2rem;--sp-11:3rem;--border-w:1px;--color-border:#888;--radius-xl:16px}';
    d.head.appendChild(token);
    d.body.style.margin = '0';
    const el = fixture.nativeElement as HTMLElement;
    d.body.appendChild(d.adoptNode(el));
    return {
      d,
      khung: iframe.contentWindow as Window,
      cot: el.querySelector('app-auth-page') as HTMLElement,
      footer: el.querySelector('footer') as HTMLElement,
      main: el.querySelector('main') as HTMLElement,
      khungThe: el.querySelector('.auth-card') as HTMLElement,
    };
  }

  afterEach(() => iframe.remove());

  it('đủ chỗ (1280×900): cột đúng bằng khung nhìn, Footer sát đáy, không thanh cuộn', () => {
    const { d, khung, cot, footer } = dung(1280, 900);
    expect(cot.getBoundingClientRect().height).toBeCloseTo(khung.innerHeight, 0);
    expect(footer.getBoundingClientRect().bottom).toBeCloseTo(khung.innerHeight, 0);
    expect(d.documentElement.scrollHeight).toBeLessThanOrEqual(khung.innerHeight);
  });

  it('thiếu chỗ (1280×300): cột cao theo nội dung, Footer ngay sau khung, trang cuộn, không bị cắt', () => {
    const { d, khung, cot, footer, main } = dung(1280, 300, 600);
    const hCot = cot.getBoundingClientRect().height;
    expect(hCot).toBeGreaterThan(khung.innerHeight);
    expect(d.documentElement.scrollHeight).toBeGreaterThanOrEqual(hCot - 1);
    expect(footer.getBoundingClientRect().top).toBeGreaterThanOrEqual(
      main.getBoundingClientRect().bottom - 1,
    );
    expect(footer.getBoundingClientRect().bottom).toBeCloseTo(hCot, 0);
    expect(khung.getComputedStyle(footer).position).not.toBe('fixed');
  });

  /** Giá trị token đã phân giải thành px, đo bằng phần tử thăm dò trong chính iframe. */
  function px(d: Document, khung: Window, token: string): string {
    const t = d.createElement('div');
    t.style.paddingLeft = `var(${token})`;
    d.body.appendChild(t);
    const v = khung.getComputedStyle(t).paddingLeft;
    t.remove();
    return v;
  }

  describe('responsive của khung (AuthCard.md §Responsive)', () => {
    it('≥ $bp-md (1280): đệm --sp-11, có viền và bo góc, căn giữa dọc', () => {
      const { d, khung, khungThe, main } = dung(1280, 900);
      const cs = khung.getComputedStyle(khungThe);
      expect(cs.paddingLeft).toBe(px(d, khung, '--sp-11'));
      expect(cs.borderTopWidth).toBe('1px');
      expect(cs.borderTopLeftRadius).not.toBe('0px');
      expect(khung.getComputedStyle(main).alignItems).toBe('center');
    });

    it('$bp-xs … $bp-md (700): đệm --sp-8, vẫn có viền và bo góc', () => {
      const { d, khung, khungThe } = dung(700, 900);
      const cs = khung.getComputedStyle(khungThe);
      expect(cs.paddingLeft).toBe(px(d, khung, '--sp-8'));
      expect(cs.borderTopWidth).toBe('1px');
      expect(cs.borderTopLeftRadius).not.toBe('0px');
    });

    it('< $bp-xs (390): trải hết bề rộng, bỏ viền và bo góc, đệm --sp-6, căn trên', () => {
      const { d, khung, khungThe, main } = dung(390, 900);
      const cs = khung.getComputedStyle(khungThe);
      expect(khungThe.getBoundingClientRect().width).toBeCloseTo(390, 0);
      expect(cs.borderTopWidth).toBe('0px');
      expect(cs.borderTopLeftRadius).toBe('0px');
      expect(cs.paddingLeft).toBe(px(d, khung, '--sp-6'));
      expect(khung.getComputedStyle(main).alignItems).toBe('flex-start');
    });
  });
});
