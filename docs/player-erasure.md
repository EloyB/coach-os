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

`PlayerRetention` in appsettings (productie: Secret Manager / env `PlayerRetention__…`):

| Instelling | Standaard | Betekenis |
|---|---|---|
| `Enabled` | `false` | Bewaartermijn voor spelersdossiers aan/uit |
| `DryRun` | `true` | Enkel tellen en loggen. Geldt ook voor de opruiming hieronder |
| `OrganizationIds` | `[]` | Optionele beperking tot deze clubs; leeg = alle organisaties |
| `CleanupEnabled` | `true` | Opruiming van verstuurde mails en verlopen links (zie onder) |

Dagelijks, per organisatie, voor elke inschrijving waarvan de laatste deelname **meer dan 24 maanden** geleden is
(`PlayerErasureRules.DecideRetention`):

- **Wissen** als niets blokkeert (zoals de handmatige verwijdering).
- **Anonimiseren** als de énige blokkering voltooide betalingen van de speler zelf zijn en hij in geen groep zit.
  De betaling blijft (bedrag, datum, status, Mollie-id) voor de boekhouding van de club.
  Op de inschrijving worden naam (`Geanonimiseerd`), e-mail (uniek, onbruikbaar `…@anonymized.invalid`), telefoon,
  geboortedatum en notities gewist en `AnonymizedAt` gezet; de omschrijving van de betaling (bevat namen) wordt vervangen.
  Formulierantwoorden, voorkeuren, toewijzingen, verplaatsingsverzoeken, bevestigingslinks en verstuurde mails verdwijnen
  zoals bij wissen. Bewijs: `PrivacyErasureReceipt` met `Method = Anonymized`.
- **Overslaan** (geteld als geblokkeerd) bij een open betaling, betalingen in een groep, een groepsleider met andere leden,
  onverwerkte mail, openstaand verplaatsingsverzoek of onvolledige verplaatsingsketen.

Geanonimiseerde rijen zijn geen kandidaat meer en tellen niet mee als "dezelfde speler".
De beslissing wordt binnen de lock opnieuw genomen op verse data. Gelogd worden enkel aantallen per organisatie.
De handmatige verwijdering blijft betalingen blokkeren.

### Opruiming (deel van #261)

Platformbreed, dagelijks, enkel op leeftijd en status (`PrivacyCleanupPolicy`):

- verstuurde outboxmails (`completed`), 30 dagen na verzending — mislukte mails blijven voor onderzoek;
- inloglinks voor spelers (bevatten een e-mailadres), zodra verlopen;
- bevestigingslinks voor lesmomenten, 30 dagen na het verlopen. Beantwoorde, nog geldige links blijven (de planning leest er de status uit), en ook onbeantwoorde links van een toewijzing die nog op bevestiging wacht (de lijst "niet gereageerd" en het afronden van de reeks hebben ze nodig).

### Aanzetten op productie

1. `PlayerRetention__Enabled=true` (DryRun blijft `true`), deployen, de logregels `Spelerretentie …` en `Privacy-opruiming …` in Grafana nakijken.
2. Kloppen de aantallen: `PlayerRetention__DryRun=false`.

De oudste gegevens op productie dateren van april 2026: de eerste spelers bereiken de termijn rond april 2028.
