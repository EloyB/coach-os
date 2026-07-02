# Abonnement, betaling, toegang & facturatie — design

**Datum:** 2026-07-02
**Status:** Goedgekeurd (flow), klaar voor implementatieplan
**Scope:** Self-serve flow waarbij een school een CoachOS-abonnement neemt: account/trial → betaling → toegang → factuur.

## Probleem

Een bezoeker kiest op de website een abonnement (Starter/School/Federatie). Vandaag is er geen enkele weg van "plan gekozen" naar "betalende, toegang hebbende klant met een factuur voor de boekhouding". Concreet ontbreekt alles:

- **Self-registration staat uit** (`/register` → `/login`).
- De `Subscription`-entity is een lege placeholder: **geen repository, service of endpoint**.
- De bestaande Mollie-integratie is **Mollie Connect** — leerlingen betalen de *school* voor lessen via het Mollie-account van de school (`PaymentService` gebruikt `mollieConnect.GetValidAccessTokenAsync(organizationId)`). Dat is de omgekeerde geldstroom; onbruikbaar voor abonnementsbetaling.
- **Toegang tot de app is nergens gekoppeld aan een actief abonnement** — enkel JWT + org-membership.
- **Geen factuur-concept** in het domein.

Dit design beschrijft de volledige flow en het datamodel om dit greenfield te bouwen.

## Vastgelegde beslissingen

| Onderwerp | Keuze |
|---|---|
| Volgorde | **Trial-first**: account → gratis gebruik → betalen om te blijven. Past bij de pilot-marketing. |
| Trial | 60 dagen. **Geen betaalgegevens bij aanmelden.** |
| Betaalmodel | Mollie **recurring** op CoachOS' **eigen** Mollie-account (los van Connect). Mandaat via een eerste betaling bij upgrade, daarna automatische verlenging. |
| Btw | **Altijd 21% Belgische btw**, ongeacht land. (Reverse-charge voor NL/EU-btw bewust niet in scope.) |
| Prijzen | Websiteprijzen zijn **excl. btw** (maand €35/€70/€99; jaar €30/€65/€94 per maand). Aangerekend bedrag = netto × 1,21. |
| Factuur | Eigen **genummerde PDF** per geslaagde betaling; downloadbaar in-app + gemaild. |
| Toegang-gating | Trial afgelopen of betaling gefaald (na grace) → **lock met data-behoud**: login werkt, enkel billing-scherm; rest `403 subscription_required`. |
| Dunning | Gefaalde recurring betaling → `PastDue` → grace (5 dagen) + herinneringsmails → nog onbetaald → `Expired` → lock. |

## Datamodel (nieuw / gewijzigd)

Alle entities krijgen `OrganizationId` en volgen `BaseEntity`. Eén `Subscription` per organisatie (1-op-1).

### `Subscription` (uitbreiden)
Bestaande velden (`Plan`, `MonthlyPrice`, `MollieSubscriptionId`, `MollieCustomerId`, `IsActive`, `StartDate`, `EndDate`) worden aangevuld/vervangen door een expliciete statusmachine:

```
Status            : SubscriptionStatus  // Trialing | Active | PastDue | Canceled | Expired
Plan              : SubscriptionPlan    // Starter | School | (Federatie) — enum hernoemen Professional→School? zie open item
Interval          : BillingInterval     // Monthly | Yearly
TrialEndsAt       : DateTimeOffset?
CurrentPeriodEnd  : DateTimeOffset?      // tot wanneer betaalde toegang loopt
CanceledAt        : DateTimeOffset?
MollieCustomerId  : string?
MollieMandateId   : string?
MollieSubscriptionId : string?
NetMonthlyPrice   : decimal             // netto (excl. btw), bron van waarheid voor facturatie
```

`SubscriptionStatus` semantiek voor gating:
- `Trialing` + `TrialEndsAt > now` → toegang
- `Active` + `CurrentPeriodEnd > now` → toegang
- `PastDue` binnen grace → toegang (met waarschuwingsbanner)
- anders (`Expired`, `Canceled`, grace voorbij) → gelockt

### `BillingProfile` (nieuw, 1-op-1 met org)
Vastgelegd bij de eerste upgrade; nodig op de factuur.
```
CompanyName, VatNumber?, AddressLine, PostalCode, City, Country, InvoiceEmail
```

### `Invoice` (nieuw)
```
Number        : string       // sequentieel per jaar, bv. "2026-0001"
OrganizationId, IssuedAt
Description   : string        // "CoachOS School — maandabonnement juli 2026"
NetAmount     : decimal
VatRate       : decimal = 0.21
VatAmount     : decimal
GrossAmount   : decimal
MolliePaymentId : string
PdfObjectKey  : string        // S3 (Scaleway Object Storage)
Status        : InvoiceStatus // Paid (MVP kent enkel betaalde facturen)
```
Nummering via een transactioneel `InvoiceSequence`-record per jaar (voorkomt gaten/duplicaten onder concurrency).

### Abonnementsbetalingen scheiden van Connect
Abonnementsbetalingen lopen op CoachOS' eigen account en mogen **niet** in de bestaande `Payment`-tabel (die hoort bij enrollment-Connect). Aparte entiteit `SubscriptionPayment` (of minstens een `Source`-discriminator) — MVP: aparte entiteit met `MolliePaymentId`, `Amount`, `Status`, `Subscription`-FK.

## Flows

### A. Signup → trial (self-serve)
1. Website plan-CTA linkt naar `/register?plan=school&interval=monthly`.
2. Registratieformulier weer aanzetten (e-mail, wachtwoord, organisatienaam, sport). `RegisterAsync` maakt admin-`ApplicationUser` + `Organization` + `Subscription{ Status=Trialing, Plan=gekozen, Interval=gekozen, TrialEndsAt = now + 60d }`. **Geen betaling.**
3. JWT → meteen volledige toegang. Dashboard toont trial-banner met resterende dagen + "Kies je abonnement"-CTA.

### B. Upgrade → betaald abonnement
1. In-app **Abonnement**-scherm (of het lock-scherm) → plan + interval kiezen.
2. `BillingProfile` invullen/bevestigen (bedrijfsnaam, adres, btw-nummer optioneel, factuur-e-mail).
3. Backend: Mollie **Customer** aanmaken (CoachOS-account) → **eerste betaling** `sequenceType=first`, bedrag = netto × 1,21, methodes Bancontact + iDEAL → checkout-redirect.
4. Webhook `paid` op de eerste betaling:
   - mandaat ophalen → `MollieMandateId` opslaan
   - Mollie **Subscription** aanmaken (`interval` = "1 month" / "12 months", `startDate` = volgende periode, `mandateId`)
   - `Status=Active`, `CurrentPeriodEnd` = einde eerste periode
   - **Invoice** genereren voor de eerste betaling → PDF → mailen + opslaan
5. Redirect naar succespagina; volledige toegang bevestigd.

### C. Terugkerende verlengingen
Mollie int automatisch per periode via het mandaat en stuurt per betaling een webhook.
- `paid` → `CurrentPeriodEnd` verlengen + nieuwe **Invoice** genereren + mailen
- `failed` / `expired` → `Status=PastDue`, dunning starten (herinneringsmails), grace 5 dagen; nog onbetaald na grace → `Status=Expired` → lock

### D. Toegang-gating
Eén centrale check (endpoint-filter of `ITenantContext`-uitbreiding) resolvet de `Subscription.Status` van de org:
- toegang toegestaan → request loopt normaal
- geen toegang → enkel `/auth/*` en `/billing/*` blijven bereikbaar; alle andere endpoints geven `403 { code: "subscription_required" }`
- Frontend: axios-interceptor vangt `subscription_required` → routeert naar het lock-/billing-scherm.

### E. Facturatie
Bij élke geslaagde betaling (eerste + verlengingen):
- Volgnummer via `InvoiceSequence` (transactioneel).
- PDF renderen met **QuestPDF** (CoachOS-gegevens, `BillingProfile`, netto/btw 21%/bruto, Mollie-ref, factuurnummer, datum).
- Opslaan in S3 (Scaleway Object Storage, bestaande storage-infra).
- Mailen via **Resend** (bestaande e-mailpijplijn / MJML voor de begeleidende mail; PDF als bijlage).
- Downloadbaar via `GET /billing/invoices/{id}/pdf` (org-scoped).

## Mollie-configuratie (kritisch)

Een **tweede Mollie-config** naast de bestaande Connect-setup:
- CoachOS' **eigen** Mollie API-key (`MolliePlatformApiKey`) — géén org-access-token.
- Aparte **webhook-URL** voor abonnementsbetalingen (`/api/webhooks/mollie/subscription`), los van de enrollment-webhook.
- Gebruikt Mollie **Customers**, **Payments** (`sequenceType=first`/`recurring`), **Subscriptions** en **Mandates** API's.
- `IMolliePlatformClient` interface in Domain; implementatie in Infrastructure — bewust gescheiden van `IMollieClient` (Connect) zodat de twee geldstromen niet vermengen.

## Foutafhandeling & edge cases
- Webhook is **idempotent**: dezelfde `MolliePaymentId` mag niet twee facturen maken (uniek-constraint + check).
- Eerste betaling geannuleerd/gefaald → geen subscription, `Status` blijft `Trialing`; gebruiker kan opnieuw proberen.
- Trial verlopen tijdens een openstaande eerste betaling → toegang blijft geblokkeerd tot `paid`.
- Btw-nummer wordt **niet** gevalideerd (altijd 21%); enkel opgeslagen voor op de factuur.
- Plan wijzigen / opzeggen: MVP = opzeggen zet `CanceledAt`, toegang loopt tot `CurrentPeriodEnd`, daarna `Expired`. Proration bewust uit scope.

## Testing
- **Unit**: statusmachine-overgangen (trial→active→pastdue→expired), gating-beslissing per status, btw-berekening (netto→bruto), factuurnummering onder concurrency.
- **Integratie**: webhook-handlers met gemockte Mollie-payloads (paid/failed), idempotentie.
- **Reset+seed**: seed-script uitbreiden zodat een org met `Trialing`-subscription ontstaat; een tweede scenario met `Active`.
- **E2E (Playwright)**: signup→trial→lock-scherm na (gesimuleerd) trial-einde; upgrade-happy-path met Mollie testmode.

## Fasering (elk een eigen implementatieplan)
1. **Trial + access-gating** — `Subscription`-uitbreiding + status-machine, registratie weer aan (maakt trial), gating-filter + `403 subscription_required`, frontend lock-scherm + trial-banner. *Levert waarde zonder geld.*
2. **Betaalde upgrade** — eigen Mollie-config, Customer + first payment + mandaat + Subscription, subscription-webhook, `SubscriptionPayment`.
3. **Facturatie** — `Invoice` + `InvoiceSequence`, QuestPDF-renderer, S3-opslag, Resend-mail, download-endpoint.
4. **Billing-UI** — plan wijzigen, opzeggen, factuurhistoriek.

Fase 1 is het eerste implementatieplan.

## Open items (niet-blokkerend)
- **Enum-naam**: `SubscriptionPlan.Professional` (€79 in code) vs website-tier "School" (€70). De website is nu leidend voor prijs/naam; backend-enum + prijzen moeten hierop afgestemd worden bij implementatie (Starter €35 / School €70 / Federatie €99, excl. btw; jaar −€5/maand). Beslissen in fase 1/2.
- Grace-duur (nu 5 dagen) en trial-duur (60 dagen) als configuratie.
- Bewaartermijn na `Expired` (data-behoud is beslist; definitieve verwijdering later).
