import { Component, TemplateRef, provideZonelessChangeDetection, viewChild } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TableColumnDef, TableComponent } from './table.component';

interface Hang {
  readonly id: string;
  readonly ten: string;
}

/** Chỉ để có một `TemplateRef` cho ô — `Table` không có bộ vẽ ô mặc định theo chuỗi. */
@Component({
  standalone: true,
  template: `<ng-template #o let-hang>{{ hang.ten }}</ng-template>`,
})
class OHost {
  readonly o =
    viewChild.required<TemplateRef<{ $implicit: Hang; column: TableColumnDef<Hang> }>>('o');
}

const HANG: readonly Hang[] = [
  { id: '1', ten: 'Một' },
  { id: '2', ten: 'Hai' },
];

/**
 * Design/Components/Table.md §Trạng thái `error` và §API: hàng lỗi là một hàng `colspan` chứa
 * `NoticeBanner` vai `danger` — tiêu đề `errorHeading`, thân `errorMessage` — cộng nút thử lại.
 * Hàng lỗi vẽ khi MỘT TRONG HAI input có giá trị: màn đặt tiêu đề cố định, thân có thể rỗng (lớp lỗi
 * xuyên suốt đã toast chi tiết — fe-api-client.md §2.2), và bảng vẫn phải báo hỏng.
 */
describe('TableComponent — trạng thái error (errorHeading / errorMessage)', () => {
  let fixture: ComponentFixture<TableComponent<Hang>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TableComponent, OHost],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    const host = TestBed.createComponent(OHost);
    host.detectChanges();
    const o = host.componentInstance.o();

    fixture = TestBed.createComponent(TableComponent<Hang>);
    fixture.componentRef.setInput('caption', 'Bảng thử');
    fixture.componentRef.setInput('rowKeyField', 'id');
    fixture.componentRef.setInput('columns', [
      { key: 'ten', header: 'Tên', cell: o },
      { key: 'khac', header: 'Khác', cell: o },
    ] satisfies TableColumnDef<Hang>[]);
    fixture.componentRef.setInput('rows', HANG);
    fixture.componentRef.setInput('retryLabel', 'Thử lại');
  });

  function goc(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function banner(): HTMLElement | null {
    return goc().querySelector('tbody app-notice-banner');
  }

  it('chỉ có errorHeading (thân rỗng) → VẪN vẽ hàng lỗi: tiêu đề banner, không thân, có nút thử lại', () => {
    fixture.componentRef.setInput('errorHeading', 'Không tải được bảng');
    fixture.detectChanges();

    expect(banner()).withContext('hàng lỗi phải có banner').not.toBeNull();
    expect(banner()?.querySelector('.notice-banner--danger')).not.toBeNull();
    expect(banner()?.querySelector('.notice-banner__tieu-de')?.textContent?.trim()).toBe(
      'Không tải được bảng',
    );
    expect(banner()?.querySelector('.notice-banner__than')?.textContent?.trim()).toBe('');
    expect(goc().querySelector('tbody app-button')?.textContent?.trim()).toBe('Thử lại');
    expect(goc().textContent).not.toContain('Một');
  });

  it('errorHeading + errorMessage → tiêu đề và thân cùng hiện trong MỘT banner', () => {
    fixture.componentRef.setInput('errorHeading', 'Không tải được bảng');
    fixture.componentRef.setInput('errorMessage', 'Câu dịch theo mã.');
    fixture.detectChanges();

    expect(goc().querySelectorAll('tbody app-notice-banner').length).toBe(1);
    expect(banner()?.querySelector('.notice-banner__tieu-de')?.textContent?.trim()).toBe(
      'Không tải được bảng',
    );
    expect(banner()?.querySelector('.notice-banner__than')?.textContent).toContain(
      'Câu dịch theo mã.',
    );
  });

  it('chỉ có errorMessage → hàng lỗi như trước, banner không có tiêu đề', () => {
    fixture.componentRef.setInput('errorMessage', 'Câu dịch theo mã.');
    fixture.detectChanges();

    expect(banner()?.querySelector('.notice-banner__tieu-de')).toBeNull();
    expect(banner()?.querySelector('.notice-banner__than')?.textContent).toContain(
      'Câu dịch theo mã.',
    );
  });

  it('hàng lỗi chiếm hết cột và <thead> giữ nguyên (Table.md: cấu trúc bảng là thông tin)', () => {
    fixture.componentRef.setInput('errorHeading', 'Không tải được bảng');
    fixture.detectChanges();

    expect(goc().querySelectorAll('thead th[scope="col"]').length).toBe(2);
    const o = goc().querySelector('tbody td');
    expect(o?.getAttribute('colspan')).toBe('2');
  });

  it('không có errorHeading lẫn errorMessage → vẽ hàng dữ liệu, không banner', () => {
    fixture.detectChanges();

    expect(banner()).toBeNull();
    expect(goc().querySelectorAll('tbody tr').length).toBe(2);
    expect(goc().textContent).toContain('Một');
  });

  it('bấm nút thử lại ở hàng lỗi → phát retry đúng một lần', () => {
    let soLan = 0;
    fixture.componentInstance.retry.subscribe(() => soLan++);
    fixture.componentRef.setInput('errorHeading', 'Không tải được bảng');
    fixture.detectChanges();

    goc().querySelector<HTMLButtonElement>('tbody app-button button')?.click();

    expect(soLan).toBe(1);
  });
});
