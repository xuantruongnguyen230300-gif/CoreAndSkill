import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, Validators } from '@angular/forms';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { LoiTruongTaoDonVi, TaoDonViForm } from '../../models/don-vi-hop.model';
import { HopTaoDonViComponent } from './hop-tao-don-vi.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const KHONG_LOI: LoiTruongTaoDonVi = {
  code: null,
  name: null,
  adminUserName: null,
  adminEmail: null,
  adminFullName: null,
  adminTempPassword: null,
};

/** Form dựng ở spec — component dumb nhận form từ ngoài, không cần biết ai dựng nó. */
function dungForm(): TaoDonViForm {
  const fb = new FormBuilder().nonNullable;
  return fb.group({
    code: fb.control('', [Validators.required]),
    name: fb.control('', [Validators.required]),
    adminUserName: fb.control('', [Validators.required]),
    adminEmail: fb.control('', [Validators.required]),
    adminFullName: fb.control('', [Validators.required]),
    adminTempPassword: fb.control('', [Validators.required]),
  });
}

describe('HopTaoDonViComponent — component dumb: chỉ nhận giá trị, chỉ phát output', () => {
  let fixture: ComponentFixture<HopTaoDonViComponent>;
  let form: TaoDonViForm;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HopTaoDonViComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();

    form = dungForm();
    fixture = TestBed.createComponent(HopTaoDonViComponent);
    fixture.componentRef.setInput('hienThi', true);
    fixture.componentRef.setInput('form', form);
    fixture.componentRef.setInput('loiTruong', KHONG_LOI);
    fixture.componentRef.setInput('loiChung', null);
    fixture.componentRef.setInput('dangLuu', false);
    fixture.componentRef.setInput('hienThiXacNhanHuy', false);
    fixture.componentRef.setInput('lanGuiSai', 0);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  async function veLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
  }

  /** Mô phỏng "bấm gửi khi form sai" đúng như store làm: chạm mọi ô rồi tăng nhịp. */
  async function bamGuiSai(lan: number): Promise<void> {
    form.markAllAsTouched();
    // Store tính lại câu lỗi mỗi khi form đổi; ở đây đưa vào một giá trị mới như nó sẽ làm.
    fixture.componentRef.setInput('loiTruong', {
      code: 'Bắt buộc',
      name: 'Bắt buộc',
      adminUserName: 'Bắt buộc',
      adminEmail: 'Bắt buộc',
      adminFullName: 'Bắt buộc',
      adminTempPassword: 'Bắt buộc',
    });
    fixture.componentRef.setInput('lanGuiSai', lan);
    await veLai();
  }

  describe('focus ô sai đầu tiên khi bấm gửi (09-forms-validation.md §5)', () => {
    it('gửi form trống → focus về ô ĐẦU TIÊN (Mã đơn vị)', async () => {
      await bamGuiSai(1);
      expect(document.activeElement?.id).toBe('dv-tao-ma');
    });

    it('chỉ ô sau sai (mã, tên hợp lệ; tên đăng nhập trống) → focus về ô sai đầu tiên còn lại', async () => {
      form.patchValue({ code: 'ABC', name: 'Đơn vị ABC' });
      await bamGuiSai(1);
      expect(document.activeElement?.id).toBe('dv-tao-tk');
    });

    it('lanGuiSai còn 0 → không tự kéo focus dù form đang sai', async () => {
      form.markAllAsTouched();
      await veLai();
      expect(document.activeElement?.id).not.toBe('dv-tao-ma');
    });
  });

  describe('hiển thị giá trị nhận vào', () => {
    it('loiChung có câu → NoticeBanner hiện câu đó; null → không có banner', async () => {
      expect(fixture.nativeElement.querySelector('app-notice-banner')).toBeNull();
      fixture.componentRef.setInput('loiChung', 'Câu lỗi chung');
      await veLai();
      expect(fixture.nativeElement.querySelector('app-notice-banner')?.textContent).toContain(
        'Câu lỗi chung',
      );
    });

    it('loiTruong.code có câu → ô Mã đơn vị hiện đúng câu đó', async () => {
      fixture.componentRef.setInput('loiTruong', { ...KHONG_LOI, code: 'Mã bị trùng' });
      await veLai();
      expect(fixture.nativeElement.textContent).toContain('Mã bị trùng');
    });
  });

  describe('output — component không tự làm gì ngoài việc báo', () => {
    it('submit form → phát submitted', () => {
      const spy = jasmine.createSpy('submitted');
      fixture.componentInstance.submitted.subscribe(spy);
      fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
      expect(spy).toHaveBeenCalledTimes(1);
    });

    it('ô mã mất focus → phát maBlurred, và KHÔNG tự đổi giá trị ô (chuẩn hoá là việc của store)', () => {
      const spy = jasmine.createSpy('maBlurred');
      fixture.componentInstance.maBlurred.subscribe(spy);
      form.controls.code.setValue('abc');
      fixture.nativeElement
        .querySelector('#dv-tao-ma')
        .dispatchEvent(new FocusEvent('focusout', { bubbles: true }));
      expect(spy).toHaveBeenCalledTimes(1);
      expect(form.controls.code.value).toBe('abc');
    });
  });
});
