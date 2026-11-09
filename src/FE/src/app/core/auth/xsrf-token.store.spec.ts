import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';

import { XsrfTokenService } from './xsrf-token.service';
import { XsrfTokenStore } from './xsrf-token.store';

describe('XsrfTokenStore', () => {
  let store: XsrfTokenStore;
  let service: jasmine.SpyObj<XsrfTokenService>;

  beforeEach(() => {
    service = jasmine.createSpyObj<XsrfTokenService>('XsrfTokenService', ['layToken']);
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: XsrfTokenService, useValue: service },
      ],
    });
    store = TestBed.inject(XsrfTokenStore);
  });

  it('token() là null trước khi lamMoi() từng chạy', () => {
    expect(store.token()).toBeNull();
  });

  it('lamMoi() giữ token service trả về ở bộ nhớ và phát lại chính nó', () => {
    service.layToken.and.returnValue(of('xsrf-abc'));
    let ketQua: string | undefined;
    store.lamMoi().subscribe((t) => (ketQua = t));

    expect(ketQua).toBe('xsrf-abc');
    expect(store.token()).toBe('xsrf-abc');
  });

  it('token chỉ được đặt khi service trả về, không đặt lúc bắt đầu gọi', () => {
    const phanHoi = new Subject<string>();
    service.layToken.and.returnValue(phanHoi as Observable<string>);
    store.lamMoi().subscribe();
    expect(store.token()).toBeNull();

    phanHoi.next('muon');
    expect(store.token()).toBe('muon');
  });

  it('mặc định chuyền boQuaToastLoi=false; lamMoi({ boQuaToastLoi: true }) chuyền true', () => {
    service.layToken.and.returnValue(of('a'));

    store.lamMoi().subscribe();
    expect(service.layToken).toHaveBeenCalledWith(false);

    store.lamMoi({ boQuaToastLoi: true }).subscribe();
    expect(service.layToken).toHaveBeenCalledWith(true);
  });

  it('service lỗi → lỗi đi tiếp cho nơi gọi và token cũ được giữ nguyên', () => {
    service.layToken.and.returnValue(of('cu'));
    store.lamMoi().subscribe();

    const loi = new Error('hong');
    service.layToken.and.returnValue(throwError(() => loi));
    let nhan: unknown;
    store.lamMoi().subscribe({ error: (e: unknown) => (nhan = e) });

    expect(nhan).toBe(loi);
    expect(store.token()).toBe('cu');
  });
});
