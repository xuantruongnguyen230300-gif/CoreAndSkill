import { provideZonelessChangeDetection } from '@angular/core';
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
import { KhoiPhucTaoQuanTriStore } from './khoi-phuc-tao-quan-tri.store';

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

describe('KhoiPhucTaoQuanTriStore', () => {
  let service: jasmine.SpyObj<DonViService>;
  let hop: KhoiPhucTaoQuanTriStore;
  let khiKhongTonTai: jasmine.Spy<() => void>;

  beforeEach(() => {
    service = jasmine.createSpyObj<DonViService>('DonViService', [
      'khoiPhucQuanTri',
      'taoQuanTriMoi',
    ]);
    // Bản dịch giả: câu = `[khoá]` — đủ để phân biệt câu nào được chọn, không cần nạp vi.json.
    const translate = { instant: (khoa: string) => `[${khoa}]` } as unknown as TranslateService;
    khiKhongTonTai = jasmine.createSpy('khiDonViKhongTonTai');
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        KhoiPhucTaoQuanTriStore,
        { provide: DonViService, useValue: service },
        { provide: TranslateService, useValue: translate },
      ],
    });
    hop = TestBed.inject(KhoiPhucTaoQuanTriStore);
  });

  function dienKhoiPhuc(): void {
    hop.moKhoiPhuc(DON_VI);
    hop.formKhoiPhuc.setValue({ userName: 'admin', tempPassword: 'Tam-Pass-1', goLaiMa: 'abc' });
  }

  function dienTaoQuanTri(): void {
    hop.moTaoQuanTri(DON_VI);
    hop.formTaoQuanTri.setValue({
      userName: 'admin2',
      email: 'a@b.vn',
      fullName: 'Quản Trị',
      tempPassword: 'Tam-Pass-1',
      goLaiMa: 'ABC',
    });
  }

  describe('chống gửi hai lần: gọi lần hai khi lần một chưa xong → chỉ MỘT request', () => {
    it('guiKhoiPhuc', () => {
      service.khoiPhucQuanTri.and.returnValue(new Subject<void>());
      dienKhoiPhuc();

      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);

      expect(service.khoiPhucQuanTri).toHaveBeenCalledTimes(1);
    });

    it('guiTaoQuanTri', () => {
      service.taoQuanTriMoi.and.returnValue(new Subject<void>());
      dienTaoQuanTri();

      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);
      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);

      expect(service.taoQuanTriMoi).toHaveBeenCalledTimes(1);
    });
  });

  describe('khôi phục quản trị', () => {
    it('THÀNH CÔNG → hộp ĐÓNG, gọi thanhCong đúng một lần, cờ dangGui hạ', () => {
      service.khoiPhucQuanTri.and.returnValue(of(undefined));
      const thanhCong = jasmine.createSpy('thanhCong');
      dienKhoiPhuc();

      hop.guiKhoiPhuc(thanhCong, khiKhongTonTai);

      expect(hop.dangMo()).withContext('hộp phải đóng sau khi BE trả 200').toBeNull();
      expect(hop.donViDangThaoTac()).toBeNull();
      expect(hop.dangGui()).toBeFalse();
      expect(hop.loiChung()).toBeNull();
      expect(thanhCong).toHaveBeenCalledTimes(1);
    });

    it('THÀNH CÔNG khi request bất đồng bộ → hộp đóng ngay khi next tới (không chờ finalize)', () => {
      const nguon = new Subject<void>();
      service.khoiPhucQuanTri.and.returnValue(nguon);
      dienKhoiPhuc();
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(hop.dangGui()).toBeTrue();

      nguon.next();
      expect(hop.dangMo()).toBeNull();
      nguon.complete();
      expect(hop.dangGui()).toBeFalse();
    });

    it('đang gửi thì dongHop() từ ngoài bị khoá (không đóng giữa chừng)', () => {
      service.khoiPhucQuanTri.and.returnValue(new Subject<void>());
      dienKhoiPhuc();
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);

      hop.dongHop();

      expect(hop.dangMo()).toBe('khoi-phuc');
    });

    it('form không hợp lệ → không gọi service', () => {
      hop.moKhoiPhuc(DON_VI);
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(service.khoiPhucQuanTri).not.toHaveBeenCalled();
      expect(hop.dangMo()).toBe('khoi-phuc');
    });

    it('bấm gửi khi form không hợp lệ → lanGuiSai tăng mỗi lần (để hộp đưa focus về ô sai); gửi hợp lệ thì không tăng', () => {
      hop.moKhoiPhuc(DON_VI);
      expect(hop.lanGuiSai()).toBe(0);
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(hop.lanGuiSai()).toBe(2);

      service.khoiPhucQuanTri.and.returnValue(of(undefined));
      dienKhoiPhuc();
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(hop.lanGuiSai()).toBe(2);
    });

    it('gõ sai mã đơn vị → không gọi service', () => {
      dienKhoiPhuc();
      hop.formKhoiPhuc.controls.goLaiMa.setValue('KHAC');
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(service.khoiPhucQuanTri).not.toHaveBeenCalled();
    });
  });

  describe('tạo quản trị mới', () => {
    it('THÀNH CÔNG → hộp ĐÓNG, thanhCong nhận đúng tên đăng nhập', () => {
      service.taoQuanTriMoi.and.returnValue(of(undefined));
      const thanhCong = jasmine.createSpy('thanhCong');
      dienTaoQuanTri();

      hop.guiTaoQuanTri(thanhCong, khiKhongTonTai);

      expect(hop.dangMo()).withContext('hộp phải đóng sau khi BE trả 200').toBeNull();
      expect(hop.dangGui()).toBeFalse();
      expect(thanhCong).toHaveBeenCalledOnceWith('admin2');
    });

    it('form không hợp lệ → không gọi service (email sai định dạng), lanGuiSai tăng', () => {
      dienTaoQuanTri();
      hop.formTaoQuanTri.controls.email.setValue('abc');
      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);
      expect(service.taoQuanTriMoi).not.toHaveBeenCalled();
      expect(hop.lanGuiSai()).toBe(1);
    });
  });

  // §4.3 09-forms-validation.md — lỗi KHÔNG được biến mất. Mã ADMIN_CREATE_FAILED gộp mọi ca thất
  // bại và thường KHÔNG kèm fieldErrors (contracts/tenants.md §6).
  describe('lỗi tạo quản trị — không ô nào nhận lỗi thì banner chung PHẢI hiện', () => {
    beforeEach(() => dienTaoQuanTri());

    it('fieldErrors = null → banner chung theo mã, hộp giữ mở', () => {
      service.taoQuanTriMoi.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.ADMIN_CREATE_FAILED', null)),
      );
      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBe('[loi.CORE.TENANT.ADMIN_CREATE_FAILED]');
      expect(hop.dangMo()).toBe('tao-quan-tri');
      expect(hop.dangGui()).toBeFalse();
    });

    it('fieldErrors = {} → banner chung theo mã', () => {
      service.taoQuanTriMoi.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.ADMIN_CREATE_FAILED', {})),
      );
      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBe('[loi.CORE.TENANT.ADMIN_CREATE_FAILED]');
    });

    // fe-ui-conventions.md §6.2: mã gốc không nói thay mã con — lý do cụ thể nằm ở mã con.
    it('fieldErrors toàn khoá lạ → banner mang câu của MÃ CON, không phải của ADMIN_CREATE_FAILED', () => {
      service.taoQuanTriMoi.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.ADMIN_CREATE_FAILED', { KhoaLa: BAT_BUOC })),
      );
      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    it('fieldErrors có khoá khớp → lỗi vào đúng ô, KHÔNG banner chung', () => {
      service.taoQuanTriMoi.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.ADMIN_CREATE_FAILED', { TempPassword: BAT_BUOC })),
      );
      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBeNull();
      expect(hop.formTaoQuanTri.controls.tempPassword.errors?.['server']).toEqual([
        '[loi.CORE.VALIDATION.REQUIRED]',
      ]);
    });

    it('lẫn khoá khớp và khoá lạ → ô nhận lỗi VÀ banner mang câu của mã ở khoá lạ, không lặp mã đã hiện ở ô', () => {
      service.taoQuanTriMoi.and.returnValue(
        throwError(() =>
          loiApi('CORE.TENANT.ADMIN_CREATE_FAILED', {
            TempPassword: BAT_BUOC,
            KhoaLa: [{ code: 'CORE.USER.PASSWORD_TOO_SHORT', messageParams: null }],
          }),
        ),
      );
      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);
      expect(hop.formTaoQuanTri.controls.tempPassword.errors?.['server']).toEqual([
        '[loi.CORE.VALIDATION.REQUIRED]',
      ]);
      expect(hop.loiChung()).toBe('[loi.CORE.USER.PASSWORD_TOO_SHORT]');
    });

    it('lỗi không phải ApiFailureError (mất mạng) → banner chung câu mất kết nối', () => {
      service.taoQuanTriMoi.and.returnValue(throwError(() => new Error('boom')));
      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBe('[loi.CORE.CLIENT.NO_CONNECTION]');
    });

    it('mã khác (TENANT.NOT_FOUND) → banner chung theo mã', () => {
      service.taoQuanTriMoi.and.returnValue(throwError(() => loiApi('CORE.TENANT.NOT_FOUND')));
      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBe('[loi.CORE.TENANT.NOT_FOUND]');
    });
  });

  describe('lỗi khôi phục quản trị', () => {
    beforeEach(() => dienKhoiPhuc());

    it('RECOVERY_RESET_FAILED với fieldErrors = null → banner chung, hộp giữ mở', () => {
      service.khoiPhucQuanTri.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.RECOVERY_RESET_FAILED', null)),
      );
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBe('[loi.CORE.TENANT.RECOVERY_RESET_FAILED]');
      expect(hop.dangMo()).toBe('khoi-phuc');
    });

    it('RECOVERY_RESET_FAILED với fieldErrors = {} → banner chung', () => {
      service.khoiPhucQuanTri.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.RECOVERY_RESET_FAILED', {})),
      );
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBe('[loi.CORE.TENANT.RECOVERY_RESET_FAILED]');
    });

    it('fieldErrors toàn khoá lạ → banner mang câu của MÃ CON (§6.2)', () => {
      service.khoiPhucQuanTri.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.RECOVERY_RESET_FAILED', { KhoaLa: BAT_BUOC })),
      );
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    it('fieldErrors có khoá khớp → lỗi vào ô, KHÔNG banner', () => {
      service.khoiPhucQuanTri.and.returnValue(
        throwError(() => loiApi('CORE.VALIDATION.FAILED', { TempPassword: BAT_BUOC })),
      );
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBeNull();
      expect(hop.formKhoiPhuc.controls.tempPassword.errors?.['server']).toBeDefined();
    });
  });

  // Screen 20, bảng "Mã lỗi → chỗ hiện": NOT_FOUND → banner trong hộp đang mở VÀ tải lại danh sách
  // (đơn vị đã bị người khác xoá thì hàng đó không được nằm lại trong lưới).
  describe('CORE.TENANT.NOT_FOUND → banner trong hộp VÀ tải lại danh sách', () => {
    it('khôi phục: banner theo mã, hộp giữ mở, gọi tải lại đúng một lần', () => {
      dienKhoiPhuc();
      service.khoiPhucQuanTri.and.returnValue(throwError(() => loiApi('CORE.TENANT.NOT_FOUND')));
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBe('[loi.CORE.TENANT.NOT_FOUND]');
      expect(hop.dangMo()).toBe('khoi-phuc');
      expect(khiKhongTonTai).toHaveBeenCalledTimes(1);
    });

    it('tạo quản trị: banner theo mã, hộp giữ mở, gọi tải lại đúng một lần', () => {
      dienTaoQuanTri();
      service.taoQuanTriMoi.and.returnValue(throwError(() => loiApi('CORE.TENANT.NOT_FOUND')));
      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);
      expect(hop.loiChung()).toBe('[loi.CORE.TENANT.NOT_FOUND]');
      expect(hop.dangMo()).toBe('tao-quan-tri');
      expect(khiKhongTonTai).toHaveBeenCalledTimes(1);
    });

    it('mã lỗi khác (RECOVERY_TARGET_NOT_ELIGIBLE) → KHÔNG tải lại danh sách', () => {
      dienKhoiPhuc();
      service.khoiPhucQuanTri.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.RECOVERY_TARGET_NOT_ELIGIBLE')),
      );
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(khiKhongTonTai).not.toHaveBeenCalled();
    });

    it('mất mạng → KHÔNG tải lại danh sách', () => {
      dienKhoiPhuc();
      service.khoiPhucQuanTri.and.returnValue(throwError(() => new Error('boom')));
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(khiKhongTonTai).not.toHaveBeenCalled();
    });
  });

  // `loiTruong` là thứ component hộp nhận qua input() — control không phải signal, nên nó phải tự
  // tính lại khi form đổi (giá trị, chạm, lỗi từ server), không đợi ai gọi lại.
  describe('loiTruong — câu lỗi từng ô cho component dumb', () => {
    it('hộp vừa mở → không ô nào có lỗi', () => {
      hop.moKhoiPhuc(DON_VI);
      expect(hop.loiTruong().khoiPhuc.userName).toBeNull();
      expect(hop.loiTruong().taoQuanTri.email).toBeNull();
    });

    it('bấm gửi form trống → mọi ô của hộp đó hiện lỗi, hộp kia không bị đụng', () => {
      hop.moKhoiPhuc(DON_VI);
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(hop.loiTruong().khoiPhuc.userName).toBe('[loi.CORE.CLIENT.VALIDATION_REQUIRED]');
      expect(hop.loiTruong().khoiPhuc.tempPassword).toBe('[loi.CORE.CLIENT.VALIDATION_REQUIRED]');
    });

    it('gõ sai mã đơn vị → ô "gõ lại mã" hiện lỗi mismatch; gõ đúng (không phân biệt hoa thường) → hết', () => {
      hop.moKhoiPhuc(DON_VI);
      hop.formKhoiPhuc.controls.goLaiMa.setValue('KHAC');
      hop.formKhoiPhuc.controls.goLaiMa.markAsTouched();
      expect(hop.loiTruong().khoiPhuc.goLaiMa).toBe('[loi.CORE.CLIENT.VALIDATION_MISMATCH]');

      hop.formKhoiPhuc.controls.goLaiMa.setValue('abc');
      expect(hop.loiTruong().khoiPhuc.goLaiMa).toBeNull();
    });

    it('lỗi server gắn vào ô của hộp tạo quản trị → loiTruong mang đúng câu đó', () => {
      dienTaoQuanTri();
      service.taoQuanTriMoi.and.returnValue(
        throwError(() => loiApi('CORE.TENANT.ADMIN_CREATE_FAILED', { TempPassword: BAT_BUOC })),
      );
      hop.guiTaoQuanTri(() => undefined, khiKhongTonTai);
      expect(hop.loiTruong().taoQuanTri.tempPassword).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    it('mở lại hộp sau khi đóng → lỗi và cờ đã gửi của lần trước không sống sót', () => {
      hop.moKhoiPhuc(DON_VI);
      hop.guiKhoiPhuc(() => undefined, khiKhongTonTai);
      expect(hop.loiTruong().khoiPhuc.userName).not.toBeNull();
      hop.dongHop();
      expect(hop.dangMo()).toBeNull();

      hop.moKhoiPhuc(DON_VI);
      expect(hop.loiTruong().khoiPhuc.userName).toBeNull();
      expect(hop.daGui()).toBeFalse();
      expect(hop.loiChung()).toBeNull();
    });

    it('xác nhận huỷ đóng hộp; huỷ xác nhận thì hộp giữ mở', () => {
      hop.moTaoQuanTri(DON_VI);
      hop.onDismissAttempted();
      expect(hop.hienThiXacNhanHuy()).toBeTrue();
      hop.huyXacNhanHuy();
      expect(hop.hienThiXacNhanHuy()).toBeFalse();
      expect(hop.dangMo()).toBe('tao-quan-tri');

      hop.onDismissAttempted();
      hop.xacNhanHuy();
      expect(hop.hienThiXacNhanHuy()).toBeFalse();
      expect(hop.dangMo()).toBeNull();
    });
  });
});

/** Nạp ĐÚNG `public/i18n/vi.json` — bản dịch giả ở trên in lại khoá nên không thấy câu thật. */
class ViJsonThatLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return from(fetch('/i18n/vi.json').then((r) => r.json() as Promise<TranslationObject>));
  }
}

/**
 * POST /system/tenants/{id}/admins: tên đăng nhập là danh tính hệ thống → 400
 * `CORE.VALIDATION.FAILED`, mã con `CORE.USER.USERNAME_RESERVED` trong `fieldErrors["UserName"]`,
 * không `messageParams`. Nó xuống ô "Tên đăng nhập" của hộp TẠO QUẢN TRỊ qua `applyFormFailure`.
 */
describe('KhoiPhucTaoQuanTriStore — tên đăng nhập bị giữ riêng qua bảng dịch thật', () => {
  it('VALIDATION.FAILED kèm fieldErrors.UserName USERNAME_RESERVED → câu thật dưới ô Tên đăng nhập của hộp tạo quản trị, không banner', async () => {
    const service = jasmine.createSpyObj<DonViService>('DonViService', [
      'khoiPhucQuanTri',
      'taoQuanTriMoi',
    ]);
    service.taoQuanTriMoi.and.returnValue(
      throwError(
        () =>
          new ApiFailureError({
            success: false,
            data: null,
            error: {
              code: 'CORE.VALIDATION.FAILED',
              type: 'Validation',
              message: 'Validation failed.',
              messageParams: null,
              fieldErrors: {
                UserName: [{ code: 'CORE.USER.USERNAME_RESERVED', messageParams: null }],
              },
            },
            traceId: 't',
          }),
      ),
    );
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        KhoiPhucTaoQuanTriStore,
        { provide: DonViService, useValue: service },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(ViJsonThatLoader),
        }),
      ],
    });
    await firstValueFrom(TestBed.inject(TranslateService).use('vi'));
    const hop = TestBed.inject(KhoiPhucTaoQuanTriStore);
    hop.moTaoQuanTri(DON_VI);
    hop.formTaoQuanTri.setValue({
      userName: 'system',
      email: 'a@b.vn',
      fullName: 'Quản Trị',
      tempPassword: 'Tam-Pass-1',
      goLaiMa: 'ABC',
    });

    hop.guiTaoQuanTri(
      () => undefined,
      () => undefined,
    );

    expect(hop.loiTruong().taoQuanTri.userName).toBe('Tên đăng nhập này được hệ thống giữ riêng.');
    expect(hop.loiChung()).toBeNull();
    expect(hop.dangMo()).toBe('tao-quan-tri');
  });
});
