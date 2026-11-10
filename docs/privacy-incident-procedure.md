# Datalek: wat doen we?

> **Concept.** Nog niet nagekeken door een jurist en nog niet geoefend. Gebruik het als checklist, niet als juridisch advies.

Een datalek is elk incident waarbij persoonsgegevens verloren gaan, uitlekken, onterecht gewijzigd worden of niet meer beschikbaar zijn. Bijvoorbeeld: een gelekte API-key of databasewachtwoord, een club die gegevens van een andere club ziet, een mail met spelersgegevens naar de verkeerde ontvanger, of een backup die kwijt is.

## 1. Melden

Elk vermoeden gaat meteen naar **info@coach-os.be**. Dat adres komt bij Lorenz en Eloy. Wie het eerst leest, bevestigt aan de ander en wordt coördinator voor dit incident.

Zet geen wachtwoorden, tokens of spelersgegevens in Slack, WhatsApp of een GitHub-issue.

## 2. Beperken en vastleggen (eerste uren)

- **Stop het lek gericht.** Draai de gelekte sleutel of het wachtwoord om, sluit de kwetsbare route af, of zet de betrokken functie tijdelijk uit. Leg de app niet volledig plat, tenzij het lek niet anders te stoppen is.
- **Bewaar het bewijs.** Exporteer de relevante logs uit Grafana/Loki voordat ze verlopen, en wis of overschrijf niets.
- **Start een tijdlijn**, in een privé-document en niet in een publieke repo:
  - wanneer ontdekt en door wie;
  - wat er gebeurd is (feiten apart van vermoedens);
  - welke clubs en hoeveel personen geraakt zijn, en welke soort gegevens (ook: minderjarigen?);
  - wat we al gedaan hebben.

## 3. Wie moeten we verwittigen?

Het hangt af van wiens gegevens het zijn.

| Gegevens | Onze rol | Wie verwittigen | Termijn |
|---|---|---|---|
| Spelers, ouders, trainers van een club | **Verwerker** (de club is verantwoordelijke) | De getroffen club(s) | Zonder onredelijke vertraging (AVG art. 33.2). De verwerkersovereenkomst belooft maximaal **48 uur** na ontdekking. |
| Onze eigen klanten (clubbeheerders), wachtlijst, contactformulier, facturatie | **Verwerkingsverantwoordelijke** | De Gegevensbeschermingsautoriteit (GBA), via het online meldformulier op gegevensbeschermingsautoriteit.be | Binnen **72 uur** na ontdekking (art. 33.1), tenzij het lek waarschijnlijk geen risico oplevert voor de betrokkenen |
| Idem, en het risico voor de betrokkenen is **hoog** | Verwerkingsverantwoordelijke | Ook de betrokken personen zelf, in duidelijke taal | Zonder onredelijke vertraging (art. 34) |

- Wacht niet tot alles uitgezocht is. Meld wat je weet en vul later aan.
- Meld je later dan 72 uur, leg dan uit waarom.
- Bij clubgegevens beslist de club of ze de GBA en de spelers verwittigen. Wij geven hen zo snel mogelijk de feiten die ze daarvoor nodig hebben.

**Eerste bericht aan een club of de GBA bevat:** wat er gebeurd is, wanneer, welke soort gegevens en ongeveer hoeveel personen, de mogelijke gevolgen, wat we al gedaan hebben, wat zij zelf kunnen doen, en info@coach-os.be als contactpunt.

## 4. Altijd documenteren

Ook als we beslissen dat melden niet nodig is, leggen we vast: de feiten, de gevolgen, de risicoafweging, de maatregelen, en waarom we wel of niet gemeld hebben (art. 33.5). Bewaar dat bij de tijdlijn.

## 5. Afsluiten

- De oorzaak is opgelost en getest, en er is een fix gedeployed.
- Wordt er een backup teruggezet? Pas dan eerst opnieuw toe wat sindsdien gewist of uitgeschreven werd, voordat de app weer mails verstuurt.
- Noteer wat we de volgende keer anders doen, met een eigenaar en een datum.

## Nog te doen voor dit document "af" is

- [ ] Laten nakijken door een jurist (vooral de 48 uur in de verwerkersovereenkomst).
- [ ] Eén keer oefenen met een fictief incident.
- [ ] Afspreken wie wat doet als één van ons onbereikbaar is.

Bron: EDPB, [Data breaches](https://www.edpb.europa.eu/sme/assess-the-risks/data-breaches_en), en AVG artikels 33 en 34.
