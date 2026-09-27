import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AuthFieldComponent } from './auth-field.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/**
 * Design/Icons.md §5: tên icon sống ở TypeScript dưới dạng tên trần, template ghép `pi pi-<tên>`.
 * Cặp icon ↔ biến thể lấy đúng bảng §5 "Tài khoản, tệp, hệ thống".
 */
describe('AuthFieldComponent — icon dẫn theo biến thể', () => {
  let fixture: ComponentFixture<AuthFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuthFieldComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AuthFieldComponent);
    fixture.componentRef.setInput('label', 'Nhãn');
    fixture.componentRef.setInput('autocomplete', 'username');
  });

  function lopIcon(): string[] {
    const i = (fixture.nativeElement as HTMLElement).querySelector('.auth-field__icon');
    return Array.from(i?.classList ?? []);
  }

  for (const [variant, lop] of [
    ['identifier', 'pi-user'],
    ['password', 'pi-key'],
    ['tenantCode', 'pi-building'],
  ] as const) {
    it(`variant=${variant} → lớp pi ${lop}, không nhân đôi tiền tố`, () => {
      fixture.componentRef.setInput('variant', variant);
      fixture.detectChanges();

      const lops = lopIcon();
      expect(lops).toContain('pi');
      expect(lops).toContain(lop);
      expect(lops.filter((c) => c.startsWith('pi-pi-'))).toEqual([]);
      expect(lops).not.toContain(lop.slice('pi-'.length));
    });
  }

  it('variant=code không có icon dẫn — không vẽ thẻ <i> nào', () => {
    fixture.componentRef.setInput('variant', 'code');
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.auth-field__icon')).toBeNull();
  });
});
