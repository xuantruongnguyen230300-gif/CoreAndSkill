import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

import { CheckComponent } from './check.component';

/**
 * Check.md §Accessibility: mọi thuộc tính trợ năng phải nằm trên phần tử NHẬN FOCUS — cái
 * `<input>` thật do PrimeNG vẽ — chứ không phải trên host `<p-checkbox>` / `<p-radioButton>`.
 * Trình đọc màn hình đọc theo phần tử đang focus, nên một thuộc tính rơi lên host là một
 * thuộc tính không ai nghe thấy: nó vẫn hiện trong DOM, vẫn "trông đúng", và không có gì báo.
 *
 * Vì thế MỖI ca dưới đây khẳng định hai vế cùng lúc: có trên `<input>` VÀ không có trên host.
 * Bỏ vế thứ hai thì một `[attr.…]` đặt nhầm chỗ vẫn làm test xanh.
 */

function inputThat(fixture: ComponentFixture<unknown>): HTMLInputElement {
  const el = (fixture.nativeElement as HTMLElement).querySelector('input');
  if (!el) throw new Error('Không tìm thấy <input> do PrimeNG vẽ');
  return el;
}

function hostThuVien(fixture: ComponentFixture<unknown>, the: string): HTMLElement {
  const el = (fixture.nativeElement as HTMLElement).querySelector(the);
  if (!el) throw new Error(`Không tìm thấy host <${the}>`);
  return el as HTMLElement;
}

describe('CheckComponent — checkbox: thuộc tính trợ năng nằm trên <input>, không trên host', () => {
  let fixture: ComponentFixture<CheckComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CheckComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(CheckComponent);
  });

  it('aria-label nằm trên <input>, KHÔNG nằm trên host <p-checkbox>', () => {
    fixture.componentRef.setInput('ariaLabel', 'Cấp quyền Xem cho vai trò Quản trị');
    fixture.detectChanges();

    expect(inputThat(fixture).getAttribute('aria-label')).toBe(
      'Cấp quyền Xem cho vai trò Quản trị',
    );
    expect(hostThuVien(fixture, 'p-checkbox').hasAttribute('aria-label')).toBeFalse();
  });

  it('ariaLabel null → <input> không mang aria-label rỗng', () => {
    fixture.detectChanges();

    expect(inputThat(fixture).hasAttribute('aria-label')).toBeFalse();
  });

  it('aria-describedby nằm trên <input>, KHÔNG nằm trên host <p-checkbox>', () => {
    fixture.componentRef.setInput('describedBy', 'o-quyen-error');
    fixture.detectChanges();

    expect(inputThat(fixture).getAttribute('aria-describedby')).toBe('o-quyen-error');
    expect(hostThuVien(fixture, 'p-checkbox').hasAttribute('aria-describedby')).toBeFalse();
  });

  it('indeterminate → aria-checked="mixed" nằm trên <input>, KHÔNG nằm trên host <p-checkbox>', () => {
    fixture.componentRef.setInput('indeterminate', true);
    fixture.detectChanges();

    expect(inputThat(fixture).getAttribute('aria-checked')).toBe('mixed');
    expect(hostThuVien(fixture, 'p-checkbox').hasAttribute('aria-checked')).toBeFalse();
  });

  it('KHÔNG indeterminate → <input> không mang aria-checked (trạng thái gốc của checkbox tự nói)', () => {
    fixture.detectChanges();

    expect(inputThat(fixture).hasAttribute('aria-checked')).toBeFalse();
  });

  it('indeterminate true → false lúc chạy: aria-checked bị GỠ khỏi <input>, không sót lại "mixed"', () => {
    fixture.componentRef.setInput('indeterminate', true);
    fixture.detectChanges();
    expect(inputThat(fixture).getAttribute('aria-checked')).toBe('mixed');

    fixture.componentRef.setInput('indeterminate', false);
    fixture.detectChanges();

    expect(inputThat(fixture).hasAttribute('aria-checked')).toBeFalse();
  });

  it('cả ba thuộc tính cùng tới <input> một lượt', () => {
    fixture.componentRef.setInput('ariaLabel', 'Nhãn');
    fixture.componentRef.setInput('describedBy', 'mo-ta');
    fixture.componentRef.setInput('indeterminate', true);
    fixture.detectChanges();

    const input = inputThat(fixture);
    expect(input.getAttribute('aria-label')).toBe('Nhãn');
    expect(input.getAttribute('aria-describedby')).toBe('mo-ta');
    expect(input.getAttribute('aria-checked')).toBe('mixed');
  });
});

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, CheckComponent],
  template: `
    <app-check
      type="radio"
      name="nhom-thu"
      value="a"
      [formControl]="control"
      [ariaLabel]="nhan()"
      [describedBy]="moTa()"
      [indeterminate]="nuaChon()"
    />
  `,
})
class HostRadioComponent {
  readonly control = new FormControl<string | null>('a');
  readonly nhan = signal<string | null>(null);
  readonly moTa = signal<string | null>(null);
  readonly nuaChon = signal(false);
}

describe('CheckComponent — radio: pt KHÔNG được đè aria-checked của thư viện', () => {
  let fixture: ComponentFixture<HostRadioComponent>;
  let host: HostRadioComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostRadioComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostRadioComponent);
    host = fixture.componentInstance;
  });

  it('aria-label nằm trên <input type="radio">, KHÔNG nằm trên host <p-radiobutton>', async () => {
    host.nhan.set('Lựa chọn A');
    await fixture.whenStable();

    const input = inputThat(fixture);
    expect(input.type).toBe('radio');
    expect(input.getAttribute('aria-label')).toBe('Lựa chọn A');
    expect(hostThuVien(fixture, 'p-radiobutton').hasAttribute('aria-label')).toBeFalse();
  });

  it('aria-describedby nằm trên <input type="radio">, KHÔNG nằm trên host <p-radiobutton>', async () => {
    host.moTa.set('nhom-error');
    await fixture.whenStable();

    expect(inputThat(fixture).getAttribute('aria-describedby')).toBe('nhom-error');
    expect(hostThuVien(fixture, 'p-radiobutton').hasAttribute('aria-describedby')).toBeFalse();
  });

  /**
   * 🛑 Template `p-radioButton` của bản PrimeNG đang cài ĐÃ có `[attr.aria-checked]="checked"`
   * riêng trên `<input>` (bản `checkbox` thì không). Ghi thêm `aria-checked` qua `pt` cho nhánh
   * này là hai nguồn cùng viết một thuộc tính: `pBind` đặt bằng `renderer.setAttribute` trong
   * một `effect`, còn thư viện đặt bằng binding của template — hai bên chạy theo hai nhịp khác
   * nhau, nên kết quả phụ thuộc thứ tự và sai trong im lặng. Ba ca dưới đây khoá việc đó lại.
   */
  it('radio ĐÃ CHỌN → aria-checked="true" của thư viện còn nguyên trên <input>', async () => {
    await fixture.whenStable();

    expect(inputThat(fixture).getAttribute('aria-checked')).toBe('true');
  });

  /**
   * Đặt giá trị TRƯỚC lượt dựng đầu tiên: ca này canh đường `pt`, nên nó cố ý không phụ thuộc
   * việc vẽ lại. Việc vẽ lại sau lượt dựng đầu do bộ ca § `vẽ lại được sau lượt dựng đầu` canh
   * riêng — hai mối lo tách bạch, hỏng cái nào thì đúng tên cái đó đỏ.
   */
  it('radio CHƯA CHỌN → aria-checked="false" của thư viện vẫn có mặt, pt không gỡ mất', async () => {
    host.control.setValue('b');
    await fixture.whenStable();

    expect(inputThat(fixture).getAttribute('aria-checked')).toBe('false');
  });

  it('indeterminate đặt nhầm lên radio KHÔNG sinh ra aria-checked="mixed" đè lên thư viện', async () => {
    host.nuaChon.set(true);
    await fixture.whenStable();

    expect(inputThat(fixture).getAttribute('aria-checked')).toBe('true');
  });
});

/**
 * Giá trị đến từ control phải vẽ lại được SAU lượt dựng đầu. Ứng dụng chạy zoneless và component
 * khai `OnPush`, nên `writeValue` chỉ tới được DOM khi nó ghi vào một `signal` mà template có
 * đọc — ghi vào một trường thường thì không gì đánh dấu view bẩn, ô đứng yên với giá trị cũ, và
 * hỏng kiểu đó không phát ra tín hiệu nào: DOM vẫn hợp lệ, không lỗi, không cảnh báo.
 *
 * Mỗi ca dưới đây dựng xong lượt đầu TRƯỚC (`await fixture.whenStable()`) rồi mới đổi control.
 * Đổi giá trị trước lượt dựng đầu thì chính lượt dựng đầu đọc giá trị mới, nên ca vẫn xanh kể cả
 * khi khiếm khuyết còn nguyên — thứ tự hai dòng đó là một phần của phép đo, không phải văn phong.
 *
 * Đo bằng `<input>.checked` — trạng thái thật mà người dùng nhìn thấy và trình đọc màn hình đọc —
 * chứ không bằng trường nội bộ của component: trường nội bộ vẫn ĐÚNG trong chính ca hỏng này.
 */

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, CheckComponent],
  template: `
    <app-check [formControl]="control" [indeterminate]="nuaChon()">Đồng ý điều khoản</app-check>
  `,
})
class HostCheckboxComponent {
  readonly control = new FormControl<boolean>(false, { nonNullable: true });
  readonly nuaChon = signal(false);
}

describe('CheckComponent — checkbox: giá trị control vẽ lại được sau lượt dựng đầu', () => {
  let fixture: ComponentFixture<HostCheckboxComponent>;
  let host: HostCheckboxComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostCheckboxComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostCheckboxComponent);
    host = fixture.componentInstance;
  });

  it('control.setValue(true) SAU lượt dựng đầu → <input>.checked thành true', async () => {
    await fixture.whenStable();
    expect(inputThat(fixture).checked)
      .withContext('lượt dựng đầu vẽ đúng giá trị khởi tạo của control')
      .toBeFalse();

    host.control.setValue(true);
    await fixture.whenStable();

    expect(inputThat(fixture).checked).toBeTrue();
  });

  it('control.setValue(false) SAU khi đang chọn → <input>.checked về false', async () => {
    host.control.setValue(true);
    await fixture.whenStable();
    expect(inputThat(fixture).checked).toBeTrue();

    host.control.setValue(false);
    await fixture.whenStable();

    expect(inputThat(fixture).checked).toBeFalse();
  });

  /**
   * Hai đường ghi vào cùng một `<input>`: giá trị control đi qua `[ngModel]` (binding của
   * template), còn `aria-checked` đi qua `pt` → `pBind` → `renderer.setAttribute` trong một
   * `effect`. `ptO` cố ý KHÔNG đọc giá trị control, chỉ đọc `indeterminate()` — ca này khoá điều
   * đó lại: đổi bên nào cũng không được kéo bên kia sai nhịp.
   *
   * `checked` bị nửa chọn phủ lên là luật của THƯ VIỆN, không phải của component:
   * `primeng/checkbox` § `get checked()` trả `false` ngay khi `_indeterminate()` bật, bất kể model.
   * Nên phép đo thật nằm ở dòng cuối: hết nửa chọn thì giá trị control phải hiện LẠI, tức nó chưa
   * bao giờ bị mất.
   */
  it('nửa chọn và giá trị control là hai đường ghi độc lập, không lệch nhịp nhau', async () => {
    await fixture.whenStable();

    host.control.setValue(true);
    await fixture.whenStable();
    expect(inputThat(fixture).checked).withContext('giá trị control tới được <input>').toBeTrue();
    expect(inputThat(fixture).hasAttribute('aria-checked'))
      .withContext('không nửa chọn → không aria-checked')
      .toBeFalse();

    host.nuaChon.set(true);
    await fixture.whenStable();
    expect(inputThat(fixture).getAttribute('aria-checked')).toBe('mixed');
    expect(inputThat(fixture).checked)
      .withContext('nửa chọn phủ lên trạng thái chọn — luật của thư viện')
      .toBeFalse();

    host.nuaChon.set(false);
    await fixture.whenStable();
    expect(inputThat(fixture).hasAttribute('aria-checked')).toBeFalse();
    expect(inputThat(fixture).checked)
      .withContext('hết nửa chọn → giá trị control hiện lại, chưa bao giờ mất')
      .toBeTrue();
  });

  /**
   * `setDisabledState` đi cùng một đường với `writeValue`: `@angular/forms` gọi thẳng, ngoài mọi
   * binding, nên nó cũng phải ghi vào `signal`. Đây là ca tốn kém nhất nếu hỏng — một ô đã khoá
   * mà vẫn **bấm được** không chỉ vẽ sai, nó cho người dùng đổi thứ mà nghiệp vụ đang cấm đổi.
   */
  it('control.disable() SAU lượt dựng đầu → <input> mang disabled', async () => {
    await fixture.whenStable();
    expect(inputThat(fixture).hasAttribute('disabled')).toBeFalse();

    host.control.disable();
    await fixture.whenStable();

    expect(inputThat(fixture).hasAttribute('disabled')).toBeTrue();

    host.control.enable();
    await fixture.whenStable();

    expect(inputThat(fixture).hasAttribute('disabled')).toBeFalse();
  });
});

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, CheckComponent],
  template: `
    <app-check type="radio" name="nhom-ve-lai" value="a" [formControl]="control">Vai A</app-check>
  `,
})
class HostRadioVeLaiComponent {
  readonly control = new FormControl<string | null>(null);
}

describe('CheckComponent — radio: giá trị control vẽ lại được sau lượt dựng đầu', () => {
  let fixture: ComponentFixture<HostRadioVeLaiComponent>;
  let host: HostRadioVeLaiComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostRadioVeLaiComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostRadioVeLaiComponent);
    host = fixture.componentInstance;
  });

  it('control.setValue("a") SAU lượt dựng đầu → <input>.checked thành true', async () => {
    await fixture.whenStable();
    expect(inputThat(fixture).checked).toBeFalse();

    host.control.setValue('a');
    await fixture.whenStable();

    expect(inputThat(fixture).checked).toBeTrue();
    expect(inputThat(fixture).getAttribute('aria-checked'))
      .withContext('aria-checked của thư viện đi cùng nhịp với checked')
      .toBe('true');
  });

  it('control chuyển sang lựa chọn khác SAU lượt dựng đầu → <input>.checked về false', async () => {
    host.control.setValue('a');
    await fixture.whenStable();
    expect(inputThat(fixture).checked).toBeTrue();

    host.control.setValue('b');
    await fixture.whenStable();

    expect(inputThat(fixture).checked).toBeFalse();
    expect(inputThat(fixture).getAttribute('aria-checked')).toBe('false');
  });
});

@Component({
  standalone: true,
  imports: [CheckComponent],
  template: `<app-check [checked]="daChon()">Chọn hàng</app-check>`,
})
class HostCheckedInputComponent {
  readonly daChon = signal(false);
}

/**
 * Đường KHÔNG gắn control (ô chọn hàng trong Table.md) đi qua `input()` nên vốn đã vẽ lại được.
 * Ca này không phải canary — nó là chốt chặn hồi quy: việc chuyển giá trị CVA sang `signal` đụng
 * vào cùng một getter `daChon`, nên đường này phải được ĐO lại chứ không suy ra.
 */
describe('CheckComponent — không gắn control: đường `checked` vẽ lại được', () => {
  let fixture: ComponentFixture<HostCheckedInputComponent>;
  let host: HostCheckedInputComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostCheckedInputComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostCheckedInputComponent);
    host = fixture.componentInstance;
  });

  it('đổi input `checked` SAU lượt dựng đầu → <input>.checked đi theo', async () => {
    await fixture.whenStable();
    expect(inputThat(fixture).checked).toBeFalse();

    host.daChon.set(true);
    await fixture.whenStable();

    expect(inputThat(fixture).checked).toBeTrue();

    host.daChon.set(false);
    await fixture.whenStable();

    expect(inputThat(fixture).checked).toBeFalse();
  });
});
