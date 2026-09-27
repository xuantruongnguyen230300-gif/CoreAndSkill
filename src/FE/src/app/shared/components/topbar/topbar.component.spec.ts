import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TopbarComponent } from './topbar.component';

/**
 * Design/Icons.md §5 "Tên icon ở API component": mọi input `icon` nhận TÊN TRẦN, component tự ghép
 * `pi pi-<tên>`. `Topbar` từng đi chiều ngược lại (`platform/shell` truyền `pi-user`, template vẽ
 * `class="pi {{ muc.icon }}"`) — hai chiều cùng tồn tại trong một mã nguồn là nguồn của lớp
 * `pi-pi-user`, thứ chỉ lộ ra sau khi gói `primeicons` được nạp.
 */
describe('TopbarComponent — tên icon của mục menu tài khoản', () => {
  let fixture: ComponentFixture<TopbarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TopbarComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(TopbarComponent);
  });

  function icon(): HTMLElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector('.topbar__muc i');
  }

  it('tên trần `user` → lớp `pi pi-user`, không có lớp trần `user`', () => {
    fixture.componentRef.setInput('menuItems', [{ key: 'ho-so', label: 'Hồ sơ', icon: 'user' }]);
    fixture.detectChanges();

    expect(icon()?.classList.contains('pi')).toBeTrue();
    expect(icon()?.classList.contains('pi-user')).toBeTrue();
    expect(icon()?.classList.contains('user')).toBeFalse();
  });

  it('mục không có icon thì không vẽ thẻ <i> nào', () => {
    fixture.componentRef.setInput('menuItems', [{ key: 'ho-so', label: 'Hồ sơ' }]);
    fixture.detectChanges();

    expect(icon()).toBeNull();
  });
});
