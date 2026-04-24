// Additional "Current" screens + state variations the critique was missing.
// Pulled directly from coach-os source: register, student-login, settings, enroll,
// confirmation, and the step-3 "Plan bevestigen" wizard flow with its button states.

// NOTE: relies on globals exported by screens-current.jsx —
//   GREEN, LIME, BEIGE, CANVAS_BG, I, CourtLines, Shell, Sidebar, Topbar
// Load order is enforced by script tag sequence in the HTML.

// ── Shared scaffolding (marketing split) ───────────────────────────────────

const AuthSplit = ({ kicker, h1Line1, h1Line2, h1Line3, body, features, children, rightTitle, rightSub }) => (
  <div style={{ width: "100%", height: "100%", display: "flex", background: "#FAFAF8", overflow: "hidden" }}>
    <div style={{ width: "52%", background: GREEN, color: "#fff", padding: 36, display: "flex", flexDirection: "column", justifyContent: "space-between", position: "relative" }}>
      <CourtLines />
      <div style={{ position: "relative", display: "flex", alignItems: "center", gap: 10 }}>
        <div style={{ width: 32, height: 32, borderRadius: 999, background: LIME, display: "grid", placeItems: "center" }}>
          <I.ball width={18} height={18} />
        </div>
        <span style={{ fontSize: 19, fontWeight: 700, letterSpacing: -0.3 }}>
          Coach<span style={{ color: LIME }}>OS</span>
        </span>
      </div>
      <div style={{ position: "relative" }}>
        <p style={{ fontSize: 10.5, color: LIME, fontWeight: 600, letterSpacing: "0.15em", textTransform: "uppercase", margin: 0, opacity: 0.8 }}>
          {kicker}
        </p>
        <h1 style={{ fontSize: 38, fontWeight: 800, lineHeight: 1.1, margin: "14px 0 12px", letterSpacing: -0.8 }}>
          {h1Line1}<br /><span style={{ color: LIME }}>{h1Line2}</span>{h1Line3 && (<><br />{h1Line3}</>)}
        </h1>
        <p style={{ fontSize: 13, color: "rgba(255,255,255,.6)", maxWidth: 260, lineHeight: 1.6, margin: 0 }}>{body}</p>
      </div>
      <div style={{ position: "relative", display: "flex", flexDirection: "column", gap: 10 }}>
        {features.map((f) => (
          <div key={f} style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <div style={{ width: 16, height: 16, borderRadius: 999, background: "rgba(208,255,20,.2)", display: "grid", placeItems: "center" }}>
              <div style={{ width: 5, height: 5, borderRadius: 999, background: LIME }} />
            </div>
            <span style={{ fontSize: 11.5, color: "rgba(255,255,255,.7)" }}>{f}</span>
          </div>
        ))}
      </div>
    </div>
    <div style={{ width: "48%", display: "grid", placeItems: "center", padding: 28, overflow: "auto" }}>
      <div style={{ width: "100%", maxWidth: 320 }}>
        <h2 style={{ fontSize: 22, fontWeight: 700, color: "#111827", margin: "0 0 4px", letterSpacing: -0.4 }}>{rightTitle}</h2>
        <p style={{ fontSize: 12.5, color: "#9ca3af", margin: 0 }}>{rightSub}</p>
        <div style={{ marginTop: 24 }}>{children}</div>
      </div>
    </div>
  </div>
);

const FormLabel = ({ children }) => (
  <p style={{ margin: "0 0 6px", fontSize: 11.5, color: "#6b7280", fontWeight: 500 }}>{children}</p>
);
const FormInput = ({ placeholder, value }) => (
  <div style={{ height: 38, border: "1px solid #e5e7eb", borderRadius: 8, background: "#fff", padding: "0 12px", display: "flex", alignItems: "center", fontSize: 12, color: value ? "#111827" : "#d1d5db" }}>
    {value || placeholder}
  </div>
);
const PrimaryBtn = ({ children, full = true }) => (
  <button style={{ height: 40, width: full ? "100%" : "auto", padding: full ? 0 : "0 18px", background: GREEN, color: "#fff", fontWeight: 600, fontSize: 12.5, border: "none", borderRadius: 8 }}>
    {children}
  </button>
);

// ── REGISTER ───────────────────────────────────────────────────────────────

const RegisterCurrent = () => (
  <AuthSplit
    kicker="Tennis &amp; Padel"
    h1Line1="Plan lessen."
    h1Line2="Vul banen."
    h1Line3="Groei je club."
    body="Maak een account en begin met inschrijvingen binnen een uur."
    features={["Gekoppelde inschrijvingen", "Automatische bevestigingsflow", "Volg je leerlingen"]}
    rightTitle="Account aanmaken"
    rightSub="Start met je eigen tennisclub."
  >
    <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
      <div><FormLabel>Naam organisatie</FormLabel><FormInput placeholder="TC Brederode" /></div>
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10 }}>
        <div><FormLabel>Voornaam</FormLabel><FormInput placeholder="Jan" /></div>
        <div><FormLabel>Achternaam</FormLabel><FormInput placeholder="Janssen" /></div>
      </div>
      <div><FormLabel>E-mailadres</FormLabel><FormInput placeholder="jouw@email.be" /></div>
      <div><FormLabel>Wachtwoord</FormLabel><FormInput placeholder="••••••••" /></div>
      <div><FormLabel>Bevestig wachtwoord</FormLabel><FormInput placeholder="••••••••" /></div>
      <div style={{ marginTop: 4 }}><PrimaryBtn>Account aanmaken</PrimaryBtn></div>
      <p style={{ margin: "10px 0 0", textAlign: "center", fontSize: 11.5, color: "#9ca3af" }}>
        Heb je al een account? <span style={{ color: GREEN, fontWeight: 600 }}>Inloggen</span>
      </p>
    </div>
  </AuthSplit>
);

// ── STUDENT LOGIN (magic link) ─────────────────────────────────────────────

const StudentLoginCurrent = () => (
  <div style={{ width: "100%", height: "100%", background: "#FAFAF8", display: "grid", placeItems: "center" }}>
    <div style={{ width: 420, padding: 32, background: "#fff", border: "1px solid #eee", borderRadius: 14, boxShadow: "0 2px 10px rgba(0,0,0,0.04)" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 24 }}>
        <div style={{ width: 28, height: 28, borderRadius: 999, background: GREEN, display: "grid", placeItems: "center" }}>
          <I.ball width={14} height={14} />
        </div>
        <span style={{ fontSize: 16, fontWeight: 700, color: GREEN }}>CoachOS</span>
      </div>
      <h2 style={{ fontSize: 20, fontWeight: 700, margin: "0 0 4px" }}>Welkom terug</h2>
      <p style={{ fontSize: 12.5, color: "#6b7280", margin: "0 0 22px" }}>
        Vul je e-mailadres in. We sturen een inloglink.
      </p>
      <FormLabel>E-mailadres</FormLabel>
      <FormInput placeholder="jouw@email.be" />
      <div style={{ marginTop: 14 }}><PrimaryBtn>Stuur inloglink</PrimaryBtn></div>
      <p style={{ margin: "18px 0 0", fontSize: 11, color: "#9ca3af", textAlign: "center" }}>
        Alleen voor leerlingen. Trainer? <span style={{ color: GREEN, fontWeight: 600 }}>Inloggen</span>
      </p>
    </div>
  </div>
);

// ── SETTINGS (tennis clubs) ────────────────────────────────────────────────

const SettingsCurrent = () => (
  <Shell active="settings">
    <div style={{ maxWidth: 660, margin: "0 auto" }}>
      <h1 style={{ fontSize: 22, fontWeight: 700, margin: "0 0 4px", letterSpacing: -0.4 }}>Instellingen</h1>
      <p style={{ fontSize: 12.5, color: "#9ca3af", margin: "0 0 22px" }}>Beheer de basisinstellingen van je organisatie.</p>

      <div style={{ background: "#fff", borderRadius: 12, boxShadow: "0 1px 2px rgba(0,0,0,.03)", overflow: "hidden" }}>
        <div style={{ padding: "14px 22px", borderBottom: "1px solid #f3f4f6", display: "flex", alignItems: "center", gap: 10 }}>
          <div style={{ width: 26, height: 26, borderRadius: 7, background: "rgba(45,80,22,0.08)", display: "grid", placeItems: "center", color: GREEN }}>
            <svg viewBox="0 0 24 24" width="13" height="13" {...iconStroke}><rect x="3" y="3" width="18" height="18" rx="2" /><path d="M9 22V12h6v10" /></svg>
          </div>
          <div>
            <p style={{ fontSize: 12.5, fontWeight: 700, margin: 0, color: "#1f2937" }}>Tennisclubs</p>
            <p style={{ fontSize: 10.5, color: "#9ca3af", margin: 0 }}>Locaties waar lessen worden gegeven</p>
          </div>
        </div>

        <div style={{ padding: 18, display: "flex", flexDirection: "column", gap: 8 }}>
          {[
            { name: "TC Brederode", addr: "Brederodestraat 5, Haarlem" },
            { name: "Padel Park Antwerpen", addr: "Plantin en Moretuslei 220" },
          ].map((c) => (
            <div key={c.name} style={{ display: "flex", alignItems: "flex-start", gap: 12, padding: 14, borderRadius: 10, border: "1px solid #f3f4f6", background: "#fff" }}>
              <div style={{ width: 28, height: 28, borderRadius: 7, background: "rgba(45,80,22,0.08)", display: "grid", placeItems: "center", color: GREEN, flexShrink: 0 }}>
                <svg viewBox="0 0 24 24" width="13" height="13" {...iconStroke}><rect x="3" y="3" width="18" height="18" rx="2" /><path d="M9 22V12h6v10" /></svg>
              </div>
              <div style={{ flex: 1 }}>
                <p style={{ margin: 0, fontSize: 12.5, fontWeight: 600, color: "#1f2937" }}>{c.name}</p>
                <div style={{ display: "flex", alignItems: "center", gap: 4, marginTop: 2 }}>
                  <svg viewBox="0 0 24 24" width="11" height="11" {...iconStroke} style={{ color: "#9ca3af" }}><path d="M21 10c0 7-9 13-9 13S3 17 3 10a9 9 0 0118 0z" /><circle cx="12" cy="10" r="3" /></svg>
                  <p style={{ margin: 0, fontSize: 11.5, color: "#6b7280" }}>{c.addr}</p>
                </div>
              </div>
              <button style={{ width: 26, height: 26, borderRadius: 7, border: "none", background: "transparent", color: "#d1d5db" }}>
                <I.trash width={13} height={13} />
              </button>
            </div>
          ))}
        </div>

        <div style={{ height: 1, background: "#f3f4f6", margin: "0 18px" }} />

        <div style={{ padding: 18 }}>
          <p style={{ fontSize: 10.5, fontWeight: 700, letterSpacing: "0.08em", textTransform: "uppercase", color: "#6b7280", margin: "0 0 12px" }}>
            Nieuwe club toevoegen
          </p>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10, marginBottom: 10 }}>
            <div><FormLabel>Naam *</FormLabel><FormInput placeholder="TC Brederode" /></div>
            <div><FormLabel>Adres *</FormLabel><FormInput placeholder="Brederodestraat 5, Haarlem" /></div>
          </div>
          <button style={{ display: "inline-flex", alignItems: "center", gap: 6, padding: "8px 14px", background: GREEN, color: "#fff", fontSize: 11, fontWeight: 700, border: "none", borderRadius: 8 }}>
            <I.plus width={12} height={12} /> Toevoegen
          </button>
        </div>
      </div>
    </div>
  </Shell>
);

// Helper we need since `iconStroke` is in the other file — recreate locally for the <svg> inline above.
const iconStroke = { fill: "none", stroke: "currentColor", strokeWidth: 1.75, strokeLinecap: "round", strokeLinejoin: "round" };

// ── SETTINGS: delete-confirm dialog STATE ──────────────────────────────────

const SettingsDeleteState = () => (
  <div style={{ position: "relative", width: "100%", height: "100%" }}>
    <SettingsCurrent />
    <div style={{ position: "absolute", inset: 0, background: "rgba(17,24,39,.55)", display: "grid", placeItems: "center" }}>
      <div style={{ width: 400, background: "#fff", borderRadius: 12, padding: 22, boxShadow: "0 16px 50px rgba(0,0,0,.25)" }}>
        <p style={{ fontSize: 14, fontWeight: 700, margin: "0 0 6px" }}>Club verwijderen?</p>
        <p style={{ fontSize: 12.5, color: "#6b7280", margin: "0 0 18px", lineHeight: 1.55 }}>
          <strong>“TC Brederode”</strong> wordt permanent verwijderd. Dit kan niet ongedaan worden gemaakt.
        </p>
        <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
          <button style={{ padding: "8px 14px", fontSize: 11.5, fontWeight: 600, color: "#374151", background: "#fff", border: "1px solid #e5e7eb", borderRadius: 8 }}>Annuleren</button>
          <button style={{ padding: "8px 14px", fontSize: 11.5, fontWeight: 700, color: "#fff", background: "#dc2626", border: "none", borderRadius: 8 }}>Verwijderen</button>
        </div>
      </div>
    </div>
  </div>
);

// ── ENROLL (public) form ───────────────────────────────────────────────────

const DAY_NAMES = ["Ma","Di","Wo","Do","Vr","Za","Zo"];

const PrefDot = ({ color, icon, active }) => (
  <div style={{ width: 26, height: 26, borderRadius: 999, border: `2px solid ${active ? color : "#e5e7eb"}`, background: active ? color : "transparent", display: "grid", placeItems: "center" }}>
    {icon === "check" && active && <svg viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="#fff" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12l5 5L20 7" /></svg>}
    {icon === "x" && active && <svg viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="#fff" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><path d="M6 6l12 12M18 6L6 18" /></svg>}
  </div>
);

const EnrollShell = ({ children }) => (
  <div style={{ width: "100%", height: "100%", background: "#FAFAF8", overflow: "auto" }}>
    <div style={{ maxWidth: 680, margin: "0 auto", padding: "28px 16px" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 22 }}>
        <div style={{ width: 28, height: 28, borderRadius: 999, background: GREEN, display: "grid", placeItems: "center" }}>
          <I.ball width={14} height={14} />
        </div>
        <span style={{ fontSize: 16, fontWeight: 700, color: GREEN }}>CoachOS</span>
      </div>
      {children}
      <p style={{ textAlign: "center", fontSize: 10.5, color: "#9ca3af", marginTop: 20 }}>Powered by CoachOS</p>
    </div>
  </div>
);

const EnrollCurrent = () => (
  <EnrollShell>
    {/* Series card */}
    <div style={{ background: "#fff", borderRadius: 12, border: "1px solid #eee", padding: 22, marginBottom: 16 }}>
      <span style={{ display: "inline-block", padding: "3px 8px", background: "rgba(208,255,20,0.25)", color: GREEN, fontSize: 10.5, fontWeight: 700, borderRadius: 999, marginBottom: 8 }}>
        Gevorderd
      </span>
      <h1 style={{ fontSize: 20, fontWeight: 800, margin: "0 0 6px", letterSpacing: -0.3 }}>Padel — Voorjaar 2026</h1>
      <p style={{ fontSize: 12.5, color: "#6b7280", margin: "0 0 14px", lineHeight: 1.55 }}>
        10 weken intensieve padel-coaching voor spelers met basisniveau+. Focus op verdedigend spel en glasgebruik.
      </p>
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10, fontSize: 12, color: "#6b7280" }}>
        {[
          ["cal", "01/04/2026 – 30/06/2026"],
          ["loc", "Padel Park Antwerpen"],
          ["eur", "€320,00 per reeks"],
          ["usr", "18 ingeschreven"],
        ].map(([i, v]) => (
          <div key={v} style={{ display: "flex", gap: 6, alignItems: "center" }}>
            <div style={{ width: 14, height: 14, color: "#9ca3af" }}>
              {i === "cal" && <I.cal width={14} height={14} />}
              {i === "loc" && <svg viewBox="0 0 24 24" width="14" height="14" {...iconStroke}><path d="M21 10c0 7-9 13-9 13S3 17 3 10a9 9 0 0118 0z" /><circle cx="12" cy="10" r="3" /></svg>}
              {i === "eur" && <I.euro width={14} height={14} />}
              {i === "usr" && <I.users width={14} height={14} />}
            </div>
            <span>{v}</span>
          </div>
        ))}
      </div>
    </div>

    {/* Form */}
    <div style={{ background: "#fff", borderRadius: 12, border: "1px solid #eee", overflow: "hidden" }}>
      <div style={{ padding: "16px 22px", borderBottom: "1px solid #f3f4f6" }}>
        <h2 style={{ fontSize: 15, fontWeight: 700, margin: 0 }}>Inschrijving</h2>
        <p style={{ fontSize: 11.5, color: "#9ca3af", margin: "2px 0 0" }}>Vul je gegevens in en kies je beschikbaarheid</p>
      </div>

      <div style={{ padding: 22, display: "flex", flexDirection: "column", gap: 20 }}>
        <div>
          <p style={{ fontSize: 10.5, fontWeight: 700, letterSpacing: "0.08em", textTransform: "uppercase", color: "#6b7280", margin: "0 0 10px" }}>Persoonlijke gegevens</p>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10, marginBottom: 10 }}>
            <div><FormLabel>Voornaam *</FormLabel><FormInput placeholder="Jan" /></div>
            <div><FormLabel>Achternaam *</FormLabel><FormInput placeholder="Janssen" /></div>
          </div>
          <div style={{ marginBottom: 10 }}><FormLabel>E-mailadres *</FormLabel><FormInput placeholder="jouw@email.be" /></div>
          <div><FormLabel>Telefoon</FormLabel><FormInput placeholder="+32 471 23 45 67" /></div>
        </div>

        <div style={{ height: 1, background: "#f3f4f6" }} />

        <div>
          <p style={{ fontSize: 10.5, fontWeight: 700, letterSpacing: "0.08em", textTransform: "uppercase", color: "#6b7280", margin: "0 0 10px" }}>Type inschrijving</p>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10 }}>
            <div style={{ padding: 14, border: `2px solid ${GREEN}`, borderRadius: 10, background: "rgba(45,80,22,.04)", display: "flex", gap: 10, alignItems: "center" }}>
              <div style={{ width: 34, height: 34, borderRadius: 999, background: "#f3f4f6", display: "grid", placeItems: "center", color: "#6b7280" }}>
                <svg viewBox="0 0 24 24" width="16" height="16" {...iconStroke}><circle cx="12" cy="7" r="4" /><path d="M4 21v-1a6 6 0 016-6h4a6 6 0 016 6v1" /></svg>
              </div>
              <div><p style={{ margin: 0, fontSize: 12.5, fontWeight: 600 }}>Individueel</p><p style={{ margin: "1px 0 0", fontSize: 10.5, color: "#9ca3af" }}>Ik schrijf mezelf in</p></div>
            </div>
            <div style={{ padding: 14, border: "2px solid #e5e7eb", borderRadius: 10, display: "flex", gap: 10, alignItems: "center" }}>
              <div style={{ width: 34, height: 34, borderRadius: 999, background: "#f3f4f6", display: "grid", placeItems: "center", color: "#6b7280" }}>
                <I.users width={16} height={16} />
              </div>
              <div><p style={{ margin: 0, fontSize: 12.5, fontWeight: 600 }}>Groep</p><p style={{ margin: "1px 0 0", fontSize: 10.5, color: "#9ca3af" }}>Meerdere personen</p></div>
            </div>
          </div>
        </div>

        <div style={{ height: 1, background: "#f3f4f6" }} />

        {/* Availability grid */}
        <div>
          <p style={{ fontSize: 10.5, fontWeight: 700, letterSpacing: "0.08em", textTransform: "uppercase", color: "#6b7280", margin: "0 0 4px" }}>Beschikbaarheid</p>
          <p style={{ fontSize: 11, color: "#9ca3af", margin: "0 0 12px" }}>Markeer per slot of je voorkeur, beschikbaar of niet bent.</p>
          <div style={{ border: "1px solid #e5e7eb", borderRadius: 10, overflow: "hidden" }}>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 72px 72px 72px", background: "#f9fafb", borderBottom: "1px solid #e5e7eb" }}>
              <div style={{ padding: "8px 12px", fontSize: 10, fontWeight: 700, color: "#6b7280", textTransform: "uppercase" }}>Tijdslot</div>
              <div style={{ padding: "8px 6px", fontSize: 10, fontWeight: 700, color: "#15803d", textTransform: "uppercase", textAlign: "center" }}>Voorkeur</div>
              <div style={{ padding: "8px 6px", fontSize: 10, fontWeight: 700, color: "#1d4ed8", textTransform: "uppercase", textAlign: "center" }}>Kan</div>
              <div style={{ padding: "8px 6px", fontSize: 10, fontWeight: 700, color: "#6b7280", textTransform: "uppercase", textAlign: "center" }}>Niet</div>
            </div>
            {[
              { day: "Maandag", slots: [["18:00 — 19:00", "Baan 1", "pref"], ["19:00 — 20:00", "Baan 1", ""]] },
              { day: "Woensdag", slots: [["17:00 — 18:00", "Baan 2", "avail"]] },
              { day: "Zaterdag", slots: [["10:00 — 11:00", "Baan 1", "unavail"]] },
            ].map((g, gi) => (
              <div key={g.day} style={{ borderBottom: gi < 2 ? "1px solid #e5e7eb" : "none" }}>
                <div style={{ padding: "6px 12px", background: "rgba(249,250,251,.7)", borderBottom: "1px solid #f3f4f6", fontSize: 11, fontWeight: 700, color: "#374151" }}>{g.day}</div>
                {g.slots.map(([time, court, pref], si) => (
                  <div key={time} style={{ display: "grid", gridTemplateColumns: "1fr 72px 72px 72px", alignItems: "center", borderBottom: si < g.slots.length - 1 ? "1px solid #f3f4f6" : "none" }}>
                    <div style={{ padding: "10px 12px" }}>
                      <p style={{ margin: 0, fontSize: 12, fontWeight: 600, color: "#111827" }}>{time}</p>
                      <p style={{ margin: 0, fontSize: 11, color: "#9ca3af" }}>{court}</p>
                    </div>
                    <div style={{ display: "grid", placeItems: "center" }}><PrefDot color="#22c55e" icon="check" active={pref === "pref"} /></div>
                    <div style={{ display: "grid", placeItems: "center" }}><PrefDot color="#3b82f6" icon="check" active={pref === "avail"} /></div>
                    <div style={{ display: "grid", placeItems: "center" }}><PrefDot color="#9ca3af" icon="x" active={pref === "unavail"} /></div>
                  </div>
                ))}
              </div>
            ))}
          </div>
        </div>
      </div>

      <div style={{ padding: 22, background: "#f9fafb", borderTop: "1px solid #f3f4f6" }}>
        <PrimaryBtn>Inschrijving versturen</PrimaryBtn>
        <p style={{ margin: "10px 0 0", textAlign: "center", fontSize: 10.5, color: "#9ca3af" }}>Je ontvangt een bevestiging per e-mail</p>
      </div>
    </div>
  </EnrollShell>
);

// ── ENROLL — submitted STATE ───────────────────────────────────────────────

const SuccessCard = ({ title, body }) => (
  <div style={{ background: "#fff", borderRadius: 12, border: "1px solid #eee", padding: "56px 24px", textAlign: "center" }}>
    <div style={{ width: 52, height: 52, borderRadius: 999, background: "#dcfce7", margin: "0 auto 16px", display: "grid", placeItems: "center", color: "#16a34a" }}>
      <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10" /><path d="M8 12l3 3 5-6" /></svg>
    </div>
    <h2 style={{ fontSize: 16, fontWeight: 700, margin: "0 0 6px" }}>{title}</h2>
    <p style={{ fontSize: 12.5, color: "#6b7280", margin: 0, lineHeight: 1.55, maxWidth: 360, marginLeft: "auto", marginRight: "auto" }}>{body}</p>
  </div>
);

const EnrollSubmittedState = () => (
  <EnrollShell>
    <SuccessCard title="Inschrijving ontvangen" body="Bedankt! Je krijgt binnen 24 uur een e-mail zodra je een plek toegewezen krijgt." />
  </EnrollShell>
);

// ── CONFIRMATION — main view ───────────────────────────────────────────────

const ConfShell = ({ children }) => (
  <div style={{ width: "100%", height: "100%", background: "#FAFAF8", overflow: "auto" }}>
    <div style={{ maxWidth: 520, margin: "0 auto", padding: "32px 16px" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 26 }}>
        <div style={{ width: 28, height: 28, borderRadius: 999, background: GREEN, display: "grid", placeItems: "center" }}>
          <I.ball width={14} height={14} />
        </div>
        <span style={{ fontSize: 16, fontWeight: 700, color: GREEN }}>CoachOS</span>
      </div>
      <div style={{ background: "#fff", borderRadius: 12, border: "1px solid #eee", boxShadow: "0 1px 3px rgba(0,0,0,0.04)", overflow: "hidden" }}>
        {children}
      </div>
      <p style={{ textAlign: "center", fontSize: 10.5, color: "#9ca3af", marginTop: 20 }}>Powered by CoachOS</p>
    </div>
  </div>
);

const ConfirmationCurrent = () => (
  <ConfShell>
    <div style={{ padding: 24 }}>
      <h2 style={{ fontSize: 16, fontWeight: 700, margin: "0 0 16px" }}>Padel — Voorjaar 2026</h2>

      <div style={{ background: "#FAFAF8", padding: 16, borderRadius: 10, marginBottom: 14 }}>
        <p style={{ fontSize: 9.5, fontWeight: 700, letterSpacing: "0.1em", textTransform: "uppercase", color: "#6b7280", margin: "0 0 10px" }}>Jouw slot</p>
        {[
          ["cal", "Maandag 18:00 — 19:00"],
          ["loc", "Baan 1 · Padel Park Antwerpen"],
          ["eur", "€32,00 per persoon"],
          ["clk", "Deze link verloopt op 28/04 om 18:00"],
        ].map(([i, v]) => (
          <div key={v} style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 12, color: i === "clk" ? "#9ca3af" : "#374151", marginBottom: 6 }}>
            <span style={{ color: i === "clk" ? "#d1d5db" : GREEN, display: "inline-flex" }}>
              {i === "cal" && <I.cal width={13} height={13} />}
              {i === "loc" && <svg viewBox="0 0 24 24" width="13" height="13" {...iconStroke}><path d="M21 10c0 7-9 13-9 13S3 17 3 10a9 9 0 0118 0z" /><circle cx="12" cy="10" r="3" /></svg>}
              {i === "eur" && <I.euro width={13} height={13} />}
              {i === "clk" && <I.clock width={11} height={11} />}
            </span>
            <span style={{ fontSize: i === "clk" ? 10.5 : 12 }}>{v}</span>
          </div>
        ))}
      </div>

      <div style={{ display: "flex", gap: 10 }}>
        <button style={{ flex: 1, height: 40, background: GREEN, color: "#fff", fontWeight: 600, fontSize: 12.5, border: "none", borderRadius: 8 }}>Plan bevestigen</button>
        <button style={{ flex: 1, height: 40, background: "#fff", color: "#374151", fontWeight: 600, fontSize: 12.5, border: "1px solid #d1d5db", borderRadius: 8 }}>Weigeren</button>
      </div>
    </div>
  </ConfShell>
);

// ── CONFIRMATION STATES ────────────────────────────────────────────────────

const ConfirmationPaymentState = () => (
  <ConfShell>
    <div style={{ padding: 24 }}>
      <h2 style={{ fontSize: 16, fontWeight: 700, margin: "0 0 16px" }}>Padel — Voorjaar 2026</h2>

      <div style={{ background: "#FAFAF8", padding: 16, borderRadius: 10, marginBottom: 14 }}>
        <p style={{ fontSize: 9.5, fontWeight: 700, letterSpacing: "0.1em", textTransform: "uppercase", color: "#6b7280", margin: "0 0 10px" }}>Jouw slot</p>
        <div style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 12, color: "#374151" }}>
          <span style={{ color: GREEN }}><I.cal width={13} height={13} /></span>
          <span>Maandag 18:00 — 19:00 · Baan 1</span>
        </div>
      </div>

      <p style={{ fontSize: 12.5, fontWeight: 600, color: "#374151", margin: "0 0 10px" }}>Betaalmethode</p>
      <div style={{ display: "flex", flexDirection: "column", gap: 8, marginBottom: 18 }}>
        <label style={{ display: "flex", alignItems: "center", gap: 10, padding: 12, border: `2px solid ${GREEN}`, background: "rgba(45,80,22,.05)", borderRadius: 8, cursor: "pointer" }}>
          <div style={{ width: 14, height: 14, borderRadius: 999, border: `4px solid ${GREEN}`, background: "#fff" }} />
          <span style={{ fontSize: 12.5, color: "#111827" }}>Cash (bij de trainer)</span>
        </label>
        <label style={{ display: "flex", alignItems: "center", gap: 10, padding: 12, border: "2px solid #e5e7eb", borderRadius: 8, opacity: 0.5 }}>
          <div style={{ width: 14, height: 14, borderRadius: 999, border: "2px solid #d1d5db" }} />
          <div>
            <span style={{ fontSize: 12.5, color: "#9ca3af" }}>Online</span>
            <span style={{ fontSize: 10.5, color: "#9ca3af", fontStyle: "italic", marginLeft: 6 }}>(binnenkort)</span>
          </div>
        </label>
      </div>

      <PrimaryBtn>Bevestig plek</PrimaryBtn>
    </div>
  </ConfShell>
);

const ConfirmationDeclineState = () => (
  <ConfShell>
    <div style={{ padding: 24 }}>
      <h2 style={{ fontSize: 15, fontWeight: 700, margin: "0 0 4px" }}>Kies een ander tijdslot</h2>
      <p style={{ fontSize: 12, color: "#9ca3af", margin: "0 0 18px" }}>Er zijn nog plekken vrij in andere slots.</p>

      <div style={{ display: "flex", flexDirection: "column", gap: 8, marginBottom: 18 }}>
        {[
          { label: "Dinsdag 19:00 — 20:00", court: "Baan 2", left: "3 vrij", active: true },
          { label: "Woensdag 17:00 — 18:00", court: "Baan 1", left: "1 vrij" },
          { label: "Zaterdag 10:00 — 11:00", court: "Baan 1", left: "2 vrij" },
        ].map((s) => (
          <div key={s.label} style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: 12, border: `2px solid ${s.active ? GREEN : "#e5e7eb"}`, background: s.active ? "rgba(45,80,22,.05)" : "#fff", borderRadius: 8 }}>
            <div>
              <p style={{ margin: 0, fontSize: 12.5, fontWeight: 600, color: "#111827" }}>{s.label}</p>
              <p style={{ margin: 0, fontSize: 11, color: "#9ca3af" }}>{s.court}</p>
            </div>
            <span style={{ fontSize: 10.5, color: "#9ca3af" }}>{s.left}</span>
          </div>
        ))}
      </div>

      <button style={{ width: "100%", height: 40, background: GREEN, color: "#fff", fontWeight: 600, fontSize: 12.5, border: "none", borderRadius: 8, display: "flex", alignItems: "center", justifyContent: "center", gap: 6 }}>
        <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="#fff" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12h14M13 5l7 7-7 7" /></svg>
        Kies dit tijdslot
      </button>
    </div>
  </ConfShell>
);

const ConfirmationSuccessState = () => (
  <ConfShell>
    <div style={{ padding: "56px 24px", textAlign: "center" }}>
      <div style={{ width: 52, height: 52, borderRadius: 999, background: "#dcfce7", margin: "0 auto 16px", display: "grid", placeItems: "center", color: "#16a34a" }}>
        <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10" /><path d="M8 12l3 3 5-6" /></svg>
      </div>
      <h2 style={{ fontSize: 16, fontWeight: 700, margin: "0 0 6px" }}>Plek bevestigd</h2>
      <p style={{ fontSize: 12.5, color: "#6b7280", margin: 0, lineHeight: 1.55 }}>Tot maandag! Je krijgt een herinnering 24u op voorhand.</p>
    </div>
  </ConfShell>
);

const ConfirmationExpiredState = () => (
  <ConfShell>
    <div style={{ padding: "56px 24px", textAlign: "center" }}>
      <div style={{ width: 52, height: 52, borderRadius: 999, background: "#fef3c7", margin: "0 auto 16px", display: "grid", placeItems: "center", color: "#d97706" }}>
        <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M10.3 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.7 3.86a2 2 0 00-3.4 0z" /><path d="M12 9v4M12 17h.01" /></svg>
      </div>
      <h2 style={{ fontSize: 16, fontWeight: 700, margin: "0 0 6px" }}>Link verlopen</h2>
      <p style={{ fontSize: 12.5, color: "#6b7280", margin: 0, lineHeight: 1.55, maxWidth: 340, marginLeft: "auto", marginRight: "auto" }}>
        Deze bevestigingslink is niet meer geldig. Neem contact op met je trainer voor een nieuwe.
      </p>
    </div>
  </ConfShell>
);

// ── NEW LESSON WIZARD — STEP 3 (Plan bevestigen) ──────────────────────────

const StepPill = ({ n, label, state }) => {
  const color = state === "done" ? GREEN : state === "active" ? GREEN : "#9ca3af";
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
      <div style={{ width: 26, height: 26, borderRadius: 999, border: `2px solid ${color}`, background: state === "done" ? GREEN : "#fff", color: state === "done" ? "#fff" : color, display: "grid", placeItems: "center", fontSize: 11, fontWeight: 700 }}>
        {state === "done" ? "✓" : n}
      </div>
      <span style={{ fontSize: 12, fontWeight: state === "active" ? 700 : 500, color: state === "pending" ? "#9ca3af" : "#111827" }}>{label}</span>
    </div>
  );
};

const StepIndicator = ({ step }) => (
  <div style={{ display: "flex", alignItems: "center", gap: 14, padding: "14px 0 22px", borderBottom: "1px solid #f3f4f6", marginBottom: 22 }}>
    <StepPill n={1} label="Basisinfo" state={step > 1 ? "done" : "active"} />
    <div style={{ flex: 0, width: 40, height: 1, background: "#e5e7eb" }} />
    <StepPill n={2} label="Planning" state={step > 2 ? "done" : step === 2 ? "active" : "pending"} />
    <div style={{ flex: 0, width: 40, height: 1, background: "#e5e7eb" }} />
    <StepPill n={3} label="Bevestigen" state={step === 3 ? "active" : "pending"} />
  </div>
);

const WeekCard = ({ label, dates, override, slots }) => (
  <div style={{ background: "#fff", borderRadius: 12, padding: 18, boxShadow: "0 1px 2px rgba(0,0,0,.03)", marginBottom: 14 }}>
    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 14 }}>
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <span style={{ fontSize: 12.5, fontWeight: 700, color: "#111827" }}>{label}</span>
        <span style={{ fontSize: 11, color: "#9ca3af" }}>{dates}</span>
        {override && (
          <span style={{ fontSize: 9.5, fontWeight: 600, color: "#d97706", background: "rgba(251,191,36,.15)", padding: "2px 6px", borderRadius: 4 }}>Aangepast</span>
        )}
      </div>
      <button style={{ display: "inline-flex", alignItems: "center", gap: 4, fontSize: 11, color: "#9ca3af", background: "transparent", border: "none", padding: "4px 8px", borderRadius: 6 }}>
        <svg viewBox="0 0 24 24" width="11" height="11" {...iconStroke}><path d="M12 20h9M16.5 3.5l4 4L7 21l-4 1 1-4z" /></svg>
        Week aanpassen
      </button>
    </div>
    <div style={{ display: "grid", gridTemplateColumns: "repeat(7,1fr)", gap: 8 }}>
      {["Ma 12","Di 13","Wo 14","Do 15","Vr 16","Za 17","Zo 18"].map((d, i) => (
        <div key={d}>
          <div style={{ textAlign: "center", padding: "5px 0", background: "#f9fafb", borderRadius: 7, marginBottom: 6 }}>
            <p style={{ fontSize: 9.5, fontWeight: 700, color: "#6b7280", margin: 0 }}>{d.split(" ")[0]}</p>
            <p style={{ fontSize: 9.5, color: "#9ca3af", margin: 0 }}>{d.split(" ")[1]} apr</p>
          </div>
          {slots.filter((s) => s.day === i).map((s, si) => (
            <div key={si} style={{ background: "rgba(45,80,22,.05)", border: "1px solid rgba(45,80,22,.15)", borderRadius: 5, padding: 5, marginBottom: 4 }}>
              <p style={{ fontSize: 9.5, fontWeight: 700, color: "#1f2937", margin: 0 }}>{s.time}</p>
              <p style={{ fontSize: 9, color: "#6b7280", margin: 0 }}>{s.court}</p>
              <p style={{ fontSize: 9, color: "#9ca3af", margin: 0 }}>{s.trainer}</p>
            </div>
          ))}
        </div>
      ))}
    </div>
  </div>
);

const WizardStep3 = ({ loading }) => (
  <Shell active="lessons">
    <div style={{ maxWidth: 880, margin: "0 auto" }}>
      <p style={{ fontSize: 11.5, color: "#9ca3af", margin: "0 0 4px", display: "flex", alignItems: "center", gap: 4 }}>
        <svg viewBox="0 0 24 24" width="11" height="11" {...iconStroke}><path d="M15 18l-6-6 6-6" /></svg>
        Terug naar lesreeksen
      </p>
      <h1 style={{ fontSize: 22, fontWeight: 700, letterSpacing: -0.4, margin: "0 0 4px" }}>Nieuwe lesreeks</h1>
      <p style={{ fontSize: 12.5, color: "#9ca3af", margin: "0 0 4px" }}>Plan je lesreeks in drie stappen.</p>
      <StepIndicator step={3} />

      <div style={{ background: "#fff", borderRadius: 12, padding: 18, marginBottom: 16 }}>
        <p style={{ fontSize: 13, fontWeight: 700, color: "#111827", margin: "0 0 2px" }}>Bekijk &amp; bevestig</p>
        <p style={{ fontSize: 11.5, color: "#9ca3af", margin: 0 }}>Elke week is identiek tenzij je afwijkingen instelt (vakanties, vervanging).</p>
      </div>

      <WeekCard
        label="Week 1" dates="12 apr – 18 apr"
        slots={[
          { day: 0, time: "18:00–19:00", court: "Baan 1", trainer: "Jan P." },
          { day: 2, time: "17:00–18:00", court: "Baan 2", trainer: "Sofie R." },
          { day: 5, time: "10:00–11:00", court: "Baan 1", trainer: "Jan P." },
        ]}
      />
      <WeekCard
        label="Week 2" dates="19 apr – 25 apr" override
        slots={[
          { day: 0, time: "18:00–19:00", court: "Baan 1", trainer: "Jan P." },
          { day: 5, time: "10:00–11:00", court: "Baan 1", trainer: "Kim V." },
        ]}
      />
      <WeekCard
        label="Week 3" dates="26 apr – 2 mei"
        slots={[
          { day: 0, time: "18:00–19:00", court: "Baan 1", trainer: "Jan P." },
          { day: 2, time: "17:00–18:00", court: "Baan 2", trainer: "Sofie R." },
        ]}
      />

      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", paddingTop: 6 }}>
        <button style={{ padding: "10px 18px", fontSize: 12.5, fontWeight: 500, color: "#374151", border: "1px solid #e5e7eb", background: "#fff", borderRadius: 8, display: "inline-flex", alignItems: "center", gap: 6 }}>
          <svg viewBox="0 0 24 24" width="13" height="13" {...iconStroke}><path d="M15 18l-6-6 6-6" /></svg>
          Vorige
        </button>
        <button style={{ padding: "10px 20px", fontSize: 12.5, fontWeight: 700, color: "#fff", background: loading ? "rgba(45,80,22,.6)" : GREEN, border: "none", borderRadius: 8, display: "inline-flex", alignItems: "center", gap: 8 }}>
          {loading && (
            <span style={{ width: 13, height: 13, borderRadius: 999, border: "2px solid rgba(255,255,255,.3)", borderTopColor: "#fff", display: "inline-block", animation: "spin .8s linear infinite" }} />
          )}
          {loading ? "Bezig met aanmaken…" : "Reeks aanmaken"}
        </button>
      </div>
      <style>{"@keyframes spin{to{transform:rotate(360deg)}}"}</style>
    </div>
  </Shell>
);

const WizardStep3Current = () => <WizardStep3 loading={false} />;
const WizardStep3Loading = () => <WizardStep3 loading={true} />;

// ── LESSONS EMPTY STATE ────────────────────────────────────────────────────

const LessonsEmptyState = () => (
  <Shell active="lessons">
    <div style={{ maxWidth: 1000, margin: "0 auto" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-end", marginBottom: 18 }}>
        <div>
          <h1 style={{ fontSize: 22, fontWeight: 700, margin: "0 0 4px", letterSpacing: -0.4 }}>Lesreeksen</h1>
          <p style={{ fontSize: 12.5, color: "#9ca3af", margin: 0 }}>Plan en beheer je lessenreeksen.</p>
        </div>
        <button style={{ display: "inline-flex", alignItems: "center", gap: 6, padding: "9px 14px", background: GREEN, color: "#fff", fontSize: 12, fontWeight: 700, border: "none", borderRadius: 8 }}>
          <I.plus width={13} height={13} /> Nieuwe reeks
        </button>
      </div>

      <div style={{ background: "#fff", borderRadius: 14, padding: "80px 24px", textAlign: "center", border: "1px dashed #e5e7eb" }}>
        <div style={{ width: 60, height: 60, borderRadius: 14, background: "rgba(45,80,22,.06)", margin: "0 auto 18px", display: "grid", placeItems: "center", color: GREEN }}>
          <I.book width={26} height={26} />
        </div>
        <h2 style={{ fontSize: 16, fontWeight: 700, margin: "0 0 6px" }}>Nog geen lesreeksen</h2>
        <p style={{ fontSize: 12.5, color: "#9ca3af", margin: "0 0 20px", maxWidth: 340, marginLeft: "auto", marginRight: "auto", lineHeight: 1.55 }}>
          Maak je eerste lesreeks aan om inschrijvingen te openen en een planning te bouwen.
        </p>
        <button style={{ display: "inline-flex", alignItems: "center", gap: 6, padding: "10px 18px", background: GREEN, color: "#fff", fontSize: 12, fontWeight: 700, border: "none", borderRadius: 8 }}>
          <I.plus width={13} height={13} /> Nieuwe reeks aanmaken
        </button>
      </div>
    </div>
  </Shell>
);

// ── Export ─────────────────────────────────────────────────────────────────

Object.assign(window, {
  RegisterCurrent, StudentLoginCurrent, SettingsCurrent, SettingsDeleteState,
  EnrollCurrent, EnrollSubmittedState,
  ConfirmationCurrent, ConfirmationPaymentState, ConfirmationDeclineState,
  ConfirmationSuccessState, ConfirmationExpiredState,
  WizardStep3Current, WizardStep3Loading,
  LessonsEmptyState,
});
