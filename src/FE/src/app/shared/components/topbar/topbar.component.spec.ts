import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { MenuComponent } from '../../ui/menu/menu.component';
import { TopbarComponent } from './topbar.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const PROVIDERS = [
  provideZonelessChangeDetection(),
  provideTranslateService({ loader: provideTranslateLoader(FakeTranslateLoader) }),
];

/** Design/Components/Topbar.md §Accessibility — khu tài khoản là MỘT nút mở menu nổi. */
describe('TopbarComponent — khu tài khoản', () => {
  let fixture: ComponentFixture<TopbarComponent>;
  const goc = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const nut = (): HTMLButtonElement =>
    goc().querySelector<HTMLButtonElement>('button.topbar__tai-khoan')!;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TopbarComponent],
      providers: PROVIDERS,
    }).compileComponents();
    fixture = TestBed.createComponent(TopbarComponent);
    fixture.componentRef.setInput('user', { fullName: 'An Bình', userName: 'ab', email: null });
    fixture.componentRef.setInput('tenantName', 'Đơn vị X');
    fixture.componentRef.setInput('menuItems', [
      { key: 'ho-so', label: 'Hồ sơ', icon: 'user' },
      { key: 'dang-xuat', label: 'Đăng xuất', icon: 'sign-out', danger: true },
    ]);
    document.body.appendChild(goc());
    fixture.detectChanges();
  });

  afterEach(() => goc().remove());

  it('đúng MỘT nút: avatar + tên + tên đơn vị; không còn nút mục inline', () => {
    expect(goc().querySelectorAll('button.topbar__tai-khoan').length).toBe(1);
    expect(nut().textContent).toContain('An Bình');
    expect(nut().textContent).toContain('Đơn vị X');
    expect(goc().querySelector('.topbar__muc')).toBeNull();
  });

  it('aria-haspopup="menu", aria-expanded phản ánh menu, nhãn đọc lên theo khoá khung.taiKhoan.moMenu', () => {
    expect(nut().getAttribute('aria-haspopup')).toBe('menu');
    expect(nut().getAttribute('aria-expanded')).toBe('false');
    expect(nut().getAttribute('aria-label')).toContain('khung.taiKhoan.moMenu');

    nut().click();
    fixture.detectChanges();
    expect(nut().getAttribute('aria-expanded')).toBe('true');
  });

  it('Escape đóng menu và trả focus về nút', async () => {
    nut().click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(document.querySelector('[role="menu"]')).not.toBeNull();

    const dangFocus = (document.activeElement as HTMLElement | null) ?? document.body;
    dangFocus.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', code: 'Escape', bubbles: true }),
    );
    // PrimeNG phát onHide sau hoạt ảnh đóng (CSS thật ở bản 21) — chờ tới khi nút báo đã đóng.
    for (let i = 0; i < 100 && nut().getAttribute('aria-expanded') !== 'false'; i++) {
      await new Promise((xong) => setTimeout(xong, 20));
      fixture.detectChanges();
    }

    expect(nut().getAttribute('aria-expanded')).toBe('false');
    expect(document.activeElement).toBe(nut());
  });

  it('chọn mục → phát menuItemSelected với key của mục', () => {
    const phat: string[] = [];
    fixture.componentInstance.menuItemSelected.subscribe((k) => phat.push(k));
    const menus = fixture.debugElement.queryAll(By.directive(MenuComponent));
    (menus[0].componentInstance as MenuComponent).selected.emit('ho-so');
    expect(phat).toEqual(['ho-so']);
  });

  it('không có input `title` — Topbar không mang tiêu đề trang', () => {
    expect('title' in fixture.componentInstance).toBeFalse();
    expect(goc().querySelector('.topbar__tieu-de')).toBeNull();
  });

  it('`scrolled` bật lớp bóng', () => {
    expect(goc().querySelector('.topbar--da-cuon')).toBeNull();
    fixture.componentRef.setInput('scrolled', true);
    fixture.detectChanges();
    expect(goc().querySelector('.topbar--da-cuon')).not.toBeNull();
  });
});

/** Design/Components/Topbar.md §Nút theme — đặc tả thi công (Q61). */
describe('TopbarComponent — nút theme', () => {
  let fixture: ComponentFixture<TopbarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TopbarComponent],
      providers: PROVIDERS,
    }).compileComponents();
    fixture = TestBed.createComponent(TopbarComponent);
  });

  function nut(): HTMLButtonElement {
    return (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '.topbar__theme button',
    )!;
  }

  function menuTheme(): MenuComponent {
    // Menu thứ hai trong DOM; menu thứ nhất là menu tài khoản.
    return fixture.debugElement.queryAll(By.directive(MenuComponent))[1]
      .componentInstance as MenuComponent;
  }

  it('là nút mở menu: aria-haspopup="menu", aria-expanded="false", KHÔNG aria-pressed', () => {
    fixture.detectChanges();

    expect(nut().getAttribute('aria-haspopup')).toBe('menu');
    expect(nut().getAttribute('aria-expanded')).toBe('false');
    expect(nut().hasAttribute('aria-pressed')).toBeFalse();
  });

  it('icon theo giá trị đang áp: light → sun, dark → moon, system → desktop', () => {
    const icon = (): HTMLElement => nut().querySelector('i')!;

    fixture.componentRef.setInput('theme', 'light');
    fixture.detectChanges();
    expect(icon().classList.contains('pi-sun')).toBeTrue();

    fixture.componentRef.setInput('theme', 'dark');
    fixture.detectChanges();
    expect(icon().classList.contains('pi-moon')).toBeTrue();

    fixture.componentRef.setInput('theme', 'system');
    fixture.detectChanges();
    expect(icon().classList.contains('pi-desktop')).toBeTrue();
  });

  it('Menu theme: ba mục Sáng · Tối · Theo hệ điều hành, selectedKey = theme đang áp', () => {
    fixture.componentRef.setInput('theme', 'dark');
    fixture.detectChanges();

    expect(
      menuTheme()
        .items()
        .map((m) => m.key),
    ).toEqual(['light', 'dark', 'system']);
    expect(menuTheme().selectedKey()).toBe('dark');
  });

  it('chọn một giá trị KHÁC giá trị đang áp → phát themeChangeRequested', () => {
    const phat: string[] = [];
    fixture.componentInstance.themeChangeRequested.subscribe((v) => phat.push(v));
    fixture.componentRef.setInput('theme', 'light');
    fixture.detectChanges();

    menuTheme().selected.emit('dark');

    expect(phat).toEqual(['dark']);
  });

  it('chọn lại giá trị đang áp → KHÔNG phát themeChangeRequested', () => {
    const phat: string[] = [];
    fixture.componentInstance.themeChangeRequested.subscribe((v) => phat.push(v));
    fixture.componentRef.setInput('theme', 'dark');
    fixture.detectChanges();

    menuTheme().selected.emit('dark');

    expect(phat).toEqual([]);
  });
});
