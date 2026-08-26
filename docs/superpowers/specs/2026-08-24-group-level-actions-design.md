# Groepen op groepsniveau bedienen

**Datum:** 2026-08-24
**Branch:** `feat/enrollment-details-dialog`
**Scope:** frontend-only, inschrijvingen-tabel op de reeks-detailpagina.

## Probleem

Bij groepen staat de ⋮-actieknop nu op elk lid. Dat is overbodig: een groep wordt beter
als geheel bediend.

## Beslissingen (uit brainstorm)

1. **Uitklappen blijft.** Een groep kan nog steeds uitklappen naar zijn leden.
2. **⋮ enkel op groepsniveau.** Leden worden read-only getoond (geen ⋮, geen klik-actie).
3. **Acties enkel op groepsniveau** (leden zijn ter inzage). Aanpassen van individuele leden is
   een **volgende stap** (aparte knop in de dialog).

## Ontwerp

### Tabel — groep-rij

- Chevron/rij-klik blijft **uitklappen** naar de leden.
- **Leden** worden read-only gerenderd (naam, contact, leeftijd, status; leider-badge blijft) —
  geen ⋮, geen klik.
- De groep-rij krijgt een **eigen ⋮** (groepsniveau, deelt de single-open-menu state):
  - **Details bekijken** → groep-detail-dialog.
  - **Markeer als betaald** — als de leider-betaling openstaat (target = leider).
  - **Groep annuleren** — annuleert alle (niet-geannuleerde) leden, met bevestiging.

### Groep-detail-dialog

Hergebruik van `EnrollmentDetailDialog` met de **leider** als `enrollment` + een nieuwe optionele
prop `groupMembers`:
- **Leden**-sectie: lijst van alle leden (naam, contact, leeftijd), leider gemarkeerd.
- **Formulierantwoorden** + **Beschikbaarheden**: die van de leider (gedeeld door de groep).
- Footer: Sluiten. *(Aanpassen van leden = volgende stap, aparte knop hier.)*

### Solo-rijen

Ongewijzigd (eigen ⋮ + klik → detail-dialog).

### Zoeken

Ongewijzigd: matchende groepen klappen uit met het matchende lid gehighlight (leden blijven inline).

## Buiten scope (volgende stap)

- Aanpassen/annuleren van een individueel groepslid vanuit de dialog.

## Verificatie

- Leden hebben geen ⋮ meer; de groep-rij heeft één ⋮ (Details / Markeer betaald / Groep annuleren).
- Uitklappen + zoeken-met-highlight blijven werken.
- Groep-detail-dialog toont leden (leider gemarkeerd) + leider-beschikbaarheden/antwoorden.
- Groep annuleren zet alle leden op geannuleerd (met bevestiging).
- `tsc --noEmit` groen; visuele check in de browser.
