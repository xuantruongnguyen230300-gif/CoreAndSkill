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
import { PagedList } from '../../../../core/http/paged.model';
import { VaiTro } from '../../vai-tro/models/vai-tro.model';
import { VaiTroService } from '../../vai-tro/services/vai-tro.service';
import { NguoiDungService } from '../services/nguoi-dung.service';
import { TaoNguoiDungStore } from './tao-nguoi-dung.store';

function loiApi(
  code: string,
  fieldErrors: Record<string, readonly ApiFieldError[]> | null = null,
  messageParams: Record<string, string> | null = null,
): ApiFailureError {
  const body: ApiFailure = {
    success: false,
    data: null,
    error: { code, type: 'BusinessRule', message: `msg:${code}`, messageParams, fieldErrors },
    traceId: 't',
  };
  return new ApiFailureError(body);
}

const BAT_BUOC: readonly ApiFieldError[] = [
  { code: 'CORE.VALIDATION.REQUIRED', messageParams: null },
];

describe('TaoNguoiDungStore', () => {
  let service: jasmine.SpyObj<NguoiDungService>;
  let vaiTroService: jasmine.SpyObj<VaiTroService>;
  let instant: jasmine.Spy<(khoa: string, tham?: unknown) => string>;
  let store: TaoNguoiDungStore;

  beforeEach(() => {
    service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', ['tao']);
    vaiTroService = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem']);
    instant = jasmine.createSpy('instant').and.callFake((khoa: string) => `[${khoa}]`);
    const translate = { instant } as unknown as TranslateService;
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        TaoNguoiDungStore,
        { provide: NguoiDungService, useValue: service },
        { provide: VaiTroService, useValue: vaiTroService },
        { provide: TranslateService, useValue: translate },
      ],
    });
    store = TestBed.inject(TaoNguoiDungStore);
    store.moHop();
    store.form.setValue({
      userName: 'an.nguyen',
      email: 'an@b.vn',
      fullName: 'Nguyễn An',
      tempPassword: 'Tam-Pass-1',
      roleIds: ['r1'],
    });
  });

  it('THÀNH CÔNG → hộp ĐÓNG, thanhCong nhận tên đăng nhập, service nhận đủ giá trị form kể cả vai trò', () => {
    service.tao.and.returnValue(of({ id: 'u1' }));
    const thanhCong = jasmine.createSpy('thanhCong');

    store.gui(thanhCong);

    expect(store.hienThi()).withContext('hộp phải đóng sau khi BE trả 200').toBeFalse();
    expect(store.dangLuu()).toBeFalse();
    expect(thanhCong).toHaveBeenCalledOnceWith('an.nguyen');
    expect(service.tao).toHaveBeenCalledOnceWith({
      userName: 'an.nguyen',
      email: 'an@b.vn',
      fullName: 'Nguyễn An',
      tempPassword: 'Tam-Pass-1',
      roleIds: ['r1'],
    });
  });

  it('đang gửi → dangLuu() bật và dongHop() bị khoá; xong thì tắt', () => {
    const nguon = new Subject<{ id: string }>();
    service.tao.and.returnValue(nguon);
    store.gui(() => undefined);
    expect(store.dangLuu()).toBeTrue();

    store.dongHop();
    expect(store.hienThi()).toBeTrue();

    nguon.next({ id: 'u1' });
    nguon.complete();
    expect(store.dangLuu()).toBeFalse();
  });

  it('form không hợp lệ → không gọi service, hộp giữ mở, mọi ô đã chạm', () => {
    store.form.controls.email.setValue('abc');
    store.gui(() => undefined);
    expect(service.tao).not.toHaveBeenCalled();
    expect(store.hienThi()).toBeTrue();
    expect(store.daGui()).toBeTrue();
    expect(store.form.controls.userName.touched).toBeTrue();
    expect(store.lanGuiSai()).withContext('đưa focus về ô sai').toBe(1);
  });

  it('chống gửi hai lần: gọi gui() lần hai khi lần một chưa xong → chỉ MỘT request', () => {
    service.tao.and.returnValue(new Subject<{ id: string }>());

    store.gui(() => undefined);
    store.gui(() => undefined);

    expect(service.tao).toHaveBeenCalledTimes(1);
  });

  it('form hợp lệ → lanGuiSai KHÔNG tăng', () => {
    service.tao.and.returnValue(of({ id: 'u1' }));
    store.gui(() => undefined);
    expect(store.lanGuiSai()).toBe(0);
  });

  describe('lỗi từ server — không được biến mất', () => {
    it('USERNAME_DUPLICATED → lỗi vào ô tên đăng nhập (kèm messageParams), không banner', () => {
      service.tao.and.returnValue(
        throwError(() => loiApi('CORE.USER.USERNAME_DUPLICATED', null, { userName: 'an.nguyen' })),
      );

      store.gui(() => undefined);

      expect(store.form.controls.userName.errors?.['server']).toEqual([
        '[loi.CORE.USER.USERNAME_DUPLICATED]',
      ]);
      expect(instant).toHaveBeenCalledWith('loi.CORE.USER.USERNAME_DUPLICATED', {
        userName: 'an.nguyen',
      });
      expect(store.loiChung()).toBeNull();
      expect(store.hienThi()).toBeTrue();
      expect(store.dangLuu()).toBeFalse();
    });

    it('EMAIL_DUPLICATED → lỗi vào ô email, không banner', () => {
      service.tao.and.returnValue(throwError(() => loiApi('CORE.USER.EMAIL_DUPLICATED')));
      store.gui(() => undefined);
      expect(store.form.controls.email.errors?.['server']).toEqual([
        '[loi.CORE.USER.EMAIL_DUPLICATED]',
      ]);
      expect(store.loiChung()).toBeNull();
    });

    // Mã trùng xuống ô, nhưng mã con đi kèm (nếu có) vẫn theo luật chung — không bị nuốt.
    it('USERNAME_DUPLICATED kèm fieldErrors khoá lạ → mã gốc dưới ô, banner mang câu của MÃ CON', () => {
      service.tao.and.returnValue(
        throwError(() => loiApi('CORE.USER.USERNAME_DUPLICATED', { KhoaLa: BAT_BUOC })),
      );
      store.gui(() => undefined);
      expect(store.form.controls.userName.errors?.['server']).toEqual([
        '[loi.CORE.USER.USERNAME_DUPLICATED]',
      ]);
      expect(store.loiChung()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    it('VALIDATION.FAILED có khoá khớp (Email) → lỗi vào ô, KHÔNG banner', () => {
      service.tao.and.returnValue(
        throwError(() => loiApi('CORE.VALIDATION.FAILED', { Email: BAT_BUOC })),
      );
      store.gui(() => undefined);
      expect(store.form.controls.email.errors?.['server']).toEqual([
        '[loi.CORE.VALIDATION.REQUIRED]',
      ]);
      expect(store.loiChung()).toBeNull();
    });

    // users.md §5: `roleIds` quá 50 phần tử → fieldErrors["RoleIds"] mã CORE.VALIDATION.MAX_ITEMS
    // kèm { MaxItems }; 10-nguoi-dung.md: dòng lỗi dưới ô "Vai trò". Không có chỗ hiện thì lỗi
    // gắn vào control mà không ai thấy — đúng dạng hỏng im lặng 09-forms-validation.md §4.3 cấm.
    it('VALIDATION.FAILED kèm RoleIds MAX_ITEMS → loiTruong().roleIds mang câu kèm tham số, KHÔNG banner', () => {
      instant.and.callFake((khoa: string, tham?: unknown) =>
        tham && Object.keys(tham as object).length > 0
          ? `[${khoa}:${JSON.stringify(tham)}]`
          : `[${khoa}]`,
      );
      service.tao.and.returnValue(
        throwError(() =>
          loiApi('CORE.VALIDATION.FAILED', {
            RoleIds: [{ code: 'CORE.VALIDATION.MAX_ITEMS', messageParams: { MaxItems: '50' } }],
          }),
        ),
      );
      store.gui(() => undefined);

      expect(store.loiTruong().roleIds).toBe('[loi.CORE.VALIDATION.MAX_ITEMS:{"MaxItems":"50"}]');
      expect(store.loiChung()).toBeNull();
      expect(store.hienThi()).toBeTrue();
    });

    // Service tắt toast (BO_QUA_TOAST_LOI) nên hộp là nơi DUY NHẤT hiện lỗi — không được im (§6.2).
    // fe-ui-conventions.md §6.2: mã gốc không nói thay mã con — lý do cụ thể nằm ở mã con.
    it('CREATE_FAILED chỉ có khoá lạ → banner mang câu của MÃ CON, không phải của CREATE_FAILED', () => {
      service.tao.and.returnValue(
        throwError(() => loiApi('CORE.USER.CREATE_FAILED', { KhoaLa: BAT_BUOC })),
      );
      store.gui(() => undefined);
      expect(store.loiChung()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    it('CREATE_FAILED với fieldErrors = null → banner mang câu của mã (Identity từ chối, không gắn được ô nào)', () => {
      service.tao.and.returnValue(throwError(() => loiApi('CORE.USER.CREATE_FAILED', null)));
      store.gui(() => undefined);
      expect(store.loiChung()).toBe('[loi.CORE.USER.CREATE_FAILED]');
      expect(store.hienThi()).toBeTrue();
    });

    it('CREATE_FAILED với fieldErrors = {} → banner mang câu của mã', () => {
      service.tao.and.returnValue(throwError(() => loiApi('CORE.USER.CREATE_FAILED', {})));
      store.gui(() => undefined);
      expect(store.loiChung()).toBe('[loi.CORE.USER.CREATE_FAILED]');
    });

    it('VALIDATION.FAILED với fieldErrors = {} → banner mang câu của mã', () => {
      service.tao.and.returnValue(throwError(() => loiApi('CORE.VALIDATION.FAILED', {})));
      store.gui(() => undefined);
      expect(store.loiChung()).toBe('[loi.CORE.VALIDATION.FAILED]');
    });

    // fe-ui-conventions.md §6.2: store không chọn câu theo một danh sách mã chép từ card. BE thêm
    // `fieldErrors` cho một mã chưa từng có trong danh sách đó thì câu vẫn phải là của MÃ CON.
    it('mã NGOÀI danh sách cũ kèm fieldErrors khoá lạ → banner mang câu của MÃ CON, không phải mã gốc', () => {
      service.tao.and.returnValue(
        throwError(() => loiApi('CORE.USER.MA_NGOAI_DANH_SACH', { KhoaLa: BAT_BUOC })),
      );
      store.gui(() => undefined);
      expect(store.loiChung()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    it('mã NGOÀI danh sách cũ kèm fieldErrors khoá khớp (Email) → lỗi vào ô, KHÔNG banner', () => {
      service.tao.and.returnValue(
        throwError(() => loiApi('CORE.USER.MA_NGOAI_DANH_SACH', { Email: BAT_BUOC })),
      );
      store.gui(() => undefined);
      expect(store.form.controls.email.errors?.['server']).toEqual([
        '[loi.CORE.VALIDATION.REQUIRED]',
      ]);
      expect(store.loiChung()).toBeNull();
    });

    it('mã khác → banner chung theo mã, nội dung form giữ nguyên', () => {
      service.tao.and.returnValue(throwError(() => loiApi('CORE.USER.SOMETHING_ELSE')));
      store.gui(() => undefined);
      expect(store.loiChung()).toBe('[loi.CORE.USER.SOMETHING_ELSE]');
      expect(store.form.controls.userName.value).toBe('an.nguyen');
      expect(store.hienThi()).toBeTrue();
    });

    it('không phải ApiFailureError (mất mạng) → banner câu dự phòng', () => {
      service.tao.and.returnValue(throwError(() => new Error('boom')));
      store.gui(() => undefined);
      expect(store.loiChung()).toBe('[loi.CORE.CLIENT.NO_CONNECTION]');
    });
  });

  describe('loiTruong — câu lỗi từng ô cho component dumb', () => {
    it('ô chưa chạm và chưa bấm gửi → không lỗi', () => {
      store.moHop();
      expect(store.loiTruong().userName).toBeNull();
    });

    it('ô sai được chạm → hiện câu lỗi client; sửa đúng → hết lỗi (tính lại theo sự kiện của form)', () => {
      store.moHop();
      store.form.controls.userName.markAsTouched();
      expect(store.loiTruong().userName).toBe('[loi.CORE.CLIENT.VALIDATION_REQUIRED]');

      store.form.controls.userName.setValue('an.nguyen');
      expect(store.loiTruong().userName).toBeNull();
    });

    it('lỗi server gắn vào ô → loiTruong mang đúng câu đó', () => {
      service.tao.and.returnValue(throwError(() => loiApi('CORE.USER.EMAIL_DUPLICATED')));
      store.gui(() => undefined);
      expect(store.loiTruong().email).toBe('[loi.CORE.USER.EMAIL_DUPLICATED]');
    });
  });

  describe('ô chọn vai trò', () => {
    it('chonVaiTro() nhận mảng → vaiTroChon().selected phản ánh; nhận chuỗi đơn → rỗng', () => {
      store.chonVaiTro(['a', 'b']);
      expect(store.vaiTroChon().selected).toEqual(['a', 'b']);
      store.chonVaiTro('a');
      expect(store.vaiTroChon().selected).toEqual([]);
    });

    it('moHop() xoá lựa chọn và danh sách gợi ý của lần trước', () => {
      store.timVaiTro.options.set([{ key: 'r1', label: 'Kế toán' }]);
      store.moHop();
      expect(store.vaiTroChon().selected).toEqual([]);
      expect(store.vaiTroChon().options).toEqual([]);
    });
  });

  describe('moHop() đặt lại ô tìm vai trò — không mang dấu vết của lần mở trước', () => {
    it('câu "không kết nối được" của lần trước bị xoá', () => {
      vaiTroService.timKiem.and.returnValue(throwError(() => new Error('boom')));
      store.timVaiTro.timKiem('k');
      expect(store.timVaiTro.error()).not.toBeNull();

      store.moHop();

      expect(store.timVaiTro.error()).toBeNull();
      expect(store.vaiTroChon().error).toBeNull();
    });

    it('kết quả của request đang bay tới muộn KHÔNG ghi đè danh sách vừa reset', () => {
      const bay = new Subject<PagedList<VaiTro>>();
      vaiTroService.timKiem.and.returnValue(bay);
      store.timVaiTro.timKiem('k');

      store.moHop();
      bay.next({
        items: [
          {
            id: 'x',
            name: 'KHÔNG ĐƯỢC HIỆN',
            isSystem: false,
            userCount: 0,
            createdAt: null,
            version: 'v',
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
      });

      expect(store.timVaiTro.options()).toEqual([]);
      expect(store.timVaiTro.loading()).toBeFalse();
    });
  });

  describe('mở / đóng / hỏi xác nhận huỷ', () => {
    it('moHop() lần sau reset lỗi và cờ đã gửi', () => {
      store.form.controls.email.setValue('');
      store.gui(() => undefined);
      expect(store.daGui()).toBeTrue();

      store.moHop();

      expect(store.daGui()).toBeFalse();
      expect(store.loiChung()).toBeNull();
      expect(store.hienThi()).toBeTrue();
      expect(store.form.controls.userName.value).toBe('');
      expect(store.loiTruong().userName).toBeNull();
    });

    it('onDismissAttempted() hỏi xác nhận; huyXacNhanHuy() giữ hộp; xacNhanHuy() đóng hộp', () => {
      store.onDismissAttempted();
      expect(store.hienThiXacNhanHuy()).toBeTrue();

      store.huyXacNhanHuy();
      expect(store.hienThiXacNhanHuy()).toBeFalse();
      expect(store.hienThi()).toBeTrue();

      store.onDismissAttempted();
      store.xacNhanHuy();
      expect(store.hienThiXacNhanHuy()).toBeFalse();
      expect(store.hienThi()).toBeFalse();
    });
  });
});

/** Nạp ĐÚNG `public/i18n/vi.json` — bản dịch giả ở trên in lại khoá nên không thấy tham số. */
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
 * Hai mã trùng ở hộp Tạo: câu dưới ô đi qua bảng dịch THẬT với `messageParams` đúng khoá của card
 * (contracts/users.md §5: `{ "UserName": "…" }`, `{ "Email": "…" }`); thiếu khoá dịch thì đường lùi
 * là câu BE gửi kèm (`dichLoi`), không phải khoá dịch thô. Cùng khối: mã con `USERNAME_RESERVED`
 * trong `fieldErrors["UserName"]` của 400 `CORE.VALIDATION.FAILED`.
 */
describe('TaoNguoiDungStore — câu lỗi dưới ô Tên đăng nhập / Email qua bảng dịch thật', () => {
  function loiTrung(code: string, tham: Record<string, string>, message: string): ApiFailureError {
    return new ApiFailureError({
      success: false,
      data: null,
      error: { code, type: 'Conflict', message, messageParams: tham, fieldErrors: null },
      traceId: 't',
    });
  }

  async function dung(
    loader: Type<TranslateLoader>,
    loi: ApiFailureError,
  ): Promise<TaoNguoiDungStore> {
    const service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', ['tao']);
    service.tao.and.returnValue(throwError(() => loi));
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        TaoNguoiDungStore,
        { provide: NguoiDungService, useValue: service },
        {
          provide: VaiTroService,
          useValue: jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem']),
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(loader),
        }),
      ],
    });
    await firstValueFrom(TestBed.inject(TranslateService).use('vi'));
    const store = TestBed.inject(TaoNguoiDungStore);
    store.moHop();
    store.form.setValue({
      userName: 'an.nguyen',
      email: 'an@b.vn',
      fullName: 'Nguyễn An',
      tempPassword: 'Tam-Pass-1',
      roleIds: [],
    });
    return store;
  }

  const TRUNG_TEN = loiTrung(
    'CORE.USER.USERNAME_DUPLICATED',
    { UserName: 'an.nguyen' },
    'User name an.nguyen already exists.',
  );
  const TRUNG_EMAIL = loiTrung(
    'CORE.USER.EMAIL_DUPLICATED',
    { Email: 'an@b.vn' },
    'Email an@b.vn already exists.',
  );

  it('USERNAME_DUPLICATED → câu dưới ô Tên đăng nhập mang tên thật', async () => {
    const store = await dung(ViJsonThatLoader, TRUNG_TEN);
    store.gui(() => undefined);
    expect(store.loiTruong().userName).toBe('Tên đăng nhập an.nguyen đã tồn tại.');
    expect(store.loiChung()).toBeNull();
  });

  it('EMAIL_DUPLICATED → câu dưới ô Email mang địa chỉ thật', async () => {
    const store = await dung(ViJsonThatLoader, TRUNG_EMAIL);
    store.gui(() => undefined);
    expect(store.loiTruong().email).toBe('Email an@b.vn đã tồn tại.');
    expect(store.loiChung()).toBeNull();
  });

  // Tên đăng nhập là danh tính hệ thống: BE từ chối bằng validator — mã nằm trong fieldErrors, không
  // messageParams — nên nó xuống ô qua `applyFormFailure` như mọi mã con, không qua bảng mã gốc.
  it('VALIDATION.FAILED kèm fieldErrors.UserName USERNAME_RESERVED → câu thật dưới ô Tên đăng nhập, không banner', async () => {
    const store = await dung(
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
            UserName: [{ code: 'CORE.USER.USERNAME_RESERVED', messageParams: null }],
          },
        },
        traceId: 't',
      }),
    );
    store.form.controls.userName.setValue('system');

    store.gui(() => undefined);

    expect(store.loiTruong().userName).toBe('Tên đăng nhập này được hệ thống giữ riêng.');
    expect(store.loiChung()).toBeNull();
  });

  it('bảng dịch thiếu khoá → câu dưới ô là câu BE gửi kèm, cho cả hai mã', async () => {
    const storeTen = await dung(BangDichRongLoader, TRUNG_TEN);
    storeTen.gui(() => undefined);
    expect(storeTen.loiTruong().userName).toBe('User name an.nguyen already exists.');

    TestBed.resetTestingModule();
    const storeEmail = await dung(BangDichRongLoader, TRUNG_EMAIL);
    storeEmail.gui(() => undefined);
    expect(storeEmail.loiTruong().email).toBe('Email an@b.vn already exists.');
  });
});
