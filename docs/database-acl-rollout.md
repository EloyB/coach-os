# Database-ACL en migraties — rolloutvoorstel (#260)

> CONCEPT / NIET UITGEROLD. Niet mergen/applyen voordat onderstaande workflowafhankelijkheid door een bevoegde eigenaar is afgehandeld. Geen productiecontrole uitgevoerd.

## Wat deze PR wijzigt

Eén `scaleway_rdb_acl` beheert **alle** ACL-regels van de bestaande database. Alleen het gereserveerde openbare IPv4-adres van de bestaande VPS met /32 wordt toegelaten. Geen extra variabele, secret, runner-IP-reeks of 0.0.0.0/0. De provider vervangt alle bestaande ACL-regels: een ‘create’ is dus een operationele beperking, niet zonder risico omdat er geen database-destroy staat.

De echte automatische CI-plan staat in de PR-body. Lokale fmt/backend-false validate zijn geen cloudplan. Een vervanging/vernietiging van database, VPS of bestaande secrets blokkeert deze wijziging onmiddellijk. Geen merge/apply/dispatch toegestaan.

## Verplichte volgorde voor een latere bevoegde uitrol

1. Verwijder eerst de GitHub-hosted `migrate-db`-job en verander deploy-needs naar uitsluitend `build-and-push`. De exacte voorgestelde diff staat **alleen in de PR-body**. Geen workflowbestand is in deze PR gewijzigd.
2. Controleer dat API-startupmigraties op de VPS de juiste rechten hebben en bij een fout niet gezond opstarten. Lokaal is dit met echte PostgreSQL bewezen: lege DB migreert + health200 + volledige seed; een bewust conflicterende tabel laat startup met exit1 stoppen, zonder een HTTP-listener te openen. Dit is lokaal Development-bewijs, niet een productiecheck met Secret Manager.
3. Controleer door een bevoegde operator dat het daadwerkelijke uitgaande IPv4-adres van de VPS overeenkomt met de gereserveerde Terraform-IP en dat geen andere legitieme beheer-/herstelroute afhankelijk is van de oude ACL. Laat uitzondering/toegang voor herstel expliciet beoordelen, niet stil 0.0.0.0/0 toevoegen.
4. Laat de echte actuele plan opnieuw beoordelen, inclusief eventuele drift/write-only attributen. Pas pas na de workflowwijziging en toegangstest de ACL gecontroleerd toe, met herstelafspraak. De bestaande main-workflow kan Terraform automatisch applyen na merge: daarom blijft deze PR draft en geblokkeerd voor uitrol.
5. Verifieer daarna daadwerkelijk: API kan verbinden en migreren; willekeurige externe runner kan niet direct verbinden; rollout-/healthgedrag en herstelroute werken. Agent heeft dit niet op productie getest.
6. Beoordeel pas daarna of GitHub uit de juridische subverwerkerslijsten kan verdwijnen. Ook resterende CI/deploy/cloudsecrettoegang en doorgifte beoordelen; niet automatisch aannemen dat één verwijderde migratiejob elke rol van GitHub opheft.

## Wat niet is aangepast

- `.github/workflows/` is ongewijzigd. Daardoor bestaat de productie-migrate-db-job vandaag nog in de bron en is de gewenste volledige migratieverplaatsing **niet afgerond**.
- Geen API-productiecodewijziging nodig voor het bewezen bestaande fail-fast gedrag: `await ApplyMigrationsAsync()` staat vóór `app.Run()`, fouten leiden tot exit1.
- GitHub blijft in `docs/legal/verwerkersovereenkomst.md` en `privacyverklaring.md`. Geen onjuiste huidige belofte, juridische publicatie, versie of ingangsdatum.
- Geen productie, SSH, cloudcredentials of secrets benaderd; geen Docker/reset/deploy/merge. Lege lokale DB migrate+seed is uitgevoerd, niet Docker-reset.

Issue #260 blijft open totdat de bevoegde workflow- en uitrolstappen plus juridische beoordeling voltooid en echt geverifieerd zijn.
