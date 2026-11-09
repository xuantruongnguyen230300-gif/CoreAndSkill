import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormRowComponent } from './form-row.component';

/** DESIGN.md §4: nhãn → ô `--sp-4` (8px), ô → dòng lỗi/gợi ý `--sp-3` (6px). */
describe('FormRowComponent — hai khe tách riêng', () => {
  let fixture: ComponentFixture<FormRowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormRowComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(FormRowComponent);
    fixture.componentRef.setInput('label', 'Nhãn');
    fixture.detectChanges();
  });

  const rect = (sel: string): DOMRect =>
    (fixture.nativeElement as HTMLElement).querySelector(sel)!.getBoundingClientRect();

  it('khe nhãn → ô 8px, ô → dòng lỗi 6px', () => {
    expect(getComputedStyle(document.documentElement).fontSize).withContext('rem gốc').toBe('16px');
    fixture.componentRef.setInput('error', 'Sai rồi');
    fixture.detectChanges();
    expect(rect('.form-row__o').top - rect('.form-row__nhan').bottom).toBeCloseTo(8, 1);
    expect(rect('.form-row__loi').top - rect('.form-row__o').bottom).toBeCloseTo(6, 1);
  });

  it('khe ô → dòng gợi ý cũng 6px', () => {
    fixture.componentRef.setInput('hint', 'Gợi ý');
    fixture.detectChanges();
    expect(rect('.form-row__goi-y').top - rect('.form-row__o').bottom).toBeCloseTo(6, 1);
  });
});
