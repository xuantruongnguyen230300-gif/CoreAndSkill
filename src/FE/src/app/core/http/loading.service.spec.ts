import { LoadingService } from './loading.service';

describe('LoadingService', () => {
  let service: LoadingService;

  beforeEach(() => {
    service = new LoadingService();
  });

  it('hienThi là false khi chưa có request nào chạy', () => {
    expect(service.hienThi()).toBeFalse();
  });

  it('hienThi là true khi có request đang chạy, và đếm nhiều request song song', () => {
    service.batDau();
    service.batDau();
    expect(service.hienThi()).toBeTrue();

    service.ketThuc();
    expect(service.hienThi()).toBeTrue(); // còn một request khác đang chạy

    service.ketThuc();
    expect(service.hienThi()).toBeFalse();
  });

  it('không bao giờ đếm âm — gọi ketThuc thừa không làm hienThi kẹt ở true', () => {
    service.ketThuc();
    service.ketThuc();
    expect(service.hienThi()).toBeFalse();

    service.batDau();
    expect(service.hienThi()).toBeTrue();
  });
});
