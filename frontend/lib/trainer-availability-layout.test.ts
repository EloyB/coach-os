import { describe, expect, it } from "vitest";
import type { TrainerAvailabilityDto } from "@/lib/api/trainerAvailabilities";
import {
  blockPosition,
  clubColorIndex,
  computeAxisRange,
  groupByDay,
  minutesFromOffset,
  overlaps,
  snapToQuarter,
  toMinutes,
  toTime,
} from "./trainer-availability-layout";

const RANGE = { startHour: 7, endHour: 23 };

function availability(
  overrides: Partial<TrainerAvailabilityDto>,
): TrainerAvailabilityDto {
  return {
    id: "a",
    trainerId: "t",
    tennisClubId: null,
    tennisClubName: null,
    dayOfWeek: 0,
    startTime: "09:00",
    endTime: "10:00",
    ...overrides,
  };
}

describe("tijdconversie", () => {
  it("zet HH:mm om naar minuten en terug", () => {
    expect(toMinutes("17:30")).toBe(1050);
    expect(toTime(1050)).toBe("17:30");
    expect(toTime(9 * 60 + 5)).toBe("09:05");
  });

  it("snapt op kwartieren", () => {
    expect(snapToQuarter(7)).toBe(0);
    expect(snapToQuarter(8)).toBe(15);
    expect(snapToQuarter(52)).toBe(45);
  });
});

describe("computeAxisRange", () => {
  it("geeft 7 tot 23 zonder tijdvakken", () => {
    expect(computeAxisRange([])).toEqual(RANGE);
  });

  it("wordt breder voor een tijdvak voor 07:00", () => {
    expect(
      computeAxisRange([{ startTime: "06:30", endTime: "08:00" }]).startHour,
    ).toBe(6);
  });

  it("wordt breder voor een tijdvak na 23:00", () => {
    expect(
      computeAxisRange([{ startTime: "20:00", endTime: "23:45" }]).endHour,
    ).toBe(24);
  });

  it("blijft 7 tot 23 voor tijdvakken binnen het standaardbereik", () => {
    expect(
      computeAxisRange([{ startTime: "09:00", endTime: "12:00" }]),
    ).toEqual(RANGE);
  });
});

describe("blockPosition", () => {
  it("berekent top en hoogte in procenten van de as", () => {
    expect(blockPosition("09:00", "12:00", RANGE)).toEqual({
      topPercent: 12.5,
      heightPercent: 18.75,
    });
  });
});

describe("minutesFromOffset", () => {
  it("zet een hoogtepositie om naar gesnapte minuten", () => {
    // bovenaan de as = 07:00; halverwege = 15:00
    expect(minutesFromOffset(0, 800, RANGE)).toBe(7 * 60);
    expect(minutesFromOffset(400, 800, RANGE)).toBe(15 * 60);
  });

  it("begrenst binnen de as", () => {
    expect(minutesFromOffset(-50, 800, RANGE)).toBe(7 * 60);
    expect(minutesFromOffset(5000, 800, RANGE)).toBe(23 * 60);
  });
});

describe("overlaps", () => {
  it("raken is geen overlap", () => {
    expect(
      overlaps(
        { startTime: "09:00", endTime: "10:00" },
        { startTime: "10:00", endTime: "11:00" },
      ),
    ).toBe(false);
  });

  it("deels overlappen is overlap", () => {
    expect(
      overlaps(
        { startTime: "09:00", endTime: "10:00" },
        { startTime: "09:30", endTime: "10:30" },
      ),
    ).toBe(true);
  });
});

describe("groupByDay", () => {
  it("bevat alle zeven dagen en sorteert op starttijd", () => {
    const grouped = groupByDay([
      availability({ id: "late", dayOfWeek: 2, startTime: "18:00", endTime: "20:00" }),
      availability({ id: "vroeg", dayOfWeek: 2, startTime: "09:00", endTime: "11:00" }),
    ]);

    expect(Object.keys(grouped)).toHaveLength(7);
    expect(grouped[2].map((a) => a.id)).toEqual(["vroeg", "late"]);
    expect(grouped[0]).toEqual([]);
  });
});

describe("clubColorIndex", () => {
  it("geeft elke club index 0 en clubs een eigen volgorde", () => {
    const clubs = ["c1", "c2"];
    expect(clubColorIndex(null, clubs)).toBe(0);
    expect(clubColorIndex("c1", clubs)).toBe(1);
    expect(clubColorIndex("c2", clubs)).toBe(2);
  });
});
