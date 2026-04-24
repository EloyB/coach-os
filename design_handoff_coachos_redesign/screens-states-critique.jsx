// Critique notes + proposed redesigns for the "02b — Pages not yet covered" set.
// Uses AnnotationFrame (from annotations.jsx) + Shell/I/GREEN/LIME (from screens-current.jsx).

const iconStrokeX = { fill: "none", stroke: "currentColor", strokeWidth: 1.75, strokeLinecap: "round", strokeLinejoin: "round" };

// ════════════════════════════════════════════════════════════════════════════
// NOTES
// ════════════════════════════════════════════════════════════════════════════

const NOTES_MORE = {
  register: {
    title: "Register — a wall of fields with no trust",
    takeaway: "Six fields, no password rules, no ‘what happens next’, no social proof. First impressions matter more here than on login.",
    items: [
      { tone: "red",   title: "Password requirements are invisible", body: "‘8 tekens’ only appears *after* the user submits and fails. Show the rule inline with live check-off — don’t punish the user for guessing wrong." },
      { tone: "amber", title: "Organization + personal in one column", body: "A single form mixes ‘my organization’ and ‘me’. Split into two short steps — Org info first, personal second — or at least group visually with a divider and ‘STAP 1: over je club’ header." },
      { tone: "amber", title: "Marketing copy doesn’t mention pricing or trial", body: "‘Plan lessen. Vul banen.’ is identical to the login hero. A register page should answer: how much? free trial? setup time? — none of that is visible." },
      { tone: "blue",  title: "No social or magic-link alternative", body: "Every coach has Google Workspace for their club. An SSO button would remove 5 fields. Instead the form asks for a second password confirmation — a 2007-era pattern." },
    ],
  },
  slogin: {
    title: "Student login — tiny card, big question marks",
    takeaway: "Parents arrive here from a confirmation email link. The page doesn’t explain that, so every second user emails the coach asking ‘did I do something wrong?’",
    items: [
      { tone: "red",   title: "No indication this is the right page", body: "A parent clicking ‘Bekijk je lessen’ in an email lands here. Nothing says ‘Yes, this is where you manage Sophie’s tennis lessons’. The card floats in space with zero context." },
      { tone: "amber", title: "Magic link flow is invisible", body: "After you enter an email, what happens? No confirmation, no ‘check your inbox’ screen shown here — it probably exists, but the page gives no hint of the shape of the flow." },
      { tone: "amber", title: "‘Trainer? Inloggen’ link is the only escape hatch", body: "A coach who opens the wrong URL has a single tiny underlined word to recover. This should be a proper role-switcher at the top." },
      { tone: "blue",  title: "Zero personality", body: "Compared to the full-bleed marketing on the coach login, the student login looks like a 2013 admin panel. Students are 60% of the users — this page deserves equal polish." },
    ],
  },
  settings: {
    title: "Settings — a settings page in name only",
    takeaway: "Tennis Clubs is the sole setting. A club admin opens this expecting 15 things and finds one.",
    items: [
      { tone: "red",   title: "Missing every other settings surface", body: "No profile, billing, notifications, team/roles, branding, integrations, data export, or org-wide defaults (default lesson length, price, cancellation window). The product ships without half its settings UI." },
      { tone: "amber", title: "Add-form buried below the list", body: "The first-time user sees ‘Nog geen clubs’ and then has to scan down to find the add form. The form should be the prominent CTA when the list is empty." },
      { tone: "amber", title: "Address as free text", body: "‘Brederodestraat 5, Haarlem’ typed by hand leads to inconsistency (postal code? country?). A Google Places autocomplete would normalize this and unlock the calendar showing a map marker." },
      { tone: "blue",  title: "No way to edit a club", body: "Delete is the only per-row action. Typo in the name? Moved address? You have to delete and re-add — but deletion is blocked if the club is in use. Coaches get stuck." },
    ],
  },
  enroll: {
    title: "Public enroll — a form, not a sales page",
    takeaway: "This is the most-trafficked page in the product (every student sees it). It should sell AND convert; right now it only converts.",
    items: [
      { tone: "red",   title: "Availability grid has no smart defaults", body: "A student has to click every single slot across preferred / available / unavailable. At 8 slots × 3 columns = 24 decisions. Default ‘available’ on all, let them downgrade. Also: no ‘alle avonden oké’ shortcut." },
      { tone: "red",   title: "Price appears once, 600px from the submit button", body: "€320 is stated at the top but there’s no running total as the student picks slots, adds group members, etc. Checkout patterns exist for a reason." },
      { tone: "amber", title: "No level/skill filter visible", body: "The card says ‘Gevorderd’ but there’s no self-assessment tool (how do I know if I’m gevorderd?). Result: students enroll in the wrong level, coaches manually reassign." },
      { tone: "blue",  title: "Trainer not shown", body: "Students enroll in a series without knowing who their trainer will be — a huge part of the purchase decision, especially for children’s programs. Add trainer profile cards." },
    ],
  },
  confirm: {
    title: "Confirmation — functional, but cold and brittle",
    takeaway: "This page does ONE thing: get a yes/no from a student. It’s fine — but the UX is fragile at every edge.",
    items: [
      { tone: "red",   title: "‘Cash’ is the only payment method", body: "‘Online’ is shown disabled with ‘(binnenkort)’ — a dark pattern. If it’s not shipped, don’t show it. Coaches using SumUp/Payconiq need this route yesterday." },
      { tone: "amber", title: "No add-to-calendar after confirming", body: "Success state says ‘Tot maandag!’ and ends. No .ics file, no ‘add to Google Calendar’ button. Half of no-shows are diary misses." },
      { tone: "amber", title: "Decline flow loses context", body: "Clicking ‘Weigeren’ jumps to an alternative-slot picker with just ‘Dinsdag 19:00’ — no trainer, no price, no ‘you’ll be in group X’. Choosing an alternative is almost as big a commitment as the original." },
      { tone: "blue",  title: "Expiry is a grey footnote", body: "‘Verloopt op 28/04 om 18:00’ is in light gray at the bottom. This is the most urgent piece of info on the page — it should be a countdown or a colored strip at the top." },
    ],
  },
  wiz3: {
    title: "New lesson wizard · step 3 — a wall of identical weeks",
    takeaway: "For a 12-week reeks the coach scrolls past 12 near-identical cards. Collapse what’s the same, highlight what’s different.",
    items: [
      { tone: "red",   title: "Every week renders in full even when identical", body: "Week 1, Week 3, Week 4, Week 5... are copies of each other. Show the weekly template ONCE at the top, then list only the weeks that deviate (holidays, trainer swap, cancellation)." },
      { tone: "amber", title: "No summary before the big button", body: "‘Reeks aanmaken’ commits to 36 lessons, 3 trainers, €X of revenue — none of which is summarized. A coach can hit this button and realize only afterward they forgot the Easter week." },
      { tone: "amber", title: "Step indicator is generic", body: "Basisinfo → Planning → Bevestigen. Three dots and three words. No sense of how much is left, no ability to jump back to step 1 without ‘Vorige’ twice." },
      { tone: "blue",  title: "‘Week aanpassen’ affordance is weak", body: "A tiny gray pencil icon in the top-right of each week. For the single most important interaction on this screen (adding a holiday exception), it should be much more prominent — ideally an inline ‘+ uitzondering’ button per day cell." },
    ],
  },
};

// States critique — lighter, one per state.
const NOTES_STATES = {
  confPay: {
    title: "Confirmation · payment step",
    takeaway: "Showing a disabled ‘Online’ option is a trust-killer. Hide it until it ships.",
    items: [
      { tone: "red",   title: "Disabled ‘Online (binnenkort)’ tile", body: "The user scans for the cheapest/easiest option, sees ‘Online’, reaches for it, and is stopped. Either hide the feature or make the entire row a clickable waitlist (‘Laat me weten wanneer online werkt’)." },
      { tone: "amber", title: "No breakdown for groups", body: "A group confirmation should show €32 × 4 = €128 and which members are included. This step barely differs from solo." },
      { tone: "blue",  title: "No ‘Terug’ on payment step", body: "Once the user clicks ‘Plan bevestigen’ they’re committed. There’s no way to go back to review without reloading the page and losing token context." },
    ],
  },
  confDec: {
    title: "Confirmation · decline → pick alternative",
    takeaway: "A good fallback, but the alternatives are stripped of everything that helps a decision.",
    items: [
      { tone: "red",   title: "No trainer or level on alternatives", body: "‘Dinsdag 19:00 · Baan 2 · 3 vrij’. The student doesn’t know who the trainer is, whether the group level matches, or if it’s solo vs group. Paste the key metadata on each option." },
      { tone: "amber", title: "‘3 vrij’ is ambiguous", body: "3 of how many? Use ‘3 / 4 vrij’ or better — render a tiny capacity bar." },
      { tone: "blue",  title: "No ‘none of these work’ path", body: "If all alternatives are wrong, the student has no escape — just an un-confirmed slot that expires. Add a ‘Geen van deze — neem contact op’ link." },
    ],
  },
  wizLoad: {
    title: "Wizard submitting — the blind second",
    takeaway: "A spinner is fine; telling the user what’s actually happening (creating 36 lessons, sending 18 invites) builds more trust.",
    items: [
      { tone: "amber", title: "‘Bezig met aanmaken…’ is vague", body: "This operation creates the reeks, 36 lessons, and likely sends confirmation emails. Show progress — e.g. ‘Reeks aanmaken · 12 van 36 lessen geplaatst’ — or at least list what’s happening underneath." },
      { tone: "blue",  title: "Button-only loading state", body: "The whole page stays interactive during submit. A user could click ‘Vorige’ mid-submit and enter an inconsistent state. Disable the whole form during mutation." },
    ],
  },
};

// ════════════════════════════════════════════════════════════════════════════
// PINS — per artboard, positioned for 1280×800 (critique artboards are 1620w)
// ════════════════════════════════════════════════════════════════════════════

const PINS = {
  register: [
    { n: 1, top: 460, left: 900, tone: "red" },
    { n: 2, top: 220, left: 900, tone: "amber" },
    { n: 3, top: 280, left: 160, tone: "amber" },
    { n: 4, top: 100, left: 900, tone: "blue" },
  ],
  slogin: [
    { n: 1, top: 210, left: 500, tone: "red" },
    { n: 2, top: 420, left: 500, tone: "amber" },
    { n: 3, top: 520, left: 500, tone: "amber" },
    { n: 4, top: 60, left: 100, tone: "blue" },
  ],
  settings: [
    { n: 1, top: 70, left: 380, tone: "red" },
    { n: 2, top: 500, left: 340, tone: "amber" },
    { n: 3, top: 620, left: 900, tone: "amber" },
    { n: 4, top: 300, left: 1060, tone: "blue" },
  ],
  enroll: [
    { n: 1, top: 730, left: 340, tone: "red" },
    { n: 2, top: 170, left: 100, tone: "red" },
    { n: 3, top: 200, left: 600, tone: "amber" },
    { n: 4, top: 260, left: 100, tone: "blue" },
  ],
  confirm: [
    { n: 1, top: 430, left: 280, tone: "red" },
    { n: 2, top: 520, left: 100, tone: "amber" },
    { n: 3, top: 480, left: 380, tone: "amber" },
    { n: 4, top: 390, left: 100, tone: "blue" },
  ],
  wiz3: [
    { n: 1, top: 500, left: 300, tone: "red" },
    { n: 2, top: 820, left: 700, tone: "amber" },
    { n: 3, top: 240, left: 300, tone: "amber" },
    { n: 4, top: 380, left: 820, tone: "blue" },
  ],
  confPay: [
    { n: 1, top: 380, left: 120, tone: "red" },
    { n: 2, top: 230, left: 100, tone: "amber" },
    { n: 3, top: 490, left: 300, tone: "blue" },
  ],
  confDec: [
    { n: 1, top: 290, left: 140, tone: "red" },
    { n: 2, top: 290, left: 420, tone: "amber" },
    { n: 3, top: 510, left: 140, tone: "blue" },
  ],
  wizLoad: [
    { n: 1, top: 820, left: 700, tone: "amber" },
    { n: 2, top: 280, left: 500, tone: "blue" },
  ],
};

// ════════════════════════════════════════════════════════════════════════════
// PROPOSED REDESIGNS
// ════════════════════════════════════════════════════════════════════════════

const mono = "'JetBrains Mono', ui-monospace, monospace";
const INK = "#161513";
const PAPER = "#fdfcf9";
const MUTED = "#6b6760";
const LINE = "#e8e4db";

// ── REGISTER (proposed) ────────────────────────────────────────────────────

const RegisterP = () => (
  <div style={{ width: "100%", height: "100%", display: "flex", background: PAPER, fontFamily: "'Inter',system-ui,sans-serif" }}>
    {/* left — smaller, punchier */}
    <div style={{ width: "42%", background: INK, color: "#fff", padding: 36, display: "flex", flexDirection: "column", justifyContent: "space-between", position: "relative" }}>
      <div>
        <div style={{ fontFamily: mono, fontSize: 11, letterSpacing: "0.15em", color: LIME, textTransform: "uppercase" }}>
          /sign-up · v2026.04
        </div>
      </div>
      <div>
        <h1 style={{ fontSize: 40, fontWeight: 800, lineHeight: 1.02, margin: 0, letterSpacing: -1 }}>
          Van 40 Excel-<br />regels naar<br /><span style={{ color: LIME }}>één planning.</span>
        </h1>
        <p style={{ fontSize: 13, color: "#a8a195", maxWidth: 300, lineHeight: 1.6, margin: "18px 0 0" }}>
          CoachOS is de planningstool voor tennis- en padelclubs — €49/mnd per trainer, eerste 30 dagen gratis.
        </p>
      </div>
      <div style={{ display: "flex", flexDirection: "column", gap: 4, fontFamily: mono, fontSize: 10.5, color: "#8a8377" }}>
        <div>200+ clubs &middot; 12.400 leerlingen</div>
        <div style={{ color: "#5b554d" }}>— TC Brederode, TC Diest, TC Wervik…</div>
      </div>
    </div>

    {/* right — form */}
    <div style={{ flex: 1, display: "grid", placeItems: "center", padding: 28 }}>
      <div style={{ width: 400 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 28 }}>
          <div style={{ width: 28, height: 28, borderRadius: 999, background: GREEN, display: "grid", placeItems: "center" }}>
            <I.ball width={14} height={14} />
          </div>
          <span style={{ fontSize: 15, fontWeight: 700, color: INK }}>CoachOS</span>
        </div>

        <p style={{ fontFamily: mono, fontSize: 10.5, letterSpacing: "0.1em", textTransform: "uppercase", color: MUTED, margin: "0 0 6px" }}>/01 — je club</p>
        <h2 style={{ fontSize: 24, fontWeight: 700, letterSpacing: -0.4, margin: "0 0 22px" }}>Maak je account</h2>

        <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          <label>
            <div style={{ fontSize: 11, color: MUTED, marginBottom: 4, fontWeight: 500 }}>Clubnaam</div>
            <div style={{ height: 40, border: `1px solid ${LINE}`, borderRadius: 8, padding: "0 12px", display: "flex", alignItems: "center", fontSize: 13, color: "#c5c0b6", background: "#fff" }}>bv. TC Brederode</div>
          </label>

          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10 }}>
            <label>
              <div style={{ fontSize: 11, color: MUTED, marginBottom: 4, fontWeight: 500 }}>Voornaam</div>
              <div style={{ height: 40, border: `1px solid ${LINE}`, borderRadius: 8, padding: "0 12px", display: "flex", alignItems: "center", fontSize: 13, color: "#c5c0b6", background: "#fff" }}>Jan</div>
            </label>
            <label>
              <div style={{ fontSize: 11, color: MUTED, marginBottom: 4, fontWeight: 500 }}>Achternaam</div>
              <div style={{ height: 40, border: `1px solid ${LINE}`, borderRadius: 8, padding: "0 12px", display: "flex", alignItems: "center", fontSize: 13, color: "#c5c0b6", background: "#fff" }}>Janssen</div>
            </label>
          </div>

          <label>
            <div style={{ fontSize: 11, color: MUTED, marginBottom: 4, fontWeight: 500 }}>E-mail</div>
            <div style={{ height: 40, border: `1px solid ${LINE}`, borderRadius: 8, padding: "0 12px", display: "flex", alignItems: "center", fontSize: 13, background: "#fff" }}>jan@brederode.be</div>
          </label>

          <label>
            <div style={{ fontSize: 11, color: MUTED, marginBottom: 4, fontWeight: 500 }}>Wachtwoord</div>
            <div style={{ height: 40, border: `1px solid ${LINE}`, borderRadius: 8, padding: "0 12px", display: "flex", alignItems: "center", fontSize: 13, background: "#fff" }}>••••••••••••</div>
            {/* live requirement chips */}
            <div style={{ display: "flex", gap: 6, marginTop: 6 }}>
              {[
                ["8+ tekens", true],
                ["1 hoofdletter", true],
                ["1 cijfer", false],
              ].map(([t, ok]) => (
                <span key={t} style={{ fontSize: 10, fontFamily: mono, padding: "2px 8px", borderRadius: 999, background: ok ? "rgba(22,163,74,.1)" : "#f0eee9", color: ok ? "#15803d" : "#9a948a", display: "inline-flex", alignItems: "center", gap: 4 }}>
                  {ok ? "✓" : "○"} {t}
                </span>
              ))}
            </div>
          </label>

          <button style={{ height: 44, background: INK, color: "#fff", fontWeight: 600, fontSize: 13, border: "none", borderRadius: 8, marginTop: 10, cursor: "pointer" }}>
            Start 30-dagen proef →
          </button>

          <div style={{ display: "flex", alignItems: "center", gap: 10, margin: "8px 0", color: "#c5c0b6", fontSize: 10.5 }}>
            <div style={{ flex: 1, height: 1, background: LINE }} />OF<div style={{ flex: 1, height: 1, background: LINE }} />
          </div>

          <button style={{ height: 42, background: "#fff", color: INK, fontWeight: 600, fontSize: 12.5, border: `1px solid ${LINE}`, borderRadius: 8, display: "flex", alignItems: "center", justifyContent: "center", gap: 8 }}>
            <svg width="16" height="16" viewBox="0 0 24 24"><path fill="#4285F4" d="M22.5 12.2c0-.7-.1-1.4-.2-2H12v3.8h5.9c-.3 1.4-1.1 2.5-2.3 3.3v2.7h3.7c2.2-2 3.2-4.9 3.2-7.8z"/><path fill="#34A853" d="M12 23c3.1 0 5.6-1 7.5-2.8l-3.7-2.8c-1 .7-2.3 1.1-3.8 1.1-2.9 0-5.4-2-6.3-4.6H1.9v2.9C3.7 20.4 7.6 23 12 23z"/><path fill="#FBBC05" d="M5.7 13.9A6.6 6.6 0 015.4 12c0-.6.1-1.3.3-1.9V7.2H1.9A11 11 0 001 12c0 1.7.4 3.3 1 4.8l3.7-2.9z"/><path fill="#EA4335" d="M12 5.4c1.6 0 3.1.6 4.3 1.7L19.5 4A11 11 0 0012 1C7.6 1 3.7 3.6 1.9 7.2l3.8 2.9c.9-2.6 3.4-4.6 6.3-4.6z"/></svg>
            Doorgaan met Google
          </button>
        </div>

        <p style={{ fontSize: 11.5, color: MUTED, margin: "20px 0 0", textAlign: "center" }}>
          Al een account? <span style={{ color: GREEN, fontWeight: 600 }}>Inloggen</span>
        </p>
      </div>
    </div>
  </div>
);

// ── STUDENT LOGIN (proposed) ───────────────────────────────────────────────

const StudentLoginP = () => (
  <div style={{ width: "100%", height: "100%", background: PAPER, position: "relative", overflow: "hidden" }}>
    {/* faint court pattern */}
    <svg width="100%" height="100%" style={{ position: "absolute", inset: 0, opacity: 0.06 }}>
      <defs>
        <pattern id="grid" width="40" height="40" patternUnits="userSpaceOnUse">
          <path d="M 40 0 L 0 0 0 40" fill="none" stroke={INK} strokeWidth="0.5" />
        </pattern>
      </defs>
      <rect width="100%" height="100%" fill="url(#grid)" />
    </svg>

    <div style={{ position: "relative", maxWidth: 880, margin: "0 auto", padding: "60px 32px" }}>
      {/* topbar with role switcher */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 64 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div style={{ width: 30, height: 30, borderRadius: 999, background: GREEN, display: "grid", placeItems: "center" }}>
            <I.ball width={14} height={14} />
          </div>
          <span style={{ fontSize: 16, fontWeight: 700, color: INK }}>CoachOS</span>
        </div>
        <div style={{ display: "inline-flex", background: "#fff", border: `1px solid ${LINE}`, borderRadius: 999, padding: 3, fontSize: 11.5 }}>
          <button style={{ padding: "6px 14px", borderRadius: 999, background: INK, color: "#fff", border: "none", fontWeight: 600, cursor: "pointer" }}>Leerling</button>
          <button style={{ padding: "6px 14px", borderRadius: 999, background: "transparent", color: MUTED, border: "none", fontWeight: 600, cursor: "pointer" }}>Trainer</button>
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 420px", gap: 48, alignItems: "center" }}>
        <div>
          <p style={{ fontFamily: mono, fontSize: 11, color: GREEN, letterSpacing: "0.15em", textTransform: "uppercase", margin: "0 0 16px" }}>/leerlingen-portaal</p>
          <h1 style={{ fontSize: 44, fontWeight: 800, letterSpacing: -1, lineHeight: 1.05, margin: "0 0 16px", color: INK }}>
            Je lessen,<br />op één plek.
          </h1>
          <p style={{ fontSize: 14, color: MUTED, lineHeight: 1.6, margin: "0 0 22px", maxWidth: 360 }}>
            Vul je e-mailadres in. Je krijgt een link om in te loggen — geen wachtwoord nodig.
          </p>
          <div style={{ display: "flex", flexDirection: "column", gap: 10, fontSize: 12, color: MUTED }}>
            {["Bevestig je plekken", "Zie wie je trainer is", "Zet lessen in je kalender"].map((x) => (
              <div key={x} style={{ display: "flex", alignItems: "center", gap: 8 }}>
                <div style={{ width: 16, height: 16, borderRadius: 999, background: GREEN, display: "grid", placeItems: "center" }}>
                  <svg viewBox="0 0 24 24" width="10" height="10" fill="none" stroke="#fff" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12l5 5L20 7" /></svg>
                </div>
                {x}
              </div>
            ))}
          </div>
        </div>

        <div style={{ background: "#fff", borderRadius: 16, padding: 26, boxShadow: "0 20px 50px -20px rgba(0,0,0,.15)", border: `1px solid ${LINE}` }}>
          <p style={{ fontFamily: mono, fontSize: 10, color: MUTED, letterSpacing: "0.1em", textTransform: "uppercase", margin: "0 0 6px" }}>Stap 1 van 2</p>
          <h3 style={{ fontSize: 18, fontWeight: 700, margin: "0 0 16px", letterSpacing: -0.3 }}>E-mailadres</h3>
          <div style={{ height: 44, border: `1.5px solid ${INK}`, borderRadius: 10, padding: "0 14px", display: "flex", alignItems: "center", fontSize: 13 }}>
            sophie.parent@gmail.com
          </div>
          <button style={{ width: "100%", height: 44, background: INK, color: "#fff", fontWeight: 600, fontSize: 13, border: "none", borderRadius: 10, marginTop: 12, cursor: "pointer" }}>
            Stuur login-link →
          </button>
          <p style={{ fontSize: 11, color: MUTED, margin: "12px 0 0", textAlign: "center", lineHeight: 1.5 }}>
            Check je inbox — de link werkt 15 minuten.<br />Geen mail? <span style={{ color: GREEN, fontWeight: 600 }}>Opnieuw sturen</span>
          </p>
        </div>
      </div>
    </div>
  </div>
);

// ── SETTINGS (proposed) ────────────────────────────────────────────────────

const SettingsP = () => (
  <Shell active="settings">
    <div style={{ maxWidth: 960, margin: "0 auto" }}>
      <div style={{ marginBottom: 18 }}>
        <p style={{ fontFamily: mono, fontSize: 11, color: MUTED, letterSpacing: "0.1em", textTransform: "uppercase", margin: 0 }}>/settings</p>
        <h1 style={{ fontSize: 24, fontWeight: 800, letterSpacing: -0.6, margin: "4px 0 2px" }}>Instellingen</h1>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "200px 1fr", gap: 24 }}>
        {/* subnav */}
        <aside style={{ fontSize: 12.5 }}>
          {[
            ["Mijn profiel", false],
            ["Tennisclubs", true],
            ["Team & rollen", false],
            ["Standaardprijzen", false],
            ["E-mailsjablonen", false],
            ["Betaalmethodes", false],
            ["Notificaties", false],
            ["Facturatie", false],
            ["Integraties", false],
            ["Data export", false],
          ].map(([label, active]) => (
            <div key={label} style={{ padding: "8px 12px", borderRadius: 8, background: active ? INK : "transparent", color: active ? "#fff" : INK, fontWeight: active ? 600 : 500, marginBottom: 2, cursor: "pointer" }}>
              {label}
            </div>
          ))}
        </aside>

        {/* main */}
        <div>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-end", marginBottom: 18, paddingBottom: 14, borderBottom: `1px solid ${LINE}` }}>
            <div>
              <h2 style={{ fontSize: 18, fontWeight: 700, margin: "0 0 2px", letterSpacing: -0.3 }}>Tennisclubs</h2>
              <p style={{ fontSize: 12, color: MUTED, margin: 0 }}>Locaties waar jouw organisatie lessen geeft.</p>
            </div>
            <button style={{ display: "inline-flex", alignItems: "center", gap: 6, padding: "8px 14px", background: INK, color: "#fff", fontSize: 12, fontWeight: 600, border: "none", borderRadius: 8, cursor: "pointer" }}>
              <I.plus width={12} height={12} /> Nieuwe club
            </button>
          </div>

          {/* cards with richer data */}
          <div style={{ display: "grid", gap: 10 }}>
            {[
              { name: "TC Brederode", addr: "Brederodestraat 5, 2000 Haarlem", series: 4, lessons: 48, trainers: 3, active: true },
              { name: "Padel Park Antwerpen", addr: "Plantin en Moretuslei 220, 2018 Antwerpen", series: 2, lessons: 16, trainers: 2, active: true },
              { name: "TC Deurne", addr: "Boekenberglei 1, 2100 Deurne", series: 0, lessons: 0, trainers: 0, active: false },
            ].map((c) => (
              <div key={c.name} style={{ background: "#fff", border: `1px solid ${LINE}`, borderRadius: 12, padding: 16, display: "grid", gridTemplateColumns: "1fr auto auto auto auto auto", alignItems: "center", gap: 18 }}>
                <div>
                  <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                    <p style={{ margin: 0, fontSize: 14, fontWeight: 700 }}>{c.name}</p>
                    {!c.active && (
                      <span style={{ fontSize: 9.5, fontFamily: mono, padding: "1px 6px", background: "#f0eee9", color: MUTED, borderRadius: 4, textTransform: "uppercase" }}>ongebruikt</span>
                    )}
                  </div>
                  <p style={{ margin: "2px 0 0", fontSize: 11.5, color: MUTED }}>{c.addr}</p>
                </div>
                <Stat label="reeksen" value={c.series} />
                <Stat label="lessen" value={c.lessons} />
                <Stat label="trainers" value={c.trainers} />
                <button style={{ padding: "6px 10px", fontSize: 11, fontWeight: 600, color: INK, background: "#fff", border: `1px solid ${LINE}`, borderRadius: 6 }}>Bewerken</button>
                <button style={{ padding: 6, background: "transparent", border: "none", color: "#c5c0b6", cursor: "pointer" }}>
                  <I.trash width={14} height={14} />
                </button>
              </div>
            ))}
          </div>

          <p style={{ fontSize: 11, color: MUTED, margin: "14px 0 0", fontFamily: mono }}>
            ↳ Je kunt een club pas verwijderen als er geen actieve reeksen meer zijn.
          </p>
        </div>
      </div>
    </div>
  </Shell>
);

const Stat = ({ label, value }) => (
  <div style={{ textAlign: "right", minWidth: 64 }}>
    <div style={{ fontFamily: mono, fontSize: 16, fontWeight: 700, color: INK, lineHeight: 1 }}>{value}</div>
    <div style={{ fontSize: 10, color: MUTED, marginTop: 2, fontFamily: mono, letterSpacing: "0.05em" }}>{label}</div>
  </div>
);

// ── ENROLL (proposed) ──────────────────────────────────────────────────────

const EnrollP = () => (
  <div style={{ width: "100%", height: "100%", background: PAPER, overflow: "auto", fontFamily: "'Inter',system-ui,sans-serif" }}>
    <div style={{ maxWidth: 720, margin: "0 auto", padding: "28px 20px 100px" }}>
      {/* brand bar */}
      <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 22 }}>
        <div style={{ width: 28, height: 28, borderRadius: 999, background: GREEN, display: "grid", placeItems: "center" }}>
          <I.ball width={14} height={14} />
        </div>
        <span style={{ fontSize: 15, fontWeight: 700, color: INK }}>CoachOS</span>
        <span style={{ fontSize: 11, fontFamily: mono, color: MUTED, marginLeft: "auto" }}>TC Brederode &middot; inschrijving</span>
      </div>

      {/* hero */}
      <div style={{ background: INK, color: "#fff", borderRadius: 16, padding: 28, position: "relative", overflow: "hidden", marginBottom: 14 }}>
        <span style={{ display: "inline-block", padding: "3px 9px", background: LIME, color: INK, fontSize: 10.5, fontWeight: 700, borderRadius: 999, marginBottom: 10, fontFamily: mono, letterSpacing: "0.05em", textTransform: "uppercase" }}>
          Gevorderd · niveau 4
        </span>
        <h1 style={{ fontSize: 30, fontWeight: 800, margin: "0 0 6px", letterSpacing: -0.8 }}>Padel — Voorjaar 2026</h1>
        <p style={{ fontSize: 13, color: "#b8b0a0", margin: "0 0 18px", lineHeight: 1.55, maxWidth: 500 }}>
          10 weken intensieve padel-coaching. Focus op verdedigend spel en glasgebruik.
        </p>
        <div style={{ display: "flex", gap: 22, fontSize: 12, color: "#8a8377", fontFamily: mono }}>
          <span><span style={{ color: "#fff", fontWeight: 600 }}>€320</span> / reeks</span>
          <span>01 apr → 30 jun</span>
          <span>Padel Park Antwerpen</span>
          <span style={{ color: LIME }}>18 / 24 ingeschreven</span>
        </div>
        {/* trainer strip */}
        <div style={{ display: "flex", alignItems: "center", gap: 10, marginTop: 18, paddingTop: 16, borderTop: "1px solid rgba(255,255,255,.1)" }}>
          <div style={{ display: "flex" }}>
            {["#a7f3d0", "#fcd34d", "#f9a8d4"].map((c, i) => (
              <div key={i} style={{ width: 28, height: 28, borderRadius: 999, background: c, border: "2px solid #161513", marginLeft: i === 0 ? 0 : -8, display: "grid", placeItems: "center", fontFamily: mono, fontSize: 10, fontWeight: 700, color: INK }}>
                {["JP", "SR", "KV"][i]}
              </div>
            ))}
          </div>
          <div style={{ fontSize: 11.5, color: "#a8a195" }}>Je trainers: Jan P., Sofie R., Kim V.</div>
        </div>
      </div>

      {/* Form card */}
      <div style={{ background: "#fff", border: `1px solid ${LINE}`, borderRadius: 16, overflow: "hidden" }}>
        <div style={{ padding: "18px 24px", borderBottom: `1px solid ${LINE}`, display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <div>
            <h2 style={{ fontSize: 16, fontWeight: 700, margin: 0, letterSpacing: -0.2 }}>Jouw inschrijving</h2>
            <p style={{ fontSize: 11.5, color: MUTED, margin: "2px 0 0" }}>Vul in, kies slots, en bevestig.</p>
          </div>
          <div style={{ fontFamily: mono, fontSize: 11, color: MUTED }}>3 / 4 velden</div>
        </div>

        <div style={{ padding: 24, display: "flex", flexDirection: "column", gap: 22 }}>
          {/* personal */}
          <section>
            <p style={{ fontFamily: mono, fontSize: 10.5, letterSpacing: "0.1em", textTransform: "uppercase", color: MUTED, margin: "0 0 10px" }}>/persoonlijk</p>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10 }}>
              <FPInput label="Voornaam" val="Jan" />
              <FPInput label="Achternaam" val="Janssen" />
              <FPInput label="E-mail" val="jan@gmail.be" wide />
              <FPInput label="Telefoon" val="+32 471 23 45 67" />
            </div>
          </section>

          {/* availability — with smart defaults */}
          <section>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", marginBottom: 10 }}>
              <p style={{ fontFamily: mono, fontSize: 10.5, letterSpacing: "0.1em", textTransform: "uppercase", color: MUTED, margin: 0 }}>/beschikbaarheid</p>
              <span style={{ fontSize: 10.5, color: GREEN, fontWeight: 600, cursor: "pointer" }}>Alle avonden oké ↓</span>
            </div>
            <p style={{ fontSize: 11.5, color: MUTED, margin: "0 0 12px" }}>We hebben alles op <strong>Kan</strong> gezet — pas aan naar <strong style={{ color: "#16a34a" }}>Voorkeur</strong> of <strong style={{ color: MUTED }}>Niet</strong>.</p>

            <div style={{ border: `1px solid ${LINE}`, borderRadius: 10, overflow: "hidden" }}>
              {[
                { day: "Ma 14 apr", time: "18:00 — 19:00", court: "Baan 1", trainer: "Jan P.", state: "pref" },
                { day: "Ma 14 apr", time: "19:00 — 20:00", court: "Baan 1", trainer: "Jan P.", state: "avail" },
                { day: "Wo 16 apr", time: "17:00 — 18:00", court: "Baan 2", trainer: "Sofie R.", state: "avail" },
                { day: "Za 19 apr", time: "10:00 — 11:00", court: "Baan 1", trainer: "Kim V.", state: "unavail" },
              ].map((s, i) => (
                <div key={i} style={{ display: "grid", gridTemplateColumns: "90px 1fr auto", alignItems: "center", padding: "10px 14px", borderBottom: i < 3 ? `1px solid ${LINE}` : "none", background: i % 2 ? "#fdfcf9" : "#fff" }}>
                  <div style={{ fontFamily: mono, fontSize: 11, color: INK }}>{s.day}</div>
                  <div>
                    <div style={{ fontSize: 12.5, fontWeight: 600, color: INK }}>{s.time} · {s.court}</div>
                    <div style={{ fontSize: 11, color: MUTED }}>met {s.trainer}</div>
                  </div>
                  <SegBtns val={s.state} />
                </div>
              ))}
            </div>
          </section>
        </div>

        {/* sticky-ish summary footer */}
        <div style={{ padding: "18px 24px", borderTop: `1px solid ${LINE}`, background: PAPER, display: "flex", alignItems: "center", justifyContent: "space-between", gap: 16 }}>
          <div>
            <div style={{ fontFamily: mono, fontSize: 11, color: MUTED }}>Totaal voor 10 weken</div>
            <div style={{ fontSize: 22, fontWeight: 800, fontFamily: mono, color: INK }}>€320,00</div>
            <div style={{ fontSize: 10.5, color: MUTED }}>Betaling bij bevestiging van je plek</div>
          </div>
          <button style={{ padding: "14px 26px", background: INK, color: "#fff", fontSize: 13, fontWeight: 700, border: "none", borderRadius: 10 }}>Inschrijven →</button>
        </div>
      </div>
    </div>
  </div>
);

const FPInput = ({ label, val, wide }) => (
  <label style={{ gridColumn: wide ? "span 2" : "auto" }}>
    <div style={{ fontSize: 11, color: MUTED, marginBottom: 4 }}>{label}</div>
    <div style={{ height: 38, border: `1px solid ${LINE}`, borderRadius: 8, padding: "0 12px", display: "flex", alignItems: "center", fontSize: 12.5, background: "#fff" }}>{val}</div>
  </label>
);

const SegBtns = ({ val }) => {
  const states = [
    { k: "pref", label: "Voorkeur", bg: "#16a34a" },
    { k: "avail", label: "Kan", bg: "#2563eb" },
    { k: "unavail", label: "Niet", bg: MUTED },
  ];
  return (
    <div style={{ display: "inline-flex", background: "#f0eee9", borderRadius: 8, padding: 2 }}>
      {states.map((s) => (
        <div key={s.k} style={{ padding: "5px 10px", fontSize: 10.5, fontWeight: 600, color: val === s.k ? "#fff" : MUTED, background: val === s.k ? s.bg : "transparent", borderRadius: 6 }}>
          {s.label}
        </div>
      ))}
    </div>
  );
};

// ── CONFIRMATION (proposed) ────────────────────────────────────────────────

const ConfirmationP = () => (
  <div style={{ width: "100%", height: "100%", background: PAPER, overflow: "auto" }}>
    <div style={{ maxWidth: 520, margin: "0 auto", padding: "28px 16px" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 18 }}>
        <div style={{ width: 26, height: 26, borderRadius: 999, background: GREEN, display: "grid", placeItems: "center" }}>
          <I.ball width={13} height={13} />
        </div>
        <span style={{ fontSize: 14, fontWeight: 700, color: INK }}>CoachOS</span>
      </div>

      {/* URGENT — countdown strip */}
      <div style={{ background: "#fee2e2", color: "#991b1b", padding: "10px 14px", borderRadius: 10, marginBottom: 12, display: "flex", alignItems: "center", gap: 8, fontSize: 12 }}>
        <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10" /><path d="M12 6v6l4 2" /></svg>
        <span style={{ fontWeight: 600 }}>Deze link verloopt over <span style={{ fontFamily: mono }}>18 u 24 min</span></span>
      </div>

      <div style={{ background: "#fff", border: `1px solid ${LINE}`, borderRadius: 14, overflow: "hidden" }}>
        {/* greeting */}
        <div style={{ padding: "20px 24px 14px" }}>
          <p style={{ fontSize: 11, color: MUTED, margin: 0, fontFamily: mono }}>Hi Sophie!</p>
          <h2 style={{ fontSize: 20, fontWeight: 800, margin: "4px 0 2px", letterSpacing: -0.4 }}>Je hebt een plek</h2>
          <p style={{ fontSize: 12.5, color: MUTED, margin: 0 }}>Bevestig om je inschrijving vast te leggen.</p>
        </div>

        {/* slot card — big & visual */}
        <div style={{ margin: "0 24px 16px", background: INK, color: "#fff", borderRadius: 12, padding: 20, position: "relative", overflow: "hidden" }}>
          <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 12 }}>
            <div>
              <div style={{ fontFamily: mono, fontSize: 10.5, color: LIME, letterSpacing: "0.1em", textTransform: "uppercase" }}>Maa 14 apr</div>
              <div style={{ fontSize: 28, fontWeight: 800, fontFamily: mono, margin: "4px 0 0" }}>18:00 <span style={{ color: "#8a8377", fontSize: 18 }}>— 19:00</span></div>
            </div>
            <div style={{ textAlign: "right" }}>
              <div style={{ fontFamily: mono, fontSize: 10.5, color: "#8a8377" }}>Totaal</div>
              <div style={{ fontSize: 18, fontWeight: 800, fontFamily: mono }}>€32,00</div>
            </div>
          </div>
          <div style={{ fontSize: 12.5, color: "#e9e4db", display: "flex", flexDirection: "column", gap: 4 }}>
            <div>Padel — Voorjaar 2026</div>
            <div style={{ color: "#a8a195" }}>Baan 1 · Padel Park Antwerpen</div>
            <div style={{ color: "#a8a195" }}>met Jan P.</div>
          </div>
        </div>

        {/* payment */}
        <div style={{ padding: "0 24px 18px" }}>
          <p style={{ fontFamily: mono, fontSize: 10.5, letterSpacing: "0.1em", textTransform: "uppercase", color: MUTED, margin: "0 0 10px" }}>/betaal</p>
          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            <label style={{ padding: 14, border: `2px solid ${GREEN}`, background: "rgba(45,80,22,.04)", borderRadius: 10, display: "flex", alignItems: "center", gap: 12, cursor: "pointer" }}>
              <div style={{ width: 16, height: 16, borderRadius: 999, border: `5px solid ${GREEN}`, background: "#fff" }} />
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 13, fontWeight: 600 }}>Cash bij trainer</div>
                <div style={{ fontSize: 11, color: MUTED }}>Betaal op de eerste les</div>
              </div>
            </label>
            <label style={{ padding: 14, border: `1px solid ${LINE}`, borderRadius: 10, display: "flex", alignItems: "center", gap: 12, cursor: "pointer" }}>
              <div style={{ width: 16, height: 16, borderRadius: 999, border: `1.5px solid ${LINE}` }} />
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 13, fontWeight: 600 }}>Payconiq</div>
                <div style={{ fontSize: 11, color: MUTED }}>Scannen &amp; betalen</div>
              </div>
              <svg width="48" height="14" viewBox="0 0 48 14"><rect width="48" height="14" rx="3" fill="#f0eee9" /></svg>
            </label>
          </div>
        </div>

        {/* actions */}
        <div style={{ padding: "16px 24px 22px", borderTop: `1px solid ${LINE}`, display: "flex", gap: 10 }}>
          <button style={{ flex: 1, height: 46, background: INK, color: "#fff", fontSize: 13, fontWeight: 700, border: "none", borderRadius: 10 }}>
            Plan bevestigen ✓
          </button>
          <button style={{ padding: "0 18px", height: 46, background: "#fff", color: INK, fontSize: 12.5, fontWeight: 600, border: `1px solid ${LINE}`, borderRadius: 10 }}>
            Ander slot
          </button>
        </div>
      </div>

      {/* add-to-calendar preview */}
      <div style={{ marginTop: 14, padding: 14, border: `1px dashed ${LINE}`, borderRadius: 10, display: "flex", alignItems: "center", gap: 10, fontSize: 11.5, color: MUTED }}>
        <I.cal width={15} height={15} />
        Na bevestigen krijg je een .ics bijlage — klik hem open en je les staat in je agenda.
      </div>
    </div>
  </div>
);

// ── WIZARD STEP 3 (proposed) ───────────────────────────────────────────────

const WizardStep3P = () => (
  <Shell active="lessons">
    <div style={{ maxWidth: 1040, margin: "0 auto" }}>
      {/* breadcrumb-style step nav */}
      <div style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 12, fontFamily: mono, marginBottom: 14 }}>
        <span style={{ color: GREEN, cursor: "pointer" }}>✓ 01 basisinfo</span>
        <span style={{ color: MUTED }}>/</span>
        <span style={{ color: GREEN, cursor: "pointer" }}>✓ 02 planning</span>
        <span style={{ color: MUTED }}>/</span>
        <span style={{ color: INK, fontWeight: 700 }}>03 bevestigen</span>
      </div>

      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-end", marginBottom: 18 }}>
        <div>
          <h1 style={{ fontSize: 28, fontWeight: 800, letterSpacing: -0.8, margin: "0 0 4px" }}>Klaar om aan te maken</h1>
          <p style={{ fontSize: 13, color: MUTED, margin: 0 }}>Controleer het overzicht en pas uitzonderingen aan.</p>
        </div>
      </div>

      {/* Summary — the headline operation */}
      <div style={{ background: "#fff", border: `1px solid ${LINE}`, borderRadius: 14, padding: 20, marginBottom: 14, display: "grid", gridTemplateColumns: "repeat(5, 1fr)", gap: 18 }}>
        {[
          ["36", "lessen", GREEN],
          ["12", "weken", INK],
          ["3", "trainers", INK],
          ["€11.520", "omzet (max)", INK],
          ["01.04 → 30.06", "looptijd", MUTED],
        ].map(([v, l, c], i) => (
          <div key={i} style={{ borderLeft: i === 0 ? "none" : `1px solid ${LINE}`, paddingLeft: i === 0 ? 0 : 16 }}>
            <div style={{ fontFamily: mono, fontSize: 22, fontWeight: 800, color: c, lineHeight: 1 }}>{v}</div>
            <div style={{ fontSize: 11, color: MUTED, marginTop: 4, fontFamily: mono, letterSpacing: "0.05em" }}>{l}</div>
          </div>
        ))}
      </div>

      {/* Weekly template — shown ONCE */}
      <div style={{ background: "#fff", border: `1px solid ${LINE}`, borderRadius: 14, padding: 20, marginBottom: 14 }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 14 }}>
          <div>
            <p style={{ fontFamily: mono, fontSize: 10.5, color: MUTED, letterSpacing: "0.1em", textTransform: "uppercase", margin: "0 0 2px" }}>/wekelijks patroon</p>
            <h3 style={{ fontSize: 15, fontWeight: 700, margin: 0 }}>Elke week — behalve de uitzonderingen</h3>
          </div>
          <button style={{ padding: "6px 10px", fontSize: 11, color: MUTED, background: "transparent", border: `1px solid ${LINE}`, borderRadius: 6 }}>Bewerk patroon</button>
        </div>

        <div style={{ display: "grid", gridTemplateColumns: "repeat(7,1fr)", gap: 6 }}>
          {["Ma","Di","Wo","Do","Vr","Za","Zo"].map((d, i) => (
            <div key={d}>
              <div style={{ textAlign: "center", fontSize: 10.5, fontWeight: 700, fontFamily: mono, color: MUTED, padding: "4px 0" }}>{d}</div>
              {i === 0 && <SlotChip time="18:00" court="Baan 1" trainer="Jan P." />}
              {i === 2 && <SlotChip time="17:00" court="Baan 2" trainer="Sofie R." />}
              {i === 5 && <SlotChip time="10:00" court="Baan 1" trainer="Kim V." />}
            </div>
          ))}
        </div>
      </div>

      {/* Exceptions only */}
      <div style={{ background: "#fff", border: `1px solid ${LINE}`, borderRadius: 14, padding: 20, marginBottom: 14 }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 14 }}>
          <div>
            <p style={{ fontFamily: mono, fontSize: 10.5, color: MUTED, letterSpacing: "0.1em", textTransform: "uppercase", margin: "0 0 2px" }}>/uitzonderingen</p>
            <h3 style={{ fontSize: 15, fontWeight: 700, margin: 0 }}>2 weken wijken af</h3>
          </div>
          <button style={{ padding: "6px 10px", fontSize: 11, color: GREEN, background: "transparent", border: `1px solid ${GREEN}`, borderRadius: 6, fontWeight: 600 }}>+ Uitzondering</button>
        </div>

        <div style={{ display: "grid", gap: 8 }}>
          <ExceptionRow week="Week 2" dates="19 – 25 apr" title="Trainer Jan P. op verlof" detail="Kim V. neemt ma + za over" tone="amber" />
          <ExceptionRow week="Week 7" dates="24 – 30 mei" title="Paasweek — geen lessen" detail="3 lessen overgeslagen, €960 minder omzet" tone="red" />
        </div>
      </div>

      {/* Sticky confirm bar */}
      <div style={{ position: "sticky", bottom: 20, background: INK, color: "#fff", padding: "14px 20px", borderRadius: 12, display: "flex", alignItems: "center", justifyContent: "space-between", marginTop: 20, boxShadow: "0 8px 30px rgba(0,0,0,.18)" }}>
        <div>
          <div style={{ fontSize: 11, color: "#8a8377", fontFamily: mono }}>klaar om aan te maken</div>
          <div style={{ fontSize: 14, fontWeight: 600 }}>36 lessen · 12 weken · 18 e-mailuitnodigingen</div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button style={{ padding: "10px 16px", background: "transparent", color: "#e9e4db", fontSize: 12.5, fontWeight: 500, border: "1px solid rgba(255,255,255,.15)", borderRadius: 8 }}>Vorige</button>
          <button style={{ padding: "10px 20px", background: LIME, color: INK, fontSize: 13, fontWeight: 700, border: "none", borderRadius: 8 }}>
            Reeks aanmaken →
          </button>
        </div>
      </div>
    </div>
  </Shell>
);

const SlotChip = ({ time, court, trainer }) => (
  <div style={{ background: "rgba(45,80,22,.06)", border: `1px solid rgba(45,80,22,.2)`, borderRadius: 8, padding: "8px 10px", marginBottom: 6 }}>
    <div style={{ fontFamily: mono, fontSize: 12, fontWeight: 700, color: INK }}>{time}</div>
    <div style={{ fontSize: 10.5, color: MUTED }}>{court}</div>
    <div style={{ fontSize: 10.5, color: MUTED }}>{trainer}</div>
  </div>
);

const ExceptionRow = ({ week, dates, title, detail, tone }) => {
  const bg = tone === "red" ? "rgba(220,38,38,.05)" : "rgba(245,158,11,.08)";
  const border = tone === "red" ? "rgba(220,38,38,.2)" : "rgba(245,158,11,.3)";
  const dot = tone === "red" ? "#dc2626" : "#f59e0b";
  return (
    <div style={{ background: bg, border: `1px solid ${border}`, borderRadius: 10, padding: "12px 14px", display: "flex", alignItems: "center", gap: 14 }}>
      <div style={{ width: 8, height: 8, borderRadius: 999, background: dot }} />
      <div style={{ fontFamily: mono, fontSize: 11, color: MUTED, minWidth: 130 }}>
        <div style={{ color: INK, fontWeight: 700 }}>{week}</div>
        <div>{dates}</div>
      </div>
      <div style={{ flex: 1 }}>
        <div style={{ fontSize: 12.5, fontWeight: 600, color: INK }}>{title}</div>
        <div style={{ fontSize: 11, color: MUTED }}>{detail}</div>
      </div>
      <button style={{ padding: "5px 10px", fontSize: 10.5, color: MUTED, background: "transparent", border: `1px solid ${LINE}`, borderRadius: 6 }}>Bewerk</button>
    </div>
  );
};

// ════════════════════════════════════════════════════════════════════════════

Object.assign(window, {
  NOTES_MORE, NOTES_STATES, PINS,
  RegisterP, StudentLoginP, SettingsP, EnrollP, ConfirmationP, WizardStep3P,
});
