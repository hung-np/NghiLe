import { mkdirSync, writeFileSync } from 'node:fs';
import { deflateSync, crc32 } from 'node:zlib';

const folder = new URL('../src-tauri/icons/', import.meta.url);
mkdirSync(folder, { recursive: true });

function draw(size) {
  const pixels = Buffer.alloc((size * 4 + 1) * size);
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const u = (x + 0.5) / size;
      const v = (y + 0.5) / size;
      const corner = Math.hypot(Math.max(0, Math.abs(u - 0.5) - 0.33), Math.max(0, Math.abs(v - 0.5) - 0.33));
      const flowerX = u - 0.5;
      const flowerY = v - 0.48;
      const angle = Math.atan2(flowerY, flowerX);
      const limit = 0.23 + Math.cos(5 * (angle + Math.PI / 2)) * 0.065;
      const bloom = Math.hypot(flowerX, flowerY) < limit;
      const center = Math.hypot(flowerX, flowerY) < 0.064;
      const edge = Math.max(Math.abs(u - 0.5), Math.abs(v - 0.5)) > 0.466 || corner > 0.14;
      const color = center ? [114, 44, 66] : bloom ? [244, 187, 160] : edge ? [188, 125, 105] : [85, 25, 45];
      const index = y * (size * 4 + 1) + 1 + x * 4;
      pixels.set([...color, corner > 0.172 ? 0 : 255], index);
    }
  }
  return pixels;
}

function pngChunk(name, body) {
  const chunk = Buffer.alloc(12 + body.length);
  chunk.writeUInt32BE(body.length, 0);
  chunk.write(name, 4);
  body.copy(chunk, 8);
  chunk.writeUInt32BE(crc32(chunk.subarray(4, 8 + body.length)), 8 + body.length);
  return chunk;
}

function makePng(size) {
  const header = Buffer.alloc(13);
  header.writeUInt32BE(size, 0);
  header.writeUInt32BE(size, 4);
  header[8] = 8;
  header[9] = 6;
  return Buffer.concat([Buffer.from('89504e470d0a1a0a', 'hex'),
    pngChunk('IHDR', header), pngChunk('IDAT', deflateSync(draw(size))), pngChunk('IEND', Buffer.alloc(0))]);
}

const sizes = [16, 24, 32, 48, 64, 128, 256];
const images = sizes.map(makePng);
const directory = Buffer.alloc(6 + 16 * sizes.length);
directory.writeUInt16LE(1, 2);
directory.writeUInt16LE(sizes.length, 4);
let offset = directory.length;
for (const [index, size] of sizes.entries()) {
  const entry = 6 + 16 * index;
  directory[entry] = directory[entry + 1] = size % 256;
  directory.writeUInt16LE(1, entry + 4);
  directory.writeUInt16LE(32, entry + 6);
  directory.writeUInt32LE(images[index].length, entry + 8);
  directory.writeUInt32LE(offset, entry + 12);
  offset += images[index].length;
}
writeFileSync(new URL('icon.ico', folder), Buffer.concat([directory, ...images]));
writeFileSync(new URL('icon.png', folder), images.at(-1));
