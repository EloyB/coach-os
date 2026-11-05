# Spelerretentie en echte verwijderroute (#261 / PR290)

> CONCEPT / NIET GEMERGD OF UITGEROLD. Geen compliancegarantie. De achttien vastgelegde besluiten gelden; dit document vraagt ze niet opnieuw. De niet-financiële uitvoering is code, niet langer alleen een preview. Financiële veld-/termijnkeuzes blijven expliciete externe reviewgates.

## Wat de code doet

- `PlayerErasureService` verwijdert daadwerkelijk in PostgreSQL: de doelinschrijving, bijbehorende vrije antwoorden, voorkeuren, afgehandelde verplaatsingsgegevens, toewijzingen, bijbehorende bevestigingslinks en voltooide outboxgegevens. Kampen hebben een afzonderlijke getypeerde route met hun eigen antwoorden/groepsverbanden. Geen verwijdering op een globaal e-mailadres.
- Termijn: 24 kalendermaanden sinds laatste clubdeelname; op de kalenderverjaardag wordt de inschrijving in aanmerking genomen. Reekseinddatum, concrete lessen, losse lessen inclusief begrensd gevolgde verplaatsingsketens, kampen en kampdagen tellen mee. Logins, ontvangen mail en audit-UpdatedAt niet. Alle deelnamereads zijn doelorganisatiegescoped.
- Identiteit voor de deelnametermijn: naam+geboortedatum waar beschikbaar; anders naam+spelersadres (StudentEmail, alleen bij ontbreken ContactEmail), binnen dezelfde organisatie. Kampen gebruiken naam+deelnemeradres; een afwijkend oudercontactadres vervangt het spelersadres niet. Een gedeeld gezinsadres alleen verbindt geen spelers. Geen globaal persoonprofiel of claim dat naams-/adreswijzigingen hiermee universeel zijn opgelost; onzekere identiteit vóór opt-in beoordelen.
- Actieve/toekomstige deelname, pending betaling, expliciete `PrivacyHold`, onopgeloste verplaatsing of pending/failedmail blokkeert uitvoering. Alle betalingsbewijs bij het doelrecord en geprijsde bronaanbiedingen (ook zonder betaalrij) geven `financial_review_required`; groepsbetalingen een afzonderlijke reviewhold. Een vroege aanvraag heft die guards niet op.
- Niet-leiders kunnen uit een gratis groep worden verwijderd zonder gezinsgenoten te verwijderen. De laatste deelnemer wordt transactioneel uit de circulaire groepsleider-FK losgemaakt, waarna de lege groep verdwijnt. Een groepsleider met andere leden vereist een expliciet gekozen andere bestaande deelnemer uit dezelfde groep (`newGroupLeaderId`) in preview én bevestiging. De oude groepsnaam wordt daarbij neutraal `Groep`; er is geen automatische willekeurige leidersoverdracht. Kampgroepen volgen hetzelfde patroon.
- Er blijven geen speleridentificeerbare ‘anonieme’ kopieën achter als statistiek. Alleen operationele aantallen en minimale herstelmetadata; pseudonieme identifiers zijn geen anonieme statistiek.

## Beveiligde browserbediening en API

Clubbeheerders openen vanuit het les-/kampinschrijvingsdetail “Privacy: verwijderoverzicht”. De pagina `/dashboard/privacy` gebruikt de bestaande geauthenticeerde APIclient. Selecteer de doelinschrijving, eventueel vroegtijdig verzoek of expliciete groepsleideroverdracht; haal het overzicht op, beoordeel aantallen/blokkades, typ de exacte deelnemersnaam en bevestig dat het bevoegde verzoek/de identiteit werkelijk gecontroleerd zijn. Pas dan is de definitieve actie beschikbaar. Wijziging van scope maakt de preview ongeldig. Mislukte/stale/403-responses tonen nooit succes. Geen persoonsgegevens in de link, alleen getypeerde inschrijvings-ID. Serverautorisatie blijft doorslaggevend.

`POST /api/privacy/enrollments/{id}/erasure`

`POST /api/privacy/camp-enrollments/{id}/erasure`

Alleen JWT-Admin én opnieuw in de transactionele databasecontrole actieve Adminmembership bij de doelorganisatie, met actief bevestigd niet-superadminaccount. Een trainer/andere club/stale membership kan niet previewen of verwijderen. OrganizationId en actor komen uit de geverifieerde sessie, niet uit het requestbody. Superadmin is hier geen tenant-impersonatiebypass.

1. Preview met `{}` of, voor een gecontroleerd vroeg clubverzoek, `{"early":true}`. Dit muteert niets en retourneert eligibility, aantallen, exacte reviewholds en een current-previewhash. Bij groepsleideroverdracht dezelfde beoogde `newGroupLeaderId` meegeven.
2. Na controle identiteit/verzoek en onafhankelijke afhandeling van blockers: dezelfde scope met `apply:true`, dezelfde `early`/nieuwe groepsleider, `previewHash`, `confirmation` (exacte bestaande deelnemersnaam) en `verifiedRequest:true`. Dit is een bewuste per-request opt-in, geen silent delete.
3. De service controleert alles opnieuw. Gewijzigde gegevens, financiële verplichting, hernieuwde deelname/hold of onjuiste bevestiging verhinderen uitvoering. Na werkelijk geslaagde uitvoering: `deleted:true`; herhaling/andere club dezelfde oninformatieve 404.

De previewhash is een optimistic scopebevestiging, geen geheim of alternatieve authenticatie. Een checkbox is geen bewijs van juridische vrijgave; clubbeheerder moet de bevoegde aanvraag werkelijk verifiëren. Expliciete holds worden niet door deze route gewist. Geen automatische e-mail, annulering, betaling of externe provideractie.

## Automatisering en technische cleanup — standaard UIT

Bestaande optionele sectie `PlayerRetentionPreview` blijft compatibel:

- `Enabled=false` standaard.
- `OrganizationIds=[]` standaard; expliciete allowlist vereist.
- `Apply=false` standaard.
- `DryRun=true` standaard.

Enabled+allowlist gebruikt in dry-run dezelfde eligibility-service als apply, voor lessen én kampen, zonder mutaties. Pas `Enabled=true`, expliciete scope, `Apply=true` én `DryRun=false` activeert de echte execution worker; iedere kandidaat krijgt een verse inspectie en transactionele hercontrole. Tussen preview en apply kan geschiktheid terecht veranderen. Geen nieuwe verplichte appconfig of actieve productieplanning.

Onder dezelfde expliciete maintenance-opt-in:
- Globale magiclinks: alleen UsedAt gezet of ExpiresAt verstreken verwijderen, geldige links intact.
- Globale accountinvite/verification-capabilities: uitsluitend bewezen verlopen payload+expiry leegmaken; accounts, memberships, activatiestatus, rate-limitcounters en password/security-gegevens niet wijzigen. Stateless Identity-tokens hebben geen op te schonen databaserij.
- Organisatiegebonden assignmentconfirmationlinks: gebruikt/afgehandeld of verlopen verwijderen; geldige links intact.
- Organisatiegebonden mailoutbox: alleen `Completed` én niet-lege `ProcessedAt` minstens 30dagen geleden. Pending/processing/failed en recente completed intact. De klok is succesvolle afhandeling, niet creatie/AvailableAt.
- Dit verlengt op geen enkele manier de gevoelige kortdurende wachtlijstmailpayload uit PR289. Deze branches worden niet gestapeld.

De account/magiclink-tabellen hebben geen OrganizationId: hun cleanup is expliciete platformsecurity-maintenance, geen e-mailadresgestuurde tenantverwijdering. De overige reads en deletes zijn tenantgescoped, inclusief formfield/form-parentjoins.

## Transacties, herstel en uitvoeringsgates

- Een begrensde PostgreSQL table-write-lock (5seconden lock-timeout) beschermt eligibility en cleanup ook tegen bestaande inschrijving-/betaal-/holdwriters die geen nieuwe advisory lock kennen. Dit serialiseert onderhoud kort over betrokken tabellen; geen schaal-/latentiegarantie voor productie. Geen lange batchtransactie: één kandidaat per transactie.
- Corrupt organisatieoverschrijdend formulier-FK wordt niet ‘opgelost’ door vreemde antwoorden te wissen: echte FK-fout rolt eerdere eigen deletes terug en levert een generieke dependency-reviewhold. Geen SQLpayload of persoonsgegevens naar logs.
- Na daadwerkelijke deletion wordt in dezelfde transactie `PrivacyErasureReceipts` geschreven: uitsluitend OrganizationId, targetkind, target-UUID, CompletedAt; geen naam, adres, antwoorden of tokens. Geen tenant-FK zodat het herstelbewijs het subject kan overleven. SQLschema is expliciet auxiliary operational metadata (niet EF-entiteit) en idempotent aangemaakt; migratiedowngrade mag zulke herstelbewijzen niet stil verwijderen.
- Die minimale receipt is op zichzelf nog geen herstelgarantie. Een oudere DBrestore mist nieuwere tombstones: vóór ingebruikname moet een onafhankelijke actuele journal/suppression-replaybron worden bijgehouden/getest in #265; restore blijft geïsoleerd tot replay+controle. Geen claim dat lokaal geteste deletion reeds cloud-/productierestore bewijst.
- Financiële exact nodige velden, wettelijke termijnen/startdatum, geschilvrijgave en garanties van Billit zijn niet verzonnen. Het geprijsde/betaalde deel heeft expliciete reviewholds en blijft een open acceptatiecriterium, geen onbeperkte goedkeuring om het volledige dossier te bewaren. De aantoonbaar niet-financiële code is wel echt uitvoerbaar.
- Vóór enige latere opt-in: identiteit-/geschilregistratie reconciliëren, holds werkelijk vastleggen, financiële veldbesluiten laten bevestigen, export/herstel/replay oefenen, scope/locks op representatieve volumes beoordelen en gecontroleerd uitrollen. Deze PR machtigt dat niet.

## Bewijs

Echte PostgreSQL- en HTTP-tests met wegwerpdata, geen productie of echte SMTP. Geobserveerde REDs voor technische cleanup, werkelijke player-delete, hold-bypass, geprijsde bron zonder betaalrij, groepscleanup/overdracht, verplaatsingsketen, ontbrekende HTTProute, herstelreceipt en verlopen accountcapabilities. GREEN-tests bewijzen gezins-/tenantisolatie, stale hash, expliciete bevestiging, real concurrent holdwriter, echte restrict-FK en volledige rollback, lesson/camp-free-answers en groepen. De complete backend-suite, lege DBmigratie en volledige seed zijn de finale deliverygate; exacte logs en remote head/CI staan in de PRbody, niet in fictieve screenshots.
