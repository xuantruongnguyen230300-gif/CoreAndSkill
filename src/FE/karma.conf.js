// Cấu hình Karma của FE. Giữ khuôn mặc định của builder `@angular/build:karma`, thêm MỘT thứ:
// sàn coverage nhánh theo tầng (luật F15, docs/wiki-core/fe/06-testing-strategy.md §4).
//
// Sàn khai ở đúng một chỗ — hằng SAN_NHANH dưới đây — và không chép vào tài liệu nào. Hạ sàn là
// việc người dùng quyết. Sàn chỉ có hiệu lực khi chạy `ng test --code-coverage`; tính trên TỔNG
// nhánh của mọi tệp trong tầng, không trên từng tệp.
const path = require('node:path');

const SAN_NHANH = { core: 87, shared: 75, platform: 75 };

function tangCuaTep(duongDan) {
  const m = /[\\/]src[\\/]app[\\/](core|shared|platform)[\\/]/.exec(duongDan);
  return m ? m[1] : null;
}

function CoverageFloorReporter(config, logger) {
  const log = logger.create('coverage-floor');
  // Cùng đường remap qua source map mà reporter `coverage` dùng, để số đo khớp báo cáo HTML.
  const sourceMapStore = require('karma-coverage/lib/source-map-store').get(config.basePath);
  const { createCoverageMap } = require('istanbul-lib-coverage');
  let coverageMap = null;
  let phan = null;

  this.onRunStart = function () {
    coverageMap = createCoverageMap({});
    phan = null;
  };
  this.onBrowserComplete = function (_browser, result) {
    if (coverageMap && result && result.coverage) coverageMap.merge(result.coverage);
  };
  this.onRunComplete = function () {
    if (!coverageMap || coverageMap.files().length === 0) return; // không bật --code-coverage
    phan = (async () => {
      const remapped = await sourceMapStore.transformCoverage(coverageMap);
      const tong = {};
      for (const tep of remapped.files()) {
        const tang = tangCuaTep(tep);
        if (!tang) continue;
        const nhanh = remapped.fileCoverageFor(tep).toSummary().branches;
        const t = (tong[tang] ??= { phu: 0, tong: 0 });
        t.phu += nhanh.covered;
        t.tong += nhanh.total;
      }
      let dat = true;
      for (const [tang, san] of Object.entries(SAN_NHANH)) {
        const t = tong[tang];
        if (!t || t.tong === 0) {
          log.error(
            `Sàn coverage: tầng '${tang}' không đo được nhánh nào — đỏ, không phải xanh rỗng`,
          );
          dat = false;
          continue;
        }
        const pct = (t.phu / t.tong) * 100;
        const ok = pct >= san;
        const msg = `Sàn coverage nhánh '${tang}': ${pct.toFixed(2)}% (${t.phu}/${t.tong}) — sàn ${san}% — ${ok ? 'đạt' : 'DƯỚI SÀN'}`;
        if (ok) log.info(msg);
        else log.error(msg);
        if (!ok) dat = false;
      }
      return dat;
    })();
    return phan;
  };
  this.onExit = async function (done) {
    try {
      const dat = phan ? await phan : true;
      done(dat ? 0 : 1);
    } catch (e) {
      log.error('Không tính được sàn coverage.', e);
      done(1);
    }
  };
}
CoverageFloorReporter.$inject = ['config', 'logger'];

module.exports = function (config) {
  config.set({
    basePath: '',
    frameworks: ['jasmine'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
      require('karma-coverage'),
      { 'reporter:coverage-floor': ['type', CoverageFloorReporter] },
    ],
    jasmineHtmlReporter: {
      suppressAll: true, // bỏ vết lặp
    },
    coverageReporter: {
      dir: path.join(__dirname, 'coverage/fe'),
      subdir: '.',
      reporters: [{ type: 'html' }, { type: 'text-summary' }],
    },
    reporters: ['progress', 'kjhtml', 'coverage-floor'],
    browsers: ['Chrome'],
    restartOnFileChange: true,
  });
};
