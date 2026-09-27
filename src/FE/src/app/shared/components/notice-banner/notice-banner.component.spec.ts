import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NoticeBannerComponent } from './notice-banner.component';

/**
 * Design/Icons.md §5: tên icon sống ở TypeScript dưới dạng tên trần, template ghép `pi pi-<tên>`.
 * Cặp icon ↔ vai lấy đúng bảng §5 "Trạng thái và phản hồi".
 */
describe('NoticeBannerComponent — icon vai', () => {
  let fixture: ComponentFixture<NoticeBannerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NoticeBannerComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(NoticeBannerComponent);
  });

  function lopIcon(): string[] {
    const i = (fixture.nativeElement as HTMLElement).querySelector('.notice-banner__icon');
    return Array.from(i?.classList ?? []);
  }

  for (const [severity, lop] of [
    ['info', 'pi-info-circle'],
    ['success', 'pi-check-circle'],
    ['warning', 'pi-exclamation-triangle'],
    ['danger', 'pi-times-circle'],
  ] as const) {
    it(`severity=${severity} → lớp pi ${lop}, không có lớp trần và không nhân đôi tiền tố`, () => {
      fixture.componentRef.setInput('severity', severity);
      fixture.detectChanges();

      const lops = lopIcon();
      expect(lops).toContain('pi');
      expect(lops).toContain(lop);
      expect(lops).toContain('notice-banner__icon');
      expect(lops.filter((c) => c.startsWith('pi-pi-'))).toEqual([]);
      expect(lops).not.toContain(lop.slice('pi-'.length));
    });
  }

  it('icon là trang trí — nhãn nằm ở nội dung, nên nó vô hình với trình đọc màn hình', () => {
    fixture.detectChanges();
    const i = (fixture.nativeElement as HTMLElement).querySelector('.notice-banner__icon');
    expect(i?.getAttribute('aria-hidden')).toBe('true');
  });
});

@Component({
  standalone: true,
  imports: [NoticeBannerComponent],
  template: `<app-notice-banner severity="danger" size="sm">{{ cau }}</app-notice-banner>`,
})
class HostNhieuDong {
  cau = 'Dòng một\nDòng hai';
}

/**
 * quy-uoc/fe-ui-conventions.md §6.2: câu từ `applyFormFailure`/`fieldErrorsText` mang mỗi mã một
 * dòng, nối bằng `\n` — chỗ hiện phải giữ ngắt dòng, không để trình duyệt gộp thành một dòng.
 */
describe('NoticeBannerComponent — nội dung nhiều dòng', () => {
  it('thân banner giữ ngắt dòng của câu chiếu vào (white-space: pre-line)', async () => {
    await TestBed.configureTestingModule({
      imports: [HostNhieuDong],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    const fixture = TestBed.createComponent(HostNhieuDong);
    fixture.detectChanges();

    const than = (fixture.nativeElement as HTMLElement).querySelector('.notice-banner__than');
    expect(than).not.toBeNull();
    expect(getComputedStyle(than as Element).whiteSpace).toBe('pre-line');
    expect(than?.textContent).toContain('Dòng một\nDòng hai');
  });
});
