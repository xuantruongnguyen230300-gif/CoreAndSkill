import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService, ToastMessageOptions } from 'primeng/api';

import { ToastComponent } from './toast.component';
import { ToastService } from '../../../core/toast/toast.service';

describe('ToastComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ToastComponent],
      providers: [provideZonelessChangeDetection(), provideNoopAnimations()],
    });
  });

  it('đẩy MỖI thông báo mới của ToastService sang MessageService của PrimeNG', async () => {
    const fixture = TestBed.createComponent(ToastComponent);
    const messageService = fixture.debugElement.injector.get(MessageService);
    const addSpy = spyOn(messageService, 'add').and.callThrough();
    fixture.detectChanges();

    const toast = TestBed.inject(ToastService);
    toast.loi('Đã có lỗi xảy ra.', 'trace-42');
    await fixture.whenStable();

    expect(addSpy).toHaveBeenCalledWith(
      jasmine.objectContaining({
        severity: 'error',
        summary: 'Đã có lỗi xảy ra.',
        detail: 'traceId: trace-42',
      }),
    );
  });

  it('V-01 — hai thông báo liên tiếp trước khi effect chạy → CẢ HAI tới MessageService, không mất cái đầu', async () => {
    const fixture = TestBed.createComponent(ToastComponent);
    const messageService = fixture.debugElement.injector.get(MessageService);
    const addSpy = spyOn(messageService, 'add').and.callThrough();
    fixture.detectChanges();
    await fixture.whenStable();

    const toast = TestBed.inject(ToastService);
    toast.loi('Lỗi thứ nhất.');
    toast.loi('Lỗi thứ hai.');
    await fixture.whenStable();

    expect(addSpy.calls.allArgs().map(([m]) => m.summary)).toEqual([
      'Lỗi thứ nhất.',
      'Lỗi thứ hai.',
    ]);
  });

  // Design/Components/Toast.md §Biến thể. Hai khoản, và khoản thứ hai là khoản nguy hiểm:
  // thời gian tự tắt tăng dần theo mức nghiêm trọng, còn vai LỖI thì không tự tắt bao giờ.
  describe('thời gian tự tắt theo vai (§Biến thể)', () => {
    async function dayMotThongBao(day: (t: ToastService) => void): Promise<ToastMessageOptions> {
      const fixture = TestBed.createComponent(ToastComponent);
      const messageService = fixture.debugElement.injector.get(MessageService);
      const addSpy = spyOn(messageService, 'add').and.callThrough();
      fixture.detectChanges();

      day(TestBed.inject(ToastService));
      await fixture.whenStable();

      expect(addSpy).toHaveBeenCalledTimes(1);
      return addSpy.calls.mostRecent().args[0];
    }

    it('🛑 toast LỖI không tự tắt — sticky bật và KHÔNG có life', async () => {
      const msg = await dayMotThongBao((t) => t.loi('Lưu thất bại.'));

      // `sticky` là đường duy nhất tắt đồng hồ của primeng/toast; thiếu nó thì `life` rỗng rơi về
      // mặc định 3000 của thư viện và người dùng bỏ lỡ lỗi, tin là thao tác đã thành công.
      expect(msg.sticky).toBeTrue();
      expect(msg.life).toBeUndefined();
    });

    it('toast thành công tự tắt sau 4 giây, sticky tắt', async () => {
      const msg = await dayMotThongBao((t) => t.thanhCong('Đã lưu.'));

      expect(msg.life).toBe(4_000);
      expect(msg.sticky).toBeFalse();
    });

    it('toast thông tin tự tắt sau 5 giây, sticky tắt', async () => {
      const msg = await dayMotThongBao((t) => t.thongTin('Đang đồng bộ.'));

      expect(msg.life).toBe(5_000);
      expect(msg.sticky).toBeFalse();
    });

    it('toast cảnh báo tự tắt sau 8 giây, sticky tắt', async () => {
      const msg = await dayMotThongBao((t) => t.canhBao('Phiên sắp hết hạn.'));

      expect(msg.life).toBe(8_000);
      expect(msg.sticky).toBeFalse();
    });

    it('ba vai tự tắt xếp TĂNG DẦN theo mức nghiêm trọng, và vai lỗi đứng ngoài thang đó', async () => {
      const fixture = TestBed.createComponent(ToastComponent);
      const messageService = fixture.debugElement.injector.get(MessageService);
      const addSpy = spyOn(messageService, 'add').and.callThrough();
      fixture.detectChanges();

      const toast = TestBed.inject(ToastService);
      toast.thanhCong('a');
      toast.thongTin('b');
      toast.canhBao('c');
      toast.loi('d');
      await fixture.whenStable();

      const [thanhCong, thongTin, canhBao, loi] = addSpy.calls
        .allArgs()
        .map(([m]) => m as ToastMessageOptions);

      // So bằng bất đẳng thức chứ không chỉ bằng ba con số: khoá luôn cả LÝ DO của thang
      // (§Biến thể — "Thời gian tăng dần theo mức nghiêm trọng").
      expect(thanhCong.life!).toBeLessThan(thongTin.life!);
      expect(thongTin.life!).toBeLessThan(canhBao.life!);
      expect(loi.life).toBeUndefined();
      expect(loi.sticky).toBeTrue();
    });
  });

  it('thông báo không kèm traceId → detail là undefined, không phải chuỗi "traceId: null"', async () => {
    const fixture = TestBed.createComponent(ToastComponent);
    const messageService = fixture.debugElement.injector.get(MessageService);
    const addSpy = spyOn(messageService, 'add').and.callThrough();
    fixture.detectChanges();

    const toast = TestBed.inject(ToastService);
    toast.thanhCong('Lưu thành công.');
    await fixture.whenStable();

    expect(addSpy).toHaveBeenCalledWith(
      jasmine.objectContaining({
        severity: 'success',
        summary: 'Lưu thành công.',
        detail: undefined,
      }),
    );
  });
});
