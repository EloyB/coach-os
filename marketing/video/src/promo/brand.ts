// Brand tokens voor de promo-video.
// Zelfde waarden als marketing/social-media-posts/animations/src/brand.ts,
// zodat geporte mocks (PlanningView e.d.) ongewijzigd werken.

export const COLORS = {
  bg: '#161513',
  lime: '#D0FF14',
  white: '#ffffff',
  tennisGreen: '#2D5016',
  tennisLime: '#D0FF14',
  paper: '#fdfcf9',
  canvas: '#f5f4f1',
  rule: '#e7e4dc',
  ink: '#161513',
  ink2: '#4a4741',
  ink3: '#8a867e',
};

export const FONTS = {
  sans: '"Inter", "SF Pro Display", -apple-system, BlinkMacSystemFont, system-ui, sans-serif',
  mono: '"JetBrains Mono", "SF Mono", Menlo, Consolas, monospace',
};

// Spring-config van de bestaande animaties: snappy met lichte overshoot.
export const SPRING_SNAPPY = { damping: 13, mass: 0.7, stiffness: 130 };
export const SPRING_SOFT = { damping: 16, mass: 0.7, stiffness: 110 };
export const SPRING_POP = { damping: 14, mass: 0.6, stiffness: 160 };
