"use client";

import { useEffect, useRef, useState } from "react";
import {
  AnimatePresence,
  motion,
  useInView,
  useReducedMotion,
} from "motion/react";
import type { Variants } from "motion/react";
import {
  Check,
  ChevronDown,
  ClipboardList,
  ListChecks,
  Loader2,
  Plus,
  ToggleLeft,
  Type as TypeIcon,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Mono } from "@/components/ui/mono";

type FieldType = "text" | "yesno" | "multi";

interface CustomField {
  id: string;
  label: string;
  type: FieldType;
  required: boolean;
  options?: string[];
}

const FIXED_FIELDS = [
  "Voornaam",
  "Achternaam",
  "E-mailadres",
  "Inschrijvingstype",
  "Beschikbaarheid",
];

const CUSTOM_FIELDS: CustomField[] = [
  {
    id: "phone",
    label: "Telefoonnummer",
    type: "text",
    required: true,
  },
  {
    id: "racket",
    label: "Heb je een eigen racket?",
    type: "yesno",
    required: false,
  },
  {
    id: "times",
    label: "Voorkeurstijden",
    type: "multi",
    required: true,
    options: ["Maandag 18u", "Woensdag 14u", "Zaterdag 10u"],
  },
];

type SaveState = "idle" | "saving" | "saved";

interface Step {
  visibleCount: number;
  save: SaveState;
  /** Briefly highlight the "Veld toevoegen" button before the next field arrives. */
  highlightAdd?: boolean;
}

const STEPS: Step[] = [
  { visibleCount: 0, save: "idle" },
  { visibleCount: 0, save: "idle", highlightAdd: true },
  { visibleCount: 1, save: "idle" },
  { visibleCount: 1, save: "idle", highlightAdd: true },
  { visibleCount: 2, save: "idle" },
  { visibleCount: 2, save: "idle", highlightAdd: true },
  { visibleCount: 3, save: "idle" },
  { visibleCount: 3, save: "saving" },
  { visibleCount: 3, save: "saved" },
];

const STEP_DURATIONS_MS = [1000, 350, 1100, 350, 1300, 350, 1300, 1000, 2200];

const containerVariants: Variants = {
  hidden: {},
  visible: {
    transition: { delayChildren: 0.05, staggerChildren: 0.06 },
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

const pillsContainer: Variants = {
  hidden: {},
  visible: { transition: { staggerChildren: 0.05 } },
};

const pillVariant: Variants = {
  hidden: { opacity: 0, scale: 0.85 },
  visible: {
    opacity: 1,
    scale: 1,
    transition: { type: "spring", stiffness: 320, damping: 24 },
  },
};

const TYPE_META: Record<
  FieldType,
  { label: string; Icon: LucideIcon }
> = {
  text: { label: "Vrije tekst", Icon: TypeIcon },
  multi: { label: "Meerkeuze", Icon: ListChecks },
  yesno: { label: "Ja/Nee", Icon: ToggleLeft },
};

export function AnimatedFormBuilderMock() {
  const ref = useRef<HTMLDivElement>(null);
  // `once: true` latches inView to true the first time the section is
  // scrolled into view — it never flips back, so the sequence keeps
  // running to the end even if the user scrolls past mid-animation.
  const inView = useInView(ref, { amount: 0.4, once: true });
  const prefersReducedMotion = useReducedMotion();
  const [stepIndex, setStepIndex] = useState(0);

  const isComplete = stepIndex >= STEPS.length - 1;

  // When motion is disabled, jump straight to the final populated state
  // so users see the full "story" without any motion.
  const effectiveStep: Step = prefersReducedMotion
    ? { visibleCount: CUSTOM_FIELDS.length, save: "saved" }
    : STEPS[stepIndex];

  useEffect(() => {
    // Stop scheduling once the final step is reached — the mock then
    // sits as a static end-state ("Opgeslagen ✓" with all 3 fields).
    if (prefersReducedMotion || !inView || isComplete) return;
    const t = window.setTimeout(() => {
      setStepIndex((s) => s + 1);
    }, STEP_DURATIONS_MS[stepIndex]);
    return () => window.clearTimeout(t);
  }, [stepIndex, inView, isComplete, prefersReducedMotion]);

  const visibleFields = CUSTOM_FIELDS.slice(0, effectiveStep.visibleCount);
  const animate = prefersReducedMotion || inView ? "visible" : "hidden";

  return (
    <motion.div
      ref={ref}
      className="relative bg-paper p-5"
      initial={prefersReducedMotion ? "visible" : "hidden"}
      animate={animate}
      variants={containerVariants}
    >
      {/* Header */}
      <motion.div variants={fadeUp} className="flex items-center gap-2.5">
        <div className="flex h-6 w-6 items-center justify-center rounded-md bg-tennis-green/10">
          <ClipboardList className="h-3.5 w-3.5 text-tennis-green" />
        </div>
        <h3 className="text-sm font-bold tracking-tight">Inschrijfformulier</h3>
      </motion.div>

      {/* Vaste velden */}
      <motion.div variants={fadeUp} className="mt-4">
        <Mono className="text-[10px] tracking-[0.12em] text-ink-3">
          VASTE VELDEN (ALTIJD ZICHTBAAR)
        </Mono>
        <motion.div
          variants={pillsContainer}
          className="mt-2 flex flex-wrap gap-1.5"
        >
          {FIXED_FIELDS.map((label) => (
            <motion.span
              key={label}
              variants={pillVariant}
              className="inline-flex items-center rounded-full bg-tennis-green/10 px-2.5 py-1 text-[11px] font-medium text-tennis-green"
            >
              {label}
            </motion.span>
          ))}
        </motion.div>
      </motion.div>

      {/* Custom fields */}
      <motion.div variants={fadeUp} className="mt-5">
        <Mono className="text-[10px] tracking-[0.12em] text-ink-3">
          AANGEPASTE VELDEN
        </Mono>

        {/* Reserve space for the max state (3 cards, last one with 3 options +
         * required) so the surrounding row doesn't reflow as fields appear. */}
        <div className="mt-2 min-h-[360px] space-y-2">
          <AnimatePresence mode="popLayout" initial={false}>
            {visibleFields.length === 0 ? (
              <motion.div
                key="empty"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                transition={{ duration: 0.2 }}
                className="rounded-lg border border-dashed border-rule bg-canvas/40 px-3 py-4 text-center text-[11px] text-ink-3"
              >
                Nog geen aangepaste velden. Klik &quot;Veld toevoegen&quot; om
                te beginnen.
              </motion.div>
            ) : (
              visibleFields.map((field) => (
                <FieldCard key={field.id} field={field} />
              ))
            )}
          </AnimatePresence>
        </div>
      </motion.div>

      {/* Action row */}
      <motion.div
        variants={fadeUp}
        className="mt-5 flex items-center justify-between gap-2"
      >
        <AddFieldButton highlight={effectiveStep.highlightAdd ?? false} />
        <SaveButton state={effectiveStep.save} />
      </motion.div>
    </motion.div>
  );
}

function FieldCard({ field }: { field: CustomField }) {
  const { label: typeLabel, Icon: TypeIconCmp } = TYPE_META[field.type];

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 10, scale: 0.97 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      exit={{ opacity: 0, scale: 0.97 }}
      transition={{ type: "spring", stiffness: 280, damping: 28 }}
      className="rounded-lg border border-rule bg-canvas/50 p-3"
    >
      {/* Label "input" */}
      <div className="rounded-md border border-rule bg-paper px-2.5 py-1.5 text-[12px] text-ink">
        {field.label}
      </div>

      {/* Row: type + required */}
      <div className="mt-2 flex items-center gap-3">
        <div className="inline-flex items-center gap-1.5 rounded-md border border-rule bg-paper px-2 py-1 text-[11px] text-ink-2">
          <TypeIconCmp className="h-3 w-3 text-ink-3" />
          {typeLabel}
          <ChevronDown className="h-3 w-3 text-ink-3" />
        </div>

        <label className="inline-flex items-center gap-1.5 text-[11px] text-ink-2">
          <motion.span
            animate={{
              backgroundColor: field.required ? "#2D5016" : "#fdfcf9",
              borderColor: field.required ? "#2D5016" : "#e7e4dc",
            }}
            transition={{ duration: 0.25 }}
            className="flex h-3.5 w-3.5 items-center justify-center rounded-sm border"
          >
            <AnimatePresence>
              {field.required ? (
                <motion.span
                  key="check"
                  initial={{ scale: 0, opacity: 0 }}
                  animate={{ scale: 1, opacity: 1 }}
                  exit={{ scale: 0, opacity: 0 }}
                  transition={{ type: "spring", stiffness: 500, damping: 22 }}
                >
                  <Check className="h-2.5 w-2.5 text-tennis-lime" strokeWidth={4} />
                </motion.span>
              ) : null}
            </AnimatePresence>
          </motion.span>
          Verplicht
        </label>
      </div>

      {/* Options for multi-choice */}
      <AnimatePresence>
        {field.type === "multi" && field.options ? (
          <motion.div
            key="options"
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: "auto" }}
            exit={{ opacity: 0, height: 0 }}
            transition={{ duration: 0.25 }}
            className="overflow-hidden"
          >
            <div className="mt-2 space-y-1 border-t border-rule pt-2">
              {field.options.map((opt, i) => (
                <motion.div
                  key={opt}
                  initial={{ opacity: 0, x: -6 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.1 + i * 0.08, duration: 0.25 }}
                  className="flex items-center gap-2"
                >
                  <span className="h-2.5 w-2.5 rounded-full border border-ink-3/50" />
                  <span className="text-[11px] text-ink-2">{opt}</span>
                </motion.div>
              ))}
            </div>
          </motion.div>
        ) : null}
      </AnimatePresence>
    </motion.div>
  );
}

function AddFieldButton({ highlight }: { highlight: boolean }) {
  return (
    <motion.div
      animate={
        highlight
          ? { scale: [1, 1.04, 1], boxShadow: ["0 0 0 0 rgba(45,80,22,0)", "0 0 0 4px rgba(45,80,22,0.18)", "0 0 0 0 rgba(45,80,22,0)"] }
          : { scale: 1, boxShadow: "0 0 0 0 rgba(45,80,22,0)" }
      }
      transition={{ duration: 0.35 }}
      className="inline-flex items-center gap-1.5 rounded-md border border-rule bg-paper px-2.5 py-1.5 text-[11px] font-medium text-ink-2"
    >
      <Plus className="h-3 w-3" />
      Veld toevoegen
    </motion.div>
  );
}

function SaveButton({ state }: { state: SaveState }) {
  return (
    <motion.div
      layout
      transition={{ type: "spring", stiffness: 300, damping: 28 }}
      className={`inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-[11px] font-semibold ${
        state === "saved"
          ? "bg-tennis-green text-tennis-lime"
          : state === "saving"
          ? "bg-tennis-green/70 text-paper"
          : "bg-tennis-green text-paper"
      }`}
    >
      <AnimatePresence mode="wait" initial={false}>
        {state === "saved" ? (
          <motion.span
            key="saved"
            initial={{ opacity: 0, y: -4 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 4 }}
            transition={{ duration: 0.2 }}
            className="inline-flex items-center gap-1.5"
          >
            <Check className="h-3 w-3" strokeWidth={3} />
            Opgeslagen
          </motion.span>
        ) : state === "saving" ? (
          <motion.span
            key="saving"
            initial={{ opacity: 0, y: -4 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 4 }}
            transition={{ duration: 0.2 }}
            className="inline-flex items-center gap-1.5"
          >
            <Loader2 className="h-3 w-3 animate-spin" />
            Opslaan...
          </motion.span>
        ) : (
          <motion.span
            key="idle"
            initial={{ opacity: 0, y: -4 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 4 }}
            transition={{ duration: 0.2 }}
          >
            Formulier opslaan
          </motion.span>
        )}
      </AnimatePresence>
    </motion.div>
  );
}
