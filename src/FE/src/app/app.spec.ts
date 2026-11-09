import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideTranslateLoader, provideTranslateService } from '@ngx-translate/core';

import { App } from './app';
import { CORE_I18N } from './core/config/core-i18n';
import { LoiTaiPhanMaService } from './core/navigation/loi-tai-phan-ma';
import { MultiSourceTranslateLoader } from './core/i18n/multi-source-translate-loader';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideLocationMocks(),
        // `<app-unsaved-changes-dialog>` (ADR-0040) dùng TranslatePipe trong template — render nó
        // (detectChanges) buộc TranslateService tạo MultiSourceTranslateLoader, và loader đó cần
        // HttpBackend dù CORE_I18N.sources rỗng (multi-source-translate-loader.ts).
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: CORE_I18N, useValue: { languages: [], defaultLanguage: 'vi', sources: [] } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(MultiSourceTranslateLoader),
        }),
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render a router outlet and the toast host', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-toast')).toBeTruthy();
  });

  describe('khối "Không tải được phần ứng dụng"', () => {
    async function dungKhoiHien(): Promise<HTMLElement> {
      const router = TestBed.inject(Router);
      TestBed.inject(LoiTaiPhanMaService);
      router.resetConfig([
        {
          path: 'hong',
          loadComponent: () =>
            Promise.reject(new TypeError('Failed to fetch dynamically imported module: x.js')),
        },
      ]);
      await router.navigateByUrl('/hong').catch(() => undefined);
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
      await fixture.whenStable();
      return fixture.nativeElement as HTMLElement;
    }

    it('khoiToanTrang bật → đúng một <h1>, một nút, và KHÔNG còn router-outlet', async () => {
      const el = await dungKhoiHien();

      expect(el.querySelectorAll('h1').length).toBe(1);
      expect(el.querySelector('h1')?.textContent).toContain('trangLoi.khongTaiDuoc.tieuDe');
      expect(el.querySelectorAll('main button').length).toBe(1);
      expect(el.querySelector('router-outlet')).toBeNull();
    });

    it('bấm nút → tải lại toàn trang', async () => {
      const taiLai = spyOn(TestBed.inject(LoiTaiPhanMaService), 'taiLaiTrang').and.stub();
      const el = await dungKhoiHien();

      el.querySelector<HTMLButtonElement>('main button')!.click();

      expect(taiLai).toHaveBeenCalledTimes(1);
    });

    it('khoiToanTrang tắt → có router-outlet, không có khối', () => {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
      const el = fixture.nativeElement as HTMLElement;

      expect(el.querySelector('router-outlet')).not.toBeNull();
      expect(el.querySelector('.khoi-loi-tai')).toBeNull();
    });
  });
});
