import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { Router, TitleStrategy, provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, Subject } from 'rxjs';

import { CORE_BRANDING } from '../config/core-branding';
import { CoreTitleStrategy } from './core-title.strategy';

@Component({ selector: 'app-trong', template: '' })
class TrongComponent {}

/** Tệp dịch về MUỘN — mô phỏng mạng chậm: điều hướng đầu tiên xong trước khi bảng dịch có mặt. */
const tepDich = new Subject<TranslationObject>();

class LoaderCham extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return tepDich;
  }
}

describe('CoreTitleStrategy', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([{ path: 'a', title: 'trang.a', component: TrongComponent }]),
        { provide: TitleStrategy, useClass: CoreTitleStrategy },
        { provide: CORE_BRANDING, useValue: { name: 'SP', shortName: 'S' } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(LoaderCham),
        }),
      ],
    });
  });

  it('bảng dịch về SAU điều hướng → tiêu đề vẫn thành câu đã dịch, không kẹt ở khoá thô (V-01)', async () => {
    await TestBed.inject(Router).navigateByUrl('/a');
    const title = TestBed.inject(Title);

    tepDich.next({ trang: { a: 'Trang A' } });
    tepDich.complete();

    expect(title.getTitle()).toBe('Trang A · SP');
  });
});
