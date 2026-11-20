export interface Track {
  path: string;
  name: string;
}

function order(a: string, b: string) {
  const left = a.toUpperCase();
  const right = b.toUpperCase();
  return left < right ? -1 : left > right ? 1 : 0;
}

export function sortTracks(tracks: Track[]) {
  return [...tracks].sort((a, b) => order(a.name, b.name));
}

export function following(tracks: Track[], current: Track, skipped: ReadonlySet<string>) {
  const playable = tracks.filter(track => !skipped.has(track.path));
  return playable.find(track => order(track.name, current.name) > 0) ?? playable[0];
}

export function trackTitle(track: Track) {
  return track.name.replace(/\.[^.]+$/, '');
}
