"use client";

import { useState } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { DayPicker } from "react-day-picker";
import { nl } from "date-fns/locale";
import { setMonth, setYear, addMonths, subMonths } from "date-fns";
import { NativeSelect } from "@/components/ui/native-select";
import { cn } from "@/lib/utils";

const MONTH_NAMES = [
  "Januari", "Februari", "Maart", "April", "Mei", "Juni",
  "Juli", "Augustus", "September", "Oktober", "November", "December",
];

function Calendar({
  className,
  showOutsideDays = true,
  defaultMonth,
  ...props
}: React.ComponentProps<typeof DayPicker>) {
  const [displayMonth, setDisplayMonth] = useState(defaultMonth ?? new Date());

  const currentYear = displayMonth.getFullYear();
  const currentMonthIdx = displayMonth.getMonth();

  // Year range: 2 years back, 5 years forward
  const startYear = new Date().getFullYear() - 2;
  const endYear = new Date().getFullYear() + 5;
  const years = Array.from({ length: endYear - startYear + 1 }, (_, i) => startYear + i);

  return (
    <div className={cn("p-3 select-none", className)}>
      {/* Custom header with dropdowns + nav */}
      <div className="flex items-center justify-between mb-3 px-1">
        <button
          type="button"
          onClick={() => setDisplayMonth(subMonths(displayMonth, 1))}
          className="size-8 inline-flex items-center justify-center rounded-md text-ink-3 hover:text-ink hover:bg-canvas transition-colors"
        >
          <ChevronLeft className="size-4" />
        </button>

        <div className="flex items-center gap-1">
          <NativeSelect
            variant="compact"
            value={currentMonthIdx}
            onChange={(e) => setDisplayMonth(setMonth(displayMonth, parseInt(e.target.value)))}
          >
            {MONTH_NAMES.map((name, i) => (
              <option key={i} value={i}>{name}</option>
            ))}
          </NativeSelect>
          <NativeSelect
            variant="compact"
            value={currentYear}
            onChange={(e) => setDisplayMonth(setYear(displayMonth, parseInt(e.target.value)))}
            className="tabular-nums"
          >
            {years.map((y) => (
              <option key={y} value={y}>{y}</option>
            ))}
          </NativeSelect>
        </div>

        <button
          type="button"
          onClick={() => setDisplayMonth(addMonths(displayMonth, 1))}
          className="size-8 inline-flex items-center justify-center rounded-md text-ink-3 hover:text-ink hover:bg-canvas transition-colors"
        >
          <ChevronRight className="size-4" />
        </button>
      </div>

      <DayPicker
        locale={nl}
        showOutsideDays={showOutsideDays}
        weekStartsOn={1}
        month={displayMonth}
        onMonthChange={setDisplayMonth}
        hideNavigation
        classNames={{
          months: "flex flex-col",
          month: "flex flex-col",
          month_caption: "hidden",
          month_grid: "w-full border-collapse",
          weekdays: "flex",
          weekday:
            "text-ink-3 rounded-md w-9 font-medium text-[0.65rem] uppercase tracking-wider",
          week: "flex w-full mt-1",
          day: "h-9 w-9 text-center text-sm p-0 relative flex items-center justify-center",
          day_button:
            "h-8 w-8 p-0 font-normal rounded-md transition-colors hover:bg-tennis-green/10 hover:text-tennis-green cursor-pointer inline-flex items-center justify-center",
          selected:
            "bg-tennis-green text-white hover:bg-tennis-green hover:text-white rounded-md font-semibold [&>button]:bg-tennis-green [&>button]:text-white [&>button]:hover:bg-tennis-green [&>button]:hover:text-white",
          today:
            "font-bold text-tennis-green [&>button]:font-bold",
          outside:
            "text-ink-3/40 [&>button]:text-ink-3/40 [&>button]:hover:bg-transparent",
          disabled: "text-ink-3/30 [&>button]:cursor-not-allowed [&>button]:hover:bg-transparent",
          hidden: "invisible",
        }}
        {...props}
      />
    </div>
  );
}

Calendar.displayName = "Calendar";

export { Calendar };
