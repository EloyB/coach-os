# Juridische gereedheidsinventaris — concept, geen publicatie

> CONCEPT — eigenaar/advocaatreview blijft verplicht; geen complianceverklaring of publicatiemachtiging. Geen juridische versie of ingangsdatum ingesteld. Open PR is niet gemergd, deployed of enabled.

Werkelijk gecontroleerde snapshot: 2026-11-06T09:46:11.594098+00:00. Basis `ab03c68d5b44afbba73f42fe5314c67878406ab3`. Onafhankelijke branches op main, geen featurebranchstack. Alle18 besluiten hieronder zijn productinput, geen juridische goedkeuring of productiebevoegdheid.

## Betekenis en volledige broninventaris

Gebouwd = code in open PR met echt lokaal bewijs; tekstcorrectie = concept of verduidelijking; onbeslist = concrete juridische/operatorgate, ook als niet-financiële code al geleverd is. Alle actuele occurrences inclusief legenda en exacte vier bronhashes staan in JSON. Baseline-occurrences zijn daarnaast behouden voor herleidbaarheid van iedere oorspronkelijke marker. Markers zijn niet stil verwijderd wegens groene CI.

| Bron:regel | Classificatie | Beoordeling |
|---|---|---|
| docs/legal/algemene-voorwaarden-clubs.md:7 (#1) | tekstcorrectie | Verklarende legenda/conceptwaarschuwing behouden; geen feature of publicatieautorisatie. |
| docs/legal/algemene-voorwaarden-clubs.md:104 (#2) | gebouwd | Waarschuwing gebouwd/getest in open PR288, desktop/mobiel4PASS; niet gemergd/uitgerold of juridisch goedgekeurd. |
| docs/legal/privacyverklaring.md:6 (#1) | tekstcorrectie | Verklarende legenda/conceptwaarschuwing behouden; geen feature of publicatieautorisatie. |
| docs/legal/privacyverklaring.md:25 (#2) | onbeslist | Billit als primair eigen factuurarchief goedgekeurd; noodzakelijke lokale velden/financiële termijnen en garanties na beëindiging blijven externe juridische/accountgates; geen volledig dubbel archief. |
| docs/legal/privacyverklaring.md:26 (#3) | onbeslist | Code geleverd in PR289: contact API→PG,12kalendermaanden vanaf ontvangst, echte cleanup/historische offlineimport en mailflow getest. Marker betreft resterende bevoegde livecutover/uitrol/restore, niet ontbrekende feature. |
| docs/legal/privacyverklaring.md:28 (#4) | onbeslist | PR289: echte registratie→PG→SMTP→uitschrijfbevestiging, historischeimport/dryrun/idempotentie. Lancering15nov2026 goedgekeurd. Uitrol en historischecutover/operatorreplay blijven open. |
| docs/legal/verwerkersovereenkomst.md:6 (#1) | tekstcorrectie | Verklarende legenda/conceptwaarschuwing behouden; geen feature of publicatieautorisatie. |
| docs/legal/verwerkersovereenkomst.md:139 (#2) | gebouwd | Waarschuwing gebouwd/getest in open PR288, desktop/mobiel4PASS; niet gemergd/uitgerold of juridisch goedgekeurd. |
| docs/legal/verwerkersovereenkomst.md:151 (#3) | onbeslist | PR290: echte niet-financiële spelerdelete/adminUI/technischecleanup met tenant/FK/concurrencytests;959backendPASS/1BillitSKIP en echte UI/API/PG. Financieel/holdbeleid en onafhankelijke herstelledger/uitrol blijven gates; geen preview-only meer. |
| docs/legal/verwerkersovereenkomst.md:152 (#4) | onbeslist | Billit als primair eigen factuurarchief goedgekeurd; noodzakelijke lokale velden/financiële termijnen en garanties na beëindiging blijven externe juridische/accountgates; geen volledig dubbel archief. |
| docs/legal/verwerkersovereenkomst.md:193 (#5) | tekstcorrectie | Goedgekeurd dagelijks14d; PR292 wijzigt bronvoorstel en bevat echte lokale Restic/DB/keyringrestore+quarantaineguard. Werkelijke cloud/scheduler/ledger/kluis/operatorrestore niet bewezen; password_wo vóór apply verklaren. Zie afzonderlijke PR292-brondelta. |
| docs/legal/verwerkersovereenkomst.md:203 (#6) | onbeslist | Bruikbaar intern concept opgesteld met gedeeld info@coach-os.be en officieel gecontroleerde EDPBmeldregels. Eigenaars-/advocaatreview, bereikbaarheid/fallback en oefening nog niet bewezen; geen echte melding. |

## Echte finale featureheads en bewijs

| PR | Head/checks | Werkelijk resultaat en resterende gates |
|---|---|---|
| [#287](https://github.com/EloyB/coach-os/pull/287) | `a7cce1999fb39270778a6af6e8928c2c6cb92095`; geen checks op deze head; open, draft=False, merged=False | Bedrijfsgegevens spelersvoorwaarden bestaande PR, niet gedupliceerd. Dit bestand blijft ongewijzigd door PR294. |
| [#288](https://github.com/EloyB/coach-os/pull/288) | `7475386b79f933e85dea91a2c96e9aded8a29d97`; typecheck-test-build:success; open, draft=False, merged=False | Bestaande formulierwaarschuwing behouden; echte desktop/mobiel4PASS. Geen juridische toestemming voor bijzondere gegevens afgeleid. |
| [#289](https://github.com/EloyB/coach-os/pull/289) | `3291c85683c147446d7a9fa46547610181adafbe`; build-and-test:success; open, draft=True, merged=False | 951backendPASS/1externeBillitSKIP;website21tests72assertions/types/lint/build;echte SMTP/browser/PGimport/dryrun/concurrency/suppression/cleanup. Operatorcutover/restoregate;één bestaande lintwarning. |
| [#290](https://github.com/EloyB/coach-os/pull/290) | `2b54a29f471843e1aa6057c5438184bf54f7d841`; build-and-test:success, typecheck-test-build:success; open, draft=True, merged=False | 959backendPASS/1BillitSKIP;legeDBmigrate/seed;echte UI/API/PGles/kampdelete;UI4PASS/unit25PASS/build/types/changedlint. Financieel/hold/restoregates;17 bestaande lintfouten elders. |
| [#291](https://github.com/EloyB/coach-os/pull/291) | `9a35aa7c49a8c4ab9ba8d0d4107e93ca2a26d766`; build-and-test:success, typecheck-test-build:success; open, draft=True, merged=False | 974backendPASS/1BillitSKIP;legeDBmigrate/seed;echte ZIPexport/UI/API/PG/Mailpit/delete/noticeconcurrency. UI3PASS/unit25PASS/build/types/changedlint. Brede browsersuite NIET groen:22PASS20FAIL1onderbroken102nietgerund;17 bestaande lintfouten. Externe factuurPDF/Terms/financial/restoregates. |
| [#292](https://github.com/EloyB/coach-os/pull/292) | `d04ebb8483327645b70325355bda5ab28f2818b8`; apply:skipped, plan:success; open, draft=True, merged=False | 12scriptsPASS;echte pg_dump/Restic/pg_restore+fresh-processdecryption;verkeerde key/app/passwordgeweigerd;echte forget/prune/read-data;restoreguard blokkeert resurrectie.937backendPASS/1skip+legeDBseed. CIplan0add/1change/0destroy,retentie7→14 plus password_wo-reviewgate. Cloud/ledger/kluis/scheduler/alerting/managedrestore niet geverifieerd. |
| [#293](https://github.com/EloyB/coach-os/pull/293) | `2e5a5cc264afc5b6e49aea29e804263ca86a363f`; geen checks op deze head; open, draft=True, merged=False | Afgewezen VPS-only ACL daadwerkelijk verwijderd.2configtestsRED→GREEN;alleTF/workflowsidentiek aanmain. Geen nieuwe exacte-headCI:pathsfilter alleenTF/workflow;oud1ACLaddplan is NIET geldig. Geen no-opcloudplan geclaimd. Onderliggende hardening blijft open;directe dynamischetoegang behouden. |

Geen brede frontendgroenclaim. Echte lokale PostgreSQLtests zijn geen bewijs dat CI dezelfde opt-ins uitvoert. Empty-DBmigrate+APIseed is geen Dockerreset/cloudrestore. Graphify module ontbreekt, niet groen. Geen prodHTTP/DB/SSH/cloudcredentials/echte mail/merge/deploy/apply/workflowwijziging.

## Alle18 vastgelegde besluiten

| Nr | Besluit | Implementatie/gate |
|---|---|---|
| 1: Lancering | 15 november2026; uiterste wachtlijstgrens15mei2027 | Gebouwd als code-default PR289; niet uitgerold |
| 2: Formulierclock | 12kalendermaanden na ontvangst, niet mailboxactiviteit | API/PGcleanup getest PR289 |
| 3: Opslag | API→bestaandePG, geen brede websiteDBcredentials; gecontroleerde historischeimport | Gebouwd/getest PR289; livecutover operator |
| 4: Wachtlijst | Behouden; persoonlijke bevestigingsuitschrijflink alleen wachtlijst; #295 later | Echte lokale SMTP/browserflow PR289; geen bulkmail |
| 5: Speleractie | Persoonsgegevens/vrijeantwoorden weg; alleen echt anonieme statistiek; minimaal financieel bewijs apart | Niet-financiële uitvoering PR290; financiële velden/termijnen/holds open |
| 6: Spelerclock | 24maanden sinds laatste deelname eigen club; actief/toekomstig blokkeert; geen login/mail/andereclub | Gebouwd/getest PR290 |
| 7: Vroegtijdig speler | Clubadmin preview/bevestiging, geen financiële/holdbypass | Echte UI/API/PG PR290 |
| 8: Technischecleanup | Used/expiredlinks; completedmail30dagen; pending/failure en tijdelijke tokenpolicy behouden | Gebouwd/getest PR290/289 |
| 9: Offboarding | Werkelijk einde betaald/proef;30dagen adminread/export;dan dicht;dag90processing;reactivatie recheck | Niet-financiële uitvoering PR291; financiële/Terms/restoregates |
| 10: Notices | Einddatum en7dagen voor dag90 alleen actieveadmins;begrensde retry/waarschuwing | Echte lokale Mailpit/PG PR291 |
| 11: Vroegtijdig organisatie | Superadmin met geverifieerdclubadminverzoek,preview,extrabevestiging;andereclubs/accounts intact | Gebouwd/getest PR291; holds blijven |
| 12: Export | BeveiligdeclubadminZIP CSV+JSON,geen tokens/andereclubs/mailbijlage | Echte export PR291; externe BillitPDF/financialarchivegate |
| 13: Backup | Restic EUScalewayS3;daily14d;SMruntimepassword;onafhankelijkeeigenaarskluis | Lokale echte Restic/DB/keyringrestore PR292;cloudprovisioning/ledger/operatorgate |
| 14: Beschikbaarheid | Geen standaard dagelijkse APIstop | Tool PR292 stopt API niet; echte lokale jointrestore;volumeverificatie operator |
| 15: Financieel | Billit primair eigen facturen;minimale lokale refs/status;geen dubbelvolledigarchief | Leveranciersgaranties/veldiscount/termijnen externonbevestigd;clubbetalingen apart |
| 16: Incidentcontact | info@coach-os.be bereikt volgens Lorenz beideeigenaars;geen persoonlijkeprimary/deputy | Bruikbaar concept;bezorging/fallback/oefening niet gecontroleerd |
| 17: DBtoegang | Huidige directe toegang ook wisselende netwerken behouden;geen VPN/tunnel/vasteIPdwang | Afgewezen VPSonlycode verwijderd PR293;hardeningissue open |
| 18: Mailbox | Gewone info/demo12maanden na afhandeling;relatie/afspraak/geschil apart | Conceptbeleid aangepast;geen mailboxautomatisering |

## Afzonderlijke documentdeltas en juridische grenzen

PR292 heeft een onafhankelijke DPAbackuptekst met twee invulmarkers; de exacte branchbronhash en markers staan in `feature_document_deltas`. Deze legalbranch is niet op PR292 gestapeld. PR289 en PR294 raken beide dezelfde privacytabel; lokale gecombineerde merge moet de finale goedgekeurde juridische tekst behouden, geen oude onbeantwoorde mailboxvraag terugplaatsen. Tekstconflicten zijn expliciete reviewgates, geen impliciete featuredependency.

Studio Swyft BV, Molenzicht17,2380Ravels,België;ondernemingsnummer1032.227.379,btwBE1032.227.379. Bestaande bedrijfsgegevens staan in club/platformdocumenten; PR287 blijft eigenaar van spelersvoorwaarden. De daar bestaande7jaar-tekst is NIET goedgekeurd door dit overzicht;financiële termijnen/velden/startmomenten en Billitgarantie zijn reviewgates,geen nieuwe7/10jaar-regel gegokt.

[Incidentconcept](./privacy-incident-procedure.md) gebruikt info@coach-os.be, verwerker zonder onredelijke vertraging, controller72uur/risico en hoogrisicocommunicatie, volledige incidentdocumentatie;officiëleEDPBbron werkelijk gelezen. Contractuele48uur is geen wettelijke wachttijd. Geen externe melding of mailboxautomatisering. Eigenaars-/advocaatreview/fallback/oefening blijven open.

## Lokale gecombineerde integratie

Eigen detached validation-worktree combineert PR288–294 lokaal, nooit naar main/featurebranch gepusht. **1010backendPASS/1externeBillitSKIP**, legeDBmigrate+volledigeAPIseed inclhoofdtrainerPASS;geen schema-/migratiefout aangetroffen. Beide mailinterfaces en DIregistraties behouden bij2codeconflicten;1privacytabelconflict opgelost met finale goedgekeurde legaltekst. Deze3tekstconflicten moeten ook bij menselijke merge expliciet worden opgelost.

Gecombineerdfrontendunit25PASS/build/typesPASS;website21PASS72assertions/build/types/lintPASS. Frontendfull-lintFAIL17bestaandeerrors14warnings, geen groeneallesclaim. Eerste gecombineerde scriptsuite15PASS/2FAIL:branchspecifiekeTFbytevergelijkingbotsteopgoedgekeurdebackupmetadataenlegalhashsnapshotbotsteopPR292DPAtoevoeging. Accessinvariant is nu beperkt tot access (niet backupinstellingen);branch en gecombineerde legalbronsnapshots zijn beide exact gehashd/geïnventariseerd, onbekende drift blijft fout. Definitieve rerunbewijs staat in de PRbody/task-evidence.

Lokale gecombineerde full-schemaResticrestore en APIbeschikbaarheidsproef wordt in de PRbody met echte uitkomst gerapporteerd; geen cloudrestoreclaim. Bewijs: /opt/data/task-evidence/coachos-privacy/finish-combined-nov6-rerun/ en finish-prs/integration/.

## Inventariscontrole

`python3 -m unittest discover -s scripts/tests -p test_legal_inventory.py -v`

Dit is document-/configuratie-TDD: regressies op oude beleids-/headclaims eerst echt RED, daarna GREEN met actuele bronhashes. Geen backend/UIproductiecode gewijzigd in PR294. Alle conceptwaarschuwingen behouden;geen publicatie/versie/ingangsdatum.
