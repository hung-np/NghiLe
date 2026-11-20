const stems = ['Giáp', 'Ất', 'Bính', 'Đinh', 'Mậu', 'Kỷ', 'Canh', 'Tân', 'Nhâm', 'Quý'];
const branches = ['Tý', 'Sửu', 'Dần', 'Mão', 'Thìn', 'Tỵ', 'Ngọ', 'Mùi', 'Thân', 'Dậu', 'Tuất', 'Hợi'];

export interface Tet {
  year: number;
  name: string;
  date: string;
  timestamp: number;
}

export function loadCalendar(data: string): Tet[] {
  return data.trim().split(/\r?\n/).map(date => {
    const [year, month, day] = date.split('-').map(Number);
    const timestamp = Date.parse(`${date}T00:00:00+07:00`);
    if (!Number.isFinite(timestamp) || !year || !month || !day) throw new Error(`Ngày Tết không hợp lệ: ${date}`);
    return {
      year,
      name: `${stems[(year - 4) % 10]} ${branches[(year - 4) % 12]}`,
      date: `${String(day).padStart(2, '0')}/${String(month).padStart(2, '0')}/${year}`,
      timestamp
    };
  });
}

export function selectTarget(calendar: Tet[], automatic: boolean, year: number, now: number) {
  return calendar.find(day => automatic ? day.timestamp > now : day.year === year);
}

export function countdown(target: number, now: number) {
  const remaining = Math.max(0, Math.ceil((target - now) / 1000));
  return {
    days: Math.floor(remaining / 86400),
    hours: Math.floor(remaining / 3600) % 24,
    minutes: Math.floor(remaining / 60) % 60,
    seconds: remaining % 60,
    reached: target <= now
  };
}
