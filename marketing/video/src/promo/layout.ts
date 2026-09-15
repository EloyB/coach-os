// Alle formaat-afhankelijke maten op één plek.
// 'wide'  = 1920×1080 (website, LinkedIn)
// 'tall'  = 1080×1920 (Instagram Reels)

export type PromoFormat = 'wide' | 'tall';

export interface PromoLayout {
  width: number;
  height: number;
  /** buitenmarge van het podium */
  pad: number;
  /** richting van headline + demo: rij (16:9) of kolom (9:16) */
  direction: 'row' | 'column';
  headline: {
    fontSize: number;
    lineHeight: number;
    maxWidth: number;
  };
  tagFontSize: number;
  /** schaal van de zwevende demo-kaart */
  cardScale: number;
  /** hook/outro typografie */
  heroFontSize: number;
}

export const LAYOUTS: Record<PromoFormat, PromoLayout> = {
  wide: {
    width: 1920,
    height: 1080,
    pad: 110,
    direction: 'row',
    headline: { fontSize: 72, lineHeight: 1.04, maxWidth: 620 },
    tagFontSize: 22,
    cardScale: 1,
    heroFontSize: 130,
  },
  tall: {
    width: 1080,
    height: 1920,
    pad: 90,
    direction: 'column',
    headline: { fontSize: 64, lineHeight: 1.06, maxWidth: 900 },
    tagFontSize: 22,
    cardScale: 0.92,
    heroFontSize: 104,
  },
};

export const FPS = 30;

// Scènelengtes in frames (~40s totaal).
export const PROMO_DURATIONS = {
  consolidation: 150,
  series: 180,
  camp: 150,
  enroll: 180,
  planner: 240,
  payments: 180,
  outro: 150,
} as const;

export const PROMO_TOTAL = Object.values(PROMO_DURATIONS).reduce((a, b) => a + b, 0); // 1200
