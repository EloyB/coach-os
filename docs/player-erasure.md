# Spelergegevens verwijderen (#261)

## Handmatig (clubbeheerder)

In het detail van een les- of kampinschrijving staat **Speler verwijderen** (alleen Admin, geen superadmin).
De dialoog toont eerst een overzicht (`GET /api/privacy/{enrollments|camp-enrollments}/{id}/erasure-preview`, neemt geen locks):
wat verdwijnt, of de bewaartermijn verstreken is en waarom verwijderen eventueel geblokkeerd is.
Bevestigen vraagt de exacte spelersnaam (`POST .../erasure`); is de speler groepsleider van een groep met andere leden, dan kiest de beheerder een nieuwe leider.

Verwijderd: de inschrijving, formulierantwoorden, tijdslotvoorkeuren, toewijzingen, verplaatsingsverzoeken, bevestigingslinks en verstuurde outboxmails van die inschrijving (en een groep die daardoor leeg wordt).
Andere inschrijvingen van dezelfde speler/familie, andere clubs en accounts blijven ongewijzigd; ze tellen alleen mee om te blokkeren.

## Wat blokkeert

- actieve of toekomstige deelname (lesreeks, les incl. verplaatsingsketen, kamp/kampdag) of geen bekende deelname;
- **betalingen**: elke betaalrij (open of voltooid) op de inschrijving of haar groep — financiële records moeten bewaard blijven. Een geprijsd aanbod **zonder** betaalrij blokkeert niet. Open betalingen of status `PendingPayment` van dezelfde speler blokkeren ook;
- nog te verzenden of mislukte mail, openstaand verplaatsingsverzoek, inconsistente groep.

## Transactie

Eén transactie per inschrijving: `pg_advisory_xact_lock` per organisatie + `SELECT ... FOR UPDATE` op de doelrij (lock-timeout 5 s),
daarna worden alle regels opnieuw gecontroleerd op de actuele data (optioneel ook `UpdatedAt` uit het overzicht).
Lock-timeout en FK-fout geven elk een eigen foutmelding; er wordt dan niets gewist.
Bij succes wordt in dezelfde transactie een PII-vrije `PrivacyErasureReceipt` geschreven (OrganizationId, Kind, SubjectId, ErasedAt; bewust zonder FK).

## Automatische bewaartermijn — standaard uit

`PlayerRetention` in appsettings: `Enabled=false`, `DryRun=true`, `OrganizationIds=[]`.
Dagelijks, per opgegeven club: dezelfde regels plus "24 maanden na laatste deelname verstreken"; groepsleiders met andere leden worden overgeslagen.
In dry-run worden enkel aantallen gelogd.
