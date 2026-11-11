export interface CalendarColor {
  id: string;
  name: string;
  color: string;
}

export const CALENDAR_COLORS: CalendarColor[] = [
  { id: '', name: 'Mặc định', color: '#6366f1' },
  { id: '1', name: 'Lavender', color: '#7986cb' },
  { id: '2', name: 'Sage', color: '#33b679' },
  { id: '3', name: 'Grape', color: '#8e24aa' },
  { id: '4', name: 'Flamingo', color: '#e67c73' },
  { id: '5', name: 'Banana', color: '#f6bf26' },
  { id: '6', name: 'Tangerine', color: '#f4511e' },
  { id: '7', name: 'Peacock', color: '#039be5' },
  { id: '8', name: 'Graphite', color: '#616161' },
  { id: '9', name: 'Blueberry', color: '#3f51b5' },
  { id: '10', name: 'Basil', color: '#0b8043' },
  { id: '11', name: 'Tomato', color: '#d50000' },
];

export const getEventColor = (colorId?: string): string => {
  if (!colorId) return '#6366f1';
  const found = CALENDAR_COLORS.find(c => c.id === colorId);
  return found ? found.color : '#6366f1';
};

export const toLocalIsoString = (d: Date): string => {
  const pad = (n: number) => (n < 10 ? '0' + n : n);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

export const isSameDay = (d1: Date, d2: Date): boolean => {
  return d1.getFullYear() === d2.getFullYear() &&
         d1.getMonth() === d2.getMonth() &&
         d1.getDate() === d2.getDate();
};

export const isToday = (d: Date): boolean => isSameDay(d, new Date());

export const isSameMonth = (d: Date, baseDate: Date): boolean => {
  return d.getFullYear() === baseDate.getFullYear() &&
         d.getMonth() === baseDate.getMonth();
};

export const getWeekDays = (currentDate: Date): Date[] => {
  const curr = new Date(currentDate);
  const dayOfWeek = curr.getDay();
  const diffToMon = dayOfWeek === 0 ? 6 : dayOfWeek - 1;

  const monday = new Date(curr);
  monday.setDate(monday.getDate() - diffToMon);

  const days: Date[] = [];
  for (let i = 0; i < 7; i++) {
    const d = new Date(monday);
    d.setDate(monday.getDate() + i);
    days.push(d);
  }
  return days;
};

export const getMonthGridDays = (currentDate: Date): Date[] => {
  const year = currentDate.getFullYear();
  const month = currentDate.getMonth();
  const firstDay = new Date(year, month, 1);

  const dayOfWeek = firstDay.getDay();
  const diffToMon = dayOfWeek === 0 ? 6 : dayOfWeek - 1;

  const startDate = new Date(firstDay);
  startDate.setDate(startDate.getDate() - diffToMon);

  const lastDay = new Date(year, month + 1, 0);
  const totalDaysNeeded = Math.ceil((diffToMon + lastDay.getDate()) / 7) * 7;
  const cellCount = Math.max(35, totalDaysNeeded);

  const days: Date[] = [];
  const iter = new Date(startDate);
  for (let i = 0; i < cellCount; i++) {
    days.push(new Date(iter));
    iter.setDate(iter.getDate() + 1);
  }
  return days;
};
