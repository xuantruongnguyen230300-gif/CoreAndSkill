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

import {
  KhoiPhucForm,
  LoaiHopQuanTri,
  LoiTruongQuanTri,
  TaoQuanTriForm,
} from '../../models/don-vi-hop.model';
import { DonVi } from '../../models/don-vi.model';
import { HopQuanTriDonViComponent } from './hop-quan-tri-don-vi.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const DON_VI: DonVi = {
  id: 'dv-1',
  code: 'ABC',
  name: 'Đơn vị ABC',
  isActive: true,
  createdAt: null,
};

const KHONG_LOI: LoiTruongQuanTri = {
  khoiPhuc: { userName: null, tempPassword: null, goLaiMa: null },
  taoQuanTri: { userName: null, email: null, fullName: null, tempPassword: null, goLaiMa: null },
};

describe('HopQuanTriDonViComponent — component dumb: chỉ nhận giá trị, chỉ phát output', () => {
  let fixture: ComponentFixture<HopQuanTriDonViComponent>;
  let formKhoiPhuc: KhoiPhucForm;
  let formTaoQuanTri: TaoQuanTriForm;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HopQuanTriDonViComponent],
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
    formKhoiPhuc = fb.group({
      userName: fb.control('', [Validators.required]),
      tempPassword: fb.control('', [Validators.required]),
      goLaiMa: fb.control('', [Validators.required]),
    });
    formTaoQuanTri = fb.group({
      userName: fb.control('', [Validators.required]),
      email: fb.control('', [Validators.required, Validators.email]),
      fullName: fb.control('', [Validators.required]),
      tempPassword: fb.control('', [Validators.required]),
      goLaiMa: fb.control('', [Validators.required]),
    });

    fixture = TestBed.createComponent(HopQuanTriDonViComponent);
    fixture.componentRef.setInput('dangMo', null);
    fixture.componentRef.setInput('donVi', null);
    fixture.componentRef.setInput('formKhoiPhuc', formKhoiPhuc);
    fixture.componentRef.setInput('formTaoQuanTri', formTaoQuanTri);
    fixture.componentRef.setInput('loiTruong', KHONG_LOI);
    fixture.componentRef.setInput('loiChung', null);
    fixture.componentRef.setInput('dangGui', false);
    fixture.componentRef.setInput('hienThiXacNhanHuy', false);
    fixture.componentRef.setInput('lanGuiSai', 0);
    fixture.detectChanges();
  });

  async function veLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
  }

  async function mo(loai: LoaiHopQuanTri): Promise<void> {
    fixture.componentRef.setInput('donVi', DON_VI);
    fixture.componentRef.setInput('dangMo', loai);
    await veLai();
  }

  /** Mô phỏng "bấm gửi khi form sai" đúng như store làm: chạm mọi ô rồi tăng nhịp. */
  async function bamGuiSai(form: KhoiPhucForm | TaoQuanTriForm, lan: number): Promise<void> {
    form.markAllAsTouched();
    // Store tính lại câu lỗi mỗi khi form đổi; ở đây đưa vào một giá trị mới như nó sẽ làm.
    fixture.componentRef.setInput('loiTruong', {
      khoiPhuc: { userName: 'Bắt buộc', tempPassword: 'Bắt buộc', goLaiMa: 'Bắt buộc' },
      taoQuanTri: {
        userName: 'Bắt buộc',
        email: 'Bắt buộc',
        fullName: 'Bắt buộc',
        tempPassword: 'Bắt buộc',
        goLaiMa: 'Bắt buộc',
      },
    });
    fixture.componentRef.setInput('lanGuiSai', lan);
    await veLai();
  }

  describe('focus ô sai đầu tiên của HỘP ĐANG MỞ (09-forms-validation.md §5)', () => {
    it('hộp Khôi phục: gửi form trống → focus về ô Tên đăng nhập', async () => {
      await mo('khoi-phuc');
      await bamGuiSai(formKhoiPhuc, 1);
      expect(document.activeElement?.id).toBe('dv-kp-ten-dang-nhap');
    });

    it('hộp Tạo quản trị: gửi form trống → focus về ô Tên đăng nhập CỦA HỘP TẠO (không phải hộp khôi phục)', async () => {
      await mo('tao-quan-tri');
      await bamGuiSai(formTaoQuanTri, 1);
      expect(document.activeElement?.id).toBe('dv-tq-ten-dang-nhap');
    });

    it('hộp Tạo quản trị: chỉ email sai → focus về ô Email', async () => {
      await mo('tao-quan-tri');
      formTaoQuanTri.patchValue({ userName: 'admin2', email: 'abc' });
      await bamGuiSai(formTaoQuanTri, 1);
      expect(document.activeElement?.id).toBe('dv-tq-email');
    });
  });

  describe('hiển thị giá trị nhận vào', () => {
    it('loiChung có câu → NoticeBanner hiện câu đó', async () => {
      await mo('tao-quan-tri');
      fixture.componentRef.setInput('loiChung', 'Câu lỗi chung');
      await veLai();
      expect(fixture.nativeElement.textContent).toContain('Câu lỗi chung');
    });

    it('loiTruong.khoiPhuc.userName có câu → hiện ở hộp Khôi phục', async () => {
      await mo('khoi-phuc');
      fixture.componentRef.setInput('loiTruong', {
        ...KHONG_LOI,
        khoiPhuc: { ...KHONG_LOI.khoiPhuc, userName: 'Tên đăng nhập sai' },
      });
      await veLai();
      expect(fixture.nativeElement.textContent).toContain('Tên đăng nhập sai');
    });
  });

  describe('output — component không tự làm gì ngoài việc báo', () => {
    it('submit form của hộp Khôi phục → chỉ phát submittedKhoiPhuc', async () => {
      await mo('khoi-phuc');
      const kp = jasmine.createSpy('submittedKhoiPhuc');
      const tq = jasmine.createSpy('submittedTaoQuanTri');
      fixture.componentInstance.submittedKhoiPhuc.subscribe(kp);
      fixture.componentInstance.submittedTaoQuanTri.subscribe(tq);

      fixture.nativeElement.querySelectorAll('form')[0].dispatchEvent(new Event('submit'));

      expect(kp).toHaveBeenCalledTimes(1);
      expect(tq).not.toHaveBeenCalled();
    });

    it('submit form của hộp Tạo quản trị → chỉ phát submittedTaoQuanTri', async () => {
      await mo('tao-quan-tri');
      const kp = jasmine.createSpy('submittedKhoiPhuc');
      const tq = jasmine.createSpy('submittedTaoQuanTri');
      fixture.componentInstance.submittedKhoiPhuc.subscribe(kp);
      fixture.componentInstance.submittedTaoQuanTri.subscribe(tq);

      fixture.nativeElement.querySelectorAll('form')[0].dispatchEvent(new Event('submit'));

      expect(tq).toHaveBeenCalledTimes(1);
      expect(kp).not.toHaveBeenCalled();
    });
  });
});
