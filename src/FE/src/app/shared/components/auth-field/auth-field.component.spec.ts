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

  it('variant=password: ô chừa đệm phải cho nút hiện/ẩn, variant khác thì không', () => {
    fixture.componentRef.setInput('variant', 'password');
    fixture.detectChanges();
    const oNhap = (fixture.nativeElement as HTMLElement).querySelector(
      '.auth-field__input',
    ) as HTMLInputElement;
    const nut = (fixture.nativeElement as HTMLElement).querySelector(
      '.auth-field__nut-hien-an',
    ) as HTMLElement;
    const duongNhap = oNhap.getBoundingClientRect();
    const duongNut = nut.getBoundingClientRect();
    const demPhai = Number.parseFloat(getComputedStyle(oNhap).paddingRight);
    expect(demPhai).toBeGreaterThanOrEqual(duongNhap.right - duongNut.left);

    fixture.componentRef.setInput('variant', 'identifier');
    fixture.detectChanges();
    expect(oNhap.classList).not.toContain('auth-field__input--co-nut');
  });

  it('khi control disabled (đang gửi) → nút hiện/ẩn cũng disabled và bấm không đổi kiểu ô', () => {
    fixture.componentRef.setInput('variant', 'password');
    fixture.detectChanges();
    fixture.componentInstance.setDisabledState(true);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const nut = root.querySelector('.auth-field__nut-hien-an') as HTMLButtonElement;
    const oNhap = root.querySelector('.auth-field__input') as HTMLInputElement;
    expect(nut.disabled).toBeTrue();
    nut.click();
    fixture.detectChanges();
    expect(oNhap.type).toBe('password');
  });

  it('variant=code không có icon dẫn — không vẽ thẻ <i> nào', () => {
    fixture.componentRef.setInput('variant', 'code');
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.auth-field__icon')).toBeNull();
  });

  // AuthField.md "Cỡ nút hiện/ẩn": cỡ lấy từ `--size-control-sm`, không số trần trong SCSS.
  it('nút hiện/ẩn có cỡ đúng token --size-control-sm', () => {
    fixture.componentRef.setInput('variant', 'password');
    fixture.detectChanges();
    const nut = (fixture.nativeElement as HTMLElement).querySelector(
      '.auth-field__nut-hien-an',
    ) as HTMLElement;
    const token = Number.parseFloat(
      getComputedStyle(document.documentElement).getPropertyValue('--size-control-sm'),
    );
    expect(token).toBeGreaterThan(0);
    const r = nut.getBoundingClientRect();
    expect(r.width).toBe(token);
    expect(r.height).toBe(token);
  });
});
