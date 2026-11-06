# Privacyverklaring CoachOS

**Versie:** concept 2026-10 (nog niet in werking)
**Laatst bijgewerkt:** [DATUM INWERKINGTREDING]

> ⚠️ **Concept — na te lezen door een advocaat (privacyrecht).** Wat nog in de software gebouwd moet worden, staat als **[TE BOUWEN]**.

## 1. Wie is verantwoordelijk?

**Studio Swyft BV**, Molenzicht 17, 2380 Ravels, België, ondernemingsnummer 1032.227.379, is de verwerkingsverantwoordelijke voor de gegevens in deze verklaring.

Vragen of verzoeken over je gegevens stuur je naar **info@coach-os.be**.

## 2. Speel je tennis of padel bij een club die CoachOS gebruikt?

Schreef je jezelf of je kind via CoachOS in bij een club, dan is **die club** verantwoordelijk voor je gegevens. Wij verwerken ze alleen in opdracht van de club, als verwerker. Met vragen of verzoeken, zoals inzage of verwijdering, kan je dus het best terecht bij je club. Stuur je ze naar ons, dan bezorgen we ze aan de club.

Hoe de club je gegevens gebruikt, lees je in de privacyverklaring van de club en in de voorwaarden bij de inschrijving.

## 3. Welke gegevens verwerken wij zelf, en waarom?

| Wie | Welke gegevens | Waarvoor | Rechtsgrond | Hoe lang |
|---|---|---|---|---|
| **Clubs en hun gebruikers** (beheerders, trainers) | Naam, e-mailadres, telefoon, naam en adres van de organisatie, rol, inloggegevens (wachtwoord enkel versleuteld), aanvaarde versie van onze voorwaarden | Het account aanmaken en beheren, de dienst leveren, service-e-mails sturen (verificatie, uitnodigingen, herinneringen over de proefperiode) | Uitvoering van de overeenkomst (art. 6.1.b AVG) | Zolang het account bestaat, daarna zoals in artikel 14 van de algemene voorwaarden (export tot 30 dagen, verwijdering na 90 dagen) |
| **Klanten met een abonnement** | Noodzakelijke factuurreferenties/status en overige afzonderlijk vereiste bewijsgegevens; Billit als primair archief voor eigen CoachOS-facturen, geen automatisch volledig dubbel archief | Facturen opmaken en betalingen opvolgen | Uitvoering van de overeenkomst en wettelijke verplichting (boekhouding) | **[INVULLEN: noodzakelijke lokale velden, toepasselijke financiële termijnen en startmomenten juridisch/boekhoudkundig bevestigen; Billit-bewaar-/raadpleeg-/exportgarantie ook na beëindiging verifiëren. Geen termijn gegokt of volledig spelersdossier bewaard wegens één betaling.]** |
| **Wie het contactformulier gebruikt** | Naam, e-mailadres, organisatie en je bericht | Je vraag beantwoorden en je demo of contact opvolgen | Gerechtvaardigd belang (art. 6.1.f AVG): je vraag beantwoorden | Tot 12 maanden na ontvangst van de formulierinzending, niet na latere e-mailcorrespondentie **[TE BOUWEN: gecontroleerde uitrol en historische cutover; contact-API/PostgreSQL, opschoning en offline import gebouwd/getest in open PR #289, niet uitgerold]** |
| **Wie ons rechtstreeks mailt** | Naam, e-mailadres, organisatie en bericht | Je vraag beantwoorden en contact opvolgen | Gerechtvaardigd belang (art. 6.1.f AVG): je vraag beantwoorden | Gewone informatie- en demovragen: twaalf maanden na afhandeling. Correspondentie nodig voor een lopende klantrelatie, afspraken of geschillen afzonderlijk behandelen; geen blinde gezamenlijke verwijdering. Geen mailboxautomatisering door dit concept. |
| **Inschrijvers op de wachtlijst** | E-mailadres en eventueel naam en organisatie | Je verwittigen wanneer CoachOS beschikbaar is | Toestemming (art. 6.1.a AVG); je kan die altijd intrekken | Tot je je uitschrijft, en uiterlijk tot 6 maanden na de goedgekeurde lancering op 15 november 2026 (uiterste grens 15 mei 2027). Persoonlijke uitschrijflink met expliciete bevestiging, alleen voor de wachtlijst, niet account-/les-/betaalmails. **[TE BOUWEN: gecontroleerde uitrol/historische cutover; registratie, mailbezorging, bevestiging en opschoning gebouwd/getest in open PR #289, niet uitgerold]** |
| **Bezoekers van coach-os.be en app.coach-os.be** | IP-adres, browsertype, opgevraagde pagina, tijdstip | Beveiliging, misbruik tegengaan (zoals beperking van het aantal aanvragen) en foutopsporing | Gerechtvaardigd belang (art. 6.1.f AVG): een veilige en werkende dienst | 7 dagen in onze logs |

We verkopen je gegevens niet en gebruiken ze niet voor reclame.

## 4. Cookies en tracking

We gebruiken **geen** cookies of trackers voor statistieken of reclame, en geen analysediensten van derden.

Wanneer je inlogt in de app, bewaart je browser een technische sleutel zodat je ingelogd blijft (local storage). Die is strikt noodzakelijk voor de werking. Je browser verwijdert hem wanneer je uitlogt. Lettertypes laden we vanaf onze eigen server, niet via Google.

## 5. Met wie delen we gegevens?

We werken met deze dienstverleners, die gegevens alleen in onze opdracht verwerken:

| Dienstverlener | Waarvoor | Waar |
|---|---|---|
| Scaleway SAS | Hosting, database, versturen van e-mails | Frankrijk (EU) |
| Google Ireland Ltd (Google Workspace) | Onze mailbox | EU/wereldwijd. Bij doorgifte buiten de EER gelden standaardcontractbepalingen en het EU-US Data Privacy Framework. |
| Mollie B.V. | Betaling van abonnementen **[vanaf de start van betaalde abonnementen]** | Nederland (EU) |
| Billit NV | Facturatie en verzending van facturen via Peppol **[vanaf de start van betaalde abonnementen]** | België (EU) |
| GitHub Inc. | Ontwikkeling en uitrol van nieuwe versies | VS (standaardcontractbepalingen, EU-US Data Privacy Framework); werkelijke rol/doorgiften blijven afzonderlijk te beoordelen. Het afgewezen VPS-only ACL-voorstel van #260 is geen grond om deze leverancier te schrappen. |

Daarnaast geven we gegevens alleen door als de wet ons daartoe verplicht, bijvoorbeeld op vraag van een gerechtelijke instantie.

## 6. Beveiliging

We beveiligen je gegevens met onder meer:
- versleutelde verbindingen (HTTPS);
- wachtwoorden die alleen versleuteld worden opgeslagen;
- strikte scheiding tussen de gegevens van verschillende clubs;
- beperkte toegang voor ons team;
- hosting in de EU.

Meer details staan in bijlage II van de [verwerkersovereenkomst](./verwerkersovereenkomst.md).

## 7. Je rechten

Je hebt het recht om:
- je gegevens in te kijken;
- ze te laten verbeteren of verwijderen;
- de verwerking te laten beperken;
- je gegevens te krijgen in een gangbaar formaat (overdraagbaarheid);
- bezwaar te maken tegen een verwerking op basis van gerechtvaardigd belang;
- je toestemming op elk moment in te trekken.

Stuur je verzoek naar **info@coach-os.be**. We antwoorden binnen een maand. Soms vragen we je om aan te tonen wie je bent.

Ben je niet tevreden over hoe we met je gegevens omgaan, dan kan je een klacht indienen bij de **Gegevensbeschermingsautoriteit**, Drukpersstraat 35, 1000 Brussel, [www.gegevensbeschermingsautoriteit.be](https://www.gegevensbeschermingsautoriteit.be).

## 8. Wijzigingen

We kunnen deze verklaring aanpassen. De datum bovenaan toont de laatste wijziging. Bij belangrijke wijzigingen verwittigen we klanten per e-mail.
