import localeVi from '@angular/common/locales/vi';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, Subject, of, throwError } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { DoiMatKhauService } from '../../../../core/auth/doi-mat-khau.service';
import { SessionExpiryHandler } from '../../../../core/auth/session-expiry.handler';
import { XsrfTokenStore } from '../../../../core/auth/xsrf-token.store';
import { CORE_I18N } from '../../../../core/config/core-i18n';
import { ApiFailure, ApiFailureError } from '../../../../core/http/api-result.model';
import { errorInterceptor } from '../../../../core/interceptors/error.interceptor';
import { ToastService } from '../../../../core/toast/toast.service';
import { UnsavedChangesService } from '../../../../core/unsaved-changes/unsaved-changes.service';
import { HoSo } from '../../models/ho-so.model';
import { HoSoService } from '../../services/ho-so.service';
import { HoSoPage } from './ho-so.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const HO_SO: HoSo = {
  userName: 'an',
  email: 'an@b.vn',
  hoTen: 'Nguyễn An',
  soDienThoai: null,
  ngonNguUaThich: null,
  coDacQuyen: false,
  version: 'v-1',
};

function loiApi(code: string, fieldErrors: ApiFailure['error']['fieldErrors']): ApiFailureError {
  return new ApiFailureError({
    success: false,
    data: null,
    error: { code, type: 'Validation', message: `msg:${code}`, messageParams: null, fieldErrors },
    traceId: 't',
  });
}

interface PageTest {
  luuThongTin(): void;
  doiMatKhau(): void;
  thuLaiTaiHoSo(): void;
  formMatKhau: FormGroup<{
    currentPassword: FormControl<string>;
    newPassword: FormControl<string>;
    confirmNewPassword: FormControl<string>;
  }>;
  loiLuuThongTin(): string | null;
  loiDoiMatKhau(): string | null;
  xungDotDongThoi(): boolean;
}

/**
 * `HoSoService` và `DoiMatKhauService` đều tắt toast (BO_QUA_TOAST_LOI) nên màn là nơi DUY NHẤT
 * hiện lỗi hai form: 422 mà không có `fieldErrors` dùng được mà màn im lặng thì người dùng chỉ thấy
 * nút quay xong rồi thôi.
 */
describe('HoSoPage — lỗi 422 không gắn được vào ô nào vẫn phải HIỆN', () => {
  let fixture: ComponentFixture<HoSoPage>;
  let service: jasmine.SpyObj<HoSoService>;
  let doiMatKhau: jasmine.SpyObj<DoiMatKhauService>;
  let page: PageTest;

  beforeEach(async () => {
    service = jasmine.createSpyObj<HoSoService>('HoSoService', ['layHoSo', 'capNhatHoSo']);
    service.layHoSo.and.returnValue(of(HO_SO));
    doiMatKhau = jasmine.createSpyObj<DoiMatKhauService>('DoiMatKhauService', ['doiTuNguyen']);
    await TestBed.configureTestingModule({
      imports: [HoSoPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        { provide: HoSoService, useValue: service },
        { provide: DoiMatKhauService, useValue: doiMatKhau },
        { provide: AuthService, useValue: { lamMoiPhien: () => Promise.resolve() } },
        {
          provide: CORE_I18N,
          useValue: {
            languages: [{ code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi }],
            defaultLanguage: 'vi',
            sources: [],
          },
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HoSoPage);
    page = fixture.componentInstance as unknown as PageTest;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  async function veLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
  }

  describe('form thông tin cá nhân', () => {
    for (const fieldErrors of [null, {}]) {
      it(`VALIDATION.FAILED với fieldErrors = ${JSON.stringify(fieldErrors)} → banner mang câu của mã`, async () => {
        service.capNhatHoSo.and.returnValue(
          throwError(() => loiApi('CORE.VALIDATION.FAILED', fieldErrors)),
        );

        page.luuThongTin();
        await veLai();

        expect(page.loiLuuThongTin()).toBe('msg:CORE.VALIDATION.FAILED');
        expect(fixture.nativeElement.textContent).toContain('msg:CORE.VALIDATION.FAILED');
      });
    }

    // fe-ui-conventions.md §6.1/§6.2: mọi mã đi qua `applyFormFailure`, không lọc theo danh sách mã.
    it('mã NGOÀI danh sách cũ kèm fieldErrors khoá lạ → banner mang câu của MÃ CON, không phải mã gốc', async () => {
      service.capNhatHoSo.and.returnValue(
        throwError(() =>
          loiApi('CORE.PROFILE.MA_NGOAI_DANH_SACH', {
            KhoaLa: [{ code: 'CORE.VALIDATION.REQUIRED', messageParams: null }],
          }),
        ),
      );

      page.luuThongTin();
      await veLai();

      expect(page.loiLuuThongTin()).toBe('loi.CORE.VALIDATION.REQUIRED');
    });

    // fe-ui-conventions.md §6.1: tên control trùng tên field của request, nên khoá `fieldErrors` của
    // profile.md §2 xuống đúng ô (Design/Screens/03 §Trạng thái, "kiểm tra dữ liệu"). Kiểm trên dòng
    // lỗi đã vẽ (`<controlId>-error`, FormRow.md) — thứ người dùng thật sự thấy.
    for (const [khoaBE, controlId] of [
      ['FullName', 'ho-so-ho-ten'],
      ['PhoneNumber', 'ho-so-so-dien-thoai'],
      ['PreferredLanguage', 'ho-so-ngon-ngu'],
    ] as const) {
      it(`VALIDATION.FAILED kèm fieldErrors.${khoaBE} → dòng lỗi dưới ô #${controlId}, không banner`, async () => {
        service.capNhatHoSo.and.returnValue(
          throwError(() =>
            loiApi('CORE.VALIDATION.FAILED', {
              [khoaBE]: [{ code: 'CORE.VALIDATION.MAX_LENGTH', messageParams: null }],
            }),
          ),
        );

        page.luuThongTin();
        await veLai();

        const dongLoi = (fixture.nativeElement as HTMLElement).querySelector(`#${controlId}-error`);
        expect(dongLoi?.textContent).toContain('loi.CORE.VALIDATION.MAX_LENGTH');
        expect(page.loiLuuThongTin()).toBeNull();
      });
    }

    // Đổi tên control không được đổi hình dạng request: khoá và cách đổi giá trị vẫn theo profile.md §2.
    it('payload giữ đúng hợp đồng: fullName · phoneNumber · preferredLanguage · version', async () => {
      service.layHoSo.and.returnValue(
        of({
          ...HO_SO,
          hoTen: 'Trần Bình',
          soDienThoai: '0901 234 567',
          ngonNguUaThich: 'vi',
          version: 'v-2',
        }),
      );
      page.thuLaiTaiHoSo();
      await veLai();
      service.capNhatHoSo.and.returnValue(of(HO_SO));

      page.luuThongTin();

      expect(service.capNhatHoSo).toHaveBeenCalledOnceWith({
        fullName: 'Trần Bình',
        phoneNumber: '0901 234 567',
        preferredLanguage: 'vi',
        version: 'v-2',
      });
    });

    it('payload: số điện thoại trống → null; ngôn ngữ "mặc định hệ thống" → null', async () => {
      service.capNhatHoSo.and.returnValue(of(HO_SO));

      page.luuThongTin();

      expect(service.capNhatHoSo).toHaveBeenCalledOnceWith({
        fullName: 'Nguyễn An',
        phoneNumber: null,
        preferredLanguage: null,
        version: 'v-1',
      });
    });

    describe('số điện thoại — kiểm ở client theo khuôn của profile.md §2', () => {
      /** Gõ vào ô thật (`app-input` đặt `id` lên `<input>` bên trong) — không phụ thuộc tên control. */
      function goSoDienThoai(giaTri: string): void {
        const o = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
          'input#ho-so-so-dien-thoai',
        );
        if (!o) throw new Error('Không thấy ô số điện thoại');
        o.value = giaTri;
        o.dispatchEvent(new Event('input'));
      }

      function dongLoiSoDienThoai(): string | undefined {
        return (fixture.nativeElement as HTMLElement).querySelector('#ho-so-so-dien-thoai-error')
          ?.textContent;
      }

      for (const [giaTri, ma] of [
        ['0912 abc 678', 'CORE.CLIENT.VALIDATION_PATTERN'],
        ['012345678901234567890', 'CORE.CLIENT.VALIDATION_MAXLENGTH'],
      ] as const) {
        it(`"${giaTri}" sai khuôn → KHÔNG gửi, dòng lỗi dưới ô mang câu của ${ma}`, async () => {
          service.capNhatHoSo.and.returnValue(of(HO_SO));
          goSoDienThoai(giaTri);

          page.luuThongTin();
          await veLai();

          expect(service.capNhatHoSo).not.toHaveBeenCalled();
          expect(dongLoiSoDienThoai()).toContain(`loi.${ma}`);
        });
      }

      // Chữ số, khoảng trắng, + - ( ); đúng 20 ký tự vẫn hợp lệ (trần tính cả khoảng trắng).
      for (const giaTri of ['+84 (028) 3822-1234', '01234567890123456789']) {
        it(`"${giaTri}" đúng khuôn → gửi, payload mang nguyên giá trị`, async () => {
          service.capNhatHoSo.and.returnValue(of(HO_SO));
          goSoDienThoai(giaTri);

          page.luuThongTin();
          await veLai();

          expect(service.capNhatHoSo).toHaveBeenCalledOnceWith(
            jasmine.objectContaining({ phoneNumber: giaTri }),
          );
          expect(dongLoiSoDienThoai()).toBeUndefined();
        });
      }

      // Rỗng là "không đặt": payload gửi `null` (profile.md §2), nên ô rỗng phải hợp lệ.
      it('xoá trắng số đang có → hợp lệ, payload gửi null', async () => {
        service.layHoSo.and.returnValue(of({ ...HO_SO, soDienThoai: '0901 234 567' }));
        page.thuLaiTaiHoSo();
        await veLai();
        service.capNhatHoSo.and.returnValue(of(HO_SO));
        goSoDienThoai('');

        page.luuThongTin();
        await veLai();

        expect(service.capNhatHoSo).toHaveBeenCalledOnceWith(
          jasmine.objectContaining({ phoneNumber: null }),
        );
        expect(dongLoiSoDienThoai()).toBeUndefined();
      });
    });

    it('CONCURRENCY.CONFLICT → banner xung đột (Tải lại), KHÔNG banner lỗi', async () => {
      service.capNhatHoSo.and.returnValue(
        throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT', null)),
      );

      page.luuThongTin();
      await veLai();

      expect(page.xungDotDongThoi()).toBeTrue();
      expect(page.loiLuuThongTin()).toBeNull();
    });
  });

  describe('form đổi mật khẩu', () => {
    beforeEach(() => {
      page.formMatKhau.setValue({
        currentPassword: 'cu',
        newPassword: 'moi',
        confirmNewPassword: 'moi',
      });
    });

    for (const fieldErrors of [null, {}]) {
      it(`CHANGE_PASSWORD_FAILED với fieldErrors = ${JSON.stringify(fieldErrors)} → banner mang câu của mã`, async () => {
        doiMatKhau.doiTuNguyen.and.returnValue(
          throwError(() => loiApi('CORE.AUTH.CHANGE_PASSWORD_FAILED', fieldErrors)),
        );

        page.doiMatKhau();
        await veLai();

        expect(page.loiDoiMatKhau()).toBe('msg:CORE.AUTH.CHANGE_PASSWORD_FAILED');
        expect(fixture.nativeElement.textContent).toContain('msg:CORE.AUTH.CHANGE_PASSWORD_FAILED');
      });
    }

    // fe-ui-conventions.md §6.2: mã gốc không nói thay mã con.
    it('CHANGE_PASSWORD_FAILED chỉ có khoá lạ → banner mang câu của MÃ CON, không phải của mã gốc', async () => {
      doiMatKhau.doiTuNguyen.and.returnValue(
        throwError(() =>
          loiApi('CORE.AUTH.CHANGE_PASSWORD_FAILED', {
            KhoaLa: [{ code: 'CORE.VALIDATION.REQUIRED', messageParams: null }],
          }),
        ),
      );

      page.doiMatKhau();
      await veLai();

      expect(page.loiDoiMatKhau()).toBe('loi.CORE.VALIDATION.REQUIRED');
      expect(fixture.nativeElement.textContent).toContain('loi.CORE.VALIDATION.REQUIRED');
    });

    // fe-ui-conventions.md §6.1/§6.2: mọi mã đi qua `applyFormFailure`, không lọc theo danh sách mã.
    it('mã NGOÀI danh sách cũ kèm fieldErrors khoá khớp (NewPassword) → lỗi vào ô, KHÔNG banner', async () => {
      doiMatKhau.doiTuNguyen.and.returnValue(
        throwError(() =>
          loiApi('CORE.AUTH.MA_NGOAI_DANH_SACH', {
            NewPassword: [{ code: 'CORE.AUTH.PASSWORD_TOO_SHORT', messageParams: null }],
          }),
        ),
      );

      page.doiMatKhau();
      await veLai();

      expect(page.formMatKhau.controls.newPassword.errors?.['server']).toEqual([
        'loi.CORE.AUTH.PASSWORD_TOO_SHORT',
      ]);
      expect(page.loiDoiMatKhau()).toBeNull();
    });

    // Cùng phép thử §6.1 như form thông tin, với phản hồi mẫu của auth.md §6: PASSWORD_MISMATCH ở
    // khoá CurrentPassword, hai mã chính sách ở NewPassword — mỗi ô hiện mã đầu của nó.
    it('CHANGE_PASSWORD_FAILED theo mẫu auth.md §6 → dòng lỗi dưới ô mật khẩu hiện tại và ô mật khẩu mới, không banner', async () => {
      doiMatKhau.doiTuNguyen.and.returnValue(
        throwError(() =>
          loiApi('CORE.AUTH.CHANGE_PASSWORD_FAILED', {
            CurrentPassword: [{ code: 'CORE.AUTH.PASSWORD_MISMATCH', messageParams: null }],
            NewPassword: [
              { code: 'CORE.AUTH.PASSWORD_TOO_SHORT', messageParams: { MinLength: '8' } },
              { code: 'CORE.AUTH.PASSWORD_REQUIRES_DIGIT', messageParams: null },
            ],
          }),
        ),
      );

      page.doiMatKhau();
      await veLai();

      const goc = fixture.nativeElement as HTMLElement;
      expect(goc.querySelector('#ho-so-mat-khau-hien-tai-error')?.textContent).toContain(
        'loi.CORE.AUTH.PASSWORD_MISMATCH',
      );
      expect(goc.querySelector('#ho-so-mat-khau-moi-error')?.textContent).toContain(
        'loi.CORE.AUTH.PASSWORD_TOO_SHORT',
      );
      expect(page.loiDoiMatKhau()).toBeNull();
    });

    it('payload giữ đúng hợp đồng auth.md §6: currentPassword · newPassword, không gửi ô nhập lại', () => {
      doiMatKhau.doiTuNguyen.and.returnValue(of(undefined));

      page.doiMatKhau();

      expect(doiMatKhau.doiTuNguyen).toHaveBeenCalledOnceWith({
        currentPassword: 'cu',
        newPassword: 'moi',
      });
    });
  });

  // fe-ui-conventions.md §6.3 + Design/Screens/01 "kiểm tra dữ liệu" (03 dùng chung): ô nhập lại phải
  // khớp ô mật khẩu mới, kiểm ở FE. Lỗi `mismatch` nằm trên CHÍNH ô nhập lại — dòng lỗi dưới ô đó
  // mang câu `VALIDATION_MISMATCH`, viền lỗi của ô đó bật. Gõ qua DOM: `Input` đánh dấu touched khi gõ.
  describe('ô nhập lại mật khẩu mới — kiểm khớp ở FE', () => {
    function o(id: string): HTMLInputElement {
      const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
        `input#${id}`,
      );
      if (!input) throw new Error(`Không thấy ô #${id}`);
      return input;
    }

    function go(id: string, giaTri: string): void {
      o(id).value = giaTri;
      o(id).dispatchEvent(new Event('input'));
    }

    function loiNhapLai(): string | undefined {
      return (fixture.nativeElement as HTMLElement)
        .querySelector('#ho-so-mat-khau-nhap-lai-error')
        ?.textContent?.trim();
    }

    it('nhập lại khác mật khẩu mới → lỗi không khớp dưới ô nhập lại, viền lỗi ở ô đó; bấm Đổi không gửi', async () => {
      go('ho-so-mat-khau-hien-tai', 'cu');
      go('ho-so-mat-khau-moi', 'Moi-12345');
      go('ho-so-mat-khau-nhap-lai', 'Moi-54321');
      await veLai();

      expect(loiNhapLai()).toBe('loi.CORE.CLIENT.VALIDATION_MISMATCH');
      expect(o('ho-so-mat-khau-nhap-lai').getAttribute('aria-invalid')).toBe('true');
      expect(o('ho-so-mat-khau-moi').getAttribute('aria-invalid')).toBeNull();

      page.doiMatKhau();
      await veLai();

      expect(doiMatKhau.doiTuNguyen).not.toHaveBeenCalled();
      expect(loiNhapLai()).toBe('loi.CORE.CLIENT.VALIDATION_MISMATCH');
    });

    it('sửa ô mật khẩu mới cho khớp → lỗi không khớp biến mất; bấm Đổi thì gửi', async () => {
      doiMatKhau.doiTuNguyen.and.returnValue(of(undefined));
      go('ho-so-mat-khau-hien-tai', 'cu');
      go('ho-so-mat-khau-moi', 'Moi-12345');
      go('ho-so-mat-khau-nhap-lai', 'Moi-54321');
      await veLai();
      expect(loiNhapLai()).toBe('loi.CORE.CLIENT.VALIDATION_MISMATCH');

      go('ho-so-mat-khau-moi', 'Moi-54321');
      await veLai();
      expect(loiNhapLai()).toBeUndefined();

      page.doiMatKhau();
      expect(doiMatKhau.doiTuNguyen).toHaveBeenCalledOnceWith({
        currentPassword: 'cu',
        newPassword: 'Moi-54321',
      });
    });

    it('ô nhập lại trống → lỗi bắt buộc, không phải lỗi không khớp', async () => {
      go('ho-so-mat-khau-hien-tai', 'cu');
      go('ho-so-mat-khau-moi', 'Moi-12345');

      page.doiMatKhau();
      await veLai();

      expect(loiNhapLai()).toBe('loi.CORE.CLIENT.VALIDATION_REQUIRED');
    });

    // Luật ba nhánh (Screens/01 "kiểm tra dữ liệu"): ô đã chạm mang giá trị không khớp thì hiện lỗi,
    // bất kể người dùng gõ ô nào trước.
    it('gõ ô nhập lại khi ô mật khẩu mới CHƯA từng gõ → lỗi không khớp hiện ngay dưới ô nhập lại', async () => {
      go('ho-so-mat-khau-nhap-lai', 'Moi-54321');
      await veLai();

      expect(loiNhapLai()).toBe('loi.CORE.CLIENT.VALIDATION_MISMATCH');
    });
  });

  // fe-api-client.md §6.1: lượt ĐỌC của màn chết cùng màn — rời màn thì request đang chạy bị huỷ.
  it('rời màn khi GET hồ sơ đang chạy → request bị huỷ, không sống quá màn', () => {
    const nguon = new Subject<HoSo>();
    service.layHoSo.and.returnValue(nguon);
    page.thuLaiTaiHoSo();
    expect(nguon.observed).toBeTrue();

    fixture.destroy();

    expect(nguon.observed).toBeFalse();
  });

  // Design/Screens/03 §Trạng thái, "kiểm tra dữ liệu": bấm gửi khi còn lỗi → focus về ô lỗi đầu tiên
  // CỦA FORM ĐÓ (fe-ui-conventions.md §6.2, `focusOSaiKhiGuiSai`). Màn có hai form: gốc tìm ô sai
  // của mỗi form là chính form đó, nên ô sai của form kia không được giành focus.
  describe('bấm gửi khi còn lỗi → focus về ô sai đầu tiên của chính form đó', () => {
    function o(id: string): HTMLInputElement | null {
      return (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(`input#${id}`);
    }

    function go(id: string, giaTri: string): void {
      const input = o(id);
      if (!input) throw new Error(`Không thấy ô #${id}`);
      input.value = giaTri;
      input.dispatchEvent(new Event('input'));
    }

    function boFocus(): void {
      (document.activeElement as HTMLElement | null)?.blur();
    }

    it('Lưu khi số điện thoại sai khuôn → focus về ô số điện thoại, không gửi', async () => {
      service.capNhatHoSo.and.returnValue(of(HO_SO));
      go('ho-so-so-dien-thoai', '0912 abc 678');
      boFocus();

      page.luuThongTin();
      await veLai();

      expect(service.capNhatHoSo).not.toHaveBeenCalled();
      expect(document.activeElement).toBe(o('ho-so-so-dien-thoai'));
    });

    it('Đổi mật khẩu khi thiếu ô → focus về ô thiếu đầu tiên CỦA FORM MẬT KHẨU, dù form thông tin đang có ô sai phía trên', async () => {
      // Form thông tin để lại một ô sai ĐÃ vẽ `aria-invalid` — đứng trước form mật khẩu trên trang.
      service.capNhatHoSo.and.returnValue(of(HO_SO));
      go('ho-so-so-dien-thoai', '0912 abc 678');
      page.luuThongTin();
      await veLai();
      expect(o('ho-so-so-dien-thoai')?.getAttribute('aria-invalid')).toBe('true');

      doiMatKhau.doiTuNguyen.and.returnValue(of(undefined));
      page.formMatKhau.setValue({ currentPassword: 'cu', newPassword: '', confirmNewPassword: '' });
      boFocus();

      page.doiMatKhau();
      await veLai();

      expect(doiMatKhau.doiTuNguyen).not.toHaveBeenCalled();
      expect(document.activeElement).toBe(o('ho-so-mat-khau-moi'));
    });
  });
});

/**
 * fe-ui-conventions.md §6.2 — hộp xác nhận từ bỏ cờ đặc quyền không có form: banner `danger` trong
 * `ConfirmDialog` (Design/Screens/03 §Trạng thái, "Từ bỏ cờ hỏng") lấy câu từ `fieldErrorsText`, page
 * không tự chọn câu của mã gốc. profile.md §3 không khai `fieldErrors` cho lệnh này, nên ca mã con ở
 * đây ghim LUẬT của khu lỗi chung, không phải một ca hợp đồng; ca không `fieldErrors` là đường đi thật.
 */
describe('HoSoPage — câu của banner lỗi trong hộp từ bỏ cờ đặc quyền (fe-ui-conventions.md §6.2)', () => {
  const CAU_GOC_VALIDATION = 'Kiểm tra lại các trường được đánh dấu.';
  const CAU_REQUIRED = 'Trường này là bắt buộc.';
  const CAU_NO_OTHER_ADMIN = 'Chưa từ bỏ được: đơn vị cần ít nhất một tài khoản khác.';

  class CauLoiLoader extends TranslateLoader {
    getTranslation(): Observable<TranslationObject> {
      return of({
        loi: {
          CORE: {
            VALIDATION: { FAILED: CAU_GOC_VALIDATION, REQUIRED: CAU_REQUIRED },
            PROFILE: { NO_OTHER_PERMISSION_ADMIN: CAU_NO_OTHER_ADMIN },
          },
        },
      });
    }
  }

  interface TuBoTest {
    moHopXacNhanTuBo(): void;
    xacNhanTuBoCoDacQuyen(): void;
  }
  let service: jasmine.SpyObj<HoSoService>;
  let fixture: ComponentFixture<HoSoPage>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<HoSoService>('HoSoService', ['layHoSo', 'tuBoCoDacQuyen']);
    service.layHoSo.and.returnValue(of({ ...HO_SO, coDacQuyen: true }));
    await TestBed.configureTestingModule({
      imports: [HoSoPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        { provide: HoSoService, useValue: service },
        {
          provide: DoiMatKhauService,
          useValue: jasmine.createSpyObj<DoiMatKhauService>('DoiMatKhauService', ['doiTuNguyen']),
        },
        { provide: AuthService, useValue: { lamMoiPhien: () => Promise.resolve() } },
        {
          provide: CORE_I18N,
          useValue: {
            languages: [{ code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi }],
            defaultLanguage: 'vi',
            sources: [],
          },
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(CauLoiLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HoSoPage);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  /** Xác nhận từ bỏ với lỗi cho trước, rồi đọc thân banner trong `ConfirmDialog` — thứ người dùng thấy. */
  async function tuBoHong(loi: ApiFailureError): Promise<string | undefined> {
    service.tuBoCoDacQuyen.and.returnValue(throwError(() => loi));
    const page = fixture.componentInstance as unknown as TuBoTest;
    page.moHopXacNhanTuBo();
    page.xacNhanTuBoCoDacQuyen();
    fixture.detectChanges();
    await fixture.whenStable();
    return (fixture.nativeElement as HTMLElement)
      .querySelector('app-confirm-dialog .notice-banner__than')
      ?.textContent?.trim();
  }

  it('fieldErrors mang mã con → banner trong hộp hiện câu của MÃ CON, không phải câu mã gốc', async () => {
    const cau = await tuBoHong(
      loiApi('CORE.VALIDATION.FAILED', {
        KhoaLa: [{ code: 'CORE.VALIDATION.REQUIRED', messageParams: null }],
      }),
    );

    expect(cau).toBe(CAU_REQUIRED);
    expect(cau).not.toContain(CAU_GOC_VALIDATION);
  });

  it('không kèm fieldErrors (NO_OTHER_PERMISSION_ADMIN) → câu của mã gốc như cũ', async () => {
    expect(await tuBoHong(loiApi('CORE.PROFILE.NO_OTHER_PERMISSION_ADMIN', null))).toBe(
      CAU_NO_OTHER_ADMIN,
    );
  });
});

/**
 * Design/Screens/03 §Trạng thái, "Từ bỏ cờ hỏng": hộp giữ mở; "Riêng `PERMISSION_BYPASS_NOT_HELD`: đóng
 * hộp xong gọi lại `GET` hồ sơ để banner nhắc và khối cờ phản ánh đúng dữ liệu". Cách hiểu hẹp theo
 * chữ: chỉ tải lại khi hộp được đóng lúc lần từ bỏ GẦN NHẤT hỏng với đúng mã đó — đóng hộp không lỗi,
 * hay sau mã khác, thì không tải lại. Đóng bằng nút Huỷ thật của hộp (Escape và backdrop cùng đi qua
 * `cancelled`).
 */
describe('HoSoPage — từ bỏ cờ hỏng với PERMISSION_BYPASS_NOT_HELD: đóng hộp xong gọi lại GET hồ sơ', () => {
  const NOT_HELD = 'CORE.PROFILE.PERMISSION_BYPASS_NOT_HELD';

  let service: jasmine.SpyObj<HoSoService>;
  let fixture: ComponentFixture<HoSoPage>;

  interface TuBoTest {
    moHopXacNhanTuBo(): void;
    xacNhanTuBoCoDacQuyen(): void;
  }

  beforeEach(async () => {
    service = jasmine.createSpyObj<HoSoService>('HoSoService', ['layHoSo', 'tuBoCoDacQuyen']);
    // Lần GET đầu: còn cờ. Mọi lần GET sau: máy chủ nói đã hết cờ.
    service.layHoSo.and.returnValues(of({ ...HO_SO, coDacQuyen: true }), of(HO_SO));
    await TestBed.configureTestingModule({
      imports: [HoSoPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        { provide: HoSoService, useValue: service },
        {
          provide: DoiMatKhauService,
          useValue: jasmine.createSpyObj<DoiMatKhauService>('DoiMatKhauService', ['doiTuNguyen']),
        },
        { provide: AuthService, useValue: { lamMoiPhien: () => Promise.resolve() } },
        {
          provide: CORE_I18N,
          useValue: {
            languages: [{ code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi }],
            defaultLanguage: 'vi',
            sources: [],
          },
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HoSoPage);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  async function veLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function trang(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  /** Mở hộp, bấm xác nhận; mỗi phần tử của `loi` là kết quả của một lần bấm xác nhận. */
  async function tuBoHong(...loi: ApiFailureError[]): Promise<void> {
    const page = fixture.componentInstance as unknown as TuBoTest;
    page.moHopXacNhanTuBo();
    await veLai();
    for (const l of loi) {
      service.tuBoCoDacQuyen.and.returnValue(throwError(() => l));
      page.xacNhanTuBoCoDacQuyen();
      await veLai();
    }
  }

  async function bamHuyTrongHop(): Promise<void> {
    const nutHuy = trang().querySelector<HTMLButtonElement>(
      '.confirm-dialog__hanh-dong app-button button',
    );
    if (!nutHuy) throw new Error('Không thấy nút Huỷ của hộp xác nhận');
    nutHuy.click();
    await veLai();
  }

  it('NOT_HELD → hộp giữ mở, CHƯA tải lại; đóng hộp → gọi lại GET, banner nhắc và Card cờ biến mất', async () => {
    expect(trang().textContent).toContain('hoSo.coDacQuyen.nhac.tieuDe');

    await tuBoHong(loiApi(NOT_HELD, null));

    expect(trang().querySelector('app-confirm-dialog .notice-banner__than')?.textContent).toContain(
      `msg:${NOT_HELD}`,
    );
    expect(service.layHoSo).withContext('hộp còn mở thì chưa tải lại').toHaveBeenCalledTimes(1);

    await bamHuyTrongHop();

    expect(trang().querySelector('[role="alertdialog"]')).withContext('hộp đã đóng').toBeNull();
    expect(service.layHoSo).toHaveBeenCalledTimes(2);
    expect(trang().textContent).not.toContain('hoSo.coDacQuyen.nhac.tieuDe');
    expect(trang().textContent).not.toContain('hoSo.coDacQuyen.tieuDe');
  });

  it('mở hộp rồi Huỷ, không có lỗi → KHÔNG tải lại', async () => {
    await tuBoHong();
    await bamHuyTrongHop();

    expect(service.layHoSo).toHaveBeenCalledTimes(1);
  });

  it('hỏng với mã khác (NO_OTHER_PERMISSION_ADMIN) → đóng hộp → KHÔNG tải lại', async () => {
    await tuBoHong(loiApi('CORE.PROFILE.NO_OTHER_PERMISSION_ADMIN', null));
    await bamHuyTrongHop();

    expect(service.layHoSo).toHaveBeenCalledTimes(1);
  });

  it('NOT_HELD rồi bấm lại gặp mất kết nối → đóng hộp → KHÔNG tải lại (lần thử gần nhất quyết)', async () => {
    await tuBoHong(loiApi(NOT_HELD, null), new ApiFailureError(null));
    await bamHuyTrongHop();

    expect(service.layHoSo).toHaveBeenCalledTimes(1);
  });

  it('đóng sau NOT_HELD đã tải lại một lần; mở lại rồi Huỷ không lỗi → KHÔNG tải lại lần nữa', async () => {
    await tuBoHong(loiApi(NOT_HELD, null));
    await bamHuyTrongHop();
    expect(service.layHoSo).toHaveBeenCalledTimes(2);

    await tuBoHong();
    await bamHuyTrongHop();

    expect(service.layHoSo).toHaveBeenCalledTimes(2);
  });
});

/**
 * Lớp lỗi xuyên suốt (5xx, 403 `CORE.AUTH.*` trừ `PASSWORD_CHANGE_REQUIRED`) qua ĐƯỜNG THẬT:
 * `HoSoService` thật (request mang BO_QUA_TOAST_LOI) → `errorInterceptor` thật → page. Interceptor
 * toast kèm `traceId`; banner của form KHÔNG hiện gì — không hiện hai lần.
 */
describe('HoSoPage — 500 khi lưu thông tin: toast kèm traceId, banner của form không hiện gì', () => {
  let fixture: ComponentFixture<HoSoPage>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HoSoPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: SessionExpiryHandler, useValue: { handle: () => undefined } },
        { provide: XsrfTokenStore, useValue: { token: () => null, lamMoi: () => of('t') } },
        {
          provide: AuthService,
          useValue: { lamMoiPhien: () => Promise.resolve(), lamMoiQuyen: () => undefined },
        },
        {
          provide: DoiMatKhauService,
          useValue: jasmine.createSpyObj<DoiMatKhauService>('DoiMatKhauService', ['doiTuNguyen']),
        },
        {
          provide: CORE_I18N,
          useValue: {
            languages: [{ code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi }],
            defaultLanguage: 'vi',
            sources: [],
          },
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(HoSoPage);
    fixture.detectChanges();
    httpMock.expectOne({ method: 'GET', url: '/core/profile' }).flush({
      success: true,
      data: {
        userName: 'an',
        email: 'an@b.vn',
        fullName: 'Nguyễn An',
        phoneNumber: null,
        preferredLanguage: null,
        hasPermissionBypass: false,
        version: 'v-1',
      },
      error: null,
      traceId: 't-get',
    });
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('PUT hồ sơ trả 500 CORE.SYSTEM.UNEXPECTED → toast mang traceId; banner lỗi lưu trống', async () => {
    const page = fixture.componentInstance as unknown as PageTest;

    page.luuThongTin();
    httpMock.expectOne({ method: 'PUT', url: '/core/profile' }).flush(
      {
        success: false,
        data: null,
        error: {
          code: 'CORE.SYSTEM.UNEXPECTED',
          type: 'Unexpected',
          message: 'msg:CORE.SYSTEM.UNEXPECTED',
          messageParams: null,
          fieldErrors: null,
        },
        traceId: 'trace-500',
      },
      { status: 500, statusText: 'Internal Server Error' },
    );
    fixture.detectChanges();
    await fixture.whenStable();

    const toast = TestBed.inject(ToastService).latest();
    expect(toast?.severity).toBe('error');
    expect(toast?.traceId).toBe('trace-500');
    expect(page.loiLuuThongTin()).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain(
      'msg:CORE.SYSTEM.UNEXPECTED',
    );
  });
});

/**
 * Design/Screens/03 — từ bỏ cờ đổi TẬP QUYỀN, nên phiên (`me`) và menu phải cùng làm mới:
 * - thành công → gọi lại `me` và nạp lại menu (bảng "Thao tác xong");
 * - `PERMISSION_BYPASS_NOT_HELD` → làm mới phiên (`me` + menu) NGAY khi nhận mã, hộp còn mở; `GET`
 *   hồ sơ vẫn chờ đóng hộp (§Trạng thái).
 * Đi đường thật: `AuthService`, `MenuStore`, `HoSoService`, `errorInterceptor` thật; đếm request.
 */
describe('HoSoPage — từ bỏ cờ: làm mới phiên (me) VÀ nạp lại menu', () => {
  const URL_ME = '/core/auth/me';
  const URL_MENU = '/core/meta/menu';
  const URL_TU_BO = '/core/profile/renounce-permission-bypass';
  const PHIEN = {
    id: 'u-1',
    userName: 'an',
    email: 'an@b.vn',
    fullName: 'Nguyễn An',
    roles: [],
    permissions: ['core.permission.write'],
    mustChangePassword: false,
    isSystemOperator: false,
    sessionMinutes: 30,
    preferredLanguage: null,
    tenantCode: 'DV',
    tenantName: 'Đơn vị',
  };
  const HO_SO_DTO = {
    userName: 'an',
    email: 'an@b.vn',
    fullName: 'Nguyễn An',
    phoneNumber: null,
    preferredLanguage: null,
    hasPermissionBypass: true,
    version: 'v-1',
  };

  let fixture: ComponentFixture<HoSoPage>;
  let httpMock: HttpTestingController;

  interface TuBoTest {
    moHopXacNhanTuBo(): void;
    xacNhanTuBoCoDacQuyen(): void;
  }

  function ok(data: unknown): object {
    return { success: true, data, error: null, traceId: 't' };
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HoSoPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: SessionExpiryHandler, useValue: { handle: () => undefined } },
        {
          provide: DoiMatKhauService,
          useValue: jasmine.createSpyObj<DoiMatKhauService>('DoiMatKhauService', ['doiTuNguyen']),
        },
        {
          provide: CORE_I18N,
          useValue: {
            languages: [{ code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi }],
            defaultLanguage: 'vi',
            sources: [],
          },
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
    // Phiên đã đăng nhập — `lamMoiPhien` không gọi `me` khi chưa có phiên.
    TestBed.inject(AuthService).napPhienKhoiDong().subscribe();
    httpMock.expectOne(URL_ME).flush(ok(PHIEN));

    fixture = TestBed.createComponent(HoSoPage);
    fixture.detectChanges();
    httpMock.expectOne({ method: 'GET', url: '/core/profile' }).flush(ok(HO_SO_DTO));
    await veLai();
  });

  afterEach(() => httpMock.verify());

  async function veLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
  }

  async function moHopVaXacNhan(): Promise<void> {
    const page = fixture.componentInstance as unknown as TuBoTest;
    page.moHopXacNhanTuBo();
    await veLai();
    page.xacNhanTuBoCoDacQuyen();
  }

  /** Số request menu đang chờ — trả lời hết để `verify()` không còn request treo. */
  function soLanNapMenu(): number {
    const ds = httpMock.match(URL_MENU);
    ds.forEach((r) => r.flush(ok([])));
    return ds.length;
  }

  async function bamHuyTrongHop(): Promise<void> {
    const nutHuy = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '.confirm-dialog__hanh-dong app-button button',
    );
    if (!nutHuy) throw new Error('Không thấy nút Huỷ của hộp xác nhận');
    nutHuy.click();
    await veLai();
  }

  it('thành công → `me` gọi SAU khi POST xong; `me` về thì menu nạp lại đúng một lần', async () => {
    await moHopVaXacNhan();
    httpMock.expectNone(URL_ME);

    httpMock.expectOne({ method: 'POST', url: URL_TU_BO }).flush(ok(null));
    httpMock.expectOne(URL_ME).flush(ok({ ...PHIEN, permissions: [] }));

    expect(soLanNapMenu()).toBe(1);
    httpMock
      .expectOne({ method: 'GET', url: '/core/profile' })
      .flush(ok({ ...HO_SO_DTO, hasPermissionBypass: false }));
  });

  it('PERMISSION_BYPASS_NOT_HELD → `me` gọi NGAY (hộp còn mở), menu nạp lại một lần; GET hồ sơ chỉ sau khi đóng hộp', async () => {
    await moHopVaXacNhan();
    httpMock.expectOne({ method: 'POST', url: URL_TU_BO }).flush(
      {
        success: false,
        data: null,
        error: {
          code: 'CORE.PROFILE.PERMISSION_BYPASS_NOT_HELD',
          type: 'BusinessRule',
          message: 'msg',
          messageParams: null,
          fieldErrors: null,
        },
        traceId: 't-422',
      },
      { status: 422, statusText: 'Unprocessable Entity' },
    );
    await veLai();

    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alertdialog"]'))
      .withContext('hộp còn mở')
      .not.toBeNull();
    httpMock.expectOne(URL_ME).flush(ok({ ...PHIEN, permissions: [] }));
    expect(soLanNapMenu()).toBe(1);
    httpMock.expectNone({ method: 'GET', url: '/core/profile' });

    await bamHuyTrongHop();

    httpMock
      .expectOne({ method: 'GET', url: '/core/profile' })
      .flush(ok({ ...HO_SO_DTO, hasPermissionBypass: false }));
    httpMock.expectNone(URL_ME);
    expect(soLanNapMenu()).toBe(0);
  });

  it('mở hộp rồi Huỷ, không có lỗi → không `me`, không nạp menu', async () => {
    const page = fixture.componentInstance as unknown as TuBoTest;
    page.moHopXacNhanTuBo();
    await veLai();

    await bamHuyTrongHop();

    httpMock.expectNone(URL_ME);
    expect(soLanNapMenu()).toBe(0);
  });
});

/** Dựng `HoSoPage` với service giả; `layHoSo` trả `nguon`. Dùng cho hai khối dưới. */
async function dungHoSo(nguon: Observable<HoSo>): Promise<{
  fixture: ComponentFixture<HoSoPage>;
  service: jasmine.SpyObj<HoSoService>;
  veLai: () => Promise<void>;
}> {
  const service = jasmine.createSpyObj<HoSoService>('HoSoService', [
    'layHoSo',
    'capNhatHoSo',
    'tuBoCoDacQuyen',
  ]);
  service.layHoSo.and.returnValue(nguon);
  await TestBed.configureTestingModule({
    imports: [HoSoPage],
    providers: [
      provideZonelessChangeDetection(),
      provideNoopAnimations(),
      { provide: HoSoService, useValue: service },
      {
        provide: DoiMatKhauService,
        useValue: jasmine.createSpyObj<DoiMatKhauService>('DoiMatKhauService', ['doiTuNguyen']),
      },
      { provide: AuthService, useValue: { lamMoiPhien: () => Promise.resolve() } },
      {
        provide: CORE_I18N,
        useValue: {
          languages: [{ code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi }],
          defaultLanguage: 'vi',
          sources: [],
        },
      },
      provideTranslateService({
        lang: 'vi',
        fallbackLang: 'vi',
        loader: provideTranslateLoader(FakeTranslateLoader),
      }),
    ],
  }).compileComponents();
  const fixture = TestBed.createComponent(HoSoPage);
  const veLai = async (): Promise<void> => {
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
  };
  await veLai();
  return { fixture, service, veLai };
}

/**
 * Design/Screens/03-ho-so-ca-nhan.md §Trạng thái "lỗi": `GET` hỏng → `NoticeBanner` `danger` + Thử
 * lại; tiêu đề `hoSo.loi.taiThatBai` luôn có; thân là câu `dichLoiChoMan` — lớp lỗi xuyên suốt thì
 * không thân (chi tiết và `traceId` ở toast; không viết "xem thông báo").
 */
describe('HoSoPage — tải hồ sơ hỏng: khung lỗi giữ nguyên, thân theo dichLoiChoMan', () => {
  function loiTai(code: string, status: number): ApiFailureError {
    return new ApiFailureError(
      {
        success: false,
        data: null,
        error: { code, type: 'X', message: `msg:${code}`, messageParams: null, fieldErrors: null },
        traceId: 't',
      },
      status,
    );
  }

  function bannerLoiTai(fixture: ComponentFixture<HoSoPage>): HTMLElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector('app-card .notice-banner--danger');
  }

  it('lớp xuyên suốt (500) → tiêu đề có, KHÔNG thân, nút Thử lại còn', async () => {
    const { fixture } = await dungHoSo(throwError(() => loiTai('CORE.SYSTEM.UNEXPECTED', 500)));

    const banner = bannerLoiTai(fixture);
    expect(banner?.querySelector('.notice-banner__tieu-de')?.textContent?.trim()).toBe(
      'hoSo.loi.taiThatBai',
    );
    const than = banner?.querySelector('.notice-banner__than')?.textContent ?? '';
    expect(than).not.toContain('msg:');
    expect(than).not.toContain('loi.CORE');
    expect(banner?.querySelector('app-button')?.textContent?.trim()).toBe('chung.thuLai');
  });

  it('5xx không envelope (503) → cũng không thân — không phải câu mất kết nối', async () => {
    const { fixture } = await dungHoSo(throwError(() => new ApiFailureError(null, 503)));

    const than =
      bannerLoiTai(fixture)?.querySelector('.notice-banner__than')?.textContent ?? '<không banner>';
    expect(than).not.toContain('loi.CORE.CLIENT.NO_CONNECTION');
    expect(bannerLoiTai(fixture)?.querySelector('.notice-banner__tieu-de')).not.toBeNull();
  });

  it('lỗi ngoài lớp (mất kết nối, status 0) → thân là câu mất kết nối', async () => {
    const { fixture } = await dungHoSo(throwError(() => new ApiFailureError(null, 0)));

    expect(bannerLoiTai(fixture)?.querySelector('.notice-banner__than')?.textContent).toContain(
      'loi.CORE.CLIENT.NO_CONNECTION',
    );
  });
});

/**
 * fe-routing-guard.md §4.1 "Chỉ hỏi khi giá trị THẬT SỰ đổi": gõ rồi sửa về đúng giá trị đã nạp
 * không còn là thay đổi — hỏi "Rời trang?" lúc đó là hỏi thừa. Form mật khẩu: "đổi" khi còn ô không
 * rỗng. Đọc qua `UnsavedChangesService` thật — đúng thứ guard và khung ứng dụng đọc.
 */
describe('HoSoPage — "còn thay đổi chưa lưu" so giá trị, không so cờ dirty', () => {
  let fixture: ComponentFixture<HoSoPage>;
  let veLai: () => Promise<void>;
  let thayDoi: UnsavedChangesService;

  beforeEach(async () => {
    ({ fixture, veLai } = await dungHoSo(of({ ...HO_SO, soDienThoai: '0912' })));
    thayDoi = TestBed.inject(UnsavedChangesService);
  });

  function go(id: string, giaTri: string): void {
    // `app-input` giữ `id` ở CẢ thẻ host lẫn phần tử nhập bên trong — chọn đúng phần tử nhập.
    const el = (fixture.nativeElement as HTMLElement).querySelector<
      HTMLInputElement | HTMLSelectElement
    >(`input#${id}, select#${id}`);
    if (!el) throw new Error(`Không thấy ô #${id}`);
    el.value = giaTri;
    el.dispatchEvent(new Event(el instanceof HTMLSelectElement ? 'change' : 'input'));
  }

  it('điểm xuất phát: vừa nạp xong → không có thay đổi', () => {
    expect(thayDoi.coThayDoiChuaLuu()).toBeFalse();
  });

  it('gõ họ tên khác → có thay đổi; gõ trả về đúng tên đã nạp → KHÔNG còn thay đổi', async () => {
    go('ho-so-ho-ten', 'Tên khác');
    await veLai();
    expect(thayDoi.coThayDoiChuaLuu()).toBeTrue();

    go('ho-so-ho-ten', HO_SO.hoTen);
    await veLai();
    expect(thayDoi.coThayDoiChuaLuu()).toBeFalse();
  });

  it('số điện thoại và ngôn ngữ đổi rồi trả về → KHÔNG còn thay đổi', async () => {
    go('ho-so-so-dien-thoai', '0999');
    go('ho-so-ngon-ngu', 'vi');
    await veLai();
    expect(thayDoi.coThayDoiChuaLuu()).toBeTrue();

    go('ho-so-so-dien-thoai', '0912');
    go('ho-so-ngon-ngu', '__mac-dinh__');
    await veLai();
    expect(thayDoi.coThayDoiChuaLuu()).toBeFalse();
  });

  it('form mật khẩu: gõ một ô → có thay đổi; xoá trắng lại → KHÔNG còn thay đổi', async () => {
    go('ho-so-mat-khau-hien-tai', 'abc');
    await veLai();
    expect(thayDoi.coThayDoiChuaLuu()).toBeTrue();

    go('ho-so-mat-khau-hien-tai', '');
    await veLai();
    expect(thayDoi.coThayDoiChuaLuu()).toBeFalse();
  });
});

/**
 * Design/Screens/03 bảng "Thao tác xong", dòng "Từ bỏ cờ": không có việc nạp lại form thông tin. GET
 * hồ sơ sau khi từ bỏ vẫn chạy — POST trả `data: null` mà ghi vào chính bản ghi tài khoản, nên
 * `version` đang giữ hết hiệu lực (wiki-core/be/06-concurrency-control.md §6.3 luật 4) — nhưng lượt
 * GET đó KHÔNG được đè giá trị đang gõ dở của form thông tin, cũng không dời mốc so sánh của guard.
 */
describe('HoSoPage — từ bỏ cờ khi form thông tin đang gõ dở', () => {
  interface TuBoTest {
    moHopXacNhanTuBo(): void;
    xacNhanTuBoCoDacQuyen(): void;
    dongHopXacNhanTuBo(): void;
    luuThongTin(): void;
    thuLaiTaiHoSo(): void;
  }

  const SO_GO_DO = '0912 345 678';

  let fixture: ComponentFixture<HoSoPage>;
  let service: jasmine.SpyObj<HoSoService>;
  let veLai: () => Promise<void>;
  let thayDoi: UnsavedChangesService;
  let page: TuBoTest;

  beforeEach(async () => {
    ({ fixture, service, veLai } = await dungHoSo(of({ ...HO_SO, coDacQuyen: true })));
    thayDoi = TestBed.inject(UnsavedChangesService);
    page = fixture.componentInstance as unknown as TuBoTest;
  });

  function oSoDienThoai(): HTMLInputElement {
    const o = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      'input#ho-so-so-dien-thoai',
    );
    if (!o) throw new Error('Không thấy ô số điện thoại');
    return o;
  }

  async function goSoDienThoai(giaTri: string): Promise<void> {
    const o = oSoDienThoai();
    o.value = giaTri;
    o.dispatchEvent(new Event('input'));
    await veLai();
  }

  async function tuBoThanhCong(hoSoSauTuBo: HoSo): Promise<void> {
    service.tuBoCoDacQuyen.and.returnValue(of(undefined));
    service.layHoSo.and.returnValue(of(hoSoSauTuBo));
    page.moHopXacNhanTuBo();
    await veLai();
    page.xacNhanTuBoCoDacQuyen();
    await veLai();
  }

  it('từ bỏ thành công → số gõ dở còn nguyên trên ô, guard vẫn thấy thay đổi; Lưu gửi version MỚI', async () => {
    await goSoDienThoai(SO_GO_DO);
    expect(thayDoi.coThayDoiChuaLuu()).toBeTrue();

    await tuBoThanhCong({ ...HO_SO, coDacQuyen: false, version: 'v-2' });

    expect(service.layHoSo).withContext('GET lại để lấy version mới').toHaveBeenCalledTimes(2);
    expect(oSoDienThoai().value).withContext('giá trị gõ dở còn nguyên').toBe(SO_GO_DO);
    expect(thayDoi.coThayDoiChuaLuu()).withContext('guard vẫn thấy thay đổi').toBeTrue();
    expect((fixture.nativeElement as HTMLElement).textContent)
      .withContext('cờ vẫn nạp theo GET: banner nhắc biến mất')
      .not.toContain('hoSo.coDacQuyen.nhac.tieuDe');

    service.capNhatHoSo.and.returnValue(of(HO_SO));
    page.luuThongTin();
    expect(service.capNhatHoSo).toHaveBeenCalledOnceWith({
      fullName: HO_SO.hoTen,
      phoneNumber: SO_GO_DO,
      preferredLanguage: null,
      version: 'v-2',
    });
  });

  it('NOT_HELD → đóng hộp → GET lại: số gõ dở còn nguyên, guard vẫn thấy thay đổi', async () => {
    await goSoDienThoai(SO_GO_DO);
    service.tuBoCoDacQuyen.and.returnValue(
      throwError(() => loiApi('CORE.PROFILE.PERMISSION_BYPASS_NOT_HELD', null)),
    );
    service.layHoSo.and.returnValue(of({ ...HO_SO, coDacQuyen: false, version: 'v-2' }));
    page.moHopXacNhanTuBo();
    await veLai();
    page.xacNhanTuBoCoDacQuyen();
    await veLai();
    page.dongHopXacNhanTuBo();
    await veLai();

    expect(service.layHoSo).toHaveBeenCalledTimes(2);
    expect(oSoDienThoai().value).toBe(SO_GO_DO);
    expect(thayDoi.coThayDoiChuaLuu()).toBeTrue();
  });

  // Design/Screens/03 "Thao tác xong", dòng "Từ bỏ cờ": gỡ banner nhắc và Card cờ NGAY khi POST 200 —
  // không đợi GET kế tiếp, vì GET đó có thể hỏng.
  it('POST từ bỏ 200 rồi GET hồ sơ hỏng → banner nhắc và Card cờ đã gỡ', async () => {
    const goc = fixture.nativeElement as HTMLElement;
    expect(goc.textContent).toContain('hoSo.coDacQuyen.nhac.tieuDe');
    service.tuBoCoDacQuyen.and.returnValue(of(undefined));
    service.layHoSo.and.returnValue(throwError(() => new ApiFailureError(null, 0)));

    page.moHopXacNhanTuBo();
    await veLai();
    page.xacNhanTuBoCoDacQuyen();
    await veLai();

    expect(goc.querySelector('app-card .notice-banner--danger'))
      .withContext('GET hỏng thật')
      .not.toBeNull();
    expect(goc.textContent).not.toContain('hoSo.coDacQuyen.nhac.tieuDe');
    expect(goc.textContent).not.toContain('hoSo.coDacQuyen.tieuDe');
  });

  it('form KHÔNG dở → GET sau từ bỏ nạp giá trị máy chủ vào form như trước', async () => {
    await tuBoThanhCong({ ...HO_SO, soDienThoai: '0909', coDacQuyen: false, version: 'v-2' });

    expect(oSoDienThoai().value).toBe('0909');
    expect(thayDoi.coThayDoiChuaLuu()).toBeFalse();
  });

  it('đang dở mà máy chủ đã đổi một trường sửa được (thao tác khác) → giữ gõ dở, KHÔNG nhận version mới: Lưu để máy chủ trả 409 thay vì ghi đè im lặng', async () => {
    await goSoDienThoai(SO_GO_DO);

    await tuBoThanhCong({
      ...HO_SO,
      hoTen: 'Tên do người khác sửa',
      coDacQuyen: false,
      version: 'v-2',
    });

    expect(oSoDienThoai().value).toBe(SO_GO_DO);
    expect(thayDoi.coThayDoiChuaLuu()).toBeTrue();
    service.capNhatHoSo.and.returnValue(of(HO_SO));
    page.luuThongTin();
    expect(service.capNhatHoSo).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ fullName: HO_SO.hoTen, version: 'v-1' }),
    );
  });

  it('PUT 409 rồi "Tải lại" → form nạp giá trị mới dù đang dở (Screens/03 §Trạng thái, PUT 409)', async () => {
    await goSoDienThoai(SO_GO_DO);
    service.capNhatHoSo.and.returnValue(
      throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT', null)),
    );
    page.luuThongTin();
    await veLai();

    service.layHoSo.and.returnValue(
      of({ ...HO_SO, hoTen: 'Tên mới', coDacQuyen: true, version: 'v-2' }),
    );
    page.thuLaiTaiHoSo();
    await veLai();

    expect(oSoDienThoai().value).toBe('');
    expect(thayDoi.coThayDoiChuaLuu()).toBeFalse();
  });
});
