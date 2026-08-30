# Leeftijdsgrens op een lessenreeks

**Datum:** 2026-07-24
**Status:** ontwerp, goedgekeurd

## Probleem

Een lessenreeks heeft geen leeftijdsgrens. Een trainer wil kunnen aangeven voor welke
leeftijden een reeks bedoeld is (bv. een jeugdreeks van 3 t/m 12 jaar), zodat deelnemers
het vooraf zien én zodat inschrijvingen buiten die grens geweigerd worden.

## Uitgangspunt

Elke reeks krijgt een minimum- en maximumleeftijd. De grens wordt getoetst op de
**startdatum van de reeks** (het gaat om de leeftijd tijdens de lessen, niet bij het
inschrijven), met de bestaande, geteste leeftijdsberekening. De grens wordt zowel
publiek getoond als hard afgedwongen bij het inschrijven.

## Beslissingen

| Onderwerp | Keuze |
|---|---|
| Gedrag | Publiek tonen én afdwingen bij inschrijven |
| Peildatum leeftijd | Startdatum van de reeks |
| Range | Inclusief: toegelaten als `MinAge ≤ leeftijd ≤ MaxAge` |
| Defaults | `MinAge = 3`, `MaxAge = 99` |
| Verplicht | Beide velden altijd ingevuld (met defaults) |
| Bewerkbaar | Ja — ook via het bewerken-formulier op de reeksdetailpagina |

## 1. Datamodel & validatie

`LessonSerie` krijgt twee verplichte gehele velden:

| Kolom | Type | Betekenis |
|---|---|---|
| `MinAge` | `int` | Minimumleeftijd (inclusief) op de startdatum. |
| `MaxAge` | `int` | Maximumleeftijd (inclusief) op de startdatum. |

- **Migratie**: kolommen toevoegen met `defaultValueSql` zodat bestaande rijen `MinAge = 3`
  en `MaxAge = 99` krijgen. Daarna blijven de defaults ook op DB-niveau staan (of via de
  entity-initializers `= 3` / `= 99` — de EF-configuratie legt de default vast).
- `CreateLessonSerieRequest` en `UpdateLessonSerieRequest` krijgen `MinAge`/`MaxAge` (`int`).
- `LessonSerieDto` en `PublicLessonSerieDto` krijgen `MinAge`/`MaxAge`.
- `ApplicationMapper` vult de velden bij aanmaken en bij het mappen naar de DTO's.

**Validatie** (`CreateLessonSerieRequestValidator` + `UpdateLessonSerieRequestValidator`):

- `MinAge`: `InclusiveBetween(0, 120)`.
- `MaxAge`: `InclusiveBetween(0, 120)`.
- `MinAge <= MaxAge` → "Minimumleeftijd mag niet groter zijn dan de maximumleeftijd."

## 2. Afdwingen bij inschrijven

De leeftijd wordt berekend met de bestaande
`ParticipantCategoryResolver.CalculateAge(geboortedatum, startdatum)` — dezelfde methode
die de tariefcategorie al gebruikt, inclusief het schrikkeljaar-geval.

In `EnrollmentService.SubmitEnrollmentAsync`, vóór de transactie, komt een check **per
deelnemer** (leider én elk groepslid):

- Leeftijd op `series.StartDate` valt buiten `[series.MinAge, series.MaxAge]` →
  `Result<Guid>.Fail` met `ErrorCodes.Validation` en een duidelijke melding, bv.
  *"Lore De Boer (2 jaar) valt buiten de leeftijdsgrens van deze reeks (3–99 jaar)."*
- Geen bruikbare geboortedatum → geen leeftijdsblokkade (consistent met hoe de categorie
  en de partiële index nu al met een lege geboortedatum omgaan). De validator dwingt de
  geboortedatum in de praktijk al af.

De check gebruikt dezelfde deelnemerslijst die al voor de dubbelcheck wordt opgebouwd
(leider + groepsleden met hun geboortedatum), zodat er geen tweede lus bijkomt.

## 3. UI

**Stap 1 aanmaakwizard (`step-1-basisinfo.tsx`)**: twee getalvelden naast elkaar,
"Min. leeftijd" en "Max. leeftijd", voorgevuld op 3 en 99. Zod-schema: gehele getallen
0–120, met een `refine` dat `minAge ≤ maxAge` afdwingt (foutmelding op het maxAge-veld).
Geplaatst bij "Max. inschrijvingen". Labels via `useTranslations` + `messages/nl.json`.

**Publieke inschrijfpagina (`/enroll/[seriesId]`)**: de range verschijnt bij de reeksinfo
als "Leeftijd: 3–99 jaar" (of "vanaf 3 jaar" / "t/m 99 jaar" indien een grens op de
default staat — optioneel; MVP toont gewoon "3–99 jaar"). `PublicLessonSerieDto` levert
`minAge`/`maxAge`. Client-side spiegelt dezelfde leeftijdscheck de servergrens bij het
invullen, zodat de gebruiker de fout meteen ziet; de server blijft de harde grens.

**Bewerken-formulier (reeksdetailpagina, `EditSeriesForm`)**: dezelfde twee velden, zodat
de grens ook na het aanmaken te wijzigen is. Dit formulier gebruikt hardcoded Nederlandse
strings — daar volg ik het bestaande patroon (geen i18n-sleutels).

## 4. Testen

- **Unit** (`CreateLessonSerieRequestValidatorTests`): `MinAge > MaxAge` faalt; grenzen 0
  en 120 slagen; 121 faalt.
- **Unit** (`EnrollmentServiceTests` / nieuwe cases): deelnemer jonger dan `MinAge` op de
  startdatum → conflict; deelnemer precies `MinAge` en `MaxAge` → toegelaten; groepslid
  buiten de grens → hele inschrijving geweigerd, rollback; kind dat vóór de startdatum de
  minimumleeftijd bereikt → toegelaten (peildatum = startdatum).
- **Reset + seed**: `seed-data.json` bevat reeksen met een expliciete range; de bestaande
  jeugdgroep (De Boer) valt binnen de grens.
- **Frontend**: `bun run build` + de wizard-Zod-validatie (`minAge ≤ maxAge`).

## Buiten scope

- Verschillende leeftijdsgrenzen per lesmoment of per groep binnen één reeks.
- Automatisch de tariefcategorie (Jeugd/Volwassen) uit deze grens afleiden — die blijft
  op `OrganizationSettings.YouthMaxAge` staan.
- Waarschuwen (i.p.v. blokkeren) bij een leeftijd net buiten de grens.
