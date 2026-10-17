# Onboarding, abonnement & betaling — plan

**Status:** Fase 1 gebouwd (okt 2026, branch `feat/onboarding-trial-phase1`); Fase 2–5 nog niet. Opgesteld okt 2026, na het live zetten van de prijzen-pagina.
**Doel:** de flow vanaf "Start gratis" op de website tot een werkend account met abonnement en toegang tot de app.
**Bron:** "CoachOS registratie & facturatie" + afstemming over het nieuwe prijsmodel.

## Stand van zaken (huidige code)

**Bestaat al**
- `RegisterEndpoint` (maakt organisatie + admin + lidmaatschap aan, geeft meteen een login terug).
- `Subscription`-entity in Domain.
- Mollie **Connect** (`IMollieClient`, `MollieConnectService`, `PaymentService`): scholen laten hún leerlingen betalen.
- Website-knop "Start gratis" wijst naar `${appUrl}/register`.

**Ontbreekt nog**
- E-mailverificatie, voorwaarden-acceptatie, proefperiode, toegangslogica.
- Een **apart platform-Mollie-account** om de scholen zélf te laten betalen (los van Connect).
- Facturatie via Billit (nummering, btw, Peppol).
- `SubscriptionPlan` is nu een **per-trainer** enum (Starter/Professional/Business/Enterprise, €29–299) — moet naar per-leerling **Game/Set/Match**.
- Entities `BillingProfile`, `TermsAcceptance`, `UsagePeriod` bestaan nog niet.

## Prijsmodel (vastgelegd)

3 tiers per actieve leerling, onbeperkt trainers: **Game** €25/€250 (tot 60), **Set** €49/€490 (tot 200), **Match** €89/€890 (tot 500). Jaarlijks = 2 maanden gratis, elk jaar. 30 dagen gratis proef zonder betaalgegevens. Prijzen excl. btw. Overschrijding boven een marge van 10%: kleine meerprijs per extra leerling, afgerekend bij verlenging. Geen op-maat/multi-club tier voorlopig. Geen early bird.

## Plan in 5 fases

Elke fase is pas "done" na een groene reset + seed.

### Fase 1 — Gratis account + proef
*Kan live zonder Mollie/Billit. Dit maakt "Start gratis" echt werkend.*
- `register` uitbreiden: voorwaarden + verwerkersovereenkomst accepteren (versie + tijdstip → `TermsAcceptance`), e-mailverificatie (link of 6-cijferige code, niet-blokkerend behalve voor mails naar leerlingen/trainers), proef van 30 dagen op de `Subscription` (status + proefeinde), botbescherming (honeypot/Turnstile), idempotency tegen dubbele organisaties.
- `ISubscriptionAccessService`: één plek die bepaalt wat een org mag (alles / respijt / enkel-lezen). Admins zien de betaalmuur; trainers een neutrale melding.
- Frontend (app): echte registratiepagina + verificatieflow, proef-banner met resterende dagen, herinneringsmails (dag 20/27/30).
- Seed-script: auto-verificatie in Development zodat reset-en-seed groen blijft.

**Zoals gebouwd — keuzes die afwijken van of verder gaan dan bovenstaande:**
- Bestaande organisaties kregen status `Legacy`: volledige toegang, geen proef, geen banner, geen herinneringsmails. Hun gebruikers gelden als e-mail-bevestigd.
- E-mailverificatie met een 6-cijferige code (30 min geldig, max. 5 pogingen, nieuwe code na 60 s).
- Botbescherming: honeypot + bestaande rate limiting. Geen Turnstile.
- Geen aparte idempotency-key: één e-mailadres = één organisatie, afgedwongen door de unieke e-mailindex binnen de registratietransactie.
- De enkel-lezen-blokkade bestaat maar staat uit (`Billing:EnforceTrialExpiry=false`) tot Fase 2 een betaalmogelijkheid biedt. Tot dan houdt een verlopen proef volledige toegang en zegt de dag-30-mail "we nemen contact op".
- Voorwaarden en verwerkersovereenkomst worden vastgelegd met versie `concept` (`Registration:TermsVersion` / `Registration:DataProcessingAgreementVersion`) tot de echte teksten er zijn.
- Super-admin ziet per organisatie de abonnementsstatus en kan een proef met 14 dagen verlengen.

### Fase 2 — Betaald worden via Mollie + Billit
*Hierna kunnen scholen zelf betalen.*
- `SubscriptionPlan` ombouwen naar Game/Set/Match, met **vastgelegde** prijs, limiet, marge en meerprijs op het abonnement (prijswijziging raakt bestaande klanten pas bij verlenging).
- Checkout: facturatiegegevens + VIES-check (`BillingProfile`), plankeuze (voorstel op basis van huidig aantal leerlingen), eerste betaling via een **apart platform-Mollie-account** (`IBillingMollieClient`), platform-webhook, Mollie Subscription (12 maanden), betaalmuur bij het eerste moment (eerste formulier publiceren / "Nu activeren" / proef-einde).
- Facturatie: `IInvoicingService` + `BillitInvoicingService` (met fake voor lokaal), via een **outbox** met herhaalpogingen; idempotent per betaling (één factuur per betaling).

### Fase 3 — Factuur & overschrijving
*Voor vzw's/penningmeesters.*
- Mollie `banktransfer` met vervaldatum van 30 dagen, herinneringen, superadmin "markeer als betaald", dubbele betaling automatisch terugstorten + creditnota in Billit.

### Fase 4 — Gebruik & meerprijs
- `UsageService` telt actieve leerlingen per facturatiejaar (`UsagePeriod`), meldingen bij 80/100% van de limiet, upgrade pro rata, verlengmail 14 dagen vooraf, meerprijs afgerekend bij verlenging (één factuur, twee lijnen).

### Fase 5 — Wanbetaling & afloop
- Statusmachine: achterstallig → respijt (14 dagen, publieke formulieren blijven werken) → enkel-lezen (inloggen + exporteren). Chargebacks, bewaartermijn, nooit abrupt afsluiten.

## Punten die we nog moeten bespreken

### Blokkerend (regelen vóór we ver geraken)
1. **Juridisch:** wie schrijft de algemene voorwaarden + verwerkersovereenkomst (DPA)? Jurist of template? Blokkeert Fase 1 (verwerking van gegevens van vaak minderjarige leerlingen).
2. **Mollie platform-account** aanvragen (los van de Connect-app). Blokkeert Fase 2, heeft doorlooptijd.
3. **Billit-account + API** (sandbox eerst). Blokkeert facturatie.

### Prijsmodel-beslissingen
4. **`SubscriptionPlan` migratie** per-trainer → per-leerling: bevestigen dat we de oude enum vervangen en prijzen/limieten op het abonnement vastleggen.
5. **Actieve-leerling-telling:** per seizoen of rollend 12 maanden? (voorstel: per seizoen, intuïtiever). Plus de limieten (60/200/500) toetsen aan échte clubcijfers zodat clubs in de verwachte tier belanden.
6. **Meerprijs** (€3 of hoger) + wanneer we een upgrade voorstellen. Details in `docs/pricing-open-points.md`.
7. **>500 leerlingen:** sinds de Slam-tier weg is — enkel overschrijding op Match, of toch een "praat met ons"-pad?
8. **Data-bewaartermijn** na afloop (bv. 90 dagen). Nodig voor Fase 5 + privacyverklaring.

### Al beslist (ter bevestiging)
9. **Early bird = geschrapt** → de bestaande `IsEarlyBird`-vlag/wachtlijstkoppeling vervalt in dit model.
10. **Proef zonder betaalgegevens** → na 30 dagen nooit automatisch afschrijven; wel naar respijt/betaalmuur.
11. **Prijzen excl. btw** → al zo op de pagina.

## Voorgestelde aanpak

Starten met **Fase 1** (maakt "Start gratis" werkend, kan live zonder externe accounts), en parallel de drie blokkerende aanvragen in gang zetten (juridisch, Mollie-platform, Billit). De blokkerende punten (1–3) en prijsmodel-vragen (4–5) eerst één voor één doornemen voor de bouw van Fase 1 start.

## Zie ook
- `docs/pricing-open-points.md` — openstaande prijsmodel-punten (overschrijding, upgrade-drempels, "1 maand opzeggen"-lek).
- `docs/pricing-strategy.md` — prijsstrategie & unit-economics.
- Bronddocument "CoachOS registratie & facturatie" — volledige technische uitwerking (entities, services, endpoints, edge cases).
