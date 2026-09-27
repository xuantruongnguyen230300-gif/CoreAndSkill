import localeVi from '@angular/common/locales/vi';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { provideCoreI18n } from '../../core/config/core-i18n';
import { HasPermissionDirective } from './has-permission.directive';
import { AuthService } from '../../core/auth/auth.service';
import { NguoiDungHienTai } from '../../core/auth/nguoi-dung-hien-tai.model';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

@Component({
  standalone: true,
  imports: [HasPermissionDirective],
  template: `<span *appHasPermission="'core.user.write'">chi-thay-khi-co-quyen</span>`,
})
class HostMotQuyenComponent {}

@Component({
  standalone: true,
  imports: [HasPermissionDirective],
  template: `<span *appHasPermission="['core.user.read', 'core.user.write']"
    >chi-thay-khi-co-ca-hai</span
  >`,
})
class HostNhieuQuyenComponent {}

describe('HasPermissionDirective', () => {
  let auth: AuthService;

  function taoProviders() {
    return [
      provideZonelessChangeDetection(),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      provideCoreI18n({
        languages: [{ code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi }],
        defaultLanguage: 'vi',
        sources: ['/i18n/'],
      }),
      provideTranslateService({
        lang: 'vi',
        fallbackLang: 'vi',
        loader: provideTranslateLoader(FakeTranslateLoader),
      }),
    ];
  }

  function datQuyen(quyen: readonly string[]): void {
    const nguoiDung: NguoiDungHienTai = {
      id: 'u-1',
      userName: 'an.nv',
      email: null,
      fullName: 'A',
      roles: [],
      mustChangePassword: false,
      isSystemOperator: false,
      sessionMinutes: 30,
      preferredLanguage: 'vi',
      tenantCode: 'SO-GD',
      tenantName: 'SGD',
      quyen,
    };
    (auth as unknown as { _nguoiDung: { set: (v: NguoiDungHienTai) => void } })['_nguoiDung'].set(
      nguoiDung,
    );
  }

  it('ẨN phần tử khi người dùng KHÔNG có quyền', () => {
    TestBed.configureTestingModule({ providers: taoProviders() });
    auth = TestBed.inject(AuthService);
    const fixture: ComponentFixture<HostMotQuyenComponent> =
      TestBed.createComponent(HostMotQuyenComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('chi-thay-khi-co-quyen');
  });

  it('HIỆN phần tử khi người dùng CÓ đúng quyền', () => {
    TestBed.configureTestingModule({ providers: taoProviders() });
    auth = TestBed.inject(AuthService);
    datQuyen(['core.user.write']);

    const fixture: ComponentFixture<HostMotQuyenComponent> =
      TestBed.createComponent(HostMotQuyenComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('chi-thay-khi-co-quyen');
  });

  it('nhận mảng quyền — phải có ĐỦ mọi quyền (VÀ, không phải HOẶC)', () => {
    TestBed.configureTestingModule({ providers: taoProviders() });
    auth = TestBed.inject(AuthService);
    datQuyen(['core.user.read']); // thiếu core.user.write

    const fixture: ComponentFixture<HostNhieuQuyenComponent> =
      TestBed.createComponent(HostNhieuQuyenComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('chi-thay-khi-co-ca-hai');
  });

  it('làm mới phiên với CÙNG quyền → view KHÔNG bị dựng lại (cùng một phần tử DOM)', () => {
    TestBed.configureTestingModule({ providers: taoProviders() });
    auth = TestBed.inject(AuthService);
    datQuyen(['core.user.write']);

    const fixture: ComponentFixture<HostMotQuyenComponent> =
      TestBed.createComponent(HostMotQuyenComponent);
    fixture.detectChanges();
    const truoc = (fixture.nativeElement as HTMLElement).querySelector('span');
    expect(truoc).not.toBeNull();

    datQuyen(['core.user.write', 'core.role.read']); // phiên mới, đối tượng mới, cùng kết luận
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('span')).toBe(truoc);
  });

  it('thu hồi quyền sau khi đã hiện → phần tử biến mất theo signal, không cần tải lại trang', () => {
    TestBed.configureTestingModule({ providers: taoProviders() });
    auth = TestBed.inject(AuthService);
    datQuyen(['core.user.write']);

    const fixture: ComponentFixture<HostMotQuyenComponent> =
      TestBed.createComponent(HostMotQuyenComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('chi-thay-khi-co-quyen');

    datQuyen([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('chi-thay-khi-co-quyen');
  });
});
