export const PhoneFrame: React.FC<{
  width?: number;
  height?: number;
  children: React.ReactNode;
  style?: React.CSSProperties;
}> = ({ width = 320, height = 660, children, style }) => (
  <div
    style={{
      width,
      height,
      background: "#FFFFFF",
      borderRadius: 38,
      border: "10px solid #1A1A1A",
      boxShadow:
        "0 30px 80px rgba(0,0,0,0.45), 0 0 0 1px rgba(255,255,255,0.04) inset",
      overflow: "hidden",
      display: "flex",
      flexDirection: "column",
      position: "relative",
      ...style,
    }}
  >
    {/* Status bar */}
    <div
      style={{
        height: 30,
        background: "transparent",
        display: "flex",
        alignItems: "flex-end",
        justifyContent: "space-between",
        padding: "0 22px 4px",
        fontSize: 11,
        fontWeight: 600,
        color: "#0F1513",
        fontFamily:
          '"SF Pro Text", -apple-system, BlinkMacSystemFont, system-ui, sans-serif',
      }}
    >
      <span>9:41</span>
      <span style={{ display: "flex", gap: 4, alignItems: "center" }}>
        <span style={{ fontSize: 9 }}>●●●●</span>
        <span style={{ fontSize: 10 }}>5G</span>
      </span>
    </div>
    {children}
  </div>
);
