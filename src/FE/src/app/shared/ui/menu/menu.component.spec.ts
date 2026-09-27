import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Menu } from 'primeng/menu';

import { MenuComponent } from './menu.component';

@Component({
  standalone: true,
  imports: [MenuComponent],
  template: `
    <button id="nut" type="button" (click)="mo($event)"><i id="icon"></i></button>
    <app-menu [open]="open()" [anchorEvent]="suKien()" [items]="[]" ariaLabel="menu thử" />
  `,
})
class HostComponent {
  readonly open = signal(false);
  readonly suKien = signal<Event | null>(null);
  mo(evt: Event): void {
    this.suKien.set(evt);
    this.open.set(true);
  }
}

/**
 * `Menu.show(event)` của PrimeNG neo overlay vào `event.currentTarget`, mà effect của `MenuComponent`
 * chạy SAU khi sự kiện đã phát xong — lúc đó trình duyệt đã đặt `currentTarget` về `null` và PrimeNG
 * ném `Cannot read properties of null (reading 'offsetHeight')` khi căn vị trí.
 */
describe('MenuComponent — neo overlay khi mở', () => {
  let fixture: ComponentFixture<HostComponent>;
  let show: jasmine.Spy;

  beforeEach(async () => {
    show = spyOn(Menu.prototype, 'show').and.stub();
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideZonelessChangeDetection(), provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('bấm vào icon bên trong nút → PrimeNG nhận neo là chính <button>, không phải currentTarget rỗng của sự kiện đã phát xong', async () => {
    const root = fixture.nativeElement as HTMLElement;
    root.querySelector<HTMLElement>('#icon')?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();

    expect(show).toHaveBeenCalledTimes(1);
    const doiSo = show.calls.mostRecent().args[0] as { currentTarget: Element | null };
    expect(doiSo.currentTarget).toBe(root.querySelector('#nut'));
  });
});
