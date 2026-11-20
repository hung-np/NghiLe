import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { countdown, loadCalendar, selectTarget } from '../src/calendar.ts';
import { readSettings, writeSettings } from '../src/settings.ts';
import { dimensions, isCompact, widthFromDrag } from '../src/size.ts';

const calendar = loadCalendar(readFileSync(new URL('../data/tet.csv', import.meta.url), 'utf8'));
const years = calendar.map(day => day.year);
const eve = Date.parse('2027-02-05T17:00:00Z');

test('Lịch Tết Việt Nam và Can Chi', () => {
  assert.deepEqual(years, Array.from({ length: 11 }, (_, index) => 2026 + index));
  assert.equal(calendar.find(day => day.year === 2027).name, 'Đinh Mùi');
  assert.equal(calendar.find(day => day.year === 2027).timestamp, eve);
  assert.equal(calendar.find(day => day.year === 2030).date, '02/02/2030');
  assert.equal(calendar.find(day => day.year === 2034).date, '19/02/2034');
});

test('Đếm ngược tại giao thừa và sau khi nhảy thời gian', () => {
  assert.deepEqual(countdown(eve, Date.parse('2027-02-05T18:00:00+07:00')),
    { days: 0, hours: 6, minutes: 0, seconds: 0, reached: false });
  assert.equal(countdown(eve, eve - 1).seconds, 1);
  assert.equal(countdown(eve, eve).reached, true);
  assert.deepEqual(countdown(eve, eve + 120_000),
    { days: 0, hours: 0, minutes: 0, seconds: 0, reached: true });
});

test('Tự động đổi mốc, chế độ cố định giữ nguyên năm', () => {
  assert.equal(selectTarget(calendar, true, 2030, eve - 1).year, 2027);
  assert.equal(selectTarget(calendar, true, 2030, eve).year, 2028);
  assert.equal(selectTarget(calendar, false, 2027, eve + 1).year, 2027);
  assert.equal(selectTarget(calendar, true, 2026, Date.UTC(2040, 0, 1)), undefined);
});

test('Cài đặt khôi phục chính xác và bỏ qua dữ liệu hỏng', () => {
  let data = '';
  const store = { getItem: () => data, setItem: (_, value) => { data = value; } };
  const settings = { automatic: false, year: 2030, alwaysOnTop: false, motion: false, glass: 100 };
  assert.equal(writeSettings(store, settings), true);
  assert.deepEqual(readSettings(store, years, false), settings);
  data = '{"glass":88}';
  assert.equal(readSettings(store, years, false).glass, 100);
  assert.equal(writeSettings(store, { ...settings, glass: 88 }), true);
  assert.equal(readSettings(store, years, false).glass, 88);
  data = '{invalid!';
  assert.deepEqual(readSettings(store, years, true),
    { automatic: true, year: 2027, alwaysOnTop: true, motion: false, glass: 57 });
});

test('Kéo góc đổi cả hai chiều theo tỉ lệ 4:5, trong giới hạn', () => {
  assert.deepEqual(dimensions(widthFromDrag(200, -40, -50)), { width: 160, height: 200 });
  assert.deepEqual(dimensions(widthFromDrag(200, -180, -225)), { width: 120, height: 150 });
  assert.deepEqual(dimensions(widthFromDrag(200, 300, 375)), { width: 360, height: 450 });
});

test('Chế độ gọn bắt đầu từ 160 px trở xuống', () => {
  assert.equal(isCompact(161), false);
  assert.equal(isCompact(160), true);
  assert.equal(isCompact(120), true);
});
