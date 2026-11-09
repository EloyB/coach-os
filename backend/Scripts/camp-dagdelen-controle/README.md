# Controle van de migratie `AddCampDayParts`

Doel: aantonen dat de migratie van de bestaande kampen geen data wijzigt of beschadigt.

| Script | Wat | Waar draaien |
|---|---|---|
| `00-overzicht.sql` | Alleen SELECTs: hoe de kampen nu zijn opgebouwd + kampen met een niet-deelbare prijs | Productie (read-only) of de kopie |
| `01-snapshot.sql` | Bewaart de huidige toestand in het schema `camp_snapshot` | **Alleen op de gerestoreerde kopie, vóór de migratie** |
| `02-vergelijking.sql` | Vergelijkt de toestand na de migratie met de snapshot | Dezelfde kopie, na de migratie |

## Volgorde

1. `pg_dump` van productie, herstel op een lege database (de kopie).
2. Draai `00-overzicht.sql` en bekijk het met de tester (ook op productie kan, het schrijft niets).
3. Draai `01-snapshot.sql` op de kopie. **Nog niet migreren.**
4. Migreer de kopie (API starten tegen de kopie, of `dotnet ef database update`).
5. Draai `02-vergelijking.sql`.

## Resultaat lezen

`02-vergelijking.sql` bevat controles die **alleen rijen teruggeven bij een probleem**.
Elke controle heeft een kolom `check` met een nummer. Geen rijen = in orde.
De laatste query (`ROUND-VERSCHIL`) is informatief: kampen waar een nieuwe "heel kamp"-inschrijving
een paar cent afwijkt van de oude hoofdprijs. Daarvan zijn rijen te verwachten bij niet-deelbare prijzen.

Voorbeeld uitvoeren:

```bash
psql "postgresql://USER:PASS@HOST:5432/KOPIE" -f 00-overzicht.sql
```
