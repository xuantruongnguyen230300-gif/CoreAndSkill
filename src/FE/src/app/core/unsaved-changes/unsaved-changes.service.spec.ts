import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { UnsavedChangesService } from './unsaved-changes.service';

describe('UnsavedChangesService', () => {
  let service: UnsavedChangesService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    service = TestBed.inject(UnsavedChangesService);
  });

  it('chưa có màn nào đăng ký → coThayDoiChuaLuu() là false', () => {
    expect(service.coThayDoiChuaLuu()).toBeFalse();
  });

  it('xinRoiTrang() không có thay đổi chưa lưu → resolve true NGAY, không mở hộp', async () => {
    service.dangKy(() => false);

    const roiDuoc = await service.xinRoiTrang();

    expect(roiDuoc).toBeTrue();
    expect(service.dangHoi()).toBeFalse();
  });

  it('xinRoiTrang() còn thay đổi chưa lưu → mở hộp, treo tới khi xacNhanRoiDi()/huy()', async () => {
    service.dangKy(() => true);

    const cho = service.xinRoiTrang();
    expect(service.dangHoi()).toBeTrue();

    service.xacNhanRoiDi();
    const roiDuoc = await cho;

    expect(roiDuoc).toBeTrue();
    expect(service.dangHoi()).toBeFalse();
  });

  it('huy() → resolve false, thay đổi giữ nguyên (đường "Ở lại")', async () => {
    service.dangKy(() => true);

    const cho = service.xinRoiTrang();
    service.huy();
    const roiDuoc = await cho;

    expect(roiDuoc).toBeFalse();
    expect(service.dangHoi()).toBeFalse();
  });

  it('huyDangKy() bằng ĐÚNG hàm đã đăng ký → coThayDoiChuaLuu() về false', () => {
    const kiemTra = (): boolean => true;
    service.dangKy(kiemTra);
    expect(service.coThayDoiChuaLuu()).toBeTrue();

    service.huyDangKy(kiemTra);
    expect(service.coThayDoiChuaLuu()).toBeFalse();
  });

  it('huyDangKy() bằng hàm KHÁC hàm đang đăng ký → không xoá nhầm đăng ký hiện tại', () => {
    service.dangKy(() => true);
    service.huyDangKy(() => true);

    expect(service.coThayDoiChuaLuu()).toBeTrue();
  });

  it('lượt hỏi cũ còn treo mà mở lượt mới → lượt cũ tự resolve false (coi như "ở lại")', async () => {
    service.dangKy(() => true);
    const choLanDau = service.xinRoiTrang();
    service.xinRoiTrang(); // mở lượt hai — lượt đầu phải tự đóng lại, không treo mãi

    expect(await choLanDau).toBeFalse();
  });

  // "Không hỏi lần hai" khi đăng xuất THÀNH CÔNG do `unsavedChangesGuard` lo (phiên đã dọn → cho rời
  // ngay — unsaved-changes.guard.spec.ts). Ở tầng service, "Rời đi" chỉ là lời đồng ý: việc rời trang
  // chưa xảy ra thì lần xin kế tiếp vẫn phải hỏi.
  it('xacNhanRoiDi() mà việc rời trang chưa xảy ra → lần xinRoiTrang() kế tiếp VẪN hỏi', async () => {
    service.dangKy(() => true);
    const choLanMot = service.xinRoiTrang();
    service.xacNhanRoiDi();
    expect(await choLanMot).toBeTrue();

    const choLanHai = service.xinRoiTrang();

    expect(service.dangHoi()).toBeTrue();
    service.huy();
    expect(await choLanHai).toBeFalse();
  });

  it('roiTrangChacChan() → gỡ đăng ký: coThayDoiChuaLuu() về false, xin rời lần sau không mở hộp', async () => {
    service.dangKy(() => true);

    service.roiTrangChacChan();

    expect(service.coThayDoiChuaLuu()).toBeFalse();
    expect(await service.xinRoiTrang()).toBeTrue();
    expect(service.dangHoi()).toBeFalse();
  });

  it('roiTrangChacChan() khi còn lượt hỏi treo → lượt đó nhận false ("ở lại") và hộp đóng', async () => {
    service.dangKy(() => true);
    const cho = service.xinRoiTrang();

    service.roiTrangChacChan();

    expect(service.dangHoi()).toBeFalse();
    expect(await cho).toBeFalse();
  });
});

/**
 * `nghe` (handler `beforeunload`) là closure riêng trong constructor — không lộ qua API public.
 * Bắt callback THẬT đã đăng ký bằng cách spy `window.addEventListener` TRƯỚC khi service được
 * khởi tạo, rồi gọi callback đó trực tiếp với event giả.
 *
 * KHÔNG dùng `window.dispatchEvent(new Event('beforeunload'))`: ChromeHeadless coi đây là một
 * điều hướng thật ("Some of your tests did a full page reload!") và disconnect cả run — dispatch
 * thật, dù không preventDefault, vẫn không an toàn trong môi trường test.
 */
describe('UnsavedChangesService — đóng tab/tải lại (window "beforeunload")', () => {
  let service: UnsavedChangesService;
  let onBeforeUnload: (e: BeforeUnloadEvent) => void;

  beforeEach(() => {
    const addEventListenerSpy = spyOn(window, 'addEventListener').and.callThrough();

    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    service = TestBed.inject(UnsavedChangesService);

    const goiDangKy = addEventListenerSpy.calls.allArgs().find(([ten]) => ten === 'beforeunload');
    if (!goiDangKy) {
      throw new Error('UnsavedChangesService không đăng ký listener "beforeunload" trên window ở constructor');
    }
    onBeforeUnload = goiDangKy[1] as (e: BeforeUnloadEvent) => void;
  });

  function taoEventGia(): BeforeUnloadEvent {
    return { preventDefault: jasmine.createSpy('preventDefault'), returnValue: '' } as unknown as BeforeUnloadEvent;
  }

  it('còn thay đổi chưa lưu → handler gọi preventDefault() và đặt returnValue = "" (chặn đóng tab/tải lại)', () => {
    service.dangKy(() => true);
    const event = taoEventGia();

    onBeforeUnload(event);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(event.returnValue).toBe('');
  });

  // fe-routing-guard.md §4.1 "Áp cho cả điều hướng trong app lẫn đóng tab": "Rời đi" chưa phải là đã
  // rời — đăng xuất có thể hỏng (mất mạng, 5xx) và người dùng ở lại màn với đúng dữ liệu đang dở
  // (D6 §4). Lớp bảo vệ phải còn nguyên cho tới khi việc rời trang thật sự xảy ra.
  it('chọn "Rời đi" rồi việc rời trang KHÔNG xảy ra (đăng xuất hỏng) → đăng ký còn sống, đóng tab vẫn bị chặn', async () => {
    service.dangKy(() => true);
    const cho = service.xinRoiTrang();
    service.xacNhanRoiDi();
    expect(await cho).toBeTrue();

    expect(service.coThayDoiChuaLuu()).toBeTrue();
    const event = taoEventGia();
    onBeforeUnload(event);
    expect(event.preventDefault).toHaveBeenCalled();
  });

  it('không có thay đổi chưa lưu → handler KHÔNG gọi preventDefault() (không chặn đóng tab/tải lại)', () => {
    // Không dangKy() gì cả — coThayDoiChuaLuu() phải là false ngay từ đầu (mặc định của service).
    const event = taoEventGia();

    onBeforeUnload(event);

    expect(event.preventDefault).not.toHaveBeenCalled();
  });
});
