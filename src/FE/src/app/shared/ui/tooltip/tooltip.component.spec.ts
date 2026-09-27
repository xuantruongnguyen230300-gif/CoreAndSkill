import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';

import { CheckComponent } from '../check/check.component';
import { TOOLTIP_DELAY_CHUOT_MS, TooltipComponent } from './tooltip.component';

@Component({
  standalone: true,
  imports: [TooltipComponent],
  template: `
    <app-tooltip [text]="chu()" [disabled]="tat()">
      <button id="neo" type="button">Nhan</button>
    </app-tooltip>
  `,
})
class HostComponent {
  readonly chu = signal('Can quyen duyet chung tu');
  readonly tat = signal(false);
}

@Component({
  standalone: true,
  imports: [TooltipComponent],
  template: `
    <app-tooltip text="Hai phan tu">
      <button id="mot" type="button">Mot</button>
      <button id="hai" type="button">Hai</button>
    </app-tooltip>
  `,
})
class HostHaiPhanTuComponent {}

/** Phần tử neo là một component Core tự dựng `<input>` bên trong, và nơi gọi ĐÃ nối `describedBy`. */
@Component({
  standalone: true,
  imports: [TooltipComponent, CheckComponent],
  template: `
    <app-tooltip #tip="appTooltip" text="O bi khoa boi he thong">
      <app-check type="checkbox" [describedBy]="tip.idMoTa" ariaLabel="O" />
    </app-tooltip>
  `,
})
class HostComponentCoreNoiDuComponent {}

/** Cùng ca trên nhưng nơi gọi QUÊN nối — phải báo lúc phát triển, không im lặng. */
@Component({
  standalone: true,
  imports: [TooltipComponent, CheckComponent],
  template: `
    <app-tooltip text="O bi khoa boi he thong">
      <app-check type="checkbox" ariaLabel="O" />
    </app-tooltip>
  `,
})
class HostComponentCoreQuenNoiComponent {}

function hopNoi(): HTMLElement | null {
  return document.querySelector<HTMLElement>('.app-tooltip');
}

/**
 * Design/Components/Tooltip.md. Ba hành vi dưới đây là phần Core TỰ viết, không phải thứ thư viện
 * cho sẵn — mỗi cái đi vòng qua đúng một giới hạn đã ghi trong spec:
 *   - bàn phím: thư viện gắn `focus`/`blur` lên chính host mà `focus` KHÔNG nổi bọt, nên phần tử
 *     chiếu vào nhận focus thì đường của thư viện không bắn gì;
 *   - `aria-describedby`: hộp nổi của thư viện không mang `id` nào để trỏ tới;
 *   - `Escape`: `hideOnEscape` của thư viện chỉ đăng ký lắng nghe bên trong nhánh mở CÓ độ trễ,
 *     mà đường bàn phím cố ý không đi qua nhánh đó.
 */
describe('TooltipComponent — lớp bọc, hai kênh tách nhau', () => {
  let fixture: ComponentFixture<HostComponent>;

  async function dungXong(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    // `afterNextRender` đọc nội dung chiếu vào — chỉ chạy khi ứng dụng render xong một lượt.
    TestBed.tick();
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function neo(): HTMLElement {
    const e = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('#neo');
    if (!e) throw new Error('không thấy phần tử neo');
    return e;
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideZonelessChangeDetection(), provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    await dungXong();
  });

  afterEach(() => {
    fixture.destroy();
    document.querySelectorAll('.p-tooltip').forEach((e) => e.remove());
  });

  it('nút bên trong nhận focus bàn phím → hộp nổi hiện NGAY (không chờ delay)', () => {
    expect(hopNoi()).withContext('chưa focus thì chưa có hộp').toBeNull();

    neo().dispatchEvent(new FocusEvent('focusin', { bubbles: true }));

    expect(hopNoi()).withContext('focusin nổi bọt tới host → lớp bọc mở ngay').not.toBeNull();
    expect(hopNoi()?.classList.contains('app-tooltip--ban-phim')).toBeTrue();
  });

  it('hộp nổi nhận khối override của Core — nền và trần bề rộng đi từ token, không từ mặc định thư viện', () => {
    neo().dispatchEvent(new FocusEvent('focusin', { bubbles: true }));
    const hop = hopNoi();
    if (!hop) throw new Error('hộp nổi chưa hiện');

    // Cả chuỗi phải nối liền: `tooltipStyleClass` → lớp `.app-tooltip` trên hộp thật → khối
    // override ở styles/_thu-vien.scss → token của Design. Đứt ở bất kỳ mắt nào thì biến rơi về
    // mặc định Aura (`{surface.700}`), khác hẳn hai giá trị dưới.
    const cuaHop = (ten: string) => getComputedStyle(hop).getPropertyValue(ten).trim();
    const cuaGoc = (ten: string) =>
      getComputedStyle(document.documentElement).getPropertyValue(ten).trim();

    expect(cuaGoc('--color-inverse-surface')).withContext('token phải có giá trị').not.toBe('');
    expect(cuaHop('--p-tooltip-background')).toBe(cuaGoc('--color-inverse-surface'));
    expect(cuaHop('--p-tooltip-color')).toBe(cuaGoc('--color-inverse-text'));
    expect(cuaHop('--p-tooltip-max-width')).toBe(cuaGoc('--layout-tooltip-w-max'));
  });

  it('rê chuột thì PHẢI chờ — cùng một hộp, hai nguồn mở khác nhau', () => {
    (fixture.nativeElement as HTMLElement)
      .querySelector('app-tooltip')
      ?.dispatchEvent(new MouseEvent('mouseenter'));

    expect(hopNoi()).withContext(`chuột chờ ${TOOLTIP_DELAY_CHUOT_MS}ms rồi mới hiện`).toBeNull();
  });

  it('mặc định của input `delay` là hằng số trỏ về spec, không phải một con số rải trong template', () => {
    const tooltip = fixture.debugElement.query((de) => de.name === 'app-tooltip')
      .componentInstance as TooltipComponent;

    expect(tooltip.delay()).toBe(TOOLTIP_DELAY_CHUOT_MS);
  });

  it('`aria-describedby` của phần tử neo trỏ đúng phần tử mô tả — không trỏ vào hộp nổi', () => {
    const id = neo().getAttribute('aria-describedby');
    expect(id).withContext('phần tử neo phải được trỏ').not.toBeNull();

    const moTa = document.getElementById(id ?? '');
    expect(moTa)
      .withContext('phần tử mô tả phải tồn tại kể cả khi hộp nổi chưa hiện')
      .not.toBeNull();
    expect(moTa?.getAttribute('role')).toBe('tooltip');
    expect(moTa?.classList.contains('sr-only')).toBeTrue();
    expect(moTa?.textContent?.trim()).toBe('Can quyen duyet chung tu');
    // Kênh nhìn và kênh nghe tách hẳn: phần tử mô tả nằm TRONG lớp bọc, không phải hộp nổi.
    expect(moTa?.closest('app-tooltip')).not.toBeNull();
  });

  it('Escape đóng hộp nổi mà KHÔNG lấy mất focus khỏi phần tử neo', () => {
    const nut = neo();
    nut.focus();
    nut.dispatchEvent(new FocusEvent('focusin', { bubbles: true }));
    expect(hopNoi()).not.toBeNull();

    nut.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(hopNoi()).withContext('Escape đóng').toBeNull();
    expect(document.activeElement).withContext('focus ở lại trên phần tử neo').toBe(nut);
  });

  it('rời focus ra ngoài lớp bọc → hộp nổi đóng', () => {
    const nut = neo();
    nut.dispatchEvent(new FocusEvent('focusin', { bubbles: true }));
    expect(hopNoi()).not.toBeNull();

    nut.dispatchEvent(new FocusEvent('focusout', { bubbles: true, relatedTarget: document.body }));

    expect(hopNoi()).toBeNull();
  });

  it('`tooltipEvent` giữ mặc định `hover` — thư viện KHÔNG gắn thêm cặp focus/blur lên host', () => {
    // `focus` không nổi bọt: bắn thẳng lên host là cách duy nhất chạm được listener của thư viện
    // nếu nó có. Không có hộp nào hiện ra nghĩa là chỉ có một đường mở, đúng chốt ở §Bàn phím.
    (fixture.nativeElement as HTMLElement)
      .querySelector('app-tooltip')
      ?.dispatchEvent(new FocusEvent('focus'));

    expect(hopNoi()).toBeNull();
  });

  it('nội dung rỗng → không dựng gì: không phần tử mô tả, focus không mở hộp', async () => {
    fixture.componentInstance.chu.set('   ');
    await dungXong();

    expect(neo().getAttribute('aria-describedby')).toBeNull();
    expect(document.querySelector('app-tooltip [role="tooltip"]')).toBeNull();

    neo().dispatchEvent(new FocusEvent('focusin', { bubbles: true }));
    expect(hopNoi()).toBeNull();
  });

  it('`disabled` tắt cả hai kênh, và bật lại được mà không phải gỡ khỏi template', async () => {
    fixture.componentInstance.tat.set(true);
    await dungXong();
    neo().dispatchEvent(new FocusEvent('focusin', { bubbles: true }));
    expect(hopNoi()).toBeNull();
    expect(neo().getAttribute('aria-describedby')).toBeNull();

    fixture.componentInstance.tat.set(false);
    await dungXong();
    neo().dispatchEvent(new FocusEvent('focusin', { bubbles: true }));
    expect(hopNoi()).not.toBeNull();
    expect(neo().getAttribute('aria-describedby')).not.toBeNull();
  });

  it('host có hộp riêng (`inline-flex`) — `display: contents` làm hình chữ nhật neo rỗng', () => {
    const host = (fixture.nativeElement as HTMLElement).querySelector('app-tooltip');
    expect(host && getComputedStyle(host).display).toBe('inline-flex');
  });
});

/**
 * §Accessibility, "Phần tử neo là một component Core" — đã chốt 2026-09-23. `aria-describedby`
 * phải nằm trên phần tử NHẬN FOCUS. Với `<app-check>` thì phần tử đó là `<input>` bên trong, nên
 * lớp bọc thôi ghi lên host và nhường quyền ghi cho input `describedBy` của chính component neo.
 */
describe('TooltipComponent — phần tử neo là một component Core', () => {
  async function dung<T>(loai: new () => T): Promise<ComponentFixture<T>> {
    await TestBed.configureTestingModule({
      imports: [loai as never],
      providers: [provideZonelessChangeDetection(), provideNoopAnimations()],
    }).compileComponents();
    const fixture = TestBed.createComponent(loai);
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  afterEach(() => {
    document.querySelectorAll('.p-tooltip').forEach((e) => e.remove());
  });

  it('lớp bọc KHÔNG ghi lên host component; `id` đi tới `<input>` thật qua `describedBy`', async () => {
    const fixture = await dung(HostComponentCoreNoiDuComponent);
    const goc = fixture.nativeElement as HTMLElement;
    const hostCheck = goc.querySelector<HTMLElement>('app-check');
    const tooltip = fixture.debugElement.query((de) => de.name === 'app-tooltip')
      .componentInstance as TooltipComponent;

    expect(hostCheck)
      .withContext('ca này chỉ có nghĩa khi phần tử neo đúng là host của component')
      .not.toBeNull();
    expect(hostCheck?.getAttribute('aria-describedby'))
      .withContext('thuộc tính rơi lên host là ca hỏng mà spec cấm — focus không ở đây')
      .toBeNull();

    // Chuỗi phải nối liền tới phần tử NHẬN FOCUS thật.
    const oNhanFocus = goc.querySelector<HTMLElement>('input');
    expect(oNhanFocus).withContext('app-check dựng <input> bên trong').not.toBeNull();
    expect(oNhanFocus?.getAttribute('aria-describedby')).toBe(tooltip.idMoTa);

    // Và `id` đó phải trỏ tới phần tử mô tả có thật.
    const moTa = document.getElementById(tooltip.idMoTa);
    expect(moTa?.getAttribute('role')).toBe('tooltip');
    expect(moTa?.textContent?.trim()).toBe('O bi khoa boi he thong');

    fixture.destroy();
  });

  it('nơi gọi QUÊN nối `describedBy` → báo lúc phát triển, không im lặng', async () => {
    const canh = spyOn(console, 'warn');
    const fixture = await dung(HostComponentCoreQuenNoiComponent);
    const goc = fixture.nativeElement as HTMLElement;

    expect(goc.querySelector('app-check')?.getAttribute('aria-describedby'))
      .withContext('vẫn không được ghi bù lên host — ghi bù là đúng ca hỏng spec cấm')
      .toBeNull();
    expect(goc.querySelector('input')?.getAttribute('aria-describedby'))
      .withContext('không ai nối thì không phần tử nào mang id')
      .toBeNull();
    expect(canh)
      .withContext('một lời chú rơi khỏi trình đọc màn hình là thứ không ai nhìn thấy')
      .toHaveBeenCalled();

    fixture.destroy();
  });
});

describe('TooltipComponent — nội dung chiếu vào không đúng một phần tử', () => {
  it('không dựng tooltip nào và báo lỗi lúc phát triển', async () => {
    const loi = spyOn(console, 'error');
    await TestBed.configureTestingModule({
      imports: [HostHaiPhanTuComponent],
      providers: [provideZonelessChangeDetection(), provideNoopAnimations()],
    }).compileComponents();
    const fixture = TestBed.createComponent(HostHaiPhanTuComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
    await fixture.whenStable();

    const nut = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('#mot');
    nut?.dispatchEvent(new FocusEvent('focusin', { bubbles: true }));

    expect(hopNoi())
      .withContext('im lặng bỏ qua là cách tooltip biến mất không ai biết')
      .toBeNull();
    expect(nut?.getAttribute('aria-describedby')).toBeNull();
    expect(loi).toHaveBeenCalled();

    fixture.destroy();
    document.querySelectorAll('.p-tooltip').forEach((e) => e.remove());
  });
});
