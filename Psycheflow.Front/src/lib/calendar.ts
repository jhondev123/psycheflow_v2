import type { AgendaItem, ScheduleStatus } from "@/types";
import { hm } from "@/lib/format";
import { toMinutes } from "@/lib/time";

export const DAY_START_HOUR = 7;
export const DAY_END_HOUR = 21;
export const HOUR_PX = 56;
export const GRID_HEIGHT = (DAY_END_HOUR - DAY_START_HOUR) * HOUR_PX;

/** Distância do topo da grade para um horário "HH:mm". */
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

const statusColors: Record<ScheduleStatus, EventColors> = {
  Pending: { color: "var(--warning)", bg: "var(--warning-bg)" },
  Confirmed: { color: "var(--success)", bg: "var(--success-bg)" },
  Cancelled: { color: "var(--danger)", bg: "var(--danger-bg)" },
};

const sessionDoneColors: EventColors = { color: "var(--info)", bg: "var(--info-bg)" };
const blockColors: EventColors = { color: "var(--text-soft)", bg: "var(--surface-2)" };

export interface CalEvent {
  item: AgendaItem;
  start: string;
  end: string;
  startMin: number;
  endMin: number;
  title: string;
  isBlock: boolean;
  isCancelled: boolean;
  colors: EventColors;
  lane: number;
  lanes: number;
}

export function toEvent(item: AgendaItem): CalEvent {
  const isBlock = item.type === "Block";
  const start = hm(item.startTime);
  const end = hm(item.endTime);
  const finished = item.sessionStatus === "Completed" || item.sessionStatus === "NoShow";
  return {
    item,
    start,
    end,
    startMin: toMinutes(start),
    endMin: toMinutes(end),
    title: isBlock ? item.blockReason || "Bloqueio" : item.patientName ?? "Sessão",
    isBlock,
    isCancelled: item.status === "Cancelled",
    colors: isBlock ? blockColors : finished ? sessionDoneColors : statusColors[item.status],
    lane: 0,
    lanes: 1,
  };
}

/** Eventos de um dia, ordenados por horário. */
export function eventsForDate(items: AgendaItem[], dateISO: string): CalEvent[] {
  return items
    .filter((i) => i.date === dateISO)
    .map(toEvent)
    .sort((a, b) => a.startMin - b.startMin || a.endMin - b.endMin);
}

/** Distribui eventos sobrepostos em colunas lado a lado. */
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
