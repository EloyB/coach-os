# Organisatie-offboarding en beveiligde export (#262)

Status: daadwerkelijke lokale implementatie voor het afgebakende niet-financiële pad; draft PR, niet ingeschakeld, niet uitgerold. Dit is geen juridische goedkeuring of bewijs van productieconformiteit. Financiële archivering en externe herstel-/Billitgaranties blijven expliciete gates.

## Levenscyclus en toegang

`OrganizationRetentionClock` gebruikt het werkelijke einde van proef/betaalde periode, niet de eerdere opzeggingsdag. Een actieve verlenging met historische proefdatum wordt niet als verlopen behandeld. Onduidelijke einddatums geven geen automatische verwijderautorisatie.

Met de bestaande `Billing:EnforceTrialExpiry`-toegangsgate krijgen uitsluitend clubbeheerders nog dertig kalenderdagen lees-/exporttoegang; nieuwe inschrijvingen, andere wijzigingen en publieke mutaties worden geweigerd. Na het exportvenster sluit gewone toegang. Exacte bestaande authenticatie-/abonnementsroutes blijven bereikbaar voor herstel/heractivatie. Publieke resources/capabilities bepalen hun eigen tenant, ook bij een niet-gerelateerde ingelogde sessie. De expliciet getargete subscription-repositorylookup negeert de ambient tenantfilter en past onmiddellijk de resource-OrganizationId toe; hierdoor kan een andere JWT-tenant de einddatumcontrole niet omzeilen.

## Werkelijke export

Een bevoegde, actieve, bevestigde clubbeheerder opent Instellingen → Clubgegevens exporteren (`/dashboard/offboarding`). `GET /api/privacy/organization/export` levert rechtstreeks één ZIP met CSV plus JSON: spelers/contacten, groepen, inschrijvingen/antwoorden, planning/losse lessen/reeksen, kampen/dagen, prijsopties/betalingen, instellingen/lidmaatschappen en beschikbare factuurreferenties/status.

De export heeft een repeatable-read snapshot, vaste veilige kolomlijsten, correct gequote CSV en neutralisering van Excel-formuleprefixen. Antwoorden en join-entiteiten controleren beide tenantouders. Globale accountgeheimen, wachtwoorden, authenticatie-/providercredentials en andere-clublidmaatschappen ontbreken. Geen mailbijlage of publieke downloadtoken; HTTP-responses zijn `no-store`. De browser accepteert geen onverwacht JSON/HTML-document als succesvolle ZIP.

Billit-PDF's/bijlagen zijn niet lokaal opgeslagen in dit datamodel en worden niet stilzwijgend als opgenomen voorgesteld. Het manifest noemt deze externe beperking. Contractuele archiefgaranties en een bevoegd extern document-exportpad zijn operator-/juridische gates; er is geen Billit-account of provider aangeroepen.

## Werkelijke verwijdering en bediening

`OrganizationErasureService` voert een transactionele, expliciet geordende tenantverwijdering uit. Geen nieuwe brede cascades. Groeps-/lescycli worden binnen de transactie verbroken; antwoorden gaan via beide ouders. FK-/concurrentiefouten rollen de hele uitvoering terug. Expliciete tabel-writelocks plus korte locktimeout serialiseren met bestaande schrijvers en hercontroleren status, holds, actualiteit en lidmaatschappen voordat iets verdwijnt. Dit is een zeldzame, mogelijk blokkerende onderhoudsoperatie; schaal/lockimpact moet vóór activering beoordeeld worden.

Superadminbediening: Organisaties → organisatie → Organisatieverwijdering beoordelen. Alleen de bestaande SuperAdmin-policy plus actuele DB-superadminbevoegdheid. Vroege verwijdering vereist attestatie van een geverifieerd verzoek van een werkelijke actieve bevoegde clubbeheerder, een preview met aantallen, actuele previewhash en extra exacte organisatienaambevestiging. De attestatie is geen automatische identiteitscontrole: de operator moet het verzoek echt controleren. Een hold is geen overrideknop.

Alleen doelorganisatiegegevens/lidmaatschappen verdwijnen. Accounts met enig ander lidmaatschap (ook inactief), actuele andere-clubreferenties, globale leerlingidentiteit en superadmins blijven behouden. Niet-gedeelde clubaccounts zonder die beschermingen worden daadwerkelijk verwijderd. Globale leerlingidentiteit en globale magic links worden niet op basis van een gedeeld e-mailadres weggehaald.

Een minimale `PrivacyErasureReceipts`-regel met organisatie/target-ID, type en uitvoeringstijd wordt in dezelfde transactie opgeslagen, zonder naam/email/payload. Deze operationele herstelbewijzen zijn geen wettelijke financiële archiefregel. Voordat een oude back-up weer live komt, moeten recentere herstelbewijzen opnieuw toegepast worden; zie #265. Een ontvangstbewijs zonder getest replayproces maakt restore niet vanzelf privacyveilig.

## Notices, retries en operatorwaarschuwing

Een aparte duurzame notice-tabel bewaart uitsluitend organisatie-ID, echte einddatum, soort, ontvanger-user-ID en bezorg-/retrystatus; geen adres of volledige berichtpayload. Alle actieve bevoegde bevestigde clubbeheerders krijgen een vast bericht op het einde en één herinnering vanaf dag83, met expliciete export-/verwijderdatum. Geen spelers/trainers zonder beheerdersrol. Reeds bekende notices zijn uniek en worden niet opnieuw aangemaakt.

Delivery gebruikt atomaire claims met `SKIP LOCKED`, leases/fencing via pogingteller, maximaal acht pogingen met begrensde backoff. Heractivatie en actuele ontvangerbevoegdheid worden vóór SMTP opnieuw gecontroleerd. Aanhoudend mislukte clubnotices maken een unieke duurzame waarschuwing aan actuele CoachOS-superadmins via de bestaande mailvoorziening; ook die aflevering is begrensd/retrybaar. Als er geen geldig superadminadres bestaat of SMTP geheel faalt, blijven foutstatus en waarschuwing in beheerlogs staan voor handmatige opvolging. Geen stilzwijgend succes of termijnverlenging. SMTP kan na acceptatie maar vóór de statuscommit crashen: zoals ieder SMTP-outboxpad is dit at-least-once, geen onbewezen exact-oncegarantie.

Automatische dag90-verwijdering blokkeert wanneer tijdige verplichte notices niet bewezen zijn. Te laat activeren, gewijzigde ontvangers en mislukte bezorging vragen operatorbeslissing; dit is geen goedgekeurd onbeperkt uitstel van de dag90-verplichting.

## Opt-in; standaard niets automatisch uitvoeren

De bestaande optionele `OrganizationRetentionPreview`-sectie behoudt `Enabled=false` en een lege `OrganizationIds`-allowlist. Nieuwe optionele schakelaars: `Apply=false`, `DryRun=true`. Alleen `Enabled=true`, expliciete doel-ID, `Apply=true` én `DryRun=false` laten de dagelijkse worker notices verzenden en het werkelijk geschikte niet-financiële dag90-pad uitvoeren. Alle andere combinaties veroorzaken geen noticewrite, SMTP of verwijdering. Er is geen nieuwe verplichte environmentvariabele. Geen config of dagelijkse taak is in productie gewijzigd.

## Concrete externe gates; geen volledigheidsclaim

- Een privacy-/geschilhold blokkeert. Financiële/contractuele feiten (betalingen, betaalde plannen/prijsopties, voorwaardenacceptaties) gaan naar afzonderlijke beoordeling. Exacte noodzakelijke archiefvelden/termijnen/legal-holdafhandeling zijn niet door Lorenz vastgesteld. De code wist zulke bewijsstukken niet en beweert niet dat het hele dossier onbeperkt mag blijven staan. Het financiële gedeeltelijke-erasure-/archiefpad blijft geblokkeerd op die beslissing.
- Billit-bewaargarantie en export van externe facturen/bijlagen: niet geverifieerd, geen credentials/live toegang gebruikt.
- Herstelbewijs buiten een teruggedraaide database en geteste replay/holdprocedure: operatorgate onder #265, geen veilige productie-enable vóór die gate.
- Lockimpact, correcte lokale tijd/daggrenzen, veilige registratie van geverifieerde verzoeken en tijdige operatoropvolging moeten vóór activering worden gereviewd.
- Alleen lokale synthetische PostgreSQL-, HTTP-, browser- en Mailpitproeven. Geen echte mail, productie/cloud, deployment, merge, hoofdbranchpush of workflowwijziging.
