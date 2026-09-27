import { AnimationDriver } from '@angular/animations/browser';
import { ANIMATION_MODULE_TYPE, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { TruyVanMedia, chonProviderHoatAnh, provideCoreAnimations } from './core-animations';

// Chuỗi truy vấn viết thẳng ở đây, không lấy từ tệp nguồn: spec là thước đo độc lập. Nguồn hỏi sai
// truy vấn (gõ nhầm, đảo sang `no-preference`) thì hàm giả này thôi trả lời "khớp" và ca bật cờ đỏ.
const TRUY_VAN_GIAM = '(prefers-reduced-motion: reduce)';

/**
 * Nửa provider của nhánh giảm chuyển động (docs/Design/DESIGN.md §7): cài đặt giảm chuyển động của
 * hệ điều hành chọn bộ chạy hoạt ảnh của Angular. Trình duyệt chạy test không bật được cờ đó từ bên
 * trong một spec, nên ba ca đầu không dùng `matchMedia` thật — hai ca truyền hàm giả, một ca không
 * truyền gì. Hàm chọn không đọc biến toàn cục nào, và đó là lý do nó kiểm được bằng unit test
 * thường.
 *
 * Kết quả đọc qua `ANIMATION_MODULE_TYPE`, token công khai của Angular cho biết bộ hoạt ảnh nào
 * đang được cấp: `'NoopAnimations'` là bộ chạy rỗng, `'BrowserAnimations'` là bộ chạy thật.
 *
 * Token đó KHÔNG phân biệt bộ chạy rỗng nạp lười (`provideAnimationsAsync('noop')`) với bản nạp
 * ngay (`provideNoopAnimations()`) — cả hai đều ra `'NoopAnimations'`. Ca thứ tư khoá riêng khác
 * biệt đó qua `AnimationDriver`: chỉ bản nạp ngay đăng ký nó vào DI, và bản nạp ngay là bản kéo
 * bộ máy hoạt ảnh vào bundle khởi động (lý do ở chú thích của `chonProviderHoatAnh`).
 */
describe('chonProviderHoatAnh — chọn bộ chạy hoạt ảnh theo prefers-reduced-motion', () => {
  function boChayDuocChon(matchMedia: TruyVanMedia | undefined): string | null {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), chonProviderHoatAnh(matchMedia)],
    });
    return TestBed.inject(ANIMATION_MODULE_TYPE, null);
  }

  const heDieuHanhXinGiam: TruyVanMedia = (truyVan) => ({ matches: truyVan === TRUY_VAN_GIAM });
  const heDieuHanhKhongXin: TruyVanMedia = () => ({ matches: false });

  it('hệ điều hành bật giảm chuyển động → bộ chạy rỗng (NoopAnimations)', () => {
    expect(boChayDuocChon(heDieuHanhXinGiam)).toBe('NoopAnimations');
  });

  it('hệ điều hành không bật → bộ chạy thật (BrowserAnimations), hoạt ảnh giữ nguyên', () => {
    expect(boChayDuocChon(heDieuHanhKhongXin)).toBe('BrowserAnimations');
  });

  it('không có matchMedia (SSR, môi trường test không cài nó) → bộ chạy rỗng', () => {
    expect(boChayDuocChon(undefined)).toBe('NoopAnimations');
  });

  it('không nhánh nào cấp sẵn AnimationDriver — bộ máy hoạt ảnh luôn nạp lười', () => {
    // Ngân sách bundle (F14) chỉ CẢNH BÁO khi vượt, không làm build đỏ; ca này là thứ làm đỏ.
    const driverCuaNhanh = (matchMedia: TruyVanMedia | undefined): AnimationDriver | null => {
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        providers: [provideZonelessChangeDetection(), chonProviderHoatAnh(matchMedia)],
      });
      return TestBed.inject(AnimationDriver, null);
    };

    expect(driverCuaNhanh(heDieuHanhXinGiam)).withContext('nhánh bật cờ').toBeNull();
    expect(driverCuaNhanh(undefined)).withContext('nhánh vắng matchMedia').toBeNull();
    expect(driverCuaNhanh(heDieuHanhKhongXin)).withContext('nhánh không bật cờ').toBeNull();
  });

  it('nhận matchMedia THẬT, tách rời khỏi window — đúng cách provideCoreAnimations truyền vào', () => {
    // Máy chạy test bật hay tắt cờ thì kết quả khác nhau, nên thứ khoá là hàm chọn trả lời KHỚP
    // với chính trình duyệt, không phải một giá trị cố định.
    const xinGiam = globalThis.matchMedia(TRUY_VAN_GIAM).matches;

    expect(boChayDuocChon(globalThis.matchMedia)).toBe(
      xinGiam ? 'NoopAnimations' : 'BrowserAnimations',
    );
  });
});

/**
 * Hàm composition root gọi (luật F39). Ba ca của `chonProviderHoatAnh` ở trên truyền hàm giả vào
 * tham số; chúng không nói gì về việc `provideCoreAnimations` có THẬT SỰ hỏi trình duyệt hay không.
 * Một thân hàm bị rút ruột — luôn trả bộ chạy thật — vẫn để mọi ca trên xanh, vì trình duyệt chạy
 * test thường không bật cờ.
 *
 * Nên ở đây `matchMedia` của trình duyệt bị thay bằng spy trong đúng một ca, và Jasmine trả nó lại
 * sau ca đó. Đọc `globalThis.matchMedia` lúc GỌI hàm, không lúc nạp tệp — nên spy phải cài trước
 * `boChayCuaCore()`.
 */
describe('provideCoreAnimations — hỏi matchMedia của trình duyệt', () => {
  function boChayCuaCore(): string | null {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideCoreAnimations()],
    });
    return TestBed.inject(ANIMATION_MODULE_TYPE, null);
  }

  function traLoiCuaHeDieuHanh(xinGiam: boolean): (truyVan: string) => MediaQueryList {
    return (truyVan) => ({ matches: xinGiam && truyVan === TRUY_VAN_GIAM }) as MediaQueryList;
  }

  it('trình duyệt báo bật giảm chuyển động → bộ chạy rỗng, và đã hỏi đúng truy vấn', () => {
    const hoi = spyOn(globalThis, 'matchMedia').and.callFake(traLoiCuaHeDieuHanh(true));

    expect(boChayCuaCore()).toBe('NoopAnimations');
    expect(hoi).toHaveBeenCalledOnceWith(TRUY_VAN_GIAM);
  });

  it('trình duyệt báo không bật → bộ chạy thật', () => {
    spyOn(globalThis, 'matchMedia').and.callFake(traLoiCuaHeDieuHanh(false));

    expect(boChayCuaCore()).toBe('BrowserAnimations');
  });

  it('matchMedia thật của trình duyệt, không spy → khớp với chính trình duyệt', () => {
    const xinGiam = globalThis.matchMedia(TRUY_VAN_GIAM).matches;

    expect(boChayCuaCore()).toBe(xinGiam ? 'NoopAnimations' : 'BrowserAnimations');
  });
});
