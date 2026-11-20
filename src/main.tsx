import { useEffect, useRef, useState } from 'react';
import type { CSSProperties, PointerEvent as ReactPointerEvent } from 'react';
import { createRoot } from 'react-dom/client';
import { invoke, isTauri } from '@tauri-apps/api/core';
import { LogicalSize } from '@tauri-apps/api/dpi';
import { getCurrentWindow } from '@tauri-apps/api/window';
import calendarData from '../data/tet.csv?raw';
import { countdown, loadCalendar, selectTarget } from './calendar.ts';
import { readSettings, writeSettings } from './settings.ts';
import { dimensions, isCompact, widgetHeight, widgetWidth, widthFromDrag } from './size.ts';
import { Music, useMusic } from './Music.tsx';
import { Weather } from './Weather.tsx';
import './style.css';

const calendar = loadCalendar(calendarData);
const years = calendar.map(day => day.year);
const nativeWindow = isTauri() ? getCurrentWindow() : null;
if (!nativeWindow) document.documentElement.classList.add('browser-demo');

const currentScale = () => nativeWindow ? Math.min(innerWidth / widgetWidth, innerHeight / widgetHeight) : 1;

function App() {
  const [settings, changeSettings] = useState(() => readSettings(localStorage, years, matchMedia('(prefers-reduced-motion: reduce)').matches));
  const [now, setNow] = useState(Date.now);
  const [panel, setPanel] = useState<'settings' | 'music' | false>(false);
  const [visible, setVisible] = useState(!document.hidden);
  const music = useMusic(Boolean(nativeWindow), panel === 'music' && visible);
  const [error, setError] = useState('');
  const [scale, setScale] = useState(currentScale);
  const grip = useRef<{ id: number; x: number; y: number; start: number; width: number } | null>(null);
  const resize = useRef<{ busy: boolean; next?: { width: number; save: boolean } }>({ busy: false });
  const target = selectTarget(calendar, settings.automatic, settings.year, now);
  const left = target && countdown(target.timestamp, now);
  const motion = settings.motion && visible;
  const compact = isCompact(scale * widgetWidth);
  const dayLabel = compact && !target ? 'Hết dữ liệu' : compact && left?.reached ? 'Tết đã đến' : 'Ngày';

  useEffect(() => {
    const update = () => setScale(currentScale());
    window.addEventListener('resize', update);
    return () => window.removeEventListener('resize', update);
  }, []);

  useEffect(() => {
    const refresh = () => { setVisible(!document.hidden); setNow(Date.now()); };
    document.addEventListener('visibilitychange', refresh);
    window.addEventListener('focus', refresh);
    return () => {
      document.removeEventListener('visibilitychange', refresh);
      window.removeEventListener('focus', refresh);
    };
  }, []);

  useEffect(() => {
    if (!visible || panel) return;
    setNow(Date.now());
    const timer = window.setInterval(() => setNow(Date.now()), 250);
    return () => window.clearInterval(timer);
  }, [visible, panel]);

  useEffect(() => {
    if (!writeSettings(localStorage, settings)) setError('Không lưu được cài đặt.');
  }, [settings]);

  useEffect(() => {
    nativeWindow?.setAlwaysOnTop(settings.alwaysOnTop).catch(() => setError('Không đổi được chế độ luôn nổi.'));
  }, [settings.alwaysOnTop]);

  useEffect(() => {
    const keyboard = (event: KeyboardEvent) => {
      if (event.key === 'F2') { event.preventDefault(); setPanel(value => value ? false : 'settings'); }
      if (event.key === 'Escape') setPanel(value => value === 'music' ? 'settings' : false);
    };
    window.addEventListener('keydown', keyboard);
    return () => window.removeEventListener('keydown', keyboard);
  }, []);

  const parts = [['Giờ', left?.hours], ['Phút', left?.minutes], ['Giây', left?.seconds]] as const;
  const format = (value: number | undefined) => value === undefined ? '–' : String(value).padStart(2, '0');

  async function requestSize(width: number, save: boolean) {
    if (!nativeWindow) return;
    const state = resize.current;
    state.next = { width, save };
    if (state.busy) return;
    state.busy = true;
    try {
      while (state.next) {
        const request = state.next;
        state.next = undefined;
        const size = dimensions(request.width);
        await nativeWindow.setSize(new LogicalSize(size.width, size.height));
        if (request.save) await invoke('finish_resize');
      }
    } catch {
      setError('Không đổi được kích thước.');
    } finally {
      state.busy = false;
    }
  }

  function finishDrag(event: ReactPointerEvent<HTMLButtonElement>) {
    if (grip.current?.id !== event.pointerId) return;
    const { width } = grip.current;
    grip.current = null;
    void requestSize(width, true);
  }

  return (
    <main className={`widget ${motion ? 'animate' : ''} ${compact ? 'compact' : ''} ${panel ? 'panel-open' : ''}`}
      style={{ '--glass': settings.glass / 100, '--scale': scale } as CSSProperties}>
      {panel === 'music' ? <Music player={music} back={() => setPanel('settings')} /> : panel ? (
        <section className="panel-page settings" aria-labelledby="settings-title">
          <header className="panel-header">
            <button className="panel-back" type="button" aria-label="Về đồng hồ" title="Về đồng hồ" onClick={() => setPanel(false)}>‹</button>
            <h1 id="settings-title">Cài đặt</h1>
          </header>
          <div className="settings-body">
            <h2 className="panel-section">Đếm ngược</h2>
            <select aria-label="Năm Tết" value={settings.automatic ? 'next' : settings.year}
              onChange={event => {
                const choice = event.target.value;
                changeSettings(choice === 'next'
                  ? { ...settings, automatic: true }
                  : { ...settings, year: Number(choice), automatic: false });
              }}>
              <option value="next">Tết kế tiếp</option>
              {calendar.map(day => <option key={day.year} value={day.year}>{day.year} · {day.name}</option>)}
            </select>
            <h2 className="panel-section display-title">Hiển thị</h2>
            <label className="switch">Luôn nổi<input type="checkbox" aria-label="Luôn nổi trên màn hình" checked={settings.alwaysOnTop}
              onChange={event => changeSettings({ ...settings, alwaysOnTop: event.target.checked })} /></label>
            <label className="switch">Chuyển động<input type="checkbox" aria-label="Hiệu ứng chuyển động" checked={settings.motion}
              onChange={event => changeSettings({ ...settings, motion: event.target.checked })} /></label>
            <label className="glass-setting">
              <span>Độ đậm kính <span className="glass-value">{settings.glass}%</span></span>
              <input type="range" aria-label="Độ đậm kính" min="42" max="100" value={settings.glass}
                onChange={event => changeSettings({ ...settings, glass: event.target.valueAsNumber })} />
            </label>
            {error && <span role="alert" className="error">{error}</span>}
          </div>
          <button className="panel-primary" type="button" onClick={() => setPanel('music')}>Nhạc ›</button>
        </section>
      ) : (
        <>
          <Weather motion={motion} />
          <div className="face" role="timer" aria-live="off" aria-label="Đếm ngược đến Tết theo giờ Việt Nam">
            <p className="year">{target ? `${target.name} · ${target.year}` : 'Nhịp Ngày'}</p>
            <p className={`days ${left && left.days >= 1000 ? 'wide-number' : ''}`}><span className="numeral" key={left?.days}>{left?.days ?? '–'}</span></p>
            <span className="unit day-unit">{dayLabel}</span>
            <span className="face-divider" aria-hidden="true" />
            <div className="clock">
              {parts.map(([name, value]) => <div key={name}><span className="numeral" key={value}>{format(value)}</span><span className="unit">{name}</span></div>)}
            </div>
            {!compact && <p className="date">{!target ? 'Hết dữ liệu lịch Tết' : left?.reached ? 'Tết này đã đến' : `${target.date} · Giờ Việt Nam`}</p>}
          </div>
          <div className="drag" data-tauri-drag-region aria-hidden="true" />
          <div className="controls">
            <button type="button" aria-label="Cài đặt" title="Cài đặt (F2)" onClick={() => setPanel('settings')}>⚙</button>
            {nativeWindow && <button type="button" aria-label="Ẩn widget" title="Ẩn widget" onClick={() => {
              void invoke('hide_widget').catch(() => setError('Không ẩn được widget.'));
            }}>×</button>}
          </div>
          {nativeWindow && <button type="button" className="grip" aria-label="Đổi kích thước widget"
            title="Kéo để thu nhỏ hoặc phóng to"
            onPointerDown={event => {
              if (event.button !== 0 || grip.current) return;
              event.currentTarget.setPointerCapture(event.pointerId);
              grip.current = { id: event.pointerId, x: event.screenX, y: event.screenY, start: innerWidth, width: innerWidth };
            }}
            onPointerMove={event => {
              const drag = grip.current;
              if (!drag || drag.id !== event.pointerId) return;
              drag.width = widthFromDrag(drag.start, event.screenX - drag.x, event.screenY - drag.y);
              void requestSize(drag.width, false);
            }}
            onPointerUp={finishDrag} onPointerCancel={finishDrag} onLostPointerCapture={finishDrag}
            onKeyDown={event => {
              const direction = ['ArrowUp', 'ArrowRight'].includes(event.key) ? 1 :
                ['ArrowDown', 'ArrowLeft'].includes(event.key) ? -1 : 0;
              if (!direction) return;
              event.preventDefault();
              void requestSize(innerWidth + 10 * direction, true);
            }}>↘</button>}
        </>
      )}
    </main>
  );
}

createRoot(document.getElementById('root')!).render(<App />);
