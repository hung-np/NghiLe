import { useEffect, useState, useSyncExternalStore } from 'react';
import { convertFileSrc, invoke } from '@tauri-apps/api/core';
import { following, sortTracks, trackTitle } from './playlist.ts';
import type { Track } from './playlist.ts';

interface MediaState {
  tracks: Track[];
  selected?: Track;
  current?: Track;
  playing: boolean;
  readError?: string;
  playError?: string;
}

class MusicPlayer {
  private state: MediaState = { tracks: [], playing: false };
  private listeners = new Set<() => void>();
  private audio?: HTMLAudioElement;
  private skipped = new Set<string>();
  private scan?: Promise<void>;

  constructor(private readonly available: boolean) {}

  subscribe = (listener: () => void) => {
    this.listeners.add(listener);
    return () => { this.listeners.delete(listener); };
  };
  snapshot = () => this.state;

  private change(update: Partial<MediaState>) {
    this.state = { ...this.state, ...update };
    for (const listener of this.listeners) listener();
  }

  refresh = () => {
    if (!this.available) return Promise.resolve();
    if (this.scan) return this.scan;
    this.scan = invoke<[string, string][]>('list_music')
      .then(files => {
        const tracks = sortTracks(files.map(([path, name]) => ({ path, name })));
        const old = this.state.tracks;
        if (this.state.readError || old.length !== tracks.length || tracks.some((track, i) => track.path !== old[i].path)) {
          const selected = tracks.find(track => track.path === this.state.selected?.path);
          this.change({ tracks, selected, readError: undefined });
        }
      })
      .catch(() => this.change({ readError: 'Không đọc được thư mục nhac.' }))
      .finally(() => { this.scan = undefined; });
    return this.scan;
  };

  choose = (track: Track) => {
    if (track.path === (this.state.selected ?? this.state.current)?.path) return;
    this.stop();
    this.change({ selected: track, current: undefined, playing: false, playError: undefined });
  };

  toggle = () => {
    const audio = this.audio;
    if (!audio) {
      const track = this.state.selected ?? this.state.tracks[0];
      if (track) {
        this.skipped.clear();
        this.play(track);
      }
    } else if (this.state.playing) {
      audio.pause();
      this.change({ playing: false });
    } else {
      this.change({ playing: true, playError: undefined });
      void audio.play().catch(error => this.handlePlayError(audio, error));
    }
  };

  private play(track: Track) {
    this.stop();
    const audio = new Audio(convertFileSrc(track.path));
    this.audio = audio;
    audio.volume = .6;
    audio.onended = () => { void this.advance(audio, false); };
    audio.onerror = () => { void this.advance(audio, true); };
    this.change({ selected: undefined, current: track, playing: true, playError: undefined });
    void audio.play().catch(error => this.handlePlayError(audio, error));
  }

  private handlePlayError(audio: HTMLAudioElement, error: DOMException) {
    if (audio !== this.audio || !this.state.playing) return;
    if (error.name === 'NotAllowedError') {
      this.change({ playing: false, playError: 'Bấm Bắt đầu phát để tiếp tục nhạc.' });
    } else if (error.name !== 'AbortError') {
      void this.advance(audio, true);
    }
  }

  private async advance(audio: HTMLAudioElement, failed: boolean) {
    const current = this.state.current;
    if (this.audio !== audio || !current || !this.state.playing) return;
    if (failed) this.skipped.add(current.path); else this.skipped.clear();
    await this.refresh();
    if (this.audio !== audio || !this.state.playing) return;
    const next = following(this.state.tracks, current, this.skipped);
    if (next) this.play(next);
    else {
      this.stop();
      this.change({ current: undefined, playing: false, playError: failed ? 'Không phát được tệp nhạc.' : undefined });
    }
  }

  stop = () => {
    const audio = this.audio;
    this.audio = undefined;
    if (!audio) return;
    audio.onended = audio.onerror = null;
    audio.pause();
    audio.removeAttribute('src');
    audio.load();
  };
}

export function useMusic(available: boolean, open: boolean) {
  const [player] = useState(() => new MusicPlayer(available));
  useEffect(() => {
    if (!open) return;
    void player.refresh();
    const timer = window.setInterval(() => void player.refresh(), 2000);
    return () => window.clearInterval(timer);
  }, [open, player]);
  useEffect(() => player.stop, [player]);
  return player;
}

export function Music({ player, back }: { player: MusicPlayer; back: () => void }) {
  const state = useSyncExternalStore(player.subscribe, player.snapshot);
  const highlighted = state.selected ?? state.current;
  const description = state.readError ?? state.playError ?? (state.current
    ? `${state.playing ? 'Đang phát' : 'Tạm dừng'}: ${trackTitle(state.current)}`
    : state.selected ? `Đã chọn: ${trackTitle(state.selected)}`
    : state.tracks.length ? `${state.tracks.length} bài trong thư mục` : 'Thêm nhạc vào thư mục nhac.');
  return (
    <section className="panel-page music" aria-labelledby="music-title">
      <header className="panel-header">
        <button className="panel-back" type="button" aria-label="Về cài đặt" title="Về cài đặt" onClick={back}>‹</button>
        <h1 id="music-title">Nhạc</h1>
      </header>
      <div className="songs" aria-label="Danh sách nhạc">
        {state.tracks.map(track => <button key={track.path} type="button" title={trackTitle(track)}
          aria-pressed={track.path === highlighted?.path} onClick={() => player.choose(track)}>{trackTitle(track)}</button>)}
      </div>
      <div className="music-footer">
        <p className="music-status" title={description} role="status">{description}</p>
        <button className="panel-primary" type="button" disabled={!state.tracks.length && !state.current} onClick={player.toggle}>
          {state.playing ? 'Tạm dừng' : state.current && !state.playError ? 'Tiếp tục' : 'Bắt đầu phát'}
        </button>
      </div>
    </section>
  );
}
