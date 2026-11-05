# #264 — contactopslag en gecontroleerde historische cutover

Bestaande PR289 voortzetten, geen duplicaat/stapeling; vastgelegde 18 besluiten gelden.

1. Gedrags-RED echte ontbrekende contact-HTTProute en websiteproxy vastleggen.
2. Minimale API→PG-opslag met servervalidatie, bestaande limiter, geen PIIlogs/websiteDBcredentials; ontvangstdatum server-owned.
3. Werkelijke twaalfmaandscleanup testen op PostgreSQL; UpdatedAt verlengt niet.
4. Offline historische JSONtool: standaard rollback-dryrun, expliciete bron+suppressionhash, volledige validatie, atomaire apply, receiptledger voor idempotentie, bestaande capabilities behouden, geen mail.
5. Werkelijke synthetische PG-import/reconcile/delete-reimport/expired/invalid/SQLmetatekentests; volledige backendmigrate+seed en websitesuite/types/lint/build.
6. Bestaande branch normaal commit/push, PR-body en exacte-head CI teruglezen. Uitrol/livebron/cutover/restoregates onderscheiden van gebouwde code.

Geen productie/cloud/secrets/SSH/apply/workflowedit/merge/mainpush/forcepush/echte mail. UI ongewijzigd, geen verborgen formulieractivatie. Bronreconciliatie/suppressionmanifest en onafhankelijk restorereplay zijn operatorgates, geen fictieve garanties.
