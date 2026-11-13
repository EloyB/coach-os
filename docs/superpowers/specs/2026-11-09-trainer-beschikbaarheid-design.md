# Trainer-beschikbaarheid: ontwerp

Datum: 2026-11-09. Status: goedgekeurd in gesprek met de gebruiker (brainstorm), klaar voor een plan.

## Waarom

Trainers geven hun beschikbaarheid nu door aan de admin, die ze per trainer overtypt in de dialoog op de Trainers-pagina. Dat is omslachtig. Trainers moeten hun beschikbaarheid zelf kunnen invullen: per weekdag de uren waarop ze kunnen lesgeven.

## Doelen

- Een trainer beheert zijn eigen beschikbaarheid per weekdag, met meerdere tijdvakken per dag en een optionele club per tijdvak ("elke club" is de standaard).
- Een eigen instellingenpagina voor trainers, met "Beschikbaarheid" als eerste sectie. De opzet laat ruimte voor een sectie verlof later.
- Trainer en admin beheren samen **één lijst per trainer**. Wat de trainer invult is meteen zichtbaar voor de admin en bruikbaar in de planning en de slotsuggesties. De trainer mag ook tijdvakken wijzigen of verwijderen die de admin invulde, en omgekeerd.
- Een trainer kan alleen zijn eigen tijdvakken zien en wijzigen. De backend dwingt dat af.

## Niet-doelen

- Verlofdagen, vervanging en meldingen naar de admin (komen later).
- Wijzigingen aan de planning-algoritmes of de slotsuggesties.
- Wijzigingen aan de bestaande admin-dialoog of de admin-endpoints `/trainer-availabilities`.
- Beheerders die zelf lesgeven (de org-instelling `AdminsActAsTrainers`) krijgen deze pagina niet. De pagina is voor de rol Trainer.

## Anti-doel

Een trainer mag nooit tijdvakken van een andere trainer kunnen zien, aanmaken, wijzigen of verwijderen, ook niet door een andere `trainerId` of tijdvak-id mee te sturen.

## Bestaand model (blijft)

`TrainerAvailability`: `TrainerId`, `TennisClubId?` (null = elke club), `DayOfWeek` (0 = maandag, 6 = zondag), `StartTime`, `EndTime`, `IsActive` (soft delete), per organisatie. Geen migratie nodig. Overlap per trainer per dag is niet toegestaan, over alle clubs heen (bestaande regel `HasOverlapAsync`).

## Backend

Nieuwe endpoints, alleen voor de rol Trainer (`RequireRole("Trainer")`). De trainer komt uit het login-token (`GetUserId()`), de organisatie uit het token (`GetOrganizationId()`). Er wordt nooit een `trainerId` uit de aanvraag gelezen.

| Methode | Pad | Doel |
|---|---|---|
| GET | `/me/trainer-availabilities` | eigen tijdvakken (zelfde `TrainerAvailabilityDto`) |
| POST | `/me/trainer-availabilities` | tijdvak toevoegen: `{ tennisClubId?, dayOfWeek, startTime, endTime }` |
| PUT | `/me/trainer-availabilities/{id}` | tijdvak wijzigen (zelfde body) |
| DELETE | `/me/trainer-availabilities/{id}` | tijdvak verwijderen (soft delete) |

Service (`ITrainerAvailabilityService`, nieuwe methoden voor de eigen trainer):
- `GetForTrainerAsync(organizationId, trainerId)`.
- `CreateForTrainerAsync(organizationId, trainerId, request)`: hergebruikt de bestaande regels (club bestaat, overlap).
- `UpdateForTrainerAsync(id, organizationId, trainerId, request)`: het tijdvak moet bij de trainer horen, anders `NotFound` (niet verraden dat het bestaat). Overlapcontrole sluit het tijdvak zelf uit.
- `DeleteForTrainerAsync(id, organizationId, trainerId)`: idem, `NotFound` als het niet van de trainer is.
- `ITrainerAvailabilityRepository.HasOverlapAsync` krijgt een optionele parameter `Guid? excludeId`. `GetByIdAsync` blijft; de eigenaarcheck gebeurt in de service. Een repository-methode `GetByTrainerAsync(organizationId, trainerId)` komt erbij.
- Validatie: dezelfde regels als `CreateTrainerAvailabilityRequestValidator` (weekdag 0-6, tijden `HH:mm`, einde na begin), in een request-record zonder `TrainerId` (`SaveOwnTrainerAvailabilityRequest`).
- Alle fouten als `Result<T>`, geen exceptions. Dutch foutteksten zoals de bestaande service.

## Frontend

- Route `/dashboard/trainer-settings`. De sidebar toont "Instellingen" voor de rol Trainer en wijst naar die route (de admin blijft naar `/dashboard/settings` gaan). De middleware stuurt een niet-trainer van `/dashboard/trainer-settings` terug naar `/dashboard` (UX, de backend blijft de autoriteit).
- Pagina = titel plus secties. Nu één sectie "Beschikbaarheid" (`availability-section.tsx`), zo dat een sectie verlof er later naast kan.
- **Desktop (vanaf `md`)**: weekoverzicht met 7 kolommen en een tijdas. De as loopt standaard van 07:00 tot 23:00 en wordt breder wanneer bestaande tijdvakken daarbuiten vallen (door de admin ingevuld). Tijden snappen op kwartieren. Klik op een leeg stuk of sleep over de as om een tijdvak toe te voegen: een formulier gaat open met dag, start en einde voor ingevuld. Klik op een blok om te wijzigen of te verwijderen. Blokken hebben een kleur per club, "elke club" is groen, met een legenda.
- **Mobiel**: lijst per weekdag met dezelfde tijdvakken, een knop "Tijdvak toevoegen" per dag en hetzelfde formulier.
- Formulier (dialoog): club (standaard "Elke club"), dag (alleen bij toevoegen vanuit de lijst of de bewerking), van, tot, opslaan, verwijderen bij bewerken. Fouten van de API (bijvoorbeeld overlap) worden in het formulier getoond.
- Gedeelde logica in `lib/trainer-availability-layout.ts` met unit tests: as-bereik, positie en hoogte van een blok, snappen op kwartieren, tijd uit een muispositie, overlapdetectie aan de clientzijde (extra gemak, de backend blijft leidend).
- API-client `lib/api/trainerAvailabilities.ts` krijgt de vier `/me`-functies. Teksten in `messages/nl.json` onder een nieuwe namespace `trainerSettings`. Geen hardcoded Nederlands.

## Seed en accounts

De seed activeert nu alleen de hoofdtrainer. De seed wordt uitgebreid met een gewone (niet-hoofd-)trainer met een bekend wachtwoord, zodat er na het draaien klaarstaan:
- Admin: `jan@deaces.be` / `Demo1234!`.
- Trainer: een gewone trainer uit `seed-data.json` met `Demo1234!` (de hoofdtrainer Sophie blijft ook inlogbaar).
De seed vult voor de gewone trainer enkele eigen tijdvakken in zodat de pagina niet leeg opent.

## Tests

- Backend (NUnit, Moq, FluentAssertions): service (eigen tijdvakken ophalen, aanmaken, wijzigen en verwijderen, ander eigenaar = NotFound, overlap, wijziging overlapt niet met zichzelf, onbekende club), validator, repository-methode `excludeId`. Het eigendomsrisico (anti-doel) krijgt expliciete tests.
- Frontend: unit tests voor `trainer-availability-layout.ts` (bun test), Playwright met gemockte API voor de trainerpagina (desktop-grid en mobiele lijst), en een middleware-test dat een admin niet op de trainerpagina blijft.
- Reset + seed end-to-end groen, daarna een gewone trainer en de admin inloggen.

## Door de builder bepaald (te controleren)

- Route `/dashboard/trainer-settings`.
- Kwartier-snapping en as 07:00-23:00, automatisch breder bij bestaande tijdvakken.
- Geen backend-regel voor kwartieren: de frontend snapt, de backend accepteert elke geldige `HH:mm`.
- De admin-endpoints en de admin-dialoog blijven ongewijzigd.
