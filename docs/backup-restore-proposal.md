# Dagelijkse database + keyringbackup met Restic (#265)

> CONCEPT / NIET UITGEROLD. De lokale echte tests hieronder bewijzen geen actuele productieback-up, Scaleway-upload of beheerde cloudrestore. Refs #265; issue blijft open voor operatorgates.

## Vastgelegd ontwerp

Lorenz heeft dagelijks / 14 dagen, Restic naar S3-compatibel Scaleway Object Storage in EU, runtimewachtwoord uit Scaleway Secret Manager en een onafhankelijke herstelkopie in de zakelijke kluis van Lorenz/Eloy goedgekeurd. Normale dagelijkse back-ups stoppen de API niet. Deze punten zijn geen open beleidsvragen meer. Welke kluis, provisionering, eigenaar/RPO/RTO en werkelijk herstel zijn nog operationeel te bevestigen.

Bestaande managed PostgreSQL-back-ups blijven aan: voorgestelde `backup_schedule_frequency=24`, `backup_schedule_retention=14`. Geen nieuwe cloudresources, versionloze secrets of verplichte appconfig worden gemaakt. Echte CI-plan en de afzonderlijke `password_wo`-reviewgate staan in de PR-body; geen onbevoegde apply of wachtwoordwijziging.

## Werkende expliciete operatortool

`scripts/restic_backup.py backup --keyring /operator/gecontroleerde/keyring --staging-parent /private/0700/staging`

De tool gebruikt standaard libpq-configuratie (`PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSFILE`), geïnstalleerde PostgreSQL-client, tar en Restic. Zet geheimen niet op de commandline. Restic gebruikt eigen `RESTIC_REPOSITORY`, `RESTIC_PASSWORD_COMMAND` of private passwordfile en S3-credentialconfiguratie. De app krijgt geen nieuwe configuratie of brede website-DBcredentials.

1. Lees alle reguliere XML-keyringbestanden, inclusief oude keys en revocations. Geen symlinks, submappen, lege ring of onbegrensde inhoud.
2. Maak een consistente volledige PostgreSQL custom-format dump via `pg_dump`; geen API-stop of mutatie van applicatiedata.
3. Lees de volledige ring opnieuw. Is iets toegevoegd, verwijderd of veranderd tijdens de dump, dan fail-closed zonder nieuwe Restic-snapshot. Een volgende run kan opnieuw proberen; wijzig nooit de ring om de test groen te maken. Dit is bewust conservatief bij rotatie/revocation.
4. Bij byte-identieke ring: pak dump, volledige ring en hashmanifest samen in een encrypted Restic-snapshot met vaste host/tag/pad. De private staging wordt op succes en fout verwijderd. Een processcrash kan lokale plaintext achterlaten: staging moet op een afgeschermde/encrypted hostdisk staan; operator beheert opruiming en diskquota. Geen plaintext ZIP uploaden.

Consistentievoorwaarde: de actieve app gebruikt de standaard Data Protection file-store, schrijft een nieuwe key vóór die voor encryptie wordt gebruikt en verwijdert/wijzigt geen keys tijdens capture. Het dubbel lezen detecteert gewone rotatie; dit is geen distributed lock of bescherming tegen kwaadwillige ABA-wijziging. Operator moet dit met de echte volume-/appconfig verifiëren. De lokale test herstelt ciphertext uit de database met uitsluitend de herstelde ring in een vers proces; verkeerde ring/application falen.

`python3 scripts/restic_backup.py prune` gebruikt echte `restic forget --keep-within 14d --group-by host,tags --prune` voor uitsluitend de vaste backuphost/tag. Het newest snapshot blijft bij Restic behouden; 14 dagen is dus geen garantie dat ieder object exact dag14 verdwenen is. Gebruik GEEN bucket-expiry die willekeurige repositoryobjecten na14dagen wist. `restic check --read-data` en periodieke restore zijn afzonderlijke controles.

## Niet-geïnstalleerd scheduler-/secretvoorstel

Reviewbare units staan in `infrastructure/backup/`; niets wordt automatisch geïnstalleerd of enabled. Bevoegde operator moet een dedicated backupidentiteit, private staging, read-only volledige keyringmount, minimaal bevoegde DBbackupaccount, beperkte bucketrechten en alleen access op de gekozen Secret Manager-versie configureren. Geen versie/resource met lege echte waarde in Terraform opnemen.

Voorbeeldconfiguratie (bewust geen echte ID/secret):

```sh
RESTIC_REPOSITORY=s3:https://s3.fr-par.scw.cloud/OPERATOR_BUCKET/coachos
RESTIC_PASSWORD_COMMAND="scw secret version access OPERATOR_SECRET_ID revision=OPERATOR_VERSION raw=true region=fr-par"
PGPASSFILE=/etc/coachos-backup/pgpass
# PGHOST/PGPORT/PGDATABASE/PGUSER door operator, TLS verify-full met juiste CA.
# AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY uit afgeschermde runtimeconfig, niet uit appcredentials kopiëren.
# KEYRING_DIR en STAGING_DIR zijn private/gecontroleerde hostpaden.
```

Scaleway CLI-opdracht gecontroleerd tegen officiële openbare CLI-documentatie; niet tegen een cloudaccount uitgevoerd. S3-upload/IAM/Secret Manager-toegang zijn dus NIET bewezen. Restic-wachtwoord hoort daarnaast onafhankelijk bij Lorenz/Eloy in zakelijke kluis, niet Git/chat/DB/bucket; backuphost met runtime-toegang kan ontsleutelen, dus geen bescherming tegen volledige hostovername claimen. Credentialrotatie en kluisherstel moeten werkelijk geoefend worden. Mislukte unit moet via bestaande operatoralerting worden opgevolgd; geen dagelijkse succesmail of echte mail in deze opdracht.

## Herstel: altijd quarantaine, niet stil activeren

1. Herstel uitsluitend naar aparte afgeschermde omgeving zonder publiek verkeer, jobs of SMTP. `restic check --read-data`; `restic restore SNAPSHOT --target NIEUWE_PRIVATE_MAP`. Pak `capture.tar` met een veilige extractor uit; controleer manifesthashes. Gebruik nooit een onbeoordeeld archief om een actieve ring te overschrijven.
2. Restore dump naar lege disposable DB met `pg_restore --no-owner --no-privileges --exit-on-error`; herstel volledige ring met passende uid/gid en private rechten. Behoud netwerk-/mailquarantaine. Deze tool start geen API, mailworker of release.
3. Verkrijg de **actuele onafhankelijke, complete** verwijder-/uitschrijfledger, ook gebeurtenissen ná de snapshot. Een ledger die uitsluitend in diezelfde oude DB/back-up zit is onvoldoende. Minimale gegevens: organisatie-ID + recordsoort/ID en uitschrijfentry-ID; geen namen, e-mailadressen, vrije antwoorden of tokens. Voor historische import kunnen daarnaast afzonderlijke suppressionhashes/reconciliatie nodig zijn.
4. `python3 scripts/restic_backup.py guard --ledger /private/current-ledger.json` voert alleen tenantgebonden read-only queries uit. Exit3 betekent gewiste/uitgeschreven gegevens zijn teruggekomen: GEEN vrijgave, herstel onder beoordeling opnieuw verwerken en guard herhalen. Exit1 betekent ontbrekende schema's, ongeldige input, verouderde ledger (>24uur), futuretimestamp of technische fout: eveneens geen vrijgave. Exit0 bewijst alleen afwezigheid van opgegeven targets, NIET volledigheid of vrijgavetoestemming.
5. Goedgekeurde herverwerking moet actuele financiële/hold-/tenantguards blijven respecteren; de tool doet bewust geen blinde SQL-cascade of legal-hold-bypass. Bevoegde operator moet volledigheid/laatste ledgerwatermark vaststellen, verwijderingen en uitschrijvingen herverwerken, overige verlopen gegevens controleren en expliciet vrijgeven. Tot dan geen API/SMTP. Deze handmatige gate is geen automatische volledige replay en blijft een operationele acceptatievoorwaarde.

Ledgerformaat:

```json
{"complete_through":"UTC-ISO-TIMESTAMP","erasures":[{"organization_id":"UUID","kind":"enrollment","target_id":"UUID"}],"unsubscribed_entry_ids":["UUID"]}
```

Soorten: `enrollment`, `camp_enrollment`, `organization`; organisatie-target moet gelijk zijn aan organisatie-ID. Een ontbrekende tabel is fout, geen stil overslaan. Deze versie levert geen automatische onafhankelijke ledgerpublicatie vanuit #261/#262 of wachtlijsttransacties. Dat vereist een gecontroleerde externe opslag-/failure-atomicitykeuze; zonder die operationele voorziening mag deletion/restore niet als veilig automatisch heractiveerbaar worden ingeschakeld. De lokale guardproef is geen cloudrestore of afdoende compleet historisch bewijs.

## Echte lokale verificatie

```sh
python3 scripts/tests/verify_keyring_restore.py
COACHOS_BACKUP_LOCAL_TEST=1 python3 -m unittest discover -s scripts/tests -p 'test_*.py' -v
```

Vereist lokaal loopback PostgreSQL127.0.0.1:55432, Restic en .NET10. De integratietests maken uitsluitend eigen willekeurige `backup_test_*`-databases en tijdelijke versleutelde repositories met een duidelijk disposable testwachtwoord; geen echte sleutels/credentials. Ze bewijzen echte pg_dump/Restic/pg_restore, verlies van originele ring en DBinhoud, cryptografisch herstel in nieuwe processen, verkeerde ring/app/passwordweigering, capturefalen tijdens ringwijziging, echte forget/prune + read-data, en een non-mutating restorequarantaineguard. Volledige backend + lege-DBmigraties/APIseed worden apart gerapporteerd.

De oudere `scripts/keyring_snapshot.py` blijft alleen een lokale plaintext/quiesced ZIP-testtool. Die is niet het dagelijkse productieontwerp; geen standaard API-stop goedgekeurd of nodig gemaakt.

## Open operator-/juridische gates

- Secret/bucket/IAM/kluis/scheduler/alerting daadwerkelijk provisioneren en controleren door bevoegde eigenaar; geen huidige werking geclaimd.
- `password_wo`-diff uit echte CI-plan vóór apply afzonderlijk verklaren; geen onbedoelde passwordwijziging toegestaan.
- Onafhankelijke actuele volledige ledger en gecontroleerde herverwerking/vrijgave. Deletion blijft standaard uit; geen automatische heractivatie.
- Managed-cloud/off-host restore met echte afhankelijkheden onder bevoegdheid uitvoeren; lokale test vervangt die niet.
- Financiële/Billit-/legal-holdbesluiten niet afleiden uit technische backupretentie. Geen juridische publicatie/versie/ingangsdatum.

Bronnen: https://restic.readthedocs.io/en/stable/030_preparing_a_new_repo.html (S3); https://restic.readthedocs.io/en/stable/060_forget.html (retentie); https://github.com/scaleway/scaleway-cli/blob/master/docs/commands/secret.md (version access). Openbare bronkopieën staan in task-evidence; geen live cloud benaderd.
