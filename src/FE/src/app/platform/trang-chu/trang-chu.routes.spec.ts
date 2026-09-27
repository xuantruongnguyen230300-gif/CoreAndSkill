import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { provideCoreHome } from '../../core/config/core-home';
import { TrangChuPage } from './pages/trang-chu/trang-chu.page';
import { TRANG_CHU_ROUTES } from './trang-chu.routes';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/** Trang chủ của một dự án hạ nguồn — thứ `CORE_HOME` nạp lười thay cho trang chào của Core. */
@Component({ selector: 'app-bang-tong-hop-du-an', template: '<p id="bang-du-an">bang-du-an</p>' })
class BangTongHopDuAnComponent {}

/**
 * fe-architecture.md §2.5 và ADR-0057: route trang chủ đọc `CORE_HOME` NGAY TRONG `loadComponent`
 * bằng `inject()`, nên phải chạy `loadComponent` qua Router THẬT — không gọi tay hàm đó trong một
 * `runInInjectionContext` tự dựng. Router bỏ hành vi "chạy `loadComponent` trong ngữ cảnh tiêm"
 * ở một lần nâng cấp thì hai ca dưới nổ lỗi NG0203, và không cổng nào khác bắt được ca đó.
 */
describe('TRANG_CHU_ROUTES — seam CORE_HOME đọc trong loadComponent', () => {
  function dung(coKhaiToken: boolean): Promise<RouterTestingHarness> {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter(TRANG_CHU_ROUTES),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
        { provide: AuthService, useValue: { nguoiDung: signal(null) } },
        ...(coKhaiToken ? [provideCoreHome(() => Promise.resolve(BangTongHopDuAnComponent))] : []),
      ],
    });
    return RouterTestingHarness.create();
  }

  it('dự án khai provideCoreHome → route dựng component của dự án, không dựng trang chào của Core', async () => {
    const harness = await dung(true);
    await harness.navigateByUrl('/');
    harness.detectChanges();
    const el = harness.routeNativeElement as HTMLElement;

    expect(el.querySelector('#bang-du-an'))
      .withContext('component của dự án phải được nạp')
      .not.toBeNull();
    expect(el.querySelector('h1')).withContext('trang chào của Core không được dựng').toBeNull();
  });

  it('không khai CORE_HOME → route dựng trang chào của Core (TrangChuPage), không lỗi', async () => {
    const harness = await dung(false);
    const page = await harness.navigateByUrl('/', TrangChuPage);
    harness.detectChanges();

    expect(page).toBeInstanceOf(TrangChuPage);
    expect(harness.routeNativeElement?.querySelector('#bang-du-an')).toBeNull();
  });
});
