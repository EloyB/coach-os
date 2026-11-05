# PR289 aanvulling: persoonlijke wachtlijstmail

Scope: wachtlijst behouden, geen formulieren activeren, geen selfserviceherontwerp (#295).

1. Echte HTTP/Mailpit-integratietest RED: nieuwe inschrijving moet duurzaam bewaard worden en bevestigingsmail ontvangen; endpoint ontbreekt op huidige branch.
2. Kleine platformwachtlijstflow via API en PostgreSQL. Dit is expliciet platformdata vóór organisatieaanmaak (geen tenant/OrganizationId), geen toegang tot tenantdata. Dunne gevalideerde endpoints, vaste template en vaste vertrouwde websitehost. Bestaande SMTP/MJML hergebruiken. Eigen beperkte mailtaak op dezelfde rij, geen fictieve organisatie/enrollment in bestaande outbox.
3. Willekeurige capability alleen gehasht in wachtlijst; tijdelijke gevoelige mailcapability uitsluitend tot bezorging, verwijdering of aflopen. Lease/retry/acht pogingen, beperkt bewaren; geen PII/tokenlogs. GET nooit verwijderen, POST expliciete bevestiging; duplicaten zelfde antwoord, geen token of mailherhaling.
4. Websitehandlers doorgeven via server-side backend-URL (bestaande conventie/default, geen DBkeys); bestaande legacy-JSON blijft veilig bewaard/geschoond maar wordt niet automatisch geïmporteerd of gemaild. Beheerde operatorroute expliciet en dry-run, geen publiek resend.
5. Lokaal PostgreSQL/Mailpit keten + failure/retry, volledige backendtests en lege-DB-migratie/complete seed; website tests/types/lint/build en echte browserregressies. Geen extern testverkeer.
6. Gewone commit/push bestaande branch; PR289 actualiseren (Refs264, niet Closes), keuzes en resterend contactopslag/migratiewerk expliciet; exacte remote head/files/checks teruglezen.

Goedgekeurd: lancering 2026-11-15; contactretentie twaalf maanden na ontvangst, mailbox apart; uiteindelijk contact en wachtlijst via API/PostgreSQL. Alleen wachtlijstopslag nu meenemen voor mail; contactmigratie blijft afzonderlijk werk. Geen productie, bulkmail, automatische legacy-import of workflows.
