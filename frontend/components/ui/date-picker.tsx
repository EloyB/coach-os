"use client";

import { useState } from "react";
import { format, parse } from "date-fns";
import { nl } from "date-fns/locale";
import { CalendarDays } from "lucide-react";
import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";

interface DatePickerProps {
  /** Value as yyyy-MM-dd string */
  value?: string;
  /** Callback with yyyy-MM-dd string */
  onChange: (value: string) => void;
  /** Placeholder when no date selected */
  placeholder?: string;
  /** Additional className for the trigger button */
  className?: string;
  /** Disable the picker */
  disabled?: boolean;
  /** Earliest selectable date (yyyy-MM-dd). Dates before this are disabled. */
  minDate?: string;
}

export function DatePicker({
  value,
  onChange,
  placeholder = "Kies een datum",
  className,
  disabled = false,
  minDate,
}: DatePickerProps) {
  const [open, setOpen] = useState(false);

  const selectedDate = value
    ? parse(value, "yyyy-MM-dd", new Date())
    : undefined;

  const minParsed = minDate
    ? parse(minDate, "yyyy-MM-dd", new Date())
    : undefined;

  const displayText = selectedDate
    ? format(selectedDate, "d MMM yyyy", { locale: nl })
    : null;

  function handleSelect(date: Date | undefined) {
    if (date) {
      const formatted = format(date, "yyyy-MM-dd");
      onChange(formatted);
      setOpen(false);
    }
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          disabled={disabled}
          className={cn(
            "w-full flex items-center justify-between h-10 px-3 py-2 text-left text-[12.5px] rounded-lg border transition-colors",
            "border-gray-200 bg-white hover:border-tennis-green/50 focus:outline-none focus:ring-1 focus:ring-tennis-green focus:border-tennis-green",
            "disabled:opacity-50 disabled:cursor-not-allowed",
            displayText ? "text-ink" : "text-[#c5c0b6]",
            className,
          )}
        >
          <span className={displayText ? "font-medium" : ""}>
            {displayText ?? placeholder}
          </span>
          <CalendarDays className="size-4 text-ink-3 shrink-0" />
        </button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0 border-rule shadow-lg" align="start">
        <Calendar
          mode="single"
          selected={selectedDate}
          onSelect={handleSelect}
          defaultMonth={selectedDate ?? minParsed}
          disabled={minParsed ? { before: minParsed } : undefined}
          startMonth={minParsed}
        />
      </PopoverContent>
    </Popover>
  );
}
