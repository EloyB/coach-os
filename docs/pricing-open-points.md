# Pricing — open punten om te verfijnen

**Status:** openstaand, later te bespreken.
**Context:** verzameld tijdens het bouwen van de nieuwe prijzen-pagina (okt 2026).
Model zoals vastgelegd: 4 tiers per *actieve leerling* (onbeperkt trainers) —
Game €25/€250 (tot 60), Set €49/€490 (tot 200), Match €89/€890 (tot 500),
Slam op maat (500+/meerdere clubs). Jaarlijks = 2 maanden gratis, **elk jaar**
(ingebakken in de jaarprijs). 30 dagen gratis proef zonder betaalgegevens.
Overschrijding: €3 per leerling/jaar bóven een marge van 10% boven de limiet.

## 1. Overschrijding & upgrade-drempels

Het omslagpunt (meerprijs = jaarverschil met volgende tier) ligt nu vrij hoog:

| Van → naar | Jaarverschil | Leerlingen bóven de marge | ≈ totaal actieve leerlingen |
| --- | --- | --- | --- |
| Game → Set | €240 | 80 | ~146 |
| Set → Match | €400 | ~134 | ~353 |
| Match → Slam | op aanvraag | — | 550+ |

- Bij maandbetaling ligt het iets hoger (jaarverschil Game→Set = (49−25)×12 = €288 → ~96 boven de marge).
- Te bespreken: overschrijding verhogen (bv. €4–€5) **of** de in-app "upgrade voorstellen"-nudge vroeger zetten (bv. bij 50–60% van het tier-verschil) zodat Club-grote clubs eerder naar de voorspelbare tier schuiven.

## 2. Is "boven de marge blijven" gunstig voor ons?

Nee — het is **geen** winstmachine. In de overschrijdingszone betaalt een klant
*minder* dan de volgende tier (bv. Game op 100 leerlingen = €352 < Set €490).
De €3 is bewust **neutraal** geprijsd op het omslagpunt: een zachte, nooit-blokkeren-
overgang, geen opbrengstoptimalisatie. Infra-kost per leerling is ~€0, dus het is
sowieso marge. Wie eerder upgrades (en dus voorspelbare recurring) wil, draait aan
de knoppen uit punt 1. **Beslissing uitgesteld.**

## 3. "1 maand abonneren → exporteren → opzeggen"-lek

Risico: een club neemt 1 maand (€25), zet de reeksen op, verzamelt inschrijvingen,
genereert + exporteert de planning, en zegt dan op — en draait de rest van het
seizoen manueel. Reëel maar beperkt, omdat:

- De waarde stopt niet na de planning (reschedules, late inschrijvingen, communicatie, bevestigingen lopen het hele seizoen).
- **Betalingen via Mollie** zijn het sterkste anker: opzeggen breekt de inning.
- Besparing is klein (~€225/jaar) t.o.v. het gedoe van terug-naar-manueel.
- Doelgroep (clubs/vzw's) optimaliseert hier zelden op.

Mogelijke maatregelen (zonder "nooit blokkeren / altijd exporteren" te breken):

1. Jaarabonnement hard pushen (jaarklanten kunnen niet maandelijks gamen).
2. Doorlopende waarde onmisbaar maken: online betalingen, herinneringen, self-service, reschedule.
3. Abonnement koppelen aan een actief seizoen: open inschrijflink of toekomstige lessen → account blijft actief; opzeggen → enkel-lezen (export blijft, bewerken niet).
4. Maandelijks enkel via mandaat (auto-renew); eventueel minimumtermijn (bv. 3 maanden) voor maandplannen.

**Aanpak:** niet over-engineeren; sterkste verdediging = betalingen + in-seizoen-
features onmisbaar maken + jaarbilling. Monitoring op "opzegt na 1 maand" i.p.v.
zware guards. **Beslissing uitgesteld.**
