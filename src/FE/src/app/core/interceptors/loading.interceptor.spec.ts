import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { loadingInterceptor } from './loading.interceptor';
import { LoadingService } from '../http/loading.service';

describe('loadingInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let loading: LoadingService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([loadingInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    loading = TestBed.inject(LoadingService);
  });

  afterEach(() => httpMock.verify());

  it('hienThi() lên true khi request bắt đầu, về false khi request thành công', () => {
    expect(loading.hienThi()).toBeFalse();

    http.get('/anything').subscribe();
    expect(loading.hienThi()).toBeTrue();

    httpMock.expectOne('/anything').flush({});
    expect(loading.hienThi()).toBeFalse();
  });

  it('vẫn giảm đếm khi request lỗi — finalize() chạy cả ở nhánh lỗi', () => {
    http.get('/anything').subscribe({ error: () => undefined });
    expect(loading.hienThi()).toBeTrue();

    httpMock.expectOne('/anything').error(new ProgressEvent('network error'));
    expect(loading.hienThi()).toBeFalse();
  });

  it('đếm đúng với nhiều request song song', () => {
    http.get('/a').subscribe();
    http.get('/b').subscribe();
    expect(loading.hienThi()).toBeTrue();

    httpMock.expectOne('/a').flush({});
    expect(loading.hienThi()).toBeTrue(); // /b vẫn đang chạy

    httpMock.expectOne('/b').flush({});
    expect(loading.hienThi()).toBeFalse();
  });
});
