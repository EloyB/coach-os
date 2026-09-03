# Plan: beheer van inschrijvingen door admin en hoofdtrainer

## Doel
Admins en hoofdtrainers moeten vanuit een lessenreeks een deelnemer uit een bestaande groep individueel kunnen annuleren en een nieuwe solo-deelnemer handmatig kunnen inschrijven.

## Productbeslissingen
- Admin: toegang tot alle reeksen in de organisatie.
- Hoofdtrainer: toegang tot reeksen van de gekoppelde hoofdtrainer-clubs.
- Gewone trainer: geen nieuwe schrijfrechten.
- Annuleren blijft een soft-cancel; groepsleden blijven onafhankelijk annuleerbaar.
- Handmatig toevoegen is solo-only, meteen `Confirmed`, zonder planning en zonder betaalflow.
- Dezelfde bevestigingsmail als bij een normale inschrijving wordt via de outbox verstuurd.
- Groepsleden toevoegen en nieuwe groepen samenstellen vallen buiten scope.

## Verticale slices
1. Backend: manual enrollment service + request validation + endpoint, inclusief organisatie/serie-scope, duplicate/capacity/age/form-validatie en bevestigingsmail. RED → GREEN.
2. Backend: cancellation endpoint scope voor hoofdtrainers en regressietests; individuele groepsleden blijven onafhankelijk. RED → GREEN.
3. Frontend: API-client, knop/dialog voor manuele solo-inschrijving en hoofdtrainer-acties; annulatieknop zichtbaar voor bevoegde gebruikers.
4. Verification: backend tests, frontend build/lint en gerichte Playwright-test indien lokale app-configuratie beschikbaar.

## Risico's
- De bestaande submit-flow is publiek en laat `Pending` achter; manual enrollment mag die flow niet hergebruiken omdat dat een bevestigings-/betaalpad kan openen.
- Autorisatie moet zowel de rol als de hoofdtrainer-clubscope afdwingen; UI-gating is niet voldoende.
