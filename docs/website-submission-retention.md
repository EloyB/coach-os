# Websiteformulieren: opslag, bewaartermijn en eenmalige import (#264)

## Beleid

- Wachtlijst: open tot **2027-05-15 00:00 UTC** (lancering 2026-11-15 + zes maanden). Daarna weigert de API nieuwe inschrijvingen (HTTP 410) en verwijdert de worker alle wachtlijstdata.
- Contactformulier: elk bericht wordt **twaalf kalendermaanden na ontvangst** (UTC) verwijderd. Een bericht van 29 februari verloopt op 28 februari.
- Geen IP-adressen opgeslagen. IP's worden enkel in het geheugen gebruikt voor rate limiting.

## Flow

Website (`/api/contact`, `/api/waitlist`, `/api/waitlist/unsubscribe`) valideert en proxyt naar de API (`/api/website/...`, standaard `http://api:8080/api`, overschrijfbaar met `WEBSITE_API_URL`). De website stuurt enkel het door nginx gezette `X-Real-IP` door als `X-Forwarded-For`; de API vertrouwt die header alleen vanaf het interne Docker-netwerk.

- **Contact**: opgeslagen in `WebsiteContactEntries`. Daarna gaat een melding naar `Website:ContactNotificationEmail` (standaard `info@coach-os.be`). Mislukt die mail, dan wordt dat gelogd zonder persoonsgegevens en blijft het bericht in de database staan.
- **Wachtlijst**: entry (alleen SHA-256 van het uitschrijftoken) en een tijdelijke mailpayload worden samen opgeslagen. De worker verstuurt de bevestigingsmail met persoonlijke uitschrijflink; de payload verdwijnt na verzending, na 8 mislukte pogingen of na 7 dagen.
- **Uitschrijven**: enkel via POST (geen GET, dus linkscanners verwijderen niets).
- **Superadmin**: `POST /api/super-admin/website-waitlist/{entryId}/mail` met `{ "apply": true }` zet voor één bestaande entry een nieuwe link klaar (standaard dry-run).

## Rate limits

| Endpoint | Per IP | Globale noodrem |
|---|---|---|
| `/api/website/contact` | 5 per 10 min | 100 per uur |
| `/api/website/waitlist` | 5 per 10 min | 200 per uur |
| superadmin-mailroute | 30 per uur | - |

Limieten gelden per API-instantie (in geheugen).

## Opschoning

De `WebsiteWaitlistMailWorker` verwerkt elke 2 seconden de wachtlijstmails en draait de contactopschoning hoogstens één keer per uur (`CreatedAt <= cutoff`, gebruikt de index).

## Eenmalige import van de oude JSON-bestanden

`backend/Scripts/import-website-submissions.py` (Python 3 + `psql`, verbinding via de gewone `PG*`-variabelen).

1. Zet de oude bestanden opzij (`data/contact-submissions.json`, `data/waitlist-submissions.json`). Verwijder handmatig records van mensen die al gevraagd hebben om verwijderd te worden; records met `unsubscribedAt`/`deletedAt` worden automatisch overgeslagen.
2. Dry-run (standaard, draait alles in een transactie en rolt terug):
   `python3 backend/Scripts/import-website-submissions.py --kind waitlist --input waitlist-submissions.json`
3. Controleer de aantallen (`source`, `eligible`, `skipped`, `inserted`) en herhaal met `--apply`.
4. Doe hetzelfde met `--kind contact`.
5. Verwijder daarna de JSON-bestanden van de websiteserver.

Gedrag:

- Alles of niets: één ongeldig record (e-mail, datum, velden, rol) blokkeert de hele import. Foutmeldingen noemen enkel recordnummer en veld, nooit inhoud.
- Verlopen data (contact ouder dan 12 maanden, wachtlijst na de deadline) wordt niet geïmporteerd.
- Herhalen is veilig: wachtlijst slaat bestaande e-mailadressen over, contact slaat berichten met hetzelfde adres en dezelfde ontvangstdatum over.
- Wachtlijstrecords behouden hun bestaande `unsubscribeTokenHash`, zodat oude uitschrijflinks blijven werken. Records zonder hash krijgen een willekeurige, unieke hash; wie een link nodig heeft, krijgt die via de superadmin-route.
- Er wordt nooit mail verstuurd of klaargezet.
- Met `--psql "docker exec -i <container> psql -U <user> -d <db>"` kan een ander psql-commando gebruikt worden.

Tot de JSON-bestanden verwijderd zijn, haalt de website-uitschrijfroute een oude link ook uit `data/waitlist-submissions.json`.
