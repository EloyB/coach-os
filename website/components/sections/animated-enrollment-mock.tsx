"use client";

import { useEffect, useRef, useState } from "react";
import {
  AnimatePresence,
  motion,
  useInView,
  useReducedMotion,
} from "motion/react";
import type { Variants } from "motion/react";
import { Check, CheckCircle2, Loader2, User, Users, X } from "lucide-react";
import { Mono } from "@/components/ui/mono";

type Pref = "preferred" | "available" | "unavailable";
type SubmitState = "idle" | "pulse" | "submitting" | "submitted";

interface FieldDef {
  id: string;
  label: string;
  value: string;
}

const FIELDS: FieldDef[] = [
  { id: "first", label: "Voornaam", value: "Lotte" },
  { id: "last", label: "Achternaam", value: "Janssens" },
  { id: "email", label: "E-mailadres", value: "lotte.j@example.be" },
];

interface SlotDef {
  id: string;
  day: string;
  time: string;
  court: string;
  /** The preference that gets tapped during the animation. */
  pick: Pref;
}

const SLOTS: SlotDef[] = [
  { id: "ma", day: "Ma", time: "18:00", court: "Baan 2", pick: "preferred" },
  { id: "wo", day: "Wo", time: "14:00", court: "Baan 1", pick: "available" },
  { id: "za", day: "Za", time: "10:00", court: "Baan 3", pick: "unavailable" },
];

interface Step {
  fieldsFilled: number; // 0..3
  typeSelected: boolean;
  slotsChosen: number; // 0..3
  submit: SubmitState;
}

const STEPS: Step[] = [
  { fieldsFilled: 0, typeSelected: false, slotsChosen: 0, submit: "idle" }, // 0: scaffold appears
  { fieldsFilled: 1, typeSelected: false, slotsChosen: 0, submit: "idle" }, // 1: Voornaam
  { fieldsFilled: 2, typeSelected: false, slotsChosen: 0, submit: "idle" }, // 2: Achternaam
  { fieldsFilled: 3, typeSelected: false, slotsChosen: 0, submit: "idle" }, // 3: Email
  { fieldsFilled: 3, typeSelected: true, slotsChosen: 0, submit: "idle" }, // 4: Solo
  { fieldsFilled: 3, typeSelected: true, slotsChosen: 1, submit: "idle" }, // 5: slot 1
  { fieldsFilled: 3, typeSelected: true, slotsChosen: 2, submit: "idle" }, // 6: slot 2
  { fieldsFilled: 3, typeSelected: true, slotsChosen: 3, submit: "idle" }, // 7: slot 3
  { fieldsFilled: 3, typeSelected: true, slotsChosen: 3, submit: "pulse" }, // 8: pulse
  { fieldsFilled: 3, typeSelected: true, slotsChosen: 3, submit: "submitting" }, // 9
  { fieldsFilled: 3, typeSelected: true, slotsChosen: 3, submit: "submitted" }, // 10: success
];

const STEP_DURATIONS_MS = [900, 500, 500, 600, 700, 500, 500, 600, 350, 800, 0];

/**
 * Per-step vertical scroll offset (in px). Negative values slide the form
 * upward inside the phone canvas to reveal content lower down. Tuned for
 * the 9:19 aspect ratio + ~604px content height — tweak any value if a
 * step doesn't end with the focal element comfortably in view.
 */
const SCROLL_OFFSETS_PX = [
  0, // 0: scaffold appears
  0, // 1: Voornaam typed
  -10, // 2: Achternaam (slight drift)
  -25, // 3: E-mail (cursor reaches third field)
  -55, // 4: Solo selected (Inschrijvingstype centered)
  -90, // 5: slot 1 (Beschikbaarheid in view)
  -100, // 6: slot 2
  -110, // 7: slot 3
  -135, // 8: submit pulses (button at bottom of viewport)
  -135, // 9: submitting
  -135, // 10: submitted (form fades; success card replaces it)
];

const containerVariants: Variants = {
  hidden: {},
  visible: { transition: { delayChildren: 0.05, staggerChildren: 0.07 } },
};

const fadeUp: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { type: "spring", stiffness: 220, damping: 26 },
  },
};

export function AnimatedEnrollmentMock() {
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

  const scrollOffset = prefersReducedMotion
    ? SCROLL_OFFSETS_PX[SCROLL_OFFSETS_PX.length - 1]
    : (SCROLL_OFFSETS_PX[stepIndex] ?? 0);

  return (
    <motion.div
      ref={ref}
      // 9:19 aspect ratio mirrors a real phone screen; overflow-hidden clips
      // the form so it appears to scroll inside the phone instead of
      // stretching the frame's height.
      className="relative aspect-[9/19] overflow-hidden bg-[#FAFAF8] text-ink"
      initial={prefersReducedMotion ? "visible" : "hidden"}
      animate={animate}
      variants={containerVariants}
    >
      <AnimatePresence mode="wait" initial={false}>
        {effectiveStep.submit === "submitted" ? (
          <SuccessCard key="success" />
        ) : (
          <FormBody
            key="form"
            step={effectiveStep}
            scrollOffset={scrollOffset}
          />
        )}
      </AnimatePresence>
    </motion.div>
  );
}

function FormBody({
  step,
  scrollOffset,
}: {
  step: Step;
  scrollOffset: number;
}) {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.25 }}
      className="absolute inset-x-0 top-0"
    >
      {/* Translation layer — slides upward as the animation progresses to
       * reveal content below, simulating a user scrolling the form. */}
      <motion.div
        animate={{ y: scrollOffset }}
        transition={{ type: "spring", stiffness: 80, damping: 22 }}
        className="space-y-4 px-3.5 pb-4 pt-7"
      >
        {/* Series header card */}
        <motion.div
          variants={fadeUp}
          className="rounded-lg border border-rule bg-paper p-3"
        >
          <div className="flex items-center gap-1.5">
            <span className="inline-flex items-center rounded-full bg-tennis-lime/40 px-2 py-0.5 text-[9px] font-bold tracking-wide text-tennis-green">
              JUNIOR
            </span>
            <Mono className="text-[9px] tracking-[0.12em] text-ink-3">
              TC COACHOS
            </Mono>
          </div>
          <h4 className="mt-1.5 text-[13px] font-bold leading-tight tracking-tight">
            Voorjaarslessen
          </h4>
          <p className="mt-0.5 text-[10px] leading-snug text-ink-3">
            12 lessen · spel & techniek 12-15j
          </p>
        </motion.div>

        {/* Persoonlijke gegevens */}
        <motion.div variants={fadeUp}>
          <Mono className="text-[9px] tracking-[0.14em] text-ink-3">
            PERSOONLIJKE GEGEVENS
          </Mono>
          <div className="mt-1.5 space-y-2">
            {FIELDS.map((field, i) => (
              <FieldInput
                key={field.id}
                label={field.label}
                value={field.value}
                filled={step.fieldsFilled > i}
                focused={step.fieldsFilled === i}
              />
            ))}
          </div>
        </motion.div>

        {/* Inschrijvingstype */}
        <motion.div variants={fadeUp}>
          <Mono className="text-[9px] tracking-[0.14em] text-ink-3">
            INSCHRIJVINGSTYPE
          </Mono>
          <div className="mt-1.5 grid grid-cols-2 gap-1.5">
            <TypeCard
              icon={User}
              title="Solo"
              sub="Mezelf"
              selected={step.typeSelected}
            />
            <TypeCard icon={Users} title="Groep" sub="Meer" selected={false} />
          </div>
        </motion.div>

        {/* Beschikbaarheid */}
        <motion.div variants={fadeUp}>
          <Mono className="text-[9px] tracking-[0.14em] text-ink-3">
            BESCHIKBAARHEID
          </Mono>
          <div className="mt-1.5 space-y-1.5">
            {SLOTS.map((slot, i) => (
              <SlotRow
                key={slot.id}
                slot={slot}
                chosen={step.slotsChosen > i ? slot.pick : null}
              />
            ))}
          </div>
        </motion.div>

        <motion.div variants={fadeUp}>
          <SubmitButton state={step.submit} />
        </motion.div>
      </motion.div>
    </motion.div>
  );
}

function FieldInput({
  label,
  value,
  filled,
  focused,
}: {
  label: string;
  value: string;
  filled: boolean;
  focused: boolean;
}) {
  return (
    <div>
      <label className="block text-[9px] font-medium text-ink-3">{label}</label>
      <motion.div
        animate={{
          borderColor: focused ? "#2D5016" : filled ? "#e7e4dc" : "#e7e4dc",
          boxShadow: focused
            ? "0 0 0 2px rgba(45,80,22,0.15)"
            : "0 0 0 0 rgba(45,80,22,0)",
        }}
        transition={{ duration: 0.2 }}
        className="mt-0.5 flex h-7 items-center rounded-md border bg-paper px-2 text-[11px]"
      >
        <AnimatePresence mode="wait" initial={false}>
          {filled ? (
            <motion.span
              key="value"
              initial={{ opacity: 0, y: -3 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.2 }}
              className="text-ink"
            >
              {value}
            </motion.span>
          ) : focused ? (
            <motion.span
              key="cursor"
              animate={{ opacity: [1, 0, 1] }}
              transition={{ duration: 0.9, repeat: Infinity, ease: "linear" }}
              className="inline-block h-3 w-[1.5px] bg-tennis-green"
            />
          ) : null}
        </AnimatePresence>
      </motion.div>
    </div>
  );
}

function TypeCard({
  icon: Icon,
  title,
  sub,
  selected,
}: {
  icon: typeof User;
  title: string;
  sub: string;
  selected: boolean;
}) {
  return (
    <motion.div
      animate={{
        borderColor: selected ? "#2D5016" : "#e7e4dc",
        backgroundColor: selected
          ? "rgba(45,80,22,0.07)"
          : "rgba(253,252,249,1)",
      }}
      transition={{ duration: 0.25 }}
      className="relative rounded-md border p-2"
    >
      <Icon
        className={
          selected ? "h-3.5 w-3.5 text-tennis-green" : "h-3.5 w-3.5 text-ink-3"
        }
      />
      <div className="mt-1 text-[11px] font-semibold leading-tight">
        {title}
      </div>
      <div className="text-[9px] text-ink-3">{sub}</div>
      <AnimatePresence>
        {selected ? (
          <motion.span
            key="check"
            initial={{ scale: 0, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            exit={{ scale: 0, opacity: 0 }}
            transition={{ type: "spring", stiffness: 480, damping: 22 }}
            className="absolute right-1.5 top-1.5 inline-flex h-3.5 w-3.5 items-center justify-center rounded-full bg-tennis-green"
          >
            <Check className="h-2.5 w-2.5 text-tennis-lime" strokeWidth={4} />
          </motion.span>
        ) : null}
      </AnimatePresence>
    </motion.div>
  );
}

function SlotRow({ slot, chosen }: { slot: SlotDef; chosen: Pref | null }) {
  return (
    <div className="flex items-center justify-between rounded-md border border-rule bg-paper px-2 py-1.5">
      <div className="min-w-0">
        <div className="text-[10px] font-semibold leading-none text-ink">
          {slot.day} {slot.time}
        </div>
        <div className="mt-0.5 text-[9px] leading-none text-ink-3">
          {slot.court}
        </div>
      </div>
      <div className="flex items-center gap-1">
        <PrefDot kind="preferred" active={chosen === "preferred"} />
        <PrefDot kind="available" active={chosen === "available"} />
        <PrefDot kind="unavailable" active={chosen === "unavailable"} />
      </div>
    </div>
  );
}

function PrefDot({ kind, active }: { kind: Pref; active: boolean }) {
  const activeBg =
    kind === "preferred"
      ? "#2D5016"
      : kind === "available"
        ? "#3B82F6"
        : "#8a867e";

  return (
    <motion.span
      animate={{
        backgroundColor: active ? activeBg : "rgba(253,252,249,1)",
        borderColor: active ? activeBg : "#e7e4dc",
      }}
      transition={{ duration: 0.25 }}
      className="inline-flex h-4 w-4 items-center justify-center rounded-full border"
    >
      <AnimatePresence>
        {active ? (
          <motion.span
            key="icon"
            initial={{ scale: 0, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            exit={{ scale: 0, opacity: 0 }}
            transition={{ type: "spring", stiffness: 480, damping: 22 }}
          >
            {kind === "unavailable" ? (
              <X className="h-2.5 w-2.5 text-paper" strokeWidth={4} />
            ) : (
              <Check className="h-2.5 w-2.5 text-paper" strokeWidth={4} />
            )}
          </motion.span>
        ) : null}
      </AnimatePresence>
    </motion.span>
  );
}

function SubmitButton({ state }: { state: SubmitState }) {
  const pulsing = state === "pulse";
  return (
    <motion.div
      animate={
        pulsing
          ? {
              boxShadow: [
                "0 0 0 0 rgba(45,80,22,0)",
                "0 0 0 5px rgba(45,80,22,0.2)",
                "0 0 0 0 rgba(45,80,22,0)",
              ],
            }
          : { boxShadow: "0 0 0 0 rgba(45,80,22,0)" }
      }
      transition={{ duration: 0.45 }}
      className="flex h-9 w-full items-center justify-center gap-1.5 rounded-md bg-tennis-green text-[12px] font-semibold text-paper"
    >
      <AnimatePresence mode="wait" initial={false}>
        {state === "submitting" ? (
          <motion.span
            key="submitting"
            initial={{ opacity: 0, y: -3 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 3 }}
            transition={{ duration: 0.18 }}
            className="inline-flex items-center gap-1.5"
          >
            <Loader2 className="h-3.5 w-3.5 animate-spin" />
            Bezig...
          </motion.span>
        ) : (
          <motion.span
            key="idle"
            initial={{ opacity: 0, y: -3 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 3 }}
            transition={{ duration: 0.18 }}
          >
            Inschrijven
          </motion.span>
        )}
      </AnimatePresence>
    </motion.div>
  );
}

function SuccessCard() {
  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.96 }}
      animate={{ opacity: 1, scale: 1 }}
      exit={{ opacity: 0 }}
      transition={{ type: "spring", stiffness: 220, damping: 24 }}
      className="absolute inset-0 flex flex-col items-center justify-center bg-[#FAFAF8] px-4 text-center"
    >
      <motion.div
        initial={{ scale: 0, rotate: -15 }}
        animate={{ scale: 1, rotate: 0 }}
        transition={{
          type: "spring",
          stiffness: 320,
          damping: 18,
          delay: 0.05,
        }}
        className="flex h-14 w-14 items-center justify-center rounded-full bg-tennis-green"
      >
        <CheckCircle2 className="h-8 w-8 text-tennis-lime" strokeWidth={2.2} />
      </motion.div>
      <h4 className="mt-4 text-base font-bold tracking-tight">Ingeschreven!</h4>
      <p className="mt-1.5 max-w-[180px] text-[11px] leading-snug text-ink-2">
        Je ontvangt een bevestiging per e-mail.
      </p>
      <Mono className="mt-4 text-[9px] tracking-[0.18em] text-ink-3">
        POWERED BY COACHOS
      </Mono>
    </motion.div>
  );
}
