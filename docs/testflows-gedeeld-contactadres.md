# Manuele testflows — gedeeld contactadres bij inschrijvingen

Branch: `feat/gedeeld-contactadres-inschrijvingen`
Spec: `docs/superpowers/specs/2026-07-22-gedeeld-contactadres-inschrijvingen-design.md`

Kernidee: **wie communicatie ontvangt staat los van wie deelneemt.** Elke inschrijving heeft
een `ContactEmail` (waar alle mail heen gaat, verplicht) en een optioneel eigen `StudentEmail`
(null = communicatie loopt via de contactpersoon). Adressen mogen gedeeld worden; dezelfde
persoon (naam + geboortedatum) mag niet twee keer in een reeks.

---

## 0. Reset + seed (definitieve E2E-check — destructief)

> Wist het `postgres_data`-volume en herbouwt de containers.

```bash
cd backend
bash Scripts/reset-db.sh --no-frontend      # 1. down -v + rebuild
# wacht tot http://localhost:5142/health → 200 (auto-migrate bij startup)
bash Scripts/seed-demo-data.sh              # 2. registratie, clubs, series, enrollments, planning
cd ../frontend && bun dev                   # http://localhost:5317
```

Verwacht:
- De migratie `20260723172051_AddContactEmailToEnrollment` past toe op een lege DB: `ContactEmail`
  wordt gevuld, `StudentEmail` wordt nullable, de partiële unique index `IX_Enrollments_Participant`
  wordt aangemaakt.
- `seed-demo-data.sh` loopt volledig door zonder 4xx/5xx. De gezaaide groep **De Boer** (ouder Sofie
  + kinderen Fien & Stan) gebruikt nu `studentEmail: null` voor de kinderen → gedeeld contactadres.

DB-controle (optioneel):
```bash
docker exec -it coachos_postgres psql -U coachos -d coachos_dev -c \
  "SELECT \"StudentName\", \"ContactEmail\", \"StudentEmail\" FROM \"Enrollments\" WHERE \"StudentName\" LIKE '%De Boer%';"
```
Verwacht: Fien en Stan hebben `ContactEmail = sofie.deboer@gmail.com` en `StudentEmail = NULL`;
Sofie heeft beide gelijk aan haar eigen adres.

---

## 1. Publiek inschrijfformulier — gedeeld contactadres

Pad: `/enroll/{seriesId}` (Inschrijflink kopiëren via dashboard → reeks → Inschrijvingen).

| # | Stap | Verwacht |
|---|---|---|
| 1.1 | Kies "Ik schrijf meerdere personen in" (Groep) | Groepsvelden verschijnen + zin "De contactpersoon ontvangt alle e-mails en de betaallink voor de hele groep." |
| 1.2 | Vul leider in (naam, e-mail, geboortedatum) | Leider-e-mail blijft verplicht |
| 1.3 | Voeg een groepslid toe, checkbox "Dit lid heeft een eigen e-mailadres" **uit** laten | Onder de checkbox: "Alle communicatie loopt via `<leider-e-mail>`", geen e-mailveld |
| 1.4 | Vink de checkbox **aan** | E-mailveld klapt open en is dan verplicht |
| 1.5 | Verstuur met één lid zonder eigen adres | Inschrijving lukt; in de POST-body heeft dat lid `studentEmail: null` |
| 1.6 | Twee leden met identieke **naam + geboortedatum** (of lid = leider) | Inline fout "Deze deelnemer staat al in de groep", geen submit |
| 1.7 | Twee leden, zelfde adres, **verschillende** naam | Toegestaan |

---

## 2. Dubbeldetectie op serverniveau

| # | Stap | Verwacht |
|---|---|---|
| 2.1 | Schrijf een deelnemer in; probeer dezelfde persoon (zelfde reeks, naam, geboortedatum, contactadres) nog eens | 409 "`<naam>` is al ingeschreven voor deze lessenreeks." |
| 2.2 | Twee kinderen op één ouderadres, verschillende namen/geboortedata | Beide toegelaten (adres mag gedeeld) |
| 2.3 | Inschrijving zonder geboortedatum (enkel via API mogelijk) | Geen dubbelblokkade — partiële index geldt enkel bij ingevulde geboortedatum |
| 2.4 | Bij een geweigerde groep | Niets in de DB (transactie-rollback) — check sectie Inschrijvingen |

---

## 3. Admin-weergave (dashboard → reeks → Inschrijvingen)

| # | Stap | Verwacht |
|---|---|---|
| 3.1 | Groepslid zonder eigen adres (Fien/Stan De Boer) | Onder de naam staat "via `sofie.deboer@gmail.com`" i.p.v. een eigen adres |
| 3.2 | Lid met eigen adres | Toont het eigen adres, zonder "via" |
| 3.3 | Twee rijen met hetzelfde contactadres én dezelfde genormaliseerde naam | Amber badge "mogelijk dubbel" op beide rijen |
| 3.4 | Categorie-badge (Jeugd/Volwassenen) | Blijft werken naast de nieuwe badge |

---

## 4. Planningsmail bundelen (mailbox is de test)

> Lokaal gaan mails naar **smtp4dev** — open `http://localhost:5000` (of de smtp4dev-poort uit docker-compose).

| # | Stap | Verwacht |
|---|---|---|
| 4.1 | Genereer planning voor een reeks met de groep De Boer, en bevestig de planning | Voor `sofie.deboer@gmail.com` komt **één** mail binnen, niet drie |
| 4.2 | Inhoud van die mail | Eén blok per deelnemer (Sofie, Fien, Stan) met naam, dag/uur, baan en een **eigen** "Bevestigen of wijzigen"-knop |
| 4.3 | De drie bevestigingslinks | Elk een andere token-URL (elke deelnemer bevestigt apart) |
| 4.4 | Een deelnemer met eigen adres | Krijgt de bestaande enkelvoudige template op zijn eigen adres |
| 4.5 | Onderwerp bij meerdere deelnemers | "Bevestig de lesmomenten voor Sofie, Fien, Stan — `<reeks>`" |

---

## 5. Student-portaal (magic link)

| # | Stap | Verwacht |
|---|---|---|
| 5.1 | Vraag magic link aan voor `sofie.deboer@gmail.com` en log in → Mijn lessen | Aparte regel per deelnemer (Sofie, Fien, Stan), elk met de **deelnemersnaam** erboven |
| 5.2 | Zonder deelnemersnaam | Zouden er meerdere identieke rijen staan — die naam maakt ze onderscheidbaar |
| 5.3 | Lid met eigen adres logt in | Ziet enkel zijn eigen lessen |

---

## 6. Overige verzendmails (regressie — ontdubbeling)

| # | Stap | Verwacht |
|---|---|---|
| 6.1 | Annuleer een lesmoment in een reeks met de groep De Boer | Eén annuleringsmail naar `sofie.deboer@gmail.com`, niet drie |
| 6.2 | Verzet een les in zo'n reeks | Eén verzet-mail per contactadres |
| 6.3 | Bevestigingsmail na inschrijving van de groep | Eén mail naar het contactadres, niet per lid |
| 6.4 | Mollie-betaling van een groepsinschrijving slaagt | Alle leden naar Confirmed (bestaand gedrag, blijft werken) |

---

## 7. Geautomatiseerde checks (al groen op deze branch)

```bash
cd backend && dotnet test CoachOS.slnx          # 372 tests groen
cd ../frontend && bun run build                 # compileert
cd frontend && bun run test:e2e                 # Playwright (draaiende stack nodig)
```

Relevante nieuwe/aangepaste tests:
- `SharedContactEmailTests` — contactadres-resolutie, dubbelcheck op persoon, mail-ontdubbeling
- `ConfirmationBundlingTests` — één mail per contactadres, eigen link per deelnemer
- `MjmlTemplateRenderTests` — de `schedule-confirmation-multi` template compileert en de
  deelnemersblokken (mj-raw) overleven de MJML-compilatie
- `StudentLessonsServiceTests` — portaal-lookup op contactadres + deelnemersnaam
- `SubmitEnrollmentRequestValidatorTests` — gedeeld adres toegestaan, dubbele deelnemer geweigerd
- `enrollment.spec.ts` — groepslid zonder eigen adres wordt als `studentEmail: null` verstuurd

---

## Buiten scope (bewust, aparte iteratie)

- Kampinschrijvingen (`CampEnrollment`) — zelfde probleem, later.
- Eén gecombineerde bevestigingspagina met één betaling voor alle deelnemers.
- Een `ContactName`-veld om mails te openen met de naam van de contactpersoon.
