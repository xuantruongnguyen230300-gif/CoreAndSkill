import { CORE_PRIME_Z_INDEX } from './prime-z-index';

/** Đọc một token `--z-*` đã giải ra số bằng trình duyệt thật (token khai trong `styles/_tokens.scss`). */
function zCuaToken(token: string): number {
  const m = document.createElement('div');
  m.style.position = 'absolute';
  m.style.zIndex = `var(${token})`;
  document.body.appendChild(m);
  const z = parseInt(getComputedStyle(m).zIndex, 10);
  m.remove();
  return z;
}

describe('CORE_PRIME_Z_INDEX — phản chiếu thang --z-* (DESIGN.md §6.4)', () => {
  it('modal = --z-backdrop', () => {
    expect(CORE_PRIME_Z_INDEX.modal).toBe(zCuaToken('--z-backdrop'));
  });

  it('overlay, menu, tooltip = --z-popover', () => {
    const popover = zCuaToken('--z-popover');
    expect(CORE_PRIME_Z_INDEX.overlay).toBe(popover);
    expect(CORE_PRIME_Z_INDEX.menu).toBe(popover);
    expect(CORE_PRIME_Z_INDEX.tooltip).toBe(popover);
  });
});
