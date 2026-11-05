# Juridische gereedheidsinventaris — concept, geen publicatie

> **CONCEPT — nazicht door eigenaar en advocaat blijft verplicht.** Dit is een technisch overzicht, geen complianceverklaring, juridische goedkeuring of machtiging om te publiceren. Alle bestaande conceptwaarschuwingen, versies en datumplaatsaanduidingen in `docs/legal/` zijn ongewijzigd. Geen productie benaderd, geen merge, deployment of workflowwijziging uitgevoerd.

## Scope en betekenis van de statussen

Basis: vers opgehaalde `origin/main` op `ab03c68d5b44afbba73f42fe5314c67878406ab3`. Alle elf voorkomens van de gevraagde drie markertypes in vier juridische Markdownbestanden zijn afzonderlijk opgenomen, **inclusief de drie verklarende legenda's**. Het spelersvoorwaardenbestand heeft geen van deze drie markertypes, maar wel andere plaatsaanduidingen; die zijn hierdoor niet automatisch ingevuld of goedgekeurd.

De exacte bronquote, bestandsnaam, regel, volgnummer, classificatie, assessment, bron-SHA256 en teruggelezen PR-status staan in [de machineleesbare inventaris](./legal-readiness-inventory.json). De lokale test vergelijkt de inventaris met elke werkelijke bronoccurrence en alle vier bronhashes. Daardoor kan een nieuwe/verplaatste marker niet ongemerkt worden overgeslagen.

- **Gebouwd** betekent uitsluitend: in de genoemde open PR gebouwd en getest. Niet gemergd is niet in main, en is geen bewijs van deployment, activering of juridische naleving.
- **Tekstcorrectie** betekent: legenda behouden of voorgestelde formulering/status verduidelijken; geen toestemming om een waarschuwing te verwijderen of een onbewezen actuele garantie te publiceren.
- **Onbeslist** betekent: implementatie en/of juridische/operationele keuze ontbreekt. Ook gedeeltelijk gebouwde guarded functionaliteit kan hier staan.
- Productie-/activeringsstatus is **niet gecontroleerd**. Voor deze batch zijn geen deployments uitgevoerd. #261/#262 staan bovendien in de code standaard uit en kunnen alleen read-only dry-run activeren, geen verwijdering.

## Iedere occurrence

| ID | Bron en regel op deze basis | Classificatie | Werkelijke status en vervolg |
|---|---|---|---|
| C1 | [Clubs, regel 7](./legal/algemene-voorwaarden-clubs.md#L7) — legenda | Tekstcorrectie | Behoud legenda, conceptwaarschuwing en advocaatreview. Geen feature-acceptatiepunt. |
| C2 | [Clubs, regel 104](./legal/algemene-voorwaarden-clubs.md#L104) — formulierwaarschuwing | Gebouwd, alleen open PR | #263 / PR #288 heeft waarschuwing en regressiebewijs. Niet gemergd of op productie getest. Bestaande juridische uitzondering voor allergieën/aandachtspunten blijft een reviewkeuze. |
| P1 | [Privacy, regel 6](./legal/privacyverklaring.md#L6) — legenda | Tekstcorrectie | Conceptstatus en marker behouden. Geen ingangsdatum of publicatie. |
| P2 | [Privacy, regel 26](./legal/privacyverklaring.md#L26) — contactbewaring/IP | Onbeslist, deels gebouwd | #264 / #289: IP-velden uit nieuwe/bestaande bestandswrites, twaalf kalendermaandenopschoning en onderhoud getest. Juridische ‘laatste contact’ versus `submittedAt`, persistente opslag, back-up en live opruiming niet bevestigd. |
| P3 | [Privacy, regel 27](./legal/privacyverklaring.md#L27) — wachtlijst | Onbeslist, deels gebouwd | #264 / #289: capability en expliciete POST gebouwd, geen lek bij duplicaten. Officiële lanceringsdatum, bestaande abonneelinks/mailbezorging en opslaguitrol onbeslist. Optionele datum is niet ingesteld/gecontroleerd. |
| V1 | [DPA, regel 6](./legal/verwerkersovereenkomst.md#L6) — legenda | Tekstcorrectie | Dezelfde conceptlegenda bewaren; geen automatische verwijdering na samenvoegen van code. |
| V2 | [DPA, regel 137](./legal/verwerkersovereenkomst.md#L137) — formulierwaarschuwing | Gebouwd, alleen open PR | Dezelfde #263 / #288 als C2, afzonderlijke occurrence. Geen nieuwe verwerking/rechtsgrond goedgekeurd. |
| V3 | [DPA, regel 149](./legal/verwerkersovereenkomst.md#L149) — spelerretentie | Onbeslist | #261 / #290 bevat uitsluitend opt-in beperkte read-only preview, geen admin-delete, anonimisering of volledige laatste-deelnameberekening. Betalingen/boekhouding, gedeelde identiteit en afhankelijke records/tokens/outbox blijven vragen. Issue open. |
| V4 | [DPA, regel 150](./legal/verwerkersovereenkomst.md#L150) — organisatieverwijdering | Onbeslist | #262 / #291: preview en voorlopige procedure, geen volledige export, 30/90-dag toegangsgates, notice, financieel archief of definitieve delete. Manuele procedure is geen ongetest deletescript en machtigt geen uitvoering. Issue open. |
| V5 | [DPA, regel 189](./legal/verwerkersovereenkomst.md#L189) — back-uptermijn | Tekstcorrectie, operationeel open | #265 / #292 stelt dagelijks/14dagen voor en maakt de tekst expliciet nog te bevestigen. Echte CI-plan en echte lokale keyringrestore bestaan; off-host versleuteling/scheduler, productie-inrichting en managed-DB-restore zijn niet bewezen. Niet als huidige garantie invullen. |
| V6 | [DPA, regel 199](./legal/verwerkersovereenkomst.md#L199) — interne datalekprocedure | Onbeslist | Geen goedgekeurde nieuwe procedure in deze batch. Eigenaar/bereikbaarheid, escalatie, meldklok, bewijs en oefening vastleggen; advocaat/eigenaar beoordelen de contractuele 48uur. |

### Delta in nog open #292

Deze branch is niet gestapeld op #292. Die PR vervangt de oorspronkelijke back-upmarker V5 door een expliciete ‘na goedkeuring/uitrol/restorecontrole’-marker en voegt één extra invulmarker voor de keyringback-upbestemming, encryptie, herstelidentiteit, frequentie en retentie toe. Die nieuwe occurrence is **onbeslist**; niet een extra voltooid punt. Na eventuele merge de broninventaris opnieuw genereren en testen, niet alvast main als gewijzigd presenteren.

## PR's, echte checks en grenzen

Alle genoemde PR's zijn bij het ophalen van de JSON-snapshot **open en niet gemergd**. #289–#293 zijn draft; #287/#288 niet. Iedere nieuwe issuebranch komt onafhankelijk van dezelfde vers gefetchte main, niet uit de vorige issuebranch.

| Werk | PR | Geverifieerd bewijs | Niet bewezen / nog open |
|---|---|---|---|
| Bedrijfsgegevens spelersvoorwaarden | [#287](https://github.com/EloyB/coach-os/pull/287) | Bestaande PR en head teruggelezen; niet door deze batch gedupliceerd | Samenvoegen/publiceren niet uitgevoerd; overige placeholders niet als voltooid gepresenteerd |
| #263 formulierwaarschuwing | [#288](https://github.com/EloyB/coach-os/pull/288) | Head7475386, automatische typecheck/test/build success; bestaand lokaal/browserbewijs uit handoff | Geen productie-uitrol of juridische goedkeuring |
| #264 websiteformulierdata | [#289](https://github.com/EloyB/coach-os/pull/289) | Head a5a231e; opnieuw 14 tests/53 assertions, types, lint en build pass; echte lokale browsergroepen en screenshot gecontroleerd; raw screenshot200 image/png | Geen automatische PR-check aangetroffen voor website; één bestaande lintwarning. Opslag/datum/last-contact/bestaande links open |
| #261 spelerretentiepreview | [#290](https://github.com/EloyB/coach-os/pull/290) | Head08f9c3c; 2 gerichte tests; volledige backend939 pass/1 externe Billit skip; lege lokale DB migraties + complete API-seed; automatische build-and-test success | Geen verwijderfunctie; nieuwe querytests gebruiken EF InMemory, geen PG-delete/FK-test geclaimd |
| #262 offboardingpreview | [#291](https://github.com/EloyB/coach-os/pull/291) | Head0a01ca3; 7 gerichte tests; volledige backend944 pass/1 externe Billit skip; lege lokale DB migraties + complete API-seed; automatische build-and-test success | Geen volledige offboarding; nieuwe previewtests EF InMemory; financiële/notice/access/exportkeuzes open |
| #265 backupvoorstel | [#292](https://github.com/EloyB/coach-os/pull/292) | Heada029e33; 6 scripttests; echte fresh-process DataProtectionTokenProtector-restore nadat originele ring verwijderd is; fmt/backend-false validate; aanvullend937 backendtests/1 skip en empty-DB seed; [echte CI-plan41975129240](https://github.com/EloyB/coach-os/actions/runs/41975129240):0 add/1 change/0 destroy | DB-retentie7→14 plus `password_wo` write-only attribuut: vóór apply beoordelen, niet als bewezen wachtwoord-no-op claimen. Geen off-host encryptie/scheduler/managed-DB-restore. Apply-check skipped |
| #260 DB-netwerk/migratievoorstel | [#293](https://github.com/EloyB/coach-os/pull/293) | Headc81c397; configRED/GREEN, fmt/backend-false validate;937 backendtests/1 skip en empty-DB seed; echte migratiefout exit1 zonder HTTP-listener; [echte CI-plan41975714116](https://github.com/EloyB/coach-os/actions/runs/41975714116):1 ACL add/0 change/0 destroy | ACL vervangt ALLE toegangen; eerst bevoegde workflowwijziging en VPS-egress/herstelroute controleren. Workflowdiff alleen in PR-body, geen bronwijziging. GitHub juridisch niet geschrapt. Apply-check skipped |

De volledige backendruns gebruikten de bestaande lokale PostgreSQL-concurrencytests. Empty-DB migraties + API-seed zijn **niet** als Docker-reset of restore van een beheerde back-up gepresenteerd. Externe Billit-test bleef zonder credentials overgeslagen. Geen frontend-/productiecheck geclaimd voor backend/docs-only werk. Graphify-herbouw geprobeerd maar module ontbreekt; dat is gerapporteerd, niet als geslaagd aangemerkt.

## Bedrijfsgegevens en andere niet-markergaten

Bekende gegevens: **Studio Swyft BV, Molenzicht 17, 2380 Ravels, België; ondernemingsnummer 1032.227.379; btw BE 1032.227.379**. Deze staan al in de drie club/platformdocumenten. De bedrijfsgegevens in `algemene-voorwaarden-inschrijving.md` zijn uitsluitend eigendom van open PR #287; deze PR wijzigt dat bestand niet.

Niet elke inhoudelijke lacune heeft één van de drie geselecteerde markers. Dit overzicht keurt andere plaatsen zoals datums, contactadressen, juridische grondslagen, 7jaar versus10jaar boekhouding, log-/secretrotatieclaims of de overige vierkante-hakenplaatsaanduidingen niet goed. Ook de `[TE VERBETEREN]`-securitypunten vallen buiten de drie gevraagde markertypes en zijn niet stil als afgehandeld beschouwd.

GitHub staat nog als (sub)verwerker in de privacy-/DPA-concepten. #260 is niet gemergd en de workflow is ongewijzigd. Pas na echte uitrol en beoordeling van alle resterende CI/deploy-/secrettoegang beoordelen of die tekst mag veranderen. Geen leverancier schrappen op grond van alleen een open ACL-PR.

## Verificatie van deze inventaris

```sh
python3 -m unittest discover -s scripts/tests -p 'test_legal_inventory.py'
```

Dit is een document-/broninventaristest, geen producttest. Eerst echt RED doordat de inventaris ontbrak, daarna GREEN. Deze PR wijzigt geen bestand in `docs/legal/`, workflow of productiecode; geen backend-reset/build/UI-check nodig voor deze documentwijziging. Nieuwe beslissingen, merges en deployments vereisen een hercontrole van de snapshot. Geen juridisch versienummer of datum instellen via deze PR.
