import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.removeItem('theme');
    document.documentElement.removeAttribute('data-theme');
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
  });

  afterEach(() => {
    localStorage.removeItem('theme');
    document.documentElement.removeAttribute('data-theme');
  });

  it('preference() là "light" khi localStorage chưa có gì — DESIGN.md §8: mặc định là sáng', () => {
    const service = TestBed.inject(ThemeService);
    expect(service.preference()).toBe('light');
  });

  it('preference() là "light" khi localStorage chứa giá trị lạ — suy giảm êm, không vỡ màn hình', () => {
    localStorage.setItem('theme', 'khong-hop-le');
    const service = TestBed.inject(ThemeService);
    expect(service.preference()).toBe('light');
  });

  it('đọc lại đúng lựa chọn đã lưu trước đó — "dark"', () => {
    localStorage.setItem('theme', 'dark');
    const service = TestBed.inject(ThemeService);
    expect(service.preference()).toBe('dark');
  });

  it('đọc lại đúng lựa chọn đã lưu trước đó — "system"', () => {
    localStorage.setItem('theme', 'system');
    const service = TestBed.inject(ThemeService);
    expect(service.preference()).toBe('system');
  });

  it('setPreference("dark") ghi localStorage, đặt data-theme="dark", và cập nhật signal', () => {
    const service = TestBed.inject(ThemeService);
    service.setPreference('dark');

    expect(localStorage.getItem('theme')).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(service.preference()).toBe('dark');
  });

  it('setPreference("light") ghi localStorage và đặt data-theme="light"', () => {
    const service = TestBed.inject(ThemeService);
    service.setPreference('light');

    expect(localStorage.getItem('theme')).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('setPreference("system") ghi localStorage và GỠ thuộc tính data-theme — để theo hệ điều hành', () => {
    const service = TestBed.inject(ThemeService);
    service.setPreference('dark'); // đặt trước để chắc chắn có thuộc tính cần gỡ
    service.setPreference('system');

    expect(localStorage.getItem('theme')).toBe('system');
    expect(document.documentElement.hasAttribute('data-theme')).toBe(false);
    expect(service.preference()).toBe('system');
  });
});
