import { kiemCotMoRong, sangCotDataTable } from './cot-mo-rong';
import type { ScreenExtColumn } from './core-screen-ext';

interface Dong {
  readonly ma: string;
}

/**
 * fe-architecture.md §2.7 (ADR-0057) và `ColumnDef.value` ở fe-ui-conventions.md §9 — một hàm thuần
 * dùng chung cho ba màn danh sách Core. Hồi quy ở đây hiện ra cùng lúc trên cả ba màn.
 */
describe('sangCotDataTable()', () => {
  const dich = (khoa: string): string => `[dich]${khoa}`;

  it('không khai seam (`undefined`) → không cột nào, không lỗi', () => {
    expect(sangCotDataTable<Dong>(undefined, dich)).toEqual([]);
  });

  it('danh sách rỗng → không cột nào', () => {
    expect(sangCotDataTable<Dong>([], dich)).toEqual([]);
  });

  it('`header` là `headerKey` ĐÃ DỊCH qua hàm `dich` của màn — không phải khoá thô', () => {
    const [cot] = sangCotDataTable<Dong>(
      [{ key: 'maNv', headerKey: 'duAn.cot.maNv', value: (d) => d.ma }],
      dich,
    );
    expect(cot.header).toBe('[dich]duAn.cot.maNv');
  });

  it('giữ nguyên `key`, CHÍNH hàm `value`, và các thuộc tính trình bày', () => {
    const value = (d: Dong): string => `MNV-${d.ma}`;
    const nguon: ScreenExtColumn<Dong> = {
      key: 'maNv',
      headerKey: 'duAn.cot.maNv',
      value,
      width: '8rem',
      align: 'end',
      hideBelow: 'md',
      priority: 'low',
    };

    const [cot] = sangCotDataTable<Dong>([nguon], dich);

    expect(cot.key).toBe('maNv');
    expect(cot.value).toBe(value);
    expect(cot.value?.({ ma: '7' })).toBe('MNV-7');
    expect(cot.width).toBe('8rem');
    expect(cot.align).toBe('end');
    expect(cot.hideBelow).toBe('md');
    expect(cot.priority).toBe('low');
  });

  it('cột ra có ĐÚNG MỘT trong `cell`/`value` (điều kiện DataTable kiểm lúc dựng cột) và KHÔNG sắp xếp được', () => {
    const [cot] = sangCotDataTable<Dong>(
      [{ key: 'maNv', headerKey: 'duAn.cot.maNv', value: (d) => d.ma }],
      dich,
    );
    expect(cot.value).toBeDefined();
    expect(cot.cell).toBeUndefined();
    expect(cot.sortable).withContext('sortBy chỉ nhận allowlist của endpoint Core').toBeFalsy();
  });

  it('giữ thứ tự khai — cột nối vào sau cột Core theo đúng thứ tự dự án viết', () => {
    const cot = sangCotDataTable<Dong>(
      [
        { key: 'b', headerKey: 'k.b', value: () => 'b' },
        { key: 'a', headerKey: 'k.a', value: () => 'a' },
      ],
      dich,
    );
    expect(cot.map((c) => c.key)).toEqual(['b', 'a']);
  });
});

/**
 * fe-architecture.md §2.7 luật 6 (ADR-0084): khoá trùng là lỗi cấu hình — ném `Error` ở MỌI môi
 * trường, không bỏ qua cột, không đè cột Core. Thông báo nêu mã màn và khoá.
 */
describe('kiemCotMoRong()', () => {
  const cot = (key: string): ScreenExtColumn<Dong> => ({
    key,
    headerKey: `duAn.cot.${key}`,
    value: (d) => d.ma,
  });
  const KHOA_CORE = ['userName', 'email', 'createdAt'];

  it('không khai seam (`undefined`) → danh sách rỗng, không lỗi', () => {
    expect(kiemCotMoRong<Dong>('users', KHOA_CORE, undefined)).toEqual([]);
  });

  it('khoá không trùng → trả lại CHÍNH các cột, đúng thứ tự khai', () => {
    const b = cot('b');
    const a = cot('a');
    const ra = kiemCotMoRong<Dong>('users', KHOA_CORE, [b, a]);
    expect(ra.length).toBe(2);
    expect(ra[0]).toBe(b);
    expect(ra[1]).toBe(a);
  });

  it('cột dự án trùng khoá cột Core → ném Error nêu mã màn và khoá', () => {
    expect(() =>
      kiemCotMoRong<Dong>('users', KHOA_CORE, [cot('maNv'), cot('email')]),
    ).toThrowMatching(
      (e) => e instanceof Error && e.message.includes('"users"') && e.message.includes('"email"'),
    );
  });

  it('hai cột dự án trùng khoá nhau → ném Error nêu mã màn và khoá', () => {
    expect(() =>
      kiemCotMoRong<Dong>('roles', KHOA_CORE, [cot('maNv'), cot('maNv')]),
    ).toThrowMatching(
      (e) => e instanceof Error && e.message.includes('"roles"') && e.message.includes('"maNv"'),
    );
  });

  it('nhiều khoá trùng → một lần ném nêu ĐỦ các khoá, không dừng ở khoá đầu', () => {
    expect(() =>
      kiemCotMoRong<Dong>('tenants', KHOA_CORE, [
        cot('email'),
        cot('x'),
        cot('x'),
        cot('createdAt'),
      ]),
    ).toThrowMatching(
      (e) =>
        e instanceof Error &&
        e.message.includes('"tenants"') &&
        ['"email"', '"x"', '"createdAt"'].every((k) => e.message.includes(k)),
    );
  });
});
