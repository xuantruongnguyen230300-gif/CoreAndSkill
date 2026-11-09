import { Component, ElementRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { providePrimeNG } from 'primeng/config';
import { Menu } from 'primeng/menu';

import { CORE_PRIME_PRESET } from '../../../core/theme/prime-preset';
import { MenuComponent } from './menu.component';

@Component({
  standalone: true,
  imports: [MenuComponent],
  template: `
    <button id="nut" type="button" (click)="mo($event)"><i id="icon"></i></button>
    <app-menu [open]="open()" [anchorEvent]="suKien()" [items]="[]" ariaLabel="menu thử" />
  `,
})
class HostComponent {
  readonly open = signal(false);
  readonly suKien = signal<Event | null>(null);
  mo(evt: Event): void {
    this.suKien.set(evt);
    this.open.set(true);
  }
}

/**
 * `Menu.show(event)` của PrimeNG neo overlay vào `event.currentTarget`, mà effect của `MenuComponent`
 * chạy SAU khi sự kiện đã phát xong — lúc đó trình duyệt đã đặt `currentTarget` về `null` và PrimeNG
 * ném `Cannot read properties of null (reading 'offsetHeight')` khi căn vị trí.
 */
describe('MenuComponent — neo overlay khi mở', () => {
  let fixture: ComponentFixture<HostComponent>;
  let show: jasmine.Spy;

  beforeEach(async () => {
    show = spyOn(Menu.prototype, 'show').and.stub();
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('bấm vào icon bên trong nút → PrimeNG nhận neo là chính <button>, không phải currentTarget rỗng của sự kiện đã phát xong', async () => {
    const root = fixture.nativeElement as HTMLElement;
    root.querySelector<HTMLElement>('#icon')?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();

    expect(show).toHaveBeenCalledTimes(1);
    const doiSo = show.calls.mostRecent().args[0] as { currentTarget: Element | null };
    expect(doiSo.currentTarget).toBe(root.querySelector('#nut'));
  });
});

describe('MenuComponent — selectedKey (menu chọn một giá trị)', () => {
  let fixture: ComponentFixture<MenuComponent>;
  const ITEMS = [
    { key: 'light', label: 'Sáng' },
    { key: 'dark', label: 'Tối' },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MenuComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(MenuComponent);
    fixture.componentRef.setInput('ariaLabel', 'Giao diện');
    fixture.componentRef.setInput('items', ITEMS);
    fixture.componentRef.setInput('selectedKey', 'dark');
    fixture.detectChanges();
  });

  it('chọn lại mục đang aria-checked → không phát selected; mục khác → phát', () => {
    const phat: string[] = [];
    fixture.componentInstance.selected.subscribe((k) => phat.push(k));
    const model = (
      fixture.componentInstance as unknown as { model(): { command: () => void }[] }
    ).model();

    model[1].command();
    model[0].command();

    expect(phat).toEqual(['light']);
  });

  it('khi overlay hiện: mọi mục menuitemradio, đúng mục mang aria-checked=true và nhận focus ảo', () => {
    const li = ITEMS.map((_, i) => {
      const e = document.createElement('li');
      e.setAttribute('data-pc-section', 'item');
      e.id = `m_${i}`;
      return e;
    });
    const goc = document.createElement('div');
    li.forEach((e) => goc.appendChild(e));
    const prime = (fixture.componentInstance as unknown as { primeMenu(): Menu }).primeMenu();
    spyOn(prime, 'containerViewChild').and.returnValue(new ElementRef(goc));

    (fixture.componentInstance as unknown as { onShow(): void }).onShow();

    expect(li.map((e) => e.getAttribute('role'))).toEqual(['menuitemradio', 'menuitemradio']);
    expect(li.map((e) => e.getAttribute('aria-checked'))).toEqual(['false', 'true']);
    expect(prime.focusedOptionIndex()).toBe('m_1');
  });
});

@Component({
  standalone: true,
  imports: [MenuComponent],
  template: `
    <button id="nut-that" type="button" (click)="mo($event)">Mở</button>
    <app-menu
      [open]="open()"
      [anchorEvent]="suKien()"
      [items]="items"
      [selectedKey]="'dark'"
      ariaLabel="Giao diện"
      (openChange)="open.set($event)"
    />
  `,
})
class HostDomThatComponent {
  readonly items = [
    { key: 'light', label: 'Sáng' },
    { key: 'dark', label: 'Tối' },
  ];
  readonly open = signal(false);
  readonly suKien = signal<Event | null>(null);
  mo(evt: Event): void {
    this.suKien.set(evt);
    this.open.set(true);
  }
}

/**
 * Ca trên dùng `<li>` tự dựng nên không thấy PrimeNG đổi DOM của chính nó (bản 21 đổi
 * `data-pc-section` của `<li role="menuitem">` từ `menuitem` sang `item`). Ca này mở menu THẬT và đọc
 * DOM thật.
 */
describe('MenuComponent — menu chọn giá trị trên DOM PrimeNG thật', () => {
  it('mở menu thật → mọi mục là menuitemradio, đúng mục đang chọn mang aria-checked=true', async () => {
    await TestBed.configureTestingModule({
      imports: [HostDomThatComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    const fixture = TestBed.createComponent(HostDomThatComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('#nut-that')!.click();
    for (let i = 0; i < 100 && !document.querySelector('li[role="menuitemradio"]'); i++) {
      await new Promise((xong) => setTimeout(xong, 20));
      fixture.detectChanges();
    }

    const dsLi = Array.from(document.querySelectorAll<HTMLElement>('li[role="menuitemradio"]'));
    expect(dsLi.map((li) => li.getAttribute('aria-checked'))).toEqual(['false', 'true']);
    expect(document.querySelectorAll('li[role="menuitem"]').length).toBe(0);
    fixture.destroy();
  });
});

/**
 * Luật F62 / ADR-0123 quyết định 4 và 5: bung và thu MỌI lớp nổi neo đi bằng token QUA CSS, qua MỘT
 * khối đè trên lớp pha chung `p-anchored-overlay` ở `_thu-vien.scss` — Menu, Autocomplete, DatePicker
 * và ô số dòng của Paginator cùng dùng nó. Spec mở menu thật (đại diện cho cả nhóm) và đo phần tử
 * đang mang lớp pha — bắt được ba ca mà không cổng script nào thấy: thư viện đổi tên lớp pha ở một
 * lượt nâng (khối đè thôi khớp, mọi lớp nổi neo chạy lại 300ms của thư viện), một lần truyền
 * `duration` bằng số (style nội tuyến, token bị bỏ qua), và một `transition` dài hơn `animation` trên
 * chính phần tử (thư viện định thời theo giá trị lớn nhất).
 *
 * Đo ngay trong lúc lớp pha còn trên phần tử — `MutationObserver` đọc style ở microtask ngay sau khi
 * lớp được gắn, không qua `setTimeout` đoán giờ — nên không có cửa sổ thời gian để chập chờn. Bật
 * `prefers-reduced-motion` thì thư viện bỏ qua pha (`safe`), spec không có gì để đo.
 */
describe('MenuComponent — chuyển động lớp nổi neo đi bằng token qua CSS (F62)', () => {
  let fixture: ComponentFixture<HostDomThatComponent>;

  interface MauPha {
    ten: string;
    thoiLuong: string;
    duongCong: string;
    transitionDuration: string;
    transitionDelay: string;
    noiTuyen: string;
  }

  /** Chờ phần tử khớp `chon`, rồi đọc style đã tính NGAY lúc đó. */
  function batPha(chon: string): Promise<MauPha> {
    return new Promise((resolve, reject) => {
      const doc = (): boolean => {
        const el = document.querySelector<HTMLElement>(chon);
        if (!el) {
          return false;
        }
        const t = getComputedStyle(el);
        resolve({
          ten: t.animationName,
          thoiLuong: t.animationDuration,
          duongCong: t.animationTimingFunction,
          transitionDuration: t.transitionDuration,
          transitionDelay: t.transitionDelay,
          noiTuyen: el.style.animationDuration + el.style.transitionDuration,
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
    await TestBed.configureTestingModule({
      imports: [HostDomThatComponent],
      providers: [
        provideZonelessChangeDetection(),
        providePrimeNG({
          theme: {
            preset: CORE_PRIME_PRESET,
            options: { darkModeSelector: false, cssLayer: true },
          },
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HostDomThatComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => fixture.destroy());

  async function moMenu(): Promise<void> {
    (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('#nut-that')!.click();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
  }

  it('pha bung: --dur-base, --ease-decelerate', async () => {
    const pha = batPha('.p-anchored-overlay-enter-active');
    await moMenu();
    kiemMau(await pha, {
      ten: 'p-animate-anchored-overlay-enter',
      thoiLuong: '--dur-base',
      duongCong: '--ease-decelerate',
    });
  });

  it('pha thu: --dur-base, --ease-accelerate', async () => {
    await moMenu();
    await choHetPha('.p-anchored-overlay-enter-active');
    const pha = batPha('.p-anchored-overlay-leave-active');
    fixture.componentInstance.open.set(false);
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    kiemMau(await pha, {
      ten: 'p-animate-anchored-overlay-leave',
      thoiLuong: '--dur-base',
      duongCong: '--ease-accelerate',
    });
  });
});

@Component({
  standalone: true,
  imports: [MenuComponent],
  template: `
    <div id="khung-cuon" style="position: relative; overflow: auto; height: 60px">
      <button id="nut" type="button" (click)="mo($event)"><i id="icon"></i></button>
      <app-menu
        [open]="open()"
        [anchorEvent]="suKien()"
        [items]="[{ key: 'a', label: 'Mục A' }]"
        ariaLabel="menu thử"
      />
    </div>
  `,
})
class HostTrongKhungCuonComponent {
  readonly open = signal(false);
  readonly suKien = signal<Event | null>(null);
  mo(evt: Event): void {
    this.suKien.set(evt);
    this.open.set(true);
  }
}

/**
 * Lớp nổi neo của Menu nằm ngoài mọi khung cuộn/định vị của trang. `Menu` căn vị trí bằng toạ độ
 * trong TÀI LIỆU (`absolutePosition`) và gắn bộ nghe cuộn trên các cha cuộn được của nút neo; để tại chỗ
 * (`appendTo` mặc định `self`) thì lớp nổi nằm trong khung cuộn, toạ độ lệch theo cha định vị (ô `sticky`
 * của bảng), `focus()` danh sách cuộn khung, và chính cú cuộn đó đóng menu — thấy rõ khi giảm chuyển động,
 * vì lúc ấy không còn hoạt ảnh ra che việc menu biến mất.
 */
describe('MenuComponent — lớp nổi gắn vào body, không nằm trong khung cuộn của trang', () => {
  it('mở menu từ nút trong khung cuộn: .p-menu là con của body, không nằm trong khung', async () => {
    await TestBed.configureTestingModule({
      imports: [HostTrongKhungCuonComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    const fixture = TestBed.createComponent(HostTrongKhungCuonComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const root = fixture.nativeElement as HTMLElement;
    root.querySelector<HTMLElement>('#icon')?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    await new Promise<void>((xong, loi) => {
      const han = Date.now() + 2000;
      const cho = (): void => {
        if (document.querySelector('.p-menu')) xong();
        else if (Date.now() > han) loi(new Error('.p-menu không xuất hiện'));
        else setTimeout(cho, 20);
      };
      cho();
    });

    const menu = document.querySelector('.p-menu') as HTMLElement;
    expect(menu.parentElement).toBe(document.body);
    expect(root.querySelector('#khung-cuon')?.contains(menu)).toBeFalse();
    fixture.destroy();
  });
});
