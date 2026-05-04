"use client";

import { useState, KeyboardEvent } from "react";
import { X } from "lucide-react";

interface EmailTagInputProps {
  value: string[];
  onChange: (next: string[]) => void;
  placeholder?: string;
  disabled?: boolean;
  hasError?: boolean;
  /** Tail-call to validate before adding. Returns true to accept. */
  validate?: (email: string) => boolean;
}

const DEFAULT_VALIDATE = (email: string): boolean =>
  /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);

export function EmailTagInput({
  value,
  onChange,
  placeholder,
  disabled,
  hasError,
  validate = DEFAULT_VALIDATE,
}: EmailTagInputProps) {
  const [draft, setDraft] = useState("");
  const [localError, setLocalError] = useState<string | null>(null);

  function tryAdd() {
    const trimmed = draft.trim().toLowerCase();
    if (!trimmed) return;
    if (!validate(trimmed)) {
      setLocalError("Ongeldig e-mailadres");
      return;
    }
    if (value.includes(trimmed)) {
      setDraft("");
      setLocalError(null);
      return;
    }
    onChange([...value, trimmed]);
    setDraft("");
    setLocalError(null);
  }

  function handleKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Enter" || e.key === ",") {
      e.preventDefault();
      tryAdd();
    } else if (e.key === "Backspace" && !draft && value.length > 0) {
      onChange(value.slice(0, -1));
    }
  }

  function removeAt(i: number) {
    onChange(value.filter((_, idx) => idx !== i));
  }

  return (
    <div>
      <div
        className={`flex flex-wrap items-center gap-1.5 min-h-10 px-2 py-1.5 rounded-lg bg-white border ${
          hasError ? "border-red-300" : "border-rule"
        } focus-within:ring-2 focus-within:ring-tennis-green/30 focus-within:border-tennis-green transition-colors`}
      >
        {value.map((email, i) => (
          <span
            key={`${email}-${i}`}
            className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-tennis-green/[.08] text-tennis-green text-[11.5px] font-semibold"
          >
            {email}
            <button
              type="button"
              onClick={() => removeAt(i)}
              disabled={disabled}
              className="hover:bg-tennis-green/[.15] rounded p-0.5"
              aria-label={`Verwijder ${email}`}
            >
              <X size={11} />
            </button>
          </span>
        ))}
        <input
          type="email"
          value={draft}
          disabled={disabled}
          onChange={(e) => {
            setDraft(e.target.value);
            if (localError) setLocalError(null);
          }}
          onKeyDown={handleKeyDown}
          onBlur={() => tryAdd()}
          placeholder={value.length === 0 ? placeholder : ""}
          className="flex-1 min-w-32 bg-transparent outline-none text-[12.5px] placeholder:text-[#c5c0b6] py-1"
        />
      </div>
      {localError && (
        <p className="mt-1 text-[11px] text-red-600">{localError}</p>
      )}
    </div>
  );
}
