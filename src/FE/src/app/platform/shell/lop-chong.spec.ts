import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { providePrimeNG } from 'primeng/config';
import { ZIndexUtils } from 'primeng/utils';
import { Observable, of } from 'rxjs';

import { ToastService } from '../../core/toast/toast.service';
import { CORE_PRIME_Z_INDEX } from '../../core/theme/prime-z-index';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { DialogComponent } from '../../shared/ui/dialog/dialog.component';
import { ToastComponent } from '../../shared/ui/toast/toast.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

@Component({
  selector: 'app-thu-lop-chong',
  standalone: true,
  imports: [DialogComponent, ConfirmDialogComponent, ToastComponent],
  template: `
    <app-dialog [open]="true" title="Hộp"><p>nội dung</p></app-dialog>
    <app-confirm-dialog
      [open]="xacNhan"
      title="Bỏ thay đổi?"
      message="Mất dữ liệu"
      confirmLabel="Bỏ"
      cancelLabel="Ở lại"
    />
    <app-toast />
  `,
})
class ThuLopChongComponent {
  xacNhan = false;
}

/**
 * Thang lớp chồng duy nhất `--z-*` (Design/DESIGN.md §6.4): Dialog + mặt nạ < ConfirmDialog <
 * Toast. Chạy trên Chrome thật nên `getComputedStyle` và `elementFromPoint` là giá trị thật —
 * không mô phỏng. `ThuLopChongComponent` là chỗ mở Dialog rồi mở ConfirmDialog ngay trên nó,
 * đúng khuôn "đang sửa mà bấm đóng → hỏi bỏ thay đổi".
 */
describe('Lớp chồng — ConfirmDialog nằm trên Dialog và mặt nạ của nó', () => {
  let fixture: ComponentFixture<ThuLopChongComponent>;

  const z = (el: Element | null): number => {
    expect(el).withContext('thiếu phần tử').not.toBeNull();
    return Number(getComputedStyle(el as Element).zIndex);
  };

  // Bộ đếm z-index của PrimeNG là trạng thái toàn cục của module: Dialog ở spec khác bị huỷ khi
  // còn mở thì không trả số đếm, nên kết quả tuyệt đối phụ thuộc thứ tự chạy. Trả hết bằng API
  // công khai (`getCurrent` + `revertZIndex`) để mỗi ca bắt đầu từ 0.
  const datLaiBoDemZIndex = (): void => {
    for (let cur = ZIndexUtils.getCurrent(); cur > 0; cur = ZIndexUtils.getCurrent()) {
      ZIndexUtils.revertZIndex(cur);
    }
  };

  beforeEach(async () => {
    // Hàng rào: ca dưới đo bằng `elementFromPoint` trên cả `document`, nên lớp phủ do spec khác bỏ
    // lại ở <body> làm ca đỏ mơ hồ. Rò thì đỏ ngay ở đây, nêu đúng lớp phủ — spec rò phải tự dọn.
    const roTuSpecKhac = Array.from(
      document.body.querySelectorAll(
        '.p-dialog-mask, .p-drawer-mask, .p-overlay-mask, .p-toast, .confirm-dialog__backdrop',
      ),
    ).map((e) => e.className);
    expect(roTuSpecKhac).withContext('lớp phủ còn sót từ spec trước').toEqual([]);
    datLaiBoDemZIndex();
    await TestBed.configureTestingModule({
      imports: [ThuLopChongComponent],
      providers: [
        provideZonelessChangeDetection(),
        providePrimeNG({ zIndex: CORE_PRIME_Z_INDEX }),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ThuLopChongComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentInstance.xacNhan = true;
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  afterEach(() => {
    datLaiBoDemZIndex();
    document.body.style.overflow = '';
  });

  it('z-index tính ra: mặt nạ Dialog < hộp Dialog < nền = hộp ConfirmDialog < Toast', () => {
    const matNa = document.querySelector('.p-dialog-mask');
    const hopDialog = document.querySelector('.app-dialog');
    const nenXacNhan = document.querySelector('.confirm-dialog__backdrop');
    const hopXacNhan = document.querySelector('.confirm-dialog');
    const toast = document.querySelector('p-toast');

    expect(z(matNa)).toBeLessThan(z(hopDialog));
    expect(z(hopDialog)).toBeLessThan(z(nenXacNhan));
    // Nền và hộp cùng `--z-dialog`; hộp đứng sau trong DOM nên nằm trên (nút ở ca dưới chứng minh).
    expect(z(nenXacNhan)).toBe(z(hopXacNhan));
    expect(z(hopXacNhan)).toBeLessThan(z(toast));
  });

  it('tâm nút xác nhận của ConfirmDialog bấm được: phần tử trên cùng ở đó chính là nút', () => {
    const nut = document.querySelector<HTMLButtonElement>('.confirm-dialog__hanh-dong button');
    expect(nut).not.toBeNull();
    const r = (nut as HTMLElement).getBoundingClientRect();
    const tren = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
    expect(tren === nut || (nut as HTMLElement).contains(tren)).toBeTrue();
  });

  // Toast.md dòng `--z-toast`: `z-index` trên phần tử chủ `p-toast` tĩnh không có tác dụng — toast
  // phải nằm trên cả Dialog lẫn ConfirmDialog, kể cả ở điểm ảnh chồng lên hộp.
  it('toast lỗi nằm TRÊN Dialog và ConfirmDialog: z-index tính ra và phần tử trên cùng tại tâm toast', async () => {
    TestBed.inject(ToastService).loi('Lỗi thử lớp chồng');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const hop = document.querySelector('.p-toast-message') as HTMLElement | null;
    expect(hop).withContext('toast chưa được vẽ').not.toBeNull();
    const lop = (hop as HTMLElement).closest('.p-toast') as HTMLElement;
    expect(getComputedStyle(lop).position).not.toBe('static');
    expect(z(lop)).toBeGreaterThan(z(document.querySelector('.confirm-dialog')));
    expect(z(lop)).toBeGreaterThan(z(document.querySelector('.app-dialog')));

    const r = (hop as HTMLElement).getBoundingClientRect();
    const tren = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
    expect(lop.contains(tren)).toBeTrue();
  });
});
