# FEATURE CAROUSEL · "Wat kan CoachOS?" — Instagram

**Platform:** Instagram (CoachOS account)
**Format:** 5-slide carousel (1 static image + 4 MP4 clips, all 1080×1080)
**Time:** Volgende Maandag / Woensdag / Vrijdag · 07:30 CET
**Status:** READY

Deze map bevat alles wat je nodig hebt om de post te plaatsen. AirDrop / share
de hele map naar je telefoon en alle bestanden zijn er bij — al in de juiste
volgorde dankzij de `1-` t/m `5-` prefixes.

---

## Slide order (post in this sequence)

| #   | Type       | File                               | Counter   |
| --- | ---------- | ---------------------------------- | --------- |
| 1   | PNG (hook) | [`1-hook.png`](1-hook.png)         | `01 / 05` |
| 2   | MP4 (9s)   | [`2-lesreeks.mp4`](2-lesreeks.mp4) | `02 / 05` |
| 3   | MP4 (9s)   | [`3-form.mp4`](3-form.mp4)         | `03 / 05` |
| 4   | MP4 (10s)  | [`4-enroll.mp4`](4-enroll.mp4)     | `04 / 05` |
| 5   | MP4 (10s)  | [`5-planner.mp4`](5-planner.mp4)   | `05 / 05` |

De tellers `01/05 → 05/05` zitten al in de visuals gebakken — de volgorde
ligt vast.

---

## Caption

```
Wat kan CoachOS? 🎾

In 4 stappen door jouw lessenseizoen:

→ Maak je lesreeks aan
→ Deel het inschrijfformulier
→ Spelers schrijven zich in via één link
→ De auto-planner berekent het rooster

Wat je vroeger drie weekenden Excel kostte, doe je nu in één middag.

We zoeken nog 1 tester. 1 maand gratis, levenslang 25% korting nadien.
DM ons of mail naar info@coach-os.be.
```

**Hashtags (add to caption end or first comment):**

```
#padelclub #tennisclub #padelbelgie #vlaanderen #tennisleraar #tennisschool #lesplanning #padeltrainer #saasnl #buildinpublic #padel #tennis
```

---

## Posting checklist (mobile)

1. AirDrop / share deze map naar je telefoon. Alle 5 bestanden komen in de
   juiste volgorde binnen omdat ze al `1-…` t/m `5-…` heten.
2. Open Instagram → tap `+` → **Post**.
3. Tap het multi-select icoon (gestapelde rechthoeken rechtsboven in de
   galerij).
4. Selecteer de 5 bestanden in volgorde. Check rechtsboven of de
   mini-thumbnails de juiste teller laten zien (`01/05` eerst, `05/05`
   laatst).
5. Tap **Volgende** → swipe door alle 5 om visueel te checken dat alles
   netjes loopt (vooral: speelt elke MP4 zonder zwart frame aan begin/eind?).
6. **Volgende** → plak de caption hierboven.
7. Voeg `coach-os.be` toe als locatie of in de bio link (geen klikbare
   links in IG captions).
8. **Delen**.

---

## Quality gates voor posten

- [ ] `1-hook.png` opent en toont `01 / 05`
- [ ] Alle 4 MP4's spelen volledig af (geen zwart frame, geen glitch)
- [ ] Tellers op elke MP4 kloppen: `02/05`, `03/05`, `04/05`, `05/05`
- [ ] Volgorde in IG picker correct (hook eerst, planner laatst)
- [ ] Caption kopieert schoon zonder Markdown-artefacten
- [ ] Bio link wijst naar `coach-os.be`
- [ ] Volgende 2u beschikbaar voor DM-antwoorden

---

## Notes

- **Eerste reactie wordt de eerste comment.** Pin daar `coach-os.be` zodat
  de link zichtbaar is naast de caption.
- **DM-template klaarzetten** (zie ook [`../launch-instagram.md`](../launch-instagram.md)):
  > "Hey! Bedankt voor je interesse. Kunnen we 15 min bellen om te zien of
  > jullie club een goeie match is voor de pilot? Stuur me een paar tijden
  > die voor jou werken."
- **Engagement-window**: de eerste 60 min na posten bepalen de feed reach.
  Plan in dat je in die periode actief comments beantwoordt — IG ziet dat
  als signaal dat de post boeit.
- **Saves > likes** voor dit type educatieve carousel. Iemand die saved is
  veel waarschijnlijker een pilot lead dan iemand die alleen liked. Houd
  saves bij in `../../posts.md`.
- **A/B variant voor volgende ronde**: dezelfde 5 slides met een andere
  hook copy ("Hoe plan je een lessenseizoen in 1 middag?") om te zien wat
  beter stopt.

---

## Bronbestanden

Deze map bevat kopieën — de canonieke bronnen leven in:

- Hook: `../../drafts/exports/feature-carousel-slide-1-hook.png` (SVG: `../../drafts/feature-carousel-slide-1-hook.svg`)
- MP4's: `../../animations/out/{create-lessenreeks,enrollment-form,student-enrollment,auto-planner}.mp4`
- Remotion-bronnen: `../../animations/src/compositions/`

Als de bron-MP4's opnieuw gerenderd worden (`cd animations && npm run render:all`),
moeten de kopieën in deze map handmatig ververst worden.
