import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

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
