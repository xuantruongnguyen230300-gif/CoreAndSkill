import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { KHOA_BAO_TAB_PHIEN_KET_THUC, TabSessionBroadcastService } from './tab-session-broadcast.service';

describe('TabSessionBroadcastService', () => {
  let service: TabSessionBroadcastService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    service = TestBed.inject(TabSessionBroadcastService);
    localStorage.removeItem(KHOA_BAO_TAB_PHIEN_KET_THUC);
  });

  afterEach(() => localStorage.removeItem(KHOA_BAO_TAB_PHIEN_KET_THUC));

  it('baoPhienKetThuc() ghi một giá trị vào ĐÚNG khoá localStorage dùng chung', () => {
    service.baoPhienKetThuc();
    expect(localStorage.getItem(KHOA_BAO_TAB_PHIEN_KET_THUC)).not.toBeNull();
  });

  it('không ném lỗi khi localStorage.setItem hỏng (chế độ riêng tư, dung lượng đầy)', () => {
    spyOn(localStorage, 'setItem').and.throwError('quota exceeded');
    expect(() => service.baoPhienKetThuc()).not.toThrow();
  });
});
