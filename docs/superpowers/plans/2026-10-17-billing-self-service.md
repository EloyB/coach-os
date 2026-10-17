# Self-service abonnement (Mollie + Billit) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Club-admins kiezen zelf Game/Set/Match per maand of jaar en betalen via het Mollie-platformaccount van Studio Swyft (automatische verlenging) of per factuur + overschrijving; elke betaling krijgt een factuur via een (voorlopig fake) Billit-koppeling.

**Architecture:** `Subscription` (fase 1) blijft de bron van de status; een nieuwe tabel `SubscriptionPayments` is per betaalde periode zowel betaalhistoriek als factuur-outbox. Mollie-webhooks (`/webhooks/mollie-billing`) en een terugkeer-sync vanuit de frontend sturen de statusovergangen; een achtergrondjob maakt facturen en verlengt overschrijvings-/deal-abonnementen. Alle externe diensten zitten achter poorten in `Application/Abstractions` met fakes zolang er geen sleutels zijn.

**Tech Stack:** .NET 10, EF Core (PostgreSQL), NUnit + Moq + FluentAssertions (InMemory; Postgres opt-in via `COACHOS_TEST_PG`), Next.js App Router + React Query + next-intl, vitest.

**Spec:** GitHub-issue #267 (EloyB/coach-os) — beslissingen + "Keuzes van de bouwer" 1–9.

## Global Constraints
- Enkel Belgische klanten: `BillingProfile.Country` moet `BE` zijn; btw altijd 21% (`VatRate = 0.21m`), bedrag incl. btw = `Math.Round(net * 1.21m, 2, MidpointRounding.AwayFromZero)`.
- Prijzen en limieten uitsluitend uit `SubscriptionPlanCatalog` (Game 25/250, 60 · Set 49/490, 200 · Match 89/890, 500, excl. btw). Snapshot op het abonnement: prijs, limiet, marge **10%**, meerprijs **€3** per extra leerling per abonnementsjaar.
- Overschrijving enkel met `BillingInterval.Yearly`; betaaltermijn **30 dagen**.
- Eerste betaalde periode start de dag na `TrialEndsAt` als de proef nog loopt, anders vandaag. Periode-einde = start + 1 maand of 12 maanden − 1 dag. Mollie Subscription `startDate` = einde eerste periode + 1 dag (nooit dubbel afschrijven).
- Geen `var` in C#; services geven `Result<T>`; geen hardcoded Nederlandse UI-strings in de frontend (`messages/nl.json`); geen cascade deletes; elke query op tenant-data gefilterd op `OrganizationId`.
- Toegang: `Locked` = enkel-lezen, ongeacht `Billing:EnforceTrialExpiry`. Alle andere statussen volgen fase 1 (volledige toegang).
- Eén EF-migratie in de PR: `ReworkSubscriptionForBilling` wordt na de laatste modelwijziging verwijderd en opnieuw gegenereerd, met SQL die oude `Plan`-waarden op NULL zet voor rijen zonder betaling.
- Webhook-endpoint antwoordt altijd 200 en vertrouwt de body niet (status altijd ophalen bij Mollie).
- Echte Billit-HTTP-client valt buiten dit plan (geen API-toegang om contracten te verifiëren) → apart issue; enkel poort + fake.

## Review Focus
1. **Dubbele of gelijktijdige webhooks** voor dezelfde Mollie-betaling → precies één `SubscriptionPayment`, één factuur, één Mollie Subscription. Test: Postgres-concurrency in Task 5.
2. **Club die tijdens de proef betaalt en daarna opzegt** → terugbetaling, Mollie Subscription geannuleerd, status `Locked`, proef voorbij; een nieuwe checkout start vandaag. Test in Task 7.
3. **Terugkeer van Mollie zonder webhook** (lokaal / webhook vertraagd) → `POST /subscription/checkout/sync` levert dezelfde eindtoestand als de webhook. Test in Task 5.
4. **Checkout terwijl er al een actief of openstaand abonnement is** → `Conflict`, geen tweede Mollie-betaling. Test in Task 4.
5. **Niet-Belgisch facturatieadres of ongeldig btw-nummer / VIES onbereikbaar** → land ≠ BE geweigerd met duidelijke melding; VIES-storing blokkeert niet (`VatVerified = null`). Test in Task 3.

---

### Task 1: Merge main + migratie bovenop fase 1 — **DONE** (`4dff245`)

### Task 2: Domeinmodel en pure periode-/btw-logica

**Files:**
- Modify: `backend/CoachOS.Domain/Entities/Subscription.cs`, `backend/CoachOS.Domain/Enums/SubscriptionStatus.cs`, `backend/CoachOS.Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs`, `backend/CoachOS.Infrastructure/Persistence/ApplicationDbContext.cs`
- Create: `backend/CoachOS.Domain/Enums/BillingPaymentMethod.cs`, `.../Enums/SubscriptionPaymentStatus.cs`, `.../Enums/SubscriptionPaymentKind.cs`, `.../Enums/InvoiceStatus.cs`, `backend/CoachOS.Domain/Entities/BillingProfile.cs`, `backend/CoachOS.Domain/Entities/SubscriptionPayment.cs`, `backend/CoachOS.Domain/Subscriptions/BillingPeriods.cs`, `backend/CoachOS.Domain/Subscriptions/BelgianVat.cs`, configurations `BillingProfileConfiguration.cs`, `SubscriptionPaymentConfiguration.cs`
- Test: `backend/CoachOS.Tests/DomainLogic/BillingPeriodsTests.cs`, `BelgianVatTests.cs`

**Interfaces — Produces:**
- `SubscriptionStatus`: bestaande Legacy=1, Trialing=2, Active=3 + **Cancelled=4**, **Locked=5**.
- `BillingPaymentMethod { Mollie = 1, BankTransfer = 2 }`; `SubscriptionPaymentStatus { Open = 1, Paid = 2, Failed = 3, Canceled = 4, Expired = 5, Refunded = 6 }`; `SubscriptionPaymentKind { First = 1, Recurring = 2, OneOff = 3, BankTransfer = 4, Deal = 5 }`; `InvoiceStatus { NotNeeded = 0, Pending = 1, Created = 2, Failed = 3 }`.
- `Subscription` + : `BillingPaymentMethod? PaymentMethod`, `DateOnly? CurrentPeriodStart`, `DateOnly? CurrentPeriodEnd`, `DateOnly? CancelAt`, `bool IsDeal`, `int? StudentLimit`, `decimal? OverageMarginPercent`, `decimal? OveragePricePerStudent`. **Verwijderen:** `LastPaymentId`, `LastCheckoutUrl`, `LastPaymentStatus`, `LastPaidAt` (vervangen door `SubscriptionPayment`).
- `BillingProfile : BaseEntity` — `Guid OrganizationId` (uniek), `string Name` (200), `string Street` (200), `string PostalCode` (20), `string City` (100), `string Country` (2, default "BE"), `string InvoiceEmail` (256), `string? VatNumber` (20), `bool? VatVerified`.
- `SubscriptionPayment : BaseEntity` — `Guid OrganizationId`, `Guid SubscriptionId`, `SubscriptionPaymentKind Kind`, `BillingPaymentMethod Method`, `SubscriptionPaymentStatus Status`, `SubscriptionPlan Plan`, `BillingInterval Interval`, `DateOnly PeriodStart`, `DateOnly PeriodEnd`, `decimal AmountExclVat` (10,2), `decimal VatRate` (5,4), `decimal AmountInclVat` (10,2), `string? MolliePaymentId` (100, **unique filtered index** where not null), `string? CheckoutUrl` (1000), `DateTime? PaidAt`, `DateOnly? DueDate`, `InvoiceStatus InvoiceStatus`, `int InvoiceAttempts`, `string? InvoiceId` (100), `string? InvoiceNumber` (50), `string? InvoicePdfUrl` (1000), `string? InvoiceLastError` (2000). Index op `(SubscriptionId, PeriodStart)`. Tenant query filter zoals andere entities; FK's `Restrict`.
- `static class BillingPeriods`: `DateOnly FirstPeriodStart(Subscription s, DateOnly today)`; `DateOnly PeriodEnd(DateOnly start, BillingInterval interval)`; `string MollieInterval(BillingInterval interval)` → `"1 month"` / `"12 months"`.
- `static class BelgianVat`: `const decimal Rate = 0.21m`; `decimal Gross(decimal net)`; `static bool IsValidFormat(string vatNumber)` (accepteert `BE0123456789`, `0123.456.789`, spaties/punten; 10 cijfers, eerste 0 of 1); `string Normalize(string vatNumber)` → `BE0123456789`.

- [ ] **Step 1: Schrijf de falende tests** — `BillingPeriodsTests`: `FirstPeriodStart_TrialRunning_StartsDayAfterTrialEnd` (TrialEndsAt 2026-11-04, today 2026-10-17 → 2026-11-05); `FirstPeriodStart_TrialExpired_StartsToday`; `FirstPeriodStart_LegacyOrLocked_StartsToday`; `PeriodEnd_Monthly_IsOneMonthMinusOneDay` (2026-11-05 → 2026-12-04); `PeriodEnd_Yearly` (2026-11-05 → 2027-11-04); `PeriodEnd_MonthlyFromJan31` (2027-01-31 → 2027-02-27); `MollieInterval_Values`. `BelgianVatTests`: `Gross_25_Is30_25`, `Gross_890_Is1076_90`, `IsValidFormat` cases (`BE0123456789` true, `0123.456.789` true, `BE 1234 567 890` true, `BE2123456789` false, `NL123` false), `Normalize`.
- [ ] **Step 2:** `dotnet test --filter "FullyQualifiedName~BillingPeriodsTests|FullyQualifiedName~BelgianVatTests"` → FAIL (types bestaan niet).
- [ ] **Step 3:** Implementeer enums, entities, configuraties, DbSets (`BillingProfiles`, `SubscriptionPayments`) en de twee statische klassen. Pas compileerfouten in bestaande code aan (Eloy's `SubscriptionService`/DTO's gebruiken de verwijderde `Last*`-velden: tijdelijk uit de DTO halen; Task 8 herschrijft de superadmin-kant).
- [ ] **Step 4:** Tests → PASS; `dotnet build CoachOS.slnx` groen.
- [ ] **Step 5:** Migratie: `dotnet ef migrations remove` + `dotnet ef migrations add ReworkSubscriptionForBilling` (herhaal na elke latere modelwijziging; de PR bevat er één).
- [ ] **Step 6: Commit** `feat(billing): domeinmodel voor betalingen, facturatieprofiel en periodes`

### Task 3: Poorten voor Mollie, facturatie en VIES + fakes

**Files:**
- Modify: `backend/CoachOS.Application/Abstractions/IBillingMollieClient.cs`, `backend/CoachOS.Infrastructure/Mollie/BillingMollieClient.cs`, `backend/CoachOS.Infrastructure/Mollie/FakeBillingMollieClient.cs`, `backend/CoachOS.Infrastructure/DependencyInjection.cs`
- Create: `backend/CoachOS.Application/Abstractions/IInvoicingService.cs`, `backend/CoachOS.Infrastructure/Invoicing/FakeInvoicingService.cs`, `backend/CoachOS.Application/Abstractions/IVatNumberValidator.cs`, `backend/CoachOS.Infrastructure/Vat/ViesVatNumberValidator.cs`
- Test: `backend/CoachOS.Tests/Infrastructure/BillingMollieClientTests.cs`, `ViesVatNumberValidatorTests.cs`, `FakeBillingMollieClientTests.cs`

**Interfaces — Produces** (`IBillingMollieClient` uitgebreid; bestaande `CreateCustomerAsync`/`CreatePaymentAsync` blijven):
- `Task<Result<BillingPaymentResult>> CreateFirstPaymentAsync(BillingPaymentRequest request, CancellationToken ct)` — `sequenceType=first`, `customerId` verplicht.
- `Task<Result<BillingPaymentDetails>> GetPaymentDetailsAsync(string paymentId, CancellationToken ct)` met `record BillingPaymentDetails(string Id, string Status, decimal Amount, DateTime? PaidAt, string SequenceType, string? CustomerId, string? SubscriptionId, IReadOnlyDictionary<string,string> Metadata)`.
- `Task<Result<string>> CreateSubscriptionAsync(BillingSubscriptionRequest request, CancellationToken ct)` met `record BillingSubscriptionRequest(string CustomerId, decimal Amount, string Interval, DateOnly StartDate, string Description, string? WebhookUrl, IReadOnlyDictionary<string,string> Metadata)` → Mollie subscription-id.
- `Task<Result> CancelSubscriptionAsync(string customerId, string subscriptionId, CancellationToken ct)`.
- `Task<Result> RefundPaymentAsync(string paymentId, decimal amount, string description, CancellationToken ct)`.
- Fake: houdt betalingen in een singleton-dictionary bij; checkout-URL = `{App:FrontendBaseUrl}/dashboard/settings/subscription?fakePayment={id}`; methode `void SetFakeStatus(string paymentId, string status)` op de concrete fake (gebruikt door Task 5 dev-endpoint).
- `IInvoicingService`: `bool IsConfigured`; `Task<Result<CreatedInvoice>> CreateInvoiceAsync(InvoiceRequest request, CancellationToken ct)`; `record InvoiceRequest(Guid PaymentId, BillingProfileSnapshot Customer, string Description, DateOnly PeriodStart, DateOnly PeriodEnd, decimal AmountExclVat, decimal VatRate, bool IsPaid, DateOnly? DueDate)`; `record BillingProfileSnapshot(string Name, string Street, string PostalCode, string City, string Country, string InvoiceEmail, string? VatNumber)`; `record CreatedInvoice(string Id, string Number, string? PdfUrl)`. Fake: nummer `FAKE-{yyyy}-{0001}` (oplopend, thread-safe), PdfUrl null; idempotent per `PaymentId` (zelfde payment → zelfde factuur).
- `IVatNumberValidator`: `Task<VatCheckResult> CheckAsync(string normalizedVatNumber, CancellationToken ct)` met `enum VatCheckResult { Valid, Invalid, Unavailable }`. VIES: `POST https://ec.europa.eu/taxation_customs/vies/rest-api/check-vat-number` body `{"countryCode":"BE","vatNumber":"0123456789"}` → `valid`; timeout 5 s; elke fout → `Unavailable`.

- [ ] **Step 1: Falende tests** — `BillingMollieClientTests` met een fake `HttpMessageHandler` die het request vastlegt: `CreateFirstPayment_SendsSequenceTypeFirstAndCustomerId`; `CreateSubscription_SendsIntervalStartDateAndAmountAsString` (body: `amount.value == "30.25"`, `amount.currency == "EUR"`, `interval == "1 month"`, `startDate == "2026-12-05"`); `CancelSubscription_SendsDeleteToCustomerSubscription` (`DELETE /v2/customers/cst_1/subscriptions/sub_1`); `Refund_PostsAmountToPaymentRefunds`; `GetPaymentDetails_ParsesSequenceTypeSubscriptionAndMetadata`. `ViesVatNumberValidatorTests`: valid → `Valid`, `valid:false` → `Invalid`, HTTP 500 / timeout → `Unavailable`. `FakeBillingMollieClientTests`: aangemaakte betaling is `open`, `SetFakeStatus(paid)` → `GetPaymentDetails` geeft `paid` met `PaidAt`.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implementeer (Mollie-bedragen als string met 2 decimalen, `CultureInfo.InvariantCulture`). DI: fake als `MollieBilling:ApiKey` leeg is (bestaand patroon), `FakeInvoicingService` altijd (Billit later), `ViesVatNumberValidator` via `AddHttpClient`.
- [ ] **Step 4:** Run → PASS.
- [ ] **Step 5: Commit** `feat(billing): Mollie-recurring, facturatie- en VIES-poorten met fakes`

### Task 4: Facturatieprofiel, overzicht en checkout voor de club-admin

**Files:**
- Create: `backend/CoachOS.Application/Billing/IBillingService.cs`, `BillingService.cs`, `DTOs/` (`BillingOverviewDto`, `BillingProfileDto`, `SaveBillingProfileRequest`, `CheckoutRequest`, `CheckoutResultDto`, `PlanOptionDto`, `SubscriptionPaymentDto`), `Validators/SaveBillingProfileRequestValidator.cs`, `Validators/CheckoutRequestValidator.cs`, `backend/CoachOS.Domain/Interfaces/IBillingProfileRepository.cs`, `ISubscriptionPaymentRepository.cs`, `IActiveStudentCounter.cs` (+ implementaties in `Infrastructure/Repositories/`), endpoints `backend/CoachOS.API/Endpoints/Billing/{GetBillingOverview,SaveBillingProfile,StartCheckout}Endpoint.cs`
- Test: `backend/CoachOS.Tests/Billing/BillingServiceCheckoutTests.cs`, `Validators/SaveBillingProfileRequestValidatorTests.cs`

**Interfaces — Produces:**
- `IBillingService.GetOverviewAsync(Guid organizationId, CancellationToken ct) → Result<BillingOverviewDto>`: status, plan, interval, method, price excl/incl btw, currentPeriodStart/End, cancelAt, trialEndsAt, `isDeal`, `activeStudentCount`, `suggestedPlan`, `plans: List<PlanOptionDto(int Plan, string Name, decimal MonthlyPrice, decimal YearlyPrice, int StudentLimit)>`, `profile: BillingProfileDto?`, `payments: List<SubscriptionPaymentDto>` (nieuwste eerst; factuurnummer + pdf-url), `canCheckout` (bool).
- `SaveBillingProfileAsync(Guid organizationId, SaveBillingProfileRequest r, CancellationToken ct) → Result<BillingProfileDto>`: country moet "BE" (anders `Validation`: "CoachOS is voorlopig enkel beschikbaar voor Belgische clubs. Neem contact op via info@coach-os.be."); btw-nummer genormaliseerd; VIES → `VatVerified` true/false/null; `Invalid` → `Validation` "Dit btw-nummer is niet geldig volgens VIES.".
- `StartCheckoutAsync(Guid organizationId, CheckoutRequest r(int Plan, int Interval, int Method), string returnUrl, CancellationToken ct) → Result<CheckoutResultDto(string? CheckoutUrl)>`:
  - geweigerd (`Conflict`) bij status Active, of Cancelled met `CancelAt >= today`, of een `Open` First-betaling jonger dan 1 uur;
  - geen profiel → `Validation` "Vul eerst je facturatiegegevens in.";
  - `BankTransfer` met `Monthly` → `Validation`;
  - Mollie: customer (indien nodig, opgeslagen op `Subscription.MollieCustomerId`), `SubscriptionPayment` Kind=First/Status=Open met plan/interval/periode/bedragen, `CreateFirstPaymentAsync` (description `CoachOS {Plan} ({maand|jaar}) — {org}`, metadata `subscriptionPaymentId`, redirect `returnUrl`, webhook `{MollieBilling:WebhookBaseUrl}/api/webhooks/mollie-billing` of null als leeg) → `CheckoutUrl` opgeslagen en teruggegeven. Subscription zelf nog niet gewijzigd.
  - BankTransfer: subscription meteen `Active` met plan/interval/method/snapshot/periode; payment Kind=BankTransfer, Status=Open, `DueDate = today + 30`, `InvoiceStatus=Pending`; `CheckoutUrl` null; bevestigingsmail (Task 9).
  - Locked of verlopen proef → periode start vandaag (via `BillingPeriods.FirstPeriodStart`).
- `IActiveStudentCounter.CountAsync(Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct) → int`: distinct `(lower(trim(naam)), lower(trim(e-mail)))` over niet-geannuleerde `Enrollment`s (`StudentName`, `ContactEmail`) en `CampEnrollment`s (`ParticipantName`, `ParticipantEmail`) met `EnrolledAt` in `[from, to]`. Overzicht gebruikt de lopende periode, anders de laatste 365 dagen. Voorstel = kleinste plan met `StudentLimit >= count`, anders Match.
- Endpoints (alle `.RequireAuthorization("Admin")` — gebruik de bestaande admin-policy; geen trainers/hoofdtrainers): `GET /billing`, `PUT /billing/profile` (+ `ValidationFilter`), `POST /billing/checkout` (+ `ValidationFilter`; `returnUrl` = `{App:FrontendBaseUrl}/dashboard/settings/subscription?return=1`). Billing-endpoints vallen buiten de enkel-lezen-blokkade van `SubscriptionAccessMiddleware` (een locked club moet kunnen betalen).

- [ ] **Step 1: Falende tests** — `StartCheckout_Mollie_CreatesOpenFirstPaymentWithGrossAmountAndPeriodAfterTrial` (Set/Monthly, proef tot 2026-11-04 → payment 49.00/59.29, periode 2026-11-05..2026-12-04, subscription nog Trialing); `StartCheckout_BankTransferYearly_ActivatesImmediatelyWithDueDate` (Match/Yearly → Active, payment 890/1076.90, DueDate today+30, InvoiceStatus Pending); `StartCheckout_BankTransferMonthly_IsRejected`; `StartCheckout_WhenActive_ReturnsConflictAndCallsNoMollie`; `StartCheckout_WithoutProfile_ReturnsValidation`; `StartCheckout_FromLocked_StartsToday`; `StartCheckout_ReusesExistingMollieCustomer`; `SaveProfile_NonBelgianCountry_IsRejected`; `SaveProfile_ViesUnavailable_SavesWithVatVerifiedNull`; `SaveProfile_ViesInvalid_IsRejected`; `GetOverview_SuggestsSmallestPlanCoveringActiveStudents` (70 leerlingen → Set). Validatortests: verplichte velden, e-mailformaat, postcode 4 cijfers.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implementeer service, repositories, counter, validators, endpoints; middleware-uitzondering voor `/api/billing`.
- [ ] **Step 4:** Run → PASS.
- [ ] **Step 5: Commit** `feat(billing): facturatieprofiel, overzicht en checkout voor club-admins`

### Task 5: Mollie-webhook en terugkeer-sync (eerste betaling, verlengingen, mislukte betalingen)

**Files:**
- Create: `backend/CoachOS.Application/Billing/IBillingPaymentSyncService.cs`, `BillingPaymentSyncService.cs`, `backend/CoachOS.API/Endpoints/Webhooks/MollieBillingWebhookEndpoint.cs`, `backend/CoachOS.API/Endpoints/Billing/SyncCheckoutEndpoint.cs`, `backend/CoachOS.API/Endpoints/Dev/SetFakeBillingPaymentStatusEndpoint.cs` (enkel Development)
- Test: `backend/CoachOS.Tests/Billing/BillingPaymentSyncServiceTests.cs`, `backend/CoachOS.Tests/Billing/BillingWebhookConcurrencyTests.cs` (Postgres opt-in)

**Interfaces:**
- Consumes: Task 3 Mollie-poort, Task 2 model.
- Produces: `IBillingPaymentSyncService.SyncAsync(string molliePaymentId, CancellationToken ct) → Result` (idempotent); `POST /webhooks/mollie-billing` (anoniem, form `id`, altijd 200, rate limit `public`); `POST /billing/checkout/sync` (admin; synct de nieuwste Open-betaling van de eigen organisatie en geeft `BillingOverviewDto` terug); dev: `POST /dev/billing/fake-payments/{id}/status` body `{status}` → `SetFakeStatus` + `SyncAsync`.
- Overgangen (status altijd uit `GetPaymentDetailsAsync`):
  - **First → paid**: payment Paid + `PaidAt` + `InvoiceStatus=Pending`; subscription `Active`, plan/interval/method=Mollie/snapshot/prijs uit de payment, `CurrentPeriodStart/End` = payment-periode, `CancelAt=null`, `TrialEndsAt` behouden; `CreateSubscriptionAsync` (amount = AmountInclVat, interval via `BillingPeriods.MollieInterval`, `StartDate = PeriodEnd + 1`, metadata `organizationId`) → `MollieSubscriptionId`; bevestigingsmail.
  - **First → failed/canceled/expired**: payment status bijwerken; niets anders.
  - **Onbekend payment-id met `SubscriptionId`** (Mollie-verlenging): zoek subscription op `MollieSubscriptionId`; maak `SubscriptionPayment` Kind=Recurring voor periode `CurrentPeriodEnd + 1 .. PeriodEnd(...)`; bij paid: `CurrentPeriod` opschuiven + factuur Pending; bij failed: OneOff-betaling aanmaken voor diezelfde periode (`CreatePaymentAsync`, customer, geen mandaat) en mail "betaling mislukt" met die link naar admin(s) + superadmin.
  - **OneOff → paid**: payment Paid + factuur Pending (periode was al geregistreerd).
  - Gelijktijdigheid: unieke index op `MolliePaymentId`; `DbUpdateException` op insert = andere instantie was voor → herladen en als verwerkt beschouwen. Statusovergang `Open → Paid` via conditionele `ExecuteUpdate` (`WHERE Status = Open`) zodat maar één instantie de Mollie Subscription aanmaakt.
- [ ] **Step 1: Falende tests** — `Sync_FirstPaid_ActivatesSubscriptionAndCreatesMollieSubscriptionStartingAfterFirstPeriod` (assert `CreateSubscriptionAsync` met `StartDate 2026-12-05`, amount 59.29, `"1 month"`); `Sync_FirstPaid_Twice_CreatesOneMollieSubscription`; `Sync_FirstFailed_LeavesSubscriptionUntouched`; `Sync_RecurringPaid_AdvancesPeriodAndQueuesInvoice`; `Sync_RecurringPaid_SameIdTwice_OnePaymentRow`; `Sync_RecurringFailed_CreatesOneOffLinkAndMailsAdminAndSuperAdmin`; `Sync_UnknownPaymentWithoutSubscription_IsIgnored`. Postgres (`COACHOS_TEST_PG`): `ConcurrentWebhooks_SameFirstPayment_ExactlyOneMollieSubscriptionAndOneInvoiceRow` (8 parallel, 10 rondes, patroon `AuthServiceResendConcurrencyTests`).
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implementeer service + endpoints.
- [ ] **Step 4:** Run → PASS (ook met `COACHOS_TEST_PG`).
- [ ] **Step 5: Commit** `feat(billing): Mollie-webhook voor eerste betaling, verlengingen en mislukte betalingen`

### Task 6: Factuur-outbox en dagelijkse billing-job

**Files:**
- Create: `backend/CoachOS.Application/Billing/IBillingMaintenanceService.cs`, `BillingMaintenanceService.cs`, `backend/CoachOS.Infrastructure/Billing/BillingWorker.cs` (`BackgroundService`)
- Test: `backend/CoachOS.Tests/Billing/BillingMaintenanceServiceTests.cs`

**Interfaces — Produces:** `IBillingMaintenanceService`:
- `ProcessPendingInvoicesAsync(CancellationToken ct) → int`: payments met `InvoiceStatus Pending` (of `Failed` met `InvoiceAttempts < 8` en backoff `2^attempts` minuten); claim via conditionele `ExecuteUpdate` (attempts+1); `CreateInvoiceAsync` met profiel-snapshot, `IsPaid = Status==Paid`, `DueDate`; succes → `Created` + id/nummer/pdf; fout → `Failed` + `InvoiceLastError`; na 8 pogingen mail superadmin.
- `RunDailyAsync(DateOnly today, CancellationToken ct)`:
  - **Overschrijving-verlenging**: Active + `PaymentMethod=BankTransfer` + niet deal + `CancelAt == null` + `CurrentPeriodEnd - today <= 30` en nog geen payment met `PeriodStart = CurrentPeriodEnd + 1` → nieuwe BankTransfer-payment (Open, DueDate = `CurrentPeriodEnd + 1`, factuur Pending, bedrag uit snapshot); is er nog een Open BankTransfer-payment → mail superadmin.
  - **Periode opschuiven** (overschrijving): `today > CurrentPeriodEnd` en er bestaat een payment voor de volgende periode → `CurrentPeriodStart/End` = die periode.
  - **Deal afgelopen**: `IsDeal` en `today > CurrentPeriodEnd` → `Status = Cancelled`, `CancelAt = CurrentPeriodEnd`.
- Worker: facturen elke 30 s; `RunDailyAsync` één keer per Brusselse dag (`TimeProvider.GetBrusselsToday()`), zelfde hosting-patroon als `TrialReminderWorker`.
- [ ] **Step 1: Falende tests** — `ProcessPendingInvoices_PaidMolliePayment_CreatesPaidInvoiceOnce`; `ProcessPendingInvoices_BankTransfer_CreatesUnpaidInvoiceWithDueDate`; `ProcessPendingInvoices_Failure_IncrementsAttemptsAndKeepsRetrying`; `RunDaily_BankTransfer30DaysBeforeEnd_CreatesRenewalPaymentOnce` (twee keer draaien → één rij); `RunDaily_BankTransferWithUnpaidPrevious_NotifiesSuperAdmin`; `RunDaily_CancelledBankTransfer_NoRenewal`; `RunDaily_DealPastEndDate_BecomesCancelled`; `RunDaily_PeriodRollsOverWhenNextPeriodStarts`.
- [ ] **Step 2–4:** FAIL → implementeren (+ registratie `AddHostedService<BillingWorker>`) → PASS.
- [ ] **Step 5: Commit** `feat(billing): factuur-outbox en dagelijkse verlenging voor overschrijving en deals`

### Task 7: Opzeggen, terugbetalen en Locked

**Files:**
- Modify: `backend/CoachOS.Application/Billing/BillingService.cs`, `backend/CoachOS.Application/Subscriptions/SubscriptionAccessService.cs`
- Create: `backend/CoachOS.API/Endpoints/Billing/CancelSubscriptionEndpoint.cs`
- Test: `backend/CoachOS.Tests/Billing/BillingServiceCancelTests.cs`, uitbreiding `backend/CoachOS.Tests/Services/SubscriptionAccessServiceTests.cs`

**Interfaces — Produces:** `IBillingService.CancelAsync(Guid organizationId, CancellationToken ct) → Result<BillingOverviewDto>` (ook gebruikt door superadmin); `POST /billing/cancel` (admin).
- Active, periode al begonnen (`today >= CurrentPeriodStart`) → Mollie Subscription annuleren (indien aanwezig), `Status = Cancelled`, `CancelAt = CurrentPeriodEnd`; geen terugbetaling.
- Active, betaald maar periode nog niet begonnen (`today < CurrentPeriodStart`, d.w.z. betaald tijdens de proef) → Mollie: `RefundPaymentAsync` van de First-payment (volledig bedrag incl. btw) + payment `Refunded` + Mollie Subscription annuleren; BankTransfer: openstaande payment `Canceled` (invoice niet meer aanmaken als nog Pending → `NotNeeded`; al gemaakte factuur → mail superadmin voor creditnota); daarna `Status = Locked`, `TrialEndsAt = today - 1`, plan/periode/`CancelAt` leeg, `MollieSubscriptionId` leeg; mail superadmin "creditnota maken in Billit".
- Niet Active (Trialing/Legacy/Cancelled/Locked) → `Validation` "Er is geen lopend abonnement om op te zeggen."
- `SubscriptionAccessService`: `Locked` → `AccessLevel = ReadOnly` altijd; `Cancelled` en `Active` → Full; DTO krijgt `CancelAt`, `CurrentPeriodEnd`.
- [ ] **Step 1: Falende tests** — `Cancel_ActiveRunningPeriod_CancelsMollieAndSetsCancelAtPeriodEnd`; `Cancel_PaidDuringTrial_RefundsAndLocks` (assert refund 59.29, status Locked, TrialEndsAt = today−1, superadmin-mail); `Cancel_BankTransferDuringTrial_CancelsOpenPaymentAndLocks`; `Cancel_NoRunningSubscription_ReturnsValidation`; `Checkout_AfterLocked_StartsToday` (hergebruik Task 4); access: `GetStatus_Locked_IsReadOnlyEvenWhenEnforcementOff`, `GetStatus_Cancelled_IsFull`.
- [ ] **Step 2–4:** FAIL → implementeren → PASS.
- [ ] **Step 5: Commit** `feat(billing): opzeggen met terugbetaling tijdens de proef en locked-status`

### Task 8: Superadmin — overzicht, deal, opzeggen, overschrijving betaald, uitnodiging

**Files:**
- Modify: `backend/CoachOS.Application/Subscriptions/ISubscriptionService.cs`, `SubscriptionService.cs`, `DTOs/SubscriptionDto.cs`
- Delete: `backend/CoachOS.API/Endpoints/SuperAdmin/{SetSubscriptionActive,CreateSubscriptionPaymentLink,RefreshSubscriptionPayment,MarkSubscriptionPaid,AssignSubscription}Endpoint.cs`, `DTOs/SetSubscriptionActiveRequest.cs`, `DTOs/AssignSubscriptionRequest.cs` + validator, Eloy's `SubscriptionServiceTests` die over die methodes gaan (herschrijven)
- Create: endpoints `SuperAdmin/{AssignDeal,MarkSubscriptionPaymentPaid,CancelOrganizationSubscription,SendPaymentInvitation}Endpoint.cs`, `DTOs/AssignDealRequest.cs` + validator
- Test: `backend/CoachOS.Tests/Services/SubscriptionServiceTests.cs` (herschreven)

**Interfaces — Produces** (`ISubscriptionService`, superadmin, cross-org via `IgnoreQueryFilters` zoals Eloy):
- `GetForOrganizationAsync(Guid organizationId) → Result<SubscriptionDto?>`: status, plan, interval, method, prijs, periode, cancelAt, trialEndsAt, isDeal, profiel, payments (incl. factuurnummer/-status, `InvoiceLastError`), `billingConfigured`, `invoicingConfigured`.
- `AssignDealAsync(Guid organizationId, AssignDealRequest(int Plan, decimal Price, string EndDate), ...)`: geweigerd als er een lopend Mollie-abonnement is (`Conflict`); `Status Active`, `IsDeal`, plan, prijs (excl. btw; ≥ 0), periode today..EndDate (EndDate > today), method BankTransfer; prijs > 0 → payment Kind=Deal, BankTransfer, Open, DueDate today+30, factuur Pending (vereist profiel, anders `Validation`); prijs 0 → geen payment.
- `MarkPaymentPaidAsync(Guid organizationId, Guid paymentId)`: enkel BankTransfer/Deal-payments met status Open → Paid + `PaidAt`; status van het abonnement verandert niet.
- `CancelAsync(Guid organizationId)` → delegeert naar `IBillingService.CancelAsync`.
- `SendPaymentInvitationAsync(Guid organizationId)`: enkel Legacy; mailt de admins de uitnodiging (Task 9) met link `{App:FrontendBaseUrl}/dashboard/settings/subscription`.
- `GetPlanCatalog()` blijft. Proef verlengen (`ExtendTrialAsync`, fase 1) blijft ongewijzigd.
- Endpoints (`SuperAdmin`-policy): `POST /super-admin/organizations/{id}/subscription/deal`, `POST .../subscription/payments/{paymentId}/mark-paid`, `POST .../subscription/cancel`, `POST .../subscription/invite`. `GET .../subscription` en `GET /super-admin/subscription-plans` blijven.
- [ ] **Step 1: Falende tests** — `AssignDeal_PriceZero_ActivatesWithoutPayment`; `AssignDeal_PricePositive_CreatesBankTransferPaymentAndInvoice`; `AssignDeal_WithRunningMollieSubscription_Conflict`; `AssignDeal_EndDateInPast_Validation`; `MarkPaymentPaid_OpenBankTransfer_SetsPaid`; `MarkPaymentPaid_MolliePayment_Validation`; `SendPaymentInvitation_NonLegacy_Validation`; `SendPaymentInvitation_Legacy_MailsAllActiveAdmins`.
- [ ] **Step 2–4:** FAIL → implementeren, oude endpoints/DTO's/tests verwijderen → PASS.
- [ ] **Step 5: Commit** `feat(superadmin): abonnement-overzicht met deal, opzeggen, overschrijving en uitnodiging`

### Task 9: E-mails

**Files:**
- Modify: `backend/CoachOS.Application/Abstractions/IEmailService.cs`, `backend/CoachOS.Infrastructure/Email/EmailService.cs`
- Create: templates `backend/CoachOS.Infrastructure/Email/Templates/{billing-subscription-started,billing-payment-failed,billing-invitation,billing-superadmin-notice}.mjml`
- Test: `backend/CoachOS.Tests/Services/MjmlTemplateRenderTests.cs` (uitbreiden)

**Interfaces — Produces** (`IEmailService`):
- `SendSubscriptionStartedAsync(string toEmail, string organizationName, string planName, string intervalLabel, decimal amountInclVat, DateOnly periodStart, bool bankTransfer, DateOnly? dueDate, string manageUrl, CancellationToken ct)`
- `SendSubscriptionPaymentFailedAsync(string toEmail, string organizationName, decimal amountInclVat, string paymentUrl, CancellationToken ct)`
- `SendSubscriptionInvitationAsync(string toEmail, string organizationName, string subscribeUrl, CancellationToken ct)`
- `SendBillingSuperAdminNoticeAsync(string toEmail, string subject, string body, string? link, CancellationToken ct)` — gebruikt voor: mislukte betaling (kopie), onbetaalde vorige factuur, creditnota nodig, factuur 8× mislukt.
- Bedragen in `nl-BE` (`€ 59,29`), datums `dd/MM/yyyy`. Elke template volgt het bestaande layout-partial; geen Google Fonts (test uit #256 dekt dat automatisch).
- [ ] **Step 1:** Rendertests per template (`*_RendersAmountDateAndLink`, geen `{{`).
- [ ] **Step 2–4:** FAIL → templates + methodes → PASS. De aanroepen in Tasks 4–8 gebruiken deze methodes (in die tasks via `Mock<IEmailService>` geverifieerd).
- [ ] **Step 5: Commit** `feat(billing): e-mails voor start, mislukte betaling, uitnodiging en superadmin-meldingen`

> Uitvoeringsvolgorde: Task 9 vóór Task 4 uitvoeren (Tasks 4–8 roepen de mailmethodes aan).

### Task 10: Frontend — Instellingen > Abonnement en banner

**Files:**
- Create: `frontend/lib/api/billing.ts`, `frontend/lib/billing.ts` (pure helpers), `frontend/lib/billing.test.ts`, `frontend/app/(dashboard)/dashboard/settings/subscription/page.tsx`, `frontend/components/billing/{plan-picker,billing-profile-form,payment-history,subscription-status-card,cancel-subscription-dialog}.tsx`
- Modify: `frontend/app/(dashboard)/dashboard/settings/page.tsx` (link-kaart "Abonnement", enkel admins), `frontend/components/dashboard/account-banners.tsx` (CTA "Kies een plan" voor admins bij Trialing; Locked-banner), `frontend/lib/api/subscription.ts` (+ `Cancelled`/`Locked`, `cancelAt`, `currentPeriodEnd`), `frontend/messages/nl.json` (namespace `billing`)

**Interfaces — Produces:**
- `lib/billing.ts`: `grossAmount(net: number): number` (×1.21, 2 decimalen); `suggestPlan(plans: PlanOption[], activeStudents: number): PlanOption`; `exceedsLimit(plan: PlanOption, activeStudents: number): boolean` (> limiet × 1.10); `canPayByBankTransfer(interval: "Monthly" | "Yearly"): boolean`.
- Pagina-gedrag: statuskaart (Proef tot … / Actief tot … / Opgezegd, loopt tot … / Locked); plankeuze met maand/jaar-schakelaar (prijzen excl. btw + "incl. btw € …"), voorstel-badge op het voorgestelde plan, waarschuwing bij `exceedsLimit` (meerprijs €3 per extra leerling per jaar, aangerekend bij verlenging); betaalwijze (Mollie / factuur + overschrijving; overschrijving uitgeschakeld bij maand); facturatieprofiel-formulier (react-hook-form + zod, land vast "België"); knop afrekenen → `window.location.href = checkoutUrl`; bij `?return=1` of `?fakePayment=…` eerst `POST /billing/checkout/sync`, dan resultaatmelding; in Development bij `?fakePayment=` knoppen "Betaling geslaagd / mislukt" die het dev-endpoint aanroepen; betaalhistoriek met factuurnummer en pdf-link; opzeggen-dialoog (tekst verschilt: "loopt tot {datum}" vs. "je betaling wordt terugbetaald en je account wordt vergrendeld tot je een plan kiest").
- Trainers en hoofdtrainers: route redirect naar `/dashboard` (middleware-regel voor `/dashboard/settings/subscription`, zie memory "nieuwe route → middleware").
- [ ] **Step 1:** vitest `lib/billing.test.ts`: `grossAmount(25) === 30.25`, `grossAmount(890) === 1076.9`; `suggestPlan` (70 → Set, 600 → Match, 0 → Game); `exceedsLimit(Game, 66) === false`, `(Game, 67) === true`; `canPayByBankTransfer`.
- [ ] **Step 2–4:** FAIL → helpers, API-client, pagina, componenten, banner, nl.json → PASS; `bunx tsc --noEmit`, `bun run build`.
- [ ] **Step 5: Commit** `feat(billing): abonnementspagina voor club-admins en plan-CTA in de banner`

### Task 11: Frontend — superadmin-detailpagina herwerken

**Files:**
- Modify: `frontend/app/(super-admin)/super-admin/organizations/[id]/page.tsx`, `frontend/app/(super-admin)/super-admin/organizations/page.tsx`, `frontend/components/super-admin/subscription-badge.tsx`, `frontend/lib/api/super-admin.ts`, `frontend/messages/nl.json`
- **Interfaces — Consumes:** Task 8 endpoints.
- Detailpagina: statuskaart (status, plan, interval, methode, periode, cancelAt, proefeinde + bestaande "proef verlengen"), facturatieprofiel (read-only, btw geverifieerd ja/nee/onbekend), betalingen/facturen met "Markeer als betaald" bij open overschrijvingen, factuurfout zichtbaar, acties "Deal toekennen" (plan, prijs excl. btw, einddatum), "Opzeggen namens de club" (bevestiging), "Uitnodiging tot betalen sturen" (enkel Legacy). Verwijderd: Activeer/Deactiveer, betaallink maken, status verversen, plan toekennen. Lijst: badge toont status + plan.
- [ ] **Step 1–3:** API-client aanpassen → pagina → `bunx tsc --noEmit` + `bun run build` groen.
- [ ] **Step 4: Commit** `feat(superadmin): abonnement-detailpagina afgestemd op self-service billing`

### Task 12: Config, secrets, docs, finale migratie, reset + seed, PR

**Files:**
- Modify: `infrastructure/terraform/secrets.tf` (manuele secret `MollieBilling__ApiKey`; `MollieBilling__WebhookBaseUrl` = `https://app.${var.domain_name}`), `infrastructure/SECRETS.md`, `backend/CoachOS.API/appsettings.Development.json` (geen sleutel = fake), `docs/onboarding-billing-plan.md` (fase 2 "zoals gebouwd"), seed-scripts indien contract wijzigt
- [ ] **Step 1:** Migratie definitief opnieuw genereren (remove + add) en SQL toevoegen: `UPDATE "Subscriptions" SET "Plan" = NULL WHERE "Status" IN (1, 2);`
- [ ] **Step 2:** `dotnet test CoachOS.slnx` (+ `COACHOS_TEST_PG`) groen; frontend `bunx tsc --noEmit`, `bun run test:unit`, `bun run build` groen.
- [ ] **Step 3:** Reset + seed groen; handmatige E2E via de app met fake Mollie: proef → plan kiezen (Mollie) → fake "geslaagd" → Actief + factuur FAKE-…; opzeggen tijdens proef → Locked; overschrijving jaarlijks → Actief + open factuur → superadmin markeert betaald; deal €0.
- [ ] **Step 4:** Follow-up issues: echte Billit-HTTP-client (zodra API-toegang er is); meerprijs aanrekenen (fase 4); Mollie Subscription die Mollie zelf stopzet bij een ongeldig mandaat (herstel via nieuwe `first`-betaling die de Subscription opnieuw aanmaakt — keuze 4 tweede helft uit #267).
- [ ] **Step 5:** Commit, push `feat/billing-self-service`, PR openen (verwijst naar #267, vermeldt de externe actiepunten). Niet mergen.
