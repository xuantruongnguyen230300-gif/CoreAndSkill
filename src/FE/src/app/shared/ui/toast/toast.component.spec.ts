import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MessageService, ToastMessageOptions } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';

import { CORE_PRIME_PRESET } from '../../../core/theme/prime-preset';
import { ToastComponent } from './toast.component';
import { ToastService } from '../../../core/toast/toast.service';

// Nơi ghép mẫu nút hành động như `app.html`. Dùng <button> trần: shared/ui/ không import được Button,
// và ca kiểm này chỉ khoá đường nối (thứ tự Tab, focus, bấm một lần), không khoá hình thức nút.
@Component({
  selector: 'app-toast-host',
  standalone: true,
  imports: [ToastComponent],
  template: `
    <ng-template #nut let-hanhDong let-id="id" let-daBam="daBam" let-bam="bam">
      <button type="button" [disabled]="daBam" (click)="bam(id, hanhDong)">
        {{ hanhDong.nhan }}
      </button>
    </ng-template>
    <app-toast [nutHanhDong]="nut" />
  `,
})
class ToastHostComponent {}

describe('ToastComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ToastComponent, ToastHostComponent],
      providers: [provideZonelessChangeDetection()],
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

  // Toast.md ~:144-147: toast không cướp focus, kể cả toast lỗi (primeng gắn `autofocus` lên nút đóng).
  it('toast lỗi xuất hiện → focus vẫn ở phần tử người dùng đang giữ, không nhảy vào nút đóng', async () => {
    const o = document.createElement('input');
    document.body.appendChild(o);
    o.focus();
    const fixture = TestBed.createComponent(ToastComponent);
    document.body.appendChild(fixture.nativeElement as HTMLElement);
    fixture.detectChanges();
    await fixture.whenStable();

    TestBed.inject(ToastService).loi('Đã có lỗi.', 'trace-f');
    await fixture.whenStable();
    fixture.detectChanges();
    await new Promise<void>((r) => requestAnimationFrame(() => requestAnimationFrame(() => r())));

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('button[autofocus]')).toBeNull();
    expect(document.activeElement).toBe(o);
    o.remove();
    root.remove();
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
  // Toast.md §Accessibility, hàng "Toast có nút hành động" (chốt 2026-10-08).
  describe('nút hành động', () => {
    async function hienToastCoHanhDong(chay: () => void) {
      const nguoiDangGiu = document.createElement('input');
      document.body.appendChild(nguoiDangGiu);
      nguoiDangGiu.focus();
      const fixture = TestBed.createComponent(ToastHostComponent);
      document.body.appendChild(fixture.nativeElement as HTMLElement);
      fixture.detectChanges();
      await fixture.whenStable();

      TestBed.inject(ToastService).loi('Không tải được.', null, { nhan: 'Tải lại trang', chay });
      await fixture.whenStable();
      fixture.detectChanges();
      await new Promise<void>((r) => requestAnimationFrame(() => requestAnimationFrame(() => r())));
      const root = fixture.nativeElement as HTMLElement;
      return {
        fixture,
        root,
        nguoiDangGiu,
        don: () => {
          nguoiDangGiu.remove();
          root.remove();
        },
      };
    }

    it('toast có hanhDong vẽ nút mang nhãn đã dịch, đứng TRƯỚC nút đóng trong thứ tự Tab', async () => {
      const { root, don } = await hienToastCoHanhDong(() => undefined);

      const nut = root.querySelector<HTMLButtonElement>('.toast-hanh-dong button');
      expect(nut?.textContent?.trim()).toBe('Tải lại trang');
      const nuts = Array.from(root.querySelectorAll('.p-toast-message button'));
      expect(nuts.length).toBe(2);
      expect(nuts[0]).toBe(nut as Element);
      expect(nuts[1].getAttribute('aria-label')).not.toBeNull();
      don();
    });

    it('KHÔNG cướp focus: focus ở lại phần tử người dùng đang giữ, không vào nút hành động', async () => {
      const { nguoiDangGiu, don } = await hienToastCoHanhDong(() => undefined);

      expect(document.activeElement).toBe(nguoiDangGiu);
      don();
    });

    it('bấm nút → hành động chạy một lần, nút disabled ngay, bấm lần hai không chạy lại', async () => {
      const chay = jasmine.createSpy('chay');
      const { fixture, root, don } = await hienToastCoHanhDong(chay);
      const nut = root.querySelector<HTMLButtonElement>('.toast-hanh-dong button')!;

      nut.click();
      fixture.detectChanges();
      await fixture.whenStable();
      nut.click();

      expect(chay).toHaveBeenCalledTimes(1);
      expect(nut.disabled).toBeTrue();
      don();
    });

    it('toast không có hanhDong → không vẽ nút hành động', async () => {
      const fixture = TestBed.createComponent(ToastHostComponent);
      document.body.appendChild(fixture.nativeElement as HTMLElement);
      fixture.detectChanges();
      await fixture.whenStable();

      TestBed.inject(ToastService).loi('Lỗi thường.');
      await fixture.whenStable();
      fixture.detectChanges();

      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('.p-toast-message')).not.toBeNull();
      expect(root.querySelector('.toast-hanh-dong')).toBeNull();
      root.remove();
    });

    it('toast có hanhDong vẫn là vai lỗi: sticky bật, không life', async () => {
      const fixture = TestBed.createComponent(ToastComponent);
      const messageService = fixture.debugElement.injector.get(MessageService);
      const addSpy = spyOn(messageService, 'add').and.callThrough();
      fixture.detectChanges();

      TestBed.inject(ToastService).loi('x', null, { nhan: 'Tải lại trang', chay: () => undefined });
      await fixture.whenStable();

      const m = addSpy.calls.mostRecent().args[0];
      expect(m.sticky).toBeTrue();
      expect(m.life).toBeUndefined();
    });
  });

  // Design/Components/Toast.md §Accessibility — vùng thông báo, `role`, ngăn chứa tồn tại sẵn.
  // Chỉ đo được phần DOM; việc trình đọc màn hình có đọc hai lần `warn` / `error` trong vùng `polite`
  // của phần tử chủ hay không cần đo bằng trình đọc màn hình thật.
  describe('vùng thông báo theo vai (pass-through)', () => {
    async function dung() {
      const fixture = TestBed.createComponent(ToastComponent);
      document.body.appendChild(fixture.nativeElement as HTMLElement);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();
      return { fixture, root: fixture.nativeElement as HTMLElement };
    }

    async function hien(day: (t: ToastService) => void) {
      const { fixture, root } = await dung();
      day(TestBed.inject(ToastService));
      await fixture.whenStable();
      TestBed.tick();
      fixture.detectChanges();
      await fixture.whenStable();
      const thongBao = root.querySelector<HTMLElement>('.p-toast-message');
      expect(thongBao).withContext('toast phải được vẽ').not.toBeNull();
      return { root, thongBao: thongBao! };
    }

    it('p-toast mang aria-live="polite" TRƯỚC toast đầu tiên (vùng live có mặt từ đầu)', async () => {
      const { root } = await dung();

      expect(root.querySelector('.p-toast-message')).toBeNull();
      expect(root.querySelector('p-toast')?.getAttribute('aria-live')).toBe('polite');
      root.remove();
    });

    it('success → role="status" + aria-live="polite"', async () => {
      const { root, thongBao } = await hien((t) => t.thanhCong('Đã lưu.'));

      expect(thongBao.getAttribute('role')).toBe('status');
      expect(thongBao.getAttribute('aria-live')).toBe('polite');
      root.remove();
    });

    it('info → role="status" + aria-live="polite"', async () => {
      const { root, thongBao } = await hien((t) => t.thongTin('Thông báo.'));

      expect(thongBao.getAttribute('role')).toBe('status');
      expect(thongBao.getAttribute('aria-live')).toBe('polite');
      root.remove();
    });

    it('warn → GIỮ role="alert" + aria-live="assertive" của thư viện', async () => {
      const { root, thongBao } = await hien((t) => t.canhBao('Cẩn thận.'));

      expect(thongBao.getAttribute('role')).toBe('alert');
      expect(thongBao.getAttribute('aria-live')).toBe('assertive');
      root.remove();
    });

    it('error → GIỮ role="alert" + aria-live="assertive" của thư viện', async () => {
      const { root, thongBao } = await hien((t) => t.loi('Lưu thất bại.'));

      expect(thongBao.getAttribute('role')).toBe('alert');
      expect(thongBao.getAttribute('aria-live')).toBe('assertive');
      root.remove();
    });
  });
});

/**
 * Luật F62 / ADR-0123 quyết định 5: thời lượng và đường cong chuyển động của toast đi bằng token QUA
 * CSS, và giữ bất đối xứng của Toast.md — vào chậm (`--dur-slow`, `--ease-decelerate`), ra nhanh
 * (`--dur-base`, `--ease-accelerate`). Spec đẩy một toast thật và đo phần tử đang mang lớp pha — bắt
 * được ba ca mà không cổng script nào thấy: thư viện đổi tên lớp pha ở một lượt nâng (khối đè ở
 * `_thu-vien.scss` thôi khớp), một lần truyền `duration` bằng số (thư viện ghi style nội tuyến, token
 * bị bỏ qua), và một `transition` dài hơn `animation` (thư viện định thời theo giá trị lớn nhất) —
 * ca cuối đo cả phần tử con `.p-toast-message-content` mà thư viện cho thu padding bằng `transition`
 * ở pha ra.
 *
 * Đo ngay trong lúc lớp pha còn trên phần tử — `MutationObserver` đọc style ở microtask ngay sau khi
 * lớp được gắn, không qua `setTimeout` đoán giờ — nên không có cửa sổ thời gian để chập chờn. Bật
 * `prefers-reduced-motion` thì thư viện bỏ qua pha (`safe`), spec không có gì để đo.
 */
describe('ToastComponent — chuyển động đi bằng token qua CSS (F62)', () => {
  let fixture: ComponentFixture<ToastComponent>;

  interface MauPha {
    ten: string;
    thoiLuong: string;
    duongCong: string;
    transitionDuration: string;
    transitionDelay: string;
    noiTuyen: string;
    con: { transitionDuration: string; duongCong: string } | null;
  }

  /** Chờ phần tử khớp `chon`, rồi đọc style đã tính NGAY lúc đó (cả phần tử con `chonCon` nếu có). */
  function batPha(chon: string, chonCon?: string): Promise<MauPha> {
    return new Promise((resolve, reject) => {
      const doc = (): boolean => {
        const el = document.querySelector<HTMLElement>(chon);
        if (!el) {
          return false;
        }
        const t = getComputedStyle(el);
        const con = chonCon ? el.querySelector<HTMLElement>(chonCon) : null;
        const tc = con ? getComputedStyle(con) : null;
        resolve({
          ten: t.animationName,
          thoiLuong: t.animationDuration,
          duongCong: t.animationTimingFunction,
          transitionDuration: t.transitionDuration,
          transitionDelay: t.transitionDelay,
          noiTuyen: el.style.animationDuration + el.style.transitionDuration,
          con: tc
            ? { transitionDuration: tc.transitionDuration, duongCong: tc.transitionTimingFunction }
            : null,
        });
        return true;
      };
      if (doc()) {
        return;
      }
      const quanSat = new MutationObserver(() => {
        if (doc()) {
          quanSat.disconnect();
          clearTimeout(hen);
        }
      });
      quanSat.observe(document.body, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeFilter: ['class'],
      });
      const hen = setTimeout(() => {
        quanSat.disconnect();
        reject(new Error(`Không thấy phần tử "${chon}" trong 2 giây — lớp pha đổi tên?`));
      }, 2000);
    });
  }

  /** Giá trị đã tính của một token, qua cùng bộ máy tính với phần tử đo (không chép số vào spec). */
  function giaTriToken(thuocTinh: string, token: string): string {
    expect(getComputedStyle(document.documentElement).getPropertyValue(token).trim())
      .withContext(`token ${token} chưa khai`)
      .not.toBe('');
    const dau = document.createElement('div');
    dau.style.setProperty(thuocTinh, `var(${token})`);
    document.body.appendChild(dau);
    const v = getComputedStyle(dau).getPropertyValue(thuocTinh);
    dau.remove();
    return v;
  }

  function mili(dsach: string): number[] {
    return dsach.split(',').map((x) => {
      const so = parseFloat(x);
      return x.trim().endsWith('ms') ? so : so * 1000;
    });
  }

  async function choHetPha(chon: string): Promise<void> {
    const batDau = Date.now();
    while (document.querySelector(chon) && Date.now() - batDau < 2000) {
      await new Promise((r) => setTimeout(r, 20));
    }
  }

  function kiemMau(
    mau: MauPha,
    token: { ten: string; thoiLuong: string; duongCong: string },
  ): void {
    expect(mau.ten).withContext('keyframes của thư viện phải còn nguyên').toBe(token.ten);
    expect(mau.thoiLuong).toBe(giaTriToken('animation-duration', token.thoiLuong));
    expect(mau.duongCong).toBe(giaTriToken('animation-timing-function', token.duongCong));
    expect(mau.noiTuyen)
      .withContext('thư viện ghi thời lượng nội tuyến — có ai truyền `duration` bằng số?')
      .toBe('');
    // Thư viện kết thúc pha theo thời lượng LỚN NHẤT: một transition dài hơn animation giành quyền định thời.
    const animation = Math.max(...mili(mau.thoiLuong));
    const transition = Math.max(
      ...mili(mau.transitionDuration).map((d, i) => d + (mili(mau.transitionDelay)[i] ?? 0)),
    );
    expect(transition)
      .withContext('có transition dài hơn animation trên chính phần tử chuyển động')
      .toBeLessThanOrEqual(animation);
  }

  beforeEach(async () => {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      pending('prefers-reduced-motion bật — thư viện bỏ qua pha, không có gì để đo');
    }
    TestBed.configureTestingModule({
      imports: [ToastComponent],
      providers: [
        provideZonelessChangeDetection(),
        providePrimeNG({
          theme: {
            preset: CORE_PRIME_PRESET,
            options: { darkModeSelector: false, cssLayer: true },
          },
        }),
      ],
    });
    fixture = TestBed.createComponent(ToastComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  async function dayThongBao(): Promise<void> {
    TestBed.inject(ToastService).thanhCong('Đã lưu.');
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
  }

  it('pha vào: --dur-slow, --ease-decelerate', async () => {
    const pha = batPha('p-toast .p-toast-message-enter-active');
    await dayThongBao();
    kiemMau(await pha, {
      ten: 'p-animate-toast-enter',
      thoiLuong: '--dur-slow',
      duongCong: '--ease-decelerate',
    });
  });

  it('pha ra: --dur-base, --ease-accelerate — kể cả transition padding của phần tử con', async () => {
    await dayThongBao();
    await choHetPha('p-toast .p-toast-message-enter-active');
    const pha = batPha('p-toast .p-toast-message-leave-active', '.p-toast-message-content');
    fixture.debugElement.injector.get(MessageService).clear();
    fixture.detectChanges();
    await fixture.whenStable();
    const mau = await pha;
    kiemMau(mau, {
      ten: 'p-animate-toast-leave',
      thoiLuong: '--dur-base',
      duongCong: '--ease-accelerate',
    });
    // Phần tử con thu padding bằng `transition` — phải mang cùng token với pha ra, nếu không nó
    // giành quyền định thời (dài hơn) hoặc chạy lệch nhịp với phần còn lại (ngắn hơn).
    expect(mau.con).not.toBeNull();
    expect(mau.con!.transitionDuration).toBe(giaTriToken('transition-duration', '--dur-base'));
    expect(mau.con!.duongCong).toBe(giaTriToken('transition-timing-function', '--ease-accelerate'));
  });
});
