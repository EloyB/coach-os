// Annotation overlay — numbered pins with sidebar notes.
// Wraps an inner screen and overlays positioned pins + a right-side legend.

function Pin({ n, top, left, right, bottom, tone = "red" }) {
  const colors = {
    red: { bg: "#DC2626", tx: "#fff" },
    amber: { bg: "#F59E0B", tx: "#fff" },
    blue: { bg: "#2563EB", tx: "#fff" },
  }[tone];
  return (
    <div style={{
      position: "absolute", top, left, right, bottom,
      width: 22, height: 22, borderRadius: 999,
      background: colors.bg, color: colors.tx,
      display: "grid", placeItems: "center",
      fontSize: 11, fontWeight: 700,
      boxShadow: "0 2px 8px rgba(0,0,0,.2), 0 0 0 3px rgba(255,255,255,.9)",
      zIndex: 20,
      fontFamily: "'Inter', system-ui, sans-serif",
    }}>{n}</div>
  );
}

function AnnotationFrame({ pins, notes, children }) {
  return (
    <div style={{ display: "flex", width: "100%", height: "100%", background: "#f7f6f2" }}>
      <div style={{ flex: 1, position: "relative", overflow: "hidden" }}>
        {children}
        {pins.map((p, i) => <Pin key={i} {...p} />)}
      </div>
      <aside style={{
        width: 340, background: "#1a1613", color: "#e9e4db",
        padding: "28px 24px", overflow: "auto", flexShrink: 0,
        fontFamily: "'Inter', system-ui, sans-serif",
      }}>
        <p style={{ margin: 0, fontSize: 10.5, letterSpacing: "0.15em", textTransform: "uppercase", color: "#8f877a", fontWeight: 600 }}>
          Critique
        </p>
        <h2 style={{ margin: "4px 0 22px", fontSize: 18, fontWeight: 700, color: "#fff", letterSpacing: -0.3 }}>
          {notes.title}
        </h2>
        <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
          {notes.items.map((it, i) => {
            const toneColor = { red: "#F87171", amber: "#FBBF24", blue: "#60A5FA" }[it.tone || "red"];
            return (
              <div key={i} style={{ display: "flex", gap: 12 }}>
                <div style={{
                  width: 22, height: 22, borderRadius: 999, flexShrink: 0,
                  background: toneColor, color: "#1a1613",
                  display: "grid", placeItems: "center",
                  fontSize: 11, fontWeight: 700,
                }}>{i + 1}</div>
                <div style={{ flex: 1 }}>
                  <p style={{ margin: 0, fontSize: 13, fontWeight: 600, color: "#fff", lineHeight: 1.3 }}>
                    {it.title}
                  </p>
                  <p style={{ margin: "4px 0 0", fontSize: 12, color: "#b8b0a0", lineHeight: 1.55 }}>
                    {it.body}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
        {notes.takeaway && (
          <div style={{ marginTop: 24, padding: "14px 14px", borderRadius: 10, background: "rgba(208,255,20,.08)", borderLeft: `3px solid ${LIME}` }}>
            <p style={{ margin: 0, fontSize: 10.5, fontWeight: 700, color: LIME, textTransform: "uppercase", letterSpacing: "0.1em" }}>
              Takeaway
            </p>
            <p style={{ margin: "6px 0 0", fontSize: 12.5, color: "#e9e4db", lineHeight: 1.55 }}>
              {notes.takeaway}
            </p>
          </div>
        )}
      </aside>
    </div>
  );
}

Object.assign(window, { AnnotationFrame, Pin });
