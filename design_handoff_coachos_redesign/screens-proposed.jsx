// Proposed improved versions. Keep brand colors but upgrade information
// density, hierarchy, and personality.

const P_BG = "#F5F4F1";
const INK = "#161513";
const INK_2 = "#4a4741";
const INK_3 = "#8a867e";
const RULE = "#e7e4dc";
const SURFACE = "#fdfcf9";
const ACCENT_LIME = "#D0FF14";
const ACCENT_GREEN = "#2D5016";

// Refined sidebar: keeps tennis-green but trades opaque block for
// more hierarchy, court-mark count indicators and a profile at bottom.
const SidebarP = ({ active = "Planning" }) => {
  const items = [
    { label: "Vandaag", icon: I.dash, badge: 4 },
    { label: "Planning", icon: I.cal, badge: null },
    { label: "Lesreeksen", icon: I.book, badge: 12 },
    { label: "Leerlingen", icon: I.users, badge: 214 },
    { label: "Trainers", icon: I.cap, badge: 6 },
    { label: "Instellingen", icon: I.gear, badge: null },
  ];
  return (
    <aside style={{ width: 224, background: ACCENT_GREEN, color: "#fff", display: "flex", flexDirection: "column", flexShrink: 0, position: "relative", overflow: "hidden" }}>
      <CourtLines opacity={0.05} />
      <div style={{ position: "relative", padding: "20px 18px 18px" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div style={{ width: 28, height: 28, borderRadius: 6, background: ACCENT_LIME, display: "grid", placeItems: "center" }}>
            <span style={{ color: ACCENT_GREEN, fontWeight: 800, fontSize: 13, fontFamily: "'JetBrains Mono', monospace" }}>c/</span>
          </div>
          <span style={{ fontWeight: 700, fontSize: 15.5, letterSpacing: -0.3 }}>CoachOS</span>
        </div>
        {/* Club switcher */}
        <div style={{ marginTop: 16, padding: "8px 10px", background: "rgba(0,0,0,.18)", borderRadius: 6, display: "flex", alignItems: "center", gap: 8 }}>
          <div style={{ width: 20, height: 20, borderRadius: 4, background: "rgba(208,255,20,.2)", display: "grid", placeItems: "center", fontSize: 10, color: ACCENT_LIME, fontWeight: 700 }}>TL</div>
          <div style={{ flex: 1, minWidth: 0 }}>
            <p style={{ margin: 0, fontSize: 10, color: "rgba(255,255,255,.5)", textTransform: "uppercase", letterSpacing: "0.08em" }}>Club</p>
            <p style={{ margin: 0, fontSize: 11.5, fontWeight: 600, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>TC Leuven</p>
          </div>
          <I.chev style={{ width: 12, height: 12, opacity: 0.5, transform: "rotate(90deg)" }} />
        </div>
      </div>
      <nav style={{ position: "relative", flex: 1, padding: "4px 10px", display: "flex", flexDirection: "column", gap: 1 }}>
        <p style={{ margin: "10px 12px 6px", fontSize: 9.5, color: "rgba(255,255,255,.35)", fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.12em" }}>
          Werk
        </p>
        {items.map(({ label, icon: Icon, badge }) => {
          const on = label === active;
          return (
            <div key={label} style={{
              display: "flex", alignItems: "center", gap: 11, padding: "8px 12px",
              borderRadius: 6, fontSize: 12.5, fontWeight: 500,
              color: on ? "#fff" : "rgba(255,255,255,.7)",
              background: on ? "rgba(208,255,20,.12)" : "transparent",
              boxShadow: on ? `inset 2px 0 0 ${ACCENT_LIME}` : "none",
            }}>
              <Icon style={{ width: 14.5, height: 14.5, color: on ? ACCENT_LIME : "rgba(255,255,255,.5)" }} />
              <span style={{ flex: 1 }}>{label}</span>
              {badge != null && (
                <span style={{ fontSize: 10, color: "rgba(255,255,255,.55)", fontFamily: "'JetBrains Mono', monospace" }}>
                  {badge}
                </span>
              )}
            </div>
          );
        })}
      </nav>
      {/* Profile */}
      <div style={{ position: "relative", margin: 10, padding: "10px 12px", background: "rgba(0,0,0,.18)", borderRadius: 8, display: "flex", alignItems: "center", gap: 10 }}>
        <div style={{ width: 28, height: 28, borderRadius: 999, background: ACCENT_LIME, display: "grid", placeItems: "center", color: ACCENT_GREEN, fontWeight: 700, fontSize: 11 }}>SD</div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <p style={{ margin: 0, fontSize: 11.5, fontWeight: 600 }}>Sara De Vos</p>
          <p style={{ margin: 0, fontSize: 10, color: "rgba(255,255,255,.5)" }}>Beheerder</p>
        </div>
        <I.logout style={{ width: 13, height: 13, color: "rgba(255,255,255,.4)" }} />
      </div>
    </aside>
  );
};

const TopbarP = ({ title, subtitle, actions = null }) => (
  <header style={{
    background: SURFACE, borderBottom: `1px solid ${RULE}`,
    padding: "14px 28px", display: "flex", alignItems: "center", justifyContent: "space-between", flexShrink: 0
  }}>
    <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
      <div>
        <p style={{ margin: 0, fontSize: 10.5, color: INK_3, fontFamily: "'JetBrains Mono', monospace", textTransform: "uppercase", letterSpacing: "0.08em" }}>{subtitle}</p>
        <h1 style={{ margin: "1px 0 0", fontSize: 18, fontWeight: 700, color: INK, letterSpacing: -0.3 }}>{title}</h1>
      </div>
    </div>
    <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
      {actions}
      <div style={{ width: 30, height: 30, borderRadius: 999, background: ACCENT_GREEN, display: "grid", placeItems: "center", color: ACCENT_LIME, fontWeight: 700, fontSize: 11 }}>SD</div>
    </div>
  </header>
);

const ShellP = ({ active, title, subtitle, actions, children }) => (
  <div style={{ display: "flex", width: "100%", height: "100%", background: P_BG, fontFamily: "'Inter', system-ui, sans-serif", color: INK }}>
    <SidebarP active={active} />
    <div style={{ flex: 1, display: "flex", flexDirection: "column", minWidth: 0 }}>
      <TopbarP title={title} subtitle={subtitle} actions={actions} />
      <main style={{ flex: 1, padding: "24px 28px", overflow: "hidden" }}>{children}</main>
    </div>
  </div>
);

// ── Dashboard (proposed) ───────────────────────────────────────────────────
const DashboardP = () => (
  <ShellP
    active="Vandaag"
    title="Vandaag"
    subtitle="DI · 21 APR 2026 · WEEK 17"
    actions={
      <button style={{ display: "inline-flex", alignItems: "center", gap: 6, padding: "7px 12px", background: INK, color: "#fff", fontSize: 12, fontWeight: 600, borderRadius: 6, border: "none" }}>
        <I.plus style={{ width: 12, height: 12 }} /> Nieuwe les
      </button>
    }
  >
    {/* Dark hero stat strip — headline numbers */}
    <div style={{ background: INK, color: "#fff", borderRadius: 12, padding: "16px 20px", marginBottom: 14, display: "grid", gridTemplateColumns: "repeat(5,1fr)", gap: 18 }}>
      {[
        ["5", "lessen vandaag", ACCENT_LIME],
        ["38", "deze week", "#fff"],
        ["84%", "bezetting", "#fff"],
        ["€820", "openstaand", "#F59E0B"],
        ["4", "vragen actie", "#fff"],
      ].map(([v, l, c], i) => (
        <div key={i} style={{ borderLeft: i === 0 ? "none" : "1px solid rgba(255,255,255,.1)", paddingLeft: i === 0 ? 0 : 16 }}>
          <div style={{ fontFamily: "'JetBrains Mono', monospace", fontSize: 22, fontWeight: 800, color: c, lineHeight: 1 }}>{v}</div>
          <div style={{ fontSize: 10, color: "#a8a195", marginTop: 4, fontFamily: "'JetBrains Mono', monospace", letterSpacing: "0.05em" }}>{l}</div>
        </div>
      ))}
    </div>

    {/* Priority row — things that need the coach's attention today */}
    <div style={{ background: SURFACE, border: `1px solid ${RULE}`, borderRadius: 12, padding: 18, marginBottom: 18 }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 12 }}>
        <div>
          <p style={{ margin: 0, fontFamily: "'JetBrains Mono', monospace", fontSize: 10.5, color: INK_3, letterSpacing: "0.1em", textTransform: "uppercase" }}>/vraagt actie</p>
          <p style={{ margin: "2px 0 0", fontSize: 14, fontWeight: 700, color: INK }}>4 dingen om nu aan te pakken</p>
        </div>
        <p style={{ margin: 0, fontSize: 11, color: INK_3, fontFamily: "'JetBrains Mono', monospace" }}>laatst 14:02</p>
      </div>
      <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 10 }}>
        {[
          { who: "Els Verhaegen", msg: "heeft nog niet bevestigd voor morgen 18:00", deadline: "verloopt over 6u", tone: "#DC2626" },
          { who: "Padel U12 — Do", msg: "3 plaatsen leeg, start over 8 dagen", deadline: "€42 verlies", tone: "#F59E0B" },
          { who: "Tom De Wit", msg: "vraagt of Wo 17:30 kan verplaatsen", deadline: "sinds 2u", tone: "#F59E0B" },
          { who: "Betaling — Jef M.", msg: "is 5 dagen te laat voor reeks #124", deadline: "€180", tone: "#DC2626" },
        ].map((x, i) => (
          <div key={i} style={{ display: "flex", alignItems: "flex-start", gap: 10, padding: "10px 12px", borderRadius: 8, background: P_BG, border: `1px solid ${RULE}` }}>
            <div style={{ width: 3, alignSelf: "stretch", background: x.tone, borderRadius: 2 }} />
            <div style={{ flex: 1, minWidth: 0 }}>
              <p style={{ margin: 0, fontSize: 12, color: INK }}>
                <span style={{ fontWeight: 600 }}>{x.who}</span> <span style={{ color: INK_2 }}>{x.msg}</span>
              </p>
              <p style={{ margin: "3px 0 0", fontSize: 10.5, color: INK_3, fontFamily: "'JetBrains Mono', monospace" }}>{x.deadline}</p>
            </div>
            <div style={{ display: "flex", gap: 4 }}>
              <button style={{ padding: "3px 8px", fontSize: 10.5, border: `1px solid ${RULE}`, background: "#fff", borderRadius: 5, color: INK_2 }}>later</button>
              <button style={{ padding: "3px 8px", fontSize: 10.5, background: INK, color: "#fff", borderRadius: 5, border: "none", fontWeight: 600 }}>oplos</button>
            </div>
          </div>
        ))}
      </div>
    </div>

    {/* Two-column: today schedule + week KPI sparklines */}
    <div style={{ display: "grid", gridTemplateColumns: "1.6fr 1fr", gap: 18 }}>
      <div style={{ background: SURFACE, border: `1px solid ${RULE}`, borderRadius: 12, overflow: "hidden" }}>
        <div style={{ padding: "14px 18px", borderBottom: `1px solid ${RULE}`, display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <h2 style={{ margin: 0, fontSize: 13, fontWeight: 700, color: INK }}>Vandaag — 5 lessen</h2>
          <span style={{ fontSize: 10.5, color: INK_3, fontFamily: "'JetBrains Mono', monospace" }}>13:00 – 21:00</span>
        </div>
        {/* Timeline row */}
        <div style={{ padding: "16px 18px" }}>
          <div style={{ position: "relative", height: 120 }}>
            {/* hour ticks */}
            {[13,14,15,16,17,18,19,20,21].map((h, i) => (
              <div key={i} style={{ position: "absolute", top: 0, bottom: 0, left: `${(i/8)*100}%`, borderLeft: `1px dashed ${RULE}` }}>
                <span style={{ position: "absolute", top: -2, left: 2, fontSize: 9.5, color: INK_3, fontFamily: "'JetBrains Mono', monospace" }}>{h}h</span>
              </div>
            ))}
            {/* now line */}
            <div style={{ position: "absolute", top: 0, bottom: 0, left: "16%", borderLeft: `2px solid ${ACCENT_GREEN}`, zIndex: 2 }}>
              <span style={{ position: "absolute", top: -18, left: -18, background: ACCENT_GREEN, color: ACCENT_LIME, padding: "2px 6px", fontSize: 9.5, fontWeight: 700, borderRadius: 3 }}>14:02</span>
            </div>
            {/* lesson bars */}
            {[
              { s: 14, e: 15, lbl: "Jeugd U10 · Baan 2", tr: "Lies", who: "4/4", tone: "#E8DCC4", txt: INK },
              { s: 16, e: 17, lbl: "Jeugd U12 · Baan 3", tr: "Lies", who: "6/6", tone: "#E8DCC4", txt: INK },
              { s: 17.5, e: 19, lbl: "Tennis Gev · Baan 1", tr: "Tom", who: "5/6 · 1 wacht", tone: ACCENT_LIME, txt: INK },
              { s: 18, e: 19, lbl: "Padel Beg · Baan 3", tr: "Jan", who: "3/4", tone: "#fff", txt: INK, border: true },
              { s: 19.5, e: 21, lbl: "Padel Gev · Baan 1", tr: "Jan", who: "6/6", tone: "#E8DCC4", txt: INK },
            ].map((b, i) => (
              <div key={i} style={{
                position: "absolute",
                top: 22 + (i * 18),
                left: `${((b.s - 13) / 8) * 100}%`,
                width: `${((b.e - b.s) / 8) * 100}%`,
                height: 16,
                background: b.tone,
                border: b.border ? `1px solid ${RULE}` : "none",
                borderRadius: 3,
                padding: "0 6px",
                display: "flex", alignItems: "center", gap: 6,
                fontSize: 9.5, fontWeight: 600, color: b.txt, overflow: "hidden", whiteSpace: "nowrap",
              }}>
                <span>{b.lbl}</span>
                <span style={{ fontWeight: 400, opacity: 0.65 }}>· {b.who}</span>
              </div>
            ))}
          </div>
        </div>
        <div style={{ padding: "10px 18px", borderTop: `1px solid ${RULE}`, display: "flex", justifyContent: "space-between", fontSize: 10.5, color: INK_3 }}>
          <span>4 bevestigd · 1 wacht op reactie</span>
          <span style={{ fontFamily: "'JetBrains Mono', monospace" }}>bezetting 91%</span>
        </div>
      </div>

      {/* Week stats w/ sparklines */}
      <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
        {[
          { label: "Lessen deze week", val: "38", sub: "+3 t.o.v. vorige", spark: [24,28,30,26,32,35,38] },
          { label: "Bezetting", val: "84%", sub: "doel 80%", spark: [70,72,75,73,78,82,84], good: true },
          { label: "Openstaand", val: "€820", sub: "5 leerlingen", spark: [200,300,500,700,900,850,820], bad: true },
        ].map((s, i) => (
          <div key={i} style={{ background: SURFACE, border: `1px solid ${RULE}`, borderRadius: 12, padding: 14, display: "flex", alignItems: "center", gap: 14 }}>
            <div style={{ flex: 1 }}>
              <p style={{ margin: 0, fontSize: 10.5, color: INK_3, fontWeight: 500, textTransform: "uppercase", letterSpacing: "0.05em" }}>{s.label}</p>
              <p style={{ margin: "4px 0 0", fontSize: 22, fontWeight: 700, color: INK, letterSpacing: -0.4, fontVariantNumeric: "tabular-nums" }}>{s.val}</p>
              <p style={{ margin: "2px 0 0", fontSize: 10.5, color: s.good ? "#047857" : s.bad ? "#DC2626" : INK_3 }}>{s.sub}</p>
            </div>
            <Sparkline data={s.spark} color={s.bad ? "#DC2626" : s.good ? "#047857" : ACCENT_GREEN} />
          </div>
        ))}
      </div>
    </div>
  </ShellP>
);

// Tiny sparkline
function Sparkline({ data, color }) {
  const w = 70, h = 32;
  const min = Math.min(...data), max = Math.max(...data);
  const pts = data.map((v, i) => {
    const x = (i / (data.length - 1)) * w;
    const y = h - ((v - min) / (max - min || 1)) * h;
    return `${x},${y}`;
  }).join(" ");
  return (
    <svg width={w} height={h} viewBox={`0 0 ${w} ${h}`}>
      <polyline points={pts} fill="none" stroke={color} strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
      <circle cx={w} cy={h - ((data[data.length - 1] - min) / (max - min || 1)) * h} r="2.2" fill={color} />
    </svg>
  );
}

// ── Planning calendar (proposed) ───────────────────────────────────────────
const CalendarP = () => {
  const DAYS = ["Ma 01", "Di 02", "Wo 03", "Do 04", "Vr 05", "Za 06", "Zo 07"];
  const HOURS = [9,10,11,12,13,14,15,16,17,18,19,20];
  const HOUR_H = 30;
  const startH = HOURS[0];
  const trainers = {
    jan:  { name: "Jan", color: "#7C3AED", bg: "#EDE9FE" },
    tom:  { name: "Tom", color: "#2563EB", bg: "#DBEAFE" },
    lies: { name: "Lies", color: "#B45309", bg: "#FEF3C7" },
    piet: { name: "Piet", color: "#0D9488", bg: "#CCFBF1" },
  };
  const slots = [
    { d: 0, s: 17, dur: 1, t: "jan", court: "B1", cap: "4/4" },
    { d: 0, s: 18.5, dur: 1, t: "tom", court: "B2", cap: "5/6" },
    { d: 1, s: 16, dur: 1, t: "lies", court: "B3", cap: "3/4", low: true },
    { d: 1, s: 17.5, dur: 1.5, t: "jan", court: "B1", cap: "6/6" },
    { d: 1, s: 18.5, dur: 1, t: "tom", court: "B2", cap: "4/6", low: true },
    { d: 2, s: 17, dur: 1, t: "piet", court: "B2", cap: "4/4" },
    { d: 2, s: 19, dur: 1, t: "jan", court: "B1", cap: "8/8" },
    { d: 3, s: 16, dur: 1, t: "tom", court: "B3", cap: "4/4" },
    { d: 3, s: 17.5, dur: 1, t: "lies", court: "B2", cap: "4/4" },
    { d: 4, s: 17, dur: 1, t: "jan", court: "B1", cap: "6/6" },
    { d: 4, s: 18.5, dur: 1.5, t: "piet", court: "B2", cap: "8/8" },
    { d: 5, s: 10, dur: 1, t: "lies", court: "B2", cap: "2/4", low: true },
    { d: 5, s: 11, dur: 1.5, t: "tom", court: "B1", cap: "6/6" },
  ];

  return (
    <ShellP
      active="Planning"
      title="Padel Beginners — Dinsdagavond"
      subtitle="PLANNING · WEEK 14 VAN 52"
      actions={
        <>
          <div style={{ display: "flex", border: `1px solid ${RULE}`, borderRadius: 6, overflow: "hidden" }}>
            <button style={{ padding: "6px 10px", background: "#fff", color: INK_2, fontSize: 11, border: "none", borderRight: `1px solid ${RULE}` }}>←</button>
            <button style={{ padding: "6px 10px", background: "#fff", color: INK_2, fontSize: 11, border: "none" }}>→</button>
          </div>
          <button style={{ padding: "6px 10px", background: "#fff", border: `1px solid ${RULE}`, borderRadius: 6, fontSize: 11, color: INK_2 }}>Deze week</button>
          <button style={{ padding: "6px 12px", background: INK, color: "#fff", border: "none", borderRadius: 6, fontSize: 11.5, fontWeight: 600 }}>+ Slot</button>
        </>
      }
    >
      {/* Dark hero sticky-confirm-style summary */}
      <div style={{ background: INK, color: "#fff", borderRadius: 10, padding: "12px 18px", marginBottom: 12, display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div style={{ display: "flex", gap: 22 }}>
          {[["13", "slots"], ["66", "plaatsen"], ["61", "bezet"], ["3", "ondervol", "#F59E0B"]].map(([v, l, c], i) => (
            <div key={i} style={{ display: "flex", alignItems: "baseline", gap: 6 }}>
              <span style={{ fontFamily: "'JetBrains Mono', monospace", fontSize: 18, fontWeight: 800, color: c || "#fff" }}>{v}</span>
              <span style={{ fontSize: 11, color: "#a8a195" }}>{l}</span>
            </div>
          ))}
        </div>
        <div style={{ fontFamily: "'JetBrains Mono', monospace", fontSize: 11, color: ACCENT_LIME }}>/week 14 · lente 2026</div>
      </div>

      {/* Legend + filters bar */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12, padding: "0 2px" }}>
        <div style={{ display: "flex", gap: 14, alignItems: "center" }}>
          {Object.entries(trainers).map(([k, t]) => (
            <div key={k} style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 11, color: INK_2 }}>
              <span style={{ width: 10, height: 10, borderRadius: 2, background: t.bg, borderLeft: `3px solid ${t.color}` }} />
              {t.name}
            </div>
          ))}
        </div>
        <div style={{ display: "flex", gap: 14, fontSize: 10.5, color: INK_3, fontFamily: "'JetBrains Mono', monospace" }}>
          <span>13 slots · 66 plaatsen · 61 bezet</span>
          <span style={{ color: "#B45309" }}>● 3 ondervol</span>
        </div>
      </div>

      <div style={{ background: SURFACE, borderRadius: 12, border: `1px solid ${RULE}`, overflow: "hidden" }}>
        {/* Headers */}
        <div style={{ display: "grid", gridTemplateColumns: "52px repeat(7, 1fr)", borderBottom: `1px solid ${RULE}`, background: "#fbfaf6" }}>
          <div />
          {DAYS.map((d, i) => {
            const [day, date] = d.split(" ");
            const weekend = i >= 5;
            return (
              <div key={i} style={{ padding: "8px 8px", borderLeft: `1px solid ${RULE}`, background: weekend ? "#f7f5f0" : "transparent" }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline" }}>
                  <span style={{ fontSize: 10.5, fontWeight: 700, color: INK, textTransform: "uppercase", letterSpacing: "0.06em" }}>{day}</span>
                  <span style={{ fontSize: 10.5, color: INK_3, fontFamily: "'JetBrains Mono', monospace" }}>{date}</span>
                </div>
              </div>
            );
          })}
        </div>
        {/* Body */}
        <div style={{ position: "relative", display: "grid", gridTemplateColumns: "52px repeat(7, 1fr)", height: HOUR_H * HOURS.length }}>
          <div style={{ position: "relative" }}>
            {HOURS.map((h, i) => (
              <div key={i} style={{ position: "absolute", top: i * HOUR_H - 6, right: 6, fontSize: 9.5, color: INK_3, fontFamily: "'JetBrains Mono', monospace" }}>
                {String(h).padStart(2, "0")}
              </div>
            ))}
          </div>
          {DAYS.map((_, di) => (
            <div key={di} style={{ position: "relative", borderLeft: `1px solid ${RULE}`, background: di >= 5 ? "#fbfaf6" : "transparent" }}>
              {HOURS.map((h, hi) => (
                <div key={hi} style={{ position: "absolute", left: 0, right: 0, top: hi * HOUR_H, height: HOUR_H, borderBottom: `1px solid ${RULE}` }} />
              ))}
              {slots.filter((s) => s.d === di).map((s, i) => {
                const tr = trainers[s.t];
                return (
                  <div key={i} style={{
                    position: "absolute",
                    top: (s.s - startH) * HOUR_H + 1,
                    height: s.dur * HOUR_H - 2,
                    left: 2, right: 2,
                    background: tr.bg,
                    borderLeft: `3px solid ${tr.color}`,
                    borderRadius: "0 4px 4px 0",
                    padding: "3px 5px",
                    fontSize: 8.5,
                    overflow: "hidden",
                    position: "absolute",
                  }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline" }}>
                      <span style={{ fontWeight: 700, color: tr.color, fontFamily: "'JetBrains Mono', monospace" }}>
                        {String(Math.floor(s.s)).padStart(2,"0")}:{s.s % 1 ? "30" : "00"}
                      </span>
                      <span style={{ fontSize: 8, color: s.low ? "#B45309" : INK_3, fontWeight: s.low ? 700 : 500 }}>{s.cap}</span>
                    </div>
                    <div style={{ color: INK_2, marginTop: 1, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis", fontWeight: 500 }}>
                      {tr.name} · {s.court}
                    </div>
                  </div>
                );
              })}
            </div>
          ))}
        </div>
      </div>
    </ShellP>
  );
};

// ── Lessons list (proposed) — density upgrade ──────────────────────────────
const LessonsP = () => {
  const rows = [
    { name: "Padel Beginners — Dinsdagavond", tr: "Jan Peeters", dates: "01 apr → 30 jun", slots: 12, filled: 58, cap: 60, price: 180, status: "on" },
    { name: "Tennis Gevorderd — Woensdag", tr: "Tom De Wit", dates: "01 apr → 30 jun", slots: 10, filled: 48, cap: 60, price: 220, status: "on", low: true },
    { name: "Jeugd U12 — Donderdag", tr: "Lies Janssens", dates: "01 apr → 30 jun", slots: 14, filled: 42, cap: 42, price: 140, status: "on" },
    { name: "Padel Zomer — Vrijdagavond", tr: "—", dates: "01 jul → 31 aug", slots: 8, filled: 0, cap: 32, price: 120, status: "draft" },
    { name: "Tennis Beginners — Zaterdag", tr: "Piet V.", dates: "03 apr → 25 jun", slots: 10, filled: 35, cap: 40, price: 160, status: "on" },
  ];
  return (
    <ShellP
      active="Lesreeksen"
      title="Lesreeksen"
      subtitle="5 ACTIEF · 1 CONCEPT · LENTE 2026"
      actions={
        <>
          <div style={{ display: "flex", border: `1px solid ${RULE}`, borderRadius: 6, background: "#fff", overflow: "hidden" }}>
            <span style={{ padding: "6px 10px", fontSize: 11, color: INK, background: "#f5f3ed", fontWeight: 600 }}>Tabel</span>
            <span style={{ padding: "6px 10px", fontSize: 11, color: INK_3, borderLeft: `1px solid ${RULE}` }}>Kaarten</span>
            <span style={{ padding: "6px 10px", fontSize: 11, color: INK_3, borderLeft: `1px solid ${RULE}` }}>Kalender</span>
          </div>
          <button style={{ padding: "6px 12px", background: INK, color: "#fff", border: "none", borderRadius: 6, fontSize: 11.5, fontWeight: 600 }}>+ Nieuw</button>
        </>
      }
    >
      <div style={{ background: SURFACE, border: `1px solid ${RULE}`, borderRadius: 12, overflow: "hidden" }}>
        {/* Column header */}
        <div style={{ display: "grid", gridTemplateColumns: "2.2fr 1fr 1.1fr 1.4fr 0.9fr 0.7fr", padding: "10px 16px", fontSize: 10.5, color: INK_3, fontWeight: 600, fontFamily: "'JetBrains Mono', monospace", textTransform: "uppercase", letterSpacing: "0.08em", borderBottom: `1px solid ${RULE}`, background: "#fbfaf6" }}>
          <span>Reeks</span>
          <span>Trainer</span>
          <span>Periode</span>
          <span>Bezetting</span>
          <span>Prijs</span>
          <span style={{ textAlign: "right" }}>Status</span>
        </div>
        {rows.map((r, i) => (
          <div key={i} style={{ display: "grid", gridTemplateColumns: "2.2fr 1fr 1.1fr 1.4fr 0.9fr 0.7fr", padding: "14px 16px", alignItems: "center", borderBottom: i < rows.length - 1 ? `1px solid ${RULE}` : "none", fontSize: 12 }}>
            <div>
              <p style={{ margin: 0, fontWeight: 600, color: INK }}>{r.name}</p>
              <p style={{ margin: "3px 0 0", fontSize: 10.5, color: INK_3, fontFamily: "'JetBrains Mono', monospace" }}>{r.slots} lesmomenten · reeks #{120 + i}</p>
            </div>
            <p style={{ margin: 0, color: INK_2 }}>{r.tr}</p>
            <p style={{ margin: 0, color: INK_2, fontFamily: "'JetBrains Mono', monospace", fontSize: 11 }}>{r.dates}</p>
            <div>
              <div style={{ display: "flex", alignItems: "baseline", gap: 6 }}>
                <span style={{ fontWeight: 700, color: INK, fontFamily: "'JetBrains Mono', monospace" }}>{r.filled}<span style={{ color: INK_3, fontWeight: 400 }}>/{r.cap}</span></span>
                <span style={{ fontSize: 10.5, color: r.low ? "#B45309" : INK_3 }}>{Math.round((r.filled/r.cap)*100)}%</span>
              </div>
              <div style={{ marginTop: 5, height: 4, borderRadius: 2, background: "#efece4", overflow: "hidden" }}>
                <div style={{ width: `${(r.filled/r.cap)*100}%`, height: "100%", background: r.low ? "#F59E0B" : ACCENT_GREEN }} />
              </div>
            </div>
            <p style={{ margin: 0, fontWeight: 700, color: INK, fontFamily: "'JetBrains Mono', monospace" }}>€{r.price}</p>
            <div style={{ textAlign: "right" }}>
              {r.status === "on"
                ? <span style={{ fontSize: 10.5, padding: "3px 8px", borderRadius: 999, background: "rgba(45,80,22,.1)", color: ACCENT_GREEN, fontWeight: 600 }}>● actief</span>
                : <span style={{ fontSize: 10.5, padding: "3px 8px", borderRadius: 999, background: "#f4f2eb", color: INK_3, fontWeight: 600 }}>○ concept</span>}
            </div>
          </div>
        ))}
      </div>
    </ShellP>
  );
};

// ── Trainers (proposed) ────────────────────────────────────────────────────
const TrainersP = () => {
  const trainers = [
    { first: "Jan", last: "Peeters", email: "jan.peeters@tcleuven.be", load: 14, cap: 16, series: 4, rating: 4.8, status: "on" },
    { first: "Tom", last: "De Wit", email: "tom.dewit@tcleuven.be", load: 10, cap: 12, series: 3, rating: 4.6, status: "on" },
    { first: "Lies", last: "Janssens", email: "lies.j@tcleuven.be", load: 8, cap: 16, series: 2, rating: 4.9, status: "on" },
    { first: "Piet", last: "Van Damme", email: "piet.vd@tcleuven.be", load: 0, cap: 0, series: 0, rating: null, status: "inv" },
  ];
  return (
    <ShellP
      active="Trainers"
      title="Trainers"
      subtitle="3 ACTIEF · 1 UITGENODIGD · CAPACITEIT 32U/W"
      actions={<button style={{ padding: "6px 12px", background: INK, color: "#fff", border: "none", borderRadius: 6, fontSize: 11.5, fontWeight: 600 }}>+ Uitnodigen</button>}
    >
      <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 14 }}>
        {trainers.map((t, i) => {
          const initials = (t.first[0] + t.last[0]).toUpperCase();
          const invited = t.status === "inv";
          const pct = t.cap ? (t.load/t.cap) * 100 : 0;
          return (
            <div key={i} style={{ background: SURFACE, border: `1px solid ${RULE}`, borderRadius: 12, padding: 16 }}>
              <div style={{ display: "flex", alignItems: "flex-start", gap: 12 }}>
                <div style={{ width: 40, height: 40, borderRadius: 10, background: ACCENT_GREEN, display: "grid", placeItems: "center", color: ACCENT_LIME, fontWeight: 700, fontSize: 13, flexShrink: 0 }}>
                  {initials}
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                    <p style={{ margin: 0, fontSize: 13.5, fontWeight: 700, color: INK }}>{t.first} {t.last}</p>
                    {invited
                      ? <span style={{ fontSize: 10, padding: "2px 8px", borderRadius: 999, background: "#fef3c7", color: "#b45309", fontWeight: 600 }}>Uitgenodigd</span>
                      : <span style={{ fontSize: 10, padding: "2px 8px", borderRadius: 999, background: "rgba(45,80,22,.1)", color: ACCENT_GREEN, fontWeight: 600 }}>Actief</span>}
                  </div>
                  <p style={{ margin: "2px 0 0", fontSize: 11, color: INK_3, fontFamily: "'JetBrains Mono', monospace" }}>{t.email}</p>
                </div>
              </div>
              {!invited && (
                <>
                  <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 10, marginTop: 14, paddingTop: 12, borderTop: `1px dashed ${RULE}` }}>
                    <div>
                      <p style={{ margin: 0, fontSize: 9.5, color: INK_3, textTransform: "uppercase", letterSpacing: "0.06em" }}>Lesuren/w</p>
                      <p style={{ margin: "2px 0 0", fontSize: 14, fontWeight: 700, color: INK, fontFamily: "'JetBrains Mono', monospace" }}>{t.load}<span style={{ color: INK_3, fontWeight: 400, fontSize: 11 }}>/{t.cap}</span></p>
                    </div>
                    <div>
                      <p style={{ margin: 0, fontSize: 9.5, color: INK_3, textTransform: "uppercase", letterSpacing: "0.06em" }}>Reeksen</p>
                      <p style={{ margin: "2px 0 0", fontSize: 14, fontWeight: 700, color: INK, fontFamily: "'JetBrains Mono', monospace" }}>{t.series}</p>
                    </div>
                    <div>
                      <p style={{ margin: 0, fontSize: 9.5, color: INK_3, textTransform: "uppercase", letterSpacing: "0.06em" }}>Score</p>
                      <p style={{ margin: "2px 0 0", fontSize: 14, fontWeight: 700, color: INK, fontFamily: "'JetBrains Mono', monospace" }}>{t.rating} <span style={{ color: "#F59E0B" }}>★</span></p>
                    </div>
                  </div>
                  <div style={{ marginTop: 10, height: 4, borderRadius: 2, background: "#efece4", overflow: "hidden" }}>
                    <div style={{ width: `${pct}%`, height: "100%", background: pct > 85 ? "#DC2626" : ACCENT_GREEN }} />
                  </div>
                  <p style={{ margin: "4px 0 0", fontSize: 10, color: INK_3, fontFamily: "'JetBrains Mono', monospace" }}>
                    {pct > 85 ? "bijna vol" : pct < 60 ? "ruimte over" : "gezonde belasting"}
                  </p>
                </>
              )}
              {invited && (
                <div style={{ marginTop: 12, padding: "10px 12px", background: "#fef9ec", borderRadius: 8, border: "1px dashed #f4d48a", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <p style={{ margin: 0, fontSize: 11, color: "#8a5a1a" }}>Uitnodiging verzonden 2d geleden</p>
                  <button style={{ padding: "4px 10px", background: "#fff", border: "1px solid #f0c574", borderRadius: 4, fontSize: 10.5, color: "#8a5a1a", fontWeight: 600 }}>Opnieuw sturen</button>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </ShellP>
  );
};

// ── Student portal (proposed) ──────────────────────────────────────────────
const StudentP = () => (
  <div style={{ width: "100%", height: "100%", background: P_BG, fontFamily: "'Inter', system-ui, sans-serif", color: INK, display: "flex", flexDirection: "column" }}>
    <header style={{ padding: "18px 24px", background: SURFACE, borderBottom: `1px solid ${RULE}`, display: "flex", alignItems: "center", justifyContent: "space-between" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
        <div style={{ width: 28, height: 28, borderRadius: 6, background: ACCENT_GREEN, display: "grid", placeItems: "center" }}>
          <span style={{ color: ACCENT_LIME, fontWeight: 800, fontSize: 13, fontFamily: "'JetBrains Mono', monospace" }}>c/</span>
        </div>
        <p style={{ margin: 0, fontSize: 13, fontWeight: 700 }}>CoachOS</p>
      </div>
      <p style={{ margin: 0, fontSize: 12, color: INK_3 }}>Hallo, Els 👋</p>
    </header>
    <main style={{ flex: 1, padding: "20px 24px", maxWidth: 680, margin: "0 auto", width: "100%" }}>
      {/* Hero — next lesson w/ confirm CTA */}
      <div style={{ background: INK, color: "#fff", borderRadius: 14, padding: 22, marginBottom: 18, position: "relative", overflow: "hidden" }}>
        <CourtLines opacity={0.08} />
        <div style={{ position: "relative" }}>
          <p style={{ margin: 0, fontSize: 10.5, color: ACCENT_LIME, fontFamily: "'JetBrains Mono', monospace", letterSpacing: "0.15em", textTransform: "uppercase" }}>/eerstvolgende</p>
          <h2 style={{ margin: "6px 0 2px", fontSize: 22, fontWeight: 700, letterSpacing: -0.4 }}>Dinsdag 22 apr · 18:00</h2>
          <p style={{ margin: 0, fontSize: 13, color: "rgba(255,255,255,.75)" }}>Padel Beginners met Jan Peeters · Baan 3</p>
          <div style={{ marginTop: 14, display: "flex", alignItems: "center", gap: 10 }}>
            <div style={{ padding: "6px 12px", background: "rgba(255,255,255,.12)", borderRadius: 999, fontSize: 11.5, display: "inline-flex", alignItems: "center", gap: 6 }}>
              <I.clock style={{ width: 12, height: 12, color: ACCENT_LIME }} /> Bevestiging nodig voor 17:00
            </div>
          </div>
          <div style={{ marginTop: 16, display: "flex", gap: 8 }}>
            <button style={{ padding: "10px 16px", background: ACCENT_LIME, color: ACCENT_GREEN, border: "none", borderRadius: 8, fontSize: 12.5, fontWeight: 700 }}>✓ Ik kom</button>
            <button style={{ padding: "10px 14px", background: "rgba(255,255,255,.1)", color: "#fff", border: "1px solid rgba(255,255,255,.2)", borderRadius: 8, fontSize: 12.5, fontWeight: 500 }}>Vraag ruil</button>
          </div>
        </div>
      </div>

      {/* List */}
      <p style={{ margin: "4px 0 10px", fontSize: 11, color: INK_3, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.08em" }}>Deze reeks — 8 van 12</p>
      {[
        { date: "22 apr", wd: "di", status: "open", txt: "Wacht op bevestiging" },
        { date: "29 apr", wd: "di", status: "conf", txt: "Bevestigd" },
        { date: "06 mei", wd: "di", status: "conf", txt: "Bevestigd" },
        { date: "13 mei", wd: "di", status: "swap", txt: "Ruil aangeboden aan Tom B." },
        { date: "20 mei", wd: "di", status: "conf", txt: "Bevestigd" },
      ].map((l, i) => {
        const dot = { open: "#F59E0B", conf: ACCENT_GREEN, swap: "#2563EB" }[l.status];
        return (
          <div key={i} style={{ display: "flex", alignItems: "center", gap: 14, padding: "12px 14px", background: SURFACE, border: `1px solid ${RULE}`, borderRadius: 10, marginBottom: 8 }}>
            <div style={{ width: 44, textAlign: "center", padding: "6px 0", background: P_BG, borderRadius: 6 }}>
              <p style={{ margin: 0, fontSize: 9.5, color: INK_3, textTransform: "uppercase", fontWeight: 600 }}>{l.wd}</p>
              <p style={{ margin: 0, fontSize: 13, fontWeight: 700, color: INK, fontFamily: "'JetBrains Mono', monospace" }}>{l.date.split(" ")[0]}</p>
            </div>
            <div style={{ flex: 1 }}>
              <p style={{ margin: 0, fontSize: 12.5, fontWeight: 600 }}>18:00 – 19:00 · Baan 3</p>
              <p style={{ margin: "3px 0 0", fontSize: 11, color: INK_3, display: "flex", alignItems: "center", gap: 6 }}>
                <span style={{ width: 6, height: 6, borderRadius: 999, background: dot }} /> {l.txt}
              </p>
            </div>
            <I.chev style={{ width: 14, height: 14, color: INK_3 }} />
          </div>
        );
      })}
    </main>
  </div>
);

// ── Login (proposed) — keep brand, trim promo ──────────────────────────────
const LoginP = () => (
  <div style={{ display: "flex", width: "100%", height: "100%", fontFamily: "'Inter', system-ui, sans-serif", background: P_BG }}>
    <div style={{ width: "44%", background: INK, color: "#fff", padding: 40, display: "flex", flexDirection: "column", justifyContent: "space-between", position: "relative", overflow: "hidden" }}>
      <CourtLines opacity={0.05} />
      {/* subtle green glow accent */}
      <div style={{ position: "absolute", width: 320, height: 320, borderRadius: 999, background: "radial-gradient(closest-side, rgba(45,80,22,.55), transparent)", right: -120, top: -80, filter: "blur(10px)" }} />
      <div style={{ position: "relative", display: "flex", alignItems: "center", gap: 10 }}>
        <div style={{ width: 30, height: 30, borderRadius: 6, background: ACCENT_LIME, display: "grid", placeItems: "center" }}>
          <span style={{ color: INK, fontWeight: 800, fontSize: 13, fontFamily: "'JetBrains Mono', monospace" }}>c/</span>
        </div>
        <span style={{ fontSize: 16, fontWeight: 700 }}>CoachOS</span>
        <span style={{ marginLeft: "auto", fontFamily: "'JetBrains Mono', monospace", fontSize: 10.5, color: "#8a8377" }}>/v2026.04</span>
      </div>

      <div style={{ position: "relative" }}>
        <p style={{ margin: 0, fontSize: 10.5, color: ACCENT_LIME, fontFamily: "'JetBrains Mono', monospace", letterSpacing: "0.15em", textTransform: "uppercase" }}>
          /lente release
        </p>
        <h1 style={{ margin: "10px 0 12px", fontSize: 34, fontWeight: 800, lineHeight: 1.05, letterSpacing: -0.9 }}>
          Een planning die<br />zichzelf <span style={{ color: ACCENT_LIME }}>bevestigt.</span>
        </h1>
        <p style={{ margin: 0, fontSize: 13, color: "#a8a195", maxWidth: 300, lineHeight: 1.6 }}>
          Lesreeksen, inschrijvingen en betalingen voor tennis- en padelclubs — één systeem.
        </p>
      </div>

      {/* Proof */}
      <div style={{ position: "relative", padding: "16px 18px", background: "rgba(255,255,255,.04)", borderRadius: 10, border: "1px solid rgba(255,255,255,.08)" }}>
        <p style={{ margin: 0, fontSize: 13, lineHeight: 1.5, color: "#e9e4db", fontWeight: 500 }}>
          "We houden nu 6 uur per week over doordat leerlingen zélf bevestigen."
        </p>
        <div style={{ display: "flex", alignItems: "center", gap: 10, marginTop: 12 }}>
          <div style={{ width: 24, height: 24, borderRadius: 999, background: ACCENT_LIME, display: "grid", placeItems: "center", fontFamily: "'JetBrains Mono', monospace", fontSize: 10, fontWeight: 700, color: INK }}>SD</div>
          <div>
            <p style={{ margin: 0, fontSize: 11, color: "#fff", fontWeight: 600 }}>Sara De Vos</p>
            <p style={{ margin: 0, fontSize: 10.5, color: "#8a8377", fontFamily: "'JetBrains Mono', monospace" }}>TC Leuven · 214 leerlingen</p>
          </div>
        </div>
      </div>
    </div>

    <div style={{ width: "56%", background: SURFACE, display: "grid", placeItems: "center", padding: 32 }}>
      <div style={{ width: "100%", maxWidth: 340 }}>
        <p style={{ margin: 0, fontSize: 10.5, color: INK_3, fontFamily: "'JetBrains Mono', monospace", textTransform: "uppercase", letterSpacing: "0.1em" }}>
          /login
        </p>
        <h2 style={{ margin: "4px 0 6px", fontSize: 24, fontWeight: 700, letterSpacing: -0.4, color: INK }}>Welkom terug</h2>
        <p style={{ margin: 0, fontSize: 12.5, color: INK_3 }}>Log in bij je clubaccount.</p>

        {/* Role segment */}
        <div style={{ marginTop: 20, display: "flex", border: `1px solid ${RULE}`, borderRadius: 8, padding: 3, background: P_BG }}>
          <div style={{ flex: 1, padding: "7px 0", textAlign: "center", background: "#fff", borderRadius: 6, fontSize: 11.5, fontWeight: 600, color: INK, boxShadow: "0 1px 2px rgba(0,0,0,.04)" }}>Coach / beheerder</div>
          <div style={{ flex: 1, padding: "7px 0", textAlign: "center", fontSize: 11.5, color: INK_3 }}>Leerling</div>
        </div>

        <div style={{ marginTop: 18, display: "flex", flexDirection: "column", gap: 14 }}>
          <div>
            <p style={{ margin: "0 0 6px", fontSize: 11, color: INK_2, fontWeight: 600, fontFamily: "'JetBrains Mono', monospace", textTransform: "uppercase", letterSpacing: "0.06em" }}>E-mail</p>
            <div style={{ height: 40, border: `1px solid ${RULE}`, borderRadius: 8, background: "#fff", padding: "0 12px", display: "flex", alignItems: "center", fontSize: 12.5, color: INK }}>
              sara@tcleuven.be
            </div>
          </div>
          <div>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <p style={{ margin: "0 0 6px", fontSize: 11, color: INK_2, fontWeight: 600, fontFamily: "'JetBrains Mono', monospace", textTransform: "uppercase", letterSpacing: "0.06em" }}>Wachtwoord</p>
              <p style={{ margin: 0, fontSize: 11, color: ACCENT_GREEN, fontWeight: 600 }}>Vergeten?</p>
            </div>
            <div style={{ height: 40, border: `1px solid ${RULE}`, borderRadius: 8, background: "#fff", padding: "0 12px", display: "flex", alignItems: "center", fontSize: 12.5, color: INK }}>
              ••••••••••••
            </div>
          </div>
          <button style={{ height: 42, background: INK, color: "#fff", fontSize: 13, fontWeight: 700, border: "none", borderRadius: 8 }}>
            Inloggen →
          </button>
          <div style={{ display: "flex", alignItems: "center", gap: 10, margin: "6px 0" }}>
            <div style={{ flex: 1, height: 1, background: RULE }} />
            <span style={{ fontSize: 10.5, color: INK_3, fontFamily: "'JetBrains Mono', monospace" }}>of</span>
            <div style={{ flex: 1, height: 1, background: RULE }} />
          </div>
          <button style={{ height: 40, background: "#fff", color: INK, fontSize: 12, fontWeight: 600, border: `1px solid ${RULE}`, borderRadius: 8 }}>
            Magische link per e-mail
          </button>
        </div>
        <p style={{ margin: "20px 0 0", textAlign: "center", fontSize: 11.5, color: INK_3 }}>
          Nog geen club? <span style={{ color: INK, fontWeight: 700, borderBottom: `1px solid ${ACCENT_LIME}` }}>Probeer 30 dagen gratis</span>
        </p>
      </div>
    </div>
  </div>
);

Object.assign(window, {
  DashboardP, CalendarP, LessonsP, TrainersP, StudentP, LoginP,
  ShellP, SidebarP, TopbarP, P_BG, INK, INK_2, INK_3, RULE, SURFACE,
});
