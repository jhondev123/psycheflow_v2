/** Time-of-day helpers working with "HH:mm" strings and minute integers. */

export function toMinutes(hhmm: string): number {
  const [h, m] = hhmm.split(":").map(Number);
  return h * 60 + (m || 0);
}

export function fromMinutes(total: number): string {
  const clamped = Math.max(0, Math.min(24 * 60, total));
  const h = Math.floor(clamped / 60);
  const m = clamped % 60;
  return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
}

/** Inclusive-exclusive overlap test on minute ranges. */
export function rangesOverlap(aStart: number, aEnd: number, bStart: number, bEnd: number): boolean {
  return aStart < bEnd && bStart < aEnd;
}

/** True when [innerStart, innerEnd] fits inside [outerStart, outerEnd]. */
export function rangeContains(outerStart: number, outerEnd: number, innerStart: number, innerEnd: number): boolean {
  return innerStart >= outerStart && innerEnd <= outerEnd;
}

export function durationLabel(start: string, end: string): string {
  const mins = toMinutes(end) - toMinutes(start);
  if (mins <= 0) return "—";
  const h = Math.floor(mins / 60);
  const m = mins % 60;
  if (h && m) return `${h}h${m}`;
  if (h) return `${h}h`;
  return `${m}min`;
}

/** Build "HH:mm" options every `stepMin` minutes between two hours. */
export function timeOptions(startHour = 6, endHour = 22, stepMin = 30): string[] {
  const out: string[] = [];
  for (let t = startHour * 60; t <= endHour * 60; t += stepMin) out.push(fromMinutes(t));
  return out;
}
