# Websiteformulierdata — voorstel en beheercheck (#264)

> **Operationeel voorstel, niet uitgevoerd op productie.** Dit is geen gepubliceerde privacytekst of juridische goedkeuring.

## Wat de code doet

- Contact- en wachtlijst-API slaan geen `x-forwarded-for`/IP-veld meer op.
- Bij een schrijf- of onderhoudstransactie worden bestaande `ip`-velden verwijderd. Ongeldige JSON, ongeldige records of ongeldige opgegeven lanceringsdatums worden niet stil vervangen: de bewerking faalt zonder het betrokken bronbestand te overschrijven.
- Contactinzendingen ouder dan twaalf kalendermaanden verdwijnen bij onderhoud en bij nieuwe inzendingen. Dit gebruikt `submittedAt`, **niet** een onbekende datum van latere e-mailcorrespondentie. De privacyverklaring zegt ‘laatste contact’: die betekenis moet nog worden afgestemd.
- Nieuwe wachtlijstinschrijvingen krijgen één willekeurige uitschrijfcapability; alleen de SHA-256-hash wordt bewaard. Een duplicaat krijgt de bestaande link niet te zien en roteert die niet. Uitschrijven vereist een expliciete POST; het openen van een link verwijdert niets. Het fragment staat niet in HTTP-URL/querylogs.
- `WEBSITE_LAUNCH_DATE=YYYY-MM-DD` is **optioneel**, zonder verzonnen standaarddatum. Zodra een bevoegde beheerder de werkelijke lanceringsdatum heeft bevestigd en ingesteld, wordt zes kalendermaanden later de wachtlijst leeg gemaakt en worden nieuwe inschrijvingen afgewezen. Ontbreekt de datum, dan meldt de runtime dat deze limiet nog niet actief is. Er is dus nog geen onvoorwaardelijke complianceclaim.
- Onderhoud start via Next.js `instrumentation.register` alleen in de Node-runtime, niet tijdens de productiebuild. Het draait bij startup, vervolgens uiterlijk elk uur, en plant eerder als de zesmaandengrens dichterbij is. Na stilstand wordt bij herstart ingehaald. Tijdens stilstand kan uiteraard geen job draaien.
- Schrijven gebeurt met een exclusieve lockmap en atomische bestandsvervanging, inclusief fsync. Een achtergebleven lock wordt niet blind gestolen: onderzoek eerst of de schrijver nog leeft. Verwijder een lock alleen na die controle en met bevoegde beheerdersrechten.

## Wat in de repository aantoonbaar is

`website/lib/submissions.ts` gebruikt `<process.cwd()>/data`. `website/Dockerfile.prod` gebruikt `/app` als werkdirectory en draait als gebruiker `nextjs` (uid 1001).

`infrastructure/docker-compose.yml` configureert **geen volume** voor de website. Een containerrecreatie kan daardoor inzendingen verliezen. In deze compose staat ook geen back-up van websiteformulierbestanden. De beheerde PostgreSQL-back-up omvat deze JSON-bestanden niet.

Dit is een vaststelling uit broncode, **geen inspectie van de live server**: een handmatig ingestelde live mount of externe back-up is niet gecontroleerd. Productie, SSH, Scaleway en secrets zijn niet benaderd.

## Voor uitrol door een bevoegde beheerder

1. Bevestig of er al live inzendingen zijn, waar die werkelijk staan en of `/app/data` schrijfbaar is voor uid 1001. Deze PR heeft geen live bestanden gelezen of opgeschoond.
2. Regel een persistent, schrijfbaar directoryvolume voor `/app/data`. Bewaar aanwezige overlaybestanden **vóór** een containerrecreatie of nieuwe mount; een lege mount kan bestaande bestanden verbergen. Stel bron en volledigheid vast, niet alleen een kopiecommando uitvoeren.
3. Kies expliciet een beveiligde back-upbestemming en retentie voor deze bestanden. Een mount is geen back-up. Test een herstel met fictieve data en herhaal na herstel de verwijder-/uitschrijfregels; oude back-ups kunnen anders ingetrokken toestemming en IP's terugbrengen.
4. Leg de werkelijke lanceringsdatum vast. Geef `WEBSITE_LAUNCH_DATE` door aan het websiteproces zodra die datum is goedgekeurd. De bestaande compose geeft deze variabele nog niet door; dit is een bewuste uitrolvraag, geen nieuwe verplichte configuratie en geen handmatige productieactie in deze PR.
5. Maak een gecontroleerde uitrolafspraak om bestaande bestanden met de nieuwe code te laten opschonen. Verifieer aantallen, geen IP-velden, behoud van recente contactdata en ongewijzigde andere bestanden. Neem historische back-ups mee in de bewaarbeleidbeslissing.
6. Bestaande wachtlijstregels zonder capability kunnen niet veilig via een herhaalde publieke registratie van een link worden voorzien. Verwerk hun geverifieerde uitschrijfverzoeken via info@coach-os.be, of keur eerst een e-mailbezorg-/migratieflow goed. De website heeft nog geen uitgaande maildienst voor deze links; een nieuwe link wordt bij de inschrijving getoond en moet bewaard worden.

## Vragen voor review

- Welke datum is de officiële lancering, en wie beheert die runtime-instelling?
- Is twaalf maanden na de formulierinzending de bedoelde termijn, of moet een latere e-mailreactie als nieuw contactmoment gelden?
- Welke persistente opslag en back-upbestemming/retentie worden goedgekeurd, inclusief veilige migratie en herstel?
- Hoe bezorgen we bestaande wachtlijstinschrijvers een link zonder een capability aan derden prijs te geven?

De bestaande conceptwaarschuwingen, juridische versies en inwerkingtredingsdatums zijn niet aangepast. PR #287 blijft eigenaar van de bedrijfsgegevens in de inschrijvingsvoorwaarden.
