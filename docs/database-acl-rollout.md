# Databasebeheerstoegang behouden — besluit #260

> CONCEPT / GEEN UITROL. Refs #260. Het oorspronkelijke VPS-only ACL-voorstel is door Lorenz afgewezen en uit de PR-code teruggenomen. Dit is géén afgeronde hardeningfix of veiligheidsgarantie.

## Laatste expliciete beslissing

Lorenz en Eloy behouden directe databasetoegang vanuit hun eigen machines/Rider, ook vanaf wisselende netwerken (bijvoorbeeld remote werken in Spanje). Geen verplichte VPN of SSH-tunnel; geen vaste thuis-/kantoor-IP-allowlist als enige beheerroute. Het eerdere voorstel en de oude groene plan van PR293 zijn GEEN goedgekeurde oplossing.

Daarom bevat deze PR geen `scaleway_rdb_acl`, nieuwe netwerkvariabele of andere Terraform-/runtimewijziging. Alle bestaande Terraformbronbestanden en workflows zijn byte-identiek aan de huidige main-bron. Verwijdering van het nooit uitgerolde branchvoorstel is geen live ACL-verwijdering. Als een operator dit voorstel toch buiten deze opdracht zou hebben toegepast, moet die eerst de echte state/toegang onderzoeken; er is geen productie-/statecontrole uitgevoerd en geen dergelijke drift aangenomen.

## Geen onnodige migratiewijziging

De oude PR-body stelde voor de GitHub `migrate-db`-job te verwijderen om een VPS-only ACL mogelijk te maken. Omdat dat ontwerp is afgewezen, is die workflowwijziging niet meer vereist en niet onderdeel van dit voorstel. Bestaande startupmigraties/fail-fast zijn eerder lokaal met echte PostgreSQL getest, maar dat bewijs is geen opdracht om deploy-/runnerrechten nu te wijzigen. `.github/workflows/` blijft ongewijzigd, geen dispatch of apply.

GitHub blijft eerlijk in de juridische subverwerkersinventaris zolang de werkelijke rol niet apart beoordeeld/gewijzigd is. Geen complianceclaim uit het behouden van huidige bereikbaarheid afleiden.

## Bewijs en grenzen

De configuratieregressie faalde eerst op de aanwezige VPS-only resource en diens afwijking van main, daarna slaagde zij na terugname. Zij controleert afwezigheid van nieuwe ACL-resources en byte-identiteit van alle Terraform- en workflowbronnen. Dit is bron-/configuratiebewijs, geen netwerk-/TLS-/accountcontrole op productie.

De finale exacte-head automatische CI-plan wordt in de PR-body gerapporteerd. Geen lokaal cloudplan, state/secretuitlezing of apply. Een onverwachte DB/VPS/bestaand-secret destroy/replace blokkeert elke infra-uitrolroute onmiddellijk; zelfs een no-op plan bewijst geen veilige bestaande DB-toegang.

## Bewust open hardeningissue

#260 blijft open: de gewenste netwerkhardening is niet geleverd zolang geen ontwerp verenigbaar met deze toegangseis is goedgekeurd en onder bevoegdheid geverifieerd. Geen nieuw VPN-/vaste-IP-voorstel stil invoeren of reeds afgewezen ontwerpen opnieuw als vereiste presenteren.

Een afzonderlijke bevoegde beoordeling kan huidige TLS-certificaatcontrole, persoonsgebonden minimaal bevoegde accounts, credentialrotatie/audit en netwerkblootstelling onderzoeken. Dat is geen bewijs dat die nu ontbreken of correct zijn; deze opdracht verleent geen productie-/DB-/cloudtoegang. Geen echte credentials, bestaande verbinding of productieconfig gewijzigd; geen merge/deploy/juridische publicatie.
