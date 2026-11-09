import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, Validators } from '@angular/forms';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { TaoNguoiDungForm, VaiTroChon } from '../../models/nguoi-dung-hop.model';
import { HopTaoNguoiDungComponent } from './hop-tao-nguoi-dung.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const VAI_TRO_TRONG: VaiTroChon = { options: [], loading: false, error: null, selected: [] };

function dungForm(): TaoNguoiDungForm {
  const fb = new FormBuilder().nonNullable;
  return fb.group({
    userName: fb.control('', [Validators.required]),
    email: fb.control('', [Validators.required]),
    fullName: fb.control('', [Validators.required]),
    tempPassword: fb.control('', [Validators.required]),
    roleIds: fb.control<readonly string[]>([]),
  });
}

describe('HopTaoNguoiDungComponent — focus về ô sai đầu tiên khi bấm gửi (09-forms-validation.md §5)', () => {
  let fixture: ComponentFixture<HopTaoNguoiDungComponent>;
  let form: TaoNguoiDungForm;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HopTaoNguoiDungComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();

    form = dungForm();
    fixture = TestBed.createComponent(HopTaoNguoiDungComponent);
    fixture.componentRef.setInput('hienThi', true);
    fixture.componentRef.setInput('form', form);
    fixture.componentRef.setInput('loiTruong', {
      userName: null,
      email: null,
      fullName: null,
      tempPassword: null,
      roleIds: null,
    });
    fixture.componentRef.setInput('loiChung', null);
    fixture.componentRef.setInput('dangLuu', false);
    fixture.componentRef.setInput('coQuyenGanVaiTro', false);
    fixture.componentRef.setInput('vaiTro', VAI_TRO_TRONG);
    fixture.componentRef.setInput('lanGuiSai', 0);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  async function veLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
  }

  /**
   * Chờ điều kiện đúng. PrimeNG 21 thoát phần tử bằng hoạt ảnh CSS thật (lớp `p-leave-to`, gỡ khỏi
   * DOM khi `transitionend`), không còn gỡ đồng bộ như bản 20 — nên không thể đo ngay sau `onDinh`.
   */
  async function choDen(dieuKien: () => boolean, toiDaMs = 2000): Promise<void> {
    const batDau = Date.now();
    while (!dieuKien() && Date.now() - batDau < toiDaMs) {
      await new Promise((xong) => setTimeout(xong, 20));
      fixture.detectChanges();
    }
  }

  async function bamGuiSai(lan: number): Promise<void> {
    form.markAllAsTouched();
    fixture.componentRef.setInput('loiTruong', {
      userName: 'Bắt buộc',
      email: 'Bắt buộc',
      fullName: 'Bắt buộc',
      tempPassword: 'Bắt buộc',
      roleIds: null,
    });
    fixture.componentRef.setInput('lanGuiSai', lan);
    await veLai();
  }

  it('gửi form trống → focus về ô ĐẦU TIÊN (Tên đăng nhập)', async () => {
    await bamGuiSai(1);
    expect(document.activeElement?.id).toBe('nd-tao-ten');
  });

  it('chỉ ô sau sai → focus về ô sai đầu tiên còn lại (Email)', async () => {
    form.patchValue({ userName: 'an.nguyen' });
    await bamGuiSai(1);
    expect(document.activeElement?.id).toBe('nd-tao-email');
  });

  it('hộp mở → focus ở ô đầu; rời ô khi chưa gõ → control không touched, không aria-invalid (§6.5)', async () => {
    await veLai();
    const o = document.querySelector<HTMLInputElement>('input#nd-tao-ten')!;
    // Focus vào ô đầu đặt ở `onShow` — cuối hoạt ảnh vào, không còn đồng bộ với lần vẽ đầu.
    await choDen(() => document.activeElement === o);
    expect(document.activeElement).toBe(o);
    o.dispatchEvent(new Event('blur'));
    await veLai();
    expect(form.controls['userName'].touched).toBeFalse();
    expect(o.hasAttribute('aria-invalid')).toBeFalse();
  });

  it('lanGuiSai còn 0 → không tự kéo focus dù form đang sai', async () => {
    // Hộp vừa mở đã đặt focus vào ô đầu (Dialog.md, *Focus lúc mở*); bỏ focus đó để thấy lanGuiSai không tự kéo.
    (document.activeElement as HTMLElement | null)?.blur();
    form.markAllAsTouched();
    await veLai();
    expect(document.activeElement?.id).not.toBe('nd-tao-ten');
  });
});

describe('HopTaoNguoiDungComponent — lỗi của ô "Vai trò" (users.md §5 trần 50 phần tử; 10-nguoi-dung.md: dòng lỗi dưới ô)', () => {
  it('loiTruong().roleIds có câu → FormRow "Vai trò" hiện đúng câu đó dưới ô (role=alert)', async () => {
    await TestBed.configureTestingModule({
      imports: [HopTaoNguoiDungComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(HopTaoNguoiDungComponent);
    fixture.componentRef.setInput('hienThi', true);
    fixture.componentRef.setInput('form', dungForm());
    fixture.componentRef.setInput('loiTruong', {
      userName: null,
      email: null,
      fullName: null,
      tempPassword: null,
      roleIds: 'Tối đa 50 phần tử.',
    });
    fixture.componentRef.setInput('loiChung', null);
    fixture.componentRef.setInput('dangLuu', false);
    fixture.componentRef.setInput('coQuyenGanVaiTro', true);
    fixture.componentRef.setInput('vaiTro', VAI_TRO_TRONG);
    fixture.componentRef.setInput('lanGuiSai', 0);
    fixture.detectChanges();
    await fixture.whenStable();

    const loi = document.getElementById('nd-tao-vai-tro-error');
    expect(loi?.textContent).toContain('Tối đa 50 phần tử.');
    expect(loi?.getAttribute('role')).toBe('alert');
  });
});

/**
 * Design/Screens/10-nguoi-dung.md, bảng mã lỗi dòng "tìm vai trò (ô chọn)": lớp nổi hiện ĐÚNG câu
 * `TraCuuVaiTroStore` đã chọn cho lỗi đó (dịch theo mã; lớp xuyên suốt → `vaiTro.tim.loi`) — không
 * phải một câu mất kết nối cố định cho mọi lỗi.
 */
describe('HopTaoNguoiDungComponent — lớp nổi ô "Vai trò" hiện câu lỗi của lần tìm', () => {
  it('vaiTro().error mang câu → errorTemplate hiện đúng câu đó', async () => {
    await TestBed.configureTestingModule({
      imports: [HopTaoNguoiDungComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(HopTaoNguoiDungComponent);
    fixture.componentRef.setInput('hienThi', true);
    fixture.componentRef.setInput('form', dungForm());
    fixture.componentRef.setInput('loiTruong', {
      userName: null,
      email: null,
      fullName: null,
      tempPassword: null,
      roleIds: null,
    });
    fixture.componentRef.setInput('loiChung', null);
    fixture.componentRef.setInput('dangLuu', false);
    fixture.componentRef.setInput('coQuyenGanVaiTro', true);
    fixture.componentRef.setInput('vaiTro', { ...VAI_TRO_TRONG, error: 'Câu lỗi của lần tìm.' });
    fixture.componentRef.setInput('lanGuiSai', 0);
    fixture.detectChanges();
    await fixture.whenStable();

    const loi = document.querySelector('.hop-tao-nguoi-dung__loi-tim');
    expect(loi?.textContent?.trim()).toBe('Câu lỗi của lần tìm.');
  });
});
