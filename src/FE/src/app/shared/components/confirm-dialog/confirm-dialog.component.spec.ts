import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { ConfirmDialogComponent } from './confirm-dialog.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/**
 * Design/Icons.md §5: tên icon sống ở TypeScript dưới dạng tên trần, template ghép `pi pi-<tên>`.
 * Cặp icon ↔ mức lấy đúng bảng §5 "Trạng thái và phản hồi": hỏi xác nhận `pi-question-circle`,
 * mức nguy hiểm `pi-exclamation-triangle`.
 */
describe('ConfirmDialogComponent — icon theo mức', () => {
  let fixture: ComponentFixture<ConfirmDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmDialogComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ConfirmDialogComponent);
    fixture.componentRef.setInput('open', true);
    fixture.componentRef.setInput('title', 'Tiêu đề');
    fixture.componentRef.setInput('message', 'Nội dung');
    fixture.componentRef.setInput('confirmLabel', 'Đồng ý');
  });

  function lopIcon(): string[] {
    const i = (fixture.nativeElement as HTMLElement).querySelector('.confirm-dialog__icon-vong i');
    return Array.from(i?.classList ?? []);
  }

  for (const [severity, lop] of [
    ['ask', 'pi-question-circle'],
    ['warning', 'pi-exclamation-triangle'],
    ['danger', 'pi-exclamation-triangle'],
  ] as const) {
    it(`severity=${severity} → lớp pi ${lop}, không nhân đôi tiền tố`, () => {
      fixture.componentRef.setInput('severity', severity);
      fixture.detectChanges();

      const lops = lopIcon();
      expect(lops).toContain('pi');
      expect(lops).toContain(lop);
      expect(lops.filter((c) => c.startsWith('pi-pi-'))).toEqual([]);
      expect(lops).not.toContain(lop.slice('pi-'.length));
    });
  }
});

/**
 * Ô "tôi đã hiểu" (`requireAcknowledge`) là trạng thái của MỘT lần mở: lần mở sau phải bắt đầu từ chưa
 * đánh dấu, nếu không nút xác nhận mở sẵn cho một hành động nguy hiểm mà người dùng chưa đọc lại.
 */
describe('ConfirmDialogComponent — ô xác nhận đã hiểu', () => {
  let fixture: ComponentFixture<ConfirmDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmDialogComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ConfirmDialogComponent);
    fixture.componentRef.setInput('title', 'Tiêu đề');
    fixture.componentRef.setInput('message', 'Nội dung');
    fixture.componentRef.setInput('confirmLabel', 'Đồng ý');
    fixture.componentRef.setInput('requireAcknowledge', 'Tôi đã hiểu');
  });

  async function datOpen(mo: boolean): Promise<void> {
    fixture.componentRef.setInput('open', mo);
    fixture.detectChanges();
    await fixture.whenStable();
  }

  const oXacNhan = (): HTMLInputElement =>
    (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      '.confirm-dialog__xac-nhan input',
    )!;
  const nutDongY = (): HTMLButtonElement =>
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '.confirm-dialog__hanh-dong app-button:last-child button',
    )!;

  it('mở: ô chưa đánh dấu và nút xác nhận khoá', async () => {
    await datOpen(true);
    expect(oXacNhan().checked).toBeFalse();
    expect(nutDongY().disabled).toBeTrue();
  });

  it('đánh dấu thì mở khoá nút; đóng rồi mở lại thì về chưa đánh dấu và khoá lại', async () => {
    await datOpen(true);
    oXacNhan().click();
    fixture.detectChanges();
    expect(oXacNhan().checked).toBeTrue();
    expect(nutDongY().disabled).toBeFalse();

    await datOpen(false);
    await datOpen(true);
    expect(oXacNhan().checked).toBeFalse();
    expect(nutDongY().disabled).toBeTrue();
  });
});
