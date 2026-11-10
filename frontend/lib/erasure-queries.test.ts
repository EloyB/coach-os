import { describe, expect, it } from "vitest";
import { isAboutErasedEnrollment } from "./erasure-queries";

const erased = "019e1c1f-b297-73b5-ab0b-07226dd5e569";

describe("isAboutErasedEnrollment", () => {
  it("sluit het verwijderoverzicht uit, ook van een andere inschrijving", () => {
    expect(isAboutErasedEnrollment(["playerErasurePreview", "enrollments", erased], erased)).toBe(true);
    expect(isAboutErasedEnrollment(["playerErasurePreview", "enrollments", "andere"], erased)).toBe(true);
  });

  it("sluit queries uit die de verwijderde inschrijving bevatten", () => {
    expect(isAboutErasedEnrollment(["enrollment", erased], erased)).toBe(true);
  });

  it("vernieuwt lijsten en andere inschrijvingen gewoon", () => {
    expect(isAboutErasedEnrollment(["lessonSerie", "019dfe27"], erased)).toBe(false);
    expect(isAboutErasedEnrollment(["enrollments", "019dfe27"], erased)).toBe(false);
    expect(isAboutErasedEnrollment([], erased)).toBe(false);
  });
});
