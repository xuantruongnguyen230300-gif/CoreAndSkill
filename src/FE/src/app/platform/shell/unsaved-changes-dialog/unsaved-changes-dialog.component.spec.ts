import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { UnsavedChangesDialogComponent } from './unsaved-changes-dialog.component';
import { UnsavedChangesService } from '../../../core/unsaved-changes/unsaved-changes.service';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

describe('UnsavedChangesDialogComponent', () => {
  let fixture: ComponentFixture<UnsavedChangesDialogComponent>;
  let service: UnsavedChangesService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UnsavedChangesDialogComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(UnsavedChangesDialogComponent);
    service = TestBed.inject(UnsavedChangesService);
    fixture.detectChanges();
  });

  function hopDangMo(): boolean {
    return (fixture.nativeElement as HTMLElement).querySelector('.confirm-dialog') !== null;
  }

  it('service chưa hỏi → không vẽ hộp', () => {
    expect(hopDangMo()).toBeFalse();
  });

  it('service đang hỏi (dangHoi true) → hộp hiện ra', () => {
    service.dangKy(() => true);
    void service.xinRoiTrang();
    fixture.detectChanges();

    expect(hopDangMo()).toBeTrue();
  });

  it('bấm "Rời đi" trong hộp → gọi xacNhanRoiDi() của service, hộp đóng', async () => {
    service.dangKy(() => true);
    const cho = service.xinRoiTrang();
    fixture.detectChanges();

    const nutXacNhan = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '.confirm-dialog__hanh-dong .nut--primary',
    );
    nutXacNhan?.click();
    fixture.detectChanges();

    expect(await cho).toBeTrue();
    expect(hopDangMo()).toBeFalse();
  });
});
