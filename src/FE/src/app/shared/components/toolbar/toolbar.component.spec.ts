import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ToolbarComponent } from './toolbar.component';

function mauCua(token: string): string {
  const mau = document.createElement('span');
  mau.style.color = `var(${token})`;
  document.body.appendChild(mau);
  const ra = getComputedStyle(mau).color;
  mau.remove();
  return ra;
}

/** DESIGN.md §2.6 *Ô nhập chữ* — ô tìm của Toolbar: một đường biên, không vòng `--color-focus`. */
describe('ToolbarComponent — ô tìm: đường biên focus', () => {
  let fixture: ComponentFixture<ToolbarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToolbarComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(ToolbarComponent);
    fixture.componentRef.setInput('searchLabel', 'Tìm kiếm');
    fixture.detectChanges();
    o().style.transition = 'none'; // đo giá trị cuối, không đo giữa chuyển tiếp màu
  });

  const o = (): HTMLInputElement =>
    (fixture.nativeElement as HTMLElement).querySelector('.toolbar__tim-input')!;

  it('có focus: không outline ngoài, viền 1px + bóng trong 1px = 2px màu --color-text', () => {
    o().focus();
    const cs = getComputedStyle(o());
    expect(cs.outlineStyle).toBe('none');
    expect(cs.borderTopColor).toBe(mauCua('--color-text'));
    expect(cs.boxShadow).toBe(`${mauCua('--color-text')} 0px 0px 0px 1px inset`);
  });

  it('kích thước hộp ô không đổi giữa nghỉ và focus', () => {
    const truoc = o().getBoundingClientRect();
    o().focus();
    const sau = o().getBoundingClientRect();
    expect([sau.width, sau.height]).toEqual([truoc.width, truoc.height]);
  });

  it('font-family tính ra bắt đầu bằng Be Vietnam Pro (không rơi về font hệ thống của <input>)', () => {
    expect(getComputedStyle(o()).fontFamily).toMatch(/^["']?Be Vietnam Pro/);
  });
});
