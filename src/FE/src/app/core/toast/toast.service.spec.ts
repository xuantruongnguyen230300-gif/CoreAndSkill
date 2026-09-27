import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    service = new ToastService();
  });

  it('latest() là null khi chưa có thông báo nào', () => {
    expect(service.latest()).toBeNull();
  });

  it('loi() đẩy một thông báo severity error mang traceId', () => {
    service.loi('Đã có lỗi.', 'trace-1');

    const m = service.latest();
    expect(m?.severity).toBe('error');
    expect(m?.summary).toBe('Đã có lỗi.');
    expect(m?.traceId).toBe('trace-1');
  });

  it('mỗi lần push tăng id — hai thông báo TRÙNG NỘI DUNG vẫn là hai lần hiện khác nhau', () => {
    service.loi('Trùng nội dung.', null);
    const first = service.latest();

    service.loi('Trùng nội dung.', null);
    const second = service.latest();

    expect(second?.id).not.toBe(first?.id);
  });

  it('thanhCong() đẩy severity success, không mang traceId', () => {
    service.thanhCong('Lưu thành công.');
    expect(service.latest()?.severity).toBe('success');
    expect(service.latest()?.traceId).toBeNull();
  });

  it('hai thông báo liên tiếp → cả hai nằm trong hàng đợi theo thứ tự; layHet() lấy hết rồi xoá', () => {
    service.loi('Một.');
    service.thanhCong('Hai.');

    expect(service.hangDoi().map((m) => m.summary)).toEqual(['Một.', 'Hai.']);
    expect(service.layHet().map((m) => m.summary)).toEqual(['Một.', 'Hai.']);
    expect(service.hangDoi()).toEqual([]);
    expect(service.layHet()).toEqual([]);
  });
});
