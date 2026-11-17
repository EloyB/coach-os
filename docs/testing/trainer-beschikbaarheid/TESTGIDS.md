# Testgids: trainer-beschikbaarheid

Voorbereiding: `cd backend && bash Scripts/reset-db.sh --no-frontend`, wachten op `http://localhost:5142/health` (200), dan `bash Scripts/seed-demo-data.sh`, en `cd frontend && bun dev`.

Accounts na de seed (zie de uitvoer van de seed):

| Rol | E-mail | Wachtwoord |
|---|---|---|
| Admin | `jan@deaces.be` | `Demo1234!` |
| Trainer (gewoon, met eigen tijdvakken) | `pieter@deaces.be` | `Demo1234!` |
| Hoofdtrainer | `sophie@deaces.be` | `Demo1234!` |

## Scenario's

1. **Trainerpagina openen.** Log in als Pieter. De sidebar toont "Instellingen". Klik erop: pagina "Instellingen" met sectie "Beschikbaarheid". Op desktop staat een weekoverzicht met zijn vier tijdvakken (di 17-21 TC De Aces, do 09-12 elke club, do 14-18 Padel Center Brussel, za 10-13 elke club). Het kleurenlegenda toont "Elke club" (groen) en de clubs.
2. **Toevoegen met klikken.** Klik op een leeg stuk van de maandagkolom: formulier "Tijdvak toevoegen" met dag maandag, een startuur gesnapt op een kwartier en een duur van een uur, club "Elke club". Opslaan: het blok verschijnt.
3. **Toevoegen met slepen.** Sleep over een vrij stuk van vrijdag: het formulier opent met die begin- en eindtijd.
4. **Club kiezen.** Kies een club in het formulier: het blok krijgt de kleur van die club.
5. **Wijzigen.** Klik op een blok, pas de eindtijd aan, opslaan.
6. **Verwijderen.** Klik op een blok en kies "Verwijderen".
7. **Overlap.** Voeg een tijdvak toe dat overlapt met een bestaand tijdvak op dezelfde dag: melding "Dit tijdvak overlapt met een ander tijdvak op deze dag." en er wordt niets opgeslagen. Een tijdvak dat precies aansluit (einde = begin van het volgende) mag wel.
8. **Eindtijd voor begintijd.** Melding "De eindtijd moet na de starttijd liggen."
9. **Mobiel.** Maak het venster smal (onder 768 px) of gebruik de telefoon: een lijst per weekdag met "Niet beschikbaar" bij lege dagen en een knop "Tijdvak toevoegen" per dag. Het formulier heeft de juiste dag al gekozen.
10. **Gedeelde lijst met de admin.** Log in als admin, ga naar Trainers, open "Beschikbaarheid" bij Pieter: de tijdvakken uit scenario 2 tot 6 staan er. Voeg als admin een tijdvak toe of verwijder er een, ververs de trainerpagina van Pieter: de wijziging is zichtbaar.
11. **Tijdvak buiten 07:00-23:00.** Voeg als admin een tijdvak 06:00-08:00 toe voor Pieter: in het weekoverzicht loopt de as nu vanaf 06u en het blok is zichtbaar.
12. **Planning gebruikt het.** Maak of open een lesreeks-planning: de slotsuggesties en de waarschuwing "trainer niet beschikbaar" volgen de tijdvakken die Pieter invulde.

## Toegang (negatieve tests)

13. **Admin op de trainerpagina.** Open `/dashboard/trainer-settings` als admin: je wordt naar `/dashboard` gestuurd. De sidebar van de admin toont "Instellingen" naar de admin-instellingen, niet naar de trainerpagina.
14. **Vreemd tijdvak via de API.** Log in als Pieter, kopieer zijn token en probeer een tijdvak van een andere trainer te wijzigen of te verwijderen: `PUT`/`DELETE /api/me/trainer-availabilities/{id}` met dat id geeft 404 en het tijdvak blijft ongewijzigd.
15. **Admin-token op /me.** `GET /api/me/trainer-availabilities` met het admin-token geeft 403.
