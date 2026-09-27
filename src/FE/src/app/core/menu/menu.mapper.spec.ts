import { MenuItemDto } from './menu.dto';
import { dungCayMenu } from './menu.mapper';

const PHANG: readonly MenuItemDto[] = [
  {
    id: '1',
    parentId: null,
    code: 'trang-chu',
    labelKey: 'menu.trang-chu',
    icon: 'pi-home',
    route: '/trang-chu',
    displayOrder: 10,
  },
  {
    id: '2',
    parentId: null,
    code: 'quan-tri',
    labelKey: 'menu.quan-tri',
    icon: 'pi-cog',
    route: null,
    displayOrder: 90,
  },
  {
    id: '3',
    parentId: '2',
    code: 'quan-tri-nguoi-dung',
    labelKey: 'menu.quan-tri-nguoi-dung',
    icon: 'pi-users',
    route: '/quan-tri/nguoi-dung',
    displayOrder: 10,
  },
];

describe('dungCayMenu()', () => {
  it('dựng đúng cây hai cấp từ danh sách phẳng', () => {
    const cay = dungCayMenu(PHANG);

    expect(cay.length).toBe(2);
    expect(cay[0].code).toBe('trang-chu');
    expect(cay[0].children.length).toBe(0);
    expect(cay[1].code).toBe('quan-tri');
    expect(cay[1].children.length).toBe(1);
    expect(cay[1].children[0].code).toBe('quan-tri-nguoi-dung');
  });

  it('mục có parentId KHÔNG trỏ tới mục nào đang có → rơi xuống làm gốc, không bị mất (meta-menu.md §1.6)', () => {
    const moCoi: MenuItemDto = {
      id: '9',
      parentId: 'khong-ton-tai',
      code: 'mo-coi',
      labelKey: 'menu.mo-coi',
      icon: null,
      route: '/mo-coi',
      displayOrder: 5,
    };
    const cay = dungCayMenu([...PHANG, moCoi]);

    const goc = cay.map((n) => n.code);
    expect(goc).toContain('mo-coi');
  });

  it('GIỮ NGUYÊN thứ tự máy chủ trả, KHÔNG tự sắp theo displayOrder (meta-menu.md §1)', () => {
    // `displayOrder` giảm dần: nếu mapper sắp lại thì thứ tự ra sẽ đảo so với thứ tự nhận được.
    const nghichThuTu: readonly MenuItemDto[] = [
      { ...PHANG[0], id: 'a', code: 'a', parentId: null, displayOrder: 30 },
      { ...PHANG[0], id: 'b', code: 'b', parentId: null, displayOrder: 20 },
      { ...PHANG[0], id: 'c', code: 'c', parentId: null, displayOrder: 10 },
    ];

    expect(dungCayMenu(nghichThuTu).map((n) => n.code)).toEqual(['a', 'b', 'c']);
  });

  it('GIỮ NGUYÊN thứ tự máy chủ trả ở cấp con (meta-menu.md §1)', () => {
    const cha: MenuItemDto = { ...PHANG[1], id: 'p', code: 'p', parentId: null, displayOrder: 1 };
    const con: readonly MenuItemDto[] = [
      { ...PHANG[2], id: 'c2', code: 'c2', parentId: 'p', displayOrder: 20 },
      { ...PHANG[2], id: 'c1', code: 'c1', parentId: 'p', displayOrder: 10 },
    ];

    const cay = dungCayMenu([cha, ...con]);

    expect(cay[0].children.map((n) => n.code)).toEqual(['c2', 'c1']);
  });

  it('danh sách rỗng → cây rỗng', () => {
    expect(dungCayMenu([])).toEqual([]);
  });

  it('model KHÔNG mang trường của dây (parentId, displayOrder) — mapper rút về đúng thứ màn cần (fe-api-client.md §4.2)', () => {
    const cay = dungCayMenu(PHANG);
    const goc = cay[1] as unknown as Record<string, unknown>;
    const con = cay[1].children[0] as unknown as Record<string, unknown>;

    expect(Object.keys(goc).sort()).toEqual([
      'children',
      'code',
      'icon',
      'id',
      'labelKey',
      'route',
    ]);
    expect('parentId' in con).toBeFalse();
    expect('displayOrder' in con).toBeFalse();
  });
});
