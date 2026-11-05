# Back-upvoorstel en lokale keyring-restore (#265)

> CONCEPT / NIET UITGEROLD. Geen bewijs van een actuele productieback-up of beheerde Scaleway-restore.

## Database

De bestaande RDB-resource krijgt `backup_schedule_frequency = 24` (uren) en `backup_schedule_retention = 14` (dagen). Geen nieuwe resource, secret of verplichte configuratie. `disable_backup=false` blijft behouden. De echte automatische CI-planuitkomst staat in de PR-body: lokale fmt/validate zijn GEEN plan of bewijs van een veilige wijziging. Bij destroy/replace van database, VPS of bestaande secrets stopt dit werk; niet toepassen/mergen.

## Lokale keyringtool (geen productieadapter)

`scripts/keyring_snapshot.py` maakt een volledige snapshot van een **vlakke XML-keyringdirectory**, inclusief oude keys en revocation-XML. Geen selectieve keyrotatie of verwijdering. Mappen/symlinks, niet-XML, lege bron of meer dan 512 bestanden/16MiB worden geweigerd. Backup gebruikt 0600-bestand en atomaire no-clobber-publicatie, controleert ZIP-CRC, overschrijft geen bestaand archief en vereist een private 0700-parent buiten de bron. Een falende opname verwijdert geen eerdere back-up.

Writers moeten aantoonbaar gestopt zijn; `--quiesced` is een expliciete bevestiging, geen automatische API-lock. Voor productie vereist dit een goedgekeurde stop of apart bewezen consistentieontwerp. Geen SSH/Docker/stop/deploy is uitgevoerd.

Voor een goedgekeurde lokale test met eigen fictieve directory:

```sh
python3 scripts/keyring_snapshot.py backup /eigen/fictieve-keyring /eigen/private-0700-map/nieuwe-snapshot.zip --quiesced
python3 scripts/keyring_snapshot.py restore /eigen/private-0700-map/nieuwe-snapshot.zip /eigen/nog-niet-bestaande-herstelmap
```

Restore weigert een bestaande doelmap (ook een lege), onveilige/absolute/nested paden, links, dubbele namen, overmatige inhoud en corrupte ZIP/CRC. Na volledige validatie wordt staging atomisch gepubliceerd; bij falen geen gedeeltelijke keyringdoelmap. Bestanden 0600, directory0700. Controleer voor een echte uitrol de bevoegde uid/gid; deze tool neemt niet automatisch de productie-appidentiteit over. Draai niet tegen een actieve ring of als algemene zip-extractor.

**Deze ZIP is NIET versleuteld.** CRC is foutdetectie, geen authenticiteit/vertrouwelijkheid. Upload de plaintext niet als productieback-up. Off-host bestemming, authenticated encryption, onafhankelijke herstelidentiteit, quota, dagelijkse scheduler, monitoring en archiefretentie zijn open operationele keuzes. De tool uploadt/prunet/plaatst geen scheduler en wordt niet automatisch door bestaande workflows geïnstalleerd. Verwijder nooit actieve oude keys omdat een back-uparchief ouder is dan veertien dagen: bestaande ciphertext kan die keys nog nodig hebben.

## Echte lokale verificatie

```sh
python3 -m unittest discover -s scripts/tests -p 'test_*.py'
python3 scripts/tests/verify_keyring_restore.py
```

De tweede check bouwt een lokale .NET-probe tegen de echte `DataProtectionTokenProtector`. Een apart proces beschermt een duidelijk fictieve payload met application name CoachOS en de echte OAuth-purpose. Dat proces stopt vóór opname. De oorspronkelijke ring wordt verwijderd; alleen het herstelde archief wordt in een nieuw proces gelezen. Verkeerde/lege ring en verkeerde application name moeten falen. Geen API-startup, cloud, UserSecrets of werkelijke Mollie-credentials. Bewijs betreft keyringrestore, NIET database-/managed-back-uprestore. Volledige backend en lege lokale PostgreSQL-migraties/API-seed zijn aanvullend uitgevoerd, niet als database-restore geclaimd.

## Nog te beslissen

1. Is dagelijks/14dagen goedgekeurd en welk RPO/RTO, eigenaar en alarmcriterium horen daarbij?
2. Welke versleutelde off-host bestemming/regio en herstelidentiteit? Geen nieuwe appsecret of versionless Secret Manager-resource toevoegen.
3. Mag een korte API-stop voor capture? Hoe wordt de scheduler veilig uitgerold onder het verbod op workflowwijzigingen?
4. Wanneer voert een bevoegde beheerder een managed database-restore naar een nieuwe testomgeving uit? Dat is onder het huidige productie/cloudverbod niet toegestaan.
5. Hoe worden ingetrokken toestemmingen/verwijderingen na restore opnieuw verwerkt (#261/#262), en wat gebeurt met historische/handmatige back-ups?

Geen conceptwaarschuwing verwijderd, juridische versie of ingangsdatum ingesteld. Bijlage II is alleen als voorgesteld/nog te verifiëren bijgewerkt. Issue blijft open tot operationele onderdelen en echte uitrol gecontroleerd zijn.
