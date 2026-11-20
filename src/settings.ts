export interface Settings {
  automatic: boolean;
  year: number;
  alwaysOnTop: boolean;
  motion: boolean;
  glass: number;
}

const key = 'nocturne-settings-v1';

export function readSettings(store: Pick<Storage, 'getItem'>, years: number[], reducedMotion: boolean): Settings {
  const defaults = { automatic: true, year: years.includes(2027) ? 2027 : years[0], alwaysOnTop: true,
    motion: !reducedMotion, glass: 57 };
  try {
    const parsed = JSON.parse(store.getItem(key) ?? '{}');
    if (!parsed || typeof parsed !== 'object') return defaults;
    const glass = parsed.version !== 2 && parsed.glass === 88 ? 100 : parsed.glass;
    return {
      automatic: typeof parsed.automatic === 'boolean' ? parsed.automatic : defaults.automatic,
      year: years.includes(parsed.year) ? parsed.year : defaults.year,
      alwaysOnTop: typeof parsed.alwaysOnTop === 'boolean' ? parsed.alwaysOnTop : defaults.alwaysOnTop,
      motion: typeof parsed.motion === 'boolean' ? parsed.motion : defaults.motion,
      glass: Number.isFinite(glass) ? Math.max(42, Math.min(100, Math.round(glass))) : defaults.glass
    };
  } catch {
    return defaults;
  }
}

export function writeSettings(store: Pick<Storage, 'setItem'>, value: Settings) {
  try { store.setItem(key, JSON.stringify({ ...value, version: 2 })); return true; }
  catch { return false; }
}
