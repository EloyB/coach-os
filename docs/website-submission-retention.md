# Websiteformulierdata en persoonlijke wachtlijstmail (#264 / PR #289)

> Reviewbare code, niet gemergd/uitgerold. Geen live configuratie, persoonsgegevens of back-ups onderzocht. Juridische concepten blijven concepten; geen complianceclaim. Wachtlijst behouden; #295 blijft later werk. Formulieren zijn niet onverwacht openbaar gemount.

## Besloten beleid

- Lancering 2026-11-15; wachtlijsteinde 2027-05-15 00:00 UTC. Backend heeft deze grens zonder nieuwe verplichte configuratie; bestaande optionele WEBSITE_LAUNCH_DATE is alleen voor legacybeheer/testfixtures. Geen afwijkende legacydatum gebruiken bij uitrol.
- Contactformulier: twaalf kalendermaanden na ontvangst, niet na latere mailboxactiviteit. Gewone info-/demomails: afzonderlijke mailboxrichtlijn twaalf maanden na afhandeling, lopende relatie/afspraken/geschillen apart; geen mailboxautomatisering.
- Website → specifieke CoachOS-API → bestaande PostgreSQL. Geen brede DBcredentials op website, geen nieuw providerabonnement.

## Contactopslag

`POST /api/contact` op de website valideert schema en honeypot en proxy't uitsluitend naam, e-mail, bericht en optionele organisatie naar `POST /api/website/contact`. De backend valideert opnieuw, begrenst velden en slaat in WebsiteContactEntries op. De server kiest de ontvangstdatum; een publieke client kan die niet verlengen/vervalsen. Geen IP/honeypot/authheaderopslag, geen e-mailadres in applicatielogs, geen JSONfallback als de API faalt. Upstreamstoringen leveren 503 en geen vals success op. WEBSITE_API_URL is optioneel; standaard http://api:8080/api.

De bestaande platformworker verwijdert ontvangen contactrecords op de twaalfmaandsgrens (UTC/calendar months), onafhankelijk van UpdatedAt. Dit raakt geen mailbox, club, account of factuur. Tijdens APIstilstand vindt geen cleanup plaats; volgende iteratie haalt in. PostgreSQLback-ups omvatten deze records na uitrol; feitelijke cloudbackup/restore blijft operatorverificatie.

Contact en wachtlijst hebben onafhankelijke strenge website-noodremmen (elk10 requests/uur per APIproces); contactverkeer kan de wachtlijstmailcapaciteit niet verbruiken. Beheerde wachtlijstmail deelt alleen de wachtlijst-noodrem. Daarnaast geldt de bestaande globale limiter. Dit is geen gedistribueerde quota-garantie; geen IPveld wordt bewaard.

## Bestaande wachtlijstmail behouden

Nieuwe inschrijving maakt de unieke genormaliseerde WebsiteWaitlistEntry plus WebsiteWaitlistMail atomair. Nieuw/duplicaat geven dezelfde {ok:true}, geen publiek token, geen automatische duplicate resend. De entry bewaart SHA256; het ruwe capabilitytoken staat slechts tijdelijk in de gevoelige afzonderlijke mailpayloadtabel. Geen fictieve OrganizationId: dit zijn platformleads vóór clubaanmaak.

De bestaande SMTP/MJML/MIME-flow bezorgt de vaste persoonlijke https://coach-os.be/uitschrijven#…-link: ‘Verwijder mij van de wachtlijst’, niet afmelden voor account-/les-/betaalmails. GET verwijdert niets; expliciete POST verwijdert alleen hashmatch en pending payload. Oude JSONlinks blijven via de websiteroute bruikbaar tijdens gecontroleerde cutover.

Transactionele PostgreSQL advisory-lock serializeert delivery/unsubscribe/import; SMTPtimeout20s, at-least-once (crash na acceptatie kan duplicaat veroorzaken). Na SMTPacceptatie wordt payload direct verwijderd. Begrensde retry: vanaf5s tot1uur, maximaal8pogingen; daarna raw token weg, operatorwaarschuwing zonder PII/providerexception. Payload uiterlijk7dagen of eerder wachtlijsteinde weg. De technische 30dagen-mailqueuepolicy verlengt deze tokenpayloadtermijn niet.

`POST /api/super-admin/website-waitlist/{entryId}/mail`: bestaande SuperAdminpolicy, één bestaande PG-ID, standaard dry-run `{eligible,queued:false}`. Alleen expliciet apply roteert capability/queue't; bestaande payload voorkomt dubbel queue'en. Geen token in response, vrij ontvanger-/URL-/templateveld, publieke resend of bulkloop.

## Werkelijke historische importtool — geen startup/live import

`backend/Scripts/import-website-submissions.py` gebruikt standaard Python + bestaande psql met door de operator geselecteerde PGHOST/PGPORT/PGDATABASE/PGUSER en veilige libpq-auth. Geen appsettings/UserSecrets/.env inlezen, geen website-DBcredentials. De tool is uitsluitend lokaal met synthetische data uitgevoerd.

1. Inventariseer/bescherm huidige bronbestanden vóór recreatie; live locatie/data zijn niet onderzocht. Stop legacy writes tijdens cutover en neem de actuele bron, niet een oude back-up als huidige toestemming.
2. Maak een expliciet gereviewde JSON-suppressielijst met SHA256 van getrimde lowercase e-mailadressen die eerder verwijderd/uitgeschreven zijn. Een lege lijst mag alleen na verificatie; de tool kan onbekende historische verzoeken niet zelf ontdekken. Bronvelden unsubscribedAt/deletedAt worden ook gerespecteerd. Neem geen ruwe tokens/IP's in rapportage op.
3. Dry-run (PG*-omgeving vooraf door bevoegde operator gekozen):
   `python3 backend/Scripts/import-website-submissions.py --kind waitlist --input <actuele-json> --suppressions <hashmanifest-json>`
   Of `--kind contact`. Response bevat alleen aantallen, dry_run en SHA256 van kind + exacte bron + suppressiemanifest, geen persoonsgegevens. Alle invoer wordt eerst gevalideerd; corrupte velden, conflicterende duplicaten, toekomstige/naïeve timestamps blokkeren de hele import. Verlopen contact/wachtlijstdata worden niet geïmporteerd. IP/extra velden worden niet opgeslagen.
4. Pas na review dezelfde opdracht met `--apply --confirm-sha256 <exacte-dryrunhash>`. Verandering van bron OF suppressielijst maakt de bevestiging ongeldig. Eén transactie en advisory-lock, bounded locktimeout; bij fout rollback. Dry-run rolt ook tijdelijke ledgeraanmaak terug. Er wordt nooit mail gequeue'd.
5. Oorspronkelijke received/submittedAt en bestaande capabilityhash blijven behouden. Bestaande PG-wachtlijstentry wordt niet overschreven. Contactimport is inhoud+ontvangstdatum-idempotent; wachtlijst op genormaliseerd e-mailadres. De operationele tabel WebsiteSubmissionImportReceipts bevat alleen SHA256-sleutels en importtijd, geen PII/raw capabilities. Hij blijft behouden na entryverwijdering en voorkomt dat dezelfde import verwijderde entries recreëert. Deze tabel is bewust operatorledger buiten EF, wordt door apply transactioneel aangemaakt en moet samen met de DB worden geback-upt; hij vervangt geen onafhankelijk restore-/suppressionjournal (#265).
6. Reconcilieer bron/eligible/skipped/inserted en herhaalde dry-run. Verwijder/archief bron pas na verificatie volgens beleid; PGbackup dekt oude JSONbestanden niet. Behoud gecontroleerd bridged legacyunsubscribe tot writers definitief uit staan. Herstel van een ouder DBsnapshot vereist replay van latere suppressies vóór netwerktoegang; lokale importidempotentie alleen bewijst dat niet.
7. Historische entries zonder capability vereisen desgewenst per-entry SuperAdmin dry-run/apply om een link te bezorgen. Geen automatische bulkmail. Verifieer oude uitschrijvingen vóór herbezorging.

## Bewijs en operatorgates

- Geobserveerde HTTP404→200 contact-RED/GREEN, echte loopback HTTP→PG, validatie400/rate-limit429, geen e-mail in applicatielog; websiteproxy RED/GREEN.
- Echte PG-contactcleanupgrens met recente UpdatedAt versus receipt; gecorrigeerde auditfixture apart vastgelegd.
- Import: noop-gedrags-RED→echte PG dry-run/apply/reconcile, oorspronkelijke hash, geen mail, na delete geen resurrectie, contactdatum/expiry, foutieve bevestiging/invoer en SQL-metatekens.
- Bestaande volledige wachtlijstmail/SMTP/browserveiligheid blijft vereist en wordt niet opnieuw als nieuw werk opgevoerd. Exacte logs/testcounts/remotechecks staan in PR289 en task-evidence/coachos-privacy/finish-prs.
- Geen productieimport, cloudrestore, live mail, merge of deploy. Live bronreconciliatie, cutover/writepauze, backuprestore/replay en juridische financiële garanties blijven bevoegde operatorgates; #264 niet automatisch sluiten.
