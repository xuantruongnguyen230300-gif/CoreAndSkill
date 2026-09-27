import { Type, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslateService,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, Subject, firstValueFrom, from, of, throwError } from 'rxjs';

import { ApiFailure, ApiFailureError, ApiFieldError } from '../../../../core/http/api-result.model';
import { DonVi } from '../models/don-vi.model';
import { DonViService } from '../services/don-vi.service';
import { TaoDonViStore } from './tao-don-vi.store';

const DON_VI: DonVi = {
  id: 'dv-1',
  code: 'ABC',
  name: 'Đơn vị ABC',
  isActive: true,
  createdAt: null,
};

function loiApi(
  code: string,
  fieldErrors: Record<string, readonly ApiFieldError[]> | null = null,
): ApiFailureError {
  const body: ApiFailure = {
    success: false,
    data: null,
    error: { code, type: 'BusinessRule', message: `msg:${code}`, messageParams: null, fieldErrors },
    traceId: 't',
  };
  return new ApiFailureError(body);
}

const BAT_BUOC: readonly ApiFieldError[] = [
  { code: 'CORE.VALIDATION.REQUIRED', messageParams: null },
];

describe('TaoDonViStore', () => {
  let service: jasmine.SpyObj<DonViService>;
  let hop: TaoDonViStore;

  beforeEach(() => {
    service = jasmine.createSpyObj<DonViService>('DonViService', ['tao']);
    const translate = { instant: (khoa: string) => `[${khoa}]` } as unknown as TranslateService;
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        TaoDonViStore,
        { provide: DonViService, useValue: service },
        { provide: TranslateService, useValue: translate },
      ],
    });
    hop = TestBed.inject(TaoDonViStore);
    hop.moHop();
    hop.form.setValue({
      code: 'abc',
      name: 'Đơn vị ABC',
      adminUserName: 'admin',
      adminEmail: 'a@b.vn',
      adminFullName: 'Quản Trị',
      adminTempPassword: 'Tam-Pass-1',
    });
  });

  it('THÀNH CÔNG → hộp ĐÓNG, thanhCong nhận đơn vị vừa tạo, mã được chuẩn hoá chữ hoa khi gửi', () => {
    service.tao.and.returnValue(of(DON_VI));
    const thanhCong = jasmine.createSpy('thanhCong');

    hop.gui(thanhCong);

    expect(hop.hienThi()).withContext('hộp phải đóng sau khi BE trả 200').toBeFalse();
    expect(hop.dangLuu()).toBeFalse();
    expect(thanhCong).toHaveBeenCalledOnceWith(DON_VI);
    expect(service.tao.calls.mostRecent().args[0].code).toBe('ABC');
  });

  it('chống gửi hai lần: gọi gui() lần hai khi lần một chưa xong → chỉ MỘT request', () => {
    service.tao.and.returnValue(new Subject<DonVi>());

    hop.gui(() => undefined);
    hop.gui(() => undefined);

    expect(service.tao).toHaveBeenCalledTimes(1);
  });

  it('THÀNH CÔNG khi request bất đồng bộ → hộp đóng ngay khi next tới', () => {
    const nguon = new Subject<DonVi>();
    service.tao.and.returnValue(nguon);
    hop.gui(() => undefined);
    expect(hop.dangLuu()).toBeTrue();

    nguon.next(DON_VI);
    expect(hop.hienThi()).toBeFalse();
    nguon.complete();
    expect(hop.dangLuu()).toBeFalse();
  });

  it('đang gửi thì dongHop() bị khoá', () => {
    service.tao.and.returnValue(new Subject<DonVi>());
    hop.gui(() => undefined);
    hop.dongHop();
    expect(hop.hienThi()).toBeTrue();
  });

  it('form không hợp lệ → không gọi service, hộp giữ mở', () => {
    hop.form.controls.adminEmail.setValue('abc');
    hop.gui(() => undefined);
    expect(service.tao).not.toHaveBeenCalled();
    expect(hop.hienThi()).toBeTrue();
    // Tín hiệu để hộp đưa focus về ô sai đầu tiên (09-forms-validation.md §5).
    expect(hop.lanGuiSai()).toBe(1);
    hop.gui(() => undefined);
    expect(hop.lanGuiSai()).toBe(2);
  });

  it('gửi hợp lệ → lanGuiSai không tăng', () => {
    service.tao.and.returnValue(of(DON_VI));
    hop.gui(() => undefined);
    expect(hop.lanGuiSai()).toBe(0);
  });

  it('CODE_DUPLICATE → lỗi vào ô mã, không banner', () => {
    service.tao.and.returnValue(throwError(() => loiApi('CORE.TENANT.CODE_DUPLICATE')));
    hop.gui(() => undefined);
    expect(hop.form.controls.code.errors?.['server']).toEqual(['[loi.CORE.TENANT.CODE_DUPLICATE]']);
    expect(hop.loiChung()).toBeNull();
  });

  describe('ADMIN_CREATE_FAILED / VALIDATION.FAILED — lỗi không được biến mất (§4.3)', () => {
    it('ADMIN_CREATE_FAILED với fieldErrors = null → banner chung theo mã', () => {
      service.tao.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.ADMIN_CREATE_FAILED', null)),
      );
      hop.gui(() => undefined);
      expect(hop.loiChung()).toBe('[loi.CORE.TENANT.ADMIN_CREATE_FAILED]');
      expect(hop.hienThi()).toBeTrue();
      expect(hop.dangLuu()).toBeFalse();
    });

    it('ADMIN_CREATE_FAILED với fieldErrors = {} → banner chung theo mã', () => {
      service.tao.and.returnValue(throwError(() => loiApi('CORE.TENANT.ADMIN_CREATE_FAILED', {})));
      hop.gui(() => undefined);
      expect(hop.loiChung()).toBe('[loi.CORE.TENANT.ADMIN_CREATE_FAILED]');
    });

    // fe-ui-conventions.md §6.2: mã gốc không nói thay mã con — lý do cụ thể nằm ở mã con.
    it('fieldErrors toàn khoá lạ → banner mang câu của MÃ CON, không phải của ADMIN_CREATE_FAILED', () => {
      service.tao.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.ADMIN_CREATE_FAILED', { KhoaLa: BAT_BUOC })),
      );
      hop.gui(() => undefined);
      expect(hop.loiChung()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    it('fieldErrors có khoá khớp (AdminTempPassword → adminTempPassword) → lỗi vào ô, KHÔNG banner', () => {
      service.tao.and.returnValue(
        throwError(() =>
          loiApi('CORE.TENANT.ADMIN_CREATE_FAILED', { AdminTempPassword: BAT_BUOC }),
        ),
      );
      hop.gui(() => undefined);
      expect(hop.loiChung()).toBeNull();
      expect(hop.form.controls.adminTempPassword.errors?.['server']).toEqual([
        '[loi.CORE.VALIDATION.REQUIRED]',
      ]);
    });

    it('lẫn khoá khớp và khoá lạ → ô nhận lỗi VÀ banner mang câu của mã ở khoá lạ, không lặp mã đã hiện ở ô', () => {
      service.tao.and.returnValue(
        throwError(() =>
          loiApi('CORE.TENANT.ADMIN_CREATE_FAILED', {
            Name: BAT_BUOC,
            KhoaLa: [{ code: 'CORE.USER.PASSWORD_TOO_SHORT', messageParams: null }],
          }),
        ),
      );
      hop.gui(() => undefined);
      expect(hop.form.controls.name.errors?.['server']).toEqual(['[loi.CORE.VALIDATION.REQUIRED]']);
      expect(hop.loiChung()).toBe('[loi.CORE.USER.PASSWORD_TOO_SHORT]');
    });

    it('SEED_FAILED → banner chung, hộp và nội dung giữ nguyên', () => {
      service.tao.and.returnValue(throwError(() => loiApi('CORE.TENANT.SEED_FAILED')));
      hop.gui(() => undefined);
      expect(hop.loiChung()).toBe('[loi.CORE.TENANT.SEED_FAILED]');
      expect(hop.form.controls.name.value).toBe('Đơn vị ABC');
    });

    // fe-ui-conventions.md §6.1/§6.2: mọi mã đi qua `applyFormFailure`, không lọc theo danh sách mã.
    it('mã NGOÀI danh sách cũ kèm fieldErrors khoá khớp → lỗi vào ô, KHÔNG banner mang câu mã gốc', () => {
      service.tao.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.SEED_FAILED', { Name: BAT_BUOC })),
      );
      hop.gui(() => undefined);
      expect(hop.form.controls.name.errors?.['server']).toEqual(['[loi.CORE.VALIDATION.REQUIRED]']);
      expect(hop.loiChung()).toBeNull();
    });
  });

  // `loiTruong` là thứ component hộp nhận qua input() — control không phải signal, nên nó phải tự
  // tính lại khi form đổi (giá trị, chạm, lỗi từ server), không đợi ai gọi lại.
  describe('loiTruong — câu lỗi từng ô cho component dumb', () => {
    beforeEach(() => hop.moHop());

    it('ô chưa chạm và chưa bấm gửi → không lỗi', () => {
      expect(hop.loiTruong().code).toBeNull();
    });

    it('ô sai được chạm → hiện câu lỗi client; sửa đúng → hết lỗi (tính lại theo sự kiện của form)', () => {
      hop.form.controls.code.markAsTouched();
      expect(hop.loiTruong().code).toBe('[loi.CORE.CLIENT.VALIDATION_REQUIRED]');

      hop.form.controls.code.setValue('ABC');
      expect(hop.loiTruong().code).toBeNull();
    });

    it('bấm gửi khi form sai → mọi ô sai hiện lỗi dù chưa chạm', () => {
      hop.gui(() => undefined);
      expect(hop.loiTruong().name).toBe('[loi.CORE.CLIENT.VALIDATION_REQUIRED]');
      expect(hop.loiTruong().adminEmail).toBe('[loi.CORE.CLIENT.VALIDATION_REQUIRED]');
    });

    it('lỗi server gắn vào ô → loiTruong mang đúng câu đó', () => {
      hop.form.setValue({
        code: 'ABC',
        name: 'Đơn vị ABC',
        adminUserName: 'admin',
        adminEmail: 'a@b.vn',
        adminFullName: 'Quản Trị',
        adminTempPassword: 'Tam-Pass-1',
      });
      service.tao.and.returnValue(throwError(() => loiApi('CORE.TENANT.CODE_DUPLICATE')));
      hop.gui(() => undefined);
      expect(hop.loiTruong().code).toBe('[loi.CORE.TENANT.CODE_DUPLICATE]');
    });

    it('moHop() lần sau reset: lỗi và cờ đã gửi của lần trước không sống sót', () => {
      hop.gui(() => undefined);
      expect(hop.loiTruong().name).not.toBeNull();

      hop.moHop();
      expect(hop.loiTruong().name).toBeNull();
      expect(hop.daGui()).toBeFalse();
      expect(hop.loiChung()).toBeNull();
    });
  });
});

/** Nạp ĐÚNG `public/i18n/vi.json` — bản dịch giả ở trên in lại khoá nên không thấy câu thật. */
class ViJsonThatLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return from(fetch('/i18n/vi.json').then((r) => r.json() as Promise<TranslationObject>));
  }
}

class BangDichRongLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/**
 * `CORE.TENANT.CODE_DUPLICATE` dưới ô "Mã đơn vị" (Design/Screens/20-don-vi.md, bảng Mã lỗi → chỗ
 * hiện): câu đi qua `dichLoi` — bảng dịch thật cho câu thật; thiếu khoá dịch thì đường lùi là câu BE
 * gửi kèm, không phải khoá dịch thô. Cùng khối: mã con `USERNAME_RESERVED` trong
 * `fieldErrors["AdminUserName"]` của 400 `CORE.VALIDATION.FAILED`.
 */
describe('TaoDonViStore — câu lỗi dưới ô qua bảng dịch thật', () => {
  const TRUNG_MA = new ApiFailureError({
    success: false,
    data: null,
    error: {
      code: 'CORE.TENANT.CODE_DUPLICATE',
      type: 'Conflict',
      message: 'Tenant code ABC already exists.',
      messageParams: null,
      fieldErrors: null,
    },
    traceId: 't',
  });

  async function dung(
    loader: Type<TranslateLoader>,
    loi: ApiFailureError = TRUNG_MA,
  ): Promise<TaoDonViStore> {
    const service = jasmine.createSpyObj<DonViService>('DonViService', ['tao']);
    service.tao.and.returnValue(throwError(() => loi));
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        TaoDonViStore,
        { provide: DonViService, useValue: service },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(loader),
        }),
      ],
    });
    await firstValueFrom(TestBed.inject(TranslateService).use('vi'));
    const hop = TestBed.inject(TaoDonViStore);
    hop.moHop();
    hop.form.setValue({
      code: 'ABC',
      name: 'Đơn vị ABC',
      adminUserName: 'admin',
      adminEmail: 'a@b.vn',
      adminFullName: 'Quản Trị',
      adminTempPassword: 'Tam-Pass-1',
    });
    return hop;
  }

  it('câu dưới ô mã là câu thật của bảng dịch; không banner', async () => {
    const hop = await dung(ViJsonThatLoader);
    hop.gui(() => undefined);
    expect(hop.loiTruong().code).toBe('Mã đơn vị đã tồn tại.');
    expect(hop.loiChung()).toBeNull();
  });

  it('bảng dịch thiếu khoá → câu dưới ô là câu BE gửi kèm, không phải khoá dịch thô', async () => {
    const hop = await dung(BangDichRongLoader);
    hop.gui(() => undefined);
    expect(hop.loiTruong().code).toBe('Tenant code ABC already exists.');
  });

  // Khoá của POST /system/tenants là `AdminUserName` — phải khớp control `adminUserName`, nếu không
  // mã lên banner thay vì nằm dưới ô "Tên đăng nhập quản trị".
  it('VALIDATION.FAILED kèm fieldErrors.AdminUserName USERNAME_RESERVED → câu thật dưới ô tên đăng nhập quản trị, không banner', async () => {
    const hop = await dung(
      ViJsonThatLoader,
      new ApiFailureError({
        success: false,
        data: null,
        error: {
          code: 'CORE.VALIDATION.FAILED',
          type: 'Validation',
          message: 'Validation failed.',
          messageParams: null,
          fieldErrors: {
            AdminUserName: [{ code: 'CORE.USER.USERNAME_RESERVED', messageParams: null }],
          },
        },
        traceId: 't',
      }),
    );
    hop.form.controls.adminUserName.setValue('system');

    hop.gui(() => undefined);

    expect(hop.loiTruong().adminUserName).toBe('Tên đăng nhập này được hệ thống giữ riêng.');
    expect(hop.loiChung()).toBeNull();
  });
});
