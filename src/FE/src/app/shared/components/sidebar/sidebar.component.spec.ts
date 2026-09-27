import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateLoader, TranslationObject, provideTranslateLoader, provideTranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { NavItem, SidebarComponent } from './sidebar.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/** Mục cha vừa có route vừa có children — đúng ca gây lỗi F1 (core-reviewer FE). */
const MENU_CHA_CO_ROUTE: readonly NavItem[] = [
  {
    id: 'quan-tri',
    label: 'Quản trị',
    route: '/quan-tri',
    children: [{ id: 'nguoi-dung', label: 'Người dùng', route: '/quan-tri/nguoi-dung' }],
  },
];

describe('SidebarComponent', () => {
  let fixture: ComponentFixture<SidebarComponent>;
  let component: SidebarComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SidebarComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideTranslateService({ lang: 'vi', fallbackLang: 'vi', loader: provideTranslateLoader(FakeTranslateLoader) }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;
  });

  function layDanhSachAriaCurrentPage(): HTMLAnchorElement[] {
    return Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLAnchorElement>('a[aria-current="page"]'));
  }

  it('cha có cả route và children — khi đang ở route CON (tiền tố của cha), chỉ mục CON nhận aria-current="page"', () => {
    fixture.componentRef.setInput('items', MENU_CHA_CO_ROUTE);
    fixture.componentRef.setInput('activeRoute', '/quan-tri/nguoi-dung');
    fixture.detectChanges();

    const mucSang = layDanhSachAriaCurrentPage();
    expect(mucSang.length).toBe(1);
    expect(mucSang[0].textContent).toContain('Người dùng');
  });

  it('cha có cả route và children — khi đang ĐÚNG route của cha, chỉ mục CHA nhận aria-current="page"', () => {
    fixture.componentRef.setInput('items', MENU_CHA_CO_ROUTE);
    fixture.componentRef.setInput('activeRoute', '/quan-tri');
    fixture.detectChanges();

    const mucSang = layDanhSachAriaCurrentPage();
    expect(mucSang.length).toBe(1);
    expect(mucSang[0].textContent).toContain('Quản trị');
  });

  it('mục lá (không children) vẫn khớp tiền tố — active khi đang ở một tuyến con của nó', () => {
    fixture.componentRef.setInput('items', [{ id: 'ho-so', label: 'Hồ sơ', route: '/ho-so' } satisfies NavItem]);
    fixture.componentRef.setInput('activeRoute', '/ho-so/doi-mat-khau');
    fixture.detectChanges();

    const mucSang = layDanhSachAriaCurrentPage();
    expect(mucSang.length).toBe(1);
    expect(mucSang[0].textContent).toContain('Hồ sơ');
  });

  it('dangOCha trả false khi mục không có route', () => {
    expect(component['dangOCha']({ id: 'nhom', label: 'Nhóm' })).toBeFalse();
  });
});
