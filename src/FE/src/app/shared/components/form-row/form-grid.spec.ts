import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

/**
 * Lớp `form-grid` khai ở `src/styles/styles.scss` (fe-ui-conventions.md §4.3): tối đa hai cột, số
 * cột theo bề rộng THẬT của vùng chứa — đủ rộng (Dialog `lg`) thì hai cột, hẹp (Dialog `md`) thì một.
 */
@Component({
  standalone: true,
  template: `
    <div id="hep" style="width: 600px">
      <form class="form-grid">
        <div>a</div>
        <div>b</div>
        <div>c</div>
      </form>
    </div>
    <div id="rong" style="width: 900px">
      <form class="form-grid">
        <div>a</div>
        <div>b</div>
        <div>c</div>
      </form>
    </div>
    <div id="rat-rong" style="width: 1400px">
      <form class="form-grid">
        <div>a</div>
        <div>b</div>
        <div>c</div>
      </form>
    </div>
  `,
})
class HostComponent {}

describe('lớp form-grid', () => {
  function cot(id: string): number {
    const form = document.querySelector(`#${id} form`)!;
    return getComputedStyle(form).gridTemplateColumns.split(' ').length;
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideZonelessChangeDetection()],
    });
    const fixture = TestBed.createComponent(HostComponent);
    document.body.appendChild(fixture.nativeElement);
    fixture.detectChanges();
  });

  it('có định nghĩa: là lưới (display: grid)', () => {
    expect(getComputedStyle(document.querySelector('#hep form')!).display).toBe('grid');
  });

  it('vùng chứa 600px (Dialog md) → một cột', () => {
    expect(cot('hep')).toBe(1);
  });

  it('vùng chứa 900px → hai cột', () => {
    expect(cot('rong')).toBe(2);
  });

  it('vùng chứa 1400px → vẫn tối đa hai cột', () => {
    expect(cot('rat-rong')).toBe(2);
  });
});
