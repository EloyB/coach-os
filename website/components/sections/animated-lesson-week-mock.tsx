"use client";

import { useEffect, useRef, useState } from "react";
import { motion, useInView, useReducedMotion } from "motion/react";
import type { Variants } from "motion/react";
import { Check, Clock } from "lucide-react";
import { Mono } from "@/components/ui/mono";

type SlotStatus = "confirmed" | "pending" | "open";

interface Slot {
  time: string;
  level: string;
  capacity: string;
  status: SlotStatus;
}

interface Day {
  day: string;
  date: string;
  slots: Slot[];
}

const BASE_DAYS: Day[] = [
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
      // The slot we drive through the live loop:
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

/**
 * Indices into the loop slot. Each step is one frame of the live loop, 
 * a new enrollment ticks capacity up, eventually flipping status.
 */
const LOOP_STEPS: Array<{
  capacity: string;
  status: SlotStatus;
  confirmed: number;
}> = [
  { capacity: "1 / 4", status: "open", confirmed: 34 },
  { capacity: "2 / 4", status: "pending", confirmed: 35 },
  { capacity: "3 / 4", status: "pending", confirmed: 36 },
  { capacity: "4 / 4", status: "confirmed", confirmed: 37 },
];

const containerVariants: Variants = {
  hidden: {},
  visible: {
    transition: {
      delayChildren: 0.05,
      staggerChildren: 0.08,
    },
  },
};

const dayVariants: Variants = {
  hidden: { opacity: 0, x: -16 },
  visible: {
    opacity: 1,
    x: 0,
    transition: { type: "spring", stiffness: 220, damping: 26 },
  },
};

const slotsContainerVariants: Variants = {
  hidden: {},
  visible: {
    transition: { staggerChildren: 0.06, delayChildren: 0.04 },
  },
};

const slotVariants: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { type: "spring", stiffness: 260, damping: 28 },
  },
};

const fadeUp: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { type: "spring", stiffness: 220, damping: 26 },
  },
};

export function AnimatedLessonWeekMock() {
  const ref = useRef<HTMLDivElement>(null);
  // `once: true` latches inView the first time the row is scrolled into
  // view; the sequence then runs to completion and stops at 4/4 confirmed.
  const inView = useInView(ref, { amount: 0.4, once: true });
  const prefersReducedMotion = useReducedMotion();
  const [step, setStep] = useState(0);

  const isComplete = step >= LOOP_STEPS.length - 1;

  useEffect(() => {
    if (prefersReducedMotion || !inView || isComplete) return;
    const t = window.setTimeout(() => {
      setStep((s) => s + 1);
    }, 1900);
    return () => window.clearTimeout(t);
  }, [step, inView, isComplete, prefersReducedMotion]);

  const current = prefersReducedMotion
    ? LOOP_STEPS[LOOP_STEPS.length - 1]
    : LOOP_STEPS[step];

  // Replace the WOE 16:00 slot with the live values
  const days: Day[] = BASE_DAYS.map((d) => {
    if (d.day !== "WOE") return d;
    return {
      ...d,
      slots: d.slots.map((s) =>
        s.time === "16:00" && s.level === "Junior"
          ? { ...s, capacity: current.capacity, status: current.status }
          : s,
      ),
    };
  });

  const animate = prefersReducedMotion || inView ? "visible" : "hidden";

  return (
    <motion.div
      ref={ref}
      className="relative h-full overflow-hidden bg-paper p-5"
      initial={prefersReducedMotion ? "visible" : "hidden"}
      animate={animate}
      variants={containerVariants}
    >
      <motion.div
        variants={fadeUp}
        className="flex items-center justify-between"
      >
        <Mono className="text-[10px] tracking-[0.12em] text-ink-3">
          WEEK 23 / TC COACHOS
        </Mono>
        <span className="inline-flex items-center gap-1.5 rounded-full bg-canvas px-2.5 py-1 text-[10px] font-semibold text-ink-2">
          <motion.span
            className="h-1.5 w-1.5 rounded-full bg-tennis-green"
            animate={
              prefersReducedMotion || isComplete
                ? undefined
                : { opacity: [0.6, 1, 0.6], scale: [1, 1.25, 1] }
            }
            transition={{ duration: 1.8, repeat: Infinity, ease: "easeInOut" }}
          />
          {isComplete ? "Voorstel klaar" : "Live planning"}
        </span>
      </motion.div>

      <motion.h3
        variants={fadeUp}
        className="mt-3 text-lg font-bold tracking-tight"
      >
        Voorjaarslessen
      </motion.h3>
      <motion.div variants={fadeUp}>
        <Mono className="text-xs text-ink-3">
          12 lessen · 6 trainers · 48 leerlingen
        </Mono>
      </motion.div>

      <div className="mt-5 space-y-3">
        {days.map((day) => (
          <motion.div
            key={day.day}
            variants={dayVariants}
            className="flex gap-3"
          >
            <div className="w-12 flex-shrink-0 rounded-md bg-canvas px-2 py-2 text-center">
              <Mono className="block text-[9px] tracking-[0.12em] text-ink-3">
                {day.day}
              </Mono>
              <Mono className="mt-0.5 block text-base font-bold leading-none">
                {day.date}
              </Mono>
            </div>
            <motion.div
              variants={slotsContainerVariants}
              className="flex-1 space-y-1.5"
            >
              {day.slots.map((slot) => (
                <motion.div
                  key={`${day.day}-${slot.time}`}
                  variants={slotVariants}
                >
                  <SlotRow slot={slot} />
                </motion.div>
              ))}
            </motion.div>
          </motion.div>
        ))}
      </div>

      <motion.div
        variants={fadeUp}
        className="mt-5 flex items-center justify-between rounded-md bg-ink px-3 py-2.5 text-white"
      >
        <div>
          <Mono className="block text-[9px] tracking-[0.12em] text-tennis-lime">
            BEVESTIGD
          </Mono>
          <div className="flex items-baseline gap-1.5 text-base font-extrabold text-tennis-lime">
            <AnimatedNumber value={current.confirmed} />
            <span>/ 48</span>
          </div>
        </div>
        <div className="text-right">
          <Mono className="block text-[9px] tracking-[0.12em] text-white/60">
            WACHT
          </Mono>
          <div className="text-base font-extrabold">
            <AnimatedNumber value={48 - current.confirmed} />
          </div>
        </div>
      </motion.div>
    </motion.div>
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
    <motion.div
      layout
      transition={{ type: "spring", stiffness: 280, damping: 30 }}
      className={`flex items-center justify-between rounded-r-md border-l-[3px] bg-canvas/60 px-3 py-2 ${borderColor}`}
    >
      <div className="flex items-center gap-3">
        <Mono className="text-xs font-semibold text-ink">{slot.time}</Mono>
        <span className="text-xs text-ink-2">{slot.level}</span>
      </div>
      <div className="flex items-center gap-2">
        <motion.span
          key={slot.capacity}
          initial={{ opacity: 0, y: -4 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.25 }}
        >
          <Mono className="text-[10px] text-ink-3">{slot.capacity}</Mono>
        </motion.span>
        <StatusIcon status={slot.status} />
      </div>
    </motion.div>
  );
}

function StatusIcon({ status }: { status: SlotStatus }) {
  return (
    <motion.span
      key={status}
      initial={{ scale: 0.6, opacity: 0 }}
      animate={{ scale: 1, opacity: 1 }}
      transition={{ type: "spring", stiffness: 360, damping: 22 }}
      className="inline-flex h-3.5 w-3.5 items-center justify-center"
    >
      {status === "confirmed" ? (
        <Check className="h-3.5 w-3.5 text-tennis-green" />
      ) : status === "pending" ? (
        <Clock className="h-3.5 w-3.5 text-warn" />
      ) : (
        <span className="h-1.5 w-1.5 rounded-full bg-ink-3/40" />
      )}
    </motion.span>
  );
}

function AnimatedNumber({ value }: { value: number }) {
  return (
    <motion.span
      key={value}
      initial={{ opacity: 0, y: -6 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.25 }}
      className="inline-block tabular-nums"
    >
      {value}
    </motion.span>
  );
}
