"use client";

import { useEffect, useRef, useState } from "react";
import {
  AnimatePresence,
  motion,
  useInView,
  useReducedMotion,
} from "motion/react";
import type { Variants } from "motion/react";
import { ArrowRight, CalendarDays, Loader2, Sparkles } from "lucide-react";
import { Mono } from "@/components/ui/mono";

interface Student {
  id: string;
  name: string;
  initials: string;
  color: string;
}

const STUDENTS: Student[] = [
  { id: "jan", name: "Jan de Vries", initials: "JV", color: "#2D5016" },
  { id: "maria", name: "Maria Verhoeven", initials: "MV", color: "#3B82F6" },
  { id: "pieter", name: "Pieter Bakker", initials: "PB", color: "#8B5CF6" },
  { id: "anke", name: "Anke Vandenberghe", initials: "AV", color: "#EC4899" },
  { id: "luc", name: "Luc Desmet", initials: "LD", color: "#F59E0B" },
  { id: "sofie", name: "Sofie Geerts", initials: "SG", color: "#14B8A6" },
  { id: "tim", name: "Tim Janssens", initials: "TJ", color: "#EF4444" },
  { id: "eva", name: "Eva De Smet", initials: "ES", color: "#06B6D4" },
];

const STUDENT_BY_ID = Object.fromEntries(STUDENTS.map((s) => [s.id, s]));

interface Slot {
  day: string;
  time: string;
  level: string;
  capacity: number;
  studentIds: string[];
}

/** Order matters — drives the sequential fill animation. */
const SLOTS: Slot[] = [
  {
    day: "Maandag",
    time: "18:00",
    level: "Beginners",
    capacity: 4,
    studentIds: ["jan", "pieter"],
  },
  {
    day: "Woensdag",
    time: "14:00",
    level: "Mini's",
    capacity: 4,
    studentIds: ["anke"],
  },
  {
    day: "Woensdag",
    time: "16:00",
    level: "Intermediate",
    capacity: 4,
    studentIds: ["maria", "sofie"],
  },
  {
    day: "Zaterdag",
    time: "10:00",
    level: "Volwassen",
    capacity: 4,
    studentIds: ["luc", "tim", "eva"],
  },
];

/** Calendar grouped by day for rendering. */
const SLOTS_BY_DAY: {
  day: string;
  slots: { slotIdx: number; slot: Slot }[];
}[] = [
  { day: "Maandag", slots: [] },
  { day: "Woensdag", slots: [] },
  { day: "Zaterdag", slots: [] },
];
SLOTS.forEach((slot, slotIdx) => {
  const group = SLOTS_BY_DAY.find((g) => g.day === slot.day);
  if (group) group.slots.push({ slotIdx, slot });
});

type Scene = "enrollments" | "loading" | "calendar";

interface Step {
  scene: Scene;
  pulseButton?: boolean;
  /** Number of slots filled (0..SLOTS.length). */
  slotsFilled?: number;
  showStats?: boolean;
}

const STEPS: Step[] = [
  { scene: "enrollments" }, // 0: list entrance
  { scene: "enrollments" }, // 1: read pause
  { scene: "enrollments", pulseButton: true }, // 2: pulse
  { scene: "loading" }, // 3: loading
  { scene: "calendar", slotsFilled: 0 }, // 4: empty grid
  { scene: "calendar", slotsFilled: 1 }, // 5
  { scene: "calendar", slotsFilled: 2 }, // 6
  { scene: "calendar", slotsFilled: 3 }, // 7
  { scene: "calendar", slotsFilled: 4 }, // 8
  { scene: "calendar", slotsFilled: 4, showStats: true }, // 9: final
];

const STEP_DURATIONS_MS = [1300, 700, 700, 900, 700, 600, 600, 600, 600, 0];

const containerVariants: Variants = {
  hidden: {},
  visible: { transition: { delayChildren: 0.05, staggerChildren: 0.05 } },
};

const fadeUp: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { type: "spring", stiffness: 220, damping: 26 },
  },
};

export function AnimatedPlanningMock() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { amount: 0.4, once: true });
  const prefersReducedMotion = useReducedMotion();
  const [stepIndex, setStepIndex] = useState(0);

  const isComplete = stepIndex >= STEPS.length - 1;

  const effectiveStep: Step = prefersReducedMotion
    ? STEPS[STEPS.length - 1]
    : STEPS[stepIndex];

  useEffect(() => {
    if (prefersReducedMotion || !inView || isComplete) return;
    const t = window.setTimeout(() => {
      setStepIndex((s) => s + 1);
    }, STEP_DURATIONS_MS[stepIndex]);
    return () => window.clearTimeout(t);
  }, [stepIndex, inView, isComplete, prefersReducedMotion]);

  const animate = prefersReducedMotion || inView ? "visible" : "hidden";

  return (
    <motion.div
      ref={ref}
      className="relative bg-paper p-5"
      initial={prefersReducedMotion ? "visible" : "hidden"}
      animate={animate}
      variants={containerVariants}
    >
      {/* Reserve max height so scene swaps don't reflow the row. */}
      <div className="min-h-[420px]">
        <AnimatePresence mode="wait" initial={false}>
          {effectiveStep.scene === "enrollments" ? (
            <EnrollmentsScene
              key="enrollments"
              pulseButton={effectiveStep.pulseButton ?? false}
            />
          ) : effectiveStep.scene === "loading" ? (
            <LoadingScene key="loading" />
          ) : (
            <CalendarScene
              key="calendar"
              slotsFilled={effectiveStep.slotsFilled ?? 0}
              showStats={effectiveStep.showStats ?? false}
            />
          )}
        </AnimatePresence>
      </div>
    </motion.div>
  );
}

// ─────────────────────────────────────────────────────────
// Scene 1: Enrollments list
// ─────────────────────────────────────────────────────────

function EnrollmentsScene({ pulseButton }: { pulseButton: boolean }) {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0, x: -12 }}
      transition={{ duration: 0.25 }}
      variants={containerVariants}
      className="space-y-4"
    >
      <motion.div
        variants={fadeUp}
        className="flex items-center justify-between"
      >
        <div>
          <Mono className="text-[10px] tracking-[0.12em] text-ink-3">
            VOORJAARSLESSEN
          </Mono>
          <h3 className="mt-0.5 text-base font-bold tracking-tight">
            8 inschrijvingen
          </h3>
        </div>
        <span className="inline-flex items-center gap-1.5 rounded-full bg-warn/10 px-2.5 py-1 text-[10px] font-semibold text-warn">
          <span className="h-1.5 w-1.5 rounded-full bg-warn" />
          Wacht op planning
        </span>
      </motion.div>

      <motion.div
        variants={fadeUp}
        className="rounded-xl border border-rule bg-canvas/40 divide-y divide-rule"
      >
        <motion.div
          variants={containerVariants}
          className="divide-y divide-rule"
        >
          {STUDENTS.map((s) => (
            <motion.div
              key={s.id}
              variants={fadeUp}
              className="flex items-center gap-3 px-3 py-2"
            >
              <Avatar student={s} size={20} />
              <div className="min-w-0 flex-1">
                <div className="truncate text-[12px] font-semibold leading-tight text-ink">
                  {s.name}
                </div>
                <div className="truncate text-[10px] leading-tight text-ink-3">
                  {s.id}@example.be
                </div>
              </div>
              <Mono className="text-[9px] tracking-tight text-ink-3">Solo</Mono>
            </motion.div>
          ))}
        </motion.div>
      </motion.div>

      <motion.div variants={fadeUp} className="flex items-center justify-end">
        <motion.div
          animate={
            pulseButton
              ? {
                  boxShadow: [
                    "0 0 0 0 rgba(45,80,22,0)",
                    "0 0 0 8px rgba(45,80,22,0.18)",
                    "0 0 0 0 rgba(45,80,22,0)",
                  ],
                }
              : { boxShadow: "0 0 0 0 rgba(45,80,22,0)" }
          }
          transition={{ duration: 0.55 }}
          className="inline-flex items-center gap-1.5 rounded-md bg-tennis-green px-3.5 py-2 text-[12px] font-semibold text-paper"
        >
          <CalendarDays className="h-3.5 w-3.5" />
          Plan lessen
          <ArrowRight className="h-3.5 w-3.5" />
        </motion.div>
      </motion.div>
    </motion.div>
  );
}

// ─────────────────────────────────────────────────────────
// Scene transitional: Loading
// ─────────────────────────────────────────────────────────

function LoadingScene() {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.18 }}
      className="flex h-full min-h-[420px] flex-col items-center justify-center gap-3 text-center"
    >
      <motion.div
        animate={{ rotate: 360 }}
        transition={{ duration: 1.2, repeat: Infinity, ease: "linear" }}
        className="text-tennis-green"
      >
        <Loader2 className="h-7 w-7" strokeWidth={2.2} />
      </motion.div>
      <div>
        <div className="text-[13px] font-bold tracking-tight">
          Planning genereren…
        </div>
        <div className="mt-1 text-[11px] text-ink-3">
          Voorkeuren matchen, groepen vormen, slots vullen.
        </div>
      </div>
    </motion.div>
  );
}

// ─────────────────────────────────────────────────────────
// Scene 2: Calendar
// ─────────────────────────────────────────────────────────

function CalendarScene({
  slotsFilled,
  showStats,
}: {
  slotsFilled: number;
  showStats: boolean;
}) {
  const totalAssigned = SLOTS.slice(0, slotsFilled).reduce(
    (sum, s) => sum + s.studentIds.length,
    0,
  );

  return (
    <motion.div
      initial={{ opacity: 0, x: 12 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.3, ease: "easeOut" }}
      className="space-y-3"
    >
      <div className="flex items-center justify-between">
        <div>
          <Mono className="text-[10px] tracking-[0.12em] text-ink-3">
            VOORJAARSLESSEN · WEEK 23
          </Mono>
          <h3 className="mt-0.5 text-base font-bold tracking-tight">
            Voorgestelde planning
          </h3>
        </div>
        <AnimatePresence>
          {showStats ? (
            <motion.span
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.9 }}
              transition={{ type: "spring", stiffness: 320, damping: 22 }}
              className="inline-flex items-center gap-1.5 rounded-full bg-tennis-green px-2.5 py-1 text-[10px] font-semibold text-tennis-lime"
            >
              <Sparkles className="h-3 w-3" strokeWidth={2.5} />
              Auto-toegewezen · {totalAssigned}/8
            </motion.span>
          ) : null}
        </AnimatePresence>
      </div>

      <div className="space-y-2.5">
        {SLOTS_BY_DAY.map((group) => (
          <DayGroup key={group.day} group={group} slotsFilled={slotsFilled} />
        ))}
      </div>
    </motion.div>
  );
}

function DayGroup({
  group,
  slotsFilled,
}: {
  group: (typeof SLOTS_BY_DAY)[number];
  slotsFilled: number;
}) {
  return (
    <div>
      <Mono className="text-[10px] tracking-[0.18em] text-ink-3">
        {group.day.toUpperCase()}
      </Mono>
      <div className="mt-1.5 space-y-1.5">
        {group.slots.map(({ slotIdx, slot }) => (
          <SlotCard
            key={`${group.day}-${slot.time}`}
            slot={slot}
            filled={slotIdx < slotsFilled}
          />
        ))}
      </div>
    </div>
  );
}

function SlotCard({ slot, filled }: { slot: Slot; filled: boolean }) {
  const occupancy = filled ? slot.studentIds.length : 0;

  return (
    <motion.div
      layout
      animate={{
        borderColor: filled ? "#86A36F" : "#e7e4dc",
        backgroundColor: filled
          ? "rgba(45,80,22,0.04)"
          : "rgba(245,244,241,0.5)",
      }}
      transition={{ duration: 0.3 }}
      className="rounded-lg border bg-paper p-2.5"
    >
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <Mono className="text-[11px] font-semibold text-ink">
            {slot.time}
          </Mono>
          <span className="rounded bg-canvas px-1.5 py-0.5 text-[9px] font-medium text-ink-2">
            {slot.level}
          </span>
        </div>
        <Mono
          className={
            filled
              ? "text-[10px] font-semibold text-tennis-green"
              : "text-[10px] text-ink-3"
          }
        >
          {occupancy} / {slot.capacity}
        </Mono>
      </div>

      <div className="mt-1.5 flex min-h-[20px] items-center gap-2">
        <AnimatePresence>
          {filled
            ? slot.studentIds.map((sid, i) => {
                const student = STUDENT_BY_ID[sid];
                return (
                  <motion.div
                    key={sid}
                    initial={{ opacity: 0, scale: 0.4, y: -4 }}
                    animate={{ opacity: 1, scale: 1, y: 0 }}
                    exit={{ opacity: 0, scale: 0.4 }}
                    transition={{
                      type: "spring",
                      stiffness: 360,
                      damping: 22,
                      delay: i * 0.12,
                    }}
                    className="flex items-center gap-1.5"
                  >
                    <Avatar student={student} size={18} />
                    <span className="text-[10px] text-ink-2">
                      {student.name.split(" ")[0]}
                    </span>
                  </motion.div>
                );
              })
            : null}
        </AnimatePresence>
        {!filled ? (
          <span className="text-[10px] italic text-ink-3">
            Wacht op planning…
          </span>
        ) : null}
      </div>
    </motion.div>
  );
}

// ─────────────────────────────────────────────────────────
// Avatar
// ─────────────────────────────────────────────────────────

function Avatar({ student, size }: { student: Student; size: number }) {
  return (
    <span
      style={{
        width: size,
        height: size,
        backgroundColor: student.color,
      }}
      className="inline-flex shrink-0 items-center justify-center rounded-full text-[8px] font-bold text-paper"
    >
      {student.initials}
    </span>
  );
}
