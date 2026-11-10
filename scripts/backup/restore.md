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

- Het restic-wachtwoord (kluis) en een Scaleway API-key met leesrecht op de bucket.
- `restic` ≥ 0.17 en `postgresql-client-17` op de machine waar je herstelt.

```bash
export RESTIC_REPOSITORY=s3:https://s3.fr-par.scw.cloud/coachos-offsite-backup/restic
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

## Installatie (eenmalig, op de VPS)

```bash
# 1. restic (officiële binary, ≥ 0.17) en pg_dump 17 (PGDG-apt-repo; Ubuntu 22.04 heeft enkel 14)
apt-get install -y bzip2 postgresql-client-17
# restic: download de release van github.com/restic/restic, controleer SHA256SUMS,
# installeer als /usr/local/bin/restic

# 2. Gebruiker en config
useradd --system --no-create-home --shell /usr/sbin/nologin coachos-backup
install -d -m 0750 -o root -g coachos-backup /etc/coachos-backup
install -m 0640 -o root -g coachos-backup infrastructure/backup/offsite-backup.env.example /etc/coachos-backup/offsite-backup.env
#   -> invullen; restic-password en pgpass als 0400 coachos-backup
install -m 0755 scripts/backup/offsite-backup.sh /usr/local/bin/coachos-offsite-backup

# 3. Repository aanmaken (eenmalig) en config testen
set -a; . /etc/coachos-backup/offsite-backup.env; set +a
restic init
systemd-run --pipe --wait -p User=coachos-backup -p AmbientCapabilities=CAP_DAC_READ_SEARCH \
  -p EnvironmentFile=/etc/coachos-backup/offsite-backup.env \
  /usr/local/bin/coachos-offsite-backup --check-config

# 4. Timer activeren en eerste run volgen
cp infrastructure/backup/coachos-offsite-backup.{service,timer} /etc/systemd/system/
systemctl daemon-reload && systemctl enable --now coachos-offsite-backup.timer
systemctl start coachos-offsite-backup.service && journalctl -u coachos-offsite-backup -e
```

Test het herstel minstens één keer per kwartaal op een wegwerp-DB.
