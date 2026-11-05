# Websiteformulierdata en persoonlijke wachtlijstmail (#264 / PR #289)

> **Reviewbare implementatie, niet gemergd of uitgerold.** Geen productiegegevens, mailprovider, back-ups of live configuratie geïnspecteerd. Geen juridische publicatie of complianceclaim. De wachtlijst blijft behouden; toekomstige herwerking staat in #295.

## Goedgekeurde keuzes

- Officiële lancering: **2026-11-15**. Zes kalendermaanden later is **2027-05-15 00:00 UTC**. De backend hanteert deze eindgrens zonder nieuwe vereiste configuratie. Legacy JSON-onderhoud gebruikt dezelfde goedgekeurde standaard; de al bestaande optionele `WEBSITE_LAUNCH_DATE` blijft voor legacybeheer/testfixtures bestaan. Gebruik bij uitrol geen afwijkende legacydatum.
- Contactformulierinzendingen: verwijderen twaalf kalendermaanden na **ontvangst/submittedAt**, niet na latere mailboxcorrespondentie. Voor mailbox-e-mails is nog geen aparte termijn vastgesteld.
- Gewenste opslag: benodigde formulierdata via de CoachOS-API naar bestaande PostgreSQL, met dezelfde databaseback-ups; geen website-DBcredentials.
- Mail eerst: **nieuwe wachtlijstdata gaan nu via API naar PostgreSQL**. Contactdata blijven in deze PR nog in veilige JSON-opslag. Historische migratie is geen automatische startupactie en is hieronder afzonderlijk begrensd.

## Nieuwe wachtlijstflow

1. Bestaande websitecomponent/route valideert de inschrijving en honeypot. De formulieren worden **niet openbaar gemount** en bestaande CTA's worden niet naar selfservice omgebouwd.
2. De serverroute verstuurt uitsluitend e-mail en optionele rol naar `POST /api/website/waitlist`. Server-only `WEBSITE_API_URL` is optioneel; de standaard `http://api:8080/api` past bij de repository-compose. Geen IP- of Authorizationheader wordt doorgestuurd. De website krijgt geen databasegeheimen.
3. De API valideert opnieuw (maximaal254 tekens, e-mail, rol club/coach/anders). Een globale noodrem staat maximaal10 wachtlijst-/beheerde mailrequests per uur per APIproces toe; de bestaande globale per-IPlimiter geldt daarnaast. Dit is bewust streng, geen gedistribueerde quota-garantie over meerdere APIinstances. Rate-limitstaat is vluchtig, geen opgeslagen IPveld.
4. Eén SaveChanges-transactie maakt de unieke genormaliseerde inschrijving en mailtaak. Concurrerende duplicaten maken hooguit één van beide. Nieuw en duplicaat geven alleen `{ok:true}` terug: geen bestaand of nieuw publiek token, geen publiek resend, geen arbitrary recipient/body/URL-mailrelay.
5. `WebsiteWaitlistEntries` bevat alleen e-mail, optionele rol, auditvelden en SHA-256-capabilityhash. Het is expliciet **platformdata vóór organisatieaanmaak**, niet tenantdata; er wordt geen fictieve OrganizationId aangemaakt en geen bestaande tenantfilter versoepeld.
6. `WebsiteWaitlistMails` is een afzonderlijke **gevoelige** duurzame payloadtabel. Het ruwe willekeurige token is tijdelijk nodig om dezelfde persoonlijke link te kunnen herbezorgen. Het is niet als versleuteld voorgesteld. De ontvanger staat op de gekoppelde inschrijving. Beperk DB-/back-uptoegang; payloads vallen zolang ze bestaan ook in DBback-ups.
7. De worker hergebruikt bestaande `IEmailService`, SMTP-configuratie, MJMLrenderer, logo en MIME/plaintext. Vaste template `website-waitlist` met vaste vertrouwde URL `https://coach-os.be/uitschrijven#<64hex>`; de request-Host of gebruikersinvoer bepaalt nooit de link. De mail zegt **‘Verwijder mij van de wachtlijst’** en sluit account-, les- en betaalmails expliciet uit. Die bestaande mailflows/templates zijn niet gewijzigd.
8. GET opent alleen een bevestigingsscherm; fragmenttoken komt niet in URL/querylogs. Alleen expliciete POST verwijdert de hashmatch plus eventuele pending mailtaak. Andere inschrijvers/accountdata blijven behouden. Herhaalde uitschrijving is idempotent. Oude JSON-capabilities blijven via de websiteroute werken totdat gecontroleerde migratie is afgerond.

## Bezorging, retry en gevoelige gegevens

- Worker elke2seconden; transactionele PostgreSQL-advisory lock serializeert de specifieke wachtlijstbezorging en uitschrijving tussen APIinstances. SMTPpoging heeft een timeout van20seconden; de lock blijft tijdens de poging vast, zodat een uitschrijfbevestiging niet tegelijk door bezorging wordt ingehaald.
- Na SMTPacceptatie wordt de mailpayload meteen verwijderd. SMTP is **at-least-once**: crash na acceptatie vóór DBcommit kan dezelfde link nogmaals bezorgen. Geen claim op exactly-once of daadwerkelijke inboxbezorging.
- Backoff vanaf5seconden, verdubbelend, maximaal1uur; maximaal8 automatische pogingen. Daarna wordt het ruwe token gewist en blijft de mislukte metadata tot de payloadtermijn beschikbaar voor operatoronderzoek.
- Payloads verlopen uiterlijk7dagen na aanmaak of eerder bij de wachtlijsteindgrens. Verlopen, niet-bezorgde taken geven een waarschuwing met aantallen; fouten loggen alleen taak-ID/poging/status, geen ontvanger, providerexception, IP of token. Geen stilteverlies; controleer workerwaarschuwingen bij operationeel beheer.
- Bij de zesmaandengrens worden alleen beide platformwachtlijsttabellen opgeruimd. Startup haalt verlopen data in; tijdens stilstand kan geen cleanup draaien. Contact-/account-/betaaldata worden niet door deze worker verwijderd.
- Verwijdering uit de live DB wist geen historische back-ups of reeds ontvangen mails. Herstel moet eerdere verwijderingen opnieuw respecteren; retentie/toegang van historische back-ups blijft een afzonderlijk beheer-/juridisch onderwerp.

## Beheerde mail voor bestaande inschrijvers

`POST /api/super-admin/website-waitlist/{entryId}/mail` vereist de bestaande **SuperAdminpolicy**, werkt alleen op één bestaande database-ID en kent geen vrij ontvanger-/URL-/templateveld.

- `{}` of `{"apply":false}` is een **dry-run**: alleen `{eligible,queued:false}`, geen tokenuitgifte, hashwijziging of mail.
- Alleen `{"apply":true}` queue't voor een daarvoor in aanmerking komende bestaande inschrijving een nieuwe persoonlijke link. Dit roteert de oude hash. Pending/mislukte payload nog aanwezig? Dan geen tweede taak/rotatie. Eerst fout onderzoeken; na de begrensde payloadtermijn kan een bevoegde operator expliciet opnieuw handelen.
- De APIantwoordbody bevat nooit een token. Dezelfde rate-limiet geldt. Geen openbare route via duplicate registration en geen automatische bulkmail.
- Deze route is lokaal getest op autorisatie (anoniem401), en met echte PostgreSQL op dry-run, apply en duplicaatbescherming. Geen productiesuperadmin gebruikt, geen live mail verzonden.

### Afzonderlijke historische JSON-migratie — NIET automatisch uitgevoerd

Er is bewust **geen blinde JSON-importer of datamigratie bij APIstartup**. De schemamigratie maakt alleen de nieuwe lege tabellen. Historische import en contactopslagmigratie zijn nog afzonderlijk uit te voeren werk; dit is niet als voltooid gerapporteerd.

Veilige operatorprocedure vóór eventuele uitrol:

1. Inventariseer waar de huidige JSONbestanden werkelijk staan en of ze recente gegevens bevatten. Bewaar een beveiligde bronkopie vóór containerrecreatie of een mount. Een lege mount kan overlaybestanden verbergen. Deze PR heeft geen live bestandsinventaris uitgevoerd.
2. Maak een **dry-runmanifest** met aantallen, bron/tijdstip en uniek genormaliseerd e-mailadres, oorspronkelijke submittedAt, toegestane rol en eventueel bestaande capabilityhash. Geen IP/honeypotvelden, geen raw tokens in rapportage. Valideer de hele bron, duplicaten en datums; corrupte records blokkeren de import, niet stil overslaan. Contactdata apart behandelen.
3. Verifieer volledigheid en eerdere uitschrijvingen/verwijderingen; reconcilieer die met alle te importeren/herstellen bronnen. Gebruik nooit een oude bron als actuele toestemming. Plan een korte write-pauze om registratie/uitschrijving tijdens cutover niet kwijt te raken.
4. Laat een bevoegd beheerder de goedgekeurde import in een **expliciete transactie** uitvoeren. Behoud bestaande hashes waar van toepassing; accounts/tenanttabellen blijven onaangeroerd. Maak bij de import **geen mailtaken**. Vergelijk vooraf/achteraf aantallen en controleer een lokaal herstelscenario met fictieve data. Een daadwerkelijk importtool met schema-/idempotentie-/hersteltests is nog nodig vóór een grootschalige historische migratie; niet gokken met ad-hoc productiescripts.
5. Verwijder/archiefbron alleen conform het goedgekeurde bewaarbeleid na verificatie. Voorkom dat opnieuw importeren of terugzetten een reeds uitgeschreven persoon herstelt.
6. Pas na verificatie kan een bevoegde SuperAdmin per bestaande PostgreSQL-entry eerst de dry-runroute en dan expliciet apply gebruiken om de persoonlijke link te bezorgen. Geen bulkloop in deze PR. Bestaande inschrijvers kunnen intussen geverifieerde verzoeken via info@coach-os.be doen.

## Resterende contact-/legacyopslag

Contactinzendingen schrijven nog naar `<website cwd>/data/contact-submissions.json`. Retentie, IPscrubbing, exclusieve locks, fsync en atomische vervanging blijven behouden. Ongeldige bronbestanden worden niet overschreven. Startup en periodiek onderhoud werken zonder nieuw verkeer. Een achtergebleven lock wordt niet blind gestolen.

De repository-compose bevat nog geen websitevolume/back-up voor historische JSON of resterende contactdata. PostgreSQL-back-ups dekken deze bestanden niet. De goedgekeurde eindrichting is **API/PostgreSQL**, niet een nieuw besluit over een JSONback-updienst. Tot contact-/historische migratie moet een bevoegde beheerder verlies bij recreatie voorkomen en de bestaande opslag veilig overbruggen. Geen infrastructuur/config-/secretwijziging in deze aanvulling; netwerkstandaard afgeleid van repository-compose, niet live geverifieerd.

## Bewijs en open punten

- RED→GREEN met echte lokale API: ontbrekend endpoint404, ontbrekende Mailpitmail, ontbrekende expliciete POST; daarna succesvolle gehele keten. Ook websiteproxy, mailcopy in browser, verlopen-payloadwaarschuwing en goedgekeurde launchdefault geobserveerd RED→GREEN.
- Echte geïsoleerde PostgreSQL + Mailpit: registratie→bevestigingsmail→vertrouwde persoonlijke HTML/plaintextlink→GET zonder delete→browserbevestiging via website→alleen eigenaar verwijderd; ander behouden; duplicaatbescherming; hash/payloadcontroles; echte SMTPverbindingsfout→duurzame retry na APIherstart→Mailpitbezorging. Geen externe SMTPrelay.
- Herhaalbare backendregressies staan in `WebsiteWaitlistTests`; websitechecks in `tests/submissions.test.mjs` en `tests/waitlist-proxy.test.mjs`. Evidencebestanden en exacte verificatieresultaten staan in PR289.
- Geen selfserviceherontwerp, geen formulieractivatie, geen productie/bulkmail/deployment/merge/workflowwijziging. #264 blijft open vanwege contactopslag, historische migratie en operationele uitrol. #295 is toekomstig werk, geen duplicaatissue nodig.
- Juridisch nog af te stemmen: afzonderlijke mailboxbewaring en historische back-up-/herstelretentie. Lancering, contactformuliertermijn en PostgreSQLrichting zijn **beslist**, niet opnieuw als productvraag opgevoerd. Conceptwaarschuwingen, juridische versie en ingangsdatum blijven staan.
