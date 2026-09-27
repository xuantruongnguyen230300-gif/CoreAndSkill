// @ts-check
const eslint = require('@eslint/js');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const importPlugin = require('eslint-plugin-import');
const { BUSINESS_MODULES } = require('./eslint.boundaries.cjs');

// Luật F1 — core/ là tầng đáy, không import ngược lên ba tầng trên
// (docs/quy-uoc/fe-architecture.md §4.2).
const coreLayerZones = [
  {
    target: './src/app/core',
    from: './src/app/shared',
    message:
      'core/ không được import shared/ — core/ là tầng đáy. Hạ tầng dùng chung cho cả ' +
      'core/ lẫn shared/ thì đưa THẲNG vào core/, không đặt ở shared/ rồi import ngược.',
  },
  {
    target: './src/app/core',
    from: './src/app/platform',
    message: 'core/ không được import platform/ — core/ là tầng đáy.',
  },
  {
    target: './src/app/core',
    from: './src/app/modules',
    message: 'core/ không được import modules/ — core/ là tầng đáy.',
  },
];

// Luật F24 — shared/ui/ là lớp bọc thư viện; nó không import component tự dựng ở
// shared/components/ (docs/quy-uoc/fe-architecture.md §4.6).
const sharedUiZones = [
  {
    target: './src/app/shared/ui',
    from: './src/app/shared/components',
    message:
      'shared/ui/ không được import shared/components/. Lớp bọc cần hiện một component tự ' +
      'dựng thì nhận nó qua input TemplateRef, và màn ghép component đó vào.',
  },
];

// Luật F35 — hai tầng giữa chỉ nhìn xuống: shared/ không import platform/ hay modules/; platform/
// không import modules/ (docs/quy-uoc/fe-architecture.md §4.7).
const sharedLayerZones = ['platform', 'modules'].map((tang) => ({
  target: './src/app/shared',
  from: `./src/app/${tang}`,
  message: `shared/ không được import ${tang}/ (fe-architecture.md §4.7).`,
}));

const platformLayerZones = [
  {
    target: './src/app/platform',
    from: './src/app/modules',
    message: 'platform/ không được import modules/ (fe-architecture.md §4.7).',
  },
];

// Luật F2 — modules/<A> không import nội bộ modules/<B> (docs/quy-uoc/fe-architecture.md §4.3).
// Sinh từ BUSINESS_MODULES; mảng rỗng ở F0 nên khối rule dưới bị BỎ HẲN (§3.2 — zones rỗng làm
// ESLint chết lúc nạp config).
const moduleBoundaryZones = BUSINESS_MODULES.map((moduleName) => ({
  target: `./src/app/modules/${moduleName}`,
  from: './src/app/modules',
  except: [`./${moduleName}`],
  message:
    `modules/${moduleName}/ không được import nội bộ một module nghiệp vụ khác. ` +
    'Chỉ được import từ core/, shared/, platform/. Cần dùng chung logic thì nâng lên ' +
    'shared/ (dumb UI, service tái dùng) hoặc core/ (hạ tầng toàn app).',
}));

module.exports = tseslint.config(
  // Luật F3 — một comment tắt không còn tắt gì thì là lỗi, không để lại làm "giấy phép" chờ sẵn.
  // Dạng tắt không kèm tên rule thì do scripts/fe-gate.sh chặn (fe-architecture.md §4.5).
  { linterOptions: { reportUnusedDisableDirectives: 'error' } },
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...tseslint.configs.stylistic,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'app',
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'app',
          style: 'kebab-case',
        },
      ],
    },
  },
  {
    files: ['src/app/core/**/*.ts'],
    plugins: { import: importPlugin },
    settings: {
      // Phải khai `.ts` — thiếu thì rule không phân giải được import và im lặng không chặn gì.
      'import/resolver': { node: { extensions: ['.ts', '.js'] } },
    },
    rules: {
      'import/no-restricted-paths': ['error', { zones: coreLayerZones }],
    },
  },
  // Một khối cho cả cây shared/ — gom zone F24 và F35. Trong flat config, hai khối cùng khai
  // `import/no-restricted-paths` cho một tệp thì khối sau THAY tuỳ chọn của khối trước (zone của
  // khối trước biến mất mà lint vẫn xanh), nên không tách khối `shared/ui/**` riêng (§4.6, §4.7).
  {
    files: ['src/app/shared/**/*.ts'],
    plugins: { import: importPlugin },
    settings: {
      'import/resolver': { node: { extensions: ['.ts', '.js'] } },
    },
    rules: {
      'import/no-restricted-paths': ['error', { zones: [...sharedLayerZones, ...sharedUiZones] }],
    },
  },
  {
    files: ['src/app/platform/**/*.ts'],
    plugins: { import: importPlugin },
    settings: {
      'import/resolver': { node: { extensions: ['.ts', '.js'] } },
    },
    rules: {
      'import/no-restricted-paths': ['error', { zones: platformLayerZones }],
    },
  },
  // 🛑 zones phải có TỐI THIỂU một phần tử — mảng rỗng làm ESLint chết lúc nạp config
  // ("Invalid Options"). BUSINESS_MODULES rỗng ở F0 ⇒ bỏ hẳn khối này (fe-architecture.md §4.3).
  ...(moduleBoundaryZones.length > 0
    ? [
        {
          files: ['src/app/modules/**/*.ts'],
          plugins: { import: importPlugin },
          settings: {
            'import/resolver': { node: { extensions: ['.ts', '.js'] } },
          },
          rules: {
            'import/no-restricted-paths': ['error', { zones: moduleBoundaryZones }],
          },
        },
      ]
    : []),
  {
    files: ['**/*.html'],
    extends: [...angular.configs.templateRecommended, ...angular.configs.templateAccessibility],
    rules: {
      // Luật F9, lớp hai — bắt cả `<ng-template [ngIf]>` mà mẫu grep của fe-gate.sh không thấy
      // (docs/quy-uoc/fe-architecture.md §4.5, bảng rule cấm tắt).
      '@angular-eslint/template/prefer-control-flow': 'error',
    },
  },
);
