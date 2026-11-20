import test from 'node:test';
import assert from 'node:assert/strict';
import { sortTracks, following } from '../src/playlist.ts';

const track = name => ({ path: `nhac/${name}`, name });
const a = track('01 Mai.mp3');
const b = track('02 Xuân.mp3');
const c = track('03 Tết.MP3');

test('Phát tiếp từ bài được chọn và quay lại đầu sau bài cuối', () => {
  const tracks = sortTracks([c, a, b]);
  assert.deepEqual(following(tracks, b, new Set()), c);
  assert.deepEqual(following(tracks, c, new Set()), a);
  assert.deepEqual(following([a], a, new Set()), a);
});

test('Dùng danh sách mới khi chuyển bài, bỏ qua tệp đã báo lỗi', () => {
  const newTrack = track('02b Về nhà.mp3');
  assert.deepEqual(following(sortTracks([a, b, c, newTrack]), b, new Set()), newTrack);
  assert.deepEqual(following(sortTracks([a, b, c]), b, new Set([c.path])), a);
  assert.equal(following([a], a, new Set([a.path])), undefined);
});
