# Inschrijvingen-tabel met zoek + groepsvisualisatie

**Datum:** 2026-08-23
**Branch:** `feat/enrollments-table-ux`
**Scope:** frontend-only, inschrijvingen-sectie op de detailpagina van een lesreeks
(`app/(dashboard)/dashboard/lessons/[id]/page.tsx`).

## Probleem

De inschrijvingen staan nu als een platte lijst (`EnrollmentsSection` + `EnrollmentRow`),
één rij per inschrijving. Bij veel inschrijvingen wordt dit onoverzichtelijk, er is geen
manier om te zoeken, en groepen zijn onzichtbaar: groepsleden staan als losse rijen tussen
de rest.

## Doel

Een **tabel** met een **zoekveld** (filter op naam) en een duidelijke **groepsvisualisatie**
zodat je ziet wie als groep is ingeschreven en wie onder welke groep valt.

## Beslissingen (uit brainstorm)

1. **Groepsweergave:** inklapbare groepsrijen. Eén rij per groep (ingeklapt: leider + ledenaantal
   + status), klik = uitklappen naar de leden. Solo's als losse rijen.
2. **Kolommen:** Naam · Contact (e-mail/tel) · Leeftijd (uit geboortedatum) · Ingeschreven · Status · ⋮.
   (Géén aparte categorie-kolom.)
3. **Zoekgedrag:** bij een match op de leider óf een lid wordt de **hele groep uitgeklapt** getoond,
   met het matchende lid gehighlight. Niet-matchende groepen/solo's verdwijnen.

## Ontwerp

### Componenten

- `_components/enrollments-table.tsx` — tabel + zoekveld + groep/solo-rijopbouw. Vervangt de platte
  lijst binnen `EnrollmentsSection`.
- Hergebruikt bestaande logica uit de huidige `EnrollmentRow`: `EditEnrollmentDialog`, annuleren
  (`cancelEnrollment`), "markeer betaald" (`markEnrollmentCashPaid`), details/formulierantwoorden,
  dubbel-detectie.
- `EnrollmentsSection` blijft de data-fetch + kop (titel, teller, inschrijflink) doen en rendert de tabel.
- De oude `EnrollmentRow` wordt vervangen/opgeruimd.

### Data-afleiding

Groeperen op `enrollmentGroupId`:
- `enrollmentGroupId == null` → solo-rij.
- Anders → groep; leider = de inschrijving met `isGroupLeader`. Groepslabel = "Groep · [leidernaam]".
- Ledenaantal = aantal inschrijvingen met dezelfde `enrollmentGroupId`.
- Standaardvolgorde: alfabetisch op naam (groepen op leidernaam), groepen en solo's samen.

Leeftijd: berekend uit `dateOfBirth` (hele jaren t.o.v. vandaag); null → "—".

### Kolommen / rijen

- **Groepskop-rij:** `▸/▾ Groep · [leider]` + badge ledenaantal, status van de leider, "markeer betaald"
  als de groepsbetaling openstaat (leider bezit de betaling). Klik = in-/uitklappen.
- **Ledenrij (uitgeklapt):** ingesprongen; leider gemarkeerd ("leider"); Contact/Leeftijd/Ingeschreven/
  Status/⋮ per lid.
- **Solo-rij:** zelfde kolommen, geen inspringing.
- Rij-klik opent de aanpas-dialog (zoals nu). ⋮-menu: aanpassen / details bekijken / annuleren.

### Zoekveld

- Input bovenaan de tabel, live (debounced ~150ms), filtert op naam (case-insensitive, leider/lid/solo).
- Match in een groep → hele groep uitgeklapt + matchend lid gehighlight; niet-matchende rijen verborgen.
- Leeg zoekveld → normale weergave (groepen ingeklapt).

### Geannuleerde inschrijvingen

- Actieve inschrijvingen standaard getoond; kop-teller telt actieve (Confirmed/Pending), zoals nu.
- Geannuleerde verborgen achter een toggle **"Toon geannuleerde (n)"** (gedimd, doorstreept).

### i18n

Nieuwe teksten (zoek-placeholder, kolomkoppen, "Groep"/"leider"/"solo", "Toon geannuleerde", "Leden",
"jr") naar `messages/nl.json` via `useTranslations`. De bestaande hardcoded NL in de omliggende pagina
blijft ongemoeid (buiten scope).

## Buiten scope

- Backend-wijziging (geen groepsnaam-DTO; label via leider).
- Kolom-sortering (mogelijke latere uitbreiding).
- Bulk-acties.

## Verificatie

- Tabel toont solo's + inklapbare groepen; leider gemarkeerd, ledenaantal klopt.
- Uitklappen/inklappen werkt.
- Zoeken op een groepslid toont de hele groep uitgeklapt met highlight; niet-matchende verdwijnen.
- Leeftijd/contact/inschrijfdatum/status kloppen; null-geboortedatum → "—".
- Aanpassen / annuleren / markeer betaald / details blijven werken.
- Geannuleerde-toggle werkt.
- `bun run build` (typecheck) slaagt; visuele check in de browser met de seed-inschrijvingen.
