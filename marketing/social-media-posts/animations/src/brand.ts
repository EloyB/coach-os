// CoachOS brand tokens — single source of truth for colors, fonts, and
// shared styling values used across every composition.

export const COLORS = {
  bg: '#161513',
  lime: '#D0FF14',
  white: '#ffffff',
  whiteFaded: 'rgba(255,255,255,0.7)',
  whiteSubtle: 'rgba(255,255,255,0.05)',
  divider: 'rgba(255,255,255,0.1)',
  // Player level color coding (used inside grid cells, etc.)
  level1: '#D0FF14', // lime — beginner
  level2: '#7AB8FF', // blue — intermediate
  level3: '#FF8B7A', // coral — advanced
  // App-side tokens (mirror frontend/app/globals.css)
  tennisGreen: '#2D5016',
  tennisLime: '#D0FF14',
  paper: '#fdfcf9',     // card background
  canvas: '#f5f4f1',    // page background
  rule: '#e7e4dc',      // borders / dividers
  ink: '#161513',       // primary text
  ink2: '#4a4741',      // secondary text
  ink3: '#8a867e',      // tertiary text / placeholders
  red400: '#f87171',    // required asterisk
} as const;

export const FONTS = {
  sans: '"Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
  mono: '"JetBrains Mono", ui-monospace, "SF Mono", Menlo, monospace',
} as const;

export const CANVAS = {
  width: 1080,
  height: 1080,
  fps: 30,
} as const;
