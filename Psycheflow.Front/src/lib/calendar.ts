import { type Database, type Schedule, ScheduleStatus, ScheduleType } from "@/types";
import { toMinutes } from "@/lib/time";

export const DAY_START_HOUR = 7;
export const DAY_END_HOUR = 21;
export const HOUR_PX = 56;
export const GRID_HEIGHT = (DAY_END_HOUR - DAY_START_HOUR) * HOUR_PX;

/** Pixel offset from the top of the grid for a given "HH:mm". */
export function yForTime(hhmm: string): number {
  return ((toMinutes(hhmm) - DAY_START_HOUR * 60) / 60) * HOUR_PX;
}

export function yForMinutes(min: number): number {
  return ((min - DAY_START_HOUR * 60) / 60) * HOUR_PX;
}

export interface EventColors {
  color: string;
  bg: string;
}

/** Schedule-status colours for the week/month events (CSS vars). */
export const statusColors: Record<ScheduleStatus, EventColors> = {
  [ScheduleStatus.Pending]: { color: "var(--warning)", bg: "var(--warning-bg)" },
  [ScheduleStatus.Confirmed]: { color: "var(--success)", bg: "var(--success-bg)" },
  [ScheduleStatus.Cancelled]: { color: "var(--danger)", bg: "var(--danger-bg)" },
};

export interface CalEvent {
  schedule: Schedule;
  startMin: number;
  endMin: number;
  title: string;
  patientName?: string;
  isBlock: boolean;
  isCancelled: boolean;
  colors: EventColors;
  // layout (filled by packDay)
  lane: number;
  lanes: number;
}

/** Builds calendar events for one psychologist on one ISO date. */
export function eventsForDate(db: Database, psychologistId: string, dateISO: string): CalEvent[] {
  return db.schedules
    .filter((s) => s.psychologistId === psychologistId && s.date === dateISO)
    .map((s) => toEvent(db, s))
    .sort((a, b) => a.startMin - b.startMin || a.endMin - b.endMin);
}

export function toEvent(db: Database, s: Schedule): CalEvent {
  const isBlock = s.type === ScheduleType.BLOCK;
  let title = s.title ?? "Bloqueio";
  let patientName: string | undefined;
  if (!isBlock) {
    const session = db.sessions.find((ss) => ss.id === s.sessionId);
    const patient = session && db.patients.find((p) => p.id === session.patientId);
    patientName = patient?.name;
    title = patient?.name ?? "Sessão";
  }
  return {
    schedule: s,
    startMin: toMinutes(s.start),
    endMin: toMinutes(s.end),
    title,
    patientName,
    isBlock,
    isCancelled: s.status === ScheduleStatus.Cancelled,
    colors: statusColors[s.status],
    lane: 0,
    lanes: 1,
  };
}

/**
 * Assigns each event a lane and the number of lanes in its overlap cluster,
 * so overlapping events can be laid out side by side.
 */
export function packDay(events: CalEvent[]): CalEvent[] {
  const sorted = [...events].sort((a, b) => a.startMin - b.startMin || a.endMin - b.endMin);

  let clusterStart = 0;
  let clusterMaxEnd = -1;
  const laneEnds: number[] = [];

  const flush = (from: number, to: number) => {
    const cols = Math.max(1, ...sorted.slice(from, to).map((e) => e.lane + 1));
    for (let i = from; i < to; i++) sorted[i].lanes = cols;
  };

  sorted.forEach((ev, i) => {
    if (ev.startMin >= clusterMaxEnd) {
      // close previous cluster
      if (i > clusterStart) flush(clusterStart, i);
      clusterStart = i;
      laneEnds.length = 0;
    }
    let lane = laneEnds.findIndex((end) => end <= ev.startMin);
    if (lane === -1) {
      lane = laneEnds.length;
      laneEnds.push(ev.endMin);
    } else {
      laneEnds[lane] = ev.endMin;
    }
    ev.lane = lane;
    clusterMaxEnd = Math.max(clusterMaxEnd, ev.endMin);
  });
  flush(clusterStart, sorted.length);

  return sorted;
}
