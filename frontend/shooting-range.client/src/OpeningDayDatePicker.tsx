import { useEffect, useRef, useState } from "react";

interface OpeningDayDatePickerProps {
  value: string;
  openingDays: number[];
  disabled?: boolean;
  onChange: (value: string) => void;
}

interface CalendarMonth {
  year: number;
  month: number;
}

const monthFormatter = new Intl.DateTimeFormat("it-IT", {
  month: "long",
  year: "numeric",
  timeZone: "UTC",
});
const dateFormatter = new Intl.DateTimeFormat("it-IT", {
  dateStyle: "full",
  timeZone: "UTC",
});
const weekdayFormatter = new Intl.DateTimeFormat("it-IT", {
  weekday: "short",
  timeZone: "UTC",
});
const weekdays = Array.from({ length: 7 }, (_, index) =>
  weekdayFormatter.format(new Date(Date.UTC(2020, 5, index + 1))),
);

function getCalendarMonth(value: string): CalendarMonth {
  const [year, month] = value.split("-").map(Number);
  return { year, month };
}

function formatDate(year: number, month: number, day: number): string {
  return `${year}-${String(month).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
}

export function OpeningDayDatePicker({
  value,
  openingDays,
  disabled = false,
  onChange,
}: OpeningDayDatePickerProps) {
  const detailsRef = useRef<HTMLDetailsElement>(null);
  const [visibleMonth, setVisibleMonth] = useState(() => getCalendarMonth(value));
  const selectedDate = getCalendarMonth(value);
  const selectedDay = Number(value.slice(-2));
  const daysInMonth = new Date(
    Date.UTC(visibleMonth.year, visibleMonth.month, 0),
  ).getUTCDate();
  const firstDay = new Date(
    Date.UTC(visibleMonth.year, visibleMonth.month - 1, 1),
  ).getUTCDay();
  const mondayOffset = (firstDay + 6) % 7;
  const monthLabel = monthFormatter.format(
    new Date(Date.UTC(visibleMonth.year, visibleMonth.month - 1, 1)),
  );
  const selectedLabel = dateFormatter.format(
    new Date(Date.UTC(selectedDate.year, selectedDate.month - 1, selectedDay)),
  );
  const days = Array.from({ length: 42 }, (_, index) => {
    const day = index - mondayOffset + 1;
    return day > 0 && day <= daysInMonth ? day : null;
  });

  useEffect(() => {
    setVisibleMonth(getCalendarMonth(value));
  }, [value]);

  function moveMonth(amount: number) {
    const next = new Date(
      Date.UTC(visibleMonth.year, visibleMonth.month - 1 + amount, 1),
    );
    setVisibleMonth({
      year: next.getUTCFullYear(),
      month: next.getUTCMonth() + 1,
    });
  }

  return (
    <div className="range-date-picker">
      <span className="range-date-picker__label">Data</span>
      <details className="range-date-picker__details" ref={detailsRef}>
        <summary aria-label={`Data selezionata: ${selectedLabel}`}>
          {selectedLabel}
        </summary>
        <div className="range-calendar">
          <div className="range-calendar__month">
            <button
              type="button"
              className="button button-quiet"
              aria-label="Mese precedente"
              disabled={disabled}
              onClick={() => moveMonth(-1)}
            >
              Prec.
            </button>
            <strong>{monthLabel}</strong>
            <button
              type="button"
              className="button button-quiet"
              aria-label="Mese successivo"
              disabled={disabled}
              onClick={() => moveMonth(1)}
            >
              Succ.
            </button>
          </div>
          <div className="range-calendar__grid">
            {weekdays.map((weekday) => (
              <span className="range-calendar__weekday" key={weekday} aria-label={weekday}>
                {weekday}
              </span>
            ))}
            {days.map((day, index) => {
              if (day === null) {
                return <span className="range-calendar__empty" key={`empty-${index}`} />;
              }
              const date = formatDate(visibleMonth.year, visibleMonth.month, day);
              const weekday = new Date(
                Date.UTC(visibleMonth.year, visibleMonth.month - 1, day),
              ).getUTCDay();
              const label = dateFormatter.format(
                new Date(Date.UTC(visibleMonth.year, visibleMonth.month - 1, day)),
              );
              return (
                <button
                  type="button"
                  className="range-calendar__day"
                  key={date}
                  data-date={date}
                  aria-label={label}
                  aria-pressed={
                    visibleMonth.year === selectedDate.year &&
                    visibleMonth.month === selectedDate.month &&
                    day === selectedDay
                  }
                  disabled={disabled || !openingDays.includes(weekday)}
                  onClick={() => {
                    onChange(date);
                    if (detailsRef.current) detailsRef.current.open = false;
                  }}
                >
                  {day}
                </button>
              );
            })}
          </div>
        </div>
      </details>
    </div>
  );
}
