# Gedeeld contactadres bij inschrijvingen

**Datum:** 2026-07-22
**Status:** ontwerp, goedgekeurd

## Probleem

Eén e-mailadres kan vandaag maar één keer voorkomen per lessenreeks: de validator weigert dubbele adressen binnen een verzoek en de unique index `IX_Enrollments_LessonSerieId_StudentEmail` weigert ze in de database. Dat blokkeert legitieme gevallen:

- een ouder schrijft meerdere kinderen in die nog geen eigen adres hebben;
- iemand neemt de communicatie op zich voor een vriendengroep.

Daarnaast: wie wél meerdere deelnemers op één adres krijgt, ontvangt bij het bevestigen van de planning één mail per deelnemer. Dat voelt als spam.

## Uitgangspunt

**Wie communicatie ontvangt staat los van wie deelneemt.** Elke inschrijving heeft een contactadres (waar alles heen gaat) en optioneel een eigen adres van de deelnemer zelf. De versoepeling geldt overal — ook voor losse inschrijvingen in dezelfde reeks, niet alleen binnen een groep.

## Beslissingen

| Onderwerp | Keuze | Waarom |
|---|---|---|
| Reikwijdte | Overal, niet alleen binnen groepen | Ouder die twee kinderen in verschillende niveaus inschrijft moet ook werken |
| Dubbeldetectie | Unique index op `(reeks, contactadres, naam, geboortedatum)` + zachte waarschuwing in de admin-UI | De index vangt dubbelklik en race conditions hard af; de badge vangt de rest |
| Datamodel | `ContactEmail` naast een nullable `StudentEmail` | Intentie expliciet vastgelegd; bundelen wordt een `GROUP BY` |
| Portaal | Login toont wat op het contactadres binnenkomt | Portaal = webversie van de mailbox, geen dubbele rijen |
| Mails | Eén verzamelmail met een aparte link per deelnemer | Lost het spamgevoel op zonder token- of betaalflow te raken |

## 1. Datamodel

`Enrollment` krijgt twee kolommen:

| Kolom | Type | Betekenis |
|---|---|---|
| `ContactEmail` | `string`, verplicht | Waar élke mail voor deze inschrijving heen gaat. Genormaliseerd opgeslagen (trim + lowercase). |
| `StudentEmail` | `string?` (wordt nullable) | Eigen adres van de deelnemer. `null` = communicatie loopt via de contactpersoon. Identiteit en weergave, nooit een verzendadres. |

Invulregels:

- Solo met eigen adres → `ContactEmail = StudentEmail`.
- Groepslid met eigen adres → idem.
- Groepslid zonder eigen adres → `ContactEmail` = adres van de leider, `StudentEmail = null`.

Indexen:

- `IX_Enrollments_LessonSerieId_StudentEmail` (unique) vervalt.
- Nieuw, unique: `(LessonSerieId, ContactEmail, StudentNameNormalized, DateOfBirth)`, partieel met `WHERE DateOfBirth IS NOT NULL`.
- `StudentNameNormalized` is een persisted computed kolom (`lower(trim(StudentName))`), zodat de index niet van de Postgres-collatie afhangt.
- Nieuw, gewoon: index op `ContactEmail` — verzendbundeling en portaal-lookup.

### Blast radius

Ongeveer vijftien plekken gebruiken vandaag `StudentEmail` als verzendadres: `PaymentService`, `LessonRescheduleService`, `EnrollmentService`, `ConfirmationOrchestrationService`, `LessonSerieService`, `PlanningExportService`, `RescheduleService`, `ScheduleAssignmentRepository`, `StudentLessonsService` en de magic-link-flow. Elk daarvan wordt `ContactEmail`. Eén regel per plek, maar het moet volledig zijn: een gemiste plek stuurt naar `null`.

## 2. Contract en validatie

`GroupMemberDto` krijgt een nullable `studentEmail`; `null` betekent "communicatie via de groepsleider". De leider (het hoofdverzoek) houdt `studentEmail` verplicht — er moet altijd één adres zijn dat de communicatie draagt. Bij een solo-inschrijving idem.

`SubmitEnrollmentRequestValidator`:

- `EnrollmentEmails.AreUnique` vervalt; dubbele adressen zijn legaal.
- Nieuw: geen twee groepsleden met identieke genormaliseerde naam én geboortedatum → "Deze deelnemer staat al in de groep." Dit werkt binnen het verzoek, zonder server-lookup, dus zonder e-mail-enumeration op de publieke pagina.
- `studentEmail` van een lid: ingevuld → bestaande formaatregels; leeg of `null` → geldig.

`EnrollmentService.SubmitEnrollmentAsync`:

- De lus met `IsDuplicateAsync` per adres vervalt. In de plaats komt binnen dezelfde SERIALIZABLE-transactie één check op `(reeks, ContactEmail, genormaliseerde naam, geboortedatum)` → 409 `"<naam> is al ingeschreven voor deze lessenreeks."`
- De bestaande `IsUniqueViolation`-vangst (SQLSTATE 23505 → 409) blijft en dekt nu de nieuwe index.
- `ContactEmail` van een lid = `member.StudentEmail ?? adres van de leider`.

`LessonSerieEnrollmentDto` krijgt `contactEmail` en `hasOwnEmail`, zodat de admin-UI het onderscheid kan tonen.

## 3. Mails bundelen

Alleen de planningsmail wordt gebundeld. Bevestiging na inschrijven, annulering en verzetten blijven per gebeurtenis: die zijn zeldzaam en gaan over één les.

In `ConfirmationOrchestrationService.ConfirmScheduleAsync`:

- Token-aanmaak blijft ongewijzigd — één token per toewijzing, één link per deelnemer. Geen wijziging aan `AssignmentConfirmationToken`, `StudentConfirmationService` of de betaalflow.
- De bestaande `emailsToSend`-lijst wordt vóór het verzenden gegroepeerd op genormaliseerd `ContactEmail`.
- Eén ontvanger → huidige template, ongewijzigd.
- Meerdere → nieuwe template `schedule-confirmation-multi.mjml`: één blok per deelnemer met naam, dag en uur, baan, en een eigen "Bevestigen"-knop.

Er is geen automatische herinneringslus: `ResendConfirmationEmailAsync` verstuurt bewust één toewijzing tegelijk, handmatig gestart door een beheerder vanuit het niet-reageerders-overzicht. Die blijft ongebundeld.

Wél gebundeld — of eigenlijk ontdubbeld — worden twee bestaande lussen die per inschrijving mailen en met een gedeeld contactadres hetzelfde postvak meermaals raken: de bevestigingsmail na het indienen van een groepsinschrijving en de lesannulering-/verzetmails. Daar volstaat `DistinctBy(ContactEmail)`.

Foutafhandeling: waar nu één mislukte mail één toewijzing betreft, treft een fout straks N deelnemers. De logregel moet alle betrokken assignment-id's bevatten.

Onderwerpregel: bij één deelnemer ongewijzigd; bij meerdere `"Planning voor <naam1>, <naam2> en <naam3> — <reeks>"`.

## 4. Portaal en UI

**Student-portaal.** `GetByStudentEmailAsync` wordt `GetByContactEmailAsync` en matcht op `Enrollment.ContactEmail`, voor solo én groepsleden. Wie inlogt ziet exact wat er in zijn mailbox landt. `StudentLessonDto` krijgt `participantName` — zonder dat staan er bij een vriendengroep meerdere identieke rijen. De magic-link-flow zelf blijft ongewijzigd: `StudentMagicLinkService` controleert geen inschrijvingen, het is de lookup in `ScheduleAssignmentRepository` die bepaalt wat je na inloggen ziet.

**Publiek inschrijfformulier.** Per groepslid komt onder naam en geboortedatum één checkbox: "Dit lid heeft een eigen e-mailadres". Standaard uit, met daaronder de tekst "Alle communicatie loopt via `<adres van de leider>`". Aanvinken klapt het e-mailveld open. Boven het groepsblok staat één zin die uitlegt dat de contactpersoon alle mails en de betaallink ontvangt.

**Admin-UI, reeks → Inschrijvingen.**

- Een rij zonder eigen adres toont het contactadres met "via" ervoor.
- Rijen met hetzelfde `ContactEmail` én dezelfde genormaliseerde naam krijgen een badge "mogelijk dubbel". Client-side afgeleid uit de lijst die er al is; geen extra endpoint.

## 5. Migratie

Eén migratie, in deze volgorde:

1. `ContactEmail` toevoegen als nullable.
2. Backfill: `ContactEmail = lower(trim(StudentEmail))` voor alle bestaande rijen.
3. `ContactEmail` op NOT NULL zetten.
4. `StudentEmail` nullable maken.
5. `StudentNameNormalized` toevoegen als persisted computed kolom.
6. Oude unique index droppen, nieuwe partiële unique index en de `ContactEmail`-index aanmaken.

De backfill moet vóór de nieuwe index draaien. Bestaande rijen kunnen de nieuwe sleutel al schenden — zelfde adres, zelfde naam, `DateOfBirth = null` bij inschrijvingen van vóór de geboortedatum-feature. Daarom is de index partieel op `WHERE DateOfBirth IS NOT NULL`: historische rijen blokkeren de migratie niet.

## 6. Testen

**Unit tests, `EnrollmentServiceTests`:** groep met drie leden zonder eigen adres; groep met gemengde adressen; dezelfde persoon twee keer → 409; twee kinderen op één adres met verschillende naam → beide toegelaten; leider zonder adres → validatiefout.

**Unit tests, `ConfirmationOrchestrationServiceTests`:** drie toewijzingen op één contactadres → één verzendaanroep met drie blokken; twee contactadressen → twee aanroepen; solo → ongewijzigde template.

**Reset-flow.** `seed-data.json` en `seed-demo-data.py` krijgen minstens één groep met een gedeeld adres, zodat de reset de nieuwe index en de bundeling echt raakt. De feature is pas klaar als `reset-db.sh` gevolgd door `seed-demo-data.sh` groen loopt.

## Buiten scope

- Kampinschrijvingen (`CampEnrollment`) hebben hetzelfde probleem en krijgen een eigen iteratie.
- Eén gecombineerde bevestigingspagina met één betaling voor alle deelnemers op een contactadres. Logische volgende stap zodra dit staat.
- Een `ContactName`-veld om mails te openen met de naam van de contactpersoon in plaats van die van de eerste deelnemer.
