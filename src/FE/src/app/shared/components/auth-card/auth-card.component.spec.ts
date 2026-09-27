import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AuthCardComponent } from './auth-card.component';

/**
 * quy-uoc/fe-ui-conventions.md §6.2: câu của `applyFormFailure` mang mỗi mã một dòng, nối bằng
 * `\n`; màn xác thực đưa câu đó vào khu lỗi của `AuthCard` qua `errorMessage`. Khu lỗi phải giữ
 * ngắt dòng — không thì hai lý do dính thành một câu.
 */
describe('AuthCardComponent — khu lỗi', () => {
  let fixture: ComponentFixture<AuthCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuthCardComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(AuthCardComponent);
    fixture.componentRef.setInput('title', 'Đăng nhập');
  });

  function khuLoi(): HTMLElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector('.auth-card__khu-loi');
  }

  it('errorMessage = null → không có khu lỗi', () => {
    fixture.detectChanges();
    expect(khuLoi()).toBeNull();
  });

  it('câu nhiều dòng → khu lỗi role=alert giữ ngắt dòng (white-space: pre-line)', () => {
    fixture.componentRef.setInput('errorMessage', 'Lý do một\nLý do hai');
    fixture.detectChanges();

    const khu = khuLoi();
    expect(khu?.getAttribute('role')).toBe('alert');
    expect(getComputedStyle(khu as Element).whiteSpace).toBe('pre-line');
    const cau = khu?.querySelector('span');
    expect(cau?.textContent).toBe('Lý do một\nLý do hai');
    expect(getComputedStyle(cau as Element).whiteSpace)
      .withContext('phần tử mang chữ thừa hưởng pre-line, không tự đè về normal')
      .toBe('pre-line');
  });
});
