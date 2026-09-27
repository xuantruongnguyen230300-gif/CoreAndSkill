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
import { ToastService } from '../../../../core/toast/toast.service';
import { VaiTro } from '../models/vai-tro.model';
import { VaiTroService } from '../services/vai-tro.service';
import { HopVaiTroStore } from './hop-vai-tro.store';

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

function vaiTro(name: string, version = 'v1'): VaiTro {
  return { id: 'r1', name, isSystem: false, userCount: 0, createdAt: null, version };
}

describe('HopVaiTroStore', () => {
  let service: jasmine.SpyObj<VaiTroService>;
  let toast: ToastService;
  let store: HopVaiTroStore;

  beforeEach(() => {
    service = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['tao', 'doiTen', 'chiTiet']);
    const translate = {
      instant: (khoa: string) => `[${khoa}]`,
    } as unknown as TranslateService;
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        HopVaiTroStore,
        { provide: VaiTroService, useValue: service },
        { provide: TranslateService, useValue: translate },
      ],
    });
    store = TestBed.inject(HopVaiTroStore);
    toast = TestBed.inject(ToastService);
  });

  describe('mở hộp', () => {
    it('moHopTao() → hộp hiện, ô tên trống, không ở chế độ sửa, tiêu đề "tạo"', () => {
      store.moHopTao();
      expect(store.hienThi()).toBeTrue();
      expect(store.dangSua()).toBeNull();
      expect(store.form.getRawValue().name).toBe('');
      expect(store.tieuDe()).toBe('[vaiTro.form.tieuDeTao]');
    });

    it('moHopSua() → ô mang tên hiện tại, tiêu đề "đổi tên"; mở lại không mang dấu vết lần trước', () => {
      store.moHopTao();
      store.daGui.set(true);
      store.loi.set('cũ');
      store.xungDot.set(true);

      store.moHopSua(vaiTro('Kế toán'));

      expect(store.form.getRawValue().name).toBe('Kế toán');
      expect(store.tieuDe()).toBe('[vaiTro.form.tieuDeDoiTen]');
      expect(store.daGui()).toBeFalse();
      expect(store.loi()).toBeNull();
      expect(store.xungDot()).toBeFalse();
    });
  });

  describe('gui()', () => {
    it('form trống → không gọi service, lỗi bắt buộc hiện ở ô', () => {
      store.moHopTao();
      store.gui(() => undefined);
      expect(service.tao).not.toHaveBeenCalled();
      expect(store.loiTen()).toBe('[loi.CORE.CLIENT.VALIDATION_REQUIRED]');
    });

    it('tạo thành công → hộp đóng, toast thành công, xong nhận null', () => {
      service.tao.and.returnValue(of({ id: 'r9' }));
      const xong = jasmine.createSpy('xong');
      store.moHopTao();
      store.form.setValue({ name: 'Kế toán' });

      store.gui(xong);

      expect(service.tao).toHaveBeenCalledOnceWith('Kế toán');
      expect(store.hienThi()).toBeFalse();
      expect(store.dangLuu()).toBeFalse();
      expect(toast.latest()?.severity).toBe('success');
      expect(toast.latest()?.summary).toBe('[vaiTro.thongBao.taoThanhCong]');
      expect(xong).toHaveBeenCalledOnceWith(null);
    });

    it('đổi tên thành công → gửi kèm version, xong nhận vai trò mới', () => {
      const moi = vaiTro('Kế toán trưởng', 'v2');
      service.doiTen.and.returnValue(of(moi));
      const xong = jasmine.createSpy('xong');
      store.moHopSua(vaiTro('Kế toán'));
      store.form.setValue({ name: 'Kế toán trưởng' });

      store.gui(xong);

      expect(service.doiTen).toHaveBeenCalledOnceWith('r1', 'Kế toán trưởng', 'v1');
      expect(toast.latest()?.summary).toBe('[vaiTro.thongBao.doiTenThanhCong]');
      expect(xong).toHaveBeenCalledOnceWith(moi);
    });

    it('đang lưu → gửi lần hai bị chặn, dong() bị khoá', () => {
      const nguon = new Subject<{ id: string }>();
      service.tao.and.returnValue(nguon);
      store.moHopTao();
      store.form.setValue({ name: 'Kế toán' });

      store.gui(() => undefined);
      store.gui(() => undefined);
      store.dong();

      expect(service.tao).toHaveBeenCalledTimes(1);
      expect(store.hienThi()).toBeTrue();
      nguon.next({ id: 'r9' });
      nguon.complete();
      expect(store.dangLuu()).toBeFalse();
    });

    it('409 CONCURRENCY.CONFLICT → bật xungDot, hộp vẫn mở, ô giữ tên đang nhập', () => {
      service.doiTen.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
      store.moHopSua(vaiTro('Kế toán'));
      store.form.setValue({ name: 'Tên mới' });

      store.gui(() => undefined);

      expect(store.xungDot()).toBeTrue();
      expect(store.hienThi()).toBeTrue();
      expect(store.form.getRawValue().name).toBe('Tên mới');
    });

    it('409 ROLE.NAME_DUPLICATE → lỗi nằm ở ô tên, không banner', () => {
      service.tao.and.returnValue(throwError(() => loiApi('CORE.ROLE.NAME_DUPLICATE')));
      store.moHopTao();
      store.form.setValue({ name: 'Kế toán' });

      store.gui(() => undefined);

      expect(store.loiTen()).toBe('[loi.CORE.ROLE.NAME_DUPLICATE]');
      expect(store.loi()).toBeNull();
    });

    it('lỗi không phải ApiFailureError → banner "không kết nối được"', () => {
      service.tao.and.returnValue(throwError(() => new Error('mạng')));
      store.moHopTao();
      store.form.setValue({ name: 'Kế toán' });

      store.gui(() => undefined);

      expect(store.loi()).toBe('[loi.CORE.CLIENT.NO_CONNECTION]');
    });

    it('mã khác → banner mang câu của chính mã', () => {
      service.tao.and.returnValue(throwError(() => loiApi('CORE.ROLE.KHAC')));
      store.moHopTao();
      store.form.setValue({ name: 'Kế toán' });

      store.gui(() => undefined);

      expect(store.loi()).toBe('[loi.CORE.ROLE.KHAC]');
    });

    // fe-ui-conventions.md §6.1/§6.2: mọi mã đi qua `applyFormFailure`, không lọc theo danh sách mã.
    it('mã NGOÀI danh sách cũ kèm fieldErrors: khoá khớp → lỗi vào ô tên; chỉ khoá lạ → banner mang câu của MÃ CON', () => {
      store.moHopTao();
      store.form.setValue({ name: 'Kế toán' });
      service.tao.and.returnValue(
        throwError(() =>
          loiApi('CORE.ROLE.KHAC', {
            Name: [{ code: 'CORE.VALIDATION.REQUIRED', messageParams: null }],
          }),
        ),
      );

      store.gui(() => undefined);

      expect(store.loiTen()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
      expect(store.loi()).toBeNull();

      store.moHopTao();
      store.form.setValue({ name: 'Kế toán' });
      service.tao.and.returnValue(
        throwError(() =>
          loiApi('CORE.ROLE.KHAC', {
            KhoaLa: [{ code: 'CORE.VALIDATION.REQUIRED', messageParams: null }],
          }),
        ),
      );

      store.gui(() => undefined);

      expect(store.loi()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });
  });

  describe('taiLaiSauXungDot()', () => {
    it('nạp tên từ máy chủ, tắt xungDot, tăng lanNapLai, gọi tải lại danh sách', () => {
      service.doiTen.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
      service.chiTiet.and.returnValue(of(vaiTro('Tên của người khác', 'v2')));
      const taiLaiDanhSach = jasmine.createSpy('taiLaiDanhSach');
      store.moHopSua(vaiTro('Kế toán'));
      store.form.setValue({ name: 'Tên mới' });
      store.gui(() => undefined);

      store.taiLaiSauXungDot(taiLaiDanhSach);

      expect(taiLaiDanhSach).toHaveBeenCalledTimes(1);
      expect(store.xungDot()).toBeFalse();
      expect(store.form.getRawValue().name).toBe('Tên của người khác');
      expect(store.dangSua()?.version).toBe('v2');
      expect(store.lanNapLai()).toBe(1);
      expect(store.dangLuu()).toBeFalse();
    });

    it('không ở chế độ sửa → không làm gì', () => {
      store.moHopTao();
      const taiLaiDanhSach = jasmine.createSpy('taiLaiDanhSach');
      store.taiLaiSauXungDot(taiLaiDanhSach);
      expect(taiLaiDanhSach).not.toHaveBeenCalled();
      expect(service.chiTiet).not.toHaveBeenCalled();
    });
  });

  describe('xác nhận huỷ', () => {
    it('onDismissAttempted → hỏi; xacNhanHuy → đóng hộp; huyXacNhanHuy → giữ hộp', () => {
      store.moHopTao();
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
 * `CORE.ROLE.NAME_DUPLICATE` dưới ô "Tên vai trò" (Design/Screens/11-vai-tro.md, bảng Mã lỗi → chỗ
 * hiện): câu đi qua `dichLoi` — bảng dịch thật cho câu thật; thiếu khoá dịch thì đường lùi là câu BE
 * gửi kèm, không phải khoá dịch thô.
 */
describe('HopVaiTroStore — NAME_DUPLICATE qua bảng dịch thật', () => {
  const TRUNG_TEN = new ApiFailureError({
    success: false,
    data: null,
    error: {
      code: 'CORE.ROLE.NAME_DUPLICATE',
      type: 'Conflict',
      message: 'Role name already exists.',
      messageParams: null,
      fieldErrors: null,
    },
    traceId: 't',
  });

  async function dung(loader: Type<TranslateLoader>): Promise<HopVaiTroStore> {
    const service = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['tao']);
    service.tao.and.returnValue(throwError(() => TRUNG_TEN));
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        HopVaiTroStore,
        { provide: VaiTroService, useValue: service },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(loader),
        }),
      ],
    });
    await firstValueFrom(TestBed.inject(TranslateService).use('vi'));
    const store = TestBed.inject(HopVaiTroStore);
    store.moHopTao();
    store.form.setValue({ name: 'Kế toán' });
    return store;
  }

  it('câu dưới ô tên là câu thật của bảng dịch; không banner', async () => {
    const store = await dung(ViJsonThatLoader);
    store.gui(() => undefined);
    expect(store.loiTen()).toBe('Tên vai trò đã tồn tại trong đơn vị.');
    expect(store.loi()).toBeNull();
  });

  it('bảng dịch thiếu khoá → câu dưới ô là câu BE gửi kèm, không phải khoá dịch thô', async () => {
    const store = await dung(BangDichRongLoader);
    store.gui(() => undefined);
    expect(store.loiTen()).toBe('Role name already exists.');
  });
});
