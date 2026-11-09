import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, Subject, of } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import { VaiTroService } from '../../../vai-tro/services/vai-tro.service';
import { NguoiDung } from '../../models/nguoi-dung.model';
import { NguoiDungService } from '../../services/nguoi-dung.service';
import { ChiTietNguoiDungPage } from './chi-tiet-nguoi-dung.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const NGUOI_DUNG: NguoiDung = {
  id: 'u1',
  userName: 'an.nguyen',
  email: 'an@b.vn',
  fullName: 'Nguyễn An',
  roles: [],
  isLocked: false,
  lockoutEnd: null,
  lockedByAdmin: false,
  mustChangePassword: false,
  createdAt: null,
  version: 'v-7',
};

/** Screens/10-nguoi-dung.md §Chi tiết · đang tải: PageHeader `loading` + hai `Card` `loading`. */
describe('ChiTietNguoiDungPage — đang tải', () => {
  it('chưa có dữ liệu → hai Card ở trạng thái loading (aria-busy) kèm skeleton', async () => {
    const chiTiet = new Subject<NguoiDung>();
    const service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', ['chiTiet']);
    service.chiTiet.and.returnValue(chiTiet);
    await TestBed.configureTestingModule({
      imports: [ChiTietNguoiDungPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: NguoiDungService, useValue: service },
        {
          provide: AuthService,
          useValue: { nguoiDung: signal({ id: 'x' }), coQuyen: () => true },
        },
        {
          provide: VaiTroService,
          useValue: jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem']),
        },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'u1' }) } },
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(ChiTietNguoiDungPage);
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    const card = el.querySelectorAll('app-card [aria-busy="true"]');
    expect(card.length).toBe(2);
    expect(el.querySelectorAll('app-card .skeleton-loader').length).toBeGreaterThan(0);
  });

  it('tải lại sau thao tác khi đã có dữ liệu → giữ nguyên hai thẻ, không thay bằng skeleton (Screens/10 §Tải lại)', async () => {
    const lanDau = new Subject<NguoiDung>();
    const lanHai = new Subject<NguoiDung>();
    const service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', ['chiTiet']);
    service.chiTiet.and.returnValues(lanDau, lanHai);
    await TestBed.configureTestingModule({
      imports: [ChiTietNguoiDungPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: NguoiDungService, useValue: service },
        {
          provide: AuthService,
          useValue: { nguoiDung: signal({ id: 'x' }), coQuyen: () => true },
        },
        {
          provide: VaiTroService,
          useValue: jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem']),
        },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'u1' }) } },
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(ChiTietNguoiDungPage);
    fixture.detectChanges();
    lanDau.next(NGUOI_DUNG);
    lanDau.complete();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const theVaiTro = el.querySelector('app-the-vai-tro-nguoi-dung');
    expect(theVaiTro).not.toBeNull();

    // `xong` của store gọi lại đúng đường này sau mỗi lần ghi.
    (fixture.componentInstance as unknown as { thuLaiSauXungDot(): void }).thuLaiSauXungDot();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(service.chiTiet).toHaveBeenCalledTimes(2);
    expect(el.querySelector('app-the-vai-tro-nguoi-dung')).toBe(theVaiTro);
    expect(el.querySelector('app-the-thong-tin-nguoi-dung')).not.toBeNull();
    expect(el.querySelectorAll('app-card .skeleton-loader').length).toBe(0);
  });
});
