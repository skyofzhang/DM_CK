const SurvivalGameEngine = require('../src/SurvivalGameEngine');

function assertEqual(label, actual, expected) {
  if (actual !== expected) {
    console.error(`FAIL  ${label}: expected ${expected}, got ${actual}`);
    process.exitCode = 1;
    return;
  }
  console.log(`PASS  ${label}: ${actual}`);
}

function energyFromGift(giftId) {
  const engine = new SurvivalGameEngine({}, () => {});
  engine.room = { roomId: 'tribe_energy_test' };
  engine.tribeWarMgr = {
    onEnergyAdded(roomId, delta) {
      if (roomId === 'tribe_energy_test') energyFromGift.total += delta;
    },
  };
  energyFromGift.total = 0;

  engine.contributions.p1 = 0;
  engine.playerNames.p1 = 'Tester';
  engine.totalPlayers = 1;

  engine.handleGift('p1', 'Tester', '', giftId, 0, giftId);
  engine._clearAllTimers();
  return energyFromGift.total;
}

const expectedByGift = {
  fairy_wand: 1,
  ability_pill: 5,
  donut: 10,
  energy_battery: 20,
  love_explosion: 50,
  mystery_airdrop: 100,
};

for (const [giftId, expected] of Object.entries(expectedByGift)) {
  assertEqual(`§35 gift energy ${giftId}`, energyFromGift(giftId), expected);
}

if (process.exitCode) {
  console.error('\nResult: FAIL');
} else {
  console.log('\nResult: PASS');
}
