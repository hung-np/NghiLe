import { useEffect, useState } from 'react';

export function useClock(visible: boolean) {
  const [now, setNow] = useState(Date.now);
  useEffect(() => {
    if (!visible) return;
    let timer: number;
    const refresh = () => {
      window.clearTimeout(timer);
      const current = Date.now();
      setNow(current);
      // Use wall time after each wakeup to avoid accumulating timer drift.
      timer = window.setTimeout(refresh, 1000 - current % 1000);
    };
    refresh();
    window.addEventListener('focus', refresh);
    return () => {
      window.clearTimeout(timer);
      window.removeEventListener('focus', refresh);
    };
  }, [visible]);
  return now;
}

