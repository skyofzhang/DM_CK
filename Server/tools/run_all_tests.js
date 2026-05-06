const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');

const toolsDir = __dirname;
const serverDir = path.resolve(toolsDir, '..');

const tests = fs.readdirSync(toolsDir)
  .filter(name => /^test_.*\.js$/.test(name))
  .sort();

if (tests.length === 0) {
  console.log('No tool tests found.');
  process.exit(0);
}

let failed = 0;
for (const test of tests) {
  console.log(`\n=== ${test} ===`);
  const result = spawnSync(process.execPath, [path.join(toolsDir, test)], {
    cwd: serverDir,
    stdio: 'inherit',
  });
  if (result.status !== 0) {
    failed += 1;
    console.error(`FAIL ${test} (exit ${result.status})`);
  }
}

if (failed > 0) {
  console.error(`\n${failed}/${tests.length} tool tests failed.`);
  process.exit(1);
}

console.log(`\nPASS ${tests.length}/${tests.length} tool tests`);
