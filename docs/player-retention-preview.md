# Spelerretentie — uitsluitend veilige voorbereiding (#261)

> CONCEPT / NIET UITGEROLD. Geen verwijderfunctie of compliancegarantie.

De read-only `PlayerRetentionPreview` telt uitsluitend oude reeksinschrijvingen, ouder dan 24 kalendermaanden (strikt vóór de UTC-datumgrens). Zowel inschrijving als reeks en elke subquery zijn OrganizationId-gescoped. Reeksen met recente concrete lessen vallen af. Een contactadres dat binnen dezelfde organisatie bij een andere inschrijving of kamp voorkomt valt conservatief volledig af; een gedeeld adres is geen bewezen speleridentiteit. Onbekende/lege contactadressen vallen af.

De dagelijkse worker staat standaard uit. Optionele configuratie: `PlayerRetentionPreview:Enabled=true` plus expliciete `PlayerRetentionPreview:OrganizationIds` GUID-lijst. Dit activeert ALLEEN dry-run: een telling per toegestane organisatie, zonder namen, e-mails, payloads of tokens. Er is geen configuratieschakelaar die verwijderen activeert. Geen SaveChanges, deletes, anonimisering of uitgaande mail. Een expliciete organisatieaanroep buiten de allowlist doet geen queries. Geen nieuwe verplichte configuratie, migratie of gewijzigde API/seed-contracten.

## Nog niet gebouwd — issue blijft open

- Definitief verwijderen/anonimiseren door clubadmin, losse lessen en kampinschrijvingen.
- Volledige laatste-deelnameberekening over reeksen/lessen/kampen en verplaatsingen; de preview is GEEN goedgekeurde verwijderlijst.
- Verwijderen van formulierantwoorden, voorkeuren, verplaatsingsverzoeken, toewijzingen, tokens en groepsverbanden.
- Financiële bewaarplicht, betalingen, geschillen/legal hold en goedgekeurde anonimiseringsvelden.
- Cleanup van verlopen bevestigingstokens, globale magic-link-tokens en afgeronde outbox. Globale magic-link-tokens hebben geen OrganizationId en mogen niet op basis van één tenant of gedeeld e-mailadres verdwijnen. Pending/processing/failed outbox is niet afgerond.
- Back-up/herstelbeleid en gecontroleerde uitrol.

## Beslissingen voor eigenaar/jurist

1. Hard delete of anonimiseren, en welke gegevens blijven hoe lang noodzakelijk voor de clubboekhouding en bewijsvoering?
2. Hoe herkennen we dezelfde speler veilig bij verschillende adressen, gedeelde gezinsadressen en nieuwe inschrijvingen? Welke datum geldt bij verplaatsingen?
3. Wat betekent een vroegtijdig clubverzoek voor betalingen, actieve groepsleden, bevestigingen en openstaande berichten?
4. Welke afzonderlijke globale tokenretentie en outboxretentie zijn goedgekeurd? Wie controleert uitzonderingen/legal holds?

Tot die beslissingen geen onomkeerbare automatische actie. De beheerdersprocedure begint bij identiteit/bevoegdheid en OrganizationId controleren, scope/afhankelijkheden inventariseren en een voorstel laten goedkeuren; geen ad-hoc DELETE op e-mail. Deze PR machtigt geen productieactie.
