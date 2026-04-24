// Recreations of the CURRENT CoachOS frontend, pixel-faithful from source.
// Each screen is a static React component sized to fit inside a DCArtboard.

// ── Shared bits ────────────────────────────────────────────────────────────

const GREEN = "#2D5016";
const LIME = "#D0FF14";
const BEIGE = "#E8DCC4";
const CANVAS_BG = "#F5F4F1";

const iconStroke = {
  fill: "none",
  stroke: "currentColor",
  strokeWidth: 1.75,
  strokeLinecap: "round",
  strokeLinejoin: "round",
};

// Tiny hand-rolled icons — match lucide shapes closely enough for mock.
const I = {
  dash: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <rect x="3" y="3" width="7" height="9" rx="1" />
      <rect x="14" y="3" width="7" height="5" rx="1" />
      <rect x="14" y="12" width="7" height="9" rx="1" />
      <rect x="3" y="16" width="7" height="5" rx="1" />
    </svg>
  ),
  book: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M4 19.5V4a2 2 0 012-2h14v20H6a2 2 0 010-4h14" />
    </svg>
  ),
  cap: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M22 10L12 5 2 10l10 5 10-5z" />
      <path d="M6 12v5c0 1.5 3 3 6 3s6-1.5 6-3v-5" />
    </svg>
  ),
  users: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" />
    </svg>
  ),
  gear: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <circle cx="12" cy="12" r="3" />
      <path d="M19.4 15a1.7 1.7 0 00.34 1.87l.06.06a2 2 0 11-2.83 2.83l-.06-.06a1.7 1.7 0 00-1.87-.34 1.7 1.7 0 00-1.03 1.56V21a2 2 0 01-4 0v-.09A1.7 1.7 0 009 19.4a1.7 1.7 0 00-1.87.34l-.06.06a2 2 0 11-2.83-2.83l.06-.06A1.7 1.7 0 004.6 15a1.7 1.7 0 00-1.56-1.03H3a2 2 0 010-4h.09A1.7 1.7 0 004.6 9a1.7 1.7 0 00-.34-1.87l-.06-.06a2 2 0 112.83-2.83l.06.06A1.7 1.7 0 009 4.6a1.7 1.7 0 001.03-1.56V3a2 2 0 014 0v.09A1.7 1.7 0 0015 4.6a1.7 1.7 0 001.87-.34l.06-.06a2 2 0 112.83 2.83l-.06.06A1.7 1.7 0 0019.4 9a1.7 1.7 0 001.56 1.03H21a2 2 0 010 4h-.09A1.7 1.7 0 0019.4 15z" />
    </svg>
  ),
  plus: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M12 5v14M5 12h14" />
    </svg>
  ),
  cal: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <rect x="3" y="4" width="18" height="18" rx="2" />
      <path d="M16 2v4M8 2v4M3 10h18" />
    </svg>
  ),
  card: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <rect x="2" y="5" width="20" height="14" rx="2" />
      <path d="M2 10h20" />
    </svg>
  ),
  chev: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M9 18l6-6-6-6" />
    </svg>
  ),
  clock: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <circle cx="12" cy="12" r="10" />
      <path d="M12 6v6l4 2" />
    </svg>
  ),
  logout: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4M16 17l5-5-5-5M21 12H9" />
    </svg>
  ),
  ball: (p = {}) => (
    <svg viewBox="0 0 24 24" fill="none">
      <circle cx="12" cy="12" r="9" fill={GREEN} />
      <path d="M3 12c3-2 6-2 9 0s6 2 9 0M3 12c3 2 6 2 9 0s6-2 9 0" stroke={LIME} strokeWidth="1.5" fill="none" />
    </svg>
  ),
  euro: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M4 10h10M4 14h10M18 6a8 8 0 100 12" />
    </svg>
  ),
  mail: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <rect x="2" y="5" width="20" height="14" rx="2" />
      <path d="M3 6l9 7 9-7" />
    </svg>
  ),
  trash: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M3 6h18M8 6V4a2 2 0 012-2h4a2 2 0 012 2v2M6 6l1 14a2 2 0 002 2h6a2 2 0 002-2l1-14" />
    </svg>
  ),
  userx: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
      <circle cx="8.5" cy="7" r="4" />
      <path d="M18 8l5 5M23 8l-5 5" />
    </svg>
  ),
  check: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <circle cx="12" cy="12" r="10" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  ),
  phone: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M22 16.9v3a2 2 0 01-2.2 2 19.8 19.8 0 01-8.6-3 19.5 19.5 0 01-6-6 19.8 19.8 0 01-3-8.7A2 2 0 014 2h3a2 2 0 012 1.7c0 1 .3 2 .6 3a2 2 0 01-.4 2L7.9 10a16 16 0 006 6l1.3-1.3a2 2 0 012-.4c1 .3 2 .6 3 .6a2 2 0 011.8 2z" />
    </svg>
  ),
  msg: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M21 11.5a8.4 8.4 0 01-.9 3.8 8.5 8.5 0 01-7.6 4.7 8.4 8.4 0 01-3.8-.9L3 21l1.9-5.7a8.4 8.4 0 01-.9-3.8 8.5 8.5 0 014.7-7.6 8.4 8.4 0 013.8-.9 8.5 8.5 0 018.5 8.5z" />
    </svg>
  ),
  copy: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <rect x="9" y="9" width="13" height="13" rx="2" />
      <path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1" />
    </svg>
  ),
  refresh: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M23 4v6h-6M1 20v-6h6" />
      <path d="M20.5 9A9 9 0 003.5 9M3.5 15a9 9 0 0017 0" />
    </svg>
  ),
  x: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M18 6L6 18M6 6l12 12" />
    </svg>
  ),
  trend: (p = {}) => (
    <svg viewBox="0 0 24 24" {...iconStroke} {...p}>
      <path d="M22 7L13.5 15.5 8.5 10.5 2 17" />
      <path d="M16 7h6v6" />
    </svg>
  ),
};

// ── Court lines background pattern (matches tennis-green sidebar) ──────────
const CourtLines = ({ opacity = 0.08 }) => (
  <svg
    style={{ position: "absolute", inset: 0, width: "100%", height: "100%", pointerEvents: "none" }}
    viewBox="0 0 200 800"
    preserveAspectRatio="none"
  >
    <g stroke={LIME} strokeWidth="1" fill="none" opacity={opacity}>
      <rect x="20" y="40" width="160" height="720" />
      <line x1="20" y1="200" x2="180" y2="200" />
      <line x1="20" y1="560" x2="180" y2="560" />
      <line x1="100" y1="200" x2="100" y2="560" />
      <line x1="100" y1="40" x2="100" y2="200" strokeDasharray="4 4" />
      <line x1="100" y1="560" x2="100" y2="760" strokeDasharray="4 4" />
    </g>
  </svg>
);

// ── Sidebar (exactly as in dashboard-sidebar.tsx) ───────────────────────────
const Sidebar = ({ active = "Dashboard" }) => {
  const items = [
    { label: "Dashboard", icon: I.dash },
    { label: "Lessen", icon: I.book },
    { label: "Trainers", icon: I.cap },
    { label: "Leerlingen", icon: I.users },
    { label: "Instellingen", icon: I.gear },
  ];
  return (
    <aside style={{
      width: 220, background: GREEN, position: "relative", overflow: "hidden",
      display: "flex", flexDirection: "column", flexShrink: 0, color: "#fff",
    }}>
      <CourtLines />
      <div style={{ position: "relative", padding: "22px 24px 20px", borderBottom: "1px solid rgba(255,255,255,.1)" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div style={{ width: 30, height: 30, borderRadius: 999, background: LIME, display: "grid", placeItems: "center" }}>
            <I.ball style={{ width: 18, height: 18 }} />
          </div>
          <span style={{ fontWeight: 700, fontSize: 17, letterSpacing: -0.3 }}>
            Coach<span style={{ color: LIME }}>OS</span>
          </span>
        </div>
      </div>
      <nav style={{ position: "relative", flex: 1, padding: "14px 10px", display: "flex", flexDirection: "column", gap: 2 }}>
        {items.map(({ label, icon: Icon }) => {
          const on = label === active;
          return (
            <div key={label}
              style={{
                display: "flex", alignItems: "center", gap: 12, padding: "9px 12px",
                borderRadius: 8, fontSize: 13, fontWeight: 500,
                color: on ? "#fff" : "rgba(255,255,255,.6)",
                background: on ? "rgba(255,255,255,.15)" : "transparent",
                position: "relative",
              }}>
              {on && <span style={{ position: "absolute", left: 4, width: 2, height: 18, background: LIME, borderRadius: 2 }} />}
              <Icon style={{ width: 16, height: 16, color: on ? LIME : "rgba(255,255,255,.5)" }} />
              <span>{label}</span>
            </div>
          );
        })}
      </nav>
      <div style={{ position: "relative", padding: "14px 10px", borderTop: "1px solid rgba(255,255,255,.1)" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 12, padding: "9px 12px", color: "rgba(255,255,255,.55)", fontSize: 13 }}>
          <I.logout style={{ width: 16, height: 16 }} />
          <span>Uitloggen</span>
        </div>
      </div>
    </aside>
  );
};

// ── Topbar ──────────────────────────────────────────────────────────────────
const Topbar = () => (
  <header style={{
    background: "#fff", borderBottom: "1px solid #f3f3f3",
    padding: "14px 28px", display: "flex", alignItems: "center", justifyContent: "space-between",
    flexShrink: 0,
  }}>
    <p style={{ fontSize: 10.5, color: "#9ca3af", fontWeight: 500, letterSpacing: "0.1em", textTransform: "uppercase", margin: 0 }}>
      Tennis &amp; Padel Platform
    </p>
    <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
      <div style={{ textAlign: "right" }}>
        <p style={{ fontSize: 13, fontWeight: 600, color: "#1f2937", margin: 0, lineHeight: 1 }}>Sara De Vos</p>
        <p style={{ fontSize: 11, color: "#9ca3af", margin: "2px 0 0" }}>Beheerder</p>
      </div>
      <div style={{ width: 34, height: 34, borderRadius: 999, background: GREEN, display: "grid", placeItems: "center" }}>
        <span style={{ color: LIME, fontWeight: 700, fontSize: 12 }}>SD</span>
      </div>
    </div>
  </header>
);

const Shell = ({ active, children }) => (
  <div style={{ display: "flex", width: "100%", height: "100%", background: CANVAS_BG, fontFamily: "'Inter', system-ui, sans-serif", color: "#1f2937" }}>
    <Sidebar active={active} />
    <div style={{ flex: 1, display: "flex", flexDirection: "column", minWidth: 0 }}>
      <Topbar />
      <main style={{ flex: 1, padding: "28px 32px", overflow: "hidden" }}>{children}</main>
    </div>
  </div>
);

// ── Screen: Dashboard (current) ────────────────────────────────────────────
const DashboardCurrent = () => {
  const stats = [
    { label: "Actieve lesreeksen", value: 12, color: GREEN, border: GREEN, icon: I.book },
    { label: "Lessen deze week", value: 38, color: "#2563eb", border: "#3b82f6", icon: I.cal },
    { label: "Inschrijvingen", value: 214, color: "#7c3aed", border: "#8b5cf6", icon: I.users },
    { label: "Trainers", value: 6, color: "#d97706", border: "#f59e0b", icon: I.card },
  ];
  const lessons = [
    { s: "Padel Beginners — Dinsdag", who: "Jan Peeters · Baan 3", date: "22/04/2026", time: "18:00 – 19:00" },
    { s: "Tennis Gevorderd — Woensdag", who: "Tom De Wit · Baan 1", date: "23/04/2026", time: "17:30 – 19:00" },
    { s: "Jeugd U12 — Donderdag", who: "Lies Janssens · Baan 2", date: "24/04/2026", time: "16:00 – 17:00" },
  ];
  return (
    <Shell active="Dashboard">
      <h1 style={{ fontSize: 22, fontWeight: 700, letterSpacing: -0.4, margin: 0, color: "#111827" }}>Goedemorgen, Sara 👋</h1>
      <p style={{ fontSize: 12.5, color: "#9ca3af", margin: "4px 0 22px" }}>Hier is een overzicht van vandaag.</p>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 14, marginBottom: 24 }}>
        {stats.map((s, i) => (
          <div key={i} style={{
            background: "#fff", borderRadius: 12, padding: 16, borderLeft: `4px solid ${s.border}`,
            boxShadow: "0 1px 2px rgba(0,0,0,.03)",
            display: "flex", justifyContent: "space-between", alignItems: "flex-start"
          }}>
            <div>
              <p style={{ margin: 0, fontSize: 9.5, color: "#9ca3af", fontWeight: 500, textTransform: "uppercase", letterSpacing: "0.05em" }}>
                {s.label}
              </p>
              <p style={{ margin: "6px 0 0", fontSize: 26, fontWeight: 700, color: s.color, lineHeight: 1 }}>{s.value}</p>
            </div>
            <div style={{ background: "#f9fafb", borderRadius: 8, padding: 6 }}>
              <s.icon style={{ width: 14, height: 14, color: "#9ca3af" }} />
            </div>
          </div>
        ))}
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr", gap: 20 }}>
        <div style={{ background: "#fff", borderRadius: 12, boxShadow: "0 1px 2px rgba(0,0,0,.03)", overflow: "hidden" }}>
          <div style={{ padding: "16px 20px", borderBottom: "1px solid #f4f4f4", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <h2 style={{ margin: 0, fontSize: 13, fontWeight: 600, color: "#1f2937" }}>Komende lessen</h2>
            <span style={{ display: "inline-flex", alignItems: "center", gap: 4, fontSize: 11.5, fontWeight: 500, color: GREEN }}>
              <I.plus style={{ width: 11, height: 11 }} />
              Nieuwe les
            </span>
          </div>
          {lessons.map((l, i) => (
            <div key={i} style={{ padding: "14px 20px", borderBottom: i < lessons.length - 1 ? "1px solid #f9fafb" : "none", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div style={{ display: "flex", alignItems: "center", gap: 14 }}>
                <div style={{ width: 36, height: 36, borderRadius: 8, background: "rgba(45,80,22,.1)", display: "grid", placeItems: "center" }}>
                  <I.cal style={{ width: 16, height: 16, color: GREEN }} />
                </div>
                <div>
                  <p style={{ margin: 0, fontSize: 13, fontWeight: 500, color: "#111827" }}>{l.s}</p>
                  <p style={{ margin: "2px 0 0", fontSize: 11, color: "#9ca3af" }}>{l.who}</p>
                </div>
              </div>
              <div style={{ textAlign: "right" }}>
                <p style={{ margin: 0, fontSize: 12.5, fontWeight: 500, color: "#374151" }}>{l.date}</p>
                <p style={{ margin: "2px 0 0", fontSize: 10.5, color: "#9ca3af", display: "flex", alignItems: "center", gap: 4, justifyContent: "flex-end" }}>
                  <I.clock style={{ width: 10, height: 10 }} /> {l.time}
                </p>
              </div>
            </div>
          ))}
        </div>

        <div style={{ background: "#fff", borderRadius: 12, boxShadow: "0 1px 2px rgba(0,0,0,.03)", overflow: "hidden" }}>
          <div style={{ padding: "16px 20px", borderBottom: "1px solid #f4f4f4" }}>
            <h2 style={{ margin: 0, fontSize: 13, fontWeight: 600, color: "#1f2937" }}>Snelle acties</h2>
          </div>
          <div style={{ padding: 12 }}>
            {[
              { label: "Nieuwe les", icon: I.plus },
              { label: "Leerling toevoegen", icon: I.users },
            ].map((a, i) => (
              <div key={i} style={{ display: "flex", alignItems: "center", gap: 12, padding: "11px 12px", borderRadius: 8 }}>
                <div style={{ width: 28, height: 28, borderRadius: 8, background: "rgba(45,80,22,.1)", display: "grid", placeItems: "center" }}>
                  <a.icon style={{ width: 13, height: 13, color: GREEN }} />
                </div>
                <span style={{ fontSize: 12.5, fontWeight: 500, color: "#4b5563" }}>{a.label}</span>
                <I.chev style={{ width: 13, height: 13, color: "#d1d5db", marginLeft: "auto" }} />
              </div>
            ))}
            <div style={{ marginTop: 10, paddingTop: 12, borderTop: "1px solid #f9fafb" }}>
              <div style={{ background: "rgba(232,220,196,.6)", borderRadius: 8, padding: 12 }}>
                <p style={{ margin: 0, fontSize: 10.5, fontWeight: 600, color: GREEN }}>Tip</p>
                <p style={{ margin: "4px 0 0", fontSize: 10.5, color: "#6b7280", lineHeight: 1.5 }}>
                  Maak eerst je tennisclubs aan voordat je lessen plant.
                </p>
                <p style={{ margin: "8px 0 0", fontSize: 10.5, fontWeight: 600, color: GREEN }}>Instellingen →</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Shell>
  );
};

// ── Screen: Lessons list (current) ─────────────────────────────────────────
const LessonsCurrent = () => {
  const series = [
    { name: "Padel Beginners — Dinsdagavond", dates: "2026-04-01 → 2026-06-30", price: 180, count: 12, active: true },
    { name: "Tennis Gevorderd — Woensdag", dates: "2026-04-01 → 2026-06-30", price: 220, count: 10, active: true },
    { name: "Jeugd U12 — Donderdag", dates: "2026-04-01 → 2026-06-30", price: 140, count: 14, active: true },
    { name: "Padel Zomer — Vrijdagavond", dates: "2026-07-01 → 2026-08-31", price: 120, count: 8, active: false },
  ];
  return (
    <Shell active="Lessen">
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 28 }}>
        <div>
          <h1 style={{ fontSize: 22, fontWeight: 700, color: "#111827", margin: 0, letterSpacing: -0.4 }}>Lesreeksen</h1>
          <p style={{ fontSize: 12.5, color: "#9ca3af", margin: "4px 0 0" }}>Beheer je lesreeksen en lesmomenten.</p>
        </div>
        <button style={{
          display: "inline-flex", alignItems: "center", gap: 8, padding: "10px 16px",
          background: GREEN, color: "#fff", fontSize: 12.5, fontWeight: 600,
          borderRadius: 8, border: "none",
        }}>
          <I.plus style={{ width: 14, height: 14 }} /> Nieuwe lesreeks
        </button>
      </div>
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 14 }}>
        {series.map((s, i) => (
          <div key={i} style={{
            background: "#fff", borderRadius: 12, padding: 18,
            borderLeft: `4px solid ${GREEN}`,
            boxShadow: "0 1px 2px rgba(0,0,0,.03)",
            display: "flex", flexDirection: "column"
          }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 2 }}>
              <h3 style={{ margin: 0, fontSize: 13, fontWeight: 600, color: "#111827" }}>{s.name}</h3>
              <span style={{
                padding: "3px 9px", borderRadius: 999, fontSize: 10.5, fontWeight: 500,
                background: s.active ? "#ecfdf5" : "#f3f4f6",
                color: s.active ? "#047857" : "#6b7280",
              }}>
                {s.active ? "Actief" : "Inactief"}
              </span>
            </div>
            <p style={{ margin: "0 0 14px", fontSize: 11, color: "#6b7280" }}>{s.dates}</p>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 6, marginBottom: 14 }}>
              <div style={{ background: CANVAS_BG, borderRadius: 8, padding: "8px 4px", textAlign: "center" }}>
                <I.euro style={{ width: 11, height: 11, color: GREEN, margin: "0 auto 4px", display: "block" }} />
                <p style={{ margin: 0, fontSize: 8.5, color: "#9ca3af", lineHeight: 1 }}>Prijs</p>
                <p style={{ margin: "2px 0 0", fontSize: 11, fontWeight: 600, color: "#374151", lineHeight: 1 }}>€{s.price}</p>
              </div>
              <div style={{ background: CANVAS_BG, borderRadius: 8, padding: "8px 4px" }} />
              <div style={{ background: CANVAS_BG, borderRadius: 8, padding: "8px 4px" }} />
              <div style={{ background: CANVAS_BG, borderRadius: 8, padding: "8px 4px" }} />
            </div>
            <div style={{ paddingTop: 12, borderTop: "1px solid #f3f4f6", display: "flex", justifyContent: "space-between", alignItems: "center", marginTop: "auto" }}>
              <span style={{ fontSize: 10.5, color: "#9ca3af" }}>{s.count} lesmomenten</span>
              <span style={{ fontSize: 11, fontWeight: 500, color: GREEN, display: "inline-flex", alignItems: "center", gap: 2 }}>
                Bekijken <I.chev style={{ width: 11, height: 11 }} />
              </span>
            </div>
          </div>
        ))}
      </div>
    </Shell>
  );
};

// ── Screen: Trainers (current) ─────────────────────────────────────────────
const TrainersCurrent = () => {
  const active = [
    { first: "Jan", last: "Peeters", email: "jan.peeters@tcleuven.be" },
    { first: "Tom", last: "De Wit", email: "tom.dewit@tcleuven.be" },
    { first: "Lies", last: "Janssens", email: "lies.j@tcleuven.be" },
  ];
  const invited = [{ first: "Piet", last: "Van Damme", email: "piet.vd@tcleuven.be" }];
  return (
    <Shell active="Trainers">
      <div style={{ maxWidth: 640, margin: "0 auto" }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 22 }}>
          <div>
            <h1 style={{ fontSize: 22, fontWeight: 700, color: "#111827", margin: 0 }}>Trainers</h1>
            <p style={{ fontSize: 12.5, color: "#9ca3af", margin: "4px 0 0" }}>Beheer de trainers van je organisatie.</p>
          </div>
          <button style={{ display: "inline-flex", alignItems: "center", gap: 6, padding: "8px 14px", background: GREEN, color: "#fff", fontSize: 12, fontWeight: 600, borderRadius: 8, border: "none" }}>
            <I.plus style={{ width: 13, height: 13 }} /> Trainer uitnodigen
          </button>
        </div>

        <div style={{ background: "#fff", borderRadius: 12, padding: 22, boxShadow: "0 1px 2px rgba(0,0,0,.03)" }}>
          <p style={{ margin: "0 0 8px", fontSize: 10, fontWeight: 600, color: "#9ca3af", textTransform: "uppercase", letterSpacing: "0.08em" }}>
            Actief ({active.length})
          </p>
          {active.map((t, i) => {
            const initials = (t.first[0] + t.last[0]).toUpperCase();
            return (
              <div key={i} style={{ padding: "14px 0", borderBottom: i < active.length - 1 ? "1px solid #f3f4f6" : "none", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                  <div style={{ width: 34, height: 34, borderRadius: 999, background: "rgba(45,80,22,.1)", display: "grid", placeItems: "center" }}>
                    <span style={{ color: GREEN, fontSize: 12, fontWeight: 600 }}>{initials}</span>
                  </div>
                  <div>
                    <p style={{ margin: 0, fontSize: 12.5, fontWeight: 500, color: "#111827" }}>{t.first} {t.last}</p>
                    <p style={{ margin: "2px 0 0", fontSize: 10.5, color: "#9ca3af" }}>{t.email}</p>
                  </div>
                </div>
                <span style={{ display: "inline-flex", alignItems: "center", gap: 4, padding: "4px 10px", borderRadius: 999, fontSize: 10.5, fontWeight: 500, background: "#ecfdf5", color: "#047857" }}>
                  <I.check style={{ width: 10, height: 10 }} /> Actief
                </span>
              </div>
            );
          })}

          <p style={{ margin: "22px 0 8px", fontSize: 10, fontWeight: 600, color: "#9ca3af", textTransform: "uppercase", letterSpacing: "0.08em" }}>
            Uitgenodigd ({invited.length})
          </p>
          {invited.map((t, i) => {
            const initials = (t.first[0] + t.last[0]).toUpperCase();
            return (
              <div key={i} style={{ padding: "14px 0", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                  <div style={{ width: 34, height: 34, borderRadius: 999, background: "rgba(45,80,22,.1)", display: "grid", placeItems: "center" }}>
                    <span style={{ color: GREEN, fontSize: 12, fontWeight: 600 }}>{initials}</span>
                  </div>
                  <div>
                    <p style={{ margin: 0, fontSize: 12.5, fontWeight: 500, color: "#111827" }}>{t.first} {t.last}</p>
                    <p style={{ margin: "2px 0 0", fontSize: 10.5, color: "#9ca3af" }}>{t.email}</p>
                  </div>
                </div>
                <span style={{ display: "inline-flex", alignItems: "center", gap: 4, padding: "4px 10px", borderRadius: 999, fontSize: 10.5, fontWeight: 500, background: "#fffbeb", color: "#b45309" }}>
                  <I.clock style={{ width: 10, height: 10 }} /> Uitgenodigd
                </span>
              </div>
            );
          })}
        </div>
      </div>
    </Shell>
  );
};

// ── Screen: Planning calendar (current) ────────────────────────────────────
const CalendarCurrent = () => {
  const DAYS = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];
  const HOURS = Array.from({ length: 12 }, (_, i) => 9 + i); // 09 – 20
  // slots: dayIdx, startHour (float), durationH, trainerColor
  const COL = {
    violet: { bg: "rgba(139,92,246,.12)", bd: "#8B5CF6", tx: "#6D28D9" },
    blue:   { bg: "rgba(59,130,246,.12)", bd: "#3B82F6", tx: "#2563EB" },
    amber:  { bg: "rgba(245,158,11,.12)", bd: "#F59E0B", tx: "#B45309" },
    pink:   { bg: "rgba(236,72,153,.12)", bd: "#EC4899", tx: "#BE185D" },
    teal:   { bg: "rgba(20,184,166,.12)", bd: "#14B8A6", tx: "#0D9488" },
  };
  const slots = [
    { d: 0, s: 17, dur: 1, c: "violet", title: "17:00 — 18:00", sub: "Baan 1 · Jan P. · 4 max" },
    { d: 0, s: 18.5, dur: 1, c: "blue", title: "18:30 — 19:30", sub: "Baan 2 · Tom · 6 max" },
    { d: 1, s: 16, dur: 1, c: "amber", title: "16:00 — 17:00", sub: "Baan 3 · Lies · 4 max" },
    { d: 1, s: 17.5, dur: 1.5, c: "violet", title: "17:30 — 19:00", sub: "Baan 1 · Jan · 6 max" },
    { d: 2, s: 17, dur: 1, c: "teal", title: "17:00 — 18:00", sub: "Baan 2 · Piet · 4 max" },
    { d: 2, s: 19, dur: 1, c: "pink", title: "19:00 — 20:00", sub: "Baan 1 · Jan · 8 max" },
    { d: 3, s: 16, dur: 1, c: "blue", title: "16:00 — 17:00", sub: "Baan 3 · Tom · 4 max" },
    { d: 3, s: 17.5, dur: 1, c: "amber", title: "17:30 — 18:30", sub: "Baan 2 · Lies · 4 max" },
    { d: 4, s: 17, dur: 1, c: "violet", title: "17:00 — 18:00", sub: "Baan 1 · Jan · 6 max" },
    { d: 4, s: 18.5, dur: 1.5, c: "teal", title: "18:30 — 20:00", sub: "Baan 2 · Piet · 8 max" },
    { d: 5, s: 10, dur: 1, c: "pink", title: "10:00 — 11:00", sub: "Baan 2 · Lies · 4 max" },
    { d: 5, s: 11, dur: 1.5, c: "blue", title: "11:00 — 12:30", sub: "Baan 1 · Tom · 6 max" },
  ];
  const HOUR_H = 32;
  const startH = HOURS[0];

  return (
    <Shell active="Lessen">
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 16 }}>
        <div>
          <p style={{ margin: 0, fontSize: 11, color: GREEN, fontWeight: 600 }}>LESREEKS</p>
          <h1 style={{ fontSize: 20, fontWeight: 700, color: "#111827", margin: "2px 0 0" }}>Padel Beginners — Dinsdagavond</h1>
          <p style={{ fontSize: 12, color: "#9ca3af", margin: "2px 0 0" }}>Week 1 van 12 · 01 – 07 apr 2026</p>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button style={{ padding: "7px 12px", border: "1px solid #e5e7eb", borderRadius: 8, background: "#fff", fontSize: 11.5, color: "#6b7280" }}>
            ← Vorige week
          </button>
          <button style={{ padding: "7px 12px", border: "1px solid #e5e7eb", borderRadius: 8, background: "#fff", fontSize: 11.5, color: "#6b7280" }}>
            Volgende week →
          </button>
        </div>
      </div>

      <div style={{ background: "#fff", borderRadius: 12, border: "1px solid #e5e7eb", boxShadow: "0 1px 2px rgba(0,0,0,.03)", overflow: "hidden" }}>
        {/* Day headers */}
        <div style={{ display: "grid", gridTemplateColumns: "44px repeat(7, 1fr)", background: "#f9fafb", borderBottom: "1px solid #e5e7eb" }}>
          <div />
          {DAYS.map((d, i) => (
            <div key={i} style={{ padding: "10px 6px", textAlign: "center", borderLeft: "1px solid #e5e7eb" }}>
              <div style={{ fontSize: 10.5, fontWeight: 700, color: "#111827", textTransform: "uppercase" }}>{d}</div>
              <div style={{ fontSize: 9.5, color: "#9ca3af", marginTop: 2 }}>{["01","02","03","04","05","06","07"][i]} apr</div>
            </div>
          ))}
        </div>
        {/* Grid body */}
        <div style={{ position: "relative", display: "grid", gridTemplateColumns: "44px repeat(7, 1fr)", height: HOUR_H * HOURS.length }}>
          {/* Hour axis */}
          <div style={{ position: "relative" }}>
            {HOURS.map((h, i) => (
              <div key={i} style={{ position: "absolute", top: i * HOUR_H, right: 6, fontSize: 9, color: "#9ca3af", fontWeight: 500 }}>
                {String(h).padStart(2, "0")}:00
              </div>
            ))}
          </div>
          {DAYS.map((_, di) => (
            <div key={di} style={{ position: "relative", borderLeft: "1px solid #e5e7eb" }}>
              {HOURS.map((h, hi) => (
                <React.Fragment key={hi}>
                  <div style={{ position: "absolute", left: 0, right: 0, top: hi * HOUR_H, height: HOUR_H / 2, borderBottom: "1px dashed #e5e7eb" }} />
                  <div style={{ position: "absolute", left: 0, right: 0, top: hi * HOUR_H + HOUR_H / 2, height: HOUR_H / 2, borderBottom: "1px solid #e5e7eb" }} />
                </React.Fragment>
              ))}
              {slots.filter((s) => s.d === di).map((s, i) => {
                const c = COL[s.c];
                return (
                  <div key={i} style={{
                    position: "absolute",
                    top: (s.s - startH) * HOUR_H + 1,
                    height: s.dur * HOUR_H - 3,
                    left: 2, right: 2,
                    background: c.bg,
                    borderLeft: `3px solid ${c.bd}`,
                    borderRadius: "0 4px 4px 0",
                    padding: "3px 5px",
                    fontSize: 8.5,
                    overflow: "hidden",
                  }}>
                    <div style={{ fontWeight: 600, color: c.tx, lineHeight: 1.1 }}>{s.title}</div>
                    <div style={{ color: "#4b5563", lineHeight: 1.2, marginTop: 2, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{s.sub}</div>
                  </div>
                );
              })}
            </div>
          ))}
        </div>
      </div>
    </Shell>
  );
};

// ── Screen: Student portal (current) ───────────────────────────────────────
const StudentPortalCurrent = () => {
  const lessons = [
    { series: "Padel Beginners — Dinsdagavond", day: "dinsdag 18:00 — 19:00", court: "Baan 3", trainer: "Jan Peeters", status: "Confirmed" },
    { series: "Padel Beginners — Dinsdagavond", day: "dinsdag 18:00 — 19:00", court: "Baan 3", trainer: "Jan Peeters", status: "AwaitingConfirmation" },
    { series: "Padel Beginners — Dinsdagavond", day: "dinsdag 18:00 — 19:00", court: "Baan 3", trainer: "Jan Peeters", status: "Proposed" },
  ];
  const badge = (s) => {
    const styles = {
      Confirmed: { bg: "rgba(208,255,20,.2)", tx: GREEN },
      AwaitingConfirmation: { bg: "#fef3c7", tx: "#92400e" },
      Proposed: { bg: "#dbeafe", tx: "#1e40af" },
    }[s];
    return <span style={{ padding: "4px 10px", borderRadius: 999, fontSize: 10.5, fontWeight: 600, background: styles.bg, color: styles.tx }}>{s}</span>;
  };
  return (
    <div style={{ width: "100%", height: "100%", background: CANVAS_BG, padding: "32px 36px", fontFamily: "'Inter', system-ui, sans-serif" }}>
      <div style={{ maxWidth: 640, margin: "0 auto" }}>
        <h1 style={{ fontSize: 22, fontWeight: 700, color: "#111827", margin: 0 }}>Mijn lessen</h1>
        <p style={{ fontSize: 12.5, color: "#6b7280", margin: "4px 0 20px" }}>Overzicht van je ingeplande lessen.</p>
        <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          {lessons.map((l, i) => (
            <div key={i} style={{ background: "#fff", borderRadius: 10, padding: "14px 18px", borderLeft: `4px solid ${LIME}`, boxShadow: "0 1px 2px rgba(0,0,0,.03)", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div>
                <p style={{ margin: 0, fontSize: 13, fontWeight: 600, color: "#111827" }}>{l.series}</p>
                <p style={{ margin: "2px 0 0", fontSize: 11.5, color: "#6b7280" }}>{l.day} · {l.court}</p>
                <p style={{ margin: "2px 0 0", fontSize: 10.5, color: "#9ca3af" }}>{l.trainer}</p>
              </div>
              {badge(l.status)}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

// ── Screen: Login (current) ────────────────────────────────────────────────
const LoginCurrent = () => (
  <div style={{ display: "flex", width: "100%", height: "100%", fontFamily: "'Inter', system-ui, sans-serif" }}>
    <div style={{ width: "52%", background: GREEN, position: "relative", overflow: "hidden", padding: 48, display: "flex", flexDirection: "column", justifyContent: "space-between", color: "#fff" }}>
      <CourtLines opacity={0.07} />
      <div style={{ position: "relative", display: "flex", alignItems: "center", gap: 12 }}>
        <div style={{ width: 36, height: 36, borderRadius: 999, background: LIME, display: "grid", placeItems: "center" }}>
          <I.ball style={{ width: 22, height: 22 }} />
        </div>
        <span style={{ fontSize: 22, fontWeight: 700 }}>Coach<span style={{ color: LIME }}>OS</span></span>
      </div>
      <div style={{ position: "relative" }}>
        <p style={{ fontSize: 10.5, color: LIME, fontWeight: 600, letterSpacing: "0.15em", textTransform: "uppercase", margin: 0, opacity: 0.8 }}>
          Tennis &amp; Padel
        </p>
        <h1 style={{ fontSize: 38, fontWeight: 800, lineHeight: 1.1, margin: "14px 0 12px", letterSpacing: -0.8 }}>
          Plan lessen.<br />
          <span style={{ color: LIME }}>Vul banen.</span><br />
          Groei je club.
        </h1>
        <p style={{ fontSize: 13, color: "rgba(255,255,255,.6)", maxWidth: 260, lineHeight: 1.6, margin: 0 }}>
          Dé planningstool voor tennis- en padelclubs in de Benelux.
        </p>
      </div>
      <div style={{ position: "relative", display: "flex", flexDirection: "column", gap: 10 }}>
        {["Gekoppelde inschrijvingen", "Automatische bevestigingsflow", "Volg je leerlingen"].map((f) => (
          <div key={f} style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <div style={{ width: 16, height: 16, borderRadius: 999, background: "rgba(208,255,20,.2)", display: "grid", placeItems: "center" }}>
              <div style={{ width: 5, height: 5, borderRadius: 999, background: LIME }} />
            </div>
            <span style={{ fontSize: 11.5, color: "rgba(255,255,255,.7)" }}>{f}</span>
          </div>
        ))}
      </div>
    </div>
    <div style={{ width: "48%", background: "#FAFAF8", display: "grid", placeItems: "center", padding: 28 }}>
      <div style={{ width: "100%", maxWidth: 320 }}>
        <h2 style={{ fontSize: 22, fontWeight: 700, color: "#111827", margin: "0 0 4px", letterSpacing: -0.4 }}>Inloggen</h2>
        <p style={{ fontSize: 12.5, color: "#9ca3af", margin: 0 }}>Welkom terug.</p>
        <div style={{ marginTop: 28, display: "flex", flexDirection: "column", gap: 16 }}>
          <div>
            <p style={{ margin: "0 0 6px", fontSize: 11.5, color: "#6b7280", fontWeight: 500 }}>E-mailadres</p>
            <div style={{ height: 38, border: "1px solid #e5e7eb", borderRadius: 8, background: "#fff", padding: "0 12px", display: "flex", alignItems: "center", fontSize: 12, color: "#d1d5db" }}>
              jouw@email.be
            </div>
          </div>
          <div>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <p style={{ margin: "0 0 6px", fontSize: 11.5, color: "#6b7280", fontWeight: 500 }}>Wachtwoord</p>
              <p style={{ margin: "0 0 6px", fontSize: 10.5, color: GREEN, fontWeight: 500 }}>Vergeten?</p>
            </div>
            <div style={{ height: 38, border: "1px solid #e5e7eb", borderRadius: 8, background: "#fff", padding: "0 12px", display: "flex", alignItems: "center", fontSize: 12, color: "#d1d5db" }}>
              ••••••••
            </div>
          </div>
          <button style={{ height: 40, background: GREEN, color: "#fff", fontWeight: 600, fontSize: 12.5, border: "none", borderRadius: 8, marginTop: 6 }}>Inloggen</button>
          <p style={{ margin: "14px 0 0", textAlign: "center", fontSize: 11.5, color: "#9ca3af" }}>
            Nog geen account? <span style={{ color: GREEN, fontWeight: 600 }}>Registreren</span>
          </p>
        </div>
      </div>
    </div>
  </div>
);

Object.assign(window, {
  DashboardCurrent, LessonsCurrent, TrainersCurrent,
  CalendarCurrent, StudentPortalCurrent, LoginCurrent,
  GREEN, LIME, BEIGE, CANVAS_BG, I, CourtLines, Shell, Sidebar, Topbar,
});
