import {
  ChangeDetectionStrategy,
  Component,
  provideZonelessChangeDetection,
  signal,
} from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Router, RouterLink, provideRouter } from '@angular/router';

import { ButtonComponent } from './button.component';

describe('ButtonComponent', () => {
  let fixture: ComponentFixture<ButtonComponent>;
  let component: ButtonComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ButtonComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(ButtonComponent);
    component = fixture.componentInstance;
  });

  it('phát clicked khi bấm bình thường', () => {
    fixture.detectChanges();
    const spy = jasmine.createSpy();
    component.clicked.subscribe(spy);

    (fixture.nativeElement as HTMLElement)
      .querySelector('button')!
      .dispatchEvent(new Event('click'));

    expect(spy).toHaveBeenCalled();
  });

  it('KHÔNG phát clicked khi loading — nút tự vô hiệu hoá', () => {
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();
    const spy = jasmine.createSpy();
    component.clicked.subscribe(spy);

    const nut = (fixture.nativeElement as HTMLElement).querySelector('button')!;
    expect(nut.hasAttribute('disabled')).toBeTrue();
    nut.dispatchEvent(new Event('click'));

    expect(spy).not.toHaveBeenCalled();
  });

  it('KHÔNG phát clicked khi disabled', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    const spy = jasmine.createSpy();
    component.clicked.subscribe(spy);

    (fixture.nativeElement as HTMLElement)
      .querySelector('button')!
      .dispatchEvent(new Event('click'));

    expect(spy).not.toHaveBeenCalled();
  });

  // Nơi gọi truyền TÊN icon không kèm tiền tố (`icon="refresh"`) — cùng quy ước với IconButton,
  // Badge, EmptyState. Thiếu tiền tố thì lớp thành `pi refresh`, primeicons không vẽ gì.
  for (const viTri of ['leading', 'trailing'] as const) {
    it(`icon ${viTri}: icon="refresh" → lớp pi pi-refresh, không có lớp trần "refresh"`, () => {
      fixture.componentRef.setInput('icon', 'refresh');
      fixture.componentRef.setInput('iconPosition', viTri);
      fixture.detectChanges();

      const i = (fixture.nativeElement as HTMLElement).querySelector('button i');
      expect(i?.classList.contains('pi')).toBeTrue();
      expect(i?.classList.contains('pi-refresh')).toBeTrue();
      expect(i?.classList.contains('refresh')).toBeFalse();
    });
  }
});

describe('ButtonComponent — font và vòng focus', () => {
  let fixture: ComponentFixture<ButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ButtonComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(ButtonComponent);
    fixture.detectChanges();
  });

  const nut = (): HTMLButtonElement =>
    (fixture.nativeElement as HTMLElement).querySelector('button')!;

  it('font-family tính ra bắt đầu bằng Be Vietnam Pro (không rơi về font hệ thống của <button>)', () => {
    expect(getComputedStyle(nut()).fontFamily).toMatch(/^["']?Be Vietnam Pro/);
  });

  it('nút GIỮ vòng --color-focus có khoảng hở 2px (luật "một dấu hiệu" chỉ áp cho ô nhập chữ)', () => {
    const mau = document.createElement('span');
    mau.style.color = 'var(--color-focus)';
    document.body.appendChild(mau);
    const focus = getComputedStyle(mau).color;
    mau.remove();

    nut().focus();
    const cs = getComputedStyle(nut());
    expect(cs.outlineStyle).toBe('solid');
    expect(cs.outlineColor).toBe(focus);
    expect(cs.outlineOffset).toBe('2px');
  });
});

/** Button.md §Liên kết mang hình thức nút — `a[app-button]`. */
@Component({
  standalone: true,
  imports: [ButtonComponent, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a app-button [routerLink]="khoa() ? null : '/x'" variant="ghost" [disabled]="khoa()">Đi</a>
  `,
})
class LienKetHostComponent {
  readonly khoa = signal(false);
}

describe('ButtonComponent — host <a>', () => {
  let fixture: ComponentFixture<LienKetHostComponent>;
  const a = (): HTMLAnchorElement => (fixture.nativeElement as HTMLElement).querySelector('a')!;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LienKetHostComponent],
      providers: [provideZonelessChangeDetection(), provideRouter([])],
    }).compileComponents();
    fixture = TestBed.createComponent(LienKetHostComponent);
    fixture.detectChanges();
  });

  it('có href, lớp biến thể, KHÔNG có <button> con', () => {
    expect(a().getAttribute('href')).toBe('/x');
    expect(a().classList).toContain('nut--ghost');
    expect(a().querySelector('button')).toBeNull();
    expect(a().textContent).toContain('Đi');
  });

  it('font-family là --font-sans, không gạch chân', () => {
    const cs = getComputedStyle(a());
    expect(cs.fontFamily).toMatch(/^["']?Be Vietnam Pro/);
    expect(cs.textDecorationLine).toBe('none');
  });

  it('disabled: aria-disabled, không href, bấm không điều hướng', async () => {
    const router = TestBed.inject(Router);
    const spy = spyOn(router, 'navigateByUrl');
    fixture.componentInstance.khoa.set(true);
    fixture.detectChanges();

    expect(a().getAttribute('aria-disabled')).toBe('true');
    expect(a().hasAttribute('href')).toBeFalse();
    expect(getComputedStyle(a()).opacity).toBe('0.55');
    expect(getComputedStyle(a()).cursor).toBe('not-allowed');
    a().click();
    expect(spy).not.toHaveBeenCalled();
  });
});

describe('ButtonComponent — nút mở lớp nổi (haspopup / expanded / controls)', () => {
  let fixture: ComponentFixture<ButtonComponent>;
  const nut = (): HTMLButtonElement =>
    (fixture.nativeElement as HTMLElement).querySelector('button')!;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ButtonComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(ButtonComponent);
  });

  it('mặc định không sinh aria-haspopup / aria-expanded / aria-controls', () => {
    fixture.detectChanges();
    for (const a of ['aria-haspopup', 'aria-expanded', 'aria-controls']) {
      expect(nut().hasAttribute(a)).withContext(a).toBeFalse();
    }
  });

  it('khai cả ba → sinh đúng thuộc tính; expanded=false vẫn sinh "false"', () => {
    fixture.componentRef.setInput('haspopup', 'dialog');
    fixture.componentRef.setInput('expanded', false);
    fixture.componentRef.setInput('controls', 'panel-1');
    fixture.detectChanges();
    expect(nut().getAttribute('aria-haspopup')).toBe('dialog');
    expect(nut().getAttribute('aria-expanded')).toBe('false');
    expect(nut().getAttribute('aria-controls')).toBe('panel-1');
    fixture.componentRef.setInput('expanded', true);
    fixture.detectChanges();
    expect(nut().getAttribute('aria-expanded')).toBe('true');
  });

  it('form: mặc định không sinh thuộc tính form; khai id thì nút thuộc về form đó dù đứng ngoài thẻ', () => {
    fixture.detectChanges();
    expect(nut().hasAttribute('form')).toBeFalse();
    fixture.componentRef.setInput('type', 'submit');
    fixture.componentRef.setInput('form', 'bieu-mau-1');
    fixture.detectChanges();
    expect(nut().getAttribute('form')).toBe('bieu-mau-1');
    expect(nut().type).toBe('submit');
  });
});
