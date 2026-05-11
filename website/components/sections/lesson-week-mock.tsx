import { Check, Clock } from "lucide-react";
import { Mono } from "@/components/ui/mono";

interface Slot {
  time: string;
  level: string;
  capacity: string;
  status: "confirmed" | "pending" | "open";
}

const DAYS: { day: string; date: string; slots: Slot[] }[] = [
  {
    day: "MAA",
    date: "06",
    slots: [
      {
        time: "16:00",
        level: "Beginner",
        capacity: "4 / 4",
        status: "confirmed",
      },
      { time: "17:00", level: "Junior", capacity: "3 / 4", status: "pending" },
      { time: "18:00", level: "Comp.", capacity: "2 / 4", status: "open" },
    ],
  },
  {
    day: "WOE",
    date: "08",
    slots: [
      { time: "14:00", level: "Mini", capacity: "6 / 6", status: "confirmed" },
      {
        time: "15:00",
        level: "Beginner",
        capacity: "4 / 4",
        status: "confirmed",
      },
      { time: "16:00", level: "Junior", capacity: "1 / 4", status: "open" },
    ],
  },
  {
    day: "ZAT",
    date: "11",
    slots: [
      {
        time: "09:00",
        level: "Volwassen",
        capacity: "4 / 4",
        status: "confirmed",
      },
      {
        time: "10:00",
        level: "Volwassen",
        capacity: "3 / 4",
        status: "pending",
      },
    ],
  },
];

export function LessonWeekMock() {
  return (
    <div className="relative h-full overflow-hidden rounded-xl border border-rule bg-paper p-5">
      <div className="flex items-center justify-between">
        <Mono className="text-[10px] tracking-[0.12em] text-ink-3">
          WEEK 23 / TC COACHOS
        </Mono>
        <span className="inline-flex items-center gap-1.5 rounded-full bg-canvas px-2.5 py-1 text-[10px] font-semibold text-ink-2">
          <span className="h-1.5 w-1.5 rounded-full bg-tennis-green" />
          Voorstel klaar
        </span>
      </div>

      <h3 className="mt-3 text-lg font-bold tracking-tight">Voorjaarslessen</h3>
      <Mono className="text-xs text-ink-3">
        12 lessen · 6 trainers · 48 leerlingen
      </Mono>

      <div className="mt-5 space-y-3">
        {DAYS.map((day) => (
          <div key={day.day} className="flex gap-3">
            <div className="w-12 flex-shrink-0 rounded-md bg-canvas px-2 py-2 text-center">
              <Mono className="block text-[9px] tracking-[0.12em] text-ink-3">
                {day.day}
              </Mono>
              <Mono className="mt-0.5 block text-base font-bold leading-none">
                {day.date}
              </Mono>
            </div>
            <div className="flex-1 space-y-1.5">
              {day.slots.map((slot) => (
                <SlotRow key={`${day.day}-${slot.time}`} slot={slot} />
              ))}
            </div>
          </div>
        ))}
      </div>

      <div className="mt-5 flex items-center justify-between rounded-md bg-ink px-3 py-2.5 text-white">
        <div>
          <Mono className="block text-[9px] tracking-[0.12em] text-tennis-lime">
            BEVESTIGD
          </Mono>
          <Mono className="text-base font-extrabold text-tennis-lime">
            34 / 48
          </Mono>
        </div>
        <div className="text-right">
          <Mono className="block text-[9px] tracking-[0.12em] text-white/60">
            WACHT
          </Mono>
          <Mono className="text-base font-extrabold">14</Mono>
        </div>
      </div>
    </div>
  );
}

function SlotRow({ slot }: { slot: Slot }) {
  const borderColor =
    slot.status === "confirmed"
      ? "border-l-tennis-green"
      : slot.status === "pending"
        ? "border-l-warn"
        : "border-l-ink-3/40";

  return (
    <div
      className={`flex items-center justify-between rounded-r-md border-l-[3px] bg-canvas/60 px-3 py-2 ${borderColor}`}
    >
      <div className="flex items-center gap-3">
        <Mono className="text-xs font-semibold text-ink">{slot.time}</Mono>
        <span className="text-xs text-ink-2">{slot.level}</span>
      </div>
      <div className="flex items-center gap-2">
        <Mono className="text-[10px] text-ink-3">{slot.capacity}</Mono>
        {slot.status === "confirmed" ? (
          <Check className="h-3.5 w-3.5 text-tennis-green" />
        ) : slot.status === "pending" ? (
          <Clock className="h-3.5 w-3.5 text-warn" />
        ) : (
          <span className="h-1.5 w-1.5 rounded-full bg-ink-3/40" />
        )}
      </div>
    </div>
  );
}
