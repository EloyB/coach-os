# Herstel uit de off-site backup

De off-site backup (`offsite-backup.sh`) bewaart twee dingen in een versleutelde
restic-repository op Scaleway Object Storage:

| Tag       | Bestand in snapshot | Inhoud                                          |
|-----------|---------------------|-------------------------------------------------|
| `keyring` | `/keyring.tar`      | ASP.NET DataProtection-sleutels (Docker-volume) |
| `db`      | `/coachos.dump`     | `pg_dump --format=custom` van de database       |

**Zonder de keyring zijn de versleutelde velden in de database (o.a. Mollie-tokens)
onbruikbaar.** Herstel daarom altijd beide.

Eerste keuze bij een DB-probleem blijft de beheerde Scaleway-backup (14 dagen,
via de console). Deze off-site backup is voor als die niet volstaat: Scaleway-
project of VPS kwijt, of een snapshot ouder dan 14 dagen nodig.

## Benodigd

- Het restic-wachtwoord. Staat in Secret Manager (`/offsite-backup/offsite-backup-restic-password`)
  én in de wachtwoordkluis. Is het Scaleway-account zelf kwijt, dan heb je de kluis nodig.
- Een API-key met leesrecht op de bucket (project `coach-os-offsite-backup`).
- `restic` ≥ 0.17 en `postgresql-client-17` op de machine waar je herstelt.

Op de VPS staat alles al klaar in `/etc/coachos-backup/offsite-backup.env`:

```bash
set -a; . /etc/coachos-backup/offsite-backup.env; set +a
restic snapshots --tag coachos
```

Elders (nieuwe machine):

```bash
export RESTIC_REPOSITORY=s3:https://s3.nl-ams.scw.cloud/coach-os-offsite-backup/restic   # tofu output offsite_backup_repository
export RESTIC_PASSWORD_FILE=/root/restic-password   # 0400, uit de kluis
export AWS_ACCESS_KEY_ID=... AWS_SECRET_ACCESS_KEY=...
restic snapshots --tag coachos
```

## 1. Uitgaande mail blokkeren (vóór de API start)

Een oudere database kan mails opnieuw versturen naar mensen die zich intussen
uitschreven of gewist werden. Blokkeer SMTP vanuit de containers tot stap 4 klaar is:

```bash
for p in 25 465 587 2465 2587; do iptables -I DOCKER-USER -p tcp --dport $p -j REJECT; done
```

## 2. Keyring terugzetten

Neem altijd de **nieuwste** keyring-snapshot, ook als je een oudere DB terugzet
(sleutels worden nooit verwijderd, dus een nieuwere keyring kan oudere data lezen).

```bash
docker compose stop api
KEYRING=$(docker volume inspect app_data_protection_keys --format '{{ .Mountpoint }}')
restic dump latest --tag keyring /keyring.tar | tar -C "$KEYRING" -xf -
chown -R 1654:1654 "$KEYRING"   # uid van de 'app'-gebruiker in de API-container
ls -l "$KEYRING"                # verwacht: key-*.xml
```

## 3. Database terugzetten

In een **lege** database (nieuwe Scaleway-instance of een vers aangemaakte DB):

```bash
restic snapshots --tag db                          # kies de snapshot-ID
export PGHOST=... PGUSER=coachos PGDATABASE=coachos PGSSLMODE=require
restic dump <snapshot-id> /coachos.dump \
  | pg_restore --no-owner --no-privileges --exit-on-error --dbname="$PGDATABASE"
```

Zet daarna de connection string in Secret Manager als het een nieuwe instance is,
en start de API: `docker compose up -d api`. Controleer `/health` en of een club
met Mollie-koppeling nog werkt (bewijst dat keyring en DB bij elkaar passen).

## 4. Checklist vóór mail weer aan mag

Alles wat na het tijdstip van de DB-snapshot gebeurde, zit niet in de herstelde
database. Werk dit handmatig af:

- [ ] Noteer het tijdstip van de teruggezette DB-snapshot.
- [ ] Spelers/leden die sinds dat tijdstip **gewist** werden (AVG-verzoek,
      verwijdering door club): opnieuw wissen in de app. Bronnen: privacy-mailbox,
      supporttickets, Grafana-logs.
- [ ] Mensen die zich sinds dat tijdstip **uitschreven** voor mails: opnieuw uitschrijven.
- [ ] Betalingen/abonnementswijzigingen sinds dat tijdstip: nakijken in Mollie.
- [ ] Pas daarna de mailblokkade opheffen:
      `for p in 25 465 587 2465 2587; do iptables -D DOCKER-USER -p tcp --dport $p -j REJECT; done`

## Installatie

Volledig automatisch, er is niets met de hand te doen op de VPS:

1. **Terraform** (`infrastructure/terraform/offsite-backup.tf`) maakt een apart
   project `coach-os-offsite-backup` met een bucket in `nl-ams`, een API-key die
   enkel daar mag schrijven, het restic-wachtwoord, en drie secrets onder
   `/offsite-backup` in Secret Manager.
2. **De workflow** `.github/workflows/offsite-backup.yml` draait na elke
   geslaagde Terraform-run op main en bij wijzigingen aan de backupbestanden. Hij
   voert `infrastructure/backup/install-offsite-backup.sh` uit op de VPS:
   restic en pg_dump installeren, de secrets naar `/etc/coachos-backup/`
   schrijven, de repository aanmaken (eenmalig), de config testen en de timer
   aanzetten.

Eenmalig met de hand:

- Het restic-wachtwoord in de wachtwoordkluis zetten:
  `tofu output -raw offsite_backup_restic_password`.
- Eén eerste backup starten (workflow manueel draaien met *run_now*) en
  daarna een herstel testen volgens dit document.

Status op de VPS: `systemctl list-timers coachos-offsite-backup.timer` en
`journalctl -u coachos-offsite-backup -e`.

Test het herstel minstens één keer per kwartaal op een wegwerp-DB.
