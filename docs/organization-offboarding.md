# Organisatie-offboarding — voorlopige procedure (#262)

> CONCEPT. Niet uitgevoerd, geen verwijderautorisatie en geen bewijs van naleving van 30/90 dagen. De verwijderfunctie is nog niet gebouwd.

## Veilige preview

`OrganizationRetentionPreview:Enabled` is optioneel en standaard false. Alleen de expliciete GUID-lijst `OrganizationRetentionPreview:OrganizationIds` wordt dagelijks bekeken. De worker gebruikt de Brusselse kalenderdatum. Dry-run logt uitsluitend organisatie-ID, vastgestelde einddatum en reviewmelding; geen mail, outboxmutatie, statuswijziging, providers, accountverwijdering of databasewrite.

Alle queries filteren expliciet OrganizationId. Trialing telt alleen met TrialEndsAt, zonder plan, betaalperiode of MollieSubscriptionId. Cancelled telt alleen als CancelAt en CurrentPeriodEnd beide bestaan en gelijk zijn. Vanaf dag90 volgt een reviewmelding, geen verwijdering. Active (ook met historisch TrialEndsAt), Legacy, Locked, ontbrekend abonnement en onduidelijke datums worden overgeslagen. Locked vereist een afzonderlijk goedgekeurde einddatumregel. Heractivatie wordt iedere run opnieuw gelezen. Dit is een conservatieve reviewselectie en geen definitieve juridische einddatumresolver.

## Handmatige procedure — alleen door bevoegde beheerder na goedkeuring

1. Verifieer identiteit/bevoegdheid van de aanvrager en expliciet doel-OrganizationId. Leg reden en verzoek vast zonder onnodige persoonsgegevens. Geen account/e-mail als tenantselector gebruiken.
2. Bevestig daadwerkelijke einddag, open betalingen, refunds/creditnota's, geschillen en legal holds. Bij tegenstrijdige datums of actieve dienstverlening: pauzeer en laat eigenaar beslissen.
3. Bied export aan gedurende de overeengekomen 30 dagen en leg veilige aflevering vast. Bestaande reeks- en kampexports zijn GEEN volledige organisatie-export; losse lessen, instellingen, lidmaatschappen en overige data apart inventariseren. Geen onbewezen volledigheidsclaim.
4. Meld voorgenomen verwijdering vooraf aan bevoegde beheerders; laat termijn, ontvangers en gevolg van bezorgfouten goedkeuren. Er bestaat nog geen nieuwe automatische verwijdermail of duurzame notice-status.
5. Inventariseer per OrganizationId alle afhankelijke data: inschrijvingen, antwoorden via gescopete ouders, voorkeuren, groepen/leiders (cyclische FK's), lessen/verplaatsingen/planning, kampen/dagen/formulieren, bevestigingstokens, outbox, OAuth/Mollie, instellingen, memberships en financiële administratie. Globale magic-link-tokens/users nooit verwijderen op basis van één organisatie of gedeeld adres.
6. Laat financieel archiefmodel expliciet goedkeuren. SubscriptionPayment heeft restrictieve FK's naar Subscription en Organization; facturen tien jaar bewaren is niet opgelost door Organizations.Remove. Minimale archiefvelden, start bewaartermijn en bewijs/legal holds moeten worden beslist. Verwijder niet klakkeloos BillingProfile: open facturen gebruiken de huidige adresgegevens. Verwar clubbetalingen niet met platformfacturen.
7. Bewaar gedeelde users zodra er een ander lidmaatschap of relevante referentie is, ook inactief; bescherm superadmin en globale leerlingidentiteit. Controleer plain UserId/TrainerId-referenties naast echte FK's.
8. Voor daadwerkelijke verwijdering is nog een gereviewde tenant-scoped transactionele implementatie met PostgreSQL FK-, rollback-, concurrency- en negatieve andere-tenanttests nodig. Deze procedure bevat bewust geen ongeteste DELETE-SQL. Tot dat gebouwd/goedgekeurd is: escaleer naar eigenaar/jurist; de preview is niet de uitvoering.
9. Een latere bevoegde uitvoering moet vóór commit status/renewal/leden opnieuw controleren, externe provideracties apart retrybaar afhandelen, voor/na-aantallen en andere-tenantcontroles bewijzen, minimale audit/bevestiging geven en herintroductie na back-uprestore voorkomen. Geen workflow/deploymentactie in deze PR.

## Nog te beslissen/bouwen

- Exacte dag0/30/90-semantiek, wat mag dag31–90 en hoe publieke writes, jobs, webhooks en heractivatie omgaan met read-only?
- Juridische einddatum per status, conflicterende datums, voortijdig superadminverzoek en voorafgaande notificatietermijn/bezorgfalen.
- Financiële archiefmigratie en tienjaarsstart, clubbetalingen en TermsAcceptance.
- Volledige export, toegangsgates, echte deletes/anonimisering en superadmin preview/confirm/audit.
- Backupretentie en restore-verwijderlijst; operationele eigenaar en uitrolbewijs.

De huidige SubscriptionAccessService/middleware zijn niet gewijzigd. Ze bieden nog geen complete 30/90-dagenlevenscyclus en laten sommige anonieme mutaties door. Deze PR is uitsluitend de veilige opt-in voorbereiding plus procedure. Issue blijft open; conceptteksten worden niet gepubliceerd.
