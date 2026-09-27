import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateLoader, provideTranslateService } from '@ngx-translate/core';

import { App } from './app';
import { CORE_I18N } from './core/config/core-i18n';
import { MultiSourceTranslateLoader } from './core/i18n/multi-source-translate-loader';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
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
});
