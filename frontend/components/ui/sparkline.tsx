interface SparklineProps {
  data: number[];
  color?: string;
  width?: number;
  height?: number;
}

export function Sparkline({
  data,
  color = "#2D5016",
  width = 70,
  height = 32,
}: SparklineProps) {
  if (data.length < 2) return null;

  const min = Math.min(...data);
  const max = Math.max(...data);
  const range = max - min || 1;

  const points = data
    .map((v, i) => {
      const x = (i / (data.length - 1)) * width;
      const y = height - ((v - min) / range) * height;
      return `${x},${y}`;
    })
    .join(" ");

  const lastX = width;
  const lastY = height - ((data[data.length - 1] - min) / range) * height;

  return (
    <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`}>
      <polyline
        points={points}
        fill="none"
        stroke={color}
        strokeWidth="1.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx={lastX} cy={lastY} r="2.2" fill={color} />
    </svg>
  );
}
