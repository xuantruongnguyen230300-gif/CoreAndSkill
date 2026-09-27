import { TranslateService } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';

import { ApiFailureError } from '../../../../core/http/api-result.model';
import { PagedList } from '../../../../core/http/paged.model';
import { VaiTro } from '../models/vai-tro.model';
import { VaiTroService } from '../services/vai-tro.service';
import { TraCuuVaiTroStore } from './tra-cuu-vai-tro.store';

function vaiTro(id: string, name: string, isSystem = false): VaiTro {
  return { id, name, isSystem, userCount: 0, createdAt: null, version: 'v' };
}

function trang(...items: VaiTro[]): PagedList<VaiTro> {
  return { items, page: 1, pageSize: 20, totalCount: items.length };
}

describe('TraCuuVaiTroStore', () => {
  let service: jasmine.SpyObj<VaiTroService>;
  let tim: TraCuuVaiTroStore;

  beforeEach(() => {
    service = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem']);
    const translate = { instant: (khoa: string) => `[${khoa}]` } as unknown as TranslateService;
    tim = new TraCuuVaiTroStore(service, translate);
  });

  it('tìm xong → options là các vai trò tìm được; vai trò hệ thống mang gợi ý "hệ thống"', () => {
    service.timKiem.and.returnValue(of(trang(vaiTro('1', 'Kế toán'), vaiTro('2', 'Admin', true))));

    tim.timKiem('a');

    expect(tim.options()).toEqual([
      { key: '1', label: 'Kế toán', hint: undefined },
      { key: '2', label: 'Admin', hint: '[vaiTro.loai.heThong]' },
    ]);
    expect(tim.loading()).toBeFalse();
    expect(tim.error()).toBeNull();
  });

  it('đang chờ server → loading() bật, xong thì tắt', () => {
    const nguon = new Subject<PagedList<VaiTro>>();
    service.timKiem.and.returnValue(nguon);

    tim.timKiem('a');
    expect(tim.loading()).toBeTrue();

    nguon.next(trang(vaiTro('1', 'Kế toán')));
    expect(tim.loading()).toBeFalse();
  });

  it('bộ lọc loại bớt kết quả (vai trò hệ thống đứng ngoài ô chọn)', () => {
    service.timKiem.and.returnValue(of(trang(vaiTro('1', 'Kế toán'), vaiTro('2', 'Admin', true))));

    tim.timKiem('a', (vt) => !vt.isSystem);

    expect(tim.options().map((o) => o.key)).toEqual(['1']);
  });

  it('gõ tiếp khi request cũ chưa về → kết quả của request cũ tới muộn KHÔNG ghi đè kết quả mới', () => {
    const cu = new Subject<PagedList<VaiTro>>();
    const moi = new Subject<PagedList<VaiTro>>();
    service.timKiem.and.returnValues(cu, moi);

    tim.timKiem('k');
    tim.timKiem('ke');
    moi.next(trang(vaiTro('9', 'Kế toán')));
    cu.next(trang(vaiTro('1', 'KHÔNG ĐƯỢC HIỆN')));

    expect(tim.options().map((o) => o.key)).toEqual(['9']);
    expect(cu.observed)
      .withContext('request cũ phải bị HUỶ thật, không chỉ bỏ kết quả')
      .toBeFalse();
  });

  describe('reset() — mở lại hộp không mang dấu vết của lần mở trước', () => {
    it('kết quả của request đang bay tới muộn KHÔNG ghi đè danh sách vừa reset, và request bị HUỶ thật', () => {
      const bay = new Subject<PagedList<VaiTro>>();
      service.timKiem.and.returnValue(bay);
      tim.timKiem('k');
      expect(tim.loading()).toBeTrue();

      tim.reset();
      bay.next(trang(vaiTro('1', 'KHÔNG ĐƯỢC HIỆN')));

      expect(tim.options()).toEqual([]);
      expect(tim.loading()).toBeFalse();
      expect(bay.observed).withContext('request đang bay phải bị huỷ thật').toBeFalse();
    });

    it('xoá câu lỗi mạng và danh sách của lần trước', () => {
      service.timKiem.and.returnValue(throwError(() => new Error('boom')));
      tim.seed([vaiTro('7', 'Có sẵn')]);
      tim.timKiem('k');
      expect(tim.error()).not.toBeNull();

      tim.reset();

      expect(tim.error()).toBeNull();
      expect(tim.options()).toEqual([]);
    });

    it('sau reset vẫn tìm được (Subject không bị complete)', () => {
      service.timKiem.and.returnValue(of(trang(vaiTro('1', 'Kế toán'))));
      tim.reset();
      tim.timKiem('k');
      expect(tim.options().map((o) => o.key)).toEqual(['1']);
    });
  });

  it('lỗi mạng → error() mang câu mất kết nối, options giữ nguyên; lần gõ SAU vẫn gọi được (catchError nằm trong switchMap)', () => {
    service.timKiem.and.returnValues(
      throwError(() => new Error('boom')),
      of(trang(vaiTro('1', 'Kế toán'))),
    );
    tim.seed([vaiTro('7', 'Có sẵn')]);

    tim.timKiem('a');
    expect(tim.error()).toBe('[loi.CORE.CLIENT.NO_CONNECTION]');
    expect(tim.loading()).toBeFalse();
    expect(tim.options().map((o) => o.key)).toEqual(['7']);

    tim.timKiem('b');
    expect(service.timKiem).toHaveBeenCalledTimes(2);
    expect(tim.error()).toBeNull();
    expect(tim.options().map((o) => o.key)).toEqual(['1']);
  });

  it('seed() đặt nhãn ban đầu mà không gọi server', () => {
    tim.seed([vaiTro('1', 'Kế toán')]);
    expect(tim.options().map((o) => o.label)).toEqual(['Kế toán']);
    expect(service.timKiem).not.toHaveBeenCalled();
  });

  it('seed() nhận vai trò dạng rút gọn (id/name/isSystem) — không cần userCount/createdAt', () => {
    // Đây là hình dạng của `NguoiDung.roles`. Trước đây phải ép kiểu `as VaiTro[]` để qua được compiler.
    tim.seed([{ id: '9', name: 'Quản trị', isSystem: true }]);
    expect(tim.options()).toEqual([{ key: '9', label: 'Quản trị', hint: jasmine.any(String) }]);
  });

  it('hai thể hiện độc lập: tìm ở ô A không đổi options của ô B (lý do lớp này không cấp bằng DI)', () => {
    const translate = { instant: (khoa: string) => `[${khoa}]` } as unknown as TranslateService;
    const b = new TraCuuVaiTroStore(service, translate);
    service.timKiem.and.returnValue(of(trang(vaiTro('1', 'Kế toán'))));

    tim.timKiem('a');

    expect(tim.options().length).toBe(1);
    expect(b.options()).toEqual([]);
  });
});

/**
 * Design/Screens/10-nguoi-dung.md, bảng mã lỗi dòng "tìm vai trò (ô chọn)": store GIỮ lỗi để lớp nổi
 * hiện đúng câu — `dichLoiChoMan` (mất kết nối → `loi.CORE.CLIENT.NO_CONNECTION`); lớp xuyên suốt
 * (5xx, 403 `CORE.AUTH.*`, đã toast kèm traceId) → `vaiTro.tim.loi`. Không nuốt thành một cờ.
 */
describe('TraCuuVaiTroStore — giữ lỗi của lần tìm', () => {
  let service: jasmine.SpyObj<VaiTroService>;
  let tim: TraCuuVaiTroStore;

  beforeEach(() => {
    service = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem']);
    const translate = { instant: (khoa: string) => `[${khoa}]` } as unknown as TranslateService;
    tim = new TraCuuVaiTroStore(service, translate);
  });

  function loi(code: string, status: number): ApiFailureError {
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

  it('lỗi thường (400 VALIDATION.FAILED) → câu dịch theo MÃ', () => {
    service.timKiem.and.returnValue(throwError(() => loi('CORE.VALIDATION.FAILED', 400)));
    tim.timKiem('a');
    expect(tim.error()).toBe('[loi.CORE.VALIDATION.FAILED]');
  });

  it('lớp xuyên suốt (500) → câu vaiTro.tim.loi — chi tiết đã ở toast', () => {
    service.timKiem.and.returnValue(throwError(() => loi('CORE.SYSTEM.UNEXPECTED', 500)));
    tim.timKiem('a');
    expect(tim.error()).toBe('[vaiTro.tim.loi]');
  });

  it('lớp xuyên suốt (403 CORE.AUTH.FORBIDDEN) → câu vaiTro.tim.loi', () => {
    service.timKiem.and.returnValue(throwError(() => loi('CORE.AUTH.FORBIDDEN', 403)));
    tim.timKiem('a');
    expect(tim.error()).toBe('[vaiTro.tim.loi]');
  });

  it('mất kết nối (status 0, không envelope) → câu mất kết nối', () => {
    service.timKiem.and.returnValue(throwError(() => new ApiFailureError(null, 0)));
    tim.timKiem('a');
    expect(tim.error()).toBe('[loi.CORE.CLIENT.NO_CONNECTION]');
  });
});
