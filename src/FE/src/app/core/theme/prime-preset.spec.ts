import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { AutoComplete } from 'primeng/autocomplete';
import { providePrimeNG } from 'primeng/config';
import { Dialog } from 'primeng/dialog';
import { Paginator } from 'primeng/paginator';

import { CORE_PRIME_PRESET } from './prime-preset';

/**
 * PrimeNG dựng component CON bên trong component mà lớp bọc ở shared/ui/ dùng: nút đóng của Dialog
 * là `p-button`, ô số dòng của Paginator là `p-select`, Autocomplete `multiple` vẽ `p-chip` và ô nhập
 * đơn mang `pInputText`. Biến `--p-<con>-*` chỉ sinh ra khi preset có `components.<con>` — thiếu thì
 * CSS tĩnh của con tham chiếu biến rỗng: vẫn chạy, sai theme, không lỗi nào báo.
 *
 * Cấu hình dưới lặp đúng cấu hình các lớp bọc đang dùng: Dialog `closable`, Paginator có
 * `rowsPerPageOptions`, Autocomplete cả hai biến thể (`multiple` đã có giá trị, và đơn).
 */
@Component({
  standalone: true,
  imports: [Dialog, Paginator, AutoComplete, FormsModule],
  template: `
    <p-dialog [visible]="true" [modal]="true" [closable]="true" header="x" />
    <p-paginator [rows]="20" [totalRecords]="100" [rowsPerPageOptions]="[10, 20]" />
    <p-autoComplete [multiple]="true" [ngModel]="chon" optionLabel="label" [suggestions]="[]" />
    <p-autoComplete [multiple]="false" optionLabel="label" [suggestions]="[]" />
  `,
})
class HostComponent {
  readonly chon = [{ label: 'Kế toán' }];
}

function bienGoc(ten: string): string {
  return getComputedStyle(document.documentElement).getPropertyValue(ten).trim();
}

describe('CORE_PRIME_PRESET — biến theme của component con PrimeNG', () => {
  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        providePrimeNG({
          theme: {
            preset: CORE_PRIME_PRESET,
            options: { darkModeSelector: false, cssLayer: true },
          },
        }),
      ],
    });
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    // ngModel ghi giá trị ở một microtask sau — cần thêm một vòng để chip được vẽ.
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('component con thật sự được dựng (điều kiện để phép kiểm biến dưới có nghĩa)', () => {
    expect(document.querySelector('.p-dialog-close-button')).withContext('p-button').not.toBeNull();
    expect(document.querySelector('.p-paginator .p-select')).withContext('p-select').not.toBeNull();
    expect(document.querySelector('.p-autocomplete p-chip')).withContext('p-chip').not.toBeNull();
    expect(document.querySelector('.p-autocomplete input.p-inputtext'))
      .withContext('pInputText')
      .not.toBeNull();
  });

  for (const [con, bien] of [
    ['button (nút đóng Dialog)', '--p-button-padding-x'],
    ['select (ô số dòng Paginator)', '--p-select-background'],
    ['chip (Autocomplete multiple)', '--p-chip-border-radius'],
    ['inputtext (ô nhập Autocomplete đơn)', '--p-inputtext-background'],
  ] as const) {
    it(`sinh biến của ${con}: ${bien}`, () => {
      expect(bienGoc(bien)).withContext(`${bien} rỗng — preset thiếu sub-preset`).not.toBe('');
    });
  }

  it('đối chứng: biến của component có sub-preset (dialog) có giá trị', () => {
    expect(bienGoc('--p-dialog-background')).not.toBe('');
  });
});

/**
 * ADR-0073 — hai khoá semantic thời lượng. Chúng KHÔNG phải màu, nên chúng không nằm trong
 * `colorScheme` và không lộ ra ở bất kỳ phép kiểm nào của describe trên.
 *
 * Phép kiểm so `--p-*` với CHÍNH token `--dur-*`, không so với chuỗi `'120ms'`/`'200ms'` viết tay:
 * đổi giá trị một token trong `_tokens.scss` thì phép kiểm vẫn đúng, còn NGẮT sợi nối thì nó đỏ —
 * đó mới là thứ cần canh. Hai `it` đầu là canary chống *cổng xanh rỗng*: token không tới được
 * `getComputedStyle` thì cả hai vế đều là chuỗi rỗng và mọi phép so bằng dưới đây xanh một cách
 * vô nghĩa.
 */
describe('CORE_PRIME_PRESET — khoá semantic thời lượng chuyển động (ADR-0073)', () => {
  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        providePrimeNG({
          theme: {
            preset: CORE_PRIME_PRESET,
            options: { darkModeSelector: false, cssLayer: true },
          },
        }),
      ],
    });
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('canary: token --dur-fast đọc được (nếu không, mọi phép so dưới đây là so hai chuỗi rỗng)', () => {
    expect(bienGoc('--dur-fast')).withContext('_tokens.scss chưa vào tài liệu test').not.toBe('');
  });

  it('canary: token --dur-base đọc được', () => {
    expect(bienGoc('--dur-base')).withContext('_tokens.scss chưa vào tài liệu test').not.toBe('');
  });

  it('semantic.transitionDuration nối về --dur-fast (quyết định 1 — MỘT khoá cho cả thư viện)', () => {
    expect(bienGoc('--p-transition-duration'))
      .withContext('khoá transitionDuration rơi khỏi definePreset, hoặc trỏ sai token')
      .toBe(bienGoc('--dur-fast'));
  });

  it('formField KHÔNG được khai riêng: nó thừa hưởng đúng khoá trên, không phải nguồn thứ hai', () => {
    // Aura khai `semantic.formField.transitionDuration = '{transition.duration}'`. Khai lại khoá
    // đó trong `definePreset` là dựng nguồn thứ hai cho cùng một giá trị (ADR-0073, vế 1 phương
    // án B). Phép kiểm này đỏ khi ai đó khai riêng rồi cho nó một token khác.
    expect(bienGoc('--p-form-field-transition-duration'))
      .withContext('formField đã bị khai riêng và trôi khỏi transitionDuration')
      .toBe(bienGoc('--dur-fast'));
  });

  it('semantic.mask.transitionDuration nối về --dur-base — CÙNG token với DIALOG_THOI_LUONG_MS', () => {
    // Luật F1 cấm `core/` import `shared/`, nên phép kiểm này không nhắc tới hằng số của Dialog
    // được; nó canh nửa còn lại — tên token. Cổng F37 canh nửa kia (hằng số ↔ `--dur-base`).
    expect(bienGoc('--p-mask-transition-duration'))
      .withContext('backdrop trôi khỏi hộp — Dialog.md đòi hai thứ mờ dần CÙNG LÚC')
      .toBe(bienGoc('--dur-base'));
  });

  it('backdrop KHÔNG dùng chung token với chuyển tiếp nhỏ (quyết định 7)', () => {
    // Nối lẻ backdrop về `--dur-fast` là đúng cái ADR cấm: nó làm khoảng lệch với hộp rộng ra.
    expect(bienGoc('--p-mask-transition-duration')).not.toBe(bienGoc('--p-transition-duration'));
  });
});
