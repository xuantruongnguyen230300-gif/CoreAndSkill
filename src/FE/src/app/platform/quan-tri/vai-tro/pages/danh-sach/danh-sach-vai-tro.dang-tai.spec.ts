import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { NEVER, Observable, of } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import { VaiTroService } from '../../services/vai-tro.service';
import { DanhSachVaiTroPage } from './danh-sach-vai-tro.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/** DataTable.md §Trạng thái `loading` — lần đầu: skeleton dạng hàng, ĐÚNG số dòng của trang. */
describe('DanhSachVaiTroPage — skeleton lần đầu khớp số dòng mỗi trang', () => {
  let fixture: ComponentFixture<DanhSachVaiTroPage>;
  let el: HTMLElement;

  async function dung(url: string): Promise<void> {
    const service = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['danhSach']);
    service.danhSach.and.returnValue(NEVER);
    await TestBed.configureTestingModule({
      imports: [DanhSachVaiTroPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: VaiTroService, useValue: service },
        {
          provide: AuthService,
          useValue: {
            nguoiDung: signal({ id: 'x', tenantName: 'Cty A' }),
            coQuyen: () => true,
          },
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    await TestBed.inject(Router).navigateByUrl(url);
    fixture = TestBed.createComponent(DanhSachVaiTroPage);
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
