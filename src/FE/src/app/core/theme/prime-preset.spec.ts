import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
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

  it('semantic.mask.transitionDuration nối về --dur-base — CÙNG token với khối đè thời lượng của Dialog', () => {
    // Luật F1 cấm `core/` import `shared/`, nên phép kiểm này không nhắc tới lớp bọc của Dialog
    // được; nó canh nửa còn lại — tên token. Spec của lớp bọc canh nửa kia (khối đè ↔ `--dur-base`).
    expect(bienGoc('--p-mask-transition-duration'))
      .withContext('backdrop trôi khỏi hộp — Dialog.md đòi hai thứ mờ dần CÙNG LÚC')
      .toBe(bienGoc('--dur-base'));
  });

  it('backdrop KHÔNG dùng chung token với chuyển tiếp nhỏ (quyết định 7)', () => {
    // Nối lẻ backdrop về `--dur-fast` là đúng cái ADR cấm: nó làm khoảng lệch với hộp rộng ra.
    expect(bienGoc('--p-mask-transition-duration')).not.toBe(bienGoc('--p-transition-duration'));
  });

  it('semantic.mask.background nối về --color-overlay — màu lớp phủ của Dialog, Drawer', () => {
    // Keyframes mặt nạ của PrimeNG 21 đọc `mask.background`; thiếu nối thì lớp phủ là đen 40% của Aura.
    expect(bienGoc('--color-overlay')).withContext('token nguồn rỗng').not.toBe('');
    expect(bienGoc('--p-mask-background'))
      .withContext('lớp phủ trôi khỏi --color-overlay')
      .toBe(bienGoc('--color-overlay'));
  });
});

/**
 * DESIGN.md §2.6 *Ô nhập chữ*: ô của lớp bọc PrimeNG (Autocomplete đơn = `pInputText`) có MỘT đường
 * biên khi focus — viền `--color-text`, phần dày thêm là bóng `inset`, không vòng ngoài, không
 * `--color-focus`. Khoá `focusRing` CHUNG của preset (nút, checkbox…) vẫn là `--color-focus`.
 */
describe('CORE_PRIME_PRESET — ô nhập chữ: một đường biên khi focus', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
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
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('biến viền focus của ô nhập là --color-text, KHÔNG phải --color-focus', () => {
    expect(bienGoc('--p-form-field-focus-border-color')).toBe(bienGoc('--color-text'));
    expect(bienGoc('--p-form-field-focus-border-color')).not.toBe(bienGoc('--color-focus'));
  });

  it('hover ô nhập KHÔNG đổi viền: màu viền hover = màu viền nghỉ (DESIGN.md §2.6 hàng hover)', () => {
    expect(bienGoc('--p-form-field-hover-border-color')).toBe(
      bienGoc('--p-form-field-border-color'),
    );
  });

  // Karma không giả lập được `:hover` thật, nên viền ô lỗi khi hover (`_thu-vien.scss`, khối
  // `.p-invalid:enabled:hover`) chưa có test đo — để Playwright đo trên Chrome thật.
  it('khoá focusRing CHUNG giữ nguyên --color-focus (nút, checkbox, liên kết)', () => {
    expect(bienGoc('--p-focus-ring-color')).toBe(bienGoc('--color-focus'));
  });

  it('ô nhập Autocomplete đơn có focus: không outline ngoài, viền --color-text', () => {
    const o = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      'p-autocomplete:not(.p-autocomplete-multiple) input.p-inputtext',
    )!;
    o.style.transition = 'none'; // đo giá trị cuối, không đo giữa chuyển tiếp màu
    o.focus();
    const cs = getComputedStyle(o);
    const mau = document.createElement('span');
    mau.style.color = 'var(--color-text)';
    document.body.appendChild(mau);
    const chu = getComputedStyle(mau).color;
    mau.remove();
    expect(cs.outlineStyle).toBe('none');
    expect(cs.borderTopColor).toBe(chu);
    expect(cs.boxShadow).toBe(`${chu} 0px 0px 0px 1px inset`);
  });
});

@Component({
  standalone: true,
  imports: [AutoComplete, FormsModule],
  template: `
    <p-autoComplete
      class="don"
      [invalid]="true"
      inputStyleClass="o-don"
      optionLabel="label"
      [suggestions]="[]"
    />
    <p-autoComplete
      class="nhieu"
      [multiple]="true"
      [invalid]="true"
      optionLabel="label"
      [suggestions]="[]"
    />
    <p-autoComplete
      class="hop-le"
      inputStyleClass="o-hop-le"
      optionLabel="label"
      [suggestions]="[]"
    />
  `,
})
class LoiHostComponent {}

/**
 * DESIGN.md §2.6 bảng hai trục: ô đang LỖI giữ màu lỗi ở mọi trạng thái, chỉ đổi độ dày. Khối
 * `:enabled:focus` của Aura (0,3,0) từng thắng `.p-invalid` (0,2,0) nên ô lỗi mất màu khi focus —
 * `_thu-vien.scss` § `.p-invalid` đè lại. Đo bằng `.focus()` thật; riêng `:hover` Karma không giả
 * lập được nên để Playwright đo.
 */
describe('CORE_PRIME_PRESET — ô nhập chữ đang lỗi: giữ màu lỗi, chỉ đổi độ dày', () => {
  let fixture: ComponentFixture<LoiHostComponent>;
  let goc: HTMLElement;

  function mau(bien: string): string {
    const span = document.createElement('span');
    span.style.color = `var(${bien})`;
    document.body.appendChild(span);
    const ra = getComputedStyle(span).color;
    span.remove();
    return ra;
  }

  function oDon(): HTMLInputElement {
    const o = goc.querySelector<HTMLInputElement>('p-autocomplete.don input.p-inputtext')!;
    o.style.transition = 'none';
    return o;
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [LoiHostComponent],
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
    fixture = TestBed.createComponent(LoiHostComponent);
    document.body.appendChild(fixture.nativeElement);
    fixture.detectChanges();
    await fixture.whenStable();
    goc = fixture.nativeElement as HTMLElement;
  });

  it('canary: ô lỗi thật sự mang cờ lỗi (không thì các phép đo dưới đo nhầm ô hợp lệ)', () => {
    expect(goc.querySelector('p-autocomplete.don.p-invalid')).not.toBeNull();
    expect(goc.querySelector('p-autocomplete.nhieu.p-invalid')).not.toBeNull();
  });

  it('lúc nghỉ: viền thật --border-w, màu lỗi, dày thêm vào trong bằng bóng inset (tổng --border-w-strong)', () => {
    const cs = getComputedStyle(oDon());
    const loi = mau('--color-danger-border');
    expect(cs.borderTopColor).toBe(loi);
    expect(cs.borderTopWidth).toBe('1px');
    expect(cs.boxShadow).toBe(`${loi} 0px 0px 0px 1px inset`);
  });

  it('có focus: GIỮ màu lỗi, dày lên --border-w-accent, không outline, không --color-text', () => {
    const o = oDon();
    o.focus();
    const cs = getComputedStyle(o);
    const loi = mau('--color-danger-border');
    expect(document.activeElement).withContext('focus thật').toBe(o);
    expect(cs.borderTopColor).toBe(loi);
    expect(cs.borderTopColor).not.toBe(mau('--color-text'));
    expect(cs.borderTopWidth).toBe('1px');
    expect(cs.boxShadow).toBe(`${loi} 0px 0px 0px 2px inset`);
    expect(cs.outlineStyle).toBe('none');
  });

  it('ô hợp lệ không bị lây khối lỗi: nghỉ không bóng inset, có focus viền --color-text', () => {
    const o = goc.querySelector<HTMLInputElement>('input.o-hop-le')!;
    o.style.transition = 'none';
    expect(getComputedStyle(o).boxShadow).not.toContain('inset');
    o.focus();
    const cs = getComputedStyle(o);
    expect(cs.borderTopColor).toBe(mau('--color-text'));
    expect(cs.boxShadow).toBe(`${mau('--color-text')} 0px 0px 0px 1px inset`);
  });

  it('khung của Autocomplete multiple đang lỗi: màu lỗi lúc nghỉ, và khi root có p-focus', () => {
    const khung = goc.querySelector<HTMLElement>(
      'p-autocomplete.nhieu .p-autocomplete-input-multiple',
    )!;
    khung.style.transition = 'none';
    const loi = mau('--color-danger-border');
    expect(getComputedStyle(khung).borderTopColor).toBe(loi);
    expect(getComputedStyle(khung).boxShadow).toBe(`${loi} 0px 0px 0px 1px inset`);
    goc.querySelector('p-autocomplete.nhieu')!.classList.add('p-focus'); // cùng lớp PrimeNG gắn khi focus
    expect(getComputedStyle(khung).borderTopColor).toBe(loi);
    expect(getComputedStyle(khung).boxShadow).toBe(`${loi} 0px 0px 0px 2px inset`);
  });
});

/**
 * DESIGN.md §2.6 hàng *Phạm vi*: `formField.focusBorderColor`/`focusRing` chỉ dành cho ô nhập chữ.
 * Ô số dòng của Paginator (`p-select`) đọc cùng khoá qua Aura nên phải được khai lại về vòng chung.
 */
describe('CORE_PRIME_PRESET — ô chọn (p-select) giữ vòng focus chung, không lây viền ô nhập chữ', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
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
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('biến viền focus của select KHÔNG phải --color-text', () => {
    expect(bienGoc('--p-select-focus-border-color')).not.toBe('');
    expect(bienGoc('--p-select-focus-border-color')).not.toBe(bienGoc('--color-text'));
  });

  it('vòng focus của select là vòng chung --color-focus, không phải bóng inset của ô nhập chữ', () => {
    expect(bienGoc('--p-select-focus-ring-color')).toBe(bienGoc('--color-focus'));
    expect(bienGoc('--p-select-focus-ring-shadow')).toBe('none');
    expect(bienGoc('--p-select-focus-ring-width')).not.toBe('0');
  });

  it('ô số dòng của Paginator có focus: viền không đổi sang --color-text, có outline, không bóng inset', () => {
    const o = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>(
      '.p-paginator .p-select',
    )!;
    o.style.transition = 'none';
    o.classList.add('p-focus'); // cùng lớp PrimeNG gắn khi ô nhận focus
    const cs = getComputedStyle(o);
    const mau = document.createElement('span');
    mau.style.color = 'var(--color-text)';
    document.body.appendChild(mau);
    const chu = getComputedStyle(mau).color;
    mau.remove();
    expect(cs.borderTopColor).not.toBe(chu);
    expect(cs.outlineStyle).toBe('solid');
    expect(cs.boxShadow).toBe('none');
    // Dấu hiệu thứ hai không được có trên nhãn bên trong.
    const nhan = o.querySelector<HTMLElement>('.p-select-label')!;
    expect(getComputedStyle(nhan).outlineStyle).toBe('none');
  });
});
