// macOS-style cursor pointer. The `x`/`y` props position the arrow TIP
// at that coordinate — internal offsets account for the tip not being at
// the SVG's top-left corner.

interface CursorProps {
  x: number;
  y: number;
  pressed?: boolean;
  size?: number;
}

export const Cursor: React.FC<CursorProps> = ({ x, y, pressed = false, size = 30 }) => {
  const tipOffsetX = (2 / 24) * size;
  const tipOffsetY = (1 / 24) * size;
  const scale = pressed ? 0.88 : 1;
  return (
    <div
      style={{
        position: 'absolute',
        left: x - tipOffsetX,
        top: y - tipOffsetY,
        width: size,
        height: size,
        transform: `scale(${scale})`,
        transformOrigin: `${tipOffsetX}px ${tipOffsetY}px`,
        pointerEvents: 'none',
        filter: 'drop-shadow(0 2px 5px rgba(0,0,0,0.5))',
        zIndex: 200,
      }}
    >
      <svg width={size} height={size} viewBox="0 0 24 24" fill="none">
        <path
          d="M 2 1 L 2 20 L 7 15 L 10 22 L 13 21 L 10 14 L 16 14 Z"
          fill="#1a1a1a"
          stroke="#ffffff"
          strokeWidth="1.6"
          strokeLinejoin="round"
          strokeLinecap="round"
        />
      </svg>
    </div>
  );
};
