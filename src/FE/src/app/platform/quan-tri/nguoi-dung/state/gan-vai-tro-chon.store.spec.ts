import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { Subject, of } from 'rxjs';

import { PagedList } from '../../../../core/http/paged.model';
import { VaiTro } from '../../vai-tro/models/vai-tro.model';
import { VaiTroService } from '../../vai-tro/services/vai-tro.service';
import { VaiTroRutGon } from '../models/nguoi-dung.model';
import { GanVaiTroChonStore } from './gan-vai-tro-chon.store';

const KE_TOAN: VaiTroRutGon = { id: 'kt', name: 'Kế toán', isSystem: false };
const NHAN_SU: VaiTroRutGon = { id: 'ns', name: 'Nhân sự', isSystem: false };
const QUAN_TRI: VaiTroRutGon = { id: 'qt', name: 'Quản trị', isSystem: true };

function vaiTro(r: VaiTroRutGon): VaiTro {
  return { ...r, userCount: 0, createdAt: null, version: 'v' };
}

describe('GanVaiTroChonStore', () => {
  let service: jasmine.SpyObj<VaiTroService>;
  let store: GanVaiTroChonStore;

  beforeEach(() => {
    service = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem']);
    const translate = { instant: (khoa: string) => `[${khoa}]` } as unknown as TranslateService;
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        GanVaiTroChonStore,
        { provide: VaiTroService, useValue: service },
        { provide: TranslateService, useValue: translate },
      ],
    });
    store = TestBed.inject(GanVaiTroChonStore);
  });

  it('xem người khác: mọi vai trò hiện có vào ô chọn, không giữ gì đứng ngoài', () => {
    store.moHop([KE_TOAN, QUAN_TRI], false);

    expect(store.dangChon()).toEqual(['kt', 'qt']);
    expect(store.heThongCuaMinh()).toEqual([]);
    expect(store.tapDich()).toEqual(['kt', 'qt']);
  });

  it('xem CHÍNH MÌNH: vai trò hệ thống đứng ngoài ô chọn nhưng vẫn nằm trong tập gửi lên (users.md §2, §7)', () => {
    store.moHop([KE_TOAN, QUAN_TRI], true);

    expect(store.dangChon()).toEqual(['kt']);
    expect(store.heThongCuaMinh().map((r) => r.id)).toEqual(['qt']);
    expect(store.tapDich()).toEqual(['kt', 'qt']);
    // Nhãn ban đầu của ô chọn chỉ có phần còn lại, không lộ vai trò hệ thống.
    expect(store.timVaiTro.options().map((o) => o.key)).toEqual(['kt']);
  });

  it('dirty: false ngay sau khi mở; true khi đổi; về false khi chọn lại đúng tập ban đầu (không phụ thuộc thứ tự)', () => {
    store.moHop([KE_TOAN, NHAN_SU], false);
    expect(store.dirty()).toBeFalse();

    store.chon(['kt']);
    expect(store.dirty()).toBeTrue();

    store.chon(['ns', 'kt']);
    expect(store.dirty()).toBeFalse();
  });

  it('chon() nhận chuỗi lẻ (không phải mảng) → coi là chưa chọn gì', () => {
    store.moHop([KE_TOAN], false);
    store.chon('kt');
    expect(store.dangChon()).toEqual([]);
  });

  it('mở hộp lần sau reset trạng thái của lần trước', () => {
    store.moHop([KE_TOAN, QUAN_TRI], true);
    store.chon([]);
    expect(store.dirty()).toBeTrue();

    store.moHop([NHAN_SU], false);

    expect(store.dangChon()).toEqual(['ns']);
    expect(store.heThongCuaMinh()).toEqual([]);
    expect(store.dirty()).toBeFalse();
  });

  it('mở hộp lần sau: câu lỗi mạng của lần trước bị xoá và kết quả tới muộn KHÔNG ghi đè danh sách seed', () => {
    const bay = new Subject<PagedList<VaiTro>>();
    service.timKiem.and.returnValue(bay);
    store.moHop([KE_TOAN], false);
    store.timKiem('k');
    store.timVaiTro.error.set('câu lỗi lần trước');

    store.moHop([NHAN_SU], false);
    bay.next({ items: [vaiTro(QUAN_TRI)], page: 1, pageSize: 20, totalCount: 1 });

    expect(store.timVaiTro.error()).toBeNull();
    expect(store.timVaiTro.options().map((o) => o.key)).toEqual(['ns']);
  });

  it('tìm kiếm khi xem chính mình → kết quả KHÔNG chứa vai trò hệ thống đang đứng ngoài', () => {
    const trang: PagedList<VaiTro> = {
      items: [vaiTro(KE_TOAN), vaiTro(QUAN_TRI)],
      page: 1,
      pageSize: 20,
      totalCount: 2,
    };
    service.timKiem.and.returnValue(of(trang));
    store.moHop([QUAN_TRI], true);

    store.timKiem('a');

    expect(store.timVaiTro.options().map((o) => o.key)).toEqual(['kt']);
  });
});
