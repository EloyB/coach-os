# Mollie Payments instellen

Deze gids beschrijft hoe je Mollie configureert voor CoachOS en hoe een organisatie
inschrijvingsgeld kan ontvangen via online betaling. De koppeling gebruikt Mollie
Connect: elke organisatie koppelt haar eigen Mollie-account; betalingen komen dus
rechtstreeks bij die organisatie terecht.

> **Voor wie?** De beheerder van de CoachOS-installatie doet stap 1 en 2. De
> organisatiebeheerder doet daarna stap 3 en 4 in de CoachOS-interface.

## 1. Mollie-account en OAuth-app

1. Maak een Mollie-account aan of gebruik een bestaand zakelijk account.
2. Voltooi in Mollie de gevraagde organisatie- en verificatiestappen. Zonder een
   geverifieerde organisatie kunnen live betalingen beperkt of geblokkeerd zijn.
3. Open in het Mollie Dashboard **Developers → OAuth applications** en maak een
   OAuth-applicatie voor CoachOS.
4. Registreer exact deze redirect URI. Vervang `<app-domein>` door de publieke
   URL van de CoachOS-app:

   ```text
   https://<app-domein>/api/oauth/mollie/callback
   ```

   De URI is hoofdlettergevoelig en moet exact overeenkomen met de waarde die
   CoachOS gebruikt. Een trailing slash maakt verschil.
5. Bewaar de **Client ID** en **Client Secret** veilig. De secret wordt door
   Mollie slechts beperkt opnieuw getoond; zet hem niet in git, screenshots of
   frontend-code.

## 2. CoachOS configureren

De API heeft de OAuth-gegevens en de publieke webhook-URL nodig. Gebruik in productie
Scaleway Secret Manager; gebruik lokaal de environment variables uit `.env`.

### Productie

Vul deze secrets in Scaleway Secret Manager in:

| Secret | Waarde |
|---|---|
| `Mollie__ClientId` | Client ID van de Mollie OAuth-applicatie |
| `Mollie__ClientSecret` | Client Secret van de Mollie OAuth-applicatie |
| `Mollie__RedirectUri` | Exact dezelfde URI als in Mollie, bijvoorbeeld `https://app.example.be/api/oauth/mollie/callback` |
| `Mollie__WebhookBaseUrl` | Alleen de publieke basis-URL, bijvoorbeeld `https://app.example.be` |

`Mollie__WebhookBaseUrl` wordt gecombineerd met het webhook-pad
`/api/webhooks/mollie`. Zet dus niet het volledige webhook-pad in deze secret.

Bijvoorbeeld met `scw` (nadat de secrets door Terraform zijn aangemaakt):

```bash
scw secret secret-version create <client-id-secret-id> \\
  data='<mollie-client-id>'
scw secret secret-version create <client-secret-secret-id> \\
  data='<mollie-client-secret>'
scw secret secret-version create <redirect-uri-secret-id> \\
  data='https://app.example.be/api/oauth/mollie/callback'
```

De bestaande productie-inrichting maakt `Mollie__WebhookBaseUrl` aan met Terraform.
Controleer na het toevoegen of wijzigen van secrets dat de API-container opnieuw
wordt gestart, zodat de nieuwe configuratie wordt ingelezen.

### Lokaal ontwikkelen

Kopieer `.env.example` naar `.env` en vul minimaal in:

```dotenv
MOLLIE_CLIENT_ID=<client-id>
MOLLIE_CLIENT_SECRET=<client-secret>
```

De lokale redirect URI is:

```text
http://localhost:5142/api/oauth/mollie/callback
```

Die URI moet ook als redirect URI in de Mollie OAuth-applicatie geregistreerd zijn.
Voor een lokale test kun je desgewenst testmodus activeren via:

```dotenv
Mollie__UseTestMode=true
```

Gebruik nooit live credentials of echte klantbetalingen voor lokale tests.

## 3. Organisatie koppelen

1. Meld je aan als **Admin** van de organisatie in CoachOS.
2. Ga naar **Dashboard → Instellingen**.
3. Zoek de sectie **Online betalingen (Mollie)** en kies **Verbind met Mollie**.
4. Log in bij Mollie en keur de gevraagde CoachOS-toegang goed.
5. Controleer of je terugkomt op de CoachOS-instellingenpagina met de melding
   **Mollie succesvol gekoppeld**.

De koppeling wordt per organisatie opgeslagen. Alleen een Admin kan verbinden of
ontkoppelen. Bij ontkoppelen blijven bestaande betalingen zichtbaar, maar nieuwe
inschrijvingen kunnen niet meer via Mollie betalen totdat de organisatie opnieuw
koppelt.

## 4. Betalingen activeren

Een Mollie-koppeling alleen zet online betalen niet automatisch aan voor iedere
lesreeks. Voor een lesreeks:

1. Open de lesreeks in het dashboard.
2. Open de instellingen voor **inschrijfwijze en betaalmethodes**.
3. Kies **Online betalen (Mollie)** als betaalmethode en sla op.
4. Zorg dat de lesreeks een prijs heeft. Bij gebruik van een prijsmatrix moet er
   voor de betreffende deelnemer- en groepsgrootte een bedrag bestaan.
5. Deel daarna de publieke inschrijvingslink.

Deelnemers kunnen vervolgens via de Mollie-checkout betalen met onder andere
iDEAL of Bancontact, afhankelijk van wat voor het Mollie-account beschikbaar is.
Bij groepsinschrijvingen betaalt de groepsleider het totaalbedrag voor de groep.

CoachOS maakt voor elke betaling een webhook-URL aan op:

```text
https://<app-domein>/api/webhooks/mollie
```

Deze URL hoeft normaal niet handmatig in het Mollie Dashboard te worden ingevoerd:
CoachOS stuurt hem mee bij het aanmaken van de betaling. De API haalt de definitieve
status zelf bij Mollie op; vertrouw dus niet alleen op de browser-redirect.

## 5. Controleren

Voer na de configuratie deze controle uit:

- [ ] De redirect URI in Mollie en `Mollie__RedirectUri` zijn exact gelijk.
- [ ] De API kan starten zonder configuratiefouten.
- [ ] Een Admin ziet **Verbind met Mollie** in Instellingen.
- [ ] De OAuth-flow eindigt op CoachOS met **Mollie succesvol gekoppeld**.
- [ ] De status in Instellingen toont de gekoppelde Mollie-organisatie.
- [ ] Een betaalde lesreeks heeft een prijs en online betalen is geselecteerd.
- [ ] Een testinschrijving opent de Mollie-checkout.
- [ ] Na betaling verandert de betalingsstatus in CoachOS naar betaald.
- [ ] In productie is de webhook bereikbaar via HTTPS en antwoordt
      `/api/webhooks/mollie` met een 2xx-status.

Gebruik voor de eerste controle de Mollie-testomgeving/testmodus. Controleer pas
met een echte betaling nadat de OAuth-flow, checkout en webhook allemaal werken.

## Problemen oplossen

### `redirect_uri`-fout bij Mollie

Controleer de drie waarden naast elkaar:

1. de redirect URI in het Mollie Dashboard;
2. `Mollie__RedirectUri` in CoachOS;
3. de URL waarop de gebruiker de API werkelijk bereikt.

Ze moeten exact gelijk zijn, inclusief `https`, host, hoofdletters, pad en eventuele
trailing slash. Achter een reverse proxy moet je expliciet `Mollie__RedirectUri`
instellen; anders kan de API een interne `http`-URI afleiden.

### De koppeling start niet

Controleer of de API `Mollie__ClientId` en `Mollie__ClientSecret` heeft ingelezen
en herstart de API na een secret-wijziging. Controleer ook dat de ingelogde gebruiker
Admin is.

### Online betalen is uitgeschakeld

De organisatie moet eerst gekoppeld zijn. Controleer daarna of de lesreeks een
prijs heeft en of **Online betalen (Mollie)** als betaalmethode is geselecteerd.

### De checkout opent, maar de status wordt niet bijgewerkt

Controleer of `Mollie__WebhookBaseUrl` naar de publieke HTTPS-host wijst en niet naar
een localhost- of intern containeradres. De webhook is:

```text
POST /api/webhooks/mollie
```

Controleer daarnaast de API-logs en of de publieke proxy dit pad doorstuurt naar de
API. De webhook ontvangt `id=tr_...` en CoachOS vraagt daarna de actuele status op
bij Mollie.

### Een testbetaling wordt niet als test herkend

Zet `Mollie__UseTestMode=true` alleen voor de lokale/testomgeving en gebruik de
Mollie-testcredentials of testflow. Zorg dat productie deze instelling niet per
ongeluk erft.
