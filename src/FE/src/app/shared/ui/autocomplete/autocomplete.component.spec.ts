import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AutocompleteComponent, Option } from './autocomplete.component';

@Component({
  standalone: true,
  imports: [AutocompleteComponent],
  template: `
    <app-autocomplete
      placeholder="Gõ tên"
      inputId="o-vai-tro"
      [options]="options()"
      [value]="value()"
    />
  `,
})
class HostComponent {
  readonly options = signal<readonly Option[]>([]);
  readonly value = signal<string | null>(null);
}

describe('AutocompleteComponent — cache nhãn', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  async function onDinh(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    await new Promise((r) => setTimeout(r));
  }
  const o = (): HTMLInputElement => fixture.nativeElement.querySelector('#o-vai-tro');

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    await onDinh();
  });

  it('khoá có trong options → ô hiện nhãn', async () => {
    host.options.set([{ key: 'r1', label: 'Kế toán' }]);
    host.value.set('r1');
    await onDinh();
    expect(o().value).toBe('Kế toán');
  });

  it('options đổi sang kết quả khác → khoá đã chọn vẫn giữ nhãn', async () => {
    host.options.set([{ key: 'r1', label: 'Kế toán' }]);
    host.value.set('r1');
    await onDinh();
    host.options.set([{ key: 'r2', label: 'Nhân sự' }]);
    await onDinh();
    expect(o().value).toBe('Kế toán');
  });

  it('options rỗng không xoá nhãn đã biết', async () => {
    host.options.set([{ key: 'r1', label: 'Kế toán' }]);
    host.value.set('r1');
    await onDinh();
    host.options.set([]);
    await onDinh();
    expect(o().value).toBe('Kế toán');
  });

  it('nhãn của một khoá đổi → ô hiện nhãn mới', async () => {
    host.options.set([{ key: 'r1', label: 'Kế toán' }]);
    host.value.set('r1');
    await onDinh();
    host.options.set([{ key: 'r1', label: 'Kế toán trưởng' }]);
    await onDinh();
    expect(o().value).toBe('Kế toán trưởng');
  });

  it('khoá chưa có nhãn → ô hiện chính khoá', async () => {
    host.value.set('r9');
    await onDinh();
    expect(o().value).toBe('r9');
  });
});

@Component({
  standalone: true,
  imports: [AutocompleteComponent],
  template: `
    <div id="khung-cuon" style="position: relative; overflow: auto; height: 60px">
      <app-autocomplete
        placeholder="Gõ tên"
        inputId="o-goi-y"
        [options]="options()"
        [debounceMs]="0"
        (search)="options.set([{ key: 'r1', label: 'Kế toán' }])"
      />
    </div>
  `,
})
class HostTrongKhungCuonComponent {
  readonly options = signal<readonly Option[]>([]);
}

/**
 * Bảng gợi ý nằm ngoài khung cuộn của hộp thoại/trang: để tại chỗ thì lớp nổi làm khung cuộn đổi
 * `scroll`, bộ nghe cuộn của PrimeNG đóng bảng — và khi giảm chuyển động bộ nghe gắn ngay lúc hiện nên
 * bảng biến mất (khi có hoạt ảnh, bộ nghe gắn sau 210ms nên cú cuộn lọt qua).
 */
describe('AutocompleteComponent — lớp nổi gắn vào body, không nằm trong khung cuộn', () => {
  it('có gợi ý: .p-autocomplete-overlay là phần tử ngoài khung cuộn của host', async () => {
    await TestBed.configureTestingModule({
      imports: [HostTrongKhungCuonComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    const fixture = TestBed.createComponent(HostTrongKhungCuonComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const o = fixture.nativeElement.querySelector('#o-goi-y') as HTMLInputElement;
    o.focus();
    o.value = 'Kế';
    o.dispatchEvent(new Event('input', { bubbles: true }));
    const khung = fixture.nativeElement.querySelector('#khung-cuon') as HTMLElement;
    const bang = (): HTMLElement | null => document.querySelector('.p-autocomplete-overlay');
    // PrimeNG dựng bảng tại chỗ rồi mới chuyển sang nơi gắn ở bước "trước khi hiện" — chờ bước đó xong.
    await new Promise<void>((xong) => {
      const han = Date.now() + 3000;
      const cho = (): void => {
        fixture.detectChanges();
        const b = bang();
        if ((b && !khung.contains(b)) || Date.now() > han) xong();
        else setTimeout(cho, 20);
      };
      cho();
    });

    expect(bang()).not.toBeNull();
    expect(khung.contains(bang())).toBeFalse();
    fixture.destroy();
  });
});
