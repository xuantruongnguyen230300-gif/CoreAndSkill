import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { NEVER, Observable, of } from 'rxjs';

import { DonViService } from '../../services/don-vi.service';
import { DanhSachDonViPage } from './danh-sach-don-vi.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/** DataTable.md §Trạng thái `loading` — lần đầu: skeleton dạng hàng, ĐÚNG số dòng của trang. */
describe('DanhSachDonViPage — skeleton lần đầu khớp số dòng mỗi trang', () => {
  let fixture: ComponentFixture<DanhSachDonViPage>;
  let el: HTMLElement;

  async function dung(url: string): Promise<void> {
    const service = jasmine.createSpyObj<DonViService>('DonViService', ['danhSach']);
    service.danhSach.and.returnValue(NEVER);
    await TestBed.configureTestingModule({
      imports: [DanhSachDonViPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: DonViService, useValue: service },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    await TestBed.inject(Router).navigateByUrl(url);
    fixture = TestBed.createComponent(DanhSachDonViPage);
    el = fixture.nativeElement as HTMLElement;
    document.body.appendChild(el);
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
  }

  afterEach(() => el.remove());

  it('mặc định (URL không mang pageSize): 20 vạch', async () => {
    await dung('/');
    expect(el.querySelectorAll('tbody .skeleton-loader').length).toBe(20);
  });

  it('URL mang pageSize=50: 50 vạch', async () => {
    await dung('/?pageSize=50');
    expect(el.querySelectorAll('tbody .skeleton-loader').length).toBe(50);
  });
});
