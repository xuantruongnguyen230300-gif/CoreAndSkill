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

import { DatLaiMatKhauForm } from '../../models/nguoi-dung-hop.model';
import { HopDatLaiMatKhauComponent } from './hop-dat-lai-mat-khau.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

describe('HopDatLaiMatKhauComponent — focus về ô sai khi bấm gửi (09-forms-validation.md §5)', () => {
  let fixture: ComponentFixture<HopDatLaiMatKhauComponent>;
  let form: DatLaiMatKhauForm;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HopDatLaiMatKhauComponent],
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

    const fb = new FormBuilder().nonNullable;
    form = fb.group({ tempPassword: fb.control('', [Validators.required]) });
    fixture = TestBed.createComponent(HopDatLaiMatKhauComponent);
    fixture.componentRef.setInput('hienThi', true);
    fixture.componentRef.setInput('tenDangNhap', 'an');
    fixture.componentRef.setInput('form', form);
    fixture.componentRef.setInput('loiMatKhau', null);
    fixture.componentRef.setInput('xungDot', false);
    fixture.componentRef.setInput('loiChung', null);
    fixture.componentRef.setInput('dangGui', false);
    fixture.componentRef.setInput('lanGuiSai', 0);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  async function veLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
  }

  it('gửi ô trống → focus về ô mật khẩu tạm', async () => {
    form.markAllAsTouched();
    fixture.componentRef.setInput('loiMatKhau', 'Bắt buộc');
    fixture.componentRef.setInput('lanGuiSai', 1);
    await veLai();
    expect(document.activeElement?.id).toBe('nddat-mk');
  });

  it('lanGuiSai còn 0 → không tự kéo focus dù form đang sai', async () => {
    form.markAllAsTouched();
    await veLai();
    expect(document.activeElement?.id).not.toBe('nddat-mk');
  });
});
