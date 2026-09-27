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

import { SuaNguoiDungForm } from '../../models/nguoi-dung-hop.model';
import { HopSuaNguoiDungComponent } from './hop-sua-nguoi-dung.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

describe('HopSuaNguoiDungComponent — focus về ô sai đầu tiên khi bấm gửi (09-forms-validation.md §5)', () => {
  let fixture: ComponentFixture<HopSuaNguoiDungComponent>;
  let form: SuaNguoiDungForm;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HopSuaNguoiDungComponent],
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
    form = fb.group({
      email: fb.control('', [Validators.required]),
      fullName: fb.control('', [Validators.required]),
    });
    fixture = TestBed.createComponent(HopSuaNguoiDungComponent);
    fixture.componentRef.setInput('hienThi', true);
    fixture.componentRef.setInput('tenDangNhap', 'an');
    fixture.componentRef.setInput('form', form);
    fixture.componentRef.setInput('loiTruong', { email: null, fullName: null });
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

  async function bamGuiSai(lan: number): Promise<void> {
    form.markAllAsTouched();
    fixture.componentRef.setInput('loiTruong', { email: 'Bắt buộc', fullName: 'Bắt buộc' });
    fixture.componentRef.setInput('lanGuiSai', lan);
    await veLai();
  }

  it('gửi form trống → focus về ô ĐẦU TIÊN có lỗi (Email)', async () => {
    await bamGuiSai(1);
    expect(document.activeElement?.id).toBe('ndsua-email');
  });

  it('chỉ Họ tên sai → focus về Họ tên', async () => {
    form.patchValue({ email: 'an@b.vn' });
    await bamGuiSai(1);
    expect(document.activeElement?.id).toBe('ndsua-ho-ten');
  });

  it('lanGuiSai còn 0 → không tự kéo focus dù form đang sai', async () => {
    form.markAllAsTouched();
    await veLai();
    expect(document.activeElement?.id).not.toBe('ndsua-email');
  });
});
