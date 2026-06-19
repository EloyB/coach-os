"use client";

import { useEffect, useRef, useState } from "react";
import { motion, useInView, useReducedMotion } from "motion/react";
import type { Variants } from "motion/react";
import { Clock, GraduationCap } from "lucide-react";
import { Mono } from "@/components/ui/mono";

interface TrainerStint {
  name: string;
  hours: string;
}

interface CampDay {
  day: string;
  date: string;
  hours: string;
  trainers: TrainerStint[];
}

const DAYS: CampDay[] = [
  {
    day: "MA",
    date: "14 apr",
    hours: "09:00 - 16:00",
    trainers: [
      { name: "Jan J.", hours: "09:00 - 12:00" },
      { name: "Pieter M.", hours: "12:00 - 16:00" },
    ],
  },
  {
    day: "DI",
    date: "15 apr",
    hours: "09:00 - 16:00",
    trainers: [{ name: "Jan J.", hours: "09:00 - 16:00" }],
  },
  {
    day: "WO",
    date: "16 apr",
    hours: "10:00 - 15:00",
    trainers: [{ name: "Sophie D.", hours: "10:00 - 15:00" }],
  },
];

const CAPACITY = 20;

/**
 * Live loop: enrollments tick up toward capacity. The last step flips the
 * header badge to "Bijna vol" and stops.
 */
const LOOP_STEPS: number[] = [14, 16, 18, 20];

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

const chipsContainerVariants: Variants = {
  hidden: {},
  visible: {
    transition: { staggerChildren: 0.06, delayChildren: 0.04 },
  },
};

const chipVariants: Variants = {
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

export function AnimatedCampMock() {
  const ref = useRef<HTMLDivElement>(null);
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

  const enrolled = prefersReducedMotion
    ? LOOP_STEPS[LOOP_STEPS.length - 1]
    : LOOP_STEPS[step];
  const spotsLeft = CAPACITY - enrolled;

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
          PAASKAMP 2026 / TC COACHOS
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
          {isComplete ? "Bijna vol" : "Inschrijvingen open"}
        </span>
      </motion.div>

      <motion.h3
        variants={fadeUp}
        className="mt-3 text-lg font-bold tracking-tight"
      >
        Paaskamp Gevorderden
      </motion.h3>
      <motion.div variants={fadeUp}>
        <Mono className="text-xs text-ink-3">
          3 dagen · 14 tot 16 april · €95
        </Mono>
      </motion.div>

      <div className="mt-5 space-y-2.5">
        {DAYS.map((day) => (
          <motion.div
            key={day.day}
            variants={dayVariants}
            className="flex gap-3"
          >
            <div className="w-12 flex-shrink-0 rounded-md bg-canvas px-2 py-2 text-center">
              <Mono className="block text-[9px] tracking-[0.12em] text-ink-3">
                {day.day}
              </Mono>
              <Mono className="mt-0.5 block text-sm font-bold leading-none">
                {day.date}
              </Mono>
            </div>
            <div className="flex-1 rounded-md bg-canvas/60 px-3 py-2">
              <div className="flex items-center gap-1.5 text-ink-2">
                <Clock className="h-3 w-3 text-tennis-green" />
                <Mono className="text-[11px] font-semibold text-ink">
                  {day.hours}
                </Mono>
              </div>
              <motion.div
                variants={chipsContainerVariants}
                className="mt-2 flex flex-wrap gap-1.5"
              >
                {day.trainers.map((t) => (
                  <motion.span
                    key={`${day.day}-${t.name}-${t.hours}`}
                    variants={chipVariants}
                    className="inline-flex items-center gap-1.5 rounded-full bg-white px-2 py-1 text-[10px] text-ink-2 ring-1 ring-rule"
                  >
                    <GraduationCap className="h-3 w-3 text-tennis-green" />
                    <span className="font-semibold text-ink">{t.name}</span>
                    <Mono className="text-ink-3">{t.hours}</Mono>
                  </motion.span>
                ))}
              </motion.div>
            </div>
          </motion.div>
        ))}
      </div>

      <motion.div
        variants={fadeUp}
        className="mt-5 flex items-center justify-between rounded-md bg-ink px-3 py-2.5 text-white"
      >
        <div>
          <Mono className="block text-[9px] tracking-[0.12em] text-tennis-lime">
            INGESCHREVEN
          </Mono>
          <div className="flex items-baseline gap-1.5 text-base font-extrabold text-tennis-lime">
            <AnimatedNumber value={enrolled} />
            <span>/ {CAPACITY}</span>
          </div>
        </div>
        <div className="text-right">
          <Mono className="block text-[9px] tracking-[0.12em] text-white/60">
            PLEKKEN VRIJ
          </Mono>
          <div className="text-base font-extrabold">
            <AnimatedNumber value={spotsLeft} />
          </div>
        </div>
      </motion.div>
    </motion.div>
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
